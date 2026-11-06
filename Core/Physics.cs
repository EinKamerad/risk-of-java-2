using System;
using System.Collections.Generic;
using System.Numerics;
using RiskOfCSharp2.Models;

namespace RiskOfCSharp2.Core
{
    public struct SkyPlatform
    {
        public float X, Z, Radius, TopY, Thickness;
        public SkyPlatform(float x, float z, float radius, float topY, float thickness = 2.0f)
        {
            X = x; Z = z; Radius = radius; TopY = topY; Thickness = thickness;
        }
    }

    public struct PlatformBox
    {
        public float X, Z, HalfX, HalfZ, TopY, Thickness;
        public PlatformBox(float x, float z, float halfX, float halfZ, float topY, float thickness = 2.0f)
        {
            X = x; Z = z; HalfX = halfX; HalfZ = halfZ; TopY = topY; Thickness = thickness;
        }
        public bool Contains(float px, float pz, float pad = 0f) =>
            MathF.Abs(px - X) <= HalfX + pad && MathF.Abs(pz - Z) <= HalfZ + pad;
    }

    public struct FloorHole
    {
        public float MinX, MaxX, MinZ, MaxZ, BottomY;
        public FloorHole(float minX, float maxX, float minZ, float maxZ, float bottomY = -12.0f)
        {
            MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ; BottomY = bottomY;
        }
        public bool Contains(float x, float z) => x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;
    }

    public struct SpawnBlocker
    {
        public float X, Z, Radius, BaseY, TopY;
        public SpawnBlocker(float x, float z, float radius, float baseY, float height)
        {
            X = x;
            Z = z;
            Radius = radius;
            BaseY = baseY;
            TopY = baseY + height;
        }
    }

    public static class Physics
    {
        public const float ColosseumY = 120.0f;
        public const float ColosseumRadius = 56.0f;

        public static readonly List<ObstacleCircle> Obstacles = new();
        public static readonly List<ObstacleBox> BoxObstacles = new();
        public static readonly List<SkyPlatform> SkyPlatforms = new();
        public static readonly List<PlatformBox> Platforms = new();
        public static readonly List<FloorHole> FloorHoles = new();
        public static readonly List<SpawnBlocker> SpawnBlockers = new();
        public static string? SpecialMode = null;
        public static float WorldBound = 58.0f;
        public static float WorldBoundRadius = 0.0f;

        public static void ResetStagePhysics()
        {
            Obstacles.Clear();
            BoxObstacles.Clear();
            SkyPlatforms.Clear();
            Platforms.Clear();
            FloorHoles.Clear();
            SpawnBlockers.Clear();
            WorldBoundRadius = 0.0f;
        }

        public static float Distance2D(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        public static float Distance2DToBox(float wx, float wz, in ObstacleBox box)
        {
            float dx = wx - box.X;
            float dz = wz - box.Z;
            float lx = dx * box.CosYaw - dz * box.SinYaw;
            float lz = dx * box.SinYaw + dz * box.CosYaw;
            float ox = MathF.Max(0f, MathF.Abs(lx) - box.HalfX);
            float oz = MathF.Max(0f, MathF.Abs(lz) - box.HalfZ);
            return MathF.Sqrt(ox * ox + oz * oz);
        }

        private static void ResolveBoxCollision2D(ref float rx, ref float rz, float radius, in ObstacleBox box)
        {
            float dx = rx - box.X;
            float dz = rz - box.Z;
            float lx = dx * box.CosYaw - dz * box.SinYaw;
            float lz = dx * box.SinYaw + dz * box.CosYaw;

            float cx = Math.Clamp(lx, -box.HalfX, box.HalfX);
            float cz = Math.Clamp(lz, -box.HalfZ, box.HalfZ);

            if (lx != cx || lz != cz)
            {
                float ox = lx - cx;
                float oz = lz - cz;
                float distSq = ox * ox + oz * oz;
                if (distSq < radius * radius)
                {
                    float dist = MathF.Sqrt(distSq);
                    float nlx, nlz;
                    if (dist > 0.0001f)
                    {
                        float push = (radius - dist) / dist;
                        nlx = lx + ox * push;
                        nlz = lz + oz * push;
                    }
                    else
                    {
                        float signX = lx >= 0f ? 1f : -1f;
                        nlx = signX * (box.HalfX + radius);
                        nlz = lz;
                    }
                    rx = box.X + nlx * box.CosYaw + nlz * box.SinYaw;
                    rz = box.Z - nlx * box.SinYaw + nlz * box.CosYaw;
                }
            }
            else
            {
                float penX = box.HalfX - MathF.Abs(lx);
                float penZ = box.HalfZ - MathF.Abs(lz);
                float nlx, nlz;
                if (penX <= penZ)
                {
                    float signX = lx >= 0f ? 1f : -1f;
                    nlx = signX * (box.HalfX + radius);
                    nlz = lz;
                }
                else
                {
                    float signZ = lz >= 0f ? 1f : -1f;
                    nlx = lx;
                    nlz = signZ * (box.HalfZ + radius);
                }
                rx = box.X + nlx * box.CosYaw + nlz * box.SinYaw;
                rz = box.Z - nlx * box.SinYaw + nlz * box.CosYaw;
            }
        }

        /// <summary>
        /// Регистрирует прямоугольный (опционально повёрнутый) твёрдый коллайдер, точно повторяющий визуальный куб.
        /// sizeX / sizeZ — полные размеры по локальным осям (как scale у Entity), yawDeg — как RotationY у Entity.
        /// </summary>
        public static void AddSolidBox(float cx, float cz, float sizeX, float sizeZ, float height,
            float yawDeg = 0f, float baseY = 0f, bool walkableTop = true)
        {
            BoxObstacles.Add(new ObstacleBox(cx, cz, sizeX * 0.5f, sizeZ * 0.5f, yawDeg, height, baseY, walkableTop));
        }

        private static bool IsFootprintInsideBox(float wx, float wz, float footprint, in ObstacleBox box)
        {
            float dx = wx - box.X;
            float dz = wz - box.Z;
            float lx = dx * box.CosYaw - dz * box.SinYaw;
            float lz = dx * box.SinYaw + dz * box.CosYaw;
            return MathF.Abs(lx) + footprint <= box.HalfX && MathF.Abs(lz) + footprint <= box.HalfZ;
        }

        /// <summary>Находится ли точка внутри палуб крейсера «Safe Travels» (с отступом pad от переборок).</summary>
        public static bool IsInsideSafeTravelsHull(float x, float z, float pad)
        {
            ReadOnlySpan<(float minX, float maxX, float minZ, float maxZ)> zones = stackalloc (float, float, float, float)[]
            {
                (-28f + pad, 28f - pad, -22f + pad, 28f - pad),
                (-30f + pad, 30f - pad, 28f + pad, 70f - pad),
                (-24f + pad, 24f - pad, -66f + pad, -22f - pad),
                (-86f + pad, -28f - pad, -36f + pad, 36f - pad),
                (28f + pad, 88f - pad, -32f + pad, 32f - pad)
            };
            for (int i = 0; i < zones.Length; i++)
            {
                var zn = zones[i];
                if (x >= zn.minX && x <= zn.maxX && z >= zn.minZ && z <= zn.maxZ)
                    return true;
            }
            return false;
        }

        /// <summary>Зажимает точку внутрь игровой зоны текущего этапа (квадратная граница + круговая граница) с отступом.</summary>
        public static void ClampToArena(ref float x, ref float z, float margin)
        {
            float maxSq = MathF.Max(2.0f, WorldBound - margin);
            x = Math.Clamp(x, -maxSq, maxSq);
            z = Math.Clamp(z, -maxSq, maxSq);
            if (WorldBoundRadius > 0f)
            {
                float maxR = MathF.Max(2.0f, WorldBoundRadius - margin);
                float d = MathF.Sqrt(x * x + z * z);
                if (d > maxR && d > 0.001f)
                {
                    float s = maxR / d;
                    x *= s;
                    z *= s;
                }
            }
        }

        /// <summary>
        /// Безопасная точка спавна для статичного объекта (узлы испытаний, порталы, постаменты):
        /// внутри арены, не в стенах/скалах/колоннах, не внутри других объектов и не над ямами.
        /// </summary>
        public static Vector3 FindSafeSpawnPoint(Vector3 desired, float margin = 6f, float footprint = 1.0f,
            float height = 2.5f, bool register = false, float extraPadding = 0.35f)
        {
            float x = desired.X, z = desired.Z;
            ClampToArena(ref x, ref z, margin);
            Vector3 p = FindClearNearbyPos(new Vector3(x, desired.Y, z), footprint, height, true, 0f,
                extraPadding, 16f, MathF.Min(margin, 3.0f));
            if (register)
                RegisterSpawnBlocker(p, footprint, height);
            return p;
        }

        /// <summary>Свободна ли точка для существа радиуса radius (не внутри коллайдеров, внутри границ, не над ямой).</summary>
        public static bool IsActorSpotFree(float x, float y, float z, float radius)
        {
            for (int i = 0; i < FloorHoles.Count; i++)
                if (FloorHoles[i].Contains(x, z)) return false;
            Vector3 res = ResolveHorizontalCollision(new Vector3(x, y, z), radius, y);
            float dx = res.X - x, dz = res.Z - z;
            if (dx * dx + dz * dz > 0.0025f) return false;
            return !IsInsideSolidObstacle(new Vector3(x, y + 0.9f, z), radius * 0.6f);
        }

        /// <summary>
        /// Ищет ближайшую к желаемой свободную точку для появления монстра/босса (без застревания в скалах и стенах).
        /// </summary>
        public static Vector3 FindFreeActorPos(Vector3 desired, float radius = 0.9f, float maxSearch = 12f, float margin = 4f)
        {
            float x = desired.X, z = desired.Z;
            ClampToArena(ref x, ref z, margin);
            for (float r = 0f; r <= maxSearch; r += 1.5f)
            {
                int dirs = r <= 0.01f ? 1 : 12;
                for (int d = 0; d < dirs; d++)
                {
                    float ang = (d / (float)dirs) * MathF.PI * 2f + r * 0.37f;
                    float cx = x + MathF.Cos(ang) * r;
                    float cz = z + MathF.Sin(ang) * r;
                    ClampToArena(ref cx, ref cz, margin);
                    float cy = GetSurfaceHeight(cx, cz, desired.Y + 1.0f);
                    if (SpecialMode == "safe_travels" && !IsInsideSafeTravelsHull(cx, cz, radius + 0.5f))
                        continue;
                    if (IsActorSpotFree(cx, cy, cz, radius))
                        return new Vector3(cx, cy, cz);
                }
            }
            Vector3 fb = ResolveHorizontalCollision(new Vector3(x, desired.Y, z), radius + 0.3f, desired.Y);
            fb.Y = GetSurfaceHeight(fb.X, fb.Z, desired.Y + 1.0f);
            return fb;
        }

        /// <summary>
        /// Пошаговая проверка отрезка движения (снаряды, лучи) — защищает от «пролёта» быстрых объектов сквозь тонкие стены.
        /// </summary>
        public static bool SegmentHitsSolid(Vector3 from, Vector3 to, float radius, out Vector3 hitPos)
        {
            Vector3 diff = to - from;
            float len = diff.Length();
            int steps = Math.Clamp((int)MathF.Ceiling(len / 0.3f), 1, 64);
            Vector3 last = from;
            for (int i = 1; i <= steps; i++)
            {
                Vector3 sample = from + diff * (i / (float)steps);
                if (IsInsideSolidObstacle(sample, radius))
                {
                    hitPos = last;
                    return true;
                }
                last = sample;
            }
            hitPos = to;
            return false;
        }

        private static void ClampSafeTravelsHull(ref float rx, ref float rz, float radius)
        {
            float pad = radius + 0.46f;
            // 5 палубных крыльев крейсера + 4 дверных перехода между ними
            ReadOnlySpan<(float minX, float maxX, float minZ, float maxZ)> zones = stackalloc (float, float, float, float)[]
            {
                (-28f + pad, 28f - pad, -22f + pad, 28f - pad),    // Центральный Зал
                (-30f + pad, 30f - pad, 28f + pad, 70f - pad),     // Северный Мостик
                (-10f + pad, 10f - pad, 26f, 30f),                 // Проём Мостика
                (-24f + pad, 24f - pad, -66f + pad, -22f - pad),   // Южный Ангар
                (-9f + pad, 9f - pad, -24f, -20f),                 // Проём Ангара
                (-86f + pad, -28f - pad, -36f + pad, 36f - pad),   // Крыло Полигона
                (-30f, -26f, -9f + pad, 9f - pad),                 // Проём Полигона
                (28f + pad, 88f - pad, -32f + pad, 32f - pad),     // Крыло Кают
                (26f, 30f, -6f + pad, 6f - pad)                    // Проём Кают
            };

            for (int i = 0; i < zones.Length; i++)
            {
                var z = zones[i];
                if (rx >= z.minX && rx <= z.maxX && rz >= z.minZ && rz <= z.maxZ)
                    return;
            }

            float bestDistSq = float.MaxValue;
            float bestX = rx, bestZ = rz;
            for (int i = 0; i < zones.Length; i++)
            {
                var z = zones[i];
                float cx = Math.Clamp(rx, z.minX, z.maxX);
                float cz = Math.Clamp(rz, z.minZ, z.maxZ);
                float d2 = (rx - cx) * (rx - cx) + (rz - cz) * (rz - cz);
                if (d2 < bestDistSq)
                {
                    bestDistSq = d2;
                    bestX = cx;
                    bestZ = cz;
                }
            }
            rx = bestX;
            rz = bestZ;
        }

        /// <summary>
        /// Возвращает высоту поверхности рельефа и небесных террас (игнорируя деревья, башни, камни и пропсы),
        /// чтобы сундуки, алтари, монументы и телепорт никогда не спавнились на верхушках деревьев или внутри башен.
        /// </summary>
        public static float GetTerrainSpawnHeight(float x, float z)
        {
            float maxH = 0.0f;
            for (int i = 0; i < FloorHoles.Count; i++)
            {
                if (FloorHoles[i].Contains(x, z))
                {
                    maxH = FloorHoles[i].BottomY;
                    break;
                }
            }

            for (int i = 0; i < SkyPlatforms.Count; i++)
            {
                var plat = SkyPlatforms[i];
                float dx = x - plat.X, dz = z - plat.Z;
                if (MathF.Sqrt(dx * dx + dz * dz) <= plat.Radius - 1.1f)
                {
                    maxH = MathF.Max(maxH, plat.TopY);
                }
            }

            for (int i = 0; i < Platforms.Count; i++)
            {
                var box = Platforms[i];
                if (box.Contains(x, z, -0.25f))
                {
                    maxH = MathF.Max(maxH, box.TopY);
                }
            }

            return maxH;
        }

        public static float GetSurfaceHeight(float x, float z, float currentY, float? prevY = null)
        {
            float checkY = prevY.HasValue ? MathF.Max(currentY, prevY.Value) : currentY;
            float maxH = 0.0f;
            for (int i = 0; i < FloorHoles.Count; i++)
            {
                if (FloorHoles[i].Contains(x, z))
                {
                    maxH = FloorHoles[i].BottomY;
                    break;
                }
            }

            for (int i = 0; i < Obstacles.Count; i++)
            {
                var obs = Obstacles[i];
                float dx = x - obs.X, dz = z - obs.Z;
                if (MathF.Sqrt(dx * dx + dz * dz) <= obs.Radius + 0.35f)
                {
                    if (checkY >= obs.TopHeight - 1.2f)
                        maxH = MathF.Max(maxH, obs.TopHeight);
                }
            }

            for (int i = 0; i < BoxObstacles.Count; i++)
            {
                var box = BoxObstacles[i];
                if (!box.WalkableTop) continue;
                if (Distance2DToBox(x, z, in box) <= 0.32f)
                {
                    if (checkY >= box.TopHeight - 1.2f)
                        maxH = MathF.Max(maxH, box.TopHeight);
                }
            }

            for (int i = 0; i < SkyPlatforms.Count; i++)
            {
                var plat = SkyPlatforms[i];
                float dx = x - plat.X, dz = z - plat.Z;
                if (MathF.Sqrt(dx * dx + dz * dz) <= plat.Radius)
                {
                    if (checkY >= plat.TopY - 4.5f)
                        maxH = MathF.Max(maxH, plat.TopY);
                }
            }

            for (int i = 0; i < Platforms.Count; i++)
            {
                var box = Platforms[i];
                if (box.Contains(x, z, 0.25f))
                {
                    if (checkY >= box.TopY - 0.85f)
                        maxH = MathF.Max(maxH, box.TopY);
                }
            }

            if (SpecialMode == "moon" && checkY >= 108.0f)
            {
                if (MathF.Sqrt(x * x + z * z) <= 56.5f)
                    maxH = MathF.Max(maxH, ColosseumY);
            }

            return maxH;
        }

        public static Vector3 ResolveHorizontalCollision(Vector3 pos, float radius = 0.7f, float entityY = 0.0f)
        {
            float rx = pos.X, rz = pos.Z;
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 0; i < Obstacles.Count; i++)
                {
                    var obs = Obstacles[i];
                    if (entityY >= obs.TopHeight - 0.35f || entityY + 1.8f < obs.BaseY)
                        continue;

                    float dx = rx - obs.X;
                    float dz = rz - obs.Z;
                    float dist = MathF.Sqrt(dx * dx + dz * dz);
                    float minD = radius + obs.Radius;
                    if (dist < minD)
                    {
                        if (dist < 0.001f) rx += minD;
                        else
                        {
                            float push = (minD - dist) / dist;
                            rx += dx * push;
                            rz += dz * push;
                        }
                    }
                }

                for (int i = 0; i < BoxObstacles.Count; i++)
                {
                    var box = BoxObstacles[i];
                    if (entityY >= box.TopHeight - 0.35f || entityY + 1.8f < box.BaseY)
                        continue;

                    ResolveBoxCollision2D(ref rx, ref rz, radius, in box);
                }

                // Боковое выталкивание от толстых парящих платформ и бастионов
                for (int i = 0; i < SkyPlatforms.Count; i++)
                {
                    var plat = SkyPlatforms[i];
                    float bottomY = plat.TopY - plat.Thickness;
                    if (entityY >= plat.TopY - 0.45f || entityY + 1.8f < bottomY)
                        continue;

                    float dx = rx - plat.X;
                    float dz = rz - plat.Z;
                    float dist = MathF.Sqrt(dx * dx + dz * dz);
                    float minD = radius + plat.Radius;
                    if (dist < minD && dist > 0.001f)
                    {
                        float push = (minD - dist) / dist;
                        rx += dx * push;
                        rz += dz * push;
                    }
                }

                // Боковое выталкивание от прямоугольных палуб (PlatformBox), если персонаж ниже ступеньки
                for (int i = 0; i < Platforms.Count; i++)
                {
                    var box = Platforms[i];
                    float bottomY = box.TopY - box.Thickness;
                    if (entityY >= box.TopY - 0.70f || entityY + 1.8f < bottomY)
                        continue;

                    float dx = rx - box.X;
                    float dz = rz - box.Z;
                    float ox = (box.HalfX + radius) - MathF.Abs(dx);
                    float oz = (box.HalfZ + radius) - MathF.Abs(dz);
                    if (ox > 0f && oz > 0f)
                    {
                        if (ox < oz)
                            rx += dx >= 0f ? ox : -ox;
                        else
                            rz += dz >= 0f ? oz : -oz;
                    }
                }

                // Удержание внутри вырытых ям пола (FloorHoles), если персонаж/враг ниже края пола
                if (entityY < -0.55f && FloorHoles.Count > 0)
                {
                    bool inAnyHole = false;
                    int nearestHole = -1;
                    float bestHoleDist = 9999f;
                    for (int i = 0; i < FloorHoles.Count; i++)
                    {
                        var h = FloorHoles[i];
                        if (rx >= h.MinX + radius * 0.4f && rx <= h.MaxX - radius * 0.4f &&
                            rz >= h.MinZ + radius * 0.4f && rz <= h.MaxZ - radius * 0.4f)
                        {
                            inAnyHole = true;
                            break;
                        }
                        float cx = (h.MinX + h.MaxX) * 0.5f;
                        float cz = (h.MinZ + h.MaxZ) * 0.5f;
                        float d2 = (rx - cx) * (rx - cx) + (rz - cz) * (rz - cz);
                        if (d2 < bestHoleDist) { bestHoleDist = d2; nearestHole = i; }
                    }
                    if (!inAnyHole && nearestHole >= 0)
                    {
                        var h = FloorHoles[nearestHole];
                        float pad = radius * 0.42f;
                        if (rx >= h.MinX - 1.5f && rx <= h.MaxX + 1.5f && rz >= h.MinZ - 1.5f && rz <= h.MaxZ + 1.5f)
                        {
                            rx = Math.Clamp(rx, h.MinX + pad, h.MaxX - pad);
                            rz = Math.Clamp(rz, h.MinZ + pad, h.MaxZ - pad);
                        }
                    }
                }

                // Аналитическое удержание внутри кругового периметра внешних стен / скал
                if (SpecialMode == "moon" && entityY >= 105.0f)
                {
                    float maxColR = MathF.Max(1.0f, ColosseumRadius - 2.8f - radius);
                    float colD = MathF.Sqrt(rx * rx + rz * rz);
                    if (colD > maxColR && colD > 0.001f)
                    {
                        float s = maxColR / colD;
                        rx *= s;
                        rz *= s;
                    }
                }
                else if (WorldBoundRadius > 0.0f)
                {
                    float maxR = MathF.Max(1.0f, WorldBoundRadius - radius);
                    float d = MathF.Sqrt(rx * rx + rz * rz);
                    if (d > maxR && d > 0.001f)
                    {
                        float s = maxR / d;
                        rx *= s;
                        rz *= s;
                    }
                }

                if (SpecialMode == "safe_travels")
                {
                    ClampSafeTravelsHull(ref rx, ref rz, radius);
                }

                float maxSq = MathF.Max(2.0f, WorldBound - radius);
                rx = Math.Clamp(rx, -maxSq, maxSq);
                rz = Math.Clamp(rz, -maxSq, maxSq);
            }

            return new Vector3(rx, pos.Y, rz);
        }

        /// <summary>
        /// Пошаговая трассировка перемещения (защита от пролёта сквозь стены на рывках, телепортах, крюках и высокой скорости).
        /// </summary>
        public static Vector3 SweepPlayerMovement(Vector3 fromPos, Vector3 toPos, float radius = 0.72f, float entityY = 0.0f)
        {
            float dx = toPos.X - fromPos.X;
            float dz = toPos.Z - fromPos.Z;
            float dist2D = MathF.Sqrt(dx * dx + dz * dz);
            if (dist2D <= 0.40f)
            {
                Vector3 single = ResolveHorizontalCollision(toPos, radius, entityY);
                return new Vector3(single.X, toPos.Y, single.Z);
            }

            int steps = Math.Clamp((int)MathF.Ceiling(dist2D / 0.38f), 2, 96);
            float stepX = dx / steps;
            float stepZ = dz / steps;
            float curX = fromPos.X;
            float curZ = fromPos.Z;
            float curY = fromPos.Y;
            float stepY = (toPos.Y - fromPos.Y) / steps;

            for (int s = 1; s <= steps; s++)
            {
                float targetX = curX + stepX;
                float targetZ = curZ + stepZ;
                curY += stepY;
                float testY = MathF.Min(entityY, curY);
                Vector3 resolved = ResolveHorizontalCollision(new Vector3(targetX, curY, targetZ), radius, testY);
                curX = resolved.X;
                curZ = resolved.Z;
            }

            return new Vector3(curX, toPos.Y, curZ);
        }

        public static Vector3 ResolveCameraCollision(Vector3 headOrigin, Vector3 shoulderPivot, Vector3 desiredCamPos, float camRadius = 0.48f)
        {
            // 1. Проверяем плечевое смещение от головы персонажа до точки пивота камеры
            Vector3 safePivot = headOrigin;
            const int pivotSteps = 6;
            for (int i = 1; i <= pivotSteps; i++)
            {
                Vector3 sample = Vector3.Lerp(headOrigin, shoulderPivot, i / (float)pivotSteps);
                if (IsInsideSolidObstacle(sample, camRadius * 0.75f))
                    break;
                safePivot = sample;
            }

            // 2. Трассировка луча от пивота до желаемой точки камеры с защитой от входа внутрь колонн, скал, платформ и стен
            Vector3 safeCam = safePivot;
            const int raySteps = 28;
            for (int i = 1; i <= raySteps; i++)
            {
                float t = i / (float)raySteps;
                Vector3 sample = Vector3.Lerp(safePivot, desiredCamPos, t);
                float surfH = GetSurfaceHeight(sample.X, sample.Z, sample.Y);
                if (sample.Y < surfH + 0.42f || IsInsideSolidObstacle(sample, camRadius))
                    break;
                safeCam = sample;
            }

            // 3. Дополнительное выталкивание сферы камеры из близлежащих препятствий и пола
            Vector3 pushed = ResolveHorizontalCollision(safeCam, camRadius, safeCam.Y - 0.65f);
            safeCam.X = pushed.X;
            safeCam.Z = pushed.Z;
            float camFloor = GetSurfaceHeight(safeCam.X, safeCam.Z, safeCam.Y) + 0.50f;
            if (safeCam.Y < camFloor) safeCam.Y = camFloor;

            return safeCam;
        }

        public static bool IsInsideSolidObstacle(Vector3 pos, float radius = 0.2f)
        {
            for (int i = 0; i < Obstacles.Count; i++)
            {
                var obs = Obstacles[i];
                if (pos.Y >= obs.BaseY && pos.Y <= obs.TopHeight)
                {
                    float dx = pos.X - obs.X, dz = pos.Z - obs.Z;
                    if (MathF.Sqrt(dx * dx + dz * dz) <= obs.Radius + radius)
                        return true;
                }
            }

            for (int i = 0; i < BoxObstacles.Count; i++)
            {
                var box = BoxObstacles[i];
                if (pos.Y >= box.BaseY && pos.Y <= box.TopHeight)
                {
                    if (Distance2DToBox(pos.X, pos.Z, in box) <= radius)
                        return true;
                }
            }

            // Блокируем снаряды и лучи сквозь парящие платформы и бастионы столпов
            for (int i = 0; i < SkyPlatforms.Count; i++)
            {
                var plat = SkyPlatforms[i];
                float bottomY = plat.TopY - MathF.Max(1.6f, plat.Thickness);
                if (pos.Y >= bottomY && pos.Y <= plat.TopY + 0.15f)
                {
                    float dx = pos.X - plat.X, dz = pos.Z - plat.Z;
                    if (MathF.Sqrt(dx * dx + dz * dz) <= plat.Radius + radius)
                        return true;
                }
            }

            return false;
        }

        public static bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 diff = to - from;
            float dist = diff.Length();
            if (dist < 1.0f) return true;
            Vector3 dir = diff / dist;
            int steps = Math.Clamp((int)(dist / 1.2f), 2, 40);
            for (int i = 1; i < steps; i++)
            {
                Vector3 sample = from + dir * (dist * (i / (float)steps));
                if (IsInsideSolidObstacle(sample, 0.15f))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Трассировка луча прицела из центра экрана камеры в мир (рельеф, платформы и твёрдые препятствия).
        /// minDist пропускает сегмент позади игрока от 3-го лица, чтобы полупрозрачные колонны у камеры не уводили выстрел назад.
        /// </summary>
        public static bool RaycastAimWorld(Vector3 rayOrigin, Vector3 rayDir, float minDist, float maxDist, out Vector3 hitPoint)
        {
            if (rayDir.LengthSquared() < 0.0001f)
            {
                hitPoint = rayOrigin + Vector3.UnitZ * maxDist;
                return false;
            }

            Vector3 dir = Vector3.Normalize(rayDir);
            float startT = MathF.Max(0.25f, minDist);
            float prevT = startT;
            float t = startT;

            static bool IsPointWorldSolid(Vector3 sample)
            {
                if (IsInsideSolidObstacle(sample, 0.06f))
                    return true;
                for (int i = 0; i < Platforms.Count; i++)
                {
                    var pBox = Platforms[i];
                    float bottomY = pBox.TopY - MathF.Max(0.8f, pBox.Thickness);
                    if (sample.Y >= bottomY && sample.Y <= pBox.TopY + 0.05f && pBox.Contains(sample.X, sample.Z, 0f))
                        return true;
                }
                float surfY = GetSurfaceHeight(sample.X, sample.Z, sample.Y + 0.55f);
                return sample.Y <= surfY + 0.04f;
            }

            while (t <= maxDist)
            {
                Vector3 sample = rayOrigin + dir * t;
                if (IsPointWorldSolid(sample))
                {
                    float lo = prevT;
                    float hi = t;
                    for (int iter = 0; iter < 5; iter++)
                    {
                        float mid = (lo + hi) * 0.5f;
                        if (IsPointWorldSolid(rayOrigin + dir * mid))
                            hi = mid;
                        else
                            lo = mid;
                    }
                    hitPoint = rayOrigin + dir * hi;
                    return true;
                }
                prevT = t;
                t += t < 36f ? 0.80f : 1.55f;
            }

            hitPoint = rayOrigin + dir * maxDist;
            return false;
        }

        public static bool IsPathBlocked2D(Vector3 pos, Vector3 dir2D, float checkDist, float radius, float entityY, float maxStepHeight = 0.45f)
        {
            float sampleX = pos.X + dir2D.X * checkDist;
            float sampleZ = pos.Z + dir2D.Z * checkDist;

            for (int i = 0; i < Obstacles.Count; i++)
            {
                var obs = Obstacles[i];
                if (entityY >= obs.TopHeight - maxStepHeight || entityY + 1.8f < obs.BaseY)
                    continue;
                float dx = sampleX - obs.X, dz = sampleZ - obs.Z;
                if (MathF.Sqrt(dx * dx + dz * dz) < obs.Radius + radius + 0.25f)
                    return true;
            }

            for (int i = 0; i < BoxObstacles.Count; i++)
            {
                var box = BoxObstacles[i];
                if (entityY >= box.TopHeight - maxStepHeight || entityY + 1.8f < box.BaseY)
                    continue;
                if (Distance2DToBox(sampleX, sampleZ, in box) < radius + 0.25f)
                    return true;
            }

            for (int i = 0; i < SkyPlatforms.Count; i++)
            {
                var plat = SkyPlatforms[i];
                float bottomY = plat.TopY - plat.Thickness;
                if (entityY >= plat.TopY - maxStepHeight || entityY + 1.8f < bottomY)
                    continue;
                float dx = sampleX - plat.X, dz = sampleZ - plat.Z;
                if (MathF.Sqrt(dx * dx + dz * dz) < plat.Radius + radius + 0.25f)
                    return true;
            }

            return false;
        }

        public static Vector3 ComputeSmartSteerDirection(Vector3 pos, Vector3 desiredDir2D, float radius, float entityY, ref float preferSide)
        {
            if (desiredDir2D.LengthSquared() < 0.0001f)
                return Vector3.UnitZ;
            Vector3 baseDir = Vector3.Normalize(new Vector3(desiredDir2D.X, 0f, desiredDir2D.Z));

            if (!IsPathBlocked2D(pos, baseDir, 1.8f, radius, entityY) &&
                !IsPathBlocked2D(pos, baseDir, 3.2f, radius * 0.85f, entityY))
            {
                return baseDir;
            }

            float[] testAngles = { 25f, 50f, 75f, 105f, 135f };
            foreach (float deg in testAngles)
            {
                foreach (float side in new[] { preferSide, -preferSide })
                {
                    float rad = deg * side * (MathF.PI / 180f);
                    float c = MathF.Cos(rad), s = MathF.Sin(rad);
                    Vector3 cand = new(baseDir.X * c - baseDir.Z * s, 0f, baseDir.X * s + baseDir.Z * c);
                    if (!IsPathBlocked2D(pos, cand, 2.2f, radius, entityY) &&
                        !IsPathBlocked2D(pos, cand, 3.6f, radius * 0.8f, entityY))
                    {
                        preferSide = side;
                        return cand;
                    }
                }
            }

            return baseDir;
        }

        public static bool TryFindClimbTarget(
            Vector3 enemyPos, Vector3 targetPos, float maxJumpHeight,
            out Vector3 climbGoalPos, out float ledgeTopY, out bool readyToJumpNow)
        {
            climbGoalPos = targetPos;
            ledgeTopY = enemyPos.Y;
            readyToJumpNow = false;

            float bestScore = 99999f;
            bool found = false;

            // 1. Проверяем небесные платформы и террасы (SkyPlatforms)
            for (int i = 0; i < SkyPlatforms.Count; i++)
            {
                var plat = SkyPlatforms[i];
                float hGain = plat.TopY - enemyPos.Y;
                if (hGain < 0.55f || hGain > maxJumpHeight) continue;

                float dToTarget = MathF.Sqrt((plat.X - targetPos.X) * (plat.X - targetPos.X) + (plat.Z - targetPos.Z) * (plat.Z - targetPos.Z));
                float dFromEnemy = MathF.Sqrt((plat.X - enemyPos.X) * (plat.X - enemyPos.X) + (plat.Z - enemyPos.Z) * (plat.Z - enemyPos.Z));
                if (dFromEnemy > 28.0f && dToTarget > plat.Radius + 6.0f) continue;

                float score = dToTarget * 1.15f + MathF.Max(0f, dFromEnemy - plat.Radius) * 0.85f + MathF.Abs(targetPos.Y - plat.TopY) * 2.0f;
                if (score < bestScore)
                {
                    bestScore = score;
                    found = true;
                    ledgeTopY = plat.TopY;
                    Vector3 toCenter = new(plat.X - enemyPos.X, 0f, plat.Z - enemyPos.Z);
                    float len = toCenter.Length();
                    Vector3 dir = len > 0.01f ? toCenter / len : Vector3.UnitZ;
                    climbGoalPos = new Vector3(plat.X - dir.X * MathF.Max(0f, plat.Radius - 1.2f), plat.TopY, plat.Z - dir.Z * MathF.Max(0f, plat.Radius - 1.2f));
                    readyToJumpNow = dFromEnemy <= plat.Radius + 3.4f && dFromEnemy >= plat.Radius - 1.8f;
                }
            }

            // 2. Проверяем широкие плоские препятствия и шляпки грибов (Obstacles)
            for (int i = 0; i < Obstacles.Count; i++)
            {
                var obs = Obstacles[i];
                if (obs.Radius < 1.35f) continue;
                float hGain = obs.TopHeight - enemyPos.Y;
                if (hGain < 0.55f || hGain > maxJumpHeight) continue;

                float dToTarget = MathF.Sqrt((obs.X - targetPos.X) * (obs.X - targetPos.X) + (obs.Z - targetPos.Z) * (obs.Z - targetPos.Z));
                float dFromEnemy = MathF.Sqrt((obs.X - enemyPos.X) * (obs.X - enemyPos.X) + (obs.Z - enemyPos.Z) * (obs.Z - enemyPos.Z));
                if (dFromEnemy > 20.0f) continue;

                float score = dToTarget * 1.2f + MathF.Max(0f, dFromEnemy - obs.Radius) * 0.9f + MathF.Abs(targetPos.Y - obs.TopHeight) * 2.0f;
                if (score < bestScore)
                {
                    bestScore = score;
                    found = true;
                    ledgeTopY = obs.TopHeight;
                    climbGoalPos = new Vector3(obs.X, obs.TopHeight, obs.Z);
                    readyToJumpNow = dFromEnemy <= obs.Radius + 2.8f;
                }
            }

            // 3. Проверяем прямоугольные площадки и сцены (BoxObstacles с WalkableTop)
            for (int i = 0; i < BoxObstacles.Count; i++)
            {
                var box = BoxObstacles[i];
                if (!box.WalkableTop || (box.HalfX < 1.3f && box.HalfZ < 1.3f)) continue;
                float hGain = box.TopHeight - enemyPos.Y;
                if (hGain < 0.55f || hGain > maxJumpHeight) continue;

                float dFromEnemy = Distance2DToBox(enemyPos.X, enemyPos.Z, in box);
                if (dFromEnemy > 20.0f) continue;
                float dToTarget = MathF.Sqrt((box.X - targetPos.X) * (box.X - targetPos.X) + (box.Z - targetPos.Z) * (box.Z - targetPos.Z));
                float score = dToTarget * 1.2f + dFromEnemy * 0.9f + MathF.Abs(targetPos.Y - box.TopHeight) * 2.0f;
                if (score < bestScore)
                {
                    bestScore = score;
                    found = true;
                    ledgeTopY = box.TopHeight;
                    climbGoalPos = new Vector3(box.X, box.TopHeight, box.Z);
                    readyToJumpNow = dFromEnemy <= 2.8f;
                }
            }

            return found;
        }

        // =========================================================================
        // ПРОВЕРКА СВОБОДНОГО МЕСТА ДЛЯ СПАВНА (ЗАЩИТА ОТ СПАВНА ДРУГ В ДРУГЕ / В СТЕНАХ)
        // =========================================================================

        public static void RegisterSpawnBlocker(Vector3 pos, float radius, float height = 4.0f)
        {
            SpawnBlockers.Add(new SpawnBlocker(pos.X, pos.Z, radius, pos.Y, height));
        }

        public static bool IsSpawnSpotValid(
            float x, float z, float footprintRadius, float objectHeight,
            out float spawnY, bool allowSkyPlatforms = true,
            float minDistFromOrigin = 0f, float extraPadding = 0.55f, float boundMargin = 2.0f)
        {
            spawnY = 0.0f;

            if (minDistFromOrigin > 0f && (x * x + z * z) < minDistFromOrigin * minDistFromOrigin)
                return false;

            float maxSq = WorldBound - boundMargin - footprintRadius;
            if (MathF.Abs(x) > maxSq || MathF.Abs(z) > maxSq)
                return false;

            if (WorldBoundRadius > 0f)
            {
                float maxR = WorldBoundRadius - boundMargin - footprintRadius;
                if (maxR <= 0.5f || (x * x + z * z) > maxR * maxR)
                    return false;
            }

            // На крейсере «Safe Travels» объект должен целиком стоять внутри палуб, а не за переборками корпуса
            if (SpecialMode == "safe_travels" && !IsInsideSafeTravelsHull(x, z, footprintRadius + 0.6f))
                return false;

            // Никогда не спавним объекты над ямами пола или на их кромке
            for (int i = 0; i < FloorHoles.Count; i++)
            {
                var h = FloorHoles[i];
                float pad = footprintRadius + 0.85f;
                if (x >= h.MinX - pad && x <= h.MaxX + pad && z >= h.MinZ - pad && z <= h.MaxZ + pad)
                    return false;
            }

            // Проверяем террасы и парящие острова: объект должен либо целиком помещаться на верхней плоскости террасы,
            // либо находиться полностью снаружи её каменного склона (никогда не зарываясь в склон и не свисая с края!)
            int chosenPlatIdx = -1;
            float bestTopY = 0.0f;
            for (int i = 0; i < SkyPlatforms.Count; i++)
            {
                var plat = SkyPlatforms[i];
                float dx = x - plat.X, dz = z - plat.Z;
                float dist = MathF.Sqrt(dx * dx + dz * dz);
                if (dist + footprintRadius <= plat.Radius - 1.05f)
                {
                    if (plat.TopY > bestTopY)
                    {
                        bestTopY = plat.TopY;
                        chosenPlatIdx = i;
                    }
                }
            }

            if (chosenPlatIdx >= 0)
            {
                if (!allowSkyPlatforms)
                    return false;
                spawnY = bestTopY;
            }

            for (int i = 0; i < SkyPlatforms.Count; i++)
            {
                if (i == chosenPlatIdx) continue;
                var plat = SkyPlatforms[i];
                float dx = x - plat.X, dz = z - plat.Z;
                float dist = MathF.Sqrt(dx * dx + dz * dz);
                float outerWallR = plat.Radius * 1.04f + footprintRadius + 0.65f;
                if (dist >= outerWallR)
                    continue;

                // Если это нижний ярус под уже выбранным верхним ярусом — он не мешает
                if (chosenPlatIdx >= 0 && plat.TopY <= spawnY + 0.05f)
                    continue;

                // Если это высокий парящий остров, под которым низкий объект свободно помещается по высоте
                float platBottom = plat.TopY - plat.Thickness;
                if (platBottom >= spawnY + objectHeight + 2.0f)
                    continue;

                return false;
            }

            // Низкие проходимые площадки (сцены, постаменты, широкие плиты): объект может стоять сверху,
            // только если целиком помещается на их верхней грани (никогда не свисает и не зарывается в бок)
            for (int i = 0; i < BoxObstacles.Count; i++)
            {
                var box = BoxObstacles[i];
                if (!box.WalkableTop) continue;
                float rise = box.TopHeight - spawnY;
                if (rise <= 0.02f || rise > 1.4f || box.BaseY > spawnY + 0.3f) continue;
                if (IsFootprintInsideBox(x, z, footprintRadius + 0.15f, in box))
                    spawnY = box.TopHeight;
            }

            // Проверка пересечения со всеми ранее размещёнными объектами (SpawnBlockers)
            for (int i = 0; i < SpawnBlockers.Count; i++)
            {
                var b = SpawnBlockers[i];
                if (spawnY >= b.TopY + 0.6f || spawnY + objectHeight <= b.BaseY - 0.6f)
                    continue;
                float dx = x - b.X, dz = z - b.Z;
                float minD = footprintRadius + b.Radius + extraPadding;
                if (dx * dx + dz * dz < minD * minD)
                    return false;
            }

            // Проверка пересечения с круглыми физическими коллайдерами (колонны, скалы, грибы)
            for (int i = 0; i < Obstacles.Count; i++)
            {
                var obs = Obstacles[i];
                if (spawnY >= obs.TopHeight + 0.35f || spawnY + objectHeight <= obs.BaseY - 0.35f)
                    continue;
                float dx = x - obs.X, dz = z - obs.Z;
                float minD = footprintRadius + obs.Radius + extraPadding;
                if (dx * dx + dz * dz < minD * minD)
                    return false;
            }

            // Проверка пересечения с прямоугольными коллайдерами (стены, башни, сцены, заборы, контейнеры)
            for (int i = 0; i < BoxObstacles.Count; i++)
            {
                var box = BoxObstacles[i];
                if (spawnY >= box.TopHeight + 0.35f || spawnY + objectHeight <= box.BaseY - 0.35f)
                    continue;
                // Площадка, на которой объект стоит сверху (или лежащая под ней), не считается пересечением
                if (spawnY >= box.TopHeight - 0.05f && IsFootprintInsideBox(x, z, 0f, in box))
                    continue;
                if (Distance2DToBox(x, z, in box) < footprintRadius + extraPadding)
                    return false;
            }

            return true;
        }

        public static bool TryFindFreeSpawnPos(
            Random rng, float footprintRadius, float objectHeight, out Vector3 resultPos,
            float minDistFromOrigin = 11.0f, float maxDistFromOrigin = 78.0f,
            bool allowSkyPlatforms = true, float extraPadding = 0.65f, float boundMargin = 2.5f,
            int maxAttempts = 120)
        {
            float effMaxR = maxDistFromOrigin;
            if (WorldBoundRadius > 0f)
                effMaxR = MathF.Min(effMaxR, WorldBoundRadius - boundMargin - footprintRadius);
            effMaxR = MathF.Min(effMaxR, WorldBound - boundMargin - footprintRadius);

            if (effMaxR <= minDistFromOrigin + 0.5f)
                effMaxR = minDistFromOrigin + 2.0f;

            for (int pass = 0; pass < 2; pass++)
            {
                float pad = pass == 0 ? extraPadding : MathF.Max(0.22f, extraPadding * 0.4f);
                int tries = pass == 0 ? maxAttempts : 80;
                for (int attempt = 0; attempt < tries; attempt++)
                {
                    float ang = (float)rng.NextDouble() * MathF.PI * 2f;
                    float t = MathF.Sqrt((float)rng.NextDouble());
                    float dist = minDistFromOrigin + t * (effMaxR - minDistFromOrigin);
                    float cx = MathF.Cos(ang) * dist;
                    float cz = MathF.Sin(ang) * dist;

                    if (IsSpawnSpotValid(cx, cz, footprintRadius, objectHeight, out float cy,
                        allowSkyPlatforms, minDistFromOrigin, pad, boundMargin))
                    {
                        resultPos = new Vector3(cx, cy, cz);
                        return true;
                    }
                }
            }

            resultPos = Vector3.Zero;
            return false;
        }

        public static Vector3 FindClearNearbyPos(
            Vector3 preferredPos, float footprintRadius, float objectHeight = 3.5f,
            bool allowSkyPlatforms = true, float minDistFromOrigin = 0f,
            float extraPadding = 0.45f, float maxSearchRadius = 26f, float boundMargin = 1.8f)
        {
            if (IsSpawnSpotValid(preferredPos.X, preferredPos.Z, footprintRadius, objectHeight, out float py,
                allowSkyPlatforms, minDistFromOrigin, extraPadding, boundMargin))
            {
                return new Vector3(preferredPos.X, py, preferredPos.Z);
            }

            for (float r = 1.6f; r <= maxSearchRadius; r += 1.6f)
            {
                const int dirs = 16;
                for (int d = 0; d < dirs; d++)
                {
                    float ang = (d / (float)dirs) * MathF.PI * 2f + r * 0.19f;
                    float tx = preferredPos.X + MathF.Cos(ang) * r;
                    float tz = preferredPos.Z + MathF.Sin(ang) * r;
                    if (IsSpawnSpotValid(tx, tz, footprintRadius, objectHeight, out float ty,
                        allowSkyPlatforms, minDistFromOrigin, extraPadding, boundMargin))
                    {
                        return new Vector3(tx, ty, tz);
                    }
                }
            }

            // Мягкий запасной проход с минимальным зазором (без физического пересечения)
            for (float r = 1.2f; r <= maxSearchRadius + 8f; r += 1.6f)
            {
                const int dirs = 16;
                for (int d = 0; d < dirs; d++)
                {
                    float ang = (d / (float)dirs) * MathF.PI * 2f;
                    float tx = preferredPos.X + MathF.Cos(ang) * r;
                    float tz = preferredPos.Z + MathF.Sin(ang) * r;
                    if (IsSpawnSpotValid(tx, tz, footprintRadius, objectHeight, out float ty,
                        allowSkyPlatforms, minDistFromOrigin, 0.18f, 1.2f))
                    {
                        return new Vector3(tx, ty, tz);
                    }
                }
            }

            Vector3 fallback = ResolveHorizontalCollision(preferredPos, footprintRadius + 0.35f, preferredPos.Y);
            fallback.Y = GetTerrainSpawnHeight(fallback.X, fallback.Z);
            return fallback;
        }
    }
}
