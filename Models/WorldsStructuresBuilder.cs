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

    public static class WorldStructuresBuilder
    {
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
            Entity? parentEntity, string structType, StagePalette palette, Vector3 pos, float rotY, Random? rng = null)
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
                (byte)Math.Min(palette.Ruins.R + 16, 255),
                (byte)Math.Min(palette.Ruins.G + 16, 255),
                (byte)Math.Min(palette.Ruins.B + 14, 255));

            if (structType == "arch")
            {
                float h = NextRange(8.0f, 12.5f);
                float w = NextRange(7.0f, 10.0f);
                new Entity(parent: root, model: "cube", scale: new(1.8f, h, 1.8f), position: new(-w * 0.5f, h * 0.5f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(1.8f, h, 1.8f), position: new(w * 0.5f, h * 0.5f, 0), color: palette.Ruins);

                foreach (float sx in new[] { -w * 0.5f, w * 0.5f })
                {
                    // Наружные каннелюры (выступают за пределы 1.8f на 0.06f, без Z-fighting)
                    foreach (float fOff in new[] { -0.55f, 0.0f, 0.55f })
                    {
                        new Entity(parent: root, model: "cube", scale: new(0.14f, h * 0.82f, 1.88f),
                            position: new(sx + fOff, h * 0.48f, 0), color: darkRuins);
                    }
                    new Entity(parent: root, model: "cube", scale: new(2.4f, 0.5f, 2.4f), position: new(sx, h - 0.25f, 0), color: lightRuins);
                    new Entity(parent: root, model: "diamond", scale: new(2.6f, 0.35f, 2.6f), position: new(sx, h + 0.15f, 0), color: palette.Ruins);
                    new Entity(parent: root, model: "cube", scale: new(2.35f, 0.45f, 2.35f), position: new(sx, 0.225f, 0), color: darkRuins);
                    new Entity(parent: root, model: "cube", scale: new(0.22f, 1.4f, 1.94f), position: new(sx, h * 0.42f, 0), color: palette.Accent, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(0.55f, 0.55f, 2.02f), position: new(sx, h * 0.68f, 0), color: palette.Accent, unlit: true);
                }

                // Архитрав и карниз (разнесены по высоте и ширине)
                new Entity(parent: root, model: "cube", scale: new(w + 2.8f, 1.5f, 2.2f), position: new(0, h + 0.75f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(w + 3.2f, 0.35f, 2.6f), position: new(0, h + 1.68f, 0), color: lightRuins);
                new Entity(parent: root, model: "diamond", scale: new(1.35f, 2.4f, 1.35f), position: new(0, h - 1.4f, 0), color: palette.Accent, unlit: true);

                var (wx1, wz1) = ToWorld(-w * 0.5f, 0);
                var (wx2, wz2) = ToWorld(w * 0.5f, 0);
                obstacles.Add(new ObstacleCircle(wx1, wz1, 1.2f, h, pos.Y));
                obstacles.Add(new ObstacleCircle(wx2, wz2, 1.2f, h, pos.Y));
            }
            else if (structType == "monolith")
            {
                float h = NextRange(8.0f, 15.0f);
                new Entity(parent: root, model: "cube", scale: new(4.4f, 0.6f, 4.4f), position: new(0, 0.3f, 0), color: darkRuins);
                new Entity(parent: root, model: "cube", scale: new(3.7f, 0.6f, 3.7f), position: new(0, 0.9f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(3.1f, 0.5f, 3.1f), position: new(0, 1.45f, 0), color: lightRuins);
                new Entity(parent: root, model: "cube", scale: new(2.5f, h, 2.5f), position: new(0, h * 0.5f + 1.7f, 0), color: palette.Ruins);

                // Рельефные угловые рёбра
                foreach (float fx in new[] { -1.22f, 1.22f })
                    foreach (float fz in new[] { -1.22f, 1.22f })
                        new Entity(parent: root, model: "cube", scale: new(0.24f, h * 0.92f, 0.24f),
                            position: new(fx, h * 0.5f + 1.7f, fz), color: darkRuins);

                // Вертикальная и горизонтальные руны с разной толщиной Z, чтобы не было ряби
                new Entity(parent: root, model: "cube", scale: new(0.42f, h * 0.72f, 2.58f), position: new(0, h * 0.5f + 1.7f, 0), color: palette.Accent, unlit: true);
                foreach (float ry in new[] { h * 0.32f + 1.7f, h * 0.62f + 1.7f })
                    new Entity(parent: root, model: "cube", scale: new(2.64f, 0.16f, 2.64f), position: new(0, ry, 0), color: palette.Accent, unlit: true);

                new Entity(parent: root, model: "diamond", scale: new(2.7f, 2.8f, 2.7f), position: new(0, h + 2.1f, 0), color: lightRuins);
                new Entity(parent: root, model: "diamond", scale: new(1.2f, 2.0f, 1.2f), position: new(0, h + 4.1f, 0), color: palette.Accent, unlit: true);
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.0f, h + 2.0f, pos.Y));
            }
            else if (structType == "crystals")
            {
                new Entity(parent: root, model: "sphere", scale: new(4.0f, 2.2f, 3.8f), position: new(0, 0.65f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "sphere", scale: new(2.8f, 1.4f, 2.6f), position: new(0.4f, 0.85f, -0.3f), color: darkRuins);
                for (int i = 0; i < 4; i++)
                {
                    float ang = NextRange(-26f, 26f);
                    float ch = NextRange(3.0f, 5.6f);
                    float cx = NextRange(-1.1f, 1.1f);
                    float cz = NextRange(-1.1f, 1.1f);
                    new Entity(parent: root, model: "diamond", scale: new(1.05f, ch, 1.05f),
                        position: new(cx, ch * 0.42f, cz),
                        rotation: new(ang, NextRange(0f, 360f), -ang * 0.7f), color: palette.Accent, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(0.55f, ch * 0.65f, 0.55f),
                        position: new(cx + 0.35f, ch * 0.30f, cz - 0.25f),
                        rotation: new(-ang * 1.1f, NextRange(0f, 360f), ang), color: Color.White, unlit: true);
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.05f, 5.2f, pos.Y));
            }
            else if (structType == "platform")
            {
                new Entity(parent: root, model: "cube", scale: new(6.4f, 0.35f, 6.4f), position: new(0, 0.175f, 0), color: darkRuins);
                new Entity(parent: root, model: "cube", scale: new(5.8f, 0.45f, 5.8f), position: new(0, 0.575f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(5.2f, 1.0f, 5.2f), position: new(0, 1.30f, 0), color: lightRuins);
                new Entity(parent: root, model: "cube", scale: new(5.32f, 0.18f, 5.32f), position: new(0, 1.62f, 0), color: palette.Accent, unlit: true);

                // Рунные линии разнесены по высоте (1.82f vs 1.84f), чтобы на перекрёстках не было Z-fighting!
                foreach (float lx in new[] { -1.5f, 0f, 1.5f })
                    new Entity(parent: root, model: "cube", scale: new(0.10f, 0.04f, 4.8f), position: new(lx, 1.82f, 0), color: palette.Accent, unlit: true);
                foreach (float lz in new[] { -1.5f, 0f, 1.5f })
                    new Entity(parent: root, model: "cube", scale: new(4.8f, 0.06f, 0.10f), position: new(0, 1.84f, lz), color: palette.Accent, unlit: true);

                foreach (var (px, pz) in new[] { (-2.3f, -2.3f), (2.3f, -2.3f), (-2.3f, 2.3f), (2.3f, 2.3f) })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.62f, 1.4f, 0.62f), position: new(px, 2.5f, pz), color: palette.Ruins);
                    new Entity(parent: root, model: "diamond", scale: new(0.55f, 0.65f, 0.55f), position: new(px, 3.45f, pz), color: palette.Accent, unlit: true);
                    var (pwx, pwz) = ToWorld(px, pz);
                    obstacles.Add(new ObstacleCircle(pwx, pwz, 0.55f, 3.8f, pos.Y));
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.95f, 1.82f, pos.Y));
            }
            else if (structType == "ruined_tower")
            {
                // Древняя сторожевая башня с окнами, зубцами и жаровней наверху
                float h = NextRange(10.5f, 15.5f);
                new Entity(parent: root, model: "cube", scale: new(5.2f, 0.8f, 5.2f), position: new(0, 0.4f, 0), color: darkRuins);
                new Entity(parent: root, model: "cube", scale: new(4.2f, h, 4.2f), position: new(0, h * 0.5f + 0.8f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(4.6f, 0.6f, 4.6f), position: new(0, h + 0.8f, 0), color: lightRuins);
                // Зубцы башни и маяк
                foreach (var (bx, bz) in new[] { (-1.9f, -1.9f), (1.9f, -1.9f), (-1.9f, 1.9f), (1.9f, 1.9f) })
                    new Entity(parent: root, model: "cube", scale: new(0.8f, 0.9f, 0.8f), position: new(bx, h + 1.55f, bz), color: darkRuins);
                new Entity(parent: root, model: "diamond", scale: new(1.8f, 2.6f, 1.8f), position: new(0, h + 2.4f, 0), color: palette.Accent, unlit: true);
                // Светящиеся бойницы
                foreach (float wy in new[] { h * 0.38f, h * 0.72f })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.65f, 1.5f, 4.32f), position: new(0, wy, 0), color: palette.Accent, unlit: true);
                    new Entity(parent: root, model: "cube", scale: new(4.36f, 1.5f, 0.65f), position: new(0, wy, 0), color: palette.Accent, unlit: true);
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.45f, h + 1.1f, pos.Y));
            }
            else if (structType == "torii")
            {
                // Священные алые врата Тории (Иония) с изогнутой двойной перекладиной и фонарями
                float h = NextRange(9.0f, 12.5f);
                float w = NextRange(7.5f, 10.0f);
                Color crimson = Utils.Rgb(185, 38, 52);
                Color darkWood = Utils.Rgb(42, 32, 38);
                foreach (float sx in new[] { -w * 0.5f, w * 0.5f })
                {
                    new Entity(parent: root, model: "cube", scale: new(1.6f, 1.4f, 1.6f), position: new(sx, 0.7f, 0), color: darkWood);
                    new Entity(parent: root, model: "cube", scale: new(1.25f, h, 1.25f), position: new(sx, h * 0.5f, 0), color: crimson);
                    new Entity(parent: root, model: "cube", scale: new(1.35f, 0.35f, 1.35f), position: new(sx, h * 0.65f, 0), color: Color.Gold, unlit: true);
                    // Подвесной духовный фонарь
                    new Entity(parent: root, model: "diamond", scale: new(0.75f, 1.2f, 0.75f), position: new(sx * 0.72f, h - 1.8f, 0), color: palette.Accent, unlit: true);
                }
                // Нижняя перекладина (нуки) и верхняя крыша (касаги)
                new Entity(parent: root, model: "cube", scale: new(w + 2.4f, 0.75f, 1.0f), position: new(0, h - 1.4f, 0), color: crimson);
                new Entity(parent: root, model: "cube", scale: new(w + 4.2f, 0.85f, 1.6f), position: new(0, h + 0.35f, 0), color: crimson);
                new Entity(parent: root, model: "cube", scale: new(w + 4.8f, 0.45f, 1.9f), position: new(0, h + 0.95f, 0), color: darkWood);
                // Центральная золотая табличка
                new Entity(parent: root, model: "cube", scale: new(1.1f, 1.6f, 1.15f), position: new(0, h - 0.5f, 0), color: Color.Gold, unlit: true);

                var (wx1, wz1) = ToWorld(-w * 0.5f, 0);
                var (wx2, wz2) = ToWorld(w * 0.5f, 0);
                obstacles.Add(new ObstacleCircle(wx1, wz1, 1.0f, h, pos.Y));
                obstacles.Add(new ObstacleCircle(wx2, wz2, 1.0f, h, pos.Y));
            }
            else if (structType == "sakura_tree")
            {
                // Древнее цветущее дерево сакуры / духовного леса
                float trunkH = NextRange(6.5f, 9.5f);
                Color barkCol = Utils.Rgb(68, 44, 52);
                new Entity(parent: root, model: "cube", scale: new(2.2f, 1.2f, 2.2f), position: new(0, 0.6f, 0), color: barkCol);
                new Entity(parent: root, model: "cube", scale: new(1.6f, trunkH, 1.6f), position: new(0, trunkH * 0.5f, 0), rotation: new(4f, 0, -5f), color: barkCol);
                // Ветви и пышная многослойная крона
                foreach (var (bx, by, bz, bw, bh) in new[]
                {
                    (0f, trunkH + 1.2f, 0f, 8.2f, 3.6f),
                    (-2.8f, trunkH - 0.2f, 1.4f, 5.6f, 2.6f),
                    (2.6f, trunkH + 0.4f, -1.2f, 5.8f, 2.8f),
                    (0.5f, trunkH + 2.6f, 0.8f, 5.2f, 2.4f)
                })
                {
                    new Entity(parent: root, model: "sphere", scale: new(bw, bh, bw), position: new(bx, by, bz), color: palette.Accent, unlit: true);
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.45f, trunkH + 3.2f, pos.Y));
            }
            else if (structType == "giant_mushroom")
            {
                // Исполинский биолюминесцентный гриб (Расколотая Роща / Болото)
                float stemH = NextRange(6.5f, 11.0f);
                float capR = NextRange(4.2f, 6.8f);
                new Entity(parent: root, model: "cube", scale: new(1.8f, stemH, 1.8f), position: new(0, stemH * 0.5f, 0), color: lightRuins);
                new Entity(parent: root, model: "diamond", scale: new(capR * 1.45f, 0.9f, capR * 1.45f), position: new(0, stemH - 0.4f, 0), color: darkRuins);
                new Entity(parent: root, model: "sphere", scale: new(capR * 2f, 2.8f, capR * 2f), position: new(0, stemH + 0.45f, 0), color: palette.Accent, unlit: true);
                // Светящиеся пятна на шляпке
                foreach (float ang in new[] { 0f, 90f, 180f, 270f })
                {
                    float r = ang * MathF.PI / 180f;
                    new Entity(parent: root, model: "sphere", scale: new(1.4f, 0.55f, 1.4f),
                        position: new(MathF.Cos(r) * capR * 0.55f, stemH + 1.55f, MathF.Sin(r) * capR * 0.55f), color: Color.White, unlit: true);
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.35f, stemH + 1.5f, pos.Y));
                // По шляпке гриба можно прыгать как по высокой платформе!
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, capR * 0.90f, 1.5f, pos.Y + stemH));
            }
            else if (structType == "ice_spire")
            {
                // Полярный ледяной шпиль + арктический контейнер (Точка Сбора Дельта)
                float h = NextRange(8.5f, 14.0f);
                new Entity(parent: root, model: "diamond", scale: new(3.2f, h, 3.2f), position: new(0, h * 0.45f, 0),
                    rotation: new(6f, 25f, -8f), color: Utils.Rgb(165, 225, 255, 215), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(1.9f, h * 0.68f, 1.9f), position: new(1.4f, h * 0.30f, 0.8f),
                    rotation: new(-12f, 60f, 14f), color: Color.White, unlit: true);
                // Военный контейнер рядом со шпилем
                new Entity(parent: root, model: "cube", scale: new(3.2f, 2.2f, 5.4f), position: new(-2.8f, 1.1f, 0), color: palette.Accent);
                new Entity(parent: root, model: "cube", scale: new(3.3f, 0.25f, 5.5f), position: new(-2.8f, 2.15f, 0), color: palette.Ruins);
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.95f, h * 0.85f, pos.Y));
                foreach (float czOff in new[] { -1.6f, 0f, 1.6f })
                {
                    var (cwx, cwz) = ToWorld(-2.8f, czOff);
                    obstacles.Add(new ObstacleCircle(cwx, cwz, 1.75f, 2.3f, pos.Y));
                }
            }
            else if (structType == "sulfur_geyser")
            {
                // Кислотный гейзер и террасная чаша (Серные Озёра)
                new Entity(parent: root, model: "sphere", scale: new(6.4f, 1.6f, 6.4f), position: new(0, 0.4f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "sphere", scale: new(4.6f, 1.9f, 4.6f), position: new(0, 0.55f, 0), color: palette.Accent, unlit: true);
                // Столб ядовитых испарений и пузырей
                for (int i = 0; i < 4; i++)
                {
                    float by = 1.8f + i * 1.65f;
                    float sc = 1.6f - i * 0.25f;
                    new Entity(parent: root, model: "sphere", scaleUniform: sc,
                        position: new(MathF.Sin(i * 1.7f) * 0.4f, by, MathF.Cos(i * 1.7f) * 0.4f),
                        color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 145), unlit: true);
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.6f, 1.4f, pos.Y));
            }
            else if (structType == "aqueduct_pillar")
            {
                // Высокая песчаная арка акведука с водопадом (Заброшенный Акведук)
                float h = NextRange(10.0f, 14.5f);
                new Entity(parent: root, model: "cube", scale: new(2.6f, h, 2.6f), position: new(-4.2f, h * 0.5f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(2.6f, h, 2.6f), position: new(4.2f, h * 0.5f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(11.6f, 1.8f, 3.2f), position: new(0, h + 0.6f, 0), color: lightRuins);
                // Водный жёлоб и падающий поток бирюзовой воды
                new Entity(parent: root, model: "cube", scale: new(11.8f, 0.45f, 1.8f), position: new(0, h + 1.55f, 0), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(1.8f, h, 0.45f), position: new(0, h * 0.5f, 1.65f),
                    color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 165), unlit: true);
                new Entity(parent: root, model: "sphere", scale: new(4.6f, 0.45f, 4.6f), position: new(0, 0.15f, 1.65f),
                    color: palette.Accent, unlit: true);

                var (wx1, wz1) = ToWorld(-4.2f, 0);
                var (wx2, wz2) = ToWorld(4.2f, 0);
                obstacles.Add(new ObstacleCircle(wx1, wz1, 1.5f, h + 1.5f, pos.Y));
                obstacles.Add(new ObstacleCircle(wx2, wz2, 1.5f, h + 1.5f, pos.Y));
            }
            else if (structType == "sun_temple")
            {
                // Святилище Афелия — мраморно-золотой алтарь Солнца с парящими кольцами
                new Entity(parent: root, model: "cube", scale: new(6.8f, 0.6f, 6.8f), position: new(0, 0.3f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(5.2f, 0.7f, 5.2f), position: new(0, 0.95f, 0), color: lightRuins);
                new Entity(parent: root, model: "cube", scale: new(1.6f, 6.2f, 1.6f), position: new(0, 4.1f, 0), color: palette.Ruins);
                // Золотой солнечный диск наверху
                new Entity(parent: root, model: "diamond", scale: new(4.8f, 4.8f, 0.55f), position: new(0, 8.6f, 0), color: Color.Gold, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 2.2f, position: new(0, 8.6f, 0), color: Color.White, unlit: true);
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 3.1f, 1.35f, pos.Y));
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.15f, 9.6f, pos.Y));
            }
            else if (structType == "swamp_root")
            {
                // Гигантская корневая дуга болота с янтарными спорами (Болотистый Уступ)
                float w = NextRange(7.5f, 11.0f);
                float h = NextRange(6.5f, 9.5f);
                new Entity(parent: root, model: "cube", scale: new(1.8f, h, 1.8f), position: new(-w * 0.42f, h * 0.45f, 0), rotation: new(0, 0, -24f), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(1.8f, h, 1.8f), position: new(w * 0.42f, h * 0.45f, 0), rotation: new(0, 0, 24f), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(w * 0.75f, 1.5f, 1.8f), position: new(0, h - 0.6f, 0), color: darkRuins);
                foreach (float sx in new[] { -w * 0.25f, 0f, w * 0.25f })
                    new Entity(parent: root, model: "sphere", scaleUniform: 1.15f, position: new(sx, h - 1.4f, 0), color: palette.Accent, unlit: true);

                var (wx1, wz1) = ToWorld(-w * 0.42f, 0);
                var (wx2, wz2) = ToWorld(w * 0.42f, 0);
                obstacles.Add(new ObstacleCircle(wx1, wz1, 1.35f, h * 0.9f, pos.Y));
                obstacles.Add(new ObstacleCircle(wx2, wz2, 1.35f, h * 0.9f, pos.Y));
            }
            else if (structType == "astral_ring")
            {
                // Врезанное в землю древнее астральное кольцо (Небесный Луг / Зов Сирены)
                float r = NextRange(6.0f, 9.0f);
                new Entity(parent: root, model: "diamond", scale: new(r * 2f, r * 2f, 1.4f), position: new(0, r * 0.65f, 0),
                    rotation: new(18f, 0, 22f), color: palette.Ruins);
                new Entity(parent: root, model: "diamond", scale: new(r * 1.45f, r * 1.45f, 1.6f), position: new(0, r * 0.65f, 0),
                    rotation: new(18f, 0, 22f), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 2.1f, position: new(0, r * 0.65f, 0), color: Color.White, unlit: true);
                foreach (float rxOff in new[] { -r * 0.55f, 0f, r * 0.55f })
                {
                    var (rwx, rwz) = ToWorld(rxOff, 0);
                    obstacles.Add(new ObstacleCircle(rwx, rwz, 2.35f, r * 1.15f, pos.Y));
                }
            }

            return (root, obstacles);
        }
    }
}