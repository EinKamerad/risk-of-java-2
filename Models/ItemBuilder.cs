using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Models
{
    public static class ItemBuilder
    {
        public static Entity BuildItemMesh(Entity parentEntity, string itemKey)
        {
            var root = new Entity(parent: parentEntity);
            Color itemCol = GameData.ItemPool.TryGetValue(itemKey, out var info)
                ? info.MeshColor
                : Color.Yellow;

            if (itemKey == "syringe")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 35));
                // Корпус шприца — стеклянный цилиндр
                new Entity(parent: tilt, model: "cube", scale: new(0.28f, 0.75f, 0.28f), color: Rgb(220, 235, 250));
                // Внутренняя жидкость
                new Entity(parent: tilt, model: "cube", scale: new(0.22f, 0.55f, 0.22f), color: Color.Yellow, unlit: true);
                // Мерные линии на корпусе
                foreach (float my in new[] { -0.18f, -0.05f, 0.08f, 0.21f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.30f, 0.02f, 0.30f), position: new(0, my, 0), color: Rgb(160, 170, 185));
                // Игла
                new Entity(parent: tilt, model: "cube", scale: new(0.05f, 0.45f, 0.05f), position: new(0, 0.55f, 0), color: Color.LightGray);
                new Entity(parent: tilt, model: "diamond", scale: new(0.04f, 0.14f, 0.04f), position: new(0, 0.78f, 0), color: Rgb(210, 215, 225));
                // Поршень
                new Entity(parent: tilt, model: "cube", scale: new(0.42f, 0.08f, 0.42f), position: new(0, -0.42f, 0), color: Color.Gray);
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.22f, 0.10f), position: new(0, -0.56f, 0), color: Rgb(120, 128, 140));
                // Колечко на поршне
                new Entity(parent: tilt, model: "cube", scale: new(0.32f, 0.06f, 0.32f), position: new(0, -0.68f, 0), color: Rgb(100, 108, 120));
            }
            else if (itemKey == "hoof")
            {
                // Нога — голень
                new Entity(parent: root, model: "cube", scale: new(0.26f, 0.65f, 0.26f), position: new(0, 0.15f, 0), rotation: new(0, 0, -15), color: Rgb(235, 240, 250));
                // Сустав колена
                new Entity(parent: root, model: "sphere", scale: new(0.30f, 0.26f, 0.30f), position: new(0.02f, 0.42f, 0), color: Rgb(200, 210, 230));
                // Копыто — основное
                new Entity(parent: root, model: "diamond", scale: new(0.44f, 0.35f, 0.55f), position: new(0.1f, -0.22f, 0.08f), color: Color.SkyBlue, unlit: true);
                // Подкова — нижняя часть копыта
                new Entity(parent: root, model: "cube", scale: new(0.40f, 0.08f, 0.50f), position: new(0.1f, -0.38f, 0.08f), color: Rgb(180, 195, 220));
                // Свечение скорости
                new Entity(parent: root, model: "diamond", scale: new(0.52f, 0.22f, 0.62f), position: new(0.1f, -0.22f, 0.08f), color: Rgb(120, 220, 255, 95), unlit: true);
            }
            else if (itemKey is "bear" or "safer_spaces")
            {
                Color brown = itemKey == "safer_spaces" ? Rgb(185, 85, 245) : Rgb(215, 130, 55);
                Color innerEar = itemKey == "safer_spaces" ? Rgb(225, 145, 255) : Rgb(235, 175, 115);
                // Тело
                new Entity(parent: root, model: "sphere", scaleUniform: 0.52f, position: new(0, -0.12f, 0), color: brown);
                // Животик
                new Entity(parent: root, model: "sphere", scale: new(0.38f, 0.42f, 0.28f), position: new(0, -0.15f, 0.14f), color: innerEar);
                // Голова
                var head = new Entity(parent: root, model: "sphere", scaleUniform: 0.42f, position: new(0, 0.28f, 0), color: brown);
                // Уши с внутренним ухом
                foreach (float ex in new[] { -0.38f, 0.38f })
                {
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.38f, position: new(ex, 0.38f, 0), color: brown);
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.22f, position: new(ex, 0.38f, 0.08f), color: innerEar);
                }
                // Мордочка
                new Entity(parent: head, model: "sphere", scaleUniform: 0.28f, position: new(0, -0.05f, 0.42f), color: Rgb(245, 210, 170));
                // Глаза-бусинки
                new Entity(parent: head, model: "sphere", scaleUniform: 0.08f, position: new(-0.12f, 0.08f, 0.36f), color: Rgb(25, 20, 20));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.08f, position: new(0.12f, 0.08f, 0.36f), color: Rgb(25, 20, 20));
                // Нос
                new Entity(parent: head, model: "sphere", scaleUniform: 0.08f, position: new(0, -0.02f, 0.48f), color: Rgb(35, 30, 30));
                // Лапки
                foreach (float lx in new[] { -0.22f, 0.22f })
                {
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.16f, position: new(lx, -0.52f, 0.12f), color: brown);
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.14f, position: new(lx, -0.18f, 0.32f), color: brown);
                }
                if (itemKey == "safer_spaces")
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.88f, color: Rgb(200, 85, 255, 95), unlit: true);
            }
            else if (itemKey is "ukulele" or "polylute")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -30));
                Color bodyC = itemKey == "polylute" ? Rgb(155, 55, 225) : Rgb(210, 125, 45);
                Color detailC = itemKey == "polylute" ? Rgb(195, 95, 255) : Rgb(240, 165, 75);
                // Корпус — нижняя часть
                new Entity(parent: tilt, model: "sphere", scale: new(0.48f, 0.55f, 0.18f), position: new(0, -0.18f, 0), color: bodyC);
                // Розетка (резонаторное отверстие)
                new Entity(parent: tilt, model: "sphere", scale: new(0.18f, 0.18f, 0.22f), position: new(0, -0.12f, 0), color: Color.Magenta, unlit: true);
                // Верхняя часть корпуса
                new Entity(parent: tilt, model: "sphere", scale: new(0.36f, 0.40f, 0.19f), position: new(0, 0.05f, 0), color: bodyC);
                // Гриф
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.65f, 0.08f), position: new(0, 0.42f, 0), color: Rgb(110, 65, 25));
                // Лады на грифе
                foreach (float fy in new[] { 0.18f, 0.30f, 0.42f, 0.54f, 0.64f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.11f, 0.015f, 0.09f), position: new(0, fy, 0), color: Rgb(200, 200, 210));
                // Струны
                foreach (float sx in new[] { -0.025f, 0.0f, 0.025f, 0.05f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.005f, 0.85f, 0.005f), position: new(sx - 0.012f, 0.15f, 0.01f), color: detailC, unlit: true);
                // Головка грифа
                new Entity(parent: tilt, model: "diamond", scale: new(0.14f, 0.12f, 0.10f), position: new(0, 0.78f, 0), color: Rgb(90, 50, 18));
                // Колки
                foreach (float kx in new[] { -0.06f, 0.06f })
                    new Entity(parent: tilt, model: "sphere", scaleUniform: 0.035f, position: new(kx, 0.76f, 0.03f), color: Color.Gold, unlit: true);
            }
            else if (itemKey == "gasoline")
            {
                // Канистра — корпус
                new Entity(parent: root, model: "cube", scale: new(0.52f, 0.65f, 0.32f), position: new(0, -0.05f, 0), color: Color.Red);
                // Рифлёная текстура на канистре
                foreach (float gy in new[] { -0.22f, -0.08f, 0.06f, 0.20f })
                    new Entity(parent: root, model: "cube", scale: new(0.54f, 0.04f, 0.34f), position: new(0, gy, 0), color: Rgb(200, 30, 30));
                // Крышка горлышка
                new Entity(parent: root, model: "cube", scale: new(0.32f, 0.14f, 0.16f), position: new(0, 0.34f, 0), color: Rgb(180, 30, 30));
                new Entity(parent: root, model: "cube", scale: new(0.18f, 0.08f, 0.12f), position: new(0, 0.42f, 0), color: Rgb(60, 60, 65));
                // Носик
                new Entity(parent: root, model: "cube", scale: new(0.10f, 0.28f, 0.10f), position: new(0.18f, 0.38f, 0), rotation: new(0, 0, -35), color: Color.Yellow, unlit: true);
                // Капля бензина на носике
                new Entity(parent: root, model: "sphere", scaleUniform: 0.06f, position: new(0.30f, 0.52f, 0), color: Rgb(255, 180, 35), unlit: true);
                // Ручка
                new Entity(parent: root, model: "cube", scale: new(0.28f, 0.06f, 0.10f), position: new(0, 0.38f, 0), color: Rgb(50, 50, 55));
                // Этикетка
                new Entity(parent: root, model: "cube", scale: new(0.36f, 0.18f, 0.01f), position: new(0, -0.02f, 0.17f), color: Rgb(255, 230, 50));
                new Entity(parent: root, model: "diamond", scale: new(0.12f, 0.12f, 0.02f), position: new(0, -0.02f, 0.18f), color: Rgb(35, 35, 40));
            }
            else if (itemKey == "lens")
            {
                // Оправа — дужка
                new Entity(parent: root, model: "cube", scale: new(0.75f, 0.08f, 0.10f), position: new(0, 0.10f, 0), color: Rgb(40, 45, 55));
                // Переносица
                new Entity(parent: root, model: "cube", scale: new(0.08f, 0.06f, 0.12f), position: new(0, 0.04f, 0.04f), color: Rgb(50, 55, 65));
                // Левая линза — оправа + стекло
                new Entity(parent: root, model: "sphere", scale: new(0.34f, 0.30f, 0.06f), position: new(-0.20f, 0.04f, 0.02f), color: Rgb(55, 60, 72));
                new Entity(parent: root, model: "sphere", scale: new(0.30f, 0.26f, 0.06f), position: new(-0.20f, 0.04f, 0.04f), color: Rgb(255, 35, 75), unlit: true);
                // Правая линза — оправа + стекло
                new Entity(parent: root, model: "sphere", scale: new(0.34f, 0.30f, 0.06f), position: new(0.20f, 0.04f, 0.02f), color: Rgb(55, 60, 72));
                new Entity(parent: root, model: "sphere", scale: new(0.30f, 0.26f, 0.06f), position: new(0.20f, 0.04f, 0.04f), color: Rgb(255, 35, 75), unlit: true);
                // Заушники (дужки за уши)
                new Entity(parent: root, model: "cube", scale: new(0.04f, 0.06f, 0.28f), position: new(-0.38f, 0.10f, -0.10f), color: Rgb(40, 45, 55));
                new Entity(parent: root, model: "cube", scale: new(0.04f, 0.06f, 0.28f), position: new(0.38f, 0.10f, -0.10f), color: Rgb(40, 45, 55));
                // Блик на линзе
                new Entity(parent: root, model: "sphere", scaleUniform: 0.06f, position: new(-0.14f, 0.10f, 0.06f), color: Color.White, unlit: true);
            }
            else if (itemKey == "feather")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 25));
                // Стержень пера
                new Entity(parent: tilt, model: "cube", scale: new(0.06f, 0.85f, 0.06f), color: Color.Gold, unlit: true);
                // Основное опахало — левое и правое
                new Entity(parent: tilt, model: "diamond", scale: new(0.28f, 0.68f, 0.08f), position: new(-0.10f, 0.12f, 0), color: Rgb(90, 255, 150), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.22f, 0.62f, 0.08f), position: new(0.10f, 0.14f, 0), color: Rgb(70, 235, 130), unlit: true);
                // Пушистое основание
                new Entity(parent: tilt, model: "sphere", scale: new(0.12f, 0.18f, 0.10f), position: new(0, -0.38f, 0), color: Rgb(255, 250, 220), unlit: true);
                // Верхний кончик пера
                new Entity(parent: tilt, model: "diamond", scale: new(0.10f, 0.22f, 0.05f), position: new(0, 0.52f, 0), color: Rgb(120, 255, 180), unlit: true);
            }
            else if (itemKey == "bungus")
            {
                // Ножка гриба
                new Entity(parent: root, model: "cube", scale: new(0.24f, 0.48f, 0.24f), position: new(0, -0.15f, 0), color: Rgb(230, 240, 225));
                new Entity(parent: root, model: "cube", scale: new(0.18f, 0.12f, 0.18f), position: new(0, -0.38f, 0), color: Rgb(200, 210, 195));
                // Юбочка на ножке
                new Entity(parent: root, model: "diamond", scale: new(0.38f, 0.12f, 0.38f), position: new(0, 0.02f, 0), color: Rgb(240, 248, 235));
                // Шляпка
                new Entity(parent: root, model: "sphere", scale: new(0.75f, 0.42f, 0.75f), position: new(0, 0.18f, 0), color: Rgb(85, 245, 120), unlit: true);
                // Пятнышки на шляпке
                foreach (var (sx, sz) in new[] { (-0.18f, 0.15f), (0.18f, -0.15f), (0.12f, 0.18f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.14f, position: new(sx, 0.32f, sz), color: Color.SkyBlue, unlit: true);
                foreach (var (sx, sz) in new[] { (-0.08f, -0.20f), (0.22f, 0.08f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.10f, position: new(sx, 0.35f, sz), color: Rgb(145, 255, 185), unlit: true);
                // Споры — маленькие парящие частицы
                foreach (var (sx, sy, sz) in new[] { (-0.28f, 0.42f, 0.12f), (0.24f, 0.48f, -0.08f), (0.05f, 0.52f, 0.22f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.04f, position: new(sx, sy, sz), color: Rgb(180, 255, 200, 160), unlit: true);
            }
            else if (itemKey == "tarrasque")
            {
                // Корпус клейма — кристалл крови с рогами
                new Entity(parent: root, model: "diamond", scale: new(0.65f, 0.78f, 0.55f), color: Rgb(235, 35, 60), unlit: true);
                // Верхние рога
                new Entity(parent: root, model: "sphere", scaleUniform: 0.45f, position: new(-0.14f, 0.18f, 0), color: Rgb(255, 85, 95), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.45f, position: new(0.14f, 0.18f, 0), color: Rgb(255, 85, 95), unlit: true);
                // Вены — тонкие линии
                foreach (float vr in new[] { -20f, 0f, 20f })
                    new Entity(parent: root, model: "diamond", scale: new(0.06f, 0.55f, 0.06f), position: new(0, 0, 0), rotation: new(0, 0, vr), color: Rgb(180, 20, 35), unlit: true);
                // Пульсирующее ядро
                new Entity(parent: root, model: "sphere", scaleUniform: 0.22f, position: new(0, -0.08f, 0), color: Color.White, unlit: true);
            }
            else if (itemKey == "tesla_coil")
            {
                // Основание — тяжёлая платформа
                new Entity(parent: root, model: "cube", scale: new(0.45f, 0.12f, 0.45f), position: new(0, -0.30f, 0), color: Rgb(35, 40, 50));
                new Entity(parent: root, model: "cube", scale: new(0.52f, 0.12f, 0.52f), position: new(0, -0.22f, 0), color: Rgb(55, 65, 82));
                // Изоляторы
                foreach (float ix in new[] { -0.14f, 0.14f })
                    new Entity(parent: root, model: "cube", scale: new(0.08f, 0.14f, 0.08f), position: new(ix, -0.14f, 0), color: Rgb(185, 145, 75));
                // Катушка — столб
                new Entity(parent: root, model: "cube", scale: new(0.18f, 0.55f, 0.18f), position: new(0, 0.05f, 0), color: Rgb(180, 120, 60));
                // Обмотки на катушке
                foreach (float cy in new[] { -0.10f, 0.0f, 0.10f, 0.20f })
                    new Entity(parent: root, model: "cube", scale: new(0.22f, 0.04f, 0.22f), position: new(0, cy, 0), color: Rgb(210, 155, 80));
                // Верхняя сфера — разрядник
                new Entity(parent: root, model: "sphere", scale: new(0.48f, 0.28f, 0.48f), position: new(0, 0.38f, 0), color: Color.SkyBlue, unlit: true);
                // Электрические разряды-усики
                foreach (var (rx, ry, rz) in new[] { (-0.32f, 0.45f, 0.08f), (0.28f, 0.50f, -0.12f), (0.05f, 0.55f, 0.25f) })
                    new Entity(parent: root, model: "diamond", scale: new(0.04f, 0.18f, 0.04f), position: new(rx, ry, rz), rotation: new(0, 0, rx * 80f), color: Rgb(150, 235, 255), unlit: true);
            }
            else if (itemKey == "aegis")
            {
                // Щит — внешний контур
                new Entity(parent: root, model: "diamond", scale: new(0.72f, 0.82f, 0.08f), color: Rgb(55, 62, 78));
                // Золотой щит — основной
                new Entity(parent: root, model: "diamond", scale: new(0.68f, 0.78f, 0.12f), position: new(0, 0, 0.01f), color: Color.Gold, unlit: true);
                // Центральный герб
                new Entity(parent: root, model: "diamond", scale: new(0.42f, 0.52f, 0.16f), position: new(0, 0, 0.02f), color: Color.White, unlit: true);
                // Крестообразный символ
                new Entity(parent: root, model: "cube", scale: new(0.08f, 0.36f, 0.18f), position: new(0, 0, 0.04f), color: Color.Gold, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.28f, 0.08f, 0.18f), position: new(0, 0.05f, 0.04f), color: Color.Gold, unlit: true);
                // Заклёпки по краям
                foreach (var (px, py) in new[] { (-0.22f, 0.28f), (0.22f, 0.28f), (-0.28f, -0.08f), (0.28f, -0.08f), (0f, -0.32f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.05f, position: new(px, py, 0.08f), color: Rgb(255, 225, 120), unlit: true);
            }
            else if (itemKey is "shattering_justice" or "guillotine")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 25));
                // Рукоять
                new Entity(parent: tilt, model: "cube", scale: new(0.12f, 0.85f, 0.12f), color: Rgb(80, 85, 95));
                // Обмотка рукояти
                foreach (float wy in new[] { -0.32f, -0.18f, -0.04f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.14f, 0.06f, 0.14f), position: new(0, wy, 0), color: Rgb(120, 85, 45));
                // Навершие
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.10f, position: new(0, -0.48f, 0), color: Rgb(100, 105, 115));
                // Лезвие/голова
                new Entity(parent: tilt, model: "cube", scale: new(0.55f, 0.28f, 0.28f), position: new(0, 0.35f, 0), color: itemCol, unlit: true);
                // Режущая кромка
                new Entity(parent: tilt, model: "diamond", scale: new(0.62f, 0.14f, 0.14f), position: new(0, 0.35f, 0.12f), color: Color.White, unlit: true);
                // Гарда
                new Entity(parent: tilt, model: "cube", scale: new(0.28f, 0.06f, 0.18f), position: new(0, 0.14f, 0), color: Rgb(100, 105, 115));
            }
            else if (itemKey == "crowbar")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 30));
                // Стержень лома
                new Entity(parent: tilt, model: "cube", scale: new(0.12f, 0.85f, 0.12f), color: Rgb(210, 45, 45));
                // Рифлёная часть
                foreach (float gy in new[] { -0.15f, -0.05f, 0.05f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.14f, 0.04f, 0.14f), position: new(0, gy, 0), color: Rgb(180, 35, 35));
                // Загнутый конец — гвоздодёр
                new Entity(parent: tilt, model: "cube", scale: new(0.32f, 0.12f, 0.12f), position: new(0.1f, 0.42f, 0), color: Rgb(200, 210, 225));
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.18f, 0.12f), position: new(0.26f, 0.36f, 0), color: Rgb(190, 200, 215));
                // Второй загнутый конец
                new Entity(parent: tilt, model: "cube", scale: new(0.16f, 0.10f, 0.12f), position: new(0, -0.48f, 0.04f), rotation: new(20, 0, 0), color: Rgb(200, 210, 225));
            }
            else if (itemKey is "tri_tip" or "harvester_scythe" or "ceremonial_dagger")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -35));
                // Лезвие
                new Entity(parent: tilt, model: "diamond", scale: new(0.28f, 0.85f, 0.14f), position: new(0, 0.12f, 0), color: itemCol, unlit: true);
                // Острие
                new Entity(parent: tilt, model: "diamond", scale: new(0.12f, 0.28f, 0.08f), position: new(0, 0.58f, 0), color: Color.White, unlit: true);
                // Режущая кромка
                new Entity(parent: tilt, model: "cube", scale: new(0.02f, 0.72f, 0.08f), position: new(0.12f, 0.14f, 0), color: Rgb(255, 245, 235), unlit: true);
                // Рукоять
                new Entity(parent: tilt, model: "cube", scale: new(0.12f, 0.35f, 0.12f), position: new(0, -0.35f, 0), color: Rgb(60, 65, 75));
                // Обмотка рукояти
                foreach (float wy in new[] { -0.42f, -0.32f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.14f, 0.04f, 0.14f), position: new(0, wy, 0), color: Rgb(90, 55, 35));
                // Гарда
                new Entity(parent: tilt, model: "cube", scale: new(0.22f, 0.06f, 0.16f), position: new(0, -0.18f, 0), color: Rgb(80, 85, 95));
            }
            else if (itemKey is "ap_rounds" or "backup_mag" or "bandolier" or "afterburner" or "lysate_cell")
            {
                // Корпус магазина / контейнера
                new Entity(parent: root, model: "cube", scale: new(0.48f, 0.58f, 0.28f), color: Rgb(65, 75, 90));
                // Рёбра жёсткости
                foreach (float ry in new[] { -0.18f, 0.08f })
                    new Entity(parent: root, model: "cube", scale: new(0.50f, 0.04f, 0.30f), position: new(0, ry, 0), color: Rgb(52, 60, 75));
                // Патроны / индикаторы
                foreach (float bx in new[] { -0.14f, 0.0f, 0.14f })
                    new Entity(parent: root, model: "diamond", scale: new(0.12f, 0.45f, 0.12f), position: new(bx, 0.32f, 0), color: itemCol, unlit: true);
                // Крышка
                new Entity(parent: root, model: "cube", scale: new(0.44f, 0.06f, 0.24f), position: new(0, -0.32f, 0), color: Rgb(50, 58, 72));
                // Маркировка
                new Entity(parent: root, model: "cube", scale: new(0.18f, 0.08f, 0.01f), position: new(0.08f, 0.12f, 0.15f), color: itemCol, unlit: true);
            }
            else if (itemKey is "energy_drink" or "will_o_wisp" or "infusion" or "plasma_shrimp")
            {
                // Банка / колба
                new Entity(parent: root, model: "cube", scale: new(0.38f, 0.68f, 0.38f), color: itemCol, unlit: true);
                // Обод сверху
                new Entity(parent: root, model: "cube", scale: new(0.42f, 0.08f, 0.42f), position: new(0, 0.35f, 0), color: Color.White);
                // Обод снизу
                new Entity(parent: root, model: "cube", scale: new(0.42f, 0.06f, 0.42f), position: new(0, -0.35f, 0), color: Rgb(180, 185, 195));
                // Этикетка
                new Entity(parent: root, model: "cube", scale: new(0.40f, 0.22f, 0.01f), position: new(0, 0.02f, 0.20f), color: Rgb(255, 255, 255, 180));
                // Внутренняя жидкость (свечение)
                new Entity(parent: root, model: "sphere", scale: new(0.28f, 0.45f, 0.28f), position: new(0, -0.05f, 0), color: Color.White, unlit: true);
            }
            else if (itemKey is "bison_steak" or "monster_tooth" or "leeching_seed" or "alien_head")
            {
                // Основной объект
                new Entity(parent: root, model: "sphere", scale: new(0.62f, 0.52f, 0.48f), color: itemCol);
                // Рельеф поверхности
                new Entity(parent: root, model: "sphere", scale: new(0.55f, 0.42f, 0.52f), position: new(0, 0.02f, 0.02f), color: itemCol);
                // Глаза
                new Entity(parent: root, model: "sphere", scaleUniform: 0.12f, position: new(-0.14f, 0.08f, 0.22f), color: Color.White);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.22f, position: new(-0.12f, 0.05f, 0.24f), color: Color.Black, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.12f, position: new(0.14f, 0.08f, 0.22f), color: Color.White);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.22f, position: new(0.12f, 0.05f, 0.24f), color: Color.Black, unlit: true);
                // Зрачки-блики
                new Entity(parent: root, model: "sphere", scaleUniform: 0.04f, position: new(-0.10f, 0.10f, 0.28f), color: Color.White, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.04f, position: new(0.14f, 0.10f, 0.28f), color: Color.White, unlit: true);
            }
            else if (itemKey is "medkit" or "repulsion_armor" or "rose_buckler")
            {
                // Коробка аптечки / пластина брони
                new Entity(parent: root, model: "cube", scale: new(0.64f, 0.52f, 0.24f), color: Rgb(235, 240, 250));
                // Рамка
                new Entity(parent: root, model: "cube", scale: new(0.66f, 0.04f, 0.26f), position: new(0, 0.26f, 0), color: Rgb(200, 208, 220));
                new Entity(parent: root, model: "cube", scale: new(0.66f, 0.04f, 0.26f), position: new(0, -0.26f, 0), color: Rgb(200, 208, 220));
                // Крест
                new Entity(parent: root, model: "cube", scale: new(0.42f, 0.14f, 0.28f), color: itemCol, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.14f, 0.42f, 0.28f), color: itemCol, unlit: true);
                // Застёжка
                new Entity(parent: root, model: "cube", scale: new(0.12f, 0.08f, 0.28f), position: new(0.28f, 0, 0), color: Rgb(160, 168, 180));
                // Петля
                new Entity(parent: root, model: "cube", scale: new(0.08f, 0.16f, 0.06f), position: new(-0.34f, 0, 0), color: Rgb(150, 158, 170));
            }
            else if (itemKey is "topaz_brooch" or "opal" or "focus_crystal" or "chronobauble" or "frost_relic" or "razorwire")
            {
                // Огранённый кристалл
                new Entity(parent: root, model: "diamond", scale: new(0.58f, 0.78f, 0.58f), color: itemCol, unlit: true);
                // Верхняя грань
                new Entity(parent: root, model: "diamond", scale: new(0.36f, 0.22f, 0.36f), position: new(0, 0.38f, 0), color: Color.White, unlit: true);
                // Золотая оправа — кольцо
                new Entity(parent: root, model: "diamond", scale: new(0.82f, 0.16f, 0.82f), color: Color.Gold, unlit: true);
                // Нижняя подвеска
                new Entity(parent: root, model: "diamond", scale: new(0.14f, 0.22f, 0.14f), position: new(0, -0.48f, 0), color: Color.Gold, unlit: true);
                // Боковые грани
                foreach (float fx in new[] { -0.28f, 0.28f })
                    new Entity(parent: root, model: "diamond", scale: new(0.12f, 0.32f, 0.18f), position: new(fx, 0.08f, 0), color: itemCol, unlit: true);
            }
            else if (itemKey is "kjaro_band" or "runald_band")
            {
                // Кольцо — основное
                new Entity(parent: root, model: "diamond", scale: new(0.72f, 0.22f, 0.72f), color: itemCol, unlit: true);
                // Второе кольцо — внутреннее
                new Entity(parent: root, model: "diamond", scale: new(0.52f, 0.18f, 0.52f), position: new(0, 0.02f, 0), color: Rgb(255, 255, 255, 180), unlit: true);
                // Камень наверху
                new Entity(parent: root, model: "sphere", scaleUniform: 0.32f, position: new(0, 0.22f, 0), color: Color.White, unlit: true);
                // Свечение камня
                new Entity(parent: root, model: "sphere", scaleUniform: 0.42f, position: new(0, 0.22f, 0), color: Rgb(itemCol.R, itemCol.G, itemCol.B, 85), unlit: true);
                // Руны на кольце
                foreach (float ra in new[] { 0f, 90f, 180f, 270f })
                {
                    float rx = MathF.Cos(ra * MathF.PI / 180f) * 0.32f;
                    float rz = MathF.Sin(ra * MathF.PI / 180f) * 0.32f;
                    new Entity(parent: root, model: "diamond", scale: new(0.06f, 0.12f, 0.06f), position: new(rx, 0, rz), color: itemCol, unlit: true);
                }
            }
            else if (itemKey is "atg_missile" or "bundle_fireworks" or "behemoth" or "sticky_bomb" or "stun_grenade")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 25));
                // Корпус ракеты / бомбы
                new Entity(parent: tilt, model: "cube", scale: new(0.32f, 0.72f, 0.32f), color: itemCol, unlit: true);
                // Обтекатель (нос)
                new Entity(parent: tilt, model: "diamond", scale: new(0.36f, 0.36f, 0.36f), position: new(0, 0.48f, 0), color: Color.Yellow, unlit: true);
                // Стабилизаторы (крылышки)
                foreach (float sx in new[] { -0.18f, 0.18f })
                {
                    new Entity(parent: tilt, model: "cube", scale: new(0.22f, 0.14f, 0.04f), position: new(sx, -0.28f, 0), color: Rgb(80, 85, 95));
                }
                foreach (float sz in new[] { -0.18f, 0.18f })
                {
                    new Entity(parent: tilt, model: "cube", scale: new(0.04f, 0.14f, 0.22f), position: new(0, -0.28f, sz), color: Rgb(80, 85, 95));
                }
                // Сопло
                new Entity(parent: tilt, model: "cube", scale: new(0.24f, 0.08f, 0.24f), position: new(0, -0.38f, 0), color: Rgb(55, 60, 70));
                // Маркировочные полосы
                new Entity(parent: tilt, model: "cube", scale: new(0.34f, 0.04f, 0.34f), position: new(0, 0.12f, 0), color: Rgb(255, 255, 255, 150));
                new Entity(parent: tilt, model: "cube", scale: new(0.34f, 0.04f, 0.34f), position: new(0, -0.08f, 0), color: Rgb(255, 255, 255, 150));
            }
            else
            {
                // Дефолт: парящий кристалл с оправой
                new Entity(parent: root, model: "diamond", scale: new(0.55f, 0.72f, 0.55f), color: itemCol, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.38f, color: Color.White, unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.72f, 0.12f, 0.72f), color: Rgb(itemCol.R, itemCol.G, itemCol.B, 120), unlit: true);
            }

            return root;
        }
    }
}