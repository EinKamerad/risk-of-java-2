using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;

namespace RiskOfCSharp2.Models
{
    public struct ObstacleCircle
    {
        public float X, Z, Radius, TopHeight, BaseY;
        public ObstacleCircle(float x, float z, float r, float h, float baseY = 0f)
        {
            X = x; Z = z; Radius = r; BaseY = baseY; TopHeight = baseY + h;
        }
    }

    public struct ObstacleBox
    {
        public float X, Z, HalfX, HalfZ, CosYaw, SinYaw, BaseY, TopHeight;
        public bool WalkableTop;

        public ObstacleBox(float x, float z, float halfX, float halfZ, float yawDeg, float height, float baseY = 0f, bool walkableTop = true)
        {
            X = x;
            Z = z;
            HalfX = halfX;
            HalfZ = halfZ;
            float rad = yawDeg * (MathF.PI / 180f);
            CosYaw = MathF.Cos(rad);
            SinYaw = MathF.Sin(rad);
            BaseY = baseY;
            TopHeight = baseY + height;
            WalkableTop = walkableTop;
        }
    }

    public class SkyboxController : Entity
    {
        private readonly Entity _moonPivot;
        private readonly Entity _debrisRing1;
        private readonly Entity _debrisRing2;
        private readonly Entity _planetPivot;
        private readonly Entity _auroraRing;

        public SkyboxController(Entity? parent, Entity moonPivot, Entity debrisRing1, Entity debrisRing2, Entity planetPivot, Entity auroraRing)
            : base(parent: parent)
        {
            _moonPivot = moonPivot;
            _debrisRing1 = debrisRing1;
            _debrisRing2 = debrisRing2;
            _planetPivot = planetPivot;
            _auroraRing = auroraRing;
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var g = RiskOfCSharp2.World.WorldContext.Game;
            if (g != null)
            {
                // Мягкий параллакс скайбокса относительно позиции игрока, чтобы края локации никогда не приближались к звёздам
                Position = new Vector3(g.PlayerPos.X * 0.45f, 0f, g.PlayerPos.Z * 0.45f);
            }

            _debrisRing1.RotationY += 4.5f * dt;
            _debrisRing2.RotationY -= 6.2f * dt;
            _moonPivot.RotationY += 1.2f * dt;
            _planetPivot.RotationY -= 1.8f * dt;
            _auroraRing.RotationY += 2.0f * dt;
        }
    }

    public static partial class WorldStructuresBuilder
    {
        public static float GetStructureFootprintRadius(string structType) => structType switch
        {
            "dark_fountain_spire" => 5.4f,
            "pipis_billboard" => 4.2f,
            "qliphoth_cage" => 5.0f,
            "originium_monolith" => 4.8f,
            "penacony_organ" => 6.0f,
            "aurelionite_geode" => 4.2f,
            "arch" => 6.4f,
            "monolith" => 3.6f,
            "crystals" => 3.2f,
            "platform" => 4.8f,
            "ruined_tower" => 4.2f,
            "torii" => 6.5f,
            "sakura_tree" => 4.8f,
            "giant_mushroom" => 6.2f,
            "ice_spire" => 5.6f,
            "sulfur_geyser" => 4.0f,
            "aqueduct_pillar" => 6.6f,
            "sun_temple" => 4.9f,
            "swamp_root" => 6.5f,
            "astral_ring" => 8.0f,
            _ => 4.5f
        };

        public static float GetStructureApproxHeight(string structType) => structType switch
        {
            "arch" => 14.2f,
            "monolith" => 17.5f,
            "crystals" => 5.8f,
            "platform" => 4.0f,
            "ruined_tower" => 17.0f,
            "torii" => 13.5f,
            "sakura_tree" => 13.0f,
            "giant_mushroom" => 13.0f,
            "ice_spire" => 14.0f,
            "sulfur_geyser" => 7.2f,
            "aqueduct_pillar" => 16.5f,
            "sun_temple" => 11.2f,
            "swamp_root" => 9.8f,
            "astral_ring" => 11.5f,
            _ => 10.0f
        };

        public static Entity BuildSkybox(Entity? parentEntity, StagePalette palette)
        {
            var rng = new Random(42);
            float NextRange(float min, float max) => min + (float)rng.NextDouble() * (max - min);

            var tempRoot = new Entity(parent: parentEntity);

            // 1. Далёкое кольцо горных хребтов на горизонте (создаёт глубину пейзажа без Z-fighting)
            int mountainCount = 28;
            for (int i = 0; i < mountainCount; i++)
            {
                float ang = (i / (float)mountainCount) * MathF.PI * 2f + NextRange(-0.04f, 0.04f);
                float dist = 285f + (i % 3) * 14f;
                float mh = NextRange(38f, 88f);
                float mw = NextRange(48f, 78f);
                Color mCol = Utils.Rgb(
                    (byte)Math.Clamp((palette.Sky.R * 2 + palette.Ruins.R) / 3 + (i % 2) * 6, 8, 240),
                    (byte)Math.Clamp((palette.Sky.G * 2 + palette.Ruins.G) / 3 + (i % 2) * 6, 8, 240),
                    (byte)Math.Clamp((palette.Sky.B * 2 + palette.Ruins.B) / 3 + (i % 2) * 8, 12, 245));
                new Entity(
                    parent: tempRoot, model: "diamond",
                    position: new(MathF.Cos(ang) * dist, mh * 0.22f - 18f, MathF.Sin(ang) * dist),
                    scale: new(mw, mh, mw * 0.65f),
                    rotation: new(0, -ang * (180f / MathF.PI) + 45f, 0),
                    color: mCol, unlit: true
                );
            }

            // 2. Многослойные космические туманности на большом радиусе (разнесённые по дистанции без пересечений)
            for (int i = 0; i < 18; i++)
            {
                float ang = (i / 18.0f) * MathF.PI * 2f;
                float dist = 265f + (i % 3) * 18f;
                float nebY = NextRange(28f, 92f);
                new Entity(
                    parent: tempRoot, model: "sphere",
                    position: new(MathF.Cos(ang) * dist, nebY, MathF.Sin(ang) * dist),
                    scale: new(NextRange(62f, 98f), NextRange(16f, 30f), NextRange(42f, 68f)),
                    rotation: new(NextRange(-8f, 8f), NextRange(0f, 360f), 0),
                    color: Utils.Rgb(palette.Nebula.R, palette.Nebula.G, palette.Nebula.B, 95), unlit: true
                );
            }

            // 3. Небесное полярное сияние / астральный пояс
            var auroraRing = new Entity(parent: tempRoot, position: new(0, 118f, 0));
            for (int i = 0; i < 12; i++)
            {
                float ang = (i / 12.0f) * MathF.PI * 2f;
                float r = 235f + (i % 2) * 12f;
                new Entity(
                    parent: auroraRing, model: "diamond",
                    position: new(MathF.Cos(ang) * r, MathF.Sin(i * 1.5f) * 14f, MathF.Sin(ang) * r),
                    scale: new(54f, 6.5f, 14f),
                    rotation: new(12f, -ang * (180f / MathF.PI), 8f),
                    color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 70), unlit: true
                );
            }

            // 4. Звёздный купол (220 звёзд разного размера и оттенка)
            for (int i = 0; i < 220; i++)
            {
                float theta = NextRange(0f, MathF.PI * 2f);
                float phi = NextRange(0.10f, MathF.PI * 0.49f);
                float r = NextRange(275f, 330f);
                float starSize = NextRange(1.0f, 2.8f);
                Color sCol = (i % 5 == 0) ? palette.Accent : ((i % 7 == 0) ? palette.Moon : Color.White);
                new Entity(
                    parent: tempRoot, model: "diamond",
                    position: new(r * MathF.Cos(phi) * MathF.Cos(theta), r * MathF.Sin(phi), r * MathF.Cos(phi) * MathF.Sin(theta)),
                    scaleUniform: starSize,
                    color: sCol, unlit: true
                );
            }

            // Яркие звёзды-маяки с аккуратными лучами (с разной толщиной по осям, чтобы не было Z-fighting)
            for (int i = 0; i < 12; i++)
            {
                float theta = NextRange(0f, MathF.PI * 2f);
                float phi = NextRange(0.22f, MathF.PI * 0.44f);
                float r = NextRange(280f, 315f);
                var starPos = new Vector3(r * MathF.Cos(phi) * MathF.Cos(theta), r * MathF.Sin(phi), r * MathF.Cos(phi) * MathF.Sin(theta));
                new Entity(parent: tempRoot, model: "sphere", scaleUniform: NextRange(2.8f, 4.4f), position: starPos, color: Color.White, unlit: true);
                new Entity(parent: tempRoot, model: "diamond", scale: new(NextRange(6f, 10f), 0.48f, 0.48f), position: starPos, color: Utils.Rgb(255, 255, 255, 175), unlit: true);
                new Entity(parent: tempRoot, model: "diamond", scale: new(0.42f, NextRange(6f, 10f), 0.42f), position: starPos, color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 155), unlit: true);
            }

            // 5. Расколотая Луна Petrichor V (радиус сферы = 18 при scaleUniform = 36, все кратеры и разлом строго на сфере!)
            var moonPivot = new Entity(parent: tempRoot, position: new(115, 128, 210), rotation: new(-14, -28, 16));
            // Мягкое внешнее гало луны
            new Entity(parent: moonPivot, model: "sphere", scaleUniform: 40.5f,
                color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 42), unlit: true);
            // Основное сферическое тело луны (диаметр 36 -> радиус 18)
            new Entity(parent: moonPivot, model: "sphere", scaleUniform: 36f, color: palette.Moon, unlit: true);

            // Кратеры на поверхности сферы (расстояние от центра ~ 16.5f, поэтому они аккуратно выступают на поверхности!)
            Color craterCol = Utils.Rgb(
                (byte)Math.Max(palette.Moon.R - 32, 0),
                (byte)Math.Max(palette.Moon.G - 32, 0),
                (byte)Math.Max(palette.Moon.B - 24, 0));
            foreach (var (dx, dy, dz, cs) in new[]
            {
                (0.35f, 0.42f, -0.84f, 6.2f),
                (-0.48f, 0.18f, -0.86f, 4.8f),
                (0.12f, -0.38f, -0.92f, 4.2f),
                (0.56f, -0.15f, -0.81f, 5.0f),
                (-0.24f, 0.58f, -0.78f, 3.8f)
            })
            {
                Vector3 dir = Vector3.Normalize(new Vector3(dx, dy, dz));
                new Entity(parent: moonPivot, model: "sphere", scaleUniform: cs, position: dir * 16.2f, color: craterCol, unlit: true);
            }

            // Светящийся экваториальный разлом Луны (сферический срез вместо торчащего прямоугольника!)
            new Entity(parent: moonPivot, model: "sphere", scale: new(36.8f, 2.2f, 36.8f), rotation: new(12f, 0f, 18f),
                color: palette.Accent, unlit: true);
            new Entity(parent: moonPivot, model: "sphere", scale: new(37.2f, 0.9f, 37.2f), rotation: new(12f, 0f, 18f),
                color: Color.White, unlit: true);

            // Вращающиеся пояса астероидов и колец вокруг расколотой Луны
            var debrisRing1 = new Entity(parent: moonPivot, rotation: new(10f, 0f, 14f));
            var debrisRing2 = new Entity(parent: moonPivot, rotation: new(-12f, 25f, -10f));
            new Entity(parent: debrisRing1, model: "diamond", scale: new(68f, 0.9f, 68f),
                color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 145), unlit: true);
            new Entity(parent: debrisRing2, model: "diamond", scale: new(54f, 0.5f, 54f),
                color: Utils.Rgb(palette.Moon.R, palette.Moon.G, palette.Moon.B, 120), unlit: true);

            for (int i = 0; i < 18; i++)
            {
                float ang = (i / 18.0f) * MathF.PI * 2f;
                float dist = NextRange(25f, 36f);
                float aSize = NextRange(1.8f, 4.2f);
                new Entity(
                    parent: i % 2 == 0 ? debrisRing1 : debrisRing2, model: "diamond",
                    position: new(MathF.Cos(ang) * dist, NextRange(-2.5f, 2.5f), MathF.Sin(ang) * dist),
                    scale: new(aSize, aSize * 0.75f, aSize * 1.15f),
                    rotation: new(NextRange(0f, 180f), NextRange(0f, 180f), 0f),
                    color: i % 3 == 0 ? palette.Accent : palette.Moon, unlit: true
                );
            }

            // 6. Далёкий окольцованный Газовый Гигант (со сферическими полосами атмосферы без торчащих углов!)
            var planetPivot = new Entity(parent: tempRoot, position: new(-155, 102, -185), rotation: new(22, 18, -18));
            float planetDiam = 32f;
            float planetRad = planetDiam * 0.5f;
            new Entity(parent: planetPivot, model: "sphere", scaleUniform: planetDiam, color: palette.Nebula, unlit: true);
            // Сферические атмосферные пояса, точно вписанные в профиль шара
            foreach (float by in new[] { -9f, -4.5f, 0f, 4.5f, 9f })
            {
                float ratio = by / planetRad;
                float sliceDiam = MathF.Sqrt(MathF.Max(0.1f, 1f - ratio * ratio)) * planetDiam * 1.02f;
                new Entity(parent: planetPivot, model: "sphere",
                    scale: new(sliceDiam, 2.1f, sliceDiam),
                    position: new(0, by, 0),
                    color: Utils.Rgb(
                        (byte)Math.Min(palette.Nebula.R + 38, 255),
                        (byte)Math.Min(palette.Nebula.G + 28, 255),
                        (byte)Math.Min(palette.Nebula.B + 22, 255), 115), unlit: true);
            }
            // Кольца газового гиганта (с разной толщиной по Y, чтобы не накладывались)
            new Entity(parent: planetPivot, model: "diamond", scale: new(68f, 0.75f, 68f),
                color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 165), unlit: true);
            new Entity(parent: planetPivot, model: "diamond", scale: new(56f, 0.45f, 56f), rotation: new(0, 30f, 0),
                color: Utils.Rgb(palette.Moon.R, palette.Moon.G, palette.Moon.B, 130), unlit: true);
            // Два спутника газового гиганта
            new Entity(parent: planetPivot, model: "sphere", scaleUniform: 4.4f, position: new(42f, 4f, 18f), color: palette.Moon, unlit: true);
            new Entity(parent: planetPivot, model: "sphere", scaleUniform: 2.8f, position: new(-36f, -3f, -24f), color: palette.Accent, unlit: true);

            var controller = new SkyboxController(parentEntity, moonPivot, debrisRing1, debrisRing2, planetPivot, auroraRing);
            tempRoot.Parent = controller;
            return controller;
        }

        public static (Entity Root, List<ObstacleCircle> Obstacles) BuildMapStructure(
            Entity? parentEntity, string structType, StagePalette palette, Vector3 pos, float rotY,
            Random? rng = null, List<ObstacleBox>? outBoxes = null)
        {
            rng ??= new Random();
            float NextRange(float min, float max) => min + (float)rng.NextDouble() * (max - min);

            var root = new Entity(parent: parentEntity, position: pos, rotation: new(0, rotY, 0));
            var obstacles = new List<ObstacleCircle>();
            float radY = rotY * (MathF.PI / 180f);
            float cosY = MathF.Cos(radY), sinY = MathF.Sin(radY);

            (float wx, float wz) ToWorld(float lx, float lz)
                => (pos.X + lx * cosY + lz * sinY, pos.Z - lx * sinY + lz * cosY);

            Color darkRuins = Utils.Rgb(
                (byte)Math.Max(palette.Ruins.R - 18, 0),
                (byte)Math.Max(palette.Ruins.G - 18, 0),
                (byte)Math.Max(palette.Ruins.B - 15, 0));
            Color lightRuins = Utils.Rgb(
                (byte)Math.Min(palette.Ruins.R + 18, 255),
                (byte)Math.Min(palette.Ruins.G + 18, 255),
                (byte)Math.Min(palette.Ruins.B + 16, 255));

            if (structType == "arch")
            {
                float h = NextRange(8.0f, 12.5f);
                float w = NextRange(7.0f, 10.0f);
                root.AllowObstructionFade = true;
                root.ObstructionRadius = Math.Clamp(w * 0.5f + 1.2f, 4.6f, 6.0f);
                root.ObstructionHeight = h + 2.6f;

                foreach (float sx in new[] { -w * 0.5f, w * 0.5f })
                {
                    // Двухступенчатый цоколь колонны
                    new Entity(parent: root, model: "cube", scale: new(2.75f, 0.42f, 2.75f), position: new(sx, 0.21f, 0), color: darkRuins);
                    new Entity(parent: root, model: "cube", scale: new(2.32f, 0.40f, 2.32f), position: new(sx, 0.62f, 0), color: lightRuins);
                    // Ствол колонны
                    new Entity(parent: root, model: "cube", scale: new(1.80f, h, 1.80f), position: new(sx, h * 0.5f, 0), color: palette.Ruins);
                    // Наружные каннелюры (выступают на 0.08f без совпадения плоскостей)
                    foreach (float fOff in new[] { -0.55f, 0.0f, 0.55f })
                    {
                        new Entity(parent: root, model: "cube", scale: new(0.14f, h * 0.78f, 1.88f),
                            position: new(sx + fOff, h * 0.50f, 0), color: darkRuins);
                    }
                    // Резная капитель с волютами и абакой
                    new Entity(parent: root, model: "cube", scale: new(2.26f, 0.42f, 2.26f), position: new(sx, h - 0.52f, 0), color: darkRuins);
                    new Entity(parent: root, model: "cube", scale: new(2.58f, 0.46f, 2.58f), position: new(sx, h - 0.08f, 0), color: lightRuins);
                    foreach (float vx in new[] { -1.16f, 1.16f })
                        new Entity(parent: root, model: "sphere", scale: new(0.46f, 0.46f, 2.66f), position: new(sx + vx, h - 0.28f, 0), color: palette.Ruins);
                    // Светящиеся рунные инкрустации на колоннах
                    new Entity(parent: root, model: "cube", scale: new(0.22f, 1.6f, 1.94f), position: new(sx, h * 0.42f, 0), color: palette.Accent, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(0.58f, 0.58f, 2.02f), position: new(sx, h * 0.70f, 0), color: palette.Accent, unlit: true);
                }

                // Архитрав, фриз с метопами и ступенчатый карниз
                new Entity(parent: root, model: "cube", scale: new(w + 2.8f, 1.35f, 2.20f), position: new(0, h + 0.82f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(w + 2.4f, 0.24f, 2.34f), position: new(0, h + 0.82f, 0), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(w + 3.3f, 0.38f, 2.64f), position: new(0, h + 1.69f, 0), color: lightRuins);
                new Entity(parent: root, model: "diamond", scale: new(3.6f, 0.95f, 2.36f), position: new(0, h + 2.18f, 0), color: darkRuins);

                // Замковый камень и парящий рунический кристалл с полупрозрачным ореолом
                new Entity(parent: root, model: "cube", scale: new(1.45f, 1.85f, 2.46f), position: new(0, h + 0.65f, 0), color: darkRuins);
                new Entity(parent: root, model: "diamond", scale: new(1.25f, 2.35f, 1.25f), position: new(0, h - 1.35f, 0), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.62f, position: new(0, h - 1.35f, 0), color: Color.White, unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(1.85f, 3.05f, 1.85f), position: new(0, h - 1.35f, 0),
                    color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 78), unlit: true);

                var (wx1, wz1) = ToWorld(-w * 0.5f, 0);
                var (wx2, wz2) = ToWorld(w * 0.5f, 0);
                obstacles.Add(new ObstacleCircle(wx1, wz1, 1.25f, h, pos.Y));
                obstacles.Add(new ObstacleCircle(wx2, wz2, 1.25f, h, pos.Y));
                outBoxes?.Add(new ObstacleBox(wx1, wz1, 1.20f, 1.20f, rotY, h, pos.Y, walkableTop: false));
                outBoxes?.Add(new ObstacleBox(wx2, wz2, 1.20f, 1.20f, rotY, h, pos.Y, walkableTop: false));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, (w + 3.0f) * 0.5f, 1.25f, rotY, 1.85f, pos.Y + h, walkableTop: true));
            }
            else if (structType == "monolith")
            {
                float h = NextRange(8.0f, 15.0f);
                root.AllowObstructionFade = true;
                root.ObstructionRadius = 3.6f;
                root.ObstructionHeight = h + 4.8f;

                new Entity(parent: root, model: "cube", scale: new(4.4f, 0.6f, 4.4f), position: new(0, 0.3f, 0), color: darkRuins);
                new Entity(parent: root, model: "cube", scale: new(3.7f, 0.6f, 3.7f), position: new(0, 0.9f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(3.1f, 0.5f, 3.1f), position: new(0, 1.45f, 0), color: lightRuins);
                new Entity(parent: root, model: "cube", scale: new(2.5f, h, 2.5f), position: new(0, h * 0.5f + 1.7f, 0), color: palette.Ruins);

                // Рельефные угловые рёбра
                foreach (float fx in new[] { -1.24f, 1.24f })
                    foreach (float fz in new[] { -1.24f, 1.24f })
                        new Entity(parent: root, model: "cube", scale: new(0.24f, h * 0.92f, 0.24f),
                            position: new(fx, h * 0.5f + 1.7f, fz), color: darkRuins);

                // Вертикальная и горизонтальные руны с разной глубиной, чтобы не было Z-fighting
                new Entity(parent: root, model: "cube", scale: new(0.40f, h * 0.72f, 2.58f), position: new(0, h * 0.5f + 1.7f, 0), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(2.62f, h * 0.72f + 0.08f, 0.40f), position: new(0, h * 0.5f + 1.7f, 0), color: palette.Accent, unlit: true);
                foreach (float ry in new[] { h * 0.30f + 1.7f, h * 0.64f + 1.7f })
                    new Entity(parent: root, model: "cube", scale: new(2.68f, 0.16f, 2.68f), position: new(0, ry, 0), color: palette.Accent, unlit: true);

                new Entity(parent: root, model: "diamond", scale: new(2.7f, 2.8f, 2.7f), position: new(0, h + 2.1f, 0), color: lightRuins);
                new Entity(parent: root, model: "diamond", scale: new(1.2f, 2.1f, 1.2f), position: new(0, h + 4.2f, 0), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(2.2f, 0.22f, 2.2f), position: new(0, h + 4.2f, 0),
                    rotation: new(14f, 35f, -12f), color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 145), unlit: true);
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.0f, h + 2.0f, pos.Y));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.22f, 2.22f, rotY, 1.70f, pos.Y, walkableTop: true));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 1.36f, 1.36f, rotY, h + 2.5f, pos.Y, walkableTop: false));
            }
            else if (structType == "crystals")
            {
                root.AllowObstructionFade = true;
                root.ObstructionRadius = 3.5f;
                root.ObstructionHeight = 7.2f;

                new Entity(parent: root, model: "sphere", scale: new(4.0f, 2.2f, 3.8f), position: new(0, 0.65f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "sphere", scale: new(2.8f, 1.4f, 2.6f), position: new(0.4f, 0.85f, -0.3f), color: darkRuins);
                for (int i = 0; i < 4; i++)
                {
                    float ang = NextRange(-26f, 26f);
                    float ch = NextRange(3.2f, 5.8f);
                    float cx = NextRange(-1.1f, 1.1f);
                    float cz = NextRange(-1.1f, 1.1f);
                    new Entity(parent: root, model: "diamond", scale: new(1.08f, ch, 1.08f),
                        position: new(cx, ch * 0.44f, cz),
                        rotation: new(ang, NextRange(0f, 360f), -ang * 0.7f), color: palette.Accent, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(0.55f, ch * 0.65f, 0.55f),
                        position: new(cx + 0.35f, ch * 0.30f, cz - 0.25f),
                        rotation: new(-ang * 1.1f, NextRange(0f, 360f), ang), color: Color.White, unlit: true);
                }
                new Entity(parent: root, model: "sphere", scale: new(3.6f, 2.6f, 3.6f), position: new(0, 2.1f, 0),
                    color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 58), unlit: true);
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.15f, 5.2f, pos.Y));
            }
            else if (structType == "platform")
            {
                // Плоская прогулочная платформа — не затеняет обзор камеры (AllowObstructionFade остаётся false)
                new Entity(parent: root, model: "cube", scale: new(6.4f, 0.35f, 6.4f), position: new(0, 0.175f, 0), color: darkRuins);
                new Entity(parent: root, model: "cube", scale: new(5.8f, 0.45f, 5.8f), position: new(0, 0.575f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(5.2f, 1.0f, 5.2f), position: new(0, 1.30f, 0), color: lightRuins);
                new Entity(parent: root, model: "cube", scale: new(5.32f, 0.18f, 5.32f), position: new(0, 1.62f, 0), color: palette.Accent, unlit: true);

                // Рунные линии разнесены по высоте (1.82f vs 1.85f), чтобы на перекрёстках не было Z-fighting!
                foreach (float lx in new[] { -1.5f, 0f, 1.5f })
                    new Entity(parent: root, model: "cube", scale: new(0.10f, 0.04f, 4.8f), position: new(lx, 1.82f, 0), color: palette.Accent, unlit: true);
                foreach (float lz in new[] { -1.5f, 0f, 1.5f })
                    new Entity(parent: root, model: "cube", scale: new(4.8f, 0.05f, 0.10f), position: new(0, 1.85f, lz), color: palette.Accent, unlit: true);

                foreach (var (px, pz) in new[] { (-2.3f, -2.3f), (2.3f, -2.3f), (-2.3f, 2.3f), (2.3f, 2.3f) })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.62f, 1.4f, 0.62f), position: new(px, 2.5f, pz), color: palette.Ruins);
                    new Entity(parent: root, model: "diamond", scale: new(0.55f, 0.65f, 0.55f), position: new(px, 3.45f, pz), color: palette.Accent, unlit: true);
                    var (pwx, pwz) = ToWorld(px, pz);
                    obstacles.Add(new ObstacleCircle(pwx, pwz, 0.58f, 3.8f, pos.Y));
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.95f, 1.82f, pos.Y));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 3.22f, 3.22f, rotY, 0.80f, pos.Y, walkableTop: true));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.65f, 2.65f, rotY, 1.84f, pos.Y, walkableTop: true));
            }
            else if (structType == "ruined_tower")
            {
                // Древняя сторожевая башня с контрфорсами, обрамлёнными бойницами, зубцами и сигнальной жаровней
                float h = NextRange(10.5f, 15.5f);
                root.AllowObstructionFade = true;
                root.ObstructionRadius = 4.4f;
                root.ObstructionHeight = h + 3.8f;

                // Двухъярусный каменный фундамент
                new Entity(parent: root, model: "cube", scale: new(5.4f, 0.65f, 5.4f), position: new(0, 0.325f, 0), color: darkRuins);
                new Entity(parent: root, model: "cube", scale: new(4.75f, 0.55f, 4.75f), position: new(0, 0.925f, 0), color: lightRuins);
                // Основная шахта башни
                new Entity(parent: root, model: "cube", scale: new(4.15f, h, 4.15f), position: new(0, h * 0.5f + 1.15f, 0), color: palette.Ruins);

                // Угловые пилястры-контрфорсы (выступают за 4.15f без совпадения граней)
                foreach (float cx in new[] { -2.02f, 2.02f })
                    foreach (float cz in new[] { -2.02f, 2.02f })
                        new Entity(parent: root, model: "cube", scale: new(0.52f, h * 0.94f, 0.52f),
                            position: new(cx, h * 0.48f + 1.15f, cz), color: darkRuins);

                // Каменные пояса и светящиеся бойницы с подоконниками
                foreach (float wy in new[] { h * 0.38f + 1.0f, h * 0.70f + 1.0f })
                {
                    new Entity(parent: root, model: "cube", scale: new(4.32f, 0.24f, 4.32f), position: new(0, wy - 0.92f, 0), color: lightRuins);
                    new Entity(parent: root, model: "cube", scale: new(0.62f, 1.50f, 4.26f), position: new(0, wy, 0), color: palette.Accent, unlit: true);
                    new Entity(parent: root, model: "cube", scale: new(4.30f, 1.56f, 0.62f), position: new(0, wy, 0), color: palette.Accent, unlit: true);
                }

                // Машикули и верхняя площадка парапета
                new Entity(parent: root, model: "cube", scale: new(4.56f, 0.38f, 4.56f), position: new(0, h + 0.96f, 0), color: darkRuins);
                new Entity(parent: root, model: "cube", scale: new(4.92f, 0.55f, 4.92f), position: new(0, h + 1.42f, 0), color: lightRuins);

                // Угловые и центральные зубцы парапета
                foreach (var (bx, bz) in new[] { (-2.05f, -2.05f), (2.05f, -2.05f), (-2.05f, 2.05f), (2.05f, 2.05f) })
                    new Entity(parent: root, model: "cube", scale: new(0.84f, 0.96f, 0.84f), position: new(bx, h + 2.18f, bz), color: darkRuins);
                foreach (var (mx, mz, sx, sz) in new[] { (0f, -2.12f, 0.95f, 0.56f), (0f, 2.12f, 0.95f, 0.56f), (-2.12f, 0f, 0.56f, 0.95f), (2.12f, 0f, 0.56f, 0.95f) })
                    new Entity(parent: root, model: "cube", scale: new(sx, 0.68f, sz), position: new(mx, h + 2.04f, mz), color: palette.Ruins);

                // Чаша сигнальной жаровни и пламя маяка наверху
                new Entity(parent: root, model: "diamond", scale: new(1.95f, 0.85f, 1.95f), position: new(0, h + 1.95f, 0), color: darkRuins);
                new Entity(parent: root, model: "diamond", scale: new(1.55f, 2.55f, 1.55f), position: new(0, h + 3.05f, 0), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.88f, position: new(0, h + 2.75f, 0), color: Color.White, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 2.65f, position: new(0, h + 3.05f, 0),
                    color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 68), unlit: true);

                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.45f, h + 1.6f, pos.Y));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.62f, 2.62f, rotY, 0.82f, pos.Y, walkableTop: true));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.46f, 2.46f, rotY, h + 1.70f, pos.Y, walkableTop: true));
            }
            else if (structType == "torii")
            {
                // Священные алые врата Тории с каменными базами, изогнутой крышей касаги, табличкой и духовными фонарями
                float h = NextRange(9.0f, 12.5f);
                float w = NextRange(7.5f, 10.0f);
                root.AllowObstructionFade = true;
                root.ObstructionRadius = Math.Clamp(w * 0.5f + 1.2f, 4.8f, 6.0f);
                root.ObstructionHeight = h + 2.5f;

                Color crimson = Utils.Rgb(192, 38, 52);
                Color darkWood = Utils.Rgb(38, 28, 34);
                foreach (float sx in new[] { -w * 0.5f, w * 0.5f })
                {
                    // Каменная база (камэбара) и чёрная оковка основания столба (нэмаки)
                    new Entity(parent: root, model: "cube", scale: new(1.92f, 0.55f, 1.92f), position: new(sx, 0.275f, 0), color: darkRuins);
                    new Entity(parent: root, model: "cube", scale: new(1.52f, 1.45f, 1.52f), position: new(sx, 1.0f, 0), color: darkWood);
                    // Алый столб (хасира) и золотые обручи
                    new Entity(parent: root, model: "cube", scale: new(1.24f, h, 1.24f), position: new(sx, h * 0.5f, 0), color: crimson);
                    new Entity(parent: root, model: "cube", scale: new(1.36f, 0.28f, 1.36f), position: new(sx, h * 0.62f, 0), color: Color.Gold, unlit: true);

                    // Подвесной духовный фонарь на цепи
                    float lx = sx * 0.70f;
                    new Entity(parent: root, model: "cube", scale: new(0.08f, 0.72f, 0.08f), position: new(lx, h - 1.72f, 0), color: darkWood);
                    new Entity(parent: root, model: "cube", scale: new(0.82f, 0.16f, 0.82f), position: new(lx, h - 2.12f, 0), color: darkWood);
                    new Entity(parent: root, model: "diamond", scale: new(0.76f, 1.15f, 0.76f), position: new(lx, h - 2.62f, 0), color: palette.Accent, unlit: true);
                    new Entity(parent: root, model: "cube", scale: new(0.62f, 0.14f, 0.62f), position: new(lx, h - 3.16f, 0), color: darkWood);
                    new Entity(parent: root, model: "sphere", scaleUniform: 1.35f, position: new(lx, h - 2.62f, 0),
                        color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 72), unlit: true);
                }

                // Нижняя перекладина (нуки) с декоративными клиньями (кусаби)
                new Entity(parent: root, model: "cube", scale: new(w + 2.8f, 0.68f, 0.96f), position: new(0, h - 1.45f, 0), color: crimson);
                foreach (float sx in new[] { -w * 0.5f, w * 0.5f })
                    new Entity(parent: root, model: "cube", scale: new(1.48f, 0.82f, 1.06f), position: new(sx, h - 1.45f, 0), color: darkWood);

                // Верхняя перекладина (симаги) и изогнутая крыша (касаги) с приподнятыми краями
                new Entity(parent: root, model: "cube", scale: new(w + 3.8f, 0.68f, 1.48f), position: new(0, h + 0.24f, 0), color: crimson);
                new Entity(parent: root, model: "cube", scale: new(w + 4.5f, 0.44f, 1.88f), position: new(0, h + 0.80f, 0), color: darkWood);
                foreach (float side in new[] { -1f, 1f })
                {
                    new Entity(parent: root, model: "cube", scale: new(1.85f, 0.42f, 1.96f),
                        position: new(side * (w * 0.5f + 2.55f), h + 1.08f, 0),
                        rotation: new(0, 0, side * 14f), color: darkWood);
                    new Entity(parent: root, model: "cube", scale: new(1.65f, 0.34f, 1.56f),
                        position: new(side * (w * 0.5f + 2.35f), h + 0.68f, 0),
                        rotation: new(0, 0, side * 14f), color: crimson);
                }

                // Центральная стойка (гакудзука) и золотая табличка с рамой
                new Entity(parent: root, model: "cube", scale: new(1.28f, 1.85f, 1.12f), position: new(0, h - 0.55f, 0), color: darkWood);
                new Entity(parent: root, model: "cube", scale: new(0.96f, 1.48f, 1.22f), position: new(0, h - 0.55f, 0), color: Color.Gold, unlit: true);

                var (wx1, wz1) = ToWorld(-w * 0.5f, 0);
                var (wx2, wz2) = ToWorld(w * 0.5f, 0);
                obstacles.Add(new ObstacleCircle(wx1, wz1, 1.05f, h, pos.Y));
                obstacles.Add(new ObstacleCircle(wx2, wz2, 1.05f, h, pos.Y));
                outBoxes?.Add(new ObstacleBox(wx1, wz1, 0.88f, 0.88f, rotY, h, pos.Y, walkableTop: false));
                outBoxes?.Add(new ObstacleBox(wx2, wz2, 0.88f, 0.88f, rotY, h, pos.Y, walkableTop: false));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, (w + 4.6f) * 0.5f, 0.95f, rotY, 2.65f, pos.Y + h - 1.5f, walkableTop: true));
            }
            else if (structType == "sakura_tree")
            {
                // Древнее цветущее дерево сакуры с узловатыми корнями, ветвями, многослойной кроной и лепестками
                float trunkH = NextRange(6.5f, 9.5f);
                root.AllowObstructionFade = true;
                root.ObstructionRadius = 5.4f;
                root.ObstructionHeight = trunkH + 4.4f;

                Color barkCol = Utils.Rgb(68, 44, 52);
                Color darkBark = Utils.Rgb(48, 30, 38);
                Color lightBlossom = Utils.Rgb(
                    (byte)Math.Min(palette.Accent.R + 45, 255),
                    (byte)Math.Min(palette.Accent.G + 45, 255),
                    (byte)Math.Min(palette.Accent.B + 45, 255));

                // Раскидистые узловатые корни у основания
                new Entity(parent: root, model: "cube", scale: new(2.35f, 1.15f, 2.35f), position: new(0, 0.55f, 0), color: darkBark);
                for (int i = 0; i < 4; i++)
                {
                    float rAng = (i * 90f + 35f) * MathF.PI / 180f;
                    new Entity(parent: root, model: "diamond", scale: new(0.95f, 0.65f, 2.45f),
                        position: new(MathF.Sin(rAng) * 1.35f, 0.32f, MathF.Cos(rAng) * 1.35f),
                        rotation: new(12f, i * 90f + 35f, 0), color: darkBark);
                }

                // Ствол и расходящиеся несущие ветви
                new Entity(parent: root, model: "cube", scale: new(1.58f, trunkH, 1.58f), position: new(0, trunkH * 0.5f, 0), rotation: new(4f, 0, -5f), color: barkCol);
                foreach (var (brX, brZ, rotX, rotZ) in new[] { (-1.35f, 0.65f, 18f, 38f), (1.30f, -0.55f, -16f, -36f), (0.25f, 1.15f, 34f, -10f) })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.68f, 3.2f, 0.68f),
                        position: new(brX, trunkH - 0.5f, brZ), rotation: new(rotX, 0, rotZ), color: barkCol);
                }

                // Пышная многослойная крона с подсветкой верхних соцветий
                foreach (var (bx, by, bz, bw, bh) in new[]
                {
                    (0f, trunkH + 1.25f, 0f, 8.2f, 3.5f),
                    (-2.85f, trunkH - 0.15f, 1.4f, 5.6f, 2.5f),
                    (2.65f, trunkH + 0.45f, -1.2f, 5.8f, 2.7f),
                    (0.55f, trunkH + 2.75f, 0.75f, 5.1f, 2.3f),
                    (-0.95f, trunkH + 2.25f, -1.35f, 4.6f, 2.1f)
                })
                {
                    new Entity(parent: root, model: "sphere", scale: new(bw, bh, bw), position: new(bx, by, bz), color: palette.Accent, unlit: true);
                    new Entity(parent: root, model: "sphere", scale: new(bw * 0.62f, bh * 0.65f, bw * 0.62f),
                        position: new(bx, by + bh * 0.28f, bz), color: lightBlossom, unlit: true);
                }
                // Мягкое полупрозрачное облако лепестков вокруг кроны
                new Entity(parent: root, model: "sphere", scale: new(9.2f, 4.2f, 9.2f), position: new(0, trunkH + 1.2f, 0),
                    color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 55), unlit: true);

                // Опадающие светящиеся лепестки в воздухе под ветвями
                foreach (var (px, py, pz) in new[] { (-2.4f, trunkH * 0.55f, 1.6f), (2.2f, trunkH * 0.42f, -1.4f), (0.9f, trunkH * 0.68f, 2.3f), (-1.5f, trunkH * 0.32f, -1.9f) })
                    new Entity(parent: root, model: "diamond", scale: new(0.32f, 0.12f, 0.22f), position: new(px, py, pz), rotation: new(25f, 40f, 18f), color: lightBlossom, unlit: true);

                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.45f, trunkH + 3.2f, pos.Y));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 1.15f, 1.15f, rotY, 1.25f, pos.Y, walkableTop: true));
            }
            else if (structType == "giant_mushroom")
            {
                // Исполинский биолюминесцентный гриб с пластинками гименофора, дочерними грибами и спорами
                float stemH = NextRange(6.5f, 11.0f);
                float capR = NextRange(4.2f, 6.8f);
                root.AllowObstructionFade = true;
                root.ObstructionRadius = Math.Clamp(capR + 0.35f, 4.5f, 6.0f);
                root.ObstructionHeight = stemH + 2.8f;

                // Утолщённое основание ножки и семейка малых грибов у корней
                new Entity(parent: root, model: "sphere", scale: new(3.1f, 1.8f, 3.1f), position: new(0, 0.65f, 0), color: darkRuins);
                new Entity(parent: root, model: "cube", scale: new(1.82f, stemH, 1.82f), position: new(0, stemH * 0.5f, 0), color: lightRuins);
                new Entity(parent: root, model: "diamond", scale: new(3.1f, 0.65f, 3.1f), position: new(0, stemH * 0.68f, 0), color: lightRuins);

                foreach (var (mx, mz, mh) in new[] { (1.45f, 0.85f, 1.45f), (-1.35f, -0.95f, 1.15f) })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.36f, mh, 0.36f), position: new(mx, mh * 0.5f, mz), color: lightRuins);
                    new Entity(parent: root, model: "sphere", scale: new(1.25f, 0.58f, 1.25f), position: new(mx, mh + 0.1f, mz), color: palette.Accent, unlit: true);
                }

                // Нижние спороносные пластинки и купол шляпки
                new Entity(parent: root, model: "diamond", scale: new(capR * 1.65f, 0.95f, capR * 1.65f), position: new(0, stemH - 0.35f, 0), color: darkRuins);
                for (int i = 0; i < 4; i++)
                {
                    new Entity(parent: root, model: "cube", scale: new(capR * 1.55f, 0.14f, 0.16f),
                        position: new(0, stemH - 0.25f - i * 0.03f, 0), rotation: new(0, i * 45f, 0), color: lightRuins);
                }
                new Entity(parent: root, model: "sphere", scale: new(capR * 2f, 2.85f, capR * 2f), position: new(0, stemH + 0.45f, 0), color: palette.Accent, unlit: true);

                // Светящиеся биолюминесцентные пятна на шляпке и парящее облако спор
                for (int i = 0; i < 6; i++)
                {
                    float r = (i * 60f) * MathF.PI / 180f;
                    float dist = capR * (i % 2 == 0 ? 0.56f : 0.36f);
                    float spotY = stemH + (i % 2 == 0 ? 1.52f : 1.76f);
                    new Entity(parent: root, model: "sphere", scale: new(1.32f, 0.52f, 1.32f),
                        position: new(MathF.Cos(r) * dist, spotY, MathF.Sin(r) * dist), color: Color.White, unlit: true);
                }
                new Entity(parent: root, model: "sphere", scale: new(capR * 1.85f, 1.4f, capR * 1.85f), position: new(0, stemH - 0.65f, 0),
                    color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 62), unlit: true);

                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.35f, stemH + 1.5f, pos.Y));
                // По шляпке гриба можно прыгать как по высокой платформе!
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, capR * 0.92f, 1.6f, pos.Y + stemH));
            }
            else if (structType == "ice_spire")
            {
                // Полярный ледяной шпиль + арктический контейнер (Точка Сбора Дельта)
                float h = NextRange(8.5f, 14.0f);
                root.AllowObstructionFade = true;
                root.ObstructionRadius = 4.6f;
                root.ObstructionHeight = h + 2.0f;

                new Entity(parent: root, model: "diamond", scale: new(1.9f, h * 0.68f, 1.9f), position: new(1.4f, h * 0.30f, 0.8f),
                    rotation: new(-12f, 60f, 14f), color: Color.White, unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(1.5f, h * 0.52f, 1.5f), position: new(-0.5f, h * 0.24f, 1.5f),
                    rotation: new(16f, -35f, 10f), color: Utils.Rgb(210, 242, 255), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(3.2f, h, 3.2f), position: new(0, h * 0.45f, 0),
                    rotation: new(6f, 25f, -8f), color: Utils.Rgb(155, 222, 255, 205), unlit: true);

                // Военный контейнер рядом со шпилем с рёбрами жёсткости
                new Entity(parent: root, model: "cube", scale: new(3.2f, 2.2f, 5.4f), position: new(-2.8f, 1.1f, 0), color: palette.Accent);
                foreach (float rz in new[] { -1.8f, -0.6f, 0.6f, 1.8f })
                    new Entity(parent: root, model: "cube", scale: new(3.32f, 1.95f, 0.22f), position: new(-2.8f, 1.1f, rz), color: darkRuins);
                new Entity(parent: root, model: "cube", scale: new(3.36f, 0.26f, 5.54f), position: new(-2.8f, 2.24f, 0), color: palette.Ruins);

                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.95f, h * 0.85f, pos.Y));
                foreach (float czOff in new[] { -1.6f, 0f, 1.6f })
                {
                    var (cwx, cwz) = ToWorld(-2.8f, czOff);
                    obstacles.Add(new ObstacleCircle(cwx, cwz, 1.75f, 2.3f, pos.Y));
                }
                var (bwx, bwz) = ToWorld(-2.8f, 0f);
                outBoxes?.Add(new ObstacleBox(bwx, bwz, 1.68f, 2.78f, rotY, 2.32f, pos.Y, walkableTop: true));
            }
            else if (structType == "sulfur_geyser")
            {
                // Низкая кислотная чаша гейзера — не перекрывает обзор (AllowObstructionFade = false)
                new Entity(parent: root, model: "sphere", scale: new(6.4f, 1.6f, 6.4f), position: new(0, 0.4f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "sphere", scale: new(4.6f, 1.9f, 4.6f), position: new(0, 0.55f, 0), color: palette.Accent, unlit: true);
                for (int i = 0; i < 4; i++)
                {
                    float by = 1.8f + i * 1.65f;
                    float sc = 1.6f - i * 0.25f;
                    new Entity(parent: root, model: "sphere", scaleUniform: sc,
                        position: new(MathF.Sin(i * 1.7f) * 0.4f, by, MathF.Cos(i * 1.7f) * 0.4f),
                        color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 145), unlit: true);
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 3.05f, 1.45f, pos.Y));
            }
            else if (structType == "aqueduct_pillar")
            {
                // Высокая песчаная арка акведука с каменным сводом, парапетами жёлоба и бирюзовым водопадом
                float h = NextRange(10.0f, 14.5f);
                root.AllowObstructionFade = true;
                root.ObstructionRadius = 6.0f;
                root.ObstructionHeight = h + 2.8f;

                foreach (float px in new[] { -4.2f, 4.2f })
                {
                    new Entity(parent: root, model: "cube", scale: new(3.35f, 0.65f, 3.35f), position: new(px, 0.325f, 0), color: darkRuins);
                    new Entity(parent: root, model: "cube", scale: new(2.60f, h, 2.60f), position: new(px, h * 0.5f, 0), color: palette.Ruins);
                    new Entity(parent: root, model: "cube", scale: new(3.05f, 0.55f, 3.05f), position: new(px, h - 0.25f, 0), color: lightRuins);
                    // Подкосы арочного свода под настилом акведука
                    float archSign = px < 0 ? 1f : -1f;
                    new Entity(parent: root, model: "cube", scale: new(2.6f, 0.85f, 2.2f),
                        position: new(px + archSign * 1.65f, h - 0.65f, 0),
                        rotation: new(0, 0, archSign * 26f), color: darkRuins);
                }

                // Верхний мост-акведук и каменные бортики жёлоба
                new Entity(parent: root, model: "cube", scale: new(11.6f, 1.65f, 3.20f), position: new(0, h + 0.65f, 0), color: lightRuins);
                foreach (float bz in new[] { -1.35f, 1.35f })
                    new Entity(parent: root, model: "cube", scale: new(11.8f, 0.62f, 0.46f), position: new(0, h + 1.72f, bz), color: darkRuins);
                // Проём слива в переднем бортике
                new Entity(parent: root, model: "cube", scale: new(2.15f, 0.72f, 0.62f), position: new(0, h + 1.68f, 1.38f), color: palette.Accent, unlit: true);

                // Водный поток в жёлобе, пенный гребень и полупрозрачная завеса водопада
                new Entity(parent: root, model: "cube", scale: new(11.7f, 0.42f, 2.15f), position: new(0, h + 1.56f, 0), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "sphere", scale: new(2.35f, 0.65f, 1.15f), position: new(0, h + 1.48f, 1.68f), color: Color.White, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(1.15f, h + 1.1f, 0.26f), position: new(0, (h + 1.1f) * 0.5f, 1.70f),
                    color: Color.White, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(1.95f, h + 1.3f, 0.52f), position: new(0, (h + 1.3f) * 0.5f, 1.72f),
                    color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 148), unlit: true);

                // Чаша бассейна и водяная дымка у подножия водопада
                new Entity(parent: root, model: "sphere", scale: new(5.4f, 0.42f, 5.4f), position: new(0, 0.16f, 1.72f), color: darkRuins);
                new Entity(parent: root, model: "sphere", scale: new(4.6f, 0.52f, 4.6f), position: new(0, 0.22f, 1.72f), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "sphere", scale: new(3.2f, 1.35f, 3.2f), position: new(0, 0.68f, 1.72f),
                    color: Utils.Rgb(255, 255, 255, 92), unlit: true);

                var (wx1, wz1) = ToWorld(-4.2f, 0);
                var (wx2, wz2) = ToWorld(4.2f, 0);
                obstacles.Add(new ObstacleCircle(wx1, wz1, 1.55f, h + 1.5f, pos.Y));
                obstacles.Add(new ObstacleCircle(wx2, wz2, 1.55f, h + 1.5f, pos.Y));
                outBoxes?.Add(new ObstacleBox(wx1, wz1, 1.38f, 1.38f, rotY, h + 1.5f, pos.Y, walkableTop: false));
                outBoxes?.Add(new ObstacleBox(wx2, wz2, 1.38f, 1.38f, rotY, h + 1.5f, pos.Y, walkableTop: false));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 5.9f, 1.62f, rotY, 2.15f, pos.Y + h - 0.3f, walkableTop: true));
            }
            else if (structType == "sun_temple")
            {
                // Святилище Солнца — ступенчатый мраморно-золотой алтарь с обелисками, небесными кольцами и солнечной короной
                root.AllowObstructionFade = true;
                root.ObstructionRadius = 4.4f;
                root.ObstructionHeight = 11.8f;

                new Entity(parent: root, model: "cube", scale: new(6.8f, 0.55f, 6.8f), position: new(0, 0.275f, 0), color: darkRuins);
                new Entity(parent: root, model: "cube", scale: new(5.5f, 0.60f, 5.5f), position: new(0, 0.85f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(4.1f, 0.48f, 4.1f), position: new(0, 1.39f, 0), color: lightRuins);

                // Лестничные марши с четырёх сторон алтаря и угловые золотые обелиски
                new Entity(parent: root, model: "cube", scale: new(1.8f, 0.38f, 7.30f), position: new(0, 0.19f, 0), color: lightRuins);
                new Entity(parent: root, model: "cube", scale: new(7.30f, 0.34f, 1.8f), position: new(0, 0.20f, 0), color: lightRuins);
                foreach (float sgn in new[] { -1f, 1f })
                {
                    foreach (float cz in new[] { -2.35f, 2.35f })
                    {
                        new Entity(parent: root, model: "cube", scale: new(0.62f, 1.25f, 0.62f), position: new(sgn * 2.35f, 1.75f, cz), color: palette.Ruins);
                        new Entity(parent: root, model: "diamond", scale: new(0.52f, 0.72f, 0.52f), position: new(sgn * 2.35f, 2.65f, cz), color: Color.Gold, unlit: true);
                    }
                }

                // Центральная солнечная колонна с золотыми перехватами
                new Entity(parent: root, model: "cube", scale: new(1.62f, 6.2f, 1.62f), position: new(0, 4.4f, 0), color: palette.Ruins);
                foreach (float gy in new[] { 2.6f, 4.8f, 6.9f })
                    new Entity(parent: root, model: "cube", scale: new(1.86f, 0.24f, 1.86f), position: new(0, gy, 0), color: Color.Gold, unlit: true);

                // Парящий солнечный диск с лучами, астролябией и полупрозрачной короной
                var sunCenter = new Vector3(0, 9.1f, 0);
                new Entity(parent: root, model: "diamond", scale: new(4.6f, 4.6f, 0.52f), position: sunCenter, color: Color.Gold, unlit: true);
                for (int i = 0; i < 4; i++)
                {
                    new Entity(parent: root, model: "diamond", scale: new(5.8f, 0.58f, 0.36f),
                        position: sunCenter, rotation: new(0, 0, i * 45f), color: Utils.Rgb(255, 220, 95), unlit: true);
                }
                new Entity(parent: root, model: "diamond", scale: new(5.6f, 0.22f, 5.6f), position: sunCenter,
                    rotation: new(28f, 20f, -18f), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 2.15f, position: sunCenter, color: Color.White, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 4.35f, position: sunCenter,
                    color: Utils.Rgb(255, 215, 85, 68), unlit: true);

                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 3.1f, 1.65f, pos.Y));
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.15f, 10.2f, pos.Y));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 3.42f, 3.42f, rotY, 0.55f, pos.Y, walkableTop: true));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.75f, 2.75f, rotY, 1.15f, pos.Y, walkableTop: true));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.05f, 2.05f, rotY, 1.63f, pos.Y, walkableTop: true));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 0.95f, 0.95f, rotY, 10.2f, pos.Y, walkableTop: false));
            }
            else if (structType == "swamp_root")
            {
                // Гигантская корневая дуга болота со свисающими лианами и янтарными спорами
                float w = NextRange(7.5f, 11.0f);
                float h = NextRange(6.5f, 9.5f);
                root.AllowObstructionFade = true;
                root.ObstructionRadius = Math.Clamp(w * 0.5f + 0.8f, 4.6f, 6.0f);
                root.ObstructionHeight = h + 2.0f;

                new Entity(parent: root, model: "cube", scale: new(1.85f, h, 1.85f), position: new(-w * 0.42f, h * 0.45f, 0), rotation: new(0, 0, -24f), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(1.85f, h, 1.85f), position: new(w * 0.42f, h * 0.45f, 0), rotation: new(0, 0, 24f), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(w * 0.76f, 1.55f, 1.92f), position: new(0, h - 0.6f, 0), color: darkRuins);
                foreach (float sx in new[] { -w * 0.25f, 0f, w * 0.25f })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.22f, 1.6f, 0.22f), position: new(sx, h - 1.55f, 0), color: darkRuins);
                    new Entity(parent: root, model: "sphere", scaleUniform: 1.12f, position: new(sx, h - 2.35f, 0), color: palette.Accent, unlit: true);
                }

                var (wx1, wz1) = ToWorld(-w * 0.42f, 0);
                var (wx2, wz2) = ToWorld(w * 0.42f, 0);
                obstacles.Add(new ObstacleCircle(wx1, wz1, 1.35f, h * 0.9f, pos.Y));
                obstacles.Add(new ObstacleCircle(wx2, wz2, 1.35f, h * 0.9f, pos.Y));

                // Опорные стопы наклонных корней на уровне земли
                float footOff = w * 0.42f + h * 0.16f;
                var (fx1, fz1) = ToWorld(-footOff, 0);
                var (fx2, fz2) = ToWorld(footOff, 0);
                obstacles.Add(new ObstacleCircle(fx1, fz1, 1.25f, 3.2f, pos.Y));
                obstacles.Add(new ObstacleCircle(fx2, fz2, 1.25f, 3.2f, pos.Y));
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, w * 0.38f, 0.95f, rotY, 1.6f, pos.Y + h - 1.4f, walkableTop: true));
            }
            else if (structType == "astral_ring")
            {
                // Врезанное в землю древнее астральное кольцо с каменными пилонами, парящими рунами и портальной завесой
                float r = NextRange(6.0f, 9.0f);
                root.AllowObstructionFade = true;
                root.ObstructionRadius = Math.Clamp(r * 0.78f, 4.8f, 6.0f);
                root.ObstructionHeight = r * 1.65f + 2.0f;

                var ringCenter = new Vector3(0, r * 0.65f, 0);
                var ringPivot = new Entity(parent: root, position: ringCenter, rotation: new(18f, 0, 22f));

                // Опорные каменные башмаки в месте входа кольца в землю
                foreach (float bx in new[] { -r * 0.62f, r * 0.62f })
                    new Entity(parent: root, model: "cube", scale: new(2.8f, 1.45f, 2.8f), position: new(bx, 0.65f, 0), rotation: new(0, 18f, 0), color: darkRuins);

                // Внешний каменный обод, внутренний рунический контур и гироскопическое кольцо
                new Entity(parent: ringPivot, model: "diamond", scale: new(r * 2.0f, r * 2.0f, 1.35f), color: palette.Ruins);
                new Entity(parent: ringPivot, model: "diamond", scale: new(r * 1.52f, r * 1.52f, 1.58f), color: palette.Accent, unlit: true);
                new Entity(parent: ringPivot, model: "diamond", scale: new(r * 1.18f, r * 1.18f, 0.42f), rotation: new(36f, 28f, 0), color: lightRuins);

                // 8 парящих рунных кристаллов по периметру астрального врата
                for (int i = 0; i < 8; i++)
                {
                    float ang = (i * 45f) * MathF.PI / 180f;
                    float rx = MathF.Cos(ang) * r * 0.88f;
                    float ry = MathF.Sin(ang) * r * 0.88f;
                    new Entity(parent: ringPivot, model: "diamond", scale: new(0.68f, 1.25f, 1.72f),
                        position: new(rx, ry, 0), rotation: new(0, 0, i * 45f - 90f), color: Color.White, unlit: true);
                }

                // Центральная сингулярность и полупрозрачная портальная мембрана
                new Entity(parent: ringPivot, model: "sphere", scaleUniform: 2.15f, color: Color.White, unlit: true);
                new Entity(parent: ringPivot, model: "diamond", scale: new(r * 1.25f, r * 1.25f, 0.24f),
                    color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 72), unlit: true);

                foreach (float rxOff in new[] { -r * 0.55f, 0f, r * 0.55f })
                {
                    var (rwx, rwz) = ToWorld(rxOff, 0);
                    obstacles.Add(new ObstacleCircle(rwx, rwz, 2.35f, r * 1.15f, pos.Y));
                }
                outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, r * 0.86f, 1.12f, rotY, r * 1.2f, pos.Y, walkableTop: false));
            }
            else
            {
                TryBuildExpansionMapStructure(root, structType, palette, pos, rotY, rng, obstacles, outBoxes);
            }

            return (root, obstacles);
        }
    }
}
