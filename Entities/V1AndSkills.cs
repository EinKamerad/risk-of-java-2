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

    public class GunFlamePillar : Entity
    {
        private float _delay;
        private bool _spawned = false;
        private readonly float _dmg;

        public GunFlamePillar(Vector3 pos, float delay, float dmg) : base(position: pos)
        {
            _delay = delay;
            _dmg = dmg;
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _delay -= dt;
            if (!_spawned && _delay <= 0f)
            {
                _spawned = true;
                float fy = Physics.GetSurfaceHeight(X, Z, Y + 1.5f);
                Y = fy;
                var flame = new Entity(parent: this, model: "diamond", scale: new(1.6f, 4.2f, 1.6f), position: new(0, 2.0f, 0), color: Rgb(255, 115, 25), unlit: true);
                var core = new Entity(parent: this, model: "diamond", scale: new(0.95f, 4.6f, 0.95f), position: new(0, 2.1f, 0), color: Color.Yellow, unlit: true);
                flame.AnimateScale(new Vector3(0.2f, 5.2f, 0.2f), 0.30f);
                core.AnimateScale(new Vector3(0.1f, 5.5f, 0.1f), 0.28f);
                new VisualFX(Position + new Vector3(0, 1.0f, 0), Rgb(255, 140, 35), 2.8f, 0.26f);

                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Physics.Distance2D(Position, e.Position) <= 2.6f + e.HitRadius && MathF.Abs(e.Y - Y) <= 4.0f)
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight, 0), _dmg, Rgb(255, 125, 30), aoe: 1.5f, lifesteal: 1.5f, canProc: true);
                }
                Destroy(this, 0.32f);
            }
        }
    }

    public class ThrownPhysicsObject : Entity
    {
        private Vector3 _vel;
        private readonly float _dmg;
        private readonly Entity? _originalChest;
        private float _life = 2.8f;

        public ThrownPhysicsObject(Vector3 startPos, Vector3 dir, float dmg, Entity? originalChest, Color col)
            : base(position: startPos)
        {
            _vel = Vector3.Normalize(dir) * 68f;
            _dmg = dmg;
            _originalChest = originalChest;
            new Entity(parent: this, model: originalChest != null ? "cube" : "diamond", scale: new(1.4f, 1.1f, 1.4f), color: col);
            new Entity(parent: this, model: "diamond", scaleUniform: 1.6f, color: Rgb(255, 185, 55, 140), unlit: true);
        }

        private void Detonate()
        {
            new VisualFX(Position, Color.Orange, 6.5f, 0.38f);
            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position) <= 6.2f)
                {
                    if (!e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, 1.8f);
                    CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight, 0), _dmg, Color.Orange, aoe: 2.5f, canProc: true);
                }
            }
            if (_originalChest != null && !_originalChest.IsDestroyed)
            {
                float fy = Physics.GetSurfaceHeight(X, Z, Y + 2f);
                _originalChest.Position = new Vector3(X, fy, Z);
                _originalChest.Enabled = true;
            }
            Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _vel.Y -= 9.5f * dt;
            Position += _vel * dt;
            RotationX += 360f * dt;
            RotationY += 280f * dt;
            _life -= dt;

            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position + new Vector3(0, e.HitHeight, 0)) <= e.HitRadius + 1.3f)
                {
                    Detonate();
                    return;
                }
            }
            float fy = Physics.GetSurfaceHeight(X, Z, Y);
            if (Y <= fy + 0.3f || _life <= 0f)
                Detonate();
        }
    }

    public class OrangeClusterProjectile : Entity
    {
        private Vector3 _vel;
        private readonly float _dmg;
        private readonly int _shardsCount;
        private float _life = 3.2f;

        public OrangeClusterProjectile(Vector3 startPos, Vector3 aimDir, float dmg, int shardsCount = 8)
            : base(model: "sphere", position: startPos, scaleUniform: 0.48f, color: Rgb(255, 145, 20), unlit: true)
        {
            _vel = Vector3.Normalize(aimDir) * 44f + new Vector3(0, 6.5f, 0);
            _dmg = dmg;
            _shardsCount = shardsCount;
            new Entity(parent: this, model: "diamond", scale: new(0.35f, 0.25f, 0.35f), position: new(0, 0.32f, 0), color: Color.Lime, unlit: true);
        }

        private void ExplodeIntoSlices()
        {
            if (IsDestroyed) return;
            Vector3 hitPos = Position;
            new VisualFX(hitPos, Rgb(255, 155, 30), 4.6f, 0.32f);

            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(hitPos, e.Position + new Vector3(0, e.HitHeight, 0)) <= 4.2f)
                    CombatManager.ApplyPlayerHitEffects(e, hitPos, _dmg, Rgb(255, 150, 25), aoe: 2.0f, lifesteal: 2.0f, canProc: true);
            }

            // Разлёт долек апельсина как шрапнели во все стороны!
            for (int i = 0; i < _shardsCount; i++)
            {
                float ang = (i / (float)_shardsCount) * MathF.PI * 2f;
                Vector3 sliceDir = Vector3.Normalize(new Vector3(MathF.Cos(ang), 0.32f, MathF.Sin(ang)));
                var slice = new Projectile(
                    hitPos + new Vector3(0, 0.35f, 0), sliceDir, 32f, _dmg * 0.48f, 0.24f,
                    i % 2 == 0 ? Rgb(255, 165, 35) : Color.Yellow,
                    isPlayer: true, aoe: 1.8f, homing: false, lifesteal: 0.8f, maxLifetime: 0.65f)
                {
                    GravityArc = 1.6f
                };
            }
            Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _vel.Y -= 16.5f * dt;
            Position += _vel * dt;
            RotationX += 420f * dt;
            RotationZ += 340f * dt;
            _life -= dt;

            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position + new Vector3(0, e.HitHeight, 0)) <= e.HitRadius + 0.65f)
                {
                    ExplodeIntoSlices();
                    return;
                }
            }

            float fy = Physics.GetSurfaceHeight(X, Z, Y);
            if (Y <= fy + 0.25f || Physics.IsInsideSolidObstacle(Position, 0.25f) || _life <= 0f)
                ExplodeIntoSlices();
        }
    }

    public class ShapoklyakThrowFX : Entity
    {
        public ShapoklyakThrowFX(Vector3 pos, float rotY) : base(position: pos, rotation: new(0, rotY, 0))
        {
            // Силуэт старухи Шапокляк в тёмном костюме, шляпке-шапокляк и с ридикюлем
            new Entity(parent: this, model: "diamond", scale: new(0.75f, 1.35f, 0.65f), position: new(0, 0.7f, 0), color: Rgb(32, 30, 42));
            new Entity(parent: this, model: "sphere", scale: new(0.42f, 0.44f, 0.42f), position: new(0, 1.55f, 0.05f), color: Rgb(245, 215, 195));
            new Entity(parent: this, model: "diamond", scale: new(0.14f, 0.14f, 0.34f), position: new(0, 1.55f, 0.28f), color: Rgb(235, 195, 175));
            new Entity(parent: this, model: "cube", scale: new(0.48f, 0.32f, 0.48f), position: new(0, 1.84f, 0.02f), color: Rgb(22, 22, 28));
            // Ридикюль в замахе
            new Entity(parent: this, model: "cube", scale: new(0.36f, 0.32f, 0.22f), position: new(0.42f, 1.35f, 0.38f), color: Rgb(140, 35, 45));
            Destroy(this, 0.85f);
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
        public float EnforcerParryTimer = 0f;
        public float SlarkShadowTimer = 0f;
        public int SlarkEssenceStacks = 0;

        // Механики Сола (Bandit Revolver 2-stage & Bandit Bringer)
        public float SolBanditWindow = 0f;
        public int SolBringerState = 0; // 0=нет, 1=взлёт, 2=пикирование
        public float SolBringerTimer = 0f;
        public Vector3 SolBringerDir = Vector3.Zero;

        // Механики Гвен (4 удара -> 6 быстрых разрезов, бафф скорости от тумана, 1->2->3 ульта)
        public int GwenAttackCounter = 0;
        public int GwenFlurryShots = 0;
        public float GwenFlurryTimer = 0f;
        public float GwenMistAtkBuff = 0f;
        public int GwenUltStage = 0;
        public float GwenUltTimer = 0f;
        public Vector3 GwenUltDir = Vector3.UnitZ;

        // Насмешка Медика TF2 [G]
        public float MedicTauntTimer = 0f;

        // Полёт Рейму на зажатие Пробела
        public float ReimuFlightFuel = 4.5f;
        public bool ReimuIsFlying = false;
        public bool ReimuFlightBroken = false;

        // Строительство Корабля Mindustry (3 ресурса, 2 режима и 8 построек)
        public bool MindustryBuildMode = false; // false = Боевой режим (уязвим), true = Строительный режим (парит, неуязвим)
        public int MindustrySelectedBuild = -1; // -1 = ничего не выбрано (ЛКМ стреляет), 0..7 = выбран чертёж
        public float MindustryCopper = 65f;
        public float MindustrySilicon = 30f;
        public float MindustryThorium = 10f;
        public float MindustryOverdriveTimer = 0f;
        public float MindustryCoreRebuildTimer = 0f;
        public Entity? MindustryShieldSphere = null;
        public MindustryTurret? MindustryMainCore = null;

        public static readonly (string Type, string ShortName, string Name, float Copper, float Silicon, float Thorium)[] MindustryBlueprints =
        {
            ("drill", "1.БУР", "Пневмо-Бур (+Медь/Жилы)", 25f, 0f, 0f),
            ("smelter", "2.ПЛАВ", "Плавильщик (Медь->Кремний)", 40f, 0f, 0f),
            ("refinery", "3.СИНТ", "Синтезатор (Кремний->Торий)", 55f, 30f, 0f),
            ("pylon", "4.ПИЛОН", "Ремонтный Купол (Хил+Щит)", 45f, 25f, 0f),
            ("core_omega", "5.ЯДРО", "Ядро «Цитадель» (+45% Урон)", 80f, 55f, 25f),
            ("duo", "6.ДУЭТ", "Турель «Дуэт» (Лёгкая)", 35f, 0f, 0f),
            ("salvo", "7.ЗАЛП", "Ракетница «Залп» (Самонав.)", 65f, 40f, 0f),
            ("spectre", "8.ЛАЗЕР", "Лазер «Испепелитель» (Тяж.)", 100f, 70f, 40f)
        };

        // Грави-пушка и Тау-пушка Гордона Фримана
        public Entity? GrabbedChest = null;
        public bool HasGrabbedObject = false;
        public Color GrabbedColor = Color.Orange;
        public Entity? FpGrabbedPreview = null;
        public bool TauCharging = false;
        public float TauCharge = 0f;

        // Командные баффы Данте
        public float DanteAtkBuffTimer = 0f;
        public float DanteSpdBuffTimer = 0f;

        // Механики Чебурашки и Стенда Крокодила Гены
        public float GenaStandTimer = 0f;
        public float ChebPeakAirY = 0f;
        public bool ChebWasAirborne = false;

        // Поля управления и прицеливания для MOBA-режима
        public Vector3 MobaCursorPos = Vector3.Zero;
        public Vector3? MobaMoveTarget = null;
        public CombatTarget? MobaAttackTarget = null;

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
        public float TonicBuffTimer = 0f;
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

        public static bool IsFirstPersonCharacter(string charKey)
            => charKey is "v1" or "medic" or "freeman";

        public SurvivorCombatController(GameRunContext run)
        {
            Run = run;
            CombatManager.Controller = this;

            BungusRing = new Entity(parent: run.PlayerEntity, model: "sphere", scale: new(11.0f, 0.12f, 11.0f),
                position: new(0, 0.08f, 0), color: Rgb(85, 255, 120, 65), unlit: true, enabled: false);
            // Кольцо Ледяной Реликвии опущено низко к земле (Y=0.12), чтобы не закрывать сундуки и платформы!
            FrostRing = new Entity(parent: run.PlayerEntity, model: "diamond", scale: new(13.0f, 0.06f, 13.0f),
                position: new(0, 0.12f, 0), color: Rgb(110, 235, 255, 75), unlit: true, enabled: false);

            if (run.CharKey == "mindustry")
            {
                MindustryShieldSphere = new Entity(parent: run.PlayerEntity, model: "sphere", scaleUniform: 2.8f,
                    position: new(0, 1.2f, 0), color: Rgb(80, 215, 255, 85), unlit: true, enabled: false);
            }

            if (IsFirstPersonCharacter(run.CharKey))
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

            // Левая механическая рука (Feedbacker / Knuckleblaster / Whiplash / Монтировка Фримана)
            FpLeftArm = new Entity(parent: FpRoot, position: new(-0.38f, -0.34f, 0.35f), rotation: new(-12, 18, 0));
            FpArmForearm = new Entity(parent: FpLeftArm, model: "cube", scale: new(0.11f, 0.11f, 0.46f),
                color: CharKey == "medic" ? Rgb(205, 38, 45) : (CharKey == "freeman" ? Rgb(235, 125, 30) : Rgb(45, 115, 235)));
            FpArmFist = new Entity(parent: FpLeftArm, model: "diamond", scale: new(0.17f, 0.17f, 0.22f), position: new(0, 0, 0.26f),
                color: CharKey == "medic" ? Color.Red : (CharKey == "freeman" ? Color.Orange : Color.SkyBlue), unlit: true);

            if (CharKey == "medic")
            {
                foreach (var kv in FpWeapons) kv.Value.Enabled = false;
                var medGun = new Entity(parent: FpWeaponHolder);
                new Entity(parent: medGun, model: "cube", scale: new(0.16f, 0.18f, 0.62f), color: Rgb(65, 72, 84));
                new Entity(parent: medGun, model: "sphere", scale: new(0.22f, 0.22f, 0.36f), position: new(0, 0.12f, 0.05f), color: Color.Red, unlit: true);
                new Entity(parent: medGun, model: "diamond", scale: new(0.18f, 0.18f, 0.24f), position: new(0, 0, 0.38f), color: Color.SkyBlue, unlit: true);
                FpWeapons[10] = medGun;
            }
            else if (CharKey == "freeman")
            {
                foreach (var kv in FpWeapons) kv.Value.Enabled = false;
                var grav = new Entity(parent: FpWeaponHolder);
                new Entity(parent: grav, model: "cube", scale: new(0.16f, 0.18f, 0.58f), color: Rgb(55, 62, 72));
                new Entity(parent: grav, model: "sphere", scaleUniform: 0.20f, position: new(0, 0.02f, 0.28f), color: Color.Orange, unlit: true);
                foreach (float cx in new[] { -0.09f, 0.09f })
                    new Entity(parent: grav, model: "diamond", scale: new(0.05f, 0.12f, 0.24f), position: new(cx, 0.02f, 0.42f), color: Rgb(235, 125, 30));
                FpGrabbedPreview = new Entity(parent: grav, model: "cube", scaleUniform: 0.45f, position: new(0, 0.08f, 1.35f), color: Color.Orange, enabled: false);
                FpWeapons[11] = grav;

                // Монтировка в левой руке Фримана
                new Entity(parent: FpLeftArm, model: "cube", scale: new(0.05f, 0.52f, 0.05f), position: new(0, 0.18f, 0.28f), rotation: new(25, 0, 0), color: Rgb(220, 38, 38));
            }
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
                float raw = Cfg.Damage + (CharKey == "slark" ? SlarkEssenceStacks * 0.45f : 0f);
                float glassMult = MathF.Pow(2.0f, CombatManager.GetItemCount("shaped_glass"));
                float tonicMult = (1.0f + 0.25f * CombatManager.GetItemCount("spinel_tonic"))
                                  * MathF.Pow(0.88f, CombatManager.GetItemCount("tonic_affliction"));
                float beaconMult = 1.0f;
                foreach (var b in MindustryTurrets)
                {
                    if (IsValid(b) && b.BuildType == "core_beacon" && Vector3.Distance(Run.PlayerPos, b.Position) <= 24f)
                    { beaconMult = 1.35f; break; }
                }
                float danteMult = DanteAtkBuffTimer > 0f ? 1.45f : 1.0f;
                float genaMult = GenaStandTimer > 0f ? 1.45f : 1.0f;
                float orgiaMult = OrgiaTimer > 0f ? 1.35f : 1.0f;
                float uberMult = UberTimer > 0f ? 1.35f : 1.0f;
                return raw * glassMult * tonicMult * orgiaMult * uberMult * danteMult * beaconMult * genaMult;
            }
        }

        public float MoveSpeed
        {
            get
            {
                float diMult = (DragonInstallTimer > 0f || SlarkShadowTimer > 0f) ? 1.45f : 1.0f;
                float genaMult = GenaStandTimer > 0f ? 1.28f : 1.0f;
                float orgiaMult = OrgiaTimer > 0f ? 1.45f : 1.0f;
                float uberMult = UberTimer > 0f ? 1.25f : 1.0f;
                float stanceMult = ShieldStance ? 0.65f : 1.0f;
                float scoutMult = (ScoutSpeedBuff > 0f || DanteSpdBuffTimer > 0f) ? 1.35f : 1.0f;
                float tonicMult = (1.0f + 0.25f * CombatManager.GetItemCount("spinel_tonic"))
                                  * MathF.Pow(0.88f, CombatManager.GetItemCount("tonic_affliction"));
                float fluxPen = MathF.Pow(0.70f, CombatManager.GetItemCount("lunar_pauldron"));
                float spdBonus = 1.0f
                    + 0.14f * CombatManager.GetItemCount("hoof")
                    + 0.08f * CombatManager.GetItemCount("energy_drink")
                    + 0.12f * CombatManager.GetItemCount("red_whip");
                return Cfg.Speed * spdBonus * diMult * genaMult * orgiaMult * uberMult * stanceMult * scoutMult * tonicMult * fluxPen;
            }
        }

        public float FireRate
        {
            get
            {
                float diMult = DragonInstallTimer > 0f ? 1.65f : 1.0f;
                float genaMult = GenaStandTimer > 0f ? 1.55f : 1.0f;
                float orgiaMult = OrgiaTimer > 0f ? 1.50f : 1.0f;
                float uberMult = UberTimer > 0f ? 1.50f : 1.0f;
                float stanceMult = ShieldStance ? 1.45f : 1.0f;
                float gwenMistMult = GwenMistAtkBuff > 0f ? 1.55f : 1.0f;
                float danteMult = DanteAtkBuffTimer > 0f ? 1.35f : 1.0f;
                float tonicMult = (1.0f + 0.25f * CombatManager.GetItemCount("spinel_tonic"))
                                  * MathF.Pow(0.88f, CombatManager.GetItemCount("tonic_affliction"));
                float predBonus = PredatoryTimer > 0f ? 0.12f * CombatManager.GetItemCount("predatory") : 0f;
                return Cfg.FireRate * genaMult * gwenMistMult * danteMult * (1.0f + 0.15f * CombatManager.GetItemCount("syringe") + predBonus);
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

        public void SpawnMindustryMainCore(Vector3 pos)
        {
            if (CharKey != "mindustry") return;
            float fy = Physics.GetSurfaceHeight(pos.X, pos.Z, pos.Y);
            Vector3 cPos = new(pos.X, fy, pos.Z - 3.5f);
            var mainCore = new MindustryTurret(cPos, Run.PlayerMaxHp, 0, "core_main", isRemoteVisual: false);
            MindustryMainCore = mainCore;
            MindustryTurrets.Add(mainCore);
            Run.StageEntities.Add(mainCore);
            if (Net.NetworkManager.Instance.Connected)
            {
                Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "mindustry_build",
                    ["id"] = mainCore.StructNetId,
                    ["btype"] = "core_main",
                    ["max_hp"] = MathF.Round(Run.PlayerMaxHp, 1),
                    ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(cPos.X, 2), MathF.Round(cPos.Y, 2), MathF.Round(cPos.Z, 2))
                });
            }
        }

        public void OnMainCoreDestroyed()
        {
            if (MindustryCoreRebuildTimer <= 0f)
            {
                MindustryCoreRebuildTimer = 60.0f;
                Run.ShowBanner("ГЛАВНОЕ ЯДРО УНИЧТОЖЕНО! У ВАС ЕСТЬ 60 СЕК, ЧТОБЫ ПОСТРОИТЬ НОВОЕ [5.ЯДРО]!", Color.Red, 5.0f);
            }
        }

        public void ToggleMindustryMode()
        {
            MindustryBuildMode = !MindustryBuildMode;
            if (!MindustryBuildMode)
                MindustrySelectedBuild = -1;
            if (MindustryShieldSphere != null)
                MindustryShieldSphere.Enabled = MindustryBuildMode;
            Run.ShowBanner(
                MindustryBuildMode
                    ? "СТРОИТЕЛЬНЫЙ РЕЖИМ :: ВЫ ПАРИТЕ И НЕУЯЗВИМЫ! ВЫБЕРИТЕ ЧЕРТЁЖ СПРАВА СНИЗУ [1-8]"
                    : "БОЕВОЙ РЕЖИМ :: КОРАБЛЬ УЯЗВИМ! ДОСТУПНА СТРЕЛЬБА [ЛКМ] И ИНТЕРАКТИВ [E]",
                MindustryBuildMode ? Color.SkyBlue : Color.Gold, 2.4f);
        }

        public void TryPlaceMindustryStructureAtCursor()
        {
            if (MindustrySelectedBuild < 0 || MindustrySelectedBuild >= MindustryBlueprints.Length) return;
            var bp = MindustryBlueprints[MindustrySelectedBuild];
            if (MindustryCopper < bp.Copper || MindustrySilicon < bp.Silicon || MindustryThorium < bp.Thorium)
            {
                Run.ShowBanner($"НЕ ХВАТАЕТ РЕСУРСОВ ДЛЯ «{bp.Name}»! НУЖНО: {(int)bp.Copper} Меди, {(int)bp.Silicon} Кремния, {(int)bp.Thorium} Тория", Color.Red, 2.2f);
                return;
            }
            Vector3 buildPos = MobaCursorPos;
            if (Physics.Distance2D(Run.PlayerPos, buildPos) > 34.0f)
            {
                Run.ShowBanner("СЛИШКОМ ДАЛЕКО ДЛЯ СТРОИТЕЛЬСТВА (МАКС. 34М ОТ КОРАБЛЯ)!", Color.Orange, 1.8f);
                return;
            }
            MindustryCopper -= bp.Copper;
            MindustrySilicon -= bp.Silicon;
            MindustryThorium -= bp.Thorium;

            MindustryTurrets.RemoveAll(t => !IsValid(t));
            if (MindustryTurrets.Count >= 12)
            {
                var removable = MindustryTurrets.Find(t => t.BuildType != "core_main");
                if (removable != null) { Destroy(removable); MindustryTurrets.Remove(removable); }
            }
            float fy = Physics.GetSurfaceHeight(buildPos.X, buildPos.Z, Run.PlayerPos.Y);
            bool replaceMainCore = bp.Type == "core_omega" && (!IsValid(MindustryMainCore) || MindustryCoreRebuildTimer > 0f);
            string actualType = replaceMainCore ? "core_main" : bp.Type;
            var structure = new MindustryTurret(new Vector3(buildPos.X, fy, buildPos.Z), Run.PlayerMaxHp, MindustryTurrets.Count, actualType, isRemoteVisual: false);
            if (replaceMainCore)
            {
                MindustryMainCore = structure;
                MindustryCoreRebuildTimer = 0f;
                Run.ShowBanner("НОВОЕ ГЛАВНОЕ ЯДРО УСТАНОВЛЕНО! ТАЙМЕР ДЕТОНАЦИИ ОСТАНОВЛЕН!", Color.Lime, 3.5f);
            }
            MindustryTurrets.Add(structure);
            Run.StageEntities.Add(structure);
            new VisualFX(new Vector3(buildPos.X, fy + 1f, buildPos.Z), Color.Gold, 3.2f, 0.28f);

            if (Net.NetworkManager.Instance.Connected)
            {
                Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "mindustry_build",
                    ["id"] = structure.StructNetId,
                    ["btype"] = bp.Type,
                    ["max_hp"] = MathF.Round(Run.PlayerMaxHp, 1),
                    ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(buildPos.X, 2), MathF.Round(fy, 2), MathF.Round(buildPos.Z, 2))
                });
            }
            Run.ShowBanner($"ПОСТРОЕНО: {bp.Name}!", Color.Lime, 1.6f);
        }

        public Vector3 GetAimDirection()
        {
            if (SurvivorDatabase.IsMobaCharacter(CharKey) || CharKey == "mindustry")
            {
                Vector3 from = new(Run.PlayerPos.X, MobaCursorPos.Y + 1.25f, Run.PlayerPos.Z);
                Vector3 to = MobaCursorPos + new Vector3(0, 1.15f, 0);
                Vector3 diff = to - from;
                if (diff.LengthSquared() > 0.01f)
                    return Vector3.Normalize(diff);
                return Run.PlayerForward;
            }

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

            if (Physics.SpecialMode == "pvp")
            {
                if (Program.TryHitRemotePlayerPvP(strikeCenter, cleaveRadius + 1.0f, BaseDamage * damageMult))
                    hitCount++;
                foreach (var st in MindustryTurrets.ToArray())
                {
                    if (IsValid(st) && st.IsRemoteVisual && Physics.Distance2D(strikeCenter, st.Position) <= cleaveRadius + st.HitRadius)
                    {
                        st.TakeStructureDamage(BaseDamage * damageMult, fromNetwork: false);
                        hitCount++;
                    }
                }
            }

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

        public bool TryReflectOrParryProjectile(Projectile proj)
        {
            if (CharKey != "enforcer") return false;
            Vector3 myPos = Run.PlayerPos + new Vector3(0, 1.4f, 0);

            // 1. Парирование при выходе из щита (любая атака с 3-кратной силой!)
            if (EnforcerParryTimer > 0f)
            {
                EnforcerParryTimer = 0.25f;
                proj.IsPlayer = true;
                proj.Damage = MathF.Max(proj.Damage * 3.0f, BaseDamage * 4.5f);
                proj.Speed = MathF.Max(proj.Speed * 1.6f, 82f);
                proj.Direction = GetAimDirection();
                proj.Color = Color.Gold;
                proj.Aoe = MathF.Max(proj.Aoe, 4.5f);
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 30f);
                new VisualFX(proj.Position, Color.Gold, 4.2f, 0.30f);
                Run.ShowBanner("ИДЕАЛЬНОЕ ПАРИРОВАНИЕ ЩИТОМ (УРОН x3)!", Color.Gold, 1.8f);
                return true;
            }

            // 2. Отражение лобовых атак в стойке щита
            if (ShieldStance)
            {
                Vector3 incoming = Vector3.Normalize(proj.Direction);
                float dot = Vector3.Dot(incoming, Run.PlayerForward);
                if (dot < -0.10f)
                {
                    proj.IsPlayer = true;
                    proj.Damage = MathF.Max(proj.Damage * 1.5f, BaseDamage * 1.8f);
                    proj.Speed = MathF.Max(proj.Speed * 1.3f, 68f);
                    proj.Direction = GetAimDirection();
                    proj.Color = Color.SkyBlue;
                    new VisualFX(myPos + Run.PlayerForward * 0.9f, Color.SkyBlue, 2.2f, 0.22f);
                    return true;
                }
            }
            return false;
        }

        public bool TryParryExternalAttack(float incomingDmg, Vector3 sourcePos)
        {
            if (CharKey != "enforcer") return false;
            Vector3 myPos = Run.PlayerPos + new Vector3(0, 1.4f, 0);

            if (EnforcerParryTimer > 0f)
            {
                EnforcerParryTimer = 0.25f;
                float reflectDmg = MathF.Max(incomingDmg * 3.0f, BaseDamage * 5.0f);
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 40f);
                new VisualFX(myPos, Color.Gold, 6.5f, 0.38f);

                CombatTarget? target = null;
                float bestD = 90f;
                foreach (var e in CombatManager.Enemies)
                {
                    if (IsValid(e) && !e.IsAlly)
                    {
                        float d = Vector3.Distance(sourcePos, e.Position);
                        if (d < bestD) { bestD = d; target = e; }
                    }
                }
                if (target != null)
                {
                    Vector3 hitP = target.Position + new Vector3(0, target.HitHeight, 0);
                    new LightningBeam(myPos, hitP, Color.Gold, 0.48f);
                    CombatManager.ApplyPlayerHitEffects(target, hitP, reflectDmg, Color.Gold, aoe: 6.0f, lifesteal: 15f, canProc: true);
                }
                Run.ShowBanner("АБСОЛЮТНОЕ ПАРИРОВАНИЕ ЩИТОМ :: ВОЗВРАТ УРОНА x3!", Color.Gold, 2.2f);
                return true;
            }

            if (ShieldStance)
            {
                Vector3 toSource = sourcePos - Run.PlayerPos;
                toSource.Y = 0;
                if (toSource.LengthSquared() > 0.01f && Vector3.Dot(Vector3.Normalize(toSource), Run.PlayerForward) > 0.15f)
                {
                    new VisualFX(myPos + Run.PlayerForward * 1.0f, Color.SkyBlue, 3.0f, 0.25f);
                    new Projectile(myPos + Run.PlayerForward * 1.1f, GetAimDirection(), 74f, MathF.Max(incomingDmg * 1.5f, BaseDamage * 2.0f), 0.45f, Color.SkyBlue, isPlayer: true, aoe: 3.5f);
                    return true;
                }
            }
            return false;
        }

        public void FirePrimaryProjectile(float damageMult = 1.0f, Color? colOverride = null)
        {
            Run.Parts.TriggerSkillAnim("m1", Math.Clamp(0.85f / MathF.Max(1f, FireRate), 0.14f, 0.30f));
            Vector3 aimDir = GetAimDirection();
            Vector3 spawnPos = Run.PlayerPos + new Vector3(0, 1.55f, 0) + Run.PlayerForward * 0.95f;

            if (CharKey == "sol" && colOverride == null)
            {
                float diBonus = DragonInstallTimer > 0f ? 1.35f : 1.0f;
                PerformMeleeAttack(4.5f, 3.2f, damageMult * diBonus, Rgb(255, 115, 25), lifesteal: 1.5f);
                Vector3 fwd = Run.PlayerForward;
                for (int i = 1; i <= 5; i++)
                {
                    Vector3 pPos = Run.PlayerPos + fwd * (i * 2.7f);
                    Run.StageEntities.Add(new GunFlamePillar(pPos, i * 0.055f, BaseDamage * 0.85f * damageMult * diBonus));
                }
                return;
            }
            if (CharKey == "gwen" && colOverride == null)
            {
                GwenAttackCounter++;
                PerformMeleeAttack(4.8f, 3.1f, damageMult, Rgb(95, 225, 255), lifesteal: 2.4f, scissorCross: true, aoe: 1.5f);
                if (GwenAttackCounter >= 4)
                {
                    GwenAttackCounter = 0;
                    GwenFlurryShots = 6;
                    GwenFlurryTimer = 0.04f;
                    Run.ShowBanner("ЧИК-ЧИК! :: СЕРИЯ ИЗ 6 РАЗРЕЗОВ!", Rgb(140, 235, 255), 1.2f);
                }
                return;
            }
            if (CharKey == "slark" && colOverride == null)
            {
                int hits = PerformMeleeAttack(4.6f, 3.2f, damageMult, Rgb(80, 255, 210), lifesteal: 3.0f, aoe: 1.4f);
                if (hits > 0) SlarkEssenceStacks = Math.Min(40, SlarkEssenceStacks + hits);
                return;
            }
            if (CharKey == "tidehunter" && colOverride == null)
            {
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.0f, 0), Rgb(75, 225, 185), 5.8f, 0.25f);
                PerformMeleeAttack(6.0f, 4.8f, damageMult, Rgb(85, 240, 195), lifesteal: 2.5f, stunDur: 0.35f, knockback: 1.5f, aoe: 3.8f);
                return;
            }
            if (CharKey == "freeman" && colOverride == null)
            {
                V1AnimateLeftPunch(Color.Orange);
                V1AnimateWeaponRecoil(-0.08f, -8f);
                PerformMeleeAttack(4.5f, 3.0f, damageMult * 0.95f, Rgb(255, 70, 50), lifesteal: 2.0f);
                new Projectile(spawnPos, aimDir, Cfg.ProjSpeed, BaseDamage * damageMult, Cfg.ProjScale, Rgb(255, 195, 55), isPlayer: true, aoe: Cfg.Aoe);
                return;
            }
            if (CharKey == "cheburashka" && colOverride == null)
            {
                if (GenaStandTimer > 0f)
                {
                    // ПУЛ АТАК ГЕНЫ (ЛКМ): Мощный JoJo-шквал ударов лапами и укусов Крокодила Гены!
                    PerformMeleeAttack(6.8f, 4.8f, damageMult * 1.45f, Rgb(65, 235, 95), lifesteal: 3.5f, stunDur: 0.25f, knockback: 2.2f, scissorCross: true, aoe: 3.2f);
                    new Projectile(spawnPos + Run.PlayerForward * 1.2f, aimDir, 58f, BaseDamage * 0.95f * damageMult, 0.55f, Rgb(75, 245, 110), isPlayer: true, aoe: 3.0f, pierce: true, maxLifetime: 0.28f);
                }
                else
                {
                    // Обычный ЛКМ: размашистый удар большими ушами
                    PerformMeleeAttack(5.0f, 3.8f, damageMult * 1.05f, Rgb(215, 140, 75), lifesteal: 2.0f, knockback: 2.6f, aoe: 2.2f);
                }
                return;
            }
            if (CharKey == "dante" && colOverride == null)
            {
                new Projectile(spawnPos, aimDir, Cfg.ProjSpeed, BaseDamage * damageMult, Cfg.ProjScale, Rgb(255, 205, 55), isPlayer: true, aoe: Cfg.Aoe, pierce: true, lifesteal: 1.5f);
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
                if (MindustryBuildMode && MindustrySelectedBuild >= 0)
                {
                    TryPlaceMindustryStructureAtCursor();
                    M1Cd = 0.28f;
                    return;
                }
                // Если в боевом режиме или не выбрано ни одно строение — стреляет слабым лазером с высоты корабля/проекции в курсор
                Vector3 mSpawn = new(Run.PlayerPos.X, MobaCursorPos.Y + 1.3f, Run.PlayerPos.Z);
                new Projectile(mSpawn, aimDir, 56f, BaseDamage * 0.26f * damageMult, 0.16f, Rgb(255, 215, 65), isPlayer: true, aoe: 0f);
                return;
            }

            if (CharKey == "medic")
                V1AnimateWeaponRecoil(-0.07f, -6f);

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

            if (CharKey == "sol")
            {
                Run.Parts.TriggerSkillAnim("m2", 0.38f);
                if (SolBanditWindow > 0f)
                {
                    // Второй удар: REVOLVER!
                    SolBanditWindow = 0f;
                    M2Cd = Cfg.Skills["m2"].Cd * cdMult;
                    Run.PlayerVelY = 7.5f;
                    Run.ApplyDash(Run.PlayerForward * 18.0f, 0.18f);
                    PerformMeleeAttack(6.8f, 4.8f, 2.4f, Rgb(255, 70, 20), lifesteal: 5.0f, stunDur: 1.0f, knockback: 5.0f, aoe: 3.8f);
                    Run.ShowBanner("«REVOLVER!!» — ДВОЙНОЙ СОКРУШИТЕЛЬНЫЙ ПИНОК!", Rgb(255, 85, 25), 1.6f);
                }
                else
                {
                    // Первый удар: BANDIT! (открывает окно 1.1с для второго нажатия ПКМ)
                    SolBanditWindow = 1.15f;
                    M2Cd = 0.14f;
                    Run.PlayerVelY = 8.5f;
                    Run.PlayerGrounded = false;
                    Run.ApplyDash(Run.PlayerForward * 22.0f, 0.22f);
                    PerformMeleeAttack(6.2f, 4.2f, 1.8f, Rgb(255, 130, 30), lifesteal: 3.0f, knockback: 2.0f, aoe: 3.0f);
                    Run.ShowBanner("«BANDIT!» (НАЖМИТЕ ПКМ ЕЩЁ РАЗ ДЛЯ «REVOLVER!»)", Color.Orange, 1.2f);
                }
                return;
            }

            if (CharKey == "mindustry")
            {
                M2Cd = 0.15f;
                if (!MindustryBuildMode)
                {
                    Run.ShowBanner("В БОЕВОМ РЕЖИМЕ ДОСТУПНА ТОЛЬКО СТРЕЛЬБА [ЛКМ] И ИНТЕРАКТИВ [E]!", Color.Orange, 1.6f);
                    return;
                }
                MindustrySelectedBuild = -1;
                Run.ShowBanner("ВЫБОР СТРОЕНИЯ СБРОШЕН — ЛКМ ТЕПЕРЬ СТРЕЛЯЕТ!", Color.SkyBlue, 1.4f);
                return;
            }

            if (CharKey == "freeman")
            {
                M2Cd = 0.35f;
                V1AnimateWeaponRecoil(-0.14f, -12f);
                if (!HasGrabbedObject)
                {
                    // Ищем сундук впереди для захвата Грави-пушкой
                    Chest? bestCh = null;
                    float bestD = 18.0f;
                    foreach (var ent in Run.StageEntities)
                    {
                        if (ent is Chest ch && IsValid(ch))
                        {
                            float d = Vector3.Distance(Run.PlayerPos, ch.Position);
                            if (d < bestD && Vector3.Dot(aimDir, Vector3.Normalize(ch.Position - Run.PlayerPos)) > 0.55f)
                            {
                                bestD = d;
                                bestCh = ch;
                            }
                        }
                    }
                    if (bestCh != null)
                    {
                        GrabbedChest = bestCh;
                        bestCh.Enabled = false;
                        HasGrabbedObject = true;
                        GrabbedColor = bestCh.ChestType == "legendary" ? Color.Gold : Color.SkyBlue;
                        if (FpGrabbedPreview != null) { FpGrabbedPreview.Enabled = true; FpGrabbedPreview.Color = GrabbedColor; }
                        Run.ShowBanner("ГРАВИ-ПУШКА: СУНДУК ЗАХВАЧЕН! [ПКМ — ШВЫРНУТЬ ВО ВРАГА]", Color.Orange, 2.0f);
                    }
                    else
                    {
                        HasGrabbedObject = true;
                        GrabbedChest = null;
                        GrabbedColor = Rgb(140, 215, 255);
                        if (FpGrabbedPreview != null) { FpGrabbedPreview.Enabled = true; FpGrabbedPreview.Color = GrabbedColor; }
                        new VisualFX(spawnPos + aimDir * 2f, Color.Orange, 2.2f, 0.2f);
                        Run.ShowBanner("ГРАВИ-ПУШКА: ЗАХВАЧЕН КАМЕННЫЙ МОНОЛИТ! [ПКМ — ВЫСТРЕЛИТЬ]", Color.Orange, 2.0f);
                    }
                }
                else
                {
                    HasGrabbedObject = false;
                    if (FpGrabbedPreview != null) FpGrabbedPreview.Enabled = false;
                    var thrown = new ThrownPhysicsObject(spawnPos + aimDir * 1.2f, aimDir, BaseDamage * 4.2f, GrabbedChest, GrabbedColor);
                    GrabbedChest = null;
                    Run.StageEntities.Add(thrown);
                    Run.ShowBanner("ГРАВИ-ПУШКА: СНАРЯД ЗАПУЩЕН!", Color.Orange, 1.2f);
                }
                return;
            }

            M2Cd = Cfg.Skills["m2"].Cd * cdMult;
            Run.Parts.TriggerSkillAnim("m2", 0.42f);

            if (CharKey == "commando")
                new Projectile(spawnPos, aimDir, 68f, BaseDamage * 3.0f, 0.48f, Color.SkyBlue, isPlayer: true, pierce: true);
            else if (CharKey == "artificer")
                new Projectile(spawnPos, aimDir, 26f, BaseDamage * 2.8f, 0.95f, Color.SkyBlue, isPlayer: true, aoe: 7.5f);
            else if (CharKey == "huntress")
                new Projectile(spawnPos, aimDir, 48f, BaseDamage * 2.2f, 0.55f, Color.SkyBlue, isPlayer: true, bounces: 5, homing: true);
            else if (CharKey == "buzz")
                new Projectile(spawnPos, aimDir, 74f, BaseDamage * 2.9f, 0.55f, Color.Red, isPlayer: true, aoe: 4.2f, pierce: true);
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
                V1AnimateWeaponRecoil(-0.14f, -12f);
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
            else if (CharKey == "slark")
            {
                Run.PlayerHp = MathF.Max(1f, Run.PlayerHp - Run.PlayerHp * 0.05f);
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.2f, 0), Rgb(115, 65, 245), 9.0f, 0.38f);
                int hitCnt = 0;
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(e.Position, Run.PlayerPos) <= 8.5f)
                    {
                        hitCnt++;
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight, 0), BaseDamage * 3.2f, Rgb(130, 85, 255), aoe: 2.0f, lifesteal: 4.5f);
                    }
                }
                if (hitCnt > 0) SlarkEssenceStacks = Math.Min(40, SlarkEssenceStacks + hitCnt);
            }
            else if (CharKey == "tidehunter")
            {
                new VisualFX(spawnPos, Rgb(70, 215, 255), 3.2f, 0.28f);
                new Projectile(spawnPos, aimDir, 52f, BaseDamage * 2.8f, 0.85f, Rgb(70, 215, 255), isPlayer: true, aoe: 5.8f, pierce: true);
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(e.Position, Run.PlayerPos + Run.PlayerForward * 6f) <= 7.5f)
                    {
                        e.SlowTimer = MathF.Max(e.SlowTimer, 4.0f);
                        e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 6.0f);
                    }
                }
            }
            else if (CharKey == "cheburashka")
            {
                if (GenaStandTimer > 0f)
                {
                    // ПУЛ АТАК ГЕНЫ (ПКМ): Звуковой удар гармошки Гены + веер из 3 шрапнельных апельсинов!
                    new VisualFX(spawnPos, Rgb(80, 245, 120), 6.5f, 0.32f);
                    PerformMeleeAttack(8.5f, 6.0f, 2.5f, Color.Gold, lifesteal: 5.0f, stunDur: 1.4f, knockback: 6.0f, aoe: 4.5f);
                    foreach (float deg in new[] { -14f, 0f, 14f })
                    {
                        float rad = deg * (MathF.PI / 180f);
                        Vector3 d = Vector3.Normalize(new Vector3(aimDir.X * MathF.Cos(rad) - aimDir.Z * MathF.Sin(rad), aimDir.Y, aimDir.X * MathF.Sin(rad) + aimDir.Z * MathF.Cos(rad)));
                        Run.StageEntities.Add(new OrangeClusterProjectile(spawnPos, d, BaseDamage * 2.1f, shardsCount: 8));
                    }
                    Run.ShowBanner("ГАРМОШКА ГЕНЫ :: ЗВУКОВОЙ СОКРУШИТЕЛЬНЫЙ АККОРД!", Color.Gold, 1.6f);
                }
                else
                {
                    Run.StageEntities.Add(new OrangeClusterProjectile(spawnPos, aimDir, BaseDamage * 2.2f, shardsCount: 8));
                }
            }
            else if (CharKey == "dante")
            {
                // Бафф союзников: Резонанс Золотой Ветви
                DanteAtkBuffTimer = 7.0f;
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.4f, 0), Color.Gold, 9.5f, 0.38f);
                foreach (var e in CombatManager.Enemies)
                {
                    if (IsValid(e) && e.IsAlly)
                        e.HealAlly(e.MaxHp * 0.25f, uberDur: 7.0f);
                }
                if (Net.NetworkManager.Instance.Connected)
                    Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject { ["type"] = "dante_buff", ["kind"] = "atk" });
                Run.ShowBanner("ПРИКАЗ МЕНЕДЖЕРА :: +45% К УРОНУ И +35% К АТАКЕ ВСЕМ СОЮЗНИКАМ (7 СЕК)!", Color.Gold, 2.5f);
            }
        }

        public void UseShiftSkill()
        {
            if (SkillLockTimer > 0f) return;

            float alienCdr = MathF.Pow(0.80f, CombatManager.GetItemCount("alien_head"));
            float burnCdr = MathF.Pow(0.70f, CombatManager.GetItemCount("afterburner"));
            ShiftCd = MathF.Max(0.25f, Cfg.Skills["shift"].Cd * alienCdr * burnCdr);
            Vector3 myPos = Run.PlayerPos;
            float nrgMult = 1.0f + 0.18f * CombatManager.GetItemCount("energy_drink") + 0.35f * CombatManager.GetItemCount("afterburner");
            Run.Parts.TriggerSkillAnim("shift", 0.38f);

            if (CharKey == "enforcer")
            {
                ShieldStance = !ShieldStance;
                if (Run.Parts.ShieldAura != null) Run.Parts.ShieldAura.Enabled = ShieldStance;
                if (!ShieldStance)
                {
                    EnforcerParryTimer = 0.60f;
                    new VisualFX(myPos + new Vector3(0, 1.3f, 0) + Run.PlayerForward * 1.1f, Color.Gold, 3.8f, 0.28f);
                    Run.ApplyDash(Run.PlayerForward * 18.0f * nrgMult, 0.18f);
                    Run.ShowBanner("ВЫХОД ИЗ ЩИТА :: ОКНО АБСОЛЮТНОГО ПАРИРОВАНИЯ (x3 УРОН)!", Color.Gold, 1.8f);
                }
                else
                {
                    Run.ShowBanner("СТОЙКА ЩИТА АКТИВНА: ОТРАЖЕНИЕ ЛОБОВЫХ АТАК!", Color.SkyBlue, 1.8f);
                }
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
                // Bandit Bringer: диагональный взлёт вверх -> пикирующий огненный удар кулаком вниз
                SolBringerState = 1;
                SolBringerTimer = 0.26f;
                SolBringerDir = Run.PlayerForward;
                Run.PlayerVelY = 19.5f;
                Run.PlayerGrounded = false;
                Run.ApplyDash(Run.PlayerForward * 18.0f * nrgMult, 0.26f);
                new VisualFX(myPos + new Vector3(0, 1.2f, 0), Rgb(255, 115, 25), 3.2f, 0.25f);
                Run.ShowBanner("«BANDIT BRINGER!!»", Rgb(255, 120, 30), 1.4f);
            }
            else if (CharKey == "gwen")
            {
                Run.ApplyDash(Run.PlayerForward * 22.0f * nrgMult, 0.22f);
                M1Cd = 0f;
                GwenMistAtkBuff = 4.5f;
                Run.StageEntities.Add(new HallowedMistZone(myPos + Run.PlayerForward * 3.0f, BaseDamage * 0.65f));
                Run.ShowBanner("СВЯЩЕННЫЙ ТУМАН :: НЕВИДИМОСТЬ ДЛЯ СТРЕЛКОВ И +55% АТАКИ!", Rgb(120, 235, 255), 2.0f);
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
                ReimuFlightFuel = 4.5f;
                ReimuFlightBroken = false;
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
                ShiftCd = 0.6f;
                ToggleMindustryMode();
            }
            else if (CharKey == "slark")
            {
                Run.PlayerVelY = 11.5f;
                Run.PlayerGrounded = false;
                Run.ApplyDash(Run.PlayerForward * 26.0f * nrgMult, 0.26f);
                PerformMeleeAttack(6.5f, 4.2f, 2.2f, Rgb(95, 255, 215), stunDur: 2.2f, aoe: 2.8f);
            }
            else if (CharKey == "tidehunter")
            {
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 40.0f);
                Run.ApplyDash(Run.PlayerForward * 22.0f * nrgMult, 0.24f);
                PerformMeleeAttack(6.0f, 4.5f, 2.0f, Rgb(80, 235, 195), stunDur: 1.5f, knockback: 4.5f, aoe: 3.5f);
            }
            else if (CharKey == "freeman")
            {
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 25.0f);
                Run.PlayerVelY = 12.0f;
                Run.PlayerGrounded = false;
                Run.ApplyDash(Run.PlayerForward * 29.0f * nrgMult, 0.26f);
            }
            else if (CharKey == "cheburashka")
            {
                Run.StageEntities.Add(new ShapoklyakThrowFX(myPos - Run.PlayerForward * 1.3f, Run.PlayerEntity.RotationY));
                float boostY = GenaStandTimer > 0f ? 21.5f : 17.5f;
                float boostFwd = (GenaStandTimer > 0f ? 32.0f : 25.0f) * nrgMult;
                Run.PlayerVelY = boostY;
                Run.PlayerGrounded = false;
                ChebWasAirborne = true;
                ChebPeakAirY = myPos.Y + 4.0f;
                Run.ApplyDash(Run.PlayerForward * boostFwd, 0.32f);
                new VisualFX(myPos + new Vector3(0, 1.0f, 0), Color.Orange, 3.5f, 0.28f);
                Run.ShowBanner(GenaStandTimer > 0f
                    ? "ШАПОКЛЯК И ГЕНА :: СВЕРХЗВУКОВОЙ ЗАПУСК ЧЕБУРАШКИ!"
                    : "ШАПОКЛЯК: «КТО ЛЮДЯМ ПОМОГАЕТ — ТОТ ТРАТИТ ВРЕМЯ ЗРЯ!» (БРОСОК ВПЕРЁД!)", Color.Orange, 2.0f);
            }
            else if (CharKey == "dante")
            {
                DanteSpdBuffTimer = 6.0f;
                Run.HealPlayer(Run.PlayerMaxHp * 0.30f);
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 45.0f);
                foreach (var e in CombatManager.Enemies)
                    if (IsValid(e) && e.IsAlly) e.HealAlly(e.MaxHp * 0.45f);
                if (Net.NetworkManager.Instance.Connected)
                    Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject { ["type"] = "dante_buff", ["kind"] = "def" });
                new VisualFX(myPos + new Vector3(0, 1.2f, 0), Color.Gold, 8.0f, 0.35f);
                Run.ShowBanner("ЗАЩИТНЫЙ ПРОТОКОЛ ДАНТЕ :: +45 ЩИТА, ИСЦЕЛЕНИЕ И УСКОРЕНИЕ ОТРЯДА!", Color.Gold, 2.4f);
            }
        }

        private void FireGwenNeedleWave(int count, float dmgMult)
        {
            Vector3 myPos = Run.PlayerPos + new Vector3(0, 1.55f, 0);
            Vector3 baseDir = GetAimDirection();
            float stepDeg = count == 1 ? 0f : 9f;
            float startDeg = -stepDeg * (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float rad = (startDeg + i * stepDeg) * (MathF.PI / 180f);
                Vector3 d = Vector3.Normalize(new Vector3(baseDir.X * MathF.Cos(rad) - baseDir.Z * MathF.Sin(rad), baseDir.Y, baseDir.X * MathF.Sin(rad) + baseDir.Z * MathF.Cos(rad)));
                var needleProj = new Projectile(myPos, d, 68f, BaseDamage * dmgMult, 0.48f, Rgb(110, 225, 255), isPlayer: true, aoe: 2.8f, pierce: true, lifesteal: 4.5f);
                // Превращаем стандартный шарик в длинную 3D-иглу с золотым ушком и сияющим остриём
                ClearChildren(needleProj);
                needleProj.Model = "";
                needleProj.LookAt(myPos + d * 10f);
                // Основное стальное/лазурное тело иглы
                new Entity(parent: needleProj, model: "diamond", scale: new(0.22f, 0.22f, 3.8f), color: Rgb(135, 235, 255), unlit: true);
                // Острое светящееся жало иглы спереди
                new Entity(parent: needleProj, model: "diamond", scale: new(0.14f, 0.14f, 1.8f), position: new(0, 0, 1.4f), color: Color.White, unlit: true);
                // Золотое ушко иглы и энергетическая нить сзади
                new Entity(parent: needleProj, model: "diamond", scale: new(0.36f, 0.16f, 0.65f), position: new(0, 0, -1.65f), color: Color.Gold, unlit: true);
                new Entity(parent: needleProj, model: "cube", scale: new(0.06f, 0.06f, 1.6f), position: new(0, 0, -2.5f), color: Rgb(90, 215, 255, 180), unlit: true);
            }
            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(e.Position, Run.PlayerPos) <= 26f)
                    e.SlowTimer = MathF.Max(e.SlowTimer, 2.5f + count * 0.8f);
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
            if (CharKey == "freeman")
            {
                if (!TauCharging)
                {
                    TauCharging = true;
                    TauCharge = 0.25f;
                    Run.ShowBanner("ТАУ-ПУШКА: УДЕРЖИВАЙТЕ [R] ДЛЯ НАКОПЛЕНИЯ ЗАРЯДА ГАУССА!", Color.Yellow, 1.8f);
                }
                return;
            }

            float alienCdr = MathF.Pow(0.80f, CombatManager.GetItemCount("alien_head"));
            float cellCdr = MathF.Pow(0.75f, CombatManager.GetItemCount("lysate_cell"));
            RCd = MathF.Max(0.5f, Cfg.Skills["r"].Cd * alienCdr * cellCdr);
            Run.Parts.TriggerSkillAnim("r", 0.65f);
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
                DragonInstallTimer = 8.0f;
                Run.Parts.SetUltForm(true);
                new VisualFX(myPos + new Vector3(0, 1.5f, 0), Rgb(255, 60, 20), 11.0f, 0.45f);
                foreach (var e in CombatManager.Enemies.ToArray())
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(e.Position, myPos) <= 11.0f)
                        e.TakeDamage(BaseDamage * 3.4f);
                Run.ShowBanner("ДРАКОНИЙ ИНСТАЛЛ :: ИСТИННАЯ ФОРМА ГИРА ПРОБУЖДЕНА!", Rgb(255, 120, 30), 2.5f);
            }
            else if (CharKey == "gwen")
            {
                // Вышивание: 1 игла сразу -> 2 иглы через 0.5с -> 3 иглы ещё через 0.5с с повышением урона и замедлением
                FireGwenNeedleWave(1, 1.8f);
                GwenUltStage = 1;
                GwenUltTimer = 0.50f;
                Run.ShowBanner("ВЫШИВАНИЕ [1/3] -> СЛЕДОМ 2 И 3 ИГЛЫ!", Rgb(120, 235, 255), 1.6f);
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
                Run.ShowBanner($"СИСТЕМА DUMMY LINK :: АВТОНОМНЫЙ ОТРЯД M4A1 ({DummyClones.Count}/4 КЛОНОВ)!", Rgb(95, 255, 135), 2.6f);
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
                if (Net.NetworkManager.Instance.Connected && Physics.SpecialMode != "pvp")
                {
                    Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "ally_heal",
                        ["target_id"] = "all",
                        ["heal"] = 65f,
                        ["uber"] = true
                    });
                }
                Run.ShowBanner("УБЕР-ЗАРЯД АКТИВЕН :: НЕУЯЗВИМОСТЬ И УСИЛЕНИЕ ОТРЯДА (6.5 СЕК)!", Rgb(255, 75, 90), 3.0f);
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
                if (!MindustryBuildMode)
                {
                    RCd = 0.4f;
                    Run.ShowBanner("ВЕРНИТЕСЬ В СТРОИТЕЛЬНЫЙ РЕЖИМ [Shift / B] ДЛЯ УПРАВЛЕНИЯ СИСТЕМАМИ!", Color.Orange, 1.8f);
                    return;
                }
                RCd = 9.0f;
                MindustryOverdriveTimer = 5.0f;
                new VisualFX(Run.PlayerPos, Color.Gold, 8.0f, 0.38f);
                Run.ShowBanner("ПЕРЕГРУЗКА ЯДРА (5 СЕК) :: ВСЕ БУРЫ, ЗАВОДЫ И ТУРЕЛИ РАБОТАЮТ ВДВОЕ БЫСТРЕЕ!", Color.Gold, 2.5f);
            }
            else if (CharKey == "slark")
            {
                SlarkShadowTimer = 5.5f;
                new VisualFX(myPos + new Vector3(0, 1.2f, 0), Rgb(35, 45, 75), 8.5f, 0.45f);
                Run.ShowBanner("ТАНЕЦ ТЕНЕЙ (SHADOW DANCE) :: НЕУЯЗВИМОСТЬ И РЕГЕНЕРАЦИЯ!", Rgb(100, 255, 215), 3.0f);
            }
            else if (CharKey == "cheburashka")
            {
                RCd = 60.0f * alienCdr * cellCdr;
                GenaStandTimer = 30.0f;
                if (Run.Parts.ShieldAura != null)
                    Run.Parts.ShieldAura.Enabled = true;
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 50.0f);
                Run.HealPlayer(Run.PlayerMaxHp * 0.35f);
                new VisualFX(myPos + new Vector3(0, 1.5f, 0), Color.Gold, 11.0f, 0.45f);
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(e.Position, myPos) <= 10.5f)
                    {
                        if (!e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, 1.8f);
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight, 0), BaseDamage * 2.8f, Color.Gold, aoe: 2.5f);
                    }
                }
                Run.ShowBanner("СТЕНД ПРИЗВАН: 『КРОКОДИЛ ГЕНА』 (30 СЕКУНД) :: ПУСТЬ БЕГУТ НЕУКЛЮЖЕ!", Color.Gold, 4.0f);
            }
            else if (CharKey == "tidehunter")
            {
                new VisualFX(myPos + new Vector3(0, 1.0f, 0), Rgb(75, 235, 185), 18.0f, 0.55f);
                for (int i = 0; i < 12; i++)
                {
                    float ang = (i / 12.0f) * MathF.PI * 2f;
                    Vector3 tPos = myPos + new Vector3(MathF.Cos(ang) * 10.5f, 0, MathF.Sin(ang) * 10.5f);
                    new OrbitalStrikeFX(tPos, 0f);
                }
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(e.Position, myPos) <= 18.0f)
                    {
                        if (!e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, 2.8f);
                        e.SlowTimer = MathF.Max(e.SlowTimer, 4.5f);
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight, 0), BaseDamage * 3.8f, Rgb(80, 245, 195), aoe: 2.5f, lifesteal: 3.0f);
                    }
                }
                Run.ShowBanner("ОПУСТОШЕНИЕ (RAVAGE) :: ЗОВ ЛЕВИАФАНА БЕЗДНЫ!", Rgb(95, 255, 200), 3.0f);
            }
            else if (CharKey == "dante")
            {
                bool anyInjuredMinion = false;
                foreach (var e in CombatManager.Enemies)
                    if (IsValid(e) && e.IsAlly && e.Hp < e.MaxHp * 0.9f) { anyInjuredMinion = true; break; }

                if (Net.NetworkManager.Instance.Connected)
                {
                    if (!Program.HasAnyDeadCoopPlayers())
                    {
                        RCd = 0.25f;
                        new VisualFX(myPos + new Vector3(0, 1.4f, 0), Color.Gold, 3.5f, 0.25f);
                        Run.ShowBanner("ВСЕ ГРЕШНИКИ ЖИВЫ — ПОВОРОТ ЧАСОВ НЕ ТРЕБУЕТСЯ!", Color.Gold, 1.8f);
                        return;
                    }
                    foreach (var e in CombatManager.Enemies)
                        if (IsValid(e) && e.IsAlly) e.HealAlly(e.MaxHp, uberDur: 8.0f);
                    new VisualFX(myPos + new Vector3(0, 1.5f, 0), Color.Gold, 16.0f, 0.55f);
                    Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "dante_revive",
                        ["healer"] = Net.NetworkManager.Instance.MyId
                    });
                    Run.ShowBanner("ПОВОРОТ ЧАСОВ ВСПЯТЬ :: ГРЕШНИКИ ВОЗРОЖДЕНЫ ЦЕНОЙ ЖИЗНИ ДАНТЕ!", Color.Gold, 4.5f);
                    Program.OnPlayerDefeated();
                }
                else
                {
                    if (!anyInjuredMinion && Run.PlayerHp >= Run.PlayerMaxHp * 0.65f)
                    {
                        RCd = 0.25f;
                        new VisualFX(myPos + new Vector3(0, 1.4f, 0), Color.Gold, 3.5f, 0.25f);
                        Run.ShowBanner("ЧАСЫ СТАБИЛЬНЫ (ЗДОРОВЬЕ В НОРМЕ) — УЛЬТА СОХРАНЕНА!", Color.Gold, 1.8f);
                        return;
                    }
                    Run.PlayerHp = MathF.Max(1f, Run.PlayerHp * 0.5f);
                    Shield = Run.PlayerMaxHp * 1.2f;
                    DanteAtkBuffTimer = 9.0f;
                    DanteSpdBuffTimer = 9.0f;
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 tPos = myPos + new Vector3(MathF.Cos(i * 1.047f) * 8.5f, 0, MathF.Sin(i * 1.047f) * 8.5f);
                        new OrbitalStrikeFX(tPos, BaseDamage * 3.2f);
                    }
                    Run.ShowBanner("ПОВОРОТ ЧАСОВ ВСПЯТЬ :: ЖЕРТВА HP РАДИ БАРЬЕРА И СИЛЫ ВЕТВИ!", Color.Gold, 3.2f);
                    Program.OnPlayerDefeated();
                }
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
            if (TonicBuffTimer > 0f)
            {
                TonicBuffTimer -= dt;
                if (TonicBuffTimer <= 0f)
                    Run.ExpireSpinelTonic();
            }

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

            if (DragonInstallTimer > 0f)
            {
                DragonInstallTimer -= dt;
                Run.HealPlayer(6.0f * dt);
                if (CharKey == "sol") Run.Parts.SetUltForm(true);
                if (DragonInstallTimer <= 0f && CharKey == "sol")
                {
                    Run.Parts.SetUltForm(false);
                    new VisualFX(Run.PlayerPos + new Vector3(0, 1.2f, 0), Rgb(255, 90, 25), 3.5f, 0.28f);
                }
            }
            if (UberTimer > 0f) { UberTimer -= dt; Run.HealPlayer(12.0f * dt); }
            if (OrgiaTimer > 0f) OrgiaTimer -= dt;
            if (SlarkShadowTimer > 0f)
            {
                SlarkShadowTimer -= dt;
                Run.HealPlayer((Run.PlayerMaxHp * 0.12f + 8.0f) * dt);
            }

            if (CharKey == "mindustry" && MindustryCoreRebuildTimer > 0f)
            {
                MindustryCoreRebuildTimer -= dt;
                if (MindustryCoreRebuildTimer <= 0f)
                {
                    Run.ShowBanner("ВРЕМЯ НА ВОССТАНОВЛЕНИЕ ЯДРА ИСТЕКЛО (60 СЕК)! КОРАБЛЬ УНИЧТОЖЕН!", Color.Red, 4.5f);
                    Program.OnPlayerDefeated();
                    return;
                }
            }

            // Bustling Fungus (Bungus): быстрая активация за 0.55с
            int bungus = CombatManager.GetItemCount("bungus");
            if (!isMoving && Run.PlayerGrounded && Run.DashTimer <= 0f && !GrappleActive)
                StationaryTimer += dt;
            else
                StationaryTimer = 0f;

            if (bungus > 0 && StationaryTimer >= 0.55f)
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

            // Frost Relic (тонкое кольцо у самой земли)
            int frost = CombatManager.GetItemCount("frost_relic");
            if (FrostRelicTimer > 0f && frost > 0)
            {
                FrostRelicTimer -= dt;
                float fRad = 6.5f + 1.5f * frost;
                FrostRing.Enabled = true;
                FrostRing.Scale = new Vector3(fRad * 2f, 0.06f, fRad * 2f);
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

            // Таймеры Сола, Гвен, Силовика, Данте, Медика и Фримана
            SolBanditWindow = MathF.Max(0f, SolBanditWindow - dt);
            EnforcerParryTimer = MathF.Max(0f, EnforcerParryTimer - dt);
            GwenMistAtkBuff = MathF.Max(0f, GwenMistAtkBuff - dt);
            DanteAtkBuffTimer = MathF.Max(0f, DanteAtkBuffTimer - dt);
            DanteSpdBuffTimer = MathF.Max(0f, DanteSpdBuffTimer - dt);

            if (CharKey == "cheburashka")
            {
                if (GenaStandTimer > 0f)
                {
                    GenaStandTimer -= dt;
                    Run.HealPlayer(6.0f * dt);
                    if (Run.Parts.ShieldAura != null)
                    {
                        Run.Parts.ShieldAura.Enabled = true;
                        Run.Parts.ShieldAura.Y = 0.85f + MathF.Sin((float)Raylib.GetTime() * 6.0f) * 0.14f;
                    }
                    if (GenaStandTimer <= 0f)
                    {
                        if (Run.Parts.ShieldAura != null) Run.Parts.ShieldAura.Enabled = false;
                        Run.ShowBanner("СТЕНД 『КРОКОДИЛ ГЕНА』 ЗАВЕРШИЛ ДЕЙСТВИЕ", Color.Orange, 2.2f);
                    }
                }

                // Пассивка Чебурашки: отслеживание максимальной высоты и удар по области при приземлении
                if (!Run.PlayerGrounded)
                {
                    if (!ChebWasAirborne)
                    {
                        ChebWasAirborne = true;
                        ChebPeakAirY = Run.PlayerPos.Y;
                    }
                    else if (Run.PlayerPos.Y > ChebPeakAirY)
                    {
                        ChebPeakAirY = Run.PlayerPos.Y;
                    }
                }
                else if (ChebWasAirborne)
                {
                    ChebWasAirborne = false;
                    float fallHeight = ChebPeakAirY - Run.PlayerPos.Y;
                    if (fallHeight >= 2.2f)
                    {
                        float hClamped = Math.Clamp(fallHeight, 2.2f, 45.0f);
                        float slamRadius = Math.Clamp(4.5f + hClamped * 0.42f, 5.0f, 22.0f);
                        float slamDmg = BaseDamage * (1.2f + hClamped * 0.28f);
                        Color slamCol = GenaStandTimer > 0f ? Color.Gold : Rgb(255, 150, 25);

                        new VisualFX(Run.PlayerPos + new Vector3(0, 0.5f, 0), slamCol, slamRadius * 1.35f, 0.38f);
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (IsValid(e) && !e.IsAlly && Physics.Distance2D(Run.PlayerPos, e.Position) <= slamRadius && MathF.Abs(e.Y - Run.PlayerPos.Y) <= 5.0f)
                            {
                                if (!e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, Math.Clamp(0.6f + hClamped * 0.08f, 0.8f, 3.0f));
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + new Vector3(0, e.HitHeight, 0), slamDmg, slamCol, aoe: 2.0f, lifesteal: 2.5f, canProc: true);
                            }
                        }
                        if (fallHeight >= 7.0f)
                            Run.ShowBanner($"ЧЕБУРАХНУЛСЯ С ВЫСОТЫ {fallHeight:F0}М! (УРОН {(int)slamDmg}, РАДИУС {slamRadius:F0}М)", slamCol, 2.0f);
                    }
                }
            }

            if (SolBringerState == 1)
            {
                SolBringerTimer -= dt;
                if (SolBringerTimer <= 0f)
                {
                    SolBringerState = 2;
                    SolBringerTimer = 0.32f;
                    Run.PlayerVelY = -28.0f;
                    Run.ApplyDash(SolBringerDir * 24.0f, 0.28f);
                }
            }
            else if (SolBringerState == 2)
            {
                SolBringerTimer -= dt;
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.0f, 0), Rgb(255, 115, 25), 2.4f, 0.15f);
                if (Run.PlayerGrounded || SolBringerTimer <= 0f)
                {
                    SolBringerState = 0;
                    new VisualFX(Run.PlayerPos + new Vector3(0, 0.8f, 0), Rgb(255, 90, 20), 7.5f, 0.35f);
                    PerformMeleeAttack(7.0f, 5.2f, 3.0f, Rgb(255, 100, 20), lifesteal: 6.0f, stunDur: 1.2f, knockback: 4.5f, aoe: 4.5f);
                }
            }

            if (GwenFlurryShots > 0)
            {
                GwenFlurryTimer -= dt;
                if (GwenFlurryTimer <= 0f)
                {
                    GwenFlurryTimer = 0.055f;
                    GwenFlurryShots--;
                    Run.Parts.TriggerSkillAnim("m1", 0.12f);
                    PerformMeleeAttack(5.2f, 3.4f, 0.58f, Rgb(135, 240, 255), lifesteal: 1.5f, scissorCross: true, aoe: 1.6f);
                }
            }

            if (GwenUltStage > 0)
            {
                GwenUltTimer -= dt;
                if (GwenUltTimer <= 0f)
                {
                    Run.Parts.TriggerSkillAnim("r", 0.35f);
                    if (GwenUltStage == 1)
                    {
                        FireGwenNeedleWave(2, 2.3f);
                        GwenUltStage = 2;
                        GwenUltTimer = 0.50f;
                    }
                    else
                    {
                        FireGwenNeedleWave(3, 2.9f);
                        GwenUltStage = 0;
                    }
                }
            }

            if (CharKey == "medic")
            {
                if (Raylib.IsKeyPressed(KeyboardKey.G) && MedicTauntTimer <= 0f)
                {
                    MedicTauntTimer = 2.8f;
                    Run.Parts.Root.Enabled = true;
                    if (FpRoot != null) FpRoot.Enabled = false;
                    new VisualFX(Run.PlayerPos + new Vector3(0, 1.2f, 0), Color.Lime, 6.5f, 0.45f);
                    Run.HealPlayer(25.0f);
                    Run.ShowBanner("НАСМЕШКА МЕДИКА :: «OKTOBERFEST!» ♪ (+ЛЕЧЕНИЕ)", Color.Lime, 2.8f);
                }
                if (MedicTauntTimer > 0f)
                {
                    MedicTauntTimer -= dt;
                    Run.PlayerEntity.RotationY += 160f * dt;
                    Run.Parts.RightArm.RotationX = -110f + MathF.Sin((float)Raylib.GetTime() * 14f) * 35f;
                    Run.Parts.LeftArm.RotationX = -75f - MathF.Sin((float)Raylib.GetTime() * 14f) * 35f;
                    if (MedicTauntTimer <= 0f)
                    {
                        Run.Parts.Root.Enabled = false;
                        if (FpRoot != null) FpRoot.Enabled = true;
                        Run.Parts.RightArm.RotationX = 0f;
                        Run.Parts.LeftArm.RotationX = 0f;
                    }
                }
            }

            if (CharKey == "freeman" && TauCharging)
            {
                if (Raylib.IsKeyDown(KeyboardKey.R))
                {
                    TauCharge = MathF.Min(2.5f, TauCharge + dt);
                    if (FpWeaponHolder != null)
                        FpWeaponHolder.X = 0.36f + MathF.Sin((float)Raylib.GetTime() * 45f) * 0.015f * TauCharge;
                }
                else
                {
                    TauCharging = false;
                    float ratio = Math.Clamp(TauCharge / 2.5f, 0.2f, 1.0f);
                    RCd = 4.5f;
                    V1AnimateWeaponRecoil(-0.30f, -26f);
                    Vector3 eyePos = Run.PlayerPos + new Vector3(0, 1.65f, 0);
                    Vector3 aim = GetAimDirection();
                    new LightningBeam(eyePos, eyePos + aim * 55f, Color.Yellow, 0.25f + 0.35f * ratio);
                    new Projectile(eyePos, aim, 125f, BaseDamage * (2.2f + 4.8f * ratio), 0.45f + 0.35f * ratio, Color.Gold, isPlayer: true, aoe: 3.5f + 4.5f * ratio, pierce: true);
                    Vector3 recoil = -aim * (12.0f + 24.0f * ratio);
                    Run.PlayerVelY = MathF.Max(Run.PlayerVelY, recoil.Y + 6.0f * ratio);
                    Run.PlayerGrounded = false;
                    Run.ApplyDash(new Vector3(recoil.X, 0, recoil.Z), 0.26f);
                    TauCharge = 0f;
                }
            }

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

            // Обновление механик V1 и покачивания оружия от 1-го лица
            if (CharKey == "v1")
                UpdateV1Mechanics(dt, isMoving);
            else if (IsFirstPersonCharacter(CharKey) && FpWeaponHolder != null && !FpWeaponHolder.IsDestroyed)
            {
                float t = (float)Raylib.GetTime();
                float inX = (Raylib.IsKeyDown(KeyboardKey.D) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.A) ? 1f : 0f);
                float bobY = (isMoving && Run.PlayerGrounded) ? MathF.Sin(t * 13.0f) * 0.020f : MathF.Sin(t * 2.5f) * 0.005f;
                float bobX = (isMoving && Run.PlayerGrounded) ? MathF.Cos(t * 6.5f) * 0.016f : 0f;
                float airOffset = !Run.PlayerGrounded ? Math.Clamp(-Run.PlayerVelY * 0.004f, -0.05f, 0.05f) : 0f;
                FpWeaponHolder.X = Lerp(FpWeaponHolder.X, 0.36f + bobX, dt * 12.0f);
                FpWeaponHolder.Y = Lerp(FpWeaponHolder.Y, -0.27f + bobY + airOffset, dt * 12.0f);
                FpWeaponHolder.RotationZ = Lerp(FpWeaponHolder.RotationZ, -inX * 5.5f, dt * 10.0f);
                if (FpLeftArm != null && !FpLeftArm.IsDestroyed)
                {
                    FpLeftArm.Y = Lerp(FpLeftArm.Y, -0.34f - bobY * 0.8f + airOffset, dt * 12.0f);
                    FpLeftArm.RotationZ = Lerp(FpLeftArm.RotationZ, -inX * 4.0f, dt * 10.0f);
                }
            }
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
            if (CharKey == "mindustry" && MindustryBuildMode)
            {
                // В строительном режиме корабль парит высоко над локацией и неуязвим!
                return;
            }
            if (CharKey == "enforcer" && EnforcerParryTimer > 0f)
            {
                TryParryExternalAttack(amount, Run.PlayerPos + Run.PlayerForward * 3f);
                return;
            }
            if (CharKey == "reimu" && !Run.PlayerGrounded)
            {
                ReimuIsFlying = false;
                ReimuFlightBroken = true;
                Run.ShowBanner("ПОЛЁТ РЕЙМУ ПРЕРВАН ПОПАДАНИЕМ!", Color.Orange, 1.4f);
            }
            if (UberTimer > 0f || SlarkShadowTimer > 0f)
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

            Program.TriggerDamageFlash(dmg);
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
                Program.OnPlayerDefeated();
            }
        }
    }
}