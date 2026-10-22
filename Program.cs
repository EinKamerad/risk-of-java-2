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
                int tarr = Items.TryGetValue("tarrasque", out int t) ? t * 100 : 0;
                float raw = baseHp + steak + tarr + (Combat != null ? Combat.InfusionBonus : 0f);
                float transMult = 1.0f + 0.50f * (Items.TryGetValue("transcendence", out int tr) ? tr : 0);
                float fluxMult = MathF.Pow(2.0f, Items.TryGetValue("lunar_pauldron", out int fl) ? fl : 0);
                float tonicMult = 1.0f + 0.25f * (Items.TryGetValue("spinel_tonic", out int tn) ? tn : 0);
                float glassDiv = MathF.Pow(0.5f, Items.TryGetValue("shaped_glass", out int gl) ? gl : 0);
                return MathF.Max(1f, MathF.Floor(raw * transMult * fluxMult * tonicMult * glassDiv));
            }
        }
        public int Gold { get; set; } = 35;
        public int LunarCoins { get; set; } = 5;
        public bool BlueOrbSpawned { get; set; } = false;
        public CombatTarget? ActiveBoss;
        public string BossTitle = "";
        public bool IsBossAlive => IsValid(ActiveBoss) && !ActiveBoss!.IsAlly;
        public float MoonDetonationTimer { get; set; } = 0f;
        public bool MithrixSpawned = false;

        public float ElapsedTime = 0f;
        public float DirectorCredits = 0f;
        public float SpawnTimer = 1.5f;

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
                return (1.0f + 0.18f * minutes) * MathF.Pow(1.15f, Stage - 1);
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

        public void UnlockAchievement(string achKey)
        {
            if (GameData.Achievements.TryGetValue(achKey, out var ach) &&
                !ProgressData.Instance.UnlockedAchievements.Contains(achKey))
            {
                ProgressData.Instance.UnlockedAchievements.Add(achKey);
                if (ach.UnlocksItem != null)
                    ProgressData.Instance.UnlockedItems.Add(ach.UnlocksItem);
                ProgressData.Instance.Save();
                ShowBanner($"ДОСТИЖЕНИЕ: {ach.Name} ({ach.RewardText})", Color.Gold, 4.0f);
            }
        }

        public void HealPlayer(float amount)
        {
            if (amount <= 0f) return;
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
            if (!ItemOrder.Contains(itemKey)) ItemOrder.Add(itemKey);
            Items[itemKey] = (Items.TryGetValue(itemKey, out int c) ? c : 0) + 1;
            var info = GameData.ItemPool[itemKey];
            ShowBanner($"ПОДОБРАНО: {info.Name} ({info.Desc})", info.Color, 3.2f);
            int total = 0;
            foreach (var v in Items.Values) total += v;
            if (total >= 6) UnlockAchievement("ach_hoarder");
        }

        public void SpawnItemDrop(Vector3 pos, string? itemKey = null, string poolType = "small_chest")
        {
            string key = itemKey != null && GameData.ItemPool.ContainsKey(itemKey) ? itemKey : GameData.RollItemFromPool(poolType);
            StageEntities.Add(new ItemDrop(pos, key));
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
            }
            ShowBanner("ТЕЛЕПОРТ АКТИВИРОВАН! ПОБЕДИТЕ БОССА ТЕЛЕПОРТА!", Color.Red, 3.2f);
        }

        public void NextStage()
        {
            Stage++;
            if (Stage >= 2) UnlockAchievement("ach_traveler");
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
        private static GameRunContext? _run;
        private static bool _paused = false;

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

        // Состояние Журнала (Logbook)
        private static string _logCategory = "items";
        private static int _logPage = 0;
        private static string _logSelectedKey = "syringe";

        public static Vector3 GetNearestPlayerPos(Vector3 fromPos, out bool isLocalPlayer)
        {
            isLocalPlayer = true;
            if (_run == null) return Vector3.Zero;
            Vector3 bestPos = _run.PlayerPos;
            float bestDist = Vector3.Distance(fromPos, bestPos);

            foreach (var rp in _remotePlayers.Values)
            {
                if (IsValid(rp))
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

                if (_screen == "in_game")
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    {
                        _paused = !_paused;
                        if (_paused || (_run != null && SurvivorDatabase.IsMobaCharacter(_run.CharKey)))
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
                    ShowBanner($"ПОДКЛЮЧЕНО К КОМНАТЕ {_net.RoomCode}!", Color.Lime, 3.0f);
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
                        EvaluateLobbyCountdown();
                    }
                }
                else if (mtype == "lobby_start_run")
                {
                    int st = msg["stage"]?.GetValue<int>() ?? 1;
                    int seed = msg["seed"]?.GetValue<int>() ?? 1001;
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
                            rp = new RemotePlayer(pid, msg["char_key"]?.GetValue<string>() ?? "commando");
                            _remotePlayers[pid] = rp;
                            _run.StageEntities.Add(rp);
                        }
                        rp.ApplyNetState(msg);
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
                ["ready"] = _myReady
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
            _previewEnemyParts = null;
            _previewBaseScale = 1.0f;
            _previewActor.Position = new Vector3(0, 0.12f, 0);
            _previewActor.Rotation = new Vector3(0, 15f, 0);

            var cfg = SurvivorDatabase.Classes[charKey];
            _pedestalRing.Color = cfg.BodyColor;
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
                _previewBaseScale = key is "golem" or "greater_wisp" or "lunar_chimera" ? 0.76f : 0.95f;
                float yOff = key is "wisp" or "jellyfish" or "greater_wisp" or "lunar_chimera" ? -0.75f : 0.12f;
                _previewActor.Position = new Vector3(0, yOff, 0);
                _pedestalRing.Color = Rgb(255, 140, 50);
                _previewEnemyParts = EnemyAndBossBuilder.BuildEnemyMesh(_previewActor, key);
            }
            else if (category == "bosses")
            {
                _previewBaseScale = 0.48f;
                _previewActor.Position = new Vector3(0, key == "vagrant" ? -0.35f : 0.12f, 0);
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
                string stType = key is "roost" or "moon" ? "arch" : (key is "acres" or "bazaar" ? "monolith" : "crystals");
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

            if (_screen == "lobby")
            {
                KeyboardKey[] keys =
                {
                    KeyboardKey.One, KeyboardKey.Two, KeyboardKey.Three, KeyboardKey.Four,
                    KeyboardKey.Five, KeyboardKey.Six, KeyboardKey.Seven, KeyboardKey.Eight,
                    KeyboardKey.Nine, KeyboardKey.Zero, KeyboardKey.Minus, KeyboardKey.Equal,
                    KeyboardKey.LeftBracket, KeyboardKey.RightBracket
                };
                for (int i = 0; i < keys.Length && i < SurvivorDatabase.OrderedKeys.Length; i++)
                {
                    if (Raylib.IsKeyPressed(keys[i]))
                        SelectCharacter(SurvivorDatabase.OrderedKeys[i]);
                }
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

            if (_manualTimer > 0f) _manualTimer -= dt;
            else
            {
                _previewActor.RotationY += 22f * dt;
                _previewActor.RotationX = Lerp(_previewActor.RotationX, 0f, dt * 5f);
            }

            if (_previewSurvivorParts?.Halo != null)
                _previewSurvivorParts.Halo.RotationZ += 95f * dt;
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
            Engine.DrawTextCentered("СЕТЕВАЯ ИГРА (ЛОКАЛЬНАЯ СЕТЬ / КОД КОМНАТЫ)", 350, 168, 21f, Rgb(150, 225, 255));

            if (Engine.DrawButton(new Rectangle(100, 215, 500, 58), "СОЗДАТЬ КОМНАТУ (ХОСТ)", Rgb(35, 100, 68), Rgb(50, 145, 95), Color.White, 19f))
            {
                _typingCode = false;
                _lobbyPlayers.Clear();
                var (ok, info) = _net.HostRoom();
                if (ok)
                {
                    _myReady = false;
                    _lobbyCountdownActive = false;
                    _screen = "lobby";
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
            string[] hotkeys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "[", "]", "F1", "F2", "F3", "F4" };

            for (int i = 0; i < SurvivorDatabase.OrderedKeys.Length; i++)
            {
                string k = SurvivorDatabase.OrderedKeys[i];
                var scfg = SurvivorDatabase.Classes[k];
                int row = i / 9, col = i % 9;
                Rectangle rect = new(36 + col * 80, 70 + row * 46, 74, 38);
                bool isSel = k == _selectedChar;
                string hk = i < hotkeys.Length ? hotkeys[i] : "";
                if (Engine.DrawButton(rect, $"[{hk}] {scfg.Icon}", Rgb(22, 28, 42, 245), scfg.BodyColor,
                    isSel ? Color.Yellow : Color.White, 13f, isSel ? Color.White : scfg.BodyColor))
                {
                    SelectCharacter(k);
                }
            }

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

            // Панель статуса комнаты справа сверху
            Raylib.DrawRectangle(sw - 470, 74, 434, 190, Rgb(12, 16, 28, 225));
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

        public static void StartRun(string charKey, int? seed = null, bool fromNetwork = false, int startStage = 1)
        {
            _screen = "in_game";
            _paused = false;
            _mainPedestal.Enabled = false;
            _menuCrystal.Enabled = false;
            if (SurvivorDatabase.IsMobaCharacter(charKey)) Raylib.EnableCursor();
            else Raylib.DisableCursor();

            _run = new GameRunContext { CharKey = charKey };
            foreach (var k in GameData.ItemPool.Keys) _run.Items[k] = 0;
            WorldContext.Game = _run;

            _run.PlayerEntity = new Entity(position: new(0, charKey == "mindustry" ? 1.8f : 0f, 0));
            _run.Parts = SurvivorDatabase.BuildSurvivorMesh(_run.PlayerEntity, charKey);
            _run.Combat = new SurvivorCombatController(_run);
            if (charKey == "v1") _run.Parts.Root.Enabled = false;
            _run.PlayerHp = _run.PlayerMaxHp;

            int chosenSeed = seed ?? (Environment.TickCount & 0x7FFFFFFF);
            LoadStage(startStage, null, chosenSeed, fromNetwork: true);

            if (_net.Connected && _net.Mode == "host" && !fromNetwork)
            {
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "lobby_start_run",
                    ["stage"] = _run.Stage,
                    ["seed"] = chosenSeed
                });
            }
        }

        private static void SpawnTestEnemiesWave(int count = 6)
        {
            if (_run == null) return;
            string[] types = { "lemurian", "wisp", "beetle", "jellyfish", "golem", "imp", "greater_wisp" };
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

            _run.Stage = stageNum;
            _run.BlueOrbSpawned = false;
            _run.MoonPillarsDone = 0;
            _run.MithrixSpawned = false;
            _run.MoonDetonationTimer = 0f;
            _run.ActiveBoss = null;
            _run.DirectorCredits = 26.0f * _run.DifficultyCoeff;

            int seed = fixedSeed ?? (Environment.TickCount & 0x7FFFFFFF);
            var (ents, tpOrPad, pal) = StageBuilder.BuildStageWorld(stageNum, seed, specialMode);
            _run.StageEntities.AddRange(ents);
            if (_run.CharKey == "v1")
                _run.Combat.BuildV1FpViewmodel();
            _run.ActiveTeleporterOrPad = tpOrPad;
            _run.StageName = pal.Name;

            if (specialMode == "bazaar") _run.PlayerPos = new Vector3(0, 1f, -2f);
            else if (specialMode == "moon") _run.PlayerPos = new Vector3(0, 1f, -16f);
            else _run.PlayerPos = new Vector3(0, 1f, 0f);
            _run.PlayerVelY = 0f;

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

            if (specialMode == "bazaar")
                ShowBanner("БАЗАР МЕЖДУ ВРЕМЕНЕМ :: ТОРГОВЛЯ С ТРИТОНОМ", Rgb(80, 215, 255), 3.5f);
            else if (specialMode == "moon")
                ShowBanner("НАЧАЛО (ЛУНА) :: ЗАРЯДИТЕ ВСЕ 4 СТОЛПА ЛУНЫ НА БАСТИОНАХ!", Rgb(100, 225, 255), 4.5f);
            else
                ShowBanner($"ЭТАП {stageNum}: {pal.Name.ToUpper()}", Color.Yellow, 3.0f);
        }

        public static void OnVictory()
        {
            QuitRunToLobby();
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
                BroadcastLobbyState();
            }
            SelectCharacter(_selectedChar);
        }

        private static void UpdateInGame(float dt)
        {
            if (_run == null) return;

            if (Raylib.IsKeyPressed(KeyboardKey.F5)) _run.NextStage();
            if (Raylib.IsKeyPressed(KeyboardKey.F6)) _run.EnterBazaar();
            if (Raylib.IsKeyPressed(KeyboardKey.F7)) _run.EnterMoonStage();
            if (Raylib.IsKeyPressed(KeyboardKey.F8))
            {
                _run.Gold += 500;
                _run.LunarCoins += 10;
                ShowBanner("ТЕСТ-БОНУС: +$500 ЗОЛОТА И +10 ЛУННЫХ МОНЕТ!", Color.Gold, 2.0f);
            }
            if (Raylib.IsKeyPressed(KeyboardKey.F9))
            {
                if (Physics.SpecialMode == "moon" && !_run.MithrixSpawned)
                {
                    _run.PlayerPos = new Vector3(0, Physics.ColosseumY + 1.5f, -36.0f);
                    _run.StartMithrixEncounter();
                }
                else
                {
                    SpawnTestEnemiesWave(5);
                    ShowBanner("СПАВН +5 МОНСТРОВ ДЛЯ ТЕСТА БОЯ!", Color.Orange, 1.8f);
                }
            }

            _run.ElapsedTime += dt;
            float coeff = _run.DifficultyCoeff;
            CombatManager.Enemies.RemoveAll(e => !IsValid(e));

            if (_run.MoonDetonationTimer > 0f)
            {
                _run.MoonDetonationTimer -= dt;
                if (_run.MoonDetonationTimer <= 0f)
                {
                    QuitRunToLobby();
                    ShowBanner("ЛУНА ДЕТОНИРОВАЛА! ВЫ НЕ УСПЕЛИ К СПАСАТЕЛЬНОМУ КОРАБЛЮ!", Color.Red, 5.0f);
                    return;
                }
            }

            // ИИ-Режиссёр (Combat Director)
            if (_net.Mode != "client" && Physics.SpecialMode != "bazaar" && !_run.MithrixSpawned)
            {
                float tpMult = (_run.ActiveTeleporterOrPad is Teleporter tp && tp.State == "charging") ? 1.8f : 1.0f;
                _run.DirectorCredits += (4.2f * coeff * tpMult) * dt;
                _run.SpawnTimer -= dt;

                if (_run.SpawnTimer <= 0f && CombatManager.Enemies.Count < 24)
                {
                    var rng = new Random();
                    _run.SpawnTimer = 1.6f + (float)rng.NextDouble() * 1.6f;
                    (string et, float cost)[] table = Physics.SpecialMode == "moon"
                        ? new[] { ("lunar_chimera", 18f), ("golem", 28f), ("greater_wisp", 26f) }
                        : new[] { ("beetle", 10f), ("lemurian", 12f), ("jellyfish", 14f), ("wisp", 16f), ("imp", 22f), ("greater_wisp", 30f), ("golem", 34f) };

                    var affordable = new List<(string et, float cost)>();
                    foreach (var pair in table)
                        if (_run.DirectorCredits >= pair.cost) affordable.Add(pair);

                    if (affordable.Count > 0)
                    {
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
                        float dist = 16.0f + (float)rng.NextDouble() * 12.0f;
                        float maxB = Physics.WorldBound - 4.0f;
                        float sx = Math.Clamp(anchorPos.X + MathF.Cos(ang) * dist, -maxB, maxB);
                        float sz = Math.Clamp(anchorPos.Z + MathF.Sin(ang) * dist, -maxB, maxB);
                        float sy = Physics.GetSurfaceHeight(sx, sz, anchorPos.Y);
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
            float prevY = p.Y;
            bool isMoba = SurvivorDatabase.IsMobaCharacter(_run.CharKey);
            bool isMoving = false;

            if (isMoba)
            {
                if (Raylib.IsCursorHidden())
                    Raylib.EnableCursor();

                // Проекция луча мыши на плоскость арены (с учётом инверсии оси X в Raylib)
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

                // ПКМ — клик по врагу для атаки или клик/удержание по земле для движения
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
                    float walkCycle = (float)Raylib.GetTime() * (11.0f + CombatManager.GetItemCount("hoof") * 1.2f);
                    float swing = MathF.Sin(walkCycle) * 38f;
                    _run.Parts.LeftLeg.RotationX = swing;
                    _run.Parts.RightLeg.RotationX = -swing;
                    _run.Parts.Torso.Y = 1.26f + MathF.Abs(MathF.Cos(walkCycle)) * 0.08f;
                }
                else
                {
                    _run.Parts.LeftLeg.RotationX = Lerp(_run.Parts.LeftLeg.RotationX, 0f, dt * 10f);
                    _run.Parts.RightLeg.RotationX = Lerp(_run.Parts.RightLeg.RotationX, 0f, dt * 10f);
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
                Vector2 md = Raylib.GetMouseDelta();
                p.RotationY += md.X * _mouseSens;
                float maxPitch = _run.CharKey == "v1" ? 82f : 48f;
                float minPitch = _run.CharKey == "v1" ? -82f : -42f;
                _run.CamPitch = Math.Clamp(_run.CamPitch + md.Y * _mouseSens, minPitch, maxPitch);

                float inX = (Raylib.IsKeyDown(KeyboardKey.D) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.A) ? 1f : 0f);
                float inZ = (Raylib.IsKeyDown(KeyboardKey.W) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.S) ? 1f : 0f);
                isMoving = MathF.Abs(inX) > 0.01f || MathF.Abs(inZ) > 0.01f;

                if (isMoving && !c.GrappleActive)
                {
                    Vector3 moveDir = Vector3.Normalize(p.Forward * inZ + p.Right * inX);
                    p.Position += moveDir * c.MoveSpeed * dt;

                    if (_run.CharKey == "mindustry")
                    {
                        _run.Parts.Torso.RotationZ = Lerp(_run.Parts.Torso.RotationZ, -inX * 14f, dt * 8f);
                        _run.Parts.Torso.RotationX = Lerp(_run.Parts.Torso.RotationX, inZ * 10f, dt * 8f);
                    }
                    else
                    {
                        float walkCycle = (float)Raylib.GetTime() * (11.0f + CombatManager.GetItemCount("hoof") * 1.2f);
                        float swing = MathF.Sin(walkCycle) * (c.ShieldStance ? 22f : 38f);
                        _run.Parts.LeftLeg.RotationX = swing;
                        _run.Parts.RightLeg.RotationX = -swing;
                        _run.Parts.Torso.Y = 1.26f + MathF.Abs(MathF.Cos(walkCycle)) * 0.08f;
                    }
                }
                else
                {
                    _run.Parts.LeftLeg.RotationX = Lerp(_run.Parts.LeftLeg.RotationX, 0f, dt * 10f);
                    _run.Parts.RightLeg.RotationX = Lerp(_run.Parts.RightLeg.RotationX, 0f, dt * 10f);
                }

                c.Update(dt, isMoving);
                if (Raylib.IsMouseButtonDown(MouseButton.Left) && c.M1Cd <= 0f) c.UseM1Skill();
                if (Raylib.IsMouseButtonDown(MouseButton.Right) && c.M2Cd <= 0f) c.UseM2Skill();
                if (Raylib.IsKeyPressed(KeyboardKey.LeftShift) && c.ShiftCd <= 0f) c.UseShiftSkill();
                if (Raylib.IsKeyPressed(KeyboardKey.R) && c.RCd <= 0f) c.UseRSkill();
            }

            float floorH = Physics.GetSurfaceHeight(p.X, p.Z, p.Y, prevY);

            if (_run.CharKey == "mindustry")
            {
                if (_run.DashTimer > 0f) { p.Position += _run.DashVector * dt; _run.DashTimer -= dt; }
                bool ascend = Raylib.IsKeyDown(KeyboardKey.Space);
                bool descend = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.C);
                float flySpd = 11.5f + 1.2f * CombatManager.GetItemCount("feather");
                if (ascend && !descend) _run.PlayerVelY = Lerp(_run.PlayerVelY, flySpd, dt * 9f);
                else if (descend && !ascend) _run.PlayerVelY = Lerp(_run.PlayerVelY, -flySpd, dt * 9f);
                else _run.PlayerVelY = Lerp(_run.PlayerVelY, 0f, dt * 10f);

                p.Y += _run.PlayerVelY * dt;
                float minHover = floorH + 0.45f;
                if (p.Y < minHover) { p.Y = minHover; _run.PlayerVelY = 0f; }
                _run.PlayerGrounded = p.Y <= minHover + 0.25f;
            }
            else if (!c.GrappleActive)
            {
                if (_run.DashTimer > 0f)
                {
                    p.Position += _run.DashVector * dt;
                    _run.DashTimer -= dt;
                    _run.Parts.Torso.RotationX = 28f;
                }
                else _run.Parts.Torso.RotationX = Lerp(_run.Parts.Torso.RotationX, 0f, dt * 10f);

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

                _run.PlayerVelY -= 26.0f * dt;
                p.Y += _run.PlayerVelY * dt;
                if (p.Y <= floorH)
                {
                    p.Y = floorH;
                    _run.PlayerVelY = 0f;
                    _run.PlayerGrounded = true;
                    _run.PlayerJumpsLeft = _run.PlayerMaxJumps;
                }
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

            if (_run.Parts.Halo != null) _run.Parts.Halo.RotationZ += 95f * dt;

            // Сетевая синхронизация позиции игрока и активных врагов
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
                        ["moving"] = isMoving
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
                                ["a"] = e.IsAlly
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
            if (isMoba)
            {
                Vector3 camPos = p.Position + new Vector3(0f, 22.0f, -14.5f);
                Vector3 target = p.Position + new Vector3(0f, 0.8f, 1.5f);
                Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                Engine.Camera.Target = new Vector3(-target.X, target.Y, target.Z);
            }
            else if (_run.CharKey == "v1")
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
                Vector3 camPos = pivot - camLookDir * 8.6f;
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

            // Лаконичный аккуратный прицел в центре (только для персонажей не в MOBA-режиме)
            if (!SurvivorDatabase.IsMobaCharacter(_run.CharKey))
            {
                Raylib.DrawCircleLines(sw / 2, sh / 2, 4f, Rgb(90, 230, 255, 180));
                Raylib.DrawPixel(sw / 2, sh / 2, Color.White);
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
            Engine.DrawTextLeft($"Этап {_run.Stage}: {_run.StageName}{detStr}  [Враги: {CombatManager.Enemies.Count}]", rx + 12, ry + 35, 14f, Rgb(185, 205, 230));

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

            // 6. КОМПАКТНЫЙ ULTRAKILL STYLE METER (только для V1)
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

                Engine.DrawTextLeft($"ОРУЖ [{c.V1Weapon}/5] | МОНЕТЫ {(int)c.V1Coins} | РЕЛЬСА {(int)c.V1RailCharge}% | РЫВОК {(int)c.V1Stamina}",
                    sw - 268, sh - 98, 13f, Color.Gold);
            }

            // 7. ОКНО ДЕТАЛЬНОЙ СТАТИСТИКИ ПО УДЕРЖАНИЮ [TAB]
            if (Raylib.IsKeyDown(KeyboardKey.Tab))
            {
                int tw = 780, th = 420, tx = (sw - tw) / 2, ty = (sh - th) / 2;
                Raylib.DrawRectangle(tx, ty, tw, th, Rgb(12, 16, 26, 240));
                Raylib.DrawRectangle(tx, ty, tw, 3, Color.SkyBlue);
                string netInfo = _net.Connected ? $"Кооп: Комната {_net.RoomCode} ({1 + _remotePlayers.Count} игр.)" : "Одиночная игра";
                Engine.DrawTextLeft($"{SurvivorDatabase.Classes[_run.CharKey].Name}  |  {netInfo}", tx + 24, ty + 18, 20f, Color.Gold);
                Engine.DrawTextLeft($"Урон: {c.BaseDamage:F1}  |  Атака: {c.FireRate:F1}/с  |  Скорость: {c.MoveSpeed:F1}  |  Крит: {c.CritChance * 100f:F0}%  |  Блок: {c.BlockChance * 100f:F0}%",
                    tx + 24, ty + 48, 16f, Color.White);
                Engine.DrawTextLeft("Чит-клавиши теста: [F5] След. этап | [F6] Базар | [F7] Луна | [F8] +$500 | [F9] +Враги/Митрикс",
                    tx + 24, ty + 74, 14f, Rgb(150, 195, 235));

                int sy = ty + 108;
                foreach (var k in _run.ItemOrder)
                {
                    if (sy > ty + th - 32) break;
                    var info = GameData.ItemPool[k];
                    Engine.DrawTextLeft($"• {info.Name} [x{_run.Items[k]}] — {info.Desc}", tx + 24, sy, 15f, info.Color);
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
                ("bosses", "БОССЫ"), ("stages", "ЛОКАЦИИ"), ("achievements", "ДОСТИЖЕНИЯ")
            };

            for (int i = 0; i < tabs.Length; i++)
            {
                var (cid, title) = tabs[i];
                bool active = _logCategory == cid;
                if (Engine.DrawButton(new Rectangle(36 + i * 165, 72, 156, 38), title,
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
            return list;
        }
    }
}