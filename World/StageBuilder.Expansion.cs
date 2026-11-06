using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using RiskOfCSharp2.Models;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.World
{
    public static partial class StageBuilder
    {
        private static void BuildExpansionSecretArenaGeometry(List<Entity> entitiesList, StagePalette palette, string specialMode, Random rng)
        {
            switch (specialMode)
            {
                case "dark_sanctuary":
                {
                    // Тёмное Святилище Пророчества (Deltarune): руна Дельта на полу, готический алтарь Великого Фонтана и хоровые балконы
                    Color gothicDark = Rgb(26, 22, 52);
                    Color gothicStone = Rgb(48, 42, 92);
                    Color fountainCyan = Rgb(75, 245, 245);
                    Color fountainViolet = Rgb(165, 68, 255);
                    Color crimsonCarpet = Rgb(148, 32, 76);

                    // Ковровая дорожка и светящаяся Руна Дельта в центре зала (без коллайдера)
                    entitiesList.Add(new Entity(model: "cube", scale: new(5.4f, 0.03f, 54f), position: new(0, 0.015f, 4f), color: crimsonCarpet));
                    entitiesList.Add(new Entity(model: "diamond", scale: new(13.5f, 0.04f, 13.5f), position: new(0, 0.025f, 4f), color: Rgb(44, 36, 96), unlit: true));
                    entitiesList.Add(new Entity(model: "sphere", scale: new(3.2f, 0.05f, 3.2f), position: new(0, 0.035f, 7.2f), color: fountainCyan, unlit: true));
                    foreach (float wx in new[] { -4.6f, 4.6f })
                        entitiesList.Add(new Entity(model: "diamond", scale: new(4.2f, 0.05f, 1.5f), position: new(wx, 0.035f, 6.8f), rotation: new(0, 0, wx < 0 ? 18f : -18f), color: fountainViolet, unlit: true));
                    foreach (float tx in new[] { -2.8f, 0f, 2.8f })
                        entitiesList.Add(new Entity(model: "diamond", scale: new(1.6f, 0.05f, 2.2f), position: new(tx, 0.035f, 2.2f + (tx == 0f ? -1.2f : 0f)), color: fountainCyan, unlit: true));

                    // Северный готический алтарь и бьющий в небо Великий Тёмный Фонтан
                    var nave = new Entity(position: new(0f, 0f, 37f));
                    new Entity(parent: nave, model: "cube", scale: new(26f, 1.0f, 11f), position: new(0, 0.50f, 0), color: gothicDark);
                    new Entity(parent: nave, model: "cube", scale: new(16f, 0.9f, 6.8f), position: new(0, 1.45f, 1.4f), color: gothicStone);
                    new Entity(parent: nave, model: "diamond", scale: new(16.6f, 0.14f, 7.2f), position: new(0, 1.92f, 1.4f), color: fountainCyan, unlit: true);
                    foreach (float bx in new[] { -10.5f, 10.5f })
                    {
                        new Entity(parent: nave, model: "cube", scale: new(3.2f, 14.5f, 3.2f), position: new(bx, 7.25f, 5.2f), color: gothicStone);
                        new Entity(parent: nave, model: "diamond", scale: new(2.2f, 3.6f, 2.2f), position: new(bx, 16.0f, 5.2f), color: gothicDark);
                        new Entity(parent: nave, model: "cube", scale: new(0.85f, 10.5f, 3.38f), position: new(bx, 7.5f, 5.2f), color: fountainViolet, unlit: true);
                    }
                    new Entity(parent: nave, model: "cube", scale: new(24.5f, 2.2f, 2.6f), position: new(0, 13.8f, 5.2f), color: gothicDark);
                    new Entity(parent: nave, model: "sphere", scale: new(5.4f, 5.4f, 0.48f), position: new(0, 10.2f, 5.2f), color: fountainCyan, unlit: true);
                    // Столб Великого Фонтана позади алтаря
                    new Entity(parent: nave, model: "diamond", scale: new(4.2f, 28f, 4.2f), position: new(0, 14.5f, 6.8f), color: Rgb(135, 65, 255, 185), unlit: true);
                    new Entity(parent: nave, model: "cube", scale: new(1.25f, 30f, 1.25f), position: new(0, 15.0f, 6.8f), color: Color.White, unlit: true);
                    entitiesList.Add(nave);

                    Physics.AddSolidBox(0f, 37f, 26f, 11f, 1.0f, 0f, 0f, walkableTop: true);
                    Physics.AddSolidBox(0f, 38.4f, 16f, 6.8f, 0.9f, 0f, 1.0f, walkableTop: true);
                    foreach (float bx in new[] { -10.5f, 10.5f })
                        Physics.AddSolidBox(bx, 42.2f, 3.2f, 3.2f, 15.5f, 0f, 0f, walkableTop: false);
                    Physics.RegisterSpawnBlocker(new Vector3(0f, 0f, 43.8f), 3.0f, 24f);

                    // Четыре готических шпиля Тёмного Фонтана по углам нефа + парящие балконы с трамплинами
                    foreach (var (sx, sz, syaw) in new[] { (-28f, 24f, 35f), (28f, 24f, -35f), (-28f, -15f, 145f), (28f, -15f, -145f) })
                    {
                        var (spEnt, spObs) = WorldStructuresBuilder.BuildMapStructure(null, "dark_fountain_spire", palette, new Vector3(sx, 0f, sz), syaw, rng, Physics.BoxObstacles);
                        entitiesList.Add(spEnt);
                        Physics.Obstacles.AddRange(spObs);
                        Physics.RegisterSpawnBlocker(new Vector3(sx, 0f, sz), 3.0f, 14f);
                    }

                    foreach (float sideX in new[] { -24f, 24f })
                    {
                        SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(sideX, 3.8f, 5f), 5.8f, 1.1f, gothicStone, fountainCyan);
                        entitiesList.Add(new LunarBouncePad(new Vector3(sideX, 0f, -5.5f), 17.5f, 5.5f));
                    }
                    break;
                }

                case "terminal_lucidity":
                {
                    // Свалка Кибер-Мира (Deltarune Ch.2): эстакада Особняка Королевы, мегаборд [[BIG SHOT]], рельсы американских горок и [Pipis]
                    Color cyberDark = Rgb(22, 18, 38);
                    Color spamPink = Rgb(255, 95, 205);
                    Color spamYellow = Rgb(255, 235, 65);
                    Color neonGreen = Rgb(65, 245, 135);
                    Color pipisBlue = Rgb(48, 195, 255);

                    // Неоновые дорожные полосы Кибер-Шоссе на полу
                    foreach (float rx in new[] { -18.5f, 18.5f })
                    {
                        entitiesList.Add(new Entity(model: "cube", scale: new(0.28f, 0.04f, 68f), position: new(rx, 0.02f, 2f), color: rx < 0 ? spamPink : spamYellow, unlit: true));
                    }
                    for (int d = -3; d <= 3; d++)
                        entitiesList.Add(new Entity(model: "cube", scale: new(0.45f, 0.04f, 3.2f), position: new(0, 0.025f, d * 7.5f), color: neonGreen, unlit: true));

                    // Северная эстакада Кибер-Лавки Спамтона и гигантский неоновый фасад [[1997]]
                    var overpass = new Entity(position: new(0f, 0f, 36f));
                    new Entity(parent: overpass, model: "cube", scale: new(28f, 1.15f, 9.5f), position: new(0, 0.58f, 0), color: Rgb(36, 42, 58));
                    new Entity(parent: overpass, model: "cube", scale: new(28.2f, 0.14f, 0.32f), position: new(0, 1.18f, -4.65f), color: neonGreen, unlit: true);
                    new Entity(parent: overpass, model: "cube", scale: new(15f, 5.0f, 6.4f), position: new(0, 3.65f, 4.6f), color: cyberDark);
                    new Entity(parent: overpass, model: "cube", scale: new(18.5f, 4.4f, 0.75f), position: new(0, 9.2f, 3.2f), color: Rgb(16, 14, 28));
                    new Entity(parent: overpass, model: "cube", scale: new(18.9f, 0.24f, 0.85f), position: new(0, 11.45f, 3.2f), color: spamPink, unlit: true);
                    new Entity(parent: overpass, model: "cube", scale: new(18.9f, 0.24f, 0.85f), position: new(0, 6.95f, 3.2f), color: spamYellow, unlit: true);
                    new Entity(parent: overpass, model: "sphere", scale: new(3.1f, 3.1f, 0.45f), position: new(-2.5f, 9.2f, 2.75f), color: spamPink, unlit: true);
                    new Entity(parent: overpass, model: "sphere", scale: new(3.1f, 3.1f, 0.45f), position: new(2.5f, 9.2f, 2.75f), color: spamYellow, unlit: true);
                    new Entity(parent: overpass, model: "cube", scale: new(1.2f, 0.30f, 0.42f), position: new(0, 9.35f, 2.78f), color: Color.White, unlit: true);
                    // Зелёные кукольные провода [[НЕО]], уходящие высоко в темноту
                    for (int w = -3; w <= 3; w++)
                        new Entity(parent: overpass, model: "cube", scale: new(0.10f, 16f, 0.10f), position: new(w * 2.4f, 16.5f, 4.2f), rotation: new(0, 0, w * 3.5f), color: neonGreen, unlit: true);
                    entitiesList.Add(overpass);

                    Physics.AddSolidBox(0f, 36f, 28f, 9.5f, 1.15f, 0f, 0f, walkableTop: true);
                    Physics.AddSolidBox(0f, 40.6f, 15f, 6.4f, 6.15f, 0f, 0f, walkableTop: true);
                    Physics.RegisterSpawnBlocker(new Vector3(0f, 0f, 40.6f), 4.2f, 12f);

                    // Четыре неоновых рекламных щита с мусорными баками и [Pipis] по бокам арены
                    foreach (var (bx, bz, byaw) in new[] { (-27f, 22f, 55f), (27f, 22f, -55f), (-27f, -16f, 125f), (27f, -16f, -125f) })
                    {
                        var (bEnt, bObs) = WorldStructuresBuilder.BuildMapStructure(null, "pipis_billboard", palette, new Vector3(bx, 0f, bz), byaw, rng, Physics.BoxObstacles);
                        entitiesList.Add(bEnt);
                        Physics.Obstacles.AddRange(bObs);
                        Physics.RegisterSpawnBlocker(new Vector3(bx, 0f, bz), 3.4f, 11f);
                    }

                    // Западная витражная розетка Третьего Глаза Тирэйден (Сатори и Коиши) и астральные вены подсознания
                    var chireiden = new Entity(position: new(-18f, 0f, 34f));
                    new Entity(parent: chireiden, model: "cube", scale: new(8.5f, 11.5f, 1.4f), position: new(0, 5.75f, 0), color: Rgb(58, 24, 52));
                    new Entity(parent: chireiden, model: "sphere", scale: new(5.2f, 5.2f, 0.45f), position: new(0, 8.2f, -0.65f), color: Rgb(245, 65, 145), unlit: true);
                    new Entity(parent: chireiden, model: "sphere", scale: new(2.4f, 2.4f, 0.55f), position: new(0, 8.2f, -0.72f), color: Rgb(65, 225, 185), unlit: true);
                    for (int cord = -2; cord <= 2; cord++)
                        new Entity(parent: chireiden, model: "cube", scale: new(0.16f, 9.5f, 0.16f), position: new(cord * 1.4f, 5.5f, -0.8f), rotation: new(0, 0, cord * 14f), color: cord % 2 == 0 ? Rgb(235, 45, 95) : Rgb(65, 165, 255), unlit: true);
                    entitiesList.Add(chireiden);
                    Physics.AddSolidBox(-18f, 34f, 8.5f, 1.4f, 11.5f, 0f, 0f, walkableTop: false);

                    // Платформы вагонеток американских горок и контейнеры с [Pipis]
                    foreach (float sideX in new[] { -23f, 23f })
                    {
                        SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(sideX, 4.0f, 3f), 5.6f, 1.1f, cyberDark, sideX < 0 ? spamPink : spamYellow);
                        entitiesList.Add(new LunarBouncePad(new Vector3(sideX, 0f, -6.5f), 18.0f, 5.5f));

                        float crateX = sideX < 0 ? -34f : 34f;
                        var pStack = new Entity(position: new(crateX, 0f, 3f));
                        new Entity(parent: pStack, model: "cube", scale: new(3.6f, 1.8f, 3.6f), position: new(0, 0.9f, 0), color: Rgb(36, 98, 64));
                        new Entity(parent: pStack, model: "sphere", scale: new(1.35f, 1.05f, 1.15f), position: new(0, 2.25f, 0), color: pipisBlue, unlit: true);
                        entitiesList.Add(pStack);
                        Physics.AddSolidBox(crateX, 3f, 3.6f, 3.6f, 1.8f, 0f, 0f, walkableTop: true);
                    }
                    break;
                }

                case "extraction_floor":
                {
                    // Этаж Философии и Извлечения (Project Moon): золотые цепи Арбитра Бины и алая карусель Ла-Манчаленда Дон Кихот
                    Color arbiterBlack = Rgb(22, 20, 24);
                    Color arbiterGold = Rgb(255, 208, 68);
                    Color bloodCrimson = Rgb(205, 34, 58);

                    // Схема Древа Сефирот и кольца Замка/Крови на полу (без коллайдера)
                    entitiesList.Add(new Entity(model: "diamond", scale: new(9.5f, 0.04f, 9.5f), position: new(-11f, 0.025f, 15f), color: arbiterGold, unlit: true));
                    entitiesList.Add(new Entity(model: "diamond", scale: new(9.5f, 0.04f, 9.5f), position: new(11f, 0.025f, 15f), color: bloodCrimson, unlit: true));
                    entitiesList.Add(new Entity(model: "cube", scale: new(22f, 0.04f, 0.32f), position: new(0f, 0.02f, 15f), color: arbiterGold, unlit: true));
                    entitiesList.Add(new Entity(model: "cube", scale: new(0.32f, 0.04f, 52f), position: new(0f, 0.025f, 4f), color: arbiterGold, unlit: true));

                    // Северный балкон Этажа Философии и резервуар Когито с Золотой Ветвью
                    var terrace = new Entity(position: new(0f, 0f, 38f));
                    new Entity(parent: terrace, model: "cube", scale: new(30f, 1.15f, 10.5f), position: new(0, 0.58f, 0), color: arbiterBlack);
                    new Entity(parent: terrace, model: "cube", scale: new(30.2f, 0.14f, 10.7f), position: new(0, 1.18f, 0), color: arbiterGold, unlit: true);
                    // Западный павильон: чайный столик Бины; Восточный павильон: штандарт Ла-Манчаленда
                    new Entity(parent: terrace, model: "cube", scale: new(3.2f, 0.95f, 3.2f), position: new(-8.5f, 1.65f, 0.5f), color: Rgb(34, 30, 38));
                    new Entity(parent: terrace, model: "diamond", scale: new(1.8f, 0.22f, 1.8f), position: new(-8.5f, 2.25f, 0.5f), color: arbiterGold, unlit: true);
                    new Entity(parent: terrace, model: "cube", scale: new(0.45f, 7.5f, 0.45f), position: new(8.5f, 4.9f, 0.5f), color: arbiterGold);
                    new Entity(parent: terrace, model: "diamond", scale: new(2.6f, 4.2f, 0.32f), position: new(8.5f, 6.2f, 0.5f), color: bloodCrimson, unlit: true);
                    // Центральная колонна Извлечения Когито и сияющая Золотая Ветвь
                    new Entity(parent: terrace, model: "cube", scale: new(5.8f, 15.5f, 5.8f), position: new(0, 7.75f, 5.6f), color: arbiterBlack);
                    new Entity(parent: terrace, model: "cube", scale: new(3.8f, 11.5f, 0.45f), position: new(0, 7.8f, 2.65f), color: Rgb(225, 48, 72, 185), unlit: true);
                    new Entity(parent: terrace, model: "diamond", scale: new(2.8f, 5.4f, 2.8f), position: new(0, 17.5f, 5.6f), color: arbiterGold, unlit: true);
                    entitiesList.Add(terrace);

                    Physics.AddSolidBox(0f, 38f, 30f, 10.5f, 1.15f, 0f, 0f, walkableTop: true);
                    Physics.AddSolidBox(-8.5f, 38.5f, 3.2f, 3.2f, 1.0f, 0f, 1.15f, walkableTop: true);
                    Physics.AddSolidBox(0f, 43.6f, 5.8f, 5.8f, 16.5f, 0f, 0f, walkableTop: false);
                    Physics.RegisterSpawnBlocker(new Vector3(0f, 0f, 43.6f), 3.6f, 18f);

                    // Четыре клети Клипота по периметру и парящие читальные балконы Библиотеки
                    foreach (var (qx, qz, qyaw) in new[] { (-28f, 24f, 30f), (28f, 24f, -30f), (-28f, -14f, 150f), (28f, -14f, -150f) })
                    {
                        var (qEnt, qObs) = WorldStructuresBuilder.BuildMapStructure(null, "qliphoth_cage", palette, new Vector3(qx, 0f, qz), qyaw, rng, Physics.BoxObstacles);
                        entitiesList.Add(qEnt);
                        Physics.Obstacles.AddRange(qObs);
                        Physics.RegisterSpawnBlocker(new Vector3(qx, 0f, qz), 3.0f, 13f);
                    }

                    SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(-24f, 3.8f, 4f), 5.8f, 1.1f, arbiterBlack, arbiterGold);
                    SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(24f, 3.8f, 4f), 5.8f, 1.1f, Rgb(54, 18, 28), bloodCrimson);
                    entitiesList.Add(new LunarBouncePad(new Vector3(-23f, 0f, -5.5f), 17.5f, 5.5f));
                    entitiesList.Add(new LunarBouncePad(new Vector3(23f, 0f, -5.5f), 17.5f, 5.5f));
                    break;
                }

                case "originium_core":
                {
                    // Ассимилированное Ядро Саркофага PRTS ◇ (Arknights): врата Первой Цивилизации, шины данных и доки Родос Айленд
                    Color rhodesSlate = Rgb(42, 50, 64);
                    Color origBlack = Rgb(18, 18, 24);
                    Color prtsRed = Rgb(255, 58, 72);
                    Color rhodesCyan = Rgb(72, 215, 255);

                    // Тактическая разметка палубы «Родос Айленд» и красные шины данных PRTS ◇ на полу
                    entitiesList.Add(new Entity(model: "diamond", scale: new(12f, 0.04f, 12f), position: new(0f, 0.02f, 6f), color: rhodesSlate));
                    entitiesList.Add(new Entity(model: "diamond", scale: new(7.5f, 0.05f, 9.5f), position: new(0f, 0.03f, 6f), color: prtsRed, unlit: true));
                    foreach (float busX in new[] { -11f, 11f })
                        entitiesList.Add(new Entity(model: "cube", scale: new(0.32f, 0.04f, 46f), position: new(busX, 0.025f, 10f), color: busX < 0 ? prtsRed : rhodesCyan, unlit: true));

                    // Северный помост Саркофага Первой Цивилизации и парящее Око PRTS ◇
                    var sarc = new Entity(position: new(0f, 0f, 37f));
                    new Entity(parent: sarc, model: "cube", scale: new(28f, 1.2f, 10.5f), position: new(0, 0.60f, 0), color: rhodesSlate);
                    new Entity(parent: sarc, model: "cube", scale: new(28.2f, 0.14f, 0.30f), position: new(0, 1.22f, -5.15f), color: rhodesCyan, unlit: true);
                    foreach (float gx in new[] { -7.5f, 7.5f })
                    {
                        new Entity(parent: sarc, model: "cube", scale: new(3.4f, 15.0f, 3.8f), position: new(gx, 7.5f, 5.4f), color: rhodesSlate);
                        new Entity(parent: sarc, model: "cube", scale: new(0.65f, 12.5f, 4.0f), position: new(gx, 7.8f, 5.4f), color: prtsRed, unlit: true);
                    }
                    new Entity(parent: sarc, model: "cube", scale: new(19.5f, 2.6f, 4.2f), position: new(0, 15.2f, 5.4f), color: origBlack);
                    new Entity(parent: sarc, model: "cube", scale: new(10.5f, 11.8f, 1.2f), position: new(0, 7.4f, 5.8f), color: Rgb(62, 208, 255, 145), unlit: true);
                    // Гигантский красный ромб «Глаза Пристэсс» ◇ над Саркофагом
                    new Entity(parent: sarc, model: "diamond", scale: new(6.4f, 8.6f, 0.55f), position: new(0, 14.2f, 3.2f), color: prtsRed, unlit: true);
                    new Entity(parent: sarc, model: "diamond", scale: new(3.6f, 5.0f, 0.72f), position: new(0, 14.2f, 3.2f), color: origBlack);
                    new Entity(parent: sarc, model: "diamond", scale: new(1.4f, 2.1f, 0.88f), position: new(0, 14.2f, 3.2f), color: Color.White, unlit: true);
                    entitiesList.Add(sarc);

                    Physics.AddSolidBox(0f, 37f, 28f, 10.5f, 1.2f, 0f, 0f, walkableTop: true);
                    foreach (float gx in new[] { -7.5f, 7.5f })
                        Physics.AddSolidBox(gx, 42.4f, 3.4f, 3.8f, 16.0f, 0f, 0f, walkableTop: false);
                    Physics.AddSolidBox(0f, 42.8f, 10.5f, 1.6f, 13.5f, 0f, 0f, walkableTop: false);
                    Physics.RegisterSpawnBlocker(new Vector3(0f, 0f, 42.4f), 4.0f, 18f);

                    // Четыре монолита Ассимилированной Вселенной и боковые инженерные платформы дронов Кложур
                    foreach (var (ox, oz, oyaw) in new[] { (-28f, 24f, 25f), (28f, 24f, -25f), (-28f, -15f, 155f), (28f, -15f, -155f) })
                    {
                        var (oEnt, oObs) = WorldStructuresBuilder.BuildMapStructure(null, "originium_monolith", palette, new Vector3(ox, 0f, oz), oyaw, rng, Physics.BoxObstacles);
                        entitiesList.Add(oEnt);
                        Physics.Obstacles.AddRange(oObs);
                        Physics.RegisterSpawnBlocker(new Vector3(ox, 0f, oz), 3.0f, 13f);
                    }

                    SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(-24f, 3.8f, 4f), 5.8f, 1.1f, origBlack, prtsRed);
                    SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(24f, 3.8f, 4f), 5.8f, 1.1f, rhodesSlate, rhodesCyan);
                    entitiesList.Add(new LunarBouncePad(new Vector3(-22.5f, 0f, -6f), 17.5f, 5.5f));
                    entitiesList.Add(new LunarBouncePad(new Vector3(22.5f, 0f, -6f), 17.5f, 5.5f));
                    break;
                }

                case "penacony_theater":
                {
                    // Большой Театр Пенаконии (Honkai: Star Rail): сцена Фестиваля Гармонии, золотой кафедральный орган и ложи Семьи
                    Color marbleCream = Rgb(238, 230, 218);
                    Color velvetCrimson = Rgb(148, 26, 48);
                    Color organGold = Rgb(242, 198, 82);
                    Color harmonyLavender = Rgb(192, 145, 255);

                    // Золотой нотный стан и кольца Гармонии на полу зрительного зала
                    for (int st = -2; st <= 2; st++)
                        entitiesList.Add(new Entity(model: "cube", scale: new(32f, 0.03f, 0.14f), position: new(0f, 0.02f, 6f + st * 1.4f), color: organGold, unlit: true));
                    entitiesList.Add(new Entity(model: "diamond", scale: new(16f, 0.04f, 16f), position: new(0f, 0.025f, 15f), color: Rgb(192, 145, 255, 120), unlit: true));

                    // Главная оперная сцена Фестиваля Гармонии на севере
                    var opera = new Entity(position: new(0f, 0f, 36f));
                    new Entity(parent: opera, model: "cube", scale: new(28f, 1.15f, 11f), position: new(0, 0.58f, 0), color: marbleCream);
                    new Entity(parent: opera, model: "cube", scale: new(26.5f, 0.12f, 9.8f), position: new(0, 1.18f, 0), color: velvetCrimson);
                    new Entity(parent: opera, model: "cube", scale: new(15f, 0.75f, 6.2f), position: new(0, 1.55f, 1.5f), color: marbleCream);
                    foreach (float cx in new[] { -11.5f, 11.5f })
                    {
                        new Entity(parent: opera, model: "cube", scale: new(3.4f, 13.5f, 2.2f), position: new(cx, 7.2f, 4.2f), color: velvetCrimson);
                        new Entity(parent: opera, model: "cube", scale: new(3.6f, 0.45f, 2.4f), position: new(cx, 14.0f, 4.2f), color: organGold);
                    }
                    // Центральные золотые трубы органа Хора Септимия
                    for (int p = -4; p <= 4; p++)
                    {
                        float ph = 13.8f - MathF.Abs(p) * 1.05f;
                        new Entity(parent: opera, model: "cube", scale: new(0.85f, ph, 0.95f),
                            position: new(p * 1.55f, 1.9f + ph * 0.5f, 4.8f), color: p % 2 == 0 ? Color.Gold : organGold, unlit: p == 0);
                    }
                    new Entity(parent: opera, model: "diamond", scale: new(9.5f, 0.28f, 9.5f), position: new(0, 13.8f, 3.2f), rotation: new(24f, 0, 0), color: harmonyLavender, unlit: true);
                    new Entity(parent: opera, model: "sphere", scaleUniform: 1.45f, position: new(0, 13.8f, 3.2f), color: Color.Gold, unlit: true);
                    entitiesList.Add(opera);

                    Physics.AddSolidBox(0f, 36f, 28f, 11f, 1.15f, 0f, 0f, walkableTop: true);
                    Physics.AddSolidBox(0f, 37.5f, 15f, 6.2f, 0.75f, 0f, 1.15f, walkableTop: true);
                    foreach (float cx in new[] { -11.5f, 11.5f })
                        Physics.AddSolidBox(cx, 40.2f, 3.4f, 2.2f, 14.2f, 0f, 0f, walkableTop: false);
                    Physics.AddSolidBox(0f, 40.8f, 15.0f, 1.4f, 15.5f, 0f, 0f, walkableTop: false);
                    Physics.RegisterSpawnBlocker(new Vector3(0f, 0f, 40.5f), 4.0f, 16f);

                    // Четыре золотые органные арки вдоль боковых кулис и парящие ложи VIP-гостей
                    foreach (var (px, pz, pyaw) in new[] { (-30f, 20f, 90f), (30f, 20f, -90f), (-30f, -12f, 90f), (30f, -12f, -90f) })
                    {
                        var (pEnt, pObs) = WorldStructuresBuilder.BuildMapStructure(null, "penacony_organ", palette, new Vector3(px, 0f, pz), pyaw, rng, Physics.BoxObstacles);
                        entitiesList.Add(pEnt);
                        Physics.Obstacles.AddRange(pObs);
                        Physics.RegisterSpawnBlocker(new Vector3(px, 0f, pz), 3.6f, 13f);
                    }

                    foreach (float sideX in new[] { -21.5f, 21.5f })
                    {
                        SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(sideX, 4.2f, 4f), 5.6f, 1.1f, velvetCrimson, organGold);
                        entitiesList.Add(new LunarBouncePad(new Vector3(sideX, 0f, -6.0f), 18.5f, 5.5f));
                    }
                    break;
                }

                case "prime_meridian":
                {
                    // Главный Меридиан (Risk of Rain 2: Seekers of the Storm): корона Колосса Провидения, жеоды Аурелионита и грозовые уступы
                    Color stormSlate = Rgb(48, 58, 80);
                    Color stormDark = Rgb(30, 36, 52);
                    Color aurelGold = Rgb(255, 208, 58);
                    Color lunarBolt = Rgb(90, 222, 255);

                    // Золотые и кобальтовые грозовые разломы на камнях Меридиана (без коллайдера)
                    for (int ray = 0; ray < 6; ray++)
                    {
                        float ang = ray * (MathF.PI / 3f) + 0.25f;
                        float lx = MathF.Cos(ang) * 14f, lz = 10f + MathF.Sin(ang) * 14f;
                        entitiesList.Add(new Entity(model: "cube", scale: new(0.42f, 0.04f, 10.5f),
                            position: new(lx, 0.025f, lz), rotation: new(0, -ang * (180f / MathF.PI), 0),
                            color: ray % 2 == 0 ? aurelGold : lunarBolt, unlit: true));
                    }

                    // Северное святилище Короны Колосса и Сердце Аурелионита
                    var crown = new Entity(position: new(0f, 0f, 38f));
                    new Entity(parent: crown, model: "cube", scale: new(26f, 1.25f, 11f), position: new(0, 0.625f, 0), color: stormDark);
                    new Entity(parent: crown, model: "cube", scale: new(16f, 1.10f, 6.8f), position: new(0, 1.80f, 1.4f), color: stormSlate);
                    foreach (float hx in new[] { -8.5f, 8.5f })
                    {
                        new Entity(parent: crown, model: "cube", scale: new(3.4f, 15.5f, 3.4f), position: new(hx, 7.75f, 4.8f),
                            rotation: new(0, 0, hx < 0 ? -12f : 12f), color: stormSlate);
                        new Entity(parent: crown, model: "diamond", scale: new(1.8f, 6.5f, 1.8f), position: new(hx * 0.88f, 16.2f, 4.8f), color: lunarBolt, unlit: true);
                    }
                    new Entity(parent: crown, model: "diamond", scale: new(4.8f, 7.2f, 4.8f), position: new(0, 10.8f, 4.8f), color: aurelGold, unlit: true);
                    new Entity(parent: crown, model: "diamond", scale: new(8.4f, 0.35f, 8.4f), position: new(0, 10.8f, 4.8f), rotation: new(18f, 35f, -16f), color: lunarBolt, unlit: true);
                    entitiesList.Add(crown);

                    Physics.AddSolidBox(0f, 38f, 26f, 11f, 1.25f, 0f, 0f, walkableTop: true);
                    Physics.AddSolidBox(0f, 39.4f, 16f, 6.8f, 1.10f, 0f, 1.25f, walkableTop: true);
                    foreach (float hx in new[] { -8.5f, 8.5f })
                        Physics.AddSolidBox(hx, 42.8f, 3.6f, 3.6f, 16.0f, 0f, 0f, walkableTop: false);
                    Physics.RegisterSpawnBlocker(new Vector3(0f, 0f, 42.8f), 4.0f, 18f);

                    // Четыре грозовые жеоды Аурелионита и двухъярусные скальные уступы для паркура
                    foreach (var (gx, gz, gyaw) in new[] { (-27f, 24f, 40f), (27f, 24f, -40f), (-27f, -14f, 140f), (27f, -14f, -140f) })
                    {
                        var (gEnt, gObs) = WorldStructuresBuilder.BuildMapStructure(null, "aurelionite_geode", palette, new Vector3(gx, 0f, gz), gyaw, rng, Physics.BoxObstacles);
                        entitiesList.Add(gEnt);
                        Physics.Obstacles.AddRange(gObs);
                        Physics.RegisterSpawnBlocker(new Vector3(gx, 0f, gz), 3.0f, 10f);
                    }

                    foreach (float sideX in new[] { -23f, 23f })
                    {
                        SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(sideX, 3.8f, 5f), 5.6f, 1.2f, stormSlate, aurelGold);
                        SpawnParkourDisc(entitiesList, Physics.SkyPlatforms, new Vector3(sideX * 0.82f, 6.4f, 28f), 4.8f, 1.1f, stormDark, lunarBolt);
                        entitiesList.Add(new LunarBouncePad(new Vector3(sideX, 0f, -5.5f), 18.5f, 5.5f));
                    }
                    break;
                }
            }
        }

        private static void PopulateExpansionSafeTravelsCrew(List<Entity> entitiesList, HashSet<string> spawnedSet)
        {
            Color cyanNeon = Rgb(75, 215, 255);
            Color arbiterGold = Rgb(255, 208, 68);
            Color spamPink = Rgb(255, 95, 205);
            Color spamYellow = Rgb(255, 235, 65);
            Color prtsRed = Rgb(255, 58, 72);

            // --- 1. АТМОСФЕРНЫЕ 3D-УГОЛКИ НОВЫХ ЧЛЕНОВ ЭКИПАЖА НА БОРТУ UES «SAFE TRAVELS» ---

            // Центральный Зал (Юг у Бара «Бездна»): Школьный шкафчик Сьюзи и коробка мела
            var susieLocker = new Entity(position: new(-19.5f, 0f, 3.6f));
            new Entity(parent: susieLocker, model: "cube", scale: new(1.8f, 2.35f, 1.1f), position: new(0, 1.18f, 0), color: Rgb(88, 44, 118));
            new Entity(parent: susieLocker, model: "diamond", scale: new(0.55f, 1.55f, 0.24f), position: new(0.85f, 1.1f, 0.35f), rotation: new(0, 0, -18f), color: Rgb(235, 58, 142), unlit: true);
            new Entity(parent: susieLocker, model: "cube", scale: new(0.52f, 0.26f, 0.38f), position: new(-0.25f, 2.48f, 0.1f), color: Color.White, unlit: true);
            entitiesList.Add(susieLocker);
            Physics.AddSolidBox(-19.5f, 3.6f, 1.8f, 1.1f, 2.4f, 0f, 0f, walkableTop: true);
            entitiesList.Add(new SafeTravelsRoomProp(new(-19.5f, 0f, 3.6f), "Шкафчик Сьюзи и Стратегический Запас Мела",
                "К шкафчику прислонена секира Тёмного Мира, а сверху стоит коробка отборного белого школьного мела «Не трогать — перекус Сьюзи!»", Rgb(235, 72, 168), "slam_land", labelHeight: 3.1f));

            // Центральный Зал (СЗ угол): Мусорный киоск Спамтона [[BIG SHOT]] и синие [Pipis]
            var spamDumpster = new Entity(position: new(-24.2f, 0f, 24.8f), rotation: new(0, 35f, 0));
            new Entity(parent: spamDumpster, model: "cube", scale: new(2.5f, 1.45f, 1.65f), position: new(0, 0.725f, 0), color: Rgb(36, 104, 66));
            new Entity(parent: spamDumpster, model: "cube", scale: new(2.4f, 0.75f, 0.16f), position: new(0, 2.15f, -0.55f), color: Rgb(22, 18, 36));
            new Entity(parent: spamDumpster, model: "sphere", scale: new(0.52f, 0.52f, 0.22f), position: new(-0.36f, 2.15f, -0.44f), color: spamPink, unlit: true);
            new Entity(parent: spamDumpster, model: "sphere", scale: new(0.52f, 0.52f, 0.22f), position: new(0.36f, 2.15f, -0.44f), color: spamYellow, unlit: true);
            new Entity(parent: spamDumpster, model: "sphere", scale: new(0.75f, 0.58f, 0.65f), position: new(0.55f, 1.62f, 0.2f), color: Rgb(48, 195, 255), unlit: true);
            entitiesList.Add(spamDumpster);
            Physics.AddSolidBox(-24.2f, 24.8f, 2.5f, 1.65f, 1.55f, 35f, 0f, walkableTop: true);
            entitiesList.Add(new SafeTravelsRoomProp(new(-24.2f, 0f, 24.8f), "Торговый Контейнер [[BIG SHOT 1997]] и [Pipis]",
                "Из контейнера доносится звон старинного телефона, рекламный джингл и мерцание свежих синих сфер [Pipis] по акции [[0% СКИДКА]]!", spamPink, "coin_flip", labelHeight: 2.9f));

            // Центральный Зал (Рок-Сцена): Золотой оперный микрофон Пенаконии для Зарянки
            var robinMic = new Entity(position: new(20.4f, 0.55f, 13.4f));
            new Entity(parent: robinMic, model: "diamond", scale: new(0.95f, 0.14f, 0.95f), position: new(0, 0.07f, 0), color: arbiterGold, unlit: true);
            new Entity(parent: robinMic, model: "cube", scale: new(0.10f, 1.65f, 0.10f), position: new(0, 0.85f, 0), color: arbiterGold);
            new Entity(parent: robinMic, model: "sphere", scaleUniform: 0.34f, position: new(0, 1.72f, 0), color: Rgb(215, 175, 255), unlit: true);
            entitiesList.Add(robinMic);
            entitiesList.Add(new SafeTravelsRoomProp(new(20.4f, 0.55f, 13.4f), "Золотой Микрофон Гармонии Пенаконии ♫",
                "Концертная стойка Зарянки идеально настроена в унисон с ламповым усилителем Александра Пушного.", Rgb(225, 185, 255), "magic_cast", labelHeight: 2.8f));

            // Центральный Зал (ЮВ угол у кубка Трейсен): Диджейский пульт Дайтаку Хелиос «WEI~★!»
            var djBooth = new Entity(position: new(23.5f, 0f, -16.5f), rotation: new(0, -35f, 0));
            new Entity(parent: djBooth, model: "cube", scale: new(2.5f, 1.15f, 1.35f), position: new(0, 0.58f, 0), color: Rgb(34, 42, 68));
            foreach (float dx in new[] { -0.62f, 0.62f })
                new Entity(parent: djBooth, model: "sphere", scale: new(0.78f, 0.12f, 0.78f), position: new(dx, 1.20f, 0), color: dx < 0 ? cyanNeon : spamPink, unlit: true);
            entitiesList.Add(djBooth);
            Physics.AddSolidBox(23.5f, -16.5f, 2.5f, 1.35f, 1.22f, -35f, 0f, walkableTop: true);
            entitiesList.Add(new SafeTravelsRoomProp(new(23.5f, 0f, -16.5f), "Диджейский Пульт Трейсен «WEI~★!»",
                "Виниловые вертушки Дайтаку Хелиос мигают неоном: в плейлисте брейкбит Арима Кинэн и треки для вечеринки экипажа!", cyanNeon, "ui_ready"));

            // Северный Мостик (Восточное крыло): Чайный столик Арбитра Бины и Нейронный монолит PRTS Пристэсс
            var binahTea = new Entity(position: new(23.8f, 0f, 39.5f));
            new Entity(parent: binahTea, model: "cube", scale: new(2.1f, 0.98f, 2.1f), position: new(0, 0.49f, 0), color: Rgb(24, 22, 28));
            new Entity(parent: binahTea, model: "diamond", scale: new(2.25f, 0.12f, 2.25f), position: new(0, 1.02f, 0), color: arbiterGold, unlit: true);
            new Entity(parent: binahTea, model: "sphere", scaleUniform: 0.42f, position: new(-0.25f, 1.24f, 0.1f), color: Rgb(238, 232, 220));
            entitiesList.Add(binahTea);
            Physics.AddSolidBox(23.8f, 39.5f, 2.1f, 2.1f, 1.08f, 0f, 0f, walkableTop: true);
            entitiesList.Add(new SafeTravelsRoomProp(new(23.8f, 0f, 39.5f), "Чёрно-Золотой Чайный Столик Арбитра",
                "На полированном обсидиане дымится чашка крепкого чёрного чая Бины рядом с запечатанным золотым ключом Главы.", arbiterGold, "ui_tick"));

            var prtsNode = new Entity(position: new(24.2f, 0f, 51.5f), rotation: new(0, -90f, 0));
            new Entity(parent: prtsNode, model: "cube", scale: new(2.4f, 2.35f, 1.25f), position: new(0, 1.18f, 0), color: Rgb(24, 28, 38));
            new Entity(parent: prtsNode, model: "diamond", scale: new(1.15f, 1.55f, 0.22f), position: new(0, 1.65f, 0.68f), color: prtsRed, unlit: true);
            entitiesList.Add(prtsNode);
            Physics.AddSolidBox(24.2f, 51.5f, 2.4f, 1.25f, 2.35f, -90f, 0f, walkableTop: false);
            entitiesList.Add(new SafeTravelsRoomProp(new(24.2f, 0f, 51.5f), "Терминал Синхронизации PRTS ◇",
                "На экране светится багровый ромб ◇: «Все системы корабля синхронизированы. Я всегда буду рядом с тобой среди звёзд.»", prtsRed, "ui_ready", labelHeight: 3.0f));

            // Каюта C3 (Западная стена): Копьё Дон Кихот и знамя Ла-Манчаленда
            var donRack = new Entity(position: new(71.0f, 0f, 24.5f));
            new Entity(parent: donRack, model: "cube", scale: new(1.5f, 1.1f, 2.3f), position: new(0, 0.55f, 0), color: Rgb(78, 24, 34));
            new Entity(parent: donRack, model: "diamond", scale: new(0.45f, 2.85f, 0.45f), position: new(0.15f, 1.85f, -0.35f), rotation: new(12f, 0, -10f), color: arbiterGold, unlit: true);
            new Entity(parent: donRack, model: "cube", scale: new(0.22f, 2.4f, 1.15f), position: new(-0.55f, 1.95f, 0.45f), color: Rgb(215, 38, 58), unlit: true);
            entitiesList.Add(donRack);
            Physics.AddSolidBox(71.0f, 24.5f, 1.5f, 2.3f, 2.6f, 0f, 0f, walkableTop: false);
            entitiesList.Add(new SafeTravelsRoomProp(new(71.0f, 0f, 24.5f), "Стойка Рыцарского Копья и Значков Корректировщиков",
                "Коллекция значков Красного Взгляда, кроссовки «Росинант» и праздничный флажок Ла-Манчаленда сияют образцовым блеском!", arbiterGold, "parry", labelHeight: 3.1f));

            // Каюта C4 (СВ угол): Инженерный док дронов Кложур
            var closureBench = new Entity(position: new(45.2f, 0f, -12.0f));
            new Entity(parent: closureBench, model: "cube", scale: new(1.55f, 1.05f, 2.3f), position: new(0, 0.52f, 0), color: Rgb(44, 52, 66));
            new Entity(parent: closureBench, model: "cube", scale: new(0.85f, 0.72f, 0.85f), position: new(0, 1.42f, -0.35f), color: Rgb(235, 240, 248));
            new Entity(parent: closureBench, model: "sphere", scaleUniform: 0.36f, position: new(-0.35f, 1.48f, 0.55f), color: cyanNeon, unlit: true);
            entitiesList.Add(closureBench);
            Physics.AddSolidBox(45.2f, -12.0f, 1.55f, 2.3f, 1.75f, 0f, 0f, walkableTop: true);
            entitiesList.Add(new SafeTravelsRoomProp(new(45.2f, 0f, -12.0f), "Док-Станция «Lancet-2» и Инженерная Касса Кложур",
                "Дроны Кложур подзаряжаются от сети UES; рядом лежит VIP-прейскурант магазина снабжения Родос Айленд со скидкой 1%!", cyanNeon, "build_place"));

            // Каюта C6 (Западная стена): Подземные розы Дворца Духов Земли сестёр Комейдзи
            var roseStand = new Entity(position: new(71.0f, 0f, -24.5f));
            new Entity(parent: roseStand, model: "cube", scale: new(1.65f, 0.85f, 2.3f), position: new(0, 0.425f, 0), color: Rgb(56, 36, 72));
            new Entity(parent: roseStand, model: "sphere", scale: new(0.85f, 0.85f, 0.45f), position: new(0, 1.85f, -0.35f), color: Rgb(235, 72, 138), unlit: true);
            new Entity(parent: roseStand, model: "sphere", scale: new(0.75f, 0.75f, 0.45f), position: new(0.15f, 1.45f, 0.45f), color: Rgb(85, 235, 165), unlit: true);
            entitiesList.Add(roseStand);
            Physics.AddSolidBox(71.0f, -24.5f, 1.65f, 2.3f, 2.2f, 0f, 0f, walkableTop: false);
            entitiesList.Add(new SafeTravelsRoomProp(new(71.0f, 0f, -24.5f), "Витраж Тирэйдэна и Куст Роз Подсознания",
                "Алые розы Сатори и изумрудные розы Коиши переплетаются вокруг спящего Третьего Глаза. Кошка Орин оставила рядом тёплый след.", Rgb(245, 105, 175), "charm_heart"));

            // Боевой Полигон (СВ ниша огневого рубежа): Грозовая жеода Аурелионита для Ложного Сына
            var meridianShrine = new Entity(position: new(-35.5f, 0f, 18.5f));
            new Entity(parent: meridianShrine, model: "sphere", scale: new(2.3f, 0.95f, 2.2f), position: new(0, 0.45f, 0), color: Rgb(44, 54, 76));
            new Entity(parent: meridianShrine, model: "diamond", scale: new(0.95f, 2.55f, 0.95f), position: new(0, 1.45f, 0), rotation: new(8f, 25f, -6f), color: arbiterGold, unlit: true);
            new Entity(parent: meridianShrine, model: "diamond", scale: new(1.85f, 0.16f, 1.85f), position: new(0, 2.05f, 0), rotation: new(16f, 0, -14f), color: cyanNeon, unlit: true);
            entitiesList.Add(meridianShrine);
            Physics.Obstacles.Add(new ObstacleCircle(-35.5f, 18.5f, 1.15f, 2.8f, 0f));
            entitiesList.Add(new SafeTravelsRoomProp(new(-35.5f, 0f, 18.5f), "Грозовая Жеода Аурелионита с Меридиана",
                "Золотой осколок Позолоченного Побережья тихо гудит в унисон с лунными молниями Петрикора V, очищенными от порчи.", arbiterGold, "zap", labelHeight: 3.0f));

            // --- 2. РАЗМЕЩЕНИЕ 14 ОСНОВНЫХ ЭКСПАНСИОННЫХ ПЕРСОН ЭКИПАЖА ---
            (string key, Vector3 pos, float yaw, string zone, string emote)[] expansionCrew =
            {
                ("susie", new(-15.8f, 0f, 3.8f), 80f, "Бар «Бездна» — Уголок Тёмного Мира", ""),
                ("spamton", new(-21.2f, 0f, 23.2f), 140f, "Торговый Киоск [[BIG SHOT]]", ""),
                ("spamton_neo", new(-24.5f, 0f, 20.6f), 115f, "Сцена Марионетки [[НЕО]]", ""),
                ("robin", new(20.2f, 0f, 16.2f), 215f, "Оперная Сцена Пенаконии", ""),
                ("daitaku_helios", new(21.2f, 0f, -14.2f), 315f, "Танцпол Трейсен «WEI~★!»", "california"),
                ("binah", new(20.6f, 0f, 39.5f), 275f, "Чайный Балкон Арбитра", ""),
                ("priestess", new(20.8f, 0f, 51.5f), 270f, "Терминал PRTS ◇ на Мостике", ""),
                ("don_quixote", new(71.8f, 0f, 19.5f), 115f, "Каюта C3: Уголок Корректировщика", ""),
                ("don_lantern", new(71.5f, 0f, 14.5f), 95f, "Каюта C3: Дозор Э.Г.О. «Фонарь»", ""),
                ("don_manager", new(74.2f, 0f, 25.0f), 155f, "Каюта C3: Зал Ла-Манчаленда", ""),
                ("closure", new(42.4f, 0f, -12.5f), 285f, "Каюта C4: Инженерный Отсек Родоса", ""),
                ("komeiji", new(71.6f, 0f, -19.5f), 75f, "Каюта C6: Дворец Духов Земли", ""),
                ("komeiji_koishi", new(71.6f, 0f, -13.5f), 60f, "Каюта C6: Сад Роз Подсознания", ""),
                ("false_son", new(-39.2f, 0f, 16.5f), 145f, "Огневой Рубеж: Пост Меридиана", "")
            };

            foreach (var (key, pos, yaw, zone, emote) in expansionCrew)
            {
                if (spawnedSet.Add(key))
                    entitiesList.Add(new SafeTravelsNpc(PlaceCrewMember(pos, 0.6f), key, yaw, zone, emote));
            }

            // --- 3. СТРОГАЯ ПРОВЕРКА ДОСТУПА ДЛЯ ПТИЦ ШВАРЦВАЛЬДА И ПТИЦЫ АПОКАЛИПСИСА ---
            if (ProgressData.Instance.IsBirdTrioAuthorized())
            {
                var forestLamp = new Entity(position: new(-24.8f, 0f, 63.8f));
                new Entity(parent: forestLamp, model: "cube", scale: new(0.85f, 0.35f, 0.85f), position: new(0, 0.18f, 0), color: Rgb(36, 30, 28));
                new Entity(parent: forestLamp, model: "cube", scale: new(0.24f, 2.85f, 0.24f), position: new(0, 1.60f, 0), color: Rgb(52, 42, 34));
                new Entity(parent: forestLamp, model: "sphere", scaleUniform: 0.72f, position: new(0, 3.15f, 0), color: Rgb(255, 215, 85), unlit: true);
                new Entity(parent: forestLamp, model: "diamond", scale: new(1.45f, 0.18f, 0.35f), position: new(0, 2.35f, 0), color: arbiterGold, unlit: true);
                entitiesList.Add(forestLamp);
                Physics.Obstacles.Add(new ObstacleCircle(-24.8f, 63.8f, 0.75f, 3.5f, 0f));
                entitiesList.Add(new SafeTravelsRoomProp(new(-24.8f, 0f, 63.8f), "Негасимый Фонарь и Весы Тёмного Леса",
                    "Тёплый фонарь Большой Птицы, повязка Длинной Птицы и красный клюв Маленькой Птицы мирно хранят покой смотровой палубы.", arbiterGold, "dante_clock", labelHeight: 3.7f));

                (string key, Vector3 pos, float yaw, string zone)[] birdCrew =
                {
                    ("bird_trio", new(-21.6f, 0f, 64.5f), 155f, "Смотровая Палуба Тёмного Леса"),
                    ("bird_long", new(-25.2f, 0f, 59.8f), 135f, "Смотровая Палуба: Весы Суда"),
                    ("bird_small", new(-20.4f, 0f, 60.8f), 150f, "Смотровая Палуба: Дозор Кары")
                };
                foreach (var (key, pos, yaw, zone) in birdCrew)
                {
                    if (spawnedSet.Add(key))
                        entitiesList.Add(new SafeTravelsNpc(PlaceCrewMember(pos, 0.6f), key, yaw, zone, ""));
                }
            }

            if (ProgressData.Instance.IsCharacterUnlocked("apocalypse_bird"))
            {
                var twilightGate = new Entity(position: new(24.8f, 0f, 64.2f), rotation: new(0, -25f, 0));
                foreach (float gx in new[] { -1.0f, 1.0f })
                    new Entity(parent: twilightGate, model: "cube", scale: new(0.36f, 3.2f, 0.36f), position: new(gx, 1.6f, 0), color: Rgb(22, 18, 24));
                new Entity(parent: twilightGate, model: "cube", scale: new(2.5f, 0.42f, 0.48f), position: new(0, 3.25f, 0), color: arbiterGold);
                foreach (float ey in new[] { 1.4f, 2.1f, 2.7f })
                    new Entity(parent: twilightGate, model: "sphere", scale: new(0.46f, 0.24f, 0.22f), position: new(0, ey, 0), color: Rgb(255, 195, 55), unlit: true);
                entitiesList.Add(twilightGate);
                Physics.AddSolidBox(24.8f, 64.2f, 2.5f, 0.8f, 3.5f, -25f, 0f, walkableTop: false);
                entitiesList.Add(new SafeTravelsRoomProp(new(24.8f, 0f, 64.2f), "Врата Сумерек Тёмного Леса",
                    "Чёрно-золотые врата хранят память о дне, когда Три Птицы объединились ради спасения леса. Звёзды за стеклом отражаются в золотых глазах.", Rgb(255, 175, 55), "boss_roar", labelHeight: 3.9f));

                if (spawnedSet.Add("apocalypse_bird"))
                    entitiesList.Add(new SafeTravelsNpc(PlaceCrewMember(new(21.5f, 0f, 63.2f), 0.7f), "apocalypse_bird", 210f, "Врата Сумерек на Мостике", ""));
            }
        }
    }
}
