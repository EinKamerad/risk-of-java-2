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
                    CombatManager.DamagePlayer(_damage, CombatManager.FindNearestHostileAt(Position));
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
                        CombatManager.DamagePlayer(_damage, CombatManager.FindNearestHostileAt(Position));
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
        public bool IsElite = false;
        public bool IsDoppelganger = false;
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
        public float LiandryBurnTimer = 0f;
        public float LiandryBurnTick = 0f;
        public float LiandryBurnDps = 0f;
        public float CharmTimer = 0f;
        public float SlowTimer = 0f;
        public float StunTimer = 0f;
        public float ArmorBreakTimer = 0f;
        public float AttackTimer;
        public float SpecialTimer;
        public float SecondaryTimer = 1.6f;

        // Поля вертикальной физики (прыжки на платформы) и тактической навигации ИИ
        public float VelY = 0f;
        public bool IsGrounded = true;
        public float JumpCooldown = 0f;
        public Vector3 JumpHorizVel = Vector3.Zero;
        private float _steerSide = 1f;
        private float _strafeDir = 1f;
        private float _strafeSwitchTimer = 2.2f;

        // Адаптивная защита боссов от мгновенного стирания и система замахов/телеграфов
        public float RecentDamageTaken = 0f;
        public float RecentDamageTimer = 0f;
        public int WindupType = 0; // 0 = нет, 1 = замах основной атаки, 2 = замах спец-атаки
        public int WindupSubType = 0;
        public float WindupTimer = 0f;
        public float WindupDuration = 0.01f;
        public Vector3 LockedAimPos = Vector3.Zero;
        public string WindupTag = "";
        public Entity? WindupIndicator;

        // Поля 4 фаз Митрикса
        public int Phase = 1;
        public float PhaseTransitionTimer = 0f;
        public int SkyLeapState = 0;
        public float SkyLeapTimer = 0f;
        public Vector3 SkyLeapTarget = new(0, Physics.ColosseumY, 0);
        public Entity? TelegraphRing;
        public readonly List<string> StolenItems = new();
        public float ReclaimAccum = 0f;
        public float MithrixEffCoeff = 2.65f;
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
            _steerSide = (NetId % 2 == 0) ? 1f : -1f;
            _strafeDir = (NetId % 2 == 0) ? 1f : -1f;
            _strafeSwitchTimer = 1.8f + (float)_rng.NextDouble() * 2.0f;
            AttackTimer = 0.8f + (float)_rng.NextDouble() * 0.8f;
            SpecialTimer = 3.5f + (float)_rng.NextDouble() * 2.0f;
            SecondaryTimer = 1.4f + (float)_rng.NextDouble() * 1.4f;

            Parts = EnemyAndBossBuilder.BuildEnemyMesh(this, enemyType);

            (float hpB, float dmgB, float spdB, int goldB, float hh, float hr) = enemyType switch
            {
                "wisp" => (46f, 14.5f, 5.0f, 18, 2.3f, 1.25f),
                "beetle" => (52f, 10.5f, 8.0f, 14, 0.7f, 1.10f),
                "jellyfish" => (42f, 18.0f, 8.6f, 17, 2.2f, 1.25f),
                "bison" => (155f, 20.0f, 7.6f, 30, 1.3f, 1.50f),
                "brass_contraption" => (145f, 22.0f, 4.8f, 32, 2.4f, 1.45f),
                "golem" => (195f, 24.0f, 4.2f, 36, 1.8f, 1.55f),
                "imp" => (95f, 17.5f, 7.8f, 28, 1.3f, 1.20f),
                "greater_wisp" => (175f, 21.5f, 4.4f, 40, 2.7f, 1.55f),
                "parent" => (240f, 27.0f, 6.2f, 48, 2.3f, 1.75f),
                "elder_lemurian" => (290f, 26.0f, 5.8f, 54, 2.2f, 1.80f),
                "lunar_chimera" => (215f, 23.0f, 6.5f, 45, 2.5f, 1.45f),
                "boss" => (2100f, 32.0f, 4.2f, 185, 2.8f, 2.70f),
                "vagrant" => (2250f, 30.0f, 4.4f, 195, 3.8f, 2.90f),
                "overlord" => (2400f, 34.0f, 5.8f, 210, 2.9f, 2.75f),
                "magma_worm" => (2600f, 35.0f, 7.4f, 225, 3.2f, 2.95f),
                "dunestrider" => (2350f, 31.0f, 4.5f, 205, 3.3f, 2.85f),
                "solus_control" => (2300f, 32.0f, 5.0f, 205, 3.8f, 2.80f),
                "mithrix" => (3600f, 38.0f, 8.8f, 650, 2.8f, 2.55f),
                _ => (65f, 12.0f, 6.8f, 16, 1.1f, 1.15f)
            };

            int itemCnt = 0;
            if (WorldContext.Game != null)
            {
                foreach (var v in WorldContext.Game.Items.Values) itemCnt += v;
            }

            if (enemyType == "mithrix")
            {
                MithrixEffCoeff = MathF.Max(coeff, 2.75f) * (1.0f + 0.065f * itemCnt);
                MaxHp = hpB * MithrixEffCoeff;
                Damage = dmgB * MathF.Max(coeff, 2.15f);
            }
            else if (IsBoss)
            {
                float bossItemMult = 1.0f + 0.04f * itemCnt;
                MaxHp = hpB * coeff * bossItemMult;
                Damage = dmgB * coeff;
            }
            else
            {
                MaxHp = hpB * coeff;
                Damage = dmgB * coeff;
                if (GameData.HasArtifact("swarm"))
                    MaxHp *= 0.50f;
            }

            Speed = spdB * GameData.GetActiveDifficulty().EnemySpeedMult;
            GoldReward = (int)MathF.Ceiling(goldB * MathF.Pow(coeff, 1.32f));
            HitHeight = hh;
            HitRadius = hr;

            float barY = IsBoss ? 6.0f : (enemyType is "golem" or "greater_wisp" or "lunar_chimera" or "parent" or "elder_lemurian" or "brass_contraption" ? 3.8f : 2.5f);

            if (GameData.HasArtifact("evolution") && enemyType != "mithrix")
            {
                int curStage = WorldContext.Game is RiskOfCSharp2.GameRunContext grc ? grc.Stage : 1;
                MaxHp *= 1.0f + Math.Min(0.45f, 0.08f * curStage);
                Damage *= 1.0f + Math.Min(0.50f, 0.09f * curStage);
                Speed *= 1.0f + Math.Min(0.40f, 0.07f * curStage);
            }

            if (GameData.HasArtifact("honor") && enemyType != "mithrix")
            {
                IsElite = true;
                MaxHp *= 1.35f;
                Damage *= 1.25f;
                GoldReward = (int)MathF.Ceiling(GoldReward * 1.40f);
                new Entity(parent: this, model: "diamond", scale: new(HitRadius * 1.35f, 0.16f, HitRadius * 1.35f),
                    position: new(0, barY - 0.38f, 0), color: Rgb(255, 165, 35), unlit: true);
            }

            Hp = MaxHp;
            LabelEnt = new Entity(parent: this, position: new(0, barY, 0), color: Color.Red);
            UpdateHpBillboard();
        }

        public void MakeDoppelganger()
        {
            if (IsDoppelganger) return;
            IsDoppelganger = true;
            IsElite = true;
            MaxHp *= 2.2f;
            Hp = MaxHp;
            Damage *= 1.35f;
            Speed *= 1.25f;
            GoldReward = (int)MathF.Ceiling(GoldReward * 2.5f);
            new Entity(parent: this, model: "diamond", scale: new(HitRadius * 1.75f, 0.20f, HitRadius * 1.75f),
                position: new(0, 0.12f, 0), color: Rgb(215, 75, 255), unlit: true);
            UpdateHpBillboard();
        }

        public void ClearWindup()
        {
            WindupType = 0;
            WindupSubType = 0;
            WindupTimer = 0f;
            WindupTag = "";
            if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                Destroy(WindupIndicator);
            WindupIndicator = null;
        }

        public void UpdateHpBillboard()
        {
            if (IsDestroyed) return;
            int bars = Math.Clamp((int)MathF.Ceiling((Hp / MathF.Max(1f, MaxHp)) * 10f), 0, 10);
            string barStr = new string('█', bars) + new string('░', 10 - bars);
            string prefix = IsAlly ? "[СОЮЗНИК МЕДИКА ✓]\n"
                : (CharmTimer > 0f ? "[♥ ОЧАРОВАН ♥]\n"
                : (IsDoppelganger ? "★ ТЕНЕВОЙ ДОППЕЛЬГАНГЕР ★\n"
                : (IsElite ? "[ЭЛИТНЫЙ] " : "")));
            string warnLine = (!string.IsNullOrEmpty(WindupTag) && WindupType > 0) ? $"⚠ {WindupTag} ⚠\n" : "";
            string phaseTag = EnemyType == "mithrix" ? $" [Ф{Phase}/4]" : "";
            string displayName = GameData.MonsterLog.TryGetValue(EnemyType, out var mLog)
                ? mLog.Name.ToUpper()
                : EnemyType.ToUpper();
            LabelEnt.Color = IsAlly ? Color.Lime
                : (CharmTimer > 0f ? Rgb(255, 115, 205)
                : (IsDoppelganger ? Rgb(215, 75, 255)
                : (WindupType > 0 ? Color.Yellow
                : (EnemyType == "mithrix" ? Color.SkyBlue : (IsElite ? Rgb(255, 165, 45) : Color.Red)))));
            LabelEnt.BillboardText = $"{prefix}{warnLine}{displayName}{phaseTag} [{(int)MathF.Max(0, Hp)}/{(int)MaxHp}]\n{barStr}";
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
            ClearWindup();
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

            ClearWindup();
            RecentDamageTaken = 0f;
            Phase++;
            PhaseTransitionTimer = 2.2f;
            SkyLeapState = 0;
            if (TelegraphRing != null && !TelegraphRing.IsDestroyed)
            {
                Destroy(TelegraphRing);
                TelegraphRing = null;
            }

            if (Phase == 2)
            {
                MaxHp = 3200.0f * MithrixEffCoeff;
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
                MaxHp = 3500.0f * MithrixEffCoeff;
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
                MaxHp = 2100.0f * MathF.Max(Coeff, 2.2f);
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

            if (CharmTimer > 0f && !fromNetwork)
                amount *= 1.25f;

            var g = WorldContext.Game;
            if (IsBoss && !fromNetwork)
            {
                // 1. Ограничение максимального урона с одного удара (защита от ваншотов)
                float maxSingleHit = EnemyType == "mithrix" ? MaxHp * 0.075f : MaxHp * 0.12f;
                amount = MathF.Min(amount, maxSingleHit);

                // 2. Стойкость во время замаха спец-атаки (30% снижение урона)
                if (WindupType > 0)
                    amount *= 0.70f;

                // 3. Адаптивная реактивная броня против закликивания всеми скиллами за секунду
                float burstRatio = RecentDamageTaken / MathF.Max(1f, MaxHp);
                if (burstRatio > 0.16f)
                {
                    float armorMult = Math.Clamp(1.0f - (burstRatio - 0.16f) * 1.45f, 0.42f, 1.0f);
                    amount *= armorMult;
                }
                RecentDamageTaken += amount;
                RecentDamageTimer = 1.6f;
            }

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
            ClearWindup();
            if (!fromNetwork)
                WorldContext.Game?.NotifyEnemyKilled(NetId);
            if (TelegraphRing != null && !TelegraphRing.IsDestroyed)
                Destroy(TelegraphRing);
            if (VagrantNovaSphere != null && !VagrantNovaSphere.IsDestroyed)
                Destroy(VagrantNovaSphere);
            CombatManager.OnEnemyKilled(this);
            Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || !IsValid(this)) return;

            ArmorBreakTimer = MathF.Max(0f, ArmorBreakTimer - dt);
            if (RecentDamageTimer > 0f)
            {
                RecentDamageTimer -= dt;
                if (RecentDamageTimer <= 0f) RecentDamageTaken = 0f;
            }
            else if (RecentDamageTaken > 0f)
            {
                RecentDamageTaken = MathF.Max(0f, RecentDamageTaken - MaxHp * 0.20f * dt);
            }

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

            if (LiandryBurnTimer > 0f)
            {
                LiandryBurnTimer -= dt;
                LiandryBurnTick -= dt;
                if (LiandryBurnTick <= 0f)
                {
                    LiandryBurnTick = 0.45f;
                    new VisualFX(Position + new Vector3(0, HitHeight, 0), Rgb(135, 225, 255), 1.35f, 0.20f);
                    TakeDamage(LiandryBurnDps * 0.45f);
                    if (!IsValid(this)) return;
                }
            }

            if (CharmTimer > 0f)
            {
                CharmTimer -= dt;
                if (!IsBoss && WindupType > 0)
                    ClearWindup();
                UpdateHpBillboard();
                if (!IsBoss)
                {
                    Vector3 charmGoal = g.PlayerPos;
                    LookAt2D(charmGoal);
                    float charmFloor = Physics.GetSurfaceHeight(X, Z, Y);
                    if (Physics.Distance2D(Position, charmGoal) > 2.4f)
                    {
                        Position += Forward * (Speed * 0.42f) * dt;
                        Vector3 res = Physics.ResolveHorizontalCollision(Position, 0.8f, charmFloor);
                        Position = new Vector3(res.X, charmFloor, res.Z);
                        float cWalk = MathF.Sin((float)Raylib.GetTime() * 6f) * 18f;
                        if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = cWalk;
                        if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -cWalk;
                    }
                    return;
                }
            }

            if (StunTimer > 0f)
            {
                StunTimer -= dt;
                if (!IsBoss && WindupType > 0)
                {
                    ClearWindup();
                    UpdateHpBillboard();
                }
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
                bool localUntargetable = !g.CanPlayerInteract;
                float bestTargetDist = (targetIsLocal && localUntargetable) ? 9999f : Vector3.Distance(Position, pPos);

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

                if (bestTargetDist >= 9990f)
                    return;
            }

            float prevY = Y;
            JumpCooldown = MathF.Max(0f, JumpCooldown - dt);
            SecondaryTimer = MathF.Max(0f, SecondaryTimer - dt);
            _strafeSwitchTimer -= dt;
            if (_strafeSwitchTimer <= 0f)
            {
                _strafeSwitchTimer = 2.0f + (float)_rng.NextDouble() * 2.2f;
                if (_rng.NextDouble() < 0.65) _strafeDir = -_strafeDir;
            }

            float dist = Physics.Distance2D(Position, pPos);
            float heightDiff = MathF.Abs(pPos.Y - Y);
            float signedHeightDiff = pPos.Y - Y;
            float localDist = Physics.Distance2D(Position, g.PlayerPos);
            float localHeightDiff = MathF.Abs(g.PlayerPos.Y - Y);
            float floorY = Physics.GetSurfaceHeight(X, Z, Y, prevY);
            bool projIsPlayer = IsAlly;

            if (!IsAlly && HallowedMistZone.IsHiddenFromOutside(pPos, Position))
            {
                AttackTimer = MathF.Max(AttackTimer, 0.45f);
                SpecialTimer = MathF.Max(SpecialTimer, 0.45f);
            }

            void TriggerJump(float heightGain, Vector3 horizBoost, float cd = 1.05f)
            {
                float neededH = Math.Clamp(heightGain + 1.05f, 2.2f, 11.5f);
                VelY = MathF.Sqrt(2.0f * 26.0f * neededH);
                IsGrounded = false;
                JumpCooldown = cd;
                JumpHorizVel = new Vector3(horizBoost.X, 0f, horizBoost.Z);
            }

            void ApplyVerticalPhysics(float colRadius)
            {
                if (!IsGrounded || Y > floorY + 0.08f || VelY > 0.01f)
                {
                    VelY -= 26.0f * dt;
                    Y += VelY * dt;
                    if (JumpHorizVel.LengthSquared() > 0.01f)
                    {
                        Position += JumpHorizVel * dt;
                        JumpHorizVel = Lerp(JumpHorizVel, Vector3.Zero, dt * 3.5f);
                    }
                    floorY = Physics.GetSurfaceHeight(X, Z, Y, prevY);
                    if (VelY <= 0f && Y <= floorY + 0.05f)
                    {
                        Y = floorY;
                        VelY = 0f;
                        IsGrounded = true;
                        JumpHorizVel = Vector3.Zero;
                    }
                }
                else
                {
                    Y = floorY;
                    VelY = 0f;
                    IsGrounded = true;
                }

                // Во время взлёта ослабляем боковое выталкивание у верхушки платформы, чтобы запрыгнуть на край
                float effCheckY = (!IsGrounded && VelY > 0f) ? Y + 0.95f : Y;
                Vector3 res = Physics.ResolveHorizontalCollision(Position, colRadius, effCheckY);
                float wb = Physics.WorldBound - 1.5f;
                Position = new Vector3(Math.Clamp(res.X, -wb, wb), Y, Math.Clamp(res.Z, -wb, wb));
            }

            Vector3 GetFlockSeparation(float sepRadius = 2.2f)
            {
                Vector3 sep = Vector3.Zero;
                foreach (var other in CombatManager.Enemies)
                {
                    if (other == this || !IsValid(other) || other.IsAlly != IsAlly) continue;
                    float dx = X - other.X, dz = Z - other.Z;
                    float d2 = dx * dx + dz * dz;
                    float minSep = sepRadius + other.HitRadius * 0.45f;
                    if (d2 > 0.0001f && d2 < minSep * minSep)
                    {
                        float d = MathF.Sqrt(d2);
                        sep += new Vector3(dx / d, 0f, dz / d) * ((minSep - d) / minSep);
                    }
                }
                return sep;
            }

            void MoveSmartGround(Vector3 goalPos, float moveSpd, float colRadius, float strafeFactor = 0f, float maxJumpH = 6.2f, bool lookAtGoal = true)
            {
                Vector3 navGoal = goalPos;
                // Если цель находится выше на платформе — ищем ближайшую ступень или край платформы для прыжка
                if (maxJumpH > 0.5f && (goalPos.Y - Y) > 1.05f)
                {
                    if (Physics.TryFindClimbTarget(Position, goalPos, maxJumpH, out Vector3 climbPos, out float ledgeY, out bool jumpNow))
                    {
                        navGoal = climbPos;
                        if (jumpNow && IsGrounded && JumpCooldown <= 0f)
                        {
                            Vector3 toP = new(climbPos.X - X, 0f, climbPos.Z - Z);
                            Vector3 jDir = toP.LengthSquared() > 0.01f ? Vector3.Normalize(toP) : Forward;
                            TriggerJump(ledgeY - Y, jDir * MathF.Max(moveSpd * 1.15f, 6.8f));
                        }
                    }
                }

                Vector3 toNav = new(navGoal.X - X, 0f, navGoal.Z - Z);
                Vector3 desiredDir = toNav.LengthSquared() > 0.001f ? Vector3.Normalize(toNav) : Forward;

                if (MathF.Abs(strafeFactor) > 0.01f && IsGrounded)
                {
                    Vector3 perp = new(-desiredDir.Z * _strafeDir, 0f, desiredDir.X * _strafeDir);
                    desiredDir = Vector3.Normalize(desiredDir + perp * strafeFactor);
                }

                Vector3 steerDir = Physics.ComputeSmartSteerDirection(Position, desiredDir, colRadius, Y, ref _steerSide);
                Vector3 sep = GetFlockSeparation(colRadius * 2.1f);
                Vector3 finalDir = steerDir + sep * 0.55f;
                if (finalDir.LengthSquared() > 0.001f)
                    finalDir = Vector3.Normalize(finalDir);

                // Если впереди невысокое препятствие/уступ — перепрыгиваем его
                if (maxJumpH > 0.5f && IsGrounded && JumpCooldown <= 0f &&
                    Physics.IsPathBlocked2D(Position, finalDir, colRadius + 1.15f, colRadius * 0.8f, Y, 0.45f) &&
                    !Physics.IsPathBlocked2D(Position, finalDir, colRadius + 1.45f, colRadius * 0.8f, Y + maxJumpH * 0.75f, 0.45f))
                {
                    TriggerJump(2.6f, finalDir * (moveSpd * 0.9f), 1.2f);
                }

                Position += finalDir * moveSpd * dt;
                ApplyVerticalPhysics(colRadius);

                if (lookAtGoal)
                    LookAt2D(WindupType == 0 ? goalPos : Position + finalDir * 4f);
            }

            void MoveSmartFlying(Vector3 goalPos, float optDist, float heightOffset, float moveSpd, float orbitStrength = 0.65f)
            {
                Vector3 toGoal2D = new(goalPos.X - X, 0f, goalPos.Z - Z);
                float d2D = toGoal2D.Length();
                Vector3 dirToGoal = d2D > 0.01f ? toGoal2D / d2D : Forward;

                float radialDrive = 0f;
                if (d2D > optDist + 2.0f) radialDrive = 1.0f;
                else if (d2D < optDist - 2.5f) radialDrive = -0.85f; // Кайт назад при сближении игрока

                Vector3 orbitPerp = new(-dirToGoal.Z * _strafeDir, 0f, dirToGoal.X * _strafeDir);
                Vector3 horizMove = dirToGoal * radialDrive + orbitPerp * orbitStrength + GetFlockSeparation(2.6f) * 0.6f;
                if (horizMove.LengthSquared() > 0.001f)
                    horizMove = Vector3.Normalize(horizMove);

                Vector3 steered = Physics.ComputeSmartSteerDirection(Position, horizMove, HitRadius * 0.7f, Y, ref _steerSide);
                Position += steered * moveSpd * dt;

                // Если прямая видимость перекрыта платформой — взлетаем выше края платформы
                bool hasLos = Physics.HasLineOfSight(Position + new Vector3(0, HitHeight * 0.8f, 0), goalPos + new Vector3(0, 1.2f, 0));
                float desiredY = MathF.Max(floorY + 1.2f, goalPos.Y + heightOffset + (hasLos ? 0f : 3.8f) + MathF.Sin(t * 2.5f) * 0.45f);
                Y = Lerp(Y, desiredY, dt * 3.2f);

                float wb = Physics.WorldBound - 2.0f;
                X = Math.Clamp(X, -wb, wb);
                Z = Math.Clamp(Z, -wb, wb);
                LookAt2D(goalPos);
            }

            if (WindupType == 0 || EnemyType is "golem" or "solus_control")
                LookAt2D(pPos);

            void ApplyEnemyMeleeDamage(float dmgVal)
            {
                if (IsAlly && IsValid(allyEnemyTarget)) allyEnemyTarget!.TakeDamage(dmgVal);
                else if (!IsAlly)
                {
                    if (IsValid(structTarget)) structTarget!.TakeStructureDamage(dmgVal);
                    else if (IsValid(cloneTarget)) { cloneTarget!.Hp -= dmgVal; if (cloneTarget.Hp <= 0f) Destroy(cloneTarget); }
                    else if (IsValid(allyEnemyTarget)) allyEnemyTarget!.TakeDamage(dmgVal);
                    else if (g.CanPlayerInteract && (targetIsLocal || (localDist <= 2.4f && localHeightDiff <= 2.8f)))
                        CombatManager.DamagePlayer(dmgVal, this);
                }
            }

            if (IsAlly && allyEnemyTarget == null)
            {
                if (dist > 6.5f)
                    MoveSmartGround(pPos, effSpeed, 0.75f, strafeFactor: 0.15f, maxJumpH: 6.0f);
                else
                    ApplyVerticalPhysics(0.75f);
                return;
            }

            if (EnemyType == "lemurian")
            {
                if (Parts.Tail != null)
                {
                    Parts.Tail.RotationY = MathF.Sin(t * 6f) * 24f;
                    Parts.Tail.RotationX = MathF.Cos(t * 3f) * 8f;
                }
                if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, 0f, dt * 8f);
                if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, 0f, dt * 8f);

                // На средней дистанции (6.5–22м) Лемуриец плюётся огненным шаром с небольшим упреждением
                if (SecondaryTimer <= 0f && dist is >= 6.5f and <= 22.0f &&
                    Physics.HasLineOfSight(Position + new Vector3(0, 1.4f, 0), pPos + new Vector3(0, 1.1f, 0)))
                {
                    SecondaryTimer = 3.2f + (float)_rng.NextDouble() * 1.2f;
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -55f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -55f;
                    Vector3 sp = Position + new Vector3(0, 1.45f, 0) + Forward * 0.7f;
                    Vector3 aim = (pPos + new Vector3(0, 1.0f, 0)) - sp;
                    new Projectile(sp, aim, 26f, Damage * 0.85f, 0.34f, IsAlly ? Color.Lime : Rgb(255, 110, 30), isPlayer: projIsPlayer, canProc: false);
                }

                // Стайный заход с фланга и прыжок-выпад вблизи
                if (dist is >= 3.5f and <= 6.2f && IsGrounded && JumpCooldown <= 0f && MathF.Abs(signedHeightDiff) <= 3.5f)
                {
                    TriggerJump(MathF.Max(1.4f, signedHeightDiff + 1.2f), Forward * (effSpeed * 1.45f), 2.4f);
                }

                if (dist > 1.75f || heightDiff > 2.2f)
                {
                    float flankStrafe = dist is > 3.5f and < 14.0f ? 0.42f : 0.12f;
                    MoveSmartGround(pPos, effSpeed, 0.7f, strafeFactor: flankStrafe, maxJumpH: 6.5f);
                    float swing = MathF.Sin(t * 11f) * 36f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = swing;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -swing;
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -swing * 0.45f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = swing * 0.45f;
                    if (Parts.Torso != null)
                    {
                        Parts.Torso.Y = 1.15f + MathF.Abs(MathF.Cos(t * 11f)) * 0.08f;
                        Parts.Torso.RotationZ = MathF.Sin(t * 11f) * 5.5f;
                    }
                }
                else
                {
                    ApplyVerticalPhysics(0.7f);
                    AttackTimer -= dt;
                    if (AttackTimer <= 0.30f && AttackTimer > 0f)
                    {
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -75f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = -75f;
                    }
                    if (AttackTimer <= 0f)
                    {
                        AttackTimer = 1.05f;
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = 35f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = 35f;
                        ApplyEnemyMeleeDamage(Damage);
                        new VisualFX(pPos + new Vector3(0, 1f, 0), IsAlly ? Color.Lime : Color.Red, 1.6f, 0.2f);
                    }
                }
            }
            else if (EnemyType == "beetle")
            {
                // Жук бежит зигзагом и совершает прыжок-наскок с дистанции 3.5–7.5м
                if (dist is >= 3.5f and <= 7.5f && IsGrounded && JumpCooldown <= 0f && signedHeightDiff >= -1.0f && signedHeightDiff <= 4.5f)
                {
                    TriggerJump(MathF.Max(1.6f, signedHeightDiff + 1.2f), Forward * (effSpeed * 1.65f), 2.1f);
                    if (Parts.Torso != null) Parts.Torso.RotationX = -20f;
                }

                if (dist > 1.65f || heightDiff > 2.1f)
                {
                    float zigzag = MathF.Sin(t * 6.5f) * 0.58f;
                    MoveSmartGround(pPos, effSpeed * (!IsGrounded ? 1.2f : 1.0f), 0.65f, strafeFactor: zigzag, maxJumpH: 6.0f);
                    float scurry = MathF.Sin(t * 16f) * 30f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = scurry;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -scurry;
                    if (Parts.Torso != null)
                    {
                        Parts.Torso.Y = 0.65f + MathF.Abs(MathF.Cos(t * 16f)) * 0.06f;
                        Parts.Torso.RotationZ = MathF.Sin(t * 16f) * 6.0f;
                        Parts.Torso.RotationX = Lerp(Parts.Torso.RotationX, 0f, dt * 8f);
                    }
                }
                else
                {
                    ApplyVerticalPhysics(0.65f);
                    AttackTimer -= dt;
                    if (Parts.Torso != null && AttackTimer <= 0.28f && AttackTimer > 0f)
                        Parts.Torso.RotationX = -22f;
                    if (AttackTimer <= 0f)
                    {
                        AttackTimer = 0.90f;
                        if (Parts.Torso != null) Parts.Torso.RotationX = 24f;
                        ApplyEnemyMeleeDamage(Damage);
                        new VisualFX(pPos + new Vector3(0, 0.8f, 0), Color.Orange, 1.5f, 0.2f);
                    }
                }
            }
            else if (EnemyType == "bison")
            {
                if (Parts.Tail != null) Parts.Tail.RotationY = MathF.Sin(t * 8f) * 22f;
                if (WindupType == 1)
                {
                    ApplyVerticalPhysics(0.95f);
                    WindupTimer -= dt;
                    LookAt2D(Lerp(Position + Forward * 5f, pPos, dt * 4.5f));
                    if (Parts.Crown != null) Parts.Crown.RotationX = 35f;
                    if (Parts.Torso != null) Parts.Torso.Y = 1.05f + MathF.Sin(t * 28f) * 0.06f;
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        WindupType = 2; // Состояние стремительного тарана
                        WindupTimer = 0.62f;
                        UpdateHpBillboard();
                    }
                    return;
                }
                if (WindupType == 2)
                {
                    WindupTimer -= dt;
                    // Если Бизон на полном ходу врезается в колонну — ударная волна и короткое оглушение
                    if (Physics.IsPathBlocked2D(Position, Forward, 1.35f, 0.85f, Y, 0.6f))
                    {
                        new VisualFX(Position + Forward * 1.0f + new Vector3(0, 1.0f, 0), Color.Orange, 3.8f, 0.28f);
                        ClearWindup();
                        StunTimer = 0.9f;
                        AttackTimer = 2.4f;
                        UpdateHpBillboard();
                        return;
                    }

                    Position += Forward * (effSpeed * 2.65f) * dt;
                    ApplyVerticalPhysics(0.95f);
                    float chargeLegs = MathF.Sin(t * 22f) * 44f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = chargeLegs;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -chargeLegs;
                    if (dist <= 2.4f && heightDiff <= 2.5f)
                    {
                        ApplyEnemyMeleeDamage(Damage * 1.45f);
                        new VisualFX(Position + Forward * 1.2f + new Vector3(0, 1.0f, 0), Color.SkyBlue, 3.2f, 0.25f);
                        ClearWindup();
                        AttackTimer = 2.6f;
                        UpdateHpBillboard();
                        return;
                    }
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        if (Parts.Crown != null) Parts.Crown.RotationX = 0f;
                        AttackTimer = 2.2f;
                        UpdateHpBillboard();
                    }
                    return;
                }

                if (Parts.Crown != null) Parts.Crown.RotationX = Lerp(Parts.Crown.RotationX, 0f, dt * 8f);
                if (dist > 2.2f || heightDiff > 2.2f)
                {
                    MoveSmartGround(pPos, effSpeed, 0.9f, strafeFactor: 0.15f, maxJumpH: 6.0f);
                    float galop = MathF.Sin(t * 10f) * 28f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = galop;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -galop;
                    if (Parts.Torso != null) Parts.Torso.RotationX = MathF.Cos(t * 10f) * 5.5f;
                }
                else ApplyVerticalPhysics(0.9f);

                AttackTimer -= dt;
                if (AttackTimer <= 0f && dist <= 19.0f && heightDiff <= 2.8f && IsGrounded &&
                    Physics.HasLineOfSight(Position + new Vector3(0, 1.2f, 0), pPos + new Vector3(0, 1.2f, 0)))
                {
                    WindupType = 1;
                    WindupTimer = 0.65f;
                    WindupTag = "ТАРАННЫЙ РАЗГОН!";
                    LookAt2D(pPos);
                    new VisualFX(Position + new Vector3(0, 1.2f, 0), Color.SkyBlue, 2.5f, 0.25f);
                    UpdateHpBillboard();
                }
            }
            else if (EnemyType == "brass_contraption")
            {
                if (Parts.Ring1 != null) Parts.Ring1.RotationY += (WindupType > 0 ? 360f : 110f) * dt;
                if (Parts.Ring2 != null) Parts.Ring2.RotationX += (WindupType > 0 ? 280f : 75f) * dt;
                if (Parts.Core != null) Parts.Core.Y = 2.4f + MathF.Sin(t * 3.8f) * 0.25f;

                // Кружит по орбите на средней дистанции, набирая высоту над платформами
                MoveSmartFlying(pPos, optDist: 16.0f, heightOffset: 2.2f, moveSpd: WindupType > 0 ? effSpeed * 0.45f : effSpeed, orbitStrength: 0.78f);

                if (WindupType == 1)
                {
                    WindupTimer -= dt;
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 2.6f;
                        Vector3 sp = Position + new Vector3(0, 2.4f, 0);
                        Vector3 baseAim = (pPos + new Vector3(0, 1.1f, 0)) - sp;
                        foreach (float deg in new[] { -10f, 0f, 10f })
                        {
                            float rad = deg * (MathF.PI / 180f);
                            Vector3 d = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                            new Projectile(sp, d, 31f, Damage * 0.85f, 0.55f, Rgb(235, 175, 65), isPlayer: projIsPlayer, canProc: false);
                        }
                    }
                    return;
                }

                AttackTimer -= dt;
                if (AttackTimer <= 0f && dist < 38.0f && Physics.HasLineOfSight(Position + new Vector3(0, 2.4f, 0), pPos + new Vector3(0, 1.2f, 0)))
                {
                    WindupType = 1;
                    WindupTimer = 0.65f;
                    WindupTag = "ЗАЛП КОЛОКОЛА!";
                    new VisualFX(Position + new Vector3(0, 2.4f, 0), Color.Gold, 2.6f, 0.25f);
                    UpdateHpBillboard();
                }
            }
            else if (EnemyType == "parent")
            {
                if (Parts.Crown != null)
                {
                    Parts.Crown.Y = 1.15f + MathF.Sin(t * 3.4f) * 0.10f;
                    Parts.Crown.RotationZ = MathF.Sin(t * 2.2f) * 8f;
                }
                if (WindupType == 1)
                {
                    ApplyVerticalPhysics(1.0f);
                    WindupTimer -= dt;
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, -135f, dt * 10f);
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, -135f, dt * 10f);
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                        WindupIndicator.RotationY += 160f * dt;
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 2.3f;
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = 35f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = 35f;
                        Vector3 slamPos = Position + Forward * 2.0f;
                        new VisualFX(slamPos + new Vector3(0, 0.8f, 0), Color.Gold, 7.5f, 0.35f);
                        if (Physics.Distance2D(pPos, slamPos) <= 5.2f && MathF.Abs(pPos.Y - Y) <= 3.2f)
                            ApplyEnemyMeleeDamage(Damage * 1.45f);
                    }
                    return;
                }

                if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, 0f, dt * 6f);
                if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, 0f, dt * 6f);

                // Если игрок забрался на высокую платформу или отошёл — Прародитель телепортируется прямо на его платформу!
                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && ((dist > 8.5f && dist < 34.0f) || heightDiff > 2.4f))
                {
                    SpecialTimer = 4.8f;
                    new VisualFX(Position + new Vector3(0, 2.0f, 0), Color.Gold, 4.0f, 0.28f);
                    float targetFloor = Physics.GetSurfaceHeight(pPos.X, pPos.Z, pPos.Y + 1.5f);
                    Vector3 tpPos = Physics.ResolveHorizontalCollision(pPos - Forward * 2.8f, 1.1f, targetFloor);
                    Position = new Vector3(tpPos.X, Physics.GetSurfaceHeight(tpPos.X, tpPos.Z, targetFloor + 1.0f), tpPos.Z);
                    VelY = 0f;
                    IsGrounded = true;
                    LookAt2D(pPos);
                    new VisualFX(Position + new Vector3(0, 2.0f, 0), Color.Gold, 4.5f, 0.30f);
                }

                if (dist > 3.6f || heightDiff > 2.4f)
                {
                    MoveSmartGround(pPos, effSpeed, 1.0f, strafeFactor: 0.15f, maxJumpH: 6.8f);
                    float stride = MathF.Sin(t * 7f) * 28f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stride;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stride;
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -stride * 0.45f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = stride * 0.45f;
                }
                else
                {
                    ApplyVerticalPhysics(1.0f);
                    AttackTimer -= dt;
                    if (AttackTimer <= 0f)
                    {
                        WindupType = 1;
                        WindupTimer = 0.78f;
                        WindupTag = "АСТРАЛЬНЫЙ ХЛОПОК!";
                        Vector3 slamPos = Position + Forward * 2.0f;
                        WindupIndicator = new Entity(model: "diamond", position: new(slamPos.X, floorY + 0.12f, slamPos.Z),
                            scale: new(9.5f, 0.16f, 9.5f), color: Rgb(255, 210, 75, 145), unlit: true);
                        UpdateHpBillboard();
                    }
                }
            }
            else if (EnemyType == "elder_lemurian")
            {
                if (Parts.Tail != null) Parts.Tail.RotationY = MathF.Sin(t * 5f) * 20f;
                if (WindupType == 1)
                {
                    ApplyVerticalPhysics(1.1f);
                    WindupTimer -= dt;
                    if (Parts.Torso != null) Parts.Torso.RotationX = -12f;
                    if (Parts.Crown != null) Parts.Crown.RotationX = -32f;
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 2.5f;
                        if (Parts.Torso != null) Parts.Torso.RotationX = 24f;
                        if (Parts.Crown != null) Parts.Crown.RotationX = 10f;
                        Vector3 mouthPos = Position + new Vector3(0, 2.6f, 0) + Forward * 1.1f;
                        Vector3 baseAim = (pPos + new Vector3(0, 1.0f, 0)) - mouthPos;
                        foreach (float deg in new[] { -22f, -11f, 0f, 11f, 22f })
                        {
                            float rad = deg * (MathF.PI / 180f);
                            Vector3 d = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                            new Projectile(mouthPos, d, 28f, Damage * 0.85f, 0.52f, Rgb(255, 115, 25), isPlayer: projIsPlayer, aoe: 2.2f, canProc: false);
                        }
                    }
                    return;
                }

                if (Parts.Torso != null) Parts.Torso.RotationX = Lerp(Parts.Torso.RotationX, 16f, dt * 6f);
                if (Parts.Crown != null) Parts.Crown.RotationX = Lerp(Parts.Crown.RotationX, -16f, dt * 6f);

                // Вблизи Древний Лемуриец бьёт лапой о землю с огненной волной
                if (SecondaryTimer <= 0f && dist <= 4.6f && heightDiff <= 2.8f && IsGrounded)
                {
                    SecondaryTimer = 3.6f;
                    new VisualFX(Position + Forward * 1.4f + new Vector3(0, 0.6f, 0), Color.Orange, 6.2f, 0.32f);
                    ApplyEnemyMeleeDamage(Damage * 1.25f);
                }

                if (dist > 8.0f || heightDiff > 2.5f)
                {
                    MoveSmartGround(pPos, effSpeed, 1.1f, strafeFactor: 0.28f, maxJumpH: 6.5f);
                    float stomp = MathF.Sin(t * 6.5f) * 28f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stomp;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stomp;
                }
                else ApplyVerticalPhysics(1.1f);

                AttackTimer -= dt;
                if (AttackTimer <= 0f && dist < 28.0f && Physics.HasLineOfSight(Position + new Vector3(0, 2.6f, 0), pPos + new Vector3(0, 1.2f, 0)))
                {
                    WindupType = 1;
                    WindupTimer = 0.72f;
                    WindupTag = "ОГНЕННОЕ ДЫХАНИЕ!";
                    new VisualFX(Position + new Vector3(0, 2.8f, 0) + Forward * 1.0f, Color.Orange, 2.8f, 0.28f);
                    UpdateHpBillboard();
                }
            }
            else if (EnemyType == "jellyfish")
            {
                if (Parts.Core != null) Parts.Core.Y = 2.2f + MathF.Sin(t * 5.0f) * 0.3f;
                if (Parts.Tentacles != null) Parts.Tentacles.RotationY += 90f * dt;
                if (dist > 2.2f || heightDiff > 2.8f)
                {
                    // Идёт волнообразной спиралью сверху, а вблизи резко пикирует к цели
                    float diveMult = dist < 7.5f ? 1.45f : 1.0f;
                    float weave = dist < 6.0f ? 0.2f : 0.7f * MathF.Sin(t * 4.2f);
                    MoveSmartFlying(pPos, optDist: 0.5f, heightOffset: dist < 6.0f ? -0.4f : 1.8f, moveSpd: effSpeed * diveMult, orbitStrength: weave);
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

                // Поддерживает дистанцию 14м (отступает, если игрок подходит ближе, и стрейфит вокруг)
                MoveSmartFlying(pPos, optDist: 14.5f, heightOffset: 1.4f, moveSpd: effSpeed, orbitStrength: 0.72f);

                AttackTimer -= dt;
                Vector3 sp = Position + new Vector3(0, 2.3f, 0);
                Vector3 aimPoint = pPos + new Vector3(0, 1.2f, 0);
                if (AttackTimer <= 0f && dist < 36.0f && Physics.HasLineOfSight(sp, aimPoint))
                {
                    AttackTimer = 2.3f;
                    new Projectile(sp, aimPoint - sp, 26f, Damage, 0.35f, IsAlly ? Color.Lime : Color.Orange, isPlayer: projIsPlayer, canProc: false);
                }
            }
            else if (EnemyType == "greater_wisp")
            {
                if (Parts.Ring1 != null) Parts.Ring1.RotationY += (WindupType > 0 ? 320f : 110f) * dt;
                if (Parts.Core != null) Parts.Core.Y = 2.7f + MathF.Sin(t * 3.2f) * 0.3f;

                MoveSmartFlying(pPos, optDist: 17.5f, heightOffset: 2.0f, moveSpd: WindupType > 0 ? effSpeed * 0.4f : effSpeed, orbitStrength: 0.68f);

                if (WindupType == 1)
                {
                    WindupTimer -= dt;
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 2.7f;
                        Vector3 aimPoint = pPos + new Vector3(0, 1.2f, 0);
                        foreach (float sx in new[] { -1.1f, 1.1f })
                        {
                            Vector3 sp = Position + new Vector3(0, 2.7f, 0) + Right * sx;
                            new Projectile(sp, aimPoint - sp, 29f, Damage, 0.52f, Color.Lime, isPlayer: projIsPlayer, aoe: 2.0f, canProc: false);
                        }
                    }
                    return;
                }

                AttackTimer -= dt;
                if (AttackTimer <= 0f && dist < 40.0f && Physics.HasLineOfSight(Position + new Vector3(0, 2.7f, 0), pPos + new Vector3(0, 1.2f, 0)))
                {
                    WindupType = 1;
                    WindupTimer = 0.60f;
                    WindupTag = "ЗАРЯД ЧЕРЕПОВ!";
                    new VisualFX(Position + new Vector3(0, 2.7f, 0), Color.Lime, 2.6f, 0.25f);
                    UpdateHpBillboard();
                }
            }
            else if (EnemyType == "golem")
            {
                Vector3 eyePos = Position + new Vector3(0, 2.7f, 0);
                Vector3 aimPoint = pPos + new Vector3(0, 1.2f, 0);

                if (WindupType == 1)
                {
                    ApplyVerticalPhysics(0.95f);
                    WindupTimer -= dt;
                    LockedAimPos = Lerp(LockedAimPos, aimPoint, dt * 5.5f);
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -55f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -55f;
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                    {
                        float len = MathF.Max(0.2f, Vector3.Distance(eyePos, LockedAimPos));
                        WindupIndicator.Position = (eyePos + LockedAimPos) * 0.5f;
                        WindupIndicator.Scale = new Vector3(0.08f, 0.08f, len);
                        WindupIndicator.LookAt(LockedAimPos);
                    }
                    if (WindupTimer <= 0f)
                    {
                        Vector3 finalAim = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 3.0f;
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = 0f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = 0f;
                        Color col = IsAlly ? Color.Lime : Color.Red;
                        new LightningBeam(eyePos, finalAim, col, 0.32f);
                        new Projectile(eyePos, finalAim - eyePos, 54f, Damage, 0.50f, col, isPlayer: projIsPlayer, aoe: 2.2f, canProc: false);
                    }
                    return;
                }

                // Защита от закруживания вблизи: удар ладонями по земле
                if (SecondaryTimer <= 0f && dist <= 3.8f && heightDiff <= 2.6f)
                {
                    SecondaryTimer = 3.2f;
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = 35f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = 35f;
                    new VisualFX(Position + Forward * 1.3f + new Vector3(0, 0.9f, 0), Color.Red, 4.8f, 0.28f);
                    ApplyEnemyMeleeDamage(Damage * 1.15f);
                }

                if (dist > 10.5f || heightDiff > 2.4f || !Physics.HasLineOfSight(eyePos, aimPoint))
                {
                    MoveSmartGround(pPos, effSpeed, 0.95f, strafeFactor: 0.12f, maxJumpH: 6.2f);
                    float stomp = MathF.Sin(t * 5.5f) * 26f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stomp;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stomp;
                }
                else ApplyVerticalPhysics(0.95f);

                AttackTimer -= dt;
                if (AttackTimer <= 0f && dist < 34.0f && Physics.HasLineOfSight(eyePos, aimPoint))
                {
                    WindupType = 1;
                    WindupTimer = 0.85f;
                    LockedAimPos = aimPoint;
                    WindupTag = "НАВЕДЕНИЕ ЛАЗЕРА!";
                    WindupIndicator = new Entity(model: "cube", position: (eyePos + aimPoint) * 0.5f, scale: new(0.07f, 0.07f, Vector3.Distance(eyePos, aimPoint)), color: Rgb(255, 60, 60, 175), unlit: true);
                    WindupIndicator.LookAt(aimPoint);
                    UpdateHpBillboard();
                }
            }
            else if (EnemyType == "imp")
            {
                if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, 0f, dt * 8f);
                if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, 0f, dt * 8f);

                // Бес телепортируется прямо на платформу к игроку (за спину или во фланг!)
                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && ((dist > 5.0f && dist < 26.0f) || heightDiff > 2.2f))
                {
                    SpecialTimer = 3.8f;
                    new VisualFX(Position + new Vector3(0, 1.2f, 0), Rgb(245, 35, 70), 2.5f, 0.25f);
                    Vector3 behindOffset = targetIsLocal ? -g.PlayerForward * 2.1f : -Forward * 2.1f;
                    Vector3 rawDest = pPos + behindOffset;
                    float destFloor = Physics.GetSurfaceHeight(rawDest.X, rawDest.Z, pPos.Y + 1.5f);
                    Vector3 jumpPos = Physics.ResolveHorizontalCollision(rawDest, 0.7f, destFloor);
                    Position = new Vector3(jumpPos.X, Physics.GetSurfaceHeight(jumpPos.X, jumpPos.Z, destFloor + 0.8f), jumpPos.Z);
                    VelY = 0f;
                    IsGrounded = true;
                    LookAt2D(pPos);
                    new VisualFX(Position + new Vector3(0, 1.2f, 0), Rgb(245, 35, 70), 2.5f, 0.25f);
                }

                if (dist > 1.8f || heightDiff > 2.2f)
                {
                    MoveSmartGround(pPos, effSpeed, 0.7f, strafeFactor: 0.62f, maxJumpH: 6.5f);
                    float swing = MathF.Sin(t * 13f) * 38f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = swing;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -swing;
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -swing * 0.55f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = swing * 0.55f;
                    if (Parts.Torso != null) Parts.Torso.RotationX = 12f + MathF.Sin(t * 13f) * 4f;
                }
                else
                {
                    ApplyVerticalPhysics(0.7f);
                    AttackTimer -= dt;
                    if (AttackTimer <= 0.25f && AttackTimer > 0f)
                    {
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -80f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = -80f;
                    }
                    if (AttackTimer <= 0f)
                    {
                        AttackTimer = 0.92f;
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = 42f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = 42f;
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
            float dt = TimeDelta();
            float prevY = Y;

            void MoveBossSmartGround(float colRadius, float stopDist, float maxClimbH = 7.5f)
            {
                if (dist > stopDist || MathF.Abs(pPos.Y - Y) > 2.8f)
                {
                    Vector3 goal = pPos;
                    if ((pPos.Y - Y) > 1.2f && Physics.TryFindClimbTarget(Position, pPos, maxClimbH, out Vector3 cPos, out float ledgeY, out bool jumpNow))
                    {
                        goal = cPos;
                        if (jumpNow && IsGrounded && JumpCooldown <= 0f)
                        {
                            VelY = MathF.Sqrt(2.0f * 26.0f * Math.Clamp(ledgeY - Y + 1.2f, 3.0f, 11.0f));
                            IsGrounded = false;
                            JumpCooldown = 1.4f;
                        }
                    }
                    Vector3 toG = new(goal.X - X, 0f, goal.Z - Z);
                    Vector3 dir = toG.LengthSquared() > 0.01f ? Vector3.Normalize(toG) : Forward;
                    Vector3 steered = Physics.ComputeSmartSteerDirection(Position, dir, colRadius, Y, ref _steerSide);
                    Position += steered * effSpeed * dt;
                }

                if (!IsGrounded || Y > floorY + 0.08f || VelY > 0.01f)
                {
                    VelY -= 26.0f * dt;
                    Y += VelY * dt;
                    floorY = Physics.GetSurfaceHeight(X, Z, Y, prevY);
                    if (VelY <= 0f && Y <= floorY + 0.05f)
                    {
                        Y = floorY;
                        VelY = 0f;
                        IsGrounded = true;
                    }
                }
                else
                {
                    Y = floorY;
                    VelY = 0f;
                    IsGrounded = true;
                }

                Vector3 res = Physics.ResolveHorizontalCollision(Position, colRadius, (!IsGrounded && VelY > 0f) ? Y + 1.1f : Y);
                float wb = Physics.WorldBound - 2.0f;
                Position = new Vector3(Math.Clamp(res.X, -wb, wb), Y, Math.Clamp(res.Z, -wb, wb));
            }

            if (EnemyType == "boss")
            {
                if (Parts.Crown != null) Parts.Crown.RotationY += (WindupType > 0 ? 240f : 85f) * dt;
                Vector3 corePos = Position + new Vector3(0, 3.4f, 0);

                // Замах 1: Лазерный веер из рунного ядра с лучом-указателем
                if (WindupType == 1)
                {
                    WindupTimer -= dt;
                    LockedAimPos = Lerp(LockedAimPos, pPos + new Vector3(0, 1.2f, 0), dt * 6f);
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, -75f, dt * 8f);
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, -75f, dt * 8f);
                    if (Parts.Torso != null) Parts.Torso.RotationX = Lerp(Parts.Torso.RotationX, -14f, dt * 8f);
                    if (Parts.Core != null) Parts.Core.Scale = new Vector3(1.55f, 1.55f, 0.95f);
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                    {
                        float len = MathF.Max(0.5f, Vector3.Distance(corePos, LockedAimPos));
                        WindupIndicator.Position = (corePos + LockedAimPos) * 0.5f;
                        WindupIndicator.Scale = new Vector3(0.14f, 0.14f, len);
                        WindupIndicator.LookAt(LockedAimPos);
                    }
                    if (WindupTimer <= 0f)
                    {
                        Vector3 aimTarget = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 2.4f;
                        if (Parts.Core != null) Parts.Core.Scale = new Vector3(1.04f, 1.04f, 0.62f);
                        if (Parts.Torso != null) Parts.Torso.RotationX = 12f;
                        Vector3 baseAim = aimTarget - corePos;
                        foreach (float angle in new[] { -16f, -8f, 0f, 8f, 16f })
                        {
                            float rad = angle * (MathF.PI / 180f);
                            Vector3 rotDir = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                            new Projectile(corePos, rotDir, 30f, Damage, 0.65f, IsAlly ? Color.Lime : Color.Red, isPlayer: projIsPlayer, canProc: false);
                        }
                    }
                    return;
                }

                // Замах 2: Вбивание кулаков в землю -> Каменный разлом под игроком
                if (WindupType == 2)
                {
                    WindupTimer -= dt;
                    if (WindupTimer > 0.35f)
                    {
                        LockedAimPos = Lerp(LockedAimPos, new Vector3(pPos.X, Physics.GetSurfaceHeight(pPos.X, pPos.Z, pPos.Y), pPos.Z), dt * 4.5f);
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, -145f, dt * 9f);
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, -145f, dt * 9f);
                    }
                    else
                    {
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, 40f, dt * 16f);
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, 40f, dt * 16f);
                    }
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                    {
                        WindupIndicator.Position = LockedAimPos + new Vector3(0, 0.14f, 0);
                        WindupIndicator.RotationY += 180f * dt;
                    }
                    if (WindupTimer <= 0f)
                    {
                        Vector3 eruptPos = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = 6.2f;
                        new OrbitalStrikeFX(eruptPos, 0f);
                        new VisualFX(eruptPos + new Vector3(0, 1.2f, 0), Color.Red, 10.5f, 0.45f);
                        new LunarShockwaveRing(eruptPos, Damage * 1.15f, 26.0f, 18.0f);
                        if (!IsAlly && Physics.Distance2D(g.PlayerPos, eruptPos) <= 5.8f && MathF.Abs(g.PlayerPos.Y - eruptPos.Y) <= 3.5f)
                            CombatManager.DamagePlayer(Damage * 1.35f, this);
                    }
                    return;
                }

                if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, 0f, dt * 6f);
                if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, 0f, dt * 6f);
                if (Parts.Torso != null) Parts.Torso.RotationX = Lerp(Parts.Torso.RotationX, 0f, dt * 6f);

                MoveBossSmartGround(1.4f, 8.5f);
                if (dist > 8.5f)
                {
                    float stomp = MathF.Sin(t * 4.5f) * 24f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stomp;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stomp;
                }

                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && !IsAlly)
                {
                    WindupType = 2;
                    WindupTimer = 1.15f;
                    LockedAimPos = new Vector3(pPos.X, Physics.GetSurfaceHeight(pPos.X, pPos.Z, pPos.Y), pPos.Z);
                    WindupTag = "КАМЕННЫЙ РАЗЛОМ!";
                    WindupIndicator = new Entity(model: "diamond", position: LockedAimPos + new Vector3(0, 0.14f, 0),
                        scale: new(11.0f, 0.22f, 11.0f), color: Rgb(255, 55, 45, 165), unlit: true);
                    UpdateHpBillboard();
                    return;
                }

                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    WindupType = 1;
                    WindupTimer = 0.85f;
                    LockedAimPos = pPos + new Vector3(0, 1.2f, 0);
                    WindupTag = "ЛАЗЕРНЫЙ ЗАЛП!";
                    WindupIndicator = new Entity(model: "cube", position: (corePos + LockedAimPos) * 0.5f,
                        scale: new(0.14f, 0.14f, Vector3.Distance(corePos, LockedAimPos)), color: Rgb(255, 80, 50, 185), unlit: true);
                    WindupIndicator.LookAt(LockedAimPos);
                    UpdateHpBillboard();
                }
            }
            else if (EnemyType == "vagrant")
            {
                if (Parts.Bell != null) Parts.Bell.Y = 4.2f + MathF.Sin(t * 2.8f) * 0.45f;
                if (Parts.Tentacles != null) Parts.Tentacles.RotationY += (WindupType > 0 || VagrantChargingNova ? 280f : 65f) * dt;
                if (!VagrantChargingNova && WindupType == 0)
                {
                    Vector3 toP = new(pPos.X - X, 0f, pPos.Z - Z);
                    Vector3 dir = toP.LengthSquared() > 0.01f ? Vector3.Normalize(toP) : Forward;
                    Vector3 orbit = new(-dir.Z * _strafeDir, 0f, dir.X * _strafeDir);
                    Vector3 moveVec = (dist > 14.0f ? dir : (dist < 8.5f ? -dir * 0.5f : Vector3.Zero)) + orbit * 0.55f;
                    if (moveVec.LengthSquared() > 0.01f)
                        Position += Physics.ComputeSmartSteerDirection(Position, Vector3.Normalize(moveVec), 1.8f, Y, ref _steerSide) * effSpeed * dt;
                    Y = Lerp(Y, MathF.Max(floorY, pPos.Y), dt * 2.5f);
                }

                // Замах 1: Сжатие купола перед веером энергетических сфер
                if (WindupType == 1)
                {
                    WindupTimer -= dt;
                    if (Parts.Bell != null)
                        Parts.Bell.Scale = Lerp(Parts.Bell.Scale, new Vector3(1.26f, 0.72f, 1.26f), dt * 8f);
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 2.4f;
                        if (Parts.Bell != null) Parts.Bell.Scale = Vector3.One;
                        Vector3 sp = Position + new Vector3(0, 3.8f, 0);
                        Vector3 baseAim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                        foreach (float angle in new[] { -18f, -6f, 6f, 18f })
                        {
                            float rad = angle * (MathF.PI / 180f);
                            Vector3 rotDir = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                            new Projectile(sp, rotDir, 25f, Damage * 0.85f, 0.62f, Color.SkyBlue, isPlayer: projIsPlayer, homing: true, canProc: false);
                        }
                    }
                    return;
                }

                AttackTimer -= dt;
                if (AttackTimer <= 0f && !VagrantChargingNova)
                {
                    WindupType = 1;
                    WindupTimer = 0.75f;
                    WindupTag = "СФЕРЫ МЕДУЗЫ!";
                    new VisualFX(Position + new Vector3(0, 3.8f, 0), Color.SkyBlue, 4.2f, 0.28f);
                    UpdateHpBillboard();
                }

                // Сверхнова Бродяги (<60% HP) с кольцом радиуса поражения на земле и проверкой укрытий
                if (!IsAlly && Hp <= MaxHp * 0.60f)
                {
                    if (!VagrantChargingNova)
                    {
                        SpecialTimer -= dt;
                        if (SpecialTimer <= 0f)
                        {
                            VagrantChargingNova = true;
                            VagrantNovaTimer = 2.4f;
                            WindupType = 2;
                            WindupTag = "СВЕРХНОВА — В УКРЫТИЕ!";
                            VagrantNovaSphere = new Entity(parent: this, model: "sphere", position: new(0, 3.8f, 0),
                                scaleUniform: 1.5f, color: Rgb(255, 215, 65, 95), unlit: true);
                            VagrantNovaSphere.AnimateScale(33.0f, 2.4f);
                            WindupIndicator = new Entity(model: "diamond", position: new(X, floorY + 0.14f, Z),
                                scale: new(33.0f, 0.22f, 33.0f), color: Rgb(255, 195, 50, 120), unlit: true);
                            UpdateHpBillboard();
                        }
                    }
                    else
                    {
                        VagrantNovaTimer -= dt;
                        if (Parts.Bell != null)
                        {
                            float pulse = 1.0f + MathF.Sin(t * 22f) * 0.18f;
                            Parts.Bell.Scale = new Vector3(pulse, pulse, pulse);
                        }
                        if (VagrantNovaSphere != null && !VagrantNovaSphere.IsDestroyed)
                        {
                            byte alpha = (byte)(90 + (int)(MathF.Abs(MathF.Sin((float)Raylib.GetTime() * 14f)) * 95f));
                            VagrantNovaSphere.Color = Rgb(255, 195, 50, alpha);
                        }
                        if (VagrantNovaTimer <= 0f)
                        {
                            VagrantChargingNova = false;
                            SpecialTimer = 9.5f;
                            if (Parts.Bell != null) Parts.Bell.Scale = Vector3.One;
                            if (VagrantNovaSphere != null && !VagrantNovaSphere.IsDestroyed)
                                Destroy(VagrantNovaSphere);
                            VagrantNovaSphere = null;
                            ClearWindup();
                            UpdateHpBillboard();

                            Vector3 novaOrigin = Position + new Vector3(0, 3.8f, 0);
                            new VisualFX(novaOrigin, Color.Yellow, 33.0f, 0.55f);
                            Vector3 playerEye = g.PlayerPos + new Vector3(0, 1.1f, 0);
                            if (Vector3.Distance(novaOrigin, playerEye) <= 16.5f && Physics.HasLineOfSight(novaOrigin, playerEye))
                            {
                                if (CombatManager.Controller == null || !CombatManager.Controller.TryParryExternalAttack(Damage * 1.85f, novaOrigin))
                                    CombatManager.DamagePlayer(Damage * 1.85f, this);
                            }
                        }
                    }
                }
            }
            else if (EnemyType == "overlord")
            {
                if (Parts.Crown != null) Parts.Crown.RotationY -= (WindupType > 0 ? 260f : 95f) * dt;

                // Замах 1: Отведение когтистых рук назад перед веером шипов Бездны
                if (WindupType == 1)
                {
                    WindupTimer -= dt;
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, -85f, dt * 10f);
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, 65f, dt * 10f);
                    if (Parts.Torso != null) Parts.Torso.RotationY = Lerp(Parts.Torso.RotationY, 28f, dt * 10f);
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 2.3f;
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = 40f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = -95f;
                        if (Parts.Torso != null) Parts.Torso.RotationY = -22f;
                        Vector3 sp = Position + new Vector3(0, 3.2f, 0);
                        Vector3 baseAim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                        foreach (float angle in new[] { -20f, -10f, 0f, 10f, 20f })
                        {
                            float rad = angle * (MathF.PI / 180f);
                            Vector3 rotDir = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                            new Projectile(sp, rotDir, 33f, Damage * 0.82f, 0.55f, Rgb(245, 30, 65), isPlayer: projIsPlayer, canProc: false);
                        }
                    }
                    return;
                }

                // Замах 2: Телеграф прыжка Бездны (багровый круг на месте приземления заранее!)
                if (WindupType == 2)
                {
                    WindupTimer -= dt;
                    if (Parts.Torso != null) Parts.Torso.Y = Lerp(Parts.Torso.Y, 2.2f, dt * 8f);
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationZ = -40f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationZ = 40f;
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                        WindupIndicator.RotationY += 220f * dt;
                    if (WindupTimer <= 0f)
                    {
                        Vector3 landPos = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = 6.0f;
                        if (Parts.Torso != null) Parts.Torso.Y = 3.1f;
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationZ = 0f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationZ = 0f;
                        new VisualFX(Position + new Vector3(0, 2.5f, 0), Rgb(245, 30, 65), 6.0f, 0.35f);
                        Position = landPos;
                        VelY = 0f;
                        IsGrounded = true;
                        new VisualFX(Position + new Vector3(0, 1.5f, 0), Rgb(245, 30, 65), 11.0f, 0.42f);
                        if (!IsAlly && Physics.Distance2D(g.PlayerPos, Position) <= 6.5f && MathF.Abs(g.PlayerPos.Y - Y) <= 3.5f)
                            CombatManager.DamagePlayer(Damage * 1.40f, this);
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = (i / 8.0f) * MathF.PI * 2f;
                            new Projectile(Position + new Vector3(0, 2.0f, 0), new Vector3(MathF.Cos(ang), 0.05f, MathF.Sin(ang)), 28f, Damage * 0.75f, 0.48f, Rgb(245, 30, 65), isPlayer: projIsPlayer, canProc: false);
                        }
                    }
                    return;
                }

                if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, 0f, dt * 6f);
                if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, 0f, dt * 6f);
                if (Parts.Torso != null) Parts.Torso.RotationY = Lerp(Parts.Torso.RotationY, 0f, dt * 6f);

                MoveBossSmartGround(1.3f, 5.5f);
                if (dist > 5.5f)
                {
                    float stomp = MathF.Sin(t * 6.0f) * 28f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stomp;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stomp;
                }
                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    WindupType = 1;
                    WindupTimer = 0.75f;
                    WindupTag = "ВЕЕР ШИПОВ БЕЗДНЫ!";
                    new VisualFX(Position + new Vector3(0, 3.2f, 0), Rgb(245, 30, 65), 3.0f, 0.25f);
                    UpdateHpBillboard();
                    return;
                }
                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && (dist > 6.5f || MathF.Abs(pPos.Y - Y) > 2.5f) && !IsAlly)
                {
                    WindupType = 2;
                    WindupTimer = 1.10f;
                    float b = Physics.WorldBound - 4.0f;
                    float dx = Math.Clamp(pPos.X, -b, b), dz = Math.Clamp(pPos.Z, -b, b);
                    LockedAimPos = new Vector3(dx, Physics.GetSurfaceHeight(dx, dz, pPos.Y + 1.5f), dz);
                    WindupTag = "ПРЫЖОК БЕЗДНЫ!";
                    WindupIndicator = new Entity(model: "diamond", position: LockedAimPos + new Vector3(0, 0.15f, 0),
                        scale: new(12.5f, 0.24f, 12.5f), color: Rgb(245, 30, 65, 170), unlit: true);
                    UpdateHpBillboard();
                }
            }
            else if (EnemyType == "magma_worm")
            {
                // Живая волнообразная анимация сегментов тела Магмового Червя
                if (Parts.Crown != null) Parts.Crown.Y = 1.4f + MathF.Sin(t * 6.0f) * 0.35f;
                if (Parts.Ring1 != null)
                {
                    Parts.Ring1.X = MathF.Sin(t * 5.5f) * 0.65f;
                    Parts.Ring1.Y = 0.1f + MathF.Cos(t * 5.5f) * 0.35f;
                }
                if (Parts.Ring2 != null)
                {
                    Parts.Ring2.X = MathF.Sin(t * 5.5f - 1.2f) * 0.95f;
                    Parts.Ring2.Y = -1.2f + MathF.Cos(t * 5.5f - 1.2f) * 0.45f;
                }
                if (Parts.Tail != null)
                    Parts.Tail.X = MathF.Sin(t * 5.5f - 2.4f) * 1.25f;

                if (WindupType == 2)
                {
                    WindupTimer -= dt;
                    if (Parts.Torso != null) Parts.Torso.Y = Lerp(Parts.Torso.Y, -2.2f, dt * 6f);
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                        WindupIndicator.RotationY += 240f * dt;
                    if (WindupTimer <= 0f)
                    {
                        Vector3 eruptPos = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = 6.2f;
                        Position = eruptPos;
                        VelY = 0f;
                        IsGrounded = true;
                        if (Parts.Torso != null) Parts.Torso.Y = 3.8f;
                        new VisualFX(eruptPos + new Vector3(0, 2.0f, 0), Color.Orange, 14.0f, 0.50f);
                        new LunarShockwaveRing(eruptPos, Damage * 1.15f, 30.0f, 19.0f);
                        if (!IsAlly && Physics.Distance2D(g.PlayerPos, eruptPos) <= 7.2f && MathF.Abs(g.PlayerPos.Y - eruptPos.Y) <= 4.0f)
                            CombatManager.DamagePlayer(Damage * 1.45f, this);
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = (i / 8.0f) * MathF.PI * 2f;
                            Vector3 d = Vector3.Normalize(new Vector3(MathF.Cos(ang), 0.45f, MathF.Sin(ang)));
                            var p = new Projectile(eruptPos + new Vector3(0, 3.2f, 0), d, 28f, Damage * 0.80f, 0.58f, Rgb(255, 120, 20), isPlayer: projIsPlayer, aoe: 2.8f, canProc: false)
                            {
                                GravityArc = 1.15f
                            };
                        }
                    }
                    return;
                }

                if (Parts.Torso != null) Parts.Torso.Y = Lerp(Parts.Torso.Y, 3.2f, dt * 5f);
                MoveBossSmartGround(1.5f, 7.5f, 9.0f);

                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && !IsAlly)
                {
                    WindupType = 2;
                    WindupTimer = 1.25f;
                    float b = Physics.WorldBound - 4.0f;
                    float dx = Math.Clamp(pPos.X, -b, b), dz = Math.Clamp(pPos.Z, -b, b);
                    LockedAimPos = new Vector3(dx, Physics.GetSurfaceHeight(dx, dz, pPos.Y + 1.5f), dz);
                    WindupTag = "ПРОРЫВ МАГМЫ ИЗ-ПОД ЗЕМЛИ!";
                    WindupIndicator = new Entity(model: "diamond", position: LockedAimPos + new Vector3(0, 0.16f, 0),
                        scale: new(14.0f, 0.25f, 14.0f), color: Rgb(255, 110, 20, 180), unlit: true);
                    UpdateHpBillboard();
                    return;
                }

                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    AttackTimer = 2.1f;
                    Vector3 sp = Position + new Vector3(0, 4.6f, 0);
                    Vector3 baseAim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                    foreach (float deg in new[] { -14f, 0f, 14f })
                    {
                        float rad = deg * (MathF.PI / 180f);
                        Vector3 d = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y + 4.0f, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                        var lavaProj = new Projectile(sp, d, 32f, Damage * 0.85f, 0.62f, Rgb(255, 140, 30), isPlayer: projIsPlayer, aoe: 2.8f, canProc: false)
                        {
                            GravityArc = 0.85f
                        };
                    }
                }
            }
            else if (EnemyType == "dunestrider")
            {
                if (Parts.Tentacles != null)
                {
                    Parts.Tentacles.RotationY += 80f * dt;
                    Parts.Tentacles.RotationZ = MathF.Sin(t * 4.5f) * 8f;
                }
                if (WindupType == 1)
                {
                    WindupTimer -= dt;
                    if (Parts.Torso != null) Parts.Torso.RotationX = Lerp(Parts.Torso.RotationX, 42f, dt * 8f);
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 2.4f;
                        if (Parts.Torso != null) Parts.Torso.RotationX = -15f;
                        Vector3 sp = Position + new Vector3(0, 3.6f, 0) + Forward * 1.4f;
                        Vector3 baseAim = (pPos + new Vector3(0, 1.0f, 0)) - sp;
                        foreach (float deg in new[] { -15f, 0f, 15f })
                        {
                            float rad = deg * (MathF.PI / 180f);
                            Vector3 d = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                            new Projectile(sp, d, 27f, Damage * 0.90f, 0.68f, Rgb(255, 130, 35), isPlayer: projIsPlayer, aoe: 3.0f, canProc: false);
                        }
                    }
                    return;
                }

                if (WindupType == 2)
                {
                    WindupTimer -= dt;
                    if (Parts.Torso != null) Parts.Torso.Y = Lerp(Parts.Torso.Y, 2.35f, dt * 6f);
                    if (Parts.Crown != null) Parts.Crown.Y = 1.65f + MathF.Sin(t * 20f) * 0.15f;
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                        WindupIndicator.RotationY -= 200f * dt;
                    // Плавное затягивание игрока в воронку дегтя
                    if (!IsAlly && Physics.Distance2D(g.PlayerPos, Position) <= 15.5f && g.CanPlayerInteract)
                    {
                        Vector3 pullDir = Vector3.Normalize(new Vector3(X - g.PlayerPos.X, 0, Z - g.PlayerPos.Z));
                        g.PlayerPos = Physics.ResolveHorizontalCollision(g.PlayerPos + pullDir * 5.2f * dt, 0.72f, g.PlayerPos.Y);
                    }
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = 7.2f;
                        if (Parts.Torso != null) Parts.Torso.Y = 3.35f;
                        new VisualFX(Position + new Vector3(0, 2.5f, 0), Rgb(255, 130, 35), 14.0f, 0.45f);
                        new LunarShockwaveRing(Position, Damage * 1.15f, 34.0f, 18.0f);
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = (i / 8.0f) * MathF.PI * 2f;
                            new Projectile(Position + new Vector3(0, 2.8f, 0), new Vector3(MathF.Cos(ang), 0.05f, MathF.Sin(ang)), 24f, Damage * 0.80f, 0.58f, Rgb(255, 130, 35), isPlayer: projIsPlayer, canProc: false);
                        }
                    }
                    return;
                }

                if (Parts.Torso != null)
                {
                    Parts.Torso.RotationX = Lerp(Parts.Torso.RotationX, 0f, dt * 6f);
                    Parts.Torso.Y = Lerp(Parts.Torso.Y, 3.35f, dt * 6f);
                }
                MoveBossSmartGround(1.4f, 10.5f);
                if (dist > 10.5f)
                {
                    float stride = MathF.Sin(t * 5.0f) * 24f;
                    if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stride;
                    if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stride;
                }

                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && !IsAlly && dist <= 22.0f)
                {
                    WindupType = 2;
                    WindupTimer = 1.35f;
                    WindupTag = "ВОРОНКА ДЕГТЯ (ПРИТЯЖЕНИЕ)!";
                    WindupIndicator = new Entity(model: "diamond", position: new(X, floorY + 0.14f, Z),
                        scale: new(28.0f, 0.22f, 28.0f), color: Rgb(255, 135, 35, 135), unlit: true);
                    UpdateHpBillboard();
                    return;
                }

                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    WindupType = 1;
                    WindupTimer = 0.78f;
                    WindupTag = "КАТЯЩИЕСЯ БОМБЫ!";
                    UpdateHpBillboard();
                }
            }
            else if (EnemyType == "solus_control")
            {
                if (Parts.Ring1 != null) Parts.Ring1.RotationY += (WindupType > 0 ? 360f : 95f) * dt;
                if (Parts.Ring2 != null) Parts.Ring2.RotationX += (WindupType > 0 ? 320f : 110f) * dt;
                if (Parts.Core != null) Parts.Core.Y = 3.8f + MathF.Sin(t * 3.2f) * 0.35f;
                Y = Lerp(Y, MathF.Max(floorY, pPos.Y), dt * 3f);
                Vector3 eyePos = Position + new Vector3(0, 3.8f, 0);

                if (WindupType == 1)
                {
                    WindupTimer -= dt;
                    LockedAimPos = Lerp(LockedAimPos, pPos + new Vector3(0, 1.2f, 0), dt * 7f);
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationY = 28f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationY = -28f;
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                    {
                        float len = MathF.Max(0.5f, Vector3.Distance(eyePos, LockedAimPos));
                        WindupIndicator.Position = (eyePos + LockedAimPos) * 0.5f;
                        WindupIndicator.Scale = new Vector3(0.12f, 0.12f, len);
                        WindupIndicator.LookAt(LockedAimPos);
                    }
                    if (WindupTimer <= 0f)
                    {
                        Vector3 aimTarget = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 2.2f;
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationY = 0f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationY = 0f;
                        Vector3 baseAim = aimTarget - eyePos;
                        foreach (float deg in new[] { -12f, 0f, 12f })
                        {
                            float rad = deg * (MathF.PI / 180f);
                            Vector3 d = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                            new Projectile(eyePos, d, 35f, Damage * 0.85f, 0.55f, Color.SkyBlue, isPlayer: projIsPlayer, canProc: false);
                        }
                    }
                    return;
                }

                if (WindupType == 2)
                {
                    WindupTimer -= dt;
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                        WindupIndicator.RotationY += 240f * dt;
                    if (WindupTimer <= 0f)
                    {
                        Vector3 gravPos = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = 6.5f;
                        new VisualFX(gravPos + new Vector3(0, 1.0f, 0), Color.SkyBlue, 12.0f, 0.40f);
                        new LunarShockwaveRing(gravPos, Damage * 1.10f, 32.0f, 20.0f);
                        if (!IsAlly && Physics.Distance2D(g.PlayerPos, gravPos) <= 8.0f && MathF.Abs(g.PlayerPos.Y - gravPos.Y) <= 4.0f)
                        {
                            g.PlayerVelY = 17.5f;
                            g.PlayerGrounded = false;
                            CombatManager.DamagePlayer(Damage * 1.25f, this);
                        }
                    }
                    return;
                }

                if (dist > 11.0f)
                {
                    Vector3 toP = new(pPos.X - X, 0f, pPos.Z - Z);
                    Vector3 dir = toP.LengthSquared() > 0.01f ? Vector3.Normalize(toP) : Forward;
                    Vector3 orbit = new(-dir.Z * _strafeDir, 0f, dir.X * _strafeDir);
                    Vector3 moveDir = Vector3.Normalize(dir + orbit * 0.55f);
                    Position += Physics.ComputeSmartSteerDirection(Position, moveDir, 1.8f, Y, ref _steerSide) * effSpeed * dt;
                }

                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && !IsAlly)
                {
                    WindupType = 2;
                    WindupTimer = 1.15f;
                    LockedAimPos = new Vector3(pPos.X, Physics.GetSurfaceHeight(pPos.X, pPos.Z, pPos.Y), pPos.Z);
                    WindupTag = "АНТИГРАВИТАЦИОННЫЙ ВЗРЫВ!";
                    WindupIndicator = new Entity(model: "diamond", position: LockedAimPos + new Vector3(0, 0.15f, 0),
                        scale: new(15.0f, 0.24f, 15.0f), color: Rgb(65, 225, 255, 165), unlit: true);
                    UpdateHpBillboard();
                    return;
                }

                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    WindupType = 1;
                    WindupTimer = 0.72f;
                    LockedAimPos = pPos + new Vector3(0, 1.2f, 0);
                    WindupTag = "ФОКУСИРОВКА ПРОТОКОЛА!";
                    WindupIndicator = new Entity(model: "cube", position: (eyePos + LockedAimPos) * 0.5f,
                        scale: new(0.12f, 0.12f, Vector3.Distance(eyePos, LockedAimPos)), color: Rgb(65, 225, 255, 175), unlit: true);
                    WindupIndicator.LookAt(LockedAimPos);
                    UpdateHpBillboard();
                }
            }
            else if (EnemyType == "lunar_chimera")
            {
                if (Parts.Ring1 != null) Parts.Ring1.RotationZ += 120f * dt;
                if (Parts.Ring2 != null) Parts.Ring2.RotationX += 140f * dt;
                if (Parts.Core != null) Parts.Core.Y = 2.5f + MathF.Sin(t * 4.5f) * 0.25f;

                // Стрейфит по дуге, держит высоту уровня игрока +1.8м и облетает препятствия
                Vector3 toP = new(pPos.X - X, 0f, pPos.Z - Z);
                Vector3 dir = toP.LengthSquared() > 0.01f ? Vector3.Normalize(toP) : Forward;
                float radial = dist > 15.0f ? 1.0f : (dist < 9.5f ? -0.75f : 0f);
                Vector3 orbit = new(-dir.Z * _strafeDir, 0f, dir.X * _strafeDir);
                Vector3 moveVec = dir * radial + orbit * 0.75f;
                if (moveVec.LengthSquared() > 0.01f)
                    Position += Physics.ComputeSmartSteerDirection(Position, Vector3.Normalize(moveVec), 1.2f, Y, ref _steerSide) * effSpeed * dt;
                Y = Lerp(Y, MathF.Max(floorY + 0.5f, pPos.Y + 1.5f), dt * 3.0f);

                AttackTimer -= dt;
                if (AttackTimer <= 0f && dist < 42.0f && Physics.HasLineOfSight(Position + new Vector3(0, 2.5f, 0), pPos + new Vector3(0, 1.2f, 0)))
                {
                    AttackTimer = 1.95f;
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

            if (Parts.Crown != null) Parts.Crown.RotationZ += (WindupType > 0 ? 260f : 95f) * dt;
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
                        CombatManager.DamagePlayer(Damage * 1.45f, this);

                    for (int i = 0; i < 12; i++)
                    {
                        float ang = (i / 12.0f) * MathF.PI * 2f;
                        new Projectile(landPos + new Vector3(0, 1.8f, 0), new Vector3(MathF.Cos(ang), 0.04f, MathF.Sin(ang)), 32f, Damage, 0.58f, Color.SkyBlue, isPlayer: false, canProc: false);
                    }
                }
                return;
            }

            // Обработка активных замахов Митрикса с выразительной анимацией молота и зоны поражения
            if (WindupType == 1)
            {
                WindupTimer -= dt;
                if (WindupSubType == 1) // Высокий замах молота двумя руками над головой!
                {
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, -155f, dt * 10f);
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, -115f, dt * 10f);
                    if (Parts.Torso != null) Parts.Torso.RotationX = Lerp(Parts.Torso.RotationX, -20f, dt * 10f);
                    if (Parts.HammerAura != null) Parts.HammerAura.Color = Color.White;
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                        WindupIndicator.RotationY += 220f * dt;

                    if (WindupTimer <= 0f)
                    {
                        Vector3 impactPos = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = Phase < 3 ? 1.75f : 1.40f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = 38f;
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = 20f;
                        if (Parts.Torso != null) Parts.Torso.RotationX = 22f;
                        if (Parts.HammerAura != null) Parts.HammerAura.Color = Phase == 3 ? Color.White : Color.SkyBlue;

                        new VisualFX(impactPos + new Vector3(0, 0.8f, 0), Color.SkyBlue, 9.5f, 0.38f);
                        new LunarShockwaveRing(impactPos, Damage * 1.25f, 54.0f, 21.0f);
                        if (Physics.Distance2D(impactPos, WorldContext.Game!.PlayerPos) <= 6.2f && MathF.Abs(WorldContext.Game.PlayerPos.Y - arenaY) < 2.8f)
                        {
                            if (CombatManager.Controller == null || !CombatManager.Controller.TryParryExternalAttack(Damage * 1.35f, impactPos))
                                CombatManager.DamagePlayer(Damage * 1.35f, this);
                        }
                    }
                }
                else if (WindupSubType == 2) // Прицеливание перед веером лунных осколков
                {
                    LookAt2D(pPos);
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -85f;
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = Phase < 3 ? 1.75f : 1.45f;
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = 0f;
                        Vector3 sp = Position + new Vector3(0, 3.0f, 0);
                        Vector3 baseAim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                        float[] angles = Phase >= 2 ? new[] { -12f, -6f, 0f, 6f, 12f } : new[] { -8f, 0f, 8f };
                        foreach (float angle in angles)
                        {
                            float rad = angle * (MathF.PI / 180f);
                            Vector3 d = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                            new Projectile(sp, d, 46f, Damage * 0.75f, 0.38f, Color.SkyBlue, isPlayer: false, canProc: false);
                        }
                    }
                }
                else if (WindupSubType == 3) // Фаза 4: Заряд сфер кражи и шоквейва
                {
                    LookAt2D(pPos);
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -95f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -95f;
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = 1.95f;
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
                }
                return;
            }

            // Спец-замах Фазы 1: Подготовка продольного рывка с линией-телеграфом на полу
            if (WindupType == 2)
            {
                WindupTimer -= dt;
                if (Parts.Torso != null) Parts.Torso.RotationX = 24f;
                if (Parts.RightArm != null) Parts.RightArm.RotationX = 55f;
                if (WindupTimer <= 0f)
                {
                    Vector3 lungeDest = LockedAimPos;
                    ClearWindup();
                    UpdateHpBillboard();
                    SpecialTimer = 5.2f;
                    Position = lungeDest;
                    if (Parts.Torso != null) Parts.Torso.RotationX = 0f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -130f;
                    new VisualFX(Position + new Vector3(0, 1.2f, 0), Color.SkyBlue, 9.0f, 0.35f);
                    new LunarShockwaveRing(Position, Damage, 54.0f, 20.0f);
                    if (Physics.Distance2D(Position, WorldContext.Game!.PlayerPos) <= 6.0f && MathF.Abs(WorldContext.Game.PlayerPos.Y - arenaY) < 2.8f)
                        CombatManager.DamagePlayer(Damage * 1.25f, this);
                }
                return;
            }

            float moveSpd = effSpeed * (Phase == 4 ? 0.68f : (Phase == 3 ? 1.30f : (Phase == 2 ? 1.18f : 1.05f)));
            if (dist > 4.4f)
            {
                Vector3 toP = new(pPos.X - X, 0f, pPos.Z - Z);
                Vector3 dir = toP.LengthSquared() > 0.01f ? Vector3.Normalize(toP) : Forward;
                Vector3 perp = new(-dir.Z * _strafeDir, 0f, dir.X * _strafeDir);
                Vector3 desired = Vector3.Normalize(dir + perp * (Phase == 4 ? 0.15f : 0.28f));
                Vector3 steered = Physics.ComputeSmartSteerDirection(Position, desired, 1.5f, arenaY, ref _steerSide);
                Position += steered * moveSpd * dt;
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
            if (Parts.LeftArm != null)
                Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, 0f, dt * 7.0f);
            if (Parts.Torso != null)
                Parts.Torso.RotationX = Lerp(Parts.Torso.RotationX, 0f, dt * 7.0f);

            AttackTimer -= dt;
            if (AttackTimer <= 0f)
            {
                if (Phase == 4)
                {
                    WindupType = 1;
                    WindupSubType = 3;
                    WindupTimer = 0.70f;
                    WindupTag = "ЗАЛП КОРОЛЯ НИЧЕГО!";
                    new VisualFX(Position + new Vector3(0, 3.0f, 0), Color.Magenta, 4.5f, 0.30f);
                    UpdateHpBillboard();
                    return;
                }
                else if (dist <= 9.2f && MathF.Abs(pPos.Y - arenaY) <= 4.5f)
                {
                    WindupType = 1;
                    WindupSubType = 1;
                    WindupTimer = 0.85f;
                    LockedAimPos = new Vector3(X + Forward.X * 2.8f, arenaY, Z + Forward.Z * 2.8f);
                    WindupTag = "СОКРУШИТЕЛЬНЫЙ МОЛОТ!";
                    WindupIndicator = new Entity(model: "diamond", position: LockedAimPos + new Vector3(0, 0.14f, 0),
                        scale: new(12.0f, 0.22f, 12.0f), color: Rgb(80, 225, 255, 165), unlit: true);
                    UpdateHpBillboard();
                    return;
                }
                else
                {
                    float dashSide = _rng.Next(2) == 0 ? -1f : 1f;
                    Vector3 newP = Position + Right * (dashSide * 5.5f);
                    if (MathF.Sqrt(newP.X * newP.X + newP.Z * newP.Z) <= maxArenaR)
                        Position = new Vector3(newP.X, arenaY, newP.Z);
                    new VisualFX(Position + new Vector3(0, 2.0f, 0), Color.SkyBlue, 3.8f, 0.25f);

                    WindupType = 1;
                    WindupSubType = 2;
                    WindupTimer = 0.55f;
                    WindupTag = "ЛУННЫЕ ОСКОЛКИ!";
                    UpdateHpBillboard();
                    return;
                }
            }

            SpecialTimer -= dt;
            if (SpecialTimer <= 0f)
            {
                if (Phase == 1)
                {
                    Vector3 lunge = Position + Forward * MathF.Min(dist - 1.5f, 16.0f);
                    if (MathF.Sqrt(lunge.X * lunge.X + lunge.Z * lunge.Z) > maxArenaR)
                    {
                        float ang = MathF.Atan2(lunge.Z, lunge.X);
                        lunge.X = MathF.Cos(ang) * maxArenaR;
                        lunge.Z = MathF.Sin(ang) * maxArenaR;
                    }
                    LockedAimPos = new Vector3(lunge.X, arenaY, lunge.Z);
                    WindupType = 2;
                    WindupTimer = 0.90f;
                    WindupTag = "РЫВОК С МОЛОТОМ!";
                    float len = MathF.Max(1f, Vector3.Distance(Position, LockedAimPos));
                    WindupIndicator = new Entity(model: "cube", position: (Position + LockedAimPos) * 0.5f + new Vector3(0, 0.14f, 0),
                        scale: new(2.4f, 0.15f, len), color: Rgb(80, 225, 255, 145), unlit: true);
                    WindupIndicator.LookAt(LockedAimPos + new Vector3(0, 0.14f, 0));
                    UpdateHpBillboard();
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
                    SpecialTimer = 5.4f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -135f;
                    new LunarPizzaAttack(new Vector3(0, arenaY, 0), Damage * 1.35f, 8);
                    new OrbitalStrikeFX(new Vector3(pPos.X, arenaY, pPos.Z), 0f);
                }
                else if (Phase == 4)
                {
                    SpecialTimer = 4.8f;
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
                _parts.TriggerSkillAnim(isJupiter ? "m2" : "m1");
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
                    _parts.TriggerSkillAnim("shift", 0.35f);
                    new VisualFX(Position + new Vector3(0, 1f, 0), Rgb(95, 255, 135), 2.2f, 0.22f);
                }
                else
                {
                    Position += dir * (c.MoveSpeed * 1.08f) * dt;
                }
                LookAt2D(desiredPos);
            }

            _parts.UpdateAnimations("m4a1", dt, toDesired.Length() > 0.75f, true, 0f, 1.08f, 0f, 1f);
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