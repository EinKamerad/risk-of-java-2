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
    public class KoishiRosebudTrapEntity : Entity
    {
        private float _life = 14.0f;
        private float _armTimer = 0.35f;
        private readonly float _dmg;
        private readonly Entity _bud;

        public KoishiRosebudTrapEntity(Vector3 pos, float dmg) : base(position: pos)
        {
            _dmg = dmg;
            Y = Physics.GetSurfaceHeight(X, Z, Y + 1.5f);
            _bud = new Entity(parent: this, model: "diamond", scale: new(0.65f, 0.85f, 0.65f), position: new(0, 0.45f, 0), color: Rgb(255, 65, 135), unlit: true);
            new Entity(parent: this, model: "sphere", scale: new(3.2f, 0.08f, 3.2f), position: new(0, 0.06f, 0), color: Rgb(75, 255, 165, 75), unlit: true);
        }

        public void BloomDetonate(float mult = 1.0f)
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            SoundManager.Play("magic_cast", 0.72f, 1.35f);
            new VisualFX(Position + new Vector3(0, 0.6f, 0), Rgb(255, 65, 140), 4.8f, 0.30f);

            for (int i = 0; i < 6; i++)
            {
                float ang = i * (MathF.PI / 3f);
                Vector3 dir = new(MathF.Cos(ang), 0.06f, MathF.Sin(ang));
                Color col = (i & 1) == 0 ? Rgb(255, 65, 135) : Rgb(75, 255, 165);
                new Projectile(Position + new Vector3(0, 0.7f, 0), dir, 36f, _dmg * 0.65f * mult, 0.32f, col, isPlayer: true, aoe: 1.6f, homing: true, maxLifetime: 1.5f);
            }

            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (IsValid(e) && !e.IsAlly && Vector3.Distance(Position, e.Position) <= 5.2f + e.HitRadius)
                {
                    e.SlowTimer = MathF.Max(e.SlowTimer, 2.2f);
                    CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, _dmg * mult, Rgb(255, 65, 140), aoe: 1.8f, canProc: true);
                }
            }
            Destroy(this);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            _bud.RotationY += 180f * dt;
            if (_life <= 0f) { BloomDetonate(1.0f); return; }
            if (_armTimer > 0f) { _armTimer -= dt; return; }

            foreach (var e in CombatManager.Enemies)
            {
                if (IsValid(e) && !e.IsAlly && Physics.Distance2D(Position, e.Position) <= 2.5f + e.HitRadius)
                {
                    BloomDetonate(1.0f);
                    return;
                }
            }
        }
    }

    public partial class SurvivorCombatController
    {
        // --- Состояния углубления героев Пак 1 и Пак 2 (#33 - #44) ---
        public Projectile? SusieActiveBusterProj = null;
        public bool SusieBusterBoosted = false;

        public float SpamtonDealCd = 0f;
        public int SpamtonNeoStrings = 6;
        public float SpamtonNeoRegrowTimer = 0f;

        public float DonLanternDevourBonusHp = 0f;
        public float DonChompKillWindow = 0f;
        private int _donLanternStageTracker = -1;

        public float HeliosBeatTimer = 0f; // 128 BPM (2.1333 удара/сек)
        public float HeliosLastBeatFlash = 0f;

        public float RobinAllyHarmonyIcd = 0f;

        public CombatTarget? PunishingBirdGrudgeTarget = null;

        public string SatoriCopiedArchetype = "fire";
        public readonly List<KoishiRosebudTrapEntity> KoishiRosebuds = new();

        public float GetKoishiStealthValue() => _koishiStealth;

        public float GetHeliosBeatPhase() => HeliosBeatTimer - MathF.Floor(HeliosBeatTimer);

        public bool IsHeliosOnBeat()
        {
            float ph = GetHeliosBeatPhase();
            return ph <= 0.19f || ph >= 0.81f;
        }

        public float ConsumeHeliosBeatBonus(string label)
        {
            if (CharKey != "daitaku_helios" || !IsHeliosOnBeat())
                return 1.0f;
            HeliosHype = Math.Clamp(HeliosHype + 0.18f, 0f, 1.0f);
            HeliosLastBeatFlash = 0.45f;
            SoundManager.Play("coin_flip", 0.80f, 1.42f);
            new VisualFX(Run.PlayerPos + new Vector3(0, 1.3f, 0), Rgb(0, 255, 240), 3.6f, 0.22f);
            Run.ShowBanner($"♪ PERFECT BEAT :: {label} (+45% МОЩЬ!) ♪", Rgb(0, 255, 240), 1.15f);
            return 1.45f;
        }

        public void UseSpamtonHotDealGamble()
        {
            if (SpamtonDealCd > 0f) return;
            if (Run.Gold < 25 && Run.PlayerHp <= Run.PlayerMaxHp * 0.20f)
            {
                Run.ShowBanner("НЕДОСТАТОЧНО [[КРОМЕРОВ]] ДЛЯ СДЕЛКИ!", Color.Gray, 1.1f);
                return;
            }

            SpamtonDealCd = 7.5f;
            if (Run.Gold >= 25)
                Run.Gold -= 25;
            else
                Run.PlayerHp = MathF.Max(1f, Run.PlayerHp - Run.PlayerMaxHp * 0.12f);

            bool jackpot = _rng.NextDouble() < 0.65;
            if (jackpot)
            {
                _spamtonKromerMult = 1.85f;
                _spamtonKromerTimer = 6.0f;
                SoundManager.Play("coin_flip", 1.0f, 1.30f);
                new VisualFX(Run.PlayerPos + Vector3.UnitY, Color.Gold, 5.5f, 0.32f);
                Vector3 aim = GetAimDirection();
                for (int i = 0; i < 4; i++)
                {
                    float a = (i / 4f) * MathF.PI * 2f;
                    Vector3 vel = aim * 16f + new Vector3(MathF.Cos(a) * 6f, 10f, MathF.Sin(a) * 6f);
                    Run.StageEntities.Add(new PipisBombEntity(Run.PlayerPos + new Vector3(0, 1.5f, 0), vel, BaseDamage * 1.95f, isNeo: false, bounces: 2));
                }
                Run.ShowBanner("💰 [[ГОРЯЧАЯ СДЕЛКА]] ДЖЕКПОТ! x1.85 УРОН + ДОЖДЬ PIPIS! 💰", Color.Gold, 2.0f);
            }
            else
            {
                _spamtonKromerMult = 1.18f;
                _spamtonKromerTimer = 4.0f;
                Shield = MathF.Min(Run.PlayerMaxHp, Shield + 55f);
                SoundManager.Play("zap", 0.85f, 0.90f);
                new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(95, 225, 255), 4.0f, 0.28f);
                Run.ShowBanner("⚡ [[СБОЙ КОТИРОВОК]] +55 ГЛИТЧ-ЩИТА И x1.18 КРОМЕР! ⚡", Rgb(95, 225, 255), 1.8f);
            }
        }

        public void OnSpamtonNeoHitStringSnap(ref float dmg)
        {
            if (CharKey != "spamton" || ProgressData.Instance.GetSelectedSkin("spamton") != 2)
                return;
            if (dmg < 6f || SpamtonNeoStrings <= 0)
                return;

            dmg *= 0.78f;
            SpamtonNeoStrings--;
            SoundManager.Play("parry", 0.75f, 1.35f + (6 - SpamtonNeoStrings) * 0.08f);
            new LightningBeam(Run.PlayerPos + new Vector3(0, 1.5f, 0), Run.PlayerPos + new Vector3(0, 16f, 0), Rgb(115, 255, 135), 0.22f);

            if (SpamtonNeoStrings <= 0)
            {
                SpamtonNeoRegrowTimer = 5.0f;
                _spamtonFrenzyTimer = MathF.Max(_spamtonFrenzyTimer, 4.5f);
                SoundManager.Play("explosion", 1.0f, 1.10f);
                new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(255, 95, 215), 9.5f, 0.42f);
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 12.0f)
                        CombatManager.ApplyPlayerHitEffects(e, e.Position + Vector3.UnitY, BaseDamage * 5.2f, Rgb(255, 95, 215), aoe: 2.5f, lifesteal: 8f, canProc: true);
                }
                Run.ShowBanner("★ НИТИ ОБОРВАНЫ :: [[СВОБОДА / FREEDOM!!]] ★", Rgb(255, 110, 225), 2.2f);
            }
        }

        public void OnDonLanternEnemyKilled(CombatTarget dead)
        {
            if (CharKey != "don_quixote" || ProgressData.Instance.GetSelectedSkin("don_quixote") != 1)
                return;
            bool nearLure = false;
            foreach (var ent in Run.StageEntities)
            {
                if (ent is MeatLanternLureEntity lure && IsValid(lure) && Vector3.Distance(lure.Position, dead.Position) <= 8.5f)
                {
                    nearLure = true;
                    break;
                }
            }
            if ((DonChompKillWindow > 0f || nearLure) && DonLanternDevourBonusHp < 160f)
            {
                float add = dead.IsBoss ? 24f : 8f;
                DonLanternDevourBonusHp = MathF.Min(160f, DonLanternDevourBonusHp + add);
                Run.HealPlayer(add + 6f);
                new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(145, 255, 95), 2.6f, 0.22f);
            }
        }

        public void TryRefractPriestessPrisms(Vector3 origin, Vector3 aimDir, float refractDmg)
        {
            if (CharKey != "priestess") return;
            foreach (var ent in Run.StageEntities.ToArray())
            {
                if (ent is not OriginiumPrismEntity prism || !IsValid(prism)) continue;
                Vector3 pCenter = prism.Position + new Vector3(0, 1.85f, 0);
                float proj = Math.Clamp(Vector3.Dot(pCenter - origin, aimDir), 0f, 34f);
                float rayDist = Vector3.Distance(pCenter, origin + aimDir * proj);
                if (rayDist <= 4.2f || Vector3.Distance(origin, pCenter) <= 6.5f)
                {
                    new LightningBeam(origin, pCenter, Rgb(255, 65, 95), 0.18f);
                    new VisualFX(pCenter, Rgb(255, 65, 95), 2.4f, 0.18f);
                    int splits = 0;
                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (!IsValid(e) || e.IsAlly || Vector3.Distance(pCenter, e.Position) > 22f) continue;
                        Vector3 hp = e.Position + new Vector3(0, e.HitHeight * 0.6f, 0);
                        new LightningBeam(pCenter, hp, Rgb(255, 85, 115), 0.20f);
                        AddPriestessOriginiumMark(e, 1);
                        CombatManager.ApplyPlayerHitEffects(e, hp, refractDmg, Rgb(255, 65, 95), aoe: 1.5f, canProc: true);
                        if (++splits >= 3) break;
                    }
                }
            }
        }

        public void FireClosureSpecializedDrone(ClosureDroneEntity drone, CombatTarget best)
        {
            Vector3 hp = best.Position + new Vector3(0, best.HitHeight * 0.55f, 0);
            Vector3 aim = Vector3.Normalize(hp - drone.Position);
            float baseMult = (drone.IsUpgraded ? 0.78f : 0.54f) * (drone.Overdrive ? 1.35f : 1.0f);

            if (drone.SlotIndex == 0)
            {
                // Дрон #1: Высокоточный Лазерный Резак (раскол брони)
                new LightningBeam(drone.Position, hp, Rgb(255, 145, 55), 0.16f);
                best.ArmorBreakTimer = MathF.Max(best.ArmorBreakTimer, 3.5f);
                CombatManager.ApplyPlayerHitEffects(best, hp, BaseDamage * baseMult * 1.25f, Rgb(255, 145, 55), canProc: true);
            }
            else if (drone.SlotIndex == 1)
            {
                // Дрон #2: Эгид-Щитовик (подпитка щита хозяйки и союзников + дуговой разряд)
                Shield = MathF.Min(Run.PlayerMaxHp * 0.90f, Shield + (drone.IsUpgraded ? 9f : 6f));
                Program.TryHealRemotePlayerCoop(Run.PlayerPos, 10f, 5f);
                new Projectile(drone.Position, aim, 54f, BaseDamage * baseMult * 0.95f, 0.28f, Rgb(85, 245, 195), isPlayer: true, homing: true, canProc: true, maxLifetime: 1.6f);
            }
            else
            {
                // Дрон #3: Крио-Подавитель (замедляющий AoE-снаряд)
                var p = new Projectile(drone.Position, aim, 48f, BaseDamage * baseMult, 0.36f, Rgb(95, 215, 255), isPlayer: true, aoe: 3.2f, canProc: true, maxLifetime: 1.8f);
                best.SlowTimer = MathF.Max(best.SlowTimer, 2.2f);
            }
        }

        public void TryTriggerRobinConcertFollowUp(Vector3 hitPos)
        {
            if (CharKey != "robin" || _robinConcertTimer <= 0f || RobinAllyHarmonyIcd > 0f)
                return;
            RobinAllyHarmonyIcd = 0.32f;
            RobinTriggerHarmonyStrike(hitPos, 0.85f);
        }

        public void CaptureSatoriEnemyTrauma(CombatTarget target)
        {
            if (!IsValid(target)) return;
            if (target.EnemyType.Contains("golem") || target.EnemyType.Contains("construct") || target.EnemyType.Contains("mithrix"))
                SatoriCopiedArchetype = "laser";
            else if (target.EnemyType.Contains("lunar") || target.EnemyType.Contains("void"))
                SatoriCopiedArchetype = "lunar";
            else if (target.IsBoss || target.EnemyType.Contains("beetle") || target.EnemyType.Contains("bison"))
                SatoriCopiedArchetype = "shockwave";
            else
                SatoriCopiedArchetype = "fire";
        }

        public void UnleashSatoriCopiedTraumaWave(Vector3 center)
        {
            Color col = SatoriCopiedArchetype switch
            {
                "laser" => Rgb(255, 75, 95),
                "lunar" => Rgb(110, 235, 255),
                "shockwave" => Color.Gold,
                _ => Rgb(255, 135, 55)
            };
            string title = SatoriCopiedArchetype switch
            {
                "laser" => "ВОСПОМИНАНИЕ «ЛУЧ ТИТАНА»",
                "lunar" => "ВОСПОМИНАНИЕ «ЛУННОЕ РАЗОРЕНИЕ»",
                "shockwave" => "ВОСПОМИНАНИЕ «РАСКОЛ КОЛОССА»",
                _ => "ВОСПОМИНАНИЕ «ПЛАМЯ ЯЩЕРОВ»"
            };
            new VisualFX(center + Vector3.UnitY, col, 7.5f, 0.34f);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * (MathF.PI / 4f);
                Vector3 dir = new(MathF.Cos(ang), 0.04f, MathF.Sin(ang));
                new Projectile(center + new Vector3(0, 1.3f, 0), dir, 44f, BaseDamage * 1.85f, 0.42f, col, isPlayer: true, aoe: 2.8f, pierce: SatoriCopiedArchetype == "laser", homing: SatoriCopiedArchetype == "lunar", canProc: true);
            }
            Run.ShowBanner($"ТРЕТИЙ ГЛАЗ САТОРИ :: {title}!", col, 1.8f);
        }

        public void PlantKoishiRosebud(Vector3 pos)
        {
            KoishiRosebuds.RemoveAll(b => !IsValid(b));
            if (KoishiRosebuds.Count >= 6)
            {
                var oldest = KoishiRosebuds[0];
                KoishiRosebuds.RemoveAt(0);
                oldest.BloomDetonate(0.85f);
            }
            var trap = new KoishiRosebudTrapEntity(pos, BaseDamage * 2.25f);
            KoishiRosebuds.Add(trap);
            Run.StageEntities.Add(trap);
        }

        public void UpdateReworkPackMechanics(float dt, bool isMoving)
        {
            if (_donLanternStageTracker != Run.Stage)
            {
                _donLanternStageTracker = Run.Stage;
                DonLanternDevourBonusHp = 0f;
            }
            if (SpamtonDealCd > 0f) SpamtonDealCd -= dt;
            if (DonChompKillWindow > 0f) DonChompKillWindow -= dt;
            if (HeliosLastBeatFlash > 0f) HeliosLastBeatFlash -= dt;
            if (RobinAllyHarmonyIcd > 0f) RobinAllyHarmonyIcd -= dt;

            // Susie: Тайминг взрыва Rude Buster в полёте по нажатию [Q] или [ПКМ]!
            if (CharKey == "susie" && IsValid(SusieActiveBusterProj) && !SusieBusterBoosted)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Q) || Raylib.IsMouseButtonPressed(MouseButton.Right))
                {
                    foreach (var e in CombatManager.Enemies)
                    {
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(SusieActiveBusterProj!.Position, e.Position + Vector3.UnitY) <= e.HitRadius + 5.0f)
                        {
                            SusieBusterBoosted = true;
                            SusieActiveBusterProj.Damage *= 1.60f;
                            SusieActiveBusterProj.Aoe += 3.0f;
                            M2Cd *= 0.65f;
                            SoundManager.Play("parry", 0.95f, 1.25f);
                            new VisualFX(SusieActiveBusterProj.Position, Color.Gold, 4.2f, 0.25f);
                            Run.ShowBanner("★ RUDE BUSTER CRIT! (+60% УРОН, -35% ПЕРЕЗАРЯДКИ) ★", Rgb(255, 115, 225), 1.3f);
                            break;
                        }
                    }
                }
            }

            // Spamton NEO: Парение на кукольных нитях и их регенерация после [[FREEDOM]]
            if (CharKey == "spamton" && ProgressData.Instance.GetSelectedSkin("spamton") == 2)
            {
                if (!Run.PlayerGrounded && SpamtonNeoStrings > 0 && Raylib.IsKeyDown(KeyboardKey.Space) && Run.PlayerVelY < 1.0f)
                    Run.PlayerVelY = MathF.Max(Run.PlayerVelY, -1.6f);

                if (SpamtonNeoStrings <= 0)
                {
                    SpamtonNeoRegrowTimer -= dt;
                    if (SpamtonNeoRegrowTimer <= 0f)
                    {
                        SpamtonNeoStrings = 6;
                        Shield = MathF.Min(Run.PlayerMaxHp, Shield + 40f);
                        Run.ShowBanner("КУКОЛЬНЫЕ НИТИ НЕО ВОССТАНОВЛЕНЫ [6/6]", Rgb(115, 255, 135), 1.3f);
                    }
                }
            }

            // Don Quixote: Авто-пробуждение Кровавых Крыльев Санчо на 30 Твёрдой Крови!
            if (CharKey == "don_quixote" && ProgressData.Instance.GetSelectedSkin("don_quixote") == 2)
            {
                if (_donHardblood >= 30 && _donAscendTimer <= 0.05f)
                {
                    _donAscendTimer = 4.5f;
                    Run.Parts.SetUltForm(true);
                    SoundManager.Play("ult_cast", 0.85f, 1.18f);
                    new VisualFX(Run.PlayerPos + Vector3.UnitY, Rgb(235, 35, 65), 4.8f, 0.28f);
                    Run.ShowBanner("АУРА ВТОРОГО РОДИЧА :: КРЫЛЬЯ САНЧО ПРОБУЖДЕНЫ [30/30]!", Rgb(245, 45, 75), 1.6f);
                }
            }

            // Daitaku Helios: Живой метроном 128 BPM!
            if (CharKey == "daitaku_helios")
            {
                HeliosBeatTimer += (128f / 60f) * dt;
            }

            // Apocalypse Bird: Активное влияние фазы Яйца (_apocEggPhase 0 / 1 / 2)
            if (CharKey == "apocalypse_bird")
            {
                if (_apocEggPhase == 0)
                {
                    // Большие Глаза: подсвечивают уязвимости врагов вокруг
                    foreach (var e in CombatManager.Enemies)
                        if (IsValid(e) && !e.IsAlly && Vector3.Distance(Run.PlayerPos, e.Position) <= 15f)
                            e.WeakpointScanTimer = MathF.Max(e.WeakpointScanTimer, 0.5f);
                }
            }
        }
    }
}
