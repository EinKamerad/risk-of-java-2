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
            Color cDark = Rgb(
                (byte)Math.Max(cMain.R - 32, 16),
                (byte)Math.Max(cMain.G - 32, 16),
                (byte)Math.Max(cMain.B - 26, 20));
            float physThick = MathF.Max(2.2f, thickness * 1.6f);
            var plat = new Entity(position: pos);
            // Два восьмигранных яруса разнесены по высоте и толщине, чтобы полностью исключить Z-fighting ("рябь")!
            new Entity(parent: plat, model: "cube", scale: new(radius * 1.54f, physThick * 0.72f, radius * 1.54f), position: new(0, -physThick * 0.42f, 0), color: cDark);
            new Entity(parent: plat, model: "cube", scale: new(radius * 1.48f, physThick * 0.64f, radius * 1.48f), position: new(0, -physThick * 0.48f, 0), rotation: new(0, 45f, 0), color: cMain);
            new Entity(parent: plat, model: "sphere", scale: new(radius * 2f, thickness, radius * 2f), position: new(0, -thickness * 0.34f, 0), color: cMain);
            new Entity(parent: plat, model: "diamond", scale: new(radius * 2.06f, 0.22f, radius * 2.06f), position: new(0, -0.04f, 0), color: cAcc, unlit: true);
            entitiesList.Add(plat);
            skyPlatforms.Add(new SkyPlatform(pos.X, pos.Z, radius, pos.Y, physThick));
            return plat;
        }

        private static string[] GetBiomeStructures(string biomeId) => biomeId switch
        {
            "ionia" => new[] { "torii", "sakura_tree", "sakura_tree", "arch", "platform", "crystals" },
            "sundered" => new[] { "giant_mushroom", "giant_mushroom", "swamp_root", "crystals", "monolith", "platform" },
            "wetland" => new[] { "swamp_root", "swamp_root", "giant_mushroom", "ruined_tower", "arch", "platform" },
            "rallypoint" => new[] { "ice_spire", "ice_spire", "ruined_tower", "platform", "monolith", "crystals" },
            "sulfur" => new[] { "sulfur_geyser", "sulfur_geyser", "crystals", "monolith", "arch", "platform" },
            "aqueduct" => new[] { "aqueduct_pillar", "aqueduct_pillar", "sun_temple", "arch", "monolith", "platform" },
            "aphelian" => new[] { "sun_temple", "sun_temple", "arch", "monolith", "platform", "astral_ring" },
            "meadow" => new[] { "astral_ring", "astral_ring", "crystals", "monolith", "arch", "platform" },
            "siren" => new[] { "astral_ring", "crystals", "crystals", "arch", "monolith", "ruined_tower" },
            "depths" => new[] { "crystals", "crystals", "monolith", "sulfur_geyser", "ruined_tower", "arch" },
            "acres" => new[] { "monolith", "sun_temple", "ruined_tower", "arch", "platform", "crystals" },
            _ => new[] { "arch", "monolith", "ruined_tower", "crystals", "platform", "astral_ring" }
        };

        public static (List<Entity> Entities, Entity? TeleporterOrPad, StagePalette Palette) BuildStageWorld(
            int stageNum, int seed, string? specialMode = null)
        {
            var entitiesList = new List<Entity>();
            Physics.Obstacles.Clear();
            Physics.SkyPlatforms.Clear();
            Physics.SpecialMode = specialMode;
            Physics.WorldBound = specialMode == "moon" ? 118.0f : (specialMode is "bazaar" or "pvp" ? 58.0f : 86.0f);

            StagePalette palette = specialMode switch
            {
                "bazaar" => GameData.SpecialPalettes["bazaar"],
                "moon" => GameData.SpecialPalettes["moon"],
                "pvp" => GameData.SpecialPalettes["pvp"],
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

            float groundSize = specialMode == "moon" ? 260f : (specialMode is "bazaar" or "pvp" ? 136f : 220f);
            var ground = new Entity(model: "cube", scale: new(groundSize, 2f, groundSize), position: new(0, -1.0f, 0), color: palette.Ground);
            entitiesList.Add(ground);

            // ==========================================================
            // РЕЖИМ 1: БАЗАР МЕЖДУ ВРЕМЕНЕМ (BAZAAR BETWEEN TIME)
            // ==========================================================
            if (specialMode == "pvp")
            {
                int pvpChestCost = 28;
                int pvpChestId = 300;
                SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(0, 3.2f, 0), 11.5f, 1.5f, palette.Ruins, palette.Accent);
                entitiesList.Add(new Chest(new Vector3(0, 3.2f, 0), pvpChestCost, pvpChestId++, "large"));

                foreach (var (bx, bz) in new[] { (-26f, -26f), (26f, -26f), (-26f, 26f), (26f, 26f) })
                {
                    SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(bx, 2.5f, bz), 7.5f, 1.2f, palette.Ruins, palette.Accent);
                    entitiesList.Add(new Chest(new Vector3(bx, 2.5f, bz), pvpChestCost, pvpChestId++, "small"));
                    var (cEnt, cObs) = WorldStructuresBuilder.BuildMapStructure(null, "crystals", palette, new Vector3(bx * 0.65f, 0, bz * 0.65f), 45f, rng);
                    entitiesList.Add(cEnt);
                    Physics.Obstacles.AddRange(cObs);
                    entitiesList.Add(new LunarBouncePad(new Vector3(bx * 0.5f, 0f, 0f), 17.5f, 9.0f));
                }
                entitiesList.Add(new HealingShrine(new Vector3(0, 0, -22f), 25));
                entitiesList.Add(new HealingShrine(new Vector3(0, 0, 22f), 25));

                for (int i = 0; i < 8; i++)
                {
                    float ang = i * (MathF.PI * 0.25f);
                    string st = i % 2 == 0 ? "arch" : "monolith";
                    var (sEnt, localObs) = WorldStructuresBuilder.BuildMapStructure(
                        null, st, palette, new Vector3(MathF.Cos(ang) * 36f, 0, MathF.Sin(ang) * 36f), i * 45f, rng);
                    entitiesList.Add(sEnt);
                    Physics.Obstacles.AddRange(localObs);
                }
                return (entitiesList, null, palette);
            }

            if (specialMode == "bazaar")
            {
                var newt = new Entity(position: new(0, 0, 22));
                new Entity(parent: newt, model: "sphere", scale: new(6.5f, 5.2f, 7.5f), position: new(0, 2.6f, 0), color: Rgb(45, 95, 165));
                new Entity(parent: newt, model: "sphere", scale: new(4.2f, 3.8f, 4.8f), position: new(0, 5.0f, 2.8f), color: Rgb(55, 115, 195));
                foreach (float ex in new[] { -1.2f, 1.2f })
                    new Entity(parent: newt, model: "sphere", scaleUniform: 0.65f, position: new(ex, 5.6f, 4.8f), color: Color.SkyBlue, unlit: true);
                entitiesList.Add(newt);
                Physics.Obstacles.Add(new ObstacleCircle(0f, 22f, 3.4f, 6.8f, 0f));

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

                int mChestCost = (int)(28 * MathF.Pow(1.38f, Math.Max(0, stageNum - 1)));
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
                        if (sIdx == 2 && !GameData.HasArtifact("sacrifice"))
                        {
                            string cType = (dx + dz == 0) ? "large" : "small";
                            entitiesList.Add(new Chest(new Vector3(px, py, pz), mChestCost, chestIdCounter++, cType));
                        }
                    }

                    entitiesList.Add(new LunarBouncePad(new Vector3(normX * 32f + normZ * 9f, 0f, normZ * 32f - normX * 9f), 21.5f, 14.0f));

                    float bx = normX * 84f, bz = normZ * 84f;
                    SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(bx, bHeight, bz), 16.5f, 2.2f, Rgb(210, 222, 240), bCol);
                    entitiesList.Add(new MoonPillar(new Vector3(bx, bHeight, bz), pType));
                    Physics.Obstacles.Add(new ObstacleCircle(bx, bz, 1.6f, 5.5f, bHeight));
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
                    Physics.Obstacles.Add(new ObstacleCircle(cx, cz, 1.6f, 15.0f, Physics.ColosseumY));
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
            // РЕЖИМ 3: ОБЫЧНЫЕ ЭТАПЫ PETRICHOR V (УВЕЛИЧЕННЫЕ И МНОГОУРОВНЕВЫЕ)
            // ==========================================================
            Color darkGround = Rgb(
                (byte)Math.Max(palette.Ground.R - 14, 8),
                (byte)Math.Max(palette.Ground.G - 14, 8),
                (byte)Math.Max(palette.Ground.B - 12, 10));
            Color lightRuins = Rgb(
                (byte)Math.Min(palette.Ruins.R + 18, 255),
                (byte)Math.Min(palette.Ruins.G + 18, 255),
                (byte)Math.Min(palette.Ruins.B + 16, 255));

            // 1. Окаймляющие утёсы и скальные массивы по периметру карты (за границей WorldBound)
            for (int i = 0; i < 32; i++)
            {
                float ang = (i / 32.0f) * MathF.PI * 2f + NextRange(-0.05f, 0.05f);
                float dist = NextRange(90f, 102f);
                float ch = NextRange(9.0f, 22.0f);
                float cw = NextRange(12.0f, 22.0f);
                var cliff = new Entity(position: new(MathF.Cos(ang) * dist, 0, MathF.Sin(ang) * dist), rotation: new(0, NextRange(0, 360f), 0));
                new Entity(parent: cliff, model: "cube", scale: new(cw, ch, cw * 0.75f), position: new(0, ch * 0.45f, 0), color: darkGround);
                new Entity(parent: cliff, model: "diamond", scale: new(cw * 0.85f, ch * 0.55f, cw * 0.65f), position: new(0, ch * 0.88f, 0), color: palette.Ruins);
                if (i % 3 == 0)
                    new Entity(parent: cliff, model: "diamond", scale: new(2.2f, 5.5f, 2.2f), position: new(0, ch + 1.5f, 0), color: palette.Accent, unlit: true);
                entitiesList.Add(cliff);
            }

            // 2. Многоуровневые возвышенности (террасы, плато, ступенчатые подъёмы и прыжковые гейзеры) в 4 секторах карты
            (float qx, float qz)[] quadrants = { (-46f, -44f), (46f, -44f), (-44f, 46f), (46f, 46f) };
            for (int q = 0; q < quadrants.Length; q++)
            {
                float bx = quadrants[q].qx + NextRange(-6f, 6f);
                float bz = quadrants[q].qz + NextRange(-6f, 6f);
                float tier1Y = 2.6f;
                float tier2Y = 5.4f;
                float tier3Y = 8.4f;

                // Ярус 1 — широкое нижнее плато
                SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(bx, tier1Y, bz), 14.5f, 1.6f, palette.Ruins, palette.Accent);
                // Ярус 2 — средняя терраса со смещением
                float t2x = bx + (q % 2 == 0 ? 6.5f : -6.5f);
                float t2z = bz + (q < 2 ? 6.5f : -6.5f);
                SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(t2x, tier2Y, t2z), 10.5f, 1.5f, lightRuins, palette.Accent);
                // Ярус 3 — вершина утёса
                float t3x = t2x + (q % 2 == 0 ? 4.5f : -4.5f);
                float t3z = t2z + (q < 2 ? -4.0f : 4.0f);
                SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(t3x, tier3Y, t3z), 7.2f, 1.4f, palette.Ruins, palette.Accent);

                // Ступенчатый подъём с земли на 1-й ярус (доступен обычным шагом и прыжком)
                Vector3 dirToCenter = Vector3.Normalize(new Vector3(-bx, 0, -bz));
                SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(bx, 1.3f, bz) + dirToCenter * 15.0f, 4.8f, 0.9f, darkGround, palette.Accent);

                // Гейзер-трамплин у подножия для мгновенного взлёта на верхние ярусы
                Vector3 padPos = new Vector3(bx, 0f, bz) + dirToCenter * 19.5f + new Vector3(dirToCenter.Z * 5f, 0, -dirToCenter.X * 5f);
                entitiesList.Add(new LunarBouncePad(padPos, 19.5f, 9.5f));
            }

            // 3. Небесный мост / архипелаг парящих островов между секторами (высота Y = 6.5 .. 11.2)
            float bridgeAngle = NextRange(0f, MathF.PI);
            Vector3 bridgeDir = new(MathF.Cos(bridgeAngle), 0, MathF.Sin(bridgeAngle));
            for (int s = -2; s <= 2; s++)
            {
                float dist = s * 13.5f;
                float py = 7.2f + (2 - Math.Abs(s)) * 1.6f;
                Vector3 discPos = bridgeDir * dist + new Vector3(-bridgeDir.Z * 22f, py, bridgeDir.X * 22f);
                SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, discPos, s == 0 ? 7.8f : 4.8f, 1.1f, lightRuins, palette.Accent);
            }

            // 4. Биомные структуры и архитектурные объекты (44 штуки с учётом высоты рельефа)
            string[] structTypes = GetBiomeStructures(palette.Id);
            for (int i = 0; i < 44; i++)
            {
                float px = NextRange(-76f, 76f), pz = NextRange(-76f, 76f);
                if (MathF.Abs(px) < 11f && MathF.Abs(pz) < 11f) continue;
                float surfY = Physics.GetSurfaceHeight(px, pz, 12f);
                string st = structTypes[rng.Next(structTypes.Length)];
                var (sEnt, localObs) = WorldStructuresBuilder.BuildMapStructure(null, st, palette, new Vector3(px, surfY, pz), NextRange(0f, 360f), rng);
                entitiesList.Add(sEnt);
                Physics.Obstacles.AddRange(localObs);
            }

            // 5. Малые декорации окружения (валуны, светящиеся растения/кристаллы, рунные столбики — 48 штук без Z-fighting)
            for (int i = 0; i < 48; i++)
            {
                float dx = NextRange(-78f, 78f), dz = NextRange(-78f, 78f);
                if (MathF.Abs(dx) < 8f && MathF.Abs(dz) < 8f) continue;
                float dy = Physics.GetSurfaceHeight(dx, dz, 12f);
                var prop = new Entity(position: new(dx, dy, dz), rotation: new(0, NextRange(0f, 360f), 0));
                int kind = i % 3;
                if (kind == 0)
                {
                    // Каменный валун со мхом / снегом / кристаллами
                    float r = NextRange(0.9f, 1.8f);
                    new Entity(parent: prop, model: "sphere", scale: new(r * 1.4f, r * 0.9f, r * 1.2f), position: new(0, r * 0.35f, 0), color: palette.Ruins);
                    new Entity(parent: prop, model: "diamond", scale: new(r * 0.45f, r * 0.95f, r * 0.45f), position: new(r * 0.25f, r * 0.75f, 0), rotation: new(15f, 0, -18f), color: palette.Accent, unlit: true);
                    Physics.Obstacles.Add(new ObstacleCircle(dx, dz, r * 0.68f, r * 0.95f, dy));
                }
                else if (kind == 1)
                {
                    // Древний дорожный фонарь / рунный столбик
                    float h = NextRange(1.6f, 2.6f);
                    new Entity(parent: prop, model: "cube", scale: new(0.55f, h, 0.55f), position: new(0, h * 0.5f, 0), color: palette.Ruins);
                    new Entity(parent: prop, model: "cube", scale: new(0.85f, 0.22f, 0.85f), position: new(0, h + 0.11f, 0), color: lightRuins);
                    new Entity(parent: prop, model: "sphere", scaleUniform: 0.42f, position: new(0, h + 0.45f, 0), color: palette.Accent, unlit: true);
                    Physics.Obstacles.Add(new ObstacleCircle(dx, dz, 0.55f, h + 0.65f, dy));
                }
                else
                {
                    // Биолюминесцентная флора / куст кристаллов
                    for (int b = 0; b < 3; b++)
                    {
                        float ang = b * 120f * (MathF.PI / 180f);
                        new Entity(parent: prop, model: "diamond", scale: new(0.32f, NextRange(0.8f, 1.4f), 0.32f),
                            position: new(MathF.Cos(ang) * 0.35f, 0.45f, MathF.Sin(ang) * 0.35f),
                            rotation: new(20f, b * 120f, 0), color: palette.Accent, unlit: true);
                    }
                    Physics.Obstacles.Add(new ObstacleCircle(dx, dz, 0.65f, 1.4f, dy));
                }
                entitiesList.Add(prop);
            }

            // 6. Интерактивные объекты: увеличенное число сундуков (включая сундуки на возвышенностях!), алтарей и тритонов
            int chestCost = (int)(28 * MathF.Pow(1.38f, stageNum - 1));
            var chestPlan = new List<string>();
            if (!GameData.HasArtifact("sacrifice"))
            {
                int smallCount = 9 + (rng.NextDouble() < 0.60 ? 2 : 1);
                for (int i = 0; i < smallCount; i++) chestPlan.Add("small");
                int largeCount = 3 + (rng.NextDouble() < 0.45 ? 1 : 0);
                for (int i = 0; i < largeCount; i++) chestPlan.Add("large");
                if (stageNum % 2 == 0 || rng.NextDouble() < 0.40) chestPlan.Add("legendary");
            }
            if (rng.NextDouble() < 0.65) chestPlan.Add("lunar_pod");

            for (int idx = 0; idx < chestPlan.Count; idx++)
            {
                float cx = NextRange(-70f, 70f), cz = NextRange(-70f, 70f);
                if (MathF.Abs(cx) < 8f && MathF.Abs(cz) < 8f) cx += 15f;
                float cy = Physics.GetSurfaceHeight(cx, cz, 12f);
                entitiesList.Add(new Chest(new Vector3(cx, cy, cz), chestCost, idx, chestPlan[idx]));
                Physics.Obstacles.Add(new ObstacleCircle(cx, cz, 0.9f, 1.0f, cy));
            }

            for (int i = 0; i < 3; i++)
            {
                float sx = NextRange(-58f, 58f), sz = NextRange(-58f, 58f);
                float sy = Physics.GetSurfaceHeight(sx, sz, 12f);
                entitiesList.Add(new HealingShrine(new Vector3(sx, sy, sz), (int)(chestCost * 0.8f)));
                Physics.Obstacles.Add(new ObstacleCircle(sx, sz, 1.1f, 3.2f, sy));
            }

            for (int i = 0; i < 2; i++)
            {
                float nx = NextRange(-62f, 62f), nz = NextRange(-62f, 62f);
                float ny = Physics.GetSurfaceHeight(nx, nz, 12f);
                entitiesList.Add(new NewtAltar(new Vector3(nx, ny, nz)));
            }

            float tpAngle = NextRange(0f, MathF.PI * 2f);
            float tpDist = NextRange(44f, 58f);
            float tpx = MathF.Cos(tpAngle) * tpDist;
            float tpz = MathF.Sin(tpAngle) * tpDist;
            float tpy = Physics.GetSurfaceHeight(tpx, tpz, 12f);
            Vector3 tpPos = new(tpx, tpy, tpz);
            var tp = new Teleporter(tpPos);
            entitiesList.Add(tp);
            foreach (float a in new[] { 0f, 90f, 180f, 270f })
            {
                float r = a * (MathF.PI / 180f);
                Physics.Obstacles.Add(new ObstacleCircle(tpPos.X + MathF.Cos(r) * 2.2f, tpPos.Z + MathF.Sin(r) * 2.2f, 0.65f, 3.2f, tpy));
            }

            return (entitiesList, tp, palette);
        }
    }
}