using System;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Models
{
    public class EnemyParts
    {
        public Entity Root = null!;
        public Entity? Torso;
        public Entity? LeftLeg;
        public Entity? RightLeg;
        public Entity? LeftArm;
        public Entity? RightArm;
        public Entity? Tail;
        public Entity? Core;
        public Entity? Ring1;
        public Entity? Ring2;
        public Entity? Tentacles;
        public Entity? Bell;
        public Entity? Crown;
        public Entity? Cape;
        public Entity? Hammer;
        public Entity? HammerAura;
        public Entity? PhaseRing;
    }

    public static class EnemyAndBossBuilder
    {
        public static EnemyParts BuildEnemyMesh(Entity parentEntity, string enemyType)
        {
            var root = new Entity(parent: parentEntity);
            var parts = new EnemyParts { Root = root };

            if (enemyType == "lemurian")
            {
                Color skinCol = Rgb(190, 58, 46);
                Color bellyCol = Rgb(230, 140, 75);
                Color spineCol = Rgb(245, 235, 200);
                Color armorCol = Rgb(82, 42, 38);

                var lLeg = new Entity(parent: root, position: new(-0.26f, 0.75f, 0));
                var rLeg = new Entity(parent: root, position: new(0.26f, 0.75f, 0));
                foreach (var leg in new[] { lLeg, rLeg })
                {
                    new Entity(parent: leg, model: "cube", scale: new(0.24f, 0.75f, 0.28f), position: new(0, -0.35f, 0), color: skinCol);
                    new Entity(parent: leg, model: "diamond", scale: new(0.28f, 0.24f, 0.32f), position: new(0, -0.35f, 0.08f), color: armorCol);
                    new Entity(parent: leg, model: "diamond", scale: new(0.30f, 0.20f, 0.46f), position: new(0, -0.70f, 0.14f), color: spineCol);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 1.15f, 0), rotation: new(18, 0, 0));
                new Entity(parent: torso, model: "cube", scale: new(0.72f, 0.98f, 0.56f), color: skinCol);
                new Entity(parent: torso, model: "cube", scale: new(0.54f, 0.86f, 0.18f), position: new(0, 0, 0.23f), color: bellyCol);
                new Entity(parent: torso, model: "diamond", scale: new(1.05f, 0.38f, 0.58f), position: new(0, 0.34f, 0), color: armorCol);
                for (int i = 0; i < 5; i++)
                    new Entity(parent: torso, model: "diamond", scale: new(0.15f, 0.42f, 0.28f), position: new(0, 0.38f - i * 0.22f, -0.32f), rotation: new(-35, 0, 0), color: spineCol);

                var tail = new Entity(parent: torso, position: new(0, -0.4f, -0.25f));
                new Entity(parent: tail, model: "diamond", scale: new(0.32f, 0.28f, 1.25f), position: new(0, -0.1f, -0.52f), rotation: new(-25, 0, 0), color: skinCol);
                new Entity(parent: tail, model: "diamond", scale: new(0.20f, 0.32f, 0.45f), position: new(0, -0.32f, -1.05f), rotation: new(-15, 0, 0), color: spineCol);
                parts.Tail = tail;
                parts.Torso = torso;

                var head = new Entity(parent: torso, position: new(0, 0.64f, 0.18f), rotation: new(-18, 0, 0));
                new Entity(parent: head, model: "diamond", scale: new(0.72f, 0.54f, 1.02f), color: skinCol);
                new Entity(parent: head, model: "cube", scale: new(0.48f, 0.16f, 0.72f), position: new(0, -0.20f, 0.16f), color: bellyCol);
                foreach (float tx in new[] { -0.16f, 0.0f, 0.16f })
                    new Entity(parent: head, model: "diamond", scale: new(0.07f, 0.14f, 0.07f), position: new(tx, -0.12f, 0.42f), color: spineCol);
                new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.72f, 0.20f), position: new(-0.26f, 0.38f, -0.22f), rotation: new(-30, 0, -20), color: spineCol);
                new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.72f, 0.20f), position: new(0.26f, 0.38f, -0.22f), rotation: new(-30, 0, 20), color: spineCol);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.16f, position: new(-0.21f, 0.09f, 0.27f), color: Color.Yellow, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.16f, position: new(0.21f, 0.09f, 0.27f), color: Color.Yellow, unlit: true);

                var lArm = new Entity(parent: torso, position: new(-0.46f, 0.22f, 0.1f));
                var rArm = new Entity(parent: torso, position: new(0.46f, 0.22f, 0.1f));
                foreach (var arm in new[] { lArm, rArm })
                {
                    new Entity(parent: arm, model: "cube", scale: new(0.19f, 0.58f, 0.19f), position: new(0, -0.2f, 0.2f), rotation: new(-45, 0, 0), color: skinCol);
                    new Entity(parent: arm, model: "diamond", scale: new(0.22f, 0.28f, 0.18f), position: new(0, -0.44f, 0.42f), rotation: new(-65, 0, 0), color: spineCol);
                }
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
            }
            else if (enemyType == "wisp")
            {
                var core = new Entity(parent: root, position: new(0, 2.3f, 0));
                new Entity(parent: core, model: "sphere", scaleUniform: 0.90f, color: Rgb(255, 135, 20), unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(0.60f, 1.55f, 0.60f), position: new(0, 0.40f, 0), color: Color.Yellow, unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(0.38f, 1.85f, 0.38f), position: new(0, 0.58f, 0), color: Color.White, unlit: true);

                var mask = new Entity(parent: core, position: new(0, 0, 0.40f));
                new Entity(parent: mask, model: "cube", scale: new(0.80f, 0.90f, 0.22f), color: Rgb(55, 60, 72));
                new Entity(parent: mask, model: "diamond", scale: new(0.65f, 0.35f, 0.26f), position: new(0, -0.38f, 0), color: Rgb(42, 46, 56));
                new Entity(parent: mask, model: "cube", scale: new(0.26f, 0.13f, 0.28f), position: new(-0.18f, 0.12f, 0.02f), color: Color.Yellow, unlit: true);
                new Entity(parent: mask, model: "cube", scale: new(0.26f, 0.13f, 0.28f), position: new(0.18f, 0.12f, 0.02f), color: Color.Yellow, unlit: true);
                new Entity(parent: mask, model: "diamond", scale: new(0.26f, 0.88f, 0.26f), position: new(-0.45f, 0.48f, -0.1f), rotation: new(0, 0, -25f), color: Rgb(95, 100, 115));
                new Entity(parent: mask, model: "diamond", scale: new(0.26f, 0.88f, 0.26f), position: new(0.45f, 0.48f, -0.1f), rotation: new(0, 0, 25f), color: Rgb(95, 100, 115));

                parts.Core = core;
                parts.Ring1 = new Entity(parent: core, model: "diamond", scale: new(1.85f, 0.18f, 1.85f), color: Rgb(70, 75, 88));
                parts.Ring2 = new Entity(parent: core, model: "diamond", scale: new(0.18f, 1.85f, 1.85f), color: Rgb(115, 120, 135));
            }
            else if (enemyType == "beetle")
            {
                Color shellCol = Rgb(95, 62, 42);
                Color plateCol = Rgb(135, 88, 55);
                Color hornCol = Rgb(225, 175, 85);
                var torso = new Entity(parent: root, position: new(0, 0.65f, 0));
                new Entity(parent: torso, model: "sphere", scale: new(1.18f, 0.74f, 1.48f), color: shellCol);
                new Entity(parent: torso, model: "diamond", scale: new(0.92f, 0.38f, 1.22f), position: new(0, 0.28f, 0), color: plateCol);
                new Entity(parent: torso, model: "cube", scale: new(0.06f, 0.42f, 1.15f), position: new(0, 0.29f, 0), color: Rgb(55, 35, 22));
                new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.82f, 0.38f), position: new(0, 0.24f, 0.78f), rotation: new(55, 0, 0), color: hornCol);
                foreach (float mx in new[] { -0.24f, 0.24f })
                    new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.14f, 0.45f), position: new(mx, -0.05f, 0.75f), rotation: new(0, mx * -35f, 0), color: hornCol);
                new Entity(parent: torso, model: "sphere", scaleUniform: 0.16f, position: new(-0.27f, 0.13f, 0.64f), color: Color.Orange, unlit: true);
                new Entity(parent: torso, model: "sphere", scaleUniform: 0.16f, position: new(0.27f, 0.13f, 0.64f), color: Color.Orange, unlit: true);

                var lLeg = new Entity(parent: root, position: new(-0.55f, 0.45f, 0));
                var rLeg = new Entity(parent: root, position: new(0.55f, 0.45f, 0));
                foreach (var (side, leg) in new[] { (-1, lLeg), (1, rLeg) })
                {
                    foreach (float zOff in new[] { -0.36f, 0.0f, 0.36f })
                        new Entity(parent: leg, model: "diamond", scale: new(0.22f, 0.60f, 0.22f), position: new(side * 0.12f, -0.2f, zOff), rotation: new(0, 0, side * 28f), color: shellCol);
                }
                parts.Torso = torso;
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;
            }
            else if (enemyType == "jellyfish")
            {
                var core = new Entity(parent: root, position: new(0, 2.2f, 0));
                new Entity(parent: core, model: "sphere", scale: new(1.40f, 1.08f, 1.40f), color: Rgb(75, 225, 255, 185), unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(1.55f, 0.28f, 1.55f), position: new(0, -0.22f, 0), color: Rgb(135, 245, 255, 160), unlit: true);
                new Entity(parent: core, model: "sphere", scaleUniform: 0.68f, position: new(0, -0.05f, 0), color: Rgb(245, 110, 255), unlit: true);
                var tentacles = new Entity(parent: core, position: new(0, -0.35f, 0));
                for (int i = 0; i < 8; i++)
                {
                    float ang = (i / 8.0f) * MathF.PI * 2f;
                    new Entity(parent: tentacles, model: "diamond", scale: new(0.13f, 1.35f, 0.13f),
                        position: new(MathF.Cos(ang) * 0.38f, -0.55f, MathF.Sin(ang) * 0.38f),
                        color: i % 2 == 0 ? Color.SkyBlue : Rgb(235, 120, 255), unlit: true);
                }
                parts.Core = core;
                parts.Tentacles = tentacles;
            }
            else if (enemyType == "golem")
            {
                Color stCol = Rgb(78, 86, 98);
                Color stDark = Rgb(52, 58, 68);
                Color runeCol = Rgb(255, 70, 55);
                var lLeg = new Entity(parent: root, position: new(-0.46f, 1.0f, 0));
                var rLeg = new Entity(parent: root, position: new(0.46f, 1.0f, 0));
                foreach (var leg in new[] { lLeg, rLeg })
                {
                    new Entity(parent: leg, model: "cube", scale: new(0.48f, 1.0f, 0.52f), position: new(0, -0.5f, 0), color: stDark);
                    new Entity(parent: leg, model: "cube", scale: new(0.56f, 0.28f, 0.64f), position: new(0, -0.88f, 0.08f), color: stCol);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 1.85f, 0));
                new Entity(parent: torso, model: "cube", scale: new(1.48f, 1.38f, 0.95f), color: stCol);
                new Entity(parent: torso, model: "diamond", scale: new(1.65f, 0.45f, 1.05f), position: new(0, 0.45f, 0), color: stDark);
                new Entity(parent: torso, model: "diamond", scale: new(0.32f, 0.78f, 1.02f), color: runeCol, unlit: true);
                var head = new Entity(parent: torso, model: "cube", scale: new(0.78f, 0.62f, 0.74f), position: new(0, 0.95f, 0), color: stDark);
                new Entity(parent: head, model: "cube", scale: new(0.68f, 0.16f, 0.78f), position: new(0, 0.05f, 0), color: runeCol, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.36f, position: new(0, 0.05f, 0.46f), color: Color.Red, unlit: true);

                var lArm = new Entity(parent: torso, position: new(-1.02f, 0.35f, 0));
                var rArm = new Entity(parent: torso, position: new(1.02f, 0.35f, 0));
                foreach (var arm in new[] { lArm, rArm })
                {
                    new Entity(parent: arm, model: "diamond", scale: new(0.62f, 0.55f, 0.62f), position: new(0, 0.12f, 0), color: stDark);
                    new Entity(parent: arm, model: "cube", scale: new(0.44f, 1.25f, 0.44f), position: new(0, -0.55f, 0), color: stCol);
                    new Entity(parent: arm, model: "cube", scale: new(0.56f, 0.48f, 0.56f), position: new(0, -1.15f, 0), color: stDark);
                }
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
            }
            else if (enemyType == "imp")
            {
                Color impCol = Rgb(45, 22, 48);
                Color crimson = Rgb(245, 35, 70);
                var lLeg = new Entity(parent: root, position: new(-0.24f, 0.78f, 0));
                var rLeg = new Entity(parent: root, position: new(0.24f, 0.78f, 0));
                foreach (var leg in new[] { lLeg, rLeg })
                {
                    new Entity(parent: leg, model: "diamond", scale: new(0.25f, 0.84f, 0.25f), position: new(0, -0.4f, 0), color: impCol);
                    new Entity(parent: leg, model: "diamond", scale: new(0.18f, 0.18f, 0.38f), position: new(0, -0.75f, 0.12f), color: crimson, unlit: true);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 1.28f, 0));
                new Entity(parent: torso, model: "diamond", scale: new(0.86f, 1.18f, 0.60f), color: impCol);
                new Entity(parent: torso, model: "diamond", scale: new(0.22f, 0.72f, 0.28f), position: new(0, 0, 0.24f), color: crimson, unlit: true);
                new Entity(parent: torso, model: "sphere", scaleUniform: 0.18f, position: new(0, 0, 0.30f), color: Color.White, unlit: true);
                foreach (int side in new[] { -1, 1 })
                {
                    new Entity(parent: torso, model: "diamond", scale: new(0.18f, 0.68f, 0.18f), position: new(side * 0.30f, 0.68f, 0), rotation: new(0, 0, side * -24f), color: crimson, unlit: true);
                    new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.42f, 0.24f), position: new(side * 0.24f, 0.25f, -0.24f), rotation: new(-30, 0, 0), color: crimson, unlit: true);
                }

                var lArm = new Entity(parent: torso, position: new(-0.54f, 0.22f, 0));
                var rArm = new Entity(parent: torso, position: new(0.54f, 0.22f, 0));
                foreach (var arm in new[] { lArm, rArm })
                {
                    new Entity(parent: arm, model: "diamond", scale: new(0.24f, 0.78f, 0.24f), position: new(0, -0.25f, 0.22f), rotation: new(-50, 0, 0), color: impCol);
                    foreach (float cx in new[] { -0.06f, 0.06f })
                        new Entity(parent: arm, model: "diamond", scale: new(0.10f, 0.48f, 0.14f), position: new(cx, -0.54f, 0.44f), rotation: new(-70, 0, 0), color: crimson, unlit: true);
                }
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
            }
            else if (enemyType == "greater_wisp")
            {
                var core = new Entity(parent: root, position: new(0, 2.7f, 0));
                new Entity(parent: core, model: "sphere", scaleUniform: 1.28f, color: Rgb(65, 245, 110), unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(0.90f, 2.25f, 0.90f), position: new(0, 0.52f, 0), color: Rgb(175, 255, 120), unlit: true);

                var mask = new Entity(parent: core, position: new(0, 0, 0.54f));
                new Entity(parent: mask, model: "diamond", scale: new(1.42f, 1.65f, 0.36f), color: Rgb(48, 54, 64));
                new Entity(parent: mask, model: "diamond", scale: new(0.38f, 0.24f, 0.40f), position: new(-0.28f, 0.16f, 0.05f), color: Color.Lime, unlit: true);
                new Entity(parent: mask, model: "diamond", scale: new(0.38f, 0.24f, 0.40f), position: new(0.28f, 0.16f, 0.05f), color: Color.Lime, unlit: true);
                foreach (int side in new[] { -1, 1 })
                    new Entity(parent: mask, model: "diamond", scale: new(0.30f, 0.95f, 0.28f), position: new(side * 0.58f, 0.58f, -0.05f), rotation: new(0, 0, side * -22f), color: Rgb(78, 86, 100));

                parts.Core = core;
                parts.Ring1 = new Entity(parent: core, model: "diamond", scale: new(2.6f, 0.24f, 2.6f), color: Rgb(65, 72, 85));
                foreach (float ox in new[] { -1.38f, 1.38f })
                {
                    var orb = new Entity(parent: core, model: "sphere", scaleUniform: 0.52f, position: new(ox, -0.1f, 0.35f), color: Color.Lime, unlit: true);
                    new Entity(parent: orb, model: "diamond", scale: new(1.45f, 0.45f, 1.45f), position: new(0, -0.35f, 0), color: Rgb(55, 62, 74));
                }
            }
            else
            {
                return BuildBossMesh(parentEntity, enemyType, root, parts);
            }

            return parts;
        }

        private static EnemyParts BuildBossMesh(Entity parentEntity, string enemyType, Entity root, EnemyParts parts)
        {
            if (enemyType == "boss")
            {
                Color stoneDark = Rgb(68, 74, 86);
                Color stoneLight = Rgb(98, 106, 120);
                var lLeg = new Entity(parent: root, position: new(-0.85f, 1.6f, 0));
                var rLeg = new Entity(parent: root, position: new(0.85f, 1.6f, 0));
                foreach (var leg in new[] { lLeg, rLeg })
                {
                    new Entity(parent: leg, model: "cube", scale: new(0.90f, 1.6f, 0.96f), position: new(0, -0.8f, 0), color: stoneDark);
                    new Entity(parent: leg, model: "cube", scale: new(0.96f, 0.16f, 1.02f), position: new(0, -0.65f, 0), color: Color.Red, unlit: true);
                    new Entity(parent: leg, model: "cube", scale: new(1.08f, 0.44f, 1.28f), position: new(0, -1.5f, 0.15f), color: stoneLight);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 3.2f, 0));
                new Entity(parent: torso, model: "cube", scale: new(2.68f, 2.85f, 1.85f), color: stoneLight);
                new Entity(parent: torso, model: "diamond", scale: new(2.95f, 0.85f, 2.05f), position: new(0, 1.05f, 0), color: stoneDark);
                var core = new Entity(parent: torso, model: "sphere", scale: new(1.04f, 1.04f, 0.62f), position: new(0, 0.25f, 0.84f), color: Color.Red, unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(0.60f, 0.60f, 0.90f), position: new(0, 0, 0.22f), color: Color.Yellow, unlit: true);
                foreach (float sx in new[] { -0.88f, 0.88f })
                    new Entity(parent: torso, model: "cube", scale: new(0.24f, 2.25f, 1.90f), position: new(sx, 0, 0), color: Color.Red, unlit: true);

                var lArm = new Entity(parent: torso, position: new(-1.88f, 0.6f, 0));
                var rArm = new Entity(parent: torso, position: new(1.88f, 0.6f, 0));
                foreach (var arm in new[] { lArm, rArm })
                {
                    new Entity(parent: arm, model: "diamond", scale: new(1.12f, 1.22f, 1.12f), position: new(0, 0.32f, 0), color: stoneDark);
                    new Entity(parent: arm, model: "cube", scale: new(0.82f, 2.0f, 0.82f), position: new(0, -0.9f, 0), color: stoneDark);
                    new Entity(parent: arm, model: "cube", scale: new(1.06f, 0.92f, 1.06f), position: new(0, -1.9f, 0), color: stoneLight);
                    new Entity(parent: arm, model: "cube", scale: new(1.12f, 0.18f, 1.12f), position: new(0, -1.95f, 0), color: Color.Red, unlit: true);
                }

                var crown = new Entity(parent: torso, position: new(0, 2.15f, 0));
                new Entity(parent: crown, model: "diamond", scale: new(1.62f, 0.90f, 1.62f), color: Color.Red, unlit: true);
                new Entity(parent: crown, model: "diamond", scale: new(2.05f, 0.28f, 2.05f), rotation: new(0, 45, 0), color: stoneDark);
                new Entity(parent: crown, model: "diamond", scale: new(2.05f, 0.28f, 2.05f), color: stoneLight);
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
                parts.Crown = crown;
                parts.Core = core;
                parts.Torso = torso;
            }
            else if (enemyType == "vagrant")
            {
                var bell = new Entity(parent: root, position: new(0, 4.2f, 0));
                new Entity(parent: bell, model: "sphere", scale: new(4.3f, 2.85f, 4.3f), color: Rgb(55, 205, 255, 185), unlit: true);
                new Entity(parent: bell, model: "diamond", scale: new(2.6f, 1.6f, 2.6f), position: new(0, 1.1f, 0), color: Rgb(110, 240, 255), unlit: true);
                new Entity(parent: bell, model: "diamond", scale: new(4.75f, 0.45f, 4.75f), position: new(0, -0.5f, 0), color: Rgb(120, 245, 255), unlit: true);
                new Entity(parent: bell, model: "diamond", scale: new(4.75f, 0.45f, 4.75f), position: new(0, -0.5f, 0), rotation: new(0, 45f, 0), color: Rgb(80, 220, 255), unlit: true);
                var core = new Entity(parent: bell, model: "sphere", scaleUniform: 1.85f, position: new(0, -0.3f, 0), color: Rgb(255, 230, 80), unlit: true);
                var tentacles = new Entity(parent: bell, position: new(0, -1.0f, 0));
                for (int i = 0; i < 10; i++)
                {
                    float ang = (i / 10.0f) * MathF.PI * 2f;
                    new Entity(parent: tentacles, model: "diamond", scale: new(0.38f, i % 2 == 0 ? 3.4f : 2.4f, 0.38f),
                        position: new(MathF.Cos(ang) * 1.42f, -1.4f, MathF.Sin(ang) * 1.42f),
                        color: i % 2 == 0 ? Rgb(80, 235, 255) : Color.Yellow, unlit: true);
                }
                parts.Bell = bell;
                parts.Core = core;
                parts.Tentacles = tentacles;
            }
            else if (enemyType == "overlord")
            {
                Color voidDark = Rgb(36, 18, 42);
                Color crimson = Rgb(245, 30, 65);
                var lLeg = new Entity(parent: root, position: new(-0.85f, 1.5f, 0));
                var rLeg = new Entity(parent: root, position: new(0.85f, 1.5f, 0));
                foreach (var leg in new[] { lLeg, rLeg })
                {
                    new Entity(parent: leg, model: "diamond", scale: new(0.88f, 1.72f, 0.88f), position: new(0, -0.8f, 0), color: voidDark);
                    new Entity(parent: leg, model: "diamond", scale: new(0.55f, 0.45f, 0.95f), position: new(0, -1.45f, 0.25f), color: crimson, unlit: true);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 3.1f, 0));
                new Entity(parent: torso, model: "diamond", scale: new(2.85f, 3.15f, 1.95f), color: voidDark);
                new Entity(parent: torso, model: "diamond", scale: new(0.72f, 2.05f, 0.72f), position: new(0, 0, 0.80f), color: crimson, unlit: true);
                new Entity(parent: torso, model: "sphere", scaleUniform: 0.45f, position: new(0, 0, 0.95f), color: Color.White, unlit: true);
                foreach (int side in new[] { -1, 1 })
                {
                    new Entity(parent: torso, model: "diamond", scale: new(0.48f, 1.95f, 0.48f), position: new(side * 0.98f, 1.70f, 0), rotation: new(0, 0, side * -25f), color: crimson, unlit: true);
                    new Entity(parent: torso, model: "diamond", scale: new(0.38f, 1.25f, 0.45f), position: new(side * 0.72f, 0.85f, -0.65f), rotation: new(-30, 0, 0), color: crimson, unlit: true);
                }

                var lArm = new Entity(parent: torso, position: new(-1.88f, 0.55f, 0));
                var rArm = new Entity(parent: torso, position: new(1.88f, 0.55f, 0));
                foreach (var arm in new[] { lArm, rArm })
                {
                    new Entity(parent: arm, model: "diamond", scale: new(0.90f, 2.25f, 0.90f), position: new(0, -0.85f, 0), color: voidDark);
                    foreach (float cx in new[] { -0.22f, 0.0f, 0.22f })
                        new Entity(parent: arm, model: "diamond", scale: new(0.28f, 1.25f, 0.45f), position: new(cx, -1.78f, 0.38f), rotation: new(-40, 0, 0), color: crimson, unlit: true);
                }

                var crown = new Entity(parent: torso, position: new(0, 2.05f, 0));
                new Entity(parent: crown, model: "diamond", scale: new(2.15f, 0.36f, 2.15f), color: crimson, unlit: true);
                new Entity(parent: crown, model: "diamond", scale: new(2.15f, 0.36f, 2.15f), rotation: new(0, 45f, 0), color: voidDark);
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
                parts.Crown = crown;
                parts.Torso = torso;
            }
            else if (enemyType == "lunar_chimera")
            {
                Color marble = Rgb(225, 235, 248);
                Color slate = Rgb(45, 58, 82);
                Color cyanGlow = Rgb(70, 225, 255);
                var core = new Entity(parent: root, position: new(0, 2.5f, 0));
                new Entity(parent: core, model: "sphere", scaleUniform: 0.98f, color: cyanGlow, unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(1.68f, 0.54f, 2.35f), color: marble);
                new Entity(parent: core, model: "diamond", scale: new(0.85f, 0.75f, 1.95f), color: slate);
                parts.Core = core;
                parts.Ring1 = new Entity(parent: core, model: "diamond", scale: new(2.68f, 0.24f, 2.68f), color: slate);
                parts.Ring2 = new Entity(parent: core, model: "diamond", scale: new(0.24f, 2.68f, 2.68f), color: cyanGlow, unlit: true);
                foreach (int side in new[] { -1, 1 })
                {
                    var wing = new Entity(parent: core, model: "cube", scale: new(1.28f, 0.20f, 0.70f), position: new(side * 1.44f, 0, -0.2f), rotation: new(0, 0, side * 15f), color: marble);
                    new Entity(parent: wing, model: "cube", scale: new(0.95f, 0.09f, 0.88f), position: new(0, 0, 0.42f), color: cyanGlow, unlit: true);
                }
            }
            else if (enemyType == "mithrix")
            {
                Color lunarWhite = Rgb(230, 238, 250);
                Color lunarSilver = Rgb(165, 182, 208);
                Color obsidian = Rgb(32, 38, 54);
                Color cobalt = Rgb(40, 95, 205);
                Color cyanFire = Rgb(75, 235, 255);

                // 1. Составные ноги с тассетами, наколенниками и сабатонами
                var lLeg = new Entity(parent: root, position: new(-0.55f, 1.95f, 0));
                var rLeg = new Entity(parent: root, position: new(0.55f, 1.95f, 0));
                foreach (var (side, leg) in new[] { (-1, lLeg), (1, rLeg) })
                {
                    new Entity(parent: leg, model: "cube", scale: new(0.46f, 0.95f, 0.48f), position: new(0, -0.45f, 0), color: obsidian);
                    new Entity(parent: leg, model: "diamond", scale: new(0.22f, 0.85f, 0.62f), position: new(side * 0.28f, -0.35f, 0), rotation: new(0, 0, side * -12f), color: lunarWhite);
                    new Entity(parent: leg, model: "diamond", scale: new(0.54f, 0.45f, 0.58f), position: new(0, -0.95f, 0.14f), color: lunarWhite);
                    new Entity(parent: leg, model: "diamond", scale: new(0.24f, 0.32f, 0.62f), position: new(0, -0.95f, 0.18f), color: cyanFire, unlit: true);
                    new Entity(parent: leg, model: "cube", scale: new(0.40f, 0.92f, 0.44f), position: new(0, -1.40f, 0), color: lunarSilver);
                    new Entity(parent: leg, model: "diamond", scale: new(0.52f, 0.32f, 0.85f), position: new(0, -1.85f, 0.16f), color: lunarWhite);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                // 2. Атлетичный торс, пояс и V-образный нагрудник
                var torso = new Entity(parent: root, position: new(0, 2.85f, 0));
                new Entity(parent: torso, model: "cube", scale: new(1.05f, 0.55f, 0.68f), position: new(0, -0.65f, 0), color: obsidian);
                new Entity(parent: torso, model: "diamond", scale: new(1.25f, 0.32f, 0.78f), position: new(0, -0.45f, 0), color: cobalt);
                new Entity(parent: torso, model: "diamond", scale: new(0.42f, 1.25f, 0.16f), position: new(0, -1.15f, 0.36f), color: cobalt);
                new Entity(parent: torso, model: "diamond", scale: new(1.75f, 1.55f, 1.05f), position: new(0, 0.20f, 0), color: lunarWhite);
                new Entity(parent: torso, model: "cube", scale: new(1.25f, 1.10f, 0.82f), position: new(0, 0.15f, 0), color: obsidian);
                new Entity(parent: torso, model: "cube", scale: new(0.55f, 1.15f, 0.92f), position: new(-0.32f, 0.22f, 0.05f), rotation: new(0, 0, 18f), color: lunarWhite);
                new Entity(parent: torso, model: "cube", scale: new(0.55f, 1.15f, 0.92f), position: new(0.32f, 0.22f, 0.05f), rotation: new(0, 0, -18f), color: lunarWhite);
                parts.Core = new Entity(parent: torso, model: "diamond", scale: new(0.48f, 0.78f, 0.45f), position: new(0, 0.25f, 0.42f), color: cyanFire, unlit: true);
                parts.Torso = torso;

                // 3. Развевающийся королевский плащ
                var cape = new Entity(parent: torso, position: new(-0.25f, 0.75f, -0.45f), rotation: new(14, 0, 0));
                new Entity(parent: cape, model: "cube", scale: new(1.45f, 0.28f, 0.35f), color: cobalt);
                new Entity(parent: cape, model: "diamond", scale: new(1.65f, 2.65f, 0.18f), position: new(0, -1.35f, -0.15f), color: cobalt);
                new Entity(parent: cape, model: "diamond", scale: new(0.85f, 2.45f, 0.20f), position: new(0, -1.35f, -0.17f), color: cyanFire, unlit: true);
                parts.Cape = cape;

                // 4. Шлем-корона Митрикса
                var head = new Entity(parent: torso, position: new(0, 1.18f, 0.05f));
                new Entity(parent: head, model: "sphere", scale: new(0.68f, 0.78f, 0.72f), color: lunarWhite);
                new Entity(parent: head, model: "diamond", scale: new(0.58f, 0.75f, 0.55f), position: new(0, 0.02f, 0.18f), color: obsidian);
                new Entity(parent: head, model: "cube", scale: new(0.10f, 0.62f, 0.35f), position: new(0, 0.06f, 0.30f), color: cyanFire, unlit: true);
                new Entity(parent: head, model: "sphere", scale: new(0.24f, 0.24f, 0.28f), position: new(0, 0.08f, 0.38f), color: Color.White, unlit: true);
                foreach (int side in new[] { -1, 1 })
                {
                    var crest = new Entity(parent: head, position: new(side * 0.22f, 0.38f, -0.02f), rotation: new(0, 0, side * -14f));
                    new Entity(parent: crest, model: "diamond", scale: new(0.18f, 1.15f, 0.42f), position: new(0, 0.45f, 0), color: lunarWhite);
                    new Entity(parent: crest, model: "diamond", scale: new(0.12f, 0.95f, 0.22f), position: new(side * -0.05f, 0.52f, 0.08f), color: cyanFire, unlit: true);
                }
                parts.Crown = new Entity(parent: head, model: "diamond", scale: new(1.35f, 1.35f, 0.12f), position: new(0, 0.35f, -0.32f), color: cyanFire, unlit: true);

                // 5. Асимметричные плечи и руки
                var lArm = new Entity(parent: torso, position: new(-1.05f, 0.62f, 0));
                new Entity(parent: lArm, model: "diamond", scale: new(0.95f, 0.65f, 0.95f), position: new(-0.15f, 0.28f, 0), rotation: new(0, 0, 25), color: lunarWhite);
                new Entity(parent: lArm, model: "diamond", scale: new(0.75f, 0.95f, 0.55f), position: new(-0.32f, 0.55f, 0), rotation: new(0, 0, 35), color: cyanFire, unlit: true);
                new Entity(parent: lArm, model: "cube", scale: new(0.38f, 1.35f, 0.38f), position: new(0, -0.55f, 0), color: obsidian);
                new Entity(parent: lArm, model: "cube", scale: new(0.44f, 0.65f, 0.44f), position: new(0, -1.15f, 0), color: lunarWhite);
                parts.LeftArm = lArm;

                var rArm = new Entity(parent: torso, position: new(1.05f, 0.62f, 0));
                new Entity(parent: rArm, model: "diamond", scale: new(0.72f, 0.55f, 0.72f), position: new(0, 0.20f, 0), color: lunarSilver);
                new Entity(parent: rArm, model: "cube", scale: new(0.40f, 1.35f, 0.40f), position: new(0, -0.55f, 0), color: obsidian);
                new Entity(parent: rArm, model: "cube", scale: new(0.46f, 0.65f, 0.46f), position: new(0, -1.15f, 0), color: lunarWhite);
                parts.RightArm = rArm;

                // 6. Колоссальный Лунный Молот в правой руке
                var hammer = new Entity(parent: rArm, position: new(0, -1.25f, 0.35f), rotation: new(65, 0, 0));
                new Entity(parent: hammer, model: "cube", scale: new(0.18f, 4.6f, 0.18f), position: new(0, 1.4f, 0), color: obsidian);
                foreach (float ry in new[] { 0.2f, 1.2f, 2.2f, 3.0f })
                    new Entity(parent: hammer, model: "cube", scale: new(0.24f, 0.12f, 0.24f), position: new(0, ry, 0), color: cyanFire, unlit: true);
                new Entity(parent: hammer, model: "diamond", scale: new(0.42f, 0.55f, 0.42f), position: new(0, -0.95f, 0), color: lunarWhite);

                var headBlock = new Entity(parent: hammer, position: new(0, 3.65f, 0));
                new Entity(parent: headBlock, model: "cube", scale: new(1.35f, 0.95f, 2.45f), color: obsidian);
                new Entity(parent: headBlock, model: "cube", scale: new(1.42f, 0.65f, 1.85f), color: lunarWhite);
                new Entity(parent: headBlock, model: "diamond", scale: new(1.25f, 0.85f, 0.95f), position: new(0, 0, 1.35f), color: lunarWhite);
                new Entity(parent: headBlock, model: "diamond", scale: new(0.95f, 0.75f, 1.15f), position: new(0, 0, -1.35f), color: lunarSilver);
                parts.HammerAura = new Entity(parent: headBlock, model: "cube", scale: new(1.48f, 0.28f, 2.55f), color: cyanFire, unlit: true);
                new Entity(parent: headBlock, model: "diamond", scale: new(1.75f, 0.18f, 1.75f), color: cyanFire, unlit: true);
                parts.Hammer = hammer;
                parts.PhaseRing = new Entity(parent: torso, model: "diamond", scale: new(3.6f, 0.15f, 3.6f), position: new(0, -0.2f, 0), color: cyanFire, unlit: true, enabled: false);
            }

            return parts;
        }
    }
}