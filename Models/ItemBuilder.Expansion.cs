using System;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Models
{
    public static partial class ItemBuilder
    {
        private static bool TryBuildExpansionItemMesh(Entity root, string itemKey, Color itemCol)
        {
            // ================= 4 БЕЛЫХ ПРЕДМЕТА РАСШИРЕНИЯ =================
            if (itemKey == "iron_branch")
            {
                // Железная Ветвь [Dota 2] — раздвоенная деревянная веточка в железной оправе с сияющими листьями
                var tilt = new Entity(parent: root, rotation: new(0, 0, -24));
                new Entity(parent: tilt, model: "cube", scale: new(0.085f, 0.72f, 0.085f), position: new(-0.03f, -0.06f, 0), rotation: new(0, 0, 6), color: Rgb(108, 72, 42));
                new Entity(parent: tilt, model: "cube", scale: new(0.065f, 0.38f, 0.065f), position: new(0.11f, 0.08f, 0), rotation: new(0, 0, -34), color: Rgb(118, 80, 48));
                // Железные кольца-оковки на стволе и развилке
                foreach (float ry in new[] { -0.28f, -0.04f, 0.16f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.115f, 0.055f, 0.115f), position: new(-0.03f, ry, 0), color: Rgb(185, 198, 215));
                // Три листа с железной кромкой и изумрудной прожилкой
                var leaves = new (Vector3 pos, Vector3 rot, float sc)[]
                {
                    (new(-0.06f, 0.36f, 0f), new(8f, 0f, 10f), 1.0f),
                    (new(0.24f, 0.24f, 0.02f), new(-8f, 0f, -46f), 0.88f),
                    (new(-0.20f, 0.12f, -0.02f), new(6f, 0f, 48f), 0.82f)
                };
                foreach (var (pos, rot, sc) in leaves)
                {
                    new Entity(parent: tilt, model: "diamond", scale: new(0.24f * sc, 0.36f * sc, 0.05f), position: pos, rotation: rot, color: Rgb(195, 210, 228));
                    new Entity(parent: tilt, model: "diamond", scale: new(0.19f * sc, 0.31f * sc, 0.07f), position: pos, rotation: rot, color: Rgb(110, 225, 75), unlit: true);
                    new Entity(parent: tilt, model: "cube", scale: new(0.03f, 0.22f * sc, 0.08f), position: pos, rotation: rot, color: Rgb(195, 255, 135), unlit: true);
                }
                return true;
            }
            if (itemKey == "golden_apple")
            {
                // Золотое Яблоко [Minecraft] — ступенчатое воксельное золотое яблоко с черенком и изумрудным листом
                new Entity(parent: root, model: "cube", scale: new(0.48f, 0.44f, 0.48f), position: new(0, -0.05f, 0), color: Rgb(255, 205, 35), unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.56f, 0.30f, 0.42f), position: new(0, -0.04f, 0), color: Rgb(255, 220, 58), unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.42f, 0.30f, 0.56f), position: new(0, -0.04f, 0), color: Rgb(242, 188, 28));
                new Entity(parent: root, model: "cube", scale: new(0.36f, 0.52f, 0.36f), position: new(0, -0.05f, 0), color: Rgb(235, 175, 24));
                // Пиксельный золотой блик спереди
                new Entity(parent: root, model: "cube", scale: new(0.12f, 0.12f, 0.05f), position: new(-0.12f, 0.08f, 0.27f), color: Rgb(255, 252, 200), unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.07f, 0.07f, 0.05f), position: new(-0.18f, -0.02f, 0.27f), color: Rgb(255, 245, 170), unlit: true);
                // Дубовый черенок и воксельный листок сверху
                new Entity(parent: root, model: "cube", scale: new(0.085f, 0.22f, 0.085f), position: new(0, 0.28f, 0), rotation: new(0, 0, 10), color: Rgb(105, 65, 32));
                new Entity(parent: root, model: "cube", scale: new(0.22f, 0.08f, 0.14f), position: new(0.15f, 0.32f, 0), rotation: new(8, 0, -24), color: Rgb(75, 215, 85), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.68f, position: new(0, -0.04f, 0), color: Rgb(255, 215, 55, 60), unlit: true);
                return true;
            }
            if (itemKey == "sad_onion")
            {
                // Грустная Луковица [Isaac] — бледно-зелёная луковица с ростком, печальными глазками и голубыми слезами
                new Entity(parent: root, model: "sphere", scale: new(0.56f, 0.50f, 0.56f), position: new(0, -0.08f, 0), color: Rgb(222, 236, 172));
                new Entity(parent: root, model: "diamond", scale: new(0.36f, 0.36f, 0.36f), position: new(0, 0.14f, 0), color: Rgb(205, 226, 148));
                // Продольные дольки шелухи и корешки снизу
                foreach (float ang in new[] { 0f, 60f, 120f })
                    new Entity(parent: root, model: "sphere", scale: new(0.58f, 0.46f, 0.14f), position: new(0, -0.08f, 0), rotation: new(0, ang, 0), color: Rgb(198, 218, 142));
                foreach (float rx in new[] { -0.09f, 0f, 0.09f })
                    new Entity(parent: root, model: "cube", scale: new(0.03f, 0.12f, 0.03f), position: new(rx, -0.35f, 0), rotation: new(0, 0, rx * 180f), color: Rgb(168, 148, 108));
                // Зелёные перья ростка сверху
                new Entity(parent: root, model: "diamond", scale: new(0.10f, 0.30f, 0.10f), position: new(-0.04f, 0.36f, 0), rotation: new(0, 0, 14), color: Rgb(95, 210, 75), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.09f, 0.26f, 0.09f), position: new(0.05f, 0.34f, 0), rotation: new(0, 0, -18), color: Rgb(120, 232, 92), unlit: true);
                // Печальные глазки и ручьи едких слёз
                foreach (float sx in new[] { -1f, 1f })
                {
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.09f, position: new(sx * 0.12f, -0.02f, 0.25f), color: Rgb(35, 32, 42));
                    new Entity(parent: root, model: "cube", scale: new(0.11f, 0.028f, 0.03f), position: new(sx * 0.12f, 0.05f, 0.26f), rotation: new(0, 0, sx * -18f), color: Rgb(75, 68, 62));
                    new Entity(parent: root, model: "cube", scale: new(0.045f, 0.20f, 0.045f), position: new(sx * 0.12f, -0.14f, 0.26f), color: Rgb(85, 215, 255), unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(0.09f, 0.14f, 0.09f), position: new(sx * 0.22f, -0.22f, 0.22f), color: Rgb(115, 232, 255), unlit: true);
                }
                return true;
            }
            if (itemKey == "orirock_cluster")
            {
                // Кластер Орирока [Arknights] — связка угловатых камней Ориджиниума в жёлто-чёрной стяжке Родоса
                var rocks = new (Vector3 pos, Vector3 sc, Vector3 rot, Color col)[]
                {
                    (new(-0.10f, -0.04f, 0.04f), new(0.34f, 0.38f, 0.32f), new(8f, 14f, -6f), Rgb(138, 126, 108)),
                    (new(0.13f, 0.02f, -0.03f), new(0.32f, 0.42f, 0.30f), new(-6f, -18f, 10f), Rgb(122, 110, 94)),
                    (new(0.02f, -0.12f, 0.14f), new(0.28f, 0.28f, 0.26f), new(12f, 25f, 4f), Rgb(152, 138, 118)),
                    (new(-0.04f, 0.14f, -0.10f), new(0.26f, 0.32f, 0.26f), new(-10f, 8f, -12f), Rgb(106, 96, 82))
                };
                foreach (var (pos, sc, rot, col) in rocks)
                    new Entity(parent: root, model: "cube", scale: sc, position: pos, rotation: rot, color: col);
                // Тёмные и янтарные кристаллы Ориджиниума на сколах породы
                new Entity(parent: root, model: "diamond", scale: new(0.14f, 0.34f, 0.14f), position: new(-0.14f, 0.22f, 0.06f), rotation: new(12, 0, 22), color: Rgb(38, 34, 34));
                new Entity(parent: root, model: "diamond", scale: new(0.12f, 0.30f, 0.12f), position: new(0.16f, 0.24f, 0.02f), rotation: new(-10, 0, -20), color: Rgb(255, 175, 42), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.10f, 0.22f, 0.10f), position: new(0.02f, -0.02f, 0.26f), rotation: new(38, 0, 0), color: Rgb(255, 195, 65), unlit: true);
                // Стяжная промышленная лента Rhodes Island
                new Entity(parent: root, model: "cube", scale: new(0.58f, 0.09f, 0.50f), position: new(0.01f, -0.04f, 0.02f), color: Rgb(245, 195, 35));
                foreach (float sx in new[] { -0.18f, 0f, 0.18f })
                    new Entity(parent: root, model: "cube", scale: new(0.06f, 0.10f, 0.52f), position: new(sx, -0.04f, 0.02f), color: Rgb(32, 32, 38));
                return true;
            }

            // ================= 4 ЗЕЛЁНЫХ ПРЕДМЕТА РАСШИРЕНИЯ =================
            if (itemKey == "maelstrom")
            {
                // Маэлстром [Dota 2] — серебристо-синий боевой молот в дугах грозовых молний
                var tilt = new Entity(parent: root, rotation: new(0, 0, -28));
                new Entity(parent: tilt, model: "cube", scale: new(0.09f, 0.72f, 0.09f), position: new(0, -0.12f, 0), color: Rgb(58, 82, 118));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.48f, 0), color: Rgb(185, 208, 235));
                // Массивное навершие молота с ударными бойками и рунным ядром
                new Entity(parent: tilt, model: "cube", scale: new(0.54f, 0.26f, 0.26f), position: new(0, 0.22f, 0), color: Rgb(172, 196, 226));
                foreach (float sx in new[] { -1f, 1f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.08f, 0.30f, 0.30f), position: new(sx * 0.28f, 0.22f, 0), color: Rgb(218, 234, 252));
                new Entity(parent: tilt, model: "diamond", scale: new(0.24f, 0.20f, 0.32f), position: new(0, 0.22f, 0), color: Rgb(65, 210, 255), unlit: true);
                // Искрящие зигзаги молний вокруг бойка
                var sparks = new (Vector3 pos, Vector3 rot)[]
                {
                    (new(-0.26f, 0.40f, 0.08f), new(15f, 0f, 38f)),
                    (new(0.28f, 0.38f, -0.06f), new(-12f, 0f, -42f)),
                    (new(0.18f, 0.04f, 0.18f), new(28f, 0f, 52f)),
                    (new(-0.20f, 0.06f, -0.16f), new(-24f, 0f, -48f))
                };
                foreach (var (pos, rot) in sparks)
                    new Entity(parent: tilt, model: "diamond", scale: new(0.06f, 0.28f, 0.06f), position: pos, rotation: rot, color: Rgb(115, 238, 255), unlit: true);
                return true;
            }
            if (itemKey == "ender_pearl")
            {
                // Эндер-Жемчуг [Minecraft] — глянцевая тёмно-бирюзовая сфера с фиолетово-изумрудным оком Края внутри
                new Entity(parent: root, model: "sphere", scaleUniform: 0.56f, color: Rgb(16, 74, 68));
                new Entity(parent: root, model: "sphere", scaleUniform: 0.34f, position: new(0, 0, 0.06f), color: Rgb(75, 235, 195), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.14f, 0.28f, 0.14f), position: new(0, 0, 0.16f), color: Rgb(165, 55, 245), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.66f, color: Rgb(55, 215, 185, 110), unlit: true);
                // Орбитальные частицы телепортации Края
                new Entity(parent: root, model: "diamond", scale: new(0.76f, 0.06f, 0.76f), rotation: new(24, 0, -18), color: Rgb(185, 75, 255, 140), unlit: true);
                foreach (var (px, py, pz) in new[] { (-0.34f, 0.22f, 0.12f), (0.32f, -0.20f, -0.14f), (0.12f, 0.36f, -0.18f) })
                    new Entity(parent: root, model: "diamond", scaleUniform: 0.10f, position: new(px, py, pz), color: Rgb(205, 95, 255), unlit: true);
                return true;
            }
            if (itemKey == "holy_mantle")
            {
                // Святая Мантия [Isaac] — небесно-голубой расширяющийся святой крест с золотым нимбом сверху
                new Entity(parent: root, model: "cube", scale: new(0.18f, 0.68f, 0.12f), position: new(0, -0.05f, 0), color: Rgb(95, 198, 255), unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.68f, 0.18f, 0.12f), position: new(0, -0.05f, 0), color: Rgb(95, 198, 255), unlit: true);
                // Расширяющиеся лапки креста (Cross Pattee)
                new Entity(parent: root, model: "diamond", scale: new(0.30f, 0.20f, 0.13f), position: new(0, 0.26f, 0), color: Rgb(125, 215, 255), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.30f, 0.20f, 0.13f), position: new(0, -0.36f, 0), color: Rgb(125, 215, 255), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.20f, 0.30f, 0.13f), position: new(-0.31f, -0.05f, 0), color: Rgb(125, 215, 255), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.20f, 0.30f, 0.13f), position: new(0.31f, -0.05f, 0), color: Rgb(125, 215, 255), unlit: true);
                // Белоснежное внутреннее сияние креста
                new Entity(parent: root, model: "cube", scale: new(0.08f, 0.52f, 0.15f), position: new(0, -0.05f, 0), color: Color.White, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.52f, 0.08f, 0.15f), position: new(0, -0.05f, 0), color: Color.White, unlit: true);
                // Парящий золотой нимб над крестом
                var halo = new Entity(parent: root, position: new(0, 0.42f, 0), rotation: new(16, 0, -8));
                for (int i = 0; i < 8; i++)
                {
                    float a = i * MathF.PI * 0.25f;
                    new Entity(parent: halo, model: "cube", scale: new(0.13f, 0.05f, 0.06f),
                        position: new(MathF.Cos(a) * 0.20f, 0, MathF.Sin(a) * 0.20f),
                        rotation: new(0, -i * 45f, 0), color: Color.Gold, unlit: true);
                }
                return true;
            }
            if (itemKey == "poca_sp_battery")
            {
                // Батарея SP Родоса [Arknights] — тактический мятно-антрацитовый конденсатор SP с 3 индикаторами заряда
                var tilt = new Entity(parent: root, rotation: new(0, 0, 14));
                new Entity(parent: tilt, model: "cube", scale: new(0.38f, 0.62f, 0.34f), color: Rgb(42, 48, 56));
                new Entity(parent: tilt, model: "cube", scale: new(0.44f, 0.09f, 0.40f), position: new(0, 0.31f, 0), color: Rgb(74, 84, 96));
                new Entity(parent: tilt, model: "cube", scale: new(0.44f, 0.09f, 0.40f), position: new(0, -0.31f, 0), color: Rgb(74, 84, 96));
                // Три светящиеся мятно-бирюзовые шкалы SP-заряда
                foreach (float cy in new[] { -0.15f, 0f, 0.15f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.30f, 0.085f, 0.38f), position: new(0, cy, 0), color: Rgb(85, 255, 195), unlit: true);
                // Верхние клеммы и дуга разряда
                foreach (float sx in new[] { -0.11f, 0.11f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.07f, 0.11f, 0.07f), position: new(sx, 0.40f, 0), color: Rgb(215, 226, 238));
                new Entity(parent: tilt, model: "cube", scale: new(0.20f, 0.035f, 0.035f), position: new(0, 0.45f, 0), color: Rgb(135, 255, 225), unlit: true);
                return true;
            }

            // ================= 4 КРАСНЫХ (ЛЕГЕНДАРНЫХ) ПРЕДМЕТА РАСШИРЕНИЯ =================
            if (itemKey == "black_king_bar")
            {
                // Посох Чёрного Короля [Dota 2] — грозный золотой рогатый посох с пылающим ядром Аватара
                var tilt = new Entity(parent: root, rotation: new(0, 0, -26));
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.76f, 0.10f), position: new(0, -0.10f, 0), color: Rgb(54, 42, 34));
                foreach (float gy in new[] { -0.36f, -0.16f, 0.04f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.135f, 0.05f, 0.135f), position: new(0, gy, 0), color: Rgb(235, 185, 45));
                // Золотое навершие короля и пылающий янтарный кристалл
                new Entity(parent: tilt, model: "sphere", scale: new(0.28f, 0.26f, 0.24f), position: new(0, 0.22f, 0), color: Rgb(220, 168, 38));
                new Entity(parent: tilt, model: "diamond", scale: new(0.18f, 0.30f, 0.26f), position: new(0, 0.24f, 0), color: Rgb(255, 238, 105), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.12f, 0.24f, 0.12f), position: new(0, 0.42f, 0), color: Color.Gold, unlit: true);
                // Изогнутые золотые рога по бокам навершия
                foreach (float sx in new[] { -1f, 1f })
                {
                    new Entity(parent: tilt, model: "diamond", scale: new(0.12f, 0.34f, 0.12f),
                        position: new(sx * 0.20f, 0.26f, 0), rotation: new(0, 0, sx * -42f), color: Rgb(255, 200, 45), unlit: true);
                    new Entity(parent: tilt, model: "diamond", scale: new(0.09f, 0.26f, 0.09f),
                        position: new(sx * 0.28f, 0.42f, 0), rotation: new(0, 0, sx * -12f), color: Rgb(255, 228, 95), unlit: true);
                }
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.50f, position: new(0, 0.24f, 0), color: Rgb(255, 205, 55, 65), unlit: true);
                return true;
            }
            if (itemKey == "totem_undying")
            {
                // Тотем Бессмертия [Minecraft] — золотая воксельная фигурка с крыльями и сияющими изумрудными глазами
                new Entity(parent: root, model: "cube", scale: new(0.30f, 0.38f, 0.20f), position: new(0, -0.15f, 0), color: Rgb(238, 192, 46));
                new Entity(parent: root, model: "cube", scale: new(0.22f, 0.16f, 0.18f), position: new(0, -0.37f, 0), color: Rgb(208, 160, 34));
                new Entity(parent: root, model: "cube", scale: new(0.24f, 0.11f, 0.24f), position: new(0, -0.10f, 0.02f), color: Rgb(255, 220, 78));
                // Расправленные золотые крылья
                new Entity(parent: root, model: "cube", scale: new(0.76f, 0.15f, 0.13f), position: new(0, -0.02f, -0.02f), color: Rgb(255, 212, 62), unlit: true);
                foreach (float sx in new[] { -1f, 1f })
                    new Entity(parent: root, model: "cube", scale: new(0.18f, 0.12f, 0.13f), position: new(sx * 0.33f, 0.07f, -0.02f), color: Rgb(255, 228, 95), unlit: true);
                // Голова тотема, массивный нос и ярко-зелёные изумрудные глаза
                new Entity(parent: root, model: "cube", scale: new(0.36f, 0.34f, 0.28f), position: new(0, 0.20f, 0), color: Rgb(250, 208, 58));
                new Entity(parent: root, model: "cube", scale: new(0.09f, 0.15f, 0.09f), position: new(0, 0.16f, 0.16f), color: Rgb(222, 174, 38));
                foreach (float sx in new[] { -0.10f, 0.10f })
                    new Entity(parent: root, model: "cube", scale: new(0.09f, 0.075f, 0.06f), position: new(sx, 0.24f, 0.14f), color: Rgb(45, 255, 115), unlit: true);
                return true;
            }
            if (itemKey == "brimstone")
            {
                // Сера (Brimstone) [Isaac] — кроваво-красный алхимический крест Левиафана со знаком бесконечности в основании
                Color brimCol = Rgb(235, 25, 48);
                Color coreCol = Rgb(255, 95, 112);
                // Центральный стержень и две поперечные перекладины
                new Entity(parent: root, model: "cube", scale: new(0.095f, 0.68f, 0.095f), position: new(0, 0.08f, 0), color: brimCol, unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.18f, 0.22f, 0.13f), position: new(0, 0.44f, 0), color: coreCol, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.56f, 0.085f, 0.095f), position: new(0, 0.08f, 0), color: brimCol, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.40f, 0.085f, 0.095f), position: new(0, 0.26f, 0), color: brimCol, unlit: true);
                // Символ бесконечности (две петли) снизу
                foreach (float sx in new[] { -1f, 1f })
                {
                    float cx = sx * 0.18f, cy = -0.24f;
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * MathF.PI / 3f;
                        new Entity(parent: root, model: "cube", scale: new(0.13f, 0.065f, 0.095f),
                            position: new(cx + MathF.Cos(a) * 0.15f, cy + MathF.Sin(a) * 0.11f, 0),
                            rotation: new(0, 0, a * 180f / MathF.PI + 90f), color: brimCol, unlit: true);
                    }
                }
                new Entity(parent: root, model: "sphere", scaleUniform: 0.13f, position: new(0, -0.24f, 0), color: coreCol, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.68f, color: Rgb(225, 20, 42, 55), unlit: true);
                return true;
            }
            if (itemKey == "silverash_truesilver")
            {
                // Истинно-Серебряный Разрез [Arknights] — аристократическая трость-клинок СильверЭша в окружении 3 волн разреза
                var tilt = new Entity(parent: root, rotation: new(0, 0, -30));
                new Entity(parent: tilt, model: "cube", scale: new(0.075f, 0.30f, 0.075f), position: new(0, -0.34f, 0), color: Rgb(36, 42, 54));
                new Entity(parent: tilt, model: "diamond", scale: new(0.18f, 0.12f, 0.10f), position: new(-0.05f, -0.48f, 0), rotation: new(0, 0, 28), color: Rgb(215, 228, 245));
                new Entity(parent: tilt, model: "diamond", scale: new(0.46f, 0.12f, 0.14f), position: new(0, -0.17f, 0), color: Rgb(195, 212, 235));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.11f, position: new(0, -0.17f, 0.05f), color: Color.Gold, unlit: true);
                // Ослепительно белый клинок Истинного Серебра
                new Entity(parent: tilt, model: "diamond", scale: new(0.15f, 0.86f, 0.07f), position: new(0, 0.20f, 0), color: Color.White, unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.07f, 0.72f, 0.10f), position: new(0, 0.18f, 0), color: Rgb(155, 218, 255), unlit: true);
                // Три серебряные дуги-волны вокруг клинка
                var arcs = new (Vector3 pos, Vector3 rot, float sc)[]
                {
                    (new(0.16f, 0.08f, 0.08f), new(18f, 20f, -15f), 0.56f),
                    (new(0.22f, 0.22f, -0.06f), new(-16f, -18f, -22f), 0.68f),
                    (new(0.12f, 0.36f, 0.10f), new(22f, 12f, -8f), 0.48f)
                };
                foreach (var (pos, rot, sc) in arcs)
                    new Entity(parent: tilt, model: "diamond", scale: new(0.10f, sc, 0.04f), position: pos, rotation: rot, color: Rgb(210, 240, 255, 195), unlit: true);
                return true;
            }

            // ================= 4 ПРЕДМЕТА ПУСТОТЫ РАСШИРЕНИЯ =================
            if (itemKey == "desolator_void")
            {
                // Опустошитель Бездны [Void / Dota 2] — изогнутый ало-пурпурный серп со сферой сингулярности внутри лезвия
                var tilt = new Entity(parent: root, rotation: new(0, 0, -28));
                new Entity(parent: tilt, model: "cube", scale: new(0.085f, 0.76f, 0.085f), position: new(-0.08f, -0.08f, 0), color: Rgb(38, 22, 50));
                // Двойные сегментированные лезвия серпа
                new Entity(parent: tilt, model: "diamond", scale: new(0.18f, 0.54f, 0.09f), position: new(0.08f, 0.32f, 0), rotation: new(0, 0, -48), color: Rgb(245, 38, 95), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.16f, 0.46f, 0.09f), position: new(0.14f, 0.02f, 0), rotation: new(0, 0, 44), color: Rgb(235, 45, 115), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.10f, 0.38f, 0.11f), position: new(0.24f, 0.18f, 0), rotation: new(0, 0, 4), color: Rgb(255, 120, 170), unlit: true);
                // Чёрное ядро Бездны в центре серпа
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.21f, position: new(0.08f, 0.18f, 0), color: Rgb(16, 8, 26));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.32f, position: new(0.08f, 0.18f, 0), color: Rgb(235, 45, 115, 105), unlit: true);
                return true;
            }
            if (itemKey == "sculk_catalyst")
            {
                // Скалк-Катализатор [Void / Minecraft] — тёмно-синий блок Скалка с костяным основанием и бирюзовыми сенсорами душ
                new Entity(parent: root, model: "cube", scale: new(0.54f, 0.34f, 0.54f), position: new(0, 0.08f, 0), color: Rgb(14, 34, 46));
                new Entity(parent: root, model: "cube", scale: new(0.56f, 0.22f, 0.56f), position: new(0, -0.18f, 0), color: Rgb(218, 222, 204));
                foreach (float sx in new[] { -0.22f, 0.22f })
                {
                    foreach (float sz in new[] { -0.22f, 0.22f })
                    {
                        new Entity(parent: root, model: "cube", scale: new(0.12f, 0.42f, 0.12f), position: new(sx, -0.06f, sz), color: Rgb(228, 232, 214));
                        new Entity(parent: root, model: "diamond", scale: new(0.09f, 0.22f, 0.09f),
                            position: new(sx * 0.85f, 0.31f, sz * 0.85f), rotation: new(sz * 45f, 0, sx * -45f), color: Rgb(35, 225, 235), unlit: true);
                    }
                }
                // Бирюзовые прожилки Скалка и всплывающая душа сверху
                new Entity(parent: root, model: "cube", scale: new(0.28f, 0.05f, 0.28f), position: new(0, 0.26f, 0), color: Rgb(25, 205, 215), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.14f, position: new(0, 0.40f, 0), color: Rgb(125, 252, 255), unlit: true);
                return true;
            }
            if (itemKey == "maw_of_void")
            {
                // Пасть Пустоты [Void / Isaac] — чёрно-алое вихревое кольцо Бездны с острыми клыками внутрь
                var ring = new Entity(parent: root, rotation: new(28, 0, 12));
                new Entity(parent: ring, model: "sphere", scale: new(0.46f, 0.12f, 0.46f), color: Rgb(10, 4, 16));
                for (int i = 0; i < 8; i++)
                {
                    float a = i * MathF.PI * 0.25f;
                    float cx = MathF.Cos(a) * 0.32f, cz = MathF.Sin(a) * 0.32f;
                    float deg = -i * 45f;
                    new Entity(parent: ring, model: "sphere", scale: new(0.22f, 0.18f, 0.22f), position: new(cx, 0, cz), color: Rgb(26, 12, 34));
                    new Entity(parent: ring, model: "diamond", scale: new(0.16f, 0.12f, 0.22f), position: new(cx * 0.88f, 0.04f, cz * 0.88f),
                        rotation: new(0, deg, 0), color: Rgb(235, 32, 78), unlit: true);
                    new Entity(parent: ring, model: "diamond", scale: new(0.08f, 0.08f, 0.22f), position: new(cx * 0.62f, 0.02f, cz * 0.62f),
                        rotation: new(0, deg + 90f, 0), color: Rgb(255, 225, 232), unlit: true);
                }
                new Entity(parent: ring, model: "diamond", scale: new(0.24f, 0.18f, 0.09f), position: new(0, 0.06f, 0), color: Rgb(255, 45, 92), unlit: true);
                return true;
            }
            if (itemKey == "seaborne_ichor")
            {
                // Ихор Морских Рождений [Void / Arknights] — биолюминесцентное ядро Морских Рождений с лазурными щупальцами
                new Entity(parent: root, model: "sphere", scale: new(0.42f, 0.46f, 0.42f), position: new(0, -0.04f, 0), color: Rgb(44, 62, 162));
                new Entity(parent: root, model: "sphere", scaleUniform: 0.28f, position: new(0, -0.02f, 0), color: Rgb(95, 205, 255), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.56f, position: new(0, -0.03f, 0), color: Rgb(75, 125, 255, 90), unlit: true);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * MathF.PI * 2f / 5f;
                    float tx = MathF.Cos(a) * 0.26f, tz = MathF.Sin(a) * 0.26f;
                    new Entity(parent: root, model: "diamond", scale: new(0.11f, 0.40f, 0.11f),
                        position: new(tx, 0.16f, tz), rotation: new(tz * 85f, 0, -tx * 85f), color: Rgb(72, 132, 248), unlit: true);
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.09f,
                        position: new(tx * 1.32f, 0.34f, tz * 1.32f), color: Rgb(135, 242, 255), unlit: true);
                }
                return true;
            }

            // ================= 4 ЛУННЫХ ПРЕДМЕТА РАСШИРЕНИЯ =================
            if (itemKey == "armlet_mordiggian")
            {
                // Армлет Мордиггиана [Lunar / Dota 2] — проклятый наруч с пурпурно-багровым оком и тремя когтями
                var tilt = new Entity(parent: root, rotation: new(16, 18, -18));
                new Entity(parent: tilt, model: "cube", scale: new(0.42f, 0.30f, 0.46f), color: Rgb(52, 44, 64));
                foreach (float ty in new[] { -0.13f, 0.13f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.46f, 0.06f, 0.50f), position: new(0, ty, 0), color: Rgb(168, 128, 96));
                // Центральное пылающее око Нечестивой Мощи
                new Entity(parent: tilt, model: "sphere", scale: new(0.24f, 0.16f, 0.24f), position: new(0, 0.16f, 0), color: Rgb(255, 52, 142), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.08f, 0.18f, 0.18f), position: new(0, 0.18f, 0), color: Rgb(255, 205, 235), unlit: true);
                // Три когтя-лезвия спереди наруча
                foreach (float sx in new[] { -0.14f, 0f, 0.14f })
                    new Entity(parent: tilt, model: "diamond", scale: new(0.09f, 0.10f, 0.48f), position: new(sx, 0.12f, 0.32f), color: Rgb(235, 48, 125), unlit: true);
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.64f, color: Rgb(215, 45, 135, 60), unlit: true);
                return true;
            }
            if (itemKey == "elytra_curse")
            {
                // Проклятые Элитры [Lunar / Minecraft] — пара хитиновых лунно-фиолетовых надкрылий со светящейся каймой
                new Entity(parent: root, model: "cube", scale: new(0.34f, 0.14f, 0.16f), position: new(0, 0.32f, 0), color: Rgb(46, 54, 82));
                new Entity(parent: root, model: "sphere", scaleUniform: 0.14f, position: new(0, 0.32f, 0.08f), color: Rgb(115, 210, 255), unlit: true);
                foreach (float sx in new[] { -1f, 1f })
                {
                    new Entity(parent: root, model: "diamond", scale: new(0.34f, 0.78f, 0.10f),
                        position: new(sx * 0.22f, -0.02f, 0), rotation: new(8, 0, sx * -20f), color: Rgb(105, 132, 202));
                    new Entity(parent: root, model: "diamond", scale: new(0.38f, 0.82f, 0.05f),
                        position: new(sx * 0.24f, -0.03f, -0.02f), rotation: new(8, 0, sx * -22f), color: Rgb(125, 215, 255), unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(0.14f, 0.48f, 0.12f),
                        position: new(sx * 0.18f, 0.04f, 0.03f), rotation: new(8, 0, sx * -16f), color: Rgb(62, 76, 135));
                }
                return true;
            }
            if (itemKey == "damocles")
            {
                // Дамоклов Меч [Lunar / Isaac] — лунно-серебряный меч, подвешенный остриём вниз на тонкой нити
                new Entity(parent: root, model: "cube", scale: new(0.024f, 0.24f, 0.024f), position: new(0, 0.50f, 0), color: Rgb(225, 245, 255), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.16f, 0.16f, 0.16f), position: new(0, 0.36f, 0), color: Rgb(195, 225, 255), unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.08f, 0.20f, 0.08f), position: new(0, 0.22f, 0), color: Rgb(48, 68, 108));
                new Entity(parent: root, model: "diamond", scale: new(0.50f, 0.11f, 0.14f), position: new(0, 0.10f, 0), color: Rgb(210, 232, 255), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.13f, position: new(0, 0.10f, 0.05f), color: Rgb(85, 185, 255), unlit: true);
                // Длинное обоюдоострое лезвие остриём строго вниз
                new Entity(parent: root, model: "diamond", scale: new(0.24f, 0.64f, 0.06f), position: new(0, -0.18f, 0), color: Rgb(155, 210, 255), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.16f, 0.68f, 0.09f), position: new(0, -0.20f, 0), color: Color.White, unlit: true);
                return true;
            }
            if (itemKey == "originium_prime")
            {
                // Чистый Ориджиниум [Lunar / Arknights] — сияющий золотисто-янтарный октаэдр с парящими осколками породы
                var tilt = new Entity(parent: root, rotation: new(10, 24, -14));
                new Entity(parent: tilt, model: "diamond", scale: new(0.46f, 0.86f, 0.46f), color: Rgb(255, 185, 42), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.40f, 0.80f, 0.40f), rotation: new(0, 45, 0), color: Rgb(255, 215, 95, 210), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.22f, 0.52f, 0.22f), color: Color.White, unlit: true);
                // Тёмные графитовые сколы оболочки и орбитальные осколки Ориджиниума
                new Entity(parent: tilt, model: "diamond", scale: new(0.18f, 0.22f, 0.18f), position: new(-0.12f, -0.28f, 0.08f), rotation: new(14, 0, 22), color: Rgb(44, 38, 36));
                foreach (var (sx, sy, sz) in new[] { (-0.32f, 0.14f, 0.10f), (0.32f, -0.10f, -0.12f), (0.14f, 0.30f, -0.24f), (-0.16f, -0.22f, -0.24f) })
                    new Entity(parent: root, model: "diamond", scale: new(0.10f, 0.22f, 0.10f), position: new(sx, sy, sz), rotation: new(18, 0, -24), color: Rgb(255, 205, 68), unlit: true);
                return true;
            }

            return false;
        }
    }
}
