using System;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Models
{
    public static partial class ItemBuilder
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
                new Entity(parent: tilt, model: "cube", scale: new(0.28f, 0.75f, 0.28f), color: Rgb(220, 235, 250, 175), unlit: true);
                // Внутренняя жидкость
                new Entity(parent: tilt, model: "cube", scale: new(0.21f, 0.54f, 0.21f), position: new(0, -0.04f, 0), color: Rgb(255, 230, 45), unlit: true);
                // Мерные линии на корпусе
                foreach (float my in new[] { -0.18f, -0.05f, 0.08f, 0.21f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.31f, 0.02f, 0.31f), position: new(0, my, 0), color: Rgb(140, 155, 175));
                // Металлический переходник и игла
                new Entity(parent: tilt, model: "cube", scale: new(0.14f, 0.08f, 0.14f), position: new(0, 0.41f, 0), color: Rgb(175, 185, 198));
                new Entity(parent: tilt, model: "cube", scale: new(0.045f, 0.42f, 0.045f), position: new(0, 0.62f, 0), color: Color.LightGray);
                new Entity(parent: tilt, model: "diamond", scale: new(0.05f, 0.14f, 0.05f), position: new(0, 0.84f, 0), color: Color.White, unlit: true);
                // Капля стимулятора на острие
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.06f, position: new(0, 0.90f, 0), color: Rgb(255, 245, 95), unlit: true);
                // Поршень
                new Entity(parent: tilt, model: "cube", scale: new(0.42f, 0.07f, 0.42f), position: new(0, -0.41f, 0), color: Color.Gray);
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.22f, 0.10f), position: new(0, -0.55f, 0), color: Rgb(120, 128, 140));
                new Entity(parent: tilt, model: "cube", scale: new(0.32f, 0.06f, 0.32f), position: new(0, -0.68f, 0), color: Rgb(100, 108, 120));
            }
            else if (itemKey == "hoof")
            {
                // Нога — голень
                new Entity(parent: root, model: "cube", scale: new(0.26f, 0.65f, 0.26f), position: new(0, 0.15f, 0), rotation: new(0, 0, -15), color: Rgb(235, 240, 250));
                // Сустав колена
                new Entity(parent: root, model: "sphere", scale: new(0.30f, 0.26f, 0.30f), position: new(-0.04f, 0.44f, 0), color: Rgb(200, 210, 230));
                // Раздвоенное копыто
                new Entity(parent: root, model: "diamond", scale: new(0.44f, 0.35f, 0.55f), position: new(0.10f, -0.22f, 0.08f), color: Color.SkyBlue, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.05f, 0.16f, 0.24f), position: new(0.10f, -0.28f, 0.26f), color: Rgb(35, 65, 95));
                // Подкова — нижняя часть копыта
                new Entity(parent: root, model: "cube", scale: new(0.40f, 0.08f, 0.50f), position: new(0.10f, -0.39f, 0.08f), color: Rgb(180, 195, 220));
                // Свечение скорости
                new Entity(parent: root, model: "diamond", scale: new(0.54f, 0.20f, 0.64f), position: new(0.10f, -0.22f, 0.08f), color: Rgb(120, 220, 255, 95), unlit: true);
            }
            else if (itemKey == "bear")
            {
                Color brown = Rgb(215, 130, 55);
                Color innerEar = Rgb(235, 175, 115);
                // Тело плюшевого мишки
                new Entity(parent: root, model: "sphere", scaleUniform: 0.52f, position: new(0, -0.12f, 0), color: brown);
                new Entity(parent: root, model: "sphere", scale: new(0.38f, 0.42f, 0.28f), position: new(0, -0.15f, 0.14f), color: innerEar);
                // Заплатка на животике
                new Entity(parent: root, model: "cube", scale: new(0.13f, 0.13f, 0.04f), position: new(0.09f, -0.16f, 0.26f), rotation: new(0, 0, 18), color: Rgb(180, 98, 40));
                // Голова
                var head = new Entity(parent: root, model: "sphere", scaleUniform: 0.42f, position: new(0, 0.28f, 0), color: brown);
                foreach (float ex in new[] { -0.38f, 0.38f })
                {
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.38f, position: new(ex, 0.38f, 0), color: brown);
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.22f, position: new(ex, 0.38f, 0.08f), color: innerEar);
                }
                new Entity(parent: head, model: "sphere", scaleUniform: 0.28f, position: new(0, -0.05f, 0.42f), color: Rgb(245, 210, 170));
                // Левый глаз-пуговка, правый — зашитый крестик X
                new Entity(parent: head, model: "sphere", scaleUniform: 0.09f, position: new(-0.13f, 0.08f, 0.36f), color: Rgb(25, 20, 20));
                new Entity(parent: head, model: "cube", scale: new(0.11f, 0.025f, 0.03f), position: new(0.13f, 0.08f, 0.37f), rotation: new(0, 0, 45), color: Rgb(30, 22, 22));
                new Entity(parent: head, model: "cube", scale: new(0.11f, 0.025f, 0.03f), position: new(0.13f, 0.08f, 0.40f), rotation: new(0, 0, -45), color: Rgb(30, 22, 22));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.08f, position: new(0, -0.02f, 0.48f), color: Rgb(35, 30, 30));
                foreach (float lx in new[] { -0.22f, 0.22f })
                {
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.16f, position: new(lx, -0.52f, 0.12f), color: brown);
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.14f, position: new(lx, -0.18f, 0.32f), color: brown);
                }
            }
            else if (itemKey == "safer_spaces")
            {
                Color voidFur = Rgb(125, 48, 175);
                Color voidBelly = Rgb(195, 115, 245);
                // Искажённый мишка Бездны внутри пространственного пузыря
                new Entity(parent: root, model: "sphere", scaleUniform: 0.46f, position: new(0, -0.12f, 0), color: voidFur);
                new Entity(parent: root, model: "sphere", scale: new(0.32f, 0.36f, 0.24f), position: new(0, -0.14f, 0.13f), color: voidBelly);
                var head = new Entity(parent: root, model: "sphere", scaleUniform: 0.38f, position: new(0, 0.24f, 0), color: voidFur);
                foreach (float ex in new[] { -0.36f, 0.36f })
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.34f, position: new(ex, 0.36f, 0), color: voidFur);
                foreach (var (gx, gy, gz) in new[] { (-0.13f, 0.06f, 0.36f), (0.13f, 0.06f, 0.36f), (0f, 0.22f, 0.34f) })
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.09f, position: new(gx, gy, gz), color: Rgb(255, 145, 255), unlit: true);
                foreach (float lx in new[] { -0.20f, 0.20f })
                {
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.14f, position: new(lx, -0.46f, 0.10f), color: voidFur);
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.12f, position: new(lx, -0.16f, 0.28f), color: voidFur);
                }
                new Entity(parent: root, model: "diamond", scale: new(0.96f, 0.07f, 0.96f), rotation: new(18, 0, -22), color: Rgb(235, 120, 255), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.88f, color: Rgb(185, 75, 255, 88), unlit: true);
            }
            else if (itemKey == "ukulele")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -30));
                Color bodyC = Rgb(210, 125, 45);
                Color detailC = Rgb(240, 165, 75);
                new Entity(parent: tilt, model: "sphere", scale: new(0.48f, 0.55f, 0.18f), position: new(0, -0.18f, 0), color: bodyC);
                new Entity(parent: tilt, model: "sphere", scale: new(0.18f, 0.18f, 0.22f), position: new(0, -0.12f, 0), color: Rgb(42, 24, 14));
                new Entity(parent: tilt, model: "sphere", scale: new(0.36f, 0.40f, 0.20f), position: new(0, 0.05f, 0), color: bodyC);
                new Entity(parent: tilt, model: "cube", scale: new(0.16f, 0.04f, 0.12f), position: new(0, -0.32f, 0.06f), color: Rgb(95, 52, 22));
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.65f, 0.08f), position: new(0, 0.42f, 0), color: Rgb(110, 65, 25));
                foreach (float fy in new[] { 0.18f, 0.30f, 0.42f, 0.54f, 0.64f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.11f, 0.015f, 0.095f), position: new(0, fy, 0), color: Rgb(200, 200, 210));
                foreach (float sx in new[] { -0.025f, 0.0f, 0.025f, 0.05f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.006f, 0.85f, 0.006f), position: new(sx - 0.012f, 0.15f, 0.10f), color: detailC, unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.14f, 0.12f, 0.10f), position: new(0, 0.78f, 0), color: Rgb(90, 50, 18));
                foreach (float kx in new[] { -0.07f, 0.07f })
                    new Entity(parent: tilt, model: "sphere", scaleUniform: 0.04f, position: new(kx, 0.76f, 0.03f), color: Color.Gold, unlit: true);
                // Электрическая дуга вокруг деки
                new Entity(parent: tilt, model: "diamond", scale: new(0.06f, 0.28f, 0.06f), position: new(0.26f, -0.06f, 0.08f), rotation: new(0, 0, -28), color: Rgb(115, 235, 255), unlit: true);
            }
            else if (itemKey == "polylute")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -26));
                // Полилютня Бездны — многорукий хитиновый симбионт-лютня с глазами Пустоты
                new Entity(parent: tilt, model: "sphere", scale: new(0.54f, 0.60f, 0.24f), position: new(0, -0.16f, 0), color: Rgb(95, 32, 145));
                new Entity(parent: tilt, model: "diamond", scale: new(0.42f, 0.44f, 0.26f), position: new(0, 0.06f, 0), color: Rgb(125, 45, 185));
                new Entity(parent: tilt, model: "sphere", scale: new(0.22f, 0.22f, 0.28f), position: new(0, -0.10f, 0), color: Rgb(235, 75, 255), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.08f, 0.62f, 0.08f), position: new(-0.05f, 0.42f, 0), rotation: new(0, 0, 6), color: Rgb(68, 22, 105));
                new Entity(parent: tilt, model: "cube", scale: new(0.07f, 0.56f, 0.07f), position: new(0.06f, 0.40f, 0.02f), rotation: new(0, 0, -8), color: Rgb(78, 28, 118));
                foreach (float sx in new[] { -0.045f, -0.015f, 0.015f, 0.045f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.008f, 0.82f, 0.008f), position: new(sx, 0.16f, 0.13f), color: Rgb(255, 145, 255), unlit: true);
                foreach (float side in new[] { -1f, 1f })
                {
                    new Entity(parent: tilt, model: "diamond", scale: new(0.10f, 0.34f, 0.08f), position: new(side * 0.26f, -0.12f, 0), rotation: new(0, 0, side * -38f), color: Rgb(185, 65, 240), unlit: true);
                    new Entity(parent: tilt, model: "sphere", scaleUniform: 0.07f, position: new(side * 0.15f, 0.08f, 0.12f), color: Rgb(120, 245, 255), unlit: true);
                }
            }
            else if (itemKey == "gasoline")
            {
                new Entity(parent: root, model: "cube", scale: new(0.52f, 0.65f, 0.32f), position: new(0, -0.05f, 0), color: Color.Red);
                foreach (float gy in new[] { -0.22f, -0.08f, 0.06f, 0.20f })
                    new Entity(parent: root, model: "cube", scale: new(0.54f, 0.04f, 0.34f), position: new(0, gy, 0), color: Rgb(200, 30, 30));
                new Entity(parent: root, model: "cube", scale: new(0.32f, 0.14f, 0.16f), position: new(0, 0.34f, 0), color: Rgb(180, 30, 30));
                new Entity(parent: root, model: "cube", scale: new(0.18f, 0.08f, 0.12f), position: new(-0.08f, 0.43f, 0), color: Rgb(60, 60, 65));
                new Entity(parent: root, model: "cube", scale: new(0.10f, 0.28f, 0.10f), position: new(0.18f, 0.38f, 0), rotation: new(0, 0, -35), color: Color.Yellow, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.06f, position: new(0.30f, 0.52f, 0), color: Rgb(255, 180, 35), unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.28f, 0.06f, 0.10f), position: new(-0.04f, 0.38f, 0), color: Rgb(50, 50, 55));
                new Entity(parent: root, model: "cube", scale: new(0.36f, 0.18f, 0.02f), position: new(0, -0.02f, 0.17f), color: Rgb(255, 230, 50));
                new Entity(parent: root, model: "diamond", scale: new(0.12f, 0.12f, 0.02f), position: new(0, -0.02f, 0.19f), color: Rgb(35, 35, 40));
            }
            else if (itemKey == "lens")
            {
                new Entity(parent: root, model: "cube", scale: new(0.75f, 0.08f, 0.10f), position: new(0, 0.10f, 0), color: Rgb(40, 45, 55));
                new Entity(parent: root, model: "cube", scale: new(0.08f, 0.06f, 0.12f), position: new(0, 0.04f, 0.04f), color: Rgb(50, 55, 65));
                new Entity(parent: root, model: "sphere", scale: new(0.34f, 0.30f, 0.06f), position: new(-0.20f, 0.04f, 0.02f), color: Rgb(55, 60, 72));
                new Entity(parent: root, model: "sphere", scale: new(0.30f, 0.26f, 0.06f), position: new(-0.20f, 0.04f, 0.05f), color: Rgb(255, 35, 75), unlit: true);
                new Entity(parent: root, model: "sphere", scale: new(0.34f, 0.30f, 0.06f), position: new(0.20f, 0.04f, 0.02f), color: Rgb(55, 60, 72));
                new Entity(parent: root, model: "sphere", scale: new(0.30f, 0.26f, 0.06f), position: new(0.20f, 0.04f, 0.05f), color: Rgb(255, 35, 75), unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.04f, 0.06f, 0.28f), position: new(-0.38f, 0.10f, -0.10f), color: Rgb(40, 45, 55));
                new Entity(parent: root, model: "cube", scale: new(0.04f, 0.06f, 0.28f), position: new(0.38f, 0.10f, -0.10f), color: Rgb(40, 45, 55));
                new Entity(parent: root, model: "sphere", scaleUniform: 0.06f, position: new(-0.14f, 0.10f, 0.08f), color: Color.White, unlit: true);
            }
            else if (itemKey == "feather")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 25));
                new Entity(parent: tilt, model: "cube", scale: new(0.05f, 0.85f, 0.05f), color: Color.Gold, unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.28f, 0.68f, 0.08f), position: new(-0.10f, 0.12f, 0), color: Rgb(90, 255, 150), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.22f, 0.62f, 0.07f), position: new(0.10f, 0.14f, 0), color: Rgb(70, 235, 130), unlit: true);
                new Entity(parent: tilt, model: "sphere", scale: new(0.12f, 0.18f, 0.10f), position: new(0, -0.38f, 0), color: Rgb(255, 250, 220), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.10f, 0.22f, 0.05f), position: new(0, 0.52f, 0), color: Rgb(120, 255, 180), unlit: true);
            }
            else if (itemKey == "bungus")
            {
                new Entity(parent: root, model: "cube", scale: new(0.24f, 0.48f, 0.24f), position: new(0, -0.15f, 0), color: Rgb(230, 240, 225));
                new Entity(parent: root, model: "cube", scale: new(0.18f, 0.12f, 0.18f), position: new(0, -0.38f, 0), color: Rgb(200, 210, 195));
                new Entity(parent: root, model: "diamond", scale: new(0.38f, 0.12f, 0.38f), position: new(0, 0.02f, 0), color: Rgb(240, 248, 235));
                new Entity(parent: root, model: "sphere", scale: new(0.75f, 0.42f, 0.75f), position: new(0, 0.18f, 0), color: Rgb(85, 245, 120), unlit: true);
                foreach (var (sx, sz) in new[] { (-0.18f, 0.15f), (0.18f, -0.15f), (0.12f, 0.18f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.14f, position: new(sx, 0.32f, sz), color: Color.SkyBlue, unlit: true);
                foreach (var (sx, sz) in new[] { (-0.08f, -0.20f), (0.22f, 0.08f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.10f, position: new(sx, 0.35f, sz), color: Rgb(145, 255, 185), unlit: true);
                foreach (var (sx, sy, sz) in new[] { (-0.28f, 0.42f, 0.12f), (0.24f, 0.48f, -0.08f), (0.05f, 0.52f, 0.22f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.04f, position: new(sx, sy, sz), color: Rgb(180, 255, 200, 160), unlit: true);
            }
            else if (itemKey == "tarrasque")
            {
                new Entity(parent: root, model: "diamond", scale: new(0.65f, 0.78f, 0.55f), color: Rgb(235, 35, 60), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.45f, position: new(-0.14f, 0.18f, 0), color: Rgb(255, 85, 95), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.45f, position: new(0.14f, 0.18f, 0), color: Rgb(255, 85, 95), unlit: true);
                foreach (var (vr, vz) in new[] { (-20f, 0.24f), (0f, -0.24f), (20f, 0.26f) })
                    new Entity(parent: root, model: "diamond", scale: new(0.06f, 0.55f, 0.06f), position: new(0, 0, vz), rotation: new(0, 0, vr), color: Rgb(180, 20, 35), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.24f, position: new(0, -0.06f, 0.20f), color: Rgb(255, 215, 220), unlit: true);
            }
            else if (itemKey == "tesla_coil")
            {
                new Entity(parent: root, model: "cube", scale: new(0.45f, 0.12f, 0.45f), position: new(0, -0.32f, 0), color: Rgb(35, 40, 50));
                new Entity(parent: root, model: "cube", scale: new(0.52f, 0.10f, 0.52f), position: new(0, -0.22f, 0), color: Rgb(55, 65, 82));
                foreach (float ix in new[] { -0.14f, 0.14f })
                    new Entity(parent: root, model: "cube", scale: new(0.08f, 0.14f, 0.08f), position: new(ix, -0.14f, 0), color: Rgb(185, 145, 75));
                new Entity(parent: root, model: "cube", scale: new(0.18f, 0.55f, 0.18f), position: new(0, 0.05f, 0), color: Rgb(180, 120, 60));
                foreach (float cy in new[] { -0.10f, 0.0f, 0.10f, 0.20f })
                    new Entity(parent: root, model: "cube", scale: new(0.22f, 0.04f, 0.22f), position: new(0, cy, 0), color: Rgb(210, 155, 80));
                new Entity(parent: root, model: "sphere", scale: new(0.48f, 0.28f, 0.48f), position: new(0, 0.38f, 0), color: Color.SkyBlue, unlit: true);
                foreach (var (rx, ry, rz) in new[] { (-0.32f, 0.45f, 0.08f), (0.28f, 0.50f, -0.12f), (0.05f, 0.55f, 0.25f) })
                    new Entity(parent: root, model: "diamond", scale: new(0.04f, 0.18f, 0.04f), position: new(rx, ry, rz), rotation: new(0, 0, rx * 80f), color: Rgb(150, 235, 255), unlit: true);
            }
            else if (itemKey == "aegis")
            {
                new Entity(parent: root, model: "diamond", scale: new(0.72f, 0.82f, 0.08f), color: Rgb(55, 62, 78));
                new Entity(parent: root, model: "diamond", scale: new(0.68f, 0.78f, 0.12f), position: new(0, 0, 0.02f), color: Color.Gold, unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.42f, 0.52f, 0.16f), position: new(0, 0, 0.04f), color: Color.White, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.08f, 0.36f, 0.18f), position: new(0, 0, 0.06f), color: Color.Gold, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.28f, 0.08f, 0.22f), position: new(0, 0.05f, 0.06f), color: Color.Gold, unlit: true);
                foreach (var (px, py) in new[] { (-0.22f, 0.28f), (0.22f, 0.28f), (-0.28f, -0.08f), (0.28f, -0.08f), (0f, -0.32f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.05f, position: new(px, py, 0.10f), color: Rgb(255, 225, 120), unlit: true);
            }
            else if (itemKey == "shattering_justice")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 26));
                // Сокрушительное Правосудие — массивный боевой молот с раскалённым рунным оголовьем-наковальней
                new Entity(parent: tilt, model: "cube", scale: new(0.09f, 0.92f, 0.09f), position: new(0, -0.06f, 0), color: Rgb(62, 66, 78));
                foreach (float wy in new[] { -0.40f, -0.26f, -0.12f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.115f, 0.05f, 0.115f), position: new(0, wy, 0), color: Rgb(125, 72, 38));
                new Entity(parent: tilt, model: "diamond", scale: new(0.16f, 0.18f, 0.16f), position: new(0, -0.54f, 0), color: Rgb(215, 155, 55));
                // Стальная гарда под бойком
                new Entity(parent: tilt, model: "cube", scale: new(0.24f, 0.07f, 0.20f), position: new(0, 0.14f, 0), color: Rgb(95, 102, 118));
                // Массивная наковальня-боёк молота
                new Entity(parent: tilt, model: "cube", scale: new(0.68f, 0.30f, 0.32f), position: new(0, 0.33f, 0), color: Rgb(52, 56, 68));
                // Ударные скошенные грани слева и справа
                new Entity(parent: tilt, model: "cube", scale: new(0.08f, 0.34f, 0.35f), position: new(-0.35f, 0.33f, 0), color: Rgb(145, 152, 168));
                new Entity(parent: tilt, model: "diamond", scale: new(0.22f, 0.24f, 0.26f), position: new(0.38f, 0.33f, 0), rotation: new(0, 0, -90), color: Rgb(165, 172, 188));
                // Раскалённое рунное ядро и золотые полосы правосудия
                new Entity(parent: tilt, model: "diamond", scale: new(0.34f, 0.18f, 0.36f), position: new(0, 0.33f, 0), color: Rgb(255, 145, 35), unlit: true);
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.16f, position: new(0, 0.33f, 0.16f), color: Rgb(255, 235, 120), unlit: true);
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.16f, position: new(0, 0.33f, -0.16f), color: Rgb(255, 235, 120), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.54f, 0.04f, 0.34f), position: new(0, 0.46f, 0), color: Rgb(235, 175, 55), unlit: true);
            }
            else if (itemKey == "guillotine")
            {
                // Старая Гильотина — деревянно-кованная рама эшафота со скошенным серебряным лезвием
                Color woodDark = Rgb(92, 56, 34);
                Color woodMid = Rgb(122, 76, 46);
                Color ironCol = Rgb(68, 74, 86);
                // Основание эшафота и нижняя колодка (люнет)
                new Entity(parent: root, model: "cube", scale: new(0.62f, 0.10f, 0.36f), position: new(0, -0.44f, 0), color: woodDark);
                new Entity(parent: root, model: "cube", scale: new(0.40f, 0.12f, 0.18f), position: new(0, -0.33f, 0), color: woodMid);
                new Entity(parent: root, model: "sphere", scale: new(0.14f, 0.12f, 0.22f), position: new(0, -0.31f, 0), color: Rgb(28, 24, 26));
                // Две вертикальные стойки и верхняя перекладина
                foreach (float px in new[] { -0.21f, 0.21f })
                {
                    new Entity(parent: root, model: "cube", scale: new(0.08f, 0.88f, 0.10f), position: new(px, 0.04f, 0), color: woodMid);
                    new Entity(parent: root, model: "cube", scale: new(0.11f, 0.06f, 0.13f), position: new(px, 0.42f, 0), color: ironCol);
                }
                new Entity(parent: root, model: "cube", scale: new(0.58f, 0.10f, 0.15f), position: new(0, 0.49f, 0), color: woodDark);
                // Верёвка подъёмного механизма
                new Entity(parent: root, model: "cube", scale: new(0.025f, 0.28f, 0.025f), position: new(0, 0.32f, 0), color: Rgb(205, 180, 125));
                // Тяжёлый держатель и косое стальное лезвие гильотины
                new Entity(parent: root, model: "cube", scale: new(0.34f, 0.07f, 0.07f), position: new(0, 0.20f, 0), color: ironCol);
                new Entity(parent: root, model: "cube", scale: new(0.32f, 0.22f, 0.03f), position: new(0, 0.08f, 0), color: Rgb(205, 220, 240), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.34f, 0.14f, 0.04f), position: new(0, -0.02f, 0.01f), rotation: new(0, 0, -20), color: Color.White, unlit: true);
            }
            else if (itemKey == "crowbar")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 30));
                new Entity(parent: tilt, model: "cube", scale: new(0.11f, 0.85f, 0.11f), color: Rgb(210, 45, 45));
                foreach (float gy in new[] { -0.15f, -0.05f, 0.05f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.13f, 0.04f, 0.13f), position: new(0, gy, 0), color: Rgb(175, 32, 32));
                new Entity(parent: tilt, model: "cube", scale: new(0.30f, 0.11f, 0.12f), position: new(0.10f, 0.42f, 0), color: Rgb(200, 210, 225));
                new Entity(parent: tilt, model: "cube", scale: new(0.09f, 0.18f, 0.13f), position: new(0.25f, 0.36f, 0), color: Rgb(190, 200, 215));
                new Entity(parent: tilt, model: "cube", scale: new(0.15f, 0.10f, 0.12f), position: new(-0.04f, -0.46f, 0.03f), rotation: new(20, 0, 22), color: Rgb(200, 210, 225));
            }
            else if (itemKey == "tri_tip")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -35));
                // Трёхгранный Кинжал — скрученное трёхлезвийное жало с алым кровоточащим остриём
                for (int i = 0; i < 3; i++)
                {
                    float ang = i * 120f;
                    new Entity(parent: tilt, model: "diamond", scale: new(0.24f, 0.72f, 0.06f), position: new(0, 0.14f, 0),
                        rotation: new(0, ang, 0), color: Rgb(195, 205, 220));
                }
                // Алый кончик и капли крови
                new Entity(parent: tilt, model: "diamond", scale: new(0.18f, 0.34f, 0.18f), position: new(0, 0.44f, 0), color: Rgb(235, 30, 50), unlit: true);
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.07f, position: new(0.06f, 0.62f, 0), color: Rgb(255, 45, 65), unlit: true);
                // Гарда, тактическая рукоять и кольцо на навершии
                new Entity(parent: tilt, model: "cube", scale: new(0.28f, 0.06f, 0.28f), position: new(0, -0.20f, 0), color: Rgb(65, 70, 82));
                new Entity(parent: tilt, model: "cube", scale: new(0.11f, 0.28f, 0.11f), position: new(0, -0.36f, 0), color: Rgb(42, 46, 55));
                new Entity(parent: tilt, model: "diamond", scale: new(0.18f, 0.16f, 0.06f), position: new(0, -0.54f, 0), color: Rgb(155, 35, 45));
            }
            else if (itemKey == "harvester_scythe")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -24));
                // Коса Жнеца — длинное тёмное древко с изогнутым серповидным клинком и изумрудным свечением
                new Entity(parent: tilt, model: "cube", scale: new(0.07f, 1.04f, 0.07f), position: new(-0.10f, -0.04f, 0), rotation: new(0, 0, -8), color: Rgb(54, 38, 34));
                // Боковая рукоять жнеца на древке
                new Entity(parent: tilt, model: "cube", scale: new(0.18f, 0.05f, 0.06f), position: new(0.01f, -0.10f, 0.04f), rotation: new(0, 0, 15), color: Rgb(85, 58, 42));
                // Кованое крепление с изумрудным камнем души
                new Entity(parent: tilt, model: "cube", scale: new(0.14f, 0.14f, 0.13f), position: new(-0.16f, 0.44f, 0), color: Rgb(48, 56, 62));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.13f, position: new(-0.16f, 0.44f, 0.06f), color: Rgb(85, 255, 175), unlit: true);
                // Изогнутое лезвие косы (сегментированная дуга + светящаяся режущая кромка)
                new Entity(parent: tilt, model: "diamond", scale: new(0.56f, 0.16f, 0.08f), position: new(0.10f, 0.44f, 0), rotation: new(0, 0, -14), color: Rgb(195, 215, 225));
                new Entity(parent: tilt, model: "diamond", scale: new(0.42f, 0.12f, 0.07f), position: new(0.36f, 0.30f, 0), rotation: new(0, 0, -42), color: Rgb(215, 235, 240));
                new Entity(parent: tilt, model: "diamond", scale: new(0.58f, 0.09f, 0.10f), position: new(0.12f, 0.39f, 0), rotation: new(0, 0, -18), color: Rgb(75, 255, 165), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.38f, 0.07f, 0.09f), position: new(0.35f, 0.25f, 0), rotation: new(0, 0, -45), color: Rgb(140, 255, 200), unlit: true);
            }
            else if (itemKey == "ceremonial_dagger")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -30));
                // Церемониальный Кинжал — жертвенный волнистый крис с черепом/рубином и спектральными духами
                new Entity(parent: tilt, model: "diamond", scale: new(0.20f, 0.56f, 0.09f), position: new(-0.02f, 0.12f, 0), rotation: new(0, 0, 10), color: Rgb(235, 220, 225), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.15f, 0.44f, 0.08f), position: new(0.03f, 0.38f, 0), rotation: new(0, 0, -14), color: Rgb(255, 95, 130), unlit: true);
                // Резная костяная гарда-череп с рубиновыми глазницами
                new Entity(parent: tilt, model: "diamond", scale: new(0.46f, 0.14f, 0.14f), position: new(0, -0.14f, 0), color: Rgb(215, 165, 65));
                new Entity(parent: tilt, model: "sphere", scale: new(0.20f, 0.20f, 0.18f), position: new(0, -0.13f, 0.05f), color: Rgb(238, 228, 210));
                foreach (float ex in new[] { -0.05f, 0.05f })
                    new Entity(parent: tilt, model: "sphere", scaleUniform: 0.055f, position: new(ex, -0.12f, 0.13f), color: Rgb(255, 35, 75), unlit: true);
                // Золочёная витая рукоять и рубин в навершии
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.28f, 0.10f), position: new(0, -0.32f, 0), color: Rgb(125, 32, 52));
                new Entity(parent: tilt, model: "diamond", scale: new(0.16f, 0.16f, 0.16f), position: new(0, -0.49f, 0), color: Rgb(255, 55, 95), unlit: true);
                // Парящие спектральные кинжалы-огоньки вокруг
                foreach (var (wx, wy, wz) in new[] { (-0.28f, 0.22f, 0.10f), (0.28f, 0.06f, -0.08f), (-0.18f, 0.48f, -0.06f) })
                    new Entity(parent: root, model: "diamond", scale: new(0.07f, 0.22f, 0.07f), position: new(wx, wy, wz), rotation: new(0, 0, -28), color: Rgb(255, 85, 135, 195), unlit: true);
            }
            else if (itemKey == "ap_rounds")
            {
                // Бронебойные Патроны — обойма с тремя крупнокалиберными латунными патронами с вольфрамовыми сердечниками
                new Entity(parent: root, model: "cube", scale: new(0.56f, 0.14f, 0.22f), position: new(0, -0.32f, 0), color: Rgb(62, 74, 58));
                new Entity(parent: root, model: "cube", scale: new(0.58f, 0.05f, 0.24f), position: new(0, -0.24f, 0), color: Rgb(95, 105, 88));
                foreach (var (bx, byOff) in new[] { (-0.17f, -0.02f), (0.0f, 0.04f), (0.17f, -0.02f) })
                {
                    // Латунная гильза
                    new Entity(parent: root, model: "cube", scale: new(0.12f, 0.42f, 0.12f), position: new(bx, -0.04f + byOff, 0), color: Rgb(225, 175, 65));
                    // Сужение дульца
                    new Entity(parent: root, model: "cube", scale: new(0.10f, 0.08f, 0.10f), position: new(bx, 0.20f + byOff, 0), color: Rgb(205, 155, 50));
                    // Бронебойная пуля с чёрной полосой и раскалённым оранжевым носиком
                    new Entity(parent: root, model: "diamond", scale: new(0.11f, 0.28f, 0.11f), position: new(bx, 0.32f + byOff, 0), color: Rgb(42, 46, 54));
                    new Entity(parent: root, model: "diamond", scale: new(0.07f, 0.15f, 0.07f), position: new(bx, 0.42f + byOff, 0), color: Rgb(255, 155, 45), unlit: true);
                }
            }
            else if (itemKey == "backup_mag")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -14));
                // Запасной Магазин — спарка из двух изогнутых автоматных магазинов со стяжкой
                foreach (var (mx, my, mz, rotZ) in new[] { (-0.12f, -0.02f, -0.03f, 8f), (0.12f, 0.04f, 0.03f, 8f) })
                {
                    var mag = new Entity(parent: tilt, position: new(mx, my, mz), rotation: new(0, 0, rotZ));
                    new Entity(parent: mag, model: "cube", scale: new(0.20f, 0.62f, 0.28f), color: Rgb(62, 72, 88));
                    foreach (float ry in new[] { -0.18f, 0.0f, 0.18f })
                        new Entity(parent: mag, model: "cube", scale: new(0.22f, 0.035f, 0.24f), position: new(0, ry, 0), color: Rgb(46, 54, 68));
                    new Entity(parent: mag, model: "cube", scale: new(0.22f, 0.06f, 0.30f), position: new(0, -0.32f, 0), color: Rgb(38, 44, 56));
                    // Видимые латунные патроны сверху в губках магазина
                    new Entity(parent: mag, model: "sphere", scale: new(0.14f, 0.10f, 0.24f), position: new(0, 0.33f, 0.01f), color: Rgb(240, 185, 65), unlit: true);
                }
                // Синяя тактическая изолента-стяжка посередине спарки
                new Entity(parent: tilt, model: "cube", scale: new(0.50f, 0.11f, 0.36f), position: new(0, -0.04f, 0), rotation: new(0, 0, 8), color: Rgb(65, 155, 235), unlit: true);
            }
            else if (itemKey == "bandolier")
            {
                // Патронташ — перекрещённая кожаная пулемётная лента с латунными гильзами и пряжкой
                var belt = new Entity(parent: root, rotation: new(16, 0, -32));
                new Entity(parent: belt, model: "diamond", scale: new(0.86f, 0.22f, 0.56f), color: Rgb(115, 68, 36));
                new Entity(parent: belt, model: "diamond", scale: new(0.62f, 0.26f, 0.38f), color: Rgb(28, 22, 20));
                // Пряжка ремня
                new Entity(parent: belt, model: "cube", scale: new(0.16f, 0.24f, 0.08f), position: new(0, 0.02f, 0.28f), color: Rgb(225, 185, 75), unlit: true);
                // Патроны по периметру кожаной ленты
                for (int i = 0; i < 6; i++)
                {
                    float ang = (i / 6f) * MathF.PI * 2f + 0.35f;
                    float px = MathF.Cos(ang) * 0.36f;
                    float pz = MathF.Sin(ang) * 0.24f;
                    new Entity(parent: belt, model: "cube", scale: new(0.07f, 0.24f, 0.07f), position: new(px, 0.02f, pz), color: Rgb(235, 190, 75), unlit: true);
                    new Entity(parent: belt, model: "diamond", scale: new(0.07f, 0.12f, 0.07f), position: new(px, 0.17f, pz), color: Rgb(215, 105, 55), unlit: true);
                }
            }
            else if (itemKey == "afterburner")
            {
                // Твердосветовой Форсаж — двухсопловый реактивный ранец с энергетическими крыльями и пламенем
                new Entity(parent: root, model: "cube", scale: new(0.32f, 0.42f, 0.22f), position: new(0, 0.06f, 0), color: Rgb(58, 66, 82));
                new Entity(parent: root, model: "sphere", scaleUniform: 0.18f, position: new(0, 0.10f, 0.10f), color: Rgb(110, 230, 255), unlit: true);
                foreach (float sx in new[] { -1f, 1f })
                {
                    var pod = new Entity(parent: root, position: new(sx * 0.24f, 0.04f, 0), rotation: new(0, 0, sx * -10f));
                    // Титановая турбина
                    new Entity(parent: pod, model: "cube", scale: new(0.20f, 0.56f, 0.24f), color: Rgb(215, 222, 235));
                    new Entity(parent: pod, model: "cube", scale: new(0.22f, 0.10f, 0.26f), position: new(0, 0.20f, 0), color: Rgb(245, 95, 45));
                    new Entity(parent: pod, model: "cube", scale: new(0.16f, 0.10f, 0.18f), position: new(0, -0.30f, 0), color: Rgb(42, 46, 56));
                    // Реактивная струя пламени из сопла
                    new Entity(parent: pod, model: "diamond", scale: new(0.18f, 0.36f, 0.18f), position: new(0, -0.46f, 0), color: Rgb(255, 115, 35), unlit: true);
                    new Entity(parent: pod, model: "diamond", scale: new(0.10f, 0.22f, 0.10f), position: new(0, -0.42f, 0), color: Rgb(255, 235, 115), unlit: true);
                    // Твердосветовые стабилизаторы
                    new Entity(parent: pod, model: "diamond", scale: new(0.26f, 0.34f, 0.05f), position: new(sx * 0.16f, 0.08f, -0.02f),
                        rotation: new(0, 0, sx * -28f), color: Rgb(255, 145, 65, 185), unlit: true);
                }
            }
            else if (itemKey == "lysate_cell")
            {
                var tilt = new Entity(parent: root, rotation: new(10, 0, -16));
                // Клетка Бездны — заражённая топливная ячейка в хитиновой клетке со щупальцами Пустоты
                new Entity(parent: tilt, model: "cube", scale: new(0.38f, 0.10f, 0.38f), position: new(0, 0.34f, 0), color: Rgb(55, 28, 82));
                new Entity(parent: tilt, model: "cube", scale: new(0.38f, 0.10f, 0.38f), position: new(0, -0.34f, 0), color: Rgb(55, 28, 82));
                // Пульсирующее плазменное ядро Бездны и полупрозрачная колба
                new Entity(parent: tilt, model: "sphere", scale: new(0.26f, 0.48f, 0.26f), color: Rgb(245, 115, 255), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.32f, 0.58f, 0.32f), color: Rgb(155, 55, 235, 95), unlit: true);
                // Четыре изогнутых ребра-клетки и органические отростки
                foreach (var (rx, rz) in new[] { (-0.17f, -0.17f), (0.17f, -0.17f), (-0.17f, 0.17f), (0.17f, 0.17f) })
                    new Entity(parent: tilt, model: "cube", scale: new(0.05f, 0.66f, 0.05f), position: new(rx, 0, rz), color: Rgb(78, 36, 112));
                foreach (var (tx, ty, rotZ) in new[] { (-0.24f, 0.18f, 38f), (0.25f, -0.10f, -42f), (0.10f, 0.46f, -24f) })
                    new Entity(parent: tilt, model: "diamond", scale: new(0.08f, 0.28f, 0.08f), position: new(tx, ty, 0.06f), rotation: new(0, 0, rotZ), color: Rgb(215, 85, 255), unlit: true);
            }
            else if (itemKey == "energy_drink")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -12));
                // Энергетик — алюминиевая банка со значком молнии и открывашкой
                new Entity(parent: tilt, model: "cube", scale: new(0.36f, 0.66f, 0.36f), color: Rgb(35, 175, 245), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.38f, 0.12f, 0.38f), position: new(0, -0.14f, 0), color: Rgb(22, 95, 185));
                // Верхний и нижний алюминиевые ободки + ключ-кольцо на крышке
                new Entity(parent: tilt, model: "cube", scale: new(0.34f, 0.06f, 0.34f), position: new(0, 0.35f, 0), color: Rgb(215, 222, 232));
                new Entity(parent: tilt, model: "cube", scale: new(0.12f, 0.03f, 0.07f), position: new(0.04f, 0.39f, 0), rotation: new(0, 20, 12), color: Color.White);
                new Entity(parent: tilt, model: "cube", scale: new(0.34f, 0.05f, 0.34f), position: new(0, -0.35f, 0), color: Rgb(185, 195, 208));
                // Эмблема молнии на фасаде и пузырьки газа
                new Entity(parent: tilt, model: "diamond", scale: new(0.14f, 0.34f, 0.04f), position: new(0, 0.05f, 0.19f), rotation: new(0, 0, -22), color: Rgb(255, 240, 75), unlit: true);
                foreach (var (bx, by) in new[] { (-0.12f, 0.48f), (0.10f, 0.54f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.055f, position: new(bx, by, 0.04f), color: Rgb(150, 240, 255), unlit: true);
            }
            else if (itemKey == "will_o_wisp")
            {
                // Блуждающий Огонёк в Банке — стеклянная банка с пробковой крышкой и живым духом лавы внутри
                // Внутренний дух огня (рисуется непрозрачным/ярким внутри полупрозрачного стекла)
                new Entity(parent: root, model: "sphere", scaleUniform: 0.24f, position: new(0, -0.06f, 0), color: Rgb(255, 95, 25), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.20f, 0.38f, 0.20f), position: new(0, 0.04f, 0), color: Rgb(255, 185, 45), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.12f, position: new(0, -0.04f, 0.03f), color: Color.White, unlit: true);
                foreach (var (ex, ey, ez) in new[] { (-0.10f, 0.14f, 0.06f), (0.11f, 0.08f, -0.07f), (0.04f, -0.18f, 0.09f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.045f, position: new(ex, ey, ez), color: Rgb(255, 220, 85), unlit: true);
                // Дно банки, латунное горлышко и корковая пробка
                new Entity(parent: root, model: "cube", scale: new(0.42f, 0.05f, 0.42f), position: new(0, -0.34f, 0), color: Rgb(175, 210, 225));
                new Entity(parent: root, model: "cube", scale: new(0.34f, 0.07f, 0.34f), position: new(0, 0.31f, 0), color: Rgb(195, 145, 55));
                new Entity(parent: root, model: "cube", scale: new(0.26f, 0.11f, 0.26f), position: new(0, 0.39f, 0), color: Rgb(168, 118, 68));
                // Полупрозрачные стеклянные стенки банки
                new Entity(parent: root, model: "cube", scale: new(0.44f, 0.62f, 0.44f), position: new(0, -0.02f, 0), color: Rgb(195, 235, 250, 88), unlit: true);
            }
            else if (itemKey == "infusion")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 14));
                // Инфузия — медицинская двухкамерная колба переливания крови со стальной арматурой и капельницей
                new Entity(parent: tilt, model: "cube", scale: new(0.40f, 0.07f, 0.28f), position: new(0, 0.35f, 0), color: Rgb(185, 192, 205));
                new Entity(parent: tilt, model: "cube", scale: new(0.40f, 0.07f, 0.28f), position: new(0, -0.35f, 0), color: Rgb(185, 192, 205));
                // Боковые защитные стойки и центральная перемычка камер
                foreach (float sx in new[] { -0.19f, 0.19f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.04f, 0.66f, 0.06f), position: new(sx, 0, 0), color: Rgb(135, 142, 155));
                new Entity(parent: tilt, model: "cube", scale: new(0.36f, 0.04f, 0.26f), position: new(0, 0.02f, 0), color: Rgb(150, 158, 172));
                // Алые камеры с кровью внутри
                new Entity(parent: tilt, model: "cube", scale: new(0.28f, 0.25f, 0.18f), position: new(0, -0.17f, 0), color: Rgb(225, 25, 50), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.28f, 0.18f, 0.18f), position: new(0, 0.15f, 0), color: Rgb(245, 55, 75), unlit: true);
                // Верхний клапан и трубка капельницы
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.12f, 0.10f), position: new(-0.08f, 0.43f, 0), color: Rgb(220, 45, 60));
                new Entity(parent: tilt, model: "cube", scale: new(0.18f, 0.04f, 0.04f), position: new(0.02f, 0.48f, 0), rotation: new(0, 0, -18), color: Rgb(235, 70, 85), unlit: true);
                // Стеклянный корпус колбы
                new Entity(parent: tilt, model: "cube", scale: new(0.34f, 0.62f, 0.22f), color: Rgb(225, 240, 250, 85), unlit: true);
            }
            else if (itemKey == "plasma_shrimp")
            {
                // Плазменная Креветка — изогнутое панцирное существо Пустоты с усиками, веерным хвостом и плазма-орудием
                var segs = new (float x, float y, float s, float rotZ)[]
                {
                    (0.18f, 0.18f, 0.30f, -15f),
                    (0.02f, 0.22f, 0.28f, 10f),
                    (-0.14f, 0.14f, 0.25f, 38f),
                    (-0.24f, -0.02f, 0.21f, 68f),
                    (-0.20f, -0.18f, 0.17f, 105f)
                };
                foreach (var (sx, sy, sc, rz) in segs)
                    new Entity(parent: root, model: "sphere", scale: new(sc * 1.15f, sc * 0.92f, sc * 0.88f),
                        position: new(sx, sy, 0), rotation: new(0, 0, rz), color: Rgb(135, 52, 205));
                // Плазменная ракетная пусковая установка на спинке креветки
                new Entity(parent: root, model: "cube", scale: new(0.24f, 0.10f, 0.16f), position: new(-0.02f, 0.34f, 0), rotation: new(0, 0, 12), color: Rgb(62, 28, 95));
                new Entity(parent: root, model: "sphere", scaleUniform: 0.10f, position: new(0.10f, 0.36f, 0), color: Rgb(125, 245, 255), unlit: true);
                // Светящиеся глаза, длинные усы и веерный хвост
                foreach (float side in new[] { -1f, 1f })
                {
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.08f, position: new(0.28f, 0.22f, side * 0.10f), color: Rgb(115, 245, 255), unlit: true);
                    new Entity(parent: root, model: "cube", scale: new(0.38f, 0.02f, 0.02f), position: new(0.38f, 0.30f, side * 0.08f),
                        rotation: new(0, side * -16f, 24f), color: Rgb(235, 115, 255), unlit: true);
                }
                foreach (float tang in new[] { -26f, 0f, 26f })
                    new Entity(parent: root, model: "diamond", scale: new(0.10f, 0.24f, 0.05f),
                        position: new(-0.10f, -0.30f, tang * 0.003f), rotation: new(tang * 0.5f, 0, 125f + tang), color: Rgb(225, 95, 255), unlit: true);
            }
            else if (itemKey == "bison_steak")
            {
                var tilt = new Entity(parent: root, rotation: new(15, 0, -20));
                new Entity(parent: tilt, model: "sphere", scale: new(0.68f, 0.24f, 0.52f), color: Rgb(195, 48, 48));
                new Entity(parent: tilt, model: "sphere", scale: new(0.72f, 0.18f, 0.56f), position: new(0, -0.02f, 0), color: Rgb(235, 195, 165));
                new Entity(parent: tilt, model: "cube", scale: new(0.08f, 0.28f, 0.44f), position: new(-0.06f, 0.01f, 0), color: Rgb(245, 240, 225));
                new Entity(parent: tilt, model: "cube", scale: new(0.38f, 0.26f, 0.08f), position: new(-0.02f, 0.01f, 0.08f), color: Rgb(245, 240, 225));
                foreach (float gx in new[] { 0.08f, 0.20f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.03f, 0.26f, 0.32f), position: new(gx, 0.01f, -0.04f), color: Rgb(95, 25, 20));
            }
            else if (itemKey == "monster_tooth")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 22));
                new Entity(parent: tilt, model: "diamond", scale: new(0.28f, 0.72f, 0.24f), position: new(0, -0.04f, 0), color: Rgb(240, 238, 220));
                new Entity(parent: tilt, model: "diamond", scale: new(0.16f, 0.34f, 0.16f), position: new(0.06f, -0.34f, 0), rotation: new(0, 0, -18), color: Color.White, unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.32f, 0.14f, 0.28f), position: new(0, 0.26f, 0), color: Rgb(85, 60, 42));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.16f, position: new(0, 0.26f, 0.15f), color: Rgb(95, 255, 120), unlit: true);
            }
            else if (itemKey == "leeching_seed")
            {
                new Entity(parent: root, model: "sphere", scale: new(0.50f, 0.62f, 0.50f), color: Rgb(165, 32, 48));
                foreach (float ang in new[] { 0f, 90f, 180f, 270f })
                {
                    float r = ang * MathF.PI / 180f;
                    new Entity(parent: root, model: "diamond", scale: new(0.12f, 0.24f, 0.12f),
                        position: new(MathF.Cos(r) * 0.24f, 0, MathF.Sin(r) * 0.24f),
                        rotation: new(0, -ang, 45f), color: Rgb(235, 65, 80), unlit: true);
                }
                new Entity(parent: root, model: "diamond", scale: new(0.14f, 0.36f, 0.08f), position: new(-0.08f, 0.36f, 0), rotation: new(0, 0, 25), color: Rgb(110, 245, 95), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.14f, 0.32f, 0.08f), position: new(0.08f, 0.34f, 0), rotation: new(0, 0, -25), color: Rgb(90, 225, 80), unlit: true);
            }
            else if (itemKey == "alien_head")
            {
                new Entity(parent: root, model: "sphere", scale: new(0.58f, 0.68f, 0.52f), position: new(0, 0.06f, 0), color: Rgb(120, 235, 145));
                new Entity(parent: root, model: "diamond", scale: new(0.36f, 0.38f, 0.34f), position: new(0, -0.22f, 0.04f), color: Rgb(105, 215, 130));
                foreach (float sx in new[] { -1f, 1f })
                {
                    new Entity(parent: root, model: "sphere", scale: new(0.22f, 0.14f, 0.10f),
                        position: new(sx * 0.15f, 0.04f, 0.23f), rotation: new(0, sx * 18f, sx * -22f), color: Rgb(15, 18, 26), unlit: true);
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.05f,
                        position: new(sx * 0.13f, 0.07f, 0.28f), color: Color.White, unlit: true);
                }
                new Entity(parent: root, model: "diamond", scale: new(0.76f, 0.10f, 0.76f), position: new(0, 0.42f, 0), color: Rgb(150, 255, 185, 140), unlit: true);
            }
            else if (itemKey == "medkit")
            {
                // Аптечка — белый медицинский кейс с ручкой, защёлками и зелёным крестом
                new Entity(parent: root, model: "cube", scale: new(0.64f, 0.48f, 0.24f), position: new(0, -0.04f, 0), color: Rgb(238, 242, 250));
                new Entity(parent: root, model: "cube", scale: new(0.66f, 0.04f, 0.26f), position: new(0, -0.04f, 0), color: Rgb(190, 198, 212));
                // Ручка сверху и две металлические защёлки
                new Entity(parent: root, model: "cube", scale: new(0.24f, 0.05f, 0.08f), position: new(0, 0.26f, 0), color: Rgb(70, 78, 92));
                foreach (float hx in new[] { -0.09f, 0.09f })
                    new Entity(parent: root, model: "cube", scale: new(0.04f, 0.08f, 0.06f), position: new(hx, 0.22f, 0), color: Rgb(95, 102, 118));
                foreach (float lx in new[] { -0.20f, 0.20f })
                    new Entity(parent: root, model: "cube", scale: new(0.06f, 0.08f, 0.28f), position: new(lx, 0.16f, 0), color: Rgb(155, 165, 180));
                // Зелёный крест (разная глубина по Z исключает Z-fighting)
                new Entity(parent: root, model: "cube", scale: new(0.32f, 0.11f, 0.27f), position: new(0, -0.04f, 0), color: Rgb(70, 235, 125), unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.11f, 0.32f, 0.29f), position: new(0, -0.04f, 0), color: Rgb(70, 235, 125), unlit: true);
            }
            else if (itemKey == "repulsion_armor")
            {
                var tilt = new Entity(parent: root, rotation: new(12, -16, 10));
                // Бронепластина Отражения — скошенная титановая пластина реактивной брони с кинетическим полем
                new Entity(parent: tilt, model: "cube", scale: new(0.58f, 0.68f, 0.12f), color: Rgb(118, 132, 154));
                new Entity(parent: tilt, model: "cube", scale: new(0.46f, 0.54f, 0.16f), position: new(0, 0, 0.02f), color: Rgb(162, 178, 202));
                // Секции динамической защиты и заклёпки по углам
                foreach (float py in new[] { -0.15f, 0.15f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.38f, 0.18f, 0.19f), position: new(0, py, 0.03f), color: Rgb(192, 208, 228));
                foreach (var (rx, ry) in new[] { (-0.23f, -0.27f), (0.23f, -0.27f), (-0.23f, 0.27f), (0.23f, 0.27f) })
                    new Entity(parent: tilt, model: "sphere", scaleUniform: 0.06f, position: new(rx, ry, 0.07f), color: Rgb(72, 82, 98));
                // Кинетический контур отражения
                new Entity(parent: tilt, model: "diamond", scale: new(0.24f, 0.32f, 0.22f), position: new(0, 0, 0.04f), color: Rgb(115, 220, 255), unlit: true);
            }
            else if (itemKey == "rose_buckler")
            {
                // Розовый Баклер — круглый бронзовый кулачный щит с рельефным бутоном розы в центре
                new Entity(parent: root, model: "sphere", scale: new(0.72f, 0.72f, 0.16f), color: Rgb(195, 148, 78));
                new Entity(parent: root, model: "sphere", scale: new(0.58f, 0.58f, 0.20f), position: new(0, 0, 0.02f), color: Rgb(135, 42, 68));
                for (int i = 0; i < 5; i++)
                {
                    float ang = (i / 5f) * MathF.PI * 2f;
                    new Entity(parent: root, model: "sphere", scale: new(0.22f, 0.22f, 0.14f),
                        position: new(MathF.Cos(ang) * 0.11f, MathF.Sin(ang) * 0.11f, 0.09f), color: Rgb(235, 85, 135), unlit: true);
                }
                new Entity(parent: root, model: "diamond", scale: new(0.20f, 0.20f, 0.18f), position: new(0, 0, 0.12f), color: Rgb(255, 175, 205), unlit: true);
            }
            else if (itemKey == "topaz_brooch")
            {
                // Топазовая Брошь — золотая овальная филигранная брошь с гранёным янтарным топазом и булавкой
                new Entity(parent: root, model: "sphere", scale: new(0.62f, 0.74f, 0.14f), color: Rgb(225, 175, 50));
                new Entity(parent: root, model: "sphere", scale: new(0.48f, 0.60f, 0.18f), position: new(0, 0, 0.02f), color: Rgb(145, 95, 25));
                // Гранёный солнечный топаз в центре и блик
                new Entity(parent: root, model: "diamond", scale: new(0.42f, 0.56f, 0.24f), position: new(0, 0, 0.06f), color: Rgb(255, 195, 35), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.22f, 0.30f, 0.26f), position: new(-0.04f, 0.06f, 0.08f), color: Rgb(255, 240, 155), unlit: true);
                // Жемчужины-крапаны по золотому ободу и застёжка-булавка сзади
                for (int i = 0; i < 8; i++)
                {
                    float ang = (i / 8f) * MathF.PI * 2f;
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.07f,
                        position: new(MathF.Cos(ang) * 0.26f, MathF.Sin(ang) * 0.32f, 0.06f), color: Color.Gold, unlit: true);
                }
                new Entity(parent: root, model: "cube", scale: new(0.03f, 0.76f, 0.03f), position: new(0.06f, -0.02f, -0.10f), rotation: new(0, 0, -14), color: Rgb(210, 215, 225));
            }
            else if (itemKey == "opal")
            {
                // Причудливый Опал — асимметричный тёмный булыжник-жеода, расколотый с переливающимся ядром опала внутри
                new Entity(parent: root, model: "sphere", scale: new(0.64f, 0.58f, 0.50f), position: new(-0.06f, -0.04f, -0.04f), rotation: new(14, -20, 18), color: Rgb(58, 56, 68));
                new Entity(parent: root, model: "diamond", scale: new(0.48f, 0.52f, 0.42f), position: new(-0.14f, 0.08f, -0.06f), rotation: new(-12, 25, 22), color: Rgb(74, 70, 86));
                new Entity(parent: root, model: "sphere", scale: new(0.42f, 0.34f, 0.38f), position: new(0.12f, -0.16f, 0.02f), color: Rgb(48, 46, 58));
                // Сияющее перламутровое фиолетово-бирюзовое ядро в разломе породы
                new Entity(parent: root, model: "diamond", scale: new(0.36f, 0.44f, 0.32f), position: new(0.06f, 0.04f, 0.10f), rotation: new(8, 15, -16), color: Rgb(185, 115, 255), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.24f, 0.30f, 0.24f), position: new(0.14f, 0.10f, 0.15f), rotation: new(-10, -25, 20), color: Rgb(115, 245, 255), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.16f, position: new(0.04f, -0.02f, 0.18f), color: Rgb(255, 165, 235), unlit: true);
            }
            else if (itemKey == "focus_crystal")
            {
                // Фокусирующий Кристалл — алая розовая призма, подвешенная в металлическом кольце-треножнике
                new Entity(parent: root, model: "diamond", scale: new(0.76f, 0.76f, 0.12f), color: Rgb(62, 68, 82));
                new Entity(parent: root, model: "diamond", scale: new(0.58f, 0.58f, 0.15f), color: Rgb(28, 30, 38));
                // Три опорные ножки-излучателя к центральному кристаллу
                for (int i = 0; i < 3; i++)
                {
                    float ang = (i * 120f - 90f) * MathF.PI / 180f;
                    float px = MathF.Cos(ang) * 0.26f;
                    float py = MathF.Sin(ang) * 0.26f;
                    new Entity(parent: root, model: "cube", scale: new(0.05f, 0.24f, 0.08f),
                        position: new(px, py, 0), rotation: new(0, 0, i * 120f), color: Rgb(135, 145, 162));
                }
                // Парящая розовая октаэдрическая призма в фокусе
                new Entity(parent: root, model: "diamond", scale: new(0.32f, 0.44f, 0.32f), color: Rgb(255, 55, 135), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.18f, 0.26f, 0.35f), color: Rgb(255, 185, 215), unlit: true);
            }
            else if (itemKey == "chronobauble")
            {
                // Хронобрелок — латунные карманные часы-астролябия со стрелками и фиолетовыми кольцами времени
                new Entity(parent: root, model: "sphere", scale: new(0.56f, 0.56f, 0.18f), color: Rgb(205, 155, 55));
                new Entity(parent: root, model: "sphere", scale: new(0.46f, 0.46f, 0.21f), position: new(0, 0, 0.02f), color: Rgb(36, 24, 54));
                // Стрелки часов и центральная ось
                new Entity(parent: root, model: "cube", scale: new(0.03f, 0.18f, 0.04f), position: new(0, 0.06f, 0.12f), rotation: new(0, 0, -15), color: Rgb(245, 215, 120), unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.14f, 0.03f, 0.04f), position: new(0.05f, -0.01f, 0.13f), rotation: new(0, 0, -25), color: Rgb(225, 165, 255), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.07f, position: new(0, 0, 0.13f), color: Color.Gold, unlit: true);
                // Заводная головка и колечко сверху
                new Entity(parent: root, model: "cube", scale: new(0.10f, 0.09f, 0.10f), position: new(0, 0.31f, 0), color: Rgb(215, 165, 60));
                new Entity(parent: root, model: "diamond", scale: new(0.18f, 0.16f, 0.05f), position: new(0, 0.41f, 0), color: Color.Gold, unlit: true);
                // Два наклонных хроно-кольца вокруг брелока
                new Entity(parent: root, model: "diamond", scale: new(0.84f, 0.07f, 0.84f), rotation: new(28, 15, 24), color: Rgb(195, 105, 255, 185), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.76f, 0.06f, 0.76f), rotation: new(-26, -20, -32), color: Rgb(135, 215, 255, 165), unlit: true);
            }
            else if (itemKey == "frost_relic")
            {
                // Ледяная Реликвия — шестилучевая кристаллическая снежинка с ледяными шипами и морозным ореолом
                for (int i = 0; i < 3; i++)
                {
                    float ang = i * 60f;
                    new Entity(parent: root, model: "diamond", scale: new(0.14f, 0.90f, 0.12f),
                        rotation: new(0, 0, ang), color: Rgb(135, 235, 255), unlit: true);
                }
                for (int i = 0; i < 6; i++)
                {
                    float rad = (i * 60f) * MathF.PI / 180f;
                    float px = MathF.Sin(rad) * 0.30f;
                    float py = MathF.Cos(rad) * 0.30f;
                    new Entity(parent: root, model: "diamond", scale: new(0.22f, 0.08f, 0.08f),
                        position: new(px, py, 0.02f), rotation: new(0, 0, -i * 60f), color: Color.White, unlit: true);
                }
                new Entity(parent: root, model: "sphere", scaleUniform: 0.24f, color: Color.White, unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.68f, 0.68f, 0.18f), rotation: new(0, 0, 30), color: Rgb(95, 215, 255, 110), unlit: true);
            }
            else if (itemKey == "razorwire")
            {
                // Колючая Проволока — скрученные стальные витки с острыми крестообразными шипами
                new Entity(parent: root, model: "diamond", scale: new(0.76f, 0.14f, 0.76f), rotation: new(22, 0, 18), color: Rgb(135, 148, 160));
                new Entity(parent: root, model: "diamond", scale: new(0.72f, 0.12f, 0.72f), rotation: new(-20, 35, -24), color: Rgb(105, 118, 130));
                for (int i = 0; i < 6; i++)
                {
                    float rad = (i / 6f) * MathF.PI * 2f;
                    float bx = MathF.Cos(rad) * 0.32f;
                    float bz = MathF.Sin(rad) * 0.32f;
                    float by = MathF.Sin(rad * 2f) * 0.10f;
                    new Entity(parent: root, model: "diamond", scale: new(0.06f, 0.24f, 0.06f),
                        position: new(bx, by, bz), rotation: new(35, i * 60f, 42), color: Rgb(195, 245, 120), unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(0.06f, 0.22f, 0.06f),
                        position: new(bx, by, bz), rotation: new(-35, i * 60f, -42), color: Rgb(235, 70, 75), unlit: true);
                }
            }
            else if (itemKey == "kjaro_band")
            {
                // Кольцо Кьяро — бронзово-золотой перстень пламени с огненными зубцами и солнечным рубином
                var tilt = new Entity(parent: root, rotation: new(22, 0, -14));
                new Entity(parent: tilt, model: "diamond", scale: new(0.72f, 0.22f, 0.72f), color: Rgb(225, 135, 35));
                new Entity(parent: tilt, model: "diamond", scale: new(0.50f, 0.26f, 0.50f), color: Rgb(55, 28, 18));
                new Entity(parent: tilt, model: "diamond", scale: new(0.28f, 0.36f, 0.28f), position: new(0, 0.20f, 0.24f), color: Rgb(255, 95, 25), unlit: true);
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.16f, position: new(0, 0.22f, 0.26f), color: Rgb(255, 235, 115), unlit: true);
                foreach (float sx in new[] { -0.18f, 0.18f })
                    new Entity(parent: tilt, model: "diamond", scale: new(0.10f, 0.26f, 0.10f), position: new(sx, 0.16f, 0.20f), rotation: new(0, 0, sx * -45f), color: Color.Gold, unlit: true);
            }
            else if (itemKey == "runald_band")
            {
                // Кольцо Рунальда — серебряный морозный перстень с осколками вечного льда
                var tilt = new Entity(parent: root, rotation: new(22, 0, 14));
                new Entity(parent: tilt, model: "diamond", scale: new(0.72f, 0.20f, 0.72f), color: Rgb(175, 210, 235));
                new Entity(parent: tilt, model: "diamond", scale: new(0.50f, 0.24f, 0.50f), color: Rgb(28, 48, 72));
                new Entity(parent: tilt, model: "diamond", scale: new(0.24f, 0.42f, 0.24f), position: new(0, 0.22f, 0.24f), color: Rgb(85, 225, 255), unlit: true);
                foreach (float sx in new[] { -1f, 1f })
                    new Entity(parent: tilt, model: "diamond", scale: new(0.14f, 0.28f, 0.14f), position: new(sx * 0.15f, 0.16f, 0.22f), rotation: new(0, 0, sx * -28f), color: Color.White, unlit: true);
            }
            else if (itemKey == "atg_missile")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 28));
                // Ракетница AtG Mk. 1 — оливковый пусковой модуль с управляемой ракетой и соплом
                new Entity(parent: tilt, model: "cube", scale: new(0.28f, 0.68f, 0.28f), color: Rgb(92, 165, 68));
                new Entity(parent: tilt, model: "diamond", scale: new(0.30f, 0.34f, 0.30f), position: new(0, 0.46f, 0), color: Rgb(255, 210, 45), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.30f, 0.06f, 0.30f), position: new(0, 0.22f, 0), color: Rgb(35, 40, 45));
                new Entity(parent: tilt, model: "cube", scale: new(0.30f, 0.05f, 0.30f), position: new(0, -0.06f, 0), color: Rgb(245, 205, 55), unlit: true);
                foreach (float sx in new[] { -0.18f, 0.18f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.20f, 0.16f, 0.04f), position: new(sx, -0.26f, 0), color: Rgb(62, 70, 62));
                foreach (float sz in new[] { -0.18f, 0.18f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.04f, 0.16f, 0.20f), position: new(0, -0.26f, sz), color: Rgb(62, 70, 62));
                new Entity(parent: tilt, model: "cube", scale: new(0.20f, 0.08f, 0.20f), position: new(0, -0.37f, 0), color: Rgb(48, 52, 58));
                new Entity(parent: tilt, model: "diamond", scale: new(0.16f, 0.24f, 0.16f), position: new(0, -0.50f, 0), color: Rgb(255, 145, 35), unlit: true);
            }
            else if (itemKey == "bundle_fireworks")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -14));
                // Связка Фейерверков — 5 праздничных ракет на деревянных палочках, перевязанных золотой лентой
                var offsets = new (float x, float z, float yOff, Color capCol)[]
                {
                    (0f, 0f, 0.06f, Rgb(255, 215, 55)),
                    (-0.14f, 0.08f, 0.0f, Rgb(255, 75, 195)),
                    (0.14f, 0.08f, -0.02f, Rgb(85, 235, 255)),
                    (-0.10f, -0.12f, -0.01f, Rgb(115, 255, 125)),
                    (0.12f, -0.11f, 0.02f, Rgb(255, 145, 45))
                };
                foreach (var (fx, fz, fy, capCol) in offsets)
                {
                    new Entity(parent: tilt, model: "cube", scale: new(0.03f, 0.42f, 0.03f), position: new(fx, -0.28f + fy, fz), color: Rgb(195, 155, 95));
                    new Entity(parent: tilt, model: "cube", scale: new(0.11f, 0.44f, 0.11f), position: new(fx, 0.08f + fy, fz), color: Rgb(225, 42, 58));
                    new Entity(parent: tilt, model: "cube", scale: new(0.12f, 0.05f, 0.12f), position: new(fx, 0.12f + fy, fz), color: Color.Gold, unlit: true);
                    new Entity(parent: tilt, model: "diamond", scale: new(0.15f, 0.20f, 0.15f), position: new(fx, 0.36f + fy, fz), color: capCol, unlit: true);
                }
                // Перевязь связки посередине
                new Entity(parent: tilt, model: "cube", scale: new(0.42f, 0.08f, 0.38f), position: new(0, 0.02f, -0.01f), color: Rgb(245, 195, 45), unlit: true);
            }
            else if (itemKey == "behemoth")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 18, -22));
                // Блистательный Бегемот — тяжёлая осадная мортира с латунными обручами и раскалённым жерлом
                new Entity(parent: tilt, model: "cube", scale: new(0.38f, 0.22f, 0.48f), position: new(0, -0.18f, -0.06f), color: Rgb(58, 62, 72));
                // Массивный ствол орудия
                new Entity(parent: tilt, model: "cube", scale: new(0.32f, 0.32f, 0.78f), position: new(0, 0.06f, 0.04f), color: Rgb(78, 84, 96));
                foreach (float bz in new[] { -0.20f, 0.06f, 0.30f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.36f, 0.36f, 0.07f), position: new(0, 0.06f, bz), color: Rgb(215, 162, 52));
                // Прицел сверху и запальный фитиль сзади
                new Entity(parent: tilt, model: "cube", scale: new(0.08f, 0.10f, 0.24f), position: new(0.12f, 0.27f, -0.04f), color: Rgb(185, 138, 45));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.14f, position: new(0, 0.06f, -0.38f), color: Rgb(195, 145, 48));
                // Огненное жерло пушки
                new Entity(parent: tilt, model: "cube", scale: new(0.40f, 0.40f, 0.10f), position: new(0, 0.06f, 0.42f), color: Rgb(46, 50, 58));
                new Entity(parent: tilt, model: "sphere", scale: new(0.28f, 0.28f, 0.18f), position: new(0, 0.06f, 0.46f), color: Rgb(255, 95, 25), unlit: true);
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.15f, position: new(0, 0.06f, 0.52f), color: Rgb(255, 235, 110), unlit: true);
            }
            else if (itemKey == "sticky_bomb")
            {
                var tilt = new Entity(parent: root, rotation: new(28, 0, -16));
                // Липкая Бомба — круглая оранжево-жёлтая шашка C4 с присосками, таймером, мигающим диодом и антенной
                new Entity(parent: tilt, model: "sphere", scale: new(0.68f, 0.28f, 0.68f), color: Rgb(245, 145, 30));
                new Entity(parent: tilt, model: "sphere", scale: new(0.52f, 0.31f, 0.52f), position: new(0, 0.02f, 0), color: Rgb(52, 56, 66));
                // Липкие подушечки по периметру
                for (int i = 0; i < 6; i++)
                {
                    float rad = (i / 6f) * MathF.PI * 2f;
                    new Entity(parent: tilt, model: "sphere", scale: new(0.16f, 0.12f, 0.16f),
                        position: new(MathF.Cos(rad) * 0.30f, -0.04f, MathF.Sin(rad) * 0.30f), color: Rgb(255, 195, 45), unlit: true);
                }
                // Цифровой экранчик, красный светодиод и антенна
                new Entity(parent: tilt, model: "cube", scale: new(0.22f, 0.05f, 0.12f), position: new(-0.04f, 0.16f, 0.06f), color: Rgb(25, 30, 35));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.11f, position: new(0.08f, 0.18f, -0.05f), color: Rgb(255, 35, 45), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.025f, 0.28f, 0.025f), position: new(-0.14f, 0.28f, -0.08f), rotation: new(0, 0, 12), color: Rgb(185, 195, 210));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.06f, position: new(-0.17f, 0.42f, -0.08f), color: Rgb(255, 65, 65), unlit: true);
            }
            else if (itemKey == "stun_grenade")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 16));
                // Светошумовая Граната — тактическая граната M84 с перфорированным корпусом, рычагом и чекой
                new Entity(parent: tilt, model: "cube", scale: new(0.32f, 0.56f, 0.32f), color: Rgb(78, 92, 82));
                new Entity(parent: tilt, model: "cube", scale: new(0.36f, 0.10f, 0.36f), position: new(0, 0.24f, 0), color: Rgb(165, 175, 188));
                new Entity(parent: tilt, model: "cube", scale: new(0.36f, 0.10f, 0.36f), position: new(0, -0.24f, 0), color: Rgb(165, 175, 188));
                // Светошумовые прорези вспышки по кругу корпуса
                foreach (float vy in new[] { -0.08f, 0.08f })
                {
                    new Entity(parent: tilt, model: "cube", scale: new(0.35f, 0.06f, 0.12f), position: new(0, vy, 0), color: Rgb(215, 245, 255), unlit: true);
                    new Entity(parent: tilt, model: "cube", scale: new(0.12f, 0.08f, 0.35f), position: new(0, vy, 0), color: Rgb(215, 245, 255), unlit: true);
                }
                // Запал, предохранительная скоба (рычаг) и кольцо чеки
                new Entity(parent: tilt, model: "cube", scale: new(0.16f, 0.14f, 0.16f), position: new(0, 0.36f, 0), color: Rgb(135, 142, 155));
                new Entity(parent: tilt, model: "cube", scale: new(0.05f, 0.44f, 0.08f), position: new(0.21f, 0.16f, 0), rotation: new(0, 0, -8), color: Rgb(115, 125, 138));
                new Entity(parent: tilt, model: "diamond", scale: new(0.16f, 0.16f, 0.04f), position: new(-0.14f, 0.38f, 0.06f), color: Rgb(235, 210, 95), unlit: true);
            }
            else if (itemKey == "clover")
            {
                new Entity(parent: root, model: "cube", scale: new(0.07f, 0.55f, 0.07f), position: new(0, -0.22f, 0), rotation: new(0, 0, -12), color: Rgb(55, 185, 85));
                foreach (float ang in new[] { 0f, 90f, 180f, 270f })
                {
                    float rad = ang * MathF.PI / 180f;
                    float lx = MathF.Cos(rad) * 0.22f, lz = MathF.Sin(rad) * 0.22f;
                    new Entity(parent: root, model: "sphere", scale: new(0.32f, 0.12f, 0.32f), position: new(lx, 0.08f, lz), color: Rgb(65, 235, 110), unlit: true);
                    new Entity(parent: root, model: "sphere", scale: new(0.22f, 0.15f, 0.22f), position: new(lx * 0.85f, 0.14f, lz * 0.85f), color: Rgb(135, 255, 165), unlit: true);
                }
                new Entity(parent: root, model: "sphere", scaleUniform: 0.14f, position: new(0, 0.18f, 0), color: Color.Gold, unlit: true);
            }
            else if (itemKey == "predatory")
            {
                new Entity(parent: root, model: "sphere", scale: new(0.52f, 0.44f, 0.56f), color: Rgb(235, 225, 205));
                foreach (float sx in new[] { -1f, 1f })
                {
                    new Entity(parent: root, model: "diamond", scale: new(0.16f, 0.52f, 0.16f),
                        position: new(sx * 0.28f, 0.22f, -0.04f), rotation: new(15, 0, sx * -38f), color: Rgb(85, 65, 52));
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.14f,
                        position: new(sx * 0.14f, 0.04f, 0.24f), color: Rgb(255, 70, 50), unlit: true);
                }
                new Entity(parent: root, model: "cube", scale: new(0.36f, 0.06f, 0.04f), position: new(0, 0.14f, 0.27f), color: Rgb(225, 45, 35), unlit: true);
            }
            else if (itemKey == "wax_quill")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 26));
                // Восковое Перо — золотистое перо на подставке из расплавленного красного сургуча/воска
                new Entity(parent: tilt, model: "cube", scale: new(0.045f, 0.78f, 0.045f), position: new(0, 0.08f, 0), color: Rgb(245, 210, 120), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.28f, 0.68f, 0.09f), position: new(-0.07f, 0.16f, 0), color: Rgb(255, 232, 155), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.20f, 0.56f, 0.08f), position: new(0.07f, 0.18f, 0), color: Rgb(245, 198, 105), unlit: true);
                new Entity(parent: tilt, model: "sphere", scale: new(0.34f, 0.18f, 0.34f), position: new(0, -0.30f, 0), color: Rgb(215, 42, 52), unlit: true);
                foreach (var (wx, wz) in new[] { (-0.12f, 0.08f), (0.13f, -0.06f) })
                    new Entity(parent: tilt, model: "sphere", scaleUniform: 0.11f, position: new(wx, -0.34f, wz), color: Rgb(185, 28, 38), unlit: true);
            }
            else if (itemKey == "red_whip")
            {
                var tilt = new Entity(parent: root, rotation: new(18, 0, 22));
                // Красный Кнут — свёрнутый кольцами алый кожаный кнут с рукоятью и кисточкой
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.42f, 0.10f), position: new(-0.22f, -0.14f, 0), rotation: new(0, 0, 14), color: Rgb(95, 52, 36));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.13f, position: new(-0.27f, -0.35f, 0), color: Rgb(215, 165, 55));
                new Entity(parent: tilt, model: "diamond", scale: new(0.62f, 0.12f, 0.62f), position: new(0.04f, -0.02f, 0), color: Rgb(215, 42, 42), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.46f, 0.14f, 0.46f), position: new(0.06f, 0.10f, 0), color: Rgb(245, 70, 60), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.30f, 0.15f, 0.30f), position: new(0.08f, 0.22f, 0), color: Rgb(255, 110, 90), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.08f, 0.24f, 0.08f), position: new(0.28f, 0.28f, 0.06f), rotation: new(0, 0, -45), color: Color.White, unlit: true);
            }
            else if (itemKey == "shaped_glass")
            {
                var tilt = new Entity(parent: root, rotation: new(12, 0, -24));
                new Entity(parent: tilt, model: "diamond", scale: new(0.32f, 0.96f, 0.14f), color: Rgb(135, 220, 255), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.18f, 0.65f, 0.18f), position: new(0.10f, 0.08f, 0), rotation: new(0, 0, -18), color: Color.White, unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.14f, 0.44f, 0.12f), position: new(-0.11f, -0.14f, 0.03f), rotation: new(0, 0, 22), color: Rgb(75, 175, 255), unlit: true);
            }
            else if (itemKey == "transcendence")
            {
                new Entity(parent: root, model: "sphere", scale: new(0.48f, 0.32f, 0.64f), color: Rgb(45, 155, 245), unlit: true);
                new Entity(parent: root, model: "sphere", scale: new(0.28f, 0.24f, 0.26f), position: new(0, 0.04f, 0.32f), color: Rgb(95, 215, 255), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.08f, 0.24f, 0.12f), position: new(0, 0.18f, 0.42f), rotation: new(35, 0, 0), color: Color.White, unlit: true);
                foreach (float sx in new[] { -1f, 1f })
                    new Entity(parent: root, model: "diamond", scale: new(0.36f, 0.06f, 0.42f), position: new(sx * 0.24f, 0.14f, -0.04f), rotation: new(0, 0, sx * -20f), color: Color.White, unlit: true);
            }
            else if (itemKey == "purity")
            {
                for (int i = 0; i < 4; i++)
                    new Entity(parent: root, model: "diamond", scale: new(0.24f, 0.78f, 0.24f), rotation: new(0, i * 45f, 45f), color: i % 2 == 0 ? Color.White : Rgb(110, 215, 255), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.22f, color: Rgb(185, 240, 255), unlit: true);
            }
            else if (itemKey == "lunar_pauldron")
            {
                // Каменный Наплечник — тяжёлый резной лунный наплечник с кристаллами и рунами
                new Entity(parent: root, model: "sphere", scale: new(0.70f, 0.46f, 0.56f), color: Rgb(148, 165, 192));
                new Entity(parent: root, model: "sphere", scale: new(0.56f, 0.34f, 0.46f), position: new(-0.08f, 0.14f, 0), color: Rgb(175, 192, 218));
                new Entity(parent: root, model: "diamond", scale: new(0.78f, 0.16f, 0.62f), position: new(0, -0.10f, 0), color: Rgb(65, 165, 255), unlit: true);
                foreach (var (sx, sy, rotZ) in new[] { (-0.18f, 0.30f, 22f), (0.08f, 0.34f, -12f), (0.28f, 0.22f, -36f) })
                    new Entity(parent: root, model: "diamond", scale: new(0.14f, 0.28f, 0.14f), position: new(sx, sy, 0), rotation: new(0, 0, rotZ), color: Rgb(120, 215, 255), unlit: true);
            }
            else if (itemKey == "spinel_tonic")
            {
                // Лунный Тоник — сияющий графин лунного эликсира
                new Entity(parent: root, model: "sphere", scale: new(0.42f, 0.42f, 0.42f), position: new(0, -0.12f, 0), color: Rgb(55, 175, 255), unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.18f, 0.24f, 0.18f), position: new(0, 0.24f, 0), color: Rgb(165, 195, 225));
                new Entity(parent: root, model: "cube", scale: new(0.22f, 0.10f, 0.22f), position: new(0, 0.39f, 0), color: Rgb(135, 90, 50));
                new Entity(parent: root, model: "diamond", scale: new(0.62f, 0.08f, 0.62f), position: new(0, -0.10f, 0), color: Rgb(125, 220, 255), unlit: true);
                new Entity(parent: root, model: "sphere", scale: new(0.52f, 0.56f, 0.52f), position: new(0, -0.10f, 0), color: Rgb(180, 220, 250, 110), unlit: true);
            }
            else if (itemKey == "tonic_affliction")
            {
                // Испорченный Тоник — треснувший наклонный флакон с тёмно-фиолетовыми миазмами проклятия
                var tilt = new Entity(parent: root, rotation: new(0, 0, -24));
                new Entity(parent: tilt, model: "sphere", scale: new(0.40f, 0.36f, 0.40f), position: new(0, -0.14f, 0), color: Rgb(125, 55, 175), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.18f, 0.22f, 0.18f), position: new(0, 0.22f, 0), color: Rgb(95, 85, 115));
                new Entity(parent: tilt, model: "sphere", scale: new(0.50f, 0.54f, 0.50f), position: new(0, -0.10f, 0), color: Rgb(115, 75, 155, 105), unlit: true);
                foreach (var (fx, fy) in new[] { (-0.12f, 0.40f), (0.10f, 0.50f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.11f, position: new(fx, fy, 0), color: Rgb(155, 75, 215, 165), unlit: true);
            }
            // ================= 10 ПРЕДМЕТОВ LEAGUE OF LEGENDS =================
            else if (itemKey == "dorans_blade")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -32));
                new Entity(parent: tilt, model: "diamond", scale: new(0.32f, 0.78f, 0.10f), position: new(0, 0.18f, 0), color: Rgb(225, 215, 195), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.06f, 0.48f, 0.12f), position: new(0, 0.12f, 0), color: Rgb(185, 135, 65));
                new Entity(parent: tilt, model: "cube", scale: new(0.46f, 0.10f, 0.16f), position: new(0, -0.16f, 0), color: Rgb(215, 165, 55));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.13f, position: new(0, -0.16f, 0.08f), color: Rgb(245, 55, 65), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.11f, 0.30f, 0.11f), position: new(0, -0.34f, 0), color: Rgb(115, 62, 35));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.15f, position: new(0, -0.50f, 0), color: Rgb(215, 165, 55));
            }
            else if (itemKey == "boots_swiftness")
            {
                new Entity(parent: root, model: "cube", scale: new(0.28f, 0.52f, 0.28f), position: new(-0.04f, 0.08f, 0), color: Rgb(235, 240, 250));
                new Entity(parent: root, model: "cube", scale: new(0.30f, 0.10f, 0.30f), position: new(-0.04f, 0.30f, 0), color: Rgb(215, 45, 55));
                new Entity(parent: root, model: "cube", scale: new(0.30f, 0.20f, 0.52f), position: new(-0.04f, -0.22f, 0.12f), color: Rgb(240, 244, 252));
                new Entity(parent: root, model: "cube", scale: new(0.32f, 0.06f, 0.54f), position: new(-0.04f, -0.35f, 0.12f), color: Rgb(185, 40, 48));
                foreach (float sx in new[] { -1f, 1f })
                {
                    new Entity(parent: root, model: "diamond", scale: new(0.06f, 0.36f, 0.26f),
                        position: new(-0.04f + sx * 0.18f, 0.14f, -0.06f), rotation: new(-25f, 0, sx * -22f), color: Color.SkyBlue, unlit: true);
                }
            }
            else if (itemKey == "sheen")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 30));
                new Entity(parent: tilt, model: "diamond", scale: new(0.26f, 0.86f, 0.10f), position: new(0, 0.18f, 0), color: Rgb(75, 205, 255), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.14f, 0.68f, 0.13f), position: new(0, 0.20f, 0), color: Color.White, unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.54f, 0.14f, 0.16f), position: new(0, -0.18f, 0), color: Rgb(190, 225, 255));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.18f, 0.06f), color: Color.SkyBlue, unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.30f, 0.10f), position: new(0, -0.36f, 0), color: Rgb(48, 78, 135));
            }
            else if (itemKey == "liandrys_torment")
            {
                new Entity(parent: root, model: "sphere", scale: new(0.56f, 0.68f, 0.26f), color: Rgb(238, 242, 250));
                new Entity(parent: root, model: "diamond", scale: new(0.38f, 0.28f, 0.24f), position: new(0, -0.26f, 0.02f), color: Rgb(225, 230, 242));
                foreach (float sx in new[] { -1f, 1f })
                {
                    new Entity(parent: root, model: "sphere", scale: new(0.15f, 0.09f, 0.10f),
                        position: new(sx * 0.14f, 0.08f, 0.12f), rotation: new(0, 0, sx * -14f), color: Rgb(65, 215, 255), unlit: true);
                    new Entity(parent: root, model: "cube", scale: new(0.04f, 0.26f, 0.06f),
                        position: new(sx * 0.14f, -0.08f, 0.14f), color: Rgb(95, 230, 255), unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(0.12f, 0.28f, 0.12f),
                        position: new(sx * 0.20f, 0.34f, -0.02f), rotation: new(0, 0, sx * -22f), color: Rgb(52, 42, 72));
                }
            }
            else if (itemKey == "ludens_companion")
            {
                new Entity(parent: root, model: "cube", scale: new(0.09f, 0.72f, 0.09f), position: new(0, -0.14f, 0), color: Rgb(205, 165, 75));
                new Entity(parent: root, model: "diamond", scale: new(0.56f, 0.42f, 0.14f), position: new(0, 0.22f, 0), color: Rgb(225, 185, 85));
                new Entity(parent: root, model: "sphere", scaleUniform: 0.28f, position: new(0, 0.24f, 0), color: Rgb(195, 85, 255), unlit: true);
                foreach (float ang in new[] { -45f, 0f, 45f })
                {
                    float rad = ang * MathF.PI / 180f;
                    new Entity(parent: root, model: "diamond", scale: new(0.10f, 0.20f, 0.10f),
                        position: new(MathF.Sin(rad) * 0.36f, 0.48f + MathF.Cos(rad) * 0.08f, 0), color: Rgb(235, 145, 255), unlit: true);
                }
            }
            else if (itemKey == "statikk_shiv")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -30));
                new Entity(parent: tilt, model: "diamond", scale: new(0.24f, 0.84f, 0.10f), position: new(0, 0.16f, 0), color: Rgb(255, 235, 75), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.12f, 0.46f, 0.14f), position: new(-0.06f, 0.08f, 0), color: Rgb(75, 155, 215));
                foreach (float cy in new[] { -0.06f, 0.08f, 0.22f })
                    new Entity(parent: tilt, model: "cube", scale: new(0.16f, 0.04f, 0.16f), position: new(-0.06f, cy, 0), color: Color.SkyBlue, unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.30f, 0.10f), position: new(0, -0.32f, 0), color: Rgb(165, 125, 45));
            }
            else if (itemKey == "blade_ruined_king")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -28));
                new Entity(parent: tilt, model: "diamond", scale: new(0.12f, 0.88f, 0.10f), position: new(-0.10f, 0.20f, 0), color: Rgb(42, 52, 58));
                new Entity(parent: tilt, model: "diamond", scale: new(0.12f, 0.88f, 0.10f), position: new(0.10f, 0.20f, 0), color: Rgb(42, 52, 58));
                new Entity(parent: tilt, model: "diamond", scale: new(0.14f, 0.82f, 0.07f), position: new(0, 0.22f, 0), color: Rgb(65, 255, 195), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.58f, 0.16f, 0.16f), position: new(0, -0.18f, 0), color: Rgb(35, 45, 50));
                new Entity(parent: tilt, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.18f, 0.05f), color: Rgb(80, 255, 205), unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.34f, 0.10f), position: new(0, -0.38f, 0), color: Rgb(30, 38, 44));
            }
            else if (itemKey == "rabadons_deathcap")
            {
                new Entity(parent: root, model: "sphere", scale: new(0.92f, 0.12f, 0.92f), position: new(0, -0.18f, 0), color: Rgb(175, 28, 52));
                new Entity(parent: root, model: "diamond", scale: new(0.54f, 0.62f, 0.54f), position: new(0, 0.10f, 0), color: Rgb(205, 36, 64));
                new Entity(parent: root, model: "diamond", scale: new(0.28f, 0.38f, 0.28f), position: new(0.14f, 0.38f, 0), rotation: new(0, 0, -35), color: Rgb(185, 30, 56));
                new Entity(parent: root, model: "diamond", scale: new(0.58f, 0.12f, 0.58f), position: new(0, -0.10f, 0), color: Rgb(245, 195, 55), unlit: true);
                new Entity(parent: root, model: "sphere", scale: new(0.18f, 0.12f, 0.10f), position: new(0, -0.06f, 0.26f), color: Rgb(255, 235, 95), unlit: true);
            }
            else if (itemKey == "infinity_edge")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 28));
                new Entity(parent: tilt, model: "diamond", scale: new(0.36f, 0.94f, 0.12f), position: new(0, 0.18f, 0), color: Rgb(255, 205, 55), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.16f, 0.66f, 0.15f), position: new(0, 0.16f, 0), color: Rgb(40, 48, 68));
                new Entity(parent: tilt, model: "diamond", scale: new(0.08f, 0.52f, 0.17f), position: new(0, 0.16f, 0), color: Rgb(255, 245, 160), unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.62f, 0.18f, 0.18f), position: new(0, -0.22f, 0), color: Rgb(235, 165, 35));
                new Entity(parent: tilt, model: "cube", scale: new(0.11f, 0.32f, 0.11f), position: new(0, -0.40f, 0), color: Rgb(125, 75, 30));
            }
            else if (itemKey == "zhonyas_hourglass")
            {
                new Entity(parent: root, model: "cube", scale: new(0.52f, 0.08f, 0.52f), position: new(0, 0.42f, 0), color: Color.Gold, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.52f, 0.08f, 0.52f), position: new(0, -0.42f, 0), color: Color.Gold, unlit: true);
                foreach (float sx in new[] { -0.21f, 0.21f })
                    new Entity(parent: root, model: "cube", scale: new(0.05f, 0.78f, 0.05f), position: new(sx, 0, 0), color: Rgb(235, 185, 45));
                new Entity(parent: root, model: "diamond", scale: new(0.38f, 0.42f, 0.38f), position: new(0, 0.20f, 0), color: Rgb(85, 205, 255), unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.38f, 0.42f, 0.38f), position: new(0, -0.20f, 0), color: Rgb(85, 205, 255), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.18f, position: new(0, 0, 0), color: Color.Gold, unlit: true);
            }
            else if (TryBuildExpansionItemMesh(root, itemKey, itemCol))
            {
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
