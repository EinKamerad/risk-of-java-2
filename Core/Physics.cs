using System;
using System.Collections.Generic;
using System.Numerics;
using RiskOfCSharp2.Models;

namespace RiskOfCSharp2.Core
{
    public struct SkyPlatform
    {
        public float X, Z, Radius, TopY;
        public SkyPlatform(float x, float z, float radius, float topY)
        {
            X = x; Z = z; Radius = radius; TopY = topY;
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

            // Железная страховка пола Небесного Колизея на Луне (Y = 120.0, R = 56.5)
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
                    if (dist < 0.001f)
                    {
                        rx += minD;
                    }
                    else
                    {
                        float push = (minD - dist) / dist;
                        rx += dx * push;
                        rz += dz * push;
                    }
                }
            }
            return new Vector3(rx, pos.Y, rz);
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
            return false;
        }
    }
}