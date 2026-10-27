using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Models
{
    public class SkillDef
    {
        public string Name = "";
        public float Cd;
        public string Desc = "";
    }

    public class SurvivorConfig
    {
        public string Key = "";
        public string Name = "";
        public string Subtitle = "";
        public string Icon = "";
        public int Hp;
        public float Speed;
        public float Damage;
        public float FireRate;
        public float ProjSpeed = 62f;
        public float ProjScale = 0.22f;
        public float Aoe = 0f;
        public Color BodyColor;
        public Color ArmorColor;
        public Color VisorColor;
        public Dictionary<string, SkillDef> Skills = new();
    }

    public class SurvivorParts
    {
        public Entity Root = null!;
        public Entity Torso = null!;
        public Entity LeftLeg = null!;
        public Entity RightLeg = null!;
        public Entity LeftArm = null!;
        public Entity RightArm = null!;
        public Entity? Halo;
        public Entity? Bow;
        public Entity? Shield;
        public Entity? ShieldAura;
        public readonly List<Entity> NormalFormNodes = new();
        public readonly List<Entity> UltFormNodes = new();
        public bool UltFormActive = false;

        public void SetUltForm(bool active)
        {
            if (UltFormActive == active || UltFormNodes.Count == 0) return;
            UltFormActive = active;
            foreach (var n in NormalFormNodes)
                if (n != null && !n.IsDestroyed) n.Enabled = !active;
            foreach (var n in UltFormNodes)
                if (n != null && !n.IsDestroyed) n.Enabled = active;
        }
    }

    public static class SurvivorDatabase
    {
        public static readonly string[] OrderedKeys =
        {
            "commando", "artificer", "huntress", "buzz", "v1", "sol", "gwen",
            "enforcer", "scout", "m4a1", "medic", "aigis", "reimu", "mindustry",
            "slark", "tidehunter", "freeman", "dante", "cheburashka"
        };

        public static bool IsMobaCharacter(string charKey)
            => charKey is "gwen" or "slark" or "tidehunter";

        public static readonly Dictionary<string, SurvivorConfig> Classes = new()
        {
            ["commando"] = new()
            {
                Key = "commando", Name = "КОММАНДО", Icon = "CMD", Subtitle = "Универсальный боец UES",
                Hp = 115, Speed = 9.6f, Damage = 12.0f, FireRate = 6.5f, ProjSpeed = 62f, ProjScale = 0.22f,
                BodyColor = Rgb(235, 130, 35), ArmorColor = Rgb(70, 78, 95), VisorColor = Rgb(60, 245, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Двойной выстрел", Cd = 0.15f, Desc = "[ЛКМ] Скорострельная стрельба из двух пистолетов." },
                    ["m2"] = new() { Name = "Фазовый снаряд", Cd = 3.2f, Desc = "[ПКМ] Мощный плазменный заряд (300% урона), прошивающий врагов насквозь." },
                    ["shift"] = new() { Name = "Тактический кувырок", Cd = 4.0f, Desc = "[Shift] Стремительный тактический рывок вперёд." },
                    ["r"] = new() { Name = "Подавляющий огонь", Cd = 8.0f, Desc = "[R] Шквальная очередь из 8 выстрелов повышенной точности." }
                }
            },
            ["artificer"] = new()
            {
                Key = "artificer", Name = "ИЗОБРЕТАТЕЛЬ", Icon = "ART", Subtitle = "Высокотехнологичный Архимаг",
                Hp = 100, Speed = 8.5f, Damage = 34.0f, FireRate = 1.9f, ProjSpeed = 36f, ProjScale = 0.52f, Aoe = 4.2f,
                BodyColor = Rgb(155, 55, 230), ArmorColor = Rgb(225, 195, 105), VisorColor = Rgb(255, 235, 65),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Плазменный разряд", Cd = 0.52f, Desc = "[ЛКМ] Взрывные плазменные сферы, наносящие урон по площади." },
                    ["m2"] = new() { Name = "Заряженная нано-бомба", Cd = 5.0f, Desc = "[ПКМ] Сверхтяжёлая грави-сфера (280% урона в огромном радиусе)." },
                    ["shift"] = new() { Name = "Морозный взлёт", Cd = 4.5f, Desc = "[Shift] Реактивный взлёт вверх с ледяным взрывом под ногами." },
                    ["r"] = new() { Name = "Орбитальный ионный удар", Cd = 10.0f, Desc = "[R] Обрушивает 5 орбитальных ионных лучей на ближайших врагов." }
                }
            },
            ["huntress"] = new()
            {
                Key = "huntress", Name = "ОХОТНИЦА", Icon = "HNT", Subtitle = "Стремительный Снайпер",
                Hp = 95, Speed = 10.8f, Damage = 18.0f, FireRate = 3.4f, ProjSpeed = 54f, ProjScale = 0.28f,
                BodyColor = Rgb(215, 45, 70), ArmorColor = Rgb(45, 50, 64), VisorColor = Rgb(255, 240, 90),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Стрельба в движении", Cd = 0.29f, Desc = "[ЛКМ] Выпускает энергетические стрелы с автонаведением." },
                    ["m2"] = new() { Name = "Лазерная глефа", Cd = 5.5f, Desc = "[ПКМ] Бросает лазерную глефу, рикошетящую до 5 раз между врагами!" },
                    ["shift"] = new() { Name = "Фазовый скачок", Cd = 3.5f, Desc = "[Shift] Мгновенная телепортация в направлении взгляда." },
                    ["r"] = new() { Name = "Дождь стрел", Cd = 9.5f, Desc = "[R] Прыжок в небо и обстрел зоны градом энергетических стрел." }
                }
            },
            ["buzz"] = new()
            {
                Key = "buzz", Name = "БАЗЗ ЛАЙТЕР", Icon = "BUZ", Subtitle = "Космический Рейнджер Звёздного Командования",
                Hp = 135, Speed = 9.8f, Damage = 16.0f, FireRate = 4.8f, ProjSpeed = 66f, ProjScale = 0.25f, Aoe = 1.2f,
                BodyColor = Rgb(125, 235, 65), ArmorColor = Rgb(240, 245, 250), VisorColor = Rgb(155, 65, 245),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Наручный лазер", Cd = 0.21f, Desc = "[ЛКМ] Скорострельный наручный лазер Звёздного Командования." },
                    ["m2"] = new() { Name = "Гипер-луч", Cd = 4.0f, Desc = "[ПКМ] Пробивной луч полной мощности с взрывным импульсом." },
                    ["shift"] = new() { Name = "Бесконечность — не предел!", Cd = 4.5f, Desc = "[Shift] Раскрывает крылья и взмывает в воздух на реактивном ранце." },
                    ["r"] = new() { Name = "Залп Звёздного Командования", Cd = 9.0f, Desc = "[R] Залп из 6 самонаводящихся фотонных ракет по врагам." }
                }
            },
            ["v1"] = new()
            {
                Key = "v1", Name = "V1 (ULTRAKILL)", Icon = "V1", Subtitle = "ВИД ОТ 1-ГО ЛИЦА | 5 ОРУЖИЙ [1-5] | 3 РУКИ [F/G/R] | СТИЛЬ",
                Hp = 100, Speed = 12.6f, Damage = 19.0f, FireRate = 5.2f, ProjSpeed = 85f, ProjScale = 0.22f,
                BodyColor = Rgb(45, 115, 235), ArmorColor = Rgb(32, 38, 52), VisorColor = Rgb(255, 225, 40),
                Skills = new()
                {
                    ["m1"] = new() { Name = "[1-5 / Колёсико] 5 Оружий", Cd = 0.15f, Desc = "1: Револьвер (Монетки) | 2: Дробовик (Ядро) | 3: Гвоздемёт | 4: Рельсотрон | 5: Ракетница." },
                    ["m2"] = new() { Name = "[F] Фидбекер и [G] Наклбластер", Cd = 0.45f, Desc = "[F] Синяя рука: Парирование, Ускорение снаряда и удар по монетке. [G] Красная рука." },
                    ["shift"] = new() { Name = "[Shift] 3x Рывок выносливости", Cd = 1.0f, Desc = "3 заряда рывка. Быстрое перемещение и уклонение от атак." },
                    ["r"] = new() { Name = "[R] Крюк-хлыст (Зелёная рука)", Cd = 2.2f, Desc = "Притягивает лёгких врагов к вам, а вас — к тяжёлым врагам и боссам. Кровь лечит HP!" }
                }
            },
            ["sol"] = new()
            {
                Key = "sol", Name = "СОЛ БЭДГАЙ", Icon = "SOL", Subtitle = "Пламя Порчи [GUNFLAME И BANDIT REVOLVER]",
                Hp = 160, Speed = 10.6f, Damage = 30.0f, FireRate = 2.6f, ProjSpeed = 42f, ProjScale = 0.48f, Aoe = 3.5f,
                BodyColor = Rgb(210, 35, 45), ArmorColor = Rgb(40, 35, 38), VisorColor = Rgb(255, 165, 30),
                Skills = new()
                {
                    ["m1"] = new() { Name = "GunFlame (Огненная волна)", Cd = 0.38f, Desc = "[ЛКМ] Вонзает меч в землю и пускает вперёд волну столпов пламени!" },
                    ["m2"] = new() { Name = "Bandit Revolver (Двойной пинок)", Cd = 4.0f, Desc = "[ПКМ] Рывок с пинком («BANDIT!»). Повторное нажатие ПКМ проводит второй удар («REVOLVER!»)!" },
                    ["shift"] = new() { Name = "Bandit Bringer (Пикирующий кулак)", Cd = 4.2f, Desc = "[Shift] Взмывает вверх по диагонали и обрушивается вниз с пылающим ударом кулака!" },
                    ["r"] = new() { Name = "ДРАКОНИЙ ИНСТАЛЛ", Cd = 11.0f, Desc = "[R] Пробуждение силы Гира: огненный взрыв + ускорение боя и регенерация на 6 сек." }
                }
            },
            ["gwen"] = new()
            {
                Key = "gwen", Name = "ГВЕН", Icon = "GWN", Subtitle = "Благая Швея [MOBA-РЕЖИМ | ВИД СВЕРХУ]",
                Hp = 135, Speed = 10.8f, Damage = 18.5f, FireRate = 4.8f, ProjSpeed = 52f, ProjScale = 0.30f, Aoe = 1.8f,
                BodyColor = Rgb(65, 195, 255), ArmorColor = Rgb(32, 28, 46), VisorColor = Rgb(195, 240, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Чик-чик! (4 удара -> 6 разрезов)", Cd = 0.21f, Desc = "[ЛКМ / Q] Каждый 4-й удар ножницами мгновенно проводит серию из 6 быстрых разрезов!" },
                    ["m2"] = new() { Name = "Точный разрез!", Cd = 3.5f, Desc = "[W / СКМ] Мощный смыкающий разрез гигантскими ножницами (320% урона + лечение)." },
                    ["shift"] = new() { Name = "Прыжок и Священный Туман", Cd = 5.2f, Desc = "[Shift] Рывок (+скорость атаки) и Туман: стрелки снаружи НЕ ВИДЯТ Гвен внутри тумана!" },
                    ["r"] = new() { Name = "Вышивание (1 -> 2 -> 3 иглы)", Cd = 9.0f, Desc = "[R] Три волны игл с интервалом 0.5с (1, затем 2, затем 3 иглы) с ростом урона и замедлением!" }
                }
            },
            ["enforcer"] = new()
            {
                Key = "enforcer", Name = "СИЛОВИК", Icon = "ENF", Subtitle = "Тяжёлый Бастион [ОТРАЖЕНИЕ И ПАРИРОВАНИЕ x3]",
                Hp = 175, Speed = 9.0f, Damage = 15.0f, FireRate = 1.65f, ProjSpeed = 64f, ProjScale = 0.20f,
                BodyColor = Rgb(55, 115, 195), ArmorColor = Rgb(38, 48, 68), VisorColor = Rgb(110, 225, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Штурмовой дробовик", Cd = 0.60f, Desc = "[ЛКМ] Мощный залп картечью из 6 пробивных дробин." },
                    ["m2"] = new() { Name = "Удар щитом", Cd = 4.0f, Desc = "[ПКМ] Оглушающий удар щитом перед собой (стан 2с и +20 щита)." },
                    ["shift"] = new() { Name = "Щит и Парирование (x3)", Cd = 1.0f, Desc = "[Shift] Щит отражает лобовые атаки. Выход из щита ПАРИРУЕТ ЛЮБУЮ атаку с возвратом урона x3!" },
                    ["r"] = new() { Name = "Слезоточивый газ", Cd = 9.5f, Desc = "[R] Бросает гранату со слезоточивым газом (замедляет врагов и бьёт по области)." }
                }
            },
            ["scout"] = new()
            {
                Key = "scout", Name = "РАЗВЕДЧИК (DRG)", Icon = "SCT", Subtitle = "СКАЛА И КАМЕНЬ! Разведчик Хоксеса IV",
                Hp = 120, Speed = 11.4f, Damage = 14.5f, FireRate = 6.8f, ProjSpeed = 72f, ProjScale = 0.22f,
                BodyColor = Rgb(35, 115, 215), ArmorColor = Rgb(55, 65, 82), VisorColor = Rgb(255, 210, 55),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Винтовка Deepcore GK2", Cd = 0.14f, Desc = "[ЛКМ] Скорострельная штурмовая винтовка с высокой точностью." },
                    ["m2"] = new() { Name = "Двустволка (Особый порох)", Cd = 3.6f, Desc = "[ПКМ] Дуплет из обреза + реактивный прыжок от отдачи в воздухе!" },
                    ["shift"] = new() { Name = "Крюк-кошка", Cd = 3.2f, Desc = "[Shift] Выстреливает крюком-кошкой на 45м и притягивает Разведчика на тросе." },
                    ["r"] = new() { Name = "Поле IFG и ракета", Cd = 9.0f, Desc = "[R] Поле IFG: замедляет врагов на 65% и бьёт током по зоне." }
                }
            },
            ["m4a1"] = new()
            {
                Key = "m4a1", Name = "M4A1 (GFL)", Icon = "M4", Subtitle = "Лидер отряда AR [АВТОНОМНЫЕ ИИ-КЛОНЫ]",
                Hp = 110, Speed = 10.4f, Damage = 15.5f, FireRate = 5.8f, ProjSpeed = 68f, ProjScale = 0.23f,
                BodyColor = Rgb(75, 95, 78), ArmorColor = Rgb(42, 46, 48), VisorColor = Rgb(95, 255, 135),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Штурмовой карабин M4A1", Cd = 0.17f, Desc = "[ЛКМ] Огонь из карабина. Клоны самостоятельно зачищают карту и собирают лут!" },
                    ["m2"] = new() { Name = "Пушка «Юпитер» (MOD3)", Cd = 4.5f, Desc = "[ПКМ] Пробивной энерго-выстрел рельсовой пушки с взрывом (340% ур.)." },
                    ["shift"] = new() { Name = "Тактическое уклонение", Cd = 3.6f, Desc = "[Shift] Тактический рывок с получением +25 щита и залпом клонов." },
                    ["r"] = new() { Name = "Автономные клоны (До 4 шт.)", Cd = 7.5f, Desc = "[R] Спавнит ИИ-клона: они сами бегают по карте, открывают сундуки, берут лут и юзают скиллы!" }
                }
            },
            ["medic"] = new()
            {
                Key = "medic", Name = "МЕДИК (TF2)", Icon = "MED", Subtitle = "ВИД ОТ 1-ГО ЛИЦА | НАСМЕШКА [G] | УБЕР-ЗАРЯД",
                Hp = 130, Speed = 10.2f, Damage = 14.0f, FireRate = 6.2f, ProjSpeed = 64f, ProjScale = 0.22f,
                BodyColor = Rgb(235, 238, 245), ArmorColor = Rgb(205, 38, 45), VisorColor = Rgb(255, 75, 75),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Шприцемёт (От 1-го лица)", Cd = 0.16f, Desc = "[ЛКМ] Наносит урон врагам и лечит союзников. [G] — Насмешка «Oktoberfest!» с видом от 3-го лица!" },
                    ["m2"] = new() { Name = "Лечебный луч Крицкрига", Cd = 3.4f, Desc = "[ПКМ] Лечит и усиливает союзника (+барьер) или выкачивает HP из врага." },
                    ["shift"] = new() { Name = "Целебный рывок Ампутатора", Cd = 4.2f, Desc = "[Shift] Рывок с ударом убер-пилой и целебным импульсом (+25% HP)." },
                    ["r"] = new() { Name = "Убер-заряд / Контроль разума", Cd = 10.5f, Desc = "[R] На врага (<50% HP) — ПЕРЕМАНИВАЕТ его! Иначе — УБЕР-ЗАРЯД на 6.5с!" }
                }
            },
            ["aigis"] = new()
            {
                Key = "aigis", Name = "АЙГИС (PERSONA 3)", Icon = "AIG", Subtitle = "Оружие против Теней [ОРГИЯ И ПАЛЛАДИОН]",
                Hp = 140, Speed = 10.6f, Damage = 13.5f, FireRate = 8.2f, ProjSpeed = 76f, ProjScale = 0.20f,
                BodyColor = Rgb(240, 242, 250), ArmorColor = Rgb(235, 195, 55), VisorColor = Rgb(65, 195, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Пальцевые пулемёты «Вулкан»", Cd = 0.12f, Desc = "[ЛКМ] Ураганный пулемётный огонь из пальцев-стволов правой руки." },
                    ["m2"] = new() { Name = "Гранатомёт «Пандора»", Cd = 3.8f, Desc = "[ПКМ] Тяжёлый фугасный залп из гранатомёта (300% урона, AoE 5.2м)." },
                    ["shift"] = new() { Name = "Режим перегрузки «Оргия»", Cd = 5.0f, Desc = "[Shift] Взлёт и режим Оргии на 5.5 сек (+50% атаки, +45% скорости, +35% урона)." },
                    ["r"] = new() { Name = "Персона: Удар Палладиона", Cd = 9.5f, Desc = "[R] Обрушивает копья света на врагов вокруг и даёт +45 щита." }
                }
            },
            ["reimu"] = new()
            {
                Key = "reimu", Name = "ХАКУРЕЙ РЕЙМУ", Icon = "RMU", Subtitle = "Жрица Хакурей [ПОЛЁТ НА ЗАЖАТИЕ ПРОБЕЛА]",
                Hp = 105, Speed = 11.2f, Damage = 17.5f, FireRate = 4.8f, ProjSpeed = 58f, ProjScale = 0.26f, Aoe = 1.2f,
                BodyColor = Rgb(225, 42, 52), ArmorColor = Rgb(245, 245, 250), VisorColor = Rgb(255, 230, 80),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Амулеты Офуда и Полёт", Cd = 0.21f, Desc = "[ЛКМ] Парные амулеты. [Прыжок + Зажать Space] Свободный полёт (сбивается при получении урона)!" },
                    ["m2"] = new() { Name = "Сфера Инь-Ян", Cd = 4.0f, Desc = "[ПКМ] Сфера Инь-Ян: рикошетит до 6 раз между врагами с AoE!" },
                    ["shift"] = new() { Name = "Парение Природы Фантазии", Cd = 3.8f, Desc = "[Shift] Воздушный рывок (+щит и выпуск 4 защитных талисманов)." },
                    ["r"] = new() { Name = "Спелл-карта: Печать Фантазии", Cd = 9.5f, Desc = "[R] Град из 10 радужных сфер Данмаку с самонаведением!" }
                }
            },
            ["mindustry"] = new()
            {
                Key = "mindustry", Name = "КОРАБЛЬ ЯДРА «ГАММА»", Icon = "MND", Subtitle = "ВИД СВЕРХУ | 2 РЕЖИМА: СТРОИТЕЛЬСТВО [B/Shift] И БОЙ",
                Hp = 160, Speed = 11.8f, Damage = 14.0f, FireRate = 3.2f, ProjSpeed = 58f, ProjScale = 0.18f, Aoe = 0f,
                BodyColor = Rgb(245, 185, 45), ArmorColor = Rgb(62, 68, 82), VisorColor = Rgb(255, 235, 95),
                Skills = new()
                {
                    ["m1"] = new() { Name = "ЛКМ: Построить чертёж / Огонь", Cd = 0.30f, Desc = "[ЛКМ] В режиме строительства возводит выбранное строение (или стреляет, если выбор сброшен). В боевом режиме — стреляет." },
                    ["m2"] = new() { Name = "ПКМ: Сбросить выбор чертежа", Cd = 0.15f, Desc = "[ПКМ / 1-8] Меню справа снизу: Буры, Плавильщики, Переработчики, Пилон, Ядро и 3 Турели (Медь, Кремний, Торий)." },
                    ["shift"] = new() { Name = "[Shift / B] Смена Режима (Стройка / Бой)", Cd = 0.8f, Desc = "В Стройке корабль парит высоко и НЕУЯЗВИМ. В Боевом режиме — уязвим, стреляет и открывает сундуки/портал на [E]!" },
                    ["r"] = new() { Name = "Главное Ядро и Перегрузка", Cd = 8.0f, Desc = "[R] Ускоряет работу всех буров, заводов и турелей. Берегите Главное Ядро в PvP!" }
                }
            },
            ["slark"] = new()
            {
                Key = "slark", Name = "СЛАРК (DOTA 2)", Icon = "SLK", Subtitle = "Ночной Ужас Тёмного Рифа [MOBA-РЕЖИМ | ВИД СВЕРХУ]",
                Hp = 125, Speed = 11.6f, Damage = 17.5f, FireRate = 5.0f, ProjSpeed = 55f, ProjScale = 0.28f, Aoe = 1.6f,
                BodyColor = Rgb(45, 135, 165), ArmorColor = Rgb(28, 42, 62), VisorColor = Rgb(110, 255, 215),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Кража Сущности (Essence Shift)", Cd = 0.20f, Desc = "[ЛКМ / Q / ПКМ] Удары заточкой крадут сущность врагов (+урон за стак и вампиризм)." },
                    ["m2"] = new() { Name = "Тёмный Пакт (Dark Pact)", Cd = 4.0f, Desc = "[W / СКМ] Взрыв волн тёмной воды вокруг Сларка, снимающий дебаффы и наносящий 320% урона." },
                    ["shift"] = new() { Name = "Прыжок хищника (Pounce)", Cd = 4.2f, Desc = "[Shift] Стремительный прыжок к курсору, оглушающий и привязывающий врагов на пути." },
                    ["r"] = new() { Name = "Танец Теней (Shadow Dance)", Cd = 11.0f, Desc = "[R] Облако чёрного дыма на 5.5с: неуязвимость к урону, +45% скорости и мощное лечение!" }
                }
            },
            ["tidehunter"] = new()
            {
                Key = "tidehunter", Name = "ТАЙДХАНТЕР (DOTA 2)", Icon = "TID", Subtitle = "Левиафан Бездны [MOBA-РЕЖИМ | ВИД СВЕРХУ]",
                Hp = 190, Speed = 9.4f, Damage = 26.0f, FireRate = 2.4f, ProjSpeed = 48f, ProjScale = 0.55f, Aoe = 3.8f,
                BodyColor = Rgb(62, 155, 115), ArmorColor = Rgb(135, 78, 52), VisorColor = Rgb(110, 245, 200),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Удар Якорем (Anchor Smash)", Cd = 0.42f, Desc = "[ЛКМ / Q / ПКМ] Круговой сокрушительный взмах гигантским морским якорем с замедлением." },
                    ["m2"] = new() { Name = "Океанский Поток (Gush)", Cd = 3.8f, Desc = "[W / СКМ] Струя бездны в сторону курсора: 280% урона, замедление и раскол брони." },
                    ["shift"] = new() { Name = "Панцирь Кракена (Kraken Shell)", Cd = 4.5f, Desc = "[Shift] Таранный рывок вперёд, дающий +40 щита и сбивающий врагов с ног." },
                    ["r"] = new() { Name = "Опустошение (Ravage)", Cd = 12.0f, Desc = "[R] Шипы и щупальца бездны подбрасывают и оглушают ВСЕХ врагов в радиусе 18 метров!" }
                }
            },
            ["freeman"] = new()
            {
                Key = "freeman", Name = "ГОРДОН ФРИМАН", Icon = "hl2", Subtitle = "ВИД ОТ 1-ГО ЛИЦА | ГРАВИ-ПУШКА И ТАУ-ПУШКА",
                Hp = 140, Speed = 10.6f, Damage = 16.5f, FireRate = 5.6f, ProjSpeed = 74f, ProjScale = 0.24f, Aoe = 1.4f,
                BodyColor = Rgb(235, 125, 30), ArmorColor = Rgb(48, 54, 62), VisorColor = Rgb(255, 185, 45),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Монтировка и SMG (От 1-го лица)", Cd = 0.18f, Desc = "[ЛКМ] Огонь из автомата и сокрушительные удары монтировкой вблизи." },
                    ["m2"] = new() { Name = "Грави-пушка (Захват и Бросок)", Cd = 0.8f, Desc = "[ПКМ] Притягивает сундуки, кристаллы и камни с карты; повторное нажатие швыряет их во врагов!" },
                    ["shift"] = new() { Name = "Модуль длинных прыжков HEV", Cd = 3.8f, Desc = "[Shift] Длинный реактивный прыжок костюма HEV Mark IV и зарядка +25 щита." },
                    ["r"] = new() { Name = "Тау-пушка (Заряд при удержании R)", Cd = 5.0f, Desc = "[Удерживать R] Накапливает заряд Гаусса для колоссального выстрела и реактивного прыжка от отдачи!" }
                }
            },
            ["dante"] = new()
            {
                Key = "dante", Name = "ДАНТЕ (LIMBUS)", Icon = "DNT", Subtitle = "Менеджер №10 [БАФФЫ ОТРЯДА И ЖЕРТВЕННОЕ ВОЗРОЖДЕНИЕ]",
                Hp = 135, Speed = 10.5f, Damage = 18.0f, FireRate = 4.6f, ProjSpeed = 66f, ProjScale = 0.28f, Aoe = 2.0f,
                BodyColor = Rgb(185, 32, 42), ArmorColor = Rgb(28, 26, 34), VisorColor = Rgb(255, 205, 55),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Приказ: Цепи Грешников", Cd = 0.22f, Desc = "[ЛКМ] Единственная прямая атака Данте: пылающие золотые цепи, прошивающие врагов." },
                    ["m2"] = new() { Name = "Резонанс Ветви (Бафф Атаки)", Cd = 6.0f, Desc = "[ПКМ] Усиливает всех союзников и игроков (+45% урона и +35% скорости атаки на 7 сек)." },
                    ["shift"] = new() { Name = "Защитный Протокол (Бафф Щита)", Cd = 5.5f, Desc = "[Shift] Исцеляет отряд, даёт +45 щита и ускоряет передвижение всех союзников." },
                    ["r"] = new() { Name = "Поворот Часов Вспять (Жертва)", Cd = 12.0f, Desc = "[R] ВОЗРОЖДАЕТ всех погибших игроков и лечит союзников ЦЕНОЙ ЖИЗНИ самого Данте!" }
                }
            },
            ["cheburashka"] = new()
            {
                Key = "cheburashka", Name = "ЧЕБУРАШКА", Icon = "ЧБР", Subtitle = "Ушастый Друг [ПАДЕНИЕ С ВЫСОТЫ | СТЕНД: КРОКОДИЛ ГЕНА]",
                Hp = 130, Speed = 10.6f, Damage = 18.0f, FireRate = 4.2f, ProjSpeed = 48f, ProjScale = 0.38f, Aoe = 2.4f,
                BodyColor = Rgb(115, 68, 38), ArmorColor = Rgb(225, 185, 135), VisorColor = Rgb(255, 150, 25),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Удар ушами / Кулаки Гены", Cd = 0.24f, Desc = "[ЛКМ] Хлопок гигантскими ушами (со стендом — серия сокрушительных ударов и укусов Гены) + Пассивка: Удар при падении с высоты!" },
                    ["m2"] = new() { Name = "Взрывной апельсин / Гармошка Гены", Cd = 3.2f, Desc = "[ПКМ] Бросает апельсин, разрывающийся на 8 долек-шрапнелин (со стендом — звуковая волна гармошки и залп апельсинов)!" },
                    ["shift"] = new() { Name = "Бросок Шапокляк / Рывок Стенда", Cd = 4.2f, Desc = "[Shift] Шапокляк швыряет Чебурашку далеко вперёд и вверх для мощного приземления!" },
                    ["r"] = new() { Name = "СТЕНД: «КРОКОДИЛ ГЕНА» (30 сек.)", Cd = 60.0f, Desc = "[R] На 30 сек. призывает стенд Крокодила Гену, заменяя все атаки на сокрушительные комбо Гены!" }
                }
            }
        };

        public static SurvivorParts BuildSurvivorMesh(Entity parentEntity, string charKey)
        {
            if (!Classes.TryGetValue(charKey, out var cfg))
                cfg = Classes["commando"];

            var root = new Entity(parent: parentEntity);
            var parts = new SurvivorParts { Root = root };

            switch (charKey)
            {
                case "commando": BuildCommando(root, cfg, parts); break;
                case "artificer": BuildArtificer(root, cfg, parts); break;
                case "huntress": BuildHuntress(root, cfg, parts); break;
                case "buzz": BuildBuzz(root, cfg, parts); break;
                case "v1": BuildV1(root, cfg, parts); break;
                case "sol": BuildSol(root, cfg, parts); break;
                case "gwen": BuildGwen(root, cfg, parts); break;
                case "enforcer": BuildEnforcer(root, cfg, parts); break;
                case "scout": BuildScout(root, cfg, parts); break;
                case "m4a1": BuildM4A1(root, cfg, parts); break;
                case "medic": BuildMedic(root, cfg, parts); break;
                case "aigis": BuildAigis(root, cfg, parts); break;
                case "reimu": BuildReimu(root, cfg, parts); break;
                case "mindustry": BuildMindustry(root, cfg, parts); break;
                case "slark": BuildSlark(root, cfg, parts); break;
                case "tidehunter": BuildTidehunter(root, cfg, parts); break;
                case "freeman": BuildFreeman(root, cfg, parts); break;
                case "dante": BuildDante(root, cfg, parts); break;
                case "cheburashka": BuildCheburashka(root, cfg, parts); break;
                default: BuildCommando(root, cfg, parts); break;
            }

            return parts;
        }

        // ==========================================
        // 1. COMMANDO (Risk of Rain 2)
        // ==========================================
        private static void BuildCommando(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.22f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.22f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.44f, 0.26f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.08f, 0.40f, 0.24f), position: new(-side * 0.10f, -0.20f, 0), color: Rgb(32, 36, 44));
                new Entity(parent: hip, model: "cube", scale: new(0.12f, 0.26f, 0.22f), position: new(side * 0.15f, -0.18f, 0.02f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.04f, 0.28f), position: new(0, -0.14f, 0), color: Rgb(32, 36, 44));
                new Entity(parent: hip, model: "sphere", scale: new(0.22f, 0.20f, 0.22f), position: new(0, -0.42f, 0.02f), color: Rgb(35, 40, 50));
                new Entity(parent: hip, model: "diamond", scale: new(0.26f, 0.26f, 0.28f), position: new(0, -0.42f, 0.10f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.10f, 0.10f, 0.08f), position: new(0, -0.42f, 0.20f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.42f, 0.24f), position: new(0, -0.62f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.32f, 0.10f), position: new(0, -0.60f, 0.09f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.04f, 0.22f, 0.04f), position: new(0, -0.60f, 0.14f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.18f, 0.42f), position: new(0, -0.80f, 0.06f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.06f, 0.44f), position: new(0, -0.87f, 0.06f), color: Rgb(25, 28, 35));
                new Entity(parent: hip, model: "diamond", scale: new(0.24f, 0.14f, 0.18f), position: new(0, -0.80f, 0.24f), color: Rgb(35, 40, 50));
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.06f, 0.12f), position: new(0, -0.76f, -0.14f), color: cfg.VisorColor, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.62f, 0.20f, 0.42f), position: new(0, -0.32f, 0), color: Rgb(32, 36, 44));
            new Entity(parent: torso, model: "cube", scale: new(0.18f, 0.14f, 0.45f), position: new(0, -0.32f, 0), color: cfg.ArmorColor);
            foreach (float px in new[] { -0.18f, 0.18f })
                new Entity(parent: torso, model: "cube", scale: new(0.12f, 0.14f, 0.10f), position: new(px, -0.32f, 0.21f), color: cfg.ArmorColor);
            foreach (float px in new[] { -0.31f, 0.31f })
                new Entity(parent: torso, model: "cube", scale: new(0.10f, 0.18f, 0.24f), position: new(px, -0.31f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.70f, 0.62f, 0.44f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.64f, 0.48f, 0.20f), position: new(0, 0.10f, 0.14f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.58f, 0.42f, 0.28f), position: new(0, 0.12f, 0.18f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.20f, 0.24f, 0.32f), position: new(0, 0.14f, 0.24f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.72f, 0.08f, 0.48f), position: new(0, 0.08f, 0.02f), rotation: new(0, 0, 26f), color: Rgb(35, 40, 50));
            new Entity(parent: torso, model: "cube", scale: new(0.44f, 0.14f, 0.42f), position: new(0, 0.39f, 0), color: cfg.ArmorColor);

            var backpack = new Entity(parent: torso, position: new(0, 0.10f, -0.30f));
            new Entity(parent: backpack, model: "cube", scale: new(0.52f, 0.56f, 0.26f), color: cfg.ArmorColor);
            new Entity(parent: backpack, model: "cube", scale: new(0.38f, 0.38f, 0.16f), position: new(0, 0, -0.12f), color: Rgb(30, 35, 45));
            new Entity(parent: backpack, model: "cube", scale: new(0.12f, 0.30f, 0.20f), position: new(0, 0, -0.14f), color: cfg.VisorColor, unlit: true);
            foreach (float sideX in new[] { -0.18f, 0.18f })
            {
                var thruster = new Entity(parent: backpack, model: "cube", scale: new(0.16f, 0.26f, 0.16f), position: new(sideX, -0.14f, -0.06f), rotation: new(14, 0, 0), color: Rgb(40, 45, 55));
                new Entity(parent: thruster, model: "diamond", scale: new(0.20f, 0.18f, 0.20f), position: new(0, -0.16f, 0), color: cfg.ArmorColor);
                new Entity(parent: thruster, model: "diamond", scale: new(0.14f, 0.26f, 0.14f), position: new(0, -0.28f, 0), color: cfg.VisorColor, unlit: true);
            }
            new Entity(parent: backpack, model: "sphere", scaleUniform: 0.08f, position: new(0, 0.32f, -0.06f), color: Color.Red, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.58f, 0.56f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.36f, 0.54f), position: new(0, 0.14f, -0.02f), color: cfg.BodyColor);
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.42f, 0.44f), position: new(0, 0.02f, 0.15f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "cube", scale: new(0.56f, 0.08f, 0.38f), position: new(0, 0.18f, 0.12f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "cube", scale: new(0.24f, 0.16f, 0.22f), position: new(0, -0.18f, 0.22f), color: Rgb(35, 40, 50));
            foreach (float cx in new[] { -0.16f, 0.16f })
                new Entity(parent: head, model: "sphere", scale: new(0.12f, 0.12f, 0.10f), position: new(cx, -0.18f, 0.20f), color: Rgb(60, 68, 80));
            new Entity(parent: head, model: "cube", scale: new(0.04f, 0.38f, 0.04f), position: new(-0.28f, 0.28f, -0.06f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.08f, position: new(-0.28f, 0.48f, -0.06f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "cube", scale: new(0.08f, 0.08f, 0.16f), position: new(0.28f, 0.14f, 0.10f), color: Rgb(35, 40, 50));
            new Entity(parent: head, model: "sphere", scaleUniform: 0.07f, position: new(0.28f, 0.14f, 0.18f), color: Color.White, unlit: true);
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));
            for (int idx = 0; idx < 2; idx++)
            {
                var sh = idx == 0 ? lShoulder : rShoulder;
                float side = idx == 0 ? -1f : 1f;

                // Наплечник Commando
                new Entity(parent: sh, model: "diamond", scale: new(0.44f, 0.40f, 0.44f), position: new(0, 0.08f, 0), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "cube", scale: new(0.42f, 0.06f, 0.42f), position: new(0, 0.08f, 0), color: cfg.BodyColor);

                // Верхняя часть руки (бицепс)
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.06f, 0.24f, 0.20f), position: new(side * 0.08f, -0.16f, 0.02f), color: cfg.ArmorColor);

                // Локтевой сустав
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);

                // Предплечье и бронещиток наруча
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.22f, 0.20f, 0.22f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "cube", scale: new(0.04f, 0.14f, 0.16f), position: new(side * 0.10f, -0.30f, 0.22f), color: cfg.VisorColor, unlit: true);

                // Кисть / Перчатка
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.16f, 0.14f), position: new(0, -0.30f, 0.40f), color: Rgb(32, 36, 44));

                // Dual UES Phase Pistols в обеих руках
                var gun = new Entity(parent: sh, position: new(0, -0.30f, 0.44f));
                new Entity(parent: gun, model: "cube", scale: new(0.11f, 0.14f, 0.65f), position: new(0, 0.06f, 0.18f), color: Rgb(35, 40, 52));
                new Entity(parent: gun, model: "cube", scale: new(0.06f, 0.04f, 0.58f), position: new(0, 0.14f, 0.18f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: gun, model: "cube", scale: new(0.12f, 0.12f, 0.48f), position: new(0, -0.04f, 0.15f), color: cfg.ArmorColor);
                new Entity(parent: gun, model: "diamond", scale: new(0.08f, 0.08f, 0.22f), position: new(0, -0.10f, 0.28f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: gun, model: "cube", scale: new(0.10f, 0.22f, 0.14f), position: new(0, -0.16f, -0.02f), rotation: new(-20, 0, 0), color: cfg.BodyColor);
                new Entity(parent: gun, model: "cube", scale: new(0.11f, 0.04f, 0.16f), position: new(0, -0.27f, -0.06f), color: Rgb(25, 28, 35));
                new Entity(parent: gun, model: "cube", scale: new(0.10f, 0.10f, 0.08f), position: new(0, 0.06f, 0.52f), color: Rgb(70, 78, 92));
                new Entity(parent: gun, model: "sphere", scaleUniform: 0.06f, position: new(0, 0.06f, 0.56f), color: cfg.VisorColor, unlit: true);
            }
            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 2. ARTIFICER (Risk of Rain 2)
        // ==========================================
        private static void BuildArtificer(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.44f, 0.22f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.04f, 0.40f, 0.20f), position: new(side * 0.11f, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.26f, 0.28f, 0.28f), position: new(0, -0.42f, 0.06f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "sphere", scaleUniform: 0.08f, position: new(0, -0.42f, 0.16f), color: Color.SkyBlue, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.40f, 0.20f), position: new(0, -0.62f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.32f, 0.12f), position: new(0, -0.60f, 0.08f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.14f, 0.36f), position: new(0, -0.80f, 0.06f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.04f, 0.38f), position: new(0, -0.86f, 0.06f), color: Color.SkyBlue, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.20f, 0.38f), position: new(0, -0.32f, 0), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.22f, 0.22f, 0.42f), position: new(0, -0.32f, 0), color: cfg.VisorColor, unlit: true);
            // 4 раздельных подола мантии с золотой окантовкой
            foreach (float sx in new[] { -0.28f, 0.28f })
            {
                new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.85f, 0.44f), position: new(sx, -0.52f, 0), rotation: new(0, 0, sx * -16f), color: cfg.BodyColor);
                new Entity(parent: torso, model: "diamond", scale: new(0.28f, 0.22f, 0.46f), position: new(sx, -0.82f, 0), color: cfg.ArmorColor);
            }
            new Entity(parent: torso, model: "diamond", scale: new(0.58f, 0.90f, 0.20f), position: new(0, -0.54f, -0.22f), rotation: new(12, 0, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.54f, 0.70f, 0.16f), position: new(0, -0.48f, 0.20f), rotation: new(-10, 0, 0), color: cfg.BodyColor);

            new Entity(parent: torso, model: "cube", scale: new(0.64f, 0.60f, 0.40f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.66f, 0.52f, 0.36f), position: new(0, 0.12f, 0.12f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.22f, 0.30f, 0.32f), position: new(0, 0.14f, 0.20f), color: Color.Yellow, unlit: true);
            new Entity(parent: torso, model: "diamond", scale: new(0.72f, 0.32f, 0.72f), position: new(0, 0.38f, 0), color: cfg.ArmorColor);

            // Реактивный парящий ранец Архимага
            var jet = new Entity(parent: torso, position: new(0, 0.12f, -0.28f));
            new Entity(parent: jet, model: "diamond", scale: new(0.72f, 0.68f, 0.24f), color: cfg.ArmorColor);
            foreach (float sideX in new[] { -0.24f, 0.24f })
            {
                new Entity(parent: jet, model: "diamond", scale: new(0.18f, 0.72f, 0.18f), position: new(sideX, -0.16f, 0), rotation: new(14, 0, 0), color: cfg.BodyColor);
                new Entity(parent: jet, model: "sphere", scaleUniform: 0.14f, position: new(sideX, -0.48f, 0), color: Color.SkyBlue, unlit: true);
            }

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.56f, 0.52f), color: cfg.BodyColor);
            new Entity(parent: head, model: "diamond", scale: new(0.68f, 0.32f, 0.68f), position: new(0, 0.20f, 0), color: cfg.ArmorColor);
            new Entity(parent: head, model: "diamond", scale: new(0.22f, 0.52f, 0.38f), position: new(0, 0.38f, 0.06f), color: cfg.BodyColor);
            new Entity(parent: head, model: "cube", scale: new(0.48f, 0.14f, 0.36f), position: new(0, 0.04f, 0.16f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "diamond", scale: new(0.32f, 0.18f, 0.24f), position: new(0, -0.16f, 0.16f), color: cfg.ArmorColor);
            parts.Torso = torso;

            // Плазменное вращающееся гало (Halo)
            var halo = new Entity(parent: torso, position: new(0, 0.42f, -0.42f));
            new Entity(parent: halo, model: "diamond", scale: new(1.35f, 1.35f, 0.08f), color: Color.Gold, unlit: true);
            new Entity(parent: halo, model: "diamond", scale: new(1.35f, 1.35f, 0.08f), rotation: new(0, 0, 45f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: halo, model: "sphere", scaleUniform: 0.32f, color: Color.Magenta, unlit: true);
            new Entity(parent: halo, model: "sphere", scaleUniform: 0.16f, color: Color.Yellow, unlit: true);
            parts.Halo = halo;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));
            for (int idx = 0; idx < 2; idx++)
            {
                var sh = idx == 0 ? lShoulder : rShoulder;
                float side = idx == 0 ? -1f : 1f;

                // Наплечник мантии Архимага
                new Entity(parent: sh, model: "diamond", scale: new(0.46f, 0.44f, 0.46f), position: new(0, 0.08f, 0), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "diamond", scale: new(0.24f, 0.24f, 0.24f), position: new(side * 0.12f, 0.16f, 0), color: Color.Gold, unlit: true);

                // Верхняя часть руки и локоть
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);

                // Предплечье и золотой наруч
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "diamond", scale: new(0.24f, 0.24f, 0.28f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: cfg.BodyColor);

                if (idx == 0) // Левая рука: Charged Nano-Bomb
                {
                    var orb = new Entity(parent: sh, position: new(0, -0.30f, 0.52f));
                    new Entity(parent: orb, model: "sphere", scaleUniform: 0.46f, color: Color.Magenta, unlit: true);
                    new Entity(parent: orb, model: "sphere", scaleUniform: 0.26f, color: Color.Yellow, unlit: true);
                    foreach (float ang in new[] { 0f, 90f, 180f, 270f })
                        new Entity(parent: orb, model: "diamond", scale: new(0.16f, 0.16f, 0.16f), position: new(MathF.Cos(ang * 0.0174f) * 0.35f, MathF.Sin(ang * 0.0174f) * 0.35f, 0), color: Color.SkyBlue, unlit: true);
                }
                else // Правая рука: Plasma Bolt Gauntlet
                {
                    var plasma = new Entity(parent: sh, position: new(0, -0.30f, 0.50f));
                    new Entity(parent: plasma, model: "sphere", scaleUniform: 0.38f, color: Color.Yellow, unlit: true);
                    new Entity(parent: plasma, model: "diamond", scale: new(0.55f, 0.14f, 0.55f), color: Color.SkyBlue, unlit: true);
                    new Entity(parent: plasma, model: "diamond", scale: new(0.18f, 0.18f, 0.65f), position: new(0, 0, 0.30f), color: Color.White, unlit: true);
                }
            }
            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 3. HUNTRESS (Risk of Rain 2)
        // ==========================================
        private static void BuildHuntress(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.44f, 0.22f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.38f, 0.20f), position: new(side * 0.10f, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.26f, 0.26f, 0.28f), position: new(0, -0.42f, 0.06f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.08f, 0.08f, 0.06f), position: new(0, -0.42f, 0.16f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.20f), position: new(0, -0.62f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.04f, 0.22f), position: new(0, -0.54f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.16f, 0.38f), position: new(0, -0.80f, 0.06f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.18f, 0.12f, 0.16f), position: new(0, -0.80f, 0.20f), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.16f, 0.04f, 0.08f), position: new(0, -0.76f, -0.12f), color: Color.SkyBlue, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.20f, 0.38f), position: new(0, -0.32f, 0), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.18f, 0.18f, 0.42f), position: new(0, -0.32f, 0), color: cfg.VisorColor, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.62f, 0.60f, 0.38f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.54f, 0.48f, 0.26f), position: new(0, 0.12f, 0.14f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.18f, 0.24f, 0.28f), position: new(0, 0.12f, 0.20f), color: Color.Gold, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.08f, 0.42f), position: new(0, 0.06f, 0.02f), rotation: new(0, 0, -28f), color: cfg.ArmorColor);

            // Знаменитый развевающийся алый шарф Охотницы
            var scarf = new Entity(parent: torso, position: new(0, 0.38f, -0.06f));
            new Entity(parent: scarf, model: "cube", scale: new(0.54f, 0.16f, 0.42f), color: cfg.BodyColor);
            new Entity(parent: scarf, model: "diamond", scale: new(0.32f, 0.98f, 0.14f), position: new(-0.16f, -0.52f, -0.26f), rotation: new(20, 8, 0), color: cfg.BodyColor);
            new Entity(parent: scarf, model: "diamond", scale: new(0.28f, 0.85f, 0.14f), position: new(0.16f, -0.46f, -0.24f), rotation: new(22, -8, 0), color: cfg.BodyColor);

            // Колчан с энергетическими стрелами на спине
            var quiver = new Entity(parent: torso, position: new(0.22f, 0.16f, -0.28f), rotation: new(0, 0, -22f));
            new Entity(parent: quiver, model: "cube", scale: new(0.20f, 0.68f, 0.20f), color: cfg.ArmorColor);
            new Entity(parent: quiver, model: "cube", scale: new(0.22f, 0.08f, 0.22f), position: new(0, 0.26f, 0), color: cfg.BodyColor);
            foreach (float ax in new[] { -0.05f, 0.0f, 0.05f })
            {
                new Entity(parent: quiver, model: "cube", scale: new(0.04f, 0.40f, 0.04f), position: new(ax, 0.38f, 0), color: Rgb(35, 40, 50));
                new Entity(parent: quiver, model: "diamond", scale: new(0.08f, 0.18f, 0.08f), position: new(ax, 0.54f, 0), color: Color.SkyBlue, unlit: true);
            }

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.56f, 0.54f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "diamond", scale: new(0.66f, 0.60f, 0.66f), position: new(0, 0.08f, -0.04f), color: cfg.BodyColor);
            // Визор-сенсор Охотницы с горизонтальной световой полосой
            new Entity(parent: head, model: "cube", scale: new(0.46f, 0.10f, 0.38f), position: new(0, 0.04f, 0.18f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(0, 0.04f, 0.26f), color: Color.Yellow, unlit: true);
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.46f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.46f, 0.25f, 0));

            // Левая рука: Композитный Лазерный Лук (Strafe Bow)
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.38f, 0.34f, 0.38f), position: new(0, 0.06f, 0), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: cfg.BodyColor);

            var bow = new Entity(parent: lShoulder, position: new(-0.06f, -0.30f, 0.44f), rotation: new(0, 0, 16f));
            new Entity(parent: bow, model: "diamond", scale: new(0.12f, 1.45f, 0.24f), color: cfg.ArmorColor);
            new Entity(parent: bow, model: "diamond", scale: new(0.15f, 0.55f, 0.30f), color: cfg.BodyColor);
            foreach (float py in new[] { -0.68f, 0.68f })
            {
                new Entity(parent: bow, model: "sphere", scaleUniform: 0.15f, position: new(0, py, 0), color: Color.Gold, unlit: true);
                new Entity(parent: bow, model: "diamond", scale: new(0.10f, 0.24f, 0.18f), position: new(0, py + (py > 0 ? 0.08f : -0.08f), -0.05f), color: cfg.ArmorColor);
            }
            new Entity(parent: bow, model: "cube", scale: new(0.03f, 1.35f, 0.03f), position: new(0, 0, -0.10f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: bow, model: "diamond", scale: new(0.07f, 0.07f, 0.95f), position: new(0, 0, 0.22f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: bow, model: "diamond", scale: new(0.14f, 0.14f, 0.22f), position: new(0, 0, 0.72f), color: Color.Gold, unlit: true);
            parts.Bow = bow;

            // Правая рука: Лазерная Глефа (Laser Glaive)
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.38f, 0.34f, 0.38f), position: new(0, 0.06f, 0), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: cfg.BodyColor);

            var glaive = new Entity(parent: rShoulder, position: new(0.04f, -0.30f, 0.44f));
            new Entity(parent: glaive, model: "sphere", scaleUniform: 0.22f, color: Color.Gold, unlit: true);
            new Entity(parent: glaive, model: "sphere", scaleUniform: 0.12f, color: Color.White, unlit: true);
            for (int i = 0; i < 3; i++)
            {
                var blade = new Entity(parent: glaive, rotation: new(0, 0, i * 120f));
                new Entity(parent: blade, model: "diamond", scale: new(0.14f, 0.52f, 0.09f), position: new(0, 0.26f, 0), color: Color.SkyBlue, unlit: true);
                new Entity(parent: blade, model: "sphere", scaleUniform: 0.09f, position: new(0, 0.46f, 0), color: Color.White, unlit: true);
            }

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 4. BUZZ LIGHTYEAR (Star Command)
        // ==========================================
        private static void BuildBuzz(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.24f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.24f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.44f, 0.28f), position: new(0, -0.20f, 0), color: Color.White);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.12f, 0.30f), position: new(0, -0.42f, 0), color: Rgb(145, 55, 225));
                new Entity(parent: hip, model: "diamond", scale: new(0.28f, 0.26f, 0.32f), position: new(0, -0.42f, 0.08f), color: Rgb(125, 235, 65));
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.40f, 0.26f), position: new(0, -0.62f, 0), color: Color.White);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.08f, 0.28f), position: new(0, -0.74f, 0.04f), color: Rgb(125, 235, 65));
                new Entity(parent: hip, model: "cube", scale: new(0.30f, 0.18f, 0.46f), position: new(0, -0.80f, 0.08f), color: Color.White);
                new Entity(parent: hip, model: "cube", scale: new(0.32f, 0.06f, 0.48f), position: new(0, -0.87f, 0.08f), color: Rgb(125, 235, 65));
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.04f, 0.20f), position: new(0, -0.88f, 0.12f), color: Color.SkyBlue, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.66f, 0.20f, 0.44f), position: new(0, -0.32f, 0), color: Rgb(125, 235, 65));
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.32f, 0.22f), color: Color.Red, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.64f, 0.16f, 0.42f), position: new(0, -0.16f, 0), color: Rgb(35, 38, 44));
            new Entity(parent: torso, model: "cube", scale: new(0.74f, 0.54f, 0.48f), position: new(0, 0.12f, 0), color: Color.White);
            new Entity(parent: torso, model: "cube", scale: new(0.76f, 0.36f, 0.50f), position: new(0, 0.22f, 0.02f), color: Rgb(125, 235, 65));
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.12f, 0.52f), position: new(0, 0.36f, 0.02f), color: Rgb(145, 55, 225));

            // Кнопки Star Command на груди
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.10f, position: new(-0.20f, 0.24f, 0.28f), color: Color.Blue, unlit: true);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.10f, position: new(-0.08f, 0.24f, 0.28f), color: Color.Lime, unlit: true);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.10f, position: new(0.04f, 0.24f, 0.28f), color: Color.Red, unlit: true);
            // Красная треугольная кнопка лазера
            new Entity(parent: torso, model: "diamond", scale: new(0.18f, 0.14f, 0.10f), position: new(0.20f, 0.24f, 0.28f), color: Color.Red, unlit: true);
            // Эмблема Star Command
            new Entity(parent: torso, model: "diamond", scale: new(0.22f, 0.12f, 0.08f), position: new(0, 0.32f, 0.28f), color: Color.Gold, unlit: true);

            // Реактивный ранец и крылья Базза Лайтера
            var pack = new Entity(parent: torso, position: new(0, 0.12f, -0.30f));
            new Entity(parent: pack, model: "cube", scale: new(0.54f, 0.60f, 0.26f), color: Color.White);
            new Entity(parent: pack, model: "cube", scale: new(0.40f, 0.40f, 0.14f), position: new(0, 0, -0.12f), color: Rgb(145, 55, 225));
            foreach (float sideX in new[] { -0.18f, 0.18f })
            {
                var cone = new Entity(parent: pack, model: "cube", scale: new(0.14f, 0.24f, 0.14f), position: new(sideX, -0.22f, -0.06f), rotation: new(12, 0, 0), color: Rgb(145, 55, 225));
                new Entity(parent: cone, model: "diamond", scale: new(0.18f, 0.24f, 0.18f), position: new(0, -0.16f, 0), color: Color.Yellow, unlit: true);
            }
            // Развёрнутые знаменитые крылья Косморейнджера с габаритными огнями!
            foreach (int side in new[] { -1, 1 })
            {
                var wing = new Entity(parent: pack, model: "cube", scale: new(1.35f, 0.32f, 0.08f), position: new(side * 0.85f, 0.14f, -0.06f), rotation: new(0, 0, side * -8f), color: Rgb(155, 65, 245));
                new Entity(parent: wing, model: "cube", scale: new(1.15f, 0.42f, 0.12f), position: new(0, 0.32f, 0), color: Color.White);
                new Entity(parent: wing, model: "cube", scale: new(1.12f, 0.20f, 0.14f), position: new(0, 0.42f, 0), color: Color.Red, unlit: true);
                new Entity(parent: wing, model: "sphere", scale: new(0.20f, 0.72f, 0.38f), position: new(side * 0.58f, 0, 0), color: side > 0 ? Color.Lime : Color.Red, unlit: true);
            }

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.58f, 0.56f), color: Rgb(145, 55, 225));
            new Entity(parent: head, model: "sphere", scale: new(0.44f, 0.44f, 0.40f), position: new(0, -0.02f, 0.12f), color: Rgb(245, 205, 175));
            new Entity(parent: head, model: "cube", scale: new(0.24f, 0.14f, 0.20f), position: new(0, -0.16f, 0.22f), color: Rgb(235, 190, 160));
            // Подбородочная завитушка Базза
            new Entity(parent: head, model: "sphere", scaleUniform: 0.09f, position: new(0, -0.19f, 0.28f), color: Rgb(215, 165, 135));
            // Прозрачный сферический купол шлема
            new Entity(parent: head, model: "sphere", scale: new(0.78f, 0.78f, 0.78f), color: Rgb(190, 240, 255, 80), unlit: true);
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.52f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.52f, 0.25f, 0));
            for (int idx = 0; idx < 2; idx++)
            {
                var sh = idx == 0 ? lShoulder : rShoulder;

                // Сферический плечевой сустав Косморейнджера с фиолетовым кольцом
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.40f, position: new(0, 0.06f, 0), color: Color.White);
                new Entity(parent: sh, model: "cube", scale: new(0.42f, 0.08f, 0.42f), position: new(0, 0.06f, 0), color: Rgb(145, 55, 225));

                // Бицепс
                new Entity(parent: sh, model: "cube", scale: new(0.22f, 0.28f, 0.22f), position: new(0, -0.16f, 0.02f), color: Color.White);
                // Локоть
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.20f, position: new(0, -0.30f, 0.02f), color: Rgb(145, 55, 225));

                // Предплечье (Зелёная бронерукав Star Command)
                new Entity(parent: sh, model: "cube", scale: new(0.24f, 0.22f, 0.38f), position: new(0, -0.30f, 0.22f), color: Rgb(125, 235, 65));
                new Entity(parent: sh, model: "cube", scale: new(0.26f, 0.24f, 0.12f), position: new(0, -0.30f, 0.12f), color: Color.White);

                // Кисть / Белая перчатка с фиолетовыми костяшками
                new Entity(parent: sh, model: "cube", scale: new(0.20f, 0.18f, 0.16f), position: new(0, -0.30f, 0.42f), color: Color.White);
                new Entity(parent: sh, model: "cube", scale: new(0.21f, 0.08f, 0.12f), position: new(0, -0.26f, 0.42f), color: Rgb(145, 55, 225));

                if (idx == 1) // Правая рука: Наручный лазер с открывающейся крышкой
                {
                    var laser = new Entity(parent: sh, position: new(0, -0.18f, 0.28f));
                    new Entity(parent: laser, model: "cube", scale: new(0.18f, 0.09f, 0.30f), color: Color.Yellow);
                    new Entity(parent: laser, model: "sphere", scaleUniform: 0.14f, position: new(0, 0.06f, 0.16f), color: Color.Red, unlit: true);
                }
                else // Левая рука: Коммуникатор
                {
                    var comm = new Entity(parent: sh, position: new(0, -0.18f, 0.28f));
                    new Entity(parent: comm, model: "cube", scale: new(0.18f, 0.08f, 0.26f), color: Rgb(35, 38, 44));
                    new Entity(parent: comm, model: "cube", scale: new(0.14f, 0.04f, 0.18f), position: new(0, 0.04f, 0), color: Color.SkyBlue, unlit: true);
                }
            }
            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 5. V1 (ULTRAKILL)
        // ==========================================
        private static void BuildV1(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.44f, 0.20f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.38f, 0.06f), position: new(side * 0.10f, -0.20f, 0), color: Color.LightGray);
                new Entity(parent: hip, model: "diamond", scale: new(0.24f, 0.24f, 0.24f), position: new(0, -0.42f, 0.04f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.16f, 0.44f, 0.18f), position: new(0, -0.62f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.12f, 0.36f, 0.08f), position: new(0, -0.60f, 0.08f), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.16f, 0.20f, 0.42f), position: new(0, -0.80f, 0.10f), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.12f, 0.18f, 0.16f), position: new(0, -0.80f, -0.12f), color: Color.LightGray);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.48f, 0.18f, 0.34f), position: new(0, -0.32f, 0), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.12f, 0.22f, 0.12f), position: new(0, -0.22f, 0), color: Color.LightGray);
            foreach (float sideX in new[] { -0.14f, 0.14f })
                new Entity(parent: torso, model: "cube", scale: new(0.06f, 0.22f, 0.06f), position: new(sideX, -0.22f, 0.04f), color: Color.LightGray);
            new Entity(parent: torso, model: "cube", scale: new(0.58f, 0.54f, 0.38f), position: new(0, 0.08f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.52f, 0.44f, 0.26f), position: new(0, 0.14f, 0.14f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.12f, 0.24f, 0.08f), position: new(0, 0.12f, 0.22f), color: Color.Red, unlit: true);

            // Крылья V1 (8 излучателей плазменных перьев)
            var wingHub = new Entity(parent: torso, position: new(0, 0.16f, -0.26f));
            new Entity(parent: wingHub, model: "cube", scale: new(0.34f, 0.44f, 0.18f), color: cfg.ArmorColor);
            foreach (int side in new[] { -1, 1 })
            {
                foreach (float ang in new[] { 44f, 18f, -8f, -34f })
                {
                    var w = new Entity(parent: wingHub, rotation: new(0, side * -12f, side * ang));
                    new Entity(parent: w, model: "cube", scale: new(0.42f, 0.06f, 0.10f), position: new(side * 0.28f, 0, 0), color: cfg.BodyColor);
                    new Entity(parent: w, model: "diamond", scale: new(1.25f, 0.14f, 0.08f), position: new(side * 0.76f, 0, 0), color: cfg.VisorColor, unlit: true);
                    new Entity(parent: w, model: "sphere", scaleUniform: 0.09f, position: new(side * 1.38f, 0, 0), color: Color.White, unlit: true);
                }
            }

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            var camHead = new Entity(parent: head, model: "cube", scale: new(0.32f, 0.30f, 0.64f), position: new(0, 0.02f, 0.06f), color: cfg.BodyColor);
            foreach (float sx in new[] { -0.19f, 0.19f })
                new Entity(parent: camHead, model: "diamond", scale: new(0.10f, 0.38f, 0.82f), position: new(sx, 0.04f, -0.05f), color: Rgb(35, 90, 195));
            new Entity(parent: camHead, model: "cube", scale: new(0.38f, 0.38f, 0.14f), position: new(0, 0, 0.46f), color: Rgb(24, 28, 38));
            // Одиночный жёлтый оптический глаз V1
            new Entity(parent: camHead, model: "sphere", scale: new(0.34f, 0.36f, 0.20f), position: new(0, 0, 0.52f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: camHead, model: "sphere", scaleUniform: 0.14f, position: new(0, 0, 0.60f), color: Color.White, unlit: true);
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.44f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.44f, 0.25f, 0));

            // Левая рука: Feedbacker (Синяя рука с подброшенной монеткой)
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.32f, 0.32f, 0.32f), position: new(0, 0.06f, 0), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.26f, 0.14f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.30f, 0.02f), color: Color.LightGray);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.15f, 0.15f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.40f), color: Color.LightGray);
            // Парящая золотая монетка (+RICOSHOT)
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.24f, 0.24f, 0.07f), position: new(0, -0.06f, 0.46f), color: Color.Gold, unlit: true);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.10f, position: new(0, -0.06f, 0.46f), color: Color.White, unlit: true);

            // Правая рука: Револьвер Marksman
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.32f, 0.32f, 0.32f), position: new(0, 0.06f, 0), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.26f, 0.14f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.30f, 0.02f), color: Color.LightGray);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.15f, 0.15f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.14f), position: new(0, -0.30f, 0.40f), color: Color.LightGray);

            var rev = new Entity(parent: rShoulder, position: new(0, -0.30f, 0.44f));
            new Entity(parent: rev, model: "cube", scale: new(0.11f, 0.20f, 0.78f), position: new(0, 0.04f, 0.24f), color: Rgb(30, 36, 48));
            new Entity(parent: rev, model: "sphere", scale: new(0.20f, 0.20f, 0.26f), position: new(0, 0.04f, 0.06f), color: Rgb(75, 85, 105));
            new Entity(parent: rev, model: "cube", scale: new(0.09f, 0.05f, 0.70f), position: new(0, 0.16f, 0.24f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: rev, model: "cube", scale: new(0.09f, 0.22f, 0.14f), position: new(0, -0.14f, -0.04f), rotation: new(-20, 0, 0), color: Rgb(35, 40, 52));
            new Entity(parent: rev, model: "diamond", scale: new(0.08f, 0.08f, 0.12f), position: new(0, 0.14f, 0.62f), color: Color.Gold, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 6. SOL BADGUY (Guilty Gear)
        // ==========================================
        private static void BuildSol(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color diCrimson = Rgb(125, 22, 36);
            Color diGlow = Rgb(255, 175, 65);
            Color diFlame = Rgb(255, 245, 165);
            Color diJacket = Rgb(145, 34, 32);
            Color diObsidian = Rgb(24, 20, 26);

            var lHip = new Entity(parent: root, position: new(-0.22f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.22f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                // Обычная форма ног
                var normLeg = new Entity(parent: hip);
                new Entity(parent: normLeg, model: "cube", scale: new(0.24f, 0.44f, 0.26f), position: new(0, -0.20f, 0), color: Color.White);
                new Entity(parent: normLeg, model: "cube", scale: new(0.06f, 0.42f, 0.26f), position: new(side * 0.11f, -0.20f, 0), color: Rgb(40, 35, 38));
                new Entity(parent: normLeg, model: "cube", scale: new(0.26f, 0.24f, 0.28f), position: new(0, -0.42f, 0.06f), color: Rgb(40, 35, 38));
                new Entity(parent: normLeg, model: "cube", scale: new(0.12f, 0.08f, 0.06f), position: new(0, -0.42f, 0.18f), color: Color.LightGray);
                new Entity(parent: normLeg, model: "cube", scale: new(0.22f, 0.42f, 0.24f), position: new(0, -0.62f, 0), color: cfg.BodyColor);
                new Entity(parent: normLeg, model: "cube", scale: new(0.26f, 0.18f, 0.42f), position: new(0, -0.80f, 0.06f), color: Rgb(35, 30, 32));
                new Entity(parent: normLeg, model: "diamond", scale: new(0.24f, 0.12f, 0.18f), position: new(0, -0.80f, 0.24f), color: Color.LightGray);
                parts.NormalFormNodes.Add(normLeg);

                // Форма Dragon Install: багрово-огненные мускулистые ноги и когтистые лапы
                var diLeg = new Entity(parent: hip, enabled: false);
                new Entity(parent: diLeg, model: "sphere", scale: new(0.28f, 0.46f, 0.30f), position: new(0, -0.20f, 0), color: diCrimson);
                new Entity(parent: diLeg, model: "diamond", scale: new(0.10f, 0.38f, 0.32f), position: new(0, -0.20f, 0.02f), color: diGlow, unlit: true);
                new Entity(parent: diLeg, model: "sphere", scale: new(0.24f, 0.22f, 0.26f), position: new(0, -0.42f, 0.04f), color: diGlow, unlit: true);
                new Entity(parent: diLeg, model: "diamond", scale: new(0.24f, 0.44f, 0.26f), position: new(0, -0.62f, 0), color: diGlow, unlit: true);
                new Entity(parent: diLeg, model: "diamond", scale: new(0.30f, 0.16f, 0.46f), position: new(0, -0.80f, 0.10f), color: diCrimson);
                foreach (float cx in new[] { -0.10f, -0.03f, 0.03f, 0.10f })
                    new Entity(parent: diLeg, model: "diamond", scale: new(0.05f, 0.08f, 0.20f), position: new(cx, -0.83f, 0.34f), rotation: new(-15, 0, 0), color: diFlame, unlit: true);
                parts.UltFormNodes.Add(diLeg);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));

            // Обычный торс и голова Сола
            var normTorso = new Entity(parent: torso);
            new Entity(parent: normTorso, model: "cube", scale: new(0.62f, 0.20f, 0.42f), position: new(0, -0.32f, 0), color: Rgb(50, 42, 38));
            new Entity(parent: normTorso, model: "cube", scale: new(0.28f, 0.16f, 0.46f), position: new(0, -0.32f, 0.02f), color: Color.Gold, unlit: true);
            new Entity(parent: normTorso, model: "cube", scale: new(0.64f, 0.60f, 0.40f), position: new(0, 0.06f, 0), color: Rgb(235, 185, 155));
            new Entity(parent: normTorso, model: "cube", scale: new(0.68f, 0.58f, 0.20f), position: new(0, 0.08f, -0.16f), color: cfg.BodyColor);
            foreach (float sideX in new[] { -0.32f, 0.32f })
                new Entity(parent: normTorso, model: "cube", scale: new(0.14f, 0.58f, 0.42f), position: new(sideX, 0.08f, 0), color: cfg.BodyColor);
            new Entity(parent: normTorso, model: "cube", scale: new(0.58f, 0.22f, 0.46f), position: new(0, 0.38f, -0.04f), color: cfg.BodyColor);
            new Entity(parent: normTorso, model: "cube", scale: new(0.72f, 0.54f, 0.14f), position: new(0, -0.38f, -0.20f), rotation: new(12, 0, 0), color: cfg.BodyColor);

            var head = new Entity(parent: normTorso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(245, 200, 170));
            new Entity(parent: head, model: "cube", scale: new(0.56f, 0.14f, 0.56f), position: new(0, 0.14f, 0), color: cfg.BodyColor);
            new Entity(parent: head, model: "cube", scale: new(0.26f, 0.12f, 0.08f), position: new(0, 0.14f, 0.27f), color: Color.LightGray);
            foreach (float lx in new[] { -0.12f, 0.12f })
                new Entity(parent: head, model: "diamond", scale: new(0.12f, 0.45f, 0.10f), position: new(lx, 0.02f, -0.32f), rotation: new(28, 0, 0), color: cfg.BodyColor);
            foreach (var (hx, hy, hz, rx) in new[] {
                (0f, 0.34f, -0.16f, 35f), (-0.24f, 0.22f, -0.20f, 30f), (0.24f, 0.22f, -0.20f, 30f),
                (0f, -0.06f, -0.36f, 42f), (-0.20f, 0.28f, 0.08f, -15f), (0.20f, 0.28f, 0.08f, -15f) })
            {
                new Entity(parent: head, model: "diamond", scale: new(0.30f, 0.56f, 0.38f), position: new(hx, hy, hz), rotation: new(rx, 0, 0), color: Rgb(52, 32, 28));
            }
            parts.NormalFormNodes.Add(normTorso);

            // Торс и рогатая голова Dragon Install (по скриншоту)
            var diTorso = new Entity(parent: torso, enabled: false);
            // Багровый рельефный торс со светящимися прожилками пресса
            new Entity(parent: diTorso, model: "cube", scale: new(0.64f, 0.76f, 0.42f), position: new(0, -0.02f, 0), color: diCrimson);
            new Entity(parent: diTorso, model: "cube", scale: new(0.05f, 0.68f, 0.44f), position: new(0, -0.02f, 0.01f), color: diGlow, unlit: true);
            foreach (float ay in new[] { -0.22f, -0.06f, 0.10f })
                new Entity(parent: diTorso, model: "cube", scale: new(0.48f, 0.04f, 0.44f), position: new(0, ay, 0.01f), color: diGlow, unlit: true);
            // Укороченная рваная куртка на груди и плечах с шипами на лацканах и цепью
            new Entity(parent: diTorso, model: "cube", scale: new(0.76f, 0.38f, 0.48f), position: new(0, 0.22f, -0.02f), color: diJacket);
            new Entity(parent: diTorso, model: "diamond", scale: new(0.84f, 0.22f, 0.52f), position: new(0, 0.40f, -0.04f), color: diJacket);
            foreach (float sx in new[] { -0.26f, 0.26f })
            {
                new Entity(parent: diTorso, model: "diamond", scale: new(0.10f, 0.10f, 0.14f), position: new(sx, 0.26f, 0.24f), color: Color.LightGray);
                new Entity(parent: diTorso, model: "diamond", scale: new(0.10f, 0.10f, 0.14f), position: new(sx * 0.75f, 0.12f, 0.24f), color: Color.LightGray);
            }
            // Массивная тёмная цепь на груди слева
            for (int c = 0; c < 3; c++)
                new Entity(parent: diTorso, model: "cube", scale: new(0.12f, 0.16f, 0.08f), position: new(0.12f + c * 0.04f, 0.28f - c * 0.12f, 0.24f), rotation: new(0, 0, -20f), color: diObsidian);
            // Демонический хвост сзади
            var diTail = new Entity(parent: diTorso, position: new(0, -0.32f, -0.22f), rotation: new(-28, 0, 0));
            new Entity(parent: diTail, model: "diamond", scale: new(0.22f, 0.22f, 0.95f), position: new(0, 0, -0.40f), color: diCrimson);
            new Entity(parent: diTail, model: "diamond", scale: new(0.16f, 0.24f, 0.42f), position: new(0, 0.04f, -0.88f), color: diGlow, unlit: true);

            // Голова Dragon Install с большими чёрными рогами, пламенем и гривой
            var diHead = new Entity(parent: diTorso, position: new(0, 0.58f, 0.04f));
            new Entity(parent: diHead, model: "diamond", scale: new(0.46f, 0.52f, 0.50f), color: diObsidian);
            new Entity(parent: diHead, model: "cube", scale: new(0.36f, 0.24f, 0.38f), position: new(0, -0.08f, 0.08f), color: diCrimson);
            foreach (float ex in new[] { -0.11f, 0.11f })
                new Entity(parent: diHead, model: "sphere", scaleUniform: 0.10f, position: new(ex, 0.04f, 0.22f), color: diFlame, unlit: true);
            // Главные длинные чёрные рога + бело-жёлтое пламя на вершинах рогов
            foreach (int s in new[] { -1, 1 })
            {
                var horn = new Entity(parent: diHead, position: new(s * 0.16f, 0.26f, 0.02f), rotation: new(18f, 0, s * -22f));
                new Entity(parent: horn, model: "diamond", scale: new(0.18f, 0.88f, 0.24f), position: new(0, 0.34f, 0), color: diObsidian);
                new Entity(parent: horn, model: "diamond", scale: new(0.22f, 0.52f, 0.26f), position: new(s * 0.04f, 0.68f, -0.06f), color: diFlame, unlit: true);
                // Боковые малые рога
                new Entity(parent: diHead, model: "diamond", scale: new(0.12f, 0.42f, 0.14f), position: new(s * 0.28f, 0.18f, -0.04f), rotation: new(-12f, 0, s * -42f), color: diObsidian);
            }
            // Тёмно-багровая грива до пояса
            new Entity(parent: diHead, model: "diamond", scale: new(0.48f, 0.92f, 0.36f), position: new(0, -0.22f, -0.28f), rotation: new(24, 0, 0), color: Rgb(85, 16, 26));
            parts.UltFormNodes.Add(diTorso);
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));

            // Левая рука (Обычная + Dragon Install с кандалами на плече и когтями)
            var normLArm = new Entity(parent: lShoulder);
            new Entity(parent: normLArm, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0.02f), color: Rgb(235, 185, 155));
            new Entity(parent: normLArm, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: Rgb(235, 185, 155));
            new Entity(parent: normLArm, model: "cube", scale: new(0.20f, 0.20f, 0.38f), position: new(0, -0.30f, 0.22f), color: Rgb(40, 35, 38));
            new Entity(parent: normLArm, model: "cube", scale: new(0.22f, 0.10f, 0.20f), position: new(0, -0.30f, 0.22f), color: cfg.BodyColor);
            new Entity(parent: normLArm, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.42f), color: Rgb(40, 35, 38));
            new Entity(parent: normLArm, model: "diamond", scale: new(0.12f, 0.12f, 0.12f), position: new(0, -0.22f, 0.42f), color: Color.Gold, unlit: true);
            parts.NormalFormNodes.Add(normLArm);

            var diLArm = new Entity(parent: lShoulder, enabled: false);
            new Entity(parent: diLArm, model: "cube", scale: new(0.30f, 0.32f, 0.30f), position: new(0, 0.02f, 0), color: diJacket);
            // Металлическое кольцо-кандалы на плече
            new Entity(parent: diLArm, model: "diamond", scale: new(0.36f, 0.10f, 0.36f), position: new(-0.06f, -0.06f, 0), color: Color.LightGray);
            new Entity(parent: diLArm, model: "cube", scale: new(0.24f, 0.28f, 0.24f), position: new(0, -0.20f, 0.04f), color: diJacket);
            new Entity(parent: diLArm, model: "cube", scale: new(0.26f, 0.14f, 0.26f), position: new(0, -0.30f, 0.24f), color: diObsidian);
            new Entity(parent: diLArm, model: "diamond", scale: new(0.10f, 0.10f, 0.14f), position: new(-0.12f, -0.30f, 0.24f), color: Color.LightGray);
            new Entity(parent: diLArm, model: "sphere", scale: new(0.20f, 0.18f, 0.22f), position: new(0, -0.30f, 0.42f), color: diCrimson);
            foreach (float cx in new[] { -0.06f, 0f, 0.06f })
                new Entity(parent: diLArm, model: "diamond", scale: new(0.04f, 0.05f, 0.16f), position: new(cx, -0.32f, 0.54f), color: diFlame, unlit: true);
            parts.UltFormNodes.Add(diLArm);

            // Правая рука: Меч Junkyard Dog (обычный и тёмный монолитный в форме Dragon Install)
            var normRArm = new Entity(parent: rShoulder);
            new Entity(parent: normRArm, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0.02f), color: Rgb(235, 185, 155));
            new Entity(parent: normRArm, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: Rgb(235, 185, 155));
            new Entity(parent: normRArm, model: "cube", scale: new(0.20f, 0.20f, 0.38f), position: new(0, -0.30f, 0.22f), color: Rgb(40, 35, 38));
            new Entity(parent: normRArm, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.42f), color: Rgb(40, 35, 38));

            var sword = new Entity(parent: normRArm, position: new(0, -0.28f, 0.44f), rotation: new(16, 0, 0));
            new Entity(parent: sword, model: "cube", scale: new(0.18f, 0.28f, 0.34f), position: new(0, 0, 0.06f), color: Rgb(55, 60, 70));
            new Entity(parent: sword, model: "cube", scale: new(0.16f, 0.32f, 1.65f), position: new(0, 0, 0.85f), color: cfg.BodyColor);
            new Entity(parent: sword, model: "cube", scale: new(0.18f, 0.14f, 1.58f), position: new(0, 0, 0.56f), color: Rgb(255, 165, 30), unlit: true);
            new Entity(parent: sword, model: "diamond", scale: new(0.14f, 0.22f, 1.70f), position: new(0, -0.14f, 0.88f), color: Color.LightGray);
            foreach (float ez in new[] { 0.45f, 0.85f, 1.25f })
                new Entity(parent: sword, model: "cube", scale: new(0.20f, 0.18f, 0.12f), position: new(0, 0.20f, ez), color: Rgb(40, 42, 50));
            parts.NormalFormNodes.Add(normRArm);

            var diRArm = new Entity(parent: rShoulder, enabled: false);
            new Entity(parent: diRArm, model: "cube", scale: new(0.30f, 0.32f, 0.30f), position: new(0, 0.02f, 0), color: diJacket);
            new Entity(parent: diRArm, model: "cube", scale: new(0.24f, 0.28f, 0.24f), position: new(0, -0.20f, 0.04f), color: diJacket);
            new Entity(parent: diRArm, model: "cube", scale: new(0.26f, 0.14f, 0.26f), position: new(0, -0.30f, 0.24f), color: diObsidian);
            new Entity(parent: diRArm, model: "sphere", scale: new(0.20f, 0.18f, 0.22f), position: new(0, -0.30f, 0.42f), color: diCrimson);

            var diSword = new Entity(parent: diRArm, position: new(0, -0.28f, 0.44f), rotation: new(16, 0, 0));
            new Entity(parent: diSword, model: "cube", scale: new(0.20f, 0.36f, 1.75f), position: new(0, 0, 0.88f), color: diObsidian);
            new Entity(parent: diSword, model: "cube", scale: new(0.22f, 0.10f, 1.68f), position: new(0, 0, 0.88f), color: diGlow, unlit: true);
            parts.UltFormNodes.Add(diRArm);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 7. GWEN (League of Legends)
        // ==========================================
        private static void BuildGwen(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.44f, 0.20f), position: new(0, -0.20f, 0), color: side < 0 ? Rgb(32, 28, 46) : Rgb(248, 225, 220));
                new Entity(parent: hip, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.42f, 0.02f), color: Rgb(248, 225, 220));
                new Entity(parent: hip, model: "cube", scale: new(0.16f, 0.42f, 0.18f), position: new(0, -0.62f, 0), color: Rgb(32, 28, 46));
                new Entity(parent: hip, model: "diamond", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.80f, 0.06f), color: Rgb(32, 28, 46));
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.06f, 0.06f), position: new(0, -0.78f, 0.18f), color: Color.Gold, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            // Готическое платье с пышным кринолином
            new Entity(parent: torso, model: "diamond", scale: new(1.18f, 0.72f, 1.18f), position: new(0, -0.36f, 0), color: Rgb(32, 28, 46));
            new Entity(parent: torso, model: "diamond", scale: new(1.26f, 0.24f, 1.26f), position: new(0, -0.56f, 0), color: Rgb(195, 240, 255), unlit: true);
            new Entity(parent: torso, model: "diamond", scale: new(0.68f, 0.38f, 0.26f), position: new(0, -0.18f, -0.28f), color: cfg.BodyColor, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.52f, 0.54f, 0.36f), position: new(0, 0.08f, 0), color: Rgb(32, 28, 46));
            new Entity(parent: torso, model: "diamond", scale: new(0.44f, 0.38f, 0.22f), position: new(0, 0.12f, 0.12f), color: Rgb(240, 245, 255));
            new Entity(parent: torso, model: "diamond", scale: new(0.16f, 0.16f, 0.16f), position: new(0, 0.16f, 0.20f), color: Color.Gold, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 225, 220));
            // Основная причёска и чёлка Гвен
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.46f, 0.56f), position: new(0, 0.12f, -0.02f), color: Rgb(75, 205, 255));
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.48f, 0.44f), position: new(0, -0.02f, -0.10f), color: Rgb(60, 180, 240));
            foreach (float bx in new[] { -0.14f, 0.0f, 0.14f })
                new Entity(parent: head, model: "diamond", scale: new(0.18f, 0.22f, 0.14f), position: new(bx, 0.16f, 0.22f), rotation: new(-15, 0, bx * -25f), color: Rgb(75, 205, 255));
            // Гетерохромные кукольные глазки (лазурный и пурпурный)
            new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(-0.12f, 0.04f, 0.22f), color: Rgb(90, 220, 255), unlit: true);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(0.12f, 0.04f, 0.22f), color: Rgb(255, 110, 195), unlit: true);
            new Entity(parent: head, model: "diamond", scale: new(0.68f, 0.26f, 0.32f), position: new(0, 0.28f, 0.05f), color: Rgb(32, 28, 46));
            new Entity(parent: head, model: "diamond", scale: new(0.24f, 0.28f, 0.14f), position: new(0, 0.32f, 0.18f), color: Color.Gold, unlit: true);
            // Знаменитые огромные бирюзовые спиральные хвостики Гвен
            foreach (int side in new[] { -1, 1 })
            {
                var drill = new Entity(parent: head, position: new(side * 0.38f, 0.08f, -0.10f), rotation: new(0, 0, side * -18f));
                new Entity(parent: drill, model: "diamond", scale: new(0.32f, 0.20f, 0.24f), position: new(0, 0.05f, 0), color: Rgb(215, 55, 110));
                new Entity(parent: drill, model: "diamond", scale: new(0.38f, 1.05f, 0.38f), position: new(0, -0.40f, 0), color: Rgb(75, 205, 255));
                new Entity(parent: drill, model: "diamond", scale: new(0.44f, 0.52f, 0.44f), position: new(0, -0.66f, 0), color: Rgb(60, 180, 240));
                new Entity(parent: drill, model: "diamond", scale: new(0.28f, 0.48f, 0.28f), position: new(0, -0.92f, 0), color: Rgb(185, 240, 255), unlit: true);
            }
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.44f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.44f, 0.25f, 0));

            // Левая рука: Фарфоровая рука и Парящая Игла
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.28f, position: new(0, 0.04f, 0), color: Rgb(32, 28, 46));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.26f, 0.14f), position: new(0, -0.16f, 0.02f), color: Rgb(248, 225, 220));
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.30f, 0.02f), color: Rgb(248, 225, 220));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(248, 225, 220));
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.20f, 0.20f, 0.16f), position: new(0, -0.30f, 0.36f), color: Rgb(32, 28, 46));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.12f, 0.12f, 0.12f), position: new(0, -0.30f, 0.42f), color: Rgb(248, 225, 220));
            // Игла со священной нитью
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.06f, 0.65f, 0.06f), position: new(0, -0.16f, 0.48f), rotation: new(30, 0, 0), color: Color.Gold, unlit: true);

            // Правая рука: The Hallowed Scissors (Священные Ножницы)
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.28f, position: new(0, 0.04f, 0), color: Rgb(32, 28, 46));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.26f, 0.14f), position: new(0, -0.16f, 0.02f), color: Rgb(248, 225, 220));
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.30f, 0.02f), color: Rgb(248, 225, 220));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(248, 225, 220));
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.20f, 0.20f, 0.16f), position: new(0, -0.30f, 0.36f), color: Rgb(32, 28, 46));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.12f, 0.12f, 0.12f), position: new(0, -0.30f, 0.42f), color: Rgb(248, 225, 220));

            var scis = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f), rotation: new(12, 0, 0));
            new Entity(parent: scis, model: "sphere", scaleUniform: 0.32f, position: new(0, 0, 0.06f), color: Color.Gold, unlit: true);
            foreach (float sy in new[] { -0.18f, 0.18f })
                new Entity(parent: scis, model: "diamond", scale: new(0.12f, 0.32f, 0.34f), position: new(0, sy, -0.14f), color: Color.Gold, unlit: true);
            new Entity(parent: scis, model: "diamond", scale: new(0.12f, 0.26f, 1.75f), position: new(0, 0.16f, 0.85f), rotation: new(-14, 0, 0), color: Rgb(110, 225, 255), unlit: true);
            new Entity(parent: scis, model: "diamond", scale: new(0.12f, 0.26f, 1.75f), position: new(0, -0.12f, 0.85f), rotation: new(14, 0, 0), color: Rgb(195, 245, 255), unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 8. ENFORCER (Risk of Rain 1 / Modded)
        // ==========================================
        private static void BuildEnforcer(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.25f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.25f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.44f, 0.28f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.08f, 0.38f, 0.28f), position: new(side * 0.12f, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.26f, 0.30f), position: new(0, -0.42f, 0.08f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.12f, 0.10f, 0.08f), position: new(0, -0.42f, 0.22f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.42f, 0.26f), position: new(0, -0.62f, 0), color: Rgb(30, 36, 48));
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.34f, 0.12f), position: new(0, -0.60f, 0.11f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.30f, 0.18f, 0.46f), position: new(0, -0.80f, 0.08f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.32f, 0.06f, 0.48f), position: new(0, -0.87f, 0.08f), color: Rgb(20, 24, 32));
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.22f, 0.46f), position: new(0, -0.32f, 0), color: Rgb(28, 34, 46));
            new Entity(parent: torso, model: "cube", scale: new(0.76f, 0.64f, 0.48f), position: new(0, 0.08f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.72f, 0.52f, 0.22f), position: new(0, 0.12f, 0.18f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.88f, 0.28f, 0.58f), position: new(0, 0.34f, -0.02f), color: cfg.ArmorColor);
            // Проблесковые полицейские маячки на плечах
            new Entity(parent: torso, model: "sphere", scale: new(0.22f, 0.16f, 0.22f), position: new(-0.38f, 0.52f, 0), color: Color.SkyBlue, unlit: true);
            new Entity(parent: torso, model: "sphere", scale: new(0.22f, 0.16f, 0.22f), position: new(0.38f, 0.52f, 0), color: Color.Red, unlit: true);

            var pack = new Entity(parent: torso, position: new(0, 0.10f, -0.30f));
            new Entity(parent: pack, model: "cube", scale: new(0.56f, 0.58f, 0.26f), color: cfg.ArmorColor);
            new Entity(parent: pack, model: "cube", scale: new(0.38f, 0.40f, 0.16f), position: new(0, 0, -0.12f), color: Rgb(25, 30, 40));

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "cube", scale: new(0.64f, 0.58f, 0.64f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "cube", scale: new(0.66f, 0.16f, 0.66f), position: new(0, 0.28f, 0), color: cfg.BodyColor);
            new Entity(parent: head, model: "cube", scale: new(0.56f, 0.18f, 0.40f), position: new(0, 0.04f, 0.16f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "cube", scale: new(0.58f, 0.05f, 0.42f), position: new(0, 0.04f, 0.17f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "cube", scale: new(0.28f, 0.16f, 0.22f), position: new(0, -0.20f, 0.22f), color: Rgb(30, 36, 48));
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.52f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.52f, 0.25f, 0));

            // Левая рука: Тяжёлый Бастионный Щит
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.58f, 0.52f, 0.58f), position: new(0, 0.08f, 0), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.22f, 0.30f, 0.22f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.20f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.22f, 0.22f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.20f, 0.20f, 0.16f), position: new(0, -0.30f, 0.42f), color: Rgb(30, 36, 48));

            var shield = new Entity(parent: lShoulder, position: new(-0.28f, -0.10f, 0.42f), rotation: new(0, -22f, 0));
            new Entity(parent: shield, model: "cube", scale: new(1.45f, 1.95f, 0.18f), color: cfg.ArmorColor);
            new Entity(parent: shield, model: "cube", scale: new(1.52f, 2.02f, 0.08f), position: new(0, 0, -0.04f), color: cfg.BodyColor);
            new Entity(parent: shield, model: "cube", scale: new(0.95f, 0.32f, 0.20f), position: new(0, 0.44f, 0), color: cfg.VisorColor, unlit: true);
            new Entity(parent: shield, model: "diamond", scale: new(0.58f, 0.80f, 0.22f), position: new(0, -0.24f, 0), color: cfg.VisorColor, unlit: true);
            foreach (float cy in new[] { -0.82f, 0.82f })
                new Entity(parent: shield, model: "cube", scale: new(1.48f, 0.16f, 0.22f), position: new(0, cy, 0), color: Rgb(85, 95, 115));
            var aura = new Entity(parent: shield, model: "cube", scale: new(1.85f, 2.25f, 0.08f), position: new(0, 0, 0.14f), color: Rgb(70, 210, 255, 110), unlit: true, enabled: false);
            parts.Shield = shield;
            parts.ShieldAura = aura;

            // Правая рука: Дробовик Riot Shotgun
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.52f, 0.46f, 0.52f), position: new(0, 0.08f, 0), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.20f, 0.20f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.42f), color: Rgb(30, 36, 48));

            var sg = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: sg, model: "cube", scale: new(0.20f, 0.28f, 1.25f), position: new(0, 0.04f, 0.42f), color: Rgb(30, 36, 46));
            new Entity(parent: sg, model: "cube", scale: new(0.24f, 0.18f, 0.70f), position: new(0, -0.06f, 0.52f), color: cfg.BodyColor);
            new Entity(parent: sg, model: "cube", scale: new(0.16f, 0.10f, 1.10f), position: new(0, 0.18f, 0.42f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: sg, model: "cube", scale: new(0.12f, 0.22f, 0.16f), position: new(0, -0.14f, 0.05f), rotation: new(-20, 0, 0), color: Rgb(25, 30, 38));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 9. SCOUT (Deep Rock Galactic)
        // ==========================================
        private static void BuildScout(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.22f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.22f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.44f, 0.28f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.22f, 0.30f), position: new(0, -0.42f, 0.06f), color: Rgb(55, 65, 82));
                new Entity(parent: hip, model: "cube", scale: new(0.12f, 0.08f, 0.06f), position: new(0, -0.42f, 0.20f), color: Color.Yellow, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.42f, 0.26f), position: new(0, -0.62f, 0), color: Rgb(42, 48, 60));
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.18f, 0.44f), position: new(0, -0.80f, 0.08f), color: Rgb(65, 48, 35));
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.22f, 0.46f), position: new(0, -0.32f, 0), color: Rgb(75, 52, 38));
            new Entity(parent: torso, model: "cube", scale: new(0.24f, 0.16f, 0.48f), position: new(0, -0.32f, 0.02f), color: Color.Gold, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.74f, 0.62f, 0.48f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.48f, 0.22f), position: new(0, 0.10f, 0.18f), color: cfg.ArmorColor);
            // Патронташ со светящимися сигнальными шашками (Flares)
            foreach (float bx in new[] { -0.24f, -0.08f, 0.08f, 0.24f })
                new Entity(parent: torso, model: "cube", scale: new(0.08f, 0.22f, 0.10f), position: new(bx, -0.16f, 0.26f), color: Color.Yellow, unlit: true);

            var pack = new Entity(parent: torso, position: new(0, 0.10f, -0.30f));
            new Entity(parent: pack, model: "cube", scale: new(0.52f, 0.54f, 0.26f), color: cfg.ArmorColor);
            new Entity(parent: pack, model: "sphere", scaleUniform: 0.16f, position: new(0, 0, -0.14f), color: Color.SkyBlue, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.48f, 0.56f), position: new(0, 0.08f, 0), color: Rgb(245, 185, 40));
            new Entity(parent: head, model: "cube", scale: new(0.50f, 0.08f, 0.36f), position: new(0, 0.16f, 0.20f), color: Rgb(55, 65, 82));
            // Мощный налобный фонарь шахтёра
            new Entity(parent: head, model: "sphere", scale: new(0.24f, 0.24f, 0.22f), position: new(0, 0.24f, 0.28f), color: Color.White, unlit: true);
            new Entity(parent: head, model: "cube", scale: new(0.50f, 0.14f, 0.32f), position: new(0, 0.02f, 0.16f), color: Rgb(60, 225, 255), unlit: true);

            // Пышная рыжая борода Скаута с золотыми кольцами
            var beard = new Entity(parent: head, position: new(0, -0.22f, 0.18f));
            new Entity(parent: beard, model: "diamond", scale: new(0.66f, 0.76f, 0.46f), color: Rgb(225, 95, 25));
            new Entity(parent: beard, model: "diamond", scale: new(0.42f, 0.62f, 0.38f), position: new(-0.22f, -0.08f, -0.04f), color: Rgb(205, 80, 20));
            new Entity(parent: beard, model: "diamond", scale: new(0.42f, 0.62f, 0.38f), position: new(0.22f, -0.08f, -0.04f), color: Rgb(205, 80, 20));
            new Entity(parent: beard, model: "cube", scale: new(0.22f, 0.08f, 0.22f), position: new(0, -0.26f, 0), color: Color.Gold, unlit: true);
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));

            // Левая рука: Крюк-кошка (Grappling Hook)
            new Entity(parent: lShoulder, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.20f, 0.20f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.40f), color: Rgb(75, 52, 38));

            var grap = new Entity(parent: lShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: grap, model: "cube", scale: new(0.20f, 0.24f, 0.72f), position: new(0, 0.04f, 0.22f), color: Rgb(235, 175, 40));
            new Entity(parent: grap, model: "sphere", scale: new(0.26f, 0.26f, 0.24f), position: new(0, 0.04f, 0.06f), color: Rgb(65, 75, 90));
            new Entity(parent: grap, model: "diamond", scale: new(0.42f, 0.18f, 0.42f), position: new(0, 0.04f, 0.60f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: grap, model: "diamond", scale: new(0.18f, 0.42f, 0.42f), position: new(0, 0.04f, 0.60f), color: Color.SkyBlue, unlit: true);

            // Правая рука: Штурмовая винтовка Deepcore GK2
            new Entity(parent: rShoulder, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.20f, 0.20f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.40f), color: Rgb(75, 52, 38));

            var gk2 = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: gk2, model: "cube", scale: new(0.18f, 0.26f, 1.15f), position: new(0, 0.04f, 0.38f), color: Rgb(45, 52, 68));
            new Entity(parent: gk2, model: "cube", scale: new(0.14f, 0.32f, 0.22f), position: new(0, -0.18f, 0.30f), color: Rgb(245, 185, 40));
            new Entity(parent: gk2, model: "cube", scale: new(0.20f, 0.12f, 0.95f), position: new(0, 0.16f, 0.40f), color: Rgb(35, 130, 235), unlit: true);
            new Entity(parent: gk2, model: "cube", scale: new(0.12f, 0.22f, 0.14f), position: new(0, -0.14f, 0.04f), rotation: new(-20, 0, 0), color: Rgb(35, 40, 52));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 10. M4A1 (Girls' Frontline)
        // ==========================================
        private static void BuildM4A1(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.44f, 0.20f), position: new(0, -0.20f, 0), color: Rgb(35, 38, 42));
                new Entity(parent: hip, model: "cube", scale: new(0.16f, 0.42f, 0.18f), position: new(0, -0.62f, 0), color: Rgb(35, 38, 42));
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.18f, 0.38f), position: new(0, -0.80f, 0.06f), color: Rgb(48, 52, 58));
                new Entity(parent: hip, model: "cube", scale: new(0.04f, 0.12f, 0.04f), position: new(0, -0.74f, 0.20f), color: cfg.VisorColor, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "diamond", scale: new(0.98f, 0.72f, 0.92f), position: new(0, -0.38f, 0), color: Rgb(42, 46, 50));
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.54f, 0.38f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.66f, 0.22f, 0.48f), position: new(0, 0.32f, 0), color: Rgb(65, 110, 82));
            new Entity(parent: torso, model: "cube", scale: new(0.50f, 0.42f, 0.18f), position: new(0, 0.08f, 0.16f), color: cfg.ArmorColor);

            // Тактический рюкзак с пушкой Jupiter Cannon на спине
            var pack = new Entity(parent: torso, position: new(0, 0.10f, -0.28f));
            new Entity(parent: pack, model: "cube", scale: new(0.48f, 0.54f, 0.24f), color: cfg.ArmorColor);
            var jup = new Entity(parent: pack, position: new(0.28f, 0.32f, -0.14f), rotation: new(-18, 0, 0));
            new Entity(parent: jup, model: "cube", scale: new(0.26f, 1.15f, 0.30f), position: new(0, 0.28f, 0), color: Rgb(48, 54, 62));
            new Entity(parent: jup, model: "cube", scale: new(0.14f, 1.05f, 0.34f), position: new(0, 0.30f, 0), color: cfg.VisorColor, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 222, 205));
            new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(-0.12f, 0.03f, 0.22f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(0.12f, 0.03f, 0.22f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.42f, 0.56f), position: new(0, 0.14f, 0), color: Rgb(155, 135, 115));
            new Entity(parent: head, model: "diamond", scale: new(0.14f, 0.48f, 0.16f), position: new(-0.14f, -0.05f, 0.24f), rotation: new(0, 0, 12f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "diamond", scale: new(0.26f, 0.95f, 0.26f), position: new(0.26f, -0.24f, -0.16f), rotation: new(18, 0, 0), color: Rgb(145, 125, 105));
            // Тактическая гарнитура на ушах
            foreach (int side in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "cube", scale: new(0.12f, 0.26f, 0.24f), position: new(side * 0.28f, 0.04f, 0.02f), color: Rgb(40, 44, 52));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.09f, position: new(side * 0.33f, 0.04f, 0.02f), color: cfg.VisorColor, unlit: true);
            }
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.46f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.46f, 0.25f, 0));

            // Левая рука: Зелёная тактическая повязка AR Team и перчатка
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.10f, 0.18f), position: new(0, -0.12f, 0.02f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.15f, position: new(0, -0.30f, 0.02f), color: Rgb(35, 38, 42));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(35, 38, 42));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: Rgb(35, 38, 42));

            // Правая рука: Карабин M4A1
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.15f, position: new(0, -0.30f, 0.02f), color: Rgb(35, 38, 42));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(35, 38, 42));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: Rgb(35, 38, 42));

            var rifle = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: rifle, model: "cube", scale: new(0.14f, 0.24f, 1.25f), position: new(0, 0.04f, 0.40f), color: Rgb(32, 35, 40));
            new Entity(parent: rifle, model: "cube", scale: new(0.10f, 0.32f, 0.20f), position: new(0, -0.18f, 0.34f), color: Rgb(50, 56, 52));
            new Entity(parent: rifle, model: "cube", scale: new(0.16f, 0.15f, 0.34f), position: new(0, 0.18f, 0.32f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: rifle, model: "cube", scale: new(0.10f, 0.22f, 0.14f), position: new(0, -0.14f, 0.06f), rotation: new(-20, 0, 0), color: Rgb(35, 38, 42));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 11. MEDIC (Team Fortress 2)
        // ==========================================
        private static void BuildMedic(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.44f, 0.24f), position: new(0, -0.20f, 0), color: Rgb(45, 48, 55));
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.42f, 0.24f), position: new(0, -0.62f, 0), color: Rgb(30, 32, 36));
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.18f, 0.40f), position: new(0, -0.80f, 0.06f), color: Rgb(25, 26, 30));
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.06f, 0.06f), position: new(0, -0.74f, 0.18f), color: Color.LightGray);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            // Белый медицинский халат с красным галстуком и фалдами
            new Entity(parent: torso, model: "cube", scale: new(0.78f, 0.72f, 0.48f), position: new(0, -0.38f, 0), color: Rgb(235, 238, 245));
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.62f, 0.44f), position: new(0, 0.06f, 0), color: Rgb(235, 238, 245));
            new Entity(parent: torso, model: "cube", scale: new(0.32f, 0.48f, 0.16f), position: new(0, 0.08f, 0.18f), color: Rgb(45, 48, 55));
            new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.44f, 0.46f), position: new(0, 0.10f, 0.03f), color: Color.Red, unlit: true);

            // Знаменитый лечебный ранец Медика (Über-Pack)
            var pack = new Entity(parent: torso, position: new(0, 0.10f, -0.30f));
            new Entity(parent: pack, model: "cube", scale: new(0.38f, 0.72f, 0.38f), position: new(0.16f, 0.08f, 0), color: Rgb(220, 45, 45), unlit: true);
            new Entity(parent: pack, model: "cube", scale: new(0.26f, 0.65f, 0.30f), position: new(-0.16f, 0.06f, 0), color: Rgb(255, 195, 55), unlit: true);
            new Entity(parent: pack, model: "sphere", scaleUniform: 0.18f, position: new(0.16f, 0.34f, -0.16f), color: Color.White, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.52f, 0.48f), color: Rgb(245, 205, 180));
            new Entity(parent: head, model: "cube", scale: new(0.52f, 0.18f, 0.52f), position: new(0, 0.22f, 0), color: Rgb(35, 30, 32));
            new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.24f, 0.22f), position: new(0.08f, 0.32f, 0.20f), rotation: new(0, 0, -20f), color: Rgb(35, 30, 32));
            // Круглые очки Медика
            new Entity(parent: head, model: "cube", scale: new(0.46f, 0.04f, 0.10f), position: new(0, 0.05f, 0.22f), color: Rgb(40, 40, 45));
            new Entity(parent: head, model: "sphere", scale: new(0.18f, 0.16f, 0.10f), position: new(-0.12f, 0.04f, 0.24f), color: Color.White, unlit: true);
            new Entity(parent: head, model: "sphere", scale: new(0.18f, 0.16f, 0.10f), position: new(0.12f, 0.04f, 0.24f), color: Color.White, unlit: true);
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));

            // Левая рука: Medi Gun
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.16f, 0.02f), color: Rgb(235, 238, 245));
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: Rgb(205, 38, 45));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(205, 38, 45));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.14f), position: new(0, -0.30f, 0.40f), color: Rgb(205, 38, 45));

            var medigun = new Entity(parent: lShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: medigun, model: "cube", scale: new(0.24f, 0.26f, 1.05f), position: new(0, 0.04f, 0.32f), color: Rgb(70, 75, 85));
            new Entity(parent: medigun, model: "cube", scale: new(0.28f, 0.28f, 0.28f), position: new(0, 0.04f, 0.68f), color: Rgb(185, 195, 210));
            new Entity(parent: medigun, model: "sphere", scaleUniform: 0.34f, position: new(0, 0.04f, 0.86f), color: Color.Red, unlit: true);

            // Правая рука: Syringe Gun
            new Entity(parent: rShoulder, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.16f, 0.02f), color: Rgb(235, 238, 245));
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: Rgb(205, 38, 45));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(205, 38, 45));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.14f), position: new(0, -0.30f, 0.40f), color: Rgb(205, 38, 45));

            var syrGun = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: syrGun, model: "cube", scale: new(0.20f, 0.24f, 0.90f), position: new(0, 0.04f, 0.30f), color: Rgb(60, 65, 75));
            new Entity(parent: syrGun, model: "sphere", scale: new(0.32f, 0.32f, 0.54f), position: new(0, 0.20f, 0.30f), color: Color.Red, unlit: true);
            new Entity(parent: syrGun, model: "diamond", scale: new(0.08f, 0.20f, 0.68f), position: new(0, -0.12f, 0.58f), color: Rgb(220, 230, 245));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 12. AIGIS (Persona 3)
        // ==========================================
        private static void BuildAigis(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.44f, 0.20f), position: new(0, -0.20f, 0), color: Color.White);
                new Entity(parent: hip, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.42f, 0.02f), color: Color.Gold, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.16f, 0.42f, 0.18f), position: new(0, -0.62f, 0), color: Color.White);
                new Entity(parent: hip, model: "diamond", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.80f, 0.06f), color: Color.Gold, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.50f, 0.18f, 0.36f), position: new(0, -0.32f, 0), color: Color.Gold, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.58f, 0.56f, 0.38f), position: new(0, 0.06f, 0), color: Color.White);
            // Красный бант Айгис
            new Entity(parent: torso, model: "diamond", scale: new(0.58f, 0.24f, 0.28f), position: new(0, 0.34f, 0.20f), color: Rgb(235, 45, 60), unlit: true);
            // Золотые пластины Orgia Mode на спине
            foreach (int side in new[] { -1, 1 })
                new Entity(parent: torso, model: "diamond", scale: new(0.88f, 0.26f, 0.18f), position: new(side * 0.48f, 0.14f, -0.22f), rotation: new(0, 0, side * 18f), color: Rgb(235, 195, 55));

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 228, 212));
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.45f, 0.56f), position: new(0, 0.10f, 0), color: Rgb(245, 205, 65));
            new Entity(parent: head, model: "cube", scale: new(0.58f, 0.08f, 0.56f), position: new(0, 0.16f, 0), color: Rgb(55, 65, 85));
            new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(-0.12f, 0.02f, 0.22f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(0.12f, 0.02f, 0.22f), color: Color.SkyBlue, unlit: true);
            // Механические датчики-наушники Айгис
            foreach (int side in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.38f, 0.38f), position: new(side * 0.30f, 0.02f, 0), color: Rgb(225, 230, 240));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.14f, position: new(side * 0.35f, 0.02f, 0), color: Color.SkyBlue, unlit: true);
            }
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.46f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.46f, 0.25f, 0));

            // Левая рука: Пушка Pandora
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.26f, position: new(0, 0.04f, 0), color: Color.Gold, unlit: true);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.26f, 0.16f), position: new(0, -0.16f, 0.02f), color: Color.White);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: Color.Gold, unlit: true);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: Color.White);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: Color.Gold, unlit: true);

            var cannon = new Entity(parent: lShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: cannon, model: "cube", scale: new(0.30f, 0.30f, 0.95f), position: new(0, 0.04f, 0.30f), color: Rgb(235, 195, 55));
            new Entity(parent: cannon, model: "sphere", scaleUniform: 0.28f, position: new(0, 0.04f, 0.78f), color: Color.Orange, unlit: true);

            // Правая рука: Пулемётные пальцы Finger Vulcan
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.26f, position: new(0, 0.04f, 0), color: Color.Gold, unlit: true);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.26f, 0.16f), position: new(0, -0.16f, 0.02f), color: Color.White);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: Color.Gold, unlit: true);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: Color.White);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: Color.Gold, unlit: true);

            var barrel = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: barrel, model: "cube", scale: new(0.26f, 0.20f, 0.70f), position: new(0, 0.04f, 0.25f), color: Color.SkyBlue, unlit: true);
            foreach (float fx in new[] { -0.09f, -0.04f, 0.0f, 0.04f, 0.09f })
                new Entity(parent: barrel, model: "cube", scale: new(0.04f, 0.06f, 0.42f), position: new(fx, 0.04f, 0.62f), color: Rgb(235, 195, 55));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 13. HAKUREI REIMU (Touhou Project)
        // ==========================================
        private static void BuildReimu(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.44f, 0.20f), position: new(0, -0.20f, 0), color: Rgb(248, 225, 215));
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.20f), position: new(0, -0.62f, 0), color: Color.White);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.16f, 0.36f), position: new(0, -0.80f, 0.06f), color: cfg.BodyColor);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            // Красно-белое традиционное жреческое кимоно Рейму
            new Entity(parent: torso, model: "diamond", scale: new(1.14f, 0.76f, 1.14f), position: new(0, -0.38f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(1.18f, 0.20f, 1.18f), position: new(0, -0.64f, 0), color: Color.White, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.52f, 0.52f, 0.38f), position: new(0, 0.04f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.14f, 0.42f), position: new(0, -0.18f, 0), color: Color.White);
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.28f, 0.48f), position: new(0, 0.22f, 0), color: Color.White);
            new Entity(parent: torso, model: "diamond", scale: new(0.40f, 0.20f, 0.26f), position: new(0, 0.28f, 0.22f), color: Color.Yellow, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 225, 215));
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.44f, 0.56f), position: new(0, 0.12f, 0), color: Rgb(38, 28, 36));
            // Боковые пряди в белой бумажной обёртке (сараси)
            foreach (int side in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "cube", scale: new(0.14f, 0.40f, 0.14f), position: new(side * 0.28f, -0.10f, 0.08f), color: Color.White);
                new Entity(parent: head, model: "diamond", scale: new(0.16f, 0.26f, 0.16f), position: new(side * 0.28f, -0.34f, 0.08f), color: Rgb(38, 28, 36));
            }
            // Огромный фирменный красный бант Рейму
            var bowKnot = new Entity(parent: head, position: new(0, 0.20f, -0.26f), rotation: new(15, 0, 0));
            new Entity(parent: bowKnot, model: "diamond", scale: new(1.15f, 0.50f, 0.24f), color: cfg.BodyColor, unlit: true);
            new Entity(parent: bowKnot, model: "diamond", scale: new(0.45f, 0.70f, 0.20f), position: new(0, -0.30f, 0), color: cfg.BodyColor, unlit: true);
            parts.Torso = torso;

            // Вращающиеся сферы Инь-Ян Хакурей
            var halo = new Entity(parent: torso, position: new(0, 0.35f, -0.36f));
            foreach (var (side, colOrb) in new[] { (-1, cfg.BodyColor), (1, Color.White) })
            {
                var orb = new Entity(parent: halo, model: "sphere", scaleUniform: 0.42f, position: new(side * 0.72f, 0, 0), color: colOrb, unlit: true);
                new Entity(parent: orb, model: "diamond", scale: new(1.50f, 0.20f, 1.50f), color: Color.Gold, unlit: true);
            }
            parts.Halo = halo;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));

            // Левая рука: Белый пышный отдельный рукав и Офуда
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.44f, 0.52f, 0.44f), position: new(0, -0.10f, 0.08f), color: Color.White);
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.36f, 0.68f, 0.46f), position: new(0, -0.32f, 0.12f), color: Color.White);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.24f, 0.14f), position: new(0, -0.16f, 0.02f), color: Rgb(248, 225, 215));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.36f), position: new(0, -0.28f, 0.20f), color: Rgb(248, 225, 215));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.12f, 0.12f, 0.12f), position: new(0, -0.28f, 0.38f), color: Rgb(248, 225, 215));
            // Карты Офуда
            foreach (float oy in new[] { -0.10f, 0.0f, 0.10f })
            {
                new Entity(parent: lShoulder, model: "cube", scale: new(0.24f, 0.03f, 0.46f), position: new(0, -0.26f + oy * 0.5f, 0.52f), rotation: new(0, oy * 120f, 0), color: Color.White, unlit: true);
                new Entity(parent: lShoulder, model: "cube", scale: new(0.10f, 0.04f, 0.38f), position: new(0, -0.26f + oy * 0.5f, 0.52f), rotation: new(0, oy * 120f, 0), color: Rgb(245, 50, 65), unlit: true);
            }

            // Правая рука: Белый рукав и Жезл Гохэй с лентами сидэ
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.44f, 0.52f, 0.44f), position: new(0, -0.10f, 0.08f), color: Color.White);
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.36f, 0.68f, 0.46f), position: new(0, -0.32f, 0.12f), color: Color.White);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.24f, 0.14f), position: new(0, -0.16f, 0.02f), color: Rgb(248, 225, 215));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.36f), position: new(0, -0.28f, 0.20f), color: Rgb(248, 225, 215));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.12f, 0.12f, 0.12f), position: new(0, -0.28f, 0.38f), color: Rgb(248, 225, 215));

            var gohei = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f), rotation: new(20, 0, 0));
            new Entity(parent: gohei, model: "cube", scale: new(0.08f, 0.08f, 1.45f), position: new(0, 0, 0.45f), color: Rgb(140, 85, 45));
            new Entity(parent: gohei, model: "diamond", scale: new(0.46f, 0.58f, 0.46f), position: new(0, 0, 1.15f), color: Color.White, unlit: true);
            new Entity(parent: gohei, model: "diamond", scale: new(0.36f, 0.44f, 0.36f), position: new(0, -0.22f, 1.05f), color: cfg.BodyColor, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 14. MINDUSTRY GAMMA CORE SHIP
        // ==========================================
        private static void BuildMindustry(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var leftHip = new Entity(parent: root, position: new(-0.36f, 0.85f, -0.28f));
            var rightHip = new Entity(parent: root, position: new(0.36f, 0.85f, -0.28f));
            foreach (var hip in new[] { leftHip, rightHip })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.30f, 0.28f, 0.56f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.34f, 0.12f, 0.38f), position: new(0, 0.14f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.32f, 0.32f, 0.68f), position: new(0, 0, -0.38f), color: Color.SkyBlue, unlit: true);
                new Entity(parent: hip, model: "diamond", scale: new(0.20f, 0.20f, 0.90f), position: new(0, 0, -0.54f), color: Color.White, unlit: true);
            }
            parts.LeftLeg = leftHip;
            parts.RightLeg = rightHip;

            var mTorso = new Entity(parent: root, position: new(0, 1.35f, 0));
            new Entity(parent: mTorso, model: "diamond", scale: new(1.45f, 0.48f, 1.88f), color: cfg.BodyColor);
            new Entity(parent: mTorso, model: "diamond", scale: new(1.02f, 0.52f, 2.05f), position: new(0, 0, 0.10f), color: cfg.ArmorColor);
            new Entity(parent: mTorso, model: "cube", scale: new(0.78f, 0.42f, 1.15f), position: new(0, 0.12f, 0), color: cfg.ArmorColor);
            new Entity(parent: mTorso, model: "cube", scale: new(0.56f, 0.18f, 1.45f), position: new(0, -0.18f, 0), color: Rgb(40, 45, 58));
            // Реактор ядра и мостик
            new Entity(parent: mTorso, model: "diamond", scale: new(0.55f, 0.42f, 0.85f), position: new(0, 0.30f, 0.16f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: mTorso, model: "sphere", scaleUniform: 0.32f, position: new(0, 0.34f, 0.20f), color: Color.White, unlit: true);
            foreach (float sx in new[] { -0.24f, 0.24f })
            {
                new Entity(parent: mTorso, model: "diamond", scale: new(0.16f, 0.18f, 0.72f), position: new(sx, -0.02f, 0.96f), color: cfg.BodyColor);
                new Entity(parent: mTorso, model: "sphere", scaleUniform: 0.14f, position: new(sx, -0.02f, 1.24f), color: Color.Yellow, unlit: true);
            }

            // Стреловидные крылья с солнечными батареями и пушками
            foreach (int side in new[] { -1, 1 })
            {
                var wing = new Entity(parent: mTorso, model: "diamond", scale: new(1.38f, 0.20f, 1.15f),
                    position: new(side * 0.76f, -0.02f, -0.08f), rotation: new(0, 0, side * -10f), color: cfg.BodyColor);
                new Entity(parent: wing, model: "cube", scale: new(0.64f, 0.16f, 0.80f), position: new(side * 0.14f, 0.05f, 0.05f), color: cfg.ArmorColor);
                new Entity(parent: wing, model: "cube", scale: new(0.16f, 0.18f, 1.02f), position: new(side * 0.30f, 0.02f, 0.38f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: wing, model: "diamond", scale: new(0.24f, 0.46f, 0.52f), position: new(side * 0.50f, 0.12f, -0.18f), rotation: new(0, 0, side * 22f), color: cfg.ArmorColor);
            }

            var mHalo = new Entity(parent: mTorso, position: new(0, 0.56f, -0.18f));
            new Entity(parent: mHalo, model: "diamond", scale: new(1.15f, 1.15f, 0.08f), rotation: new(90, 0, 0), color: Color.Yellow, unlit: true);
            new Entity(parent: mHalo, model: "diamond", scale: new(0.85f, 0.85f, 0.08f), rotation: new(90, 45, 0), color: Color.SkyBlue, unlit: true);
            parts.Halo = mHalo;
            parts.Torso = mTorso;

            var mLeftSh = new Entity(parent: mTorso, position: new(-0.70f, 0.06f, 0.38f));
            var mRightSh = new Entity(parent: mTorso, position: new(0.70f, 0.06f, 0.38f));
            foreach (var sh in new[] { mLeftSh, mRightSh })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.36f, 0.28f, 0.56f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.16f, 0.98f), position: new(-0.10f, 0, 0.42f), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.16f, 0.98f), position: new(0.10f, 0, 0.42f), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.30f, position: new(0, 0, 0.88f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: sh, model: "diamond", scale: new(0.20f, 0.20f, 0.30f), position: new(0, 0, 0.96f), color: Color.White, unlit: true);
            }
            parts.LeftArm = mLeftSh;
            parts.RightArm = mRightSh;
        }

        // ==========================================
        // 15. SLARK (Dota 2)
        // ==========================================
        private static void BuildSlark(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color deepTeal = Rgb(32, 105, 132);
            Color reefGlow = Rgb(95, 255, 215);
            Color bellyTone = Rgb(155, 225, 215);
            Color leatherDark = Rgb(24, 34, 52);

            var lHip = new Entity(parent: root, position: new(-0.26f, 0.82f, 0));
            var rHip = new Entity(parent: root, position: new(0.26f, 0.82f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                // Мощное мускулистое бедро с бронепластиной
                new Entity(parent: hip, model: "sphere", scale: new(0.32f, 0.46f, 0.34f), position: new(0, -0.18f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.34f, 0.28f, 0.36f), position: new(side * 0.08f, -0.18f, 0.04f), color: cfg.ArmorColor);
                // Наколенник с шипом
                new Entity(parent: hip, model: "diamond", scale: new(0.28f, 0.26f, 0.32f), position: new(0, -0.40f, 0.08f), color: leatherDark);
                new Entity(parent: hip, model: "diamond", scale: new(0.12f, 0.20f, 0.24f), position: new(0, -0.38f, 0.18f), color: reefGlow, unlit: true);
                // Голень и плавник на икре
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.38f, 0.24f), position: new(0, -0.58f, 0.02f), color: deepTeal);
                new Entity(parent: hip, model: "diamond", scale: new(0.08f, 0.28f, 0.24f), position: new(0, -0.56f, -0.14f), rotation: new(-20, 0, 0), color: reefGlow, unlit: true);
                // Когтистая перепончатая лапа
                new Entity(parent: hip, model: "diamond", scale: new(0.34f, 0.16f, 0.50f), position: new(0, -0.76f, 0.14f), color: deepTeal);
                foreach (float cx in new[] { -0.09f, 0f, 0.09f })
                    new Entity(parent: hip, model: "diamond", scale: new(0.06f, 0.08f, 0.18f), position: new(cx, -0.79f, 0.36f), color: Color.White);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.24f, 0), rotation: new(18f, 0, 0));
            // Широкая атлетичная спина и грудная клетка
            new Entity(parent: torso, model: "sphere", scale: new(0.86f, 0.78f, 0.62f), position: new(0, 0.10f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.34f, 0.46f), position: new(0, -0.24f, 0), color: leatherDark);
            new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.24f, 0.52f), position: new(0, -0.24f, 0.04f), color: Color.Gold, unlit: true);
            // Рельефный пресс и светящиеся жабры по бокам
            new Entity(parent: torso, model: "sphere", scale: new(0.62f, 0.64f, 0.28f), position: new(0, 0.04f, 0.22f), color: bellyTone);
            foreach (int side in new[] { -1, 1 })
            {
                foreach (float gy in new[] { -0.04f, 0.08f, 0.20f })
                    new Entity(parent: torso, model: "cube", scale: new(0.06f, 0.04f, 0.18f), position: new(side * 0.38f, gy, 0.12f), rotation: new(0, 0, side * 18f), color: reefGlow, unlit: true);
            }
            // Портупея и броневоротник Тёмного Рифа
            new Entity(parent: torso, model: "diamond", scale: new(0.96f, 0.42f, 0.66f), position: new(0, 0.30f, -0.02f), color: cfg.ArmorColor);
            // Мощный гребень из 6 биолюминесцентных плавников и хвост
            for (int i = 0; i < 6; i++)
                new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.54f - i * 0.05f, 0.30f), position: new(0, 0.38f - i * 0.14f, -0.32f), rotation: new(-36, 0, 0), color: reefGlow, unlit: true);
            var tail = new Entity(parent: torso, position: new(0, -0.34f, -0.24f), rotation: new(-24f, 0, 0));
            new Entity(parent: tail, model: "diamond", scale: new(0.30f, 0.26f, 0.75f), position: new(0, 0, -0.30f), color: cfg.BodyColor);
            new Entity(parent: tail, model: "diamond", scale: new(0.16f, 0.42f, 0.48f), position: new(0, 0.06f, -0.68f), color: reefGlow, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0.16f), rotation: new(-16f, 0, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.62f, 0.54f, 0.72f), color: cfg.BodyColor);
            // Глубокий зубчатый капюшон и нижняя челюсть с клыками
            new Entity(parent: head, model: "diamond", scale: new(0.76f, 0.58f, 0.78f), position: new(0, 0.14f, -0.04f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "cube", scale: new(0.46f, 0.16f, 0.48f), position: new(0, -0.18f, 0.14f), color: bellyTone);
            foreach (float tx in new[] { -0.14f, -0.05f, 0.05f, 0.14f })
                new Entity(parent: head, model: "diamond", scale: new(0.06f, 0.14f, 0.06f), position: new(tx, -0.10f, 0.34f), color: Color.White);
            foreach (float ex in new[] { -0.17f, 0.17f })
            {
                new Entity(parent: head, model: "sphere", scaleUniform: 0.16f, position: new(ex, 0.06f, 0.30f), color: reefGlow, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.07f, position: new(ex, 0.06f, 0.36f), color: Color.White, unlit: true);
            }
            // Боковые жаберные плавники на голове
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.42f, 0.28f), position: new(s * 0.36f, 0.08f, -0.02f), rotation: new(0, 0, s * -32f), color: reefGlow, unlit: true);
                new Entity(parent: head, model: "diamond", scale: new(0.14f, 0.32f, 0.22f), position: new(s * 0.32f, -0.08f, -0.06f), rotation: new(0, 0, s * -48f), color: cfg.BodyColor);
            }
            parts.Torso = torso;

            // Рельефные раскачанные руки с наплечниками, кандалами и плавниками
            var lShoulder = new Entity(parent: torso, position: new(-0.54f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.54f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "diamond", scale: new(0.50f, 0.42f, 0.50f), position: new(0, 0.08f, 0), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "diamond", scale: new(0.20f, 0.36f, 0.22f), position: new(side * 0.18f, 0.24f, 0), rotation: new(0, 0, side * -24f), color: reefGlow, unlit: true);
                // Крупный бицепс и предплечье
                new Entity(parent: sh, model: "sphere", scale: new(0.28f, 0.36f, 0.28f), position: new(0, -0.16f, 0.04f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "sphere", scale: new(0.26f, 0.26f, 0.44f), position: new(0, -0.30f, 0.22f), color: deepTeal);
                // Тюремные кандалы с обрывками цепи и плавники на локтях
                new Entity(parent: sh, model: "cube", scale: new(0.28f, 0.28f, 0.14f), position: new(0, -0.30f, 0.34f), color: Rgb(65, 75, 90));
                new Entity(parent: sh, model: "diamond", scale: new(0.08f, 0.26f, 0.34f), position: new(side * 0.14f, -0.30f, 0.18f), color: reefGlow, unlit: true);
            }
            // Зазубренный клинок-заточка из челюсти акулы в правой руке
            var dagger = new Entity(parent: rShoulder, position: new(0, -0.30f, 0.46f), rotation: new(10f, 0, 0));
            new Entity(parent: dagger, model: "cube", scale: new(0.12f, 0.14f, 0.34f), color: Rgb(75, 52, 38));
            new Entity(parent: dagger, model: "diamond", scale: new(0.32f, 0.12f, 0.18f), position: new(0, 0, 0.16f), color: Color.Gold, unlit: true);
            new Entity(parent: dagger, model: "diamond", scale: new(0.18f, 0.34f, 1.15f), position: new(0, 0, 0.66f), color: reefGlow, unlit: true);
            foreach (float tz in new[] { 0.38f, 0.58f, 0.78f, 0.96f })
                new Entity(parent: dagger, model: "diamond", scale: new(0.10f, 0.18f, 0.14f), position: new(0, 0.16f, tz), rotation: new(-25, 0, 0), color: Color.White, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 16. TIDEHUNTER (Dota 2)
        // ==========================================
        private static void BuildTidehunter(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color shellDark = Rgb(34, 86, 68);
            Color bellyLight = Rgb(182, 222, 168);
            Color abyssGlow = Rgb(95, 255, 205);
            Color ironDark = Rgb(56, 66, 80);

            var lHip = new Entity(parent: root, position: new(-0.38f, 0.88f, 0));
            var rHip = new Entity(parent: root, position: new(0.38f, 0.88f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                // Колоссальные слоновьи ноги Левиафана
                new Entity(parent: hip, model: "sphere", scale: new(0.46f, 0.52f, 0.48f), position: new(0, -0.22f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.40f, 0.44f, 0.42f), position: new(0, -0.58f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.44f, 0.24f, 0.46f), position: new(0, -0.42f, 0.08f), color: shellDark);
                new Entity(parent: hip, model: "cube", scale: new(0.46f, 0.22f, 0.58f), position: new(0, -0.80f, 0.10f), color: shellDark);
                foreach (float cx in new[] { -0.14f, 0f, 0.14f })
                    new Entity(parent: hip, model: "diamond", scale: new(0.10f, 0.12f, 0.20f), position: new(cx, -0.84f, 0.40f), color: bellyLight);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.38f, 0));
            // Огромный мускулистый корпус с сегментированным брюхом
            new Entity(parent: torso, model: "sphere", scale: new(1.52f, 1.28f, 1.18f), color: cfg.BodyColor);
            new Entity(parent: torso, model: "sphere", scale: new(1.18f, 1.02f, 0.54f), position: new(0, -0.04f, 0.40f), color: bellyLight);
            foreach (float by in new[] { -0.28f, -0.06f, 0.16f })
                new Entity(parent: torso, model: "cube", scale: new(0.96f, 0.05f, 0.36f), position: new(0, by, 0.48f), color: cfg.BodyColor);

            // Массивный бронированный Панцирь Кракена с шипами и наростами
            var shell = new Entity(parent: torso, position: new(0, 0.10f, -0.34f));
            new Entity(parent: shell, model: "sphere", scale: new(1.48f, 1.32f, 0.82f), color: shellDark);
            new Entity(parent: shell, model: "diamond", scale: new(1.62f, 1.42f, 0.44f), position: new(0, 0, -0.10f), color: Rgb(28, 72, 58));
            foreach (var (sx, sy, sz) in new[] { (0f, 0.42f, -0.36f), (0f, 0.04f, -0.42f), (0f, -0.34f, -0.34f), (-0.48f, 0.18f, -0.28f), (0.48f, 0.18f, -0.28f), (-0.42f, -0.22f, -0.26f), (0.42f, -0.22f, -0.26f) })
                new Entity(parent: shell, model: "diamond", scale: new(0.28f, 0.48f, 0.46f), position: new(sx, sy, sz), rotation: new(-35, 0, sx * -30f), color: abyssGlow, unlit: true);

            // Голова Левиафана с массивной челюстью, клыками и светящимся рогом-приманкой
            var head = new Entity(parent: torso, position: new(0, 0.68f, 0.30f));
            new Entity(parent: head, model: "sphere", scale: new(0.94f, 0.70f, 0.88f), color: cfg.BodyColor);
            new Entity(parent: head, model: "diamond", scale: new(0.86f, 0.34f, 0.76f), position: new(0, 0.24f, -0.04f), color: shellDark);
            new Entity(parent: head, model: "cube", scale: new(0.74f, 0.26f, 0.62f), position: new(0, -0.22f, 0.22f), color: bellyLight);
            foreach (float tx in new[] { -0.26f, 0.26f })
                new Entity(parent: head, model: "diamond", scale: new(0.12f, 0.28f, 0.12f), position: new(tx, -0.06f, 0.46f), color: Color.White);
            new Entity(parent: head, model: "diamond", scale: new(0.22f, 0.52f, 0.22f), position: new(0, 0.38f, 0.28f), rotation: new(28, 0, 0), color: abyssGlow, unlit: true);
            foreach (float ex in new[] { -0.28f, 0.28f })
            {
                new Entity(parent: head, model: "sphere", scaleUniform: 0.18f, position: new(ex, 0.10f, 0.36f), color: Color.Yellow, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.09f, position: new(ex, 0.10f, 0.42f), color: Color.White, unlit: true);
            }
            parts.Torso = torso;

            // Мощные раскачанные руки-клешни с морскими браслетами и цепями
            var lShoulder = new Entity(parent: torso, position: new(-0.82f, 0.28f, 0.04f));
            var rShoulder = new Entity(parent: torso, position: new(0.82f, 0.28f, 0.04f));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "sphere", scale: new(0.54f, 0.50f, 0.54f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "diamond", scale: new(0.60f, 0.36f, 0.58f), position: new(side * 0.10f, 0.12f, 0), color: shellDark);
                new Entity(parent: sh, model: "diamond", scale: new(0.22f, 0.38f, 0.22f), position: new(side * 0.26f, 0.30f, 0), rotation: new(0, 0, side * -28f), color: abyssGlow, unlit: true);
                new Entity(parent: sh, model: "sphere", scale: new(0.42f, 0.48f, 0.42f), position: new(0, -0.24f, 0.06f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.42f, 0.38f, 0.52f), position: new(0, -0.40f, 0.28f), color: shellDark);
                new Entity(parent: sh, model: "cube", scale: new(0.46f, 0.16f, 0.22f), position: new(0, -0.40f, 0.36f), color: ironDark);
            }

            // Колоссальный Адмиралтейский Якорь Бездны в правой руке
            var anchor = new Entity(parent: rShoulder, position: new(0, -0.38f, 0.52f), rotation: new(16f, 0, 0));
            // Рым-кольцо и поперечный деревянный шток
            new Entity(parent: anchor, model: "diamond", scale: new(0.42f, 0.42f, 0.14f), position: new(0, 0, -0.18f), color: Color.Gold, unlit: true);
            new Entity(parent: anchor, model: "cube", scale: new(0.95f, 0.22f, 0.22f), position: new(0, 0, 0.18f), color: cfg.ArmorColor);
            // Веретено якоря, обвитое цепью
            new Entity(parent: anchor, model: "cube", scale: new(0.24f, 0.24f, 2.15f), position: new(0, 0, 0.82f), color: ironDark);
            foreach (float cz in new[] { 0.45f, 0.85f, 1.25f })
                new Entity(parent: anchor, model: "diamond", scale: new(0.32f, 0.32f, 0.16f), position: new(0, 0, cz), color: abyssGlow, unlit: true);
            // Рога и лапы якоря
            new Entity(parent: anchor, model: "diamond", scale: new(1.75f, 0.38f, 0.62f), position: new(0, 0, 1.75f), color: ironDark);
            new Entity(parent: anchor, model: "diamond", scale: new(0.42f, 0.42f, 0.55f), position: new(0, 0, 2.02f), color: abyssGlow, unlit: true);
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: anchor, model: "diamond", scale: new(0.44f, 0.38f, 0.72f), position: new(s * 0.74f, 0, 1.56f), rotation: new(0, s * -32f, 0), color: abyssGlow, unlit: true);
                new Entity(parent: anchor, model: "diamond", scale: new(0.26f, 0.28f, 0.46f), position: new(s * 0.88f, 0, 1.32f), rotation: new(0, s * -42f, 0), color: Color.White, unlit: true);
            }

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 17. GORDON FREEMAN (Half-Life)
        // ==========================================
        private static void BuildFreeman(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.22f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.22f, 0.86f, 0));
            foreach (var hip in new[] { lHip, rHip })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.44f, 0.26f), position: new(0, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.22f, 0.28f), position: new(0, -0.42f, 0.04f), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.42f, 0.24f), position: new(0, -0.62f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.18f, 0.42f), position: new(0, -0.80f, 0.06f), color: cfg.ArmorColor);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.62f, 0.20f, 0.42f), position: new(0, -0.32f, 0), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.70f, 0.62f, 0.44f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.46f, 0.22f), position: new(0, 0.10f, 0.15f), color: cfg.ArmorColor);
            // Знак Лямбды (λ) на груди костюма HEV Mark IV
            new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.26f, 0.08f), position: new(0, 0.14f, 0.26f), color: cfg.VisorColor, unlit: true);
            // Модуль длинных прыжков на спине
            new Entity(parent: torso, model: "cube", scale: new(0.48f, 0.52f, 0.24f), position: new(0, 0.10f, -0.28f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.18f, position: new(0, 0.12f, -0.40f), color: cfg.VisorColor, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.52f, 0.48f), color: Rgb(245, 205, 178));
            new Entity(parent: head, model: "cube", scale: new(0.52f, 0.20f, 0.52f), position: new(0, 0.20f, -0.02f), color: Rgb(72, 46, 32));
            // Эспаньолка (бородка) Фримана
            new Entity(parent: head, model: "cube", scale: new(0.22f, 0.14f, 0.14f), position: new(0, -0.16f, 0.20f), color: Rgb(72, 46, 32));
            // Очки в чёрной оправе
            new Entity(parent: head, model: "cube", scale: new(0.46f, 0.05f, 0.10f), position: new(0, 0.05f, 0.22f), color: Color.Black);
            foreach (float gx in new[] { -0.12f, 0.12f })
                new Entity(parent: head, model: "cube", scale: new(0.16f, 0.12f, 0.06f), position: new(gx, 0.04f, 0.24f), color: Rgb(190, 235, 255), unlit: true);
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));
            foreach (var sh in new[] { lShoulder, rShoulder })
            {
                new Entity(parent: sh, model: "diamond", scale: new(0.42f, 0.36f, 0.42f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.16f, 0.02f), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.BodyColor);
            }

            // Левая рука: Красная монтировка (Crowbar)
            var crowbar = new Entity(parent: lShoulder, position: new(0, -0.28f, 0.42f), rotation: new(25f, 0, 0));
            new Entity(parent: crowbar, model: "cube", scale: new(0.08f, 0.82f, 0.08f), position: new(0, 0.28f, 0), color: Rgb(220, 38, 38));
            new Entity(parent: crowbar, model: "cube", scale: new(0.08f, 0.12f, 0.24f), position: new(0, 0.68f, 0.08f), color: Color.LightGray);

            // Правая рука: Грави-пушка (Zero-Point Energy Field Manipulator)
            var gravGun = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: gravGun, model: "cube", scale: new(0.22f, 0.24f, 0.85f), position: new(0, 0.04f, 0.28f), color: Rgb(55, 62, 72));
            new Entity(parent: gravGun, model: "sphere", scaleUniform: 0.26f, position: new(0, 0.06f, 0.58f), color: cfg.VisorColor, unlit: true);
            foreach (float cx in new[] { -0.12f, 0.12f })
                new Entity(parent: gravGun, model: "diamond", scale: new(0.08f, 0.16f, 0.32f), position: new(cx, 0.04f, 0.72f), color: cfg.BodyColor);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 18. DANTE (Limbus Company)
        // ==========================================
        private static void BuildDante(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.19f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.19f, 0.86f, 0));
            foreach (var hip in new[] { lHip, rHip })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.44f, 0.22f), position: new(0, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.20f), position: new(0, -0.62f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.16f, 0.38f), position: new(0, -0.80f, 0.06f), color: Rgb(18, 18, 24));
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            // Алый плащ Менеджера Limbus Company поверх чёрного жилета
            new Entity(parent: torso, model: "diamond", scale: new(0.88f, 0.78f, 0.62f), position: new(0, -0.38f, -0.06f), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.58f, 0.60f, 0.38f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.36f, 0.56f, 0.18f), position: new(0, 0.06f, 0.14f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.38f, 0.08f), position: new(0, 0.12f, 0.24f), color: cfg.VisorColor, unlit: true);

            // Голова-Часы Данте в языках пламени
            var head = new Entity(parent: torso, position: new(0, 0.64f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.62f, 0.62f, 0.26f), color: Rgb(35, 28, 32));
            new Entity(parent: head, model: "diamond", scale: new(0.72f, 0.72f, 0.18f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.48f, 0.28f), position: new(0, 0, 0.04f), color: Rgb(22, 18, 24));
            // Золотые стрелки на циферблате
            new Entity(parent: head, model: "cube", scale: new(0.05f, 0.22f, 0.06f), position: new(0, 0.08f, 0.16f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "cube", scale: new(0.18f, 0.05f, 0.06f), position: new(0.06f, 0, 0.16f), color: cfg.VisorColor, unlit: true);
            // Огненная корона над часами
            new Entity(parent: head, model: "diamond", scale: new(0.48f, 0.55f, 0.24f), position: new(0, 0.42f, 0), color: Rgb(255, 95, 25), unlit: true);
            new Entity(parent: head, model: "diamond", scale: new(0.28f, 0.68f, 0.18f), position: new(0, 0.52f, 0), color: Color.Yellow, unlit: true);

            var halo = new Entity(parent: torso, position: new(0, 0.64f, -0.22f));
            new Entity(parent: halo, model: "diamond", scale: new(1.18f, 1.18f, 0.08f), color: cfg.VisorColor, unlit: true);
            parts.Halo = halo;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.46f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.46f, 0.25f, 0));
            foreach (var sh in new[] { lShoulder, rShoulder })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.22f, 0.24f, 0.22f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.06f, 0.24f), position: new(0, 0.16f, 0), color: cfg.VisorColor, unlit: true);
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            }
            // Ветвь Золотой Ветви (Golden Bough) в правой руке
            var bough = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: bough, model: "diamond", scale: new(0.18f, 0.18f, 0.85f), position: new(0, 0.04f, 0.28f), color: cfg.VisorColor, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 19. ЧЕБУРАШКА + СТЕНД КРОКОДИЛ ГЕНА
        // ==========================================
        private static void BuildCheburashka(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color furCol = cfg.BodyColor;       // Тёплый шоколадный мех
            Color beigeCol = cfg.ArmorColor;    // Светло-бежевая мордочка и животик
            Color orangeCol = cfg.VisorColor;   // Спелый апельсин

            var lHip = new Entity(parent: root, position: new(-0.20f, 0.55f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.55f, 0));
            foreach (var hip in new[] { lHip, rHip })
            {
                new Entity(parent: hip, model: "sphere", scale: new(0.26f, 0.34f, 0.28f), position: new(0, -0.15f, 0), color: furCol);
                new Entity(parent: hip, model: "sphere", scale: new(0.28f, 0.18f, 0.38f), position: new(0, -0.36f, 0.08f), color: furCol);
                new Entity(parent: hip, model: "sphere", scale: new(0.20f, 0.10f, 0.12f), position: new(0, -0.38f, 0.24f), color: beigeCol);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            // Пухлое уютное туловище Чебурашки
            var torso = new Entity(parent: root, position: new(0, 0.92f, 0));
            new Entity(parent: torso, model: "sphere", scale: new(0.76f, 0.82f, 0.66f), color: furCol);
            new Entity(parent: torso, model: "sphere", scale: new(0.56f, 0.62f, 0.28f), position: new(0, -0.02f, 0.24f), color: beigeCol);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.20f, position: new(0, -0.22f, -0.32f), color: furCol);

            // Голова с огромными фирменными ушами
            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.68f, 0.62f, 0.62f), color: furCol);
            new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.46f, 0.26f), position: new(0, -0.02f, 0.22f), color: beigeCol);

            // Большие добрые глаза, бровки и носик-пуговка
            foreach (float ex in new[] { -0.14f, 0.14f })
            {
                new Entity(parent: head, model: "sphere", scaleUniform: 0.15f, position: new(ex, 0.04f, 0.30f), color: Color.White);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.10f, position: new(ex, 0.04f, 0.34f), color: Rgb(28, 20, 18));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.04f, position: new(ex + 0.03f, 0.07f, 0.38f), color: Color.White, unlit: true);
                new Entity(parent: head, model: "cube", scale: new(0.12f, 0.03f, 0.04f), position: new(ex, 0.16f, 0.31f), rotation: new(0, 0, ex * -25f), color: furCol);
            }
            new Entity(parent: head, model: "sphere", scale: new(0.08f, 0.06f, 0.08f), position: new(0, -0.04f, 0.35f), color: Rgb(40, 26, 22));

            // ГИГАНТСКИЕ КРУГЛЫЕ УШИ ЧЕБУРАШКИ
            foreach (int side in new[] { -1, 1 })
            {
                var ear = new Entity(parent: head, position: new(side * 0.56f, 0.04f, -0.02f), rotation: new(0, side * -12f, 0));
                new Entity(parent: ear, model: "sphere", scale: new(0.62f, 0.66f, 0.16f), color: furCol);
                new Entity(parent: ear, model: "sphere", scale: new(0.46f, 0.50f, 0.18f), position: new(0, 0, 0.02f), color: beigeCol);
            }
            parts.Torso = torso;

            // Лапки (в правой лапке — яркий апельсин)
            var lShoulder = new Entity(parent: torso, position: new(-0.42f, 0.16f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.42f, 0.16f, 0));
            foreach (var sh in new[] { lShoulder, rShoulder })
            {
                new Entity(parent: sh, model: "sphere", scale: new(0.22f, 0.38f, 0.22f), position: new(0, -0.14f, 0.08f), rotation: new(-25f, 0, 0), color: furCol);
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.28f, 0.22f), color: beigeCol);
            }
            var orange = new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.28f, position: new(0, -0.28f, 0.38f), color: orangeCol, unlit: true);
            new Entity(parent: orange, model: "diamond", scale: new(0.10f, 0.08f, 0.10f), position: new(0, 0.15f, 0), color: Color.Lime, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;

            // ==========================================================
            // СТЕНД [КРОКОДИЛ ГЕНА] (привязан к parts.ShieldAura, скрыт до ульты)
            // ==========================================================
            var genaStand = new Entity(parent: torso, position: new(0.85f, 0.85f, -0.65f), enabled: false);
            Color crocGreen = Rgb(55, 165, 75);
            Color coatRed = Rgb(205, 38, 48);

            // Золотая JoJo-аура стенда
            new Entity(parent: genaStand, model: "diamond", scale: new(1.85f, 2.75f, 1.85f), position: new(0, 0.4f, 0), color: Rgb(255, 215, 55, 75), unlit: true);

            // Торс Гены в красном пиджаке, белой манишке и чёрной бабочке
            new Entity(parent: genaStand, model: "cube", scale: new(0.82f, 1.15f, 0.52f), position: new(0, 0.35f, 0), color: coatRed);
            new Entity(parent: genaStand, model: "diamond", scale: new(0.36f, 0.65f, 0.56f), position: new(0, 0.52f, 0.04f), color: Color.White);
            new Entity(parent: genaStand, model: "diamond", scale: new(0.28f, 0.14f, 0.60f), position: new(0, 0.78f, 0.06f), color: Rgb(25, 25, 30));
            // Мощный крокодилий хвост
            new Entity(parent: genaStand, model: "diamond", scale: new(0.34f, 0.32f, 1.05f), position: new(0, -0.18f, -0.52f), rotation: new(-25f, 0, 0), color: crocGreen);

            // Голова Крокодила Гены с длинной пастью, зубами и шляпой-федорой
            var gHead = new Entity(parent: genaStand, position: new(0, 1.15f, 0.10f));
            new Entity(parent: gHead, model: "cube", scale: new(0.56f, 0.46f, 0.58f), color: crocGreen);
            new Entity(parent: gHead, model: "cube", scale: new(0.46f, 0.24f, 0.92f), position: new(0, -0.04f, 0.52f), color: crocGreen);
            new Entity(parent: gHead, model: "cube", scale: new(0.42f, 0.12f, 0.84f), position: new(0, -0.18f, 0.48f), color: Rgb(140, 215, 130));
            foreach (float tx in new[] { -0.16f, 0.16f })
            {
                new Entity(parent: gHead, model: "diamond", scale: new(0.07f, 0.14f, 0.07f), position: new(tx, -0.12f, 0.82f), color: Color.White);
                new Entity(parent: gHead, model: "sphere", scaleUniform: 0.14f, position: new(tx, 0.16f, 0.24f), color: Color.Yellow, unlit: true);
            }
            // Фирменная шляпа Гены
            new Entity(parent: gHead, model: "cylinder" == "cube" ? "cube" : "diamond", scale: new(0.86f, 0.10f, 0.86f), position: new(0, 0.28f, -0.02f), color: Rgb(65, 45, 35));
            new Entity(parent: gHead, model: "cube", scale: new(0.52f, 0.32f, 0.52f), position: new(0, 0.42f, -0.02f), color: Rgb(80, 55, 42));

            // Мускулистые руки Гены и гармошка в руках
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: genaStand, model: "cube", scale: new(0.28f, 0.68f, 0.28f), position: new(s * 0.56f, 0.42f, 0.22f), rotation: new(-40f, 0, 0), color: coatRed);
                new Entity(parent: genaStand, model: "sphere", scaleUniform: 0.26f, position: new(s * 0.52f, 0.22f, 0.54f), color: crocGreen);
            }
            // Гармошка спереди
            var accordion = new Entity(parent: genaStand, position: new(0, 0.26f, 0.58f));
            new Entity(parent: accordion, model: "cube", scale: new(0.68f, 0.42f, 0.26f), color: Rgb(35, 38, 46));
            new Entity(parent: accordion, model: "cube", scale: new(0.42f, 0.36f, 0.30f), color: Color.Gold, unlit: true);

            parts.ShieldAura = genaStand;
        }
    }
}