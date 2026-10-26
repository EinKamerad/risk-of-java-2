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
            X = x; Z = z; Radius = r; TopHeight = h; BaseY = baseY;
        }
    }

    public static class WorldStructuresBuilder
    {
        public static Entity BuildSkybox(Entity? parentEntity, StagePalette palette)
        {
            var skyRoot = new Entity(parent: parentEntity);
            var rng = new Random(42);
            float NextRange(float min, float max) => min + (float)rng.NextDouble() * (max - min);

            // Туманности — многослойные облака разных размеров и непрозрачности
            for (int i = 0; i < 14; i++)
            {
                float ang = (i / 14.0f) * MathF.PI * 2f;
                float dist = NextRange(165f, 195f);
                float nebY = NextRange(12f, 48f);
                // Основной слой туманности
                new Entity(
                    parent: skyRoot, model: "sphere",
                    position: new(MathF.Cos(ang) * dist, nebY, MathF.Sin(ang) * dist),
                    scale: new(NextRange(45f, 75f), NextRange(14f, 26f), NextRange(45f, 75f)),
                    rotation: new(0, NextRange(0f, 360f), 0), color: palette.Nebula, unlit: true
                );
                // Подслой — тонкая полупрозрачная вуаль вокруг туманности
                if (i % 2 == 0)
                {
                    new Entity(
                        parent: skyRoot, model: "sphere",
                        position: new(MathF.Cos(ang) * (dist - 5f), nebY + NextRange(-4f, 4f), MathF.Sin(ang) * (dist - 5f)),
                        scale: new(NextRange(55f, 85f), NextRange(8f, 14f), NextRange(55f, 85f)),
                        rotation: new(0, NextRange(0f, 360f), 0),
                        color: Utils.Rgb(palette.Nebula.R, palette.Nebula.G, palette.Nebula.B, 110), unlit: true
                    );
                }
            }

            // Далёкие пылевые облака — дополнительные туманности на дальнем фоне
            for (int i = 0; i < 6; i++)
            {
                float ang = (i / 6.0f) * MathF.PI * 2f + 0.45f;
                float dist = NextRange(200f, 220f);
                new Entity(
                    parent: skyRoot, model: "diamond",
                    position: new(MathF.Cos(ang) * dist, NextRange(45f, 85f), MathF.Sin(ang) * dist),
                    scale: new(NextRange(30f, 55f), NextRange(6f, 12f), NextRange(30f, 55f)),
                    rotation: new(NextRange(0f, 20f), NextRange(0f, 360f), 0),
                    color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 85), unlit: true
                );
            }

            // Звёзды — яркие точки с вариативными размерами
            for (int i = 0; i < 140; i++)
            {
                float theta = NextRange(0f, MathF.PI * 2f);
                float phi = NextRange(0.12f, MathF.PI * 0.48f);
                float r = NextRange(175f, 215f);
                float starSize = NextRange(0.9f, 2.4f);
                new Entity(
                    parent: skyRoot, model: "diamond",
                    position: new(r * MathF.Cos(phi) * MathF.Cos(theta), r * MathF.Sin(phi), r * MathF.Cos(phi) * MathF.Sin(theta)),
                    scaleUniform: starSize,
                    color: rng.NextDouble() > 0.35 ? Color.White : palette.Accent, unlit: true
                );
            }

            // Яркие звёзды-маяки — крупные, с лучами
            for (int i = 0; i < 8; i++)
            {
                float theta = NextRange(0f, MathF.PI * 2f);
                float phi = NextRange(0.25f, MathF.PI * 0.42f);
                float r = NextRange(180f, 210f);
                var starPos = new Vector3(r * MathF.Cos(phi) * MathF.Cos(theta), r * MathF.Sin(phi), r * MathF.Cos(phi) * MathF.Sin(theta));
                new Entity(parent: skyRoot, model: "sphere", scaleUniform: NextRange(2.2f, 3.8f), position: starPos, color: Color.White, unlit: true);
                // Крестообразные лучи
                new Entity(parent: skyRoot, model: "diamond", scale: new(NextRange(4f, 7f), 0.4f, 0.4f), position: starPos, color: Utils.Rgb(255, 255, 255, 160), unlit: true);
                new Entity(parent: skyRoot, model: "diamond", scale: new(0.4f, NextRange(4f, 7f), 0.4f), position: starPos, color: Utils.Rgb(255, 255, 255, 120), unlit: true);
            }

            // Расколотая Луна Petrichor V с деталями и трещинами
            var moonPivot = new Entity(parent: skyRoot, position: new(65, 78, 125), rotation: new(-15, -30, 20));
            // Основное тело луны
            new Entity(parent: moonPivot, model: "sphere", scaleUniform: 28f, color: palette.Moon, unlit: true);
            // Кратеры на поверхности
            foreach (var (cx, cy, cz, cs) in new[] { (8f, 12f, 22f, 4.5f), (-14f, 5f, 20f, 3.2f), (3f, -8f, 26f, 2.8f), (16f, -3f, 18f, 3.8f), (-6f, 18f, 16f, 2.5f) })
            {
                new Entity(parent: moonPivot, model: "sphere", scaleUniform: cs, position: new(cx, cy, cz),
                    color: Utils.Rgb((byte)Math.Max(palette.Moon.R - 25, 0), (byte)Math.Max(palette.Moon.G - 25, 0), (byte)Math.Max(palette.Moon.B - 20, 0)), unlit: true);
            }
            // Раскол — линия трещины по луне
            new Entity(parent: moonPivot, model: "cube", scale: new(0.8f, 35f, 2.5f), rotation: new(0, 0, 12f), color: Utils.Rgb(255, 120, 45), unlit: true);
            new Entity(parent: moonPivot, model: "cube", scale: new(0.5f, 32f, 1.5f), rotation: new(0, 0, 12f), color: Utils.Rgb(255, 200, 80), unlit: true);

            // Пояс из 14 астероидов с деталями
            for (int i = 0; i < 14; i++)
            {
                float ang = (i / 14.0f) * MathF.PI * 2f;
                float dist = NextRange(19f, 30f);
                float aSize = NextRange(2.5f, 5.2f);
                var asteroid = new Entity(
                    parent: moonPivot, model: "diamond",
                    position: new(MathF.Cos(ang) * dist, NextRange(-5f, 7f), MathF.Sin(ang) * dist),
                    scaleUniform: aSize,
                    rotation: new(NextRange(0f, 180f), NextRange(0f, 180f), 0f),
                    color: palette.Moon, unlit: true
                );
                // Детали на крупных астероидах
                if (aSize > 3.5f)
                {
                    new Entity(parent: asteroid, model: "sphere", scaleUniform: 0.35f, position: new(0.3f, 0.2f, 0), 
                        color: Utils.Rgb((byte)Math.Max(palette.Moon.R - 30, 0), (byte)Math.Max(palette.Moon.G - 30, 0), (byte)Math.Max(palette.Moon.B - 25, 0)), unlit: true);
                }
            }
            // Кольцо обломков
            new Entity(parent: moonPivot, model: "diamond", scale: new(52f, 0.9f, 52f), color: palette.Accent, unlit: true);
            // Второе тонкое кольцо
            new Entity(parent: moonPivot, model: "diamond", scale: new(42f, 0.4f, 42f), rotation: new(0, 25f, 5f), 
                color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 120), unlit: true);

            // Далёкий газовый гигант с атмосферой и двойным кольцом
            var planet = new Entity(parent: skyRoot, position: new(-95, 62, -105), rotation: new(25, 15, -20));
            // Ядро планеты
            new Entity(parent: planet, model: "sphere", scaleUniform: 22f, color: palette.Nebula, unlit: true);
            // Атмосферные полосы
            new Entity(parent: planet, model: "sphere", scale: new(22.5f, 22.5f, 22.5f), 
                color: Utils.Rgb(palette.Nebula.R, palette.Nebula.G, palette.Nebula.B, 140), unlit: true);
            foreach (float by in new[] { -8f, -3f, 2f, 7f, 12f })
            {
                new Entity(parent: planet, model: "cube", scale: new(22.5f, 0.6f, 22.5f), position: new(0, by, 0),
                    color: Utils.Rgb(
                        (byte)Math.Min(palette.Nebula.R + 20, 255), 
                        (byte)Math.Min(palette.Nebula.G + 15, 255), 
                        (byte)Math.Min(palette.Nebula.B + 10, 255), 80), unlit: true);
            }
            // Кольца
            new Entity(parent: planet, model: "diamond", scale: new(48f, 0.7f, 48f), color: palette.Accent, unlit: true);
            new Entity(parent: planet, model: "diamond", scale: new(44f, 0.35f, 44f), 
                color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 140), unlit: true);
            new Entity(parent: planet, model: "diamond", scale: new(36f, 0.5f, 36f), rotation: new(0, 45f, 0), color: palette.Moon, unlit: true);
            // Спутник
            var moonlet = new Entity(parent: planet, position: new(32f, 5f, 18f));
            new Entity(parent: moonlet, model: "sphere", scaleUniform: 3.2f, color: palette.Moon, unlit: true);

            // Далёкая туманность-змея — декоративная протяжённая структура
            var snakeNeb = new Entity(parent: skyRoot, position: new(140, 95, -60), rotation: new(10, 45, 15));
            new Entity(parent: snakeNeb, model: "sphere", scale: new(35f, 6f, 8f), color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 65), unlit: true);
            new Entity(parent: snakeNeb, model: "sphere", scale: new(25f, 4f, 6f), position: new(18f, 3f, -2f), color: Utils.Rgb(palette.Nebula.R, palette.Nebula.G, palette.Nebula.B, 55), unlit: true);

            return skyRoot;
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

            if (structType == "arch")
            {
                float h = NextRange(7.5f, 11.5f);
                float w = NextRange(6.5f, 9.0f);
                // Левая колонна
                new Entity(parent: root, model: "cube", scale: new(1.8f, h, 1.8f), position: new(-w * 0.5f, h * 0.5f, 0), color: palette.Ruins);
                // Правая колонна
                new Entity(parent: root, model: "cube", scale: new(1.8f, h, 1.8f), position: new(w * 0.5f, h * 0.5f, 0), color: palette.Ruins);
                // Каннелюры на колоннах — вертикальные борозды
                foreach (float sx in new[] { -w * 0.5f, w * 0.5f })
                {
                    foreach (float fz in new[] { -0.65f, 0.0f, 0.65f })
                    {
                        new Entity(parent: root, model: "cube", scale: new(0.15f, h * 0.85f, 0.15f), 
                            position: new(sx, h * 0.45f, fz), color: Utils.Rgb(
                                (byte)Math.Max(palette.Ruins.R - 15, 0),
                                (byte)Math.Max(palette.Ruins.G - 15, 0),
                                (byte)Math.Max(palette.Ruins.B - 12, 0)));
                    }
                    // Капитель — расширение вершины колонны
                    new Entity(parent: root, model: "cube", scale: new(2.4f, 0.5f, 2.4f), position: new(sx, h - 0.25f, 0), color: palette.Ruins);
                    new Entity(parent: root, model: "diamond", scale: new(2.6f, 0.3f, 2.6f), position: new(sx, h + 0.1f, 0), color: palette.Ruins);
                    // Основание колонны
                    new Entity(parent: root, model: "cube", scale: new(2.3f, 0.4f, 2.3f), position: new(sx, 0.2f, 0), color: palette.Ruins);
                }
                // Перемычка-архитрав
                new Entity(parent: root, model: "cube", scale: new(w + 2.8f, 1.6f, 2.2f), position: new(0, h + 0.5f, 0), color: palette.Ruins);
                // Фриз — декоративный слой на перемычке
                new Entity(parent: root, model: "cube", scale: new(w + 2.6f, 0.35f, 2.3f), position: new(0, h + 1.3f, 0), color: palette.Ruins);
                // Карниз — козырёк
                new Entity(parent: root, model: "cube", scale: new(w + 3.2f, 0.25f, 2.8f), position: new(0, h + 1.55f, 0), 
                    color: Utils.Rgb(
                        (byte)Math.Min(palette.Ruins.R + 10, 255),
                        (byte)Math.Min(palette.Ruins.G + 10, 255),
                        (byte)Math.Min(palette.Ruins.B + 8, 255)));
                // Руна под аркой — светящийся элемент
                new Entity(parent: root, model: "diamond", scale: new(1.3f, 2.3f, 1.3f), position: new(0, h - 1.5f, 0), color: palette.Accent, unlit: true);
                // Дополнительные руны на колоннах
                foreach (float sx in new[] { -w * 0.5f, w * 0.5f })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.2f, 1.2f, 0.05f), position: new(sx, h * 0.4f, 0.92f), color: palette.Accent, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(0.5f, 0.5f, 0.5f), position: new(sx, h * 0.65f, 0.92f), color: palette.Accent, unlit: true);
                }
                // Обломки у основания
                foreach (float dx in new[] { -w * 0.3f, w * 0.25f })
                {
                    new Entity(parent: root, model: "cube", scale: new(NextRange(0.6f, 1.2f), NextRange(0.3f, 0.6f), NextRange(0.6f, 1.0f)),
                        position: new(dx, NextRange(0.15f, 0.3f), NextRange(-1.5f, 1.5f)),
                        rotation: new(NextRange(0, 25), NextRange(0, 45), NextRange(0, 20)),
                        color: palette.Ruins);
                }

                var (wx1, wz1) = ToWorld(-w * 0.5f, 0);
                var (wx2, wz2) = ToWorld(w * 0.5f, 0);
                obstacles.Add(new ObstacleCircle(wx1, wz1, 1.15f, h));
                obstacles.Add(new ObstacleCircle(wx2, wz2, 1.15f, h));
            }
            else if (structType == "monolith")
            {
                float h = NextRange(7.0f, 14.0f);
                // Ступенчатый постамент
                new Entity(parent: root, model: "cube", scale: new(4.4f, 0.6f, 4.4f), position: new(0, 0.3f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(3.8f, 0.6f, 3.8f), position: new(0, 0.9f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(3.2f, 0.5f, 3.2f), position: new(0, 1.35f, 0), 
                    color: Utils.Rgb(
                        (byte)Math.Max(palette.Ruins.R - 10, 0),
                        (byte)Math.Max(palette.Ruins.G - 10, 0),
                        (byte)Math.Max(palette.Ruins.B - 8, 0)));
                // Стела
                new Entity(parent: root, model: "cube", scale: new(2.5f, h, 2.5f), position: new(0, h * 0.5f + 1.5f, 0), color: palette.Ruins);
                // Рельефные грани стелы
                foreach (float fz in new[] { -1.28f, 1.28f })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.2f, h * 0.8f, 0.2f), 
                        position: new(0, h * 0.5f + 1.5f, fz), color: Utils.Rgb(
                            (byte)Math.Max(palette.Ruins.R - 18, 0),
                            (byte)Math.Max(palette.Ruins.G - 18, 0),
                            (byte)Math.Max(palette.Ruins.B - 15, 0)));
                }
                foreach (float fx in new[] { -1.28f, 1.28f })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.2f, h * 0.8f, 0.2f), 
                        position: new(fx, h * 0.5f + 1.5f, 0), color: Utils.Rgb(
                            (byte)Math.Max(palette.Ruins.R - 18, 0),
                            (byte)Math.Max(palette.Ruins.G - 18, 0),
                            (byte)Math.Max(palette.Ruins.B - 15, 0)));
                }
                // Руна на стеле — вертикальная светящаяся полоса
                new Entity(parent: root, model: "cube", scale: new(0.45f, h * 0.72f, 2.6f), position: new(0, h * 0.5f + 1.5f, 0), color: palette.Accent, unlit: true);
                // Горизонтальные руны
                foreach (float ry in new[] { h * 0.3f + 1.5f, h * 0.6f + 1.5f })
                    new Entity(parent: root, model: "cube", scale: new(2.6f, 0.12f, 0.1f), position: new(0, ry, 1.28f), color: palette.Accent, unlit: true);
                // Навершие — пирамида
                new Entity(parent: root, model: "diamond", scale: new(2.6f, 2.6f, 2.6f), position: new(0, h + 1.6f, 0), color: palette.Ruins);
                // Глаз на навершии
                new Entity(parent: root, model: "sphere", scaleUniform: 0.6f, position: new(0, h + 1.6f, 1.3f), color: palette.Accent, unlit: true);
                // Обломки у подножия
                for (int i = 0; i < 3; i++)
                {
                    float ang = NextRange(0f, 360f);
                    float dist = NextRange(2.2f, 3.5f);
                    new Entity(parent: root, model: "cube", 
                        scale: new(NextRange(0.5f, 0.9f), NextRange(0.3f, 0.7f), NextRange(0.5f, 0.9f)),
                        position: new(MathF.Cos(ang * MathF.PI / 180f) * dist, NextRange(0.15f, 0.35f), MathF.Sin(ang * MathF.PI / 180f) * dist),
                        rotation: new(NextRange(0, 30), NextRange(0, 60), NextRange(0, 15)),
                        color: palette.Ruins);
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.95f, h + 1.5f));
            }
            else if (structType == "crystals")
            {
                // Основание — каменный выход
                new Entity(parent: root, model: "sphere", scale: new(3.6f, 2.2f, 3.4f), position: new(0, 0.7f, 0), color: palette.Ruins);
                // Каменные трещины на основании
                new Entity(parent: root, model: "cube", scale: new(3.8f, 0.1f, 0.15f), position: new(0, 1.5f, 0.4f), color: Utils.Rgb(
                    (byte)Math.Max(palette.Ruins.R - 20, 0),
                    (byte)Math.Max(palette.Ruins.G - 20, 0),
                    (byte)Math.Max(palette.Ruins.B - 18, 0)));
                new Entity(parent: root, model: "cube", scale: new(0.15f, 0.1f, 3.5f), position: new(-0.3f, 1.5f, 0), color: Utils.Rgb(
                    (byte)Math.Max(palette.Ruins.R - 20, 0),
                    (byte)Math.Max(palette.Ruins.G - 20, 0),
                    (byte)Math.Max(palette.Ruins.B - 18, 0)));
                // Основные кристаллы
                for (int i = 0; i < 3; i++)
                {
                    float ang = NextRange(-25f, 25f);
                    float ch = NextRange(2.5f, 4.8f);
                    float cx = NextRange(-1f, 1f);
                    float cz = NextRange(-1f, 1f);
                    // Кристалл
                    new Entity(parent: root, model: "diamond", scale: new(0.95f, ch, 0.95f),
                        position: new(cx, ch * 0.4f, cz),
                        rotation: new(ang, NextRange(0f, 360f), ang), color: palette.Accent, unlit: true);
                    // Свечение у основания кристалла
                    new Entity(parent: root, model: "sphere", scale: new(1.2f, 0.4f, 1.2f),
                        position: new(cx, 0.5f, cz),
                        color: Utils.Rgb(palette.Accent.R, palette.Accent.G, palette.Accent.B, 65), unlit: true);
                }
                // Маленькие побочные кристаллы
                for (int i = 0; i < 4; i++)
                {
                    float ang = NextRange(-35f, 35f);
                    float ch = NextRange(0.8f, 1.8f);
                    new Entity(parent: root, model: "diamond", scale: new(0.45f, ch, 0.45f),
                        position: new(NextRange(-1.5f, 1.5f), ch * 0.3f, NextRange(-1.5f, 1.5f)),
                        rotation: new(ang, NextRange(0f, 360f), ang * 0.6f), 
                        color: Utils.Rgb(
                            (byte)Math.Min(palette.Accent.R + 30, 255),
                            (byte)Math.Min(palette.Accent.G + 30, 255),
                            (byte)Math.Min(palette.Accent.B + 20, 255), 180), unlit: true);
                }
                // Мелкие осколки вокруг
                for (int i = 0; i < 5; i++)
                {
                    float sAng = NextRange(0f, 360f) * MathF.PI / 180f;
                    float sDist = NextRange(1.8f, 2.8f);
                    new Entity(parent: root, model: "diamond", scale: new(NextRange(0.2f, 0.4f), NextRange(0.3f, 0.6f), NextRange(0.2f, 0.4f)),
                        position: new(MathF.Cos(sAng) * sDist, NextRange(0.1f, 0.3f), MathF.Sin(sAng) * sDist),
                        rotation: new(NextRange(0, 45), NextRange(0, 180), NextRange(0, 30)),
                        color: palette.Accent, unlit: true);
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.85f, 2.2f));
            }
            else if (structType == "platform")
            {
                // Основание платформы
                new Entity(parent: root, model: "cube", scale: new(5.2f, 1.8f, 5.2f), position: new(0, 0.9f, 0), color: palette.Ruins);
                // Светящийся обод
                new Entity(parent: root, model: "cube", scale: new(5.3f, 0.2f, 5.3f), position: new(0, 1.7f, 0), color: palette.Accent, unlit: true);
                // Ступени вокруг платформы
                new Entity(parent: root, model: "cube", scale: new(5.8f, 0.5f, 5.8f), position: new(0, 0.25f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(6.2f, 0.3f, 6.2f), position: new(0, 0.05f, 0), 
                    color: Utils.Rgb(
                        (byte)Math.Max(palette.Ruins.R - 12, 0),
                        (byte)Math.Max(palette.Ruins.G - 12, 0),
                        (byte)Math.Max(palette.Ruins.B - 10, 0)));
                // Рунные линии на поверхности
                foreach (float lx in new[] { -1.5f, 0f, 1.5f })
                    new Entity(parent: root, model: "cube", scale: new(0.08f, 0.05f, 4.8f), position: new(lx, 1.82f, 0), color: palette.Accent, unlit: true);
                foreach (float lz in new[] { -1.5f, 0f, 1.5f })
                    new Entity(parent: root, model: "cube", scale: new(4.8f, 0.05f, 0.08f), position: new(0, 1.82f, lz), color: palette.Accent, unlit: true);
                // Угловые пилоны
                foreach (var (px, pz) in new[] { (-2.3f, -2.3f), (2.3f, -2.3f), (-2.3f, 2.3f), (2.3f, 2.3f) })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.6f, 2.6f, 0.6f), position: new(px, 1.3f, pz), color: palette.Ruins);
                    new Entity(parent: root, model: "diamond", scale: new(0.5f, 0.5f, 0.5f), position: new(px, 2.8f, pz), color: palette.Accent, unlit: true);
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.75f, 1.8f));
            }

            return (root, obstacles);
        }
    }
}