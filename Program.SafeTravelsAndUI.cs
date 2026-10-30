using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;
using Raylib_cs;
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
        public float BossDamageDealt = 0f;
        public bool ZeroCooldownSandbox = false;
        public bool IsSafeTravels => Physics.SpecialMode == "safe_travels";

        public void OpenNpcDialogue(string npcCharKey, int npcSkinIdx = 0)
            => Program.OpenSafeTravelsDialogue(npcCharKey, npcSkinIdx, null);

        public void OpenNpcDialogue(string npcCharKey, int npcSkinIdx, Entity? npcEntity)
            => Program.OpenSafeTravelsDialogue(npcCharKey, npcSkinIdx, npcEntity);

        public void StartNpcRps(string npcCharKey, int npcSkinIdx = 0)
            => Program.StartRpsMiniGame(npcCharKey, npcSkinIdx, null, null);

        public void StartNpcRps(string npcCharKey, int npcSkinIdx, Entity? npcEntity)
            => Program.StartRpsMiniGame(npcCharKey, npcSkinIdx, npcEntity, null);

        public void TriggerCharacterUnlockCutscene(string unlockedCharKey)
            => Program.StartCharacterUnlockCutscene(unlockedCharKey);

        public void ExitSafeTravelsToMenu()
            => Program.LeaveSafeTravelsToMainMenu();

        public void StartRunFromSafeTravels()
            => Program.LaunchRunFromSafeTravelsPod();

        public void SwitchSafeTravelsCharacter(string charKey, int skinIdx = 0)
            => Program.SwitchPlayerPersonaOnSafeTravels(charKey, skinIdx);

        public void OpenSafeTravelsCharacterSelector()
            => Program.OpenSafeTravelsWardrobe();

        public void ClearSafeTravelsTestItems()
        {
            foreach (var k in GameData.ItemPool.Keys.ToArray())
                Items[k] = 0;
            ItemOrder.Clear();
            if (Combat != null)
            {
                Combat.Shield = 0f;
                Combat.TonicBuffTimer = 0f;
            }
            PlayerHp = PlayerMaxHp;
            SoundManager.Play("ui_click");
            ShowBanner("ТЕСТОВЫЙ ИНВЕНТАРЬ ПОЛИГОНА ОЧИЩЕН! ЗДОРОВЬЕ ВОССТАНОВЛЕНО.", Color.SkyBlue, 3.0f);
        }

        public void ToggleSafeTravelsZeroCooldowns()
        {
            ZeroCooldownSandbox = !ZeroCooldownSandbox;
            SoundManager.Play("ui_ready");
            ShowBanner(
                ZeroCooldownSandbox
                    ? "РЕЖИМ ПЕСОЧНИЦЫ ПОЛИГОНА: ПЕРЕЗАРЯДКА НАВЫКОВ ОТКЛЮЧЕНА (0 СЕК)!"
                    : "РЕЖИМ ПЕСОЧНИЦЫ ПОЛИГОНА: СТАНДАРТНАЯ ПЕРЕЗАРЯДКА НАВЫКОВ ВОЗВРАЩЕНА.",
                ZeroCooldownSandbox ? Color.Lime : Color.Gold,
                3.2f);
        }
    }

    public static partial class Program
    {
        // =========================================================================================
        // 1. КОЛЕСО ЭМОЦИЙ НА КЛАВИШУ [L] (10 ЭМОЦИЙ + БЕЛЫЙ ПЛАСТИКОВЫЙ СТУЛ + КАМЕРА 3-ГО ЛИЦА)
        // =========================================================================================
        private readonly record struct EmoteWheelItem(string Key, string Title, string Sub, Color Accent);

        private static readonly EmoteWheelItem[] _emoteWheelItems =
        {
            new("chair", "1. Белый пластиковый стул", "Мотивация и покой в буре", Rgb(235, 242, 255)),
            new("fortnite", "2. Танец Фортнайта", "Классический Default Dance", Rgb(85, 215, 255)),
            new("california", "3. Калифорния гёрлс", "California Gurls мем-танец", Rgb(255, 115, 205)),
            new("caramelldansen", "4. Карамеллдансен", "Ушки на макушке и диско-бит", Rgb(255, 165, 65)),
            new("sun", "5. Восхвали Солнце!", "Praise the Sun \\[T]/", Rgb(255, 225, 75)),
            new("kazotsky", "6. Танец вприсядку", "Казачок / Kazotsky Kick", Rgb(255, 85, 75)),
            new("gangnam", "7. Gangnam Style", "Ритмичный скачок наездника", Rgb(95, 240, 145)),
            new("jojo", "8. JoJo Поза", "Зловещая аура Menacing", Rgb(205, 105, 255)),
            new("distraction", "9. Танец Отвлечения", "Генри Стикмин одобряет", Rgb(125, 225, 245)),
            new("rps_wait", "0. Камень-Ножницы-Бумага", "Пригласить игрока или NPC", Rgb(255, 195, 85))
        };

        private static bool _emoteWheelOpen = false;
        private static float _emoteWheelOpenDuration = 0f;
        private static int _emoteHoveredIdx = -1;
        private static float _emoteBeatAccum = 0f;
        private static float _emoteCamYawOffset = 0f;
        private static Entity? _localChairProp = null;

        private static bool IsDanceBeatEmote(string em)
            => em is "fortnite" or "california" or "caramelldansen" or "kazotsky" or "gangnam" or "distraction";

        public static bool HasAnyInGameModalOpen()
            => (_commandPickerQueue.Count > 0) || _workbenchOpen || _dialogueOpen || _rpsOpen || _emoteWheelOpen || _safeTravelsWardrobeOpen || (_cutsceneTimer > 0f);

        public static void RecordInteractableOpened()
        {
            if (_run == null || _run.IsSafeTravels) return;
            _run.ChestsOpened++;
            if (_run.ChestsOpened >= 4) _run.UnlockAchievement("ach_chest_opener");
        }

        private static void RestoreInGameCursorIfNeeded()
        {
            if (_paused || HasAnyInGameModalOpen() || (_run != null && (SurvivorDatabase.IsMobaCharacter(_run.CharKey) || _run.CharKey == "mindustry")))
                Raylib.EnableCursor();
            else
                Raylib.DisableCursor();
        }

        private static void EnsureLocalChairProp(bool visible)
        {
            if (_run == null || !IsValid(_run.PlayerEntity))
            {
                if (IsValid(_localChairProp)) Destroy(_localChairProp!);
                _localChairProp = null;
                return;
            }

            if (visible)
            {
                if (!IsValid(_localChairProp) || _localChairProp!.Parent != _run.PlayerEntity)
                {
                    if (IsValid(_localChairProp)) Destroy(_localChairProp!);
                    _localChairProp = RemotePlayer.BuildPlasticChairEntity(_run.PlayerEntity);
                }
                _localChairProp.Position = new Vector3(0f, 0f, -0.10f);
                _localChairProp.Scale = _run.CharKey == "mindustry"
                    ? new Vector3(1.22f, 1.0f, 1.22f)
                    : Vector3.One;
                _localChairProp.Enabled = true;
            }
            else if (IsValid(_localChairProp))
            {
                _localChairProp!.Enabled = false;
            }
        }

        public static void SetLocalPlayerEmote(string emoteKey)
        {
            if (_run == null) return;
            _run.Parts.SetEmote(emoteKey);
            EnsureLocalChairProp(emoteKey == "chair");
            _emoteBeatAccum = 0.05f;
            if (!string.IsNullOrEmpty(emoteKey))
                _emoteCamYawOffset = 0f;

            bool isFp = SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey);
            if (isFp && _run.Combat.MedicTauntTimer <= 0f)
            {
                bool active = !string.IsNullOrEmpty(emoteKey);
                _run.Parts.Root.Enabled = active;
                if (_run.Combat.FpRoot != null && !_run.Combat.FpRoot.IsDestroyed)
                    _run.Combat.FpRoot.Enabled = !active;
            }

            if (!string.IsNullOrEmpty(emoteKey))
            {
                var found = Array.Find(_emoteWheelItems, x => x.Key == emoteKey);
                if (!string.IsNullOrEmpty(found.Title))
                    ShowBanner($"ЭМОЦИЯ: {found.Title.Substring(found.Title.IndexOf(' ') + 1)}  [Движение или ЛКМ — прервать]", found.Accent, 2.2f);
            }
        }

        private static void UpdateEmoteWheelAndActiveEmote(float dt)
        {
            if (_run == null) return;

            // Открытие / закрытие колеса эмоций по клавише [L]
            if (!_dialogueOpen && !_rpsOpen && !_workbenchOpen && _commandPickerQueue.Count == 0 && !_safeTravelsWardrobeOpen && _cutsceneTimer <= 0f)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.L))
                {
                    _emoteWheelOpen = !_emoteWheelOpen;
                    _emoteWheelOpenDuration = 0f;
                    SoundManager.Play("ui_click", 0.85f);
                    RestoreInGameCursorIfNeeded();
                }
            }

            if (_emoteWheelOpen)
            {
                if (Raylib.IsCursorHidden()) Raylib.EnableCursor();
                _emoteWheelOpenDuration += dt;

                int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
                Vector2 center = new(sw * 0.5f, sh * 0.5f);
                Vector2 mp = Raylib.GetMousePosition();
                Vector2 delta = mp - center;
                float dist = delta.Length();

                if (dist < 68f)
                {
                    _emoteHoveredIdx = -2; // Центр — остановить эмоцию
                }
                else if (dist <= 315f)
                {
                    float angDeg = MathF.Atan2(delta.Y, delta.X) * (180f / MathF.PI) + 90f;
                    if (angDeg < 0f) angDeg += 360f;
                    float sliceDeg = 360f / _emoteWheelItems.Length;
                    int idx = (int)MathF.Floor(((angDeg + sliceDeg * 0.5f) % 360f) / sliceDeg);
                    _emoteHoveredIdx = Math.Clamp(idx, 0, _emoteWheelItems.Length - 1);
                }
                else
                {
                    _emoteHoveredIdx = -1;
                }

                // Выбор цифрами 1..9, 0
                KeyboardKey[] hotkeys =
                {
                    KeyboardKey.One, KeyboardKey.Two, KeyboardKey.Three, KeyboardKey.Four, KeyboardKey.Five,
                    KeyboardKey.Six, KeyboardKey.Seven, KeyboardKey.Eight, KeyboardKey.Nine, KeyboardKey.Zero
                };
                for (int i = 0; i < hotkeys.Length && i < _emoteWheelItems.Length; i++)
                {
                    if (Raylib.IsKeyPressed(hotkeys[i]))
                    {
                        SetLocalPlayerEmote(_emoteWheelItems[i].Key);
                        _emoteWheelOpen = false;
                        RestoreInGameCursorIfNeeded();
                        return;
                    }
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Backspace))
                {
                    SetLocalPlayerEmote("");
                    _emoteWheelOpen = false;
                    RestoreInGameCursorIfNeeded();
                    return;
                }

                bool clicked = Raylib.IsMouseButtonPressed(MouseButton.Left);
                bool releasedHold = _emoteWheelOpenDuration >= 0.26f && Raylib.IsKeyReleased(KeyboardKey.L) && _emoteHoveredIdx != -1;
                if (clicked || releasedHold)
                {
                    if (_emoteHoveredIdx >= 0 && _emoteHoveredIdx < _emoteWheelItems.Length)
                        SetLocalPlayerEmote(_emoteWheelItems[_emoteHoveredIdx].Key);
                    else if (_emoteHoveredIdx == -2)
                        SetLocalPlayerEmote("");

                    _emoteWheelOpen = false;
                    SoundManager.Play("ui_ready", 0.8f);
                    RestoreInGameCursorIfNeeded();
                    return;
                }
            }

            // Поддержание стула, 3-го лица и ритмичного бита во время активной эмоции
            string curEmote = _run.Parts.ActiveEmote;
            EnsureLocalChairProp(curEmote == "chair");

            if (SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey) && _run.Combat.MedicTauntTimer <= 0f)
            {
                bool showBody = !string.IsNullOrEmpty(curEmote) || _dialogueOpen || _cutsceneTimer > 0f;
                _run.Parts.Root.Enabled = showBody;
                if (_run.Combat.FpRoot != null && !_run.Combat.FpRoot.IsDestroyed)
                    _run.Combat.FpRoot.Enabled = !showBody;
            }

            if (IsDanceBeatEmote(curEmote))
            {
                _emoteBeatAccum -= dt;
                if (_emoteBeatAccum <= 0f)
                {
                    _emoteBeatAccum = curEmote is "caramelldansen" or "kazotsky" ? 0.36f : 0.48f;
                    SoundManager.Play("emote_beat", 0.65f);
                }
            }

            // Кооперативный вызов на Камень-Ножницы-Бумага с живым игроком по [E]
            if (!_rpsOpen && !_dialogueOpen && Raylib.IsKeyPressed(KeyboardKey.E))
            {
                foreach (var kv in _remotePlayers)
                {
                    var rp = kv.Value;
                    if (IsValid(rp) && rp.Hp > 0f && rp.ActiveEmote == "rps_wait" &&
                        Vector3.Distance(_run.PlayerPos, rp.Position) <= 4.5f)
                    {
                        StartRpsMiniGame(rp.CharKey, rp.SkinIdx, rp, kv.Key);
                        break;
                    }
                }
            }
        }

        private static void DrawEmoteWheelOverlay(int sw, int sh)
        {
            if (!_emoteWheelOpen || _run == null) return;

            Raylib.DrawRectangle(0, 0, sw, sh, Rgb(6, 10, 18, 155));
            Vector2 center = new(sw * 0.5f, sh * 0.5f);

            Engine.DrawTextCentered("КОЛЕСО ЭМОЦИЙ ВЫЖИВШЕГО  [L / ЛКМ / Клавиши 1–0]", center.X, center.Y - 332f, 22f, Color.Gold);
            string subHint = _run.CharKey == "mindustry"
                ? "Пилотажный модуль корабля Mindustry адаптирует каждую эмоцию в фигуру высшего пилотажа!"
                : "Выберите жест курсором или цифрой 1–0  |  Центр или движение прерывает эмоцию";
            Engine.DrawTextCentered(subHint, center.X, center.Y - 304f, 14f, Rgb(175, 215, 245));

            float outerR = 248f;
            float innerR = 72f;
            int count = _emoteWheelItems.Length;
            float sliceDeg = 360f / count;

            for (int i = 0; i < count; i++)
            {
                var item = _emoteWheelItems[i];
                bool hov = _emoteHoveredIdx == i;
                bool activeNow = _run.Parts.ActiveEmote == item.Key;
                float startAng = -90f - sliceDeg * 0.5f + i * sliceDeg;
                float endAng = startAng + sliceDeg - 2.0f;

                Color fill = hov
                    ? Rgb(44, 78, 132, 242)
                    : (activeNow ? Rgb(36, 62, 96, 232) : Rgb(16, 22, 36, 224));
                Raylib.DrawRing(center, innerR, hov ? outerR + 10f : outerR, startAng, endAng, 24, fill);
                Raylib.DrawRingLines(center, innerR, hov ? outerR + 10f : outerR, startAng, endAng, 24,
                    hov ? Color.Yellow : item.Accent);

                float midRad = (-90f + i * sliceDeg) * (MathF.PI / 180f);
                float cardDist = 172f;
                Vector2 labelPos = center + new Vector2(MathF.Cos(midRad) * cardDist, MathF.Sin(midRad) * cardDist);
                Engine.DrawTextCentered(item.Title, labelPos.X, labelPos.Y - 7f, hov ? 14f : 13f, hov ? Color.Yellow : item.Accent);
                Engine.DrawTextCentered(item.Sub, labelPos.X, labelPos.Y + 10f, 11f, Rgb(205, 220, 240));
            }

            // Центральная кнопка отмены эмоции
            bool centerHov = _emoteHoveredIdx == -2;
            Raylib.DrawCircleV(center, innerR - 4f, centerHov ? Rgb(125, 42, 54, 245) : Rgb(20, 26, 42, 240));
            Raylib.DrawCircleLines((int)center.X, (int)center.Y, innerR - 4f, centerHov ? Color.Yellow : Color.SkyBlue);
            Engine.DrawTextCentered(
                string.IsNullOrEmpty(_run.Parts.ActiveEmote) ? "ЗАКРЫТЬ\n[ESC / L]" : "СТОП\nЭМОЦИИ",
                center.X, center.Y, 14f, centerHov ? Color.Yellow : Color.White);

            if (_emoteHoveredIdx >= 0 && _emoteHoveredIdx < count)
            {
                var sel = _emoteWheelItems[_emoteHoveredIdx];
                Raylib.DrawRectangle((int)center.X - 270, (int)center.Y + 278, 540, 58, Rgb(12, 18, 30, 242));
                Raylib.DrawRectangle((int)center.X - 270, (int)center.Y + 278, 540, 3, sel.Accent);
                Engine.DrawTextCentered(sel.Title, center.X, center.Y + 296f, 18f, sel.Accent);
                Engine.DrawTextCentered(sel.Sub, center.X, center.Y + 318f, 14f, Color.White);
            }
        }

        // =========================================================================================
        // 2. КОРАБЛЬ UES «SAFE TRAVELS»: ВХОД, СМЕНА ГЕРОЕВ/АЛЬТ-ВЕРСИЙ И БОРТОВОЙ ИНТЕРФЕЙС
        // =========================================================================================
        private static bool _safeTravelsWardrobeOpen = false;
        private static int _safeTravelsWardrobePage = 0;

        public static bool IsSafeTravelsUnlocked()
            => ProgressData.Instance.MithrixDefeated
               || ProgressData.Instance.UnlockedAchievements.Contains("ach_safe_travels")
               || ProgressData.Instance.Victories > 0;

        public static void EnterSafeTravels(string? overrideCharKey = null)
        {
            _net.StopSession();
            _lobbyPlayers.Clear();
            CloseSafeTravelsDialogueModal();
            _rpsOpen = false;
            _emoteWheelOpen = false;
            _safeTravelsWardrobeOpen = false;
            _cutsceneTimer = 0f;

            string charKey = overrideCharKey ?? _selectedChar;
            bool charAllowedOnShip = charKey == "sanabi"
                ? (ProgressData.Instance.IsSanabiAuthorized() || IsSafeTravelsUnlocked())
                : ProgressData.Instance.IsCharacterUnlocked(charKey);
            if (!SurvivorDatabase.Classes.ContainsKey(charKey) || !charAllowedOnShip)
                charKey = "commando";

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

            _run = new GameRunContext
            {
                CharKey = charKey,
                LunarCoins = Math.Max(2, ProgressData.Instance.LunarCoins)
            };
            foreach (var k in GameData.ItemPool.Keys) _run.Items[k] = 0;
            WorldContext.Game = _run;

            int skinIdx = ProgressData.Instance.GetSelectedSkin(charKey);
            _run.PlayerEntity = new Entity(position: new(0, charKey == "mindustry" ? 1.2f : 0f, 0));
            _run.Parts = SurvivorDatabase.BuildSurvivorMesh(_run.PlayerEntity, charKey, skinIdx);
            _run.Combat = new SurvivorCombatController(_run);
            _isSpectating = false;
            if (SurvivorCombatController.IsFirstPersonCharacter(charKey))
                _run.Parts.Root.Enabled = false;
            _run.PlayerHp = _run.PlayerMaxHp;

            ProgressData.Instance.DiscoveredStages.Add("safe_travels");
            ProgressData.Instance.Save();

            LoadStage(1, "safe_travels", fixedSeed: 2056, fromNetwork: true);
            ShowBanner("ДОБРО ПОЖАЛОВАТЬ НА БОРТ UES «SAFE TRAVELS»! [E] Диалоги | [H] Смена героя | [L] Эмоции", Rgb(85, 225, 255), 5.5f);
        }

        public static void LeaveSafeTravelsToMainMenu()
        {
            CloseSafeTravelsDialogueModal();
            _rpsOpen = false;
            _emoteWheelOpen = false;
            _safeTravelsWardrobeOpen = false;
            EnsureLocalChairProp(false);
            QuitRunToLobby(toMainMenu: true);
        }

        public static void LaunchRunFromSafeTravelsPod()
        {
            string launchChar = _run != null ? _run.CharKey : _selectedChar;
            if (!ProgressData.Instance.IsCharacterUnlocked(launchChar))
                launchChar = "commando";
            _dialogueOpen = false;
            _rpsOpen = false;
            _emoteWheelOpen = false;
            _safeTravelsWardrobeOpen = false;
            EnsureLocalChairProp(false);
            QuitRunToLobby(toMainMenu: false);
            _selectedChar = launchChar;
            StartRun(launchChar);
        }

        public static void OpenSafeTravelsWardrobe()
        {
            if (_run == null || !_run.IsSafeTravels) return;
            _safeTravelsWardrobeOpen = true;
            Raylib.EnableCursor();
            SoundManager.Play("ui_click");
        }

        public static void SwitchPlayerPersonaOnSafeTravels(string charKey, int skinIdx = 0)
        {
            if (_run == null || !SurvivorDatabase.Classes.ContainsKey(charKey)) return;

            skinIdx = Math.Clamp(skinIdx, 0, Math.Max(0, SurvivorDatabase.Classes[charKey].Skins.Count - 1));
            EnsureLocalChairProp(false);

            foreach (var t in _run.Combat.MindustryTurrets)
                if (IsValid(t)) Destroy(t);
            _run.Combat.MindustryTurrets.Clear();
            if (IsValid(_run.Combat.MindustryMainCore))
            {
                Destroy(_run.Combat.MindustryMainCore!);
                _run.Combat.MindustryMainCore = null;
            }
            if (IsValid(_run.Combat.FpRoot))
            {
                Destroy(_run.Combat.FpRoot!);
                _run.Combat.FpRoot = null;
            }
            if (IsValid(_run.Parts.Root))
                Destroy(_run.Parts.Root);

            _selectedChar = charKey;
            ProgressData.Instance.SelectedSkins[charKey] = skinIdx;
            _run.CharKey = charKey;
            SurvivorDatabase.Classes[charKey].SyncLoadout();

            _run.Parts = SurvivorDatabase.BuildSurvivorMesh(_run.PlayerEntity, charKey, skinIdx);
            _run.Combat = new SurvivorCombatController(_run);
            _run.Parts.Root.Enabled = !SurvivorCombatController.IsFirstPersonCharacter(charKey);
            if (SurvivorCombatController.IsFirstPersonCharacter(charKey))
                _run.Combat.BuildV1FpViewmodel();
            if (charKey == "mindustry")
                _run.Combat.SpawnMindustryMainCore(_run.PlayerPos + new Vector3(3.5f, 0f, 0f));

            _run.PlayerHp = _run.PlayerMaxHp;
            new VisualFX(_run.PlayerPos + new Vector3(0f, 1.2f, 0f), SurvivorDatabase.Classes[charKey].BodyColor, 3.2f, 0.35f);
            SoundManager.Play("ui_select_char");

            string pKey = SafeTravelsDialogues.GetPersonaKey(charKey, skinIdx);
            string disp = SafeTravelsDialogues.GetPersonaDisplayName(pKey);
            ShowBanner($"АКТИВНЫЙ ПЕРСОНАЖ НА КОРАБЛЕ: {disp.ToUpper()}!", Color.Gold, 3.2f);
            RestoreInGameCursorIfNeeded();
        }

        private static string GetSafeTravelsDeckSectorName(Vector3 pos)
        {
            if (pos.Z >= 34f) return "КОМАНДНЫЙ МОСТИК И СМОТРОВАЯ ПАЛУБА";
            if (pos.Z <= -34f) return "ДЕСАНТНЫЙ АНГАР КАПСУЛ И ШЛЮЗ ЭВАКУАЦИИ";
            if (pos.X <= -22f) return "БОЕВОЙ ТРЕНИРОВОЧНЫЙ ПОЛИГОН (ПРОВЕРКА DPS И СБОРОК)";
            if (pos.X >= 22f) return "ЖИЛЫЕ КАЮТЫ ЭКИПАЖА, МЕДБЛОК И ИНЖЕНЕРНЫЙ ОТСЕК";
            return "ЦЕНТРАЛЬНАЯ КАЮТ-КОМПАНИЯ И БАР ВЫЖИВШИХ";
        }

        private static void DrawSafeTravelsHubOverlay(int sw, int sh)
        {
            if (_run == null || !_run.IsSafeTravels) return;

            string curPersona = SafeTravelsDialogues.GetPersonaKey(_run.CharKey, ProgressData.Instance.GetSelectedSkin(_run.CharKey));
            string personaTitle = SafeTravelsDialogues.GetPersonaDisplayName(curPersona);
            string sector = GetSafeTravelsDeckSectorName(_run.PlayerPos);

            int bw = 720, bx = (sw - bw) / 2, by = 10;
            if (!_run.IsBossAlive)
            {
                Raylib.DrawRectangle(bx, by, bw, 46, Rgb(10, 16, 28, 212));
                Raylib.DrawRectangle(bx, by, bw, 3, Rgb(75, 215, 255));
                Engine.DrawTextCentered($"UES «SAFE TRAVELS» :: {sector}", sw * 0.5f, by + 14f, 15f, Rgb(95, 225, 255));
                string cdInfo = _run.ZeroCooldownSandbox ? " | КД: 0 СЕК (ПЕСОЧНИЦА)" : "";
                Engine.DrawTextCentered(
                    $"Вы играете за: {personaTitle}  |  [E] Поговорить  |  [K/R] К-Н-Б  |  [H] Сменить героя  |  [L] Эмоции{cdInfo}",
                    sw * 0.5f, by + 34f, 12f, Color.Gold);
            }

            if (_safeTravelsWardrobeOpen && !_paused)
                DrawSafeTravelsWardrobeModal(sw, sh);
        }

        private static void DrawSafeTravelsWardrobeModal(int sw, int sh)
        {
            if (_run == null) return;
            if (Raylib.IsCursorHidden()) Raylib.EnableCursor();

            // Собираем список всех 39 личностей экипажа (открытые доступны сразу, закрытые показывают условие открытия)
            var entries = new List<(string CharKey, int SkinIdx, string Title, string Sub, Color Col, bool Unlocked)>();
            int unlockedCount = 0;
            foreach (var pKey in SafeTravelsDialogues.AllPersonaKeys)
            {
                var (cKey, sIdx) = SafeTravelsDialogues.SplitPersonaKey(pKey);
                if (!SurvivorDatabase.Classes.TryGetValue(cKey, out var cfg)) continue;
                bool charUnlocked = cKey == "sanabi"
                    ? (ProgressData.Instance.IsSanabiAuthorized() || IsSafeTravelsUnlocked())
                    : ProgressData.Instance.IsCharacterUnlocked(cKey);
                bool skinUnlocked = sIdx <= 0 || (sIdx < cfg.Skins.Count && ProgressData.Instance.IsSkinUnlocked(cKey, sIdx, cfg.Skins[sIdx].UnlockAchievementKey));
                bool unlocked = charUnlocked && skinUnlocked;
                if (unlocked) unlockedCount++;

                Color col = sIdx < cfg.Skins.Count ? cfg.Skins[sIdx].PrimaryColor : cfg.BodyColor;
                string sub = !unlocked
                    ? "Нажмите, чтобы узнать условие открытия"
                    : (sIdx > 0 ? "Альтернативная личность на борту" : cfg.Subtitle);
                entries.Add((cKey, sIdx, SafeTravelsDialogues.GetPersonaDisplayName(pKey), sub, col, unlocked));
            }

            int mw = 1040, mh = 640, mx = (sw - mw) / 2, my = (sh - mh) / 2;
            Raylib.DrawRectangle(0, 0, sw, sh, Rgb(6, 10, 18, 175));
            Raylib.DrawRectangle(mx, my, mw, mh, Rgb(12, 18, 32, 250));
            Raylib.DrawRectangle(mx, my, mw, 4, Color.SkyBlue);

            Engine.DrawTextLeft("ТЕРМИНАЛ ЭКИПАЖА UES «SAFE TRAVELS» :: ВЫБОР ПЕРСОНАЖА ИЛИ АЛЬТ-ВЕРСИИ", mx + 24, my + 16, 19f, Color.SkyBlue);
            Engine.DrawTextLeft("Переключайтесь между любыми открытыми героями и их альт-версиями, чтобы увидеть уникальные диалоги со всем экипажем!", mx + 24, my + 42, 13f, Color.Gold);

            if (Engine.DrawButton(new Rectangle(mx + mw - 180, my + 12, 156, 34), "ЗАКРЫТЬ [ESC/H]",
                Rgb(115, 38, 48), Rgb(160, 55, 68), Color.White, 13f, Color.Red))
            {
                _safeTravelsWardrobeOpen = false;
                RestoreInGameCursorIfNeeded();
                return;
            }

            int perPage = 16;
            int totalPages = Math.Max(1, (entries.Count - 1) / perPage + 1);
            _safeTravelsWardrobePage = Math.Clamp(_safeTravelsWardrobePage, 0, totalPages - 1);
            int start = _safeTravelsWardrobePage * perPage;

            string curPersona = SafeTravelsDialogues.GetPersonaKey(_run.CharKey, ProgressData.Instance.GetSelectedSkin(_run.CharKey));

            for (int i = 0; i < perPage && start + i < entries.Count; i++)
            {
                var item = entries[start + i];
                int r = i / 4, col = i % 4;
                Rectangle rect = new(mx + 24 + col * 248, my + 74 + r * 116, 238, 104);
                string itemPersona = SafeTravelsDialogues.GetPersonaKey(item.CharKey, item.SkinIdx);
                bool isCur = itemPersona == curPersona;

                string titlePrefix = item.Unlocked ? "" : "[ЗАКР.] ";
                if (Engine.DrawButton(rect, $"{titlePrefix}{item.Title}\n{item.Sub}",
                    isCur ? Rgb(42, 86, 148) : (item.Unlocked ? Rgb(20, 28, 44) : Rgb(14, 16, 26)), Rgb(56, 104, 175),
                    isCur ? Color.Yellow : (item.Unlocked ? Color.White : Rgb(145, 155, 175)), 13f,
                    isCur ? Color.Yellow : (item.Unlocked ? item.Col : Color.Gray)))
                {
                    if (item.Unlocked)
                    {
                        SwitchPlayerPersonaOnSafeTravels(item.CharKey, item.SkinIdx);
                        _safeTravelsWardrobeOpen = false;
                        return;
                    }
                    else
                    {
                        SoundManager.Play("ui_error");
                        string howMsg = item.SkinIdx > 0 && ProgressData.Instance.IsCharacterUnlocked(item.CharKey) && SurvivorDatabase.Classes.TryGetValue(item.CharKey, out var scfg)
                            ? GetSkinUnlockInfo(scfg, item.SkinIdx)
                            : GetCharacterUnlockInfo(item.CharKey).HowTo;
                        ShowBanner($"ЗАКРЫТО :: {howMsg}", Color.Orange, 3.8f);
                    }
                }
            }

            int botY = my + mh - 62;
            if (Engine.DrawButton(new Rectangle(mx + 24, botY, 150, 40), "<- ПРЕД.", Rgb(36, 48, 72), Rgb(58, 78, 114), Color.White, 14f))
                _safeTravelsWardrobePage = Math.Clamp(_safeTravelsWardrobePage - 1, 0, totalPages - 1);
            Engine.DrawTextCentered($"Страница {_safeTravelsWardrobePage + 1} / {totalPages}  (Открыто личностей: {unlockedCount}/{entries.Count})", mx + 360, botY + 20, 15f, Color.White);
            if (Engine.DrawButton(new Rectangle(mx + 560, botY, 150, 40), "СЛЕД. ->", Rgb(36, 48, 72), Rgb(58, 78, 114), Color.White, 14f))
                _safeTravelsWardrobePage = Math.Clamp(_safeTravelsWardrobePage + 1, 0, totalPages - 1);

            if (Engine.DrawButton(new Rectangle(mx + 730, botY, 286, 40),
                _run.ZeroCooldownSandbox ? "КД НАВЫКОВ: 0 СЕК [ВКЛ]" : "ОТКЛЮЧИТЬ КД НА ПОЛИГОНЕ",
                _run.ZeroCooldownSandbox ? Rgb(36, 105, 68) : Rgb(44, 56, 82), Rgb(62, 128, 92), Color.White, 13f, Color.Lime))
            {
                _run.ToggleSafeTravelsZeroCooldowns();
            }
        }

        // =========================================================================================
        // 3. СИСТЕМА ДИАЛОГОВ ВСЕХ ПЕРСОНАЖЕЙ НА БОРТУ UES «SAFE TRAVELS»
        // =========================================================================================
        private static bool _dialogueOpen = false;
        private static string _dialogueNpcCharKey = "commando";
        private static int _dialogueNpcSkinIdx = 0;
        private static Entity? _dialogueNpcEntity = null;
        private static IReadOnlyList<DialogueLine> _dialogueLines = Array.Empty<DialogueLine>();
        private static int _dialogueLineIdx = 0;
        private static float _dialogueVisibleChars = 0f;
        private static int _dialogueLastBlipChar = 0;
        private static int _dialogueVisitIndex = 0;
        private static int _dialogueTotalConvs = 1;
        private static readonly Dictionary<string, int> _dialoguePairVisitCounts = new();

        public static void CloseSafeTravelsDialogueModal()
        {
            if (_dialogueNpcEntity is SafeTravelsNpc prevNpc && IsValid(prevNpc))
                prevNpc.InConversation = false;
            _dialogueOpen = false;
            _dialogueNpcEntity = null;

            if (_run != null && string.IsNullOrEmpty(_run.Parts.ActiveEmote) && _cutsceneTimer <= 0f)
            {
                bool isFp = SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey) && _run.Combat.MedicTauntTimer <= 0f;
                _run.Parts.Root.Enabled = !isFp;
                if (_run.Combat.FpRoot != null && !_run.Combat.FpRoot.IsDestroyed)
                    _run.Combat.FpRoot.Enabled = isFp;
            }
            RestoreInGameCursorIfNeeded();
        }

        public static void OpenSafeTravelsDialogue(string npcCharKey, int npcSkinIdx, Entity? npcEntity)
        {
            if (_run == null || _cutsceneTimer > 0f) return;
            if (_dialogueNpcEntity is SafeTravelsNpc prevNpc && IsValid(prevNpc))
                prevNpc.InConversation = false;

            int mySkin = ProgressData.Instance.GetSelectedSkin(_run.CharKey);
            string myPersona = SafeTravelsDialogues.GetPersonaKey(_run.CharKey, mySkin);
            string npcPersona = SafeTravelsDialogues.GetPersonaKey(npcCharKey, npcSkinIdx);
            string pairKey = $"{myPersona}->{npcPersona}";

            int visit = _dialoguePairVisitCounts.TryGetValue(pairKey, out int v) ? v : 0;
            _dialoguePairVisitCounts[pairKey] = visit + 1;

            _dialogueNpcCharKey = npcCharKey;
            _dialogueNpcSkinIdx = npcSkinIdx;
            _dialogueNpcEntity = npcEntity;
            _dialogueVisitIndex = visit;
            _dialogueTotalConvs = Math.Max(1, SafeTravelsDialogues.GetConversationCount(_run.CharKey, mySkin, npcCharKey, npcSkinIdx));
            _dialogueLines = SafeTravelsDialogues.GetConversation(_run.CharKey, mySkin, npcCharKey, npcSkinIdx, visit);
            _dialogueLineIdx = 0;
            _dialogueVisibleChars = 0f;
            _dialogueLastBlipChar = 0;
            _dialogueOpen = _dialogueLines.Count > 0;
            _emoteWheelOpen = false;
            _safeTravelsWardrobeOpen = false;
            SetLocalPlayerEmote("");

            if (_dialogueOpen)
            {
                if (npcEntity is SafeTravelsNpc curNpc && IsValid(curNpc))
                    curNpc.InConversation = true;

                // Показываем 3D-модель даже у персонажей от 1-го лица для кинематографичной камеры диалога
                _run.Parts.Root.Enabled = true;
                if (_run.Combat.FpRoot != null && !_run.Combat.FpRoot.IsDestroyed)
                    _run.Combat.FpRoot.Enabled = false;

                Raylib.EnableCursor();
                SoundManager.Play("interact", 0.85f);
                if (IsValid(npcEntity))
                    _run.PlayerEntity.LookAt2D(npcEntity!.Position);
            }
        }

        private static void AdvanceOrFinishDialogueLine()
        {
            if (!_dialogueOpen || _dialogueLines.Count == 0) return;
            string fullText = _dialogueLines[_dialogueLineIdx].Text ?? "";
            if (_dialogueVisibleChars < fullText.Length)
            {
                _dialogueVisibleChars = fullText.Length;
                SoundManager.Play("ui_click", 0.6f);
                return;
            }

            _dialogueLineIdx++;
            if (_dialogueLineIdx >= _dialogueLines.Count)
            {
                SoundManager.Play("ui_click", 0.75f);
                CloseSafeTravelsDialogueModal();
            }
            else
            {
                _dialogueVisibleChars = 0f;
                _dialogueLastBlipChar = 0;
                SoundManager.Play("dialogue_blip", 0.85f);
            }
        }

        private static void UpdateSafeTravelsDialogue(float dt)
        {
            if (!_dialogueOpen || _dialogueLines.Count == 0) return;
            if (Raylib.IsCursorHidden()) Raylib.EnableCursor();

            string fullText = _dialogueLines[_dialogueLineIdx].Text ?? "";
            if (_dialogueVisibleChars < fullText.Length)
            {
                _dialogueVisibleChars = MathF.Min(fullText.Length, _dialogueVisibleChars + dt * 58f);
                int curCh = (int)_dialogueVisibleChars;
                if (curCh - _dialogueLastBlipChar >= 3)
                {
                    _dialogueLastBlipChar = curCh;
                    SoundManager.Play("dialogue_blip", 0.65f);
                }
            }

            // Мягкая диалоговая камера сбоку, показывающая и игрока, и собеседника
            if (_run != null && IsValid(_dialogueNpcEntity))
            {
                Vector3 npcPos = _dialogueNpcEntity!.Position;
                Vector3 mid = (_run.PlayerPos + npcPos) * 0.5f + new Vector3(0f, 1.55f, 0f);
                Vector3 between = npcPos - _run.PlayerPos;
                between.Y = 0f;
                if (between.LengthSquared() < 0.05f) between = new Vector3(0, 0, 1);
                else between = Vector3.Normalize(between);
                Vector3 side = new(between.Z, 0f, -between.X);
                Vector3 desiredCam = mid + side * 3.8f - between * 1.4f + new Vector3(0f, 0.75f, 0f);
                Vector3 resolvedCam = Physics.ResolveCameraCollision(mid, mid, desiredCam, 0.38f);
                Engine.Camera.Position = Lerp(Engine.Camera.Position, new Vector3(-resolvedCam.X, resolvedCam.Y, resolvedCam.Z), dt * 7.5f);
                Engine.Camera.Target = Lerp(Engine.Camera.Target, new Vector3(-mid.X, mid.Y, mid.Z), dt * 9.0f);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.R))
            {
                string oppKey = _dialogueNpcCharKey;
                int oppSkin = _dialogueNpcSkinIdx;
                Entity? oppEnt = _dialogueNpcEntity;
                CloseSafeTravelsDialogueModal();
                StartRpsMiniGame(oppKey, oppSkin, oppEnt, null);
                return;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter))
                AdvanceOrFinishDialogueLine();
        }

        private static void DrawSafeTravelsDialogueModal(int sw, int sh)
        {
            if (!_dialogueOpen || _run == null || _dialogueLines.Count == 0) return;

            var line = _dialogueLines[Math.Clamp(_dialogueLineIdx, 0, _dialogueLines.Count - 1)];
            int mySkin = ProgressData.Instance.GetSelectedSkin(_run.CharKey);
            string myTitle = SafeTravelsDialogues.GetPersonaDisplayName(SafeTravelsDialogues.GetPersonaKey(_run.CharKey, mySkin));
            string npcTitle = SafeTravelsDialogues.GetPersonaDisplayName(SafeTravelsDialogues.GetPersonaKey(_dialogueNpcCharKey, _dialogueNpcSkinIdx));

            var (spkBase, spkSkin) = SafeTravelsDialogues.SplitPersonaKey(line.SpeakerKey);
            string speakerName = !string.IsNullOrWhiteSpace(line.SpeakerTitle)
                ? line.SpeakerTitle
                : SafeTravelsDialogues.GetPersonaDisplayName(line.SpeakerKey);
            Color speakerColor = Color.SkyBlue;
            if (SurvivorDatabase.Classes.TryGetValue(spkBase, out var spkCfg))
                speakerColor = spkSkin > 0 && spkSkin < spkCfg.Skins.Count ? spkCfg.Skins[spkSkin].PrimaryColor : spkCfg.BodyColor;

            int dw = Math.Min(1080, sw - 80);
            int dh = 258;
            int dx = (sw - dw) / 2;
            int dy = sh - dh - 28;

            Raylib.DrawRectangle(dx - 4, dy - 4, dw + 8, dh + 8, Rgb(6, 10, 20, 215));
            Raylib.DrawRectangle(dx, dy, dw, dh, Rgb(12, 18, 32, 248));
            Raylib.DrawRectangle(dx, dy, dw, 4, speakerColor);
            Raylib.DrawRectangleLines(dx, dy, dw, dh, Rgb(speakerColor.R, speakerColor.G, speakerColor.B, 175));

            int convDisplayNum = (_dialogueVisitIndex % _dialogueTotalConvs) + 1;
            Engine.DrawTextLeft(
                $"КАНАЛ ЭКИПАЖА :: {myTitle}  <->  {npcTitle}   [Тема {convDisplayNum} из {_dialogueTotalConvs} · Реплика {_dialogueLineIdx + 1}/{_dialogueLines.Count}]",
                dx + 22, dy + 12, 14f, Rgb(145, 205, 245));

            // Плашка говорящего
            Raylib.DrawRectangle(dx + 22, dy + 38, dw - 44, 34, Rgb(20, 28, 48, 240));
            Raylib.DrawRectangle(dx + 22, dy + 38, 5, 34, speakerColor);
            Engine.DrawTextLeft(speakerName, dx + 36, dy + 46, 19f, speakerColor);

            string full = line.Text ?? "";
            int showLen = Math.Clamp((int)_dialogueVisibleChars, 0, full.Length);
            string shown = full[..showLen];
            Engine.DrawTextWrapped(shown, dx + 26, dy + 84, dw - 52, 17f, Color.White, 5f);

            // Нижние кнопки управления диалогом
            int btnY = dy + dh - 48;
            string nextLabel = showLen < full.Length
                ? "ПОКАЗАТЬ СРАЗУ [ПРОБЕЛ]"
                : (_dialogueLineIdx + 1 < _dialogueLines.Count ? "ДАЛЕЕ -> [ПРОБЕЛ]" : "ЗАВЕРШИТЬ [ПРОБЕЛ]");

            if (Engine.DrawButton(new Rectangle(dx + 22, btnY, 248, 36), nextLabel,
                Rgb(36, 98, 68), Rgb(54, 142, 94), Color.White, 14f, Color.Lime))
            {
                AdvanceOrFinishDialogueLine();
            }

            if (Engine.DrawButton(new Rectangle(dx + 282, btnY, 244, 36), "СЛЕДУЮЩАЯ ТЕМА →",
                Rgb(38, 68, 122), Rgb(58, 98, 172), Color.Yellow, 14f, Color.SkyBlue))
            {
                OpenSafeTravelsDialogue(_dialogueNpcCharKey, _dialogueNpcSkinIdx, _dialogueNpcEntity);
            }

            if (Engine.DrawButton(new Rectangle(dx + 538, btnY, 290, 36), "СЫГРАТЬ В К-Н-Б [R]",
                Rgb(92, 62, 28), Rgb(136, 92, 42), Color.Gold, 14f, Color.Gold))
            {
                string oppKey = _dialogueNpcCharKey;
                int oppSkin = _dialogueNpcSkinIdx;
                Entity? oppEnt = _dialogueNpcEntity;
                CloseSafeTravelsDialogueModal();
                StartRpsMiniGame(oppKey, oppSkin, oppEnt, null);
                return;
            }

            if (Engine.DrawButton(new Rectangle(dx + dw - 210, btnY, 188, 36), "ЗАКРЫТЬ [ESC]",
                Rgb(105, 36, 46), Rgb(148, 52, 66), Color.White, 14f, Color.Red))
            {
                CloseSafeTravelsDialogueModal();
            }
        }

        // =========================================================================================
        // 4. МИНИ-ИГРА «КАМЕНЬ-НОЖНИЦЫ-БУМАГА» (С ВЫЖИВШИМИ НА КОРАБЛЕ И В КООПЕРАТИВЕ)
        // =========================================================================================
        private static bool _rpsOpen = false;
        private static string _rpsPhase = "choose"; // "choose", "countdown", "result"
        private static float _rpsTimer = 0f;
        private static int _rpsLastTick = 0;
        private static int _rpsMyChoice = 0;  // 0=Камень, 1=Ножницы, 2=Бумага
        private static int _rpsOppChoice = 0;
        private static int _rpsOutcome = 0;   // 1=Победа, -1=Поражение, 0=Ничья
        private static string _rpsOppCharKey = "commando";
        private static int _rpsOppSkinIdx = 0;
        private static Entity? _rpsOppEntity = null;
        private static string? _rpsRemotePlayerId = null;
        private static string _rpsMyReaction = "";
        private static string _rpsOppReaction = "";

        private static string GetRpsMoveName(int move) => move switch
        {
            0 => "КАМЕНЬ",
            1 => "НОЖНИЦЫ",
            _ => "БУМАГА"
        };

        private static void ApplyNpcOrRemoteEmoteForRps(string emote)
        {
            if (_rpsOppEntity is SafeTravelsNpc npc && IsValid(npc))
                npc.SetNpcEmote(emote);
            else if (_rpsOppEntity is RemotePlayer rp && IsValid(rp))
                rp.Parts.SetEmote(emote);
        }

        public static void StartRpsMiniGame(string oppCharKey, int oppSkinIdx, Entity? oppEntity, string? remotePlayerId)
        {
            if (_run == null) return;
            CloseSafeTravelsDialogueModal();
            _emoteWheelOpen = false;
            _rpsOpen = true;
            _rpsPhase = "choose";
            _rpsTimer = 0f;
            _rpsOppCharKey = oppCharKey;
            _rpsOppSkinIdx = oppSkinIdx;
            _rpsOppEntity = oppEntity;
            _rpsRemotePlayerId = remotePlayerId;
            _rpsMyReaction = "";
            _rpsOppReaction = "";

            SetLocalPlayerEmote("rps_wait");
            ApplyNpcOrRemoteEmoteForRps("rps_wait");
            if (IsValid(oppEntity))
                _run.PlayerEntity.LookAt2D(oppEntity!.Position);

            Raylib.EnableCursor();
            SoundManager.Play("ui_ready", 0.85f);
        }

        private static void LockInRpsChoice(int playerChoice)
        {
            if (_run == null) return;
            _rpsMyChoice = Math.Clamp(playerChoice, 0, 2);
            _rpsOppChoice = Random.Shared.Next(0, 3);
            _rpsPhase = "countdown";
            _rpsTimer = 1.50f;
            _rpsLastTick = 4;

            SetLocalPlayerEmote("rps_count");
            ApplyNpcOrRemoteEmoteForRps("rps_count");
            SoundManager.Play("rps_tick", 0.9f);

            if (!string.IsNullOrEmpty(_rpsRemotePlayerId) && _net.Connected)
            {
                _net.Broadcast(new JsonObject
                {
                    ["type"] = "rps_duel",
                    ["from_id"] = _net.MyId,
                    ["target_id"] = _rpsRemotePlayerId,
                    ["host_move"] = _rpsMyChoice,
                    ["guest_move"] = _rpsOppChoice
                });
            }
        }

        public static void HandleNetworkRpsDuel(JsonObject msg)
        {
            if (_run == null) return;
            string targetId = msg["target_id"]?.GetValue<string>() ?? "";
            string fromId = msg["from_id"]?.GetValue<string>() ?? "";
            if (targetId != _net.MyId) return;

            int theirMove = msg["host_move"]?.GetValue<int>() ?? 0;
            int myAssignedMove = msg["guest_move"]?.GetValue<int>() ?? 0;
            if (_remotePlayers.TryGetValue(fromId, out var rp) && IsValid(rp))
            {
                _rpsOpen = true;
                _rpsOppCharKey = rp.CharKey;
                _rpsOppSkinIdx = rp.SkinIdx;
                _rpsOppEntity = rp;
                _rpsRemotePlayerId = fromId;
                _rpsMyChoice = myAssignedMove;
                _rpsOppChoice = theirMove;
                _rpsPhase = "countdown";
                _rpsTimer = 1.50f;
                _rpsLastTick = 4;
                SetLocalPlayerEmote("rps_count");
                rp.Parts.SetEmote("rps_count");
                Raylib.EnableCursor();
                SoundManager.Play("rps_tick", 0.9f);
            }
        }

        private static void ResolveRpsOutcome()
        {
            if (_run == null) return;
            if (_rpsMyChoice == _rpsOppChoice) _rpsOutcome = 0;
            else if ((_rpsMyChoice == 0 && _rpsOppChoice == 1) ||
                     (_rpsMyChoice == 1 && _rpsOppChoice == 2) ||
                     (_rpsMyChoice == 2 && _rpsOppChoice == 0))
                _rpsOutcome = 1;
            else
                _rpsOutcome = -1;

            _rpsPhase = "result";
            _rpsTimer = 4.2f;

            string myEm = _rpsOutcome > 0 ? "rps_win" : (_rpsOutcome < 0 ? "rps_lose" : "rps_draw");
            string oppEm = _rpsOutcome > 0 ? "rps_lose" : (_rpsOutcome < 0 ? "rps_win" : "rps_draw");
            SetLocalPlayerEmote(myEm);
            ApplyNpcOrRemoteEmoteForRps(oppEm);

            SoundManager.Play(_rpsOutcome > 0 ? "rps_win" : (_rpsOutcome < 0 ? "rps_lose" : "rps_draw"));

            int mySkin = ProgressData.Instance.GetSelectedSkin(_run.CharKey);
            _rpsMyReaction = SafeTravelsDialogues.GetRpsReaction(_run.CharKey, npcWon: _rpsOutcome > 0, isDraw: _rpsOutcome == 0, npcSkinIdx: mySkin);
            _rpsOppReaction = SafeTravelsDialogues.GetRpsReaction(_rpsOppCharKey, npcWon: _rpsOutcome < 0, isDraw: _rpsOutcome == 0, npcSkinIdx: _rpsOppSkinIdx);
        }

        private static void CloseRpsMiniGame()
        {
            _rpsOpen = false;
            SetLocalPlayerEmote("");
            ApplyNpcOrRemoteEmoteForRps("");
            RestoreInGameCursorIfNeeded();
        }

        private static void UpdateRpsMiniGame(float dt)
        {
            if (!_rpsOpen || _run == null) return;
            if (Raylib.IsCursorHidden()) Raylib.EnableCursor();

            if (IsValid(_rpsOppEntity))
            {
                Vector3 oppPos = _rpsOppEntity!.Position;
                Vector3 mid = (_run.PlayerPos + oppPos) * 0.5f + new Vector3(0f, 1.55f, 0f);
                Vector3 between = oppPos - _run.PlayerPos;
                between.Y = 0f;
                if (between.LengthSquared() < 0.05f) between = new Vector3(0, 0, 1);
                else between = Vector3.Normalize(between);
                Vector3 side = new(between.Z, 0f, -between.X);
                Vector3 desiredCam = mid + side * 3.85f - between * 1.15f + new Vector3(0f, 0.85f, 0f);
                Vector3 resolvedCam = Physics.ResolveCameraCollision(mid, mid, desiredCam, 0.38f);
                Engine.Camera.Position = Lerp(Engine.Camera.Position, new Vector3(-resolvedCam.X, resolvedCam.Y, resolvedCam.Z), dt * 7.5f);
                Engine.Camera.Target = Lerp(Engine.Camera.Target, new Vector3(-mid.X, mid.Y, mid.Z), dt * 9.0f);
            }

            if (_rpsPhase == "choose")
            {
                if (Raylib.IsKeyPressed(KeyboardKey.One)) LockInRpsChoice(0);
                else if (Raylib.IsKeyPressed(KeyboardKey.Two)) LockInRpsChoice(1);
                else if (Raylib.IsKeyPressed(KeyboardKey.Three)) LockInRpsChoice(2);
            }
            else if (_rpsPhase == "countdown")
            {
                _rpsTimer -= dt;
                int step = (int)MathF.Ceiling(_rpsTimer / 0.5f);
                if (step < _rpsLastTick && step >= 1)
                {
                    _rpsLastTick = step;
                    SoundManager.Play("rps_tick", 0.95f);
                }
                if (_rpsTimer <= 0f)
                    ResolveRpsOutcome();
            }
        }

        private static void DrawRpsMiniGameModal(int sw, int sh)
        {
            if (!_rpsOpen || _run == null) return;

            string oppTitle = SafeTravelsDialogues.GetPersonaDisplayName(SafeTravelsDialogues.GetPersonaKey(_rpsOppCharKey, _rpsOppSkinIdx));
            string myTitle = SafeTravelsDialogues.GetPersonaDisplayName(SafeTravelsDialogues.GetPersonaKey(_run.CharKey, ProgressData.Instance.GetSelectedSkin(_run.CharKey)));

            int mw = 760, mh = _rpsPhase == "result" ? 310 : 220;
            int mx = (sw - mw) / 2, my = sh - mh - 42;

            Color accent = _rpsPhase == "result"
                ? (_rpsOutcome > 0 ? Color.Lime : (_rpsOutcome < 0 ? Color.Red : Color.Gold))
                : Color.Gold;

            Raylib.DrawRectangle(mx, my, mw, mh, Rgb(12, 18, 32, 248));
            Raylib.DrawRectangle(mx, my, mw, 4, accent);
            Raylib.DrawRectangleLines(mx, my, mw, mh, Rgb(accent.R, accent.G, accent.B, 175));

            Engine.DrawTextCentered($"ДУЭЛЬ «КАМЕНЬ-НОЖНИЦЫ-БУМАГА» :: {myTitle}  VS  {oppTitle}", sw * 0.5f, my + 22f, 17f, Color.Gold);

            if (_rpsPhase == "choose")
            {
                Engine.DrawTextCentered("Выберите ваш ход кнопкой или клавишами [1] / [2] / [3]:", sw * 0.5f, my + 56f, 15f, Color.White);
                if (Engine.DrawButton(new Rectangle(mx + 36, my + 86, 210, 62), "[1] КАМЕНЬ", Rgb(36, 56, 88), Rgb(56, 88, 138), Color.White, 17f, Color.SkyBlue))
                    LockInRpsChoice(0);
                if (Engine.DrawButton(new Rectangle(mx + 274, my + 86, 210, 62), "[2] НОЖНИЦЫ", Rgb(88, 42, 62), Rgb(132, 64, 92), Color.White, 17f, Color.Orange))
                    LockInRpsChoice(1);
                if (Engine.DrawButton(new Rectangle(mx + 512, my + 86, 210, 62), "[3] БУМАГА", Rgb(36, 84, 62), Rgb(56, 128, 92), Color.White, 17f, Color.Lime))
                    LockInRpsChoice(2);

                if (Engine.DrawButton(new Rectangle(mx + mw - 180, my + mh - 46, 156, 34), "ОТМЕНА [ESC]", Rgb(95, 36, 44), Rgb(138, 52, 62), Color.White, 13f))
                    CloseRpsMiniGame();
            }
            else if (_rpsPhase == "countdown")
            {
                string countWord = _rpsTimer > 1.0f ? "КА-МЕНЬ..." : (_rpsTimer > 0.5f ? "НОЖ-НИ-ЦЫ..." : "БУ-МА-ГА — РАЗ-ДВА-ТРИ!");
                Engine.DrawTextCentered(countWord, sw * 0.5f, my + 112f, 30f, Color.Yellow);
            }
            else
            {
                string resHeadline = _rpsOutcome > 0 ? "★ ВЫ ПОБЕДИЛИ! ★" : (_rpsOutcome < 0 ? "СОПЕРНИК ПОБЕДИЛ!" : "НИЧЬЯ!");
                Engine.DrawTextCentered(
                    $"{resHeadline}   ({myTitle}: {GetRpsMoveName(_rpsMyChoice)}   vs   {oppTitle}: {GetRpsMoveName(_rpsOppChoice)})",
                    sw * 0.5f, my + 56f, 19f, accent);

                Raylib.DrawRectangle(mx + 22, my + 84, mw - 44, 72, Rgb(18, 26, 44, 238));
                Raylib.DrawRectangle(mx + 22, my + 84, 4, 72, Color.SkyBlue);
                Engine.DrawTextLeft($"{oppTitle}:", mx + 34, my + 90, 14f, Color.SkyBlue);
                Engine.DrawTextWrapped($"«{_rpsOppReaction}»", mx + 34, my + 110, mw - 68, 15f, Color.White, 3f);

                Raylib.DrawRectangle(mx + 22, my + 166, mw - 44, 72, Rgb(18, 26, 44, 238));
                Raylib.DrawRectangle(mx + 22, my + 166, 4, 72, Color.Gold);
                Engine.DrawTextLeft($"{myTitle}:", mx + 34, my + 172, 14f, Color.Gold);
                Engine.DrawTextWrapped($"«{_rpsMyReaction}»", mx + 34, my + 192, mw - 68, 15f, Color.White, 3f);

                if (Engine.DrawButton(new Rectangle(mx + 22, my + mh - 52, 240, 38), "СЫГРАТЬ ЕЩЁ РАЗ", Rgb(36, 98, 68), Rgb(54, 142, 94), Color.White, 14f, Color.Lime))
                {
                    _rpsPhase = "choose";
                    SetLocalPlayerEmote("rps_wait");
                    ApplyNpcOrRemoteEmoteForRps("rps_wait");
                }
                if (Engine.DrawButton(new Rectangle(mx + mw - 202, my + mh - 52, 180, 38), "ЗАКРЫТЬ [ESC]", Rgb(102, 38, 48), Rgb(145, 55, 68), Color.White, 14f, Color.Red))
                {
                    CloseRpsMiniGame();
                }
            }
        }

        // =========================================================================================
        // 5. ТРЁХМЕРНАЯ КАТСЦЕНА РАЗБЛОКИРОВКИ НОВОГО ВЫЖИВШЕГО
        // =========================================================================================
        private static float _cutsceneTimer = 0f;
        private static float _cutsceneDuration = 4.8f;
        private static string _cutsceneCharKey = "";
        private static Entity? _cutsceneStageRoot = null;
        private static Entity? _cutsceneHeroRoot = null;
        private static Entity? _cutsceneRing = null;
        private static SurvivorParts? _cutsceneHeroParts = null;
        private static bool _cutsceneUltTriggered = false;
        // Короткая неуязвимость после катсцены, чтобы стоящие рядом враги не добили игрока в первую же секунду
        private static float _postCutsceneGraceTimer = 0f;
        private const float PostCutsceneGraceDuration = 2.5f;

        /// <summary>Идёт ли сейчас катсцена (мир на паузе, ввод заблокирован).</summary>
        public static bool IsCutscenePlaying => _cutsceneTimer > 0f;

        /// <summary>Игрок неуязвим: во время катсцены и ещё 2.5 секунды после неё.</summary>
        public static bool IsCutsceneInvulnerable => _cutsceneTimer > 0f || _postCutsceneGraceTimer > 0f;

        private static void TickPostCutsceneGrace(float dt)
        {
            if (_postCutsceneGraceTimer > 0f)
                _postCutsceneGraceTimer = MathF.Max(0f, _postCutsceneGraceTimer - dt);
        }

        public static void StartCharacterUnlockCutscene(string unlockedCharKey)
        {
            if (!SurvivorDatabase.Classes.ContainsKey(unlockedCharKey)) return;
            if (_cutsceneTimer > 0f && _cutsceneCharKey == unlockedCharKey && (_cutsceneDuration - _cutsceneTimer) < 0.35f)
                return;
            StopCharacterUnlockCutscene();

            _cutsceneCharKey = unlockedCharKey;
            _cutsceneDuration = 4.8f;
            _cutsceneTimer = _cutsceneDuration;
            _cutsceneUltTriggered = false;

            var cfg = SurvivorDatabase.Classes[unlockedCharKey];
            Vector3 anchor = (_run != null ? _run.PlayerPos : Vector3.Zero) + new Vector3(0f, 88f, 0f);

            _cutsceneStageRoot = new Entity(position: anchor);
            // Космический купол и пьедестал пробуждения героя
            new Entity(parent: _cutsceneStageRoot, model: "sphere", scale: new(-42f, 42f, 42f), color: Rgb(10, 14, 28), unlit: true);
            new Entity(parent: _cutsceneStageRoot, model: "cube", scale: new(5.4f, 0.45f, 5.4f), position: new(0, -0.24f, 0), color: Rgb(30, 38, 56));
            new Entity(parent: _cutsceneStageRoot, model: "cube", scale: new(4.2f, 0.16f, 4.2f), position: new(0, 0.04f, 0), color: Rgb(54, 68, 96));
            _cutsceneRing = new Entity(parent: _cutsceneStageRoot, model: "diamond", scale: new(4.8f, 0.12f, 4.8f), position: new(0, 0.16f, 0), color: cfg.BodyColor, unlit: true);
            new Entity(parent: _cutsceneStageRoot, model: "cube", scale: new(0.85f, 26f, 0.85f), position: new(0, 12f, 0), color: Rgb(cfg.BodyColor.R, cfg.BodyColor.G, cfg.BodyColor.B, 65), unlit: true);

            for (int i = 0; i < 6; i++)
            {
                float a = i * (MathF.PI * 2f / 6f);
                new Entity(
                    parent: _cutsceneStageRoot, model: "diamond",
                    scale: new(0.55f, 2.2f, 0.55f),
                    position: new(MathF.Cos(a) * 3.8f, 1.4f, MathF.Sin(a) * 3.8f),
                    color: i % 2 == 0 ? cfg.BodyColor : Color.Gold, unlit: true);
            }

            _cutsceneHeroRoot = new Entity(parent: _cutsceneStageRoot, position: new(0, unlockedCharKey == "mindustry" ? 1.05f : 0.14f, 0));
            _cutsceneHeroParts = SurvivorDatabase.BuildSurvivorMesh(_cutsceneHeroRoot, unlockedCharKey, 0);
            _cutsceneHeroParts.TriggerSkillAnim("m2", 0.9f);

            SoundManager.Play("cutscene_whoosh");
            SoundManager.Play("victory");
        }

        private static void StopCharacterUnlockCutscene()
        {
            bool wasPlaying = _cutsceneTimer > 0f || IsValid(_cutsceneStageRoot);
            _cutsceneTimer = 0f;
            if (wasPlaying && _run != null)
                _postCutsceneGraceTimer = PostCutsceneGraceDuration;
            if (IsValid(_cutsceneStageRoot))
                Destroy(_cutsceneStageRoot!);
            _cutsceneStageRoot = null;
            _cutsceneHeroRoot = null;
            _cutsceneRing = null;
            _cutsceneHeroParts = null;
            if (_run != null && _run.Combat != null)
                _run.Combat.Shield = MathF.Max(_run.Combat.Shield, _run.PlayerMaxHp * 0.35f);
            RestoreInGameCursorIfNeeded();
        }

        private static bool UpdateCharacterUnlockCutscene(float dt)
        {
            if (_cutsceneTimer <= 0f || !IsValid(_cutsceneStageRoot) || !IsValid(_cutsceneHeroRoot))
                return false;

            _cutsceneTimer -= dt;
            if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                StopCharacterUnlockCutscene();
                return false;
            }

            float elapsed = _cutsceneDuration - _cutsceneTimer;
            float progress = Math.Clamp(elapsed / _cutsceneDuration, 0f, 1f);

            if (!_cutsceneUltTriggered && elapsed >= 1.55f)
            {
                _cutsceneUltTriggered = true;
                _cutsceneHeroParts?.SetUltForm(true);
                _cutsceneHeroParts?.TriggerSkillAnim("r", 1.15f);
                SoundManager.Play("item_legendary", 0.85f);
            }

            if (_cutsceneRing != null && !_cutsceneRing.IsDestroyed)
                _cutsceneRing.RotationY += 95f * dt;

            _cutsceneHeroParts?.UpdateAnimations(_cutsceneCharKey, dt, false, true, 0f, 1f, 0f, 0f);

            // Кинематографичный облёт камеры вокруг пробудившегося выжившего
            Vector3 stageCenter = _cutsceneStageRoot!.Position;
            float orbitAng = (-38f + progress * 86f) * (MathF.PI / 180f);
            float dist = Lerp(4.2f, 6.2f, progress);
            float camY = stageCenter.Y + Lerp(1.45f, 2.35f, progress);
            Vector3 camUrsina = new(
                stageCenter.X + MathF.Sin(orbitAng) * dist,
                camY,
                stageCenter.Z - MathF.Cos(orbitAng) * dist);
            Vector3 targetUrsina = stageCenter + new Vector3(0f, 1.42f, 0f);

            Engine.Camera.FovY = 52f;
            Engine.Camera.Position = new Vector3(-camUrsina.X, camUrsina.Y, camUrsina.Z);
            Engine.Camera.Target = new Vector3(-targetUrsina.X, targetUrsina.Y, targetUrsina.Z);

            if (_cutsceneTimer <= 0f)
            {
                StopCharacterUnlockCutscene();
                return false;
            }
            return true;
        }

        private static void DrawCharacterUnlockCutsceneOverlay(int sw, int sh)
        {
            if (_cutsceneTimer <= 0f || !SurvivorDatabase.Classes.TryGetValue(_cutsceneCharKey, out var cfg))
                return;

            int barH = 96;
            Raylib.DrawRectangle(0, 0, sw, barH, Rgb(4, 6, 12, 238));
            Raylib.DrawRectangle(0, barH - 3, sw, 3, Color.Gold);
            Raylib.DrawRectangle(0, sh - barH, sw, barH, Rgb(4, 6, 12, 238));
            Raylib.DrawRectangle(0, sh - barH, sw, 3, cfg.BodyColor);

            Engine.DrawTextCentered("★ ИСПЫТАНИЕ ПРОЙДЕНО :: НОВЫЙ ВЫЖИВШИЙ РАЗБЛОКИРОВАН ★", sw * 0.5f, 34f, 20f, Color.Gold);
            Engine.DrawTextCentered($"{cfg.Name}  —  {cfg.Subtitle}", sw * 0.5f, 66f, 24f, cfg.BodyColor);

            var lore = LoreDatabase.GetLore("survivors", _cutsceneCharKey);
            if (!string.IsNullOrEmpty(lore.Quote))
                Engine.DrawTextCentered(lore.Quote, sw * 0.5f, sh - 74f, 14f, Rgb(220, 232, 248));
            Engine.DrawTextCentered($"Пассивная особенность: {cfg.PassiveName} — {cfg.PassiveDesc}", sw * 0.5f, sh - 50f, 14f, Color.White);
            Engine.DrawTextCentered("[ПРОБЕЛ / ENTER — ПРОПУСТИТЬ КАТСЦЕНУ]", sw * 0.5f, sh - 22f, 13f, Rgb(165, 195, 225));
        }

        // =========================================================================================
        // СПРАВКА ПО УСЛОВИЯМ ОТКРЫТИЯ И НАГРАДАМ (ЛОББИ, SAFE TRAVELS И ЖУРНАЛ)
        // =========================================================================================
        public static (string HowTo, string Reward) GetCharacterUnlockInfo(string charKey)
        {
            if (charKey == "sanabi")
            {
                return (
                    "Авторизуйтесь в главном меню под секретным кодовым позывным Бригадного Генерала (или посетите UES «Safe Travels»).",
                    "Секретный выживший «Бригадный Генерал (SANABI)»"
                );
            }

            string achKey = $"ach_char_{charKey}";
            if (GameData.Achievements.TryGetValue(achKey, out var ach) && ach.UnlocksCharacter == charKey)
            {
                string reward = !string.IsNullOrEmpty(ach.RewardText)
                    ? ach.RewardText
                    : (SurvivorDatabase.Classes.TryGetValue(charKey, out var sc) ? $"Выживший «{sc.Name}»" : "Новый выживший");
                return (ach.Desc, reward);
            }

            foreach (var kv in GameData.Achievements)
            {
                if (kv.Value.UnlocksCharacter == charKey)
                    return (kv.Value.Desc, !string.IsNullOrEmpty(kv.Value.RewardText) ? kv.Value.RewardText : kv.Value.Name);
            }

            return ("Доступен со старта экспедиции.", "Базовый выживший UES");
        }

        public static (string HowTo, string Reward) GetItemUnlockInfo(string itemKey)
        {
            foreach (var kv in GameData.Achievements)
            {
                if (kv.Value.UnlocksItem == itemKey)
                {
                    string rew = !string.IsNullOrEmpty(kv.Value.RewardText) ? kv.Value.RewardText : $"Предмет «{GameData.ItemPool[itemKey].Name}»";
                    return ($"Достижение «{kv.Value.Name}»: {kv.Value.Desc}", rew);
                }
            }
            if (GameData.ItemPool.TryGetValue(itemKey, out var inf))
                return ("Открыт в общем пуле снабжения по умолчанию (сундуки, алтари и боссы).", $"Предмет редкости «{inf.TierName}»: {inf.Name}");
            return ("Находится в ходе экспедиции.", "Артефакт забега");
        }

        public static string GetSkinUnlockInfo(SurvivorConfig cfg, int skinIdx)
        {
            if (skinIdx <= 0 || skinIdx >= cfg.Skins.Count) return "Открыт по умолчанию";
            var sk = cfg.Skins[skinIdx];
            if (!string.IsNullOrEmpty(sk.UnlockAchKey) && GameData.Achievements.TryGetValue(sk.UnlockAchKey, out var ach))
                return $"Играя за «{cfg.Name}», выполните «{ach.Name}»: {ach.Desc}";
            return !string.IsNullOrEmpty(sk.UnlockReqText) ? sk.UnlockReqText : "Открывается испытанием персонажа";
        }

        public static string GetSkillUnlockInfo(SurvivorConfig cfg, SkillDef skDef)
        {
            if (!string.IsNullOrEmpty(skDef.UnlockAchKey) && GameData.Achievements.TryGetValue(skDef.UnlockAchKey, out var ach))
                return $"Играя за «{cfg.Name}», выполните «{ach.Name}»: {ach.Desc}";
            return !string.IsNullOrEmpty(skDef.UnlockReqText) ? skDef.UnlockReqText : "Открыт по умолчанию";
        }

        public static string GetAchievementRewardSummary(AchievementInfo ach, string? contextCharKey = null)
        {
            var rewards = new List<string>();
            if (!string.IsNullOrEmpty(ach.RewardText))
                rewards.Add(ach.RewardText);
            else
            {
                if (!string.IsNullOrEmpty(ach.UnlocksCharacter) && SurvivorDatabase.Classes.TryGetValue(ach.UnlocksCharacter, out var sc))
                    rewards.Add($"Персонаж «{sc.Name}»");
                if (!string.IsNullOrEmpty(ach.UnlocksItem) && GameData.ItemPool.TryGetValue(ach.UnlocksItem, out var it))
                    rewards.Add($"Предмет «{it.Name}»");
            }

            if (!string.IsNullOrEmpty(contextCharKey) && SurvivorDatabase.Classes.TryGetValue(contextCharKey, out var cCfg))
            {
                for (int si = 1; si < cCfg.Skins.Count; si++)
                {
                    if (cCfg.Skins[si].UnlockAchKey == ach.Key)
                        rewards.Add($"Облик «{cCfg.Skins[si].Name}» ({cCfg.Name})");
                }
                foreach (var kv in cCfg.SkillVariants)
                {
                    for (int vi = 1; vi < kv.Value.Count; vi++)
                        if (kv.Value[vi].UnlockAchKey == ach.Key)
                            rewards.Add($"Альт-навык «{kv.Value[vi].Name}» ({cCfg.Name})");
                }
            }

            return rewards.Count > 0 ? string.Join("  |  ", rewards) : "Памятный знак отличия в Журнале Выжившего";
        }

        // =========================================================================================
        // 6. СТРАНИЦА ЗАПИСИ ЖУРНАЛА (ДВОЙНОЙ КЛИК): ЛОР + ИНТЕРАКТИВНАЯ 3D МОДЕЛЬ СПРАВА
        // =========================================================================================
        private static bool _logDetailOpen = false;
        private static string _logDetailCategory = "items";
        private static string _logDetailKey = "syringe";
        private static int _logDetailPersonaSkinIdx = 0;
        private static float _logDetailScrollY = 0f;
        private static float _logDetailYaw = 24f;
        private static float _logDetailPitch = 12f;
        private static float _logDetailZoom = 1.0f;
        private static bool _logDetailAutoRotate = true;
        private static bool _logDetailDragging3D = false;

        private static double _logLastTileClickTime = -10.0;
        private static string _logLastTileKey = "";
        private static string _logLastTileCat = "";

        private static Entity? _logDetailPreviewRoot = null;
        private static Entity? _logDetailPreviewActor = null;
        private static Entity? _logDetailPreviewRing = null;
        private static SurvivorParts? _logDetailSurvParts = null;
        private static EnemyParts? _logDetailEnemyParts = null;
        private static float _logDetailActorBaseScale = 1.0f;
        private static float _logDetailTargetY = 1.25f;

        public static void RegisterLogbookTileClick(string category, string key)
        {
            double now = Raylib.GetTime();
            if (category == _logLastTileCat && key == _logLastTileKey && (now - _logLastTileClickTime) <= 0.45)
            {
                OpenLogbookDetailPage(category, key, 0);
                _logLastTileClickTime = -10.0;
                return;
            }
            _logLastTileCat = category;
            _logLastTileKey = key;
            _logLastTileClickTime = now;
        }

        public static void OpenLogbookDetailPage(string category, string key, int skinIdx = 0)
        {
            _logDetailCategory = category;
            _logDetailKey = key;
            _logDetailPersonaSkinIdx = Math.Max(0, skinIdx);
            _logDetailScrollY = 0f;
            _logDetailYaw = 24f;
            _logDetailPitch = 12f;
            _logDetailZoom = 1.0f;
            _logDetailDragging3D = false;
            _logDetailOpen = true;
            SoundManager.Play("ui_ready", 0.85f);
            RebuildLogbookDetail3DScene();
        }

        public static void CloseLogbookDetailPage()
        {
            _logDetailOpen = false;
            _logDetailDragging3D = false;
            if (IsValid(_logDetailPreviewRoot))
                Destroy(_logDetailPreviewRoot!);
            _logDetailPreviewRoot = null;
            _logDetailPreviewActor = null;
            _logDetailPreviewRing = null;
            _logDetailSurvParts = null;
            _logDetailEnemyParts = null;
            SoundManager.Play("ui_click", 0.8f);
        }

        private static void RebuildLogbookDetail3DScene()
        {
            if (IsValid(_logDetailPreviewRoot))
                Destroy(_logDetailPreviewRoot!);

            _logDetailSurvParts = null;
            _logDetailEnemyParts = null;

            _logDetailPreviewRoot = new Entity(position: Vector3.Zero)
            {
                IsPreviewIsolated = true
            };

            // Пьедестал изолированного 3D-просмотрщика Журнала
            new Entity(parent: _logDetailPreviewRoot, model: "cube", scale: new(3.1f, 0.28f, 3.1f), position: new(0, -0.16f, 0), color: Rgb(30, 38, 54));
            new Entity(parent: _logDetailPreviewRoot, model: "cube", scale: new(2.55f, 0.12f, 2.55f), position: new(0, 0.03f, 0), color: Rgb(50, 64, 90));
            _logDetailPreviewRing = new Entity(parent: _logDetailPreviewRoot, model: "cube", scale: new(2.68f, 0.05f, 2.68f), position: new(0, 0.09f, 0), color: Color.SkyBlue, unlit: true);
            _logDetailPreviewActor = new Entity(parent: _logDetailPreviewRoot, position: new(0, 0.12f, 0), rotation: new(0, _logDetailYaw, 0));

            string cat = _logDetailCategory;
            string key = _logDetailKey;
            _logDetailActorBaseScale = 1.0f;
            _logDetailTargetY = 1.25f;

            if (cat == "items" && GameData.ItemPool.TryGetValue(key, out var item))
            {
                _logDetailActorBaseScale = 1.95f;
                _logDetailTargetY = 1.35f;
                _logDetailPreviewActor.Position = new Vector3(0f, 1.35f, 0f);
                _logDetailPreviewRing.Color = item.Color;
                ItemBuilder.BuildItemMesh(_logDetailPreviewActor, key);
            }
            else if (cat == "survivors" && SurvivorDatabase.Classes.TryGetValue(key, out var surv))
            {
                int sIdx = Math.Clamp(_logDetailPersonaSkinIdx, 0, Math.Max(0, surv.Skins.Count - 1));
                _logDetailActorBaseScale = key == "mindustry" ? 1.15f : 1.02f;
                _logDetailTargetY = key == "cheburashka" ? 0.95f : 1.18f;
                _logDetailPreviewActor.Position = new Vector3(0f, key == "mindustry" ? 0.95f : 0.12f, 0f);
                _logDetailPreviewRing.Color = sIdx < surv.Skins.Count ? surv.Skins[sIdx].PrimaryColor : surv.BodyColor;
                _logDetailSurvParts = SurvivorDatabase.BuildSurvivorMesh(_logDetailPreviewActor, key, sIdx);
            }
            else if (cat == "monsters")
            {
                _logDetailActorBaseScale = key is "parent" or "elder_lemurian" ? 0.62f : (key is "golem" or "greater_wisp" or "lunar_chimera" or "bison" or "brass_contraption" ? 0.80f : 0.98f);
                float yOff = key is "wisp" or "jellyfish" or "greater_wisp" or "lunar_chimera" or "brass_contraption" or "hell_drone" or "nether_blaze" ? -0.65f : 0.12f;
                _logDetailTargetY = 1.18f;
                _logDetailPreviewActor.Position = new Vector3(0f, yOff, 0f);
                _logDetailPreviewRing.Color = Rgb(255, 140, 55);
                _logDetailEnemyParts = EnemyAndBossBuilder.BuildEnemyMesh(_logDetailPreviewActor, key);
            }
            else if (cat == "bosses")
            {
                _logDetailActorBaseScale = key == "magma_worm" ? 0.44f : 0.52f;
                float yOff = key is "vagrant" or "solus_control" ? -0.30f : (key == "magma_worm" ? 0.35f : 0.12f);
                _logDetailTargetY = 1.25f;
                _logDetailPreviewActor.Position = new Vector3(0f, yOff, 0f);
                _logDetailPreviewRing.Color = key == "mithrix" ? Color.SkyBlue : Color.Red;
                _logDetailEnemyParts = EnemyAndBossBuilder.BuildEnemyMesh(_logDetailPreviewActor, key);
                if (key == "mithrix" && _logDetailEnemyParts.PhaseRing != null)
                    _logDetailEnemyParts.PhaseRing.Enabled = true;
            }
            else if (cat == "stages")
            {
                var pal = GameData.StagePalettes.Find(p => p.Id == key)
                          ?? (GameData.SpecialPalettes.TryGetValue(key, out var sp) ? sp : GameData.StagePalettes[0]);
                _logDetailActorBaseScale = 0.25f;
                _logDetailTargetY = 1.05f;
                _logDetailPreviewActor.Position = new Vector3(0f, 0.45f, 0f);
                _logDetailPreviewRing.Color = pal.Accent;

                var root = new Entity(parent: _logDetailPreviewActor);
                new Entity(parent: root, model: "sphere", scale: new(9.5f, 1.2f, 9.5f), position: new(0, -0.4f, 0), color: pal.Ground);
                new Entity(parent: root, model: "diamond", scale: new(10.2f, 0.25f, 10.2f), color: pal.Accent, unlit: true);
                if (key == "safe_travels")
                {
                    // Миниатюра орбитального крейсера UES «Safe Travels»
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
            else if (cat == "achievements" && GameData.Achievements.TryGetValue(key, out var ach))
            {
                if (!string.IsNullOrEmpty(ach.UnlocksCharacter) && SurvivorDatabase.Classes.TryGetValue(ach.UnlocksCharacter, out var achSurv))
                {
                    _logDetailActorBaseScale = ach.UnlocksCharacter == "mindustry" ? 1.12f : 1.0f;
                    _logDetailTargetY = 1.18f;
                    _logDetailPreviewActor.Position = new Vector3(0f, ach.UnlocksCharacter == "mindustry" ? 0.92f : 0.12f, 0f);
                    _logDetailPreviewRing.Color = achSurv.BodyColor;
                    _logDetailSurvParts = SurvivorDatabase.BuildSurvivorMesh(_logDetailPreviewActor, ach.UnlocksCharacter, 0);
                }
                else if (!string.IsNullOrEmpty(ach.UnlocksItem) && GameData.ItemPool.TryGetValue(ach.UnlocksItem, out var achItem))
                {
                    _logDetailActorBaseScale = 1.92f;
                    _logDetailTargetY = 1.35f;
                    _logDetailPreviewActor.Position = new Vector3(0f, 1.35f, 0f);
                    _logDetailPreviewRing.Color = achItem.Color;
                    ItemBuilder.BuildItemMesh(_logDetailPreviewActor, ach.UnlocksItem);
                }
                else
                {
                    _logDetailActorBaseScale = 1.45f;
                    _logDetailTargetY = 1.30f;
                    _logDetailPreviewActor.Position = new Vector3(0f, 1.30f, 0f);
                    _logDetailPreviewRing.Color = Color.Gold;
                    var cup = new Entity(parent: _logDetailPreviewActor);
                    new Entity(parent: cup, model: "diamond", scale: new(0.85f, 1.35f, 0.85f), color: Color.Gold, unlit: true);
                    new Entity(parent: cup, model: "cube", scale: new(1.25f, 0.08f, 1.25f), rotation: new(0, 45f, 18f), color: Color.SkyBlue, unlit: true);
                }
            }
            else if (cat == "modifiers")
            {
                Color modCol = GameData.RunArtifacts.TryGetValue(key, out var rArt)
                    ? rArt.Color
                    : (GameData.Difficulties.TryGetValue(key, out var dDef) ? dDef.Color : Color.Gold);
                _logDetailActorBaseScale = 1.50f;
                _logDetailTargetY = 1.30f;
                _logDetailPreviewActor.Position = new Vector3(0f, 1.30f, 0f);
                _logDetailPreviewRing.Color = modCol;
                var relic = new Entity(parent: _logDetailPreviewActor);
                new Entity(parent: relic, model: "diamond", scale: new(0.84f, 1.32f, 0.84f), color: modCol, unlit: true);
                new Entity(parent: relic, model: "cube", scale: new(1.16f, 0.08f, 1.16f), rotation: new(0, 45f, 18f), color: Color.White, unlit: true);
                new Entity(parent: relic, model: "cube", scale: new(1.16f, 0.08f, 1.16f), rotation: new(18f, 45f, 0), color: modCol, unlit: true);
                new Entity(parent: relic, model: "sphere", scale: new(0.42f), color: Color.White, unlit: true);
            }

            _logDetailPreviewActor.Scale = new Vector3(_logDetailActorBaseScale);
        }

        private static void UpdateAndDrawLogbookDetailModal()
        {
            if (!_logDetailOpen) return;
            float dt = Math.Min(Raylib.GetFrameTime(), 0.05f);
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();

            int mx = 30, my = 24, mw = sw - 60, mh = sh - 48;
            int leftX = mx + 22, leftY = my + 66, leftW = (int)(mw * 0.54f) - 28, leftH = mh - 86;
            int rightX = leftX + leftW + 20, rightY = leftY, rightW = mx + mw - rightX - 22, rightH = leftH - 58;
            Rectangle leftRect = new(leftX, leftY, leftW, leftH);
            Rectangle vpRect = new(rightX, rightY, rightW, rightH);

            Vector2 mp = Raylib.GetMousePosition();
            float wheel = Raylib.GetMouseWheelMove();

            // Вращение и зум 3D-модели справа
            if (Raylib.CheckCollisionPointRec(mp, vpRect) &&
                (Raylib.IsMouseButtonPressed(MouseButton.Left) || Raylib.IsMouseButtonPressed(MouseButton.Right)))
            {
                _logDetailDragging3D = true;
            }
            if (!Raylib.IsMouseButtonDown(MouseButton.Left) && !Raylib.IsMouseButtonDown(MouseButton.Right))
                _logDetailDragging3D = false;

            if (_logDetailDragging3D)
            {
                Vector2 md = Raylib.GetMouseDelta();
                _logDetailYaw -= md.X * 0.65f;
                _logDetailPitch = Math.Clamp(_logDetailPitch + md.Y * 0.42f, -30f, 52f);
            }
            else if (_logDetailAutoRotate)
            {
                _logDetailYaw += 24f * dt;
            }

            if (MathF.Abs(wheel) > 0.01f)
            {
                if (Raylib.CheckCollisionPointRec(mp, vpRect))
                    _logDetailZoom = Math.Clamp(_logDetailZoom + wheel * 0.14f, 0.48f, 2.35f);
                else if (Raylib.CheckCollisionPointRec(mp, leftRect))
                    _logDetailScrollY = MathF.Max(0f, _logDetailScrollY - wheel * 48f);
            }

            if (_logDetailSurvParts != null)
            {
                string activeSKey = _logDetailCategory == "achievements" && GameData.Achievements.TryGetValue(_logDetailKey, out var aInf) && !string.IsNullOrEmpty(aInf.UnlocksCharacter)
                    ? aInf.UnlocksCharacter!
                    : _logDetailKey;
                _logDetailSurvParts.UpdateAnimations(activeSKey, dt, false, true, 0f, 1f, 0f, 0f);
            }
            if (_logDetailEnemyParts != null)
            {
                float et = (float)Raylib.GetTime();
                if (_logDetailEnemyParts.Ring1 != null) _logDetailEnemyParts.Ring1.RotationY += 85f * dt;
                if (_logDetailEnemyParts.Ring2 != null) _logDetailEnemyParts.Ring2.RotationZ -= 65f * dt;
                if (_logDetailEnemyParts.PhaseRing != null) _logDetailEnemyParts.PhaseRing.RotationY += 50f * dt;
                if (_logDetailEnemyParts.Core != null) _logDetailEnemyParts.Core.RotationY += 70f * dt;
                if (_logDetailEnemyParts.Tentacles != null) _logDetailEnemyParts.Tentacles.RotationY = MathF.Sin(et * 2.4f) * 14f;
                if (_logDetailEnemyParts.Tail != null) _logDetailEnemyParts.Tail.RotationY = MathF.Sin(et * 2.8f) * 12f;
            }
            if (IsValid(_logDetailPreviewActor))
            {
                _logDetailPreviewActor!.Rotation = new Vector3(0f, _logDetailYaw, 0f);
                _logDetailPreviewActor.Scale = new Vector3(_logDetailActorBaseScale);
            }

            // Сбор полных данных страницы: Заголовок, Статус, Механика, Условие открытия + Награда, Лор
            var (entryTitle, entryBadge, accentCol, isUnlocked, mechanicsLines, unlockLines, loreRec) = BuildLogbookDetailContent();

            // Фон и шапка страницы архива
            Raylib.DrawRectangle(0, 0, sw, sh, Rgb(6, 8, 16, 232));
            Raylib.DrawRectangle(mx, my, mw, mh, Rgb(12, 16, 28, 252));
            Raylib.DrawRectangle(mx, my, mw, 4, accentCol);
            Raylib.DrawRectangleLines(mx, my, mw, mh, Rgb(accentCol.R, accentCol.G, accentCol.B, 150));

            Engine.DrawTextLeft($"АРХИВ UES «SAFE TRAVELS» :: {entryBadge}", mx + 24, my + 18, 18f, accentCol);

            // Навигация между соседними записями текущей вкладки Журнала
            var catEntries = GetLogEntries(_logDetailCategory);
            int curEntryIdx = catEntries.FindIndex(e => e.Key == _logDetailKey);
            if (catEntries.Count > 1 && curEntryIdx >= 0)
            {
                if (Engine.DrawButton(new Rectangle(mx + mw - 472, my + 12, 122, 36), "<- ПРЕД.", Rgb(34, 46, 70), Rgb(56, 76, 112), Color.White, 14f))
                {
                    int prev = (curEntryIdx - 1 + catEntries.Count) % catEntries.Count;
                    SelectLogbookEntry(_logDetailCategory, catEntries[prev].Key);
                    OpenLogbookDetailPage(_logDetailCategory, catEntries[prev].Key, 0);
                    return;
                }
                if (Engine.DrawButton(new Rectangle(mx + mw - 342, my + 12, 122, 36), "СЛЕД. ->", Rgb(34, 46, 70), Rgb(56, 76, 112), Color.White, 14f))
                {
                    int next = (curEntryIdx + 1) % catEntries.Count;
                    SelectLogbookEntry(_logDetailCategory, catEntries[next].Key);
                    OpenLogbookDetailPage(_logDetailCategory, catEntries[next].Key, 0);
                    return;
                }
            }

            if (Engine.DrawButton(new Rectangle(mx + mw - 210, my + 12, 188, 36), "НАЗАД В ЖУРНАЛ [ESC]",
                Rgb(110, 38, 48), Rgb(155, 56, 68), Color.White, 14f, Color.Red))
            {
                CloseLogbookDetailPage();
                return;
            }

            // =========================================================================
            // ЛЕВАЯ ПОЛОВИНА: ПРОКРУЧИВАЕМОЕ ДОСЬЕ, ОПИСАНИЕ ОТКРЫТИЯ И ЛОР ИГРЫ
            // =========================================================================
            Raylib.DrawRectangle(leftX, leftY, leftW, leftH, Rgb(16, 22, 36, 245));
            Raylib.DrawRectangleLines(leftX, leftY, leftW, leftH, Rgb(48, 66, 98, 200));

            Raylib.BeginScissorMode(leftX + 2, leftY + 2, leftW - 14, leftH - 4);
            float cy = leftY + 18f - _logDetailScrollY;
            float textW = leftW - 44f;
            float tx = leftX + 20f;

            // 1. Название и статус разблокировки
            Engine.DrawTextLeft(entryTitle, tx, cy, 25f, accentCol);
            cy += 34f;
            string statusTxt = isUnlocked ? "СТАТУС В ПРОФИЛЕ: ОТКРЫТО В ИГРЕ ✓" : "СТАТУС В ПРОФИЛЕ: ЕЩЁ НЕ ОТКРЫТО (СМ. УСЛОВИЕ НИЖЕ)";
            Engine.DrawTextLeft(statusTxt, tx, cy, 14f, isUnlocked ? Color.Lime : Color.Orange);
            cy += 26f;

            // Переключатель обликов и альтернативных личностей прямо на странице выжившего
            if (_logDetailCategory == "survivors" && SurvivorDatabase.Classes.TryGetValue(_logDetailKey, out var sCfg) && sCfg.Skins.Count > 1)
            {
                Engine.DrawTextLeft("ВЕРСИИ И ОБЛИКИ ПЕРСОНАЖА (меняет 3D-модель и лор):", tx, cy, 13f, Color.Gold);
                cy += 22f;
                int btnW = Math.Min(220, (leftW - 48) / sCfg.Skins.Count - 6);
                for (int si = 0; si < sCfg.Skins.Count; si++)
                {
                    var sk = sCfg.Skins[si];
                    bool sel = _logDetailPersonaSkinIdx == si;
                    Rectangle skRect = new(tx + si * (btnW + 8), cy, btnW, 32);
                    if (Engine.DrawButton(skRect, $"{si + 1}. {sk.Name}",
                        sel ? Rgb(44, 88, 152) : Rgb(24, 32, 50), Rgb(62, 112, 182),
                        sel ? Color.Yellow : Color.White, 12f, sel ? Color.Yellow : sk.PrimaryColor))
                    {
                        _logDetailPersonaSkinIdx = si;
                        _logDetailScrollY = 0f;
                        RebuildLogbookDetail3DModelKeepingAngle();
                    }
                }
                cy += 44f;
            }

            // 2. Блок «ЧТО ДЕЛАЕТ / БОЕВЫЕ ХАРАКТЕРИСТИКИ»
            Raylib.DrawRectangle((int)tx, (int)cy, (int)textW, 28, Rgb(26, 38, 62, 240));
            Raylib.DrawRectangle((int)tx, (int)cy, 4, 28, Color.SkyBlue);
            Engine.DrawTextLeft("ЧТО ДЕЛАЕТ И ХАРАКТЕРИСТИКИ:", tx + 12f, cy + 6f, 15f, Color.SkyBlue);
            cy += 36f;
            foreach (var mLine in mechanicsLines)
            {
                cy += Engine.DrawTextWrapped(mLine, tx + 4f, cy, textW - 8f, 15f, Color.White, 4f) + 6f;
            }
            cy += 10f;

            // 3. Блок «КАК ОТКРЫВАЕТСЯ И НАГРАДА»
            Raylib.DrawRectangle((int)tx, (int)cy, (int)textW, 28, Rgb(42, 34, 20, 240));
            Raylib.DrawRectangle((int)tx, (int)cy, 4, 28, Color.Gold);
            Engine.DrawTextLeft("КАК ОТКРЫВАЕТСЯ И НАГРАДА:", tx + 12f, cy + 6f, 15f, Color.Gold);
            cy += 36f;
            foreach (var uLine in unlockLines)
            {
                cy += Engine.DrawTextWrapped(uLine, tx + 4f, cy, textW - 8f, 15f, Rgb(255, 232, 165), 4f) + 6f;
            }
            cy += 14f;

            // 4. Блок «ЛОРНОЕ ОПИСАНИЕ»
            Raylib.DrawRectangle((int)tx, (int)cy, (int)textW, 30, Rgb(34, 24, 52, 245));
            Raylib.DrawRectangle((int)tx, (int)cy, 4, 30, Rgb(205, 135, 255));
            Engine.DrawTextLeft("ЛОРНОЕ ОПИСАНИЕ :: БОРТОВОЙ АРХИВ", tx + 12f, cy + 7f, 15f, Rgb(215, 155, 255));
            cy += 38f;

            if (!string.IsNullOrWhiteSpace(loreRec.Header))
            {
                cy += Engine.DrawTextWrapped(loreRec.Header, tx + 4f, cy, textW - 8f, 13f, Color.SkyBlue, 3f) + 8f;
            }
            if (!string.IsNullOrWhiteSpace(loreRec.Quote))
            {
                var qLines = Engine.WrapText($"«{loreRec.Quote.Trim('«', '»', '"')}»", 15f, textW - 26f);
                float qH = qLines.Count * 20f + 14f;
                Raylib.DrawRectangle((int)tx + 4, (int)cy, (int)textW - 8, (int)qH, Rgb(22, 30, 48, 235));
                Raylib.DrawRectangle((int)tx + 4, (int)cy, 3, (int)qH, accentCol);
                Engine.DrawTextWrapped($"«{loreRec.Quote.Trim('«', '»', '"')}»", tx + 16f, cy + 7f, textW - 26f, 15f, Color.Gold, 5f);
                cy += qH + 12f;
            }

            cy += Engine.DrawTextWrapped(loreRec.Body, tx + 4f, cy, textW - 8f, 15f, Rgb(226, 236, 248), 5f) + 14f;

            if (!string.IsNullOrWhiteSpace(loreRec.ExtraNotes))
            {
                cy += Engine.DrawTextWrapped(loreRec.ExtraNotes, tx + 4f, cy, textW - 8f, 14f, Rgb(155, 215, 185), 4f) + 16f;
            }

            float totalContentHeight = (cy + _logDetailScrollY) - (leftY + 18f);
            float maxScroll = MathF.Max(0f, totalContentHeight - (leftH - 36f));
            _logDetailScrollY = Math.Clamp(_logDetailScrollY, 0f, maxScroll);
            Raylib.EndScissorMode();

            // Полоса прокрутки слева
            if (maxScroll > 4f)
            {
                int sbX = leftX + leftW - 9;
                int sbH = leftH - 12;
                Raylib.DrawRectangle(sbX, leftY + 6, 5, sbH, Rgb(24, 32, 48));
                float thumbRatio = Math.Clamp(leftH / (totalContentHeight + 1f), 0.12f, 1f);
                int thumbH = Math.Max(28, (int)(sbH * thumbRatio));
                int thumbY = leftY + 6 + (int)((sbH - thumbH) * (_logDetailScrollY / maxScroll));
                Raylib.DrawRectangle(sbX, thumbY, 5, thumbH, accentCol);
            }

            // =========================================================================
            // ПРАВАЯ ПОЛОВИНА: ИНТЕРАКТИВНЫЙ 3D ПРОСМОТРЩИК МОДЕЛИ
            // =========================================================================
            Raylib.DrawRectangle(rightX, rightY, rightW, rightH, Rgb(10, 14, 24, 250));
            Raylib.DrawRectangleLines(rightX, rightY, rightW, rightH, Rgb(accentCol.R, accentCol.G, accentCol.B, 185));

            if (IsValid(_logDetailPreviewRoot))
            {
                float dist = (5.45f / _logDetailZoom);
                float pitchRad = _logDetailPitch * (MathF.PI / 180f);
                Vector3 camUrsina = new(
                    0f,
                    _logDetailTargetY + MathF.Sin(pitchRad) * dist,
                    -MathF.Cos(pitchRad) * dist);
                Vector3 targetUrsina = new(0f, _logDetailTargetY, 0f);

                Camera3D previewCam = new()
                {
                    Position = new Vector3(-camUrsina.X, camUrsina.Y, camUrsina.Z),
                    Target = new Vector3(-targetUrsina.X, targetUrsina.Y, targetUrsina.Z),
                    Up = Vector3.UnitY,
                    FovY = 46f,
                    Projection = CameraProjection.Perspective
                };

                Raylib.BeginScissorMode(rightX + 2, rightY + 2, rightW - 4, rightH - 4);
                Raylib.BeginMode3D(previewCam);
                Engine.DrawEntityPreviewIsolated(_logDetailPreviewRoot!, previewCam);
                Raylib.EndMode3D();
                Raylib.EndScissorMode();
            }

            Engine.DrawTextCentered("Зажмите ЛКМ/ПКМ и двигайте мышь для вращения 3D-модели  |  Колёсико — Масштаб",
                rightX + rightW * 0.5f, rightY + rightH - 18f, 13f, Rgb(165, 200, 235));

            // Кнопки управления 3D-камерой под окном предпросмотра
            int ctrlY = rightY + rightH + 10;
            if (Engine.DrawButton(new Rectangle(rightX, ctrlY, 210, 40),
                _logDetailAutoRotate ? "АВТО-ВРАЩЕНИЕ: ВКЛ" : "АВТО-ВРАЩЕНИЕ: ВЫКЛ",
                _logDetailAutoRotate ? Rgb(36, 78, 128) : Rgb(26, 34, 52), Rgb(56, 104, 168), Color.White, 13f, Color.SkyBlue))
            {
                _logDetailAutoRotate = !_logDetailAutoRotate;
            }

            if (Engine.DrawButton(new Rectangle(rightX + 220, ctrlY, 185, 40), "СБРОСИТЬ РАКУРС",
                Rgb(38, 48, 72), Rgb(62, 78, 114), Color.White, 13f))
            {
                _logDetailYaw = 24f;
                _logDetailPitch = 12f;
                _logDetailZoom = 1.0f;
            }

            if (_logDetailSurvParts != null)
            {
                if (Engine.DrawButton(new Rectangle(rightX + 415, ctrlY, rightW - 415, 40), "▶ БОЕВАЯ СТОЙКА",
                    Rgb(72, 42, 112), Rgb(105, 64, 158), Color.Yellow, 13f, Color.Magenta))
                {
                    _logDetailSurvParts.TriggerSkillAnim("r", 0.95f);
                    SoundManager.Play("ui_ready", 0.8f);
                }
            }
        }

        private static void RebuildLogbookDetail3DModelKeepingAngle()
        {
            float savedYaw = _logDetailYaw;
            float savedPitch = _logDetailPitch;
            float savedZoom = _logDetailZoom;
            RebuildLogbookDetail3DScene();
            _logDetailYaw = savedYaw;
            _logDetailPitch = savedPitch;
            _logDetailZoom = savedZoom;
        }

        private static (string Title, string Badge, Color Accent, bool Unlocked, List<string> Mechanics, List<string> UnlockHow, LoreRecord Lore)
            BuildLogbookDetailContent()
        {
            var prog = ProgressData.Instance;
            var mech = new List<string>();
            var unlHow = new List<string>();
            string cat = _logDetailCategory;
            string key = _logDetailKey;

            if (cat == "items" && GameData.ItemPool.TryGetValue(key, out var item))
            {
                bool unl = prog.UnlockedItems.Contains(key);
                mech.Add($"• Редкость: {item.TierName}   |   Тактический код: [{item.Short}]");
                mech.Add($"• Эффект в забеге: {item.Desc}");

                var ach = GameData.GetItemUnlockAchievement(key);
                if (ach == null || item.DefaultUnlocked)
                {
                    unlHow.Add("• Доступен в базовом пуле предметов с самого начала игры.");
                }
                else
                {
                    unlHow.Add($"• Требуется достижение «{ach.Name}»: {ach.Desc}");
                    unlHow.Add($"• Награда за выполнение: {ach.RewardText}");
                }

                return (item.Name, $"ПРЕДМЕТ [{item.TierName}]", item.Color, unl, mech, unlHow, LoreDatabase.GetLore("items", key));
            }

            if (cat == "survivors" && SurvivorDatabase.Classes.TryGetValue(key, out var surv))
            {
                bool unl = prog.IsCharacterUnlocked(key);
                int sIdx = Math.Clamp(_logDetailPersonaSkinIdx, 0, Math.Max(0, surv.Skins.Count - 1));
                string personaKey = SafeTravelsDialogues.GetPersonaKey(key, sIdx);
                var skinObj = sIdx < surv.Skins.Count ? surv.Skins[sIdx] : surv.Skins[0];
                Color col = sIdx > 0 ? skinObj.PrimaryColor : surv.BodyColor;
                string title = sIdx > 0 && personaKey != key
                    ? $"{SafeTravelsDialogues.GetPersonaDisplayName(personaKey)} (Альт. личность {surv.Name})"
                    : $"{surv.Name} — {surv.Subtitle}";

                mech.Add($"• Базовое здоровье: {surv.Hp} HP  |  Скорость: {surv.Speed}  |  Базовый урон: {surv.Damage}  |  Темп: {surv.FireRate}/с");
                mech.Add($"• Пассивная способность «{surv.PassiveName}»: {surv.PassiveDesc}");

                var varMap = skinObj.CustomSkillVariants ?? surv.SkillVariants;
                foreach (var (slot, label) in new[] { ("m1", "ЛКМ"), ("m2", "ПКМ"), ("shift", "Shift"), ("r", "R / Ульта") })
                {
                    if (varMap.TryGetValue(slot, out var vList) && vList.Count > 0)
                        mech.Add($"• [{label}] {vList[0].Name} (КД {vList[0].Cd}с): {vList[0].Desc}");
                    else if (surv.Skills.TryGetValue(slot, out var sk))
                        mech.Add($"• [{label}] {sk.Name} (КД {sk.Cd}с): {sk.Desc}");
                }

                if (key == "commando")
                {
                    unlHow.Add("• Стартовый оперативник десанта UES — открыт по умолчанию.");
                }
                else if (key == "sanabi")
                {
                    unlHow.Add("• Секретный выживший: Бригадный Генерал и Мари всегда присутствуют на корабле UES «Safe Travels» и доступны авторизованным командирам.");
                }
                else
                {
                    var ach = GameData.GetCharacterUnlockAchievement(key);
                    if (ach != null)
                    {
                        unlHow.Add($"• Испытание «{ach.Name}»: {ach.Desc}");
                        unlHow.Add($"• Награда: {ach.RewardText}");
                    }
                }

                LoreRecord lore = LoreDatabase.GetLore("survivors", personaKey);
                return (title, "ДОСЬЕ ВЫЖИВШЕГО", col, unl, mech, unlHow, lore);
            }

            if ((cat == "monsters" || cat == "bosses") && GameData.MonsterLog.TryGetValue(key, out var mob))
            {
                bool isBoss = GameData.IsKnownBossType(key);
                bool isTrialBoss = Array.IndexOf(GameData.TrialBossTypes, key) >= 0;
                Color col = key == "mithrix" ? Color.SkyBlue : (isBoss ? Color.Red : Rgb(255, 145, 60));

                string rank = key == "mithrix"
                    ? "Финальный Босс Луны (4 фазы)"
                    : (isTrialBoss ? "Уникальный Босс Испытания Персонажа" : (isBoss ? "Страж Телепорта" : "Полевой противник"));
                mech.Add($"• Классификация: {rank}");
                mech.Add($"• Базовое здоровье: {mob.Hp} HP   |   Базовый урон: {mob.Damage} ед.");
                mech.Add($"• Поведение в бою: {mob.Desc}");

                if (key == "mithrix")
                    unlHow.Add("• Встречается на вершине Небесного Колизея локации «Начало (Луна)» после зарядки 4 Столпов.");
                else if (isTrialBoss)
                    unlHow.Add("• Призывается во время прохождения уникальных сюжетных испытаний разблокировки персонажей и на секретных этажах.");
                else if (isBoss)
                    unlHow.Add("• Появляется при активации Телепорта на обычных этажах Petrichor V.");
                else if (key is "hell_drone" or "combine_soldier" or "sarkaz_mercenary" or "hollow_shadow" or "nether_blaze")
                    unlHow.Add("• Особый противник межпространственных разломов, секретных локаций и монументов испытаний выживших.");
                else
                    unlHow.Add("• Обитает в биомах Petrichor V; запись в бестиарии доступна для изучения в любой момент.");

                return (mob.Name, isBoss ? "БЕСТИАРИЙ :: БОСС" : "БЕСТИАРИЙ :: МОНСТР", col, true, mech, unlHow, LoreDatabase.GetLore(cat, key));
            }

            if (cat == "stages")
            {
                var pal = GameData.StagePalettes.Find(p => p.Id == key)
                          ?? (GameData.SpecialPalettes.TryGetValue(key, out var sp) ? sp : GameData.StagePalettes[0]);
                bool disc = prog.DiscoveredStages.Contains(key) || key == "safe_travels" && IsSafeTravelsUnlocked();
                int layerNum = GameData.GetStageBiomeLayerNumber(key);

                string poolDesc = layerNum switch
                {
                    1 => "Слой I — Пул 1-го этапа (также этапы 4, 7, 10... при зацикливании)",
                    2 => "Слой II — Пул 2-го этапа (также этапы 5, 8, 11... при зацикливании)",
                    3 => "Слой III — Пул 3-го этапа (также этапы 6, 9, 12... перед Луной или циклом)",
                    _ => "Уникальная / Секретная внецикловая локация (никогда не выпадает в обычных пулах этапов!)"
                };

                mech.Add($"• Распределение в пулах этапов: {poolDesc}");
                mech.Add($"• Описание среды: {pal.Desc}");

                if (layerNum > 0)
                    unlHow.Add($"• Автоматически посещается при прохождении {layerNum}-го слоя маршрута Телепортов.");
                else if (key == "safe_travels")
                    unlHow.Add("• Открывается навсегда в Главном меню и Лобби после первой победы над Митриксом на Луне.");
                else if (key == "bazaar")
                    unlHow.Add("• Открывается активацией Алтаря Тритона (за 1 Лунную монету) и входом в Синий Портал после Телепорта.");
                else if (key == "moon")
                    unlHow.Add("• Настройте внешние лепестки Изначального Телепорта на 3-м этапе (или кратном 3) на Луну.");
                else
                    unlHow.Add("• Вход через межпространственный Разлом Секретного Испытания на этапах Petrichor V.");

                return (pal.Name, "КАРТОГРАФИЯ ЛОКАЦИИ", pal.Accent, disc, mech, unlHow, LoreDatabase.GetLore("stages", key));
            }

            if (cat == "achievements" && GameData.Achievements.TryGetValue(key, out var achInfo))
            {
                bool unl = prog.UnlockedAchievements.Contains(key);
                Color col = unl ? Color.Lime : Color.Gold;
                mech.Add($"• Условие получения: {achInfo.Desc}");
                unlHow.Add($"• Гарантированная награда: {achInfo.RewardText}");
                unlHow.Add(unl ? "• Вы уже выполнили это испытание и получили награду!" : "• Награда сразу откроется в профиле, как только условие будет выполнено.");
                return (achInfo.Name, "ДОСТИЖЕНИЕ И НАГРАДА", col, unl, mech, unlHow, LoreDatabase.GetLore("achievements", key));
            }

            if (cat == "modifiers")
            {
                if (GameData.Difficulties.TryGetValue(key, out var dDef))
                {
                    mech.Add($"• Множитель роста сложности со временем: x{dDef.ScaleMult:F2}");
                    mech.Add($"• Множитель регенерации выживших: x{dDef.RegenMult:F2}  |  Множитель урона врагов: x{dDef.PlayerDmgTakenMult:F2}");
                    mech.Add($"• Правила режима: {dDef.Desc}");
                    unlHow.Add("• Доступно сразу во вкладке «3. СЛОЖНОСТЬ» в Лобби перед началом любого забега.");
                    return ($"Сложность: {dDef.Name}", "МОДИФИКАТОР СЛОЖНОСТИ", dDef.Color, true, mech, unlHow, LoreDatabase.GetLore("modifiers", key));
                }
                if (GameData.RunArtifacts.TryGetValue(key, out var aDef))
                {
                    mech.Add($"• Код реликвии: [{aDef.Short}]");
                    mech.Add($"• Изменение законов мира: {aDef.Desc}");
                    unlHow.Add("• Свободно включается и комбинируется с другими Артефактами в Лобби выживших.");
                    return (aDef.Name, "АРТЕФАКТ ЗАБЕГА", aDef.Color, true, mech, unlHow, LoreDatabase.GetLore("modifiers", key));
                }
            }

            return (key, "АРХИВ UES", Color.SkyBlue, true, mech, unlHow, LoreDatabase.GetLore(cat, key));
        }
    }
}
