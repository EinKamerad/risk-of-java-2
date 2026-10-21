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
            if (c < 1.3f) return ("EASY", Rgb(80, 225, 100));
            if (c < 1.8f) return ("MEDIUM", Rgb(255, 210, 60));
            if (c < 2.5f) return ("HARD", Rgb(255, 130, 40));
            if (c < 3.5f) return ("VERY HARD", Rgb(245, 55, 55));
            return ("HAHAHA (INSANE)", Rgb(210, 50, 255));
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

        public void SpawnBoss(Vector3 pos, string? bossType = null)
        {
            var rng = new Random();
            string bType = bossType != null && Array.IndexOf(GameData.BossTypes, bossType) >= 0
                ? bossType
                : GameData.BossTypes[rng.Next(GameData.BossTypes.Length)];
            float fy = Physics.GetSurfaceHeight(pos.X, pos.Z, pos.Y);
            var boss = new CombatTarget(new Vector3(pos.X, fy, pos.Z), bType, DifficultyCoeff);
            ActiveBoss = boss;
            BossTitle = GameData.BossTitles.TryGetValue(bType, out var bt) ? bt : GameData.BossTitles["boss"];
            CombatManager.Enemies.Add(boss);
            StageEntities.Add(boss);
        }

        public void OnTeleporterActivated()
        {
            Vector3 tpPos = ActiveTeleporterOrPad != null ? ActiveTeleporterOrPad.Position : PlayerPos;
            SpawnBoss(tpPos + new Vector3(9f, 0f, 9f));
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

            var boss = new CombatTarget(new Vector3(0, Physics.ColosseumY, 22.0f), "mithrix", DifficultyCoeff);
            ActiveBoss = boss;
            BossTitle = "MITHRIX [PHASE 1/4] :: KING OF NOTHING";
            CombatManager.Enemies.Add(boss);
            StageEntities.Add(boss);
            ShowBanner("МИТРИКС, КОРОЛЬ НИЧЕГО [ФАЗА 1/4] :: БИТВА В НЕБЕСНОМ КОЛИЗЕЕ!", Color.SkyBlue, 4.2f);
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
        private static string _screen = "lobby"; // "lobby", "logbook", "in_game"
        private static string _selectedChar = "commando";
        private static GameRunContext? _run;

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

        public static void ShowBanner(string msg, Color col, float duration = 2.5f)
        {
            _bannerText = msg;
            _bannerColor = col;
            _bannerTimer = duration;
        }

        public static void Main()
        {
            Engine.Init(1536, 864, "Risk of C# 2 - LAN & Room Code Co-Op (Phase 3: World & Physics)");
            SetupDiorama();

            while (!Raylib.WindowShouldClose())
            {
                float dt = Math.Min(Raylib.GetFrameTime(), 0.05f);
                if (_bannerTimer > 0f) _bannerTimer -= dt;

                if (_screen == "in_game")
                {
                    UpdateInGame(dt);
                    Engine.Update(dt);
                }
                else
                {
                    HandleInput(dt);
                    Engine.Update(dt);
                    UpdateDiorama(dt);
                    Engine.Camera.Position = new Vector3(0f, 2.15f, -8.6f);
                    Engine.CameraEuler = new Vector3(5f, 0f, 0f);
                    Engine.UpdateCameraTargetFromEuler();
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Engine.BackgroundColor);

                Raylib.BeginMode3D(Engine.Camera);
                Engine.Draw3D();
                Raylib.EndMode3D();

                Engine.Draw3DBillboardLabels(Engine.Camera);

                if (_screen == "in_game") DrawInGameHUD();
                else if (_screen == "lobby") DrawLobbyUI();
                else DrawLogbookUI();

                if (_bannerTimer > 0f && !string.IsNullOrEmpty(_bannerText))
                    Engine.DrawTextCentered(_bannerText, Raylib.GetScreenWidth() * 0.5f, 155f, 24f, _bannerColor);

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
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
        }

        private static void SelectLogbookEntry(string category, string key)
        {
            _logCategory = category;
            _logSelectedKey = key;
            ClearChildren(_previewActor);
            _previewSurvivorParts = null;
            _previewEnemyParts = null;
            _previewActor.Rotation = new Vector3(0, 20f, 0);

            if (category == "items" && Data.GameData.ItemPool.TryGetValue(key, out var item))
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
                var pal = Data.GameData.StagePalettes.Find(p => p.Id == key)
                          ?? (Data.GameData.SpecialPalettes.TryGetValue(key, out var sp) ? sp : Data.GameData.StagePalettes[0]);
                _previewBaseScale = 0.24f;
                _previewActor.Position = new Vector3(0, 0.45f, 0);
                _pedestalRing.Color = pal.Accent;
                var root = new Entity(parent: _previewActor);
                new Entity(parent: root, model: "sphere", scale: new(9.5f, 1.2f, 9.5f), position: new(0, -0.4f, 0), color: pal.Ground);
                new Entity(parent: root, model: "diamond", scale: new(10.0f, 0.25f, 10.0f), color: pal.Accent, unlit: true);
                string stType = key is "roost" or "moon" ? "arch" : (key is "acres" or "bazaar" ? "monolith" : "crystals");
                WorldStructuresBuilder.BuildMapStructure(root, stType, pal, Vector3.Zero, 25f);
            }
            else if (category == "achievements" && Data.GameData.Achievements.TryGetValue(key, out var ach))
            {
                bool unlocked = Data.ProgressData.Instance.UnlockedAchievements.Contains(key);
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
            if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            {
                _screen = _screen == "lobby" ? "logbook" : "lobby";
                if (_screen == "lobby") SelectCharacter(_selectedChar);
                else SelectLogbookEntry(_logCategory, _logSelectedKey);
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

            // Вращение 3D-превью мышью (в правой половине экрана или ПКМ)
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
            Raylib.DrawRectangle(0, 0, sw, 58, Rgb(12, 16, 26, 235));
            Raylib.DrawRectangle(0, 58, sw, 3, Rgb(255, 165, 50));
            Engine.DrawTextCentered("RISK OF C# 2 : LAN & CO-OP (PHASE 2: 14 SURVIVORS & LOGBOOK)", sw * 0.5f, 29f, 24f, Rgb(255, 185, 60));

            string switchTxt = _screen == "lobby" ? "ОТКРЫТЬ ЖУРНАЛ (LOGBOOK) [TAB]" : "<- ВЕРНУТЬСЯ В ЛОББИ [TAB]";
            if (Engine.DrawButton(new Rectangle(sw - 340, 10, 320, 38), switchTxt, Rgb(95, 55, 145), Rgb(135, 80, 195), Color.White, 16f))
            {
                _screen = _screen == "lobby" ? "logbook" : "lobby";
                if (_screen == "lobby") SelectCharacter(_selectedChar);
                else SelectLogbookEntry(_logCategory, _logSelectedKey);
            }
        }

        private static void DrawLobbyUI()
        {
            DrawHeader();
            var cfg = SurvivorDatabase.Classes[_selectedChar];
            string[] hotkeys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "[", "]" };

            for (int i = 0; i < SurvivorDatabase.OrderedKeys.Length; i++)
            {
                string k = SurvivorDatabase.OrderedKeys[i];
                var scfg = SurvivorDatabase.Classes[k];
                int row = i / 7, col = i % 7;
                Rectangle rect = new(36 + col * 102, 74 + row * 48, 94, 40);
                bool isSel = k == _selectedChar;
                if (Engine.DrawButton(rect, $"[{hotkeys[i]}] {scfg.Icon}", Rgb(22, 28, 42, 245), scfg.BodyColor,
                    isSel ? Color.Yellow : Color.White, 15f, isSel ? Color.White : scfg.BodyColor))
                {
                    SelectCharacter(k);
                }
            }

            Raylib.DrawRectangle(36, 178, 700, 640, Rgb(12, 16, 28, 238));
            Raylib.DrawRectangle(36, 178, 700, 4, cfg.BodyColor);
            Engine.DrawTextLeft(cfg.Name, 56, 194, 28f, cfg.BodyColor);
            Engine.DrawTextLeft(cfg.Subtitle, 56, 228, 19f, Rgb(175, 210, 245));
            Engine.DrawTextLeft($"HP: {cfg.Hp}   |   Скорость: {cfg.Speed}   |   Урон: {cfg.Damage}   |   Атака: {cfg.FireRate}/с", 56, 258, 18f, Color.Gold);

            float sy = 300f;
            foreach (var skKey in new[] { "m1", "m2", "shift", "r" })
            {
                if (!cfg.Skills.TryGetValue(skKey, out var sk)) continue;
                Engine.DrawTextLeft($"• {sk.Name} (КД: {sk.Cd}с)", 56, sy, 18f, Color.SkyBlue);
                Engine.DrawTextLeft($"  {sk.Desc}", 56, sy + 24f, 16f, Color.White);
                sy += 64f;
            }

            string controls = $"FPS: {Raylib.GetFPS()}  |  Узлов Entity: {Engine.AllEntities.Count}  |  Зажмите мышь справа для 3D-осмотра";
            Engine.DrawTextLeft(controls, 56, 780, 16f, Rgb(155, 215, 255));

            if (Engine.DrawButton(new Rectangle(960, 740, 440, 66), "НАЧАТЬ ЗАБЕГ (СТАРТ) [ENTER]",
                Rgb(38, 120, 70), Rgb(55, 165, 95), Color.White, 22f, Color.Lime) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                StartRun(_selectedChar);
            }
        }

        public static void StartRun(string charKey)
        {
            _screen = "in_game";
            _mainPedestal.Enabled = false;
            _menuCrystal.Enabled = false;
            Raylib.DisableCursor();

            _run = new GameRunContext { CharKey = charKey };
            foreach (var k in GameData.ItemPool.Keys) _run.Items[k] = 0;
            WorldContext.Game = _run;

            _run.PlayerEntity = new Entity(position: new(0, charKey == "mindustry" ? 1.8f : 0f, 0));
            _run.Parts = SurvivorDatabase.BuildSurvivorMesh(_run.PlayerEntity, charKey);
            _run.Combat = new SurvivorCombatController(_run);
            if (charKey == "v1") _run.Parts.Root.Enabled = false; // FPS-режим для V1
            _run.PlayerHp = _run.PlayerMaxHp;

            LoadStage(1, null);
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
                var enemy = new CombatTarget(new Vector3(ex, ey, ez), t, 1.0f + (_run.Stage - 1) * 0.25f);
                CombatManager.Enemies.Add(enemy);
                _run.StageEntities.Add(enemy);
            }
        }

        public static void LoadStage(int stageNum, string? specialMode)
        {
            if (_run == null) return;
            foreach (var e in CombatManager.Enemies.ToArray()) Destroy(e);
            CombatManager.Enemies.Clear();
            foreach (var e in _run.StageEntities.ToArray()) Destroy(e);
            _run.StageEntities.Clear();

            _run.Stage = stageNum;
            _run.BlueOrbSpawned = false;
            _run.MoonPillarsDone = 0;
            _run.MithrixSpawned = false;
            _run.MoonDetonationTimer = 0f;
            _run.ActiveBoss = null;
            _run.DirectorCredits = 26.0f * _run.DifficultyCoeff;

            int seed = Environment.TickCount & 0x7FFFFFFF;
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

            if (specialMode == "bazaar")
                ShowBanner("БАЗАР МЕЖДУ ВРЕМЕНЕМ :: ТОРГОВЛЯ С ТРИТОНОМ", Rgb(80, 215, 255), 3.5f);
            else if (specialMode == "moon")
                ShowBanner("COMMENCEMENT :: ЗАРЯДИТЕ ВСЕ 4 СТОЛПА ЛУНЫ НА БАСТИОНАХ!", Rgb(100, 225, 255), 4.5f);
            else
                ShowBanner($"STAGE {stageNum}: {pal.Name.ToUpper()}", Color.Yellow, 3.0f);
        }

        public static void OnVictory()
        {
            QuitRunToLobby();
            ShowBanner("ПОБЕДА! ВЫ ПОКИНУЛИ ЛУНУ НА СПАСАТЕЛЬНОМ КОРАБЛЕ (+10 ЛУННЫХ МОНЕТ)!", Color.Gold, 8.0f);
        }

        private static void QuitRunToLobby()
        {
            if (_run != null)
            {
                foreach (var e in CombatManager.Enemies.ToArray()) Destroy(e);
                CombatManager.Enemies.Clear();
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
            _screen = "lobby";
            SelectCharacter(_selectedChar);
        }

        private static void UpdateInGame(float dt)
        {
            if (_run == null) return;

            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                QuitRunToLobby();
                return;
            }

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

            // Таймер детонации Луны после победы над Митриксом
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
            if (Physics.SpecialMode != "bazaar" && !_run.MithrixSpawned)
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
                        float ang = (float)rng.NextDouble() * MathF.PI * 2f;
                        float dist = 16.0f + (float)rng.NextDouble() * 12.0f;
                        float maxB = Physics.WorldBound - 4.0f;
                        float sx = Math.Clamp(_run.PlayerPos.X + MathF.Cos(ang) * dist, -maxB, maxB);
                        float sz = Math.Clamp(_run.PlayerPos.Z + MathF.Sin(ang) * dist, -maxB, maxB);
                        float sy = Physics.GetSurfaceHeight(sx, sz, _run.PlayerPos.Y);
                        var ne = new CombatTarget(new Vector3(sx, sy, sz), chosen.et, coeff);
                        CombatManager.Enemies.Add(ne);
                        _run.StageEntities.Add(ne);
                    }
                }
            }

            var p = _run.PlayerEntity;
            var c = _run.Combat;
            float prevY = p.Y;

            Vector2 md = Raylib.GetMouseDelta();
            p.RotationY += md.X * 0.18f;
            float maxPitch = _run.CharKey == "v1" ? 82f : 48f;
            float minPitch = _run.CharKey == "v1" ? -82f : -42f;
            _run.CamPitch = Math.Clamp(_run.CamPitch + md.Y * 0.18f, minPitch, maxPitch);

            float inX = (Raylib.IsKeyDown(KeyboardKey.D) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.A) ? 1f : 0f);
            float inZ = (Raylib.IsKeyDown(KeyboardKey.W) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.S) ? 1f : 0f);
            bool isMoving = MathF.Abs(inX) > 0.01f || MathF.Abs(inZ) > 0.01f;

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

            // Удержание игрока на арене Колизея во время дуэли с Митриксом
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

            Vector3 camLookDir = c.GetAimDirection();
            if (_run.CharKey == "v1")
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

        private static void DrawInGameHUD()
        {
            if (_run == null) return;
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            var c = _run.Combat;

            Engine.DrawTextCentered("+", sw * 0.5f, sh * 0.5f, 28f, Color.SkyBlue);

            // Верхняя панель инвентаря
            Raylib.DrawRectangle(24, 16, 860, 86, Rgb(14, 18, 28, 215));
            Engine.DrawTextLeft("INVENTORY  [HOLD TAB — Подробная статистика | F8: +Золото | F9: +Враги/Митрикс]", 38, 22, 16f, Rgb(175, 195, 225));
            if (_run.ItemOrder.Count == 0)
            {
                Engine.DrawTextLeft("Нет собранных предметов — открывайте сундуки клавишей [E]!", 38, 52, 17f, Rgb(125, 140, 165));
            }
            else
            {
                for (int i = 0; i < Math.Min(12, _run.ItemOrder.Count); i++)
                {
                    string k = _run.ItemOrder[i];
                    var info = GameData.ItemPool[k];
                    int cnt = _run.Items[k];
                    int bx = 38 + i * 68;
                    Raylib.DrawRectangle(bx, 46, 62, 46, Rgb(38, 48, 70, 235));
                    Raylib.DrawRectangle(bx, 46, 62, 4, info.Color);
                    Engine.DrawTextCentered($"{info.Short}\nx{cnt}", bx + 31, 70, 14f, info.Color);
                }
            }

            // Правая верхняя панель времени, сложности и этапа
            var (diffLabel, diffCol) = _run.GetDifficultyInfo();
            int mins = (int)(_run.ElapsedTime / 60f);
            int secs = (int)(_run.ElapsedTime % 60f);
            string detStr = _run.MoonDetonationTimer > 0f ? $"  |  ESCAPE: {_run.MoonDetonationTimer:F1}s!" : "";
            Raylib.DrawRectangle(sw - 460, 16, 436, 92, Rgb(14, 18, 28, 215));
            Raylib.DrawRectangle(sw - 460, 16, 436, 5, diffCol);
            Engine.DrawTextLeft(
                $"STAGE {_run.Stage}: {_run.StageName}{detStr}\n" +
                $"TIME: {mins:00}:{secs:00}  |  {diffLabel} (x{_run.DifficultyCoeff:F2})\n" +
                $"MONSTERS: {CombatManager.Enemies.Count}  |  F5/F6/F7/F8/F9",
                sw - 444, 26, 16f, Color.White);

            // Полоска здоровья Босса Телепорта / Митрикса по центру сверху
            if (_run.IsBossAlive && _run.ActiveBoss != null)
            {
                var boss = _run.ActiveBoss;
                string bTitle = boss.EnemyType == "mithrix"
                    ? $"MITHRIX [PHASE {boss.Phase}/4] :: KING OF NOTHING"
                    : _run.BossTitle;
                Color bCol = boss.EnemyType == "mithrix"
                    ? (boss.Phase == 4 ? Color.Magenta : Color.SkyBlue)
                    : Color.Red;
                Engine.DrawTextCentered(bTitle, sw * 0.5f, 128f, 22f, bCol);
                int bw = 640, bx = (sw - bw) / 2, by = 146;
                float bRatio = Math.Clamp(boss.Hp / MathF.Max(1f, boss.MaxHp), 0f, 1f);
                Raylib.DrawRectangle(bx, by, bw, 22, Rgb(45, 15, 15, 230));
                Raylib.DrawRectangle(bx, by, (int)(bw * bRatio), 22, bCol);
                Engine.DrawTextCentered($"{(int)MathF.Max(0, boss.Hp)} / {(int)boss.MaxHp}", sw * 0.5f, by + 11f, 16f, Color.White);
            }
            // Левая нижняя панель здоровья, щита и валюты
            Raylib.DrawRectangle(24, sh - 110, 440, 90, Rgb(14, 18, 28, 220));
            Engine.DrawTextLeft($"${_run.Gold}  |  LUNAR: {_run.LunarCoins}C  |  {SurvivorDatabase.Classes[_run.CharKey].Name}", 40, sh - 98, 20f, Color.Gold);
            float hpRatio = Math.Clamp(_run.PlayerHp / _run.PlayerMaxHp, 0f, 1f);
            Raylib.DrawRectangle(40, sh - 62, 408, 28, Rgb(50, 20, 22));
            Raylib.DrawRectangle(40, sh - 62, (int)(408 * hpRatio), 28, c.Shield >= 1f ? Rgb(75, 195, 255) : Rgb(85, 225, 95));
            string hpTxt = c.Shield >= 1f
                ? $"{(int)_run.PlayerHp} (+{(int)c.Shield}) / {(int)_run.PlayerMaxHp} HP"
                : $"{(int)_run.PlayerHp} / {(int)_run.PlayerMaxHp} HP";
            Engine.DrawTextCentered(hpTxt, 244, sh - 48, 18f, Color.White);

            // Правая нижняя панель 4 навыков (M1, M2, SHIFT, ULT [R])
            (string key, string title, float cd, Color col)[] skBoxes =
            {
                ("m1", "M1", c.M1Cd, Color.Yellow),
                ("m2", "M2", c.M2Cd, Color.Orange),
                ("shift", "SHIFT", c.ShiftCd, Color.SkyBlue),
                ("r", "ULT [R]", c.RCd, Color.Magenta)
            };
            for (int i = 0; i < skBoxes.Length; i++)
            {
                var (_, title, cd, col) = skBoxes[i];
                int sx = sw - 460 + i * 108;
                int sy = sh - 106;
                Raylib.DrawRectangle(sx, sy, 96, 82, cd <= 0f ? Rgb(35, 55, 75, 235) : Rgb(22, 24, 32, 235));
                Raylib.DrawRectangle(sx, sy, 96, 4, col);
                Engine.DrawTextCentered(title, sx + 48, sy + 24, 16f, col);
                Engine.DrawTextCentered(cd <= 0f ? "READY" : $"{cd:F1}s", sx + 48, sy + 56, 17f, cd <= 0f ? Color.White : Color.Red);
            }

            // Панель ULTRAKILL STYLE METER для V1
            if (_run.CharKey == "v1")
            {
                var (rName, rCol, rMult) = c.V1GetStyleRankInfo();
                Raylib.DrawRectangle(sw - 380, 130, 356, 220, Rgb(10, 12, 20, 225));
                Raylib.DrawRectangle(sw - 380, 130, 356, 5, rCol);
                Engine.DrawTextLeft($"{rName} (x{rMult:F2})", sw - 364, 142, 24f, rCol);
                Raylib.DrawRectangle(sw - 364, 174, 320, 12, Rgb(35, 38, 50));
                Raylib.DrawRectangle(sw - 364, 174, (int)(320 * (c.V1StylePoints / 1000f)), 12, rCol);
                for (int i = 0; i < c.V1StyleLog.Count; i++)
                    Engine.DrawTextLeft(c.V1StyleLog[i].Text, sw - 364, 196 + i * 22, 16f, c.V1StyleLog[i].Col);

                Engine.DrawTextLeft($"ОРУЖИЕ [{c.V1Weapon}/5] | МОНЕТЫ: {(int)c.V1Coins}/4 | RAIL: {(int)c.V1RailCharge}% | СТАМИНА: {(int)c.V1Stamina}/3",
                    sw - 540, sh - 135, 16f, Color.Gold);
            }

            // Детальная статистика по удержанию [TAB]
            if (Raylib.IsKeyDown(KeyboardKey.Tab))
            {
                Raylib.DrawRectangle(sw / 2 - 450, sh / 2 - 240, 900, 480, Rgb(12, 16, 26, 245));
                Raylib.DrawRectangle(sw / 2 - 450, sh / 2 - 240, 900, 4, Color.SkyBlue);
                Engine.DrawTextLeft($"=== SURVIVOR OVERVIEW: {SurvivorDatabase.Classes[_run.CharKey].Name} ===", sw / 2 - 420, sh / 2 - 215, 22f, Color.Gold);
                Engine.DrawTextLeft($"Base Damage: {c.BaseDamage:F1}   |   Fire Rate: {c.FireRate:F1}/s   |   Move Speed: {c.MoveSpeed:F1}   |   Crit: {c.CritChance * 100f:F0}%",
                    sw / 2 - 420, sh / 2 - 175, 18f, Color.White);
                int sy = sh / 2 - 125;
                foreach (var k in _run.ItemOrder)
                {
                    if (sy > sh / 2 + 200) break;
                    var info = GameData.ItemPool[k];
                    Engine.DrawTextLeft($"• {info.Name} [x{_run.Items[k]}]: {info.Desc}", sw / 2 - 420, sy, 17f, info.Color);
                    sy += 26;
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