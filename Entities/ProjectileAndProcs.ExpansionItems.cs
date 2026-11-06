using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.World;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Entities
{
    public partial class SurvivorCombatController
    {
        public float ExpansionBonusMaxHp = 0f;

        // Внутренние состояния и таймеры 20 предметов расширения
        private float _goldenAppleCd = 0f;
        internal int _sadOnionHitCounter = 0;
        private float _holyMantleCd = 0f;

        private int _spBatteryM1Count = 0;
        private bool _spBatteryNextSkillReady = false;
        private float _spBatteryActiveWindow = 0f;

        private float _bkbAvatarTimer = 0f;
        private float _bkbAuraTick = 0f;

        private int _expLastStage = -1;
        private string _expLastStageName = "";
        private int _totemChargesUsed = 0;
        private float _totemInvulnTimer = 0f;
        private bool _damoclesTriggeredThisStage = false;

        private int _brimstoneM1Count = 0;
        private float _truesilverTimer = 1.4f;
        private float _mawTimer = 1.8f;
        private float _ichorRetaliateCd = 0f;
        private float _armletFxTick = 0f;
        private float _elytraTrailTick = 0f;
        private float _prevV1M1Cd = 0f;
        private int _expShiftUses = 0;

        private void SyncExpansionStageState()
        {
            if (_expLastStage != Run.Stage || _expLastStageName != Run.StageName)
            {
                _expLastStage = Run.Stage;
                _expLastStageName = Run.StageName ?? "";
                _totemChargesUsed = 0;
                _damoclesTriggeredThisStage = false;
            }
        }

        internal void GrantExpansionShield(float amount)
        {
            if (amount <= 0f) return;
            float maxCap = MathF.Max(Run.PlayerMaxHp * 1.6f, Shield + amount);
            maxCap = MathF.Min(maxCap, MathF.Max(Run.PlayerMaxHp * 2.5f, 180f + amount));
            Shield = MathF.Min(maxCap, Shield + amount);
        }

        private void TryProcGoldenApple()
        {
            int gApple = CombatManager.GetItemCount("golden_apple");
            if (gApple > 0 && _goldenAppleCd <= 0f && Run.PlayerHp > 0f && Run.PlayerHp < Run.PlayerMaxHp * 0.50f)
            {
                _goldenAppleCd = 12.0f;
                float grant = 35.0f * gApple;
                GrantExpansionShield(grant);
                SoundManager.Play("heal", 0.90f, 1.16f);
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.35f, 0), Color.Gold, 3.6f, 0.32f);
                Run.ShowBanner($"ЗОЛОТОЕ ЯБЛОКО :: +{(int)grant} ЗОЛОТОГО ЩИТА!", Color.Gold, 1.4f);
            }
        }

        private float GetExpansionBaseDamageMult()
        {
            float mult = 1.0f;

            int branch = CombatManager.GetItemCount("iron_branch");
            if (branch > 0) mult *= 1.0f + 0.06f * branch;

            int armlet = CombatManager.GetItemCount("armlet_mordiggian");
            if (armlet > 0) mult *= 1.0f + 0.65f * armlet;

            int originium = CombatManager.GetItemCount("originium_prime");
            if (originium > 0) mult *= 1.0f + 0.40f * originium;

            if (_spBatteryActiveWindow > 0f)
            {
                int spBat = CombatManager.GetItemCount("poca_sp_battery");
                if (spBat > 0) mult *= 1.0f + 0.15f * spBat;
            }

            if (_bkbAvatarTimer > 0f)
                mult *= 1.30f;

            return mult;
        }

        private float GetExpansionMoveSpeedMult()
        {
            float mult = 1.0f;

            int branch = CombatManager.GetItemCount("iron_branch");
            if (branch > 0) mult *= 1.0f + 0.04f * branch;

            int elytra = CombatManager.GetItemCount("elytra_curse");
            if (elytra > 0) mult *= 1.0f + 0.28f * elytra;

            return mult;
        }

        private float GetExpansionFireRateMult()
        {
            float mult = 1.0f;

            int branch = CombatManager.GetItemCount("iron_branch");
            if (branch > 0) mult *= 1.0f + 0.05f * branch;

            int onion = CombatManager.GetItemCount("sad_onion");
            if (onion > 0) mult *= 1.0f + 0.18f * onion;

            int armlet = CombatManager.GetItemCount("armlet_mordiggian");
            if (armlet > 0) mult *= 1.0f + 0.35f * armlet;

            return mult;
        }

        private float GetExpansionCritChanceBonus() => 0f;

        private float GetExpansionCooldownTickMult(string slot)
        {
            float tickMult = 1.0f;

            int originium = CombatManager.GetItemCount("originium_prime");
            if (originium > 0)
                tickMult *= 1.0f / MathF.Pow(0.65f, originium);

            if (slot == "shift")
            {
                int pearl = CombatManager.GetItemCount("ender_pearl");
                if (pearl > 0)
                    tickMult *= 1.0f / MathF.Pow(0.88f, pearl);
            }

            return tickMult;
        }

        private void OnExpansionItemM1Fired()
        {
            // Батарея SP Родоса [Arknights]: каждая 4-я атака сокращает КД навыков на 0.8с и усиливает след. навык (+15%)
            int spBat = CombatManager.GetItemCount("poca_sp_battery");
            if (spBat > 0)
            {
                _spBatteryM1Count++;
                if (_spBatteryM1Count >= 4)
                {
                    _spBatteryM1Count = 0;
                    float cdCut = 0.80f * spBat;
                    M2Cd = MathF.Max(0f, M2Cd - cdCut);
                    ShiftCd = MathF.Max(0f, ShiftCd - cdCut);
                    RCd = MathF.Max(0f, RCd - cdCut);
                    _spBatteryNextSkillReady = true;
                    SoundManager.Play("ui_ready", 0.55f, 1.35f);
                    new VisualFX(Run.PlayerPos + new Vector3(0, 1.35f, 0), Rgb(95, 255, 195), 2.4f, 0.22f);
                }
            }

            // Сера (Brimstone) [Isaac]: каждая 6-я атака выжигает линию пробивным кровавым лазером Бездны (320% ур.)
            int brim = CombatManager.GetItemCount("brimstone");
            if (brim > 0)
            {
                _brimstoneM1Count++;
                if (_brimstoneM1Count >= 6)
                {
                    _brimstoneM1Count = 0;
                    Vector3 aimDir = GetAimDirection();
                    Vector3 origin = Run.PlayerPos + new Vector3(0, 1.48f, 0) + aimDir * 0.75f;
                    Vector3 endPos = origin + aimDir * 44.0f;

                    SoundManager.Play("railgun", 0.90f, 0.72f);
                    SoundManager.Play("flame", 0.85f, 0.78f);
                    new LightningBeam(origin, endPos, Rgb(225, 18, 42), 0.58f);
                    new LightningBeam(origin, endPos, Rgb(255, 125, 138), 0.24f);
                    new VisualFX(origin + aimDir * 0.5f, Rgb(245, 30, 55), 3.4f, 0.26f);

                    float brimDmg = BaseDamage * (3.20f + 1.40f * (brim - 1));
                    Vector3 seg = endPos - origin;
                    float segLenSq = MathF.Max(0.001f, seg.LengthSquared());

                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (!IsValid(e) || e.IsAlly) continue;
                        Vector3 eCenter = e.Position + new Vector3(0, e.HitHeight * 0.65f, 0);
                        float t = Math.Clamp(Vector3.Dot(eCenter - origin, seg) / segLenSq, 0f, 1f);
                        Vector3 closest = origin + seg * t;
                        if (Vector3.Distance(eCenter, closest) <= 2.25f + e.HitRadius * 0.60f)
                        {
                            new VisualFX(eCenter, Rgb(245, 35, 58), 2.5f, 0.24f);
                            e.TakeDamage(brimDmg);
                        }
                    }
                }
            }
        }

        private void OnExpansionItemSkillUsed(string slot)
        {
            // Активация накопленного усиления от Батареи SP Родоса для применяемого навыка
            if (_spBatteryNextSkillReady && CombatManager.GetItemCount("poca_sp_battery") > 0)
            {
                _spBatteryNextSkillReady = false;
                _spBatteryActiveWindow = 1.65f;
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.4f, 0), Rgb(85, 255, 195), 3.2f, 0.26f);
            }

            if (slot == "shift")
            {
                _expShiftUses++;
                if (_expShiftUses >= 15 || Run.ShiftCastCount + 1 >= 15)
                    Run.UnlockAchievement("ach_item_ender_pearl");

                // Эндер-Жемчуг [Minecraft]: Shift выпускает Нову Края (160% ур.) и даёт +22 Щита
                int pearl = CombatManager.GetItemCount("ender_pearl");
                if (pearl > 0)
                {
                    GrantExpansionShield(22.0f * pearl);
                    SoundManager.Play("portal_open", 0.78f, 1.35f);

                    Vector3 center = Run.PlayerPos + new Vector3(0, 1.15f, 0);
                    float novaRad = 8.0f + 1.0f * (pearl - 1);
                    float novaDmg = BaseDamage * (1.60f * pearl);
                    new VisualFX(center, Rgb(65, 225, 190), novaRad * 1.15f, 0.32f);
                    new VisualFX(center, Rgb(185, 75, 255), novaRad * 0.75f, 0.26f);

                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (!IsValid(e) || e.IsAlly) continue;
                        if (Vector3.Distance(e.Position, Run.PlayerPos) <= novaRad)
                        {
                            Vector3 eHit = e.Position + new Vector3(0, e.HitHeight * 0.7f, 0);
                            new LightningBeam(center, eHit, Rgb(175, 75, 255), 0.18f);
                            new VisualFX(eHit, Rgb(65, 235, 195), 2.0f, 0.22f);
                            e.TakeDamage(novaDmg);
                        }
                    }
                }
            }
            else if (slot == "r")
            {
                // Посох Чёрного Короля [Dota 2]: Ультимейт [R] включает Аватар на 3.5с (+1с/стак)
                int bkb = CombatManager.GetItemCount("black_king_bar");
                if (bkb > 0)
                {
                    _bkbAvatarTimer = 3.5f + 1.0f * (bkb - 1);
                    _bkbAuraTick = 0f;
                    SkillLockTimer = 0f;
                    SoundManager.Play("zhonyas", 0.92f, 0.84f);
                    new VisualFX(Run.PlayerPos + new Vector3(0, 1.4f, 0), Color.Gold, 6.2f, 0.38f);
                    Run.ShowBanner($"ПОСОХ ЧЁРНОГО КОРОЛЯ :: АВАТАР ({_bkbAvatarTimer:F1}С)!", Color.Gold, 1.8f);
                }
            }
        }

        private void UpdateExpansionItems(float dt, bool isMoving)
        {
            SyncExpansionStageState();

            _goldenAppleCd = MathF.Max(0f, _goldenAppleCd - dt);
            _holyMantleCd = MathF.Max(0f, _holyMantleCd - dt);
            _totemInvulnTimer = MathF.Max(0f, _totemInvulnTimer - dt);
            _spBatteryActiveWindow = MathF.Max(0f, _spBatteryActiveWindow - dt);
            _ichorRetaliateCd = MathF.Max(0f, _ichorRetaliateCd - dt);

            if (Run.Gold >= 250)
                Run.UnlockAchievement("ach_item_golden_apple");

            // Поддержка счётчика выстрелов ЛКМ для V1 (так как V1 выходит из UseM1Skill раньше общего вызова)
            if (CharKey == "v1")
            {
                if (M1Cd > _prevV1M1Cd + 0.02f)
                    OnExpansionItemM1Fired();
                _prevV1M1Cd = M1Cd;
            }

            // Золотое Яблоко: проверка порога < 50% HP
            TryProcGoldenApple();

            // Аватар Посоха Чёрного Короля (BKB): иммунитет к контролю и золотое свечение
            if (_bkbAvatarTimer > 0f)
            {
                _bkbAvatarTimer = MathF.Max(0f, _bkbAvatarTimer - dt);
                SkillLockTimer = 0f;
                _bkbAuraTick -= dt;
                if (_bkbAuraTick <= 0f)
                {
                    _bkbAuraTick = 0.22f;
                    new VisualFX(Run.PlayerPos + new Vector3(0, 1.25f, 0), Rgb(255, 210, 55), 2.8f, 0.20f);
                }
            }

            // Истинно-Серебряный Разрез [Arknights]: каждые 2.8с обрушивает 3 серебряные волны по 5 врагам (210% ур. + раскол брони)
            int truesilver = CombatManager.GetItemCount("silverash_truesilver");
            if (truesilver > 0)
            {
                _truesilverTimer -= dt;
                if (_truesilverTimer <= 0f)
                {
                    var targets = new List<CombatTarget>(5);
                    foreach (var e in CombatManager.Enemies)
                    {
                        if (!IsValid(e) || e.IsAlly) continue;
                        if (Vector3.Distance(e.Position, Run.PlayerPos) <= 24.0f)
                            targets.Add(e);
                    }

                    if (targets.Count > 0)
                    {
                        _truesilverTimer = 2.8f;
                        targets.Sort((a, b) => Vector3.DistanceSquared(a.Position, Run.PlayerPos).CompareTo(Vector3.DistanceSquared(b.Position, Run.PlayerPos)));
                        int hitLimit = Math.Min(5, targets.Count);

                        Vector3 waveDir = targets[0].Position - Run.PlayerPos;
                        waveDir.Y = 0f;
                        waveDir = waveDir.LengthSquared() > 0.01f ? Vector3.Normalize(waveDir) : Run.PlayerForward;

                        SoundManager.Play("scissor_snip", 0.80f, 1.15f);
                        SoundManager.Play("melee_swing", 0.75f, 0.92f);
                        Run.StageEntities.Add(new TruesilverSlashWaveFX(Run.PlayerPos + new Vector3(0, 1.25f, 0), waveDir));

                        float slashDmg = BaseDamage * (2.10f * truesilver);
                        for (int i = 0; i < hitLimit; i++)
                        {
                            var e = targets[i];
                            if (!IsValid(e)) continue;
                            e.ArmorBreakTimer = MathF.Max(e.ArmorBreakTimer, 4.0f);
                            Vector3 eHit = e.Position + new Vector3(0, e.HitHeight * 0.75f, 0);
                            new LightningBeam(eHit + new Vector3(-1.3f, 1.1f, -0.5f), eHit + new Vector3(1.3f, -0.7f, 0.5f), Rgb(230, 245, 255), 0.22f);
                            new VisualFX(eHit, Rgb(215, 240, 255), 2.6f, 0.24f);
                            e.TakeDamage(slashDmg);
                        }
                    }
                    else
                    {
                        _truesilverTimer = 0.25f;
                    }
                }
            }

            // Пасть Пустоты [Isaac]: каждые 3.5с вокруг героя пульсирует чёрно-алое кольцо Бездны (180% ур. и щит)
            int maw = CombatManager.GetItemCount("maw_of_void");
            if (maw > 0)
            {
                _mawTimer -= dt;
                if (_mawTimer <= 0f)
                {
                    _mawTimer = 3.5f;
                    float mawRad = 8.5f + 0.8f * (maw - 1);
                    float mawDmg = BaseDamage * (1.80f * maw);

                    SoundManager.Play("flame", 0.65f, 0.62f);
                    Run.StageEntities.Add(new MawOfVoidPulseFX(Run.PlayerPos + new Vector3(0, 0.95f, 0), mawRad));

                    int hitCount = 0;
                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (!IsValid(e) || e.IsAlly) continue;
                        if (Vector3.Distance(e.Position, Run.PlayerPos) <= mawRad)
                        {
                            hitCount++;
                            new VisualFX(e.Position + new Vector3(0, e.HitHeight * 0.65f, 0), Rgb(225, 35, 85), 2.2f, 0.22f);
                            e.TakeDamage(mawDmg);
                        }
                    }

                    if (hitCount > 0)
                        GrantExpansionShield(6.0f * maw * hitCount);
                }
            }

            // Ихор Морских Рождений [Arknights]: пассивная регенерация +2.5 HP/с за стак
            int ichor = CombatManager.GetItemCount("seaborne_ichor");
            if (ichor > 0 && Run.PlayerHp > 0f && Run.PlayerHp < Run.PlayerMaxHp)
            {
                Run.HealPlayer(2.5f * ichor * dt);
            }

            // Армлет Мордиггиана [Dota 2]: в бою отнимает 2.5% макс. HP/с за стак вплоть до 1 HP (не убивает)
            int armlet = CombatManager.GetItemCount("armlet_mordiggian");
            if (armlet > 0 && Run.PlayerHp > 1.0f)
            {
                bool inCombat = false;
                for (int i = 0; i < CombatManager.Enemies.Count; i++)
                {
                    var e = CombatManager.Enemies[i];
                    if (IsValid(e) && !e.IsAlly && Vector3.DistanceSquared(e.Position, Run.PlayerPos) <= 36.0f * 36.0f)
                    {
                        inCombat = true;
                        break;
                    }
                }

                if (inCombat)
                {
                    float drain = Run.PlayerMaxHp * (0.025f * armlet) * dt;
                    Run.PlayerHp = MathF.Max(1.0f, Run.PlayerHp - drain);
                    _armletFxTick -= dt;
                    if (_armletFxTick <= 0f)
                    {
                        _armletFxTick = 0.55f;
                        new VisualFX(Run.PlayerPos + new Vector3(0, 1.25f, 0), Rgb(215, 55, 135), 1.35f, 0.16f);
                    }
                }
            }

            // Проклятые Элитры [Minecraft]: визуальный лунный след крыльев при планировании в воздухе
            int elytra = CombatManager.GetItemCount("elytra_curse");
            if (elytra > 0 && !Run.PlayerGrounded && Raylib.IsKeyDown(KeyboardKey.Space) && Run.PlayerVelY <= 0.5f)
            {
                _elytraTrailTick -= dt;
                if (_elytraTrailTick <= 0f)
                {
                    _elytraTrailTick = 0.11f;
                    Vector3 backPos = Run.PlayerPos + new Vector3(0, 1.15f, 0) - Run.PlayerForward * 0.45f;
                    new VisualFX(backPos, Rgb(125, 195, 255), 1.15f, 0.18f);
                }
            }
        }

        private bool TryBlockExpansionItemHit(float amount, CombatTarget? attacker)
        {
            // Неуязвимость 2.2с после срабатывания Тотема Бессмертия
            if (_totemInvulnTimer > 0f)
            {
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.35f, 0), Color.Gold, 1.9f, 0.18f);
                return true;
            }

            // Святая Мантия [Isaac]: полностью блокирует 1 удар раз в 18 сек. (-20% КД за стак)
            int mantle = CombatManager.GetItemCount("holy_mantle");
            if (mantle > 0 && _holyMantleCd <= 0f)
            {
                _holyMantleCd = MathF.Max(3.5f, 18.0f * MathF.Pow(0.80f, mantle - 1));
                SoundManager.Play("parry", 0.90f, 1.28f);
                Run.StageEntities.Add(new HolyMantleBurstFX(Run.PlayerPos + new Vector3(0, 1.55f, 0)));
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.35f, 0), Rgb(135, 215, 255), 3.4f, 0.30f);
                Run.ShowBanner("СВЯТАЯ МАНТИЯ ОТРАЗИЛА УДАР!", Rgb(135, 215, 255), 1.2f);
                return true;
            }

            return false;
        }

        private float ModifyExpansionItemIncomingDamage(float dmg, CombatTarget? attacker)
        {
            // Посох Чёрного Короля (Аватар): снижает входящий урон на 50%
            if (_bkbAvatarTimer > 0f)
                dmg *= 0.50f;

            // Чистый Ориджиниум [Arknights]: проклятие повышает входящий урон на +25% за стак
            int originium = CombatManager.GetItemCount("originium_prime");
            if (originium > 0)
                dmg *= MathF.Pow(1.25f, originium);

            // Кластер Орирока [Arknights]: снижает весь входящий урон на 3 ед. за стак (мин. 1 ед.)
            int orirock = CombatManager.GetItemCount("orirock_cluster");
            if (orirock > 0)
                dmg = MathF.Max(1.0f, dmg - 3.0f * orirock);

            return dmg;
        }

        private void OnExpansionPostHpDamage(float dmg, CombatTarget? attacker)
        {
            SyncExpansionStageState();

            if (dmg > 0f && Run.PlayerHp > 0f && Run.PlayerHp < Run.PlayerMaxHp * 0.35f)
                Run.UnlockAchievement("ach_item_golden_apple");

            // Золотое Яблоко: мгновенно выдаёт Золотой Щит при падении HP ниже 50%
            TryProcGoldenApple();

            // Ихор Морских Рождений [Arknights]: шипы щупалец бьют атакующего и врагов в радиусе 9м (220% ур.)
            int ichor = CombatManager.GetItemCount("seaborne_ichor");
            if (ichor > 0 && _ichorRetaliateCd <= 0f)
            {
                _ichorRetaliateCd = 0.35f;
                Vector3 myP = Run.PlayerPos + new Vector3(0, 1.15f, 0);
                float spikeDmg = BaseDamage * (2.20f * ichor);
                bool lashedAny = false;

                if (IsValid(attacker) && !attacker!.IsAlly)
                {
                    lashedAny = true;
                    Vector3 aHit = attacker.Position + new Vector3(0, attacker.HitHeight * 0.7f, 0);
                    new LightningBeam(myP, aHit, Rgb(85, 155, 255), 0.24f);
                    new VisualFX(aHit, Rgb(115, 230, 255), 2.3f, 0.24f);
                    attacker.SlowTimer = MathF.Max(attacker.SlowTimer, 1.6f);
                    attacker.TakeDamage(spikeDmg);
                }

                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly || e == attacker) continue;
                    if (Vector3.Distance(e.Position, Run.PlayerPos) <= 9.0f)
                    {
                        lashedAny = true;
                        Vector3 eHit = e.Position + new Vector3(0, e.HitHeight * 0.7f, 0);
                        new LightningBeam(myP, eHit, Rgb(85, 155, 255), 0.20f);
                        new VisualFX(eHit, Rgb(115, 230, 255), 2.0f, 0.22f);
                        e.SlowTimer = MathF.Max(e.SlowTimer, 1.4f);
                        e.TakeDamage(spikeDmg);
                    }
                }

                if (lashedAny)
                    SoundManager.Play("melee_swing", 0.72f, 0.85f);
            }

            // Дамоклов Меч [Isaac]: удар по HP (>1 ед.) с шансом 3% (не более 1 раза за этап) обрушивает меч, оставляя 1 HP!
            int damocles = CombatManager.GetItemCount("damocles");
            if (damocles > 0 && !_damoclesTriggeredThisStage && dmg > 1.0f && Run.PlayerHp > 1.0f)
            {
                if (Random.Shared.NextDouble() < 0.03)
                {
                    _damoclesTriggeredThisStage = true;
                    Run.PlayerHp = 1.0f;

                    SoundManager.Play("mithrix_slam", 1.0f, 1.18f);
                    SoundManager.Play("scissor_snip", 0.95f, 0.72f);
                    Run.StageEntities.Add(new DamoclesFallFX(Run.PlayerPos));
                    Run.ShowBanner("ДАМОКЛОВ МЕЧ ОБРУШИЛСЯ! (ОСТАЛОСЬ 1 HP!)", Rgb(195, 225, 255), 3.4f);

                    float cleaveDmg = BaseDamage * 10.0f;
                    foreach (var e in CombatManager.Enemies.ToArray())
                    {
                        if (!IsValid(e) || e.IsAlly) continue;
                        if (Vector3.Distance(e.Position, Run.PlayerPos) <= 14.0f)
                        {
                            new VisualFX(e.Position + new Vector3(0, e.HitHeight * 0.75f, 0), Rgb(195, 225, 255), 3.2f, 0.28f);
                            e.TakeDamage(cleaveDmg);
                        }
                    }
                }
            }
        }

        private bool TryPreventFatalDeathExpansion()
        {
            SyncExpansionStageState();

            // Тотем Бессмертия [Minecraft]: спасает от гибели 1 раз за этап на стак
            int totem = CombatManager.GetItemCount("totem_undying");
            if (totem > 0 && _totemChargesUsed < totem)
            {
                _totemChargesUsed++;
                Run.PlayerHp = MathF.Max(1.0f, Run.PlayerMaxHp * 0.50f);
                GrantExpansionShield(Run.PlayerMaxHp * 0.50f);
                _totemInvulnTimer = MathF.Max(_totemInvulnTimer, 2.2f);
                SkillLockTimer = 0f;

                SoundManager.Play("victory", 0.95f, 1.16f);
                SoundManager.Play("explosion", 0.85f, 1.08f);
                Run.StageEntities.Add(new TotemUndyingBurstFX(Run.PlayerPos));
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.4f, 0), Color.Gold, 9.5f, 0.45f);
                new VisualFX(Run.PlayerPos + new Vector3(0, 1.2f, 0), Rgb(65, 255, 135), 6.8f, 0.38f);

                float blastDmg = BaseDamage * 4.0f;
                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly) continue;
                    if (Vector3.Distance(e.Position, Run.PlayerPos) <= 15.0f)
                    {
                        if (!e.IsBoss) e.StunTimer = MathF.Max(e.StunTimer, 2.5f);
                        new VisualFX(e.Position + new Vector3(0, e.HitHeight * 0.7f, 0), Color.Gold, 2.8f, 0.28f);
                        e.TakeDamage(blastDmg);
                    }
                }

                int left = totem - _totemChargesUsed;
                string remTag = left > 0 ? $" (ЕЩЁ {left} НА ЭТАП)" : "";
                Run.ShowBanner($"ТОТЕМ БЕССМЕРТИЯ СПАС ВАС ОТ ГИБЕЛИ!{remTag}", Color.Gold, 3.6f);
                return true;
            }

            return false;
        }
    }

    public static partial class CombatManager
    {
        private static readonly List<SculkBloomZoneEntity> _activeSculkZones = new();

        private static void ApplyExpansionItemHitEffects(
            SurvivorCombatController p, IPlayerContext g, CombatTarget target,
            Vector3 hitPos, float dmg, bool isCrit, float luckBonus)
        {
            // Опустошитель Бездны [Void / Dota 2]: накладывает Порчу Бездны (+8% получаемого урона за заряд, до 5 * стак)
            int desol = GetItemCount("desolator_void");
            if (desol > 0 && IsValid(target))
            {
                target.CorruptionStacks = Math.Min(5 * desol, target.CorruptionStacks + 1);
                target.CorruptionTimer = MathF.Max(target.CorruptionTimer, 6.0f);
                new VisualFX(hitPos, Rgb(235, 55, 115), 1.35f, 0.16f);
            }

            // Грустная Луковица [Isaac]: брызги едких слёз при попадании (25% шанс с удачей или гарантированно каждый 4-й удар)
            int onion = GetItemCount("sad_onion");
            if (onion > 0)
            {
                p._sadOnionHitCounter++;
                if (p._sadOnionHitCounter >= 4 || _rng.NextDouble() < MathF.Min(0.80f, 0.25f * luckBonus))
                {
                    p._sadOnionHitCounter = 0;
                    float tearRad = 3.5f + 0.35f * (onion - 1);
                    float tearDmg = p.BaseDamage * (0.45f * onion);

                    new VisualFX(hitPos, Rgb(95, 215, 255), 2.6f, 0.24f);
                    for (int i = 0; i < 3; i++)
                    {
                        float a = (float)(_rng.NextDouble() * Math.PI * 2.0);
                        Vector3 dropOff = new(MathF.Cos(a) * 0.95f, 0.35f, MathF.Sin(a) * 0.95f);
                        new VisualFX(hitPos + dropOff, Rgb(135, 235, 255), 0.95f, 0.18f);
                    }

                    foreach (var other in Enemies.ToArray())
                    {
                        if (IsValid(other) && !other.IsAlly && Vector3.Distance(other.Position + new Vector3(0, other.HitHeight * 0.5f, 0), hitPos) <= tearRad)
                            other.TakeDamage(tearDmg);
                    }
                }
            }

            // Маэлстром [Dota 2]: 25% шанс выпустить синюю цепную молнию в 4 (+2 за стак) врагов (140% ур.)
            int maels = GetItemCount("maelstrom");
            if (maels > 0 && _rng.NextDouble() < MathF.Min(0.85f, 0.25f * luckBonus))
            {
                int maxTargets = 4 + 2 * (maels - 1);
                float zapDmg = p.BaseDamage * 1.40f;
                Color zapCol = Rgb(75, 215, 255);

                SoundManager.Play3D("zap", hitPos, g.PlayerPos, 60f, 0.82f, 1.12f);

                var visited = new HashSet<CombatTarget>();
                Vector3 prevPos = g.PlayerPos + new Vector3(0, 1.35f, 0);

                if (IsValid(target) && !target.IsAlly)
                {
                    visited.Add(target);
                    Vector3 firstHit = target.Position + new Vector3(0, target.HitHeight * 0.7f, 0);
                    new LightningBeam(prevPos, firstHit, zapCol, 0.22f);
                    new VisualFX(firstHit, zapCol, 1.9f, 0.20f);
                    target.TakeDamage(zapDmg);
                    prevPos = firstHit;
                }

                while (visited.Count < maxTargets)
                {
                    CombatTarget? next = null;
                    float bestDist = 18.0f;
                    foreach (var cand in Enemies)
                    {
                        if (!IsValid(cand) || cand.IsAlly || visited.Contains(cand)) continue;
                        float d = Vector3.Distance(prevPos, cand.Position + new Vector3(0, cand.HitHeight * 0.6f, 0));
                        if (d <= bestDist)
                        {
                            bestDist = d;
                            next = cand;
                        }
                    }

                    if (next == null) break;
                    visited.Add(next);
                    Vector3 nextPos = next.Position + new Vector3(0, next.HitHeight * 0.7f, 0);
                    new LightningBeam(prevPos, nextPos, zapCol, 0.20f);
                    new VisualFX(nextPos, zapCol, 1.85f, 0.20f);
                    next.TakeDamage(zapDmg);
                    prevPos = nextPos;
                }
            }
        }

        private static void ApplyExpansionItemKillEffects(
            SurvivorCombatController p, IPlayerContext g, CombatTarget dead, Vector3 deathPos)
        {
            // Скалк-Катализатор [Void / Minecraft]: убийства дают +8 Щита и создают зону Скалка (замедляет + 95% ур./с)
            int sculk = GetItemCount("sculk_catalyst");
            if (sculk > 0)
            {
                p.GrantExpansionShield(8.0f * sculk);

                _activeSculkZones.RemoveAll(z => !IsValid(z));
                while (_activeSculkZones.Count >= 10)
                {
                    Destroy(_activeSculkZones[0]);
                    _activeSculkZones.RemoveAt(0);
                }

                float floorY = Physics.GetSurfaceHeight(deathPos.X, deathPos.Z, deathPos.Y + 1.0f);
                float radius = 5.5f + 1.0f * (sculk - 1);
                float dps = p.BaseDamage * (0.95f * sculk);

                var zone = new SculkBloomZoneEntity(new Vector3(deathPos.X, floorY + 0.05f, deathPos.Z), radius, dps, 5.0f);
                _activeSculkZones.Add(zone);
                g.StageEntities.Add(zone);
                SoundManager.Play3D("magic_cast", deathPos, g.PlayerPos, 55f, 0.55f, 0.66f);
            }
        }
    }

    // =========================================================================================
    // ЗОНЫ И ВИЗУАЛЬНЫЕ 3D-ЭФФЕКТЫ ПРЕДМЕТОВ РАСШИРЕНИЯ
    // =========================================================================================

    public class SculkBloomZoneEntity : Entity
    {
        public readonly float Radius;
        public readonly float Dps;
        public float Lifetime;
        private float _tickTimer = 0.15f;
        private readonly Entity _soulOrb;
        private readonly Entity _outerRing;

        public SculkBloomZoneEntity(Vector3 pos, float radius, float dps, float duration = 5.0f)
            : base(position: pos)
        {
            Radius = radius;
            Dps = dps;
            Lifetime = duration;

            // Тёмно-синий ковёр Скалка и бирюзовая сенсорная кромка
            new Entity(parent: this, model: "sphere", scale: new(radius * 2.0f, 0.08f, radius * 2.0f),
                color: Rgb(12, 32, 46, 205), unlit: true);
            _outerRing = new Entity(parent: this, model: "diamond", scale: new(radius * 2.04f, 0.06f, radius * 2.04f),
                color: Rgb(35, 215, 225, 130), unlit: true);

            // Миниатюрный блок Скалк-Катализатора в центре зоны
            new Entity(parent: this, model: "cube", scale: new(0.46f, 0.16f, 0.46f), position: new(0, 0.08f, 0), color: Rgb(218, 222, 204));
            new Entity(parent: this, model: "cube", scale: new(0.44f, 0.24f, 0.44f), position: new(0, 0.26f, 0), color: Rgb(14, 36, 50));
            _soulOrb = new Entity(parent: this, model: "diamond", scale: new(0.24f, 0.38f, 0.24f),
                position: new(0, 0.58f, 0), color: Rgb(65, 245, 255), unlit: true);

            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.PI / 3f;
                float r = radius * 0.62f;
                new Entity(parent: this, model: "diamond", scale: new(0.22f, 0.32f, 0.22f),
                    position: new(MathF.Cos(a) * r, 0.18f, MathF.Sin(a) * r), color: Rgb(28, 215, 228), unlit: true);
            }
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            Lifetime -= dt;
            if (Lifetime <= 0f)
            {
                Destroy(this);
                return;
            }

            if (Lifetime < 0.40f)
            {
                float s = MathF.Max(0.05f, Lifetime / 0.40f);
                Scale = new Vector3(s, 1f, s);
            }

            _outerRing.RotationY += 42f * dt;
            _soulOrb.RotationY -= 120f * dt;
            _soulOrb.Y = 0.58f + MathF.Sin((5.0f - Lifetime) * 5.0f) * 0.12f;

            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                const float interval = 0.50f;
                _tickTimer = interval;
                float tickDmg = Dps * interval;

                foreach (var e in CombatManager.Enemies.ToArray())
                {
                    if (!IsValid(e) || e.IsAlly) continue;
                    if (Physics.Distance2D(Position, e.Position) <= Radius && MathF.Abs(e.Y - Y) <= 4.5f)
                    {
                        e.SlowTimer = MathF.Max(e.SlowTimer, 0.90f);
                        new VisualFX(e.Position + new Vector3(0, e.HitHeight * 0.55f, 0), Rgb(45, 235, 245), 1.45f, 0.18f);
                        e.TakeDamage(tickDmg);
                    }
                }
            }
        }
    }

    internal class HolyMantleBurstFX : Entity
    {
        private float _life = 0.45f;

        public HolyMantleBurstFX(Vector3 pos)
            : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(0.28f, 1.35f, 0.14f), color: Rgb(115, 210, 255), unlit: true);
            new Entity(parent: this, model: "cube", scale: new(1.35f, 0.28f, 0.14f), color: Rgb(115, 210, 255), unlit: true);
            new Entity(parent: this, model: "cube", scale: new(0.12f, 1.05f, 0.18f), color: Color.White, unlit: true);
            new Entity(parent: this, model: "cube", scale: new(1.05f, 0.12f, 0.18f), color: Color.White, unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(0.75f, 0.10f, 0.75f), position: new(0, 0.85f, 0), color: Color.Gold, unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            Y += 1.6f * dt;
            RotationY += 180f * dt;
            float s = MathF.Max(0.05f, _life / 0.45f);
            Scale = new Vector3(1.25f - s * 0.25f, s * 1.15f, 1.25f - s * 0.25f);
            if (_life <= 0f) Destroy(this);
        }
    }

    internal class TruesilverSlashWaveFX : Entity
    {
        private readonly Vector3 _dir;
        private float _life = 0.36f;

        public TruesilverSlashWaveFX(Vector3 startPos, Vector3 forwardDir)
            : base(position: startPos)
        {
            _dir = forwardDir;
            LookAt2D(startPos + forwardDir);

            // 3 широких полумесяца Истинного Серебра
            for (int i = -1; i <= 1; i++)
            {
                float yawOff = i * 22f;
                var arc = new Entity(parent: this, position: new(i * 0.65f, i * 0.12f, 0.8f), rotation: new(0, yawOff, i * -8f));
                new Entity(parent: arc, model: "diamond", scale: new(3.2f, 0.16f, 0.85f), color: Color.White, unlit: true);
                new Entity(parent: arc, model: "diamond", scale: new(3.6f, 0.08f, 1.10f), color: Rgb(165, 220, 255, 175), unlit: true);
            }
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            Position += _dir * (38.0f * dt);
            Scale += new Vector3(3.2f * dt, 0f, 1.8f * dt);
            if (_life <= 0f) Destroy(this);
        }
    }

    internal class MawOfVoidPulseFX : Entity
    {
        private readonly float _targetDiameter;
        private float _elapsed = 0f;
        private const float Duration = 0.36f;

        public MawOfVoidPulseFX(Vector3 center, float radius)
            : base(position: center)
        {
            _targetDiameter = radius * 2.0f;
            new Entity(parent: this, model: "sphere", scale: new(1.0f, 0.07f, 1.0f), color: Rgb(16, 6, 24, 215), unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(1.04f, 0.11f, 1.04f), color: Rgb(235, 32, 78, 195), unlit: true);
            new Entity(parent: this, model: "diamond", scale: new(0.86f, 0.14f, 0.86f), rotation: new(0, 45, 0), color: Rgb(255, 95, 130, 155), unlit: true);
            Scale = new Vector3(0.5f, 1f, 0.5f);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _elapsed += dt;
            float t = Math.Clamp(_elapsed / Duration, 0f, 1f);
            float d = Lerp(1.2f, _targetDiameter, t);
            Scale = new Vector3(d, MathF.Max(0.1f, 1.0f - t * 0.85f), d);
            RotationY += 210f * dt;
            if (_elapsed >= Duration) Destroy(this);
        }
    }

    internal class TotemUndyingBurstFX : Entity
    {
        private float _life = 0.75f;

        public TotemUndyingBurstFX(Vector3 playerPos)
            : base(position: playerPos + new Vector3(0, 1.5f, 0))
        {
            new Entity(parent: this, model: "cube", scale: new(0.52f, 0.68f, 0.32f), color: Rgb(250, 208, 55), unlit: true);
            new Entity(parent: this, model: "cube", scale: new(1.35f, 0.24f, 0.22f), position: new(0, 0.10f, 0), color: Color.Gold, unlit: true);
            new Entity(parent: this, model: "cube", scale: new(0.58f, 0.52f, 0.44f), position: new(0, 0.56f, 0), color: Rgb(255, 220, 72), unlit: true);
            foreach (float sx in new[] { -0.16f, 0.16f })
                new Entity(parent: this, model: "cube", scale: new(0.14f, 0.12f, 0.10f), position: new(sx, 0.62f, 0.24f), color: Rgb(55, 255, 125), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _life -= dt;
            Y += 2.2f * dt;
            RotationY += 320f * dt;
            float s = Math.Clamp(_life / 0.75f, 0.05f, 1f);
            Scale = new Vector3(s * 1.25f);
            if (_life <= 0f) Destroy(this);
        }
    }

    internal class DamoclesFallFX : Entity
    {
        private readonly Entity _sword;
        private readonly Vector3 _groundPos;
        private float _elapsed = 0f;
        private bool _impacted = false;

        public DamoclesFallFX(Vector3 playerPos)
            : base(position: playerPos)
        {
            _groundPos = playerPos;
            _sword = new Entity(parent: this, position: new(0, 12.5f, 0));
            // Крупный падающий клинок Дамоклова Меча остриём вниз
            new Entity(parent: _sword, model: "diamond", scale: new(0.45f, 0.45f, 0.45f), position: new(0, 3.8f, 0), color: Rgb(195, 225, 255), unlit: true);
            new Entity(parent: _sword, model: "cube", scale: new(0.22f, 0.65f, 0.22f), position: new(0, 3.25f, 0), color: Rgb(52, 74, 118));
            new Entity(parent: _sword, model: "diamond", scale: new(1.65f, 0.32f, 0.40f), position: new(0, 2.85f, 0), color: Rgb(215, 238, 255), unlit: true);
            new Entity(parent: _sword, model: "diamond", scale: new(0.68f, 2.85f, 0.22f), position: new(0, 1.35f, 0), color: Color.White, unlit: true);
            new Entity(parent: _sword, model: "diamond", scale: new(0.84f, 2.70f, 0.14f), position: new(0, 1.40f, 0), color: Rgb(140, 205, 255), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            _elapsed += dt;

            if (_elapsed < 0.22f)
            {
                float t = _elapsed / 0.22f;
                _sword.Y = Lerp(12.5f, 0.1f, t * t);
            }
            else
            {
                _sword.Y = 0.1f;
                if (!_impacted)
                {
                    _impacted = true;
                    new VisualFX(_groundPos + new Vector3(0, 0.8f, 0), Rgb(195, 228, 255), 11.5f, 0.42f);
                    new VisualFX(_groundPos + new Vector3(0, 1.2f, 0), Color.White, 6.5f, 0.30f);
                }

                if (_elapsed > 1.15f)
                {
                    float fade = Math.Clamp((1.55f - _elapsed) / 0.40f, 0.02f, 1f);
                    _sword.Scale = new Vector3(fade, 1f, fade);
                }
                if (_elapsed >= 1.55f)
                    Destroy(this);
            }
        }
    }
}
