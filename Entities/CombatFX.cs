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
    public class LunarShockwaveRing : Entity
    {
        private readonly float _damage;
        private float _curRadius = 2.0f;
        private readonly float _maxRadius;
        private readonly float _speed;
        private bool _hitPlayer = false;
        private readonly List<Entity> _segments = new();
        private const int SegCount = 24;

        public LunarShockwaveRing(Vector3 centerPos, float damage, float maxRadius = 54.0f, float speed = 19.5f)
            : base(position: new Vector3(centerPos.X, centerPos.Y + 0.12f, centerPos.Z))
        {
            _damage = damage;
            _maxRadius = maxRadius;
            _speed = speed;
            for (int i = 0; i < SegCount; i++)
            {
                float angDeg = i * (360f / SegCount);
                var seg = new Entity(parent: this, model: "cube", scale: new(0.6f, 0.28f, 0.45f),
                    rotation: new(0, angDeg, 0), color: Rgb(85, 235, 255), unlit: true);
                _segments.Add(seg);
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            _curRadius += _speed * dt;
            if (_curRadius >= _maxRadius)
            {
                Destroy(this);
                return;
            }

            float arcLen = (2.0f * MathF.PI * _curRadius) / SegCount;
            for (int i = 0; i < SegCount; i++)
            {
                float rad = i * (MathF.PI * 2f / SegCount);
                var seg = _segments[i];
                seg.Position = new Vector3(MathF.Sin(rad) * _curRadius, 0f, MathF.Cos(rad) * _curRadius);
                seg.Scale = new Vector3(arcLen * 0.92f, 0.28f, 0.42f);
            }

            if (!_hitPlayer)
            {
                Vector3 pPos = g.PlayerPos;
                // Тонкий контур кольца и низкий порог по высоте (0.55м), чтобы прыжок на Space легко перепрыгивал волну!
                if (MathF.Abs(Physics.Distance2D(pPos, Position) - _curRadius) <= 0.95f &&
                    pPos.Y >= Y - 0.5f && (pPos.Y - Y) <= 0.55f)
                {
                    _hitPlayer = true;
                    CombatManager.DamagePlayer(_damage);
                    new VisualFX(pPos + new Vector3(0, 0.8f, 0), Color.SkyBlue, 2.2f, 0.22f);
                }
            }
        }
    }

    public class LunarPizzaAttack : Entity
    {
        private readonly float _damage;
        private float _rotSpeed;
        private float _timer = 2.1f;
        private bool _detonated = false;
        private readonly List<Entity> _lines = new();
        private static readonly Random _rng = new();

        public LunarPizzaAttack(Vector3 centerPos, float damage, int numSlices = 8)
            : base(position: centerPos + new Vector3(0, 0.12f, 0))
        {
            _damage = damage;
            _rotSpeed = _rng.Next(2) == 0 ? -28.0f : 28.0f;
            for (int i = 0; i < numSlices; i++)
            {
                float ang = i * (180.0f / numSlices);
                var ln = new Entity(parent: this, model: "cube", scale: new(1.4f, 0.15f, 110.0f),
                    rotation: new(0, ang, 0), color: Rgb(70, 215, 255, 140), unlit: true);
                _lines.Add(ln);
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed || _detonated) return;

            RotationY += _rotSpeed * dt;
            _timer -= dt;
            if (_timer <= 0.40f)
            {
                _rotSpeed = 0f;
                foreach (var ln in _lines) ln.Color = Color.White;
            }

            if (_timer <= 0f)
                Detonate(g);
        }

        private void Detonate(IPlayerContext g)
        {
            _detonated = true;
            foreach (var ln in _lines)
            {
                ln.ScaleY = 22.0f;
                ln.Y = 11.0f;
                ln.Color = Rgb(80, 235, 255, 225);
                ln.AnimateScale(new Vector3(0.05f, 22.0f, 110.0f), 0.42f);
            }

            Vector3 pPos = g.PlayerPos;
            float dx = pPos.X - X, dz = pPos.Z - Z;
            if (MathF.Sqrt(dx * dx + dz * dz) <= 56.0f)
            {
                foreach (var ln in _lines)
                {
                    float worldAng = (RotationY + ln.RotationY) * (MathF.PI / 180f);
                    float lx = MathF.Sin(worldAng), lz = MathF.Cos(worldAng);
                    float perpDist = MathF.Abs(dx * lz - dz * lx);
                    if (perpDist <= 1.45f)
                    {
                        CombatManager.DamagePlayer(_damage);
                        new VisualFX(pPos + new Vector3(0, 1f, 0), Color.SkyBlue, 3.2f, 0.3f);
                        break;
                    }
                }
            }
            Destroy(this, 0.45f);
        }
    }

    public class CombatTarget : Entity
    {
        private static int _nextId = 1;
        private static readonly Random _rng = new();

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
        public float AttackTimer;
        public float SpecialTimer;

        // Поля 4 фаз Митрикса
        public int Phase = 1;
        public float PhaseTransitionTimer = 0f;
        public int SkyLeapState = 0;
        public float SkyLeapTimer = 0f;
        public Vector3 SkyLeapTarget = new(0, Physics.ColosseumY, 0);
        public Entity? TelegraphRing;
        public readonly List<string> StolenItems = new();
        public float ReclaimAccum = 0f;
        public float MithrixEffCoeff = 2.35f;

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
            AttackTimer = 0.8f + (float)_rng.NextDouble() * 0.8f;
            SpecialTimer = 3.5f + (float)_rng.NextDouble() * 2.0f;

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

            if (enemyType == "mithrix")
            {
                int itemCnt = 0;
                if (WorldContext.Game != null)
                {
                    foreach (var v in WorldContext.Game.Items.Values) itemCnt += v;
                }
                MithrixEffCoeff = MathF.Max(coeff, 2.35f) * (1.0f + 0.045f * itemCnt);
                MaxHp = hpB * MithrixEffCoeff;
                Damage = dmgB * MathF.Max(coeff, 1.85f);
            }
            else
            {
                MaxHp = hpB * coeff;
                Damage = dmgB * coeff;
            }

            Hp = MaxHp;
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
            string phaseTag = EnemyType == "mithrix" ? $" [P{Phase}/4]" : "";
            LabelEnt.Color = IsAlly ? Color.Lime : (EnemyType == "mithrix" ? Color.SkyBlue : Color.Red);
            LabelEnt.BillboardText = $"{prefix}{EnemyType.ToUpper()}{phaseTag} [{(int)MathF.Max(0, Hp)}/{(int)MaxHp}]\n{barStr}";
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

        public void AdvanceMithrixPhase()
        {
            var g = WorldContext.Game;
            if (g == null) return;

            Phase++;
            PhaseTransitionTimer = 2.0f;
            SkyLeapState = 0;
            if (TelegraphRing != null && !TelegraphRing.IsDestroyed)
            {
                Destroy(TelegraphRing);
                TelegraphRing = null;
            }

            if (Phase == 2)
            {
                MaxHp = 2250.0f * MithrixEffCoeff;
                Hp = MaxHp;
                Position = new Vector3(0, Physics.ColosseumY, 0);
                new VisualFX(new Vector3(0, Physics.ColosseumY + 2.5f, 0), Color.SkyBlue, 14.0f, 0.55f);
                new LunarShockwaveRing(new Vector3(0, Physics.ColosseumY, 0), Damage, 55.0f, 22.0f);
                foreach (float deg in new[] { 0f, 120f, 240f })
                {
                    float r = deg * (MathF.PI / 180f);
                    var chimera = new CombatTarget(new Vector3(MathF.Cos(r) * 28f, Physics.ColosseumY, MathF.Sin(r) * 28f), "lunar_chimera", Coeff);
                    CombatManager.Enemies.Add(chimera);
                    g.StageEntities.Add(chimera);
                }
                g.ShowBanner("МИТРИКС (ФАЗА 2/4): НЕБЕСНЫЙ ОБВАЛ И СТРАЖИ ЛУНЫ!", Color.SkyBlue, 3.8f);
            }
            else if (Phase == 3)
            {
                MaxHp = 2500.0f * MithrixEffCoeff;
                Hp = MaxHp;
                Position = new Vector3(0, Physics.ColosseumY, 0);
                if (Parts.PhaseRing != null) Parts.PhaseRing.Enabled = true;
                if (Parts.HammerAura != null) Parts.HammerAura.Color = Color.White;
                new VisualFX(new Vector3(0, Physics.ColosseumY + 2.5f, 0), Color.White, 16.0f, 0.6f);
                new LunarShockwaveRing(new Vector3(0, Physics.ColosseumY, 0), Damage * 1.2f, 55.0f, 25.0f);
                foreach (float deg in new[] { 60f, 240f })
                {
                    float r = deg * (MathF.PI / 180f);
                    var chimera = new CombatTarget(new Vector3(MathF.Cos(r) * 32f, Physics.ColosseumY, MathF.Sin(r) * 32f), "lunar_chimera", Coeff * 1.15f);
                    CombatManager.Enemies.Add(chimera);
                    g.StageEntities.Add(chimera);
                }
                g.ShowBanner("МИТРИКС (ФАЗА 3/4): ЛУННЫЕ РАЗРЕЗЫ КОЛИЗЕЯ!", Rgb(135, 240, 255), 3.8f);
            }
            else if (Phase == 4)
            {
                MaxHp = 1450.0f * MathF.Max(Coeff, 2.0f);
                Hp = MaxHp;
                Position = new Vector3(0, Physics.ColosseumY, 0);
                if (Parts.PhaseRing != null) { Parts.PhaseRing.Enabled = true; Parts.PhaseRing.Color = Color.Magenta; }
                if (Parts.HammerAura != null) Parts.HammerAura.Color = Color.Magenta;
                if (Parts.Core != null) Parts.Core.Color = Color.Magenta;

                foreach (var kv in new Dictionary<string, int>(g.Items))
                {
                    for (int i = 0; i < kv.Value; i++) StolenItems.Add(kv.Key);
                    g.Items[kv.Key] = 0;
                }
                g.ItemOrder.Clear();
                g.PlayerHp = MathF.Min(g.PlayerMaxHp, MathF.Max(g.PlayerHp, g.PlayerMaxHp * 0.85f));
                new LightningBeam(g.PlayerPos + new Vector3(0, 1.5f, 0), new Vector3(0, Physics.ColosseumY + 3.0f, 0), Color.Magenta, 0.65f);
                new VisualFX(new Vector3(0, Physics.ColosseumY + 3.0f, 0), Color.Magenta, 15.0f, 0.6f);
                g.ShowBanner("МИТРИКС ЗАБРАЛ ВАШИ ПРЕДМЕТЫ! АТАКУЙТЕ ЕГО, ЧТОБЫ ВЫБИТЬ ИХ ОБРАТНО!", Color.Magenta, 4.8f);
            }
            UpdateHpBillboard();
        }

        public virtual void TakeDamage(float amount)
        {
            if (!IsValid(this) || SkyLeapState == 1 || PhaseTransitionTimer > 0f) return;

            var g = WorldContext.Game;
            if (EnemyType == "mithrix")
                amount = MathF.Min(amount, MaxHp * 0.085f);

            Hp -= amount;

            if (EnemyType == "mithrix" && Phase == 4 && StolenItems.Count > 0 && g != null)
            {
                ReclaimAccum += amount;
                float threshold = MathF.Max(18.0f, MaxHp / MathF.Max(4.0f, StolenItems.Count + 2.0f));
                while (ReclaimAccum >= threshold && StolenItems.Count > 0)
                {
                    ReclaimAccum -= threshold;
                    string retKey = StolenItems[0];
                    StolenItems.RemoveAt(0);
                    if (!g.ItemOrder.Contains(retKey)) g.ItemOrder.Add(retKey);
                    g.Items[retKey] = (g.Items.TryGetValue(retKey, out int c) ? c : 0) + 1;
                    new LightningBeam(Position + new Vector3(0, 2.8f, 0), g.PlayerPos + new Vector3(0, 1.2f, 0), GameData.ItemPool[retKey].Color, 0.28f);
                }
            }

            UpdateHpBillboard();
            if (Hp <= 0f)
            {
                if (EnemyType == "mithrix" && Phase < 4)
                {
                    AdvanceMithrixPhase();
                    return;
                }
                Die();
            }
        }

        public virtual void Die()
        {
            if (IsDead || IsDestroyed) return;
            IsDead = true;
            if (TelegraphRing != null && !TelegraphRing.IsDestroyed)
                Destroy(TelegraphRing);
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

            Vector3 pPos = g.PlayerPos;
            CombatTarget? allyEnemyTarget = null;
            if (IsAlly)
            {
                float bestD = 34f;
                foreach (var other in CombatManager.Enemies)
                {
                    if (other != this && IsValid(other) && !other.IsAlly)
                    {
                        float d = Vector3.Distance(Position, other.Position);
                        if (d < bestD) { bestD = d; allyEnemyTarget = other; }
                    }
                }
                if (allyEnemyTarget != null) pPos = allyEnemyTarget.Position;
            }

            float dist = Physics.Distance2D(Position, pPos);
            float heightDiff = MathF.Abs(pPos.Y - Y);
            LookAt2D(pPos);
            float floorY = Physics.GetSurfaceHeight(X, Z, Y);
            bool projIsPlayer = IsAlly;

            if (IsAlly && allyEnemyTarget == null)
            {
                if (dist > 6.5f)
                {
                    Position += Forward * effSpeed * dt;
                    Y = floorY;
                }
                return;
            }

            if (EnemyType == "lemurian")
            {
                if (Parts.Tail != null) Parts.Tail.RotationY = MathF.Sin(t * 6f) * 22f;
                if (dist > 1.7f || heightDiff > 2.6f)
                {
                    Position += Forward * effSpeed * dt;
                    Vector3 res = Physics.ResolveHorizontalCollision(Position, 0.7f, floorY);
                    Position = new Vector3(res.X, floorY, res.Z);
                    float swing = MathF.Sin(t * 11f) * 36f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = swing;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -swing;
                }
                else
                {
                    AttackTimer -= dt;
                    if (AttackTimer <= 0f)
                    {
                        AttackTimer = 1.1f;
                        if (IsAlly && IsValid(allyEnemyTarget)) allyEnemyTarget!.TakeDamage(Damage);
                        else if (!IsAlly) CombatManager.DamagePlayer(Damage);
                        new VisualFX(pPos + new Vector3(0, 1f, 0), IsAlly ? Color.Lime : Color.Red, 1.6f, 0.2f);
                    }
                }
            }
            else if (EnemyType == "beetle")
            {
                if (dist > 1.65f || heightDiff > 2.4f)
                {
                    Position += Forward * effSpeed * dt;
                    Vector3 res = Physics.ResolveHorizontalCollision(Position, 0.65f, floorY);
                    Position = new Vector3(res.X, floorY, res.Z);
                    float scurry = MathF.Sin(t * 16f) * 30f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = scurry;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -scurry;
                }
                else
                {
                    AttackTimer -= dt;
                    if (AttackTimer <= 0f)
                    {
                        AttackTimer = 0.95f;
                        if (IsAlly && IsValid(allyEnemyTarget)) allyEnemyTarget!.TakeDamage(Damage);
                        else if (!IsAlly) CombatManager.DamagePlayer(Damage);
                        new VisualFX(pPos + new Vector3(0, 0.8f, 0), Color.Orange, 1.5f, 0.2f);
                    }
                }
            }
            else if (EnemyType == "jellyfish")
            {
                if (Parts.Core != null) Parts.Core.Y = 2.2f + MathF.Sin(t * 5.0f) * 0.3f;
                if (Parts.Tentacles != null) Parts.Tentacles.RotationY += 90f * dt;
                if (dist > 2.2f || heightDiff > 3.2f)
                {
                    Position += Forward * effSpeed * dt;
                    Y = Lerp(Y, MathF.Max(floorY, pPos.Y - 0.5f), dt * 2.5f);
                }
                else
                {
                    AttackTimer -= dt;
                    if (AttackTimer <= 0f)
                    {
                        AttackTimer = 1.8f;
                        new VisualFX(Position + new Vector3(0, 2.0f, 0), Color.SkyBlue, 5.5f, 0.35f);
                        if (dist <= 4.5f && heightDiff <= 3.5f)
                        {
                            if (IsAlly && IsValid(allyEnemyTarget)) allyEnemyTarget!.TakeDamage(Damage);
                            else if (!IsAlly) CombatManager.DamagePlayer(Damage);
                        }
                    }
                }
            }
            else if (EnemyType == "wisp")
            {
                if (Parts.Ring1 != null) Parts.Ring1.RotationX += 120f * dt;
                if (Parts.Ring2 != null) Parts.Ring2.RotationY += 145f * dt;
                if (Parts.Core != null) Parts.Core.Y = 2.3f + MathF.Sin(t * 4f) * 0.28f;
                if (dist > 16.0f) Position += Forward * effSpeed * dt;
                Y = Lerp(Y, MathF.Max(floorY, pPos.Y), dt * 2.0f);
                AttackTimer -= dt;
                Vector3 sp = Position + new Vector3(0, 2.3f, 0);
                Vector3 aimPoint = pPos + new Vector3(0, 1.2f, 0);
                if (AttackTimer <= 0f && dist < 36.0f && Physics.HasLineOfSight(sp, aimPoint))
                {
                    AttackTimer = 2.4f;
                    new Projectile(sp, aimPoint - sp, 25f, Damage, 0.35f, IsAlly ? Color.Lime : Color.Orange, isPlayer: projIsPlayer, canProc: false);
                }
            }
            else if (EnemyType == "greater_wisp")
            {
                if (Parts.Ring1 != null) Parts.Ring1.RotationY += 110f * dt;
                if (Parts.Core != null) Parts.Core.Y = 2.7f + MathF.Sin(t * 3.2f) * 0.3f;
                if (dist > 18.0f) Position += Forward * effSpeed * dt;
                Y = Lerp(Y, MathF.Max(floorY, pPos.Y), dt * 2.0f);
                AttackTimer -= dt;
                Vector3 aimPoint = pPos + new Vector3(0, 1.2f, 0);
                if (AttackTimer <= 0f && dist < 40.0f && Physics.HasLineOfSight(Position + new Vector3(0, 2.7f, 0), aimPoint))
                {
                    AttackTimer = 2.8f;
                    foreach (float sx in new[] { -1.1f, 1.1f })
                    {
                        Vector3 sp = Position + new Vector3(0, 2.7f, 0) + Right * sx;
                        new Projectile(sp, aimPoint - sp, 28f, Damage, 0.52f, Color.Lime, isPlayer: projIsPlayer, canProc: false);
                    }
                }
            }
            else if (EnemyType == "golem")
            {
                if (dist > 12.0f)
                {
                    Position += Forward * effSpeed * dt;
                    Vector3 res = Physics.ResolveHorizontalCollision(Position, 0.95f, floorY);
                    Position = new Vector3(res.X, floorY, res.Z);
                    float stomp = MathF.Sin(t * 5.5f) * 26f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stomp;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stomp;
                }
                AttackTimer -= dt;
                Vector3 eyePos = Position + new Vector3(0, 2.7f, 0);
                Vector3 aimPoint = pPos + new Vector3(0, 1.2f, 0);
                if (AttackTimer <= 0f && dist < 32.0f && Physics.HasLineOfSight(eyePos, aimPoint))
                {
                    AttackTimer = 3.0f;
                    Color col = IsAlly ? Color.Lime : Color.Red;
                    new LightningBeam(eyePos, aimPoint, col, 0.28f);
                    new Projectile(eyePos, aimPoint - eyePos, 46f, Damage, 0.48f, col, isPlayer: projIsPlayer, canProc: false);
                }
            }
            else if (EnemyType == "imp")
            {
                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && dist > 6.0f && dist < 24.0f && heightDiff < 3.0f)
                {
                    SpecialTimer = 4.2f;
                    new VisualFX(Position + new Vector3(0, 1.2f, 0), Rgb(245, 35, 70), 2.5f, 0.25f);
                    Vector3 jumpPos = Physics.ResolveHorizontalCollision(Position + Forward * MathF.Min(dist - 1.5f, 9.0f), 0.7f, floorY);
                    Position = new Vector3(jumpPos.X, floorY, jumpPos.Z);
                    new VisualFX(Position + new Vector3(0, 1.2f, 0), Rgb(245, 35, 70), 2.5f, 0.25f);
                }
                if (dist > 1.8f || heightDiff > 2.6f)
                {
                    Position += Forward * effSpeed * dt;
                    Vector3 res = Physics.ResolveHorizontalCollision(Position, 0.7f, floorY);
                    Position = new Vector3(res.X, floorY, res.Z);
                    float swing = MathF.Sin(t * 13f) * 38f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = swing;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -swing;
                }
                else
                {
                    AttackTimer -= dt;
                    if (AttackTimer <= 0f)
                    {
                        AttackTimer = 1.0f;
                        if (IsAlly && IsValid(allyEnemyTarget)) allyEnemyTarget!.TakeDamage(Damage);
                        else if (!IsAlly) CombatManager.DamagePlayer(Damage);
                        new VisualFX(pPos + new Vector3(0, 1.1f, 0), Rgb(245, 35, 70), 2.0f, 0.22f);
                    }
                }
            }
            else
            {
                UpdateBossAI(g, pPos, dist, effSpeed, t, floorY, projIsPlayer);
            }
        }

        private void UpdateBossAI(IPlayerContext g, Vector3 pPos, float dist, float effSpeed, float t, float floorY, bool projIsPlayer)
        {
            if (EnemyType == "boss")
            {
                if (Parts.Crown != null) Parts.Crown.RotationY += 85f * TimeDelta();
                if (dist > 8.5f)
                {
                    Position += Forward * effSpeed * TimeDelta();
                    Vector3 res = Physics.ResolveHorizontalCollision(Position, 1.4f, floorY);
                    Position = new Vector3(res.X, floorY, res.Z);
                    float stomp = MathF.Sin(t * 4.5f) * 24f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stomp;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stomp;
                }
                AttackTimer -= TimeDelta();
                if (AttackTimer <= 0f)
                {
                    AttackTimer = 2.0f;
                    Vector3 sp = Position + new Vector3(0, 3.4f, 0);
                    Vector3 baseAim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                    foreach (float angle in new[] { -14f, 0f, 14f })
                    {
                        float rad = angle * (MathF.PI / 180f);
                        Vector3 rotDir = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                        new Projectile(sp, rotDir, 27f, Damage, 0.65f, IsAlly ? Color.Lime : Color.Red, isPlayer: projIsPlayer, canProc: false);
                    }
                }
            }
            else if (EnemyType == "vagrant")
            {
                float dt = TimeDelta();
                if (Parts.Bell != null) Parts.Bell.Y = 4.2f + MathF.Sin(t * 2.8f) * 0.45f;
                if (Parts.Tentacles != null) Parts.Tentacles.RotationY += 65f * dt;
                if (dist > 12.0f) { Position += Forward * effSpeed * dt; Y = floorY; }

                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    AttackTimer = 2.2f;
                    Vector3 sp = Position + new Vector3(0, 3.8f, 0);
                    Vector3 baseAim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                    foreach (float angle in new[] { -18f, -6f, 6f, 18f })
                    {
                        float rad = angle * (MathF.PI / 180f);
                        Vector3 rotDir = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                        new Projectile(sp, rotDir, 24f, Damage * 0.85f, 0.62f, Color.SkyBlue, isPlayer: projIsPlayer, canProc: false);
                    }
                }
                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && !IsAlly)
                {
                    SpecialTimer = 6.5f;
                    new VisualFX(Position + new Vector3(0, 3.8f, 0), Color.SkyBlue, 16.0f, 0.55f);
                    if (dist <= 13.5f) CombatManager.DamagePlayer(Damage * 1.35f);
                }
            }
            else if (EnemyType == "overlord")
            {
                float dt = TimeDelta();
                if (Parts.Crown != null) Parts.Crown.RotationY -= 95f * dt;
                if (dist > 5.5f)
                {
                    Position += Forward * effSpeed * dt;
                    Vector3 res = Physics.ResolveHorizontalCollision(Position, 1.3f, floorY);
                    Position = new Vector3(res.X, floorY, res.Z);
                    float stomp = MathF.Sin(t * 6.0f) * 28f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stomp;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stomp;
                }
                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    AttackTimer = 2.1f;
                    Vector3 sp = Position + new Vector3(0, 3.2f, 0);
                    Vector3 baseAim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                    foreach (float angle in new[] { -20f, -10f, 0f, 10f, 20f })
                    {
                        float rad = angle * (MathF.PI / 180f);
                        Vector3 rotDir = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                        new Projectile(sp, rotDir, 32f, Damage * 0.8f, 0.55f, Rgb(245, 30, 65), isPlayer: projIsPlayer, canProc: false);
                    }
                }
                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && dist > 7.0f)
                {
                    SpecialTimer = 5.5f;
                    new VisualFX(Position + new Vector3(0, 2.5f, 0), Rgb(245, 30, 65), 6.0f, 0.35f);
                    Vector3 tpDest = pPos - Forward * 3.5f;
                    float b = Physics.WorldBound - 4.0f;
                    float dx = Math.Clamp(tpDest.X, -b, b), dz = Math.Clamp(tpDest.Z, -b, b);
                    Position = new Vector3(dx, Physics.GetSurfaceHeight(dx, dz, pPos.Y), dz);
                    new VisualFX(Position + new Vector3(0, 1.5f, 0), Rgb(245, 30, 65), 8.5f, 0.4f);
                }
            }
            else if (EnemyType == "lunar_chimera")
            {
                float dt = TimeDelta();
                if (Parts.Ring1 != null) Parts.Ring1.RotationZ += 120f * dt;
                if (Parts.Ring2 != null) Parts.Ring2.RotationX += 140f * dt;
                if (Parts.Core != null) Parts.Core.Y = 2.5f + MathF.Sin(t * 4.5f) * 0.25f;
                if (dist > 14.0f) { Position += Forward * effSpeed * dt; Y = floorY; }
                AttackTimer -= dt;
                if (AttackTimer <= 0f && dist < 42.0f)
                {
                    AttackTimer = 2.0f;
                    Vector3 sp = Position + new Vector3(0, 2.5f, 0);
                    Vector3 aim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                    foreach (float ang in new[] { -8f, 0f, 8f })
                    {
                        float rad = ang * (MathF.PI / 180f);
                        Vector3 d = new(aim.X * MathF.Cos(rad) - aim.Z * MathF.Sin(rad), aim.Y, aim.X * MathF.Sin(rad) + aim.Z * MathF.Cos(rad));
                        new Projectile(sp, d, 36f, Damage * 0.75f, 0.38f, Color.SkyBlue, isPlayer: projIsPlayer, canProc: false);
                    }
                }
            }
            else if (EnemyType == "mithrix")
            {
                UpdateMithrixPhases(pPos, dist, effSpeed, t);
            }
        }

        private void UpdateMithrixPhases(Vector3 pPos, float dist, float effSpeed, float t)
        {
            float dt = TimeDelta();
            float arenaY = Physics.ColosseumY;
            float maxArenaR = Physics.ColosseumRadius - 4.0f;

            if (Parts.Crown != null) Parts.Crown.RotationZ += 95f * dt;
            if (Parts.Cape != null) Parts.Cape.RotationX = 14f + MathF.Sin(t * 7.0f) * 8.0f;
            if (Parts.PhaseRing != null && Parts.PhaseRing.Enabled) Parts.PhaseRing.RotationY += 140f * dt;

            if (PhaseTransitionTimer > 0f)
            {
                PhaseTransitionTimer -= dt;
                Position = new Vector3(0, arenaY + MathF.Sin((float)Raylib.GetTime() * 8.0f) * 0.5f, 0);
                return;
            }

            if (SkyLeapState == 1)
            {
                SkyLeapTimer -= dt;
                Y = arenaY + 42.0f;
                if (SkyLeapTimer > 0.55f)
                {
                    float tx = Math.Clamp(pPos.X, -maxArenaR, maxArenaR);
                    float tz = Math.Clamp(pPos.Z, -maxArenaR, maxArenaR);
                    if (MathF.Sqrt(tx * tx + tz * tz) > maxArenaR)
                    {
                        float ang = MathF.Atan2(tz, tx);
                        tx = MathF.Cos(ang) * maxArenaR;
                        tz = MathF.Sin(ang) * maxArenaR;
                    }
                    SkyLeapTarget = new Vector3(tx, arenaY, tz);
                }

                if (TelegraphRing != null && !TelegraphRing.IsDestroyed)
                {
                    TelegraphRing.Position = SkyLeapTarget + new Vector3(0, 0.15f, 0);
                    TelegraphRing.RotationY += 180f * dt;
                    if (SkyLeapTimer <= 0.55f) TelegraphRing.Color = Rgb(255, 70, 90, 190);
                }

                if (SkyLeapTimer <= 0f)
                {
                    SkyLeapState = 0;
                    if (TelegraphRing != null && !TelegraphRing.IsDestroyed) Destroy(TelegraphRing);
                    TelegraphRing = null;

                    Vector3 landPos = SkyLeapTarget;
                    Position = landPos;
                    new LightningBeam(landPos + new Vector3(0, 42.0f, 0), landPos, Color.SkyBlue, 1.2f);
                    new VisualFX(landPos + new Vector3(0, 1.5f, 0), Color.SkyBlue, 16.0f, 0.55f);
                    new LunarShockwaveRing(landPos, Damage * 1.35f, 55.0f, 24.0f);

                    if (Physics.Distance2D(pPos, landPos) <= 7.5f && MathF.Abs(pPos.Y - arenaY) < 3.0f)
                        CombatManager.DamagePlayer(Damage * 1.45f);

                    for (int i = 0; i < 12; i++)
                    {
                        float ang = (i / 12.0f) * MathF.PI * 2f;
                        new Projectile(landPos + new Vector3(0, 1.8f, 0), new Vector3(MathF.Cos(ang), 0.04f, MathF.Sin(ang)), 32f, Damage, 0.58f, Color.SkyBlue, isPlayer: false, canProc: false);
                    }
                }
                return;
            }

            float moveSpd = effSpeed * (Phase == 4 ? 0.68f : (Phase == 3 ? 1.30f : (Phase == 2 ? 1.18f : 1.05f)));
            if (dist > 4.4f)
            {
                Position += Forward * moveSpd * dt;
                Vector3 resolved = Physics.ResolveHorizontalCollision(Position, 1.5f, arenaY);
                float rx = resolved.X, rz = resolved.Z;
                if (MathF.Sqrt(rx * rx + rz * rz) > maxArenaR)
                {
                    float ang = MathF.Atan2(rz, rx);
                    rx = MathF.Cos(ang) * maxArenaR;
                    rz = MathF.Sin(ang) * maxArenaR;
                }
                Position = new Vector3(rx, arenaY, rz);
                float stride = MathF.Sin(t * 10.0f) * 36.0f;
                if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stride;
                if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stride;
            }

            if (Parts.RightArm != null)
                Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, 0f, dt * 7.0f);

            AttackTimer -= dt;
            if (AttackTimer <= 0f)
            {
                if (Phase == 4)
                {
                    AttackTimer = 1.85f;
                    Vector3 sp = Position + new Vector3(0, 3.0f, 0);
                    Vector3 baseAim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                    foreach (float angle in new[] { -24f, -12f, 0f, 12f, 24f })
                    {
                        float rad = angle * (MathF.PI / 180f);
                        Vector3 d = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                        new Projectile(sp, d, 36f, Damage * 0.9f, 0.55f, Color.Magenta, isPlayer: false, canProc: false);
                    }
                    new LunarShockwaveRing(Position, Damage * 1.1f, 54.0f, 18.5f);
                }
                else if (dist <= 8.0f && MathF.Abs(pPos.Y - arenaY) <= 4.0f)
                {
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -125f;
                    Vector3 impactPos = new(X + Forward.X * 2.5f, arenaY, Z + Forward.Z * 2.5f);
                    new VisualFX(impactPos + new Vector3(0, 0.8f, 0), Color.SkyBlue, 8.0f, 0.35f);
                    new LunarShockwaveRing(impactPos, Damage * 1.25f, 54.0f, 21.0f);
                    if (dist <= 5.8f && MathF.Abs(pPos.Y - arenaY) < 2.5f)
                        CombatManager.DamagePlayer(Damage * 1.3f);
                    AttackTimer = Phase < 3 ? 1.55f : 1.25f;
                }
                else
                {
                    float dashSide = _rng.Next(2) == 0 ? -1f : 1f;
                    Vector3 newP = Position + Right * (dashSide * 5.5f);
                    if (MathF.Sqrt(newP.X * newP.X + newP.Z * newP.Z) <= maxArenaR)
                        Position = new Vector3(newP.X, arenaY, newP.Z);
                    new VisualFX(Position + new Vector3(0, 2.0f, 0), Color.SkyBlue, 3.8f, 0.25f);

                    Vector3 sp = Position + new Vector3(0, 3.0f, 0);
                    Vector3 baseAim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                    float[] angles = Phase >= 2 ? new[] { -12f, -6f, 0f, 6f, 12f } : new[] { -8f, 0f, 8f };
                    foreach (float angle in angles)
                    {
                        float rad = angle * (MathF.PI / 180f);
                        Vector3 d = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                        new Projectile(sp, d, 46f, Damage * 0.72f, 0.38f, Color.SkyBlue, isPlayer: false, canProc: false);
                    }
                    AttackTimer = Phase < 3 ? 1.65f : 1.35f;
                }
            }

            SpecialTimer -= dt;
            if (SpecialTimer <= 0f)
            {
                if (Phase == 1)
                {
                    SpecialTimer = 4.8f;
                    new VisualFX(Position + new Vector3(0, 2.0f, 0), Color.SkyBlue, 4.5f, 0.25f);
                    Vector3 lunge = Position + Forward * MathF.Min(dist - 2.5f, 16.0f);
                    if (MathF.Sqrt(lunge.X * lunge.X + lunge.Z * lunge.Z) > maxArenaR)
                    {
                        float ang = MathF.Atan2(lunge.Z, lunge.X);
                        lunge.X = MathF.Cos(ang) * maxArenaR;
                        lunge.Z = MathF.Sin(ang) * maxArenaR;
                    }
                    Position = new Vector3(lunge.X, arenaY, lunge.Z);
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -130f;
                    new LunarShockwaveRing(Position, Damage, 54.0f, 20.0f);
                }
                else if (Phase == 2)
                {
                    SpecialTimer = 6.8f;
                    SkyLeapState = 1;
                    SkyLeapTimer = 1.45f;
                    SkyLeapTarget = new Vector3(Math.Clamp(pPos.X, -maxArenaR, maxArenaR), arenaY, Math.Clamp(pPos.Z, -maxArenaR, maxArenaR));
                    TelegraphRing = new Entity(model: "diamond", position: SkyLeapTarget + new Vector3(0, 0.15f, 0),
                        scale: new(14.0f, 0.25f, 14.0f), color: Rgb(80, 225, 255, 150), unlit: true);
                    new VisualFX(Position + new Vector3(0, 2.0f, 0), Color.SkyBlue, 10.0f, 0.4f);
                    new LightningBeam(Position, Position + new Vector3(0, 42f, 0), Color.SkyBlue, 0.9f);
                }
                else if (Phase == 3)
                {
                    SpecialTimer = 5.0f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -135f;
                    new LunarPizzaAttack(new Vector3(0, arenaY, 0), Damage * 1.35f, 8);
                    new OrbitalStrikeFX(new Vector3(pPos.X, arenaY, pPos.Z), 0f);
                }
                else if (Phase == 4)
                {
                    SpecialTimer = 4.5f;
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = (float)_rng.NextDouble() * MathF.PI * 2f;
                        float r = 8.0f + (float)_rng.NextDouble() * 24.0f;
                        Vector3 strikeP = new(MathF.Cos(ang) * r, arenaY, MathF.Sin(ang) * r);
                        new LightningBeam(strikeP + new Vector3(0, 30f, 0), strikeP, Color.Magenta, 0.7f);
                        new VisualFX(strikeP + new Vector3(0, 1.0f, 0), Color.Magenta, 6.0f, 0.35f);
                    }
                }
            }
        }

        private static float TimeDelta() => Math.Min(Raylib.GetFrameTime(), 0.05f);
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