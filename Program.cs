using System;
using System.Numerics;
using Raylib_cs;
using System.Collections.Generic;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using RiskOfCSharp2.Entities;
using RiskOfCSharp2.Models;
using RiskOfCSharp2.World;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2
{
    public partial class GameRunContext : IPlayerContext
    {
        public Entity PlayerEntity = null!;
        public SurvivorParts Parts = null!;
        public SurvivorCombatController Combat = null!;
        public string CharKey = "commando";
        public float CamPitch = 10f;
        public Vector3 DashVector = Vector3.Zero;
        public float DashTimer = 0f;
        public float ShiftCd = 0f;

        public Vector3 PlayerPos { get => PlayerEntity.Position; set => PlayerEntity.Position = value; }
        public Vector3 PlayerForward => PlayerEntity.Forward;
        public float PlayerVelY { get; set; }
        public bool PlayerGrounded { get; set; } = true;
        public int PlayerJumpsLeft { get; set; } = 1;
        public int PlayerMaxJumps => 1 + (Items.TryGetValue("feather", out int f) ? f : 0);
        public float PlayerHp { get; set; } = 115f;
        public float PlayerMaxHp
        {
            get
            {
                int baseHp = SurvivorDatabase.Classes[CharKey].Hp;
                int steak = Items.TryGetValue("bison_steak", out int s) ? s * 25 : 0;
                int doran = Items.TryGetValue("dorans_blade", out int db) ? db * 20 : 0;
                int tarr = Items.TryGetValue("tarrasque", out int t) ? t * 100 : 0;
                float raw = baseHp + steak + doran + tarr + (Combat != null ? Combat.InfusionBonus : 0f);
                float transMult = 1.0f + 0.50f * (Items.TryGetValue("transcendence", out int tr) ? tr : 0);
                float fluxMult = MathF.Pow(2.0f, Items.TryGetValue("lunar_pauldron", out int fl) ? fl : 0);
                float tonicMult = (1.0f + 0.25f * (Items.TryGetValue("spinel_tonic", out int tn) ? tn : 0))
                                  * MathF.Pow(0.88f, Items.TryGetValue("tonic_affliction", out int ta) ? ta : 0);
                float glassDiv = MathF.Pow(0.5f, Items.TryGetValue("shaped_glass", out int gl) ? gl : 0) * (GameData.HasArtifact("glass") ? 0.20f : 1.0f);
                return MathF.Max(1f, MathF.Floor(raw * transMult * fluxMult * tonicMult * glassDiv));
            }
        }
        public int Gold { get; set; } = 15;
        public int LunarCoins { get; set; } = 2;
        public bool BlueOrbSpawned { get; set; } = false;
        public CombatTarget? ActiveBoss;
        public string BossTitle = "";
        public bool IsBossAlive => IsValid(ActiveBoss) && !ActiveBoss!.IsAlly;
        public bool CanPlayerInteract => !(CharKey == "mindustry" && Combat != null && Combat.MindustryBuildMode) && !Program.HasAnyInGameModalOpen();
        public float MoonDetonationTimer { get; set; } = 0f;
        public bool MithrixSpawned = false;

        public float ElapsedTime = 0f;
        public float StageElapsedTime = 0f;
        public float DirectorCredits = 0f;
        public float SpawnTimer = 1.0f;
        public string StageKinType = "";
        public float VengeanceTimer = 85.0f;

        // Счётчики прогресса достижений за текущий забег
        public int M1CastCount = 0;
        public int M2CastCount = 0;
        public int ShiftCastCount = 0;
        public int RCastCount = 0;
        public int TotalAbilityCasts => M1CastCount + M2CastCount + ShiftCastCount + RCastCount;
        public float MoveStreakTimer = 0f;
        public bool DroppedLowHp = false;
        public bool SurvivedCriticalHp = false;
        public int HitsTakenCount = 0;
        public float MultiKillWindowTimer = 0f;
        public int MultiKillCount = 0;
        public float FrenzyWindowTimer = 0f;
        public int FrenzyKillCount = 0;
        public float UntouchedCombatTimer = 0f;
        public int CloseKills = 0;
        public int EliteOrBossKills = 0;
        public int ChestsOpened = 0;
        public int PyroKills = 0;
        public int CritHitsCount = 0;
        public float TeleporterActiveTimer = 0f;
        public bool TeleporterChargingNow = false;
        public int AirborneKills = 0;
        public float PostHitTimer = 0f;
        public int ThornsKills = 0;
        public float TotalHealed = 0f;

        public Dictionary<string, int> Items { get; } = new();
        public List<string> ItemOrder { get; } = new();
        public List<Entity> StageEntities { get; } = new();

        public int Stage = 1;
        public string StageName = "";
        public int MoonPillarsDone = 0;
        public Entity? ActiveTeleporterOrPad;

        public float DifficultyCoeff
        {
            get
            {
                float minutes = ElapsedTime / 60.0f;
                float scaleM = GameData.GetActiveDifficulty().ScaleMult;
                return (1.0f + 0.26f * minutes * scaleM) * MathF.Pow(1.0f + 0.28f * scaleM, Stage - 1);
            }
        }

        public (string Label, Color Col) GetDifficultyInfo()
        {
            float c = DifficultyCoeff;
            if (c < 1.3f) return ("ЛЕГКО", Rgb(80, 225, 100));
            if (c < 1.8f) return ("СРЕДНЕ", Rgb(255, 210, 60));
            if (c < 2.5f) return ("СЛОЖНО", Rgb(255, 130, 40));
            if (c < 3.5f) return ("ОЧЕНЬ СЛОЖНО", Rgb(245, 55, 55));
            return ("БЕЗУМИЕ (ХА-ХА-ХА)", Rgb(210, 50, 255));
        }

        public Vector3 GetNearestPlayerPos(Vector3 fromPos, out bool isLocalPlayer)
        {
            return Program.GetNearestPlayerPos(fromPos, out isLocalPlayer);
        }

        public bool IsNearRemotePlayer(Vector3 pos, float radius)
        {
            return Program.IsNearRemotePlayer(pos, radius);
        }

        public void NotifyEnemyDamaged(int netId, float amount, float currentHp, int phase)
        {
            if (!Net.NetworkManager.Instance.Connected) return;
            Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "enemy_hit",
                ["id"] = netId,
                ["dmg"] = MathF.Round(amount, 2),
                ["hp"] = MathF.Round(currentHp, 2),
                ["phase"] = phase
            });
        }

        public void NotifyEnemyKilled(int netId)
        {
            if (!Net.NetworkManager.Instance.Connected) return;
            Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "enemy_kill",
                ["id"] = netId
            });
        }

        public void NotifyTeleporterState(string action, bool alignedToMoon)
        {
            if (!Net.NetworkManager.Instance.Connected) return;
            Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "tp_state",
                ["action"] = action,
                ["aligned"] = alignedToMoon
            });
        }

        public void NotifyChestOpened(int chestId, string itemKey)
        {
            if (!Net.NetworkManager.Instance.Connected) return;
            Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "chest_open",
                ["id"] = chestId,
                ["item"] = itemKey
            });
        }

        public void NotifyMoonPillarState(string pillarType, string state)
        {
            if (!Net.NetworkManager.Instance.Connected) return;
            Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "pillar_state",
                ["ptype"] = pillarType,
                ["state"] = state
            });
        }

        public void UnlockAchievement(string achKey)
        {
            if (!GameData.Achievements.TryGetValue(achKey, out var ach)) return;

            var prog = ProgressData.Instance;
            var unlockedRewards = new List<string>();

            bool newlyUnlockedHero = false;
            bool isNewAch = !prog.UnlockedAchievements.Contains(achKey);
            // 1. Глобальное достижение, предмет и персонаж для всех забегов
            if (isNewAch)
            {
                prog.UnlockedAchievements.Add(achKey);
                if (achKey == "ach_safe_travels")
                    prog.MithrixDefeated = true;
                if (!string.IsNullOrEmpty(ach.UnlocksItem) && GameData.ItemPool.TryGetValue(ach.UnlocksItem, out var itemInfo))
                {
                    prog.UnlockedItems.Add(ach.UnlocksItem);
                    unlockedRewards.Add($"Предмет «{itemInfo.Name}»");
                }
                if (!string.IsNullOrEmpty(ach.UnlocksCharacter) && SurvivorDatabase.Classes.TryGetValue(ach.UnlocksCharacter, out var charCfg))
                {
                    if (!prog.UnlockedCharacters.Contains(ach.UnlocksCharacter))
                        newlyUnlockedHero = true;
                    prog.UnlockedCharacters.Add(ach.UnlocksCharacter);
                    unlockedRewards.Add($"ВЫЖИВШИЙ «{charCfg.Name}»");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(ach.UnlocksItem) && !prog.UnlockedItems.Contains(ach.UnlocksItem) && GameData.ItemPool.TryGetValue(ach.UnlocksItem, out var itemInfo))
                {
                    prog.UnlockedItems.Add(ach.UnlocksItem);
                    unlockedRewards.Add($"Предмет «{itemInfo.Name}»");
                }
                if (!string.IsNullOrEmpty(ach.UnlocksCharacter) && !prog.UnlockedCharacters.Contains(ach.UnlocksCharacter) && SurvivorDatabase.Classes.TryGetValue(ach.UnlocksCharacter, out var charCfg))
                {
                    newlyUnlockedHero = true;
                    prog.UnlockedCharacters.Add(ach.UnlocksCharacter);
                    unlockedRewards.Add($"ВЫЖИВШИЙ «{charCfg.Name}»");
                }
            }

            // 2. Персональные альтернативные навыки и скины только для текущего выжившего (CharKey и его активной альт-версии скина)
            if (SurvivorDatabase.Classes.TryGetValue(CharKey, out var cfg))
            {
                string profileKey = prog.GetLoadoutProfileKey(CharKey);
                var activeSkin = cfg.GetActiveSkin();
                var activeVariantsMap = activeSkin.CustomSkillVariants ?? cfg.SkillVariants;
                string ownerTitle = activeSkin.CustomSkillVariants != null ? activeSkin.Name : cfg.Name;

                foreach (var slot in new[] { "m1", "m2", "shift", "r" })
                {
                    if (!activeVariantsMap.TryGetValue(slot, out var variants)) continue;
                    for (int vi = 1; vi < variants.Count; vi++)
                    {
                        var vSkill = variants[vi];
                        string skillUnlockId = $"{profileKey}_{slot}_{vi}";
                        if (vSkill.UnlockAchKey == achKey && !prog.UnlockedSkills.Contains(skillUnlockId))
                        {
                            prog.UnlockedSkills.Add(skillUnlockId);
                            unlockedRewards.Add($"Навык [{ownerTitle}] «{vSkill.Name}»");
                        }
                    }
                }

                for (int si = 1; si < cfg.Skins.Count; si++)
                {
                    var skin = cfg.Skins[si];
                    string skinUnlockId = $"{CharKey}_{si}";
                    if (skin.UnlockAchKey == achKey && !prog.UnlockedSkins.Contains(skinUnlockId))
                    {
                        prog.UnlockedSkins.Add(skinUnlockId);
                        unlockedRewards.Add($"Скин [{cfg.Name}] «{skin.Name}»");
                    }
                }
            }

            if (isNewAch && unlockedRewards.Count == 0)
            {
                unlockedRewards.Add(!string.IsNullOrEmpty(ach.RewardText) ? ach.RewardText : ach.Name);
            }

            if (unlockedRewards.Count > 0)
            {
                prog.Save();
                SoundManager.Play("victory", 0.9f);
                ShowBanner($"ДОСТИЖЕНИЕ «{ach.Name}» :: ОТКРЫТО: {string.Join(" + ", unlockedRewards)}!", Color.Gold, 4.5f);
                if (newlyUnlockedHero && !string.IsNullOrEmpty(ach.UnlocksCharacter))
                    TriggerCharacterUnlockCutscene(ach.UnlocksCharacter);
            }
        }

        public void HealPlayer(float amount)
        {
            if (amount <= 0f) return;
            if (GameData.SelectedDifficulty == "eclipse") amount *= 0.65f;
            TotalHealed += amount;
            if (TotalHealed >= 250f) UnlockAchievement("ach_overheal");
            float maxH = PlayerMaxHp;
            float newHp = PlayerHp + amount;
            if (newHp > maxH)
            {
                float overflow = newHp - maxH;
                PlayerHp = maxH;
                int aegis = Items.TryGetValue("aegis", out int a) ? a : 0;
                if (aegis > 0 && Combat != null)
                {
                    float gain = overflow * MathF.Min(1.0f, 0.5f * aegis);
                    Combat.Shield = MathF.Min(maxH * aegis, Combat.Shield + gain);
                }
            }
            else PlayerHp = newHp;
            if (DroppedLowHp && PlayerHp >= maxH * 0.85f)
                UnlockAchievement("ach_field_medic");
        }

        public void ApplyDash(Vector3 dashVec, float duration)
        {
            DashVector = dashVec;
            DashTimer = duration;
        }

        public void CollectItem(string itemKey)
        {
            if (itemKey == "spinel_tonic")
            {
                int spoiled = Items.TryGetValue("tonic_affliction", out int sa) ? sa : 0;
                if (spoiled > 0)
                {
                    Items["tonic_affliction"] = 0;
                    ItemOrder.Remove("tonic_affliction");
                    Items["spinel_tonic"] = (Items.TryGetValue("spinel_tonic", out int st) ? st : 0) + spoiled;
                }
                if (!ItemOrder.Contains("spinel_tonic")) ItemOrder.Add("spinel_tonic");
                Items["spinel_tonic"] = (Items.TryGetValue("spinel_tonic", out int curT) ? curT : 0) + 1;
                if (Combat != null) Combat.TonicBuffTimer = 25.0f;
                HealPlayer(PlayerMaxHp * 0.25f);
                SoundManager.Play("item_pickup");
                ShowBanner($"ЛУННЫЙ ТОНИК АКТИВЕН (25 СЕК)! ВСЕ ИСПОРЧЕННЫЕ ТОНИКИ ВОССТАНОВЛЕНЫ (x{Items["spinel_tonic"]})!", Rgb(65, 195, 255), 3.8f);
                UnlockAchievement("ach_lunar_addict");
            }
            else
            {
                if (!ItemOrder.Contains(itemKey)) ItemOrder.Add(itemKey);
                Items[itemKey] = (Items.TryGetValue(itemKey, out int c) ? c : 0) + 1;
                var info = GameData.ItemPool[itemKey];
                SoundManager.Play(info.Tier is "red" or "void" ? "item_legendary" : "item_pickup");
                ShowBanner($"ПОДОБРАНО: {info.Name} ({info.Desc})", info.Color, 3.2f);
                if (info.Tier is "lunar" or "void")
                    UnlockAchievement("ach_lunar_addict");
                if (itemKey == "purity")
                    UnlockAchievement("ach_lunar_purity");
                if (itemKey == "lunar_pauldron")
                    UnlockAchievement("ach_lunar_stone");
            }

            int total = 0;
            foreach (var v in Items.Values) total += v;
            if (total >= 6) UnlockAchievement("ach_hoarder");
            if (total >= 12) UnlockAchievement("ach_arsenal");
        }

        public void ExpireSpinelTonic()
        {
            int activeTonics = Items.TryGetValue("spinel_tonic", out int st) ? st : 0;
            if (activeTonics <= 0) return;

            Items["spinel_tonic"] = 0;
            ItemOrder.Remove("spinel_tonic");

            if (!ItemOrder.Contains("tonic_affliction")) ItemOrder.Add("tonic_affliction");
            Items["tonic_affliction"] = (Items.TryGetValue("tonic_affliction", out int ta) ? ta : 0) + activeTonics;
            PlayerHp = MathF.Min(PlayerHp, PlayerMaxHp);
            SoundManager.Play("ui_error");
            ShowBanner($"ДЕЙСТВИЕ ТОНИКА ИСТЕКЛО! ТОНИКИ ИСПОРТИЛИСЬ (x{Items["tonic_affliction"]}): ВСЕ СТАТЫ СНИЖЕНЫ!", Rgb(175, 95, 255), 4.0f);
        }

        public void SpawnItemDrop(Vector3 pos, string? itemKey = null, string poolType = "small_chest")
        {
            if (poolType is "small_chest" or "large_chest" or "legendary_chest" or "lunar_pod" or "shrine_chance")
            {
                ChestsOpened++;
                if (ChestsOpened >= 4) UnlockAchievement("ach_chest_opener");
                if (poolType == "lunar_pod") UnlockAchievement("ach_lunar_stone");
            }
            string key = itemKey != null && GameData.ItemPool.ContainsKey(itemKey) ? itemKey : GameData.RollItemFromPool(poolType);
            StageEntities.Add(new ItemDrop(pos, key));
            if (Net.NetworkManager.Instance.Connected)
            {
                Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "item_drop",
                    ["item"] = key,
                    ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(pos.X, 2), MathF.Round(pos.Y, 2), MathF.Round(pos.Z, 2))
                });
            }
        }

        public void SpawnItemDropNet(Vector3 pos, string key)
        {
            if (GameData.ItemPool.ContainsKey(key))
                StageEntities.Add(new ItemDrop(pos, key));
        }

        public void OpenCommandPicker(string tier)
        {
            Program.EnqueueCommandPicker(tier);
        }

        public void OpenWorkbench()
        {
            Program.OpenSteveWorkbench();
        }

        public void ShowBanner(string msg, Color col, float duration = 2.5f)
            => Program.ShowBanner(msg, col, duration);

        public void SpawnBoss(Vector3 pos, string? bossType = null, int? forcedNetId = null, bool fromNetwork = false)
        {
            var rng = new Random();
            string bType = bossType != null && GameData.IsKnownBossType(bossType)
                ? bossType
                : GameData.BossTypes[rng.Next(GameData.BossTypes.Length)];
            float fy = Physics.GetSurfaceHeight(pos.X, pos.Z, pos.Y);
            var boss = new CombatTarget(new Vector3(pos.X, fy, pos.Z), bType, DifficultyCoeff, forcedNetId);
            ActiveBoss = boss;
            BossTitle = GameData.BossTitles.TryGetValue(bType, out var bt) ? bt : GameData.BossTitles["boss"];
            CombatManager.Enemies.Add(boss);
            StageEntities.Add(boss);

            if (Net.NetworkManager.Instance.Connected && !fromNetwork)
            {
                Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "enemy_spawn",
                    ["id"] = boss.NetId,
                    ["et"] = bType,
                    ["coeff"] = MathF.Round(DifficultyCoeff, 3),
                    ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(pos.X, 2), MathF.Round(fy, 2), MathF.Round(pos.Z, 2)),
                    ["boss"] = true
                });
            }
        }

        public void OnTeleporterActivated()
        {
            TeleporterActiveTimer = 0f;
            TeleporterChargingNow = true;
            if (StageElapsedTime <= 180f)
                UnlockAchievement("ach_wind_runner");
            if (Net.NetworkManager.Instance.Mode != "client")
            {
                Vector3 tpPos = ActiveTeleporterOrPad != null ? ActiveTeleporterOrPad.Position : PlayerPos;
                SpawnBoss(tpPos + new Vector3(9f, 0f, 9f));
                if (GameData.SelectedDifficulty == "eclipse")
                {
                    foreach (float offX in new[] { -7f, 7f })
                    {
                        float gy = Physics.GetSurfaceHeight(tpPos.X + offX, tpPos.Z - 6f, tpPos.Y);
                        var guard = new CombatTarget(new Vector3(tpPos.X + offX, gy, tpPos.Z - 6f), "golem", DifficultyCoeff * 1.15f);
                        guard.IsElite = true;
                        CombatManager.Enemies.Add(guard);
                        StageEntities.Add(guard);
                    }
                }
            }
            ShowBanner("ТЕЛЕПОРТ АКТИВИРОВАН! ПОБЕДИТЕ БОССА ТЕЛЕПОРТА!", Color.Red, 3.2f);
        }

        public void NextStage()
        {
            Stage++;
            if (Stage >= 2) UnlockAchievement("ach_traveler");
            if (Stage >= 3) UnlockAchievement("ach_stage3");
            if (Stage >= 4) UnlockAchievement("ach_stage4");
            Program.LoadStage(Stage, null);
        }

        public void EnterBazaar()
        {
            UnlockAchievement("ach_lunar_purity");
            Program.LoadStage(Stage, "bazaar");
        }
        public void EnterMoonStage() => Program.LoadStage(Stage, "moon");
        public void EnterSecretStage(string modeKey) => Program.LoadStage(Stage, modeKey);

        public void OnMoonPillarCharged()
        {
            MoonPillarsDone++;
            if (ActiveTeleporterOrPad is LunarJumpPad pad)
            {
                pad.UpdateProgressLabel(MoonPillarsDone, 4);
                if (MoonPillarsDone >= 4)
                {
                    pad.UnlockPad();
                    ShowBanner("ВСЕ 4 СТОЛПА ЗАРЯЖЕНЫ! ГРАВИ-ЛИФТ В ЦЕНТРЕ ОТКРЫТ (Y=120м)!", Color.SkyBlue, 5.0f);
                    return;
                }
            }
            ShowBanner($"СТОЛП ЗАРЯЖЕН ({MoonPillarsDone}/4)! ОСТАЛОСЬ ЕЩЁ {Math.Max(0, 4 - MoonPillarsDone)}", Color.Lime, 3.2f);
        }

        public void StartMithrixEncounter()
        {
            if (MithrixSpawned) return;
            MithrixSpawned = true;
            if (CharKey == "mindustry" && Combat != null && IsValid(Combat.MindustryMainCore) && Combat.MindustryMainCore!.Y < 100f)
            {
                Destroy(Combat.MindustryMainCore);
                Combat.MindustryMainCore = null;
                Combat.MindustryCoreRebuildTimer = 60.0f;
                Combat.MindustryCopper += 80f;
                Combat.MindustrySilicon += 55f;
                Combat.MindustryThorium += 25f;
            }
            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (IsValid(e) && !e.IsBoss) Destroy(e);
            }
            CombatManager.Enemies.RemoveAll(e => !IsValid(e));

            var boss = new CombatTarget(new Vector3(0, Physics.ColosseumY, 22.0f), "mithrix", DifficultyCoeff, forcedNetId: 9999)
            {
                PhaseTransitionTimer = 1.6f,
                AttackTimer = 2.4f,
                SpecialTimer = 6.2f
            };
            ActiveBoss = boss;
            BossTitle = "МИТРИКС [ФАЗА 1/4] :: КОРОЛЬ НИЧЕГО";
            CombatManager.Enemies.Add(boss);
            StageEntities.Add(boss);
            ShowBanner("МИТРИКС, КОРОЛЬ НИЧЕГО [ФАЗА 1/4] :: БИТВА В НЕБЕСНОМ КОЛИЗЕЕ!", Color.SkyBlue, 4.2f);
            if (Net.NetworkManager.Instance.Connected)
                Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject { ["type"] = "mithrix_start" });
        }
        public void OnMithrixDefeated(Vector3 deathPos)
        {
            ActiveBoss = null;
            MoonDetonationTimer = 75.0f;
            ProgressData.Instance.MithrixDefeated = true;
            UnlockAchievement("ach_safe_travels");
            UnlockAchievement("ach_stage4");
            UnlockAchievement("ach_boss_hunter");
            ProgressData.Instance.Save();
            Vector3 shipPos = new(0, 0, -52.0f);
            StageEntities.Add(new EscapeShip(shipPos));
            StageEntities.Add(new LunarEscapePad(new Vector3(0, Physics.ColosseumY, -46.0f), shipPos));
            ShowBanner("МИТРИКС ПОВЕРЖЕН! КОРАБЛЬ «SAFE TRAVELS» РАЗБЛОКИРОВАН! ПРЫГАЙТЕ В ЛУЧ ЭВАКУАЦИИ (75 СЕК)!", Color.Lime, 6.5f);
        }

        public void OnRunVictory()
            => Program.OnVictory();
    }
    public static partial class Program
    {
        private static string _screen = "login"; // "login", "main", "mp_connect", "settings", "lobby", "logbook", "in_game"
        private static string _playerNameInput = "";
        private static string _selectedChar = "commando";
        private static string _lobbyTab = "skills"; // "skills", "overview", "modifiers"
        private static GameRunContext? _run;
        private static bool _paused = false;
        private static bool _isSpectating = false;
        private static int _spectateTargetIdx = 0;
        private static readonly HashSet<string> _deadCoopPlayers = new();
        private static readonly Queue<string> _commandPickerQueue = new();
        private static bool _workbenchOpen = false;

        public static void EnqueueCommandPicker(string tier)
        {
            _commandPickerQueue.Enqueue(tier);
            Raylib.EnableCursor();
        }

        public static void OpenSteveWorkbench()
        {
            _workbenchOpen = true;
            Raylib.EnableCursor();
            SoundManager.Play("interact", 0.9f);
        }

        // Индикаторы попаданий и урона в стиле CS + Статистика забега
        private static float _hitMarkerTimer = 0f;
        private static bool _hitMarkerCrit = false;
        private static float _damageFlashTimer = 0f;

        public static int RunKills = 0;
        public static int RunBossKills = 0;
        public static float RunDamageDealt = 0f;
        public static int RunGoldEarned = 0;
        private static bool _lastRunVictory = false;
        private static float _lastRunTime = 0f;
        private static int _lastRunStage = 1;
        private static readonly List<(string Key, int Count)> _lastRunItems = new();

        public static void TriggerHitMarker(bool isCrit, float dmg)
        {
            _hitMarkerTimer = 0.18f;
            _hitMarkerCrit = isCrit;
            RunDamageDealt += dmg;
            if (_run != null && !_run.IsSafeTravels)
            {
                if (dmg >= 90f) _run.UnlockAchievement("ach_first_strike");
                if (dmg >= 220f) _run.UnlockAchievement("ach_fire_band");
                if (RunDamageDealt >= 3000f) _run.UnlockAchievement("ach_burn_bright");
                if (RunDamageDealt >= 8000f) _run.UnlockAchievement("ach_armor_breaker");
                if (isCrit)
                {
                    _run.CritHitsCount++;
                    if (_run.CritHitsCount >= 10) _run.UnlockAchievement("ach_crit_reaper");
                }
            }
        }

        public static void TriggerDamageFlash(float dmg)
        {
            if (dmg > 0f)
            {
                _damageFlashTimer = Math.Clamp(0.25f + dmg * 0.008f, 0.25f, 0.65f);
                if (_run != null && !_run.IsSafeTravels)
                {
                    _run.HitsTakenCount++;
                    _run.UntouchedCombatTimer = 0f;
                    _run.PostHitTimer = 4.5f;
                    if (_run.HitsTakenCount >= 12) _run.UnlockAchievement("ach_tank_hits");
                }
            }
        }

        public static void RecordEnemyKill(int goldGain, CombatTarget? dead = null)
        {
            RunKills++;
            RunGoldEarned += goldGain;
            if (_run != null && !_run.IsSafeTravels)
            {
                if (_run.Stage == 1 && RunKills >= 8) _run.UnlockAchievement("ach_trophies");
                if (RunKills >= 12) _run.UnlockAchievement("ach_slayer");
                if (RunKills >= 25) _run.UnlockAchievement("ach_carnage");
                if (RunKills >= 35) _run.UnlockAchievement("ach_high_voltage");
                if (RunKills >= 50) _run.UnlockAchievement("ach_ritual_blood");
                if (RunGoldEarned >= 120 || _run.Gold >= 120) _run.UnlockAchievement("ach_doran_start");

                if (_run.MultiKillWindowTimer <= 0f)
                {
                    _run.MultiKillWindowTimer = 5.0f;
                    _run.MultiKillCount = 1;
                }
                else
                {
                    _run.MultiKillCount++;
                    _run.MultiKillWindowTimer = MathF.Max(_run.MultiKillWindowTimer, 4.0f);
                }
                if (_run.MultiKillCount >= 3) _run.UnlockAchievement("ach_demolition");
                if (_run.MultiKillCount >= 4) _run.UnlockAchievement("ach_shield_bearer");

                if (_run.FrenzyWindowTimer <= 0f)
                {
                    _run.FrenzyWindowTimer = 12.0f;
                    _run.FrenzyKillCount = 1;
                }
                else
                {
                    _run.FrenzyKillCount++;
                }
                if (_run.FrenzyKillCount >= 6) _run.UnlockAchievement("ach_frenzy");

                if (!_run.PlayerGrounded)
                {
                    _run.AirborneKills++;
                    if (_run.AirborneKills >= 6) _run.UnlockAchievement("ach_airborne");
                }
                if (_run.PostHitTimer > 0f)
                {
                    _run.ThornsKills++;
                    if (_run.ThornsKills >= 5) _run.UnlockAchievement("ach_thorns");
                }

                if (dead != null)
                {
                    if (dead.IsBoss)
                    {
                        RunBossKills++;
                        _run.UnlockAchievement("ach_boss");
                        if (RunBossKills >= 2) _run.UnlockAchievement("ach_boss_hunter");
                    }
                    if (Vector3.Distance(_run.PlayerPos, dead.Position) <= 13.0f)
                    {
                        _run.CloseKills++;
                        if (_run.CloseKills >= 10) _run.UnlockAchievement("ach_close_combat");
                        if (_run.CloseKills >= 20) _run.UnlockAchievement("ach_blizzard");
                    }
                    if (dead.IsElite || dead.IsBoss || dead.IsDoppelganger)
                    {
                        _run.EliteOrBossKills++;
                        if (_run.EliteOrBossKills >= 3) _run.UnlockAchievement("ach_shock_tactics");
                        if (_run.EliteOrBossKills >= 7) _run.UnlockAchievement("ach_elite_slayer");
                    }
                    if (dead.EnemyType is "wisp" or "greater_wisp" or "lemurian" or "elder_lemurian" or "imp" or "nether_blaze" or "hell_drone")
                    {
                        _run.PyroKills++;
                        if (_run.PyroKills >= 10) _run.UnlockAchievement("ach_wisp_hunter");
                    }
                    if (dead.IsBoss && _run.TeleporterChargingNow && _run.TeleporterActiveTimer <= 45.0f)
                    {
                        _run.UnlockAchievement("ach_ice_band");
                    }
                }
            }
        }

        public static bool HasAnyDeadCoopPlayers() => _deadCoopPlayers.Count > 0;

        public static bool TryHealRemotePlayerCoop(Vector3 hitPos, float radius, float healAmount)
        {
            if (!_net.Connected || Physics.SpecialMode == "pvp") return false;
            foreach (var kv in _remotePlayers)
            {
                var rp = kv.Value;
                if (IsValid(rp) && rp.Hp > 0f && Vector3.Distance(hitPos, rp.Position + new Vector3(0, 1.1f, 0)) <= radius)
                {
                    _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "ally_heal",
                        ["target_id"] = kv.Key,
                        ["heal"] = MathF.Round(healAmount, 1),
                        ["uber"] = false
                    });
                    return true;
                }
            }
            return false;
        }

        // Настройки
        private static float _mouseSens = 0.18f;
        private static float _fov = 60f;

        // Сетевое состояние и Лобби
        private static readonly Net.NetworkManager _net = new();
        private static readonly Dictionary<string, (string CharKey, bool Ready)> _lobbyPlayers = new();
        private static readonly Dictionary<string, RemotePlayer> _remotePlayers = new();
        private static bool _myReady = false;
        private static bool _lobbyCountdownActive = false;
        private static float _lobbyCountdown = 60f;
        private static bool _typingCode = false;
        private static string _roomCodeInput = "";
        private static string _netStatusText = "";
        private static float _netSyncTimer = 0f;
        private static Vector3 _lastResolvedPlayerPos = Vector3.Zero;
        private static bool _playerSweepValid = false;
        private static float _enemySyncTimer = 0f;

        private static string _bannerText = "";
        private static Color _bannerColor = Color.Yellow;
        private static float _bannerTimer = 0f;

        // 3D Диорама
        private static Entity _mainPedestal = null!;
        private static Entity _pedestalRing = null!;
        private static Entity _previewActor = null!;
        private static SurvivorParts? _previewSurvivorParts;
        private static EnemyParts? _previewEnemyParts;
        private static Entity _menuCrystal = null!;

        private static float _previewBaseScale = 1.0f;
        private static float _previewZoom = 1.0f;
        private static float _manualTimer = 0.0f;
        private static Entity? _activeSkillDemo = null;
        private static float _skillPreviewTimer = 0f;
        private static string _skillPreviewTitle = "";
        private static Color _skillPreviewColor = Color.SkyBlue;

        // Данные об убийце игрока (для отображения моба в правом верхнем углу после смерти)
        private static string _lastKillerKey = "lemurian";
        private static string _lastKillerName = "Лемурианец";
        private static string _lastKillerRank = "ОБЫЧНЫЙ ВРАГ";
        private static string _lastKillerDesc = "Коренной ящер Petrichor V. Атакует огненными шарами и когтями вблизи.";
        private static float _lastKillerDamage = 12f;
        private static Color _lastKillerColor = Rgb(255, 110, 65);
        private static bool _lastKillerIsBoss = false;
        private static bool _lastKillerIsElite = false;
        private static bool _lastKillerIsDoppel = false;
        private static bool _summaryShowKiller3D = true;

        public static void RecordPlayerDamageSource(CombatTarget? attacker, float dmg, string? customSource = null)
        {
            _lastKillerDamage = MathF.Max(1f, dmg);

            if (customSource == "pvp")
            {
                _lastKillerKey = "pvp_rival";
                _lastKillerName = "Соперник на Арене PvP";
                _lastKillerRank = "ИГРОК-ДУЭЛЯНТ [PvP]";
                _lastKillerDesc = "Враждебный Выживший на Арене Раздора, нанёсший вам решающий удар.";
                _lastKillerColor = Rgb(255, 65, 85);
                _lastKillerIsBoss = false;
                _lastKillerIsElite = true;
                _lastKillerIsDoppel = false;
                return;
            }
            if (customSource == "chaos")
            {
                _lastKillerKey = "chaos_self";
                _lastKillerName = "Артефакт Хаоса";
                _lastKillerRank = "ДРУЖЕСТВЕННЫЙ ОГОНЬ";
                _lastKillerDesc = "Собственный взрыв по области задел вас из-за активного Артефакта Хаоса.";
                _lastKillerColor = Rgb(255, 95, 55);
                _lastKillerIsBoss = false;
                _lastKillerIsElite = false;
                _lastKillerIsDoppel = false;
                return;
            }
            if (customSource == "moon_detonation")
            {
                _lastKillerKey = "mithrix";
                _lastKillerName = "Детонация Луны";
                _lastKillerRank = "КАТАКЛИЗМ PETRICHOR V";
                _lastKillerDesc = "Таймер эвакуации истёк до того, как спасательный корабль покинул орбиту Луны.";
                _lastKillerColor = Color.SkyBlue;
                _lastKillerIsBoss = true;
                _lastKillerIsElite = false;
                _lastKillerIsDoppel = false;
                return;
            }
            if (customSource == "dante_sacrifice")
            {
                _lastKillerKey = "imp";
                _lastKillerName = "Поворот Часов Данте";
                _lastKillerRank = "ЖЕРТВА МЕНЕДЖЕРА";
                _lastKillerDesc = "Вы отдали собственную жизнь, чтобы повернуть стрелки часов вспять.";
                _lastKillerColor = Color.Gold;
                _lastKillerIsBoss = false;
                _lastKillerIsElite = false;
                _lastKillerIsDoppel = false;
                return;
            }

            string et = "lemurian";
            bool isBoss = false, isElite = false, isDoppel = false;
            int phase = 1;

            if (attacker != null)
            {
                et = attacker.EnemyType;
                isBoss = attacker.IsBoss;
                isElite = attacker.IsElite;
                isDoppel = attacker.IsDoppelganger;
                phase = attacker.Phase;
            }
            else if (!string.IsNullOrEmpty(customSource) && customSource.StartsWith("mob:"))
            {
                string[] parts = customSource.Split(':');
                if (parts.Length >= 5)
                {
                    et = parts[1];
                    isBoss = parts[2] == "1";
                    isElite = parts[3] == "1";
                    isDoppel = parts[4] == "1";
                }
            }
            else if (_run?.ActiveBoss != null && IsValid(_run.ActiveBoss))
            {
                et = _run.ActiveBoss.EnemyType;
                isBoss = true;
                isElite = _run.ActiveBoss.IsElite;
                phase = _run.ActiveBoss.Phase;
            }

            _lastKillerKey = et;
            _lastKillerIsBoss = isBoss;
            _lastKillerIsElite = isElite;
            _lastKillerIsDoppel = isDoppel;

            if (isDoppel)
            {
                _lastKillerName = "Теневой Доппельгангер";
                _lastKillerRank = "★ ВТОРЖЕНИЕ АРТЕФАКТА МЕСТИ";
                _lastKillerDesc = "Ваше собственное потустороннее отражение, вторгшееся в мир за вашей душой.";
                _lastKillerColor = Rgb(215, 75, 255);
            }
            else if (GameData.MonsterLog.TryGetValue(et, out var mInfo))
            {
                _lastKillerName = isElite && !isBoss ? $"Элитный {mInfo.Name}" : mInfo.Name;
                _lastKillerRank = et == "mithrix"
                    ? $"ФИНАЛЬНЫЙ БОСС [ФАЗА {phase}/4]"
                    : (isBoss ? "БОСС ТЕЛЕПОРТА" : (isElite ? "★ ЭЛИТНЫЙ МОНСТР" : "ОБЫЧНЫЙ ВРАГ"));
                _lastKillerDesc = mInfo.Desc;
                _lastKillerColor = et == "mithrix"
                    ? Color.SkyBlue
                    : (isBoss ? Rgb(255, 65, 65) : (isElite ? Rgb(255, 175, 45) : Rgb(255, 115, 65)));
            }
            else
            {
                _lastKillerName = et.ToUpper();
                _lastKillerRank = isBoss ? "БОСС ТЕЛЕПОРТА" : (isElite ? "★ ЭЛИТНЫЙ МОНСТР" : "ОБЫЧНЫЙ ВРАГ");
                _lastKillerDesc = "Опасное существо планеты Petrichor V.";
                _lastKillerColor = isBoss ? Color.Red : Color.Orange;
            }
        }

        // Состояние Журнала (Logbook)
        private static string _logCategory = "items";
        private static int _logPage = 0;
        private static string _logSelectedKey = "syringe";

        public static Vector3 GetNearestPlayerPos(Vector3 fromPos, out bool isLocalPlayer)
        {
            isLocalPlayer = true;
            if (_run == null) return Vector3.Zero;
            Vector3 bestPos = _run.PlayerPos;
            float bestDist = _run.CanPlayerInteract ? Vector3.Distance(fromPos, bestPos) : 99999f;

            foreach (var rp in _remotePlayers.Values)
            {
                if (IsValid(rp) && rp.Hp > 0f)
                {
                    float d = Vector3.Distance(fromPos, rp.Position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestPos = rp.Position;
                        isLocalPlayer = false;
                    }
                }
            }
            return bestPos;
        }

        public static bool IsNearRemotePlayer(Vector3 pos, float radius)
        {
            foreach (var rp in _remotePlayers.Values)
            {
                if (IsValid(rp) && Vector3.Distance(pos, rp.Position + new Vector3(0, 1.0f, 0)) <= radius)
                    return true;
            }
            return false;
        }

        public static Vector3 GetNearestRemotePlayerPos(Vector3 fromPos, out float bestDist)
        {
            bestDist = 999f;
            Vector3 best = Vector3.Zero;
            foreach (var rp in _remotePlayers.Values)
            {
                if (IsValid(rp) && rp.Hp > 0f)
                {
                    float d = Vector3.Distance(fromPos, rp.Position);
                    if (d < bestDist) { bestDist = d; best = rp.Position; }
                }
            }
            return best;
        }

        public static bool TryHitRemotePlayerPvP(Vector3 hitPos, float radius, float dmg)
        {
            if (Physics.SpecialMode != "pvp" || !_net.Connected) return false;
            foreach (var kv in _remotePlayers)
            {
                var rp = kv.Value;
                if (IsValid(rp) && rp.Hp > 0f && Vector3.Distance(hitPos, rp.Position + new Vector3(0, 1.1f, 0)) <= radius)
                {
                    _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "pvp_hit",
                        ["target_id"] = kv.Key,
                        ["attacker_id"] = _net.MyId,
                        ["dmg"] = MathF.Round(dmg, 1)
                    });
                    return true;
                }
            }
            return false;
        }

        public static void ShowBanner(string msg, Color col, float duration = 2.5f)
        {
            _bannerText = msg;
            _bannerColor = col;
            _bannerTimer = duration;
        }

        public static void Main()
        {
            Engine.Init(1536, 864, "Risk of C# 2 — Кооператив по LAN и коду комнаты");
            _netStatusText = $"Ваш локальный IP: {_net.HostIp}";
            _playerNameInput = ProgressData.Instance.PlayerName;
            _screen = "login";
            SetupDiorama();

            while (!Raylib.WindowShouldClose())
            {
                float dt = Math.Min(Raylib.GetFrameTime(), 0.05f);
                ProcessNetwork();
                SoundManager.UpdateAmbient(dt, _screen == "in_game" && !_paused, _run != null && _run.IsBossAlive);
                if (_bannerTimer > 0f) _bannerTimer -= dt;
                if (_hitMarkerTimer > 0f) _hitMarkerTimer -= dt;
                if (_damageFlashTimer > 0f) _damageFlashTimer -= dt;

                if (_screen == "in_game")
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    {
                        if (_dialogueOpen)
                        {
                            CloseSafeTravelsDialogueModal();
                            SoundManager.Play("ui_click");
                        }
                        else if (_rpsOpen)
                        {
                            CloseRpsMiniGame();
                            SoundManager.Play("ui_click");
                        }
                        else if (_emoteWheelOpen)
                        {
                            _emoteWheelOpen = false;
                            SoundManager.Play("ui_click");
                            RestoreInGameCursorIfNeeded();
                        }
                        else if (_safeTravelsWardrobeOpen)
                        {
                            _safeTravelsWardrobeOpen = false;
                            SoundManager.Play("ui_click");
                            RestoreInGameCursorIfNeeded();
                        }
                        else if (_workbenchOpen)
                        {
                            _workbenchOpen = false;
                            SoundManager.Play("ui_click");
                            RestoreInGameCursorIfNeeded();
                        }
                        else
                        {
                            _paused = !_paused;
                            SoundManager.Play("ui_click");
                            RestoreInGameCursorIfNeeded();
                        }
                    }

                    if (!_paused)
                    {
                        UpdateInGame(dt);
                        // Во время катсцены разблокировки мир стоит на паузе: враги, снаряды и ловушки замирают
                        // (в сетевой игре мир продолжает жить у других игроков, поэтому там только неуязвимость)
                        if (!IsCutscenePlaying || _net.Connected)
                            Engine.Update(dt);
                    }
                    Engine.ObstructionFadeEnabled = _run != null && (!SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey) || !string.IsNullOrEmpty(_run.Parts.ActiveEmote) || _dialogueOpen);
                    if (_run != null) Engine.PlayerWorldPos = _run.PlayerPos;
                }
                else
                {
                    Engine.ObstructionFadeEnabled = false;
                    HandleInput(dt);
                    Engine.Update(dt);
                    UpdateDiorama(dt);
                    Engine.Camera.Position = new Vector3(0f, 2.15f, -8.6f);
                    Engine.Camera.FovY = _fov;
                    Engine.CameraEuler = new Vector3(5f, 0f, 0f);
                    Engine.UpdateCameraTargetFromEuler();

                    if (_screen == "lobby" && _net.Connected && _lobbyCountdownActive)
                    {
                        int prevSec = (int)MathF.Ceiling(_lobbyCountdown);
                        _lobbyCountdown = MathF.Max(0f, _lobbyCountdown - dt);
                        int newSec = (int)MathF.Ceiling(_lobbyCountdown);
                        if (newSec < prevSec && newSec <= 5 && newSec > 0)
                            SoundManager.Play("ui_tick");
                        if (_net.Mode == "host" && _lobbyCountdown <= 0f)
                        {
                            _lobbyCountdownActive = false;
                            StartRun(_selectedChar);
                        }
                    }
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Engine.BackgroundColor);

                if (!(_screen == "logbook" && _logDetailOpen))
                {
                    Raylib.BeginMode3D(Engine.Camera);
                    Engine.Draw3D();
                    Raylib.EndMode3D();

                    Engine.Draw3DBillboardLabels(Engine.Camera);
                }

                if (_screen == "in_game")
                {
                    DrawInGameHUD();
                    if (_paused) DrawPauseOverlay();
                }
                else if (_screen == "login") DrawLoginUI();
                else if (_screen == "main") DrawMainMenuUI();
                else if (_screen == "mp_connect") DrawMpConnectUI();
                else if (_screen == "settings") DrawSettingsUI();
                else if (_screen == "lobby") DrawLobbyUI();
                else if (_screen == "run_summary") DrawRunSummaryUI();
                else DrawLogbookUI();
                // Компактный тост уведомлений внизу экрана (не закрывает прицел и полоску Босса!)
                if (_bannerTimer > 0f && !string.IsNullOrEmpty(_bannerText))
                {
                    int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
                    float ty = _screen == "in_game" ? sh - 128f : 108f;
                    Raylib.DrawRectangle(sw / 2 - 420, (int)ty - 18, 840, 36, Rgb(10, 14, 24, 210));
                    Raylib.DrawRectangle(sw / 2 - 420, (int)ty - 18, 840, 2, _bannerColor);
                    Engine.DrawTextCentered(_bannerText, sw * 0.5f, ty, 18f, _bannerColor);
                }

                Raylib.EndDrawing();
            }

            _net.StopSession();
            SoundManager.Shutdown();
            Raylib.CloseWindow();
        }

        private static void ProcessNetwork()
        {
            foreach (var msg in _net.PollMessages())
            {
                string mtype = msg["type"]?.GetValue<string>() ?? "";
                if (mtype == "welcome")
                {
                    _net.MyId = msg["player_id"]?.GetValue<string>() ?? "p2";
                    _net.RoomCode = msg["room_code"]?.GetValue<string>() ?? _net.RoomCode;
                    _net.GameType = msg["game_type"]?.GetValue<string>() ?? _net.GameType;
                    ShowBanner($"ПОДКЛЮЧЕНО К КОМНАТЕ {_net.RoomCode} [{_net.GameType.ToUpper()}]!", Color.Lime, 3.0f);
                    BroadcastLobbyState();
                }
                else if (mtype == "client_joined")
                {
                    string pid = msg["player_id"]?.GetValue<string>() ?? "p2";
                    if (pid != _net.MyId) _lobbyPlayers[pid] = ("commando", false);
                    ShowBanner($"ИГРОК [{pid.ToUpper()}] ВОШЁЛ В ЛОББИ!", Color.SkyBlue, 2.5f);
                    BroadcastLobbyState();
                }
                else if (mtype == "player_left")
                {
                    string pid = msg["player_id"]?.GetValue<string>() ?? "";
                    _lobbyPlayers.Remove(pid);
                    if (_remotePlayers.TryGetValue(pid, out var rp))
                    {
                        Destroy(rp);
                        _remotePlayers.Remove(pid);
                    }
                    ShowBanner($"ИГРОК [{pid.ToUpper()}] ПОКИНУЛ ИГРУ", Color.Orange, 2.5f);
                }
                else if (mtype == "disconnected")
                {
                    _lobbyPlayers.Clear();
                    if (_screen == "in_game") QuitRunToLobby();
                    _screen = "main";
                    ShowBanner("СОЕДИНЕНИЕ С ХОСТОМ ПОТЕРЯНО", Color.Red, 3.5f);
                }
                else if (mtype == "lobby_state")
                {
                    string pid = msg["player_id"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(pid) && pid != _net.MyId)
                    {
                        bool isNew = !_lobbyPlayers.ContainsKey(pid);
                        _lobbyPlayers[pid] = (
                            msg["char_key"]?.GetValue<string>() ?? "commando",
                            msg["ready"]?.GetValue<bool>() ?? false
                        );
                        if (isNew) BroadcastLobbyState();
                        if (_net.Mode == "client" && pid == "host")
                        {
                            string? hostDiff = msg["diff"]?.GetValue<string>();
                            if (!string.IsNullOrEmpty(hostDiff) && GameData.Difficulties.ContainsKey(hostDiff))
                                GameData.SelectedDifficulty = hostDiff;
                            string? hostArts = msg["arts"]?.GetValue<string>();
                            if (hostArts != null)
                            {
                                GameData.ActiveArtifacts.Clear();
                                foreach (var ak in hostArts.Split(',', StringSplitOptions.RemoveEmptyEntries))
                                    if (GameData.RunArtifacts.ContainsKey(ak))
                                        GameData.ActiveArtifacts.Add(ak);
                            }
                        }
                        EvaluateLobbyCountdown();
                    }
                }
                else if (mtype == "lobby_start_run")
                {
                    int st = msg["stage"]?.GetValue<int>() ?? 1;
                    int seed = msg["seed"]?.GetValue<int>() ?? 1001;
                    string? startDiff = msg["diff"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(startDiff) && GameData.Difficulties.ContainsKey(startDiff))
                        GameData.SelectedDifficulty = startDiff;
                    string? startArts = msg["arts"]?.GetValue<string>();
                    if (startArts != null)
                    {
                        GameData.ActiveArtifacts.Clear();
                        foreach (var ak in startArts.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            if (GameData.RunArtifacts.ContainsKey(ak))
                                GameData.ActiveArtifacts.Add(ak);
                    }
                    if (_screen != "in_game")
                        StartRun(_selectedChar, seed, fromNetwork: true, startStage: st);
                }
                else if (mtype == "player_state" && _screen == "in_game" && _run != null)
                {
                    string pid = msg["player_id"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(pid) && pid != _net.MyId)
                    {
                        if (!_remotePlayers.TryGetValue(pid, out var rp) || !IsValid(rp))
                        {
                            rp = new RemotePlayer(pid, msg["char_key"]?.GetValue<string>() ?? "commando", msg["skin_idx"]?.GetValue<int>() ?? 0);
                            _remotePlayers[pid] = rp;
                            _run.StageEntities.Add(rp);
                        }
                        rp.ApplyNetState(msg);
                        if (rp.Hp <= 0f) _deadCoopPlayers.Add(pid);
                        else if (_deadCoopPlayers.Contains(pid)) rp.Hp = 0f;
                    }
                }
                else if (mtype == "enemy_spawn" && _screen == "in_game" && _run != null)
                {
                    int netId = msg["id"]?.GetValue<int>() ?? 0;
                    string et = msg["et"]?.GetValue<string>() ?? "lemurian";
                    float coeff = msg["coeff"]?.GetValue<float>() ?? _run.DifficultyCoeff;
                    bool isBoss = msg["boss"]?.GetValue<bool>() ?? false;
                    Vector3 spawnPos = Vector3.Zero;
                    if (msg["pos"] is System.Text.Json.Nodes.JsonArray parr && parr.Count >= 3)
                    {
                        spawnPos = new Vector3(
                            parr[0]?.GetValue<float>() ?? 0f,
                            parr[1]?.GetValue<float>() ?? 0f,
                            parr[2]?.GetValue<float>() ?? 0f
                        );
                    }
                    if (!CombatManager.Enemies.Exists(e => IsValid(e) && e.NetId == netId))
                    {
                        if (isBoss)
                        {
                            _run.SpawnBoss(spawnPos, et, netId, fromNetwork: true);
                        }
                        else
                        {
                            var ne = new CombatTarget(spawnPos, et, coeff, netId);
                            if (msg["doppel"]?.GetValue<bool>() == true) ne.MakeDoppelganger();
                            else if (msg["elite"]?.GetValue<bool>() == true) { ne.IsElite = true; ne.UpdateHpBillboard(); }
                            CombatManager.Enemies.Add(ne);
                            _run.StageEntities.Add(ne);
                        }
                    }
                }
                else if (mtype == "enemies_sync" && _screen == "in_game" && _run != null && _net.Mode == "client")
                {
                    if (msg["list"] is System.Text.Json.Nodes.JsonArray listArr)
                    {
                        foreach (var itemNode in listArr)
                        {
                            if (itemNode is not System.Text.Json.Nodes.JsonObject eo) continue;
                            int id = eo["id"]?.GetValue<int>() ?? 0;
                            string et = eo["et"]?.GetValue<string>() ?? "lemurian";
                            float coeff = eo["c"]?.GetValue<float>() ?? _run.DifficultyCoeff;
                            float hp = eo["hp"]?.GetValue<float>() ?? 10f;
                            float x = eo["x"]?.GetValue<float>() ?? 0f;
                            float y = eo["y"]?.GetValue<float>() ?? 0f;
                            float z = eo["z"]?.GetValue<float>() ?? 0f;
                            bool isBoss = eo["b"]?.GetValue<bool>() ?? false;
                            bool isAlly = eo["a"]?.GetValue<bool>() ?? false;

                            var existing = CombatManager.Enemies.Find(e => IsValid(e) && e.NetId == id);
                            if (existing == null)
                            {
                                if (isBoss && et != "mithrix")
                                {
                                    _run.SpawnBoss(new Vector3(x, y, z), et, id, fromNetwork: true);
                                    existing = _run.ActiveBoss;
                                }
                                else if (et != "mithrix")
                                {
                                    existing = new CombatTarget(new Vector3(x, y, z), et, coeff, id);
                                    CombatManager.Enemies.Add(existing);
                                    _run.StageEntities.Add(existing);
                                }
                            }
                            if (existing != null && IsValid(existing))
                            {
                                existing.Position = Lerp(existing.Position, new Vector3(x, y, z), 0.45f);
                                existing.Hp = hp;
                                if (isAlly && !existing.IsAlly) existing.ConvertToAlly();
                                if (eo["dp"]?.GetValue<bool>() == true && !existing.IsDoppelganger) existing.MakeDoppelganger();
                                else if (eo["el"]?.GetValue<bool>() == true && !existing.IsElite) existing.IsElite = true;
                                existing.UpdateHpBillboard();
                            }
                        }
                    }
                }
                else if (mtype == "enemy_hit" && _screen == "in_game" && _run != null)
                {
                    int id = msg["id"]?.GetValue<int>() ?? 0;
                    float dmg = msg["dmg"]?.GetValue<float>() ?? 0f;
                    var target = CombatManager.Enemies.Find(e => IsValid(e) && e.NetId == id);
                    if (target != null)
                        target.TakeDamage(dmg, fromNetwork: true);
                }
                else if (mtype == "enemy_kill" && _screen == "in_game" && _run != null)
                {
                    int id = msg["id"]?.GetValue<int>() ?? 0;
                    var target = CombatManager.Enemies.Find(e => IsValid(e) && e.NetId == id);
                    if (target != null)
                        target.Die(fromNetwork: true);
                }
                else if (mtype == "tp_state" && _screen == "in_game" && _run != null)
                {
                    string act = msg["action"]?.GetValue<string>() ?? "";
                    bool aligned = msg["aligned"]?.GetValue<bool>() ?? false;
                    if (_run.ActiveTeleporterOrPad is Teleporter tp)
                    {
                        if (act == "align")
                            tp.SetAlignment(aligned);
                        else if (act == "activate" && tp.State == "idle")
                        {
                            tp.SetAlignment(aligned);
                            tp.State = "charging";
                            _run.OnTeleporterActivated();
                        }
                    }
                }
                else if (mtype == "stage_sync" && _screen == "in_game" && _run != null)
                {
                    int newStage = msg["stage"]?.GetValue<int>() ?? _run.Stage;
                    int newSeed = msg["seed"]?.GetValue<int>() ?? 1001;
                    string? newMode = msg["special_mode"]?.GetValue<string>();
                    LoadStage(newStage, newMode, newSeed, fromNetwork: true);
                }
                else if (mtype == "mithrix_start" && _screen == "in_game" && _run != null)
                {
                    _run.StartMithrixEncounter();
                }
                else if (mtype == "dante_buff" && _screen == "in_game" && _run != null)
                {
                    string kind = msg["kind"]?.GetValue<string>() ?? "atk";
                    if (kind == "atk")
                    {
                        _run.Combat.DanteAtkBuffTimer = 7.0f;
                        ShowBanner("ПРИКАЗ ДАНТЕ :: +45% К УРОНУ И +35% К СКОРОСТИ АТАКИ!", Color.Gold, 2.5f);
                    }
                    else
                    {
                        _run.Combat.DanteSpdBuffTimer = 6.0f;
                        _run.HealPlayer(_run.PlayerMaxHp * 0.30f);
                        _run.Combat.Shield = MathF.Min(_run.PlayerMaxHp, _run.Combat.Shield + 45f);
                        ShowBanner("ПРОТОКОЛ ДАНТЕ :: +45 ЩИТА, ИСЦЕЛЕНИЕ И УСКОРЕНИЕ!", Color.Gold, 2.5f);
                    }
                }
                else if (mtype == "dante_revive" && _screen == "in_game" && _run != null)
                {
                    _deadCoopPlayers.Clear();
            _commandPickerQueue.Clear();
                    _isSpectating = false;
                    _run.PlayerHp = _run.PlayerMaxHp;
                    _run.Combat.Shield = 50f;
                    _run.Parts.Root.Enabled = !SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey);
                    if (_run.Combat.FpRoot != null) _run.Combat.FpRoot.Enabled = true;
                    new VisualFX(_run.PlayerPos + new Vector3(0, 1.2f, 0), Color.Gold, 6.0f, 0.45f);
                    ShowBanner("ДАНТЕ ПОВЕРНУЛ ЧАСЫ ВСПЯТЬ И ВОЗРОДИЛ ВАС ЦЕНОЙ СВОЕЙ ЖИЗНИ!", Color.Gold, 4.5f);
                }
                else if (mtype == "mindustry_build" && _screen == "in_game" && _run != null)
                {
                    int sid = msg["id"]?.GetValue<int>() ?? 5500;
                    string btype = msg["btype"]?.GetValue<string>() ?? "spectre";
                    float ownerMaxHp = msg["max_hp"]?.GetValue<float>() ?? 160f;
                    Vector3 bPos = Vector3.Zero;
                    if (msg["pos"] is System.Text.Json.Nodes.JsonArray parr && parr.Count >= 3)
                    {
                        bPos = new Vector3(
                            parr[0]?.GetValue<float>() ?? 0f,
                            parr[1]?.GetValue<float>() ?? 0f,
                            parr[2]?.GetValue<float>() ?? 0f
                        );
                    }
                    _run.Combat.MindustryTurrets.RemoveAll(t => !IsValid(t));
                    var remoteStruct = new MindustryTurret(bPos, ownerMaxHp, _run.Combat.MindustryTurrets.Count, btype, isRemoteVisual: true, forcedNetId: sid);
                    _run.Combat.MindustryTurrets.Add(remoteStruct);
                    _run.StageEntities.Add(remoteStruct);
                    new VisualFX(bPos + new Vector3(0, 1.0f, 0), Color.Gold, 3.0f, 0.3f);
                }
                else if (mtype == "mindustry_hit" && _screen == "in_game" && _run != null)
                {
                    int sid = msg["id"]?.GetValue<int>() ?? -1;
                    float dmg = msg["dmg"]?.GetValue<float>() ?? 10f;
                    var st = _run.Combat.MindustryTurrets.Find(t => IsValid(t) && t.StructNetId == sid);
                    if (st != null)
                        st.TakeStructureDamage(dmg, fromNetwork: true);
                }
                else if (mtype == "pvp_hit" && _screen == "in_game" && _run != null)
                {
                    string targetId = msg["target_id"]?.GetValue<string>() ?? "";
                    float dmg = msg["dmg"]?.GetValue<float>() ?? 10f;
                    if (targetId == _net.MyId && !_isSpectating)
                        _run.Combat.TakeDamage(dmg, null, "pvp");
                }
                else if (mtype == "pvp_eliminated" && _screen == "in_game" && _run != null)
                {
                    string loserId = msg["player_id"]?.GetValue<string>() ?? "враг";
                    if (_remotePlayers.TryGetValue(loserId, out var rp)) rp.Hp = 0f;
                    bool anyOtherAlive = false;
                    foreach (var kv in _remotePlayers)
                        if (kv.Key != loserId && IsValid(kv.Value) && kv.Value.Hp > 0f) { anyOtherAlive = true; break; }
                    if (!anyOtherAlive && !_isSpectating)
                    {
                        FinishRunWithSummary(victory: true);
                        ShowBanner($"ПОБЕДА НА АРЕНЕ PvP! СОПЕРНИК [{loserId.ToUpper()}] ПОВЕРЖЕН!", Color.Gold, 6.5f);
                    }
                }
                else if (mtype == "ally_heal" && _screen == "in_game" && _run != null)
                {
                    string tid = msg["target_id"]?.GetValue<string>() ?? "";
                    if (tid == _net.MyId || tid == "all")
                    {
                        float hAmt = msg["heal"]?.GetValue<float>() ?? 20f;
                        bool uber = msg["uber"]?.GetValue<bool>() ?? false;
                        _run.HealPlayer(hAmt);
                        if (uber)
                        {
                            _run.Combat.UberTimer = 6.5f;
                            _run.Combat.Shield = MathF.Min(_run.PlayerMaxHp, _run.Combat.Shield + 50f);
                            ShowBanner("МЕДИК АКТИВИРОВАЛ НА ВАС УБЕР-ЗАРЯД (6.5 СЕК НЕУЯЗВИМОСТИ)!", Rgb(255, 75, 90), 3.0f);
                        }
                        else
                        {
                            new VisualFX(_run.PlayerPos + new Vector3(0, 1.2f, 0), Color.Lime, 2.2f, 0.25f);
                        }
                    }
                }
                else if (mtype == "chest_open" && _screen == "in_game" && _run != null)
                {
                    int cid = msg["id"]?.GetValue<int>() ?? -1;
                    string itemK = msg["item"]?.GetValue<string>() ?? "syringe";
                    foreach (var ent in _run.StageEntities)
                    {
                        if (ent is Chest ch && IsValid(ch) && ch.ChestId == cid && !ch.Opened)
                        {
                            ch.OpenVisual(itemK);
                            break;
                        }
                    }
                }
                else if (mtype == "pillar_state" && _screen == "in_game" && _run != null)
                {
                    string ptype = msg["ptype"]?.GetValue<string>() ?? "";
                    string pstate = msg["state"]?.GetValue<string>() ?? "";
                    foreach (var ent in _run.StageEntities)
                    {
                        if (ent is MoonPillar mp && IsValid(mp) && mp.PillarType == ptype)
                        {
                            if (pstate == "charging") mp.ActivatePillar(fromNetwork: true);
                            else if (pstate == "done") mp.CompletePillar(fromNetwork: true);
                            break;
                        }
                    }
                }
                else if (mtype == "player_dead" && _screen == "in_game")
                {
                    string deadPid = msg["player_id"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(deadPid))
                    {
                        _deadCoopPlayers.Add(deadPid);
                        if (_remotePlayers.TryGetValue(deadPid, out var deadRp))
                            deadRp.Hp = 0f;
                    }
                    if (_isSpectating)
                    {
                        bool anyAlive = false;
                        foreach (var kv in _remotePlayers)
                        {
                            if (!_deadCoopPlayers.Contains(kv.Key) && IsValid(kv.Value) && kv.Value.Hp > 0f)
                            {
                                anyAlive = true;
                                break;
                            }
                        }
                        if (!anyAlive)
                        {
                            _isSpectating = false;
                            FinishRunWithSummary(victory: false);
                            ShowBanner("ВСЕ ИГРОКИ ПОГИБЛИ! ЗАБЕГ ОКОНЧЕН", Color.Red, 5.5f);
                        }
                    }
                }
                else if (mtype == "all_players_dead" && _screen == "in_game")
                {
                    _isSpectating = false;
                    FinishRunWithSummary(victory: false);
                    ShowBanner("ВСЕ ИГРОКИ ПОГИБЛИ! ЗАБЕГ ОКОНЧЕН", Color.Red, 5.5f);
                }
                else if (mtype == "item_drop" && _screen == "in_game" && _run != null)
                {
                    string ik = msg["item"]?.GetValue<string>() ?? "syringe";
                    if (msg["pos"] is System.Text.Json.Nodes.JsonArray parr && parr.Count >= 3)
                    {
                        Vector3 dPos = new(
                            parr[0]?.GetValue<float>() ?? 0f,
                            parr[1]?.GetValue<float>() ?? 0f,
                            parr[2]?.GetValue<float>() ?? 0f
                        );
                        _run.SpawnItemDropNet(dPos, ik);
                    }
                }
                else if (mtype == "run_victory" && _screen == "in_game")
                {
                    FinishRunWithSummary(victory: true);
                    ShowBanner("ПОБЕДА! КОМАНДА ЭВАКУИРОВАЛАСЬ С ЛУНЫ!", Color.Gold, 6.5f);
                }
                else if (mtype == "rps_duel" && _screen == "in_game" && _run != null)
                {
                    HandleNetworkRpsDuel(msg);
                }
            }
        }

        private static void BroadcastLobbyState()
        {
            if (!_net.Connected) return;
            string netChar = Data.ProgressData.Instance.IsCharacterUnlocked(_selectedChar) ? _selectedChar : "commando";
            _net.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "lobby_state",
                ["player_id"] = _net.MyId,
                ["char_key"] = netChar,
                ["ready"] = _myReady,
                ["skin_idx"] = Data.ProgressData.Instance.GetSelectedSkin(netChar),
                ["diff"] = GameData.SelectedDifficulty,
                ["arts"] = string.Join(",", GameData.ActiveArtifacts)
            });
        }

        private static void EvaluateLobbyCountdown()
        {
            if (!_net.Connected) return;
            int totalP = 1 + _lobbyPlayers.Count;
            int readyP = (_myReady ? 1 : 0);
            foreach (var v in _lobbyPlayers.Values) if (v.Ready) readyP++;
            if (readyP >= 1 && !_lobbyCountdownActive)
            {
                _lobbyCountdownActive = true;
                _lobbyCountdown = 60f;
            }
            if (readyP >= totalP && _lobbyCountdownActive)
                _lobbyCountdown = MathF.Min(_lobbyCountdown, 3.0f);
        }

        private static void SetupDiorama()
        {
            _mainPedestal = new Entity(position: new(1.35f, 0f, -1.0f));
            new Entity(parent: _mainPedestal, model: "cube", scale: new(3.4f, 0.35f, 3.4f), position: new(0, -0.18f, 0), color: Rgb(34, 42, 58));
            new Entity(parent: _mainPedestal, model: "cube", scale: new(2.8f, 0.14f, 2.8f), position: new(0, 0.04f, 0), color: Rgb(55, 70, 96));
            _pedestalRing = new Entity(parent: _mainPedestal, model: "cube", scale: new(2.9f, 0.05f, 2.9f), position: new(0, 0.10f, 0), color: Color.SkyBlue, unlit: true);

            _previewActor = new Entity(parent: _mainPedestal, position: new(0, 0.12f, 0), rotation: new(0, 15f, 0));
            _menuCrystal = new Entity(
                model: "diamond", scale: new(0.85f, 1.85f, 0.85f),
                position: new(1.35f, 3.3f, 2.2f), color: Rgb(60, 215, 255), unlit: true
            );

            SelectCharacter("commando");
        }

        private static void SelectCharacter(string charKey)
        {
            if (!SurvivorDatabase.Classes.ContainsKey(charKey)) return;
            _selectedChar = charKey;
            ClearChildren(_previewActor);
            if (IsValid(_activeSkillDemo)) { Destroy(_activeSkillDemo!); _activeSkillDemo = null; }
            _skillPreviewTimer = 0f;
            _previewEnemyParts = null;
            _previewBaseScale = 1.0f;
            _previewActor.Position = new Vector3(0, 0.12f, 0);
            _previewActor.Rotation = new Vector3(0, 15f, 0);

            var cfg = SurvivorDatabase.Classes[charKey];
            cfg.SyncLoadout();
            var activeSkin = cfg.GetActiveSkin();
            _pedestalRing.Color = activeSkin.PrimaryColor;
            _previewSurvivorParts = SurvivorDatabase.BuildSurvivorMesh(_previewActor, charKey);
            new VisualFX(_mainPedestal.Position + new Vector3(0, 1.2f, 0), cfg.BodyColor, maxScale: 3.0f, duration: 0.25f);
            if (_screen == "lobby") SoundManager.Play("ui_select_char");
            BroadcastLobbyState();
        }

        private static void SelectLogbookEntry(string category, string key)
        {
            _logCategory = category;
            _logSelectedKey = key;
            ClearChildren(_previewActor);
            _previewSurvivorParts = null;
            if (IsValid(_activeSkillDemo)) { Destroy(_activeSkillDemo!); _activeSkillDemo = null; }
            _skillPreviewTimer = 0f;
            _previewEnemyParts = null;
            _previewActor.Rotation = new Vector3(0, 20f, 0);

            if (category == "items" && GameData.ItemPool.TryGetValue(key, out var item))
            {
                bool unl = ProgressData.Instance.UnlockedItems.Contains(key);
                _previewBaseScale = 1.85f;
                _previewActor.Position = new Vector3(0, 1.35f, 0);
                _pedestalRing.Color = unl ? item.Color : Color.Gray;
                if (unl)
                    ItemBuilder.BuildItemMesh(_previewActor, key);
                else
                    new Entity(parent: _previewActor, model: "diamond", scale: new(0.65f, 1.05f, 0.65f), color: Rgb(65, 72, 88), unlit: true);
            }
            else if (category == "survivors" && SurvivorDatabase.Classes.TryGetValue(key, out var surv))
            {
                bool unl = ProgressData.Instance.IsCharacterUnlocked(key);
                _previewBaseScale = 1.0f;
                _previewActor.Position = new Vector3(0, 0.12f, 0);
                _pedestalRing.Color = unl ? surv.BodyColor : Color.Gray;
                if (unl)
                    _previewSurvivorParts = SurvivorDatabase.BuildSurvivorMesh(_previewActor, key);
                else
                    new Entity(parent: _previewActor, model: "diamond", scale: new(0.85f, 1.65f, 0.85f), position: new(0, 1.1f, 0), color: Rgb(65, 72, 88), unlit: true);
            }
            else if (category == "monsters")
            {
                _previewBaseScale = key is "parent" or "elder_lemurian" ? 0.58f : (key is "golem" or "greater_wisp" or "lunar_chimera" or "bison" or "brass_contraption" ? 0.76f : 0.95f);
                float yOff = key is "wisp" or "jellyfish" or "greater_wisp" or "lunar_chimera" or "brass_contraption" or "hell_drone" or "nether_blaze" ? -0.75f : 0.12f;
                _previewActor.Position = new Vector3(0, yOff, 0);
                _pedestalRing.Color = Rgb(255, 140, 50);
                _previewEnemyParts = EnemyAndBossBuilder.BuildEnemyMesh(_previewActor, key);
            }
            else if (category == "bosses")
            {
                _previewBaseScale = key == "magma_worm" ? 0.42f : 0.48f;
                float yOff = key is "vagrant" or "solus_control" ? -0.35f : (key == "magma_worm" ? 0.35f : 0.12f);
                _previewActor.Position = new Vector3(0, yOff, 0);
                _pedestalRing.Color = key == "mithrix" ? Color.SkyBlue : Color.Red;
                _previewEnemyParts = EnemyAndBossBuilder.BuildEnemyMesh(_previewActor, key);
                if (key == "mithrix" && _previewEnemyParts.PhaseRing != null)
                    _previewEnemyParts.PhaseRing.Enabled = true;
            }
            else if (category == "stages")
            {
                bool disc = ProgressData.Instance.DiscoveredStages.Contains(key) || (key == "safe_travels" && IsSafeTravelsUnlocked());
                var pal = GameData.StagePalettes.Find(p => p.Id == key)
                          ?? (GameData.SpecialPalettes.TryGetValue(key, out var sp) ? sp : GameData.StagePalettes[0]);
                _previewBaseScale = 0.24f;
                _previewActor.Position = new Vector3(0, 0.45f, 0);
                _pedestalRing.Color = disc ? pal.Accent : Color.Gray;
                if (!disc)
                {
                    new Entity(parent: _previewActor, model: "diamond", scale: new(3.2f, 4.8f, 3.2f), position: new(0, 2.5f, 0), color: Rgb(65, 72, 88), unlit: true);
                    return;
                }
                var root = new Entity(parent: _previewActor);
                new Entity(parent: root, model: "sphere", scale: new(9.5f, 1.2f, 9.5f), position: new(0, -0.4f, 0), color: pal.Ground);
                new Entity(parent: root, model: "diamond", scale: new(10.0f, 0.25f, 10.0f), color: pal.Accent, unlit: true);
                if (key == "safe_travels")
                {
                    new Entity(parent: root, model: "cube", scale: new(2.6f, 1.2f, 7.6f), position: new(0, 2.2f, 0), color: Rgb(185, 200, 222));
                    new Entity(parent: root, model: "cube", scale: new(4.8f, 0.4f, 2.8f), position: new(0, 2.1f, -1.2f), color: Rgb(95, 118, 152));
                    new Entity(parent: root, model: "cube", scale: new(1.6f, 0.7f, 2.0f), position: new(0, 3.05f, 2.1f), color: Rgb(80, 220, 255), unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        new Entity(parent: root, model: "sphere", scale: new(0.95f, 0.95f, 1.8f), position: new(s * 1.65f, 2.1f, -3.8f), color: Rgb(75, 225, 255), unlit: true);
                }
                else
                {
                    string stType = key switch
                    {
                        "ionia" or "hakurei_shrine" => "torii",
                        "sundered" => "giant_mushroom",
                        "rallypoint" => "ice_spire",
                        "sulfur" or "black_mesa" or "hell_cybergrind" => "sulfur_geyser",
                        "aqueduct" or "tartarus" => "aqueduct_pillar",
                        "aphelian" or "tracen_derby" => "sun_temple",
                        "wetland" => "swamp_root",
                        "meadow" => "astral_ring",
                        "roost" or "moon" or "qliphoth" => "arch",
                        "acres" or "bazaar" or "pizzeria" => "monolith",
                        _ => "crystals"
                    };
                    WorldStructuresBuilder.BuildMapStructure(root, stType, pal, Vector3.Zero, 25f);
                }
            }
            else if (category == "achievements" && GameData.Achievements.TryGetValue(key, out var ach))
            {
                bool unlocked = ProgressData.Instance.UnlockedAchievements.Contains(key);
                if (unlocked && !string.IsNullOrEmpty(ach.UnlocksCharacter) && SurvivorDatabase.Classes.TryGetValue(ach.UnlocksCharacter, out var achSurv))
                {
                    _previewBaseScale = 1.0f;
                    _previewActor.Position = new Vector3(0, 0.12f, 0);
                    _pedestalRing.Color = Color.Gold;
                    _previewSurvivorParts = SurvivorDatabase.BuildSurvivorMesh(_previewActor, ach.UnlocksCharacter);
                }
                else
                {
                    _previewBaseScale = 1.45f;
                    _previewActor.Position = new Vector3(0, 1.25f, 0);
                    _pedestalRing.Color = unlocked ? Color.Gold : Color.Gray;
                    if (unlocked && ach.UnlocksItem != null)
                        ItemBuilder.BuildItemMesh(_previewActor, ach.UnlocksItem);
                    else
                        new Entity(parent: _previewActor, model: "diamond", scale: new(0.75f, 1.15f, 0.75f), color: unlocked ? Color.Gold : Rgb(65, 72, 88), unlit: true);
                }
            }
            else if (category == "modifiers")
            {
                _previewBaseScale = 1.45f;
                _previewActor.Position = new Vector3(0, 1.25f, 0);
                Color modCol = GameData.RunArtifacts.TryGetValue(key, out var rArt)
                    ? rArt.Color
                    : (GameData.Difficulties.TryGetValue(key, out var dDef) ? dDef.Color : Color.Gold);
                _pedestalRing.Color = modCol;
                var relic = new Entity(parent: _previewActor);
                new Entity(parent: relic, model: "diamond", scale: new(0.82f, 1.28f, 0.82f), color: modCol, unlit: true);
                new Entity(parent: relic, model: "cube", scale: new(1.12f, 0.08f, 1.12f), rotation: new(0, 45f, 18f), color: Color.White, unlit: true);
                new Entity(parent: relic, model: "cube", scale: new(1.12f, 0.08f, 1.12f), rotation: new(18f, 45f, 0), color: modCol, unlit: true);
                new Entity(parent: relic, model: "sphere", scale: new(0.42f), color: Color.White, unlit: true);
            }
        }

        private static void TriggerLobbySkillPreview(string charKey, string slot, int variantIdx)
        {
            if (!SurvivorDatabase.Classes.TryGetValue(charKey, out var cfg)) return;
            cfg.SyncLoadout();
            SoundManager.Play("ui_ready");

            float animDur = slot switch
            {
                "r" => 0.95f,
                "shift" => 0.62f,
                "m2" => 0.52f,
                _ => 0.42f
            };
            _previewSurvivorParts?.TriggerSkillAnim(slot, animDur);
            _previewActor.Rotation = new Vector3(0f, 18f, 0f);
            _manualTimer = 1.85f;

            Color slotCol = slot switch
            {
                "m1" => Color.Yellow,
                "m2" => Color.Orange,
                "shift" => Color.SkyBlue,
                "r" => Color.Magenta,
                _ => Color.SkyBlue
            };
            string bindLabel = slot switch
            {
                "m1" => "ЛКМ",
                "m2" => "ПКМ",
                "shift" => "SHIFT",
                "r" => "R / УЛЬТА",
                _ => slot.ToUpper()
            };

            string skName = cfg.Skills.TryGetValue(slot, out var sk) ? sk.Name : slot.ToUpper();
            var activeVars = cfg.GetActiveSkin().CustomSkillVariants ?? cfg.SkillVariants;
            if (activeVars.TryGetValue(slot, out var vars) && variantIdx >= 0 && variantIdx < vars.Count)
                skName = vars[variantIdx].Name;

            _skillPreviewTitle = $"▶ АНИМАЦИЯ НАВЫКА [{bindLabel}]: {skName} {(variantIdx == 0 ? "[СТАНД.]" : "[АЛЬТ.]")}";
            _skillPreviewColor = slotCol;
            _skillPreviewTimer = 2.4f;

            if (IsValid(_activeSkillDemo))
                Destroy(_activeSkillDemo!);
            _activeSkillDemo = new LobbySkillDemoFX(_mainPedestal, slot, variantIdx, slotCol, cfg.GetActiveSkin().AccentColor);
        }

        private static void ShowKillerOnDiorama()
        {
            if (IsValid(_activeSkillDemo))
            {
                Destroy(_activeSkillDemo!);
                _activeSkillDemo = null;
            }
            ClearChildren(_previewActor);
            _previewSurvivorParts = null;
            if (IsValid(_activeSkillDemo)) { Destroy(_activeSkillDemo!); _activeSkillDemo = null; }
            _skillPreviewTimer = 0f;
            _previewEnemyParts = null;
            _previewActor.Rotation = new Vector3(0f, 20f, 0f);

            string key = string.IsNullOrEmpty(_lastKillerKey) ? "lemurian" : _lastKillerKey;
            _pedestalRing.Color = _lastKillerColor;

            if (key is "pvp_rival" or "chaos_self")
            {
                _previewBaseScale = 1.0f;
                _previewActor.Position = new Vector3(0f, 0.12f, 0f);
                _previewSurvivorParts = SurvivorDatabase.BuildSurvivorMesh(_previewActor, _selectedChar);
                new Entity(parent: _previewActor, model: "cube", scale: new(1.45f, 0.05f, 1.45f), position: new(0, 0.1f, 0), color: _lastKillerColor, unlit: true);
                return;
            }

            bool isBossModel = GameData.IsKnownBossType(key);
            if (isBossModel)
            {
                _previewBaseScale = key == "magma_worm" ? 0.42f : 0.48f;
                float yOff = key is "vagrant" or "solus_control" ? -0.35f : (key == "magma_worm" ? 0.35f : 0.12f);
                _previewActor.Position = new Vector3(0f, yOff, 0f);
            }
            else
            {
                _previewBaseScale = key is "parent" or "elder_lemurian" ? 0.58f : (key is "golem" or "greater_wisp" or "lunar_chimera" or "bison" or "brass_contraption" ? 0.76f : 0.95f);
                float yOff = key is "wisp" or "jellyfish" or "greater_wisp" or "lunar_chimera" or "brass_contraption" or "hell_drone" or "nether_blaze" ? -0.75f : 0.12f;
                _previewActor.Position = new Vector3(0f, yOff, 0f);
            }

            _previewEnemyParts = EnemyAndBossBuilder.BuildEnemyMesh(_previewActor, key);
            if (key == "mithrix" && _previewEnemyParts.PhaseRing != null)
                _previewEnemyParts.PhaseRing.Enabled = true;
            if (_lastKillerIsElite && _previewEnemyParts.Crown != null)
                _previewEnemyParts.Crown.Enabled = true;
            if (_lastKillerIsDoppel)
                new Entity(parent: _previewActor, model: "diamond", scale: new(1.35f, 0.12f, 1.35f), position: new(0, 0.25f, 0), color: Rgb(205, 65, 255), unlit: true);
        }

        private static void DrawKillerMobTopRightWidget(int x, int y, int w, bool allow3DToggle)
        {
            int h = allow3DToggle ? 176 : 138;
            Raylib.DrawRectangle(x, y, w, h, Rgb(14, 16, 28, 244));
            Raylib.DrawRectangle(x, y, w, 4, _lastKillerColor);
            Raylib.DrawRectangleLines(x, y, w, h, Rgb(_lastKillerColor.R, _lastKillerColor.G, _lastKillerColor.B, 165));

            // Верхняя плашка "КТО ВАС УБИЛ"
            Raylib.DrawRectangle(x + 12, y + 10, w - 24, 24, Rgb(42, 18, 26, 230));
            Engine.DrawTextLeft("☠ КТО ВАС УБИЛ (УБИЙЦА):", x + 20, y + 14, 14f, Rgb(255, 120, 120));
            Engine.DrawTextLeft($"УДАР: -{(int)MathF.Ceiling(_lastKillerDamage)} HP", x + w - 150, y + 14, 13f, Color.Gold);

            // Мини-эмблема моба слева
            int iconX = x + 14, iconY = y + 42, iconSz = 58;
            Raylib.DrawRectangle(iconX, iconY, iconSz, iconSz, Rgb(24, 28, 44, 250));
            Raylib.DrawRectangleLines(iconX, iconY, iconSz, iconSz, _lastKillerColor);
            Raylib.DrawCircle(iconX + iconSz / 2, iconY + iconSz / 2 - 4, 15f, Rgb(_lastKillerColor.R, _lastKillerColor.G, _lastKillerColor.B, 85));
            Raylib.DrawCircle(iconX + iconSz / 2 - 5, iconY + iconSz / 2 - 6, 3.2f, Color.White);
            Raylib.DrawCircle(iconX + iconSz / 2 + 5, iconY + iconSz / 2 - 6, 3.2f, Color.White);
            string badgeShort = _lastKillerIsBoss ? "БОСС" : (_lastKillerIsDoppel ? "ТЕНЬ" : (_lastKillerIsElite ? "ЭЛИТА" : "МОБ"));
            Engine.DrawTextCentered(badgeShort, iconX + iconSz / 2, iconY + iconSz - 11, 11f, _lastKillerColor);

            // Имя, ранг и краткое описание моба
            Engine.DrawTextLeft(_lastKillerName, iconX + iconSz + 12, y + 40, 20f, Color.White);
            Engine.DrawTextLeft(_lastKillerRank, iconX + iconSz + 12, y + 64, 13f, _lastKillerColor);

            string shortDesc = _lastKillerDesc.Length > 58 ? _lastKillerDesc[..55] + "..." : _lastKillerDesc;
            Engine.DrawTextLeft(shortDesc, iconX + iconSz + 12, y + 84, 13f, Rgb(195, 210, 232));

            if (allow3DToggle)
            {
                string toggleTxt = _summaryShowKiller3D
                    ? "НА 3D-ПОДИУМЕ: МОБ-УБИЙЦА (НАЖМИТЕ ДЛЯ ГЕРОЯ)"
                    : "НА 3D-ПОДИУМЕ: ВАШ ГЕРОЙ (НАЖМИТЕ ДЛЯ МОБА-УБИЙЦЫ)";
                if (Engine.DrawButton(new Rectangle(x + 14, y + 132, w - 28, 34), toggleTxt,
                    _summaryShowKiller3D ? Rgb(95, 34, 44) : Rgb(32, 68, 112),
                    Rgb(135, 52, 66), Color.White, 13f, _lastKillerColor))
                {
                    _summaryShowKiller3D = !_summaryShowKiller3D;
                    if (_summaryShowKiller3D) ShowKillerOnDiorama();
                    else SelectCharacter(_selectedChar);
                }
            }
        }

        private class LobbySkillDemoFX : Entity
        {
            private readonly string _slot;
            private readonly int _variant;
            private readonly Color _col;
            private readonly Color _accent;
            private readonly Entity _dummy;
            private readonly Entity _dummyBody;
            private readonly Entity _fxGroup;
            private readonly List<Entity> _projectiles = new();
            private readonly Entity _auraRing;
            private float _timer = 0f;
            private const float Duration = 1.45f;

            public LobbySkillDemoFX(Entity pedestal, string slot, int variant, Color slotCol, Color accentCol)
                : base(parent: pedestal, position: Vector3.Zero)
            {
                _slot = slot;
                _variant = variant;
                _col = variant == 0 ? slotCol : accentCol;
                _accent = accentCol;

                // Голографический манекен-мишень перед персонажем для наглядной демонстрации удара/выстрела
                _dummy = new Entity(parent: this, position: new(0.28f, 0.14f, 1.25f));
                new Entity(parent: _dummy, model: "cube", scale: new(0.68f, 0.04f, 0.68f), color: Rgb(255, 85, 85), unlit: true);
                _dummyBody = new Entity(parent: _dummy, model: "cube", scale: new(0.38f, 0.92f, 0.38f), position: new(0, 0.52f, 0), color: Rgb(220, 65, 75), unlit: true);
                new Entity(parent: _dummyBody, model: "sphere", scale: new(0.75f, 0.36f, 0.75f), position: new(0, 0.72f, 0), color: Color.White, unlit: true);

                _auraRing = new Entity(parent: this, model: "cube", scale: new(1.25f, 0.04f, 1.25f), position: new(0, 0.16f, 0), color: _col, unlit: true);
                _fxGroup = new Entity(parent: this, position: new(0f, 1.05f, 0.15f));

                if (_slot == "m1")
                {
                    int cnt = _variant == 0 ? 3 : 1;
                    for (int i = 0; i < cnt; i++)
                    {
                        var p = new Entity(parent: _fxGroup,
                            model: _variant == 0 ? "sphere" : "diamond",
                            scale: _variant == 0 ? new(0.22f, 0.22f, 0.42f) : new(0.55f, 0.18f, 0.65f),
                            position: new((i - (cnt - 1) * 0.5f) * 0.18f, 0, 0),
                            color: _col, unlit: true);
                        _projectiles.Add(p);
                    }
                }
                else if (_slot == "m2")
                {
                    if (_variant == 0)
                    {
                        var orb = new Entity(parent: _fxGroup, model: "diamond", scale: new(0.45f, 0.45f, 0.65f), color: _col, unlit: true);
                        new Entity(parent: orb, model: "sphere", scale: new(0.65f), color: Color.White, unlit: true);
                        _projectiles.Add(orb);
                    }
                    else
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            var beam = new Entity(parent: _fxGroup, model: "cube", scale: new(0.16f, 0.16f, 0.55f),
                                position: new(i * 0.28f, MathF.Abs(i) * 0.08f, 0), color: _col, unlit: true);
                            _projectiles.Add(beam);
                        }
                    }
                }
                else if (_slot == "shift")
                {
                    // Голографический след рывка / прыжка
                    for (int i = 0; i < 3; i++)
                    {
                        var clone = new Entity(parent: _fxGroup,
                            model: _variant == 0 ? "cube" : "diamond",
                            scale: new(0.42f, 0.85f - i * 0.15f, 0.22f),
                            position: new(0, -0.25f, -0.35f * i),
                            color: _col, unlit: true);
                        _projectiles.Add(clone);
                    }
                }
                else // "r" (Ульта)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = i * MathF.PI * 0.5f;
                        var sat = new Entity(parent: _fxGroup,
                            model: _variant == 0 ? "diamond" : "sphere",
                            scale: _variant == 0 ? new(0.32f, 0.75f, 0.32f) : new(0.38f, 0.38f, 0.38f),
                            position: new(MathF.Cos(ang) * 0.85f, 0.15f, MathF.Sin(ang) * 0.85f),
                            color: i % 2 == 0 ? _col : Color.Gold, unlit: true);
                        _projectiles.Add(sat);
                    }
                }
            }

            public override void OnUpdate(float dt)
            {
                _timer += dt;
                float p = Math.Clamp(_timer / Duration, 0f, 1f);
                Vector3 targetLocal = new(0.28f, 0f, 1.10f);

                _auraRing.RotationY += 180f * dt;
                float ringPulse = 1.15f + MathF.Sin(p * MathF.PI) * (_slot == "r" ? 1.1f : 0.55f);
                _auraRing.Scale = new Vector3(ringPulse, 0.04f, ringPulse);

                if (_slot is "m1" or "m2")
                {
                    float flight = Math.Clamp((p * 1.8f) % 1.0f, 0f, 1f);
                    for (int i = 0; i < _projectiles.Count; i++)
                    {
                        var pr = _projectiles[i];
                        float offsetP = Math.Clamp(flight - i * 0.12f, 0f, 1f);
                        float spreadX = (_projectiles.Count > 1 ? (i - (_projectiles.Count - 1) * 0.5f) * 0.24f * (1f - offsetP * 0.6f) : 0f);
                        pr.Position = Vector3.Lerp(new Vector3(spreadX, 0f, 0f), targetLocal + new Vector3(spreadX * 0.3f, 0f, 0f), offsetP);
                        pr.RotationZ += 260f * dt;
                    }
                    bool hitNow = flight > 0.72f;
                    _dummyBody.Color = hitNow ? Color.Yellow : Rgb(220, 65, 75);
                    _dummyBody.Scale = hitNow ? new Vector3(0.46f, 0.84f, 0.46f) : new Vector3(0.38f, 0.92f, 0.38f);
                }
                else if (_slot == "shift")
                {
                    float dashWave = MathF.Sin(p * MathF.PI);
                    for (int i = 0; i < _projectiles.Count; i++)
                    {
                        var cl = _projectiles[i];
                        float step = Math.Clamp(dashWave * 1.2f - i * 0.18f, 0f, 1f);
                        float arcY = _variant == 1 ? MathF.Sin(step * MathF.PI) * 0.65f : 0f;
                        cl.Position = Vector3.Lerp(new Vector3(0f, -0.25f, -0.2f), targetLocal + new Vector3(0f, -0.25f + arcY, 0f), step);
                        cl.RotationY += 180f * dt;
                    }
                    _dummyBody.Color = dashWave > 0.65f ? Color.SkyBlue : Rgb(220, 65, 75);
                }
                else // "r"
                {
                    for (int i = 0; i < _projectiles.Count; i++)
                    {
                        var sat = _projectiles[i];
                        float ang = _timer * 5.5f + i * MathF.PI * 0.5f;
                        if (p < 0.55f)
                        {
                            float r = 0.85f + MathF.Sin(p * MathF.PI * 2f) * 0.25f;
                            sat.Position = new Vector3(MathF.Cos(ang) * r, 0.2f + MathF.Sin(ang * 2f) * 0.18f, MathF.Sin(ang) * r);
                        }
                        else
                        {
                            float strikeT = (p - 0.55f) / 0.45f;
                            Vector3 orbitPos = new(MathF.Cos(ang) * 0.85f, 0.25f, MathF.Sin(ang) * 0.85f);
                            sat.Position = Vector3.Lerp(orbitPos, targetLocal, strikeT);
                        }
                        sat.RotationY += 240f * dt;
                    }
                    _dummyBody.Color = p > 0.62f ? Color.Magenta : Rgb(220, 65, 75);
                    _dummyBody.Scale = p > 0.62f ? new Vector3(0.50f, 0.78f, 0.50f) : new Vector3(0.38f, 0.92f, 0.38f);
                }

                if (_timer >= Duration)
                    Destroy(this);
            }
        }

        private static void ConfirmLoginProfile()
        {
            string finalName = string.IsNullOrWhiteSpace(_playerNameInput) ? "Survivor" : _playerNameInput.Trim();
            ProgressData.Instance.SetPlayerName(finalName);
            _playerNameInput = ProgressData.Instance.PlayerName;
            _screen = "main";
            if (!ProgressData.Instance.IsSanabiAuthorized() && _selectedChar == "sanabi")
                SelectCharacter("commando");
            SoundManager.Play("ui_ready", 0.8f);
            ShowBanner($"ПРОФИЛЬ ИГРОКА: {finalName}", Color.SkyBlue, 2.2f);
        }

        private static void HandleInput(float dt)
        {
            if (_screen == "login")
            {
                int ch = Raylib.GetCharPressed();
                while (ch > 0)
                {
                    char c = (char)ch;
                    if ((char.IsLetterOrDigit(c) || c is '_' or '-' or '.') && _playerNameInput.Length < 22)
                        _playerNameInput += c;
                    ch = Raylib.GetCharPressed();
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _playerNameInput.Length > 0)
                    _playerNameInput = _playerNameInput[..^1];
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    ConfirmLoginProfile();
                return;
            }

            if (_screen == "mp_connect" && _typingCode)
            {
                int ch = Raylib.GetCharPressed();
                while (ch > 0)
                {
                    char c = char.ToUpperInvariant((char)ch);
                    if ((char.IsLetterOrDigit(c) || c is '-' or '.' or ':') && _roomCodeInput.Length < 21)
                        _roomCodeInput += c;
                    ch = Raylib.GetCharPressed();
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _roomCodeInput.Length > 0)
                    _roomCodeInput = _roomCodeInput[..^1];
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    _typingCode = false;
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    _typingCode = false;
                return;
            }

            if (_screen == "logbook" && _logDetailOpen)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    CloseLogbookDetailPage();
                return;
            }

            if (_screen == "logbook" && !_logDetailOpen && Raylib.IsKeyPressed(KeyboardKey.Enter) && !string.IsNullOrEmpty(_logSelectedKey))
            {
                OpenLogbookDetailPage(_logCategory, _logSelectedKey, 0);
                return;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Escape) && _screen != "main")
            {
                if (_screen == "lobby") _net.StopSession();
                _screen = "main";
                SelectCharacter(_selectedChar);
                return;
            }

            float wheel = Raylib.GetMouseWheelMove();
            if (MathF.Abs(wheel) > 0.01f)
            {
                _previewZoom = Math.Clamp(_previewZoom + wheel * 0.12f, 0.55f, 1.75f);
                _manualTimer = 4.0f;
            }

            bool dragging = Raylib.IsMouseButtonDown(MouseButton.Right) ||
                            (Raylib.IsMouseButtonDown(MouseButton.Left) &&
                             (_screen == "lobby"
                                 ? (Raylib.GetMouseX() > 420 && Raylib.GetMouseX() < 910 && Raylib.GetMouseY() > 165 && Raylib.GetMouseY() < 620)
                                 : Raylib.GetMouseX() > 760));
            if (dragging)
            {
                Vector2 delta = Raylib.GetMouseDelta();
                if (delta.LengthSquared() > 0.1f)
                {
                    _previewActor.RotationY -= delta.X * 0.45f;
                    _previewActor.RotationX = Math.Clamp(_previewActor.RotationX + delta.Y * 0.35f, -65f, 65f);
                    _manualTimer = 4.5f;
                }
            }
        }

        private static void UpdateDiorama(float dt)
        {
            float t = (float)Raylib.GetTime();
            float targetPedestalX = _screen == "lobby" ? -0.95f : 1.35f;
            _mainPedestal.X = Lerp(_mainPedestal.X, targetPedestalX, MathF.Min(1f, dt * 12f));
            _menuCrystal.X = _mainPedestal.X;
            _menuCrystal.RotationY += 55f * dt;
            _menuCrystal.Y = 3.3f + MathF.Sin(t * 2.5f) * 0.18f;
            _previewActor.Scale = new Vector3(_previewBaseScale * _previewZoom);

            if (_skillPreviewTimer > 0f) _skillPreviewTimer = MathF.Max(0f, _skillPreviewTimer - dt);
            if (_manualTimer > 0f) _manualTimer -= dt;
            else
            {
                _previewActor.RotationY += 22f * dt;
                _previewActor.RotationX = Lerp(_previewActor.RotationX, 0f, dt * 5f);
            }

            if (_previewSurvivorParts != null)
            {
                string previewKey = _screen == "logbook" ? _logSelectedKey : _selectedChar;
                _previewSurvivorParts.UpdateAnimations(previewKey, dt, isMoving: false, isGrounded: true, velY: 0f, moveSpeedRatio: 1f, strafeInput: 0f, forwardInput: 0f);
            }
            if (_previewEnemyParts != null)
            {
                if (_previewEnemyParts.Crown != null) _previewEnemyParts.Crown.RotationY += 85f * dt;
                if (_previewEnemyParts.Ring1 != null) _previewEnemyParts.Ring1.RotationY += 110f * dt;
                if (_previewEnemyParts.Ring2 != null) _previewEnemyParts.Ring2.RotationX += 125f * dt;
                if (_previewEnemyParts.Tentacles != null) _previewEnemyParts.Tentacles.RotationY += 70f * dt;
            }
        }

        private static void DrawHeader()
        {
            int sw = Raylib.GetScreenWidth();
            Raylib.DrawRectangle(0, 0, sw, 54, Rgb(12, 16, 26, 235));
            Raylib.DrawRectangle(0, 54, sw, 3, Rgb(255, 165, 50));
            Engine.DrawTextCentered("RISK OF C# 2 : КООПЕРАТИВ ПО СЕТИ И КОДУ КОМНАТЫ", sw * 0.5f, 27f, 23f, Rgb(255, 185, 60));
        }

        private static void DrawLoginUI()
        {
            DrawHeader();
            Raylib.DrawRectangle(60, 165, 580, 320, Rgb(14, 18, 30, 244));
            Raylib.DrawRectangle(60, 165, 580, 4, Color.SkyBlue);
            Engine.DrawTextCentered("ПРОФИЛЬ ВЫЖИВШЕГО", 350, 204, 24f, Color.White);
            Engine.DrawTextCentered("Введите ваше имя пользователя:", 350, 240, 16f, Rgb(190, 215, 245));

            bool blink = ((int)(Raylib.GetTime() * 2.5f) % 2) == 0;
            string shownName = string.IsNullOrEmpty(_playerNameInput)
                ? (blink ? "_" : "Введите никнейм...")
                : (_playerNameInput + (blink ? "_" : ""));

            Raylib.DrawRectangle(100, 274, 500, 58, Rgb(22, 30, 48, 250));
            Raylib.DrawRectangleLines(100, 274, 500, 58, Color.SkyBlue);
            Engine.DrawTextCentered(shownName, 350, 303, 22f, Color.Yellow);

            if (Engine.DrawButton(new Rectangle(100, 356, 500, 58), "ВОЙТИ В ИГРУ [ENTER]",
                Rgb(38, 115, 72), Rgb(55, 160, 98), Color.White, 19f, Color.Lime))
            {
                ConfirmLoginProfile();
            }

            if (Engine.DrawButton(new Rectangle(100, 426, 500, 38), "ОЧИСТИТЬ ПОЛЕ ВВОДА [BACKSPACE]", Rgb(55, 36, 44), Rgb(85, 52, 64), Rgb(220, 180, 180), 14f))
                _playerNameInput = "";
        }

        private static void DrawMainMenuUI()
        {
            DrawHeader();
            Raylib.DrawRectangle(60, 118, 480, 628, Rgb(14, 18, 30, 238));
            Raylib.DrawRectangle(60, 118, 480, 4, Color.SkyBlue);
            Engine.DrawTextCentered("ГЛАВНОЕ МЕНЮ", 300, 148, 24f, Color.White);

            string curUser = string.IsNullOrWhiteSpace(ProgressData.Instance.PlayerName) ? "Survivor" : ProgressData.Instance.PlayerName;
            if (Engine.DrawButton(new Rectangle(100, 174, 400, 36),
                $"ПРОФИЛЬ: {curUser}  [СМЕНИТЬ НИК]",
                Rgb(26, 36, 56), Rgb(48, 68, 104),
                Color.SkyBlue, 14f, Color.SkyBlue))
            {
                _playerNameInput = ProgressData.Instance.PlayerName;
                _screen = "login";
            }

            if (Engine.DrawButton(new Rectangle(100, 222, 400, 54), "ОДИНОЧНАЯ ИГРА", Rgb(38, 105, 70), Rgb(55, 150, 95), Color.White, 19f))
            {
                _net.StopSession();
                _lobbyPlayers.Clear();
                _myReady = false;
                _lobbyCountdownActive = false;
                _screen = "lobby";
                SelectCharacter(_selectedChar);
            }

            bool shipUnl = IsSafeTravelsUnlocked();
            string shipLabel = shipUnl
                ? "★ КОРАБЛЬ UES «SAFE TRAVELS»"
                : "[ЗАКРЫТО] КОРАБЛЬ «SAFE TRAVELS»";
            if (Engine.DrawButton(new Rectangle(100, 286, 400, 58), shipLabel,
                shipUnl ? Rgb(28, 78, 128) : Rgb(36, 34, 46),
                shipUnl ? Rgb(48, 116, 186) : Rgb(56, 50, 68),
                shipUnl ? Color.Yellow : Rgb(165, 175, 195), 17f,
                shipUnl ? Color.SkyBlue : Color.Gray))
            {
                if (shipUnl)
                {
                    EnterSafeTravels(_selectedChar);
                }
                else
                {
                    SoundManager.Play("ui_error");
                    ShowBanner("ПОБЕДИТЕ МИТРИКСА НА ЛУНЕ ХОТЯ БЫ 1 РАЗ, ЧТОБЫ ПОДНЯТЬСЯ НА БОРТ UES «SAFE TRAVELS»!", Color.Orange, 4.0f);
                }
            }
            Engine.DrawTextCentered(
                shipUnl ? "Хаб выживших: каюты, полигон урона и диалоги всего экипажа" : "Условие: одолейте Митрикса хотя бы один раз",
                300f, 354f, 12f, shipUnl ? Rgb(120, 225, 255) : Rgb(155, 165, 185));

            if (Engine.DrawButton(new Rectangle(100, 368, 400, 54), "СЕТЕВАЯ ИГРА (КООПЕРАТИВ)", Rgb(40, 85, 145), Rgb(60, 125, 205), Color.White, 19f))
            {
                _typingCode = false;
                _netStatusText = $"Ваш локальный IP: {_net.HostIp}";
                _screen = "mp_connect";
            }
            if (Engine.DrawButton(new Rectangle(100, 434, 400, 54), "ЖУРНАЛ ВЫЖИВШЕГО", Rgb(125, 75, 165), Rgb(165, 105, 215), Color.White, 19f))
            {
                _logDetailOpen = false;
                _screen = "logbook";
                var list = GetLogEntries(_logCategory);
                if (list.Count > 0) SelectLogbookEntry(_logCategory, list[0].Key);
            }
            if (Engine.DrawButton(new Rectangle(100, 500, 400, 54), "НАСТРОЙКИ", Rgb(65, 72, 95), Rgb(95, 105, 135), Color.White, 19f))
            {
                _screen = "settings";
            }
            if (Engine.DrawButton(new Rectangle(100, 566, 400, 54), "ВЫХОД ИЗ ИГРЫ", Rgb(135, 40, 45), Rgb(185, 60, 65), Color.White, 19f))
            {
                _net.StopSession();
                SoundManager.Shutdown();
                Raylib.CloseWindow();
                Environment.Exit(0);
            }
        }

        private static void DrawMpConnectUI()
        {
            DrawHeader();
            Raylib.DrawRectangle(60, 130, 580, 540, Rgb(14, 18, 30, 240));
            Raylib.DrawRectangle(60, 130, 580, 4, Color.SkyBlue);
            Engine.DrawTextCentered("СЕТЕВАЯ ИГРА :: ВЫБЕРИТЕ РЕЖИМ (ЛОББИ РАЗДЕЛЕНЫ)", 350, 156, 19f, Rgb(150, 225, 255));

            bool isCoop = _net.GameType == "coop";
            if (Engine.DrawButton(new Rectangle(100, 178, 244, 34), "КООПЕРАТИВ (PvE)",
                isCoop ? Rgb(38, 115, 75) : Rgb(26, 32, 48), Rgb(50, 145, 95), Color.White, 15f, isCoop ? Color.Lime : null))
            {
                _net.GameType = "coop";
                _net.DiscoveredRooms.Clear();
            }
            if (Engine.DrawButton(new Rectangle(356, 178, 244, 34), "АРЕНА PvP (ДУЭЛЬ)",
                !isCoop ? Rgb(145, 42, 55) : Rgb(26, 32, 48), Rgb(185, 60, 75), Color.White, 15f, !isCoop ? Color.Red : null))
            {
                _net.GameType = "pvp";
                _net.DiscoveredRooms.Clear();
            }

            if (Engine.DrawButton(new Rectangle(100, 222, 500, 54), $"СОЗДАТЬ КОМНАТУ [{_net.GameType.ToUpper()}] (ХОСТ)", Rgb(35, 100, 68), Rgb(50, 145, 95), Color.White, 18f))
            {
                _typingCode = false;
                _lobbyPlayers.Clear();
                var (ok, info) = _net.HostRoom();
                if (ok)
                {
                    _myReady = false;
                    _lobbyCountdownActive = false;
                    _screen = "lobby";
                SelectCharacter(_selectedChar);
                    ShowBanner($"КОМНАТА СОЗДАНА! КОД: {info}", Color.Lime, 4.0f);
                }
                else _netStatusText = $"Ошибка: {info}";
            }

            string codeShown = string.IsNullOrEmpty(_roomCodeInput)
                ? (_typingCode ? "ВВЕДИТЕ КОД..." : "НАЖМИТЕ ДЛЯ ВВОДА")
                : _roomCodeInput;
            string boxTitle = _typingCode ? $"КОД: > {codeShown}_ <" : $"КОД: [ {codeShown} ]";
            if (Engine.DrawButton(new Rectangle(100, 292, 500, 54), boxTitle,
                _typingCode ? Rgb(55, 75, 115) : Rgb(28, 36, 54), Rgb(45, 60, 88), Color.Yellow, 18f, Color.SkyBlue))
            {
                _typingCode = !_typingCode;
            }

            if (Engine.DrawButton(new Rectangle(100, 364, 242, 54), "ПО КОДУ", Rgb(40, 85, 145), Rgb(60, 120, 200), Color.White, 18f))
            {
                _typingCode = false;
                var (ok, info) = _net.JoinByCode(_roomCodeInput);
                if (ok) { _myReady = false; _screen = "lobby"; }
                else _netStatusText = info;
            }
            if (Engine.DrawButton(new Rectangle(358, 364, 242, 54), "НАЙТИ В LAN (АВТО)", Rgb(95, 55, 145), Rgb(130, 80, 195), Color.White, 18f))
            {
                _typingCode = false;
                var (ok, info) = _net.JoinLanAuto();
                if (ok) { _roomCodeInput = info; _myReady = false; _screen = "lobby"; }
                else _netStatusText = $"Не найдено ({info})";
            }

            Engine.DrawTextCentered(_netStatusText, 350, 465, 17f, Color.SkyBlue);

            if (Engine.DrawButton(new Rectangle(160, 580, 380, 50), "<- НАЗАД В ГЛАВНОЕ МЕНЮ", Rgb(60, 65, 82), Rgb(85, 92, 115), Color.White, 17f))
            {
                _typingCode = false;
                _screen = "main";
            }
        }

        private static void DrawSettingsUI()
        {
            DrawHeader();
            Raylib.DrawRectangle(60, 110, 580, 620, Rgb(14, 18, 30, 240));
            Raylib.DrawRectangle(60, 110, 580, 4, Color.Gold);
            Engine.DrawTextCentered("НАСТРОЙКИ ИГРЫ И ЗВУКА", 350, 142, 22f, Color.Gold);

            Engine.DrawTextLeft($"Чувствительность мыши: {(int)(_mouseSens * 250f)}", 95, 192, 18f, Color.White);
            if (Engine.DrawButton(new Rectangle(460, 184, 52, 38), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                _mouseSens = Math.Clamp(_mouseSens - 0.02f, 0.04f, 0.50f);
            if (Engine.DrawButton(new Rectangle(524, 184, 52, 38), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                _mouseSens = Math.Clamp(_mouseSens + 0.02f, 0.04f, 0.50f);

            Engine.DrawTextLeft($"Угол обзора камеры (FOV): {(int)_fov}", 95, 244, 18f, Color.White);
            if (Engine.DrawButton(new Rectangle(460, 236, 52, 38), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                _fov = Math.Clamp(_fov - 5f, 45f, 95f);
            if (Engine.DrawButton(new Rectangle(524, 236, 52, 38), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                _fov = Math.Clamp(_fov + 5f, 45f, 95f);

            Engine.DrawTextLeft($"Общая громкость: {(int)MathF.Round(SoundManager.MasterVolume * 100f)}%", 95, 296, 18f, Color.SkyBlue);
            if (Engine.DrawButton(new Rectangle(460, 288, 52, 38), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                SoundManager.SetMasterVolume(SoundManager.MasterVolume - 0.10f);
            if (Engine.DrawButton(new Rectangle(524, 288, 52, 38), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                SoundManager.SetMasterVolume(SoundManager.MasterVolume + 0.10f);

            Engine.DrawTextLeft($"Громкость эффектов (SFX): {(int)MathF.Round(SoundManager.SfxVolume * 100f)}%", 95, 348, 18f, Color.SkyBlue);
            if (Engine.DrawButton(new Rectangle(460, 340, 52, 38), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                SoundManager.SetSfxVolume(SoundManager.SfxVolume - 0.10f);
            if (Engine.DrawButton(new Rectangle(524, 340, 52, 38), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                SoundManager.SetSfxVolume(SoundManager.SfxVolume + 0.10f);

            Engine.DrawTextLeft($"Фоновый синтезатор: {(int)MathF.Round(SoundManager.AmbientVolume * 100f)}%", 95, 400, 18f, Color.SkyBlue);
            if (Engine.DrawButton(new Rectangle(460, 392, 52, 38), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                SoundManager.SetAmbientVolume(SoundManager.AmbientVolume - 0.05f);
            if (Engine.DrawButton(new Rectangle(524, 392, 52, 38), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                SoundManager.SetAmbientVolume(SoundManager.AmbientVolume + 0.05f);

            if (Engine.DrawButton(new Rectangle(160, 452, 380, 44),
                $"ЗВУК В ИГРЕ: {(SoundManager.Muted ? "ВЫКЛЮЧЕН (MUTE)" : "ВКЛЮЧЕН")}",
                SoundManager.Muted ? Rgb(115, 42, 48) : Rgb(38, 95, 68),
                SoundManager.Muted ? Rgb(155, 58, 65) : Rgb(55, 135, 95), Color.White, 17f))
            {
                SoundManager.ToggleMute();
            }

            bool isFs = Raylib.IsWindowFullscreen();
            if (Engine.DrawButton(new Rectangle(160, 508, 380, 44), $"ПОЛНЫЙ ЭКРАН: {(isFs ? "ВКЛ" : "ВЫКЛ")}", Rgb(45, 65, 95), Rgb(65, 95, 135), Color.White, 17f))
                Raylib.ToggleFullscreen();

            if (Engine.DrawButton(new Rectangle(160, 566, 380, 48), "<- НАЗАД В ГЛАВНОЕ МЕНЮ", Rgb(60, 65, 82), Rgb(85, 92, 115), Color.White, 17f))
                _screen = "main";
        }

        private static void DrawWrappedText(string text, float x, float y, int maxCharsPerLine, float fontSize, Color col, int maxLines = 3, float lineStep = 18f)
        {
            if (string.IsNullOrEmpty(text)) return;
            string clean = text.Replace("\r", "").Replace("\n", " ");
            string[] words = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int curLine = 0;
            string line = "";

            for (int i = 0; i < words.Length; i++)
            {
                string candidate = string.IsNullOrEmpty(line) ? words[i] : $"{line} {words[i]}";
                if (candidate.Length > maxCharsPerLine && !string.IsNullOrEmpty(line))
                {
                    if (curLine == maxLines - 1)
                    {
                        string ell = line.Length > maxCharsPerLine - 3 ? line[..(maxCharsPerLine - 3)] + "..." : line + "...";
                        Engine.DrawTextLeft(ell, x, y + curLine * lineStep, fontSize, col);
                        return;
                    }
                    Engine.DrawTextLeft(line, x, y + curLine * lineStep, fontSize, col);
                    curLine++;
                    line = words[i];
                }
                else
                {
                    line = candidate;
                }
            }

            if (!string.IsNullOrEmpty(line) && curLine < maxLines)
            {
                Engine.DrawTextLeft(line, x, y + curLine * lineStep, fontSize, col);
            }
        }

        private static string GetReadableRosterName(string key, SurvivorConfig scfg) => key switch
        {
            "commando" => "Коммандо",
            "artificer" => "Изобретатель",
            "huntress" => "Охотница",
            "buzz" => "Базз Лайтер",
            "v1" => "V1 (ULTRAKILL)",
            "sol" => "Сол Бэдгай",
            "gwen" => "Гвен (LoL)",
            "enforcer" => "Силовик",
            "scout" => "Разведчик TF2",
            "m4a1" => "M4A1 (GFL)",
            "medic" => "Медик TF2",
            "aigis" => "Айгис (P3)",
            "reimu" => "Рейму Хакурей",
            "mindustry" => "Ядро Mindustry",
            "slark" => "Сларк (Dota)",
            "tidehunter" => "Тайдхантер",
            "freeman" => "Гордон Фриман",
            "dante" => "Данте (Limbus)",
            "cheburashka" => "Чебурашка",
            "ahri" => "Ари (LoL)",
            "lappland" => "Лаппланд",
            "w_arknights" => "W / Вишадель",
            "gold_ship" => "Голд Шип",
            "pushnoy" => "А. Пушной",
            "freddy" => "Мишка Фредди",
            "steve" => "Стив (Minecraft)",
            "jotaro" => "Джотаро Куджо",
            "vyper" => "Вайпер",
            "haze" => "Хейз (Deadlock)",
            "dmc_trio" => "Трио DMC",
            "jack_o" => "Джек-О Валентайн",
            "sanabi" => "SANABI (Генерал)",
            _ => scfg.Name
        };

        private static void DrawLobbyUI()
        {
            DrawHeader();
            if (_selectedChar == "sanabi" && !Data.ProgressData.Instance.IsSanabiAuthorized())
                _selectedChar = "commando";

            var cfg = SurvivorDatabase.Classes[_selectedChar];
            cfg.SyncLoadout();
            var prog = Data.ProgressData.Instance;
            var activeSkin = cfg.GetActiveSkin();
            var rosterKeys = SurvivorDatabase.GetVisibleRosterKeys();

            // =========================================================================
            // 1. ЛЕВАЯ КОЛОНКА (x = 18..408): СПИСОК ПЕРСОНАЖЕЙ С ОТМЕТКАМИ ОТКРЫТИЯ
            // =========================================================================
            Raylib.DrawRectangle(18, 66, 390, 782, Rgb(12, 16, 28, 244));
            Raylib.DrawRectangle(18, 66, 390, 4, Color.SkyBlue);
            int unlockedHeroCount = 0;
            foreach (var rk in rosterKeys) if (prog.IsCharacterUnlocked(rk)) unlockedHeroCount++;
            Engine.DrawTextLeft($"ВЫБОР ГЕРОЯ ({unlockedHeroCount}/{rosterKeys.Count} ОТКРЫТО)", 30, 76, 16f, Color.SkyBlue);

            for (int i = 0; i < rosterKeys.Count; i++)
            {
                string k = rosterKeys[i];
                var scfg = SurvivorDatabase.Classes[k];
                int row = i / 2, col = i % 2;
                int bx = 28 + col * 188;
                int by = 100 + row * 39;
                Rectangle rect = new(bx, by, 182, 35);
                bool isSel = k == _selectedChar;
                bool isUnlocked = prog.IsCharacterUnlocked(k);
                bool isSanabi = k == "sanabi";

                string readableName = GetReadableRosterName(k, scfg);
                string btnTitle = isUnlocked
                    ? (isSanabi ? "★ " : "") + readableName
                    : $"[ЗАКР.] {readableName}";
                Color bgCol = isSel
                    ? Rgb(44, 86, 148, 250)
                    : (isSanabi ? Rgb(54, 36, 20, 245) : (isUnlocked ? Rgb(20, 26, 40, 240) : Rgb(14, 16, 24, 235)));
                Color borderCol = isSel
                    ? Color.Yellow
                    : (isSanabi ? Color.Gold : (isUnlocked ? Rgb(scfg.BodyColor.R, scfg.BodyColor.G, scfg.BodyColor.B, 140) : Rgb(75, 82, 98, 130)));
                Color txtCol = isSel
                    ? Color.Yellow
                    : (isSanabi ? Color.Gold : (isUnlocked ? Color.White : Rgb(132, 142, 162)));

                if (Engine.DrawButton(rect, btnTitle, bgCol, Rgb(56, 98, 165), txtCol, 12.5f, borderCol))
                {
                    SelectCharacter(k);
                    if (!isUnlocked)
                    {
                        var (howTo, _) = GetCharacterUnlockInfo(k);
                        ShowBanner($"КАК ОТКРЫТЬ «{scfg.Name}»: {howTo}", Color.Orange, 3.8f);
                    }
                }
                // Цветной маркер героя слева на кнопке
                Raylib.DrawRectangle(bx, by, 5, 35, isSel ? Color.Yellow : (isUnlocked ? scfg.BodyColor : Rgb(70, 75, 90)));
            }

            if (!_net.Connected)
            {
                bool shipUnlLobby = IsSafeTravelsUnlocked();
                if (Engine.DrawButton(new Rectangle(28, 746, 370, 40),
                    shipUnlLobby ? "★ НА БОРТ UES «SAFE TRAVELS»" : "[ЗАКР.] UES «SAFE TRAVELS» (ПОБЕДИТЕ МИТРИКСА)",
                    shipUnlLobby ? Rgb(28, 76, 126) : Rgb(28, 28, 38),
                    shipUnlLobby ? Rgb(48, 114, 182) : Rgb(44, 44, 58),
                    shipUnlLobby ? Color.Yellow : Color.Gray, 13f,
                    shipUnlLobby ? Color.SkyBlue : Color.Gray))
                {
                    if (shipUnlLobby) EnterSafeTravels(_selectedChar);
                    else
                    {
                        SoundManager.Play("ui_error");
                        ShowBanner("ПОБЕДИТЕ МИТРИКСА ХОТЯ БЫ 1 РАЗ, ЧТОБЫ ОТКРЫТЬ КОРАБЛЬ UES «SAFE TRAVELS»!", Color.Orange, 3.5f);
                    }
                }
            }

            if (Engine.DrawButton(new Rectangle(28, 792, 370, 44), "<- ВЫЙТИ В ГЛАВНОЕ МЕНЮ [ESC]",
                Rgb(82, 38, 46), Rgb(122, 54, 64), Color.White, 15f))
            {
                _net.StopSession();
                _lobbyPlayers.Clear();
                _screen = "main";
            }

            // =========================================================================
            // 2. ЦЕНТРАЛЬНАЯ ВИТРИНА (x = 420..908): КАРТОЧКА СТАТОВ, 3D-МОДЕЛЬ И СКИНЫ
            // =========================================================================
            // Верхняя плашка имени, подзаголовка и 4 ключевых статов
            Raylib.DrawRectangle(420, 66, 488, 92, Rgb(12, 16, 28, 238));
            Raylib.DrawRectangle(420, 66, 488, 4, activeSkin.PrimaryColor);

            string heroDisplayTitle = activeSkin.CustomSkillVariants != null
                ? $"{cfg.Name} :: {activeSkin.Name.ToUpper()}"
                : cfg.Name;
            Engine.DrawTextCentered(heroDisplayTitle, 664f, 84f, 22f, activeSkin.PrimaryColor);
            Engine.DrawTextCentered(cfg.Subtitle, 664f, 108f, 13f, Rgb(185, 215, 245));

            // 4 аккуратных индикатора характеристик
            (string lbl, string val, Color c)[] statPills =
            {
                ("ЗДОРОВЬЕ", $"{cfg.Hp} HP", Rgb(95, 230, 110)),
                ("УРОН", $"{cfg.Damage}", Rgb(255, 195, 65)),
                ("СКОРОСТЬ", $"{cfg.Speed} м/с", Color.SkyBlue),
                ("АТАКА", $"{cfg.FireRate}/с", Rgb(235, 130, 255))
            };
            for (int si = 0; si < statPills.Length; si++)
            {
                int px = 430 + si * 118;
                Raylib.DrawRectangle(px, 124, 112, 26, Rgb(20, 28, 44, 235));
                Engine.DrawTextCentered($"{statPills[si].lbl}: {statPills[si].val}", px + 56, 137, 12f, statPills[si].c);
            }

            // Подсказка вращения 3D-модели или баннер активного превью навыка
            if (_skillPreviewTimer > 0f)
            {
                Raylib.DrawRectangle(420, 580, 488, 34, Rgb(14, 20, 34, 240));
                Raylib.DrawRectangle(420, 580, 4, 34, _skillPreviewColor);
                Engine.DrawTextCentered(_skillPreviewTitle, 664f, 597f, 13f, Color.Yellow);
            }
            else
            {
                Raylib.DrawRectangle(460, 584, 408, 26, Rgb(10, 14, 24, 175));
                Engine.DrawTextCentered("Зажмите ЛКМ/ПКМ по герою для вращения  |  Колесо — масштаб", 664f, 597f, 12f, Rgb(170, 195, 225));
            }

            // Панель быстрого выбора Скина / Альтернативной Версии прямо под 3D-моделью!
            Raylib.DrawRectangle(420, 620, 488, 84, Rgb(12, 16, 28, 242));
            Raylib.DrawRectangle(420, 620, 488, 3, Color.Gold);
            string skinHeader = cfg.Skins.Exists(s => s.CustomSkillVariants != null)
                ? "ВЫБОР ОБЛИКА / ВЕРСИИ ГЕРОЯ (МЕНЯЕТ НАВЫКИ ★):"
                : "ВЫБОР ОБЛИКА (СКИНА) ГЕРОЯ:";
            Engine.DrawTextLeft(skinHeader, 432, 628, 13f, Color.Gold);

            int curSkinIdx = prog.GetSelectedSkin(_selectedChar);
            for (int si = 0; si < cfg.Skins.Count; si++)
            {
                var skDef = cfg.Skins[si];
                bool unlocked = prog.IsSkinUnlocked(_selectedChar, si, skDef.UnlockAchievementKey);
                bool selected = curSkinIdx == si && unlocked;
                Rectangle sRect = new(430 + si * 158, 648, 152, 48);

                string prefix = !unlocked ? "[ЗАКР.] " : (skDef.CustomSkillVariants != null ? "★ " : "");
                string btnLabel = skDef.CustomSkillVariants != null
                    ? $"{prefix}{skDef.Name}\n[НОВЫЕ НАВЫКИ]"
                    : $"{prefix}{skDef.Name}";

                Color bgCol = selected
                    ? Rgb(48, 95, 160)
                    : (unlocked ? Rgb(24, 32, 50) : Rgb(18, 20, 28));
                Color borderCol = selected
                    ? Color.Yellow
                    : (unlocked ? skDef.PrimaryColor : Color.Gray);

                if (Engine.DrawButton(sRect, btnLabel, bgCol, Rgb(65, 115, 185),
                    unlocked ? (selected ? Color.Yellow : Color.White) : Color.Gray, 12f, borderCol))
                {
                    if (unlocked)
                    {
                        prog.SetSelectedSkin(_selectedChar, si);
                        SelectCharacter(_selectedChar);
                    }
                    else
                    {
                        SoundManager.Play("ui_error");
                        ShowBanner($"ОБЛИК ЗАКРЫТ :: {GetSkinUnlockInfo(cfg, si)}", Color.Orange, 3.2f);
                    }
                }
            }

            // Крупная кнопка старта забега
            bool isSelectedUnlocked = prog.IsCharacterUnlocked(_selectedChar);
            if (isSelectedUnlocked)
            {
                string startLabel = !_net.Connected
                    ? "НАЧАТЬ ЗАБЕГ (СТАРТ) [ENTER]"
                    : (_myReady ? "ВЫ ГОТОВЫ ✓ (ОЖИДАНИЕ ИГРОКОВ)" : "ГОТОВ К БОЮ! [ENTER]");
                if (Engine.DrawButton(new Rectangle(420, 712, 488, 62), startLabel,
                    Rgb(36, 122, 70), Rgb(54, 168, 96), Color.White, 20f, Color.Lime) || Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    if (!_net.Connected)
                    {
                        StartRun(_selectedChar);
                    }
                    else if (!_myReady)
                    {
                        _myReady = true;
                        SoundManager.Play("ui_ready");
                        BroadcastLobbyState();
                        EvaluateLobbyCountdown();
                    }
                }
            }
            else
            {
                var (howToUnlock, _) = GetCharacterUnlockInfo(_selectedChar);
                Raylib.DrawRectangle(420, 712, 488, 62, Rgb(44, 20, 28, 246));
                Raylib.DrawRectangleLines(420, 712, 488, 62, Color.Orange);
                Raylib.DrawRectangle(420, 712, 5, 62, Color.Orange);
                Engine.DrawTextLeft("★ ПЕРСОНАЖ ЗАКРЫТ — УСЛОВИЕ РАЗБЛОКИРОВКИ:", 434, 717, 13f, Color.Gold);
                DrawWrappedText(howToUnlock, 434, 735, 58, 12.5f, Color.White, maxLines: 2, lineStep: 16f);
                if ((Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), new Rectangle(420, 712, 488, 62))
                     && Raylib.IsMouseButtonPressed(MouseButton.Left)) || Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    SoundManager.Play("ui_error");
                    ShowBanner($"КАК ОТКРЫТЬ «{cfg.Name}»: {howToUnlock}", Color.Orange, 3.8f);
                }
            }

            // Компактная плашка статуса комнаты и сложности под кнопкой старта
            var diffInfo = GameData.GetActiveDifficulty();
            Raylib.DrawRectangle(420, 782, 488, 66, Rgb(12, 16, 28, 238));
            Raylib.DrawRectangle(420, 782, 4, 66, diffInfo.Color);
            if (!_net.Connected)
            {
                Engine.DrawTextLeft($"РЕЖИМ: Одиночная игра  |  Сложность: {diffInfo.Name} (x{diffInfo.ScaleMult:F2})", 434, 792, 14f, diffInfo.Color);
                string artTxt = GameData.ActiveArtifacts.Count == 0
                    ? "Артефакты забега: Выключены (0/10)"
                    : $"Активные артефакты ({GameData.ActiveArtifacts.Count}): " + string.Join(", ", System.Linq.Enumerable.Select(GameData.ActiveArtifacts, ak => GameData.RunArtifacts[ak].Short));
                Engine.DrawTextLeft(artTxt, 434, 816, 13f, GameData.ActiveArtifacts.Count > 0 ? Color.Gold : Rgb(165, 185, 210));
            }
            else
            {
                string cdTxt = _lobbyCountdownActive ? $"  |  СТАРТ ЧЕРЕЗ {_lobbyCountdown:F1}С" : "";
                Engine.DrawTextLeft($"КОМНАТА: {_net.RoomCode} [{_net.Mode?.ToUpper()}]{cdTxt}", 434, 792, 14f, Color.Lime);
                Engine.DrawTextLeft($"Игроков в лобби: {1 + _lobbyPlayers.Count}  |  Сложность: {diffInfo.Name}", 434, 816, 13f, Color.SkyBlue);
            }

            // =========================================================================
            // 3. ПРАВАЯ КОЛОНКА (x = 920..1518): НАВЫКИ, ДОСЬЕ И МОДИФИКАТОРЫ
            // =========================================================================
            DrawLobbyCharacterPanel(cfg);
        }

        private static void DrawLobbyCharacterPanel(SurvivorConfig cfg)
        {
            cfg.SyncLoadout();
            var prog = Data.ProgressData.Instance;
            var activeSkin = cfg.GetActiveSkin();

            int px = 920, py = 66, pw = 598, ph = 782;
            Raylib.DrawRectangle(px, py, pw, ph, Rgb(12, 16, 28, 244));
            Raylib.DrawRectangle(px, py, pw, 4, activeSkin.PrimaryColor);

            if (_lobbyTab == "loadout") _lobbyTab = "skills";

            // 3 ЧИСТЫЕ И ПОНЯТНЫЕ ВКЛАДКИ СПРАВА
            (string tabId, string tabTitle)[] lobbyTabs =
            {
                ("skills", "1. НАВЫКИ И СНАРЯЖЕНИЕ"),
                ("overview", "2. ДОСЬЕ И ОПИСАНИЕ"),
                ("modifiers", $"3. СЛОЖНОСТЬ ({GameData.ActiveArtifacts.Count})")
            };
            for (int ti = 0; ti < lobbyTabs.Length; ti++)
            {
                var (tid, ttitle) = lobbyTabs[ti];
                bool isTab = _lobbyTab == tid;
                Rectangle tRect = new(px + 14 + ti * 192, py + 12, 186, 40);
                if (Engine.DrawButton(tRect, ttitle,
                    isTab ? Rgb(45, 92, 158) : Rgb(22, 28, 44),
                    Rgb(65, 120, 195),
                    isTab ? Color.Yellow : Color.White, 13f,
                    isTab ? Color.Yellow : activeSkin.PrimaryColor))
                {
                    _lobbyTab = tid;
                }
            }

            if (_lobbyTab == "skills")
            {
                // Карточка пассивной способности
                int passY = py + 62;
                Raylib.DrawRectangle(px + 14, passY, pw - 28, 84, Rgb(18, 26, 44, 240));
                Raylib.DrawRectangle(px + 14, passY, 4, 84, Color.Gold);
                Engine.DrawTextLeft($"ПАССИВКА :: {cfg.PassiveName}", px + 26, passY + 8, 16f, Color.Gold);
                DrawWrappedText(cfg.PassiveDesc, px + 26, passY + 32, 66, 14f, Rgb(220, 232, 248), maxLines: 2, lineStep: 20f);

                // 4 Карточки активных навыков с прямым переключением [СТАНД.] / [АЛЬТ.] и кнопкой [▶ 3D]
                (string slot, string bindName, Color slotCol)[] slotInfos =
                {
                    ("m1", "ЛКМ", Color.Yellow),
                    ("m2", "ПКМ", Color.Orange),
                    ("shift", "SHIFT", Color.SkyBlue),
                    ("r", "R (УЛЬТА)", Color.Magenta)
                };

                var activeVariantsMap = activeSkin.CustomSkillVariants ?? cfg.SkillVariants;
                int sy = passY + 94;

                foreach (var (slot, bindName, slotCol) in slotInfos)
                {
                    if (!cfg.Skills.TryGetValue(slot, out var sk)) continue;
                    int curVar = prog.GetSkillVariant(_selectedChar, slot);
                    var variants = activeVariantsMap.TryGetValue(slot, out var vList) ? vList : null;

                    Raylib.DrawRectangle(px + 14, sy, pw - 28, 146, Rgb(18, 24, 38, 240));
                    Raylib.DrawRectangle(px + 14, sy, 4, 146, slotCol);

                    // Верхняя строка карточки: клавиша + название выбранного навыка + КД
                    Engine.DrawTextLeft($"[{bindName}] {sk.Name}", px + 26, sy + 8, 17f, slotCol);
                    Engine.DrawTextLeft($"КД: {sk.Cd}с", px + 420, sy + 10, 14f, Color.Gold);

                    if (Engine.DrawButton(new Rectangle(px + pw - 96, sy + 6, 74, 24), "▶ 3D",
                        Rgb(34, 62, 104), Rgb(54, 96, 156), Color.Yellow, 12f, slotCol))
                    {
                        TriggerLobbySkillPreview(_selectedChar, slot, curVar);
                    }

                    // Переключатели вариантов навыка (Обычный / Альтернативный) прямо внутри карточки
                    if (variants != null && variants.Count > 0)
                    {
                        for (int vi = 0; vi < variants.Count; vi++)
                        {
                            var vSkill = variants[vi];
                            bool unlocked = prog.IsSkillUnlocked(_selectedChar, slot, vi);
                            bool isSel = curVar == vi && unlocked;
                            int btnW = 272;
                            Rectangle vRect = new(px + 26 + vi * 280, sy + 36, btnW, 30);

                            string tag = vi == 0 ? "1. СТАНД" : "2. АЛЬТ";
                            string shortName = vSkill.Name.Length > 22 ? vSkill.Name[..20] + ".." : vSkill.Name;
                            string btnTxt = unlocked ? $"{tag}: {shortName}" : $"[ЗАКР.] {tag}: ???";

                            Color bgCol = isSel ? Rgb(44, 88, 152) : (unlocked ? Rgb(24, 32, 48) : Rgb(18, 20, 28));
                            Color brdCol = isSel ? Color.Yellow : (unlocked ? slotCol : Color.Gray);

                            if (Engine.DrawButton(vRect, btnTxt, bgCol, Rgb(62, 112, 182),
                                unlocked ? (isSel ? Color.Yellow : Color.White) : Color.Gray, 12f, brdCol))
                            {
                                if (unlocked)
                                {
                                    prog.SetSkillVariant(_selectedChar, slot, vi);
                                    cfg.SyncLoadout();
                                    TriggerLobbySkillPreview(_selectedChar, slot, vi);
                                }
                                else
                                {
                                    SoundManager.Play("ui_error");
                                    ShowBanner($"НАВЫК ЗАКРЫТ :: {GetSkillUnlockInfo(cfg, vSkill)}", Color.Orange, 3.2f);
                                }
                            }
                        }
                    }

                    // Читаемое многострочное описание выбранного навыка
                    DrawWrappedText(sk.Desc, px + 26, sy + 74, 66, 14f, Color.White, maxLines: 3, lineStep: 19f);

                    sy += 154;
                }
            }
            else if (_lobbyTab == "overview")
            {
                int cy = py + 66;
                // 1. Роль и биография
                Raylib.DrawRectangle(px + 14, cy, pw - 28, 186, Rgb(18, 24, 40, 240));
                Raylib.DrawRectangle(px + 14, cy, 4, 186, activeSkin.PrimaryColor);
                Engine.DrawTextLeft($"ДОСЬЕ ВЫЖИВШЕГО :: {cfg.Name}", px + 28, cy + 12, 18f, activeSkin.PrimaryColor);
                Engine.DrawTextLeft(cfg.Subtitle, px + 28, cy + 38, 14f, Color.Gold);
                DrawWrappedText(cfg.OverviewLore, px + 28, cy + 66, 64, 15f, Rgb(220, 232, 248), maxLines: 5, lineStep: 22f);

                // 2. Врождённая пассивная особенность
                cy += 198;
                Raylib.DrawRectangle(px + 14, cy, pw - 28, 142, Rgb(18, 24, 40, 240));
                Raylib.DrawRectangle(px + 14, cy, 4, 142, Color.SkyBlue);
                Engine.DrawTextLeft($"ВРОЖДЁННАЯ ОСОБЕННОСТЬ :: {cfg.PassiveName}", px + 28, cy + 12, 16f, Color.SkyBlue);
                DrawWrappedText(cfg.PassiveDesc, px + 28, cy + 42, 64, 15f, Color.White, maxLines: 4, lineStep: 22f);

                // 3. Подробности об обликах / альтернативных версиях
                cy += 154;
                Raylib.DrawRectangle(px + 14, cy, pw - 28, 236, Rgb(18, 24, 40, 240));
                Raylib.DrawRectangle(px + 14, cy, 4, 236, Color.Gold);
                Engine.DrawTextLeft("ДОСТУПНЫЕ ОБЛИКИ И ВЕРСИИ ПЕРСОНАЖА:", px + 28, cy + 12, 16f, Color.Gold);

                int curSkin = prog.GetSelectedSkin(_selectedChar);
                for (int si = 0; si < cfg.Skins.Count; si++)
                {
                    var skDef = cfg.Skins[si];
                    bool unlocked = prog.IsSkinUnlocked(_selectedChar, si, skDef.UnlockAchievementKey);
                    bool isSel = curSkin == si && unlocked;
                    int sy = cy + 42 + si * 62;
                    Raylib.DrawRectangle(px + 26, sy, pw - 52, 56, isSel ? Rgb(32, 52, 88, 235) : Rgb(14, 18, 30, 220));
                    Raylib.DrawRectangle(px + 26, sy, 3, 56, unlocked ? skDef.PrimaryColor : Color.Gray);

                    string status = unlocked ? (isSel ? "[АКТИВЕН ✓]" : "[ОТКРЫТ]") : "[ЗАКРЫТ]";
                    Engine.DrawTextLeft($"{si + 1}. {skDef.Name}  {status}", px + 38, sy + 6, 15f, isSel ? Color.Yellow : (unlocked ? skDef.PrimaryColor : Color.Gray));
                    string skinLine = unlocked ? skDef.Desc : GetSkinUnlockInfo(cfg, si);
                    DrawWrappedText(skinLine, px + 38, sy + 28, 62, 13f, unlocked ? Rgb(195, 210, 232) : Color.Orange, maxLines: 1, lineStep: 18f);
                }

                // 4. Список игроков в коопе (если подключены)
                cy += 248;
                Raylib.DrawRectangle(px + 14, cy, pw - 28, 128, Rgb(16, 22, 36, 240));
                Raylib.DrawRectangle(px + 14, cy, 4, 128, Color.Lime);
                if (!_net.Connected)
                {
                    Engine.DrawTextLeft("СТАТУС ОТРЯДА: ОДИНОЧНЫЙ ЗАБЕГ", px + 28, cy + 14, 15f, Color.SkyBlue);
                    Engine.DrawTextLeft("Для игры с друзьями откройте «Сетевая игра» в Главном меню.", px + 28, cy + 42, 14f, Rgb(185, 205, 230));
                }
                else
                {
                    Engine.DrawTextLeft($"ОТРЯД В КОМНАТЕ {_net.RoomCode}:", px + 28, cy + 10, 15f, Color.Lime);
                    int ly = cy + 34;
                    Engine.DrawTextLeft($"• ВЫ [{_net.MyId.ToUpper()}]: {cfg.Name} — [{(_myReady ? "ГОТОВ ✓" : "ВЫБИРАЕТ")}]", px + 28, ly, 14f, Color.White);
                    foreach (var kv in _lobbyPlayers)
                    {
                        ly += 22;
                        string cName = SurvivorDatabase.Classes.TryGetValue(kv.Value.CharKey, out var sc) ? sc.Name : "COMMANDO";
                        Engine.DrawTextLeft($"• ИГРОК [{kv.Key.ToUpper()}]: {cName} — [{(kv.Value.Ready ? "ГОТОВ ✓" : "ВЫБИРАЕТ")}]", px + 28, ly, 14f, Color.SkyBlue);
                    }
                }
            }
            else if (_lobbyTab == "modifiers")
            {
                Vector2 mp = Raylib.GetMousePosition();
                var curDiff = GameData.GetActiveDifficulty();
                string hoverTitle = $"{curDiff.Name}  —  {curDiff.Subtitle}";
                string hoverSub = $"Рост сложности: x{curDiff.ScaleMult:F2}  |  Реген: x{curDiff.RegenMult:F2}  |  Урон врагов: x{curDiff.PlayerDmgTakenMult:F2}";
                string hoverDesc = curDiff.Desc;
                Color hoverCol = curDiff.Color;

                bool canEditMods = !_net.Connected || _net.Mode == "host";

                // 1. ВЫБОР РЕЖИМА СЛОЖНОСТИ (4 РЕЖИМА)
                int dy = py + 64;
                Raylib.DrawRectangle(px + 14, dy, pw - 28, 122, Rgb(18, 24, 40, 238));
                Raylib.DrawRectangle(px + 14, dy, 4, 122, curDiff.Color);
                Engine.DrawTextLeft("РЕЖИМ СЛОЖНОСТИ ЗАБЕГА (4 УРОВНЯ):", px + 28, dy + 10, 15f, curDiff.Color);
                if (!canEditMods)
                    Engine.DrawTextLeft("[Управляется хостом]", px + 380, dy + 10, 13f, Color.Orange);

                for (int di = 0; di < GameData.OrderedDifficultyKeys.Length; di++)
                {
                    string dKey = GameData.OrderedDifficultyKeys[di];
                    var dDef = GameData.Difficulties[dKey];
                    bool isSel = GameData.SelectedDifficulty == dKey;
                    Rectangle dRect = new(px + 26 + di * 138, dy + 38, 132, 72);

                    if (Raylib.CheckCollisionPointRec(mp, dRect))
                    {
                        hoverCol = dDef.Color;
                        hoverTitle = $"СЛОЖНОСТЬ: {dDef.Name} [{dDef.Icon}]";
                        hoverSub = $"{dDef.Subtitle}  |  Множитель времени: x{dDef.ScaleMult:F2}";
                        hoverDesc = dDef.Desc;
                    }

                    string dBtnTxt = $"{dDef.Name}\nx{dDef.ScaleMult:F2} [{dDef.Icon}]";
                    if (Engine.DrawButton(dRect, dBtnTxt,
                        isSel ? Rgb(44, 78, 132) : Rgb(24, 30, 46),
                        Rgb(62, 105, 170),
                        isSel ? Color.Yellow : dDef.Color, 13f,
                        isSel ? Color.Yellow : dDef.Color) && canEditMods)
                    {
                        GameData.SelectedDifficulty = dKey;
                        prog.Save();
                        BroadcastLobbyState();
                    }
                }

                // 2. СЕТКА 10 АРТЕФАКТОВ ЗАБЕГА (5x2)
                int ay = dy + 134;
                Raylib.DrawRectangle(px + 14, ay, pw - 28, 246, Rgb(18, 24, 40, 238));
                Raylib.DrawRectangle(px + 14, ay, 4, 246, Color.Gold);
                Engine.DrawTextLeft($"АРТЕФАКТЫ ЗАБЕГА ({GameData.ActiveArtifacts.Count}/10):", px + 28, ay + 10, 15f, Color.Gold);

                if (canEditMods)
                {
                    if (Engine.DrawButton(new Rectangle(px + 332, ay + 8, 118, 26), "ВКЛЮЧИТЬ ВСЕ",
                        Rgb(42, 72, 48), Rgb(62, 110, 72), Color.Lime, 12f, Color.Lime))
                    {
                        foreach (var ak in GameData.OrderedArtifactKeys) GameData.ActiveArtifacts.Add(ak);
                        prog.Save();
                        BroadcastLobbyState();
                    }
                    if (Engine.DrawButton(new Rectangle(px + 458, ay + 8, 118, 26), "СБРОСИТЬ ВСЕ",
                        Rgb(78, 38, 44), Rgb(115, 55, 65), Color.White, 12f, Color.Red))
                    {
                        GameData.ActiveArtifacts.Clear();
                        prog.Save();
                        BroadcastLobbyState();
                    }
                }

                for (int ai = 0; ai < GameData.OrderedArtifactKeys.Length; ai++)
                {
                    string aKey = GameData.OrderedArtifactKeys[ai];
                    var aDef = GameData.RunArtifacts[aKey];
                    bool enabled = GameData.HasArtifact(aKey);
                    int r = ai / 5, c = ai % 5;
                    Rectangle aRect = new(px + 26 + c * 110, ay + 44 + r * 94, 104, 84);

                    if (Raylib.CheckCollisionPointRec(mp, aRect))
                    {
                        hoverCol = aDef.Color;
                        hoverTitle = $"{aDef.Name}  [Код: {aDef.Short}]";
                        hoverSub = $"{aDef.Subtitle}  —  Статус: {(enabled ? "ВКЛЮЧЁН ✓" : "ВЫКЛЮЧЕН")}";
                        hoverDesc = aDef.Desc;
                    }

                    string shortTitle = aDef.Name.Replace("Артефакт ", "");
                    string aBtnTxt = $"{(enabled ? "★ ВКЛ" : "ВЫКЛ")}\n{shortTitle}";
                    if (Engine.DrawButton(aRect, aBtnTxt,
                        enabled ? Rgb(48, 82, 138) : Rgb(22, 26, 38),
                        Rgb(65, 108, 175),
                        enabled ? Color.Yellow : Rgb(175, 190, 215), 12f,
                        enabled ? Color.Yellow : aDef.Color) && canEditMods)
                    {
                        if (enabled) GameData.ActiveArtifacts.Remove(aKey);
                        else GameData.ActiveArtifacts.Add(aKey);
                        prog.Save();
                        BroadcastLobbyState();
                    }
                }

                // 3. ОКНО ПОДРОБНОГО ОПИСАНИЯ ВЫБРАННОГО МОДИФИКАТОРА
                int descY = ay + 258;
                Raylib.DrawRectangle(px + 14, descY, pw - 28, 180, Rgb(14, 20, 34, 248));
                Raylib.DrawRectangle(px + 14, descY, pw - 28, 3, hoverCol);
                Engine.DrawTextLeft(hoverTitle, px + 28, descY + 14, 18f, hoverCol);
                Engine.DrawTextLeft(hoverSub, px + 28, descY + 42, 14f, Color.Gold);
                DrawWrappedText(hoverDesc, px + 28, descY + 72, 64, 15f, Color.White, maxLines: 4, lineStep: 22f);
            }
        }

        public static void StartRun(string charKey, int? seed = null, bool fromNetwork = false, int startStage = 1)
        {
            if (!SurvivorDatabase.Classes.ContainsKey(charKey) || !ProgressData.Instance.IsCharacterUnlocked(charKey))
                charKey = "commando";
            _selectedChar = charKey;

            _screen = "in_game";
            _paused = false;
            _workbenchOpen = false;
            _mainPedestal.Enabled = false;
            _menuCrystal.Enabled = false;
            if (SurvivorDatabase.IsMobaCharacter(charKey) || charKey == "mindustry") Raylib.EnableCursor();
            else Raylib.DisableCursor();

            RunKills = 0;
            RunBossKills = 0;
            RunDamageDealt = 0f;
            RunGoldEarned = 0;
            _deadCoopPlayers.Clear();
            _commandPickerQueue.Clear();
            _dialogueOpen = false;
            _rpsOpen = false;
            _emoteWheelOpen = false;
            _safeTravelsWardrobeOpen = false;
            _run = new GameRunContext
            {
                CharKey = charKey,
                LunarCoins = Math.Max(2, ProgressData.Instance.LunarCoins)
            };
            foreach (var k in GameData.ItemPool.Keys) _run.Items[k] = 0;
            WorldContext.Game = _run;

            _run.PlayerEntity = new Entity(position: new(0, charKey == "mindustry" ? 1.2f : 0f, 0));
            _run.Parts = SurvivorDatabase.BuildSurvivorMesh(_run.PlayerEntity, charKey);
            _run.Combat = new SurvivorCombatController(_run);
            _isSpectating = false;
            if (SurvivorCombatController.IsFirstPersonCharacter(charKey))
                _run.Parts.Root.Enabled = false;
            _run.PlayerHp = _run.PlayerMaxHp;

            int chosenSeed = seed ?? (Environment.TickCount & 0x7FFFFFFF);
            string? initMode = (_net.Connected && _net.GameType == "pvp") ? "pvp" : null;
            LoadStage(startStage, initMode, chosenSeed, fromNetwork: true);

            if (_net.Connected && _net.Mode == "host" && !fromNetwork)
            {
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "lobby_start_run",
                    ["stage"] = _run.Stage,
                    ["seed"] = chosenSeed,
                    ["diff"] = GameData.SelectedDifficulty,
                    ["arts"] = string.Join(",", GameData.ActiveArtifacts)
                });
            }
        }

        public static void LoadStage(int stageNum, string? specialMode, int? fixedSeed = null, bool fromNetwork = false)
        {
            if (_run == null) return;
            foreach (var e in CombatManager.Enemies.ToArray()) Destroy(e);
            CombatManager.Enemies.Clear();
            foreach (var rp in _remotePlayers.Values) Destroy(rp);
            _remotePlayers.Clear();
            foreach (var e in _run.StageEntities.ToArray()) Destroy(e);
            _run.StageEntities.Clear();

            int coopCount = 1 + (_net.Connected ? Math.Max(_remotePlayers.Count, _lobbyPlayers.Count) : 0);
            _run.Stage = stageNum;
            _run.StageElapsedTime = 0f;
            _run.TeleporterChargingNow = false;
            _run.TeleporterActiveTimer = 0f;
            _run.Gold = (stageNum == 1 && specialMode == null) ? 15 : 0;
            _run.Combat.MindustryCopper = specialMode == "bazaar" ? 0f : 65f;
            _run.Combat.MindustrySilicon = specialMode == "bazaar" ? 0f : 30f;
            _run.Combat.MindustryThorium = specialMode == "bazaar" ? 0f : 10f;
            _run.Combat.MindustryBuildMode = false;
            _run.Combat.MindustrySelectedBuild = -1;
            if (_run.Combat.MindustryShieldSphere != null) _run.Combat.MindustryShieldSphere.Enabled = false;
            _run.Combat.MindustryTurrets.Clear();
            _run.Combat.DummyClones.Clear();
            _run.BlueOrbSpawned = false;
            _run.MoonPillarsDone = 0;
            _run.MithrixSpawned = false;
            _run.MoonDetonationTimer = 0f;
            _run.ActiveBoss = null;
            _run.DirectorCredits = 38.0f * _run.DifficultyCoeff * (1.0f + 0.55f * (coopCount - 1));

            int seed = fixedSeed ?? (Environment.TickCount & 0x7FFFFFFF);
            var (ents, tpOrPad, pal) = StageBuilder.BuildStageWorld(stageNum, seed, specialMode);
            _run.StageEntities.AddRange(ents);
            if (!string.IsNullOrEmpty(pal.Id) && pal.Id != "pvp")
            {
                ProgressData.Instance.DiscoveredStages.Add(pal.Id);
                if (pal.Id == "rallypoint")
                    _run.UnlockAchievement("ach_blizzard");
                ProgressData.Instance.Save();
            }
            _deadCoopPlayers.Clear();
            _commandPickerQueue.Clear();
            _isSpectating = false;
            _run.PlayerHp = (stageNum == 1 && specialMode == null && GameData.SelectedDifficulty == "eclipse")
                ? _run.PlayerMaxHp * 0.80f
                : MathF.Max(_run.PlayerHp, _run.PlayerMaxHp * 0.5f);
            _run.VengeanceTimer = 85.0f;
            if (GameData.HasArtifact("kin") && specialMode == null)
            {
                string[] kinPool = { "beetle", "lemurian", "jellyfish", "wisp", "hell_drone", "combine_soldier", "sarkaz_mercenary", "imp", "hollow_shadow", "bison", "nether_blaze", "brass_contraption", "greater_wisp", "golem", "parent", "elder_lemurian" };
                _run.StageKinType = kinPool[Math.Abs(seed + stageNum * 17) % kinPool.Length];
            }
            else
            {
                _run.StageKinType = "";
            }
            _run.Parts.Root.Enabled = !SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey);
            if (SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey))
                _run.Combat.BuildV1FpViewmodel();
            _run.ActiveTeleporterOrPad = tpOrPad;
            _run.StageName = pal.Name;

            if (specialMode == "pvp")
            {
                float spawnSide = _net.MyId == "host" ? -28f : 28f;
                _run.PlayerPos = new Vector3(spawnSide, 1f, spawnSide);
            }
            else if (specialMode == "safe_travels") _run.PlayerPos = new Vector3(0f, 1f, -6f);
            else if (specialMode == "bazaar") _run.PlayerPos = new Vector3(0, 1f, -2f);
            else if (specialMode == "moon") _run.PlayerPos = new Vector3(0, 1f, -16f);
            else _run.PlayerPos = new Vector3(0, 1f, 0f);
            _run.PlayerVelY = 0f;
            // Новый этап: трассировка движения начинается заново с точки высадки (без «протаскивания» с прошлой карты)
            _playerSweepValid = false;
            _lastResolvedPlayerPos = _run.PlayerPos;

            if (_run.CharKey == "mindustry" && specialMode is not "bazaar" and not "safe_travels")
                _run.Combat.SpawnMindustryMainCore(_run.PlayerPos);

            if (_net.Connected && !fromNetwork)
            {
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "stage_sync",
                    ["stage"] = stageNum,
                    ["seed"] = seed,
                    ["special_mode"] = specialMode
                });
            }

            if (specialMode == "pvp")
                ShowBanner("КОЛИЗЕЙ РАЗДОРА (PvP) :: ВЫБИВАЙТЕ ПРЕДМЕТЫ ИЗ МОНСТРОВ И СОКРУШИТЕ СОПЕРНИКА!", Color.Red, 4.5f);
            else if (specialMode == "safe_travels")
                ShowBanner("КОРАБЛЬ UES «SAFE TRAVELS» :: КАЮТ-КОМПАНИЯ, МОСТИК, КАЮТЫ И БОЕВОЙ ПОЛИГОН", Rgb(85, 225, 255), 4.5f);
            else if (specialMode == "bazaar")
                ShowBanner("БАЗАР МЕЖДУ ВРЕМЕНЕМ :: ТОРГОВЛЯ С ТРИТОНОМ", Rgb(80, 215, 255), 3.5f);
            else if (specialMode == "pizzeria")
                ShowBanner("ПИЦЦЕРИЯ ФАЗБЕРА :: ПОДДЕРЖИВАЙТЕ ГЕНЕРАТОР МУЗЫКАЛЬНОЙ ШКАТУЛКИ!", Rgb(235, 165, 50), 4.5f);
            else if (specialMode == "black_mesa")
                ShowBanner("СЕКТОР ЧЁРНАЯ МЕЗА :: ПРОНЕСИТЕ КРИСТАЛЛ GG-3883 В АНТИМАСС-СПЕКТРОМЕТР!", Rgb(255, 145, 35), 4.5f);
            else if (specialMode == "qliphoth")
                ShowBanner("ВЕРШИНА ДРЕВА КЛИФОТ :: ПОБЕДИТЕ ДЕМОНИЧЕСКОГО СТРАЖА ВЕРГИЛИЯ!", Rgb(185, 65, 245), 4.5f);
            else if (specialMode == "hell_cybergrind")
                ShowBanner("ПРЕДЕЛ АДА (КИБЕР-ГРАЙНД) :: СОКРУШИТЕ ВОЛНЫ АДСКИХ МАШИН!", Rgb(255, 65, 75), 4.5f);
            else if (specialMode == "tartarus")
                ShowBanner("БАШНЯ ТАРТАР (ТЁМНЫЙ ЧАС) :: ОДОЛЕЙТЕ ТЕНЕЙ И ЖНЕЦА ТАРТАРА!", Rgb(85, 255, 165), 4.5f);
            else if (specialMode == "hakurei_shrine")
                ShowBanner("ХРАМ ХАКУРЕЙ (ГЕНСОКЁ) :: ВЫДЕРЖИТЕ ДАНМАКУ И ЗАПЕЧАТАЙТЕ РАЗЛОМ!", Rgb(255, 145, 195), 4.5f);
            else if (specialMode == "tracen_derby")
                ShowBanner("ИППОДРОМ ТРЕЙСЕН :: ПРОЙДИТЕ ЧЕКПОЙНТЫ ФИНАЛЬНОГО ДЕРБИ!", Rgb(255, 210, 75), 4.5f);
            else if (specialMode == "moon")
                ShowBanner("НАЧАЛО (ЛУНА) :: ЗАРЯДИТЕ ВСЕ 4 СТОЛПА ЛУНЫ НА БАСТИОНАХ!", Rgb(100, 225, 255), 4.5f);
            else
            {
                string layerTag = GameData.GetStagePoolLayerLabel(stageNum);
                string kinNote = (!string.IsNullOrEmpty(_run.StageKinType) && GameData.MonsterLog.TryGetValue(_run.StageKinType, out var km))
                    ? $"  [РОДСТВО: {km.Name.ToUpper()}]"
                    : "";
                ShowBanner($"ЭТАП {stageNum} ({layerTag}): {pal.Name.ToUpper()}{kinNote}", Color.Yellow, 3.8f);
            }
            SoundManager.Play("run_start");
        }

        private static void FinishRunWithSummary(bool victory)
        {
            _lastRunVictory = victory;
            if (_run != null)
            {
                _lastRunTime = _run.ElapsedTime;
                _lastRunStage = _run.Stage;
                _lastRunItems.Clear();
                foreach (var k in _run.ItemOrder)
                    if (_run.Items.TryGetValue(k, out int cnt) && cnt > 0)
                        _lastRunItems.Add((k, cnt));

                if (!_run.IsSafeTravels)
                {
                    var prog = ProgressData.Instance;
                    prog.Kills += RunKills;
                    prog.BossKills += RunBossKills;
                    prog.MaxStage = Math.Max(prog.MaxStage, _run.Stage);
                    prog.LunarCoins = Math.Max(prog.LunarCoins, _run.LunarCoins);
                    if (victory)
                    {
                        prog.MithrixDefeated = true;
                        prog.Victories++;
                        prog.LunarCoins += 10;
                        _run.UnlockAchievement("ach_safe_travels");
                        _run.UnlockAchievement("ach_stage4");
                    }
                    prog.Save();
                }
            }
            QuitRunToLobby(toMainMenu: false);
            _screen = "run_summary";
            if (!victory)
            {
                _summaryShowKiller3D = true;
                ShowKillerOnDiorama();
            }
            else
            {
                _summaryShowKiller3D = false;
            }
        }

        public static void OnPlayerDefeated()
        {
            SoundManager.Play("player_death");
            if (Physics.SpecialMode == "pvp" && _net.Connected)
            {
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "pvp_eliminated",
                    ["player_id"] = _net.MyId
                });
                _isSpectating = false;
                FinishRunWithSummary(victory: false);
                ShowBanner("ВЫ ПАЛИ НА АРЕНЕ PvP! МАТЧ ОКОНЧЕН", Color.Red, 5.5f);
                return;
            }

            if (_net.Connected && _remotePlayers.Count > 0)
            {
                _deadCoopPlayers.Add(_net.MyId);
                if (_run != null) _run.PlayerHp = 0f;
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "player_dead",
                    ["player_id"] = _net.MyId
                });
                bool anyAlive = false;
                foreach (var kv in _remotePlayers)
                {
                    if (!_deadCoopPlayers.Contains(kv.Key) && IsValid(kv.Value) && kv.Value.Hp > 0f)
                    {
                        anyAlive = true;
                        break;
                    }
                }
                if (anyAlive && _run != null)
                {
                    _isSpectating = true;
                    _run.PlayerHp = 0f;
                    _run.Parts.Root.Enabled = false;
                    if (_run.Combat.FpRoot != null) _run.Combat.FpRoot.Enabled = false;
                    ShowBanner("ВЫ ПОГИБЛИ! РЕЖИМ НАБЛЮДАТЕЛЯ [A/D или ЛКМ — Сменить игрока]", Color.Orange, 4.5f);
                    return;
                }
                else
                {
                    _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "all_players_dead"
                    });
                }
            }
            _isSpectating = false;
            FinishRunWithSummary(victory: false);
            ShowBanner("ВЫ ПОГИБЛИ! ЗАБЕГ ОКОНЧЕН", Color.Red, 5.5f);
        }

        public static void OnVictory()
        {
            SoundManager.Play("victory");
            if (_net.Connected)
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject { ["type"] = "run_victory" });
            FinishRunWithSummary(victory: true);
            ShowBanner("ПОБЕДА! ВЫ ПОКИНУЛИ ЛУНУ НА СПАСАТЕЛЬНОМ КОРАБЛЕ (+10 ЛУННЫХ МОНЕТ)!", Color.Gold, 8.0f);
        }

        private static void QuitRunToLobby(bool toMainMenu = false)
        {
            _paused = false;
            CloseSafeTravelsDialogueModal();
            _rpsOpen = false;
            _emoteWheelOpen = false;
            _safeTravelsWardrobeOpen = false;
            StopCharacterUnlockCutscene();
            EnsureLocalChairProp(false);
            if (_run != null)
            {
                foreach (var e in CombatManager.Enemies.ToArray()) Destroy(e);
                CombatManager.Enemies.Clear();
                foreach (var rp in _remotePlayers.Values) Destroy(rp);
                _remotePlayers.Clear();
                foreach (var e in _run.StageEntities.ToArray()) Destroy(e);
                _run.StageEntities.Clear();
                Destroy(_run.PlayerEntity);
                _run = null;
            }
            CombatManager.Controller = null;
            WorldContext.Game = null;
            Raylib.EnableCursor();
            Engine.BackgroundColor = Rgb(18, 20, 32);
            _mainPedestal.Enabled = true;
            _menuCrystal.Enabled = true;
            _myReady = false;
            _lobbyCountdownActive = false;
            if (toMainMenu)
            {
                _net.StopSession();
                _lobbyPlayers.Clear();
                _screen = "main";
            }
            else
            {
                _screen = "lobby";
                BroadcastLobbyState();
            }
            if (!ProgressData.Instance.IsCharacterUnlocked(_selectedChar))
                _selectedChar = "commando";
            SelectCharacter(_selectedChar);
        }

        private static void RecordSkillCast(string slot)
        {
            if (_run == null) return;
            if (!string.IsNullOrEmpty(_run.Parts.ActiveEmote) && !_rpsOpen)
                SetLocalPlayerEmote("");

            if (_run.IsSafeTravels && _run.ZeroCooldownSandbox && _run.Combat != null)
            {
                _run.Combat.M1Cd = MathF.Min(_run.Combat.M1Cd, 0.08f);
                _run.Combat.M2Cd = 0f;
                _run.Combat.ShiftCd = 0f;
                _run.Combat.RCd = 0f;
            }
            if (_run.IsSafeTravels) return;

            if (slot == "m1") _run.M1CastCount++;
            else if (slot == "m2") _run.M2CastCount++;
            else if (slot == "shift") _run.ShiftCastCount++;
            else if (slot == "r") _run.RCastCount++;

            if (_run.M1CastCount >= 35) _run.UnlockAchievement("ach_bleeder");
            if (_run.M1CastCount >= 75) _run.UnlockAchievement("ach_lightning_strike");
            if (_run.M2CastCount >= 15) _run.UnlockAchievement("ach_secondary_master");
            if (_run.ShiftCastCount >= 12) _run.UnlockAchievement("ach_combat_dash");
            if (_run.RCastCount >= 5) _run.UnlockAchievement("ach_skill_spam");
            int totalCasts = _run.M1CastCount + _run.M2CastCount + _run.ShiftCastCount + _run.RCastCount;
            if (totalCasts >= 30) _run.UnlockAchievement("ach_arcane_burst");
            if (totalCasts >= 60) _run.UnlockAchievement("ach_cosmic_mind");
        }

        private static void UpdateInGame(float dt)
        {
            if (_run == null) return;

            if (UpdateCharacterUnlockCutscene(dt))
                return;
            TickPostCutsceneGrace(dt);

            if (_run.IsSafeTravels && Raylib.IsKeyPressed(KeyboardKey.H) && !_dialogueOpen && !_rpsOpen && !_emoteWheelOpen)
            {
                _safeTravelsWardrobeOpen = !_safeTravelsWardrobeOpen;
                SoundManager.Play("ui_click");
                RestoreInGameCursorIfNeeded();
            }

            UpdateSafeTravelsDialogue(dt);
            UpdateRpsMiniGame(dt);
            UpdateEmoteWheelAndActiveEmote(dt);

            _run.ElapsedTime += dt;
            if (!_run.IsSafeTravels)
            {
                _run.StageElapsedTime += dt;
                if (_run.ElapsedTime >= 300f) _run.UnlockAchievement("ach_time_keeper");
                if (_run.PlayerMaxHp >= 180f) _run.UnlockAchievement("ach_bison_hunt");
                if (_run.PlayerMaxHp >= 260f) _run.UnlockAchievement("ach_immortal_titan");
                if (_run.PlayerHp <= _run.PlayerMaxHp * 0.40f) _run.DroppedLowHp = true;
                if (_run.BlueOrbSpawned) _run.UnlockAchievement("ach_lunar_stone");
                if (_run.MultiKillWindowTimer > 0f) _run.MultiKillWindowTimer -= dt;
                if (_run.FrenzyWindowTimer > 0f) _run.FrenzyWindowTimer -= dt;
                if (_run.PostHitTimer > 0f) _run.PostHitTimer -= dt;
                if (_run.TeleporterChargingNow) _run.TeleporterActiveTimer += dt;
                if (CombatManager.Enemies.Count > 0)
                {
                    _run.UntouchedCombatTimer += dt;
                    if (_run.UntouchedCombatTimer >= 35f) _run.UnlockAchievement("ach_untouched");
                }
            }

            float coeff = _run.DifficultyCoeff;
            CombatManager.Enemies.RemoveAll(e => !IsValid(e));

            if (_run.MoonDetonationTimer > 0f)
            {
                _run.MoonDetonationTimer -= dt;
                if (_run.MoonDetonationTimer <= 0f)
                {
                    FinishRunWithSummary(victory: false);
                    RecordPlayerDamageSource(null, 9999f, "moon_detonation");
                    ShowKillerOnDiorama();
                    ShowBanner("ЛУНА ДЕТОНИРОВАЛА! ВЫ НЕ УСПЕЛИ К СПАСАТЕЛЬНОМУ КОРАБЛЮ!", Color.Red, 5.0f);
                    return;
                }
            }

            // ИИ-Режиссёр: спавнит мобов до полной зарядки Телепорта, а после зарядки ("ready") останавливается для передышки
            bool tpActivated = _run.ActiveTeleporterOrPad is Teleporter tpCheck && tpCheck.State == "ready";
            if (_net.Mode != "client" && Physics.SpecialMode is not "bazaar" and not "safe_travels" && !_run.MithrixSpawned && !tpActivated)
            {
                int playerCount = 1 + _remotePlayers.Count;
                float playerMult = 1.0f + 0.55f * (playerCount - 1);
                int maxEnemies = 36 + (playerCount - 1) * 14 + (GameData.HasArtifact("swarm") ? 18 : 0);

                // Артефакт Мести: вторжение теневого двойника каждые 85 секунд
                if (GameData.HasArtifact("vengeance"))
                {
                    _run.VengeanceTimer -= dt;
                    if (_run.VengeanceTimer <= 0f)
                    {
                        _run.VengeanceTimer = 85.0f;
                        var rngV = new Random();
                        float vang = (float)rngV.NextDouble() * MathF.PI * 2f;
                        Vector3 vPos = Physics.FindFreeActorPos(new Vector3(
                            _run.PlayerPos.X + MathF.Cos(vang) * 16f, _run.PlayerPos.Y, _run.PlayerPos.Z + MathF.Sin(vang) * 16f), 0.9f, 10f, 5f);
                        float vx = vPos.X, vy = vPos.Y, vz = vPos.Z;
                        var doppel = new CombatTarget(new Vector3(vx, vy, vz), "imp", coeff * 1.15f);
                        doppel.MakeDoppelganger();
                        CombatManager.Enemies.Add(doppel);
                        _run.StageEntities.Add(doppel);
                        ShowBanner("АРТЕФАКТ МЕСТИ :: ВАШ ТЕНЕВОЙ ДВОЙНИК ВТОРГСЯ В ЭТОТ МИР!", Rgb(185, 90, 255), 4.2f);
                        if (_net.Connected)
                        {
                            _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                            {
                                ["type"] = "enemy_spawn",
                                ["id"] = doppel.NetId,
                                ["et"] = "imp",
                                ["coeff"] = MathF.Round(coeff * 1.15f, 3),
                                ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(vx, 2), MathF.Round(vy, 2), MathF.Round(vz, 2)),
                                ["boss"] = false,
                                ["doppel"] = true
                            });
                        }
                    }
                }

                _run.DirectorCredits += (6.5f * coeff * playerMult) * dt;
                _run.SpawnTimer -= dt;

                if (_run.SpawnTimer <= 0f && CombatManager.Enemies.Count < maxEnemies)
                {
                    var rng = new Random();
                    _run.SpawnTimer = (0.9f + (float)rng.NextDouble() * 0.9f) / MathF.Sqrt(playerCount);
                    (string et, float cost)[] table = Physics.SpecialMode switch
                    {
                        "moon" => new[] { ("lunar_chimera", 18f), ("brass_contraption", 22f), ("greater_wisp", 26f), ("golem", 28f), ("parent", 34f) },
                        "hell_cybergrind" => new[] { ("hell_drone", 15f), ("wisp", 16f), ("imp", 20f), ("nether_blaze", 24f), ("greater_wisp", 28f) },
                        "black_mesa" => new[] { ("combine_soldier", 18f), ("hell_drone", 16f), ("brass_contraption", 24f), ("golem", 30f) },
                        "qliphoth" => new[] { ("hollow_shadow", 17f), ("imp", 20f), ("sarkaz_mercenary", 22f), ("elder_lemurian", 40f) },
                        "tartarus" => new[] { ("hollow_shadow", 16f), ("wisp", 16f), ("lunar_chimera", 22f), ("parent", 36f) },
                        "hakurei_shrine" => new[] { ("wisp", 14f), ("hollow_shadow", 18f), ("greater_wisp", 26f), ("nether_blaze", 24f) },
                        "tracen_derby" => new[] { ("bison", 20f), ("sarkaz_mercenary", 20f), ("lemurian", 14f), ("golem", 32f) },
                        "pizzeria" => new[] { ("hollow_shadow", 16f), ("combine_soldier", 18f), ("brass_contraption", 24f) },
                        _ => _run.Stage >= 4
                            ? new[]
                            {
                                ("beetle", 10f), ("lemurian", 12f), ("jellyfish", 14f), ("wisp", 16f),
                                ("hell_drone", 18f), ("combine_soldier", 20f), ("sarkaz_mercenary", 21f),
                                ("imp", 22f), ("hollow_shadow", 23f), ("bison", 24f), ("nether_blaze", 25f),
                                ("brass_contraption", 26f), ("greater_wisp", 30f), ("golem", 34f),
                                ("parent", 40f), ("elder_lemurian", 45f)
                            }
                            : new[]
                            {
                                ("beetle", 10f), ("lemurian", 12f), ("jellyfish", 14f), ("wisp", 16f),
                                ("imp", 22f), ("bison", 24f), ("brass_contraption", 26f),
                                ("greater_wisp", 30f), ("golem", 34f), ("parent", 40f), ("elder_lemurian", 45f)
                            }
                    };

                    int packLimit = rng.Next(playerCount, 4 + playerCount * 2); // Размер пачки растёт от числа игроков
                    if (GameData.HasArtifact("dissonance"))
                    {
                        table = new[]
                        {
                            ("beetle", 10f), ("lemurian", 12f), ("jellyfish", 14f), ("wisp", 16f),
                            ("lunar_chimera", 18f), ("hell_drone", 18f), ("combine_soldier", 20f),
                            ("sarkaz_mercenary", 21f), ("imp", 22f), ("hollow_shadow", 23f),
                            ("bison", 24f), ("nether_blaze", 25f), ("brass_contraption", 26f),
                            ("greater_wisp", 30f), ("golem", 34f), ("parent", 40f), ("elder_lemurian", 45f)
                        };
                    }
                    if (GameData.HasArtifact("kin") && !string.IsNullOrEmpty(_run.StageKinType))
                    {
                        table = new[] { (_run.StageKinType, 16f) };
                    }
                    for (int s = 0; s < packLimit && CombatManager.Enemies.Count < maxEnemies; s++)
                    {
                        var affordable = new List<(string et, float cost)>();
                        foreach (var pair in table)
                            if (_run.DirectorCredits >= pair.cost) affordable.Add(pair);

                        if (affordable.Count == 0) break;

                        var chosen = affordable[rng.Next(affordable.Count)];
                        _run.DirectorCredits -= chosen.cost;

                        // Спавним врагов рядом с любым из активных игроков (хостом или гостем)
                        Vector3 anchorPos = _run.PlayerPos;
                        if (_remotePlayers.Count > 0 && rng.Next(2) == 0)
                        {
                            foreach (var rp in _remotePlayers.Values)
                            {
                                if (IsValid(rp)) { anchorPos = rp.Position; break; }
                            }
                        }

                        float ang = (float)rng.NextDouble() * MathF.PI * 2f;
                        float dist = 15.0f + (float)rng.NextDouble() * 13.0f;
                        // Монстры появляются только на свободной земле: не внутри скал, колонн, стен, мебели и за внешней оградой
                        Vector3 spawnBase = Physics.FindFreeActorPos(new Vector3(
                            anchorPos.X + MathF.Cos(ang) * dist, anchorPos.Y, anchorPos.Z + MathF.Sin(ang) * dist), 1.0f, 10f, 4f);
                        float sy = spawnBase.Y;
                        int swarmSpawnCount = GameData.HasArtifact("swarm") ? 2 : 1;
                        for (int swi = 0; swi < swarmSpawnCount && CombatManager.Enemies.Count < maxEnemies; swi++)
                        {
                            Vector3 swPos = swi == 0 ? spawnBase
                                : Physics.FindFreeActorPos(spawnBase + new Vector3(swi * 1.6f, 0f, -swi * 1.6f), 1.0f, 6f, 4f);
                            float osx = swPos.X, osy = swPos.Y, osz = swPos.Z;
                            var ne = new CombatTarget(new Vector3(osx, osy, osz), chosen.et, coeff);
                            CombatManager.Enemies.Add(ne);
                            _run.StageEntities.Add(ne);

                            if (_net.Connected)
                            {
                                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                                {
                                    ["type"] = "enemy_spawn",
                                    ["id"] = ne.NetId,
                                    ["et"] = chosen.et,
                                    ["coeff"] = MathF.Round(coeff, 3),
                                    ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(osx, 2), MathF.Round(osy, 2), MathF.Round(osz, 2)),
                                    ["boss"] = false,
                                    ["elite"] = ne.IsElite
                                });
                            }
                        }
                    }
                }
            }

            var p = _run.PlayerEntity;
            var c = _run.Combat;

            if (_isSpectating)
            {
                var aliveList = new List<RemotePlayer>();
                foreach (var kv in _remotePlayers)
                    if (!_deadCoopPlayers.Contains(kv.Key) && IsValid(kv.Value) && kv.Value.Hp > 0f)
                        aliveList.Add(kv.Value);

                if (aliveList.Count == 0)
                {
                    _isSpectating = false;
                    FinishRunWithSummary(victory: false);
                    ShowBanner("ВСЕ ИГРОКИ ПОГИБЛИ! ЗАБЕГ ОКОНЧЕН", Color.Red, 5.0f);
                    return;
                }

                if (Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsMouseButtonPressed(MouseButton.Left))
                    _spectateTargetIdx = (_spectateTargetIdx - 1 + aliveList.Count) % aliveList.Count;
                if (Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsMouseButtonPressed(MouseButton.Right))
                    _spectateTargetIdx = (_spectateTargetIdx + 1) % aliveList.Count;

                Vector2 mdSpec = Raylib.GetMouseDelta();
                p.RotationY += mdSpec.X * _mouseSens;
                _run.CamPitch = Math.Clamp(_run.CamPitch + mdSpec.Y * _mouseSens, -40f, 45f);

                var specTarget = aliveList[_spectateTargetIdx % aliveList.Count];
                p.Position = specTarget.Position;
                Vector3 lookDir = c.GetAimDirection();
                Vector3 pivot = specTarget.Position + new Vector3(0, 2.4f, 0);
                Vector3 camPos = Physics.ResolveCameraCollision(specTarget.Position + new Vector3(0, 1.85f, 0), pivot, pivot - lookDir * 9.5f, 0.48f);
                Engine.Camera.Position = new Vector3(-camPos.X, MathF.Max(1.0f, camPos.Y), camPos.Z);
                Vector3 lookAt = pivot + lookDir * 8.0f;
                Engine.Camera.Target = new Vector3(-lookAt.X, lookAt.Y, lookAt.Z);
                return;
            }

            float prevY = p.Y;
            bool isMoba = SurvivorDatabase.IsMobaCharacter(_run.CharKey);
            bool isMoving = false;
            float animStrafe = 0f;
            float animFwd = 0f;
            bool modalOpen = HasAnyInGameModalOpen();
            bool nearHubNpc = _run.IsSafeTravels && SafeTravelsNpc.IsPlayerNearAnyHubNpc(_run.PlayerPos);

            if (isMoba)
            {
                if (Raylib.IsCursorHidden())
                    Raylib.EnableCursor();

                Ray rlRay = Raylib.GetMouseRay(Raylib.GetMousePosition(), Engine.Camera);
                Vector3 rayOrig = new(-rlRay.Position.X, rlRay.Position.Y, rlRay.Position.Z);
                Vector3 rayDir = new(-rlRay.Direction.X, rlRay.Direction.Y, rlRay.Direction.Z);
                if (!modalOpen && MathF.Abs(rayDir.Y) > 0.001f)
                {
                    float tHit = (p.Y - rayOrig.Y) / rayDir.Y;
                    if (tHit > 0f && tHit < 220f)
                    {
                        Vector3 hitP = rayOrig + rayDir * tHit;
                        float wb = Physics.WorldBound;
                        c.MobaCursorPos = new Vector3(
                            Math.Clamp(hitP.X, -wb, wb),
                            Physics.GetSurfaceHeight(hitP.X, hitP.Z, p.Y),
                            Math.Clamp(hitP.Z, -wb, wb)
                        );
                    }
                }

                if (!modalOpen && Raylib.IsMouseButtonPressed(MouseButton.Right))
                {
                    CombatTarget? clickedEnemy = null;
                    float bestD = 2.6f;
                    foreach (var e in CombatManager.Enemies)
                    {
                        if (IsValid(e) && !e.IsAlly)
                        {
                            float d = Physics.Distance2D(c.MobaCursorPos, e.Position);
                            if (d <= e.HitRadius + bestD) { bestD = d; clickedEnemy = e; }
                        }
                    }
                    if (clickedEnemy != null)
                    {
                        c.MobaAttackTarget = clickedEnemy;
                        c.MobaMoveTarget = null;
                        new VisualFX(clickedEnemy.Position + new Vector3(0, 0.25f, 0), Color.Red, 1.6f, 0.20f);
                    }
                    else
                    {
                        c.MobaAttackTarget = null;
                        c.MobaMoveTarget = c.MobaCursorPos;
                        new VisualFX(c.MobaCursorPos + new Vector3(0, 0.15f, 0), Color.Lime, 1.3f, 0.20f);
                    }
                }
                else if (!modalOpen && Raylib.IsMouseButtonDown(MouseButton.Right) && c.MobaAttackTarget == null)
                {
                    c.MobaMoveTarget = c.MobaCursorPos;
                }

                if (modalOpen)
                {
                    c.MobaMoveTarget = null;
                    c.MobaAttackTarget = null;
                }

                Vector3 moveDir = Vector3.Zero;
                if (IsValid(c.MobaAttackTarget) && !c.MobaAttackTarget!.IsAlly)
                {
                    Vector3 toEnemy = new(c.MobaAttackTarget.X - p.X, 0f, c.MobaAttackTarget.Z - p.Z);
                    float distE = toEnemy.Length();
                    if (distE > 3.6f)
                        moveDir = toEnemy / distE;
                    else
                    {
                        p.LookAt2D(c.MobaAttackTarget.Position);
                        if (c.M1Cd <= 0f) { c.UseM1Skill(); RecordSkillCast("m1"); }
                    }
                }
                else if (c.MobaMoveTarget.HasValue)
                {
                    Vector3 toPoint = new(c.MobaMoveTarget.Value.X - p.X, 0f, c.MobaMoveTarget.Value.Z - p.Z);
                    if (toPoint.Length() > 0.45f)
                        moveDir = Vector3.Normalize(toPoint);
                    else
                        c.MobaMoveTarget = null;
                }

                isMoving = moveDir.LengthSquared() > 0.01f;
                if (isMoving && !c.GrappleActive)
                {
                    p.Position += moveDir * c.MoveSpeed * dt;
                    p.LookAt2D(p.Position + moveDir);
                    animFwd = 1f;
                }

                c.Update(dt, isMoving);
                if (!modalOpen)
                {
                    if ((Raylib.IsMouseButtonDown(MouseButton.Left) || Raylib.IsKeyDown(KeyboardKey.Q)) && c.M1Cd <= 0f)
                    {
                        p.LookAt2D(c.MobaCursorPos);
                        c.UseM1Skill();
                        RecordSkillCast("m1");
                    }
                    if ((Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsMouseButtonPressed(MouseButton.Middle)) && c.M2Cd <= 0f)
                    {
                        p.LookAt2D(c.MobaCursorPos);
                        c.UseM2Skill();
                        RecordSkillCast("m2");
                    }
                    if (Raylib.IsKeyPressed(KeyboardKey.LeftShift) && c.ShiftCd <= 0f)
                    {
                        c.MobaMoveTarget = null;
                        p.LookAt2D(c.MobaCursorPos);
                        c.UseShiftSkill();
                        RecordSkillCast("shift");
                    }
                    if (!nearHubNpc && Raylib.IsKeyPressed(KeyboardKey.R) && c.RCd <= 0f)
                    {
                        p.LookAt2D(c.MobaCursorPos);
                        c.UseRSkill();
                        RecordSkillCast("r");
                    }
                }
            }
            else
            {
                Vector2 md = modalOpen ? Vector2.Zero : Raylib.GetMouseDelta();
                bool emotingFreeCam = !string.IsNullOrEmpty(_run.Parts.ActiveEmote) && !_rpsOpen && !_dialogueOpen;
                if (c.MedicTauntTimer <= 0f && !modalOpen)
                {
                    if (emotingFreeCam)
                        _emoteCamYawOffset += md.X * _mouseSens;
                    else
                        p.RotationY += md.X * _mouseSens;
                }
                bool isFp = SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey) && c.MedicTauntTimer <= 0f && string.IsNullOrEmpty(_run.Parts.ActiveEmote) && !_dialogueOpen;
                float maxPitch = isFp ? 82f : 48f;
                float minPitch = isFp ? -82f : -42f;
                _run.CamPitch = Math.Clamp(_run.CamPitch + md.Y * _mouseSens, minPitch, maxPitch);

                float inX = (Raylib.IsKeyDown(KeyboardKey.D) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.A) ? 1f : 0f);
                float inZ = (Raylib.IsKeyDown(KeyboardKey.W) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.S) ? 1f : 0f);
                animStrafe = inX;
                animFwd = inZ;
                isMoving = !modalOpen && (MathF.Abs(inX) > 0.01f || MathF.Abs(inZ) > 0.01f) && c.MedicTauntTimer <= 0f;

                if (isMoving && !c.GrappleActive)
                {
                    Vector3 moveDir = _run.CharKey == "mindustry"
                        ? Vector3.Normalize(new Vector3(inX, 0f, inZ))
                        : Vector3.Normalize(p.Forward * inZ + p.Right * inX);
                    p.Position += moveDir * c.MoveSpeed * dt;
                }

                c.Update(dt, isMoving);
                if (c.MedicTauntTimer <= 0f && !modalOpen)
                {
                    bool mouseOverBuildMenu = _run.CharKey == "mindustry" &&
                        Raylib.GetMouseX() >= Raylib.GetScreenWidth() - 525 &&
                        Raylib.GetMouseY() >= Raylib.GetScreenHeight() - 285;

                    if (!mouseOverBuildMenu && (_run.CharKey == "mindustry" && c.MindustryBuildMode && c.MindustrySelectedBuild >= 0
                        ? Raylib.IsMouseButtonPressed(MouseButton.Left)
                        : Raylib.IsMouseButtonDown(MouseButton.Left)) && c.M1Cd <= 0f)
                    {
                        c.UseM1Skill();
                        RecordSkillCast("m1");
                    }
                    if ((_run.CharKey is "sol" or "mindustry" or "freeman" ? Raylib.IsMouseButtonPressed(MouseButton.Right) : Raylib.IsMouseButtonDown(MouseButton.Right)) && c.M2Cd <= 0f)
                    {
                        c.UseM2Skill();
                        RecordSkillCast("m2");
                    }
                    if (Raylib.IsKeyPressed(KeyboardKey.LeftShift) && c.ShiftCd <= 0f)
                    {
                        c.UseShiftSkill();
                        RecordSkillCast("shift");
                    }
                    if (!nearHubNpc && Raylib.IsKeyPressed(KeyboardKey.R) && c.RCd <= 0f)
                    {
                        c.UseRSkill();
                        RecordSkillCast("r");
                    }
                }
            }

            if (isMoving || c.GrappleActive)
            {
                if (!string.IsNullOrEmpty(_run.Parts.ActiveEmote) && !_rpsOpen)
                    SetLocalPlayerEmote("");
                _run.MoveStreakTimer += dt;
                if (!_run.IsSafeTravels && _run.MoveStreakTimer >= 20f) _run.UnlockAchievement("ach_marathon");
            }
            else
            {
                _run.MoveStreakTimer = 0f;
            }
            if (!_run.IsSafeTravels && c.StationaryTimer >= 12f) _run.UnlockAchievement("ach_stationary");
            if (!_run.IsSafeTravels && c.MoveSpeed >= c.Cfg.Speed * 1.65f) _run.UnlockAchievement("ach_supersonic");

            float floorH = Physics.GetSurfaceHeight(p.X, p.Z, p.Y, prevY);

            if (_run.CharKey == "mindustry")
            {
                if (Raylib.IsCursorHidden()) Raylib.EnableCursor();
                c.MindustryOverdriveTimer = MathF.Max(0f, c.MindustryOverdriveTimer - dt);

                Ray rlRay = Raylib.GetMouseRay(Raylib.GetMousePosition(), Engine.Camera);
                Vector3 rayOrig = new(-rlRay.Position.X, rlRay.Position.Y, rlRay.Position.Z);
                Vector3 rayDir = new(-rlRay.Direction.X, rlRay.Direction.Y, rlRay.Direction.Z);
                if (MathF.Abs(rayDir.Y) > 0.001f)
                {
                    float groundRefY = Physics.GetSurfaceHeight(p.X, p.Z, p.Y);
                    float tHit = (groundRefY - rayOrig.Y) / rayDir.Y;
                    if (tHit > 0f && tHit < 260f)
                    {
                        Vector3 hitP = rayOrig + rayDir * tHit;
                        float wb = Physics.WorldBound;
                        c.MobaCursorPos = new Vector3(
                            Math.Clamp(hitP.X, -wb, wb),
                            Physics.GetSurfaceHeight(hitP.X, hitP.Z, groundRefY + 4f),
                            Math.Clamp(hitP.Z, -wb, wb)
                        );
                    }
                }
                p.LookAt2D(c.MobaCursorPos);

                if (Raylib.IsKeyPressed(KeyboardKey.B))
                    c.ToggleMindustryMode();

                if (c.MindustryBuildMode)
                {
                    KeyboardKey[] buildKeys = { KeyboardKey.One, KeyboardKey.Two, KeyboardKey.Three, KeyboardKey.Four, KeyboardKey.Five, KeyboardKey.Six, KeyboardKey.Seven, KeyboardKey.Eight };
                    for (int bi = 0; bi < buildKeys.Length; bi++)
                        if (Raylib.IsKeyPressed(buildKeys[bi])) c.MindustrySelectedBuild = bi;
                }

                float targetHoverY = c.MindustryBuildMode
                    ? (p.Y >= 105f ? Physics.ColosseumY + 14.5f : MathF.Min(floorH + 16.5f, Physics.ColosseumY - 18.0f))
                    : floorH + 0.85f;
                p.Y = Lerp(p.Y, targetHoverY, dt * 7.5f);
                _run.PlayerVelY = 0f;
                _run.PlayerGrounded = !c.MindustryBuildMode;
            }
            else if (!c.GrappleActive)
            {
                if (_run.DashTimer > 0f)
                {
                    p.Position += _run.DashVector * dt;
                    _run.DashTimer -= dt;
                }

                if (!HasAnyInGameModalOpen() && Raylib.IsKeyPressed(KeyboardKey.Space) && _run.PlayerJumpsLeft > 0)
                {
                    if (!string.IsNullOrEmpty(_run.Parts.ActiveEmote))
                        SetLocalPlayerEmote("");
                    SoundManager.Play(_run.PlayerGrounded ? "jump" : "double_jump");
                    _run.PlayerVelY = 12.0f;
                    _run.PlayerGrounded = false;
                    _run.PlayerJumpsLeft--;
                    int quill = CombatManager.GetItemCount("wax_quill");
                    if (quill > 0 && isMoving)
                        _run.ApplyDash(p.Forward * (14.0f + 4.0f * quill), 0.16f);
                    new VisualFX(p.Position, Color.SkyBlue, 1.8f, 0.22f);
                }

                if (isMoving && _run.PlayerGrounded)
                {
                    SoundManager.Play("footstep", 0.50f);
                }

                if (_run.CharKey == "reimu" && !_run.PlayerGrounded && !c.ReimuFlightBroken && c.ReimuFlightFuel > 0f && Raylib.IsKeyDown(KeyboardKey.Space) && _run.PlayerVelY <= 4.0f)
                {
                    c.ReimuIsFlying = true;
                    c.ReimuFlightFuel = MathF.Max(0f, c.ReimuFlightFuel - dt);
                    bool descend = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.C);
                    float targetVy = descend ? -8.5f : 6.5f;
                    _run.PlayerVelY = Lerp(_run.PlayerVelY, targetVy, dt * 10f);
                }
                else
                {
                    c.ReimuIsFlying = false;
                    _run.PlayerVelY -= 26.0f * dt;
                }

                p.Y += _run.PlayerVelY * dt;
                if (p.Y <= floorH)
                {
                    p.Y = floorH;
                    _run.PlayerVelY = 0f;
                    _run.PlayerGrounded = true;
                    _run.PlayerJumpsLeft = _run.PlayerMaxJumps;
                    c.ReimuFlightBroken = false;
                    c.ReimuFlightFuel = MathF.Min(4.5f, c.ReimuFlightFuel + 1.8f * dt);
                }
            }

            if (c.MedicTauntTimer <= 0f)
            {
                float spdRatio = c.MoveSpeed / MathF.Max(1f, c.Cfg.Speed);
                _run.Parts.UpdateAnimations(
                    _run.CharKey, dt, isMoving, _run.PlayerGrounded,
                    _run.PlayerVelY, spdRatio, animStrafe, animFwd,
                    c.ShieldStance, c.ReimuIsFlying, c.OrgiaTimer > 0f);
            }

            // Пошаговая трассировка от позиции прошлого кадра: рывки, блинки, крюки и скоростные навыки больше не
            // «просачиваются» сквозь стены, утёсы и колонны. Явные телепорты (смена этапа, лунный трамплин, возрождение)
            // распознаются по большому скачку и не трассируются.
            Vector3 resolved;
            Vector3 sweepDelta = p.Position - _lastResolvedPlayerPos;
            float sweepDist2D = MathF.Sqrt(sweepDelta.X * sweepDelta.X + sweepDelta.Z * sweepDelta.Z);
            if (_playerSweepValid && sweepDist2D <= 40f && MathF.Abs(sweepDelta.Y) <= 10f)
                resolved = Physics.SweepPlayerMovement(_lastResolvedPlayerPos, p.Position, 0.72f, p.Y);
            else
                resolved = Physics.ResolveHorizontalCollision(p.Position, 0.72f, p.Y);
            float b = Physics.WorldBound;
            p.X = Math.Clamp(resolved.X, -b, b);
            p.Z = Math.Clamp(resolved.Z, -b, b);
            _lastResolvedPlayerPos = p.Position;
            _playerSweepValid = true;

            if (Physics.SpecialMode == "moon" && _run.MithrixSpawned && _run.IsBossAlive && p.Y >= 105.0f)
            {
                float arenaR = MathF.Sqrt(p.X * p.X + p.Z * p.Z);
                if (arenaR > 52.5f)
                {
                    float ang = MathF.Atan2(p.Z, p.X);
                    p.X = MathF.Cos(ang) * 52.5f;
                    p.Z = MathF.Sin(ang) * 52.5f;
                }
                if (p.Y < 120.0f)
                {
                    p.Y = 120.0f;
                    _run.PlayerVelY = 0f;
                    _run.PlayerGrounded = true;
                    _run.PlayerJumpsLeft = _run.PlayerMaxJumps;
                }
            }

            if (_net.Connected)
            {
                _netSyncTimer -= dt;
                if (_netSyncTimer <= 0f)
                {
                    _netSyncTimer = 0.05f;
                    _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "player_state",
                        ["player_id"] = _net.MyId,
                        ["char_key"] = _run.CharKey,
                        ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(p.X, 2), MathF.Round(p.Y, 2), MathF.Round(p.Z, 2)),
                        ["rot_y"] = MathF.Round(p.RotationY, 1),
                        ["hp"] = MathF.Round(_run.PlayerHp, 1),
                        ["max_hp"] = _run.PlayerMaxHp,
                        ["moving"] = isMoving,
                        ["ult_form"] = _run.Parts.UltFormActive,
                        ["skin_idx"] = Data.ProgressData.Instance.GetSelectedSkin(_run.CharKey),
                        ["grounded"] = _run.PlayerGrounded,
                        ["vel_y"] = MathF.Round(_run.PlayerVelY, 1),
                        ["shield_stance"] = c.ShieldStance,
                        ["emote"] = _run.Parts.ActiveEmote,
                        ["anim_slot"] = _run.Parts.ActiveAnim,
                        ["anim_step"] = _run.Parts.AnimStep
                    });
                }

                if (_net.Mode == "host")
                {
                    _enemySyncTimer -= dt;
                    if (_enemySyncTimer <= 0f)
                    {
                        _enemySyncTimer = 0.12f;
                        var arr = new System.Text.Json.Nodes.JsonArray();
                        foreach (var e in CombatManager.Enemies)
                        {
                            if (!IsValid(e)) continue;
                            arr.Add(new System.Text.Json.Nodes.JsonObject
                            {
                                ["id"] = e.NetId,
                                ["et"] = e.EnemyType,
                                ["c"] = MathF.Round(e.Coeff, 2),
                                ["hp"] = MathF.Round(e.Hp, 1),
                                ["x"] = MathF.Round(e.X, 2),
                                ["y"] = MathF.Round(e.Y, 2),
                                ["z"] = MathF.Round(e.Z, 2),
                                ["b"] = e.IsBoss,
                                ["a"] = e.IsAlly,
                                ["el"] = e.IsElite,
                                ["dp"] = e.IsDoppelganger
                            });
                        }
                        _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                        {
                            ["type"] = "enemies_sync",
                            ["list"] = arr
                        });
                    }
                }
            }

            Engine.Camera.FovY = _fov;
            Vector3 camLookDir = c.GetAimDirection();
            bool isEmotingNow = !string.IsNullOrEmpty(_run.Parts.ActiveEmote);
            bool inSideShotScene = (_dialogueOpen && IsValid(_dialogueNpcEntity)) || (_rpsOpen && IsValid(_rpsOppEntity));

            if (inSideShotScene)
            {
                // Диалоговая / дуэльная КНБ камера плавно обновляется в UpdateSafeTravelsDialogue / UpdateRpsMiniGame
            }
            else if (isEmotingNow)
            {
                // Во время любой эмоции включаем обзор от 3-го лица спереди-сбоку с вращением мышью (даже для V1, Фримана и Mindustry)
                float yawRad = (p.RotationY + 12f + _emoteCamYawOffset) * (MathF.PI / 180f);
                Vector3 orbitDir = new(MathF.Sin(yawRad), 0f, MathF.Cos(yawRad));
                Vector3 pivot = p.Position + new Vector3(0f, _run.CharKey == "mindustry" ? 0.85f : 1.50f, 0f);
                float dist = _run.CharKey == "mindustry" ? 6.8f : 4.6f;
                Vector3 desired = pivot + orbitDir * dist + new Vector3(0f, 1.05f, 0f);
                Vector3 camPos = Physics.ResolveCameraCollision(pivot, pivot, desired, 0.42f);
                float camFloor = Physics.GetSurfaceHeight(camPos.X, camPos.Z, camPos.Y) + 0.45f;
                if (camPos.Y < camFloor) camPos.Y = camFloor;
                Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                Engine.Camera.Target = new Vector3(-pivot.X, pivot.Y, pivot.Z);
            }
            else if (_run.CharKey == "mindustry")
            {
                // Полный вид сверху вниз (как в Mindustry)
                float camHeight = c.MindustryBuildMode ? 38.0f : 28.0f;
                Vector3 camPos = new(p.X, p.Y + camHeight, p.Z - 3.5f);
                Vector3 target = new(p.X, p.Y, p.Z);
                Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                Engine.Camera.Target = new Vector3(-target.X, target.Y, target.Z);
            }
            else if (isMoba)
            {
                Vector3 camPos = p.Position + new Vector3(0f, 22.0f, -14.5f);
                Vector3 target = p.Position + new Vector3(0f, 1.0f, 1.5f);
                Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                Engine.Camera.Target = new Vector3(-target.X, target.Y, target.Z);
            }
            else if (SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey) && c.MedicTauntTimer <= 0f)
            {
                Vector3 eye = p.Position + new Vector3(0, 1.72f, 0);
                if (c.FpRoot != null && !c.FpRoot.IsDestroyed)
                {
                    c.FpRoot.Position = eye;
                    c.FpRoot.Rotation = new Vector3(_run.CamPitch, p.RotationY, 0f);
                }
                Engine.Camera.Position = new Vector3(-eye.X, eye.Y, eye.Z);
                Vector3 target = eye + camLookDir * 10f;
                Engine.Camera.Target = new Vector3(-target.X, target.Y, target.Z);
            }
            else
            {
                Vector3 pivot = p.Position + new Vector3(0, 2.25f, 0) + p.Right * 0.75f;
                Vector3 camPos = Physics.ResolveCameraCollision(p.Position + new Vector3(0, 1.85f, 0), pivot, pivot - camLookDir * 8.6f, 0.48f);
                float camFloor = Physics.GetSurfaceHeight(camPos.X, camPos.Z, camPos.Y) + 0.55f;
                if (camPos.Y < camFloor) camPos.Y = camFloor;

                Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                Vector3 target = pivot + camLookDir * 10f;
                Engine.Camera.Target = new Vector3(-target.X, target.Y, target.Z);
            }
        }

        private static void DrawPauseOverlay()
        {
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            Raylib.DrawRectangle(0, 0, sw, sh, Rgb(6, 8, 14, 150));
            int pw = 440, ph = 420, px = (sw - pw) / 2, py = (sh - ph) / 2;
            Raylib.DrawRectangle(px, py, pw, ph, Rgb(14, 18, 30, 245));
            Raylib.DrawRectangle(px, py, pw, 4, Color.SkyBlue);
            Engine.DrawTextCentered("ПАУЗА", sw * 0.5f, py + 34f, 26f, Color.SkyBlue);

            if (Engine.DrawButton(new Rectangle(px + 40, py + 72, pw - 80, 50), "ПРОДОЛЖИТЬ ИГРУ [ESC]", Rgb(38, 110, 70), Rgb(55, 155, 95), Color.White, 18f))
            {
                _paused = false;
                RestoreInGameCursorIfNeeded();
            }

            Engine.DrawTextLeft($"Громкость звука: {(SoundManager.Muted ? "ВЫКЛ" : $"{(int)MathF.Round(SoundManager.MasterVolume * 100f)}%")}", px + 42, py + 144, 16f, Color.SkyBlue);
            if (Engine.DrawButton(new Rectangle(px + 256, py + 136, 42, 34), "-", Rgb(42, 52, 74), Rgb(65, 80, 110), Color.White, 18f))
                SoundManager.SetMasterVolume(SoundManager.MasterVolume - 0.10f);
            if (Engine.DrawButton(new Rectangle(px + 304, py + 136, 42, 34), "+", Rgb(42, 52, 74), Rgb(65, 80, 110), Color.White, 18f))
                SoundManager.SetMasterVolume(SoundManager.MasterVolume + 0.10f);
            if (Engine.DrawButton(new Rectangle(px + 352, py + 136, 48, 34), SoundManager.Muted ? "ВКЛ" : "MUTE",
                SoundManager.Muted ? Rgb(115, 42, 48) : Rgb(42, 65, 92), Rgb(75, 100, 138), Color.White, 12f))
                SoundManager.ToggleMute();

            if (Engine.DrawButton(new Rectangle(px + 40, py + 190, pw - 80, 50), "ВЫЙТИ В ЛОББИ ПЕРСОНАЖЕЙ", Rgb(55, 75, 115), Rgb(80, 105, 155), Color.White, 18f))
            {
                QuitRunToLobby(toMainMenu: false);
            }
            if (Engine.DrawButton(new Rectangle(px + 40, py + 256, pw - 80, 50), "ВЫЙТИ В ГЛАВНОЕ МЕНЮ", Rgb(135, 40, 48), Rgb(185, 60, 68), Color.White, 18f))
            {
                QuitRunToLobby(toMainMenu: true);
            }
        }

        private static void DrawInGameHUD()
        {
            if (_run == null) return;
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            var c = _run.Combat;

            if (_cutsceneTimer > 0f)
            {
                DrawCharacterUnlockCutsceneOverlay(sw, sh);
                return;
            }

            // Красная виньетка по краям экрана при получении урона (в стиле CS)
            if (_damageFlashTimer > 0f)
            {
                byte flashA = (byte)Math.Clamp((int)(_damageFlashTimer * 240f), 0, 165);
                Color flashCol = Rgb(235, 25, 35, flashA);
                Raylib.DrawRectangle(0, 0, sw, 18, flashCol);
                Raylib.DrawRectangle(0, sh - 18, sw, 18, flashCol);
                Raylib.DrawRectangle(0, 0, 18, sh, flashCol);
                Raylib.DrawRectangle(sw - 18, 0, 18, sh, flashCol);
            }

            // Лаконичный аккуратный прицел в центре + Хитмаркер (CS-style) + Шкала заряда Тау-пушки Фримана
            int cx = sw / 2, cy = sh / 2;
            if (!SurvivorDatabase.IsMobaCharacter(_run.CharKey) && _run.CharKey != "mindustry" && !HasAnyInGameModalOpen())
            {
                Raylib.DrawCircleLines(cx, cy, 4f, Rgb(90, 230, 255, 180));
                Raylib.DrawPixel(cx, cy, Color.White);
            }
            if (_hitMarkerTimer > 0f)
            {
                Color hmCol = _hitMarkerCrit ? Color.Red : Color.White;
                int d1 = 5, d2 = _hitMarkerCrit ? 13 : 10;
                Raylib.DrawLine(cx - d2, cy - d2, cx - d1, cy - d1, hmCol);
                Raylib.DrawLine(cx + d2, cy - d2, cx + d1, cy - d1, hmCol);
                Raylib.DrawLine(cx - d2, cy + d2, cx - d1, cy + d1, hmCol);
                Raylib.DrawLine(cx + d2, cy + d2, cx + d1, cy + d1, hmCol);
            }
            if (_run.CharKey == "freeman" && c.TauCharging)
            {
                float tauPct = Math.Clamp(c.TauCharge / 2.5f, 0f, 1f);
                Color tauCol = tauPct >= 0.85f ? Color.Red : Color.Gold;
                Raylib.DrawRectangle(cx - 42, cy + 16, 84, 8, Rgb(18, 22, 32, 220));
                Raylib.DrawRectangle(cx - 40, cy + 18, (int)(80 * tauPct), 4, tauCol);
                Engine.DrawTextCentered($"ТАУ {(int)(tauPct * 100f)}%", cx, cy + 32, 12f, tauCol);
            }

            // 1. КОМПАКТНАЯ ЛЕНТА ПРЕДМЕТОВ СЛЕВА СВЕРХУ (RoR2-style)
            if (_run.ItemOrder.Count == 0)
            {
                Raylib.DrawRectangle(16, 14, 340, 26, Rgb(12, 16, 26, 135));
                Engine.DrawTextLeft("Инвентарь пуст  [E — Сундуки | TAB — Инфо]", 24, 19, 14f, Rgb(155, 175, 200));
            }
            else
            {
                int perRow = 16;
                int rows = (_run.ItemOrder.Count - 1) / perRow + 1;
                int stripW = Math.Min(_run.ItemOrder.Count, perRow) * 48 + 12;
                Raylib.DrawRectangle(16, 12, stripW, rows * 38 + 8, Rgb(12, 16, 26, 155));
                for (int i = 0; i < _run.ItemOrder.Count; i++)
                {
                    string k = _run.ItemOrder[i];
                    var info = GameData.ItemPool[k];
                    int cnt = _run.Items[k];
                    int r = i / perRow, colIdx = i % perRow;
                    int bx = 22 + colIdx * 48, by = 16 + r * 38;
                    Raylib.DrawRectangle(bx, by, 44, 34, Rgb(28, 36, 54, 210));
                    Raylib.DrawRectangle(bx, by, 44, 3, info.Color);
                    Engine.DrawTextCentered(info.Short, bx + 22, by + 11, 12f, info.Color);
                    Engine.DrawTextCentered($"x{cnt}", bx + 22, by + 25, 12f, Color.White);
                }
            }

            // 2. КОМПАКТНЫЙ ВИДЖЕТ ВРЕМЕНИ И СЛОЖНОСТИ СПРАВА СВЕРХУ (256x58)
            var (diffLabel, diffCol) = _run.GetDifficultyInfo();
            int mins = (int)(_run.ElapsedTime / 60f);
            int secs = (int)(_run.ElapsedTime % 60f);
            int rx = sw - 272, ry = 12;
            Raylib.DrawRectangle(rx, ry, 256, 58, Rgb(12, 16, 26, 185));
            Raylib.DrawRectangle(rx, ry, 256, 3, diffCol);
            Engine.DrawTextLeft($"{mins:00}:{secs:00}", rx + 12, ry + 8, 22f, Color.White);
            Engine.DrawTextLeft($"{diffLabel} (x{_run.DifficultyCoeff:F2})", rx + 82, ry + 11, 15f, diffCol);
            string detStr = _run.MoonDetonationTimer > 0f ? $" | ЭВАКУАЦИЯ: {_run.MoonDetonationTimer:F0}с!" : "";
            var activeDiffDef = GameData.GetActiveDifficulty();
            Engine.DrawTextLeft($"[{activeDiffDef.Name}] Этап {_run.Stage}: {_run.StageName}{detStr}", rx + 12, ry + 35, 13f, activeDiffDef.Color);
            if (GameData.ActiveArtifacts.Count > 0)
            {
                int ax = rx;
                int ay = ry + 64;
                foreach (var ak in GameData.OrderedArtifactKeys)
                {
                    if (!GameData.ActiveArtifacts.Contains(ak)) continue;
                    var aDef = GameData.RunArtifacts[ak];
                    Raylib.DrawRectangle(ax, ay, 46, 22, Rgb(14, 18, 30, 215));
                    Raylib.DrawRectangle(ax, ay, 46, 2, aDef.Color);
                    Engine.DrawTextCentered(aDef.Short, ax + 23, ay + 11, 11f, aDef.Color);
                    ax += 50;
                    if (ax > sw - 56) { ax = rx; ay += 26; }
                }
            }

            // 3. ТОНКАЯ ПОЛОСКА ЗДОРОВЬЯ БОССА ВВЕРХУ ПО ЦЕНТРУ (не мешает прицелу!)
            if (_run.IsBossAlive && _run.ActiveBoss != null)
            {
                var boss = _run.ActiveBoss;
                string bTitle = boss.EnemyType == "mithrix"
                    ? $"МИТРИКС [ФАЗА {boss.Phase}/4] :: КОРОЛЬ НИЧЕГО"
                    : _run.BossTitle;
                Color bCol = boss.EnemyType == "mithrix"
                    ? (boss.Phase == 4 ? Color.Magenta : Color.SkyBlue)
                    : Color.Red;
                int bw = 500, bx = (sw - bw) / 2, by = 52;
                Engine.DrawTextCentered(bTitle, sw * 0.5f, by - 14f, 17f, bCol);
                float bRatio = Math.Clamp(boss.Hp / MathF.Max(1f, boss.MaxHp), 0f, 1f);
                Raylib.DrawRectangle(bx, by, bw, 14, Rgb(35, 12, 16, 210));
                Raylib.DrawRectangle(bx, by, (int)(bw * bRatio), 14, bCol);
                Engine.DrawTextCentered($"{(int)MathF.Max(0, boss.Hp)} / {(int)boss.MaxHp}", sw * 0.5f, by + 7f, 13f, Color.White);
            }

            // 4. КОМПАКТНЫЙ БЛОК ЗДОРОВЬЯ И ВАЛЮТ СЛЕВА ВНИЗУ (300x60) + БЕЙДЖ КОЛЕСА ЭМОЦИЙ [L]
            int hx = 20, hy = sh - 78;
            bool emoteActive = !string.IsNullOrEmpty(_run.Parts.ActiveEmote);
            string emoteBadge = emoteActive ? "[L] Эмоции  |  [Space/WASD] Стоп" : "[L] Колесо эмоций";
            int ebW = emoteActive ? 248 : 154;
            Raylib.DrawRectangle(hx, hy - 24, ebW, 21, Rgb(12, 16, 26, 185));
            Engine.DrawTextLeft(emoteBadge, hx + 8, hy - 20, 12f, emoteActive ? Color.Lime : Color.Gold);

            Raylib.DrawRectangle(hx, hy, 300, 58, Rgb(12, 16, 26, 190));
            Engine.DrawTextLeft($"${_run.Gold}", hx + 12, hy + 7, 18f, Color.Gold);
            Engine.DrawTextLeft($"{_run.LunarCoins}М", hx + 95, hy + 7, 17f, Color.SkyBlue);
            var activeSurvCfg = SurvivorDatabase.Classes[_run.CharKey];
            var activeSurvSkin = activeSurvCfg.GetActiveSkin();
            string hudHeroName = activeSurvSkin.CustomSkillVariants != null ? activeSurvSkin.Name : activeSurvCfg.Name;
            Engine.DrawTextLeft(hudHeroName, hx + 155, hy + 8, 15f, Rgb(190, 210, 235));

            float hpRatio = Math.Clamp(_run.PlayerHp / _run.PlayerMaxHp, 0f, 1f);
            float shRatio = Math.Clamp(c.Shield / _run.PlayerMaxHp, 0f, 1f);
            Raylib.DrawRectangle(hx + 10, hy + 30, 280, 20, Rgb(42, 18, 22, 230));
            Raylib.DrawRectangle(hx + 10, hy + 30, (int)(280 * hpRatio), 20, hpRatio > 0.3f ? Rgb(80, 215, 95) : Rgb(240, 65, 60));
            if (shRatio > 0.01f)
                Raylib.DrawRectangle(hx + 10, hy + 30, (int)(280 * shRatio), 6, Rgb(85, 210, 255));

            string hpTxt = c.Shield >= 1f
                ? $"{(int)_run.PlayerHp} (+{(int)c.Shield}) / {(int)_run.PlayerMaxHp}"
                : $"{(int)_run.PlayerHp} / {(int)_run.PlayerMaxHp}";
            Engine.DrawTextCentered(hpTxt, hx + 150, hy + 40, 15f, Color.White);

            // 5. АККУРАТНЫЙ ДОК СПОСОБНОСТЕЙ СПРАВА ВНИЗУ (4 иконки 56x56)
            bool isMobaHud = SurvivorDatabase.IsMobaCharacter(_run.CharKey);
            if (isMobaHud)
            {
                Vector2 mp = Raylib.GetMousePosition();
                Raylib.DrawCircleLines((int)mp.X, (int)mp.Y, 10f, Color.Lime);
                Engine.DrawTextLeft("MOBA-РЕЖИМ: ПКМ — Идти/Атака | ЛКМ/Q — Удар | W/СКМ — Навык 2 | Shift — Рывок | R — Ульта | Space — Прыжок",
                    sw - 760, sh - 98, 13f, Rgb(120, 245, 200));
            }
            (string key, string bind, float cd, float maxCd, Color col)[] skBoxes =
            {
                ("m1", isMobaHud ? "ЛКМ/Q" : "ЛКМ", c.M1Cd, MathF.Max(0.1f, c.Cfg.Skills["m1"].Cd), Color.Yellow),
                ("m2", isMobaHud ? "W/СКМ" : "ПКМ", c.M2Cd, MathF.Max(0.1f, c.Cfg.Skills["m2"].Cd), Color.Orange),
                ("shift", "Shift", c.ShiftCd, MathF.Max(0.1f, c.Cfg.Skills["shift"].Cd), Color.SkyBlue),
                ("r", "R", c.RCd, MathF.Max(0.1f, c.Cfg.Skills["r"].Cd), Color.Magenta)
            };
            for (int i = 0; i < skBoxes.Length; i++)
            {
                var (_, bind, cd, maxCd, col) = skBoxes[i];
                int sx = sw - 268 + i * 62;
                int sy = sh - 76;
                Raylib.DrawRectangle(sx, sy, 56, 56, cd <= 0f ? Rgb(26, 38, 56, 215) : Rgb(16, 18, 26, 215));
                if (cd > 0f)
                {
                    int fillH = (int)(56 * Math.Clamp(cd / maxCd, 0f, 1f));
                    Raylib.DrawRectangle(sx, sy + (56 - fillH), 56, fillH, Rgb(220, 45, 55, 95));
                }
                Raylib.DrawRectangle(sx, sy, 56, 3, cd <= 0f ? col : Color.Gray);
                Engine.DrawTextCentered(bind, sx + 28, sy + 16, 13f, col);
                Engine.DrawTextCentered(cd <= 0f ? "ГОТ" : $"{cd:F1}", sx + 28, sy + 38, 15f, cd <= 0f ? Color.White : Color.Red);
            }

            if (_isSpectating)
            {
                Raylib.DrawRectangle(sw / 2 - 340, 88, 680, 48, Rgb(12, 16, 28, 220));
                Raylib.DrawRectangle(sw / 2 - 340, 88, 680, 3, Color.Orange);
                Engine.DrawTextCentered("РЕЖИМ НАБЛЮДАТЕЛЯ :: [A / D] СМЕНИТЬ ИГРОКА | ОЖИДАНИЕ ВОЗРОЖДЕНИЯ", sw * 0.5f, 112f, 17f, Color.Orange);
                int killerY = GameData.ActiveArtifacts.Count > 0 ? 124 : 78;
                DrawKillerMobTopRightWidget(sw - 446, killerY, 430, allow3DToggle: false);
            }
            else if (_run.CharKey == "mindustry")
            {
                Vector2 mp = Raylib.GetMousePosition();
                Raylib.DrawCircleLines((int)mp.X, (int)mp.Y, c.MindustryBuildMode && c.MindustrySelectedBuild >= 0 ? 14f : 8f,
                    c.MindustryBuildMode ? Color.SkyBlue : Color.Gold);

                // Панель 3 ресурсов, здоровья Главного Ядра и текущего режима
                int pnlX = sw - 520, pnlY = sh - 242;
                Raylib.DrawRectangle(pnlX, pnlY, 504, 34, Rgb(12, 16, 26, 225));
                Raylib.DrawRectangle(pnlX, pnlY, 504, 2, c.MindustryBuildMode ? Color.SkyBlue : Color.Gold);
                string coreHpStr = IsValid(c.MindustryMainCore)
                    ? $" | ЯДРО: {(int)c.MindustryMainCore!.Hp} HP"
                    : $" | ПОСТРОЙТЕ ЯДРО [5]: {c.MindustryCoreRebuildTimer:F0}с!";
                Color resCol = (!IsValid(c.MindustryMainCore) && c.MindustryCoreRebuildTimer > 0f) ? Color.Red : Color.White;
                Engine.DrawTextLeft($"МЕДЬ: {(int)c.MindustryCopper} | КРЕМНИЙ: {(int)c.MindustrySilicon} | ТОРИЙ: {(int)c.MindustryThorium}{coreHpStr}",
                    pnlX + 10, pnlY + 8, 14f, resCol);

                string modeBtnTxt = c.MindustryBuildMode
                    ? "РЕЖИМ: СТРОИТЕЛЬСТВО (НЕУЯЗВИМ) -> В БОЙ [B/Shift]"
                    : "РЕЖИМ: БОЕВОЙ (УЯЗВИМ, СТРЕЛЬБА И [E]) -> СТРОИТЬ [B/Shift]";
                if (Engine.DrawButton(new Rectangle(pnlX, pnlY - 38, 504, 34), modeBtnTxt,
                    c.MindustryBuildMode ? Rgb(32, 85, 135) : Rgb(125, 75, 25), Rgb(65, 130, 195), Color.White, 14f,
                    c.MindustryBuildMode ? Color.SkyBlue : Color.Gold))
                {
                    c.ToggleMindustryMode();
                }

                if (c.MindustryBuildMode)
                {
                    Raylib.DrawRectangle(pnlX, pnlY + 38, 504, 122, Rgb(10, 14, 24, 235));
                    for (int bi = 0; bi < SurvivorCombatController.MindustryBlueprints.Length; bi++)
                    {
                        var bp = SurvivorCombatController.MindustryBlueprints[bi];
                        int r = bi / 4, col = bi % 4;
                        Rectangle bRect = new(pnlX + 6 + col * 124, pnlY + 44 + r * 56, 120, 50);
                        bool sel = c.MindustrySelectedBuild == bi;
                        bool canAfford = c.MindustryCopper >= bp.Copper && c.MindustrySilicon >= bp.Silicon && c.MindustryThorium >= bp.Thorium;
                        string costStr = $"{(int)bp.Copper}М" + (bp.Silicon > 0 ? $" {(int)bp.Silicon}Кр" : "") + (bp.Thorium > 0 ? $" {(int)bp.Thorium}Т" : "");
                        if (Engine.DrawButton(bRect, $"{bp.ShortName}\n{costStr}",
                            sel ? Rgb(45, 95, 155) : Rgb(22, 28, 42), Rgb(60, 115, 180),
                            canAfford ? (sel ? Color.Yellow : Color.White) : Color.Gray, 12f, sel ? Color.Yellow : null))
                        {
                            c.MindustrySelectedBuild = sel ? -1 : bi;
                        }
                    }
                }
                else
                {
                    Engine.DrawTextLeft("БОЕВОЙ РЕЖИМ: ЛКМ — Огонь | [E] — Сундуки и Портал | [B / Shift] — Режим Строительства",
                        pnlX, pnlY + 42, 13f, Color.Gold);
                }
            }
            else if (_run.CharKey == "reimu")
            {
                Engine.DrawTextLeft($"ПОЛЁТ [Зажать Space в воздухе]: {c.ReimuFlightFuel:F1}с {(c.ReimuFlightBroken ? "[СБИТ УРОНОМ!]" : "")}",
                    sw - 420, sh - 98, 14f, c.ReimuFlightBroken ? Color.Red : Color.SkyBlue);
            }
            else if (_run.CharKey == "freeman")
            {
                string gTxt = c.HasGrabbedObject ? "ОБЪЕКТ ЗАХВАЧЕН [ПКМ — БРОСИТЬ]" : "ГРАВИ-ПУШКА ГОТОВА [ПКМ]";
                string tTxt = c.TauCharging ? $"ЗАРЯД ТАУ-ПУШКИ: {(int)(c.TauCharge / 2.5f * 100f)}%" : "ТАУ-ПУШКА [Зажать R]";
                Engine.DrawTextLeft($"{gTxt}  |  {tTxt}", sw - 540, sh - 98, 14f, Color.Orange);
            }
            else if (_run.CharKey == "medic")
            {
                Engine.DrawTextLeft("НАСМЕШКА ОТ 3-ГО ЛИЦА: [G] — «Oktoberfest!»", sw - 360, sh - 98, 14f, Color.Lime);
            }
            else if (_run.CharKey == "cheburashka")
            {
                string standTxt = c.GenaStandTimer > 0f
                    ? $"СТЕНД 『КРОКОДИЛ ГЕНА』 АКТИВЕН: {c.GenaStandTimer:F1}с"
                    : "ПАССИВКА: Ударная волна при падении с высоты";
                Engine.DrawTextLeft(standTxt, sw - 460, sh - 98, 14f, c.GenaStandTimer > 0f ? Color.Gold : Color.Orange);
            }
            else if (_run.CharKey == "ahri")
            {
                string ahriTxt = $"КРАЖА СУЩНОСТИ: {c.AhriEssenceStacks}/9  |  ПРИЗРАЧНЫЙ РЫВОК [Shift]: {c.AhriDashCharges}/3";
                Engine.DrawTextLeft(ahriTxt, sw - 540, sh - 116, 14f, Rgb(105, 235, 255));
            }
            else if (_run.CharKey == "steve")
            {
                int sx = sw - 560, sy = sh - 134;
                Raylib.DrawRectangle(sx, sy, 544, 52, Rgb(12, 18, 28, 225));
                Raylib.DrawRectangle(sx, sy, 544, 3, Color.SkyBlue);
                string gearStr = $"Меч: {(c.SteveSwordTier >= 2 ? "Незерит" : (c.SteveSwordTier == 1 ? "Алмаз" : "Баз."))} | Броня: {(c.SteveArmorTier >= 3 ? "Незерит" : (c.SteveArmorTier >= 1 ? "Железо" : "Нет"))}{(c.SteveHasTotem ? " | ★ТОТЕМ" : "")}";
                Engine.DrawTextLeft($"ДЕРЕВО: {c.SteveWood}  |  КАМЕНЬ: {c.SteveStone}  |  ЖЕЛЕЗО: {c.SteveIron}  |  АЛМАЗЫ: {c.SteveDiamond}   ({gearStr})",
                    sx + 10, sy + 7, 13f, Color.SkyBlue);
                Engine.DrawTextLeft("ЛКМ вниз / ПКМ — ломать пол (ямы!), стены и алтари  |  [R] или [E] у Верстака — Крафт",
                    sx + 10, sy + 29, 12f, Color.Gold);
            }
            else if (_run.CharKey == "gold_ship")
            {
                int gx = sw - 520, gy = sh - 118;
                float momPct = Math.Clamp(c.GoldShipMomentum, 0f, 1f);
                Raylib.DrawRectangle(gx, gy, 504, 36, Rgb(14, 18, 30, 225));
                Raylib.DrawRectangle(gx, gy, (int)(504 * momPct), 4, Color.Gold);
                string gsTxt = $"РАЗБЕГ ГОЛД ШИП: {(int)(momPct * 100f)}%  |  МНОЖИТЕЛЬ УРОНА ОТ СКОРОСТИ: x{c.GoldShipSpeedScale:F2}{(c.GoldShipSpurtTimer > 0f ? $"  |  ГОЛШИ-ВАРП: {c.GoldShipSpurtTimer:F1}с" : "")}";
                Engine.DrawTextLeft(gsTxt, gx + 10, gy + 11, 13f, momPct > 0.7f ? Color.Gold : Color.White);
            }
            else if (_run.CharKey == "jotaro")
            {
                string jTxt = c.ZaWarudoTimer > 0f
                    ? $"★ STAR PLATINUM: THE WORLD :: ВРЕМЯ ОСТАНОВЛЕНО ({c.ZaWarudoTimer:F1}с) ★"
                    : "СТЕНД 『STAR PLATINUM』 АКТИВЕН :: [ЛКМ] ORA ORA | [R] Остановка Времени";
                Engine.DrawTextLeft(jTxt, sw - 560, sh - 104, 14f, c.ZaWarudoTimer > 0f ? Color.Gold : Rgb(195, 130, 255));
            }
            else if (_run.CharKey == "sol")
            {
                int solSkin = Data.ProgressData.Instance.GetSelectedSkin("sol");
                if (solSkin == 2 && c.KyStunEdgeCharging)
                {
                    int kx = sw - 560, ky = sh - 122;
                    float chPct = Math.Clamp(c.KyStunEdgeCharge / SurvivorCombatController.KyStunEdgeMaxCharge, 0f, 1f);
                    Raylib.DrawRectangle(kx, ky, 542, 38, Rgb(12, 18, 32, 236));
                    Color barCol = chPct >= 0.75f ? Rgb(255, 75, 95) : (chPct >= 0.35f ? Color.Gold : Color.SkyBlue);
                    Raylib.DrawRectangle(kx, ky, (int)(542 * chPct), 5, barCol);
                    string chTitle = chPct >= 0.75f ? "★ CHARGED STUN EDGE ГОТОВ!" : "ЗАРЯД STUN EDGE (УДЕРЖИВАЙТЕ ПКМ)";
                    Engine.DrawTextLeft($"{chTitle} :: {(int)(chPct * 100f)}%  |  Урон x{(2.45f + chPct * 3.45f):F1}", kx + 10, ky + 12, 13f, barCol);
                }
                else
                {
                    string solTxt = solSkin == 2
                        ? "★ КАЙ КИСКЕ (THUNDER SEAL) :: [Зажать ПКМ] Заряд Stun Edge | [Shift] Подкат + Разряд"
                        : "СОЛ БЭДГАЙ (OUTRAGE MK.II) :: Пламенное комбо и Dragon Install";
                    Engine.DrawTextLeft(solTxt, sw - 560, sh - 104, 13f, solSkin == 2 ? Color.SkyBlue : Color.Orange);
                }
            }
            else if (_run.CharKey == "jack_o")
            {
                int jx = sw - 560, jy = sh - 122;
                int aliveSv = 0;
                foreach (var sv in c.JackOServants) if (IsValid(sv)) aliveSv++;
                JackOCommandFlag? activeFl = null;
                foreach (var fl in c.JackOFlags) if (IsValid(fl)) { activeFl = fl; break; }

                Raylib.DrawRectangle(jx, jy, 542, 38, Rgb(22, 14, 24, 232));
                Color flCol = activeFl != null
                    ? (activeFl.BuffMode == "attack" ? Rgb(255, 95, 65) : (activeFl.BuffMode == "defense" ? Color.SkyBlue : Color.Gold))
                    : Rgb(155, 255, 75);
                float barRatio = activeFl != null ? Math.Clamp(activeFl.Lifetime / 12.0f, 0f, 1f) : Math.Clamp(aliveSv / 4.0f, 0f, 1f);
                Raylib.DrawRectangle(jx, jy, (int)(542 * barRatio), 4, flCol);

                string flagStatus = activeFl != null
                    ? $"★ ФЛАГ ({(activeFl.BuffMode == "attack" ? "+65% УРОН" : (activeFl.BuffMode == "defense" ? "+65% ЗАЩИТА" : "УРОН+ЗАЩИТА"))}): {MathF.Max(0f, activeFl.Lifetime):F1}с"
                    : $"[Shift] Прыжок с флагом ({(c.JackONextFlagDefense ? "Защита" : "Урон")})";
                Engine.DrawTextLeft($"ДЖЕК-О :: СЛУГИ: {aliveSv}/4  |  {flagStatus}", jx + 10, jy + 12, 13f, flCol);
            }
            else if (_run.CharKey == "dmc_trio")
            {
                int skin = Data.ProgressData.Instance.GetSelectedSkin("dmc_trio");
                string formName = skin == 1 ? $"НЕРО (Exceed: {c.NeroExceedCharges}/3)" : (skin == 2 ? "ВЕРГИЛИЙ (Concentration)" : "ДАНТЕ (SWORDMASTER)");
                string dtStr = c.DmcDevilTriggerTimer > 0f ? $"  |  ★ DEVIL TRIGGER: {c.DmcDevilTriggerTimer:F1}с" : "";
                Engine.DrawTextLeft($"ОХОТНИК НА ДЕМОНОВ: {formName}{dtStr}", sw - 520, sh - 104, 14f, c.DmcDevilTriggerTimer > 0f ? Color.Magenta : Color.Gold);
            }
            else if (_run.CharKey == "sanabi")
            {
                int sx = sw - 535, sy = sh - 122;
                float momPct = Math.Clamp(c.SanabiMomentum, 0f, 1f);
                Raylib.DrawRectangle(sx, sy, 518, 38, Rgb(14, 18, 30, 232));
                Raylib.DrawRectangle(sx, sy, (int)(518 * momPct), 4, Rgb(255, 195, 55));
                string ovStr = c.SanabiOverdriveTimer > 0f ? $"  |  ★ ПРОТОКОЛ SANABI (МАРИ): {c.SanabiOverdriveTimer:F1}с" : "  |  [ПКМ/Shift] Цепной Захват";
                string snTxt = $"ИМПУЛЬС ЦЕПИ: {(int)(momPct * 100f)}%  |  УРОН ОТ РАЗГОНА: x{c.SanabiSpeedScale:F2}{ovStr}";
                Engine.DrawTextLeft(snTxt, sx + 10, sy + 12, 13f, c.SanabiOverdriveTimer > 0f ? Rgb(80, 235, 255) : Color.Gold);
            }
            else if (_run.CharKey is "lappland" or "w_arknights" or "pushnoy" or "freddy" or "vyper" or "haze")
            {
                string specTxt = _run.CharKey switch
                {
                    "lappland" => c.LapplandFormTimer > 0f ? $"★ ЗАТМЕНИЕ БЕЗУМИЯ АКТИВНО: {c.LapplandFormTimer:F1}с" : "ПАССИВКА: Удары накладывают Безмолвие и раскол брони (-30%)",
                    "w_arknights" => c.WisadelTurretTimer > 0f ? $"★ НАСЛЕДИЕ РЕВЕНАНТА АКТИВНО: {c.WisadelTurretTimer:F1}с" : "ПАССИВКА: Взрывы оглушают цели и дают +35% крит. урона",
                    "pushnoy" => c.PushnoyRockBuffTimer > 0f ? $"★ МЕГА-РОК СОЛО ГАЛИЛЕО: {c.PushnoyRockBuffTimer:F1}с" : "ЭКСПЕРИМЕНТ ГАЛИЛЕО: Акустический рифф + Научные опыты",
                    "freddy" => c.FreddyBlackoutTimer > 0f ? $"★ ОТКЛЮЧЕНИЕ СВЕТА (МАРШ ТОРЕАДОРА): {c.FreddyBlackoutTimer:F1}с" : "ПАССИВКА: Ночная Смена (Ночной режим с 00:00 до 06:00)",
                    "vyper" => c.VyperSlideTimer > 0f ? $"★ СИЛОВОЙ ПОДКЛАД АКТИВЕН ({c.VyperSlideTimer:F1}с): +35% СКОРОСТРЕЛЬНОСТИ" : "ПАССИВКА: Скольжение даёт +35% к скорострельности и броню",
                    _ => c.HazeDanceTimer > 0f ? $"★ ТАНЕЦ ПУЛЬ (BULLET DANCE): {c.HazeDanceTimer:F1}с" : (c.HazeSmokeTimer > 0f ? $"ДЫМОВАЯ НЕВИДИМОСТЬ: {c.HazeSmokeTimer:F1}с" : "ПАССИВКА: Фиксация (до 20 стаков на цели -> взрыв сновидений)")
                };
                Engine.DrawTextLeft(specTxt, sw - 560, sh - 104, 13f, Color.Gold);
            }

            // 6. КОМПАКТНЫЙ ULTRAKILL STYLE METER + ПАНЕЛЬ 5 ОРУЖИЙ V1
            if (_run.CharKey == "v1")
            {
                var (rName, rCol, rMult) = c.V1GetStyleRankInfo();
                int vx = sw - 252, vy = 84;
                Raylib.DrawRectangle(vx, vy, 236, 138, Rgb(10, 12, 20, 175));
                Raylib.DrawRectangle(vx, vy, 236, 3, rCol);
                Engine.DrawTextLeft($"{rName} (x{rMult:F2})", vx + 10, vy + 8, 18f, rCol);
                Raylib.DrawRectangle(vx + 10, vy + 32, 216, 7, Rgb(35, 38, 50));
                Raylib.DrawRectangle(vx + 10, vy + 32, (int)(216 * (c.V1StylePoints / 1000f)), 7, rCol);
                for (int i = 0; i < Math.Min(4, c.V1StyleLog.Count); i++)
                    Engine.DrawTextLeft(c.V1StyleLog[i].Text, vx + 10, vy + 46 + i * 19, 14f, c.V1StyleLog[i].Col);

                string[] wNames = { "1.РЕВОЛЬВ", "2.ДРОБОВ", "3.ГВОЗДИ", "4.РЕЛЬСА", "5.РАКЕТЫ" };
                for (int wi = 0; wi < 5; wi++)
                {
                    bool wActive = c.V1Weapon == wi + 1;
                    int wx = sw - 535 + wi * 104, wy = sh - 122;
                    Raylib.DrawRectangle(wx, wy, 98, 28, wActive ? Rgb(42, 90, 165, 230) : Rgb(14, 18, 28, 195));
                    Raylib.DrawRectangle(wx, wy, 98, 2, wActive ? Color.Gold : Color.Gray);
                    Engine.DrawTextCentered(wNames[wi], wx + 49, wy + 14, 12f, wActive ? Color.Gold : Color.White);
                }
                string armStr = c.V1ActiveArm == "feedbacker" ? "РУКА [V]: СИНЯЯ [F]" : "РУКА [V]: КРАСНАЯ [F/G]";
                Engine.DrawTextLeft($"{armStr} | МОНЕТЫ: {(int)c.V1Coins} | РЕЛЬСА: {(int)c.V1RailCharge}% | РЫВОК: {(int)c.V1Stamina}",
                    sw - 535, sh - 92, 13f, Color.SkyBlue);
            }

            // 7. ОКНО ДЕТАЛЬНОЙ СТАТИСТИКИ ПО УДЕРЖАНИЮ [TAB]
            if (Raylib.IsKeyDown(KeyboardKey.Tab))
            {
                int tw = 780, th = 420, tx = (sw - tw) / 2, ty = (sh - th) / 2;
                Raylib.DrawRectangle(tx, ty, tw, th, Rgb(12, 16, 26, 240));
                Raylib.DrawRectangle(tx, ty, tw, 3, Color.SkyBlue);
                string netInfo = _net.Connected ? $"Сеть [{_net.GameType.ToUpper()}]: Комната {_net.RoomCode} ({1 + _remotePlayers.Count} игр.)" : "Одиночная игра";
                Engine.DrawTextLeft($"{SurvivorDatabase.Classes[_run.CharKey].Name}  |  {netInfo}", tx + 24, ty + 18, 20f, Color.Gold);
                Engine.DrawTextLeft($"Урон: {c.BaseDamage:F1}  |  Атака: {c.FireRate:F1}/с  |  Скорость: {c.MoveSpeed:F1}  |  Крит: {c.CritChance * 100f:F0}%  |  Блок: {c.BlockChance * 100f:F0}%",
                    tx + 24, ty + 48, 16f, Color.White);
                Engine.DrawTextLeft($"Убито врагов: {RunKills}  |  Нанесено урона: {(int)RunDamageDealt}  |  Собрано золота: ${RunGoldEarned}",
                    tx + 24, ty + 74, 14f, Rgb(150, 195, 235));

                string tabMods = $"Сложность: {GameData.GetActiveDifficulty().Name}  |  Артефакты ({GameData.ActiveArtifacts.Count}): " +
                    (GameData.ActiveArtifacts.Count > 0 ? string.Join(", ", System.Linq.Enumerable.Select(GameData.ActiveArtifacts, ak => GameData.RunArtifacts[ak].Short)) : "Нет");
                Engine.DrawTextLeft(tabMods, tx + 24, ty + 94, 13f, Color.Gold);
                int sy = ty + 120;
                foreach (var k in _run.ItemOrder)
                {
                    if (sy > ty + th - 32) break;
                    var info = GameData.ItemPool[k];
                    Engine.DrawTextLeft($"• {info.Name} [x{_run.Items[k]}] — {info.Desc}", tx + 24, sy, 15f, info.Color);
                    sy += 23;
                }
            }

            // 8. ИНТЕРАКТИВНОЕ ОКНО АРТЕФАКТА КОМАНДОВАНИЯ (ВЫБОР ЛЮБОГО ПРЕДМЕТА ПОДОБРАННОЙ РЕДКОСТИ)
            if (_commandPickerQueue.Count > 0 && !_paused)
            {
                if (Raylib.IsCursorHidden()) Raylib.EnableCursor();
                string pickTier = _commandPickerQueue.Peek();
                var tierItems = new List<string>();
                foreach (var kv in GameData.ItemPool)
                {
                    if (kv.Key == "tonic_affliction") continue;
                    if (!ProgressData.Instance.UnlockedItems.Contains(kv.Key)) continue;
                    if (kv.Value.Tier == pickTier) tierItems.Add(kv.Key);
                }
                if (tierItems.Count == 0)
                {
                    foreach (var kv in GameData.ItemPool)
                        if (kv.Value.Tier == "white") tierItems.Add(kv.Key);
                }

                Color tierCol = pickTier switch
                {
                    "green" => Rgb(80, 235, 95),
                    "red" => Rgb(250, 65, 65),
                    "lunar" => Rgb(75, 185, 255),
                    "void" => Rgb(195, 90, 255),
                    _ => Color.White
                };
                string tierTitle = pickTier switch
                {
                    "green" => "НЕОБЫЧНАЯ ЭССЕНЦИЯ (ЗЕЛЁНЫЙ ПРЕДМЕТ)",
                    "red" => "ЛЕГЕНДАРНАЯ ЭССЕНЦИЯ (КРАСНЫЙ ПРЕДМЕТ)",
                    "lunar" => "ЛУННАЯ ЭССЕНЦИЯ (СИНИЙ ПРЕДМЕТ)",
                    "void" => "ЭССЕНЦИЯ ПУСТОТЫ (ФИОЛЕТОВЫЙ ПРЕДМЕТ)",
                    _ => "ОБЫЧНАЯ ЭССЕНЦИЯ (БЕЛЫЙ ПРЕДМЕТ)"
                };

                int mw = 820, mh = 510, mx = (sw - mw) / 2, my = (sh - mh) / 2;
                Raylib.DrawRectangle(0, 0, sw, sh, Rgb(6, 8, 16, 145));
                Raylib.DrawRectangle(mx, my, mw, mh, Rgb(12, 16, 28, 248));
                Raylib.DrawRectangle(mx, my, mw, 4, tierCol);
                Engine.DrawTextCentered($"АРТЕФАКТ КОМАНДОВАНИЯ :: {tierTitle}", sw * 0.5f, my + 24f, 20f, tierCol);
                Engine.DrawTextCentered($"Выберите предмет кликом мыши (В очереди эссенций: {_commandPickerQueue.Count})", sw * 0.5f, my + 48f, 14f, Color.Gold);

                Vector2 mp = Raylib.GetMousePosition();
                string hName = "Наведите курсор на предмет";
                string hDesc = "Нажмите ЛКМ по любому предмету ниже, чтобы немедленно добавить его в инвентарь.";
                Color hCol = tierCol;

                int cols = 5;
                for (int idx = 0; idx < tierItems.Count; idx++)
                {
                    string ik = tierItems[idx];
                    var inf = GameData.ItemPool[ik];
                    int r = idx / cols, col = idx % cols;
                    Rectangle iRect = new(mx + 25 + col * 156, my + 72 + r * 64, 148, 56);
                    int haveCnt = _run.Items.TryGetValue(ik, out int hc) ? hc : 0;
                    string countBadge = haveCnt > 0 ? $" (x{haveCnt})" : "";

                    if (Raylib.CheckCollisionPointRec(mp, iRect))
                    {
                        hName = $"{inf.Name} [{inf.Short}]{countBadge}";
                        hDesc = inf.Desc;
                        hCol = inf.Color;
                    }

                    if (Engine.DrawButton(iRect, $"[{inf.Short}]{countBadge}\n{inf.Name}",
                        Rgb(22, 30, 46), Rgb(48, 78, 128), Color.White, 12f, inf.Color))
                    {
                        _commandPickerQueue.Dequeue();
                        _run.CollectItem(ik);
                        if (_commandPickerQueue.Count == 0 && !SurvivorDatabase.IsMobaCharacter(_run.CharKey) && _run.CharKey != "mindustry")
                            Raylib.DisableCursor();
                        break;
                    }
                }

                Raylib.DrawRectangle(mx + 25, my + mh - 96, mw - 50, 76, Rgb(18, 24, 40, 245));
                Raylib.DrawRectangle(mx + 25, my + mh - 96, 4, 76, hCol);
                Engine.DrawTextLeft(hName, mx + 40, my + mh - 86, 18f, hCol);
                Engine.DrawTextLeft(hDesc, mx + 40, my + mh - 56, 15f, Color.White);
            }

            // 9. ИНТЕРАКТИВНОЕ ОКНО ВЕРСТАКА СТИВА (MINECRAFT WORKBENCH CRAFTING POOL)
            if (_workbenchOpen && !_paused)
            {
                DrawWorkbenchModalUI(sw, sh);
            }

            // 10. ХАБ UES «SAFE TRAVELS», КОЛЕСО ЭМОЦИЙ [L], ДИАЛОГИ ЭКИПАЖА И К-Н-Б
            if (!_paused)
            {
                if (_run.IsSafeTravels)
                    DrawSafeTravelsHubOverlay(sw, sh);

                DrawSafeTravelsDialogueModal(sw, sh);
                DrawRpsMiniGameModal(sw, sh);
                DrawEmoteWheelOverlay(sw, sh);
            }
        }

        private static int CountRunItemsByTier(string? tierFilter = null)
        {
            if (_run == null) return 0;
            int total = 0;
            foreach (var kv in _run.Items)
            {
                if (kv.Value <= 0 || kv.Key == "tonic_affliction") continue;
                if (tierFilter == null || (GameData.ItemPool.TryGetValue(kv.Key, out var inf) && inf.Tier == tierFilter))
                    total += kv.Value;
            }
            return total;
        }

        private static bool ConsumeRunItems(int count, string? tierFilter = null)
        {
            if (_run == null || CountRunItemsByTier(tierFilter) < count) return false;
            int left = count;
            for (int i = _run.ItemOrder.Count - 1; i >= 0 && left > 0; i--)
            {
                string k = _run.ItemOrder[i];
                if (k == "tonic_affliction" || !_run.Items.TryGetValue(k, out int have) || have <= 0) continue;
                if (tierFilter != null && (!GameData.ItemPool.TryGetValue(k, out var inf) || inf.Tier != tierFilter)) continue;

                int take = Math.Min(have, left);
                _run.Items[k] -= take;
                left -= take;
                if (_run.Items[k] <= 0)
                {
                    _run.Items[k] = 0;
                    _run.ItemOrder.RemoveAt(i);
                }
            }
            return true;
        }

        private static void DrawWorkbenchModalUI(int sw, int sh)
        {
            if (_run == null) return;
            if (Raylib.IsCursorHidden()) Raylib.EnableCursor();
            var c = _run.Combat;

            int mw = 920, mh = 590, mx = (sw - mw) / 2, my = (sh - mh) / 2;
            Raylib.DrawRectangle(0, 0, sw, sh, Rgb(6, 8, 16, 165));
            Raylib.DrawRectangle(mx, my, mw, mh, Rgb(14, 18, 30, 250));
            Raylib.DrawRectangle(mx, my, mw, 4, Rgb(80, 235, 245));

            Engine.DrawTextLeft("⛏ ВЕРСТАК И НАКОВАЛЬНЯ СТИВА :: ПУЛ КРАФТОВ MINECRAFT", mx + 24, my + 16, 20f, Rgb(80, 235, 245));
            if (Engine.DrawButton(new Rectangle(mx + mw - 184, my + 12, 160, 32), "ЗАКРЫТЬ [ESC]",
                Rgb(110, 38, 46), Rgb(155, 55, 65), Color.White, 14f, Color.Red))
            {
                _workbenchOpen = false;
                RestoreInGameCursorIfNeeded();
                return;
            }

            // Плашка текущих добытых ресурсов с карты и предметов в инвентаре
            int whiteItems = CountRunItemsByTier("white");
            int greenItems = CountRunItemsByTier("green");
            int allItems = CountRunItemsByTier(null);
            Raylib.DrawRectangle(mx + 24, my + 52, mw - 48, 44, Rgb(20, 28, 44, 240));
            Raylib.DrawRectangle(mx + 24, my + 52, 4, 44, Color.Gold);
            Engine.DrawTextLeft(
                $"РЕСУРСЫ С КАРТЫ:   Дерево: {c.SteveWood}   |   Камень: {c.SteveStone}   |   Железо: {c.SteveIron}   |   Алмазы: {c.SteveDiamond}",
                mx + 38, my + 58, 15f, Color.SkyBlue);
            Engine.DrawTextLeft(
                $"ПРЕДМЕТЫ В ИНВЕНТАРЕ:   Обычные (Белые): {whiteItems}   |   Необычные (Зелёные): {greenItems}   |   Всего предметов: {allItems}",
                mx + 38, my + 77, 13f, Color.Gold);

            var recipes = new (string Title, string CostText, string Desc, Color Col, bool CanCraft, Action DoCraft)[]
            {
                (
                    "1. Алмазный Меч Остроты V + Очки Творца",
                    "Цена: 2 Дерева + 3 Алмаза",
                    "Повышает ранг меча Стива до Алмазного (+35% базового урона навсегда) и создаёт предмет «Очки Творца» (+10% крита).",
                    Rgb(80, 240, 250),
                    c.SteveWood >= 2 && c.SteveDiamond >= 3,
                    () =>
                    {
                        c.SteveWood -= 2; c.SteveDiamond -= 3;
                        c.SteveSwordTier = Math.Max(c.SteveSwordTier, 1);
                        _run.CollectItem("lens");
                        ShowBanner("СКРАФЧЕНО: Алмазный Меч Остроты V (+35% урона) + Очки Творца!", Rgb(80, 240, 250), 3.5f);
                    }
                ),
                (
                    "2. Незеритовый Клинок + Грань Бесконечности",
                    "Цена: 4 Железа + 5 Алмазов + 1 Белый предмет",
                    "Максимальный ранг меча Стива (+75% базового урона) + перековывает 1 обычный предмет в «Грань Бесконечности»!",
                    Rgb(255, 95, 75),
                    c.SteveIron >= 4 && c.SteveDiamond >= 5 && whiteItems >= 1,
                    () =>
                    {
                        if (!ConsumeRunItems(1, "white")) return;
                        c.SteveIron -= 4; c.SteveDiamond -= 5;
                        c.SteveSwordTier = 2;
                        _run.CollectItem("infinity_edge");
                        ShowBanner("СКРАФЧЕНО: Незеритовый Клинок (+75% урона) + Грань Бесконечности!", Color.Gold, 4.0f);
                    }
                ),
                (
                    "3. Железный Доспех Защиты + Бронепластина",
                    "Цена: 5 Камня + 4 Железа",
                    "Надевает на Стива Железный Доспех (-22% получаемого урона, +50 Щита) и создаёт предмет «Бронепластина».",
                    Rgb(215, 225, 240),
                    c.SteveStone >= 5 && c.SteveIron >= 4,
                    () =>
                    {
                        c.SteveStone -= 5; c.SteveIron -= 4;
                        c.SteveArmorTier = Math.Max(c.SteveArmorTier, 1);
                        c.Shield = MathF.Min(_run.PlayerMaxHp, c.Shield + 50f);
                        _run.CollectItem("repulsion_armor");
                        ShowBanner("СКРАФЧЕНО: Железный Доспех Защиты + Бронепластина!", Color.SkyBlue, 3.5f);
                    }
                ),
                (
                    "4. Незеритовый Доспех + Сердце Тарраски",
                    "Цена: 6 Железа + 6 Алмазов + 1 Предмет",
                    "Максимальная Незеритовая Броня (-54% получаемого урона, +120 Щита) и легендарное «Сердце Тарраски» (+100 HP и реген)!",
                    Rgb(195, 90, 255),
                    c.SteveIron >= 6 && c.SteveDiamond >= 6 && allItems >= 1,
                    () =>
                    {
                        if (!ConsumeRunItems(1, null)) return;
                        c.SteveIron -= 6; c.SteveDiamond -= 6;
                        c.SteveArmorTier = 3;
                        c.Shield = MathF.Min(_run.PlayerMaxHp, c.Shield + 120f);
                        _run.CollectItem("tarrasque");
                        ShowBanner("СКРАФЧЕНО: Незеритовый Доспех + Сердце Тарраски!", Color.Magenta, 4.0f);
                    }
                ),
                (
                    "5. Тотем Бессмертия (Totem of Undying)",
                    "Цена: 4 Дерева + 3 Железа + 2 Алмаза",
                    "Даёт Тотем Бессмертия в левую руку: при смертельном ударе спасает жизнь, восстанавливает 65% HP и взрывает врагов!",
                    Color.Gold,
                    c.SteveWood >= 4 && c.SteveIron >= 3 && c.SteveDiamond >= 2 && !c.SteveHasTotem,
                    () =>
                    {
                        c.SteveWood -= 4; c.SteveIron -= 3; c.SteveDiamond -= 2;
                        c.SteveHasTotem = true;
                        _run.HealPlayer(_run.PlayerMaxHp * 0.50f);
                        ShowBanner("СКРАФЧЕНО: Тотем Бессмертия экипирован!", Color.Gold, 3.8f);
                    }
                ),
                (
                    "6. Зачарованное Золотое Яблоко Нотча",
                    "Цена: 3 Дерева + 2 Камня + 2 Алмаза",
                    "Мгновенно восстанавливает 100% HP, даёт +80 Щита и создаёт «Копыто Пауля» (+скорость) и «Шприц Солдата» (+атака).",
                    Rgb(255, 215, 55),
                    c.SteveWood >= 3 && c.SteveStone >= 2 && c.SteveDiamond >= 2,
                    () =>
                    {
                        c.SteveWood -= 3; c.SteveStone -= 2; c.SteveDiamond -= 2;
                        _run.HealPlayer(_run.PlayerMaxHp);
                        c.Shield = MathF.Min(_run.PlayerMaxHp, c.Shield + 80f);
                        _run.CollectItem("hoof");
                        _run.CollectItem("syringe");
                        ShowBanner("СКРАФЧЕНО: Зачарованное Золотое Яблоко (+100% HP, Копыто и Шприц)!", Color.Gold, 3.8f);
                    }
                ),
                (
                    "7. Шахтёрская Ковка: Необычный Предмет (Зелёный)",
                    "Цена: 4 Дерева + 4 Камня + 2 Железа",
                    "Выковывает из добытой руды случайный зелёный предмет (Ракета AT-G, Укулеле, Кольцо Кьяро/Рунальда, Перо Хопу или Коса).",
                    Rgb(80, 235, 95),
                    c.SteveWood >= 4 && c.SteveStone >= 4 && c.SteveIron >= 2,
                    () =>
                    {
                        c.SteveWood -= 4; c.SteveStone -= 4; c.SteveIron -= 2;
                        string[] greenPool = { "atg_missile", "ukulele", "kjaro_band", "runald_band", "harvester_scythe", "feather" };
                        string rolled = greenPool[Random.Shared.Next(0, greenPool.Length)];
                        _run.CollectItem(rolled);
                    }
                ),
                (
                    "8. Перековка на Наковальне: Легендарный Предмет (Красный)",
                    "Цена: 2 Белых предмета + 4 Камня + 3 Железа + 2 Алмаза",
                    "Переплавляет 2 обычных предмета из инвентаря и ресурсы карты в случайный Легендарный (Красный) артефакт!",
                    Rgb(250, 65, 65),
                    whiteItems >= 2 && c.SteveStone >= 4 && c.SteveIron >= 3 && c.SteveDiamond >= 2,
                    () =>
                    {
                        if (!ConsumeRunItems(2, "white")) return;
                        c.SteveStone -= 4; c.SteveIron -= 3; c.SteveDiamond -= 2;
                        string[] redPool = { "clover", "behemoth", "tesla_coil", "alien_head", "rabadons_deathcap", "blade_ruined_king" };
                        string rolled = redPool[Random.Shared.Next(0, redPool.Length)];
                        _run.CollectItem(rolled);
                    }
                )
            };

            Vector2 mp = Raylib.GetMousePosition();
            string hoverTitle = "НАВЕДИТЕ КУРСОР НА РЕЦЕПТ ВЕРСТАКА";
            string hoverCost = "Ломайте пол (ЛКМ вниз / ПКМ), стены, скалы, сундуки и алтари киркой Стива для добычи ресурсов";
            string hoverDesc = "После победы над Боссом Телепорта и зарядки портала рядом всегда спавнится Верстак для спокойного крафта!";
            Color hoverCol = Rgb(80, 235, 245);

            for (int i = 0; i < recipes.Length; i++)
            {
                var r = recipes[i];
                int row = i / 2, col = i % 2;
                int bx = mx + 24 + col * 442;
                int by = my + 108 + row * 90;
                Rectangle rect = new(bx, by, 430, 80);

                if (Raylib.CheckCollisionPointRec(mp, rect))
                {
                    hoverTitle = r.Title;
                    hoverCost = $"{r.CostText}   —   {(r.CanCraft ? "ДОСТУПНО ДЛЯ КРАФТА ✓" : "НЕДОСТАТОЧНО РЕСУРСОВ ИЛИ ПРЕДМЕТОВ")}";
                    hoverDesc = r.Desc;
                    hoverCol = r.CanCraft ? r.Col : Color.Red;
                }

                string btnTxt = $"{r.Title}\n{r.CostText}";
                Color bgCol = r.CanCraft ? Rgb(24, 38, 58, 245) : Rgb(18, 20, 28, 230);
                Color brdCol = r.CanCraft ? r.Col : Color.Gray;

                if (Engine.DrawButton(rect, btnTxt, bgCol, Rgb(46, 78, 126),
                    r.CanCraft ? Color.White : Color.Gray, 13f, brdCol))
                {
                    if (r.CanCraft)
                    {
                        SoundManager.Play("item_legendary", 0.95f);
                        r.DoCraft();
                    }
                    else
                    {
                        SoundManager.Play("ui_error", 0.85f);
                        ShowBanner($"НЕ ХВАТАЕТ РЕСУРСОВ! ({r.CostText})", Color.Red, 2.5f);
                    }
                }
            }

            // Нижняя карточка подробного описания рецепта
            int descY = my + mh - 110;
            Raylib.DrawRectangle(mx + 24, descY, mw - 48, 92, Rgb(18, 24, 40, 248));
            Raylib.DrawRectangle(mx + 24, descY, 4, 92, hoverCol);
            Engine.DrawTextLeft(hoverTitle, mx + 38, descY + 10, 17f, hoverCol);
            Engine.DrawTextLeft(hoverCost, mx + 38, descY + 34, 14f, Color.Gold);
            DrawWrappedText(hoverDesc, mx + 38, descY + 58, 95, 14f, Color.White, maxLines: 2, lineStep: 18f);
        }

        

        private static void DrawLogbookUI()
        {
            if (_logDetailOpen)
            {
                UpdateAndDrawLogbookDetailModal();
                return;
            }

            DrawHeader();
            (string id, string title)[] tabs =
            {
                ("items", "АРТЕФАКТЫ"), ("survivors", "ПЕРСОНАЖИ"), ("monsters", "ВРАГИ"),
                ("bosses", "БОССЫ"), ("stages", "ЛОКАЦИИ"), ("achievements", "ДОСТИЖЕНИЯ"), ("modifiers", "МОДИФИКАТОРЫ")
            };

            for (int i = 0; i < tabs.Length; i++)
            {
                var (cid, title) = tabs[i];
                bool active = _logCategory == cid;
                if (Engine.DrawButton(new Rectangle(36 + i * 142, 72, 136, 38), title,
                    active ? Rgb(75, 115, 185) : Rgb(32, 40, 62), Rgb(95, 135, 210), Color.White, 15f))
                {
                    _logCategory = cid;
                    _logPage = 0;
                    var list = GetLogEntries(cid);
                    if (list.Count > 0) SelectLogbookEntry(cid, list[0].Key);
                }
            }

            Raylib.DrawRectangle(36, 122, 700, 700, Rgb(12, 16, 28, 240));
            Engine.DrawTextLeft("Двойной клик по ячейке (или Enter) — открыть полную страницу с лором и 3D-моделью!", 54, 126, 12f, Rgb(145, 215, 255));
            var entries = GetLogEntries(_logCategory);
            int perPage = 18;
            int totalPages = Math.Max(1, (entries.Count - 1) / perPage + 1);
            _logPage = Math.Clamp(_logPage, 0, totalPages - 1);

            int startIdx = _logPage * perPage;
            for (int i = 0; i < perPage && startIdx + i < entries.Count; i++)
            {
                var e = entries[startIdx + i];
                int r = i / 6, c = i % 6;
                Rectangle box = new(54 + c * 110, 146 + r * 86, 100, 74);
                bool isSel = e.Key == _logSelectedKey;
                string tileLabel = _logCategory == "achievements"
                    ? (e.Unlocked ? "✓ ACH" : "ACH")
                    : (e.Unlocked ? e.Short : $"[{e.Short}]");
                if (Engine.DrawButton(box, tileLabel,
                    e.Unlocked ? Rgb(24, 32, 48) : Rgb(18, 20, 30), e.Color,
                    e.Unlocked ? e.Color : Rgb(165, 178, 198), 15f, isSel ? Color.Yellow : e.Color))
                {
                    SelectLogbookEntry(_logCategory, e.Key);
                    RegisterLogbookTileClick(_logCategory, e.Key);
                }
            }

            if (Engine.DrawButton(new Rectangle(140, 412, 130, 34), "<- ПРЕД.", Rgb(40, 52, 78), Rgb(65, 85, 120), Color.White, 15f))
                _logPage = Math.Clamp(_logPage - 1, 0, totalPages - 1);
            Engine.DrawTextCentered($"Стр. {_logPage + 1} / {totalPages}", 380, 429, 17f, Color.White);
            if (Engine.DrawButton(new Rectangle(490, 412, 130, 34), "СЛЕД. ->", Rgb(40, 52, 78), Rgb(65, 85, 120), Color.White, 15f))
                _logPage = Math.Clamp(_logPage + 1, 0, totalPages - 1);

            if (Engine.DrawButton(new Rectangle(54, 668, 300, 44), "<- В ГЛАВНОЕ МЕНЮ [ESC]", Rgb(65, 72, 95), Rgb(95, 105, 135), Color.White, 15f))
            {
                _screen = "main";
                SelectCharacter(_selectedChar);
            }

            // Нижняя карточка описания выбранной записи + кнопка открытия полной страницы с лором и 3D-вращением
            var cur = entries.Find(x => x.Key == _logSelectedKey);
            if (cur.Key != null)
            {
                if (Engine.DrawButton(new Rectangle(366, 668, 352, 44), "ОТКРЫТЬ СТРАНИЦУ И ЛОР [2x КЛИК / ENTER]",
                    Rgb(42, 86, 148), Rgb(64, 122, 198), Color.Yellow, 14f, Color.SkyBlue)
                    || Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    OpenLogbookDetailPage(_logCategory, cur.Key, 0);
                    return;
                }

                Raylib.DrawRectangle(54, 456, 664, 202, Rgb(18, 24, 40, 245));
                Raylib.DrawRectangle(54, 456, 664, 4, cur.Color);
                Engine.DrawTextLeft(cur.Title, 70, 468, 21f, cur.Color);
                Engine.DrawTextLeft(cur.Sub, 70, 498, 15f, Color.Gold);
                DrawWrappedText(cur.Desc, 70, 524, 72, 15f, Color.White, maxLines: 5, lineStep: 21f);
            }
        }

        private static void DrawRunSummaryUI()
        {
            DrawHeader();
            int sw = Raylib.GetScreenWidth();
            Color titleCol = _lastRunVictory ? Color.Gold : Rgb(245, 65, 65);
            string titleStr = _lastRunVictory ? "ПОБЕДА :: ЗАБЕГ УСПЕШНО ЗАВЕРШЁН!" : "ПОРАЖЕНИЕ :: ОТЧЁТ О ЗАБЕГЕ";

            Raylib.DrawRectangle(48, 100, 680, 680, Rgb(12, 16, 28, 242));
            Raylib.DrawRectangle(48, 100, 680, 4, titleCol);
            Engine.DrawTextLeft(titleStr, 72, 122, 26f, titleCol);
            if (!_lastRunVictory)
            {
                DrawKillerMobTopRightWidget(sw - 472, 74, 444, allow3DToggle: true);
            }

            var cfg = SurvivorDatabase.Classes[_selectedChar];
            int mins = (int)(_lastRunTime / 60f), secs = (int)(_lastRunTime % 60f);
            Engine.DrawTextLeft($"Выживший: {cfg.Name} ({cfg.Subtitle})", 72, 168, 19f, cfg.BodyColor);
            Engine.DrawTextLeft($"Время выживания: {mins:00}:{secs:00}   |   Пройдено этапов: {_lastRunStage}", 72, 204, 18f, Color.White);
            Engine.DrawTextLeft($"Уничтожено врагов: {RunKills}", 72, 240, 18f, Color.Lime);
            Engine.DrawTextLeft($"Всего нанесено урона: {(int)RunDamageDealt}", 72, 272, 18f, Color.SkyBlue);
            Engine.DrawTextLeft($"Собрано золота за забег: ${RunGoldEarned}", 72, 304, 18f, Color.Gold);

            Engine.DrawTextLeft("СОБРАННЫЕ АРТЕФАКТЫ:", 72, 352, 18f, Color.Yellow);
            if (_lastRunItems.Count == 0)
            {
                Engine.DrawTextLeft("Предметы не были собраны", 72, 384, 16f, Color.Gray);
            }
            else
            {
                for (int i = 0; i < _lastRunItems.Count && i < 24; i++)
                {
                    var (k, cnt) = _lastRunItems[i];
                    var info = GameData.ItemPool[k];
                    int r = i / 8, c = i % 8;
                    int bx = 72 + c * 76, by = 386 + r * 54;
                    Raylib.DrawRectangle(bx, by, 68, 46, Rgb(24, 32, 48, 230));
                    Raylib.DrawRectangle(bx, by, 68, 3, info.Color);
                    Engine.DrawTextCentered(info.Short, bx + 34, by + 15, 14f, info.Color);
                    Engine.DrawTextCentered($"x{cnt}", bx + 34, by + 33, 14f, Color.White);
                }
            }

            if (Engine.DrawButton(new Rectangle(72, 690, 310, 56), "В ЛОББИ ВЫЖИВШИХ [ENTER]", Rgb(38, 115, 70), Rgb(55, 160, 95), Color.White, 18f, Color.Lime)
                || Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                _screen = "lobby";
                SelectCharacter(_selectedChar);
            }
            if (Engine.DrawButton(new Rectangle(402, 690, 290, 56), "В ГЛАВНОЕ МЕНЮ", Rgb(65, 72, 95), Rgb(95, 105, 135), Color.White, 18f))
            {
                _net.StopSession();
                _lobbyPlayers.Clear();
                _screen = "main";
            }
        }

        private struct LogEntryView
        {
            public string Key, Short, Title, Sub, Desc;
            public Color Color;
            public bool Unlocked;
        }

        private static System.Collections.Generic.List<LogEntryView> GetLogEntries(string cat)
        {
            var list = new System.Collections.Generic.List<LogEntryView>();
            var prog = Data.ProgressData.Instance;

            if (cat == "items")
            {
                foreach (var kv in Data.GameData.ItemPool)
                {
                    if (kv.Key == "tonic_affliction") continue;
                    var v = kv.Value;
                    bool unl = prog.UnlockedItems.Contains(kv.Key);
                    var itemAch = Data.GameData.GetItemUnlockAchievement(kv.Key);
                    string lockedHow = itemAch != null
                        ? $"{v.Desc}\nКак открыть: «{itemAch.Name}» — {itemAch.Desc}"
                        : v.Desc;
                    list.Add(new LogEntryView
                    {
                        Key = kv.Key, Short = v.Short, Color = v.Color,
                        Unlocked = unl,
                        Title = unl ? v.Name : $"{v.Name} [ЗАКРЫТО]",
                        Sub = unl ? $"Редкость: {v.TierName}  |  Код: [{v.Short}]  |  ОТКРЫТО ✓" : $"Редкость: {v.TierName}  |  Статус: НЕ ОТКРЫТО",
                        Desc = unl ? v.Desc : lockedHow
                    });
                }
            }
            else if (cat == "survivors")
            {
                foreach (var k in SurvivorDatabase.GetVisibleRosterKeys())
                {
                    var v = SurvivorDatabase.Classes[k];
                    bool isUnl = prog.IsCharacterUnlocked(k);
                    var charAch = Data.GameData.GetCharacterUnlockAchievement(k);
                    string howTxt = charAch != null ? $"Испытание: {charAch.Desc}\nНаграда: {charAch.RewardText}" : "Секретный персонаж корабля Safe Travels.";
                    list.Add(new LogEntryView
                    {
                        Key = k, Short = v.Icon, Color = v.BodyColor, Unlocked = isUnl,
                        Title = isUnl ? $"{v.Name} — {v.Subtitle}" : $"{v.Name} [ЗАКРЫТО]",
                        Sub = isUnl
                            ? $"HP: {v.Hp}  |  Скорость: {v.Speed}  |  Урон: {v.Damage}  |  Атака: {v.FireRate}/с"
                            : $"Статус: НЕ ОТКРЫТ  |  {v.Subtitle}",
                        Desc = isUnl
                            ? $"{v.Skills["m1"].Name} / {v.Skills["m2"].Name} / {v.Skills["shift"].Name} / {v.Skills["r"].Name}"
                            : howTxt
                    });
                }
            }
            else if (cat is "monsters" or "bosses")
            {
                foreach (var kv in Data.GameData.MonsterLog)
                {
                    bool isBoss = Data.GameData.IsKnownBossType(kv.Key);
                    if ((cat == "bosses") != isBoss) continue;
                    var m = kv.Value;
                    bool isTrialBoss = Array.IndexOf(Data.GameData.TrialBossTypes, kv.Key) >= 0;
                    string rankTag = kv.Key == "mithrix" ? " [ФИНАЛЬНЫЙ БОСС]" : (isTrialBoss ? " [БОСС ИСПЫТАНИЯ]" : (isBoss ? " [СТРАЖ ТЕЛЕПОРТА]" : ""));
                    list.Add(new LogEntryView
                    {
                        Key = kv.Key, Short = kv.Key[..Math.Min(5, kv.Key.Length)].ToUpper(),
                        Color = kv.Key == "mithrix" ? Color.SkyBlue : (isBoss ? Color.Red : Rgb(255, 140, 60)),
                        Unlocked = true, Title = m.Name,
                        Sub = $"Базовое HP: {m.Hp}   |   Базовый урон: {m.Damage}{rankTag}", Desc = m.Desc
                    });
                }
            }
            else if (cat == "stages")
            {
                var all = new System.Collections.Generic.List<Data.StagePalette>(Data.GameData.StagePalettes);
                foreach (var spKey in new[] { "safe_travels", "bazaar", "moon", "pizzeria", "black_mesa", "qliphoth", "hell_cybergrind", "tartarus", "hakurei_shrine", "tracen_derby" })
                {
                    if (Data.GameData.SpecialPalettes.TryGetValue(spKey, out var spPal))
                        all.Add(spPal);
                }
                foreach (var st in all)
                {
                    bool disc = prog.DiscoveredStages.Contains(st.Id) || (st.Id == "safe_travels" && IsSafeTravelsUnlocked());
                    int layerNum = Data.GameData.GetStageBiomeLayerNumber(st.Id);
                    string poolTag = layerNum > 0 ? $"Пул Этапа {layerNum}" : "Внецикловая / Особая";
                    list.Add(new LogEntryView
                    {
                        Key = st.Id, Short = st.Id[..Math.Min(5, st.Id.Length)].ToUpper(),
                        Color = st.Accent, Unlocked = disc,
                        Title = st.Name,
                        Sub = $"{poolTag}  |  Статус: {(disc ? "ИССЛЕДОВАНА ✓" : "ЕЩЁ НЕ ПОСЕЩЕНА")}",
                        Desc = st.Desc
                    });
                }
            }
            else if (cat == "achievements")
            {
                foreach (var kv in Data.GameData.Achievements)
                {
                    bool unl = prog.UnlockedAchievements.Contains(kv.Key);
                    string rewSummary = GetAchievementRewardSummary(kv.Value, _selectedChar);
                    list.Add(new LogEntryView
                    {
                        Key = kv.Key, Short = "ACH", Color = unl ? Color.Lime : Color.Orange,
                        Unlocked = unl, Title = kv.Value.Name,
                        Sub = $"{(unl ? "ВЫПОЛНЕНО ✓" : "НЕ ВЫПОЛНЕНО")}  |  Награда: {rewSummary}",
                        Desc = $"Как получить: {kv.Value.Desc}\nНаграда: {rewSummary}"
                    });
                }
            }
            else if (cat == "modifiers")
            {
                foreach (var dk in Data.GameData.OrderedDifficultyKeys)
                {
                    var d = Data.GameData.Difficulties[dk];
                    list.Add(new LogEntryView
                    {
                        Key = dk, Short = d.Icon, Color = d.Color, Unlocked = true,
                        Title = $"СЛОЖНОСТЬ: {d.Name}",
                        Sub = $"{d.Subtitle}  |  Множитель роста сложности: x{d.ScaleMult:F2}",
                        Desc = d.Desc
                    });
                }
                foreach (var ak in Data.GameData.OrderedArtifactKeys)
                {
                    var a = Data.GameData.RunArtifacts[ak];
                    bool isOn = Data.GameData.HasArtifact(ak);
                    list.Add(new LogEntryView
                    {
                        Key = ak, Short = a.Short, Color = a.Color, Unlocked = true,
                        Title = a.Name,
                        Sub = $"{a.Subtitle}  |  В лобби: {(isOn ? "ВКЛЮЧЁН ✓" : "ВЫКЛЮЧЕН")}",
                        Desc = a.Desc
                    });
                }
            }
            return list;
        }
    }
}