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

    public static class Physics
    {
        public const float ColosseumY = 120.0f;
        public const float ColosseumRadius = 56.0f;

        public static readonly List<ObstacleCircle> Obstacles = new();
        public static readonly List<SkyPlatform> SkyPlatforms = new();
        public static string? SpecialMode = null;
        public static float WorldBound = 58.0f;

        public static float Distance2D(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        public static float GetSurfaceHeight(float x, float z, float currentY, float? prevY = null)
        {
            float checkY = prevY.HasValue ? MathF.Max(currentY, prevY.Value) : currentY;
            float maxH = 0.0f;

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
            for (int pass = 0; pass < 2; pass++)
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
            }

            return new Vector3(rx, pos.Y, rz);
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

            return found;
        }
    }
}