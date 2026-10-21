using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using RiskOfCSharp2.World;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Entities
{
    public static class CombatManager
    {
        public static readonly List<CombatTarget> Enemies = new();
        public static SurvivorCombatController? Controller;
        private static readonly Random _rng = new();

        public static int GetItemCount(string key)
        {
            var g = WorldContext.Game;
            return g != null && g.Items.TryGetValue(key, out int cnt) ? cnt : 0;
        }

        public static void ApplyPlayerHitEffects(
            CombatTarget target, Vector3 hitPos, float baseDmg, Color hitColor,
            float aoe = 0f, float lifesteal = 0f, bool canProc = true)
        {
            var g = WorldContext.Game;
            var p = Controller;
            if (g == null || p == null || !IsValid(target) || target.IsAlly) return;

            Vector3 targetPos = target.Position;
            float luckBonus = 1.0f + 0.12f * GetItemCount("clover");
            float dmg = baseDmg;

            // Shattering Justice
            int shat = GetItemCount("shattering_justice");
            if (shat > 0) target.ArmorBreakTimer = 4.0f * shat;
            if (target.ArmorBreakTimer > 0f) dmg *= 1.0f + 0.20f * Math.Max(1, shat);

            // Crowbar (>85% HP)
            int crow = GetItemCount("crowbar");
            if (crow > 0 && target.Hp >= target.MaxHp * 0.85f) dmg *= 1.0f + 0.50f * crow;

            // Armor-Piercing Rounds
            int apr = GetItemCount("ap_rounds");
            if (apr > 0 && target.IsBoss) dmg *= 1.0f + 0.20f * apr;

            // Focus Crystal (<13m)
            int focus = GetItemCount("focus_crystal");
            if (focus > 0 && Vector3.Distance(g.PlayerPos, targetPos) <= 13.0f) dmg *= 1.0f + 0.20f * focus;

            // Brilliant Behemoth
            float effAoe = aoe;
            int behem = GetItemCount("behemoth");
            if (behem > 0)
            {
                dmg *= 1.0f + 0.25f * behem;
                effAoe = MathF.Max(effAoe, 2.5f + 1.0f * behem);
            }

            // Критический удар + Harvester's Scythe + Predatory Instincts
            if (_rng.NextDouble() < p.CritChance)
            {
                dmg *= 2.0f;
                new VisualFX(hitPos, Rgb(255, 45, 70), 2.2f, 0.25f);
                int scythe = GetItemCount("harvester_scythe");
                if (scythe > 0) g.HealPlayer(2.0f + 4.0f * scythe);
                if (GetItemCount("predatory") > 0) p.PredatoryTimer = 3.0f;
            }

            // ULTRAKILL V1: Blood is Fuel!
            if (p.CharKey == "v1")
            {
                dmg *= p.V1StyleDmgMult;
                if (Vector3.Distance(g.PlayerPos, targetPos) <= 11.0f)
                {
                    float bloodHeal = Math.Clamp(dmg * 0.18f * p.V1StyleDmgMult, 2.0f, 18.0f);
                    g.HealPlayer(bloodHeal);
                    new VisualFX(targetPos + new Vector3(0, 1.1f, 0), Color.Red, 1.5f, 0.16f);
                }
            }

            // Leeching Seed + врождённый вампиризм навыка
            float healAmt = lifesteal + 1.5f * GetItemCount("leeching_seed");
            if (healAmt > 0f) g.HealPlayer(healAmt);

            // Статусы на враге
            int chrono = GetItemCount("chronobauble");
            if (chrono > 0) target.SlowTimer = MathF.Max(target.SlowTimer, 2.5f * chrono);

            int stunG = GetItemCount("stun_grenade");
            if (stunG > 0 && !target.IsBoss && _rng.NextDouble() < MathF.Min(0.65f, 0.10f * stunG * luckBonus))
            {
                target.StunTimer = MathF.Max(target.StunTimer, 1.5f);
                new VisualFX(targetPos + new Vector3(0, 1.5f, 0), Color.White, 2.0f, 0.25f);
            }

            int triTip = GetItemCount("tri_tip");
            if (triTip > 0 && _rng.NextDouble() < MathF.Min(1.0f, 0.15f * triTip * luckBonus))
            {
                target.BleedTimer = MathF.Max(target.BleedTimer, 3.0f);
                target.BleedDps = MathF.Max(target.BleedDps, p.BaseDamage * 0.6f * triTip);
            }

            target.TakeDamage(dmg);
            new VisualFX(hitPos, hitColor, effAoe == 0f ? 1.3f : effAoe * 1.35f, 0.22f);

            // Old Guillotine: казнь врагов с низким уровнем здоровья
            int guill = GetItemCount("guillotine");
            if (IsValid(target) && guill > 0 && target.EnemyType != "mithrix")
            {
                float thresh = MathF.Min(0.45f, 0.13f * guill);
                if (target.Hp / MathF.Max(1f, target.MaxHp) <= thresh)
                {
                    new VisualFX(targetPos + new Vector3(0, 1.5f, 0), Color.White, 4.0f, 0.3f);
                    target.TakeDamage(target.Hp + 999f);
                }
            }

            if (effAoe > 0f)
            {
                foreach (var other in Enemies.ToArray())
                {
                    if (other != target && IsValid(other) && !other.IsAlly && Vector3.Distance(other.Position, targetPos) <= effAoe)
                        other.TakeDamage(dmg * 0.65f);
                }
            }

            if (!canProc) return;

            // Plasma Shrimp
            int shrimp = GetItemCount("plasma_shrimp");
            if (shrimp > 0 && p.Shield >= 1.0f)
            {
                Vector3 mPos = g.PlayerPos + new Vector3(0, 2.0f, 0);
                Vector3 dir = Vector3.Normalize(new Vector3((float)_rng.NextDouble() * 0.6f - 0.3f, 0.5f, (float)_rng.NextDouble() * 0.6f - 0.3f));
                new Projectile(mPos, dir, 54f, p.BaseDamage * (0.40f * shrimp), 0.28f, Rgb(195, 95, 255), isPlayer: true, homing: true, canProc: false);
            }

            // Sticky Bomb
            int sticky = GetItemCount("sticky_bomb");
            if (sticky > 0 && _rng.NextDouble() < MathF.Min(0.75f, 0.12f * sticky * luckBonus))
            {
                new VisualFX(targetPos + new Vector3(0, 1.0f, 0), Color.Orange, 3.8f, 0.3f);
                if (IsValid(target)) target.TakeDamage(p.BaseDamage * (1.4f * sticky));
            }

            // Kjaro's Band & Runald's Band
            if (dmg >= p.BaseDamage * 1.8f || _rng.NextDouble() < 0.16f * luckBonus)
            {
                int kjaro = GetItemCount("kjaro_band");
                if (kjaro > 0)
                {
                    new VisualFX(targetPos + new Vector3(0, 1.2f, 0), Rgb(255, 100, 20), 5.2f, 0.38f);
                    foreach (var other in Enemies.ToArray())
                        if (IsValid(other) && !other.IsAlly && Vector3.Distance(other.Position, targetPos) <= 4.8f)
                            other.TakeDamage(p.BaseDamage * (1.5f * kjaro));
                }
                int runald = GetItemCount("runald_band");
                if (runald > 0 && IsValid(target))
                {
                    new VisualFX(targetPos + new Vector3(0, 1.2f, 0), Color.SkyBlue, 4.0f, 0.35f);
                    target.SlowTimer = 4.0f;
                    target.TakeDamage(p.BaseDamage * (1.8f * runald));
                }
            }

            // AtG Missile Mk. 1
            int atg = GetItemCount("atg_missile");
            if (atg > 0 && _rng.NextDouble() < MathF.Min(0.75f, 0.12f * atg * luckBonus))
            {
                Vector3 mPos = g.PlayerPos + new Vector3(0, 2.2f, 0);
                new Projectile(mPos, Vector3.Normalize(new Vector3(0, 0.6f, 0.8f)), 48f, p.BaseDamage * (2.2f * atg), 0.36f, Rgb(115, 255, 80), isPlayer: true, homing: true, canProc: false);
            }

            // Polylute
            int poly = GetItemCount("polylute");
            if (poly > 0 && IsValid(target) && _rng.NextDouble() < MathF.Min(0.85f, 0.25f * luckBonus))
            {
                int strikes = 2 + poly;
                new VisualFX(targetPos + new Vector3(0, 1.3f, 0), Rgb(205, 85, 255), 3.0f, 0.28f);
                new LightningBeam(g.PlayerPos + new Vector3(0, 1.5f, 0), targetPos + new Vector3(0, 1.2f, 0), Rgb(205, 85, 255), 0.24f);
                target.TakeDamage(dmg * (0.45f * strikes));
            }

            // Ukulele
            int uku = GetItemCount("ukulele");
            if (uku > 0 && _rng.NextDouble() < MathF.Min(0.85f, 0.25f * luckBonus))
            {
                int maxTargets = 2 + uku * 2;
                float chainRad = 13.0f + uku * 2.5f;
                int chained = 0;
                Vector3 prevPos = targetPos + new Vector3(0, 1.2f, 0);
                foreach (var other in Enemies.ToArray())
                {
                    if (other != target && IsValid(other) && !other.IsAlly)
                    {
                        Vector3 otherPos = other.Position + new Vector3(0, 1.2f, 0);
                        if (Vector3.Distance(otherPos, prevPos) <= chainRad)
                        {
                            other.TakeDamage(dmg * 0.8f);
                            new LightningBeam(prevPos, otherPos, Color.Magenta);
                            new VisualFX(otherPos, Color.Magenta, 2.0f, 0.25f);
                            prevPos = otherPos;
                            if (++chained >= maxTargets) break;
                        }
                    }
                }
            }
        }

        public static void OnEnemyKilled(CombatTarget dead)
        {
            Enemies.Remove(dead);
            var g = WorldContext.Game;
            var p = Controller;
            if (g == null || p == null) return;

            Vector3 deathPos = dead.Position;
            new VisualFX(deathPos + new Vector3(0, 1.2f, 0), Color.Orange, 3.5f, 0.38f);
            g.Gold += dead.GoldReward;

            ProgressData.Instance.Kills++;
            ProgressData.Instance.Save();

            if (p.CharKey == "v1")
                p.V1AddStyle(dead.IsBoss ? "+ BIG KILL" : "+ KILL", dead.IsBoss ? Color.Red : Color.White, dead.IsBoss ? 160f : 45f);

            int tooth = GetItemCount("monster_tooth");
            if (tooth > 0) g.HealPlayer(4.0f + 4.0f * tooth);

            int topaz = GetItemCount("topaz_brooch");
            if (topaz > 0) p.Shield = MathF.Min(g.PlayerMaxHp, p.Shield + 15.0f * topaz);

            int inf = GetItemCount("infusion");
            if (inf > 0 && p.InfusionBonus < 40.0f * inf)
            {
                p.InfusionBonus += 1.0f;
                g.HealPlayer(1.0f);
            }

            int band = GetItemCount("bandolier");
            if (band > 0 && _rng.NextDouble() < MathF.Min(0.65f, 0.18f * band))
            {
                p.M2Cd = MathF.Max(0f, p.M2Cd - 1.5f);
                p.ShiftCd = MathF.Max(0f, p.ShiftCd - 1.5f);
                p.RCd = MathF.Max(0f, p.RCd - 1.5f);
            }

            if (GetItemCount("frost_relic") > 0)
                p.FrostRelicTimer = 4.0f;

            int dagger = GetItemCount("ceremonial_dagger");
            if (dagger > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    float ang = (i / 3.0f) * MathF.PI * 2f;
                    Vector3 d = Vector3.Normalize(new Vector3(MathF.Cos(ang) * 0.4f, 1.0f, MathF.Sin(ang) * 0.4f));
                    new Projectile(deathPos + new Vector3(0, 1.5f, 0), d, 46f, p.BaseDamage * (1.5f * dagger), 0.32f, Rgb(255, 75, 110), isPlayer: true, homing: true, canProc: false);
                }
            }

            int gas = GetItemCount("gasoline");
            int wisp = GetItemCount("will_o_wisp");
            if (gas > 0 || wisp > 0)
            {
                float radius = 6.0f + gas * 2.0f + wisp * 3.0f;
                float burnDmg = p.BaseDamage * (1.2f * gas + 2.0f * wisp);
                new VisualFX(deathPos + new Vector3(0, 1.0f, 0), gas >= wisp ? Color.Red : Color.Orange, radius * 1.35f, 0.4f);
                foreach (var other in Enemies.ToArray())
                {
                    if (IsValid(other) && !other.IsAlly && Vector3.Distance(other.Position, deathPos) <= radius)
                        other.TakeDamage(burnDmg);
                }
            }
        }

        public static void DamagePlayer(float amount)
        {
            Controller?.TakeDamage(amount);
        }
    }

    public class Projectile : Entity
    {
        public Vector3 Direction;
        public float Speed;
        public float Damage;
        public bool IsPlayer;
        public float Aoe;
        public bool Pierce;
        public int Bounces;
        public bool Homing;
        public float Lifesteal;
        public bool CanProc;
        public float HealAllies;
        public float Lifetime;

        public bool IsV1Rocket = false;
        public bool IsV1Core = false;
        public bool IsV1Nail = false;
        public float GravityArc = 0f;

        private readonly HashSet<Entity> _hitMemory = new();

        public Projectile(
            Vector3 pos, Vector3 direction, float speed, float damage, float scaleVal, Color projColor,
            bool isPlayer = true, float aoe = 0f, bool pierce = false, int bounces = 0, bool homing = false,
            float lifesteal = 0f, bool canProc = true, float maxLifetime = 3.4f, float healAllies = 0f)
            : base(model: "sphere", position: pos, scaleUniform: scaleVal, color: projColor, unlit: true)
        {
            new Entity(parent: this, model: "diamond", scaleUniform: 1.45f, color: Color.White, unlit: true);
            Direction = direction.LengthSquared() > 0.0001f ? Vector3.Normalize(direction) : Vector3.UnitZ;
            Speed = speed;
            Damage = damage;
            IsPlayer = isPlayer;
            Aoe = aoe;
            Pierce = pierce;
            Bounces = bounces;
            Homing = homing;
            Lifesteal = lifesteal;
            CanProc = canProc;
            HealAllies = healAllies;
            Lifetime = maxLifetime;
        }

        public void SafeDestroy()
        {
            if (!IsDestroyed) Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            var p = CombatManager.Controller;
            if (g == null || p == null || IsDestroyed) return;

            if (Homing && IsPlayer && CombatManager.Enemies.Count > 0)
            {
                CombatTarget? nearest = null;
                float bestD = 28f;
                foreach (var e in CombatManager.Enemies)
                {
                    if (IsValid(e) && !e.IsAlly && !_hitMemory.Contains(e))
                    {
                        float d = Vector3.Distance(Position, e.Position + new Vector3(0, e.HitHeight, 0));
                        if (d < bestD) { bestD = d; nearest = e; }
                    }
                }
                if (nearest != null)
                {
                    Vector3 desired = Vector3.Normalize((nearest.Position + new Vector3(0, nearest.HitHeight, 0)) - Position);
                    Direction = Vector3.Normalize(Lerp(Direction, desired, dt * 8.0f));
                }
            }

            if (IsV1Rocket && p.V1RocketsFrozen)
            {
                RotationZ += 540f * dt;
                return;
            }

            if (GravityArc > 0f)
                Direction.Y -= GravityArc * dt;

            if (IsV1Nail)
            {
                foreach (var mag in p.V1Magnets)
                {
                    if (IsValid(mag))
                    {
                        Vector3 toMag = (mag.Position + new Vector3(0, 0.8f, 0)) - Position;
                        if (toMag.Length() <= 18.0f)
                        {
                            Direction = Vector3.Normalize(Lerp(Direction, Vector3.Normalize(toMag), dt * 14.0f));
                            break;
                        }
                    }
                }
            }

            Position += Direction * Speed * dt;
            RotationZ += 360f * dt;
            Lifetime -= dt;
            if (Lifetime <= 0f || Y < -1f)
            {
                if (IsV1Core || IsV1Rocket)
                {
                    new VisualFX(Position, Color.Orange, Aoe > 0f ? Aoe * 1.4f : 4.0f, 0.3f);
                    foreach (var e in CombatManager.Enemies.ToArray())
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(e.Position, Position) <= MathF.Max(3.5f, Aoe))
                            CombatManager.ApplyPlayerHitEffects(e, Position, Damage, Color, aoe: 0f, lifesteal: Lifesteal);
                }
                SafeDestroy();
                return;
            }

            if (!Pierce && Physics.IsInsideSolidObstacle(Position, 0.2f))
            {
                new VisualFX(Position, Color, 1.2f, 0.18f);
                SafeDestroy();
                return;
            }

            if (IsPlayer)
            {
                // Лечение союзников шприцемётом Медика TF2
                if (HealAllies > 0f)
                {
                    foreach (var al in CombatManager.Enemies)
                    {
                        if (IsValid(al) && al.IsAlly && !_hitMemory.Contains(al))
                        {
                            Vector3 alCenter = al.Position + new Vector3(0, al.HitHeight, 0);
                            if (Vector3.Distance(Position, alCenter) <= al.HitRadius + 0.5f)
                            {
                                _hitMemory.Add(al);
                                al.HealAlly(HealAllies);
                                g.HealPlayer(HealAllies * 0.25f);
                                new VisualFX(alCenter, Color.Lime, 2.2f, 0.25f);
                                SafeDestroy();
                                return;
                            }
                        }
                    }
                }

                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly || _hitMemory.Contains(e)) continue;
                    Vector3 hitCenter = e.Position + new Vector3(0, e.HitHeight, 0);
                    if (Vector3.Distance(Position, hitCenter) <= e.HitRadius + ScaleX * 0.4f)
                    {
                        _hitMemory.Add(e);
                        CombatManager.ApplyPlayerHitEffects(e, Position, Damage, Color, Aoe, Lifesteal, CanProc);

                        if (Bounces > 0)
                        {
                            Bounces--;
                            CombatTarget? nextTarget = null;
                            float bestD = 18.0f;
                            foreach (var other in CombatManager.Enemies)
                            {
                                if (IsValid(other) && !other.IsAlly && !_hitMemory.Contains(other))
                                {
                                    float d = Vector3.Distance(Position, other.Position);
                                    if (d < bestD) { bestD = d; nextTarget = other; }
                                }
                            }
                            if (nextTarget != null)
                            {
                                Vector3 nextPos = nextTarget.Position + new Vector3(0, nextTarget.HitHeight, 0);
                                new LightningBeam(Position, nextPos, Color, 0.22f);
                                Direction = Vector3.Normalize(nextPos - Position);
                                return;
                            }
                            SafeDestroy();
                            return;
                        }

                        if (!Pierce)
                        {
                            SafeDestroy();
                            return;
                        }
                    }
                }
            }
            else
            {
                if (Vector3.Distance(Position, g.PlayerPos + new Vector3(0, 1.0f, 0)) <= 1.05f)
                {
                    CombatManager.DamagePlayer(Damage);
                    new VisualFX(Position, Color.Red, 1.6f, 0.2f);
                    SafeDestroy();
                }
            }
        }
    }
}