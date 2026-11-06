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

        public string GetAbbreviatedDesc(int maxLen = 58)
        {
            if (string.IsNullOrWhiteSpace(Desc)) return "";
            string s = Desc.Replace("\r", "").Replace("\n", " ").Trim();
            int dot = s.IndexOf('.');
            if (dot > 10 && dot <= maxLen + 8)
                s = s[..(dot + 1)];
            if (s.Length > maxLen)
                s = s[..(maxLen - 2)].TrimEnd() + "..";
            return s;
        }
    }

    public class AchievementInfo
    {
        public string Key = "";
        public string Name = "";
        public string Desc = "";
        public string? UnlocksItem;
        public string? UnlocksCharacter;
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

    public class RunArtifactDef
    {
        public string Key = "";
        public string Name = "";
        public string Short = "";
        public string Desc = "";
        public Color Color;
        public string Subtitle => $"Модификатор забега [{Short}]";
    }

    public class DifficultyDef
    {
        public string Key = "";
        public string Name = "";
        public string Short = "";
        public float ScaleMult = 1.0f;
        public float RegenMult = 1.0f;
        public float PlayerDmgTakenMult = 1.0f;
        public float EnemySpeedMult = 1.0f;
        public float EnemyHpMult = 1.0f;
        public float SpawnRateMult = 1.0f;
        public float MinSpawnInterval = 1.35f;
        public float MaxSpawnInterval = 2.40f;
        public int MaxEnemiesBase = 30;
        public float BaseEliteChance = 0.06f;
        public float StartCoeffBonus = 0.0f;
        public float BossDamageCapRatio = 0.14f;
        public float BossArmorFloor = 0.48f;
        public string Desc = "";
        public Color Color;
        public string Icon => Short;
        public string Subtitle => $"Режим сложности [{Short}]";
    }

    public static class GameData
    {
        private static readonly Random _rng = new();

        public static string SelectedDifficulty = "rainstorm";
        public static readonly HashSet<string> ActiveArtifacts = new();

        private static bool _hasNetClientOverride = false;
        private static string _netClientDifficulty = "rainstorm";
        private static readonly HashSet<string> _netClientArtifacts = new();

        public static string EffectiveDifficultyKey => _hasNetClientOverride ? _netClientDifficulty : SelectedDifficulty;
        public static IReadOnlyCollection<string> EffectiveArtifacts => _hasNetClientOverride ? _netClientArtifacts : ActiveArtifacts;

        public static void SetNetClientOverride(string? diff, IEnumerable<string>? artifacts)
        {
            _hasNetClientOverride = true;
            if (!string.IsNullOrEmpty(diff) && Difficulties.ContainsKey(diff))
                _netClientDifficulty = diff;
            else
                _netClientDifficulty = SelectedDifficulty;

            _netClientArtifacts.Clear();
            if (artifacts != null)
            {
                foreach (var ak in artifacts)
                {
                    if (!string.IsNullOrWhiteSpace(ak) && RunArtifacts.ContainsKey(ak))
                        _netClientArtifacts.Add(ak);
                }
            }
        }

        public static void ClearNetClientOverride()
        {
            _hasNetClientOverride = false;
            _netClientArtifacts.Clear();
        }

        public static bool HasArtifact(string key)
            => _hasNetClientOverride ? _netClientArtifacts.Contains(key) : ActiveArtifacts.Contains(key);

        public static DifficultyDef GetActiveDifficulty()
            => Difficulties.TryGetValue(EffectiveDifficultyKey, out var d) ? d : Difficulties["rainstorm"];

        public static readonly string[] OrderedDifficultyKeys = { "drizzle", "rainstorm", "monsoon", "eclipse" };

        public static readonly Dictionary<string, DifficultyDef> Difficulties = new()
        {
            ["drizzle"] = new()
            {
                Key = "drizzle",
                Name = "МОРОСЬ",
                Short = "ЛЕГКО",
                ScaleMult = 0.55f,
                RegenMult = 2.40f,
                PlayerDmgTakenMult = 0.68f,
                EnemySpeedMult = 0.88f,
                EnemyHpMult = 0.72f,
                SpawnRateMult = 0.65f,
                MinSpawnInterval = 2.10f,
                MaxSpawnInterval = 3.50f,
                MaxEnemiesBase = 20,
                BaseEliteChance = 0.015f,
                StartCoeffBonus = 0.0f,
                BossDamageCapRatio = 0.20f,
                BossArmorFloor = 0.66f,
                Color = Rgb(85, 235, 105),
                Desc = "Комфортное освоение: ОЗ врагов -28%, темп спавна волн -35%, рост сложности x0.55, реген +140%, урон врагов -32%."
            },
            ["rainstorm"] = new()
            {
                Key = "rainstorm",
                Name = "ЛИВЕНЬ",
                Short = "НОРМА",
                ScaleMult = 1.00f,
                RegenMult = 1.00f,
                PlayerDmgTakenMult = 1.00f,
                EnemySpeedMult = 1.00f,
                EnemyHpMult = 1.00f,
                SpawnRateMult = 1.00f,
                MinSpawnInterval = 1.35f,
                MaxSpawnInterval = 2.40f,
                MaxEnemiesBase = 30,
                BaseEliteChance = 0.06f,
                StartCoeffBonus = 0.0f,
                BossDamageCapRatio = 0.14f,
                BossArmorFloor = 0.48f,
                Color = Rgb(255, 195, 55),
                Desc = "Эталонный баланс Petrichor V: честная живучесть врагов (100%), умеренные волны и плавный рост элитных монстров."
            },
            ["monsoon"] = new()
            {
                Key = "monsoon",
                Name = "МУССОН",
                Short = "СЛОЖНО",
                ScaleMult = 1.55f,
                RegenMult = 0.60f,
                PlayerDmgTakenMult = 1.24f,
                EnemySpeedMult = 1.12f,
                EnemyHpMult = 1.30f,
                SpawnRateMult = 1.40f,
                MinSpawnInterval = 0.90f,
                MaxSpawnInterval = 1.65f,
                MaxEnemiesBase = 42,
                BaseEliteChance = 0.14f,
                StartCoeffBonus = 0.15f,
                BossDamageCapRatio = 0.11f,
                BossArmorFloor = 0.40f,
                Color = Rgb(255, 65, 75),
                Desc = "Суровое испытание: ОЗ врагов +30%, плотные волны (+40%), частые элитные монстры, реген -40%, рост сложности x1.55."
            },
            ["eclipse"] = new()
            {
                Key = "eclipse",
                Name = "ЗАТМЕНИЕ VIII",
                Short = "КОШМАР",
                ScaleMult = 2.05f,
                RegenMult = 0.50f,
                PlayerDmgTakenMult = 1.42f,
                EnemySpeedMult = 1.20f,
                EnemyHpMult = 1.58f,
                SpawnRateMult = 1.75f,
                MinSpawnInterval = 0.68f,
                MaxSpawnInterval = 1.25f,
                MaxEnemiesBase = 54,
                BaseEliteChance = 0.24f,
                StartCoeffBonus = 0.30f,
                BossDamageCapRatio = 0.095f,
                BossArmorFloor = 0.36f,
                Color = Rgb(195, 85, 255),
                Desc = "Абсолютный предел: ОЗ врагов +58%, непрерывный натиск орд и элит, старт с 80% HP, всё исцеление -35%!"
            }
        };

        public static readonly string[] OrderedArtifactKeys =
        {
            "cmd", "swarm", "glass", "kin", "vengeance",
            "sacrifice", "honor", "chaos", "dissonance", "evolution"
        };

        public static readonly Dictionary<string, RunArtifactDef> RunArtifacts = new()
        {
            ["cmd"] = new()
            {
                Key = "cmd",
                Name = "Артефакт Командования",
                Short = "CMD",
                Color = Rgb(255, 215, 65),
                Desc = "Выбирайте свои предметы! При подборе любого трофея открывается окно выбора любого предмета этой редкости."
            },
            ["swarm"] = new()
            {
                Key = "swarm",
                Name = "Артефакт Роя",
                Short = "SWRM",
                Color = Rgb(115, 255, 95),
                Desc = "Количество появляющихся монстров удвоено (x2), но их максимальное здоровье снижено вдвое (-50%)."
            },
            ["glass"] = new()
            {
                Key = "glass",
                Name = "Артефакт Стекла",
                Short = "GLAS",
                Color = Rgb(95, 225, 255),
                Desc = "Выжившие наносят на 400% больше урона (x5.0!), но имеют лишь 20% от своего максимального здоровья."
            },
            ["kin"] = new()
            {
                Key = "kin",
                Name = "Артефакт Родства",
                Short = "KIN",
                Color = Rgb(255, 165, 60),
                Desc = "На каждом этапе появляется только один случайный вид обычных монстров (объявляется в начале локации)."
            },
            ["vengeance"] = new()
            {
                Key = "vengeance",
                Name = "Артефакт Мести",
                Short = "VENG",
                Color = Rgb(215, 75, 255),
                Desc = "Каждые 85 секунд в мир вторгается ваш неумолимый теневой Доппельгангер, роняющий предмет при смерти."
            },
            ["sacrifice"] = new()
            {
                Key = "sacrifice",
                Name = "Артефакт Жертвы",
                Short = "SACR",
                Color = Rgb(245, 55, 75),
                Desc = "Обычные сундуки исчезают с этапов, но поверженные монстры с шансом напрямую роняют предметы."
            },
            ["honor"] = new()
            {
                Key = "honor",
                Name = "Артефакт Чести",
                Short = "HONR",
                Color = Rgb(255, 125, 45),
                Desc = "Все враги рождаются в пылающей Элитной форме (+35% HP, +25% урона, элитная корона и +40% золота)."
            },
            ["chaos"] = new()
            {
                Key = "chaos",
                Name = "Артефакт Хаоса",
                Short = "CHAO",
                Color = Rgb(255, 85, 195),
                Desc = "Тотальная детонация и дружественный огонь: радиус всех взрывов +40%, но близкие взрывы обжигают и стрелка."
            },
            ["dissonance"] = new()
            {
                Key = "dissonance",
                Name = "Артефакт Диссонанса",
                Short = "DISS",
                Color = Rgb(135, 195, 255),
                Desc = "Монстры появляются вне своей естественной среды: Лунные Химеры, Прародители и Големы встречаются на любом этапе."
            },
            ["evolution"] = new()
            {
                Key = "evolution",
                Name = "Артефакт Эволюции",
                Short = "EVOL",
                Color = Rgb(85, 245, 205),
                Desc = "Монстры эволюционируют и получают усиления артефактов с каждым новым этапом (+скорость, +атака, +броня, +реген)."
            }
        };

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

        public static readonly string[] BossTypes =
        {
            "boss", "vagrant", "overlord", "magma_worm", "dunestrider", "solus_control",
            "echo_providence", "false_son_boss", "prts_source", "binah_arbiter", "roaring_knight", "great_septimus"
        };
        public static readonly string[] TrialBossTypes = { "zurg_mech", "justice_gear", "shadow_reaper", "vergil_mirage", "animatronic_endo", "warden_reef" };

        public static bool IsKnownBossType(string? t)
            => !string.IsNullOrEmpty(t) && (Array.IndexOf(BossTypes, t) >= 0 || Array.IndexOf(TrialBossTypes, t) >= 0 || t == "mithrix");

        public static readonly Dictionary<string, string> BossTitles = new()
        {
            ["boss"] = "КАМЕННЫЙ ТИТАН :: ИЗНАЧАЛЬНЫЙ СТРАЖ",
            ["vagrant"] = "БЛУЖДАЮЩИЙ БРОДЯГА :: НЕЖНЫЙ ЗАЩИТНИК",
            ["overlord"] = "ПОВЕЛИТЕЛЬ БЕСОВ :: ВЛАДЫКА КРАСНОЙ ПЛОСКОСТИ",
            ["magma_worm"] = "МАГМОВЫЙ ЧЕРВЬ :: ИСПОЛИН ПРЕИСПОДНЕЙ",
            ["dunestrider"] = "ПЕСЧАНЫЙ СТРАННИК :: СОСУД ДЕГТЯ",
            ["solus_control"] = "МОДУЛЬ СОЛУС :: АВТОНОМНЫЙ ПРОТОКОЛ",
            ["echo_providence"] = "ИСКАЖЁННЫЙ ПРОВИДЕЦ :: ОПЛОТ СЛАБЫХ",
            ["false_son_boss"] = "ЛОЖНЫЙ СЫН :: ОСКВЕРНЁННЫЙ НАСЛЕДНИК МЕРИДИАНА",
            ["prts_source"] = "PRTS, ИСХОДНЫЙ КОД :: КОВЧЕГ ИЗНАЧАЛЬНОЙ ПРОГРАММЫ",
            ["binah_arbiter"] = "АРБИТР БИНА :: ОКО ГЛАВЫ И СТОЛПЫ ИСКУПЛЕНИЯ",
            ["roaring_knight"] = "РОКОЧУЩИЙ РЫЦАРЬ :: РАССЕКАТЕЛЬ ТЁМНЫХ ФОНТАНОВ",
            ["great_septimus"] = "ВЕЛИКИЙ СЕПТИМИЙ :: ХОР БАЛАНСА И ЭМБРИОН ФИЛОСОФИИ",
            ["mithrix"] = "МИТРИКС :: КОРОЛЬ НИЧЕГО",
            ["zurg_mech"] = "ДРЕДНОУТ ЗУРГА :: ФЛАГМАН ГАЛАКТИЧЕСКОЙ ИМПЕРИИ",
            ["justice_gear"] = "ПРОТОТИП «ДЖАСТИС» :: КОМАНДНЫЙ МЕХАНИЗМ РАЗРУШЕНИЯ",
            ["shadow_reaper"] = "ЖНЕЦ ТАРТАРА :: ПАЛАЧ ТЁМНОГО ЧАСА",
            ["vergil_mirage"] = "МИРАЖ ВЕРГИЛИЯ :: АЛЬФА И ОМЕГА",
            ["animatronic_endo"] = "ЗОЛОТОЙ ЭНДОСКЕЛЕТ 06:00 :: ПРОКЛЯТЫЙ МЕХАНИЗМ",
            ["warden_reef"] = "НАДЗИРАТЕЛЬ ТЁМНОГО РИФА :: ХРАНИТЕЛЬ ОКОВ"
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
            ["hell_drone"] = new() { Key = "hell_drone", Name = "Адский Дрон", Hp = 92, Damage = 16.5f, Desc = "Скоростной кроваво-механический разведчик Кибер-Грайнда. Крушит жертву очередями адских сфер и пикирует при гибели." },
            ["combine_soldier"] = new() { Key = "combine_soldier", Name = "Штурмовик Альянса", Hp = 135, Damage = 18.5f, Desc = "Элитный трансчеловеческий солдат Надзора в противогазе с голубыми окулярами. Ведёт подавляющий импульсный огонь." },
            ["sarkaz_mercenary"] = new() { Key = "sarkaz_mercenary", Name = "Наёмник Сарказов", Hp = 160, Damage = 21.0f, Desc = "Закалённый рогатый воин Каздела с раскалённым двуручным тесаком Ориджиниума. Стремительно сокращает дистанцию рывком." },
            ["hollow_shadow"] = new() { Key = "hollow_shadow", Name = "Арканная Тень", Hp = 130, Damage = 19.0f, Desc = "Порождение Тёмного Часа в золотой маске Арканы. Скользит сквозь землю и выпускает проклятые сгустки тьмы." },
            ["nether_blaze"] = new() { Key = "nether_blaze", Name = "Ифрит Незера", Hp = 120, Damage = 19.5f, Desc = "Огненный элементаль адской крепости, окружённый вихрем раскалённых золотых стержней. Стреляет тройными залпами огня." },
            ["boss"] = new() { Key = "boss", Name = "Каменный Титан", Hp = 2100, Damage = 32, Desc = "Колоссальный страж Телепорта. Наводит лазерный луч и вызывает каменные разломы из-под земли." },
            ["vagrant"] = new() { Key = "vagrant", Name = "Блуждающий Бродяга", Hp = 2250, Damage = 30, Desc = "Гигантская небесная медуза. Сжимает купол перед залпом сфер и заряжает ослепительную сверхновую." },
            ["overlord"] = new() { Key = "overlord", Name = "Повелитель Бесов", Hp = 2400, Damage = 34, Desc = "Владыка демонов Пустоты. Обрушивается сверху в отмеченные рунные круги и мечет веера шипов." },
            ["magma_worm"] = new() { Key = "magma_worm", Name = "Магмовый Червь", Hp = 2600, Damage = 35, Desc = "Исполинский змей из раскалённой магмы. Прорывает землю в огненных кольцах и разбрасывает лаву." },
            ["dunestrider"] = new() { Key = "dunestrider", Name = "Песчаный Странник", Hp = 2350, Damage = 31, Desc = "Шагающий глиняный сосуд. Катит дегтярные бомбы и создаёт воронку, притягивающую выживших." },
            ["solus_control"] = new() { Key = "solus_control", Name = "Модуль Управления Солус", Hp = 2300, Damage = 32, Desc = "Боевое ядро ИИ. Фокусирует прицельные залпы и подбрасывает цели гравитационными кольцами." },
            ["zurg_mech"] = new() { Key = "zurg_mech", Name = "Дредноут Императора Зурга", Hp = 1850, Damage = 29, Desc = "Тяжёлый боевой экзокостюм Императора Зурга с тройным ионным бластером и гравитационным плащом." },
            ["justice_gear"] = new() { Key = "justice_gear", Name = "Прототип «Джастис»", Hp = 2150, Damage = 33, Desc = "Биомеханический Командный Механизм в белой броне с крыльями гамма-лучей и ядерными импульсами." },
            ["shadow_reaper"] = new() { Key = "shadow_reaper", Name = "Жнец Тартара", Hp = 2050, Damage = 32, Desc = "Неумолимый палач лабиринта Тартара в оковах и плаще, вооружённый парными крупнокалиберными револьверами." },
            ["vergil_mirage"] = new() { Key = "vergil_mirage", Name = "Мираж Вергилия", Hp = 2200, Damage = 34, Desc = "Призрачное воплощение Тёмного Убийцы с Ямато. Совершает мгновенные разрезы пространства и призывает Призрачные Клинки." },
            ["animatronic_endo"] = new() { Key = "animatronic_endo", Name = "Золотой Эндоскелет 06:00", Hp = 1900, Damage = 30, Desc = "Одержимый стальной гигант ночной пиццерии с искрящими сервоприводами и оглушающим звуковым скримером." },
            ["warden_reef"] = new() { Key = "warden_reef", Name = "Надзиратель Тёмного Рифа", Hp = 1950, Damage = 31, Desc = "Древний глубоководный страж тюрьмы Тёмного Рифа с гарпунными цепями и сокрушительными приливными волнами." },
            ["echo_providence"] = new() { Key = "echo_providence", Name = "Искажённый Провидец", Hp = 2550, Damage = 35, Desc = "Призрачное эхо хранителя Petrichor V в мантии цвета фуксии. Рассекает пространство теневыми двойниками и призывает мечи правосудия." },
            ["false_son_boss"] = new() { Key = "false_son_boss", Name = "Ложный Сын (Босс Меридиана)", Hp = 2700, Damage = 36, Desc = "Осквернённый лунным проклятием колосс Провиденса и Аурелионита. Накладывает Лунную Погибель и выжигает арену Лучём Отца." },
            ["prts_source"] = new() { Key = "prts_source", Name = "PRTS, Исходный Код", Hp = 2450, Damage = 33, Desc = "Изначальный нейросетевой монолит «Родос Айленд». Укрывается за Машиной-Мимиком, искажает интерфейс и выжигает сектора красными ромбами ◇." },
            ["binah_arbiter"] = new() { Key = "binah_arbiter", Name = "Арбитр Бина", Hp = 2650, Damage = 36, Desc = "Неумолимый Арбитр Главы в чёрно-золотом плаще. Пронзает арену Столпами, сковывает золотыми цепями Замка и обрушивает Ударную Волну." },
            ["roaring_knight"] = new() { Key = "roaring_knight", Name = "Рокочущий Рыцарь", Hp = 2500, Damage = 35, Desc = "Безмолвный создатель Тёмных Фонтанов в рогатом шлеме с ослепительно белым визором. Ведёт обстрел парящими мечами и разрубает реальность пополам." },
            ["great_septimus"] = new() { Key = "great_septimus", Name = "Великий Септимий, Хор Баланса", Hp = 2850, Damage = 34, Desc = "Исполинский златокрылый дирижёр Пенаконии. Повелевает эхом Хора и исполняет симфонию Сотворения за Семь Дней." },
            ["mithrix"] = new() { Key = "mithrix", Name = "Митрикс, Король Ничего", Hp = 3600, Damage = 38, Desc = "Повелитель Луны. Сражается в 4 фазах гигантским молотом с читаемыми замахами и крадёт предметы." }
        };

        // =========================================================================================
        // 76 ДОСТИЖЕНИЙ: 52 ПРЕДМЕТА + 24 УНИКАЛЬНЫХ ИСПЫТАНИЯ ПЕРСОНАЖЕЙ
        // =========================================================================================
        public static readonly Dictionary<string, AchievementInfo> Achievements = new()
        {
            // --- БАЗОВЫЕ 9 ДОСТИЖЕНИЙ (ОТКРЫВАЮТ ПРЕДМЕТЫ + АЛЬТ. НАВЫКИ И СКИНЫ АКТИВНОГО ГЕРОЯ) ---
            ["ach_slayer"] = new() { Key = "ach_slayer", Name = "Истребитель", Desc = "Уничтожьте 12 монстров за один забег.", UnlocksItem = "lens", RewardText = "Предмет: Очки Часовщика + Альт. навык [ЛКМ] текущего героя" },
            ["ach_boss"] = new() { Key = "ach_boss", Name = "Убийца Титанов", Desc = "Победите Босса Телепорта.", UnlocksItem = "feather", RewardText = "Предмет: Перо Хопу + Альт. навык [ПКМ] текущего героя" },
            ["ach_traveler"] = new() { Key = "ach_traveler", Name = "Исследователь миров", Desc = "Перейдите на 2-й этап через Телепорт.", UnlocksItem = "ukulele", RewardText = "Предмет: Укулеле + Альт. навык [Shift] текущего героя" },
            ["ach_hoarder"] = new() { Key = "ach_hoarder", Name = "Коллекционер артефактов", Desc = "Соберите 6 предметов за один забег.", UnlocksItem = "atg_missile", RewardText = "Предмет: Ракетница AtG Mk. 1 + Альт. навык [R] текущего героя" },
            ["ach_stage3"] = new() { Key = "ach_stage3", Name = "Покоритель Глубин", Desc = "Доберитесь до 3-го этапа за один забег.", UnlocksItem = "sheen", RewardText = "Предмет: Сияние + Альтернативный скин текущего героя" },
            ["ach_carnage"] = new() { Key = "ach_carnage", Name = "Кровавая Жатва", Desc = "Уничтожьте 25 врагов за один забег.", UnlocksItem = "blade_ruined_king", RewardText = "Предмет: Клинок Падшего Короля + Мастер-скин текущего героя" },
            ["ach_boss_hunter"] = new() { Key = "ach_boss_hunter", Name = "Гроза Стражей", Desc = "Победите 2 Боссов за один забег.", UnlocksItem = "infinity_edge", RewardText = "Предмет: Грань Бесконечности + Мастер-скин текущего героя" },
            ["ach_arsenal"] = new() { Key = "ach_arsenal", Name = "Вооружён до зубов", Desc = "Соберите 12 предметов за один забег.", UnlocksItem = "rabadons_deathcap", RewardText = "Предмет: Шляпа Рабадона + Мастер-скин текущего героя" },
            ["ach_stage4"] = new() { Key = "ach_stage4", Name = "Ветеран Petrichor V", Desc = "Доберитесь до 4-го этапа или победите Митрикса.", UnlocksItem = "zhonyas_hourglass", RewardText = "Предмет: Песочные Часы Жони + Мастер-скин текущего героя" },

            // --- ОСТАЛЬНЫЕ 18 БЕЛЫХ ПРЕДМЕТОВ (4 БЕЛЫХ ОТКРЫТЫ СО СТАРТА: syringe, hoof, bear, gasoline) ---
            ["ach_first_strike"] = new() { Key = "ach_first_strike", Name = "Первопроходец лома", Desc = "Нанесите более 90 ед. урона одним ударом.", UnlocksItem = "crowbar", RewardText = "Предмет: Монтировка" },
            ["ach_bleeder"] = new() { Key = "ach_bleeder", Name = "Острые грани", Desc = "Совершите 35 атак базовым оружием [ЛКМ] за один забег.", UnlocksItem = "tri_tip", RewardText = "Предмет: Трёхгранный Кинжал" },
            ["ach_giant_killer"] = new() { Key = "ach_giant_killer", Name = "Крупный калибр", Desc = "Нанесите 1 200 ед. урона Боссам за один забег.", UnlocksItem = "ap_rounds", RewardText = "Предмет: Бронебойные Патроны" },
            ["ach_secondary_master"] = new() { Key = "ach_secondary_master", Name = "Тактический резерв", Desc = "Используйте альтернативную атаку [ПКМ] 15 раз за один забег.", UnlocksItem = "backup_mag", RewardText = "Предмет: Запасной Магазин" },
            ["ach_marathon"] = new() { Key = "ach_marathon", Name = "Марафонец", Desc = "Непрерывно бегите и маневрируйте 20 секунд подряд без остановки.", UnlocksItem = "energy_drink", RewardText = "Предмет: Энергетик" },
            ["ach_bison_hunt"] = new() { Key = "ach_bison_hunt", Name = "Охота на бизона", Desc = "Достигните 180 ед. максимального здоровья.", UnlocksItem = "bison_steak", RewardText = "Предмет: Стейк из Бизона" },
            ["ach_trophies"] = new() { Key = "ach_trophies", Name = "Трофейный клык", Desc = "Уничтожьте 8 монстров на первом этапе.", UnlocksItem = "monster_tooth", RewardText = "Предмет: Клык Монстра" },
            ["ach_field_medic"] = new() { Key = "ach_field_medic", Name = "Первая помощь", Desc = "Выживите после падения здоровья ниже 40% HP и восстановите его выше 85%.", UnlocksItem = "medkit", RewardText = "Предмет: Аптечка" },
            ["ach_tank_hits"] = new() { Key = "ach_tank_hits", Name = "Железная выдержка", Desc = "Переживите 12 вражеских попаданий за один забег.", UnlocksItem = "repulsion_armor", RewardText = "Предмет: Бронепластина Отражения" },
            ["ach_shield_bearer"] = new() { Key = "ach_shield_bearer", Name = "Золотой барьер", Desc = "Уничтожьте 4 врагов подряд в одной непрерывной серии убийств.", UnlocksItem = "topaz_brooch", RewardText = "Предмет: Топазовая Брошь" },
            ["ach_untouched"] = new() { Key = "ach_untouched", Name = "Кристальная защита", Desc = "Проведите 35 секунд в бою, не получая урона.", UnlocksItem = "opal", RewardText = "Предмет: Причудливый Опал" },
            ["ach_close_combat"] = new() { Key = "ach_close_combat", Name = "В упор", Desc = "Уничтожьте 10 врагов на дистанции ближе 13 метров.", UnlocksItem = "focus_crystal", RewardText = "Предмет: Фокусирующий Кристалл" },
            ["ach_demolition"] = new() { Key = "ach_demolition", Name = "Взрывотехник", Desc = "Уничтожьте 3 врагов подряд за короткое окно серии (5 сек).", UnlocksItem = "sticky_bomb", RewardText = "Предмет: Липкая Бомба" },
            ["ach_shock_tactics"] = new() { Key = "ach_shock_tactics", Name = "Шоковая терапия", Desc = "Победите 3 Элитных монстров за один забег.", UnlocksItem = "stun_grenade", RewardText = "Предмет: Светошумовая Граната" },
            ["ach_chest_opener"] = new() { Key = "ach_chest_opener", Name = "Праздничный салют", Desc = "Откройте 4 сундука или святилища за один забег.", UnlocksItem = "bundle_fireworks", RewardText = "Предмет: Связка Фейерверков" },
            ["ach_stationary"] = new() { Key = "ach_stationary", Name = "Грибная поляна", Desc = "Проведите суммарно 12 секунд стоя неподвижно на этапе.", UnlocksItem = "bungus", RewardText = "Предмет: Неугомонный Гриб" },
            ["ach_doran_start"] = new() { Key = "ach_doran_start", Name = "Стартовый закуп", Desc = "Накопите 120 золота на счету.", UnlocksItem = "dorans_blade", RewardText = "Предмет: Клинок Дорана" },
            ["ach_wind_runner"] = new() { Key = "ach_wind_runner", Name = "Быстрее ветра", Desc = "Активируйте Телепорт в первые 3 минуты этапа.", UnlocksItem = "boots_swiftness", RewardText = "Предмет: Ботинки Стремительности" },

            // --- ОСТАЛЬНЫЕ 14 ЗЕЛЁНЫХ ПРЕДМЕТОВ (3 ЗЕЛЁНЫХ ОТКРЫТЫ СО СТАРТА: leeching_seed, infusion, red_whip) ---
            ["ach_wisp_hunter"] = new() { Key = "ach_wisp_hunter", Name = "Дух пламени", Desc = "Уничтожьте 10 Огоньков или Лемурийцев за забег.", UnlocksItem = "will_o_wisp", RewardText = "Предмет: Блуждающий Огонёк в Банке" },
            ["ach_crit_reaper"] = new() { Key = "ach_crit_reaper", Name = "Жатва душ", Desc = "Нанесите 10 критических ударов за один забег.", UnlocksItem = "harvester_scythe", RewardText = "Предмет: Коса Жнеца" },
            ["ach_frenzy"] = new() { Key = "ach_frenzy", Name = "Хищное бешенство", Desc = "Уничтожьте 6 врагов за 12 секунд.", UnlocksItem = "predatory", RewardText = "Предмет: Хищные Инстинкты" },
            ["ach_skill_spam"] = new() { Key = "ach_skill_spam", Name = "Бесконечный боезапас", Desc = "Используйте Ультимейт [R] 5 раз за один забег.", UnlocksItem = "bandolier", RewardText = "Предмет: Патронташ" },
            ["ach_fire_band"] = new() { Key = "ach_fire_band", Name = "Пламя Кьяро", Desc = "Нанесите более 220 ед. урона одним ударом или способностью.", UnlocksItem = "kjaro_band", RewardText = "Предмет: Кольцо Кьяро" },
            ["ach_ice_band"] = new() { Key = "ach_ice_band", Name = "Лёд Рунальда", Desc = "Победите Босса Телепорта быстрее чем за 45 секунд.", UnlocksItem = "runald_band", RewardText = "Предмет: Кольцо Рунальда" },
            ["ach_airborne"] = new() { Key = "ach_airborne", Name = "Прыжок Икара", Desc = "Уничтожьте 6 врагов, находясь в прыжке или полёте.", UnlocksItem = "wax_quill", RewardText = "Предмет: Восковое Перо" },
            ["ach_combat_dash"] = new() { Key = "ach_combat_dash", Name = "Танцующий с клинками", Desc = "Используйте навык движения [Shift] 12 раз за один забег.", UnlocksItem = "rose_buckler", RewardText = "Предмет: Розовый Баклер" },
            ["ach_time_keeper"] = new() { Key = "ach_time_keeper", Name = "Хранитель времени", Desc = "Продержитесь в забеге 5 минут.", UnlocksItem = "chronobauble", RewardText = "Предмет: Хронобрелок" },
            ["ach_thorns"] = new() { Key = "ach_thorns", Name = "Ответный удар", Desc = "Уничтожьте 5 врагов в течение 4 секунд после получения урона.", UnlocksItem = "razorwire", RewardText = "Предмет: Колючая Проволока" },
            ["ach_elite_slayer"] = new() { Key = "ach_elite_slayer", Name = "Палач Элиты", Desc = "Уничтожьте 7 Элитных монстров за один забег.", UnlocksItem = "guillotine", RewardText = "Предмет: Старая Гильотина" },
            ["ach_burn_bright"] = new() { Key = "ach_burn_bright", Name = "Страдания в огне", Desc = "Нанесите суммарно 3 000 ед. урона за один забег.", UnlocksItem = "liandrys_torment", RewardText = "Предмет: Мучения Лиандри" },
            ["ach_arcane_burst"] = new() { Key = "ach_arcane_burst", Name = "Магический резонанс", Desc = "Используйте 30 способностей за один забег.", UnlocksItem = "ludens_companion", RewardText = "Предмет: Спутник Людена" },
            ["ach_lightning_strike"] = new() { Key = "ach_lightning_strike", Name = "Разряд Статикка", Desc = "Совершите 75 атак за один забег.", UnlocksItem = "statikk_shiv", RewardText = "Предмет: Заточка Статикка" },

            // --- ОСТАЛЬНЫЕ 8 КРАСНЫХ ПРЕДМЕТОВ (2 КРАСНЫХ ОТКРЫТЫ СО СТАРТА: behemoth, clover) ---
            ["ach_immortal_titan"] = new() { Key = "ach_immortal_titan", Name = "Титан Живучести", Desc = "Достигните 260 ед. максимального здоровья.", UnlocksItem = "tarrasque", RewardText = "Предмет: Сердце Тарраска" },
            ["ach_overheal"] = new() { Key = "ach_overheal", Name = "Непробиваемый Щит", Desc = "Восстановите суммарно 250 HP за один забег.", UnlocksItem = "aegis", RewardText = "Предмет: Эгида" },
            ["ach_high_voltage"] = new() { Key = "ach_high_voltage", Name = "Высокое напряжение", Desc = "Уничтожьте 35 врагов за один забег.", UnlocksItem = "tesla_coil", RewardText = "Предмет: Нестабильная Катушка Теслы" },
            ["ach_ritual_blood"] = new() { Key = "ach_ritual_blood", Name = "Кровавый ритуал", Desc = "Уничтожьте 50 врагов за один забег.", UnlocksItem = "ceremonial_dagger", RewardText = "Предмет: Церемониальный Кинжал" },
            ["ach_blizzard"] = new() { Key = "ach_blizzard", Name = "Вечная мерзлота", Desc = "Посетите Точку Сбора Дельта или убейте 20 врагов вблизи.", UnlocksItem = "frost_relic", RewardText = "Предмет: Ледяная Реликвия" },
            ["ach_armor_breaker"] = new() { Key = "ach_armor_breaker", Name = "Судья Богов", Desc = "Нанесите суммарно 8 000 ед. урона за один забег.", UnlocksItem = "shattering_justice", RewardText = "Предмет: Сокрушительное Правосудие" },
            ["ach_cosmic_mind"] = new() { Key = "ach_cosmic_mind", Name = "Разум извне", Desc = "Используйте 60 способностей за один забег.", UnlocksItem = "alien_head", RewardText = "Предмет: Голова Пришельца" },
            ["ach_supersonic"] = new() { Key = "ach_supersonic", Name = "Сверхзвуковой", Desc = "Разгонитесь до 165% базовой скорости движения.", UnlocksItem = "afterburner", RewardText = "Предмет: Твердосветовой Форсаж" },

            // --- 3 СИНИХ (ЛУННЫХ) ПРЕДМЕТА + КОРАБЛЬ SAFE TRAVELS ---
            ["ach_lunar_purity"] = new() { Key = "ach_lunar_purity", Name = "Лунное Очищение", Desc = "Войдите в Синий Портал на Базар Между Временем.", UnlocksItem = "purity", RewardText = "Лунный предмет: Чистота" },
            ["ach_lunar_stone"] = new() { Key = "ach_lunar_stone", Name = "Лунный Монолит", Desc = "Откройте Лунный Бутон или активируйте Алтарь Тритона.", UnlocksItem = "lunar_pauldron", RewardText = "Лунный предмет: Каменный Наплечник" },
            ["ach_lunar_addict"] = new() { Key = "ach_lunar_addict", Name = "Зависимость от Безвременья", Desc = "Подберите любой Лунный или Пустотный предмет.", UnlocksItem = "spinel_tonic", RewardText = "Лунный предмет: Лунный Тоник" },
            ["ach_safe_travels"] = new()
            {
                Key = "ach_safe_travels",
                Name = "Возвращение на «Safe Travels»",
                Desc = "Победите Митрикса, Короля Ничего, в Небесном Колизее Луны хотя бы один раз.",
                RewardText = "Локация в главном меню: Корабль UES «Safe Travels» (прогулка и диалоги со всеми героями)"
            },

            // =====================================================================================
            // 30 УНИКАЛЬНЫХ ИСПЫТАНИЙ ДЛЯ ОТКРЫТИЯ ВСЕХ ДОСТУПНЫХ ПЕРСОНАЖЕЙ
            // (Секретные локации, уникальные боссы, особые враги и сюжетные 3D-катсцены)
            // =====================================================================================
            ["ach_char_commando"] = new()
            {
                Key = "ach_char_commando",
                Name = "Чёрный Ящик UES «Contact Light»",
                Desc = "Соберите зашифрованные Чёрные Ящики «Contact Light» последовательно на 1-м, 2-м и 3-м этапах одного забега и переживите орбитальную высадку!",
                UnlocksCharacter = "commando",
                RewardText = "Ветеранский протокол Коммандо + Легендарный трофей высадки"
            },
            ["ach_char_huntress"] = new()
            {
                Key = "ach_char_huntress",
                Name = "Безупречная Охота Полнолуния",
                Desc = "Завершите зарядку Телепорта 3-го этапа (или выше), ни разу не опустившись ниже 80% от максимального здоровья за всё время битвы у Телепорта!",
                UnlocksCharacter = "huntress",
                RewardText = "Персонаж: Охотница (Huntress) + Катсцена охоты"
            },
            ["ach_char_sol"] = new()
            {
                Key = "ach_char_sol",
                Name = "HERE COMES DAREDEVIL!",
                Desc = "Снесите Боссу Телепорта 30%+ здоровья за 4.5 секунды (или сокрушите его быстрее чем за 32 сек.), чтобы спровоцировать вторжение Прототипа «Джастис», и победите его!",
                UnlocksCharacter = "sol",
                RewardText = "Персонаж: Сол Бэдгай (альт. личность Кая Киске открывается испытанием Кодекса Ордена)"
            },
            ["ach_char_gwen"] = new()
            {
                Key = "ach_char_gwen",
                Name = "Священный Туман Изольды",
                Desc = "Найдите Золотые Ножницы Изольды в кольце сжимающегося Чёрного Тумана, соберите 3 Золотые Иглы под защитой движущегося круга Священного Тумана и изгоните Жнеца!",
                UnlocksCharacter = "gwen",
                RewardText = "Персонаж: Гвен (Благая Швея) + Катсцена пробуждения"
            },
            ["ach_char_v1"] = new()
            {
                Key = "ach_char_v1",
                Name = "ЧЕЛОВЕЧЕСТВО МЕРТВО // P-DOOR КИБЕР-ГРАЙНД",
                Desc = "Зачистите Телепорт 1-го этапа быстрее чем за 2:45 (открыв Кровавую Шахту Лифта P-1) или войдите в «Кибер-Грайнд» и пройдите 4 волны на динамических колоннах до потери топлива крови!",
                UnlocksCharacter = "v1",
                RewardText = "Персонаж: V1 (ULTRAKILL) + Секретная локация «Кибер-Грайнд»"
            },
            ["ach_char_cheburashka"] = new()
            {
                Key = "ach_char_cheburashka",
                Name = "Мирная Мелодия у Апельсинового Ящика",
                Desc = "Найдите на этапе покачивающийся Ящик с Апельсинами, НЕ атакуйте его, отыщите неподалёку Гармошку Крокодила Гены и сыграйте [E] мирную мелодию рядом с ящиком!",
                UnlocksCharacter = "cheburashka",
                RewardText = "Персонаж: Чебурашка + Катсцена прибытия"
            },
            ["ach_char_artificer"] = new()
            {
                Key = "ach_char_artificer",
                Name = "Термальная Разморозка ENV",
                Desc = "На Базаре Между Временем разогрейте Замёрзший Кристалл сериями огненных/энергетических ударов до 100°C, внесите 5 Лунных Монет и переживите всплеск Лунных Химер!",
                UnlocksCharacter = "artificer",
                RewardText = "Персонаж: Изобретательница (Artificer) + Катсцена освобождения"
            },
            ["ach_char_buzz"] = new()
            {
                Key = "ach_char_buzz",
                Name = "Бесконечность — не предел!",
                Desc = "Поднимитесь по каскаду гравитационных гейзеров на парящий в небесах Шаттл Звёздного Командования (Y=+26м), активируйте бортовой маяк и сокрушите Дредноут Императора Зурга!",
                UnlocksCharacter = "buzz",
                RewardText = "Персонаж: Базз Лайтер + Небесная дуэль с Дредноутом Зурга"
            },
            ["ach_char_enforcer"] = new()
            {
                Key = "ach_char_enforcer",
                Name = "Несгибаемый Бастион Телепорта",
                Desc = "Зарядите любой Телепорт с 0% до 100%, ни разу не выйдя за пределы круга зоны Телепорта на протяжении всей битвы с Боссом!",
                UnlocksCharacter = "enforcer",
                RewardText = "Персонаж: Силовик (Enforcer) + Катсцена щитового строя"
            },
            ["ach_char_scout"] = new()
            {
                Key = "ach_char_scout",
                Name = "ROCK AND STONE! // Эвакуация M.U.L.E.",
                Desc = "Бросайте осветительные флаеры [E], чтобы проявить в темноте 3 жилы Моркита, загрузите руду в шагающего мула M.U.L.E. и успейте запрыгнуть в рампу Десантной Капсулы за 15 секунд!",
                UnlocksCharacter = "scout",
                RewardText = "Персонаж: Разведчик (Scout DRG) + Эвакуация на Drop-Pod"
            },
            ["ach_char_m4a1"] = new()
            {
                Key = "ach_char_m4a1",
                Name = "Радиоперехват «Parapluie»",
                Desc = "Когда вирус «Parapluie» блокирует Телепорт, запеленгуйте по частотному индикатору (Холодно/Горячо) и уничтожьте 3 скрытых глушителя Sangvis Ferri на этапе!",
                UnlocksCharacter = "m4a1",
                RewardText = "Персонаж: M4A1 (Girls' Frontline) + Снятие радиопомех"
            },
            ["ach_char_medic"] = new()
            {
                Key = "ach_char_medic",
                Name = "Полевой Убер-Конвой",
                Desc = "Исцелите раненых союзников в кооперативе на 250+ HP ИЛИ сопроводите раненого Штурмовика-Пулемётчика через поле боя до точки эвакуации, поддерживая лечебный луч [E] до 100% Убер-заряда!",
                UnlocksCharacter = "medic",
                RewardText = "Персонаж: Медик (Medic) + Катсцена Убер-заряда"
            },
            ["ach_char_aigis"] = new()
            {
                Key = "ach_char_aigis",
                Name = "Тёмный Час (00:00) // Жнец Тартара",
                Desc = "Задержитесь на этапе до наступления Тёмного Часа (06:00 на таймере локации), пробейте Старинные Часы S.E.E.S. или войдите в «Башню Тартар» и одолейте преследующего вас Жнеца!",
                UnlocksCharacter = "aigis",
                RewardText = "Персонаж: Айгис (Aigis) + Событие «Тёмный Час»"
            },
            ["ach_char_reimu"] = new()
            {
                Key = "ach_char_reimu",
                Name = "Мастер Грейза Храма Хакурей",
                Desc = "В секретном «Храме Хакурей» наберите 25 очков Грейза (скользя вплотную к пулям данмаку без получения урона!) на протяжении 3 спелл-карт подряд!",
                UnlocksCharacter = "reimu",
                RewardText = "Персонаж: Рейму Хакурей + Секретная локация «Храм Хакурей»"
            },
            ["ach_char_mindustry"] = new()
            {
                Key = "ach_char_mindustry",
                Name = "Мини-Форпост Серпуло (Tower Defense)",
                Desc = "Добывайте Медь вокруг Посадочного Ядра и возведите [E] на 6 тактических ячейках Стены, Турели «Двойка» и Ремонтные Проекторы, чтобы отразить 3 волны штурма!",
                UnlocksCharacter = "mindustry",
                RewardText = "Персонаж: Ядро: Гамма (Mindustry) + Катсцена развёртывания"
            },
            ["ach_char_slark"] = new()
            {
                Key = "ach_char_slark",
                Name = "Тайный Побег из Тёмного Рифа",
                Desc = "Прокрадитесь мимо вращающихся красных прожекторов Надзирателя Тёмного Рифа, похитьте Ключ-Заточку [E], отоприте Адамантовую Клетку и одолейте Надзирателя!",
                UnlocksCharacter = "slark",
                RewardText = "Персонаж: Сларк (Slark) + Стелс-побег из Тёмного Рифа"
            },
            ["ach_char_tidehunter"] = new()
            {
                Key = "ach_char_tidehunter",
                Name = "Приливное Цунами Мальроуна",
                Desc = "Принесите 2 предмета в жертву Океанскому Водовороту Мальроуна и переживите 3 волны затопления арены, запрыгивая на высокие коралловые скалы во время Цунами!",
                UnlocksCharacter = "tidehunter",
                RewardText = "Персонаж: Тайдхантер (Tidehunter) + Катсцена прилива"
            },
            ["ach_char_freeman"] = new()
            {
                Key = "ach_char_freeman",
                Name = "Каскадный Резонанс Сектора C",
                Desc = "Отоприте вентиляционную решётку на Базаре или войдите в «Чёрную Мезу», проведите тележку с кристаллом GG-3883 сквозь лазерные растяжки HECU в луч Спектрометра и отразите десант!",
                UnlocksCharacter = "freeman",
                RewardText = "Персонаж: Гордон Фриман + Секретная локация «Чёрная Меза»"
            },
            ["ach_char_dante"] = new()
            {
                Key = "ach_char_dante",
                Name = "Стрелки Судного Часа LCB",
                Desc = "В кооперативе завершите этап с павшим союзником, вернув его в строй, ИЛИ у разбитого автобуса «Мефистофель» трижды пробейте Полночь (12:00) под цепями Грешников!",
                UnlocksCharacter = "dante",
                RewardText = "Персонаж: Данте #10 (Limbus Company) + Катсцена поворота стрелок"
            },
            ["ach_char_ahri"] = new()
            {
                Key = "ach_char_ahri",
                Name = "Танец Дикой Духовной Лисицы",
                Desc = "Выследите свободно гуляющую по этапу Дикую Духовную Лисицу Васитаи, трижды догоните её перебежки между лисьими норами без единого удара по ней и погладьте [E]!",
                UnlocksCharacter = "ahri",
                RewardText = "Персонаж: Ари (Ahri) + Катсцена Девятихвостой Лисы"
            },
            ["ach_char_lappland"] = new()
            {
                Key = "ach_char_lappland",
                Name = "Вендетта под Куполом Безмолвия",
                Desc = "Защитите курьера Penguin Logistics от засады Сиракузских Чистильщиков, сражаясь внутри ауры Безмолвия (блокирующей навыки M2, Shift и R)!",
                UnlocksCharacter = "lappland",
                RewardText = "Персонаж: Лаппланд (альт. личность Декаденцы открывается собственным испытанием)"
            },
            ["ach_char_w_arknights"] = new()
            {
                Key = "ach_char_w_arknights",
                Name = "Сундук-Мимик Наёмницы W",
                Desc = "Переживите засаду заминированного Красного Сундука-Мимика W и отфутбольте [E] или ударами 3 тикающие бомбы обратно в укрытие снайперши!",
                UnlocksCharacter = "w_arknights",
                RewardText = "Персонаж: W (альт. личность Вишадель открывается взрывным рекордом)"
            },
            ["ach_char_gold_ship"] = new()
            {
                Key = "ach_char_gold_ship",
                Name = "Финиш Первым на Арима Кинэн!",
                Desc = "На овальном треке секретного «Ипподрома Трейсен» примите старт против 5 соперниц-бегуний и пересеките финишную черту полного круга ПЕРВЫМ!",
                UnlocksCharacter = "gold_ship",
                RewardText = "Персонаж: Голд Шип (Umamusume) + Гонка на «Ипподроме Трейсен»"
            },
            ["ach_char_pushnoy"] = new()
            {
                Key = "ach_char_pushnoy",
                Name = "ЭКСПЕРИМЕНТЫ В ЭФИРЕ «ГАЛИЛЕО»!",
                Desc = "Пройдите 3 научных опыта подряд у передвижной студии «Галилео»: 1) Акустический резонанс быстрыми атаками, 2) Замыкание дуги катушки Теслы, 3) Прыжки через звуковые рок-кольца до 10 000 В!",
                UnlocksCharacter = "pushnoy",
                RewardText = "Персонаж: Александр Пушной + Рок-катсцена"
            },
            ["ach_char_freddy"] = new()
            {
                Key = "ach_char_freddy",
                Name = "Офис Охраны: 06:00 AM (Энергия 100%)",
                Desc = "Войдите в «Пиццерию Fazbear» (или сломанный аркадный автомат на Базаре), экономьте заряд батареи Офиса, вовремя закрывайте Левую/Правую гермодвери [E], заводите шкатулку и одолейте Эндоскелета!",
                UnlocksCharacter = "freddy",
                RewardText = "Персонаж: Фредди Фазбер + Оборона Офиса Fazbear"
            },
            ["ach_char_steve"] = new()
            {
                Key = "ach_char_steve",
                Name = "Руины Портала Незера",
                Desc = "Разбейте атаками 4 жилы Обсидиановой Руды и сундук с Огнивом в разных уголках карты, вручную вставьте блоки [E] в разрушенную рамку Портала Незера, зажгите его и отбейте рой Ифритов!",
                UnlocksCharacter = "steve",
                RewardText = "Персонаж: Стив (Minecraft) + Крафт Портала Незера"
            },
            ["ach_char_jotaro"] = new()
            {
                Key = "ach_char_jotaro",
                Name = "Падение Звёздного Метеорита",
                Desc = "Дождитесь падения горящего Метеорита со Стрелой с небес на 2-м+ этапе, пробейтесь сквозь ураган к кратеру и переживите испытание остановки времени фантомного Стенда!",
                UnlocksCharacter = "jotaro",
                RewardText = "Персонаж: Джотаро Куджо (JoJo) + Катсцена Star Platinum"
            },
            ["ach_char_vyper"] = new()
            {
                Key = "ach_char_vyper",
                Name = "Подкат Под Лазерами Тюрьмы OSIC",
                Desc = "Проскользните на рывках/вприсядку под низкими горизонтальными лазерными лучами тюремного периметра OSIC и отстрелите все 4 магнитных замка бронекапсулы!",
                UnlocksCharacter = "vyper",
                RewardText = "Персонаж: Вайпер (Deadlock) + Катсцена взлома"
            },
            ["ach_char_haze"] = new()
            {
                Key = "ach_char_haze",
                Name = "Контракт Бюро Сновидений OSIC",
                Desc = "Коснитесь Ловца Снов OSIC и в индиговом тумане выследите и устраните Истинного Спящего среди бродячих сонных двойников-миражей (удар по ложному двойнику поднимает тревогу)!",
                UnlocksCharacter = "haze",
                RewardText = "Персонаж: Хейз (Deadlock) + Катсцена пробуждения"
            },
            ["ach_char_dmc_trio"] = new()
            {
                Key = "ach_char_dmc_trio",
                Name = "Разлом Клипота: Ранг Стиля SSS",
                Desc = "Уничтожьте Босса Телепорта быстрее чем за 25 сек. без потери HP, чтобы рассечь воздух Разломом Клипота (или войдите на «Вершину Клипота»), наберите ранг SSS и одолейте Миража Вергилия!",
                UnlocksCharacter = "dmc_trio",
                RewardText = "Персонаж: Трио DMC (Данте; Неро и Вергилий открываются испытаниями стиля!)"
            },
            ["ach_char_jack_o"] = new()
            {
                Key = "ach_char_jack_o",
                Name = "Конфетный Бильярд Миньонов Элизиона",
                Desc = "Ударными атаками и пинками загоните 3 упругих чиби-рыцарей Железной Тыквы точно в 3 светящихся Кольца-Ворота Элизиона за отведённое время!",
                UnlocksCharacter = "jack_o",
                RewardText = "Персонаж: Джек-О Валентайн (Guilty Gear -Strive-) + Катсцена пробуждения"
            },

            // --- ДОСТИЖЕНИЯ РАСШИРЕНИЯ И АЛЬТ. ЛИЧНОСТЕЙ ---
            ["ach_char_susie"] = new()
            {
                Key = "ach_char_susie",
                Name = "Отбой Rude Buster Тёмного Святилища",
                Desc = "В секретном «Тёмном Святилище» копите шкалу TP скольжением рядом с клинками Тьмы (Graze до 100%) и трижды отразите [E] волну Rude Buster обратно в Рокочущего Рыцаря!",
                UnlocksCharacter = "susie",
                RewardText = "Персонаж: Сьюзи (Deltarune) + Секретное «Тёмное Святилище»"
            },
            ["ach_char_spamton"] = new()
            {
                Key = "ach_char_spamton",
                Name = "СДЕЛКА ВСЕЙ ЖИЗНИ [[1997 KROMER]]",
                Desc = "Ответьте на звонок Мусорного Телефона, подловите выгодный курс [[KROMER]], чтобы выкупить «Загрузочный Диск», вставьте его в пустой корпус НЕО и защитите загрузку от [Pipis]!",
                UnlocksCharacter = "spamton",
                RewardText = "Персонаж: Спамтон Г. Спамтон (СПАМТОН НЕО открывается накоплением богатства!)"
            },
            ["ach_char_binah"] = new()
            {
                Key = "ach_char_binah",
                Name = "Подавление Перегрузки Клипота",
                Desc = "На «Этаже Извлечения» примите бой с Арбитром Биной, вовремя подавляя [E] тикающие Столпы Перегрузки Клипота по углам зала до их детонации!",
                UnlocksCharacter = "binah",
                RewardText = "Персонаж: Бина / Гарион (Project Moon) + Секретный «Этаж Извлечения»"
            },
            ["ach_char_don_quixote"] = new()
            {
                Key = "ach_char_don_quixote",
                Name = "Рыцарский Турнир Верхом на Росинанте!",
                Desc = "Подберите Турнирное Копьё (+75% скорости галопа) и трижды сокрушите встречным лобовым тараном атакующих Кавалеристов Ла-Манчаленда на полном скаку!",
                UnlocksCharacter = "don_quixote",
                RewardText = "Персонаж: Дон Кихот (Фонарь E.G.O и Санчо открываются боевыми подвигами!)"
            },
            ["ach_char_priestess"] = new()
            {
                Key = "ach_char_priestess",
                Name = "Эхо Первой Цивилизации: EYESOFPRIESTESS",
                Desc = "Отследите по глитч-помехам интерфейса ◇ 3 скрытых Ромба Памяти PRTS на этапе (открыв прямой разлом в «Ядро Ориджиниума») и победите босса PRTS, Исходный Код!",
                UnlocksCharacter = "priestess",
                RewardText = "Персонаж: Пристесс (Arknights) + Секретное «Ядро Ориджиниума»"
            },
            ["ach_char_closure"] = new()
            {
                Key = "ach_char_closure",
                Name = "Инженерный Магазин «Родос Айленд»",
                Desc = "Проинвестируйте золото в 3 апгрейда полевой мастерской Кложер на этапе и защитите выкатившуюся на помощь платформу Lancet-2 от налёта дронов!",
                UnlocksCharacter = "closure",
                RewardText = "Персонаж: Кложер (Arknights) + Полевая поддержка Lancet-2"
            },
            ["ach_char_daitaku_helios"] = new()
            {
                Key = "ach_char_daitaku_helios",
                Name = "Вэй~☆! Ритм-Баттл 128 BPM!",
                Desc = "У пульта диджея «WEI~☆!» попадайте нажатиями [E] или [Space] точно в центральный бит неоновой ритм-шкалы 8 раз подряд на максимальном драйве!",
                UnlocksCharacter = "daitaku_helios",
                RewardText = "Персонаж: Дайтаку Гелиос (Umamusume) + Ритм-вечеринка"
            },
            ["ach_char_robin"] = new()
            {
                Key = "ach_char_robin",
                Name = "Мелодия Гармонии Большого Театра",
                Desc = "В «Большом Театре Пенаконии» сыграйте правильную последовательность из 5 музыкальных прожекторов-нот на сцене, чтобы развеять хоровой щит Великого Септимия, и победите его!",
                UnlocksCharacter = "robin",
                RewardText = "Персонаж: Зарянка / Робин (Honkai: Star Rail) + «Театр Пенаконии»"
            },
            ["ach_char_sanabi"] = new()
            {
                Key = "ach_char_sanabi",
                Name = "Проект №17: Вертикальный Прорыв Маго",
                Desc = "На высотной Шахте Реактора Маго перелетайте на тросе Руки-Крюка [E] между парящими дронами над поднимающимся плазменным полом и разрушьте Ядро Надзора наверху!",
                UnlocksCharacter = "sanabi",
                RewardText = "Персонаж: Бригадный Генерал и Мари (SANABI)"
            },
            ["ach_char_bird_trio"] = new()
            {
                Key = "ach_char_bird_trio",
                Name = "Три Стража Чёрного Леса",
                Desc = "Пройдите 3 испытания клеток Аномалий Чёрного Леса подряд: 1) Умиротворите Малую Птицу (8 сек. без единой атаки), 2) Уравновесьте Весы Длинной Птицы, 3) Сберегите Вечную Лампу Большой Птицы!",
                UnlocksCharacter = "bird_trio",
                RewardText = "Персонаж: Трио Птиц (Длинная и Малая Птицы открываются собственными испытаниями)"
            },
            ["ach_char_apocalypse_bird"] = new()
            {
                Key = "ach_char_apocalypse_bird",
                Name = "Врата Сумерек Чёрного Леса",
                Desc = "Соберитесь в сетевом лобби втроём с владельцами «Трио Птиц» ИЛИ (открыв Трио Птиц) активируйте Алтарь Сумерек в забеге, разбейте 3 Яйца Апокалипсиса и одолейте Зверя Чёрного Леса!",
                UnlocksCharacter = "apocalypse_bird",
                RewardText = "Рейдовый персонаж: Птица Апокалипсиса"
            },
            ["ach_char_komeiji"] = new()
            {
                Key = "ach_char_komeiji",
                Name = "Слепое Пятно Подсознания Тирэйден",
                Desc = "Во «Дворце Духов Земли» трижды отыщите невидимую при прямом взгляде Коиши с помощью периферийного зрения (отвернув камеру в сторону!) и коснитесь её [E]!",
                UnlocksCharacter = "komeiji",
                RewardText = "Персонаж: Сатори Комейдзи (Коиши открывается испытанием Неуловимого Подсознания)"
            },
            ["ach_char_false_son"] = new()
            {
                Key = "ach_char_false_son",
                Name = "Грозовое Очищение Изначального Меридиана",
                Desc = "Зарядите Золотой Алтарь Алкиона золотом на этапе (или войдите на «Изначальный Меридиан»), расколите 3 Жеоды Алкиона ударами небесных молний и победите Ложного Сына!",
                UnlocksCharacter = "false_son",
                RewardText = "Персонаж: Ложный Сын (RoR2: Seekers of the Storm) + «Изначальный Меридиан»"
            },

            // --- 13 ПЕРСОНАЛЬНЫХ ИСПЫТАНИЙ АЛЬТЕРНАТИВНЫХ ЛИЧНОСТЕЙ (ach_persona_*) ---
            ["ach_persona_sol_ky"] = new()
            {
                Key = "ach_persona_sol_ky",
                Name = "Кодекс Священного Ордена: Кай Киске",
                Desc = "Играя за Сола Бэдгая, победите Босса Телепорта, не имея в инвентаре ни одного Лунного или Пустотного предмета.",
                RewardText = "Альт. личность [Сол Бэдгай]: Кай Киске (Гром Иллирии)"
            },
            ["ach_persona_lappland_decadenza"] = new()
            {
                Key = "ach_persona_lappland_decadenza",
                Name = "Гранд-Карнавал Сиракуз: Декаденца",
                Desc = "Играя за Лаппланд, истребите 16 врагов за 14 секунд или казните 3 Элитных врагов под эффектом Безмолвия за один забег.",
                RewardText = "Альт. личность [Лаппланд]: Лаппланд Декаденца"
            },
            ["ach_persona_w_wisadel"] = new()
            {
                Key = "ach_persona_w_wisadel",
                Name = "Корона Предков Каздела: Вишадель",
                Desc = "Играя за W, нанесите 450+ урона одним взрывом или сокрушите Босса Телепорта быстрее чем за 30 секунд.",
                RewardText = "Альт. личность [W]: Вишадель (Wiš'adel)"
            },
            ["ach_persona_dmc_nero"] = new()
            {
                Key = "ach_persona_dmc_nero",
                Name = "Зажигание Алой Королевы: Неро",
                Desc = "Играя за Трио DMC, победите Босса Телепорта, применив навык перемещения/стиля [Shift] не менее 8 раз за время боя.",
                RewardText = "Альт. личность [Трио DMC]: Неро (Nero)"
            },
            ["ach_persona_dmc_vergil"] = new()
            {
                Key = "ach_persona_dmc_vergil",
                Name = "Приближающаяся Буря: Вергилий",
                Desc = "Играя за Трио DMC, победите Босса Телепорта быстрее чем за 25 секунд или одолейте Митрикса.",
                RewardText = "Альт. личность [Трио DMC]: Вергилий (Vergil)"
            },
            ["ach_persona_spamton_neo"] = new()
            {
                Key = "ach_persona_spamton_neo",
                Name = "КРЫЛЬЯ СВОБОДЫ [[СПАМТОН НЕО]]",
                Desc = "Играя за Спамтона, накопите $400 золота или соберите 10 предметов за один забег, став [[БОЛЬШОЙ ШИШКОЙ]]!",
                RewardText = "Альт. личность [Спамтон]: СПАМТОН НЕО"
            },
            ["ach_persona_don_lantern"] = new()
            {
                Key = "ach_persona_don_lantern",
                Name = "Огонёк Тёмной Чащи: Фонарь E.G.O",
                Desc = "Играя за Дон Кихот, восстановите суммарно 300 HP в бою за один забег.",
                RewardText = "Альт. личность [Дон Кихот]: Lobotomy E.G.O::Lantern"
            },
            ["ach_persona_don_manager"] = new()
            {
                Key = "ach_persona_don_manager",
                Name = "Пробуждение Второго Сородича: Санчо",
                Desc = "Играя за Дон Кихот, победите 2 Боссов за один забег или доберитесь до 4-го этапа.",
                RewardText = "Альт. личность [Дон Кихот]: Управляющая Ла-Манчаленда"
            },
            ["ach_persona_bird_long"] = new()
            {
                Key = "ach_persona_bird_long",
                Name = "Беспристрастные Весы: Длинная Птица",
                Desc = "Играя за Трио Птиц, победите Босса Телепорта, не опустившись ниже 70% здоровья за весь бой.",
                RewardText = "Альт. личность [Трио Птиц]: Длинная Птица"
            },
            ["ach_persona_bird_small"] = new()
            {
                Key = "ach_persona_bird_small",
                Name = "Карающий Клюв Возмездия: Малая Птица",
                Desc = "Играя за Трио Птиц, уничтожьте 5 врагов в течение 6 секунд после получения урона.",
                RewardText = "Альт. личность [Трио Птиц]: Маленькая Птица"
            },
            ["ach_persona_komeiji_koishi"] = new()
            {
                Key = "ach_persona_komeiji_koishi",
                Name = "Закрытые Глаза Любви: Коиши Комейдзи",
                Desc = "Играя за Сатори Комейдзи, проведите 30 секунд в активном бою рядом с врагами без получения урона.",
                RewardText = "Альт. личность [Сатори]: Коиши Комейдзи"
            },
            ["ach_persona_buzz_zurg"] = new()
            {
                Key = "ach_persona_buzz_zurg",
                Name = "Галактическая Империя Зурга",
                Desc = "Играя за Базза Лайтера, победите Дредноут Императора Зурга или 2 Боссов за один забег.",
                RewardText = "Альт. личность [Базз Лайтер]: Император Зург"
            },
            ["ach_persona_v1_v2"] = new()
            {
                Key = "ach_persona_v1_v2",
                Name = "Красная Зеркальная Дуэль: V2",
                Desc = "Играя за V1, наберите ранг стиля SSS / ULTRAKILL (800+ очков стиля) в бою.",
                RewardText = "Альт. личность [V1]: Высшая Машина V2"
            },
            ["ach_boss_echo_providence"] = new()
            {
                Key = "ach_boss_echo_providence",
                Name = "Тень Булавы Слабых",
                Desc = "Победите босса Телепорта — Искажённого Провидца.",
                UnlocksItem = "holy_mantle",
                RewardText = "Предмет [Isaac]: Святая Мантия"
            },
            ["ach_boss_false_son"] = new()
            {
                Key = "ach_boss_false_son",
                Name = "Разбитые Оковы Лунной Погибели",
                Desc = "Победите босса — Ложного Сына.",
                UnlocksItem = "totem_undying",
                RewardText = "Предмет [Minecraft]: Тотем Бессмертия"
            },
            ["ach_boss_prts"] = new()
            {
                Key = "ach_boss_prts",
                Name = "Перезагрузка Изначального Протокола",
                Desc = "Победите босса — PRTS, Исходный Код.",
                UnlocksItem = "poca_sp_battery",
                RewardText = "Предмет [Arknights]: Батарея SP Родоса"
            },
            ["ach_boss_binah"] = new()
            {
                Key = "ach_boss_binah",
                Name = "Разомкнутый Замок Главы",
                Desc = "Победите босса — Арбитра Бину.",
                UnlocksItem = "damocles",
                RewardText = "Лунный предмет [Isaac]: Дамоклов Меч"
            },
            ["ach_boss_roaring_knight"] = new()
            {
                Key = "ach_boss_roaring_knight",
                Name = "Запечатанный Тёмный Фонтан",
                Desc = "Победите босса — Рокочущего Рыцаря.",
                UnlocksItem = "brimstone",
                RewardText = "Предмет [Isaac]: Сера (Brimstone)"
            },
            ["ach_boss_septimus"] = new()
            {
                Key = "ach_boss_septimus",
                Name = "Пробуждение от Сна Порядка",
                Desc = "Победите босса — Великого Септимия, Хор Баланса.",
                UnlocksItem = "elytra_curse",
                RewardText = "Лунный предмет [Minecraft]: Проклятые Элитры"
            },
            ["ach_horde_many"] = new()
            {
                Key = "ach_horde_many",
                Name = "Один Против Легиона",
                Desc = "Уничтожьте элитную «Орду Многих», заменившую стража Телепорта и делящую общее здоровье Босса!",
                UnlocksItem = "black_king_bar",
                RewardText = "Предмет [Dota 2]: Посох Чёрного Короля (BKB)"
            },
            ["ach_item_golden_apple"] = new()
            {
                Key = "ach_item_golden_apple",
                Name = "Золотой Сад Крафтера",
                Desc = "Накопите более 250 золота за один забег или переживите падение ниже 35% HP в бою.",
                UnlocksItem = "golden_apple",
                RewardText = "Предмет [Minecraft]: Золотое Яблоко"
            },
            ["ach_item_orirock"] = new()
            {
                Key = "ach_item_orirock",
                Name = "Ферма 1-7",
                Desc = "Уничтожьте 25 монстров за один забег.",
                UnlocksItem = "orirock_cluster",
                RewardText = "Предмет [Arknights]: Кластер Орирока"
            },
            ["ach_item_ender_pearl"] = new()
            {
                Key = "ach_item_ender_pearl",
                Name = "Прыжок Сквозь Край",
                Desc = "Примените навык перемещения [Shift] 15 раз за один забег.",
                UnlocksItem = "ender_pearl",
                RewardText = "Предмет [Minecraft]: Эндер-Жемчуг"
            }
        };

        public static AchievementInfo? GetCharacterUnlockAchievement(string charKey)
        {
            foreach (var ach in Achievements.Values)
            {
                if (ach.UnlocksCharacter == charKey)
                    return ach;
            }
            return null;
        }

        public static AchievementInfo? GetItemUnlockAchievement(string itemKey)
        {
            foreach (var ach in Achievements.Values)
            {
                if (ach.UnlocksItem == itemKey)
                    return ach;
            }
            return null;
        }

        // =====================================================================================
        // ЗАЦИКЛЕННЫЙ ТРЁХСЛОЙНЫЙ ПУЛ ОБЫЧНЫХ ЭТАЖЕЙ (БЕЗ УНИКАЛЬНЫХ И СЕКРЕТНЫХ ЛОКАЦИЙ):
        // Слой 1 (Этажи 1, 4, 7...): Стартовые локации
        // Слой 2 (Этажи 2, 5, 8...): Промежуточные локации
        // Слой 3 (Этажи 3, 6, 9...): Предфинальные локации
        // =====================================================================================
        public static readonly string[][] StageLayerPools =
        {
            new[] { "roost", "acres", "sundered", "ionia" },
            new[] { "wetland", "aqueduct", "aphelian", "sulfur" },
            new[] { "rallypoint", "depths", "siren", "meadow" }
        };

        public static int GetStagePoolLayerIndex(int stageNum)
            => ((Math.Max(1, stageNum) - 1) % StageLayerPools.Length);

        public static string GetStagePoolLayerLabel(int stageNum)
            => GetStagePoolLayerIndex(stageNum) switch
            {
                0 => "СЛОЙ I :: СТАРТОВЫЙ ПУЛ",
                1 => "СЛОЙ II :: ПРОМЕЖУТОЧНЫЙ ПУЛ",
                _ => "СЛОЙ III :: ПРЕДФИНАЛЬНЫЙ ПУЛ"
            };

        public static int GetStageBiomeLayerNumber(string stageId)
        {
            for (int layer = 0; layer < StageLayerPools.Length; layer++)
            {
                if (Array.IndexOf(StageLayerPools[layer], stageId) >= 0)
                    return layer + 1;
            }
            return 0; // Уникальная / внецикловая локация
        }

        private static string? _lastRolledStageId = null;

        public static StagePalette GetLoopedStagePalette(int stageNum, Random? rng = null)
        {
            var r = rng ?? _rng;
            int layerIdx = GetStagePoolLayerIndex(stageNum);
            string[] poolIds = StageLayerPools[layerIdx];
            var candidates = StagePalettes.Where(p => Array.IndexOf(poolIds, p.Id) >= 0).ToList();
            if (candidates.Count == 0)
                return StagePalettes[0];

            if (candidates.Count > 1 && !string.IsNullOrEmpty(_lastRolledStageId))
            {
                var nonRepeat = candidates.Where(p => p.Id != _lastRolledStageId).ToList();
                if (nonRepeat.Count > 0) candidates = nonRepeat;
            }

            var chosen = candidates[r.Next(candidates.Count)];
            _lastRolledStageId = chosen.Id;
            return chosen;
        }

        public static readonly List<StagePalette> StagePalettes = new()
        {
            new() { Id = "roost", Name = "Далёкое Гнездовье", Desc = "Туманные утёсы и руины древних храмов на краю океана Petrichor V.", Ground = Rgb(36, 52, 66), Ruins = Rgb(62, 78, 96), Accent = Rgb(75, 225, 255), Sky = Rgb(14, 22, 36), Nebula = Rgb(45, 110, 165), Moon = Rgb(210, 230, 255) },
            new() { Id = "acres", Name = "Выжженные Угодья", Desc = "Выжженные солнцем террасы с раскалёнными обелисками и пеплом.", Ground = Rgb(74, 46, 36), Ruins = Rgb(102, 68, 54), Accent = Rgb(255, 150, 45), Sky = Rgb(34, 18, 16), Nebula = Rgb(165, 70, 40), Moon = Rgb(255, 200, 140) },
            new() { Id = "depths", Name = "Глубины Бездны", Desc = "Глубокий тектонический разлом, озарённый кристаллами магмы.", Ground = Rgb(42, 30, 62), Ruins = Rgb(70, 52, 98), Accent = Rgb(240, 65, 195), Sky = Rgb(20, 12, 34), Nebula = Rgb(135, 40, 145), Moon = Rgb(245, 160, 255) },
            new() { Id = "siren", Name = "Зов Сирены", Desc = "Кладбище разбившихся кораблей среди изумрудных скал.", Ground = Rgb(30, 60, 56), Ruins = Rgb(54, 90, 84), Accent = Rgb(90, 255, 165), Sky = Rgb(12, 28, 28), Nebula = Rgb(40, 140, 115), Moon = Rgb(180, 255, 220) },
            new() { Id = "meadow", Name = "Небесный Луг", Desc = "Высокогорные лавандовые плато с парящими кольцами и золотыми кристаллами целестита.", Ground = Rgb(58, 42, 82), Ruins = Rgb(86, 68, 118), Accent = Rgb(255, 210, 85), Sky = Rgb(22, 14, 42), Nebula = Rgb(140, 75, 215), Moon = Rgb(255, 235, 175) },
            new() { Id = "rallypoint", Name = "Точка Сбора Дельта", Desc = "Заснеженная арктическая база со шпилями вечного льда, контейнерами и радиовышками.", Ground = Rgb(175, 196, 218), Ruins = Rgb(72, 88, 112), Accent = Rgb(255, 85, 70), Sky = Rgb(18, 28, 46), Nebula = Rgb(70, 155, 225), Moon = Rgb(220, 240, 255) },
            new() { Id = "wetland", Name = "Болотистый Уступ", Desc = "Затопленные мшистые руины, переплетённые гигантскими корнями и янтарными спорами.", Ground = Rgb(34, 48, 36), Ruins = Rgb(58, 72, 54), Accent = Rgb(215, 245, 75), Sky = Rgb(14, 24, 18), Nebula = Rgb(75, 135, 65), Moon = Rgb(215, 255, 170) },
            new() { Id = "aqueduct", Name = "Заброшенный Акведук", Desc = "Песчаные каньоны с древними акведуками, нефтяными разломами и бирюзовыми оазисами.", Ground = Rgb(118, 88, 56), Ruins = Rgb(148, 114, 78), Accent = Rgb(65, 235, 215), Sky = Rgb(38, 26, 18), Nebula = Rgb(195, 130, 65), Moon = Rgb(255, 230, 185) },
            new() { Id = "sundered", Name = "Расколотая Роща", Desc = "Древний лес исполинских биолюминесцентных грибов и парящих лиан.", Ground = Rgb(32, 54, 48), Ruins = Rgb(68, 48, 58), Accent = Rgb(255, 95, 210), Sky = Rgb(14, 22, 30), Nebula = Rgb(175, 55, 145), Moon = Rgb(255, 185, 235) },
            new() { Id = "sulfur", Name = "Серные Озёра", Desc = "Ступенчатые кислотные террасы, гейзеры ядовитого пара и янтарные купола.", Ground = Rgb(68, 74, 34), Ruins = Rgb(92, 98, 50), Accent = Rgb(165, 255, 55), Sky = Rgb(26, 30, 14), Nebula = Rgb(135, 175, 40), Moon = Rgb(225, 255, 140) },
            new() { Id = "aphelian", Name = "Святилище Афелия", Desc = "Парящие мраморно-золотые храмы Солнца с солнечными часами и небесными обелисками.", Ground = Rgb(142, 134, 118), Ruins = Rgb(188, 176, 152), Accent = Rgb(255, 195, 55), Sky = Rgb(28, 22, 38), Nebula = Rgb(215, 145, 55), Moon = Rgb(255, 240, 195) },
            new() { Id = "ionia", Name = "Первозданная Иония", Desc = "Священная роща цветущей сакуры, алых врат Тории и духовных фонарей.", Ground = Rgb(46, 58, 72), Ruins = Rgb(145, 42, 58), Accent = Rgb(255, 145, 205), Sky = Rgb(20, 16, 38), Nebula = Rgb(215, 95, 165), Moon = Rgb(255, 215, 240) }
        };

        public static readonly Dictionary<string, StagePalette> SpecialPalettes = new()
        {
            ["bazaar"] = new() { Id = "bazaar", Name = "Базар Между Временем", Desc = "Тайное святилище Тритона вне пространства и времени.", Ground = Rgb(20, 26, 46), Ruins = Rgb(38, 52, 88), Accent = Rgb(65, 195, 255), Sky = Rgb(10, 14, 28), Nebula = Rgb(35, 95, 185), Moon = Rgb(140, 215, 255) },
            ["moon"] = new() { Id = "moon", Name = "Начало (Луна)", Desc = "Расколотая цитадель Митрикса на орбите Petrichor V.", Ground = Rgb(185, 198, 218), Ruins = Rgb(55, 68, 92), Accent = Rgb(75, 215, 255), Sky = Rgb(8, 12, 24), Nebula = Rgb(40, 110, 195), Moon = Rgb(90, 210, 255) },
            ["pvp"] = new() { Id = "pvp", Name = "Колизей Раздора (PvP Арена)", Desc = "Гладиаторская арена выживших. Сражайтесь с соперниками и выбивайте артефакты из монстров!", Ground = Rgb(38, 30, 48), Ruins = Rgb(68, 54, 82), Accent = Rgb(255, 95, 65), Sky = Rgb(18, 12, 24), Nebula = Rgb(165, 45, 75), Moon = Rgb(255, 165, 120) },
            ["pizzeria"] = new() { Id = "pizzeria", Name = "Пиццерия Freddy Fazbear's (Ночная Смена)", Desc = "Секретный ночной зал 1987 года с аркадными автоматами, сценой аниматроников и музыкальной шкатулкой.", Ground = Rgb(28, 22, 38), Ruins = Rgb(65, 32, 48), Accent = Rgb(255, 185, 45), Sky = Rgb(10, 8, 16), Nebula = Rgb(125, 35, 85), Moon = Rgb(255, 215, 95) },
            ["qliphoth"] = new() { Id = "qliphoth", Name = "Вершина Древа Клипота", Desc = "Секретное демоническое измерение корней Клипота, где покоятся клинки Спарды — Ямато и Мятежник.", Ground = Rgb(42, 16, 24), Ruins = Rgb(82, 24, 36), Accent = Rgb(255, 45, 75), Sky = Rgb(16, 6, 12), Nebula = Rgb(185, 25, 55), Moon = Rgb(255, 85, 110) },
            ["black_mesa"] = new() { Id = "black_mesa", Name = "Сектор C Чёрной Мезы (Аномальные Материалы)", Desc = "Секретная тестовая камера исследовательского центра с антимасс-спектрометром и каскадным резонансом.", Ground = Rgb(48, 56, 52), Ruins = Rgb(78, 88, 82), Accent = Rgb(255, 155, 35), Sky = Rgb(14, 24, 18), Nebula = Rgb(65, 195, 95), Moon = Rgb(185, 255, 145) },
            ["hell_cybergrind"] = new() { Id = "hell_cybergrind", Name = "Кибер-Грайнд (Предел Ада)", Desc = "Секретный симулятор боевого терминала Ада с геометрическими колоннами и потоками раскалённой крови.", Ground = Rgb(34, 14, 18), Ruins = Rgb(64, 22, 28), Accent = Rgb(255, 45, 55), Sky = Rgb(18, 4, 8), Nebula = Rgb(205, 25, 35), Moon = Rgb(255, 95, 75) },
            ["tartarus"] = new() { Id = "tartarus", Name = "Башня Тартар (Тёмный Час)", Desc = "Искажённый лабиринт полуночного часа под изумрудной луной, кишащий Арканными Тенями и цепями Жнеца.", Ground = Rgb(18, 36, 28), Ruins = Rgb(34, 66, 52), Accent = Rgb(85, 255, 165), Sky = Rgb(8, 22, 16), Nebula = Rgb(35, 165, 95), Moon = Rgb(145, 255, 175) },
            ["hakurei_shrine"] = new() { Id = "hakurei_shrine", Name = "Храм Хакурей (Генсокё)", Desc = "Заоблачное святилище на границе реальности и фантазии, усыпанное лепестками сакуры и печатями Инь-Ян.", Ground = Rgb(56, 44, 58), Ruins = Rgb(168, 42, 48), Accent = Rgb(255, 135, 185), Sky = Rgb(24, 16, 36), Nebula = Rgb(225, 85, 145), Moon = Rgb(255, 230, 240) },
            ["tracen_derby"] = new() { Id = "tracen_derby", Name = "Ипподром Трейсен (Арима Кинэн)", Desc = "Парадный зелёный стадион финального дерби со стартовыми воротами, трибунами и подковами удачи.", Ground = Rgb(38, 92, 48), Ruins = Rgb(225, 232, 245), Accent = Rgb(255, 205, 65), Sky = Rgb(22, 48, 86), Nebula = Rgb(95, 185, 255), Moon = Rgb(255, 245, 195) },
            ["dark_sanctuary"] = new() { Id = "dark_sanctuary", Name = "Тёмное Святилище (Deltarune)", Desc = "Готический собор вокруг бьющего в небо чёрно-бирюзового Тёмного Фонтана и витражей пророчества.", Ground = Rgb(20, 18, 42), Ruins = Rgb(48, 42, 88), Accent = Rgb(75, 245, 235), Sky = Rgb(8, 6, 22), Nebula = Rgb(125, 45, 195), Moon = Rgb(185, 255, 250) },
            ["terminal_lucidity"] = new() { Id = "terminal_lucidity", Name = "Дворец Духов Земли (Тирэйден)", Desc = "Подземный особняк сестёр Комеидзи над Пылающим Адом с витражной розеткой Третьего Глаза и астральными венами.", Ground = Rgb(36, 20, 44), Ruins = Rgb(78, 36, 68), Accent = Rgb(255, 75, 155), Sky = Rgb(16, 8, 24), Nebula = Rgb(195, 45, 115), Moon = Rgb(255, 175, 215) },
            ["extraction_floor"] = new() { Id = "extraction_floor", Name = "Этаж Извлечения и Философии", Desc = "Чёрно-золотой зал Библиотеки и Корпорации Лоботомия со столпами Сефирот, цепями Замка и чайным павильоном Арбитра.", Ground = Rgb(24, 20, 18), Ruins = Rgb(52, 44, 34), Accent = Rgb(255, 205, 65), Sky = Rgb(10, 8, 8), Nebula = Rgb(165, 120, 35), Moon = Rgb(255, 225, 120) },
            ["originium_core"] = new() { Id = "originium_core", Name = "Ядро Ассимилированной Вселенной (PRTS)", Desc = "Горизонт кристаллического Ориджиниума и саркофагов Первой Цивилизации под алым созвездием ромбов PRTS ◇.", Ground = Rgb(22, 24, 32), Ruins = Rgb(46, 52, 68), Accent = Rgb(255, 65, 75), Sky = Rgb(8, 10, 18), Nebula = Rgb(195, 38, 52), Moon = Rgb(255, 135, 145) },
            ["penacony_theater"] = new() { Id = "penacony_theater", Name = "Большой Театр Пенаконии", Desc = "Величественная золотая оперная сцена Мира Грёз с органными трубами Порядка и софитами Гармонии.", Ground = Rgb(44, 26, 48), Ruins = Rgb(168, 136, 72), Accent = Rgb(255, 215, 95), Sky = Rgb(18, 10, 32), Nebula = Rgb(195, 135, 65), Moon = Rgb(255, 240, 185) },
            ["prime_meridian"] = new() { Id = "prime_meridian", Name = "Изначальный Меридиан", Desc = "Расколотая грозовая вершина колоссальной статуи Провиденса, пронизанная золотыми жеодами Аурелионита и лунными молниями.", Ground = Rgb(34, 42, 58), Ruins = Rgb(66, 78, 104), Accent = Rgb(95, 215, 255), Sky = Rgb(12, 18, 34), Nebula = Rgb(65, 155, 235), Moon = Rgb(255, 215, 95) },
            ["safe_travels"] = new() { Id = "safe_travels", Name = "Корабль UES «Safe Travels»", Desc = "Орбитальный крейсер Объединённой Земли — мирный штаб экипажа выживших после победы над Митриксом.", Ground = Rgb(38, 46, 62), Ruins = Rgb(68, 82, 108), Accent = Rgb(75, 215, 255), Sky = Rgb(6, 10, 22), Nebula = Rgb(42, 115, 205), Moon = Rgb(185, 230, 255) }
        };

        static GameData()
        {
            // Обычные (White) — 4 открыты со старта (syringe, hoof, bear, gasoline), 19 за достижения
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
            AddItem("dorans_blade", "Клинок Дорана", "DORAN", "white", "[LoL] +20 к макс. HP, +8% к урону и +2 HP вампиризма при ударе", Rgb(215, 175, 95));
            AddItem("boots_swiftness", "Ботинки Стремительности", "SWIFT", "white", "[LoL] +16% к скорости бега и устойчивость к замедлению", Rgb(235, 245, 255));
            AddItem("iron_branch", "Железная Ветвь", "BRNCH", "white", "[Dota 2] +12 к макс. HP, +6% к урону, +5% к атаке и +4% к скорости бега", Rgb(135, 215, 85));
            AddItem("golden_apple", "Золотое Яблоко", "GAPPL", "white", "[Minecraft] +20 к макс. HP; при падении HP < 50% даёт +35 Золотого Щита (раз в 12с)", Rgb(255, 215, 55));
            AddItem("sad_onion", "Грустная Луковица", "ONION", "white", "[Isaac] +18% к скорости атаки и брызги едких слёз при попадании", Rgb(215, 235, 175));
            AddItem("orirock_cluster", "Кластер Орирока", "ORIRK", "white", "[Arknights] +18 к макс. HP и снижает весь входящий урон на 3 ед. за стак", Rgb(145, 132, 112));

            // Необычные (Green) — 3 открыты со старта (leeching_seed, infusion, red_whip), 18 за достижения
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
            AddItem("sheen", "Сияние", "SHEEN", "green", "[LoL] Чародейский клинок: после навыка следующая атака наносит +100% урона", Rgb(75, 195, 255));
            AddItem("liandrys_torment", "Мучения Лиандри", "LIAND", "green", "[LoL] Атаки поджигают врагов духовным огнём (6% макс. HP цели + урон за 3с)", Rgb(235, 245, 255));
            AddItem("ludens_companion", "Спутник Людена", "LUDEN", "green", "[LoL] Движение и навыки копят заряды; на 100 выпускает 4 магические сферы", Rgb(185, 95, 255));
            AddItem("statikk_shiv", "Заточка Статикка", "SHIV", "green", "[LoL] Каждая 4-я атака поражает до 5 врагов цепной молнией Статикка", Rgb(255, 235, 75));
            AddItem("maelstrom", "Маэлстром", "MAELS", "green", "[Dota 2] 25% шанс выпустить синюю цепную молнию в 4 (+2 за стак) врагов (140% ур.)", Rgb(85, 195, 255));
            AddItem("ender_pearl", "Эндер-Жемчуг", "PEARL", "green", "[Minecraft] Навык [Shift] выпускает Нову Края (160% ур.), даёт +22 Щита и -12% КД Shift", Rgb(65, 215, 185));
            AddItem("holy_mantle", "Святая Мантия", "MANTL", "green", "[Isaac] Святой щит полностью блокирует 1 удар раз в 18 сек. (-20% КД за стак)", Rgb(135, 215, 255));
            AddItem("poca_sp_battery", "Батарея SP Родоса", "SPBAT", "green", "[Arknights] Каждая 4-я атака сокращает КД навыков на 0.8с и усиливает след. навык (+15%)", Rgb(95, 245, 195));

            // Легендарные (Red) — 2 открыты со старта (behemoth, clover), 12 за достижения
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
            AddItem("blade_ruined_king", "Клинок Падшего Короля", "BORK", "red", "[LoL] Удары наносят +6% от текущего HP врага, лечат и крадут скорость бега", Rgb(65, 245, 195));
            AddItem("rabadons_deathcap", "Смертельная Шляпа Рабадона", "RABA", "red", "[LoL] Увеличивает весь наносимый урон атак и способностей на +35%", Rgb(215, 45, 75));
            AddItem("infinity_edge", "Грань Бесконечности", "IE", "red", "[LoL] +15% к шансу крита и увеличивает критический урон на +45%", Rgb(255, 195, 55));
            AddItem("zhonyas_hourglass", "Песочные Часы Жони", "ZHON", "red", "[LoL] При падении HP < 30% включает Золотой Стазис (2.5с неуязвимости, щит и лечение)", Rgb(255, 215, 65));
            AddItem("black_king_bar", "Посох Чёрного Короля", "BKB", "red", "[Dota 2] Ультимейт [R] включает Аватар на 3.5с (+1с/стак): -50% урона, иммунитет к контролю и +30% ур.", Rgb(255, 205, 55));
            AddItem("totem_undying", "Тотем Бессмертия", "TOTEM", "red", "[Minecraft] Спасает от гибели 1 раз за этап на стак (50% HP + 50% Щита + золотая взрывная волна)", Rgb(255, 225, 75));
            AddItem("brimstone", "Сера (Brimstone)", "BRIMS", "red", "[Isaac] Каждая 6-я атака выжигает линию пробивным кровавым лазером Бездны (320% ур.)", Rgb(235, 35, 55));
            AddItem("silverash_truesilver", "Истинно-Серебряный Разрез", "TRUSL", "red", "[Arknights] Каждые 2.8с обрушивает 3 серебряные волны по 5 врагам (210% ур. и раскол брони)", Rgb(225, 240, 255));

            // Пустота (Void) — все 8 открыты со старта
            AddItem("plasma_shrimp", "Плазменная Креветка", "SHRM", "void", "[ПУСТОТА] Пока есть щит (+20 щита), каждая атака пускает ракету (40% ур.)", Rgb(195, 95, 255), isVoid: true);
            AddItem("polylute", "Полилютня Бездны", "PLUTE", "void", "[ПУСТОТА] 25% шанс обрушить на цель серию из 3 ударов молнии Бездны", Rgb(215, 75, 255), isVoid: true);
            AddItem("safer_spaces", "Сфера Бездны", "SPACE", "void", "[ПУСТОТА] Гарантированно блокирует любой удар раз в 12 сек. (-15% КД за стак)", Rgb(180, 90, 255), isVoid: true);
            AddItem("lysate_cell", "Клетка Бездны", "CELL", "void", "[ПУСТОТА] Сокращает перезарядку Ультимейта [R] на 25% за стак", Rgb(225, 110, 255), isVoid: true);
            AddItem("desolator_void", "Опустошитель Бездны", "DESOL", "void", "[ПУСТОТА / Dota 2] Удары накладывают Порчу Бездны (+8% получаемого урона за заряд до +40%)", Rgb(235, 55, 115), isVoid: true);
            AddItem("sculk_catalyst", "Скалк-Катализатор", "SCULK", "void", "[ПУСТОТА / MC] Убийства создают зону Скалка (замедляет и бьёт врагов на 95% ур./с, +8 Щита)", Rgb(35, 195, 205), isVoid: true);
            AddItem("maw_of_void", "Пасть Пустоты", "MAW", "void", "[ПУСТОТА / Isaac] Каждые 3.5с вокруг героя пульсирует чёрно-алое кольцо Бездны (180% ур. и щит)", Rgb(215, 45, 95), isVoid: true);
            AddItem("seaborne_ichor", "Ихор Морских Рождений", "ICHOR", "void", "[ПУСТОТА / Arknights] Адаптация Морей: +2.5 HP/с регена за стак и шипы щупалец по атакующим", Rgb(95, 135, 255), isVoid: true);

            // Лунные (Lunar) — 4 открыты со старта, 5 за достижения
            AddItem("shaped_glass", "Лунное Стекло", "GLASS", "lunar", "[ЛУННЫЙ] Урон x2 (+100%), но макс. HP снижено на 50%", Rgb(70, 175, 255), isLunar: true);
            AddItem("transcendence", "Жук Превосходства", "TRANS", "lunar", "[ЛУННЫЙ] +50% макс. HP и мощный регенерирующий щит", Rgb(65, 195, 255), isLunar: true);
            AddItem("purity", "Чистота", "PURE", "lunar", "[ЛУННЫЙ] Перезарядка всех навыков -2 сек., но удача ниже (-25% к шансам проков)", Rgb(140, 225, 255), isLunar: true);
            AddItem("lunar_pauldron", "Каменный Наплечник", "FLUX", "lunar", "[ЛУННЫЙ] Макс. HP +100% (x2), но скорость бега -30%", Rgb(90, 160, 255), isLunar: true);
            AddItem("spinel_tonic", "Лунный Тоник", "TONIC", "lunar", "[ЛУННЫЙ] На 25с все статы +25% (очищает старые тоники), затем портится!", Rgb(55, 140, 255), isLunar: true);
            AddItem("armlet_mordiggian", "Армлет Мордиггиана", "ARMLT", "lunar", "[ЛУННЫЙ / Dota 2] +65% к урону и +35% к скорости атаки, но в бою отнимает 2.5% HP/с (до 1 HP)", Rgb(215, 75, 145), isLunar: true);
            AddItem("elytra_curse", "Проклятые Элитры", "ELYTR", "lunar", "[ЛУННЫЙ / MC] +2 воздушных прыжка и +28% к скорости бега, но макс. HP снижено на 20%", Rgb(125, 165, 245), isLunar: true);
            AddItem("damocles", "Дамоклов Меч", "DAMOC", "lunar", "[ЛУННЫЙ / Isaac] Каждый сундук роняет 2 предмета! Но удар по HP с шансом 3% обрушивает меч (1 HP)", Rgb(195, 225, 255), isLunar: true);
            AddItem("originium_prime", "Чистый Ориджиниум", "OPRIM", "lunar", "[ЛУННЫЙ / Arknights] Перезарядка всех навыков -35% и урон навыков +40%, но входящий урон +25%", Rgb(255, 195, 75), isLunar: true);
            AddItem("tonic_affliction", "Испорченный Тоник", "AFLT", "lunar", "[ПРОКЛЯТИЕ ТОНИКА] Все характеристики (-Урон, -HP, -Атака, -Бег) снижены на 12% за стак", Rgb(95, 70, 135), isLunar: false);

            foreach (var ach in Achievements.Values)
            {
                if (!string.IsNullOrEmpty(ach.UnlocksItem) && ItemPool.TryGetValue(ach.UnlocksItem, out var lockedItem))
                    lockedItem.DefaultUnlocked = false;
            }

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

            var rawList = ItemsByTier.TryGetValue(tier, out var cands) && cands.Count > 0 ? cands : ItemsByTier["white"];
            var unlockedList = rawList.Where(k => ProgressData.Instance.UnlockedItems.Contains(k)).ToList();
            var list = unlockedList.Count > 0 ? unlockedList : rawList;
            return list[_rng.Next(list.Count)];
        }
    }
}
