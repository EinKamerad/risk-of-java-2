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
    public class PipisBombEntity : Entity
    {
        public Vector3 Vel;
        public float BaseDmg;
        public int BouncesLeft;
        public bool IsNeo;
        public float Lifetime = 4.5f;

        public PipisBombEntity(Vector3 pos, Vector3 vel, float dmg, bool isNeo = false, int bounces = 3)
            : base(model: "sphere", position: pos,
                scale: isNeo ? new(0.62f, 0.48f, 0.62f) : new(0.50f, 0.38f, 0.50f),
                color: isNeo ? Rgb(255, 90, 210) : Rgb(35, 165, 255), unlit: true)
        {
            Vel = vel;
            BaseDmg = dmg;
            BouncesLeft = bounces;
            IsNeo = isNeo;
            new Entity(parent: this, model: "sphere", scaleUniform: 1.32f,
                color: isNeo ? Rgb(255, 235, 75, 95) : Rgb(120, 215, 255, 90), unlit: true);
        }

        private void Detonate()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            Vector3 p = Position;
            Color fxCol = IsNeo ? Rgb(255, 110, 220) : Rgb(55, 185, 255);
            SoundManager.Play("explosion", 0.75f, IsNeo ? 1.15f : 1.30f);
            new VisualFX(p, fxCol, IsNeo ? 5.2f : 4.0f, 0.32f);
            float rad = IsNeo ? 5.8f : 4.4f;
            for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
            {
                var e = CombatManager.Enemies[i];
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(p, e.Position) <= rad + e.HitRadius)
                    CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight * 0.5f, 0), BaseDmg, fxCol, aoe: 0f, lifesteal: IsNeo ? 3f : 0f, canProc: true);
            }
            Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            Lifetime -= dt;
            Vel.Y -= 21f * dt;
            Position += Vel * dt;
            RotationY += 460f * dt;
            RotationX += 310f * dt;

            for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
            {
                var e = CombatManager.Enemies[i];
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position + new Vector3(0, e.HitHeight * 0.5f, 0)) <= 1.25f + e.HitRadius)
                {
                    Detonate();
                    return;
                }
            }

            float floorY = Physics.GetSurfaceHeight(X, Z, Y);
            if (Y <= floorY + 0.25f)
            {
                Y = floorY + 0.25f;
                if (BouncesLeft > 0 && Lifetime > 0.1f)
                {
                    BouncesLeft--;
                    Vel.Y = 7.8f + BouncesLeft * 1.1f;
                    Vel.X *= 0.88f;
                    Vel.Z *= 0.88f;
                    SoundManager.Play("coin_flip", 0.55f, 0.85f + BouncesLeft * 0.12f);
                    new VisualFX(Position, Rgb(90, 200, 255), 1.6f, 0.18f);
                }
                else
                {
                    Detonate();
                    return;
                }
            }

            if (Lifetime <= 0f) Detonate();
        }
    }

    public class BinahPillarEntity : Entity
    {
        private Vector3 _dir;
        private readonly float _dmg;
        private float _life = 2.6f;
        private float _windup = 0.22f;
        private float _fxTick;
        private readonly HashSet<CombatTarget> _hitTargets = new();
        private readonly Entity _crown;

        public BinahPillarEntity(Vector3 pos, Vector3 dir, float dmg) : base(position: pos)
        {
            _dir = dir.LengthSquared() > 0.001f ? Vector3.Normalize(new Vector3(dir.X, 0f, dir.Z)) : Vector3.UnitZ;
            if (_dir.LengthSquared() < 0.001f) _dir = Vector3.UnitZ;
            _dmg = dmg;
            new Entity(parent: this, model: "cube", scale: new(1.25f, 4.4f, 1.25f), position: new(0, 2.3f, 0), color: Rgb(18, 16, 22));
            new Entity(parent: this, model: "cube", scale: new(1.38f, 0.32f, 1.38f), position: new(0, 1.2f, 0), color: Rgb(235, 190, 55), unlit: true);
            new Entity(parent: this, model: "cube", scale: new(1.38f, 0.32f, 1.38f), position: new(0, 3.4f, 0), color: Rgb(235, 190, 55), unlit: true);
            _crown = new Entity(parent: this, model: "diamond", scale: new(1.1f, 1.1f, 1.1f), position: new(0, 4.9f, 0), color: Rgb(255, 215, 75), unlit: true);
            new Entity(parent: this, model: "sphere", scale: new(3.6f, 0.12f, 3.6f), position: new(0, 0.15f, 0), color: Rgb(235, 190, 55, 85), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _crown.RotationY += 260f * dt;
            RotationY += 95f * dt;
            if (_windup > 0f)
            {
                _windup -= dt;
                return;
            }

            _life -= dt;
            Position += _dir * (24f * dt);
            Y = Physics.GetSurfaceHeight(X, Z, Y);

            _fxTick -= dt;
            if (_fxTick <= 0f)
            {
                _fxTick = 0.14f;
                new VisualFX(Position + new Vector3(0, 1.2f, 0), Rgb(240, 195, 60), 2.6f, 0.20f);
            }

            var ctrl = CombatManager.Controller;
            for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
            {
                var e = CombatManager.Enemies[i];
                if (!IsValid(e) || e.IsAlly || _hitTargets.Contains(e)) continue;
                if (Physics.Distance2D(Position, e.Position) <= 2.6f + e.HitRadius)
                {
                    _hitTargets.Add(e);
                    e.StunTimer = MathF.Max(e.StunTimer, 1.5f);
                    ctrl?.ApplyBinahFairy(e, 1.25f);
                    CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight * 0.5f, 0), _dmg, Rgb(245, 200, 65), aoe: 2.2f, canProc: true);
                }
            }

            if (_life <= 0f)
            {
                IsDestroyed = true;
                SoundManager.Play("slam_land", 0.8f, 0.9f);
                new VisualFX(Position + new Vector3(0, 1.5f, 0), Rgb(245, 200, 65), 5.5f, 0.35f);
                Destroy(this);
            }
        }
    }

    public class MeatLanternLureEntity : Entity
    {
        private float _life = 6.5f;
        private float _chompTick = 0.25f;
        private readonly float _biteDmg;
        private readonly Entity _bulb;

        public MeatLanternLureEntity(Vector3 pos, float biteDmg) : base(position: pos)
        {
            _biteDmg = biteDmg;
            new Entity(parent: this, model: "sphere", scale: new(7.5f, 0.12f, 7.5f), position: new(0, 0.1f, 0), color: Rgb(95, 190, 75, 85), unlit: true);
            new Entity(parent: this, model: "cube", scale: new(0.18f, 1.5f, 0.18f), position: new(0, 0.75f, 0), color: Rgb(65, 110, 50));
            _bulb = new Entity(parent: this, model: "sphere", scaleUniform: 0.65f, position: new(0, 1.65f, 0), color: Rgb(190, 255, 120), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            _chompTick -= dt;
            _bulb.Position = new Vector3(0, 1.65f + MathF.Sin(_life * 6.5f) * 0.18f, 0);

            for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
            {
                var e = CombatManager.Enemies[i];
                if (!IsValid(e) || e.IsAlly) continue;
                float d = Physics.Distance2D(Position, e.Position);
                if (d > 1.2f && d <= 13.0f)
                {
                    Vector3 pull = Vector3.Normalize(new Vector3(X - e.X, 0f, Z - e.Z));
                    float pullSpd = (e.IsBoss ? 3.5f : 7.8f) * dt;
                    e.Position += pull * pullSpd;
                }
            }

            if (_chompTick <= 0f)
            {
                _chompTick = 0.65f;
                SoundManager.Play("slam_land", 0.75f, 1.15f);
                new VisualFX(Position + new Vector3(0, 0.8f, 0), Rgb(160, 45, 55), 5.0f, 0.28f);
                var ctrl = CombatManager.Controller;
                for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
                {
                    var e = CombatManager.Enemies[i];
                    if (!IsValid(e) || e.IsAlly) continue;
                    if (Physics.Distance2D(Position, e.Position) <= 6.2f + e.HitRadius)
                    {
                        e.BleedTimer = MathF.Max(e.BleedTimer, 5.0f);
                        e.BleedDps = MathF.Max(e.BleedDps, _biteDmg * 0.35f);
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight * 0.5f, 0), _biteDmg, Rgb(140, 240, 95), aoe: 0f, lifesteal: 8f, canProc: true);
                        ctrl?.Run.HealPlayer(4.0f);
                    }
                }
            }

            if (_life <= 0f)
            {
                IsDestroyed = true;
                Destroy(this);
            }
        }
    }

    public class OriginiumPrismEntity : Entity
    {
        private float _life = 10.0f;
        private float _fireTick = 0.25f;
        private readonly float _rayDmg;
        private readonly Entity _core;

        public OriginiumPrismEntity(Vector3 pos, float rayDmg) : base(position: pos)
        {
            _rayDmg = rayDmg;
            _core = new Entity(parent: this, model: "diamond", scale: new(0.85f, 1.65f, 0.85f), position: new(0, 1.8f, 0), color: Rgb(25, 18, 28), unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(1.05f, 1.85f, 1.05f), position: new(0, 1.8f, 0), color: Rgb(235, 45, 75, 110), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            _fireTick -= dt;
            _core.RotationY += 160f * dt;

            if (_fireTick <= 0f)
            {
                _fireTick = 0.65f;
                var ctrl = CombatManager.Controller;
                int shots = 0;
                Vector3 beamOrigin = Position + new Vector3(0, 1.9f, 0);
                for (int i = CombatManager.Enemies.Count - 1; i >= 0 && shots < 2; i--)
                {
                    var e = CombatManager.Enemies[i];
                    if (!IsValid(e) || e.IsAlly) continue;
                    if (Vector3.Distance(beamOrigin, e.Position) <= 22f)
                    {
                        shots++;
                        Vector3 hitPt = e.Position + new Vector3(0, e.HitHeight * 0.6f, 0);
                        new LightningBeam(beamOrigin, hitPt, Rgb(240, 50, 80), 0.22f);
                        CombatManager.ApplyPlayerHitEffects(e, hitPt, _rayDmg, Rgb(240, 50, 80), aoe: 1.5f, canProc: true);
                        ctrl?.AddPriestessOriginiumMark(e, 1);
                    }
                }
                if (shots > 0) SoundManager.Play("laser", 0.55f, 1.35f);
            }

            if (_life <= 0f)
            {
                IsDestroyed = true;
                new VisualFX(Position + new Vector3(0, 1.6f, 0), Rgb(235, 45, 75), 3.2f, 0.25f);
                Destroy(this);
            }
        }
    }

    public class ClosureDroneEntity : Entity
    {
        public int SlotIndex;
        public bool IsUpgraded;
        public bool Overdrive;
        private float _orbitAngle;
        private float _shotCd = 0.4f;
        private readonly Entity _lens;

        public ClosureDroneEntity(Vector3 pos, int slotIndex, bool upgraded = false) : base(position: pos)
        {
            SlotIndex = slotIndex;
            IsUpgraded = upgraded;
            _orbitAngle = slotIndex * (MathF.PI * 2f / 3f);
            Color baseLens = slotIndex switch
            {
                0 => Rgb(255, 145, 55),
                1 => Rgb(85, 245, 185),
                _ => Rgb(85, 210, 255)
            };
            new Entity(parent: this, model: "sphere", scale: new(0.52f, 0.36f, 0.52f), color: Rgb(55, 65, 82));
            new Entity(parent: this, model: "cube", scale: new(0.95f, 0.08f, 0.22f), position: new(0, 0.18f, 0), color: Rgb(180, 195, 215));
            _lens = new Entity(parent: this, model: "sphere", scaleUniform: 0.24f, position: new(0, 0f, 0.26f), color: baseLens, unlit: true);
        }

        public void Upgrade()
        {
            IsUpgraded = true;
            _lens.Color = Rgb(255, 215, 65);
            Scale = new Vector3(1.22f, 1.22f, 1.22f);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var ctrl = CombatManager.Controller;
            if (ctrl == null || ctrl.CharKey != "closure")
            {
                IsDestroyed = true;
                Destroy(this);
                return;
            }

            _orbitAngle += (Overdrive ? 3.4f : 1.75f) * dt;
            float radius = 2.35f;
            Vector3 targetPos = ctrl.Run.PlayerPos + new Vector3(
                MathF.Cos(_orbitAngle) * radius,
                2.15f + MathF.Sin(_orbitAngle * 2f) * 0.22f,
                MathF.Sin(_orbitAngle) * radius);
            Position = Vector3.Lerp(Position, targetPos, Math.Clamp(dt * 10f, 0f, 1f));
            RotationY += 180f * dt;

            _shotCd -= dt * (Overdrive ? 2.2f : 1.0f);
            if (_shotCd <= 0f)
            {
                _shotCd = IsUpgraded ? 0.58f : 0.82f;
                CombatTarget? best = null;
                float bestD = 26f;
                for (int i = 0; i < CombatManager.Enemies.Count; i++)
                {
                    var e = CombatManager.Enemies[i];
                    if (IsValid(e) && !e.IsAlly)
                    {
                        float d = Vector3.Distance(Position, e.Position);
                        if (d < bestD) { bestD = d; best = e; }
                    }
                }
                if (best != null)
                {
                    ctrl.FireClosureSpecializedDrone(this, best);
                }
            }
        }
    }

    public partial class SurvivorCombatController
    {
        private float _susieUltTimer;
        private float _susieStormTimer;
        private float _susieStormTick;

        private float _spamtonKromerMult = 1.0f;
        private float _spamtonKromerTimer;
        private float _spamtonFrenzyTimer;
        private float _spamtonFrenzyTick;

        private float _binahTeaPoiseTimer;
        private float _binahUltPillarTimer;
        private float _binahUltPillarTick;

        private float _donBannerTimer;
        private float _donBurrowTimer;
        private int _donHardblood;
        private float _donBloodShieldTimer;
        private float _donWaltzTimer;
        private float _donAscendTimer;
        private float _donLanternRegenTick;

        private float _priestessPhaseTimer;
        private float _priestessUltTimer;
        private float _priestessUltTick;

        private readonly List<ClosureDroneEntity> _closureDrones = new();
        private float _closureShieldTick;
        private float _closureOverdriveTimer;

        public void ApplyBinahFairy(CombatTarget e, float mult = 1.0f)
        {
            if (!IsValid(e) || e.IsAlly) return;
            e.FairyTimer = 5.0f;
            e.FairyDps = MathF.Max(e.FairyDps, BaseDamage * 0.55f * mult);
        }

        public void AddPriestessOriginiumMark(CombatTarget e, int count = 1)
        {
            if (!IsValid(e) || e.IsAlly) return;
            e.OriginiumMarks += count;
            if (e.OriginiumMarks >= 3)
            {
                e.OriginiumMarks = 0;
                Vector3 hitPt = e.Position + new Vector3(0, e.HitHeight * 0.55f, 0);
                Shield = MathF.Min(Run.PlayerMaxHp * 0.90f, Shield + 12f);
                SoundManager.Play("explosion", 0.7f, 1.35f);
                new VisualFX(hitPt, Rgb(235, 40, 75), 4.4f, 0.30f);
                for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
                {
                    var other = CombatManager.Enemies[i];
                    if (IsValid(other) && !other.IsAlly && Vector3.Distance(hitPt, other.Position) <= 5.5f + other.HitRadius)
                        CombatManager.ApplyPlayerHitEffects(other, other.Position + new Vector3(0, other.HitHeight * 0.5f, 0), BaseDamage * 2.15f, Rgb(235, 40, 75), aoe: 0f, canProc: true);
                }
            }
        }

        private void EnsureClosureDrone(bool upgradeOrAdd)
        {
            _closureDrones.RemoveAll(d => !IsValid(d));
            if (_closureDrones.Count == 0 || (upgradeOrAdd && _closureDrones.Count < 3))
            {
                var d = new ClosureDroneEntity(Run.PlayerPos + new Vector3(0, 2.2f, 0), _closureDrones.Count, upgraded: upgradeOrAdd && _closureDrones.Count > 0);
                d.Overdrive = _closureOverdriveTimer > 0f;
                _closureDrones.Add(d);
                Run.StageEntities.Add(d);
            }
            else if (upgradeOrAdd)
            {
                foreach (var d in _closureDrones)
                    if (!d.IsUpgraded) { d.Upgrade(); break; }
            }
        }

        private float GetPack1DamageMult()
        {
            int skin = ProgressData.Instance.GetSelectedSkin(CharKey);
            return CharKey switch
            {
                "susie" => 1.0f + 0.45f * Math.Clamp(1f - Run.PlayerHp / MathF.Max(1f, Run.PlayerMaxHp), 0f, 1f) + (_susieUltTimer > 0f ? 0.25f : 0f),
                "spamton" => _spamtonKromerMult * (_spamtonFrenzyTimer > 0f ? 1.25f : 1.0f) * (skin == 2 ? 1.15f : 1.0f),
                "binah" => _binahUltPillarTimer > 0f ? 1.20f : 1.0f,
                "don_quixote" => skin switch
                {
                    0 => 1.0f + (_donBannerTimer > 0f ? 0.22f : 0f) + Math.Clamp((MoveSpeed - 8.5f) * 0.03f, 0f, 0.35f),
                    2 => 1.0f + (_donHardblood / 30f) * 0.45f + (_donAscendTimer > 0f ? 0.25f : 0f),
                    _ => 1.08f
                },
                "priestess" => _priestessUltTimer > 0f ? 1.22f : 1.0f,
                "closure" => _closureOverdriveTimer > 0f ? 1.22f : 1.0f,
                _ => 1.0f
            };
        }

        private float GetPack1MoveSpeedMult()
        {
            int skin = ProgressData.Instance.GetSelectedSkin(CharKey);
            return CharKey switch
            {
                "susie" => _susieUltTimer > 0f ? 1.22f : 1.0f,
                "spamton" => (skin == 2 ? (1.15f + (6 - Math.Clamp(SpamtonNeoStrings, 0, 6)) * 0.08f) : 1.0f) * (_spamtonFrenzyTimer > 0f ? 1.18f : 1.0f),
                "don_quixote" => (skin == 0 && _donBannerTimer > 0f ? 1.28f : 1.0f) * (_donBurrowTimer > 0f ? 1.45f : 1.0f) * (_donAscendTimer > 0f ? 1.20f : 1.0f),
                "closure" => _closureOverdriveTimer > 0f ? 1.18f : 1.0f,
                _ => 1.0f
            };
        }

        private float GetPack1FireRateMult()
        {
            int skin = ProgressData.Instance.GetSelectedSkin(CharKey);
            return CharKey switch
            {
                "susie" => 1.0f + 0.25f * Math.Clamp(1f - Run.PlayerHp / MathF.Max(1f, Run.PlayerMaxHp), 0f, 1f) + (_susieUltTimer > 0f ? 0.25f : 0f),
                "spamton" => (_spamtonFrenzyTimer > 0f ? 1.45f : 1.0f) * (skin == 2 ? (1.0f + (6 - Math.Clamp(SpamtonNeoStrings, 0, 6)) * 0.14f) : 1.0f),
                "don_quixote" => (_donBannerTimer > 0f || _donAscendTimer > 0f) ? 1.25f : 1.0f,
                "closure" => _closureOverdriveTimer > 0f ? 1.35f : 1.0f,
                _ => 1.0f
            };
        }

        private float GetPack1CritBonus()
        {
            int skin = ProgressData.Instance.GetSelectedSkin(CharKey);
            return CharKey switch
            {
                "spamton" => _spamtonKromerMult > 1.30f ? 0.18f : 0.06f,
                "binah" => 0.08f,
                "don_quixote" => skin == 2 ? _donHardblood * 0.005f : 0.05f,
                _ => 0f
            };
        }

        private CombatTarget? FindAimedPack1Target(Vector3 origin, Vector3 aimDir, float maxDist, float minDot = 0.55f)
        {
            CombatTarget? best = null;
            float bestScore = float.MaxValue;
            Vector3 normAim = aimDir.LengthSquared() > 0.001f ? Vector3.Normalize(aimDir) : Vector3.UnitZ;
            for (int i = 0; i < CombatManager.Enemies.Count; i++)
            {
                var e = CombatManager.Enemies[i];
                if (!IsValid(e) || e.IsAlly) continue;
                Vector3 toE = (e.Position + new Vector3(0, e.HitHeight * 0.55f, 0)) - origin;
                float dist = toE.Length();
                if (dist > maxDist || dist < 0.05f) continue;
                float dot = Vector3.Dot(normAim, toE / dist);
                if (dot < minDot && dist > 3.5f) continue;
                float score = dist * (2.0f - dot);
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        private bool TryUsePack1M1() => false;

        private bool TryFirePack1Primary(Vector3 aimDir, Vector3 spawnPos, float damageMult, int m1Var)
        {
            int skinIdx = ProgressData.Instance.GetSelectedSkin(CharKey);
            Vector3 right = Vector3.Cross(aimDir, Vector3.UnitY);
            right = right.LengthSquared() > 0.001f ? Vector3.Normalize(right) : Vector3.UnitX;

            switch (CharKey)
            {
                case "susie":
                {
                    TriggerLoLSpellblade();
                    if (m1Var == 0)
                    {
                        SoundManager.Play("shoot_heavy", 0.9f, 0.85f);
                        PerformMeleeAttack(5.4f, 4.4f, 1.55f * damageMult, Rgb(235, 70, 185), lifesteal: _susieUltTimer > 0f ? 5f : 0f, knockback: 5.5f, aoe: 2.8f);
                        new Projectile(spawnPos, aimDir, 30f, BaseDamage * 0.80f * damageMult, 0.55f, Rgb(235, 70, 185), aoe: 2.0f, pierce: true, maxLifetime: 0.45f);
                    }
                    else
                    {
                        SoundManager.Play("shoot_heavy", 0.85f, 1.08f);
                        new Projectile(spawnPos, aimDir, 40f, BaseDamage * 1.48f * damageMult, 0.68f, Rgb(255, 85, 195), aoe: 2.4f, pierce: true, maxLifetime: 1.4f);
                    }
                    return true;
                }
                case "spamton":
                {
                    TriggerLoLSpellblade();
                    _spamtonKromerMult = 0.90f + (float)_rng.NextDouble() * 0.65f;
                    if (skinIdx == 2)
                    {
                        if (m1Var == 0)
                        {
                            SoundManager.Play("laser", 0.85f, 1.15f);
                            Color c1 = (_shootSide & 1) == 0 ? Rgb(255, 95, 210) : Rgb(255, 235, 70);
                            Color c2 = (_shootSide & 1) == 0 ? Rgb(255, 235, 70) : Rgb(255, 95, 210);
                            new Projectile(spawnPos - right * 0.28f, aimDir, 56f, BaseDamage * 0.82f * damageMult, 0.36f, c1, aoe: 1.5f, pierce: true, maxLifetime: 1.8f);
                            new Projectile(spawnPos + right * 0.28f, aimDir, 56f, BaseDamage * 0.82f * damageMult, 0.36f, c2, aoe: 1.5f, maxLifetime: 1.8f);
                        }
                        else
                        {
                            SoundManager.Play("railgun", 0.8f, 1.25f);
                            new VisualFX(spawnPos, Rgb(255, 235, 75), 1.8f, 0.16f);
                            new Projectile(spawnPos, aimDir, 50f, BaseDamage * 2.15f * damageMult, 0.82f, Rgb(255, 110, 215), aoe: 3.6f, pierce: true, maxLifetime: 2.2f);
                        }
                    }
                    else
                    {
                        if (m1Var == 0)
                        {
                            SoundManager.Play("shoot_light", 0.85f, 0.92f + (float)_rng.NextDouble() * 0.32f);
                            for (int i = -1; i <= 1; i += 2)
                            {
                                Vector3 jit = Vector3.Normalize(aimDir + right * (i * 0.045f));
                                Color kCol = i < 0 ? Rgb(110, 245, 130) : Rgb(255, 225, 75);
                                new Projectile(spawnPos, jit, 46f, BaseDamage * 0.72f * damageMult, 0.32f, kCol, bounces: 1, maxLifetime: 1.9f);
                            }
                        }
                        else
                        {
                            SoundManager.Play("zap", 0.8f, 1.2f);
                            for (int i = -1; i <= 1; i++)
                            {
                                Vector3 fan = Vector3.Normalize(aimDir + right * (i * 0.06f));
                                new Projectile(spawnPos, fan, 44f, BaseDamage * 0.56f * damageMult, 0.30f, Rgb(95, 215, 255), homing: true, maxLifetime: 1.8f);
                            }
                        }
                    }
                    return true;
                }
                case "binah":
                {
                    TriggerLoLSpellblade();
                    if (m1Var == 0)
                    {
                        SoundManager.Play("magic_cast", 0.85f, 1.15f);
                        PerformMeleeAttack(6.2f, 3.8f, 1.10f * damageMult, Rgb(245, 200, 65), aoe: 2.2f);
                        new Projectile(spawnPos, aimDir, 52f, BaseDamage * 1.05f * damageMult, 0.48f, Rgb(245, 200, 65), aoe: 2.0f, pierce: true, maxLifetime: 0.9f);
                        var t = FindAimedPack1Target(spawnPos, aimDir, 22f, 0.65f);
                        if (t != null) ApplyBinahFairy(t, 1.0f);
                    }
                    else
                    {
                        SoundManager.Play("shoot_light", 0.85f, 1.35f);
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector3 fan = Vector3.Normalize(aimDir + right * (i * 0.09f));
                            new Projectile(spawnPos + right * (i * 0.2f), fan, 56f, BaseDamage * 0.62f * damageMult, 0.30f, Rgb(250, 205, 70), homing: true, pierce: true, maxLifetime: 1.8f);
                        }
                        var t = FindAimedPack1Target(spawnPos, aimDir, 26f, 0.65f);
                        if (t != null) ApplyBinahFairy(t, 1.0f);
                    }
                    return true;
                }
                case "don_quixote":
                {
                    TriggerLoLSpellblade();
                    if (skinIdx == 0)
                    {
                        float spdScale = 1.0f + Math.Clamp((MoveSpeed - 8.0f) * 0.05f, 0f, 0.55f);
                        SoundManager.Play("shoot_heavy", 0.8f, 1.18f);
                        if (m1Var == 0)
                        {
                            PerformMeleeAttack(5.8f, 2.8f, 1.50f * damageMult * spdScale, Rgb(255, 215, 65), knockback: 4.0f, aoe: 2.0f);
                            new Projectile(spawnPos, aimDir, 46f, BaseDamage * 0.65f * damageMult * spdScale, 0.38f, Rgb(255, 220, 75), pierce: true, maxLifetime: 0.35f);
                        }
                        else
                        {
                            PerformMeleeAttack(5.4f, 3.2f, 1.35f * damageMult * spdScale, Rgb(255, 215, 65), scissorCross: true, aoe: 2.2f);
                        }
                    }
                    else if (skinIdx == 1)
                    {
                        SoundManager.Play("shoot_heavy", 0.85f, 0.92f);
                        PerformMeleeAttack(5.5f, m1Var == 0 ? 3.5f : 4.4f, (m1Var == 0 ? 1.48f : 1.38f) * damageMult, Rgb(135, 235, 95), lifesteal: 5.5f, scissorCross: m1Var == 1, aoe: 2.4f);
                        var t = FindAimedPack1Target(Run.PlayerPos, aimDir, 6.5f, 0.25f);
                        if (t != null) { t.BleedTimer = MathF.Max(t.BleedTimer, 4.5f); t.BleedDps = MathF.Max(t.BleedDps, BaseDamage * 0.45f); }
                    }
                    else
                    {
                        _donHardblood = Math.Min(30, _donHardblood + 2);
                        SoundManager.Play("shoot_heavy", 0.85f, 1.05f);
                        if (m1Var == 0)
                        {
                            PerformMeleeAttack(6.2f, 3.2f, 1.55f * damageMult, Rgb(220, 28, 58), lifesteal: 4.5f, aoe: 2.5f);
                            new Projectile(spawnPos, aimDir, 48f, BaseDamage * 0.78f * damageMult, 0.44f, Rgb(230, 35, 65), lifesteal: 3.0f, pierce: true, maxLifetime: 0.55f);
                        }
                        else
                        {
                            PerformMeleeAttack(7.2f, 5.0f, 1.42f * damageMult, Rgb(235, 42, 75), lifesteal: 4.0f, knockback: 3.5f, scissorCross: true, aoe: 3.0f);
                        }
                    }
                    return true;
                }
                case "priestess":
                {
                    TriggerLoLSpellblade();
                    if (m1Var == 0)
                    {
                        SoundManager.Play("magic_cast", 0.8f, 1.25f);
                        new Projectile(spawnPos - right * 0.22f, aimDir, 54f, BaseDamage * 0.68f * damageMult, 0.32f, Rgb(235, 45, 78), aoe: 1.4f, homing: true, maxLifetime: 1.9f);
                        new Projectile(spawnPos + right * 0.22f, aimDir, 54f, BaseDamage * 0.68f * damageMult, 0.32f, Rgb(35, 22, 42), aoe: 1.4f, homing: true, maxLifetime: 1.9f);
                        var t = FindAimedPack1Target(spawnPos, aimDir, 28f, 0.65f);
                        if (t != null) AddPriestessOriginiumMark(t, 1);
                    }
                    else
                    {
                        SoundManager.Play("laser", 0.8f, 1.35f);
                        var t = FindAimedPack1Target(spawnPos, aimDir, 38f, 0.72f);
                        Vector3 endPt = t != null ? t.Position + new Vector3(0, t.HitHeight * 0.55f, 0) : spawnPos + aimDir * 34f;
                        new LightningBeam(spawnPos, endPt, Rgb(240, 50, 82), 0.20f);
                        if (t != null)
                        {
                            CombatManager.ApplyPlayerHitEffects(t, endPt, BaseDamage * 1.48f * damageMult, Rgb(240, 50, 82), aoe: 1.8f, canProc: true);
                            AddPriestessOriginiumMark(t, 1);
                        }
                    }
                    TryRefractPriestessPrisms(spawnPos, aimDir, BaseDamage * 0.92f * damageMult);
                    return true;
                }
                case "closure":
                {
                    TriggerLoLSpellblade();
                    EnsureClosureDrone(false);
                    if (m1Var == 0)
                    {
                        SoundManager.Play("shoot_light", 0.8f, 1.22f);
                        new Projectile(spawnPos, aimDir, 55f, BaseDamage * 1.12f * damageMult, 0.34f, Rgb(70, 225, 255), aoe: 1.2f, maxLifetime: 1.8f);
                        var t = FindAimedPack1Target(spawnPos, aimDir, 32f, 0.65f);
                        Vector3 focusPt = t != null ? t.Position + new Vector3(0, t.HitHeight * 0.55f, 0) : spawnPos + aimDir * 24f;
                        foreach (var d in _closureDrones)
                        {
                            if (!IsValid(d)) continue;
                            Vector3 dAim = Vector3.Normalize(focusPt - d.Position);
                            new Projectile(d.Position, dAim, 60f, BaseDamage * 0.36f * damageMult, 0.22f, Rgb(110, 240, 255), maxLifetime: 1.2f);
                        }
                    }
                    else
                    {
                        SoundManager.Play("zap", 0.85f, 1.15f);
                        var t = FindAimedPack1Target(spawnPos, aimDir, 22f, 0.55f);
                        Vector3 endPt = t != null ? t.Position + new Vector3(0, t.HitHeight * 0.55f, 0) : spawnPos + aimDir * 18f;
                        new LightningBeam(spawnPos, endPt, Rgb(80, 235, 255), 0.22f);
                        if (t != null)
                        {
                            t.SlowTimer = MathF.Max(t.SlowTimer, 1.6f);
                            CombatManager.ApplyPlayerHitEffects(t, endPt, BaseDamage * 1.22f * damageMult, Rgb(80, 235, 255), aoe: 2.2f, canProc: true);
                            int chains = 0;
                            for (int i = CombatManager.Enemies.Count - 1; i >= 0 && chains < 2; i--)
                            {
                                var ce = CombatManager.Enemies[i];
                                if (IsValid(ce) && !ce.IsAlly && ce != t && Vector3.Distance(t.Position, ce.Position) <= 8.0f)
                                {
                                    chains++;
                                    Vector3 cPt = ce.Position + new Vector3(0, ce.HitHeight * 0.55f, 0);
                                    new LightningBeam(endPt, cPt, Rgb(255, 175, 65), 0.20f);
                                    CombatManager.ApplyPlayerHitEffects(ce, cPt, BaseDamage * 0.78f * damageMult, Rgb(255, 175, 65), canProc: true);
                                }
                            }
                        }
                    }
                    return true;
                }
                default:
                    return false;
            }
        }

        private bool TryUsePack1M2(Vector3 aimDir, Vector3 spawnPos)
        {
            if (CharKey is not ("susie" or "spamton" or "binah" or "don_quixote" or "priestess" or "closure"))
                return false;

            float cdr = MathF.Pow(0.88f, CombatManager.GetItemCount("backup_mag")) * MathF.Pow(0.80f, CombatManager.GetItemCount("alien_head"));
            M2Cd = MathF.Max(0.25f, Cfg.Skills["m2"].Cd * cdr);
            TriggerLoLSpellblade();
            Run.Parts.TriggerSkillAnim("m2", 0.42f);

            int skinIdx = ProgressData.Instance.GetSelectedSkin(CharKey);
            int m2Var = SkillVar("m2");
            Vector3 right = Vector3.Cross(aimDir, Vector3.UnitY);
            right = right.LengthSquared() > 0.001f ? Vector3.Normalize(right) : Vector3.UnitX;

            switch (CharKey)
            {
                case "susie":
                {
                    SusieBusterBoosted = false;
                    if (m2Var == 0)
                    {
                        SoundManager.Play("railgun", 0.85f, 1.05f);
                        PerformMeleeAttack(5.8f, 4.6f, 1.8f, Rgb(240, 65, 200), knockback: 6f, aoe: 3.0f);
                        SusieActiveBusterProj = new Projectile(spawnPos, aimDir, 45f, BaseDamage * 3.45f, 0.95f, Rgb(240, 65, 200), aoe: 4.2f, pierce: true, maxLifetime: 2.2f);
                    }
                    else
                    {
                        SoundManager.Play("railgun", 0.95f, 0.85f);
                        new VisualFX(spawnPos, Rgb(255, 70, 55), 3.4f, 0.26f);
                        SusieActiveBusterProj = new Projectile(spawnPos, aimDir, 48f, BaseDamage * 4.45f, 1.15f, Rgb(255, 70, 55), aoe: 5.2f, pierce: true, maxLifetime: 2.4f);
                        var t = FindAimedPack1Target(spawnPos, aimDir, 30f, 0.45f);
                        if (t != null) t.ArmorBreakTimer = MathF.Max(t.ArmorBreakTimer, 6.0f);
                    }
                    return true;
                }
                case "spamton":
                {
                    if (skinIdx == 2)
                    {
                        if (m2Var == 0)
                        {
                            SoundManager.Play("rocket", 0.85f, 1.2f);
                            for (int i = -1; i <= 1; i++)
                            {
                                Vector3 v = Vector3.Normalize(aimDir + right * (i * 0.16f)) * 21f + new Vector3(0, 7.2f, 0);
                                var pipis = new PipisBombEntity(spawnPos + right * (i * 0.35f), v, BaseDamage * 2.15f, isNeo: true, bounces: 3);
                                Run.StageEntities.Add(pipis);
                            }
                        }
                        else
                        {
                            SoundManager.Play("laser", 0.95f, 0.95f);
                            var t = FindAimedPack1Target(spawnPos, aimDir, 28f, 0.45f);
                            Vector3 heartPt = t != null ? t.Position + new Vector3(0, 1.2f, 0) : spawnPos + aimDir * 12f;
                            new LightningBeam(spawnPos, heartPt, Rgb(255, 95, 210), 0.32f);
                            new LightningBeam(heartPt - new Vector3(6f, 0, 0), heartPt + new Vector3(6f, 0, 0), Rgb(255, 235, 75), 0.32f);
                            new LightningBeam(heartPt - new Vector3(0, 0, 6f), heartPt + new Vector3(0, 0, 6f), Rgb(255, 95, 210), 0.32f);
                            new VisualFX(heartPt, Rgb(255, 110, 215), 5.5f, 0.32f);
                            for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
                            {
                                var e = CombatManager.Enemies[i];
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(heartPt, e.Position) <= 6.2f + e.HitRadius)
                                    CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight * 0.5f, 0), BaseDamage * 3.85f, Rgb(255, 110, 215), lifesteal: 8f, canProc: true);
                            }
                        }
                    }
                    else
                    {
                        if (m2Var == 0)
                        {
                            SoundManager.Play("coin_flip", 0.85f, 1.0f);
                            for (int i = -1; i <= 1; i += 2)
                            {
                                Vector3 v = Vector3.Normalize(aimDir + right * (i * 0.12f)) * 18.5f + new Vector3(0, 6.8f, 0);
                                var pipis = new PipisBombEntity(spawnPos, v, BaseDamage * 2.10f, isNeo: false, bounces: 3);
                                Run.StageEntities.Add(pipis);
                            }
                        }
                        else
                        {
                            SoundManager.Play("magic_cast", 0.85f, 1.15f);
                            new Projectile(spawnPos, aimDir, 42f, BaseDamage * 2.95f, 0.65f, Rgb(255, 85, 195), aoe: 3.2f, homing: true, lifesteal: 10f, bounces: 2, maxLifetime: 2.4f);
                        }
                    }
                    return true;
                }
                case "binah":
                {
                    if (m2Var == 0)
                    {
                        SoundManager.Play("slam_land", 0.85f, 0.85f);
                        Vector3 pPos = Run.PlayerPos + Vector3.Normalize(new Vector3(aimDir.X, 0f, aimDir.Z) + new Vector3(0.001f, 0, 0)) * 2.2f;
                        pPos.Y = Physics.GetSurfaceHeight(pPos.X, pPos.Z, Run.PlayerPos.Y);
                        var pillar = new BinahPillarEntity(pPos, aimDir, BaseDamage * 3.65f);
                        Run.StageEntities.Add(pillar);
                    }
                    else
                    {
                        SoundManager.Play("dante_clock", 0.85f, 1.25f);
                        Vector3 flatAim = Vector3.Normalize(new Vector3(aimDir.X, 0f, aimDir.Z) + new Vector3(0.001f, 0, 0));
                        for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
                        {
                            var e = CombatManager.Enemies[i];
                            if (!IsValid(e) || e.IsAlly) continue;
                            Vector3 toE = e.Position - Run.PlayerPos;
                            float d = toE.Length();
                            if (d <= 16.5f && (d < 3.0f || Vector3.Dot(flatAim, Vector3.Normalize(new Vector3(toE.X, 0f, toE.Z))) > 0.42f))
                            {
                                e.StunTimer = MathF.Max(e.StunTimer, 2.0f);
                                e.BleedTimer = MathF.Max(e.BleedTimer, 5.0f);
                                e.BleedDps = MathF.Max(e.BleedDps, BaseDamage * 0.65f);
                                ApplyBinahFairy(e, 1.3f);
                                Vector3 hitPt = e.Position + new Vector3(0, e.HitHeight * 0.55f, 0);
                                new LightningBeam(spawnPos, hitPt, Rgb(245, 200, 65), 0.30f);
                                CombatManager.ApplyPlayerHitEffects(e, hitPt, BaseDamage * 2.85f, Rgb(245, 200, 65), canProc: true);
                            }
                        }
                    }
                    return true;
                }
                case "don_quixote":
                {
                    if (skinIdx == 0)
                    {
                        if (m2Var == 0)
                        {
                            SoundManager.Play("shoot_heavy", 0.9f, 1.25f);
                            PerformMeleeAttack(6.5f, 3.8f, 2.95f, Rgb(255, 215, 65), knockback: 5f, aoe: 2.8f);
                            for (int i = -1; i <= 1; i++)
                                new Projectile(spawnPos + right * (i * 0.25f), aimDir, 52f, BaseDamage * 0.85f, 0.36f, Rgb(255, 215, 65), pierce: true, maxLifetime: 0.45f);
                            for (int i = 0; i < CombatManager.Enemies.Count; i++)
                            {
                                var e = CombatManager.Enemies[i];
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 7.5f)
                                {
                                    e.BleedTimer = MathF.Max(e.BleedTimer, 5.0f);
                                    e.BleedDps = MathF.Max(e.BleedDps, BaseDamage * 0.60f);
                                }
                            }
                        }
                        else
                        {
                            _donBannerTimer = 6.0f;
                            Shield = MathF.Min(Run.PlayerMaxHp, Shield + 28f);
                            SoundManager.Play("heal", 0.9f, 1.15f);
                            new VisualFX(Run.PlayerPos + new Vector3(0, 1f, 0), Rgb(255, 215, 65), 4.5f, 0.32f);
                            Run.ShowBanner("ЗНАМЯ КОРРЕКТОРА! ★", Rgb(255, 220, 75), 1.6f);
                        }
                    }
                    else if (skinIdx == 1)
                    {
                        DonChompKillWindow = 1.6f;
                        SoundManager.Play("slam_land", 0.9f, 1.1f);
                        PerformMeleeAttack(6.2f, 4.8f, 3.55f, Rgb(130, 245, 95), lifesteal: 12f, stunDur: 1.2f, aoe: 3.6f);
                        Run.HealPlayer((Run.PlayerMaxHp - Run.PlayerHp) * 0.22f + 12f);
                        for (int i = 0; i < CombatManager.Enemies.Count; i++)
                        {
                            var e = CombatManager.Enemies[i];
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 7.0f)
                            {
                                e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 6.0f);
                                e.BleedTimer = MathF.Max(e.BleedTimer, 5.0f);
                                e.BleedDps = MathF.Max(e.BleedDps, BaseDamage * 0.60f);
                            }
                        }
                    }
                    else
                    {
                        _donHardblood = Math.Min(30, _donHardblood + 6);
                        if (m2Var == 0)
                        {
                            SoundManager.Play("shoot_heavy", 0.9f, 1.1f);
                            PerformMeleeAttack(7.0f, 5.0f, 3.25f, Rgb(225, 30, 60), lifesteal: 10f, scissorCross: true, aoe: 3.6f);
                            new Projectile(spawnPos, aimDir, 48f, BaseDamage * 1.45f, 0.65f, Rgb(235, 35, 68), aoe: 2.8f, lifesteal: 6f, pierce: true, maxLifetime: 0.9f);
                            for (int i = 0; i < CombatManager.Enemies.Count; i++)
                            {
                                var e = CombatManager.Enemies[i];
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 8.0f)
                                {
                                    e.BleedTimer = MathF.Max(e.BleedTimer, 6.0f);
                                    e.BleedDps = MathF.Max(e.BleedDps, BaseDamage * 0.75f);
                                }
                            }
                        }
                        else
                        {
                            _donBloodShieldTimer = 5.0f;
                            Shield = MathF.Min(Run.PlayerMaxHp, Shield + 45f);
                            SoundManager.Play("magic_cast", 0.85f, 0.9f);
                            new VisualFX(Run.PlayerPos + new Vector3(0, 1.0f, 0), Rgb(225, 30, 60), 5.0f, 0.32f);
                            PerformMeleeAttack(5.5f, 5.5f, 2.1f, Rgb(225, 30, 60), lifesteal: 8f, aoe: 4.0f);
                        }
                    }
                    return true;
                }
                case "priestess":
                {
                    if (m2Var == 0)
                    {
                        SoundManager.Play("magic_cast", 0.9f, 0.85f);
                        var t = FindAimedPack1Target(spawnPos, aimDir, 24f, 0.45f);
                        Vector3 center = t != null ? t.Position : Run.PlayerPos + aimDir * 11f;
                        center.Y = Physics.GetSurfaceHeight(center.X, center.Z, Run.PlayerPos.Y) + 0.8f;
                        new VisualFX(center, Rgb(235, 45, 78), 6.2f, 0.35f);
                        for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
                        {
                            var e = CombatManager.Enemies[i];
                            if (!IsValid(e) || e.IsAlly) continue;
                            if (Vector3.Distance(center, e.Position) <= 10.5f + e.HitRadius)
                            {
                                e.Position = Vector3.Lerp(e.Position, new Vector3(center.X, e.Y, center.Z), e.IsBoss ? 0.25f : 0.62f);
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight * 0.5f, 0), BaseDamage * 2.65f, Rgb(235, 45, 78), canProc: true);
                                AddPriestessOriginiumMark(e, Math.Max(2, 3 - e.OriginiumMarks));
                            }
                        }
                    }
                    else
                    {
                        SoundManager.Play("magic_cast", 0.85f, 1.15f);
                        Vector3 pPos = Run.PlayerPos + Vector3.Normalize(new Vector3(aimDir.X, 0f, aimDir.Z) + new Vector3(0.001f, 0, 0)) * 3.0f;
                        pPos.Y = Physics.GetSurfaceHeight(pPos.X, pPos.Z, Run.PlayerPos.Y);
                        var prism = new OriginiumPrismEntity(pPos, BaseDamage * 1.35f);
                        Run.StageEntities.Add(prism);
                    }
                    return true;
                }
                case "closure":
                {
                    if (m2Var == 0)
                    {
                        SoundManager.Play("heal", 0.85f, 1.2f);
                        EnsureClosureDrone(true);
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 24f);
                        foreach (var d in _closureDrones)
                            if (IsValid(d))
                                new Projectile(d.Position, aimDir, 46f, BaseDamage * 1.45f, 0.38f, Rgb(255, 165, 55), aoe: 2.6f, homing: true, maxLifetime: 2.2f);
                    }
                    else
                    {
                        SoundManager.Play("railgun", 0.95f, 1.1f);
                        var t = FindAimedPack1Target(spawnPos, aimDir, 32f, 0.45f);
                        Vector3 strikePt = t != null ? t.Position : Run.PlayerPos + aimDir * 14f;
                        strikePt.Y = Physics.GetSurfaceHeight(strikePt.X, strikePt.Z, Run.PlayerPos.Y);
                        new LightningBeam(strikePt + new Vector3(0, 28f, 0), strikePt, Rgb(95, 240, 255), 0.40f);
                        new VisualFX(strikePt + new Vector3(0, 0.8f, 0), Rgb(95, 240, 255), 6.5f, 0.38f);
                        for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
                        {
                            var e = CombatManager.Enemies[i];
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(strikePt, e.Position) <= 6.5f + e.HitRadius)
                            {
                                e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 6.0f);
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight * 0.5f, 0), BaseDamage * 4.25f, Rgb(95, 240, 255), canProc: true);
                            }
                        }
                    }
                    return true;
                }
                default:
                    return false;
            }
        }

        private void TeleportPack1Player(Vector3 rawTarget, Vector3 refPos)
        {
            Vector3 safe = Physics.ResolveHorizontalCollision(rawTarget, 0.55f, refPos.Y);
            float floorY = Physics.GetSurfaceHeight(safe.X, safe.Z, refPos.Y + 1.8f);
            safe.Y = MathF.Max(safe.Y, floorY + 0.05f);
            Run.PlayerEntity.Position = safe;
        }

        private bool TryUsePack1Shift(Vector3 myPos, int shVar, float cdrMult, float nrgMult)
        {
            int skinIdx = ProgressData.Instance.GetSelectedSkin(CharKey);
            Vector3 aimDir = GetAimDirection();
            Vector3 flatAim = new(aimDir.X, 0f, aimDir.Z);
            flatAim = flatAim.LengthSquared() > 0.001f ? Vector3.Normalize(flatAim) : Run.PlayerForward;

            switch (CharKey)
            {
                case "susie":
                {
                    SoundManager.Play("slam_land", 0.85f, shVar == 0 ? 0.95f : 0.82f);
                    if (shVar == 0)
                    {
                        Run.ApplyDash(flatAim * (26f * nrgMult), 0.32f);
                        PerformMeleeAttack(5.8f, 4.6f, 2.25f, Rgb(235, 70, 185), stunDur: 1.3f, knockback: 9.0f, aoe: 3.4f);
                    }
                    else
                    {
                        Run.PlayerVelY = 13.0f;
                        Run.ApplyDash(flatAim * (22f * nrgMult), 0.38f);
                        new VisualFX(myPos + new Vector3(0, 0.5f, 0), Rgb(255, 80, 195), 5.0f, 0.30f);
                        PerformMeleeAttack(6.5f, 6.0f, 2.65f, Rgb(255, 80, 195), stunDur: 1.5f, knockback: 7.0f, aoe: 4.5f);
                    }
                    return true;
                }
                case "spamton":
                {
                    if (skinIdx == 2)
                    {
                        if (shVar == 0)
                        {
                            SoundManager.Play("rocket", 0.85f, 1.15f);
                            Run.PlayerVelY = 14.2f;
                            Run.ApplyDash(aimDir * (30f * nrgMult), 0.36f);
                            var pipis = new PipisBombEntity(myPos + new Vector3(0, 0.8f, 0), -flatAim * 6f + new Vector3(0, 5f, 0), BaseDamage * 2.2f, isNeo: true, bounces: 2);
                            Run.StageEntities.Add(pipis);
                        }
                        else
                        {
                            SoundManager.Play("zap", 0.9f, 1.35f);
                            Shield = MathF.Min(Run.PlayerMaxHp, Shield + 32f);
                            var pipis = new PipisBombEntity(myPos + new Vector3(0, 0.8f, 0), new Vector3(0, 6f, 0), BaseDamage * 2.4f, isNeo: true, bounces: 2);
                            Run.StageEntities.Add(pipis);
                            Vector3 dest = myPos + aimDir * (14f * nrgMult);
                            new LightningBeam(myPos + new Vector3(0, 1f, 0), dest + new Vector3(0, 1f, 0), Rgb(95, 255, 130), 0.25f);
                            TeleportPack1Player(dest, myPos);
                        }
                    }
                    else
                    {
                        if (shVar == 0)
                        {
                            SoundManager.Play("coin_flip", 0.85f, 0.9f);
                            Run.ApplyDash(flatAim * (25f * nrgMult), 0.30f);
                            var pipis = new PipisBombEntity(myPos + new Vector3(0, 0.8f, 0), -flatAim * 7f + new Vector3(0, 6f, 0), BaseDamage * 1.95f, isNeo: false, bounces: 3);
                            Run.StageEntities.Add(pipis);
                        }
                        else
                        {
                            SoundManager.Play("grapple", 0.85f, 1.25f);
                            new LightningBeam(myPos + new Vector3(0, 1.1f, 0), myPos + aimDir * 14f, Rgb(95, 245, 135), 0.22f);
                            Run.ApplyDash(aimDir * (32f * nrgMult), 0.24f);
                        }
                    }
                    return true;
                }
                case "binah":
                {
                    if (shVar == 0)
                    {
                        SoundManager.Play("magic_cast", 0.85f, 1.3f);
                        Vector3 dest = myPos + aimDir * (13.5f * nrgMult);
                        new LightningBeam(myPos + new Vector3(0, 1.1f, 0), dest + new Vector3(0, 1.1f, 0), Rgb(245, 200, 65), 0.28f);
                        TeleportPack1Player(dest, myPos);
                        new VisualFX(Run.PlayerPos + new Vector3(0, 1.0f, 0), Rgb(245, 200, 65), 4.0f, 0.26f);
                        for (int i = 0; i < CombatManager.Enemies.Count; i++)
                        {
                            var e = CombatManager.Enemies[i];
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 5.5f)
                            {
                                e.StunTimer = MathF.Max(e.StunTimer, 1.0f);
                                ApplyBinahFairy(e, 1.1f);
                            }
                        }
                    }
                    else
                    {
                        _binahTeaPoiseTimer = 1.2f;
                        Run.HealPlayer(Run.PlayerMaxHp * 0.12f + 12f);
                        M2Cd = MathF.Max(0f, M2Cd - 1.5f);
                        RCd = MathF.Max(0f, RCd - 1.5f);
                        SoundManager.Play("parry", 0.85f, 1.15f);
                        new VisualFX(myPos + new Vector3(0, 1.1f, 0), Rgb(250, 210, 75), 3.8f, 0.35f);
                        Run.ShowBanner("ЧАЕПИТИЕ АРБИТРА ◇", Rgb(250, 210, 75), 1.4f);
                    }
                    return true;
                }
                case "don_quixote":
                {
                    if (skinIdx == 0)
                    {
                        SoundManager.Play("shoot_heavy", 0.9f, 1.15f);
                        float spdMult = 1.0f + Math.Clamp((MoveSpeed - 8.0f) * 0.08f, 0f, 0.85f);
                        Run.ApplyDash(flatAim * (30f * nrgMult), 0.36f);
                        PerformMeleeAttack(7.2f, 4.4f, 2.45f * spdMult, Rgb(255, 215, 65), stunDur: 1.0f, knockback: 7.5f, aoe: 3.5f);
                        foreach (var e in CombatManager.Enemies.ToArray())
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(myPos, e.Position) <= 8.5f)
                                ApplyEnforcerWallSplat(e, flatAim, 6.5f);
                    }
                    else if (skinIdx == 1)
                    {
                        _donBurrowTimer = 1.0f;
                        SoundManager.Play("slam_land", 0.9f, 0.95f);
                        Run.ApplyDash(flatAim * (26f * nrgMult), 0.36f);
                        new VisualFX(myPos + flatAim * 4f + new Vector3(0, 0.6f, 0), Rgb(130, 240, 95), 5.2f, 0.32f);
                        PerformMeleeAttack(6.5f, 5.8f, 2.85f, Rgb(130, 240, 95), lifesteal: 10f, stunDur: 1.4f, aoe: 4.4f);
                    }
                    else
                    {
                        _donWaltzTimer = 0.65f;
                        _donHardblood = Math.Min(30, _donHardblood + 5);
                        SoundManager.Play("magic_cast", 0.9f, 1.1f);
                        Run.ApplyDash(flatAim * (29f * nrgMult), 0.34f);
                        new VisualFX(myPos + new Vector3(0, 1.0f, 0), Rgb(225, 30, 60), 4.2f, 0.28f);
                        PerformMeleeAttack(7.5f, 4.8f, 2.70f, Rgb(225, 30, 60), lifesteal: 9f, aoe: 3.8f);
                    }
                    return true;
                }
                case "priestess":
                {
                    if (shVar == 0)
                    {
                        SoundManager.Play("magic_cast", 0.85f, 1.35f);
                        new VisualFX(myPos + new Vector3(0, 1f, 0), Rgb(235, 45, 78), 3.6f, 0.25f);
                        TeleportPack1Player(myPos + aimDir * (14f * nrgMult), myPos);
                        new VisualFX(Run.PlayerPos + new Vector3(0, 1f, 0), Rgb(235, 45, 78), 3.8f, 0.25f);
                        for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
                        {
                            var e = CombatManager.Enemies[i];
                            if (IsValid(e) && !e.IsAlly && (Vector3.Distance(myPos, e.Position) <= 5.5f || Vector3.Distance(Run.PlayerPos, e.Position) <= 5.5f))
                                AddPriestessOriginiumMark(e, 1);
                        }
                    }
                    else
                    {
                        _priestessPhaseTimer = 2.6f;
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 42f);
                        SoundManager.Play("heal", 0.85f, 1.25f);
                        Run.ApplyDash(flatAim * (21f * nrgMult), 0.26f);
                        new VisualFX(myPos + new Vector3(0, 1f, 0), Rgb(235, 45, 78), 4.2f, 0.28f);
                    }
                    return true;
                }
                case "closure":
                {
                    EnsureClosureDrone(false);
                    if (shVar == 0)
                    {
                        SoundManager.Play("grapple", 0.85f, 1.15f);
                        SoundManager.Play("zap", 0.75f, 1.25f);
                        new LightningBeam(myPos + new Vector3(0, 1.1f, 0), myPos + aimDir * 15f, Rgb(75, 235, 255), 0.24f);
                        Run.ApplyDash(aimDir * (28f * nrgMult), 0.30f);
                        new VisualFX(myPos + new Vector3(0, 0.8f, 0), Rgb(75, 235, 255), 5.0f, 0.28f);
                        for (int i = 0; i < CombatManager.Enemies.Count; i++)
                        {
                            var e = CombatManager.Enemies[i];
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(myPos, e.Position) <= 7.5f)
                                e.StunTimer = MathF.Max(e.StunTimer, 1.6f);
                        }
                    }
                    else
                    {
                        SoundManager.Play("heal", 0.85f, 1.2f);
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 48f);
                        Run.HealPlayer(15f);
                        Run.ApplyDash(flatAim * (22f * nrgMult), 0.25f);
                        new VisualFX(myPos + new Vector3(0, 1.0f, 0), Rgb(85, 240, 255), 4.2f, 0.26f);
                    }
                    return true;
                }
                default:
                    return false;
            }
        }

        private bool TryUsePack1R(int rVar)
        {
            if (CharKey is not ("susie" or "spamton" or "binah" or "don_quixote" or "priestess" or "closure"))
                return false;

            float rCdr = MathF.Pow(0.80f, CombatManager.GetItemCount("alien_head")) * MathF.Pow(0.75f, CombatManager.GetItemCount("lysate_cell"));
            RCd = MathF.Max(0.5f, Cfg.Skills["r"].Cd * rCdr);
            TriggerLoLSpellblade();
            Run.Parts.TriggerSkillAnim("r", 0.65f);

            int skinIdx = ProgressData.Instance.GetSelectedSkin(CharKey);
            Vector3 aimDir = GetAimDirection();
            Vector3 spawnPos = Run.PlayerPos + new Vector3(0, 1.25f, 0) + aimDir * 0.65f;

            switch (CharKey)
            {
                case "susie":
                {
                    _susieUltTimer = 6.0f;
                    Run.Parts.SetUltForm(true);
                    if (rVar == 0)
                    {
                        SoundManager.Play("heal", 1.0f, 0.95f);
                        SoundManager.Play("ult_cast", 0.9f, 1.05f);
                        Run.HealPlayer(Run.PlayerMaxHp * 0.38f + 35f);
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 35f);
                        new Projectile(spawnPos, aimDir, 42f, BaseDamage * 3.8f, 1.05f, Rgb(125, 255, 150), aoe: 5.0f, pierce: true, maxLifetime: 2.5f, healAllies: 55f);
                        Run.ShowBanner("ULTIMATE HEAL?! ★ ЯРОСТЬ СЬЮЗИ!", Rgb(245, 85, 200), 2.2f);
                    }
                    else
                    {
                        _susieStormTimer = 4.5f;
                        _susieStormTick = 0f;
                        SoundManager.Play("ult_cast", 1.0f, 0.85f);
                        new LightningBeam(Run.PlayerPos + new Vector3(0, 26f, 0), Run.PlayerPos, Rgb(235, 65, 200), 0.45f);
                        PerformMeleeAttack(9.0f, 9.0f, 5.2f, Rgb(245, 70, 200), stunDur: 1.8f, aoe: 7.5f);
                        Run.ShowBanner("ШТОРМ ТЁМНОГО ФОНТАНА! ★", Rgb(245, 75, 205), 2.2f);
                    }
                    return true;
                }
                case "spamton":
                {
                    _spamtonFrenzyTimer = 6.5f;
                    _spamtonKromerMult = 1.55f;
                    Run.Parts.SetUltForm(true);
                    if (skinIdx == 2)
                    {
                        SoundManager.Play("railgun", 1.0f, 0.82f);
                        SoundManager.Play("ult_cast", 1.0f, 1.1f);
                        Vector3 endPt = spawnPos + aimDir * 46f;
                        new LightningBeam(spawnPos, endPt, Rgb(255, 95, 210), 0.50f);
                        new LightningBeam(spawnPos + new Vector3(0, 0.25f, 0), endPt, Rgb(255, 235, 75), 0.50f);
                        for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
                        {
                            var e = CombatManager.Enemies[i];
                            if (!IsValid(e) || e.IsAlly) continue;
                            Vector3 toE = e.Position - spawnPos;
                            float proj = Math.Clamp(Vector3.Dot(toE, aimDir), 0f, 46f);
                            if (Vector3.Distance(e.Position, spawnPos + aimDir * proj) <= 4.5f + e.HitRadius)
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight * 0.5f, 0), BaseDamage * 6.8f, Rgb(255, 225, 80), lifesteal: 8f, canProc: true);
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            float ang = i * (MathF.PI * 2f / 6f);
                            Vector3 mDir = Vector3.Normalize(aimDir + new Vector3(MathF.Cos(ang) * 0.35f, 0.25f, MathF.Sin(ang) * 0.35f));
                            new Projectile(spawnPos, mDir, 44f, BaseDamage * 1.65f, 0.42f, (i & 1) == 0 ? Rgb(255, 95, 210) : Rgb(255, 235, 75), aoe: 2.4f, homing: true, maxLifetime: 2.6f);
                        }
                        Run.ShowBanner("СИЛА [[БОЛЬШОЙ ШИШКИ]]!! ★", Rgb(255, 235, 75), 2.3f);
                    }
                    else
                    {
                        SoundManager.Play("ult_cast", 1.0f, 1.15f);
                        new Projectile(spawnPos, aimDir, 48f, BaseDamage * 5.2f, 1.15f, Rgb(255, 230, 75), aoe: 6.0f, pierce: true, maxLifetime: 2.6f);
                        for (int i = 0; i < 4; i++)
                        {
                            float ang = (i - 1.5f) * 0.28f;
                            Vector3 v = Vector3.Normalize(new Vector3(aimDir.X * MathF.Cos(ang) - aimDir.Z * MathF.Sin(ang), 0.35f, aimDir.X * MathF.Sin(ang) + aimDir.Z * MathF.Cos(ang))) * 20f;
                            Run.StageEntities.Add(new PipisBombEntity(spawnPos, v, BaseDamage * 2.1f, isNeo: false, bounces: 3));
                        }
                        Run.ShowBanner("ТЕПЕРЬ ТВОЙ ШАНС СТАТЬ [[БОЛЬШОЙ ШИШКОЙ]]!", Rgb(255, 225, 75), 2.2f);
                    }
                    return true;
                }
                case "binah":
                {
                    if (rVar == 0)
                    {
                        SoundManager.Play("dante_clock", 1.0f, 0.9f);
                        SoundManager.Play("ult_cast", 1.0f, 0.95f);
                        new VisualFX(Run.PlayerPos + new Vector3(0, 1.2f, 0), Rgb(245, 200, 65), 14.0f, 0.48f);
                        for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
                        {
                            var e = CombatManager.Enemies[i];
                            if (!IsValid(e) || e.IsAlly || Vector3.Distance(Run.PlayerPos, e.Position) > 24.0f) continue;
                            e.StunTimer = MathF.Max(e.StunTimer, 3.2f);
                            e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 7.0f);
                            ApplyBinahFairy(e, 1.8f);
                            Vector3 hitPt = e.Position + new Vector3(0, e.HitHeight * 0.55f, 0);
                            new LightningBeam(spawnPos, hitPt, Rgb(245, 200, 65), 0.42f);
                            CombatManager.ApplyPlayerHitEffects(e, hitPt, BaseDamage * 6.0f, Rgb(245, 200, 65), aoe: 2.5f, canProc: true);
                        }
                        Run.ShowBanner("ДЕГРАДИРОВАННЫЙ ЗАМОК — ШОКОВАЯ ВОЛНА! ◇", Rgb(245, 200, 65), 2.3f);
                    }
                    else
                    {
                        _binahUltPillarTimer = 6.0f;
                        _binahUltPillarTick = 0.05f;
                        Run.Parts.SetUltForm(true);
                        SoundManager.Play("ult_cast", 1.0f, 1.0f);
                        Run.ShowBanner("ШТОРМ СТОЛПОВ АРБИТРА! ◇", Rgb(245, 200, 65), 2.2f);
                    }
                    return true;
                }
                case "don_quixote":
                {
                    if (skinIdx == 0)
                    {
                        SoundManager.Play("ult_cast", 1.0f, 1.1f);
                        Run.HealPlayer(35f);
                        new Projectile(spawnPos, aimDir, 58f, BaseDamage * 6.2f, 1.25f, Rgb(235, 35, 65), aoe: 6.5f, lifesteal: 18f, pierce: true, maxLifetime: 2.5f);
                        PerformMeleeAttack(10.0f, 8.0f, 3.8f, Rgb(255, 215, 65), lifesteal: 12f, aoe: 6.0f);
                        Run.ShowBanner("LA SANGRE DE SANCHO! ★", Rgb(240, 45, 70), 2.2f);
                    }
                    else if (skinIdx == 1)
                    {
                        SoundManager.Play("ult_cast", 0.95f, 0.9f);
                        Vector3 lPos = Run.PlayerPos + Vector3.Normalize(new Vector3(aimDir.X, 0f, aimDir.Z) + new Vector3(0.001f, 0, 0)) * 5.0f;
                        lPos.Y = Physics.GetSurfaceHeight(lPos.X, lPos.Z, Run.PlayerPos.Y);
                        Run.StageEntities.Add(new MeatLanternLureEntity(lPos, BaseDamage * 2.35f));
                        Run.ShowBanner("ПРИМАНКА МЯСНОГО ФОНАРЯ!", Rgb(145, 245, 100), 2.2f);
                    }
                    else
                    {
                        _donHardblood = 30;
                        _donAscendTimer = 6.0f;
                        Run.Parts.SetUltForm(true);
                        Run.PlayerVelY = 11.5f;
                        SoundManager.Play("ult_cast", 1.0f, 0.95f);
                        SoundManager.Play("railgun", 0.95f, 0.9f);
                        new VisualFX(Run.PlayerPos + new Vector3(0, 1.5f, 0), Rgb(230, 25, 55), 8.5f, 0.42f);
                        new Projectile(spawnPos + new Vector3(0, 1.2f, 0), aimDir, 62f, BaseDamage * 7.2f, 1.35f, Rgb(235, 25, 58), aoe: 7.5f, lifesteal: 22f, pierce: true, maxLifetime: 2.6f);
                        Run.ShowBanner("ВОСХОДЯЩЕЕ ИСКУССТВО САНЧО: LA SANGRE! ★", Rgb(235, 35, 65), 2.4f);
                    }
                    return true;
                }
                case "priestess":
                {
                    _priestessUltTimer = 7.0f;
                    _priestessUltTick = 0.05f;
                    Shield = MathF.Min(Run.PlayerMaxHp, Shield + 45f);
                    Run.Parts.SetUltForm(true);
                    SoundManager.Play("ult_cast", 1.0f, 1.05f);
                    new VisualFX(Run.PlayerPos + new Vector3(0, 1.5f, 0), Rgb(235, 45, 78), 12.0f, 0.45f);
                    for (int i = CombatManager.Enemies.Count - 1; i >= 0; i--)
                    {
                        var e = CombatManager.Enemies[i];
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 24.0f)
                            AddPriestessOriginiumMark(e, 2);
                    }
                    Run.ShowBanner("EYESOFPRIESTESS ◇ АРХИВ ОРИДЖИНИУМА", Rgb(240, 55, 85), 2.4f);
                    return true;
                }
                case "closure":
                {
                    _closureOverdriveTimer = 8.0f;
                    Run.Parts.SetUltForm(true);
                    while (_closureDrones.Count < 3) EnsureClosureDrone(true);
                    foreach (var d in _closureDrones)
                    {
                        if (!IsValid(d)) continue;
                        d.Upgrade();
                        d.Overdrive = true;
                        new Projectile(d.Position, aimDir, 50f, BaseDamage * 2.2f, 0.45f, Rgb(255, 155, 55), aoe: 3.4f, homing: true, maxLifetime: 2.5f);
                    }
                    Shield = MathF.Min(Run.PlayerMaxHp, Shield + 60f);
                    SoundManager.Play("ult_cast", 1.0f, 1.15f);
                    Run.ShowBanner("ОВЕРДРАЙВ ИНЖЕНЕРНОГО ОТСЕКА! ★", Rgb(85, 235, 255), 2.3f);
                    return true;
                }
                default:
                    return false;
            }
        }

        private void UpdatePack1Skills(float dt, bool isMoving)
        {
            if (CharKey is not ("susie" or "spamton" or "binah" or "don_quixote" or "priestess" or "closure"))
                return;

            int skinIdx = ProgressData.Instance.GetSelectedSkin(CharKey);

            if (_susieUltTimer > 0f && (_susieUltTimer -= dt) <= 0f) Run.Parts.SetUltForm(false);
            if (_susieStormTimer > 0f)
            {
                _susieStormTimer -= dt;
                _susieStormTick -= dt;
                if (_susieStormTick <= 0f)
                {
                    _susieStormTick = 0.55f;
                    var t = FindAimedPack1Target(Run.PlayerPos, GetAimDirection(), 26f, -1f);
                    Vector3 dir = t != null ? Vector3.Normalize(t.Position - Run.PlayerPos) : GetAimDirection();
                    new Projectile(Run.PlayerPos + new Vector3(0, 1.5f, 0), dir, 46f, BaseDamage * 2.15f, 0.78f, Rgb(245, 70, 195), aoe: 3.6f, pierce: true, maxLifetime: 1.8f);
                }
            }

            if (CharKey == "spamton")
            {
                _spamtonKromerTimer -= dt;
                if (_spamtonKromerTimer <= 0f)
                {
                    _spamtonKromerTimer = 1.8f;
                    _spamtonKromerMult = 0.90f + (float)_rng.NextDouble() * 0.65f;
                }
                if (_spamtonFrenzyTimer > 0f)
                {
                    if ((_spamtonFrenzyTimer -= dt) <= 0f) Run.Parts.SetUltForm(false);
                    _spamtonFrenzyTick -= dt;
                    if (_spamtonFrenzyTick <= 0f)
                    {
                        _spamtonFrenzyTick = 0.72f;
                        new Projectile(Run.PlayerPos + new Vector3(0, 1.6f, 0), GetAimDirection(), 48f, BaseDamage * 1.35f, 0.38f, Rgb(255, 110, 215), aoe: 2.0f, homing: true, maxLifetime: 2.0f);
                    }
                }
            }

            if (_binahTeaPoiseTimer > 0f) _binahTeaPoiseTimer -= dt;
            if (_binahUltPillarTimer > 0f)
            {
                if ((_binahUltPillarTimer -= dt) <= 0f) Run.Parts.SetUltForm(false);
                _binahUltPillarTick -= dt;
                if (_binahUltPillarTick <= 0f)
                {
                    _binahUltPillarTick = 0.95f;
                    var t = FindAimedPack1Target(Run.PlayerPos, GetAimDirection(), 28f, -1f);
                    Vector3 dir = t != null ? Vector3.Normalize(t.Position - Run.PlayerPos) : GetAimDirection();
                    Vector3 pPos = Run.PlayerPos + new Vector3(dir.X, 0f, dir.Z) * 2.0f;
                    pPos.Y = Physics.GetSurfaceHeight(pPos.X, pPos.Z, Run.PlayerPos.Y);
                    Run.StageEntities.Add(new BinahPillarEntity(pPos, dir, BaseDamage * 3.1f));
                }
            }

            if (_donBannerTimer > 0f) _donBannerTimer -= dt;
            if (_donBurrowTimer > 0f) _donBurrowTimer -= dt;
            if (_donBloodShieldTimer > 0f) _donBloodShieldTimer -= dt;
            if (_donWaltzTimer > 0f) _donWaltzTimer -= dt;
            if (_donAscendTimer > 0f && (_donAscendTimer -= dt) <= 0f) Run.Parts.SetUltForm(false);
            if (CharKey == "don_quixote" && skinIdx == 1)
            {
                _donLanternRegenTick -= dt;
                if (_donLanternRegenTick <= 0f)
                {
                    _donLanternRegenTick = 0.5f;
                    int bleedNear = 0;
                    for (int i = 0; i < CombatManager.Enemies.Count; i++)
                    {
                        var e = CombatManager.Enemies[i];
                        if (IsValid(e) && !e.IsAlly && e.BleedTimer > 0f && Vector3.Distance(Run.PlayerPos, e.Position) <= 14f)
                            bleedNear++;
                    }
                    if (bleedNear > 0) Run.HealPlayer(1.5f + bleedNear * 1.25f);
                }
            }

            if (_priestessPhaseTimer > 0f) _priestessPhaseTimer -= dt;
            if (_priestessUltTimer > 0f)
            {
                if ((_priestessUltTimer -= dt) <= 0f) Run.Parts.SetUltForm(false);
                _priestessUltTick -= dt;
                if (_priestessUltTick <= 0f)
                {
                    _priestessUltTick = 0.55f;
                    int hits = 0;
                    for (int i = CombatManager.Enemies.Count - 1; i >= 0 && hits < 3; i--)
                    {
                        var e = CombatManager.Enemies[i];
                        if (!IsValid(e) || e.IsAlly || Vector3.Distance(Run.PlayerPos, e.Position) > 24.0f) continue;
                        hits++;
                        Vector3 hitPt = e.Position + new Vector3(0, e.HitHeight * 0.5f, 0);
                        new LightningBeam(hitPt + new Vector3(0, 18f, 0), hitPt, Rgb(235, 45, 78), 0.25f);
                        CombatManager.ApplyPlayerHitEffects(e, hitPt, BaseDamage * 1.65f, Rgb(235, 45, 78), aoe: 2.2f, canProc: true);
                        AddPriestessOriginiumMark(e, 1);
                    }
                }
            }

            if (CharKey == "closure")
            {
                EnsureClosureDrone(false);
                if (_closureOverdriveTimer > 0f && (_closureOverdriveTimer -= dt) <= 0f)
                {
                    Run.Parts.SetUltForm(false);
                    foreach (var d in _closureDrones) if (IsValid(d)) d.Overdrive = false;
                }
                _closureShieldTick -= dt;
                if (_closureShieldTick <= 0f)
                {
                    _closureShieldTick = 1.0f;
                    Shield = MathF.Min(Run.PlayerMaxHp, Shield + (_closureOverdriveTimer > 0f ? 12f : 4f));
                    if (_closureOverdriveTimer > 0f) Run.HealPlayer(8f);
                }
            }
        }

        private bool TryBlockPack1Hit(float amount, CombatTarget? attacker)
        {
            if (CharKey == "binah" && _binahTeaPoiseTimer > 0f)
            {
                SoundManager.Play("parry", 0.85f, 1.2f);
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.1f, 0), Rgb(245, 200, 65), 3.2f, 0.22f);
                if (IsValid(attacker) && !attacker!.IsAlly)
                {
                    attacker.StunTimer = MathF.Max(attacker.StunTimer, 1.5f);
                    ApplyBinahFairy(attacker, 1.4f);
                }
                return true;
            }
            if (CharKey == "don_quixote")
            {
                int skin = ProgressData.Instance.GetSelectedSkin("don_quixote");
                if (skin == 1 && _donBurrowTimer > 0f) return true;
                if (skin == 2 && _donWaltzTimer > 0f)
                {
                    _donHardblood = Math.Min(30, _donHardblood + 2);
                    return true;
                }
            }
            if (CharKey == "priestess" && _priestessPhaseTimer > 0f && _rng.NextDouble() < 0.35)
                return true;
            return false;
        }

        private float ModifyPack1IncomingDamage(float dmg, CombatTarget? attacker)
        {
            OnSpamtonNeoHitStringSnap(ref dmg);
            int skin = ProgressData.Instance.GetSelectedSkin(CharKey);
            return CharKey switch
            {
                "susie" => dmg * (Run.PlayerHp < Run.PlayerMaxHp * 0.5f ? 0.85f : 1.0f) * (_susieUltTimer > 0f ? 0.88f : 1.0f),
                "spamton" => skin == 2 ? dmg * 0.78f : dmg,
                "don_quixote" => skin switch
                {
                    0 => _donBannerTimer > 0f ? dmg * 0.82f : dmg,
                    2 => dmg * (1.0f - (_donHardblood / 30f) * 0.20f) * (_donBloodShieldTimer > 0f ? 0.75f : 1.0f),
                    _ => dmg
                },
                "priestess" => _priestessPhaseTimer > 0f ? dmg * 0.70f : dmg,
                "closure" => _closureOverdriveTimer > 0f ? dmg * 0.80f : dmg,
                _ => dmg
            };
        }
    }
}
