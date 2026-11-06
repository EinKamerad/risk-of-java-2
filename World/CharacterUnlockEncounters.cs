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
    public static class CharacterUnlockEncounters
    {
        public static bool DetectPlayerAttackNear(IPlayerContext g, Vector3 targetPos, float hitRadius, out Vector3 pushDir)
        {
            pushDir = g.PlayerForward.LengthSquared() > 0.01f ? Vector3.Normalize(g.PlayerForward) : Vector3.UnitZ;
            foreach (var ent in Engine.AllEntities)
            {
                if (ent is Projectile proj && !proj.IsDestroyed && proj.IsPlayer && Vector3.Distance(proj.Position, targetPos) <= hitRadius + 0.65f)
                {
                    if (proj.Direction.LengthSquared() > 0.001f)
                        pushDir = Vector3.Normalize(new Vector3(proj.Direction.X, 0f, proj.Direction.Z));
                    if (!proj.Pierce) proj.SafeDestroy();
                    return true;
                }
            }
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Vector3.Distance(g.PlayerPos + new Vector3(0, 1.1f, 0), targetPos) <= hitRadius + 3.4f)
            {
                Vector3 toTarget = targetPos - (g.PlayerPos + new Vector3(0, 1.1f, 0));
                if (toTarget.LengthSquared() > 0.01f && Vector3.Dot(Vector3.Normalize(toTarget), g.PlayerForward) >= 0.25f)
                {
                    Vector3 flat = new(toTarget.X, 0f, toTarget.Z);
                    if (flat.LengthSquared() > 0.001f) pushDir = Vector3.Normalize(flat);
                    return true;
                }
            }
            return false;
        }

        public static bool DetectPlayerAttackNear(IPlayerContext g, Vector3 targetPos, float hitRadius)
            => DetectPlayerAttackNear(g, targetPos, hitRadius, out _);

        public static CombatTarget SpawnEncounterEnemy(IPlayerContext g, Vector3 pos, string enemyType, bool elite = false, float mult = 1.0f)
        {
            float coeff = (g is GameRunContext grc ? grc.DifficultyCoeff : 1.3f) * mult;
            Vector3 sp = Physics.FindSafeSpawnPoint(pos, 10f, 1.4f, 3.0f);
            var mob = new CombatTarget(sp, enemyType, coeff);
            if (elite)
            {
                mob.IsElite = true;
                mob.MaxHp *= 1.35f;
                mob.Hp = mob.MaxHp;
                mob.UpdateHpBillboard();
            }
            CombatManager.Enemies.Add(mob);
            g.StageEntities.Add(mob);
            return mob;
        }
    }

    // 1. АРИ: Дикая Духовная Лисица Васитаи (гуляет между 3 норами; атака пугает, нужно догнать и погладить [E] 3 раза)
    public class WildSpiritFoxEncounter : Entity
    {
        private int _pets = 0;
        private float _fleeTimer = 0f;
        private Vector3 _goalPos;
        private readonly Entity _foxRoot;
        private readonly Entity _labelEnt;

        public WildSpiritFoxEncounter(Vector3 pos) : base(position: pos)
        {
            _foxRoot = new Entity(parent: this);
            new Entity(parent: _foxRoot, model: "sphere", scale: new(0.65f, 0.52f, 1.05f), position: new(0, 0.55f, 0), color: Rgb(255, 235, 248), unlit: true);
            for (int t = -1; t <= 1; t++)
                new Entity(parent: _foxRoot, model: "diamond", scale: new(0.32f, 0.32f, 0.95f), position: new(t * 0.25f, 0.72f, -0.75f), color: Rgb(255, 115, 205), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.0f, 0), color: Rgb(255, 130, 215))
            {
                BillboardText = "[E] Погладить Дикую Духовную Лисицу (0/3)\n[НЕ атакуйте её — догоняйте на бегу!]"
            };
            _goalPos = pos;
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && dist < 20f;
            _fleeTimer = MathF.Max(0f, _fleeTimer - dt);

            if (CharacterUnlockEncounters.DetectPlayerAttackNear(g, Position + new Vector3(0, 0.7f, 0), 2.5f))
            {
                _pets = 0;
                _fleeTimer = 2.6f;
                Vector3 away = Vector3.Normalize(new Vector3(Position.X - g.PlayerPos.X + 0.1f, 0f, Position.Z - g.PlayerPos.Z + 0.1f));
                _goalPos = Physics.FindSafeSpawnPoint(Position + away * 22f, 14f, 1.2f, 2.5f);
                SoundManager.Play("ui_error");
                g.ShowBanner("ВЫ НАПУГАЛИ ЛИСИЦУ АТАКОЙ! ДОВЕРИЕ СБРОШЕНО (0/3)!", Color.Red, 2.4f);
            }

            if (Physics.Distance2D(Position, _goalPos) > 1.2f)
            {
                Vector3 dir = Vector3.Normalize(new Vector3(_goalPos.X - X, 0f, _goalPos.Z - Z));
                float spd = _fleeTimer > 0f ? 15.5f : 9.2f;
                Vector3 np = Physics.ResolveHorizontalCollision(Position + dir * spd * dt, 0.8f, Y);
                Position = new Vector3(np.X, Physics.GetSurfaceHeight(np.X, np.Z, Y + 1.5f), np.Z);
                LookAt2D(_goalPos);
            }
            else if (dist < 8.5f && _pets > 0 && _fleeTimer <= 0f && Random.Shared.NextDouble() < 0.03)
            {
                float a = (float)Random.Shared.NextDouble() * MathF.PI * 2f;
                _goalPos = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 16f, 0f, MathF.Sin(a) * 16f));
            }

            if (dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _pets++;
                SoundManager.Play("charm_heart", 1.0f, 1.0f + _pets * 0.14f);
                new VisualFX(Position + new Vector3(0, 1.0f, 0), Rgb(255, 115, 205), 3.6f, 0.3f);
                if (_pets >= 3)
                {
                    g.UnlockAchievement("ach_char_ahri");
                    Destroy(this);
                    return;
                }
                float ang = (float)Random.Shared.NextDouble() * MathF.PI * 2f;
                _goalPos = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(ang) * 19f, 0f, MathF.Sin(ang) * 19f));
                _labelEnt.BillboardText = $"[E] Погладить Дикую Духовную Лисицу ({_pets}/3)";
                g.ShowBanner($"ЛИСИЦА ИГРАЕТ С ВАМИ ({_pets}/3)! ДОГОНИТЕ ЕЁ СНОВА!", Rgb(255, 125, 215), 2.2f);
            }
        }
    }

    // 2. ЧЕБУРАШКА: Мирный Ящик с Апельсинами + Гармошка Крокодила Гены неподалёку (атака ломает ящик!)
    public class PeacefulOrangeCrateEncounter : Entity
    {
        private bool _hasHarmonica = false;
        private bool _ruined = false;
        private readonly Entity? _harmonicaEnt;
        private readonly Entity _crateMesh;
        private readonly Entity _labelEnt;

        public PeacefulOrangeCrateEncounter(Vector3 pos, Random rng) : base(position: pos)
        {
            _crateMesh = new Entity(parent: this, model: "cube", scale: new(1.9f, 1.5f, 1.9f), position: new(0, 0.75f, 0), color: Rgb(185, 122, 62));
            new Entity(parent: _crateMesh, model: "sphere", scaleUniform: 0.48f, position: new(0, 0.65f, 0), color: Color.Orange, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.6f, 0), color: Color.Orange)
            {
                BillboardText = "Покачивающийся Ящик с Апельсинами\n[НЕ ломайте его! Найдите рядом Гармошку Гены и сыграйте [E]!]"
            };

            float a = (float)rng.NextDouble() * MathF.PI * 2f;
            Vector3 hp = Physics.FindSafeSpawnPoint(pos + new Vector3(MathF.Cos(a) * 19f, 0f, MathF.Sin(a) * 19f));
            _harmonicaEnt = new Entity(position: hp);
            new Entity(parent: _harmonicaEnt, model: "cube", scale: new(0.95f, 0.65f, 0.45f), position: new(0, 0.8f, 0), color: Rgb(65, 215, 95), unlit: true);
            _harmonicaEnt.BillboardText = "[E] Взять Гармошку Крокодила Гены ♫";
            _harmonicaEnt.Color = Color.Lime;
        }

        public override void OnDestroy()
        {
            if (_harmonicaEnt != null && !_harmonicaEnt.IsDestroyed)
                Destroy(_harmonicaEnt);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || _ruined || IsDestroyed) return;
            _crateMesh.RotationZ = MathF.Sin((float)Raylib.GetTime() * 6.5f) * 6.5f;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && dist < 15f;

            if (CharacterUnlockEncounters.DetectPlayerAttackNear(g, Position + new Vector3(0, 0.8f, 0), 2.5f))
            {
                _ruined = true;
                if (_harmonicaEnt != null && !_harmonicaEnt.IsDestroyed) Destroy(_harmonicaEnt);
                SoundManager.Play("explosion", 0.9f, 1.1f);
                new VisualFX(Position + new Vector3(0, 0.8f, 0), Color.Orange, 4.5f, 0.35f);
                g.ShowBanner("ВЫ РАЗБИЛИ ЯЩИК АТАКОЙ! ЧЕБУРАШКА ИСПУГАЛСЯ И СПРЯТАЛСЯ ДО СЛЕДУЮЩЕГО ЭТАПА!", Color.Red, 3.2f);
                Destroy(this);
                return;
            }

            if (!_hasHarmonica && _harmonicaEnt != null && IsValid(_harmonicaEnt))
            {
                if (Vector3.Distance(g.PlayerPos, _harmonicaEnt.Position) < 4.0f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    _hasHarmonica = true;
                    SoundManager.Play("magic_cast", 1.0f, 1.15f);
                    Destroy(_harmonicaEnt);
                    _labelEnt.BillboardText = "[E] Сыграть на Гармошке Гены у Ящика с Апельсинами ♫";
                    g.ShowBanner("ГАРМОШКА НАЙДЕНА! ВЕРНИТЕСЬ К ЯЩИКУ С АПЕЛЬСИНАМИ И СЫГРАЙТЕ [E]!", Color.Lime, 3.0f);
                }
            }
            else if (_hasHarmonica && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                SoundManager.Play("victory", 1.0f, 1.08f);
                new VisualFX(Position + new Vector3(0, 1.2f, 0), Color.Orange, 6.0f, 0.4f);
                g.UnlockAchievement("ach_char_cheburashka");
                Destroy(this);
            }
        }
    }

    // 3. W (ARKNIGHTS): Заминированный Красный Сундук-Мимик (отфутболить 3 тикающие бомбы обратно в укрытие W!)
    public class MimicChestAmbushEncounter : Entity
    {
        private bool _sprung = false;
        private int _bombsReturned = 0;
        private float _spawnBombTimer = 0.6f;
        private readonly Entity _perchTarget;
        private readonly List<(Entity Bomb, Vector3 Vel, bool Kicked)> _bombs = new();
        private readonly Entity _labelEnt;

        public MimicChestAmbushEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(1.8f, 1.15f, 1.25f), position: new(0, 0.58f, 0), color: Rgb(195, 34, 48));
            new Entity(parent: this, model: "cube", scale: new(1.85f, 0.22f, 1.30f), position: new(0, 1.15f, 0), color: Color.Gold);
            _perchTarget = new Entity(parent: this, position: new(0, 4.2f, 9.5f), enabled: false);
            new Entity(parent: _perchTarget, model: "diamond", scale: new(2.2f, 2.8f, 2.2f), color: Rgb(255, 55, 75), unlit: true);
            _perchTarget.BillboardText = "УКРЫТИЕ СНАЙПЕРШИ W [3/3 БОМБ]";
            _perchTarget.Color = Color.Red;
            _labelEnt = new Entity(parent: this, position: new(0, 2.4f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Подозрительный Легендарный Сундук ($0?!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 14f || _sprung);

            if (!_sprung && dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _sprung = true;
                _perchTarget.Enabled = true;
                SoundManager.Play("explosion", 1.1f, 1.15f);
                new VisualFX(Position + new Vector3(0, 1.2f, 0), Color.Red, 6.5f, 0.38f);
                g.ShowBanner("«ХА-ХА, ПОПАЛСЯ!» — ОТФУТБОЛЬТЕ [E / УДАР] 3 БОМБЫ ОБРАТНО В УКРЫТИЕ W!", Color.Red, 4.0f);
            }
            else if (_sprung)
            {
                Vector3 perchWorld = Position + _perchTarget.Position;
                _labelEnt.BillboardText = $"Засада W: Отбито тикающих бомб в укрытие ({_bombsReturned}/3)";
                _spawnBombTimer -= dt;
                if (_spawnBombTimer <= 0f && _bombs.Count < 2)
                {
                    _spawnBombTimer = 2.4f;
                    var b = new Entity(position: perchWorld);
                    new Entity(parent: b, model: "sphere", scaleUniform: 0.78f, color: Color.Red, unlit: true);
                    b.BillboardText = "[E / УДАР] ОТБИТЬ БОМБУ!";
                    b.Color = Color.Orange;
                    Vector3 toPlayer = Vector3.Normalize((g.PlayerPos + new Vector3(0, 0.8f, 0)) - perchWorld);
                    _bombs.Add((b, toPlayer * 11.5f, false));
                }

                for (int i = _bombs.Count - 1; i >= 0; i--)
                {
                    var item = _bombs[i];
                    if (!IsValid(item.Bomb)) { _bombs.RemoveAt(i); continue; }
                    item.Bomb.Position += item.Vel * dt;
                    float fy = Physics.GetSurfaceHeight(item.Bomb.X, item.Bomb.Z, item.Bomb.Y + 1.5f);
                    if (!item.Kicked && item.Bomb.Y <= fy + 0.55f)
                    {
                        item.Bomb.Y = fy + 0.55f;
                        item.Vel = Vector3.Zero;
                    }

                    if (!item.Kicked && (Vector3.Distance(g.PlayerPos, item.Bomb.Position) <= 3.6f && Raylib.IsKeyPressed(KeyboardKey.E)
                        || CharacterUnlockEncounters.DetectPlayerAttackNear(g, item.Bomb.Position, 2.6f)))
                    {
                        SoundManager.Play("parry", 1.0f, 1.2f);
                        Vector3 backDir = Vector3.Normalize(perchWorld - item.Bomb.Position);
                        _bombs[i] = (item.Bomb, backDir * 28f, true);
                        continue;
                    }

                    if (item.Kicked && Vector3.Distance(item.Bomb.Position, perchWorld) <= 2.4f)
                    {
                        _bombsReturned++;
                        SoundManager.Play("explosion", 1.05f, 1.05f);
                        new VisualFX(perchWorld, Color.Orange, 5.5f, 0.35f);
                        Destroy(item.Bomb);
                        _bombs.RemoveAt(i);
                        if (_bombsReturned >= 3)
                        {
                            foreach (var rem in _bombs) if (IsValid(rem.Bomb)) Destroy(rem.Bomb);
                            g.UnlockAchievement("ach_char_w_arknights");
                            Destroy(this);
                            return;
                        }
                    }
                    else
                    {
                        _bombs[i] = item;
                    }
                }
            }
        }
    }

    // 4. ЛАППЛАНД: Купол Безмолвия Сиракуз (защитить курьера, сражаясь только [ЛКМ] без M2/Shift/R!)
    public class SiracusaSilenceAmbushEncounter : Entity
    {
        private bool _active = false;
        private readonly List<CombatTarget> _cleaners = new();
        private readonly Entity _domeRing;
        private readonly Entity _labelEnt;

        public SiracusaSilenceAmbushEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(1.2f, 1.8f, 1.2f), position: new(0, 0.9f, 0), color: Rgb(55, 165, 245));
            _domeRing = new Entity(parent: this, model: "sphere", scale: new(24f, 0.12f, 24f), position: new(0, 0.12f, 0), color: Rgb(195, 75, 255, 70), unlit: true, enabled: false);
            _labelEnt = new Entity(parent: this, position: new(0, 3.0f, 0), color: Rgb(215, 115, 255))
            {
                BillboardText = "[E] Загнаный Курьер Penguin Logistics\n[Примите бой внутри Купола Безмолвия Сиракуз!]"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 15f || _active);

            if (!_active && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _active = true;
                _domeRing.Enabled = true;
                for (int i = 0; i < 4; i++)
                {
                    float a = i * (MathF.PI * 0.5f);
                    _cleaners.Add(CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(MathF.Cos(a) * 8.5f, 0f, MathF.Sin(a) * 8.5f), "sarkaz_mercenary", elite: i < 2));
                }
                SoundManager.Play("boss_roar", 1.0f, 1.2f);
                g.ShowBanner("АУРА БЕЗМОЛВИЯ АКТИВИРОВАНА! НАВЫКИ ЗАБЛОКИРОВАНЫ — СОКРУШИТЕ САРКАЗОВ ОРУЖИЕМ [ЛКМ]!", Rgb(215, 95, 255), 4.0f);
            }
            else if (_active)
            {
                if (dist <= 15f)
                    CharacterUnlockSystems.SilenceDomeTimer = 0.25f;
                _cleaners.RemoveAll(c => !IsValid(c) || c.Hp <= 0f || c.IsAlly);
                _labelEnt.BillboardText = $"Купол Безмолвия Сиракуз: осталось Чистильщиков {_cleaners.Count}/4";
                if (_cleaners.Count == 0)
                {
                    g.UnlockAchievement("ach_char_lappland");
                    Destroy(this);
                }
            }
        }
    }

    // 5. ГВЕН: Движущийся круг Священного Тумана сквозь сжимающийся Чёрный Туман к 3 Золотым Иглам
    public class IsoldeShrinkingMistEncounter : Entity
    {
        private bool _active = false;
        private int _needles = 0;
        private float _orbitAng = 0f;
        private CombatTarget? _reaper = null;
        private readonly Entity _safeZone;
        private readonly List<Entity> _needleEnts = new();
        private readonly Entity _labelEnt;

        public IsoldeShrinkingMistEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "diamond", scale: new(0.8f, 2.2f, 0.35f), position: new(0, 1.3f, 0), rotation: new(0, 0, 22f), color: Rgb(95, 235, 255), unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(0.8f, 2.2f, 0.35f), position: new(0, 1.3f, 0), rotation: new(0, 0, -22f), color: Rgb(95, 235, 255), unlit: true);
            _safeZone = new Entity(parent: this, model: "sphere", scale: new(9.0f, 0.12f, 9.0f), position: new(0, 0.15f, 0), color: Rgb(85, 235, 255, 95), unlit: true, enabled: false);
            _labelEnt = new Entity(parent: this, position: new(0, 3.2f, 0), color: Rgb(95, 235, 255))
            {
                BillboardText = "[E] Золотые Ножницы Изольды (Священный Туман)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 15f || _active);

            if (!_active && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _active = true;
                _safeZone.Enabled = true;
                for (int i = 0; i < 3; i++)
                {
                    float a = i * (MathF.PI * 2f / 3f);
                    Vector3 np = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 10.5f, 0f, MathF.Sin(a) * 10.5f));
                    var nd = new Entity(position: np);
                    new Entity(parent: nd, model: "diamond", scale: new(0.45f, 2.2f, 0.45f), position: new(0, 1.3f, 0), color: Color.Gold, unlit: true);
                    nd.BillboardText = $"[E] Золотая Игла #{i + 1}/3";
                    nd.Color = Color.Gold;
                    _needleEnts.Add(nd);
                }
                SoundManager.Play("scissor_snip", 1.0f, 1.05f);
                g.ShowBanner("ИДИТЕ ВНУТРИ ДВИЖУЩЕГОСЯ ЛАЗУРНОГО КРУГА ТУМАНА И СОБЕРИТЕ [E] 3 ЗОЛОТЫЕ ИГЛЫ!", Rgb(95, 235, 255), 4.0f);
            }
            else if (_active)
            {
                _orbitAng += 0.85f * dt;
                Vector3 safeOffset = _needles < 3 ? new Vector3(MathF.Cos(_orbitAng) * 10.5f, 0.15f, MathF.Sin(_orbitAng) * 10.5f) : new Vector3(0, 0.15f, 0);
                _safeZone.Position = safeOffset;
                Vector3 safeWorld = Position + safeOffset;

                if (dist <= 18f && Physics.Distance2D(g.PlayerPos, safeWorld) > 5.2f)
                    CombatManager.DamagePlayer(5.5f * dt);

                for (int i = _needleEnts.Count - 1; i >= 0; i--)
                {
                    var nd = _needleEnts[i];
                    if (Vector3.Distance(g.PlayerPos, nd.Position) < 4.0f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                    {
                        _needles++;
                        SoundManager.Play("scissor_snip", 1.0f, 1.2f);
                        Destroy(nd);
                        _needleEnts.RemoveAt(i);
                        if (_needles == 3)
                        {
                            _reaper = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, -8f), "hollow_shadow", elite: true, mult: 2.2f);
                            g.ShowBanner("ИГЛЫ СОБРАНЫ! ИЗГОНИТЕ СТРАЖА ЧЁРНОГО ТУМАНА!", Rgb(95, 235, 255), 3.2f);
                        }
                    }
                }

                _labelEnt.BillboardText = _needles < 3
                    ? $"Священный Туман: собрано Золотых Игл {_needles}/3 (Держитесь в лазурном круге!)"
                    : "Добейте Стража Чёрного Тумана!";
                if (_needles >= 3 && (_reaper == null || !IsValid(_reaper) || _reaper.Hp <= 0f || _reaper.IsAlly))
                {
                    g.UnlockAchievement("ach_char_gwen");
                    Destroy(this);
                }
            }
        }
    }

    // 6. РАЗВЕДЧИК DRG: Бросок осветительных флаеров [E] для поиска скрытых жил Моркита + M.U.L.E. + Капсула (15с)
    public class DeepRockMorkiteEncounter : Entity
    {
        private int _phase = 0; // 0 = старт, 1 = поиск руды флаерами, 2 = посадка в капсулу 15с
        private int _mined = 0;
        private float _escapeTimer = 15.0f;
        private readonly List<(Entity Vein, bool Lit)> _veins = new();
        private Entity? _dropPod;
        private readonly Entity _labelEnt;

        public DeepRockMorkiteEncounter(Vector3 pos, Random rng) : base(position: pos)
        {
            // Шагающий мул M.U.L.E. (Молли)
            new Entity(parent: this, model: "cube", scale: new(1.6f, 1.3f, 1.6f), position: new(0, 0.9f, 0), color: Rgb(225, 145, 35));
            new Entity(parent: this, model: "sphere", scaleUniform: 0.45f, position: new(0, 1.65f, 0.55f), color: Color.SkyBlue, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.7f, 0), color: Color.SkyBlue)
            {
                BillboardText = "[E] Грузовой Мул M.U.L.E. «Молли» (Добыча Моркита DRG)"
            };

            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.09f + (float)rng.NextDouble() * 0.5f;
                Vector3 vp = Physics.FindSafeSpawnPoint(pos + new Vector3(MathF.Cos(a) * 14f, 0f, MathF.Sin(a) * 14f));
                var v = new Entity(position: vp, enabled: false);
                new Entity(parent: v, model: "diamond", scale: new(1.3f, 1.9f, 1.3f), position: new(0, 0.95f, 0), color: Rgb(45, 225, 235), unlit: true);
                v.BillboardText = "Жила Моркита [Разбейте ударом!]";
                v.Color = Color.SkyBlue;
                _veins.Add((v, false));
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 16f || _phase > 0);

            if (_phase == 0 && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _phase = 1;
                SoundManager.Play("ui_ready");
                g.ShowBanner("ROCK AND STONE! ЖМИТЕ [E], ЧТОБЫ БРОСАТЬ ОСВЕТИТЕЛЬНЫЕ ФЛАЕРЫ И НАЙТИ 3 ЖИЛЫ МОРКИТА!", Color.SkyBlue, 4.2f);
            }
            else if (_phase == 1)
            {
                _labelEnt.BillboardText = $"Добыто Моркита: {_mined}/3 — Жмите [E] на ходу, чтобы бросить Флаер и подсветить руду!";
                if (Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    Vector3 flarePos = g.PlayerPos + g.PlayerForward * 5.0f;
                    new VisualFX(flarePos + new Vector3(0, 0.8f, 0), Color.Yellow, 10.5f, 0.55f);
                    SoundManager.Play("coin_flip", 0.9f, 0.85f);
                    for (int i = 0; i < _veins.Count; i++)
                    {
                        if (!_veins[i].Lit && IsValid(_veins[i].Vein) && Vector3.Distance(flarePos, _veins[i].Vein.Position) <= 13.5f)
                        {
                            _veins[i].Vein.Enabled = true;
                            _veins[i] = (_veins[i].Vein, true);
                            g.ShowBanner("ФЛАЕР ВЫСВЕТИЛ ЖИЛУ МОРКИТА! РАЗБЕЙТЕ ЕЁ ОРУЖИЕМ!", Color.Lime, 2.2f);
                        }
                    }
                }

                for (int i = _veins.Count - 1; i >= 0; i--)
                {
                    if (_veins[i].Lit && IsValid(_veins[i].Vein)
                        && CharacterUnlockEncounters.DetectPlayerAttackNear(g, _veins[i].Vein.Position + new Vector3(0, 1.0f, 0), 3.0f))
                    {
                        _mined++;
                        SoundManager.Play("slam_land", 0.95f, 1.2f);
                        new VisualFX(_veins[i].Vein.Position + new Vector3(0, 1f, 0), Color.SkyBlue, 4.0f, 0.3f);
                        Destroy(_veins[i].Vein);
                        _veins.RemoveAt(i);
                        CharacterUnlockEncounters.SpawnEncounterEnemy(g, g.PlayerPos + new Vector3(6f, 0f, 6f), "beetle");
                    }
                }

                if (_mined >= 3)
                {
                    _phase = 2;
                    _escapeTimer = 15.0f;
                    Vector3 podPos = Physics.FindSafeSpawnPoint(Position + new Vector3(-16f, 0f, 12f));
                    _dropPod = new Entity(position: podPos);
                    new Entity(parent: _dropPod, model: "cube", scale: new(3.2f, 5.8f, 3.2f), position: new(0, 2.9f, 0), color: Rgb(62, 68, 78));
                    new Entity(parent: _dropPod, model: "diamond", scale: new(4.5f, 0.2f, 4.5f), position: new(0, 0.2f, 0), color: Color.Lime, unlit: true);
                    _dropPod.BillboardText = "★ ЗАПРЫГНИТЕ В РАМПУ ДЕCАНТНОЙ КАПСУЛЫ! ★";
                    _dropPod.Color = Color.Lime;
                    SoundManager.Play("boss_roar", 1.0f, 1.15f);
                    g.ShowBanner("КВОТА ВЫПОЛНЕНА! УСПЕЙТЕ ДОБЕЖАТЬ ДО ДЕСАНТНОЙ КАПСУЛЫ ЗА 15 СЕКУНД!", Color.Lime, 4.0f);
                }
            }
            else if (_phase == 2 && _dropPod != null)
            {
                _escapeTimer -= dt;
                _labelEnt.BillboardText = $"ЭВАКУАЦИЯ КАПСУЛЫ DRG ЧЕРЕЗ {_escapeTimer:F1} СЕК!";
                if (Vector3.Distance(g.PlayerPos, _dropPod.Position) <= 4.5f)
                {
                    Destroy(_dropPod);
                    g.UnlockAchievement("ach_char_scout");
                    Destroy(this);
                }
                else if (_escapeTimer <= 0f)
                {
                    _escapeTimer = 15.0f;
                    g.ShowBanner("КАПСУЛА ПЕРЕЗАПУЩЕНА — БЕГИТЕ К ЗЕЛЁНОМУ МАЯКУ КАПСУЛЫ!", Color.Orange, 2.5f);
                }
            }
        }
    }

    // 7. МЕДИК: Эскорт раненого Штурмовика-Пулемётчика с Убер-лучом [E] через поле боя!
    public class WoundedHeavyEscortEncounter : Entity
    {
        private bool _escorting = false;
        private float _uberCharge = 0f;
        private Vector3 _goalPos;
        private float _waveTimer = 2.5f;
        private readonly Entity _heavyBody;
        private readonly Entity _labelEnt;

        public WoundedHeavyEscortEncounter(Vector3 pos) : base(position: pos)
        {
            _heavyBody = new Entity(parent: this);
            new Entity(parent: _heavyBody, model: "cube", scale: new(1.1f, 1.6f, 0.9f), position: new(0, 0.95f, 0), color: Rgb(195, 42, 48));
            new Entity(parent: _heavyBody, model: "sphere", scaleUniform: 0.58f, position: new(0, 2.0f, 0), color: Rgb(238, 195, 165));
            _labelEnt = new Entity(parent: this, position: new(0, 2.9f, 0), color: Color.Red)
            {
                BillboardText = "[E] Раненый Пулемётчик RED (Подключить Лечебный Убер-Луч)"
            };
            Vector3 toCenter = pos.Length() > 5f ? Vector3.Normalize(new Vector3(-pos.X, 0f, -pos.Z)) : Vector3.UnitZ;
            _goalPos = Physics.FindSafeSpawnPoint(pos + toCenter * 26f);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 16f || _escorting);

            if (!_escorting && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _escorting = true;
                SoundManager.Play("heal", 1.0f, 1.0f);
                g.ShowBanner("ДЕРЖИТЕСЬ В 9М ОТ ПУЛЕМЁТЧИКА, ЧТОБЫ ПИТАТЬ ЕГО ЛЕЧЕБНЫМ ЛУЧОМ ДО 100% УБЕР-ЗАРЯДА!", Color.Red, 4.2f);
            }
            else if (_escorting)
            {
                if (dist <= 9.5f)
                {
                    _uberCharge = MathF.Min(100f, _uberCharge + 10.5f * dt);
                    Vector3 dir = _goalPos - Position;
                    dir.Y = 0f;
                    if (dir.LengthSquared() > 1.0f)
                    {
                        Vector3 step = Physics.ResolveHorizontalCollision(Position + Vector3.Normalize(dir) * 3.2f * dt, 0.8f, Y);
                        Position = new Vector3(step.X, Physics.GetSurfaceHeight(step.X, step.Z, Y + 1.5f), step.Z);
                    }
                    if ((int)(_uberCharge * 2f) % 5 == 0)
                        new LightningBeam(g.PlayerPos + new Vector3(0, 1.2f, 0), Position + new Vector3(0, 1.2f, 0), Color.Red, 0.14f);
                }
                _waveTimer -= dt;
                if (_waveTimer <= 0f)
                {
                    _waveTimer = 4.2f;
                    CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(7f, 0f, -6f), "imp");
                }
                _labelEnt.BillboardText = $"Убер-Заряд Конвоя: {(int)_uberCharge}% (Держитесь рядом для лечебного луча!)";
                if (_uberCharge >= 100f)
                {
                    SoundManager.Play("ult_cast", 1.15f, 1.0f);
                    g.UnlockAchievement("ach_char_medic");
                    Destroy(this);
                }
            }
        }
    }

    // 8. MINDUSTRY: Мини-Форпост Серпуло с добычей Меди и постройкой Стен/Турелей [E] на 6 ячейках!
    public class SerpuloMiniOutpostEncounter : Entity
    {
        private bool _active = false;
        private int _copper = 30;
        private float _survivalTimer = 22.0f;
        private float _waveCd = 2.0f;
        private float _turretFireCd = 0.5f;
        private readonly List<(Entity Pad, int BuiltType)> _pads = new(); // 0=пусто, 1=Турель Двойка
        private readonly Entity _labelEnt;

        public SerpuloMiniOutpostEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(2.4f, 1.6f, 2.4f), position: new(0, 0.8f, 0), color: Rgb(55, 62, 76));
            new Entity(parent: this, model: "diamond", scale: new(1.4f, 1.8f, 1.4f), position: new(0, 2.1f, 0), color: Color.Gold, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.6f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Посадочное Ядро Серпуло (Мини-Tower Defense Mindustry)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 16f || _active);

            if (!_active && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _active = true;
                _copper = 45;
                for (int i = 0; i < 4; i++)
                {
                    float a = i * (MathF.PI * 0.5f) + 0.4f;
                    Vector3 pp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 6.5f, 0f, MathF.Sin(a) * 6.5f));
                    var pad = new Entity(position: pp);
                    new Entity(parent: pad, model: "cube", scale: new(1.6f, 0.2f, 1.6f), position: new(0, 0.1f, 0), color: Rgb(215, 145, 55));
                    pad.BillboardText = "[E] Построить Турель «Двойка» (15 Меди)";
                    pad.Color = Color.Gold;
                    _pads.Add((pad, 0));
                }
                SoundManager.Play("build_place");
                g.ShowBanner("ФОРПОСТ СЕРПУЛО: СТРОЙТЕ [E] ТУРЕЛИ «ДВОЙКА» НА ЯЧЕЙКАХ И ОТРАЗИТЕ ВОЛНЫ (22 СЕК)!", Color.Gold, 4.0f);
            }
            else if (_active)
            {
                _copper += (int)(12f * dt * 10f) > 0 ? 0 : 0;
                if (Random.Shared.NextDouble() < 6.0 * dt) _copper += 2;
                _survivalTimer -= dt;
                _waveCd -= dt;
                if (_waveCd <= 0f)
                {
                    _waveCd = 4.2f;
                    _copper += 10;
                    CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(11f, 0f, 8f), "beetle");
                    CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(-11f, 0f, -8f), "wisp");
                }

                for (int i = 0; i < _pads.Count; i++)
                {
                    var p = _pads[i];
                    if (p.BuiltType == 0 && Vector3.Distance(g.PlayerPos, p.Pad.Position) < 3.6f && Raylib.IsKeyPressed(KeyboardKey.E) && _copper >= 15)
                    {
                        _copper -= 15;
                        new Entity(parent: p.Pad, model: "diamond", scale: new(1.1f, 1.8f, 1.1f), position: new(0, 1.1f, 0), color: Color.SkyBlue, unlit: true);
                        p.Pad.BillboardText = "Турель «Двойка» [АКТИВНА]";
                        _pads[i] = (p.Pad, 1);
                        SoundManager.Play("build_place", 1.0f, 1.15f);
                    }
                }

                _turretFireCd -= dt;
                if (_turretFireCd <= 0f)
                {
                    _turretFireCd = 0.55f;
                    foreach (var p in _pads)
                    {
                        if (p.BuiltType != 1) continue;
                        var target = CombatManager.FindNearestHostileAt(p.Pad.Position);
                        if (target != null && Vector3.Distance(target.Position, p.Pad.Position) <= 18f)
                        {
                            Vector3 dir = Vector3.Normalize((target.Position + new Vector3(0, target.HitHeight, 0)) - (p.Pad.Position + new Vector3(0, 1.4f, 0)));
                            new Projectile(p.Pad.Position + new Vector3(0, 1.4f, 0), dir, 58f, 26f, 0.25f, Color.Gold, isPlayer: true, canProc: false);
                        }
                    }
                }

                int builtCnt = 0;
                foreach (var p in _pads) if (p.BuiltType == 1) builtCnt++;
                _labelEnt.BillboardText = $"Оборона Ядра: {_survivalTimer:F1}с | Медь: {_copper} | Турелей: {builtCnt}/4";
                if (_survivalTimer <= 0f && builtCnt >= 2)
                {
                    foreach (var p in _pads) if (IsValid(p.Pad)) Destroy(p.Pad);
                    g.UnlockAchievement("ach_char_mindustry");
                    Destroy(this);
                }
                else if (_survivalTimer <= 0f)
                {
                    _survivalTimer = 6.0f;
                    g.ShowBanner("ПОСТРОЙТЕ ХОТЯ БЫ 2 ТУРЕЛИ «ДВОЙКА» [E] ДЛЯ ЗАВЕРШЕНИЯ ФОРПОСТА!", Color.Orange, 2.2f);
                }
            }
        }
    }

    // 9. СЛАРК: Стелс-побег мимо вращающегося прожектора Надзирателя Тёмного Рифа -> Ключ-Заточка -> Босс
    public class DarkReefStealthEncounter : Entity
    {
        private int _phase = 0; // 0 = украсть ключ вне прожектора, 1 = открыть клетку, 2 = бой с Надзирателем
        private float _spotlightAng = 0f;
        private CombatTarget? _warden = null;
        private readonly Entity _beamEnt;
        private readonly Entity _keyEnt;
        private readonly Entity _labelEnt;

        public DarkReefStealthEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(2.2f, 2.8f, 2.2f), position: new(0, 1.4f, 0), color: Rgb(32, 62, 68));
            _beamEnt = new Entity(parent: this, model: "cube", scale: new(1.8f, 0.14f, 13.0f), position: new(0, 0.2f, 6.5f), color: Rgb(255, 45, 55, 130), unlit: true);
            _keyEnt = new Entity(parent: this, position: new(0, 0, 9.5f));
            new Entity(parent: _keyEnt, model: "diamond", scale: new(0.6f, 1.3f, 0.6f), position: new(0, 1.2f, 0), color: Color.Lime, unlit: true);
            _keyEnt.BillboardText = "[E] Похитить Ключ-Заточку (Не попадайтесь в луч!)";
            _keyEnt.Color = Color.Lime;

            _labelEnt = new Entity(parent: this, position: new(0, 3.6f, 0), color: Rgb(65, 235, 195))
            {
                BillboardText = "Адамантовая Клетка Тёмного Рифа\n[Прокрадитесь мимо красного луча и украдите Ключ-Заточку [E]!]"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _phase == 2);

            if (_phase == 0)
            {
                _spotlightAng += 68f * dt;
                float rad = _spotlightAng * (MathF.PI / 180f);
                Vector3 beamDir = new(MathF.Sin(rad), 0f, MathF.Cos(rad));
                _beamEnt.Position = beamDir * 6.5f + new Vector3(0, 0.22f, 0);
                _beamEnt.RotationY = _spotlightAng;

                Vector3 toP = g.PlayerPos - Position;
                toP.Y = 0f;
                if (toP.Length() > 1.5f && toP.Length() <= 13.0f && Vector3.Dot(Vector3.Normalize(toP), beamDir) > 0.90f)
                {
                    Vector3 push = Position + beamDir * 15.5f;
                    g.PlayerPos = new Vector3(push.X, Physics.GetSurfaceHeight(push.X, push.Z, g.PlayerPos.Y + 1f), push.Z);
                    SoundManager.Play("ui_error");
                    g.ShowBanner("ПРОЖЕКТОР РИФА ЗАСЕК ВАС! ОБХОДИТЕ ЛУЧ ПО КРУГУ!", Color.Red, 1.8f);
                }

                Vector3 keyWorld = Position + _keyEnt.Position;
                if (Vector3.Distance(g.PlayerPos, keyWorld) < 3.8f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    _phase = 1;
                    _keyEnt.Enabled = false;
                    _beamEnt.Enabled = false;
                    SoundManager.Play("coin_flip", 1.0f, 1.25f);
                    _labelEnt.BillboardText = "[E] Отоприте Адамантовую Клетку Ключом-Заточкой!";
                    g.ShowBanner("КЛЮЧ-ЗАТОЧКА ПОХИЩЕН! ОТОПРИТЕ [E] АДАМАНТОВУЮ КЛЕТКУ!", Color.Lime, 3.0f);
                }
            }
            else if (_phase == 1 && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _phase = 2;
                _warden = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(0, 0, -7f), "warden_reef", mult: 0.85f);
                SoundManager.Play("boss_roar");
                g.ShowBanner("КЛЕТКА ВСКРЫТА! ПОБЕДИТЕ НАДЗИРАТЕЛЯ ТЁМНОГО РИФА!", Rgb(65, 235, 195), 3.6f);
            }
            else if (_phase == 2)
            {
                _labelEnt.BillboardText = "Одолейте Надзирателя Тёмного Рифа!";
                if (_warden == null || !IsValid(_warden) || _warden.Hp <= 0f || _warden.IsAlly)
                {
                    g.UnlockAchievement("ach_char_slark");
                    Destroy(this);
                }
            }
        }
    }

    // 10. ТАЙДХАНТЕР: 2 предмета в Водоворот Мальроуна + 3 волны Приливного Цунами (прыгать на скалы!)
    public class MaelrawnTsunamiEncounter : Entity
    {
        private int _fed = 0;
        private int _wavesDone = 0;
        private float _waveTimer = 3.5f;
        private readonly Entity _waterDisk;
        private readonly Entity _labelEnt;

        public MaelrawnTsunamiEncounter(Vector3 pos) : base(position: pos)
        {
            _waterDisk = new Entity(parent: this, model: "sphere", scale: new(8f, 0.25f, 8f), position: new(0, 0.15f, 0), color: Rgb(35, 185, 195, 155), unlit: true);
            // Высокие коралловые скалы для спасения от волны Цунами
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.09f;
                float rx = pos.X + MathF.Cos(a) * 6.8f, rz = pos.Z + MathF.Sin(a) * 6.8f;
                new Entity(parent: this, model: "cube", scale: new(2.8f, 2.6f, 2.8f), position: new(MathF.Cos(a) * 6.8f, 1.3f, MathF.Sin(a) * 6.8f), color: Rgb(68, 125, 118));
                Physics.AddSolidBox(rx, rz, 2.8f, 2.8f, 2.6f, 0f, pos.Y, walkableTop: true);
            }
            _labelEnt = new Entity(parent: this, position: new(0, 3.6f, 0), color: Rgb(65, 245, 205))
            {
                BillboardText = "[E] Водоворот Мальроуна (Пожертвовать предмет: 0/2)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _fed >= 2);

            if (_fed < 2 && dist < 4.8f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                string? sac = null;
                foreach (var kv in g.Items)
                    if (kv.Value > 0 && kv.Key != "tonic_affliction") { sac = kv.Key; break; }
                if (sac != null)
                {
                    g.Items[sac]--;
                    if (g.Items[sac] <= 0) g.ItemOrder.Remove(sac);
                    _fed++;
                    SoundManager.Play("slam_land", 1.0f, 0.9f);
                    _labelEnt.BillboardText = $"[E] Водоворот Мальроуна (Пожертвовано предметов: {_fed}/2)";
                    if (_fed >= 2)
                    {
                        _waterDisk.Scale = new Vector3(24f, 0.4f, 24f);
                        g.ShowBanner("МАЛЬРОУН ПРОБУДИЛСЯ! ЗАПРЫГИВАЙТЕ НА КОРАЛЛОВЫЕ СКАЛЫ ВО ВРЕМЯ 3 ВОЛН ЦУНАМИ!", Rgb(65, 245, 205), 4.2f);
                    }
                }
                else
                {
                    SoundManager.Play("ui_error");
                    g.ShowBanner("НУЖЕН ПРЕДМЕТ В ИНВЕНТАРЕ ДЛЯ ЖЕРТВЫ БЕЗДНЕ!", Color.Red, 2.0f);
                }
            }
            else if (_fed >= 2)
            {
                _waveTimer -= dt;
                _waterDisk.Y = _waveTimer <= 1.2f ? 1.85f : 0.25f;
                if (_waveTimer <= 0f)
                {
                    _wavesDone++;
                    _waveTimer = 3.8f;
                    SoundManager.Play("slam_land", 1.15f, 0.8f);
                    if (dist <= 14f && g.PlayerPos.Y < Position.Y + 1.9f)
                    {
                        CombatManager.DamagePlayer(22f);
                        g.ShowBanner("ВАС НАКРЫЛО ЦУНАМИ! СТОЙТЕ НА ВЕРШИНЕ КОРАЛЛОВЫХ СКАЛ!", Color.Red, 2.0f);
                    }
                    if (_wavesDone >= 3)
                    {
                        g.UnlockAchievement("ach_char_tidehunter");
                        Destroy(this);
                    }
                }
                _labelEnt.BillboardText = $"Приливное Цунами Мальроуна: Волна {_wavesDone}/3 (Стойте на скалах!)";
            }
        }
    }

    // 11. ДАНТЕ LCB: Автобус «Мефистофель» с цепями Грешников и 3 ударами Полночи (12:00)
    public class MephistophelesClockEncounter : Entity
    {
        private bool _active = false;
        private float _clockHand = 6.0f;
        private int _strikes = 0;
        private readonly Entity _labelEnt;

        public MephistophelesClockEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(3.6f, 2.2f, 1.8f), position: new(0, 1.1f, 0), color: Rgb(68, 24, 32));
            new Entity(parent: this, model: "diamond", scale: new(1.5f, 1.5f, 0.35f), position: new(0, 2.8f, 0), color: Color.Gold, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 4.1f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Остов Автобуса «Мефистофель» (Часы Судного Дня LCB)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 15f || _active);

            if (!_active && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _active = true;
                CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(7f, 0f, 6f), "hollow_shadow");
                SoundManager.Play("dante_clock");
                g.ShowBanner("ЧАСЫ МЕФИСТОФЕЛЯ ЗАПУЩЕНЫ! ТРИЖДЫ НАЖМИТЕ [E] РОВНО НА 12:00!", Color.Gold, 3.8f);
            }
            else if (_active)
            {
                _clockHand = (_clockHand + 9.4f * dt) % 12f;
                int hr = ((int)MathF.Floor(_clockHand) % 12) + 1;
                bool inZone = _clockHand >= 10.6f || _clockHand <= 1.4f;
                _labelEnt.Color = inZone ? Color.Lime : Color.Gold;
                _labelEnt.BillboardText = $"Часы LCB: [{hr:00}:00] {(inZone ? "★ ЖМИТЕ [E]! ★" : "")} ({_strikes}/3)";

                if (dist < 4.8f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    if (inZone)
                    {
                        _strikes++;
                        SoundManager.Play("dante_clock", 1.1f, 1.0f + _strikes * 0.1f);
                        if (_strikes >= 3)
                        {
                            g.UnlockAchievement("ach_char_dante");
                            Destroy(this);
                        }
                    }
                    else
                    {
                        SoundManager.Play("ui_error");
                        _strikes = Math.Max(0, _strikes - 1);
                    }
                }
            }
        }
    }

    // 12. ПУШНОЙ: 3 научных опыта «Галилео» подряд (Акустика -> Дуга Теслы -> 3 прыжковых рок-кольца!)
    public class GalileoStudioEncounter : Entity
    {
        private int _exp = 0; // 0=старт, 1=акустика, 2=катушка Теслы, 3=прыжки рок-волн
        private float _meter = 0f;
        private int _ringsJumped = 0;
        private float _ringTimer = 1.6f;
        private readonly Entity _coilNode;
        private readonly Entity _labelEnt;

        public GalileoStudioEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(2.4f, 1.4f, 1.4f), position: new(0, 0.7f, 0), color: Rgb(38, 42, 54));
            new Entity(parent: this, model: "diamond", scale: new(0.9f, 2.6f, 0.9f), position: new(0, 2.2f, 0), color: Color.SkyBlue, unlit: true);
            _coilNode = new Entity(parent: this, position: new(8.5f, 0f, 0f), enabled: false);
            new Entity(parent: _coilNode, model: "diamond", scale: new(1.2f, 2.4f, 1.2f), position: new(0, 1.2f, 0), color: Color.Yellow, unlit: true);
            _coilNode.BillboardText = "КАТУШКА-ПРИЁМНИК ТЕСЛЫ [Встаньте сюда!]";
            _coilNode.Color = Color.Yellow;
            _labelEnt = new Entity(parent: this, position: new(0, 3.9f, 0), color: Color.Orange)
            {
                BillboardText = "[E] Передвижная Студия «ГАЛИЛЕО» (3 Эксперимента в Эфире!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _exp > 0);

            if (_exp == 0 && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _exp = 1;
                _meter = 0f;
                SoundManager.Play("zap");
                g.ShowBanner("ОПЫТ 1/3 (АКУСТИКА): БЫСТРО АТАКУЙТЕ РЕЗОНАТОР ДО 100 ДБ!", Color.Orange, 3.5f);
            }
            else if (_exp == 1)
            {
                _meter = MathF.Max(0f, _meter - 16f * dt);
                if (CharacterUnlockEncounters.DetectPlayerAttackNear(g, Position + new Vector3(0, 1.6f, 0), 3.0f))
                {
                    _meter += 26f;
                    SoundManager.Play("zap", 0.8f, 1.1f);
                }
                _labelEnt.BillboardText = $"Опыт 1/3 (Акустика): {(int)MathF.Min(100f, _meter)} / 100 дБ [Атакуйте!]";
                if (_meter >= 100f)
                {
                    _exp = 2;
                    _meter = 0f;
                    _coilNode.Enabled = true;
                    g.ShowBanner("ОПЫТ 2/3 (ТЕСЛА): ЗАМКНИТЕ СОБОЙ ДУГУ У ЖЁЛТОЙ КАТУШКИ СПРАВА!", Color.Yellow, 3.5f);
                }
            }
            else if (_exp == 2)
            {
                Vector3 coilWorld = Position + _coilNode.Position;
                if (Vector3.Distance(g.PlayerPos, coilWorld) <= 4.0f)
                {
                    _meter += 38f * dt;
                    new LightningBeam(Position + new Vector3(0, 2.4f, 0), g.PlayerPos + new Vector3(0, 1.2f, 0), Color.SkyBlue, 0.2f);
                }
                _labelEnt.BillboardText = $"Опыт 2/3 (Дуга Теслы): {(int)MathF.Min(100f, _meter)}%";
                if (_meter >= 100f)
                {
                    _exp = 3;
                    _coilNode.Enabled = false;
                    g.ShowBanner("ОПЫТ 3/3 (РОК-ВОЛНЫ 10 000 В): ПЕРЕПРЫГНИТЕ [SPACE] 3 ЗВУКОВЫХ КОЛЬЦА!", Color.SkyBlue, 3.5f);
                }
            }
            else if (_exp == 3)
            {
                _ringTimer -= dt;
                if (_ringTimer <= 0f)
                {
                    _ringTimer = 2.0f;
                    new LunarShockwaveRing(Position, 12f, 18f, 16f);
                    _ringsJumped++;
                    if (_ringsJumped >= 3)
                    {
                        g.UnlockAchievement("ach_char_pushnoy");
                        Destroy(this);
                    }
                }
                _labelEnt.BillboardText = $"Опыт 3/3 (10 000 Вольт!): Перепрыгнуто рок-волн {_ringsJumped}/3";
            }
        }
    }

    // 13. СТИВ: Добыча 4 жил Обсидиана + Огнива и ручная сборка Портала Незера [E] + рой Ифритов!
    public class RuinedNetherPortalEncounter : Entity
    {
        private int _obsidian = 0;
        private bool _lit = false;
        private readonly List<Entity> _ores = new();
        private readonly List<CombatTarget> _blazes = new();
        private readonly Entity _portalPlane;
        private readonly Entity _labelEnt;

        public RuinedNetherPortalEncounter(Vector3 pos, Random rng) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(3.2f, 0.45f, 0.8f), position: new(0, 0.22f, 0), color: Rgb(28, 18, 42));
            foreach (float sx in new[] { -1.35f, 1.35f })
                new Entity(parent: this, model: "cube", scale: new(0.55f, 3.8f, 0.8f), position: new(sx, 2.1f, 0), color: Rgb(28, 18, 42));
            _portalPlane = new Entity(parent: this, model: "cube", scale: new(2.2f, 3.2f, 0.18f), position: new(0, 2.0f, 0), color: Rgb(185, 65, 255, 190), unlit: true, enabled: false);
            _labelEnt = new Entity(parent: this, position: new(0, 4.5f, 0), color: Rgb(195, 95, 255))
            {
                BillboardText = "Разрушенный Портал Незера [Добудьте 4 Обсидиана вокруг (0/4)]"
            };

            for (int i = 0; i < 4; i++)
            {
                float a = i * (MathF.PI * 0.5f) + (float)rng.NextDouble() * 0.5f;
                Vector3 op = Physics.FindSafeSpawnPoint(pos + new Vector3(MathF.Cos(a) * 12.5f, 0f, MathF.Sin(a) * 12.5f));
                var ore = new Entity(position: op);
                new Entity(parent: ore, model: "cube", scale: new(1.2f, 1.2f, 1.2f), position: new(0, 0.6f, 0), color: Rgb(44, 22, 68));
                ore.BillboardText = $"Блок Обсидиана #{i + 1}/4 [Разбейте!]";
                ore.Color = Rgb(195, 95, 255);
                _ores.Add(ore);
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 18f || _lit);

            for (int i = _ores.Count - 1; i >= 0; i--)
            {
                var ore = _ores[i];
                if (CharacterUnlockEncounters.DetectPlayerAttackNear(g, ore.Position + new Vector3(0, 0.7f, 0), 2.8f))
                {
                    _obsidian++;
                    SoundManager.Play("slam_land", 0.9f, 1.25f);
                    new VisualFX(ore.Position + new Vector3(0, 0.7f, 0), Rgb(185, 65, 255), 3.2f, 0.25f);
                    Destroy(ore);
                    _ores.RemoveAt(i);
                    _labelEnt.BillboardText = _obsidian >= 4
                        ? "[E] Вставить Обсидиан и зажечь Портал Незера Огнивом!"
                        : $"Разрушенный Портал Незера [Добыто Обсидиана: {_obsidian}/4]";
                }
            }

            if (!_lit && _obsidian >= 4 && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _lit = true;
                _portalPlane.Enabled = true;
                SoundManager.Play("portal_open");
                for (int i = 0; i < 3; i++)
                    _blazes.Add(CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3((i - 1) * 6f, 0f, 6f), "nether_blaze"));
                g.ShowBanner("ПОРТАЛ НЕЗЕРА ВОССТАНОВЛЕН! ОТРАЗИТЕ РОЙ ИФРИТОВ!", Color.Orange, 3.6f);
            }
            else if (_lit)
            {
                _blazes.RemoveAll(b => !IsValid(b) || b.Hp <= 0f || b.IsAlly);
                _labelEnt.BillboardText = $"Отразите рой Ифритов Незера: осталось {_blazes.Count}";
                if (_blazes.Count == 0)
                {
                    g.UnlockAchievement("ach_char_steve");
                    Destroy(this);
                }
            }
        }
    }

    // 14. ВАЙПЕР: Подкат под низкими горизонтальными лазерами тюрьмы OSIC + отстрел 4 магнитных замков!
    public class OsicLaserVaultEncounter : Entity
    {
        private bool _active = false;
        private int _locksShot = 0;
        private float _laserAng = 0f;
        private readonly Entity _laserBar;
        private readonly List<Entity> _locks = new();
        private readonly Entity _labelEnt;

        public OsicLaserVaultEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(2.0f, 3.2f, 2.0f), position: new(0, 1.6f, 0), color: Rgb(36, 48, 42));
            _laserBar = new Entity(parent: this, model: "cube", scale: new(16.0f, 0.16f, 0.16f), position: new(0, 1.35f, 0), color: Color.Lime, unlit: true, enabled: false);
            _labelEnt = new Entity(parent: this, position: new(0, 3.8f, 0), color: Color.Lime)
            {
                BillboardText = "[E] Бронекапсула OSIC (Подкат под лазерами + 4 замка)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 16f || _active);

            if (!_active && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _active = true;
                _laserBar.Enabled = true;
                for (int i = 0; i < 4; i++)
                {
                    float a = i * (MathF.PI * 0.5f);
                    var lk = new Entity(position: Position + new Vector3(MathF.Cos(a) * 5.2f, 2.2f, MathF.Sin(a) * 5.2f));
                    new Entity(parent: lk, model: "diamond", scaleUniform: 0.9f, color: Color.Lime, unlit: true);
                    lk.BillboardText = $"[ЗАМОК #{i + 1}/4]";
                    lk.Color = Color.Lime;
                    _locks.Add(lk);
                }
                SoundManager.Play("laser");
                g.ShowBanner("ПРОСКАЛЬЗЫВАЙТЕ НА РЫВКАХ [SHIFT] ИЛИ ПЕРЕПРЫГИВАЙТЕ ЛАЗЕРЫ И СБЕЙТЕ 4 ЗАМКА!", Color.Lime, 3.8f);
            }
            else if (_active)
            {
                _laserAng += 95f * dt;
                _laserBar.RotationY = _laserAng;
                for (int i = _locks.Count - 1; i >= 0; i--)
                {
                    if (CharacterUnlockEncounters.DetectPlayerAttackNear(g, _locks[i].Position, 2.2f))
                    {
                        _locksShot++;
                        SoundManager.Play("parry", 0.95f, 1.25f);
                        Destroy(_locks[i]);
                        _locks.RemoveAt(i);
                    }
                }
                _labelEnt.BillboardText = $"Сбито магнитных замков OSIC: {_locksShot}/4";
                if (_locksShot >= 4)
                {
                    g.UnlockAchievement("ach_char_vyper");
                    Destroy(this);
                }
            }
        }
    }

    // 15. ХЕЙЗ: Контракт Сновидений OSIC (выследить 1 Истинного Спящего среди 5 сонных силуэтов без промаха!)
    public class OsicDreamAssassinationEncounter : Entity
    {
        private bool _active = false;
        private int _contracts = 0;
        private readonly List<(Entity Fig, bool IsTrue)> _sleepers = new();
        private readonly Entity _labelEnt;

        public OsicDreamAssassinationEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "diamond", scale: new(1.6f, 2.4f, 0.35f), position: new(0, 1.8f, 0), color: Rgb(235, 185, 65), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.5f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Ловец Снов OSIC (Контракт Устранения во Сне)"
            };
        }

        private void SpawnRound(IPlayerContext g)
        {
            foreach (var s in _sleepers) if (IsValid(s.Fig)) Destroy(s.Fig);
            _sleepers.Clear();
            int trueIdx = Random.Shared.Next(0, 5);
            for (int i = 0; i < 5; i++)
            {
                float a = (i / 5f) * MathF.PI * 2f + _contracts * 0.6f;
                Vector3 sp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 9.5f, 0f, MathF.Sin(a) * 9.5f));
                bool isTrue = i == trueIdx;
                var fig = new Entity(position: sp);
                new Entity(parent: fig, model: "cube", scale: new(0.8f, 1.8f, 0.6f), position: new(0, 0.9f, 0),
                    color: isTrue ? Rgb(255, 215, 75) : Rgb(145, 95, 195), unlit: true);
                fig.BillboardText = "Силуэт во Сне [Устраните Истинного Золотого Спящего!]";
                fig.Color = Color.Gold;
                _sleepers.Add((fig, isTrue));
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 16f || _active);

            if (!_active && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _active = true;
                _contracts = 0;
                SpawnRound(g);
                g.ShowBanner("ТУМАН СНОВ OSIC: ТОЧНЫМ УДАРОМ УСТРАНИТЕ ТОЛЬКО СПОКОЙНОГО ЗОЛОТОГО СПЯЩЕГО (2 РАЗА)!", Color.Gold, 4.0f);
            }
            else if (_active)
            {
                float t = (float)Raylib.GetTime();
                foreach (var s in _sleepers)
                {
                    if (!s.IsTrue && IsValid(s.Fig))
                        s.Fig.RotationZ = MathF.Sin(t * 18f + s.Fig.X) * 12f; // Ложные миражи нервно подрагивают!
                }

                for (int i = 0; i < _sleepers.Count; i++)
                {
                    var s = _sleepers[i];
                    if (IsValid(s.Fig) && CharacterUnlockEncounters.DetectPlayerAttackNear(g, s.Fig.Position + new Vector3(0, 1.0f, 0), 2.0f))
                    {
                        if (s.IsTrue)
                        {
                            _contracts++;
                            SoundManager.Play("crit", 1.0f, 1.15f);
                            if (_contracts >= 2)
                            {
                                foreach (var rem in _sleepers) if (IsValid(rem.Fig)) Destroy(rem.Fig);
                                g.UnlockAchievement("ach_char_haze");
                                Destroy(this);
                                return;
                            }
                            SpawnRound(g);
                            g.ShowBanner($"ЦЕЛЬ УСТРАНЕНА БЕСШУМНО ({_contracts}/2)!", Color.Gold, 2.0f);
                        }
                        else
                        {
                            _contracts = 0;
                            SoundManager.Play("ui_error");
                            CharacterUnlockEncounters.SpawnEncounterEnemy(g, s.Fig.Position, "hollow_shadow");
                            SpawnRound(g);
                            g.ShowBanner("ТРЕВОГА! ВЫ УДАРИЛИ ЛОЖНЫЙ МИРАЖ — ПРОГРЕСС СБРОШЕН!", Color.Red, 2.4f);
                        }
                        break;
                    }
                }
                _labelEnt.BillboardText = $"Контракт OSIC: устранено истинных целей {_contracts}/2";
            }
        }
    }

    // 16. ДЖЕК-О ВАЛЕНТАЙН: Бильярд Миньонов (пинками и ударами загнать 3 чиби-рыцарей в 3 Кольца Элизиона!)
    public class JackOServantBilliardsEncounter : Entity
    {
        private bool _active = false;
        private int _scored = 0;
        private readonly List<(Entity Ball, Vector3 Vel)> _servants = new();
        private readonly List<Entity> _goals = new();
        private readonly Entity _labelEnt;

        public JackOServantBilliardsEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "sphere", scaleUniform: 1.35f, position: new(0, 0.9f, 0), color: Rgb(255, 135, 35));
            _labelEnt = new Entity(parent: this, position: new(0, 2.6f, 0), color: Rgb(155, 255, 75))
            {
                BillboardText = "[E] Железная Тыква Джек-О (Конфетный Бильярд Миньонов!)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && (dist < 16f || _active);

            if (!_active && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _active = true;
                for (int i = 0; i < 3; i++)
                {
                    float a = i * 2.09f;
                    Vector3 gp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 11.5f, 0f, MathF.Sin(a) * 11.5f));
                    var goal = new Entity(position: gp);
                    new Entity(parent: goal, model: "diamond", scale: new(3.6f, 0.16f, 3.6f), position: new(0, 0.15f, 0), color: Color.Lime, unlit: true);
                    goal.BillboardText = $"ВОРОТА ЭЛИЗИОНА #{i + 1}";
                    goal.Color = Color.Lime;
                    _goals.Add(goal);

                    Vector3 sp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(a) * 4.5f, 0f, MathF.Sin(a) * 4.5f));
                    var sv = new Entity(position: sp);
                    new Entity(parent: sv, model: "sphere", scaleUniform: 0.85f, position: new(0, 0.55f, 0), color: Color.White, unlit: true);
                    sv.BillboardText = "[УДАР / E] Пнуть Миньона в Ворота!";
                    sv.Color = Rgb(155, 255, 75);
                    _servants.Add((sv, Vector3.Zero));
                }
                SoundManager.Play("bounce_pad");
                g.ShowBanner("БИЛЬЯРД ЭЛИЗИОНА: УДАРАМИ ИЛИ [E] ЗАГОНИТЕ 3 ЧИБИ-МИНЬОНОВ В ЗЕЛЁНЫЕ КОЛЬЦА!", Rgb(155, 255, 75), 4.0f);
            }
            else if (_active)
            {
                for (int i = _servants.Count - 1; i >= 0; i--)
                {
                    var item = _servants[i];
                    Vector3 vel = item.Vel * MathF.Max(0f, 1f - 2.6f * dt);
                    if (CharacterUnlockEncounters.DetectPlayerAttackNear(g, item.Ball.Position + new Vector3(0, 0.5f, 0), 2.4f, out Vector3 hitDir)
                        || (Vector3.Distance(g.PlayerPos, item.Ball.Position) < 3.4f && Raylib.IsKeyPressed(KeyboardKey.E)))
                    {
                        Vector3 pushD = g.PlayerForward.LengthSquared() > 0.01f ? new Vector3(g.PlayerForward.X, 0f, g.PlayerForward.Z) : hitDir;
                        if (pushD.LengthSquared() > 0.001f) pushD = Vector3.Normalize(pushD);
                        vel = pushD * 22f;
                        SoundManager.Play("bounce_pad", 0.9f, 1.3f);
                    }
                    Vector3 np = Physics.ResolveHorizontalCollision(item.Ball.Position + vel * dt, 0.65f, item.Ball.Y);
                    item.Ball.Position = new Vector3(np.X, Physics.GetSurfaceHeight(np.X, np.Z, item.Ball.Y + 1.5f), np.Z);

                    bool goaled = false;
                    foreach (var gl in _goals)
                    {
                        if (Physics.Distance2D(item.Ball.Position, gl.Position) <= 2.4f)
                        {
                            _scored++;
                            goaled = true;
                            SoundManager.Play("victory", 0.85f, 1.2f);
                            new VisualFX(gl.Position + new Vector3(0, 1f, 0), Color.Lime, 4.0f, 0.3f);
                            Destroy(item.Ball);
                            _servants.RemoveAt(i);
                            break;
                        }
                    }
                    if (!goaled) _servants[i] = (item.Ball, vel);
                }

                _labelEnt.BillboardText = $"Забито Миньонов в Ворота Элизиона: {_scored}/3";
                if (_scored >= 3)
                {
                    foreach (var gl in _goals) if (IsValid(gl)) Destroy(gl);
                    g.UnlockAchievement("ach_char_jack_o");
                    Destroy(this);
                }
            }
        }
    }

    // 17. БАЗЗ ЛАЙТЕР: Каскад гейзеров на парящий в небесах Шаттл Звёздного Командования (Y=+26м) + Дредноут Зурга!
    public class StarCommandSkyShuttleEncounter : Entity
    {
        private bool _boarded = false;
        private CombatTarget? _zurg = null;
        private readonly Vector3 _shuttlePos;
        private readonly Entity _labelEnt;

        public StarCommandSkyShuttleEncounter(Vector3 pos) : base(position: pos)
        {
            _shuttlePos = pos + new Vector3(0f, 26f, 0f);
            // Высотная платформа шаттла в небесах!
            var shuttle = new Entity(parent: this, position: new(0, 26f, 0));
            new Entity(parent: shuttle, model: "cube", scale: new(9.5f, 1.0f, 9.5f), position: new(0, -0.5f, 0), color: Rgb(238, 242, 250));
            new Entity(parent: shuttle, model: "diamond", scale: new(3.5f, 2.4f, 5.5f), position: new(0, 1.2f, 0), color: Color.Lime, unlit: true);
            Physics.AddSolidBox(pos.X, pos.Z, 9.5f, 9.5f, 1.0f, 0f, pos.Y + 25.0f, walkableTop: true);

            // Прыжковый грави-гейзер внизу
            var padPos = Physics.FindSafeSpawnPoint(pos + new Vector3(5.5f, 0f, 0f));
            WorldContext.Game?.StageEntities.Add(new LunarBouncePad(padPos, 31.5f, 0f));
            new LunarBouncePad(padPos, 31.5f, 0f);

            _labelEnt = new Entity(parent: this, position: new(0, 3.2f, 0), color: Color.Lime)
            {
                BillboardText = "Грави-Лифт Звёздного Командования\n[Взлетите на гейзере рядом на небесный Шаттл XP-38 (высота 26м)!]"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist2D = Physics.Distance2D(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && dist2D < 20f;

            if (!_boarded && Vector3.Distance(g.PlayerPos, _shuttlePos) <= 5.5f)
            {
                _boarded = true;
                _zurg = CharacterUnlockEncounters.SpawnEncounterEnemy(g, Position + new Vector3(8f, 0f, 8f), "zurg_mech", mult: 0.90f);
                SoundManager.Play("boss_roar");
                g.ShowBanner("ШАТТЛ XP-38 ДОСТИГНУТ! СОКРУШИТЕ ДРЕДНОУТ ИМПЕРАТОРА ЗУРГА!", Color.Lime, 4.0f);
            }
            else if (_boarded && (_zurg == null || !IsValid(_zurg) || _zurg.Hp <= 0f || _zurg.IsAlly))
            {
                g.UnlockAchievement("ach_char_buzz");
                Destroy(this);
            }
        }
    }

    // 18. АЛТАРЬ АЛКИОНА (FALSE SON): Накопление золота -> Золотой Портал на «Изначальный Меридиан»!
    public class HalcyonGoldShrineEncounter : Entity
    {
        private int _goldPaid = 0;
        private readonly Entity _labelEnt;

        public HalcyonGoldShrineEncounter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "diamond", scale: new(1.8f, 3.4f, 1.8f), position: new(0, 1.8f, 0), color: Color.Gold, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 4.0f, 0), color: Color.Gold)
            {
                BillboardText = "[E] Золотой Алтарь Алкиона (Внести $40 -> 0 / $120 для Портала Меридиана)"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = !Program.IsSpectating && dist < 15f;

            if (dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                if (g.Gold >= 40)
                {
                    g.Gold -= 40;
                    _goldPaid += 40;
                    SoundManager.Play("coin_flip", 1.0f, 1.15f);
                    _labelEnt.BillboardText = $"[E] Золотой Алтарь Алкиона (${_goldPaid} / $120)";
                    if (_goldPaid >= 120)
                    {
                        Vector3 pp = Physics.FindSafeSpawnPoint(Position + new Vector3(4.5f, 0f, 0f));
                        g.StageEntities.Add(new SecretStagePortal(pp, "prime_meridian"));
                        SoundManager.Play("portal_open");
                        g.ShowBanner("★ ЗОЛОТОЙ ПОРТАЛ ОТКРЫТ! ВОЙДИТЕ НА «ИЗНАЧАЛЬНЫЙ МЕРИДИАН»! ★", Color.Gold, 4.2f);
                        Destroy(this);
                    }
                }
                else
                {
                    SoundManager.Play("ui_error");
                    g.ShowBanner("НУЖНО $40 ЗОЛОТА ДЛЯ ПОДНОШЕНИЯ АЛКИОНУ!", Color.Orange, 1.8f);
                }
            }
        }
    }
}
