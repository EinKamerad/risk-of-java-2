using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using RiskOfCSharp2.Entities;
using RiskOfCSharp2.Models;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.World
{
    public static partial class StageBuilder
    {
        public static readonly List<Entity> GroundTiles = new();
        public static Color CurrentGroundColor = Rgb(48, 56, 52);

        public static bool DigFloorHole(Vector3 worldPos, float holeSize = 4.0f)
        {
            if (Physics.SpecialMode is "moon" or "bazaar" or "safe_travels")
                return false;

            var ctx = WorldContext.Game;
            // Не ломаем пол прямо под телепортом, чтобы не заблокировать зарядку
            if (ctx != null)
            {
                foreach (var ent in ctx.StageEntities)
                {
                    if (ent is Teleporter tp && IsValid(tp) && Physics.Distance2D(worldPos, tp.Position) < 4.8f)
                        return false;
                }
            }

            float gx = MathF.Round(worldPos.X / holeSize) * holeSize;
            float gz = MathF.Round(worldPos.Z / holeSize) * holeSize;
            float half = holeSize * 0.5f;
            float hMinX = gx - half, hMaxX = gx + half;
            float hMinZ = gz - half, hMaxZ = gz + half;

            for (int i = 0; i < Physics.FloorHoles.Count; i++)
            {
                var fh = Physics.FloorHoles[i];
                if (MathF.Abs((fh.MinX + fh.MaxX) * 0.5f - gx) < 0.2f &&
                    MathF.Abs((fh.MinZ + fh.MaxZ) * 0.5f - gz) < 0.2f)
                    return false;
            }

            Physics.FloorHoles.Add(new FloorHole(hMinX, hMaxX, hMinZ, hMaxZ, -2.4f));

            // Разрезаем все пересекающиеся плиты пола (2D box-splitting), чтобы кусок пола реально исчезал!
            var newTiles = new List<Entity>();
            for (int i = GroundTiles.Count - 1; i >= 0; i--)
            {
                var tile = GroundTiles[i];
                if (!IsValid(tile))
                {
                    GroundTiles.RemoveAt(i);
                    continue;
                }

                float tMinX = tile.X - tile.Scale.X * 0.5f;
                float tMaxX = tile.X + tile.Scale.X * 0.5f;
                float tMinZ = tile.Z - tile.Scale.Z * 0.5f;
                float tMaxZ = tile.Z + tile.Scale.Z * 0.5f;

                if (hMaxX <= tMinX + 0.02f || hMinX >= tMaxX - 0.02f ||
                    hMaxZ <= tMinZ + 0.02f || hMinZ >= tMaxZ - 0.02f)
                    continue;

                Color col = tile.Color;
                float ty = tile.Y;
                float sy = tile.Scale.Y;
                GroundTiles.RemoveAt(i);
                ctx?.StageEntities.Remove(tile);
                Destroy(tile);

                float cMinX = MathF.Max(tMinX, hMinX);
                float cMaxX = MathF.Min(tMaxX, hMaxX);
                float cMinZ = MathF.Max(tMinZ, hMinZ);
                float cMaxZ = MathF.Min(tMaxZ, hMaxZ);

                void AddSubTile(float x0, float x1, float z0, float z1)
                {
                    float w = x1 - x0, d = z1 - z0;
                    if (w < 0.08f || d < 0.08f) return;
                    var sub = new Entity(model: "cube", scale: new(w, sy, d), position: new((x0 + x1) * 0.5f, ty, (z0 + z1) * 0.5f), color: col);
                    newTiles.Add(sub);
                    ctx?.StageEntities.Add(sub);
                }

                // Левая и правая полосы по X
                AddSubTile(tMinX, cMinX, tMinZ, tMaxZ);
                AddSubTile(cMaxX, tMaxX, tMinZ, tMaxZ);
                // Передняя и задняя полосы по Z внутри центральной колонки
                AddSubTile(cMinX, cMaxX, tMinZ, cMinZ);
                AddSubTile(cMinX, cMaxX, cMaxZ, tMaxZ);
            }
            GroundTiles.AddRange(newTiles);

            // Строим каменную шахту ямы и дно на глубине Y = -2.4
            Color wallCol = Rgb(
                (byte)Math.Max(CurrentGroundColor.R - 22, 14),
                (byte)Math.Max(CurrentGroundColor.G - 22, 14),
                (byte)Math.Max(CurrentGroundColor.B - 18, 18));
            Color deepCol = Rgb(22, 24, 30);
            var pitRoot = new Entity(position: new(gx, 0f, gz));
            new Entity(parent: pitRoot, model: "cube", scale: new(holeSize, 2.4f, 0.22f), position: new(0, -1.55f, -half + 0.11f), color: wallCol);
            new Entity(parent: pitRoot, model: "cube", scale: new(holeSize, 2.4f, 0.22f), position: new(0, -1.55f, half - 0.11f), color: wallCol);
            new Entity(parent: pitRoot, model: "cube", scale: new(0.22f, 2.4f, holeSize), position: new(-half + 0.11f, -1.55f, 0), color: wallCol);
            new Entity(parent: pitRoot, model: "cube", scale: new(0.22f, 2.4f, holeSize), position: new(half - 0.11f, -1.55f, 0), color: wallCol);
            new Entity(parent: pitRoot, model: "cube", scale: new(holeSize + 0.3f, 0.4f, holeSize + 0.3f), position: new(0, -2.60f, 0), color: deepCol);
            ctx?.StageEntities.Add(pitRoot);

            // Вылетающие воксельные обломки блока
            for (int b = 0; b < 8; b++)
            {
                float ang = b * (MathF.PI * 0.25f);
                new VisualFX(new Vector3(gx + MathF.Cos(ang) * 0.8f, 0.3f, gz + MathF.Sin(ang) * 0.8f), CurrentGroundColor, 0.45f, 0.38f);
            }
            return true;
        }

        public static bool TryMineWorld(
            Vector3 origin, Vector3 aimDir,
            out int wood, out int stone, out int iron, out int diamond,
            out string msg, bool allowFloorDig = true)
        {
            msg = "";
            wood = 0; stone = 0; iron = 0; diamond = 0;
            if (Physics.SpecialMode == "safe_travels") return false;
            var ctx = WorldContext.Game;
            if (ctx == null) return false;

            Vector3 flatAim = new(aimDir.X, 0f, aimDir.Z);
            if (flatAim.LengthSquared() < 0.001f) flatAim = Vector3.UnitZ;
            else flatAim = Vector3.Normalize(flatAim);

            // Если Стив смотрит сильно вниз — сразу ломаем пол перед собой
            if (aimDir.Y > -0.52f)
            {
                Vector3 probe = origin + flatAim * 3.0f;

                // 1. Алтари, святилища, тритоны, сундуки и котлы
                for (int i = ctx.StageEntities.Count - 1; i >= 0; i--)
                {
                    var ent = ctx.StageEntities[i];
                    if (!IsValid(ent)) continue;
                    if (ent is Teleporter or MinecraftWorkbench or LunarJumpPad or BluePortal or SecretStagePortal
                        or CharacterQuestMonument or QuestSecondaryNode or SafeTravelsNpc or SafeTravelsRoomProp
                        or SafeTravelsDpsConsole or SafeTravelsSandboxStation or SafeTravelsDropPod or SafeTravelsExitAirlock) continue;

                    float d = Physics.Distance2D(probe, ent.Position);
                    if (ent is HealingShrine hs && d < 3.6f)
                    {
                        RemoveObstaclesNear(hs.Position, 2.2f);
                        ctx.StageEntities.RemoveAt(i);
                        Destroy(hs);
                        stone = 4; iron = 3; diamond = 1;
                        msg = "Алтарь исцеления добыт! (+4 Камня, +3 Железа, +1 Алмаз)";
                        return true;
                    }
                    if (ent is NewtAltar na && d < 3.6f)
                    {
                        RemoveObstaclesNear(na.Position, 2.0f);
                        ctx.StageEntities.RemoveAt(i);
                        Destroy(na);
                        stone = 3; diamond = 2;
                        msg = "Алтарь Тритона разбит киркой! (+3 Камня, +2 Алмаза)";
                        return true;
                    }
                    if (ent is LunarCauldron lc && d < 3.6f)
                    {
                        RemoveObstaclesNear(lc.Position, 2.2f);
                        ctx.StageEntities.RemoveAt(i);
                        Destroy(lc);
                        stone = 5; iron = 3; diamond = 2;
                        msg = "Лунный котёл добыт! (+5 Камня, +3 Железа, +2 Алмаза)";
                        return true;
                    }
                    if (ent is Chest ch && d < 3.4f)
                    {
                        RemoveObstaclesNear(ch.Position, 1.8f);
                        ctx.StageEntities.RemoveAt(i);
                        Destroy(ch);
                        wood = 3; iron = 2;
                        if (!ch.Opened)
                        {
                            diamond = 1;
                            ctx.Gold += Math.Max(15, ch.Cost / 2);
                            msg = "Сундук срублен! (+3 Дерева, +2 Железа, +1 Алмаз и золото)";
                        }
                        else
                        {
                            msg = "Пустой сундук разобран! (+3 Дерева, +2 Железа)";
                        }
                        return true;
                    }
                }

                // 2. Стены, колонны, деревья, кристаллы и скалы (Obstacles + StageEntities)
                int hitObsIdx = -1;
                float bestObsDist = 4.2f;
                for (int i = 0; i < Physics.Obstacles.Count; i++)
                {
                    var obs = Physics.Obstacles[i];
                    float dx = probe.X - obs.X, dz = probe.Z - obs.Z;
                    float dist = MathF.Sqrt(dx * dx + dz * dz) - obs.Radius;
                    if (dist < bestObsDist && MathF.Abs(origin.Y - obs.BaseY) < 6.0f)
                    {
                        bestObsDist = dist;
                        hitObsIdx = i;
                    }
                }

                if (hitObsIdx >= 0)
                {
                    var hitObs = Physics.Obstacles[hitObsIdx];
                    Vector3 obsPos = new(hitObs.X, hitObs.BaseY, hitObs.Z);

                    // Ищем родительскую структуру на сцене и сносим её целиком вместе с коллизией
                    Entity? targetStruct = null;
                    float bestEntDist = 7.5f;
                    foreach (var ent in ctx.StageEntities)
                    {
                        if (!IsValid(ent) || GroundTiles.Contains(ent)) continue;
                        if (ent is Teleporter or MinecraftWorkbench or LunarJumpPad or BluePortal or LunarBouncePad or MoonPillar
                            or SecretStagePortal or CharacterQuestMonument or QuestSecondaryNode or SafeTravelsNpc or SafeTravelsRoomProp
                            or SafeTravelsDpsConsole or SafeTravelsSandboxStation or SafeTravelsDropPod or SafeTravelsExitAirlock) continue;
                        if (ent.Scale.X > 60f) continue; // Не удаляем скайбокс или колизей
                        float d = Physics.Distance2D(obsPos, ent.Position);
                        if (d < bestEntDist)
                        {
                            bestEntDist = d;
                            targetStruct = ent;
                        }
                    }

                    if (targetStruct != null)
                    {
                        RemoveObstaclesNear(targetStruct.Position, 6.2f);
                        ctx.StageEntities.Remove(targetStruct);
                        Destroy(targetStruct);
                    }
                    else
                    {
                        Physics.Obstacles.RemoveAt(hitObsIdx);
                    }

                    for (int b = 0; b < 8; b++)
                    {
                        float ang = b * (MathF.PI * 0.25f);
                        new VisualFX(obsPos + new Vector3(MathF.Cos(ang), 1.2f, MathF.Sin(ang)), Rgb(180, 175, 165), 0.5f, 0.38f);
                    }

                    wood = 2 + Random.Shared.Next(0, 3);
                    stone = 3 + Random.Shared.Next(0, 4);
                    iron = Random.Shared.NextDouble() < 0.65 ? 2 : 1;
                    diamond = Random.Shared.NextDouble() < 0.28 ? 1 : 0;
                    msg = $"Структура разрушена! (+{wood} Дерева, +{stone} Камня, +{iron} Железа{(diamond > 0 ? $", +{diamond} Алмаз!" : "")})";
                    return true;
                }
            }

            // 3. Копаем пол прямо перед Стивом (создаёт реальную 3D-яму в полу!)
            if (allowFloorDig || aimDir.Y <= -0.35f)
            {
                Vector3 digPos = origin + flatAim * 3.4f;
                if (DigFloorHole(digPos, 4.0f))
                {
                    wood = 1;
                    stone = 3;
                    iron = Random.Shared.NextDouble() < 0.50 ? 1 : 0;
                    diamond = Random.Shared.NextDouble() < 0.18 ? 1 : 0;
                    msg = $"Блок пола выкопан — создана яма! (+1 Дерево, +{stone} Камня{(iron > 0 ? ", +1 Железо" : "")}{(diamond > 0 ? ", +1 Алмаз!" : "")})";
                    return true;
                }
            }

            return false;
        }

        private static void RemoveObstaclesNear(Vector3 pos, float radius)
        {
            for (int i = Physics.Obstacles.Count - 1; i >= 0; i--)
            {
                var o = Physics.Obstacles[i];
                float dx = o.X - pos.X, dz = o.Z - pos.Z;
                if (MathF.Sqrt(dx * dx + dz * dz) <= radius + o.Radius * 0.5f)
                    Physics.Obstacles.RemoveAt(i);
            }
        }

        /// <summary>
        /// Ставит объект с фиксированной «дизайнерской» позицией в ближайшую свободную точку
        /// (не внутри стен, колонн, столов и других интерактивных объектов) и резервирует её.
        /// </summary>
        private static Vector3 PlaceFixed(Vector3 desired, float footprint, float height, float padding = 0.45f)
        {
            Vector3 p = Physics.FindClearNearbyPos(desired, footprint, height, true, 0f, padding, 18f, 1.5f);
            Physics.RegisterSpawnBlocker(p, footprint, height);
            return p;
        }

        /// <summary>
        /// Случайная свободная точка на кольце [minDist..maxDist] от центра этапа (детерминированно от rng этапа).
        /// Возвращает false, если за maxAttempts попыток место не нашлось.
        /// </summary>
        private static bool TryPlaceRandom(Random rng, float footprint, float height, float minDist, float maxDist,
            out Vector3 pos, float padding = 0.6f, bool allowSky = true, int maxAttempts = 90, bool register = true)
        {
            if (Physics.TryFindFreeSpawnPos(rng, footprint, height, out pos, minDist, maxDist, allowSky, padding, 2.5f, maxAttempts))
            {
                if (register) Physics.RegisterSpawnBlocker(pos, footprint, height);
                return true;
            }
            return false;
        }
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
            // Полный сброс физики этапа: круглые и прямоугольные коллайдеры, террасы, ямы, блокираторы спавна и круговая граница
            Physics.ResetStagePhysics();
            GroundTiles.Clear();
            Physics.SpecialMode = specialMode;
            Physics.WorldBound = specialMode switch
            {
                "moon" => 118.0f,
                "safe_travels" => 96.0f,
                "tracen_derby" => 68.0f,
                "dark_sanctuary" or "terminal_lucidity" or "extraction_floor" or "originium_core" or "penacony_theater" or "prime_meridian" => 66.0f,
                "bazaar" or "pvp" or "pizzeria" or "black_mesa" or "qliphoth" or "hell_cybergrind" or "tartarus" or "hakurei_shrine" => 62.0f,
                _ => 86.0f
            };

            var rng = new Random(seed);
            float NextRange(float min, float max) => min + (float)rng.NextDouble() * (max - min);

            StagePalette palette = specialMode switch
            {
                "bazaar" => GameData.SpecialPalettes["bazaar"],
                "moon" => GameData.SpecialPalettes["moon"],
                "pvp" => GameData.SpecialPalettes["pvp"],
                "pizzeria" => GameData.SpecialPalettes["pizzeria"],
                "black_mesa" => GameData.SpecialPalettes["black_mesa"],
                "qliphoth" => GameData.SpecialPalettes["qliphoth"],
                "hell_cybergrind" => GameData.SpecialPalettes["hell_cybergrind"],
                "tartarus" => GameData.SpecialPalettes["tartarus"],
                "hakurei_shrine" => GameData.SpecialPalettes["hakurei_shrine"],
                "tracen_derby" => GameData.SpecialPalettes["tracen_derby"],
                "dark_sanctuary" => GameData.SpecialPalettes["dark_sanctuary"],
                "terminal_lucidity" => GameData.SpecialPalettes["terminal_lucidity"],
                "extraction_floor" => GameData.SpecialPalettes["extraction_floor"],
                "originium_core" => GameData.SpecialPalettes["originium_core"],
                "penacony_theater" => GameData.SpecialPalettes["penacony_theater"],
                "prime_meridian" => GameData.SpecialPalettes["prime_meridian"],
                "safe_travels" => GameData.SpecialPalettes["safe_travels"],
                _ => GameData.GetLoopedStagePalette(stageNum, rng)
            };

            if (!ProgressData.Instance.DiscoveredStages.Contains(palette.Id))
            {
                ProgressData.Instance.DiscoveredStages.Add(palette.Id);
                ProgressData.Instance.Save();
            }

            Engine.BackgroundColor = palette.Sky;
            CurrentGroundColor = palette.Ground;
            entitiesList.Add(WorldStructuresBuilder.BuildSkybox(null, palette));

            if (specialMode == "safe_travels")
            {
                BuildSafeTravelsHub(entitiesList, palette);
                return (entitiesList, null, palette);
            }

            float groundSize = specialMode == "moon" ? 260f
                : (specialMode is "tracen_derby" ? 156f
                : (specialMode is "dark_sanctuary" or "terminal_lucidity" or "extraction_floor" or "originium_core" or "penacony_theater" or "prime_meridian" ? 152f
                : (specialMode is "bazaar" or "pvp" or "pizzeria" or "black_mesa" or "qliphoth" or "hell_cybergrind" or "tartarus" or "hakurei_shrine" ? 142f : 220f)));
            var ground = new Entity(model: "cube", scale: new(groundSize, 2f, groundSize), position: new(0, -1.0f, 0), color: palette.Ground);
            entitiesList.Add(ground);
            GroundTiles.Add(ground);

            // ==========================================================
            // РЕЖИМ 1: БАЗАР МЕЖДУ ВРЕМЕНЕМ И PVP АРЕНА
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
                    var (cEnt, cObs) = WorldStructuresBuilder.BuildMapStructure(null, "crystals", palette, new Vector3(bx * 0.65f, 0, bz * 0.65f), 45f, rng, Physics.BoxObstacles);
                    entitiesList.Add(cEnt);
                    Physics.Obstacles.AddRange(cObs);
                    // Трамплины разнесены по диагоналям (раньше по два трамплина спавнились в одной точке)
                    entitiesList.Add(new LunarBouncePad(new Vector3(bx * 0.42f, 0f, bz * 0.42f), 17.5f, 9.0f));
                }
                entitiesList.Add(new HealingShrine(new Vector3(0, 0, -22f), 25));
                entitiesList.Add(new HealingShrine(new Vector3(0, 0, 22f), 25));

                for (int i = 0; i < 8; i++)
                {
                    // Кольцо структур смещено на 22.5°, чтобы монолиты больше не стояли прямо на угловых платформах с сундуками
                    float ang = i * (MathF.PI * 0.25f) + MathF.PI * 0.125f;
                    string st = i % 2 == 0 ? "arch" : "monolith";
                    var (sEnt, localObs) = WorldStructuresBuilder.BuildMapStructure(
                        null, st, palette, new Vector3(MathF.Cos(ang) * 38f, 0, MathF.Sin(ang) * 38f), i * 45f + 22.5f, rng, Physics.BoxObstacles);
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
                Physics.RegisterSpawnBlocker(new Vector3(0f, 0f, 22f), 3.8f, 7.0f);

                var lunarKeys = GameData.LunarItems.Keys.OrderBy(_ => rng.Next()).ToList();
                var voidKeys = GameData.VoidItems.Keys.OrderBy(_ => rng.Next()).ToList();
                string[] offers = { lunarKeys[0], voidKeys[0], lunarKeys[1], voidKeys[1] };
                for (int i = 0; i < offers.Length; i++)
                {
                    Vector3 budPos = PlaceFixed(new Vector3(-12f + i * 8f, 0, 8f), 1.0f, 2.4f);
                    entitiesList.Add(new LunarShopBud(budPos, offers[i], cost: 2));
                }

                entitiesList.Add(new LunarCauldron(PlaceFixed(new Vector3(-10, 0, -6), 1.3f, 2.5f), GameData.RollItemFromPool("green"), reqCount: 3));
                entitiesList.Add(new LunarCauldron(PlaceFixed(new Vector3(10, 0, -6), 1.3f, 2.5f), GameData.RollItemFromPool("red"), reqCount: 5));
                entitiesList.Add(new BluePortal(PlaceFixed(new Vector3(0, 0, -18), 2.0f, 4.0f), "next_stage"));

                // Замёрзший Термальный Кристалл Artificer + скрытые триггеры Пиццерии Fazbear и Сектора C Black Mesa прямо на Базаре
                CharacterUnlockSystems.PopulateBazaarUnlocks(entitiesList);

                // Секретные порталы во все 13 особых измерений испытаний по внешнему кольцу Базара
                (string mode, string[] charKeys, float px, float pz)[] bazaarSecretPortals =
                {
                    ("pizzeria", new[] { "freddy" }, -24f, 14f),
                    ("black_mesa", new[] { "freeman" }, -28f, -2f),
                    ("qliphoth", new[] { "dmc_trio" }, -22f, -18f),
                    ("hell_cybergrind", new[] { "v1" }, 22f, -18f),
                    ("tartarus", new[] { "aigis" }, 28f, -2f),
                    ("hakurei_shrine", new[] { "reimu", "komeiji" }, 24f, 14f),
                    ("tracen_derby", new[] { "gold_ship", "daitaku_helios" }, -15f, 24f),
                    ("dark_sanctuary", new[] { "susie" }, -37f, 22f),
                    ("terminal_lucidity", new[] { "spamton" }, -40f, -12f),
                    ("extraction_floor", new[] { "binah", "don_quixote" }, -14f, -34f),
                    ("originium_core", new[] { "priestess", "closure" }, 14f, -34f),
                    ("penacony_theater", new[] { "robin" }, 40f, -12f),
                    ("prime_meridian", new[] { "false_son" }, 37f, 22f)
                };
                foreach (var (sMode, sChars, spx, spz) in bazaarSecretPortals)
                {
                    bool anyLocked = sChars.Any(sChar =>
                        !ProgressData.Instance.UnlockedAchievements.Contains($"ach_char_{sChar}") &&
                        (sChar == "v1" || !ProgressData.Instance.IsCharacterUnlocked(sChar)));
                    if (anyLocked)
                    {
                        entitiesList.Add(new SecretStagePortal(PlaceFixed(new Vector3(spx, 0f, spz), 1.8f, 4.5f), sMode));
                    }
                }

                return (entitiesList, null, palette);
            }

            if (specialMode is "pizzeria" or "black_mesa" or "qliphoth" or "hell_cybergrind" or "tartarus" or "hakurei_shrine" or "tracen_derby"
                or "dark_sanctuary" or "terminal_lucidity" or "extraction_floor" or "originium_core" or "penacony_theater" or "prime_meridian")
            {
                BuildSecretStageArena(entitiesList, palette, specialMode, stageNum, rng);
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
                        null, "arch", palette, new Vector3(MathF.Cos(rad) * 26f, 0, MathF.Sin(rad) * 26f), angleDeg, rng, Physics.BoxObstacles);
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

            // 1. Окаймляющие утёсы — СПЛОШНАЯ внешняя стена по периметру карты с твёрдыми коллайдерами.
            // Игровая зона — круг радиуса 86; утёсы стоят вплотную к границе внахлёст, без щелей, и каждый имеет
            // прямоугольный коллайдер, точно повторяющий его каменный блок (раньше утёсы были «прозрачными»,
            // а в углах квадратной границы можно было уйти за них).
            const float stageBoundR = 86.0f;
            Physics.WorldBoundRadius = stageBoundR;
            const int cliffCount = 40;
            for (int i = 0; i < cliffCount; i++)
            {
                float ang = (i / (float)cliffCount) * MathF.PI * 2f + NextRange(-0.02f, 0.02f);
                float ch = NextRange(10.0f, 22.0f);
                float cw = NextRange(16.0f, 24.0f);
                float cd = cw * 0.75f;
                float dist = stageBoundR + cd * 0.5f - 0.6f;
                // Локальная ось Z блока направлена радиально (к центру карты), ширина cw — вдоль окружности
                float yaw = 90f - ang * (180f / MathF.PI) + NextRange(-7f, 7f);
                float cx = MathF.Cos(ang) * dist, cz = MathF.Sin(ang) * dist;
                var cliff = new Entity(position: new(cx, 0, cz), rotation: new(0, yaw, 0));
                new Entity(parent: cliff, model: "cube", scale: new(cw, ch, cd), position: new(0, ch * 0.5f, 0), color: darkGround);
                new Entity(parent: cliff, model: "diamond", scale: new(cw * 0.85f, ch * 0.55f, cd * 0.85f), position: new(0, ch * 0.92f, 0), color: palette.Ruins);
                if (i % 3 == 0)
                    new Entity(parent: cliff, model: "diamond", scale: new(2.2f, 5.5f, 2.2f), position: new(0, ch + 1.5f, 0), color: palette.Accent, unlit: true);
                entitiesList.Add(cliff);
                Physics.AddSolidBox(cx, cz, cw, cd, ch + 40f, yaw, 0f, walkableTop: false);
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
                Physics.RegisterSpawnBlocker(padPos, 2.4f, 3.0f);
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

            // Центр этапа (точка высадки игрока) всегда остаётся свободной
            Physics.RegisterSpawnBlocker(Vector3.Zero, 9.0f, 6.0f);

            // 4. ТЕЛЕПОРТ — резервируется первым, на ровной свободной площадке (без деревьев, башен и скал вокруг)
            Vector3 tpPos = Vector3.Zero;
            bool tpPlaced = false;
            for (int attempt = 0; attempt < 140 && !tpPlaced; attempt++)
            {
                float tpAngle = NextRange(0f, MathF.PI * 2f);
                float tpDist = NextRange(42f, 62f);
                float tx = MathF.Cos(tpAngle) * tpDist, tz = MathF.Sin(tpAngle) * tpDist;
                if (Physics.IsSpawnSpotValid(tx, tz, 3.4f, 4.5f, out float ty, allowSkyPlatforms: true, extraPadding: 1.0f, boundMargin: 4.0f))
                {
                    tpPos = new Vector3(tx, ty, tz);
                    tpPlaced = true;
                }
            }
            if (!tpPlaced)
                tpPos = Physics.FindClearNearbyPos(new Vector3(50f, 0f, 0f), 3.4f, 4.5f);
            // Широкая зона вокруг телепорта резервируется под бой с боссом
            Physics.RegisterSpawnBlocker(tpPos, 6.5f, 6.0f);
            var tp = new Teleporter(tpPos);
            entitiesList.Add(tp);
            foreach (float a in new[] { 0f, 90f, 180f, 270f })
            {
                float r = a * (MathF.PI / 180f);
                Physics.Obstacles.Add(new ObstacleCircle(tpPos.X + MathF.Cos(r) * 2.2f, tpPos.Z + MathF.Sin(r) * 2.2f, 0.65f, 3.2f, tpPos.Y));
            }

            // 5. Интерактивные объекты: сундуки, алтари и тритоны — только на свободном твёрдом рельефе / террасах,
            // никогда внутри друг друга, структур, скал, стен или над ямами
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
                string cType = chestPlan[idx];
                float cFp = cType is "legendary" or "lunar_pod" ? 1.35f : (cType == "large" ? 1.2f : 1.0f);
                if (!TryPlaceRandom(rng, cFp, 1.8f, 8f, 78f, out Vector3 cPos, padding: 0.7f))
                    cPos = PlaceFixed(new Vector3(NextRange(-40f, 40f), 0f, NextRange(-40f, 40f)), cFp, 1.8f);
                entitiesList.Add(new Chest(cPos, chestCost, idx, cType));
                Physics.Obstacles.Add(new ObstacleCircle(cPos.X, cPos.Z, 0.9f, 1.0f, cPos.Y));
            }

            for (int i = 0; i < 3; i++)
            {
                if (!TryPlaceRandom(rng, 1.3f, 3.6f, 10f, 62f, out Vector3 sPos, padding: 0.8f))
                    sPos = PlaceFixed(new Vector3(NextRange(-30f, 30f), 0f, NextRange(-30f, 30f)), 1.3f, 3.6f);
                entitiesList.Add(new HealingShrine(sPos, (int)(chestCost * 0.8f)));
                Physics.Obstacles.Add(new ObstacleCircle(sPos.X, sPos.Z, 1.1f, 3.2f, sPos.Y));
            }

            for (int i = 0; i < 2; i++)
            {
                if (!TryPlaceRandom(rng, 1.1f, 2.8f, 10f, 66f, out Vector3 nPos, padding: 0.8f))
                    nPos = PlaceFixed(new Vector3(NextRange(-30f, 30f), 0f, NextRange(-30f, 30f)), 1.1f, 2.8f);
                entitiesList.Add(new NewtAltar(nPos));
            }

            // 6. Биомные структуры и архитектурные объекты (до 44 штук): каждая занимает свободное место с учётом
            // своего реального радиуса — без наложения на соседние структуры, террасы, телепорт, сундуки и внешнюю стену
            string[] structTypes = GetBiomeStructures(palette.Id);
            for (int i = 0; i < 44; i++)
            {
                string st = structTypes[rng.Next(structTypes.Length)];
                float rotY = NextRange(0f, 360f);
                float sFp = WorldStructuresBuilder.GetStructureFootprintRadius(st);
                float sH = WorldStructuresBuilder.GetStructureApproxHeight(st);
                if (!TryPlaceRandom(rng, sFp, sH, 12f, 80f, out Vector3 stPos, padding: 0.9f, maxAttempts: 40))
                    continue;
                var (sEnt, localObs) = WorldStructuresBuilder.BuildMapStructure(null, st, palette, stPos, rotY, rng, Physics.BoxObstacles);
                entitiesList.Add(sEnt);
                Physics.Obstacles.AddRange(localObs);
            }

            // 7. Малые декорации окружения (валуны, рунные столбики, кристаллические кусты — до 48 штук, тоже без наложений)
            for (int i = 0; i < 48; i++)
            {
                int kind = i % 3;
                float rockR = kind == 0 ? NextRange(0.9f, 1.8f) : 0f;
                float postH = kind == 1 ? NextRange(1.6f, 2.6f) : 0f;
                float dFp = kind == 0 ? rockR * 0.9f : 0.7f;
                if (!TryPlaceRandom(rng, dFp, 2.6f, 8f, 82f, out Vector3 dPos, padding: 0.4f, maxAttempts: 25))
                    continue;
                float dx = dPos.X, dy = dPos.Y, dz = dPos.Z;
                var prop = new Entity(position: new(dx, dy, dz), rotation: new(0, NextRange(0f, 360f), 0));
                if (kind == 0)
                {
                    // Каменный валун со мхом / снегом / кристаллами
                    float r = rockR;
                    new Entity(parent: prop, model: "sphere", scale: new(r * 1.4f, r * 0.9f, r * 1.2f), position: new(0, r * 0.35f, 0), color: palette.Ruins);
                    new Entity(parent: prop, model: "diamond", scale: new(r * 0.45f, r * 0.95f, r * 0.45f), position: new(r * 0.25f, r * 0.75f, 0), rotation: new(15f, 0, -18f), color: palette.Accent, unlit: true);
                    Physics.Obstacles.Add(new ObstacleCircle(dx, dz, r * 0.68f, r * 0.95f, dy));
                }
                else if (kind == 1)
                {
                    // Древний дорожный фонарь / рунный столбик
                    float h = postH;
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

            // 8. Уникальные каноничные события разблокировки 44 выживших (отдельный RNG — не сдвигает реплеи и кооператив).
            CharacterUnlockSystems.PopulateRegularStageUnlocks(entitiesList, stageNum, seed, tp);

            return (entitiesList, tp, palette);
        }

        // =========================================================================================
        // 13 УНИКАЛЬНЫХ СЕКРЕТНЫХ ИЗМЕРЕНИЙ И АРЕН ИСПЫТАНИЙ
        // =========================================================================================
        private static void BuildSecretStageArena(
            List<Entity> entitiesList, StagePalette palette, string specialMode, int stageNum, Random rng)
        {
            int secChestCost = (int)(28 * MathF.Pow(1.38f, Math.Max(0, stageNum - 1)));

            // Сначала строится геометрия арены (с коллайдерами), а уже потом в свободные точки ставятся
            // уникальные испытания разблокировки, портал, сундуки и святилище.
            BuildSecretArenaGeometry(entitiesList, palette, specialMode, rng);

            // Точка высадки игрока в центре арены остаётся свободной
            Physics.RegisterSpawnBlocker(Vector3.Zero, 3.5f, 4.0f);

            CharacterUnlockSystems.PopulateSecretArenaUnlocks(entitiesList, specialMode, stageNum);

            entitiesList.Add(new BluePortal(PlaceFixed(new Vector3(0f, 0f, -26f), 2.0f, 4.0f), "next_stage"));
            if (!GameData.HasArtifact("sacrifice"))
            {
                entitiesList.Add(new Chest(PlaceFixed(new Vector3(-15f, 0f, -6f), 1.2f, 1.8f), secChestCost, 401, "large"));
                entitiesList.Add(new Chest(PlaceFixed(new Vector3(15f, 0f, -6f), 1.2f, 1.8f), secChestCost, 402, "large"));
                entitiesList.Add(new Chest(PlaceFixed(new Vector3(0f, 0f, -14f), 1.0f, 1.8f), secChestCost, 403, "small"));
            }
            entitiesList.Add(new HealingShrine(PlaceFixed(new Vector3(-12f, 0f, -18f), 1.3f, 3.6f), Math.Max(20, (int)(secChestCost * 0.7f))));
        }

        private static void BuildSecretArenaGeometry(List<Entity> entitiesList, StagePalette palette, string specialMode, Random rng)
        {
            switch (specialMode)
            {
                case "pizzeria":
                {
                    // Пиццерия Freddy Fazbear's: шахматная сцена с бархатными шторами, прожекторами, столиками и аркадами
                    var stageRoot = new Entity(position: new(0, 0, 26f));
                    new Entity(parent: stageRoot, model: "cube", scale: new(22f, 1.1f, 8.5f), position: new(0, 0.55f, 0), color: Rgb(95, 58, 36));
                    for (int cx = -4; cx <= 4; cx++)
                        new Entity(parent: stageRoot, model: "cube", scale: new(2.1f, 0.08f, 8.2f), position: new(cx * 2.2f, 1.12f, 0), color: cx % 2 == 0 ? Rgb(235, 235, 240) : Rgb(26, 26, 32));
                    foreach (float sx in new[] { -9.5f, 9.5f })
                        new Entity(parent: stageRoot, model: "cube", scale: new(3.2f, 8.5f, 1.2f), position: new(sx, 4.8f, 3.2f), color: Rgb(135, 28, 165));
                    new Entity(parent: stageRoot, model: "cube", scale: new(23f, 1.6f, 1.4f), position: new(0, 8.8f, 3.2f), color: Rgb(165, 35, 195));
                    foreach (float lx in new[] { -6f, 0f, 6f })
                        new Entity(parent: stageRoot, model: "sphere", scaleUniform: 0.85f, position: new(lx, 8.1f, 2.4f), color: Color.Gold, unlit: true);
                    entitiesList.Add(stageRoot);
                    // Сцена — проходимая платформа (на неё можно запрыгнуть), кулисы и верхняя балка — твёрдые
                    Physics.AddSolidBox(0f, 26f, 22f, 8.5f, 1.1f, 0f, 0f, walkableTop: true);
                    foreach (float sx in new[] { -9.5f, 9.5f })
                        Physics.AddSolidBox(sx, 29.2f, 3.2f, 1.2f, 8.5f, 0f, 0.55f, walkableTop: false);
                    Physics.AddSolidBox(0f, 29.2f, 23f, 1.4f, 1.6f, 0f, 8.0f, walkableTop: true);


                    // Обеденные столы с коробками пиццы и праздничными колпаками + аркадные автоматы по бокам
                    foreach (float tx in new[] { -14f, 14f })
                    {
                        for (int tz = -1; tz <= 1; tz++)
                        {
                            var tbl = new Entity(position: new(tx, 0, tz * 9f + 4f));
                            new Entity(parent: tbl, model: "cube", scale: new(3.2f, 1.1f, 3.2f), position: new(0, 0.55f, 0), color: Rgb(215, 45, 55));
                            new Entity(parent: tbl, model: "cube", scale: new(3.4f, 0.14f, 3.4f), position: new(0, 1.12f, 0), color: Rgb(240, 240, 248));
                            new Entity(parent: tbl, model: "cube", scale: new(0.95f, 0.16f, 0.95f), position: new(0.3f, 1.24f, 0.2f), rotation: new(0, 18f, 0), color: Rgb(225, 175, 95));
                            entitiesList.Add(tbl);
                            Physics.AddSolidBox(tx, tz * 9f + 4f, 3.4f, 3.4f, 1.19f, 0f, 0f, walkableTop: true);
                            Physics.RegisterSpawnBlocker(new Vector3(tx, 0f, tz * 9f + 4f), 2.4f, 1.4f);
                        }
                    }
                    for (int a = -2; a <= 2; a++)
                    {
                        foreach (float sideX in new[] { -28f, 28f })
                        {
                            float arcYaw = sideX < 0 ? 90f : -90f;
                            var arc = new Entity(position: new(sideX, 0, a * 6.5f), rotation: new(0, arcYaw, 0));
                            new Entity(parent: arc, model: "cube", scale: new(1.8f, 3.4f, 1.6f), position: new(0, 1.7f, 0), color: Rgb(42, 28, 72));
                            new Entity(parent: arc, model: "cube", scale: new(1.4f, 1.2f, 0.12f), position: new(0, 2.2f, 0.82f), color: a % 2 == 0 ? Color.SkyBlue : Color.Magenta, unlit: true);
                            entitiesList.Add(arc);
                            Physics.AddSolidBox(sideX, a * 6.5f, 1.8f, 1.8f, 3.4f, arcYaw, 0f, walkableTop: true);
                            Physics.RegisterSpawnBlocker(new Vector3(sideX, 0f, a * 6.5f), 1.4f, 3.4f);
                        }
                    }
                    break;
                }

                case "black_mesa":
                {
                    // Сектор C Чёрной Мезы: тестовая камера антимасс-спектрометра, верхние излучатели и окно наблюдения
                    var rotor = new Entity(position: new(0, 0, 15f));
                    for (int k = 0; k < 4; k++)
                    {
                        float ang = k * (MathF.PI * 0.5f) + MathF.PI * 0.25f;
                        float px = MathF.Cos(ang) * 14f, pz = MathF.Sin(ang) * 14f;
                        new Entity(parent: rotor, model: "cube", scale: new(2.4f, 14.0f, 2.4f), position: new(px, 7.0f, pz), color: Rgb(68, 76, 74));
                        new Entity(parent: rotor, model: "cube", scale: new(2.6f, 0.55f, 2.6f), position: new(px, 2.2f, pz), color: Rgb(255, 165, 35), unlit: true);
                        new Entity(parent: rotor, model: "cube", scale: new(0.45f, 0.45f, 13.5f), position: new(px * 0.5f, 11.5f, pz * 0.5f),
                            rotation: new(0, k * 90f + 45f, 0), color: Rgb(95, 235, 125), unlit: true);
                        Physics.AddSolidBox(px, 15f + pz, 2.6f, 2.6f, 14.0f, 0f, 0f, walkableTop: false);
                    }
                    new Entity(parent: rotor, model: "diamond", scale: new(5.5f, 3.2f, 5.5f), position: new(0, 12.2f, 0), color: Rgb(255, 165, 35), unlit: true);
                    entitiesList.Add(rotor);

                    // Верхняя рубка наблюдения учёных (твёрдая, на крышу можно запрыгнуть с трамплина/полётом)
                    var booth = new Entity(position: new(0, 7.5f, 36f));
                    new Entity(parent: booth, model: "cube", scale: new(24f, 6.0f, 5.0f), color: Rgb(58, 66, 64));
                    new Entity(parent: booth, model: "cube", scale: new(19f, 3.2f, 0.35f), position: new(0, 0.3f, -2.55f), color: Rgb(125, 235, 215, 170), unlit: true);
                    entitiesList.Add(booth);
                    Physics.AddSolidBox(0f, 36f, 24f, 5.2f, 6.0f, 0f, 4.5f, walkableTop: true);
                    break;
                }

                case "qliphoth":
                {
                    // Вершина Древа Клипота: переплетённые кровавые корни, демонические монолиты и парящие уступы
                    for (int i = 0; i < 10; i++)
                    {
                        float ang = (i / 10f) * MathF.PI * 2f;
                        Vector3 rp = new(MathF.Cos(ang) * 32f, 0f, MathF.Sin(ang) * 32f);
                        var root = new Entity(position: rp, rotation: new(0, i * 36f, 0));
                        new Entity(parent: root, model: "cube", scale: new(3.2f, 11.5f, 2.8f), position: new(0, 5.5f, 0), rotation: new(-18f, 0, 8f), color: Rgb(62, 18, 28));
                        new Entity(parent: root, model: "diamond", scale: new(1.8f, 9.5f, 1.8f), position: new(0, 8.5f, -1.8f), rotation: new(-26f, 0, 0), color: Rgb(235, 35, 65), unlit: true);
                        entitiesList.Add(root);
                        Physics.Obstacles.Add(new ObstacleCircle(rp.X, rp.Z, 2.2f, 11.0f, 0f));
                    }
                    foreach (float sx in new[] { -20f, 20f })
                        SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(sx, 3.2f, 6f), 6.5f, 1.2f, Rgb(78, 22, 34), Rgb(255, 45, 75));
                    break;
                }

                case "hell_cybergrind":
                {
                    // Кибер-Грайнд (Предел Ада): ступенчатые геометрические колонны терминала, кровавая сетка и трамплины
                    (float cx, float cz, float h)[] gridCols =
                    {
                        (-18f, 18f, 3.0f), (18f, 18f, 3.0f), (-22f, -4f, 5.2f), (22f, -4f, 5.2f),
                        (-14f, 30f, 5.6f), (14f, 30f, 5.6f)
                    };
                    foreach (var (cx, cz, h) in gridCols)
                    {
                        SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(cx, h, cz), 5.6f, 1.4f, Rgb(46, 18, 24), Rgb(255, 45, 55));
                    }
                    entitiesList.Add(new LunarBouncePad(new Vector3(-18f, 0f, 4f), 21.0f, 7.0f));
                    entitiesList.Add(new LunarBouncePad(new Vector3(18f, 0f, 4f), 21.0f, 7.0f));
                    for (int gLine = -4; gLine <= 4; gLine++)
                    {
                        entitiesList.Add(new Entity(model: "cube", scale: new(76f, 0.05f, 0.16f), position: new(0, 0.03f, gLine * 9f), color: Rgb(225, 35, 45), unlit: true));
                        entitiesList.Add(new Entity(model: "cube", scale: new(0.16f, 0.05f, 76f), position: new(gLine * 9f, 0.04f, 0), color: Rgb(225, 35, 45), unlit: true));
                    }
                    break;
                }

                case "tartarus":
                {
                    // Башня Тартар (Тёмный Час): изумрудный циферблат на полу, искажённые часовые колонны и парящие саркофаги
                    for (int hr = 0; hr < 12; hr++)
                    {
                        float ang = hr * (MathF.PI * 2f / 12f);
                        float rx = MathF.Cos(ang) * 20f, rz = 8f + MathF.Sin(ang) * 20f;
                        entitiesList.Add(new Entity(model: "cube", scale: new(0.65f, 0.08f, 3.2f),
                            position: new(rx, 0.04f, rz), rotation: new(0, -ang * (180f / MathF.PI), 0), color: Rgb(75, 255, 155), unlit: true));

                        float px = MathF.Cos(ang) * 34f, pz = MathF.Sin(ang) * 34f;
                        var col = new Entity(position: new(px, 0, pz), rotation: new(0, hr * 30f, 0));
                        new Entity(parent: col, model: "cube", scale: new(2.4f, 12.5f, 2.4f), position: new(0, 6.25f, 0), color: Rgb(28, 56, 44));
                        new Entity(parent: col, model: "diamond", scale: new(3.2f, 0.45f, 3.2f), position: new(0, 9.5f, 0), color: Rgb(85, 255, 165), unlit: true);
                        new Entity(parent: col, model: "cube", scale: new(1.1f, 3.2f, 0.75f), position: new(0, 14.5f, 0), color: Rgb(62, 42, 32));
                        entitiesList.Add(col);
                        Physics.AddSolidBox(px, pz, 2.4f, 2.4f, 12.5f, hr * 30f, 0f, walkableTop: false);
                    }
                    break;
                }

                case "hakurei_shrine":
                {
                    // Храм Хакурей (Генсокё): аллея алых врат Тории, деревянный павильон святилища и цветущая сакура
                    var shrine = new Entity(position: new(0, 0, 32f));
                    new Entity(parent: shrine, model: "cube", scale: new(18f, 1.2f, 10f), position: new(0, 0.6f, 0), color: Rgb(115, 86, 68));
                    new Entity(parent: shrine, model: "cube", scale: new(15f, 5.8f, 7.5f), position: new(0, 4.1f, 0), color: Rgb(228, 218, 202));
                    new Entity(parent: shrine, model: "cube", scale: new(19.5f, 1.4f, 10.5f), position: new(0, 7.5f, 0), color: Rgb(175, 38, 46));
                    new Entity(parent: shrine, model: "sphere", scale: new(2.4f, 2.4f, 0.35f), position: new(0, 5.2f, -3.85f), color: Color.Gold, unlit: true);
                    entitiesList.Add(shrine);
                    // Ступень-основание (проходимая), стены павильона (твёрдые) и крыша (на неё можно запрыгнуть)
                    Physics.AddSolidBox(0f, 32f, 18f, 10f, 1.2f, 0f, 0f, walkableTop: true);
                    Physics.AddSolidBox(0f, 32f, 15f, 7.5f, 6.8f, 0f, 0f, walkableTop: false);
                    Physics.AddSolidBox(0f, 32f, 19.5f, 10.5f, 1.4f, 0f, 6.8f, walkableTop: true);
                    Physics.RegisterSpawnBlocker(new Vector3(0f, 0f, 32f), 2.0f, 8.2f);

                    foreach (float tz in new[] { -18f, -8f, 2f })
                    {
                        var (tEnt, tObs) = WorldStructuresBuilder.BuildMapStructure(null, "torii", palette, new Vector3(0, 0, tz), 0f, rng, Physics.BoxObstacles);
                        entitiesList.Add(tEnt);
                        Physics.Obstacles.AddRange(tObs);
                    }
                    foreach (var (sx, sz) in new[] { (-22f, 18f), (22f, 18f), (-24f, -8f), (24f, -8f) })
                    {
                        var (sEnt, sObs) = WorldStructuresBuilder.BuildMapStructure(null, "sakura_tree", palette, new Vector3(sx, 0, sz), rng.Next(0, 360), rng, Physics.BoxObstacles);
                        entitiesList.Add(sEnt);
                        Physics.Obstacles.AddRange(sObs);
                        Physics.RegisterSpawnBlocker(new Vector3(sx, 0f, sz), 2.2f, 10f);
                    }
                    break;
                }

                case "tracen_derby":
                {
                    // Ипподром Трейсен (Арима Кинэн): овальная дерновая дорожка с белыми ограждениями, стартовыми боксами и трибунами
                    for (int seg = 0; seg < 24; seg++)
                    {
                        float ang = (seg / 24f) * MathF.PI * 2f;
                        // Секции ограждения разворачиваются по касательной к овалу дорожки (раньше стояли поперёк трека)
                        float outerYaw = MathF.Atan2(-34f * MathF.Cos(ang), -44f * MathF.Sin(ang)) * (180f / MathF.PI);
                        float innerYaw = MathF.Atan2(-12f * MathF.Cos(ang), -16f * MathF.Sin(ang)) * (180f / MathF.PI);
                        float ox = MathF.Cos(ang) * 44f, oz = MathF.Sin(ang) * 34f;
                        float ix = MathF.Cos(ang) * 16f, iz = MathF.Sin(ang) * 12f;
                        entitiesList.Add(new Entity(model: "cube", scale: new(3.2f, 1.1f, 0.25f),
                            position: new(ox, 0.55f, oz), rotation: new(0, outerYaw, 0), color: Rgb(242, 245, 252)));
                        entitiesList.Add(new Entity(model: "cube", scale: new(1.8f, 0.95f, 0.22f),
                            position: new(ix, 0.48f, iz), rotation: new(0, innerYaw, 0), color: Rgb(242, 245, 252)));
                        // Ограждения ипподрома — твёрдые (низкие, их можно перепрыгнуть)
                        Physics.AddSolidBox(ox, oz, 3.2f, 0.5f, 1.1f, outerYaw, 0f, walkableTop: false);
                        Physics.AddSolidBox(ix, iz, 1.8f, 0.45f, 0.95f, innerYaw, 0f, walkableTop: false);
                        Physics.RegisterSpawnBlocker(new Vector3(ox, 0f, oz), 1.7f, 1.2f);
                        Physics.RegisterSpawnBlocker(new Vector3(ix, 0f, iz), 1.0f, 1.0f);
                    }
                    // Главная трибуна зрителей на севере и золотая подкова в центре газона
                    var stand = new Entity(position: new(0, 0, 46f));
                    for (int tier = 0; tier < 4; tier++)
                    {
                        new Entity(parent: stand, model: "cube", scale: new(38f, 1.8f, 3.2f), position: new(0, 0.9f + tier * 1.6f, tier * 2.6f), color: tier % 2 == 0 ? Rgb(225, 232, 245) : Rgb(65, 135, 215));
                        // Каждый ярус трибуны — ступень, по которой можно подняться прыжками
                        Physics.AddSolidBox(0f, 46f + tier * 2.6f, 38f, 3.2f, 1.8f + tier * 1.6f, 0f, 0f, walkableTop: true);
                    }
                    new Entity(parent: stand, model: "cube", scale: new(40f, 0.8f, 12f), position: new(0, 9.2f, 4f), color: Rgb(215, 48, 65));
                    entitiesList.Add(stand);
                    Physics.AddSolidBox(0f, 50f, 40f, 12f, 0.8f, 0f, 8.8f, walkableTop: true);

                    var centerPiece = new Entity(position: new(0f, 0f, 5.5f));
                    new Entity(parent: centerPiece, model: "diamond", scale: new(5.5f, 0.45f, 5.5f), position: new(0, 0.25f, 0), color: Rgb(235, 238, 245));
                    new Entity(parent: centerPiece, model: "diamond", scale: new(2.6f, 4.4f, 1.2f), position: new(0, 2.6f, 0), color: Color.Gold, unlit: true);
                    entitiesList.Add(centerPiece);
                    Physics.Obstacles.Add(new ObstacleCircle(0f, 5.5f, 2.2f, 4.5f, 0f));
                    break;
                }
                default:
                    BuildExpansionSecretArenaGeometry(entitiesList, palette, specialMode, rng);
                    break;
            }

            // Внешняя ограда по периметру арены: видимая балюстрада с твёрдым коллайдером
            // (раньше край арены был невидимой стеной посреди пола)
            float wb = Physics.WorldBound + 0.9f;
            float span = wb * 2f + 1.8f;
            const float railH = 2.2f;
            foreach (var (wx, wz, sx, sz) in new[] { (0f, wb, span, 1.8f), (0f, -wb, span, 1.8f), (wb, 0f, 1.8f, span), (-wb, 0f, 1.8f, span) })
            {
                var rail = new Entity(position: new(wx, 0f, wz));
                new Entity(parent: rail, model: "cube", scale: new(sx, railH, sz), position: new(0, railH * 0.5f, 0), color: palette.Ruins);
                new Entity(parent: rail, model: "cube", scale: new(sx + 0.1f, 0.18f, sz + 0.1f), position: new(0, railH + 0.09f, 0), color: palette.Accent, unlit: true);
                entitiesList.Add(rail);
                Physics.AddSolidBox(wx, wz, sx, sz, 60f, 0f, 0f, walkableTop: false);
            }
        }

        // =========================================================================================
        // ОРБИТАЛЬНЫЙ КРЕЙСЕР UES «SAFE TRAVELS» — ПОЛНОЦЕННЫЙ ИГРОВОЙ ХАБ ЭКИПАЖА ПОСЛЕ МИТРИКСА
        // =========================================================================================
        private static void BuildSafeTravelsHub(List<Entity> entitiesList, StagePalette palette)
        {
            SafeTravelsNpc.ActiveHubNpcs.Clear();

            Color deckCol = Rgb(34, 42, 58);
            Color deckSubCol = Rgb(28, 35, 48);
            Color hullCol = Rgb(54, 66, 88);
            Color innerWallCol = Rgb(46, 56, 76);
            Color cyanNeon = Rgb(75, 215, 255);
            Color amberNeon = Rgb(245, 165, 45);

            void AddDeckPlate(float cx, float cz, float w, float d, Color mainCol, Color borderCol)
            {
                var tile = new Entity(model: "cube", scale: new(w, 1.2f, d), position: new(cx, -0.60f, cz), color: mainCol);
                entitiesList.Add(tile);
                GroundTiles.Add(tile);
                entitiesList.Add(new Entity(model: "cube", scale: new(w - 1.2f, 0.04f, 0.24f), position: new(cx, 0.02f, cz + d * 0.5f - 1.0f), color: borderCol, unlit: true));
                entitiesList.Add(new Entity(model: "cube", scale: new(w - 1.2f, 0.04f, 0.24f), position: new(cx, 0.02f, cz - d * 0.5f + 1.0f), color: borderCol, unlit: true));
            }

            void AddShipWall(float x0, float z0, float x1, float z1, float visualH, float physH, Color wCol, Color stripeCol, float thick = 0.90f)
            {
                float dx = x1 - x0, dz = z1 - z0;
                float len = MathF.Sqrt(dx * dx + dz * dz);
                if (len < 0.2f) return;
                float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f;
                float yaw = MathF.Atan2(dx, dz) * (180f / MathF.PI);

                var wall = new Entity(position: new(cx, 0f, cz), rotation: new(0f, yaw, 0f));
                new Entity(parent: wall, model: "cube", scale: new(thick, visualH, len), position: new(0f, visualH * 0.5f, 0f), color: wCol);
                new Entity(parent: wall, model: "cube", scale: new(thick + 0.08f, 0.16f, len), position: new(0f, MathF.Min(1.55f, visualH * 0.45f), 0f), color: stripeCol, unlit: true);
                new Entity(parent: wall, model: "cube", scale: new(thick + 0.14f, 0.26f, len), position: new(0f, visualH + 0.13f, 0f), color: Rgb(68, 82, 108));
                entitiesList.Add(wall);

                // Сплошной прямоугольный коллайдер по всей длине переборки (+ половина толщины на концах, чтобы
                // стыки и углы корпуса были герметичны). Раньше стена была цепочкой кругов с щелями между ними,
                // сквозь которые пролетали снаряды, камера и рывки.
                Physics.AddSolidBox(cx, cz, thick, len + thick, physH, yaw, 0f, walkableTop: false);
            }

            // 1. ПАЛУБНЫЕ ПЛИТЫ ВСЕХ 5 КРЫЛЬЕВ КРЕЙСЕРА (Y = 0.0 — твёрдая поверхность без провалов)
            AddDeckPlate(0f, 3f, 56f, 50f, deckCol, cyanNeon);          // Центральный Зал (Кают-Компания)
            AddDeckPlate(0f, 49f, 60f, 42f, deckSubCol, cyanNeon);      // Северное Крыло (Капитанский Мостик)
            AddDeckPlate(0f, -44f, 48f, 44f, deckSubCol, amberNeon);    // Южный Ангар (Десантный Отсек)
            AddDeckPlate(-57f, 0f, 58f, 72f, deckSubCol, amberNeon);    // Крыло Полигона (X <= -28: Тир DPS и Песочница)
            AddDeckPlate(58f, 0f, 60f, 64f, deckCol, cyanNeon);         // Крыло Кают (X >= +28: Коридор, Лазарет и 6 Кают)

            // Световые дорожки навигации и центральная эмблема UES «Safe Travels»
            var emblem = new Entity(position: Vector3.Zero);
            new Entity(parent: emblem, model: "diamond", scale: new(7.5f, 0.05f, 7.5f), position: new(0, 0.02f, 0), color: Rgb(48, 88, 138), unlit: true);
            new Entity(parent: emblem, model: "diamond", scale: new(4.2f, 0.07f, 4.2f), position: new(0, 0.03f, 0), color: cyanNeon, unlit: true);
            entitiesList.Add(emblem);
            entitiesList.Add(new Entity(model: "cube", scale: new(0.45f, 0.04f, 96f), position: new(0, 0.02f, 2f), color: cyanNeon, unlit: true));
            entitiesList.Add(new Entity(model: "cube", scale: new(126f, 0.04f, 0.45f), position: new(8f, 0.03f, 0f), color: cyanNeon, unlit: true));

            // Указатели отсеков вокруг центральной палубы
            (Vector3 pos, string txt, Color col)[] signs =
            {
                (new(0f, 4.4f, 26.5f), "▲ КАПИТАНСКИЙ МОСТИК И СМОТРОВАЯ ПАЛУБА", cyanNeon),
                (new(-26.5f, 4.4f, 0f), "◆ ТРЕНИРОВОЧНЫЙ ПОЛИГОН И ТИР ПРОВЕРКИ УРОНА", amberNeon),
                (new(26.5f, 4.4f, 0f), "◆ ЖИЛЫЕ КАЮТЫ, ЛАЗАРЕТ И МАСТЕРСКИЕ", cyanNeon),
                (new(0f, 4.4f, -20.5f), "▼ ДЕСАНТНЫЙ АНГАР (НОВЫЙ ЗАБЕГ / ВЫХОД В МЕНЮ)", Color.Lime)
            };
            foreach (var (sPos, sTxt, sCol) in signs)
                entitiesList.Add(new Entity(position: sPos, color: sCol) { BillboardText = sTxt });

            // 2. ВНЕШНИЕ ПЕРЕБОРКИ КОРПУСА И КАЮТНЫЕ СТЕНЫ (высота стен 4.6м — потолок открыт для MOBA/Mindustry камер!)
            // Центральный зал — переходы в 4 крыла
            AddShipWall(-28f, 28f, -10f, 28f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(10f, 28f, 28f, 28f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(-28f, -22f, -9f, -22f, 4.6f, 80f, hullCol, amberNeon);
            AddShipWall(9f, -22f, 28f, -22f, 4.6f, 80f, hullCol, amberNeon);
            AddShipWall(-28f, -22f, -28f, -9f, 4.6f, 80f, hullCol, amberNeon);
            AddShipWall(-28f, 9f, -28f, 28f, 4.6f, 80f, hullCol, amberNeon);
            AddShipWall(28f, -22f, 28f, -6f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(28f, 6f, 28f, 28f, 4.6f, 80f, hullCol, cyanNeon);

            // Северный Мостик + панорамное бронестекло на Z = +70
            AddShipWall(-30f, 28f, -28f, 28f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(28f, 28f, 30f, 28f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(-30f, 28f, -30f, 70f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(30f, 28f, 30f, 70f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(-30f, 70f, 30f, 70f, 1.15f, 80f, hullCol, cyanNeon);
            var bridgeGlass = new Entity(position: new(0f, 0f, 70f));
            new Entity(parent: bridgeGlass, model: "cube", scale: new(59f, 5.8f, 0.22f), position: new(0f, 4.1f, 0f), color: Rgb(85, 210, 255, 58), unlit: true);
            new Entity(parent: bridgeGlass, model: "cube", scale: new(60.5f, 0.55f, 1.1f), position: new(0f, 7.15f, 0f), color: hullCol);
            for (int m = -2; m <= 2; m++)
                new Entity(parent: bridgeGlass, model: "cube", scale: new(0.38f, 5.8f, 0.42f), position: new(m * 11.5f, 4.1f, 0f), color: hullCol);
            entitiesList.Add(bridgeGlass);

            // Южный Десантный Ангар
            AddShipWall(-24f, -66f, -24f, -22f, 4.6f, 80f, hullCol, amberNeon);
            AddShipWall(24f, -66f, 24f, -22f, 4.6f, 80f, hullCol, amberNeon);
            AddShipWall(-24f, -66f, 24f, -66f, 4.6f, 80f, hullCol, amberNeon);

            // Крыло Боевого Тренировочного Полигона (X = -86..-28)
            AddShipWall(-28f, -36f, -28f, -22f, 4.6f, 80f, hullCol, amberNeon);
            AddShipWall(-28f, 28f, -28f, 36f, 4.6f, 80f, hullCol, amberNeon);
            AddShipWall(-86f, 36f, -28f, 36f, 4.6f, 80f, hullCol, amberNeon);
            AddShipWall(-86f, -36f, -28f, -36f, 4.6f, 80f, hullCol, amberNeon);
            AddShipWall(-86f, -36f, -86f, 36f, 5.4f, 80f, hullCol, Color.Red);

            // Крыло Кают Экипажа (X = +28..+88, внешний контур + 6 персональных кают вдоль коридора Z = -6..+6)
            AddShipWall(28f, -32f, 28f, -22f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(28f, 28f, 28f, 32f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(28f, 32f, 88f, 32f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(28f, -32f, 88f, -32f, 4.6f, 80f, hullCol, cyanNeon);
            AddShipWall(88f, -32f, 88f, 32f, 4.6f, 80f, hullCol, cyanNeon);
            // Перегородки между 6 комнатами (C1/C2/C3 сверху, C4/C5/C6 снизу) и широкие дверные проёмы по 7 метров
            AddShipWall(48f, 6f, 48f, 32f, 3.6f, 4.2f, innerWallCol, cyanNeon, 0.65f);
            AddShipWall(68f, 6f, 68f, 32f, 3.6f, 4.2f, innerWallCol, cyanNeon, 0.65f);
            AddShipWall(48f, -32f, 48f, -6f, 3.6f, 4.2f, innerWallCol, cyanNeon, 0.65f);
            AddShipWall(68f, -32f, 68f, -6f, 3.6f, 4.2f, innerWallCol, cyanNeon, 0.65f);
            foreach (float czWall in new[] { 6f, -6f })
            {
                AddShipWall(28f, czWall, 34.5f, czWall, 3.6f, 4.2f, innerWallCol, cyanNeon, 0.65f);
                AddShipWall(41.5f, czWall, 54.5f, czWall, 3.6f, 4.2f, innerWallCol, cyanNeon, 0.65f);
                AddShipWall(61.5f, czWall, 74.5f, czWall, 3.6f, 4.2f, innerWallCol, cyanNeon, 0.65f);
                AddShipWall(81.5f, czWall, 88f, czWall, 3.6f, 4.2f, innerWallCol, cyanNeon, 0.65f);
            }

            // Высокие редкие силовые шпангоуты крейсера на Y = 26.5 (без коллизии и без сплошной крыши!)
            foreach (float rz in new[] { -44f, 0f, 48f })
            {
                var rib = new Entity(position: new(0f, 26.5f, rz));
                new Entity(parent: rib, model: "cube", scale: new(56f, 0.85f, 1.1f), color: Rgb(48, 60, 82));
                new Entity(parent: rib, model: "cube", scale: new(54f, 0.18f, 0.35f), position: new(0, -0.48f, 0), color: cyanNeon, unlit: true);
                entitiesList.Add(rib);
            }

            // Два обзорных балкона-антресоли в Центральном Зале + грави-трамплины
            SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(-21f, 3.4f, 0f), 4.6f, 0.9f, hullCol, cyanNeon);
            SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(21f, 3.4f, 0f), 4.6f, 0.9f, hullCol, cyanNeon);
            entitiesList.Add(new LunarBouncePad(new Vector3(-22f, 0f, -8.5f), 14.5f, 3.5f));
            entitiesList.Add(new LunarBouncePad(new Vector3(22f, 0f, -8.5f), 14.5f, 3.5f));

            // 3. ЖИВАЯ КОСМИЧЕСКАЯ ПАНОРАМА ЗА ИЛЛЮМИНАТОРОМ МОСТИКА (PETRICHOR V, РАСКОЛОТАЯ ЛУНА, ЭСКОРТНЫЙ ФРЕГАТ И ЗВЁЗДЫ) + СЕРВИСНЫЕ ДРОНЫ
            entitiesList.Add(new SafeTravelsCosmicBackdrop());
            entitiesList.Add(new SafeTravelsAmbientDrone(
                new[] { new Vector3(0f, 0f, -18f), new Vector3(0f, 0f, 38f), new Vector3(8f, 0f, 6f), new Vector3(-8f, 0f, 6f) },
                cyanNeon, "[ДРОН-КУРЬЕР UES #01]"));
            entitiesList.Add(new SafeTravelsAmbientDrone(
                new[] { new Vector3(32f, 0f, -1.6f), new Vector3(82f, 0f, -1.6f) },
                Color.Gold, "[СЕРВИС-ДРОН КАЮТ #02]"));
            entitiesList.Add(new SafeTravelsAmbientDrone(
                new[] { new Vector3(-38f, 0f, -16f), new Vector3(-38f, 0f, 16f) },
                amberNeon, "[ДРОН-ХРОНОМЕТР ПОЛИГОНА]"));

            // 4. ДЕТАЛИЗИРОВАННАЯ МЕБЕЛЬ, ПЕРСОНАЛЬНЫЕ УГОЛКИ И ИНТЕРАКТИВНЫЕ ОБЪЕКТЫ ВСЕХ ОТСЕКОВ
            BuildSafeTravelsInteriorProps(entitiesList);
            PopulateSafeTravelsCrew(entitiesList);
        }

        private static void BuildSafeTravelsInteriorProps(List<Entity> entitiesList)
        {
            Color cyanNeon = Rgb(75, 215, 255);
            Color amberNeon = Rgb(245, 165, 45);
            Color metalDark = Rgb(38, 46, 62);
            Color metalMid = Rgb(56, 68, 88);

            // --- СЕВЕРНЫЙ МОСТИК: Голографический стол орбиты, штурвал и терминал «САННАБИ» ---
            var holoTable = new Entity(position: new(0f, 0f, 49f));
            new Entity(parent: holoTable, model: "diamond", scale: new(5.4f, 1.15f, 5.4f), position: new(0, 0.58f, 0), color: metalDark);
            new Entity(parent: holoTable, model: "diamond", scale: new(5.8f, 0.14f, 5.8f), position: new(0, 1.16f, 0), color: cyanNeon, unlit: true);
            new Entity(parent: holoTable, model: "sphere", scaleUniform: 1.55f, position: new(-0.6f, 2.35f, 0), color: Rgb(65, 195, 255, 185), unlit: true);
            new Entity(parent: holoTable, model: "sphere", scaleUniform: 0.72f, position: new(1.1f, 2.85f, 0.4f), color: Rgb(215, 235, 255, 200), unlit: true);
            entitiesList.Add(holoTable);
            Physics.Obstacles.Add(new ObstacleCircle(0f, 49f, 2.55f, 2.2f, 0f));
            entitiesList.Add(new SafeTravelsRoomProp(new(0f, 0f, 49f), "Голографический Стол Навигации UES",
                "Орбита Petrichor V стабильна. Расколотая Луна удерживается гравитационным шлейфом, сигнал бедствия «Contact Light» закрыт.", cyanNeon, "ui_ready", spawnHoloMarker: false, labelHeight: 3.7f));

            foreach (float hx in new[] { -9.5f, 9.5f })
            {
                var helm = new Entity(position: new(hx, 0f, 60f));
                new Entity(parent: helm, model: "cube", scale: new(4.2f, 1.25f, 1.5f), position: new(0, 0.62f, 0), color: metalMid);
                new Entity(parent: helm, model: "cube", scale: new(3.8f, 0.85f, 0.16f), position: new(0, 1.55f, 0.35f), rotation: new(-22f, 0, 0), color: cyanNeon, unlit: true);
                entitiesList.Add(helm);
                Physics.AddSolidBox(hx, 60f, 4.2f, 1.6f, 1.9f, 0f, 0f, walkableTop: false);
            }
            entitiesList.Add(new SafeTravelsRoomProp(new(9.5f, 0f, 60f), "Радар Дальнего Космоса",
                "Сканеры фиксируют 7 пространственных аномалий вокруг Базара Между Временем. Все члены экипажа на борту.", cyanNeon, "ui_tick"));

            var sanabiTerm = new Entity(position: new(-23.5f, 0f, 42f), rotation: new(0, 90f, 0));
            new Entity(parent: sanabiTerm, model: "cube", scale: new(2.6f, 2.4f, 1.2f), position: new(0, 1.2f, 0), color: Rgb(32, 26, 38));
            new Entity(parent: sanabiTerm, model: "cube", scale: new(2.1f, 1.1f, 0.14f), position: new(0, 1.75f, 0.62f), color: Rgb(255, 65, 95), unlit: true);
            entitiesList.Add(sanabiTerm);
            Physics.AddSolidBox(-23.5f, 42f, 2.6f, 1.5f, 2.5f, 90f, 0f, walkableTop: false);
            entitiesList.Add(new SafeTravelsRoomProp(new(-23.5f, 0f, 42f), "Зашифрованный Архив «МАГО / САННАБИ»",
                "Военный терминал Генерала Бригады с цепным крюком-кошкой и музыкальной записью дочери Мари.", Rgb(255, 85, 110), "dante_clock"));

            // --- ЦЕНТРАЛЬНЫЙ ЗАЛ: Бар «Бездна», Рок-Сцена, Котацу Рейму, Ящик Чебурашки, Трофей Дерби и Офис DMC ---
            var bar = new Entity(position: new(-19.5f, 0f, 12f));
            new Entity(parent: bar, model: "cube", scale: new(3.2f, 1.25f, 11.0f), position: new(0, 0.62f, 0), color: Rgb(58, 36, 74));
            new Entity(parent: bar, model: "cube", scale: new(3.45f, 0.14f, 11.2f), position: new(0, 1.26f, 0), color: Rgb(195, 75, 255), unlit: true);
            for (int b = -2; b <= 2; b++)
                new Entity(parent: bar, model: "diamond", scale: new(0.28f, 0.65f, 0.28f), position: new(-0.6f, 1.62f, b * 1.8f), color: b % 2 == 0 ? cyanNeon : Color.Gold, unlit: true);
            entitiesList.Add(bar);
            // Барная стойка — сплошной прямоугольник на всю длину (раньше между тремя кругами были «дыры»)
            Physics.AddSolidBox(-19.5f, 12f, 3.45f, 11.2f, 1.33f, 0f, 0f, walkableTop: true);
            entitiesList.Add(new SafeTravelsRoomProp(new(-19.5f, 0f, 12f), "Бар «Бездна»",
                "В меню: безалкогольный «Шпинелевый Тоник», чай для Кая Киске и трофейный морской эль Тайдхантера!", Rgb(205, 95, 255), "chest_open"));

            var stage = new Entity(position: new(20.5f, 0f, 13.5f));
            new Entity(parent: stage, model: "cube", scale: new(9.5f, 0.55f, 9.5f), position: new(0, 0.28f, 0), color: Rgb(48, 34, 58));
            new Entity(parent: stage, model: "cube", scale: new(2.4f, 2.8f, 1.4f), position: new(2.2f, 1.9f, 2.2f), color: Rgb(28, 28, 34));
            new Entity(parent: stage, model: "sphere", scaleUniform: 0.95f, position: new(2.2f, 2.1f, 1.45f), color: amberNeon, unlit: true);
            new Entity(parent: stage, model: "cube", scale: new(1.7f, 3.1f, 1.4f), position: new(2.4f, 2.1f, -2.4f), color: Rgb(62, 28, 96));
            new Entity(parent: stage, model: "cube", scale: new(1.3f, 1.0f, 0.12f), position: new(1.68f, 2.4f, -2.4f), rotation: new(0, 90f, 0), color: Color.Magenta, unlit: true);
            entitiesList.Add(stage);
            // Подиум сцены — проходимая ступень (раньше он был «прозрачным» и персонажи проваливались в него по колено)
            Physics.AddSolidBox(20.5f, 13.5f, 9.5f, 9.5f, 0.55f, 0f, 0f, walkableTop: true);
            Physics.AddSolidBox(22.7f, 15.7f, 2.4f, 1.4f, 3.3f, 0f, 0f, walkableTop: false);
            Physics.AddSolidBox(22.9f, 11.1f, 1.7f, 1.4f, 3.65f, 0f, 0f, walkableTop: false);
            entitiesList.Add(new SafeTravelsRoomProp(new(21.5f, 0f, 15.2f), "Ламповый Стек «Галилео» и Аркада 1987",
                "Гитарный усилитель Александра Пушного на 10 000 Вольт подключён к старому игровому автомату Фредди Фазбера!", amberNeon, "zap", labelHeight: 3.5f));

            var kotatsu = new Entity(position: new(-16.5f, 0f, -13.0f));
            new Entity(parent: kotatsu, model: "cube", scale: new(3.6f, 0.78f, 3.6f), position: new(0, 0.39f, 0), color: Rgb(178, 42, 54));
            new Entity(parent: kotatsu, model: "cube", scale: new(2.9f, 0.12f, 2.9f), position: new(0, 0.82f, 0), color: Rgb(125, 86, 52));
            new Entity(parent: kotatsu, model: "cube", scale: new(1.4f, 1.0f, 0.95f), position: new(-2.8f, 0.50f, 0), color: Rgb(138, 88, 48));
            entitiesList.Add(kotatsu);
            Physics.AddSolidBox(-16.5f, -13.0f, 3.6f, 3.6f, 0.88f, 0f, 0f, walkableTop: true);
            Physics.AddSolidBox(-19.3f, -13.0f, 1.4f, 0.95f, 1.0f, 0f, 0f, walkableTop: true);
            entitiesList.Add(new SafeTravelsRoomProp(new(-16.5f, 0f, -13.0f), "Столик Котацу и Ящик Подношений Хакурей",
                "Рейму повесила табличку: «Принимаются кредиты UES, лунные монеты и мандарины. Вход без пожертвований воспрещён!»", Rgb(255, 105, 135), "coin_flip"));

            var orangeCrate = new Entity(position: new(-9.5f, 0f, -15.5f));
            new Entity(parent: orangeCrate, model: "cube", scale: new(2.2f, 1.3f, 1.8f), position: new(0, 0.65f, 0), color: Rgb(168, 118, 62));
            for (int o = -1; o <= 1; o++)
                new Entity(parent: orangeCrate, model: "sphere", scaleUniform: 0.55f, position: new(o * 0.55f, 1.42f, 0), color: Color.Orange, unlit: true);
            entitiesList.Add(orangeCrate);
            Physics.AddSolidBox(-9.5f, -15.5f, 2.2f, 1.8f, 1.3f, 0f, 0f, walkableTop: true);
            entitiesList.Add(new SafeTravelsRoomProp(new(-9.5f, 0f, -15.5f), "Запас Спелых Апельсинов Чебурашки",
                "Тёплые солнечные апельсины для всего экипажа UES «Safe Travels». Пахнет детством и дружбой!", Color.Orange, "heal"));

            var derbyStand = new Entity(position: new(13.5f, 0f, -14.5f));
            new Entity(parent: derbyStand, model: "cube", scale: new(2.4f, 1.35f, 2.4f), position: new(0, 0.68f, 0), color: Rgb(225, 232, 245));
            new Entity(parent: derbyStand, model: "diamond", scale: new(1.3f, 2.1f, 0.65f), position: new(0, 2.25f, 0), color: Color.Gold, unlit: true);
            entitiesList.Add(derbyStand);
            Physics.AddSolidBox(13.5f, -14.5f, 2.4f, 2.4f, 3.3f, 0f, 0f, walkableTop: false);
            entitiesList.Add(new SafeTravelsRoomProp(new(13.5f, 0f, -14.5f), "Золотой Кубок «Арима Кинэн»",
                "Трофей Голд Шип с гравировкой: «Первое место в межзвёздном забеге от Луны до ангара за 11.8 секунды!»", Color.Gold, "victory"));

            var dmcDesk = new Entity(position: new(7.5f, 0f, 21.5f));
            new Entity(parent: dmcDesk, model: "cube", scale: new(3.8f, 1.05f, 1.9f), position: new(0, 0.52f, 0), color: Rgb(92, 46, 36));
            new Entity(parent: dmcDesk, model: "cube", scale: new(1.1f, 0.16f, 1.1f), position: new(-0.7f, 1.12f, 0.1f), rotation: new(0, 14f, 0), color: Rgb(232, 182, 102));
            new Entity(parent: dmcDesk, model: "cube", scale: new(2.8f, 0.55f, 0.14f), position: new(0, 2.55f, 0.8f), color: Rgb(245, 45, 75), unlit: true);
            entitiesList.Add(dmcDesk);
            Physics.AddSolidBox(7.5f, 21.5f, 3.8f, 1.9f, 1.2f, 0f, 0f, walkableTop: true);
            entitiesList.Add(new SafeTravelsRoomProp(new(7.5f, 0f, 21.5f), "Стол Агентства «Devil May Cry»",
                "Коробки пиццы с оливками (без маслин!), клубничное мороженое Данте и счёт за ремонт пластикового стула Вергилия.", Rgb(255, 65, 85), "parry"));

            // --- КРЫЛО КАЮТ (X >= +28): 6 ТЕМАТИЧЕСКИХ КАЮТ, ЛАЗАРЕТ, ЛАБОРАТОРИЯ И ВЕРСТАК СТИВА ---
            (Vector3 signPos, string title)[] cabinSigns =
            {
                (new(38f, 3.8f, 6.6f), "КАЮТА C1 :: ЛАЗАРЕТ И ХИРУРГИЯ"),
                (new(58f, 3.8f, 6.6f), "КАЮТА C2 :: КСЕНОФИЗИКА И МАГО-ИНЖЕНЕРИЯ"),
                (new(78f, 3.8f, 6.6f), "КАЮТА C3 :: ОТСЕК S.E.E.S., LIMBUS И OSIC"),
                (new(38f, 3.8f, -6.6f), "КАЮТА C4 :: ГОСТИНАЯ РОДОС АЙЛЕНД И СИРАКУЗ"),
                (new(58f, 3.8f, -6.6f), "КАЮТА C5 :: МАСТЕРСКАЯ СТИВА И ЯДРО ГАММА"),
                (new(78f, 3.8f, -6.6f), "КАЮТА C6 :: САД ИОНИИ И ОКЕАНОЛОГИЯ ДЖОТАРО")
            };
            foreach (var (cPos, cTitle) in cabinSigns)
                entitiesList.Add(new Entity(position: cPos, color: cyanNeon) { BillboardText = cTitle });

            // C1: Лазарет Медика
            var medBed = new Entity(position: new(38.5f, 0f, 26f));
            new Entity(parent: medBed, model: "cube", scale: new(4.2f, 0.95f, 2.2f), position: new(0, 0.48f, 0), color: Rgb(225, 235, 245));
            new Entity(parent: medBed, model: "cube", scale: new(1.2f, 2.4f, 1.2f), position: new(-2.8f, 1.2f, 0), color: Rgb(235, 55, 65), unlit: true);
            entitiesList.Add(medBed);
            Physics.AddSolidBox(38.5f, 26f, 4.2f, 2.2f, 0.95f, 0f, 0f, walkableTop: true);
            Physics.AddSolidBox(35.7f, 26f, 1.2f, 1.2f, 2.4f, 0f, 0f, walkableTop: false);
            entitiesList.Add(new SafeTravelsRoomProp(new(38.5f, 0f, 26f), "Хирургический Убер-Стол и Голубь Архимед",
                "Стерильность условная, зато заряд Убер-Пилы всегда 100%! На стойке воркует белоснежный голубь Архимед.", Rgb(255, 75, 85), "heal"));

            // C2: Ксенофизика Фримана и Кольца Изобретательницы
            var lab = new Entity(position: new(58.5f, 0f, 26f));
            new Entity(parent: lab, model: "cube", scale: new(2.2f, 2.8f, 1.2f), position: new(2.2f, 1.4f, 0), color: Rgb(235, 135, 35));
            new Entity(parent: lab, model: "diamond", scale: new(1.4f, 2.2f, 1.4f), position: new(-1.8f, 1.8f, 0), color: cyanNeon, unlit: true);
            entitiesList.Add(lab);
            Physics.AddSolidBox(60.7f, 26f, 2.2f, 1.2f, 2.8f, 0f, 0f, walkableTop: false);
            Physics.Obstacles.Add(new ObstacleCircle(56.7f, 26f, 0.75f, 2.9f, 0f));
            entitiesList.Add(new SafeTravelsRoomProp(new(58.5f, 0f, 26f), "Зарядник HEV Mk.IV и Кристалл Плазмы",
                "Монтировка Гордона Фримана аккуратно висит рядом с антигравитационными кольцами Высшего Суда.", Color.Orange, "laser"));

            // C3: S.E.E.S., Limbus и OSIC
            var seesPod = new Entity(position: new(78.5f, 0f, 26f));
            new Entity(parent: seesPod, model: "cube", scale: new(2.6f, 3.2f, 1.6f), position: new(2.0f, 1.6f, 0), color: Rgb(32, 68, 54));
            new Entity(parent: seesPod, model: "diamond", scale: new(1.8f, 1.8f, 0.35f), position: new(-2.0f, 1.9f, 0), color: Color.Gold, unlit: true);
            entitiesList.Add(seesPod);
            Physics.AddSolidBox(80.5f, 26f, 2.6f, 1.6f, 3.2f, 0f, 0f, walkableTop: false);
            Physics.Obstacles.Add(new ObstacleCircle(76.5f, 26f, 0.7f, 2.8f, 0f));
            entitiesList.Add(new SafeTravelsRoomProp(new(78.5f, 0f, 26f), "Док-Капсула Айгис и Журнал Мефистофеля",
                "Рядом с капсулой калибровки S.E.E.S. лежат путевые заметки Данте №10 и песочные часы сновидений Хейз.", Rgb(85, 255, 165), "dante_clock"));

            // C4: Гостиная Родос Айленд и Сиракуз
            var rhodes = new Entity(position: new(38.5f, 0f, -26f));
            new Entity(parent: rhodes, model: "cube", scale: new(3.6f, 1.0f, 2.2f), position: new(0, 0.5f, 0), color: Rgb(54, 42, 48));
            new Entity(parent: rhodes, model: "diamond", scale: new(0.65f, 0.85f, 0.65f), position: new(-0.8f, 1.35f, 0), color: Color.Red, unlit: true);
            entitiesList.Add(rhodes);
            Physics.AddSolidBox(38.5f, -26f, 3.6f, 2.2f, 1.0f, 0f, 0f, walkableTop: true);
            entitiesList.Add(new SafeTravelsRoomProp(new(38.5f, 0f, -26f), "Карточный Стол W и Стойка Клинков Лаппланд",
                "На столе разложены карты с детонаторами (учебными!) и рецепт миллефолье по-сиракузски от Лаппланд.", Rgb(235, 75, 95), "ui_tick"));

            // C5: Инженерная Мастерская Стива (функциональный Верстак!) и Ядро «Гамма»
            entitiesList.Add(new MinecraftWorkbench(new Vector3(54.5f, 0f, -25.5f)));
            var steveBed = new Entity(position: new(61.5f, 0f, -26.5f));
            new Entity(parent: steveBed, model: "cube", scale: new(2.2f, 0.65f, 3.4f), position: new(0, 0.32f, 0), color: Rgb(205, 42, 48));
            new Entity(parent: steveBed, model: "cube", scale: new(2.1f, 0.18f, 1.0f), position: new(0, 0.68f, 1.1f), color: Rgb(242, 242, 248));
            new Entity(parent: steveBed, model: "diamond", scale: new(1.6f, 1.8f, 1.6f), position: new(3.2f, 1.4f, 0.5f), color: amberNeon, unlit: true);
            entitiesList.Add(steveBed);
            Physics.AddSolidBox(61.5f, -26.5f, 2.2f, 3.4f, 0.77f, 0f, 0f, walkableTop: true);
            Physics.Obstacles.Add(new ObstacleCircle(64.7f, -26.0f, 0.8f, 2.3f, 0f));
            entitiesList.Add(new SafeTravelsRoomProp(new(61.5f, 0f, -26.5f), "Красная Кровать Стива и Реактор Ядра «Гамма»",
                "Точка возрождения установлена! Рядом гудит миниатюрный ториевый синтезатор Mindustry.", amberNeon, "build_place"));

            // C6: Сад Ионии, Ателье Гвен и Стол Океанологии Джотаро
            var garden = new Entity(position: new(78.5f, 0f, -26f));
            new Entity(parent: garden, model: "cube", scale: new(4.4f, 0.65f, 2.6f), position: new(0, 0.32f, 0), color: Rgb(96, 64, 78));
            new Entity(parent: garden, model: "sphere", scale: new(3.6f, 2.4f, 3.6f), position: new(1.2f, 2.8f, 0), color: Rgb(255, 145, 205), unlit: true);
            new Entity(parent: garden, model: "cube", scale: new(0.45f, 2.4f, 0.45f), position: new(1.2f, 1.2f, 0), color: Rgb(105, 68, 48));
            new Entity(parent: garden, model: "diamond", scale: new(0.85f, 1.35f, 0.85f), position: new(-1.6f, 1.4f, 0), color: cyanNeon, unlit: true);
            entitiesList.Add(garden);
            Physics.AddSolidBox(78.5f, -26f, 4.4f, 2.6f, 0.65f, 0f, 0f, walkableTop: true);
            Physics.Obstacles.Add(new ObstacleCircle(79.7f, -26f, 0.4f, 2.4f, 0f));
            Physics.Obstacles.Add(new ObstacleCircle(79.7f, -26f, 1.7f, 2.4f, 1.6f));
            Physics.Obstacles.Add(new ObstacleCircle(76.9f, -26f, 0.45f, 2.1f, 0f));
            entitiesList.Add(new SafeTravelsRoomProp(new(78.5f, 0f, -26f), "Бонсай Духовного Цветения, Катушки Гвен и Атлас Дельфинов",
                "Под лепестками Ионийской сакуры лежат голубые выкройки Гвен и диссертация Джотаро Куджо о морских звёздах.", Rgb(255, 145, 205), "scissor_snip"));

            // --- КРЫЛО БОЕВОГО ПОЛИГОНА (X <= -28): 5 МАНЕКЕНОВ + КОНСОЛЬ DPS + 9 СТАНЦИЙ ПЕСОЧНИЦЫ ---
            entitiesList.Add(new Entity(model: "cube", scale: new(0.55f, 0.05f, 66f), position: new(-61f, 0.03f, 0f), color: amberNeon, unlit: true));
            entitiesList.Add(new SafeTravelsDpsConsole(new Vector3(-56.5f, 0f, 0f), 90f));

            void SpawnHubDummy(Vector3 dPos, string enemyType, string dTitle, float customHp)
            {
                var dummy = new CombatTarget(dPos, enemyType, 1.0f);
                dummy.ConfigureAsTrainingDummy(dTitle, customHp, 90f);
                CombatManager.Enemies.Add(dummy);
                entitiesList.Add(dummy);
            }

            SpawnHubDummy(new Vector3(-71.0f, 0f, -15.5f), "combine_soldier", "МАНЕКЕН ПЕХОТЫ UES", 2500f);
            SpawnHubDummy(new Vector3(-73.5f, 0f, 0.0f), "boss", "БРОНИРОВАННЫЙ МАНЕКЕН-ТИТАН", 10000f);
            SpawnHubDummy(new Vector3(-70.0f, 0f, 14.0f), "lemurian", "РОЕВОЙ МАНЕКЕН #1", 1500f);
            SpawnHubDummy(new Vector3(-72.3f, 0f, 16.0f), "beetle", "РОЕВОЙ МАНЕКЕН #2", 1500f);
            SpawnHubDummy(new Vector3(-70.2f, 0f, 18.0f), "lemurian", "РОЕВОЙ МАНЕКЕН #3", 1500f);

            string[] itemStations = { "item_white", "item_green", "item_red", "item_void", "item_lunar" };
            for (int i = 0; i < itemStations.Length; i++)
                entitiesList.Add(new SafeTravelsSandboxStation(new Vector3(-35f - i * 5.5f, 0f, 27f), itemStations[i], 180f));

            entitiesList.Add(new SafeTravelsSandboxStation(new Vector3(-35.0f, 0f, -27f), "clear_items", 0f));
            entitiesList.Add(new SafeTravelsSandboxStation(new Vector3(-42.0f, 0f, -27f), "recharge", 0f));
            entitiesList.Add(new SafeTravelsSandboxStation(new Vector3(-49.0f, 0f, -27f), "zero_cd", 0f));
            entitiesList.Add(new SafeTravelsSandboxStation(new Vector3(-56.5f, 0f, -27f), "char_pod", 0f));

            // --- ЮЖНЫЙ ДЕСАНТНЫЙ АНГАР: КАПСУЛА НОВОГО ЗАБЕГА И ШЛЮЗ В ГЛАВНОЕ МЕНЮ ---
            for (int chv = -2; chv <= 2; chv++)
                entitiesList.Add(new Entity(model: "cube", scale: new(9.0f, 0.05f, 0.42f), position: new(0f, 0.03f, -34f + chv * 4.5f), color: amberNeon, unlit: true));
            entitiesList.Add(new SafeTravelsDropPod(new Vector3(0f, 0f, -47f)));
            entitiesList.Add(new SafeTravelsExitAirlock(new Vector3(-14.5f, 0f, -63.5f), 0f));
        }

        private static void PopulateSafeTravelsCrew(List<Entity> entitiesList)
        {
            (string key, Vector3 pos, float yaw, string zone, string emote)[] placements =
            {
                // Северное Крыло — Капитанский Мостик и Смотровая Палуба (7)
                ("commando", new(-5.5f, 0f, 51.5f), 145f, "Капитанский Мостик", ""),
                ("m4a1", new(5.5f, 0f, 51.5f), 215f, "Тактическая Связь Мостика", ""),
                ("enforcer", new(-8.5f, 0f, 34.0f), 180f, "Пост Охраны Мостика", ""),
                ("buzz", new(15.5f, 0f, 58.0f), 220f, "Звёздная Навигация", ""),
                ("buzz_zurg", new(-15.5f, 0f, 58.0f), 140f, "Смотровая Палуба", ""),
                ("huntress", new(0.0f, 0f, 64.5f), 180f, "Панорамный Иллюминатор", ""),
                ("sanabi", new(-20.0f, 0f, 42.0f), 95f, "Зашифрованный Архив «МАГО»", ""),

                // Центральный Зал — Кают-Компания, Бар «Бездна», Сцена и Зона Отдыха (14)
                ("sol", new(-14.5f, 0f, 13.5f), 110f, "Бар «Бездна»", ""),
                ("sol_ky", new(-14.5f, 0f, 8.0f), 75f, "Бар «Бездна»", ""),
                ("jack_o", new(-12.5f, 0f, 16.5f), 135f, "Бар «Бездна»", ""),
                ("tidehunter", new(-16.5f, 0f, 20.5f), 150f, "Бар «Бездна»", ""),
                ("slark", new(-11.0f, 0f, 22.0f), 175f, "Теневой Угол Кают-Компании", ""),
                ("pushnoy", new(16.8f, 0f, 15.0f), 235f, "Рок-Сцена «Галилео»", ""),
                ("freddy", new(17.2f, 0f, 9.8f), 255f, "Сцена и Аркадные Автоматы", ""),
                ("reimu", new(-16.5f, 0f, -9.2f), 15f, "Уголок Котацу Хакурей", ""),
                ("cheburashka", new(-9.5f, 0f, -12.0f), 20f, "Уголок Спелых Апельсинов", ""),
                ("gold_ship", new(10.0f, 0f, -13.5f), 325f, "Стенд Чемпионов Трейсен", ""),
                ("scout", new(17.5f, 0f, -12.5f), 300f, "Зона Отдыха Экипажа", "california"),
                ("dmc_trio", new(7.5f, 0f, 18.0f), 185f, "Стол Агентства Devil May Cry", ""),
                ("dmc_nero", new(12.2f, 0f, 19.5f), 215f, "Агентство Devil May Cry", ""),
                ("dmc_vergil", new(1.8f, 0f, 21.5f), 180f, "Пластиковый Стул Мотивации", ""),

                // Крыло Кают Экипажа (X >= +28) — Жилые Каюты, Лазарет, Лаборатории и Мастерская (16)
                ("vyper", new(68.0f, 0f, 2.6f), 180f, "Коридор Кают", ""),
                ("medic", new(38.5f, 0f, 19.5f), 180f, "Каюта C1: Лазарет", ""),
                ("freeman", new(55.0f, 0f, 19.5f), 165f, "Каюта C2: Отсек Ксенофизики", ""),
                ("artificer", new(62.0f, 0f, 19.5f), 195f, "Каюта C2: Высший Суд", ""),
                ("aigis", new(74.5f, 0f, 20.5f), 160f, "Каюта C3: Отсек S.E.E.S.", ""),
                ("dante", new(82.5f, 0f, 20.5f), 205f, "Каюта C3: Кабинет Limbus", ""),
                ("haze", new(83.0f, 0f, 12.5f), 240f, "Каюта C3: Архив OSIC", ""),
                ("lappland", new(34.5f, 0f, -18.5f), 25f, "Каюта C4: Гостиная Сиракуз", ""),
                ("lappland_decadenza", new(34.0f, 0f, -24.5f), 55f, "Каюта C4: Гостиная Сиракуз", ""),
                ("w_arknights", new(42.5f, 0f, -18.5f), 335f, "Каюта C4: Родос Айленд", ""),
                ("w_wisadel", new(43.5f, 0f, -24.5f), 305f, "Каюта C4: Трон Вишадель", ""),
                ("steve", new(55.0f, 0f, -18.5f), 10f, "Каюта C5: Мастерская Стива", ""),
                ("mindustry", new(62.5f, 0f, -18.5f), 335f, "Каюта C5: Док Ядра «Гамма»", ""),
                ("ahri", new(74.5f, 0f, -19.5f), 25f, "Каюта C6: Сад Ионии", ""),
                ("gwen", new(82.5f, 0f, -19.5f), 330f, "Каюта C6: Ателье Тумана", ""),
                ("jotaro", new(83.0f, 0f, -12.5f), 285f, "Каюта C6: Океанология", ""),

                // Крыло Боевого Полигона (X <= -28) — Огневой Рубеж и Испытание Оружия (2)
                ("v1", new(-49.5f, 0f, 12.5f), 125f, "Огневой Рубеж Полигона", ""),
                ("v1_v2", new(-49.5f, 0f, -12.5f), 55f, "Огневой Рубеж Полигона", "")
            };

            var spawnedSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (key, pos, yaw, zone, emote) in placements)
            {
                if (spawnedSet.Add(key))
                    entitiesList.Add(new SafeTravelsNpc(PlaceCrewMember(pos, key == "dmc_vergil" ? 0.9f : 0.6f), key, yaw, zone, emote));
            }

            PopulateExpansionSafeTravelsCrew(entitiesList, spawnedSet);

            // Страховочная проверка: гарантируем присутствие всех доступных персон из SafeTravelsDialogues.AllPersonaKeys
            int extraIdx = 0;
            foreach (var pk in SafeTravelsDialogues.AllPersonaKeys)
            {
                if ((pk is "bird_trio" or "bird_long" or "bird_small") && !ProgressData.Instance.IsBirdTrioAuthorized())
                    continue;
                if (pk == "apocalypse_bird" && !ProgressData.Instance.IsCharacterUnlocked("apocalypse_bird"))
                    continue;
                if (spawnedSet.Add(pk))
                {
                    float ang = extraIdx * 0.85f;
                    Vector3 fallbackPos = new(MathF.Cos(ang) * 9.5f, 0f, MathF.Sin(ang) * 9.5f);
                    entitiesList.Add(new SafeTravelsNpc(PlaceCrewMember(fallbackPos, 0.6f), pk, 180f, "Кают-Компания", ""));
                    extraIdx++;
                }
            }
        }

        /// <summary>
        /// Член экипажа ставится в задуманную точку, если она свободна; иначе — в ближайшее свободное место
        /// (не внутри мебели, переборок и других персонажей). Высота берётся по реальной поверхности (подиум, ступень).
        /// </summary>
        private static Vector3 PlaceCrewMember(Vector3 desired, float footprint)
        {
            Vector3 p = Physics.FindClearNearbyPos(desired, footprint, 2.2f, false, 0f, 0.25f, 8f, 1.0f);
            Physics.RegisterSpawnBlocker(p, footprint, 2.2f);
            return p;
        }
    }
}