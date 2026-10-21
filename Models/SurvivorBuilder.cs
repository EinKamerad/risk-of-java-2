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
    }

    public static class SurvivorDatabase
    {
        public static readonly string[] OrderedKeys =
        {
            "commando", "artificer", "huntress", "buzz", "v1", "sol", "gwen",
            "enforcer", "scout", "m4a1", "medic", "aigis", "reimu", "mindustry"
        };

        public static readonly Dictionary<string, SurvivorConfig> Classes = new()
        {
            ["commando"] = new()
            {
                Key = "commando", Name = "COMMANDO", Icon = "CMD", Subtitle = "Универсальный боец UES",
                Hp = 115, Speed = 9.6f, Damage = 12.0f, FireRate = 6.5f, ProjSpeed = 62f, ProjScale = 0.22f,
                BodyColor = Rgb(235, 130, 35), ArmorColor = Rgb(70, 78, 95), VisorColor = Rgb(60, 245, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Double Tap", Cd = 0.15f, Desc = "[ЛКМ] Скорострельная стрельба из двух пистолетов." },
                    ["m2"] = new() { Name = "Phase Round", Cd = 3.2f, Desc = "[ПКМ] Мощный плазменный заряд (300% урона), прошивающий врагов насквозь." },
                    ["shift"] = new() { Name = "Tactical Dive", Cd = 4.0f, Desc = "[Shift] Стремительный тактический рывок вперёд." },
                    ["r"] = new() { Name = "Suppressive Fire", Cd = 8.0f, Desc = "[R] Шквальная очередь из 8 выстрелов повышенной точности." }
                }
            },
            ["artificer"] = new()
            {
                Key = "artificer", Name = "ARTIFICER", Icon = "ART", Subtitle = "Высокотехнологичный Архимаг",
                Hp = 100, Speed = 8.5f, Damage = 34.0f, FireRate = 1.9f, ProjSpeed = 36f, ProjScale = 0.52f, Aoe = 4.2f,
                BodyColor = Rgb(155, 55, 230), ArmorColor = Rgb(225, 195, 105), VisorColor = Rgb(255, 235, 65),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Plasma Bolt", Cd = 0.52f, Desc = "[ЛКМ] Взрывные плазменные сферы, наносящие урон по площади." },
                    ["m2"] = new() { Name = "Charged Nano-Bomb", Cd = 5.0f, Desc = "[ПКМ] Сверхтяжёлая грави-сфера (280% урона в огромном радиусе)." },
                    ["shift"] = new() { Name = "Snapfreeze Surge", Cd = 4.5f, Desc = "[Shift] Реактивный взлёт вверх с ледяным взрывом под ногами." },
                    ["r"] = new() { Name = "Orbital Ion Strike", Cd = 10.0f, Desc = "[R] Обрушивает 5 орбитальных ионных лучей на ближайших врагов." }
                }
            },
            ["huntress"] = new()
            {
                Key = "huntress", Name = "HUNTRESS", Icon = "HNT", Subtitle = "Стремительный Снайпер",
                Hp = 95, Speed = 10.8f, Damage = 18.0f, FireRate = 3.4f, ProjSpeed = 54f, ProjScale = 0.28f,
                BodyColor = Rgb(215, 45, 70), ArmorColor = Rgb(45, 50, 64), VisorColor = Rgb(255, 240, 90),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Strafe Bow", Cd = 0.29f, Desc = "[ЛКМ] Выпускает энергетические стрелы с автонаведением." },
                    ["m2"] = new() { Name = "Laser Glaive", Cd = 5.5f, Desc = "[ПКМ] Бросает лазерную глефу, рикошетящую до 5 раз между врагами!" },
                    ["shift"] = new() { Name = "Phase Blink", Cd = 3.5f, Desc = "[Shift] Мгновенная телепортация в направлении взгляда." },
                    ["r"] = new() { Name = "Arrow Rain", Cd = 9.5f, Desc = "[R] Прыжок в небо и обстрел зоны градом энергетических стрел." }
                }
            },
            ["buzz"] = new()
            {
                Key = "buzz", Name = "BUZZ LIGHTYEAR", Icon = "BUZ", Subtitle = "Космический Рейнджер Star Command",
                Hp = 135, Speed = 9.8f, Damage = 16.0f, FireRate = 4.8f, ProjSpeed = 66f, ProjScale = 0.25f, Aoe = 1.2f,
                BodyColor = Rgb(125, 235, 65), ArmorColor = Rgb(240, 245, 250), VisorColor = Rgb(155, 65, 245),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Wrist Laser", Cd = 0.21f, Desc = "[ЛКМ] Скорострельный наручный лазер Звёздного Командования." },
                    ["m2"] = new() { Name = "Hyper-Beam", Cd = 4.0f, Desc = "[ПКМ] Пробивной луч полной мощности с взрывным импульсом." },
                    ["shift"] = new() { Name = "To Infinity & Beyond!", Cd = 4.5f, Desc = "[Shift] Раскрывает крылья и взмывает в воздух на реактивном ранце." },
                    ["r"] = new() { Name = "Star Command Salvo", Cd = 9.0f, Desc = "[R] Залп из 6 самонаводящихся фотонных ракет по врагам." }
                }
            },
            ["v1"] = new()
            {
                Key = "v1", Name = "V1 (ULTRAKILL)", Icon = "V1", Subtitle = "FPS-РЕЖИМ | 5 ОРУЖИЙ [1-5] | 3 РУКИ [F/G/R] | STYLE METER",
                Hp = 100, Speed = 12.6f, Damage = 19.0f, FireRate = 5.2f, ProjSpeed = 85f, ProjScale = 0.22f,
                BodyColor = Rgb(45, 115, 235), ArmorColor = Rgb(32, 38, 52), VisorColor = Rgb(255, 225, 40),
                Skills = new()
                {
                    ["m1"] = new() { Name = "[1-5 / Колёсико] 5 Оружий", Cd = 0.15f, Desc = "1: Marksman (Монетки) | 2: Shotgun (Ядро) | 3: Nailgun | 4: Railcannon | 5: Rocket." },
                    ["m2"] = new() { Name = "[F] Feedbacker & [G] Knuckleblaster", Cd = 0.45f, Desc = "[F] Синяя рука: Парирование (+PARRY), Projectile Boost и удар по монетке. [G] Красная рука." },
                    ["shift"] = new() { Name = "[Shift] 3x Stamina Dash & [Ctrl] Slam", Cd = 1.0f, Desc = "3 заряда рывка. [Ctrl] на земле — подкат, в воздухе — Ground Slam!" },
                    ["r"] = new() { Name = "[R] Whiplash (Зелёная рука-лассо)", Cd = 2.2f, Desc = "Притягивает лёгких врагов к вам, а вас — к тяжёлым врагам и боссам. Кровь лечит HP!" }
                }
            },
            ["sol"] = new()
            {
                Key = "sol", Name = "SOL BADGUY", Icon = "SOL", Subtitle = "Flame of Corruption [БЛИЖНИЙ БОЙ]",
                Hp = 160, Speed = 10.6f, Damage = 30.0f, FireRate = 2.9f, ProjSpeed = 42f, ProjScale = 0.48f, Aoe = 3.5f,
                BodyColor = Rgb(210, 35, 45), ArmorColor = Rgb(40, 35, 38), VisorColor = Rgb(255, 165, 30),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Junkyard Dog Slash", Cd = 0.34f, Desc = "[ЛКМ - Ближний бой] Широкие рубящие удары огненным мечом перед собой." },
                    ["m2"] = new() { Name = "Volcanic Viper & Gun Flame", Cd = 4.0f, Desc = "[ПКМ] Сокрушительный огненный апперкот мечом в прыжке + волна пламени." },
                    ["shift"] = new() { Name = "Fafnir Lunge", Cd = 3.8f, Desc = "[Shift] Стремительный пробивной рывок с огненным ударом кулака." },
                    ["r"] = new() { Name = "DRAGON INSTALL", Cd = 11.0f, Desc = "[R] Пробуждение силы Гира: взрыв Tyrant Rave + ускорение боя и регенерация на 6 сек." }
                }
            },
            ["gwen"] = new()
            {
                Key = "gwen", Name = "GWEN", Icon = "GWN", Subtitle = "Благая Швея [БЛИЖНИЙ БОЙ]",
                Hp = 135, Speed = 10.8f, Damage = 18.5f, FireRate = 4.8f, ProjSpeed = 52f, ProjScale = 0.30f, Aoe = 1.8f,
                BodyColor = Rgb(65, 195, 255), ArmorColor = Rgb(32, 28, 46), VisorColor = Rgb(195, 240, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Snip Snip! (Thousand Cuts)", Cd = 0.21f, Desc = "[ЛКМ - Ближний бой] Сверхбыстрые разрезы ножницами в конусе перед собой с вампиризмом." },
                    ["m2"] = new() { Name = "Center Snip!", Cd = 3.5f, Desc = "[ПКМ - Ближний бой] Мощный смыкающий разрез гигантскими ножницами (320% урона + лечение)." },
                    ["shift"] = new() { Name = "Skip 'n Slash & Hallowed Mist", Cd = 5.2f, Desc = "[Shift] Рывок вперёд и призыв Священного Тумана, лечащего Гвен и бьющего врагов." },
                    ["r"] = new() { Name = "Needlework", Cd = 9.0f, Desc = "[R] Дальний залп веером из 5 гигантских призрачных игл насквозь." }
                }
            },
            ["enforcer"] = new()
            {
                Key = "enforcer", Name = "ENFORCER", Icon = "ENF", Subtitle = "Тяжёлый Бастион UES",
                Hp = 175, Speed = 9.0f, Damage = 15.0f, FireRate = 1.65f, ProjSpeed = 64f, ProjScale = 0.20f,
                BodyColor = Rgb(55, 115, 195), ArmorColor = Rgb(38, 48, 68), VisorColor = Rgb(110, 225, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Riot Shotgun", Cd = 0.60f, Desc = "[ЛКМ] Мощный залп картечью из 6 пробивных дробин." },
                    ["m2"] = new() { Name = "Shield Bash", Cd = 4.0f, Desc = "[ПКМ - Ближний бой] Оглушающий удар щитом перед собой (стан 2с и +20 щита)." },
                    ["shift"] = new() { Name = "Protect and Serve", Cd = 1.2f, Desc = "[Shift] Переключение Стойки Щита: -65% входящего урона и +45% скорости стрельбы." },
                    ["r"] = new() { Name = "Tear Gas Grenade", Cd = 9.5f, Desc = "[R] Бросает гранату со слезоточивым газом (замедляет врагов и бьёт по области)." }
                }
            },
            ["scout"] = new()
            {
                Key = "scout", Name = "SCOUT (DRG)", Icon = "SCT", Subtitle = "ROCK AND STONE! Разведчик Hoxxes IV",
                Hp = 120, Speed = 11.4f, Damage = 14.5f, FireRate = 6.8f, ProjSpeed = 72f, ProjScale = 0.22f,
                BodyColor = Rgb(35, 115, 215), ArmorColor = Rgb(55, 65, 82), VisorColor = Rgb(255, 210, 55),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Deepcore GK2", Cd = 0.14f, Desc = "[ЛКМ] Скорострельная штурмовая винтовка с высокой точностью." },
                    ["m2"] = new() { Name = "Boomstick (Special Powder)", Cd = 3.6f, Desc = "[ПКМ] Дуплет из обреза + реактивный прыжок от отдачи в воздухе!" },
                    ["shift"] = new() { Name = "Grappling Hook", Cd = 3.2f, Desc = "[Shift] Выстреливает крюком-кошкой на 45м и притягивает Скаута на тросе." },
                    ["r"] = new() { Name = "IFG & Flare Grenade", Cd = 9.0f, Desc = "[R] Поле IFG: замедляет врагов на 65% и бьёт током по зоне." }
                }
            },
            ["m4a1"] = new()
            {
                Key = "m4a1", Name = "M4A1 (GFL)", Icon = "M4", Subtitle = "Лидер AR Team [DUMMY LINK SYSTEM]",
                Hp = 110, Speed = 10.4f, Damage = 15.5f, FireRate = 5.8f, ProjSpeed = 68f, ProjScale = 0.23f,
                BodyColor = Rgb(75, 95, 78), ArmorColor = Rgb(42, 46, 48), VisorColor = Rgb(95, 255, 135),
                Skills = new()
                {
                    ["m1"] = new() { Name = "M4A1 Assault Carbine", Cd = 0.17f, Desc = "[ЛКМ] Огонь из карабина. Клоны Dummy Link синхронно атакуют цели." },
                    ["m2"] = new() { Name = "Jupiter Cannon (MOD3)", Cd = 4.5f, Desc = "[ПКМ] Пробивной энерго-выстрел рельсовой пушки с взрывом (340% ур.)." },
                    ["shift"] = new() { Name = "Tactical Evasion", Cd = 3.6f, Desc = "[Shift] Тактический рывок с получением +25 щита и залпом клонов." },
                    ["r"] = new() { Name = "Dummy Link (До 4 Клонов)", Cd = 7.5f, Desc = "[R] Спавнит ИИ-клона. При смерти вы переселяетесь в живого клона!" }
                }
            },
            ["medic"] = new()
            {
                Key = "medic", Name = "MEDIC (TF2)", Icon = "MED", Subtitle = "Безумный Доктор Teufort [ÜBER & MIND CONTROL]",
                Hp = 130, Speed = 10.2f, Damage = 14.0f, FireRate = 6.2f, ProjSpeed = 64f, ProjScale = 0.22f,
                BodyColor = Rgb(235, 238, 245), ArmorColor = Rgb(205, 38, 45), VisorColor = Rgb(255, 75, 75),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Crusader's Syringe Gun", Cd = 0.16f, Desc = "[ЛКМ] Наносит урон врагам и ЛЕЧИТ союзников при попадании!" },
                    ["m2"] = new() { Name = "Kritzkrieg Medi-Beam", Cd = 3.4f, Desc = "[ПКМ] Лечит и усиливает союзника (+барьер) или выкачивает HP из врага." },
                    ["shift"] = new() { Name = "Amputator Healing Surge", Cd = 4.2f, Desc = "[Shift] Рывок с ударом пилой Ubersaw и целебным импульсом (+25% HP)." },
                    ["r"] = new() { Name = "ÜberCharge / Mind Control", Cd = 10.5f, Desc = "[R] На врага (<50% HP) — ПЕРЕМАНИВАЕТ его! Иначе — УБЕР-ЗАРЯД на 6.5с!" }
                }
            },
            ["aigis"] = new()
            {
                Key = "aigis", Name = "AIGIS (PERSONA 3)", Icon = "AIG", Subtitle = "Anti-Shadow Weapon [ORGIA & PALLADION]",
                Hp = 140, Speed = 10.6f, Damage = 13.5f, FireRate = 8.2f, ProjSpeed = 76f, ProjScale = 0.20f,
                BodyColor = Rgb(240, 242, 250), ArmorColor = Rgb(235, 195, 55), VisorColor = Rgb(65, 195, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Finger Vulcan Guns", Cd = 0.12f, Desc = "[ЛКМ] Ураганный пулемётный огонь из пальцев-стволов правой руки." },
                    ["m2"] = new() { Name = "Pandora Grenade Cannon", Cd = 3.8f, Desc = "[ПКМ] Тяжёлый фугасный залп из гранатомёта (300% урона, AoE 5.2м)." },
                    ["shift"] = new() { Name = "Orgia Mode Overdrive", Cd = 5.0f, Desc = "[Shift] Взлёт и режим Orgia на 5.5 сек (+50% атаки, +45% скорости, +35% урона)." },
                    ["r"] = new() { Name = "Persona: Palladion Strike", Cd = 9.5f, Desc = "[R] Обрушивает копья света на врагов вокруг и даёт +45 щита." }
                }
            },
            ["reimu"] = new()
            {
                Key = "reimu", Name = "HAKUREI REIMU", Icon = "RMU", Subtitle = "Жрица Храма Хакурей (Touhou Project)",
                Hp = 105, Speed = 11.2f, Damage = 17.5f, FireRate = 4.8f, ProjSpeed = 58f, ProjScale = 0.26f, Aoe = 1.2f,
                BodyColor = Rgb(225, 42, 52), ArmorColor = Rgb(245, 245, 250), VisorColor = Rgb(255, 230, 80),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Homing Ofuda Amulets", Cd = 0.21f, Desc = "[ЛКМ] Выпускает парные красно-белые амулеты с самонаведением." },
                    ["m2"] = new() { Name = "Yin-Yang Persuasion Orb", Cd = 4.0f, Desc = "[ПКМ] Сфера Инь-Ян: рикошетит до 6 раз между врагами с AoE!" },
                    ["shift"] = new() { Name = "Fantasy Nature Float", Cd = 3.8f, Desc = "[Shift] Воздушный рывок (+щит и выпуск 4 защитных талисманов)." },
                    ["r"] = new() { Name = "Spell Card: Fantasy Seal", Cd = 9.5f, Desc = "[R] Град из 10 радужных сфер Данмаку с самонаведением!" }
                }
            },
            ["mindustry"] = new()
            {
                Key = "mindustry", Name = "GAMMA CORE SHIP", Icon = "MND", Subtitle = "Командный Корабль Ядра (Mindustry)",
                Hp = 150, Speed = 11.5f, Damage = 15.0f, FireRate = 5.2f, ProjSpeed = 68f, ProjScale = 0.24f, Aoe = 1.0f,
                BodyColor = Rgb(245, 185, 45), ArmorColor = Rgb(62, 68, 82), VisorColor = Rgb(255, 235, 95),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Twin Mount Lasers", Cd = 0.19f, Desc = "[ЛКМ] Спаренные плазменные пушки корабля с обоих крыльев." },
                    ["m2"] = new() { Name = "Surge Alloy Blast", Cd = 3.8f, Desc = "[ПКМ] Снаряд Кинетического Сплава, поражающий врагов молниями." },
                    ["shift"] = new() { Name = "Core Engine Overdrive", Cd = 3.5f, Desc = "[Shift] Реактивный полёт двигателей с авто-ремонтом корпуса (+15% HP)." },
                    ["r"] = new() { Name = "Build Mode: Spectre Turret", Cd = 6.0f, Desc = "[R - Тратит 20% HP] Строит Турель Mindustry (до 4 шт.), которая наследует предметы!" }
                }
            }
        };

        public static SurvivorParts BuildSurvivorMesh(Entity parentEntity, string charKey)
        {
            if (!Classes.TryGetValue(charKey, out var cfg))
                cfg = Classes["commando"];

            var root = new Entity(parent: parentEntity);
            var parts = new SurvivorParts { Root = root };

            // Особая 3D-модель парящего Космического Корабля из Mindustry (Gamma Core Unit)
            if (charKey == "mindustry")
            {
                var leftHip = new Entity(parent: root, position: new(-0.36f, 0.85f, -0.28f));
                var rightHip = new Entity(parent: root, position: new(0.36f, 0.85f, -0.28f));
                foreach (var hip in new[] { leftHip, rightHip })
                {
                    new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.26f, 0.52f), color: cfg.ArmorColor);
                    new Entity(parent: hip, model: "cube", scale: new(0.32f, 0.10f, 0.36f), position: new(0, 0.14f, 0), color: cfg.BodyColor);
                    new Entity(parent: hip, model: "diamond", scale: new(0.30f, 0.30f, 0.62f), position: new(0, 0, -0.38f), color: Color.SkyBlue, unlit: true);
                    new Entity(parent: hip, model: "diamond", scale: new(0.18f, 0.18f, 0.82f), position: new(0, 0, -0.52f), color: Color.White, unlit: true);
                }
                parts.LeftLeg = leftHip;
                parts.RightLeg = rightHip;

                var mTorso = new Entity(parent: root, position: new(0, 1.35f, 0));
                new Entity(parent: mTorso, model: "diamond", scale: new(1.35f, 0.44f, 1.78f), color: cfg.BodyColor);
                new Entity(parent: mTorso, model: "diamond", scale: new(0.92f, 0.48f, 1.95f), position: new(0, 0, 0.10f), color: cfg.ArmorColor);
                new Entity(parent: mTorso, model: "cube", scale: new(0.72f, 0.38f, 1.05f), position: new(0, 0.12f, 0), color: cfg.ArmorColor);
                new Entity(parent: mTorso, model: "cube", scale: new(0.52f, 0.16f, 1.35f), position: new(0, -0.18f, 0), color: Rgb(40, 45, 58));
                new Entity(parent: mTorso, model: "diamond", scale: new(0.50f, 0.38f, 0.78f), position: new(0, 0.28f, 0.16f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: mTorso, model: "sphere", scaleUniform: 0.28f, position: new(0, 0.32f, 0.20f), color: Color.White, unlit: true);
                foreach (float sx in new[] { -0.22f, 0.22f })
                {
                    new Entity(parent: mTorso, model: "diamond", scale: new(0.14f, 0.16f, 0.65f), position: new(sx, -0.02f, 0.92f), color: cfg.BodyColor);
                    new Entity(parent: mTorso, model: "sphere", scaleUniform: 0.12f, position: new(sx, -0.02f, 1.18f), color: Color.Yellow, unlit: true);
                }

                foreach (int side in new[] { -1, 1 })
                {
                    var wing = new Entity(parent: mTorso, model: "diamond", scale: new(1.28f, 0.18f, 1.05f),
                        position: new(side * 0.72f, -0.02f, -0.08f), rotation: new(0, 0, side * -10f), color: cfg.BodyColor);
                    new Entity(parent: wing, model: "cube", scale: new(0.58f, 0.14f, 0.72f), position: new(side * 0.14f, 0.05f, 0.05f), color: cfg.ArmorColor);
                    new Entity(parent: wing, model: "cube", scale: new(0.14f, 0.16f, 0.92f), position: new(side * 0.28f, 0.02f, 0.36f), color: cfg.VisorColor, unlit: true);
                    new Entity(parent: wing, model: "diamond", scale: new(0.22f, 0.42f, 0.48f), position: new(side * 0.46f, 0.12f, -0.18f), rotation: new(0, 0, side * 22f), color: cfg.ArmorColor);
                }

                var mHalo = new Entity(parent: mTorso, position: new(0, 0.52f, -0.18f));
                new Entity(parent: mHalo, model: "diamond", scale: new(1.05f, 1.05f, 0.08f), rotation: new(90, 0, 0), color: Color.Yellow, unlit: true);
                new Entity(parent: mHalo, model: "diamond", scale: new(0.75f, 0.75f, 0.08f), rotation: new(90, 45, 0), color: Color.SkyBlue, unlit: true);
                parts.Halo = mHalo;
                parts.Torso = mTorso;

                var mLeftSh = new Entity(parent: mTorso, position: new(-0.66f, 0.06f, 0.35f));
                var mRightSh = new Entity(parent: mTorso, position: new(0.66f, 0.06f, 0.35f));
                foreach (var sh in new[] { mLeftSh, mRightSh })
                {
                    new Entity(parent: sh, model: "cube", scale: new(0.28f, 0.22f, 0.42f), color: cfg.BodyColor);
                    new Entity(parent: sh, model: "cube", scale: new(0.10f, 0.12f, 0.72f), position: new(-0.07f, 0, 0.32f), color: cfg.ArmorColor);
                    new Entity(parent: sh, model: "cube", scale: new(0.10f, 0.12f, 0.72f), position: new(0.07f, 0, 0.32f), color: cfg.ArmorColor);
                    new Entity(parent: sh, model: "sphere", scaleUniform: 0.22f, position: new(0, 0, 0.68f), color: cfg.VisorColor, unlit: true);
                }
                parts.LeftArm = mLeftSh;
                parts.RightArm = mRightSh;
                return parts;
            }

            // Детализированные ноги с наколенниками, щитками и подошвами
            var lHip = new Entity(parent: root, position: new(-0.22f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.22f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.44f, 0.25f), position: new(0, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.12f, 0.34f, 0.28f), position: new(side * 0.13f, -0.18f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.29f, 0.25f, 0.32f), position: new(0, -0.42f, 0.07f), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.14f, 0.08f, 0.10f), position: new(0, -0.42f, 0.18f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.42f, 0.22f), position: new(0, -0.62f, 0), color: Rgb(40, 44, 54));
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.30f, 0.12f), position: new(0, -0.60f, 0.10f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.16f, 0.38f), position: new(0, -0.80f, 0.07f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.06f, 0.40f), position: new(0, -0.86f, 0.07f), color: Rgb(28, 30, 38));
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.06f, 0.12f), position: new(0, -0.75f, 0.22f), color: cfg.VisorColor, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            // Торс с поясом, подсумками, нагрудником и реактивным ранцем
            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.60f, 0.22f, 0.40f), position: new(0, -0.32f, 0), color: Rgb(38, 42, 52));
            new Entity(parent: torso, model: "cube", scale: new(0.20f, 0.13f, 0.44f), position: new(0, -0.32f, 0), color: cfg.VisorColor, unlit: true);
            foreach (float px in new[] { -0.31f, 0.31f })
                new Entity(parent: torso, model: "cube", scale: new(0.10f, 0.18f, 0.24f), position: new(px, -0.31f, 0.04f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.64f, 0.42f), position: new(0, 0.05f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.72f, 0.28f, 0.36f), position: new(0, 0.18f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.60f, 0.54f, 0.30f), position: new(0, 0.12f, 0.16f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.24f, 0.28f, 0.34f), position: new(0, 0.14f, 0.21f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.44f, 0.12f, 0.40f), position: new(0, 0.39f, 0), color: cfg.ArmorColor);

            var backpack = new Entity(parent: torso, position: new(0, 0.08f, -0.30f));
            new Entity(parent: backpack, model: "cube", scale: new(0.50f, 0.54f, 0.26f), color: cfg.ArmorColor);
            new Entity(parent: backpack, model: "cube", scale: new(0.36f, 0.38f, 0.16f), position: new(0, 0, -0.10f), color: Rgb(42, 48, 58));
            new Entity(parent: backpack, model: "cube", scale: new(0.14f, 0.28f, 0.20f), position: new(0, 0, -0.12f), color: cfg.VisorColor, unlit: true);
            foreach (float sideX in new[] { -0.17f, 0.17f })
            {
                var thruster = new Entity(parent: backpack, model: "cube", scale: new(0.14f, 0.24f, 0.14f), position: new(sideX, -0.18f, -0.06f), rotation: new(12, 0, 0), color: Rgb(45, 50, 60));
                new Entity(parent: thruster, model: "diamond", scale: new(0.75f, 1.1f, 0.75f), position: new(0, -0.65f, 0), color: cfg.VisorColor, unlit: true);
            }
            parts.Torso = torso;

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            if (charKey != "v1")
                new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.58f, 0.56f), color: cfg.ArmorColor);

            if (charKey == "commando")
            {
                new Entity(parent: head, model: "sphere", scale: new(0.50f, 0.40f, 0.44f), position: new(0, 0.02f, 0.14f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: head, model: "cube", scale: new(0.56f, 0.08f, 0.42f), position: new(0, 0.20f, 0.05f), color: cfg.BodyColor);
                new Entity(parent: head, model: "cube", scale: new(0.24f, 0.14f, 0.22f), position: new(0, -0.18f, 0.22f), color: Rgb(40, 45, 55));
                new Entity(parent: head, model: "cube", scale: new(0.04f, 0.36f, 0.04f), position: new(-0.26f, 0.28f, -0.05f), color: cfg.BodyColor);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.07f, position: new(-0.26f, 0.46f, -0.05f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: torso, model: "cube", scale: new(0.66f, 0.10f, 0.45f), position: new(0, 0.08f, 0.02f), rotation: new(0, 0, 26f), color: Rgb(45, 50, 60));
                foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
                {
                    new Entity(parent: hip, model: "cube", scale: new(0.14f, 0.28f, 0.24f), position: new(side * 0.16f, -0.15f, 0), color: Rgb(35, 38, 45));
                    new Entity(parent: hip, model: "cube", scale: new(0.08f, 0.14f, 0.12f), position: new(side * 0.16f, -0.02f, -0.04f), color: cfg.BodyColor);
                }
            }
            else if (charKey == "artificer")
            {
                new Entity(parent: head, model: "cube", scale: new(0.46f, 0.18f, 0.38f), position: new(0, 0.02f, 0.16f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: head, model: "diamond", scale: new(0.72f, 0.30f, 0.72f), position: new(0, 0.22f, 0), color: cfg.ArmorColor);
                new Entity(parent: head, model: "diamond", scale: new(0.24f, 0.45f, 0.42f), position: new(0, 0.36f, 0.05f), color: cfg.BodyColor);
                foreach (float sx in new[] { -0.28f, 0.28f })
                {
                    new Entity(parent: torso, model: "diamond", scale: new(0.24f, 0.75f, 0.44f), position: new(sx, -0.48f, -0.04f), rotation: new(0, 0, sx * -18f), color: cfg.BodyColor);
                    new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.20f, 0.46f), position: new(sx, -0.78f, -0.04f), color: cfg.ArmorColor);
                }
                new Entity(parent: torso, model: "diamond", scale: new(0.56f, 0.82f, 0.18f), position: new(0, -0.52f, -0.22f), rotation: new(10, 0, 0), color: cfg.BodyColor);
                var halo = new Entity(parent: torso, position: new(0, 0.38f, -0.52f));
                new Entity(parent: halo, model: "diamond", scale: new(1.32f, 1.32f, 0.10f), color: Color.SkyBlue, unlit: true);
                new Entity(parent: halo, model: "diamond", scale: new(1.32f, 1.32f, 0.10f), rotation: new(0, 0, 45f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: halo, model: "sphere", scaleUniform: 0.28f, color: Color.Magenta, unlit: true);
                parts.Halo = halo;
            }
            else if (charKey == "huntress")
            {
                new Entity(parent: head, model: "diamond", scale: new(0.62f, 0.56f, 0.64f), position: new(0, 0.06f, -0.04f), color: cfg.BodyColor);
                foreach (float vy in new[] { -0.09f, 0.03f, 0.15f })
                    new Entity(parent: head, model: "diamond", scale: new(0.14f, 0.08f, 0.38f), position: new(0, vy, 0.19f), color: cfg.VisorColor, unlit: true);
                var scarf = new Entity(parent: torso, position: new(0, 0.35f, -0.20f), rotation: new(24, 0, 0));
                new Entity(parent: scarf, model: "cube", scale: new(0.58f, 0.18f, 0.38f), color: cfg.BodyColor);
                new Entity(parent: scarf, model: "diamond", scale: new(0.32f, 0.92f, 0.14f), position: new(-0.14f, -0.48f, -0.24f), rotation: new(0, 0, 8f), color: cfg.BodyColor);
                new Entity(parent: scarf, model: "diamond", scale: new(0.28f, 0.78f, 0.14f), position: new(0.16f, -0.42f, -0.22f), rotation: new(0, 0, -10f), color: cfg.BodyColor);
                var quiver = new Entity(parent: backpack, position: new(0.24f, 0.14f, -0.08f), rotation: new(0, 0, -20f));
                new Entity(parent: quiver, model: "cube", scale: new(0.18f, 0.58f, 0.18f), color: Rgb(45, 50, 64));
                foreach (float ax in new[] { -0.04f, 0.0f, 0.04f })
                    new Entity(parent: quiver, model: "diamond", scale: new(0.05f, 0.35f, 0.05f), position: new(ax, 0.36f, 0), color: Color.SkyBlue, unlit: true);
            }
            else if (charKey == "buzz")
            {
                new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.58f, 0.56f), color: Rgb(145, 55, 225));
                new Entity(parent: head, model: "sphere", scale: new(0.42f, 0.42f, 0.38f), position: new(0, -0.02f, 0.12f), color: Rgb(245, 205, 175));
                new Entity(parent: head, model: "cube", scale: new(0.24f, 0.14f, 0.20f), position: new(0, -0.16f, 0.22f), color: Rgb(235, 190, 160));
                new Entity(parent: head, model: "sphere", scale: new(0.76f, 0.74f, 0.76f), color: Rgb(190, 240, 255, 75), unlit: true);
                new Entity(parent: torso, model: "cube", scale: new(0.74f, 0.38f, 0.48f), position: new(0, 0.16f, 0.04f), color: Rgb(125, 235, 65));
                new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.10f, 0.50f), position: new(0, 0.32f, 0.04f), color: Rgb(145, 55, 225));
                new Entity(parent: torso, model: "sphere", scaleUniform: 0.09f, position: new(-0.20f, 0.18f, 0.29f), color: Color.Red, unlit: true);
                new Entity(parent: torso, model: "sphere", scaleUniform: 0.09f, position: new(-0.08f, 0.18f, 0.29f), color: Color.Lime, unlit: true);
                new Entity(parent: torso, model: "sphere", scaleUniform: 0.09f, position: new(0.04f, 0.18f, 0.29f), color: Color.Blue, unlit: true);
                new Entity(parent: torso, model: "sphere", scale: new(0.18f, 0.14f, 0.08f), position: new(0.20f, 0.18f, 0.28f), color: Color.Red, unlit: true);
                foreach (int side in new[] { -1, 1 })
                {
                    var wing = new Entity(parent: backpack, model: "cube", scale: new(1.22f, 0.30f, 0.08f), position: new(side * 0.75f, 0.12f, -0.06f), rotation: new(0, 0, side * -8f), color: Rgb(155, 65, 245));
                    new Entity(parent: wing, model: "cube", scale: new(0.98f, 0.42f, 0.12f), position: new(0, 0.35f, 0), color: Color.White);
                    new Entity(parent: wing, model: "cube", scale: new(0.96f, 0.22f, 0.14f), position: new(0, 0.45f, 0), color: Color.Red, unlit: true);
                    new Entity(parent: wing, model: "sphere", scale: new(0.18f, 0.72f, 0.38f), position: new(side * 0.52f, 0, 0), color: side > 0 ? Color.Lime : Color.Red, unlit: true);
                }
            }
            else if (charKey == "v1")
            {
                var camHead = new Entity(parent: head, model: "cube", scale: new(0.34f, 0.30f, 0.66f), position: new(0, 0.02f, 0.06f), color: Rgb(45, 115, 235));
                foreach (float sx in new[] { -0.20f, 0.20f })
                    new Entity(parent: camHead, model: "diamond", scale: new(0.12f, 0.42f, 0.85f), position: new(sx, 0.04f, -0.05f), color: Rgb(35, 90, 195));
                new Entity(parent: camHead, model: "cube", scale: new(0.42f, 0.42f, 0.16f), position: new(0, 0, 0.48f), color: Rgb(28, 32, 44));
                new Entity(parent: camHead, model: "sphere", scale: new(0.38f, 0.40f, 0.22f), position: new(0, 0, 0.55f), color: Rgb(255, 230, 45), unlit: true);
                var wingHub = new Entity(parent: torso, position: new(0, 0.18f, -0.34f));
                new Entity(parent: wingHub, model: "cube", scale: new(0.38f, 0.44f, 0.20f), color: Rgb(32, 38, 52));
                foreach (int side in new[] { -1, 1 })
                {
                    foreach (float ang in new[] { 42f, 16f, -10f, -36f })
                    {
                        var w = new Entity(parent: wingHub, rotation: new(0, side * -14f, side * ang));
                        new Entity(parent: w, model: "cube", scale: new(0.45f, 0.08f, 0.14f), position: new(side * 0.30f, 0, 0), color: Rgb(45, 115, 235));
                        new Entity(parent: w, model: "diamond", scale: new(1.25f, 0.15f, 0.10f), position: new(side * 0.78f, 0, 0), color: Rgb(255, 215, 35), unlit: true);
                    }
                }
            }
            else if (charKey == "sol")
            {
                new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(245, 200, 170));
                new Entity(parent: head, model: "cube", scale: new(0.55f, 0.14f, 0.55f), position: new(0, 0.14f, 0), color: Rgb(215, 30, 40));
                new Entity(parent: head, model: "cube", scale: new(0.24f, 0.12f, 0.08f), position: new(0, 0.14f, 0.26f), color: Rgb(190, 198, 210));
                foreach (float lx in new[] { -0.12f, 0.12f })
                    new Entity(parent: head, model: "diamond", scale: new(0.12f, 0.45f, 0.10f), position: new(lx, 0.02f, -0.32f), rotation: new(28, 0, 0), color: Rgb(215, 30, 40));
                foreach (var (hx, hy, hz, rx) in new[] { (0f, 0.32f, -0.16f, 35f), (-0.24f, 0.20f, -0.20f, 30f), (0.24f, 0.20f, -0.20f, 30f), (0f, -0.06f, -0.34f, 42f), (-0.18f, 0.28f, 0.08f, -15f), (0.18f, 0.28f, 0.08f, -15f) })
                    new Entity(parent: head, model: "diamond", scale: new(0.28f, 0.54f, 0.36f), position: new(hx, hy, hz), rotation: new(rx, 0, 0), color: Rgb(48, 30, 28));
                new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.20f, 0.44f), position: new(0, 0.38f, -0.04f), color: Rgb(210, 35, 45));
                new Entity(parent: torso, model: "cube", scale: new(0.26f, 0.12f, 0.44f), position: new(0, -0.32f, 0.02f), color: Color.Gold, unlit: true);
                new Entity(parent: torso, model: "cube", scale: new(0.76f, 0.58f, 0.14f), position: new(0, -0.40f, -0.20f), rotation: new(12, 0, 0), color: Rgb(200, 30, 40));
            }
            else if (charKey == "gwen")
            {
                new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 225, 220));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(-0.12f, 0.04f, 0.22f), color: Rgb(90, 220, 255), unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(0.12f, 0.04f, 0.22f), color: Rgb(255, 110, 195), unlit: true);
                new Entity(parent: head, model: "diamond", scale: new(0.68f, 0.24f, 0.30f), position: new(0, 0.28f, 0.05f), color: Rgb(32, 28, 46));
                new Entity(parent: head, model: "diamond", scale: new(0.22f, 0.26f, 0.12f), position: new(0, 0.32f, 0.18f), color: Color.Gold, unlit: true);
                foreach (int side in new[] { -1, 1 })
                {
                    var drill = new Entity(parent: head, position: new(side * 0.36f, 0.08f, -0.10f), rotation: new(0, 0, side * -18f));
                    new Entity(parent: drill, model: "diamond", scale: new(0.30f, 0.18f, 0.22f), position: new(0, 0.05f, 0), color: Rgb(215, 55, 110));
                    new Entity(parent: drill, model: "diamond", scale: new(0.36f, 0.98f, 0.36f), position: new(0, -0.38f, 0), color: Rgb(75, 205, 255));
                    new Entity(parent: drill, model: "diamond", scale: new(0.42f, 0.48f, 0.42f), position: new(0, -0.62f, 0), color: Rgb(60, 180, 240));
                    new Entity(parent: drill, model: "diamond", scale: new(0.26f, 0.45f, 0.26f), position: new(0, -0.88f, 0), color: Rgb(185, 240, 255), unlit: true);
                }
                new Entity(parent: torso, model: "diamond", scale: new(1.15f, 0.68f, 1.15f), position: new(0, -0.36f, 0), color: Rgb(32, 28, 46));
                new Entity(parent: torso, model: "diamond", scale: new(1.22f, 0.24f, 1.22f), position: new(0, -0.55f, 0), color: Rgb(195, 240, 255), unlit: true);
                new Entity(parent: torso, model: "diamond", scale: new(0.65f, 0.35f, 0.24f), position: new(0, -0.18f, -0.26f), color: Rgb(75, 205, 255), unlit: true);
            }
            else if (charKey == "enforcer")
            {
                new Entity(parent: head, model: "cube", scale: new(0.62f, 0.56f, 0.62f), color: cfg.ArmorColor);
                new Entity(parent: head, model: "cube", scale: new(0.56f, 0.18f, 0.40f), position: new(0, 0.04f, 0.16f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: head, model: "cube", scale: new(0.58f, 0.06f, 0.42f), position: new(0, 0.04f, 0.17f), color: cfg.ArmorColor);
                new Entity(parent: head, model: "cube", scale: new(0.66f, 0.14f, 0.66f), position: new(0, 0.26f, 0), color: cfg.BodyColor);
                new Entity(parent: torso, model: "cube", scale: new(0.86f, 0.28f, 0.58f), position: new(0, 0.34f, -0.02f), color: cfg.ArmorColor);
                new Entity(parent: torso, model: "sphere", scale: new(0.20f, 0.14f, 0.20f), position: new(-0.36f, 0.50f, 0), color: Color.SkyBlue, unlit: true);
                new Entity(parent: torso, model: "sphere", scale: new(0.20f, 0.14f, 0.20f), position: new(0.36f, 0.50f, 0), color: Color.Red, unlit: true);
            }
            else if (charKey == "scout")
            {
                new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.48f, 0.56f), position: new(0, 0.08f, 0), color: Rgb(245, 185, 40));
                new Entity(parent: head, model: "cube", scale: new(0.48f, 0.08f, 0.32f), position: new(0, 0.16f, 0.20f), color: Rgb(55, 65, 82));
                new Entity(parent: head, model: "sphere", scale: new(0.24f, 0.24f, 0.22f), position: new(0, 0.24f, 0.28f), color: Color.White, unlit: true);
                new Entity(parent: head, model: "cube", scale: new(0.50f, 0.14f, 0.32f), position: new(0, 0.02f, 0.16f), color: Rgb(60, 225, 255), unlit: true);
                var beard = new Entity(parent: head, position: new(0, -0.22f, 0.18f));
                new Entity(parent: beard, model: "diamond", scale: new(0.62f, 0.70f, 0.44f), color: Rgb(225, 95, 25));
                new Entity(parent: beard, model: "diamond", scale: new(0.40f, 0.58f, 0.36f), position: new(-0.20f, -0.08f, -0.04f), color: Rgb(205, 80, 20));
                new Entity(parent: beard, model: "diamond", scale: new(0.40f, 0.58f, 0.36f), position: new(0.20f, -0.08f, -0.04f), color: Rgb(205, 80, 20));
                new Entity(parent: beard, model: "cube", scale: new(0.22f, 0.08f, 0.22f), position: new(0, -0.22f, 0), color: Color.Gold, unlit: true);
                foreach (float bx in new[] { -0.28f, -0.14f, 0.14f, 0.28f })
                    new Entity(parent: torso, model: "cube", scale: new(0.09f, 0.24f, 0.10f), position: new(bx, -0.26f, 0.22f), color: Color.Yellow, unlit: true);
            }
            else if (charKey == "m4a1")
            {
                new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 222, 205));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.10f, position: new(-0.12f, 0.03f, 0.22f), color: Rgb(75, 245, 130), unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.10f, position: new(0.12f, 0.03f, 0.22f), color: Rgb(75, 245, 130), unlit: true);
                new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.40f, 0.54f), position: new(0, 0.14f, 0), color: Rgb(155, 135, 115));
                new Entity(parent: head, model: "diamond", scale: new(0.14f, 0.46f, 0.16f), position: new(-0.14f, -0.05f, 0.24f), rotation: new(0, 0, 12f), color: Rgb(85, 245, 140), unlit: true);
                new Entity(parent: head, model: "diamond", scale: new(0.26f, 0.90f, 0.26f), position: new(0.26f, -0.24f, -0.16f), rotation: new(18, 0, 0), color: Rgb(145, 125, 105));
                foreach (int side in new[] { -1, 1 })
                {
                    new Entity(parent: head, model: "cube", scale: new(0.12f, 0.26f, 0.24f), position: new(side * 0.27f, 0.04f, 0.02f), color: Rgb(40, 44, 52));
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.08f, position: new(side * 0.32f, 0.04f, 0.02f), color: Rgb(95, 255, 135), unlit: true);
                }
                new Entity(parent: torso, model: "cube", scale: new(0.66f, 0.22f, 0.48f), position: new(0, 0.32f, 0), color: Rgb(65, 110, 82));
                new Entity(parent: torso, model: "diamond", scale: new(0.98f, 0.72f, 0.92f), position: new(0, -0.38f, 0), color: Rgb(42, 46, 50));
                var jup = new Entity(parent: backpack, position: new(0.28f, 0.32f, -0.14f), rotation: new(-18, 0, 0));
                new Entity(parent: jup, model: "cube", scale: new(0.26f, 1.05f, 0.30f), position: new(0, 0.28f, 0), color: Rgb(48, 54, 62));
                new Entity(parent: jup, model: "cube", scale: new(0.14f, 0.96f, 0.34f), position: new(0, 0.30f, 0), color: Rgb(95, 255, 135), unlit: true);
            }
            else if (charKey == "medic")
            {
                new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.52f, 0.48f), color: Rgb(245, 205, 180));
                new Entity(parent: head, model: "cube", scale: new(0.50f, 0.18f, 0.50f), position: new(0, 0.22f, 0), color: Rgb(35, 30, 32));
                new Entity(parent: head, model: "diamond", scale: new(0.18f, 0.22f, 0.20f), position: new(0.08f, 0.32f, 0.20f), rotation: new(0, 0, -20f), color: Rgb(35, 30, 32));
                new Entity(parent: head, model: "cube", scale: new(0.44f, 0.04f, 0.10f), position: new(0, 0.05f, 0.22f), color: Rgb(40, 40, 45));
                new Entity(parent: head, model: "sphere", scale: new(0.18f, 0.16f, 0.10f), position: new(-0.12f, 0.04f, 0.24f), color: Color.White, unlit: true);
                new Entity(parent: head, model: "sphere", scale: new(0.18f, 0.16f, 0.10f), position: new(0.12f, 0.04f, 0.24f), color: Color.White, unlit: true);
                new Entity(parent: torso, model: "cube", scale: new(0.76f, 0.66f, 0.46f), position: new(0, -0.38f, 0), color: Rgb(235, 238, 245));
                new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.44f, 0.46f), position: new(0, 0.10f, 0.03f), color: Color.Red, unlit: true);
                new Entity(parent: backpack, model: "cube", scale: new(0.36f, 0.68f, 0.36f), position: new(0.18f, 0.10f, -0.12f), color: Rgb(220, 45, 45), unlit: true);
                new Entity(parent: backpack, model: "cube", scale: new(0.24f, 0.62f, 0.28f), position: new(-0.16f, 0.08f, -0.12f), color: Rgb(255, 195, 55), unlit: true);
            }
            else if (charKey == "aigis")
            {
                new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 228, 212));
                new Entity(parent: head, model: "sphere", scale: new(0.55f, 0.44f, 0.55f), position: new(0, 0.10f, 0), color: Rgb(245, 205, 65));
                new Entity(parent: head, model: "cube", scale: new(0.56f, 0.08f, 0.54f), position: new(0, 0.16f, 0), color: Rgb(55, 65, 85));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(-0.12f, 0.02f, 0.22f), color: Color.SkyBlue, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(0.12f, 0.02f, 0.22f), color: Color.SkyBlue, unlit: true);
                foreach (int side in new[] { -1, 1 })
                {
                    new Entity(parent: head, model: "diamond", scale: new(0.18f, 0.36f, 0.36f), position: new(side * 0.29f, 0.02f, 0), color: Rgb(225, 230, 240));
                    new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(side * 0.34f, 0.02f, 0), color: Color.SkyBlue, unlit: true);
                }
                new Entity(parent: torso, model: "diamond", scale: new(0.56f, 0.22f, 0.26f), position: new(0, 0.34f, 0.20f), color: Rgb(235, 45, 60), unlit: true);
                foreach (int side in new[] { -1, 1 })
                    new Entity(parent: backpack, model: "diamond", scale: new(0.85f, 0.24f, 0.16f), position: new(side * 0.48f, 0.14f, -0.06f), rotation: new(0, 0, side * 18f), color: Rgb(235, 195, 55));
            }
            else if (charKey == "reimu")
            {
                new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 225, 215));
                new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.42f, 0.54f), position: new(0, 0.12f, 0), color: Rgb(38, 28, 36));
                foreach (int side in new[] { -1, 1 })
                {
                    new Entity(parent: head, model: "cube", scale: new(0.14f, 0.38f, 0.14f), position: new(side * 0.28f, -0.10f, 0.08f), color: Color.White);
                    new Entity(parent: head, model: "diamond", scale: new(0.16f, 0.24f, 0.16f), position: new(side * 0.28f, -0.32f, 0.08f), color: Rgb(38, 28, 36));
                }
                var bowKnot = new Entity(parent: head, position: new(0, 0.18f, -0.26f), rotation: new(15, 0, 0));
                new Entity(parent: bowKnot, model: "diamond", scale: new(1.08f, 0.46f, 0.22f), color: Rgb(235, 35, 45), unlit: true);
                new Entity(parent: bowKnot, model: "diamond", scale: new(0.42f, 0.65f, 0.18f), position: new(0, -0.28f, 0), color: Rgb(235, 35, 45), unlit: true);
                new Entity(parent: torso, model: "diamond", scale: new(1.10f, 0.72f, 1.10f), position: new(0, -0.38f, 0), color: Rgb(225, 38, 48));
                new Entity(parent: torso, model: "diamond", scale: new(1.14f, 0.18f, 1.14f), position: new(0, -0.62f, 0), color: Color.White, unlit: true);
                new Entity(parent: torso, model: "cube", scale: new(0.54f, 0.26f, 0.46f), position: new(0, 0.22f, 0), color: Color.White);
                new Entity(parent: torso, model: "diamond", scale: new(0.38f, 0.18f, 0.24f), position: new(0, 0.28f, 0.22f), color: Color.Yellow, unlit: true);
                var halo = new Entity(parent: torso, position: new(0, 0.35f, -0.36f));
                foreach (var (side, colOrb) in new[] { (-1, Rgb(235, 40, 50)), (1, Color.White) })
                {
                    var orb = new Entity(parent: halo, model: "sphere", scaleUniform: 0.38f, position: new(side * 0.68f, 0, 0), color: colOrb, unlit: true);
                    new Entity(parent: orb, model: "diamond", scale: new(1.45f, 0.18f, 1.45f), color: Color.Gold, unlit: true);
                }
                parts.Halo = halo;
            }

            // Плечи, руки и фирменное оружие каждого из 14 выживших
            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));
            for (int idx = 0; idx < 2; idx++)
            {
                var sh = idx == 0 ? lShoulder : rShoulder;
                Vector3 padScale = charKey == "enforcer" ? new(0.56f, 0.50f, 0.56f) : new(0.42f, 0.38f, 0.42f);
                new Entity(parent: sh, model: "diamond", scale: padScale, position: new(0, 0.08f, 0), color: cfg.ArmorColor);
                if (charKey == "reimu")
                    new Entity(parent: sh, model: "diamond", scale: new(0.36f, 0.42f, 0.36f), position: new(0, -0.08f, 0.12f), color: Color.White);

                var arm = new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.18f, 0.56f), position: new(0, -0.12f, 0.22f), color: cfg.BodyColor);
                new Entity(parent: arm, model: "cube", scale: new(1.12f, 1.12f, 0.35f), position: new(0, 0, 0.35f), color: cfg.ArmorColor);

                if (charKey == "commando")
                {
                    var gun = new Entity(parent: arm, model: "cube", scale: new(0.65f, 1.1f, 0.95f), position: new(0, 0.15f, 0.58f), color: Rgb(38, 42, 52));
                    new Entity(parent: gun, model: "cube", scale: new(0.44f, 0.44f, 1.05f), position: new(0, 0.38f, 0.46f), color: cfg.VisorColor, unlit: true);
                    new Entity(parent: gun, model: "cube", scale: new(0.35f, 0.35f, 0.45f), position: new(0, -0.25f, 0.55f), color: cfg.BodyColor);
                }
                else if (charKey == "artificer")
                {
                    var orb = new Entity(parent: arm, model: "diamond", scale: new(1.45f, 1.45f, 0.95f), position: new(0, 0.12f, 0.68f), color: Color.Magenta, unlit: true);
                    new Entity(parent: orb, model: "sphere", scaleUniform: 0.65f, color: Color.Yellow, unlit: true);
                    new Entity(parent: orb, model: "diamond", scale: new(1.65f, 0.20f, 1.65f), color: Color.SkyBlue, unlit: true);
                }
                else if (charKey == "huntress")
                {
                    if (idx == 0)
                    {
                        var bow = new Entity(parent: sh, position: new(0, -0.1f, 0.56f), rotation: new(0, 0, 15f));
                        new Entity(parent: bow, model: "diamond", scale: new(0.10f, 0.82f, 0.24f), color: cfg.ArmorColor);
                        new Entity(parent: bow, model: "diamond", scale: new(0.12f, 0.26f, 0.28f), color: cfg.BodyColor);
                        new Entity(parent: bow, model: "cube", scale: new(0.03f, 0.76f, 0.04f), position: new(0, 0, -0.08f), color: cfg.VisorColor, unlit: true);
                        new Entity(parent: bow, model: "diamond", scale: new(0.05f, 0.05f, 0.52f), position: new(0, 0, 0.14f), color: Color.SkyBlue, unlit: true);
                        parts.Bow = bow;
                    }
                    else
                    {
                        var glaive = new Entity(parent: sh, model: "diamond", scale: new(0.36f, 0.08f, 0.36f), position: new(0.08f, -0.10f, 0.48f), color: Color.SkyBlue, unlit: true);
                        new Entity(parent: glaive, model: "sphere", scaleUniform: 0.45f, color: Color.White, unlit: true);
                    }
                }
                else if (charKey == "buzz" && idx == 1)
                {
                    var emitter = new Entity(parent: sh, model: "cube", scale: new(0.26f, 0.26f, 0.32f), position: new(0, -0.10f, 0.38f), color: Rgb(240, 245, 250));
                    new Entity(parent: emitter, model: "cube", scale: new(0.75f, 0.35f, 0.85f), position: new(0, 0.45f, 0), color: Rgb(125, 235, 65));
                    new Entity(parent: emitter, model: "sphere", scaleUniform: 0.48f, position: new(0, 0, 0.52f), color: Color.Red, unlit: true);
                }
                else if (charKey == "v1")
                {
                    if (idx == 0)
                        new Entity(parent: sh, model: "diamond", scale: new(0.18f, 0.18f, 0.06f), position: new(0, 0.05f, 0.58f), color: Color.Gold, unlit: true);
                    else
                    {
                        var rev = new Entity(parent: sh, model: "cube", scale: new(0.12f, 0.20f, 0.68f), position: new(0, -0.08f, 0.58f), color: Rgb(35, 42, 58));
                        new Entity(parent: rev, model: "sphere", scale: new(1.25f, 0.95f, 0.35f), position: new(0, 0.05f, -0.10f), color: Rgb(70, 80, 100));
                        new Entity(parent: rev, model: "cube", scale: new(0.68f, 0.40f, 0.98f), position: new(0, 0.36f, 0.16f), color: Color.SkyBlue, unlit: true);
                    }
                }
                else if (charKey == "sol" && idx == 1)
                {
                    var sword = new Entity(parent: sh, position: new(0, -0.12f, 0.52f), rotation: new(15, 0, 0));
                    new Entity(parent: sword, model: "cube", scale: new(0.20f, 0.32f, 0.36f), position: new(0, 0, 0.05f), color: Rgb(60, 65, 75));
                    new Entity(parent: sword, model: "cube", scale: new(0.13f, 0.26f, 1.42f), position: new(0, 0, 0.58f), color: Rgb(200, 35, 35));
                    new Entity(parent: sword, model: "cube", scale: new(0.15f, 0.10f, 1.38f), position: new(0, 0.10f, 0.58f), color: Rgb(255, 165, 30), unlit: true);
                    new Entity(parent: sword, model: "diamond", scale: new(0.10f, 0.16f, 1.45f), position: new(0, -0.10f, 0.60f), color: Rgb(220, 230, 245));
                }
                else if (charKey == "gwen" && idx == 1)
                {
                    var scis = new Entity(parent: sh, position: new(0, -0.12f, 0.48f));
                    foreach (float sy in new[] { -0.12f, 0.14f })
                        new Entity(parent: scis, model: "diamond", scale: new(0.08f, 0.22f, 0.24f), position: new(0, sy, -0.08f), color: Color.Gold, unlit: true);
                    new Entity(parent: scis, model: "diamond", scale: new(0.08f, 0.20f, 1.42f), position: new(0, 0.11f, 0.55f), rotation: new(-14, 0, 0), color: Rgb(110, 225, 255), unlit: true);
                    new Entity(parent: scis, model: "diamond", scale: new(0.08f, 0.20f, 1.42f), position: new(0, -0.07f, 0.55f), rotation: new(14, 0, 0), color: Rgb(195, 245, 255), unlit: true);
                    new Entity(parent: scis, model: "sphere", scaleUniform: 0.26f, position: new(0, 0.02f, 0.05f), color: Color.Gold, unlit: true);
                }
                else if (charKey == "enforcer")
                {
                    if (idx == 0)
                    {
                        var shield = new Entity(parent: sh, position: new(-0.28f, -0.15f, 0.45f), rotation: new(0, -25f, 0));
                        new Entity(parent: shield, model: "cube", scale: new(1.18f, 1.68f, 0.14f), color: cfg.ArmorColor);
                        new Entity(parent: shield, model: "cube", scale: new(1.25f, 1.75f, 0.08f), position: new(0, 0, -0.02f), color: cfg.BodyColor);
                        new Entity(parent: shield, model: "cube", scale: new(0.84f, 0.28f, 0.18f), position: new(0, 0.38f, 0), color: cfg.VisorColor, unlit: true);
                        new Entity(parent: shield, model: "diamond", scale: new(0.48f, 0.68f, 0.20f), position: new(0, -0.20f, 0), color: cfg.VisorColor, unlit: true);
                        foreach (float cy in new[] { -0.72f, 0.72f })
                            new Entity(parent: shield, model: "cube", scale: new(1.28f, 0.12f, 0.18f), position: new(0, cy, 0), color: Rgb(85, 95, 115));
                        var aura = new Entity(parent: shield, model: "cube", scale: new(1.68f, 1.98f, 0.06f), position: new(0, 0, 0.10f), color: Rgb(70, 210, 255, 110), unlit: true, enabled: false);
                        parts.Shield = shield;
                        parts.ShieldAura = aura;
                    }
                    else
                    {
                        var sg = new Entity(parent: sh, position: new(0, -0.10f, 0.55f));
                        new Entity(parent: sg, model: "cube", scale: new(0.16f, 0.22f, 0.98f), position: new(0, 0, 0.26f), color: Rgb(35, 40, 50));
                        new Entity(parent: sg, model: "cube", scale: new(0.22f, 0.15f, 0.56f), position: new(0, -0.08f, 0.36f), color: cfg.BodyColor);
                        new Entity(parent: sg, model: "cube", scale: new(0.12f, 0.08f, 0.88f), position: new(0, 0.12f, 0.28f), color: cfg.VisorColor, unlit: true);
                    }
                }
                else if (charKey == "scout")
                {
                    if (idx == 0)
                    {
                        var grap = new Entity(parent: sh, position: new(0, -0.10f, 0.50f));
                        new Entity(parent: grap, model: "cube", scale: new(0.16f, 0.20f, 0.58f), position: new(0, 0, 0.16f), color: Rgb(235, 175, 40));
                        new Entity(parent: grap, model: "diamond", scale: new(0.34f, 0.14f, 0.34f), position: new(0, 0, 0.48f), color: Color.SkyBlue, unlit: true);
                        new Entity(parent: grap, model: "diamond", scale: new(0.14f, 0.34f, 0.34f), position: new(0, 0, 0.48f), color: Color.SkyBlue, unlit: true);
                    }
                    else
                    {
                        var gk2 = new Entity(parent: sh, position: new(0, -0.10f, 0.55f));
                        new Entity(parent: gk2, model: "cube", scale: new(0.14f, 0.22f, 0.88f), position: new(0, 0, 0.26f), color: Rgb(45, 52, 68));
                        new Entity(parent: gk2, model: "cube", scale: new(0.10f, 0.26f, 0.18f), position: new(0, -0.18f, 0.22f), color: Rgb(245, 185, 40));
                        new Entity(parent: gk2, model: "cube", scale: new(0.16f, 0.09f, 0.74f), position: new(0, 0.11f, 0.28f), color: Rgb(35, 130, 235), unlit: true);
                    }
                }
                else if (charKey == "m4a1" && idx == 1)
                {
                    var rifle = new Entity(parent: sh, position: new(0, -0.10f, 0.55f));
                    new Entity(parent: rifle, model: "cube", scale: new(0.12f, 0.20f, 0.96f), position: new(0, 0, 0.28f), color: Rgb(35, 38, 42));
                    new Entity(parent: rifle, model: "cube", scale: new(0.09f, 0.26f, 0.16f), position: new(0, -0.18f, 0.24f), color: Rgb(55, 62, 58));
                    new Entity(parent: rifle, model: "cube", scale: new(0.14f, 0.12f, 0.26f), position: new(0, 0.15f, 0.22f), color: Rgb(95, 255, 135), unlit: true);
                }
                else if (charKey == "medic")
                {
                    if (idx == 0)
                    {
                        var medigun = new Entity(parent: sh, position: new(0, -0.12f, 0.52f));
                        new Entity(parent: medigun, model: "cube", scale: new(0.22f, 0.24f, 0.82f), position: new(0, 0, 0.22f), color: Rgb(70, 75, 85));
                        new Entity(parent: medigun, model: "cube", scale: new(0.26f, 0.26f, 0.22f), position: new(0, 0, 0.52f), color: Rgb(185, 195, 210));
                        new Entity(parent: medigun, model: "sphere", scaleUniform: 0.28f, position: new(0, 0, 0.66f), color: Color.Red, unlit: true);
                    }
                    else
                    {
                        var syrGun = new Entity(parent: sh, position: new(0, -0.10f, 0.52f));
                        new Entity(parent: syrGun, model: "cube", scale: new(0.18f, 0.20f, 0.72f), position: new(0, 0, 0.22f), color: Rgb(60, 65, 75));
                        new Entity(parent: syrGun, model: "sphere", scale: new(0.26f, 0.26f, 0.45f), position: new(0, 0.15f, 0.22f), color: Color.Red, unlit: true);
                        new Entity(parent: syrGun, model: "diamond", scale: new(0.06f, 0.18f, 0.55f), position: new(0, -0.12f, 0.45f), color: Rgb(220, 230, 245));
                    }
                }
                else if (charKey == "aigis")
                {
                    if (idx == 0)
                    {
                        var cannon = new Entity(parent: sh, position: new(0, -0.10f, 0.50f));
                        new Entity(parent: cannon, model: "cube", scale: new(0.26f, 0.26f, 0.76f), position: new(0, 0, 0.22f), color: Rgb(235, 195, 55));
                        new Entity(parent: cannon, model: "sphere", scaleUniform: 0.24f, position: new(0, 0, 0.62f), color: Color.Orange, unlit: true);
                    }
                    else
                    {
                        var barrel = new Entity(parent: sh, position: new(0, -0.10f, 0.52f));
                        new Entity(parent: barrel, model: "cube", scale: new(0.24f, 0.18f, 0.58f), position: new(0, 0, 0.18f), color: Color.SkyBlue, unlit: true);
                        foreach (float fx in new[] { -0.08f, 0.0f, 0.08f })
                            new Entity(parent: barrel, model: "cube", scale: new(0.04f, 0.06f, 0.32f), position: new(fx, 0, 0.52f), color: Rgb(235, 195, 55));
                    }
                }
                else if (charKey == "reimu")
                {
                    if (idx == 0)
                    {
                        foreach (float oy in new[] { -0.06f, 0.06f })
                            new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.03f, 0.32f), position: new(0, oy, 0.52f), rotation: new(0, oy * 180f, 0), color: Rgb(245, 50, 65), unlit: true);
                    }
                    else
                    {
                        var gohei = new Entity(parent: sh, position: new(0, -0.10f, 0.55f), rotation: new(20, 0, 0));
                        new Entity(parent: gohei, model: "cube", scale: new(0.08f, 0.08f, 1.18f), position: new(0, 0, 0.36f), color: Rgb(140, 85, 45));
                        new Entity(parent: gohei, model: "diamond", scale: new(0.38f, 0.48f, 0.38f), position: new(0, 0, 0.98f), color: Color.White, unlit: true);
                        new Entity(parent: gohei, model: "diamond", scale: new(0.28f, 0.36f, 0.28f), position: new(0, -0.18f, 0.88f), color: Rgb(245, 50, 65), unlit: true);
                    }
                }
            }

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
            return parts;
        }
    }
}