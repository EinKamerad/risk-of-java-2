using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using RiskOfCSharp2.Models;
using RiskOfCSharp2.World;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Entities
{
    public partial class CombatTarget
    {
        // Новые статусы уникальных механик героев
        public float SilenceTimer = 0f;
        public float LeashTimer = 0f;
        public Vector3 LeashOrigin = Vector3.Zero;
        public float ElemFireTimer = 0f;
        public float ElemIceTimer = 0f;
        public float ElemShockTimer = 0f;
        public float HuntressMarkTimer = 0f;
        public float WeakpointScanTimer = 0f;
        public float D12StickyTimer = 0f;
        public float D12AccumDmg = 0f;
        public float TheWorldStoredDmg = 0f;
        public float VergilFreezeTimer = 0f;
        public float PunishingGrudgeTimer = 0f;
        public int PrevWindupState = 0;
        public float FairyPunishIcd = 0f;

        public void TriggerBinahFairyAttackPunish()
        {
            if (!IsValid(this) || IsAlly || FairyTimer <= 0f || FairyPunishIcd > 0f) return;
            FairyPunishIcd = 0.42f;
            float punishDmg = FairyDps * 0.90f + MathF.Min(FairyDps * 3.5f, MaxHp * 0.015f);
            Vector3 p = Position + new Vector3(0, HitHeight * 0.65f, 0);
            new VisualFX(p, Rgb(245, 200, 65), 2.1f, 0.20f);
            new LightningBeam(p + new Vector3(-1.1f, 0.9f, 0), p + new Vector3(1.1f, -0.7f, 0), Rgb(250, 210, 75), 0.16f);
            TakeDamage(punishDmg);
        }

        public void ApplyArtificerElement(string elem, float refBaseDmg)
        {
            if (!IsValid(this) || IsAlly) return;
            Vector3 center = Position + new Vector3(0, HitHeight * 0.6f, 0);

            if (elem == "fire")
            {
                if (ElemIceTimer > 0f)
                {
                    ElemIceTimer = 0f;
                    float meltDmg = refBaseDmg * 1.95f + MathF.Min(refBaseDmg * 4.5f, MaxHp * 0.04f);
                    new VisualFX(center, Rgb(255, 195, 135), 4.2f, 0.30f);
                    SoundManager.Play("explosion", 0.72f, 1.32f);
                    TakeDamage(meltDmg);
                    WorldContext.Game?.ShowBanner("ЭЛЕМЕНТАЛЬНЫЙ РЕЗОНАНС: ТАЯНИЕ! (ПАРОВОЙ ВЗРЫВ)", Rgb(255, 185, 115), 1.2f);
                    return;
                }
                if (ElemShockTimer > 0f)
                {
                    ElemShockTimer = 0f;
                    new VisualFX(center, Rgb(255, 95, 165), 5.0f, 0.32f);
                    SoundManager.Play("explosion", 0.80f, 1.12f);
                    foreach (var other in CombatManager.Enemies.ToArray())
                    {
                        if (IsValid(other) && !other.IsAlly && Vector3.Distance(Position, other.Position) <= 6.5f)
                        {
                            other.TakeDamage(refBaseDmg * 1.75f);
                            if (!other.IsBoss) other.VelY = MathF.Max(other.VelY, 8.5f);
                        }
                    }
                    WorldContext.Game?.ShowBanner("ЭЛЕМЕНТАЛЬНЫЙ РЕЗОНАНС: ПЕРЕГРУЗКА!", Rgb(255, 95, 175), 1.2f);
                    return;
                }
                ElemFireTimer = 4.5f;
                LiandryBurnTimer = MathF.Max(LiandryBurnTimer, 3.0f);
                LiandryBurnDps = MathF.Max(LiandryBurnDps, refBaseDmg * 0.45f);
            }
            else if (elem == "ice")
            {
                if (ElemFireTimer > 0f)
                {
                    ElemFireTimer = 0f;
                    float meltDmg = refBaseDmg * 2.15f + MathF.Min(refBaseDmg * 4.5f, MaxHp * 0.04f);
                    new VisualFX(center, Rgb(165, 235, 255), 4.4f, 0.30f);
                    SoundManager.Play("explosion", 0.72f, 1.38f);
                    TakeDamage(meltDmg);
                    WorldContext.Game?.ShowBanner("ТЕПЛОВОЙ ШОК / ТАЯНИЕ! (x2.2 РАСКОЛ)", Rgb(145, 235, 255), 1.2f);
                    return;
                }
                if (ElemShockTimer > 0f)
                {
                    ElemShockTimer = 0f;
                    ArmorBreakTimer = MathF.Max(ArmorBreakTimer, 6.5f);
                    SlowTimer = MathF.Max(SlowTimer, 4.0f);
                    new VisualFX(center, Rgb(125, 225, 255), 4.8f, 0.30f);
                    SoundManager.Play("zap", 0.78f, 1.25f);
                    TakeDamage(refBaseDmg * 1.65f);
                    WorldContext.Game?.ShowBanner("ЭЛЕМЕНТАЛЬНЫЙ РЕЗОНАНС: СВЕРХПРОВОДИМОСТЬ! (-БРОНЯ)", Rgb(125, 235, 255), 1.2f);
                    return;
                }
                ElemIceTimer = 4.5f;
                SlowTimer = MathF.Max(SlowTimer, 3.2f);
            }
            else if (elem == "shock")
            {
                if (ElemFireTimer > 0f)
                {
                    ApplyArtificerElement("fire", refBaseDmg);
                    return;
                }
                if (ElemIceTimer > 0f)
                {
                    ElemIceTimer = 0f;
                    ArmorBreakTimer = MathF.Max(ArmorBreakTimer, 6.5f);
                    new VisualFX(center, Rgb(135, 235, 255), 4.6f, 0.30f);
                    TakeDamage(refBaseDmg * 1.75f);
                    return;
                }
                ElemShockTimer = 4.5f;
            }
        }
    }

    // =================================================================================================
    // УНИКАЛЬНЫЕ 3D-СУЩНОСТИ СПОСОБНОСТЕЙ (ЗАМЕНА ОРБИТАЛЬНЫХ УДАРОВ И ЧУЖИХ ЭФФЕКТОВ)
    // =================================================================================================

    public class ArtificerFlamethrowerEntity : Entity
    {
        private readonly SurvivorCombatController _owner;
        private float _life = 2.3f;
        private float _tick = 0f;

        public ArtificerFlamethrowerEntity(SurvivorCombatController owner)
            : base(position: owner.Run.PlayerPos + new Vector3(0, 1.35f, 0))
        {
            _owner = owner;
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            if (_owner == null || _owner.CharKey != "artificer" || _owner.Run.PlayerHp <= 0f)
            {
                Destroy(this);
                return;
            }
            _life -= dt;
            if (_life <= 0f) { Destroy(this); return; }

            Vector3 aim = _owner.GetAimDirection();
            Vector3 right = Vector3.Cross(aim, Vector3.UnitY);
            right = right.LengthSquared() > 0.001f ? Vector3.Normalize(right) : Vector3.UnitX;
            Vector3 origin = _owner.Run.PlayerPos + new Vector3(0, 1.38f, 0) + aim * 0.75f;
            Position = origin;

            _tick -= dt;
            if (_tick <= 0f)
            {
                _tick = 0.11f;
                SoundManager.Play("flame", 0.55f, 1.05f);
                Vector3 end = origin + aim * 17.5f;
                new LightningBeam(origin - right * 0.32f, end, Rgb(255, 115, 25), 0.14f);
                new LightningBeam(origin + right * 0.32f, end, Rgb(255, 215, 65), 0.14f);
                new VisualFX(origin + aim * 8.5f, Rgb(255, 130, 35), 2.4f, 0.16f);

                Vector3 seg = end - origin;
                float lenSq = MathF.Max(0.01f, seg.LengthSquared());
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly) continue;
                    Vector3 c = e.Position + new Vector3(0, e.HitHeight * 0.55f, 0);
                    float t = Math.Clamp(Vector3.Dot(c - origin, seg) / lenSq, 0f, 1f);
                    if (Vector3.Distance(c, origin + seg * t) <= e.HitRadius + 1.85f)
                    {
                        e.ApplyArtificerElement("fire", _owner.BaseDamage);
                        CombatManager.ApplyPlayerHitEffects(e, c, _owner.BaseDamage * 0.76f, Rgb(255, 125, 30), aoe: 1.4f, lifesteal: 1.5f, canProc: true);
                    }
                }
            }
        }
    }

    public class ArtificerIonSphereEntity : Entity
    {
        private float _life = 5.5f;
        private float _zapTick = 0.15f;
        private readonly float _dmg;
        private readonly Entity _ring;

        public ArtificerIonSphereEntity(Vector3 pos, float dmg)
            : base(model: "sphere", position: pos, scaleUniform: 1.35f, color: Rgb(85, 215, 255), unlit: true)
        {
            _dmg = dmg;
            new Entity(parent: this, model: "sphere", scaleUniform: 1.45f, color: Rgb(145, 240, 255, 95), unlit: true);
            _ring = new Entity(parent: this, model: "diamond", scale: new(2.4f, 0.15f, 2.4f), color: Color.White, unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            _ring.RotationY += 320f * dt;
            RotationY += 160f * dt;
            if (_life <= 0f)
            {
                new VisualFX(Position, Color.SkyBlue, 5.5f, 0.32f);
                Destroy(this);
                return;
            }
            _zapTick -= dt;
            if (_zapTick <= 0f)
            {
                _zapTick = 0.45f;
                int hits = 0;
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly || Vector3.Distance(Position, e.Position) > 16.5f) continue;
                    Vector3 hp = e.Position + new Vector3(0, e.HitHeight * 0.6f, 0);
                    new LightningBeam(Position, hp, Color.SkyBlue, 0.22f);
                    e.ApplyArtificerElement("shock", _dmg * 0.65f);
                    CombatManager.ApplyPlayerHitEffects(e, hp, _dmg, Color.SkyBlue, aoe: 1.8f, canProc: true);
                    if (++hits >= 4) break;
                }
                if (hits > 0) SoundManager.Play("zap", 0.55f, 1.25f);
            }
        }
    }

    public class BuzzStarLaserSweepEntity : Entity
    {
        private Vector3 _dir;
        private readonly float _dmg;
        private float _life = 2.4f;
        private float _tick = 0f;

        public BuzzStarLaserSweepEntity(Vector3 startPos, Vector3 dir, float dmg) : base(position: startPos)
        {
            _dir = dir.LengthSquared() > 0.001f ? Vector3.Normalize(new Vector3(dir.X, 0f, dir.Z)) : Vector3.UnitZ;
            _dmg = dmg;
            new Entity(parent: this, model: "sphere", scale: new(3.6f, 0.16f, 3.6f), position: new(0, 0.12f, 0), color: Rgb(95, 255, 115, 135), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            if (_life <= 0f) { Destroy(this); return; }
            Position += _dir * 16.0f * dt;
            Y = Physics.GetSurfaceHeight(X, Z, Y + 2f);

            _tick -= dt;
            if (_tick <= 0f)
            {
                _tick = 0.16f;
                new LightningBeam(Position + new Vector3(0, 26f, 0), Position, Color.Lime, 0.22f);
                new VisualFX(Position + new Vector3(0, 0.5f, 0), Color.Lime, 2.8f, 0.20f);
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Physics.Distance2D(Position, e.Position) <= 4.4f + e.HitRadius)
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, _dmg, Color.Lime, aoe: 1.8f, canProc: true);
                }
            }
        }
    }

    public class ZurgHornetBotEntity : Entity
    {
        public float Hp = 110f;
        public float HitRadius = 0.95f;
        private float _life = 14.0f;
        private float _shotCd = 0.45f;
        private readonly int _slot;
        private float _orbitAng;

        public ZurgHornetBotEntity(Vector3 pos, int slot) : base(position: pos)
        {
            _slot = slot;
            _orbitAng = slot * MathF.PI;
            new Entity(parent: this, model: "sphere", scale: new(0.62f, 0.52f, 0.72f), color: Rgb(145, 42, 195));
            new Entity(parent: this, model: "diamond", scale: new(1.25f, 0.10f, 0.35f), position: new(0, 0.26f, 0), color: Color.Gold, unlit: true);
            new Entity(parent: this, model: "sphere", scaleUniform: 0.24f, position: new(0, 0.04f, 0.36f), color: Rgb(255, 55, 65), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var c = CombatManager.Controller;
            if (c == null || c.CharKey != "buzz") { Destroy(this); return; }
            _life -= dt;
            if (_life <= 0f || Hp <= 0f)
            {
                new VisualFX(Position, Rgb(185, 65, 255), 2.8f, 0.25f);
                Destroy(this);
                return;
            }
            _orbitAng += 2.2f * dt;
            Vector3 desired = c.Run.PlayerPos + new Vector3(MathF.Cos(_orbitAng) * 3.4f, 2.2f + MathF.Sin(_orbitAng * 2f) * 0.25f, MathF.Sin(_orbitAng) * 3.4f);
            Position = Vector3.Lerp(Position, desired, Math.Clamp(dt * 8f, 0f, 1f));

            _shotCd -= dt;
            if (_shotCd <= 0f)
            {
                _shotCd = 0.65f;
                var t = CombatManager.FindNearestHostileAt(Position);
                if (t != null && Vector3.Distance(Position, t.Position) <= 30f)
                {
                    LookAt2D(t.Position);
                    Vector3 aim = Vector3.Normalize((t.Position + new Vector3(0, t.HitHeight * 0.6f, 0)) - Position);
                    new Projectile(Position, aim, 54f, c.BaseDamage * 0.72f, 0.28f, Rgb(215, 65, 255), isPlayer: true, aoe: 1.5f, canProc: true);
                }
            }
        }
    }

    public class ZurgGravityVortexEntity : Entity
    {
        private float _life = 4.2f;
        private float _tick = 0.2f;
        private readonly float _dmg;

        public ZurgGravityVortexEntity(Vector3 pos, float dmg)
            : base(model: "sphere", position: pos + new Vector3(0, 1.0f, 0), scaleUniform: 1.25f, color: Rgb(95, 25, 145), unlit: true)
        {
            _dmg = dmg;
            new Entity(parent: this, model: "diamond", scale: new(3.6f, 0.14f, 3.6f), color: Rgb(235, 65, 255, 145), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            RotationY += 340f * dt;
            if (_life <= 0f)
            {
                new VisualFX(Position, Rgb(215, 55, 255), 6.2f, 0.35f);
                Destroy(this);
                return;
            }
            foreach (var e in CombatManager.Enemies)
            {
                if (!IsValid(e) || e.IsAlly) continue;
                float d = Physics.Distance2D(Position, e.Position);
                if (d > 1.0f && d <= 12.5f)
                {
                    Vector3 pull = Vector3.Normalize(new Vector3(X - e.X, 0f, Z - e.Z));
                    e.Position += pull * ((e.IsBoss ? 3.8f : 9.2f) * dt);
                }
            }
            _tick -= dt;
            if (_tick <= 0f)
            {
                _tick = 0.55f;
                new VisualFX(Position, Rgb(185, 55, 245), 3.4f, 0.22f);
                foreach (var e in CombatManager.Enemies.ToArray())
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position) <= 7.5f)
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, _dmg, Rgb(215, 65, 255), canProc: true);
            }
        }
    }

    public class M4JupiterCannonEntity : Entity
    {
        private float _life = 4.6f;
        private float _shotTimer = 0.25f;
        private int _shotsLeft = 3;
        private readonly Entity _barrel;

        public M4JupiterCannonEntity(Vector3 pos, float rotY) : base(position: pos, rotation: new(0, rotY, 0))
        {
            new Entity(parent: this, model: "cube", scale: new(1.8f, 0.45f, 1.8f), position: new(0, 0.22f, 0), color: Rgb(42, 48, 58));
            _barrel = new Entity(parent: this, position: new(0, 1.05f, 0));
            new Entity(parent: _barrel, model: "cube", scale: new(0.65f, 0.65f, 2.8f), position: new(0, 0, 0.7f), color: Rgb(55, 64, 76));
            new Entity(parent: _barrel, model: "diamond", scale: new(0.95f, 0.35f, 2.4f), position: new(0, 0.22f, 0.7f), color: Rgb(95, 255, 135), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var c = CombatManager.Controller;
            if (c == null || c.CharKey != "m4a1") { Destroy(this); return; }
            _life -= dt;
            if (_life <= 0f || _shotsLeft <= 0) { Destroy(this, 0.35f); return; }

            _shotTimer -= dt;
            if (_shotTimer <= 0f)
            {
                _shotTimer = 1.05f;
                _shotsLeft--;
                var target = CombatManager.FindNearestHostileAt(Position);
                Vector3 aim = target != null
                    ? Vector3.Normalize((target.Position + new Vector3(0, target.HitHeight * 0.6f, 0)) - (Position + new Vector3(0, 1.1f, 0)))
                    : c.GetAimDirection();
                if (target != null) _barrel.LookAt2D(target.Position);

                int linkedClones = 0;
                foreach (var cl in c.DummyClones)
                {
                    if (IsValid(cl))
                    {
                        linkedClones++;
                        new LightningBeam(cl.Position + new Vector3(0, 1.3f, 0), Position + new Vector3(0, 1.2f, 0), Rgb(95, 255, 135), 0.28f);
                    }
                }
                new LightningBeam(c.Run.PlayerPos + new Vector3(0, 1.3f, 0), Position + new Vector3(0, 1.2f, 0), Color.Gold, 0.28f);

                SoundManager.Play("railgun", 1.0f, 0.88f);
                Vector3 muzzle = Position + new Vector3(0, 1.15f, 0) + aim * 1.6f;
                new VisualFX(muzzle, Rgb(95, 255, 135), 4.5f, 0.32f);
                float mult = 3.8f + linkedClones * 0.85f;
                new Projectile(muzzle, aim, 86f, c.BaseDamage * mult, 0.85f, Rgb(95, 255, 135), isPlayer: true, aoe: 6.5f, pierce: true, canProc: true);
            }
        }
    }

    public class AthenaPersonaEntity : Entity
    {
        private float _life = 5.5f;
        private float _strikeTick = 0.20f;

        public AthenaPersonaEntity(Vector3 pos) : base(position: pos)
        {
            // Монументальный 3D-силуэт Персоны Афины с золотым шлемом, копьём и щитом Палладион
            new Entity(parent: this, model: "diamond", scale: new(1.1f, 2.4f, 0.85f), position: new(0, 2.2f, 0), color: Rgb(125, 215, 255, 195), unlit: true);
            new Entity(parent: this, model: "sphere", scaleUniform: 0.55f, position: new(0, 3.65f, 0), color: Color.White, unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(0.28f, 0.85f, 0.95f), position: new(0, 4.1f, 0), color: Color.Gold, unlit: true);
            // Копьё и Щит
            new Entity(parent: this, model: "cube", scale: new(0.14f, 3.8f, 0.14f), position: new(0.85f, 2.4f, 0.35f), rotation: new(20, 0, 0), color: Color.Gold, unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(0.22f, 1.65f, 1.25f), position: new(-0.85f, 2.2f, 0.35f), color: Rgb(235, 195, 55), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var c = CombatManager.Controller;
            if (c == null || c.CharKey != "aigis") { Destroy(this); return; }
            _life -= dt;
            if (_life <= 0f) { Destroy(this); return; }

            Vector3 follow = c.Run.PlayerPos - c.Run.PlayerForward * 1.5f + new Vector3(0, 0.45f + MathF.Sin(_life * 4f) * 0.18f, 0);
            Position = Vector3.Lerp(Position, follow, Math.Clamp(dt * 10f, 0f, 1f));
            RotationY = c.Run.PlayerEntity.RotationY;

            _strikeTick -= dt;
            if (_strikeTick <= 0f)
            {
                _strikeTick = 0.85f;
                var t = CombatManager.FindNearestHostileAt(c.Run.PlayerPos);
                if (t != null && Vector3.Distance(c.Run.PlayerPos, t.Position) <= 28f)
                {
                    Vector3 hp = t.Position + new Vector3(0, t.HitHeight * 0.6f, 0);
                    new LightningBeam(Position + new Vector3(0.85f, 3.2f, 0), hp, Color.Gold, 0.32f);
                    new VisualFX(hp, Color.SkyBlue, 3.8f, 0.28f);
                    SoundManager.Play("shoot_heavy", 0.72f, 1.25f);
                    CombatManager.ApplyPlayerHitEffects(t, hp, c.BaseDamage * 2.45f, Color.Gold, aoe: 3.6f, canProc: true);
                }
            }
        }
    }

    public class MindustryAssaultDroneEntity : Entity
    {
        public float Hp = 140f;
        public float HitRadius = 1.0f;
        private float _life = 26f;
        private float _shotCd = 0.35f;
        private float _orbit;

        public MindustryAssaultDroneEntity(Vector3 pos, float seed) : base(position: pos + new Vector3(0, 2.0f, 0))
        {
            _orbit = seed;
            new Entity(parent: this, model: "diamond", scale: new(1.15f, 0.38f, 1.35f), color: Rgb(255, 195, 55));
            foreach (int s in new[] { -1, 1 })
                new Entity(parent: this, model: "cube", scale: new(0.85f, 0.12f, 0.45f), position: new(s * 0.75f, 0, -0.1f), color: Rgb(65, 78, 96));
            new Entity(parent: this, model: "sphere", scaleUniform: 0.32f, position: new(0, 0.12f, 0.35f), color: Color.SkyBlue, unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var c = CombatManager.Controller;
            if (c == null || c.CharKey != "mindustry" || Hp <= 0f || (_life -= dt) <= 0f)
            {
                new VisualFX(Position, Color.Gold, 2.6f, 0.24f);
                Destroy(this);
                return;
            }
            _orbit += 1.8f * dt;
            var enemy = CombatManager.FindNearestHostileAt(Position);
            Vector3 anchor = enemy != null && Vector3.Distance(c.Run.PlayerPos, enemy.Position) <= 42f
                ? enemy.Position + new Vector3(MathF.Cos(_orbit) * 6.5f, 3.2f, MathF.Sin(_orbit) * 6.5f)
                : c.Run.PlayerPos + new Vector3(MathF.Cos(_orbit) * 5.0f, 3.2f, MathF.Sin(_orbit) * 5.0f);
            Position = Vector3.Lerp(Position, anchor, Math.Clamp(dt * 4.5f, 0f, 1f));
            if (enemy != null) LookAt2D(enemy.Position);

            _shotCd -= dt * (c.MindustryOverdriveTimer > 0f ? 1.8f : 1.0f);
            if (_shotCd <= 0f && enemy != null && Vector3.Distance(Position, enemy.Position) <= 34f)
            {
                _shotCd = 0.55f;
                Vector3 aim = Vector3.Normalize((enemy.Position + new Vector3(0, enemy.HitHeight * 0.6f, 0)) - Position);
                new Projectile(Position, aim, 62f, c.BaseDamage * 0.85f, 0.28f, Color.Gold, isPlayer: true, aoe: 1.8f, canProc: true);
            }
        }
    }

    public class AbyssalTentacleEntity : Entity
    {
        private float _delay;
        private bool _erupted = false;
        private float _hold = 0.85f;
        private readonly float _dmg;
        private Entity? _stalk;

        public AbyssalTentacleEntity(Vector3 pos, float delay, float dmg) : base(position: pos)
        {
            _delay = delay;
            _dmg = dmg;
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            if (!_erupted)
            {
                _delay -= dt;
                if (_delay <= 0f)
                {
                    _erupted = true;
                    Y = Physics.GetSurfaceHeight(X, Z, Y + 2f);
                    _stalk = new Entity(parent: this, model: "diamond", scale: new(1.45f, 0.2f, 1.45f), position: new(0, 0.2f, 0), color: Rgb(38, 165, 145));
                    new Entity(parent: _stalk, model: "sphere", scaleUniform: 0.42f, position: new(0.35f, 0.25f, 0), color: Rgb(95, 245, 215), unlit: true);
                    _stalk.AnimateScale(new Vector3(1.45f, 6.2f, 1.45f), 0.18f);
                    _stalk.AnimatePosition(new Vector3(0, 2.8f, 0), 0.18f);
                    new VisualFX(Position + new Vector3(0, 0.6f, 0), Rgb(65, 225, 215), 3.8f, 0.28f);

                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (IsValid(e) && !e.IsAlly && Physics.Distance2D(Position, e.Position) <= 3.8f + e.HitRadius)
                        {
                            if (!e.IsBoss)
                            {
                                e.StunTimer = MathF.Max(e.StunTimer, 2.4f);
                                e.VelY = MathF.Max(e.VelY, 13.5f);
                                e.IsGrounded = false;
                            }
                            CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, _dmg, Rgb(65, 225, 215), aoe: 2.0f, canProc: true);
                        }
                    }
                }
                return;
            }
            _hold -= dt;
            if (_hold <= 0f) Destroy(this);
        }
    }

    public class FreemanThrownMonsterEntity : Entity
    {
        private Vector3 _vel;
        private readonly CombatTarget _victim;
        private readonly float _impactDmg;
        private float _life = 1.8f;

        public FreemanThrownMonsterEntity(CombatTarget victim, Vector3 startPos, Vector3 dir, float impactDmg)
            : base(position: startPos)
        {
            _victim = victim;
            _vel = Vector3.Normalize(dir) * 62f;
            _impactDmg = impactDmg;
            new Entity(parent: this, model: "sphere", scaleUniform: 1.55f, color: Rgb(75, 215, 255, 130), unlit: true);
        }

        private void Detonate()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            SoundManager.Play("explosion", 0.95f, 1.05f);
            new VisualFX(Position, Color.SkyBlue, 7.2f, 0.36f);
            if (IsValid(_victim))
            {
                _victim.Position = new Vector3(X, Physics.GetSurfaceHeight(X, Z, Y + 2f), Z);
                _victim.StunTimer = MathF.Max(_victim.StunTimer, 2.2f);
                CombatManager.ApplyPlayerHitEffects(_victim, Position, _impactDmg * 1.25f, Color.SkyBlue, aoe: 3.0f, canProc: true);
            }
            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (IsValid(e) && !e.IsAlly && e != _victim && Vector3.Distance(Position, e.Position) <= 6.8f)
                {
                    if (!e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, 1.8f);
                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, _impactDmg, Color.SkyBlue, canProc: true);
                }
            }
            Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            _vel.Y -= 8.0f * dt;
            Position += _vel * dt;
            if (IsValid(_victim))
            {
                _victim.Position = Position;
                _victim.StunTimer = 0.5f;
            }
            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly && e != _victim && Vector3.Distance(Position, e.Position + Vector3.UnitY) <= e.HitRadius + 1.4f)
                {
                    Detonate();
                    return;
                }
            }
            float fy = Physics.GetSurfaceHeight(X, Z, Y);
            if (Y <= fy + 0.35f || Physics.IsInsideSolidObstacle(Position, 0.35f) || _life <= 0f)
                Detonate();
        }
    }

    public class DanteSinnerProjectionEntity : Entity
    {
        private float _life = 0.95f;
        private bool _struck = false;
        private readonly CombatTarget? _target;
        private readonly float _dmg;
        private readonly Color _col;

        public DanteSinnerProjectionEntity(Vector3 pos, CombatTarget? target, float dmg, string sinnerTitle, Color col)
            : base(position: pos)
        {
            _target = target;
            _dmg = dmg;
            _col = col;
            new Entity(parent: this, model: "diamond", scale: new(0.85f, 1.95f, 0.65f), position: new(0, 1.15f, 0), color: new Color(col.R, col.G, col.B, (byte)185), unlit: true);
            new Entity(parent: this, model: "sphere", scaleUniform: 0.42f, position: new(0, 2.25f, 0), color: Color.White, unlit: true);
            new Entity(parent: this, model: "cube", scale: new(0.12f, 0.12f, 2.1f), position: new(0.45f, 1.3f, 0.65f), color: Color.Gold, unlit: true);
            new Entity(parent: this, position: new(0, 2.85f, 0), color: col) { BillboardText = sinnerTitle };
            if (target != null) LookAt2D(target.Position);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            if (!_struck && _life <= 0.72f)
            {
                _struck = true;
                Vector3 hitPos = IsValid(_target) ? _target!.Position + new Vector3(0, _target.HitHeight * 0.6f, 0) : Position + Forward * 3.0f + Vector3.UnitY;
                new LightningBeam(Position + new Vector3(0, 1.3f, 0), hitPos, _col, 0.28f);
                new VisualFX(hitPos, Color.Gold, 3.6f, 0.26f);
                SoundManager.Play("melee_swing", 0.7f, 1.18f);
                if (IsValid(_target) && !_target!.IsAlly)
                {
                    _target.ArmorBreakTimer = MathF.Max(_target.ArmorBreakTimer, 4.5f);
                    CombatManager.ApplyPlayerHitEffects(_target, hitPos, _dmg, _col, aoe: 3.4f, lifesteal: 4.0f, canProc: true);
                }
                else
                {
                    foreach (var e in CombatManager.Enemies.ToArray())
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(hitPos, e.Position) <= 5.2f)
                            CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, _dmg, _col, canProc: true);
                }
            }
            if (_life <= 0f) Destroy(this);
        }
    }

    public class ChebFriendsHouseEntity : Entity
    {
        private float _life = 8.5f;
        private float _pulse = 0.25f;

        public ChebFriendsHouseEntity(Vector3 pos) : base(position: pos)
        {
            Y = Physics.GetSurfaceHeight(X, Z, Y + 1.5f);
            new Entity(parent: this, model: "cube", scale: new(2.4f, 1.8f, 2.4f), position: new(0, 0.9f, 0), color: Rgb(235, 165, 75));
            new Entity(parent: this, model: "diamond", scale: new(3.1f, 1.4f, 3.1f), position: new(0, 2.15f, 0), color: Rgb(215, 55, 55));
            new Entity(parent: this, model: "sphere", scale: new(11.0f, 0.10f, 11.0f), position: new(0, 0.10f, 0), color: Rgb(255, 195, 65, 75), unlit: true);
            new Entity(parent: this, position: new(0, 3.35f, 0), color: Color.Gold) { BillboardText = "ДОМИК ДРУЗЕЙ" };
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var g = WorldContext.Game;
            var c = CombatManager.Controller;
            _life -= dt;
            if (g == null || c == null || _life <= 0f) { Destroy(this); return; }

            _pulse -= dt;
            if (_pulse <= 0f)
            {
                _pulse = 1.0f;
                new VisualFX(Position + new Vector3(0, 1.5f, 0), Color.Gold, 5.5f, 0.28f);
                if (Vector3.Distance(g.PlayerPos, Position) <= 12.0f)
                {
                    g.HealPlayer(12.0f);
                    c.Shield = MathF.Min(g.PlayerMaxHp, c.Shield + 14.0f);
                }
                foreach (var e in CombatManager.Enemies.ToArray())
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position) <= 10.5f)
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, c.BaseDamage * 1.45f, Color.Orange, canProc: true);
            }
        }
    }

    public class LapplandWolfSpiritEntity : Entity
    {
        private CombatTarget? _target;
        private readonly float _dmg;
        private readonly Color _col;
        private float _life = 2.4f;

        public LapplandWolfSpiritEntity(Vector3 startPos, CombatTarget? target, float dmg, Color col)
            : base(position: startPos)
        {
            _target = target;
            _dmg = dmg;
            _col = col;
            new Entity(parent: this, model: "diamond", scale: new(0.75f, 0.65f, 1.85f), color: col, unlit: true);
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: this, model: "diamond", scale: new(0.18f, 0.45f, 0.22f), position: new(s * 0.25f, 0.42f, 0.35f), color: Color.White, unlit: true);
                new Entity(parent: this, model: "sphere", scaleUniform: 0.14f, position: new(s * 0.18f, 0.14f, 0.65f), color: Color.Red, unlit: true);
            }
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            if (_life <= 0f) { Destroy(this); return; }
            if (!IsValid(_target) || _target!.IsAlly)
                _target = CombatManager.FindNearestHostileAt(Position);
            if (_target == null)
            {
                Position += Forward * 42f * dt;
                return;
            }
            Vector3 goal = _target.Position + new Vector3(0, _target.HitHeight * 0.6f, 0);
            LookAt2D(_target.Position);
            Vector3 toG = goal - Position;
            if (toG.Length() <= _target.HitRadius + 1.2f)
            {
                _target.SilenceTimer = MathF.Max(_target.SilenceTimer, 5.0f);
                _target.ArmorBreakTimer = MathF.Max(_target.ArmorBreakTimer, 5.0f);
                new VisualFX(goal, _col, 3.6f, 0.26f);
                CombatManager.ApplyPlayerHitEffects(_target, goal, _dmg, _col, aoe: 3.2f, lifesteal: 4.5f, canProc: true);
                Destroy(this);
                return;
            }
            Position += Vector3.Normalize(toG) * 48f * dt;
        }
    }

    public class DecadenzaWolfFamiliarEntity : Entity
    {
        public readonly int Slot;
        private float _orbit;
        private float _biteCd = 0.5f;
        private CombatTarget? _focusTarget;
        private int _frenzyStacks = 0;

        public DecadenzaWolfFamiliarEntity(Vector3 pos, int slot) : base(position: pos)
        {
            Slot = slot;
            _orbit = slot * MathF.PI;
            Color c = slot == 0 ? Rgb(215, 175, 65) : Rgb(195, 75, 255);
            new Entity(parent: this, model: "diamond", scale: new(0.68f, 0.58f, 1.65f), color: c, unlit: true);
            foreach (int s in new[] { -1, 1 })
                new Entity(parent: this, model: "diamond", scale: new(0.16f, 0.42f, 0.20f), position: new(s * 0.22f, 0.38f, 0.32f), color: Color.White, unlit: true);
        }

        public void CommandFocus(CombatTarget t)
        {
            if (_focusTarget != t) { _focusTarget = t; _frenzyStacks = 0; }
            else _frenzyStacks = Math.Min(6, _frenzyStacks + 1);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var c = CombatManager.Controller;
            if (c == null || c.CharKey != "lappland" || ProgressData.Instance.GetSelectedSkin("lappland") != 2)
            {
                Destroy(this);
                return;
            }
            _orbit += 2.3f * dt;
            if (!IsValid(_focusTarget) || _focusTarget!.IsAlly || Vector3.Distance(c.Run.PlayerPos, _focusTarget.Position) > 36f)
            {
                _focusTarget = CombatManager.FindNearestHostileAt(c.Run.PlayerPos);
                _frenzyStacks = 0;
            }

            Vector3 desired = IsValid(_focusTarget) && Vector3.Distance(c.Run.PlayerPos, _focusTarget!.Position) <= 30f
                ? _focusTarget.Position + new Vector3(MathF.Cos(_orbit) * 2.8f, _focusTarget.HitHeight + 0.8f, MathF.Sin(_orbit) * 2.8f)
                : c.Run.PlayerPos + new Vector3(MathF.Cos(_orbit) * 2.4f, 2.1f, MathF.Sin(_orbit) * 2.4f);

            Position = Vector3.Lerp(Position, desired, Math.Clamp(dt * 9.5f, 0f, 1f));
            if (IsValid(_focusTarget)) LookAt2D(_focusTarget!.Position);

            _biteCd -= dt * (1.0f + _frenzyStacks * 0.18f + (c.LapplandFormTimer > 0f ? 0.5f : 0f));
            if (_biteCd <= 0f && IsValid(_focusTarget) && Vector3.Distance(c.Run.PlayerPos, _focusTarget!.Position) <= 30f)
            {
                _biteCd = 0.85f;
                _frenzyStacks = Math.Min(6, _frenzyStacks + 1);
                Vector3 hp = _focusTarget.Position + new Vector3(0, _focusTarget.HitHeight * 0.6f, 0);
                _focusTarget.SilenceTimer = MathF.Max(_focusTarget.SilenceTimer, 4.0f);
                new VisualFX(hp, Rgb(215, 115, 255), 1.9f, 0.18f);
                CombatManager.ApplyPlayerHitEffects(_focusTarget, hp, c.BaseDamage * (0.62f + _frenzyStacks * 0.08f), Rgb(215, 115, 255), canProc: true);
            }
        }
    }

    public class WC4ClaymoreMineEntity : Entity
    {
        private float _armTimer = 0.45f;
        private float _life = 18.0f;
        private readonly float _dmg;
        private readonly Entity _led;

        public WC4ClaymoreMineEntity(Vector3 pos, float dmg) : base(position: pos)
        {
            _dmg = dmg;
            Y = Physics.GetSurfaceHeight(X, Z, Y + 1.5f);
            new Entity(parent: this, model: "cube", scale: new(0.65f, 0.24f, 0.45f), position: new(0, 0.12f, 0), color: Rgb(38, 38, 44));
            _led = new Entity(parent: this, model: "sphere", scaleUniform: 0.22f, position: new(0, 0.28f, 0), color: Color.Red, unlit: true);
            new Entity(parent: this, model: "sphere", scale: new(4.8f, 0.08f, 4.8f), position: new(0, 0.06f, 0), color: Rgb(255, 45, 55, 65), unlit: true);
        }

        public void Detonate(float mult = 1.0f)
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            SoundManager.Play("explosion", 0.90f, 1.05f);
            new VisualFX(Position + new Vector3(0, 0.6f, 0), Rgb(255, 65, 55), 6.2f, 0.35f);
            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position) <= 6.5f + e.HitRadius)
                {
                    if (!e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, 2.0f);
                    e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 6.0f);
                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, _dmg * mult, Rgb(255, 75, 55), aoe: 2.2f, canProc: true);
                }
            }
            Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            if (_life <= 0f) { Detonate(1.0f); return; }
            _led.Enabled = ((int)(_life * 6f) % 2) == 0;
            if (_armTimer > 0f) { _armTimer -= dt; return; }

            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly && Physics.Distance2D(Position, e.Position) <= 2.8f + e.HitRadius)
                {
                    Detonate(1.0f);
                    return;
                }
            }
        }
    }

    public class WisadelShadowTotemEntity : Entity
    {
        public float Hp = 190f;
        public float HitRadius = 1.15f;
        private float _life = 14.0f;
        private float _shotCd = 0.55f;
        private readonly Entity _core;

        public WisadelShadowTotemEntity(Vector3 pos) : base(position: pos)
        {
            Y = Physics.GetSurfaceHeight(X, Z, Y + 1.5f);
            new Entity(parent: this, model: "diamond", scale: new(1.1f, 2.6f, 1.1f), position: new(0, 1.5f, 0), color: Rgb(36, 26, 38));
            _core = new Entity(parent: this, model: "diamond", scale: new(1.35f, 0.95f, 1.35f), position: new(0, 2.4f, 0), color: Rgb(235, 38, 58), unlit: true);
            new Entity(parent: this, position: new(0, 3.35f, 0), color: Rgb(255, 85, 95)) { BillboardText = "ТЕНЬ ВОССТАВШИХ" };
        }

        public void TakeTotemDamage(float dmg)
        {
            Hp -= dmg;
            if (Hp <= 0f && !IsDestroyed)
            {
                new VisualFX(Position + Vector3.UnitY, Rgb(235, 38, 58), 4.5f, 0.30f);
                Destroy(this);
            }
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var c = CombatManager.Controller;
            if (c == null || c.CharKey != "w_arknights" || (_life -= dt) <= 0f || Hp <= 0f)
            {
                Destroy(this);
                return;
            }
            _core.RotationY += 210f * dt;
            _shotCd -= dt;
            if (_shotCd <= 0f)
            {
                _shotCd = 0.90f;
                var t = CombatManager.FindNearestHostileAt(Position);
                if (t != null && Vector3.Distance(Position, t.Position) <= 32f)
                {
                    Vector3 sp = Position + new Vector3(0, 2.4f, 0);
                    Vector3 aim = Vector3.Normalize((t.Position + new Vector3(0, t.HitHeight * 0.6f, 0)) - sp);
                    new Projectile(sp, aim, 52f, c.BaseDamage * 1.45f, 0.42f, Rgb(235, 42, 65), isPlayer: true, aoe: 3.5f, homing: true, canProc: true);
                }
            }
        }
    }

    public class GoldShipSkyAnchorEntity : Entity
    {
        private readonly float _dmg;
        private bool _landed = false;
        private float _fade = 0.55f;

        public GoldShipSkyAnchorEntity(Vector3 targetPos, float dmg)
            : base(position: targetPos + new Vector3(0, 18.0f, 0))
        {
            _dmg = dmg;
            new Entity(parent: this, model: "cube", scale: new(0.55f, 4.2f, 0.55f), color: Color.Gold, unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(3.6f, 0.95f, 0.75f), position: new(0, -1.6f, 0), color: Color.Gold, unlit: true);
            new Entity(parent: this, model: "sphere", scaleUniform: 0.95f, position: new(0, 2.2f, 0), color: Rgb(255, 235, 95), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            if (!_landed)
            {
                Y -= 58f * dt;
                float fy = Physics.GetSurfaceHeight(X, Z, Y + 3f);
                if (Y <= fy + 1.8f)
                {
                    Y = fy + 1.8f;
                    _landed = true;
                    SoundManager.Play("slam_land", 1.0f, 0.78f);
                    new VisualFX(new Vector3(X, fy + 0.8f, Z), Color.Gold, 9.5f, 0.42f);
                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(new Vector3(X, fy, Z), e.Position) <= 10.5f)
                        {
                            if (!e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, 2.2f);
                            CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, _dmg, Color.Gold, aoe: 3.0f, canProc: true);
                        }
                    }
                }
                return;
            }
            _fade -= dt;
            if (_fade <= 0f) Destroy(this);
        }
    }

    public class PushnoyTeslaTowerEntity : Entity
    {
        private float _life = 8.0f;
        private float _zapTick = 0.2f;
        private readonly Entity _coil;

        public PushnoyTeslaTowerEntity(Vector3 pos) : base(position: pos)
        {
            Y = Physics.GetSurfaceHeight(X, Z, Y + 1.5f);
            new Entity(parent: this, model: "cube", scale: new(2.1f, 0.6f, 2.1f), position: new(0, 0.3f, 0), color: Rgb(48, 52, 64));
            new Entity(parent: this, model: "diamond", scale: new(1.25f, 3.6f, 1.25f), position: new(0, 2.1f, 0), color: Rgb(215, 135, 55));
            for (int i = 0; i < 3; i++)
                new Entity(parent: this, model: "diamond", scale: new(1.9f - i * 0.25f, 0.18f, 1.9f - i * 0.25f), position: new(0, 1.4f + i * 0.85f, 0), color: Color.SkyBlue, unlit: true);
            _coil = new Entity(parent: this, model: "sphere", scaleUniform: 1.05f, position: new(0, 4.15f, 0), color: Rgb(125, 240, 255), unlit: true);
            new Entity(parent: this, position: new(0, 5.1f, 0), color: Color.SkyBlue) { BillboardText = "МЕГАВАТТНАЯ КАТУШКА ТЕСЛЫ" };
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var c = CombatManager.Controller;
            if (c == null || (_life -= dt) <= 0f) { Destroy(this); return; }
            _coil.RotationY += 260f * dt;
            _zapTick -= dt;
            if (_zapTick <= 0f)
            {
                _zapTick = 0.48f;
                int hits = 0;
                Vector3 top = Position + new Vector3(0, 4.15f, 0);
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly || Vector3.Distance(Position, e.Position) > 22.0f) continue;
                    Vector3 hp = e.Position + new Vector3(0, e.HitHeight * 0.6f, 0);
                    new LightningBeam(top, hp, Rgb(95, 235, 255), 0.26f);
                    e.ApplyArtificerElement("shock", c.BaseDamage);
                    CombatManager.ApplyPlayerHitEffects(e, hp, c.BaseDamage * 1.65f, Rgb(95, 235, 255), aoe: 2.2f, canProc: true);
                    if (++hits >= 4) break;
                }
                if (hits > 0) SoundManager.Play("zap", 0.65f, 1.15f);
            }
        }
    }

    public class SteveBeaconEntity : Entity
    {
        private float _life = 10.0f;
        private float _pulse = 0.2f;

        public SteveBeaconEntity(Vector3 pos) : base(position: pos)
        {
            Y = Physics.GetSurfaceHeight(X, Z, Y + 1.5f);
            new Entity(parent: this, model: "cube", scale: new(2.8f, 0.55f, 2.8f), position: new(0, 0.28f, 0), color: Rgb(65, 225, 135));
            new Entity(parent: this, model: "cube", scale: new(1.8f, 0.55f, 1.8f), position: new(0, 0.82f, 0), color: Color.Gold);
            new Entity(parent: this, model: "cube", scale: new(0.95f, 0.95f, 0.95f), position: new(0, 1.45f, 0), color: Rgb(135, 245, 255), unlit: true);
            new Entity(parent: this, model: "cube", scale: new(0.55f, 26.0f, 0.55f), position: new(0, 14.0f, 0), color: Rgb(125, 245, 255, 140), unlit: true);
            new Entity(parent: this, position: new(0, 3.1f, 0), color: Rgb(75, 245, 240)) { BillboardText = "МАЯК СТИВА [СПЕШКА II + СИЛА II]" };
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var g = WorldContext.Game;
            var c = CombatManager.Controller;
            if (g == null || c == null || (_life -= dt) <= 0f) { Destroy(this); return; }
            _pulse -= dt;
            if (_pulse <= 0f)
            {
                _pulse = 0.75f;
                if (Vector3.Distance(g.PlayerPos, Position) <= 22.0f)
                {
                    g.HealPlayer(10.0f);
                    c.Shield = MathF.Min(g.PlayerMaxHp, c.Shield + 10.0f);
                    c.SteveBeaconBuffTimer = 1.2f;
                }
                foreach (var e in CombatManager.Enemies.ToArray())
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position) <= 14.0f)
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, c.BaseDamage * 1.25f, Color.SkyBlue, canProc: true);
            }
        }
    }

    public class SteveIronGolemEntity : Entity
    {
        public float Hp = 380f;
        public float HitRadius = 1.45f;
        private float _life = 20.0f;
        private float _slamCd = 0.6f;
        private readonly Entity _leftArm;
        private readonly Entity _rightArm;

        public SteveIronGolemEntity(Vector3 pos) : base(position: pos)
        {
            Y = Physics.GetSurfaceHeight(X, Z, Y + 1.5f);
            new Entity(parent: this, model: "cube", scale: new(1.65f, 1.55f, 0.95f), position: new(0, 1.85f, 0), color: Rgb(225, 228, 232));
            new Entity(parent: this, model: "cube", scale: new(0.95f, 0.85f, 0.75f), position: new(0, 0.75f, 0), color: Rgb(185, 190, 195));
            new Entity(parent: this, model: "cube", scale: new(0.65f, 0.80f, 0.65f), position: new(0, 2.95f, 0.1f), color: Rgb(225, 228, 232));
            new Entity(parent: this, model: "cube", scale: new(0.45f, 0.16f, 0.12f), position: new(0, 3.0f, 0.44f), color: Color.Red, unlit: true);
            _leftArm = new Entity(parent: this, model: "cube", scale: new(0.45f, 1.95f, 0.45f), position: new(-1.12f, 1.65f, 0), color: Rgb(210, 215, 220));
            _rightArm = new Entity(parent: this, model: "cube", scale: new(0.45f, 1.95f, 0.45f), position: new(1.12f, 1.65f, 0), color: Rgb(210, 215, 220));
            new Entity(parent: this, position: new(0, 3.85f, 0), color: Color.White) { BillboardText = "ЖЕЛЕЗНЫЙ ГОЛЕМ-ЗАЩИТНИК" };
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var c = CombatManager.Controller;
            if (c == null || c.CharKey != "steve" || Hp <= 0f || (_life -= dt) <= 0f)
            {
                new VisualFX(Position + Vector3.UnitY, Color.White, 3.8f, 0.28f);
                Destroy(this);
                return;
            }
            var t = CombatManager.FindNearestHostileAt(Position);
            Vector3 goal = t != null && Vector3.Distance(Position, t.Position) <= 32f ? t.Position : c.Run.PlayerPos + c.Run.PlayerForward * 3.2f;
            Vector3 toG = new(goal.X - X, 0f, goal.Z - Z);
            if (toG.Length() > 2.2f)
            {
                Position += Vector3.Normalize(toG) * 9.5f * dt;
                Y = Physics.GetSurfaceHeight(X, Z, Y + 1.5f);
                LookAt2D(goal);
            }
            _leftArm.RotationX = Lerp(_leftArm.RotationX, 0f, dt * 8f);
            _rightArm.RotationX = Lerp(_rightArm.RotationX, 0f, dt * 8f);

            _slamCd -= dt;
            if (_slamCd <= 0f && t != null && Vector3.Distance(Position, t.Position) <= 4.5f + t.HitRadius)
            {
                _slamCd = 1.25f;
                _leftArm.RotationX = -75f;
                _rightArm.RotationX = -75f;
                SoundManager.Play("slam_land", 0.85f, 0.92f);
                new VisualFX(t.Position + Vector3.UnitY, Color.White, 3.6f, 0.25f);
                if (!t.IsBoss)
                {
                    t.VelY = MathF.Max(t.VelY, 14.5f);
                    t.IsGrounded = false;
                    t.StunTimer = MathF.Max(t.StunTimer, 1.5f);
                }
                CombatManager.ApplyPlayerHitEffects(t, t.Position + Vector3.UnitY, c.BaseDamage * 2.85f, Color.White, aoe: 3.5f, canProc: true);
            }
        }
    }

    public class VyperVenomPoolEntity : Entity
    {
        private float _life = 4.5f;
        private float _tick = 0.2f;
        private readonly float _dmg;

        public VyperVenomPoolEntity(Vector3 pos, float dmg)
            : base(model: "sphere", position: pos, scale: new(5.4f, 0.12f, 5.4f), color: Rgb(85, 245, 75, 110), unlit: true)
        {
            _dmg = dmg;
            Y = Physics.GetSurfaceHeight(X, Z, Y + 1.2f) + 0.08f;
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            if (_life <= 0f) { Destroy(this); return; }
            _tick -= dt;
            if (_tick <= 0f)
            {
                _tick = 0.55f;
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Physics.Distance2D(Position, e.Position) <= 3.2f + e.HitRadius)
                    {
                        e.SlowTimer = MathF.Max(e.SlowTimer, 1.8f);
                        e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 3.0f);
                        float missingBonus = 1.0f + (1.0f - Math.Clamp(e.Hp / MathF.Max(1f, e.MaxHp), 0f, 1f)) * 0.65f;
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, _dmg * missingBonus, Rgb(95, 255, 85), canProc: true);
                    }
                }
            }
        }
    }

    public class SanabiCyberSlashZoneEntity : Entity
    {
        private float _life = 2.6f;
        private float _slashTick = 0.05f;
        private readonly float _dmg;

        public SanabiCyberSlashZoneEntity(Vector3 center, float dmg) : base(position: center)
        {
            _dmg = dmg;
            new Entity(parent: this, model: "sphere", scale: new(22.0f, 0.12f, 22.0f), position: new(0, 0.12f, 0), color: Rgb(255, 55, 165, 65), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            if (_life <= 0f) { Destroy(this); return; }
            _slashTick -= dt;
            if (_slashTick <= 0f)
            {
                _slashTick = 0.28f;
                int hits = 0;
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly || Vector3.Distance(Position, e.Position) > 22.0f) continue;
                    Vector3 hp = e.Position + new Vector3(0, e.HitHeight * 0.6f, 0);
                    Vector3 offset = new(MathF.Cos(_life * 13f + hits) * 3.5f, 2.2f, MathF.Sin(_life * 13f + hits) * 3.5f);
                    new LightningBeam(hp - offset, hp + offset, (hits & 1) == 0 ? Rgb(255, 65, 165) : Rgb(75, 235, 255), 0.22f);
                    e.SlowTimer = MathF.Max(e.SlowTimer, 1.5f);
                    CombatManager.ApplyPlayerHitEffects(e, hp, _dmg, Rgb(255, 65, 165), aoe: 2.0f, lifesteal: 2.5f, canProc: true);
                    if (++hits >= 5) break;
                }
            }
        }
    }

    public class ScoutFlareBeaconEntity : Entity
    {
        private Vector3 _vel;
        private bool _stuck = false;
        private float _life = 12.0f;
        private float _scanTick = 0.1f;

        public ScoutFlareBeaconEntity(Vector3 startPos, Vector3 dir)
            : base(model: "diamond", position: startPos, scale: new(0.28f, 0.28f, 0.65f), color: Rgb(255, 235, 95), unlit: true)
        {
            _vel = Vector3.Normalize(dir) * 58f;
            new Entity(parent: this, model: "sphere", scaleUniform: 2.2f, color: Rgb(255, 240, 130, 95), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            if (_life <= 0f) { Destroy(this); return; }
            if (!_stuck)
            {
                Vector3 prev = Position;
                Position += _vel * dt;
                float fy = Physics.GetSurfaceHeight(X, Z, Y);
                bool hitFloor = Y <= fy + 0.2f;
                Vector3 hitPt = Position;
                if (hitFloor || Physics.SegmentHitsSolid(prev, Position, 0.2f, out hitPt))
                {
                    if (hitFloor) Y = fy + 0.25f;
                    else Position = hitPt;
                    _stuck = true;
                    SoundManager.Play("coin_flip", 0.7f, 1.35f);
                    new VisualFX(Position, Rgb(255, 240, 120), 4.5f, 0.28f);
                }
            }
            _scanTick -= dt;
            if (_scanTick <= 0f)
            {
                _scanTick = 0.5f;
                foreach (var e in CombatManager.Enemies)
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position) <= 22.0f)
                        e.WeakpointScanTimer = MathF.Max(e.WeakpointScanTimer, 4.0f);
            }
        }
    }

    public class JackOThrownServantArcEntity : Entity
    {
        private Vector3 _vel;
        private readonly float _dmg;
        private float _life = 3.0f;

        public JackOThrownServantArcEntity(Vector3 startPos, Vector3 aimDir, float dmg)
            : base(position: startPos)
        {
            _vel = Vector3.Normalize(aimDir) * 34f + new Vector3(0, 9.5f, 0);
            _dmg = dmg;
            new Entity(parent: this, model: "sphere", scaleUniform: 0.52f, color: Rgb(245, 245, 252));
            new Entity(parent: this, model: "diamond", scale: new(0.45f, 0.35f, 0.45f), position: new(0, 0.45f, 0), color: Rgb(255, 135, 35), unlit: true);
        }

        private void ExplodeServant()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            SoundManager.Play("explosion", 0.88f, 1.15f);
            new VisualFX(Position, Rgb(255, 135, 35), 5.8f, 0.34f);
            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position) <= 6.2f + e.HitRadius)
                {
                    if (!e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, 1.5f);
                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, _dmg, Rgb(255, 135, 35), aoe: 2.0f, canProc: true);
                }
            }
            Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            _vel.Y -= 21f * dt;
            Position += _vel * dt;
            RotationX += 380f * dt;
            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position + Vector3.UnitY) <= e.HitRadius + 0.9f)
                {
                    ExplodeServant();
                    return;
                }
            }
            float fy = Physics.GetSurfaceHeight(X, Z, Y);
            if (Y <= fy + 0.25f || _life <= 0f) ExplodeServant();
        }
    }

    // =================================================================================================
    // РАСШИРЕНИЕ КОНТРОЛЛЕРА ДЛЯ ВСЕХ ПЕРЕРАБОТАННЫХ ГЕРОЕВ (#1 - #32 + АЛЬТ-ПЕРСОНЫ)
    // =================================================================================================

    public partial class SurvivorCombatController
    {
        // Commando
        public int CommandoHeavyRounds = 4;

        // Artificer
        public bool ArtificerHovering = false;

        // Huntress
        public float HuntressMomentumCharge = 0f;
        public int HuntressBallistaShots = 0;
        public float HuntressBallistaTimer = 0f;

        // Buzz & Emperor Zurg
        public bool BuzzWingGlideReady = false;
        public bool BuzzWingGliding = false;
        public float ZurgIonHeat = 0f;
        public float ZurgReflectTimer = 0f;
        public int ZurgReflectedCount = 0;
        public readonly List<ZurgHornetBotEntity> ZurgHornetBots = new();

        // V1 & V2
        public float V2WingEnrageTimer = 0f;
        public bool IsV2Persona => CharKey == "v1" && ProgressData.Instance.GetSelectedSkin("v1") == 2;

        // Sol & Ky
        public float SolTension = 0f;
        public float SolRomanCancelBuff = 0f;

        // Scout
        public float ScoutFlareCd = 0f;

        // Medic
        public float MedicUberCharge = 0f;
        public bool MedicBeamActive = false;
        private float _medicBeamFxTick = 0f;

        // Aigis
        public float AigisOverheatTimer = 0f;
        public int AigisTetrakarnCharges = 0;

        // Reimu
        public int ReimuGrazeStacks = 0;
        public float ReimuGrazeDecayTimer = 0f;

        // Mindustry
        public readonly List<MindustryAssaultDroneEntity> MindustryDrones = new();

        // Slark
        public float SlarkDarkPactPulseTimer = 0f;

        // Tidehunter
        public float TideKrakenDamageAccum = 0f;

        // Freeman
        public float FreemanBlueGravTimer = 0f;
        public CombatTarget? FreemanGrabbedEnemy = null;
        public float FreemanMorphineCd = 0f;

        // Cheburashka
        public float ChebAccordionPulseTimer = 0f;

        // Lappland Decadenza
        public readonly List<DecadenzaWolfFamiliarEntity> DecadenzaFamiliars = new();

        // W & Wis'adel
        public readonly List<WC4ClaymoreMineEntity> WMines = new();
        public readonly List<WisadelShadowTotemEntity> WisadelShadows = new();
        public int WisadelAncestralCharges = 0;

        // Freddy
        public float FreddyBattery = 100f;
        public float FreddyPowerOutageTimer = 0f;
        public float FreddyCamStealthTimer = 0f;

        // Steve
        public float SteveBeaconBuffTimer = 0f;
        public SteveIronGolemEntity? SteveActiveGolem = null;

        // Jotaro
        private bool _zaWarudoWasActive = false;

        // Vyper
        private float _vyperPoolTick = 0f;

        // Haze
        public bool HazeAmbushReady = false;

        // DMC Trio
        public int DanteStyle = 0; // 0: Trickster, 1: Swordmaster, 2: Gunslinger, 3: Royal Guard
        public float DanteRoyalGauge = 0f;
        public float DanteRoyalBlockTimer = 0f;
        public float NeroMaxActWindow = 0f;
        public float VergilConcentration = 0f;
        public float VergilJustCutWindow = 0f;

        // Jack-O
        public float JackOThrowCd = 0f;

        public bool IsPlayerStealthedForAggro()
        {
            if (HazeSmokeTimer > 0f || FreddyCamStealthTimer > 0f) return true;
            if (CharKey == "komeiji" && ProgressData.Instance.GetSelectedSkin("komeiji") == 1 && GetKoishiStealthValue() >= 0.82f)
                return true;
            return false;
        }

        public float GetReworkDamageMult()
        {
            float m = 1.0f;
            if (CharKey == "artificer" && (!Run.PlayerGrounded || ArtificerHovering))
                m *= 1.20f;
            if (CharKey == "buzz" && ProgressData.Instance.GetSelectedSkin("buzz") == 2)
                m *= 1.0f + ZurgIonHeat * 0.30f;
            if (SolRomanCancelBuff > 0f)
                m *= 1.25f;
            if (CharKey == "reimu" && ReimuGrazeStacks > 0)
                m *= 1.0f + ReimuGrazeStacks * 0.03f;
            if (CharKey == "steve" && SteveBeaconBuffTimer > 0f)
                m *= 1.28f;
            if (CharKey == "freddy" && FreddyPowerOutageTimer > 0f)
                m *= 1.35f;
            if (CharKey == "dmc_trio")
            {
                int sk = ProgressData.Instance.GetSelectedSkin("dmc_trio");
                if (sk == 0 && DanteStyle == 1) m *= 1.18f; // Swordmaster
                else if (sk == 2) m *= 1.0f + (VergilConcentration / 100f) * 0.35f;
            }
            return m;
        }

        public float GetReworkMoveSpeedMult()
        {
            float m = 1.0f;
            if (CharKey == "buzz")
            {
                if (ProgressData.Instance.GetSelectedSkin("buzz") == 2) m *= 1.16f;
                else if (BuzzWingGliding) m *= 1.38f;
            }
            if (IsV2Persona && V2WingEnrageTimer > 0f) m *= 1.30f;
            if (CharKey == "aigis" && OrgiaTimer > 0f && !Run.PlayerGrounded) m *= 1.25f;
            if (CharKey == "freddy" && FreddyPowerOutageTimer > 0f) m *= 1.42f;
            if (CharKey == "steve" && SteveBeaconBuffTimer > 0f) m *= 1.25f;
            if (CharKey == "dmc_trio" && ProgressData.Instance.GetSelectedSkin("dmc_trio") == 0 && DanteStyle == 0)
                m *= 1.22f; // Trickster
            return m;
        }

        public float GetReworkFireRateMult()
        {
            float m = 1.0f;
            if (CharKey == "commando" && CommandoHeavyRounds > 0) m *= 1.20f;
            if (IsV2Persona && V2WingEnrageTimer > 0f) m *= 1.35f;
            if (CharKey == "steve" && SteveBeaconBuffTimer > 0f) m *= 1.25f;
            if (CharKey == "dmc_trio" && ProgressData.Instance.GetSelectedSkin("dmc_trio") == 0 && DanteStyle == 2)
                m *= 1.25f; // Gunslinger
            return m;
        }

        public void OnV2StylishActionHeal(float hpRestore, float shieldRestore)
        {
            if (!IsV2Persona) return;
            Run.HealPlayer(hpRestore);
            Shield = MathF.Min(Run.PlayerMaxHp + 80f, Shield + shieldRestore);
        }

        public bool IsFreemanBlueGravityGunActive()
        {
            if (CharKey != "freeman") return false;
            if (FreemanBlueGravTimer > 0f) return true;
            if (Run.TeleporterChargingNow) return true; // Во время зарядки Телепорта Грави-пушка перегружается!
            return false;
        }

        public void TriggerKrakenShellAnchorSmash()
        {
            TideKrakenDamageAccum = 0f;
            SoundManager.Play("slam_land", 0.95f, 0.90f);
            new VisualFX(Run.PlayerPos + new Vector3(0, 1.0f, 0), Rgb(65, 225, 215), 8.5f, 0.36f);
            Run.ShowBanner("ПАНЦИРЬ КРАКЕНА :: АВТО-ОЧИЩЕНИЕ И КРУГОВОЙ ЯКОРЬ!", Rgb(75, 235, 220), 1.6f);
            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 9.5f)
                {
                    if (!e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, 1.2f);
                    e.SlowTimer = MathF.Max(e.SlowTimer, 2.5f);
                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 2.25f, Rgb(65, 225, 215), canProc: true);
                }
            }
        }

        public void ApplyEnforcerWallSplat(CombatTarget e, Vector3 pushDir, float pushDist)
        {
            if (!IsValid(e) || e.IsBoss) return;
            Vector3 desired = e.Position + pushDir * pushDist;
            if (Physics.SegmentHitsSolid(e.Position + Vector3.UnitY, desired + Vector3.UnitY, 0.55f, out Vector3 wallPt) ||
                Physics.IsInsideSolidObstacle(desired, 0.75f))
            {
                e.StunTimer = MathF.Max(e.StunTimer, 2.6f);
                e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 5.0f);
                SoundManager.Play("slam_land", 0.90f, 1.10f);
                new VisualFX(e.Position + Vector3.UnitY, Color.Gold, 4.2f, 0.30f);
                CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 2.20f, Color.Gold, aoe: 2.5f, canProc: true);
                Run.ShowBanner("ТАРАН В СТЕНУ! (WALL-SPLAT x2.2)", Color.Gold, 1.2f);
            }
        }

        public void SpendFreddyBattery(float cost)
        {
            if (CharKey != "freddy" || FreddyPowerOutageTimer > 0f) return;
            FreddyBattery = MathF.Max(0f, FreddyBattery - cost);
            if (FreddyBattery <= 0.1f)
            {
                FreddyBattery = 0f;
                FreddyPowerOutageTimer = 4.5f;
                SoundManager.Play("ult_cast", 1.0f, 0.85f);
                new VisualFX(Run.PlayerPos + Vector3.UnitY, Color.Gold, 7.5f, 0.38f);
                Run.ShowBanner("⚡ 0% ЗАРЯДА FAZBEAR :: МАРШ ТОРЕАДОРА (ОВЕРДРАЙВ ТЕМНОТЫ)! ⚡", Color.Gold, 2.6f);
            }
        }

        // --- АКТИВАЦИЯ ДОПОЛНИТЕЛЬНЫХ КНОПОК [Q], [F], [1-4] ---
        public void HandleReworkExtraHotkeys()
        {
            if (CharKey == "sol" && !SurvivorDatabase.IsMobaCharacter(CharKey))
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Q) && SolTension >= 50f)
                {
                    SolTension -= 50f;
                    SolRomanCancelBuff = 3.2f;
                    M1Cd = 0f;
                    SkillLockTimer = 0f;
                    SolBringerState = 0;
                    SoundManager.Play("dante_clock", 0.95f, 1.25f);
                    new VisualFX(Run.PlayerPos + new Vector3(0, 1.2f, 0), Rgb(255, 65, 95), 8.5f, 0.36f);
                    foreach (var e in CombatManager.Enemies)
                    {
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 9.5f)
                        {
                            e.SlowTimer = MathF.Max(e.SlowTimer, 2.2f);
                            if (!e.IsBoss) e.VelY = MathF.Max(e.VelY, 5.5f);
                        }
                    }
                    Run.ShowBanner("★ ROMAN CANCEL! (ЗАМЕДЛЕНИЕ ВРЕМЕНИ +25% УРОН) ★", Rgb(255, 75, 95), 1.6f);
                }
            }
            else if (CharKey == "scout")
            {
                if (Raylib.IsKeyPressed(KeyboardKey.F) && ScoutFlareCd <= 0f)
                {
                    ScoutFlareCd = 9.0f;
                    SoundManager.Play("shoot_light", 0.85f, 1.35f);
                    var flare = new ScoutFlareBeaconEntity(Run.PlayerPos + new Vector3(0, 1.5f, 0), GetAimDirection());
                    Run.StageEntities.Add(flare);
                    Run.ShowBanner("СИГНАЛЬНАЯ РАКЕТНИЦА [F] :: УЯЗВИМЫЕ ТОЧКИ ПОДСВЕЧЕНЫ (+25% КРИТ)!", Color.Yellow, 1.8f);
                }
            }
            else if (CharKey == "jack_o")
            {
                if (Raylib.IsKeyPressed(KeyboardKey.F) && JackOThrowCd <= 0f)
                {
                    JackOThrowCd = 3.2f;
                    JackOServants.RemoveAll(s => !IsValid(s));
                    if (JackOServants.Count > 0)
                    {
                        var picked = JackOServants[0];
                        JackOServants.RemoveAt(0);
                        Destroy(picked);
                    }
                    SoundManager.Play("melee_swing", 0.85f, 1.25f);
                    var thrown = new JackOThrownServantArcEntity(Run.PlayerPos + new Vector3(0, 1.55f, 0), GetAimDirection(), BaseDamage * 3.4f);
                    Run.StageEntities.Add(thrown);
                    Run.ShowBanner("БРОСОК СЛУГИ НАВЕСОМ [F]!", Rgb(255, 145, 45), 1.2f);
                }
            }
            else if (CharKey == "dmc_trio")
            {
                int sk = ProgressData.Instance.GetSelectedSkin("dmc_trio");
                if (sk == 0)
                {
                    int newStyle = -1;
                    if (Raylib.IsKeyPressed(KeyboardKey.One)) newStyle = 0;
                    else if (Raylib.IsKeyPressed(KeyboardKey.Two)) newStyle = 1;
                    else if (Raylib.IsKeyPressed(KeyboardKey.Three)) newStyle = 2;
                    else if (Raylib.IsKeyPressed(KeyboardKey.Four)) newStyle = 3;
                    if (newStyle >= 0 && newStyle != DanteStyle)
                    {
                        DanteStyle = newStyle;
                        (string sName, Color sCol) = DanteStyle switch
                        {
                            0 => ("TRICKSTER! (+СКОРОСТЬ И УКЛОНЕНИЕ)", Color.Yellow),
                            1 => ("SWORDMASTER! (+18% УРОН БЛИЖНЕГО БОЯ)", Rgb(255, 65, 75)),
                            2 => ("GUNSLINGER! (+25% СКОРОСТРЕЛЬНОСТЬ)", Color.SkyBlue),
                            _ => ("ROYAL GUARD! (БЛОК И ШКАЛА ROYAL RELEASE)", Color.Gold)
                        };
                        SoundManager.Play("coin_flip", 0.75f, 1.25f);
                        new VisualFX(Run.PlayerPos + new Vector3(0, 1.3f, 0), sCol, 2.8f, 0.22f);
                        Run.ShowBanner($"СТИЛЬ ДАНТЕ: {sName}", sCol, 1.4f);
                    }
                }
                else if (sk == 1)
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.Q))
                    {
                        if (NeroMaxActWindow > 0f)
                        {
                            NeroMaxActWindow = 0f;
                            NeroExceedCharges = 3;
                            SoundManager.Play("flame", 0.95f, 1.35f);
                            new VisualFX(Run.PlayerPos + new Vector3(0, 1.3f, 0), Rgb(255, 115, 30), 3.6f, 0.25f);
                            Run.ShowBanner("🔥 MAX-ACT!! [3/3 EXCEED ЗАРЯЖЕНО!] 🔥", Rgb(255, 125, 35), 1.3f);
                        }
                        else if (NeroExceedCharges < 3)
                        {
                            NeroExceedCharges++;
                            SoundManager.Play("flame", 0.55f, 0.95f);
                        }
                    }
                }
            }
            else if (CharKey == "spamton" && ProgressData.Instance.GetSelectedSkin("spamton") != 2)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.F))
                    UseSpamtonHotDealGamble();
            }
        }

        // --- ПОЛНЫЙ НАБОР СКИЛЛОВ ИМПЕРАТОРА ЗУРГА (buzz skinIdx == 2) ---
        public bool TryFireZurgPrimary(Vector3 aimDir, Vector3 spawnPos, float damageMult, int m1Var)
        {
            if (CharKey != "buzz" || ProgressData.Instance.GetSelectedSkin("buzz") != 2)
                return false;
            ZurgIonHeat = Math.Clamp(ZurgIonHeat + 0.08f, 0f, 1.0f);
            Vector3 right = Vector3.Cross(aimDir, Vector3.UnitY);
            right = right.LengthSquared() > 0.001f ? Vector3.Normalize(right) : Vector3.UnitX;

            if (m1Var == 0)
            {
                SoundManager.Play("laser", 0.80f, 0.95f + (_shootSide % 3) * 0.08f);
                float off = ((_shootSide % 3) - 1) * 0.22f;
                new Projectile(spawnPos + right * off, aimDir, 62f, BaseDamage * 1.32f * damageMult, 0.38f, Rgb(215, 55, 255), aoe: 2.4f, canProc: true);
            }
            else
            {
                SoundManager.Play("railgun", 0.78f, 1.18f);
                Vector3 end = spawnPos + aimDir * 36f;
                new LightningBeam(spawnPos, end, Rgb(215, 55, 255), 0.22f);
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly) continue;
                    Vector3 c = e.Position + Vector3.UnitY;
                    float proj = Math.Clamp(Vector3.Dot(c - spawnPos, aimDir), 0f, 36f);
                    if (Vector3.Distance(c, spawnPos + aimDir * proj) <= e.HitRadius + 1.4f)
                    {
                        e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 4.0f);
                        CombatManager.ApplyPlayerHitEffects(e, c, BaseDamage * 1.55f * damageMult, Rgb(215, 55, 255), aoe: 1.6f, canProc: true);
                    }
                }
            }
            return true;
        }

        public bool TryUseZurgM2(Vector3 aimDir, Vector3 spawnPos, int m2Var)
        {
            if (CharKey != "buzz" || ProgressData.Instance.GetSelectedSkin("buzz") != 2)
                return false;
            if (m2Var == 0)
            {
                ZurgReflectTimer = 2.2f;
                ZurgReflectedCount = 0;
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 40f);
                SoundManager.Play("parry", 0.90f, 0.95f);
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.3f, 0), Rgb(215, 55, 255), 3.8f, 0.30f);
                Run.ShowBanner("МАГНИТНЫЙ ОТРАЖАТЕЛЬ ЗУРГА АКТИВЕН!", Rgb(215, 65, 255), 1.4f);
            }
            else
            {
                SoundManager.Play("magic_cast", 0.90f, 0.85f);
                Vector3 vPos = Run.PlayerPos + aimDir * 12.0f;
                vPos.Y = Physics.GetSurfaceHeight(vPos.X, vPos.Z, Run.PlayerPos.Y);
                Run.StageEntities.Add(new ZurgGravityVortexEntity(vPos, BaseDamage * 1.85f));
            }
            return true;
        }

        public bool TryUseZurgShift(Vector3 myPos, int shVar, float nrgMult)
        {
            if (CharKey != "buzz" || ProgressData.Instance.GetSelectedSkin("buzz") != 2)
                return false;
            Vector3 aim = GetAimDirection();
            Vector3 flat = new(aim.X, 0f, aim.Z);
            flat = flat.LengthSquared() > 0.001f ? Vector3.Normalize(flat) : Run.PlayerForward;
            Shield = MathF.Min(Run.PlayerMaxHp, Shield + 35f);

            if (shVar == 0)
            {
                Run.ApplyDash(flat * (28f * nrgMult), 0.32f);
                ZurgHornetBots.RemoveAll(b => !IsValid(b));
                for (int i = 0; i < 2 && ZurgHornetBots.Count < 4; i++)
                {
                    var bot = new ZurgHornetBotEntity(myPos + new Vector3(0, 2.0f, 0), ZurgHornetBots.Count);
                    ZurgHornetBots.Add(bot);
                    Run.StageEntities.Add(bot);
                }
                SoundManager.Play("rocket", 0.80f, 1.20f);
            }
            else
            {
                new VisualFX(myPos + Vector3.UnitY, Rgb(215, 55, 255), 5.2f, 0.30f);
                foreach (var e in CombatManager.Enemies.ToArray())
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(myPos, e.Position) <= 6.5f)
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 2.4f, Rgb(215, 55, 255), canProc: true);
                Vector3 dst = Physics.ResolveHorizontalCollision(myPos + flat * (15.5f * nrgMult), 0.65f, myPos.Y);
                dst.Y = MathF.Max(dst.Y, Physics.GetSurfaceHeight(dst.X, dst.Z, myPos.Y + 1.5f));
                new LightningBeam(myPos + Vector3.UnitY, dst + Vector3.UnitY, Rgb(215, 55, 255), 0.28f);
                Run.PlayerPos = dst;
                SoundManager.Play("zap", 0.85f, 1.15f);
            }
            return true;
        }

        public bool TryUseZurgR(int rVar)
        {
            if (CharKey != "buzz" || ProgressData.Instance.GetSelectedSkin("buzz") != 2)
                return false;
            ZurgIonHeat = 1.0f;
            Shield = MathF.Min(Run.PlayerMaxHp, Shield + 65f);
            Vector3 aim = GetAimDirection();
            Vector3 sp = Run.PlayerPos + new Vector3(0, 1.45f, 0) + aim * 0.8f;

            if (rVar == 0)
            {
                SoundManager.Play("railgun", 1.1f, 0.72f);
                Vector3 end = sp + aim * 52f;
                new LightningBeam(sp, end, Rgb(225, 45, 255), 0.55f);
                new LightningBeam(sp + new Vector3(0, 0.2f, 0), end, Color.Red, 0.45f);
                new VisualFX(sp + aim * 4f, Rgb(225, 45, 255), 6.5f, 0.38f);
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly) continue;
                    Vector3 c = e.Position + Vector3.UnitY;
                    float proj = Math.Clamp(Vector3.Dot(c - sp, aim), 0f, 52f);
                    if (Vector3.Distance(c, sp + aim * proj) <= e.HitRadius + 3.6f)
                    {
                        e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 7.0f);
                        CombatManager.ApplyPlayerHitEffects(e, c, BaseDamage * 6.8f, Rgb(225, 45, 255), aoe: 3.5f, canProc: true);
                    }
                }
                Run.ShowBanner("ИМПЕРАТОР ЗУРГ :: ОРУДИЕ РАЗРУШИТЕЛЯ ПЛАНЕТ!", Rgb(225, 55, 255), 2.2f);
            }
            else
            {
                SoundManager.Play("ult_cast", 1.0f, 0.90f);
                for (int i = 0; i < 8; i++)
                {
                    float ang = (i / 8.0f) * MathF.PI * 2f;
                    Vector3 mDir = Vector3.Normalize(aim + new Vector3(MathF.Cos(ang) * 0.4f, 0.35f, MathF.Sin(ang) * 0.4f));
                    new Projectile(sp, mDir, 48f, BaseDamage * 2.15f, 0.48f, Rgb(215, 55, 255), isPlayer: true, aoe: 4.2f, homing: true, canProc: true);
                }
                ZurgHornetBots.RemoveAll(b => !IsValid(b));
                while (ZurgHornetBots.Count < 4)
                {
                    var bot = new ZurgHornetBotEntity(Run.PlayerPos + new Vector3(0, 2.2f, 0), ZurgHornetBots.Count);
                    ZurgHornetBots.Add(bot);
                    Run.StageEntities.Add(bot);
                }
                Run.ShowBanner("АРМАДА ЗУРГА :: ИОННАЯ БОМБАРДИРОВКА!", Rgb(225, 55, 255), 2.2f);
            }
            return true;
        }

        // --- ПОЛНЫЙ НАБОР СКИЛЛОВ БАГРОВОГО ДВОЙНИКА V2 (v1 skinIdx == 2) ---
        public bool TryUseV2M2(Vector3 aimDir, Vector3 spawnPos, int m2Var)
        {
            if (!IsV2Persona) return false;
            Vector3 right = Vector3.Cross(aimDir, Vector3.UnitY);
            right = right.LengthSquared() > 0.001f ? Vector3.Normalize(right) : Vector3.UnitX;

            if (m2Var == 0)
            {
                // Сальто назад и веер из 3 золотых монет!
                SoundManager.Play("coin_flip", 1.0f, 1.12f);
                Run.PlayerVelY = 9.5f;
                Run.PlayerGrounded = false;
                Run.ApplyDash(-new Vector3(aimDir.X, 0f, aimDir.Z) * 14f, 0.22f);
                for (int i = -1; i <= 1; i++)
                {
                    Vector3 cVel = Vector3.Normalize(aimDir + right * (i * 0.14f)) * 18.5f + new Vector3(0, 10.2f, 0);
                    var coin = new V1Coin(spawnPos + right * (i * 0.25f), cVel);
                    V1ActiveCoins.Add(coin);
                    Run.StageEntities.Add(coin);
                }
                V1AddStyle("+ ДУЭЛЬНЫЙ ВЕЕР V2 [x3]", Color.Gold, 65f);
            }
            else
            {
                // Мгновенный рикошетящий луч Пирсера V2 (или двойное ядро на дробовике)
                if (V1Weapon == 2)
                {
                    SoundManager.Play("rocket", 0.95f, 1.15f);
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var core = new Projectile(spawnPos + right * (s * 0.25f), aimDir + new Vector3(0, 0.16f, 0), 34f, BaseDamage * 2.4f, 0.45f, Color.Orange, isPlayer: true, aoe: 6.0f)
                        {
                            IsV1Core = true,
                            GravityArc = 0.95f
                        };
                        V1ActiveCores.Add(core);
                    }
                }
                else
                {
                    SoundManager.Play("railgun", 0.90f, 1.25f);
                    var beamProj = new Projectile(spawnPos, aimDir, 92f, BaseDamage * 2.85f, 0.38f, Rgb(255, 45, 65), isPlayer: true, aoe: 2.2f, pierce: false, canProc: true)
                    {
                        WallBounces = 3,
                        BounceDamageMult = 1.30f
                    };
                    Run.StageEntities.Add(beamProj);
                    V1AddStyle("+ РИКОШЕТ ПИРСЕРА V2", Rgb(255, 55, 75), 55f);
                }
            }
            return true;
        }

        public bool TryUseV2Shift(Vector3 myPos, int shVar, float nrgMult)
        {
            if (!IsV2Persona) return false;
            Vector3 aim = GetAimDirection();
            Vector3 flat = new(aim.X, 0f, aim.Z);
            flat = flat.LengthSquared() > 0.001f ? Vector3.Normalize(flat) : Run.PlayerForward;

            if (shVar == 0)
            {
                Shield = MathF.Min(Run.PlayerMaxHp + 80f, Shield + 25f);
                Run.ApplyDash(flat * (36f * nrgMult), 0.30f);
                SoundManager.Play("dash", 0.95f, 1.2f);
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 mDir = Vector3.Normalize(aim + new Vector3(s * 0.15f, 0.12f, 0f));
                    new Projectile(myPos + new Vector3(0, 1.3f, 0), mDir, 54f, BaseDamage * 1.15f, 0.30f, Color.Red, isPlayer: true, aoe: 2.4f, homing: true, canProc: true);
                }
            }
            else
            {
                Run.PlayerVelY = -28f;
                Run.ApplyDash(flat * (26f * nrgMult), 0.25f);
                V1UseKnuckleblaster();
                Shield = MathF.Min(Run.PlayerMaxHp + 80f, Shield + 30f);
            }
            return true;
        }

        public bool TryUseV2R(int rVar)
        {
            if (!IsV2Persona) return false;
            if (rVar == 0)
            {
                V1UseWhiplash();
                V1UseKnuckleblaster();
                OnV2StylishActionHeal(18f, 35f);
                V1AddStyle("+ WHIPLASH BUSTER V2", Color.Red, 85f);
            }
            else
            {
                V2WingEnrageTimer = 6.5f;
                OnV2StylishActionHeal(30f, 80f);
                SoundManager.Play("ult_cast", 1.0f, 1.18f);
                Vector3 sp = Run.PlayerPos + new Vector3(0, 1.6f, 0);
                Vector3 aim = GetAimDirection();
                Vector3 right = Vector3.Cross(aim, Vector3.UnitY);
                right = right.LengthSquared() > 0.001f ? Vector3.Normalize(right) : Vector3.UnitX;
                for (int i = 0; i < 4; i++)
                {
                    float off = (i - 1.5f) * 0.18f;
                    var coin = new V1Coin(sp, Vector3.Normalize(aim + right * off) * 19f + new Vector3(0, 10.5f, 0));
                    V1ActiveCoins.Add(coin);
                    Run.StageEntities.Add(coin);
                }
                Run.ShowBanner("V2 :: ПРОТОКОЛ «КЛЭР ДЕ ЛЮН» — ТИТАНОВАЯ ЯРОСТЬ!", Rgb(255, 55, 65), 2.2f);
            }
            return true;
        }

        // --- ЕЖЕКАДРОВОЕ ОБНОВЛЕНИЕ ВСЕХ НОВЫХ МЕХАНИК (#1 - #32) ---
        public void UpdateReworkCoreMechanics(float dt, bool isMoving)
        {
            HandleReworkExtraHotkeys();

            if (SolRomanCancelBuff > 0f) SolRomanCancelBuff -= dt;
            if (ScoutFlareCd > 0f) ScoutFlareCd -= dt;
            if (JackOThrowCd > 0f) JackOThrowCd -= dt;
            if (FreemanBlueGravTimer > 0f) FreemanBlueGravTimer -= dt;
            if (FreemanMorphineCd > 0f) FreemanMorphineCd -= dt;
            if (SteveBeaconBuffTimer > 0f) SteveBeaconBuffTimer -= dt;
            if (V2WingEnrageTimer > 0f) V2WingEnrageTimer -= dt;
            if (AigisOverheatTimer > 0f) AigisOverheatTimer -= dt;
            if (FreddyCamStealthTimer > 0f) FreddyCamStealthTimer -= dt;
            if (DanteRoyalBlockTimer > 0f) DanteRoyalBlockTimer -= dt;
            if (NeroMaxActWindow > 0f) NeroMaxActWindow -= dt;
            if (VergilJustCutWindow > 0f) VergilJustCutWindow -= dt;

            // Jotaro: Завершение ZA WARUDO взрывает весь накопленный урон и подбрасывает врагов!
            bool zwNow = ZaWarudoTimer > 0f;
            if (_zaWarudoWasActive && !zwNow)
            {
                SoundManager.Play("slam_land", 1.0f, 0.85f);
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && e.TheWorldStoredDmg > 0f)
                    {
                        float burst = e.TheWorldStoredDmg;
                        e.TheWorldStoredDmg = 0f;
                        new VisualFX(e.Position + new Vector3(0, e.HitHeight, 0), Rgb(215, 135, 255), 4.5f, 0.30f);
                        if (!e.IsBoss)
                        {
                            e.VelY = MathF.Max(e.VelY, 11.5f);
                            e.IsGrounded = false;
                        }
                        e.TakeDamage(burst);
                    }
                }
                if (CharKey == "jotaro")
                    Run.ShowBanner("«ТОКИ ВА УГОКИДАСУ» :: ВРЕМЯ ПОШЛО!", Rgb(215, 155, 255), 1.8f);
            }
            _zaWarudoWasActive = zwNow;

            // Artificer: Парение костюма ENV на удержание Пробела
            ArtificerHovering = false;
            if (CharKey == "artificer" && !Run.PlayerGrounded && Run.PlayerVelY < 1.5f && Raylib.IsKeyDown(KeyboardKey.Space))
            {
                ArtificerHovering = true;
                Run.PlayerVelY = MathF.Max(Run.PlayerVelY, -1.35f);
            }

            // Huntress: Заряд критической стрелы в движении + 3 ручных выстрела Баллисты
            if (CharKey == "huntress")
            {
                if (isMoving) HuntressMomentumCharge = Math.Clamp(HuntressMomentumCharge + 0.38f * dt, 0f, 1.0f);
                else HuntressMomentumCharge = MathF.Max(0f, HuntressMomentumCharge - 0.25f * dt);

                if (HuntressBallistaTimer > 0f)
                {
                    HuntressBallistaTimer -= dt;
                    if (!Run.PlayerGrounded && Run.PlayerVelY < 0.5f)
                        Run.PlayerVelY = -0.65f;
                    if (HuntressBallistaTimer <= 0f) HuntressBallistaShots = 0;
                }
            }

            // Buzz: Планирование на крыльях Star Command и Метеор-удар [Ctrl/C], а также пассив Зурга
            if (CharKey == "buzz")
            {
                if (ProgressData.Instance.GetSelectedSkin("buzz") == 2)
                {
                    ZurgIonHeat = MathF.Max(0f, ZurgIonHeat - 0.14f * dt);
                    if (ZurgReflectTimer > 0f)
                    {
                        ZurgReflectTimer -= dt;
                        if (ZurgReflectTimer <= 0f && ZurgReflectedCount > 0)
                        {
                            SoundManager.Play("laser", 0.95f, 1.15f);
                            Vector3 aim = GetAimDirection();
                            for (int i = 0; i < Math.Min(6, ZurgReflectedCount + 1); i++)
                                new Projectile(Run.PlayerPos + new Vector3(0, 1.4f, 0), aim, 68f, BaseDamage * 1.8f, 0.42f, Rgb(255, 65, 255), isPlayer: true, aoe: 2.8f, homing: true);
                            ZurgReflectedCount = 0;
                        }
                    }
                }
                else
                {
                    if (Run.PlayerGrounded) { BuzzWingGlideReady = false; BuzzWingGliding = false; }
                    else if (BuzzWingGlideReady)
                    {
                        if (Raylib.IsKeyDown(KeyboardKey.Space) && Run.PlayerVelY < 1.0f)
                        {
                            BuzzWingGliding = true;
                            Run.PlayerVelY = MathF.Max(Run.PlayerVelY, -2.0f);
                        }
                        else BuzzWingGliding = false;

                        if (Raylib.IsKeyPressed(KeyboardKey.LeftControl) || Raylib.IsKeyPressed(KeyboardKey.C))
                        {
                            BuzzWingGlideReady = false;
                            BuzzWingGliding = false;
                            Run.PlayerVelY = -34f;
                            SoundManager.Play("slam_land", 0.95f, 1.1f);
                            new VisualFX(Run.PlayerPos + Vector3.UnitY, Color.Lime, 6.5f, 0.34f);
                            PerformMeleeAttack(7.5f, 6.5f, 3.2f, Color.Lime, stunDur: 1.8f, aoe: 4.5f);
                            Run.ShowBanner("«ЭТО НЕ ПОЛЁТ — ЭТО ПАДЕНИЕ СО СТИЛЕМ!»", Color.Lime, 1.6f);
                        }
                    }
                }
            }

            // V1 / V2: Оседлание ракет Freezeframe (Rocket Riding!) и пассивная броня V2
            if (CharKey == "v1")
            {
                if (IsV2Persona && Shield < 80f && StationaryTimer < 0.2f)
                    Shield = MathF.Min(Run.PlayerMaxHp + 80f, Shield + 4.5f * dt);

                foreach (var ent in Run.StageEntities)
                {
                    if (ent is Projectile rk && IsValid(rk) && rk.IsV1Rocket)
                    {
                        Vector3 diff = Run.PlayerPos - rk.Position;
                        if (Physics.Distance2D(Run.PlayerPos, rk.Position) <= 1.85f && diff.Y >= -0.6f && diff.Y <= 2.4f)
                        {
                            Vector3 rideDir = new(rk.Direction.X, MathF.Max(-0.08f, rk.Direction.Y * 0.35f), rk.Direction.Z);
                            if (rideDir.LengthSquared() > 0.001f) rideDir = Vector3.Normalize(rideDir);
                            if (V1RocketsFrozen)
                            {
                                Run.PlayerPos = new Vector3(rk.Position.X, rk.Position.Y + 0.55f, rk.Position.Z);
                                Run.PlayerVelY = 0f;
                                Run.PlayerGrounded = true;
                            }
                            else
                            {
                                rk.Position += rideDir * 6.0f * dt;
                                Run.PlayerPos = new Vector3(rk.Position.X, rk.Position.Y + 0.55f, rk.Position.Z);
                                Run.PlayerVelY = 0.5f;
                            }
                            break;
                        }
                    }
                }
            }

            // Sol / Ky: Набор Tension от движения в бою
            if (CharKey == "sol" && isMoving)
                SolTension = Math.Clamp(SolTension + 6.5f * dt, 0f, 100f);

            // Medic: Непрерывный Лечебный Луч Medi-Gun на удержание ПКМ!
            MedicBeamActive = false;
            if (CharKey == "medic" && Raylib.IsMouseButtonDown(MouseButton.Right) && !Program.IsSpectating)
            {
                Vector3 myEye = Run.PlayerPos + new Vector3(0, 1.35f, 0);
                Vector3? allyPos = null;
                CombatTarget? allyMob = null;
                float bestD = 22.0f;

                if (Physics.SpecialMode != "pvp")
                {
                    Vector3 rp = Program.GetNearestRemotePlayerPos(Run.PlayerPos, out float rpDist);
                    if (rpDist <= bestD) { bestD = rpDist; allyPos = rp + new Vector3(0, 1.2f, 0); }
                }
                foreach (var al in CombatManager.Enemies)
                {
                    if (IsValid(al) && al.IsAlly)
                    {
                        float d = Vector3.Distance(Run.PlayerPos, al.Position);
                        if (d < bestD) { bestD = d; allyMob = al; allyPos = al.Position + new Vector3(0, al.HitHeight * 0.7f, 0); }
                    }
                }

                MedicBeamActive = true;
                MedicUberCharge = Math.Clamp(MedicUberCharge + (allyPos.HasValue ? 14.5f : 7.0f) * dt, 0f, 100f);
                if (allyMob != null) allyMob.HealAlly(36f * dt);
                else if (allyPos.HasValue) Program.TryHealRemotePlayerCoop(allyPos.Value, 3.0f, 32f * dt);
                Shield = MathF.Min(Run.PlayerMaxHp * 0.65f, Shield + 12f * dt);

                _medicBeamFxTick -= dt;
                if (_medicBeamFxTick <= 0f)
                {
                    _medicBeamFxTick = 0.09f;
                    Vector3 beamEnd = allyPos ?? (myEye + GetAimDirection() * 5.5f);
                    new LightningBeam(myEye + Run.PlayerEntity.Right * 0.35f, beamEnd, UberTimer > 0f ? Color.Gold : Rgb(255, 75, 95), 0.11f);
                }
            }

            // Aigis: Реактивный полёт в режиме Оргии + защитный паровой сброс при перегреве
            if (CharKey == "aigis" && OrgiaTimer > 0f && !Run.PlayerGrounded && Raylib.IsKeyDown(KeyboardKey.Space))
            {
                Run.PlayerVelY = Math.Clamp(Run.PlayerVelY + 34f * dt, -1.5f, 7.5f);
            }

            // Reimu: Плавный спад стаков Грейза вне опасности
            if (ReimuGrazeStacks > 0)
            {
                ReimuGrazeDecayTimer -= dt;
                if (ReimuGrazeDecayTimer <= 0f)
                {
                    ReimuGrazeDecayTimer = 2.5f;
                    ReimuGrazeStacks = Math.Max(0, ReimuGrazeStacks - 1);
                }
            }

            // Slark: Отложенный 2-й импульс Тёмного Договора со снятием контроля!
            if (SlarkDarkPactPulseTimer > 0f)
            {
                SlarkDarkPactPulseTimer -= dt;
                if (SlarkDarkPactPulseTimer <= 0f)
                {
                    SoundManager.Play("magic_cast", 0.85f, 1.25f);
                    new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(65, 215, 225), 5.8f, 0.30f);
                    SlarkEssenceStacks += 1;
                    foreach (var e in CombatManager.Enemies.ToArray())
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 8.5f)
                            CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 2.15f, Rgb(65, 215, 225), lifesteal: 5.0f, canProc: true);
                }
            }

            // Freeman: Удержание захваченного живого врага Синей Грави-пушкой
            if (CharKey == "freeman" && IsValid(FreemanGrabbedEnemy))
            {
                Vector3 holdPos = Run.PlayerPos + new Vector3(0, 1.4f, 0) + GetAimDirection() * 3.4f;
                FreemanGrabbedEnemy!.Position = holdPos;
                FreemanGrabbedEnemy.StunTimer = 0.4f;
            }

            // Cheburashka: Ритмичный пульс Гармошки Гены!
            if (CharKey == "cheburashka" && GenaStandTimer > 0f)
            {
                ChebAccordionPulseTimer -= dt;
                if (ChebAccordionPulseTimer <= 0f)
                {
                    ChebAccordionPulseTimer = 1.6f;
                    new VisualFX(Run.PlayerPos + new Vector3(0, 1.2f, 0), Color.Gold, 4.2f, 0.24f);
                    Run.HealPlayer(6.0f);
                }
            }

            // Lappland Decadenza: Поддержка 2 постоянных парящих Волчьих Фамильяров!
            if (CharKey == "lappland" && ProgressData.Instance.GetSelectedSkin("lappland") == 2)
            {
                DecadenzaFamiliars.RemoveAll(f => !IsValid(f));
                while (DecadenzaFamiliars.Count < 2)
                {
                    var fam = new DecadenzaWolfFamiliarEntity(Run.PlayerPos + new Vector3(0, 2.0f, 0), DecadenzaFamiliars.Count);
                    DecadenzaFamiliars.Add(fam);
                    Run.StageEntities.Add(fam);
                }
            }

            // Freddy: Авто-восстановление батареи и завершение овердрайва Марша Тореадора
            if (CharKey == "freddy")
            {
                if (FreddyPowerOutageTimer > 0f)
                {
                    FreddyPowerOutageTimer -= dt;
                    if (FreddyPowerOutageTimer <= 0f)
                    {
                        FreddyBattery = 100f;
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 45f);
                        Run.ShowBanner("ПИТАНИЕ FAZBEAR ВОССТАНОВЛЕНО [100%]!", Color.Lime, 1.5f);
                    }
                }
                else
                {
                    FreddyBattery = MathF.Min(100f, FreddyBattery + 5.5f * dt);
                }
            }

            // Vyper: Оставляет токсичный след яда во время слайда!
            if (CharKey == "vyper" && VyperSlideTimer > 0f)
            {
                _vyperPoolTick -= dt;
                if (_vyperPoolTick <= 0f)
                {
                    _vyperPoolTick = 0.22f;
                    Run.StageEntities.Add(new VyperVenomPoolEntity(Run.PlayerPos, BaseDamage * 0.72f));
                }
            }

            // Vergil: Шкала Концентрации (Motivation) растёт в спокойном/точном бою!
            if (CharKey == "dmc_trio" && ProgressData.Instance.GetSelectedSkin("dmc_trio") == 2)
            {
                float rate = isMoving ? 5.5f : 11.0f;
                VergilConcentration = Math.Clamp(VergilConcentration + rate * dt, 0f, 100f);
            }
        }
    }
}
