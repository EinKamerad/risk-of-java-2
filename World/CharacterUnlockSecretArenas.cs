using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using RiskOfCSharp2.Entities;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.World
{
    // 1. ФРЕДДИ ФАЗБЕР: Офис Ночной Смены (Левая/Правая Бронедвери [E], Завод Шкатулки [E], Блэкаут Тореадора + Эндоскелет)
    public class FazbearSecurityOfficeEncounter : Entity
    {
        public bool IsActive = false;
        public string HourLabel = "12:00 AM";
        public float PowerPercent = 100f;
        public float MusicBoxPercent = 100f;

        private float _shiftElapsed = 0f;
        private bool _leftDoorClosed = false;
        private bool _rightDoorClosed = false;
        private float _leftThreatTimer = 4.5f;
        private float _rightThreatTimer = 7.0f;
        private bool _blackoutPhase = false;
        private CombatTarget? _endoBoss = null;
        private readonly Entity _leftDoorMesh;
        private readonly Entity _rightDoorMesh;
        private readonly Entity _musicBoxCore;
        private readonly Entity _labelEnt;

        public FazbearSecurityOfficeEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(5.4f, 0.25f, 4.4f), position: new(0, 0.12f, 0), color: Rgb(36, 32, 48));
            _leftDoorMesh = new Entity(parent: this, model: "cube", scale: new(0.35f, 2.8f, 2.2f), position: new(-3.2f, 3.6f, 0), color: Rgb(185, 55, 55));
            _rightDoorMesh = new Entity(parent: this, model: "cube", scale: new(0.35f, 2.8f, 2.2f), position: new(3.2f, 3.6f, 0), color: Rgb(185, 55, 55));
            _musicBoxCore = new Entity(parent: this, model: "cube", scale: new(1.2f, 1.0f, 1.0f), position: new(0, 0.9f, 1.2f), color: Rgb(145, 75, 225), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.4f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Начать Ночную Смену Охраны Fazbear (12:00 AM -> 06:00 AM)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || IsActive);

            if (!IsActive && !_blackoutPhase && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                IsActive = true;
                CharacterUnlockSystems.ActiveFazbearOffice = this;
                SoundManager.Play("dante_clock", 1.0f, 1.15f);
                g.ShowBanner("НОЧНАЯ СМЕНА НАЧАЛАСЬ! СЛЕДИТЕ ЗА ДВЕРЯМИ СЛЕВА/СПРАВА [E] И МУЗЫКАЛЬНОЙ ШКАТУЛКОЙ В ЦЕНТРЕ [E]!", Color.Gold, 4.2f);
                return;
            }

            if (!IsActive) return;

            if (!_blackoutPhase)
            {
                _shiftElapsed += dt;
                int hr = Math.Clamp((int)(_shiftElapsed / 4.2f), 0, 5);
                HourLabel = hr == 0 ? "12:00 AM" : $"0{hr}:00 AM";

                float drain = 1.4f + (_leftDoorClosed ? 3.2f : 0f) + (_rightDoorClosed ? 3.2f : 0f);
                PowerPercent = MathF.Max(0f, PowerPercent - drain * dt);
                MusicBoxPercent = MathF.Max(0f, MusicBoxPercent - 6.8f * dt);

                Vector3 leftBtn = Position + new Vector3(-3.2f, 0f, 0f);
                Vector3 rightBtn = Position + new Vector3(3.2f, 0f, 0f);
                Vector3 boxBtn = Position + new Vector3(0f, 0f, 1.2f);

                if (Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    if (Vector3.Distance(g.PlayerPos, leftBtn) < 2.8f && PowerPercent > 0f)
                    {
                        _leftDoorClosed = !_leftDoorClosed;
                        SoundManager.Play("shield_toggle", 0.9f, _leftDoorClosed ? 0.85f : 1.15f);
                    }
                    else if (Vector3.Distance(g.PlayerPos, rightBtn) < 2.8f && PowerPercent > 0f)
                    {
                        _rightDoorClosed = !_rightDoorClosed;
                        SoundManager.Play("shield_toggle", 0.9f, _rightDoorClosed ? 0.85f : 1.15f);
                    }
                    else if (Vector3.Distance(g.PlayerPos, boxBtn) < 3.0f)
                    {
                        MusicBoxPercent = MathF.Min(100f, MusicBoxPercent + 38f);
                        SoundManager.Play("ui_ready", 0.85f, 1.3f);
                    }
                }

                if (PowerPercent <= 0f) { _leftDoorClosed = false; _rightDoorClosed = false; }
                _leftDoorMesh.Y = Lerp(_leftDoorMesh.Y, _leftDoorClosed ? 1.4f : 3.6f, dt * 10f);
                _rightDoorMesh.Y = Lerp(_rightDoorMesh.Y, _rightDoorClosed ? 1.4f : 3.6f, dt * 10f);

                _leftThreatTimer -= dt;
                if (_leftThreatTimer <= 0f)
                {
                    _leftThreatTimer = 6.5f;
                    if (!_leftDoorClosed)
                    {
                        CharacterUnlockEncounters.SpawnEncounterEnemy(g, leftBtn + new Vector3(-3f, 0, 0), "hollow_shadow");
                        g.ShowBanner("ЛЕВАЯ ДВЕРЬ БЫЛА ОТКРЫТА — ТЕНЬ ПРОНИКЛА В ОФИС!", Color.Red, 2.2f);
                    }
                    else SoundManager.Play("parry", 0.85f, 0.85f);
                }

                _rightThreatTimer -= dt;
                if (_rightThreatTimer <= 0f)
                {
                    _rightThreatTimer = 7.2f;
                    if (!_rightDoorClosed)
                    {
                        CharacterUnlockEncounters.SpawnEncounterEnemy(g, rightBtn + new Vector3(3f, 0, 0), "brass_contraption");
                        g.ShowBanner("ПРАВАЯ ДВЕРЬ БЫЛА ОТКРЫТА — АНИМАТРОНИК ВОШЁЛ В ЗАЛ!", Color.Red, 2.2f);
                    }
                    else SoundManager.Play("parry", 0.85f, 0.85f);
                }

                if (MusicBoxPercent <= 0f)
                {
                    MusicBoxPercent = 45f;
                    CombatManager.DamagePlayer(16f);
                    g.ShowBanner("ШКАТУЛКА РАЗРЯДИЛАСЬ! ЗАВОДИТЕ ЕЁ В ЦЕНТРЕ ОФИСА [E]!", Color.Magenta, 2.4f);
                }

                _labelEnt.BillboardText = $"ОФИС FAZBEAR ({HourLabel}) | Двери ПоБокам [E] | Завести Шкатулку В Центре [E]";

                if (_shiftElapsed >= 21.0f || PowerPercent <= 0f)
                {
                    _blackoutPhase = true;
                    HourLabel = "05:00 AM (БЛЭКАУТ)";
                    _endoBoss = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, 10f), "animatronic_endo", mult: 0.90f);
                    SoundManager.Play("freddy_toreador", 1.15f, 1.0f);
                    g.ShowBanner("⚡ 05:00 AM — БЛЭКАУТ И МАРШ ТОРЕАДОРА! ПОБЕДИТЕ ЗОЛОТОЙ ЭНДОСКЕЛЕТ ДО 06:00 AM! ⚡", Color.Gold, 4.5f);
                }
            }
            else
            {
                _labelEnt.BillboardText = "05:00 AM — Марш Тореадора: Одолейте Золотой Эндоскелет!";
                if (_endoBoss == null || !IsValid(_endoBoss) || _endoBoss.Hp <= 0f || _endoBoss.IsAlly)
                {
                    IsActive = false;
                    if (CharacterUnlockSystems.ActiveFazbearOffice == this) CharacterUnlockSystems.ActiveFazbearOffice = null;
                    SoundManager.Play("victory", 1.0f, 1.1f);
                    g.UnlockAchievement("ach_char_freddy");
                    Destroy(this);
                }
            }
        }
    }

    // 2. ГОРДОН ФРИМАН: Вагонетка GG-3883 + Лазерные Растяжки HECU + Антимасс-Спектрометр
    public class BlackMesaSpectrometerEncounter : Entity
    {
        private bool _started = false;
        private float _cartProgress = 0f; // 0..18м до луча
        private bool _cascadeTriggered = false;
        private readonly Entity _cart;
        private readonly Entity _beam;
        private readonly Entity _labelEnt;
        private readonly List<(Entity Mine, float ZOff)> _tripmines = new();
        private readonly List<CombatTarget> _hecuWave = new();

        public BlackMesaSpectrometerEncounter(Vector3 pos) : base(position: pos)
        {
            _cart = new Entity(parent: this, position: new(0, 0, -9f));
            new Entity(parent: _cart, model: "cube", scale: new(1.6f, 0.9f, 1.8f), position: new(0, 0.5f, 0), color: Rgb(78, 86, 98));
            new Entity(parent: _cart, model: "diamond", scale: new(0.95f, 1.45f, 0.95f), position: new(0, 1.45f, 0), color: Color.Orange, unlit: true);

            _beam = new Entity(parent: this, model: "cube", scale: new(0.65f, 14f, 0.65f), position: new(0, 7f, 9f), color: Rgb(255, 185, 45), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.6f, -9f), color: Color.Orange)
            {
                BillboardText = "[E] Толкать Вагонетку с Образцом GG-3883 в Антимасс-Спектрометр"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            Vector3 cartWorld = Position + _cart.Position;
            float distToCart = Vector3.Distance(g.PlayerPos, cartWorld);
            _labelEnt.Position = _cart.Position + new Vector3(0, 3.2f, 0);
            _labelEnt.Enabled = !Program.IsSpectating && (distToCart < 16f || _started);

            if (!_started && distToCart < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _started = true;
                foreach (float zOff in new[] { -4.0f, 1.5f, 6.0f })
                {
                    var mine = new Entity(parent: this, position: new(0, 1.1f, zOff));
                    new Entity(parent: mine, model: "cube", scale: new(5.5f, 0.08f, 0.08f), color: Color.Red, unlit: true);
                    new Entity(parent: mine, model: "sphere", scaleUniform: 0.45f, position: new(-2.7f, 0, 0), color: Color.Red, unlit: true);
                    _tripmines.Add((mine, zOff));
                }
                g.ShowBanner("ТОЛКАЙТЕ ВАГОНЕТКУ GG-3883 В ЛУЧ И ОТСТРЕЛИВАЙТЕ КРАСНЫЕ ЛАЗЕРНЫЕ РАСТЯЖКИ HECU ПЕРЕД НЕЙ!", Color.Orange, 4.2f);
            }
            else if (_started && !_cascadeTriggered)
            {
                for (int i = _tripmines.Count - 1; i >= 0; i--)
                {
                    Vector3 mWorld = Position + _tripmines[i].Mine.Position;
                    if (CharacterUnlockEncounters.DetectPlayerAttackNear(g, mWorld, 3.0f))
                    {
                        SoundManager.Play("explosion", 0.9f, 1.2f);
                        new VisualFX(mWorld, Color.Orange, 3.2f, 0.25f);
                        Destroy(_tripmines[i].Mine);
                        _tripmines.RemoveAt(i);
                    }
                    else if (MathF.Abs(_cart.Z - _tripmines[i].ZOff) < 0.85f)
                    {
                        SoundManager.Play("explosion", 1.1f, 0.9f);
                        CombatManager.DamagePlayer(20f);
                        _cartProgress = MathF.Max(0f, _cartProgress - 4.5f);
                        Destroy(_tripmines[i].Mine);
                        _tripmines.RemoveAt(i);
                        g.ShowBanner("ВАГОНЕТКА ЗАДЕЛА ЛАЗЕРНУЮ МИНУ HECU! СБИВАЙТЕ ЛАЗЕРЫ ЗАРАНЕЕ!", Color.Red, 2.5f);
                    }
                }

                if (distToCart <= 4.5f)
                    _cartProgress = MathF.Min(18.0f, _cartProgress + 3.6f * dt);
                _cart.Z = -9.0f + _cartProgress;
                _labelEnt.BillboardText = $"Доставка GG-3883 в Спектрометр: {(int)(_cartProgress / 18f * 100f)}% | Лазерных растяжек: {_tripmines.Count}";

                if (_cartProgress >= 18.0f)
                {
                    _cascadeTriggered = true;
                    SoundManager.Play("explosion", 1.25f, 0.72f);
                    new VisualFX(Position + new Vector3(0, 2.5f, 9f), Color.Lime, 12.0f, 0.55f);
                    for (int i = 0; i < 4; i++)
                        _hecuWave.Add(CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3((i - 1.5f) * 5f, 0, 4f), i % 2 == 0 ? "combine_soldier" : "hell_drone", elite: i == 0));
                    g.ShowBanner("☢ КАСКАДНЫЙ РЕЗОНАНС! ОТРАЗИТЕ ШТУРМОВОЙ ДЕСАНТ АЛЬЯНСА И HECU! ☢", Color.Lime, 4.2f);
                }
            }
            else if (_cascadeTriggered)
            {
                _hecuWave.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                _labelEnt.BillboardText = $"Каскадный Резонанс: Отразите десант — осталось {_hecuWave.Count}";
                if (_hecuWave.Count == 0)
                {
                    g.UnlockAchievement("ach_char_freeman");
                    Destroy(this);
                }
            }
        }
    }

    // 3. ОХОТНИКИ DMC: Шкала Стиля SSS + Дуэль с Миражом Вергилия на Вершине Клипота
    public class QliphothStyleDuelEncounter : Entity
    {
        private int _phase = 0; // 0 = ожидание, 1 = набор ранга SSS, 2 = Мираж Вергилия
        private float _styleMeter = 0f;
        private float _prevPlayerHp = 0f;
        private int _prevKills = 0;
        private CombatTarget? _vergil = null;
        private readonly Entity _labelEnt;

        public QliphothStyleDuelEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "diamond", scale: new(2.2f, 0.6f, 2.2f), position: new(0, 0.3f, 0), color: Rgb(135, 25, 42));
            new Entity(parent: this, model: "diamond", scale: new(0.45f, 3.0f, 0.25f), position: new(0, 1.9f, 0), color: Rgb(95, 215, 255), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.9f, 0), color: Rgb(195, 85, 255))
            {
                BillboardText = "[E] Клинок Ямато на Вершине Клипота (Наберите Ранг SSS + Дуэль с Вергилием!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _phase > 0);

            if (_phase == 0 && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _phase = 1;
                _styleMeter = 20f;
                _prevPlayerHp = g.PlayerHp;
                _prevKills = Program.RunKills;
                for (int i = 0; i < 4; i++)
                    CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3((i - 1.5f) * 5.5f, 0, -5f), "hollow_shadow");
                SoundManager.Play("vergil_slash", 1.1f, 1.0f);
                g.ShowBanner("ДОКАЖИТЕ МОТИВАЦИЮ! АТАКУЙТЕ БЕЗ ПОЛУЧЕНИЯ УРОНА, ЧТОБЫ ПОДНЯТЬ СТИЛЬ ДО РАНГА «SSS»!", Rgb(195, 85, 255), 4.2f);
            }
            else if (_phase == 1)
            {
                _styleMeter = MathF.Max(0f, _styleMeter - 4.5f * dt);
                if (g.PlayerHp < _prevPlayerHp - 1f)
                    _styleMeter = MathF.Max(0f, _styleMeter - 28f);
                _prevPlayerHp = g.PlayerHp;

                if (Program.RunKills > _prevKills)
                {
                    _styleMeter = MathF.Min(100f, _styleMeter + (Program.RunKills - _prevKills) * 26f);
                    _prevKills = Program.RunKills;
                }
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) || Raylib.IsMouseButtonPressed(MouseButton.Right))
                    _styleMeter = MathF.Min(100f, _styleMeter + 5.5f);

                string rank = _styleMeter >= 90f ? "SSS" : (_styleMeter >= 70f ? "SS" : (_styleMeter >= 50f ? "S" : (_styleMeter >= 30f ? "A" : "B")));
                _labelEnt.BillboardText = $"Шкала Стиля Спарды: [{rank}] {(int)_styleMeter}% / 100% (Урон сбивает ранг!)";

                if (_styleMeter >= 100f)
                {
                    _phase = 2;
                    _vergil = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, 8f), "vergil_mirage", mult: 0.92f);
                    SoundManager.Play("vergil_slash", 1.25f, 0.88f);
                    g.ShowBanner("★ SMOKIN' SEXY STYLE [SSS]! МИРАЖ ВЕРГИЛИЯ ПРИНЯЛ ВАШ ВЫЗОВ! ★", Rgb(95, 225, 255), 4.5f);
                }
            }
            else if (_phase == 2)
            {
                _labelEnt.BillboardText = "Дуэль Сыновей Спарды: Одолейте Мираж Вергилия!";
                if (_vergil == null || !IsValid(_vergil) || _vergil.Hp <= 0f || _vergil.IsAlly)
                {
                    g.UnlockAchievement("ach_char_dmc_trio");
                    Destroy(this);
                }
            }
        }
    }

    // 4. V1: Кибер-Грайнд с перестраивающимися колоннами и таймером Кровавого Топлива (4 волны)
    public class CyberGrindArenaEncounter : Entity
    {
        private int _wave = 0; // 0 = ожидание, 1..4 = волны
        private float _bloodFuel = 20f;
        private int _prevKills = 0;
        private readonly List<Entity> _gridPillars = new();
        private readonly List<CombatTarget> _waveMobs = new();
        private readonly Entity _labelEnt;

        public CyberGrindArenaEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(2.4f, 2.8f, 1.4f), position: new(0, 1.4f, 0), color: Rgb(35, 38, 48));
            new Entity(parent: this, model: "cube", scale: new(1.8f, 1.1f, 0.2f), position: new(0, 1.8f, 0.65f), color: Color.Red, unlit: true);
            for (int i = 0; i < 6; i++)
            {
                float a = i * (MathF.PI / 3f);
                var pil = new Entity(parent: this, model: "cube", scale: new(3.2f, 4.0f, 3.2f),
                    position: new(MathF.Cos(a) * 10f, -1.8f, MathF.Sin(a) * 10f), color: Rgb(52, 22, 28));
                _gridPillars.Add(pil);
            }
            _labelEnt = new Entity(parent: this, position: new(0, 3.6f, 0), color: Color.Red)
            {
                BillboardText = "[E] Терминал P-1 «КИБЕР-ГРАЙНД» (4 волны на Кровавом Топливе!)"
            };
        }

        private void SpawnWave(IPlayerContext g, int w)
        {
            _wave = w;
            _bloodFuel = 22f;
            _waveMobs.Clear();
            int cnt = 2 + w;
            for (int i = 0; i < cnt; i++)
            {
                float a = (i / (float)cnt) * MathF.PI * 2f;
                string et = (w >= 3 && i == 0) ? "greater_wisp" : (i % 2 == 0 ? "hell_drone" : "imp");
                _waveMobs.Add(CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(MathF.Cos(a) * 11f, 0, MathF.Sin(a) * 11f), et, elite: w == 4));
            }
            SoundManager.Play("ult_cast", 1.0f, 0.9f + w * 0.08f);
            g.ShowBanner($"КИБЕР-ГРАЙНД: ВОЛНА {w}/4! УБИЙСТВА ПОПОЛНЯЮТ КРОВАВОЕ ТОПЛИВО!", Color.Red, 3.2f);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _wave > 0);

            if (_wave == 0 && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _prevKills = Program.RunKills;
                SpawnWave(g, 1);
            }
            else if (_wave > 0)
            {
                float t = (float)Raylib.GetTime();
                for (int i = 0; i < _gridPillars.Count; i++)
                    _gridPillars[i].Y = 0.8f + MathF.Sin(t * 1.8f + i + _wave * 1.4f) * 1.8f;

                if (Program.RunKills > _prevKills)
                {
                    _bloodFuel = MathF.Min(25f, _bloodFuel + (Program.RunKills - _prevKills) * 7.5f);
                    _prevKills = Program.RunKills;
                }
                _bloodFuel -= dt;
                if (_bloodFuel <= 0f)
                {
                    _bloodFuel = 8f;
                    CombatManager.DamagePlayer(12f);
                }

                _waveMobs.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                _labelEnt.BillboardText = $"КИБЕР-ГРАЙНД [Волна {_wave}/4] | Кровавое Топливо: {_bloodFuel:F1}с | Врагов: {_waveMobs.Count}";

                if (_waveMobs.Count == 0)
                {
                    if (_wave < 4) SpawnWave(g, _wave + 1);
                    else
                    {
                        g.UnlockAchievement("ach_char_v1");
                        Destroy(this);
                    }
                }
            }
        }
    }

    // 5. АЙГИС: Саркофаг S.E.E.S. в Башне Тартар (Жнец Тёмного Часа)
    public class DarkHourReaperEncounter : Entity
    {
        private bool _started = false;
        private CombatTarget? _reaper = null;
        private readonly Entity _labelEnt;

        public DarkHourReaperEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(1.5f, 3.2f, 1.2f), position: new(0, 1.6f, 0), color: Rgb(235, 195, 65));
            new Entity(parent: this, model: "diamond", scale: new(0.95f, 1.4f, 0.45f), position: new(0, 1.9f, 0.55f), color: Rgb(65, 245, 135), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.8f, 0), color: Rgb(65, 245, 135))
            {
                BillboardText = "[E] Саркофаг Тартара S.E.E.S. (Призвать и одолеть Жнеца Тёмного Часа!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _started);

            if (!_started && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _started = true;
                _reaper = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, 9f), "shadow_reaper", mult: 0.90f);
                CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(-6f, 0, 6f), "hollow_shadow");
                CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(6f, 0, 6f), "hollow_shadow");
                SoundManager.Play("dante_clock", 1.2f, 0.78f);
                g.ShowBanner("🕛 ТЁМНЫЙ ЧАС В БАШНЕ ТАРТАР! СОКРУШИТЕ ЖНЕЦА, ЧТОБЫ ПРОБУДИТЬ АЙГИС! 🕛", Rgb(65, 245, 135), 4.2f);
            }
            else if (_started)
            {
                _labelEnt.BillboardText = "Тёмный Час: Одолейте Жнеца Тартара!";
                if (_reaper == null || !IsValid(_reaper) || _reaper.Hp <= 0f || _reaper.IsAlly)
                {
                    g.UnlockAchievement("ach_char_aigis");
                    Destroy(this);
                }
            }
        }
    }

    // 6. РЕЙМУ ХАКУРЕЙ: Настоящий Touhou Graze (скольжение вплотную к пулям Спелл-Карт без получения урона)
    public class HakureiGrazeEncounter : Entity
    {
        private bool _active = false;
        private int _grazeCount = 0;
        private float _waveCd = 0f;
        private float _prevHp = 0f;
        private readonly HashSet<Entity> _grazedProjectiles = new();
        private readonly Entity _orb;
        private readonly Entity _labelEnt;

        public HakureiGrazeEncounter(Vector3 pos) : base(position: pos)
        {
            foreach (float sx in new[] { -1.5f, 1.5f })
                new Entity(parent: this, model: "cube", scale: new(0.35f, 3.6f, 0.35f), position: new(sx, 1.8f, 0), color: Rgb(220, 42, 46));
            new Entity(parent: this, model: "cube", scale: new(3.8f, 0.38f, 0.45f), position: new(0, 3.5f, 0), color: Rgb(220, 42, 46));
            _orb = new Entity(parent: this, model: "sphere", scaleUniform: 1.1f, position: new(0, 1.9f, 0), color: Color.White, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 4.3f, 0), color: Rgb(255, 115, 145))
            {
                BillboardText = "[E] Спелл-Карта Храма Хакурей (Наберите 25 очков Graze вплотную к пулям!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _orb.RotationY += 120f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _active);

            if (!_active && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _active = true;
                _grazeCount = 0;
                _prevHp = g.PlayerHp;
                SoundManager.Play("ult_cast", 1.0f, 1.2f);
                g.ShowBanner("НАЧАЛАСЬ ДУЭЛЬ СПЕЛЛ-КАРТ! СКОЛЬЗИТЕ РЯДОМ С ПУЛЯМИ (2.8М), НЕ ПОЛУЧАЯ УРОНА (0/25 GRAZE)!", Rgb(255, 115, 145), 4.2f);
            }
            else if (_active)
            {
                if (g.PlayerHp < _prevHp - 1f)
                {
                    _grazeCount = Math.Max(0, _grazeCount - 3);
                    _prevHp = g.PlayerHp;
                }
                else _prevHp = g.PlayerHp;

                _waveCd -= dt;
                if (_waveCd <= 0f)
                {
                    _waveCd = 0.95f;
                    float spin = (float)Raylib.GetTime() * 1.9f;
                    for (int i = 0; i < 12; i++)
                    {
                        float a = spin + (i / 12f) * MathF.PI * 2f;
                        Vector3 d = new(MathF.Cos(a), 0f, MathF.Sin(a));
                        new Projectile(Position + new Vector3(0, 1.35f, 0), d, 13.5f, 9f, 0.36f, i % 2 == 0 ? Color.Red : Color.White, isPlayer: false, maxLifetime: 4.0f);
                    }
                }

                Vector3 pCenter = g.PlayerPos + new Vector3(0, 1.2f, 0);
                foreach (var ent in Engine.AllEntities)
                {
                    if (ent is Projectile proj && !proj.IsDestroyed && !proj.IsPlayer && !_grazedProjectiles.Contains(proj))
                    {
                        float d = Vector3.Distance(proj.Position, pCenter);
                        if (d <= 2.85f && d > 0.75f)
                        {
                            _grazedProjectiles.Add(proj);
                            _grazeCount++;
                            SoundManager.Play("coin_ricoshot", 0.55f, 1.55f);
                            new VisualFX(pCenter, Color.White, 1.4f, 0.14f);
                        }
                    }
                }

                _labelEnt.BillboardText = $"Спелл-Карта Хакурей :: Набрано GRAZE: {_grazeCount} / 25";
                if (_grazeCount >= 25)
                {
                    g.UnlockAchievement("ach_char_reimu");
                    Destroy(this);
                }
            }
        }
    }

    // 7. СЁСТРЫ КОМЕЙДЗИ: Незримая Коиши на Периферии Зрения (исчезает при прямом взгляде, проявляется боковым зрением!)
    public class SubconsciousKoishiEncounter : Entity
    {
        private int _catches = 0;
        private readonly Entity _koishiVisual;
        private readonly Entity _labelEnt;

        public SubconsciousKoishiEncounter(Vector3 pos) : base(position: pos)
        {
            _koishiVisual = new Entity(parent: this);
            new Entity(parent: _koishiVisual, model: "sphere", scale: new(0.75f, 1.35f, 0.75f), position: new(0, 1.0f, 0), color: Rgb(95, 235, 165), unlit: true);
            new Entity(parent: _koishiVisual, model: "sphere", scale: new(1.35f, 0.22f, 1.35f), position: new(0, 1.75f, 0), color: Rgb(38, 42, 52));
            new Entity(parent: _koishiVisual, model: "diamond", scaleUniform: 0.45f, position: new(0, 1.15f, 0.45f), color: Rgb(255, 85, 155), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.55f, 0), color: Rgb(125, 255, 185));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            Vector3 toKoishi = Position - g.PlayerPos;
            float dist = toKoishi.Length();
            Vector3 flatToKoishi = new(toKoishi.X, 0f, toKoishi.Z);
            float dot = flatToKoishi.LengthSquared() > 0.01f
                ? Vector3.Dot(Vector3.Normalize(new Vector3(g.PlayerForward.X, 0f, g.PlayerForward.Z)), Vector3.Normalize(flatToKoishi))
                : 0f;

            // Если смотреть ПРЯМО на Коиши (dot > 0.62), она стирается из восприятия!
            bool inPeripheralVision = dot <= 0.62f;
            _koishiVisual.Enabled = inPeripheralVision && dist < 34f;
            _labelEnt.Enabled = !Program.IsSpectating && dist < 22f;
            _labelEnt.BillboardText = inPeripheralVision
                ? $"[E] Окликнуть Коиши на периферии сознания ({_catches}/3)"
                : "«...здесь никого нет, пока вы смотрите прямо...»\n[Отверните камеру вбок, чтобы увидеть её боковым зрением!]";

            if (inPeripheralVision && dist < 4.6f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _catches++;
                SoundManager.Play("ui_ready", 1.0f, 1.25f + _catches * 0.1f);
                new VisualFX(Position + new Vector3(0, 1.2f, 0), Rgb(255, 105, 175), 3.8f, 0.3f);
                if (_catches >= 3)
                {
                    g.UnlockAchievement("ach_char_komeiji");
                    Destroy(this);
                }
                else
                {
                    float a = _catches * 2.2f;
                    Position = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 14f, 0f, MathF.Sin(a) * 14f));
                    g.ShowBanner($"ВЫ ЗАМЕТИЛИ КОИШИ БОКОВЫМ ЗРЕНИЕМ ({_catches}/3)! ИЩИТЕ ЕЁ СНОВА НЕ ГЛЯДЯ В УПОР!", Rgb(125, 255, 185), 3.2f);
                }
            }
        }
    }

    // 8. ГОЛД ШИП: Полноценный забег по Ипподрому Трейсен против 5 ИИ-соперниц с рывком с последнего места!
    public class TracenDerbyRaceEncounter : Entity
    {
        private bool _racing = false;
        private int _playerCheckpoint = 0;
        private readonly List<(Entity Runner, float Angle, float Speed)> _rivals = new();
        private readonly List<Entity> _gates = new();
        private readonly Entity _labelEnt;
        private const float Rx = 32f;
        private const float Rz = 19f;

        public TracenDerbyRaceEncounter(Vector3 pos) : base(position: pos)
        {
            foreach (float sx in new[] { -1.8f, 1.8f })
                new Entity(parent: this, model: "cube", scale: new(0.38f, 3.8f, 0.38f), position: new(sx, 1.9f, 0), color: Color.White);
            new Entity(parent: this, model: "cube", scale: new(4.2f, 0.55f, 0.55f), position: new(0, 3.8f, 0), color: Rgb(225, 45, 65));
            _labelEnt = new Entity(parent: this, position: new(0, 4.7f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Стартовые Ворота Гран-При Трейсен (Обгоните 5 соперниц за полный круг!)"
            };
        }

        private static Vector3 TrackPos(float ang) => new(MathF.Cos(ang) * Rx, 0f, MathF.Sin(ang) * Rz);

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 22f || _racing);

            if (!_racing && dist < 5.0f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _racing = true;
                _playerCheckpoint = 0;
                for (int i = 0; i < 8; i++)
                {
                    float ga = MathF.PI + ((i + 1) / 8f) * MathF.PI * 2f;
                    Vector3 gp = TrackPos(ga);
                    var gate = new Entity(position: gp);
                    new Entity(parent: gate, model: "diamond", scale: new(3.2f, 0.25f, 3.2f), position: new(0, 0.25f, 0), color: i == 0 ? Color.Lime : Color.Gold, unlit: true);
                    gate.BillboardText = i == 7 ? "ФИНИШ #8!" : $"Створ #{i + 1}";
                    gate.Color = Color.Gold;
                    g.StageEntities.Add(gate);
                    _gates.Add(gate);
                }
                for (int r = 0; r < 5; r++)
                {
                    var rv = new Entity(position: TrackPos(MathF.PI + 0.18f + r * 0.08f));
                    new Entity(parent: rv, model: "sphere", scale: new(0.85f, 1.75f, 0.85f), position: new(0, 1.0f, 0), color: Rgb((byte)(180 + r * 14), 95, 165), unlit: true);
                    rv.BillboardText = $"Соперница #{r + 1}";
                    rv.Color = Color.Yellow;
                    g.StageEntities.Add(rv);
                    _rivals.Add((rv, MathF.PI + 0.18f + r * 0.08f, 0.36f + r * 0.025f));
                }
                CharacterUnlockSystems.RocinanteGallopTimer = 28f;
                SoundManager.Play("ult_cast", 1.1f, 1.15f);
                g.ShowBanner("СТАРТ ФИНАЛЬНОГО ДЕРБИ! ПРОБЕГИТЕ ВСЕ 8 СТВОРОВ ОВАЛА БЫСТРЕЕ 5 СОПЕРНИЦ!", Color.Gold, 4.2f);
            }
            else if (_racing)
            {
                bool rivalFinished = false;
                for (int i = 0; i < _rivals.Count; i++)
                {
                    var r = _rivals[i];
                    float newAng = r.Angle + r.Speed * dt;
                    r.Runner.Position = TrackPos(newAng);
                    _rivals[i] = (r.Runner, newAng, r.Speed);
                    if (newAng >= MathF.PI * 3f) rivalFinished = true;
                }

                if (_playerCheckpoint < _gates.Count)
                {
                    Vector3 targetGate = _gates[_playerCheckpoint].Position;
                    if (Physics.Distance2D(g.PlayerPos, targetGate) <= 5.5f)
                    {
                        SoundManager.Play("coin_ricoshot", 0.9f, 1.1f + _playerCheckpoint * 0.06f);
                        Destroy(_gates[_playerCheckpoint]);
                        _playerCheckpoint++;
                        if (_playerCheckpoint < _gates.Count) _gates[_playerCheckpoint].Color = Color.Lime;
                    }
                }

                _labelEnt.BillboardText = $"Финальное Дерби Трейсен: Створ {_playerCheckpoint}/8";
                if (_playerCheckpoint >= 8)
                {
                    foreach (var r in _rivals) if (IsValid(r.Runner)) Destroy(r.Runner);
                    g.UnlockAchievement("ach_char_gold_ship");
                    Destroy(this);
                }
                else if (rivalFinished)
                {
                    _racing = false;
                    foreach (var r in _rivals) if (IsValid(r.Runner)) Destroy(r.Runner);
                    foreach (var gt in _gates) if (IsValid(gt)) Destroy(gt);
                    _rivals.Clear();
                    _gates.Clear();
                    SoundManager.Play("ui_error");
                    g.ShowBanner("СОПЕРНИЦА ПРИШЛА ПЕРВОЙ! ПОДОЙДИТЕ К ВОРОТАМ [E] ДЛЯ РЕВАНША!", Color.Red, 3.4f);
                }
            }
        }
    }

    // 9. ДАЙТАКУ ГЕЛИОС: 128 BPM Ритм-Испытание на DJ-пульте «WEI~☆!»
    public class HeliosRhythmDjEncounter : Entity
    {
        public bool IsActive = false;
        public float BeatCursor = 0f;
        public int Combo = 0;

        private float _phaseTime = 0f;
        private readonly Entity _turntable;
        private readonly Entity _labelEnt;

        public HeliosRhythmDjEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(2.4f, 1.2f, 1.4f), position: new(0, 0.6f, 0), color: Rgb(28, 34, 58));
            _turntable = new Entity(parent: this, model: "diamond", scale: new(1.8f, 0.2f, 1.8f), position: new(0, 1.35f, 0), color: Rgb(85, 235, 255), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.85f, 0), color: Rgb(85, 235, 255))
            {
                BillboardText = "[E] Неоновый DJ-пульт Гелиос «WEI~☆!» (Ритм-комбо 8 битов подряд!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _turntable.RotationY += 180f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 16f || IsActive);

            if (!IsActive && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                IsActive = true;
                Combo = 0;
                _phaseTime = 0f;
                CharacterUnlockSystems.ActiveRhythmEncounter = this;
                SoundManager.Play("ui_ready", 1.0f, 1.25f);
                g.ShowBanner("♫ СЛУШАЙТЕ БИТ 128 BPM! НАЖИМАЙТЕ [E], КОГДА БЕГУНОК В ЗЕЛЁНОМ ЦЕНТРЕ (0/8)! ♫", Rgb(85, 235, 255), 3.8f);
                return;
            }

            if (!IsActive) return;
            if (dist > 14f)
            {
                IsActive = false;
                if (CharacterUnlockSystems.ActiveRhythmEncounter == this) CharacterUnlockSystems.ActiveRhythmEncounter = null;
                return;
            }

            _phaseTime += dt * 1.45f;
            BeatCursor = 0.5f + MathF.Sin(_phaseTime * MathF.PI) * 0.48f;

            if (Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                if (BeatCursor >= 0.38f && BeatCursor <= 0.62f)
                {
                    Combo++;
                    SoundManager.Play("coin_ricoshot", 1.0f, 1.0f + Combo * 0.08f);
                    new VisualFX(Position + new Vector3(0, 1.6f, 0), Rgb(85, 255, 175), 3.2f, 0.24f);
                    if (Combo >= 8)
                    {
                        IsActive = false;
                        if (CharacterUnlockSystems.ActiveRhythmEncounter == this) CharacterUnlockSystems.ActiveRhythmEncounter = null;
                        g.UnlockAchievement("ach_char_daitaku_helios");
                        Destroy(this);
                    }
                }
                else
                {
                    Combo = Math.Max(0, Combo - 1);
                    SoundManager.Play("ui_error", 0.85f, 0.9f);
                }
            }
        }
    }

    // 10. СЬЮЗИ: Шкала Напряжения TP от уклонения (Graze) + Контр-луч Rude Buster [E] в Рокочущего Рыцаря
    public class DarkSanctuaryRudeBusterEncounter : Entity
    {
        public bool IsActive = false;
        public float TpPercent = 0f;
        public int HitsLanded = 0;

        private float _volleyCd = 0f;
        private CombatTarget? _knight = null;
        private readonly HashSet<Entity> _grazed = new();
        private readonly Entity _fountainSpire;
        private readonly Entity _labelEnt;

        public DarkSanctuaryRudeBusterEncounter(Vector3 pos) : base(position: pos)
        {
            _fountainSpire = new Entity(parent: this, model: "diamond", scale: new(1.8f, 4.8f, 1.8f), position: new(0, 2.4f, 0), color: Rgb(185, 65, 245), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 5.2f, 0), color: Rgb(235, 85, 195))
            {
                BillboardText = "[E] Тёмный Фонтан (Зарядите 100% TP уклонением и 3 раза ударьте Рыцаря Rude Buster!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _fountainSpire.RotationY += 85f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || IsActive);

            if (!IsActive && dist < 4.8f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                IsActive = true;
                TpPercent = 25f;
                HitsLanded = 0;
                CharacterUnlockSystems.ActiveSusieEncounter = this;
                _knight = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, 9f), "roaring_knight", mult: 0.90f);
                SoundManager.Play("boss_roar", 1.15f, 0.85f);
                g.ShowBanner("УКЛОНЯЙТЕСЬ ВБЛИЗИ КЛИНКОВ ДЛЯ НАБОРА 100% TP И ЖМИТЕ [E] ДЛЯ ВЫСТРЕЛА RUDE BUSTER!", Rgb(235, 85, 195), 4.5f);
                return;
            }

            if (!IsActive) return;

            TpPercent = MathF.Min(100f, TpPercent + 7.5f * dt);
            _volleyCd -= dt;
            if (_volleyCd <= 0f && _knight != null && IsValid(_knight))
            {
                _volleyCd = 1.4f;
                for (int i = -2; i <= 2; i++)
                {
                    Vector3 toP = Vector3.Normalize((g.PlayerPos - _knight.Position) with { Y = 0f });
                    float ang = MathF.Atan2(toP.Z, toP.X) + i * 0.24f;
                    new Projectile(_knight.Position + new Vector3(0, 1.4f, 0), new Vector3(MathF.Cos(ang), 0, MathF.Sin(ang)), 16f, 11f, 0.38f, Rgb(185, 65, 255), isPlayer: false, maxLifetime: 3.5f);
                }
            }

            Vector3 pCenter = g.PlayerPos + new Vector3(0, 1.2f, 0);
            foreach (var ent in Engine.AllEntities)
            {
                if (ent is Projectile proj && !proj.IsDestroyed && !proj.IsPlayer && !_grazed.Contains(proj))
                {
                    if (Vector3.Distance(proj.Position, pCenter) <= 3.0f)
                    {
                        _grazed.Add(proj);
                        TpPercent = MathF.Min(100f, TpPercent + 18f);
                    }
                }
            }

            if (TpPercent >= 100f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract && _knight != null && IsValid(_knight))
            {
                TpPercent = 0f;
                HitsLanded++;
                SoundManager.Play("explosion", 1.15f, 1.05f);
                new LightningBeam(pCenter, _knight.Position + new Vector3(0, 1.5f, 0), Rgb(245, 75, 185), 0.45f);
                new VisualFX(_knight.Position + new Vector3(0, 1.5f, 0), Rgb(245, 75, 185), 6.5f, 0.35f);
                _knight.TakeDamage(_knight.MaxHp * 0.36f);
            }

            bool knightDefeated = _knight == null || !IsValid(_knight) || _knight.Hp <= 0f || _knight.IsAlly;
            _labelEnt.BillboardText = $"Тёмный Фонтан :: Rude Buster {HitsLanded}/3 | TP {(int)TpPercent}%";
            if (HitsLanded >= 3 || knightDefeated)
            {
                IsActive = false;
                if (CharacterUnlockSystems.ActiveSusieEncounter == this) CharacterUnlockSystems.ActiveSusieEncounter = null;
                if (_knight != null && IsValid(_knight) && !_knight.IsAlly) Destroy(_knight);
                g.UnlockAchievement("ach_char_susie");
                Destroy(this);
            }
        }
    }

    // 11. СПАМТОН G. СПАМТОН: Биржа [[КРОМЕРОВ]], покупка Пустого Диска на обвале цены + Корпус НЕО и шторм [Pipis]
    public class SpamtonKromerDealEncounter : Entity
    {
        private int _phase = 0; // 0 = биржа Кромеров, 1 = отнести диск в Корпус НЕО, 2 = отстрел [Pipis] 12с
        private int _kromerPrice = 150;
        private float _surviveTimer = 12f;
        private float _pipisCd = 0f;
        private readonly Entity _neoChassis;
        private readonly Entity _labelEnt;

        public SpamtonKromerDealEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(1.4f, 1.6f, 1.1f), position: new(0, 0.8f, 0), color: Rgb(38, 44, 52));
            new Entity(parent: this, model: "sphere", scaleUniform: 0.48f, position: new(-0.32f, 1.75f, 0.35f), color: Rgb(255, 95, 205), unlit: true);
            new Entity(parent: this, model: "sphere", scaleUniform: 0.48f, position: new(0.32f, 1.75f, 0.35f), color: Rgb(255, 235, 65), unlit: true);
            _neoChassis = new Entity(parent: this, model: "diamond", scale: new(1.8f, 3.2f, 1.2f), position: new(7.5f, 1.8f, 0), color: Rgb(145, 65, 195), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.95f, 0), color: Rgb(255, 235, 65));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 20f || _phase > 0);

            if (_phase == 0)
            {
                float wave = MathF.Sin((float)Raylib.GetTime() * 2.4f);
                _kromerPrice = Math.Max(15, (int)(115 + wave * 95f));
                bool cheap = _kromerPrice <= 75;
                _labelEnt.Color = cheap ? Color.Lime : Rgb(255, 95, 205);
                _labelEnt.BillboardText = $"[E] Телефон Помойки :: КУРС [[КРОМЕРА]]: ${_kromerPrice}\n{(cheap ? "ОБВАЛ ЦЕНЫ! ПОКУПАЙТЕ LoadedDisk СЕЙЧАС!" : "[Подождите падения курса <= $75!]")}";

                if (dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    if (g.Gold >= _kromerPrice)
                    {
                        g.Gold -= _kromerPrice;
                        _phase = 1;
                        SoundManager.Play("chest_open", 1.1f, 1.25f);
                        g.ShowBanner("СДЕЛКА ЗАКЛЮЧЕНА! ВСТАВЬТЕ ДИСК [E] В ПУСТОЙ КОРПУС НЕО СПРАВА!", Rgb(255, 235, 65), 3.8f);
                    }
                    else
                    {
                        SoundManager.Play("ui_error");
                        g.ShowBanner($"НЕДОСТАТОЧНО [[КРОМЕРОВ]] (НУЖНО ${_kromerPrice})!", Color.Red, 2.0f);
                    }
                }
            }
            else if (_phase == 1)
            {
                Vector3 neoWorld = Position + _neoChassis.Position;
                _labelEnt.Position = _neoChassis.Position + new Vector3(0, 2.2f, 0);
                _labelEnt.BillboardText = "[E] Вставить LoadedDisk в Пустой Корпус НЕО";
                if (Vector3.Distance(g.PlayerPos, neoWorld) < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    _phase = 2;
                    SoundManager.Play("ult_cast", 1.2f, 0.95f);
                    g.ShowBanner("★ ПЕРЕГРУЗКА [[PIPIS]]! ПРОДЕРЖИТЕСЬ 12 СЕКУНД ПОД ЗАЛПАМИ СИНИХ СФЕР! ★", Color.SkyBlue, 3.8f);
                }
            }
            else if (_phase == 2)
            {
                _surviveTimer -= dt;
                _pipisCd -= dt;
                if (_pipisCd <= 0f)
                {
                    _pipisCd = 1.05f;
                    Vector3 neoWorld = Position + _neoChassis.Position;
                    Vector3 dir = Vector3.Normalize((g.PlayerPos + new Vector3(0, 1.1f, 0)) - neoWorld);
                    new Projectile(neoWorld, dir, 15f, 12f, 0.65f, Rgb(48, 195, 255), isPlayer: false, maxLifetime: 3.5f);
                }
                _labelEnt.BillboardText = $"Шторм [[PIPIS]]: продержитесь {MathF.Max(0f, _surviveTimer):F1}с";
                if (_surviveTimer <= 0f)
                {
                    g.UnlockAchievement("ach_char_spamton");
                    Destroy(this);
                }
            }
        }
    }

    // 12. БИНА (АРБИТР ГЛАВЫ): Подавление Ядра Сефиры + 4 столпа Перегрева Клипота [E]
    public class BinahMeltdownEncounter : Entity
    {
        private bool _started = false;
        private int _activeMeltdownIdx = -1;
        private float _meltdownTimer = 10f;
        private CombatTarget? _binah = null;
        private readonly List<Entity> _pillars = new();
        private readonly Entity _labelEnt;

        public BinahMeltdownEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(1.8f, 3.0f, 1.8f), position: new(0, 1.5f, 0), color: Rgb(22, 20, 26));
            new Entity(parent: this, model: "diamond", scale: new(2.2f, 0.25f, 2.2f), position: new(0, 2.2f, 0), color: Color.Gold, unlit: true);
            for (int i = 0; i < 4; i++)
            {
                float a = i * MathF.PI * 0.5f + 0.78f;
                var p = new Entity(parent: this, model: "cube", scale: new(1.1f, 2.6f, 1.1f),
                    position: new(MathF.Cos(a) * 9.5f, 1.3f, MathF.Sin(a) * 9.5f), color: Rgb(45, 42, 52));
                _pillars.Add(p);
            }
            _labelEnt = new Entity(parent: this, position: new(0, 3.9f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Подавление Ядра Этажа Философии (Арбитр Бина + Столпы Перегрева Клипота!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _started);

            if (!_started && dist < 4.6f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _started = true;
                _activeMeltdownIdx = 0;
                _meltdownTimer = 11f;
                _binah = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, 8f), "binah_arbiter", mult: 0.90f);
                SoundManager.Play("boss_roar", 1.15f, 0.85f);
                g.ShowBanner("НАЧАЛОСЬ ПОДАВЛЕНИЕ АРБИТРА! СНИМАЙТЕ ПЕРЕГРЕВ КЛИПОТА [E] СО СВЕТЯЩИХСЯ СТОЛПОВ!", Color.Gold, 4.5f);
            }
            else if (_started)
            {
                if (_activeMeltdownIdx >= 0 && _activeMeltdownIdx < _pillars.Count)
                {
                    var mp = _pillars[_activeMeltdownIdx];
                    mp.Color = Color.Red;
                    Vector3 mpWorld = Position + mp.Position;
                    mp.BillboardText = $"[E] ПЕРЕГРЕВ КЛИПОТА: {_meltdownTimer:F1}с!";
                    _meltdownTimer -= dt;

                    if (Vector3.Distance(g.PlayerPos, mpWorld) < 3.8f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                    {
                        SoundManager.Play("parry", 1.0f, 1.2f);
                        mp.Color = Rgb(45, 42, 52);
                        mp.BillboardText = "";
                        if (_binah != null && IsValid(_binah)) _binah.ArmorBreakTimer = 6.0f;
                        _activeMeltdownIdx = (_activeMeltdownIdx + 1) % _pillars.Count;
                        _meltdownTimer = 12f;
                    }
                    else if (_meltdownTimer <= 0f)
                    {
                        CombatManager.DamagePlayer(22f);
                        mp.Color = Rgb(45, 42, 52);
                        mp.BillboardText = "";
                        _activeMeltdownIdx = (_activeMeltdownIdx + 1) % _pillars.Count;
                        _meltdownTimer = 12f;
                    }
                }

                _labelEnt.BillboardText = "Подавление Арбитра: Одолейте Бину, снимая Перегрев со столпов!";
                if (_binah == null || !IsValid(_binah) || _binah.Hp <= 0f || _binah.IsAlly)
                {
                    g.UnlockAchievement("ach_char_binah");
                    Destroy(this);
                }
            }
        }
    }

    // 13. ДОН КИХОТ: Рыцарский Турнир Верхом на Росинанте (таран 3 всадников на полном галопе!)
    public class RocinanteJoustEncounter : Entity
    {
        private bool _active = false;
        private int _jousted = 0;
        private readonly List<CombatTarget> _cavaliers = new();
        private readonly Entity _lanceBanner;
        private readonly Entity _labelEnt;

        public RocinanteJoustEncounter(Vector3 pos) : base(position: pos)
        {
            _lanceBanner = new Entity(parent: this, model: "diamond", scale: new(0.55f, 3.8f, 0.55f), position: new(0, 1.9f, 0), color: Color.Gold, unlit: true);
            new Entity(parent: this, model: "cube", scale: new(1.4f, 0.85f, 0.12f), position: new(0.7f, 3.1f, 0), color: Rgb(225, 38, 52), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 4.4f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Рыцарское Копьё «Мечта Ла-Манчи» (Сбейте с седла 3 кавалеристов на полном галопе!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _lanceBanner.RotationY += 75f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _active);

            if (!_active && dist < 4.6f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _active = true;
                CharacterUnlockSystems.RocinanteGallopTimer = 30.0f;
                for (int i = 0; i < 3; i++)
                {
                    float a = i * (MathF.PI * 2f / 3f);
                    _cavaliers.Add(CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(MathF.Cos(a) * 12f, 0, MathF.Sin(a) * 12f), "bison", elite: true));
                }
                SoundManager.Play("ult_cast", 1.15f, 1.15f);
                g.ShowBanner("ВПЕРЁД, РОСИНАНТ! ВЫ ПОЛУЧИЛИ ГАЛОП — ПРОТАРАНЬТЕ ВЛОБ 3 КАВАЛЕРИСТОВ НА БЕГУ ИЛИ РЫВКОМ [SHIFT]!", Color.Gold, 4.5f);
            }
            else if (_active)
            {
                CharacterUnlockSystems.RocinanteGallopTimer = MathF.Max(CharacterUnlockSystems.RocinanteGallopTimer, 2.0f);
                for (int i = _cavaliers.Count - 1; i >= 0; i--)
                {
                    var cv = _cavaliers[i];
                    if (!IsValid(cv) || cv.Hp <= 0f || cv.IsAlly)
                    {
                        _cavaliers.RemoveAt(i);
                        _jousted++;
                        continue;
                    }
                    if (Vector3.Distance(g.PlayerPos, cv.Position) <= 3.6f)
                    {
                        SoundManager.Play("explosion", 1.0f, 1.15f);
                        new VisualFX(cv.Position + new Vector3(0, 1.2f, 0), Color.Gold, 4.8f, 0.3f);
                        cv.TakeDamage(cv.MaxHp + 999f);
                        _cavaliers.RemoveAt(i);
                        _jousted++;
                    }
                }
                _labelEnt.BillboardText = $"Рыцарский Турнир Росинанта: сбито всадников {_jousted}/3";
                if (_jousted >= 3)
                {
                    CharacterUnlockSystems.RocinanteGallopTimer = 0f;
                    g.UnlockAchievement("ach_char_don_quixote");
                    Destroy(this);
                }
            }
        }
    }

    // 14. ПРИСТЕСС: Синхронизация 3 Якорей Саркофага ◇ + Победа над Истоком PRTS
    public class PriestessCoreEncounter : Entity
    {
        private bool _started = false;
        private int _anchorsDone = 0;
        private CombatTarget? _prtsBoss = null;
        private readonly List<Entity> _anchors = new();
        private readonly Entity _labelEnt;

        public PriestessCoreEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "diamond", scale: new(1.8f, 3.2f, 1.8f), position: new(0, 1.8f, 0), color: Rgb(255, 58, 82), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 4.1f, 0), color: Rgb(255, 85, 115))
            {
                BillboardText = "[E] Саркофаг «Начало Ассимилированной Вселенной» ◇"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _started);

            if (!_started && dist < 4.6f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _started = true;
                for (int i = 0; i < 3; i++)
                {
                    float a = i * (MathF.PI * 2f / 3f);
                    Vector3 ap = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 10f, 0, MathF.Sin(a) * 10f));
                    var anc = new Entity(position: ap);
                    new Entity(parent: anc, model: "diamond", scale: new(1.2f, 2.2f, 1.2f), position: new(0, 1.5f, 0), color: Rgb(255, 65, 95), unlit: true);
                    anc.BillboardText = $"[E] Якорь Памяти Саркофага ◇ #{i + 1}";
                    anc.Color = Rgb(255, 95, 125);
                    g.StageEntities.Add(anc);
                    _anchors.Add(anc);
                }
                _prtsBoss = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, 9f), "prts_source", mult: 0.90f);
                _prtsBoss.ShieldDroneProtected = true;
                g.ShowBanner("◇ АКТИВИРУЙТЕ 3 ЯКОРЯ ПАМЯТИ [E], ЧТОБЫ СНЯТЬ ЗАЩИТУ PRTS, И ОДОЛЕЙТЕ ИСТОК! ◇", Rgb(255, 85, 115), 4.5f);
            }
            else if (_started)
            {
                for (int i = _anchors.Count - 1; i >= 0; i--)
                {
                    if (Vector3.Distance(g.PlayerPos, _anchors[i].Position) < 4.0f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                    {
                        SoundManager.Play("ui_ready", 1.0f, 1.25f);
                        Destroy(_anchors[i]);
                        _anchors.RemoveAt(i);
                        _anchorsDone++;
                        if (_anchorsDone >= 3 && _prtsBoss != null && IsValid(_prtsBoss))
                        {
                            _prtsBoss.ShieldDroneProtected = false;
                            g.ShowBanner("ВСЕ 3 ЯКОРЯ СИНХРОНИЗИРОВАНЫ — ЗАЩИТА ИСТОКА PRTS СНЯТА!", Color.SkyBlue, 3.2f);
                        }
                    }
                }
                _labelEnt.BillboardText = $"Саркофаг PRTS: Якорей {_anchorsDone}/3 | Одолейте Исток PRTS";
                if (_prtsBoss == null || !IsValid(_prtsBoss) || _prtsBoss.Hp <= 0f || _prtsBoss.IsAlly)
                {
                    g.UnlockAchievement("ach_char_priestess");
                    Destroy(this);
                }
            }
        }
    }

    // 15. КЛОЖЕР: Инвестиция Золота в Полевую Мастерскую Родос Айленд + запуск союзного дрона Lancet-2!
    public class ClosureDroneShopEncounter : Entity
    {
        private int _tier = 0; // 0..3 апгрейда, затем оборона с союзным дроном
        private readonly List<CombatTarget> _raiders = new();
        private readonly Entity _droneModel;
        private readonly Entity _labelEnt;

        public ClosureDroneShopEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(2.4f, 1.2f, 1.6f), position: new(0, 0.6f, 0), color: Rgb(42, 52, 68));
            _droneModel = new Entity(parent: this, model: "sphere", scale: new(1.1f, 0.85f, 1.1f), position: new(0, 1.85f, 0), color: Rgb(85, 215, 255), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.1f, 0), color: Color.SkyBlue);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _droneModel.RotationY += 95f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _tier >= 3);

            if (_tier < 3)
            {
                int cost = 25 + _tier * 15;
                _labelEnt.BillboardText = $"[E] Лавка Инженера Кложер :: Апгрейд Дрона Lancet-2 ({_tier}/3) — ${cost}";
                if (dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    if (g.Gold >= cost)
                    {
                        g.Gold -= cost;
                        _tier++;
                        SoundManager.Play("chest_open", 1.0f, 1.1f + _tier * 0.1f);
                        new VisualFX(Position + new Vector3(0, 1.8f, 0), Color.SkyBlue, 3.2f, 0.25f);
                        if (_tier == 3)
                        {
                            var allyDrone = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, 2.5f), "hell_drone");
                            allyDrone.ConvertToAlly();
                            for (int i = 0; i < 4; i++)
                                _raiders.Add(CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3((i - 1.5f) * 5.5f, 0, 10f), "sarkaz_mercenary", elite: i == 0));
                            g.ShowBanner("ДРОН LANCET-2 ЗАПУЩЕН В БОЙ! ОТРАЗИТЕ НАЛЁТ МАРОДЁРОВ НА МАСТЕРСКУЮ КЛОЖЕР!", Color.SkyBlue, 4.0f);
                        }
                    }
                    else
                    {
                        SoundManager.Play("ui_error");
                        g.ShowBanner($"НЕДОСТАТОЧНО ЗОЛОТА ДЛЯ АПГРЕЙДА КЛОЖЕР (НУЖНО ${cost})!", Color.Red, 2.0f);
                    }
                }
            }
            else
            {
                _raiders.RemoveAll(r => !IsValid(r) || r.Hp <= 0f || r.IsAlly);
                _labelEnt.BillboardText = $"Испытание Lancet-2: Отразите рейдеров — осталось {_raiders.Count}";
                if (_raiders.Count == 0)
                {
                    g.UnlockAchievement("ach_char_closure");
                    Destroy(this);
                }
            }
        }
    }

    // 16. ЗАРЯНКА (ROBIN): Набор 5 нот по порядку софитов для разрушения Щита Хора Великого Септимия
    public class RobinChoirMelodyEncounter : Entity
    {
        private bool _started = false;
        private int _noteStep = 0;
        private CombatTarget? _septimus = null;
        private readonly List<Entity> _spotlights = new();
        private readonly Entity _labelEnt;

        public RobinChoirMelodyEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(0.25f, 2.8f, 0.25f), position: new(0, 1.4f, 0), color: Color.Gold);
            new Entity(parent: this, model: "sphere", scaleUniform: 0.55f, position: new(0, 2.95f, 0), color: Rgb(235, 195, 255), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.85f, 0), color: Rgb(235, 195, 255))
            {
                BillboardText = "[E] Золотой Микрофон Зарянки (Сыграйте 5 нот по порядку и сокрушите Великого Септимия!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _started);

            if (!_started && dist < 4.6f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _started = true;
                _noteStep = 0;
                Color[] noteCols = { Color.SkyBlue, Color.Lime, Color.Gold, Color.Orange, Color.Magenta };
                for (int i = 0; i < 5; i++)
                {
                    float a = -MathF.PI * 0.7f + (i / 4f) * MathF.PI * 1.4f;
                    Vector3 sp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 9.5f, 0, MathF.Sin(a) * 9.5f));
                    var spot = new Entity(position: sp);
                    new Entity(parent: spot, model: "diamond", scale: new(2.6f, 0.16f, 2.6f), position: new(0, 0.16f, 0), color: noteCols[i], unlit: true);
                    spot.BillboardText = $"Нота #{i + 1}";
                    spot.Color = noteCols[i];
                    g.StageEntities.Add(spot);
                    _spotlights.Add(spot);
                }
                _septimus = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, 10f), "great_septimus", mult: 0.88f);
                _septimus.ShieldDroneProtected = true;
                SoundManager.Play("ult_cast", 1.1f, 1.25f);
                g.ShowBanner("♫ НАСТУПИТЕ ПО ПОРЯДКУ НА СОФИТЫ НОТ 1 -> 2 -> 3 -> 4 -> 5, ЧТОБЫ РАЗБИТЬ ЩИТ СЕПТИМИЯ! ♫", Color.Gold, 4.5f);
            }
            else if (_started)
            {
                if (_noteStep < _spotlights.Count)
                {
                    var curSpot = _spotlights[_noteStep];
                    if (Physics.Distance2D(g.PlayerPos, curSpot.Position) <= 3.0f)
                    {
                        SoundManager.Play("coin_ricoshot", 1.0f, 0.95f + _noteStep * 0.14f);
                        new VisualFX(curSpot.Position + new Vector3(0, 1f, 0), Color.Gold, 3.8f, 0.28f);
                        Destroy(curSpot);
                        _noteStep++;
                        if (_noteStep >= 5 && _septimus != null && IsValid(_septimus))
                        {
                            _septimus.ShieldDroneProtected = false;
                            _septimus.StunTimer = 3.0f;
                            g.ShowBanner("★ АККОРД ГАРМОНИИ ПРОЗВУЧАЛ! ЩИТ ВЕЛИКОГО СЕПТИМИЯ РАЗРУШЕН! ★", Rgb(235, 195, 255), 3.8f);
                        }
                    }
                }
                _labelEnt.BillboardText = _noteStep < 5
                    ? $"Концерт Пенаконии: Сыграйте Ноту #{_noteStep + 1}/5"
                    : "Финал Концерта: Победите Великого Септимия!";
                if (_septimus == null || !IsValid(_septimus) || _septimus.Hp <= 0f || _septimus.IsAlly)
                {
                    for (int i = _noteStep; i < _spotlights.Count; i++) if (IsValid(_spotlights[i])) Destroy(_spotlights[i]);
                    g.UnlockAchievement("ach_char_robin");
                    Destroy(this);
                }
            }
        }
    }

    // 17. ЛОЖНЫЙ СЫН: Наведение Небесной Золотой Молнии на 3 Жеоды Аурелионита + бой с Ложным Сыном
    public class PrimeMeridianStormEncounter : Entity
    {
        private bool _started = false;
        private float _lightningTimer = 3.0f;
        private Vector3 _strikeTarget = Vector3.Zero;
        private bool _telegraphing = false;
        private CombatTarget? _falseSon = null;
        private readonly List<Entity> _geodes = new();
        private readonly Entity _strikeRing;
        private readonly Entity _labelEnt;

        public PrimeMeridianStormEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "diamond", scale: new(1.8f, 3.8f, 1.8f), position: new(0, 1.9f, 0), color: Color.Gold, unlit: true);
            _strikeRing = new Entity(model: "diamond", scale: new(5.2f, 0.16f, 5.2f), color: Color.Gold, unlit: true, enabled: false);
            _labelEnt = new Entity(parent: this, position: new(0, 4.4f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Алтарь Главного Меридиана (Наведите Золотую Молнию на 3 Жеоды и одолейте Ложного Сына!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _started);

            if (!_started && dist < 4.6f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _started = true;
                g.StageEntities.Add(_strikeRing);
                for (int i = 0; i < 3; i++)
                {
                    float a = i * (MathF.PI * 2f / 3f);
                    Vector3 gp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 10.5f, 0, MathF.Sin(a) * 10.5f));
                    var geo = new Entity(position: gp);
                    new Entity(parent: geo, model: "diamond", scale: new(1.6f, 2.8f, 1.6f), position: new(0, 1.4f, 0), color: Color.Gold, unlit: true);
                    geo.BillboardText = $"Осквернённая Жеода #{i + 1}\n[Приманите сюда Золотую Молнию!]";
                    geo.Color = Color.Gold;
                    g.StageEntities.Add(geo);
                    _geodes.Add(geo);
                }
                _falseSon = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, 9f), "false_son_boss", mult: 0.90f);
                _falseSon.ShieldDroneProtected = true;
                g.ShowBanner("⚡ ВСТАНЬТЕ РЯДОМ С ЖЕОДАМИ АУРЕЛИОНИТА, ЧТОБЫ НЕБЕСНАЯ МОЛНИЯ РАЗБИЛА ИХ БРОНЮ! ⚡", Color.Gold, 4.5f);
            }
            else if (_started)
            {
                if (_geodes.Count > 0)
                {
                    _lightningTimer -= dt;
                    if (!_telegraphing && _lightningTimer <= 1.5f)
                    {
                        _telegraphing = true;
                        _strikeTarget = g.PlayerPos;
                        _strikeRing.Position = _strikeTarget + new Vector3(0, 0.18f, 0);
                        _strikeRing.Enabled = true;
                    }
                    if (_lightningTimer <= 0f)
                    {
                        _lightningTimer = 3.8f;
                        _telegraphing = false;
                        _strikeRing.Enabled = false;
                        SoundManager.Play("zap", 1.2f, 0.85f);
                        new LightningBeam(_strikeTarget + new Vector3(0, 22f, 0), _strikeTarget, Color.Gold, 0.45f);
                        new VisualFX(_strikeTarget + new Vector3(0, 1f, 0), Color.Gold, 5.5f, 0.35f);

                        for (int i = _geodes.Count - 1; i >= 0; i--)
                        {
                            if (Physics.Distance2D(_geodes[i].Position, _strikeTarget) <= 5.2f)
                            {
                                Destroy(_geodes[i]);
                                _geodes.RemoveAt(i);
                                if (_geodes.Count == 0 && _falseSon != null && IsValid(_falseSon))
                                {
                                    _falseSon.ShieldDroneProtected = false;
                                    g.ShowBanner("ВСЕ 3 ЖЕОДЫ ОЧИЩЕНЫ МОЛНИЕЙ — ЛОЖНЫЙ СЫН УЯЗВИМ!", Color.SkyBlue, 3.5f);
                                }
                            }
                        }
                    }
                }

                _labelEnt.BillboardText = _geodes.Count > 0
                    ? $"Буря Меридиана: Жеод осталось {_geodes.Count}/3"
                    : "Очищение Провидения: Одолейте Ложного Сына!";
                if (_falseSon == null || !IsValid(_falseSon) || _falseSon.Hp <= 0f || _falseSon.IsAlly)
                {
                    if (IsValid(_strikeRing)) Destroy(_strikeRing);
                    g.UnlockAchievement("ach_char_false_son");
                    Destroy(this);
                }
            }
        }
    }
}
