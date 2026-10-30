using System;
using System.Collections.Generic;

namespace RiskOfCSharp2.Data
{
    public struct DialogueLine
    {
        public string SpeakerKey;   // charKey or personaKey of the speaker
        public string SpeakerTitle; // Display name override if needed, or "" to use default character name
        public string Text;         // Spoken line in Russian

        public DialogueLine(string speakerKey, string text, string speakerTitle = "")
        {
            SpeakerKey = speakerKey;
            Text = text;
            SpeakerTitle = speakerTitle;
        }
    }

    public class PersonaDialogueProfile
    {
        public string PersonaKey = "";
        public string DisplayName = "";
        public string RoleTag = "";           // Short in-universe role/weapon hook (used when others address them)
        public string CombatOpen = "";        // Line 1 when initiating tactical conversation (contains {0} for partner's DisplayName or RoleTag)
        public string CombatReply = "";       // Line 2 when replying in tactical conversation
        public string CombatFollow = "";      // Line 3 follow-up in tactical conversation
        public string CombatClose = "";       // Line 4 closing in tactical conversation
        public string ShipOpen = "";          // Line 1 about life/room aboard UES Safe Travels
        public string ShipReply = "";         // Line 2 reacting to partner's shipboard habit
        public string ShipFollow = "";        // Line 3 follow-up aboard Safe Travels
        public string ShipClose = "";         // Line 4 closing aboard Safe Travels
        public string MoonOpen = "";          // Line 1 reflecting on Mithrix / Moon / Petrichor V
        public string MoonReply = "";         // Line 2 replying about Mithrix / Moon
        public string MoonFollow = "";        // Line 3 follow-up on cosmic anomalies
        public string MoonClose = "";         // Line 4 closing before next drop
        public string[] FollowUpQuips = Array.Empty<string>();
    }

    public static partial class SafeTravelsDialogues
    {
        // All 39 distinct character personas (30 standard + 1 secret "sanabi" + 5 full alt-kit characters + 2 iconic rival personas)
        public static readonly string[] AllPersonaKeys =
        {
            "commando", "artificer", "huntress", "buzz", "v1", "sol", "gwen", "enforcer",
            "scout", "m4a1", "medic", "aigis", "reimu", "mindustry", "slark", "tidehunter",
            "freeman", "dante", "cheburashka", "ahri", "lappland", "w_arknights", "gold_ship",
            "pushnoy", "freddy", "steve", "jotaro", "vyper", "haze", "dmc_trio", "sanabi", "jack_o",
            // Alternative character versions (counted as separate characters!)
            "sol_ky", "lappland_decadenza", "w_wisadel", "dmc_nero", "dmc_vergil",
            "buzz_zurg", "v1_v2"
        };

        public static string CanonicalPair(string a, string b)
            => string.CompareOrdinal(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";

        public static string GetPersonaKey(string charKey, int skinIdx)
        {
            charKey ??= "commando";
            return charKey switch
            {
                "sol" when skinIdx == 2 => "sol_ky",
                "lappland" when skinIdx == 2 => "lappland_decadenza",
                "w_arknights" when skinIdx == 2 => "w_wisadel",
                "dmc_trio" when skinIdx == 1 => "dmc_nero",
                "dmc_trio" when skinIdx == 2 => "dmc_vergil",
                "buzz" when skinIdx == 2 => "buzz_zurg",
                "v1" when skinIdx == 2 => "v1_v2",
                _ => charKey
            };
        }

        public static (string BaseCharKey, int SkinIdx) SplitPersonaKey(string personaKey)
        {
            return personaKey switch
            {
                "sol_ky" => ("sol", 2),
                "lappland_decadenza" => ("lappland", 2),
                "w_wisadel" => ("w_arknights", 2),
                "dmc_nero" => ("dmc_trio", 1),
                "dmc_vergil" => ("dmc_trio", 2),
                "buzz_zurg" => ("buzz", 2),
                "v1_v2" => ("v1", 2),
                _ => (personaKey, 0)
            };
        }

        public static string GetPersonaDisplayName(string personaKey)
        {
            return personaKey switch
            {
                "commando" => "Коммандо",
                "artificer" => "Изобретатель",
                "huntress" => "Охотница",
                "buzz" => "Базз Лайтер",
                "buzz_zurg" => "Император Зург",
                "v1" => "V1",
                "v1_v2" => "V2",
                "sol" => "Сол Бэдгай",
                "sol_ky" => "Кай Киске",
                "jack_o" => "Джек-О Валентайн",
                "gwen" => "Гвен",
                "enforcer" => "Силовик",
                "scout" => "Разведчик",
                "m4a1" => "M4A1",
                "medic" => "Медик",
                "aigis" => "Айгис",
                "reimu" => "Хакурей Рейму",
                "mindustry" => "Ядро «Гамма»",
                "slark" => "Сларк",
                "tidehunter" => "Тайдхантер",
                "freeman" => "Гордон Фриман",
                "dante" => "Данте (Limbus)",
                "cheburashka" => "Чебурашка",
                "ahri" => "Ари",
                "lappland" => "Лаппланд",
                "lappland_decadenza" => "Лаппланд Декаденца",
                "w_arknights" => "W",
                "w_wisadel" => "Вишадель",
                "gold_ship" => "Голд Шип",
                "pushnoy" => "Александр Пушной",
                "freddy" => "Мишка Фредди",
                "steve" => "Стив",
                "jotaro" => "Джотаро Куджо",
                "vyper" => "Вайпер",
                "haze" => "Хейз",
                "dmc_trio" => "Данте (DMC)",
                "dmc_nero" => "Неро",
                "dmc_vergil" => "Вергилий",
                "sanabi" => "Генерал и Мари (SANABI)",
                _ => personaKey
            };
        }

        private static readonly Dictionary<string, List<DialogueLine[]>> _pairDialogues = BuildAllPairDialogues();
        private static readonly Dictionary<string, List<DialogueLine[]>> _mirrorDialogues = BuildAllMirrorDialogues();
        private static readonly Dictionary<string, PersonaDialogueProfile> _profiles = BuildAllPersonaProfiles();

        private static Dictionary<string, List<DialogueLine[]>> BuildAllPairDialogues()
        {
            var dict = new Dictionary<string, List<DialogueLine[]>>(1024, StringComparer.Ordinal);
            RegisterPairsPart1(dict);
            RegisterPairsPart2(dict);
            RegisterPairsPart3(dict);
            RegisterAltPersonaPairs(dict);
            RegisterCollegePairs(dict);
            return dict;
        }

        private static Dictionary<string, List<DialogueLine[]>> BuildAllMirrorDialogues()
        {
            var dict = new Dictionary<string, List<DialogueLine[]>>(64, StringComparer.Ordinal);
            RegisterMirrorDialogues(dict);
            RegisterCollegeMirrors(dict);
            return dict;
        }

        private static Dictionary<string, PersonaDialogueProfile> BuildAllPersonaProfiles()
        {
            var dict = new Dictionary<string, PersonaDialogueProfile>(48, StringComparer.Ordinal);
            RegisterPersonaProfiles(dict);
            RegisterCollegeQuipsAndJackO(dict);
            return dict;
        }

        public static void AddPair(Dictionary<string, List<DialogueLine[]>> dict, string firstPersona, string secondPersona,
            string line1, string line2, string line3, string line4)
        {
            string key = CanonicalPair(firstPersona, secondPersona);
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<DialogueLine[]>(4);
                dict[key] = list;
            }
            (string base1, _) = SplitPersonaKey(firstPersona);
            (string base2, _) = SplitPersonaKey(secondPersona);
            list.Add(new[]
            {
                new DialogueLine(base1, line1, GetPersonaDisplayName(firstPersona)),
                new DialogueLine(base2, line2, GetPersonaDisplayName(secondPersona)),
                new DialogueLine(base1, line3, GetPersonaDisplayName(firstPersona)),
                new DialogueLine(base2, line4, GetPersonaDisplayName(secondPersona))
            });
        }

        public static void AddMirror(Dictionary<string, List<DialogueLine[]>> dict, string personaKey,
            string line1, string line2, string line3, string line4)
        {
            if (!dict.TryGetValue(personaKey, out var list))
            {
                list = new List<DialogueLine[]>(3);
                dict[personaKey] = list;
            }
            (string baseKey, _) = SplitPersonaKey(personaKey);
            string mainName = GetPersonaDisplayName(personaKey);
            string twinName = $"{mainName} (Двойник)";
            list.Add(new[]
            {
                new DialogueLine(baseKey, line1, mainName),
                new DialogueLine(baseKey, line2, twinName),
                new DialogueLine(baseKey, line3, mainName),
                new DialogueLine(baseKey, line4, twinName)
            });
        }

        private static List<DialogueLine[]> BuildAllConversationsForPersonas(string playerPersona, string npcPersona)
        {
            var all = new List<DialogueLine[]>(5);

            if (string.Equals(playerPersona, npcPersona, StringComparison.Ordinal))
            {
                if (_mirrorDialogues.TryGetValue(playerPersona, out var mirrors))
                    all.AddRange(mirrors);
                return all;
            }

            // 1. Handcrafted pair-specific conversations (can be 1, 2, or more!)
            if (_pairDialogues.TryGetValue(CanonicalPair(playerPersona, npcPersona), out var handcrafted))
            {
                all.AddRange(handcrafted);
            }
            else
            {
                // Fallback to base character pair if an alt persona has no specific override for a minor pair, re-voicing via profiles below
                (string pBase, _) = SplitPersonaKey(playerPersona);
                (string nBase, _) = SplitPersonaKey(npcPersona);
                if (!string.Equals(pBase, nBase, StringComparison.Ordinal) &&
                    playerPersona == pBase && npcPersona == nBase &&
                    _pairDialogues.TryGetValue(CanonicalPair(pBase, nBase), out var baseHandcrafted))
                {
                    all.AddRange(baseHandcrafted);
                }
            }

            // 2. Additional full multi-turn conversations generated from both characters' rich canonical profiles
            // so EVERY pair has multiple distinct conversations (Combat Synergy, Shipboard Life on Safe Travels, Mithrix & Moon)!
            if (_profiles.TryGetValue(playerPersona, out var pProf) && _profiles.TryGetValue(npcPersona, out var nProf))
            {
                (string pBase, _) = SplitPersonaKey(playerPersona);
                (string nBase, _) = SplitPersonaKey(npcPersona);

                string FormatLine(string template, PersonaDialogueProfile target)
                {
                    if (string.IsNullOrEmpty(template)) return "...";
                    return template.Replace("{NAME}", target.DisplayName).Replace("{ROLE}", target.RoleTag);
                }

                // Conversation Theme A: Combat & Tactical Synergy
                all.Add(new[]
                {
                    new DialogueLine(pBase, FormatLine(pProf.CombatOpen, nProf), pProf.DisplayName),
                    new DialogueLine(nBase, FormatLine(nProf.CombatReply, pProf), nProf.DisplayName),
                    new DialogueLine(pBase, FormatLine(pProf.CombatFollow, nProf), pProf.DisplayName),
                    new DialogueLine(nBase, FormatLine(nProf.CombatClose, pProf), nProf.DisplayName)
                });

                // Conversation Theme B: Crew Quarters & Life Aboard UES «Safe Travels»
                all.Add(new[]
                {
                    new DialogueLine(nBase, FormatLine(nProf.ShipOpen, pProf), nProf.DisplayName),
                    new DialogueLine(pBase, FormatLine(pProf.ShipReply, nProf), pProf.DisplayName),
                    new DialogueLine(nBase, FormatLine(nProf.ShipFollow, pProf), nProf.DisplayName),
                    new DialogueLine(pBase, FormatLine(pProf.ShipClose, nProf), pProf.DisplayName)
                });

                // Conversation Theme C: Reflections on Mithrix, the Shattered Moon & Petrichor V
                all.Add(new[]
                {
                    new DialogueLine(pBase, FormatLine(pProf.MoonOpen, nProf), pProf.DisplayName),
                    new DialogueLine(nBase, FormatLine(nProf.MoonReply, pProf), nProf.DisplayName),
                    new DialogueLine(pBase, FormatLine(pProf.MoonFollow, nProf), pProf.DisplayName),
                    new DialogueLine(nBase, FormatLine(nProf.MoonClose, pProf), nProf.DisplayName)
                });
            }

            return all;
        }

        public static int GetConversationCount(string playerKey, int playerSkinIdx, string npcKey, int npcSkinIdx = 0)
        {
            string pPersona = GetPersonaKey(playerKey, playerSkinIdx);
            string nPersona = GetPersonaKey(npcKey, npcSkinIdx);
            int count = BuildAllConversationsForPersonas(pPersona, nPersona).Count;
            return Math.Max(1, count);
        }

        public static List<DialogueLine> GetConversation(string playerKey, int playerSkinIdx, string npcKey, int npcSkinIdx = 0, int visitCount = 0)
        {
            string pPersona = GetPersonaKey(playerKey, playerSkinIdx);
            string nPersona = GetPersonaKey(npcKey, npcSkinIdx);

            var convs = BuildAllConversationsForPersonas(pPersona, nPersona);
            DialogueLine[] chosen;
            if (convs.Count > 0)
            {
                int idx = Math.Abs(visitCount) % convs.Count;
                chosen = convs[idx];
            }
            else
            {
                (string pBase, _) = SplitPersonaKey(pPersona);
                (string nBase, _) = SplitPersonaKey(nPersona);
                chosen = new[]
                {
                    new DialogueLine(nBase, "Системы «Safe Travels» фиксируют стабильную орбиту над Petrichor V.", GetPersonaDisplayName(nPersona)),
                    new DialogueLine(pBase, "Принято. Проверяю снаряжение перед следующей высадкой.", GetPersonaDisplayName(pPersona)),
                    new DialogueLine(nBase, "Телепорт внизу долго ждать не будет.", GetPersonaDisplayName(nPersona)),
                    new DialogueLine(pBase, "Тогда увидимся в десантном отсеке.", GetPersonaDisplayName(pPersona))
                };
            }

            var result = new List<DialogueLine>(chosen.Length + 1);
            result.AddRange(chosen);

            // If player has cycled through all full conversations and keeps chatting, append a fresh character-specific bonus quip!
            if (convs.Count > 0 && visitCount >= convs.Count && _profiles.TryGetValue(nPersona, out var nProf) && nProf.FollowUpQuips.Length > 0)
            {
                (string nBase, _) = SplitPersonaKey(nPersona);
                string quip = nProf.FollowUpQuips[(visitCount - convs.Count) % nProf.FollowUpQuips.Length];
                string closingTitle = string.Equals(pPersona, nPersona, StringComparison.Ordinal)
                    ? $"{nProf.DisplayName} (Двойник)"
                    : nProf.DisplayName;
                result.Add(new DialogueLine(nBase, quip, closingTitle));
            }

            return result;
        }

        public static string GetRpsReaction(string npcKey, bool npcWon, bool isDraw, int npcSkinIdx = 0)
        {
            string persona = GetPersonaKey(npcKey, npcSkinIdx);
            if (isDraw)
            {
                return persona switch
                {
                    "commando" => "Ничья. Стандартный паритет сил — перебрасываем по счёту три.",
                    "artificer" => "Квантовая симметрия жестов! Какова вероятность повторения — одна треть?",
                    "huntress" => "Одновременный выпад. У тебя быстрые рефлексы.",
                    "buzz" => "Боевая ничья, рейнджер! Наши тактические компьютеры мыслят одинаково!",
                    "buzz_zurg" => "Ничья?! Даже Тёмная Империя Зурга удивлена такой синхронностью! Переиграть!",
                    "v1" => "[СТАТУС: ПАРИРОВАНИЕ ЖЕСТА // НИЧЬЯ. МОНЕТКА НЕ ПОТРАЧЕНА.]",
                    "v1_v2" => "[ДУЭЛЬ В ПАРИТЕТЕ // ЖЕСТЫ СОВПАЛИ. ПОВТОРНЫЙ РАУНД НЕМЕДЛЕННО.]",
                    "sol" => "Тц, одинаково выкинули. Давай ещё раз, только без занудства.",
                    "sol_ky" => "Достойный паритет, прямо как на рыцарском турнире Иллирии! Попробуем ещё раз?",
                    "jack_o" => "Ой, одинаково! Слуги, поднимите таблички «НИЧЬЯ» — переигрываем на конфетку!",
                    "gwen" => "Ой, мы оба выбрали одно и то же! Хотя я всё равно больше люблю ножницы — чик-чик!",
                    "enforcer" => "Щит в щит. Строй не пробит — повторяем раунд.",
                    "scout" => "Ничья! Ха, если оба покажем Камень — выпьем за Скалу и Камень!",
                    "m4a1" => "Совпадение тактических алгоритмов. Попробуем ещё одну итерацию?",
                    "medic" => "Аха-ха! Одинаковый диагноз! Ничего, щас переиграем на твою селезёнку!",
                    "aigis" => "Зафиксировано совпадение жестов. Это означает высокий уровень нашей синхронизации!",
                    "reimu" => "Ничья... Эх, даже монетку в ящик для пожертвований так не выиграешь.",
                    "mindustry" => "[ЛОГИСТИКА: БАЛАНС 1:1. ПРОПУСКНАЯ СПОСОБНОСТЬ РАВНА. ПОВТОР ЦИКЛА.]",
                    "slark" => "Хех, читали руки друг друга, как шулера в Тёмном Рифе! Ещё разок!",
                    "tidehunter" => "Ничья?! Будь ты Кунккой, я бы уже перевернул стол якорем!",
                    "freeman" => "... *молча смотрит на две одинаковые ладони, поправляет очки и кивает на переигровку*",
                    "dante" => "<Тик-так... Тик!> (Ничья! Фух, хоть не придётся крутить стрелки назад ради отыгрыша.)",
                    "cheburashka" => "Ой, у нас одинаково получилось! Значит, победила дружба и чебурашечье равенство!",
                    "ahri" => "М-м, наши мысли совпали~ Кажется, я нечаянно заглянула в твою память!",
                    "lappland" => "Ха-ха! Клинок в клинок! Обожаю, когда дуэль затягивается!",
                    "lappland_decadenza" => "Браво! Наш карнавальный ритм совпал нота в ноту! Ещё один такт?",
                    "w_arknights" => "Одинаково? Скукота! В Казделе ничью обычно решают чекой от гранаты~",
                    "w_wisadel" => "Ничья? Даже духи предков в моей пушке сейчас поспорили, кто кого перехитрил!",
                    "gold_ship" => "ФОТОФИНИШ НОЗДРЯ В НОЗДРЮ! Требую видеоповтор и порцию якисобы!",
                    "pushnoy" => "О-о-о, резонанс частот! Друзья, это чистая теория вероятностей в прямом эфире!",
                    "freddy" => "Хар-хар-хар! Ничья! Часы ещё не пробили шесть утра, играем дальше!",
                    "steve" => "Ничья. Как два булыжника в одном слоте крафта — давай по новой.",
                    "jotaro" => "Ярэ-ярэ... Даже Star Platinum показал тот же знак в ту же миллисекунду.",
                    "vyper" => "Эй-эй, ничья! Клянусь хвостом, я не подглядывала в отражение твоего визора!",
                    "haze" => "Любопытно... Твоё подсознание выбрало тот же символ, что и моё.",
                    "dmc_trio" => "Ничья! Стиль на уровне Royal Guard — давай ещё партию, без дураков!",
                    "dmc_nero" => "Эй, одновременно! У меня аж протез заискрил от азарта — давай ещё раз!",
                    "dmc_vergil" => "Хм. Абсолютное равенство концентрации. Завершим этот поединок как положено.",
                    "sanabi" => "Генерал: Паритет. — Мари: Эй, я же просчитала твой жест, как ты успел поменять?!",
                    _ => "Ничья! Перебрасываем."
                };
            }

            if (npcWon)
            {
                return persona switch
                {
                    "commando" => "Есть попадание. В пехоте UES учат читать движение кисти за долю секунды.",
                    "artificer" => "Элементарный расчёт теории игр! Высший Суд не проигрывает в тривиальной комбинаторике.",
                    "huntress" => "Твоё плечо дрогнуло перед броском. Добыча всегда выдаёт намерение.",
                    "buzz" => "Звёздное Командование одержало тактическую победу! Не вешай нос, кадет!",
                    "buzz_zurg" => "МУА-ХА-ХА! Склонись перед тактическим гением Императора Зурга!",
                    "v1" => "[+ УЛЬТРАПОБЕДА // СЧЁТ В МОЮ ПОЛЬЗУ. ВЫДАЙТЕ МОНЕТКУ.]",
                    "v1_v2" => "[ПРЕВОСХОДСТВО МОДЕЛИ V2 ДОКАЗАНО // СЧЁТ В МОЮ ПОЛЬЗУ.]",
                    "sol" => "Ха! Читаешься как дешёвый учебник физики. С тебя виниловая пластинка.",
                    "sol_ky" => "Честная победа! Не огорчайся — в фехтовании чтение ритма решает исход до замаха.",
                    "jack_o" => "Ура-а, я победила! Хе-хе, Ария внутри подсказывает, что с тебя леденец с яблочным вкусом!",
                    "gwen" => "Ура, я выиграла! И заметь — я честно показала не только ножницы!",
                    "enforcer" => "Победа за щитом правопорядка. Можешь подать апелляцию в следующем раунде.",
                    "scout" => "ХА! Скала бьёт ножницы, а дварф бьёт рекорд! С тебя кружка «Жирного Кабана» у Ллойда!",
                    "m4a1" => "Победа расчётного модуля AR. Простите, командир, я просто проанализировала ваш ритм.",
                    "medic" => "Вундербар! Я победил! А теперь ложись на кушетку, проверим твой позвоночник!",
                    "aigis" => "Победа зафиксирована! Моё ядро Папирус испытывает странное тёплое чувство триумфа!",
                    "reimu" => "Ага, моя взяла! Так и быть, принимаю проигрыш наличными в храмовый ящик Хакурей.",
                    "mindustry" => "[ОПТИМИЗАЦИЯ УСПЕШНА: КПД ПОБЕДЫ 100%. ВАША СТРАТЕГИЯ ЗАШЛА В МАРШРУТИЗАТОР.]",
                    "slark" => "Хе-хе-хе! В Тёмном Рифе на таких руках я выигрывал по три заточки за вечер!",
                    "tidehunter" => "БУА-ХА-ХА! Мощь океана сокрушила тебя, как шлюпку паршивого адмирала Кункки!",
                    "freeman" => "... *самодовольно поправляет очки и победно стучит монтировкой о ладонь* [КОСТЮМ HEV: Триумф подтверждён.]",
                    "dante" => "<Тик-так-тик!> (Ого, я реально выиграл?! Вот бы в казино J-Корпорации мне так везло!)",
                    "cheburashka" => "Ура, я выиграл! Но ты не расстраивайся — держи половинку апельсина за участие!",
                    "ahri" => "Хи-хи, победа за лисичкой~ Твои глаза выдали секрет раньше пальцев!",
                    "lappland" => "Аха-ха-ха! Слишком медленно! Вот так в Сиракузах и решают судьбу целых семей!",
                    "lappland_decadenza" => "Аплодисменты! Занавес опускается в пользу Синьоров Волка, дорогой друг!",
                    "w_arknights" => "Бум! Ты проиграл! Радуйся, что на кону стояла не расписка о разминировании~",
                    "w_wisadel" => "Ха! Председатель Каздела не проигрывает даже в детские считалки! Не дуйся~",
                    "gold_ship" => "ПЕРВОЕ МЕСТО НА ГРАН-ПРИ! А теперь — победный дропкик чемпиону... шутка! Наверное!",
                    "pushnoy" => "ЕСТЬ КОНТАКТ! Наука и рок-н-ролл снова доказали своё превосходство! *берёт победный аккорд*",
                    "freddy" => "Хар-хар-хар-хар! Пять ночей — и победа за Фазбером! Пицца за твой счёт!",
                    "steve" => "Ага! Алмазная рука не подводит. С тебя три изумруда, как у носатых жителей.",
                    "jotaro" => "Ярэ-ярэ дазэ. Твой выбор был очевиден ещё до того, как ты разжал кулак.",
                    "vyper" => "ХА! Выкуси! Ручки-то помнят нью-йоркскую школу! Так что мы там ставили на кон?",
                    "haze" => "Победа. Не удивляйся — в твоих снах этот жест повторяется слишком часто.",
                    "dmc_trio" => "Джекпот! Ранг стиля — Smokin' Sexy Style! Можешь угостить меня клубничным пломбиром.",
                    "dmc_nero" => "ХА, ВЫКУСИ! Видел бы это Данте — а теперь кто тут у нас номер один?!",
                    "dmc_vergil" => "Предсказуемый исход. Тебе не хватило мотивации.",
                    "sanabi" => "Мари: УРА, МЫ ВЫИГРАЛИ! Я же говорила — ставь бумагу, пап! — Генерал: Хм. Тактический успех.",
                    _ => "Моя взяла! Отличная партия."
                };
            }

            return persona switch
            {
                "commando" => "Чёрт, просчитался. Ладно, признаю поражение — реакция у тебя отменная.",
                "artificer" => "Статистическая аномалия! Я требую серию из ста испытаний для проверки выборки.",
                "huntress" => "...Чистый перехват. В этот раз охотником оказался ты.",
                "buzz" => "Бортовой журнал: противник применил хитрый галактический манёвр! Честная победа, партнёр!",
                "buzz_zurg" => "Проклятье! Мои сенсоры ослепил свет палубных ламп! Империя требует реванша!",
                "v1" => "[ОШИБКА ПРОГНОЗА // ПОРАЖЕНИЕ. ПЕРЕКАЛИБРОВКА СЕРВОПРИВОДОВ КИСТИ...]",
                "v1_v2" => "[НЕДОПУСТИМЫЙ СБОЙ // ТЫ ПРОСТО УГАДАЛ! ТРЕБУЮ РЕВАНША НА ПОЛИГОНЕ!]",
                "sol" => "Тц... Ну и ладно, больно надо было. Просто рука после ковки меча затекла.",
                "sol_ky" => "Блестящий ход! Признаю поражение с честью — ваша интуиция выше всяких похвал.",
                "jack_o" => "А-ах, проиграла! Ой, у моего миньончика от расстройства задымился фитиль — бежим скорее!",
                "gwen" => "Ах, мои ножницы затупились об твой ход! Как здорово ты играешь, научишь меня?",
                "enforcer" => "Пробил оборону по всем правилам. Уважаю чёткую работу.",
                "scout" => "Ах ты ж глифид хитроумный! Ладно, твоя взяла — пойду закажу нам по пенному!",
                "m4a1" => "Ой... мой модуль прогнозирования ошибся. Поздравляю с победой, это было красиво.",
                "medic" => "Майн Готт! Как ты это сделал?! Неважно — за такое везение я подарю тебе голубя... почти чистого!",
                "aigis" => "Я потерпела поражение. Сохраняю этот ценный опыт человеческих игр в долговременную память!",
                "reimu" => "Эй, ты точно не мухлюешь магией границ Юкари?! Тц, ладно, чай за мой счёт.",
                "mindustry" => "[СБОЙ СХЕМЫ: ЗАТОР НА КОНВЕЙЕРЕ РЕШЕНИЙ. ПРИЗНАЮ ВАШЕ ПРЕВОСХОДСТВО.]",
                "slark" => "Тьфу ты, обставил ночного проныру! Точно краплёными пальцами не пользуешься?",
                "tidehunter" => "Р-р-аргх! Проклятье морей! Ты жульничаешь почти так же нагло, как Кункка!",
                "freeman" => "... *удивлённо приподнимает бровь за очками, вздыхает и уважительно пожимает тебе руку*",
                "dante" => "<Тик... так...> (Эх, проиграл... Прямо как когда Родион уговаривает меня сыграть в карты.)",
                "cheburashka" => "Ой, ты победил! Какой ты молодец! Гена говорит, уметь радоваться за друга — самое главное!",
                "ahri" => "Оу, перехитрил девятихвостую лису? Пожалуй, ты заслужил маленькую награду~",
                "lappland" => "Ха! Великолепный удар прямо в сердце моей гордости! Давай ещё — пока кровь кипит!",
                "lappland_decadenza" => "Охо-хо, переиграть хозяйку маскарада на её же сцене! Моё почтение, маэстро!",
                "w_arknights" => "Ого, обыграл меня? Смелый ход... Проверь потом под подошвой, не прилипла ли карточка с C4, хи-хи~",
                "w_wisadel" => "Тц, да это старики-ревенанты под руку каркнули! Считай, тебе крупно повезло~",
                "gold_ship" => "ЧЕГО?! Фальстарт! Ветер встречный! Ипподром квадратный! Требую реванш на тысячу метров!",
                "pushnoy" => "ВОТ ЭТО ПОВОРОТ! Эксперимент пошёл не по сценарию ведущего! Аплодисменты победителю!",
                "freddy" => "Бззт... Ошибка сервопривода лапы! Поздравляем именинника с победой от лица Fazbear Entertainment!",
                "steve" => "Уф... Как крипер из-за спины. Держи честный хлеб за победу.",
                "jotaro" => "...Хм. Неплохо. Успел перестроиться за долю секунды — уважаю выдержку.",
                "vyper" => "Ай, да ладно?! У меня просто чешуя на пальцах скользкая! Давай два из трёх, по-братски!",
                "haze" => "Хм... Редкий случай: твой разум оказался совершенно непредсказуем. Браво.",
                "dmc_trio" => "Воу, сбил мне весь комбо-счётчик до ранга D! Красиво подловил, ничего не скажешь.",
                "dmc_nero" => "Аргх, чёрт! Нико мне со смеху прохода не даст! Давай реванш до трёх побед!",
                "dmc_vergil" => "...Любопытно. Ты сумел прочитать моё намерение раньше удара. Достойно уважения.",
                "sanabi" => "Мари: Э-э?! Папа, у тебя кибер-рука зависла, почему мы проиграли?! — Генерал: Противник прочитал финт. Достойно.",
                _ => "Твоя взяла! Чистая победа."
            };
        }
    }
}
