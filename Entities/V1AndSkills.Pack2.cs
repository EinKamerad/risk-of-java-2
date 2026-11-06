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
    public class HeliosRaveGateEntity : Entity
    {
        public float Lifetime = 5.0f;
        private float _pulseTick = 0.15f;
        private float _boostCd = 0f;

        public HeliosRaveGateEntity(Vector3 pos)
            : base(model: "cylinder", position: pos + new Vector3(0, 0.18f, 0), scale: new(4.6f, 0.14f, 4.6f), color: Rgb(0, 245, 255, 135), unlit: true)
        {
            new Entity(parent: this, model: "cylinder", position: new(0, 0.12f, 0), scale: new(0.78f, 1.2f, 0.78f), color: Rgb(255, 55, 200, 155), unlit: true);
            for (int i = 0; i < 4; i++)
            {
                float ang = i * MathF.PI * 0.5f;
                new Entity(
                    parent: this, model: "diamond",
                    position: new(MathF.Cos(ang) * 0.48f, 1.8f, MathF.Sin(ang) * 0.48f),
                    scale: new(0.12f, 1.9f, 0.12f),
                    color: (i % 2 == 0) ? Rgb(0, 255, 240) : Rgb(255, 70, 210), unlit: true);
            }
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var g = WorldContext.Game;
            var p = CombatManager.Controller;
            if (g == null || p == null) { Destroy(this); return; }

            Lifetime -= dt;
            if (Lifetime <= 0f) { Destroy(this); return; }

            RotationY += 210f * dt;
            if (_boostCd > 0f) _boostCd -= dt;

            if (_boostCd <= 0f && Physics.Distance2D(g.PlayerPos, Position) <= 4.8f && p.CharKey == "daitaku_helios")
            {
                _boostCd = 1.1f;
                p.HeliosHype = Math.Clamp(p.HeliosHype + 0.22f, 0f, 1f);
                p.ShiftCd *= 0.50f;
                g.HealPlayer(6f);
                SoundManager.Play("bounce_pad", 0.55f, 1.35f);
            }

            _pulseTick -= dt;
            if (_pulseTick <= 0f)
            {
                _pulseTick = 0.85f;
                new VisualFX(Position + new Vector3(0, 0.5f, 0), Rgb(255, 65, 210), 4.2f, 0.24f);
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position) <= 5.5f)
                    {
                        e.SlowTimer = MathF.Max(e.SlowTimer, 1.4f);
                        CombatManager.ApplyPlayerHitEffects(
                            e, e.Position + new Vector3(0, e.HitHeight * 0.5f, 0),
                            p.BaseDamage * 1.15f, Rgb(0, 245, 255), aoe: 0f, canProc: true);
                    }
                }
            }
        }
    }

    public class FalseSonLaserChannelFX : Entity
    {
        private readonly SurvivorCombatController _owner;
        public float Remaining;
        private float _tickTimer = 0f;

        public FalseSonLaserChannelFX(SurvivorCombatController owner, float duration)
            : base(model: "diamond", position: owner.Run.PlayerPos + new Vector3(0, 1.6f, 0), scaleUniform: 0.72f, color: Rgb(110, 245, 255), unlit: true)
        {
            _owner = owner;
            Remaining = duration;
            new Entity(parent: this, model: "sphere", scaleUniform: 1.55f, color: Rgb(255, 215, 80, 125), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var g = WorldContext.Game;
            if (g == null || _owner == null || _owner.CharKey != "false_son") { Destroy(this); return; }

            Remaining -= dt;
            if (Remaining <= 0f) { Destroy(this); return; }

            Vector3 aim = _owner.Pack2AimDirection;
            Vector3 start = g.PlayerPos + new Vector3(0, 1.55f, 0) + aim * 0.95f;
            Position = start;
            RotationY += 420f * dt;
            RotationZ += 310f * dt;

            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = 0.12f;
                Vector3 end = start + aim * 42f;
                new LightningBeam(start, end, Rgb(95, 245, 255), 0.15f);
                new LightningBeam(start + new Vector3(0, 0.08f, 0), end, Rgb(255, 215, 75), 0.13f);
                SoundManager.Play("laser", 0.48f, 0.92f + (float)Random.Shared.NextDouble() * 0.16f);

                float bonusHp = MathF.Max(0f, g.PlayerMaxHp - _owner.Cfg.Hp);
                float tickDmg = _owner.BaseDamage * 0.72f + bonusHp * 0.08f;
                Vector3 seg = end - start;
                float segLenSq = MathF.Max(0.001f, seg.LengthSquared());

                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly) continue;
                    Vector3 center = e.Position + new Vector3(0, e.HitHeight * 0.55f, 0);
                    float t = Math.Clamp(Vector3.Dot(center - start, seg) / segLenSq, 0f, 1f);
                    Vector3 closest = start + seg * t;
                    if (Vector3.Distance(center, closest) <= e.HitRadius + 1.65f)
                    {
                        e.LunarRuinStacks = Math.Min(12, e.LunarRuinStacks + 1);
                        e.LunarRuinTimer = 6.0f;
                        CombatManager.ApplyPlayerHitEffects(e, center, tickDmg, Rgb(100, 240, 255), aoe: 1.5f, lifesteal: 0.03f, canProc: true);
                    }
                }
                new VisualFX(end, Rgb(110, 245, 255), 1.9f, 0.15f);
            }
        }
    }

    public class KoishiRoseStormFX : Entity
    {
        private readonly SurvivorCombatController _owner;
        private readonly bool _homingVariant;
        public float Remaining = 6.0f;
        private float _waveTimer = 0.05f;
        private float _spinAngle = 0f;

        public KoishiRoseStormFX(SurvivorCombatController owner, bool homingVariant)
            : base(model: "diamond", position: owner.Run.PlayerPos + new Vector3(0, 1.25f, 0), scaleUniform: 0.55f, color: Rgb(255, 65, 135, 180), unlit: true)
        {
            _owner = owner;
            _homingVariant = homingVariant;
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.PI / 3f;
                new Entity(
                    parent: this, model: "diamond",
                    position: new(MathF.Cos(a) * 2.2f, 0f, MathF.Sin(a) * 2.2f),
                    scale: new(0.55f, 0.25f, 0.85f),
                    color: (i % 2 == 0) ? Rgb(255, 55, 120) : Rgb(75, 255, 165), unlit: true);
            }
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            var g = WorldContext.Game;
            if (g == null || _owner == null || _owner.CharKey != "komeiji") { Destroy(this); return; }

            Remaining -= dt;
            if (Remaining <= 0f) { Destroy(this); return; }

            Position = g.PlayerPos + new Vector3(0, 1.15f, 0);
            RotationY += 260f * dt;

            _waveTimer -= dt;
            if (_waveTimer <= 0f)
            {
                _waveTimer = 0.46f;
                _spinAngle += 0.42f;
                int petals = 8;
                float dmg = _owner.BaseDamage * 1.18f;
                SoundManager.Play("magic_cast", 0.52f, 1.25f);
                new VisualFX(Position, Rgb(255, 70, 140), 3.4f, 0.22f);

                for (int i = 0; i < petals; i++)
                {
                    float ang = _spinAngle + i * (MathF.PI * 2f / petals);
                    Vector3 dir = new(MathF.Cos(ang), 0.02f, MathF.Sin(ang));
                    Color col = (i % 2 == 0) ? Rgb(255, 60, 130) : Rgb(70, 255, 165);
                    var proj = new Projectile(
                        Position + dir * 0.9f, dir, 34f, dmg, 0.36f, col,
                        aoe: 1.6f, pierce: !_homingVariant, homing: _homingVariant, lifesteal: 0.04f, maxLifetime: 1.8f);
                    g.StageEntities.Add(proj);
                }
            }
        }
    }

    public partial class SurvivorCombatController
    {
        // --- Состояния Pack 2 ---
        public float HeliosHype = 0f;
        private float _heliosUltTimer = 0f;
        private float _heliosBassTick = 0f;

        private int _robinNoteCount = 0;
        private float _robinAriaTimer = 0f;
        private float _robinConcertTimer = 0f;
        private float _robinConcertWaveTick = 0f;
        private float _robinHarmonyCritTimer = 0f;

        private float _bigBirdAuraTick = 0f;
        private float _bigBirdDarkStepTimer = 0f;
        private float _bigBirdBiteUltTimer = 0f;
        private float _smallBirdEnrageTimer = 0f;
        private float _smallBirdGuardTimer = 0f;

        private int _apocEggPhase = 0;
        public int ApocEggPhase => _apocEggPhase;
        private float _apocUltTimer = 0f;
        private float _apocUltStrikeTick = 0f;
        private float _apocAuraTick = 0f;

        private float _satoriSubconsciousTick = 1.6f;
        private float _koishiStealth = 0f;
        private float _koishiOutOfCombatTimer = 0f;
        private float _koishiPhaseTimer = 0f;
        private float _koishiUltTimer = 0f;

        public int FalseSonSpikes = 4;
        private bool _falseSonSpikesInit = false;
        private float _falseSonSpikeRegenTimer = 0f;
        private float _falseSonRegenAccum = 0f;
        private int _falseSonClubCombo = 0;
        private float _falseSonUltTimer = 0f;
        private float _falseSonMeridianWillTimer = 0f;

        public Vector3 Pack2AimDirection => GetAimDirection();

        public int GetFalseSonMaxSpikes()
        {
            float bonusHp = MathF.Max(0f, Run.PlayerMaxHp - Cfg.Hp);
            return 4 + Math.Min(12, (int)(bonusHp / 40f));
        }

        private CombatTarget? FindBestAimedEnemy(Vector3 origin, Vector3 aimDir, float maxRange, float minDot = 0.35f)
        {
            CombatTarget? best = null;
            float bestScore = -9999f;
            Vector3 normAim = aimDir.LengthSquared() > 0.001f ? Vector3.Normalize(aimDir) : Vector3.UnitZ;
            foreach (var e in CombatManager.Enemies)
            {
                if (!IsValid(e) || e.IsAlly) continue;
                Vector3 toE = (e.Position + new Vector3(0, e.HitHeight * 0.5f, 0)) - origin;
                float dist = toE.Length();
                if (dist > maxRange || dist < 0.001f) continue;
                float dot = Vector3.Dot(toE / dist, normAim);
                if (dot < minDot) continue;
                float score = dot * 30f - dist * 0.45f;
                if (score > bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        private void TriggerSmallBirdEnrage(float dur = 5.5f)
        {
            bool wasEnraged = _smallBirdEnrageTimer > 0.1f;
            _smallBirdEnrageTimer = MathF.Max(_smallBirdEnrageTimer, dur);
            Run.Parts.SetUltForm(true);
            if (!wasEnraged)
            {
                SoundManager.Play("ult_cast", 0.85f, 1.25f);
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.1f, 0), Rgb(230, 25, 45), 3.4f, 0.32f);
                Run.ShowBanner("КАРАЮЩАЯ ПАСТЬ!", Rgb(255, 55, 70), 1.6f);
            }
        }

        private void RobinTriggerHarmonyStrike(Vector3 refPos, float dmgScale)
        {
            CombatTarget? target = FindBestAimedEnemy(Run.PlayerPos + Vector3.UnitY, GetAimDirection(), 34f, 0.15f)
                                ?? CombatManager.FindNearestHostileAt(refPos);
            if (target == null || Vector3.Distance(Run.PlayerPos, target.Position) > 36f) return;

            Vector3 hitPos = target.Position + new Vector3(0, target.HitHeight * 0.6f, 0);
            new LightningBeam(hitPos + new Vector3(0, 14f, 0), hitPos, Rgb(255, 225, 110), 0.28f);
            new VisualFX(hitPos, Rgb(255, 235, 150), 2.4f, 0.24f);
            SoundManager.Play("zap", 0.52f, 1.42f);
            CombatManager.ApplyPlayerHitEffects(target, hitPos, BaseDamage * 1.45f * dmgScale, Rgb(255, 225, 110), aoe: 2.6f, canProc: true);
        }

        private float GetPack2DamageMult() => CharKey switch
        {
            "daitaku_helios" => 1.0f + 0.25f * HeliosHype + (_heliosUltTimer > 0f ? 0.15f : 0f),
            "robin" => 1.0f + (_robinAriaTimer > 0f ? 0.28f : 0f) + (_robinConcertTimer > 0f ? 0.35f : 0f),
            "bird_trio" => ProgressData.Instance.GetSelectedSkin("bird_trio") == 2 && _smallBirdEnrageTimer > 0f ? 1.80f : 1.0f,
            "apocalypse_bird" => 1.10f + (_apocUltTimer > 0f ? 0.25f : 0f) + (_apocEggPhase == 2 ? 0.18f : 0f),
            "komeiji" => ProgressData.Instance.GetSelectedSkin("komeiji") == 1
                ? 1.0f + (_koishiStealth >= 0.60f ? 0.35f : 0.12f * _koishiStealth)
                : 1.05f,
            "false_son" => 1.0f + (_falseSonMeridianWillTimer > 0f ? 0.25f : 0f),
            _ => 1.0f,
        };

        private float GetPack2MoveSpeedMult()
        {
            switch (CharKey)
            {
                case "daitaku_helios":
                    return 1.0f + 0.35f * HeliosHype + (_heliosUltTimer > 0f ? 0.45f : 0f);
                case "robin":
                    return 1.0f + (_robinConcertTimer > 0f ? 0.18f : 0f);
                case "bird_trio":
                    return ProgressData.Instance.GetSelectedSkin("bird_trio") == 2 && _smallBirdEnrageTimer > 0f ? 1.45f : 1.0f;
                case "apocalypse_bird":
                    return 1.08f + (_apocUltTimer > 0f ? 0.18f : 0f) + (_apocEggPhase == 2 ? 0.18f : 0f);
                case "komeiji":
                    return ProgressData.Instance.GetSelectedSkin("komeiji") == 1
                        ? 1.0f + 0.25f * _koishiStealth + (_koishiPhaseTimer > 0f ? 0.30f : 0f)
                        : 1.0f;
                case "false_son":
                {
                    int spent = Math.Max(0, GetFalseSonMaxSpikes() - FalseSonSpikes);
                    float mult = _falseSonMeridianWillTimer > 0f ? 3.0f : 1.0f;
                    return 1.0f + 0.06f * spent * mult;
                }
                default:
                    return 1.0f;
            }
        }

        private float GetPack2FireRateMult() => CharKey switch
        {
            "daitaku_helios" => 1.0f + 0.30f * HeliosHype + (_heliosUltTimer > 0f ? 0.25f : 0f),
            "robin" => 1.0f + (_robinConcertTimer > 0f ? 0.35f : 0f),
            "bird_trio" => ProgressData.Instance.GetSelectedSkin("bird_trio") == 2 && _smallBirdEnrageTimer > 0f ? 1.35f : 1.0f,
            "apocalypse_bird" => _apocUltTimer > 0f ? 1.25f : 1.05f,
            "komeiji" => ProgressData.Instance.GetSelectedSkin("komeiji") == 1 && _koishiUltTimer > 0f ? 1.25f : 1.0f,
            "false_son" => 1.0f + 0.03f * Math.Max(0, GetFalseSonMaxSpikes() - FalseSonSpikes) * (_falseSonMeridianWillTimer > 0f ? 3.0f : 1.0f),
            _ => 1.0f,
        };

        private float GetPack2CritBonus() => CharKey switch
        {
            "daitaku_helios" => 0.10f * HeliosHype,
            "robin" => (_robinHarmonyCritTimer > 0f ? 0.25f : 0f) + (_robinConcertTimer > 0f ? 0.15f : 0f),
            "bird_trio" => ProgressData.Instance.GetSelectedSkin("bird_trio") == 1 ? 0.12f : 0f,
            "apocalypse_bird" => 0.15f,
            "komeiji" => ProgressData.Instance.GetSelectedSkin("komeiji") != 1 ? 0.15f : 0.18f * _koishiStealth,
            "false_son" => 0.05f,
            _ => 0f,
        };

        private bool TryUsePack2M1() => false;

        private bool TryFirePack2Primary(Vector3 aimDir, Vector3 spawnPos, float damageMult, int m1Var)
        {
            if (CharKey is not ("daitaku_helios" or "robin" or "bird_trio" or "apocalypse_bird" or "komeiji" or "false_son"))
                return false;

            TriggerLoLSpellblade();
            Vector3 right = Vector3.Cross(Vector3.UnitY, aimDir);
            right = right.LengthSquared() > 0.001f ? Vector3.Normalize(right) : Vector3.UnitX;

            switch (CharKey)
            {
                case "daitaku_helios":
                {
                    HeliosHype = Math.Clamp(HeliosHype + 0.05f, 0f, 1f);
                    Color col = (_shootSide > 0) ? Rgb(0, 245, 255) : Rgb(255, 65, 210);
                    if (m1Var == 0)
                    {
                        PerformMeleeAttack(6.5f, 3.8f, 1.35f * damageMult, col, knockback: 5f, aoe: 2.2f);
                        var p = new Projectile(spawnPos, aimDir, 54f, BaseDamage * (1.20f + 0.35f * HeliosHype) * damageMult, 0.42f, col, aoe: 2.0f, pierce: true, maxLifetime: 0.95f);
                        Run.StageEntities.Add(p);
                        SoundManager.Play("shoot_heavy", 0.55f, 1.22f);
                    }
                    else
                    {
                        var disc = new Projectile(spawnPos, aimDir, 48f, BaseDamage * 1.35f * damageMult, 0.44f, col, aoe: 1.8f, bounces: 3, maxLifetime: 2.8f);
                        Run.StageEntities.Add(disc);
                        SoundManager.Play("coin_flip", 0.58f, 1.15f);
                    }
                    return true;
                }

                case "robin":
                {
                    _robinNoteCount++;
                    bool harmony = (_robinNoteCount % 3 == 0);
                    if (harmony)
                    {
                        Run.HealPlayer(6f + Run.PlayerMaxHp * 0.03f);
                        _robinHarmonyCritTimer = 2.8f;
                        new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(255, 230, 125), 2.3f, 0.24f);
                        SoundManager.Play("heal", 0.48f, 1.38f);
                    }
                    if (m1Var == 0)
                    {
                        Color col = harmony ? Rgb(255, 225, 110) : Rgb(245, 240, 255);
                        var f = new Projectile(spawnPos, aimDir, 58f, BaseDamage * (harmony ? 1.65f : 1.18f) * damageMult, harmony ? 0.44f : 0.34f, col, aoe: harmony ? 2.6f : 1.2f, pierce: harmony, healAllies: 8f);
                        Run.StageEntities.Add(f);
                    }
                    else
                    {
                        Color[] cols = { Rgb(255, 225, 115), Rgb(215, 185, 255), Rgb(155, 225, 255) };
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector3 dir = Vector3.Normalize(aimDir + right * (i * 0.09f));
                            var note = new Projectile(spawnPos, dir, 46f, BaseDamage * 0.56f * damageMult, 0.30f, cols[i + 1], aoe: 1.2f, homing: true, healAllies: 8f);
                            Run.StageEntities.Add(note);
                        }
                    }
                    SoundManager.Play("magic_cast", 0.48f, harmony ? 1.45f : 1.22f);
                    if (_robinConcertTimer > 0f)
                        RobinTriggerHarmonyStrike(spawnPos + aimDir * 12f, damageMult);
                    return true;
                }

                case "bird_trio":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("bird_trio");
                    if (skinIdx == 0)
                    {
                        if (m1Var == 0)
                        {
                            PerformMeleeAttack(7.2f, 4.5f, 1.45f * damageMult, Rgb(255, 210, 65), stunDur: 0.35f, aoe: 2.6f);
                            var orb = new Projectile(spawnPos, aimDir, 36f, BaseDamage * 1.15f * damageMult, 0.46f, Rgb(255, 205, 55), aoe: 2.8f, maxLifetime: 1.8f);
                            Run.StageEntities.Add(orb);
                            SoundManager.Play("flame", 0.55f, 0.95f);
                        }
                        else
                        {
                            CombatTarget? t = FindBestAimedEnemy(spawnPos, aimDir, 30f, 0.25f);
                            if (t != null)
                            {
                                t.ArmorBreakTimer = MathF.Max(t.ArmorBreakTimer, 4.0f);
                                t.SlowTimer = MathF.Max(t.SlowTimer, 2.5f);
                                new LightningBeam(spawnPos, t.Position + new Vector3(0, t.HitHeight * 0.6f, 0), Rgb(255, 220, 75), 0.18f);
                            }
                            var beamProj = new Projectile(spawnPos, aimDir, 64f, BaseDamage * 1.38f * damageMult, 0.35f, Rgb(255, 225, 80), aoe: 1.8f, pierce: true);
                            Run.StageEntities.Add(beamProj);
                            SoundManager.Play("laser", 0.52f, 1.18f);
                        }
                    }
                    else if (skinIdx == 1)
                    {
                        CombatTarget? t = FindBestAimedEnemy(spawnPos, aimDir, 32f, 0.22f);
                        if (t != null) t.SinStacks = Math.Min(10, t.SinStacks + (m1Var == 1 ? 2 : 1));
                        if (m1Var == 0)
                        {
                            var bolt = new Projectile(spawnPos, aimDir, 62f, BaseDamage * 1.35f * damageMult, 0.36f, Rgb(135, 245, 240), aoe: 1.8f, pierce: true);
                            Run.StageEntities.Add(bolt);
                            SoundManager.Play("zap", 0.50f, 1.15f);
                        }
                        else
                        {
                            var seal = new Projectile(spawnPos, aimDir, 44f, BaseDamage * 1.55f * damageMult, 0.44f, Rgb(235, 215, 125), aoe: 3.0f, homing: true);
                            Run.StageEntities.Add(seal);
                            SoundManager.Play("dante_clock", 0.48f, 1.35f);
                        }
                    }
                    else
                    {
                        bool enraged = _smallBirdEnrageTimer > 0f;
                        if (m1Var == 0)
                        {
                            if (enraged)
                            {
                                PerformMeleeAttack(8.8f, 5.4f, 2.25f * damageMult, Rgb(225, 20, 45), lifesteal: 0.18f, knockback: 6f, scissorCross: true, aoe: 3.5f);
                                new VisualFX(spawnPos + aimDir * 3.0f, Rgb(235, 25, 50), 2.8f, 0.22f);
                                SoundManager.Play("slam_land", 0.65f, 1.28f);
                            }
                            else
                            {
                                PerformMeleeAttack(5.8f, 3.2f, 1.15f * damageMult, Rgb(255, 110, 110), aoe: 1.8f);
                                var dart = new Projectile(spawnPos, aimDir, 56f, BaseDamage * 0.95f * damageMult, 0.28f, Rgb(255, 235, 235));
                                Run.StageEntities.Add(dart);
                                SoundManager.Play("shoot_light", 0.50f, 1.45f);
                            }
                        }
                        else
                        {
                            int cnt = enraged ? 5 : 3;
                            for (int i = 0; i < cnt; i++)
                            {
                                float off = (i - (cnt - 1) * 0.5f) * 0.08f;
                                Vector3 dir = Vector3.Normalize(aimDir + right * off);
                                var f = new Projectile(spawnPos, dir, 58f, BaseDamage * (enraged ? 0.62f : 0.50f) * damageMult, 0.30f, enraged ? Rgb(235, 25, 50) : Rgb(255, 195, 195), pierce: enraged, lifesteal: enraged ? 0.12f : 0f);
                                Run.StageEntities.Add(f);
                            }
                            SoundManager.Play("shoot_light", 0.55f, enraged ? 0.95f : 1.35f);
                        }
                    }
                    return true;
                }

                case "apocalypse_bird":
                {
                    _apocEggPhase = (_apocEggPhase + 1) % 3;
                    foreach (var e in CombatManager.Enemies)
                    {
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 12f)
                        {
                            e.SinStacks = Math.Min(10, e.SinStacks + 1);
                            e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 4.5f);
                            if (_apocEggPhase == 1)
                                e.TakeDamage(MathF.Min(BaseDamage * 2.2f, e.MaxHp * 0.015f));
                        }
                    }
                    if (m1Var == 0)
                    {
                        PerformMeleeAttack(9.0f, 5.6f, 1.75f * damageMult, Rgb(255, 195, 55), lifesteal: 0.08f, knockback: 6f, aoe: 3.4f);
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 dir = Vector3.Normalize(aimDir + right * (s * 0.08f));
                            var bolt = new Projectile(spawnPos + right * (s * 0.6f), dir, 52f, BaseDamage * 0.85f * damageMult, 0.36f, Rgb(255, 215, 70), aoe: 2.2f, homing: true);
                            Run.StageEntities.Add(bolt);
                        }
                        SoundManager.Play("shoot_heavy", 0.65f, 0.88f);
                    }
                    else
                    {
                        PerformMeleeAttack(8.5f, 5.2f, 1.45f * damageMult, Rgb(255, 160, 60), scissorCross: true, aoe: 3.0f);
                        Color[] tri = { Rgb(255, 215, 65), Rgb(135, 245, 240), Rgb(235, 35, 60) };
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 dir = Vector3.Normalize(aimDir + right * ((i - 1) * 0.09f));
                            var wave = new Projectile(spawnPos, dir, 56f, BaseDamage * 0.82f * damageMult, 0.42f, tri[i], aoe: 2.6f, pierce: true, lifesteal: 0.05f);
                            Run.StageEntities.Add(wave);
                        }
                        SoundManager.Play("laser", 0.62f, 0.85f);
                    }
                    return true;
                }

                case "komeiji":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("komeiji");
                    if (skinIdx != 1)
                    {
                        if (m1Var == 0)
                        {
                            for (int s = -1; s <= 1; s += 2)
                            {
                                Vector3 dir = Vector3.Normalize(aimDir + right * (s * 0.045f));
                                var w = new Projectile(spawnPos + right * (s * 0.3f), dir, 52f, BaseDamage * 0.78f * damageMult, 0.34f, Rgb(255, 75, 155), aoe: 1.8f, pierce: true);
                                Run.StageEntities.Add(w);
                            }
                        }
                        else
                        {
                            CombatTarget? aimed = FindBestAimedEnemy(spawnPos, aimDir, 32f, 0.3f);
                            if (aimed != null) aimed.ArmorBreakTimer = MathF.Max(aimed.ArmorBreakTimer, 3.5f);
                            for (int i = -2; i <= 2; i++)
                            {
                                Vector3 dir = Vector3.Normalize(aimDir + right * (i * 0.038f));
                                var nd = new Projectile(spawnPos, dir, 66f, BaseDamage * 0.36f * damageMult, 0.24f, Rgb(235, 95, 215));
                                Run.StageEntities.Add(nd);
                            }
                        }
                        SoundManager.Play("magic_cast", 0.48f, 1.28f);
                    }
                    else
                    {
                        float ambush = _koishiStealth >= 0.55f ? 1.40f : 1.0f;
                        _koishiStealth *= 0.35f;
                        if (m1Var == 0)
                        {
                            PerformMeleeAttack(6.8f, 4.0f, 1.50f * damageMult * ambush, Rgb(90, 255, 165), lifesteal: 0.08f, scissorCross: true, aoe: 2.4f);
                            for (int i = -1; i <= 1; i++)
                            {
                                Vector3 dir = Vector3.Normalize(aimDir + right * (i * 0.10f));
                                Color col = (i == 0) ? Rgb(255, 70, 140) : Rgb(80, 255, 165);
                                var pet = new Projectile(spawnPos, dir, 48f, BaseDamage * 0.55f * damageMult * ambush, 0.30f, col, aoe: 1.4f);
                                Run.StageEntities.Add(pet);
                            }
                            SoundManager.Play("shoot_light", 0.55f, 1.32f);
                        }
                        else
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                float off = (i - 1.5f) * 0.09f;
                                Vector3 dir = Vector3.Normalize(aimDir + right * off);
                                Color col = (i % 2 == 0) ? Rgb(80, 255, 165) : Rgb(255, 85, 155);
                                var hb = new Projectile(spawnPos, dir, 44f, BaseDamage * 0.48f * damageMult * ambush, 0.32f, col, aoe: 1.6f, bounces: 2, homing: true);
                                Run.StageEntities.Add(hb);
                            }
                            SoundManager.Play("magic_cast", 0.50f, 1.40f);
                        }
                    }
                    return true;
                }

                case "false_son":
                {
                    if (!_falseSonSpikesInit) { FalseSonSpikes = GetFalseSonMaxSpikes(); _falseSonSpikesInit = true; }
                    float bonusHp = MathF.Max(0f, Run.PlayerMaxHp - Cfg.Hp);
                    if (m1Var == 0)
                    {
                        _falseSonClubCombo++;
                        PerformMeleeAttack(8.2f, 4.8f, 1.85f * damageMult, Rgb(95, 220, 255), knockback: 8f, aoe: 3.0f);
                        SoundManager.Play("shoot_heavy", 0.65f, 0.78f);
                        if (!Run.PlayerGrounded || _falseSonClubCombo % 3 == 0)
                        {
                            if (!Run.PlayerGrounded) Run.PlayerVelY = -28f;
                            float slamRad = 8.5f + FalseSonSpikes * 0.45f;
                            Vector3 slamPos = Run.PlayerPos + new Vector3(aimDir.X, 0f, aimDir.Z) * 4.2f;
                            float slamDmg = (BaseDamage * 2.6f + bonusHp * 0.45f) * damageMult * (!Run.PlayerGrounded ? 1.25f : 1.0f);
                            new VisualFX(slamPos + new Vector3(0, 0.4f, 0), Rgb(105, 235, 255), 5.2f + FalseSonSpikes * 0.2f, 0.32f);
                            new LightningBeam(slamPos + new Vector3(0, 9f, 0), slamPos, Rgb(255, 215, 75), 0.25f);
                            SoundManager.Play("slam_land", 0.80f, 0.88f);
                            foreach (var e in CombatManager.Enemies.ToArray())
                            {
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(slamPos, e.Position) <= slamRad)
                                {
                                    e.StunTimer = MathF.Max(e.StunTimer, 0.9f);
                                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, slamDmg, Rgb(105, 235, 255), aoe: 1.5f);
                                }
                            }
                        }
                    }
                    else
                    {
                        PerformMeleeAttack(8.5f, 5.2f, 1.65f * damageMult, Rgb(255, 210, 80), knockback: 7f, aoe: 3.2f);
                        float waveDmg = (BaseDamage * 2.0f + bonusHp * 0.30f) * damageMult;
                        var wave = new Projectile(spawnPos, aimDir, 46f, waveDmg, 0.56f, Rgb(100, 235, 255), aoe: 3.4f, pierce: true, maxLifetime: 1.4f);
                        Run.StageEntities.Add(wave);
                        SoundManager.Play("slam_land", 0.68f, 0.95f);
                    }
                    return true;
                }
            }
            return false;
        }

        private bool TryUsePack2M2(Vector3 aimDir, Vector3 spawnPos)
        {
            if (CharKey is not ("daitaku_helios" or "robin" or "bird_trio" or "apocalypse_bird" or "komeiji" or "false_son"))
                return false;

            float cdr = MathF.Pow(0.88f, CombatManager.GetItemCount("backup_mag"))
                      * MathF.Pow(0.80f, CombatManager.GetItemCount("alien_head"));
            M2Cd = MathF.Max(0.25f, Cfg.Skills["m2"].Cd * cdr);
            TriggerLoLSpellblade();
            Run.Parts.TriggerSkillAnim("m2", 0.42f);

            int m2Var = SkillVar("m2");
            Vector3 right = Vector3.Cross(Vector3.UnitY, aimDir);
            right = right.LengthSquared() > 0.001f ? Vector3.Normalize(right) : Vector3.UnitX;

            switch (CharKey)
            {
                case "daitaku_helios":
                {
                    float beatMult = ConsumeHeliosBeatBonus("БАС-ДРОП");
                    HeliosHype = Math.Clamp(HeliosHype + 0.32f, 0f, 1f);
                    if (m2Var == 0)
                    {
                        if (beatMult <= 1.01f) Run.ShowBanner("«WEI~★!»", Rgb(0, 245, 255), 1.2f);
                        SoundManager.Play("bounce_pad", 0.80f, 1.15f);
                        new VisualFX(spawnPos + aimDir * 4.5f, Rgb(255, 65, 210), 5.2f, 0.30f);
                        float coneDmg = BaseDamage * (3.4f + 1.2f * HeliosHype) * beatMult;
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (!IsValid(e) || e.IsAlly) continue;
                            Vector3 toE = e.Position - Run.PlayerPos;
                            float d = toE.Length();
                            if (d <= 14.0f && (d < 3.0f || Vector3.Dot(Vector3.Normalize(toE), aimDir) >= 0.35f))
                            {
                                e.StunTimer = MathF.Max(e.StunTimer, 1.1f);
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, coneDmg, Rgb(0, 245, 255), aoe: 2.0f);
                            }
                        }
                        for (int i = -1; i <= 1; i++)
                        {
                            var p = new Projectile(spawnPos, Vector3.Normalize(aimDir + right * (i * 0.14f)), 46f, BaseDamage * 1.4f * beatMult, 0.45f, Rgb(0, 245, 255), aoe: 2.2f, pierce: true, maxLifetime: 1.0f);
                            Run.StageEntities.Add(p);
                        }
                    }
                    else
                    {
                        SoundManager.Play("zap", 0.75f, 1.35f);
                        new VisualFX(Run.PlayerPos + new Vector3(0, 1.2f, 0), Rgb(255, 230, 95), 6.0f, 0.32f);
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 12.5f)
                            {
                                e.StunTimer = MathF.Max(e.StunTimer, 2.0f);
                                e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 6.0f);
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 2.8f * beatMult, Rgb(255, 85, 220));
                            }
                        }
                    }
                    return true;
                }

                case "robin":
                {
                    if (m2Var == 0)
                    {
                        _robinAriaTimer = 6.0f;
                        Run.HealPlayer(10f);
                        SoundManager.Play("magic_cast", 0.72f, 1.30f);
                        new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(255, 220, 110), 3.6f, 0.30f);
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector3 dir = Vector3.Normalize(aimDir + right * (i * 0.12f));
                            var w = new Projectile(spawnPos, dir, 50f, BaseDamage * 1.95f, 0.48f, Rgb(255, 220, 105), aoe: 3.0f, pierce: true, healAllies: 16f);
                            Run.StageEntities.Add(w);
                        }
                    }
                    else
                    {
                        Run.HealPlayer(15f + Run.PlayerMaxHp * 0.16f);
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 38f);
                        SoundManager.Play("heal", 0.78f, 1.18f);
                        new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(185, 235, 255), 5.0f, 0.32f);
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 13.5f)
                            {
                                e.SlowTimer = MathF.Max(e.SlowTimer, 3.0f);
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 2.2f, Rgb(220, 200, 255));
                            }
                        }
                    }
                    if (_robinConcertTimer > 0f)
                        RobinTriggerHarmonyStrike(spawnPos + aimDir * 14f, 1.75f);
                    return true;
                }

                case "bird_trio":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("bird_trio");
                    if (skinIdx == 0)
                    {
                        if (m2Var == 0)
                        {
                            SoundManager.Play("charm_heart", 0.80f, 0.92f);
                            new VisualFX(Run.PlayerPos + new Vector3(0, 1.6f, 0), Rgb(255, 215, 65), 6.5f, 0.36f);
                            foreach (var e in CombatManager.Enemies.ToArray())
                            {
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 16.0f)
                                {
                                    e.StunTimer = MathF.Max(e.StunTimer, 2.2f);
                                    if (!e.IsBoss) e.CharmTimer = MathF.Max(e.CharmTimer, 5.5f);
                                    else e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 5.5f);
                                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 3.1f, Rgb(255, 215, 65));
                                }
                            }
                        }
                        else
                        {
                            SoundManager.Play("flame", 0.78f, 0.85f);
                            var fb = new Projectile(spawnPos, aimDir, 38f, BaseDamage * 4.2f, 0.65f, Rgb(255, 165, 35), aoe: 5.5f);
                            Run.StageEntities.Add(fb);
                            foreach (var e in CombatManager.Enemies)
                            {
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 10f)
                                {
                                    e.BleedTimer = MathF.Max(e.BleedTimer, 4.5f);
                                    e.BleedDps = MathF.Max(e.BleedDps, BaseDamage * 0.75f);
                                }
                            }
                        }
                    }
                    else if (skinIdx == 1)
                    {
                        SoundManager.Play("dante_clock", 0.78f, 1.05f);
                        if (m2Var == 0)
                        {
                            Vector3 center = (FindBestAimedEnemy(spawnPos, aimDir, 28f, 0.2f)?.Position) ?? (Run.PlayerPos + aimDir * 12f);
                            new VisualFX(center + Vector3.UnitY, Rgb(135, 245, 240), 5.5f, 0.34f);
                            foreach (var e in CombatManager.Enemies.ToArray())
                            {
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(center, e.Position) <= 12.0f)
                                {
                                    e.SinStacks = Math.Min(10, e.SinStacks + 3);
                                    e.SlowTimer = MathF.Max(e.SlowTimer, 2.5f);
                                    Vector3 hp = e.Position + new Vector3(0, e.HitHeight * 0.6f, 0);
                                    new LightningBeam(hp + new Vector3(0, 12f, 0), hp, Rgb(140, 250, 240), 0.26f);
                                    CombatManager.ApplyPlayerHitEffects(e, hp, BaseDamage * (2.6f + 0.35f * e.SinStacks), Rgb(140, 250, 240));
                                }
                            }
                        }
                        else
                        {
                            Vector3 end = spawnPos + aimDir * 34f;
                            new LightningBeam(spawnPos, end, Rgb(245, 225, 120), 0.30f);
                            Vector3 seg = end - spawnPos;
                            float lenSq = MathF.Max(0.01f, seg.LengthSquared());
                            foreach (var e in CombatManager.Enemies.ToArray())
                            {
                                if (!IsValid(e) || e.IsAlly) continue;
                                Vector3 c = e.Position + Vector3.UnitY;
                                float t = Math.Clamp(Vector3.Dot(c - spawnPos, seg) / lenSq, 0f, 1f);
                                if (Vector3.Distance(c, spawnPos + seg * t) <= e.HitRadius + 2.0f)
                                {
                                    e.SinStacks = Math.Min(10, e.SinStacks + 4);
                                    e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 5.0f);
                                    CombatManager.ApplyPlayerHitEffects(e, c, BaseDamage * 3.8f, Rgb(245, 225, 120));
                                }
                            }
                        }
                    }
                    else
                    {
                        bool enr = _smallBirdEnrageTimer > 0f;
                        if (IsValid(PunishingBirdGrudgeTarget) && PunishingBirdGrudgeTarget!.PunishingGrudgeTimer > 0f &&
                            Vector3.Distance(Run.PlayerPos, PunishingBirdGrudgeTarget.Position) <= 28f)
                        {
                            var offender = PunishingBirdGrudgeTarget;
                            offender.PunishingGrudgeTimer = 0f;
                            PunishingBirdGrudgeTarget = null;
                            Vector3 toOff = offender.Position - Run.PlayerPos;
                            toOff.Y = 0f;
                            Vector3 fDir = toOff.LengthSquared() > 0.001f ? Vector3.Normalize(toOff) : Run.PlayerForward;
                            Vector3 jumpDst = Physics.ResolveHorizontalCollision(offender.Position - fDir * (offender.HitRadius + 1.4f), 0.65f, offender.Position.Y);
                            jumpDst.Y = Physics.GetSurfaceHeight(jumpDst.X, jumpDst.Z, offender.Position.Y);
                            new LightningBeam(Run.PlayerPos + Vector3.UnitY, jumpDst + Vector3.UnitY, Rgb(255, 45, 65), 0.28f);
                            Run.PlayerPos = jumpDst;
                            CombatManager.ApplyPlayerHitEffects(offender, offender.Position + Vector3.UnitY, BaseDamage * 5.8f, Rgb(255, 35, 55), aoe: 3.5f, lifesteal: 14f, canProc: true);
                            Run.ShowBanner("КАРАЮЩАЯ ПТИЦА :: НАКАЗАНИЕ ОБИДЧИКА (x2.2)!", Rgb(255, 55, 75), 1.4f);
                        }
                        if (m2Var == 0)
                        {
                            Run.ApplyDash(aimDir * 28f, 0.20f);
                            PerformMeleeAttack(7.5f, 4.5f, enr ? 4.4f : 2.8f, Rgb(235, 30, 55), lifesteal: 0.20f, aoe: 3.2f);
                            var dive = new Projectile(spawnPos, aimDir, 54f, BaseDamage * (enr ? 4.2f : 2.9f), 0.48f, Rgb(235, 25, 50), aoe: 3.6f, homing: true, lifesteal: 0.20f);
                            Run.StageEntities.Add(dive);
                            SoundManager.Play("rocket", 0.65f, 1.35f);
                        }
                        else
                        {
                            SoundManager.Play("slam_land", 0.72f, 1.20f);
                            new VisualFX(spawnPos + aimDir * 4f, Rgb(235, 25, 50), 4.6f, 0.28f);
                            foreach (var e in CombatManager.Enemies.ToArray())
                            {
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 15f)
                                {
                                    e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 6.0f);
                                    e.BleedTimer = MathF.Max(e.BleedTimer, 5.0f);
                                    e.BleedDps = MathF.Max(e.BleedDps, BaseDamage * 0.90f);
                                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 2.9f, Rgb(235, 25, 50));
                                }
                            }
                        }
                    }
                    return true;
                }

                case "apocalypse_bird":
                {
                    if (m2Var == 0)
                    {
                        SoundManager.Play("laser", 0.80f, 1.05f);
                        for (int i = 0; i < 9; i++)
                        {
                            float off = (i - 4) * 0.11f;
                            Vector3 origin = spawnPos + right * ((i - 4) * 0.45f) + new Vector3(0, 0.6f + (i % 3) * 0.35f, 0);
                            Vector3 dir = Vector3.Normalize(aimDir + right * off + new Vector3(0, 0.04f, 0));
                            var eyeProj = new Projectile(origin, dir, 52f, BaseDamage * 0.90f, 0.35f, Rgb(255, 215, 65), aoe: 2.2f, homing: true);
                            Run.StageEntities.Add(eyeProj);
                        }
                    }
                    else
                    {
                        Run.ApplyDash(new Vector3(aimDir.X, 0f, aimDir.Z) * 20f, 0.18f);
                        PerformMeleeAttack(10.0f, 6.4f, 5.2f, Rgb(230, 35, 55), lifesteal: 0.25f, stunDur: 1.2f, knockback: 8f, scissorCross: true, aoe: 4.5f);
                        new VisualFX(spawnPos + aimDir * 4.2f, Rgb(255, 175, 50), 5.0f, 0.32f);
                        SoundManager.Play("slam_land", 0.85f, 0.82f);
                    }
                    return true;
                }

                case "komeiji":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("komeiji");
                    if (skinIdx != 1)
                    {
                        if (m2Var == 0)
                        {
                            SoundManager.Play("laser", 0.68f, 1.25f);
                            for (int i = -3; i <= 3; i++)
                            {
                                Vector3 dir = Vector3.Normalize(aimDir + right * (i * 0.09f));
                                var b = new Projectile(spawnPos, dir, 46f, BaseDamage * 0.85f, 0.34f, Rgb(255, 65, 150), aoe: 1.6f, pierce: true);
                                Run.StageEntities.Add(b);
                            }
                            CombatTarget? mindT = FindBestAimedEnemy(spawnPos, aimDir, 32f, 0.25f);
                            if (mindT != null)
                            {
                                CaptureSatoriEnemyTrauma(mindT);
                                Vector3 hp = mindT.Position + new Vector3(0, mindT.HitHeight * 0.6f, 0);
                                new LightningBeam(spawnPos, hp, Rgb(255, 55, 145), 0.26f);
                                mindT.ArmorBreakTimer = MathF.Max(mindT.ArmorBreakTimer, 5.0f);
                                mindT.SlowTimer = MathF.Max(mindT.SlowTimer, 3.0f);
                                CombatManager.ApplyPlayerHitEffects(mindT, hp, BaseDamage * 3.0f, Rgb(255, 65, 150), aoe: 2.5f);
                            }
                        }
                        else
                        {
                            SoundManager.Play("flame", 0.75f, 1.12f);
                            var wheel = new Projectile(spawnPos, aimDir, 34f, BaseDamage * 3.9f, 0.68f, Rgb(65, 255, 210), aoe: 5.0f, pierce: true, maxLifetime: 2.4f);
                            Run.StageEntities.Add(wheel);
                        }
                    }
                    else
                    {
                        if (m2Var == 0)
                        {
                            SoundManager.Play("magic_cast", 0.72f, 1.32f);
                            for (int i = 0; i < 10; i++)
                            {
                                float ang = i * (MathF.PI * 2f / 10f);
                                Vector3 dir = new(MathF.Cos(ang), 0.04f, MathF.Sin(ang));
                                var h = new Projectile(Run.PlayerPos + Vector3.UnitY + dir * 0.8f, dir, 38f, BaseDamage * 0.95f, 0.36f, Rgb(255, 65, 130), aoe: 1.8f, homing: true);
                                Run.StageEntities.Add(h);
                            }
                        }
                        else
                        {
                            Shield = MathF.Min(Run.PlayerMaxHp, Shield + 42f);
                            _koishiStealth = Math.Clamp(_koishiStealth + 0.45f, 0f, 1f);
                            SoundManager.Play("parry", 0.68f, 1.30f);
                            new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(75, 255, 175), 4.2f, 0.28f);
                            for (int i = -2; i <= 2; i++)
                            {
                                Vector3 dir = Vector3.Normalize(aimDir + right * (i * 0.07f));
                                var p = new Projectile(spawnPos + right * (i * 0.6f), dir, 50f, BaseDamage * 0.85f, 0.34f, Rgb(75, 255, 175), aoe: 1.8f, pierce: true);
                                Run.StageEntities.Add(p);
                            }
                        }
                    }
                    return true;
                }

                case "false_son":
                {
                    if (!_falseSonSpikesInit) { FalseSonSpikes = GetFalseSonMaxSpikes(); _falseSonSpikesInit = true; }
                    float bonusHp = MathF.Max(0f, Run.PlayerMaxHp - Cfg.Hp);
                    bool consumed = FalseSonSpikes > 0;
                    if (consumed) FalseSonSpikes--;

                    if (m2Var == 0)
                    {
                        if (consumed) M2Cd = 0.28f;
                        SoundManager.Play("railgun", 0.62f, consumed ? 1.32f : 1.05f);
                        float dmg = BaseDamage * (consumed ? 2.85f : 1.90f) + bonusHp * 0.22f;
                        CombatTarget? t = FindBestAimedEnemy(spawnPos, aimDir, 38f, 0.25f);
                        if (t != null)
                        {
                            t.LunarRuinStacks = Math.Min(12, t.LunarRuinStacks + (consumed ? 2 : 1));
                            t.LunarRuinTimer = 6.0f;
                        }
                        var spk = new Projectile(spawnPos, aimDir, 74f, dmg, 0.40f, Rgb(95, 235, 255), aoe: 2.5f, pierce: consumed);
                        Run.StageEntities.Add(spk);
                    }
                    else
                    {
                        CombatTarget? t = FindBestAimedEnemy(spawnPos, aimDir, 34f, 0.2f);
                        Vector3 strikePos = t != null ? t.Position : (Run.PlayerPos + new Vector3(aimDir.X, 0f, aimDir.Z) * 14f);
                        strikePos.Y = Physics.GetSurfaceHeight(strikePos.X, strikePos.Z, Run.PlayerPos.Y);
                        new LightningBeam(strikePos + new Vector3(0, 18f, 0), strikePos, Rgb(105, 240, 255), 0.34f);
                        new VisualFX(strikePos + new Vector3(0, 0.5f, 0), Rgb(105, 240, 255), 6.2f, 0.34f);
                        SoundManager.Play("zap", 0.82f, 0.88f);
                        float stakeDmg = (BaseDamage * 3.8f + bonusHp * 0.35f) * (consumed ? 1.35f : 1.0f);
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(strikePos, e.Position) <= 7.2f)
                            {
                                e.StunTimer = MathF.Max(e.StunTimer, 1.2f);
                                e.LunarRuinStacks = Math.Min(12, e.LunarRuinStacks + 2);
                                e.LunarRuinTimer = 6.0f;
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, stakeDmg, Rgb(105, 240, 255));
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private bool TryUsePack2Shift(Vector3 myPos, int shVar, float cdrMult, float nrgMult)
        {
            if (CharKey is not ("daitaku_helios" or "robin" or "bird_trio" or "apocalypse_bird" or "komeiji" or "false_son"))
                return false;

            Vector3 aimDir = GetAimDirection();
            Vector3 flatAim = new(aimDir.X, 0f, aimDir.Z);
            flatAim = flatAim.LengthSquared() > 0.001f ? Vector3.Normalize(flatAim) : Run.PlayerForward;

            switch (CharKey)
            {
                case "daitaku_helios":
                {
                    float beatMult = ConsumeHeliosBeatBonus("НЕОН-СПРИНТ");
                    HeliosHype = Math.Clamp(HeliosHype + 0.35f, 0f, 1f);
                    SoundManager.Play("bounce_pad", 0.75f, 1.25f);
                    if (shVar == 0)
                    {
                        Run.StageEntities.Add(new HeliosRaveGateEntity(myPos));
                        Run.ApplyDash(flatAim * 36f * nrgMult * (beatMult > 1.01f ? 1.15f : 1.0f), 0.34f);
                    }
                    else
                    {
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 25f);
                        M2Cd = MathF.Max(0f, M2Cd - 1.5f);
                        Vector3 driftDir = Vector3.Normalize(flatAim * 0.8f + Run.PlayerEntity.Right * (_shootSide * 0.45f));
                        Run.ApplyDash(driftDir * 32f * nrgMult, 0.28f);
                        for (int i = -1; i <= 1; i++)
                        {
                            var d = new Projectile(myPos + Vector3.UnitY, Vector3.Normalize(aimDir + Run.PlayerEntity.Right * (i * 0.14f)), 48f, BaseDamage * 1.25f * beatMult, 0.40f, Rgb(255, 70, 210), bounces: 2);
                            Run.StageEntities.Add(d);
                        }
                    }
                    return true;
                }

                case "robin":
                {
                    if (shVar == 0)
                    {
                        Run.PlayerVelY = 14.5f;
                        Run.PlayerGrounded = false;
                        Run.ApplyDash(flatAim * 24f * nrgMult, 0.30f);
                        SoundManager.Play("magic_cast", 0.70f, 1.35f);
                        for (int i = -2; i <= 2; i++)
                        {
                            Vector3 dir = Vector3.Normalize(aimDir + Run.PlayerEntity.Right * (i * 0.12f) + new Vector3(0, -0.15f, 0));
                            var f = new Projectile(myPos + new Vector3(0, 1.3f, 0), dir, 48f, BaseDamage * 1.15f, 0.34f, Rgb(255, 230, 130), aoe: 1.6f, homing: true, healAllies: 10f);
                            Run.StageEntities.Add(f);
                        }
                    }
                    else
                    {
                        Vector3 blinkDir = aimDir;
                        blinkDir.Y = MathF.Max(0f, blinkDir.Y);
                        Vector3 dst = Physics.ResolveHorizontalCollision(myPos + Vector3.Normalize(blinkDir) * (14.5f * nrgMult), 0.72f, myPos.Y);
                        new LightningBeam(myPos + Vector3.UnitY, dst + Vector3.UnitY, Rgb(255, 225, 115), 0.28f);
                        Run.PlayerPos = dst;
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 30f);
                        Run.HealPlayer(12f);
                        SoundManager.Play("heal", 0.68f, 1.30f);
                    }
                    return true;
                }

                case "bird_trio":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("bird_trio");
                    if (skinIdx == 0)
                    {
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + (shVar == 0 ? 45f : 35f));
                        if (shVar == 0)
                        {
                            Run.ApplyDash(flatAim * 28f * nrgMult, 0.30f);
                            PerformMeleeAttack(6.8f, 4.8f, 2.2f, Rgb(255, 210, 65), stunDur: 1.4f, knockback: 8f, aoe: 3.4f);
                            SoundManager.Play("slam_land", 0.75f, 0.90f);
                        }
                        else
                        {
                            _bigBirdDarkStepTimer = 1.5f;
                            Run.ApplyDash(flatAim * 22f * nrgMult, 0.25f);
                            new VisualFX(myPos + Vector3.UnitY, Rgb(55, 45, 70), 3.6f, 0.30f);
                            SoundManager.Play("magic_cast", 0.65f, 0.80f);
                        }
                    }
                    else if (skinIdx == 1)
                    {
                        if (shVar == 0)
                        {
                            Vector3 endPreview = Physics.ResolveHorizontalCollision(myPos + flatAim * (26f * nrgMult), 0.72f, myPos.Y);
                            new LightningBeam(myPos + Vector3.UnitY, endPreview + Vector3.UnitY, Rgb(135, 245, 240), 0.30f);
                            Run.ApplyDash(flatAim * 30f * nrgMult, 0.28f);
                            foreach (var e in CombatManager.Enemies)
                            {
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(myPos, e.Position) <= 14f)
                                {
                                    e.SinStacks = Math.Min(10, e.SinStacks + 2);
                                    e.SlowTimer = MathF.Max(e.SlowTimer, 2.5f);
                                }
                            }
                            SoundManager.Play("dante_clock", 0.70f, 1.18f);
                        }
                        else
                        {
                            Shield = MathF.Min(Run.PlayerMaxHp, Shield + 55f);
                            Run.HealPlayer(Run.PlayerMaxHp * 0.10f);
                            new VisualFX(myPos + Vector3.UnitY, Rgb(240, 220, 120), 5.0f, 0.32f);
                            foreach (var e in CombatManager.Enemies.ToArray())
                            {
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(myPos, e.Position) <= 11f)
                                {
                                    e.SinStacks = Math.Min(10, e.SinStacks + 2);
                                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 1.8f, Rgb(140, 245, 240));
                                }
                            }
                            SoundManager.Play("heal", 0.72f, 1.05f);
                        }
                    }
                    else
                    {
                        if (shVar == 0)
                        {
                            _smallBirdGuardTimer = 1.0f;
                            Run.ApplyDash(flatAim * 18f * nrgMult, 0.20f);
                            new VisualFX(myPos + Vector3.UnitY, Rgb(255, 210, 210), 2.8f, 0.26f);
                            Run.ShowBanner("КРЫЛО НАСТОРОЖЕ!", Rgb(255, 95, 105), 1.1f);
                            SoundManager.Play("parry", 0.70f, 1.35f);
                        }
                        else
                        {
                            Run.ApplyDash(flatAim * 36f * nrgMult, 0.24f);
                            PerformMeleeAttack(8.0f, 4.4f, 2.5f, Rgb(235, 30, 55), lifesteal: 6.0f, scissorCross: true, aoe: 3.2f);
                            SoundManager.Play("rocket", 0.68f, 1.40f);
                        }
                    }
                    return true;
                }

                case "apocalypse_bird":
                {
                    if (shVar == 0)
                    {
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 55f);
                        Vector3 dst = Physics.ResolveHorizontalCollision(myPos + flatAim * (17f * nrgMult), 0.72f, myPos.Y);
                        dst.Y = Physics.GetSurfaceHeight(dst.X, dst.Z, myPos.Y);
                        new LightningBeam(myPos + Vector3.UnitY, dst + Vector3.UnitY, Rgb(255, 195, 55), 0.34f);
                        Run.PlayerPos = dst;
                        new VisualFX(dst + Vector3.UnitY, Rgb(255, 190, 50), 6.5f, 0.35f);
                        SoundManager.Play("slam_land", 0.85f, 0.80f);
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(dst, e.Position) <= 9.5f)
                            {
                                e.SinStacks = Math.Min(10, e.SinStacks + 2);
                                e.StunTimer = MathF.Max(e.StunTimer, 1.3f);
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 3.4f, Rgb(255, 200, 55));
                            }
                        }
                    }
                    else
                    {
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 40f);
                        Run.PlayerVelY = 13.5f;
                        Run.PlayerGrounded = false;
                        Run.ApplyDash(flatAim * 34f * nrgMult, 0.32f);
                        for (int i = 0; i < 4; i++)
                        {
                            float ang = i * MathF.PI * 0.5f;
                            var b = new Projectile(myPos + new Vector3(0, 1.6f, 0), new Vector3(MathF.Cos(ang), 0.1f, MathF.Sin(ang)), 50f, BaseDamage * 1.4f, 0.36f, Rgb(255, 215, 65), homing: true);
                            Run.StageEntities.Add(b);
                        }
                        SoundManager.Play("ult_cast", 0.68f, 1.15f);
                    }
                    return true;
                }

                case "komeiji":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("komeiji");
                    if (skinIdx != 1)
                    {
                        if (shVar == 0)
                        {
                            Shield = MathF.Min(Run.PlayerMaxHp, Shield + 30f);
                            new VisualFX(myPos + Vector3.UnitY, Rgb(255, 70, 155), 4.5f, 0.28f);
                            foreach (var e in CombatManager.Enemies.ToArray())
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(myPos, e.Position) <= 6.5f)
                                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 2.2f, Rgb(255, 70, 155));
                            Vector3 dst = Physics.ResolveHorizontalCollision(myPos + flatAim * (13.5f * nrgMult), 0.72f, myPos.Y);
                            new LightningBeam(myPos + Vector3.UnitY, dst + Vector3.UnitY, Rgb(255, 85, 165), 0.25f);
                            Run.PlayerPos = dst;
                            SoundManager.Play("magic_cast", 0.68f, 1.35f);
                        }
                        else
                        {
                            Run.HealPlayer(15f);
                            Run.ApplyDash(-flatAim * 24f * nrgMult, 0.24f);
                            new VisualFX(myPos + Vector3.UnitY, Rgb(75, 255, 165), 5.2f, 0.30f);
                            foreach (var e in CombatManager.Enemies.ToArray())
                            {
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(myPos, e.Position) <= 7.5f)
                                {
                                    e.StunTimer = MathF.Max(e.StunTimer, 1.2f);
                                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 3.0f, Rgb(75, 255, 165));
                                }
                            }
                            SoundManager.Play("charm_heart", 0.72f, 1.25f);
                        }
                    }
                    else
                    {
                        _koishiStealth = 1.0f;
                        PlantKoishiRosebud(myPos);
                        if (shVar == 0)
                        {
                            _koishiPhaseTimer = 1.6f;
                            Run.ApplyDash(flatAim * 30f * nrgMult, 0.30f);
                            new VisualFX(myPos + Vector3.UnitY, Rgb(80, 255, 170), 3.2f, 0.26f);
                            SoundManager.Play("magic_cast", 0.65f, 1.42f);
                        }
                        else
                        {
                            CombatTarget? t = FindBestAimedEnemy(myPos + Vector3.UnitY, aimDir, 28f, 0.15f) ?? CombatManager.FindNearestHostileAt(myPos);
                            if (t != null && Vector3.Distance(myPos, t.Position) <= 28f)
                            {
                                Vector3 dirToT = t.Position - myPos;
                                dirToT.Y = 0f;
                                dirToT = dirToT.LengthSquared() > 0.001f ? Vector3.Normalize(dirToT) : flatAim;
                                Vector3 behind = Physics.ResolveHorizontalCollision(t.Position + dirToT * (t.HitRadius + 1.6f), 0.72f, t.Position.Y);
                                behind.Y = Physics.GetSurfaceHeight(behind.X, behind.Z, t.Position.Y);
                                new LightningBeam(myPos + Vector3.UnitY, behind + Vector3.UnitY, Rgb(80, 255, 165), 0.26f);
                                Run.PlayerPos = behind;
                                t.StunTimer = MathF.Max(t.StunTimer, 1.5f);
                                CombatManager.ApplyPlayerHitEffects(t, t.Position + Vector3.UnitY, BaseDamage * 4.6f, Rgb(255, 65, 135), aoe: 3.5f, lifesteal: 8.0f);
                                Run.ShowBanner("«А Я У ТЕБЯ ЗА СПИНОЙ!»", Rgb(85, 255, 170), 1.4f);
                                SoundManager.Play("dante_clock", 0.78f, 1.35f);
                            }
                            else
                            {
                                Run.PlayerPos = Physics.ResolveHorizontalCollision(myPos + flatAim * (14f * nrgMult), 0.72f, myPos.Y);
                                SoundManager.Play("magic_cast", 0.65f, 1.35f);
                            }
                        }
                    }
                    return true;
                }

                case "false_son":
                {
                    if (!_falseSonSpikesInit) { FalseSonSpikes = GetFalseSonMaxSpikes(); _falseSonSpikesInit = true; }
                    if (shVar == 0)
                    {
                        FalseSonSpikes = Math.Min(GetFalseSonMaxSpikes(), FalseSonSpikes + 2);
                        Run.ApplyDash(flatAim * 32f * nrgMult, 0.30f);
                        PerformMeleeAttack(7.5f, 4.8f, 2.4f, Rgb(95, 225, 255), stunDur: 1.0f, knockback: 7f, aoe: 3.5f);
                        SoundManager.Play("slam_land", 0.78f, 0.95f);
                    }
                    else
                    {
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 55f);
                        FalseSonSpikes = Math.Min(GetFalseSonMaxSpikes(), FalseSonSpikes + 1);
                        Run.ApplyDash(flatAim * 26f * nrgMult, 0.28f);
                        new VisualFX(myPos + Vector3.UnitY, Rgb(255, 215, 75), 4.6f, 0.30f);
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(myPos, e.Position) <= 8.5f)
                            {
                                e.LunarRuinStacks = Math.Min(12, e.LunarRuinStacks + 1);
                                e.LunarRuinTimer = 6.0f;
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 1.9f, Rgb(255, 215, 75));
                            }
                        }
                        SoundManager.Play("zap", 0.72f, 1.05f);
                    }
                    return true;
                }
            }
            return false;
        }

        private bool TryUsePack2R(int rVar)
        {
            if (CharKey is not ("daitaku_helios" or "robin" or "bird_trio" or "apocalypse_bird" or "komeiji" or "false_son"))
                return false;

            float rCdr = MathF.Pow(0.80f, CombatManager.GetItemCount("alien_head"))
                       * MathF.Pow(0.75f, CombatManager.GetItemCount("lysate_cell"));
            RCd = MathF.Max(0.5f, Cfg.Skills["r"].Cd * rCdr);
            TriggerLoLSpellblade();
            Run.Parts.TriggerSkillAnim("r", 0.65f);
            Vector3 myPos = Run.PlayerPos;
            Vector3 aimDir = GetAimDirection();

            switch (CharKey)
            {
                case "daitaku_helios":
                {
                    _heliosUltTimer = 8.0f;
                    _heliosBassTick = 0.1f;
                    HeliosHype = 1.0f;
                    Run.Parts.SetUltForm(true);
                    Shield = MathF.Min(Run.PlayerMaxHp, Shield + 50f);
                    Run.HealPlayer(Run.PlayerMaxHp * 0.20f);
                    Run.ShowBanner("★ ПАРИПИ-ФЕСТ! WEI~★! ★", Rgb(0, 245, 255), 2.2f);
                    SoundManager.Play("ult_cast", 0.90f, 1.20f);
                    if (rVar == 1)
                    {
                        Run.StageEntities.Add(new HeliosRaveGateEntity(myPos));
                        for (int i = 0; i < 8; i++)
                        {
                            float a = i * MathF.PI * 0.25f;
                            var d = new Projectile(myPos + Vector3.UnitY, new(MathF.Cos(a), 0.05f, MathF.Sin(a)), 48f, BaseDamage * 2.0f, 0.44f, (i % 2 == 0) ? Rgb(0, 245, 255) : Rgb(255, 65, 210), bounces: 3);
                            Run.StageEntities.Add(d);
                        }
                    }
                    return true;
                }

                case "robin":
                {
                    _robinConcertTimer = 8.5f;
                    _robinConcertWaveTick = 0.2f;
                    Run.Parts.SetUltForm(true);
                    Run.HealPlayer(Run.PlayerMaxHp * 0.25f);
                    Shield = MathF.Min(Run.PlayerMaxHp, Shield + 45f);
                    Run.ShowBanner("♫ КОНЦЕРТ: КАЧАЙСЯ В РИТМЕ КОСМОСА! ♫", Rgb(255, 225, 115), 2.4f);
                    SoundManager.Play("ult_cast", 0.90f, 1.28f);
                    if (rVar == 1)
                    {
                        int hit = 0;
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (!IsValid(e) || e.IsAlly || Vector3.Distance(myPos, e.Position) > 26f) continue;
                            Vector3 hp = e.Position + Vector3.UnitY;
                            new LightningBeam(hp + new Vector3(0, 15f, 0), hp, Rgb(255, 225, 110), 0.34f);
                            CombatManager.ApplyPlayerHitEffects(e, hp, BaseDamage * 3.2f, Rgb(255, 225, 110), aoe: 2.5f);
                            if (++hit >= 6) break;
                        }
                    }
                    return true;
                }

                case "bird_trio":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("bird_trio");
                    if (skinIdx == 0)
                    {
                        _bigBirdBiteUltTimer = 1.4f;
                        Run.Parts.SetUltForm(true);
                        Vector3 flat = new(aimDir.X, 0f, aimDir.Z);
                        flat = flat.LengthSquared() > 0.001f ? Vector3.Normalize(flat) : Run.PlayerForward;
                        Run.ApplyDash(flat * 24f, 0.22f);
                        CombatTarget? victim = FindBestAimedEnemy(myPos + Vector3.UnitY, aimDir, 14f, 0.1f) ?? CombatManager.FindNearestHostileAt(myPos + flat * 4f);
                        new VisualFX(myPos + flat * 3.8f + Vector3.UnitY, Rgb(255, 200, 45), 6.0f, 0.38f);
                        SoundManager.Play("slam_land", 0.92f, 0.72f);
                        Run.ShowBanner("СПАСЕНИЕ ОТ МОНСТРА!", Rgb(255, 215, 65), 2.0f);

                        if (victim != null && Vector3.Distance(myPos, victim.Position) <= 14.5f)
                        {
                            Vector3 hp = victim.Position + Vector3.UnitY;
                            float charmMult = victim.CharmTimer > 0f ? 1.85f : 1.0f;
                            CombatManager.ApplyPlayerHitEffects(victim, hp, BaseDamage * 10.0f * charmMult, Rgb(255, 210, 55), aoe: 4.0f);
                            if (IsValid(victim) && !victim.IsBoss && victim.Hp > 0f && victim.Hp <= victim.MaxHp * 0.35f)
                                CombatManager.ApplyPlayerHitEffects(victim, hp, victim.Hp + 9999f, Rgb(255, 60, 45), canProc: false);
                            if (!IsValid(victim) || victim.Hp <= 0f)
                                RCd *= 0.30f; // Убийство гипнотизированной жертвы возвращает 70% перезарядки!
                            Run.HealPlayer(Run.PlayerMaxHp * 0.25f);
                        }
                    }
                    else if (skinIdx == 1)
                    {
                        _bigBirdBiteUltTimer = 1.6f;
                        Run.Parts.SetUltForm(true);
                        SoundManager.Play("dante_clock", 0.95f, 0.88f);
                        SoundManager.Play("zap", 0.85f, 1.05f);
                        Run.ShowBanner("ПРАВЕДНЫЙ ПРИГОВОР ВЕСОВ!", Rgb(140, 250, 240), 2.2f);
                        new VisualFX(myPos + new Vector3(0, 2.2f, 0), Rgb(245, 225, 120), 8.5f, 0.42f);
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (!IsValid(e) || e.IsAlly || Vector3.Distance(myPos, e.Position) > 32f) continue;
                            if (rVar == 1) e.SinStacks = Math.Min(10, e.SinStacks + 2);
                            Vector3 hp = e.Position + new Vector3(0, e.HitHeight * 0.6f, 0);
                            new LightningBeam(hp + new Vector3(0, 18f, 0), hp, Rgb(135, 250, 240), 0.40f);
                            float jDmg = BaseDamage * 4.5f + MathF.Min(BaseDamage * 12f, e.MaxHp * 0.035f * e.SinStacks);
                            CombatManager.ApplyPlayerHitEffects(e, hp, jDmg, Rgb(140, 250, 240), aoe: 2.0f);
                        }
                    }
                    else
                    {
                        TriggerSmallBirdEnrage(5.5f);
                        Vector3 flat = new(aimDir.X, 0f, aimDir.Z);
                        flat = flat.LengthSquared() > 0.001f ? Vector3.Normalize(flat) : Run.PlayerForward;
                        Run.ApplyDash(flat * 22f, 0.20f);
                        Vector3 biteCenter = myPos + flat * 3.8f;
                        new VisualFX(biteCenter + Vector3.UnitY, Rgb(235, 20, 45), 7.5f, 0.40f);
                        SoundManager.Play("slam_land", 0.95f, 0.85f);
                        bool hitAny = false;
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(biteCenter, e.Position) <= 9.5f)
                            {
                                hitAny = true;
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 9.5f, Rgb(235, 25, 50), aoe: 2.5f, lifesteal: 18.0f);
                            }
                        }
                        if (hitAny) Run.HealPlayer(Run.PlayerMaxHp * 0.18f);
                    }
                    return true;
                }

                case "apocalypse_bird":
                {
                    _apocUltTimer = 6.0f;
                    _apocUltStrikeTick = 0.40f;
                    Run.Parts.SetUltForm(true);
                    Shield = MathF.Min(Run.PlayerMaxHp, Shield + 80f);
                    SoundManager.Play("dante_clock", 0.95f, 0.78f);
                    SoundManager.Play("ult_cast", 0.95f, 0.88f);
                    Run.ShowBanner("★ АПОКАЛИПСИС: МИР В ЧЁРНОМ ЛЕСУ! ★", Rgb(255, 205, 55), 2.5f);
                    new VisualFX(myPos + new Vector3(0, 2.0f, 0), Rgb(255, 195, 50), 11.0f, 0.48f);

                    var hostiles = new List<CombatTarget>();
                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (!IsValid(e) || e.IsAlly || Vector3.Distance(myPos, e.Position) > 34f) continue;
                        hostiles.Add(e);
                        e.SinStacks = Math.Min(10, e.SinStacks + 3);
                        float scaleDmg = BaseDamage * 4.2f + MathF.Min(BaseDamage * 10f, e.MaxHp * 0.03f * e.SinStacks);
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, scaleDmg, Rgb(255, 210, 60));
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        Vector3 strikePos;
                        if (hostiles.Count > 0 && IsValid(hostiles[i % hostiles.Count]))
                        {
                            var t = hostiles[i % hostiles.Count];
                            strikePos = t.Position + new Vector3(0, t.HitHeight * 0.5f, 0);
                            CombatManager.ApplyPlayerHitEffects(t, strikePos, BaseDamage * 1.35f, Rgb(255, 215, 70), lifesteal: 2.0f);
                        }
                        else
                        {
                            float a = i * (MathF.PI * 2f / 16f);
                            strikePos = myPos + new Vector3(MathF.Cos(a) * 14f, 0.5f, MathF.Sin(a) * 14f);
                        }
                        new LightningBeam(strikePos + new Vector3(0, 20f, 0), strikePos, Rgb(255, 215, 65), 0.42f);
                    }
                    return true;
                }

                case "komeiji":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("komeiji");
                    if (skinIdx != 1)
                    {
                        _koishiUltTimer = 2.0f;
                        Run.Parts.SetUltForm(true);
                        SoundManager.Play("ult_cast", 0.88f, 1.22f);
                        UnleashSatoriCopiedTraumaWave(myPos);
                        for (int i = 0; i < 20; i++)
                        {
                            float ang = i * (MathF.PI * 2f / 20f);
                            Vector3 dir = new(MathF.Cos(ang), 0.03f, MathF.Sin(ang));
                            Color col = (i % 2 == 0) ? Rgb(255, 60, 145) : Rgb(195, 85, 255);
                            var p = new Projectile(myPos + new Vector3(0, 1.4f, 0), dir, 40f, BaseDamage * 1.85f, 0.38f, col, aoe: 2.0f, pierce: true, homing: rVar == 1);
                            Run.StageEntities.Add(p);
                        }
                        Vector3 end = myPos + Vector3.UnitY + aimDir * 30f;
                        new LightningBeam(myPos + new Vector3(0, 1.5f, 0), end, Rgb(255, 55, 145), 0.42f);
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (!IsValid(e) || e.IsAlly || Vector3.Distance(myPos, e.Position) > 28f) continue;
                            if (Vector3.Dot(Vector3.Normalize(e.Position - myPos), aimDir) >= 0.30f)
                            {
                                e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 6.0f);
                                e.StunTimer = MathF.Max(e.StunTimer, 1.6f);
                                CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 5.2f, Rgb(255, 60, 145), aoe: 2.5f);
                            }
                        }
                    }
                    else
                    {
                        _koishiUltTimer = 6.0f;
                        _koishiStealth = 1.0f;
                        Run.Parts.SetUltForm(true);
                        Run.ShowBanner("«ПОДЗЕМНАЯ РОЗА» :: ЦВЕТЕНИЕ БУТОНОВ!", Rgb(255, 65, 135), 2.2f);
                        SoundManager.Play("ult_cast", 0.88f, 1.32f);
                        foreach (var b in KoishiRosebuds.ToArray())
                            if (IsValid(b)) b.BloomDetonate(1.5f);
                        KoishiRosebuds.Clear();
                        Run.StageEntities.Add(new KoishiRoseStormFX(this, rVar == 1));
                    }
                    return true;
                }

                case "false_son":
                {
                    FalseSonSpikes = GetFalseSonMaxSpikes();
                    _falseSonSpikesInit = true;
                    float bonusHp = MathF.Max(0f, Run.PlayerMaxHp - Cfg.Hp);
                    if (rVar == 0)
                    {
                        float laserDur = 3.2f + MathF.Min(2.5f, bonusHp * 0.008f);
                        _falseSonUltTimer = laserDur;
                        Run.Parts.SetUltForm(true);
                        Run.ShowBanner("ЛАЗЕР ОТЦА!", Rgb(105, 245, 255), 2.2f);
                        SoundManager.Play("laser", 0.92f, 0.82f);
                        Run.StageEntities.Add(new FalseSonLaserChannelFX(this, laserDur));
                    }
                    else
                    {
                        _falseSonMeridianWillTimer = 6.0f;
                        _falseSonUltTimer = 6.0f;
                        Run.Parts.SetUltForm(true);
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 90f);
                        Run.ShowBanner("ВОЛЯ МЕРИДИАНА!", Rgb(255, 215, 75), 2.2f);
                        SoundManager.Play("zap", 0.92f, 0.80f);
                        new VisualFX(myPos + Vector3.UnitY, Rgb(255, 215, 75), 9.5f, 0.42f);
                        foreach (var e in CombatManager.Enemies.ToArray())
                        {
                            if (!IsValid(e) || e.IsAlly || Vector3.Distance(myPos, e.Position) > 24f) continue;
                            new LightningBeam(myPos + Vector3.UnitY, e.Position + Vector3.UnitY, Rgb(255, 215, 75), 0.36f);
                            Vector3 pulled = Physics.ResolveHorizontalCollision(Vector3.Lerp(e.Position, myPos, 0.62f), e.HitRadius, e.Position.Y);
                            pulled.Y = Physics.GetSurfaceHeight(pulled.X, pulled.Z, e.Position.Y);
                            e.Position = pulled;
                            e.StunTimer = MathF.Max(e.StunTimer, 1.4f);
                            e.LunarRuinStacks = Math.Min(12, e.LunarRuinStacks + 3);
                            e.LunarRuinTimer = 6.0f;
                            CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 4.5f + bonusHp * 0.40f, Rgb(255, 215, 75));
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private void UpdatePack2Skills(float dt, bool isMoving)
        {
            switch (CharKey)
            {
                case "daitaku_helios":
                {
                    if (_heliosUltTimer > 0f)
                    {
                        _heliosUltTimer -= dt;
                        HeliosHype = 1.0f;
                        _heliosBassTick -= dt;
                        if (_heliosBassTick <= 0f)
                        {
                            _heliosBassTick = 1.0f;
                            Run.HealPlayer(8f);
                            Shield = MathF.Min(Run.PlayerMaxHp, Shield + 10f);
                            new VisualFX(Run.PlayerPos + new Vector3(0, 0.6f, 0), Rgb(0, 245, 255), 6.2f, 0.28f);
                            SoundManager.Play("bounce_pad", 0.58f, 1.18f);
                            foreach (var e in CombatManager.Enemies.ToArray())
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 11f)
                                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 2.4f, Rgb(255, 65, 210), aoe: 1.5f);
                        }
                        if (_heliosUltTimer <= 0f) Run.Parts.SetUltForm(false);
                    }
                    else
                    {
                        float delta = (isMoving || Run.DashTimer > 0f) ? 0.22f * dt : -0.10f * dt;
                        HeliosHype = Math.Clamp(HeliosHype + delta, 0f, 1f);
                    }
                    break;
                }

                case "robin":
                {
                    if (_robinAriaTimer > 0f) _robinAriaTimer -= dt;
                    if (_robinHarmonyCritTimer > 0f) _robinHarmonyCritTimer -= dt;
                    if (_robinConcertTimer > 0f)
                    {
                        _robinConcertTimer -= dt;
                        _robinConcertWaveTick -= dt;
                        if (_robinConcertWaveTick <= 0f)
                        {
                            _robinConcertWaveTick = 1.2f;
                            Run.HealPlayer(10f + Run.PlayerMaxHp * 0.025f);
                            new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(255, 225, 115), 4.8f, 0.26f);
                            for (int i = 0; i < 4; i++)
                            {
                                float a = i * MathF.PI * 0.5f;
                                var p = new Projectile(Run.PlayerPos + Vector3.UnitY, new(MathF.Cos(a), 0.05f, MathF.Sin(a)), 38f, BaseDamage * 1.1f, 0.34f, Rgb(255, 230, 125), homing: true, healAllies: 14f);
                                Run.StageEntities.Add(p);
                            }
                        }
                        if (_robinConcertTimer <= 0f) Run.Parts.SetUltForm(false);
                    }
                    break;
                }

                case "bird_trio":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("bird_trio");
                    if (_bigBirdDarkStepTimer > 0f) _bigBirdDarkStepTimer -= dt;
                    if (_bigBirdBiteUltTimer > 0f) { _bigBirdBiteUltTimer -= dt; if (_bigBirdBiteUltTimer <= 0f) Run.Parts.SetUltForm(false); }
                    if (_smallBirdGuardTimer > 0f) _smallBirdGuardTimer -= dt;
                    if (_smallBirdEnrageTimer > 0f) { _smallBirdEnrageTimer -= dt; if (_smallBirdEnrageTimer <= 0f) Run.Parts.SetUltForm(false); }

                    if (skinIdx == 0)
                    {
                        _bigBirdAuraTick -= dt;
                        if (_bigBirdAuraTick <= 0f)
                        {
                            _bigBirdAuraTick = 0.6f;
                            foreach (var e in CombatManager.Enemies)
                            {
                                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 14.0f)
                                {
                                    e.SlowTimer = MathF.Max(e.SlowTimer, 1.2f);
                                    e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 1.2f);
                                }
                            }
                        }
                    }
                    break;
                }

                case "apocalypse_bird":
                {
                    _apocAuraTick -= dt;
                    if (_apocAuraTick <= 0f)
                    {
                        _apocAuraTick = 0.7f;
                        foreach (var e in CombatManager.Enemies)
                        {
                            if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 14.0f)
                            {
                                e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 1.5f);
                                e.SinStacks = Math.Min(10, e.SinStacks + 1);
                            }
                        }
                    }
                    if (_apocUltTimer > 0f)
                    {
                        _apocUltTimer -= dt;
                        _apocUltStrikeTick -= dt;
                        if (_apocUltStrikeTick <= 0f)
                        {
                            _apocUltStrikeTick = 0.45f;
                            CombatTarget? t = CombatManager.FindNearestHostileAt(Run.PlayerPos);
                            if (t != null && Vector3.Distance(Run.PlayerPos, t.Position) <= 34f)
                            {
                                Vector3 hp = t.Position + Vector3.UnitY;
                                new LightningBeam(hp + new Vector3(0, 18f, 0), hp, Rgb(255, 215, 65), 0.25f);
                                CombatManager.ApplyPlayerHitEffects(t, hp, BaseDamage * 1.9f, Rgb(255, 215, 65));
                            }
                        }
                        if (_apocUltTimer <= 0f) Run.Parts.SetUltForm(false);
                    }
                    break;
                }

                case "komeiji":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("komeiji");
                    if (_koishiUltTimer > 0f) { _koishiUltTimer -= dt; if (_koishiUltTimer <= 0f) Run.Parts.SetUltForm(false); }
                    if (skinIdx != 1)
                    {
                        _satoriSubconsciousTick -= dt;
                        if (_satoriSubconsciousTick <= 0f)
                        {
                            _satoriSubconsciousTick = 2.4f;
                            if (CombatManager.FindNearestHostileAt(Run.PlayerPos) != null)
                            {
                                var hb = new Projectile(Run.PlayerPos + new Vector3(0.6f, 1.3f, 0), GetAimDirection(), 44f, BaseDamage * 1.45f, 0.34f, Rgb(80, 255, 165), aoe: 1.8f, homing: true, healAllies: 8f);
                                Run.StageEntities.Add(hb);
                            }
                        }
                    }
                    else
                    {
                        if (_koishiPhaseTimer > 0f) _koishiPhaseTimer -= dt;
                        if (_koishiOutOfCombatTimer > 0f) _koishiOutOfCombatTimer -= dt;
                        else if (isMoving) _koishiStealth = Math.Clamp(_koishiStealth + 0.32f * dt, 0f, 1.0f);
                    }
                    break;
                }

                case "false_son":
                {
                    int maxSpikes = GetFalseSonMaxSpikes();
                    if (!_falseSonSpikesInit) { FalseSonSpikes = maxSpikes; _falseSonSpikesInit = true; }
                    if (FalseSonSpikes > maxSpikes) FalseSonSpikes = maxSpikes;
                    if (_falseSonMeridianWillTimer > 0f) _falseSonMeridianWillTimer -= dt;
                    if (_falseSonUltTimer > 0f) { _falseSonUltTimer -= dt; if (_falseSonUltTimer <= 0f) Run.Parts.SetUltForm(false); }

                    float bonusHp = MathF.Max(0f, Run.PlayerMaxHp - Cfg.Hp);
                    float growthMult = _falseSonMeridianWillTimer > 0f ? 3.0f : 1.0f;
                    if (FalseSonSpikes < maxSpikes)
                    {
                        float interval = MathF.Max(0.75f, 2.5f / (1.0f + bonusHp * 0.006f) / (_falseSonMeridianWillTimer > 0f ? 2.0f : 1.0f));
                        _falseSonSpikeRegenTimer += dt;
                        if (_falseSonSpikeRegenTimer >= interval)
                        {
                            _falseSonSpikeRegenTimer = 0f;
                            FalseSonSpikes = Math.Min(maxSpikes, FalseSonSpikes + 1);
                        }
                    }
                    int spent = Math.Max(0, maxSpikes - FalseSonSpikes);
                    if (spent > 0 && Run.PlayerHp < Run.PlayerMaxHp)
                    {
                        _falseSonRegenAccum += 1.8f * spent * growthMult * dt;
                        if (_falseSonRegenAccum >= 2.0f)
                        {
                            float healAmt = _falseSonRegenAccum;
                            _falseSonRegenAccum = 0f;
                            Run.HealPlayer(healAmt);
                        }
                    }
                    break;
                }
            }
        }

        private bool TryBlockPack2Hit(float amount, CombatTarget? attacker)
        {
            if (CharKey == "bird_trio")
            {
                int skinIdx = ProgressData.Instance.GetSelectedSkin("bird_trio");
                if (skinIdx == 0 && _bigBirdDarkStepTimer > 0f)
                {
                    new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(255, 215, 70), 2.2f, 0.22f);
                    return true;
                }
                if (skinIdx == 2 && _smallBirdGuardTimer > 0f)
                {
                    _smallBirdGuardTimer = 0f;
                    SoundManager.Play("parry", 0.90f, 1.25f);
                    TriggerSmallBirdEnrage(5.5f);
                    if (attacker != null && IsValid(attacker) && !attacker.IsAlly)
                        CombatManager.ApplyPlayerHitEffects(attacker, attacker.Position + Vector3.UnitY, BaseDamage * 3.5f, Rgb(235, 25, 50), lifesteal: 8.0f);
                    return true;
                }
            }
            else if (CharKey == "komeiji" && ProgressData.Instance.GetSelectedSkin("komeiji") == 1)
            {
                if (_koishiPhaseTimer > 0f || _rng.NextDouble() < 0.22)
                {
                    new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(85, 255, 170), 1.8f, 0.20f);
                    SoundManager.Play("magic_cast", 0.50f, 1.45f);
                    return true;
                }
            }
            return false;
        }

        private float ModifyPack2IncomingDamage(float dmg, CombatTarget? attacker)
        {
            switch (CharKey)
            {
                case "bird_trio":
                {
                    int skinIdx = ProgressData.Instance.GetSelectedSkin("bird_trio");
                    if (skinIdx == 0) return dmg * 0.85f;
                    if (skinIdx == 2)
                    {
                        if (IsValid(attacker) && !attacker!.IsAlly)
                        {
                            attacker.PunishingGrudgeTimer = 8.0f;
                            PunishingBirdGrudgeTarget = attacker;
                        }
                        TriggerSmallBirdEnrage(5.5f);
                        return dmg * 0.85f;
                    }
                    return dmg;
                }
                case "apocalypse_bird":
                    return dmg * 0.80f;
                case "komeiji":
                    if (ProgressData.Instance.GetSelectedSkin("komeiji") == 1)
                    {
                        _koishiOutOfCombatTimer = 2.0f;
                        _koishiStealth *= 0.5f;
                    }
                    return dmg;
                case "false_son":
                {
                    if (!_falseSonSpikesInit) { FalseSonSpikes = GetFalseSonMaxSpikes(); _falseSonSpikesInit = true; }
                    float mult = _falseSonMeridianWillTimer > 0f ? 2.0f : 1.0f;
                    float dr = MathF.Min(0.70f, 0.035f * FalseSonSpikes * mult);
                    return dmg * (1.0f - dr);
                }
                default:
                    return dmg;
            }
        }
    }
}
