using System;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Models
{
    public static partial class EnemyAndBossBuilder
    {
        private static bool TryBuildExpansionBossMesh(Entity root, EnemyParts parts, string enemyType)
        {
            if (enemyType == "echo_providence")
            {
                // Искажённый Провидец (Bulwark of the Weak — Echo): бело-индиговые доспехи, золотая филигрань, пурпурный плащ и золочёный клинок
                Color ivoryArmor = Rgb(236, 240, 250);
                Color deepIndigo = Rgb(28, 30, 58);
                Color royalGold = Rgb(245, 200, 68);
                Color cyanGlow = Rgb(70, 240, 255);
                Color capeCrimson = Rgb(198, 32, 92);
                Color capeTrim = Rgb(255, 85, 155);

                var lLeg = new Entity(parent: root, position: new(-0.62f, 1.88f, 0));
                var rLeg = new Entity(parent: root, position: new(0.62f, 1.88f, 0));
                foreach (var leg in new[] { lLeg, rLeg })
                {
                    new Entity(parent: leg, model: "cube", scale: new(0.54f, 0.92f, 0.56f), position: new(0, -0.46f, 0), color: deepIndigo);
                    new Entity(parent: leg, model: "cube", scale: new(0.60f, 0.68f, 0.62f), position: new(0, -0.40f, 0.04f), color: ivoryArmor);
                    new Entity(parent: leg, model: "diamond", scale: new(0.46f, 0.36f, 0.48f), position: new(0, -0.92f, 0.14f), color: royalGold);
                    new Entity(parent: leg, model: "cube", scale: new(0.52f, 0.92f, 0.58f), position: new(0, -1.40f, 0.02f), color: ivoryArmor);
                    new Entity(parent: leg, model: "cube", scale: new(0.22f, 0.86f, 0.62f), position: new(0, -1.40f, 0.05f), color: royalGold);
                    new Entity(parent: leg, model: "cube", scale: new(0.58f, 0.24f, 0.78f), position: new(0, -1.80f, 0.10f), color: deepIndigo);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 2.76f, 0));
                // Пояс и набедренные щитки
                new Entity(parent: torso, model: "cube", scale: new(1.42f, 0.44f, 0.88f), position: new(0, -0.78f, 0), color: deepIndigo);
                new Entity(parent: torso, model: "diamond", scale: new(0.58f, 0.72f, 0.22f), position: new(0, -0.98f, 0.46f), color: royalGold);
                new Entity(parent: torso, model: "cube", scale: new(0.32f, 0.78f, 0.76f), position: new(-0.82f, -0.92f, 0), rotation: new(0, 0, 12f), color: ivoryArmor);
                new Entity(parent: torso, model: "cube", scale: new(0.32f, 0.78f, 0.76f), position: new(0.82f, -0.92f, 0), rotation: new(0, 0, -12f), color: ivoryArmor);

                // Грудная кираса Провидца
                new Entity(parent: torso, model: "cube", scale: new(1.86f, 1.38f, 1.02f), position: new(0, 0.05f, 0), color: ivoryArmor);
                new Entity(parent: torso, model: "cube", scale: new(1.52f, 1.15f, 1.08f), position: new(0, -0.04f, 0), color: deepIndigo);
                new Entity(parent: torso, model: "diamond", scale: new(1.68f, 0.86f, 1.14f), position: new(0, 0.28f, 0.04f), color: ivoryArmor);
                new Entity(parent: torso, model: "diamond", scale: new(1.12f, 0.96f, 0.24f), position: new(0, 0.16f, 0.54f), color: royalGold);
                parts.Core = new Entity(parent: torso, model: "diamond", scale: new(0.58f, 0.74f, 0.36f), position: new(0, 0.18f, 0.62f), color: cyanGlow, unlit: true);

                // Королевский пурпурно-малиновый плащ
                var cape = new Entity(parent: torso, position: new(0, 0.68f, -0.50f), rotation: new(10f, 0, 0));
                new Entity(parent: cape, model: "cube", scale: new(1.96f, 0.26f, 0.30f), position: new(0, 0, 0), color: royalGold);
                new Entity(parent: cape, model: "cube", scale: new(1.88f, 2.55f, 0.12f), position: new(0, -1.32f, -0.06f), color: capeCrimson);
                new Entity(parent: cape, model: "cube", scale: new(1.96f, 0.18f, 0.15f), position: new(0, -2.54f, -0.06f), color: royalGold);
                new Entity(parent: cape, model: "diamond", scale: new(0.95f, 1.85f, 0.16f), position: new(0, -1.28f, -0.09f), color: capeTrim, unlit: true);
                parts.Cape = cape;

                // Высокая бирюзово-золотая корона-шлем Провидца
                var head = new Entity(parent: torso, position: new(0, 1.12f, 0.06f));
                new Entity(parent: head, model: "cube", scale: new(0.66f, 0.74f, 0.68f), position: new(0, 0.14f, 0), color: deepIndigo);
                new Entity(parent: head, model: "cube", scale: new(0.56f, 0.16f, 0.18f), position: new(0, 0.16f, 0.34f), color: cyanGlow, unlit: true);
                // Две высокие изогнутые пластины короны + центральный луч
                new Entity(parent: head, model: "diamond", scale: new(0.28f, 1.45f, 0.44f), position: new(-0.34f, 0.66f, -0.02f), rotation: new(0, 0, 10f), color: royalGold);
                new Entity(parent: head, model: "diamond", scale: new(0.28f, 1.45f, 0.44f), position: new(0.34f, 0.66f, -0.02f), rotation: new(0, 0, -10f), color: royalGold);
                new Entity(parent: head, model: "diamond", scale: new(0.22f, 1.68f, 0.32f), position: new(0, 0.84f, 0.02f), color: cyanGlow, unlit: true);
                new Entity(parent: head, model: "cube", scale: new(0.92f, 0.16f, 0.32f), position: new(0, 0.46f, 0.06f), color: ivoryArmor);
                parts.Crown = head;

                // Парящий ореол-полумесяц за спиной
                var halo = new Entity(parent: root, position: new(0, 3.65f, -0.58f));
                for (int i = 0; i < 8; i++)
                {
                    float a = (i / 8f) * MathF.PI * 2f;
                    new Entity(parent: halo, model: "diamond", scale: new(0.26f, 0.62f, 0.16f),
                        position: new(MathF.Cos(a) * 1.45f, MathF.Sin(a) * 1.45f, 0),
                        rotation: new(0, 0, a * (180f / MathF.PI)), color: i % 2 == 0 ? royalGold : cyanGlow, unlit: i % 2 != 0);
                }
                parts.Ring1 = halo;

                // Плечи и руки
                var lArm = new Entity(parent: torso, position: new(-1.34f, 0.58f, 0));
                var rArm = new Entity(parent: torso, position: new(1.34f, 0.58f, 0));
                foreach (var (arm, side) in new[] { (lArm, -1f), (rArm, 1f) })
                {
                    new Entity(parent: arm, model: "diamond", scale: new(0.96f, 0.66f, 0.88f), position: new(side * 0.16f, 0.14f, 0), rotation: new(0, 0, side * -18f), color: ivoryArmor);
                    new Entity(parent: arm, model: "cube", scale: new(0.84f, 0.18f, 0.80f), position: new(side * 0.16f, 0.28f, 0), color: royalGold);
                    new Entity(parent: arm, model: "diamond", scale: new(0.24f, 0.46f, 0.24f), position: new(side * 0.38f, 0.42f, 0), color: cyanGlow, unlit: true);
                    new Entity(parent: arm, model: "cube", scale: new(0.44f, 0.86f, 0.46f), position: new(0, -0.45f, 0), color: deepIndigo);
                    new Entity(parent: arm, model: "cube", scale: new(0.48f, 0.82f, 0.50f), position: new(0, -1.16f, 0.12f), color: ivoryArmor);
                    new Entity(parent: arm, model: "cube", scale: new(0.52f, 0.22f, 0.54f), position: new(0, -1.42f, 0.14f), color: royalGold);
                }

                // Исполинский Золочёный Меч Провидца (Gilded Sword of Providence)
                var sword = new Entity(parent: rArm, position: new(0, -1.45f, 0.38f), rotation: new(62f, 0, 0));
                new Entity(parent: sword, model: "cube", scale: new(0.18f, 0.85f, 0.18f), position: new(0, -0.25f, 0), color: deepIndigo);
                new Entity(parent: sword, model: "diamond", scale: new(0.36f, 0.36f, 0.36f), position: new(0, -0.72f, 0), color: royalGold);
                // Гарда
                new Entity(parent: sword, model: "diamond", scale: new(1.48f, 0.34f, 0.32f), position: new(0, 0.22f, 0), color: royalGold);
                new Entity(parent: sword, model: "sphere", scaleUniform: 0.30f, position: new(0, 0.22f, 0), color: cyanGlow, unlit: true);
                // Широкий клинок
                new Entity(parent: sword, model: "cube", scale: new(0.54f, 2.95f, 0.14f), position: new(0, 1.75f, 0), color: ivoryArmor);
                new Entity(parent: sword, model: "cube", scale: new(0.28f, 2.85f, 0.18f), position: new(0, 1.72f, 0), color: royalGold);
                new Entity(parent: sword, model: "diamond", scale: new(0.56f, 0.95f, 0.15f), position: new(0, 3.38f, 0), color: ivoryArmor);
                parts.HammerAura = new Entity(parent: sword, model: "diamond", scale: new(0.72f, 3.55f, 0.22f), position: new(0, 1.95f, 0), color: Rgb(70, 240, 255, 165), unlit: true);
                parts.Hammer = sword;

                parts.PhaseRing = new Entity(parent: root, model: "diamond", scale: new(4.2f, 0.16f, 4.2f), position: new(0, 0.22f, 0), color: Rgb(235, 55, 145, 165), unlit: true, enabled: false);
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
                return true;
            }

            if (enemyType == "false_son_boss")
            {
                // Ложный Сын (False Son): колосс из тёмного храмового камня и золота Аурелионита, пронзённый кристаллами Лунной Погибели
                Color templeStone = Rgb(92, 98, 112);
                Color darkSlate = Rgb(52, 56, 68);
                Color aurelionGold = Rgb(252, 204, 54);
                Color lunarCyan = Rgb(62, 224, 255);
                Color lunarBlue = Rgb(95, 145, 255);

                var lLeg = new Entity(parent: root, position: new(-0.82f, 2.02f, 0));
                var rLeg = new Entity(parent: root, position: new(0.82f, 2.02f, 0));
                foreach (var (leg, side) in new[] { (lLeg, -1f), (rLeg, 1f) })
                {
                    new Entity(parent: leg, model: "cube", scale: new(0.76f, 1.02f, 0.78f), position: new(0, -0.50f, 0), color: templeStone);
                    new Entity(parent: leg, model: "cube", scale: new(0.82f, 0.24f, 0.84f), position: new(0, -0.96f, 0.05f), color: aurelionGold);
                    new Entity(parent: leg, model: "cube", scale: new(0.72f, 1.02f, 0.80f), position: new(0, -1.48f, 0.02f), color: darkSlate);
                    new Entity(parent: leg, model: "diamond", scale: new(0.32f, 0.78f, 0.86f), position: new(side * 0.24f, -1.42f, 0.06f), color: aurelionGold, unlit: true);
                    new Entity(parent: leg, model: "cube", scale: new(0.84f, 0.28f, 0.98f), position: new(0, -1.90f, 0.10f), color: templeStone);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 3.05f, 0));
                new Entity(parent: torso, model: "cube", scale: new(1.78f, 0.56f, 1.12f), position: new(0, -0.86f, 0), color: darkSlate);
                new Entity(parent: torso, model: "diamond", scale: new(0.76f, 0.86f, 0.30f), position: new(0, -0.95f, 0.56f), color: aurelionGold);

                // Массивный каменный торс с золотыми прожилками и ядром Аурелионита
                new Entity(parent: torso, model: "cube", scale: new(2.35f, 1.56f, 1.38f), position: new(0, 0.08f, 0), color: templeStone);
                new Entity(parent: torso, model: "cube", scale: new(1.92f, 1.18f, 1.46f), position: new(0, 0.14f, 0), color: darkSlate);
                new Entity(parent: torso, model: "cube", scale: new(0.88f, 1.34f, 1.50f), position: new(0.62f, 0.16f, 0.02f), rotation: new(0, 0, -8f), color: aurelionGold);
                // Сияющее сердце Аурелионита и лунной порчи
                new Entity(parent: torso, model: "diamond", scale: new(0.96f, 1.05f, 0.44f), position: new(-0.12f, 0.16f, 0.66f), color: aurelionGold);
                parts.Core = new Entity(parent: torso, model: "diamond", scale: new(0.68f, 0.82f, 0.48f), position: new(-0.12f, 0.16f, 0.74f), color: lunarCyan, unlit: true);

                // Лунные шипы на левой стороне спины/торса
                var shardGroup = new Entity(parent: torso);
                for (int i = 0; i < 4; i++)
                {
                    new Entity(parent: shardGroup, model: "diamond", scale: new(0.32f, 1.15f + i * 0.12f, 0.32f),
                        position: new(-0.95f - i * 0.16f, 0.65f + i * 0.22f, -0.38f),
                        rotation: new(-18f, 0, 24f + i * 8f), color: i % 2 == 0 ? lunarCyan : lunarBlue, unlit: true);
                }
                parts.Tentacles = shardGroup;

                // Голова с асимметричными рогами (правый — золотой Аурелионит, левый — лунный кристалл)
                var head = new Entity(parent: torso, position: new(0, 1.28f, 0.10f));
                new Entity(parent: head, model: "cube", scale: new(0.84f, 0.86f, 0.84f), position: new(0, 0.18f, 0), color: templeStone);
                new Entity(parent: head, model: "cube", scale: new(0.90f, 0.24f, 0.88f), position: new(0, 0.42f, 0.04f), color: aurelionGold);
                new Entity(parent: head, model: "diamond", scale: new(0.46f, 0.26f, 0.24f), position: new(0, 0.18f, 0.44f), color: lunarCyan, unlit: true);
                // Правый величавый золотой рог
                new Entity(parent: head, model: "diamond", scale: new(0.32f, 1.55f, 0.38f), position: new(0.56f, 0.82f, -0.04f), rotation: new(0, 0, -24f), color: aurelionGold);
                // Левый расколотый лунный рог
                new Entity(parent: head, model: "diamond", scale: new(0.36f, 1.72f, 0.38f), position: new(-0.58f, 0.88f, 0.02f), rotation: new(8f, 0, 28f), color: lunarCyan, unlit: true);
                parts.Crown = head;

                // Расколотый золотой нимб за спиной
                var brokenHalo = new Entity(parent: root, position: new(0, 3.85f, -0.78f));
                for (int i = 0; i < 7; i++)
                {
                    if (i == 3) continue; // раскол в кольце
                    float a = (i / 7f) * MathF.PI * 2f;
                    new Entity(parent: brokenHalo, model: "cube", scale: new(0.68f, 0.24f, 0.20f),
                        position: new(MathF.Cos(a) * 1.85f, MathF.Sin(a) * 1.85f, 0),
                        rotation: new(0, 0, a * (180f / MathF.PI) + 90f),
                        color: i % 2 == 0 ? aurelionGold : lunarCyan, unlit: true);
                }
                parts.Ring1 = brokenHalo;

                // Могучие руки
                var lArm = new Entity(parent: torso, position: new(-1.68f, 0.58f, 0));
                var rArm = new Entity(parent: torso, position: new(1.68f, 0.58f, 0));
                // Левая рука — лунно-каменная перчатка
                new Entity(parent: lArm, model: "cube", scale: new(0.98f, 0.82f, 0.98f), position: new(-0.14f, 0.16f, 0), color: darkSlate);
                new Entity(parent: lArm, model: "diamond", scale: new(0.38f, 0.92f, 0.38f), position: new(-0.42f, 0.52f, 0), rotation: new(0, 0, 26f), color: lunarCyan, unlit: true);
                new Entity(parent: lArm, model: "cube", scale: new(0.64f, 1.05f, 0.64f), position: new(0, -0.58f, 0), color: templeStone);
                new Entity(parent: lArm, model: "cube", scale: new(0.76f, 0.98f, 0.78f), position: new(0, -1.38f, 0.12f), color: darkSlate);
                new Entity(parent: lArm, model: "diamond", scale: new(0.34f, 0.68f, 0.86f), position: new(-0.28f, -1.42f, 0.16f), color: lunarCyan, unlit: true);

                // Правая рука — золочёный наплечник Аурелионита и Палица Отверженного (Club of the Forsaken)
                new Entity(parent: rArm, model: "cube", scale: new(1.06f, 0.86f, 1.04f), position: new(0.16f, 0.18f, 0), color: aurelionGold);
                new Entity(parent: rArm, model: "cube", scale: new(0.66f, 1.05f, 0.66f), position: new(0, -0.58f, 0), color: templeStone);
                new Entity(parent: rArm, model: "cube", scale: new(0.74f, 0.94f, 0.76f), position: new(0, -1.35f, 0.12f), color: aurelionGold);

                var club = new Entity(parent: rArm, position: new(0, -1.56f, 0.42f), rotation: new(64f, 0, 0));
                new Entity(parent: club, model: "cube", scale: new(0.28f, 1.45f, 0.28f), position: new(0, 0.35f, 0), color: darkSlate);
                new Entity(parent: club, model: "cube", scale: new(0.36f, 0.24f, 0.36f), position: new(0, 0.92f, 0), color: aurelionGold);
                // Исполинское каменное навершие палицы, окованное золотом и кристаллами
                new Entity(parent: club, model: "cube", scale: new(1.12f, 2.15f, 1.12f), position: new(0, 2.08f, 0), color: templeStone);
                new Entity(parent: club, model: "cube", scale: new(1.22f, 0.30f, 1.22f), position: new(0, 1.42f, 0), color: aurelionGold);
                new Entity(parent: club, model: "cube", scale: new(1.26f, 0.32f, 1.26f), position: new(0, 2.68f, 0), color: aurelionGold);
                new Entity(parent: club, model: "diamond", scale: new(1.48f, 0.85f, 0.56f), position: new(0, 2.25f, 0), color: lunarCyan, unlit: true);
                new Entity(parent: club, model: "diamond", scale: new(0.56f, 0.85f, 1.48f), position: new(0, 2.25f, 0), color: lunarCyan, unlit: true);
                parts.HammerAura = new Entity(parent: club, model: "sphere", scale: new(1.65f, 2.45f, 1.65f), position: new(0, 2.15f, 0), color: Rgb(62, 224, 255, 85), unlit: true);
                parts.Hammer = club;

                parts.PhaseRing = new Entity(parent: root, model: "diamond", scale: new(4.8f, 0.20f, 4.8f), position: new(0, 0.24f, 0), color: Rgb(252, 204, 54, 175), unlit: true, enabled: false);
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
                return true;
            }

            if (enemyType == "prts_source")
            {
                // PRTS, Исходный Код (Originium Rhombus Matrix ◇): колоссальный чёрно-багровый ромбовидный ИИ-монолит с вращающимися контурами
                Color obsidian = Rgb(16, 18, 26);
                Color darkSteel = Rgb(36, 40, 54);
                Color origCrimson = Rgb(255, 36, 68);
                Color origAmber = Rgb(255, 118, 42);
                Color dataWhite = Rgb(255, 220, 228);

                var torso = new Entity(parent: root, position: new(0, 3.60f, 0));
                // Центральный октаэдр Ориджиниума
                new Entity(parent: torso, model: "diamond", scale: new(2.25f, 3.45f, 2.25f), color: obsidian);
                new Entity(parent: torso, model: "diamond", scale: new(2.42f, 0.36f, 2.42f), color: darkSteel);
                new Entity(parent: torso, model: "diamond", scale: new(1.65f, 2.85f, 2.46f), color: Rgb(255, 36, 68, 95), unlit: true);

                // Око Жрицы / Ядро PRTS спереди и сзади
                var core = new Entity(parent: torso, position: new(0, 0, 0));
                new Entity(parent: core, model: "diamond", scale: new(1.08f, 1.48f, 2.58f), color: origCrimson, unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(0.34f, 1.16f, 2.72f), color: dataWhite, unlit: true);
                new Entity(parent: core, model: "cube", scale: new(1.55f, 0.10f, 2.64f), color: origAmber, unlit: true);
                parts.Core = core;

                // Верхний и нижний шпили ретрансляции
                var spires = new Entity(parent: torso, position: new(0, 0, 0));
                new Entity(parent: spires, model: "diamond", scale: new(0.56f, 1.35f, 0.56f), position: new(0, 2.48f, 0), color: obsidian);
                new Entity(parent: spires, model: "diamond", scale: new(0.28f, 0.95f, 0.28f), position: new(0, 2.95f, 0), color: origCrimson, unlit: true);
                new Entity(parent: spires, model: "diamond", scale: new(0.56f, 1.25f, 0.56f), position: new(0, -2.42f, 0), color: obsidian);
                new Entity(parent: spires, model: "diamond", scale: new(0.26f, 0.85f, 0.26f), position: new(0, -2.85f, 0), color: origCrimson, unlit: true);
                parts.Crown = spires;

                // Ring1: Внешняя гигантская ромбовидная рама ◇
                var outerRhombus = new Entity(parent: torso, position: new(0, 0, 0));
                float rSpan = 2.45f;
                Vector3[] corners =
                {
                    new(0, rSpan, 0), new(rSpan, 0, 0), new(0, -rSpan, 0), new(-rSpan, 0, 0)
                };
                for (int i = 0; i < 4; i++)
                {
                    Vector3 a = corners[i];
                    Vector3 b = corners[(i + 1) % 4];
                    Vector3 mid = (a + b) * 0.5f;
                    float zRot = (i % 2 == 0) ? -45f : 45f;
                    new Entity(parent: outerRhombus, model: "cube", scale: new(0.22f, 3.48f, 0.26f), position: mid, rotation: new(0, 0, zRot), color: obsidian);
                    new Entity(parent: outerRhombus, model: "cube", scale: new(0.10f, 3.42f, 0.30f), position: mid, rotation: new(0, 0, zRot), color: origCrimson, unlit: true);
                    new Entity(parent: outerRhombus, model: "diamond", scale: new(0.52f, 0.52f, 0.52f), position: a, color: origAmber, unlit: true);
                }
                parts.Ring1 = outerRhombus;

                // Ring2: Внутренний ортогональный контур данных
                var innerRing = new Entity(parent: torso, rotation: new(28f, 45f, 0));
                for (int i = 0; i < 8; i++)
                {
                    float ang = (i / 8f) * MathF.PI * 2f;
                    new Entity(parent: innerRing, model: "cube", scale: new(0.85f, 0.14f, 0.18f),
                        position: new(MathF.Cos(ang) * 2.95f, 0, MathF.Sin(ang) * 2.95f),
                        rotation: new(0, -ang * (180f / MathF.PI), 0),
                        color: i % 2 == 0 ? origCrimson : darkSteel, unlit: i % 2 == 0);
                }
                parts.Ring2 = innerRing;

                // 6 спутниковых кристаллов ассимиляции
                var satGroup = new Entity(parent: torso);
                for (int i = 0; i < 6; i++)
                {
                    float ang = (i / 6f) * MathF.PI * 2f;
                    var sat = new Entity(parent: satGroup, position: new(MathF.Cos(ang) * 3.55f, MathF.Sin(ang * 2f) * 0.65f, MathF.Sin(ang) * 3.55f));
                    new Entity(parent: sat, model: "diamond", scale: new(0.44f, 0.96f, 0.44f), color: obsidian);
                    new Entity(parent: sat, model: "diamond", scale: new(0.26f, 0.68f, 0.50f), color: origCrimson, unlit: true);
                }
                parts.Tentacles = satGroup;

                parts.PhaseRing = new Entity(parent: torso, model: "diamond", scale: new(5.6f, 0.16f, 5.6f), color: Rgb(255, 45, 75, 165), unlit: true, enabled: false);
                parts.Torso = torso;
                return true;
            }

            if (enemyType == "binah_arbiter")
            {
                // Арбитр Бина (An Arbiter of the Head): чёрно-золотое одеяние Главы, парящие Столпы, золотой Нимб Ключей и мантии с золотыми линиями
                Color headBlack = Rgb(18, 18, 24);
                Color velvetDark = Rgb(32, 30, 40);
                Color arbiterGold = Rgb(246, 198, 58);
                Color paleGold = Rgb(255, 232, 128);
                Color porcelainSkin = Rgb(242, 235, 228);

                var lLeg = new Entity(parent: root, position: new(-0.44f, 1.68f, 0));
                var rLeg = new Entity(parent: root, position: new(0.44f, 1.68f, 0));
                foreach (var leg in new[] { lLeg, rLeg })
                {
                    new Entity(parent: leg, model: "cube", scale: new(0.38f, 0.84f, 0.40f), position: new(0, -0.42f, 0), color: headBlack);
                    new Entity(parent: leg, model: "cube", scale: new(0.36f, 0.84f, 0.42f), position: new(0, -1.24f, 0.02f), color: headBlack);
                    new Entity(parent: leg, model: "cube", scale: new(0.10f, 1.56f, 0.45f), position: new(0, -0.86f, 0.03f), color: arbiterGold);
                    new Entity(parent: leg, model: "cube", scale: new(0.40f, 0.20f, 0.58f), position: new(0, -1.60f, 0.08f), color: arbiterGold);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 2.48f, 0));
                // Расширяющиеся полы чёрно-золотого плаща-пальто Арбитра
                new Entity(parent: torso, model: "cube", scale: new(1.38f, 1.05f, 0.96f), position: new(0, -0.88f, -0.04f), color: headBlack);
                new Entity(parent: torso, model: "cube", scale: new(1.46f, 0.14f, 1.02f), position: new(0, -1.36f, -0.04f), color: arbiterGold);
                new Entity(parent: torso, model: "cube", scale: new(0.38f, 1.08f, 0.88f), position: new(-0.68f, -0.88f, 0), rotation: new(0, 0, 10f), color: velvetDark);
                new Entity(parent: torso, model: "cube", scale: new(0.38f, 1.08f, 0.88f), position: new(0.68f, -0.88f, 0), rotation: new(0, 0, -10f), color: velvetDark);

                // Верх камзола с высоким золотым воротником и Ключом-печатью на груди
                new Entity(parent: torso, model: "cube", scale: new(1.36f, 1.18f, 0.82f), position: new(0, 0.08f, 0), color: headBlack);
                new Entity(parent: torso, model: "cube", scale: new(0.18f, 1.16f, 0.86f), position: new(0, 0.06f, 0.02f), color: arbiterGold);
                new Entity(parent: torso, model: "cube", scale: new(1.06f, 0.34f, 0.78f), position: new(0, 0.66f, -0.04f), color: headBlack);
                new Entity(parent: torso, model: "cube", scale: new(1.12f, 0.10f, 0.82f), position: new(0, 0.80f, -0.04f), color: arbiterGold);
                parts.Core = new Entity(parent: torso, model: "diamond", scale: new(0.46f, 0.64f, 0.30f), position: new(0, 0.24f, 0.44f), color: paleGold, unlit: true);

                // Роскошная чёрно-золотая мантия Арбитра с золотыми цепями и узором Ока
                var cape = new Entity(parent: torso, position: new(0, 0.68f, -0.40f), rotation: new(9f, 0, 0));
                new Entity(parent: cape, model: "cube", scale: new(1.86f, 0.22f, 0.28f), position: new(0, 0, 0), color: arbiterGold);
                new Entity(parent: cape, model: "cube", scale: new(1.92f, 2.48f, 0.12f), position: new(0, -1.26f, -0.06f), color: headBlack);
                new Entity(parent: cape, model: "cube", scale: new(2.00f, 0.18f, 0.15f), position: new(0, -2.44f, -0.06f), color: arbiterGold);
                new Entity(parent: cape, model: "cube", scale: new(0.14f, 2.35f, 0.16f), position: new(-0.74f, -1.22f, -0.06f), color: arbiterGold);
                new Entity(parent: cape, model: "cube", scale: new(0.14f, 2.35f, 0.16f), position: new(0.74f, -1.22f, -0.06f), color: arbiterGold);
                new Entity(parent: cape, model: "diamond", scale: new(0.84f, 0.48f, 0.18f), position: new(0, -1.05f, -0.08f), color: paleGold, unlit: true);
                parts.Cape = cape;

                // Голова, тёмные длинные локоны с золотым подбоем и золотая заколка Главы
                var head = new Entity(parent: torso, position: new(0, 1.04f, 0.04f));
                new Entity(parent: head, model: "sphere", scale: new(0.62f, 0.66f, 0.60f), position: new(0, 0.14f, 0), color: porcelainSkin);
                // Золотые глаза
                new Entity(parent: head, model: "cube", scale: new(0.14f, 0.07f, 0.08f), position: new(-0.14f, 0.15f, 0.28f), color: paleGold, unlit: true);
                new Entity(parent: head, model: "cube", scale: new(0.14f, 0.07f, 0.08f), position: new(0.14f, 0.15f, 0.28f), color: paleGold, unlit: true);
                // Волосы с золотистым внутренним отливом
                new Entity(parent: head, model: "sphere", scale: new(0.72f, 0.56f, 0.70f), position: new(0, 0.28f, -0.05f), color: headBlack);
                new Entity(parent: head, model: "cube", scale: new(0.78f, 1.35f, 0.34f), position: new(0, -0.32f, -0.22f), color: headBlack);
                new Entity(parent: head, model: "cube", scale: new(0.68f, 1.25f, 0.16f), position: new(0, -0.34f, -0.14f), color: arbiterGold);
                new Entity(parent: head, model: "cube", scale: new(0.18f, 0.85f, 0.22f), position: new(-0.30f, -0.16f, 0.14f), color: headBlack);
                new Entity(parent: head, model: "cube", scale: new(0.18f, 0.85f, 0.22f), position: new(0.30f, -0.16f, 0.14f), color: headBlack);
                new Entity(parent: head, model: "diamond", scale: new(0.76f, 0.26f, 0.24f), position: new(0, 0.52f, 0.06f), color: arbiterGold, unlit: true);
                parts.Crown = head;

                // Золотой Нимб Ключей за спиной
                var keyHalo = new Entity(parent: root, position: new(0, 3.25f, -0.68f));
                for (int i = 0; i < 8; i++)
                {
                    float a = (i / 8f) * MathF.PI * 2f;
                    var keySpoke = new Entity(parent: keyHalo, position: new(MathF.Cos(a) * 1.55f, MathF.Sin(a) * 1.55f, 0),
                        rotation: new(0, 0, a * (180f / MathF.PI) - 90f));
                    new Entity(parent: keySpoke, model: "cube", scale: new(0.10f, 0.72f, 0.10f), color: arbiterGold, unlit: true);
                    new Entity(parent: keySpoke, model: "diamond", scale: new(0.32f, 0.32f, 0.14f), position: new(0, 0.42f, 0), color: paleGold, unlit: true);
                }
                parts.Ring1 = keyHalo;

                // 4 парящих чёрно-золотых Столпа Сингулярности (Pillars) вокруг плеч
                var pilGroup = new Entity(parent: torso);
                float[] pillarOffX = { -2.05f, -1.35f, 1.35f, 2.05f };
                float[] pillarOffY = { 0.35f, 0.95f, 0.95f, 0.35f };
                for (int i = 0; i < 4; i++)
                {
                    var pil = new Entity(parent: pilGroup, position: new(pillarOffX[i], pillarOffY[i], -0.45f));
                    new Entity(parent: pil, model: "cube", scale: new(0.34f, 1.28f, 0.34f), color: headBlack);
                    new Entity(parent: pil, model: "cube", scale: new(0.38f, 0.12f, 0.38f), position: new(0, 0.58f, 0), color: arbiterGold);
                    new Entity(parent: pil, model: "cube", scale: new(0.38f, 0.12f, 0.38f), position: new(0, -0.58f, 0), color: arbiterGold);
                    new Entity(parent: pil, model: "diamond", scale: new(0.18f, 0.86f, 0.40f), color: paleGold, unlit: true);
                }
                parts.Tentacles = pilGroup;

                // Руки и фокус-ключ «Фея / Цепь»
                var lArm = new Entity(parent: torso, position: new(-0.92f, 0.48f, 0));
                var rArm = new Entity(parent: torso, position: new(0.92f, 0.48f, 0));
                foreach (var (arm, side) in new[] { (lArm, -1f), (rArm, 1f) })
                {
                    new Entity(parent: arm, model: "cube", scale: new(0.56f, 0.42f, 0.56f), position: new(side * 0.08f, 0.08f, 0), color: arbiterGold);
                    new Entity(parent: arm, model: "cube", scale: new(0.36f, 1.18f, 0.38f), position: new(0, -0.62f, 0.08f), color: headBlack);
                    new Entity(parent: arm, model: "cube", scale: new(0.42f, 0.18f, 0.44f), position: new(0, -1.16f, 0.10f), color: arbiterGold);
                }

                var focusKey = new Entity(parent: rArm, position: new(0, -1.35f, 0.52f));
                new Entity(parent: focusKey, model: "diamond", scale: new(0.44f, 1.15f, 0.44f), color: headBlack);
                parts.HammerAura = new Entity(parent: focusKey, model: "diamond", scale: new(0.62f, 1.38f, 0.24f), color: paleGold, unlit: true);
                parts.Hammer = focusKey;

                parts.PhaseRing = new Entity(parent: root, model: "diamond", scale: new(4.6f, 0.18f, 4.6f), position: new(0, 0.22f, 0), color: Rgb(246, 198, 58, 175), unlit: true, enabled: false);
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
                return true;
            }

            if (enemyType == "roaring_knight")
            {
                // Рокочущий Рыцарь (The Roaring Knight): угольно-чёрный титан Тёмного Фонтана с ослепительно-белыми контурами, ветвистыми рогами и монохромным двуручником
                Color voidBlack = Rgb(10, 10, 15);
                Color shadowPlate = Rgb(18, 18, 26);
                Color starkWhite = Rgb(248, 250, 255);
                Color fountainCyan = Rgb(175, 245, 255);

                var lLeg = new Entity(parent: root, position: new(-0.56f, 1.90f, 0));
                var rLeg = new Entity(parent: root, position: new(0.56f, 1.90f, 0));
                foreach (var leg in new[] { lLeg, rLeg })
                {
                    new Entity(parent: leg, model: "cube", scale: new(0.48f, 0.94f, 0.50f), position: new(0, -0.46f, 0), color: voidBlack);
                    new Entity(parent: leg, model: "cube", scale: new(0.10f, 0.90f, 0.54f), position: new(0, -0.46f, 0.04f), color: starkWhite, unlit: true);
                    new Entity(parent: leg, model: "diamond", scale: new(0.42f, 0.52f, 0.48f), position: new(0, -0.94f, 0.16f), color: starkWhite, unlit: true);
                    new Entity(parent: leg, model: "cube", scale: new(0.44f, 0.96f, 0.50f), position: new(0, -1.42f, 0), color: voidBlack);
                    new Entity(parent: leg, model: "cube", scale: new(0.10f, 0.94f, 0.54f), position: new(0, -1.42f, 0.04f), color: starkWhite, unlit: true);
                    new Entity(parent: leg, model: "diamond", scale: new(0.46f, 0.26f, 0.88f), position: new(0, -1.82f, 0.14f), color: voidBlack);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 2.80f, 0));
                // Узкая талия и широкая угловатая грудная клетка с белыми контурными рёбрами
                new Entity(parent: torso, model: "cube", scale: new(1.12f, 0.58f, 0.76f), position: new(0, -0.78f, 0), color: voidBlack);
                new Entity(parent: torso, model: "diamond", scale: new(1.22f, 0.14f, 0.82f), position: new(0, -0.68f, 0), color: starkWhite, unlit: true);

                new Entity(parent: torso, model: "cube", scale: new(1.88f, 1.34f, 0.96f), position: new(0, 0.08f, 0), color: voidBlack);
                // Белые рёберные контуры Тёмного Фонтана
                for (int r = -1; r <= 1; r++)
                {
                    new Entity(parent: torso, model: "cube", scale: new(1.94f, 0.08f, 1.00f), position: new(0, 0.08f + r * 0.34f, 0), color: starkWhite, unlit: true);
                }
                parts.Core = new Entity(parent: torso, model: "diamond", scale: new(0.56f, 0.92f, 0.36f), position: new(0, 0.12f, 0.50f), color: starkWhite, unlit: true);

                // Рваный чёрный плащ с белой светящейся кромкой
                var cape = new Entity(parent: torso, position: new(0, 0.68f, -0.44f), rotation: new(12f, 0, 0));
                for (int c = -1; c <= 1; c++)
                {
                    new Entity(parent: cape, model: "diamond", scale: new(0.76f, 2.65f, 0.14f), position: new(c * 0.62f, -1.28f, -0.04f), rotation: new(0, 0, c * -6f), color: starkWhite, unlit: true);
                    new Entity(parent: cape, model: "diamond", scale: new(0.66f, 2.52f, 0.18f), position: new(c * 0.62f, -1.26f, -0.04f), rotation: new(0, 0, c * -6f), color: voidBlack);
                }
                parts.Cape = cape;

                // Заострённый шлем с сияющим белым оком и высокими ветвистыми оленьими рогами
                var head = new Entity(parent: torso, position: new(0, 1.12f, 0.06f));
                new Entity(parent: head, model: "diamond", scale: new(0.84f, 1.02f, 0.88f), position: new(0, 0.22f, 0), color: starkWhite, unlit: true);
                new Entity(parent: head, model: "diamond", scale: new(0.76f, 0.94f, 0.82f), position: new(0, 0.22f, 0), color: voidBlack);
                // Белое рокочущее око/визор во тьме шлема
                new Entity(parent: head, model: "diamond", scale: new(0.54f, 0.22f, 0.22f), position: new(0, 0.22f, 0.38f), color: starkWhite, unlit: true);
                new Entity(parent: head, model: "diamond", scale: new(0.18f, 0.42f, 0.24f), position: new(0, 0.22f, 0.40f), color: fountainCyan, unlit: true);

                // Ветвистые изогнутые рога Рыцаря (Antler Horns)
                foreach (float side in new[] { -1f, 1f })
                {
                    new Entity(parent: head, model: "cube", scale: new(0.16f, 1.38f, 0.16f), position: new(side * 0.44f, 0.92f, -0.04f), rotation: new(-8f, 0, side * -22f), color: voidBlack);
                    new Entity(parent: head, model: "cube", scale: new(0.08f, 1.40f, 0.20f), position: new(side * 0.44f, 0.92f, -0.04f), rotation: new(-8f, 0, side * -22f), color: starkWhite, unlit: true);
                    // Ветви рогов
                    new Entity(parent: head, model: "diamond", scale: new(0.14f, 0.78f, 0.14f), position: new(side * 0.78f, 1.28f, 0.04f), rotation: new(0, 0, side * -48f), color: starkWhite, unlit: true);
                    new Entity(parent: head, model: "diamond", scale: new(0.14f, 0.86f, 0.14f), position: new(side * 0.52f, 1.56f, -0.06f), rotation: new(0, 0, side * -8f), color: starkWhite, unlit: true);
                }
                parts.Crown = head;

                // Острые наплечники и когтистые латные руки
                var lArm = new Entity(parent: torso, position: new(-1.36f, 0.58f, 0));
                var rArm = new Entity(parent: torso, position: new(1.36f, 0.58f, 0));
                foreach (var (arm, side) in new[] { (lArm, -1f), (rArm, 1f) })
                {
                    new Entity(parent: arm, model: "diamond", scale: new(1.18f, 0.62f, 0.92f), position: new(side * 0.22f, 0.18f, 0), rotation: new(0, 0, side * -24f), color: starkWhite, unlit: true);
                    new Entity(parent: arm, model: "diamond", scale: new(1.06f, 0.52f, 0.82f), position: new(side * 0.22f, 0.18f, 0), rotation: new(0, 0, side * -24f), color: shadowPlate);
                    new Entity(parent: arm, model: "cube", scale: new(0.40f, 0.92f, 0.42f), position: new(0, -0.48f, 0), color: voidBlack);
                    new Entity(parent: arm, model: "cube", scale: new(0.44f, 0.88f, 0.46f), position: new(0, -1.22f, 0.12f), color: voidBlack);
                    new Entity(parent: arm, model: "cube", scale: new(0.10f, 0.88f, 0.50f), position: new(side * 0.18f, -1.22f, 0.12f), color: starkWhite, unlit: true);
                }

                // Монохромный Двуручный Клинок Тёмного Фонтана (чёрное лезвие с ослепительно-белой кромкой)
                var darkBlade = new Entity(parent: rArm, position: new(0, -1.48f, 0.40f), rotation: new(64f, 0, 0));
                new Entity(parent: darkBlade, model: "cube", scale: new(0.16f, 0.88f, 0.16f), position: new(0, -0.26f, 0), color: starkWhite, unlit: true);
                new Entity(parent: darkBlade, model: "diamond", scale: new(1.56f, 0.36f, 0.30f), position: new(0, 0.22f, 0), color: starkWhite, unlit: true);
                new Entity(parent: darkBlade, model: "diamond", scale: new(1.36f, 0.26f, 0.34f), position: new(0, 0.22f, 0), color: voidBlack);
                // Белый внешний контур клинка + угольно-чёрная сердцевина
                parts.HammerAura = new Entity(parent: darkBlade, model: "diamond", scale: new(0.84f, 3.85f, 0.16f), position: new(0, 2.05f, 0), color: starkWhite, unlit: true);
                new Entity(parent: darkBlade, model: "diamond", scale: new(0.62f, 3.58f, 0.22f), position: new(0, 2.02f, 0), color: voidBlack);
                new Entity(parent: darkBlade, model: "cube", scale: new(0.08f, 2.95f, 0.25f), position: new(0, 1.85f, 0), color: fountainCyan, unlit: true);
                parts.Hammer = darkBlade;

                // Монохромное руническое кольцо вокруг Рыцаря
                var runeRing = new Entity(parent: root, position: new(0, 3.15f, -0.55f));
                for (int i = 0; i < 6; i++)
                {
                    float a = (i / 6f) * MathF.PI * 2f;
                    new Entity(parent: runeRing, model: "diamond", scale: new(0.24f, 0.58f, 0.16f),
                        position: new(MathF.Cos(a) * 1.65f, MathF.Sin(a) * 1.65f, 0),
                        rotation: new(0, 0, a * (180f / MathF.PI)), color: starkWhite, unlit: true);
                }
                parts.Ring1 = runeRing;
                parts.PhaseRing = new Entity(parent: root, model: "diamond", scale: new(4.8f, 0.18f, 4.8f), position: new(0, 0.22f, 0), color: starkWhite, unlit: true, enabled: false);
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
                return true;
            }

            if (enemyType == "great_septimus")
            {
                // Великий Септимий, Хор Баланса (Harmonious Choir — The Great Septimus): монументальный золотой дирижёр с безликой фарфоровой маской, 6 золотыми крыльями и дирижёрским жезлом
                Color porcelainWhite = Rgb(246, 242, 236);
                Color orderGold = Rgb(252, 208, 72);
                Color radiantGold = Rgb(255, 236, 138);
                Color choirViolet = Rgb(108, 44, 184);
                Color violetGlow = Rgb(178, 105, 255);
                Color harmonyCyan = Rgb(110, 240, 255);

                // Парящий симфонический постамент / органные мантии у основания
                var lLeg = new Entity(parent: root, position: new(-0.78f, 2.05f, 0));
                var rLeg = new Entity(parent: root, position: new(0.78f, 2.05f, 0));
                foreach (var (panel, side) in new[] { (lLeg, -1f), (rLeg, 1f) })
                {
                    new Entity(parent: panel, model: "cube", scale: new(0.62f, 1.85f, 0.92f), position: new(0, -0.82f, 0), rotation: new(0, 0, side * -8f), color: porcelainWhite);
                    new Entity(parent: panel, model: "cube", scale: new(0.20f, 1.92f, 0.98f), position: new(side * 0.22f, -0.82f, 0), rotation: new(0, 0, side * -8f), color: orderGold);
                    new Entity(parent: panel, model: "diamond", scale: new(0.36f, 0.65f, 0.36f), position: new(side * 0.16f, -1.78f, 0.10f), color: radiantGold, unlit: true);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 3.32f, 0));
                // Центральная хоровая стола (фиолетово-золотая)
                new Entity(parent: torso, model: "cube", scale: new(1.68f, 1.45f, 1.18f), position: new(0, -1.05f, 0), color: choirViolet);
                new Entity(parent: torso, model: "diamond", scale: new(0.92f, 1.85f, 0.32f), position: new(0, -1.12f, 0.58f), color: orderGold);

                // Величественный фарфорово-золотой торс с органными трубами
                new Entity(parent: torso, model: "cube", scale: new(2.28f, 1.62f, 1.28f), position: new(0, 0.08f, 0), color: porcelainWhite);
                new Entity(parent: torso, model: "cube", scale: new(1.64f, 1.48f, 1.34f), position: new(0, 0.06f, 0), color: choirViolet);
                for (int p = -2; p <= 2; p++)
                {
                    new Entity(parent: torso, model: "cube", scale: new(0.14f, 1.38f + (2 - Math.Abs(p)) * 0.16f, 0.22f),
                        position: new(p * 0.36f, 0.14f, 0.64f), color: orderGold);
                }
                parts.Core = new Entity(parent: torso, model: "diamond", scale: new(0.86f, 1.08f, 0.46f), position: new(0, 0.18f, 0.72f), color: harmonyCyan, unlit: true);

                // Шесть многоярусных золотых крыльев Серафима Порядка
                var wings = new Entity(parent: torso, position: new(0, 0.56f, -0.58f));
                for (int tier = 0; tier < 3; tier++)
                {
                    float yOff = 0.72f - tier * 0.68f;
                    float span = 2.45f - tier * 0.32f;
                    float zTilt = 26f - tier * 18f;
                    foreach (float side in new[] { -1f, 1f })
                    {
                        var wingBranch = new Entity(parent: wings, position: new(side * 0.55f, yOff, -tier * 0.08f), rotation: new(0, side * -12f, side * -zTilt));
                        new Entity(parent: wingBranch, model: "diamond", scale: new(span, 0.44f, 0.16f), position: new(side * (span * 0.48f), 0, 0), color: orderGold);
                        new Entity(parent: wingBranch, model: "diamond", scale: new(span * 0.82f, 0.26f, 0.20f), position: new(side * (span * 0.52f), -0.16f, 0), color: tier == 0 ? radiantGold : violetGlow, unlit: true);
                    }
                }
                parts.Cape = wings;

                // Безликая золотая фарфоровая маска и Солнечный Венец (Sun-Crown Halo)
                var head = new Entity(parent: torso, position: new(0, 1.28f, 0.08f));
                new Entity(parent: head, model: "sphere", scale: new(0.84f, 1.04f, 0.78f), position: new(0, 0.22f, 0), color: porcelainWhite);
                new Entity(parent: head, model: "cube", scale: new(0.08f, 0.98f, 0.82f), position: new(0, 0.22f, 0.02f), color: orderGold, unlit: true);
                // Четыре золотых симметричных ока Хора на безликой маске
                foreach (float side in new[] { -1f, 1f })
                {
                    new Entity(parent: head, model: "diamond", scale: new(0.18f, 0.10f, 0.10f), position: new(side * 0.20f, 0.30f, 0.38f), color: radiantGold, unlit: true);
                    new Entity(parent: head, model: "diamond", scale: new(0.14f, 0.08f, 0.10f), position: new(side * 0.24f, 0.14f, 0.36f), color: violetGlow, unlit: true);
                }
                // Лучистая корона-солнце из 9 золотых лучей
                for (int r = -4; r <= 4; r++)
                {
                    float ang = r * 18f;
                    float rad = ang * (MathF.PI / 180f);
                    float rayLen = 0.92f - Math.Abs(r) * 0.08f;
                    new Entity(parent: head, model: "diamond", scale: new(0.18f, rayLen, 0.14f),
                        position: new(MathF.Sin(rad) * 0.78f, 0.42f + MathF.Cos(rad) * 0.78f, -0.06f),
                        rotation: new(0, 0, -ang), color: r % 2 == 0 ? radiantGold : orderGold, unlit: true);
                }
                parts.Crown = head;

                // Двойной вращающийся астрономический нимб за спиной (Ring1 и Ring2)
                var outerHalo = new Entity(parent: root, position: new(0, 4.15f, -0.82f));
                for (int i = 0; i < 12; i++)
                {
                    float a = (i / 12f) * MathF.PI * 2f;
                    new Entity(parent: outerHalo, model: "diamond", scale: new(0.28f, 0.72f, 0.16f),
                        position: new(MathF.Cos(a) * 2.35f, MathF.Sin(a) * 2.35f, 0),
                        rotation: new(0, 0, a * (180f / MathF.PI)),
                        color: i % 3 == 0 ? harmonyCyan : radiantGold, unlit: true);
                }
                parts.Ring1 = outerHalo;

                var innerHalo = new Entity(parent: root, position: new(0, 4.15f, -0.76f));
                for (int i = 0; i < 8; i++)
                {
                    float a = (i / 8f) * MathF.PI * 2f;
                    new Entity(parent: innerHalo, model: "cube", scale: new(0.58f, 0.14f, 0.14f),
                        position: new(MathF.Cos(a) * 1.48f, MathF.Sin(a) * 1.48f, 0),
                        rotation: new(0, 0, a * (180f / MathF.PI) + 90f),
                        color: violetGlow, unlit: true);
                }
                parts.Ring2 = innerHalo;

                // 4 парящие золотые ноты-сферы Хора
                var noteGroup = new Entity(parent: torso);
                for (int i = 0; i < 4; i++)
                {
                    float a = (i / 4f) * MathF.PI * 2f;
                    var note = new Entity(parent: noteGroup, position: new(MathF.Cos(a) * 2.45f, 0.35f, MathF.Sin(a) * 1.65f));
                    new Entity(parent: note, model: "sphere", scaleUniform: 0.36f, color: radiantGold, unlit: true);
                    new Entity(parent: note, model: "diamond", scale: new(0.62f, 0.14f, 0.62f), color: violetGlow, unlit: true);
                }
                parts.Tentacles = noteGroup;

                // Руки Дирижёра и Золотой Дирижёрский Жезл (Conductor's Baton)
                var lArm = new Entity(parent: torso, position: new(-1.62f, 0.58f, 0));
                var rArm = new Entity(parent: torso, position: new(1.62f, 0.58f, 0));
                foreach (var (arm, side) in new[] { (lArm, -1f), (rArm, 1f) })
                {
                    new Entity(parent: arm, model: "diamond", scale: new(1.12f, 0.72f, 0.96f), position: new(side * 0.18f, 0.18f, 0), rotation: new(0, 0, side * -18f), color: orderGold);
                    new Entity(parent: arm, model: "cube", scale: new(0.52f, 1.02f, 0.54f), position: new(0, -0.52f, 0.04f), color: porcelainWhite);
                    new Entity(parent: arm, model: "cube", scale: new(0.58f, 0.92f, 0.62f), position: new(0, -1.28f, 0.14f), color: choirViolet);
                    new Entity(parent: arm, model: "cube", scale: new(0.64f, 0.20f, 0.66f), position: new(0, -1.62f, 0.16f), color: orderGold);
                }

                var baton = new Entity(parent: rArm, position: new(0, -1.68f, 0.56f), rotation: new(52f, 0, 0));
                new Entity(parent: baton, model: "sphere", scaleUniform: 0.38f, position: new(0, -0.15f, 0), color: orderGold);
                new Entity(parent: baton, model: "cube", scale: new(0.14f, 2.85f, 0.14f), position: new(0, 1.35f, 0), color: radiantGold, unlit: true);
                new Entity(parent: baton, model: "diamond", scale: new(0.78f, 0.78f, 0.28f), position: new(0, 2.88f, 0), color: harmonyCyan, unlit: true);
                parts.HammerAura = new Entity(parent: baton, model: "diamond", scale: new(0.42f, 2.95f, 0.42f), position: new(0, 1.48f, 0), color: Rgb(178, 105, 255, 115), unlit: true);
                parts.Hammer = baton;

                parts.PhaseRing = new Entity(parent: root, model: "diamond", scale: new(5.8f, 0.20f, 5.8f), position: new(0, 0.26f, 0), color: Rgb(252, 208, 72, 185), unlit: true, enabled: false);
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
                return true;
            }

            return false;
        }
    }
}
