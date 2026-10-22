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
    public class V1Coin : Entity
    {
        public Vector3 Vel;
        public float Lifetime = 4.2f;

        public V1Coin(Vector3 startPos, Vector3 vel)
            : base(model: "diamond", position: startPos, scale: new(0.34f, 0.34f, 0.08f), color: Color.Gold, unlit: true)
        {
            new Entity(parent: this, model: "sphere", scaleUniform: 1.45f, color: Rgb(255, 240, 120, 95), unlit: true);
            Vel = vel;
        }

        public void TriggerRicoshot(float baseDmg, bool isRail = false, int chainCount = 1)
        {
            if (IsDestroyed) return;
            var p = CombatManager.Controller;
            Vector3 myPos = Position;
            IsDestroyed = true;
            new VisualFX(myPos, isRail ? Color.SkyBlue : Color.Gold, 2.4f, 0.25f);

            if (p == null) { Destroy(this); return; }

            // 1. Цепной рикошет в другую монетку
            foreach (var otherCoin in p.V1ActiveCoins.ToArray())
            {
                if (otherCoin != this && IsValid(otherCoin))
                {
                    new LightningBeam(myPos, otherCoin.Position, isRail ? Color.SkyBlue : Color.Gold, 0.26f);
                    otherCoin.TriggerRicoshot(baseDmg * 1.35f, isRail, chainCount + 1);
                    Destroy(this);
                    return;
                }
            }

            // 2. Попадание в ядро дробовика (+CORE NUKE!)
            foreach (var coreProj in p.V1ActiveCores.ToArray())
            {
                if (IsValid(coreProj))
                {
                    new LightningBeam(myPos, coreProj.Position, Color.Red, 0.38f);
                    p.V1DetonateCore(coreProj, isNuke: true);
                    Destroy(this);
                    return;
                }
            }

            // 3. Рикошет в уязвимую точку ближайшего врага
            CombatTarget? bestE = null;
            float bestD = 65f;
            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly)
                {
                    float d = Vector3.Distance(myPos, e.Position);
                    if (d < bestD) { bestD = d; bestE = e; }
                }
            }

            if (bestE != null)
            {
                Vector3 weakpoint = bestE.Position + new Vector3(0, bestE.HitHeight * 1.15f, 0);
                Color beamCol = isRail ? Color.SkyBlue : Color.Gold;
                new LightningBeam(myPos, weakpoint, beamCol, isRail ? 0.35f : 0.24f);
                new VisualFX(weakpoint, beamCol, isRail ? 4.5f : 2.8f, 0.3f);
                CombatManager.ApplyPlayerHitEffects(
                    bestE, weakpoint, baseDmg * (1.25f + 0.35f * chainCount),
                    beamCol, aoe: isRail ? 5.5f : 2.2f, lifesteal: 6.0f, canProc: true);

                p.V1AddStyle(isRail ? $"+ УЛЬТРАРИКОШЕТ x{chainCount}" : $"+ РИКОШЕТ x{chainCount}",
                    beamCol, (isRail ? 150f : 85f) * chainCount);
            }
            Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            Vel.Y -= 18.5f * dt;
            Position += Vel * dt;
            RotationX += 520f * dt;
            RotationY += 380f * dt;
            Lifetime -= dt;
            float floorY = Physics.GetSurfaceHeight(X, Z, Y);
            if (Y <= floorY + 0.15f || Lifetime <= 0f)
                Destroy(this);
        }
    }

    public class V1Magnet : Entity
    {
        private readonly CombatTarget? _stuckTarget;
        private float _duration = 4.5f;
        private readonly Entity _coil;

        public V1Magnet(Vector3 pos, CombatTarget? stuckTarget = null) : base(position: pos)
        {
            _stuckTarget = stuckTarget;
            new Entity(parent: this, model: "cube", scale: new(0.25f, 0.85f, 0.25f), position: new(0, 0.4f, 0), color: Rgb(60, 75, 95));
            _coil = new Entity(parent: this, model: "diamond", scale: new(0.65f, 0.35f, 0.65f), position: new(0, 0.85f, 0), color: Color.SkyBlue, unlit: true);
            new Entity(parent: this, model: "sphere", scale: new(9.0f, 0.08f, 9.0f), position: new(0, 0.1f, 0), color: Rgb(60, 200, 255, 55), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            if (IsValid(_stuckTarget))
                Position = _stuckTarget!.Position + new Vector3(0, _stuckTarget.HitHeight * 0.6f, 0);

            _coil.RotationY += 220f * dt;
            _duration -= dt;

            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly && !e.IsBoss)
                {
                    float d = Vector3.Distance(e.Position, Position);
                    if (d > 1.2f && d <= 10.5f)
                    {
                        Vector3 pull = Vector3.Normalize(Position - e.Position) * 7.5f * dt;
                        e.X += pull.X;
                        e.Z += pull.Z;
                    }
                }
            }

            if (_duration <= 0f)
            {
                var p = CombatManager.Controller;
                new VisualFX(Position + new Vector3(0, 0.8f, 0), Color.SkyBlue, 6.5f, 0.35f);
                if (p != null)
                {
                    foreach (var e in CombatManager.Enemies.ToArray())
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(e.Position, Position) <= 6.5f)
                            CombatManager.ApplyPlayerHitEffects(e, Position, p.BaseDamage * 2.4f, Color.SkyBlue, aoe: 0f, lifesteal: 2.0f);
                    p.V1AddStyle("+ ПРОВОДНИК", Color.SkyBlue, 80f);
                }
                Destroy(this);
            }
        }
    }

    public class V1ScrewdriverDrill : Entity
    {
        private readonly CombatTarget _target;
        private float _duration = 4.0f;
        private float _tickTimer = 0f;
        private readonly Entity _drillMesh;

        public V1ScrewdriverDrill(CombatTarget target)
            : base(position: target.Position + new Vector3(0, target.HitHeight, 0))
        {
            _target = target;
            _drillMesh = new Entity(parent: this, model: "diamond", scale: new(0.45f, 0.45f, 1.35f), color: Color.Lime, unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            var p = CombatManager.Controller;
            if (p == null || !IsValid(_target)) { Destroy(this); return; }

            Position = _target.Position + new Vector3(0, _target.HitHeight, 0);
            _drillMesh.RotationZ += 720f * dt;
            _duration -= dt;
            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = 0.25f;
                CombatManager.ApplyPlayerHitEffects(_target, Position, p.BaseDamage * 0.75f, Color.Lime, aoe: 1.5f, lifesteal: 4.5f);
                new VisualFX(Position, Color.Red, 2.2f, 0.2f);
            }
            if (_duration <= 0f) Destroy(this);
        }
    }

    public class SurvivorCombatController
    {
        public readonly GameRunContext Run;
        public string CharKey => Run.CharKey;
        public SurvivorConfig Cfg => SurvivorDatabase.Classes[Run.CharKey];

        public float Shield = 0f;
        public float InfusionBonus = 0f;

        public float M1Cd = 0f;
        public float M2Cd = 0f;
        public float ShiftCd = 0f;
        public float RCd = 0f;

        public int BarrageShots = 0;
        public float BarrageTimer = 0f;
        public float DragonInstallTimer = 0f;
        public float UberTimer = 0f;
        public float OrgiaTimer = 0f;
        public bool ShieldStance = false;

        public bool GrappleActive = false;
        public Vector3 GrappleTarget = Vector3.Zero;
        public float GrappleTimer = 0f;
        public float ScoutSpeedBuff = 0f;

        public readonly List<DummyClone> DummyClones = new();
        public readonly List<MindustryTurret> MindustryTurrets = new();
        public float SkillLockTimer = 0f;

        public float PredatoryTimer = 0f;
        public float OpalTimer = 0f;
        public float SaferSpacesTimer = 0f;
        public float MedkitTimer = 0f;
        public float StationaryTimer = 0f;
        public float BungusTickTimer = 0f;
        public float TeslaTimer = 0.8f;
        public float FrostRelicTimer = 0f;
        public float FrostTickTimer = 0f;

        public readonly Entity BungusRing;
        public readonly Entity FrostRing;

        // Арсенал V1 (ULTRAKILL)
        public int V1Weapon = 1;
        public float V1Coins = 4.0f;
        public float V1RailCharge = 100.0f;
        public bool V1RocketsFrozen = false;
        public float V1ShotgunBoostWindow = 0f;
        public string V1ActiveArm = "feedbacker";
        public float V1ArmCd = 0f;
        public float V1WhiplashCd = 0f;
        public CombatTarget? V1WhiplashTargetEnt;
        public Vector3? V1WhiplashPoint;
        public string? V1WhiplashMode;
        public float V1WhiplashTimer = 0f;
        public float V1Stamina = 3.0f;
        public bool V1Sliding = false;
        public bool V1Slamming = false;
        public float V1StylePoints = 0f;
        public float V1StyleDmgMult = 1.0f;
        public readonly List<(string Text, Color Col, float Timer)> V1StyleLog = new();
        public readonly List<V1Coin> V1ActiveCoins = new();
        public readonly List<Projectile> V1ActiveCores = new();
        public readonly List<V1Magnet> V1Magnets = new();

        public Entity? FpRoot;
        public Entity? FpWeaponHolder;
        public readonly Dictionary<int, Entity> FpWeapons = new();
        public Entity? FpNailRotator;
        public Entity? FpLeftArm;
        public Entity? FpArmForearm;
        public Entity? FpArmFist;

        private int _shootSide = 0;
        private static readonly Random _rng = new();

        public SurvivorCombatController(GameRunContext run)
        {
            Run = run;
            CombatManager.Controller = this;

            BungusRing = new Entity(parent: run.PlayerEntity, model: "sphere", scale: new(11.0f, 0.12f, 11.0f),
                position: new(0, 0.08f, 0), color: Rgb(85, 255, 120, 65), unlit: true, enabled: false);
            FrostRing = new Entity(parent: run.PlayerEntity, model: "diamond", scale: new(13.0f, 0.22f, 13.0f),
                position: new(0, 0.9f, 0), color: Rgb(110, 235, 255, 90), unlit: true, enabled: false);

            if (run.CharKey == "v1")
                BuildV1FpViewmodel();
        }

        public void BuildV1FpViewmodel()
        {
            if (FpRoot != null && !FpRoot.IsDestroyed) Destroy(FpRoot);
            FpWeapons.Clear();

            FpRoot = new Entity(position: Run.PlayerPos + new Vector3(0, 1.72f, 0));
            Run.StageEntities.Add(FpRoot);
            FpWeaponHolder = new Entity(parent: FpRoot, position: new(0.36f, -0.27f, 0.62f));

            // [1] MARKSMAN REVOLVER
            var w1 = new Entity(parent: FpWeaponHolder, enabled: V1Weapon == 1);
            new Entity(parent: w1, model: "cube", scale: new(0.08f, 0.14f, 0.48f), color: Rgb(38, 45, 62));
            new Entity(parent: w1, model: "cube", scale: new(0.09f, 0.05f, 0.44f), position: new(0, 0.06f, 0), color: Rgb(55, 130, 245));
            new Entity(parent: w1, model: "sphere", scale: new(0.11f, 0.11f, 0.14f), position: new(0, 0.01f, -0.08f), color: Color.Gold, unlit: true);
            new Entity(parent: w1, model: "cube", scale: new(0.04f, 0.04f, 0.50f), position: new(0, 0.09f, 0), color: Color.Gold, unlit: true);
            FpWeapons[1] = w1;

            // [2] CORE EJECT SHOTGUN
            var w2 = new Entity(parent: FpWeaponHolder, enabled: V1Weapon == 2);
            new Entity(parent: w2, model: "cube", scale: new(0.15f, 0.14f, 0.56f), color: Rgb(45, 50, 62));
            foreach (float sx in new[] { -0.04f, 0.04f })
                new Entity(parent: w2, model: "cube", scale: new(0.05f, 0.06f, 0.62f), position: new(sx, 0.04f, 0.04f), color: Rgb(70, 78, 92));
            new Entity(parent: w2, model: "cube", scale: new(0.17f, 0.08f, 0.24f), position: new(0, -0.05f, 0.08f), color: Color.Orange, unlit: true);
            FpWeapons[2] = w2;

            // [3] ATTRACTOR NAILGUN
            var w3 = new Entity(parent: FpWeaponHolder, enabled: V1Weapon == 3);
            new Entity(parent: w3, model: "cube", scale: new(0.18f, 0.16f, 0.46f), color: Rgb(40, 48, 64));
            var barrelHub = new Entity(parent: w3, position: new(0, 0.02f, 0.28f));
            foreach (var (bx, by) in new[] { (-0.04f, -0.04f), (0.04f, -0.04f), (-0.04f, 0.04f), (0.04f, 0.04f) })
                new Entity(parent: barrelHub, model: "cube", scale: new(0.03f, 0.03f, 0.36f), position: new(bx, by, 0), color: Rgb(140, 155, 175));
            new Entity(parent: w3, model: "diamond", scale: new(0.20f, 0.12f, 0.20f), position: new(0, 0.09f, 0.05f), color: Color.SkyBlue, unlit: true);
            FpNailRotator = barrelHub;
            FpWeapons[3] = w3;

            // [4] ELECTRIC RAILCANNON
            var w4 = new Entity(parent: FpWeaponHolder, enabled: V1Weapon == 4);
            new Entity(parent: w4, model: "cube", scale: new(0.14f, 0.16f, 0.68f), color: Rgb(32, 42, 65));
            foreach (float rz in new[] { -0.12f, 0.06f, 0.24f })
                new Entity(parent: w4, model: "diamond", scale: new(0.22f, 0.22f, 0.06f), position: new(0, 0.02f, rz), color: Color.SkyBlue, unlit: true);
            new Entity(parent: w4, model: "cube", scale: new(0.06f, 0.06f, 0.74f), position: new(0, 0.04f, 0), color: Color.White, unlit: true);
            FpWeapons[4] = w4;

            // [5] FREEZEFRAME ROCKET LAUNCHER
            var w5 = new Entity(parent: FpWeaponHolder, enabled: V1Weapon == 5);
            new Entity(parent: w5, model: "cube", scale: new(0.18f, 0.18f, 0.62f), color: Rgb(145, 35, 45));
            new Entity(parent: w5, model: "diamond", scale: new(0.22f, 0.22f, 0.16f), position: new(0, 0, 0.30f), color: Rgb(55, 62, 75));
            new Entity(parent: w5, model: "sphere", scale: new(0.12f, 0.12f, 0.24f), position: new(-0.10f, 0.04f, 0.02f), color: Color.SkyBlue, unlit: true);
            FpWeapons[5] = w5;

            // Левая механическая рука (Feedbacker / Knuckleblaster / Whiplash)
            FpLeftArm = new Entity(parent: FpRoot, position: new(-0.38f, -0.34f, 0.35f), rotation: new(-12, 18, 0));
            FpArmForearm = new Entity(parent: FpLeftArm, model: "cube", scale: new(0.11f, 0.11f, 0.46f), color: Rgb(45, 115, 235));
            FpArmFist = new Entity(parent: FpLeftArm, model: "diamond", scale: new(0.17f, 0.17f, 0.22f), position: new(0, 0, 0.26f), color: Color.SkyBlue, unlit: true);
        }

        public void V1SwitchWeapon(int slot)
        {
            if (slot < 1 || slot > 5) return;
            V1Weapon = slot;
            foreach (var kv in FpWeapons)
                if (kv.Value != null && !kv.Value.IsDestroyed)
                    kv.Value.Enabled = kv.Key == slot;
            if (FpWeaponHolder != null && !FpWeaponHolder.IsDestroyed)
            {
                FpWeaponHolder.Y = -0.36f;
                FpWeaponHolder.AnimatePosition(new Vector3(FpWeaponHolder.X, -0.27f, FpWeaponHolder.Z), 0.10f);
            }
        }

        public void V1AnimateWeaponRecoil(float kickZ = -0.14f, float pitch = -14f)
        {
            if (FpWeaponHolder != null && !FpWeaponHolder.IsDestroyed)
            {
                FpWeaponHolder.Z = 0.62f + kickZ;
                FpWeaponHolder.RotationX = pitch;
                FpWeaponHolder.AnimatePosition(new Vector3(FpWeaponHolder.X, FpWeaponHolder.Y, 0.62f), 0.12f);
                FpWeaponHolder.AnimateRotation(Vector3.Zero, 0.14f);
            }
        }

        public void V1AnimateLeftPunch(Color armCol)
        {
            if (FpLeftArm != null && !FpLeftArm.IsDestroyed && FpArmFist != null)
            {
                FpArmFist.Color = armCol;
                FpLeftArm.Position = new Vector3(-0.12f, -0.18f, 0.85f);
                FpLeftArm.AnimatePosition(new Vector3(-0.38f, -0.34f, 0.35f), 0.18f);
            }
        }

        public float BaseDamage
        {
            get
            {
                float raw = Cfg.Damage;
                float glassMult = MathF.Pow(2.0f, CombatManager.GetItemCount("shaped_glass"));
                float tonicMult = 1.0f + 0.25f * CombatManager.GetItemCount("spinel_tonic");
                float orgiaMult = OrgiaTimer > 0f ? 1.35f : 1.0f;
                float uberMult = UberTimer > 0f ? 1.35f : 1.0f;
                return raw * glassMult * tonicMult * orgiaMult * uberMult;
            }
        }

        public float MoveSpeed
        {
            get
            {
                float diMult = DragonInstallTimer > 0f ? 1.45f : 1.0f;
                float orgiaMult = OrgiaTimer > 0f ? 1.45f : 1.0f;
                float uberMult = UberTimer > 0f ? 1.25f : 1.0f;
                float stanceMult = ShieldStance ? 0.65f : 1.0f;
                float scoutMult = ScoutSpeedBuff > 0f ? 1.35f : 1.0f;
                float tonicMult = 1.0f + 0.25f * CombatManager.GetItemCount("spinel_tonic");
                float fluxPen = MathF.Pow(0.70f, CombatManager.GetItemCount("lunar_pauldron"));
                float spdBonus = 1.0f
                    + 0.14f * CombatManager.GetItemCount("hoof")
                    + 0.08f * CombatManager.GetItemCount("energy_drink")
                    + 0.12f * CombatManager.GetItemCount("red_whip");
                return Cfg.Speed * spdBonus * diMult * orgiaMult * uberMult * stanceMult * scoutMult * tonicMult * fluxPen;
            }
        }

        public float FireRate
        {
            get
            {
                float diMult = DragonInstallTimer > 0f ? 1.65f : 1.0f;
                float orgiaMult = OrgiaTimer > 0f ? 1.50f : 1.0f;
                float uberMult = UberTimer > 0f ? 1.50f : 1.0f;
                float stanceMult = ShieldStance ? 1.45f : 1.0f;
                float tonicMult = 1.0f + 0.25f * CombatManager.GetItemCount("spinel_tonic");
                float predBonus = PredatoryTimer > 0f ? 0.12f * CombatManager.GetItemCount("predatory") : 0f;
                return Cfg.FireRate * (1.0f + 0.15f * CombatManager.GetItemCount("syringe") + predBonus)
                       * diMult * orgiaMult * uberMult * stanceMult * tonicMult;
            }
        }

        public float BlockChance
        {
            get
            {
                int stacks = CombatManager.GetItemCount("bear");
                return 1.0f - (1.0f / (1.0f + 0.15f * stacks));
            }
        }

        public float CritChance => MathF.Min(1.0f,
            0.05f
            + 0.10f * CombatManager.GetItemCount("lens")
            + 0.05f * CombatManager.GetItemCount("harvester_scythe")
            + 0.07f * CombatManager.GetItemCount("clover"));

        public Vector3 GetAimDirection()
        {
            float pitchRad = Run.CamPitch * (MathF.PI / 180f);
            float yawRad = Run.PlayerEntity.RotationY * (MathF.PI / 180f);
            return Vector3.Normalize(new Vector3(
                MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                -MathF.Sin(pitchRad),
                MathF.Cos(yawRad) * MathF.Cos(pitchRad)
            ));
        }

        public void V1AddStyle(string text, Color col, float points = 60f)
        {
            V1StylePoints = MathF.Min(1000f, V1StylePoints + points);
            V1StyleLog.Insert(0, (text, col, 3.2f));
            if (V1StyleLog.Count > 6) V1StyleLog.RemoveAt(V1StyleLog.Count - 1);
        }

        public (string Rank, Color Col, float Mult) V1GetStyleRankInfo()
        {
            if (V1StylePoints >= 920) return ("УЛЬТРАКИЛЛ", Rgb(255, 215, 40), 1.50f);
            if (V1StylePoints >= 780) return ("SSS-ШТОРМ", Rgb(255, 70, 90), 1.40f);
            if (V1StylePoints >= 640) return ("SS-САДИЗМ", Rgb(255, 95, 50), 1.32f);
            if (V1StylePoints >= 500) return ("S-ПРЕВОСХОДНО", Rgb(245, 55, 55), 1.25f);
            if (V1StylePoints >= 360) return ("A-АНАРХИЯ", Color.Orange, 1.18f);
            if (V1StylePoints >= 230) return ("B-ЖЕСТОКО", Color.Yellow, 1.12f);
            if (V1StylePoints >= 110) return ("C-ХАОТИЧНО", Color.Lime, 1.06f);
            if (V1StylePoints > 5) return ("D-РАЗРУШИТЕЛЬНО", Color.SkyBlue, 1.02f);
            return ("БЕЗ СТИЛЯ", Color.Gray, 1.0f);
        }

        public void V1DetonateCore(Projectile coreProj, bool isNuke = false)
        {
            if (!IsValid(coreProj)) return;
            Vector3 cPos = coreProj.Position;
            coreProj.SafeDestroy();
            float rad = isNuke ? 14.0f : 7.5f;
            float dmgMult = isNuke ? 5.2f : 2.8f;
            Color col = isNuke ? Color.Red : Color.Orange;
            new VisualFX(cPos, col, rad * 1.35f, 0.45f);
            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(e.Position, cPos) <= rad)
                    CombatManager.ApplyPlayerHitEffects(e, cPos, BaseDamage * dmgMult, col, aoe: 0f, lifesteal: 5.0f);
            }
            if (isNuke) V1AddStyle("+ ЯДЕРНЫЙ ВЗРЫВ!", Color.Red, 175f);
        }

        public int PerformMeleeAttack(
            float reach = 5.2f, float cleaveRadius = 3.4f, float damageMult = 1.0f, Color? slashCol = null,
            float lifesteal = 0f, float stunDur = 0f, float knockback = 0f, bool scissorCross = false, float aoe = 0f)
        {
            Color col = slashCol ?? Color.Orange;
            Vector3 myPos = Run.PlayerPos;
            Vector3 fwd = Run.PlayerForward;
            _shootSide++;
            float tilt = _shootSide % 2 == 0 ? 22f : -22f;

            new MeleeSlashFX(myPos, fwd, Run.PlayerEntity.RotationY, col, cleaveRadius * 1.15f, reach, tilt, scissorCross);

            Vector3 strikeCenter = myPos + fwd * (reach * 0.55f);
            int hitCount = 0;

            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (!IsValid(e) || e.IsAlly) continue;
                if (MathF.Abs((e.Position.Y + e.HitHeight * 0.5f) - (myPos.Y + 1.2f)) > 4.2f) continue;

                float distFromPlayer = Physics.Distance2D(e.Position, myPos);
                float distFromArc = Physics.Distance2D(e.Position, strikeCenter);
                bool inCone = false;

                if (distFromPlayer <= reach + e.HitRadius)
                {
                    Vector3 flatDir = Vector3.Normalize(new Vector3(e.Position.X - myPos.X, 0, e.Position.Z - myPos.Z));
                    float dot = fwd.X * flatDir.X + fwd.Z * flatDir.Z;
                    if (dot >= 0.15f || distFromPlayer <= 2.4f + e.HitRadius) inCone = true;
                }

                if (inCone || distFromArc <= cleaveRadius + e.HitRadius * 0.6f)
                {
                    hitCount++;
                    CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight, 0),
                        BaseDamage * damageMult, col, aoe, lifesteal, canProc: true);
                    if (IsValid(e))
                    {
                        if (stunDur > 0f && !e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, stunDur);
                        if (knockback > 0f && !e.IsBoss)
                        {
                            Vector3 pushP = Physics.ResolveHorizontalCollision(e.Position + fwd * knockback, 0.8f, e.Y);
                            e.X = pushP.X; e.Z = pushP.Z;
                        }
                    }
                }
            }
            return hitCount;
        }

        public Vector3 FindGrappleAnchor(Vector3 startPos, Vector3 aimDir, float maxDist = 45.0f)
        {
            float step = 1.5f;
            Vector3 cur = startPos;
            int steps = (int)(maxDist / step);
            for (int i = 1; i <= steps; i++)
            {
                Vector3 p = startPos + aimDir * (i * step);
                if (Physics.IsInsideSolidObstacle(p, 0.35f)) return p;
                float surfH = Physics.GetSurfaceHeight(p.X, p.Z, p.Y + 1.2f);
                if (p.Y <= surfH + 0.3f) return new Vector3(p.X, surfH + 0.5f, p.Z);
                foreach (var e in CombatManager.Enemies)
                    if (IsValid(e) && Vector3.Distance(p, e.Position + new Vector3(0, e.HitHeight, 0)) <= e.HitRadius + 0.6f)
                        return e.Position + new Vector3(0, e.HitHeight, 0);
                cur = p;
            }
            float floorY = Physics.GetSurfaceHeight(cur.X, cur.Z, cur.Y);
            return new Vector3(cur.X, MathF.Max(floorY + 1.0f, cur.Y), cur.Z);
        }

        private bool V1CheckHitscanCoinOrCore(Vector3 eyePos, Vector3 aimDir, float baseDmg, bool isRail)
        {
            V1ActiveCoins.RemoveAll(c => !IsValid(c));
            foreach (var coin in V1ActiveCoins.ToArray())
            {
                Vector3 toC = coin.Position - eyePos;
                if (toC.Length() <= 45.0f && Vector3.Dot(aimDir, Vector3.Normalize(toC)) >= 0.90f)
                {
                    new LightningBeam(eyePos, coin.Position, isRail ? Color.SkyBlue : Color.Gold, 0.24f);
                    coin.TriggerRicoshot(baseDmg, isRail, 1);
                    return true;
                }
            }

            V1ActiveCores.RemoveAll(c => !IsValid(c));
            foreach (var core in V1ActiveCores.ToArray())
            {
                Vector3 toCore = core.Position - eyePos;
                if (toCore.Length() <= 45.0f && Vector3.Dot(aimDir, Vector3.Normalize(toCore)) >= 0.88f)
                {
                    new LightningBeam(eyePos, core.Position, Color.Red, 0.30f);
                    V1DetonateCore(core, isNuke: true);
                    return true;
                }
            }
            return false;
        }

        public void UseM1Skill()
        {
            Vector3 aimDir = GetAimDirection();
            Vector3 spawnPos = Run.PlayerPos + new Vector3(0, 1.55f, 0) + Run.PlayerForward * 0.95f;

            if (CharKey == "v1")
            {
                float atkSpd = FireRate / Cfg.FireRate;
                Vector3 eyePos = Run.PlayerPos + new Vector3(0, 1.72f, 0);
                Vector3 muzzlePos = eyePos + aimDir * 0.85f + Run.PlayerEntity.Right * 0.28f - new Vector3(0, 0.15f, 0);
                if (V1Weapon == 1)
                {
                    M1Cd = 0.24f / atkSpd;
                    V1AnimateWeaponRecoil(-0.12f, -16f);
                    if (V1CheckHitscanCoinOrCore(eyePos, aimDir, BaseDamage * 2.2f, false)) return;
                    new Projectile(muzzlePos, aimDir, 115f, BaseDamage * 1.25f, 0.22f, Color.Gold, isPlayer: true, lifesteal: 2.0f);
                }
                else if (V1Weapon == 2)
                {
                    M1Cd = 0.62f / atkSpd;
                    V1ShotgunBoostWindow = 0.26f;
                    V1AnimateWeaponRecoil(-0.22f, -24f);
                    new VisualFX(muzzlePos, Color.Orange, 1.4f, 0.14f);
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 spread = new((float)_rng.NextDouble() * 0.12f - 0.06f, (float)_rng.NextDouble() * 0.10f - 0.05f, (float)_rng.NextDouble() * 0.12f - 0.06f);
                        new Projectile(muzzlePos, Vector3.Normalize(aimDir + spread), 72f, BaseDamage * 0.42f, 0.18f, Color.Orange, isPlayer: true, maxLifetime: 0.48f, lifesteal: 0.8f);
                    }
                }
                else if (V1Weapon == 3)
                {
                    M1Cd = 0.075f / atkSpd;
                    V1AnimateWeaponRecoil(-0.05f, -4f);
                    if (FpNailRotator != null && !FpNailRotator.IsDestroyed)
                        FpNailRotator.RotationZ += 55f;
                    Vector3 spread = new((float)_rng.NextDouble() * 0.07f - 0.035f, (float)_rng.NextDouble() * 0.07f - 0.035f, (float)_rng.NextDouble() * 0.07f - 0.035f);
                    var nail = new Projectile(muzzlePos, Vector3.Normalize(aimDir + spread), 68f, BaseDamage * 0.48f, 0.15f, Color.SkyBlue, isPlayer: true, lifesteal: 0.5f);
                    nail.IsV1Nail = true;
                }
                else if (V1Weapon == 4)
                {
                    if (V1RailCharge < 100f)
                    {
                        M1Cd = 0.2f;
                        Run.ShowBanner($"РЕЛЬСОТРОН ПЕРЕЗАРЯЖАЕТСЯ ({(int)V1RailCharge}%)", Color.SkyBlue, 1.0f);
                        return;
                    }
                    V1RailCharge = 0f;
                    M1Cd = 0.45f;
                    V1AnimateWeaponRecoil(-0.28f, -28f);
                    if (V1CheckHitscanCoinOrCore(eyePos, aimDir, BaseDamage * 4.5f, true)) return;
                    new LightningBeam(muzzlePos, eyePos + aimDir * 65f, Color.SkyBlue, 0.42f);
                    new Projectile(muzzlePos, aimDir, 140f, BaseDamage * 4.2f, 0.65f, Color.SkyBlue, isPlayer: true, aoe: 4.5f, pierce: true, lifesteal: 12.0f);
                    V1AddStyle("+ ЗАЛП РЕЛЬСОТРОНА", Color.SkyBlue, 95f);
                }
                else if (V1Weapon == 5)
                {
                    M1Cd = 0.52f / atkSpd;
                    V1AnimateWeaponRecoil(-0.20f, -18f);
                    var rProj = new Projectile(muzzlePos, aimDir, 56f, BaseDamage * 2.35f, 0.38f, Rgb(255, 75, 95), isPlayer: true, aoe: 4.8f, lifesteal: 3.5f);
                    rProj.IsV1Rocket = true;
                }
                return;
            }

            M1Cd = 1.0f / FireRate;
            FirePrimaryProjectile(1.0f, null);
        }

        public void FirePrimaryProjectile(float damageMult = 1.0f, Color? colOverride = null)
        {
            Vector3 aimDir = GetAimDirection();
            Vector3 spawnPos = Run.PlayerPos + new Vector3(0, 1.55f, 0) + Run.PlayerForward * 0.95f;

            if (CharKey == "sol" && colOverride == null)
            {
                float diBonus = DragonInstallTimer > 0f ? 1.25f : 1.0f;
                PerformMeleeAttack(DragonInstallTimer > 0f ? 5.8f : 5.2f, 3.5f, damageMult * diBonus, Rgb(255, 115, 25),
                    lifesteal: DragonInstallTimer > 0f ? 2.2f : 1.0f, knockback: 1.2f, aoe: 2.2f);
                return;
            }
            if (CharKey == "gwen" && colOverride == null)
            {
                PerformMeleeAttack(4.8f, 3.1f, damageMult, Rgb(95, 225, 255), lifesteal: 2.4f, scissorCross: true, aoe: 1.5f);
                return;
            }
            if (CharKey == "enforcer" && colOverride == null)
            {
                float spread = ShieldStance ? 0.032f : 0.075f;
                for (int i = 0; i < 6; i++)
                {
                    Vector3 off = new(((float)_rng.NextDouble() * 2f - 1f) * spread, ((float)_rng.NextDouble() * 1.2f - 0.6f) * spread, ((float)_rng.NextDouble() * 2f - 1f) * spread);
                    new Projectile(spawnPos, Vector3.Normalize(aimDir + off), Cfg.ProjSpeed, BaseDamage * 0.52f * damageMult, 0.20f, Rgb(110, 225, 255), isPlayer: true, pierce: true, maxLifetime: 0.52f);
                }
                new VisualFX(spawnPos, Rgb(120, 225, 255), 1.4f, 0.14f);
                return;
            }
            if (CharKey == "reimu" && colOverride == null)
            {
                foreach (var (idx, sideOff) in new[] { (0, -0.32f), (1, 0.32f) })
                {
                    Vector3 sp = spawnPos + Run.PlayerEntity.Right * sideOff;
                    new Projectile(sp, aimDir, Cfg.ProjSpeed, BaseDamage * 0.65f * damageMult, Cfg.ProjScale, idx == 0 ? Rgb(255, 55, 70) : Color.White, isPlayer: true, aoe: Cfg.Aoe, homing: true);
                }
                return;
            }
            if (CharKey == "mindustry" && colOverride == null)
            {
                foreach (float sideOff in new[] { -0.55f, 0.55f })
                {
                    Vector3 sp = spawnPos + Run.PlayerEntity.Right * sideOff;
                    new Projectile(sp, aimDir, Cfg.ProjSpeed, BaseDamage * 0.58f * damageMult, Cfg.ProjScale, Rgb(255, 215, 65), isPlayer: true, aoe: Cfg.Aoe);
                }
                return;
            }

            Color projCol = colOverride ?? Cfg.VisorColor;
            float allyHeal = CharKey == "medic" ? (12.0f + BaseDamage * 0.85f) : 0f;
            new Projectile(spawnPos, aimDir, Cfg.ProjSpeed, BaseDamage * damageMult, Cfg.ProjScale, projCol,
                isPlayer: true, aoe: Cfg.Aoe, homing: CharKey == "huntress", healAllies: allyHeal);
        }

        public void UseM2Skill()
        {
            if (SkillLockTimer > 0f)
            {
                Run.ShowBanner($"НЕЙРОСЕТЬ ПЕРЕГРУЖЕНА ПОСЛЕ ПЕРЕСЕЛЕНИЯ ({SkillLockTimer:F1}С)!", Color.Red, 1.2f);
                return;
            }

            Vector3 aimDir = GetAimDirection();
            Vector3 spawnPos = Run.PlayerPos + new Vector3(0, 1.55f, 0) + Run.PlayerForward * 1.1f;

            if (CharKey == "v1")
            {
                Vector3 eyePos = Run.PlayerPos + new Vector3(0, 1.72f, 0);
                if (V1Weapon == 1)
                {
                    if (V1Coins < 1.0f) { Run.ShowBanner("НЕТ МОНЕТ (ПЕРЕЗАРЯДКА)", Color.Gold, 0.9f); return; }
                    V1Coins -= 1.0f;
                    M2Cd = 0.16f;
                    Vector3 coinVel = aimDir * 13.5f + new Vector3(0, 10.5f, 0);
                    var coin = new V1Coin(eyePos + aimDir * 0.8f + new Vector3(0, 0.1f, 0), coinVel);
                    V1ActiveCoins.Add(coin);
                    Run.StageEntities.Add(coin);
                }
                else if (V1Weapon == 2)
                {
                    M2Cd = 1.2f;
                    var core = new Projectile(spawnPos, Vector3.Normalize(aimDir + new Vector3(0, 0.28f, 0)), 34f, BaseDamage * 2.8f, 0.52f, Color.Orange, isPlayer: true, aoe: 6.5f, lifesteal: 4.0f)
                    {
                        IsV1Core = true, GravityArc = 1.25f
                    };
                    V1ActiveCores.Add(core);
                }
                else if (V1Weapon == 3)
                {
                    M2Cd = 2.0f;
                    V1Magnets.RemoveAll(m => !IsValid(m));
                    if (V1Magnets.Count >= 3) { Destroy(V1Magnets[0]); V1Magnets.RemoveAt(0); }
                    Vector3 anchor = FindGrappleAnchor(eyePos, aimDir, 32f);
                    CombatTarget? hitE = null;
                    foreach (var e in CombatManager.Enemies)
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(anchor, e.Position + new Vector3(0, e.HitHeight, 0)) <= e.HitRadius + 1.2f)
                        { hitE = e; break; }
                    var mag = new V1Magnet(anchor, hitE);
                    V1Magnets.Add(mag);
                    Run.StageEntities.Add(mag);
                    new LightningBeam(spawnPos, anchor, Color.SkyBlue, 0.14f);
                }
                else if (V1Weapon == 4)
                {
                    if (V1RailCharge < 100f) { Run.ShowBanner($"РЕЛЬСОТРОН ПЕРЕЗАРЯЖАЕТСЯ ({(int)V1RailCharge}%)", Color.Lime, 1.0f); return; }
                    V1RailCharge = 0f;
                    M2Cd = 0.5f;
                    CombatTarget? bestE = null;
                    float bestDot = 0.86f;
                    foreach (var e in CombatManager.Enemies)
                    {
                        if (IsValid(e) && !e.IsAlly)
                        {
                            Vector3 toE = (e.Position + new Vector3(0, e.HitHeight, 0)) - eyePos;
                            if (toE.Length() <= 45f && Vector3.Dot(aimDir, Vector3.Normalize(toE)) > bestDot)
                            { bestDot = Vector3.Dot(aimDir, Vector3.Normalize(toE)); bestE = e; }
                        }
                    }
                    if (bestE != null)
                    {
                        new LightningBeam(spawnPos, bestE.Position + new Vector3(0, bestE.HitHeight, 0), Color.Lime, 0.35f);
                        Run.StageEntities.Add(new V1ScrewdriverDrill(bestE));
                        V1AddStyle("+ БУР-ОТВЁРТКА", Color.Lime, 90f);
                    }
                    else
                    {
                        new Projectile(spawnPos, aimDir, 95f, BaseDamage * 3.2f, 0.55f, Color.Lime, isPlayer: true, aoe: 4.0f, lifesteal: 10.0f);
                    }
                }
                else if (V1Weapon == 5)
                {
                    M2Cd = 0.25f;
                    V1RocketsFrozen = !V1RocketsFrozen;
                    Run.ShowBanner(V1RocketsFrozen ? "СТОП-КАДР АКТИВЕН :: РАКЕТЫ ЗАМОРОЖЕНЫ!" : "СТОП-КАДР СНЯТ :: ЗАЛП ПОШЁЛ!",
                        V1RocketsFrozen ? Color.SkyBlue : Color.Orange, 1.5f);
                    if (!V1RocketsFrozen) V1AddStyle("+ ЗАЛП СТОП-КАДРА", Color.SkyBlue, 75f);
                }
                return;
            }

            float alienCdr = MathF.Pow(0.80f, CombatManager.GetItemCount("alien_head"));
            float cdMult = MathF.Max(0.20f, (1.0f - 0.12f * CombatManager.GetItemCount("backup_mag")) * alienCdr);
            M2Cd = Cfg.Skills["m2"].Cd * cdMult;

            if (CharKey == "commando")
                new Projectile(spawnPos, aimDir, 68f, BaseDamage * 3.0f, 0.48f, Color.SkyBlue, isPlayer: true, pierce: true);
            else if (CharKey == "artificer")
                new Projectile(spawnPos, aimDir, 26f, BaseDamage * 2.8f, 0.95f, Color.SkyBlue, isPlayer: true, aoe: 7.5f);
            else if (CharKey == "huntress")
                new Projectile(spawnPos, aimDir, 48f, BaseDamage * 2.2f, 0.55f, Color.SkyBlue, isPlayer: true, bounces: 5, homing: true);
            else if (CharKey == "buzz")
                new Projectile(spawnPos, aimDir, 74f, BaseDamage * 2.9f, 0.55f, Color.Red, isPlayer: true, aoe: 4.2f, pierce: true);
            else if (CharKey == "sol")
            {
                Run.PlayerVelY = 14.5f; Run.PlayerGrounded = false;
                PerformMeleeAttack(6.5f, 4.5f, 2.8f, Rgb(255, 90, 20), lifesteal: 4.0f, knockback: 3.0f, aoe: 3.5f);
                new Projectile(spawnPos, Run.PlayerForward, 34f, BaseDamage * 1.8f, 0.85f, Rgb(255, 110, 20), isPlayer: true, aoe: 4.2f, pierce: true, maxLifetime: 0.38f);
            }
            else if (CharKey == "gwen")
                PerformMeleeAttack(7.2f, 4.2f, 3.2f, Rgb(155, 240, 255), lifesteal: 6.5f, scissorCross: true, aoe: 2.8f);
            else if (CharKey == "enforcer")
            {
                Run.ApplyDash(Run.PlayerForward * 16.0f, 0.16f);
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 20.0f);
                PerformMeleeAttack(5.6f, 3.8f, 2.6f, Color.SkyBlue, stunDur: 2.0f, knockback: 5.5f, aoe: 2.5f);
            }
            else if (CharKey == "scout")
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector3 off = new((float)_rng.NextDouble() * 0.18f - 0.09f, (float)_rng.NextDouble() * 0.12f - 0.06f, (float)_rng.NextDouble() * 0.18f - 0.09f);
                    new Projectile(spawnPos, Vector3.Normalize(aimDir + off), 66f, BaseDamage * 0.55f, 0.24f, Color.Orange, isPlayer: true, pierce: true, maxLifetime: 0.38f);
                }
                PerformMeleeAttack(4.8f, 3.4f, 1.6f, Color.Orange, knockback: 4.5f, aoe: 2.0f);
                if (!Run.PlayerGrounded || aimDir.Y < -0.15f)
                {
                    Vector3 recoil = -aimDir;
                    Run.PlayerVelY = MathF.Max(14.5f, recoil.Y * 22.0f);
                    Run.PlayerGrounded = false;
                    Run.ApplyDash(new Vector3(recoil.X, 0, recoil.Z) * 24.0f, 0.28f);
                }
            }
            else if (CharKey == "m4a1")
            {
                new VisualFX(spawnPos, Rgb(95, 255, 135), 3.0f, 0.28f);
                new Projectile(spawnPos, aimDir, 58f, BaseDamage * 3.4f, 0.78f, Rgb(95, 255, 135), isPlayer: true, aoe: 6.8f, pierce: true);
                foreach (var cl in DummyClones) if (IsValid(cl)) cl.CommandBurstShot();
            }
            else if (CharKey == "medic")
            {
                Vector3 myPos = Run.PlayerPos + new Vector3(0, 1.4f, 0);
                new LightningBeam(myPos, myPos + aimDir * 26f, Rgb(255, 55, 80), 0.32f);
                Run.HealPlayer(18.0f);
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 20.0f);
                new Projectile(spawnPos, aimDir, 78f, BaseDamage * 2.6f, 0.52f, Rgb(255, 55, 80), isPlayer: true, aoe: 3.5f, pierce: true, lifesteal: 12.0f, healAllies: 35.0f);
            }
            else if (CharKey == "aigis")
                new Projectile(spawnPos, aimDir, 52f, BaseDamage * 3.0f, 0.65f, Rgb(255, 195, 50), isPlayer: true, aoe: 5.2f);
            else if (CharKey == "reimu")
                new Projectile(spawnPos, aimDir, 46f, BaseDamage * 2.4f, 0.68f, Rgb(245, 45, 65), isPlayer: true, aoe: 3.2f, bounces: 6, homing: true);
            else if (CharKey == "mindustry")
                new Projectile(spawnPos, aimDir, 58f, BaseDamage * 2.8f, 0.65f, Rgb(255, 215, 50), isPlayer: true, aoe: 4.8f, pierce: true);
        }

        public void UseShiftSkill()
        {
            if (SkillLockTimer > 0f) return;

            float alienCdr = MathF.Pow(0.80f, CombatManager.GetItemCount("alien_head"));
            float burnCdr = MathF.Pow(0.70f, CombatManager.GetItemCount("afterburner"));
            ShiftCd = MathF.Max(0.25f, Cfg.Skills["shift"].Cd * alienCdr * burnCdr);
            Vector3 myPos = Run.PlayerPos;
            float nrgMult = 1.0f + 0.18f * CombatManager.GetItemCount("energy_drink") + 0.35f * CombatManager.GetItemCount("afterburner");

            if (CharKey == "enforcer")
            {
                ShieldStance = !ShieldStance;
                Run.ShowBanner(ShieldStance ? "СТОЙКА ЩИТА АКТИВНА: БРОНЯ +65% | АТАКА +45%" : "СТОЙКА ЩИТА СНЯТА", Color.SkyBlue, 1.8f);
                if (!ShieldStance) Run.ApplyDash(Run.PlayerForward * 18.0f * nrgMult, 0.18f);
                return;
            }
            if (CharKey == "scout")
            {
                GrappleTarget = FindGrappleAnchor(myPos + new Vector3(0, 1.5f, 0), GetAimDirection(), 45.0f * nrgMult);
                GrappleActive = true;
                GrappleTimer = 0.72f;
                ScoutSpeedBuff = 3.2f;
                new LightningBeam(myPos + new Vector3(0, 1.5f, 0), GrappleTarget, Color.SkyBlue, 0.16f);
                return;
            }
            if (CharKey == "v1")
            {
                if (V1Stamina < 1.0f) { ShiftCd = 0.1f; return; }
                V1Stamina -= 1.0f;
                ShiftCd = 0.12f;
                Run.ApplyDash(Run.PlayerForward * 38.0f * nrgMult, 0.16f);
                return;
            }

            new VisualFX(myPos + new Vector3(0, 1f, 0), Color.SkyBlue, 2.4f, 0.3f);
            if (CharKey == "commando") Run.ApplyDash(Run.PlayerForward * 28.0f * nrgMult, 0.22f);
            else if (CharKey == "artificer")
            {
                Run.PlayerVelY = 16.5f; Run.PlayerGrounded = false;
                Run.ApplyDash(Run.PlayerForward * 18.0f * nrgMult, 0.32f);
            }
            else if (CharKey == "huntress")
            {
                Vector3 blinkDir = GetAimDirection();
                blinkDir.Y = MathF.Max(0f, blinkDir.Y);
                Vector3 targetPos = myPos + Vector3.Normalize(blinkDir) * (11.0f * nrgMult);
                new LightningBeam(myPos + new Vector3(0, 1f, 0), targetPos + new Vector3(0, 1f, 0), Color.SkyBlue, 0.28f);
                Run.PlayerPos = Physics.ResolveHorizontalCollision(targetPos, 0.72f, myPos.Y);
            }
            else if (CharKey == "buzz")
            {
                Run.PlayerVelY = 18.5f; Run.PlayerGrounded = false;
                Run.ApplyDash(Run.PlayerForward * 24.0f * nrgMult, 0.36f);
            }
            else if (CharKey == "sol")
            {
                Run.ApplyDash(Run.PlayerForward * 27.0f * nrgMult, 0.26f);
                PerformMeleeAttack(6.5f, 4.2f, 2.4f, Rgb(255, 100, 20), knockback: 4.0f, aoe: 3.0f);
            }
            else if (CharKey == "gwen")
            {
                Run.ApplyDash(Run.PlayerForward * 22.0f * nrgMult, 0.22f);
                M1Cd = 0f;
                Run.StageEntities.Add(new HallowedMistZone(myPos + Run.PlayerForward * 3.0f, BaseDamage * 0.65f));
            }
            else if (CharKey == "m4a1")
            {
                Run.ApplyDash(Run.PlayerForward * 25.0f * nrgMult, 0.20f);
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 25.0f);
                foreach (var cl in DummyClones) if (IsValid(cl)) cl.CommandBurstShot();
            }
            else if (CharKey == "medic")
            {
                Run.ApplyDash(Run.PlayerForward * 24.0f * nrgMult, 0.22f);
                PerformMeleeAttack(5.5f, 3.8f, 2.2f, Rgb(255, 65, 85), lifesteal: 10.0f, aoe: 2.5f);
                Run.HealPlayer(Run.PlayerMaxHp * 0.25f);
            }
            else if (CharKey == "aigis")
            {
                Run.PlayerVelY = 14.5f; Run.PlayerGrounded = false;
                Run.ApplyDash(Run.PlayerForward * 24.0f * nrgMult, 0.28f);
                OrgiaTimer = 5.5f;
                Run.ShowBanner("РЕЖИМ ОРГИИ :: АТАКА +50% | СКОРОСТЬ +45% | УРОН +35%!", Color.Gold, 2.2f);
            }
            else if (CharKey == "reimu")
            {
                Run.PlayerVelY = 12.5f; Run.PlayerGrounded = false;
                Run.ApplyDash(Run.PlayerForward * 24.0f * nrgMult, 0.24f);
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 20.0f);
                for (int i = 0; i < 4; i++)
                {
                    float ang = (i / 4.0f) * MathF.PI * 2f;
                    new Projectile(myPos + new Vector3(0, 1.5f, 0), Vector3.Normalize(new Vector3(MathF.Cos(ang), 0.2f, MathF.Sin(ang))), 54f, BaseDamage * 1.4f, 0.30f, Rgb(255, 65, 80), isPlayer: true, homing: true);
                }
            }
            else if (CharKey == "mindustry")
            {
                Run.ApplyDash(GetAimDirection() * 28.0f * nrgMult, 0.28f);
                Run.HealPlayer(Run.PlayerMaxHp * 0.15f);
            }
        }

        public void UseRSkill()
        {
            if (SkillLockTimer > 0f) return;

            if (CharKey == "v1")
            {
                V1UseWhiplash();
                return;
            }

            float alienCdr = MathF.Pow(0.80f, CombatManager.GetItemCount("alien_head"));
            float cellCdr = MathF.Pow(0.75f, CombatManager.GetItemCount("lysate_cell"));
            RCd = MathF.Max(0.5f, Cfg.Skills["r"].Cd * alienCdr * cellCdr);
            Vector3 myPos = Run.PlayerPos;
            Vector3 aimDir = GetAimDirection();

            if (CharKey == "commando")
            {
                BarrageShots = 8;
                BarrageTimer = 0.01f;
            }
            else if (CharKey is "artificer" or "aigis")
            {
                if (CharKey == "aigis") Shield = MathF.Min(Run.PlayerMaxHp, Shield + 45.0f);
                int count = CharKey == "aigis" ? 6 : 5;
                for (int i = 0; i < count; i++)
                {
                    Vector3 tPos;
                    if (i < CombatManager.Enemies.Count && IsValid(CombatManager.Enemies[i]) && !CombatManager.Enemies[i].IsAlly)
                        tPos = CombatManager.Enemies[i].Position;
                    else
                    {
                        float ang = (i / (float)count) * MathF.PI * 2f;
                        tPos = myPos + new Vector3(MathF.Cos(ang) * 8.5f, 0, MathF.Sin(ang) * 8.5f);
                    }
                    new OrbitalStrikeFX(tPos, BaseDamage * (CharKey == "aigis" ? 3.2f : 2.2f));
                }
            }
            else if (CharKey == "huntress")
            {
                Run.PlayerVelY = 14.0f; Run.PlayerGrounded = false;
                Run.StageEntities.Add(new ArrowRainZone(myPos + Run.PlayerForward * 12.0f, BaseDamage * 0.75f));
            }
            else if (CharKey == "buzz")
            {
                for (int i = 0; i < 6; i++)
                {
                    float ang = (-30f + i * 12f) * (MathF.PI / 180f);
                    Vector3 f = Run.PlayerForward;
                    Vector3 d = Vector3.Normalize(new Vector3(f.X * MathF.Cos(ang) - f.Z * MathF.Sin(ang), 0.15f, f.X * MathF.Sin(ang) + f.Z * MathF.Cos(ang)));
                    new Projectile(myPos + new Vector3(0, 1.8f, 0), d, 46f, BaseDamage * 2.1f, 0.42f, Rgb(130, 255, 90), isPlayer: true, aoe: 3.8f, homing: true);
                }
            }
            else if (CharKey == "sol")
            {
                DragonInstallTimer = 6.0f;
                new VisualFX(myPos + new Vector3(0, 1.5f, 0), Rgb(255, 60, 20), 11.0f, 0.45f);
                foreach (var e in CombatManager.Enemies.ToArray())
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(e.Position, myPos) <= 11.0f)
                        e.TakeDamage(BaseDamage * 3.4f);
                Run.ShowBanner("ДРАКОНИЙ ИНСТАЛЛ :: ПЛАМЯ ПОРЧИ ПРОБУЖДЕНО!", Rgb(255, 120, 30), 2.5f);
            }
            else if (CharKey == "gwen")
            {
                foreach (float deg in new[] { -16f, -8f, 0f, 8f, 16f })
                {
                    float rad = deg * (MathF.PI / 180f);
                    Vector3 d = Vector3.Normalize(new Vector3(aimDir.X * MathF.Cos(rad) - aimDir.Z * MathF.Sin(rad), aimDir.Y, aimDir.X * MathF.Sin(rad) + aimDir.Z * MathF.Cos(rad)));
                    new Projectile(myPos + new Vector3(0, 1.6f, 0), d, 62f, BaseDamage * 2.4f, 0.46f, Rgb(110, 225, 255), isPlayer: true, aoe: 2.5f, pierce: true, lifesteal: 4.0f);
                }
            }
            else if (CharKey == "enforcer")
            {
                Vector3 tPos = myPos + Run.PlayerForward * 10.0f;
                float fy = Physics.GetSurfaceHeight(tPos.X, tPos.Z, myPos.Y);
                Run.StageEntities.Add(new TearGasZone(new Vector3(tPos.X, fy, tPos.Z), BaseDamage * 0.85f));
                Run.ShowBanner("СЛЕЗОТОЧИВЫЙ ГАЗ :: КОНТРОЛЬ ТОЛПЫ!", Rgb(140, 255, 100), 2.0f);
            }
            else if (CharKey == "scout")
            {
                Vector3 tPos = myPos + Run.PlayerForward * 11.0f;
                float fy = Physics.GetSurfaceHeight(tPos.X, tPos.Z, myPos.Y);
                Run.StageEntities.Add(new IFGZone(new Vector3(tPos.X, fy, tPos.Z), BaseDamage * 0.90f));
                Run.ShowBanner("СКАЛА И КАМЕНЬ! :: ПОЛЕ IFG АКТИВИРОВАНО!", Color.Yellow, 2.2f);
            }
            else if (CharKey == "m4a1")
            {
                DummyClones.RemoveAll(c => !IsValid(c));
                int spawned = 0;
                for (int slot = 0; slot < 4 && DummyClones.Count < 4 && spawned < 2; slot++)
                {
                    if (!DummyClones.Exists(c => c.SlotIdx == slot))
                    {
                        var cl = new DummyClone(myPos, Run.PlayerMaxHp, slot);
                        DummyClones.Add(cl);
                        Run.StageEntities.Add(cl);
                        spawned++;
                    }
                }
                foreach (var c in DummyClones) { c.Hp = c.MaxHp; c.CommandBurstShot(); }
                Run.ShowBanner($"СИСТЕМА DUMMY LINK :: ОТРЯД M4A1 ({DummyClones.Count}/4 КЛОНОВ)!", Rgb(95, 255, 135), 2.6f);
            }
            else if (CharKey == "medic")
            {
                Vector3 eyePos = myPos + new Vector3(0, 1.55f, 0);
                CombatTarget? bestEnemy = null;
                float bestDot = 0.84f;
                foreach (var e in CombatManager.Enemies)
                {
                    if (IsValid(e) && !e.IsAlly && e.EnemyType != "mithrix")
                    {
                        Vector3 toE = (e.Position + new Vector3(0, e.HitHeight, 0)) - eyePos;
                        if (toE.Length() <= 32f && Vector3.Dot(aimDir, Vector3.Normalize(toE)) > bestDot)
                        { bestDot = Vector3.Dot(aimDir, Vector3.Normalize(toE)); bestEnemy = e; }
                    }
                }
                if (bestEnemy != null && (bestEnemy.Hp / MathF.Max(1f, bestEnemy.MaxHp)) <= (bestEnemy.IsBoss ? 0.30f : 0.50f))
                {
                    bestEnemy.ConvertToAlly();
                    new LightningBeam(eyePos, bestEnemy.Position + new Vector3(0, bestEnemy.HitHeight, 0), Color.Lime, 0.45f);
                    Run.ShowBanner("КОНТРОЛЬ РАЗУМА :: ПРОТИВНИК ПЕРЕШЁЛ НА ВАШУ СТОРОНУ!", Color.Lime, 3.2f);
                    return;
                }
                UberTimer = 6.5f;
                Run.HealPlayer(Run.PlayerMaxHp * 0.50f);
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 50.0f);
                Run.ShowBanner("УБЕР-ЗАРЯД АКТИВЕН :: НЕУЯЗВИМОСТЬ И УСИЛЕНИЕ (6.5 СЕК)!", Rgb(255, 75, 90), 3.0f);
            }
            else if (CharKey == "reimu")
            {
                Color[] rainbow = { Rgb(255, 60, 80), Rgb(255, 165, 45), Color.Yellow, Color.Lime, Color.SkyBlue, Rgb(185, 85, 255) };
                for (int i = 0; i < 10; i++)
                {
                    float ang = (i / 10.0f) * MathF.PI * 2f;
                    Vector3 d = Vector3.Normalize(new Vector3(MathF.Cos(ang), 0.25f, MathF.Sin(ang)));
                    new Projectile(myPos + new Vector3(0, 1.8f, 0), d, 50f, BaseDamage * 1.9f, 0.58f, rainbow[i % rainbow.Length], isPlayer: true, aoe: 4.0f, homing: true);
                }
                Run.ShowBanner("СПЕЛЛ-КАРТА :: ПЕЧАТЬ ФАНТАЗИИ!", Rgb(255, 85, 100), 2.5f);
            }
            else if (CharKey == "mindustry")
            {
                float hpCost = Run.PlayerMaxHp * 0.20f;
                if (Run.PlayerHp <= hpCost + 2.0f)
                {
                    RCd = 1.0f;
                    Run.ShowBanner("НЕДОСТАТОЧНО HP ДЛЯ СТРОИТЕЛЬСТВА ТУРЕЛИ (НУЖНО >20% HP)!", Color.Red, 2.0f);
                    return;
                }
                Run.PlayerHp -= hpCost;
                MindustryTurrets.RemoveAll(t => !IsValid(t));
                if (MindustryTurrets.Count >= 4) { Destroy(MindustryTurrets[0]); MindustryTurrets.RemoveAt(0); }
                Vector3 buildPos = myPos + Run.PlayerForward * 4.2f;
                float fy = Physics.GetSurfaceHeight(buildPos.X, buildPos.Z, myPos.Y);
                var turret = new MindustryTurret(new Vector3(buildPos.X, fy, buildPos.Z), Run.PlayerMaxHp, MindustryTurrets.Count);
                MindustryTurrets.Add(turret);
                Run.StageEntities.Add(turret);
                Run.ShowBanner($"РЕЖИМ ПОСТРОЙКИ :: ТУРЕЛЬ «СПЕКТР» [{MindustryTurrets.Count}/4]!", Rgb(255, 215, 65), 2.6f);
            }
        }

        public void V1UseFeedbacker()
        {
            if (V1ArmCd > 0f) return;
            V1ArmCd = 0.36f;
            V1AnimateLeftPunch(Color.SkyBlue);
            Vector3 eyePos = Run.PlayerPos + new Vector3(0, 1.72f, 0);
            Vector3 aimDir = GetAimDirection();

            // 1. Projectile Boost
            if (V1ShotgunBoostWindow > 0f)
            {
                V1ShotgunBoostWindow = 0f;
                new VisualFX(eyePos + aimDir * 1.2f, Color.Gold, 2.2f, 0.2f);
                new Projectile(eyePos + aimDir * 1.0f, aimDir, 96f, BaseDamage * 2.85f, 0.46f, Color.Gold, isPlayer: true, aoe: 5.2f, lifesteal: 4.5f);
                V1AddStyle("+ УСКОРЕНИЕ СНАРЯДА", Color.Gold, 95f);
                return;
            }

            // 2. Fistful of Dollar
            foreach (var coin in V1ActiveCoins.ToArray())
            {
                if (IsValid(coin) && Vector3.Distance(eyePos, coin.Position) <= 5.5f)
                {
                    coin.TriggerRicoshot(BaseDamage * 2.6f, false, 2);
                    V1AddStyle("+ ГОРСТЬ МОНЕТ", Color.Gold, 110f);
                    return;
                }
            }

            // 3. Парирование вражеских снарядов (+PARRY)
            foreach (var ent in Engine.AllEntities.ToArray())
            {
                if (ent is Projectile proj && !proj.IsPlayer && !proj.IsDestroyed && Vector3.Distance(eyePos, proj.Position) <= 6.2f)
                {
                    proj.SafeDestroy();
                    new VisualFX(proj.Position, Color.Yellow, 4.5f, 0.3f);
                    new Projectile(eyePos + aimDir * 1.1f, aimDir, 110f, BaseDamage * 3.8f, 0.58f, Color.Yellow, isPlayer: true, aoe: 6.5f, lifesteal: 15.0f);
                    Run.HealPlayer(Run.PlayerMaxHp);
                    V1Stamina = 3.0f;
                    V1AddStyle("+ ПАРИРОВАНИЕ", Color.Yellow, 130f);
                    Run.ShowBanner("+ ПАРИРОВАНИЕ! (ПОЛНОЕ ИСЦЕЛЕНИЕ И ВЫНОСЛИВОСТЬ)", Color.Yellow, 1.5f);
                    return;
                }
            }

            PerformMeleeAttack(5.5f, 3.5f, 1.5f, Color.SkyBlue, lifesteal: 5.0f);
        }

        public void V1UseKnuckleblaster()
        {
            if (V1ArmCd > 0f) return;
            V1ArmCd = 0.75f;
            V1AnimateLeftPunch(Color.Red);
            new VisualFX(Run.PlayerPos + new Vector3(0, 1.4f, 0) + Run.PlayerForward * 2.2f, Color.Red, 5.8f, 0.32f);
            int hits = PerformMeleeAttack(6.5f, 4.8f, 2.5f, Color.Red, lifesteal: 8.0f, stunDur: 1.2f, knockback: 6.5f, aoe: 4.0f);
            if (hits > 0) V1AddStyle("+ ВЗРЫВНОЙ КУЛАК", Color.Red, 75f);
        }

        public void V1UseWhiplash()
        {
            if (V1WhiplashCd > 0f) return;
            V1WhiplashCd = 1.4f;
            V1AnimateLeftPunch(Color.Lime);
            Vector3 eyePos = Run.PlayerPos + new Vector3(0, 1.72f, 0);
            Vector3 aimDir = GetAimDirection();

            CombatTarget? bestE = null;
            float bestDot = 0.88f;
            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly)
                {
                    Vector3 toE = (e.Position + new Vector3(0, e.HitHeight, 0)) - eyePos;
                    if (toE.Length() <= 46f && Vector3.Dot(aimDir, Vector3.Normalize(toE)) > bestDot)
                    { bestDot = Vector3.Dot(aimDir, Vector3.Normalize(toE)); bestE = e; }
                }
            }

            if (bestE != null)
            {
                new LightningBeam(eyePos, bestE.Position + new Vector3(0, bestE.HitHeight, 0), Color.Lime, 0.22f);
                if (!bestE.IsBoss && bestE.EnemyType is "lemurian" or "wisp" or "beetle" or "jellyfish" or "imp")
                {
                    V1WhiplashTargetEnt = bestE;
                    V1WhiplashMode = "pull_enemy";
                    V1WhiplashTimer = 0.45f;
                    V1AddStyle("+ ИДИ СЮДА!", Color.Lime, 60f);
                }
                else
                {
                    V1WhiplashTargetEnt = bestE;
                    V1WhiplashMode = "pull_self_to_enemy";
                    V1WhiplashTimer = 0.65f;
                    V1AddStyle("+ КРЮК-ХЛЫСТ", Color.Lime, 45f);
                }
            }
            else
            {
                Vector3 anchor = FindGrappleAnchor(eyePos, aimDir, 44f);
                new LightningBeam(eyePos, anchor, Color.Lime, 0.18f);
                V1WhiplashPoint = anchor;
                V1WhiplashMode = "pull_self_to_point";
                V1WhiplashTimer = 0.60f;
            }
        }

        public void Update(float dt, bool isMoving)
        {
            M1Cd = MathF.Max(0f, M1Cd - dt);
            M2Cd = MathF.Max(0f, M2Cd - dt);
            ShiftCd = MathF.Max(0f, ShiftCd - dt);
            RCd = MathF.Max(0f, RCd - dt);
            ScoutSpeedBuff = MathF.Max(0f, ScoutSpeedBuff - dt);
            SkillLockTimer = MathF.Max(0f, SkillLockTimer - dt);
            PredatoryTimer = MathF.Max(0f, PredatoryTimer - dt);
            OpalTimer = MathF.Max(0f, OpalTimer - dt);
            SaferSpacesTimer = MathF.Max(0f, SaferSpacesTimer - dt);

            // Purity (Чистота)
            int purity = CombatManager.GetItemCount("purity");
            if (purity > 0)
            {
                float extra = (1.2f + 0.6f * purity) * dt;
                M2Cd = MathF.Max(0f, M2Cd - extra);
                ShiftCd = MathF.Max(0f, ShiftCd - extra);
                RCd = MathF.Max(0f, RCd - extra);
            }

            // Щит Plasma Shrimp и Transcendence
            float minShrimp = 20.0f * CombatManager.GetItemCount("plasma_shrimp");
            if (CombatManager.GetItemCount("transcendence") > 0)
                minShrimp = MathF.Max(minShrimp, Run.PlayerMaxHp * 0.5f);

            if (Shield > minShrimp) Shield = MathF.Max(minShrimp, Shield - 1.5f * dt);
            else if (Shield < minShrimp) Shield = MathF.Min(minShrimp, Shield + 4.0f * dt);

            // Medkit
            if (MedkitTimer > 0f)
            {
                MedkitTimer -= dt;
                if (MedkitTimer <= 0f && CombatManager.GetItemCount("medkit") > 0)
                {
                    Run.HealPlayer(5.0f + 10.0f * CombatManager.GetItemCount("medkit"));
                    new VisualFX(Run.PlayerPos + new Vector3(0, 1.2f, 0), Color.Lime, 2.2f, 0.25f);
                }
            }

            if (DragonInstallTimer > 0f) { DragonInstallTimer -= dt; Run.HealPlayer(6.0f * dt); }
            if (UberTimer > 0f) { UberTimer -= dt; Run.HealPlayer(12.0f * dt); }
            if (OrgiaTimer > 0f) OrgiaTimer -= dt;

            // Bustling Fungus (Bungus)
            int bungus = CombatManager.GetItemCount("bungus");
            if (!isMoving && Run.PlayerGrounded && Run.DashTimer <= 0f && !GrappleActive)
                StationaryTimer += dt;
            else
                StationaryTimer = 0f;

            if (bungus > 0 && StationaryTimer >= 1.2f)
            {
                float bRad = 5.0f + 1.2f * bungus;
                BungusRing.Enabled = true;
                BungusRing.Scale = new Vector3(bRad * 2f, 0.12f, bRad * 2f);
                BungusTickTimer -= dt;
                if (BungusTickTimer <= 0f)
                {
                    BungusTickTimer = 0.45f;
                    Run.HealPlayer(Run.PlayerMaxHp * (0.045f * bungus * 0.45f) + 2.0f * bungus);
                }
            }
            else BungusRing.Enabled = false;

            // Unstable Tesla Coil
            int tesla = CombatManager.GetItemCount("tesla_coil");
            if (tesla > 0)
            {
                TeslaTimer -= dt;
                if (TeslaTimer <= 0f)
                {
                    TeslaTimer = 1.2f;
                    Vector3 myP = Run.PlayerPos + new Vector3(0, 1.4f, 0);
                    float tRad = 15.0f + 2.5f * tesla;
                    int hitT = 0;
                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(myP, e.Position) <= tRad)
                        {
                            new LightningBeam(myP, e.Position + new Vector3(0, e.HitHeight, 0), Color.SkyBlue, 0.20f);
                            e.TakeDamage(BaseDamage * (1.15f * tesla));
                            if (++hitT >= 2 + tesla) break;
                        }
                    }
                }
            }

            // Frost Relic
            int frost = CombatManager.GetItemCount("frost_relic");
            if (FrostRelicTimer > 0f && frost > 0)
            {
                FrostRelicTimer -= dt;
                float fRad = 6.5f + 1.5f * frost;
                FrostRing.Enabled = true;
                FrostRing.Scale = new Vector3(fRad * 2f, 0.22f, fRad * 2f);
                FrostRing.RotationY += 140f * dt;
                FrostTickTimer -= dt;
                if (FrostTickTimer <= 0f)
                {
                    FrostTickTimer = 0.35f;
                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (IsValid(e) && !e.IsAlly && Physics.Distance2D(Run.PlayerPos, e.Position) <= fRad)
                        {
                            e.SlowTimer = MathF.Max(e.SlowTimer, 1.5f);
                            e.TakeDamage(BaseDamage * (0.65f * frost));
                        }
                    }
                }
            }
            else FrostRing.Enabled = false;

            // Очередь ульты Commando
            if (BarrageShots > 0)
            {
                BarrageTimer -= dt;
                if (BarrageTimer <= 0f)
                {
                    BarrageTimer = 0.065f;
                    BarrageShots--;
                    FirePrimaryProjectile(1.25f, Color.SkyBlue);
                }
            }

            // Крюк-кошка Скаута
            if (GrappleActive)
            {
                GrappleTimer -= dt;
                Vector3 toAnchor = GrappleTarget - (Run.PlayerPos + new Vector3(0, 1.2f, 0));
                if (toAnchor.Length() <= 2.4f || GrappleTimer <= 0f)
                {
                    GrappleActive = false;
                    Run.PlayerVelY = 5.5f;
                    Run.PlayerJumpsLeft = Math.Max(1, Run.PlayerMaxJumps);
                }
                else
                {
                    Run.PlayerPos += Vector3.Normalize(toAnchor) * 52.0f * dt;
                    Run.PlayerVelY = 0f;
                    Run.PlayerGrounded = false;
                }
            }

            // Естественная регенерация + Heart of Tarrasque
            float regen = 1.5f + (Run.PlayerMaxHp * 0.025f * CombatManager.GetItemCount("tarrasque"));
            Run.PlayerHp = MathF.Min(Run.PlayerMaxHp, Run.PlayerHp + regen * dt);

            // Обновление механик V1
            if (CharKey == "v1")
                UpdateV1Mechanics(dt, isMoving);
        }

        private void UpdateV1Mechanics(float dt, bool isMoving)
        {
            V1ArmCd = MathF.Max(0f, V1ArmCd - dt);
            V1WhiplashCd = MathF.Max(0f, V1WhiplashCd - dt);
            V1ShotgunBoostWindow = MathF.Max(0f, V1ShotgunBoostWindow - dt);
            if (!V1Sliding) V1Stamina = MathF.Min(3.0f, V1Stamina + 0.85f * dt);
            V1Coins = MathF.Min(4.0f, V1Coins + 0.30f * dt);
            V1RailCharge = MathF.Min(100.0f, V1RailCharge + 14.0f * dt);

            if (V1StylePoints > 0f) V1StylePoints = MathF.Max(0f, V1StylePoints - 18.0f * dt);
            (_, _, V1StyleDmgMult) = V1GetStyleRankInfo();
            for (int i = V1StyleLog.Count - 1; i >= 0; i--)
            {
                var item = V1StyleLog[i];
                item.Timer -= dt;
                if (item.Timer <= 0f) V1StyleLog.RemoveAt(i);
                else V1StyleLog[i] = item;
            }

            // Покачивание оружия от 1-го лица при ходьбе
            if (FpWeaponHolder != null && !FpWeaponHolder.IsDestroyed)
            {
                if (isMoving && Run.PlayerGrounded)
                {
                    float t = (float)Raylib.GetTime();
                    float bob = MathF.Sin(t * 14.0f) * 0.018f;
                    FpWeaponHolder.X = 0.36f + MathF.Cos(t * 7.0f) * 0.015f;
                    FpWeaponHolder.Y = Lerp(FpWeaponHolder.Y, -0.27f + bob, dt * 12.0f);
                }
                else
                {
                    FpWeaponHolder.X = Lerp(FpWeaponHolder.X, 0.36f, dt * 10.0f);
                    FpWeaponHolder.Y = Lerp(FpWeaponHolder.Y, -0.27f, dt * 10.0f);
                }
            }

            // Переключение оружия V1 (1-5 и колёсико)
            if (Raylib.IsKeyPressed(KeyboardKey.One)) V1SwitchWeapon(1);
            if (Raylib.IsKeyPressed(KeyboardKey.Two)) V1SwitchWeapon(2);
            if (Raylib.IsKeyPressed(KeyboardKey.Three)) V1SwitchWeapon(3);
            if (Raylib.IsKeyPressed(KeyboardKey.Four)) V1SwitchWeapon(4);
            if (Raylib.IsKeyPressed(KeyboardKey.Five)) V1SwitchWeapon(5);
            float wheel = Raylib.GetMouseWheelMove();
            if (wheel > 0.1f) V1SwitchWeapon(V1Weapon >= 5 ? 1 : V1Weapon + 1);
            else if (wheel < -0.1f) V1SwitchWeapon(V1Weapon <= 1 ? 5 : V1Weapon - 1);

            if (Raylib.IsKeyPressed(KeyboardKey.F))
            {
                if (V1ActiveArm == "feedbacker") V1UseFeedbacker();
                else V1UseKnuckleblaster();
            }
            if (Raylib.IsKeyPressed(KeyboardKey.G)) V1UseKnuckleblaster();
            if (Raylib.IsKeyPressed(KeyboardKey.V))
            {
                V1ActiveArm = V1ActiveArm == "feedbacker" ? "knuckleblaster" : "feedbacker";
                Color armCol = V1ActiveArm == "feedbacker" ? Color.SkyBlue : Color.Red;
                if (FpArmForearm != null && FpArmFist != null)
                {
                    FpArmForearm.Color = V1ActiveArm == "feedbacker" ? Rgb(45, 115, 235) : Rgb(210, 40, 45);
                    FpArmFist.Color = armCol;
                }
                Run.ShowBanner($"АКТИВНАЯ РУКА: {(V1ActiveArm == "feedbacker" ? "ФИДБЕКЕР [СИНЯЯ]" : "НАКЛБЛАСТЕР [КРАСНАЯ]")}", armCol, 1.2f);
            }

            if (V1WhiplashTimer > 0f)
            {
                V1WhiplashTimer -= dt;
                if (V1WhiplashMode == "pull_enemy" && IsValid(V1WhiplashTargetEnt))
                {
                    Vector3 dest = Run.PlayerPos + Run.PlayerForward * 2.2f;
                    V1WhiplashTargetEnt!.Position = Lerp(V1WhiplashTargetEnt.Position, dest, dt * 18f);
                }
                else if (V1WhiplashMode == "pull_self_to_enemy" && IsValid(V1WhiplashTargetEnt))
                {
                    Vector3 toT = (V1WhiplashTargetEnt!.Position + new Vector3(0, 1.0f, 0)) - (Run.PlayerPos + new Vector3(0, 1.2f, 0));
                    if (toT.Length() <= 3.2f) { V1WhiplashTimer = 0f; Run.PlayerVelY = 8.0f; }
                    else { Run.PlayerPos += Vector3.Normalize(toT) * 58f * dt; Run.PlayerVelY = 0f; Run.PlayerGrounded = false; }
                }
                else if (V1WhiplashMode == "pull_self_to_point" && V1WhiplashPoint.HasValue)
                {
                    Vector3 toP = V1WhiplashPoint.Value - (Run.PlayerPos + new Vector3(0, 1.2f, 0));
                    if (toP.Length() <= 2.6f) { V1WhiplashTimer = 0f; Run.PlayerVelY = 6.5f; }
                    else { Run.PlayerPos += Vector3.Normalize(toP) * 56f * dt; Run.PlayerVelY = 0f; Run.PlayerGrounded = false; }
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (UberTimer > 0f)
            {
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.4f, 0), Rgb(255, 65, 85), 2.0f, 0.2f);
                return;
            }

            int safer = CombatManager.GetItemCount("safer_spaces");
            if (safer > 0 && SaferSpacesTimer <= 0f)
            {
                SaferSpacesTimer = MathF.Max(2.5f, 12.0f * MathF.Pow(0.85f, safer - 1));
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.3f, 0), Rgb(205, 85, 255), 2.6f, 0.28f);
                Run.ShowBanner("УРОН ПОГЛОЩЁН БЕЗДНОЙ! (Сфера Бездны)", Rgb(205, 85, 255), 1.0f);
                return;
            }

            if (_rng.NextDouble() < BlockChance)
            {
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.4f, 0), Color.SkyBlue, 2.2f, 0.25f);
                Run.ShowBanner("УРОН ЗАБЛОКИРОВАН! (Плюшевый Мишка)", Color.SkyBlue, 0.9f);
                return;
            }

            float dmg = amount;
            if (ShieldStance) dmg *= 0.35f;

            int buckler = CombatManager.GetItemCount("rose_buckler");
            if (buckler > 0) dmg /= (1.0f + 0.15f * buckler);

            int opal = CombatManager.GetItemCount("opal");
            if (opal > 0 && OpalTimer <= 0f)
            {
                dmg *= MathF.Max(0.25f, 1.0f - (0.20f + 0.10f * opal));
                OpalTimer = 5.0f;
            }

            int plate = CombatManager.GetItemCount("repulsion_armor");
            if (plate > 0) dmg = MathF.Max(1.0f, dmg - 4.0f * plate);

            if (CombatManager.GetItemCount("medkit") > 0) MedkitTimer = 1.1f;

            int razor = CombatManager.GetItemCount("razorwire");
            if (razor > 0)
            {
                Vector3 myP = Run.PlayerPos + new Vector3(0, 1.3f, 0);
                int hitC = 0;
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(myP, e.Position) <= 18.0f + 3.0f * razor)
                    {
                        new LightningBeam(myP, e.Position + new Vector3(0, e.HitHeight, 0), Rgb(185, 235, 115), 0.18f);
                        e.TakeDamage(BaseDamage * (1.6f * razor));
                        if (++hitC >= 3 + 2 * razor) break;
                    }
                }
            }

            if (Shield > 0f)
            {
                float absorbed = MathF.Min(Shield, dmg);
                Shield -= absorbed;
                dmg -= absorbed;
            }

            Run.PlayerHp -= dmg;
            if (Run.PlayerHp <= 0f)
            {
                DummyClones.RemoveAll(c => !IsValid(c));
                if (CharKey == "m4a1" && DummyClones.Count > 0)
                {
                    var hostClone = DummyClones[0];
                    DummyClones.RemoveAt(0);
                    Vector3 newPos = hostClone.Position;
                    Destroy(hostClone);
                    Run.PlayerPos = newPos;
                    Run.PlayerHp = Run.PlayerMaxHp;
                    Shield = 35.0f;
                    SkillLockTimer = 12.0f;
                    Run.ShowBanner("ПЕРЕНОС СОЗНАНИЯ :: ВЫ ПЕРЕСЕЛИЛИСЬ В КЛОНА!", Rgb(95, 255, 135), 4.5f);
                    return;
                }
                Run.PlayerHp = Run.PlayerMaxHp;
                Run.ShowBanner("КРИТИЧЕСКИЙ УРОН! АВТО-ВОССТАНОВЛЕНИЕ ДЛЯ ТЕСТА БОЯ", Color.Red, 2.5f);
            }
        }
    }
}