using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Data
{
    public class TierInfo
    {
        public string Name = "";
        public Color Color;
    }

    public class ItemInfo
    {
        public string Key = "";
        public string Name = "";
        public string Short = "";
        public string Tier = "white";
        public string TierName = "ОБЫЧНЫЙ";
        public string Desc = "";
        public Color MeshColor;
        public Color Color;
        public bool DefaultUnlocked = true;
        public bool IsVoid = false;
        public bool IsLunar = false;
    }

    public class AchievementInfo
    {
        public string Key = "";
        public string Name = "";
        public string Desc = "";
        public string? UnlocksItem;
        public string RewardText = "";
    }

    public class MonsterLogInfo
    {
        public string Key = "";
        public string Name = "";
        public float Hp;
        public float Damage;
        public string Desc = "";
    }

    public class StagePalette
    {
        public string Id = "";
        public string Name = "";
        public string Desc = "";
        public Color Ground;
        public Color Ruins;
        public Color Accent;
        public Color Sky;
        public Color Nebula;
        public Color Moon;
    }

    public static class GameData
    {
        private static readonly Random _rng = new();

        public static readonly Dictionary<string, TierInfo> TierMap = new()
        {
            ["white"] = new TierInfo { Name = "ОБЫЧНЫЙ", Color = Rgb(235, 242, 250) },
            ["green"] = new TierInfo { Name = "НЕОБЫЧНЫЙ", Color = Rgb(85, 235, 95) },
            ["red"] = new TierInfo { Name = "ЛЕГЕНДАРНЫЙ", Color = Rgb(255, 55, 65) },
            ["void"] = new TierInfo { Name = "ПУСТОТА", Color = Rgb(205, 85, 255) },
            ["lunar"] = new TierInfo { Name = "ЛУННЫЙ", Color = Rgb(65, 185, 255) }
        };

        public static readonly Dictionary<string, ItemInfo> ItemPool = new();
        public static readonly Dictionary<string, ItemInfo> VoidItems = new();
        public static readonly Dictionary<string, ItemInfo> LunarItems = new();
        public static readonly Dictionary<string, List<string>> ItemsByTier = new();

        public static readonly string[] BossTypes = { "boss", "vagrant", "overlord", "magma_worm", "dunestrider", "solus_control" };

        public static readonly Dictionary<string, string> BossTitles = new()
        {
            ["boss"] = "КАМЕННЫЙ ТИТАН :: ИЗНАЧАЛЬНЫЙ СТРАЖ",
            ["vagrant"] = "БЛУЖДАЮЩИЙ БРОДЯГА :: НЕЖНЫЙ ЗАЩИТНИК",
            ["overlord"] = "ПОВЕЛИТЕЛЬ БЕСОВ :: ВЛАДЫКА КРАСНОЙ ПЛОСКОСТИ",
            ["magma_worm"] = "МАГМОВЫЙ ЧЕРВЬ :: ИСПОЛИН ПРЕИСПОДНЕЙ",
            ["dunestrider"] = "ПЕСЧАНЫЙ СТРАННИК :: СОСУД ДЕГТЯ",
            ["solus_control"] = "МОДУЛЬ СОЛУС :: АВТОНОМНЫЙ ПРОТОКОЛ",
            ["mithrix"] = "МИТРИКС :: КОРОЛЬ НИЧЕГО"
        };

        public static readonly Dictionary<string, MonsterLogInfo> MonsterLog = new()
        {
            ["lemurian"] = new() { Key = "lemurian", Name = "Лемуриец", Hp = 65, Damage = 12, Desc = "Быстрый двуногий ящер ближнего боя. Охотится стаями и атакует когтями." },
            ["wisp"] = new() { Key = "wisp", Name = "Малый Огонёк", Hp = 46, Damage = 14.5f, Desc = "Парящий дух огня в древней железной маске. Атакует сгустками плазмы издалека." },
            ["beetle"] = new() { Key = "beetle", Name = "Рабочий Жук", Hp = 52, Damage = 10.5f, Desc = "Стремительное панцирное насекомое. Таранным рывком сбивает выживших с ног." },
            ["jellyfish"] = new() { Key = "jellyfish", Name = "Блуждающая Медуза", Hp = 42, Damage = 18.0f, Desc = "Парящее биолюминесцентное создание, взрывающееся электрическим импульсом вблизи." },
            ["bison"] = new() { Key = "bison", Name = "Рогатый Бизон", Hp = 155, Damage = 20.0f, Desc = "Массивный зверь с кварцевыми рогами. Склоняет голову перед стремительным таранным рывком." },
            ["brass_contraption"] = new() { Key = "brass_contraption", Name = "Латунный Механизм", Hp = 145, Damage = 22.0f, Desc = "Древний парящий колокол. Раскручивает кольцо и мечет очереди тяжёлых шипастых сфер." },
            ["golem"] = new() { Key = "golem", Name = "Каменный Голем", Hp = 195, Damage = 24.0f, Desc = "Тяжёлый страж руин. Заранее наводит красный лазерный прицел перед смертоносным выстрелом." },
            ["imp"] = new() { Key = "imp", Name = "Бес Пустоты", Hp = 95, Damage = 17.5f, Desc = "Порождение Красной Плоскости. Мгновенно телепортируется к жертве и рвёт когтями." },
            ["greater_wisp"] = new() { Key = "greater_wisp", Name = "Большой Огонёк", Hp = 175, Damage = 21.5f, Desc = "Древний дух изумрудного пламени. Сводит парящие руки перед залпом парных черепов." },
            ["parent"] = new() { Key = "parent", Name = "Прародитель", Hp = 240, Damage = 27.0f, Desc = "Астральный гигант. Телепортируется к выжившим и с размаху бьёт двумя руками по земле." },
            ["elder_lemurian"] = new() { Key = "elder_lemurian", Name = "Древний Лемуриец", Hp = 290, Damage = 26.0f, Desc = "Исполинский бронированный ящер. Извергает веер из 5 огненных шаров и сотрясает землю." },
            ["lunar_chimera"] = new() { Key = "lunar_chimera", Name = "Лунная Химера", Hp = 215, Damage = 23.0f, Desc = "Совершенный конструкт Митрикса из белого лунного камня и кобальтовой плазмы." },
            ["boss"] = new() { Key = "boss", Name = "Каменный Титан", Hp = 2100, Damage = 32, Desc = "Колоссальный страж Телепорта. Наводит лазерный луч и вызывает каменные разломы из-под земли." },
            ["vagrant"] = new() { Key = "vagrant", Name = "Блуждающий Бродяга", Hp = 2250, Damage = 30, Desc = "Гигантская небесная медуза. Сжимает купол перед залпом сфер и заряжает ослепительную сверхновую." },
            ["overlord"] = new() { Key = "overlord", Name = "Повелитель Бесов", Hp = 2400, Damage = 34, Desc = "Владыка демонов Пустоты. Обрушивается сверху в отмеченные рунные круги и мечет веера шипов." },
            ["magma_worm"] = new() { Key = "magma_worm", Name = "Магмовый Червь", Hp = 2600, Damage = 35, Desc = "Исполинский змей из раскалённой магмы. Прорывает землю в огненных кольцах и разбрасывает лаву." },
            ["dunestrider"] = new() { Key = "dunestrider", Name = "Песчаный Странник", Hp = 2350, Damage = 31, Desc = "Шагающий глиняный сосуд. Катит дегтярные бомбы и создаёт воронку, притягивающую выживших." },
            ["solus_control"] = new() { Key = "solus_control", Name = "Модуль Управления Солус", Hp = 2300, Damage = 32, Desc = "Боевое ядро ИИ. Фокусирует прицельные залпы и подбрасывает цели гравитационными кольцами." },
            ["mithrix"] = new() { Key = "mithrix", Name = "Митрикс, Король Ничего", Hp = 3600, Damage = 38, Desc = "Повелитель Луны. Сражается в 4 фазах гигантским молотом с читаемыми замахами и крадёт предметы." }
        };

        public static readonly Dictionary<string, AchievementInfo> Achievements = new()
        {
            ["ach_slayer"] = new() { Key = "ach_slayer", Name = "Истребитель", Desc = "Уничтожьте 12 монстров за всё время.", UnlocksItem = "lens", RewardText = "Открывает: Очки Часовщика (Крит x2)" },
            ["ach_boss"] = new() { Key = "ach_boss", Name = "Убийца Титанов", Desc = "Победите Босса Телепорта.", UnlocksItem = "feather", RewardText = "Открывает: Перо Хопу (+Прыжок в воздухе)" },
            ["ach_traveler"] = new() { Key = "ach_traveler", Name = "Исследователь миров", Desc = "Перейдите на 2-й этап через Телепорт.", UnlocksItem = null, RewardText = "Почётная запись в Журнале выжившего" },
            ["ach_hoarder"] = new() { Key = "ach_hoarder", Name = "Коллекционер артефактов", Desc = "Соберите 6 предметов за один забег.", UnlocksItem = null, RewardText = "Почётная запись в Журнале выжившего" }
        };

        public static readonly List<StagePalette> StagePalettes = new()
        {
            new() { Id = "roost", Name = "Далёкое Гнездовье", Desc = "Туманные утёсы и руины древних храмов на краю океана Petrichor V.", Ground = Rgb(36, 52, 66), Ruins = Rgb(62, 78, 96), Accent = Rgb(75, 225, 255), Sky = Rgb(14, 22, 36), Nebula = Rgb(45, 110, 165), Moon = Rgb(210, 230, 255) },
            new() { Id = "acres", Name = "Выжженные Угодья", Desc = "Выжженные солнцем террасы с раскалёнными обелисками и пеплом.", Ground = Rgb(74, 46, 36), Ruins = Rgb(102, 68, 54), Accent = Rgb(255, 150, 45), Sky = Rgb(34, 18, 16), Nebula = Rgb(165, 70, 40), Moon = Rgb(255, 200, 140) },
            new() { Id = "depths", Name = "Глубины Бездны", Desc = "Глубокий тектонический разлом, озарённый кристаллами магмы.", Ground = Rgb(42, 30, 62), Ruins = Rgb(70, 52, 98), Accent = Rgb(240, 65, 195), Sky = Rgb(20, 12, 34), Nebula = Rgb(135, 40, 145), Moon = Rgb(245, 160, 255) },
            new() { Id = "siren", Name = "Зов Сирены", Desc = "Кладбище разбившихся кораблей среди изумрудных скал.", Ground = Rgb(30, 60, 56), Ruins = Rgb(54, 90, 84), Accent = Rgb(90, 255, 165), Sky = Rgb(12, 28, 28), Nebula = Rgb(40, 140, 115), Moon = Rgb(180, 255, 220) }
        };

        public static readonly Dictionary<string, StagePalette> SpecialPalettes = new()
        {
            ["bazaar"] = new() { Id = "bazaar", Name = "Базар Между Временем", Desc = "Тайное святилище Тритона вне пространства и времени.", Ground = Rgb(20, 26, 46), Ruins = Rgb(38, 52, 88), Accent = Rgb(65, 195, 255), Sky = Rgb(10, 14, 28), Nebula = Rgb(35, 95, 185), Moon = Rgb(140, 215, 255) },
            ["moon"] = new() { Id = "moon", Name = "Начало (Луна)", Desc = "Расколотая цитадель Митрикса на орбите Petrichor V.", Ground = Rgb(185, 198, 218), Ruins = Rgb(55, 68, 92), Accent = Rgb(75, 215, 255), Sky = Rgb(8, 12, 24), Nebula = Rgb(40, 110, 195), Moon = Rgb(90, 210, 255) },
            ["pvp"] = new() { Id = "pvp", Name = "Колизей Раздора (PvP Арена)", Desc = "Гладиаторская арена выживших. Сражайтесь с соперниками и выбивайте артефакты из монстров!", Ground = Rgb(38, 30, 48), Ruins = Rgb(68, 54, 82), Accent = Rgb(255, 95, 65), Sky = Rgb(18, 12, 24), Nebula = Rgb(165, 45, 75), Moon = Rgb(255, 165, 120) }
        };

        static GameData()
        {
            // Обычные (White)
            AddItem("syringe", "Шприц Солдата", "SYR", "white", "+15% скорости атаки", Rgb(255, 225, 60));
            AddItem("hoof", "Козлиное Копыто Пола", "HOOF", "white", "+14% скорости бега", Rgb(70, 235, 255));
            AddItem("bear", "Плюшевый Мишка", "BEAR", "white", "+15% шанс блока урона", Rgb(255, 150, 50));
            AddItem("gasoline", "Канистра Бензина", "GAS", "white", "Взрыв врагов при смерти (AoE)", Rgb(255, 65, 65));
            AddItem("lens", "Очки Часовщика", "CRIT", "white", "+10% шанс критического урона (x2)", Rgb(255, 45, 85));
            AddItem("crowbar", "Монтировка", "CROW", "white", "+50% урона врагам с >85% HP", Rgb(210, 220, 235));
            AddItem("tri_tip", "Трёхгранный Кинжал", "BLEED", "white", "+15% шанс вызвать кровотечение", Rgb(225, 35, 55));
            AddItem("ap_rounds", "Бронебойные Патроны", "APR", "white", "+20% урона по Боссам", Rgb(255, 185, 80));
            AddItem("backup_mag", "Запасной Магазин", "MAG", "white", "-12% перезарядки навыка ПКМ (M2)", Rgb(180, 200, 230));
            AddItem("energy_drink", "Энергетик", "NRG", "white", "+20% скорости рывка и +8% бега", Rgb(50, 205, 255));
            AddItem("bison_steak", "Стейк из Бизона", "STEAK", "white", "+25 к максимальному HP", Rgb(215, 75, 65));
            AddItem("monster_tooth", "Клык Монстра", "TOOTH", "white", "Лечит +8 HP при убийстве врага", Rgb(120, 255, 130));
            AddItem("medkit", "Аптечка", "MED", "white", "Лечит +15 HP после получения урона", Rgb(90, 245, 145));
            AddItem("repulsion_armor", "Бронепластина Отражения", "PLATE", "white", "Снижает входящий урон на 4 ед.", Rgb(155, 175, 205));
            AddItem("topaz_brooch", "Топазовая Брошь", "TOPAZ", "white", "+15 временного щита при убийстве", Rgb(255, 195, 45));
            AddItem("opal", "Причудливый Опал", "OPAL", "white", "Снижает редкий урон на 30%", Rgb(175, 135, 255));
            AddItem("focus_crystal", "Фокусирующий Кристалл", "FOCUS", "white", "+20% урона врагам вблизи (<13м)", Rgb(255, 70, 145));
            AddItem("sticky_bomb", "Липкая Бомба", "BOMB", "white", "12% шанс прикрепить взрывчатку", Rgb(255, 125, 40));
            AddItem("stun_grenade", "Светошумовая Граната", "STUN", "white", "10% шанс оглушить врага на 1.5с", Rgb(210, 240, 255));
            AddItem("bundle_fireworks", "Связка Фейерверков", "FIREW", "white", "Салют ракетами при открытии сундука", Rgb(255, 95, 205));
            AddItem("bungus", "Неугомонный Гриб", "BUNG", "white", "Стоя на месте >1.2с, создаёт зону лечения (4.5% HP/с вам и союзникам)", Rgb(95, 255, 125));

            // Необычные (Green)
            AddItem("ukulele", "Укулеле", "UKU", "green", "25% шанс цепной молнии", Rgb(235, 80, 255));
            AddItem("feather", "Перо Хопу", "JMP", "green", "+1 прыжок в воздухе", Rgb(110, 255, 140));
            AddItem("atg_missile", "Ракетница AtG Mk. 1", "ATG", "green", "12% шанс выпустить ракету (220% ур.)", Rgb(115, 235, 75));
            AddItem("will_o_wisp", "Блуждающий Огонёк в Банке", "WISP", "green", "Мощный взрыв лавы при убийстве врага", Rgb(255, 100, 35));
            AddItem("harvester_scythe", "Коса Жнеца", "SCYTH", "green", "+5% крита и +6 HP при крит. ударе", Rgb(85, 245, 175));
            AddItem("predatory", "Хищные Инстинкты", "PRED", "green", "Криты ускоряют атаку на +12% (3с)", Rgb(245, 110, 85));
            AddItem("bandolier", "Патронташ", "BAND", "green", "Убийства сокращают КД всех навыков", Rgb(225, 190, 95));
            AddItem("kjaro_band", "Кольцо Кьяро", "KJARO", "green", "Сильные удары создают огненный вихрь", Rgb(255, 115, 30));
            AddItem("runald_band", "Кольцо Рунальда", "RNALD", "green", "Сильные удары замораживают цель", Rgb(85, 225, 255));
            AddItem("wax_quill", "Восковое Перо", "QUILL", "green", "Прыжок совершает рывок вперёд", Rgb(255, 215, 120));
            AddItem("rose_buckler", "Розовый Баклер", "BUCKL", "green", "-15% урона во время движения", Rgb(245, 125, 175));
            AddItem("leeching_seed", "Кровожадное Семя", "SEED", "green", "Попадания лечат на +1.5 HP", Rgb(145, 255, 95));
            AddItem("infusion", "Инфузия", "INFUS", "green", "Убийства увеличивают макс. HP (+1)", Rgb(235, 45, 70));
            AddItem("red_whip", "Красный Кнут", "WHIP", "green", "+12% к базовой скорости бега", Rgb(245, 75, 65));
            AddItem("chronobauble", "Хронобрелок", "CHRON", "green", "Атаки замедляют врагов на 35%", Rgb(195, 120, 255));
            AddItem("razorwire", "Колючая Проволока", "RAZOR", "green", "При получении урона выпускает шипы в 5 врагов вокруг", Rgb(185, 235, 115));
            AddItem("guillotine", "Старая Гильотина", "GUILL", "green", "Мгновенно казнит обычных врагов с HP ниже 6.5% за стак (не действует на Боссов)", Rgb(210, 225, 245));

            // Легендарные (Red)
            AddItem("behemoth", "Блистательный Бегемот", "BEHEM", "red", "Все атаки взрываются (+2.5м AoE, +25% ур.)", Rgb(255, 60, 40));
            AddItem("clover", "57-Листный Клевер", "CLOVR", "red", "Удача: +7% крита и +12% ко всем шансам", Rgb(75, 255, 135));
            AddItem("tarrasque", "Сердце Тарраска", "TARR", "red", "+100 к макс. HP и мощная регенерация (+2.5% макс. HP в секунду)", Rgb(245, 55, 75));
            AddItem("aegis", "Эгида", "AEGIS", "red", "Избыточное лечение сверх максимума превращается во временный щит", Rgb(255, 215, 75));
            AddItem("tesla_coil", "Нестабильная Катушка Теслы", "TESLA", "red", "Каждые 1.2с автоматически бьёт молнией ближайших врагов", Rgb(110, 225, 255));
            AddItem("ceremonial_dagger", "Церемониальный Кинжал", "DAGGR", "red", "Убийство врага выпускает 3 самонаводящихся спектральных кинжала", Rgb(255, 75, 110));
            AddItem("frost_relic", "Ледяная Реликвия", "FROST", "red", "Убийства создают вокруг вас ледяной вихрь на 4 сек.", Rgb(125, 240, 255));
            AddItem("shattering_justice", "Сокрушительное Правосудие", "JUSTC", "red", "25% шанс при атаке расколоть броню врага на 4 сек., увеличивая урон по нему на +20%", Rgb(255, 165, 55));
            AddItem("alien_head", "Голова Пришельца", "ALIEN", "red", "-20% времени перезарядки всех способностей", Rgb(140, 255, 165));
            AddItem("afterburner", "Твердосветовой Форсаж", "BURN", "red", "-30% перезарядки навыка Shift и +35% скорости рывка", Rgb(255, 110, 55));

            // Пустота (Void)
            AddItem("plasma_shrimp", "Плазменная Креветка", "SHRM", "void", "[ПУСТОТА] Пока есть щит (+20 щита), каждая атака пускает ракету (40% ур.)", Rgb(195, 95, 255), isVoid: true);
            AddItem("polylute", "Полилютня Бездны", "PLUTE", "void", "[ПУСТОТА] 25% шанс обрушить на цель серию из 3 ударов молнии Бездны", Rgb(215, 75, 255), isVoid: true);
            AddItem("safer_spaces", "Сфера Бездны", "SPACE", "void", "[ПУСТОТА] Гарантированно блокирует любой удар раз в 12 сек. (-15% КД за стак)", Rgb(180, 90, 255), isVoid: true);
            AddItem("lysate_cell", "Клетка Бездны", "CELL", "void", "[ПУСТОТА] Сокращает перезарядку Ультимейта [R] на 25% за стак", Rgb(225, 110, 255), isVoid: true);

            // Лунные (Lunar)
            AddItem("shaped_glass", "Лунное Стекло", "GLASS", "lunar", "[ЛУННЫЙ] Урон x2 (+100%), но макс. HP снижено на 50%", Rgb(70, 175, 255), isLunar: true);
            AddItem("transcendence", "Жук Превосходства", "TRANS", "lunar", "[ЛУННЫЙ] +50% макс. HP и мощный регенерирующий щит", Rgb(65, 195, 255), isLunar: true);
            AddItem("purity", "Чистота", "PURE", "lunar", "[ЛУННЫЙ] Перезарядка всех навыков -2 сек., но удача ниже (-25% к шансам проков)", Rgb(140, 225, 255), isLunar: true);
            AddItem("lunar_pauldron", "Каменный Наплечник", "FLUX", "lunar", "[ЛУННЫЙ] Макс. HP +100% (x2), но скорость бега -30%", Rgb(90, 160, 255), isLunar: true);
            AddItem("spinel_tonic", "Лунный Тоник", "TONIC", "lunar", "[ЛУННЫЙ] На 25с все статы +25% (очищает старые тоники), затем портится!", Rgb(55, 140, 255), isLunar: true);
            AddItem("tonic_affliction", "Испорченный Тоник", "AFLT", "lunar", "[ПРОКЛЯТИЕ ТОНИКА] Все характеристики (-Урон, -HP, -Атака, -Бег) снижены на 12% за стак", Rgb(95, 70, 135), isLunar: false);

            foreach (var t in TierMap.Keys)
            {
                ItemsByTier[t] = ItemPool.Values.Where(i => i.Tier == t && i.Key != "tonic_affliction").Select(i => i.Key).ToList();
            }
        }

        private static void AddItem(string key, string name, string shortCode, string tier, string desc, Color meshCol, bool isVoid = false, bool isLunar = false)
        {
            var tInfo = TierMap[tier];
            var item = new ItemInfo
            {
                Key = key, Name = name, Short = shortCode, Tier = tier,
                TierName = tInfo.Name, Color = tInfo.Color, MeshColor = meshCol,
                Desc = desc, DefaultUnlocked = true, IsVoid = isVoid, IsLunar = isLunar
            };
            ItemPool[key] = item;
            if (isVoid) VoidItems[key] = item;
            if (isLunar) LunarItems[key] = item;
        }

        public static string RollItemFromPool(string poolType = "small_chest")
        {
            double r = _rng.NextDouble();
            string tier;
            if (poolType == "small_chest")
                tier = r < 0.855 ? "white" : (r < 0.995 ? "green" : "red");
            else if (poolType == "large_chest")
                tier = r < 0.90 ? "green" : "red";
            else if (poolType == "legendary_chest")
                tier = "red";
            else if (poolType == "boss")
                tier = r < 0.85 ? "green" : "red";
            else if (poolType == "lunar_pod" || poolType == "bazaar")
                tier = r < 0.55 ? "lunar" : "void";
            else if (ItemsByTier.ContainsKey(poolType))
                tier = poolType;
            else
                tier = "white";

            var list = ItemsByTier.TryGetValue(tier, out var cands) && cands.Count > 0 ? cands : ItemsByTier["white"];
            return list[_rng.Next(list.Count)];
        }
    }
}