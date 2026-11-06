using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.World;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Entities
{
    internal static class ExpansionBossArenaHelper
    {
        private static readonly Random _rng = new();

        public static float NextFloat(float min, float max) => min + (float)_rng.NextDouble() * (max - min);

        public static float CheckPlayerAttackDamage(Entity node, float hitRadius, float centerOffsetY, ref float meleeHitCd)
        {
            var g = WorldContext.Game;
            var ctrl = CombatManager.Controller;
            if (g == null || !IsValid(node)) return 0f;

            Vector3 hitCenter = node.Position + new Vector3(0, centerOffsetY, 0);
            float totalDamage = 0f;

            for (int i = Engine.AllEntities.Count - 1; i >= 0; i--)
            {
                if (i >= Engine.AllEntities.Count) continue;
                if (Engine.AllEntities[i] is Projectile proj && proj.IsPlayer && !proj.IsDestroyed)
                {
                    if (Vector3.Distance(proj.Position, hitCenter) <= hitRadius + proj.ScaleX * 0.65f)
                    {
                        float dmg = MathF.Max(4f, proj.Damage);
                        if (!proj.Pierce)
                        {
                            totalDamage += dmg;
                            proj.SafeDestroy();
                            Program.TriggerHitMarker(false, dmg);
                            SoundManager.Play("hit", 0.72f);
                            new VisualFX(hitCenter, Color.Gold, 1.85f, 0.20f);
                        }
                        else if (meleeHitCd <= 0f)
                        {
                            meleeHitCd = 0.14f;
                            totalDamage += dmg;
                            Program.TriggerHitMarker(false, dmg);
                            SoundManager.Play("hit", 0.72f);
                            new VisualFX(hitCenter, Color.Gold, 1.85f, 0.20f);
                        }
                    }
                }
            }

            if (meleeHitCd <= 0f && ctrl != null && g.CanPlayerInteract &&
                (Raylib.IsMouseButtonDown(MouseButton.Left) || Raylib.IsMouseButtonPressed(MouseButton.Right)))
            {
                Vector3 eyePos = g.PlayerPos + new Vector3(0, 1.55f, 0);
                Vector3 toNode = hitCenter - eyePos;
                float dist = toNode.Length();
                bool hit = false;
                if (dist <= 5.6f + hitRadius)
                {
                    Vector3 flat = new(toNode.X, 0f, toNode.Z);
                    if (flat.LengthSquared() < 0.05f || Vector3.Dot(Vector3.Normalize(flat), g.PlayerForward) >= 0.15f)
                        hit = true;
                }
                else if (dist <= 46.0f)
                {
                    Vector3 aim = ctrl.GetAimDirection();
                    if (Vector3.Dot(aim, Vector3.Normalize(toNode)) >= 0.94f && Physics.HasLineOfSight(eyePos, hitCenter))
                        hit = true;
                }

                if (hit)
                {
                    meleeHitCd = 0.22f;
                    float dmg = MathF.Max(10f, ctrl.BaseDamage * 1.15f);
                    totalDamage += dmg;
                    Program.TriggerHitMarker(false, dmg);
                    SoundManager.Play("hit", 0.75f);
                    new VisualFX(hitCenter, Color.SkyBlue, 2.0f, 0.20f);
                }
            }

            return totalDamage;
        }

        public static void DamagePlayerWithSanctuaryCheck(float rawDamage, CombatTarget? attacker, Vector3 sourcePos)
        {
            var g = WorldContext.Game;
            if (g == null || rawDamage <= 0f) return;
            if (attacker != null && attacker.IsAlly) return;
            if (CombatManager.Controller != null && CombatManager.Controller.TryParryExternalAttack(rawDamage, sourcePos))
                return;

            float domeMult = HarmonySafeDomeEntity.GetPlayerDamageMultiplier(g.PlayerPos);
            CombatManager.DamagePlayer(rawDamage * domeMult, attacker);
        }

        public static Vector3 FindArenaFloorSpot(Vector3 origin, float minDist, float maxDist)
        {
            float wb = Physics.WorldBound - 4.5f;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float ang = NextFloat(0f, MathF.PI * 2f);
                float r = NextFloat(minDist, maxDist);
                float tx = Math.Clamp(origin.X + MathF.Cos(ang) * r, -wb, wb);
                float tz = Math.Clamp(origin.Z + MathF.Sin(ang) * r, -wb, wb);
                Physics.ClampToArena(ref tx, ref tz, 4.0f);
                float fy = Physics.GetSurfaceHeight(tx, tz, origin.Y + 4.0f);
                Vector3 resolved = Physics.ResolveHorizontalCollision(new Vector3(tx, fy, tz), 1.35f, fy);
                resolved.Y = Physics.GetSurfaceHeight(resolved.X, resolved.Z, fy + 2.0f);
                if (MathF.Abs(resolved.Y - origin.Y) <= 7.5f)
                    return resolved;
            }
            float fallbackY = Physics.GetSurfaceHeight(origin.X, origin.Z, origin.Y + 2.0f);
            return new Vector3(origin.X, fallbackY, origin.Z);
        }
    }

    public class CustomBossShockwaveRing : Entity
    {
        private readonly CombatTarget? _owner;
        private readonly float _damage;
        private readonly float _maxRadius;
        private readonly float _expandSpeed;
        private float _radius = 1.6f;
        private bool _hitPlayer = false;

        public CustomBossShockwaveRing(
            Vector3 center, float damage, Color ringColor, float maxRadius = 38.0f,
            float expandSpeed = 19.5f, CombatTarget? owner = null)
            : base(position: new Vector3(center.X, center.Y + 0.26f, center.Z))
        {
            _owner = owner;
            _damage = damage;
            _maxRadius = maxRadius;
            _expandSpeed = expandSpeed;

            int segs = 16;
            for (int i = 0; i < segs; i++)
            {
                float a = (i / (float)segs) * MathF.PI * 2f;
                float deg = a * (180f / MathF.PI);
                new Entity(parent: this, model: "cube", scale: new(0.42f, 0.30f, 0.14f),
                    position: new(MathF.Cos(a), 0f, MathF.Sin(a)), rotation: new(0, -deg, 0),
                    color: ringColor, unlit: true);
            }
            Scale = new Vector3(_radius, 1f, _radius);
            WorldContext.Game?.StageEntities.Add(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (CombatManager.Controller != null && CombatManager.Controller.ZaWarudoTimer > 0f) return;

            _radius += _expandSpeed * dt;
            Scale = new Vector3(_radius, 1f, _radius);
            RotationY += 55f * dt;

            if (_owner != null && _owner.IsAlly)
            {
                foreach (var enemy in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(enemy) && !enemy.IsAlly)
                    {
                        float d = Physics.Distance2D(enemy.Position, Position);
                        if (MathF.Abs(d - _radius) <= 1.25f && MathF.Abs(enemy.Position.Y - Position.Y) <= 2.2f)
                            enemy.TakeDamage(_damage * dt * 5.0f);
                    }
                }
            }
            else if (!_hitPlayer)
            {
                float pDist = Physics.Distance2D(g.PlayerPos, Position);
                float pHeightAboveWave = g.PlayerPos.Y - Position.Y;
                if (MathF.Abs(pDist - _radius) <= 1.25f && pHeightAboveWave >= -1.2f && pHeightAboveWave < 1.55f)
                {
                    _hitPlayer = true;
                    ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(_damage, _owner, Position);
                    new VisualFX(g.PlayerPos + new Vector3(0, 0.9f, 0), Color.White, 2.4f, 0.22f);
                }
            }

            if (_radius >= _maxRadius)
                Destroy(this);
        }
    }

    public class DelayedBossStrikeHazard : Entity
    {
        private readonly CombatTarget? _owner;
        private readonly float _damage;
        private readonly float _radius;
        private readonly Color _beamColor;
        private readonly bool _spawnMiniRing;
        private float _timer;
        private readonly float _totalDelay;
        private readonly Entity _innerSigil;

        public DelayedBossStrikeHazard(
            Vector3 floorPos, float delay, float radius, float damage,
            Color sigilColor, Color beamColor, CombatTarget? owner, bool spawnMiniRing = false)
            : base(position: floorPos + new Vector3(0, 0.14f, 0))
        {
            _owner = owner;
            _timer = delay;
            _totalDelay = MathF.Max(0.15f, delay);
            _radius = radius;
            _damage = damage;
            _beamColor = beamColor;
            _spawnMiniRing = spawnMiniRing;

            new Entity(parent: this, model: "diamond", scale: new(radius * 2.0f, 0.14f, radius * 2.0f),
                color: Rgb(sigilColor.R, sigilColor.G, sigilColor.B, 145), unlit: true);
            _innerSigil = new Entity(parent: this, model: "diamond", scale: new(0.4f, 0.22f, 0.4f),
                color: Color.White, unlit: true);
            WorldContext.Game?.StageEntities.Add(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (_owner != null && (!IsValid(_owner) || _owner.Hp <= 0f))
            {
                Destroy(this);
                return;
            }
            if (CombatManager.Controller != null && CombatManager.Controller.ZaWarudoTimer > 0f) return;

            _timer -= dt;
            RotationY += 210f * dt;
            float prog = Math.Clamp(1.0f - (_timer / _totalDelay), 0f, 1f);
            if (IsValid(_innerSigil))
                _innerSigil.Scale = new Vector3(_radius * 2.0f * prog, 0.22f, _radius * 2.0f * prog);

            if (_timer <= 0f)
            {
                SoundManager.Play3D("explosion", Position, g.PlayerPos, 58f, 0.72f, 1.08f);
                new LightningBeam(Position + new Vector3(0, 26.0f, 0), Position, _beamColor, _radius * 0.26f);
                new VisualFX(Position + new Vector3(0, 1.0f, 0), _beamColor, _radius * 2.15f, 0.36f);

                if (_owner != null && _owner.IsAlly)
                {
                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (IsValid(e) && !e.IsAlly && Physics.Distance2D(e.Position, Position) <= _radius)
                            e.TakeDamage(_damage);
                    }
                }
                else if (Physics.Distance2D(g.PlayerPos, Position) <= _radius && MathF.Abs(g.PlayerPos.Y - Position.Y) <= 4.2f)
                {
                    ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(_damage, _owner, Position);
                }

                if (_spawnMiniRing)
                    new CustomBossShockwaveRing(Position, _damage * 0.72f, _beamColor, 20.0f, 17.5f, _owner);

                Destroy(this);
            }
        }
    }

    // 1. Щитовой Пилон Искажённого Провидца
    public class ProvidenceShieldPylon : Entity
    {
        public readonly CombatTarget Boss;
        public float Hp;
        public readonly float MaxHp;
        private float _orbitAngle;
        private float _beamTick = 0.1f;
        private float _hitCd = 0f;
        private readonly Entity _coreCrystal;
        private readonly Entity _ringFrame;
        private readonly Entity _label;

        public ProvidenceShieldPylon(CombatTarget boss, float startAngle)
            : base(position: boss.Position + new Vector3(MathF.Cos(startAngle) * 9.0f, 3.2f, MathF.Sin(startAngle) * 9.0f))
        {
            Boss = boss;
            _orbitAngle = startAngle;
            MaxHp = MathF.Max(220f, boss.MaxHp * 0.13f);
            Hp = MaxHp;
            boss.ShieldDroneProtected = true;

            Color gold = Rgb(245, 200, 68);
            Color cyan = Rgb(70, 240, 255);
            Color ivory = Rgb(236, 240, 250);

            new Entity(parent: this, model: "diamond", scale: new(1.25f, 2.55f, 1.25f), color: ivory);
            _coreCrystal = new Entity(parent: this, model: "diamond", scale: new(0.86f, 1.85f, 0.86f), color: cyan, unlit: true);
            _ringFrame = new Entity(parent: this, model: "diamond", scale: new(2.15f, 0.18f, 2.15f), color: gold, unlit: true);

            _label = new Entity(parent: this, position: new Vector3(0, 2.15f, 0), color: cyan)
            {
                BillboardText = "◆ ЩИТОВОЙ ПИЛОН [100%] ◆"
            };

            WorldContext.Game?.StageEntities.Add(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (!IsValid(Boss) || Boss.Hp <= 0f || Boss.IsAlly)
            {
                if (IsValid(Boss)) Boss.ShieldDroneProtected = false;
                Destroy(this);
                return;
            }

            Boss.ShieldDroneProtected = true;
            _hitCd = MathF.Max(0f, _hitCd - dt);
            _orbitAngle += 0.68f * dt;

            float targetX = Boss.Position.X + MathF.Cos(_orbitAngle) * 9.2f;
            float targetZ = Boss.Position.Z + MathF.Sin(_orbitAngle) * 9.2f;
            Physics.ClampToArena(ref targetX, ref targetZ, 3.5f);
            float groundY = Physics.GetSurfaceHeight(targetX, targetZ, Boss.Position.Y + 3.0f);
            Vector3 desired = new(targetX, groundY + 2.85f + MathF.Sin((float)Raylib.GetTime() * 3.5f) * 0.35f, targetZ);
            Position = Lerp(Position, desired, dt * 5.0f);

            if (IsValid(_coreCrystal)) _coreCrystal.RotationY += 180f * dt;
            if (IsValid(_ringFrame)) _ringFrame.RotationY -= 140f * dt;

            _beamTick -= dt;
            if (_beamTick <= 0f)
            {
                _beamTick = 0.22f;
                new LightningBeam(Position, Boss.Position + new Vector3(0, 2.8f, 0), Rgb(70, 240, 255), 0.22f);
            }

            float dmg = ExpansionBossArenaHelper.CheckPlayerAttackDamage(this, 1.55f, 0f, ref _hitCd);
            if (dmg > 0f)
            {
                Hp = MathF.Max(0f, Hp - dmg);
                int pct = Math.Clamp((int)MathF.Ceiling((Hp / MaxHp) * 100f), 0, 100);
                _label.BillboardText = $"◆ ЩИТОВОЙ ПИЛОН [{pct}%] ◆";
                if (Hp <= 0f)
                {
                    Boss.ShieldDroneProtected = false;
                    Boss.InterruptExpansionBossChannel();
                    Boss.StunTimer = MathF.Max(Boss.StunTimer, 2.8f);
                    Boss.TakeDamage(Boss.MaxHp * 0.045f);
                    SoundManager.Play("parry");
                    SoundManager.Play3D("explosion", Position, g.PlayerPos, 60f, 0.95f);
                    new VisualFX(Position, Rgb(70, 240, 255), 6.5f, 0.42f);
                    new LightningBeam(Position, Boss.Position + new Vector3(0, 2.8f, 0), Rgb(255, 75, 165), 0.55f);
                    g.ShowBanner("✓ ЩИТОВОЙ ПИЛОН РАЗРУШЕН — ПРОВИДЕЦ ОГЛУШЁН! ✓", Rgb(70, 240, 255), 3.0f);
                    Destroy(this);
                }
            }
        }
    }

    // Теневой клон Провидца в Фазе 3
    public class ProvidenceShadowCloneStrike : Entity
    {
        private readonly CombatTarget _boss;
        private readonly Vector3 _dashDir;
        private float _windup = 0.85f;
        private float _dashLife = 0.36f;
        private bool _dashed = false;
        private bool _hitPlayer = false;
        private readonly Entity? _aimLine;

        public ProvidenceShadowCloneStrike(CombatTarget boss, Vector3 spawnPos, Vector3 targetPos)
            : base(position: spawnPos)
        {
            _boss = boss;
            Vector3 diff = new(targetPos.X - spawnPos.X, 0f, targetPos.Z - spawnPos.Z);
            _dashDir = diff.LengthSquared() > 0.01f ? Vector3.Normalize(diff) : Vector3.UnitZ;
            LookAt2D(targetPos);

            Color phantomCol = Rgb(225, 45, 125, 195);
            Color cyanEdge = Rgb(70, 240, 255, 220);
            new Entity(parent: this, model: "diamond", scale: new(1.35f, 3.1f, 0.95f), position: new(0, 1.75f, 0), color: phantomCol, unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(0.45f, 3.3f, 0.25f), position: new(0.85f, 2.1f, 0.65f), rotation: new(48f, 0, 0), color: cyanEdge, unlit: true);

            Vector3 lineMid = spawnPos + _dashDir * 8.0f + new Vector3(0, 0.22f, 0);
            _aimLine = new Entity(model: "cube", position: lineMid, scale: new(0.32f, 0.12f, 16.0f),
                rotation: new(0, RotationY, 0), color: Rgb(255, 65, 145, 165), unlit: true);

            WorldContext.Game?.StageEntities.Add(this);
            if (_aimLine != null) WorldContext.Game?.StageEntities.Add(_aimLine);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (!IsValid(_boss) || _boss.Hp <= 0f)
            {
                if (IsValid(_aimLine)) Destroy(_aimLine);
                Destroy(this);
                return;
            }
            if (CombatManager.Controller != null && CombatManager.Controller.ZaWarudoTimer > 0f) return;

            if (!_dashed)
            {
                _windup -= dt;
                if (_windup <= 0f)
                {
                    _dashed = true;
                    if (IsValid(_aimLine)) Destroy(_aimLine);
                    SoundManager.Play3D("melee_swing", Position, g.PlayerPos, 55f, 0.90f, 1.18f);
                    new MeleeSlashFX(Position + new Vector3(0, 1.4f, 0), _dashDir, RotationY, Rgb(255, 65, 145), 4.2f, 6.5f, 18f, false);
                }
                return;
            }

            _dashLife -= dt;
            Position += _dashDir * 44.0f * dt;
            if (!_hitPlayer && !_boss.IsAlly && Physics.Distance2D(g.PlayerPos, Position) <= 2.65f && MathF.Abs(g.PlayerPos.Y - Position.Y) <= 3.2f)
            {
                _hitPlayer = true;
                ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(_boss.Damage * 1.05f, _boss, Position);
            }

            if (_dashLife <= 0f)
            {
                new VisualFX(Position + new Vector3(0, 1.5f, 0), Rgb(225, 45, 125), 3.0f, 0.22f);
                Destroy(this);
            }
        }
    }

    // 2. Золотая Жеода Аурелионита для боя с Ложным Сыном (Фаза 3)
    public class AurelioniteArenaGeode : Entity
    {
        private readonly CombatTarget _boss;
        private readonly Entity _crystalCluster;
        private readonly Entity _halo;
        private float _lifeTimer = 18.0f;

        public AurelioniteArenaGeode(CombatTarget boss, Vector3 floorPos)
            : base(position: floorPos)
        {
            _boss = boss;
            Color gold = Rgb(255, 212, 58);
            Color brightGold = Rgb(255, 244, 155);
            Color cyan = Rgb(85, 238, 255);

            new Entity(parent: this, model: "cube", scale: new(1.85f, 0.32f, 1.85f), position: new(0, 0.16f, 0), color: Rgb(72, 76, 90));
            _crystalCluster = new Entity(parent: this, position: new(0, 1.25f, 0));
            new Entity(parent: _crystalCluster, model: "diamond", scale: new(0.92f, 2.25f, 0.92f), color: gold, unlit: true);
            new Entity(parent: _crystalCluster, model: "diamond", scale: new(0.56f, 1.65f, 0.56f), position: new(-0.52f, -0.25f, 0.25f), rotation: new(0, 0, 22f), color: brightGold, unlit: true);
            new Entity(parent: _crystalCluster, model: "diamond", scale: new(0.56f, 1.65f, 0.56f), position: new(0.52f, -0.25f, -0.25f), rotation: new(0, 0, -22f), color: cyan, unlit: true);

            _halo = new Entity(parent: this, model: "diamond", scale: new(2.65f, 0.16f, 2.65f), position: new(0, 0.35f, 0), color: Rgb(255, 215, 65, 165), unlit: true);
            new Entity(parent: this, position: new Vector3(0, 2.95f, 0), color: gold)
            {
                BillboardText = "★ ЖЕОДА АУРЕЛИОНИТА ★"
            };

            WorldContext.Game?.StageEntities.Add(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            var ctrl = CombatManager.Controller;
            if (g == null || IsDestroyed) return;
            if (!IsValid(_boss) || _boss.Hp <= 0f)
            {
                Destroy(this);
                return;
            }

            _lifeTimer -= dt;
            if (IsValid(_crystalCluster))
            {
                _crystalCluster.RotationY += 110f * dt;
                _crystalCluster.Y = 1.25f + MathF.Sin((float)Raylib.GetTime() * 4.0f) * 0.20f;
            }
            if (IsValid(_halo)) _halo.RotationY -= 95f * dt;

            if (ctrl != null && g.CanPlayerInteract && Physics.Distance2D(g.PlayerPos, Position) <= 2.85f && MathF.Abs(g.PlayerPos.Y - Position.Y) <= 3.5f)
            {
                ctrl.M2Cd = 0f;
                ctrl.ShiftCd = 0f;
                ctrl.RCd = MathF.Min(ctrl.RCd, 1.2f);
                ctrl.SkillLockTimer = 0f;
                ctrl.Shield = MathF.Min(g.PlayerMaxHp * 1.5f, ctrl.Shield + 48.0f);
                g.HealPlayer(g.PlayerMaxHp * 0.10f);

                SoundManager.Play("shrine_use");
                SoundManager.Play("zap", 0.90f, 1.12f);
                new VisualFX(Position + new Vector3(0, 1.2f, 0), Rgb(255, 215, 65), 5.5f, 0.36f);

                if (IsValid(_boss) && !_boss.IsAlly)
                {
                    Vector3 bossHeart = _boss.Position + new Vector3(0, 3.1f, 0);
                    new LightningBeam(Position + new Vector3(0, 1.6f, 0), bossHeart, Rgb(255, 215, 65), 0.55f);
                    new VisualFX(bossHeart, Rgb(255, 215, 65), 5.0f, 0.32f);
                    _boss.ArmorBreakTimer = MathF.Max(_boss.ArmorBreakTimer, 4.0f);
                    _boss.TakeDamage(_boss.MaxHp * 0.045f);
                }

                g.ShowBanner("★ ДАР АУРЕЛИОНИТА :: НАВЫКИ ВОССТАНОВЛЕНЫ + ЩИТ! ★", Rgb(255, 215, 65), 2.6f);
                Destroy(this);
                return;
            }

            if (_lifeTimer <= 0f)
                Destroy(this);
        }
    }

    // 3. Миметический Дрон-Узел PRTS (Фаза 2) и Зона Ассимиляции
    public class PrtsMimicDroneNode : Entity
    {
        private readonly CombatTarget _boss;
        public float Hp;
        private float _orbitAngle;
        private float _fireTimer;
        private float _telegraphTimer = 0f;
        private Vector3 _lockedTarget;
        private float _lifeTimer = 16.0f;
        private float _hitCd = 0f;
        private readonly Entity _outerRing;
        private Entity? _sightLine;

        public PrtsMimicDroneNode(CombatTarget boss, float startAngle, float initialDelay)
            : base(position: boss.Position + new Vector3(MathF.Cos(startAngle) * 11.5f, 3.8f, MathF.Sin(startAngle) * 11.5f))
        {
            _boss = boss;
            _orbitAngle = startAngle;
            _fireTimer = initialDelay;
            Hp = MathF.Max(140f, boss.MaxHp * 0.075f);

            new Entity(parent: this, model: "diamond", scale: new(1.05f, 1.55f, 1.05f), color: Rgb(18, 20, 28));
            new Entity(parent: this, model: "diamond", scale: new(0.58f, 0.92f, 1.15f), color: Rgb(255, 38, 68), unlit: true);
            _outerRing = new Entity(parent: this, model: "diamond", scale: new(1.95f, 0.14f, 1.95f), color: Rgb(255, 118, 42), unlit: true);
            new Entity(parent: this, position: new Vector3(0, 1.45f, 0), color: Rgb(255, 75, 95))
            {
                BillboardText = "◇ УЗЕЛ PRTS ◇"
            };

            WorldContext.Game?.StageEntities.Add(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (!IsValid(_boss) || _boss.Hp <= 0f)
            {
                if (IsValid(_sightLine)) Destroy(_sightLine);
                Destroy(this);
                return;
            }
            if (CombatManager.Controller != null && CombatManager.Controller.ZaWarudoTimer > 0f) return;

            _lifeTimer -= dt;
            _hitCd = MathF.Max(0f, _hitCd - dt);
            _orbitAngle += 0.52f * dt;
            if (IsValid(_outerRing)) _outerRing.RotationY += 220f * dt;

            float tx = _boss.Position.X + MathF.Cos(_orbitAngle) * 11.5f;
            float tz = _boss.Position.Z + MathF.Sin(_orbitAngle) * 11.5f;
            Physics.ClampToArena(ref tx, ref tz, 3.5f);
            float fy = Physics.GetSurfaceHeight(tx, tz, _boss.Position.Y + 3.0f);
            Position = Lerp(Position, new Vector3(tx, fy + 3.4f, tz), dt * 4.2f);

            float taken = ExpansionBossArenaHelper.CheckPlayerAttackDamage(this, 1.35f, 0f, ref _hitCd);
            if (taken > 0f)
            {
                Hp -= taken;
                if (Hp <= 0f)
                {
                    if (IsValid(_sightLine)) Destroy(_sightLine);
                    new VisualFX(Position, Rgb(255, 38, 68), 4.0f, 0.28f);
                    if (IsValid(_boss)) _boss.TakeDamage(_boss.MaxHp * 0.02f);
                    Destroy(this);
                    return;
                }
            }

            if (_telegraphTimer > 0f)
            {
                _telegraphTimer -= dt;
                if (_telegraphTimer <= 0f)
                {
                    if (IsValid(_sightLine)) Destroy(_sightLine);
                    _sightLine = null;
                    SoundManager.Play3D("laser", Position, g.PlayerPos, 58f, 0.72f, 1.15f);
                    new LightningBeam(Position, _lockedTarget, Rgb(255, 38, 68), 0.36f);
                    new VisualFX(_lockedTarget, Rgb(255, 38, 68), 3.6f, 0.26f);
                    if (!_boss.IsAlly && Physics.Distance2D(g.PlayerPos, _lockedTarget) <= 2.4f && MathF.Abs(g.PlayerPos.Y - _lockedTarget.Y) <= 3.2f)
                        ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(_boss.Damage * 0.78f, _boss, Position);
                    _fireTimer = 3.25f;
                }
            }
            else
            {
                _fireTimer -= dt;
                if (_fireTimer <= 0f && !_boss.IsAlly)
                {
                    _telegraphTimer = 0.82f;
                    float pFloor = Physics.GetSurfaceHeight(g.PlayerPos.X, g.PlayerPos.Z, g.PlayerPos.Y + 1.0f);
                    _lockedTarget = new Vector3(g.PlayerPos.X, pFloor + 0.6f, g.PlayerPos.Z);
                    _sightLine = new Entity(model: "diamond", position: _lockedTarget,
                        scale: new(4.6f, 0.14f, 4.6f), color: Rgb(255, 118, 42, 165), unlit: true);
                    WorldContext.Game?.StageEntities.Add(_sightLine);
                }
            }

            if (_lifeTimer <= 0f)
            {
                if (IsValid(_sightLine)) Destroy(_sightLine);
                Destroy(this);
            }
        }
    }

    public class PrtsAssimilationZone : Entity
    {
        private readonly CombatTarget _boss;
        private readonly float _radius;
        private float _armDelay = 1.0f;
        private float _lifeTimer = 5.4f;
        private float _tickTimer = 0.25f;
        private readonly Entity _grid;

        public PrtsAssimilationZone(CombatTarget boss, Vector3 floorPos, float radius = 5.2f)
            : base(position: floorPos + new Vector3(0, 0.12f, 0))
        {
            _boss = boss;
            _radius = radius;
            _grid = new Entity(parent: this, model: "diamond", scale: new(radius * 2.0f, 0.14f, radius * 2.0f),
                color: Rgb(255, 36, 68, 130), unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(radius * 1.25f, 0.18f, radius * 1.25f),
                color: Rgb(18, 20, 28, 205), unlit: true);
            WorldContext.Game?.StageEntities.Add(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (!IsValid(_boss) || _boss.Hp <= 0f)
            {
                Destroy(this);
                return;
            }
            if (CombatManager.Controller != null && CombatManager.Controller.ZaWarudoTimer > 0f) return;

            RotationY += 48f * dt;
            if (_armDelay > 0f)
            {
                _armDelay -= dt;
                return;
            }

            _lifeTimer -= dt;
            if (IsValid(_grid))
                _grid.Color = Rgb(255, 36, 68, (byte)(155 + (int)(MathF.Sin((float)Raylib.GetTime() * 12f) * 40f)));

            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = 0.55f;
                if (!_boss.IsAlly && Physics.Distance2D(g.PlayerPos, Position) <= _radius && MathF.Abs(g.PlayerPos.Y - Position.Y) <= 2.4f)
                {
                    ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(_boss.Damage * 0.42f, _boss, Position);
                    new VisualFX(g.PlayerPos + new Vector3(0, 1.0f, 0), Rgb(255, 36, 68), 1.8f, 0.18f);
                }
            }

            if (_lifeTimer <= 0f)
                Destroy(this);
        }
    }

    // 4. Столпы Сингулярности и Печати Клипот Арбитра Бины
    public class BinahPillarHazard : Entity
    {
        private readonly CombatTarget _boss;
        private readonly Vector3 _moveDir;
        private float _windup;
        private float _travelTimer = 1.65f;
        private bool _launched = false;
        private bool _hitPlayer = false;
        private readonly Entity? _laneMarker;

        public BinahPillarHazard(CombatTarget boss, Vector3 spawnPos, Vector3 dir, float windup = 0.78f)
            : base(position: spawnPos)
        {
            _boss = boss;
            _moveDir = dir.LengthSquared() > 0.01f ? Vector3.Normalize(new Vector3(dir.X, 0f, dir.Z)) : Vector3.UnitZ;
            _windup = windup;
            RotationY = MathF.Atan2(_moveDir.X, _moveDir.Z) * (180f / MathF.PI);

            Color black = Rgb(18, 18, 24);
            Color gold = Rgb(246, 198, 58);
            new Entity(parent: this, model: "cube", scale: new(1.65f, 4.6f, 1.65f), position: new(0, 2.35f, 0), color: black);
            new Entity(parent: this, model: "cube", scale: new(1.82f, 0.26f, 1.82f), position: new(0, 0.65f, 0), color: gold, unlit: true);
            new Entity(parent: this, model: "cube", scale: new(1.82f, 0.26f, 1.82f), position: new(0, 4.15f, 0), color: gold, unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(0.86f, 3.65f, 1.88f), position: new(0, 2.35f, 0), color: Rgb(255, 232, 128), unlit: true);

            Vector3 laneMid = spawnPos + _moveDir * 18.0f + new Vector3(0, 0.14f, 0);
            _laneMarker = new Entity(model: "cube", position: laneMid, scale: new(2.45f, 0.10f, 36.0f),
                rotation: new(0, RotationY, 0), color: Rgb(246, 198, 58, 145), unlit: true);

            WorldContext.Game?.StageEntities.Add(this);
            if (_laneMarker != null) WorldContext.Game?.StageEntities.Add(_laneMarker);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (!IsValid(_boss) || _boss.Hp <= 0f)
            {
                if (IsValid(_laneMarker)) Destroy(_laneMarker);
                Destroy(this);
                return;
            }
            if (CombatManager.Controller != null && CombatManager.Controller.ZaWarudoTimer > 0f) return;

            if (!_launched)
            {
                _windup -= dt;
                if (_windup <= 0f)
                {
                    _launched = true;
                    if (IsValid(_laneMarker)) Destroy(_laneMarker);
                    SoundManager.Play3D("mithrix_slam", Position, g.PlayerPos, 60f, 0.80f, 0.92f);
                }
                return;
            }

            _travelTimer -= dt;
            Position += _moveDir * 24.5f * dt;
            float fy = Physics.GetSurfaceHeight(Position.X, Position.Z, Position.Y + 2.5f);
            Y = Lerp(Y, fy, dt * 10f);

            if (!_hitPlayer && !_boss.IsAlly && Physics.Distance2D(g.PlayerPos, Position) <= 2.35f && MathF.Abs(g.PlayerPos.Y - Position.Y) <= 4.6f)
            {
                _hitPlayer = true;
                ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(_boss.Damage * 1.22f, _boss, Position);
            }

            if (_travelTimer <= 0f || MathF.Abs(Position.X) > Physics.WorldBound - 2.0f || MathF.Abs(Position.Z) > Physics.WorldBound - 2.0f)
            {
                new VisualFX(Position + new Vector3(0, 2.0f, 0), Rgb(246, 198, 58), 5.5f, 0.32f);
                Destroy(this);
            }
        }
    }

    public class BinahMeltdownPillar : Entity
    {
        public readonly CombatTarget Boss;
        public float Hp;
        public readonly float MaxHp;
        private float _hitCd = 0f;
        private float _tetherTick = 0.1f;
        private readonly Entity _lockCore;
        private readonly Entity _label;

        public BinahMeltdownPillar(CombatTarget boss, Vector3 floorPos)
            : base(position: floorPos)
        {
            Boss = boss;
            MaxHp = MathF.Max(145f, boss.MaxHp * 0.072f);
            Hp = MaxHp;

            Color black = Rgb(18, 18, 24);
            Color gold = Rgb(246, 198, 58);
            new Entity(parent: this, model: "cube", scale: new(1.45f, 3.4f, 1.45f), position: new(0, 1.70f, 0), color: black);
            new Entity(parent: this, model: "cube", scale: new(1.62f, 0.24f, 1.62f), position: new(0, 0.35f, 0), color: gold, unlit: true);
            new Entity(parent: this, model: "cube", scale: new(1.62f, 0.24f, 1.62f), position: new(0, 3.15f, 0), color: gold, unlit: true);
            _lockCore = new Entity(parent: this, model: "diamond", scale: new(1.15f, 1.55f, 1.15f), position: new(0, 4.15f, 0), color: Rgb(255, 232, 128), unlit: true);

            _label = new Entity(parent: this, position: new Vector3(0, 5.25f, 0), color: gold)
            {
                BillboardText = "★ ПЕЧАТЬ КЛИПОТ [100%] ★"
            };

            WorldContext.Game?.StageEntities.Add(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (!IsValid(Boss) || Boss.Hp <= 0f || Boss.IsAlly)
            {
                Destroy(this);
                return;
            }

            _hitCd = MathF.Max(0f, _hitCd - dt);
            if (IsValid(_lockCore)) _lockCore.RotationY += 165f * dt;

            _tetherTick -= dt;
            if (_tetherTick <= 0f)
            {
                _tetherTick = 0.24f;
                new LightningBeam(Position + new Vector3(0, 4.1f, 0), Boss.Position + new Vector3(0, 2.8f, 0), Rgb(246, 198, 58), 0.22f);
            }

            float dmg = ExpansionBossArenaHelper.CheckPlayerAttackDamage(this, 1.65f, 2.2f, ref _hitCd);
            if (dmg > 0f)
            {
                Hp = MathF.Max(0f, Hp - dmg);
                int pct = Math.Clamp((int)MathF.Ceiling((Hp / MaxHp) * 100f), 0, 100);
                _label.BillboardText = $"★ ПЕЧАТЬ КЛИПОТ [{pct}%] ★";
                if (Hp <= 0f)
                {
                    SoundManager.Play("parry");
                    new VisualFX(Position + new Vector3(0, 2.4f, 0), Rgb(246, 198, 58), 5.0f, 0.34f);
                    Destroy(this);
                }
            }
        }
    }

    // 5. Парящие Монохромные Клинки и Разломы Тёмного Фонтана Рокочущего Рыцаря
    public class KnightOrbitingSwordEntity : Entity
    {
        private readonly CombatTarget _boss;
        private float _orbitAngle;
        private float _waitTimer;
        private float _flashTimer = 0.28f;
        private float _lungeTimer = 0.42f;
        private int _state = 0; // 0 = orbit, 1 = flash lock, 2 = lunge
        private Vector3 _lungeDir = Vector3.UnitZ;
        private bool _hitPlayer = false;
        private readonly Entity _bladeEdge;

        public KnightOrbitingSwordEntity(CombatTarget boss, float startAngle, float lungeDelay)
            : base(position: boss.Position + new Vector3(0, 3.5f, 0))
        {
            _boss = boss;
            _orbitAngle = startAngle;
            _waitTimer = lungeDelay;

            _bladeEdge = new Entity(parent: this, model: "diamond", scale: new(0.46f, 0.14f, 2.55f), color: Color.White, unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(0.30f, 0.20f, 2.32f), color: Rgb(10, 10, 15));
            WorldContext.Game?.StageEntities.Add(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (!IsValid(_boss) || _boss.Hp <= 0f)
            {
                Destroy(this);
                return;
            }
            if (CombatManager.Controller != null && CombatManager.Controller.ZaWarudoTimer > 0f) return;

            if (_state == 0)
            {
                _orbitAngle += 2.2f * dt;
                Vector3 center = _boss.IsAlly ? _boss.Position : g.PlayerPos;
                Position = center + new Vector3(MathF.Cos(_orbitAngle) * 7.8f, 2.85f, MathF.Sin(_orbitAngle) * 7.8f);
                LookAt(center + new Vector3(0, 1.15f, 0));

                _waitTimer -= dt;
                if (_waitTimer <= 0f)
                {
                    _state = 1;
                    if (IsValid(_bladeEdge)) _bladeEdge.Color = Rgb(165, 245, 255);
                    new VisualFX(Position, Color.White, 1.8f, 0.16f);
                }
            }
            else if (_state == 1)
            {
                _flashTimer -= dt;
                Vector3 target = (_boss.IsAlly ? _boss.Position : g.PlayerPos) + new Vector3(0, 1.1f, 0);
                Vector3 diff = target - Position;
                _lungeDir = diff.LengthSquared() > 0.01f ? Vector3.Normalize(diff) : Vector3.UnitZ;
                LookAt(Position + _lungeDir * 4.0f);

                if (_flashTimer <= 0f)
                {
                    _state = 2;
                    SoundManager.Play3D("melee_swing", Position, g.PlayerPos, 50f, 0.78f, 1.28f);
                }
            }
            else
            {
                _lungeTimer -= dt;
                Position += _lungeDir * 46.0f * dt;
                if (!_hitPlayer && !_boss.IsAlly && Vector3.Distance(g.PlayerPos + new Vector3(0, 1.1f, 0), Position) <= 1.85f)
                {
                    _hitPlayer = true;
                    ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(_boss.Damage * 0.68f, _boss, Position);
                }

                if (_lungeTimer <= 0f)
                {
                    new VisualFX(Position, Color.White, 1.6f, 0.16f);
                    Destroy(this);
                }
            }
        }
    }

    public class KnightDarkRiftHazard : Entity
    {
        private readonly CombatTarget _boss;
        private readonly Vector3 _dir;
        private readonly float _halfLen;
        private float _delay;

        public KnightDarkRiftHazard(CombatTarget boss, Vector3 centerFloor, float angleRad, float length = 22.0f, float delay = 0.92f)
            : base(position: centerFloor + new Vector3(0, 0.14f, 0))
        {
            _boss = boss;
            _dir = new Vector3(MathF.Sin(angleRad), 0f, MathF.Cos(angleRad));
            _halfLen = length * 0.5f;
            _delay = delay;
            RotationY = angleRad * (180f / MathF.PI);

            new Entity(parent: this, model: "cube", scale: new(1.55f, 0.12f, length), color: Color.White, unlit: true);
            new Entity(parent: this, model: "cube", scale: new(0.86f, 0.18f, length * 0.96f), color: Rgb(10, 10, 15));
            WorldContext.Game?.StageEntities.Add(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (!IsValid(_boss) || _boss.Hp <= 0f)
            {
                Destroy(this);
                return;
            }
            if (CombatManager.Controller != null && CombatManager.Controller.ZaWarudoTimer > 0f) return;

            _delay -= dt;
            if (_delay <= 0f)
            {
                SoundManager.Play3D("explosion", Position, g.PlayerPos, 60f, 0.80f, 1.15f);
                for (int s = -2; s <= 2; s++)
                {
                    Vector3 pt = Position + _dir * (s * (_halfLen * 0.42f));
                    new LightningBeam(pt, pt + new Vector3(0, 16.0f, 0), Color.White, 0.42f);
                    new VisualFX(pt + new Vector3(0, 1.0f, 0), Rgb(185, 245, 255), 3.6f, 0.28f);
                }

                if (!_boss.IsAlly)
                {
                    Vector3 rel = g.PlayerPos - Position;
                    rel.Y = 0f;
                    float along = Vector3.Dot(rel, _dir);
                    Vector3 closest = Position + _dir * Math.Clamp(along, -_halfLen, _halfLen);
                    if (Physics.Distance2D(g.PlayerPos, closest) <= 1.95f && MathF.Abs(g.PlayerPos.Y - Position.Y) <= 4.0f)
                        ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(_boss.Damage * 1.12f, _boss, closest);
                }

                // Звёздные осколки Фонтана из центра разлома
                for (int i = 0; i < 4; i++)
                {
                    float a = (i / 4f) * MathF.PI * 2f + 0.4f;
                    new Projectile(Position + new Vector3(0, 1.2f, 0), new Vector3(MathF.Cos(a), 0.08f, MathF.Sin(a)),
                        28f, _boss.Damage * 0.65f, 0.38f, Color.White, isPlayer: _boss.IsAlly, canProc: false);
                }

                Destroy(this);
            }
        }
    }

    // 6. Купол Гармонии для Великого Септимия («День Седьмой»)
    public class HarmonySafeDomeEntity : Entity
    {
        private static readonly List<HarmonySafeDomeEntity> _activeDomes = new();

        public readonly CombatTarget Boss;
        public readonly float Radius;
        private float _lifeTimer;
        private bool _grantedEntryShield = false;
        private readonly Entity _choirRing;
        private readonly HashSet<Entity> _dampenedProjectiles = new();

        public static float GetPlayerDamageMultiplier(Vector3 playerPos)
        {
            for (int i = _activeDomes.Count - 1; i >= 0; i--)
            {
                var dome = _activeDomes[i];
                if (!IsValid(dome))
                {
                    _activeDomes.RemoveAt(i);
                    continue;
                }
                if (Physics.Distance2D(playerPos, dome.Position) <= dome.Radius &&
                    MathF.Abs(playerPos.Y - dome.Position.Y) <= 5.5f)
                {
                    return 0.15f; // 85% снижение урона внутри Купола Гармонии
                }
            }
            return 1.0f;
        }

        public HarmonySafeDomeEntity(CombatTarget boss, Vector3 floorPos, float duration = 6.2f, float radius = 5.5f)
            : base(position: floorPos)
        {
            Boss = boss;
            Radius = radius;
            _lifeTimer = duration;

            Color cyanShield = Rgb(110, 240, 255, 68);
            Color gold = Rgb(252, 208, 72);

            new Entity(parent: this, model: "sphere", scale: new(radius * 2.0f, radius * 1.55f, radius * 2.0f),
                position: new(0, 0.6f, 0), color: cyanShield, unlit: true);

            _choirRing = new Entity(parent: this, position: new(0, 0.25f, 0));
            for (int i = 0; i < 12; i++)
            {
                float a = (i / 12f) * MathF.PI * 2f;
                new Entity(parent: _choirRing, model: "diamond", scale: new(0.36f, 1.35f, 0.36f),
                    position: new(MathF.Cos(a) * radius, 0.55f, MathF.Sin(a) * radius),
                    color: i % 2 == 0 ? gold : Rgb(110, 240, 255), unlit: true);
            }

            new Entity(parent: this, position: new Vector3(0, radius * 0.92f + 1.0f, 0), color: Rgb(110, 240, 255))
            {
                BillboardText = "♫ КУПОЛ ГАРМОНИИ [УКРОЙТЕСЬ ВНУТРИ!] ♫"
            };

            _activeDomes.Add(this);
            WorldContext.Game?.StageEntities.Add(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            var ctrl = CombatManager.Controller;
            if (g == null || IsDestroyed) return;
            if (!IsValid(Boss) || Boss.Hp <= 0f)
            {
                _activeDomes.Remove(this);
                Destroy(this);
                return;
            }

            _lifeTimer -= dt;
            if (IsValid(_choirRing)) _choirRing.RotationY += 72f * dt;

            // Ослабляем вражеские снаряды, влетающие под купол, на 85%
            for (int i = Engine.AllEntities.Count - 1; i >= 0; i--)
            {
                if (i >= Engine.AllEntities.Count) continue;
                if (Engine.AllEntities[i] is Projectile proj && !proj.IsPlayer && !proj.IsDestroyed && !_dampenedProjectiles.Contains(proj))
                {
                    if (Physics.Distance2D(proj.Position, Position) <= Radius + 0.6f && MathF.Abs(proj.Position.Y - Position.Y) <= 5.5f)
                    {
                        _dampenedProjectiles.Add(proj);
                        proj.Damage *= 0.15f;
                    }
                }
            }

            if (ctrl != null && Physics.Distance2D(g.PlayerPos, Position) <= Radius && MathF.Abs(g.PlayerPos.Y - Position.Y) <= 5.5f)
            {
                if (!_grantedEntryShield)
                {
                    _grantedEntryShield = true;
                    ctrl.Shield = MathF.Min(g.PlayerMaxHp * 1.6f, ctrl.Shield + 120.0f);
                    SoundManager.Play("heal", 0.90f, 1.10f);
                    g.ShowBanner("♫ ЭГИДА ХОРА :: +120 ЩИТА И 85% ЗАЩИТЫ! ♫", Rgb(110, 240, 255), 2.5f);
                }
                else
                {
                    ctrl.Shield = MathF.Min(g.PlayerMaxHp * 1.6f, ctrl.Shield + 18.0f * dt);
                }
            }

            if (_lifeTimer <= 0f)
            {
                _activeDomes.Remove(this);
                Destroy(this);
            }
        }
    }

    public partial class CombatTarget
    {
        private bool _expPhase2Done = false;
        private bool _expPhase3Done = false;
        private float _expAuxTimer = 8.5f;
        private float _expSubTick = 0f;
        private float _expSweepStartYaw = 0f;
        private float _expLunarRuinIcd = 0f;
        private int _expComboStep = 0;
        private ProvidenceShieldPylon? _providencePylon;
        private readonly List<BinahMeltdownPillar> _binahLocks = new();
        private readonly List<AurelioniteArenaGeode> _falseSonGeodes = new();
        private readonly List<PrtsMimicDroneNode> _prtsDrones = new();
        private HarmonySafeDomeEntity? _septimusDome;

        internal void InterruptExpansionBossChannel()
        {
            ClearWindup();
            if (TelegraphRing != null && !TelegraphRing.IsDestroyed)
            {
                Destroy(TelegraphRing);
                TelegraphRing = null;
            }
            UpdateHpBillboard();
        }

        private void ApplyExpGroundStep(Vector3 pPos, float dist, float effSpeed, float dt, float t, float stopDist, float colRad = 1.45f, float strideAmp = 26f)
        {
            if (dist > stopDist || MathF.Abs(pPos.Y - Y) > 2.8f)
            {
                Vector3 toP = new(pPos.X - X, 0f, pPos.Z - Z);
                Vector3 dir = toP.LengthSquared() > 0.01f ? Vector3.Normalize(toP) : Forward;
                Vector3 steered = Physics.ComputeSmartSteerDirection(Position, dir, colRad, Y, ref _steerSide);
                Position += steered * effSpeed * dt;
                float stride = MathF.Sin(t * 7.2f) * strideAmp;
                if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = stride;
                if (Parts.RightLeg != null) Parts.RightLeg.RotationX = -stride;
            }
            else
            {
                if (Parts.LeftLeg != null) Parts.LeftLeg.RotationX = Lerp(Parts.LeftLeg.RotationX, 0f, dt * 8f);
                if (Parts.RightLeg != null) Parts.RightLeg.RotationX = Lerp(Parts.RightLeg.RotationX, 0f, dt * 8f);
            }

            Y = Lerp(Y, Physics.GetSurfaceHeight(X, Z, Y + 1.8f), dt * 10f);
            Vector3 res = Physics.ResolveHorizontalCollision(Position, colRad, Y);
            float wb = Physics.WorldBound - 2.2f;
            float cx = Math.Clamp(res.X, -wb, wb);
            float cz = Math.Clamp(res.Z, -wb, wb);
            Physics.ClampToArena(ref cx, ref cz, 2.2f);
            Position = new Vector3(cx, Y, cz);
        }

        private void ApplyExpHoverStep(Vector3 pPos, float dist, float effSpeed, float dt, float t, float optDist, float hoverOffset, float orbitWeight = 0.65f)
        {
            float groundH = Physics.GetSurfaceHeight(X, Z, Y + 3.0f);
            float targetY = MathF.Max(groundH + hoverOffset, pPos.Y + hoverOffset * 0.75f) + MathF.Sin(t * 3.2f) * 0.30f;
            Y = Lerp(Y, targetY, dt * 4.5f);

            Vector3 toP = new(pPos.X - X, 0f, pPos.Z - Z);
            Vector3 dir = toP.LengthSquared() > 0.01f ? Vector3.Normalize(toP) : Forward;
            Vector3 tangent = new(-dir.Z * _strafeDir, 0f, dir.X * _strafeDir);
            Vector3 moveVec = dist > optDist + 2.5f
                ? Vector3.Normalize(dir + tangent * (orbitWeight * 0.45f))
                : (dist < optDist - 3.0f ? Vector3.Normalize(-dir * 0.55f + tangent * orbitWeight) : tangent);

            Vector3 steered = Physics.ComputeSmartSteerDirection(Position, moveVec, 1.55f, groundH, ref _steerSide);
            Position += steered * effSpeed * dt;
            float wb = Physics.WorldBound - 2.8f;
            float cx = Math.Clamp(X, -wb, wb);
            float cz = Math.Clamp(Z, -wb, wb);
            Physics.ClampToArena(ref cx, ref cz, 2.8f);
            Position = new Vector3(cx, Y, cz);
        }

        private bool TryUpdateExpansionBossAI(IPlayerContext g, Vector3 pPos, float dist, float effSpeed, bool projIsPlayer, float dt, float t)
        {
            _expLunarRuinIcd = MathF.Max(0f, _expLunarRuinIcd - dt);
            float hpFrac = Hp / MathF.Max(1f, MaxHp);

            // =========================================================================
            // 1. ECHO OF PROVIDENCE — ИСКАЖЁННЫЙ ПРОВИДЕЦ
            // =========================================================================
            if (EnemyType == "echo_providence")
            {
                LookAt2D(pPos);
                if (Parts.Cape != null) Parts.Cape.RotationX = 12f + MathF.Sin(t * 6.5f) * 7.5f;
                if (Parts.Ring1 != null) Parts.Ring1.RotationZ += 68f * dt;
                if (Parts.PhaseRing != null && Parts.PhaseRing.Enabled) Parts.PhaseRing.RotationY += 135f * dt;

                // Фаза 2 (<65% HP): Щитовой Пилон + Орбитальные золотые мечи
                if (!_expPhase2Done && hpFrac <= 0.65f && !IsAlly)
                {
                    _expPhase2Done = true;
                    Phase = 2;
                    if (Parts.PhaseRing != null) Parts.PhaseRing.Enabled = true;
                    SoundManager.Play("boss_roar");
                    _providencePylon = new ProvidenceShieldPylon(this, 0f);
                    _expAuxTimer = 24.0f;
                    g.ShowBanner("★ ИСКАЖЁННЫЙ ПРОВИДЕЦ ПРИЗЫВАЕТ ЩИТОВОЙ ПИЛОН! ★", Rgb(70, 240, 255), 3.4f);
                }

                // Фаза 3 (<30% HP): Теневые клоны + Небесное рассечение
                if (!_expPhase3Done && hpFrac <= 0.30f && !IsAlly)
                {
                    _expPhase3Done = true;
                    Phase = 3;
                    SoundManager.Play("boss_roar");
                    new VisualFX(Position + new Vector3(0, 2.5f, 0), Rgb(235, 45, 135), 11.0f, 0.45f);
                    g.ShowBanner("★ ФИНАЛЬНАЯ ЭГИДА :: ТЕНЕВЫЕ КЛОНЫ ПРОВИДЦА! ★", Rgb(255, 75, 155), 3.6f);
                    SpecialTimer = 0.6f;
                }

                // Перепризыв щитового пилона на затяжном бою в Фазе 2+
                if (Phase >= 2 && !IsAlly && !IsValid(_providencePylon))
                {
                    ShieldDroneProtected = false;
                    _expAuxTimer -= dt;
                    if (_expAuxTimer <= 0f)
                    {
                        _expAuxTimer = 25.0f;
                        _providencePylon = new ProvidenceShieldPylon(this, ExpansionBossArenaHelper.NextFloat(0f, MathF.PI * 2f));
                        g.ShowBanner("◆ ПРОВИДЕЦ ВОССТАНОВИЛ ЩИТОВОЙ ПИЛОН — СБЕЙТЕ ЕГО! ◆", Rgb(70, 240, 255), 2.8f);
                    }
                }

                // Обработка замахов
                if (WindupType == 1) // Теневой рывок-рассечение
                {
                    WindupTimer -= dt;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, -88f, dt * 10f);
                    if (WindupTimer <= 0f)
                    {
                        Vector3 target = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = Phase >= 3 ? 1.55f : 1.95f;

                        Vector3 dashVec = new(target.X - X, 0f, target.Z - Z);
                        Vector3 dashDir = dashVec.LengthSquared() > 0.01f ? Vector3.Normalize(dashVec) : Forward;
                        SoundManager.Play3D("melee_swing", Position, g.PlayerPos, 58f, 1.0f, 0.92f);
                        new MeleeSlashFX(Position + new Vector3(0, 1.8f, 0), dashDir, RotationY, Rgb(70, 240, 255), 4.8f, 8.0f, -20f, false);

                        Vector3 dest = Physics.ResolveHorizontalCollision(Position + dashDir * 10.5f, 1.35f, Y);
                        Physics.ClampToArena(ref dest.X, ref dest.Z, 2.5f);
                        dest.Y = Physics.GetSurfaceHeight(dest.X, dest.Z, Y + 2.0f);
                        new LightningBeam(Position + new Vector3(0, 2.2f, 0), dest + new Vector3(0, 2.2f, 0), Rgb(235, 55, 145), 0.45f);
                        Position = dest;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = 48f;

                        if (!IsAlly && Physics.Distance2D(g.PlayerPos, Position) <= 5.4f && MathF.Abs(g.PlayerPos.Y - Y) <= 3.5f)
                            ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(Damage * 1.15f, this, Position);

                        Vector3 sp = Position + new Vector3(0, 2.4f, 0);
                        Vector3 aim = (pPos + new Vector3(0, 1.15f, 0)) - sp;
                        foreach (float deg in new[] { -14f, 0f, 14f })
                        {
                            float rad = deg * (MathF.PI / 180f);
                            Vector3 d = new(aim.X * MathF.Cos(rad) - aim.Z * MathF.Sin(rad), aim.Y, aim.X * MathF.Sin(rad) + aim.Z * MathF.Cos(rad));
                            new Projectile(sp, d, 42f, Damage * 0.78f, 0.42f, Rgb(70, 240, 255), isPlayer: projIsPlayer, canProc: false);
                        }
                    }
                    return true;
                }

                if (WindupType == 2) // Золотое сокрушение / Небесные клинки
                {
                    WindupTimer -= dt;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, -142f, dt * 9f);
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                        WindupIndicator.RotationY += 220f * dt;

                    if (WindupTimer <= 0f)
                    {
                        Vector3 slamCenter = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = Phase >= 3 ? 4.8f : 5.8f;

                        if (Phase >= 3)
                        {
                            Position = new Vector3(slamCenter.X, Physics.GetSurfaceHeight(slamCenter.X, slamCenter.Z, pPos.Y + 3.0f), slamCenter.Z);
                            new LightningBeam(Position + new Vector3(0, 32f, 0), Position, Rgb(245, 200, 68), 0.90f);
                        }
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = 52f;
                        SoundManager.Play3D("mithrix_slam", Position, g.PlayerPos, 65f, 0.95f);
                        new VisualFX(Position + new Vector3(0, 1.2f, 0), Rgb(245, 200, 68), 11.5f, 0.42f);
                        new CustomBossShockwaveRing(Position, Damage * 1.18f, Rgb(70, 240, 255), 38.0f, 20.5f, this);

                        if (Phase >= 2)
                        {
                            int swordCount = Phase >= 3 ? 4 : 3;
                            for (int s = 0; s < swordCount; s++)
                            {
                                float ang = (s / (float)swordCount) * MathF.PI * 2f + t;
                                float r = s == 0 ? 1.5f : 5.6f;
                                float sx = pPos.X + MathF.Cos(ang) * r;
                                float sz = pPos.Z + MathF.Sin(ang) * r;
                                Physics.ClampToArena(ref sx, ref sz, 3.0f);
                                float sy = Physics.GetSurfaceHeight(sx, sz, pPos.Y + 2.5f);
                                new DelayedBossStrikeHazard(new Vector3(sx, sy, sz), 0.85f + s * 0.22f, 3.1f, Damage * 0.95f,
                                    Rgb(245, 200, 68), Rgb(70, 240, 255), this);
                            }
                        }
                    }
                    return true;
                }

                ApplyExpGroundStep(pPos, dist, effSpeed * (Phase >= 3 ? 1.18f : 1.0f), dt, t, 5.2f, 1.35f);
                if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, 0f, dt * 6f);

                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && !IsAlly)
                {
                    if (Phase >= 3)
                    {
                        // Призыв 2 теневых клонов с перекрёстным выпадом + прыжок Провидца!
                        foreach (float side in new[] { -1f, 1f })
                        {
                            Vector3 cPos = pPos + Right * (side * 8.2f);
                            float cf = Physics.GetSurfaceHeight(cPos.X, cPos.Z, pPos.Y + 2.0f);
                            new ProvidenceShadowCloneStrike(this, new Vector3(cPos.X, cf, cPos.Z), pPos);
                        }
                    }

                    WindupType = 2;
                    WindupTimer = 0.92f;
                    float fy = Physics.GetSurfaceHeight(pPos.X, pPos.Z, pPos.Y + 1.5f);
                    LockedAimPos = new Vector3(pPos.X, fy, pPos.Z);
                    WindupTag = Phase >= 3 ? "ПРИГОВОР ИСКАЖЁННОГО ПРОВИДЦА!" : "ЗОЛОТОЙ УДАР ПРОВИДЦА!";
                    WindupIndicator = new Entity(model: "diamond", position: (Phase >= 3 ? LockedAimPos : Position) + new Vector3(0, 0.16f, 0),
                        scale: new(9.5f, 0.18f, 9.5f), color: Rgb(245, 200, 68, 165), unlit: true);
                    SoundManager.Play3D("enemy_windup", Position, g.PlayerPos, 55f, 0.85f);
                    UpdateHpBillboard();
                    return true;
                }

                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    WindupType = 1;
                    WindupTimer = 0.66f;
                    LockedAimPos = pPos;
                    WindupTag = "РАССЕЧЕНИЕ ОПЛОТА!";
                    UpdateHpBillboard();
                }
                return true;
            }

            // =========================================================================
            // 2. FALSE SON — ЛОЖНЫЙ СЫН
            // =========================================================================
            if (EnemyType == "false_son_boss")
            {
                if (Parts.Ring1 != null) Parts.Ring1.RotationZ -= (Phase >= 2 ? 120f : 55f) * dt;
                if (Parts.PhaseRing != null && Parts.PhaseRing.Enabled) Parts.PhaseRing.RotationY += 150f * dt;

                // Фаза 2 (<65% HP): Лунная Буря — ускорение, Рывок Братьев и Взор Месяца
                if (!_expPhase2Done && hpFrac <= 0.65f && !IsAlly)
                {
                    _expPhase2Done = true;
                    Phase = 2;
                    if (Parts.PhaseRing != null) Parts.PhaseRing.Enabled = true;
                    SoundManager.Play("boss_roar");
                    new CustomBossShockwaveRing(Position, Damage * 0.95f, Rgb(62, 224, 255), 34.0f, 19.0f, this);
                    g.ShowBanner("★ ЛОЖНЫЙ СЫН ПРОБУЖДАЕТ ЛУННУЮ БУРЮ! ★", Rgb(62, 224, 255), 3.4f);
                }

                // Фаза 3 (<30% HP): Появление 3 Золотых Жеод Аурелионита и Лазер Отца
                if (!_expPhase3Done && hpFrac <= 0.30f && !IsAlly)
                {
                    _expPhase3Done = true;
                    Phase = 3;
                    SoundManager.Play("boss_roar");
                    SpawnFalseSonGeodes();
                    _expAuxTimer = 15.0f;
                    SpecialTimer = 0.8f;
                    g.ShowBanner("★ ГНЕВ НАСЛЕДНИКА :: КОСНИТЕСЬ ЖЕОД АУРЕЛИОНИТА! ★", Rgb(252, 204, 54), 3.8f);
                }

                if (Phase >= 3 && !IsAlly)
                {
                    _falseSonGeodes.RemoveAll(geo => !IsValid(geo));
                    _expAuxTimer -= dt;
                    if (_falseSonGeodes.Count == 0 && _expAuxTimer <= 0f)
                    {
                        _expAuxTimer = 15.0f;
                        SpawnFalseSonGeodes();
                    }
                }

                if (WindupType == 1) // Сокрушение Палицей Отверженного
                {
                    LookAt2D(pPos);
                    WindupTimer -= dt;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, -145f, dt * 9f);
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        AttackTimer = Phase >= 2 ? 1.75f : 2.15f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = 55f;

                        Vector3 impact = Position + Forward * 2.4f;
                        SoundManager.Play3D("mithrix_slam", impact, g.PlayerPos, 65f, 1.0f, 0.88f);
                        new VisualFX(impact + new Vector3(0, 1.0f, 0), Rgb(252, 204, 54), 11.0f, 0.38f);
                        new CustomBossShockwaveRing(impact, Damage * 1.15f, Rgb(252, 204, 54), 36.0f, 19.5f, this);

                        if (!IsAlly && Physics.Distance2D(g.PlayerPos, impact) <= 5.5f && MathF.Abs(g.PlayerPos.Y - Y) <= 3.5f)
                            ApplyFalseSonHitToPlayer(Damage * 1.25f, impact);

                        // Линия лунных разломов вперёд
                        for (int k = 1; k <= 3; k++)
                        {
                            Vector3 fp = Position + Forward * (4.8f * k);
                            Physics.ClampToArena(ref fp.X, ref fp.Z, 2.5f);
                            fp.Y = Physics.GetSurfaceHeight(fp.X, fp.Z, Y + 2.5f);
                            new DelayedBossStrikeHazard(fp, 0.32f * k, 2.9f, Damage * 0.88f, Rgb(62, 224, 255), Rgb(252, 204, 54), this);
                        }
                    }
                    return true;
                }

                if (WindupType == 2) // Взор Осквернённого Месяца / Лазер Отца (дуговой луч)
                {
                    WindupTimer -= dt;
                    if (WindupTimer > 1.35f)
                    {
                        // Фаза зарядки — поворачиваемся к игроку и фиксируем стартовый угол
                        LookAt2D(pPos);
                        _expSweepStartYaw = RotationY - 48f;
                        if (Parts.Crown != null && _expSubTick <= 0f)
                        {
                            _expSubTick = 0.20f;
                            new VisualFX(Position + new Vector3(0, 4.3f, 0), Rgb(252, 204, 54), 2.8f, 0.20f);
                        }
                        _expSubTick -= dt;
                    }
                    else if (WindupTimer > 0f)
                    {
                        float sweepProg = 1.0f - (WindupTimer / 1.35f);
                        float curYaw = _expSweepStartYaw + sweepProg * 96f;
                        RotationY = curYaw;
                        float rad = curYaw * (MathF.PI / 180f);
                        Vector3 beamDir = new(MathF.Sin(rad), 0f, MathF.Cos(rad));
                        Vector3 headOrigin = Position + new Vector3(0, 4.25f, 0);
                        Vector3 groundTarget = Position + beamDir * 24.0f;
                        Physics.ClampToArena(ref groundTarget.X, ref groundTarget.Z, 1.5f);
                        groundTarget.Y = Physics.GetSurfaceHeight(groundTarget.X, groundTarget.Z, pPos.Y + 2.5f) + 0.4f;

                        _expSubTick -= dt;
                        if (_expSubTick <= 0f)
                        {
                            _expSubTick = 0.11f;
                            Color beamCol = Phase >= 3 ? Rgb(252, 204, 54) : Rgb(62, 224, 255);
                            SoundManager.Play3D("laser", headOrigin, g.PlayerPos, 60f, 0.55f, Phase >= 3 ? 0.85f : 1.10f);
                            new LightningBeam(headOrigin, groundTarget, beamCol, Phase >= 3 ? 0.62f : 0.42f);
                            new VisualFX(groundTarget, beamCol, 3.2f, 0.18f);

                            if (!IsAlly && Physics.HasLineOfSight(headOrigin, g.PlayerPos + new Vector3(0, 1.1f, 0)))
                            {
                                Vector3 rel = g.PlayerPos - Position;
                                rel.Y = 0f;
                                float along = Vector3.Dot(rel, beamDir);
                                if (along > 1.0f && along < 26.0f)
                                {
                                    Vector3 closest = Position + beamDir * along;
                                    if (Physics.Distance2D(g.PlayerPos, closest) <= 2.15f && MathF.Abs(g.PlayerPos.Y - groundTarget.Y) <= 3.0f)
                                        ApplyFalseSonHitToPlayer(Damage * (Phase >= 3 ? 0.52f : 0.40f), headOrigin);
                                }
                            }
                        }
                    }
                    else
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = Phase >= 3 ? 5.6f : 6.8f;
                    }
                    return true;
                }

                LookAt2D(pPos);
                float moveMult = Phase >= 2 ? 1.32f : 1.0f;
                ApplyExpGroundStep(pPos, dist, effSpeed * moveMult, dt, t, 4.8f, 1.55f, 24f);
                if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, 0f, dt * 6f);

                // Рывок Братьев (Step of the Brothers) во 2+ фазе на средней дистанции
                if (Phase >= 2 && SecondaryTimer <= 0f && dist is >= 7.5f and <= 24.0f && !IsAlly)
                {
                    SecondaryTimer = 6.2f;
                    SoundManager.Play3D("zap", Position, g.PlayerPos, 55f, 0.90f);
                    Vector3 startP = Position;
                    Vector3 rush = Physics.ResolveHorizontalCollision(Position + Forward * 10.5f, 1.5f, Y);
                    Physics.ClampToArena(ref rush.X, ref rush.Z, 2.5f);
                    rush.Y = Physics.GetSurfaceHeight(rush.X, rush.Z, Y + 2.0f);
                    Position = rush;
                    new LightningBeam(startP + new Vector3(0, 2.5f, 0), rush + new Vector3(0, 2.5f, 0), Rgb(62, 224, 255), 0.55f);
                    new DelayedBossStrikeHazard(startP, 0.65f, 3.2f, Damage * 0.85f, Rgb(62, 224, 255), Rgb(62, 224, 255), this);
                }

                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && Phase >= 2 && !IsAlly)
                {
                    WindupType = 2;
                    WindupTimer = 2.25f;
                    _expSubTick = 0f;
                    WindupTag = Phase >= 3 ? "ЛАЗЕР АУРЕЛИОНИТА (ИЩИТЕ ЖЕОДЫ)!" : "ВЗОР ОСКВЕРНЁННОГО МЕСЯЦА!";
                    SoundManager.Play3D("enemy_windup", Position, g.PlayerPos, 60f, 0.95f);
                    UpdateHpBillboard();
                    return true;
                }

                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    if (dist <= 8.5f || _expComboStep % 2 == 0)
                    {
                        _expComboStep++;
                        WindupType = 1;
                        WindupTimer = 0.84f;
                        WindupTag = "СОКРУШЕНИЕ ОТВЕРЖЕННОГО!";
                        UpdateHpBillboard();
                    }
                    else
                    {
                        _expComboStep++;
                        AttackTimer = 1.85f;
                        if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -68f;
                        SoundManager.Play3D("magic_cast", Position, g.PlayerPos, 55f, 0.85f, 1.15f);
                        Vector3 sp = Position + new Vector3(0, 3.0f, 0) - Right * 1.4f;
                        Vector3 baseAim = (pPos + new Vector3(0, 1.15f, 0)) - sp;
                        foreach (float deg in new[] { -16f, -8f, 0f, 8f, 16f })
                        {
                            float rad = deg * (MathF.PI / 180f);
                            Vector3 d = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                            new Projectile(sp, d, 40f, Damage * 0.72f, 0.40f, Rgb(62, 224, 255), isPlayer: projIsPlayer, canProc: false);
                        }
                    }
                }
                return true;
            }

            // =========================================================================
            // 3. PRTS, SOURCE CODE — PRTS, ИСХОДНЫЙ КОД
            // =========================================================================
            if (EnemyType == "prts_source")
            {
                LookAt2D(pPos);
                float rotMult = Phase >= 2 ? 2.35f : 1.0f;
                if (Parts.Ring1 != null) Parts.Ring1.RotationZ += 65f * rotMult * dt;
                if (Parts.Ring2 != null)
                {
                    Parts.Ring2.RotationY -= 115f * rotMult * dt;
                    Parts.Ring2.RotationX += 42f * rotMult * dt;
                }
                if (Parts.PhaseRing != null && Parts.PhaseRing.Enabled) Parts.PhaseRing.RotationY -= 160f * dt;
                if (Parts.Tentacles != null)
                {
                    Parts.Tentacles.RotationY += 32f * rotMult * dt;
                    for (int i = 0; i < Parts.Tentacles.Children.Count; i++)
                        Parts.Tentacles.Children[i].RotationY += 180f * dt;
                }

                // Фаза 2 (<55% HP): ДИРЕКТИВА EYESOFPRIESTESS — Миметические дроны и Протокол Удаления
                if (!_expPhase2Done && hpFrac <= 0.55f && !IsAlly)
                {
                    _expPhase2Done = true;
                    Phase = 2;
                    if (Parts.PhaseRing != null) Parts.PhaseRing.Enabled = true;
                    SoundManager.Play("ult_cast");
                    SpawnPrtsMimicDrones();
                    _expAuxTimer = 18.0f;
                    SpecialTimer = 1.0f;
                    g.ShowBanner("◇ ДИРЕКТИВА: EYESOFPRIESTESS — ПОЛНАЯ АССИМИЛЯЦИЯ! ◇", Rgb(255, 38, 68), 3.8f);
                }

                if (Phase >= 2 && !IsAlly)
                {
                    _prtsDrones.RemoveAll(d => !IsValid(d));
                    _expAuxTimer -= dt;
                    if (_prtsDrones.Count == 0 && _expAuxTimer <= 0f)
                    {
                        _expAuxTimer = 18.0f;
                        SpawnPrtsMimicDrones();
                    }
                }

                ApplyExpHoverStep(pPos, dist, effSpeed, dt, t, 15.5f, 1.4f, 0.80f);

                if (WindupType == 1) // Матрица разложения PRTS (сетка лазеров + зона ассимиляции)
                {
                    WindupTimer -= dt;
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = Phase >= 2 ? 5.2f : 6.4f;

                        float py = Physics.GetSurfaceHeight(pPos.X, pPos.Z, pPos.Y + 2.0f);
                        Vector3 pFloor = new(pPos.X, py, pPos.Z);
                        new PrtsAssimilationZone(this, pFloor, Phase >= 2 ? 5.8f : 4.8f);

                        int count = Phase >= 2 ? 5 : 4;
                        for (int i = 0; i < count; i++)
                        {
                            float a = (i / (float)count) * MathF.PI * 2f + 0.35f;
                            float r = i == 0 ? 0f : 5.2f;
                            float gx = pFloor.X + MathF.Cos(a) * r;
                            float gz = pFloor.Z + MathF.Sin(a) * r;
                            Physics.ClampToArena(ref gx, ref gz, 2.5f);
                            float gy = Physics.GetSurfaceHeight(gx, gz, py + 2.0f);
                            new DelayedBossStrikeHazard(new Vector3(gx, gy, gz), 0.90f + i * 0.12f, 2.85f, Damage * 0.92f,
                                Rgb(255, 118, 42), Rgb(255, 36, 68), this);
                        }
                    }
                    return true;
                }

                if (WindupType == 2) // Протокол удаления исходного кода (Фаза 2 — багровые секторные лучи + ударные волны)
                {
                    WindupTimer -= dt;
                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = 6.2f;
                        float floorHere = Physics.GetSurfaceHeight(X, Z, Y);
                        SoundManager.Play3D("railgun", Position, g.PlayerPos, 65f, 0.90f);
                        new CustomBossShockwaveRing(new Vector3(X, floorHere, Z), Damage * 1.10f, Rgb(255, 36, 68), 42.0f, 21.0f, this);
                        if (!IsAlly)
                            new LunarPizzaAttack(new Vector3(X, floorHere, Z), Damage * 1.25f, numSlices: 5);
                    }
                    return true;
                }

                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && !IsAlly)
                {
                    if (Phase >= 2 && _expComboStep % 2 == 1)
                    {
                        _expComboStep++;
                        WindupType = 2;
                        WindupTimer = 1.05f;
                        WindupTag = "◇ ПРОТОКОЛ УДАЛЕНИЯ ИСХОДНОГО КОДА! ◇";
                    }
                    else
                    {
                        _expComboStep++;
                        WindupType = 1;
                        WindupTimer = 0.85f;
                        WindupTag = "◇ МАТРИЦА РАЗЛОЖЕНИЯ PRTS! ◇";
                    }
                    SoundManager.Play3D("enemy_windup", Position, g.PlayerPos, 58f, 0.85f, 1.20f);
                    UpdateHpBillboard();
                    return true;
                }

                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    AttackTimer = Phase >= 2 ? 1.55f : 1.95f;
                    SoundManager.Play3D("laser", Position, g.PlayerPos, 55f, 0.65f, 1.25f);
                    Vector3 sp = Position + new Vector3(0, 3.6f, 0);
                    Vector3 right = Right;

                    // Ромбовидный залп Ориджиниума (4 вершины ромба + центральное копье)
                    Vector3[] offsets =
                    {
                        Vector3.Zero,
                        new Vector3(0, 0.95f, 0),
                        new Vector3(0, -0.95f, 0),
                        right * 0.95f,
                        -right * 0.95f
                    };
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        Color pCol = i == 0 ? Rgb(255, 118, 42) : Rgb(255, 36, 68);
                        Vector3 from = sp + offsets[i];
                        Vector3 d = ((pPos + new Vector3(0, 1.15f, 0)) - from);
                        new Projectile(from, d, i == 0 ? 46f : 38f, Damage * 0.72f, 0.36f, pCol, isPlayer: projIsPlayer, canProc: false);
                    }
                }
                return true;
            }

            // =========================================================================
            // 4. ARBITER BINAH — АРБИТР БИНА
            // =========================================================================
            if (EnemyType == "binah_arbiter")
            {
                LookAt2D(pPos);
                if (Parts.Ring1 != null) Parts.Ring1.RotationZ += 72f * dt;
                if (Parts.Cape != null) Parts.Cape.RotationX = 10f + MathF.Sin(t * 5.2f) * 6.0f;
                if (Parts.PhaseRing != null && Parts.PhaseRing.Enabled) Parts.PhaseRing.RotationY += 120f * dt;

                // Фаза 2 (<55% HP): Сингулярности Главы и Деградация Столпа (3 Печати Клипот)
                if (!_expPhase2Done && hpFrac <= 0.55f && !IsAlly)
                {
                    _expPhase2Done = true;
                    Phase = 2;
                    if (Parts.PhaseRing != null) Parts.PhaseRing.Enabled = true;
                    SoundManager.Play("boss_roar");
                    StartBinahMeltdown(g);
                    _expAuxTimer = 24.0f;
                    return true;
                }

                // Активный канал Деградации Клипот (8.5 сек — разбейте 3 золотые печати!)
                if (WindupType == 3)
                {
                    WindupTimer -= dt;
                    _binahLocks.RemoveAll(p => !IsValid(p));
                    ShieldDroneProtected = true;
                    float hoverFloor = Physics.GetSurfaceHeight(X, Z, Y + 3.0f);
                    Y = Lerp(Y, hoverFloor + 1.85f, dt * 5f);

                    if (_binahLocks.Count == 0)
                    {
                        ShieldDroneProtected = false;
                        InterruptExpansionBossChannel();
                        StunTimer = 4.0f;
                        ArmorBreakTimer = 6.0f;
                        TakeDamage(MaxHp * 0.10f);
                        SoundManager.Play("parry");
                        SoundManager.Play3D("explosion", Position, g.PlayerPos, 65f, 1.0f);
                        new VisualFX(Position + new Vector3(0, 2.5f, 0), Rgb(246, 198, 58), 11.0f, 0.45f);
                        g.ShowBanner("✓ ВСЕ ПЕЧАТИ КЛИПОТ СОРВАНЫ — АРБИТР БИНА ОГЛУШЕНА! ✓", Rgb(255, 232, 128), 3.6f);
                        SpecialTimer = 6.5f;
                        return true;
                    }

                    if (WindupTimer <= 0f)
                    {
                        foreach (var rem in _binahLocks)
                            if (IsValid(rem)) Destroy(rem);
                        _binahLocks.Clear();
                        ShieldDroneProtected = false;
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = 6.5f;

                        Vector3 floorOrigin = new(X, hoverFloor, Z);
                        SoundManager.Play3D("mithrix_slam", floorOrigin, g.PlayerPos, 70f, 1.0f, 0.85f);
                        new VisualFX(Position + new Vector3(0, 2.4f, 0), Rgb(246, 198, 58), 16.0f, 0.50f);
                        new CustomBossShockwaveRing(floorOrigin, Damage * 1.65f, Rgb(246, 198, 58), 52.0f, 23.0f, this);
                        for (int i = 0; i < 12; i++)
                        {
                            float a = (i / 12f) * MathF.PI * 2f;
                            new Projectile(Position + new Vector3(0, 2.4f, 0), new Vector3(MathF.Cos(a), 0.03f, MathF.Sin(a)),
                                34f, Damage * 0.90f, 0.46f, Rgb(246, 198, 58), isPlayer: projIsPlayer, canProc: false);
                        }
                        g.ShowBanner("★ ПРИГОВОР ГЛАВЫ ИСПОЛНЕН — ПЕРЕПРЫГНИТЕ ВОЛНУ! ★", Rgb(246, 198, 58), 3.0f);
                    }
                    return true;
                }

                if (Phase >= 2 && !IsAlly)
                {
                    _expAuxTimer -= dt;
                    if (_expAuxTimer <= 0f && WindupType == 0)
                    {
                        _expAuxTimer = 25.0f;
                        StartBinahMeltdown(g);
                        return true;
                    }
                }

                if (WindupType == 1) // «Цепь» (золотая связывающая печать под игроком)
                {
                    WindupTimer -= dt;
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                        WindupIndicator.RotationY += 250f * dt;
                    if (WindupTimer <= 0f)
                    {
                        Vector3 lockCenter = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = Phase >= 2 ? 4.8f : 5.8f;

                        SoundManager.Play3D("scissor_snip", lockCenter, g.PlayerPos, 58f, 0.95f, 0.90f);
                        for (int c = 0; c < 4; c++)
                        {
                            float a = (c / 4f) * MathF.PI * 2f + 0.4f;
                            new LightningBeam(lockCenter + new Vector3(MathF.Cos(a) * 4.8f, 5.5f, MathF.Sin(a) * 4.8f),
                                lockCenter + new Vector3(-MathF.Cos(a) * 4.8f, 0.3f, -MathF.Sin(a) * 4.8f), Rgb(246, 198, 58), 0.34f);
                        }
                        new VisualFX(lockCenter + new Vector3(0, 1.2f, 0), Rgb(246, 198, 58), 8.5f, 0.35f);
                        if (!IsAlly && Physics.Distance2D(g.PlayerPos, lockCenter) <= 4.8f && MathF.Abs(g.PlayerPos.Y - lockCenter.Y) <= 3.8f)
                        {
                            ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(Damage * 1.15f, this, lockCenter);
                            if (CombatManager.Controller != null)
                                CombatManager.Controller.ShiftCd = MathF.Max(CombatManager.Controller.ShiftCd, 1.1f);
                        }
                    }
                    return true;
                }

                ApplyExpGroundStep(pPos, dist, effSpeed, dt, t, 11.0f, 1.2f, 16f);

                // Призыв Столпов Сингулярности («Столп»)
                if (SecondaryTimer <= 0f && !IsAlly)
                {
                    SecondaryTimer = Phase >= 2 ? 5.2f : 6.8f;
                    SoundManager.Play3D("magic_cast", Position, g.PlayerPos, 58f, 0.90f, 0.88f);
                    Vector3 flatAim = new(pPos.X - X, 0f, pPos.Z - Z);
                    Vector3 baseDir = flatAim.LengthSquared() > 0.01f ? Vector3.Normalize(flatAim) : Forward;
                    float[] angles = Phase >= 2 ? new[] { -18f, 0f, 18f } : new[] { 0f };
                    foreach (float deg in angles)
                    {
                        float rad = deg * (MathF.PI / 180f);
                        Vector3 d = new(baseDir.X * MathF.Cos(rad) - baseDir.Z * MathF.Sin(rad), 0f, baseDir.X * MathF.Sin(rad) + baseDir.Z * MathF.Cos(rad));
                        Vector3 sp = Position + Right * (deg * 0.12f) + d * 2.0f;
                        sp.Y = Physics.GetSurfaceHeight(sp.X, sp.Z, Y + 2.0f);
                        new BinahPillarHazard(this, sp, d, 0.82f);
                    }
                }

                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && !IsAlly)
                {
                    WindupType = 1;
                    WindupTimer = 0.88f;
                    float py = Physics.GetSurfaceHeight(pPos.X, pPos.Z, pPos.Y + 1.5f);
                    LockedAimPos = new Vector3(pPos.X, py, pPos.Z);
                    WindupTag = "ЗОЛОТАЯ ЦЕПЬ АРБИТРА!";
                    WindupIndicator = new Entity(model: "diamond", position: LockedAimPos + new Vector3(0, 0.16f, 0),
                        scale: new(9.6f, 0.18f, 9.6f), color: Rgb(246, 198, 58, 170), unlit: true);
                    UpdateHpBillboard();
                    return true;
                }

                // «Фея» (золотые рассекающие нити)
                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    AttackTimer = Phase >= 2 ? 1.55f : 1.90f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -56f;
                    SoundManager.Play3D("scissor_snip", Position, g.PlayerPos, 55f, 0.80f, 1.18f);
                    Vector3 sp = Position + new Vector3(0, 2.5f, 0) + Forward * 1.1f;
                    Vector3 aim = (pPos + new Vector3(0, 1.2f, 0)) - sp;
                    foreach (float deg in new[] { -10f, 0f, 10f })
                    {
                        float rad = deg * (MathF.PI / 180f);
                        Vector3 d = new(aim.X * MathF.Cos(rad) - aim.Z * MathF.Sin(rad), aim.Y, aim.X * MathF.Sin(rad) + aim.Z * MathF.Cos(rad));
                        new Projectile(sp, d, 46f, Damage * 0.78f, 0.36f, Rgb(255, 232, 128), isPlayer: projIsPlayer, canProc: false);
                    }
                }
                return true;
            }

            // =========================================================================
            // 5. THE ROARING KNIGHT — РОКОЧУЩИЙ РЫЦАРЬ
            // =========================================================================
            if (EnemyType == "roaring_knight")
            {
                LookAt2D(pPos);
                if (Parts.Ring1 != null) Parts.Ring1.RotationZ -= 145f * dt;
                if (Parts.Cape != null) Parts.Cape.RotationX = 14f + MathF.Sin(t * 8.5f) * 9.0f;
                if (Parts.PhaseRing != null && Parts.PhaseRing.Enabled) Parts.PhaseRing.RotationY += 190f * dt;

                // Фаза 2 (<55% HP): Рёв Тёмного Фонтана!
                if (!_expPhase2Done && hpFrac <= 0.55f && !IsAlly)
                {
                    _expPhase2Done = true;
                    Phase = 2;
                    if (Parts.PhaseRing != null) Parts.PhaseRing.Enabled = true;
                    SoundManager.Play("boss_roar");
                    new LightningBeam(Position, Position + new Vector3(0, 44.0f, 0), Color.White, 1.15f);
                    new VisualFX(Position + new Vector3(0, 2.5f, 0), Color.White, 14.0f, 0.45f);
                    new CustomBossShockwaveRing(Position, Damage * 1.10f, Color.White, 42.0f, 22.0f, this);
                    SpecialTimer = 0.9f;
                    g.ShowBanner("★ РОКОЧУЩИЙ РЫЦАРЬ ОТКРЫВАЕТ ТЁМНЫЙ ФОНТАН! ★", Color.White, 3.6f);
                }

                if (WindupType == 2) // Нисходящее обрушение из Тёмного Фонтана (Фаза 2)
                {
                    WindupTimer -= dt;
                    if (WindupIndicator != null && !WindupIndicator.IsDestroyed)
                        WindupIndicator.RotationY += 310f * dt;
                    if (WindupTimer <= 0f)
                    {
                        Vector3 land = LockedAimPos;
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = 5.4f;

                        Position = new Vector3(land.X, Physics.GetSurfaceHeight(land.X, land.Z, pPos.Y + 3.0f), land.Z);
                        SoundManager.Play3D("mithrix_slam", Position, g.PlayerPos, 65f, 1.0f, 1.08f);
                        new LightningBeam(Position + new Vector3(0, 36.0f, 0), Position, Color.White, 0.95f);
                        new VisualFX(Position + new Vector3(0, 1.4f, 0), Color.White, 12.0f, 0.38f);
                        new CustomBossShockwaveRing(Position, Damage * 1.20f, Color.White, 38.0f, 22.5f, this);

                        if (!IsAlly && Physics.Distance2D(g.PlayerPos, Position) <= 5.4f && MathF.Abs(g.PlayerPos.Y - Y) <= 3.6f)
                            ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(Damage * 1.25f, this, Position);

                        // Перекрёстные разломы Тёмного Фонтана
                        for (int r = 0; r < 3; r++)
                        {
                            float ang = (r / 3f) * MathF.PI + t * 0.5f;
                            new KnightDarkRiftHazard(this, Position, ang, 24.0f, 0.92f);
                        }
                    }
                    return true;
                }

                ApplyExpGroundStep(pPos, dist, effSpeed * (Phase >= 2 ? 1.22f : 1.0f), dt, t, 4.4f, 1.25f, 30f);

                // Кольцо из 6 парящих монохромных клинков
                if (SecondaryTimer <= 0f && !IsAlly)
                {
                    SecondaryTimer = Phase >= 2 ? 6.5f : 8.2f;
                    SoundManager.Play3D("magic_cast", Position, g.PlayerPos, 58f, 0.88f, 1.25f);
                    for (int s = 0; s < 6; s++)
                    {
                        float a = (s / 6f) * MathF.PI * 2f;
                        new KnightOrbitingSwordEntity(this, a, 0.85f + s * 0.20f);
                    }
                }

                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && !IsAlly)
                {
                    if (Phase >= 2)
                    {
                        WindupType = 2;
                        WindupTimer = 0.88f;
                        float py = Physics.GetSurfaceHeight(pPos.X, pPos.Z, pPos.Y + 2.0f);
                        LockedAimPos = new Vector3(pPos.X, py, pPos.Z);
                        WindupTag = "НИСХОДЯЩИЙ КЛИНОК РОКОТА!";
                        WindupIndicator = new Entity(model: "diamond", position: LockedAimPos + new Vector3(0, 0.16f, 0),
                            scale: new(10.5f, 0.18f, 10.5f), color: Color.White, unlit: true);
                        UpdateHpBillboard();
                        return true;
                    }
                    else
                    {
                        SpecialTimer = 6.2f;
                        float py = Physics.GetSurfaceHeight(pPos.X, pPos.Z, pPos.Y + 2.0f);
                        float baseAng = MathF.Atan2(pPos.X - X, pPos.Z - Z);
                        new KnightDarkRiftHazard(this, new Vector3(pPos.X, py, pPos.Z), baseAng, 22.0f, 0.90f);
                        new KnightDarkRiftHazard(this, new Vector3(pPos.X, py, pPos.Z), baseAng + MathF.PI * 0.5f, 22.0f, 1.05f);
                    }
                }

                // 3-ударное монохромное комбо двуручником
                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    int hitIdx = _expComboStep % 3;
                    _expComboStep++;
                    AttackTimer = hitIdx < 2 ? 0.56f : (Phase >= 2 ? 1.45f : 1.75f);

                    float tilt = hitIdx == 0 ? 24f : (hitIdx == 1 ? -24f : 0f);
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = hitIdx == 1 ? -55f : 55f;
                    SoundManager.Play3D("melee_swing", Position, g.PlayerPos, 55f, 0.90f, 1.05f + hitIdx * 0.08f);
                    new MeleeSlashFX(Position + new Vector3(0, 1.6f, 0), Forward, RotationY, Color.White, 4.6f, 6.2f, tilt, hitIdx == 2);

                    if (dist <= 5.6f && !IsAlly && MathF.Abs(g.PlayerPos.Y - Y) <= 3.4f)
                        ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(Damage * 0.88f, this, Position);

                    Vector3 sp = Position + new Vector3(0, 2.4f, 0) + Forward * 1.2f;
                    Vector3 aim = (pPos + new Vector3(0, 1.15f, 0)) - sp;
                    if (hitIdx < 2)
                    {
                        new Projectile(sp, aim, 48f, Damage * 0.72f, 0.40f, Color.White, isPlayer: projIsPlayer, canProc: false);
                    }
                    else
                    {
                        foreach (float deg in new[] { -15f, 0f, 15f })
                        {
                            float rad = deg * (MathF.PI / 180f);
                            Vector3 d = new(aim.X * MathF.Cos(rad) - aim.Z * MathF.Sin(rad), aim.Y, aim.X * MathF.Sin(rad) + aim.Z * MathF.Cos(rad));
                            new Projectile(sp, d, 46f, Damage * 0.80f, 0.44f, Rgb(185, 245, 255), isPlayer: projIsPlayer, canProc: false);
                        }
                    }
                }
                return true;
            }

            // =========================================================================
            // 6. THE GREAT SEPTIMUS — ВЕЛИКИЙ СЕПТИМИЙ, ХОР БАЛАНСА
            // =========================================================================
            if (EnemyType == "great_septimus")
            {
                LookAt2D(pPos);
                if (Parts.Ring1 != null) Parts.Ring1.RotationZ += 52f * dt;
                if (Parts.Ring2 != null) Parts.Ring2.RotationZ -= 88f * dt;
                if (Parts.Cape != null) Parts.Cape.RotationY = MathF.Sin(t * 2.8f) * 6.5f;
                if (Parts.PhaseRing != null && Parts.PhaseRing.Enabled) Parts.PhaseRing.RotationY += 110f * dt;

                // Фаза 2 (<55% HP): «Эмбрион Философии :: День Седьмой» + Купол Гармонии!
                if (!_expPhase2Done && hpFrac <= 0.55f && !IsAlly)
                {
                    _expPhase2Done = true;
                    Phase = 2;
                    if (Parts.PhaseRing != null) Parts.PhaseRing.Enabled = true;
                    StartSeptimusSeventhDay(g);
                    _expAuxTimer = 26.0f;
                    return true;
                }

                // Гранд-финал «День Седьмой» (5.5с дирижирования метеоритной симфонией; Купол Гармонии даёт 85% защиты и +120 Щита!)
                if (WindupType == 3)
                {
                    WindupTimer -= dt;
                    float baseFloor = Physics.GetSurfaceHeight(X, Z, Y + 4.0f);
                    Y = Lerp(Y, baseFloor + 4.2f, dt * 3.8f);
                    if (Parts.LeftArm != null) Parts.LeftArm.RotationX = -125f;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -110f + MathF.Sin(t * 9.0f) * 32f;

                    _expSubTick -= dt;
                    if (_expSubTick <= 0f && WindupTimer > 0.65f)
                    {
                        _expSubTick = 0.44f;
                        SoundManager.Play3D("magic_cast", Position, g.PlayerPos, 65f, 0.70f, 1.15f);
                        for (int m = 0; m < 2; m++)
                        {
                            Vector3 spot = m == 0
                                ? new Vector3(pPos.X + ExpansionBossArenaHelper.NextFloat(-5.5f, 5.5f), 0f, pPos.Z + ExpansionBossArenaHelper.NextFloat(-5.5f, 5.5f))
                                : ExpansionBossArenaHelper.FindArenaFloorSpot(Position, 5.0f, 24.0f);
                            Physics.ClampToArena(ref spot.X, ref spot.Z, 3.0f);
                            spot.Y = Physics.GetSurfaceHeight(spot.X, spot.Z, pPos.Y + 3.0f);
                            new DelayedBossStrikeHazard(spot, 0.92f, 3.4f, Damage * 0.95f,
                                Rgb(252, 208, 72), Rgb(178, 105, 255), this);
                        }
                    }

                    if (WindupTimer <= 0f)
                    {
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = 6.2f;
                        AttackTimer = 2.0f;

                        SoundManager.Play("ult_cast");
                        SoundManager.Play3D("explosion", Position, g.PlayerPos, 80f, 1.0f, 0.85f);
                        new LightningBeam(Position + new Vector3(0, 40f, 0), new Vector3(X, baseFloor, Z), Rgb(252, 208, 72), 1.35f);
                        new VisualFX(Position + new Vector3(0, 2.0f, 0), Rgb(252, 208, 72), 20.0f, 0.55f);
                        new CustomBossShockwaveRing(new Vector3(X, baseFloor, Z), Damage * 1.45f, Rgb(252, 208, 72), 54.0f, 24.0f, this);

                        if (!IsAlly && Physics.Distance2D(g.PlayerPos, Position) <= 48.0f)
                        {
                            // Вне Купола Гармонии сокрушительный аккорд, внутри Купола 85% поглощения + 120 щита легко блокируют удар!
                            ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(Damage * 2.25f, this, Position);
                        }
                    }
                    return true;
                }

                if (Phase >= 2 && !IsAlly)
                {
                    _expAuxTimer -= dt;
                    if (_expAuxTimer <= 0f && WindupType == 0)
                    {
                        _expAuxTimer = 26.0f;
                        StartSeptimusSeventhDay(g);
                        return true;
                    }
                }

                ApplyExpHoverStep(pPos, dist, effSpeed, dt, t, 14.5f, 0.65f, 0.55f);
                if (Parts.LeftArm != null) Parts.LeftArm.RotationX = Lerp(Parts.LeftArm.RotationX, -28f, dt * 5f);
                if (Parts.RightArm != null) Parts.RightArm.RotationX = Lerp(Parts.RightArm.RotationX, -22f + MathF.Sin(t * 4.5f) * 18f, dt * 6f);

                if (WindupType == 1) // Движение Оркестра (Пиццикато-кольца или Звёздное арпеджио)
                {
                    WindupTimer -= dt;
                    if (Parts.RightArm != null) Parts.RightArm.RotationX = -115f;
                    if (WindupTimer <= 0f)
                    {
                        int mov = WindupSubType;
                        ClearWindup();
                        UpdateHpBillboard();
                        SpecialTimer = Phase >= 2 ? 4.8f : 5.8f;
                        if (Parts.RightArm != null) Parts.RightArm.RotationX = 35f;

                        float groundHere = Physics.GetSurfaceHeight(X, Z, Y + 2.0f);
                        if (mov == 0)
                        {
                            SoundManager.Play3D("shrine_use", Position, g.PlayerPos, 65f, 0.90f, 0.95f);
                            new CustomBossShockwaveRing(new Vector3(X, groundHere, Z), Damage * 1.10f, Rgb(252, 208, 72), 42.0f, 19.5f, this);
                            new DelayedBossStrikeHazard(new Vector3(pPos.X, Physics.GetSurfaceHeight(pPos.X, pPos.Z, pPos.Y + 2.0f), pPos.Z),
                                0.95f, 3.6f, Damage * 1.0f, Rgb(178, 105, 255), Rgb(252, 208, 72), this, spawnMiniRing: true);
                        }
                        else
                        {
                            SoundManager.Play3D("dante_clock", Position, g.PlayerPos, 65f, 0.85f, 1.10f);
                            for (int n = 0; n < 5; n++)
                            {
                                float ang = (n / 5f) * MathF.PI * 2f + t;
                                float r = n == 0 ? 0f : 5.4f;
                                float sx = pPos.X + MathF.Cos(ang) * r;
                                float sz = pPos.Z + MathF.Sin(ang) * r;
                                Physics.ClampToArena(ref sx, ref sz, 2.8f);
                                float sy = Physics.GetSurfaceHeight(sx, sz, pPos.Y + 2.5f);
                                new DelayedBossStrikeHazard(new Vector3(sx, sy, sz), 0.72f + n * 0.20f, 3.0f, Damage * 0.92f,
                                    Rgb(252, 208, 72), Rgb(110, 240, 255), this);
                            }
                        }
                    }
                    return true;
                }

                SpecialTimer -= dt;
                if (SpecialTimer <= 0f && !IsAlly)
                {
                    WindupType = 1;
                    WindupSubType = _expComboStep % 2;
                    _expComboStep++;
                    WindupTimer = 0.90f;
                    WindupTag = WindupSubType == 0 ? "♫ ТАКТ ПЕРВЫЙ :: КОЛЬЦО ПОРЯДКА! ♫" : "♫ ТАКТ ВТОРОЙ :: ЗВЁЗДНОЕ АРПЕДЖИО! ♫";
                    UpdateHpBillboard();
                    return true;
                }

                // Такт Третий — веер гармонических аккордов
                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    AttackTimer = Phase >= 2 ? 1.75f : 2.10f;
                    SoundManager.Play3D("magic_cast", Position, g.PlayerPos, 60f, 0.78f, 1.05f);
                    Vector3 sp = Position + new Vector3(0, 3.5f, 0) + Forward * 1.4f;
                    Vector3 baseAim = (pPos + new Vector3(0, 1.15f, 0)) - sp;
                    int idx = 0;
                    foreach (float deg in new[] { -21f, -14f, -7f, 0f, 7f, 14f, 21f })
                    {
                        float rad = deg * (MathF.PI / 180f);
                        Vector3 d = new(baseAim.X * MathF.Cos(rad) - baseAim.Z * MathF.Sin(rad), baseAim.Y, baseAim.X * MathF.Sin(rad) + baseAim.Z * MathF.Cos(rad));
                        Color noteCol = idx % 2 == 0 ? Rgb(252, 208, 72) : Rgb(178, 105, 255);
                        new Projectile(sp + new Vector3(0, (idx % 2) * 0.45f, 0), d, 35f, Damage * 0.72f, 0.44f, noteCol, isPlayer: projIsPlayer, canProc: false);
                        idx++;
                    }
                }
                return true;
            }

            return false;
        }

        private void ApplyFalseSonHitToPlayer(float dmg, Vector3 sourcePos)
        {
            ExpansionBossArenaHelper.DamagePlayerWithSanctuaryCheck(dmg, this, sourcePos);
            if (_expLunarRuinIcd <= 0f && CombatManager.Controller != null)
            {
                _expLunarRuinIcd = 2.6f;
                CombatManager.Controller.M2Cd = MathF.Min(6.0f, CombatManager.Controller.M2Cd + 1.25f);
            }
        }

        private void SpawnFalseSonGeodes()
        {
            foreach (var old in _falseSonGeodes)
                if (IsValid(old)) Destroy(old);
            _falseSonGeodes.Clear();

            for (int i = 0; i < 3; i++)
            {
                Vector3 spot = ExpansionBossArenaHelper.FindArenaFloorSpot(Position, 9.5f, 20.0f);
                _falseSonGeodes.Add(new AurelioniteArenaGeode(this, spot));
            }
        }

        private void SpawnPrtsMimicDrones()
        {
            foreach (var d in _prtsDrones)
                if (IsValid(d)) Destroy(d);
            _prtsDrones.Clear();

            for (int i = 0; i < 3; i++)
            {
                float a = (i / 3f) * MathF.PI * 2f;
                _prtsDrones.Add(new PrtsMimicDroneNode(this, a, 1.2f + i * 0.85f));
            }
        }

        private void StartBinahMeltdown(IPlayerContext g)
        {
            foreach (var p in _binahLocks)
                if (IsValid(p)) Destroy(p);
            _binahLocks.Clear();

            for (int i = 0; i < 3; i++)
            {
                float a = (i / 3f) * MathF.PI * 2f;
                float px = X + MathF.Cos(a) * 10.5f;
                float pz = Z + MathF.Sin(a) * 10.5f;
                Physics.ClampToArena(ref px, ref pz, 3.5f);
                float py = Physics.GetSurfaceHeight(px, pz, Y + 3.0f);
                _binahLocks.Add(new BinahMeltdownPillar(this, new Vector3(px, py, pz)));
            }

            ShieldDroneProtected = true;
            WindupType = 3;
            WindupTimer = 8.5f;
            WindupTag = "★ ДЕГРАДАЦИЯ КЛИПОТ — РАЗБЕЙТЕ 3 ЗОЛОТЫЕ ПЕЧАТИ! ★";
            SoundManager.Play("ult_cast");
            UpdateHpBillboard();
            g.ShowBanner("★ АРБИТР БИНА: РАЗРУШЬТЕ 3 ПЕЧАТИ КЛИПОТ ЗА 8.5 СЕК! ★", Rgb(246, 198, 58), 4.0f);
        }

        private void StartSeptimusSeventhDay(IPlayerContext g)
        {
            if (IsValid(_septimusDome)) Destroy(_septimusDome);
            Vector3 domeSpot = ExpansionBossArenaHelper.FindArenaFloorSpot(Position, 10.5f, 16.5f);
            _septimusDome = new HarmonySafeDomeEntity(this, domeSpot, duration: 6.4f, radius: 5.5f);

            WindupType = 3;
            WindupTimer = 5.5f;
            _expSubTick = 0.25f;
            WindupTag = "♫ ДЕНЬ СЕДЬМОЙ :: УКРОЙТЕСЬ В КУПОЛЕ ГАРМОНИИ! ♫";
            SoundManager.Play("ult_cast");
            UpdateHpBillboard();
            g.ShowBanner("♫ ВЕЛИКИЙ СЕПТИМИЙ: «ДЕНЬ СЕДЬМОЙ» — ВСТАНЬТЕ В КУПОЛ ГАРМОНИИ! ♫", Rgb(110, 240, 255), 4.2f);
        }
    }
}
