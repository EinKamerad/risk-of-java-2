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
            float luckBonus = MathF.Max(0.15f, 1.0f + 0.12f * GetItemCount("clover") - 0.25f * GetItemCount("purity"));
            float dmg = baseDmg;
            float bonusMult = 1.0f;

            // Shattering Justice: 25% шанс расколоть броню (с учётом удачи)
            int shat = GetItemCount("shattering_justice");
            if (shat > 0 && _rng.NextDouble() < MathF.Min(0.80f, 0.25f * luckBonus))
            {
                target.ArmorBreakTimer = 4.0f * shat;
                new VisualFX(hitPos, Rgb(255, 165, 55), 1.8f, 0.22f);
            }
            if (target.ArmorBreakTimer > 0f) bonusMult += 0.20f * Math.Max(1, shat);

            // Crowbar (>85% HP)
            int crow = GetItemCount("crowbar");
            if (crow > 0 && target.Hp >= target.MaxHp * 0.85f) bonusMult += 0.50f * crow;

            // Armor-Piercing Rounds
            int apr = GetItemCount("ap_rounds");
            if (apr > 0 && target.IsBoss) bonusMult += 0.20f * apr;

            // Focus Crystal (<13m)
            int focus = GetItemCount("focus_crystal");
            if (focus > 0 && Vector3.Distance(g.PlayerPos, targetPos) <= 13.0f) bonusMult += 0.20f * focus;

            // Brilliant Behemoth & Артефакт Хаоса
            float effAoe = aoe;
            int behem = GetItemCount("behemoth");
            if (behem > 0)
            {
                bonusMult += 0.25f * behem;
                effAoe = MathF.Max(effAoe, 2.5f + 1.0f * behem);
            }
            if (GameData.HasArtifact("chaos"))
            {
                effAoe = MathF.Max(effAoe * 1.40f, 3.0f);
            }

            dmg *= bonusMult;

            // Sheen (Сияние — Чародейский клинок после навыка)
            int sheen = GetItemCount("sheen");
            if (sheen > 0 && p.SheenProcReady)
            {
                p.SheenProcReady = false;
                p.SheenCd = 1.5f;
                dmg += p.BaseDamage * (1.0f * sheen);
                new VisualFX(hitPos, Rgb(65, 195, 255), 2.5f, 0.25f);
            }

            // Blade of the Ruined King (Клинок Падшего Короля — урон от текущего HP + вампиризм + кража скорости)
            int bork = GetItemCount("blade_ruined_king");
            if (bork > 0 && IsValid(target))
            {
                float maxBorkCap = target.IsBoss ? p.BaseDamage * (3.2f * bork) : 9999f;
                float borkDmg = MathF.Min(maxBorkCap, target.Hp * (0.06f * bork));
                dmg += borkDmg;
                g.HealPlayer(MathF.Max(1.0f, borkDmg * 0.25f));
                target.SlowTimer = MathF.Max(target.SlowTimer, 2.0f);
                p.BorkSpeedTimer = 2.5f;
            }

            // Критический удар + Infinity Edge + Harvester's Scythe + Predatory Instincts
            bool isCrit = _rng.NextDouble() < p.CritChance;
            if (isCrit)
            {
                float critMult = 2.0f + 0.45f * GetItemCount("infinity_edge");
                dmg *= critMult;
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

            // Leeching Seed + Doran's Blade + врождённый вампиризм навыка
            float healAmt = lifesteal + 1.5f * GetItemCount("leeching_seed") + 2.0f * GetItemCount("dorans_blade");
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

            // Liandry's Torment (Мучения Лиандри — духовное горение от макс. HP врага)
            int liandry = GetItemCount("liandrys_torment");
            if (liandry > 0 && IsValid(target))
            {
                target.LiandryBurnTimer = MathF.Max(target.LiandryBurnTimer, 3.0f);
                float maxHpBurn = MathF.Min(p.BaseDamage * (1.8f * liandry), target.MaxHp * (0.015f * liandry));
                target.LiandryBurnDps = MathF.Max(target.LiandryBurnDps, p.BaseDamage * (0.45f * liandry) + maxHpBurn);
            }

            Program.TriggerHitMarker(isCrit, dmg);
            target.TakeDamage(dmg);
            new VisualFX(hitPos, hitColor, effAoe == 0f ? 1.3f : effAoe * 1.35f, 0.22f);

            // Old Guillotine: казнь только обычных врагов с низким уровнем здоровья (порог снижен вдвое)
            int guill = GetItemCount("guillotine");
            if (IsValid(target) && guill > 0 && !target.IsBoss)
            {
                float thresh = MathF.Min(0.25f, 0.065f * guill);
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
                if (GameData.HasArtifact("chaos") && Vector3.Distance(g.PlayerPos, targetPos) <= effAoe * 0.62f)
                {
                    DamagePlayer(MathF.Min(g.PlayerMaxHp * 0.07f, baseDmg * 0.14f), null, "chaos");
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

            // Statikk Shiv (Заточка Статикка — каждый 4-й удар бьёт жёлтой цепной молнией)
            int shiv = GetItemCount("statikk_shiv");
            if (shiv > 0)
            {
                p.StatikkHitCounter++;
                if (p.StatikkHitCounter >= 4)
                {
                    p.StatikkHitCounter = 0;
                    int maxTargets = 3 + shiv;
                    float chainRad = 15.0f + shiv * 2.5f;
                    int chained = 0;
                    Vector3 prevPos = targetPos + new Vector3(0, 1.2f, 0);
                    float zapDmg = p.BaseDamage * (1.35f + 0.65f * shiv);
                    if (IsValid(target)) target.TakeDamage(zapDmg);
                    new VisualFX(prevPos, Color.Yellow, 2.4f, 0.24f);
                    foreach (var other in Enemies.ToArray())
                    {
                        if (other != target && IsValid(other) && !other.IsAlly)
                        {
                            Vector3 otherPos = other.Position + new Vector3(0, 1.2f, 0);
                            if (Vector3.Distance(otherPos, prevPos) <= chainRad)
                            {
                                other.TakeDamage(zapDmg);
                                new LightningBeam(prevPos, otherPos, Color.Yellow, 0.25f);
                                new VisualFX(otherPos, Color.Yellow, 2.0f, 0.22f);
                                prevPos = otherPos;
                                if (++chained >= maxTargets) break;
                            }
                        }
                    }
                }
            }

            // Luden's Companion (Спутник Людена — залп самонаводящихся магических сфер при 100 зарядах)
            int luden = GetItemCount("ludens_companion");
            if (luden > 0)
            {
                p.LudensCharges = MathF.Min(100f, p.LudensCharges + 14f);
                if (p.LudensCharges >= 100f)
                {
                    p.LudensCharges = 0f;
                    int bolts = 3 + luden;
                    new VisualFX(hitPos, Rgb(225, 85, 255), 3.2f, 0.28f);
                    for (int i = 0; i < bolts; i++)
                    {
                        float ang = (i / (float)bolts) * MathF.PI * 2f;
                        Vector3 d = Vector3.Normalize(new Vector3(MathF.Cos(ang) * 0.5f, 0.85f, MathF.Sin(ang) * 0.5f));
                        new Projectile(hitPos + new Vector3(0, 0.6f, 0), d, 52f, p.BaseDamage * (0.75f + 0.45f * luden), 0.30f, Rgb(235, 95, 255), isPlayer: true, homing: true, canProc: false);
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
            Program.RecordEnemyKill(dead.GoldReward);

            if (p.CharKey == "v1")
                p.V1AddStyle(dead.IsBoss ? "+ КРУПНОЕ УБИЙСТВО" : "+ УБИЙСТВО", dead.IsBoss ? Color.Red : Color.White, dead.IsBoss ? 160f : 45f);
            else if (p.CharKey == "ahri")
                p.AddAhriEssence(dead.IsBoss ? 3 : 1);

            if (dead.IsBoss || _rng.NextDouble() < 0.08)
            {
                int gain = dead.EnemyType == "mithrix" ? 5 : (dead.IsBoss ? 2 : 1);
                g.LunarCoins += gain;
            }

            if (dead.EnemyType == "mithrix" && dead.StolenItems.Count > 0)
            {
                foreach (var retKey in dead.StolenItems)
                {
                    if (!g.ItemOrder.Contains(retKey)) g.ItemOrder.Add(retKey);
                    g.Items[retKey] = (g.Items.TryGetValue(retKey, out int c) ? c : 0) + 1;
                }
                dead.StolenItems.Clear();
            }

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

            if (dead.IsDoppelganger && Net.NetworkManager.Instance.Mode != "client")
            {
                g.SpawnItemDrop(deathPos, null, "large_chest");
                g.ShowBanner("ТЕНЕВОЙ ДОППЕЛЬГАНГЕР ПОВЕРЖЕН! ОН ОСТАВИЛ ТРОФЕЙ!", Rgb(215, 75, 255), 3.2f);
            }
            else if (GameData.HasArtifact("sacrifice") && !dead.IsBoss && Physics.SpecialMode != "pvp" && Net.NetworkManager.Instance.Mode != "client")
            {
                bool isHeavy = dead.IsElite || dead.EnemyType is "golem" or "greater_wisp" or "imp" or "lunar_chimera" or "bison" or "brass_contraption" or "parent" or "elder_lemurian";
                double sacChance = isHeavy ? 0.28 : 0.14;
                if (_rng.NextDouble() < sacChance)
                {
                    string sacPool = (isHeavy && _rng.NextDouble() < 0.30) ? "large_chest" : "small_chest";
                    g.SpawnItemDrop(deathPos, null, sacPool);
                }
            }
            else if (Physics.SpecialMode == "pvp" && !dead.IsBoss && Net.NetworkManager.Instance.Mode != "client")
            {
                bool isEliteMob = dead.EnemyType is "golem" or "greater_wisp" or "imp" or "lunar_chimera" or "bison" or "brass_contraption" or "parent" or "elder_lemurian";
                double dropChance = isEliteMob ? 0.45 : 0.22;
                if (_rng.NextDouble() < dropChance)
                {
                    string pvpPool = (isEliteMob && _rng.NextDouble() < 0.35) ? "large_chest" : "small_chest";
                    g.SpawnItemDrop(deathPos, null, pvpPool);
                }
            }

            if (dead.IsBoss)
            {
                ProgressData.Instance.BossKills++;
                Program.RunBossKills++;
                g.UnlockAchievement("ach_boss");
                if (Program.RunBossKills >= 2)
                    g.UnlockAchievement("ach_boss_hunter");
                if (dead.EnemyType == "mithrix")
                    g.OnMithrixDefeated(deathPos);
                else if (Net.NetworkManager.Instance.Mode != "client")
                    g.SpawnItemDrop(deathPos, null, "boss");
            }

            ProgressData.Instance.Save();
        }

        public static CombatTarget? FindNearestHostileAt(Vector3 pos)
        {
            CombatTarget? best = null;
            float bestDist = 99999f;
            foreach (var e in Enemies)
            {
                if (IsValid(e) && !e.IsAlly)
                {
                    float d = Vector3.Distance(pos, e.Position + new Vector3(0, e.HitHeight * 0.5f, 0));
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = e;
                    }
                }
            }
            return best;
        }

        public static void DamagePlayer(float amount, CombatTarget? attacker = null, string? customSource = null)
        {
            Controller?.TakeDamage(amount, attacker, customSource);
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
        public CombatTarget? SourceEnemy;
        public string SourceEnemyType = "";
        public bool SourceIsElite = false;
        public bool SourceIsBoss = false;
        public bool SourceIsDoppel = false;

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
            if (!isPlayer)
            {
                SourceEnemy = CombatManager.FindNearestHostileAt(pos);
                if (SourceEnemy != null)
                {
                    SourceEnemyType = SourceEnemy.EnemyType;
                    SourceIsElite = SourceEnemy.IsElite;
                    SourceIsBoss = SourceEnemy.IsBoss;
                    SourceIsDoppel = SourceEnemy.IsDoppelganger;
                }
            }
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

            RotationY += 320f * dt;
            RotationX += 210f * dt;

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
                // Лечение союзников и друзей по коопу шприцемётом Медика TF2
                if (HealAllies > 0f)
                {
                    if (Physics.SpecialMode != "pvp" && Program.TryHealRemotePlayerCoop(Position, ScaleX * 0.4f + 1.15f, HealAllies))
                    {
                        g.HealPlayer(HealAllies * 0.25f);
                        new VisualFX(Position, Color.Lime, 2.2f, 0.25f);
                        SafeDestroy();
                        return;
                    }
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

                if (Physics.SpecialMode == "pvp")
                {
                    if (Program.TryHitRemotePlayerPvP(Position, ScaleX * 0.4f + 0.95f, Damage))
                    {
                        new VisualFX(Position, Color, 2.2f, 0.25f);
                        SafeDestroy();
                        return;
                    }
                    foreach (var st in p.MindustryTurrets.ToArray())
                    {
                        if (IsValid(st) && st.IsRemoteVisual && Vector3.Distance(Position, st.Position + new Vector3(0, 1.0f, 0)) <= st.HitRadius + 0.5f)
                        {
                            st.TakeStructureDamage(Damage, fromNetwork: false);
                            SafeDestroy();
                            return;
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
                // Попадание вражеских снарядов по постройкам Корабля Mindustry (турелям, ядру, бурам, пилонам)
                foreach (var st in p.MindustryTurrets.ToArray())
                {
                    if (IsValid(st) && Vector3.Distance(Position, st.Position + new Vector3(0, 1.0f, 0)) <= st.HitRadius + 0.35f)
                    {
                        st.TakeStructureDamage(Damage);
                        new VisualFX(Position, Color.Red, 1.6f, 0.2f);
                        SafeDestroy();
                        return;
                    }
                }

                // Попадание вражеских снарядов по клонам М4А1
                foreach (var cl in p.DummyClones.ToArray())
                {
                    if (IsValid(cl) && Vector3.Distance(Position, cl.Position + new Vector3(0, 1.0f, 0)) <= 1.15f)
                    {
                        cl.Hp -= Damage;
                        new VisualFX(Position, Color.Red, 1.5f, 0.2f);
                        if (cl.Hp <= 0f) Destroy(cl);
                        SafeDestroy();
                        return;
                    }
                }

                if (g.CanPlayerInteract && Vector3.Distance(Position, g.PlayerPos + new Vector3(0, 1.0f, 0)) <= 1.25f)
                {
                    if (p.TryReflectOrParryProjectile(this))
                        return;
                    string? snapSrc = !string.IsNullOrEmpty(SourceEnemyType)
                        ? $"mob:{SourceEnemyType}:{(SourceIsBoss ? 1 : 0)}:{(SourceIsElite ? 1 : 0)}:{(SourceIsDoppel ? 1 : 0)}"
                        : null;
                    CombatManager.DamagePlayer(Damage, IsValid(SourceEnemy) ? SourceEnemy : null, snapSrc);
                    new VisualFX(Position, Color.Red, 1.6f, 0.2f);
                    SafeDestroy();
                }
                else if (g.IsNearRemotePlayer(Position, 1.05f))
                {
                    new VisualFX(Position, Color.Red, 1.6f, 0.2f);
                    SafeDestroy();
                }
            }
        }
    }
}