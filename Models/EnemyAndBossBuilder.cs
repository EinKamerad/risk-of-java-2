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
                Color clawCol = Rgb(255, 235, 195);

                var lLeg = new Entity(parent: root, position: new(-0.26f, 0.75f, 0));
                var rLeg = new Entity(parent: root, position: new(0.26f, 0.75f, 0));
                foreach (var (side, leg) in new[] { (-1, lLeg), (1, rLeg) })
                {
                    // Бедро
                    new Entity(parent: leg, model: "cube", scale: new(0.24f, 0.38f, 0.28f), position: new(0, -0.15f, 0), color: skinCol);
                    // Бронепластина на бедре
                    new Entity(parent: leg, model: "diamond", scale: new(0.28f, 0.24f, 0.32f), position: new(side * 0.10f, -0.15f, 0.08f), color: armorCol);
                    // Коленный сустав
                    new Entity(parent: leg, model: "sphere", scale: new(0.22f, 0.20f, 0.22f), position: new(0, -0.35f, 0.04f), color: skinCol);
                    // Голень
                    new Entity(parent: leg, model: "cube", scale: new(0.20f, 0.36f, 0.24f), position: new(0, -0.52f, 0.02f), color: skinCol);
                    // Лапа-стопа
                    new Entity(parent: leg, model: "diamond", scale: new(0.30f, 0.20f, 0.46f), position: new(0, -0.70f, 0.14f), color: spineCol);
                    // Когти на лапе
                    foreach (float cx in new[] { -0.08f, 0f, 0.08f })
                        new Entity(parent: leg, model: "diamond", scale: new(0.06f, 0.08f, 0.16f), position: new(cx, -0.76f, 0.34f), rotation: new(-25, 0, 0), color: clawCol);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 1.15f, 0), rotation: new(18, 0, 0));
                // Основной корпус
                new Entity(parent: torso, model: "cube", scale: new(0.72f, 0.98f, 0.56f), color: skinCol);
                // Брюшные пластины
                new Entity(parent: torso, model: "cube", scale: new(0.54f, 0.86f, 0.18f), position: new(0, 0, 0.23f), color: bellyCol);
                // Рёбра на брюшке
                foreach (float ry in new[] { -0.25f, -0.08f, 0.08f, 0.25f })
                    new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.04f, 0.20f), position: new(0, ry, 0.24f), color: Rgb(210, 120, 58));
                // Нагрудная бронепластина
                new Entity(parent: torso, model: "diamond", scale: new(1.05f, 0.38f, 0.58f), position: new(0, 0.34f, 0), color: armorCol);
                // Чешуйки на спине
                for (int i = 0; i < 5; i++)
                    new Entity(parent: torso, model: "diamond", scale: new(0.15f, 0.42f, 0.28f), position: new(0, 0.38f - i * 0.22f, -0.32f), rotation: new(-35, 0, 0), color: spineCol);
                // Дополнительные мелкие шипы по бокам
                foreach (int side in new[] { -1, 1 })
                    foreach (float sy in new[] { 0.10f, -0.10f })
                        new Entity(parent: torso, model: "diamond", scale: new(0.10f, 0.22f, 0.14f), position: new(side * 0.38f, sy, -0.20f), rotation: new(-20, side * 15, side * 25), color: spineCol);

                // Хвост — многосегментный
                var tail = new Entity(parent: torso, position: new(0, -0.4f, -0.25f));
                new Entity(parent: tail, model: "diamond", scale: new(0.32f, 0.28f, 0.65f), position: new(0, -0.05f, -0.28f), rotation: new(-20, 0, 0), color: skinCol);
                new Entity(parent: tail, model: "diamond", scale: new(0.26f, 0.24f, 0.55f), position: new(0, -0.12f, -0.72f), rotation: new(-25, 0, 0), color: skinCol);
                new Entity(parent: tail, model: "diamond", scale: new(0.20f, 0.20f, 0.48f), position: new(0, -0.22f, -1.05f), rotation: new(-15, 0, 0), color: skinCol);
                // Кончик хвоста — шип
                new Entity(parent: tail, model: "diamond", scale: new(0.14f, 0.28f, 0.35f), position: new(0, -0.32f, -1.38f), rotation: new(-10, 0, 0), color: spineCol);
                // Гребень на хвосте
                foreach (float tz in new[] { -0.40f, -0.72f, -1.0f })
                    new Entity(parent: tail, model: "diamond", scale: new(0.04f, 0.18f, 0.12f), position: new(0, 0.05f, tz), rotation: new(-20, 0, 0), color: spineCol);
                parts.Tail = tail;
                parts.Torso = torso;

                // Голова — рептилоидная с деталями
                var head = new Entity(parent: torso, position: new(0, 0.64f, 0.18f), rotation: new(-18, 0, 0));
                // Череп
                new Entity(parent: head, model: "diamond", scale: new(0.72f, 0.54f, 1.02f), color: skinCol);
                // Верхняя часть черепа — бронь
                new Entity(parent: head, model: "diamond", scale: new(0.68f, 0.28f, 0.85f), position: new(0, 0.14f, -0.05f), color: armorCol);
                // Челюсть
                new Entity(parent: head, model: "cube", scale: new(0.48f, 0.16f, 0.72f), position: new(0, -0.20f, 0.16f), color: bellyCol);
                // Зубы
                foreach (float tx in new[] { -0.16f, 0.0f, 0.16f })
                    new Entity(parent: head, model: "diamond", scale: new(0.07f, 0.14f, 0.07f), position: new(tx, -0.12f, 0.42f), color: spineCol);
                // Нижние клыки
                foreach (float tx in new[] { -0.12f, 0.12f })
                    new Entity(parent: head, model: "diamond", scale: new(0.05f, 0.12f, 0.05f), position: new(tx, -0.22f, 0.38f), color: clawCol);
                // Рога — массивные
                new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.72f, 0.20f), position: new(-0.26f, 0.38f, -0.22f), rotation: new(-30, 0, -20), color: spineCol);
                new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.72f, 0.20f), position: new(0.26f, 0.38f, -0.22f), rotation: new(-30, 0, 20), color: spineCol);
                // Рожки поменьше
                new Entity(parent: head, model: "diamond", scale: new(0.12f, 0.35f, 0.12f), position: new(-0.14f, 0.32f, -0.10f), rotation: new(-25, 0, -10), color: spineCol);
                new Entity(parent: head, model: "diamond", scale: new(0.12f, 0.35f, 0.12f), position: new(0.14f, 0.32f, -0.10f), rotation: new(-25, 0, 10), color: spineCol);
                // Глаза — с радужкой
                foreach (float ex in new[] { -0.21f, 0.21f })
                {
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.18f, position: new(ex, 0.09f, 0.27f), color: Color.Yellow, unlit: true);
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.08f, position: new(ex, 0.09f, 0.32f), color: Rgb(20, 15, 10));
                }
                // Ноздри
                foreach (float nx in new[] { -0.08f, 0.08f })
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.05f, position: new(nx, -0.06f, 0.48f), color: Rgb(35, 25, 22));

                // Руки с когтями
                var lArm = new Entity(parent: torso, position: new(-0.46f, 0.22f, 0.1f));
                var rArm = new Entity(parent: torso, position: new(0.46f, 0.22f, 0.1f));
                foreach (var (side, arm) in new[] { (-1, lArm), (1, rArm) })
                {
                    // Плечо
                    new Entity(parent: arm, model: "sphere", scale: new(0.22f, 0.20f, 0.20f), position: new(0, 0, 0), color: skinCol);
                    // Предплечье
                    new Entity(parent: arm, model: "cube", scale: new(0.19f, 0.58f, 0.19f), position: new(0, -0.2f, 0.2f), rotation: new(-45, 0, 0), color: skinCol);
                    // Кисть
                    new Entity(parent: arm, model: "diamond", scale: new(0.22f, 0.28f, 0.18f), position: new(0, -0.44f, 0.42f), rotation: new(-65, 0, 0), color: spineCol);
                    // Когти на руках
                    foreach (float cx in new[] { -0.06f, 0f, 0.06f })
                        new Entity(parent: arm, model: "diamond", scale: new(0.04f, 0.06f, 0.14f), position: new(cx, -0.50f, 0.52f), rotation: new(-75, 0, 0), color: clawCol);
                }
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
            }
            else if (enemyType == "wisp")
            {
                var core = new Entity(parent: root, position: new(0, 2.3f, 0));
                // Ядро пламени — многослойное
                new Entity(parent: core, model: "sphere", scaleUniform: 0.90f, color: Rgb(255, 135, 20), unlit: true);
                new Entity(parent: core, model: "sphere", scaleUniform: 0.65f, position: new(0, 0.05f, 0), color: Rgb(255, 200, 80), unlit: true);
                // Языки пламени
                new Entity(parent: core, model: "diamond", scale: new(0.60f, 1.55f, 0.60f), position: new(0, 0.40f, 0), color: Color.Yellow, unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(0.38f, 1.85f, 0.38f), position: new(0, 0.58f, 0), color: Color.White, unlit: true);
                // Боковые всполохи
                foreach (float sx in new[] { -0.32f, 0.28f })
                    new Entity(parent: core, model: "diamond", scale: new(0.22f, 0.65f, 0.22f), position: new(sx, 0.25f, 0), rotation: new(0, 0, sx * 25f), color: Rgb(255, 180, 45), unlit: true);
                // Искры — мелкие частицы
                foreach (var (sx, sy, sz) in new[] { (-0.45f, 0.62f, 0.15f), (0.38f, 0.78f, -0.12f), (0.12f, 0.92f, 0.28f), (-0.22f, 0.85f, -0.20f) })
                    new Entity(parent: core, model: "sphere", scaleUniform: 0.08f, position: new(sx, sy, sz), color: Color.Yellow, unlit: true);

                // Маска — более детальная
                var mask = new Entity(parent: core, position: new(0, 0, 0.40f));
                // Основа маски
                new Entity(parent: mask, model: "cube", scale: new(0.80f, 0.90f, 0.22f), color: Rgb(55, 60, 72));
                // Рельеф на маске — вертикальные борозды
                foreach (float fx in new[] { -0.25f, 0f, 0.25f })
                    new Entity(parent: mask, model: "cube", scale: new(0.06f, 0.75f, 0.03f), position: new(fx, 0, 0.12f), color: Rgb(42, 46, 56));
                // Нижняя челюсть
                new Entity(parent: mask, model: "diamond", scale: new(0.65f, 0.35f, 0.26f), position: new(0, -0.38f, 0), color: Rgb(42, 46, 56));
                // Зубы на челюсти
                foreach (float tx in new[] { -0.18f, -0.06f, 0.06f, 0.18f })
                    new Entity(parent: mask, model: "diamond", scale: new(0.06f, 0.12f, 0.08f), position: new(tx, -0.28f, 0.10f), color: Rgb(75, 80, 92));
                // Глаза — пылающие
                new Entity(parent: mask, model: "cube", scale: new(0.26f, 0.13f, 0.28f), position: new(-0.18f, 0.12f, 0.02f), color: Color.Yellow, unlit: true);
                new Entity(parent: mask, model: "cube", scale: new(0.26f, 0.13f, 0.28f), position: new(0.18f, 0.12f, 0.02f), color: Color.Yellow, unlit: true);
                // Зрачки
                new Entity(parent: mask, model: "sphere", scaleUniform: 0.06f, position: new(-0.18f, 0.12f, 0.14f), color: Color.White, unlit: true);
                new Entity(parent: mask, model: "sphere", scaleUniform: 0.06f, position: new(0.18f, 0.12f, 0.14f), color: Color.White, unlit: true);
                // Рога маски
                new Entity(parent: mask, model: "diamond", scale: new(0.26f, 0.88f, 0.26f), position: new(-0.45f, 0.48f, -0.1f), rotation: new(0, 0, -25f), color: Rgb(95, 100, 115));
                new Entity(parent: mask, model: "diamond", scale: new(0.26f, 0.88f, 0.26f), position: new(0.45f, 0.48f, -0.1f), rotation: new(0, 0, 25f), color: Rgb(95, 100, 115));
                // Кончики рогов
                new Entity(parent: mask, model: "diamond", scale: new(0.14f, 0.28f, 0.14f), position: new(-0.58f, 0.82f, -0.12f), rotation: new(0, 0, -30f), color: Rgb(115, 120, 135));
                new Entity(parent: mask, model: "diamond", scale: new(0.14f, 0.28f, 0.14f), position: new(0.58f, 0.82f, -0.12f), rotation: new(0, 0, 30f), color: Rgb(115, 120, 135));

                parts.Core = core;
                parts.Ring1 = new Entity(parent: core, model: "diamond", scale: new(1.85f, 0.18f, 1.85f), color: Rgb(70, 75, 88));
                parts.Ring2 = new Entity(parent: core, model: "diamond", scale: new(0.18f, 1.85f, 1.85f), color: Rgb(115, 120, 135));
            }
            else if (enemyType == "beetle")
            {
                Color shellCol = Rgb(95, 62, 42);
                Color plateCol = Rgb(135, 88, 55);
                Color hornCol = Rgb(225, 175, 85);
                Color legCol = Rgb(75, 48, 32);

                var torso = new Entity(parent: root, position: new(0, 0.65f, 0));
                // Основной панцирь
                new Entity(parent: torso, model: "sphere", scale: new(1.18f, 0.74f, 1.48f), color: shellCol);
                // Надкрылья с разделением
                new Entity(parent: torso, model: "diamond", scale: new(0.92f, 0.38f, 1.22f), position: new(0, 0.28f, 0), color: plateCol);
                // Шов надкрылий
                new Entity(parent: torso, model: "cube", scale: new(0.06f, 0.42f, 1.15f), position: new(0, 0.29f, 0), color: Rgb(55, 35, 22));
                // Пятна на панцире
                foreach (var (px, pz) in new[] { (-0.32f, -0.25f), (0.28f, -0.35f), (-0.15f, 0.30f) })
                    new Entity(parent: torso, model: "sphere", scale: new(0.22f, 0.12f, 0.22f), position: new(px, 0.35f, pz), color: Rgb(115, 78, 48));
                // Головная пластина
                new Entity(parent: torso, model: "diamond", scale: new(0.65f, 0.30f, 0.52f), position: new(0, 0.08f, 0.68f), color: shellCol);
                // Рог — главный
                new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.82f, 0.38f), position: new(0, 0.24f, 0.78f), rotation: new(55, 0, 0), color: hornCol);
                // Кончик рога
                new Entity(parent: torso, model: "diamond", scale: new(0.12f, 0.25f, 0.12f), position: new(0, 0.72f, 1.15f), rotation: new(55, 0, 0), color: Rgb(245, 215, 125));
                // Боковые мандибулы
                foreach (float mx in new[] { -0.24f, 0.24f })
                {
                    new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.14f, 0.45f), position: new(mx, -0.05f, 0.75f), rotation: new(0, mx * -35f, 0), color: hornCol);
                    // Кончик мандибулы
                    new Entity(parent: torso, model: "diamond", scale: new(0.08f, 0.08f, 0.12f), position: new(mx * 1.4f, -0.05f, 1.02f), rotation: new(0, mx * -35f, 0), color: Rgb(245, 215, 125));
                }
                // Глаза — фасеточные
                foreach (float ex in new[] { -0.27f, 0.27f })
                {
                    new Entity(parent: torso, model: "sphere", scaleUniform: 0.18f, position: new(ex, 0.13f, 0.64f), color: Color.Orange, unlit: true);
                    new Entity(parent: torso, model: "sphere", scaleUniform: 0.08f, position: new(ex, 0.15f, 0.70f), color: Color.Yellow, unlit: true);
                }
                // Усики-антенны
                foreach (float ax in new[] { -0.18f, 0.18f })
                {
                    new Entity(parent: torso, model: "cube", scale: new(0.04f, 0.04f, 0.38f), position: new(ax, 0.18f, 0.88f), rotation: new(-15, ax * 20f, 0), color: Rgb(65, 42, 28));
                    new Entity(parent: torso, model: "sphere", scaleUniform: 0.05f, position: new(ax * 1.3f, 0.20f, 1.22f), color: hornCol);
                }

                // Ноги — членистые с суставами
                var lLeg = new Entity(parent: root, position: new(-0.55f, 0.45f, 0));
                var rLeg = new Entity(parent: root, position: new(0.55f, 0.45f, 0));
                foreach (var (side, leg) in new[] { (-1, lLeg), (1, rLeg) })
                {
                    foreach (float zOff in new[] { -0.36f, 0.0f, 0.36f })
                    {
                        // Бедро
                        new Entity(parent: leg, model: "diamond", scale: new(0.22f, 0.60f, 0.22f), position: new(side * 0.12f, -0.2f, zOff), rotation: new(0, 0, side * 28f), color: shellCol);
                        // Коленный сустав
                        new Entity(parent: leg, model: "sphere", scaleUniform: 0.10f, position: new(side * 0.28f, -0.42f, zOff), color: legCol);
                        // Голень
                        new Entity(parent: leg, model: "cube", scale: new(0.08f, 0.25f, 0.08f), position: new(side * 0.32f, -0.58f, zOff), color: legCol);
                        // Коготь
                        new Entity(parent: leg, model: "diamond", scale: new(0.08f, 0.08f, 0.14f), position: new(side * 0.32f, -0.70f, zOff + 0.06f), color: hornCol);
                    }
                }
                parts.Torso = torso;
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;
            }
            else if (enemyType == "jellyfish")
            {
                var core = new Entity(parent: root, position: new(0, 2.2f, 0));
                // Купол — многослойный
                new Entity(parent: core, model: "sphere", scale: new(1.40f, 1.08f, 1.40f), color: Rgb(75, 225, 255, 185), unlit: true);
                new Entity(parent: core, model: "sphere", scale: new(1.25f, 0.92f, 1.25f), position: new(0, 0.02f, 0), color: Rgb(100, 235, 255, 120), unlit: true);
                // Юбка купола
                new Entity(parent: core, model: "diamond", scale: new(1.55f, 0.28f, 1.55f), position: new(0, -0.22f, 0), color: Rgb(135, 245, 255, 160), unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(1.65f, 0.14f, 1.65f), position: new(0, -0.32f, 0), color: Rgb(165, 250, 255, 100), unlit: true);
                // Внутренний орган — ядро
                new Entity(parent: core, model: "sphere", scaleUniform: 0.68f, position: new(0, -0.05f, 0), color: Rgb(245, 110, 255), unlit: true);
                // Каналы внутри купола
                foreach (float cAng in new[] { 0f, 72f, 144f, 216f, 288f })
                {
                    float cx = MathF.Cos(cAng * MathF.PI / 180f) * 0.45f;
                    float cz = MathF.Sin(cAng * MathF.PI / 180f) * 0.45f;
                    new Entity(parent: core, model: "diamond", scale: new(0.06f, 0.55f, 0.06f), position: new(cx, 0.15f, cz), color: Rgb(200, 130, 255, 140), unlit: true);
                }
                // Светящиеся точки на куполе
                foreach (var (sx, sy, sz) in new[] { (0.48f, 0.35f, 0.22f), (-0.42f, 0.28f, -0.30f), (0.15f, 0.50f, 0.42f), (-0.35f, 0.42f, 0.35f) })
                    new Entity(parent: core, model: "sphere", scaleUniform: 0.08f, position: new(sx, sy, sz), color: Color.White, unlit: true);
                
                // Щупальца — разной длины и толщины
                var tentacles = new Entity(parent: core, position: new(0, -0.35f, 0));
                for (int i = 0; i < 8; i++)
                {
                    float ang = (i / 8.0f) * MathF.PI * 2f;
                    float tLen = i % 2 == 0 ? 1.35f : 0.95f;
                    float tRad = 0.38f;
                    // Основное щупальце
                    new Entity(parent: tentacles, model: "diamond", scale: new(0.13f, tLen, 0.13f),
                        position: new(MathF.Cos(ang) * tRad, -tLen * 0.4f, MathF.Sin(ang) * tRad),
                        color: i % 2 == 0 ? Color.SkyBlue : Rgb(235, 120, 255), unlit: true);
                    // Утолщение на кончике
                    new Entity(parent: tentacles, model: "sphere", scaleUniform: 0.08f,
                        position: new(MathF.Cos(ang) * tRad, -tLen * 0.85f, MathF.Sin(ang) * tRad),
                        color: i % 3 == 0 ? Color.White : Rgb(200, 150, 255), unlit: true);
                }
                // Оральные руки — 4 штуки, длинные
                for (int i = 0; i < 4; i++)
                {
                    float ang = (i / 4.0f) * MathF.PI * 2f + 0.4f;
                    new Entity(parent: tentacles, model: "diamond", scale: new(0.08f, 1.85f, 0.08f),
                        position: new(MathF.Cos(ang) * 0.18f, -0.85f, MathF.Sin(ang) * 0.18f),
                        color: Rgb(255, 180, 255, 140), unlit: true);
                }
                parts.Core = core;
                parts.Tentacles = tentacles;
            }
            else if (enemyType == "golem")
            {
                Color stCol = Rgb(78, 86, 98);
                Color stDark = Rgb(52, 58, 68);
                Color runeCol = Rgb(255, 70, 55);
                Color mossCol = Rgb(62, 95, 55);

                // Ноги — массивные с деталями
                var lLeg = new Entity(parent: root, position: new(-0.46f, 1.0f, 0));
                var rLeg = new Entity(parent: root, position: new(0.46f, 1.0f, 0));
                foreach (var (side, leg) in new[] { (-1, lLeg), (1, rLeg) })
                {
                    // Бедро
                    new Entity(parent: leg, model: "cube", scale: new(0.48f, 0.50f, 0.52f), position: new(0, -0.25f, 0), color: stDark);
                    // Руна на бедре
                    new Entity(parent: leg, model: "cube", scale: new(0.08f, 0.35f, 0.01f), position: new(side * 0.25f, -0.25f, 0.26f), color: runeCol, unlit: true);
                    // Колено
                    new Entity(parent: leg, model: "sphere", scale: new(0.42f, 0.32f, 0.42f), position: new(0, -0.50f, 0.05f), color: stCol);
                    // Голень
                    new Entity(parent: leg, model: "cube", scale: new(0.44f, 0.48f, 0.48f), position: new(0, -0.74f, 0), color: stDark);
                    // Стопа
                    new Entity(parent: leg, model: "cube", scale: new(0.56f, 0.28f, 0.64f), position: new(0, -0.88f, 0.08f), color: stCol);
                    // Мох на стопе
                    new Entity(parent: leg, model: "sphere", scale: new(0.28f, 0.12f, 0.28f), position: new(side * 0.08f, -0.75f, 0.22f), color: mossCol);
                    // Трещина на ноге
                    new Entity(parent: leg, model: "cube", scale: new(0.05f, 0.40f, 0.02f), position: new(side * -0.18f, -0.35f, 0.27f), rotation: new(0, 0, side * 5f), color: Rgb(42, 48, 58));
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                // Торс
                var torso = new Entity(parent: root, position: new(0, 1.85f, 0));
                new Entity(parent: torso, model: "cube", scale: new(1.48f, 1.38f, 0.95f), color: stCol);
                // Плечевые камни
                new Entity(parent: torso, model: "diamond", scale: new(1.65f, 0.45f, 1.05f), position: new(0, 0.45f, 0), color: stDark);
                // Мох на плечах
                foreach (float sx in new[] { -0.65f, 0.62f })
                    new Entity(parent: torso, model: "sphere", scale: new(0.22f, 0.10f, 0.18f), position: new(sx, 0.56f, 0.15f), color: mossCol);
                // Руновая сердцевина
                new Entity(parent: torso, model: "diamond", scale: new(0.32f, 0.78f, 1.02f), color: runeCol, unlit: true);
                // Дополнительные руны
                foreach (float ry in new[] { -0.32f, 0.28f })
                    new Entity(parent: torso, model: "cube", scale: new(1.05f, 0.08f, 0.05f), position: new(0, ry, 0.48f), color: runeCol, unlit: true);
                // Каменные сколы
                foreach (var (cx, cy, cz) in new[] { (0.62f, -0.22f, 0.35f), (-0.55f, 0.15f, 0.40f) })
                    new Entity(parent: torso, model: "diamond", scale: new(0.22f, 0.18f, 0.15f), position: new(cx, cy, cz), rotation: new(15, 25, 10), color: stDark);

                // Голова
                var head = new Entity(parent: torso, model: "cube", scale: new(0.78f, 0.62f, 0.74f), position: new(0, 0.95f, 0), color: stDark);
                // Визор
                new Entity(parent: head, model: "cube", scale: new(0.68f, 0.16f, 0.78f), position: new(0, 0.05f, 0), color: runeCol, unlit: true);
                // Глаз — центральный сенсор
                new Entity(parent: head, model: "sphere", scaleUniform: 0.36f, position: new(0, 0.05f, 0.46f), color: Color.Red, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.18f, position: new(0, 0.05f, 0.52f), color: Color.Yellow, unlit: true);
                // Гребень на голове
                new Entity(parent: head, model: "diamond", scale: new(0.85f, 0.42f, 0.28f), position: new(0, 0.35f, -0.10f), color: stCol);

                // Руки — с суставами и кулаками
                var lArm = new Entity(parent: torso, position: new(-1.02f, 0.35f, 0));
                var rArm = new Entity(parent: torso, position: new(1.02f, 0.35f, 0));
                foreach (var (side, arm) in new[] { (-1, lArm), (1, rArm) })
                {
                    // Плечевой сустав
                    new Entity(parent: arm, model: "diamond", scale: new(0.62f, 0.55f, 0.62f), position: new(0, 0.12f, 0), color: stDark);
                    // Верхняя рука
                    new Entity(parent: arm, model: "cube", scale: new(0.44f, 0.65f, 0.44f), position: new(0, -0.30f, 0), color: stCol);
                    // Руна на верхней руке
                    new Entity(parent: arm, model: "cube", scale: new(0.06f, 0.45f, 0.01f), position: new(0, -0.30f, 0.23f), color: runeCol, unlit: true);
                    // Локтевой сустав
                    new Entity(parent: arm, model: "sphere", scale: new(0.38f, 0.32f, 0.38f), position: new(0, -0.60f, 0), color: stDark);
                    // Предплечье
                    new Entity(parent: arm, model: "cube", scale: new(0.40f, 0.58f, 0.40f), position: new(0, -0.88f, 0), color: stCol);
                    // Кулак
                    new Entity(parent: arm, model: "cube", scale: new(0.56f, 0.48f, 0.56f), position: new(0, -1.15f, 0), color: stDark);
                    // Руна на кулаке (выступает наружу без Z-fighting)
                    new Entity(parent: arm, model: "cube", scale: new(0.58f, 0.18f, 0.58f), position: new(0, -1.15f, 0), color: runeCol, unlit: true);
                    // Пальцы кулака
                    foreach (float fz in new[] { -0.15f, 0f, 0.15f })
                        new Entity(parent: arm, model: "cube", scale: new(0.52f, 0.12f, 0.14f), position: new(0, -1.35f, fz), color: stCol);
                }
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
            }
            else if (enemyType == "imp")
            {
                Color impCol = Rgb(45, 22, 48);
                Color crimson = Rgb(245, 35, 70);
                Color impSkin = Rgb(65, 32, 68);

                // Ноги
                var lLeg = new Entity(parent: root, position: new(-0.24f, 0.78f, 0));
                var rLeg = new Entity(parent: root, position: new(0.24f, 0.78f, 0));
                foreach (var (side, leg) in new[] { (-1, lLeg), (1, rLeg) })
                {
                    // Бедро
                    new Entity(parent: leg, model: "diamond", scale: new(0.25f, 0.42f, 0.25f), position: new(0, -0.18f, 0), color: impCol);
                    // Колено
                    new Entity(parent: leg, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.38f, 0.04f), color: impSkin);
                    // Голень
                    new Entity(parent: leg, model: "diamond", scale: new(0.20f, 0.42f, 0.20f), position: new(0, -0.56f, 0), color: impCol);
                    // Стопа с когтями
                    new Entity(parent: leg, model: "diamond", scale: new(0.18f, 0.18f, 0.38f), position: new(0, -0.75f, 0.12f), color: crimson, unlit: true);
                    // Когти
                    foreach (float cx in new[] { -0.05f, 0.05f })
                        new Entity(parent: leg, model: "diamond", scale: new(0.04f, 0.06f, 0.12f), position: new(cx, -0.80f, 0.30f), rotation: new(-15, 0, 0), color: Rgb(255, 75, 100), unlit: true);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                // Торс
                var torso = new Entity(parent: root, position: new(0, 1.28f, 0));
                new Entity(parent: torso, model: "diamond", scale: new(0.86f, 1.18f, 0.60f), color: impCol);
                // Бронепластина на груди
                new Entity(parent: torso, model: "diamond", scale: new(0.55f, 0.42f, 0.42f), position: new(0, 0.15f, 0.12f), color: impSkin);
                // Руновая сердцевина
                new Entity(parent: torso, model: "diamond", scale: new(0.22f, 0.72f, 0.28f), position: new(0, 0, 0.24f), color: crimson, unlit: true);
                new Entity(parent: torso, model: "sphere", scaleUniform: 0.18f, position: new(0, 0, 0.30f), color: Color.White, unlit: true);
                // Шипы — рога на плечах
                foreach (int side in new[] { -1, 1 })
                {
                    // Рога
                    new Entity(parent: torso, model: "diamond", scale: new(0.18f, 0.68f, 0.18f), position: new(side * 0.30f, 0.68f, 0), rotation: new(0, 0, side * -24f), color: crimson, unlit: true);
                    new Entity(parent: torso, model: "diamond", scale: new(0.10f, 0.28f, 0.10f), position: new(side * 0.40f, 1.02f, 0), rotation: new(0, 0, side * -28f), color: Rgb(255, 85, 110), unlit: true);
                    // Спинные шипы
                    new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.42f, 0.24f), position: new(side * 0.24f, 0.25f, -0.24f), rotation: new(-30, 0, 0), color: crimson, unlit: true);
                }
                // Голова
                var head = new Entity(parent: torso, position: new(0, 0.62f, 0.08f));
                new Entity(parent: head, model: "diamond", scale: new(0.48f, 0.52f, 0.48f), color: impCol);
                // Глаза — три штуки
                new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(-0.12f, 0.05f, 0.22f), color: crimson, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.14f, position: new(0, 0.12f, 0.24f), color: crimson, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(0.12f, 0.05f, 0.22f), color: crimson, unlit: true);
                // Маленькие рожки
                foreach (float hx in new[] { -0.15f, 0.15f })
                    new Entity(parent: head, model: "diamond", scale: new(0.08f, 0.28f, 0.08f), position: new(hx, 0.28f, -0.05f), rotation: new(0, 0, hx * -60f), color: crimson, unlit: true);

                // Руки — с длинными когтями
                var lArm = new Entity(parent: torso, position: new(-0.54f, 0.22f, 0));
                var rArm = new Entity(parent: torso, position: new(0.54f, 0.22f, 0));
                foreach (var (side, arm) in new[] { (-1, lArm), (1, rArm) })
                {
                    // Плечо
                    new Entity(parent: arm, model: "sphere", scale: new(0.22f, 0.18f, 0.20f), color: impSkin);
                    // Предплечье
                    new Entity(parent: arm, model: "diamond", scale: new(0.24f, 0.78f, 0.24f), position: new(0, -0.25f, 0.22f), rotation: new(-50, 0, 0), color: impCol);
                    // Когти — 2 на каждой руке
                    foreach (float cx in new[] { -0.06f, 0.06f })
                        new Entity(parent: arm, model: "diamond", scale: new(0.10f, 0.48f, 0.14f), position: new(cx, -0.54f, 0.44f), rotation: new(-70, 0, 0), color: crimson, unlit: true);
                    // Третий коготь — большой
                    new Entity(parent: arm, model: "diamond", scale: new(0.08f, 0.55f, 0.12f), position: new(0, -0.58f, 0.48f), rotation: new(-75, 0, 0), color: Rgb(255, 75, 100), unlit: true);
                }
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
            }
            else if (enemyType == "greater_wisp")
            {
                var core = new Entity(parent: root, position: new(0, 2.7f, 0));
                // Многослойное ядро пламени
                new Entity(parent: core, model: "sphere", scaleUniform: 1.28f, color: Rgb(65, 245, 110), unlit: true);
                new Entity(parent: core, model: "sphere", scaleUniform: 0.95f, position: new(0, 0.08f, 0), color: Rgb(145, 255, 165), unlit: true);
                // Столб пламени
                new Entity(parent: core, model: "diamond", scale: new(0.90f, 2.25f, 0.90f), position: new(0, 0.52f, 0), color: Rgb(175, 255, 120), unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(0.55f, 2.65f, 0.55f), position: new(0, 0.65f, 0), color: Color.White, unlit: true);
                // Боковые языки пламени
                foreach (float sx in new[] { -0.55f, 0.48f })
                    new Entity(parent: core, model: "diamond", scale: new(0.35f, 1.05f, 0.35f), position: new(sx, 0.35f, 0), rotation: new(0, 0, sx * 18f), color: Rgb(120, 255, 95), unlit: true);
                // Искры
                foreach (var (sx, sy, sz) in new[] { (-0.65f, 0.95f, 0.25f), (0.55f, 1.15f, -0.18f), (0.18f, 1.35f, 0.42f), (-0.35f, 1.22f, -0.32f) })
                    new Entity(parent: core, model: "sphere", scaleUniform: 0.12f, position: new(sx, sy, sz), color: Color.Lime, unlit: true);

                // Маска — элитная
                var mask = new Entity(parent: core, position: new(0, 0, 0.54f));
                new Entity(parent: mask, model: "diamond", scale: new(1.42f, 1.65f, 0.36f), color: Rgb(48, 54, 64));
                // Рельеф маски
                foreach (float fx in new[] { -0.35f, 0f, 0.35f })
                    new Entity(parent: mask, model: "cube", scale: new(0.08f, 1.35f, 0.04f), position: new(fx, 0, 0.18f), color: Rgb(38, 42, 52));
                // Глаза
                new Entity(parent: mask, model: "diamond", scale: new(0.38f, 0.24f, 0.40f), position: new(-0.28f, 0.16f, 0.05f), color: Color.Lime, unlit: true);
                new Entity(parent: mask, model: "diamond", scale: new(0.38f, 0.24f, 0.40f), position: new(0.28f, 0.16f, 0.05f), color: Color.Lime, unlit: true);
                // Зрачки
                new Entity(parent: mask, model: "sphere", scaleUniform: 0.10f, position: new(-0.28f, 0.16f, 0.22f), color: Color.White, unlit: true);
                new Entity(parent: mask, model: "sphere", scaleUniform: 0.10f, position: new(0.28f, 0.16f, 0.22f), color: Color.White, unlit: true);
                // Рога маски
                foreach (int side in new[] { -1, 1 })
                {
                    new Entity(parent: mask, model: "diamond", scale: new(0.30f, 0.95f, 0.28f), position: new(side * 0.58f, 0.58f, -0.05f), rotation: new(0, 0, side * -22f), color: Rgb(78, 86, 100));
                    new Entity(parent: mask, model: "diamond", scale: new(0.18f, 0.38f, 0.18f), position: new(side * 0.72f, 0.92f, -0.08f), rotation: new(0, 0, side * -28f), color: Rgb(88, 96, 110));
                }
                // Символ на лбу маски
                new Entity(parent: mask, model: "diamond", scale: new(0.28f, 0.28f, 0.08f), position: new(0, 0.55f, 0.18f), color: Color.Lime, unlit: true);

                parts.Core = core;
                parts.Ring1 = new Entity(parent: core, model: "diamond", scale: new(2.6f, 0.24f, 2.6f), color: Rgb(65, 72, 85));
                // Парящие плечевые орбы с деталями
                foreach (float ox in new[] { -1.38f, 1.38f })
                {
                    var orb = new Entity(parent: core, model: "sphere", scaleUniform: 0.52f, position: new(ox, -0.1f, 0.35f), color: Color.Lime, unlit: true);
                    new Entity(parent: orb, model: "diamond", scale: new(1.45f, 0.45f, 1.45f), position: new(0, -0.35f, 0), color: Rgb(55, 62, 74));
                    // Мини-руны на плечевых камнях
                    new Entity(parent: orb, model: "cube", scale: new(0.08f, 0.65f, 0.08f), position: new(0, -0.35f, 0), color: Rgb(85, 255, 125), unlit: true);
                }
            }
            else if (enemyType == "bison")
            {
                Color furDark = Rgb(62, 54, 68);
                Color furMain = Rgb(92, 82, 102);
                Color hornIce = Rgb(135, 225, 255);
                Color snowFrost = Rgb(225, 240, 255);

                var lLeg = new Entity(parent: root, position: new(-0.38f, 0.72f, 0));
                var rLeg = new Entity(parent: root, position: new(0.38f, 0.72f, 0));
                foreach (var (side, leg) in new[] { (-1, lLeg), (1, rLeg) })
                {
                    foreach (float zOff in new[] { -0.45f, 0.45f })
                    {
                        new Entity(parent: leg, model: "cube", scale: new(0.28f, 0.65f, 0.28f), position: new(0, -0.32f, zOff), color: furDark);
                        new Entity(parent: leg, model: "diamond", scale: new(0.34f, 0.28f, 0.34f), position: new(0, -0.22f, zOff), color: furMain);
                        new Entity(parent: leg, model: "cube", scale: new(0.32f, 0.18f, 0.34f), position: new(0, -0.64f, zOff + 0.04f), color: Rgb(38, 34, 44));
                    }
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 1.18f, 0));
                new Entity(parent: torso, model: "sphere", scale: new(1.28f, 1.15f, 1.75f), color: furMain);
                new Entity(parent: torso, model: "sphere", scale: new(1.42f, 1.32f, 1.05f), position: new(0, 0.15f, 0.35f), color: furDark);
                // Заснеженный хребет и ледяные наросты на горбу
                new Entity(parent: torso, model: "diamond", scale: new(0.86f, 0.32f, 1.45f), position: new(0, 0.56f, 0.05f), color: snowFrost);
                new Entity(parent: torso, model: "diamond", scale: new(0.45f, 0.55f, 0.95f), position: new(0, 0.65f, 0.25f), color: hornIce, unlit: true);
                foreach (int side in new[] { -1, 1 })
                    new Entity(parent: torso, model: "diamond", scale: new(0.28f, 0.42f, 0.45f), position: new(side * 0.52f, 0.45f, 0.28f), rotation: new(0, 0, side * -24f), color: hornIce, unlit: true);

                var tail = new Entity(parent: torso, position: new(0, 0.10f, -0.85f), rotation: new(-30f, 0, 0));
                new Entity(parent: tail, model: "diamond", scale: new(0.22f, 0.22f, 0.55f), position: new(0, 0, -0.22f), color: furDark);
                parts.Tail = tail;

                var head = new Entity(parent: torso, position: new(0, -0.10f, 0.92f));
                new Entity(parent: head, model: "cube", scale: new(0.78f, 0.72f, 0.75f), color: furDark);
                new Entity(parent: head, model: "diamond", scale: new(0.84f, 0.38f, 0.82f), position: new(0, 0.24f, 0.08f), color: snowFrost);
                new Entity(parent: head, model: "cube", scale: new(0.48f, 0.26f, 0.28f), position: new(0, -0.16f, 0.36f), color: Rgb(42, 36, 48));
                foreach (int side in new[] { -1, 1 })
                {
                    new Entity(parent: head, model: "diamond", scale: new(0.28f, 0.28f, 0.98f), position: new(side * 0.48f, 0.12f, 0.32f), rotation: new(-12f, side * 22f, 0), color: hornIce, unlit: true);
                    new Entity(parent: head, model: "diamond", scale: new(0.18f, 0.36f, 0.28f), position: new(side * 0.64f, 0.26f, 0.68f), rotation: new(28f, 0, side * -15f), color: Color.White, unlit: true);
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.14f, position: new(side * 0.32f, 0.08f, 0.36f), color: Color.Orange, unlit: true);
                }
                parts.Torso = torso;
                parts.Crown = head;
            }
            else if (enemyType == "brass_contraption")
            {
                Color brass = Rgb(205, 155, 65);
                Color bronzeDark = Rgb(115, 80, 35);
                Color coreGlow = Rgb(255, 215, 90);

                var core = new Entity(parent: root, position: new(0, 2.4f, 0));
                new Entity(parent: core, model: "sphere", scaleUniform: 0.75f, color: coreGlow, unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(1.45f, 1.85f, 1.45f), color: brass);
                new Entity(parent: core, model: "cube", scale: new(1.55f, 0.28f, 1.55f), position: new(0, -0.55f, 0), color: bronzeDark);
                new Entity(parent: core, model: "diamond", scale: new(0.62f, 0.85f, 0.62f), position: new(0, -1.05f, 0), color: bronzeDark);
                new Entity(parent: core, model: "sphere", scaleUniform: 0.28f, position: new(0, 0.15f, 0.65f), color: Color.Red, unlit: true);
                new Entity(parent: core, model: "sphere", scaleUniform: 0.14f, position: new(0, 0.15f, 0.76f), color: Color.Yellow, unlit: true);

                var ring1 = new Entity(parent: core, model: "diamond", scale: new(2.45f, 0.32f, 2.45f), color: bronzeDark);
                for (int i = 0; i < 4; i++)
                {
                    float ang = i * (MathF.PI * 0.5f);
                    new Entity(parent: ring1, model: "sphere", scaleUniform: 0.24f, position: new(MathF.Cos(ang) * 0.48f, 0, MathF.Sin(ang) * 0.48f), color: coreGlow, unlit: true);
                    new Entity(parent: ring1, model: "cube", scale: new(0.18f, 0.45f, 0.18f), position: new(MathF.Cos(ang + 0.78f) * 0.42f, 0, MathF.Sin(ang + 0.78f) * 0.42f), color: brass);
                }
                parts.Core = core;
                parts.Ring1 = ring1;
                parts.Ring2 = new Entity(parent: core, model: "diamond", scale: new(0.28f, 2.25f, 2.25f), color: brass);
            }
            else if (enemyType == "parent")
            {
                Color astralBody = Rgb(58, 42, 88);
                Color astralGlow = Rgb(255, 215, 110);
                Color astralTrim = Rgb(135, 95, 195);

                var lLeg = new Entity(parent: root, position: new(-0.52f, 1.15f, 0));
                var rLeg = new Entity(parent: root, position: new(0.52f, 1.15f, 0));
                foreach (var leg in new[] { lLeg, rLeg })
                {
                    new Entity(parent: leg, model: "diamond", scale: new(0.52f, 1.15f, 0.52f), position: new(0, -0.55f, 0), color: astralBody);
                    new Entity(parent: leg, model: "sphere", scaleUniform: 0.22f, position: new(0, -0.50f, 0.18f), color: astralGlow, unlit: true);
                    new Entity(parent: leg, model: "cube", scale: new(0.56f, 0.26f, 0.68f), position: new(0, -1.02f, 0.08f), color: astralTrim);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 2.25f, 0));
                new Entity(parent: torso, model: "sphere", scale: new(1.75f, 1.65f, 1.25f), color: astralBody);
                new Entity(parent: torso, model: "diamond", scale: new(1.95f, 0.55f, 1.35f), position: new(0, 0.52f, 0), color: astralTrim);
                new Entity(parent: torso, model: "sphere", scaleUniform: 0.58f, position: new(0, 0.15f, 0.52f), color: astralGlow, unlit: true);
                // Звёздные созвездия внутри астрального тела
                foreach (var (sx, sy, sz) in new[] { (-0.45f, 0.35f, 0.42f), (0.48f, -0.18f, 0.40f), (-0.32f, -0.35f, 0.38f), (0.38f, 0.42f, -0.42f) })
                    new Entity(parent: torso, model: "sphere", scaleUniform: 0.14f, position: new(sx, sy, sz), color: Color.White, unlit: true);

                var head = new Entity(parent: torso, position: new(0, 1.15f, 0));
                new Entity(parent: head, model: "diamond", scale: new(0.85f, 0.85f, 0.45f), color: astralTrim);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.42f, position: new(0, 0, 0.18f), color: astralGlow, unlit: true);
                new Entity(parent: head, model: "diamond", scale: new(1.45f, 1.45f, 0.12f), position: new(0, 0.1f, -0.18f), color: astralGlow, unlit: true);
                parts.Crown = head;

                var lArm = new Entity(parent: torso, position: new(-1.18f, 0.42f, 0));
                var rArm = new Entity(parent: torso, position: new(1.18f, 0.42f, 0));
                foreach (var arm in new[] { lArm, rArm })
                {
                    new Entity(parent: arm, model: "sphere", scaleUniform: 0.56f, color: astralTrim);
                    new Entity(parent: arm, model: "diamond", scale: new(0.55f, 1.45f, 0.55f), position: new(0, -0.75f, 0.15f), color: astralBody);
                    new Entity(parent: arm, model: "sphere", scaleUniform: 0.68f, position: new(0, -1.45f, 0.25f), color: astralGlow, unlit: true);
                    new Entity(parent: arm, model: "diamond", scale: new(0.95f, 0.18f, 0.95f), position: new(0, -1.45f, 0.25f), color: Color.White, unlit: true);
                }
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
            }
            else if (enemyType == "elder_lemurian")
            {
                Color scaleDark = Rgb(115, 32, 28);
                Color magmaBelly = Rgb(255, 125, 35);
                Color hornBone = Rgb(245, 225, 185);
                Color plateCol = Rgb(55, 24, 24);

                var lLeg = new Entity(parent: root, position: new(-0.52f, 1.25f, 0));
                var rLeg = new Entity(parent: root, position: new(0.52f, 1.25f, 0));
                foreach (var leg in new[] { lLeg, rLeg })
                {
                    new Entity(parent: leg, model: "cube", scale: new(0.46f, 0.72f, 0.52f), position: new(0, -0.35f, 0), color: scaleDark);
                    new Entity(parent: leg, model: "diamond", scale: new(0.54f, 0.42f, 0.58f), position: new(0, -0.68f, 0.10f), color: plateCol);
                    new Entity(parent: leg, model: "cube", scale: new(0.40f, 0.58f, 0.44f), position: new(0, -0.95f, 0), color: scaleDark);
                    new Entity(parent: leg, model: "diamond", scale: new(0.55f, 0.28f, 0.82f), position: new(0, -1.18f, 0.22f), color: hornBone);
                    foreach (float cx in new[] { -0.14f, 0f, 0.14f })
                        new Entity(parent: leg, model: "diamond", scale: new(0.10f, 0.12f, 0.24f), position: new(cx, -1.24f, 0.58f), rotation: new(-20f, 0, 0), color: hornBone);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 2.05f, 0), rotation: new(16f, 0, 0));
                new Entity(parent: torso, model: "cube", scale: new(1.32f, 1.68f, 1.02f), color: scaleDark);
                new Entity(parent: torso, model: "cube", scale: new(0.96f, 1.48f, 0.28f), position: new(0, 0, 0.44f), color: magmaBelly, unlit: true);
                foreach (float ry in new[] { -0.45f, -0.12f, 0.22f })
                    new Entity(parent: torso, model: "cube", scale: new(1.02f, 0.08f, 0.32f), position: new(0, ry, 0.46f), color: plateCol);
                new Entity(parent: torso, model: "diamond", scale: new(1.85f, 0.65f, 1.08f), position: new(0, 0.58f, 0), color: plateCol);
                for (int i = 0; i < 5; i++)
                    new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.68f, 0.45f), position: new(0, 0.65f - i * 0.35f, -0.58f), rotation: new(-35, 0, 0), color: magmaBelly, unlit: true);

                var tail = new Entity(parent: torso, position: new(0, -0.68f, -0.45f));
                new Entity(parent: tail, model: "diamond", scale: new(0.58f, 0.48f, 1.65f), position: new(0, -0.15f, -0.75f), rotation: new(-20, 0, 0), color: scaleDark);
                new Entity(parent: tail, model: "diamond", scale: new(0.32f, 0.52f, 0.65f), position: new(0, -0.35f, -1.65f), rotation: new(-12, 0, 0), color: magmaBelly, unlit: true);
                parts.Tail = tail;

                var head = new Entity(parent: torso, position: new(0, 1.12f, 0.32f), rotation: new(-16f, 0, 0));
                new Entity(parent: head, model: "diamond", scale: new(1.18f, 0.85f, 1.65f), color: scaleDark);
                new Entity(parent: head, model: "diamond", scale: new(1.08f, 0.42f, 1.35f), position: new(0, 0.22f, -0.05f), color: plateCol);
                new Entity(parent: head, model: "cube", scale: new(0.82f, 0.28f, 1.18f), position: new(0, -0.32f, 0.28f), color: magmaBelly, unlit: true);
                foreach (float tx in new[] { -0.28f, -0.10f, 0.10f, 0.28f })
                    new Entity(parent: head, model: "diamond", scale: new(0.10f, 0.22f, 0.10f), position: new(tx, -0.18f, 0.68f), color: hornBone);
                foreach (int side in new[] { -1, 1 })
                {
                    new Entity(parent: head, model: "diamond", scale: new(0.32f, 1.15f, 0.32f), position: new(side * 0.44f, 0.58f, -0.35f), rotation: new(-32, 0, side * -22f), color: hornBone);
                    new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.62f, 0.20f), position: new(side * 0.28f, 0.46f, -0.15f), rotation: new(-24, 0, side * -12f), color: hornBone);
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.24f, position: new(side * 0.36f, 0.16f, 0.45f), color: Color.Yellow, unlit: true);
                }
                parts.Crown = head;

                var lArm = new Entity(parent: torso, position: new(-0.85f, 0.38f, 0.15f));
                var rArm = new Entity(parent: torso, position: new(0.85f, 0.38f, 0.15f));
                foreach (var (side, arm) in new[] { (-1, lArm), (1, rArm) })
                {
                    new Entity(parent: arm, model: "diamond", scale: new(0.56f, 0.52f, 0.56f), color: plateCol);
                    new Entity(parent: arm, model: "diamond", scale: new(0.22f, 0.48f, 0.22f), position: new(side * 0.24f, 0.32f, 0), rotation: new(0, 0, side * -25f), color: hornBone);
                    new Entity(parent: arm, model: "cube", scale: new(0.34f, 0.95f, 0.34f), position: new(0, -0.38f, 0.32f), rotation: new(-45, 0, 0), color: scaleDark);
                    new Entity(parent: arm, model: "diamond", scale: new(0.42f, 0.48f, 0.32f), position: new(0, -0.75f, 0.68f), rotation: new(-65, 0, 0), color: hornBone);
                }
                parts.Torso = torso;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
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
                Color runeRed = Rgb(255, 45, 35);

                // Ноги — массивные каменные с суставами
                var lLeg = new Entity(parent: root, position: new(-0.85f, 1.6f, 0));
                var rLeg = new Entity(parent: root, position: new(0.85f, 1.6f, 0));
                foreach (var (side, leg) in new[] { (-1, lLeg), (1, rLeg) })
                {
                    // Бедро
                    new Entity(parent: leg, model: "cube", scale: new(0.90f, 0.80f, 0.96f), position: new(0, -0.40f, 0), color: stoneDark);
                    // Руна на бедре
                    new Entity(parent: leg, model: "cube", scale: new(0.96f, 0.16f, 1.02f), position: new(0, -0.65f, 0), color: Color.Red, unlit: true);
                    // Колено
                    new Entity(parent: leg, model: "sphere", scale: new(0.72f, 0.55f, 0.72f), position: new(0, -0.82f, 0.05f), color: stoneLight);
                    // Голень
                    new Entity(parent: leg, model: "cube", scale: new(0.82f, 0.72f, 0.88f), position: new(0, -1.15f, 0), color: stoneDark);
                    // Руна на голени
                    new Entity(parent: leg, model: "cube", scale: new(0.10f, 0.55f, 0.02f), position: new(side * 0.42f, -1.0f, 0.44f), color: runeRed, unlit: true);
                    // Стопа
                    new Entity(parent: leg, model: "cube", scale: new(1.08f, 0.44f, 1.28f), position: new(0, -1.5f, 0.15f), color: stoneLight);
                    // Пальцы стопы
                    foreach (float fz in new[] { -0.25f, 0f, 0.25f })
                        new Entity(parent: leg, model: "cube", scale: new(0.28f, 0.18f, 0.32f), position: new(0, -1.68f, fz + 0.40f), color: stoneDark);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                // Торс — многослойный
                var torso = new Entity(parent: root, position: new(0, 3.2f, 0));
                new Entity(parent: torso, model: "cube", scale: new(2.68f, 2.85f, 1.85f), color: stoneLight);
                // Рёбра по бокам торса
                foreach (int side in new[] { -1, 1 })
                    foreach (float ry in new[] { -0.65f, -0.15f, 0.35f, 0.85f })
                        new Entity(parent: torso, model: "cube", scale: new(0.12f, 0.18f, 1.88f), position: new(side * 1.35f, ry, 0), color: stoneDark);
                // Нагрудная плита
                new Entity(parent: torso, model: "diamond", scale: new(2.95f, 0.85f, 2.05f), position: new(0, 1.05f, 0), color: stoneDark);
                // Сердцевина-ядро
                var core = new Entity(parent: torso, model: "sphere", scale: new(1.04f, 1.04f, 0.62f), position: new(0, 0.25f, 0.84f), color: Color.Red, unlit: true);
                new Entity(parent: core, model: "diamond", scale: new(0.60f, 0.60f, 0.90f), position: new(0, 0, 0.22f), color: Color.Yellow, unlit: true);
                // Ореол вокруг ядра
                new Entity(parent: core, model: "diamond", scale: new(1.45f, 1.45f, 0.15f), color: Rgb(255, 75, 45, 100), unlit: true);
                // Боковые руны-линии
                foreach (float sx in new[] { -0.88f, 0.88f })
                    new Entity(parent: torso, model: "cube", scale: new(0.24f, 2.25f, 1.90f), position: new(sx, 0, 0), color: Color.Red, unlit: true);
                // Дополнительные руны по центру
                new Entity(parent: torso, model: "cube", scale: new(2.25f, 0.12f, 0.05f), position: new(0, -0.85f, 0.93f), color: runeRed, unlit: true);
                new Entity(parent: torso, model: "cube", scale: new(2.25f, 0.12f, 0.05f), position: new(0, 0.55f, 0.93f), color: runeRed, unlit: true);

                // Руки — массивные
                var lArm = new Entity(parent: torso, position: new(-1.88f, 0.6f, 0));
                var rArm = new Entity(parent: torso, position: new(1.88f, 0.6f, 0));
                foreach (var (side, arm) in new[] { (-1, lArm), (1, rArm) })
                {
                    // Плечо
                    new Entity(parent: arm, model: "diamond", scale: new(1.12f, 1.22f, 1.12f), position: new(0, 0.32f, 0), color: stoneDark);
                    // Шип на плече
                    new Entity(parent: arm, model: "diamond", scale: new(0.35f, 0.65f, 0.35f), position: new(side * 0.30f, 0.72f, 0), rotation: new(0, 0, side * -15f), color: stoneLight);
                    // Верхняя рука
                    new Entity(parent: arm, model: "cube", scale: new(0.82f, 1.05f, 0.82f), position: new(0, -0.45f, 0), color: stoneDark);
                    // Руна на руке
                    new Entity(parent: arm, model: "cube", scale: new(0.10f, 0.85f, 0.02f), position: new(0, -0.45f, 0.42f), color: runeRed, unlit: true);
                    // Локоть
                    new Entity(parent: arm, model: "sphere", scale: new(0.72f, 0.52f, 0.72f), position: new(0, -0.95f, 0), color: stoneLight);
                    // Предплечье
                    new Entity(parent: arm, model: "cube", scale: new(0.78f, 0.88f, 0.78f), position: new(0, -1.40f, 0), color: stoneDark);
                    // Кулак
                    new Entity(parent: arm, model: "cube", scale: new(1.06f, 0.92f, 1.06f), position: new(0, -1.9f, 0), color: stoneLight);
                    new Entity(parent: arm, model: "cube", scale: new(1.12f, 0.18f, 1.12f), position: new(0, -1.95f, 0), color: Color.Red, unlit: true);
                    // Костяшки кулака
                    foreach (float fz in new[] { -0.25f, 0f, 0.25f })
                        new Entity(parent: arm, model: "sphere", scaleUniform: 0.18f, position: new(0, -1.48f, fz), color: stoneDark);
                }

                // Корона
                var crown = new Entity(parent: torso, position: new(0, 2.15f, 0));
                new Entity(parent: crown, model: "diamond", scale: new(1.62f, 0.90f, 1.62f), color: Color.Red, unlit: true);
                new Entity(parent: crown, model: "diamond", scale: new(2.12f, 0.22f, 2.12f), rotation: new(0, 45, 0), color: stoneDark);
                new Entity(parent: crown, model: "diamond", scale: new(2.05f, 0.28f, 2.05f), color: stoneLight);
                // Зубцы короны
                foreach (float ca in new[] { 0f, 90f, 180f, 270f })
                {
                    float cx = MathF.Cos(ca * MathF.PI / 180f) * 0.85f;
                    float cz = MathF.Sin(ca * MathF.PI / 180f) * 0.85f;
                    new Entity(parent: crown, model: "diamond", scale: new(0.22f, 0.48f, 0.22f), position: new(cx, 0.35f, cz), color: Color.Red, unlit: true);
                }
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
                parts.Crown = crown;
                parts.Core = core;
                parts.Torso = torso;
            }
            else if (enemyType == "vagrant")
            {
                // Колокол — многослойный с деталями
                var bell = new Entity(parent: root, position: new(0, 4.2f, 0));
                new Entity(parent: bell, model: "sphere", scale: new(4.3f, 2.85f, 4.3f), color: Rgb(55, 205, 255, 185), unlit: true);
                // Внутренний слой
                new Entity(parent: bell, model: "sphere", scale: new(3.8f, 2.45f, 3.8f), position: new(0, 0.1f, 0), color: Rgb(85, 225, 255, 100), unlit: true);
                // Верхний купол
                new Entity(parent: bell, model: "diamond", scale: new(2.6f, 1.6f, 2.6f), position: new(0, 1.1f, 0), color: Rgb(110, 240, 255), unlit: true);
                // Юбка — двойная (разнесена по высоте и масштабу без Z-fighting)
                new Entity(parent: bell, model: "diamond", scale: new(4.75f, 0.45f, 4.75f), position: new(0, -0.5f, 0), color: Rgb(120, 245, 255), unlit: true);
                new Entity(parent: bell, model: "diamond", scale: new(4.82f, 0.38f, 4.82f), position: new(0, -0.53f, 0), rotation: new(0, 45f, 0), color: Rgb(80, 220, 255), unlit: true);
                new Entity(parent: bell, model: "diamond", scale: new(5.05f, 0.22f, 5.05f), position: new(0, -0.7f, 0), color: Rgb(60, 200, 255, 120), unlit: true);
                // Каналы на куполе
                for (int i = 0; i < 6; i++)
                {
                    float ang = (i / 6.0f) * MathF.PI * 2f;
                    float cx = MathF.Cos(ang) * 1.8f;
                    float cz = MathF.Sin(ang) * 1.8f;
                    new Entity(parent: bell, model: "diamond", scale: new(0.15f, 1.5f, 0.15f), position: new(cx, 0.5f, cz), color: Rgb(150, 245, 255, 120), unlit: true);
                }
                // Ядро
                var core = new Entity(parent: bell, model: "sphere", scaleUniform: 1.85f, position: new(0, -0.3f, 0), color: Rgb(255, 230, 80), unlit: true);
                // Внутреннее ядро
                new Entity(parent: core, model: "sphere", scaleUniform: 0.55f, color: Color.White, unlit: true);
                // Пульсирующий ореол
                new Entity(parent: core, model: "diamond", scale: new(2.2f, 0.15f, 2.2f), color: Rgb(255, 255, 120, 120), unlit: true);

                // Щупальца с деталями
                var tentacles = new Entity(parent: bell, position: new(0, -1.0f, 0));
                for (int i = 0; i < 10; i++)
                {
                    float ang = (i / 10.0f) * MathF.PI * 2f;
                    float tLen = i % 2 == 0 ? 3.4f : 2.4f;
                    // Основное щупальце
                    new Entity(parent: tentacles, model: "diamond", scale: new(0.38f, tLen, 0.38f),
                        position: new(MathF.Cos(ang) * 1.42f, -tLen * 0.4f, MathF.Sin(ang) * 1.42f),
                        color: i % 2 == 0 ? Rgb(80, 235, 255) : Color.Yellow, unlit: true);
                    // Кончик щупальца — утолщение
                    new Entity(parent: tentacles, model: "sphere", scaleUniform: 0.22f,
                        position: new(MathF.Cos(ang) * 1.42f, -tLen * 0.82f, MathF.Sin(ang) * 1.42f),
                        color: i % 3 == 0 ? Color.White : Rgb(120, 245, 255), unlit: true);
                }
                parts.Bell = bell;
                parts.Core = core;
                parts.Tentacles = tentacles;
            }
            else if (enemyType == "overlord")
            {
                Color voidDark = Rgb(36, 18, 42);
                Color crimson = Rgb(245, 30, 65);
                Color voidMid = Rgb(55, 28, 62);

                // Ноги
                var lLeg = new Entity(parent: root, position: new(-0.85f, 1.5f, 0));
                var rLeg = new Entity(parent: root, position: new(0.85f, 1.5f, 0));
                foreach (var (side, leg) in new[] { (-1, lLeg), (1, rLeg) })
                {
                    // Бедро
                    new Entity(parent: leg, model: "diamond", scale: new(0.88f, 0.85f, 0.88f), position: new(0, -0.38f, 0), color: voidDark);
                    // Колено — сустав
                    new Entity(parent: leg, model: "sphere", scale: new(0.55f, 0.42f, 0.55f), position: new(0, -0.80f, 0.05f), color: voidMid);
                    // Голень
                    new Entity(parent: leg, model: "diamond", scale: new(0.72f, 0.82f, 0.72f), position: new(0, -1.15f, 0), color: voidDark);
                    // Руна на ноге
                    new Entity(parent: leg, model: "cube", scale: new(0.08f, 0.65f, 0.02f), position: new(side * 0.38f, -0.80f, 0.42f), color: crimson, unlit: true);
                    // Стопа с когтями
                    new Entity(parent: leg, model: "diamond", scale: new(0.55f, 0.45f, 0.95f), position: new(0, -1.45f, 0.25f), color: crimson, unlit: true);
                    // Когти
                    foreach (float cx in new[] { -0.15f, 0f, 0.15f })
                        new Entity(parent: leg, model: "diamond", scale: new(0.08f, 0.10f, 0.25f), position: new(cx, -1.58f, 0.62f), rotation: new(-20, 0, 0), color: Rgb(255, 75, 100), unlit: true);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                // Торс
                var torso = new Entity(parent: root, position: new(0, 3.1f, 0));
                new Entity(parent: torso, model: "diamond", scale: new(2.85f, 3.15f, 1.95f), color: voidDark);
                // Грудная пластина
                new Entity(parent: torso, model: "diamond", scale: new(2.15f, 1.85f, 0.65f), position: new(0, 0.15f, 0.45f), color: voidMid);
                // Руновая сердцевина
                new Entity(parent: torso, model: "diamond", scale: new(0.72f, 2.05f, 0.72f), position: new(0, 0, 0.80f), color: crimson, unlit: true);
                new Entity(parent: torso, model: "sphere", scaleUniform: 0.45f, position: new(0, 0, 0.95f), color: Color.White, unlit: true);
                // Ореол вокруг ядра
                new Entity(parent: torso, model: "diamond", scale: new(1.25f, 1.25f, 0.12f), position: new(0, 0, 0.95f), color: Rgb(255, 55, 85, 100), unlit: true);
                // Рога — двойные
                foreach (int side in new[] { -1, 1 })
                {
                    new Entity(parent: torso, model: "diamond", scale: new(0.48f, 1.95f, 0.48f), position: new(side * 0.98f, 1.70f, 0), rotation: new(0, 0, side * -25f), color: crimson, unlit: true);
                    new Entity(parent: torso, model: "diamond", scale: new(0.28f, 0.85f, 0.28f), position: new(side * 1.15f, 2.25f, 0), rotation: new(0, 0, side * -30f), color: Rgb(255, 85, 110), unlit: true);
                    // Спинные крылья
                    new Entity(parent: torso, model: "diamond", scale: new(0.38f, 1.25f, 0.45f), position: new(side * 0.72f, 0.85f, -0.65f), rotation: new(-30, 0, 0), color: crimson, unlit: true);
                    new Entity(parent: torso, model: "diamond", scale: new(0.25f, 0.85f, 0.35f), position: new(side * 0.55f, 1.25f, -0.85f), rotation: new(-35, 0, side * 10f), color: Rgb(255, 55, 90, 180), unlit: true);
                }
                // Голова Оверлорда
                var head = new Entity(parent: torso, position: new(0, 1.75f, 0.10f));
                new Entity(parent: head, model: "diamond", scale: new(0.85f, 0.95f, 0.85f), color: voidDark);
                // Глаза — четыре
                foreach (var (ex, ey) in new[] { (-0.18f, 0.12f), (0.18f, 0.12f), (-0.10f, -0.05f), (0.10f, -0.05f) })
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(ex, ey, 0.38f), color: crimson, unlit: true);
                // Третий глаз — центральный
                new Entity(parent: head, model: "sphere", scaleUniform: 0.18f, position: new(0, 0.22f, 0.35f), color: Color.White, unlit: true);

                // Руки
                var lArm = new Entity(parent: torso, position: new(-1.88f, 0.55f, 0));
                var rArm = new Entity(parent: torso, position: new(1.88f, 0.55f, 0));
                foreach (var (side, arm) in new[] { (-1, lArm), (1, rArm) })
                {
                    // Плечо
                    new Entity(parent: arm, model: "diamond", scale: new(0.75f, 0.65f, 0.75f), position: new(0, 0.15f, 0), color: voidMid);
                    // Верхняя рука
                    new Entity(parent: arm, model: "diamond", scale: new(0.60f, 1.15f, 0.60f), position: new(0, -0.42f, 0), color: voidDark);
                    // Руна на руке
                    new Entity(parent: arm, model: "cube", scale: new(0.08f, 0.85f, 0.02f), position: new(0, -0.42f, 0.32f), color: crimson, unlit: true);
                    // Локоть
                    new Entity(parent: arm, model: "sphere", scaleUniform: 0.32f, position: new(0, -0.95f, 0), color: voidMid);
                    // Предплечье
                    new Entity(parent: arm, model: "diamond", scale: new(0.55f, 1.05f, 0.55f), position: new(0, -1.35f, 0), color: voidDark);
                    // Когти
                    foreach (float cx in new[] { -0.22f, 0.0f, 0.22f })
                        new Entity(parent: arm, model: "diamond", scale: new(0.28f, 1.25f, 0.45f), position: new(cx, -1.78f, 0.38f), rotation: new(-40, 0, 0), color: crimson, unlit: true);
                    // Кончики когтей
                    foreach (float cx in new[] { -0.22f, 0.0f, 0.22f })
                        new Entity(parent: arm, model: "diamond", scale: new(0.14f, 0.45f, 0.22f), position: new(cx, -2.38f, 0.82f), rotation: new(-45, 0, 0), color: Rgb(255, 85, 110), unlit: true);
                }

                // Корона
                var crown = new Entity(parent: torso, position: new(0, 2.05f, 0));
                new Entity(parent: crown, model: "diamond", scale: new(2.15f, 0.36f, 2.15f), color: crimson, unlit: true);
                new Entity(parent: crown, model: "diamond", scale: new(2.22f, 0.28f, 2.22f), rotation: new(0, 45f, 0), color: voidDark);
                // Зубцы короны
                foreach (float ca in new[] { 0f, 60f, 120f, 180f, 240f, 300f })
                {
                    float cx = MathF.Cos(ca * MathF.PI / 180f) * 0.95f;
                    float cz = MathF.Sin(ca * MathF.PI / 180f) * 0.95f;
                    new Entity(parent: crown, model: "diamond", scale: new(0.18f, 0.42f, 0.18f), position: new(cx, 0.25f, cz), color: crimson, unlit: true);
                }
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
                // Многослойное ядро
                new Entity(parent: core, model: "sphere", scaleUniform: 0.98f, color: cyanGlow, unlit: true);
                new Entity(parent: core, model: "sphere", scaleUniform: 0.65f, position: new(0, 0.02f, 0), color: Color.White, unlit: true);
                // Каменное тело
                new Entity(parent: core, model: "diamond", scale: new(1.68f, 0.54f, 2.35f), color: marble);
                new Entity(parent: core, model: "diamond", scale: new(0.85f, 0.75f, 1.95f), color: slate);
                // Глаза
                foreach (float ex in new[] { -0.32f, 0.32f })
                    new Entity(parent: core, model: "sphere", scaleUniform: 0.18f, position: new(ex, 0.12f, 0.82f), color: cyanGlow, unlit: true);
                // Рот — щель
                new Entity(parent: core, model: "cube", scale: new(0.45f, 0.08f, 0.12f), position: new(0, -0.08f, 0.92f), color: cyanGlow, unlit: true);
                // Каменные шипы
                foreach (var (sx, sy, sz, rx) in new[] { (0f, 0.38f, -0.85f, -25f), (-0.55f, 0.28f, -0.62f, -20f), (0.55f, 0.28f, -0.62f, -20f) })
                    new Entity(parent: core, model: "diamond", scale: new(0.22f, 0.48f, 0.22f), position: new(sx, sy, sz), rotation: new(rx, 0, 0), color: marble);

                parts.Core = core;
                parts.Ring1 = new Entity(parent: core, model: "diamond", scale: new(2.68f, 0.24f, 2.68f), color: slate);
                parts.Ring2 = new Entity(parent: core, model: "diamond", scale: new(0.24f, 2.68f, 2.68f), color: cyanGlow, unlit: true);
                // Третье кольцо — диагональное
                new Entity(parent: core, model: "diamond", scale: new(2.35f, 0.18f, 2.35f), rotation: new(0, 45, 25), color: Rgb(55, 200, 235, 140), unlit: true);

                // Крылья — с деталями
                foreach (int side in new[] { -1, 1 })
                {
                    var wing = new Entity(parent: core, model: "cube", scale: new(1.28f, 0.20f, 0.70f), position: new(side * 1.44f, 0, -0.2f), rotation: new(0, 0, side * 15f), color: marble);
                    new Entity(parent: wing, model: "cube", scale: new(0.95f, 0.09f, 0.88f), position: new(0, 0, 0.42f), color: cyanGlow, unlit: true);
                    // Перья-продолжения крыла
                    new Entity(parent: wing, model: "diamond", scale: new(0.55f, 0.12f, 0.45f), position: new(side * 0.55f, 0, 0.10f), rotation: new(0, 0, side * 10f), color: marble);
                    new Entity(parent: wing, model: "diamond", scale: new(0.35f, 0.08f, 0.35f), position: new(side * 0.85f, 0, -0.05f), rotation: new(0, 0, side * 15f), color: Rgb(200, 215, 235));
                }
            }
            else if (enemyType == "magma_worm")
            {
                Color crustDark = Rgb(58, 26, 22);
                Color lavaBright = Rgb(255, 115, 20);
                Color coreYellow = Rgb(255, 225, 70);

                var torso = new Entity(parent: root, position: new(0, 3.2f, 0));
                // Голова и раскалённая пасть Магмового Червя
                var head = new Entity(parent: torso, position: new(0, 1.4f, 0.8f), rotation: new(28f, 0, 0));
                new Entity(parent: head, model: "sphere", scale: new(2.35f, 2.15f, 2.65f), color: crustDark);
                new Entity(parent: head, model: "diamond", scale: new(2.55f, 1.25f, 2.25f), color: lavaBright, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 1.45f, position: new(0, 0.15f, 0.85f), color: coreYellow, unlit: true);
                new Entity(parent: head, model: "diamond", scale: new(0.65f, 1.35f, 0.95f), position: new(0, 1.15f, -0.25f), rotation: new(-28, 0, 0), color: lavaBright, unlit: true);
                foreach (int s in new[] { -1, 1 })
                {
                    new Entity(parent: head, model: "diamond", scale: new(0.45f, 1.15f, 0.45f), position: new(s * 0.95f, 0.45f, 0.65f), rotation: new(45, 0, s * -20f), color: Color.White, unlit: true);
                    new Entity(parent: head, model: "diamond", scale: new(0.38f, 0.88f, 0.38f), position: new(s * 1.18f, -0.25f, 0.55f), rotation: new(35, 0, s * -35f), color: coreYellow, unlit: true);
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.35f, position: new(s * 0.85f, 0.65f, 0.45f), color: Color.Yellow, unlit: true);
                }

                // Сегменты извивающегося тела с боковыми лавовыми шипами
                var seg1 = new Entity(parent: torso, position: new(0, 0.1f, -0.8f));
                new Entity(parent: seg1, model: "sphere", scale: new(2.15f, 2.05f, 2.25f), color: crustDark);
                new Entity(parent: seg1, model: "diamond", scale: new(2.45f, 0.85f, 2.45f), color: lavaBright, unlit: true);
                foreach (int s in new[] { -1, 1 })
                    new Entity(parent: seg1, model: "diamond", scale: new(0.85f, 0.38f, 0.55f), position: new(s * 1.25f, 0.15f, 0), rotation: new(0, s * -25f, 0), color: coreYellow, unlit: true);

                var seg2 = new Entity(parent: torso, position: new(0, -1.2f, -2.4f));
                new Entity(parent: seg2, model: "sphere", scale: new(1.85f, 1.75f, 2.05f), color: crustDark);
                new Entity(parent: seg2, model: "diamond", scale: new(2.15f, 0.75f, 2.15f), color: coreYellow, unlit: true);
                foreach (int s in new[] { -1, 1 })
                    new Entity(parent: seg2, model: "diamond", scale: new(0.72f, 0.32f, 0.48f), position: new(s * 1.05f, 0.10f, 0), rotation: new(0, s * -25f, 0), color: lavaBright, unlit: true);

                var tail = new Entity(parent: torso, position: new(0, -2.1f, -4.1f));
                new Entity(parent: tail, model: "diamond", scale: new(1.45f, 1.45f, 2.65f), color: lavaBright, unlit: true);
                new Entity(parent: tail, model: "diamond", scale: new(0.85f, 0.85f, 1.65f), position: new(0, 0, -1.35f), color: coreYellow, unlit: true);

                parts.Torso = torso;
                parts.Crown = head;
                parts.Ring1 = seg1;
                parts.Ring2 = seg2;
                parts.Tail = tail;
            }
            else if (enemyType == "dunestrider")
            {
                Color clayPot = Rgb(145, 88, 54);
                Color clayRim = Rgb(95, 55, 36);
                Color tarBlack = Rgb(28, 22, 26);
                Color tarGlow = Rgb(255, 130, 35);

                var lLeg = new Entity(parent: root, position: new(-1.35f, 2.4f, 0));
                var rLeg = new Entity(parent: root, position: new(1.35f, 2.4f, 0));
                foreach (var (side, leg) in new[] { (-1, lLeg), (1, rLeg) })
                {
                    foreach (float zOff in new[] { -0.95f, 0.95f })
                    {
                        new Entity(parent: leg, model: "diamond", scale: new(0.55f, 2.85f, 0.55f), position: new(side * 0.45f, -1.15f, zOff), rotation: new(0, 0, side * 16f), color: tarBlack);
                        new Entity(parent: leg, model: "sphere", scaleUniform: 0.48f, position: new(side * 0.25f, 0.05f, zOff), color: tarGlow, unlit: true);
                        new Entity(parent: leg, model: "sphere", scaleUniform: 0.36f, position: new(side * 0.48f, -1.15f, zOff), color: clayRim);
                    }
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                var torso = new Entity(parent: root, position: new(0, 3.35f, 0));
                new Entity(parent: torso, model: "sphere", scale: new(2.85f, 2.45f, 2.85f), color: clayPot);
                new Entity(parent: torso, model: "diamond", scale: new(3.15f, 0.55f, 3.15f), position: new(0, 0.35f, 0), color: clayRim);
                new Entity(parent: torso, model: "cube", scale: new(2.95f, 0.24f, 2.95f), position: new(0, -0.35f, 0), color: tarGlow, unlit: true);
                // Ручки кувшина и руны пустыни по бокам
                foreach (int side in new[] { -1, 1 })
                {
                    new Entity(parent: torso, model: "diamond", scale: new(0.45f, 1.05f, 0.35f), position: new(side * 1.48f, 0.25f, 0), rotation: new(0, 0, side * -18f), color: clayRim);
                    new Entity(parent: torso, model: "sphere", scaleUniform: 0.28f, position: new(side * 0.95f, 0.15f, 1.05f), color: tarGlow, unlit: true);
                }

                // Свисающие смоляные щупальца снизу кувшина
                var tentacles = new Entity(parent: torso, position: new(0, -1.10f, 0));
                for (int i = 0; i < 6; i++)
                {
                    float ang = i * (MathF.PI / 3f);
                    new Entity(parent: tentacles, model: "diamond", scale: new(0.28f, 1.45f, 0.28f),
                        position: new(MathF.Cos(ang) * 0.65f, -0.55f, MathF.Sin(ang) * 0.65f),
                        color: i % 2 == 0 ? tarBlack : tarGlow, unlit: i % 2 != 0);
                }

                var crown = new Entity(parent: torso, position: new(0, 1.25f, 0));
                new Entity(parent: crown, model: "diamond", scale: new(2.15f, 0.65f, 2.15f), color: clayRim);
                new Entity(parent: crown, model: "sphere", scale: new(1.65f, 0.85f, 1.65f), position: new(0, 0.25f, 0), color: tarGlow, unlit: true);

                parts.Torso = torso;
                parts.Crown = crown;
                parts.Core = crown;
                parts.Tentacles = tentacles;
            }
            else if (enemyType == "solus_control")
            {
                Color alloyShell = Rgb(185, 198, 215);
                Color darkSteel = Rgb(48, 58, 76);
                Color cyberCyan = Rgb(65, 225, 255);

                var core = new Entity(parent: root, position: new(0, 3.8f, 0));
                new Entity(parent: core, model: "sphere", scaleUniform: 2.55f, color: alloyShell);
                new Entity(parent: core, model: "cube", scale: new(2.65f, 0.42f, 2.65f), color: darkSteel);
                new Entity(parent: core, model: "diamond", scale: new(0.95f, 1.15f, 0.95f), position: new(0, 1.45f, 0), color: darkSteel);
                new Entity(parent: core, model: "sphere", scaleUniform: 0.35f, position: new(0, 2.02f, 0), color: cyberCyan, unlit: true);
                var eye = new Entity(parent: core, model: "sphere", scaleUniform: 0.95f, position: new(0, 0.1f, 1.05f), color: cyberCyan, unlit: true);
                new Entity(parent: eye, model: "sphere", scaleUniform: 0.45f, position: new(0, 0, 0.35f), color: Color.White, unlit: true);

                var ring1 = new Entity(parent: core, model: "diamond", scale: new(3.85f, 0.28f, 3.85f), color: darkSteel);
                var ring2 = new Entity(parent: core, model: "diamond", scale: new(0.28f, 3.85f, 3.85f), color: cyberCyan, unlit: true);

                var lArm = new Entity(parent: core, position: new(-2.15f, -0.35f, 0.45f));
                var rArm = new Entity(parent: core, position: new(2.15f, -0.35f, 0.45f));
                foreach (var (side, arm) in new[] { (-1, lArm), (1, rArm) })
                {
                    new Entity(parent: arm, model: "sphere", scaleUniform: 0.65f, color: darkSteel);
                    new Entity(parent: arm, model: "diamond", scale: new(0.45f, 0.55f, 1.65f), position: new(0, -0.15f, 0.75f), color: alloyShell);
                    new Entity(parent: arm, model: "sphere", scaleUniform: 0.42f, position: new(0, -0.15f, 1.45f), color: cyberCyan, unlit: true);
                    foreach (float py in new[] { -0.38f, 0.08f })
                        new Entity(parent: arm, model: "diamond", scale: new(0.16f, 0.14f, 0.52f), position: new(0, py, 1.72f), color: darkSteel);
                }

                parts.Core = core;
                parts.Torso = core;
                parts.Ring1 = ring1;
                parts.Ring2 = ring2;
                parts.LeftArm = lArm;
                parts.RightArm = rArm;
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
                    // Тассеты — набедренные пластины
                    new Entity(parent: leg, model: "diamond", scale: new(0.55f, 0.32f, 0.58f), position: new(0, -0.02f, 0.05f), color: lunarWhite);
                    // Бедро
                    new Entity(parent: leg, model: "cube", scale: new(0.46f, 0.95f, 0.48f), position: new(0, -0.45f, 0), color: obsidian);
                    // Боковая бронепластина
                    new Entity(parent: leg, model: "diamond", scale: new(0.22f, 0.85f, 0.62f), position: new(side * 0.28f, -0.35f, 0), rotation: new(0, 0, side * -12f), color: lunarWhite);
                    // Наколенник
                    new Entity(parent: leg, model: "diamond", scale: new(0.54f, 0.45f, 0.58f), position: new(0, -0.95f, 0.14f), color: lunarWhite);
                    // Руна на наколеннике
                    new Entity(parent: leg, model: "diamond", scale: new(0.24f, 0.32f, 0.62f), position: new(0, -0.95f, 0.18f), color: cyanFire, unlit: true);
                    // Голень
                    new Entity(parent: leg, model: "cube", scale: new(0.40f, 0.92f, 0.44f), position: new(0, -1.40f, 0), color: lunarSilver);
                    // Руна на голени
                    new Entity(parent: leg, model: "cube", scale: new(0.06f, 0.65f, 0.02f), position: new(side * 0.20f, -1.35f, 0.22f), color: cyanFire, unlit: true);
                    // Сабатон — стопа
                    new Entity(parent: leg, model: "diamond", scale: new(0.52f, 0.32f, 0.85f), position: new(0, -1.85f, 0.16f), color: lunarWhite);
                    // Шпоры
                    new Entity(parent: leg, model: "diamond", scale: new(0.12f, 0.12f, 0.28f), position: new(0, -1.88f, -0.20f), rotation: new(20, 0, 0), color: cyanFire, unlit: true);
                }
                parts.LeftLeg = lLeg;
                parts.RightLeg = rLeg;

                // 2. Атлетичный торс, пояс и V-образный нагрудник
                var torso = new Entity(parent: root, position: new(0, 2.85f, 0));
                // Пояс
                new Entity(parent: torso, model: "cube", scale: new(1.05f, 0.55f, 0.68f), position: new(0, -0.65f, 0), color: obsidian);
                // Пряжка пояса
                new Entity(parent: torso, model: "diamond", scale: new(0.28f, 0.28f, 0.12f), position: new(0, -0.65f, 0.35f), color: cyanFire, unlit: true);
                // Талия
                new Entity(parent: torso, model: "diamond", scale: new(1.25f, 0.32f, 0.78f), position: new(0, -0.45f, 0), color: cobalt);
                // Передняя юбка брони
                new Entity(parent: torso, model: "diamond", scale: new(0.42f, 1.25f, 0.16f), position: new(0, -1.15f, 0.36f), color: cobalt);
                // Основной торс
                new Entity(parent: torso, model: "diamond", scale: new(1.75f, 1.55f, 1.05f), position: new(0, 0.20f, 0), color: lunarWhite);
                new Entity(parent: torso, model: "cube", scale: new(1.25f, 1.10f, 0.82f), position: new(0, 0.15f, 0), color: obsidian);
                // V-образный нагрудник
                new Entity(parent: torso, model: "cube", scale: new(0.55f, 1.15f, 0.92f), position: new(-0.32f, 0.22f, 0.05f), rotation: new(0, 0, 18f), color: lunarWhite);
                new Entity(parent: torso, model: "cube", scale: new(0.55f, 1.15f, 0.92f), position: new(0.32f, 0.22f, 0.05f), rotation: new(0, 0, -18f), color: lunarWhite);
                // Грудной кристалл
                parts.Core = new Entity(parent: torso, model: "diamond", scale: new(0.48f, 0.78f, 0.45f), position: new(0, 0.25f, 0.42f), color: cyanFire, unlit: true);
                // Ореол вокруг кристалла
                new Entity(parent: torso, model: "diamond", scale: new(0.65f, 0.65f, 0.12f), position: new(0, 0.25f, 0.45f), color: Rgb(75, 235, 255, 85), unlit: true);
                // Абдоминальные пластины
                foreach (float ay in new[] { -0.12f, 0.05f })
                    new Entity(parent: torso, model: "cube", scale: new(0.95f, 0.08f, 0.75f), position: new(0, ay, 0.02f), color: Rgb(42, 50, 68));
                parts.Torso = torso;

                // 3. Развевающийся королевский плащ
                var cape = new Entity(parent: torso, position: new(-0.25f, 0.75f, -0.45f), rotation: new(14, 0, 0));
                new Entity(parent: cape, model: "cube", scale: new(1.45f, 0.28f, 0.35f), color: cobalt);
                new Entity(parent: cape, model: "diamond", scale: new(1.65f, 2.65f, 0.18f), position: new(0, -1.35f, -0.15f), color: cobalt);
                new Entity(parent: cape, model: "diamond", scale: new(0.85f, 2.45f, 0.20f), position: new(0, -1.35f, -0.17f), color: cyanFire, unlit: true);
                // Дополнительные складки плаща
                new Entity(parent: cape, model: "diamond", scale: new(1.25f, 2.15f, 0.14f), position: new(0.20f, -1.25f, -0.20f), color: Rgb(35, 82, 185));
                new Entity(parent: cape, model: "diamond", scale: new(1.05f, 1.85f, 0.12f), position: new(-0.20f, -1.15f, -0.22f), color: Rgb(45, 105, 215));
                parts.Cape = cape;

                // 4. Шлем-корона Митрикса
                var head = new Entity(parent: torso, position: new(0, 1.18f, 0.05f));
                // Основа шлема
                new Entity(parent: head, model: "sphere", scale: new(0.68f, 0.78f, 0.72f), color: lunarWhite);
                // Лицевая пластина
                new Entity(parent: head, model: "diamond", scale: new(0.58f, 0.75f, 0.55f), position: new(0, 0.02f, 0.18f), color: obsidian);
                // Визор — вертикальная щель
                new Entity(parent: head, model: "cube", scale: new(0.10f, 0.62f, 0.35f), position: new(0, 0.06f, 0.30f), color: cyanFire, unlit: true);
                // Глаз
                new Entity(parent: head, model: "sphere", scale: new(0.24f, 0.24f, 0.28f), position: new(0, 0.08f, 0.38f), color: Color.White, unlit: true);
                // Дыхательная решётка
                foreach (float gy in new[] { -0.12f, -0.18f, -0.24f })
                    new Entity(parent: head, model: "cube", scale: new(0.32f, 0.03f, 0.08f), position: new(0, gy, 0.36f), color: Rgb(22, 28, 42));
                // Гребни шлема
                foreach (int side in new[] { -1, 1 })
                {
                    var crest = new Entity(parent: head, position: new(side * 0.22f, 0.38f, -0.02f), rotation: new(0, 0, side * -14f));
                    new Entity(parent: crest, model: "diamond", scale: new(0.18f, 1.15f, 0.42f), position: new(0, 0.45f, 0), color: lunarWhite);
                    new Entity(parent: crest, model: "diamond", scale: new(0.12f, 0.95f, 0.22f), position: new(side * -0.05f, 0.52f, 0.08f), color: cyanFire, unlit: true);
                    // Кончик гребня
                    new Entity(parent: crest, model: "diamond", scale: new(0.08f, 0.28f, 0.12f), position: new(0, 1.0f, 0), color: Rgb(120, 245, 255), unlit: true);
                }
                // Нимб за головой
                parts.Crown = new Entity(parent: head, model: "diamond", scale: new(1.35f, 1.35f, 0.12f), position: new(0, 0.35f, -0.32f), color: cyanFire, unlit: true);
                // Второй нимб — меньше, повёрнут
                new Entity(parent: head, model: "diamond", scale: new(1.05f, 1.05f, 0.08f), position: new(0, 0.35f, -0.36f), rotation: new(0, 0, 45), color: Rgb(95, 245, 255, 140), unlit: true);

                // 5. Асимметричные плечи и руки
                var lArm = new Entity(parent: torso, position: new(-1.05f, 0.62f, 0));
                // Левое плечо — массивное
                new Entity(parent: lArm, model: "diamond", scale: new(0.95f, 0.65f, 0.95f), position: new(-0.15f, 0.28f, 0), rotation: new(0, 0, 25), color: lunarWhite);
                new Entity(parent: lArm, model: "diamond", scale: new(0.75f, 0.95f, 0.55f), position: new(-0.32f, 0.55f, 0), rotation: new(0, 0, 35), color: cyanFire, unlit: true);
                // Шип на плече
                new Entity(parent: lArm, model: "diamond", scale: new(0.22f, 0.45f, 0.22f), position: new(-0.52f, 0.82f, 0), rotation: new(0, 0, 42), color: lunarWhite);
                // Верхняя рука
                new Entity(parent: lArm, model: "cube", scale: new(0.38f, 1.35f, 0.38f), position: new(0, -0.55f, 0), color: obsidian);
                // Руна на руке
                new Entity(parent: lArm, model: "cube", scale: new(0.06f, 1.05f, 0.02f), position: new(0, -0.55f, 0.20f), color: cyanFire, unlit: true);
                // Предплечье
                new Entity(parent: lArm, model: "cube", scale: new(0.44f, 0.65f, 0.44f), position: new(0, -1.15f, 0), color: lunarWhite);
                // Перчатка-наруч
                new Entity(parent: lArm, model: "diamond", scale: new(0.52f, 0.28f, 0.52f), position: new(0, -1.05f, 0), color: lunarSilver);
                parts.LeftArm = lArm;

                var rArm = new Entity(parent: torso, position: new(1.05f, 0.62f, 0));
                // Правое плечо
                new Entity(parent: rArm, model: "diamond", scale: new(0.72f, 0.55f, 0.72f), position: new(0, 0.20f, 0), color: lunarSilver);
                // Верхняя рука
                new Entity(parent: rArm, model: "cube", scale: new(0.40f, 1.35f, 0.40f), position: new(0, -0.55f, 0), color: obsidian);
                // Руна
                new Entity(parent: rArm, model: "cube", scale: new(0.06f, 1.05f, 0.02f), position: new(0, -0.55f, 0.21f), color: cyanFire, unlit: true);
                // Предплечье
                new Entity(parent: rArm, model: "cube", scale: new(0.46f, 0.65f, 0.46f), position: new(0, -1.15f, 0), color: lunarWhite);
                new Entity(parent: rArm, model: "diamond", scale: new(0.54f, 0.28f, 0.54f), position: new(0, -1.05f, 0), color: lunarSilver);
                parts.RightArm = rArm;

                // 6. Колоссальный Лунный Молот в правой руке
                var hammer = new Entity(parent: rArm, position: new(0, -1.25f, 0.35f), rotation: new(65, 0, 0));
                // Древко
                new Entity(parent: hammer, model: "cube", scale: new(0.18f, 4.6f, 0.18f), position: new(0, 1.4f, 0), color: obsidian);
                // Обмотки на древке
                foreach (float ry in new[] { 0.2f, 1.2f, 2.2f, 3.0f })
                    new Entity(parent: hammer, model: "cube", scale: new(0.24f, 0.12f, 0.24f), position: new(0, ry, 0), color: cyanFire, unlit: true);
                // Дополнительные декоративные кольца
                foreach (float ry in new[] { 0.7f, 1.7f, 2.6f })
                    new Entity(parent: hammer, model: "diamond", scale: new(0.22f, 0.08f, 0.22f), position: new(0, ry, 0), color: lunarSilver);
                // Навершие
                new Entity(parent: hammer, model: "diamond", scale: new(0.42f, 0.55f, 0.42f), position: new(0, -0.95f, 0), color: lunarWhite);
                // Гравировка на навершии
                new Entity(parent: hammer, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.95f, 0.18f), color: cyanFire, unlit: true);

                // Голова молота
                var headBlock = new Entity(parent: hammer, position: new(0, 3.65f, 0));
                new Entity(parent: headBlock, model: "cube", scale: new(1.35f, 0.95f, 2.45f), color: obsidian);
                new Entity(parent: headBlock, model: "cube", scale: new(1.42f, 0.65f, 1.85f), color: lunarWhite);
                // Ударная грань — передняя
                new Entity(parent: headBlock, model: "diamond", scale: new(1.25f, 0.85f, 0.95f), position: new(0, 0, 1.35f), color: lunarWhite);
                // Руна на ударной грани
                new Entity(parent: headBlock, model: "diamond", scale: new(0.55f, 0.55f, 0.08f), position: new(0, 0, 1.78f), color: cyanFire, unlit: true);
                // Задняя грань
                new Entity(parent: headBlock, model: "diamond", scale: new(0.95f, 0.75f, 1.15f), position: new(0, 0, -1.35f), color: lunarSilver);
                // Шип на задней грани
                new Entity(parent: headBlock, model: "diamond", scale: new(0.32f, 0.32f, 0.55f), position: new(0, 0, -1.85f), color: cyanFire, unlit: true);
                // Светящийся обод
                parts.HammerAura = new Entity(parent: headBlock, model: "cube", scale: new(1.48f, 0.28f, 2.55f), color: cyanFire, unlit: true);
                new Entity(parent: headBlock, model: "diamond", scale: new(1.75f, 0.18f, 1.75f), color: cyanFire, unlit: true);
                // Дополнительный ореол
                new Entity(parent: headBlock, model: "diamond", scale: new(1.55f, 0.12f, 2.85f), rotation: new(0, 0, 45), color: Rgb(95, 245, 255, 100), unlit: true);
                parts.Hammer = hammer;
                parts.PhaseRing = new Entity(parent: torso, model: "diamond", scale: new(3.6f, 0.15f, 3.6f), position: new(0, -0.2f, 0), color: cyanFire, unlit: true, enabled: false);
            }

            return parts;
        }
    }
}