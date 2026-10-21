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
    public class CombatTarget : Entity
    {
        private static int _nextId = 1;
        public readonly int NetId;
        public readonly string EnemyType;
        public readonly bool IsBoss;
        public bool IsAlly = false;
        public float Coeff;

        public float MaxHp;
        public float Hp;
        public float Damage;
        public float Speed;
        public int GoldReward;
        public float HitHeight;
        public float HitRadius;

        public float BleedTimer = 0f;
        public float BleedTick = 0f;
        public float BleedDps = 0f;
        public float SlowTimer = 0f;
        public float StunTimer = 0f;
        public float ArmorBreakTimer = 0f;
        public float AttackTimer = 1.2f;

        public readonly EnemyParts Parts;
        protected readonly Entity LabelEnt;
        private readonly float _seedOffset;

        public CombatTarget(Vector3 pos, string enemyType, float coeff = 1.0f) : base(position: pos)
        {
            NetId = _nextId++;
            EnemyType = enemyType;
            IsBoss = Array.IndexOf(GameData.BossTypes, enemyType) >= 0 || enemyType == "mithrix";
            Coeff = coeff;
            _seedOffset = (NetId * 1.37f) % 10.0f;

            Parts = EnemyAndBossBuilder.BuildEnemyMesh(this, enemyType);

            (float hpB, float dmgB, float spdB, int goldB, float hh, float hr) = enemyType switch
            {
                "wisp" => (26f, 11.5f, 4.6f, 16, 2.3f, 1.25f),
                "beetle" => (28f, 7.5f, 7.5f, 11, 0.7f, 1.10f),
                "jellyfish" => (24f, 14.0f, 8.2f, 15, 2.2f, 1.25f),
                "golem" => (95f, 18.0f, 3.8f, 28, 1.8f, 1.55f),
                "imp" => (52f, 13.5f, 7.2f, 24, 1.3f, 1.20f),
                "greater_wisp" => (82f, 16.5f, 4.0f, 32, 2.7f, 1.55f),
                "lunar_chimera" => (110f, 18.0f, 6.0f, 38, 2.5f, 1.45f),
                "boss" => (450f, 22.0f, 3.5f, 130, 2.8f, 2.70f),
                "vagrant" => (480f, 20.0f, 3.8f, 140, 3.8f, 2.90f),
                "overlord" => (520f, 24.0f, 5.2f, 150, 2.9f, 2.75f),
                "mithrix" => (1950f, 26.0f, 8.2f, 600, 2.8f, 2.55f),
                _ => (36f, 9.0f, 6.3f, 13, 1.1f, 1.15f)
            };

            MaxHp = hpB * coeff;
            Hp = MaxHp;
            Damage = dmgB * coeff;
            Speed = spdB;
            GoldReward = (int)(goldB * coeff);
            HitHeight = hh;
            HitRadius = hr;

            float barY = IsBoss ? 5.8f : (enemyType is "golem" or "greater_wisp" or "lunar_chimera" ? 3.5f : 2.5f);
            LabelEnt = new Entity(parent: this, position: new(0, barY, 0), color: Color.Red);
            UpdateHpBillboard();
        }

        public void UpdateHpBillboard()
        {
            if (IsDestroyed) return;
            int bars = Math.Clamp((int)MathF.Ceiling((Hp / MathF.Max(1f, MaxHp)) * 10f), 0, 10);
            string barStr = new string('█', bars) + new string('░', 10 - bars);
            string prefix = IsAlly ? "[СОЮЗНИК МЕДИКА ✓]\n" : "";
            LabelEnt.Color = IsAlly ? Color.Lime : (EnemyType == "mithrix" ? Color.SkyBlue : Color.Red);
            LabelEnt.BillboardText = $"{prefix}{EnemyType.ToUpper()} [{(int)MathF.Max(0, Hp)}/{(int)MaxHp}]\n{barStr}";
        }

        public void ConvertToAlly()
        {
            if (IsAlly) return;
            IsAlly = true;
            Hp = MaxHp;
            Damage *= 1.85f;
            Speed *= 1.25f;
            BleedTimer = 0f;
            StunTimer = 0f;
            new Entity(parent: this, model: "diamond", scale: new(HitRadius * 2.2f, 0.12f, HitRadius * 2.2f), position: new(0, 0.1f, 0), color: Color.Lime, unlit: true);
            UpdateHpBillboard();
        }

        public void HealAlly(float amount, float uberDur = 0f)
        {
            if (!IsValid(this)) return;
            if (amount > 0f)
            {
                Hp = MathF.Min(MaxHp, Hp + amount);
                UpdateHpBillboard();
            }
            if (uberDur > 0f) Damage *= 1.15f;
        }

        public virtual void TakeDamage(float amount)
        {
            if (!IsValid(this)) return;
            Hp -= amount;
            UpdateHpBillboard();
            if (Hp <= 0f)
            {
                Die();
            }
        }

        public virtual void Die()
        {
            if (IsDead || IsDestroyed) return;
            IsDead = true;
            CombatManager.OnEnemyKilled(this);
            Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || !IsValid(this)) return;

            ArmorBreakTimer = MathF.Max(0f, ArmorBreakTimer - dt);
            if (BleedTimer > 0f)
            {
                BleedTimer -= dt;
                BleedTick -= dt;
                if (BleedTick <= 0f)
                {
                    BleedTick = 0.35f;
                    new VisualFX(Position + new Vector3(0, HitHeight, 0), Rgb(220, 25, 45), 1.1f, 0.18f);
                    TakeDamage(BleedDps * 0.35f);
                    if (!IsValid(this)) return;
                }
            }

            if (StunTimer > 0f)
            {
                StunTimer -= dt;
                return;
            }

            float spdMult = SlowTimer > 0f ? 0.62f : 1.0f;
            if (SlowTimer > 0f) SlowTimer -= dt;
            float effSpeed = Speed * spdMult;
            float t = (float)Raylib.GetTime() + _seedOffset;

            // Базовая анимация и движение к цели
            if (Parts.Ring1 != null) Parts.Ring1.RotationY += 110f * dt;
            if (Parts.Ring2 != null) Parts.Ring2.RotationX += 130f * dt;
            if (Parts.Crown != null) Parts.Crown.RotationY += 85f * dt;
            if (Parts.Tentacles != null) Parts.Tentacles.RotationY += 75f * dt;

            Vector3 targetPos = g.PlayerPos;
            CombatTarget? enemyTarget = null;
            if (IsAlly)
            {
                float bestD = 32f;
                foreach (var other in CombatManager.Enemies)
                {
                    if (other != this && IsValid(other) && !other.IsAlly)
                    {
                        float d = Vector3.Distance(Position, other.Position);
                        if (d < bestD) { bestD = d; enemyTarget = other; }
                    }
                }
                if (enemyTarget != null) targetPos = enemyTarget.Position;
            }

            float dist2D = Physics.Distance2D(Position, targetPos);
            LookAt2D(targetPos);
            float floorY = Physics.GetSurfaceHeight(X, Z, Y);

            float stopDist = IsAlly && enemyTarget == null ? 6.5f : 2.2f;
            if (dist2D > stopDist)
            {
                Position += Forward * effSpeed * dt;
                if (EnemyType is not ("wisp" or "jellyfish" or "greater_wisp" or "vagrant"))
                {
                    Vector3 res = Physics.ResolveHorizontalCollision(Position, 0.75f, floorY);
                    Position = new Vector3(res.X, floorY, res.Z);
                }
                if (Parts.LeftLeg != null && Parts.RightLeg != null)
                {
                    float swing = MathF.Sin(t * 10f) * 32f;
                    Parts.LeftLeg.RotationX = swing;
                    Parts.RightLeg.RotationX = -swing;
                }
            }
            else if (!IsAlly && dist2D <= 2.5f && MathF.Abs(g.PlayerPos.Y - Y) < 3.0f)
            {
                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    AttackTimer = 1.25f;
                    CombatManager.DamagePlayer(Damage);
                }
            }
            else if (IsAlly && enemyTarget != null && dist2D <= 3.0f)
            {
                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    AttackTimer = 1.0f;
                    enemyTarget.TakeDamage(Damage);
                    new VisualFX(enemyTarget.Position + new Vector3(0, enemyTarget.HitHeight, 0), Color.Lime, 1.8f, 0.2f);
                }
            }
        }
    }

    public class MeleeSlashFX : Entity
    {
        public MeleeSlashFX(Vector3 pos, Vector3 forwardDir, float rotY, Color slashColor, float arcWidth = 3.6f, float reach = 2.8f, float tiltZ = 0f, bool scissorCross = false)
            : base(position: pos + new Vector3(0, 1.35f, 0) + forwardDir * (reach * 0.48f), rotation: new(0, rotY, tiltZ))
        {
            var blade1 = new Entity(parent: this, model: "diamond", scale: new(arcWidth, 0.18f, reach * 0.72f), color: slashColor, unlit: true);
            var core1 = new Entity(parent: this, model: "diamond", scale: new(arcWidth * 0.75f, 0.26f, reach * 0.45f), color: Color.White, unlit: true);
            blade1.AnimateScale(new Vector3(arcWidth * 1.35f, 0.18f, 0.05f), 0.18f);
            core1.AnimateScale(new Vector3(arcWidth * 0.75f, 0.26f, 0.02f), 0.16f);

            if (scissorCross)
            {
                var blade2 = new Entity(parent: this, model: "diamond", scale: new(arcWidth, 0.18f, reach * 0.72f),
                    rotation: new(0, 0, tiltZ != 0 ? -tiltZ * 2f : 36f), color: Rgb(195, 245, 255), unlit: true);
                blade2.AnimateScale(new Vector3(arcWidth * 1.35f, 0.18f, 0.05f), 0.18f);
            }
            Destroy(this, 0.19f);
        }
    }

    public class OrbitalStrikeFX : Entity
    {
        public OrbitalStrikeFX(Vector3 targetPos, float dmg)
            : base(model: "cube", position: targetPos + new Vector3(0, 18f, 0), scale: new(1.4f, 36.0f, 1.4f), color: Color.SkyBlue, unlit: true)
        {
            new VisualFX(targetPos + new Vector3(0, 0.8f, 0), Color.SkyBlue, 6.5f, 0.4f);
            AnimateScale(new Vector3(0.05f, 36.0f, 0.05f), 0.45f);
            Destroy(this, 0.46f);

            if (dmg > 0f)
            {
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Physics.Distance2D(e.Position, targetPos) <= 4.5f)
                        e.TakeDamage(dmg);
                }
            }
        }
    }

    public class TearGasZone : Entity
    {
        private readonly float _tickDmg;
        private float _duration = 6.0f;
        private float _tickTimer = 0f;
        private readonly float _radius = 8.5f;
        private readonly Entity _cloudsHub;

        public TearGasZone(Vector3 centerPos, float tickDmg)
            : base(position: centerPos + new Vector3(0, 0.1f, 0))
        {
            _tickDmg = tickDmg;
            new Entity(parent: this, model: "sphere", scale: new(_radius * 2f, 0.14f, _radius * 2f), color: Rgb(130, 245, 95, 75), unlit: true);
            _cloudsHub = new Entity(parent: this);
            for (int i = 0; i < 7; i++)
            {
                float ang = (i / 7.0f) * MathF.PI * 2f;
                float d = _radius * 0.55f;
                new Entity(parent: _cloudsHub, model: "sphere", position: new(MathF.Cos(ang) * d, 1.1f, MathF.Sin(ang) * d),
                    scale: new(4.2f, 2.2f, 4.2f), color: Rgb(145, 255, 110, 55), unlit: true);
            }
        }

        public override void OnUpdate(float dt)
        {
            _duration -= dt;
            _cloudsHub.RotationY += 35f * dt;
            if (_duration <= 0f) { Destroy(this); return; }

            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = 0.45f;
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Physics.Distance2D(e.Position, Position) <= _radius && MathF.Abs(e.Y - Y) <= 5.0f)
                    {
                        e.SlowTimer = MathF.Max(e.SlowTimer, 1.5f);
                        e.TakeDamage(_tickDmg);
                        new VisualFX(e.Position + new Vector3(0, 1.1f, 0), Rgb(140, 255, 100), 1.5f, 0.2f);
                    }
                }
            }
        }
    }

    public class IFGZone : Entity
    {
        private readonly float _tickDmg;
        private float _duration = 6.5f;
        private float _tickTimer = 0f;
        private readonly float _radius = 8.5f;
        private readonly Entity _pylon;

        public IFGZone(Vector3 centerPos, float tickDmg)
            : base(position: centerPos + new Vector3(0, 0.1f, 0))
        {
            _tickDmg = tickDmg;
            new Entity(parent: this, model: "sphere", scale: new(_radius * 2f, 0.15f, _radius * 2f), color: Rgb(60, 195, 255, 80), unlit: true);
            _pylon = new Entity(parent: this, model: "diamond", scale: new(0.8f, 2.6f, 0.8f), position: new(0, 1.3f, 0), color: Color.SkyBlue, unlit: true);
            new Entity(parent: this, model: "sphere", scaleUniform: 0.65f, position: new(0, 2.8f, 0), color: Color.Yellow, unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            _duration -= dt;
            _pylon.RotationY += 140f * dt;
            if (_duration <= 0f) { Destroy(this); return; }

            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = 0.40f;
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Physics.Distance2D(e.Position, Position) <= _radius && MathF.Abs(e.Y - Y) <= 5.5f)
                    {
                        e.SlowTimer = MathF.Max(e.SlowTimer, 2.0f);
                        e.TakeDamage(_tickDmg);
                        new LightningBeam(Position + new Vector3(0, 2.2f, 0), e.Position + new Vector3(0, 1.2f, 0), Color.SkyBlue, 0.14f);
                    }
                }
            }
        }
    }

    public class ArrowRainZone : Entity
    {
        private readonly float _tickDmg;
        private float _duration = 4.0f;
        private float _tickTimer = 0f;
        private readonly float _radius = 7.5f;
        private static readonly Random _rng = new();

        public ArrowRainZone(Vector3 centerPos, float tickDmg)
            : base(position: centerPos + new Vector3(0, 0.1f, 0))
        {
            _tickDmg = tickDmg;
            new Entity(parent: this, model: "sphere", scale: new(_radius * 2f, 0.1f, _radius * 2f), color: Rgb(255, 60, 90, 80), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            _duration -= dt;
            if (_duration <= 0f) { Destroy(this); return; }

            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = 0.35f;
                for (int i = 0; i < 3; i++)
                {
                    float rx = X + ((float)_rng.NextDouble() * 1.6f - 0.8f) * _radius;
                    float rz = Z + ((float)_rng.NextDouble() * 1.6f - 0.8f) * _radius;
                    new LightningBeam(new Vector3(rx, Y + 14f, rz), new Vector3(rx, Y, rz), Rgb(255, 220, 80), 0.14f);
                }
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Physics.Distance2D(e.Position, Position) <= _radius)
                        e.TakeDamage(_tickDmg);
                }
            }
        }
    }

    public class HallowedMistZone : Entity
    {
        private readonly float _tickDmg;
        private float _duration = 4.5f;
        private float _tickTimer = 0f;
        private readonly float _radius = 7.2f;
        private readonly Entity _pillarsHub;

        public HallowedMistZone(Vector3 centerPos, float tickDmg)
            : base(position: centerPos + new Vector3(0, 0.08f, 0))
        {
            _tickDmg = tickDmg;
            new Entity(parent: this, model: "sphere", scale: new(_radius * 2f, 0.08f, _radius * 2f), color: Rgb(75, 210, 255, 70), unlit: true);
            _pillarsHub = new Entity(parent: this);
            for (int i = 0; i < 8; i++)
            {
                float ang = (i / 8.0f) * MathF.PI * 2f;
                new Entity(parent: _pillarsHub, model: "diamond", position: new(MathF.Cos(ang) * _radius, 1.2f, MathF.Sin(ang) * _radius),
                    scale: new(0.35f, 2.2f, 0.35f), color: Rgb(145, 235, 255, 160), unlit: true);
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            _duration -= dt;
            _pillarsHub.RotationY += 55f * dt;
            if (_duration <= 0f) { Destroy(this); return; }

            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = 0.4f;
                if (g != null && Physics.Distance2D(g.PlayerPos, Position) <= _radius)
                    g.HealPlayer(4.5f);

                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Physics.Distance2D(e.Position, Position) <= _radius)
                    {
                        e.TakeDamage(_tickDmg);
                        new VisualFX(e.Position + new Vector3(0, 1f, 0), Rgb(130, 230, 255), 1.4f, 0.2f);
                    }
                }
            }
        }
    }

    public class DummyClone : Entity
    {
        public readonly int SlotIdx;
        public float MaxHp;
        public float Hp;
        public float HitHeight = 1.2f;
        public float HitRadius = 1.1f;
        private float _shootCd = 0.25f;
        private readonly SurvivorParts _parts;
        private readonly Entity _linkRing;
        private readonly Entity _labelEnt;

        public DummyClone(Vector3 ownerPos, float ownerMaxHp, int slotIdx)
            : base(position: ownerPos + new Vector3(MathF.Cos(slotIdx * 1.57f) * 3.0f, 0, MathF.Sin(slotIdx * 1.57f) * 3.0f))
        {
            SlotIdx = slotIdx;
            MaxHp = MathF.Max(60f, ownerMaxHp * 0.85f);
            Hp = MaxHp;
            _parts = SurvivorDatabase.BuildSurvivorMesh(this, "m4a1");
            _linkRing = new Entity(parent: this, model: "diamond", scale: new(1.35f, 0.08f, 1.35f), position: new(0, 0.06f, 0), color: Rgb(95, 255, 135, 180), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.65f, 0), color: Rgb(95, 255, 135))
            {
                BillboardText = $"DUMMY LINK #{slotIdx + 1}"
            };
        }

        public void CommandBurstShot()
        {
            if (IsDead || IsDestroyed) return;
            FireAtNearest(0.75f);
        }

        public void FireAtNearest(float dmgMult = 0.65f)
        {
            var c = CombatManager.Controller;
            if (c == null) return;
            CombatTarget? nearest = null;
            float bestD = 38f;
            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly)
                {
                    float d = Vector3.Distance(Position, e.Position);
                    if (d < bestD) { bestD = d; nearest = e; }
                }
            }
            if (nearest != null)
            {
                LookAt2D(nearest.Position);
                Vector3 sp = Position + new Vector3(0, 1.5f, 0) + Forward * 0.85f;
                Vector3 aim = Vector3.Normalize((nearest.Position + new Vector3(0, nearest.HitHeight, 0)) - sp);
                new Projectile(sp, aim, 68f, c.BaseDamage * dmgMult, 0.20f, Rgb(95, 255, 135), isPlayer: true, canProc: true);
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            var c = CombatManager.Controller;
            if (g == null || c == null || IsDead || IsDestroyed) return;

            _linkRing.RotationY += 90f * dt;
            MaxHp = MathF.Max(60f, g.PlayerMaxHp * 0.85f);
            Hp = MathF.Min(MaxHp, Hp + 2.0f * dt);
            _labelEnt.BillboardText = $"DUMMY LINK #{SlotIdx + 1} [{(int)Hp}/{(int)MaxHp}]";

            float formAngle = (float)Raylib.GetTime() * 0.6f + SlotIdx * (MathF.PI * 0.5f);
            Vector3 desiredPos = g.PlayerPos + new Vector3(MathF.Cos(formAngle) * 3.8f, 0, MathF.Sin(formAngle) * 3.8f);

            if (Vector3.Distance(Position, g.PlayerPos) > 28f || MathF.Abs(Y - g.PlayerPos.Y) > 14f)
            {
                Position = desiredPos + new Vector3(0, 0.5f, 0);
                new VisualFX(Position + new Vector3(0, 1f, 0), Rgb(95, 255, 135), 2.0f, 0.2f);
                return;
            }

            Vector3 toDesired = new(desiredPos.X - X, 0, desiredPos.Z - Z);
            if (toDesired.Length() > 0.65f)
            {
                Position += Vector3.Normalize(toDesired) * (c.MoveSpeed * 0.98f) * dt;
                LookAt2D(desiredPos);
                float swing = MathF.Sin((float)Raylib.GetTime() * 12f + SlotIdx) * 34f;
                _parts.LeftLeg.RotationX = swing;
                _parts.RightLeg.RotationX = -swing;
            }
            else
            {
                _parts.LeftLeg.RotationX = Lerp(_parts.LeftLeg.RotationX, 0f, dt * 10f);
                _parts.RightLeg.RotationX = Lerp(_parts.RightLeg.RotationX, 0f, dt * 10f);
            }

            Y = Physics.GetSurfaceHeight(X, Z, Y);

            _shootCd -= dt;
            if (_shootCd <= 0f)
            {
                _shootCd = MathF.Max(0.14f, 1.35f / MathF.Max(1f, c.FireRate));
                FireAtNearest(0.60f);
            }
        }
    }

    public class MindustryTurret : Entity
    {
        public readonly int SlotIdx;
        public float MaxHp;
        public float Hp;
        public float HitHeight = 1.1f;
        public float HitRadius = 1.2f;
        private float _shootCd = 0.2f;
        private float _mendTimer = 1.8f;
        private readonly Entity _head;
        private readonly Entity _mendRing;
        private readonly Entity _labelEnt;

        public MindustryTurret(Vector3 pos, float ownerMaxHp, int slotIdx) : base(position: pos)
        {
            SlotIdx = slotIdx;
            MaxHp = MathF.Max(90f, ownerMaxHp * 1.1f);
            Hp = MaxHp;

            new Entity(parent: this, model: "cube", scale: new(2.1f, 0.45f, 2.1f), position: new(0, 0.22f, 0), color: Rgb(55, 62, 75));
            new Entity(parent: this, model: "diamond", scale: new(2.2f, 0.15f, 2.2f), position: new(0, 0.45f, 0), color: Rgb(245, 185, 45), unlit: true);

            _head = new Entity(parent: this, position: new(0, 0.85f, 0));
            new Entity(parent: _head, model: "cube", scale: new(1.15f, 0.65f, 1.25f), color: Rgb(235, 175, 45));
            new Entity(parent: _head, model: "cube", scale: new(0.24f, 0.24f, 1.35f), position: new(-0.28f, 0.05f, 0.55f), color: Rgb(42, 48, 58));
            new Entity(parent: _head, model: "cube", scale: new(0.24f, 0.24f, 1.35f), position: new(0.28f, 0.05f, 0.55f), color: Rgb(42, 48, 58));
            new Entity(parent: _head, model: "diamond", scale: new(0.55f, 0.35f, 0.55f), position: new(0, 0.42f, 0), color: Color.Lime, unlit: true);

            _mendRing = new Entity(parent: this, model: "diamond", scale: new(1.8f, 0.08f, 1.8f), position: new(0, 0.12f, 0), color: Rgb(95, 255, 135, 160), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.4f, 0), color: Rgb(255, 215, 65))
            {
                BillboardText = $"MINDUSTRY TURRET #{slotIdx + 1}"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            var c = CombatManager.Controller;
            if (g == null || c == null || IsDead || IsDestroyed) return;

            _mendRing.RotationY += 85f * dt;
            _mendTimer -= dt;
            if (_mendTimer <= 0f)
            {
                _mendTimer = 2.0f;
                new VisualFX(Position + new Vector3(0, 0.8f, 0), Color.Lime, 5.5f, 0.3f);
                Hp = MathF.Min(MaxHp, Hp + MaxHp * 0.08f);
                if (Vector3.Distance(Position, g.PlayerPos) <= 14.0f)
                    g.HealPlayer(g.PlayerMaxHp * 0.06f + 6.0f);
            }

            _shootCd -= dt;
            if (_shootCd <= 0f)
            {
                _shootCd = MathF.Max(0.14f, 1.1f / MathF.Max(1f, c.FireRate));
                CombatTarget? nearest = null;
                float bestD = 36f;
                foreach (var e in CombatManager.Enemies)
                {
                    if (IsValid(e) && !e.IsAlly)
                    {
                        float d = Vector3.Distance(Position, e.Position);
                        if (d < bestD) { bestD = d; nearest = e; }
                    }
                }
                if (nearest != null)
                {
                    Vector3 targetP = nearest.Position + new Vector3(0, nearest.HitHeight, 0);
                    _head.LookAt2D(targetP);
                    Vector3 sp = Position + new Vector3(0, 1.0f, 0) + _head.Forward * 1.1f;
                    Vector3 aim = Vector3.Normalize(targetP - sp);
                    new Projectile(sp, aim, 72f, c.BaseDamage * 0.72f, 0.22f, Rgb(255, 210, 55), isPlayer: true, canProc: true);
                }
            }
        }
    }
}