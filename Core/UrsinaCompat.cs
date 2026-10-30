using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace RiskOfCSharp2.Core
{
    public static class Engine
    {
        public static readonly List<Entity> RootEntities = new();
        public static readonly List<Entity> AllEntities = new();
        public static readonly List<Tween> ActiveTweens = new();

        public static Camera3D Camera;
        public static Vector3 CameraEuler = new(5f, 0f, 0f); // Вращение камеры как в Ursina
        public static Color BackgroundColor = Utils.Rgb(18, 20, 32);
        public static Font GameFont;
        public static bool HasCustomFont = false;

        // Параметры умного просвечивания объектов карты между камерой и игроком
        public static bool ObstructionFadeEnabled = false;
        public static Vector3 PlayerWorldPos = Vector3.Zero;

        private struct TransparentDrawItem
        {
            public Mesh Mesh;
            public bool Unlit;
            public Color Color;
            public Matrix4x4 RlWorld;
            public float DistSqToCam;
        }

        private static readonly List<TransparentDrawItem> _transparentQueue = new(512);

        private static Mesh _cubeMesh;
        private static Mesh _sphereMesh;
        private static Mesh _diamondMesh;
        private static Material _litMaterial;
        private static Material _unlitMaterial;
        private static Shader _lightShader;

        public static void Init(int width, int height, string title)
        {
            Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
            Raylib.InitWindow(width, height, title);
            Raylib.SetTargetFPS(120);
            Raylib.SetExitKey(KeyboardKey.Null);

            _cubeMesh = Raylib.GenMeshCube(1f, 1f, 1f);
            _sphereMesh = Raylib.GenMeshSphere(0.5f, 16, 16);
            _diamondMesh = Raylib.GenMeshSphere(0.5f, 4, 4);

            // Встроенный GLSL 330 шейдер (DirectionalLight + AmbientLight как в Ursina)
            string vsCode = @"#version 330
in vec3 vertexPosition;
in vec3 vertexNormal;
uniform mat4 mvp;
uniform mat4 matModel;
out vec3 fragNormal;
void main() {
    fragNormal = normalize(mat3(matModel) * vertexNormal);
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}";
            string fsCode = @"#version 330
in vec3 fragNormal;
uniform vec4 colDiffuse;
out vec4 finalColor;
void main() {
    vec3 lightDir = normalize(vec3(-0.45, 0.80, -0.40));
    float diff = max(dot(normalize(fragNormal), lightDir), 0.0);
    float back = max(dot(normalize(fragNormal), -lightDir), 0.0) * 0.22;
    float light = clamp(0.56 + diff * 0.48 + back, 0.0, 1.15);
    finalColor = vec4(colDiffuse.rgb * light, colDiffuse.a);
}";
            _lightShader = Raylib.LoadShaderFromMemory(vsCode, fsCode);
            _litMaterial = Raylib.LoadMaterialDefault();
            _litMaterial.Shader = _lightShader;
            _unlitMaterial = Raylib.LoadMaterialDefault();

            LoadCyrillicFont();
            SoundManager.Init();

            Camera = new Camera3D
            {
                Position = new Vector3(0f, 2.15f, -8.6f),
                Target = new Vector3(0f, 2.0f, 0f),
                Up = Vector3.UnitY,
                FovY = 60f,
                Projection = CameraProjection.Perspective
            };
        }

        private static void LoadCyrillicFont()
        {
            string[] fontPaths =
            {
                "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
                "/usr/share/fonts/truetype/freefont/FreeSansBold.ttf",
                "/usr/share/fonts/TTF/DejaVuSans-Bold.ttf",
                "C:\\Windows\\Fonts\\arialbd.ttf"
            };

            string? foundPath = null;
            foreach (var p in fontPaths)
            {
                if (File.Exists(p))
                {
                    foundPath = p;
                    break;
                }
            }

            if (foundPath == null)
            {
                GameFont = Raylib.GetFontDefault();
                return;
            }

            // Загружаем ASCII + Latin-1 + Кириллицу + спецсимволы (•, ✓, —, «, », ★, ▶, …, №, °, ×, ±, кавычки и стрелки)
            List<int> codepoints = new();
            for (int i = 32; i <= 126; i++) codepoints.Add(i);
            for (int i = 0x00A0; i <= 0x017F; i++) codepoints.Add(i);
            for (int i = 0x0400; i <= 0x04FF; i++) codepoints.Add(i);
            codepoints.AddRange(new[]
            {
                0x03BB,
                0x2013, 0x2014, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2026,
                0x2116, 0x2190, 0x2191, 0x2192, 0x2193, 0x21BB, 0x221E, 0x2248,
                0x2260, 0x2264, 0x2265, 0x23F1, 0x2550, 0x2588, 0x2591, 0x25B2,
                0x25B6, 0x25BC, 0x25C0, 0x25C6, 0x2605, 0x2606, 0x2620, 0x2665,
                0x266A, 0x26A0, 0x26A1, 0x26CF, 0x2713, 0x2715, 0x300E, 0x300F
            });

            GameFont = Raylib.LoadFontEx(foundPath, 32, codepoints.ToArray(), codepoints.Count);
            Raylib.SetTextureFilter(GameFont.Texture, TextureFilter.Bilinear);
            HasCustomFont = true;
        }

        public static void Update(float dt)
        {
            // Обновление твинов и отложенного удаления
            for (int i = ActiveTweens.Count - 1; i >= 0; i--)
            {
                if (ActiveTweens[i].Update(dt))
                    ActiveTweens.RemoveAt(i);
            }

            // Копируем список, чтобы сущности могли безопасно создавать/удалять объекты в Update
            var snapshot = AllEntities.ToArray();
            foreach (var ent in snapshot)
            {
                if (Utils.IsValid(ent))
                    ent.OnUpdate(dt);
            }
        }

        public static void UpdateCameraTargetFromEuler()
        {
            float pitchRad = CameraEuler.X * (MathF.PI / 180f);
            float yawRad = CameraEuler.Y * (MathF.PI / 180f);

            // В Ursina +Z — вперёд, +X — вправо, +Y — вверх
            Vector3 forward = new(
                MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                -MathF.Sin(pitchRad),
                MathF.Cos(yawRad) * MathF.Cos(pitchRad)
            );
            // Для рендера в правосторонней системе Raylib инвертируем X на этапе отрисовки
            Vector3 rlPos = new(-Camera.Position.X, Camera.Position.Y, Camera.Position.Z);
            Vector3 rlFwd = new(-forward.X, forward.Y, forward.Z);

            Camera.Position = rlPos;
            Camera.Target = rlPos + rlFwd;
        }

        public static void Draw3D()
        {
            _transparentQueue.Clear();
            Vector3 rlCamPos = Camera.Position;
            Vector3 camUrsina = new(-rlCamPos.X, rlCamPos.Y, rlCamPos.Z);
            Vector3 playerTarget = PlayerWorldPos + new Vector3(0f, 1.35f, 0f);
            float dtFade = Math.Clamp(Raylib.GetFrameTime(), 0.001f, 0.1f);

            foreach (var root in RootEntities)
            {
                if (!Utils.IsValid(root) || root.IsUI || root.IsPreviewIsolated) continue;

                float alphaMul = 1.0f;
                if (ObstructionFadeEnabled && root.AllowObstructionFade)
                {
                    float targetAlpha = ComputeObstructionTargetAlpha(root, camUrsina, playerTarget);
                    root.ObstructionFadeAlpha = Utils.Lerp(root.ObstructionFadeAlpha, targetAlpha, Math.Clamp(dtFade * 10f, 0f, 1f));
                    alphaMul = root.ObstructionFadeAlpha;
                }
                else
                {
                    root.ObstructionFadeAlpha = 1.0f;
                }

                DrawEntityHierarchy(root, Matrix4x4.Identity, rlCamPos, alphaMul);
            }

            FlushTransparentQueue();
        }

        public static void DrawEntityPreviewIsolated(Entity root, Camera3D previewCam)
        {
            if (root == null || root.IsDestroyed || !root.Enabled) return;
            _transparentQueue.Clear();
            DrawEntityHierarchy(root, Matrix4x4.Identity, previewCam.Position, 1.0f);
            FlushTransparentQueue();
        }

        private static float ComputeObstructionTargetAlpha(Entity root, Vector3 camPos, Vector3 playerPos)
        {
            Vector3 seg = playerPos - camPos;
            float segLenSq = seg.LengthSquared();
            if (segLenSq < 0.25f) return 1.0f;

            Vector3 objCenter = root.Position;
            // Проекция в плоскости XZ на отрезок Камера -> Игрок
            Vector2 a = new(camPos.X, camPos.Z);
            Vector2 b = new(playerPos.X, playerPos.Z);
            Vector2 ab = b - a;
            float abLenSq = ab.LengthSquared();
            if (abLenSq < 0.15f) return 1.0f;

            Vector2 p = new(objCenter.X, objCenter.Z);
            float t = Math.Clamp(Vector2.Dot(p - a, ab) / abLenSq, 0f, 1f);
            if (t > 0.94f) return 1.0f; // Объект позади игрока не перекрывает камеру

            Vector2 closestXZ = a + ab * t;
            float distXZ = Vector2.Distance(p, closestXZ);
            float checkRadius = MathF.Max(1.8f, root.ObstructionRadius);
            if (distXZ > checkRadius) return 1.0f;

            float rayY = camPos.Y + (playerPos.Y - camPos.Y) * t;
            float baseY = objCenter.Y - 1.0f;
            float topY = objCenter.Y + MathF.Max(3.5f, root.ObstructionHeight);
            if (rayY < baseY || rayY > topY) return 1.0f;

            float edgeSoft = Math.Clamp(distXZ / checkRadius, 0f, 1f);
            return Utils.Lerp(0.22f, 0.58f, edgeSoft * edgeSoft);
        }

        private static void FlushTransparentQueue()
        {
            if (_transparentQueue.Count == 0) return;

            _transparentQueue.Sort(static (a, b) => b.DistSqToCam.CompareTo(a.DistSqToCam));

            Rlgl.DrawRenderBatchActive();
            Rlgl.DisableDepthMask();

            for (int i = 0; i < _transparentQueue.Count; i++)
            {
                var item = _transparentQueue[i];
                Material mat = item.Unlit ? _unlitMaterial : _litMaterial;
                unsafe
                {
                    mat.Maps[(int)MaterialMapIndex.Albedo].Color = item.Color;
                }
                Raylib.DrawMesh(item.Mesh, mat, Matrix4x4.Transpose(item.RlWorld));
            }

            Rlgl.DrawRenderBatchActive();
            Rlgl.EnableDepthMask();
            _transparentQueue.Clear();
        }

        private static void DrawEntityHierarchy(Entity ent, Matrix4x4 parentWorld, Vector3 rlCamPos, float alphaMul)
        {
            if (!ent.Enabled || ent.IsDestroyed) return;

            Matrix4x4 local = ent.GetLocalMatrix();
            Matrix4x4 world = local * parentWorld;

            if (!string.IsNullOrEmpty(ent.Model))
            {
                int finalAlpha = (int)MathF.Round(ent.Color.A * alphaMul);
                if (finalAlpha > 0)
                {
                    Mesh? mesh = ent.Model switch
                    {
                        "cube" => _cubeMesh,
                        "sphere" => _sphereMesh,
                        "diamond" => _diamondMesh,
                        _ => null
                    };

                    if (mesh.HasValue)
                    {
                        Matrix4x4 mirrorX = Matrix4x4.CreateScale(-1f, 1f, 1f);
                        Matrix4x4 rlWorld = mirrorX * world * mirrorX;
                        Color drawCol = finalAlpha >= 255
                            ? ent.Color
                            : new Color(ent.Color.R, ent.Color.G, ent.Color.B, (byte)Math.Clamp(finalAlpha, 1, 254));

                        if (drawCol.A >= 255)
                        {
                            Material mat = ent.Unlit ? _unlitMaterial : _litMaterial;
                            unsafe
                            {
                                mat.Maps[(int)MaterialMapIndex.Albedo].Color = drawCol;
                            }
                            Raylib.DrawMesh(mesh.Value, mat, Matrix4x4.Transpose(rlWorld));
                        }
                        else
                        {
                            Vector3 wTrans = rlWorld.Translation;
                            float distSq = Vector3.DistanceSquared(wTrans, rlCamPos);
                            _transparentQueue.Add(new TransparentDrawItem
                            {
                                Mesh = mesh.Value,
                                Unlit = ent.Unlit,
                                Color = drawCol,
                                RlWorld = rlWorld,
                                DistSqToCam = distSq
                            });
                        }
                    }
                }
            }

            for (int i = 0; i < ent.Children.Count; i++)
            {
                var ch = ent.Children[i];
                if (ch != null && !ch.IsDestroyed && ch.Enabled)
                    DrawEntityHierarchy(ch, world, rlCamPos, alphaMul);
            }
        }

        public static void Draw3DBillboardLabels(Camera3D activeRlCam)
        {
            Vector3 camFwd = Vector3.Normalize(activeRlCam.Target - activeRlCam.Position);
            foreach (var ent in AllEntities)
            {
                if (!Utils.IsValid(ent) || string.IsNullOrEmpty(ent.BillboardText)) continue;
                // Если один из родителей скрыт (Enabled = false), не рисуем текст
                Entity? p = ent.Parent;
                bool parentHidden = false;
                while (p != null)
                {
                    if (!p.Enabled || p.IsDestroyed) { parentHidden = true; break; }
                    p = p.Parent;
                }
                if (parentHidden) continue;

                Vector3 wPos = ent.GetWorldPosition();
                Vector3 rlWorldPos = new(-wPos.X, wPos.Y, wPos.Z);
                Vector3 toPoint = rlWorldPos - activeRlCam.Position;

                // Отсекаем точки позади камеры и слишком далёкие надписи (> 65м)
                float dist = toPoint.Length();
                if (dist > 65f || dist < 0.1f) continue;
                if (Vector3.Dot(toPoint / dist, camFwd) <= 0.15f) continue;

                Vector2 screenPos = Raylib.GetWorldToScreen(rlWorldPos, activeRlCam);
                if (screenPos.X > 0 && screenPos.X < Raylib.GetScreenWidth() &&
                    screenPos.Y > 0 && screenPos.Y < Raylib.GetScreenHeight())
                {
                    DrawTextCentered(ent.BillboardText, screenPos.X, screenPos.Y, 18f, ent.Color);
                }
            }
        }

        private static Color ApplySimpleShading(Color c, Vector3 pos)
        {
            float factor = 0.88f + 0.12f * MathF.Sin(pos.Y * 1.3f + pos.X * 0.5f);
            return new Color(
                (byte)Math.Clamp((int)(c.R * factor), 0, 255),
                (byte)Math.Clamp((int)(c.G * factor), 0, 255),
                (byte)Math.Clamp((int)(c.B * factor), 0, 255),
                c.A
            );
        }

        public static void DrawTextCentered(string text, float x, float y, float fontSize, Color col)
        {
            string[] lines = text.Split('\n');
            float totalH = lines.Length * (fontSize + 2f);
            float startY = y - totalH * 0.5f;
            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 sz = Raylib.MeasureTextEx(GameFont, lines[i], fontSize, 1f);
                Vector2 pos = new(x - sz.X * 0.5f, startY + i * (fontSize + 2f));
                Raylib.DrawTextEx(GameFont, lines[i], pos + new Vector2(1, 1), fontSize, 1f, Color.Black);
                Raylib.DrawTextEx(GameFont, lines[i], pos, fontSize, 1f, col);
            }
        }

        public static void DrawTextLeft(string text, float x, float y, float fontSize, Color col)
        {
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 pos = new(x, y + i * (fontSize + 3f));
                Raylib.DrawTextEx(GameFont, lines[i], pos + new Vector2(1, 1), fontSize, 1f, Color.Black);
                Raylib.DrawTextEx(GameFont, lines[i], pos, fontSize, 1f, col);
            }
        }
        public static List<string> WrapText(string text, float fontSize, float maxWidth)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) return result;

            string[] paragraphs = text.Replace("\r\n", "\n").Split('\n');
            foreach (string para in paragraphs)
            {
                if (string.IsNullOrEmpty(para))
                {
                    result.Add("");
                    continue;
                }

                string[] words = para.Split(' ');
                string current = "";
                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i];
                    string candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
                    float w = Raylib.MeasureTextEx(GameFont, candidate, fontSize, 1f).X;
                    if (w <= maxWidth || string.IsNullOrEmpty(current))
                    {
                        current = candidate;
                    }
                    else
                    {
                        result.Add(current);
                        current = word;
                    }
                }
                if (!string.IsNullOrEmpty(current))
                    result.Add(current);
            }
            return result;
        }

        public static float DrawTextWrapped(string text, float x, float y, float maxWidth, float fontSize, Color col, float lineSpacing = 4f)
        {
            var lines = WrapText(text, fontSize, maxWidth);
            float step = fontSize + lineSpacing;
            for (int i = 0; i < lines.Count; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    Vector2 pos = new(x, y + i * step);
                    Raylib.DrawTextEx(GameFont, lines[i], pos + new Vector2(1, 1), fontSize, 1f, Color.Black);
                    Raylib.DrawTextEx(GameFont, lines[i], pos, fontSize, 1f, col);
                }
            }
            return lines.Count * step;
        }

        public static bool DrawButton(Rectangle rect, string text, Color bgCol, Color hoverCol, Color textCol, float fontSize = 16f, Color? borderCol = null)
        {
            Vector2 mPos = Raylib.GetMousePosition();
            bool hovered = Raylib.CheckCollisionPointRec(mPos, rect);
            bool clicked = hovered && Raylib.IsMouseButtonPressed(MouseButton.Left);

            if (borderCol.HasValue)
            {
                Raylib.DrawRectangleRec(new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4), borderCol.Value);
            }

            Raylib.DrawRectangleRec(rect, hovered ? hoverCol : bgCol);
            if (!string.IsNullOrEmpty(text))
            {
                DrawTextCentered(text, rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f, fontSize, textCol);
            }
            if (clicked)
            {
                SoundManager.Play("ui_click");
            }
            return clicked;
        }

        public static float LerpAngleDeg(float current, float target, float t)
            => Utils.LerpAngleDeg(current, target, t);
    }

    public static class Utils
    {
        public static Color Rgb(int r, int g, int b, int a = 255)
        {
            return new Color(
                (byte)Math.Clamp(r, 0, 255),
                (byte)Math.Clamp(g, 0, 255),
                (byte)Math.Clamp(b, 0, 255),
                (byte)Math.Clamp(a, 0, 255)
            );
        }

        public static bool IsValid(Entity? e)
        {
            return e != null && !e.IsDestroyed && !e.IsDead && e.Enabled;
        }

        public static void ClearChildren(Entity? e)
        {
            if (e == null || e.IsDestroyed) return;
            foreach (var ch in e.Children.ToArray())
            {
                Destroy(ch);
            }
            e.Children.Clear();
        }

        public static void Destroy(Entity? e, float delay = 0f)
        {
            if (e == null || e.IsDestroyed) return;
            if (delay > 0f)
            {
                Engine.ActiveTweens.Add(Tween.CreateDestroyDelay(e, delay));
                return;
            }

            e.IsDestroyed = true;
            e.IsDead = true;
            foreach (var ch in e.Children.ToArray())
            {
                Destroy(ch, 0f);
            }
            e.Children.Clear();
            e.Parent?.Children.Remove(e);
            Engine.RootEntities.Remove(e);
            Engine.AllEntities.Remove(e);
        }

        public static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0f, 1f);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => Vector3.Lerp(a, b, Math.Clamp(t, 0f, 1f));

        public static float LerpAngleDeg(float a, float b, float t)
        {
            float delta = (b - a) % 360f;
            if (delta > 180f) delta -= 360f;
            else if (delta < -180f) delta += 360f;
            return a + delta * Math.Clamp(t, 0f, 1f);
        }

        public static Vector3 LerpEulerDeg(Vector3 a, Vector3 b, float t)
        {
            float ct = Math.Clamp(t, 0f, 1f);
            return new Vector3(
                LerpAngleDeg(a.X, b.X, ct),
                LerpAngleDeg(a.Y, b.Y, ct),
                LerpAngleDeg(a.Z, b.Z, ct)
            );
        }
    }

    public class Entity
    {
        private Entity? _parent;
        public Entity? Parent
        {
            get => _parent;
            set
            {
                _parent?.Children.Remove(this);
                _parent = value;
                if (_parent != null)
                {
                    _parent.Children.Add(this);
                    Engine.RootEntities.Remove(this);
                }
                else if (!Engine.RootEntities.Contains(this))
                {
                    Engine.RootEntities.Add(this);
                }
            }
        }

        public readonly List<Entity> Children = new();
        public string Model = "";
        public Vector3 Position = Vector3.Zero;
        public Vector3 Rotation = Vector3.Zero; // Углы Эйлера в градусах (X, Y, Z)
        public Vector3 Scale = Vector3.One;
        public Color Color = Color.White;
        public bool Unlit = false;
        public bool Enabled = true;
        public bool IsDestroyed = false;
        public bool IsDead = false;
        public bool IsUI = false;
        public bool IsPreviewIsolated = false;
        public bool AllowObstructionFade = false;
        public float ObstructionRadius = 4.2f;
        public float ObstructionHeight = 11.0f;
        public float ObstructionFadeAlpha = 1.0f;
        public string BillboardText = "";

        // Удобные свойства-обёртки как в Ursina
        public float X { get => Position.X; set => Position.X = value; }
        public float Y { get => Position.Y; set => Position.Y = value; }
        public float Z { get => Position.Z; set => Position.Z = value; }

        public float RotationX { get => Rotation.X; set => Rotation.X = value; }
        public float RotationY { get => Rotation.Y; set => Rotation.Y = value; }
        public float RotationZ { get => Rotation.Z; set => Rotation.Z = value; }

        public float ScaleX { get => Scale.X; set => Scale.X = value; }
        public float ScaleY { get => Scale.Y; set => Scale.Y = value; }
        public float ScaleZ { get => Scale.Z; set => Scale.Z = value; }

        public Vector3 Forward
        {
            get
            {
                float radY = Rotation.Y * (MathF.PI / 180f);
                return Vector3.Normalize(new Vector3(MathF.Sin(radY), 0f, MathF.Cos(radY)));
            }
        }

        public Vector3 Right
        {
            get
            {
                float radY = Rotation.Y * (MathF.PI / 180f);
                return Vector3.Normalize(new Vector3(MathF.Cos(radY), 0f, -MathF.Sin(radY)));
            }
        }

        public void LookAt2D(Vector3 targetPos)
        {
            float dx = targetPos.X - Position.X;
            float dz = targetPos.Z - Position.Z;
            if (MathF.Abs(dx) > 0.0001f || MathF.Abs(dz) > 0.0001f)
                Rotation.Y = MathF.Atan2(dx, dz) * (180f / MathF.PI);
        }

        public void LookAt(Vector3 targetPos)
        {
            Vector3 dir = targetPos - Position;
            float len = dir.Length();
            if (len < 0.0001f) return;
            dir /= len;
            Rotation.Y = MathF.Atan2(dir.X, dir.Z) * (180f / MathF.PI);
            Rotation.X = -MathF.Asin(Math.Clamp(dir.Y, -1f, 1f)) * (180f / MathF.PI);
        }

        public Entity(
            Entity? parent = null,
            string model = "",
            Vector3? position = null,
            Vector3? rotation = null,
            Vector3? scale = null,
            float? scaleUniform = null,
            Color? color = null,
            bool unlit = false,
            bool enabled = true)
        {
            Engine.AllEntities.Add(this);
            Parent = parent;
            Model = model;
            if (position.HasValue) Position = position.Value;
            if (rotation.HasValue) Rotation = rotation.Value;
            if (scale.HasValue) Scale = scale.Value;
            else if (scaleUniform.HasValue) Scale = new Vector3(scaleUniform.Value);
            if (color.HasValue) Color = color.Value;
            Unlit = unlit;
            Enabled = enabled;
        }

        public virtual void OnUpdate(float dt) { }

        public Matrix4x4 GetLocalMatrix()
        {
            float rx = Rotation.X * (MathF.PI / 180f);
            float ry = Rotation.Y * (MathF.PI / 180f);
            float rz = Rotation.Z * (MathF.PI / 180f);

            Matrix4x4 matScale = Matrix4x4.CreateScale(Scale);
            Matrix4x4 matRot = Matrix4x4.CreateFromYawPitchRoll(ry, rx, rz);
            Matrix4x4 matTrans = Matrix4x4.CreateTranslation(Position);
            return matScale * matRot * matTrans;
        }

        public Vector3 GetWorldPosition()
        {
            Matrix4x4 m = GetLocalMatrix();
            Entity? cur = Parent;
            while (cur != null)
            {
                m *= cur.GetLocalMatrix();
                cur = cur.Parent;
            }
            return m.Translation;
        }

        // Методы анимации как в Ursina
        public void AnimateScale(Vector3 targetScale, float duration)
            => Engine.ActiveTweens.Add(Tween.CreateScale(this, targetScale, duration));

        public void AnimateScale(float uniformScale, float duration)
            => AnimateScale(new Vector3(uniformScale), duration);

        public void AnimatePosition(Vector3 targetPos, float duration)
            => Engine.ActiveTweens.Add(Tween.CreatePosition(this, targetPos, duration));

        public void AnimateRotation(Vector3 targetRot, float duration)
            => Engine.ActiveTweens.Add(Tween.CreateRotation(this, targetRot, duration));

        public void AnimateColor(Color targetCol, float duration)
            => Engine.ActiveTweens.Add(Tween.CreateColor(this, targetCol, duration));
    }

    public class Tween
    {
        private readonly Entity _target;
        private readonly float _duration;
        private float _elapsed;
        private readonly int _type; // 0=Scale, 1=Pos, 2=Rot, 3=Color, 4=Destroy
        private Vector3 _startVec, _endVec;
        private Color _startCol, _endCol;

        private Tween(Entity target, float duration, int type)
        {
            _target = target;
            _duration = MathF.Max(0.001f, duration);
            _type = type;
        }

        public static Tween CreateScale(Entity e, Vector3 to, float dur)
            => new(e, dur, 0) { _startVec = e.Scale, _endVec = to };

        public static Tween CreatePosition(Entity e, Vector3 to, float dur)
            => new(e, dur, 1) { _startVec = e.Position, _endVec = to };

        public static Tween CreateRotation(Entity e, Vector3 to, float dur)
            => new(e, dur, 2) { _startVec = e.Rotation, _endVec = to };

        public static Tween CreateColor(Entity e, Color to, float dur)
            => new(e, dur, 3) { _startCol = e.Color, _endCol = to };

        public static Tween CreateDestroyDelay(Entity e, float delay)
            => new(e, delay, 4);

        public bool Update(float dt)
        {
            if (_target.IsDestroyed) return true;
            _elapsed += dt;
            float t = Math.Clamp(_elapsed / _duration, 0f, 1f);

            switch (_type)
            {
                case 0:
                    // out_expo кривая для сочных взрывов VisualFX
                    float expo = t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t);
                    _target.Scale = Vector3.Lerp(_startVec, _endVec, expo);
                    break;
                case 1:
                    _target.Position = Vector3.Lerp(_startVec, _endVec, t);
                    break;
                case 2:
                    _target.Rotation = Vector3.Lerp(_startVec, _endVec, t);
                    break;
                case 3:
                    _target.Color = new Color(
                        (byte)Utils.Lerp(_startCol.R, _endCol.R, t),
                        (byte)Utils.Lerp(_startCol.G, _endCol.G, t),
                        (byte)Utils.Lerp(_startCol.B, _endCol.B, t),
                        (byte)Utils.Lerp(_startCol.A, _endCol.A, t)
                    );
                    break;
                case 4:
                    if (t >= 1f) Utils.Destroy(_target, 0f);
                    break;
            }
            return t >= 1f;
        }
    }

    public class VisualFX : Entity
    {
        public VisualFX(Vector3 pos, Color fxColor, float maxScale = 3.0f, float duration = 0.25f)
            : base(model: "sphere", position: pos, scaleUniform: 0.2f, color: fxColor, unlit: true)
        {
            AnimateScale(maxScale, duration);
            AnimateColor(Utils.Rgb(0, 0, 0, 0), duration);
            Utils.Destroy(this, duration + 0.05f);
        }
    }
}