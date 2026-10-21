using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using RiskOfCSharp2.Models;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.World
{
    public static class StageBuilder
    {
        private static Entity SpawnParkourDisc(
            List<Entity> entitiesList, List<SkyPlatform> skyPlatforms,
            Vector3 pos, float radius, float thickness = 1.2f, Color? col = null, Color? accent = null)
        {
            Color cMain = col ?? Rgb(200, 212, 232);
            Color cAcc = accent ?? Rgb(75, 215, 255);
            float physThick = MathF.Max(2.2f, thickness * 1.6f);
            var plat = new Entity(position: pos);
            // Плотная верхняя плита и массивное каменное основание снизу (исключает визуальные щели)
            new Entity(parent: plat, model: "cube", scale: new(radius * 1.52f, physThick * 0.75f, radius * 1.52f), position: new(0, -physThick * 0.40f, 0), color: Rgb(62, 74, 98));
            new Entity(parent: plat, model: "cube", scale: new(radius * 1.52f, physThick * 0.75f, radius * 1.52f), position: new(0, -physThick * 0.40f, 0), rotation: new(0, 45f, 0), color: cMain);
            new Entity(parent: plat, model: "sphere", scale: new(radius * 2f, thickness, radius * 2f), position: new(0, -thickness * 0.35f, 0), color: cMain);
            new Entity(parent: plat, model: "diamond", scale: new(radius * 2.05f, 0.22f, radius * 2.05f), color: cAcc, unlit: true);
            entitiesList.Add(plat);
            skyPlatforms.Add(new SkyPlatform(pos.X, pos.Z, radius, pos.Y, physThick));
            return plat;
        }

        public static (List<Entity> Entities, Entity? TeleporterOrPad, StagePalette Palette) BuildStageWorld(
            int stageNum, int seed, string? specialMode = null)
        {
            var entitiesList = new List<Entity>();
            Physics.Obstacles.Clear();
            Physics.SkyPlatforms.Clear();
            Physics.SpecialMode = specialMode;
            Physics.WorldBound = specialMode == "moon" ? 112.0f : 58.0f;

            StagePalette palette = specialMode switch
            {
                "bazaar" => GameData.SpecialPalettes["bazaar"],
                "moon" => GameData.SpecialPalettes["moon"],
                _ => GameData.StagePalettes[(stageNum - 1) % GameData.StagePalettes.Count]
            };

            if (!ProgressData.Instance.DiscoveredStages.Contains(palette.Id))
            {
                ProgressData.Instance.DiscoveredStages.Add(palette.Id);
                ProgressData.Instance.Save();
            }

            Engine.BackgroundColor = palette.Sky;
            entitiesList.Add(WorldStructuresBuilder.BuildSkybox(null, palette));

            var rng = new Random(seed);
            float NextRange(float min, float max) => min + (float)rng.NextDouble() * (max - min);

            float groundSize = specialMode == "moon" ? 240f : 126f;
            var ground = new Entity(model: "cube", scale: new(groundSize, 2f, groundSize), position: new(0, -1.0f, 0), color: palette.Ground);
            entitiesList.Add(ground);

            // ==========================================================
            // РЕЖИМ 1: БАЗАР МЕЖДУ ВРЕМЕНЕМ (BAZAAR BETWEEN TIME)
            // ==========================================================
            if (specialMode == "bazaar")
            {
                var newt = new Entity(position: new(0, 0, 22));
                new Entity(parent: newt, model: "sphere", scale: new(6.5f, 5.2f, 7.5f), position: new(0, 2.6f, 0), color: Rgb(45, 95, 165));
                new Entity(parent: newt, model: "sphere", scale: new(4.2f, 3.8f, 4.8f), position: new(0, 5.0f, 2.8f), color: Rgb(55, 115, 195));
                foreach (float ex in new[] { -1.2f, 1.2f })
                    new Entity(parent: newt, model: "sphere", scaleUniform: 0.65f, position: new(ex, 5.6f, 4.8f), color: Color.SkyBlue, unlit: true);
                entitiesList.Add(newt);

                var lunarKeys = GameData.LunarItems.Keys.OrderBy(_ => rng.Next()).ToList();
                var voidKeys = GameData.VoidItems.Keys.OrderBy(_ => rng.Next()).ToList();
                string[] offers = { lunarKeys[0], voidKeys[0], lunarKeys[1], voidKeys[1] };
                for (int i = 0; i < offers.Length; i++)
                    entitiesList.Add(new LunarShopBud(new Vector3(-12f + i * 8f, 0, 8f), offers[i], cost: 2));

                entitiesList.Add(new LunarCauldron(new Vector3(-10, 0, -6), GameData.RollItemFromPool("green"), reqCount: 3));
                entitiesList.Add(new LunarCauldron(new Vector3(10, 0, -6), GameData.RollItemFromPool("red"), reqCount: 5));
                entitiesList.Add(new BluePortal(new Vector3(0, 0, -18), "next_stage"));

                return (entitiesList, null, palette);
            }

            // ==========================================================
            // РЕЖИМ 2: COMMENCEMENT — ЛУНА И НЕБЕСНЫЙ КОЛИЗЕЙ МИТРИКСА
            // ==========================================================
            if (specialMode == "moon")
            {
                var jumpPad = new LunarJumpPad(Vector3.Zero);
                entitiesList.Add(jumpPad);

                for (int angleDeg = 0; angleDeg < 360; angleDeg += 45)
                {
                    if (angleDeg is 45 or 135 or 225 or 315) continue;
                    float rad = angleDeg * (MathF.PI / 180f);
                    var (sEnt, localObs) = WorldStructuresBuilder.BuildMapStructure(
                        null, "arch", palette, new Vector3(MathF.Cos(rad) * 26f, 0, MathF.Sin(rad) * 26f), angleDeg, rng);
                    entitiesList.Add(sEnt);
                    Physics.Obstacles.AddRange(localObs);
                }

                (int dx, int dz, string pType, float bHeight, Color bCol)[] bastions =
                {
                    (-1, -1, "mass", 12.0f, Rgb(75, 210, 255)),
                    (1, -1, "design", 15.0f, Rgb(110, 180, 255)),
                    (-1, 1, "blood", 11.0f, Rgb(245, 65, 85)),
                    (1, 1, "soul", 16.5f, Rgb(165, 110, 255))
                };

                int mChestCost = (int)(25 * MathF.Pow(1.25f, Math.Max(0, stageNum - 1)));
                int chestIdCounter = 200;

                foreach (var (dx, dz, pType, bHeight, bCol) in bastions)
                {
                    float normX = dx * 0.7071f, normZ = dz * 0.7071f;
                    int numSteps = 5;
                    for (int sIdx = 1; sIdx <= numSteps; sIdx++)
                    {
                        float frac = sIdx / (float)(numSteps + 1);
                        float dist = 24.0f + frac * 54.0f;
                        float lateral = MathF.Sin(sIdx * 1.4f) * 6.5f;
                        float px = normX * dist - normZ * lateral;
                        float pz = normZ * dist + normX * lateral;
                        float py = MathF.Round(bHeight * frac, 2);

                        SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(px, py, pz), 4.8f, 1.0f, Rgb(175, 190, 215), bCol);
                        if (sIdx == 2)
                        {
                            string cType = (dx + dz == 0) ? "large" : "small";
                            entitiesList.Add(new Chest(new Vector3(px, py, pz), mChestCost, chestIdCounter++, cType));
                        }
                    }

                    entitiesList.Add(new LunarBouncePad(new Vector3(normX * 32f + normZ * 9f, 0f, normZ * 32f - normX * 9f), 21.5f, 14.0f));

                    float bx = normX * 84f, bz = normZ * 84f;
                    SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(bx, bHeight, bz), 16.5f, 2.2f, Rgb(210, 222, 240), bCol);
                    entitiesList.Add(new MoonPillar(new Vector3(bx, bHeight, bz), pType));
                    Physics.Obstacles.Add(new ObstacleCircle(bx, bz, 1.6f, bHeight + 5.5f, bHeight));
                }

                // Небесный Колизей Митрикса на высоте Y = 120.0
                var colosseum = new Entity(position: new(0, Physics.ColosseumY, 0));
                new Entity(parent: colosseum, model: "cube", scale: new(Physics.ColosseumRadius * 1.85f, 3.0f, Physics.ColosseumRadius * 1.85f), position: new(0, -1.5f, 0), color: Rgb(195, 208, 228));
                new Entity(parent: colosseum, model: "sphere", scale: new(Physics.ColosseumRadius * 2.0f, 2.2f, Physics.ColosseumRadius * 2.0f), position: new(0, -1.0f, 0), color: Rgb(220, 230, 246));
                new Entity(parent: colosseum, model: "diamond", scale: new(Physics.ColosseumRadius * 2.04f, 0.35f, Physics.ColosseumRadius * 2.04f), position: new(0, -0.02f, 0), color: Rgb(70, 205, 255), unlit: true);

                for (int i = 0; i < 16; i++)
                {
                    float ang = (i / 16.0f) * MathF.PI * 2f;
                    float cx = MathF.Cos(ang) * (Physics.ColosseumRadius - 2.0f);
                    float cz = MathF.Sin(ang) * (Physics.ColosseumRadius - 2.0f);
                    new Entity(parent: colosseum, model: "cube", scale: new(2.6f, 15.0f, 2.6f), position: new(cx, 7.5f, cz), color: Rgb(50, 62, 86));
                    Physics.Obstacles.Add(new ObstacleCircle(cx, cz, 1.6f, Physics.ColosseumY + 15.0f, Physics.ColosseumY));
                }

                entitiesList.Add(colosseum);
                Physics.SkyPlatforms.Add(new SkyPlatform(0f, 0f, Physics.ColosseumRadius + 1.0f, Physics.ColosseumY));

                for (int i = 0; i < 4; i++)
                {
                    float ang = (i * 90f + 45f) * (MathF.PI / 180f);
                    entitiesList.Add(new LunarBouncePad(new Vector3(MathF.Cos(ang) * 38f, Physics.ColosseumY, MathF.Sin(ang) * 38f), 19.0f, 0f));
                }

                return (entitiesList, jumpPad, palette);
            }

            // ==========================================================
            // РЕЖИМ 3: ОБЫЧНЫЕ ЭТАПЫ PETRICHOR V
            // ==========================================================
            string[] structTypes = { "arch", "monolith", "crystals", "platform" };
            for (int i = 0; i < 28; i++)
            {
                float px = NextRange(-50f, 50f), pz = NextRange(-50f, 50f);
                if (MathF.Abs(px) < 10f && MathF.Abs(pz) < 10f) continue;
                string st = structTypes[rng.Next(structTypes.Length)];
                var (sEnt, localObs) = WorldStructuresBuilder.BuildMapStructure(null, st, palette, new Vector3(px, 0, pz), NextRange(0f, 360f), rng);
                entitiesList.Add(sEnt);
                Physics.Obstacles.AddRange(localObs);
            }

            int chestCost = (int)(25 * MathF.Pow(1.25f, stageNum - 1));
            var chestPlan = new List<string>();
            for (int i = 0; i < 9; i++) chestPlan.Add("small");
            for (int i = 0; i < 3; i++) chestPlan.Add("large");
            chestPlan.Add("legendary");
            chestPlan.Add("lunar_pod");
            if (rng.NextDouble() < 0.45) chestPlan.Add("lunar_pod");

            for (int idx = 0; idx < chestPlan.Count; idx++)
            {
                float cx = NextRange(-46f, 46f), cz = NextRange(-46f, 46f);
                if (MathF.Abs(cx) < 6f && MathF.Abs(cz) < 6f) cx += 12f;
                entitiesList.Add(new Chest(new Vector3(cx, 0, cz), chestCost, idx, chestPlan[idx]));
                Physics.Obstacles.Add(new ObstacleCircle(cx, cz, 0.9f, 1.0f));
            }

            for (int i = 0; i < 2; i++)
            {
                float sx = NextRange(-36f, 36f), sz = NextRange(-36f, 36f);
                entitiesList.Add(new HealingShrine(new Vector3(sx, 0, sz), (int)(chestCost * 0.8f)));
                Physics.Obstacles.Add(new ObstacleCircle(sx, sz, 1.1f, 3.2f));
            }

            entitiesList.Add(new NewtAltar(new Vector3(NextRange(-34f, 34f), 0, NextRange(-34f, 34f))));

            float tpAngle = NextRange(0f, MathF.PI * 2f);
            Vector3 tpPos = new(MathF.Cos(tpAngle) * 35f, 0, MathF.Sin(tpAngle) * 35f);
            var tp = new Teleporter(tpPos);
            entitiesList.Add(tp);
            foreach (float a in new[] { 0f, 90f, 180f, 270f })
            {
                float r = a * (MathF.PI / 180f);
                Physics.Obstacles.Add(new ObstacleCircle(tpPos.X + MathF.Cos(r) * 2.2f, tpPos.Z + MathF.Sin(r) * 2.2f, 0.65f, 3.2f));
            }

            return (entitiesList, tp, palette);
        }
    }
}