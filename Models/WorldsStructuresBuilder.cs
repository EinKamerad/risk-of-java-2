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

            for (int i = 0; i < 14; i++)
            {
                float ang = (i / 14.0f) * MathF.PI * 2f;
                float dist = NextRange(165f, 195f);
                new Entity(
                    parent: skyRoot, model: "sphere",
                    position: new(MathF.Cos(ang) * dist, NextRange(12f, 48f), MathF.Sin(ang) * dist),
                    scale: new(NextRange(45f, 75f), NextRange(14f, 26f), NextRange(45f, 75f)),
                    rotation: new(0, NextRange(0f, 360f), 0), color: palette.Nebula, unlit: true
                );
            }

            for (int i = 0; i < 140; i++)
            {
                float theta = NextRange(0f, MathF.PI * 2f);
                float phi = NextRange(0.12f, MathF.PI * 0.48f);
                float r = NextRange(175f, 215f);
                new Entity(
                    parent: skyRoot, model: "diamond",
                    position: new(r * MathF.Cos(phi) * MathF.Cos(theta), r * MathF.Sin(phi), r * MathF.Cos(phi) * MathF.Sin(theta)),
                    scaleUniform: NextRange(0.9f, 2.4f),
                    color: rng.NextDouble() > 0.35 ? Color.White : palette.Accent, unlit: true
                );
            }

            // Расколотая Луна Petrichor V с поясом из 14 астероидов
            var moonPivot = new Entity(parent: skyRoot, position: new(65, 78, 125), rotation: new(-15, -30, 20));
            new Entity(parent: moonPivot, model: "sphere", scaleUniform: 28f, color: palette.Moon, unlit: true);
            for (int i = 0; i < 14; i++)
            {
                float ang = (i / 14.0f) * MathF.PI * 2f;
                float dist = NextRange(19f, 30f);
                new Entity(
                    parent: moonPivot, model: "diamond",
                    position: new(MathF.Cos(ang) * dist, NextRange(-5f, 7f), MathF.Sin(ang) * dist),
                    scaleUniform: NextRange(2.5f, 5.2f),
                    rotation: new(NextRange(0f, 180f), NextRange(0f, 180f), 0f),
                    color: palette.Moon, unlit: true
                );
            }
            new Entity(parent: moonPivot, model: "diamond", scale: new(52f, 0.9f, 52f), color: palette.Accent, unlit: true);

            // Далёкий газовый гигант с двойным кольцом
            var planet = new Entity(parent: skyRoot, position: new(-95, 62, -105), rotation: new(25, 15, -20));
            new Entity(parent: planet, model: "sphere", scaleUniform: 22f, color: palette.Nebula, unlit: true);
            new Entity(parent: planet, model: "diamond", scale: new(48f, 0.7f, 48f), color: palette.Accent, unlit: true);
            new Entity(parent: planet, model: "diamond", scale: new(36f, 0.5f, 36f), rotation: new(0, 45f, 0), color: palette.Moon, unlit: true);

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
                new Entity(parent: root, model: "cube", scale: new(1.8f, h, 1.8f), position: new(-w * 0.5f, h * 0.5f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(1.8f, h, 1.8f), position: new(w * 0.5f, h * 0.5f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(w + 2.8f, 1.6f, 2.2f), position: new(0, h + 0.5f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "diamond", scale: new(1.3f, 2.3f, 1.3f), position: new(0, h - 1.5f, 0), color: palette.Accent, unlit: true);

                var (wx1, wz1) = ToWorld(-w * 0.5f, 0);
                var (wx2, wz2) = ToWorld(w * 0.5f, 0);
                obstacles.Add(new ObstacleCircle(wx1, wz1, 1.15f, h));
                obstacles.Add(new ObstacleCircle(wx2, wz2, 1.15f, h));
            }
            else if (structType == "monolith")
            {
                float h = NextRange(7.0f, 14.0f);
                new Entity(parent: root, model: "cube", scale: new(3.8f, 1.6f, 3.8f), position: new(0, 0.8f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(2.5f, h, 2.5f), position: new(0, h * 0.5f + 1.0f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(0.45f, h * 0.72f, 2.6f), position: new(0, h * 0.5f + 1.0f, 0), color: palette.Accent, unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(2.6f, 2.6f, 2.6f), position: new(0, h + 1.6f, 0), color: palette.Ruins);
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.95f, h + 1.5f));
            }
            else if (structType == "crystals")
            {
                new Entity(parent: root, model: "sphere", scale: new(3.6f, 2.2f, 3.4f), position: new(0, 0.7f, 0), color: palette.Ruins);
                for (int i = 0; i < 3; i++)
                {
                    float ang = NextRange(-25f, 25f);
                    float ch = NextRange(2.5f, 4.8f);
                    new Entity(parent: root, model: "diamond", scale: new(0.95f, ch, 0.95f),
                        position: new(NextRange(-1f, 1f), ch * 0.4f, NextRange(-1f, 1f)),
                        rotation: new(ang, NextRange(0f, 360f), ang), color: palette.Accent, unlit: true);
                }
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.85f, 2.2f));
            }
            else if (structType == "platform")
            {
                new Entity(parent: root, model: "cube", scale: new(5.2f, 1.8f, 5.2f), position: new(0, 0.9f, 0), color: palette.Ruins);
                new Entity(parent: root, model: "cube", scale: new(5.3f, 0.2f, 5.3f), position: new(0, 1.7f, 0), color: palette.Accent, unlit: true);
                obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.75f, 1.8f));
            }

            return (root, obstacles);
        }
    }
}