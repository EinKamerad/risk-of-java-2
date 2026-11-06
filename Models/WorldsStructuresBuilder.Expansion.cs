using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Models
{
    public static partial class WorldStructuresBuilder
    {
        private static bool TryBuildExpansionMapStructure(
            Entity root, string structType, StagePalette palette, Vector3 pos, float rotY,
            Random rng, List<ObstacleCircle> obstacles, List<ObstacleBox>? outBoxes)
        {
            float NextRange(float min, float max) => min + (float)rng.NextDouble() * (max - min);

            float radY = rotY * (MathF.PI / 180f);
            float cosY = MathF.Cos(radY), sinY = MathF.Sin(radY);
            (float wx, float wz) ToWorld(float lx, float lz)
                => (pos.X + lx * cosY + lz * sinY, pos.Z - lx * sinY + lz * cosY);

            Color darkRuins = Rgb(
                (byte)Math.Max(palette.Ruins.R - 18, 0),
                (byte)Math.Max(palette.Ruins.G - 18, 0),
                (byte)Math.Max(palette.Ruins.B - 15, 0));
            Color lightRuins = Rgb(
                (byte)Math.Min(palette.Ruins.R + 18, 255),
                (byte)Math.Min(palette.Ruins.G + 18, 255),
                (byte)Math.Min(palette.Ruins.B + 16, 255));

            switch (structType)
            {
                case "dark_fountain_spire":
                {
                    // Готический столп Тёмного Святилища (Deltarune) + бьющий в небо бирюзово-фиолетовый шпиль Тёмного Фонтана
                    float h = NextRange(11.5f, 15.2f);
                    root.AllowObstructionFade = true;
                    root.ObstructionRadius = 5.0f;
                    root.ObstructionHeight = h + 4.2f;

                    Color gothicDark = Rgb(26, 22, 52);
                    Color gothicStone = Rgb(48, 42, 92);
                    Color fountainCyan = Rgb(75, 245, 245);
                    Color fountainViolet = Rgb(165, 68, 255);

                    // Двухступенчатый готический цоколь с проходимыми ступенями
                    new Entity(parent: root, model: "cube", scale: new(5.8f, 0.55f, 5.8f), position: new(0, 0.275f, 0), color: gothicDark);
                    new Entity(parent: root, model: "cube", scale: new(4.5f, 0.60f, 4.5f), position: new(0, 0.85f, 0), color: gothicStone);
                    new Entity(parent: root, model: "diamond", scale: new(4.8f, 0.16f, 4.8f), position: new(0, 1.18f, 0), color: fountainCyan, unlit: true);

                    // Ствол готической колонны с контрфорсами и стрельчатыми витражами
                    float colH = h * 0.62f;
                    new Entity(parent: root, model: "cube", scale: new(2.65f, colH, 2.65f), position: new(0, 1.15f + colH * 0.5f, 0), color: gothicStone);
                    foreach (float sign in new[] { -1f, 1f })
                    {
                        // Парные рельефные контрфорсы по осям X и Z (с разной высотой, без Z-fighting)
                        new Entity(parent: root, model: "cube", scale: new(0.65f, colH * 0.88f, 3.35f),
                            position: new(sign * 1.18f, 1.15f + colH * 0.44f, 0), color: gothicDark);
                        new Entity(parent: root, model: "cube", scale: new(3.25f, colH * 0.72f, 0.58f),
                            position: new(0, 1.15f + colH * 0.36f, sign * 1.18f), color: gothicDark);

                        // Стрельчатые витражные прорези на гранях колонны
                        new Entity(parent: root, model: "cube", scale: new(0.46f, colH * 0.58f, 2.76f),
                            position: new(0, 1.45f + colH * 0.42f, 0), color: sign < 0 ? fountainCyan : fountainViolet, unlit: true);
                    }
                    new Entity(parent: root, model: "cube", scale: new(2.80f, colH * 0.54f, 0.44f),
                        position: new(0, 1.45f + colH * 0.42f, 0), color: fountainCyan, unlit: true);

                    // Корона-чаша и прорывающийся вверх бирюзово-фиолетовый гейзер Тёмного Фонтана
                    float crownY = 1.15f + colH;
                    new Entity(parent: root, model: "cube", scale: new(3.45f, 0.48f, 3.45f), position: new(0, crownY, 0), color: lightRuins);
                    foreach (float cx in new[] { -1.45f, 1.45f })
                        foreach (float cz in new[] { -1.45f, 1.45f })
                            new Entity(parent: root, model: "diamond", scale: new(0.65f, 1.85f, 0.65f), position: new(cx, crownY + 0.95f, cz), color: gothicDark);

                    float spireH = h - colH + 2.8f;
                    new Entity(parent: root, model: "diamond", scale: new(1.95f, spireH, 1.95f),
                        position: new(0, crownY + spireH * 0.46f, 0), color: Rgb(145, 75, 255, 185), unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(1.05f, spireH * 1.06f, 1.05f),
                        position: new(0, crownY + spireH * 0.50f, 0), rotation: new(0, 45f, 0), color: fountainCyan, unlit: true);
                    new Entity(parent: root, model: "cube", scale: new(0.42f, spireH * 1.12f, 0.42f),
                        position: new(0, crownY + spireH * 0.52f, 0), color: Color.White, unlit: true);

                    // Крылья и три треугольника руны Дельта Пророчества вокруг фонтана
                    new Entity(parent: root, model: "diamond", scale: new(4.2f, 0.24f, 4.2f),
                        position: new(0, crownY + 1.8f, 0), rotation: new(16f, 25f, -14f), color: fountainCyan, unlit: true);
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.78f,
                        position: new(0, crownY + 2.8f, 0), color: Color.White, unlit: true);
                    foreach (float wingX in new[] { -1.85f, 1.85f })
                        new Entity(parent: root, model: "diamond", scale: new(1.55f, 0.55f, 0.28f),
                            position: new(wingX, crownY + 2.75f, 0), rotation: new(0, 0, wingX < 0 ? 22f : -22f), color: fountainViolet, unlit: true);

                    obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.35f, h + 1.5f, pos.Y));
                    outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.90f, 2.90f, rotY, 0.55f, pos.Y, walkableTop: true));
                    outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.25f, 2.25f, rotY, 1.15f, pos.Y, walkableTop: true));
                    outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 1.55f, 1.55f, rotY, h + 1.8f, pos.Y, walkableTop: false));
                    return true;
                }

                case "pipis_billboard":
                {
                    // Неоновый рекламный щит Кибер-Мира [[DEALS]] + мусорный бак Спамтона + светящиеся синие сферы [Pipis]
                    float h = NextRange(8.6f, 10.8f);
                    root.AllowObstructionFade = true;
                    root.ObstructionRadius = 4.2f;
                    root.ObstructionHeight = h + 2.2f;

                    Color steelDark = Rgb(34, 38, 52);
                    Color spamPink = Rgb(255, 95, 205);
                    Color spamYellow = Rgb(255, 235, 65);
                    Color pipisBlue = Rgb(48, 195, 255);
                    Color dumpsterGreen = Rgb(38, 108, 68);

                    // Две стальные опоры рекламного щита
                    foreach (float lx in new[] { -2.1f, 2.1f })
                    {
                        new Entity(parent: root, model: "cube", scale: new(0.85f, 0.45f, 0.85f), position: new(lx, 0.225f, -0.7f), color: darkRuins);
                        new Entity(parent: root, model: "cube", scale: new(0.46f, h - 1.2f, 0.46f), position: new(lx, (h - 1.2f) * 0.5f, -0.7f), color: steelDark);
                    }

                    // Верхний неоновый билборд [[DEALS]] с розово-жёлтыми очками Спамтона
                    float boardY = h - 1.6f;
                    new Entity(parent: root, model: "cube", scale: new(6.4f, 3.1f, 0.56f), position: new(0, boardY, -0.7f), color: Rgb(20, 16, 36));
                    // Неоновые рамки (с выносом по глубине без Z-fighting)
                    new Entity(parent: root, model: "cube", scale: new(6.62f, 0.20f, 0.64f), position: new(0, boardY + 1.58f, -0.7f), color: spamPink, unlit: true);
                    new Entity(parent: root, model: "cube", scale: new(6.62f, 0.20f, 0.64f), position: new(0, boardY - 1.58f, -0.7f), color: spamYellow, unlit: true);
                    foreach (float bx in new[] { -3.22f, 3.22f })
                        new Entity(parent: root, model: "cube", scale: new(0.20f, 3.32f, 0.66f), position: new(bx, boardY, -0.7f), color: bx < 0 ? spamPink : spamYellow, unlit: true);

                    // Скобки [[ ]] и круглые очки [[BIG SHOT]] на экране билборда
                    foreach (float brSign in new[] { -1f, 1f })
                    {
                        new Entity(parent: root, model: "cube", scale: new(0.18f, 2.0f, 0.14f), position: new(brSign * 2.55f, boardY, -0.35f), color: Color.White, unlit: true);
                        new Entity(parent: root, model: "cube", scale: new(0.18f, 1.65f, 0.14f), position: new(brSign * 2.18f, boardY, -0.35f), color: spamYellow, unlit: true);
                    }
                    new Entity(parent: root, model: "sphere", scale: new(1.32f, 1.32f, 0.36f), position: new(-0.82f, boardY + 0.12f, -0.36f), color: spamPink, unlit: true);
                    new Entity(parent: root, model: "sphere", scale: new(1.32f, 1.32f, 0.36f), position: new(0.82f, boardY + 0.12f, -0.36f), color: spamYellow, unlit: true);
                    new Entity(parent: root, model: "cube", scale: new(0.48f, 0.16f, 0.28f), position: new(0, boardY + 0.18f, -0.34f), color: Color.White, unlit: true);

                    // Зелёный мусорный контейнер перед щитом (на крышку можно запрыгнуть!)
                    new Entity(parent: root, model: "cube", scale: new(3.3f, 1.75f, 2.05f), position: new(-0.35f, 0.875f, 1.15f), color: dumpsterGreen);
                    foreach (float ribX in new[] { -1.35f, -0.35f, 0.65f })
                        new Entity(parent: root, model: "cube", scale: new(0.18f, 1.52f, 2.15f), position: new(ribX, 0.85f, 1.15f), color: Rgb(26, 78, 48));
                    // Приоткрытая чёрная крышка и гора синих яиц [Pipis]
                    new Entity(parent: root, model: "cube", scale: new(1.68f, 0.16f, 2.18f), position: new(-1.18f, 1.82f, 1.15f), color: Rgb(28, 30, 38));
                    new Entity(parent: root, model: "cube", scale: new(1.68f, 0.16f, 2.18f), position: new(0.45f, 2.12f, 1.05f), rotation: new(-24f, 0, 12f), color: Rgb(34, 36, 46));

                    (float px, float py, float pz, float pr)[] pipisPile =
                    {
                        (0.25f, 1.88f, 1.15f, 0.72f),
                        (0.85f, 1.78f, 1.35f, 0.64f),
                        (-0.15f, 1.82f, 1.55f, 0.58f),
                        (1.95f, 0.46f, 1.25f, 0.82f),
                        (2.45f, 0.36f, 0.55f, 0.66f)
                    };
                    foreach (var (px, py, pz, pr) in pipisPile)
                    {
                        new Entity(parent: root, model: "sphere", scale: new(pr * 1.18f, pr * 0.92f, pr), position: new(px, py, pz), color: pipisBlue, unlit: true);
                        new Entity(parent: root, model: "sphere", scaleUniform: pr * 0.32f, position: new(px - 0.12f, py + pr * 0.28f, pz + 0.16f), color: Color.White, unlit: true);
                    }

                    var (lwx1, lwz1) = ToWorld(-2.1f, -0.7f);
                    var (lwx2, lwz2) = ToWorld(2.1f, -0.7f);
                    var (dwx, dwz) = ToWorld(-0.35f, 1.15f);
                    obstacles.Add(new ObstacleCircle(lwx1, lwz1, 0.65f, h, pos.Y));
                    obstacles.Add(new ObstacleCircle(lwx2, lwz2, 0.65f, h, pos.Y));
                    obstacles.Add(new ObstacleCircle(dwx, dwz, 1.65f, 1.90f, pos.Y));
                    outBoxes?.Add(new ObstacleBox(dwx, dwz, 1.68f, 1.06f, rotY, 1.86f, pos.Y, walkableTop: true));
                    var (bwx, bwz) = ToWorld(0f, -0.7f);
                    outBoxes?.Add(new ObstacleBox(bwx, bwz, 3.25f, 0.42f, rotY, h + 0.2f, pos.Y + boardY - 1.6f, walkableTop: false));
                    return true;
                }

                case "qliphoth_cage":
                {
                    // Чёрно-золотая клеть Этажа Извлечения Арбитра Бины с парящими золотыми кольцами Замка (Project Moon)
                    float h = NextRange(10.5f, 13.4f);
                    root.AllowObstructionFade = true;
                    root.ObstructionRadius = 4.8f;
                    root.ObstructionHeight = h + 2.4f;

                    Color arbiterBlack = Rgb(22, 20, 24);
                    Color arbiterDarkGold = Rgb(168, 132, 46);
                    Color arbiterGold = Rgb(255, 208, 68);
                    Color coreCrimson = Rgb(215, 42, 68);

                    // Чёрно-золотой ступенчатый постамент
                    new Entity(parent: root, model: "cube", scale: new(5.6f, 0.52f, 5.6f), position: new(0, 0.26f, 0), color: arbiterBlack);
                    new Entity(parent: root, model: "cube", scale: new(5.72f, 0.14f, 5.72f), position: new(0, 0.54f, 0), color: arbiterGold, unlit: true);
                    new Entity(parent: root, model: "cube", scale: new(4.3f, 0.52f, 4.3f), position: new(0, 0.86f, 0), color: Rgb(34, 30, 36));

                    // Четыре угловых столпа Арбитра, образующие клеть подавления
                    float cageSpan = 1.75f;
                    float colH = h - 1.6f;
                    foreach (float cx in new[] { -cageSpan, cageSpan })
                    {
                        foreach (float cz in new[] { -cageSpan, cageSpan })
                        {
                            new Entity(parent: root, model: "cube", scale: new(0.76f, colH, 0.76f),
                                position: new(cx, 1.12f + colH * 0.5f, cz), color: arbiterBlack);
                            new Entity(parent: root, model: "cube", scale: new(0.18f, colH * 0.86f, 0.84f),
                                position: new(cx, 1.12f + colH * 0.5f, cz), color: arbiterGold, unlit: true);
                            new Entity(parent: root, model: "cube", scale: new(0.92f, 0.24f, 0.92f),
                                position: new(cx, 1.12f + colH * 0.35f, cz), color: arbiterDarkGold);
                            new Entity(parent: root, model: "cube", scale: new(0.92f, 0.24f, 0.92f),
                                position: new(cx, 1.12f + colH * 0.72f, cz), color: arbiterDarkGold);

                            var (pwx, pwz) = ToWorld(cx, cz);
                            obstacles.Add(new ObstacleCircle(pwx, pwz, 0.62f, h, pos.Y));
                        }
                    }

                    // Верхний свод клети с золотым навершием Главы
                    float capY = 1.12f + colH;
                    new Entity(parent: root, model: "cube", scale: new(4.8f, 0.72f, 4.8f), position: new(0, capY + 0.36f, 0), color: arbiterBlack);
                    new Entity(parent: root, model: "cube", scale: new(4.96f, 0.18f, 4.96f), position: new(0, capY + 0.76f, 0), color: arbiterGold, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(2.1f, 2.2f, 2.1f), position: new(0, capY + 1.85f, 0), color: arbiterBlack);
                    new Entity(parent: root, model: "diamond", scale: new(0.95f, 2.45f, 0.95f), position: new(0, capY + 1.95f, 0), rotation: new(0, 45f, 0), color: arbiterGold, unlit: true);

                    // Скованный внутри монолит Феи / Столпа и три перекрещенных кольца Золотого Замка
                    float midY = 1.2f + colH * 0.50f;
                    new Entity(parent: root, model: "cube", scale: new(0.20f, colH * 0.42f, 0.20f), position: new(0, capY - colH * 0.21f, 0), color: arbiterGold, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(1.45f, 3.8f, 1.45f), position: new(0, midY, 0), color: arbiterBlack);
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.92f, position: new(0, midY, 0), color: coreCrimson, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(3.35f, 0.22f, 3.35f), position: new(0, midY, 0), rotation: new(26f, 15f, -18f), color: arbiterGold, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(3.15f, 0.20f, 3.15f), position: new(0, midY + 0.15f, 0), rotation: new(-28f, 65f, 22f), color: arbiterGold, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(2.65f, 0.18f, 2.65f), position: new(0, midY - 0.85f, 0), color: Rgb(255, 225, 110, 175), unlit: true);

                    obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.35f, h + 1.2f, pos.Y));
                    outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.82f, 2.82f, rotY, 0.58f, pos.Y, walkableTop: true));
                    outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.15f, 2.15f, rotY, h + 0.8f, pos.Y, walkableTop: false));
                    return true;
                }

                case "originium_monolith":
                {
                    // Зубчатый чёрно-багровый кластер Ориджиниума + рамка древнего Саркофага PRTS ◇ (Arknights)
                    float h = NextRange(9.5f, 13.2f);
                    root.AllowObstructionFade = true;
                    root.ObstructionRadius = 4.6f;
                    root.ObstructionHeight = h + 3.0f;

                    Color origBlack = Rgb(20, 20, 26);
                    Color rhodesSlate = Rgb(46, 54, 68);
                    Color prtsCrimson = Rgb(255, 58, 72);
                    Color origAmber = Rgb(255, 158, 42);

                    // Металлическая база сдерживания Родоса и треснувшая порода
                    new Entity(parent: root, model: "cube", scale: new(5.2f, 0.48f, 5.2f), position: new(0, 0.24f, 0), color: rhodesSlate);
                    new Entity(parent: root, model: "sphere", scale: new(4.4f, 1.35f, 4.2f), position: new(0, 0.68f, 0), color: origBlack);

                    // Наклонная монолитная арка Саркофага Первой Цивилизации
                    var sarcPivot = new Entity(parent: root, position: new(0, h * 0.46f, -0.4f), rotation: new(-12f, 0, 0));
                    foreach (float sx in new[] { -1.65f, 1.65f })
                        new Entity(parent: sarcPivot, model: "cube", scale: new(0.68f, h * 0.86f, 0.82f), position: new(sx, 0, 0), color: rhodesSlate);
                    new Entity(parent: sarcPivot, model: "cube", scale: new(4.1f, 0.72f, 0.92f), position: new(0, h * 0.43f, 0), color: rhodesSlate);
                    new Entity(parent: sarcPivot, model: "cube", scale: new(2.45f, h * 0.74f, 0.26f), position: new(0, 0, 0), color: Rgb(65, 215, 255, 135), unlit: true);

                    // Острые чёрные кристаллы Ориджиниума с ало-янтарными прожилками Ассимиляции
                    (float ox, float oz, float ch, float tiltX, float tiltZ, bool crimson)[] shards =
                    {
                        (0.0f, 0.35f, h * 0.88f, 8f, -6f, true),
                        (-1.35f, 0.85f, h * 0.62f, -18f, -16f, false),
                        (1.40f, 0.75f, h * 0.66f, 16f, 18f, true),
                        (-0.75f, -1.15f, h * 0.54f, -14f, 12f, true),
                        (0.95f, -1.05f, h * 0.50f, 14f, -15f, false)
                    };
                    foreach (var (ox, oz, ch, tiltX, tiltZ, crimson) in shards)
                    {
                        Color veinCol = crimson ? prtsCrimson : origAmber;
                        new Entity(parent: root, model: "diamond", scale: new(1.38f, ch, 1.38f),
                            position: new(ox, ch * 0.44f, oz), rotation: new(tiltX, 25f, tiltZ), color: origBlack);
                        new Entity(parent: root, model: "diamond", scale: new(0.62f, ch * 0.92f, 0.62f),
                            position: new(ox + 0.16f, ch * 0.48f, oz + 0.14f), rotation: new(tiltX, 70f, tiltZ), color: veinCol, unlit: true);
                    }

                    // Парящий нейросетевой ромб PRTS ◇ над вершиной монолита
                    Vector3 rhombPos = new(0, h + 1.35f, 0);
                    new Entity(parent: root, model: "diamond", scale: new(2.65f, 3.35f, 0.28f), position: rhombPos, color: prtsCrimson, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(1.55f, 2.05f, 0.36f), position: rhombPos, color: origBlack);
                    new Entity(parent: root, model: "diamond", scale: new(0.72f, 0.98f, 0.44f), position: rhombPos, color: Color.White, unlit: true);

                    obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.35f, h + 1.0f, pos.Y));
                    outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.60f, 2.60f, rotY, 0.50f, pos.Y, walkableTop: true));
                    outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 1.95f, 1.65f, rotY, h, pos.Y, walkableTop: false));
                    return true;
                }

                case "penacony_organ":
                {
                    // Золотая органная арка Большого Театра Пенаконии с парящими нимбами Галовианцев и знаком ♫
                    float h = NextRange(11.0f, 13.8f);
                    root.AllowObstructionFade = true;
                    root.ObstructionRadius = 5.6f;
                    root.ObstructionHeight = h + 2.8f;

                    Color velvetRed = Rgb(148, 26, 48);
                    Color marbleCream = Rgb(238, 230, 218);
                    Color organGold = Rgb(238, 194, 78);
                    Color brightGold = Rgb(255, 228, 132);
                    Color harmonyLavender = Rgb(192, 145, 255);

                    // Мраморно-бархатный подиум оперной арки
                    new Entity(parent: root, model: "cube", scale: new(9.2f, 0.48f, 3.6f), position: new(0, 0.24f, 0), color: marbleCream);
                    new Entity(parent: root, model: "cube", scale: new(8.4f, 0.14f, 3.0f), position: new(0, 0.52f, 0), color: velvetRed);

                    // Две симметричные башни золотых органных труб по бокам просцениума
                    foreach (float sideSign in new[] { -1f, 1f })
                    {
                        float towerX = sideSign * 3.35f;
                        new Entity(parent: root, model: "cube", scale: new(2.35f, 2.2f, 2.2f), position: new(towerX, 1.55f, 0), color: Rgb(68, 42, 62));
                        new Entity(parent: root, model: "cube", scale: new(2.50f, 0.24f, 2.34f), position: new(towerX, 2.70f, 0), color: organGold);

                        for (int p = -2; p <= 2; p++)
                        {
                            float pipeX = towerX + p * 0.42f;
                            float pipeH = (h - 2.6f) * (1.0f - MathF.Abs(p) * 0.11f);
                            new Entity(parent: root, model: "cube", scale: new(0.30f, pipeH, 0.34f),
                                position: new(pipeX, 2.8f + pipeH * 0.5f, (p % 2) * 0.06f),
                                color: p % 2 == 0 ? brightGold : organGold, unlit: p == 0);
                        }

                        var (twx, twz) = ToWorld(towerX, 0f);
                        obstacles.Add(new ObstacleCircle(twx, twz, 1.35f, h, pos.Y));
                        outBoxes?.Add(new ObstacleBox(twx, twz, 1.25f, 1.15f, rotY, h, pos.Y, walkableTop: false));
                    }

                    // Золотой свод просцениума и бархатный ламбрекен
                    new Entity(parent: root, model: "cube", scale: new(9.4f, 1.25f, 2.4f), position: new(0, h + 0.35f, 0), color: organGold);
                    new Entity(parent: root, model: "cube", scale: new(5.2f, 0.85f, 2.56f), position: new(0, h - 0.45f, 0), color: velvetRed);
                    new Entity(parent: root, model: "diamond", scale: new(3.8f, 1.65f, 1.2f), position: new(0, h + 1.55f, 0), color: brightGold, unlit: true);

                    // Парящие кольца Галовианцев и золотая нота Гармонии ♫ в центре арки
                    float noteY = h * 0.56f;
                    new Entity(parent: root, model: "diamond", scale: new(4.1f, 0.18f, 4.1f),
                        position: new(0, noteY + 0.6f, 0), rotation: new(20f, 0, -14f), color: harmonyLavender, unlit: true);
                    new Entity(parent: root, model: "diamond", scale: new(3.3f, 0.16f, 3.3f),
                        position: new(0, noteY + 0.6f, 0), rotation: new(-18f, 45f, 16f), color: brightGold, unlit: true);
                    // Двойная восьмая нота ♫ из сфер и штилей
                    foreach (float nx in new[] { -0.52f, 0.52f })
                    {
                        new Entity(parent: root, model: "sphere", scale: new(0.56f, 0.42f, 0.38f), position: new(nx - 0.12f, noteY - 0.35f, 0), color: brightGold, unlit: true);
                        new Entity(parent: root, model: "cube", scale: new(0.14f, 1.35f, 0.14f), position: new(nx + 0.10f, noteY + 0.30f, 0), color: brightGold, unlit: true);
                    }
                    new Entity(parent: root, model: "cube", scale: new(1.22f, 0.22f, 0.16f),
                        position: new(0.10f, noteY + 0.92f, 0), rotation: new(0, 0, 10f), color: brightGold, unlit: true);

                    outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 4.60f, 1.80f, rotY, 0.56f, pos.Y, walkableTop: true));
                    outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 4.70f, 1.22f, rotY, 1.45f, pos.Y + h - 0.3f, walkableTop: true));
                    return true;
                }

                case "aurelionite_geode":
                {
                    // Расколотая грозовая порода Меридиана, полная золотых кристаллов Аурелионита и лунных молний
                    float h = NextRange(6.8f, 9.2f);
                    root.AllowObstructionFade = true;
                    root.ObstructionRadius = 4.0f;
                    root.ObstructionHeight = h + 2.2f;

                    Color stormSlate = Rgb(48, 58, 80);
                    Color stormDark = Rgb(30, 36, 52);
                    Color aurelGold = Rgb(255, 208, 58);
                    Color lunarBolt = Rgb(90, 222, 255);

                    // Две расколотые каменные скорлупы жеоды
                    new Entity(parent: root, model: "sphere", scale: new(4.8f, 1.4f, 4.6f), position: new(0, 0.55f, 0), color: stormDark);
                    foreach (float shellSign in new[] { -1f, 1f })
                    {
                        new Entity(parent: root, model: "cube", scale: new(1.65f, h * 0.68f, 3.2f),
                            position: new(shellSign * 1.45f, h * 0.34f, 0),
                            rotation: new(0, 0, shellSign * -16f), color: stormSlate);
                    }

                    // Сияющие золотые кристаллы Аурелионита, растущие из разлома
                    (float gx, float gz, float gh, float rx, float rz)[] goldShards =
                    {
                        (0.0f, 0.0f, h * 0.96f, 4f, -4f),
                        (-0.75f, 0.65f, h * 0.72f, -16f, 14f),
                        (0.80f, -0.55f, h * 0.76f, 15f, -18f),
                        (0.35f, 0.90f, h * 0.58f, 20f, 8f),
                        (-0.45f, -0.85f, h * 0.62f, -18f, -12f)
                    };
                    foreach (var (gx, gz, gh, rx, rz) in goldShards)
                    {
                        new Entity(parent: root, model: "diamond", scale: new(1.18f, gh, 1.18f),
                            position: new(gx, gh * 0.46f + 0.4f, gz),
                            rotation: new(rx, NextRange(0f, 180f), rz), color: aurelGold, unlit: true);
                    }

                    // Парящая золотая корона Аурелионита и осколки кобальтовых лунных молний
                    new Entity(parent: root, model: "diamond", scale: new(3.4f, 0.24f, 3.4f),
                        position: new(0, h * 0.78f, 0), rotation: new(14f, 30f, -12f), color: aurelGold, unlit: true);
                    foreach (float boltSign in new[] { -1f, 1f })
                    {
                        new Entity(parent: root, model: "diamond", scale: new(0.48f, h * 0.52f, 0.48f),
                            position: new(boltSign * 1.65f, h * 0.66f, boltSign * 0.55f),
                            rotation: new(boltSign * 24f, 35f, boltSign * -28f), color: lunarBolt, unlit: true);
                    }
                    new Entity(parent: root, model: "sphere", scaleUniform: 3.6f, position: new(0, h * 0.48f, 0),
                        color: Rgb(255, 210, 75, 58), unlit: true);

                    obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.30f, h * 0.92f, pos.Y));
                    outBoxes?.Add(new ObstacleBox(pos.X, pos.Z, 2.15f, 1.85f, rotY, h * 0.88f, pos.Y, walkableTop: false));
                    return true;
                }

                default:
                    return false;
            }
        }
    }
}
