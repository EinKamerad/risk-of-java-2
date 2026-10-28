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
    public class GameRunContext : IPlayerContext
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
        public bool CanPlayerInteract => !(CharKey == "mindustry" && Combat != null && Combat.MindustryBuildMode);
        public float MoonDetonationTimer { get; set; } = 0f;
        public bool MithrixSpawned = false;

        public float ElapsedTime = 0f;
        public float DirectorCredits = 0f;
        public float SpawnTimer = 1.0f;
        public string StageKinType = "";
        public float VengeanceTimer = 85.0f;

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

            // 1. Глобальное достижение и предмет для всех персонажей
            if (!prog.UnlockedAchievements.Contains(achKey))
            {
                prog.UnlockedAchievements.Add(achKey);
                if (!string.IsNullOrEmpty(ach.UnlocksItem) && GameData.ItemPool.TryGetValue(ach.UnlocksItem, out var itemInfo))
                {
                    prog.UnlockedItems.Add(ach.UnlocksItem);
                    unlockedRewards.Add($"Предмет «{itemInfo.Name}»");
                }
            }
            else if (!string.IsNullOrEmpty(ach.UnlocksItem) && !prog.UnlockedItems.Contains(ach.UnlocksItem) && GameData.ItemPool.TryGetValue(ach.UnlocksItem, out var itemInfo))
            {
                prog.UnlockedItems.Add(ach.UnlocksItem);
                unlockedRewards.Add($"Предмет «{itemInfo.Name}»");
            }

            // 2. Персональные альтернативные навыки и скины только для текущего выжившего (CharKey)
            if (SurvivorDatabase.Classes.TryGetValue(CharKey, out var cfg))
            {
                foreach (var slot in new[] { "m1", "m2", "shift", "r" })
                {
                    if (!cfg.SkillVariants.TryGetValue(slot, out var variants)) continue;
                    for (int vi = 1; vi < variants.Count; vi++)
                    {
                        var vSkill = variants[vi];
                        string skillUnlockId = $"{CharKey}_{slot}_{vi}";
                        if (vSkill.UnlockAchKey == achKey && !prog.UnlockedSkills.Contains(skillUnlockId))
                        {
                            prog.UnlockedSkills.Add(skillUnlockId);
                            unlockedRewards.Add($"Навык [{cfg.Name}] «{vSkill.Name}»");
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

            if (unlockedRewards.Count > 0)
            {
                prog.Save();
                ShowBanner($"ДОСТИЖЕНИЕ «{ach.Name}» :: ОТКРЫТО: {string.Join(" + ", unlockedRewards)}!", Color.Gold, 4.5f);
            }
        }

        public void HealPlayer(float amount)
        {
            if (amount <= 0f) return;
            if (GameData.SelectedDifficulty == "eclipse") amount *= 0.65f;
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
                ShowBanner($"ЛУННЫЙ ТОНИК АКТИВЕН (25 СЕК)! ВСЕ ИСПОРЧЕННЫЕ ТОНИКИ ВОССТАНОВЛЕНЫ (x{Items["spinel_tonic"]})!", Rgb(65, 195, 255), 3.8f);
            }
            else
            {
                if (!ItemOrder.Contains(itemKey)) ItemOrder.Add(itemKey);
                Items[itemKey] = (Items.TryGetValue(itemKey, out int c) ? c : 0) + 1;
                var info = GameData.ItemPool[itemKey];
                ShowBanner($"ПОДОБРАНО: {info.Name} ({info.Desc})", info.Color, 3.2f);
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
            ShowBanner($"ДЕЙСТВИЕ ТОНИКА ИСТЕКЛО! ТОНИКИ ИСПОРТИЛИСЬ (x{Items["tonic_affliction"]}): ВСЕ СТАТЫ СНИЖЕНЫ!", Rgb(175, 95, 255), 4.0f);
        }

        public void SpawnItemDrop(Vector3 pos, string? itemKey = null, string poolType = "small_chest")
        {
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

        public void ShowBanner(string msg, Color col, float duration = 2.5f)
            => Program.ShowBanner(msg, col, duration);

        public void SpawnBoss(Vector3 pos, string? bossType = null, int? forcedNetId = null, bool fromNetwork = false)
        {
            var rng = new Random();
            string bType = bossType != null && Array.IndexOf(GameData.BossTypes, bossType) >= 0
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

        public void EnterBazaar() => Program.LoadStage(Stage, "bazaar");
        public void EnterMoonStage() => Program.LoadStage(Stage, "moon");

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
            UnlockAchievement("ach_stage4");
            UnlockAchievement("ach_boss_hunter");
            Vector3 shipPos = new(0, 0, -52.0f);
            StageEntities.Add(new EscapeShip(shipPos));
            StageEntities.Add(new LunarEscapePad(new Vector3(0, Physics.ColosseumY, -46.0f), shipPos));
            ShowBanner("МИТРИКС ПОВЕРЖЕН! ПРЫГАЙТЕ В ЗЕЛЁНЫЙ ЛУЧ НА КРАЮ КОЛИЗЕЯ К КОРАБЛЮ (75 СЕК)!", Color.Lime, 6.5f);
        }

        public void OnRunVictory()
            => Program.OnVictory();
    }
    public static class Program
    {
        private static string _screen = "main"; // "main", "mp_connect", "settings", "lobby", "logbook", "in_game"
        private static string _selectedChar = "commando";
        private static string _lobbyTab = "overview"; // "overview", "skills", "loadout"
        private static GameRunContext? _run;
        private static bool _paused = false;
        private static bool _isSpectating = false;
        private static int _spectateTargetIdx = 0;
        private static readonly HashSet<string> _deadCoopPlayers = new();
        private static readonly Queue<string> _commandPickerQueue = new();

        public static void EnqueueCommandPicker(string tier)
        {
            _commandPickerQueue.Enqueue(tier);
            Raylib.EnableCursor();
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
        }

        public static void TriggerDamageFlash(float dmg)
        {
            if (dmg > 0f)
                _damageFlashTimer = Math.Clamp(0.25f + dmg * 0.008f, 0.25f, 0.65f);
        }

        public static void RecordEnemyKill(int goldGain)
        {
            RunKills++;
            RunGoldEarned += goldGain;
            if (_run != null)
            {
                if (RunKills >= 12) _run.UnlockAchievement("ach_slayer");
                if (RunKills >= 25) _run.UnlockAchievement("ach_carnage");
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
            SetupDiorama();

            while (!Raylib.WindowShouldClose())
            {
                float dt = Math.Min(Raylib.GetFrameTime(), 0.05f);
                ProcessNetwork();
                if (_bannerTimer > 0f) _bannerTimer -= dt;
                if (_hitMarkerTimer > 0f) _hitMarkerTimer -= dt;
                if (_damageFlashTimer > 0f) _damageFlashTimer -= dt;

                if (_screen == "in_game")
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    {
                        _paused = !_paused;
                        if (_paused || _commandPickerQueue.Count > 0 || (_run != null && (SurvivorDatabase.IsMobaCharacter(_run.CharKey) || _run.CharKey == "mindustry")))
                            Raylib.EnableCursor();
                        else
                            Raylib.DisableCursor();
                    }

                    if (!_paused)
                    {
                        UpdateInGame(dt);
                        Engine.Update(dt);
                    }
                }
                else
                {
                    HandleInput(dt);
                    Engine.Update(dt);
                    UpdateDiorama(dt);
                    Engine.Camera.Position = new Vector3(0f, 2.15f, -8.6f);
                    Engine.Camera.FovY = _fov;
                    Engine.CameraEuler = new Vector3(5f, 0f, 0f);
                    Engine.UpdateCameraTargetFromEuler();

                    if (_screen == "lobby" && _net.Connected && _lobbyCountdownActive)
                    {
                        _lobbyCountdown = MathF.Max(0f, _lobbyCountdown - dt);
                        if (_net.Mode == "host" && _lobbyCountdown <= 0f)
                        {
                            _lobbyCountdownActive = false;
                            StartRun(_selectedChar);
                        }
                    }
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Engine.BackgroundColor);

                Raylib.BeginMode3D(Engine.Camera);
                Engine.Draw3D();
                Raylib.EndMode3D();

                Engine.Draw3DBillboardLabels(Engine.Camera);

                if (_screen == "in_game")
                {
                    DrawInGameHUD();
                    if (_paused) DrawPauseOverlay();
                }
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
            }
        }

        private static void BroadcastLobbyState()
        {
            if (!_net.Connected) return;
            _net.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "lobby_state",
                ["player_id"] = _net.MyId,
                ["char_key"] = _selectedChar,
                ["ready"] = _myReady,
                ["skin_idx"] = Data.ProgressData.Instance.GetSelectedSkin(_selectedChar),
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
                _previewBaseScale = 1.85f;
                _previewActor.Position = new Vector3(0, 1.35f, 0);
                _pedestalRing.Color = item.Color;
                ItemBuilder.BuildItemMesh(_previewActor, key);
            }
            else if (category == "survivors" && SurvivorDatabase.Classes.TryGetValue(key, out var surv))
            {
                _previewBaseScale = 1.0f;
                _previewActor.Position = new Vector3(0, 0.12f, 0);
                _pedestalRing.Color = surv.BodyColor;
                _previewSurvivorParts = SurvivorDatabase.BuildSurvivorMesh(_previewActor, key);
            }
            else if (category == "monsters")
            {
                _previewBaseScale = key is "parent" or "elder_lemurian" ? 0.58f : (key is "golem" or "greater_wisp" or "lunar_chimera" or "bison" or "brass_contraption" ? 0.76f : 0.95f);
                float yOff = key is "wisp" or "jellyfish" or "greater_wisp" or "lunar_chimera" or "brass_contraption" ? -0.75f : 0.12f;
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
                var pal = GameData.StagePalettes.Find(p => p.Id == key)
                          ?? (GameData.SpecialPalettes.TryGetValue(key, out var sp) ? sp : GameData.StagePalettes[0]);
                _previewBaseScale = 0.24f;
                _previewActor.Position = new Vector3(0, 0.45f, 0);
                _pedestalRing.Color = pal.Accent;
                var root = new Entity(parent: _previewActor);
                new Entity(parent: root, model: "sphere", scale: new(9.5f, 1.2f, 9.5f), position: new(0, -0.4f, 0), color: pal.Ground);
                new Entity(parent: root, model: "diamond", scale: new(10.0f, 0.25f, 10.0f), color: pal.Accent, unlit: true);
                string stType = key switch
                {
                    "ionia" => "torii",
                    "sundered" => "giant_mushroom",
                    "rallypoint" => "ice_spire",
                    "sulfur" => "sulfur_geyser",
                    "aqueduct" => "aqueduct_pillar",
                    "aphelian" => "sun_temple",
                    "wetland" => "swamp_root",
                    "meadow" => "astral_ring",
                    "roost" or "moon" => "arch",
                    "acres" or "bazaar" => "monolith",
                    _ => "crystals"
                };
                WorldStructuresBuilder.BuildMapStructure(root, stType, pal, Vector3.Zero, 25f);
            }
            else if (category == "achievements" && GameData.Achievements.TryGetValue(key, out var ach))
            {
                bool unlocked = ProgressData.Instance.UnlockedAchievements.Contains(key);
                _previewBaseScale = 1.45f;
                _previewActor.Position = new Vector3(0, 1.25f, 0);
                _pedestalRing.Color = unlocked ? Color.Gold : Color.Gray;
                if (ach.UnlocksItem != null)
                    ItemBuilder.BuildItemMesh(_previewActor, ach.UnlocksItem);
                else
                    new Entity(parent: _previewActor, model: "diamond", scale: new(0.75f, 1.15f, 0.75f), color: unlocked ? Color.Gold : Color.Gray, unlit: true);
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

            if (false)
            {
                // placeholder
            }
        }

        private static void TriggerLobbySkillPreview(string charKey, string slot, int variantIdx)
        {
            if (!SurvivorDatabase.Classes.TryGetValue(charKey, out var cfg)) return;
            cfg.SyncLoadout();

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
            if (cfg.SkillVariants.TryGetValue(slot, out var vars) && variantIdx >= 0 && variantIdx < vars.Count)
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

            bool isBossModel = key is "boss" or "vagrant" or "overlord" or "magma_worm" or "solus_control" or "mithrix";
            if (isBossModel)
            {
                _previewBaseScale = key == "magma_worm" ? 0.42f : 0.48f;
                float yOff = key is "vagrant" or "solus_control" ? -0.35f : (key == "magma_worm" ? 0.35f : 0.12f);
                _previewActor.Position = new Vector3(0f, yOff, 0f);
            }
            else
            {
                _previewBaseScale = key is "parent" or "elder_lemurian" ? 0.58f : (key is "golem" or "greater_wisp" or "lunar_chimera" or "bison" or "brass_contraption" ? 0.76f : 0.95f);
                float yOff = key is "wisp" or "jellyfish" or "greater_wisp" or "lunar_chimera" or "brass_contraption" ? -0.75f : 0.12f;
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
        private static void _EndSelectLogbookPlaceholder()
        {
            if (false)
            {
            }
        }

        private static void HandleInput(float dt)
        {
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
                            (Raylib.IsMouseButtonDown(MouseButton.Left) && Raylib.GetMouseX() > 760);
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

        private static void DrawMainMenuUI()
        {
            DrawHeader();
            Raylib.DrawRectangle(60, 140, 480, 500, Rgb(14, 18, 30, 238));
            Raylib.DrawRectangle(60, 140, 480, 4, Color.SkyBlue);
            Engine.DrawTextCentered("ГЛАВНОЕ МЕНЮ", 300, 180, 24f, Color.White);

            if (Engine.DrawButton(new Rectangle(100, 225, 400, 56), "ОДИНОЧНАЯ ИГРА", Rgb(38, 105, 70), Rgb(55, 150, 95), Color.White, 19f))
            {
                _net.StopSession();
                _lobbyPlayers.Clear();
                _myReady = false;
                _lobbyCountdownActive = false;
                _screen = "lobby";
                SelectCharacter(_selectedChar);
                SelectCharacter(_selectedChar);
            }
            if (Engine.DrawButton(new Rectangle(100, 298, 400, 56), "СЕТЕВАЯ ИГРА (КООПЕРАТИВ)", Rgb(40, 85, 145), Rgb(60, 125, 205), Color.White, 19f))
            {
                _typingCode = false;
                _netStatusText = $"Ваш локальный IP: {_net.HostIp}";
                _screen = "mp_connect";
            }
            if (Engine.DrawButton(new Rectangle(100, 371, 400, 56), "ЖУРНАЛ ВЫЖИВШЕГО", Rgb(125, 75, 165), Rgb(165, 105, 215), Color.White, 19f))
            {
                _screen = "logbook";
                var list = GetLogEntries(_logCategory);
                if (list.Count > 0) SelectLogbookEntry(_logCategory, list[0].Key);
            }
            if (Engine.DrawButton(new Rectangle(100, 444, 400, 56), "НАСТРОЙКИ", Rgb(65, 72, 95), Rgb(95, 105, 135), Color.White, 19f))
            {
                _screen = "settings";
            }
            if (Engine.DrawButton(new Rectangle(100, 517, 400, 56), "ВЫХОД ИЗ ИГРЫ", Rgb(135, 40, 45), Rgb(185, 60, 65), Color.White, 19f))
            {
                _net.StopSession();
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
            Raylib.DrawRectangle(60, 130, 580, 520, Rgb(14, 18, 30, 240));
            Raylib.DrawRectangle(60, 130, 580, 4, Color.Gold);
            Engine.DrawTextCentered("НАСТРОЙКИ ИГРЫ", 350, 168, 22f, Color.Gold);

            Engine.DrawTextLeft($"Чувствительность мыши: {(int)(_mouseSens * 250f)}", 95, 232, 18f, Color.White);
            if (Engine.DrawButton(new Rectangle(460, 224, 52, 40), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                _mouseSens = Math.Clamp(_mouseSens - 0.02f, 0.04f, 0.50f);
            if (Engine.DrawButton(new Rectangle(524, 224, 52, 40), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                _mouseSens = Math.Clamp(_mouseSens + 0.02f, 0.04f, 0.50f);

            Engine.DrawTextLeft($"Угол обзора камеры (FOV): {(int)_fov}", 95, 302, 18f, Color.White);
            if (Engine.DrawButton(new Rectangle(460, 294, 52, 40), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                _fov = Math.Clamp(_fov - 5f, 45f, 95f);
            if (Engine.DrawButton(new Rectangle(524, 294, 52, 40), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 20f))
                _fov = Math.Clamp(_fov + 5f, 45f, 95f);

            bool isFs = Raylib.IsWindowFullscreen();
            if (Engine.DrawButton(new Rectangle(160, 380, 380, 48), $"ПОЛНЫЙ ЭКРАН: {(isFs ? "ВКЛ" : "ВЫКЛ")}", Rgb(45, 65, 95), Rgb(65, 95, 135), Color.White, 18f))
                Raylib.ToggleFullscreen();

            if (Engine.DrawButton(new Rectangle(160, 560, 380, 50), "<- НАЗАД В ГЛАВНОЕ МЕНЮ", Rgb(60, 65, 82), Rgb(85, 92, 115), Color.White, 17f))
                _screen = "main";
        }

        private static void DrawLobbyUI()
        {
            DrawHeader();
            int sw = Raylib.GetScreenWidth();
            var cfg = SurvivorDatabase.Classes[_selectedChar];
            for (int i = 0; i < SurvivorDatabase.OrderedKeys.Length; i++)
            {
                string k = SurvivorDatabase.OrderedKeys[i];
                var scfg = SurvivorDatabase.Classes[k];
                int row = i / 10, col = i % 10;
                Rectangle rect = new(36 + col * 72, 70 + row * 46, 68, 38);
                bool isSel = k == _selectedChar;
                if (Engine.DrawButton(rect, scfg.Icon, Rgb(22, 28, 42, 245), scfg.BodyColor,
                    isSel ? Color.Yellow : Color.White, 14f, isSel ? Color.White : scfg.BodyColor))
                {
                    SelectCharacter(k);
                }
            }

            DrawLobbyCharacterPanel(cfg);
            if (_lobbyTab == "__legacy__")
            {
                Raylib.DrawRectangle(36, 172, 680, 640, Rgb(12, 16, 28, 238));
            Raylib.DrawRectangle(36, 172, 680, 4, cfg.BodyColor);
            Engine.DrawTextLeft(cfg.Name, 56, 188, 27f, cfg.BodyColor);
            Engine.DrawTextLeft(cfg.Subtitle, 56, 222, 18f, Rgb(175, 210, 245));
            Engine.DrawTextLeft($"HP: {cfg.Hp}   |   Скорость: {cfg.Speed}   |   Урон: {cfg.Damage}   |   Атака: {cfg.FireRate}/с", 56, 252, 17f, Color.Gold);

            float sy = 294f;
            foreach (var skKey in new[] { "m1", "m2", "shift", "r" })
            {
                if (!cfg.Skills.TryGetValue(skKey, out var sk)) continue;
                Engine.DrawTextLeft($"• {sk.Name} (КД: {sk.Cd}с)", 56, sy, 18f, Color.SkyBlue);
                Engine.DrawTextLeft($"  {sk.Desc}", 56, sy + 24f, 16f, Color.White);
                sy += 64f;
            }
            }

            // Панель статуса комнаты справа сверху
            var diffInfo = GameData.GetActiveDifficulty();
            string artSummary = GameData.ActiveArtifacts.Count == 0
                ? "Артефакты: Выкл. (0/10)"
                : $"Артефакты ({GameData.ActiveArtifacts.Count}): " + string.Join(", ", System.Linq.Enumerable.Select(GameData.ActiveArtifacts, ak => GameData.RunArtifacts[ak].Short));

            // Панель статуса комнаты и модификаторов справа сверху
            Raylib.DrawRectangle(sw - 470, 74, 434, 244, Rgb(12, 16, 28, 232));
            Raylib.DrawRectangle(sw - 470, 74, 434, 3, diffInfo.Color);
            Engine.DrawTextLeft($"СЛОЖНОСТЬ: {diffInfo.Name} (x{diffInfo.ScaleMult:F2})", sw - 452, 238, 15f, diffInfo.Color);
            Engine.DrawTextLeft(artSummary, sw - 452, 260, 13f, GameData.ActiveArtifacts.Count > 0 ? Color.Gold : Rgb(160, 180, 210));
            if (Engine.DrawButton(new Rectangle(sw - 452, 282, 398, 28), "4. ВЫБРАТЬ СЛОЖНОСТЬ И АРТЕФАКТЫ ЗАБЕГА ->",
                Rgb(34, 52, 86), Rgb(52, 82, 132), Color.Yellow, 13f, diffInfo.Color))
            {
                _lobbyTab = "modifiers";
            }

            if (_skillPreviewTimer > 0f || _lobbyTab is "loadout" or "skills")
            {
                Color pCol = _skillPreviewTimer > 0f ? _skillPreviewColor : cfg.GetActiveSkin().PrimaryColor;
                string pTxt = _skillPreviewTimer > 0f
                    ? _skillPreviewTitle
                    : "3D-ПРЕВЬЮ: Нажмите на способность в «Снаряжении» для просмотра анимации";
                Raylib.DrawRectangle(sw - 470, 326, 434, 36, Rgb(14, 20, 34, 238));
                Raylib.DrawRectangle(sw - 470, 326, 4, 36, pCol);
                Engine.DrawTextLeft(pTxt, sw - 456, 336, 13f, _skillPreviewTimer > 0f ? Color.Yellow : Rgb(185, 215, 245));
            }
            if (!_net.Connected)
            {
                Engine.DrawTextLeft("РЕЖИМ: ОДИНОЧНАЯ ИГРА", sw - 450, 90, 18f, Color.SkyBlue);
                Engine.DrawTextLeft($"Выживший: {cfg.Name}\nНажмите кнопку ниже для старта.", sw - 450, 122, 16f, Color.White);
            }
            else
            {
                Engine.DrawTextLeft($"КОМНАТА: {_net.RoomCode} ({_net.Mode?.ToUpper()})", sw - 450, 90, 18f, Color.Lime);
                int ly = 120;
                Engine.DrawTextLeft($"• ВЫ [{_net.MyId.ToUpper()}]: {cfg.Name} — [{(_myReady ? "ГОТОВ ✓" : "ВЫБИРАЕТ")}]", sw - 450, ly, 16f, Color.White);
                foreach (var kv in _lobbyPlayers)
                {
                    ly += 24;
                    string cName = SurvivorDatabase.Classes.TryGetValue(kv.Value.CharKey, out var sc) ? sc.Name : "COMMANDO";
                    Engine.DrawTextLeft($"• ИГРОК [{kv.Key.ToUpper()}]: {cName} — [{(kv.Value.Ready ? "ГОТОВ ✓" : "ВЫБИРАЕТ")}]", sw - 450, ly, 16f, Color.SkyBlue);
                }
                if (_lobbyCountdownActive)
                    Engine.DrawTextCentered($"ДО СТАРТА: {_lobbyCountdown:F1} СЕК.", sw - 253, 240, 18f, Color.Yellow);
            }

            string startLabel = !_net.Connected
                ? "НАЧАТЬ ЗАБЕГ (СТАРТ) [ENTER]"
                : (_myReady ? "ВЫ ГОТОВЫ ✓ (ОЖИДАНИЕ)" : "ГОТОВ К БОЮ! [ENTER]");
            if (Engine.DrawButton(new Rectangle(sw - 470, 700, 434, 62), startLabel,
                Rgb(38, 120, 70), Rgb(55, 165, 95), Color.White, 20f, Color.Lime) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                if (!_net.Connected)
                {
                    StartRun(_selectedChar);
                }
                else if (!_myReady)
                {
                    _myReady = true;
                    BroadcastLobbyState();
                    EvaluateLobbyCountdown();
                }
            }

            if (Engine.DrawButton(new Rectangle(sw - 470, 774, 434, 42), "<- ПОКИНУТЬ ЛОББИ [ESC]", Rgb(85, 40, 48), Rgb(125, 55, 65), Color.White, 16f))
            {
                _net.StopSession();
                _lobbyPlayers.Clear();
                _screen = "main";
            }
        }

        private static void DrawLobbyCharacterPanel(SurvivorConfig cfg)
        {
            cfg.SyncLoadout();
            var prog = Data.ProgressData.Instance;
            var activeSkin = cfg.GetActiveSkin();

            // Основная подложка левой панели персонажа
            Raylib.DrawRectangle(36, 168, 684, 650, Rgb(12, 16, 28, 242));
            Raylib.DrawRectangle(36, 168, 684, 4, activeSkin.PrimaryColor);

            // Шапка персонажа + активный скин
            Engine.DrawTextLeft(cfg.Name, 54, 180, 26f, activeSkin.PrimaryColor);
            Engine.DrawTextLeft(cfg.Subtitle, 54, 210, 16f, Rgb(175, 210, 245));
            string skinBadge = activeSkin.ChangesModel ? $"★ МОДЕЛЬ: {activeSkin.Name}" : $"ОБЛИК: {activeSkin.Name}";
            Engine.DrawTextLeft(skinBadge, 440, 186, 14f, activeSkin.ChangesModel ? Color.Gold : activeSkin.AccentColor);

            // 4 КНОПКИ ВКЛАДОК: ОБЗОР | НАВЫКИ | СНАРЯЖЕНИЕ | МОДИФИКАТОРЫ
            (string tabId, string tabTitle)[] lobbyTabs =
            {
                ("overview", "1. ОБЗОР"),
                ("skills", "2. НАВЫКИ"),
                ("loadout", "3. СНАРЯЖЕНИЕ"),
                ("modifiers", "4. МОДИФИКАТОРЫ")
            };
            for (int ti = 0; ti < lobbyTabs.Length; ti++)
            {
                var (tid, ttitle) = lobbyTabs[ti];
                bool isTab = _lobbyTab == tid;
                Rectangle tRect = new(54 + ti * 164, 236, 156, 38);
                if (Engine.DrawButton(tRect, ttitle,
                    isTab ? Rgb(45, 92, 158) : Rgb(22, 28, 44),
                    Rgb(65, 120, 195),
                    isTab ? Color.Yellow : Color.White, 14f,
                    isTab ? Color.Yellow : activeSkin.PrimaryColor))
                {
                    _lobbyTab = tid;
                }
            }

            if (_lobbyTab == "overview")
            {
                // Блок базовых характеристик
                Raylib.DrawRectangle(54, 286, 648, 68, Rgb(18, 24, 40, 235));
                Raylib.DrawRectangle(54, 286, 4, 68, Color.Gold);
                Engine.DrawTextLeft("БАЗОВЫЕ ПОКАЗАТЕЛИ ВЫЖИВШЕГО:", 68, 294, 14f, Color.Gold);
                Engine.DrawTextLeft($"Здоровье: {cfg.Hp} HP   |   Урон: {cfg.Damage}   |   Скорость: {cfg.Speed} м/с   |   Атака: {cfg.FireRate}/с",
                    68, 320, 17f, Color.White);

                // Блок биографии и стиля игры
                Raylib.DrawRectangle(54, 366, 648, 128, Rgb(18, 24, 40, 235));
                Raylib.DrawRectangle(54, 366, 4, 128, activeSkin.PrimaryColor);
                Engine.DrawTextLeft("ДОСЬЕ И ОСОБЕННОСТИ ГЕРОЯ:", 68, 376, 15f, activeSkin.PrimaryColor);
                Engine.DrawTextLeft(cfg.OverviewLore, 68, 404, 16f, Rgb(220, 232, 248));

                // Блок пассивной особенности
                Raylib.DrawRectangle(54, 506, 648, 118, Rgb(18, 24, 40, 235));
                Raylib.DrawRectangle(54, 506, 4, 118, Color.SkyBlue);
                Engine.DrawTextLeft($"ВРОЖДЁННАЯ ОСОБЕННОСТЬ :: {cfg.PassiveName.ToUpper()}", 68, 516, 16f, Color.SkyBlue);
                Engine.DrawTextLeft(cfg.PassiveDesc, 68, 544, 15f, Color.White);

                // Сводка выбранного снаряжения и облика
                Raylib.DrawRectangle(54, 636, 648, 164, Rgb(16, 22, 36, 235));
                Raylib.DrawRectangle(54, 636, 648, 2, activeSkin.AccentColor);
                Engine.DrawTextLeft($"АКТИВНЫЙ ОБЛИК: {activeSkin.Name}", 68, 648, 16f, activeSkin.ChangesModel ? Color.Gold : activeSkin.AccentColor);
                Engine.DrawTextLeft(activeSkin.Desc, 68, 672, 14f, Rgb(190, 205, 225));
                Engine.DrawTextLeft(
                    $"ЭКИПИРОВАНО: [ЛКМ] {cfg.Skills["m1"].Name}  |  [ПКМ] {cfg.Skills["m2"].Name}",
                    68, 702, 14f, Color.Yellow);
                Engine.DrawTextLeft(
                    $"             [Shift] {cfg.Skills["shift"].Name}  |  [R] {cfg.Skills["r"].Name}",
                    68, 724, 14f, Color.SkyBlue);

                if (Engine.DrawButton(new Rectangle(68, 752, 360, 38), "НАСТРОИТЬ СПОСОБНОСТИ И СКИНЫ ->",
                    Rgb(38, 78, 128), Rgb(55, 110, 175), Color.White, 15f, Color.SkyBlue))
                {
                    _lobbyTab = "loadout";
                }
            }
            else if (_lobbyTab == "skills")
            {
                // Пассивная способность сверху
                Raylib.DrawRectangle(54, 286, 648, 86, Rgb(18, 26, 44, 235));
                Raylib.DrawRectangle(54, 286, 4, 86, Color.Gold);
                Engine.DrawTextLeft($"[ПАССИВКА] {cfg.PassiveName}", 68, 294, 17f, Color.Gold);
                Engine.DrawTextLeft(cfg.PassiveDesc, 68, 320, 14f, Rgb(220, 230, 245));

                // 4 выбранные игроком активные способности
                (string slot, string bindName, Color slotCol)[] slotInfos =
                {
                    ("m1", "ЛКМ / ОСНОВНАЯ", Color.Yellow),
                    ("m2", "ПКМ / ВТОРИЧНАЯ", Color.Orange),
                    ("shift", "SHIFT / УТИЛИТА", Color.SkyBlue),
                    ("r", "R / ОСОБАЯ (УЛЬТА)", Color.Magenta)
                };

                float sy = 382f;
                foreach (var (slot, bindName, slotCol) in slotInfos)
                {
                    if (!cfg.Skills.TryGetValue(slot, out var sk)) continue;
                    int varIdx = prog.GetSkillVariant(_selectedChar, slot);
                    string varTag = varIdx == 0 ? "[СТАНД.]" : "[АЛЬТ.]";

                    Raylib.DrawRectangle(54, (int)sy, 648, 98, Rgb(18, 24, 38, 235));
                    Raylib.DrawRectangle(54, (int)sy, 4, 98, slotCol);
                    Engine.DrawTextLeft($"[{bindName}]  {sk.Name}  {varTag}", 68, sy + 8f, 17f, slotCol);
                    Engine.DrawTextLeft($"КД: {sk.Cd} сек.", 455, sy + 10f, 14f, Color.Gold);
                    if (Engine.DrawButton(new Rectangle(564, (int)sy + 6, 126, 24), "▶ АНИМАЦИЯ",
                        Rgb(34, 62, 104), Rgb(54, 96, 156), Color.Yellow, 12f, slotCol))
                    {
                        TriggerLobbySkillPreview(_selectedChar, slot, varIdx);
                    }
                    Engine.DrawTextLeft(sk.Desc, 68, sy + 36f, 15f, Color.White);
                    sy += 106f;
                }
            }
            else if (_lobbyTab == "loadout")
            {
                Vector2 mp = Raylib.GetMousePosition();
                string hoverTitle = "НАВЕДИТЕ КУРСОР НА СПОСОБНОСТЬ ИЛИ СКИН";
                string hoverSub = "Выберите комбинацию навыков (Loadout) и облик для забега";
                string hoverDesc = "Все изменения мгновенно сохраняются в профиле и отображаются на 3D-модели.";
                Color hoverCol = Color.SkyBlue;

                // 1. ВЫБОР СКИНА (ОБЛИКА)
                int rowY = 286;
                Raylib.DrawRectangle(54, rowY, 648, 74, Rgb(18, 24, 40, 235));
                Raylib.DrawRectangle(54, rowY, 4, 74, Color.Gold);
                Engine.DrawTextLeft("ОБЛИК ГЕРОЯ:", 66, rowY + 8, 15f, Color.Gold);
                Engine.DrawTextLeft("(Рескины и 3D-модели)", 66, rowY + 30, 12f, Rgb(165, 185, 210));

                int curSkinIdx = prog.GetSelectedSkin(_selectedChar);
                for (int si = 0; si < cfg.Skins.Count; si++)
                {
                    var skDef = cfg.Skins[si];
                    bool unlocked = prog.IsSkinUnlocked(_selectedChar, si, skDef.UnlockAchievementKey);
                    bool selected = curSkinIdx == si && unlocked;
                    Rectangle sRect = new(222 + si * 158, rowY + 10, 150, 54);

                    string btnLabel = unlocked
                        ? (skDef.ChangesModel ? $"★ {skDef.Name}" : skDef.Name)
                        : $"[ЗАКР.] {skDef.Name}";

                    Color bgCol = selected
                        ? Rgb(48, 95, 160)
                        : (unlocked ? Rgb(26, 34, 52) : Rgb(18, 20, 28));
                    Color borderCol = selected
                        ? Color.Yellow
                        : (unlocked ? skDef.PrimaryColor : Color.Gray);

                    if (Raylib.CheckCollisionPointRec(mp, sRect))
                    {
                        hoverCol = unlocked ? (skDef.ChangesModel ? Color.Gold : skDef.PrimaryColor) : Color.Red;
                        string modelType = skDef.ChangesModel ? "ОСОБАЯ 3D-МОДЕЛЬ МАСТЕРА" : (si == 0 ? "КЛАССИЧЕСКИЙ ОБЛИК" : "РЕСКИН ЗА ДОСТИЖЕНИЕ");
                        hoverTitle = $"{skDef.Name}  [{modelType}]";
                        hoverSub = unlocked
                            ? $"Статус: ОТКРЫТО ✓ ({skDef.UnlockConditionText})"
                            : $"ЗАБЛОКИРОВАНО — Требуется: {skDef.UnlockConditionText}";
                        hoverDesc = skDef.Desc;
                    }

                    if (Engine.DrawButton(sRect, btnLabel, bgCol, Rgb(65, 115, 185),
                        unlocked ? (selected ? Color.Yellow : Color.White) : Color.Gray, 13f, borderCol))
                    {
                        if (unlocked)
                        {
                            prog.SetSelectedSkin(_selectedChar, si);
                            SelectCharacter(_selectedChar);
                        }
                        else
                        {
                            ShowBanner($"СКИН ЗАКРЫТ! УСЛОВИЕ: {skDef.UnlockConditionText}", Color.Red, 2.8f);
                        }
                    }
                }

                // 2. 4 СТРОКИ ВЫБОРА СПОСОБНОСТЕЙ (ЛКМ, ПКМ, SHIFT, R)
                (string slot, string slotLabel, Color slotCol)[] loadoutRows =
                {
                    ("m1", "ЛКМ (ОСНОВНАЯ)", Color.Yellow),
                    ("m2", "ПКМ (ВТОРИЧНАЯ)", Color.Orange),
                    ("shift", "SHIFT (УТИЛИТА)", Color.SkyBlue),
                    ("r", "R (ОСОБАЯ / УЛЬТА)", Color.Magenta)
                };

                for (int ri = 0; ri < loadoutRows.Length; ri++)
                {
                    var (slot, slotLabel, slotCol) = loadoutRows[ri];
                    int ry = 368 + ri * 70;
                    Raylib.DrawRectangle(54, ry, 648, 64, Rgb(18, 24, 40, 235));
                    Raylib.DrawRectangle(54, ry, 4, 64, slotCol);
                    Engine.DrawTextLeft(slotLabel, 66, ry + 22, 15f, slotCol);

                    if (!cfg.SkillVariants.TryGetValue(slot, out var variants)) continue;
                    int curVar = prog.GetSkillVariant(_selectedChar, slot);

                    for (int vi = 0; vi < variants.Count; vi++)
                    {
                        var vSkill = variants[vi];
                        bool unlocked = prog.IsSkillUnlocked(_selectedChar, slot, vi);
                        bool isSel = curVar == vi && unlocked;
                        Rectangle vRect = new(222 + vi * 238, ry + 8, 230, 48);

                        string btnText = unlocked ? vSkill.Name : $"[ЗАКР.] {vSkill.Name}";
                        Color bgCol = isSel
                            ? Rgb(42, 88, 150)
                            : (unlocked ? Rgb(24, 30, 46) : Rgb(18, 20, 28));
                        Color borderCol = isSel
                            ? Color.Yellow
                            : (unlocked ? slotCol : Color.Gray);

                        if (Raylib.CheckCollisionPointRec(mp, vRect))
                        {
                            hoverCol = unlocked ? slotCol : Color.Red;
                            hoverTitle = $"[{slotLabel}]  {vSkill.Name}";
                            hoverSub = unlocked
                                ? $"Перезарядка: {vSkill.Cd} сек.   |   Статус: ОТКРЫТО ✓ ({vSkill.UnlockReqText})"
                                : $"ЗАБЛОКИРОВАНО — {vSkill.UnlockReqText}";
                            hoverDesc = vSkill.Desc;
                        }

                        if (Engine.DrawButton(vRect, btnText, bgCol, Rgb(62, 115, 185),
                            unlocked ? (isSel ? Color.Yellow : Color.White) : Color.Gray, 13f, borderCol))
                        {
                            if (unlocked)
                            {
                                prog.SetSkillVariant(_selectedChar, slot, vi);
                                cfg.SyncLoadout();
                                TriggerLobbySkillPreview(_selectedChar, slot, vi);
                            }
                            else
                            {
                                ShowBanner($"НАВЫК ЗАКРЫТ! {vSkill.UnlockReqText}", Color.Red, 2.8f);
                            }
                        }
                    }
                }

                // 3. НИЖНЕЕ ОКНО ОПИСАНИЯ ПРИ НАВЕДЕНИИ + КНОПКА РАЗБЛОКИРОВКИ СКИНОВ ДЛЯ ТЕСТА
                Raylib.DrawRectangle(54, 652, 648, 154, Rgb(14, 20, 34, 248));
                Raylib.DrawRectangle(54, 652, 648, 3, hoverCol);
                Engine.DrawTextLeft(hoverTitle, 68, 662, 18f, hoverCol);
                Engine.DrawTextLeft(hoverSub, 68, 688, 14f, Color.Gold);
                Engine.DrawTextLeft(hoverDesc, 68, 714, 15f, Color.White);

                if (Engine.DrawButton(new Rectangle(468, 768, 224, 30), "ОТКРЫТЬ ВСЕ СКИНЫ",
                    Rgb(75, 55, 25), Rgb(115, 85, 35), Color.Gold, 13f, Color.Gold))
                {
                    prog.UnlockAll();
                    SelectCharacter(_selectedChar);
                    ShowBanner("ВСЕ ДОСТИЖЕНИЯ, АРТЕФАКТЫ И СКИНЫ РАЗБЛОКИРОВАНЫ!", Color.Gold, 3.0f);
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
                Raylib.DrawRectangle(54, 284, 648, 114, Rgb(18, 24, 40, 238));
                Raylib.DrawRectangle(54, 284, 4, 114, curDiff.Color);
                Engine.DrawTextLeft("РЕЖИМ СЛОЖНОСТИ ЗАБЕГА (4 УРОВНЯ):", 68, 292, 15f, curDiff.Color);
                if (!canEditMods)
                    Engine.DrawTextLeft("[Управляется хостом комнаты]", 445, 292, 13f, Color.Orange);

                for (int di = 0; di < GameData.OrderedDifficultyKeys.Length; di++)
                {
                    string dKey = GameData.OrderedDifficultyKeys[di];
                    var dDef = GameData.Difficulties[dKey];
                    bool isSel = GameData.SelectedDifficulty == dKey;
                    Rectangle dRect = new(66 + di * 158, 318, 150, 68);

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
                        isSel ? Color.Yellow : dDef.Color, 14f,
                        isSel ? Color.Yellow : dDef.Color) && canEditMods)
                    {
                        GameData.SelectedDifficulty = dKey;
                        prog.Save();
                        BroadcastLobbyState();
                    }
                }

                // 2. СЕТКА 10 АРТЕФАКТОВ ЗАБЕГА (5x2)
                Raylib.DrawRectangle(54, 408, 648, 232, Rgb(18, 24, 40, 238));
                Raylib.DrawRectangle(54, 408, 4, 232, Color.Gold);
                Engine.DrawTextLeft($"АРТЕФАКТЫ ЗАБЕГА :: АКТИВНО {GameData.ActiveArtifacts.Count} ИЗ 10", 68, 416, 15f, Color.Gold);

                if (canEditMods)
                {
                    if (Engine.DrawButton(new Rectangle(430, 414, 128, 26), "ВКЛЮЧИТЬ ВСЕ",
                        Rgb(42, 72, 48), Rgb(62, 110, 72), Color.Lime, 12f, Color.Lime))
                    {
                        foreach (var ak in GameData.OrderedArtifactKeys) GameData.ActiveArtifacts.Add(ak);
                        prog.Save();
                        BroadcastLobbyState();
                    }
                    if (Engine.DrawButton(new Rectangle(564, 414, 126, 26), "СБРОСИТЬ ВСЕ",
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
                    Rectangle aRect = new(66 + c * 126, 448 + r * 90, 120, 80);

                    if (Raylib.CheckCollisionPointRec(mp, aRect))
                    {
                        hoverCol = aDef.Color;
                        hoverTitle = $"{aDef.Name}  [Код: {aDef.Short}]";
                        hoverSub = $"{aDef.Subtitle}  —  Статус: {(enabled ? "ВКЛЮЧЁН ✓" : "ВЫКЛЮЧЕН")}";
                        hoverDesc = aDef.Desc;
                    }

                    string shortTitle = aDef.Name.Replace("Артефакт ", "");
                    string aBtnTxt = $"{(enabled ? "★ ВКЛ" : "ВЫКЛ")} [{aDef.Short}]\n{shortTitle}";
                    if (Engine.DrawButton(aRect, aBtnTxt,
                        enabled ? Rgb(48, 82, 138) : Rgb(22, 26, 38),
                        Rgb(65, 108, 175),
                        enabled ? Color.Yellow : Rgb(175, 190, 215), 13f,
                        enabled ? Color.Yellow : aDef.Color) && canEditMods)
                    {
                        if (enabled) GameData.ActiveArtifacts.Remove(aKey);
                        else GameData.ActiveArtifacts.Add(aKey);
                        prog.Save();
                        BroadcastLobbyState();
                    }
                }

                // 3. ОКНО ПОДРОБНОГО ОПИСАНИЯ ВЫБРАННОГО МОДИФИКАТОРА
                Raylib.DrawRectangle(54, 650, 648, 156, Rgb(14, 20, 34, 248));
                Raylib.DrawRectangle(54, 650, 648, 3, hoverCol);
                Engine.DrawTextLeft(hoverTitle, 68, 662, 18f, hoverCol);
                Engine.DrawTextLeft(hoverSub, 68, 690, 14f, Color.Gold);
                Engine.DrawTextLeft(hoverDesc, 68, 718, 15f, Color.White);
            }
        }

        public static void StartRun(string charKey, int? seed = null, bool fromNetwork = false, int startStage = 1)
        {
            _screen = "in_game";
            _paused = false;
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
            _run = new GameRunContext { CharKey = charKey };
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

        private static void SpawnTestEnemiesWave(int count = 6)
        {
            if (_run == null) return;
            string[] types = { "lemurian", "wisp", "beetle", "jellyfish", "bison", "brass_contraption", "golem", "imp", "greater_wisp", "parent", "elder_lemurian" };
            var rng = new Random();
            for (int i = 0; i < count; i++)
            {
                float ang = (i / (float)count) * MathF.PI * 2f + (float)rng.NextDouble() * 0.4f;
                float dist = 14f + (float)rng.NextDouble() * 14f;
                float ex = Math.Clamp(_run.PlayerPos.X + MathF.Cos(ang) * dist, -Physics.WorldBound + 4f, Physics.WorldBound - 4f);
                float ez = Math.Clamp(_run.PlayerPos.Z + MathF.Sin(ang) * dist, -Physics.WorldBound + 4f, Physics.WorldBound - 4f);
                float ey = Physics.GetSurfaceHeight(ex, ez, _run.PlayerPos.Y);
                string t = types[rng.Next(types.Length)];
                var enemy = new CombatTarget(new Vector3(ex, ey, ez), t, _run.DifficultyCoeff);
                CombatManager.Enemies.Add(enemy);
                _run.StageEntities.Add(enemy);

                if (_net.Connected)
                {
                    _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "enemy_spawn",
                        ["id"] = enemy.NetId,
                        ["et"] = t,
                        ["coeff"] = MathF.Round(_run.DifficultyCoeff, 3),
                        ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(ex, 2), MathF.Round(ey, 2), MathF.Round(ez, 2)),
                        ["boss"] = false
                    });
                }
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
            _deadCoopPlayers.Clear();
            _commandPickerQueue.Clear();
            _isSpectating = false;
            _run.PlayerHp = (stageNum == 1 && specialMode == null && GameData.SelectedDifficulty == "eclipse")
                ? _run.PlayerMaxHp * 0.80f
                : MathF.Max(_run.PlayerHp, _run.PlayerMaxHp * 0.5f);
            _run.VengeanceTimer = 85.0f;
            if (GameData.HasArtifact("kin") && specialMode == null)
            {
                string[] kinPool = { "beetle", "lemurian", "jellyfish", "wisp", "imp", "bison", "brass_contraption", "greater_wisp", "golem", "parent", "elder_lemurian" };
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
            else if (specialMode == "bazaar") _run.PlayerPos = new Vector3(0, 1f, -2f);
            else if (specialMode == "moon") _run.PlayerPos = new Vector3(0, 1f, -16f);
            else _run.PlayerPos = new Vector3(0, 1f, 0f);
            _run.PlayerVelY = 0f;

            if (_run.CharKey == "mindustry" && specialMode != "bazaar")
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
            else if (specialMode == "bazaar")
                ShowBanner("БАЗАР МЕЖДУ ВРЕМЕНЕМ :: ТОРГОВЛЯ С ТРИТОНОМ", Rgb(80, 215, 255), 3.5f);
            else if (specialMode == "moon")
                ShowBanner("НАЧАЛО (ЛУНА) :: ЗАРЯДИТЕ ВСЕ 4 СТОЛПА ЛУНЫ НА БАСТИОНАХ!", Rgb(100, 225, 255), 4.5f);
            else
                {
                    string kinNote = (!string.IsNullOrEmpty(_run.StageKinType) && GameData.MonsterLog.TryGetValue(_run.StageKinType, out var km))
                        ? $"  [РОДСТВО: {km.Name.ToUpper()}]"
                        : "";
                    ShowBanner($"ЭТАП {stageNum}: {pal.Name.ToUpper()}{kinNote}", Color.Yellow, 3.5f);
                }
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
            if (_net.Connected)
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject { ["type"] = "run_victory" });
            FinishRunWithSummary(victory: true);
            ShowBanner("ПОБЕДА! ВЫ ПОКИНУЛИ ЛУНУ НА СПАСАТЕЛЬНОМ КОРАБЛЕ (+10 ЛУННЫХ МОНЕТ)!", Color.Gold, 8.0f);
        }

        private static void QuitRunToLobby(bool toMainMenu = false)
        {
            _paused = false;
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
                SelectCharacter(_selectedChar);
                BroadcastLobbyState();
            }
            SelectCharacter(_selectedChar);
        }

        private static void UpdateInGame(float dt)
        {
            if (_run == null) return;

            _run.ElapsedTime += dt;
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
            if (_net.Mode != "client" && Physics.SpecialMode != "bazaar" && !_run.MithrixSpawned && !tpActivated)
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
                        float vx = Math.Clamp(_run.PlayerPos.X + MathF.Cos(vang) * 16f, -Physics.WorldBound + 5f, Physics.WorldBound - 5f);
                        float vz = Math.Clamp(_run.PlayerPos.Z + MathF.Sin(vang) * 16f, -Physics.WorldBound + 5f, Physics.WorldBound - 5f);
                        float vy = Physics.GetSurfaceHeight(vx, vz, _run.PlayerPos.Y);
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
                    (string et, float cost)[] table = Physics.SpecialMode == "moon"
                        ? new[] { ("lunar_chimera", 18f), ("brass_contraption", 22f), ("greater_wisp", 26f), ("golem", 28f), ("parent", 34f) }
                        : new[]
                        {
                            ("beetle", 10f), ("lemurian", 12f), ("jellyfish", 14f), ("wisp", 16f),
                            ("imp", 22f), ("bison", 24f), ("brass_contraption", 26f),
                            ("greater_wisp", 30f), ("golem", 34f), ("parent", 40f), ("elder_lemurian", 45f)
                        };

                    int packLimit = rng.Next(playerCount, 4 + playerCount * 2); // Размер пачки растёт от числа игроков
                    if (GameData.HasArtifact("dissonance"))
                    {
                        table = new[]
                        {
                            ("beetle", 10f), ("lemurian", 12f), ("jellyfish", 14f), ("wisp", 16f),
                            ("lunar_chimera", 18f), ("imp", 22f), ("bison", 24f), ("brass_contraption", 26f),
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
                        float maxB = Physics.WorldBound - 4.0f;
                        float sx = Math.Clamp(anchorPos.X + MathF.Cos(ang) * dist, -maxB, maxB);
                        float sz = Math.Clamp(anchorPos.Z + MathF.Sin(ang) * dist, -maxB, maxB);
                        float sy = Physics.GetSurfaceHeight(sx, sz, anchorPos.Y);
                        int swarmSpawnCount = GameData.HasArtifact("swarm") ? 2 : 1;
                        for (int swi = 0; swi < swarmSpawnCount && CombatManager.Enemies.Count < maxEnemies; swi++)
                        {
                            float osx = Math.Clamp(sx + swi * 1.6f, -maxB, maxB);
                            float osz = Math.Clamp(sz - swi * 1.6f, -maxB, maxB);
                            float osy = Physics.GetSurfaceHeight(osx, osz, sy);
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
                    bool _unusedDir = false;
                    if (_unusedDir)
                    {
                        (string et, float cost) chosen = ("lemurian", 10f);
                        float sx = 0f, sy = 0f, sz = 0f;
                        var ne = new CombatTarget(new Vector3(sx, sy, sz), chosen.et, coeff);
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
                                ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(sx, 2), MathF.Round(sy, 2), MathF.Round(sz, 2)),
                                ["boss"] = false
                            });
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

            if (isMoba)
            {
                if (Raylib.IsCursorHidden())
                    Raylib.EnableCursor();

                Ray rlRay = Raylib.GetMouseRay(Raylib.GetMousePosition(), Engine.Camera);
                Vector3 rayOrig = new(-rlRay.Position.X, rlRay.Position.Y, rlRay.Position.Z);
                Vector3 rayDir = new(-rlRay.Direction.X, rlRay.Direction.Y, rlRay.Direction.Z);
                if (MathF.Abs(rayDir.Y) > 0.001f)
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

                if (Raylib.IsMouseButtonPressed(MouseButton.Right))
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
                else if (Raylib.IsMouseButtonDown(MouseButton.Right) && c.MobaAttackTarget == null)
                {
                    c.MobaMoveTarget = c.MobaCursorPos;
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
                        if (c.M1Cd <= 0f) c.UseM1Skill();
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
                if ((Raylib.IsMouseButtonDown(MouseButton.Left) || Raylib.IsKeyDown(KeyboardKey.Q)) && c.M1Cd <= 0f)
                {
                    p.LookAt2D(c.MobaCursorPos);
                    c.UseM1Skill();
                }
                if ((Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsMouseButtonPressed(MouseButton.Middle)) && c.M2Cd <= 0f)
                {
                    p.LookAt2D(c.MobaCursorPos);
                    c.UseM2Skill();
                }
                if (Raylib.IsKeyPressed(KeyboardKey.LeftShift) && c.ShiftCd <= 0f)
                {
                    c.MobaMoveTarget = null;
                    p.LookAt2D(c.MobaCursorPos);
                    c.UseShiftSkill();
                }
                if (Raylib.IsKeyPressed(KeyboardKey.R) && c.RCd <= 0f)
                {
                    p.LookAt2D(c.MobaCursorPos);
                    c.UseRSkill();
                }
            }
            else
            {
                Vector2 md = _commandPickerQueue.Count > 0 ? Vector2.Zero : Raylib.GetMouseDelta();
                if (c.MedicTauntTimer <= 0f && _commandPickerQueue.Count == 0)
                    p.RotationY += md.X * _mouseSens;
                bool isFp = SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey) && c.MedicTauntTimer <= 0f;
                float maxPitch = isFp ? 82f : 48f;
                float minPitch = isFp ? -82f : -42f;
                _run.CamPitch = Math.Clamp(_run.CamPitch + md.Y * _mouseSens, minPitch, maxPitch);

                float inX = (Raylib.IsKeyDown(KeyboardKey.D) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.A) ? 1f : 0f);
                float inZ = (Raylib.IsKeyDown(KeyboardKey.W) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.S) ? 1f : 0f);
                animStrafe = inX;
                animFwd = inZ;
                isMoving = (MathF.Abs(inX) > 0.01f || MathF.Abs(inZ) > 0.01f) && c.MedicTauntTimer <= 0f;

                if (isMoving && !c.GrappleActive)
                {
                    Vector3 moveDir = _run.CharKey == "mindustry"
                        ? Vector3.Normalize(new Vector3(inX, 0f, inZ))
                        : Vector3.Normalize(p.Forward * inZ + p.Right * inX);
                    p.Position += moveDir * c.MoveSpeed * dt;
                }

                c.Update(dt, isMoving);
                if (c.MedicTauntTimer <= 0f && _commandPickerQueue.Count == 0)
                {
                    bool mouseOverBuildMenu = _run.CharKey == "mindustry" &&
                        Raylib.GetMouseX() >= Raylib.GetScreenWidth() - 525 &&
                        Raylib.GetMouseY() >= Raylib.GetScreenHeight() - 285;

                    if (!mouseOverBuildMenu && (_run.CharKey == "mindustry" && c.MindustryBuildMode && c.MindustrySelectedBuild >= 0
                        ? Raylib.IsMouseButtonPressed(MouseButton.Left)
                        : Raylib.IsMouseButtonDown(MouseButton.Left)) && c.M1Cd <= 0f)
                    {
                        c.UseM1Skill();
                    }
                    if ((_run.CharKey is "sol" or "mindustry" or "freeman" ? Raylib.IsMouseButtonPressed(MouseButton.Right) : Raylib.IsMouseButtonDown(MouseButton.Right)) && c.M2Cd <= 0f)
                        c.UseM2Skill();
                    if (Raylib.IsKeyPressed(KeyboardKey.LeftShift) && c.ShiftCd <= 0f) c.UseShiftSkill();
                    if (Raylib.IsKeyPressed(KeyboardKey.R) && c.RCd <= 0f) c.UseRSkill();
                }
            }

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

                if (Raylib.IsKeyPressed(KeyboardKey.Space) && _run.PlayerJumpsLeft > 0)
                {
                    _run.PlayerVelY = 12.0f;
                    _run.PlayerGrounded = false;
                    _run.PlayerJumpsLeft--;
                    int quill = CombatManager.GetItemCount("wax_quill");
                    if (quill > 0 && isMoving)
                        _run.ApplyDash(p.Forward * (14.0f + 4.0f * quill), 0.16f);
                    new VisualFX(p.Position, Color.SkyBlue, 1.8f, 0.22f);
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

            Vector3 resolved = Physics.ResolveHorizontalCollision(p.Position, 0.72f, p.Y);
            float b = Physics.WorldBound;
            p.X = Math.Clamp(resolved.X, -b, b);
            p.Z = Math.Clamp(resolved.Z, -b, b);

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
            if (_run.CharKey == "mindustry")
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
                Vector3 target = p.Position + new Vector3(0f, 0.8f, 1.5f);
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
            int pw = 420, ph = 330, px = (sw - pw) / 2, py = (sh - ph) / 2;
            Raylib.DrawRectangle(px, py, pw, ph, Rgb(14, 18, 30, 245));
            Raylib.DrawRectangle(px, py, pw, 4, Color.SkyBlue);
            Engine.DrawTextCentered("ПАУЗА", sw * 0.5f, py + 38f, 26f, Color.SkyBlue);

            if (Engine.DrawButton(new Rectangle(px + 40, py + 82, pw - 80, 52), "ПРОДОЛЖИТЬ ИГРУ [ESC]", Rgb(38, 110, 70), Rgb(55, 155, 95), Color.White, 18f))
            {
                _paused = false;
                if (_run != null && SurvivorDatabase.IsMobaCharacter(_run.CharKey))
                    Raylib.EnableCursor();
                else
                    Raylib.DisableCursor();
            }
            if (Engine.DrawButton(new Rectangle(px + 40, py + 150, pw - 80, 52), "ВЫЙТИ В ЛОББИ ПЕРСОНАЖЕЙ", Rgb(55, 75, 115), Rgb(80, 105, 155), Color.White, 18f))
            {
                QuitRunToLobby(toMainMenu: false);
            }
            if (Engine.DrawButton(new Rectangle(px + 40, py + 218, pw - 80, 52), "ВЫЙТИ В ГЛАВНОЕ МЕНЮ", Rgb(135, 40, 48), Rgb(185, 60, 68), Color.White, 18f))
            {
                QuitRunToLobby(toMainMenu: true);
            }
        }

        private static void DrawInGameHUD()
        {
            if (_run == null) return;
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            var c = _run.Combat;

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
            if (!SurvivorDatabase.IsMobaCharacter(_run.CharKey) && _run.CharKey != "mindustry")
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

            // 4. КОМПАКТНЫЙ БЛОК ЗДОРОВЬЯ И ВАЛЮТ СЛЕВА ВНИЗУ (300x60)
            int hx = 20, hy = sh - 78;
            Raylib.DrawRectangle(hx, hy, 300, 58, Rgb(12, 16, 26, 190));
            Engine.DrawTextLeft($"${_run.Gold}", hx + 12, hy + 7, 18f, Color.Gold);
            Engine.DrawTextLeft($"{_run.LunarCoins}М", hx + 95, hy + 7, 17f, Color.SkyBlue);
            Engine.DrawTextLeft(SurvivorDatabase.Classes[_run.CharKey].Name, hx + 155, hy + 8, 15f, Rgb(190, 210, 235));

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
            bool _unusedTab = false;
            if (_unusedTab)
            {
                {
                    int sy = 0;
                    sy += 23;
                }
            }
        }

        

        private static void DrawLogbookUI()
        {
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
            var entries = GetLogEntries(_logCategory);
            int perPage = 18;
            int totalPages = Math.Max(1, (entries.Count - 1) / perPage + 1);
            _logPage = Math.Clamp(_logPage, 0, totalPages - 1);

            int startIdx = _logPage * perPage;
            for (int i = 0; i < perPage && startIdx + i < entries.Count; i++)
            {
                var e = entries[startIdx + i];
                int r = i / 6, c = i % 6;
                Rectangle box = new(54 + c * 110, 140 + r * 86, 100, 74);
                bool isSel = e.Key == _logSelectedKey;
                if (Engine.DrawButton(box, e.Unlocked ? e.Short : "???",
                    e.Unlocked ? Rgb(24, 32, 48) : Rgb(16, 18, 26), e.Color,
                    e.Unlocked ? e.Color : Color.Gray, 16f, isSel ? Color.White : e.Color))
                {
                    SelectLogbookEntry(_logCategory, e.Key);
                }
            }

            if (Engine.DrawButton(new Rectangle(140, 410, 130, 34), "<- ПРЕД.", Rgb(40, 52, 78), Rgb(65, 85, 120), Color.White, 15f))
                _logPage = Math.Clamp(_logPage - 1, 0, totalPages - 1);
            Engine.DrawTextCentered($"Стр. {_logPage + 1} / {totalPages}", 380, 427, 17f, Color.White);
            if (Engine.DrawButton(new Rectangle(490, 410, 130, 34), "СЛЕД. ->", Rgb(40, 52, 78), Rgb(65, 85, 120), Color.White, 15f))
                _logPage = Math.Clamp(_logPage + 1, 0, totalPages - 1);

            if (Engine.DrawButton(new Rectangle(950, 760, 310, 42), "ОТКРЫТЬ ВСЁ В ЖУРНАЛЕ", Rgb(95, 65, 30), Rgb(140, 95, 45), Color.Gold, 16f))
                Data.ProgressData.Instance.UnlockAll();

            if (Engine.DrawButton(new Rectangle(54, 664, 340, 44), "<- НАЗАД В ГЛАВНОЕ МЕНЮ [ESC]", Rgb(65, 72, 95), Rgb(95, 105, 135), Color.White, 16f))
            {
                _screen = "main";
                SelectCharacter(_selectedChar);
            }

            // Нижняя карточка описания выбранной записи
            var cur = entries.Find(x => x.Key == _logSelectedKey);
            if (cur.Key != null)
            {
                Raylib.DrawRectangle(54, 462, 664, 190, Rgb(18, 24, 40, 245));
                Raylib.DrawRectangle(54, 462, 664, 4, cur.Color);
                Engine.DrawTextLeft(cur.Title, 72, 478, 24f, cur.Color);
                Engine.DrawTextLeft(cur.Sub, 72, 512, 17f, Color.Gold);
                Engine.DrawTextLeft(cur.Desc, 72, 545, 17f, Color.White);
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
                    var v = kv.Value;
                    list.Add(new LogEntryView
                    {
                        Key = kv.Key, Short = v.Short, Color = v.Color,
                        Unlocked = prog.UnlockedItems.Contains(kv.Key),
                        Title = v.Name, Sub = $"Редкость: {v.TierName}  |  Код: [{v.Short}]", Desc = v.Desc
                    });
                }
            }
            else if (cat == "survivors")
            {
                foreach (var k in SurvivorDatabase.OrderedKeys)
                {
                    var v = SurvivorDatabase.Classes[k];
                    list.Add(new LogEntryView
                    {
                        Key = k, Short = v.Icon, Color = v.BodyColor, Unlocked = true,
                        Title = $"{v.Name} — {v.Subtitle}",
                        Sub = $"HP: {v.Hp}  |  Скорость: {v.Speed}  |  Урон: {v.Damage}  |  Атака: {v.FireRate}/с",
                        Desc = $"{v.Skills["m1"].Name} / {v.Skills["m2"].Name} / {v.Skills["shift"].Name} / {v.Skills["r"].Name}"
                    });
                }
            }
            else if (cat is "monsters" or "bosses")
            {
                foreach (var kv in Data.GameData.MonsterLog)
                {
                    bool isBoss = Array.IndexOf(Data.GameData.BossTypes, kv.Key) >= 0 || kv.Key == "mithrix";
                    if ((cat == "bosses") != isBoss) continue;
                    var m = kv.Value;
                    list.Add(new LogEntryView
                    {
                        Key = kv.Key, Short = kv.Key[..Math.Min(5, kv.Key.Length)].ToUpper(),
                        Color = kv.Key == "mithrix" ? Color.SkyBlue : (isBoss ? Color.Red : Rgb(255, 140, 60)),
                        Unlocked = true, Title = m.Name,
                        Sub = $"Базовое HP: {m.Hp}   |   Базовый урон: {m.Damage}", Desc = m.Desc
                    });
                }
            }
            else if (cat == "stages")
            {
                var all = new System.Collections.Generic.List<Data.StagePalette>(Data.GameData.StagePalettes)
                {
                    Data.GameData.SpecialPalettes["bazaar"],
                    Data.GameData.SpecialPalettes["moon"]
                };
                foreach (var st in all)
                {
                    bool disc = prog.DiscoveredStages.Contains(st.Id);
                    list.Add(new LogEntryView
                    {
                        Key = st.Id, Short = st.Id[..Math.Min(5, st.Id.Length)].ToUpper(),
                        Color = st.Accent, Unlocked = disc, Title = st.Name,
                        Sub = disc ? "Статус: ИССЛЕДОВАНА ✓" : "Статус: ЕЩЁ НЕ ПОСЕЩЕНА", Desc = st.Desc
                    });
                }
            }
            else if (cat == "achievements")
            {
                foreach (var kv in Data.GameData.Achievements)
                {
                    bool unl = prog.UnlockedAchievements.Contains(kv.Key);
                    list.Add(new LogEntryView
                    {
                        Key = kv.Key, Short = "ACH", Color = unl ? Color.Lime : Color.Orange,
                        Unlocked = unl, Title = kv.Value.Name,
                        Sub = unl ? "Статус: ВЫПОЛНЕНО ✓" : "Статус: НЕ ВЫПОЛНЕНО",
                        Desc = $"{kv.Value.Desc} ({kv.Value.RewardText})"
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