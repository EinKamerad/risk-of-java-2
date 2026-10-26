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
                    if (CombatManager.Controller != null && CombatManager.Controller.TryParryExternalAttack(_damage, Position))
                    {
                        Destroy(this);
                        return;
                    }
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
                        if (CombatManager.Controller != null && CombatManager.Controller.TryParryExternalAttack(_damage, Position))
                            break;
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
        public bool VagrantChargingNova = false;
        public float VagrantNovaTimer = 0f;
        public Entity? VagrantNovaSphere;

        public readonly EnemyParts Parts;
        protected readonly Entity LabelEnt;
        private readonly float _seedOffset;

        public CombatTarget(Vector3 pos, string enemyType, float coeff = 1.0f, int? forcedNetId = null) : base(position: pos)
        {
            if (forcedNetId.HasValue)
            {
                NetId = forcedNetId.Value;
                if (forcedNetId.Value >= _nextId) _nextId = forcedNetId.Value + 1;
            }
            else
            {
                NetId = _nextId++;
            }
            EnemyType = enemyType;
            IsBoss = Array.IndexOf(GameData.BossTypes, enemyType) >= 0 || enemyType == "mithrix";
            Coeff = coeff;
            _seedOffset = (NetId * 1.37f) % 10.0f;
            AttackTimer = 0.8f + (float)_rng.NextDouble() * 0.8f;
            SpecialTimer = 3.5f + (float)_rng.NextDouble() * 2.0f;

            Parts = EnemyAndBossBuilder.BuildEnemyMesh(this, enemyType);

            (float hpB, float dmgB, float spdB, int goldB, float hh, float hr) = enemyType switch
            {
                "wisp" => (46f, 14.5f, 5.0f, 18, 2.3f, 1.25f),
                "beetle" => (52f, 10.5f, 8.0f, 14, 0.7f, 1.10f),
                "jellyfish" => (42f, 18.0f, 8.6f, 17, 2.2f, 1.25f),
                "golem" => (185f, 24.0f, 4.2f, 34, 1.8f, 1.55f),
                "imp" => (95f, 17.5f, 7.8f, 28, 1.3f, 1.20f),
                "greater_wisp" => (160f, 21.5f, 4.4f, 38, 2.7f, 1.55f),
                "lunar_chimera" => (200f, 23.0f, 6.5f, 45, 2.5f, 1.45f),
                "boss" => (1150f, 28.0f, 4.0f, 160, 2.8f, 2.70f),
                "vagrant" => (1220f, 26.0f, 4.2f, 175, 3.8f, 2.90f),
                "overlord" => (1320f, 30.0f, 5.6f, 190, 2.9f, 2.75f),
                "mithrix" => (2600f, 34.0f, 8.6f, 600, 2.8f, 2.55f),
                _ => (65f, 12.0f, 6.8f, 16, 1.1f, 1.15f)
            };

            if (enemyType == "mithrix")
            {
                int itemCnt = 0;
                if (WorldContext.Game != null)
                {
                    foreach (var v in WorldContext.Game.Items.Values) itemCnt += v;
                }
                MithrixEffCoeff = MathF.Max(coeff, 2.55f) * (1.0f + 0.055f * itemCnt);
                MaxHp = hpB * MithrixEffCoeff;
                Damage = dmgB * MathF.Max(coeff, 2.0f);
            }
            else
            {
                MaxHp = hpB * coeff;
                Damage = dmgB * coeff;
            }

            Hp = MaxHp;
            Speed = spdB;
            GoldReward = (int)MathF.Ceiling(goldB * MathF.Pow(coeff, 1.32f));
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
            string phaseTag = EnemyType == "mithrix" ? $" [Ф{Phase}/4]" : "";
            string displayName = GameData.MonsterLog.TryGetValue(EnemyType, out var mLog)
                ? mLog.Name.ToUpper()
                : EnemyType.ToUpper();
            LabelEnt.Color = IsAlly ? Color.Lime : (EnemyType == "mithrix" ? Color.SkyBlue : Color.Red);
            LabelEnt.BillboardText = $"{prefix}{displayName}{phaseTag} [{(int)MathF.Max(0, Hp)}/{(int)MaxHp}]\n{barStr}";
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
            if (VagrantNovaSphere != null && !VagrantNovaSphere.IsDestroyed) Destroy(VagrantNovaSphere);
            VagrantChargingNova = false;
            if (IsBoss && WorldContext.Game != null)
            {
                ProgressData.Instance.BossKills++;
                WorldContext.Game.UnlockAchievement("ach_boss");
                WorldContext.Game.SpawnItemDrop(Position, null, "boss");
            }
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

        public virtual void TakeDamage(float amount, bool fromNetwork = false)
        {
            if (!IsValid(this) || SkyLeapState == 1 || PhaseTransitionTimer > 0f) return;

            var g = WorldContext.Game;
            if (EnemyType == "mithrix" && !fromNetwork)
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

            if (!fromNetwork)
                g?.NotifyEnemyDamaged(NetId, amount, Hp, Phase);

            UpdateHpBillboard();
            if (Hp <= 0f)
            {
                if (EnemyType == "mithrix" && Phase < 4)
                {
                    AdvanceMithrixPhase();
                    return;
                }
                Die(fromNetwork);
            }
        }

        public virtual void Die(bool fromNetwork = false)
        {
            if (IsDead || IsDestroyed) return;
            IsDead = true;
            if (!fromNetwork)
                WorldContext.Game?.NotifyEnemyKilled(NetId);
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

            Vector3 pPos = g.GetNearestPlayerPos(Position, out bool targetIsLocal);
            CombatTarget? allyEnemyTarget = null;
            MindustryTurret? structTarget = null;
            DummyClone? cloneTarget = null;

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
                else pPos = g.PlayerPos;
            }
            else
            {
                // Если локальный игрок — Корабль Mindustry в строительном режиме (неуязвим), мобы не могут выбрать его целью!
                bool localUntargetable = ! g.CanPlayerInteract;
                float bestTargetDist = (targetIsLocal && localUntargetable) ? 9999f : Vector3.Distance(Position, pPos);

                // Проверяем союзных монстров (переманенных Медиком)
                foreach (var other in CombatManager.Enemies)
                {
                    if (IsValid(other) && other.IsAlly)
                    {
                        float d = Vector3.Distance(Position, other.Position);
                        if (d < bestTargetDist)
                        {
                            bestTargetDist = d;
                            allyEnemyTarget = other;
                            structTarget = null;
                            cloneTarget = null;
                            pPos = other.Position;
                            targetIsLocal = false;
                        }
                    }
                }

                // Проверяем клонов М4А1
                if (CombatManager.Controller != null)
                {
                    foreach (var cl in CombatManager.Controller.DummyClones)
                    {
                        if (IsValid(cl))
                        {
                            float d = Vector3.Distance(Position, cl.Position);
                            if (d < bestTargetDist)
                            {
                                bestTargetDist = d;
                                cloneTarget = cl;
                                allyEnemyTarget = null;
                                structTarget = null;
                                pPos = cl.Position;
                                targetIsLocal = false;
                            }
                        }
                    }

                    // Проверяем постройки Корабля Mindustry (турели, буры, пилон, ядро)
                    foreach (var st in CombatManager.Controller.MindustryTurrets)
                    {
                        if (IsValid(st))
                        {
                            float d = Vector3.Distance(Position, st.Position);
                            if (d < bestTargetDist)
                            {
                                bestTargetDist = d;
                                structTarget = st;
                                allyEnemyTarget = null;
                                cloneTarget = null;
                                pPos = st.Position;
                                targetIsLocal = false;
                            }
                        }
                    }
                }

                // Если все цели недоступны (например, корабль в стройке, а построек и союзников нет) — враг ждёт на месте
                if (bestTargetDist >= 9990f)
                    return;
            }

            float dist = Physics.Distance2D(Position, pPos);
            float heightDiff = MathF.Abs(pPos.Y - Y);
            float localDist = Physics.Distance2D(Position, g.PlayerPos);
            float localHeightDiff = MathF.Abs(g.PlayerPos.Y - Y);
            LookAt2D(pPos);
            float floorY = Physics.GetSurfaceHeight(X, Z, Y);
            bool projIsPlayer = IsAlly;
            if (!IsAlly && HallowedMistZone.IsHiddenFromOutside(pPos, Position))
            {
                AttackTimer = MathF.Max(AttackTimer, 0.45f);
                SpecialTimer = MathF.Max(SpecialTimer, 0.45f);
            }

            void ApplyEnemyMeleeDamage(float dmgVal)
            {
                if (IsAlly && IsValid(allyEnemyTarget)) allyEnemyTarget!.TakeDamage(dmgVal);
                else if (!IsAlly)
                {
                    if (IsValid(structTarget)) structTarget!.TakeStructureDamage(dmgVal);
                    else if (IsValid(cloneTarget)) { cloneTarget!.Hp -= dmgVal; if (cloneTarget.Hp <= 0f) Destroy(cloneTarget); }
                    else if (IsValid(allyEnemyTarget)) allyEnemyTarget!.TakeDamage(dmgVal);
                    else if (g.CanPlayerInteract && (targetIsLocal || (localDist <= 2.2f && localHeightDiff <= 2.6f)))
                        CombatManager.DamagePlayer(dmgVal);
                }
            }

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
                        ApplyEnemyMeleeDamage(Damage);
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
                        ApplyEnemyMeleeDamage(Damage);
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
                        if (dist <= 4.5f)
                            ApplyEnemyMeleeDamage(Damage);
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
                        ApplyEnemyMeleeDamage(Damage);
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
                if (dist > 12.0f && !VagrantChargingNova) { Position += Forward * effSpeed * dt; Y = floorY; }

                AttackTimer -= dt;
                if (AttackTimer <= 0f && !VagrantChargingNova)
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

                // Сверхнова Бродяги: только когда мало ХП (<50%), с 2.2с расширяющейся сферой-предупреждением и спасением за укрытиями!
                if (!IsAlly && Hp <= MaxHp * 0.50f)
                {
                    if (!VagrantChargingNova)
                    {
                        SpecialTimer -= dt;
                        if (SpecialTimer <= 0f)
                        {
                            VagrantChargingNova = true;
                            VagrantNovaTimer = 2.2f;
                            VagrantNovaSphere = new Entity(parent: this, model: "sphere", position: new(0, 3.8f, 0),
                                scaleUniform: 1.5f, color: Rgb(255, 215, 65, 95), unlit: true);
                            VagrantNovaSphere.AnimateScale(32.0f, 2.2f);
                        }
                    }
                    else
                    {
                        VagrantNovaTimer -= dt;
                        if (VagrantNovaSphere != null && !VagrantNovaSphere.IsDestroyed)
                        {
                            byte alpha = (byte)(90 + (int)(MathF.Abs(MathF.Sin((float)Raylib.GetTime() * 14f)) * 95f));
                            VagrantNovaSphere.Color = Rgb(255, 195, 50, alpha);
                        }
                        if (VagrantNovaTimer <= 0f)
                        {
                            VagrantChargingNova = false;
                            SpecialTimer = 9.0f;
                            if (VagrantNovaSphere != null && !VagrantNovaSphere.IsDestroyed)
                                Destroy(VagrantNovaSphere);
                            VagrantNovaSphere = null;

                            Vector3 novaOrigin = Position + new Vector3(0, 3.8f, 0);
                            new VisualFX(novaOrigin, Color.Yellow, 32.0f, 0.55f);
                            Vector3 playerEye = g.PlayerPos + new Vector3(0, 1.1f, 0);
                            if (Vector3.Distance(novaOrigin, playerEye) <= 16.5f && Physics.HasLineOfSight(novaOrigin, playerEye))
                            {
                                if (CombatManager.Controller == null || !CombatManager.Controller.TryParryExternalAttack(Damage * 1.85f, novaOrigin))
                                    CombatManager.DamagePlayer(Damage * 1.85f);
                            }
                        }
                    }
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
                    if (Physics.Distance2D(Position, WorldContext.Game!.PlayerPos) <= 5.8f && MathF.Abs(WorldContext.Game.PlayerPos.Y - arenaY) < 2.5f)
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
        public static readonly List<HallowedMistZone> ActiveZones = new();
        private readonly float _tickDmg;
        private float _duration = 5.0f;
        private float _tickTimer = 0f;
        public readonly float Radius = 7.8f;
        private readonly Entity _pillarsHub;

        public static bool IsHiddenFromOutside(Vector3 targetPos, Vector3 attackerPos)
        {
            ActiveZones.RemoveAll(z => !IsValid(z));
            foreach (var z in ActiveZones)
            {
                bool targetInside = Physics.Distance2D(targetPos, z.Position) <= z.Radius;
                bool attackerInside = Physics.Distance2D(attackerPos, z.Position) <= z.Radius;
                if (targetInside && !attackerInside)
                    return true;
            }
            return false;
        }

        public HallowedMistZone(Vector3 centerPos, float tickDmg)
            : base(position: centerPos + new Vector3(0, 0.08f, 0))
        {
            _tickDmg = tickDmg;
            ActiveZones.Add(this);
            new Entity(parent: this, model: "sphere", scale: new(Radius * 2f, 0.08f, Radius * 2f), color: Rgb(75, 210, 255, 70), unlit: true);
            _pillarsHub = new Entity(parent: this);
            for (int i = 0; i < 8; i++)
            {
                float ang = (i / 8.0f) * MathF.PI * 2f;
                new Entity(parent: _pillarsHub, model: "diamond", position: new(MathF.Cos(ang) * Radius, 1.2f, MathF.Sin(ang) * Radius),
                    scale: new(0.35f, 2.2f, 0.35f), color: Rgb(145, 235, 255, 160), unlit: true);
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            _duration -= dt;
            _pillarsHub.RotationY += 55f * dt;
            if (_duration <= 0f) { ActiveZones.Remove(this); Destroy(this); return; }

            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = 0.4f;
                if (g != null && Physics.Distance2D(g.PlayerPos, Position) <= Radius)
                    g.HealPlayer(5.0f);

                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Physics.Distance2D(e.Position, Position) <= Radius)
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
        private float _m2Cd = 3.5f;
        private float _shiftCd = 4.2f;
        private Vector3 _roamTarget;
        private float _roamTimer = 0f;
        private readonly SurvivorParts _parts;
        private readonly Entity _linkRing;
        private readonly Entity _labelEnt;
        private static readonly Random _rng = new();

        public DummyClone(Vector3 ownerPos, float ownerMaxHp, int slotIdx)
            : base(position: ownerPos + new Vector3(MathF.Cos(slotIdx * 1.57f) * 3.0f, 0, MathF.Sin(slotIdx * 1.57f) * 3.0f))
        {
            SlotIdx = slotIdx;
            MaxHp = MathF.Max(60f, ownerMaxHp * 0.85f);
            Hp = MaxHp;
            _roamTarget = Position;
            _parts = SurvivorDatabase.BuildSurvivorMesh(this, "m4a1");
            _linkRing = new Entity(parent: this, model: "diamond", scale: new(1.35f, 0.08f, 1.35f), position: new(0, 0.06f, 0), color: Rgb(95, 255, 135, 180), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.65f, 0), color: Rgb(95, 255, 135))
            {
                BillboardText = $"КЛОН M4A1 #{slotIdx + 1}"
            };
        }

        public void CommandBurstShot()
        {
            if (IsDead || IsDestroyed) return;
            FireAtNearest(1.85f, isJupiter: true);
        }

        public void FireAtNearest(float dmgMult = 0.65f, bool isJupiter = false)
        {
            var c = CombatManager.Controller;
            if (c == null) return;
            CombatTarget? nearest = null;
            float bestD = 44f;
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
                if (isJupiter)
                {
                    new VisualFX(sp, Rgb(95, 255, 135), 2.2f, 0.22f);
                    new Projectile(sp, aim, 58f, c.BaseDamage * dmgMult, 0.62f, Rgb(95, 255, 135), isPlayer: true, aoe: 5.5f, pierce: true, canProc: true);
                }
                else
                {
                    new Projectile(sp, aim, 68f, c.BaseDamage * dmgMult, 0.20f, Rgb(95, 255, 135), isPlayer: true, canProc: true);
                }
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            var c = CombatManager.Controller;
            if (g == null || c == null || IsDead || IsDestroyed) return;

            _linkRing.RotationY += 90f * dt;
            MaxHp = MathF.Max(60f, g.PlayerMaxHp * 0.85f);
            Hp = MathF.Min(MaxHp, Hp + 3.0f * dt);
            _m2Cd = MathF.Max(0f, _m2Cd - dt);
            _shiftCd = MathF.Max(0f, _shiftCd - dt);

            // 1. Поиск выпавших обычных предметов на карте (не Лунных и не Пустотных)
            ItemDrop? targetDrop = null;
            float bestDropDist = 48f;
            foreach (var ent in g.StageEntities)
            {
                if (ent is ItemDrop drop && IsValid(drop) && !drop.Info.IsLunar && !drop.Info.IsVoid)
                {
                    float d = Vector3.Distance(Position, drop.Position);
                    if (d < bestDropDist) { bestDropDist = d; targetDrop = drop; }
                }
            }

            // 2. Поиск закрытых обычных сундуков (игнорируем Лунные Бутоны lunar_pod)
            Chest? targetChest = null;
            float bestChestDist = 52f;
            if (targetDrop == null)
            {
                foreach (var ent in g.StageEntities)
                {
                    if (ent is Chest ch && IsValid(ch) && !ch.Opened && ch.ChestType != "lunar_pod")
                    {
                        bool canAfford = g.Gold >= ch.Cost;
                        if (canAfford)
                        {
                            float d = Vector3.Distance(Position, ch.Position);
                            if (d < bestChestDist) { bestChestDist = d; targetChest = ch; }
                        }
                    }
                }
            }

            // 3. Поиск ближайшего врага для охоты
            CombatTarget? nearestEnemy = null;
            float bestEnemyDist = 50f;
            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly)
                {
                    float d = Vector3.Distance(Position, e.Position);
                    if (d < bestEnemyDist) { bestEnemyDist = d; nearestEnemy = e; }
                }
            }

            Vector3 desiredPos;
            string stateTag = "ПАТРУЛЬ";
            if (targetDrop != null)
            {
                desiredPos = targetDrop.Position;
                stateTag = "СБОР ЛУТА";
                if (Vector3.Distance(Position, targetDrop.Position) <= 2.4f)
                {
                    g.CollectItem(targetDrop.ItemKey);
                    new VisualFX(targetDrop.Position, targetDrop.Info.Color, 2.6f, 0.28f);
                    g.StageEntities.Remove(targetDrop);
                    Destroy(targetDrop);
                }
            }
            else if (targetChest != null)
            {
                desiredPos = targetChest.Position;
                stateTag = "К СУНДУКУ";
                if (Vector3.Distance(Position, targetChest.Position) <= 3.0f)
                {
                    if (targetChest.ChestType == "lunar_pod" && g.LunarCoins >= targetChest.Cost)
                    {
                        g.LunarCoins -= targetChest.Cost;
                        targetChest.OpenVisual(GameData.RollItemFromPool(targetChest.PoolType));
                    }
                    else if (targetChest.ChestType != "lunar_pod" && g.Gold >= targetChest.Cost)
                    {
                        g.Gold -= targetChest.Cost;
                        targetChest.OpenVisual(GameData.RollItemFromPool(targetChest.PoolType));
                    }
                }
            }
            else if (nearestEnemy != null)
            {
                stateTag = "БОЙ";
                float orbitAng = (float)Raylib.GetTime() * 1.2f + SlotIdx * 1.57f;
                desiredPos = nearestEnemy.Position + new Vector3(MathF.Cos(orbitAng) * 9.5f, 0, MathF.Sin(orbitAng) * 9.5f);
            }
            else
            {
                _roamTimer -= dt;
                if (_roamTimer <= 0f || Physics.Distance2D(Position, _roamTarget) < 2.5f)
                {
                    _roamTimer = 4.0f + SlotIdx;
                    float ang = (float)_rng.NextDouble() * MathF.PI * 2f;
                    float r = 10f + (float)_rng.NextDouble() * 18f;
                    _roamTarget = g.PlayerPos + new Vector3(MathF.Cos(ang) * r, 0, MathF.Sin(ang) * r);
                }
                desiredPos = _roamTarget;
            }

            _labelEnt.BillboardText = $"КЛОН M4A1 #{SlotIdx + 1} [{stateTag}]\n{(int)Hp}/{(int)MaxHp} HP";

            if (MathF.Abs(Y - g.PlayerPos.Y) > 25f)
            {
                Position = g.PlayerPos + new Vector3(MathF.Cos(SlotIdx) * 3f, 0.5f, MathF.Sin(SlotIdx) * 3f);
            }

            Vector3 toDesired = new(desiredPos.X - X, 0, desiredPos.Z - Z);
            if (toDesired.Length() > 0.75f)
            {
                Vector3 dir = Vector3.Normalize(toDesired);
                // Самостоятельное использование Shift (рывок + щит)
                if (_shiftCd <= 0f && (toDesired.Length() > 10f || nearestEnemy != null))
                {
                    _shiftCd = 4.5f;
                    Position += dir * 6.5f;
                    c.Shield = MathF.Min(g.PlayerMaxHp, c.Shield + 10f);
                    new VisualFX(Position + new Vector3(0, 1f, 0), Rgb(95, 255, 135), 2.2f, 0.22f);
                }
                else
                {
                    Position += dir * (c.MoveSpeed * 1.08f) * dt;
                }
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

            float wb = Physics.WorldBound - 2f;
            X = Math.Clamp(X, -wb, wb);
            Z = Math.Clamp(Z, -wb, wb);
            Y = Physics.GetSurfaceHeight(X, Z, Y);

            if (nearestEnemy != null)
            {
                if (_m2Cd <= 0f)
                {
                    _m2Cd = 4.8f;
                    FireAtNearest(2.2f, isJupiter: true);
                }
                _shootCd -= dt;
                if (_shootCd <= 0f)
                {
                    _shootCd = MathF.Max(0.14f, 1.15f / MathF.Max(1f, c.FireRate));
                    FireAtNearest(0.65f, isJupiter: false);
                }
            }
        }
    }

    public class MindustryTurret : Entity
    {
        private static int _nextStructNetId = 5000;
        public readonly int StructNetId;
        public readonly int SlotIdx;
        public readonly string BuildType; // "core_main", "drill", "smelter", "refinery", "pylon", "core_omega", "duo", "salvo", "spectre"
        public readonly bool IsRemoteVisual;
        public float MaxHp;
        public float Hp;
        public float HitHeight = 1.2f;
        public float HitRadius = 1.4f;
        public bool IsNearRichVein = false;
        private float _shootCd = 0.2f;
        private float _mendTimer = 1.5f;
        private readonly Entity _head;
        private readonly Entity _mendRing;
        private readonly Entity _labelEnt;

        public MindustryTurret(Vector3 pos, float ownerMaxHp, int slotIdx, string buildType = "spectre", bool isRemoteVisual = false, int? forcedNetId = null) : base(position: pos)
        {
            StructNetId = forcedNetId ?? _nextStructNetId++;
            SlotIdx = slotIdx;
            BuildType = buildType;
            IsRemoteVisual = isRemoteVisual;

            foreach (var obs in Physics.Obstacles)
            {
                if (MathF.Sqrt((pos.X - obs.X) * (pos.X - obs.X) + (pos.Z - obs.Z) * (pos.Z - obs.Z)) <= obs.Radius + 4.5f)
                {
                    IsNearRichVein = true;
                    break;
                }
            }
            if (pos.Y > 0.5f) IsNearRichVein = true;

            Color mainCol = buildType switch
            {
                "core_main" => Rgb(255, 205, 50),
                "drill" => Rgb(235, 145, 75),
                "smelter" => Rgb(140, 210, 255),
                "refinery" => Rgb(235, 95, 215),
                "pylon" => Rgb(85, 245, 135),
                "core_omega" => Rgb(95, 225, 255),
                "duo" => Rgb(215, 145, 65),
                "salvo" => Rgb(255, 110, 55),
                _ => Rgb(255, 75, 95)
            };

            MaxHp = buildType switch
            {
                "core_main" => MathF.Max(450f, ownerMaxHp * 2.8f),
                "drill" => 130f,
                "smelter" => 160f,
                "refinery" => 190f,
                "pylon" => 180f,
                "core_omega" => 320f,
                "duo" => 170f,
                "salvo" => 240f,
                _ => 380f
            };
            Hp = MaxHp;
            if (buildType == "core_main") HitRadius = 2.2f;

            float baseScale = buildType is "core_main" or "core_omega" ? 3.2f : 2.1f;
            new Entity(parent: this, model: "cube", scale: new(baseScale, 0.45f, baseScale), position: new(0, 0.22f, 0), color: Rgb(55, 62, 75));
            new Entity(parent: this, model: "diamond", scale: new(baseScale * 1.05f, 0.15f, baseScale * 1.05f), position: new(0, 0.45f, 0), color: mainCol, unlit: true);

            _head = new Entity(parent: this, position: new(0, 0.85f, 0));
            if (buildType is "core_main" or "core_omega")
            {
                new Entity(parent: _head, model: "cube", scale: new(2.1f, 1.6f, 2.1f), position: new(0, 0.4f, 0), color: mainCol);
                new Entity(parent: _head, model: "diamond", scale: new(2.5f, 0.45f, 2.5f), position: new(0, 1.45f, 0), color: Color.White, unlit: true);
                new Entity(parent: _head, model: "sphere", scaleUniform: 0.95f, position: new(0, 1.85f, 0), color: buildType == "core_main" ? Color.Gold : Color.SkyBlue, unlit: true);
            }
            else if (buildType == "drill")
            {
                new Entity(parent: _head, model: "diamond", scale: new(1.35f, 1.45f, 1.35f), position: new(0, 0.2f, 0), color: mainCol, unlit: true);
                new Entity(parent: _head, model: "cube", scale: new(1.5f, 0.35f, 1.5f), position: new(0, 0.5f, 0), color: Rgb(65, 75, 92));
            }
            else if (buildType is "smelter" or "refinery")
            {
                new Entity(parent: _head, model: "cube", scale: new(1.55f, 1.15f, 1.55f), position: new(0, 0.25f, 0), color: Rgb(48, 56, 72));
                new Entity(parent: _head, model: "sphere", scaleUniform: 0.95f, position: new(0, 0.85f, 0), color: mainCol, unlit: true);
            }
            else if (buildType == "pylon")
            {
                new Entity(parent: _head, model: "diamond", scale: new(0.95f, 1.95f, 0.95f), position: new(0, 0.5f, 0), color: Color.Lime, unlit: true);
                new Entity(parent: _head, model: "sphere", scaleUniform: 0.72f, position: new(0, 1.65f, 0), color: Color.White, unlit: true);
            }
            else
            {
                float sc = buildType == "spectre" ? 1.25f : (buildType == "salvo" ? 1.05f : 0.85f);
                new Entity(parent: _head, model: "cube", scale: new(1.15f * sc, 0.65f * sc, 1.25f * sc), color: mainCol);
                new Entity(parent: _head, model: "cube", scale: new(0.24f * sc, 0.24f * sc, 1.35f * sc), position: new(-0.28f * sc, 0.05f, 0.55f * sc), color: Rgb(42, 48, 58));
                new Entity(parent: _head, model: "cube", scale: new(0.24f * sc, 0.24f * sc, 1.35f * sc), position: new(0.28f * sc, 0.05f, 0.55f * sc), color: Rgb(42, 48, 58));
            }

            _mendRing = new Entity(parent: this, model: "diamond", scale: new(baseScale * 0.9f, 0.08f, baseScale * 0.9f), position: new(0, 0.12f, 0), color: mainCol, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, buildType == "core_main" ? 3.4f : 2.65f, 0), color: mainCol);
        }

        public void TakeStructureDamage(float dmg, bool fromNetwork = false)
        {
            if (IsDead || IsDestroyed) return;
            Hp -= dmg;
            new VisualFX(Position + new Vector3(0, 1.0f, 0), Color.Orange, 1.8f, 0.2f);
            if (BuildType == "core_main" && !IsRemoteVisual)
                WorldContext.Game?.ShowBanner($"ВНИМАНИЕ! ГЛАВНОЕ ЯДРО АТАКУЮТ [{(int)MathF.Max(0, Hp)}/{(int)MaxHp} HP]!", Color.Red, 1.5f);
            if (!fromNetwork && Net.NetworkManager.Instance.Connected)
            {
                Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "mindustry_hit",
                    ["id"] = StructNetId,
                    ["dmg"] = MathF.Round(dmg, 1)
                });
            }
            if (Hp <= 0f)
            {
                IsDead = true;
                new VisualFX(Position + new Vector3(0, 1.2f, 0), Color.Red, 5.5f, 0.4f);
                if (BuildType == "core_main" && !IsRemoteVisual && CombatManager.Controller != null)
                    CombatManager.Controller.OnMainCoreDestroyed();
                Destroy(this);
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            var c = CombatManager.Controller;
            if (g == null || c == null || IsDead || IsDestroyed) return;

            float boostMult = c.MindustryOverdriveTimer > 0f ? 2.0f : 1.0f;
            _mendRing.RotationY += 85f * boostMult * dt;

            if (BuildType == "core_main")
            {
                _head.RotationY += 45f * dt;
                if (!IsRemoteVisual && c.CharKey == "mindustry")
                    c.MindustryCopper = MathF.Min(999f, c.MindustryCopper + 2.2f * boostMult * dt);
                _labelEnt.BillboardText = $"{(IsRemoteVisual ? "[ВРАЖЕСКОЕ] " : "")}ГЛАВНОЕ ЯДРО КОРАБЛЯ «ГАММА»\n[{(int)MathF.Max(0, Hp)}/{(int)MaxHp} HP]";
                return;
            }

            if (BuildType == "drill")
            {
                _head.RotationY += (IsNearRichVein ? 220f : 120f) * boostMult * dt;
                float copRate = (IsNearRichVein ? 3.5f : 1.8f) * boostMult;
                if (!IsRemoteVisual && c.CharKey == "mindustry")
                {
                    c.MindustryCopper = MathF.Min(999f, c.MindustryCopper + copRate * dt);
                    if (IsNearRichVein)
                    {
                        c.MindustrySilicon = MathF.Min(999f, c.MindustrySilicon + 0.9f * boostMult * dt);
                        c.MindustryThorium = MathF.Min(999f, c.MindustryThorium + 0.45f * boostMult * dt);
                    }
                }
                _mendTimer -= dt;
                if (_mendTimer <= 0f)
                {
                    _mendTimer = 3.0f;
                    if (!IsRemoteVisual) g.HealPlayer(IsNearRichVein ? 8.0f : 4.0f);
                }
                _labelEnt.BillboardText = $"ПНЕВМО-БУР {(IsNearRichVein ? "[ЖИЛА: МЕДЬ+КРЕМНИЙ+ТОРИЙ]" : "[МЕДЬ]")}\n[{(int)Hp}/{(int)MaxHp} HP]";
                return;
            }

            if (BuildType == "smelter")
            {
                _head.RotationY += 140f * boostMult * dt;
                if (!IsRemoteVisual && c.CharKey == "mindustry" && c.MindustryCopper >= 0.8f * dt)
                {
                    c.MindustryCopper = MathF.Max(0f, c.MindustryCopper - 0.8f * boostMult * dt);
                    c.MindustrySilicon = MathF.Min(999f, c.MindustrySilicon + 2.2f * boostMult * dt);
                }
                _labelEnt.BillboardText = $"КРЕМНИЕВЫЙ ПЛАВИЛЬЩИК (+Кремний)\n[{(int)Hp}/{(int)MaxHp} HP]";
                return;
            }

            if (BuildType == "refinery")
            {
                _head.RotationY -= 160f * boostMult * dt;
                if (!IsRemoteVisual && c.CharKey == "mindustry" && c.MindustrySilicon >= 0.6f * dt)
                {
                    c.MindustrySilicon = MathF.Max(0f, c.MindustrySilicon - 0.6f * boostMult * dt);
                    c.MindustryThorium = MathF.Min(999f, c.MindustryThorium + 1.8f * boostMult * dt);
                    c.Shield = MathF.Min(g.PlayerMaxHp, c.Shield + 2.5f * dt);
                }
                _labelEnt.BillboardText = $"ТОРИЕВЫЙ ПЕРЕРАБОТЧИК (+Торий и Щит)\n[{(int)Hp}/{(int)MaxHp} HP]";
                return;
            }

            if (BuildType == "pylon")
            {
                _head.RotationY += 110f * dt;
                _mendTimer -= dt;
                if (_mendTimer <= 0f)
                {
                    _mendTimer = 1.4f / boostMult;
                    new VisualFX(Position + new Vector3(0, 1.0f, 0), Color.Lime, 9.5f, 0.32f);
                    if (!IsRemoteVisual && Vector3.Distance(Position, g.PlayerPos) <= 22.0f)
                    {
                        g.HealPlayer(20.0f);
                        c.Shield = MathF.Min(g.PlayerMaxHp, c.Shield + 12f);
                    }
                    foreach (var t in c.MindustryTurrets)
                        if (IsValid(t) && t.IsRemoteVisual == IsRemoteVisual) t.Hp = MathF.Min(t.MaxHp, t.Hp + 28f);
                }
                _labelEnt.BillboardText = $"УЛУЧШЕННЫЙ РЕМОНТНЫЙ КУПОЛ (22м)\n[{(int)Hp}/{(int)MaxHp} HP]";
                return;
            }

            if (BuildType == "core_omega")
            {
                _head.RotationY -= 95f * dt;
                if (!IsRemoteVisual && c.CharKey == "mindustry")
                {
                    c.MindustryCopper = MathF.Min(999f, c.MindustryCopper + 2.0f * boostMult * dt);
                    c.MindustrySilicon = MathF.Min(999f, c.MindustrySilicon + 1.4f * boostMult * dt);
                    c.MindustryThorium = MathF.Min(999f, c.MindustryThorium + 0.9f * boostMult * dt);
                }
                _labelEnt.BillboardText = $"ЯДРО «ЦИТАДЕЛЬ» (+45% УРОН И +РЕСУРСЫ)\n[{(int)Hp}/{(int)MaxHp} HP]";
                return;
            }

            // Турели: "duo", "salvo", "spectre"
            bool buffedByBeacon = false;
            foreach (var b in c.MindustryTurrets)
            {
                if (IsValid(b) && b.BuildType is "core_omega" or "core_main" && Vector3.Distance(Position, b.Position) <= 28f)
                {
                    buffedByBeacon = true;
                    break;
                }
            }

            string tTitle = BuildType == "spectre" ? "ЛАЗЕР «ИСПЕПЕЛИТЕЛЬ»" : (BuildType == "salvo" ? "РАКЕТНИЦА «ЗАЛП»" : "ТУРЕЛЬ «ДУЭТ»");
            _labelEnt.BillboardText = $"{tTitle} {(buffedByBeacon ? "[УСИЛЕНА ЯДРОМ]" : "")}\n[{(int)Hp}/{(int)MaxHp} HP]";

            _shootCd -= dt;
            if (_shootCd <= 0f)
            {
                float rateMult = (BuildType == "spectre" ? 1.55f : (BuildType == "salvo" ? 0.85f : 1.05f)) * (buffedByBeacon ? 1.45f : 1.0f) * boostMult;
                _shootCd = MathF.Max(0.09f, 0.85f / (MathF.Max(1f, c.FireRate) * rateMult));

                Vector3? targetPos = null;
                float bestD = BuildType == "spectre" ? 48f : 36f;

                if (!IsRemoteVisual)
                {
                    foreach (var e in CombatManager.Enemies)
                    {
                        if (IsValid(e) && !e.IsAlly)
                        {
                            float d = Vector3.Distance(Position, e.Position);
                            if (d < bestD) { bestD = d; targetPos = e.Position + new Vector3(0, e.HitHeight, 0); }
                        }
                    }
                    if (Physics.SpecialMode == "pvp" && targetPos == null)
                    {
                        Vector3 nearRp = Program.GetNearestRemotePlayerPos(Position, out float rpDist);
                        if (rpDist < bestD) targetPos = nearRp + new Vector3(0, 1.2f, 0);
                    }
                }

                if (targetPos.HasValue)
                {
                    _head.LookAt2D(targetPos.Value);
                    Vector3 sp = Position + new Vector3(0, 1.0f, 0) + _head.Forward * 1.1f;
                    Vector3 aim = Vector3.Normalize(targetPos.Value - sp);
                    float dmgM = IsRemoteVisual ? 0f : (BuildType == "spectre" ? 1.65f : (BuildType == "salvo" ? 1.25f : 0.65f)) * (buffedByBeacon ? 1.45f : 1.0f);
                    if (BuildType == "salvo")
                    {
                        for (int r = -1; r <= 1; r++)
                        {
                            Vector3 rDir = Vector3.Normalize(aim + _head.Right * (r * 0.12f) + new Vector3(0, 0.15f, 0));
                            new Projectile(sp, rDir, 54f, c.BaseDamage * dmgM * 0.65f, 0.30f, Color.Orange, isPlayer: true, aoe: 3.5f, homing: true, canProc: !IsRemoteVisual);
                        }
                    }
                    else
                    {
                        float aoeR = BuildType == "spectre" ? 4.2f : 0f;
                        new Projectile(sp, aim, 84f, c.BaseDamage * dmgM, BuildType == "spectre" ? 0.42f : 0.20f, BuildType == "spectre" ? Rgb(255, 85, 110) : Rgb(255, 210, 55), isPlayer: true, aoe: aoeR, pierce: BuildType == "spectre", canProc: !IsRemoteVisual);
                    }
                }
            }
        }
    }
}