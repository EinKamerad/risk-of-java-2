using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using RiskOfCSharp2.Entities;
using RiskOfCSharp2.Models;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.World
{
    /// <summary>
    /// Единый режиссёр уникальных систем получения 44 выживших и 13 альтернативных личностей.
    /// Управляет цепочками этапов, вторжениями боссов, динамическими мировыми событиями,
    /// HUD-телеметрией и персональными испытаниями альтернативных личностей (ach_persona_*).
    /// </summary>
    public static partial class CharacterUnlockSystems
    {
        // --- Цепочка «Contact Light» (Коммандо: этапы 1 -> 2 -> 3) ---
        public static int CommandoBlackBoxChain = 0;

        // --- Телепорт: Охотница (HP >= 80% на 3+ этапе) и Силовик (0%..100% не выходя из круга) ---
        private static bool _tpTrackingActive = false;
        private static bool _tpHuntressEligible = false;
        private static bool _tpEnforcerStayInZone = false;

        // --- Босс Телепорта: Сол (Daredevil burst), DMC (Flawless <=25s), W, Неро, Вергилий, Длинная Птица ---
        private static CombatTarget? _trackedTpBoss = null;
        private static float _tpBossSpawnElapsed = 0f;
        private static bool _tookHpDamageDuringTpBoss = false;
        private static float _minHpFracDuringTpBoss = 1.0f;
        private static int _shiftCastsDuringTpBoss = 0;
        private static readonly List<(float Time, float Dmg)> _tpBossBurstWindow = new();
        private static bool _solChallengerSpawnedThisStage = false;
        private static CombatTarget? _activeSolChallenger = null;

        // --- Аид / Тёмный Час (Айгис 06:00) и Падение Метеорита (Джотаро 2+ этап) ---
        private static bool _darkHourTriggeredThisStage = false;
        private static CombatTarget? _activeDarkHourReaper = null;
        private static bool _meteorSpawnedThisRun = false;

        // --- PRTS HUD-глитч (Пристесс ◇) и Радиопомехи Parapluie (M4A1) ---
        public static int PrtsNodesSyncedThisStage = 0;
        public static int PrtsNodesTotalThisStage = 0;
        public static float NearestPrtsNodeDist = 999f;
        public static ParapluieJammerDirector? ActiveParapluieJammer = null;

        // --- Стремительный галоп Росинанта (+75% скорости для Дон Кихот) и Аура Безмолвия Лаппланд ---
        public static float RocinanteGallopTimer = 0f;
        public static float SilenceDomeTimer = 0f;
        public static float TemporaryChainHookTimer = 0f;

        // --- Персональные трекеры 13 альтернативных личностей и Медика ---
        public static float MedicAllyHealAccum = 0f;
        private static readonly List<float> _lapplandKillTimestamps = new();
        private static int _lapplandSilencedEliteKills = 0;
        private static float _smallBirdRevengeWindow = 0f;
        private static int _smallBirdRevengeKills = 0;
        private static float _koishiCombatUntouchedTimer = 0f;

        // --- Активные HUD-виджеты уникальных испытаний ---
        public static HeliosRhythmDjEncounter? ActiveRhythmEncounter = null;
        public static DarkSanctuaryRudeBusterEncounter? ActiveSusieEncounter = null;
        public static FazbearSecurityOfficeEncounter? ActiveFazbearOffice = null;

        public static void ResetForNewRun()
        {
            CommandoBlackBoxChain = 0;
            _meteorSpawnedThisRun = false;
            MedicAllyHealAccum = 0f;
            _lapplandKillTimestamps.Clear();
            _lapplandSilencedEliteKills = 0;
            _smallBirdRevengeWindow = 0f;
            _smallBirdRevengeKills = 0;
            _koishiCombatUntouchedTimer = 0f;
            ResetForNewStage();
        }

        public static void ResetForNewStage()
        {
            _tpTrackingActive = false;
            _tpHuntressEligible = false;
            _tpEnforcerStayInZone = false;
            _trackedTpBoss = null;
            _tpBossSpawnElapsed = 0f;
            _tookHpDamageDuringTpBoss = false;
            _minHpFracDuringTpBoss = 1.0f;
            _shiftCastsDuringTpBoss = 0;
            _tpBossBurstWindow.Clear();
            _solChallengerSpawnedThisStage = false;
            _activeSolChallenger = null;
            _darkHourTriggeredThisStage = false;
            _activeDarkHourReaper = null;
            PrtsNodesSyncedThisStage = 0;
            PrtsNodesTotalThisStage = 0;
            NearestPrtsNodeDist = 999f;
            ActiveParapluieJammer = null;
            RocinanteGallopTimer = 0f;
            SilenceDomeTimer = 0f;
            TemporaryChainHookTimer = 0f;
            ActiveRhythmEncounter = null;
            ActiveSusieEncounter = null;
            ActiveFazbearOffice = null;
        }

        public static bool IsCharacterLockedOrNeedsQuest(string charKey)
        {
            var prog = ProgressData.Instance;
            if (prog.UnlockedAchievements.Contains($"ach_char_{charKey}")) return false;
            if (charKey == "commando" || charKey == "v1") return true;
            return !prog.IsCharacterUnlocked(charKey);
        }

        public static bool IsTeleporterJammed(out string reason)
        {
            if (ActiveParapluieJammer != null && IsValid(ActiveParapluieJammer) && !ActiveParapluieJammer.Completed)
            {
                reason = $"РАДИОПОМЕХИ PARAPLUIE! ОСТАЛОСЬ ГЛУШИТЕЛЕЙ НА КАРТЕ: {ActiveParapluieJammer.RemainingRelays}/3";
                return true;
            }
            reason = "";
            return false;
        }

        public static Vector3 FindSafeStageSpot(Vector3 desired, float radius = 1.8f, float clearance = 3.6f)
        {
            Vector3 p = Physics.FindSafeSpawnPoint(desired, 18f, radius, clearance);
            Physics.RegisterSpawnBlocker(p, radius + 1.0f, 3.0f);
            return p;
        }

        // =========================================================================
        // ГЕНЕРАЦИЯ УНИКАЛЬНЫХ СОБЫТИЙ НА БАЗАРЕ, СЕКРЕТНЫХ АРЕНАХ И ОБЫЧНЫХ ЭТАПАХ
        // =========================================================================
        public static void PopulateBazaarUnlocks(List<Entity> entitiesList)
        {
            if (IsCharacterLockedOrNeedsQuest("artificer"))
            {
                Vector3 cPos = FindSafeStageSpot(new Vector3(14f, 0f, 16f), 1.6f, 3.4f);
                entitiesList.Add(new ArtificerThermalCrystal(cPos));
            }
            if (IsCharacterLockedOrNeedsQuest("freddy"))
            {
                Vector3 aPos = FindSafeStageSpot(new Vector3(-16f, 0f, 15f), 1.2f, 2.8f);
                entitiesList.Add(new BazaarSecretTrigger(aPos, "pizzeria",
                    "[E] Сломанный Аркадный Автомат «Fazbear 1987»", Rgb(225, 145, 45), breakable: false));
            }
            if (IsCharacterLockedOrNeedsQuest("freeman"))
            {
                Vector3 vPos = FindSafeStageSpot(new Vector3(-19f, 0f, 5f), 1.2f, 2.8f);
                entitiesList.Add(new BazaarSecretTrigger(vPos, "black_mesa",
                    "Вентиляционная Решётка Сектора C [Разбейте ударом!]", Color.Orange, breakable: true));
            }
        }

        public static void PopulateSecretArenaUnlocks(List<Entity> entitiesList, string specialMode, int stageNum)
        {
            Vector3 centerQuest = FindSafeStageSpot(new Vector3(0f, 0f, 14f), 1.8f, 3.6f);
            Vector3 leftQuest = FindSafeStageSpot(new Vector3(-11f, 0f, 14f), 1.8f, 3.6f);
            Vector3 rightQuest = FindSafeStageSpot(new Vector3(11f, 0f, 14f), 1.8f, 3.6f);

            switch (specialMode)
            {
                case "pizzeria":
                    if (IsCharacterLockedOrNeedsQuest("freddy"))
                        entitiesList.Add(new FazbearSecurityOfficeEncounter(centerQuest));
                    break;
                case "black_mesa":
                    if (IsCharacterLockedOrNeedsQuest("freeman"))
                        entitiesList.Add(new BlackMesaSpectrometerEncounter(centerQuest));
                    break;
                case "qliphoth":
                    if (IsCharacterLockedOrNeedsQuest("dmc_trio"))
                        entitiesList.Add(new QliphothStyleDuelEncounter(centerQuest));
                    break;
                case "hell_cybergrind":
                    if (IsCharacterLockedOrNeedsQuest("v1"))
                        entitiesList.Add(new CyberGrindArenaEncounter(centerQuest));
                    break;
                case "tartarus":
                    if (IsCharacterLockedOrNeedsQuest("aigis"))
                        entitiesList.Add(new DarkHourReaperEncounter(centerQuest));
                    break;
                case "hakurei_shrine":
                    if (IsCharacterLockedOrNeedsQuest("reimu"))
                        entitiesList.Add(new HakureiGrazeEncounter(leftQuest));
                    if (IsCharacterLockedOrNeedsQuest("komeiji"))
                        entitiesList.Add(new SubconsciousKoishiEncounter(rightQuest));
                    break;
                case "tracen_derby":
                    if (IsCharacterLockedOrNeedsQuest("gold_ship"))
                        entitiesList.Add(new TracenDerbyRaceEncounter(new Vector3(-24f, 0f, 0f)));
                    if (IsCharacterLockedOrNeedsQuest("daitaku_helios"))
                        entitiesList.Add(new HeliosRhythmDjEncounter(FindSafeStageSpot(new Vector3(22f, 0f, 0f), 1.6f, 3.2f)));
                    break;
                case "dark_sanctuary":
                    if (IsCharacterLockedOrNeedsQuest("susie"))
                        entitiesList.Add(new DarkSanctuaryRudeBusterEncounter(centerQuest));
                    break;
                case "terminal_lucidity":
                    if (IsCharacterLockedOrNeedsQuest("komeiji"))
                        entitiesList.Add(new SubconsciousKoishiEncounter(leftQuest));
                    if (IsCharacterLockedOrNeedsQuest("spamton"))
                        entitiesList.Add(new SpamtonKromerDealEncounter(rightQuest));
                    break;
                case "extraction_floor":
                    if (IsCharacterLockedOrNeedsQuest("binah"))
                        entitiesList.Add(new BinahMeltdownEncounter(leftQuest));
                    if (IsCharacterLockedOrNeedsQuest("don_quixote"))
                        entitiesList.Add(new RocinanteJoustEncounter(rightQuest));
                    break;
                case "originium_core":
                    if (IsCharacterLockedOrNeedsQuest("priestess"))
                        entitiesList.Add(new PriestessCoreEncounter(leftQuest));
                    if (IsCharacterLockedOrNeedsQuest("closure"))
                        entitiesList.Add(new ClosureDroneShopEncounter(rightQuest));
                    break;
                case "penacony_theater":
                    if (IsCharacterLockedOrNeedsQuest("robin"))
                        entitiesList.Add(new RobinChoirMelodyEncounter(leftQuest));
                    if (IsCharacterLockedOrNeedsQuest("daitaku_helios"))
                        entitiesList.Add(new HeliosRhythmDjEncounter(rightQuest));
                    break;
                case "prime_meridian":
                    if (IsCharacterLockedOrNeedsQuest("false_son"))
                        entitiesList.Add(new PrimeMeridianStormEncounter(centerQuest));
                    break;
            }
        }

        public static void PopulateRegularStageUnlocks(List<Entity> entitiesList, int stageNum, int seed, Teleporter? tp)
        {
            var unlockRng = new Random(seed * 104729 + stageNum * 97 + 13);

            // 1. Цепочка Чёрных Ящиков «Contact Light» для Коммандо (Этапы 1 -> 2 -> 3)
            if (IsCharacterLockedOrNeedsQuest("commando"))
            {
                int expectedStage = CommandoBlackBoxChain + 1;
                if (stageNum == expectedStage && expectedStage <= 3)
                {
                    float bAng = (float)unlockRng.NextDouble() * MathF.PI * 2f;
                    Vector3 bPos = FindSafeStageSpot(new Vector3(MathF.Cos(bAng) * 28f, 0f, MathF.Sin(bAng) * 28f));
                    entitiesList.Add(new ContactLightBlackBox(bPos, expectedStage));
                }
            }

            // 2. Ромбы Памяти PRTS ◇ для Пристесс (3 скрытых ромба с HUD-глитчем -> разлом в Ядро Ориджиниума)
            if (stageNum >= 2 && IsCharacterLockedOrNeedsQuest("priestess"))
            {
                PrtsNodesTotalThisStage = 3;
                PrtsNodesSyncedThisStage = 0;
                for (int i = 0; i < 3; i++)
                {
                    float rAng = (i / 3f) * MathF.PI * 2f + (float)unlockRng.NextDouble() * 0.8f;
                    float rDist = 26f + (float)unlockRng.NextDouble() * 24f;
                    Vector3 rPos = FindSafeStageSpot(new Vector3(MathF.Cos(rAng) * rDist, 0f, MathF.Sin(rAng) * rDist), 1.2f, 2.6f);
                    entitiesList.Add(new PrtsGlitchRhombus(rPos, i));
                }
            }

            // 3. Старинные Часы S.E.E.S. для досрочного призыва Тёмного Часа Айгис (помимо авто-призыва на 06:00)
            if (IsCharacterLockedOrNeedsQuest("aigis"))
            {
                float cAng = (float)unlockRng.NextDouble() * MathF.PI * 2f;
                Vector3 cPos = FindSafeStageSpot(new Vector3(MathF.Cos(cAng) * 32f, 0f, MathF.Sin(cAng) * 32f), 1.2f, 2.8f);
                entitiesList.Add(new AntiqueSeesClockProp(cPos));
            }

            // 4. Алтарь Сумерек Птицы Апокалипсиса (появляется, если игрок уже открыл Трио Птиц!)
            if (ProgressData.Instance.IsCharacterUnlocked("bird_trio") && IsCharacterLockedOrNeedsQuest("apocalypse_bird"))
            {
                float apAng = (float)unlockRng.NextDouble() * MathF.PI * 2f;
                Vector3 apPos = FindSafeStageSpot(new Vector3(MathF.Cos(apAng) * 35f, 0f, MathF.Sin(apAng) * 35f), 2.2f, 4.2f);
                entitiesList.Add(new TwilightAltarRaidEncounter(apPos));
            }

            // 5. Ротация уникальных полевых встреч (не пьедесталов!) для закрытых выживших
            string[] stageEncounterPool =
            {
                "ahri", "cheburashka", "w_arknights", "lappland", "gwen", "scout",
                "m4a1", "medic", "mindustry", "slark", "tidehunter", "dante",
                "pushnoy", "steve", "vyper", "haze", "jack_o", "don_quixote",
                "buzz", "closure", "spamton", "daitaku_helios", "false_son",
                "sanabi", "bird_trio"
            };

            var lockedWorldChars = stageEncounterPool.Where(IsCharacterLockedOrNeedsQuest).ToList();
            if (lockedWorldChars.Count == 0) return;

            int count = Math.Min(4, lockedWorldChars.Count);
            int offset = ((stageNum - 1) * 4 + Math.Abs(seed % 11)) % lockedWorldChars.Count;
            for (int i = 0; i < count; i++)
            {
                string ck = lockedWorldChars[(offset + i) % lockedWorldChars.Count];
                float ang = (i / (float)count) * MathF.PI * 2f + 0.42f;
                float dist = 26f + (float)unlockRng.NextDouble() * 26f;
                Vector3 pos = FindSafeStageSpot(new Vector3(MathF.Cos(ang) * dist, 0f, MathF.Sin(ang) * dist), 2.0f, 4.0f);
                SpawnWorldEncounterForHero(entitiesList, ck, pos, tp, unlockRng);
            }
        }

        private static void SpawnWorldEncounterForHero(List<Entity> list, string charKey, Vector3 pos, Teleporter? tp, Random rng)
        {
            switch (charKey)
            {
                case "ahri": list.Add(new WildSpiritFoxEncounter(pos)); break;
                case "cheburashka": list.Add(new PeacefulOrangeCrateEncounter(pos, rng)); break;
                case "w_arknights": list.Add(new MimicChestAmbushEncounter(pos)); break;
                case "lappland": list.Add(new SiracusaSilenceAmbushEncounter(pos)); break;
                case "gwen": list.Add(new IsoldeShrinkingMistEncounter(pos)); break;
                case "scout": list.Add(new DeepRockMorkiteEncounter(pos, rng)); break;
                case "m4a1":
                    var jammer = new ParapluieJammerDirector(pos, rng);
                    ActiveParapluieJammer = jammer;
                    list.Add(jammer);
                    break;
                case "medic": list.Add(new WoundedHeavyEscortEncounter(pos)); break;
                case "mindustry": list.Add(new SerpuloMiniOutpostEncounter(pos)); break;
                case "slark": list.Add(new DarkReefStealthEncounter(pos)); break;
                case "tidehunter": list.Add(new MaelrawnTsunamiEncounter(pos)); break;
                case "dante": list.Add(new MephistophelesClockEncounter(pos)); break;
                case "pushnoy": list.Add(new GalileoStudioEncounter(pos)); break;
                case "steve": list.Add(new RuinedNetherPortalEncounter(pos, rng)); break;
                case "vyper": list.Add(new OsicLaserVaultEncounter(pos)); break;
                case "haze": list.Add(new OsicDreamAssassinationEncounter(pos)); break;
                case "jack_o": list.Add(new JackOServantBilliardsEncounter(pos)); break;
                case "don_quixote": list.Add(new RocinanteJoustEncounter(pos)); break;
                case "buzz": list.Add(new StarCommandSkyShuttleEncounter(pos)); break;
                case "closure": list.Add(new ClosureDroneShopEncounter(pos)); break;
                case "spamton": list.Add(new SpamtonKromerDealEncounter(pos)); break;
                case "daitaku_helios": list.Add(new HeliosRhythmDjEncounter(pos)); break;
                case "false_son": list.Add(new HalcyonGoldShrineEncounter(pos)); break;
                case "sanabi": list.Add(new MagoReactorTowerEncounter(pos)); break;
                case "bird_trio": list.Add(new BlackForestThreeCagesEncounter(pos)); break;
            }
        }

        // =========================================================================
        // RUNTIME-ХУКИ БОЯ, ТЕЛЕПОРТА, УРОНА И 13 АЛЬТЕРНАТИВНЫХ ЛИЧНОСТЕЙ
        // =========================================================================
        public static void OnTeleporterStarted(GameRunContext g, Teleporter tp)
        {
            _tpTrackingActive = true;
            _tpHuntressEligible = g.Stage >= 3 && g.PlayerHp >= g.PlayerMaxHp * 0.80f;
            _tpEnforcerStayInZone = Vector3.Distance(g.PlayerPos, tp.Position) <= tp.ZoneRadius + 1.0f;
        }

        public static void OnTeleporterBossSpawned(GameRunContext g, CombatTarget boss)
        {
            _trackedTpBoss = boss;
            _tpBossSpawnElapsed = 0f;
            _tookHpDamageDuringTpBoss = false;
            _minHpFracDuringTpBoss = g.PlayerHp / MathF.Max(1f, g.PlayerMaxHp);
            _shiftCastsDuringTpBoss = 0;
            _tpBossBurstWindow.Clear();
        }

        public static void OnTeleporterFullyCharged(IPlayerContext ctx, Teleporter tp)
        {
            if (ctx is not GameRunContext g) return;

            if (_tpTrackingActive)
            {
                if (_tpHuntressEligible && g.Stage >= 3 && g.PlayerHp >= g.PlayerMaxHp * 0.80f)
                    g.UnlockAchievement("ach_char_huntress");

                if (_tpEnforcerStayInZone)
                    g.UnlockAchievement("ach_char_enforcer");

                _tpTrackingActive = false;
            }

            // Данте (кооперативный маршрут): завершение Телепорта с павшим напарником
            if (Net.NetworkManager.Instance.Connected && Program.HasDeadCoopAllies())
                g.UnlockAchievement("ach_char_dante");

            // V1 P-Door: спидран 1-го этапа быстрее чем за 2:45 (165 сек.)
            if (g.Stage == 1 && g.StageElapsedTime <= 165.0f && IsCharacterLockedOrNeedsQuest("v1"))
            {
                Vector3 pDoorPos = Physics.FindSafeSpawnPoint(tp.Position + new Vector3(-6.5f, 0f, 5.5f), 10f, 1.8f, 4.0f);
                g.StageEntities.Add(new SecretStagePortal(pDoorPos, "hell_cybergrind"));
                SoundManager.Play("portal_open", 1.15f, 0.88f);
                g.ShowBanner("★ СПИДРАН ДО 2:45! У ТЕЛЕПОРТА ОТКРЫЛАСЬ ШАХТА P-1 «КИБЕР-ГРАЙНД»! ★", Color.Red, 4.8f);
            }
        }

        public static void OnPlayerSkillCast(GameRunContext g, string slot)
        {
            if (slot == "shift" && _trackedTpBoss != null && IsValid(_trackedTpBoss) && !_trackedTpBoss.IsAlly)
                _shiftCastsDuringTpBoss++;
        }

        public static void OnPlayerTookDamage(GameRunContext g, float hpLost)
        {
            if (hpLost > 0.2f)
            {
                _tookHpDamageDuringTpBoss = true;
                _koishiCombatUntouchedTimer = 0f;
                if (g.CharKey == "bird_trio")
                {
                    _smallBirdRevengeWindow = 6.0f;
                    _smallBirdRevengeKills = 0;
                }
            }
            float hpFrac = g.PlayerHp / MathF.Max(1f, g.PlayerMaxHp);
            if (hpFrac < 0.80f)
                _tpHuntressEligible = false;
            if (hpFrac < _minHpFracDuringTpBoss)
                _minHpFracDuringTpBoss = hpFrac;
        }

        public static void OnAllyHealedByPlayer(IPlayerContext? ctx, float healAmount)
        {
            if (ctx == null || healAmount <= 0f) return;
            MedicAllyHealAccum += healAmount;
            if (MedicAllyHealAccum >= 250f)
                ctx.UnlockAchievement("ach_char_medic");
        }

        public static void OnEnemyDamagedByPlayer(GameRunContext g, CombatTarget target, float finalDmg)
        {
            // Альт. личность W -> Вишадель: 450+ урона одним ударом/взрывом!
            if (g.CharKey == "w_arknights" && finalDmg >= 450f && !target.IsTrainingDummy)
                g.UnlockAchievement("ach_persona_w_wisadel");

            // СОЛ БЭДГАЙ: Вторжение «HERE COMES DAREDEVIL!» при взрывном бурсте по Боссу Телепорта (30%+ MaxHP за 4.5 сек.)
            if (target == _trackedTpBoss && !target.IsTrainingDummy && !_solChallengerSpawnedThisStage && IsCharacterLockedOrNeedsQuest("sol"))
            {
                float now = g.ElapsedTime;
                _tpBossBurstWindow.Add((now, finalDmg));
                _tpBossBurstWindow.RemoveAll(x => now - x.Time > 4.5f);
                float sumDmg = 0f;
                for (int i = 0; i < _tpBossBurstWindow.Count; i++) sumDmg += _tpBossBurstWindow[i].Dmg;
                if (sumDmg >= target.MaxHp * 0.30f)
                    TriggerSolDaredevilIntrusion(g, target.Position);
            }
        }

        public static void TriggerSolDaredevilIntrusion(GameRunContext g, Vector3 nearPos)
        {
            if (_solChallengerSpawnedThisStage) return;
            _solChallengerSpawnedThisStage = true;
            Vector3 sPos = Physics.FindSafeSpawnPoint(nearPos + new Vector3(9.5f, 0f, -6.5f), 14f, 2.0f, 3.5f);
            var challenger = new CombatTarget(sPos, "justice_gear", g.DifficultyCoeff * 0.90f);
            CombatManager.Enemies.Add(challenger);
            g.StageEntities.Add(challenger);
            _activeSolChallenger = challenger;
            SoundManager.Play("flame", 1.2f, 0.9f);
            SoundManager.Play("boss_roar", 1.15f, 0.88f);
            new VisualFX(sPos + new Vector3(0, 2.2f, 0), Rgb(255, 85, 25), 11.0f, 0.5f);
            g.ShowBanner("⚠ HERE COMES DAREDEVIL! НА АРЕНУ ВОРВАЛСЯ ПРОТОТИП «ДЖАСТИС»! ⚠", Rgb(255, 85, 25), 4.5f);
        }

        public static void TriggerDarkHourReaper(IPlayerContext ctx, Vector3 nearPos)
        {
            if (_darkHourTriggeredThisStage) return;
            _darkHourTriggeredThisStage = true;
            float coeff = ctx is GameRunContext grc ? grc.DifficultyCoeff * 0.88f : 1.25f;
            Vector3 rPos = Physics.FindSafeSpawnPoint(nearPos + new Vector3(-11f, 0f, 11f), 16f, 2.0f, 3.5f);
            var reaper = new CombatTarget(rPos, "shadow_reaper", coeff);
            CombatManager.Enemies.Add(reaper);
            ctx.StageEntities.Add(reaper);
            _activeDarkHourReaper = reaper;
            SoundManager.Play("dante_clock", 1.2f, 0.72f);
            SoundManager.Play("boss_roar", 1.15f, 0.82f);
            new VisualFX(rPos + new Vector3(0, 2.0f, 0), Rgb(65, 245, 135), 12.0f, 0.55f);
            ctx.ShowBanner("🕛 00:00 — НАСТУПИЛ ТЁМНЫЙ ЧАС! ЗА ВАМИ ИДЁТ ЖНЕЦ ТАРТАРА!", Rgb(65, 245, 135), 4.8f);
        }

        public static void OnEnemyKilledByPlayer(GameRunContext g, CombatTarget dead)
        {
            // Вторженец Сола (Прототип Джастис) и Жнец Тёмного Часа Айгис
            if (dead == _activeSolChallenger || (dead.EnemyType == "justice_gear" && _solChallengerSpawnedThisStage))
                g.UnlockAchievement("ach_char_sol");
            if (dead == _activeDarkHourReaper && _darkHourTriggeredThisStage)
                g.UnlockAchievement("ach_char_aigis");

            // Быстрое убийство Босса Телепорта (<=32с провоцирует Сола; <=25с без урона открывает Разлом Клипота DMC!)
            if (dead == _trackedTpBoss)
            {
                if (_tpBossSpawnElapsed <= 32.0f && !_solChallengerSpawnedThisStage && IsCharacterLockedOrNeedsQuest("sol"))
                    TriggerSolDaredevilIntrusion(g, dead.Position);

                if (_tpBossSpawnElapsed <= 25.0f && !_tookHpDamageDuringTpBoss && IsCharacterLockedOrNeedsQuest("dmc_trio"))
                {
                    Vector3 riftPos = Physics.FindSafeSpawnPoint(dead.Position + new Vector3(5.5f, 0f, 5.5f), 12f, 1.8f, 4.0f);
                    g.StageEntities.Add(new SecretStagePortal(riftPos, "qliphoth"));
                    SoundManager.Play("portal_open", 1.15f, 1.15f);
                    g.ShowBanner("★ БЕЗУПРЕЧНЫЙ БЛИЦ! КЛИНОК ЯМАТО РАССЁК РАЗЛОМ НА ВЕРШИНУ КЛИПОТА! ★", Rgb(185, 65, 255), 4.8f);
                }
            }

            // --- ПЕРСОНАЛЬНЫЕ РАЗБЛОКИРОВКИ 13 АЛЬТ. ЛИЧНОСТЕЙ ПРИ УБИЙСТВЕ ---
            if (g.CharKey == "lappland")
            {
                float now = g.ElapsedTime;
                _lapplandKillTimestamps.Add(now);
                _lapplandKillTimestamps.RemoveAll(t => now - t > 14.0f);
                if (_lapplandKillTimestamps.Count >= 16)
                    g.UnlockAchievement("ach_persona_lappland_decadenza");
                if (dead.IsElite && dead.ArmorBreakTimer > 0f)
                {
                    _lapplandSilencedEliteKills++;
                    if (_lapplandSilencedEliteKills >= 3)
                        g.UnlockAchievement("ach_persona_lappland_decadenza");
                }
            }
            else if (g.CharKey == "bird_trio" && _smallBirdRevengeWindow > 0f)
            {
                _smallBirdRevengeKills++;
                if (_smallBirdRevengeKills >= 5)
                    g.UnlockAchievement("ach_persona_bird_small");
            }

            if (dead.IsBoss)
            {
                // 1. Сол -> Кай Киске: победить Босса без Лунных и Пустотных предметов
                if (g.CharKey == "sol")
                {
                    bool hasForbidden = false;
                    foreach (var kv in g.Items)
                    {
                        if (kv.Value > 0 && (GameData.LunarItems.ContainsKey(kv.Key) || GameData.VoidItems.ContainsKey(kv.Key)))
                        {
                            hasForbidden = true;
                            break;
                        }
                    }
                    if (!hasForbidden)
                        g.UnlockAchievement("ach_persona_sol_ky");
                }
                // 2. W -> Вишадель: босс за <= 30 сек.
                else if (g.CharKey == "w_arknights" && dead == _trackedTpBoss && _tpBossSpawnElapsed <= 30.0f)
                {
                    g.UnlockAchievement("ach_persona_w_wisadel");
                }
                // 3. DMC -> Неро (8+ раз Shift за бой с боссом) и Вергилий (босс <= 25с или Митрикс)
                else if (g.CharKey == "dmc_trio")
                {
                    if (dead == _trackedTpBoss && _shiftCastsDuringTpBoss >= 8)
                        g.UnlockAchievement("ach_persona_dmc_nero");
                    if ((dead == _trackedTpBoss && _tpBossSpawnElapsed <= 25.0f) || dead.EnemyType == "mithrix")
                        g.UnlockAchievement("ach_persona_dmc_vergil");
                }
                // 4. Дон Кихот -> Санчо: 2 босса за забег
                else if (g.CharKey == "don_quixote" && Program.RunBossKills >= 2)
                {
                    g.UnlockAchievement("ach_persona_don_manager");
                }
                // 5. Трио Птиц -> Длинная Птица: не опуститься ниже 70% HP за бой с боссом
                else if (g.CharKey == "bird_trio" && dead == _trackedTpBoss && _minHpFracDuringTpBoss >= 0.70f)
                {
                    g.UnlockAchievement("ach_persona_bird_long");
                }
                // 6. Базз Лайтер -> Император Зург: Дредноут Зурга или 2 босса за забег
                else if (g.CharKey == "buzz" && (dead.EnemyType == "zurg_mech" || Program.RunBossKills >= 2))
                {
                    g.UnlockAchievement("ach_persona_buzz_zurg");
                }
            }
        }

        public static void UpdateDirector(GameRunContext g, float dt)
        {
            if (Program.IsSpectating || Physics.SpecialMode is "safe_travels" or "pvp") return;

            bool isRegularCombatStage = string.IsNullOrEmpty(Physics.SpecialMode);

            // Таймер Босса Телепорта
            if (_trackedTpBoss != null && IsValid(_trackedTpBoss) && !_trackedTpBoss.IsAlly)
            {
                _tpBossSpawnElapsed += dt;
                float hpFrac = g.PlayerHp / MathF.Max(1f, g.PlayerMaxHp);
                if (hpFrac < _minHpFracDuringTpBoss) _minHpFracDuringTpBoss = hpFrac;
            }

            // Проверка нахождения в зоне Телепорта (Силовик + Охотница)
            if (_tpTrackingActive && g.ActiveTeleporterOrPad is Teleporter tp && tp.State == "charging")
            {
                if (g.PlayerHp < g.PlayerMaxHp * 0.80f)
                    _tpHuntressEligible = false;
                if (Physics.Distance2D(g.PlayerPos, tp.Position) > tp.ZoneRadius + 0.85f)
                    _tpEnforcerStayInZone = false;
            }

            // Тёмный Час Тартара на 06:00 обычного этапа (Айгис)
            if (isRegularCombatStage && !_darkHourTriggeredThisStage && g.StageElapsedTime >= 360.0f && IsCharacterLockedOrNeedsQuest("aigis"))
            {
                TriggerDarkHourReaper(g, g.PlayerPos);
            }

            // Падение Звёздного Метеорита со Стрелой на 2+ этапе (Джотаро)
            if (isRegularCombatStage && !_meteorSpawnedThisRun && g.Stage >= 2 && g.StageElapsedTime >= 28.0f && IsCharacterLockedOrNeedsQuest("jotaro"))
            {
                _meteorSpawnedThisRun = true;
                Vector3 fwd = g.PlayerForward.LengthSquared() > 0.01f ? g.PlayerForward : Vector3.UnitZ;
                Vector3 impactPos = Physics.FindSafeSpawnPoint(g.PlayerPos + fwd * 26.0f, 18f, 2.2f, 3.8f);
                g.StageEntities.Add(new StandMeteoriteEncounter(impactPos));
                SoundManager.Play("explosion", 1.15f, 0.78f);
                g.ShowBanner("☄ С НЕБЕС РУХНУЛ ПЫЛАЮЩИЙ МЕТЕОРИТ СО СТРЕЛОЙ СТЕНДА! ☄", Color.Gold, 4.2f);
            }

            // Временный галоп Росинанта (+75% скорости бега на турнире Дон Кихот)
            if (RocinanteGallopTimer > 0f)
            {
                RocinanteGallopTimer = MathF.Max(0f, RocinanteGallopTimer - dt);
                if (g.Combat != null) g.Combat.BorkSpeedTimer = MathF.Max(g.Combat.BorkSpeedTimer, 0.25f);
            }

            // Аура Безмолвия Сиракуз (блокирует M2 / Shift / R во время засады Лаппланд)
            if (SilenceDomeTimer > 0f)
            {
                SilenceDomeTimer = MathF.Max(0f, SilenceDomeTimer - dt);
                if (g.Combat != null)
                {
                    g.Combat.M2Cd = MathF.Max(g.Combat.M2Cd, 0.35f);
                    g.Combat.ShiftCd = MathF.Max(g.Combat.ShiftCd, 0.35f);
                    g.Combat.RCd = MathF.Max(g.Combat.RCd, 0.35f);
                }
            }

            if (TemporaryChainHookTimer > 0f)
                TemporaryChainHookTimer = MathF.Max(0f, TemporaryChainHookTimer - dt);

            if (_smallBirdRevengeWindow > 0f)
                _smallBirdRevengeWindow = MathF.Max(0f, _smallBirdRevengeWindow - dt);

            // Постоянные проверки для оставшихся альт. личностей:
            // Спамтон НЕО ($400 золота или 10 предметов)
            if (g.CharKey == "spamton")
            {
                int totalItems = 0;
                foreach (var kv in g.Items) totalItems += kv.Value;
                if (g.Gold >= 400 || totalItems >= 10)
                    g.UnlockAchievement("ach_persona_spamton_neo");
            }
            // Дон Кихот Фонарь (300+ исцеления) и Санчо (4+ этап)
            else if (g.CharKey == "don_quixote")
            {
                if (g.TotalHealed >= 300f)
                    g.UnlockAchievement("ach_persona_don_lantern");
                if (g.Stage >= 4)
                    g.UnlockAchievement("ach_persona_don_manager");
            }
            // V1 -> V2 (800+ очков стиля SSS / ULTRAKILL)
            else if (g.CharKey == "v1" && g.Combat != null && g.Combat.V1StylePoints >= 780f)
            {
                g.UnlockAchievement("ach_persona_v1_v2");
            }
            // Сатори -> Коиши (30 сек. рядом с врагами без получения урона)
            else if (g.CharKey == "komeiji")
            {
                bool nearHostile = false;
                foreach (var e in CombatManager.Enemies)
                {
                    if (IsValid(e) && !e.IsAlly && !e.IsTrainingDummy && Vector3.Distance(e.Position, g.PlayerPos) <= 22.0f)
                    {
                        nearHostile = true;
                        break;
                    }
                }
                if (nearHostile)
                {
                    _koishiCombatUntouchedTimer += dt;
                    if (_koishiCombatUntouchedTimer >= 30.0f)
                        g.UnlockAchievement("ach_persona_komeiji_koishi");
                }
            }
        }

        // =========================================================================
        // 2D HUD-ТЕЛЕМЕТРИЯ ДЛЯ ОСОБЫХ МЕХАНИК РАЗБЛОКИРОВКИ
        // =========================================================================
        public static void DrawUnlockHudOverlays(GameRunContext g, int sw, int sh)
        {
            if (Program.IsSpectating) return;

            int topOffset = 112;

            // 1. Глитч-пеленгатор PRTS ◇ (Пристесс)
            if (PrtsNodesTotalThisStage > 0 && PrtsNodesSyncedThisStage < PrtsNodesTotalThisStage && NearestPrtsNodeDist < 65f)
            {
                float alpha = Math.Clamp(1.0f - (NearestPrtsNodeDist / 65f), 0.25f, 1.0f);
                byte aByte = (byte)(alpha * 220f);
                Engine.DrawTextCentered(
                    $"◇ PRTS // EYESOFPRIESTESS :: СИГНАЛ ПАМЯТИ ({PrtsNodesSyncedThisStage}/{PrtsNodesTotalThisStage}) — ДИСТАНЦИЯ {NearestPrtsNodeDist:F0}М ◇",
                    sw / 2, topOffset, 15.5f, Rgb(255, 72, 105, aByte));
                topOffset += 24;
            }
            NearestPrtsNodeDist = 999f;

            // 2. Пеленгатор радиопомех «Parapluie» (M4A1: Холодно / Тепло / Горячо)
            if (ActiveParapluieJammer != null && IsValid(ActiveParapluieJammer) && !ActiveParapluieJammer.Completed)
            {
                float d = ActiveParapluieJammer.GetNearestRelayDistance(g.PlayerPos);
                (string tempWord, Color tCol) = d <= 11f
                    ? ("🔥 ОЧЕНЬ ГОРЯЧО (РЯДОМ!)", Color.Red)
                    : (d <= 24f ? ("⚡ ТЕПЛО (СРЕДНЯЯ ДИСТАНЦИЯ)", Color.Orange) : ("❄ ХОЛОДНО (ДАЛЕКО)", Color.SkyBlue));
                Engine.DrawTextCentered(
                    $"РАДИОПЕЛЕНГАТОР AR-TEAM [{3 - ActiveParapluieJammer.RemainingRelays}/3] :: {tempWord} ({d:F0}м)",
                    sw / 2, topOffset, 15.5f, tCol);
                topOffset += 24;
            }

            // 3. Индикатор Купола Безмолвия (Лаппланд)
            if (SilenceDomeTimer > 0.02f)
            {
                Engine.DrawTextCentered("✖ АУРА БЕЗМОЛВИЯ СИРАКУЗ: НАВЫКИ [ПКМ / SHIFT / R] ЗАБЛОКИРОВАНЫ — СРАЖАЙТЕСЬ [ЛКМ]! ✖",
                    sw / 2, topOffset, 16f, Rgb(225, 95, 255));
                topOffset += 26;
            }

            // 4. Ритм-полоса 128 BPM (Дайтаку Гелиос «WEI~☆!»)
            if (ActiveRhythmEncounter != null && IsValid(ActiveRhythmEncounter) && ActiveRhythmEncounter.IsActive)
            {
                int bw = 380, bh = 24, bx = (sw - bw) / 2, by = sh - 175;
                Raylib.DrawRectangle(bx - 4, by - 26, bw + 8, bh + 34, Rgb(12, 16, 30, 225));
                Raylib.DrawRectangle(bx, by, bw, bh, Rgb(28, 36, 58, 240));
                int sweetW = 68;
                Raylib.DrawRectangle(bx + (bw - sweetW) / 2, by, sweetW, bh, Rgb(75, 255, 165, 155));
                int cursorX = bx + (int)(ActiveRhythmEncounter.BeatCursor * bw);
                Raylib.DrawRectangle(cursorX - 4, by - 4, 8, bh + 8, Color.Gold);
                Engine.DrawTextCentered(
                    $"♫ РИТМ 128 BPM «WEI~☆!» [{ActiveRhythmEncounter.Combo}/8] — ЖМИТЕ [E] В ЗЕЛЁНОЙ ЗОНЕ! ♫",
                    sw / 2, by - 22, 15f, Rgb(95, 235, 255));
            }

            // 5. Шкала TP Graze (Сьюзи в Тёмном Святилище)
            if (ActiveSusieEncounter != null && IsValid(ActiveSusieEncounter) && ActiveSusieEncounter.IsActive)
            {
                int bw = 340, bh = 20, bx = (sw - bw) / 2, by = sh - 172;
                Raylib.DrawRectangle(bx - 4, by - 24, bw + 8, bh + 30, Rgb(24, 10, 32, 225));
                Raylib.DrawRectangle(bx, by, bw, bh, Rgb(48, 22, 58, 240));
                int fillW = (int)(Math.Clamp(ActiveSusieEncounter.TpPercent / 100f, 0f, 1f) * bw);
                Raylib.DrawRectangle(bx, by, fillW, bh, ActiveSusieEncounter.TpPercent >= 100f ? Color.Gold : Rgb(235, 72, 175));
                string tpHint = ActiveSusieEncounter.TpPercent >= 100f
                    ? "★ TP 100%! НАЖМИТЕ [E], ЧТОБЫ ОТРАЗИТЬ RUDE BUSTER В РЫЦАРЯ! ★"
                    : $"TP НАПРЯЖЕНИЯ (GRAZE): {(int)ActiveSusieEncounter.TpPercent}%  |  Контр-ударов: {ActiveSusieEncounter.HitsLanded}/3";
                Engine.DrawTextCentered(tpHint, sw / 2, by - 20, 15f, Color.White);
            }

            // 6. Энергия Офиса Охраны Fazbear (Фредди)
            if (ActiveFazbearOffice != null && IsValid(ActiveFazbearOffice) && ActiveFazbearOffice.IsActive)
            {
                Engine.DrawTextCentered(
                    $"🔋 ОФИС FAZBEAR: {ActiveFazbearOffice.HourLabel} | ЭНЕРГИЯ {(int)ActiveFazbearOffice.PowerPercent}% | ШКАТУЛКА {(int)ActiveFazbearOffice.MusicBoxPercent}%",
                    sw / 2, topOffset, 16f, ActiveFazbearOffice.PowerPercent < 25f ? Color.Red : Color.Gold);
            }
        }
    }

    // =========================================================================
    // 1. БАЗАР: ТЕРМАЛЬНЫЙ КРИСТАЛЛ ARTIFICER И СЕКРЕТНЫЕ ПРОХОДЫ (ФАЗБЕР / МЕЗА)
    // =========================================================================
    public class ArtificerThermalCrystal : Entity
    {
        private float _temperature = 0f;
        private int _state = 0; // 0 = заморожен, 1 = химеры
        private float _hitCd = 0f;
        private readonly Entity _iceShell;
        private readonly Entity _labelEnt;
        private readonly List<CombatTarget> _chimeras = new();

        public ArtificerThermalCrystal(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(2.4f, 0.4f, 2.4f), position: new(0, 0.2f, 0), color: Rgb(44, 56, 78));
            new Entity(parent: this, model: "cube", scale: new(0.8f, 1.8f, 0.8f), position: new(0, 1.2f, 0), color: Rgb(235, 240, 250));
            _iceShell = new Entity(parent: this, model: "diamond", scale: new(2.1f, 3.2f, 2.1f), position: new(0, 1.8f, 0), color: Rgb(95, 215, 255, 195), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.9f, 0), color: Color.SkyBlue);
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.25f, 3.2f, pos.Y));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed || !CharacterUnlockSystems.IsCharacterLockedOrNeedsQuest("artificer"))
            {
                _labelEnt.Enabled = false;
                return;
            }
            _hitCd = MathF.Max(0f, _hitCd - dt);
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 14f || _state == 1);

            if (_state == 0)
            {
                _temperature = MathF.Max(0f, _temperature - 18f * dt);
                if (_hitCd <= 0f && CharacterUnlockEncounters.DetectPlayerAttackNear(g, Position + new Vector3(0, 1.6f, 0), 3.1f))
                {
                    _hitCd = 0.16f;
                    _temperature = MathF.Min(100f, _temperature + 24f);
                    SoundManager.Play("flame", 0.75f, 0.9f + _temperature * 0.004f);
                    new VisualFX(Position + new Vector3(0, 1.8f, 0), Rgb(255, 125, 40), 2.8f, 0.22f);
                }
                _iceShell.Color = _temperature >= 95f ? Rgb(255, 145, 55, 210) : Rgb(95, 215, 255, 195);
                _labelEnt.BillboardText = _temperature >= 95f
                    ? "[E] Кристалл раскалён (100°C)! Снять печать (5 Лунных Монет)"
                    : $"Замёрзший Кристалл ENV: Нагрев {(int)_temperature}°C / 100°C\n[Разогрейте кристалл быстрыми атаками + 5 Лунных Монет!]";

                if (_temperature >= 95f && dist < 4.6f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    if (g.LunarCoins >= 5)
                    {
                        g.LunarCoins -= 5;
                        _state = 1;
                        _iceShell.Enabled = false;
                        SoundManager.Play("explosion", 1.0f, 1.1f);
                        for (int i = 0; i < 2; i++)
                        {
                            Vector3 cp = Physics.FindSafeSpawnPoint(Position + new Vector3(i == 0 ? -7f : 7f, 0f, -5f));
                            var ch = new CombatTarget(cp, "lunar_chimera", 1.25f) { IsElite = true };
                            CombatManager.Enemies.Add(ch);
                            g.StageEntities.Add(ch);
                            _chimeras.Add(ch);
                        }
                        g.ShowBanner("ТЕМПОРАЛЬНЫЙ ЛЁД РАСПЛАВЛЕН! ОТРАЗИТЕ ЛУННЫХ ХИМЕР!", Color.Orange, 3.5f);
                    }
                    else
                    {
                        SoundManager.Play("ui_error");
                        g.ShowBanner("НУЖНО 5 ЛУННЫХ МОНЕТ ДЛЯ ОСВОБОЖДЕНИЯ ИЗОБРЕТАТЕЛЬНИЦЫ!", Color.Red, 2.2f);
                    }
                }
            }
            else
            {
                _chimeras.RemoveAll(c => !IsValid(c) || c.Hp <= 0f || c.IsAlly);
                _labelEnt.BillboardText = $"Одолейте Стражей Высшего Суда: осталось {_chimeras.Count}";
                if (_chimeras.Count == 0)
                {
                    g.UnlockAchievement("ach_char_artificer");
                    Destroy(this);
                }
            }
        }
    }

    public class BazaarSecretTrigger : Entity
    {
        private readonly string _targetMode;
        private readonly bool _breakable;
        private float _hp = 180f;
        private float _hitCd = 0f;
        private readonly Entity _labelEnt;

        public BazaarSecretTrigger(Vector3 pos, string targetMode, string title, Color col, bool breakable) : base(position: pos)
        {
            _targetMode = targetMode;
            _breakable = breakable;
            new Entity(parent: this, model: "cube", scale: breakable ? new(2.2f, 2.2f, 0.35f) : new(1.4f, 2.4f, 1.2f),
                position: new(0, 1.2f, 0), color: Rgb(42, 46, 58));
            new Entity(parent: this, model: "diamond", scale: new(0.95f, 0.95f, 0.45f),
                position: new(0, 1.45f, 0.35f), color: col, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.95f, 0), color: col) { BillboardText = title };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _hitCd = MathF.Max(0f, _hitCd - dt);
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && dist < 10.5f;

            if (_breakable)
            {
                if (_hitCd <= 0f && CharacterUnlockEncounters.DetectPlayerAttackNear(g, Position + new Vector3(0, 1.2f, 0), 2.8f))
                {
                    _hitCd = 0.2f;
                    _hp -= 65f;
                    SoundManager.Play("hit", 0.9f, 0.85f);
                    new VisualFX(Position + new Vector3(0, 1.2f, 0), Color.Orange, 2.4f, 0.2f);
                    if (_hp <= 0f)
                    {
                        SoundManager.Play("portal_open");
                        g.EnterSecretStage(_targetMode);
                        Destroy(this);
                    }
                }
            }
            else if (dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                SoundManager.Play("portal_open");
                g.EnterSecretStage(_targetMode);
                Destroy(this);
            }
        }
    }

    // =========================================================================
    // 2. КОММАНДО: СКВОЗНАЯ ЦЕПОЧКА 3 ЧЁРНЫХ ЯЩИКОВ UES «CONTACT LIGHT» (ЭТАПЫ 1->2->3)
    // =========================================================================
    public class ContactLightBlackBox : Entity
    {
        private readonly int _stageStep;
        private int _state = 0;
        private float _decryptProgress = 0f;
        private readonly Entity _beacon;
        private readonly Entity _labelEnt;
        private readonly List<CombatTarget> _raiders = new();

        public ContactLightBlackBox(Vector3 pos, int stageStep) : base(position: pos)
        {
            _stageStep = stageStep;
            new Entity(parent: this, model: "cube", scale: new(1.8f, 0.95f, 1.3f), position: new(0, 0.48f, 0), color: Rgb(48, 54, 66));
            _beacon = new Entity(parent: this, model: "diamond", scale: new(0.65f, 1.15f, 0.65f), position: new(0, 1.45f, 0), color: Color.Orange, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.8f, 0), color: Color.Orange)
            {
                BillboardText = $"[E] Чёрный Ящик UES «Contact Light» [Часть {stageStep}/3]"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _beacon.RotationY += 95f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 14f || _state == 1);

            if (_state == 0 && dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                SoundManager.Play("ui_ready", 1.0f, 1.05f + _stageStep * 0.1f);
                if (_stageStep < 3)
                {
                    CharacterUnlockSystems.CommandoBlackBoxChain = _stageStep;
                    new VisualFX(Position + new Vector3(0, 1.2f, 0), Color.Orange, 4.5f, 0.35f);
                    g.ShowBanner($"ЧЁРНЫЙ ЯЩИК UES ({_stageStep}/3) ПОЛУЧЕН! НАЙДИТЕ СЛЕДУЮЩИЙ НА ЭТАПЕ {_stageStep + 1}!", Color.Orange, 3.8f);
                    Destroy(this);
                }
                else
                {
                    _state = 1;
                    for (int i = 0; i < 3; i++)
                    {
                        float a = i * 2.09f;
                        Vector3 sp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 9f, 0f, MathF.Sin(a) * 9f));
                        var mob = new CombatTarget(sp, "combine_soldier", g is GameRunContext grc ? grc.DifficultyCoeff : 1.5f) { IsElite = true };
                        CombatManager.Enemies.Add(mob);
                        g.StageEntities.Add(mob);
                        _raiders.Add(mob);
                    }
                    g.ShowBanner("ФИНАЛЬНАЯ ДЕШИФРОВКА (3/3)! ДЕРЖИТЕ ПЕРИМЕТР КАПСУЛЫ ПОД ОГНЁМ!", Color.Orange, 3.8f);
                }
            }
            else if (_state == 1)
            {
                _raiders.RemoveAll(r => !IsValid(r) || r.Hp <= 0f || r.IsAlly);
                if (dist <= 12f) _decryptProgress = MathF.Min(100f, _decryptProgress + 12.5f * dt);
                _labelEnt.BillboardText = $"Дешифровка Протокола «Contact Light»: {(int)_decryptProgress}% | Врагов: {_raiders.Count}";
                if (_decryptProgress >= 100f && _raiders.Count == 0)
                {
                    CharacterUnlockSystems.CommandoBlackBoxChain = 3;
                    g.SpawnItemDrop(Position + new Vector3(0, 1.5f, 0), null, "legendary");
                    g.UnlockAchievement("ach_char_commando");
                    Destroy(this);
                }
            }
        }
    }

    // =========================================================================
    // 3. ПРИСТЕСС (◇ PRTS HUD-ГЛИТЧ) И M4A1 (РАДИОГЛУШИТЕЛЬ PARAPLUIE)
    // =========================================================================
    public class PrtsGlitchRhombus : Entity
    {
        private readonly int _idx;
        private readonly Entity _diamond;
        private readonly Entity _labelEnt;

        public PrtsGlitchRhombus(Vector3 pos, int idx) : base(position: pos)
        {
            _idx = idx;
            _diamond = new Entity(parent: this, model: "diamond", scale: new(1.1f, 2.1f, 1.1f), position: new(0, 1.9f, 0), color: Rgb(255, 58, 92), unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(1.8f, 0.12f, 1.8f), position: new(0, 0.35f, 0), color: Rgb(195, 95, 255), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.4f, 0), color: Rgb(255, 78, 115))
            {
                BillboardText = $"[E] Синхронизировать Ромб Памяти PRTS ◇ #{idx + 1}/3"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _diamond.RotationY += 120f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            if (dist < CharacterUnlockSystems.NearestPrtsNodeDist)
                CharacterUnlockSystems.NearestPrtsNodeDist = dist;

            _labelEnt.Enabled = !Program.IsSpectating && dist < 9.5f;
            if (dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                CharacterUnlockSystems.PrtsNodesSyncedThisStage++;
                SoundManager.Play("ui_ready", 1.0f, 1.25f);
                new VisualFX(Position + new Vector3(0, 1.8f, 0), Rgb(255, 65, 105), 5.0f, 0.35f);
                if (CharacterUnlockSystems.PrtsNodesSyncedThisStage >= CharacterUnlockSystems.PrtsNodesTotalThisStage)
                {
                    Vector3 pPos = Physics.FindSafeSpawnPoint(Position + new Vector3(3.5f, 0f, 3.5f), 10f, 1.8f, 4.0f);
                    g.StageEntities.Add(new SecretStagePortal(pPos, "originium_core"));
                    SoundManager.Play("portal_open", 1.15f, 1.1f);
                    g.ShowBanner("◇ ВСЕ 3 РОМБА PRTS СИНХРОНИЗИРОВАНЫ! ОТКРЫТ РАЗЛОМ В «ЯДРО ОРИДЖИНИУМА»! ◇", Rgb(255, 72, 115), 4.8f);
                }
                else
                {
                    g.ShowBanner($"◇ РОМБ ПАМЯТИ PRTS СИНХРОНИЗИРОВАН ({CharacterUnlockSystems.PrtsNodesSyncedThisStage}/3)! ◇", Rgb(255, 72, 115), 2.4f);
                }
                Destroy(this);
            }
        }
    }

    public class ParapluieJammerDirector : Entity
    {
        public bool Completed = false;
        private readonly List<Entity> _relays = new();
        private float _hitCd = 0f;

        public int RemainingRelays => _relays.Count;

        public ParapluieJammerDirector(Vector3 basePos, Random rng) : base(position: basePos)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = (i / 3f) * MathF.PI * 2f + (float)rng.NextDouble() * 0.9f;
                float d = 22f + (float)rng.NextDouble() * 24f;
                Vector3 rp = Physics.FindSafeSpawnPoint(new Vector3(MathF.Cos(a) * d, 0f, MathF.Sin(a) * d), 14f, 1.2f, 2.5f);
                var relay = new Entity(position: rp);
                new Entity(parent: relay, model: "cube", scale: new(0.55f, 2.6f, 0.55f), position: new(0, 1.3f, 0), color: Rgb(42, 48, 56));
                new Entity(parent: relay, model: "diamond", scale: new(0.95f, 0.65f, 0.95f), position: new(0, 2.75f, 0), color: Color.Red, unlit: true);
                relay.BillboardText = "";
                _relays.Add(relay);
            }
        }

        public float GetNearestRelayDistance(Vector3 playerPos)
        {
            float best = 999f;
            foreach (var r in _relays)
            {
                if (IsValid(r))
                {
                    float d = Vector3.Distance(playerPos, r.Position);
                    if (d < best) best = d;
                }
            }
            return best;
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || Completed || IsDestroyed) return;
            _hitCd = MathF.Max(0f, _hitCd - dt);

            for (int i = _relays.Count - 1; i >= 0; i--)
            {
                var r = _relays[i];
                if (!IsValid(r)) { _relays.RemoveAt(i); continue; }
                float d = Vector3.Distance(g.PlayerPos, r.Position);
                if (_hitCd <= 0f && (CharacterUnlockEncounters.DetectPlayerAttackNear(g, r.Position + new Vector3(0, 1.5f, 0), 3.0f)
                    || (d < 4.0f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)))
                {
                    _hitCd = 0.25f;
                    SoundManager.Play("explosion", 0.95f, 1.15f);
                    new VisualFX(r.Position + new Vector3(0, 1.6f, 0), Color.Lime, 4.5f, 0.35f);
                    Destroy(r);
                    _relays.RemoveAt(i);
                    if (_relays.Count > 0)
                        g.ShowBanner($"ГЛУШИТЕЛЬ PARAPLUIE УНИЧТОЖЕН! ОСТАЛОСЬ: {_relays.Count}/3", Color.Lime, 2.4f);
                }
            }

            if (_relays.Count == 0)
            {
                Completed = true;
                g.UnlockAchievement("ach_char_m4a1");
                g.ShowBanner("ВСЕ ГЛУШИТЕЛИ СБИТЫ! РАДИОСВЯЗЬ ТЕЛЕПОРТА И КАНАЛ M4A1 ВОССТАНОВЛЕНЫ!", Color.Lime, 3.8f);
                Destroy(this);
            }
        }
    }

    // =========================================================================
    // 4. АЙГИС (ЧАСЫ S.E.E.S.), ДЖОТАРО (МЕТЕОРИТ СО СТРЕЛОЙ), SANABI, ПТИЦЫ
    // =========================================================================
    public class AntiqueSeesClockProp : Entity
    {
        private bool _triggered = false;
        private readonly Entity _pendulum;
        private readonly Entity _labelEnt;

        public AntiqueSeesClockProp(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(1.2f, 3.4f, 0.85f), position: new(0, 1.7f, 0), color: Rgb(68, 46, 32));
            _pendulum = new Entity(parent: this, model: "diamond", scale: new(0.65f, 0.65f, 0.25f), position: new(0, 2.5f, 0.48f), color: Rgb(85, 255, 155), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 4.0f, 0), color: Rgb(85, 255, 155))
            {
                BillboardText = "[E / Удар] Старинные Часы S.E.E.S. (Пробить Полночь Тёмного Часа)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || _triggered || IsDestroyed) return;
            _pendulum.RotationZ = MathF.Sin((float)Raylib.GetTime() * 4.5f) * 24f;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && dist < 11f;

            if ((dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                || CharacterUnlockEncounters.DetectPlayerAttackNear(g, Position + new Vector3(0, 1.8f, 0), 2.8f))
            {
                _triggered = true;
                _labelEnt.Enabled = false;
                CharacterUnlockSystems.TriggerDarkHourReaper(g, Position);
            }
        }
    }

    public class StandMeteoriteEncounter : Entity
    {
        private float _fallY = 42.0f;
        private float _attuneProgress = 0f;
        private float _timeStopPulseTimer = 3.2f;
        private readonly Entity _meteorMesh;
        private readonly Entity _arrowMesh;
        private readonly Entity _labelEnt;

        public StandMeteoriteEncounter(Vector3 impactPos) : base(position: impactPos)
        {
            _meteorMesh = new Entity(parent: this, model: "sphere", scaleUniform: 2.2f, position: new(0, _fallY, 0), color: Rgb(255, 105, 35), unlit: true);
            _arrowMesh = new Entity(parent: this, model: "diamond", scale: new(0.55f, 2.2f, 0.55f), position: new(0, 1.6f, 0), color: Color.Gold, unlit: true, enabled: false);
            _labelEnt = new Entity(parent: this, position: new(0, 3.8f, 0), color: Color.Gold, enabled: false);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            if (_fallY > 0.8f)
            {
                _fallY = MathF.Max(0.8f, _fallY - 38f * dt);
                _meteorMesh.Y = _fallY;
                if (_fallY <= 0.85f)
                {
                    _meteorMesh.Scale = new Vector3(2.4f, 0.9f, 2.4f);
                    _meteorMesh.Color = Rgb(78, 52, 95);
                    _arrowMesh.Enabled = true;
                    _labelEnt.Enabled = true;
                    SoundManager.Play("slam_land", 1.2f, 0.82f);
                    new VisualFX(Position + new Vector3(0, 1.2f, 0), Color.Gold, 11.0f, 0.5f);
                }
                return;
            }

            _arrowMesh.RotationY += 130f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && dist < 18f;

            if (dist <= 14.5f)
            {
                // Ураганный напор + периодические импульсы Time-Stop Стенда!
                Vector3 push = g.PlayerPos - Position;
                push.Y = 0f;
                push = push.LengthSquared() > 0.01f ? Vector3.Normalize(push) : Vector3.UnitZ;
                g.PlayerPos = Physics.ResolveHorizontalCollision(g.PlayerPos + push * 7.8f * dt, 0.72f, g.PlayerPos.Y);

                _timeStopPulseTimer -= dt;
                if (_timeStopPulseTimer <= 0f)
                {
                    _timeStopPulseTimer = 2.6f;
                    SoundManager.Play("dante_clock", 0.95f, 1.35f);
                    new LunarShockwaveRing(Position, 16f, 16f, 18f);
                }
            }

            if (dist <= 6.8f)
            {
                _attuneProgress += 16.5f * dt;
                _labelEnt.BillboardText = $"Пробуждение Стрелы Стенда: {(int)MathF.Min(100f, _attuneProgress)}%\n[Перепрыгивайте кольца Времени и идите к Стреле!]";
                if (_attuneProgress >= 100f)
                {
                    g.UnlockAchievement("ach_char_jotaro");
                    Destroy(this);
                }
            }
            else
            {
                _attuneProgress = MathF.Max(0f, _attuneProgress - 12f * dt);
                _labelEnt.BillboardText = $"Кратер Метеорита со Стрелой Стенда ({(int)_attuneProgress}%)\n[Пробейтесь сквозь ураган к центру кратера!]";
            }
        }
    }

    // БРИГАДНЫЙ ГЕНЕРАЛ (SANABI): Высотная Шахта Реактора Маго с полётами на Цепном Крюке [E] между дронами!
    public class MagoReactorTowerEncounter : Entity
    {
        private bool _active = false;
        private float _plasmaHeight = 0f;
        private float _coreHp = 220f;
        private readonly List<Entity> _drones = new();
        private readonly Entity _plasmaDisk;
        private readonly Entity _reactorCore;
        private readonly Entity _labelEnt;

        public MagoReactorTowerEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(4.2f, 0.4f, 4.2f), position: new(0, 0.2f, 0), color: Rgb(34, 38, 52));
            _plasmaDisk = new Entity(parent: this, model: "sphere", scale: new(18f, 0.18f, 18f), position: new(0, 0.1f, 0), color: Rgb(255, 65, 95, 120), unlit: true, enabled: false);

            Vector3[] droneOffsets =
            {
                new(5.5f, 6.5f, 0f),
                new(0f, 12.5f, 6.0f),
                new(-6.0f, 18.5f, 0f),
                new(0f, 24.5f, -5.5f)
            };
            for (int i = 0; i < droneOffsets.Length; i++)
            {
                var dr = new Entity(parent: this, position: droneOffsets[i]);
                new Entity(parent: dr, model: "sphere", scaleUniform: 1.15f, color: Rgb(55, 225, 255), unlit: true);
                new Entity(parent: dr, model: "diamond", scale: new(1.9f, 0.18f, 1.9f), color: Color.Gold, unlit: true);
                dr.BillboardText = $"[E] КРЮК #{i + 1}";
                dr.Color = Color.SkyBlue;
                _drones.Add(dr);
            }

            _reactorCore = new Entity(parent: this, model: "diamond", scale: new(2.2f, 2.8f, 2.2f), position: new(0, 27.5f, 0), color: Rgb(255, 72, 95), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.6f, 0), color: Rgb(95, 235, 255))
            {
                BillboardText = "[E] Шахта Реактора Маго (SANABI)\n[Поднимитесь на тросе Руки-Крюка [E] по дронам до Ядра на высоте 27м!]"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _reactorCore.RotationY += 120f * dt;
            float dist2D = Physics.Distance2D(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && dist2D < 18f;

            if (dist2D < 18f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                if (!_active)
                {
                    _active = true;
                    _plasmaHeight = 0.2f;
                    _plasmaDisk.Enabled = true;
                    g.ShowBanner("ПРОЕКТ №17 АКТИВИРОВАН! ЖМИТЕ [E], ЧТОБЫ ЦЕПЛЯТЬСЯ КРЮКОМ ЗА ВЫСОТНЫХ ДРОНОВ!", Rgb(95, 235, 255), 3.8f);
                }

                // Ищем ближайшего верхнего дрона или Ядро для рывка на Цепном Крюке!
                Entity? bestAnchor = null;
                float bestScore = 999f;
                foreach (var d in _drones)
                {
                    Vector3 wp = Position + d.Position;
                    float dist = Vector3.Distance(g.PlayerPos, wp);
                    if (dist <= 22f && wp.Y >= g.PlayerPos.Y - 2.5f && dist < bestScore)
                    {
                        bestScore = dist;
                        bestAnchor = d;
                    }
                }
                Vector3 coreWorld = Position + _reactorCore.Position;
                if (Vector3.Distance(g.PlayerPos, coreWorld) <= 18f)
                    bestAnchor = _reactorCore;

                if (bestAnchor != null)
                {
                    Vector3 targetWorld = Position + bestAnchor.Position;
                    Vector3 zipDir = Vector3.Normalize(targetWorld - g.PlayerPos);
                    SoundManager.Play("coin_ricoshot", 0.95f, 1.25f);
                    new LightningBeam(g.PlayerPos + new Vector3(0, 1.2f, 0), targetWorld, Color.Gold, 0.24f);
                    g.PlayerVelY = MathF.Max(16.5f, zipDir.Y * 26f + 9.5f);
                    g.PlayerGrounded = false;
                    g.PlayerJumpsLeft = Math.Max(1, g.PlayerMaxJumps);
                    g.ApplyDash(new Vector3(zipDir.X, 0f, zipDir.Z) * 28f, 0.28f);

                    if (bestAnchor == _reactorCore && Vector3.Distance(g.PlayerPos, coreWorld) <= 10.5f)
                    {
                        _coreHp -= 120f;
                        new VisualFX(coreWorld, Color.Gold, 6.5f, 0.35f);
                    }
                }
            }

            if (_active)
            {
                _plasmaHeight = MathF.Min(19.0f, _plasmaHeight + 1.35f * dt);
                _plasmaDisk.Y = _plasmaHeight;

                Vector3 coreWorld = Position + _reactorCore.Position;
                if (CharacterUnlockEncounters.DetectPlayerAttackNear(g, coreWorld, 5.5f))
                {
                    _coreHp -= 120f;
                    new VisualFX(coreWorld, Rgb(255, 85, 95), 6.0f, 0.35f);
                }

                if (_coreHp <= 0f)
                {
                    SoundManager.Play("explosion", 1.2f, 0.9f);
                    g.UnlockAchievement("ach_char_sanabi");
                    Destroy(this);
                    return;
                }

                if (dist2D <= 10f && g.PlayerPos.Y < Position.Y + _plasmaHeight - 0.8f && g.PlayerPos.Y > Position.Y + 1.2f)
                {
                    CombatManager.DamagePlayer(8f * dt);
                }
            }
        }
    }

    // ТРИО ПТИЦ: 3 Клетки Аномалий Чёрного Леса (1: Умиротворение Малой Птицы без атак, 2: Весы, 3: Лампа)
    public class BlackForestThreeCagesEncounter : Entity
    {
        private int _phase = 0; // 0 = ожидание, 1 = Малая Птица, 2 = Длинная Птица, 3 = Большая Птица
        private float _timer = 0f;
        private int _lastAttackCount = 0;
        private readonly Entity _scalePlate;
        private readonly Entity _labelEnt;
        private readonly List<CombatTarget> _moths = new();

        public BlackForestThreeCagesEncounter(Vector3 pos) : base(position: pos)
        {
            for (int i = -1; i <= 1; i++)
            {
                new Entity(parent: this, model: "cube", scale: new(1.6f, 2.8f, 1.6f), position: new(i * 3.4f, 1.4f, 0), color: Rgb(32, 28, 38));
                new Entity(parent: this, model: "sphere", scaleUniform: 0.85f, position: new(i * 3.4f, 1.6f, 0),
                    color: i == -1 ? Color.White : (i == 0 ? Color.Gold : Rgb(255, 195, 45)), unlit: true);
            }
            _scalePlate = new Entity(parent: this, model: "diamond", scale: new(5.5f, 0.14f, 5.5f), position: new(6.5f, 0.15f, 5.5f), color: Color.Gold, unlit: true, enabled: false);
            _labelEnt = new Entity(parent: this, position: new(0, 3.8f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Три Клетки Чёрного Леса (Испытание Трёх Птиц)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _phase > 0);

            if (_phase == 0 && dist < 4.8f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _phase = 1;
                _timer = 8.0f;
                _lastAttackCount = g is GameRunContext grc ? grc.TotalAbilityCasts : 0;
                SoundManager.Play("dante_clock", 1.0f, 1.2f);
                g.ShowBanner("КЛЕТКА 1/3 (МАЛАЯ ПТИЦА): ПРОДЕРЖИТЕСЬ РЯДОМ 8 СЕК., НЕ НАНОСЯ НИ ЕДИНОЙ АТАКИ!", Color.White, 3.8f);
            }
            else if (_phase == 1)
            {
                int curCasts = g is GameRunContext grc ? grc.TotalAbilityCasts : 0;
                if (curCasts > _lastAttackCount || Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    _timer = 8.0f;
                    _lastAttackCount = curCasts;
                    CombatManager.DamagePlayer(14f);
                    SoundManager.Play("boss_roar", 1.0f, 1.45f);
                    g.ShowBanner("ВЫ АТАКОВАЛИ МАЛУЮ ПТИЦУ — ЕЁ ПАСТЬ НАКАЗАЛА ВАС! ТАЙМЕР СБРОШЕН!", Color.Red, 2.4f);
                }
                else if (dist <= 11.5f)
                {
                    _timer -= dt;
                }
                _labelEnt.BillboardText = $"Испытание 1/3 (Малая Птица): Умиротворение без атак — {_timer:F1}с";
                if (_timer <= 0f)
                {
                    _phase = 2;
                    _timer = 6.5f;
                    _scalePlate.Enabled = true;
                    SoundManager.Play("dante_clock", 1.05f, 1.0f);
                    g.ShowBanner("КЛЕТКА 2/3 (ДЛИННАЯ ПТИЦА): ВСТАНЬТЕ НА ЗОЛОТУЮ ЧАШУ ВЕСОВ СПРАВА!", Color.Gold, 3.5f);
                }
            }
            else if (_phase == 2)
            {
                Vector3 plateWorld = Position + _scalePlate.Position;
                if (Physics.Distance2D(g.PlayerPos, plateWorld) <= 3.6f)
                    _timer -= dt;
                _labelEnt.BillboardText = $"Испытание 2/3 (Длинная Птица): Балансировка Весов Греха — {MathF.Max(0f, _timer):F1}с";
                if (_timer <= 0f)
                {
                    _phase = 3;
                    _scalePlate.Enabled = false;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 mp = Physics.FindSafeSpawnPoint(Position + new Vector3((i - 1) * 7f, 0f, 8f));
                        var m = new CombatTarget(mp, "hollow_shadow", g is GameRunContext grc ? grc.DifficultyCoeff : 1.3f);
                        CombatManager.Enemies.Add(m);
                        g.StageEntities.Add(m);
                        _moths.Add(m);
                    }
                    g.ShowBanner("КЛЕТКА 3/3 (БОЛЬШАЯ ПТИЦА): ЗАЩИТИТЕ ВЕЧНУЮ ЛАМПУ ОТ ТЕНЕЙ ЛЕСА!", Color.Orange, 3.5f);
                }
            }
            else if (_phase == 3)
            {
                _moths.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                _labelEnt.BillboardText = $"Испытание 3/3 (Большая Птица): Защитите Вечную Лампу — врагов {_moths.Count}";
                if (_moths.Count == 0)
                {
                    g.UnlockAchievement("ach_char_bird_trio");
                    Destroy(this);
                }
            }
        }
    }

    // ПТИЦА АПОКАЛИПСИСА (Соло/Кооп Рейд Чёрного Леса): Разбить 3 Яйца Апокалипсиса + Зверь Сумерек!
    public class TwilightAltarRaidEncounter : Entity
    {
        private bool _started = false;
        private readonly List<(Entity Egg, float Hp, string Title)> _eggs = new();
        private CombatTarget? _raidBoss = null;
        private float _hitCd = 0f;
        private readonly Entity _labelEnt;

        public TwilightAltarRaidEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "diamond", scale: new(3.2f, 0.55f, 3.2f), position: new(0, 0.28f, 0), color: Rgb(22, 18, 28));
            new Entity(parent: this, model: "diamond", scale: new(1.4f, 3.2f, 1.4f), position: new(0, 2.0f, 0), color: Rgb(255, 195, 55), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 4.4f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Врата Сумерек Чёрного Леса (Рейд Птицы Апокалипсиса)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _hitCd = MathF.Max(0f, _hitCd - dt);
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _started);

            if (!_started && dist < 4.8f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _started = true;
                (string name, Color col)[] eggDefs =
                {
                    ("Яйцо Больших Глаз", Color.Gold),
                    ("Яйцо Длинных Рук", Rgb(235, 225, 195)),
                    ("Яйцо Малого Клюва", Rgb(255, 65, 85))
                };
                for (int i = 0; i < 3; i++)
                {
                    float a = i * (MathF.PI * 2f / 3f);
                    Vector3 ep = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 11f, 0f, MathF.Sin(a) * 11f));
                    var egg = new Entity(position: ep);
                    new Entity(parent: egg, model: "sphere", scale: new(1.6f, 2.3f, 1.6f), position: new(0, 1.2f, 0), color: eggDefs[i].col, unlit: true);
                    egg.BillboardText = $"{eggDefs[i].name} [300 HP]";
                    egg.Color = eggDefs[i].col;
                    g.StageEntities.Add(egg);
                    _eggs.Add((egg, 300f, eggDefs[i].name));
                }
                Vector3 bp = Physics.FindSafeSpawnPoint(Position + new Vector3(0f, 0f, -9f));
                _raidBoss = new CombatTarget(bp, "binah_arbiter", g is GameRunContext grc ? grc.DifficultyCoeff * 0.95f : 1.5f);
                CombatManager.Enemies.Add(_raidBoss);
                g.StageEntities.Add(_raidBoss);
                SoundManager.Play("boss_roar", 1.25f, 0.75f);
                g.ShowBanner("★ ВРАТА СУМЕРЕК ОТКРЫЛИСЬ! РАЗБЕЙТЕ 3 ЯЙЦА АПОКАЛИПСИСА И СОКРУШИТЕ СТРАЖА ЛЕСА! ★", Color.Gold, 4.5f);
            }
            else if (_started)
            {
                for (int i = _eggs.Count - 1; i >= 0; i--)
                {
                    var item = _eggs[i];
                    if (!IsValid(item.Egg)) { _eggs.RemoveAt(i); continue; }
                    if (_hitCd <= 0f && CharacterUnlockEncounters.DetectPlayerAttackNear(g, item.Egg.Position + new Vector3(0, 1.2f, 0), 3.0f))
                    {
                        _hitCd = 0.18f;
                        float newHp = item.Hp - 85f;
                        new VisualFX(item.Egg.Position + new Vector3(0, 1.2f, 0), Color.Gold, 3.2f, 0.22f);
                        if (newHp <= 0f)
                        {
                            SoundManager.Play("explosion", 1.0f, 1.1f);
                            Destroy(item.Egg);
                            _eggs.RemoveAt(i);
                            g.ShowBanner($"РАЗБИТО {item.Title.ToUpper()}! ОСТАЛОСЬ ЯИЦ: {_eggs.Count}/3", Color.Gold, 2.4f);
                        }
                        else
                        {
                            item.Egg.BillboardText = $"{item.Title} [{(int)newHp} HP]";
                            _eggs[i] = (item.Egg, newHp, item.Title);
                        }
                    }
                }

                bool bossDead = _raidBoss == null || !IsValid(_raidBoss) || _raidBoss.Hp <= 0f || _raidBoss.IsAlly;
                // Пока живы Яйца, страж регенерирует!
                if (!bossDead && _eggs.Count > 0 && _raidBoss != null)
                    _raidBoss.Hp = MathF.Min(_raidBoss.MaxHp, _raidBoss.Hp + _raidBoss.MaxHp * 0.04f * dt);

                _labelEnt.BillboardText = $"Рейд Сумерек: Яиц Апокалипсиса {_eggs.Count}/3 | Страж Леса {(bossDead ? "Повержен" : "Жив")}";
                if (_eggs.Count == 0 && bossDead)
                {
                    g.UnlockAchievement("ach_char_apocalypse_bird");
                    Destroy(this);
                }
            }
        }
    }
}
