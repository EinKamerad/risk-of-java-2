using System;
using System.Numerics;
using Raylib_cs;
using System.Collections.Generic;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using RiskOfCSharp2.Entities;
using RiskOfCSharp2.Models;
using RiskOfCSharp2.World;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2
{
    public partial class GameRunContext : IPlayerContext
    {
        public Entity PlayerEntity = null!;
        public SurvivorParts Parts = null!;
        public SurvivorCombatController Combat = null!;
        public string CharKey = "commando";
        public float CamPitch = 10f;
        public Vector3 DashVector = Vector3.Zero;
        public float DashTimer = 0f;
        public float ShiftCd = 0f;

        public Vector3 PlayerPos { get => PlayerEntity.Position; set => PlayerEntity.Position = value; }
        public Vector3 PlayerForward => PlayerEntity.Forward;
        public float PlayerVelY { get; set; }
        public bool PlayerGrounded { get; set; } = true;
        public int PlayerJumpsLeft { get; set; } = 1;
        public int PlayerMaxJumps => 1 + (Items.TryGetValue("feather", out int f) ? f : 0) + (Items.TryGetValue("elytra_curse", out int elJ) ? elJ * 2 : 0);
        public float PlayerHp { get; set; } = 115f;
        public float PlayerMaxHp
        {
            get
            {
                int baseHp = SurvivorDatabase.Classes[CharKey].Hp;
                int steak = Items.TryGetValue("bison_steak", out int s) ? s * 25 : 0;
                int doran = Items.TryGetValue("dorans_blade", out int db) ? db * 20 : 0;
                int tarr = Items.TryGetValue("tarrasque", out int t) ? t * 100 : 0;
                int branch = Items.TryGetValue("iron_branch", out int ib) ? ib * 12 : 0;
                int gApple = Items.TryGetValue("golden_apple", out int ga) ? ga * 20 : 0;
                int orirock = Items.TryGetValue("orirock_cluster", out int oc) ? oc * 18 : 0;
                float raw = baseHp + steak + doran + tarr + branch + gApple + orirock
                            + (Combat != null ? Combat.InfusionBonus + Combat.ExpansionBonusMaxHp + Combat.DonLanternDevourBonusHp : 0f);
                float transMult = 1.0f + 0.50f * (Items.TryGetValue("transcendence", out int tr) ? tr : 0);
                float fluxMult = MathF.Pow(2.0f, Items.TryGetValue("lunar_pauldron", out int fl) ? fl : 0);
                float tonicMult = (1.0f + 0.25f * (Items.TryGetValue("spinel_tonic", out int tn) ? tn : 0))
                                  * MathF.Pow(0.88f, Items.TryGetValue("tonic_affliction", out int ta) ? ta : 0);
                float elytraMult = MathF.Pow(0.80f, Items.TryGetValue("elytra_curse", out int el) ? el : 0);
                float glassDiv = MathF.Pow(0.5f, Items.TryGetValue("shaped_glass", out int gl) ? gl : 0) * (GameData.HasArtifact("glass") ? 0.20f : 1.0f);
                return MathF.Max(1f, MathF.Floor(raw * transMult * fluxMult * tonicMult * elytraMult * glassDiv));
            }
        }
        public int Gold { get; set; } = 15;
        public int LunarCoins { get; set; } = 2;
        public bool BlueOrbSpawned { get; set; } = false;
        public CombatTarget? ActiveBoss;
        public string BossTitle = "";
        public readonly List<CombatTarget> ActiveHordeBosses = new();
        public float HordeTotalMaxHp = 0f;
        public string HordeTitle = "";
        public bool IsHordeActive
        {
            get
            {
                for (int i = 0; i < ActiveHordeBosses.Count; i++)
                {
                    var hb = ActiveHordeBosses[i];
                    if (IsValid(hb) && hb.Hp > 0f && !hb.IsAlly) return true;
                }
                return false;
            }
        }
        public bool IsBossAlive => (IsValid(ActiveBoss) && !ActiveBoss!.IsAlly) || IsHordeActive;
        public bool CanPlayerInteract => !Program.IsSpectating && PlayerHp > 0f && !(CharKey == "mindustry" && Combat != null && Combat.MindustryBuildMode) && !Program.HasAnyInGameModalOpen();
        public float MoonDetonationTimer { get; set; } = 0f;
        public bool MithrixSpawned = false;

        public float ElapsedTime = 0f;
        public float StageElapsedTime = 0f;
        public float DirectorCredits = 0f;
        public float SpawnTimer = 1.0f;
        public string StageKinType = "";
        public float VengeanceTimer = 85.0f;

        // Счётчики прогресса достижений за текущий забег
        public int M1CastCount = 0;
        public int M2CastCount = 0;
        public int ShiftCastCount = 0;
        public int RCastCount = 0;
        public int TotalAbilityCasts => M1CastCount + M2CastCount + ShiftCastCount + RCastCount;
        public float MoveStreakTimer = 0f;
        public bool DroppedLowHp = false;
        public bool SurvivedCriticalHp = false;
        public int HitsTakenCount = 0;
        public float MultiKillWindowTimer = 0f;
        public int MultiKillCount = 0;
        public float FrenzyWindowTimer = 0f;
        public int FrenzyKillCount = 0;
        public float UntouchedCombatTimer = 0f;
        public int CloseKills = 0;
        public int EliteOrBossKills = 0;
        public int ChestsOpened = 0;
        public int PyroKills = 0;
        public int CritHitsCount = 0;
        public float TeleporterActiveTimer = 0f;
        public bool TeleporterChargingNow = false;
        public int AirborneKills = 0;
        public float PostHitTimer = 0f;
        public int ThornsKills = 0;
        public float TotalHealed = 0f;

        public Dictionary<string, int> Items { get; } = new();
        public List<string> ItemOrder { get; } = new();
        public List<Entity> StageEntities { get; } = new();

        public int Stage = 1;
        public string StageName = "";
        public int MoonPillarsDone = 0;
        public Entity? ActiveTeleporterOrPad;

        public float DifficultyCoeff
        {
            get
            {
                float minutes = ElapsedTime / 60.0f;
                var diff = GameData.GetActiveDifficulty();
                float scaleM = diff.ScaleMult;
                return (1.0f + diff.StartCoeffBonus + 0.27f * minutes * scaleM) * MathF.Pow(1.0f + 0.30f * scaleM, Stage - 1);
            }
        }

        public (string Label, Color Col) GetDifficultyInfo()
        {
            float c = DifficultyCoeff;
            if (c < 1.3f) return ("ЛЕГКО", Rgb(80, 225, 100));
            if (c < 1.8f) return ("СРЕДНЕ", Rgb(255, 210, 60));
            if (c < 2.5f) return ("СЛОЖНО", Rgb(255, 130, 40));
            if (c < 3.5f) return ("ОЧЕНЬ СЛОЖНО", Rgb(245, 55, 55));
            return ("БЕЗУМИЕ (ХА-ХА-ХА)", Rgb(210, 50, 255));
        }

        public Vector3 GetNearestPlayerPos(Vector3 fromPos, out bool isLocalPlayer)
        {
            return Program.GetNearestPlayerPos(fromPos, out isLocalPlayer);
        }

        public bool IsNearRemotePlayer(Vector3 pos, float radius)
        {
            return Program.IsNearRemotePlayer(pos, radius);
        }

        public void NotifyEnemyDamaged(int netId, float amount, float currentHp, int phase)
        {
            if (!Net.NetworkManager.Instance.Connected) return;
            Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "enemy_hit",
                ["id"] = netId,
                ["dmg"] = MathF.Round(amount, 2),
                ["hp"] = MathF.Round(currentHp, 2),
                ["phase"] = phase
            });
        }

        public void NotifyEnemyKilled(int netId)
        {
            if (!Net.NetworkManager.Instance.Connected) return;
            Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "enemy_kill",
                ["id"] = netId
            });
        }

        public void NotifyTeleporterState(string action, bool alignedToMoon)
        {
            if (!Net.NetworkManager.Instance.Connected) return;
            Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "tp_state",
                ["action"] = action,
                ["aligned"] = alignedToMoon
            });
        }

        public void NotifyChestOpened(int chestId, string itemKey)
        {
            if (!Net.NetworkManager.Instance.Connected) return;
            Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "chest_open",
                ["id"] = chestId,
                ["item"] = itemKey
            });
        }

        public void NotifyMoonPillarState(string pillarType, string state)
        {
            if (!Net.NetworkManager.Instance.Connected) return;
            Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "pillar_state",
                ["ptype"] = pillarType,
                ["state"] = state
            });
        }

        public void UnlockAchievement(string achKey)
        {
            if (!GameData.Achievements.TryGetValue(achKey, out var ach)) return;

            var prog = ProgressData.Instance;
            var unlockedRewards = new List<string>();

            bool newlyUnlockedHero = false;
            bool isNewAch = !prog.UnlockedAchievements.Contains(achKey);
            // 1. Глобальное достижение, предмет и персонаж для всех забегов
            if (isNewAch)
            {
                prog.UnlockedAchievements.Add(achKey);
                if (achKey == "ach_safe_travels")
                    prog.MithrixDefeated = true;
                if (!string.IsNullOrEmpty(ach.UnlocksItem) && GameData.ItemPool.TryGetValue(ach.UnlocksItem, out var itemInfo))
                {
                    prog.UnlockedItems.Add(ach.UnlocksItem);
                    unlockedRewards.Add($"Предмет «{itemInfo.Name}»");
                }
                if (!string.IsNullOrEmpty(ach.UnlocksCharacter) && SurvivorDatabase.Classes.TryGetValue(ach.UnlocksCharacter, out var charCfg))
                {
                    if (!prog.UnlockedCharacters.Contains(ach.UnlocksCharacter))
                        newlyUnlockedHero = true;
                    prog.UnlockedCharacters.Add(ach.UnlocksCharacter);
                    unlockedRewards.Add($"ВЫЖИВШИЙ «{charCfg.Name}»");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(ach.UnlocksItem) && !prog.UnlockedItems.Contains(ach.UnlocksItem) && GameData.ItemPool.TryGetValue(ach.UnlocksItem, out var itemInfo))
                {
                    prog.UnlockedItems.Add(ach.UnlocksItem);
                    unlockedRewards.Add($"Предмет «{itemInfo.Name}»");
                }
                if (!string.IsNullOrEmpty(ach.UnlocksCharacter) && !prog.UnlockedCharacters.Contains(ach.UnlocksCharacter) && SurvivorDatabase.Classes.TryGetValue(ach.UnlocksCharacter, out var charCfg))
                {
                    newlyUnlockedHero = true;
                    prog.UnlockedCharacters.Add(ach.UnlocksCharacter);
                    unlockedRewards.Add($"ВЫЖИВШИЙ «{charCfg.Name}»");
                }
            }

            // 2. Персональные альтернативные навыки и скины только для текущего выжившего (CharKey и его активной альт-версии скина)
            if (SurvivorDatabase.Classes.TryGetValue(CharKey, out var cfg))
            {
                string profileKey = prog.GetLoadoutProfileKey(CharKey);
                var activeSkin = cfg.GetActiveSkin();
                var activeVariantsMap = activeSkin.CustomSkillVariants ?? cfg.SkillVariants;
                string ownerTitle = activeSkin.CustomSkillVariants != null ? activeSkin.Name : cfg.Name;

                foreach (var slot in new[] { "m1", "m2", "shift", "r" })
                {
                    if (!activeVariantsMap.TryGetValue(slot, out var variants)) continue;
                    for (int vi = 1; vi < variants.Count; vi++)
                    {
                        var vSkill = variants[vi];
                        string skillUnlockId = $"{profileKey}_{slot}_{vi}";
                        if (vSkill.UnlockAchKey == achKey && !prog.UnlockedSkills.Contains(skillUnlockId))
                        {
                            prog.UnlockedSkills.Add(skillUnlockId);
                            unlockedRewards.Add($"Навык [{ownerTitle}] «{vSkill.Name}»");
                        }
                    }
                }

                for (int si = 1; si < cfg.Skins.Count; si++)
                {
                    var skin = cfg.Skins[si];
                    string skinUnlockId = $"{CharKey}_{si}";
                    if (skin.UnlockAchKey == achKey && !prog.UnlockedSkins.Contains(skinUnlockId))
                    {
                        prog.UnlockedSkins.Add(skinUnlockId);
                        string kindLabel = skin.CustomSkillVariants != null || achKey.StartsWith("ach_persona_") ? "Альт. личность" : "Скин";
                        unlockedRewards.Add($"{kindLabel} [{cfg.Name}] «{skin.Name}»");
                    }
                }
            }

            if (achKey.StartsWith("ach_persona_"))
            {
                foreach (var (heroKey, heroCfg) in SurvivorDatabase.Classes)
                {
                    for (int si = 1; si < heroCfg.Skins.Count; si++)
                    {
                        var skin = heroCfg.Skins[si];
                        string skinUnlockId = $"{heroKey}_{si}";
                        if (skin.UnlockAchKey == achKey && !prog.UnlockedSkins.Contains(skinUnlockId))
                        {
                            prog.UnlockedSkins.Add(skinUnlockId);
                            unlockedRewards.Add($"Альт. личность [{heroCfg.Name}] «{skin.Name}»");
                        }
                    }
                }
            }

            if (isNewAch && unlockedRewards.Count == 0)
            {
                unlockedRewards.Add(!string.IsNullOrEmpty(ach.RewardText) ? ach.RewardText : ach.Name);
            }

            if (unlockedRewards.Count > 0)
            {
                prog.Save();
                SoundManager.Play("victory", 0.9f);
                ShowBanner($"ДОСТИЖЕНИЕ «{ach.Name}» :: ОТКРЫТО: {string.Join(" + ", unlockedRewards)}!", Color.Gold, 4.5f);
                if (newlyUnlockedHero && !string.IsNullOrEmpty(ach.UnlocksCharacter))
                    TriggerCharacterUnlockCutscene(ach.UnlocksCharacter);
            }
        }

        public void HealPlayer(float amount)
        {
            if (amount <= 0f || Program.IsSpectating || PlayerHp <= 0f) return;
            if (GameData.SelectedDifficulty == "eclipse") amount *= 0.65f;
            TotalHealed += amount;
            if (TotalHealed >= 250f) UnlockAchievement("ach_overheal");
            float maxH = PlayerMaxHp;
            float newHp = PlayerHp + amount;
            if (newHp > maxH)
            {
                float overflow = newHp - maxH;
                PlayerHp = maxH;
                int aegis = Items.TryGetValue("aegis", out int a) ? a : 0;
                if (aegis > 0 && Combat != null)
                {
                    float gain = overflow * MathF.Min(1.0f, 0.5f * aegis);
                    Combat.Shield = MathF.Min(maxH * aegis, Combat.Shield + gain);
                }
            }
            else PlayerHp = newHp;
            if (DroppedLowHp && PlayerHp >= maxH * 0.85f)
            {
                UnlockAchievement("ach_field_medic");
                UnlockAchievement("ach_item_golden_apple");
            }
        }

        public void ApplyDash(Vector3 dashVec, float duration)
        {
            DashVector = dashVec;
            DashTimer = duration;
        }

        public void CollectItem(string itemKey)
        {
            if (itemKey == "spinel_tonic")
            {
                int spoiled = Items.TryGetValue("tonic_affliction", out int sa) ? sa : 0;
                if (spoiled > 0)
                {
                    Items["tonic_affliction"] = 0;
                    ItemOrder.Remove("tonic_affliction");
                    Items["spinel_tonic"] = (Items.TryGetValue("spinel_tonic", out int st) ? st : 0) + spoiled;
                }
                if (!ItemOrder.Contains("spinel_tonic")) ItemOrder.Add("spinel_tonic");
                Items["spinel_tonic"] = (Items.TryGetValue("spinel_tonic", out int curT) ? curT : 0) + 1;
                if (Combat != null) Combat.TonicBuffTimer = 25.0f;
                HealPlayer(PlayerMaxHp * 0.25f);
                SoundManager.Play("item_pickup");
                ShowBanner($"ЛУННЫЙ ТОНИК АКТИВЕН (25 СЕК)! ВСЕ ИСПОРЧЕННЫЕ ТОНИКИ ВОССТАНОВЛЕНЫ (x{Items["spinel_tonic"]})!", Rgb(65, 195, 255), 3.8f);
                UnlockAchievement("ach_lunar_addict");
            }
            else
            {
                if (!ItemOrder.Contains(itemKey)) ItemOrder.Add(itemKey);
                Items[itemKey] = (Items.TryGetValue(itemKey, out int c) ? c : 0) + 1;
                var info = GameData.ItemPool[itemKey];
                SoundManager.Play(info.Tier is "red" or "void" ? "item_legendary" : "item_pickup");
                ShowBanner($"ПОДОБРАНО: {info.Name} ({info.Desc})", info.Color, 3.2f);
                if (info.Tier is "lunar" or "void")
                    UnlockAchievement("ach_lunar_addict");
                if (itemKey == "purity")
                    UnlockAchievement("ach_lunar_purity");
                if (itemKey == "lunar_pauldron")
                    UnlockAchievement("ach_lunar_stone");
            }

            int total = 0;
            foreach (var v in Items.Values) total += v;
            if (total >= 6) UnlockAchievement("ach_hoarder");
            if (total >= 12) UnlockAchievement("ach_arsenal");
        }

        public void ExpireSpinelTonic()
        {
            int activeTonics = Items.TryGetValue("spinel_tonic", out int st) ? st : 0;
            if (activeTonics <= 0) return;

            Items["spinel_tonic"] = 0;
            ItemOrder.Remove("spinel_tonic");

            if (!ItemOrder.Contains("tonic_affliction")) ItemOrder.Add("tonic_affliction");
            Items["tonic_affliction"] = (Items.TryGetValue("tonic_affliction", out int ta) ? ta : 0) + activeTonics;
            PlayerHp = MathF.Min(PlayerHp, PlayerMaxHp);
            SoundManager.Play("ui_error");
            ShowBanner($"ДЕЙСТВИЕ ТОНИКА ИСТЕКЛО! ТОНИКИ ИСПОРТИЛИСЬ (x{Items["tonic_affliction"]}): ВСЕ СТАТЫ СНИЖЕНЫ!", Rgb(175, 95, 255), 4.0f);
        }

        public void SpawnItemDrop(Vector3 pos, string? itemKey = null, string poolType = "small_chest")
        {
            if (poolType is "small_chest" or "large_chest" or "legendary_chest" or "lunar_pod" or "shrine_chance")
            {
                ChestsOpened++;
                if (ChestsOpened >= 4) UnlockAchievement("ach_chest_opener");
                if (poolType == "lunar_pod") UnlockAchievement("ach_lunar_stone");
            }
            string key = itemKey != null && GameData.ItemPool.ContainsKey(itemKey) ? itemKey : GameData.RollItemFromPool(poolType);
            StageEntities.Add(new ItemDrop(pos, key));
            if (Net.NetworkManager.Instance.Connected)
            {
                Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "item_drop",
                    ["item"] = key,
                    ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(pos.X, 2), MathF.Round(pos.Y, 2), MathF.Round(pos.Z, 2))
                });
            }
        }

        public void SpawnItemDropNet(Vector3 pos, string key)
        {
            if (GameData.ItemPool.ContainsKey(key))
                StageEntities.Add(new ItemDrop(pos, key));
        }

        public void OpenCommandPicker(string tier)
        {
            Program.EnqueueCommandPicker(tier);
        }

        public void OpenWorkbench()
        {
            Program.OpenSteveWorkbench();
        }

        public void ShowBanner(string msg, Color col, float duration = 2.5f)
            => Program.ShowBanner(msg, col, duration);

        public void SpawnBoss(Vector3 pos, string? bossType = null, int? forcedNetId = null, bool fromNetwork = false)
        {
            var rng = new Random();
            string bType = bossType != null && GameData.IsKnownBossType(bossType)
                ? bossType
                : GameData.BossTypes[rng.Next(GameData.BossTypes.Length)];
            float fy = Physics.GetSurfaceHeight(pos.X, pos.Z, pos.Y);
            var boss = new CombatTarget(new Vector3(pos.X, fy, pos.Z), bType, DifficultyCoeff, forcedNetId);
            ActiveBoss = boss;
            BossTitle = GameData.BossTitles.TryGetValue(bType, out var bt) ? bt : GameData.BossTitles["boss"];
            CombatManager.Enemies.Add(boss);
            StageEntities.Add(boss);
            World.CharacterUnlockSystems.OnTeleporterBossSpawned(this, boss);

            if (Net.NetworkManager.Instance.Connected && !fromNetwork)
            {
                Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "enemy_spawn",
                    ["id"] = boss.NetId,
                    ["et"] = bType,
                    ["coeff"] = MathF.Round(DifficultyCoeff, 3),
                    ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(pos.X, 2), MathF.Round(fy, 2), MathF.Round(pos.Z, 2)),
                    ["boss"] = true
                });
            }
        }

        public void SpawnHordeOfMany(Vector3 centerPos, string? forcedSpecies = null, int[]? forcedIds = null, Vector3[]? forcedPositions = null, float? forcedTotalHp = null, bool fromNetwork = false)
        {
            string[] hordePool =
            {
                "elder_lemurian", "golem", "greater_wisp", "parent", "lunar_chimera",
                "brass_contraption", "sarkaz_mercenary", "bison", "imp", "combine_soldier", "nether_blaze"
            };
            string species = !string.IsNullOrEmpty(forcedSpecies)
                ? forcedSpecies!
                : hordePool[Random.Shared.Next(hordePool.Length)];

            int count = forcedIds != null && forcedIds.Length > 0
                ? forcedIds.Length
                : (GameData.HasArtifact("swarm") ? 8 : 6);

            int itemCnt = 0;
            foreach (var v in Items.Values) itemCnt += v;
            float totalBossHp = forcedTotalHp ?? (2350f * DifficultyCoeff * (1.0f + 0.04f * itemCnt));
            float perMobHp = MathF.Max(110f, totalBossHp / Math.Max(1, count));

            ActiveBoss = null;
            ActiveHordeBosses.Clear();
            HordeTotalMaxHp = perMobHp * count;
            string spName = GameData.MonsterLog.TryGetValue(species, out var ml) ? ml.Name.ToUpper() : species.ToUpper();
            HordeTitle = $"ОРДА МНОГИХ :: ЭЛИТНАЯ СТАЯ ({spName})";

            var idsArr = new System.Text.Json.Nodes.JsonArray();
            var posArr = new System.Text.Json.Nodes.JsonArray();

            for (int i = 0; i < count; i++)
            {
                Vector3 mPos;
                if (forcedPositions != null && i < forcedPositions.Length)
                {
                    mPos = forcedPositions[i];
                }
                else
                {
                    float ang = (i / (float)count) * MathF.PI * 2f;
                    Vector3 rawPos = centerPos + new Vector3(MathF.Cos(ang) * 7.2f, 0f, MathF.Sin(ang) * 7.2f);
                    float fy = Physics.GetSurfaceHeight(rawPos.X, rawPos.Z, centerPos.Y);
                    mPos = Physics.FindFreeActorPos(new Vector3(rawPos.X, fy, rawPos.Z), 1.2f, 10f, 5f);
                }

                int? fId = (forcedIds != null && i < forcedIds.Length) ? forcedIds[i] : null;
                var mob = new CombatTarget(mPos, species, DifficultyCoeff, fId)
                {
                    IsElite = true,
                    IsHordeBoss = true,
                    MaxHp = perMobHp,
                    Hp = perMobHp
                };
                mob.Damage *= 1.28f;
                mob.Speed *= 1.15f;
                mob.GoldReward = Math.Max(28, 220 / count);
                mob.Scale = new Vector3(1.26f);
                mob.UpdateHpBillboard();

                ActiveHordeBosses.Add(mob);
                CombatManager.Enemies.Add(mob);
                StageEntities.Add(mob);
                new VisualFX(mPos + new Vector3(0, 1.4f, 0), Rgb(255, 145, 45), 3.6f, 0.35f);

                idsArr.Add(mob.NetId);
                posArr.Add(new System.Text.Json.Nodes.JsonArray(MathF.Round(mPos.X, 2), MathF.Round(mPos.Y, 2), MathF.Round(mPos.Z, 2)));
            }

            SoundManager.Play("boss_roar", 1.05f, 0.92f);

            if (Net.NetworkManager.Instance.Connected && !fromNetwork)
            {
                Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "horde_boss_spawn",
                    ["species"] = species,
                    ["total_hp"] = MathF.Round(HordeTotalMaxHp, 1),
                    ["ids"] = idsArr,
                    ["positions"] = posArr
                });
            }
        }

        public void OnHordeMemberKilled(CombatTarget dead, Vector3 deathPos)
        {
            if (IsHordeActive) return;
            if (ActiveHordeBosses.Count == 0) return;

            ActiveHordeBosses.Clear();
            ProgressData.Instance.BossKills++;
            Program.RunBossKills++;
            UnlockAchievement("ach_boss");
            UnlockAchievement("ach_horde_many");
            if (Program.RunBossKills >= 2)
                UnlockAchievement("ach_boss_hunter");
            if (TeleporterChargingNow && TeleporterActiveTimer <= 45.0f)
                UnlockAchievement("ach_ice_band");

            LunarCoins += 2;
            ProgressData.Instance.LunarCoins += 2;
            if (Net.NetworkManager.Instance.Mode != "client")
                SpawnItemDrop(deathPos, null, "boss");
            ShowBanner("ОРДА МНОГИХ СОКРУШЕНА! ПОЛУЧЕНА НАГРАДА БОССА И +2 ЛУННЫЕ МОНЕТЫ!", Color.Gold, 4.2f);
        }

        public void OnTeleporterActivated()
        {
            TeleporterActiveTimer = 0f;
            TeleporterChargingNow = true;
            if (ActiveTeleporterOrPad is World.Teleporter tpStart)
                World.CharacterUnlockSystems.OnTeleporterStarted(this, tpStart);
            if (StageElapsedTime <= 180f)
                UnlockAchievement("ach_wind_runner");
            bool spawnedHorde = false;
            if (Net.NetworkManager.Instance.Mode != "client")
            {
                Vector3 tpPos = ActiveTeleporterOrPad != null ? ActiveTeleporterOrPad.Position : PlayerPos;
                if (Random.Shared.NextDouble() < 0.24)
                {
                    spawnedHorde = true;
                    SpawnHordeOfMany(tpPos + new Vector3(9f, 0f, 9f));
                }
                else
                {
                    SpawnBoss(tpPos + new Vector3(9f, 0f, 9f));
                }
                if (GameData.SelectedDifficulty == "eclipse")
                {
                    foreach (float offX in new[] { -7f, 7f })
                    {
                        float gy = Physics.GetSurfaceHeight(tpPos.X + offX, tpPos.Z - 6f, tpPos.Y);
                        var guard = new CombatTarget(new Vector3(tpPos.X + offX, gy, tpPos.Z - 6f), "golem", DifficultyCoeff * 1.15f);
                        guard.IsElite = true;
                        CombatManager.Enemies.Add(guard);
                        StageEntities.Add(guard);
                    }
                }
            }
            if (spawnedHorde)
                ShowBanner($"ПРОБУДИЛАСЬ {HordeTitle}!", Rgb(255, 145, 45), 3.8f);
            else
                ShowBanner("ТЕЛЕПОРТ АКТИВИРОВАН! ПОБЕДИТЕ БОССА ТЕЛЕПОРТА!", Color.Red, 3.2f);
        }

        public void NextStage()
        {
            Stage++;
            if (Stage >= 2) UnlockAchievement("ach_traveler");
            if (Stage >= 3) UnlockAchievement("ach_stage3");
            if (Stage >= 4) UnlockAchievement("ach_stage4");
            Program.LoadStage(Stage, null);
        }

        public void EnterBazaar()
        {
            UnlockAchievement("ach_lunar_purity");
            Program.LoadStage(Stage, "bazaar");
        }
        public void EnterMoonStage() => Program.LoadStage(Stage, "moon");
        public void EnterSecretStage(string modeKey) => Program.LoadStage(Stage, modeKey);

        public void OnMoonPillarCharged()
        {
            MoonPillarsDone++;
            if (ActiveTeleporterOrPad is LunarJumpPad pad)
            {
                pad.UpdateProgressLabel(MoonPillarsDone, 4);
                if (MoonPillarsDone >= 4)
                {
                    pad.UnlockPad();
                    ShowBanner("ВСЕ 4 СТОЛПА ЗАРЯЖЕНЫ! ГРАВИ-ЛИФТ В ЦЕНТРЕ ОТКРЫТ (Y=120м)!", Color.SkyBlue, 5.0f);
                    return;
                }
            }
            ShowBanner($"СТОЛП ЗАРЯЖЕН ({MoonPillarsDone}/4)! ОСТАЛОСЬ ЕЩЁ {Math.Max(0, 4 - MoonPillarsDone)}", Color.Lime, 3.2f);
        }

        public void StartMithrixEncounter()
        {
            if (MithrixSpawned) return;
            MithrixSpawned = true;
            if (CharKey == "mindustry" && Combat != null && IsValid(Combat.MindustryMainCore) && Combat.MindustryMainCore!.Y < 100f)
            {
                Destroy(Combat.MindustryMainCore);
                Combat.MindustryMainCore = null;
                Combat.MindustryCoreRebuildTimer = 60.0f;
                Combat.MindustryCopper += 80f;
                Combat.MindustrySilicon += 55f;
                Combat.MindustryThorium += 25f;
            }
            foreach (var e in CombatManager.Enemies.ToArray())
            {
                if (IsValid(e) && !e.IsBoss) Destroy(e);
            }
            CombatManager.Enemies.RemoveAll(e => !IsValid(e));

            var boss = new CombatTarget(new Vector3(0, Physics.ColosseumY, 22.0f), "mithrix", DifficultyCoeff, forcedNetId: 9999)
            {
                PhaseTransitionTimer = 1.6f,
                AttackTimer = 2.4f,
                SpecialTimer = 6.2f
            };
            ActiveBoss = boss;
            BossTitle = "МИТРИКС [ФАЗА 1/4] :: КОРОЛЬ НИЧЕГО";
            CombatManager.Enemies.Add(boss);
            StageEntities.Add(boss);
            World.CharacterUnlockSystems.OnTeleporterBossSpawned(this, boss);
            ShowBanner("МИТРИКС, КОРОЛЬ НИЧЕГО [ФАЗА 1/4] :: БИТВА В НЕБЕСНОМ КОЛИЗЕЕ!", Color.SkyBlue, 4.2f);
            if (Net.NetworkManager.Instance.Connected)
                Net.NetworkManager.Instance.Broadcast(new System.Text.Json.Nodes.JsonObject { ["type"] = "mithrix_start" });
        }
        public void OnMithrixDefeated(Vector3 deathPos)
        {
            ActiveBoss = null;
            MoonDetonationTimer = 75.0f;
            ProgressData.Instance.MithrixDefeated = true;
            UnlockAchievement("ach_safe_travels");
            UnlockAchievement("ach_stage4");
            UnlockAchievement("ach_boss_hunter");
            ProgressData.Instance.Save();
            Vector3 shipPos = new(0, 0, -52.0f);
            StageEntities.Add(new EscapeShip(shipPos));
            StageEntities.Add(new LunarEscapePad(new Vector3(0, Physics.ColosseumY, -46.0f), shipPos));
            ShowBanner("МИТРИКС ПОВЕРЖЕН! КОРАБЛЬ «SAFE TRAVELS» РАЗБЛОКИРОВАН! ПРЫГАЙТЕ В ЛУЧ ЭВАКУАЦИИ (75 СЕК)!", Color.Lime, 6.5f);
        }

        public void OnRunVictory()
            => Program.OnVictory();
    }
    public static partial class Program
    {
        private static string _screen = "login"; // "login", "main", "mp_connect", "settings", "lobby", "logbook", "in_game", "replay"
        private static string _playerNameInput = "";
        private static string _selectedChar = "commando";
        private static string _lobbyTab = "skills"; // "skills", "overview", "modifiers"
        private static GameRunContext? _run;
        private static bool _paused = false;
        private static bool _isSpectating = false;
        private static bool _isDedicatedSpectator = false;
        private static bool _lobbyJoinAsSpectator = false;
        public static bool IsSpectating => _isSpectating || _isDedicatedSpectator;
        private static int _spectateTargetIdx = 0;
        private static bool _spectateFreeCam = false;
        private static float _spectateYaw = 0f;
        private static float _spectatePitch = 16f;
        private static float _spectateZoomDist = 9.5f;
        private static Vector3 _spectateSmoothPivot = new(0, 2f, 0);
        private static Vector3 _spectateFreePos = new(0, 8f, -12f);
        private static float _spectateStageGraceTimer = 0f;
        private static int _currentStageSeed = 1001;
        private static float _netProjFxWindowTimer = 0f;
        private static int _netProjFxCountInWindow = 0;
        private static string _lastNetItemsSig = "";
        private static float _netItemsResendTimer = 0f;
        private static string _lastCompletedReplayPath = "";
        private static string _selectedHistoryRunId = "";
        private static int _historyPage = 0;
        private static readonly HashSet<string> _deadCoopPlayers = new();
        private static readonly Queue<string> _commandPickerQueue = new();
        private static bool _workbenchOpen = false;

        public static IReadOnlyDictionary<string, RemotePlayer> GetRemotePlayersReadOnly() => _remotePlayers;
        public static bool HasDeadCoopAllies() => _deadCoopPlayers.Count > 0;

        public static void NotifyPlayerProjectileSpawned(Vector3 pos, Vector3 dir, float speed, float scaleVal, Color col, float lifetime)
        {
            if (!_net.Connected || _screen != "in_game" || _run == null || IsSpectating) return;
            if (_netProjFxCountInWindow >= 28) return;
            _netProjFxCountInWindow++;
            _net.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "proj_fx",
                ["x"] = MathF.Round(pos.X, 2),
                ["y"] = MathF.Round(pos.Y, 2),
                ["z"] = MathF.Round(pos.Z, 2),
                ["dx"] = MathF.Round(dir.X, 3),
                ["dy"] = MathF.Round(dir.Y, 3),
                ["dz"] = MathF.Round(dir.Z, 3),
                ["spd"] = MathF.Round(speed, 1),
                ["s"] = MathF.Round(scaleVal, 2),
                ["r"] = (int)col.R,
                ["g"] = (int)col.G,
                ["b"] = (int)col.B,
                ["life"] = MathF.Round(lifetime, 2)
            });
        }

        public static void NotifyItemPickedUp(Vector3 pos, string itemKey)
        {
            if (!_net.Connected || _screen != "in_game" || _run == null) return;
            _net.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "item_pickup",
                ["item"] = itemKey,
                ["x"] = MathF.Round(pos.X, 2),
                ["z"] = MathF.Round(pos.Z, 2)
            });
        }

        public static void EnqueueCommandPicker(string tier)
        {
            _commandPickerQueue.Enqueue(tier);
            Raylib.EnableCursor();
        }

        public static void OpenSteveWorkbench()
        {
            _workbenchOpen = true;
            Raylib.EnableCursor();
            SoundManager.Play("interact", 0.9f);
        }

        // Индикаторы попаданий и урона в стиле CS + Статистика забега
        private static float _hitMarkerTimer = 0f;
        private static bool _hitMarkerCrit = false;
        private static float _damageFlashTimer = 0f;

        public static int RunKills = 0;
        public static int RunBossKills = 0;
        public static float RunDamageDealt = 0f;
        public static int RunGoldEarned = 0;
        private static bool _lastRunVictory = false;
        private static float _lastRunTime = 0f;
        private static int _lastRunStage = 1;
        private static readonly List<(string Key, int Count)> _lastRunItems = new();

        public static void TriggerHitMarker(bool isCrit, float dmg)
        {
            _hitMarkerTimer = 0.18f;
            _hitMarkerCrit = isCrit;
            RunDamageDealt += dmg;
            if (_run != null && !_run.IsSafeTravels)
            {
                if (dmg >= 90f) _run.UnlockAchievement("ach_first_strike");
                if (dmg >= 220f) _run.UnlockAchievement("ach_fire_band");
                if (RunDamageDealt >= 3000f) _run.UnlockAchievement("ach_burn_bright");
                if (RunDamageDealt >= 8000f) _run.UnlockAchievement("ach_armor_breaker");
                if (isCrit)
                {
                    _run.CritHitsCount++;
                    if (_run.CritHitsCount >= 10) _run.UnlockAchievement("ach_crit_reaper");
                }
            }
        }

        public static void TriggerDamageFlash(float dmg)
        {
            if (dmg > 0f)
            {
                _damageFlashTimer = Math.Clamp(0.25f + dmg * 0.008f, 0.25f, 0.65f);
                if (_run != null && !_run.IsSafeTravels)
                {
                    _run.HitsTakenCount++;
                    _run.UntouchedCombatTimer = 0f;
                    _run.PostHitTimer = 4.5f;
                    if (_run.HitsTakenCount >= 12) _run.UnlockAchievement("ach_tank_hits");
                }
            }
        }

        public static void RecordEnemyKill(int goldGain, CombatTarget? dead = null)
        {
            RunKills++;
            RunGoldEarned += goldGain;
            if (_run != null && !_run.IsSafeTravels)
            {
                if (_run.Stage == 1 && RunKills >= 8) _run.UnlockAchievement("ach_trophies");
                if (RunKills >= 12) _run.UnlockAchievement("ach_slayer");
                if (RunKills >= 25) _run.UnlockAchievement("ach_carnage");
                if (RunKills >= 35) _run.UnlockAchievement("ach_high_voltage");
                if (RunKills >= 50) _run.UnlockAchievement("ach_ritual_blood");
                if (RunGoldEarned >= 120 || _run.Gold >= 120) _run.UnlockAchievement("ach_doran_start");
                if (RunGoldEarned >= 250 || _run.Gold >= 250) _run.UnlockAchievement("ach_item_golden_apple");

                if (_run.MultiKillWindowTimer <= 0f)
                {
                    _run.MultiKillWindowTimer = 5.0f;
                    _run.MultiKillCount = 1;
                }
                else
                {
                    _run.MultiKillCount++;
                    _run.MultiKillWindowTimer = MathF.Max(_run.MultiKillWindowTimer, 4.0f);
                }
                if (_run.MultiKillCount >= 3) _run.UnlockAchievement("ach_demolition");
                if (_run.MultiKillCount >= 4) _run.UnlockAchievement("ach_shield_bearer");

                if (_run.FrenzyWindowTimer <= 0f)
                {
                    _run.FrenzyWindowTimer = 12.0f;
                    _run.FrenzyKillCount = 1;
                }
                else
                {
                    _run.FrenzyKillCount++;
                }
                if (_run.FrenzyKillCount >= 6) _run.UnlockAchievement("ach_frenzy");

                if (!_run.PlayerGrounded)
                {
                    _run.AirborneKills++;
                    if (_run.AirborneKills >= 6) _run.UnlockAchievement("ach_airborne");
                }
                if (_run.PostHitTimer > 0f)
                {
                    _run.ThornsKills++;
                    if (_run.ThornsKills >= 5) _run.UnlockAchievement("ach_thorns");
                }

                if (dead != null)
                {
                    if (dead.IsBoss)
                    {
                        RunBossKills++;
                        _run.UnlockAchievement("ach_boss");
                        if (RunBossKills >= 2) _run.UnlockAchievement("ach_boss_hunter");
                    }
                    if (Vector3.Distance(_run.PlayerPos, dead.Position) <= 13.0f)
                    {
                        _run.CloseKills++;
                        if (_run.CloseKills >= 10) _run.UnlockAchievement("ach_close_combat");
                        if (_run.CloseKills >= 20) _run.UnlockAchievement("ach_blizzard");
                    }
                    if (dead.IsElite || dead.IsBoss || dead.IsDoppelganger)
                    {
                        _run.EliteOrBossKills++;
                        if (_run.EliteOrBossKills >= 3) _run.UnlockAchievement("ach_shock_tactics");
                        if (_run.EliteOrBossKills >= 7) _run.UnlockAchievement("ach_elite_slayer");
                    }
                    if (dead.EnemyType is "wisp" or "greater_wisp" or "lemurian" or "elder_lemurian" or "imp" or "nether_blaze" or "hell_drone")
                    {
                        _run.PyroKills++;
                        if (_run.PyroKills >= 10) _run.UnlockAchievement("ach_wisp_hunter");
                    }
                    if (dead.IsBoss && _run.TeleporterChargingNow && _run.TeleporterActiveTimer <= 45.0f)
                    {
                        _run.UnlockAchievement("ach_ice_band");
                    }
                }
            }
        }

        public static bool HasAnyDeadCoopPlayers() => _deadCoopPlayers.Count > 0;

        public static bool TryHealRemotePlayerCoop(Vector3 hitPos, float radius, float healAmount)
        {
            if (!_net.Connected || Physics.SpecialMode == "pvp") return false;
            foreach (var kv in _remotePlayers)
            {
                var rp = kv.Value;
                if (!_deadCoopPlayers.Contains(kv.Key) && rp.IsAliveCombatant && Vector3.Distance(hitPos, rp.Position + new Vector3(0, 1.1f, 0)) <= radius)
                {
                    _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "ally_heal",
                        ["target_id"] = kv.Key,
                        ["heal"] = MathF.Round(healAmount, 1),
                        ["uber"] = false
                    });
                    return true;
                }
            }
            return false;
        }

        // Настройки
        private static float _mouseSens = 0.18f;
        private static float _fov = 60f;
        private static string _mpTransportTab = "steam";
        private static bool _steamTabAutoChecked = false;
        private static int _lobbyMidPanelX = 420;
        private static int _lobbyMidPanelW = 488;
        private static int _lobbyTopY = 66;
        private static int _tabItemPage = 0;
        private static int _skipMouseDeltaFrames = 0;
        private static bool _consumeLeftClickOnResume = false;
        private static float _resumeAttackSuppressTimer = 0f;

        private static void ConsumeGameplayClickOnResume()
        {
            _consumeLeftClickOnResume = true;
            _resumeAttackSuppressTimer = 0.20f;
            _skipMouseDeltaFrames = Math.Max(_skipMouseDeltaFrames, 2);
        }

        private static bool IsDescendantOrSelf(Entity? ent, Entity? root)
        {
            if (ent == null || root == null) return false;
            Entity? cur = ent;
            while (cur != null)
            {
                if (ReferenceEquals(cur, root)) return true;
                cur = cur.Parent;
            }
            return false;
        }

        private static void PurgeWorldEntities(bool keepCurrentRunPlayer)
        {
            Entity? keepPlayer = (keepCurrentRunPlayer && _run != null) ? _run.PlayerEntity : null;

            foreach (var root in Engine.RootEntities.ToArray())
            {
                if (root == null || root.IsDestroyed) continue;
                if (IsDescendantOrSelf(root, _mainPedestal) ||
                    IsDescendantOrSelf(root, _menuCrystal) ||
                    IsDescendantOrSelf(root, _logDetailPreviewRoot) ||
                    (keepPlayer != null && IsDescendantOrSelf(root, keepPlayer)))
                {
                    continue;
                }
                Destroy(root);
            }

            foreach (var ent in Engine.AllEntities.ToArray())
            {
                if (ent == null || ent.IsDestroyed) continue;
                if (IsDescendantOrSelf(ent, _mainPedestal) ||
                    IsDescendantOrSelf(ent, _menuCrystal) ||
                    IsDescendantOrSelf(ent, _logDetailPreviewRoot) ||
                    (keepPlayer != null && IsDescendantOrSelf(ent, keepPlayer)))
                {
                    continue;
                }
                Destroy(ent);
            }

            Engine.ActiveTweens.RemoveAll(tw => tw == null || tw.IsTargetDestroyed);
        }

        private static void PersistRuntimeUserSettings()
        {
            Data.UserSettings.Instance.PreferredNetTransport = _mpTransportTab;
            Data.UserSettings.Instance.SyncFromRuntime(_mouseSens, _fov, _selectedChar);
            Data.ProgressData.Instance.SelectedCharacter = _selectedChar;
            Data.ProgressData.Instance.Save();
        }

        // Сетевое состояние и Лобби
        private static readonly Net.NetworkManager _net = new();
        private static readonly Dictionary<string, (string CharKey, bool Ready, bool HasBirdTrio, bool IsSpectator, string PlayerName)> _lobbyPlayers = new();
        private static int _lobbyCharPage = 0;
        private static readonly Dictionary<string, RemotePlayer> _remotePlayers = new();
        private static bool _myReady = false;
        private static bool _lobbyCountdownActive = false;
        private static float _lobbyCountdown = 60f;
        private static bool _typingCode = false;
        private static string _roomCodeInput = "";
        private static string _netStatusText = "";
        private static float _netSyncTimer = 0f;
        private static Vector3 _lastResolvedPlayerPos = Vector3.Zero;
        private static bool _playerSweepValid = false;
        private static float _enemySyncTimer = 0f;

        private static string _bannerText = "";
        private static Color _bannerColor = Color.Yellow;
        private static float _bannerTimer = 0f;

        // 3D Диорама
        private static Entity _mainPedestal = null!;
        private static Entity _pedestalRing = null!;
        private static Entity _previewActor = null!;
        private static SurvivorParts? _previewSurvivorParts;
        private static EnemyParts? _previewEnemyParts;
        private static Entity _menuCrystal = null!;

        private static float _previewBaseScale = 1.0f;
        private static float _previewZoom = 1.0f;
        private static float _manualTimer = 0.0f;
        private static Entity? _activeSkillDemo = null;
        private static float _skillPreviewTimer = 0f;
        private static string _skillPreviewTitle = "";
        private static Color _skillPreviewColor = Color.SkyBlue;

        // Данные об убийце игрока (для отображения моба в правом верхнем углу после смерти)
        private static string _lastKillerKey = "lemurian";
        private static string _lastKillerName = "Лемурианец";
        private static string _lastKillerRank = "ОБЫЧНЫЙ ВРАГ";
        private static string _lastKillerDesc = "Коренной ящер Petrichor V. Атакует огненными шарами и когтями вблизи.";
        private static float _lastKillerDamage = 12f;
        private static Color _lastKillerColor = Rgb(255, 110, 65);
        private static bool _lastKillerIsBoss = false;
        private static bool _lastKillerIsElite = false;
        private static bool _lastKillerIsDoppel = false;
        private static bool _summaryShowKiller3D = true;

        public static void RecordPlayerDamageSource(CombatTarget? attacker, float dmg, string? customSource = null)
        {
            _lastKillerDamage = MathF.Max(1f, dmg);

            if (customSource == "pvp")
            {
                _lastKillerKey = "pvp_rival";
                _lastKillerName = "Соперник на Арене PvP";
                _lastKillerRank = "ИГРОК-ДУЭЛЯНТ [PvP]";
                _lastKillerDesc = "Враждебный Выживший на Арене Раздора, нанёсший вам решающий удар.";
                _lastKillerColor = Rgb(255, 65, 85);
                _lastKillerIsBoss = false;
                _lastKillerIsElite = true;
                _lastKillerIsDoppel = false;
                return;
            }
            if (customSource == "chaos")
            {
                _lastKillerKey = "chaos_self";
                _lastKillerName = "Артефакт Хаоса";
                _lastKillerRank = "ДРУЖЕСТВЕННЫЙ ОГОНЬ";
                _lastKillerDesc = "Собственный взрыв по области задел вас из-за активного Артефакта Хаоса.";
                _lastKillerColor = Rgb(255, 95, 55);
                _lastKillerIsBoss = false;
                _lastKillerIsElite = false;
                _lastKillerIsDoppel = false;
                return;
            }
            if (customSource == "moon_detonation")
            {
                _lastKillerKey = "mithrix";
                _lastKillerName = "Детонация Луны";
                _lastKillerRank = "КАТАКЛИЗМ PETRICHOR V";
                _lastKillerDesc = "Таймер эвакуации истёк до того, как спасательный корабль покинул орбиту Луны.";
                _lastKillerColor = Color.SkyBlue;
                _lastKillerIsBoss = true;
                _lastKillerIsElite = false;
                _lastKillerIsDoppel = false;
                return;
            }
            if (customSource == "dante_sacrifice")
            {
                _lastKillerKey = "imp";
                _lastKillerName = "Поворот Часов Данте";
                _lastKillerRank = "ЖЕРТВА МЕНЕДЖЕРА";
                _lastKillerDesc = "Вы отдали собственную жизнь, чтобы повернуть стрелки часов вспять.";
                _lastKillerColor = Color.Gold;
                _lastKillerIsBoss = false;
                _lastKillerIsElite = false;
                _lastKillerIsDoppel = false;
                return;
            }

            string et = "lemurian";
            bool isBoss = false, isElite = false, isDoppel = false;
            int phase = 1;

            if (attacker != null)
            {
                et = attacker.EnemyType;
                isBoss = attacker.IsBoss;
                isElite = attacker.IsElite;
                isDoppel = attacker.IsDoppelganger;
                phase = attacker.Phase;
            }
            else if (!string.IsNullOrEmpty(customSource) && customSource.StartsWith("mob:"))
            {
                string[] parts = customSource.Split(':');
                if (parts.Length >= 5)
                {
                    et = parts[1];
                    isBoss = parts[2] == "1";
                    isElite = parts[3] == "1";
                    isDoppel = parts[4] == "1";
                }
            }
            else if (_run?.ActiveBoss != null && IsValid(_run.ActiveBoss))
            {
                et = _run.ActiveBoss.EnemyType;
                isBoss = true;
                isElite = _run.ActiveBoss.IsElite;
                phase = _run.ActiveBoss.Phase;
            }

            _lastKillerKey = et;
            _lastKillerIsBoss = isBoss;
            _lastKillerIsElite = isElite;
            _lastKillerIsDoppel = isDoppel;

            if (isDoppel)
            {
                _lastKillerName = "Теневой Доппельгангер";
                _lastKillerRank = "★ ВТОРЖЕНИЕ АРТЕФАКТА МЕСТИ";
                _lastKillerDesc = "Ваше собственное потустороннее отражение, вторгшееся в мир за вашей душой.";
                _lastKillerColor = Rgb(215, 75, 255);
            }
            else if (GameData.MonsterLog.TryGetValue(et, out var mInfo))
            {
                _lastKillerName = isElite && !isBoss ? $"Элитный {mInfo.Name}" : mInfo.Name;
                _lastKillerRank = et == "mithrix"
                    ? $"ФИНАЛЬНЫЙ БОСС [ФАЗА {phase}/4]"
                    : (isBoss ? "БОСС ТЕЛЕПОРТА" : (isElite ? "★ ЭЛИТНЫЙ МОНСТР" : "ОБЫЧНЫЙ ВРАГ"));
                _lastKillerDesc = mInfo.Desc;
                _lastKillerColor = et == "mithrix"
                    ? Color.SkyBlue
                    : (isBoss ? Rgb(255, 65, 65) : (isElite ? Rgb(255, 175, 45) : Rgb(255, 115, 65)));
            }
            else
            {
                _lastKillerName = et.ToUpper();
                _lastKillerRank = isBoss ? "БОСС ТЕЛЕПОРТА" : (isElite ? "★ ЭЛИТНЫЙ МОНСТР" : "ОБЫЧНЫЙ ВРАГ");
                _lastKillerDesc = "Опасное существо планеты Petrichor V.";
                _lastKillerColor = isBoss ? Color.Red : Color.Orange;
            }
        }

        // Состояние Журнала (Logbook)
        private static string _logCategory = "items";
        private static int _logPage = 0;
        private static string _logSelectedKey = "syringe";

        public static Vector3 GetNearestPlayerPos(Vector3 fromPos, out bool isLocalPlayer)
        {
            isLocalPlayer = true;
            if (_run == null) return Vector3.Zero;
            bool localAlive = !IsSpectating && _run.PlayerHp > 0f;
            Vector3 bestPos = localAlive ? _run.PlayerPos : Vector3.Zero;
            float bestDist = (localAlive && _run.CanPlayerInteract) ? Vector3.Distance(fromPos, bestPos) : 99999f;

            foreach (var kv in _remotePlayers)
            {
                var rp = kv.Value;
                if (!_deadCoopPlayers.Contains(kv.Key) && rp.IsAliveCombatant)
                {
                    float d = Vector3.Distance(fromPos, rp.Position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestPos = rp.Position;
                        isLocalPlayer = false;
                    }
                }
            }
            return bestPos;
        }

        public static bool IsNearRemotePlayer(Vector3 pos, float radius)
        {
            foreach (var kv in _remotePlayers)
            {
                var rp = kv.Value;
                if (!_deadCoopPlayers.Contains(kv.Key) && rp.IsAliveCombatant && Vector3.Distance(pos, rp.Position + new Vector3(0, 1.0f, 0)) <= radius)
                    return true;
            }
            return false;
        }

        public static Vector3 GetNearestRemotePlayerPos(Vector3 fromPos, out float bestDist)
        {
            bestDist = 999f;
            Vector3 best = Vector3.Zero;
            foreach (var kv in _remotePlayers)
            {
                var rp = kv.Value;
                if (!_deadCoopPlayers.Contains(kv.Key) && rp.IsAliveCombatant)
                {
                    float d = Vector3.Distance(fromPos, rp.Position);
                    if (d < bestDist) { bestDist = d; best = rp.Position; }
                }
            }
            return best;
        }

        public static bool TryHitRemotePlayerPvP(Vector3 hitPos, float radius, float dmg)
        {
            if (Physics.SpecialMode != "pvp" || !_net.Connected || IsSpectating) return false;
            foreach (var kv in _remotePlayers)
            {
                var rp = kv.Value;
                if (!_deadCoopPlayers.Contains(kv.Key) && rp.IsAliveCombatant && Vector3.Distance(hitPos, rp.Position + new Vector3(0, 1.1f, 0)) <= radius)
                {
                    _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "pvp_hit",
                        ["target_id"] = kv.Key,
                        ["attacker_id"] = _net.MyId,
                        ["dmg"] = MathF.Round(dmg, 1)
                    });
                    return true;
                }
            }
            return false;
        }

        public static void ShowBanner(string msg, Color col, float duration = 2.5f)
        {
            _bannerText = msg;
            _bannerColor = col;
            _bannerTimer = duration;
        }

        public static void Main()
        {
            var userSettings = Data.UserSettings.Instance;
            var prog = ProgressData.Instance;

            Net.SteamSpacewarService.Instance.ParseCommandLineConnectArgs(Environment.GetCommandLineArgs());
            Net.SteamSpacewarService.Instance.RefreshSpacewarDiskState();

            Engine.Init(userSettings.WindowWidth, userSettings.WindowHeight, "Risk of C# 2 — Steam Spacewar & LAN Кооператив");
            userSettings.ApplyToAudioAndWindow();

            _mouseSens = userSettings.MouseSens;
            _fov = userSettings.Fov;
            _mpTransportTab = userSettings.PreferredNetTransport is "steam" or "lan" ? userSettings.PreferredNetTransport : "steam";

            string savedHero = !string.IsNullOrWhiteSpace(userSettings.SelectedCharacter)
                ? userSettings.SelectedCharacter
                : prog.SelectedCharacter;
            if (!string.IsNullOrWhiteSpace(savedHero) && SurvivorDatabase.Classes.ContainsKey(savedHero) && prog.IsCharacterUnlocked(savedHero))
                _selectedChar = savedHero;

            _netStatusText = $"Ваш локальный IP: {_net.HostIp}";
            _playerNameInput = prog.PlayerName;
            _screen = string.IsNullOrWhiteSpace(prog.PlayerName) ? "login" : "main";
            SetupDiorama();

            if (Net.SteamSpacewarService.Instance.SpacewarInstalledOnDisk)
            {
                Net.SteamSpacewarService.Instance.EnsureInitialized(autoInstallIfMissing: false);
            }

            while (!Raylib.WindowShouldClose())
            {
                float dt = Math.Min(Raylib.GetFrameTime(), 0.05f);
                _netProjFxWindowTimer += dt;
                if (_netProjFxWindowTimer >= 1.0f)
                {
                    _netProjFxWindowTimer = 0f;
                    _netProjFxCountInWindow = 0;
                }
                ProcessNetwork();
                SoundManager.UpdateAmbient(dt, (_screen == "in_game" && !_paused) || _screen == "replay", _run != null && _run.IsBossAlive);
                if (_bannerTimer > 0f) _bannerTimer -= dt;
                if (_hitMarkerTimer > 0f) _hitMarkerTimer -= dt;
                if (_damageFlashTimer > 0f) _damageFlashTimer -= dt;

                if (Raylib.IsKeyPressed(KeyboardKey.F11) ||
                    ((Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt)) && Raylib.IsKeyPressed(KeyboardKey.Enter)))
                {
                    Data.UserSettings.Instance.ToggleFullscreenAndApply();
                    PersistRuntimeUserSettings();
                    SoundManager.Play("ui_click");
                }

                if (_screen == "replay")
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    {
                        SoundManager.Play("ui_click");
                        ExitReplayScreen();
                    }
                    else
                    {
                        Data.ReplayViewer.Update(dt);
                        Engine.Update(dt);
                    }
                }
                else if (_screen == "in_game")
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    {
                        if (_dialogueOpen)
                        {
                            CloseSafeTravelsDialogueModal();
                            SoundManager.Play("ui_click");
                        }
                        else if (_rpsOpen)
                        {
                            CloseRpsMiniGame();
                            SoundManager.Play("ui_click");
                        }
                        else if (_emoteWheelOpen)
                        {
                            _emoteWheelOpen = false;
                            SoundManager.Play("ui_click");
                            RestoreInGameCursorIfNeeded();
                        }
                        else if (_safeTravelsWardrobeOpen)
                        {
                            _safeTravelsWardrobeOpen = false;
                            SoundManager.Play("ui_click");
                            RestoreInGameCursorIfNeeded();
                        }
                        else if (_workbenchOpen)
                        {
                            _workbenchOpen = false;
                            SoundManager.Play("ui_click");
                            RestoreInGameCursorIfNeeded();
                        }
                        else
                        {
                            _paused = !_paused;
                            SoundManager.Play("ui_click");
                            RestoreInGameCursorIfNeeded();
                        }
                    }

                    if (!_paused)
                    {
                        UpdateInGame(dt);
                        // Во время катсцены разблокировки мир стоит на паузе: враги, снаряды и ловушки замирают
                        // (в сетевой игре мир продолжает жить у других игроков, поэтому там только неуязвимость)
                        if (!IsCutscenePlaying || _net.Connected)
                            Engine.Update(dt);
                    }
                    Engine.ObstructionFadeEnabled = _run != null && ((IsSpectating && !_spectateFreeCam) || (!IsSpectating && !SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey)) || !string.IsNullOrEmpty(_run.Parts.ActiveEmote) || _dialogueOpen);
                    if (_run != null) Engine.PlayerWorldPos = IsSpectating ? _spectateSmoothPivot : _run.PlayerPos;
                }
                else
                {
                    Engine.ObstructionFadeEnabled = false;
                    HandleInput(dt);
                    Engine.Update(dt);
                    UpdateDiorama(dt);
                    Engine.Camera.Position = new Vector3(0f, 2.15f, -8.6f);
                    Engine.Camera.FovY = _fov;
                    Engine.CameraEuler = new Vector3(5f, 0f, 0f);
                    Engine.UpdateCameraTargetFromEuler();

                    if (_screen == "lobby" && _net.Connected && _lobbyCountdownActive)
                    {
                        int prevSec = (int)MathF.Ceiling(_lobbyCountdown);
                        _lobbyCountdown = MathF.Max(0f, _lobbyCountdown - dt);
                        int newSec = (int)MathF.Ceiling(_lobbyCountdown);
                        if (newSec < prevSec && newSec <= 5 && newSec > 0)
                            SoundManager.Play("ui_tick");
                        if (_net.Mode == "host" && _lobbyCountdown <= 0f)
                        {
                            _lobbyCountdownActive = false;
                            StartRun(_selectedChar, asDedicatedSpectator: _lobbyJoinAsSpectator);
                        }
                    }
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Engine.BackgroundColor);

                if (!(_screen == "logbook" && (_logDetailOpen || _logCategory == "history")))
                {
                    Raylib.BeginMode3D(Engine.Camera);
                    Engine.Draw3D();
                    Raylib.EndMode3D();

                    Engine.Draw3DBillboardLabels(Engine.Camera);
                }

                if (_screen == "replay")
                {
                    if (Data.ReplayViewer.DrawOverlayUI())
                        ExitReplayScreen();
                }
                else if (_screen == "in_game")
                {
                    DrawInGameHUD();
                    if (_paused) DrawPauseOverlay();
                }
                else if (_screen == "login") DrawLoginUI();
                else if (_screen == "main") DrawMainMenuUI();
                else if (_screen == "mp_connect") DrawMpConnectUI();
                else if (_screen == "settings") DrawSettingsUI();
                else if (_screen == "lobby") DrawLobbyUI();
                else if (_screen == "run_summary") DrawRunSummaryUI();
                else DrawLogbookUI();
                // Компактный тост уведомлений внизу экрана (не закрывает прицел и полоску Босса!)
                if (_bannerTimer > 0f && !string.IsNullOrEmpty(_bannerText))
                {
                    int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
                    int bw = Math.Min(840, sw - 40);
                    float ty = _screen == "in_game" ? sh - 128f : 108f;
                    Raylib.DrawRectangle((sw - bw) / 2, (int)ty - 18, bw, 36, Rgb(10, 14, 24, 210));
                    Raylib.DrawRectangle((sw - bw) / 2, (int)ty - 18, bw, 2, _bannerColor);
                    Engine.DrawTextCentered(_bannerText, sw * 0.5f, ty, 17f, _bannerColor);
                }

                Raylib.EndDrawing();
            }

            PersistRuntimeUserSettings();
            _net.StopSession();
            Net.SteamSpacewarService.Instance.Shutdown();
            SoundManager.Shutdown();
            Raylib.CloseWindow();
        }

        private static void ProcessNetwork()
        {
            var polled = _net.PollMessages();
            if (_net.PendingSteamLobbyScreenTransition)
            {
                _net.PendingSteamLobbyScreenTransition = false;
                if (_screen == "in_game") QuitRunToLobby(toMainMenu: false);
                _lobbyPlayers.Clear();
                _myReady = false;
                _lobbyCountdownActive = false;
                _screen = "lobby";
                SelectCharacter(_selectedChar);
                ShowBanner("ПОДКЛЮЧЕНИЕ К STEAM-ЛОББИ ДРУГА (SPACEWAR)...", Color.Lime, 3.5f);
            }

            foreach (var msg in polled)
            {
                string mtype = msg["type"]?.GetValue<string>() ?? "";
                if (mtype == "welcome")
                {
                    _net.MyId = msg["player_id"]?.GetValue<string>() ?? "p2";
                    _net.RoomCode = msg["room_code"]?.GetValue<string>() ?? _net.RoomCode;
                    _net.GameType = msg["game_type"]?.GetValue<string>() ?? _net.GameType;
                    ShowBanner($"ПОДКЛЮЧЕНО К КОМНАТЕ {_net.RoomCode} [{_net.GameType.ToUpper()}]!", Color.Lime, 3.0f);
                    BroadcastLobbyState();
                }
                else if (mtype == "client_joined")
                {
                    string pid = msg["player_id"]?.GetValue<string>() ?? "p2";
                    string pName = msg["player_name"]?.GetValue<string>() ?? "";
                    if (pid != _net.MyId) _lobbyPlayers[pid] = ("commando", false, false, false, pName);
                    if (_screen == "in_game" && _run != null && _net.Mode == "host")
                    {
                        ShowBanner($"ГОСТЬ [{pid.ToUpper()}] ПОДКЛЮЧИЛСЯ К МАТЧУ КАК НАБЛЮДАТЕЛЬ!", Color.SkyBlue, 3.2f);
                        _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                        {
                            ["type"] = "join_in_progress",
                            ["stage"] = _run.Stage,
                            ["seed"] = _currentStageSeed,
                            ["special_mode"] = Physics.SpecialMode,
                            ["diff"] = GameData.SelectedDifficulty,
                            ["arts"] = string.Join(",", GameData.ActiveArtifacts),
                            ["elapsed"] = MathF.Round(_run.ElapsedTime, 1),
                            ["moon_det"] = MathF.Round(_run.MoonDetonationTimer, 1),
                            ["mithrix"] = _run.MithrixSpawned
                        });
                    }
                    else
                    {
                        ShowBanner($"ИГРОК [{pid.ToUpper()}] ВОШЁЛ В ЛОББИ!", Color.SkyBlue, 2.5f);
                        BroadcastLobbyState();
                    }
                }
                else if (mtype == "join_in_progress" && _net.Mode == "client" && _screen != "in_game")
                {
                    int st = msg["stage"]?.GetValue<int>() ?? 1;
                    int seed = msg["seed"]?.GetValue<int>() ?? 1001;
                    string? spMode = msg["special_mode"]?.GetValue<string>();
                    string? startDiff = msg["diff"]?.GetValue<string>();
                    string? startArts = msg["arts"]?.GetValue<string>();
                    GameData.SetNetClientOverride(startDiff, startArts != null ? startArts.Split(',', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>());
                    StartRun(_selectedChar, seed, fromNetwork: true, startStage: st, startSpecialMode: spMode, asDedicatedSpectator: true);
                    if (_run != null)
                    {
                        _run.ElapsedTime = msg["elapsed"]?.GetValue<float>() ?? 0f;
                        _run.MoonDetonationTimer = msg["moon_det"]?.GetValue<float>() ?? 0f;
                        if (msg["mithrix"]?.GetValue<bool>() == true && !_run.MithrixSpawned)
                            _run.StartMithrixEncounter();
                    }
                    ShowBanner("ПОДКЛЮЧЕНИЕ К ИДУЩЕМУ ЗАБЕГУ :: РЕЖИМ ЗРИТЕЛЯ (НАБЛЮДАТЕЛЬ)!", Color.SkyBlue, 4.5f);
                }
                else if (mtype == "player_left")
                {
                    string pid = msg["player_id"]?.GetValue<string>() ?? "";
                    _lobbyPlayers.Remove(pid);
                    if (_remotePlayers.TryGetValue(pid, out var rp))
                    {
                        Destroy(rp);
                        _remotePlayers.Remove(pid);
                    }
                    ShowBanner($"ИГРОК [{pid.ToUpper()}] ПОКИНУЛ ИГРУ", Color.Orange, 2.5f);
                }
                else if (mtype == "disconnected")
                {
                    _lobbyPlayers.Clear();
                    if (_screen == "in_game") QuitRunToLobby();
                    _screen = "main";
                    ShowBanner("СОЕДИНЕНИЕ С ХОСТОМ ПОТЕРЯНО", Color.Red, 3.5f);
                }
                else if (mtype == "lobby_state")
                {
                    string pid = msg["player_id"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(pid) && pid != _net.MyId)
                    {
                        bool isNew = !_lobbyPlayers.ContainsKey(pid);
                        string remoteChar = msg["char_key"]?.GetValue<string>() ?? "commando";
                        bool remoteHasBird = msg["has_bird_trio"]?.GetValue<bool>() ?? false;
                        bool remoteSpec = msg["spectator"]?.GetValue<bool>() ?? false;
                        string remoteName = msg["player_name"]?.GetValue<string>() ?? "";
                        if (string.IsNullOrWhiteSpace(remoteName) && _lobbyPlayers.TryGetValue(pid, out var prevLp))
                            remoteName = prevLp.PlayerName;
                        _lobbyPlayers[pid] = (
                            remoteChar,
                            msg["ready"]?.GetValue<bool>() ?? false,
                            remoteHasBird,
                            remoteSpec,
                            remoteName
                        );
                        if (isNew) BroadcastLobbyState();
                        if (_selectedChar == "apocalypse_bird" && remoteChar == "apocalypse_bird" && string.CompareOrdinal(_net.MyId, pid) > 0)
                        {
                            _myReady = false;
                            SelectCharacter("commando");
                            ShowBanner("В ЛОББИ МОЖЕТ БЫТЬ ТОЛЬКО ОДНА ПТИЦА АПОКАЛИПСИСА!", Color.Orange, 3.5f);
                        }
                        EvaluateApocalypseBirdLobby();
                        if (_net.Mode == "client" && pid == "host")
                        {
                            string? hostDiff = msg["diff"]?.GetValue<string>();
                            string? hostArts = msg["arts"]?.GetValue<string>();
                            GameData.SetNetClientOverride(hostDiff, hostArts != null ? hostArts.Split(',', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>());
                        }
                        EvaluateLobbyCountdown();
                    }
                }
                else if (mtype == "lobby_start_run")
                {
                    int st = msg["stage"]?.GetValue<int>() ?? 1;
                    int seed = msg["seed"]?.GetValue<int>() ?? 1001;
                    string? startDiff = msg["diff"]?.GetValue<string>();
                    string? startArts = msg["arts"]?.GetValue<string>();
                    if (_net.Mode == "client")
                        GameData.SetNetClientOverride(startDiff, startArts != null ? startArts.Split(',', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>());
                    if (_screen != "in_game")
                        StartRun(_selectedChar, seed, fromNetwork: true, startStage: st, asDedicatedSpectator: _lobbyJoinAsSpectator);
                }
                else if (mtype == "player_state" && _screen == "in_game" && _run != null)
                {
                    string pid = msg["player_id"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(pid) && pid != _net.MyId)
                    {
                        if (!_remotePlayers.TryGetValue(pid, out var rp) || !IsValid(rp))
                        {
                            rp = new RemotePlayer(pid, msg["char_key"]?.GetValue<string>() ?? "commando", msg["skin_idx"]?.GetValue<int>() ?? 0);
                            _remotePlayers[pid] = rp;
                            _run.StageEntities.Add(rp);
                        }
                        rp.ApplyNetState(msg);
                        if (rp.IsSpectator)
                        {
                            rp.MarkDead();
                            rp.IsSpectator = true;
                        }
                        else if (rp.Hp <= 0f)
                        {
                            _deadCoopPlayers.Add(pid);
                            rp.MarkDead();
                        }
                        else if (_deadCoopPlayers.Contains(pid))
                        {
                            rp.MarkDead();
                        }
                    }
                }
                else if (mtype == "enemy_spawn" && _screen == "in_game" && _run != null)
                {
                    int netId = msg["id"]?.GetValue<int>() ?? 0;
                    string et = msg["et"]?.GetValue<string>() ?? "lemurian";
                    float coeff = msg["coeff"]?.GetValue<float>() ?? _run.DifficultyCoeff;
                    bool isBoss = msg["boss"]?.GetValue<bool>() ?? false;
                    Vector3 spawnPos = Vector3.Zero;
                    if (msg["pos"] is System.Text.Json.Nodes.JsonArray parr && parr.Count >= 3)
                    {
                        spawnPos = new Vector3(
                            parr[0]?.GetValue<float>() ?? 0f,
                            parr[1]?.GetValue<float>() ?? 0f,
                            parr[2]?.GetValue<float>() ?? 0f
                        );
                    }
                    if (!CombatManager.Enemies.Exists(e => IsValid(e) && e.NetId == netId))
                    {
                        if (isBoss)
                        {
                            _run.SpawnBoss(spawnPos, et, netId, fromNetwork: true);
                        }
                        else
                        {
                            var ne = new CombatTarget(spawnPos, et, coeff, netId);
                            if (msg["doppel"]?.GetValue<bool>() == true) ne.MakeDoppelganger();
                            else if (msg["elite"]?.GetValue<bool>() == true) ne.PromoteToElite();
                            CombatManager.Enemies.Add(ne);
                            _run.StageEntities.Add(ne);
                        }
                    }
                }
                else if (mtype == "enemies_sync" && _screen == "in_game" && _run != null && _net.Mode == "client")
                {
                    if (_run.ActiveTeleporterOrPad is Teleporter tpSync && IsValid(tpSync))
                    {
                        string? tpSt = msg["tp_state"]?.GetValue<string>();
                        float? tpCh = msg["tp_charge"]?.GetValue<float>();
                        bool tpAl = msg["tp_aligned"]?.GetValue<bool>() ?? tpSync.AlignedToMoon;
                        if (tpAl != tpSync.AlignedToMoon)
                            tpSync.SetAlignment(tpAl);
                        if (tpSt == "charging" && tpSync.State == "idle")
                            tpSync.State = "charging";
                        if (tpCh.HasValue && tpSync.State == "charging")
                            tpSync.Charge = MathF.Max(tpSync.Charge, tpCh.Value);
                    }

                    if (msg["list"] is System.Text.Json.Nodes.JsonArray listArr)
                    {
                        foreach (var itemNode in listArr)
                        {
                            if (itemNode is not System.Text.Json.Nodes.JsonObject eo) continue;
                            int id = eo["id"]?.GetValue<int>() ?? 0;
                            string et = eo["et"]?.GetValue<string>() ?? "lemurian";
                            float coeff = eo["c"]?.GetValue<float>() ?? _run.DifficultyCoeff;
                            float hp = eo["hp"]?.GetValue<float>() ?? 10f;
                            float x = eo["x"]?.GetValue<float>() ?? 0f;
                            float y = eo["y"]?.GetValue<float>() ?? 0f;
                            float z = eo["z"]?.GetValue<float>() ?? 0f;
                            bool isBoss = eo["b"]?.GetValue<bool>() ?? false;
                            bool isAlly = eo["a"]?.GetValue<bool>() ?? false;

                            var existing = CombatManager.Enemies.Find(e => IsValid(e) && e.NetId == id);
                            if (existing == null)
                            {
                                if (et == "mithrix")
                                {
                                    if (!_run.MithrixSpawned) _run.StartMithrixEncounter();
                                    existing = _run.ActiveBoss;
                                }
                                else if (isBoss)
                                {
                                    _run.SpawnBoss(new Vector3(x, y, z), et, id, fromNetwork: true);
                                    existing = _run.ActiveBoss;
                                }
                                else
                                {
                                    existing = new CombatTarget(new Vector3(x, y, z), et, coeff, id);
                                    CombatManager.Enemies.Add(existing);
                                    _run.StageEntities.Add(existing);
                                }
                            }
                            if (existing != null && IsValid(existing))
                            {
                                existing.Position = Lerp(existing.Position, new Vector3(x, y, z), 0.45f);
                                existing.Hp = hp;
                                float? netMaxHp = eo["mhp"]?.GetValue<float>();
                                if (netMaxHp.HasValue && netMaxHp.Value > 0f) existing.MaxHp = netMaxHp.Value;
                                int? netPh = eo["ph"]?.GetValue<int>();
                                if (netPh.HasValue && netPh.Value >= 1) existing.Phase = netPh.Value;
                                if (isAlly && !existing.IsAlly) existing.ConvertToAlly();
                                if (eo["dp"]?.GetValue<bool>() == true && !existing.IsDoppelganger) existing.MakeDoppelganger();
                                else if (eo["el"]?.GetValue<bool>() == true && !existing.IsElite) existing.PromoteToElite(preserveHpRatio: true);
                                existing.UpdateHpBillboard();
                            }
                        }
                    }
                }
                else if (mtype == "enemy_hit" && _screen == "in_game" && _run != null)
                {
                    int id = msg["id"]?.GetValue<int>() ?? 0;
                    float dmg = msg["dmg"]?.GetValue<float>() ?? 0f;
                    var target = CombatManager.Enemies.Find(e => IsValid(e) && e.NetId == id);
                    if (target != null)
                        target.TakeDamage(dmg, fromNetwork: true);
                }
                else if (mtype == "enemy_kill" && _screen == "in_game" && _run != null)
                {
                    int id = msg["id"]?.GetValue<int>() ?? 0;
                    var target = CombatManager.Enemies.Find(e => IsValid(e) && e.NetId == id);
                    if (target != null)
                        target.Die(fromNetwork: true);
                }
                else if (mtype == "tp_state" && _screen == "in_game" && _run != null)
                {
                    string act = msg["action"]?.GetValue<string>() ?? "";
                    bool aligned = msg["aligned"]?.GetValue<bool>() ?? false;
                    if (_run.ActiveTeleporterOrPad is Teleporter tp)
                    {
                        if (act == "align")
                            tp.SetAlignment(aligned);
                        else if (act == "activate" && tp.State == "idle")
                        {
                            tp.SetAlignment(aligned);
                            tp.State = "charging";
                            _run.OnTeleporterActivated();
                        }
                    }
                }
                else if (mtype == "stage_sync" && _screen == "in_game" && _run != null)
                {
                    int newStage = msg["stage"]?.GetValue<int>() ?? _run.Stage;
                    int newSeed = msg["seed"]?.GetValue<int>() ?? 1001;
                    string? newMode = msg["special_mode"]?.GetValue<string>();
                    LoadStage(newStage, newMode, newSeed, fromNetwork: true);
                }
                else if (mtype == "mithrix_start" && _screen == "in_game" && _run != null)
                {
                    _run.StartMithrixEncounter();
                }
                else if (mtype == "dante_buff" && _screen == "in_game" && _run != null)
                {
                    string kind = msg["kind"]?.GetValue<string>() ?? "atk";
                    if (kind == "atk")
                    {
                        _run.Combat.DanteAtkBuffTimer = 7.0f;
                        ShowBanner("ПРИКАЗ ДАНТЕ :: +45% К УРОНУ И +35% К СКОРОСТИ АТАКИ!", Color.Gold, 2.5f);
                    }
                    else
                    {
                        _run.Combat.DanteSpdBuffTimer = 6.0f;
                        _run.HealPlayer(_run.PlayerMaxHp * 0.30f);
                        _run.Combat.Shield = MathF.Min(_run.PlayerMaxHp, _run.Combat.Shield + 45f);
                        ShowBanner("ПРОТОКОЛ ДАНТЕ :: +45 ЩИТА, ИСЦЕЛЕНИЕ И УСКОРЕНИЕ!", Color.Gold, 2.5f);
                    }
                }
                else if (mtype == "dante_revive" && _screen == "in_game" && _run != null)
                {
                    _deadCoopPlayers.Clear();
                    if (!_isDedicatedSpectator)
                    {
                        _commandPickerQueue.Clear();
                        _isSpectating = false;
                        _run.PlayerPos = GetNearestRemotePlayerPos(Vector3.Zero, out _);
                        _run.PlayerHp = _run.PlayerMaxHp;
                        _run.Combat.Shield = 50f;
                        _run.Parts.Root.Enabled = !SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey);
                        if (_run.Combat.FpRoot != null) _run.Combat.FpRoot.Enabled = true;
                        RestoreInGameCursorIfNeeded();
                        new VisualFX(_run.PlayerPos + new Vector3(0, 1.2f, 0), Color.Gold, 6.0f, 0.45f);
                        ShowBanner("ДАНТЕ ПОВЕРНУЛ ЧАСЫ ВСПЯТЬ И ВОЗРОДИЛ ВАС ЦЕНОЙ СВОЕЙ ЖИЗНИ!", Color.Gold, 4.5f);
                    }
                }
                else if (mtype == "mindustry_build" && _screen == "in_game" && _run != null)
                {
                    int sid = msg["id"]?.GetValue<int>() ?? 5500;
                    string btype = msg["btype"]?.GetValue<string>() ?? "spectre";
                    float ownerMaxHp = msg["max_hp"]?.GetValue<float>() ?? 160f;
                    Vector3 bPos = Vector3.Zero;
                    if (msg["pos"] is System.Text.Json.Nodes.JsonArray parr && parr.Count >= 3)
                    {
                        bPos = new Vector3(
                            parr[0]?.GetValue<float>() ?? 0f,
                            parr[1]?.GetValue<float>() ?? 0f,
                            parr[2]?.GetValue<float>() ?? 0f
                        );
                    }
                    _run.Combat.MindustryTurrets.RemoveAll(t => !IsValid(t));
                    var remoteStruct = new MindustryTurret(bPos, ownerMaxHp, _run.Combat.MindustryTurrets.Count, btype, isRemoteVisual: true, forcedNetId: sid);
                    _run.Combat.MindustryTurrets.Add(remoteStruct);
                    _run.StageEntities.Add(remoteStruct);
                    new VisualFX(bPos + new Vector3(0, 1.0f, 0), Color.Gold, 3.0f, 0.3f);
                }
                else if (mtype == "mindustry_hit" && _screen == "in_game" && _run != null)
                {
                    int sid = msg["id"]?.GetValue<int>() ?? -1;
                    float dmg = msg["dmg"]?.GetValue<float>() ?? 10f;
                    var st = _run.Combat.MindustryTurrets.Find(t => IsValid(t) && t.StructNetId == sid);
                    if (st != null)
                        st.TakeStructureDamage(dmg, fromNetwork: true);
                }
                else if (mtype == "pvp_hit" && _screen == "in_game" && _run != null)
                {
                    string targetId = msg["target_id"]?.GetValue<string>() ?? "";
                    float dmg = msg["dmg"]?.GetValue<float>() ?? 10f;
                    if (targetId == _net.MyId && !_isSpectating)
                        _run.Combat.TakeDamage(dmg, null, "pvp");
                }
                else if (mtype == "pvp_eliminated" && _screen == "in_game" && _run != null)
                {
                    string loserId = msg["player_id"]?.GetValue<string>() ?? "враг";
                    if (_remotePlayers.TryGetValue(loserId, out var rp)) rp.Hp = 0f;
                    bool anyOtherAlive = false;
                    foreach (var kv in _remotePlayers)
                        if (kv.Key != loserId && IsValid(kv.Value) && kv.Value.Hp > 0f) { anyOtherAlive = true; break; }
                    if (!anyOtherAlive && !_isSpectating)
                    {
                        FinishRunWithSummary(victory: true);
                        ShowBanner($"ПОБЕДА НА АРЕНЕ PvP! СОПЕРНИК [{loserId.ToUpper()}] ПОВЕРЖЕН!", Color.Gold, 6.5f);
                    }
                }
                else if (mtype == "ally_heal" && _screen == "in_game" && _run != null)
                {
                    string tid = msg["target_id"]?.GetValue<string>() ?? "";
                    if (tid == _net.MyId || tid == "all")
                    {
                        float hAmt = msg["heal"]?.GetValue<float>() ?? 20f;
                        bool uber = msg["uber"]?.GetValue<bool>() ?? false;
                        _run.HealPlayer(hAmt);
                        if (uber)
                        {
                            _run.Combat.UberTimer = 6.5f;
                            _run.Combat.Shield = MathF.Min(_run.PlayerMaxHp, _run.Combat.Shield + 50f);
                            ShowBanner("МЕДИК АКТИВИРОВАЛ НА ВАС УБЕР-ЗАРЯД (6.5 СЕК НЕУЯЗВИМОСТИ)!", Rgb(255, 75, 90), 3.0f);
                        }
                        else
                        {
                            new VisualFX(_run.PlayerPos + new Vector3(0, 1.2f, 0), Color.Lime, 2.2f, 0.25f);
                        }
                    }
                }
                else if (mtype == "chest_open" && _screen == "in_game" && _run != null)
                {
                    int cid = msg["id"]?.GetValue<int>() ?? -1;
                    string itemK = msg["item"]?.GetValue<string>() ?? "syringe";
                    foreach (var ent in _run.StageEntities)
                    {
                        if (ent is Chest ch && IsValid(ch) && ch.ChestId == cid && !ch.Opened)
                        {
                            ch.OpenVisual(itemK, spawnDrop: false);
                            break;
                        }
                    }
                }
                else if (mtype == "pillar_state" && _screen == "in_game" && _run != null)
                {
                    string ptype = msg["ptype"]?.GetValue<string>() ?? "";
                    string pstate = msg["state"]?.GetValue<string>() ?? "";
                    foreach (var ent in _run.StageEntities)
                    {
                        if (ent is MoonPillar mp && IsValid(mp) && mp.PillarType == ptype)
                        {
                            if (pstate == "charging") mp.ActivatePillar(fromNetwork: true);
                            else if (pstate == "done") mp.CompletePillar(fromNetwork: true);
                            break;
                        }
                    }
                }
                else if (mtype == "player_dead" && _screen == "in_game")
                {
                    string deadPid = msg["player_id"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(deadPid))
                    {
                        _deadCoopPlayers.Add(deadPid);
                        if (_remotePlayers.TryGetValue(deadPid, out var deadRp))
                            deadRp.MarkDead();
                    }
                    if (IsSpectating)
                    {
                        bool anyAlive = false;
                        foreach (var kv in _remotePlayers)
                        {
                            if (!_deadCoopPlayers.Contains(kv.Key) && kv.Value.IsAliveCombatant)
                            {
                                anyAlive = true;
                                break;
                            }
                        }
                        if (!anyAlive && _spectateStageGraceTimer <= 0f)
                        {
                            _isSpectating = false;
                            _isDedicatedSpectator = false;
                            FinishRunWithSummary(victory: false);
                            ShowBanner("ВСЕ ИГРОКИ ПОГИБЛИ! ЗАБЕГ ОКОНЧЕН", Color.Red, 5.5f);
                        }
                    }
                }
                else if (mtype == "all_players_dead" && _screen == "in_game")
                {
                    _isSpectating = false;
                    _isDedicatedSpectator = false;
                    FinishRunWithSummary(victory: false);
                    ShowBanner("ВСЕ ИГРОКИ ПОГИБЛИ! ЗАБЕГ ОКОНЧЕН", Color.Red, 5.5f);
                }
                else if (mtype == "item_pickup" && _screen == "in_game" && _run != null)
                {
                    string ik = msg["item"]?.GetValue<string>() ?? "";
                    float px = msg["x"]?.GetValue<float>() ?? 0f;
                    float pz = msg["z"]?.GetValue<float>() ?? 0f;
                    ItemDrop? bestDrop = null;
                    float bestD = 3.0f;
                    foreach (var se in _run.StageEntities)
                    {
                        if (se is ItemDrop idr && IsValid(idr) && (string.IsNullOrEmpty(ik) || idr.ItemKey == ik))
                        {
                            float d = Physics.Distance2D(idr.Position, new Vector3(px, 0f, pz));
                            if (d <= bestD) { bestD = d; bestDrop = idr; }
                        }
                    }
                    if (bestDrop != null)
                    {
                        new VisualFX(bestDrop.Position, bestDrop.Info.Color, 2.4f, 0.26f);
                        _run.StageEntities.Remove(bestDrop);
                        Destroy(bestDrop);
                    }
                }
                else if (mtype == "proj_fx" && _screen == "in_game" && _run != null)
                {
                    Vector3 pPos = new(msg["x"]?.GetValue<float>() ?? 0f, msg["y"]?.GetValue<float>() ?? 0f, msg["z"]?.GetValue<float>() ?? 0f);
                    Vector3 pDir = new(msg["dx"]?.GetValue<float>() ?? 0f, msg["dy"]?.GetValue<float>() ?? 0f, msg["dz"]?.GetValue<float>() ?? 1f);
                    float spd = msg["spd"]?.GetValue<float>() ?? 45f;
                    float scl = msg["s"]?.GetValue<float>() ?? 0.25f;
                    byte cr = (byte)Math.Clamp(msg["r"]?.GetValue<int>() ?? 255, 0, 255);
                    byte cg = (byte)Math.Clamp(msg["g"]?.GetValue<int>() ?? 220, 0, 255);
                    byte cb = (byte)Math.Clamp(msg["b"]?.GetValue<int>() ?? 80, 0, 255);
                    float life = msg["life"]?.GetValue<float>() ?? 1.5f;
                    _run.StageEntities.Add(new RemoteVisualProjectile(pPos, pDir, spd, scl, Rgb(cr, cg, cb), life));
                }
                else if (mtype == "item_drop" && _screen == "in_game" && _run != null)
                {
                    string ik = msg["item"]?.GetValue<string>() ?? "syringe";
                    if (msg["pos"] is System.Text.Json.Nodes.JsonArray parr && parr.Count >= 3)
                    {
                        Vector3 dPos = new(
                            parr[0]?.GetValue<float>() ?? 0f,
                            parr[1]?.GetValue<float>() ?? 0f,
                            parr[2]?.GetValue<float>() ?? 0f
                        );
                        _run.SpawnItemDropNet(dPos, ik);
                    }
                }
                else if (mtype == "run_victory" && _screen == "in_game")
                {
                    FinishRunWithSummary(victory: true);
                    ShowBanner("ПОБЕДА! КОМАНДА ЭВАКУИРОВАЛАСЬ С ЛУНЫ!", Color.Gold, 6.5f);
                }
                else if (mtype == "horde_boss_spawn" && _screen == "in_game" && _run != null)
                {
                    string sp = msg["species"]?.GetValue<string>() ?? "elder_lemurian";
                    float totHp = msg["total_hp"]?.GetValue<float>() ?? 2400f;
                    var idsList = new List<int>();
                    var posList = new List<Vector3>();
                    if (msg["ids"] is System.Text.Json.Nodes.JsonArray idArr)
                        foreach (var n in idArr) idsList.Add(n?.GetValue<int>() ?? 0);
                    if (msg["positions"] is System.Text.Json.Nodes.JsonArray pArr)
                    {
                        foreach (var pn in pArr)
                        {
                            if (pn is System.Text.Json.Nodes.JsonArray c3 && c3.Count >= 3)
                                posList.Add(new Vector3(c3[0]?.GetValue<float>() ?? 0f, c3[1]?.GetValue<float>() ?? 0f, c3[2]?.GetValue<float>() ?? 0f));
                        }
                    }
                    _run.SpawnHordeOfMany(Vector3.Zero, sp, idsList.ToArray(), posList.ToArray(), totHp, fromNetwork: true);
                    ShowBanner($"ПРОБУДИЛАСЬ {_run.HordeTitle}!", Rgb(255, 145, 45), 3.8f);
                }
                else if (mtype == "rps_duel" && _screen == "in_game" && _run != null)
                {
                    HandleNetworkRpsDuel(msg);
                }
            }
        }

        private static bool IsApocalypseBirdTakenByAnother()
        {
            if (!_net.Connected) return false;
            foreach (var kv in _lobbyPlayers)
                if (kv.Value.CharKey == "apocalypse_bird") return true;
            return false;
        }

        private static void EvaluateApocalypseBirdLobby()
        {
            if (!_net.Connected) return;
            var prog = Data.ProgressData.Instance;
            int totalPlayers = 1 + _lobbyPlayers.Count;
            int birdOwners = prog.IsCharacterUnlocked("bird_trio") ? 1 : 0;
            foreach (var v in _lobbyPlayers.Values)
                if (v.HasBirdTrio) birdOwners++;

            if (totalPlayers >= 3 && birdOwners >= 3 && !prog.IsCharacterUnlocked("apocalypse_bird"))
            {
                prog.UnlockedCharacters.Add("apocalypse_bird");
                prog.UnlockedAchievements.Add("ach_char_apocalypse_bird");
                prog.Save();
                SoundManager.Play("victory", 1.0f);
                ShowBanner("★ ВРАТА ЧЁРНОГО ЛЕСА ОТКРЫТЫ! НАВСЕГДА РАЗБЛОКИРОВАНА ПТИЦА АПОКАЛИПСИСА! ★", Color.Gold, 6.5f);
            }
        }

        private static void BroadcastLobbyState()
        {
            if (!_net.Connected) return;
            string netChar = Data.ProgressData.Instance.IsCharacterUnlocked(_selectedChar) ? _selectedChar : "commando";
            _net.Broadcast(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "lobby_state",
                ["player_id"] = _net.MyId,
                ["player_name"] = Data.ProgressData.Instance.PlayerName,
                ["char_key"] = netChar,
                ["ready"] = _myReady,
                ["spectator"] = _net.Mode == "client" && _net.GameType == "coop" && _lobbyJoinAsSpectator,
                ["has_bird_trio"] = Data.ProgressData.Instance.IsCharacterUnlocked("bird_trio"),
                ["skin_idx"] = Data.ProgressData.Instance.GetSelectedSkin(netChar),
                ["diff"] = GameData.SelectedDifficulty,
                ["arts"] = string.Join(",", GameData.ActiveArtifacts)
            });
            EvaluateApocalypseBirdLobby();
        }

        private static void EvaluateLobbyCountdown()
        {
            if (!_net.Connected) return;
            int totalP = 1 + _lobbyPlayers.Count;
            int readyP = (_myReady ? 1 : 0);
            foreach (var v in _lobbyPlayers.Values) if (v.Ready) readyP++;
            if (readyP >= 1 && !_lobbyCountdownActive)
            {
                _lobbyCountdownActive = true;
                _lobbyCountdown = 60f;
            }
            if (readyP >= totalP && _lobbyCountdownActive)
                _lobbyCountdown = MathF.Min(_lobbyCountdown, 3.0f);
        }

        private static void SetupDiorama()
        {
            _mainPedestal = new Entity(position: new(1.35f, 0f, -1.0f));
            new Entity(parent: _mainPedestal, model: "cube", scale: new(3.4f, 0.35f, 3.4f), position: new(0, -0.18f, 0), color: Rgb(34, 42, 58));
            new Entity(parent: _mainPedestal, model: "cube", scale: new(2.8f, 0.14f, 2.8f), position: new(0, 0.04f, 0), color: Rgb(55, 70, 96));
            _pedestalRing = new Entity(parent: _mainPedestal, model: "cube", scale: new(2.9f, 0.05f, 2.9f), position: new(0, 0.10f, 0), color: Color.SkyBlue, unlit: true);

            _previewActor = new Entity(parent: _mainPedestal, position: new(0, 0.12f, 0), rotation: new(0, 15f, 0));
            _menuCrystal = new Entity(
                model: "diamond", scale: new(0.85f, 1.85f, 0.85f),
                position: new(1.35f, 3.3f, 2.2f), color: Rgb(60, 215, 255), unlit: true
            );

            string initialChar = ProgressData.Instance.IsCharacterUnlocked(_selectedChar) ? _selectedChar : "commando";
            SelectCharacter(initialChar);
        }

        private static void SelectCharacter(string charKey)
        {
            if (!SurvivorDatabase.Classes.ContainsKey(charKey)) return;
            _selectedChar = charKey;
            if (ProgressData.Instance.IsCharacterUnlocked(charKey))
            {
                Data.UserSettings.Instance.SelectedCharacter = charKey;
                Data.UserSettings.Instance.Save();
                if (ProgressData.Instance.SelectedCharacter != charKey)
                {
                    ProgressData.Instance.SelectedCharacter = charKey;
                    ProgressData.Instance.Save();
                }
            }
            ClearChildren(_previewActor);
            if (IsValid(_activeSkillDemo)) { Destroy(_activeSkillDemo!); _activeSkillDemo = null; }
            _skillPreviewTimer = 0f;
            _previewEnemyParts = null;
            _previewBaseScale = 1.0f;
            _previewActor.Position = new Vector3(0, 0.12f, 0);
            _previewActor.Rotation = new Vector3(0, 15f, 0);

            var cfg = SurvivorDatabase.Classes[charKey];
            cfg.SyncLoadout();
            var activeSkin = cfg.GetActiveSkin();
            _pedestalRing.Color = activeSkin.PrimaryColor;
            _previewSurvivorParts = SurvivorDatabase.BuildSurvivorMesh(_previewActor, charKey);
            new VisualFX(_mainPedestal.Position + new Vector3(0, 1.2f, 0), cfg.BodyColor, maxScale: 3.0f, duration: 0.25f);
            if (_screen == "lobby") SoundManager.Play("ui_select_char");
            BroadcastLobbyState();
        }

        private static void SelectLogbookEntry(string category, string key)
        {
            _logCategory = category;
            _logSelectedKey = key;
            ClearChildren(_previewActor);
            _previewSurvivorParts = null;
            if (IsValid(_activeSkillDemo)) { Destroy(_activeSkillDemo!); _activeSkillDemo = null; }
            _skillPreviewTimer = 0f;
            _previewEnemyParts = null;
            _previewActor.Rotation = new Vector3(0, 20f, 0);

            if (category == "items" && GameData.ItemPool.TryGetValue(key, out var item))
            {
                bool unl = ProgressData.Instance.UnlockedItems.Contains(key);
                _previewBaseScale = 1.85f;
                _previewActor.Position = new Vector3(0, 1.35f, 0);
                _pedestalRing.Color = unl ? item.Color : Color.Gray;
                if (unl)
                    ItemBuilder.BuildItemMesh(_previewActor, key);
                else
                    new Entity(parent: _previewActor, model: "diamond", scale: new(0.65f, 1.05f, 0.65f), color: Rgb(65, 72, 88), unlit: true);
            }
            else if (category == "survivors" && SurvivorDatabase.Classes.TryGetValue(key, out var surv))
            {
                bool unl = ProgressData.Instance.IsCharacterUnlocked(key);
                _previewBaseScale = 1.0f;
                _previewActor.Position = new Vector3(0, 0.12f, 0);
                _pedestalRing.Color = unl ? surv.BodyColor : Color.Gray;
                if (unl)
                    _previewSurvivorParts = SurvivorDatabase.BuildSurvivorMesh(_previewActor, key);
                else
                    new Entity(parent: _previewActor, model: "diamond", scale: new(0.85f, 1.65f, 0.85f), position: new(0, 1.1f, 0), color: Rgb(65, 72, 88), unlit: true);
            }
            else if (category == "monsters")
            {
                _previewBaseScale = key is "parent" or "elder_lemurian" ? 0.58f : (key is "golem" or "greater_wisp" or "lunar_chimera" or "bison" or "brass_contraption" ? 0.76f : 0.95f);
                float yOff = key is "wisp" or "jellyfish" or "greater_wisp" or "lunar_chimera" or "brass_contraption" or "hell_drone" or "nether_blaze" ? -0.75f : 0.12f;
                _previewActor.Position = new Vector3(0, yOff, 0);
                _pedestalRing.Color = Rgb(255, 140, 50);
                _previewEnemyParts = EnemyAndBossBuilder.BuildEnemyMesh(_previewActor, key);
            }
            else if (category == "bosses")
            {
                _previewBaseScale = key == "magma_worm" ? 0.42f : (key is "great_septimus" or "prts_source" ? 0.44f : 0.48f);
                float yOff = key is "vagrant" or "solus_control" or "prts_source" or "great_septimus" ? -0.35f : (key == "magma_worm" ? 0.35f : 0.12f);
                _previewActor.Position = new Vector3(0, yOff, 0);
                _pedestalRing.Color = key == "mithrix" ? Color.SkyBlue : Color.Red;
                _previewEnemyParts = EnemyAndBossBuilder.BuildEnemyMesh(_previewActor, key);
                if (key == "mithrix" && _previewEnemyParts.PhaseRing != null)
                    _previewEnemyParts.PhaseRing.Enabled = true;
            }
            else if (category == "stages")
            {
                bool disc = ProgressData.Instance.DiscoveredStages.Contains(key) || (key == "safe_travels" && IsSafeTravelsUnlocked());
                var pal = GameData.StagePalettes.Find(p => p.Id == key)
                          ?? (GameData.SpecialPalettes.TryGetValue(key, out var sp) ? sp : GameData.StagePalettes[0]);
                _previewBaseScale = 0.24f;
                _previewActor.Position = new Vector3(0, 0.45f, 0);
                _pedestalRing.Color = disc ? pal.Accent : Color.Gray;
                if (!disc)
                {
                    new Entity(parent: _previewActor, model: "diamond", scale: new(3.2f, 4.8f, 3.2f), position: new(0, 2.5f, 0), color: Rgb(65, 72, 88), unlit: true);
                    return;
                }
                var root = new Entity(parent: _previewActor);
                new Entity(parent: root, model: "sphere", scale: new(9.5f, 1.2f, 9.5f), position: new(0, -0.4f, 0), color: pal.Ground);
                new Entity(parent: root, model: "diamond", scale: new(10.0f, 0.25f, 10.0f), color: pal.Accent, unlit: true);
                if (key == "safe_travels")
                {
                    new Entity(parent: root, model: "cube", scale: new(2.6f, 1.2f, 7.6f), position: new(0, 2.2f, 0), color: Rgb(185, 200, 222));
                    new Entity(parent: root, model: "cube", scale: new(4.8f, 0.4f, 2.8f), position: new(0, 2.1f, -1.2f), color: Rgb(95, 118, 152));
                    new Entity(parent: root, model: "cube", scale: new(1.6f, 0.7f, 2.0f), position: new(0, 3.05f, 2.1f), color: Rgb(80, 220, 255), unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        new Entity(parent: root, model: "sphere", scale: new(0.95f, 0.95f, 1.8f), position: new(s * 1.65f, 2.1f, -3.8f), color: Rgb(75, 225, 255), unlit: true);
                }
                else
                {
                    string stType = key switch
                    {
                        "ionia" or "hakurei_shrine" => "torii",
                        "sundered" => "giant_mushroom",
                        "rallypoint" => "ice_spire",
                        "sulfur" or "black_mesa" or "hell_cybergrind" => "sulfur_geyser",
                        "aqueduct" or "tartarus" => "aqueduct_pillar",
                        "aphelian" or "tracen_derby" => "sun_temple",
                        "wetland" => "swamp_root",
                        "meadow" => "astral_ring",
                        "roost" or "moon" or "qliphoth" => "arch",
                        "acres" or "bazaar" or "pizzeria" => "monolith",
                        "dark_sanctuary" => "dark_fountain_spire",
                        "terminal_lucidity" => "pipis_billboard",
                        "extraction_floor" => "qliphoth_cage",
                        "originium_core" => "originium_monolith",
                        "penacony_theater" => "penacony_organ",
                        "prime_meridian" => "aurelionite_geode",
                        _ => "crystals"
                    };
                    WorldStructuresBuilder.BuildMapStructure(root, stType, pal, Vector3.Zero, 25f);
                }
            }
            else if (category == "achievements" && GameData.Achievements.TryGetValue(key, out var ach))
            {
                bool unlocked = ProgressData.Instance.UnlockedAchievements.Contains(key);
                if (unlocked && !string.IsNullOrEmpty(ach.UnlocksCharacter) && SurvivorDatabase.Classes.TryGetValue(ach.UnlocksCharacter, out var achSurv))
                {
                    _previewBaseScale = 1.0f;
                    _previewActor.Position = new Vector3(0, 0.12f, 0);
                    _pedestalRing.Color = Color.Gold;
                    _previewSurvivorParts = SurvivorDatabase.BuildSurvivorMesh(_previewActor, ach.UnlocksCharacter);
                }
                else
                {
                    _previewBaseScale = 1.45f;
                    _previewActor.Position = new Vector3(0, 1.25f, 0);
                    _pedestalRing.Color = unlocked ? Color.Gold : Color.Gray;
                    if (unlocked && ach.UnlocksItem != null)
                        ItemBuilder.BuildItemMesh(_previewActor, ach.UnlocksItem);
                    else
                        new Entity(parent: _previewActor, model: "diamond", scale: new(0.75f, 1.15f, 0.75f), color: unlocked ? Color.Gold : Rgb(65, 72, 88), unlit: true);
                }
            }
            else if (category == "modifiers")
            {
                _previewBaseScale = 1.45f;
                _previewActor.Position = new Vector3(0, 1.25f, 0);
                Color modCol = GameData.RunArtifacts.TryGetValue(key, out var rArt)
                    ? rArt.Color
                    : (GameData.Difficulties.TryGetValue(key, out var dDef) ? dDef.Color : Color.Gold);
                _pedestalRing.Color = modCol;
                var relic = new Entity(parent: _previewActor);
                new Entity(parent: relic, model: "diamond", scale: new(0.82f, 1.28f, 0.82f), color: modCol, unlit: true);
                new Entity(parent: relic, model: "cube", scale: new(1.12f, 0.08f, 1.12f), rotation: new(0, 45f, 18f), color: Color.White, unlit: true);
                new Entity(parent: relic, model: "cube", scale: new(1.12f, 0.08f, 1.12f), rotation: new(18f, 45f, 0), color: modCol, unlit: true);
                new Entity(parent: relic, model: "sphere", scale: new(0.42f), color: Color.White, unlit: true);
            }
        }

        private static void TriggerLobbySkillPreview(string charKey, string slot, int variantIdx)
        {
            if (!SurvivorDatabase.Classes.TryGetValue(charKey, out var cfg)) return;
            cfg.SyncLoadout();
            SoundManager.Play("ui_ready");

            float animDur = slot switch
            {
                "r" => 0.95f,
                "shift" => 0.62f,
                "m2" => 0.52f,
                _ => 0.42f
            };
            _previewSurvivorParts?.TriggerSkillAnim(slot, animDur);
            _previewActor.Rotation = new Vector3(0f, 18f, 0f);
            _manualTimer = 1.85f;

            Color slotCol = slot switch
            {
                "m1" => Color.Yellow,
                "m2" => Color.Orange,
                "shift" => Color.SkyBlue,
                "r" => Color.Magenta,
                _ => Color.SkyBlue
            };
            string bindLabel = slot switch
            {
                "m1" => "ЛКМ",
                "m2" => "ПКМ",
                "shift" => "SHIFT",
                "r" => "R / УЛЬТА",
                _ => slot.ToUpper()
            };

            string skName = cfg.Skills.TryGetValue(slot, out var sk) ? sk.Name : slot.ToUpper();
            var activeVars = cfg.GetActiveSkin().CustomSkillVariants ?? cfg.SkillVariants;
            if (activeVars.TryGetValue(slot, out var vars) && variantIdx >= 0 && variantIdx < vars.Count)
                skName = vars[variantIdx].Name;

            _skillPreviewTitle = $"▶ АНИМАЦИЯ НАВЫКА [{bindLabel}]: {skName} {(variantIdx == 0 ? "[СТАНД.]" : "[АЛЬТ.]")}";
            _skillPreviewColor = slotCol;
            _skillPreviewTimer = 2.4f;

            if (IsValid(_activeSkillDemo))
                Destroy(_activeSkillDemo!);
            _activeSkillDemo = new LobbySkillDemoFX(_mainPedestal, slot, variantIdx, slotCol, cfg.GetActiveSkin().AccentColor);
        }

        private static void ShowKillerOnDiorama()
        {
            if (IsValid(_activeSkillDemo))
            {
                Destroy(_activeSkillDemo!);
                _activeSkillDemo = null;
            }
            ClearChildren(_previewActor);
            _previewSurvivorParts = null;
            if (IsValid(_activeSkillDemo)) { Destroy(_activeSkillDemo!); _activeSkillDemo = null; }
            _skillPreviewTimer = 0f;
            _previewEnemyParts = null;
            _previewActor.Rotation = new Vector3(0f, 20f, 0f);

            string key = string.IsNullOrEmpty(_lastKillerKey) ? "lemurian" : _lastKillerKey;
            _pedestalRing.Color = _lastKillerColor;

            if (key is "pvp_rival" or "chaos_self")
            {
                _previewBaseScale = 1.0f;
                _previewActor.Position = new Vector3(0f, 0.12f, 0f);
                _previewSurvivorParts = SurvivorDatabase.BuildSurvivorMesh(_previewActor, _selectedChar);
                new Entity(parent: _previewActor, model: "cube", scale: new(1.45f, 0.05f, 1.45f), position: new(0, 0.1f, 0), color: _lastKillerColor, unlit: true);
                return;
            }

            bool isBossModel = GameData.IsKnownBossType(key);
            if (isBossModel)
            {
                _previewBaseScale = key == "magma_worm" ? 0.42f : 0.48f;
                float yOff = key is "vagrant" or "solus_control" ? -0.35f : (key == "magma_worm" ? 0.35f : 0.12f);
                _previewActor.Position = new Vector3(0f, yOff, 0f);
            }
            else
            {
                _previewBaseScale = key is "parent" or "elder_lemurian" ? 0.58f : (key is "golem" or "greater_wisp" or "lunar_chimera" or "bison" or "brass_contraption" ? 0.76f : 0.95f);
                float yOff = key is "wisp" or "jellyfish" or "greater_wisp" or "lunar_chimera" or "brass_contraption" or "hell_drone" or "nether_blaze" ? -0.75f : 0.12f;
                _previewActor.Position = new Vector3(0f, yOff, 0f);
            }

            _previewEnemyParts = EnemyAndBossBuilder.BuildEnemyMesh(_previewActor, key);
            if (key == "mithrix" && _previewEnemyParts.PhaseRing != null)
                _previewEnemyParts.PhaseRing.Enabled = true;
            if (_lastKillerIsElite && _previewEnemyParts.Crown != null)
                _previewEnemyParts.Crown.Enabled = true;
            if (_lastKillerIsDoppel)
                new Entity(parent: _previewActor, model: "diamond", scale: new(1.35f, 0.12f, 1.35f), position: new(0, 0.25f, 0), color: Rgb(205, 65, 255), unlit: true);
        }

        private static void DrawKillerMobTopRightWidget(int x, int y, int w, bool allow3DToggle)
        {
            int h = allow3DToggle ? 176 : 138;
            Raylib.DrawRectangle(x, y, w, h, Rgb(14, 16, 28, 244));
            Raylib.DrawRectangle(x, y, w, 4, _lastKillerColor);
            Raylib.DrawRectangleLines(x, y, w, h, Rgb(_lastKillerColor.R, _lastKillerColor.G, _lastKillerColor.B, 165));

            // Верхняя плашка "КТО ВАС УБИЛ"
            Raylib.DrawRectangle(x + 12, y + 10, w - 24, 24, Rgb(42, 18, 26, 230));
            Engine.DrawTextLeft("☠ КТО ВАС УБИЛ (УБИЙЦА):", x + 20, y + 14, 14f, Rgb(255, 120, 120));
            Engine.DrawTextLeft($"УДАР: -{(int)MathF.Ceiling(_lastKillerDamage)} HP", x + w - 150, y + 14, 13f, Color.Gold);

            // Мини-эмблема моба слева
            int iconX = x + 14, iconY = y + 42, iconSz = 58;
            Raylib.DrawRectangle(iconX, iconY, iconSz, iconSz, Rgb(24, 28, 44, 250));
            Raylib.DrawRectangleLines(iconX, iconY, iconSz, iconSz, _lastKillerColor);
            Raylib.DrawCircle(iconX + iconSz / 2, iconY + iconSz / 2 - 4, 15f, Rgb(_lastKillerColor.R, _lastKillerColor.G, _lastKillerColor.B, 85));
            Raylib.DrawCircle(iconX + iconSz / 2 - 5, iconY + iconSz / 2 - 6, 3.2f, Color.White);
            Raylib.DrawCircle(iconX + iconSz / 2 + 5, iconY + iconSz / 2 - 6, 3.2f, Color.White);
            string badgeShort = _lastKillerIsBoss ? "БОСС" : (_lastKillerIsDoppel ? "ТЕНЬ" : (_lastKillerIsElite ? "ЭЛИТА" : "МОБ"));
            Engine.DrawTextCentered(badgeShort, iconX + iconSz / 2, iconY + iconSz - 11, 11f, _lastKillerColor);

            // Имя, ранг и краткое описание моба
            Engine.DrawTextLeft(_lastKillerName, iconX + iconSz + 12, y + 40, 20f, Color.White);
            Engine.DrawTextLeft(_lastKillerRank, iconX + iconSz + 12, y + 64, 13f, _lastKillerColor);

            string shortDesc = _lastKillerDesc.Length > 58 ? _lastKillerDesc[..55] + "..." : _lastKillerDesc;
            Engine.DrawTextLeft(shortDesc, iconX + iconSz + 12, y + 84, 13f, Rgb(195, 210, 232));

            if (allow3DToggle)
            {
                string toggleTxt = _summaryShowKiller3D
                    ? "НА 3D-ПОДИУМЕ: МОБ-УБИЙЦА (НАЖМИТЕ ДЛЯ ГЕРОЯ)"
                    : "НА 3D-ПОДИУМЕ: ВАШ ГЕРОЙ (НАЖМИТЕ ДЛЯ МОБА-УБИЙЦЫ)";
                if (Engine.DrawButton(new Rectangle(x + 14, y + 132, w - 28, 34), toggleTxt,
                    _summaryShowKiller3D ? Rgb(95, 34, 44) : Rgb(32, 68, 112),
                    Rgb(135, 52, 66), Color.White, 13f, _lastKillerColor))
                {
                    _summaryShowKiller3D = !_summaryShowKiller3D;
                    if (_summaryShowKiller3D) ShowKillerOnDiorama();
                    else SelectCharacter(_selectedChar);
                }
            }
        }

        private class LobbySkillDemoFX : Entity
        {
            private readonly string _slot;
            private readonly int _variant;
            private readonly Color _col;
            private readonly Color _accent;
            private readonly Entity _dummy;
            private readonly Entity _dummyBody;
            private readonly Entity _fxGroup;
            private readonly List<Entity> _projectiles = new();
            private readonly Entity _auraRing;
            private float _timer = 0f;
            private const float Duration = 1.45f;

            public LobbySkillDemoFX(Entity pedestal, string slot, int variant, Color slotCol, Color accentCol)
                : base(parent: pedestal, position: Vector3.Zero)
            {
                _slot = slot;
                _variant = variant;
                _col = variant == 0 ? slotCol : accentCol;
                _accent = accentCol;

                // Голографический манекен-мишень перед персонажем для наглядной демонстрации удара/выстрела
                _dummy = new Entity(parent: this, position: new(0.28f, 0.14f, 1.25f));
                new Entity(parent: _dummy, model: "cube", scale: new(0.68f, 0.04f, 0.68f), color: Rgb(255, 85, 85), unlit: true);
                _dummyBody = new Entity(parent: _dummy, model: "cube", scale: new(0.38f, 0.92f, 0.38f), position: new(0, 0.52f, 0), color: Rgb(220, 65, 75), unlit: true);
                new Entity(parent: _dummyBody, model: "sphere", scale: new(0.75f, 0.36f, 0.75f), position: new(0, 0.72f, 0), color: Color.White, unlit: true);

                _auraRing = new Entity(parent: this, model: "cube", scale: new(1.25f, 0.04f, 1.25f), position: new(0, 0.16f, 0), color: _col, unlit: true);
                _fxGroup = new Entity(parent: this, position: new(0f, 1.05f, 0.15f));

                if (_slot == "m1")
                {
                    int cnt = _variant == 0 ? 3 : 1;
                    for (int i = 0; i < cnt; i++)
                    {
                        var p = new Entity(parent: _fxGroup,
                            model: _variant == 0 ? "sphere" : "diamond",
                            scale: _variant == 0 ? new(0.22f, 0.22f, 0.42f) : new(0.55f, 0.18f, 0.65f),
                            position: new((i - (cnt - 1) * 0.5f) * 0.18f, 0, 0),
                            color: _col, unlit: true);
                        _projectiles.Add(p);
                    }
                }
                else if (_slot == "m2")
                {
                    if (_variant == 0)
                    {
                        var orb = new Entity(parent: _fxGroup, model: "diamond", scale: new(0.45f, 0.45f, 0.65f), color: _col, unlit: true);
                        new Entity(parent: orb, model: "sphere", scale: new(0.65f), color: Color.White, unlit: true);
                        _projectiles.Add(orb);
                    }
                    else
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            var beam = new Entity(parent: _fxGroup, model: "cube", scale: new(0.16f, 0.16f, 0.55f),
                                position: new(i * 0.28f, MathF.Abs(i) * 0.08f, 0), color: _col, unlit: true);
                            _projectiles.Add(beam);
                        }
                    }
                }
                else if (_slot == "shift")
                {
                    // Голографический след рывка / прыжка
                    for (int i = 0; i < 3; i++)
                    {
                        var clone = new Entity(parent: _fxGroup,
                            model: _variant == 0 ? "cube" : "diamond",
                            scale: new(0.42f, 0.85f - i * 0.15f, 0.22f),
                            position: new(0, -0.25f, -0.35f * i),
                            color: _col, unlit: true);
                        _projectiles.Add(clone);
                    }
                }
                else // "r" (Ульта)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = i * MathF.PI * 0.5f;
                        var sat = new Entity(parent: _fxGroup,
                            model: _variant == 0 ? "diamond" : "sphere",
                            scale: _variant == 0 ? new(0.32f, 0.75f, 0.32f) : new(0.38f, 0.38f, 0.38f),
                            position: new(MathF.Cos(ang) * 0.85f, 0.15f, MathF.Sin(ang) * 0.85f),
                            color: i % 2 == 0 ? _col : Color.Gold, unlit: true);
                        _projectiles.Add(sat);
                    }
                }
            }

            public override void OnUpdate(float dt)
            {
                _timer += dt;
                float p = Math.Clamp(_timer / Duration, 0f, 1f);
                Vector3 targetLocal = new(0.28f, 0f, 1.10f);

                _auraRing.RotationY += 180f * dt;
                float ringPulse = 1.15f + MathF.Sin(p * MathF.PI) * (_slot == "r" ? 1.1f : 0.55f);
                _auraRing.Scale = new Vector3(ringPulse, 0.04f, ringPulse);

                if (_slot is "m1" or "m2")
                {
                    float flight = Math.Clamp((p * 1.8f) % 1.0f, 0f, 1f);
                    for (int i = 0; i < _projectiles.Count; i++)
                    {
                        var pr = _projectiles[i];
                        float offsetP = Math.Clamp(flight - i * 0.12f, 0f, 1f);
                        float spreadX = (_projectiles.Count > 1 ? (i - (_projectiles.Count - 1) * 0.5f) * 0.24f * (1f - offsetP * 0.6f) : 0f);
                        pr.Position = Vector3.Lerp(new Vector3(spreadX, 0f, 0f), targetLocal + new Vector3(spreadX * 0.3f, 0f, 0f), offsetP);
                        pr.RotationZ += 260f * dt;
                    }
                    bool hitNow = flight > 0.72f;
                    _dummyBody.Color = hitNow ? Color.Yellow : Rgb(220, 65, 75);
                    _dummyBody.Scale = hitNow ? new Vector3(0.46f, 0.84f, 0.46f) : new Vector3(0.38f, 0.92f, 0.38f);
                }
                else if (_slot == "shift")
                {
                    float dashWave = MathF.Sin(p * MathF.PI);
                    for (int i = 0; i < _projectiles.Count; i++)
                    {
                        var cl = _projectiles[i];
                        float step = Math.Clamp(dashWave * 1.2f - i * 0.18f, 0f, 1f);
                        float arcY = _variant == 1 ? MathF.Sin(step * MathF.PI) * 0.65f : 0f;
                        cl.Position = Vector3.Lerp(new Vector3(0f, -0.25f, -0.2f), targetLocal + new Vector3(0f, -0.25f + arcY, 0f), step);
                        cl.RotationY += 180f * dt;
                    }
                    _dummyBody.Color = dashWave > 0.65f ? Color.SkyBlue : Rgb(220, 65, 75);
                }
                else // "r"
                {
                    for (int i = 0; i < _projectiles.Count; i++)
                    {
                        var sat = _projectiles[i];
                        float ang = _timer * 5.5f + i * MathF.PI * 0.5f;
                        if (p < 0.55f)
                        {
                            float r = 0.85f + MathF.Sin(p * MathF.PI * 2f) * 0.25f;
                            sat.Position = new Vector3(MathF.Cos(ang) * r, 0.2f + MathF.Sin(ang * 2f) * 0.18f, MathF.Sin(ang) * r);
                        }
                        else
                        {
                            float strikeT = (p - 0.55f) / 0.45f;
                            Vector3 orbitPos = new(MathF.Cos(ang) * 0.85f, 0.25f, MathF.Sin(ang) * 0.85f);
                            sat.Position = Vector3.Lerp(orbitPos, targetLocal, strikeT);
                        }
                        sat.RotationY += 240f * dt;
                    }
                    _dummyBody.Color = p > 0.62f ? Color.Magenta : Rgb(220, 65, 75);
                    _dummyBody.Scale = p > 0.62f ? new Vector3(0.50f, 0.78f, 0.50f) : new Vector3(0.38f, 0.92f, 0.38f);
                }

                if (_timer >= Duration)
                    Destroy(this);
            }
        }

        private static void ConfirmLoginProfile()
        {
            string finalName = string.IsNullOrWhiteSpace(_playerNameInput) ? "Survivor" : _playerNameInput.Trim();
            ProgressData.Instance.SetPlayerName(finalName);
            _playerNameInput = ProgressData.Instance.PlayerName;
            _screen = "main";
            if (!ProgressData.Instance.IsCharacterUnlocked(_selectedChar))
                SelectCharacter("commando");
            PersistRuntimeUserSettings();
            SoundManager.Play("ui_ready", 0.8f);
            ShowBanner($"ПРОФИЛЬ ИГРОКА: {finalName} (НАСТРОЙКИ И ПРОГРЕСС СОХРАНЕНЫ)", Color.SkyBlue, 2.5f);
        }

        private static void HandleInput(float dt)
        {
            if (_screen == "login")
            {
                int ch = Raylib.GetCharPressed();
                while (ch > 0)
                {
                    char c = (char)ch;
                    if ((char.IsLetterOrDigit(c) || c is '_' or '-' or '.') && _playerNameInput.Length < 22)
                        _playerNameInput += c;
                    ch = Raylib.GetCharPressed();
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _playerNameInput.Length > 0)
                    _playerNameInput = _playerNameInput[..^1];
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    ConfirmLoginProfile();
                return;
            }

            if (_screen == "mp_connect" && _typingCode)
            {
                bool ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
                if (ctrl && Raylib.IsKeyPressed(KeyboardKey.V))
                {
                    string clip = Raylib.GetClipboardText_() ?? "";
                    foreach (char raw in clip.Trim())
                    {
                        char c = char.ToUpperInvariant(raw);
                        if ((char.IsLetterOrDigit(c) || c is '-' or '.' or ':' or '_') && _roomCodeInput.Length < 32)
                            _roomCodeInput += c;
                    }
                }

                int ch = Raylib.GetCharPressed();
                while (ch > 0)
                {
                    char c = char.ToUpperInvariant((char)ch);
                    if ((char.IsLetterOrDigit(c) || c is '-' or '.' or ':' or '_') && _roomCodeInput.Length < 32)
                        _roomCodeInput += c;
                    ch = Raylib.GetCharPressed();
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _roomCodeInput.Length > 0)
                    _roomCodeInput = _roomCodeInput[..^1];
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    _typingCode = false;
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    _typingCode = false;
                return;
            }

            if (_screen == "logbook" && _logDetailOpen)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                    CloseLogbookDetailPage();
                return;
            }

            if (_screen == "logbook" && !_logDetailOpen && Raylib.IsKeyPressed(KeyboardKey.Enter) && !string.IsNullOrEmpty(_logSelectedKey))
            {
                OpenLogbookDetailPage(_logCategory, _logSelectedKey, 0);
                return;
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Escape) && _screen != "main")
            {
                if (_screen == "lobby") _net.StopSession();
                PersistRuntimeUserSettings();
                _screen = "main";
                SelectCharacter(_selectedChar);
                return;
            }

            float wheel = Raylib.GetMouseWheelMove();
            if (MathF.Abs(wheel) > 0.01f)
            {
                bool overLeftRoster = _screen == "lobby" && Raylib.GetMouseX() < _lobbyMidPanelX;
                if (!overLeftRoster)
                {
                    _previewZoom = Math.Clamp(_previewZoom + wheel * 0.12f, 0.55f, 1.75f);
                    _manualTimer = 4.0f;
                }
            }

            int sw = Math.Max(1024, Raylib.GetScreenWidth());
            int sh = Math.Max(600, Raylib.GetScreenHeight());
            int mx = Raylib.GetMouseX();
            int my = Raylib.GetMouseY();

            bool dragging = Raylib.IsMouseButtonDown(MouseButton.Right) ||
                            (Raylib.IsMouseButtonDown(MouseButton.Left) &&
                             (_screen == "lobby"
                                 ? (mx > _lobbyMidPanelX && mx < (_lobbyMidPanelX + _lobbyMidPanelW) && my > (_lobbyTopY + 90) && my < (sh - 220))
                                 : mx > Math.Min(660, (int)(sw * 0.45f))));
            if (dragging)
            {
                Vector2 delta = Raylib.GetMouseDelta();
                if (delta.LengthSquared() > 0.1f)
                {
                    _previewActor.RotationY -= delta.X * 0.45f;
                    _previewActor.RotationX = Math.Clamp(_previewActor.RotationX + delta.Y * 0.35f, -65f, 65f);
                    _manualTimer = 4.5f;
                }
            }
        }

        private static void UpdateDiorama(float dt)
        {
            float t = (float)Raylib.GetTime();
            int sw = Math.Max(1024, Raylib.GetScreenWidth());
            int sh = Math.Max(600, Raylib.GetScreenHeight());
            float aspect = sw / (float)sh;
            const float camDepth = 7.6f; // Camera Z=-8.6 -> Pedestal Z=-1.0
            float halfWorldH = camDepth * MathF.Tan(Math.Clamp(_fov, 45f, 95f) * 0.5f * (MathF.PI / 180f));
            float halfWorldW = halfWorldH * aspect;

            float targetScreenCenterX = _screen == "lobby"
                ? (_lobbyMidPanelX + _lobbyMidPanelW * 0.5f)
                : (Math.Min(580f, sw * 0.42f) + sw) * 0.5f;
            float ndcX = (targetScreenCenterX / sw) * 2f - 1f;
            float targetPedestalX = ndcX * halfWorldW;

            _mainPedestal.X = Lerp(_mainPedestal.X, targetPedestalX, MathF.Min(1f, dt * 12f));
            _menuCrystal.X = _mainPedestal.X;
            _menuCrystal.RotationY += 55f * dt;
            _menuCrystal.Y = 3.3f + MathF.Sin(t * 2.5f) * 0.18f;
            _previewActor.Scale = new Vector3(_previewBaseScale * _previewZoom);

            if (_skillPreviewTimer > 0f) _skillPreviewTimer = MathF.Max(0f, _skillPreviewTimer - dt);
            if (_manualTimer > 0f) _manualTimer -= dt;
            else
            {
                _previewActor.RotationY += 22f * dt;
                _previewActor.RotationX = Lerp(_previewActor.RotationX, 0f, dt * 5f);
            }

            if (_previewSurvivorParts != null)
            {
                string previewKey = _screen == "logbook" ? _logSelectedKey : _selectedChar;
                _previewSurvivorParts.UpdateAnimations(previewKey, dt, isMoving: false, isGrounded: true, velY: 0f, moveSpeedRatio: 1f, strafeInput: 0f, forwardInput: 0f);
            }
            if (_previewEnemyParts != null)
            {
                if (_previewEnemyParts.Crown != null) _previewEnemyParts.Crown.RotationY += 85f * dt;
                if (_previewEnemyParts.Ring1 != null) _previewEnemyParts.Ring1.RotationY += 110f * dt;
                if (_previewEnemyParts.Ring2 != null) _previewEnemyParts.Ring2.RotationX += 125f * dt;
                if (_previewEnemyParts.Tentacles != null) _previewEnemyParts.Tentacles.RotationY += 70f * dt;
            }
        }

        private static void DrawHeader()
        {
            int sw = Raylib.GetScreenWidth();
            int hh = sw < 1220 ? 46 : 54;
            float titleFs = sw < 1220 ? 19f : 22.5f;
            Raylib.DrawRectangle(0, 0, sw, hh, Rgb(12, 16, 26, 235));
            Raylib.DrawRectangle(0, hh, sw, 3, Rgb(255, 165, 50));
            Engine.DrawTextCentered("RISK OF C# 2 : STEAM SPACEWAR & КООПЕРАТИВ ПО СЕТИ", sw * 0.5f, hh * 0.5f, titleFs, Rgb(255, 185, 60));
        }

        private static void DrawLoginUI()
        {
            DrawHeader();
            int sw = Raylib.GetScreenWidth();
            int sh = Raylib.GetScreenHeight();
            int pw = Math.Clamp((int)(sw * 0.42f), 440, 580);
            int ph = Math.Min(320, sh - 110);
            int px = Math.Clamp(60, 24, sw - pw - 24);
            int py = Math.Max(70, (sh - ph) / 2);
            float cx = px + pw * 0.5f;

            Raylib.DrawRectangle(px, py, pw, ph, Rgb(14, 18, 30, 244));
            Raylib.DrawRectangle(px, py, pw, 4, Color.SkyBlue);
            Engine.DrawTextCentered("ПРОФИЛЬ ВЫЖИВШЕГО", cx, py + 36, 23f, Color.White);
            Engine.DrawTextCentered("Смена ника не сбрасывает настройки и открытых героев:", cx, py + 70, 14.5f, Rgb(190, 215, 245));

            bool blink = ((int)(Raylib.GetTime() * 2.5f) % 2) == 0;
            string shownName = string.IsNullOrEmpty(_playerNameInput)
                ? (blink ? "_" : "Введите никнейм...")
                : (_playerNameInput + (blink ? "_" : ""));

            int bx = px + 36, bw = pw - 72;
            Raylib.DrawRectangle(bx, py + 100, bw, 54, Rgb(22, 30, 48, 250));
            Raylib.DrawRectangleLines(bx, py + 100, bw, 54, Color.SkyBlue);
            Engine.DrawTextCentered(shownName, cx, py + 127, 21f, Color.Yellow);

            if (Engine.DrawButton(new Rectangle(bx, py + 174, bw, 54), "СОХРАНИТЬ И ВОЙТИ [ENTER]",
                Rgb(38, 115, 72), Rgb(55, 160, 98), Color.White, 18f, Color.Lime))
            {
                ConfirmLoginProfile();
            }

            if (Engine.DrawButton(new Rectangle(bx, py + 240, bw, 38), "ОЧИСТИТЬ ПОЛЕ ВВОДА [BACKSPACE]", Rgb(55, 36, 44), Rgb(85, 52, 64), Rgb(220, 180, 180), 14f))
                _playerNameInput = "";
        }

        private static void DrawMainMenuUI()
        {
            DrawHeader();
            int sw = Raylib.GetScreenWidth();
            int sh = Raylib.GetScreenHeight();
            int topBar = sw < 1220 ? 52 : 62;
            int px = Math.Clamp((int)(sw * 0.038f), 24, 60);
            int pw = Math.Clamp((int)(sw * 0.34f), 390, 480);
            int availH = Math.Max(490, sh - topBar - 20);
            int ph = Math.Min(628, availH);
            int py = topBar + Math.Max(6, (availH - ph) / 2);
            float cx = px + pw * 0.5f;
            int bx = px + 34, bw = pw - 68;
            float scaleY = ph / 628f;

            Raylib.DrawRectangle(px, py, pw, ph, Rgb(14, 18, 30, 238));
            Raylib.DrawRectangle(px, py, pw, 4, Color.SkyBlue);
            Engine.DrawTextCentered("ГЛАВНОЕ МЕНЮ", cx, py + 28 * scaleY, 22f, Color.White);

            int curY = py + (int)(52 * scaleY);
            int gap = Math.Max(6, (int)(10 * scaleY));

            string curUser = string.IsNullOrWhiteSpace(ProgressData.Instance.PlayerName) ? "Survivor" : ProgressData.Instance.PlayerName;
            int profH = Math.Max(30, (int)(36 * scaleY));
            if (Engine.DrawButton(new Rectangle(bx, curY, bw, profH),
                $"ПРОФИЛЬ: {curUser}  [СМЕНИТЬ НИК]",
                Rgb(26, 36, 56), Rgb(48, 68, 104),
                Color.SkyBlue, 13.5f, Color.SkyBlue))
            {
                _playerNameInput = ProgressData.Instance.PlayerName;
                _screen = "login";
            }
            curY += profH + gap;

            int btnH = Math.Max(38, (int)(52 * scaleY));
            if (Engine.DrawButton(new Rectangle(bx, curY, bw, btnH), "ОДИНОЧНАЯ ИГРА", Rgb(38, 105, 70), Rgb(55, 150, 95), Color.White, 18f))
            {
                _net.StopSession();
                _lobbyPlayers.Clear();
                _myReady = false;
                _lobbyCountdownActive = false;
                _screen = "lobby";
                SelectCharacter(_selectedChar);
            }
            curY += btnH + gap;

            bool shipUnl = IsSafeTravelsUnlocked();
            string shipLabel = shipUnl
                ? "★ КОРАБЛЬ UES «SAFE TRAVELS»"
                : "[ЗАКРЫТО] КОРАБЛЬ «SAFE TRAVELS»";
            if (Engine.DrawButton(new Rectangle(bx, curY, bw, btnH), shipLabel,
                shipUnl ? Rgb(28, 78, 128) : Rgb(36, 34, 46),
                shipUnl ? Rgb(48, 116, 186) : Rgb(56, 50, 68),
                shipUnl ? Color.Yellow : Rgb(165, 175, 195), 16f,
                shipUnl ? Color.SkyBlue : Color.Gray))
            {
                if (shipUnl)
                {
                    EnterSafeTravels(_selectedChar);
                }
                else
                {
                    SoundManager.Play("ui_error");
                    ShowBanner("ПОБЕДИТЕ МИТРИКСА НА ЛУНЕ ХОТЯ БЫ 1 РАЗ, ЧТОБЫ ПОДНЯТЬСЯ НА БОРТ UES «SAFE TRAVELS»!", Color.Orange, 4.0f);
                }
            }
            curY += btnH + 4;
            Engine.DrawTextCentered(
                shipUnl ? "Хаб выживших: каюты, полигон урона и диалоги экипажа" : "Условие: одолейте Митрикса хотя бы один раз",
                cx, curY + 7, 11.5f, shipUnl ? Rgb(120, 225, 255) : Rgb(155, 165, 185));
            curY += 18;

            if (Engine.DrawButton(new Rectangle(bx, curY, bw, btnH), "СЕТЕВАЯ ИГРА (STEAM / LAN)", Rgb(40, 85, 145), Rgb(60, 125, 205), Color.White, 18f, Color.SkyBlue))
            {
                _typingCode = false;
                _steamTabAutoChecked = false;
                _netStatusText = $"Ваш локальный IP: {_net.HostIp}";
                _screen = "mp_connect";
            }
            curY += btnH + gap;

            int subBtnH = Math.Max(36, (int)(48 * scaleY));
            if (Engine.DrawButton(new Rectangle(bx, curY, bw, subBtnH), "ЖУРНАЛ ВЫЖИВШЕГО", Rgb(125, 75, 165), Rgb(165, 105, 215), Color.White, 17.5f))
            {
                _logDetailOpen = false;
                if (_logCategory == "history") _logCategory = "items";
                _screen = "logbook";
                var list = GetLogEntries(_logCategory);
                if (list.Count > 0) SelectLogbookEntry(_logCategory, list[0].Key);
            }
            curY += subBtnH + gap;

            if (Engine.DrawButton(new Rectangle(bx, curY, bw, subBtnH), "▶ ИСТОРИЯ ЗАБЕГОВ И РЕПЛЕИ", Rgb(34, 88, 132), Rgb(54, 126, 188), Color.Yellow, 16.5f, Color.SkyBlue))
            {
                _logDetailOpen = false;
                _logCategory = "history";
                _historyPage = 0;
                Data.RunHistoryStorage.Load();
                if (Data.RunHistoryStorage.Entries.Count > 0 && string.IsNullOrEmpty(_selectedHistoryRunId))
                    _selectedHistoryRunId = Data.RunHistoryStorage.Entries[0].RunId;
                _screen = "logbook";
            }
            curY += subBtnH + gap;

            if (Engine.DrawButton(new Rectangle(bx, curY, bw, subBtnH), "НАСТРОЙКИ", Rgb(65, 72, 95), Rgb(95, 105, 135), Color.White, 17.5f))
            {
                _screen = "settings";
            }
            curY += subBtnH + gap;

            if (Engine.DrawButton(new Rectangle(bx, curY, bw, subBtnH), "ВЫХОД ИЗ ИГРЫ", Rgb(135, 40, 45), Rgb(185, 60, 65), Color.White, 17.5f))
            {
                PersistRuntimeUserSettings();
                _net.StopSession();
                Net.SteamSpacewarService.Instance.Shutdown();
                SoundManager.Shutdown();
                Raylib.CloseWindow();
                Environment.Exit(0);
            }
        }

        private static void DrawMpConnectUI()
        {
            DrawHeader();
            int sw = Raylib.GetScreenWidth();
            int sh = Raylib.GetScreenHeight();
            int topBar = sw < 1220 ? 52 : 62;
            int px = Math.Clamp((int)(sw * 0.038f), 24, 60);
            int pw = Math.Clamp((int)(sw * 0.44f), 480, 640);
            int availH = Math.Max(500, sh - topBar - 18);
            int ph = Math.Min(690, availH);
            int py = topBar + Math.Max(4, (availH - ph) / 2);
            float cx = px + pw * 0.5f;
            int bx = px + 26, bw = pw - 52;

            Raylib.DrawRectangle(px, py, pw, ph, Rgb(14, 18, 30, 244));
            Raylib.DrawRectangle(px, py, pw, 4, Color.SkyBlue);
            Engine.DrawTextCentered("СЕТЕВАЯ ИГРА :: STEAM SPACEWAR & LAN", cx, py + 22, 18.5f, Rgb(150, 225, 255));

            // 1. Вкладки транспорта: STEAM SPACEWAR vs LAN / IP
            int tabY = py + 44;
            int halfW = (bw - 10) / 2;
            bool isSteamTab = _mpTransportTab != "lan";
            if (Engine.DrawButton(new Rectangle(bx, tabY, halfW, 38), "★ STEAM (SPACEWAR)",
                isSteamTab ? Rgb(32, 92, 156) : Rgb(22, 28, 44), Rgb(50, 122, 198),
                isSteamTab ? Color.Yellow : Color.White, 14.5f, isSteamTab ? Color.Yellow : Color.SkyBlue))
            {
                _mpTransportTab = "steam";
                _steamTabAutoChecked = false;
                PersistRuntimeUserSettings();
            }
            if (Engine.DrawButton(new Rectangle(bx + halfW + 10, tabY, halfW, 38), "LAN / КОД КОМНАТЫ",
                !isSteamTab ? Rgb(38, 102, 82) : Rgb(22, 28, 44), Rgb(54, 136, 108),
                !isSteamTab ? Color.Yellow : Color.White, 14.5f, !isSteamTab ? Color.Yellow : Color.SkyBlue))
            {
                _mpTransportTab = "lan";
                PersistRuntimeUserSettings();
            }

            // 2. Переключатель режима: Кооператив PvE / PvP Арена
            int modeY = tabY + 46;
            bool isCoop = _net.GameType == "coop";
            if (Engine.DrawButton(new Rectangle(bx, modeY, halfW, 32), "РЕЖИМ: КООПЕРАТИВ (PvE)",
                isCoop ? Rgb(38, 115, 75) : Rgb(26, 32, 48), Rgb(50, 145, 95), Color.White, 13.5f, isCoop ? Color.Lime : null))
            {
                _net.GameType = "coop";
                _net.DiscoveredRooms.Clear();
                if (isSteamTab && Net.SteamSpacewarService.Instance.IsAvailable)
                    Net.SteamSpacewarService.Instance.RefreshLobbies("coop");
            }
            if (Engine.DrawButton(new Rectangle(bx + halfW + 10, modeY, halfW, 32), "РЕЖИМ: АРЕНА PvP (ДУЭЛЬ)",
                !isCoop ? Rgb(145, 42, 55) : Rgb(26, 32, 48), Rgb(185, 60, 75), Color.White, 13.5f, !isCoop ? Color.Red : null))
            {
                _net.GameType = "pvp";
                _net.DiscoveredRooms.Clear();
                if (isSteamTab && Net.SteamSpacewarService.Instance.IsAvailable)
                    Net.SteamSpacewarService.Instance.RefreshLobbies("pvp");
            }

            int contentY = modeY + 42;
            int backBtnY = py + ph - 52;

            if (isSteamTab)
            {
                var steam = Net.SteamSpacewarService.Instance;
                if (!_steamTabAutoChecked)
                {
                    _steamTabAutoChecked = true;
                    steam.RefreshSpacewarDiskState();
                    bool ready = steam.EnsureInitialized(autoInstallIfMissing: true);
                    if (ready)
                    {
                        steam.RefreshLobbies(_net.GameType);
                    }
                    else if (!steam.SpacewarInstalledOnDisk)
                    {
                        ShowBanner("SPACEWAR (AppID 480) НЕ НАЙДЕН — ОТКРЫТА УСТАНОВКА В STEAM!", Color.Gold, 4.5f);
                    }
                }

                // Плашка состояния Spacewar / Steam
                Color stCol = steam.IsAvailable
                    ? Color.Lime
                    : (steam.SpacewarDownloadingOnDisk ? Color.Gold : Color.Orange);
                Raylib.DrawRectangle(bx, contentY, bw, 58, Rgb(18, 24, 40, 246));
                Raylib.DrawRectangle(bx, contentY, 4, 58, stCol);
                string headerLine = steam.IsAvailable
                    ? $"✓ STEAM ПОДКЛЮЧЁН :: {steam.LocalPersonaName} (AppID 480 Spacewar)"
                    : (steam.SpacewarDownloadingOnDisk
                        ? "⬇ SPACEWAR УСТАНАВЛИВАЕТСЯ В STEAM — ОЖИДАНИЕ ЗАВЕРШЕНИЯ..."
                        : (!steam.SpacewarInstalledOnDisk
                            ? "⚠ SPACEWAR (AppID 480) НЕ УСТАНОВЛЕН НА ЭТОМ ПК"
                            : "⚠ STEAM НЕ ПОДКЛЮЧЁН (УБЕДИТЕСЬ, ЧТО КЛИЕНТ STEAM ЗАПУЩЕН)"));
                Engine.DrawTextLeft(headerLine, bx + 12, contentY + 8, 13.5f, stCol);
                Engine.DrawTextWrapped(steam.StatusText, bx + 12, contentY + 28, bw - 24, 12f, Rgb(195, 215, 240), 2f, maxLines: 2);
                contentY += 66;

                if (!steam.SpacewarInstalledOnDisk)
                {
                    int instW = (bw - 8) * 2 / 3;
                    if (Engine.DrawButton(new Rectangle(bx, contentY, instW, 42),
                        "⬇ СКАЧАТЬ SPACEWAR В STEAM (480)",
                        Rgb(145, 88, 24), Rgb(192, 120, 36), Color.Yellow, 13.5f, Color.Gold))
                    {
                        steam.TriggerSpacewarInstall();
                        ShowBanner("ОТПРАВЛЕН ЗАПРОС steam://install/480 — ПОДТВЕРДИТЕ УСТАНОВКУ В STEAM!", Color.Gold, 4.0f);
                    }
                    if (Engine.DrawButton(new Rectangle(bx + instW + 8, contentY, bw - instW - 8, 42),
                        "ПРОВЕРИТЬ",
                        Rgb(34, 68, 112), Rgb(52, 98, 158), Color.White, 13.5f, Color.SkyBlue))
                    {
                        if (steam.EnsureInitialized(autoInstallIfMissing: false))
                            steam.RefreshLobbies(_net.GameType);
                    }
                    contentY += 50;
                }
                else if (!steam.IsAvailable)
                {
                    if (Engine.DrawButton(new Rectangle(bx, contentY, bw, 40),
                        "ПОВТОРИТЬ ПОДКЛЮЧЕНИЕ К КЛИЕНТУ STEAM",
                        Rgb(36, 76, 126), Rgb(54, 108, 176), Color.White, 14f, Color.SkyBlue))
                    {
                        if (steam.EnsureInitialized(autoInstallIfMissing: true))
                            steam.RefreshLobbies(_net.GameType);
                    }
                    contentY += 48;
                }

                // Кнопка создания Steam-лобби
                if (Engine.DrawButton(new Rectangle(bx, contentY, bw, 46),
                    $"СОЗДАТЬ STEAM-ЛОББИ [{_net.GameType.ToUpper()}] (БЕЗ ПРОБРОСА ПОРТОВ)",
                    steam.IsAvailable ? Rgb(35, 106, 70) : Rgb(34, 42, 58),
                    steam.IsAvailable ? Rgb(52, 152, 98) : Rgb(48, 58, 78),
                    Color.White, 15f, steam.IsAvailable ? Color.Lime : Color.Gray))
                {
                    _typingCode = false;
                    _lobbyPlayers.Clear();
                    _lobbyJoinAsSpectator = false;
                    var (ok, info) = _net.HostSteamRoom();
                    if (ok)
                    {
                        _myReady = false;
                        _lobbyCountdownActive = false;
                        _screen = "lobby";
                        SelectCharacter(_selectedChar);
                        ShowBanner("СОЗДАЁТСЯ STEAM-ЛОББИ SPACEWAR... ПРИГЛАСИТЕ ДРУЗЕЙ КНОПКОЙ В ЛОББИ!", Color.Lime, 4.2f);
                    }
                    else
                    {
                        _netStatusText = info;
                        ShowBanner(info, Color.Orange, 3.8f);
                    }
                }
                contentY += 54;

                // Поле ввода Steam Lobby ID + Вставка из буфера + Подключение
                string idShown = string.IsNullOrEmpty(_roomCodeInput)
                    ? (_typingCode ? "ВВЕДИТЕ ИЛИ ВСТАВЬТЕ LOBBY ID..." : "LOBBY ID (НАЖМИТЕ ИЛИ CTRL+V)")
                    : _roomCodeInput;
                int inputW = bw - 118;
                if (Engine.DrawButton(new Rectangle(bx, contentY, inputW, 40),
                    _typingCode ? $"ID: > {idShown}_ <" : $"ID: [ {idShown} ]",
                    _typingCode ? Rgb(52, 74, 118) : Rgb(24, 32, 50), Rgb(42, 58, 88), Color.Yellow, 13.5f, Color.SkyBlue))
                {
                    _typingCode = !_typingCode;
                }
                if (Engine.DrawButton(new Rectangle(bx + inputW + 6, contentY, 112, 40), "ВСТАВИТЬ",
                    Rgb(42, 56, 84), Rgb(64, 86, 126), Color.White, 13f, Color.SkyBlue))
                {
                    string clip = (Raylib.GetClipboardText_() ?? "").Trim();
                    string filtered = "";
                    foreach (char c in clip)
                        if ((char.IsLetterOrDigit(c) || c is '-' or '.' or ':' or '_') && filtered.Length < 32)
                            filtered += char.ToUpperInvariant(c);
                    if (!string.IsNullOrEmpty(filtered))
                        _roomCodeInput = filtered;
                }
                contentY += 46;

                if (Engine.DrawButton(new Rectangle(bx, contentY, halfW, 38), "ВОЙТИ ПО LOBBY ID",
                    Rgb(40, 88, 148), Rgb(60, 122, 200), Color.White, 14f, Color.SkyBlue))
                {
                    _typingCode = false;
                    var (ok, info) = _net.JoinSteamByInput(_roomCodeInput);
                    if (ok)
                    {
                        _lobbyPlayers.Clear();
                        _myReady = false;
                        _screen = "lobby";
                        SelectCharacter(_selectedChar);
                    }
                    else _netStatusText = info;
                }
                if (Engine.DrawButton(new Rectangle(bx + halfW + 10, contentY, halfW, 38), "↻ НАЙТИ ЛОББИ STEAM",
                    Rgb(78, 52, 136), Rgb(110, 76, 184), Color.White, 14f, Color.Magenta))
                {
                    _typingCode = false;
                    if (steam.EnsureInitialized(autoInstallIfMissing: true))
                        steam.RefreshLobbies(_net.GameType);
                }
                contentY += 46;

                // Список открытых Steam-лобби и лобби друзей
                int listH = Math.Max(68, backBtnY - contentY - 8);
                Raylib.DrawRectangle(bx, contentY, bw, listH, Rgb(10, 14, 24, 235));
                Raylib.DrawRectangleLines(bx, contentY, bw, listH, Rgb(58, 86, 130, 180));
                Engine.DrawTextLeft($"ОТКРЫТЫЕ ЛОББИ STEAM / ДРУЗЕЙ ({steam.DiscoveredLobbies.Count}):", bx + 10, contentY + 6, 12.5f, Color.SkyBlue);

                int rowY = contentY + 26;
                int maxRows = Math.Max(1, (listH - 30) / 34);
                if (steam.DiscoveredLobbies.Count == 0)
                {
                    Engine.DrawTextCentered(
                        steam.IsSearchingLobbies ? "Поиск лобби в Steam..." : "Активных комнат не найдено — создайте лобби или примите инвайт в Steam (Shift+Tab)",
                        cx, contentY + listH * 0.56f, 12f, Rgb(155, 175, 205));
                }
                else
                {
                    for (int li = 0; li < Math.Min(maxRows, steam.DiscoveredLobbies.Count); li++)
                    {
                        var lob = steam.DiscoveredLobbies[li];
                        string friendTag = lob.IsFriend ? "★ ДРУГ: " : "";
                        string rowLabel = $"{friendTag}{lob.HostName} [{lob.MemberCount}/{lob.MaxMembers}] ({lob.Mode.ToUpper()})";
                        if (Engine.DrawButton(new Rectangle(bx + 8, rowY, bw - 16, 30), rowLabel,
                            lob.IsFriend ? Rgb(34, 72, 112) : Rgb(22, 30, 48), Rgb(52, 98, 162),
                            lob.IsFriend ? Color.Yellow : Color.White, 12.5f, lob.IsFriend ? Color.Gold : Color.SkyBlue))
                        {
                            var (ok, info) = _net.JoinSteamLobby(lob.LobbyId);
                            if (ok)
                            {
                                _lobbyPlayers.Clear();
                                _myReady = false;
                                _screen = "lobby";
                                SelectCharacter(_selectedChar);
                            }
                            else _netStatusText = info;
                        }
                        rowY += 34;
                    }
                }
            }
            else
            {
                // Вкладка LAN / КОД КОМНАТЫ
                if (Engine.DrawButton(new Rectangle(bx, contentY, bw, 52),
                    $"СОЗДАТЬ LAN-КОМНАТУ [{_net.GameType.ToUpper()}] (ХОСТ)",
                    Rgb(35, 100, 68), Rgb(50, 145, 95), Color.White, 17f, Color.Lime))
                {
                    _typingCode = false;
                    _lobbyPlayers.Clear();
                    _lobbyJoinAsSpectator = false;
                    var (ok, info) = _net.HostRoom();
                    if (ok)
                    {
                        _myReady = false;
                        _lobbyCountdownActive = false;
                        _screen = "lobby";
                        SelectCharacter(_selectedChar);
                        ShowBanner($"КОМНАТА СОЗДАНА! КОД: {info}", Color.Lime, 4.0f);
                    }
                    else _netStatusText = $"Ошибка: {info}";
                }
                contentY += 64;

                string codeShown = string.IsNullOrEmpty(_roomCodeInput)
                    ? (_typingCode ? "ВВЕДИТЕ КОД ИЛИ IP:PORT..." : "НАЖМИТЕ ДЛЯ ВВОДА КОДА")
                    : _roomCodeInput;
                string boxTitle = _typingCode ? $"КОД: > {codeShown}_ <" : $"КОД: [ {codeShown} ]";
                if (Engine.DrawButton(new Rectangle(bx, contentY, bw, 50), boxTitle,
                    _typingCode ? Rgb(55, 75, 115) : Rgb(28, 36, 54), Rgb(45, 60, 88), Color.Yellow, 17f, Color.SkyBlue))
                {
                    _typingCode = !_typingCode;
                }
                contentY += 62;

                if (Engine.DrawButton(new Rectangle(bx, contentY, halfW, 50), "ПО КОДУ / IP", Rgb(40, 85, 145), Rgb(60, 120, 200), Color.White, 17f))
                {
                    _typingCode = false;
                    var (ok, info) = _net.JoinByCode(_roomCodeInput);
                    if (ok)
                    {
                        _lobbyPlayers.Clear();
                        _myReady = false;
                        _screen = "lobby";
                        SelectCharacter(_selectedChar);
                    }
                    else _netStatusText = info;
                }
                if (Engine.DrawButton(new Rectangle(bx + halfW + 10, contentY, halfW, 50), "НАЙТИ В LAN (АВТО)", Rgb(95, 55, 145), Rgb(130, 80, 195), Color.White, 17f))
                {
                    _typingCode = false;
                    var (ok, info) = _net.JoinLanAuto();
                    if (ok)
                    {
                        _roomCodeInput = info;
                        _lobbyPlayers.Clear();
                        _myReady = false;
                        _screen = "lobby";
                        SelectCharacter(_selectedChar);
                    }
                    else _netStatusText = $"Не найдено ({info})";
                }
                contentY += 68;

                Engine.DrawTextWrapped(_netStatusText, bx + 12, contentY, bw - 24, 15.5f, Color.SkyBlue, 4f, maxLines: 2);
            }

            if (Engine.DrawButton(new Rectangle(bx + (bw - 360) / 2, backBtnY, 360, 42), "<- НАЗАД В ГЛАВНОЕ МЕНЮ", Rgb(60, 65, 82), Rgb(85, 92, 115), Color.White, 16f))
            {
                _typingCode = false;
                _screen = "main";
            }
        }

        private static void DrawSettingsUI()
        {
            DrawHeader();
            int sw = Raylib.GetScreenWidth();
            int sh = Raylib.GetScreenHeight();
            int topBar = sw < 1220 ? 52 : 62;
            int px = Math.Clamp((int)(sw * 0.038f), 24, 60);
            int pw = Math.Clamp((int)(sw * 0.44f), 500, 620);
            int availH = Math.Max(510, sh - topBar - 16);
            int ph = Math.Min(690, availH);
            int py = topBar + Math.Max(4, (availH - ph) / 2);
            float cx = px + pw * 0.5f;
            int lx = px + 30;
            int minusX = px + pw - 148;
            int plusX = px + pw - 84;
            int rowStep = Math.Clamp((ph - 310) / 5, 40, 48);
            int btnH = Math.Min(36, rowStep - 4);
            var us = Data.UserSettings.Instance;

            Raylib.DrawRectangle(px, py, pw, ph, Rgb(14, 18, 30, 242));
            Raylib.DrawRectangle(px, py, pw, 4, Color.Gold);
            Engine.DrawTextCentered("НАСТРОЙКИ ИГРЫ, ЗВУКА И ИНТЕРФЕЙСА", cx, py + 26, 20f, Color.Gold);

            int ry = py + 56;
            Engine.DrawTextLeft($"Чувствительность мыши: {(int)MathF.Round(_mouseSens * 250f)}", lx, ry + 7, 16.5f, Color.White);
            if (Engine.DrawButton(new Rectangle(minusX, ry, 52, btnH), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 19f))
            {
                _mouseSens = Math.Clamp(_mouseSens - 0.02f, 0.04f, 0.50f);
                PersistRuntimeUserSettings();
            }
            if (Engine.DrawButton(new Rectangle(plusX, ry, 52, btnH), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 19f))
            {
                _mouseSens = Math.Clamp(_mouseSens + 0.02f, 0.04f, 0.50f);
                PersistRuntimeUserSettings();
            }

            ry += rowStep;
            Engine.DrawTextLeft($"Угол обзора камеры (FOV): {(int)_fov}", lx, ry + 7, 16.5f, Color.White);
            if (Engine.DrawButton(new Rectangle(minusX, ry, 52, btnH), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 19f))
            {
                _fov = Math.Clamp(_fov - 5f, 45f, 95f);
                PersistRuntimeUserSettings();
            }
            if (Engine.DrawButton(new Rectangle(plusX, ry, 52, btnH), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 19f))
            {
                _fov = Math.Clamp(_fov + 5f, 45f, 95f);
                PersistRuntimeUserSettings();
            }

            ry += rowStep;
            Engine.DrawTextLeft($"Общая громкость: {(int)MathF.Round(SoundManager.MasterVolume * 100f)}%", lx, ry + 7, 16.5f, Color.SkyBlue);
            if (Engine.DrawButton(new Rectangle(minusX, ry, 52, btnH), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 19f))
            {
                SoundManager.SetMasterVolume(SoundManager.MasterVolume - 0.10f);
                PersistRuntimeUserSettings();
            }
            if (Engine.DrawButton(new Rectangle(plusX, ry, 52, btnH), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 19f))
            {
                SoundManager.SetMasterVolume(SoundManager.MasterVolume + 0.10f);
                PersistRuntimeUserSettings();
            }

            ry += rowStep;
            Engine.DrawTextLeft($"Громкость эффектов (SFX): {(int)MathF.Round(SoundManager.SfxVolume * 100f)}%", lx, ry + 7, 16.5f, Color.SkyBlue);
            if (Engine.DrawButton(new Rectangle(minusX, ry, 52, btnH), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 19f))
            {
                SoundManager.SetSfxVolume(SoundManager.SfxVolume - 0.10f);
                PersistRuntimeUserSettings();
            }
            if (Engine.DrawButton(new Rectangle(plusX, ry, 52, btnH), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 19f))
            {
                SoundManager.SetSfxVolume(SoundManager.SfxVolume + 0.10f);
                PersistRuntimeUserSettings();
            }

            ry += rowStep;
            Engine.DrawTextLeft($"Фоновый синтезатор: {(int)MathF.Round(SoundManager.AmbientVolume * 100f)}%", lx, ry + 7, 16.5f, Color.SkyBlue);
            if (Engine.DrawButton(new Rectangle(minusX, ry, 52, btnH), "-", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 19f))
            {
                SoundManager.SetAmbientVolume(SoundManager.AmbientVolume - 0.05f);
                PersistRuntimeUserSettings();
            }
            if (Engine.DrawButton(new Rectangle(plusX, ry, 52, btnH), "+", Rgb(45, 55, 75), Rgb(70, 85, 115), Color.White, 19f))
            {
                SoundManager.SetAmbientVolume(SoundManager.AmbientVolume + 0.05f);
                PersistRuntimeUserSettings();
            }

            ry += rowStep + 6;
            int wideW = pw - 60;
            int toggleH = Math.Clamp((ph - (ry - py) - 56) / 4 - 6, 34, 42);
            int toggleStep = toggleH + 6;

            if (Engine.DrawButton(new Rectangle(lx, ry, wideW, toggleH),
                $"ЗАПИСЬ 3D-РЕПЛЕЕВ: {us.GetReplayModeDisplayLabel()}",
                Rgb(32, 62, 98), Rgb(48, 88, 138), Color.Yellow, 14f, Color.SkyBlue))
            {
                us.CycleReplaySaveMode();
                PersistRuntimeUserSettings();
            }
            ry += toggleStep;

            string tipModeLabel = us.CompactItemTooltips
                ? "ОПИСАНИЯ ПРЕДМЕТОВ: ИКОНКИ + КРАТКОЕ ОПИСАНИЕ"
                : "ОПИСАНИЯ ПРЕДМЕТОВ: ИКОНКИ + ПОЛНЫЕ СТАТЫ (НАВЕДЕНИЕ)";
            if (Engine.DrawButton(new Rectangle(lx, ry, wideW, toggleH),
                tipModeLabel,
                Rgb(44, 54, 88), Rgb(64, 78, 124), Color.White, 13.5f, Color.Gold))
            {
                us.CompactItemTooltips = !us.CompactItemTooltips;
                PersistRuntimeUserSettings();
            }
            ry += toggleStep;

            int halfBtnW = (wideW - 10) / 2;
            if (Engine.DrawButton(new Rectangle(lx, ry, halfBtnW, toggleH),
                $"ЗВУК: {(SoundManager.Muted ? "ВЫКЛ (MUTE)" : "ВКЛЮЧЕН")}",
                SoundManager.Muted ? Rgb(115, 42, 48) : Rgb(38, 95, 68),
                SoundManager.Muted ? Rgb(155, 58, 65) : Rgb(55, 135, 95), Color.White, 14.5f))
            {
                SoundManager.ToggleMute();
                PersistRuntimeUserSettings();
            }

            bool isFs = Engine.IsFullscreenActive();
            if (Engine.DrawButton(new Rectangle(lx + halfBtnW + 10, ry, halfBtnW, toggleH),
                $"ПОЛНЫЙ ЭКРАН [F11]: {(isFs ? "ВКЛ" : "ВЫКЛ")}",
                Rgb(45, 65, 95), Rgb(65, 95, 135), Color.White, 14f))
            {
                Data.UserSettings.Instance.ToggleFullscreenAndApply();
                PersistRuntimeUserSettings();
            }

            int backY = py + ph - 48;
            if (Engine.DrawButton(new Rectangle(px + (pw - 360) / 2, backY, 360, 40), "<- НАЗАД В ГЛАВНОЕ МЕНЮ", Rgb(60, 65, 82), Rgb(85, 92, 115), Color.White, 16f))
            {
                PersistRuntimeUserSettings();
                _screen = "main";
            }
        }

        private static void DrawWrappedText(string text, float x, float y, int maxCharsPerLine, float fontSize, Color col, int maxLines = 3, float lineStep = 18f)
        {
            if (string.IsNullOrEmpty(text)) return;
            string clean = text.Replace("\r", "").Replace("\n", " ");
            string[] words = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int curLine = 0;
            string line = "";

            for (int i = 0; i < words.Length; i++)
            {
                string candidate = string.IsNullOrEmpty(line) ? words[i] : $"{line} {words[i]}";
                if (candidate.Length > maxCharsPerLine && !string.IsNullOrEmpty(line))
                {
                    if (curLine == maxLines - 1)
                    {
                        string ell = line.Length > maxCharsPerLine - 3 ? line[..(maxCharsPerLine - 3)] + "..." : line + "...";
                        Engine.DrawTextLeft(ell, x, y + curLine * lineStep, fontSize, col);
                        return;
                    }
                    Engine.DrawTextLeft(line, x, y + curLine * lineStep, fontSize, col);
                    curLine++;
                    line = words[i];
                }
                else
                {
                    line = candidate;
                }
            }

            if (!string.IsNullOrEmpty(line) && curLine < maxLines)
            {
                Engine.DrawTextLeft(line, x, y + curLine * lineStep, fontSize, col);
            }
        }

        private static string GetReadableRosterName(string key, SurvivorConfig scfg) => key switch
        {
            "commando" => "Коммандо",
            "artificer" => "Изобретатель",
            "huntress" => "Охотница",
            "buzz" => "Базз Лайтер",
            "v1" => "V1 (ULTRAKILL)",
            "sol" => "Сол Бэдгай",
            "gwen" => "Гвен (LoL)",
            "enforcer" => "Силовик",
            "scout" => "Разведчик TF2",
            "m4a1" => "M4A1 (GFL)",
            "medic" => "Медик TF2",
            "aigis" => "Айгис (P3)",
            "reimu" => "Рейму Хакурей",
            "mindustry" => "Ядро Mindustry",
            "slark" => "Сларк (Dota)",
            "tidehunter" => "Тайдхантер",
            "freeman" => "Гордон Фриман",
            "dante" => "Данте (Limbus)",
            "cheburashka" => "Чебурашка",
            "ahri" => "Ари (LoL)",
            "lappland" => "Лаппланд",
            "w_arknights" => "W / Вишадель",
            "gold_ship" => "Голд Шип",
            "pushnoy" => "А. Пушной",
            "freddy" => "Мишка Фредди",
            "steve" => "Стив (Minecraft)",
            "jotaro" => "Джотаро Куджо",
            "vyper" => "Вайпер",
            "haze" => "Хейз (Deadlock)",
            "dmc_trio" => "Трио DMC",
            "jack_o" => "Джек-О Валентайн",
            "sanabi" => "SANABI (Генерал)",
            "susie" => "Сьюзи (Deltarune)",
            "spamton" => "Спамтон / НЕО",
            "binah" => "Бина (Арбитр)",
            "don_quixote" => "Дон Кихот",
            "priestess" => "Пристесс",
            "closure" => "Кложер (PRTS)",
            "daitaku_helios" => "Дайтаку Гелиос",
            "robin" => "Зарянка (HSR)",
            "bird_trio" => "Трио Птиц",
            "apocalypse_bird" => "Птица Апокалипсиса",
            "komeiji" => "Сёстры Комейдзи",
            "false_son" => "Ложный Сын",
            _ => scfg.Name
        };

        private static void DrawLobbyUI()
        {
            DrawHeader();
            if (!Data.ProgressData.Instance.IsCharacterUnlocked(_selectedChar))
                _selectedChar = "commando";

            int sw = Math.Max(1024, Raylib.GetScreenWidth());
            int sh = Math.Max(600, Raylib.GetScreenHeight());
            bool compact = sh < 740 || sw < 1280;

            int margin = sw < 1260 ? 10 : 18;
            int colGap = sw < 1260 ? 8 : 12;
            int headerBottom = sw < 1220 ? 49 : 57;
            int topY = headerBottom + (compact ? 6 : 9);
            int panelH = Math.Max(510, sh - topY - (compact ? 8 : 14));

            int usableW = sw - margin * 2 - colGap * 2;
            int leftW = Math.Clamp((int)(usableW * 0.255f), 252, 420);
            int rightW = Math.Clamp((int)(usableW * 0.395f), 384, 680);
            int midW = Math.Max(320, usableW - leftW - rightW);

            int leftX = margin;
            int midX = leftX + leftW + colGap;
            int rightX = midX + midW + colGap;

            _lobbyMidPanelX = midX;
            _lobbyMidPanelW = midW;
            _lobbyTopY = topY;

            var cfg = SurvivorDatabase.Classes[_selectedChar];
            cfg.SyncLoadout();
            var prog = Data.ProgressData.Instance;
            var activeSkin = cfg.GetActiveSkin();
            var rosterKeys = SurvivorDatabase.GetVisibleRosterKeys();

            // =========================================================================
            // 1. ЛЕВАЯ КОЛОНКА: АДАПТИВНЫЙ СПИСОК ПЕРСОНАЖЕЙ С ПАГИНАЦИЕЙ
            // =========================================================================
            Raylib.DrawRectangle(leftX, topY, leftW, panelH, Rgb(12, 16, 28, 244));
            Raylib.DrawRectangle(leftX, topY, leftW, 4, Color.SkyBlue);
            int unlockedHeroCount = 0;
            foreach (var rk in rosterKeys) if (prog.IsCharacterUnlocked(rk)) unlockedHeroCount++;
            Engine.DrawTextLeft(
                $"ВЫБОР ГЕРОЯ ({unlockedHeroCount}/{rosterKeys.Count})",
                leftX + 10, topY + 9, compact ? 13.5f : 15f, Color.SkyBlue);

            int leftInnerX = leftX + 8;
            int leftInnerW = leftW - 16;
            int bottomCursorY = topY + panelH - 8;

            int exitBtnH = compact ? 34 : 42;
            bottomCursorY -= exitBtnH;
            int exitBtnY = bottomCursorY;

            bool showShipBtn = !_net.Connected;
            bool showSpecBtn = _net.Connected && _net.Mode == "client" && _net.GameType == "coop";
            int extraBtnH = compact ? 32 : 38;
            int extraBtnY = exitBtnY;
            if (showShipBtn || showSpecBtn)
            {
                bottomCursorY -= (extraBtnH + 6);
                extraBtnY = bottomCursorY;
            }

            int pageBarH = compact ? 26 : 30;
            bottomCursorY -= (pageBarH + 6);
            int pageBarY = bottomCursorY;

            int gridTopY = topY + (compact ? 32 : 36);
            int gridAvailH = Math.Max(140, pageBarY - gridTopY - 4);
            int cellBtnH = compact ? 28 : 34;
            int cellRowStep = cellBtnH + 4;
            int rowsPerPage = Math.Max(4, gridAvailH / cellRowStep);
            int charsPerPage = rowsPerPage * 2;

            int totalCharPages = Math.Max(1, (rosterKeys.Count - 1) / charsPerPage + 1);
            _lobbyCharPage = Math.Clamp(_lobbyCharPage, 0, totalCharPages - 1);

            float wheelMove = Raylib.GetMouseWheelMove();
            if (MathF.Abs(wheelMove) > 0.05f && Raylib.CheckCollisionPointRec(Engine.GetUiMousePosition(), new Rectangle(leftX, gridTopY, leftW, gridAvailH)))
            {
                _lobbyCharPage = Math.Clamp(_lobbyCharPage - (wheelMove > 0f ? 1 : -1), 0, totalCharPages - 1);
            }

            int pageStart = _lobbyCharPage * charsPerPage;
            bool apocTaken = IsApocalypseBirdTakenByAnother();
            int cellGapX = 6;
            int cellW = (leftInnerW - cellGapX) / 2;
            float rosterFont = cellW < 150 ? 10.8f : 12.0f;

            for (int idxOnPage = 0; idxOnPage < charsPerPage && pageStart + idxOnPage < rosterKeys.Count; idxOnPage++)
            {
                int i = pageStart + idxOnPage;
                string k = rosterKeys[i];
                var scfg = SurvivorDatabase.Classes[k];
                int row = idxOnPage / 2, col = idxOnPage % 2;
                int bx = leftInnerX + col * (cellW + cellGapX);
                int by = gridTopY + row * cellRowStep;
                Rectangle rect = new(bx, by, cellW, cellBtnH);
                bool isSel = k == _selectedChar;
                bool isUnlocked = prog.IsCharacterUnlocked(k);
                bool isSecret = k is "sanabi" or "bird_trio" or "apocalypse_bird";
                bool isApocBlocked = k == "apocalypse_bird" && apocTaken;

                string readableName = GetReadableRosterName(k, scfg);
                string btnTitle = isApocBlocked
                    ? $"[ЗАНЯТО] {readableName}"
                    : (isUnlocked
                        ? (isSecret ? "★ " : "") + readableName
                        : $"[ЗАКР.] {readableName}");
                Color bgCol = isSel
                    ? Rgb(44, 86, 148, 250)
                    : (isApocBlocked ? Rgb(56, 22, 28, 242) : (isSecret ? Rgb(54, 36, 20, 245) : (isUnlocked ? Rgb(20, 26, 40, 240) : Rgb(14, 16, 24, 235))));
                Color borderCol = isSel
                    ? Color.Yellow
                    : (isApocBlocked ? Color.Red : (isSecret ? Color.Gold : (isUnlocked ? Rgb(scfg.BodyColor.R, scfg.BodyColor.G, scfg.BodyColor.B, 140) : Rgb(75, 82, 98, 130))));
                Color txtCol = isSel
                    ? Color.Yellow
                    : (isApocBlocked ? Color.Orange : (isSecret ? Color.Gold : (isUnlocked ? Color.White : Rgb(132, 142, 162))));

                if (Engine.DrawButton(rect, btnTitle, bgCol, Rgb(56, 98, 165), txtCol, rosterFont, borderCol))
                {
                    if (isApocBlocked)
                    {
                        SoundManager.Play("ui_error");
                        ShowBanner("В ЛОББИ МОЖЕТ БЫТЬ ТОЛЬКО ОДНА ПТИЦА АПОКАЛИПСИСА!", Color.Red, 3.8f);
                    }
                    else
                    {
                        SelectCharacter(k);
                        if (!isUnlocked)
                        {
                            var (howTo, _) = GetCharacterUnlockInfo(k);
                            ShowBanner($"КАК ОТКРЫТЬ «{scfg.Name}»: {howTo}", Color.Orange, 3.8f);
                        }
                    }
                }
                Raylib.DrawRectangle(bx, by, 4, cellBtnH, isSel ? Color.Yellow : (isUnlocked ? scfg.BodyColor : Rgb(70, 75, 90)));
            }

            if (totalCharPages > 1)
            {
                int navBtnW = Math.Min(96, leftInnerW / 3);
                if (Engine.DrawButton(new Rectangle(leftInnerX, pageBarY, navBtnW, pageBarH), "<- СТР.",
                    Rgb(28, 38, 60), Rgb(46, 66, 102), Color.White, 12f, Color.SkyBlue))
                {
                    _lobbyCharPage = (_lobbyCharPage - 1 + totalCharPages) % totalCharPages;
                }
                Engine.DrawTextCentered($"СТР. {_lobbyCharPage + 1}/{totalCharPages}", leftX + leftW * 0.5f, pageBarY + pageBarH * 0.5f, 12.5f, Color.Gold);
                if (Engine.DrawButton(new Rectangle(leftInnerX + leftInnerW - navBtnW, pageBarY, navBtnW, pageBarH), "СТР. ->",
                    Rgb(28, 38, 60), Rgb(46, 66, 102), Color.White, 12f, Color.SkyBlue))
                {
                    _lobbyCharPage = (_lobbyCharPage + 1) % totalCharPages;
                }
            }
            else
            {
                Engine.DrawTextCentered("ВСЕ ГЕРОИ НА ОДНОЙ СТРАНИЦЕ", leftX + leftW * 0.5f, pageBarY + pageBarH * 0.5f, 11.5f, Rgb(140, 165, 198));
            }

            if (showShipBtn)
            {
                bool shipUnlLobby = IsSafeTravelsUnlocked();
                if (Engine.DrawButton(new Rectangle(leftInnerX, extraBtnY, leftInnerW, extraBtnH),
                    shipUnlLobby ? "★ НА БОРТ UES «SAFE TRAVELS»" : "[ЗАКР.] UES «SAFE TRAVELS»",
                    shipUnlLobby ? Rgb(28, 76, 126) : Rgb(28, 28, 38),
                    shipUnlLobby ? Rgb(48, 114, 182) : Rgb(44, 44, 58),
                    shipUnlLobby ? Color.Yellow : Color.Gray, 12.2f,
                    shipUnlLobby ? Color.SkyBlue : Color.Gray))
                {
                    if (shipUnlLobby) EnterSafeTravels(_selectedChar);
                    else
                    {
                        SoundManager.Play("ui_error");
                        ShowBanner("ПОБЕДИТЕ МИТРИКСА ХОТЯ БЫ 1 РАЗ, ЧТОБЫ ОТКРЫТЬ КОРАБЛЬ UES «SAFE TRAVELS»!", Color.Orange, 3.5f);
                    }
                }
            }
            else if (showSpecBtn)
            {
                string specBtnTitle = _lobbyJoinAsSpectator
                    ? "★ ЗРИТЕЛЬ (НАБЛЮДАТЕЛЬ): ВКЛ ✓"
                    : "ВОЙТИ КАК ЗРИТЕЛЬ: ВЫКЛ";
                if (Engine.DrawButton(new Rectangle(leftInnerX, extraBtnY, leftInnerW, extraBtnH), specBtnTitle,
                    _lobbyJoinAsSpectator ? Rgb(36, 92, 148) : Rgb(24, 30, 46),
                    Rgb(52, 118, 186),
                    _lobbyJoinAsSpectator ? Color.Yellow : Color.SkyBlue, 12.2f,
                    _lobbyJoinAsSpectator ? Color.Yellow : Color.SkyBlue))
                {
                    _lobbyJoinAsSpectator = !_lobbyJoinAsSpectator;
                    BroadcastLobbyState();
                    ShowBanner(
                        _lobbyJoinAsSpectator
                            ? "ВЫБРАНА РОЛЬ НАБЛЮДАТЕЛЯ: ВЫ СМОЖЕТЕ СМОТРЕТЬ ЗАБЕГ ГОСТЕМ БЕЗ УЧАСТИЯ В БОЮ!"
                            : "РЕЖИМ ЗРИТЕЛЯ ВЫКЛЮЧЕН: ВЫ ВЫЙДЕТЕ В ЗАБЕГ КАК АКТИВНЫЙ ИГРОК!",
                        _lobbyJoinAsSpectator ? Color.SkyBlue : Color.Lime, 3.2f);
                }
            }

            if (Engine.DrawButton(new Rectangle(leftInnerX, exitBtnY, leftInnerW, exitBtnH), "<- ГЛАВНОЕ МЕНЮ [ESC]",
                Rgb(82, 38, 46), Rgb(122, 54, 64), Color.White, compact ? 13f : 14.5f))
            {
                _net.StopSession();
                _lobbyPlayers.Clear();
                _screen = "main";
            }

            // =========================================================================
            // 2. ЦЕНТРАЛЬНАЯ ВИТРИНА: КАРТОЧКА СТАТОВ, 3D-МОДЕЛЬ, СКИНЫ И КНОПКА СТАРТА
            // =========================================================================
            float midCenterX = midX + midW * 0.5f;
            int topCardH = compact ? 78 : 92;
            Raylib.DrawRectangle(midX, topY, midW, topCardH, Rgb(12, 16, 28, 238));
            Raylib.DrawRectangle(midX, topY, midW, 4, activeSkin.PrimaryColor);

            string heroDisplayTitle = activeSkin.CustomSkillVariants != null
                ? $"{cfg.Name} :: {activeSkin.Name.ToUpper()}"
                : cfg.Name;
            Engine.DrawTextCentered(heroDisplayTitle, midCenterX, topY + (compact ? 16f : 19f), compact ? 18f : 21f, activeSkin.PrimaryColor);
            Engine.DrawTextCentered(cfg.Subtitle, midCenterX, topY + (compact ? 36f : 42f), compact ? 11.5f : 13f, Rgb(185, 215, 245));

            (string lbl, string val, Color c)[] statPills =
            {
                ("HP", $"{cfg.Hp}", Rgb(95, 230, 110)),
                ("УРОН", $"{cfg.Damage}", Rgb(255, 195, 65)),
                ("СКОР.", $"{cfg.Speed}", Color.SkyBlue),
                ("АТАКА", $"{cfg.FireRate}/с", Rgb(235, 130, 255))
            };
            int pillGap = 6;
            int pillW = (midW - 16 - pillGap * 3) / 4;
            int pillY = topY + topCardH - (compact ? 26 : 30);
            int pillH = compact ? 22 : 25;
            for (int si = 0; si < statPills.Length; si++)
            {
                int px = midX + 8 + si * (pillW + pillGap);
                Raylib.DrawRectangle(px, pillY, pillW, pillH, Rgb(20, 28, 44, 235));
                Engine.DrawTextCentered($"{statPills[si].lbl}: {statPills[si].val}", px + pillW * 0.5f, pillY + pillH * 0.5f, compact ? 11f : 12f, statPills[si].c);
            }

            // Расчёт нижнего стека центральной витрины снизу вверх
            bool isSteamSession = _net.Connected && _net.Transport == "steam";
            int statusH = isSteamSession ? (compact ? 78 : 88) : (compact ? 54 : 64);
            int statusY = topY + panelH - statusH;

            int startH = compact ? 46 : 58;
            int startY = statusY - startH - 6;

            int skinCardH = compact ? 68 : 80;
            int skinY = startY - skinCardH - 6;

            int hintH = compact ? 24 : 28;
            int hintY = skinY - hintH - 6;

            // Подсказка вращения 3D-модели или баннер превью навыка
            if (_skillPreviewTimer > 0f)
            {
                Raylib.DrawRectangle(midX, hintY, midW, hintH, Rgb(14, 20, 34, 240));
                Raylib.DrawRectangle(midX, hintY, 4, hintH, _skillPreviewColor);
                Engine.DrawTextCentered(_skillPreviewTitle, midCenterX, hintY + hintH * 0.5f, 12f, Color.Yellow);
            }
            else
            {
                int hintBoxW = Math.Min(midW - 16, 420);
                Raylib.DrawRectangle((int)(midCenterX - hintBoxW * 0.5f), hintY, hintBoxW, hintH, Rgb(10, 14, 24, 175));
                Engine.DrawTextCentered("ЛКМ/ПКМ — вращение 3D-героя  |  Колесо — масштаб", midCenterX, hintY + hintH * 0.5f, 11.5f, Rgb(170, 195, 225));
            }

            // Панель быстрого выбора Скина / Альтернативной Версии
            Raylib.DrawRectangle(midX, skinY, midW, skinCardH, Rgb(12, 16, 28, 242));
            Raylib.DrawRectangle(midX, skinY, midW, 3, Color.Gold);
            string skinHeader = cfg.Skins.Exists(s => s.CustomSkillVariants != null)
                ? "ОБЛИК / ВЕРСИЯ ГЕРОЯ (МЕНЯЕТ НАВЫКИ ★):"
                : "ВЫБОР ОБЛИКА (СКИНА):";
            Engine.DrawTextLeft(skinHeader, midX + 10, skinY + 6, compact ? 11.5f : 12.5f, Color.Gold);

            int skinCount = Math.Max(1, cfg.Skins.Count);
            int skinBtnGap = 6;
            int skinBtnW = (midW - 16 - skinBtnGap * (skinCount - 1)) / skinCount;
            int skinBtnY = skinY + (compact ? 23 : 26);
            int skinBtnH = skinCardH - (compact ? 28 : 32);
            int curSkinIdx = prog.GetSelectedSkin(_selectedChar);

            for (int si = 0; si < cfg.Skins.Count; si++)
            {
                var skDef = cfg.Skins[si];
                bool unlocked = prog.IsSkinUnlocked(_selectedChar, si, skDef.UnlockAchievementKey);
                bool selected = curSkinIdx == si && unlocked;
                Rectangle sRect = new(midX + 8 + si * (skinBtnW + skinBtnGap), skinBtnY, skinBtnW, skinBtnH);

                string prefix = !unlocked ? "[ЗАКР.] " : (skDef.CustomSkillVariants != null ? "★ " : "");
                string btnLabel = skDef.CustomSkillVariants != null && !compact
                    ? $"{prefix}{skDef.Name}\n[НАВЫКИ]"
                    : $"{prefix}{skDef.Name}";

                Color bgCol = selected
                    ? Rgb(48, 95, 160)
                    : (unlocked ? Rgb(24, 32, 50) : Rgb(18, 20, 28));
                Color borderCol = selected
                    ? Color.Yellow
                    : (unlocked ? skDef.PrimaryColor : Color.Gray);

                if (Engine.DrawButton(sRect, btnLabel, bgCol, Rgb(65, 115, 185),
                    unlocked ? (selected ? Color.Yellow : Color.White) : Color.Gray, skinBtnW < 120 ? 10.5f : 11.5f, borderCol))
                {
                    if (unlocked)
                    {
                        prog.SetSelectedSkin(_selectedChar, si);
                        SelectCharacter(_selectedChar);
                    }
                    else
                    {
                        SoundManager.Play("ui_error");
                        ShowBanner($"ОБЛИК ЗАКРЫТ :: {GetSkinUnlockInfo(cfg, si)}", Color.Orange, 3.2f);
                    }
                }
            }

            // Кнопка старта забега
            bool guestSpectatorReadyAllowed = _net.Connected && _net.Mode == "client" && _net.GameType == "coop" && _lobbyJoinAsSpectator;
            bool isSelectedUnlocked = prog.IsCharacterUnlocked(_selectedChar) || guestSpectatorReadyAllowed;
            if (isSelectedUnlocked)
            {
                string startLabel = !_net.Connected
                    ? "НАЧАТЬ ЗАБЕГ (СТАРТ) [ENTER]"
                    : (guestSpectatorReadyAllowed
                        ? (_myReady ? "ГОТОВ КАК ЗРИТЕЛЬ ✓" : "ГОТОВ КАК ЗРИТЕЛЬ! [ENTER]")
                        : (_myReady ? "ВЫ ГОТОВЫ ✓ (ОЖИДАНИЕ)" : "ГОТОВ К БОЮ! [ENTER]"));
                Color btnBase = guestSpectatorReadyAllowed ? Rgb(34, 88, 146) : Rgb(36, 122, 70);
                Color btnHov = guestSpectatorReadyAllowed ? Rgb(52, 122, 196) : Rgb(54, 168, 96);
                Color btnBrd = guestSpectatorReadyAllowed ? Color.SkyBlue : Color.Lime;
                if (Engine.DrawButton(new Rectangle(midX, startY, midW, startH), startLabel,
                    btnBase, btnHov, Color.White, compact ? 16f : 18.5f, btnBrd) || Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    if (!_net.Connected)
                    {
                        StartRun(_selectedChar);
                    }
                    else if (!_myReady)
                    {
                        _myReady = true;
                        SoundManager.Play("ui_ready");
                        BroadcastLobbyState();
                        EvaluateLobbyCountdown();
                    }
                }
            }
            else
            {
                var (howToUnlock, _) = GetCharacterUnlockInfo(_selectedChar);
                Raylib.DrawRectangle(midX, startY, midW, startH, Rgb(44, 20, 28, 246));
                Raylib.DrawRectangleLines(midX, startY, midW, startH, Color.Orange);
                Raylib.DrawRectangle(midX, startY, 5, startH, Color.Orange);
                Engine.DrawTextLeft("★ ПЕРСОНАЖ ЗАКРЫТ:", midX + 12, startY + 5, 12f, Color.Gold);
                Engine.DrawTextWrapped(howToUnlock, midX + 12, startY + 21, midW - 24, 11.5f, Color.White, 2f, maxLines: 2);
                if ((Raylib.CheckCollisionPointRec(Engine.GetUiMousePosition(), new Rectangle(midX, startY, midW, startH))
                     && Raylib.IsMouseButtonPressed(MouseButton.Left)) || Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    SoundManager.Play("ui_error");
                    ShowBanner($"КАК ОТКРЫТЬ «{cfg.Name}»: {howToUnlock}", Color.Orange, 3.8f);
                }
            }

            // Нижняя плашка статуса комнаты, сложности и кнопок Steam-инвайта
            var diffInfo = GameData.GetActiveDifficulty();
            var effArts = GameData.EffectiveArtifacts;
            Raylib.DrawRectangle(midX, statusY, midW, statusH, Rgb(12, 16, 28, 238));
            Raylib.DrawRectangle(midX, statusY, 4, statusH, diffInfo.Color);
            if (!_net.Connected)
            {
                Engine.DrawTextLeft($"Одиночная игра  |  {diffInfo.Name} (x{diffInfo.ScaleMult:F2})", midX + 12, statusY + 8, compact ? 12.5f : 13.5f, diffInfo.Color);
                string artTxt = effArts.Count == 0
                    ? "Артефакты забега: Выкл (0/10)"
                    : $"Артефакты ({effArts.Count}): " + string.Join(", ", System.Linq.Enumerable.Select(effArts, ak => GameData.RunArtifacts[ak].Short));
                Engine.DrawTextWrapped(artTxt, midX + 12, statusY + (compact ? 28 : 32), midW - 24, 12f, effArts.Count > 0 ? Color.Gold : Rgb(165, 185, 210), 2f, maxLines: 1);
            }
            else
            {
                int specCnt = (_net.Mode == "client" && _net.GameType == "coop" && _lobbyJoinAsSpectator) ? 1 : 0;
                foreach (var lp in _lobbyPlayers.Values) if (lp.IsSpectator) specCnt++;
                string specNote = specCnt > 0 ? $" (+{specCnt} зрит.)" : "";
                string cdTxt = _lobbyCountdownActive ? $" | СТАРТ: {_lobbyCountdown:F1}С" : "";
                Engine.DrawTextLeft($"{_net.RoomCode} [{_net.Mode?.ToUpper()}]{cdTxt}", midX + 12, statusY + 7, compact ? 12.5f : 13.5f, Color.Lime);
                Engine.DrawTextLeft($"Игроков: {1 + _lobbyPlayers.Count}{specNote} | Сложность: {diffInfo.Name}", midX + 12, statusY + (compact ? 25 : 28), 12f, Color.SkyBlue);

                if (isSteamSession)
                {
                    int sBtnY = statusY + statusH - 30;
                    int sBtnW = (midW - 26) / 2;
                    if (Engine.DrawButton(new Rectangle(midX + 10, sBtnY, sBtnW, 25), "★ ПРИГЛАСИТЬ STEAM",
                        Rgb(32, 86, 146), Rgb(50, 118, 196), Color.Yellow, 11.5f, Color.SkyBlue))
                    {
                        Net.SteamSpacewarService.Instance.OpenInviteOverlay();
                        ulong lid = Net.SteamSpacewarService.Instance.CurrentLobbyId;
                        if (lid != 0) Raylib.SetClipboardText(lid.ToString());
                        ShowBanner("ОТКРЫТ STEAM OVERLAY (И ID ЛОББИ СКОПИРОВАН В БУФЕР ОБМЕНА)!", Color.SkyBlue, 3.0f);
                    }
                    if (Engine.DrawButton(new Rectangle(midX + 16 + sBtnW, sBtnY, sBtnW, 25), "КОПИРОВАТЬ ID",
                        Rgb(36, 54, 84), Rgb(54, 78, 122), Color.White, 11.5f, Color.Gold))
                    {
                        ulong lid = Net.SteamSpacewarService.Instance.CurrentLobbyId;
                        if (lid != 0)
                        {
                            Raylib.SetClipboardText(lid.ToString());
                            ShowBanner($"STEAM LOBBY ID СКОПИРОВАН: {lid}", Color.Gold, 3.0f);
                        }
                    }
                }
            }

            // =========================================================================
            // 3. ПРАВАЯ КОЛОНКА: НАВЫКИ, ДОСЬЕ И МОДИФИКАТОРЫ
            // =========================================================================
            DrawLobbyCharacterPanel(cfg, rightX, topY, rightW, panelH);
        }

        private static void DrawLobbyCharacterPanel(SurvivorConfig cfg, int px, int py, int pw, int ph)
        {
            cfg.SyncLoadout();
            var prog = Data.ProgressData.Instance;
            var activeSkin = cfg.GetActiveSkin();
            var effArts = GameData.EffectiveArtifacts;
            bool compact = ph < 680 || pw < 460;

            Raylib.DrawRectangle(px, py, pw, ph, Rgb(12, 16, 28, 244));
            Raylib.DrawRectangle(px, py, pw, 4, activeSkin.PrimaryColor);

            if (_lobbyTab == "loadout") _lobbyTab = "skills";

            (string tabId, string tabTitle)[] lobbyTabs =
            {
                ("skills", compact ? "1. НАВЫКИ" : "1. НАВЫКИ И СНАРЯЖЕНИЕ"),
                ("overview", compact ? "2. ДОСЬЕ" : "2. ДОСЬЕ И ОПИСАНИЕ"),
                ("modifiers", $"3. СЛОЖНОСТЬ ({effArts.Count})")
            };
            int tabGap = 6;
            int tabW = (pw - 24 - tabGap * 2) / 3;
            int tabH = compact ? 32 : 38;
            for (int ti = 0; ti < lobbyTabs.Length; ti++)
            {
                var (tid, ttitle) = lobbyTabs[ti];
                bool isTab = _lobbyTab == tid;
                Rectangle tRect = new(px + 12 + ti * (tabW + tabGap), py + 10, tabW, tabH);
                if (Engine.DrawButton(tRect, ttitle,
                    isTab ? Rgb(45, 92, 158) : Rgb(22, 28, 44),
                    Rgb(65, 120, 195),
                    isTab ? Color.Yellow : Color.White, compact ? 11.5f : 12.5f,
                    isTab ? Color.Yellow : activeSkin.PrimaryColor))
                {
                    _lobbyTab = tid;
                }
            }

            int bodyTop = py + 10 + tabH + 8;
            int bodyH = ph - (bodyTop - py) - 10;
            int innerX = px + 12;
            int innerW = pw - 24;

            if (_lobbyTab == "skills")
            {
                int passH = Math.Clamp((int)(bodyH * 0.115f), 54, 82);
                Raylib.DrawRectangle(innerX, bodyTop, innerW, passH, Rgb(18, 26, 44, 240));
                Raylib.DrawRectangle(innerX, bodyTop, 4, passH, Color.Gold);
                Engine.DrawTextLeft($"ПАССИВКА :: {cfg.PassiveName}", innerX + 12, bodyTop + 6, compact ? 13.5f : 15.5f, Color.Gold);
                Engine.DrawTextWrapped(cfg.PassiveDesc, innerX + 12, bodyTop + (compact ? 24 : 28), innerW - 22, compact ? 12f : 13.5f, Rgb(220, 232, 248), 2f, maxLines: 2);

                (string slot, string bindName, Color slotCol)[] slotInfos =
                {
                    ("m1", "ЛКМ", Color.Yellow),
                    ("m2", "ПКМ", Color.Orange),
                    ("shift", "SHIFT", Color.SkyBlue),
                    ("r", "R (УЛЬТА)", Color.Magenta)
                };

                var activeVariantsMap = activeSkin.CustomSkillVariants ?? cfg.SkillVariants;
                int sy = bodyTop + passH + 6;
                int cardGap = compact ? 5 : 8;
                int availCardsH = Math.Max(340, bodyH - passH - 6);
                int cardH = (availCardsH - cardGap * 3) / 4;

                foreach (var (slot, bindName, slotCol) in slotInfos)
                {
                    if (!cfg.Skills.TryGetValue(slot, out var sk)) continue;
                    int curVar = prog.GetSkillVariant(_selectedChar, slot);
                    var variants = activeVariantsMap.TryGetValue(slot, out var vList) ? vList : null;

                    Raylib.DrawRectangle(innerX, sy, innerW, cardH, Rgb(18, 24, 38, 240));
                    Raylib.DrawRectangle(innerX, sy, 4, cardH, slotCol);

                    Engine.DrawTextLeft($"[{bindName}] {sk.Name}", innerX + 12, sy + 6, compact ? 14f : 16f, slotCol);
                    Engine.DrawTextLeft($"КД: {sk.Cd}с", innerX + innerW - (compact ? 142 : 160), sy + 8, compact ? 12f : 13.5f, Color.Gold);

                    if (Engine.DrawButton(new Rectangle(innerX + innerW - 74, sy + 5, 66, 22), "▶ 3D",
                        Rgb(34, 62, 104), Rgb(54, 96, 156), Color.Yellow, 11.5f, slotCol))
                    {
                        TriggerLobbySkillPreview(_selectedChar, slot, curVar);
                    }

                    int varRowY = sy + (compact ? 28 : 32);
                    int varBtnH = compact ? 24 : 28;
                    if (variants != null && variants.Count > 0)
                    {
                        int vBtnGap = 6;
                        int vBtnW = (innerW - 24 - vBtnGap * (variants.Count - 1)) / variants.Count;
                        for (int vi = 0; vi < variants.Count; vi++)
                        {
                            var vSkill = variants[vi];
                            bool unlocked = prog.IsSkillUnlocked(_selectedChar, slot, vi);
                            bool isSel = curVar == vi && unlocked;
                            Rectangle vRect = new(innerX + 12 + vi * (vBtnW + vBtnGap), varRowY, vBtnW, varBtnH);

                            string tag = vi == 0 ? "1. СТАНД" : "2. АЛЬТ";
                            int maxNm = vBtnW < 180 ? 12 : 20;
                            string shortName = vSkill.Name.Length > maxNm ? vSkill.Name[..(maxNm - 2)] + ".." : vSkill.Name;
                            string btnTxt = unlocked ? $"{tag}: {shortName}" : $"[ЗАКР.] {tag}";

                            Color bgCol = isSel ? Rgb(44, 88, 152) : (unlocked ? Rgb(24, 32, 48) : Rgb(18, 20, 28));
                            Color brdCol = isSel ? Color.Yellow : (unlocked ? slotCol : Color.Gray);

                            if (Engine.DrawButton(vRect, btnTxt, bgCol, Rgb(62, 112, 182),
                                unlocked ? (isSel ? Color.Yellow : Color.White) : Color.Gray, compact ? 11f : 12f, brdCol))
                            {
                                if (unlocked)
                                {
                                    prog.SetSkillVariant(_selectedChar, slot, vi);
                                    cfg.SyncLoadout();
                                    TriggerLobbySkillPreview(_selectedChar, slot, vi);
                                }
                                else
                                {
                                    SoundManager.Play("ui_error");
                                    ShowBanner($"НАВЫК ЗАКРЫТ :: {GetSkillUnlockInfo(cfg, vSkill)}", Color.Orange, 3.2f);
                                }
                            }
                        }
                    }

                    int descTop = varRowY + varBtnH + 5;
                    int descAvailH = Math.Max(20, sy + cardH - descTop - 4);
                    float descFont = compact ? 12f : 13.5f;
                    int maxDescLines = Math.Max(1, descAvailH / (compact ? 15 : 18));
                    Engine.DrawTextWrapped(sk.Desc, innerX + 12, descTop, innerW - 22, descFont, Color.White, 2f, maxLines: maxDescLines);

                    sy += cardH + cardGap;
                }
            }
            else if (_lobbyTab == "overview")
            {
                int cy = bodyTop;
                int loreH = Math.Clamp((int)(bodyH * 0.26f), 110, 184);
                Raylib.DrawRectangle(innerX, cy, innerW, loreH, Rgb(18, 24, 40, 240));
                Raylib.DrawRectangle(innerX, cy, 4, loreH, activeSkin.PrimaryColor);
                Engine.DrawTextLeft($"ДОСЬЕ ВЫЖИВШЕГО :: {cfg.Name}", innerX + 12, cy + 8, compact ? 15f : 17.5f, activeSkin.PrimaryColor);
                Engine.DrawTextLeft(cfg.Subtitle, innerX + 12, cy + (compact ? 28 : 34), compact ? 12.5f : 14f, Color.Gold);
                int loreMaxLines = Math.Max(2, (loreH - (compact ? 48 : 58)) / 18);
                Engine.DrawTextWrapped(cfg.OverviewLore, innerX + 12, cy + (compact ? 48 : 56), innerW - 24, compact ? 12.5f : 14f, Rgb(220, 232, 248), 3f, maxLines: loreMaxLines);

                cy += loreH + 8;
                int passBoxH = Math.Clamp((int)(bodyH * 0.19f), 82, 138);
                Raylib.DrawRectangle(innerX, cy, innerW, passBoxH, Rgb(18, 24, 40, 240));
                Raylib.DrawRectangle(innerX, cy, 4, passBoxH, Color.SkyBlue);
                Engine.DrawTextLeft($"ВРОЖДЁННАЯ ОСОБЕННОСТЬ :: {cfg.PassiveName}", innerX + 12, cy + 8, compact ? 14f : 15.5f, Color.SkyBlue);
                int passMaxLines = Math.Max(2, (passBoxH - 36) / 18);
                Engine.DrawTextWrapped(cfg.PassiveDesc, innerX + 12, cy + 32, innerW - 24, compact ? 12.5f : 14f, Color.White, 3f, maxLines: passMaxLines);

                cy += passBoxH + 8;
                int skinsBoxH = Math.Clamp((int)(bodyH * 0.34f), 148, 234);
                Raylib.DrawRectangle(innerX, cy, innerW, skinsBoxH, Rgb(18, 24, 40, 240));
                Raylib.DrawRectangle(innerX, cy, 4, skinsBoxH, Color.Gold);
                Engine.DrawTextLeft("ДОСТУПНЫЕ ОБЛИКИ И ВЕРСИИ ПЕРСОНАЖА:", innerX + 12, cy + 8, compact ? 13.5f : 15.5f, Color.Gold);

                int curSkin = prog.GetSelectedSkin(_selectedChar);
                int skinRowStep = Math.Max(36, (skinsBoxH - 36) / Math.Max(1, cfg.Skins.Count));
                for (int si = 0; si < cfg.Skins.Count; si++)
                {
                    var skDef = cfg.Skins[si];
                    bool unlocked = prog.IsSkinUnlocked(_selectedChar, si, skDef.UnlockAchievementKey);
                    bool isSel = curSkin == si && unlocked;
                    int sy = cy + 32 + si * skinRowStep;
                    int rowH = skinRowStep - 4;
                    Raylib.DrawRectangle(innerX + 10, sy, innerW - 20, rowH, isSel ? Rgb(32, 52, 88, 235) : Rgb(14, 18, 30, 220));
                    Raylib.DrawRectangle(innerX + 10, sy, 3, rowH, unlocked ? skDef.PrimaryColor : Color.Gray);

                    string status = unlocked ? (isSel ? "[АКТИВЕН ✓]" : "[ОТКРЫТ]") : "[ЗАКРЫТ]";
                    Engine.DrawTextLeft($"{si + 1}. {skDef.Name}  {status}", innerX + 20, sy + 4, compact ? 12.5f : 14f, isSel ? Color.Yellow : (unlocked ? skDef.PrimaryColor : Color.Gray));
                    string skinLine = unlocked ? skDef.Desc : GetSkinUnlockInfo(cfg, si);
                    Engine.DrawTextWrapped(skinLine, innerX + 20, sy + (compact ? 20 : 23), innerW - 36, compact ? 11.5f : 12.5f, unlocked ? Rgb(195, 210, 232) : Color.Orange, 2f, maxLines: 1);
                }

                cy += skinsBoxH + 8;
                int squadH = Math.Max(68, (py + ph - 10) - cy);
                Raylib.DrawRectangle(innerX, cy, innerW, squadH, Rgb(16, 22, 36, 240));
                Raylib.DrawRectangle(innerX, cy, 4, squadH, Color.Lime);
                if (!_net.Connected)
                {
                    Engine.DrawTextLeft("СТАТУС ОТРЯДА: ОДИНОЧНЫЙ ЗАБЕГ", innerX + 12, cy + 10, compact ? 13.5f : 15f, Color.SkyBlue);
                    Engine.DrawTextWrapped("Для кооператива через Steam Spacewar или LAN откройте «Сетевая игра» в меню.", innerX + 12, cy + 32, innerW - 24, compact ? 12f : 13.5f, Rgb(185, 205, 230), 2f, maxLines: 2);
                }
                else
                {
                    Engine.DrawTextLeft($"ОТРЯД В ЛОББИ ({_net.RoomCode}):", innerX + 12, cy + 8, compact ? 13.5f : 15f, Color.Lime);
                    int ly = cy + 28;
                    Engine.DrawTextLeft($"• ВЫ: {cfg.Name} — [{(_myReady ? "ГОТОВ ✓" : "ВЫБИРАЕТ")}]", innerX + 12, ly, 12.5f, Color.White);
                    foreach (var kv in _lobbyPlayers)
                    {
                        ly += 19;
                        if (ly > cy + squadH - 18) break;
                        string cName = SurvivorDatabase.Classes.TryGetValue(kv.Value.CharKey, out var sc) ? sc.Name : "COMMANDO";
                        string pName = string.IsNullOrWhiteSpace(kv.Value.PlayerName) ? kv.Key.ToUpper() : kv.Value.PlayerName;
                        Engine.DrawTextLeft($"• {pName}: {cName} — [{(kv.Value.Ready ? "ГОТОВ ✓" : "ВЫБИРАЕТ")}]", innerX + 12, ly, 12.5f, Color.SkyBlue);
                    }
                }
            }
            else if (_lobbyTab == "modifiers")
            {
                Vector2 mp = Engine.GetUiMousePosition();
                var curDiff = GameData.GetActiveDifficulty();
                string effDiffKey = GameData.EffectiveDifficultyKey;
                string hoverTitle = $"{curDiff.Name}  —  {curDiff.Subtitle}";
                string hoverSub = $"Время: x{curDiff.ScaleMult:F2} | Живучесть врагов: x{curDiff.EnemyHpMult:F2} | Спавн: x{curDiff.SpawnRateMult:F2} | Реген: x{curDiff.RegenMult:F2}";
                string hoverDesc = curDiff.Desc;
                Color hoverCol = curDiff.Color;

                bool canEditMods = !_net.Connected || _net.Mode == "host";

                int dy = bodyTop;
                int diffBoxH = compact ? 96 : 118;
                Raylib.DrawRectangle(innerX, dy, innerW, diffBoxH, Rgb(18, 24, 40, 238));
                Raylib.DrawRectangle(innerX, dy, 4, diffBoxH, curDiff.Color);
                Engine.DrawTextLeft("РЕЖИМ СЛОЖНОСТИ ЗАБЕГА (4 УРОВНЯ):", innerX + 12, dy + 8, compact ? 13.5f : 15f, curDiff.Color);
                if (!canEditMods)
                    Engine.DrawTextLeft("[Хост]", innerX + innerW - 72, dy + 8, 12.5f, Color.Orange);

                int dGap = 6;
                int dBtnW = (innerW - 24 - dGap * 3) / 4;
                int dBtnH = diffBoxH - (compact ? 38 : 44);
                for (int di = 0; di < GameData.OrderedDifficultyKeys.Length; di++)
                {
                    string dKey = GameData.OrderedDifficultyKeys[di];
                    var dDef = GameData.Difficulties[dKey];
                    bool isSel = effDiffKey == dKey;
                    Rectangle dRect = new(innerX + 12 + di * (dBtnW + dGap), dy + (compact ? 30 : 34), dBtnW, dBtnH);

                    if (Raylib.CheckCollisionPointRec(mp, dRect))
                    {
                        hoverCol = dDef.Color;
                        hoverTitle = $"СЛОЖНОСТЬ: {dDef.Name} [{dDef.Icon}]";
                        hoverSub = $"Время: x{dDef.ScaleMult:F2} | HP врагов: x{dDef.EnemyHpMult:F2} | Спавн: x{dDef.SpawnRateMult:F2} | Урон по игроку: x{dDef.PlayerDmgTakenMult:F2}";
                        hoverDesc = dDef.Desc;
                    }

                    string dBtnTxt = $"{dDef.Name}\nx{dDef.ScaleMult:F2}";
                    if (Engine.DrawButton(dRect, dBtnTxt,
                        isSel ? Rgb(44, 78, 132) : Rgb(24, 30, 46),
                        Rgb(62, 105, 170),
                        isSel ? Color.Yellow : dDef.Color, compact ? 11.5f : 12.5f,
                        isSel ? Color.Yellow : dDef.Color) && canEditMods)
                    {
                        GameData.SelectedDifficulty = dKey;
                        prog.Save();
                        BroadcastLobbyState();
                    }
                }

                int ay = dy + diffBoxH + 8;
                int artBoxH = Math.Clamp((int)(bodyH * 0.38f), 162, 244);
                Raylib.DrawRectangle(innerX, ay, innerW, artBoxH, Rgb(18, 24, 40, 238));
                Raylib.DrawRectangle(innerX, ay, 4, artBoxH, Color.Gold);
                Engine.DrawTextLeft($"АРТЕФАКТЫ ({effArts.Count}/10):", innerX + 12, ay + 8, compact ? 13.5f : 15f, Color.Gold);

                if (canEditMods)
                {
                    int actBtnW = compact ? 96 : 114;
                    if (Engine.DrawButton(new Rectangle(innerX + innerW - actBtnW * 2 - 14, ay + 6, actBtnW, 24), "ВКЛ. ВСЕ",
                        Rgb(42, 72, 48), Rgb(62, 110, 72), Color.Lime, 11.5f, Color.Lime))
                    {
                        foreach (var ak in GameData.OrderedArtifactKeys) GameData.ActiveArtifacts.Add(ak);
                        prog.Save();
                        BroadcastLobbyState();
                    }
                    if (Engine.DrawButton(new Rectangle(innerX + innerW - actBtnW - 8, ay + 6, actBtnW, 24), "СБРОС",
                        Rgb(78, 38, 44), Rgb(115, 55, 65), Color.White, 11.5f, Color.Red))
                    {
                        GameData.ActiveArtifacts.Clear();
                        prog.Save();
                        BroadcastLobbyState();
                    }
                }

                int aGapX = 6, aGapY = 6;
                int aBtnW = (innerW - 24 - aGapX * 4) / 5;
                int aBtnH = Math.Max(48, (artBoxH - 44 - aGapY) / 2);
                for (int ai = 0; ai < GameData.OrderedArtifactKeys.Length; ai++)
                {
                    string aKey = GameData.OrderedArtifactKeys[ai];
                    var aDef = GameData.RunArtifacts[aKey];
                    bool enabled = GameData.HasArtifact(aKey);
                    int r = ai / 5, c = ai % 5;
                    Rectangle aRect = new(innerX + 12 + c * (aBtnW + aGapX), ay + 36 + r * (aBtnH + aGapY), aBtnW, aBtnH);

                    if (Raylib.CheckCollisionPointRec(mp, aRect))
                    {
                        hoverCol = aDef.Color;
                        hoverTitle = $"{aDef.Name}  [{aDef.Short}]";
                        hoverSub = $"{aDef.Subtitle}  —  {(enabled ? "ВКЛЮЧЁН ✓" : "ВЫКЛЮЧЕН")}";
                        hoverDesc = aDef.Desc;
                    }

                    string shortTitle = aDef.Name.Replace("Артефакт ", "");
                    string aBtnTxt = $"{(enabled ? "★ ВКЛ" : "ВЫКЛ")}\n{shortTitle}";
                    if (Engine.DrawButton(aRect, aBtnTxt,
                        enabled ? Rgb(48, 82, 138) : Rgb(22, 26, 38),
                        Rgb(65, 108, 175),
                        enabled ? Color.Yellow : Rgb(175, 190, 215), aBtnW < 88 ? 10.2f : 11.5f,
                        enabled ? Color.Yellow : aDef.Color) && canEditMods)
                    {
                        if (enabled) GameData.ActiveArtifacts.Remove(aKey);
                        else GameData.ActiveArtifacts.Add(aKey);
                        prog.Save();
                        BroadcastLobbyState();
                    }
                }

                int descY = ay + artBoxH + 8;
                int descBoxH = Math.Max(90, (py + ph - 10) - descY);
                Raylib.DrawRectangle(innerX, descY, innerW, descBoxH, Rgb(14, 20, 34, 248));
                Raylib.DrawRectangle(innerX, descY, innerW, 3, hoverCol);
                Engine.DrawTextLeft(hoverTitle, innerX + 12, descY + 10, compact ? 15f : 17f, hoverCol);
                Engine.DrawTextWrapped(hoverSub, innerX + 12, descY + (compact ? 30 : 34), innerW - 24, compact ? 12f : 13.5f, Color.Gold, 2f, maxLines: 2);
                int maxDescLines = Math.Max(2, (descBoxH - (compact ? 58 : 66)) / 18);
                Engine.DrawTextWrapped(hoverDesc, innerX + 12, descY + (compact ? 54 : 62), innerW - 24, compact ? 12.5f : 14f, Color.White, 3f, maxLines: maxDescLines);
            }
        }

        public static void StartReplayPlayback(string replayRelPath, string returnScreen)
        {
            if (Data.ReplayViewer.StartPlayback(replayRelPath, returnScreen, out string err))
            {
                _mainPedestal.Enabled = false;
                _menuCrystal.Enabled = false;
                if (_previewActor != null) _previewActor.Enabled = false;
                _screen = "replay";
            }
            else
            {
                SoundManager.Play("ui_error");
                ShowBanner(err, Color.Red, 3.8f);
            }
        }

        private static void ExitReplayScreen()
        {
            string ret = Data.ReplayViewer.ReturnScreen;
            Data.ReplayViewer.StopPlayback();
            Engine.BackgroundColor = Rgb(18, 20, 32);
            _mainPedestal.Enabled = true;
            _menuCrystal.Enabled = true;
            if (_previewActor != null) _previewActor.Enabled = true;
            _screen = ret;
            if (_screen == "run_summary" && !_lastRunVictory)
                ShowKillerOnDiorama();
            else
                SelectCharacter(_selectedChar);
        }

        public static void StartRun(string charKey, int? seed = null, bool fromNetwork = false, int startStage = 1, string? startSpecialMode = null, bool asDedicatedSpectator = false)
        {
            if (!SurvivorDatabase.Classes.ContainsKey(charKey) || !ProgressData.Instance.IsCharacterUnlocked(charKey))
                charKey = "commando";
            _selectedChar = charKey;

            _screen = "in_game";
            _paused = false;
            _workbenchOpen = false;
            _consumeLeftClickOnResume = true;
            _resumeAttackSuppressTimer = 0.20f;
            _skipMouseDeltaFrames = 2;
            _mainPedestal.Enabled = false;
            _menuCrystal.Enabled = false;
            PurgeWorldEntities(keepCurrentRunPlayer: false);
            StageBuilder.GroundTiles.Clear();
            Physics.ResetStagePhysics();
            _isDedicatedSpectator = asDedicatedSpectator && _net.Connected;
            _isSpectating = _isDedicatedSpectator;
            _spectateFreeCam = false;
            _spectateYaw = 0f;
            _spectatePitch = 16f;
            _spectateZoomDist = 9.5f;
            _spectateSmoothPivot = new Vector3(0f, 2f, 0f);
            _spectateStageGraceTimer = 3.5f;
            _lastNetItemsSig = "";
            _netItemsResendTimer = 0f;
            _lastCompletedReplayPath = "";

            if (!_isDedicatedSpectator && (SurvivorDatabase.IsMobaCharacter(charKey) || charKey == "mindustry"))
                Raylib.EnableCursor();
            else
                Raylib.DisableCursor();

            RunKills = 0;
            RunBossKills = 0;
            RunDamageDealt = 0f;
            RunGoldEarned = 0;
            _deadCoopPlayers.Clear();
            _commandPickerQueue.Clear();
            _dialogueOpen = false;
            _rpsOpen = false;
            _emoteWheelOpen = false;
            _safeTravelsWardrobeOpen = false;
            World.CharacterUnlockSystems.ResetForNewRun();
            _run = new GameRunContext
            {
                CharKey = charKey,
                LunarCoins = Math.Max(2, ProgressData.Instance.LunarCoins)
            };
            foreach (var k in GameData.ItemPool.Keys) _run.Items[k] = 0;
            WorldContext.Game = _run;

            _run.PlayerEntity = new Entity(position: new(0, charKey == "mindustry" ? 1.2f : 0f, 0));
            _run.Parts = SurvivorDatabase.BuildSurvivorMesh(_run.PlayerEntity, charKey);
            _run.Combat = new SurvivorCombatController(_run);
            if (SurvivorCombatController.IsFirstPersonCharacter(charKey) || _isDedicatedSpectator)
                _run.Parts.Root.Enabled = false;
            _run.PlayerHp = _isDedicatedSpectator ? 0f : _run.PlayerMaxHp;

            string gameMode = !_net.Connected ? "solo" : (_net.GameType == "pvp" ? "pvp" : "coop");
            Data.ReplayRecorder.BeginRun(charKey, gameMode, _isDedicatedSpectator);

            int chosenSeed = seed ?? (Environment.TickCount & 0x7FFFFFFF);
            string? initMode = startSpecialMode ?? ((_net.Connected && _net.GameType == "pvp") ? "pvp" : null);
            LoadStage(startStage, initMode, chosenSeed, fromNetwork: true);

            if (_net.Connected && _net.Mode == "host" && !fromNetwork)
            {
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "lobby_start_run",
                    ["stage"] = _run.Stage,
                    ["seed"] = chosenSeed,
                    ["diff"] = GameData.SelectedDifficulty,
                    ["arts"] = string.Join(",", GameData.ActiveArtifacts)
                });
            }
        }

        public static void LoadStage(int stageNum, string? specialMode, int? fixedSeed = null, bool fromNetwork = false)
        {
            if (_run == null) return;
            if (_cutsceneTimer > 0f || IsValid(_cutsceneStageRoot))
                StopCharacterUnlockCutscene();
            EnsureLocalChairProp(false);
            foreach (var e in CombatManager.Enemies.ToArray()) Destroy(e);
            CombatManager.Enemies.Clear();
            foreach (var rp in _remotePlayers.Values) Destroy(rp);
            _remotePlayers.Clear();
            foreach (var e in _run.StageEntities.ToArray()) Destroy(e);
            _run.StageEntities.Clear();
            PurgeWorldEntities(keepCurrentRunPlayer: true);
            StageBuilder.GroundTiles.Clear();
            Physics.ResetStagePhysics();
            World.CharacterUnlockSystems.ResetForNewStage();
            _run.Combat.V1ActiveCoins.Clear();
            _run.Combat.V1ActiveCores.Clear();
            _run.Combat.V1Magnets.Clear();
            _run.Combat.JackOFlags.Clear();

            int activeLobbyCombatants = (_isDedicatedSpectator ? 0 : 1);
            if (_net.Connected)
            {
                foreach (var lp in _lobbyPlayers.Values)
                    if (!lp.IsSpectator) activeLobbyCombatants++;
            }
            int coopCount = Math.Max(1, activeLobbyCombatants);
            _run.Stage = stageNum;
            _run.StageElapsedTime = 0f;
            _run.TeleporterChargingNow = false;
            _run.TeleporterActiveTimer = 0f;
            _run.Gold = (stageNum == 1 && specialMode == null) ? 15 : 0;
            _run.Combat.MindustryCopper = specialMode == "bazaar" ? 0f : 65f;
            _run.Combat.MindustrySilicon = specialMode == "bazaar" ? 0f : 30f;
            _run.Combat.MindustryThorium = specialMode == "bazaar" ? 0f : 10f;
            _run.Combat.MindustryBuildMode = false;
            _run.Combat.MindustrySelectedBuild = -1;
            if (_run.Combat.MindustryShieldSphere != null) _run.Combat.MindustryShieldSphere.Enabled = false;
            _run.Combat.MindustryTurrets.Clear();
            _run.Combat.DummyClones.Clear();
            _run.Combat.JackOServants.Clear();
            _run.BlueOrbSpawned = false;
            _run.MoonPillarsDone = 0;
            _run.MithrixSpawned = false;
            _run.MoonDetonationTimer = 0f;
            _run.ActiveBoss = null;
            _run.ActiveHordeBosses.Clear();
            _run.HordeTotalMaxHp = 0f;
            _run.HordeTitle = "";
            var activeDiff = GameData.GetActiveDifficulty();
            _run.DirectorCredits = 34.0f * activeDiff.SpawnRateMult * _run.DifficultyCoeff * (1.0f + 0.55f * (coopCount - 1));

            int seed = fixedSeed ?? (Environment.TickCount & 0x7FFFFFFF);
            _currentStageSeed = seed;
            _spectateStageGraceTimer = 3.5f;
            var (ents, tpOrPad, pal) = StageBuilder.BuildStageWorld(stageNum, seed, specialMode);
            _run.StageEntities.AddRange(ents);
            if (!string.IsNullOrEmpty(pal.Id) && pal.Id != "pvp")
            {
                ProgressData.Instance.DiscoveredStages.Add(pal.Id);
                if (pal.Id == "rallypoint")
                    _run.UnlockAchievement("ach_blizzard");
                ProgressData.Instance.Save();
            }
            _deadCoopPlayers.Clear();
            _commandPickerQueue.Clear();
            _run.VengeanceTimer = 85.0f;
            if (GameData.HasArtifact("kin") && specialMode == null)
            {
                string[] kinPool = { "beetle", "lemurian", "jellyfish", "wisp", "hell_drone", "combine_soldier", "sarkaz_mercenary", "imp", "hollow_shadow", "bison", "nether_blaze", "brass_contraption", "greater_wisp", "golem", "parent", "elder_lemurian" };
                _run.StageKinType = kinPool[Math.Abs(seed + stageNum * 17) % kinPool.Length];
            }
            else
            {
                _run.StageKinType = "";
            }
            _run.ActiveTeleporterOrPad = tpOrPad;
            _run.StageName = pal.Name;
            Data.ReplayRecorder.RecordStage(stageNum, seed, specialMode, pal.Name, _run.ElapsedTime);

            if (_isDedicatedSpectator)
            {
                _isSpectating = true;
                _run.PlayerHp = 0f;
                _run.Parts.Root.Enabled = false;
                if (_run.Combat.FpRoot != null) _run.Combat.FpRoot.Enabled = false;
                _run.PlayerPos = new Vector3(0f, -500f, 0f);
                _run.PlayerVelY = 0f;
                _spectateSmoothPivot = specialMode == "moon" ? new Vector3(0f, 3f, -16f) : new Vector3(0f, 3f, 0f);
                RestoreInGameCursorIfNeeded();
            }
            else
            {
                _isSpectating = false;
                _run.PlayerHp = (stageNum == 1 && specialMode == null && GameData.EffectiveDifficultyKey == "eclipse")
                    ? _run.PlayerMaxHp * 0.80f
                    : MathF.Max(_run.PlayerHp, _run.PlayerMaxHp * 0.5f);
                _run.Parts.Root.Enabled = !SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey);
                if (SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey))
                    _run.Combat.BuildV1FpViewmodel();

                if (specialMode == "pvp")
                {
                    float spawnSide = _net.MyId == "host" ? -28f : 28f;
                    _run.PlayerPos = new Vector3(spawnSide, 1f, spawnSide);
                }
                else if (specialMode == "safe_travels") _run.PlayerPos = new Vector3(0f, 1f, -6f);
                else if (specialMode == "bazaar") _run.PlayerPos = new Vector3(0, 1f, -2f);
                else if (specialMode == "moon") _run.PlayerPos = new Vector3(0, 1f, -16f);
                else _run.PlayerPos = new Vector3(0, 1f, 0f);
                _run.PlayerVelY = 0f;
                RestoreInGameCursorIfNeeded();
            }
            // Новый этап: трассировка движения начинается заново с точки высадки (без «протаскивания» с прошлой карты)
            _playerSweepValid = false;
            _lastResolvedPlayerPos = _run.PlayerPos;

            if (!_isDedicatedSpectator && _run.CharKey == "mindustry" && specialMode is not "bazaar" and not "safe_travels")
                _run.Combat.SpawnMindustryMainCore(_run.PlayerPos);

            if (_net.Connected && !fromNetwork)
            {
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "stage_sync",
                    ["stage"] = stageNum,
                    ["seed"] = seed,
                    ["special_mode"] = specialMode
                });
            }

            if (specialMode == "pvp")
                ShowBanner("КОЛИЗЕЙ РАЗДОРА (PvP) :: ВЫБИВАЙТЕ ПРЕДМЕТЫ ИЗ МОНСТРОВ И СОКРУШИТЕ СОПЕРНИКА!", Color.Red, 4.5f);
            else if (specialMode == "safe_travels")
                ShowBanner("КОРАБЛЬ UES «SAFE TRAVELS» :: КАЮТ-КОМПАНИЯ, МОСТИК, КАЮТЫ И БОЕВОЙ ПОЛИГОН", Rgb(85, 225, 255), 4.5f);
            else if (specialMode == "bazaar")
                ShowBanner("БАЗАР МЕЖДУ ВРЕМЕНЕМ :: ТОРГОВЛЯ С ТРИТОНОМ", Rgb(80, 215, 255), 3.5f);
            else if (specialMode == "pizzeria")
                ShowBanner("ПИЦЦЕРИЯ ФАЗБЕРА :: ПОДДЕРЖИВАЙТЕ ГЕНЕРАТОР МУЗЫКАЛЬНОЙ ШКАТУЛКИ!", Rgb(235, 165, 50), 4.5f);
            else if (specialMode == "black_mesa")
                ShowBanner("СЕКТОР ЧЁРНАЯ МЕЗА :: ПРОНЕСИТЕ КРИСТАЛЛ GG-3883 В АНТИМАСС-СПЕКТРОМЕТР!", Rgb(255, 145, 35), 4.5f);
            else if (specialMode == "qliphoth")
                ShowBanner("ВЕРШИНА ДРЕВА КЛИФОТ :: ПОБЕДИТЕ ДЕМОНИЧЕСКОГО СТРАЖА ВЕРГИЛИЯ!", Rgb(185, 65, 245), 4.5f);
            else if (specialMode == "hell_cybergrind")
                ShowBanner("ПРЕДЕЛ АДА (КИБЕР-ГРАЙНД) :: СОКРУШИТЕ ВОЛНЫ АДСКИХ МАШИН!", Rgb(255, 65, 75), 4.5f);
            else if (specialMode == "tartarus")
                ShowBanner("БАШНЯ ТАРТАР (ТЁМНЫЙ ЧАС) :: ОДОЛЕЙТЕ ТЕНЕЙ И ЖНЕЦА ТАРТАРА!", Rgb(85, 255, 165), 4.5f);
            else if (specialMode == "hakurei_shrine")
                ShowBanner("ХРАМ ХАКУРЕЙ (ГЕНСОКЁ) :: ВЫДЕРЖИТЕ ДАНМАКУ И ЗАПЕЧАТАЙТЕ РАЗЛОМ!", Rgb(255, 145, 195), 4.5f);
            else if (specialMode == "tracen_derby")
                ShowBanner("ИППОДРОМ ТРЕЙСЕН :: ПРОЙДИТЕ ЧЕКПОЙНТЫ ФИНАЛЬНОГО ДЕРБИ!", Rgb(255, 210, 75), 4.5f);
            else if (specialMode == "dark_sanctuary")
                ShowBanner("ТЁМНОЕ СВЯТИЛИЩЕ :: ЗАКРОЙТЕ ТЁМНЫЙ ФОНТАН И ОТРАЗИТЕ РЫЦАРЯ!", Rgb(195, 75, 245), 4.5f);
            else if (specialMode == "terminal_lucidity")
                ShowBanner("ПОМОЙКА КИБЕР-МИРА :: СТАНЬТЕ [[БОЛЬШОЙ ШИШКОЙ]] В СДЕЛКЕ СПАМТОНА!", Rgb(255, 95, 205), 4.5f);
            else if (specialMode == "extraction_floor")
                ShowBanner("ЭТАЖ ФИЛОСОФИИ :: ПРОЙДИТЕ ИСПЫТАНИЕ АРБИТРА И СНА ЛА-МАНЧАЛЕНДА!", Rgb(235, 190, 65), 4.5f);
            else if (specialMode == "originium_core")
                ShowBanner("АССИМИЛИРОВАННОЕ ЯДРО PRTS :: ПЕРЕЗАПИШИТЕ ИСХОДНЫЙ КОД ОРИДЖИНИУМА!", Rgb(255, 165, 55), 4.5f);
            else if (specialMode == "penacony_theater")
                ShowBanner("БОЛЬШОЙ ТЕАТР ПЕНАКОНИИ :: УДЕРЖИТЕ КОНЦЕРТ ГАРМОНИИ ПРОТИВ ХОРА БАЛАНСА!", Rgb(245, 205, 115), 4.5f);
            else if (specialMode == "prime_meridian")
                ShowBanner("ГЛАВНЫЙ МЕРИДИАН :: ОЧИСТИТЕ ГЕОДЫ АУРЕЛИОНИТА ПОД БУРЕЙ ПРОВИДЕНИЯ!", Rgb(145, 205, 255), 4.5f);
            else if (specialMode == "moon")
                ShowBanner("НАЧАЛО (ЛУНА) :: ЗАРЯДИТЕ ВСЕ 4 СТОЛПА ЛУНЫ НА БАСТИОНАХ!", Rgb(100, 225, 255), 4.5f);
            else
            {
                string layerTag = GameData.GetStagePoolLayerLabel(stageNum);
                string kinNote = (!string.IsNullOrEmpty(_run.StageKinType) && GameData.MonsterLog.TryGetValue(_run.StageKinType, out var km))
                    ? $"  [РОДСТВО: {km.Name.ToUpper()}]"
                    : "";
                ShowBanner($"ЭТАП {stageNum} ({layerTag}): {pal.Name.ToUpper()}{kinNote}", Color.Yellow, 3.8f);
            }
            SoundManager.Play("run_start");
        }

        private static void FinishRunWithSummary(bool victory)
        {
            _lastRunVictory = victory;
            if (_run != null)
            {
                _lastRunTime = _run.ElapsedTime;
                _lastRunStage = _run.Stage;
                _lastRunItems.Clear();
                foreach (var k in _run.ItemOrder)
                    if (_run.Items.TryGetValue(k, out int cnt) && cnt > 0)
                        _lastRunItems.Add((k, cnt));

                if (!_run.IsSafeTravels)
                {
                    if (!_isDedicatedSpectator)
                    {
                        var prog = ProgressData.Instance;
                        prog.Kills += RunKills;
                        prog.BossKills += RunBossKills;
                        prog.MaxStage = Math.Max(prog.MaxStage, _run.Stage);
                        prog.LunarCoins = Math.Max(prog.LunarCoins, _run.LunarCoins);
                        if (victory)
                        {
                            prog.MithrixDefeated = true;
                            prog.Victories++;
                            prog.LunarCoins += 10;
                            _run.UnlockAchievement("ach_safe_travels");
                            _run.UnlockAchievement("ach_stage4");
                        }
                        prog.Save();
                    }

                    var savedEntry = Data.ReplayRecorder.FinalizeAndSave(
                        _run,
                        victory ? "victory" : "defeat",
                        _isDedicatedSpectator,
                        _lastKillerKey,
                        _lastKillerName,
                        _lastKillerRank,
                        _lastKillerDamage,
                        _lastKillerIsBoss,
                        _lastKillerIsElite,
                        _lastKillerIsDoppel);
                    _lastCompletedReplayPath = savedEntry?.ReplayFile ?? "";
                    if (savedEntry != null)
                        _selectedHistoryRunId = savedEntry.RunId;
                }
            }
            QuitRunToLobby(toMainMenu: false);
            _screen = "run_summary";
            if (!victory)
            {
                _summaryShowKiller3D = true;
                ShowKillerOnDiorama();
            }
            else
            {
                _summaryShowKiller3D = false;
            }
        }

        private static void CleanupLocalPlayerSummonsOnDeath()
        {
            if (_run == null) return;
            foreach (var st in _run.Combat.MindustryTurrets.ToArray())
                if (IsValid(st)) Destroy(st);
            _run.Combat.MindustryTurrets.Clear();
            foreach (var sv in _run.Combat.JackOServants.ToArray())
                if (IsValid(sv)) Destroy(sv);
            _run.Combat.JackOServants.Clear();
            foreach (var cl in _run.Combat.DummyClones.ToArray())
                if (IsValid(cl)) Destroy(cl);
            _run.Combat.DummyClones.Clear();
            EnsureLocalChairProp(false);
            _emoteWheelOpen = false;
            _workbenchOpen = false;
        }

        private static void EnterPostDeathSpectatorMode(string bannerMsg)
        {
            if (_run == null) return;
            _isSpectating = true;
            _spectateFreeCam = false;
            _spectateYaw = _run.PlayerEntity.RotationY;
            _spectatePitch = 16f;
            _spectateZoomDist = 9.5f;
            _spectateSmoothPivot = GetNearestRemotePlayerPos(Vector3.Zero, out _) + new Vector3(0f, 1.95f, 0f);
            _run.PlayerHp = 0f;
            _run.Combat.Shield = 0f;
            _run.Combat.MindustryBuildMode = false;
            _run.Parts.SetEmote("");
            _run.Parts.Root.Enabled = false;
            if (_run.Combat.FpRoot != null) _run.Combat.FpRoot.Enabled = false;
            _run.PlayerPos = new Vector3(0f, -500f, 0f);
            CleanupLocalPlayerSummonsOnDeath();
            RestoreInGameCursorIfNeeded();
            BroadcastLocalPlayerState(isMoving: false, forceItems: true);
            ShowBanner(bannerMsg, Color.Orange, 4.8f);
        }

        public static void OnPlayerDefeated()
        {
            if (IsSpectating) return;
            SoundManager.Play("player_death");
            CleanupLocalPlayerSummonsOnDeath();

            if (Physics.SpecialMode == "pvp" && _net.Connected)
            {
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "pvp_eliminated",
                    ["player_id"] = _net.MyId
                });
                int aliveFighters = 0;
                foreach (var kv in _remotePlayers)
                    if (!_deadCoopPlayers.Contains(kv.Key) && kv.Value.IsAliveCombatant) aliveFighters++;
                if (aliveFighters >= 2 && _run != null)
                {
                    _deadCoopPlayers.Add(_net.MyId);
                    EnterPostDeathSpectatorMode("ВЫ ВЫБЫЛИ ИЗ ДУЭЛИ! НАБЛЮДЕНИЕ ЗА ФИНАЛОМ МАТЧА [A/D, ЛКМ/ПКМ — Игрок | C — Камера]");
                    return;
                }
                _isSpectating = false;
                FinishRunWithSummary(victory: false);
                ShowBanner("ВЫ ПАЛИ НА АРЕНЕ PvP! МАТЧ ОКОНЧЕН", Color.Red, 5.5f);
                return;
            }

            if (_net.Connected && _remotePlayers.Count > 0)
            {
                _deadCoopPlayers.Add(_net.MyId);
                if (_run != null) _run.PlayerHp = 0f;
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                {
                    ["type"] = "player_dead",
                    ["player_id"] = _net.MyId
                });
                bool anyAlive = false;
                foreach (var kv in _remotePlayers)
                {
                    if (!_deadCoopPlayers.Contains(kv.Key) && kv.Value.IsAliveCombatant)
                    {
                        anyAlive = true;
                        break;
                    }
                }
                if (anyAlive && _run != null)
                {
                    EnterPostDeathSpectatorMode("ВЫ ПОГИБЛИ! РЕЖИМ НАБЛЮДАТЕЛЯ [A/D, ЛКМ/ПКМ — Сменить игрока | Колесо — Зум | C — Камера]");
                    return;
                }
                else
                {
                    _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                    {
                        ["type"] = "all_players_dead"
                    });
                }
            }
            _isSpectating = false;
            FinishRunWithSummary(victory: false);
            ShowBanner("ВЫ ПОГИБЛИ! ЗАБЕГ ОКОНЧЕН", Color.Red, 5.5f);
        }

        public static void OnVictory()
        {
            SoundManager.Play("victory");
            if (_net.Connected)
                _net.Broadcast(new System.Text.Json.Nodes.JsonObject { ["type"] = "run_victory" });
            FinishRunWithSummary(victory: true);
            ShowBanner("ПОБЕДА! ВЫ ПОКИНУЛИ ЛУНУ НА СПАСАТЕЛЬНОМ КОРАБЛЕ (+10 ЛУННЫХ МОНЕТ)!", Color.Gold, 8.0f);
        }

        private static void QuitRunToLobby(bool toMainMenu = false)
        {
            if (_run != null && !_run.IsSafeTravels && Data.ReplayRecorder.IsRecording)
            {
                var abEntry = Data.ReplayRecorder.FinalizeAndSave(
                    _run, "abandoned", _isDedicatedSpectator,
                    "", "", "", 0f, false, false, false);
                if (abEntry != null)
                    _selectedHistoryRunId = abEntry.RunId;
            }
            else
            {
                Data.ReplayRecorder.CancelWithoutSaving();
            }

            _paused = false;
            _workbenchOpen = false;
            _consumeLeftClickOnResume = false;
            _resumeAttackSuppressTimer = 0f;
            _skipMouseDeltaFrames = 0;
            _isSpectating = false;
            _isDedicatedSpectator = false;
            _spectateFreeCam = false;
            CloseSafeTravelsDialogueModal();
            _rpsOpen = false;
            _emoteWheelOpen = false;
            _safeTravelsWardrobeOpen = false;
            StopCharacterUnlockCutscene();
            EnsureLocalChairProp(false);
            _commandPickerQueue.Clear();
            _deadCoopPlayers.Clear();

            foreach (var e in CombatManager.Enemies.ToArray()) Destroy(e);
            CombatManager.Enemies.Clear();
            foreach (var rp in _remotePlayers.Values) Destroy(rp);
            _remotePlayers.Clear();
            if (_run != null)
            {
                foreach (var e in _run.StageEntities.ToArray()) Destroy(e);
                _run.StageEntities.Clear();
                if (_run.Combat?.FpRoot != null && !_run.Combat.FpRoot.IsDestroyed)
                    Destroy(_run.Combat.FpRoot);
                Destroy(_run.PlayerEntity);
                _run = null;
            }

            // Гарантированная очистка всех корневых и дочерних 3D-объектов локации, квестов, снарядов и физики
            PurgeWorldEntities(keepCurrentRunPlayer: false);
            StageBuilder.GroundTiles.Clear();
            Physics.ResetStagePhysics();
            Physics.SpecialMode = null;
            World.CharacterUnlockSystems.ResetForNewRun();

            CombatManager.Controller = null;
            WorldContext.Game = null;
            Raylib.EnableCursor();
            Engine.BackgroundColor = Rgb(18, 20, 32);
            _mainPedestal.Enabled = true;
            _menuCrystal.Enabled = true;
            _myReady = false;
            _lobbyCountdownActive = false;
            if (toMainMenu)
            {
                _net.StopSession();
                _lobbyPlayers.Clear();
                _screen = "main";
            }
            else
            {
                _screen = "lobby";
                BroadcastLobbyState();
            }
            if (!ProgressData.Instance.IsCharacterUnlocked(_selectedChar))
                _selectedChar = "commando";
            SelectCharacter(_selectedChar);
        }

        private static void RecordSkillCast(string slot)
        {
            if (_run == null) return;
            if (!string.IsNullOrEmpty(_run.Parts.ActiveEmote) && !_rpsOpen)
                SetLocalPlayerEmote("");

            if (_run.IsSafeTravels && _run.ZeroCooldownSandbox && _run.Combat != null)
            {
                _run.Combat.M1Cd = MathF.Min(_run.Combat.M1Cd, 0.08f);
                _run.Combat.M2Cd = 0f;
                _run.Combat.ShiftCd = 0f;
                _run.Combat.RCd = 0f;
            }
            if (_run.IsSafeTravels) return;

            World.CharacterUnlockSystems.OnPlayerSkillCast(_run, slot);
            if (slot == "m1") _run.M1CastCount++;
            else if (slot == "m2") _run.M2CastCount++;
            else if (slot == "shift") _run.ShiftCastCount++;
            else if (slot == "r") _run.RCastCount++;

            if (_run.M1CastCount >= 35) _run.UnlockAchievement("ach_bleeder");
            if (_run.M1CastCount >= 75) _run.UnlockAchievement("ach_lightning_strike");
            if (_run.M2CastCount >= 15) _run.UnlockAchievement("ach_secondary_master");
            if (_run.ShiftCastCount >= 12) _run.UnlockAchievement("ach_combat_dash");
            if (_run.RCastCount >= 5) _run.UnlockAchievement("ach_skill_spam");
            int totalCasts = _run.M1CastCount + _run.M2CastCount + _run.ShiftCastCount + _run.RCastCount;
            if (totalCasts >= 30) _run.UnlockAchievement("ach_arcane_burst");
            if (totalCasts >= 60) _run.UnlockAchievement("ach_cosmic_mind");
        }

        private static List<(string Id, RemotePlayer Player)> GetAliveSpectateTargets()
        {
            var list = new List<(string Id, RemotePlayer Player)>(_remotePlayers.Count);
            foreach (var kv in _remotePlayers)
            {
                if (!_deadCoopPlayers.Contains(kv.Key) && kv.Value.IsAliveCombatant)
                    list.Add((kv.Key, kv.Value));
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return list;
        }

        private static void BroadcastLocalPlayerState(bool isMoving, bool forceItems = false)
        {
            if (!_net.Connected || _run == null) return;
            var p = _run.PlayerEntity;
            var c = _run.Combat;

            var payload = new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "player_state",
                ["player_id"] = _net.MyId,
                ["name"] = Data.ProgressData.Instance.PlayerName,
                ["spectator"] = _isDedicatedSpectator,
                ["char_key"] = _run.CharKey,
                ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(p.X, 2), MathF.Round(p.Y, 2), MathF.Round(p.Z, 2)),
                ["rot_y"] = MathF.Round(p.RotationY, 1),
                ["hp"] = IsSpectating ? 0f : MathF.Round(_run.PlayerHp, 1),
                ["max_hp"] = _run.PlayerMaxHp,
                ["shield"] = IsSpectating ? 0f : MathF.Round(c.Shield, 1),
                ["gold"] = _run.Gold,
                ["lunar"] = _run.LunarCoins,
                ["moving"] = !IsSpectating && isMoving,
                ["ult_form"] = _run.Parts.UltFormActive,
                ["skin_idx"] = Data.ProgressData.Instance.GetSelectedSkin(_run.CharKey),
                ["grounded"] = _run.PlayerGrounded,
                ["vel_y"] = MathF.Round(_run.PlayerVelY, 1),
                ["shield_stance"] = c.ShieldStance,
                ["emote"] = IsSpectating ? "" : _run.Parts.ActiveEmote,
                ["anim_slot"] = _run.Parts.ActiveAnim,
                ["anim_step"] = _run.Parts.AnimStep,
                ["cds"] = new System.Text.Json.Nodes.JsonArray(
                    MathF.Round(MathF.Max(0f, c.M1Cd), 2),
                    MathF.Round(MathF.Max(0f, c.M2Cd), 2),
                    MathF.Round(MathF.Max(0f, c.ShiftCd), 2),
                    MathF.Round(MathF.Max(0f, c.RCd), 2))
            };

            string curItemsSig = Data.ReplayRecorder.BuildCompactItems(_run);
            if (forceItems || curItemsSig != _lastNetItemsSig || _netItemsResendTimer <= 0f)
            {
                payload["items"] = curItemsSig;
                _lastNetItemsSig = curItemsSig;
                _netItemsResendTimer = 1.0f;
            }

            _net.Broadcast(payload);
        }

        private static void TickHostEnemiesSync(float dt)
        {
            if (!_net.Connected || _net.Mode != "host" || _run == null) return;
            _enemySyncTimer -= dt;
            if (_enemySyncTimer > 0f) return;
            _enemySyncTimer = 0.12f;

            var arr = new System.Text.Json.Nodes.JsonArray();
            foreach (var e in CombatManager.Enemies)
            {
                if (!IsValid(e)) continue;
                arr.Add(new System.Text.Json.Nodes.JsonObject
                {
                    ["id"] = e.NetId,
                    ["et"] = e.EnemyType,
                    ["c"] = MathF.Round(e.Coeff, 2),
                    ["hp"] = MathF.Round(e.Hp, 1),
                    ["mhp"] = MathF.Round(e.MaxHp, 1),
                    ["ph"] = e.Phase,
                    ["x"] = MathF.Round(e.X, 2),
                    ["y"] = MathF.Round(e.Y, 2),
                    ["z"] = MathF.Round(e.Z, 2),
                    ["b"] = e.IsBoss,
                    ["a"] = e.IsAlly,
                    ["el"] = e.IsElite,
                    ["dp"] = e.IsDoppelganger
                });
            }

            var syncMsg = new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "enemies_sync",
                ["list"] = arr
            };
            if (_run.ActiveTeleporterOrPad is Teleporter tp && IsValid(tp))
            {
                syncMsg["tp_state"] = tp.State;
                syncMsg["tp_charge"] = MathF.Round(tp.Charge, 1);
                syncMsg["tp_aligned"] = tp.AlignedToMoon;
            }
            _net.Broadcast(syncMsg);
        }

        private static void TickNetworkStateInGame(float dt, bool isMoving)
        {
            if (!_net.Connected || _run == null) return;
            _netItemsResendTimer -= dt;
            _netSyncTimer -= dt;
            if (_netSyncTimer <= 0f)
            {
                _netSyncTimer = 0.05f;
                BroadcastLocalPlayerState(isMoving);
            }
            TickHostEnemiesSync(dt);
        }

        private static void UpdateInGame(float dt)
        {
            if (_run == null) return;

            if (_resumeAttackSuppressTimer > 0f)
                _resumeAttackSuppressTimer = MathF.Max(0f, _resumeAttackSuppressTimer - dt);
            if (_consumeLeftClickOnResume && !Raylib.IsMouseButtonDown(MouseButton.Left))
                _consumeLeftClickOnResume = false;
            bool suppressResumeLmb = _consumeLeftClickOnResume || _resumeAttackSuppressTimer > 0f;

            Vector2 rawMouseDelta = Raylib.GetMouseDelta();
            if (_skipMouseDeltaFrames > 0)
            {
                _skipMouseDeltaFrames--;
                rawMouseDelta = Vector2.Zero;
            }

            if (UpdateCharacterUnlockCutscene(dt))
                return;
            TickPostCutsceneGrace(dt);

            if (_run.IsSafeTravels && Raylib.IsKeyPressed(KeyboardKey.H) && !_dialogueOpen && !_rpsOpen && !_emoteWheelOpen)
            {
                _safeTravelsWardrobeOpen = !_safeTravelsWardrobeOpen;
                SoundManager.Play("ui_click");
                RestoreInGameCursorIfNeeded();
            }

            UpdateSafeTravelsDialogue(dt);
            UpdateRpsMiniGame(dt);
            UpdateEmoteWheelAndActiveEmote(dt);

            _run.ElapsedTime += dt;
            if (!_run.IsSafeTravels)
            {
                _run.StageElapsedTime += dt;
                if (!IsSpectating)
                {
                    if (_run.ElapsedTime >= 300f) _run.UnlockAchievement("ach_time_keeper");
                    if (_run.PlayerMaxHp >= 180f) _run.UnlockAchievement("ach_bison_hunt");
                    if (_run.PlayerMaxHp >= 260f) _run.UnlockAchievement("ach_immortal_titan");
                    if (_run.PlayerHp <= _run.PlayerMaxHp * 0.40f) _run.DroppedLowHp = true;
                    if (_run.BlueOrbSpawned) _run.UnlockAchievement("ach_lunar_stone");
                    if (CombatManager.Enemies.Count > 0)
                    {
                        _run.UntouchedCombatTimer += dt;
                        if (_run.UntouchedCombatTimer >= 35f) _run.UnlockAchievement("ach_untouched");
                    }
                }
                if (_run.MultiKillWindowTimer > 0f) _run.MultiKillWindowTimer -= dt;
                if (_run.FrenzyWindowTimer > 0f) _run.FrenzyWindowTimer -= dt;
                if (_run.PostHitTimer > 0f) _run.PostHitTimer -= dt;
                if (_run.TeleporterChargingNow) _run.TeleporterActiveTimer += dt;
                World.CharacterUnlockSystems.UpdateDirector(_run, dt);
            }

            float coeff = _run.DifficultyCoeff;
            CombatManager.Enemies.RemoveAll(e => !IsValid(e));

            if (_run.MoonDetonationTimer > 0f)
            {
                _run.MoonDetonationTimer -= dt;
                if (_run.MoonDetonationTimer <= 0f)
                {
                    FinishRunWithSummary(victory: false);
                    RecordPlayerDamageSource(null, 9999f, "moon_detonation");
                    ShowKillerOnDiorama();
                    ShowBanner("ЛУНА ДЕТОНИРОВАЛА! ВЫ НЕ УСПЕЛИ К СПАСАТЕЛЬНОМУ КОРАБЛЮ!", Color.Red, 5.0f);
                    return;
                }
            }

            // ИИ-Режиссёр: спавнит мобов с учётом выбранной сложности (темп, лимит врагов, состав этапа и шанс элиты)
            bool tpActivated = _run.ActiveTeleporterOrPad is Teleporter tpCheck && tpCheck.State == "ready";
            if (_net.Mode != "client" && Physics.SpecialMode is not "bazaar" and not "safe_travels" && !_run.MithrixSpawned && !tpActivated)
            {
                var activeDiff = GameData.GetActiveDifficulty();
                int aliveRemotes = 0;
                foreach (var rp in _remotePlayers.Values)
                    if (rp.IsAliveCombatant) aliveRemotes++;
                int playerCount = Math.Max(1, (!IsSpectating ? 1 : 0) + aliveRemotes);
                float playerMult = 1.0f + 0.55f * (playerCount - 1);
                int maxEnemies = activeDiff.MaxEnemiesBase + (playerCount - 1) * 12 + (GameData.HasArtifact("swarm") ? 16 : 0);

                // Артефакт Мести: вторжение теневого двойника каждые 85 секунд
                if (GameData.HasArtifact("vengeance"))
                {
                    _run.VengeanceTimer -= dt;
                    if (_run.VengeanceTimer <= 0f)
                    {
                        _run.VengeanceTimer = 85.0f;
                        var rngV = new Random();
                        float vang = (float)rngV.NextDouble() * MathF.PI * 2f;
                        Vector3 vengBase = !IsSpectating ? _run.PlayerPos : GetNearestRemotePlayerPos(Vector3.Zero, out _);
                        Vector3 vPos = Physics.FindFreeActorPos(new Vector3(
                            vengBase.X + MathF.Cos(vang) * 16f, vengBase.Y, vengBase.Z + MathF.Sin(vang) * 16f), 0.9f, 10f, 5f);
                        float vx = vPos.X, vy = vPos.Y, vz = vPos.Z;
                        var doppel = new CombatTarget(new Vector3(vx, vy, vz), "imp", coeff * 1.15f);
                        doppel.MakeDoppelganger();
                        CombatManager.Enemies.Add(doppel);
                        _run.StageEntities.Add(doppel);
                        ShowBanner("АРТЕФАКТ МЕСТИ :: ВАШ ТЕНЕВОЙ ДВОЙНИК ВТОРГСЯ В ЭТОТ МИР!", Rgb(185, 90, 255), 4.2f);
                        if (_net.Connected)
                        {
                            _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                            {
                                ["type"] = "enemy_spawn",
                                ["id"] = doppel.NetId,
                                ["et"] = "imp",
                                ["coeff"] = MathF.Round(coeff * 1.15f, 3),
                                ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(vx, 2), MathF.Round(vy, 2), MathF.Round(vz, 2)),
                                ["boss"] = false,
                                ["doppel"] = true
                            });
                        }
                    }
                }

                _run.DirectorCredits += (5.8f * activeDiff.SpawnRateMult * coeff * playerMult) * dt;
                _run.SpawnTimer -= dt;

                if (_run.SpawnTimer <= 0f && CombatManager.Enemies.Count < maxEnemies)
                {
                    var rng = new Random();
                    float intervalSpan = MathF.Max(0.10f, activeDiff.MaxSpawnInterval - activeDiff.MinSpawnInterval);
                    _run.SpawnTimer = (activeDiff.MinSpawnInterval + (float)rng.NextDouble() * intervalSpan) / MathF.Sqrt(playerCount);
                    (string et, float cost)[] table = Physics.SpecialMode switch
                    {
                        "moon" => new[] { ("lunar_chimera", 18f), ("brass_contraption", 22f), ("greater_wisp", 26f), ("golem", 28f), ("parent", 34f) },
                        "hell_cybergrind" => new[] { ("hell_drone", 15f), ("wisp", 16f), ("imp", 20f), ("nether_blaze", 24f), ("greater_wisp", 28f) },
                        "black_mesa" => new[] { ("combine_soldier", 18f), ("hell_drone", 16f), ("brass_contraption", 24f), ("golem", 30f) },
                        "qliphoth" => new[] { ("hollow_shadow", 17f), ("imp", 20f), ("sarkaz_mercenary", 22f), ("elder_lemurian", 40f) },
                        "tartarus" => new[] { ("hollow_shadow", 16f), ("wisp", 16f), ("lunar_chimera", 22f), ("parent", 36f) },
                        "hakurei_shrine" => new[] { ("wisp", 14f), ("hollow_shadow", 18f), ("greater_wisp", 26f), ("nether_blaze", 24f) },
                        "tracen_derby" => new[] { ("bison", 20f), ("sarkaz_mercenary", 20f), ("lemurian", 14f), ("golem", 32f) },
                        "pizzeria" => new[] { ("hollow_shadow", 16f), ("combine_soldier", 18f), ("brass_contraption", 24f) },
                        _ => _run.Stage >= 4
                            ? new[]
                            {
                                ("beetle", 10f), ("lemurian", 12f), ("jellyfish", 14f), ("wisp", 16f),
                                ("hell_drone", 18f), ("combine_soldier", 20f), ("sarkaz_mercenary", 21f),
                                ("imp", 22f), ("hollow_shadow", 23f), ("bison", 24f), ("nether_blaze", 25f),
                                ("brass_contraption", 26f), ("greater_wisp", 30f), ("golem", 34f),
                                ("parent", 40f), ("elder_lemurian", 45f)
                            }
                            : (_run.Stage >= 2
                                ? new[]
                                {
                                    ("beetle", 10f), ("lemurian", 12f), ("jellyfish", 14f), ("wisp", 16f),
                                    ("hell_drone", 18f), ("combine_soldier", 20f), ("imp", 22f),
                                    ("bison", 24f), ("brass_contraption", 26f), ("greater_wisp", 30f), ("golem", 34f)
                                }
                                : new[]
                                {
                                    ("beetle", 10f), ("lemurian", 12f), ("jellyfish", 14f), ("wisp", 16f),
                                    ("imp", 22f), ("golem", 34f)
                                })
                    };

                    int maxPack = Math.Max(playerCount + 1, (int)MathF.Round((2.2f + activeDiff.SpawnRateMult * 1.7f) + playerCount * 1.5f));
                    int packLimit = rng.Next(playerCount, maxPack + 1);
                    if (GameData.HasArtifact("dissonance"))
                    {
                        table = new[]
                        {
                            ("beetle", 10f), ("lemurian", 12f), ("jellyfish", 14f), ("wisp", 16f),
                            ("lunar_chimera", 18f), ("hell_drone", 18f), ("combine_soldier", 20f),
                            ("sarkaz_mercenary", 21f), ("imp", 22f), ("hollow_shadow", 23f),
                            ("bison", 24f), ("nether_blaze", 25f), ("brass_contraption", 26f),
                            ("greater_wisp", 30f), ("golem", 34f), ("parent", 40f), ("elder_lemurian", 45f)
                        };
                    }
                    if (GameData.HasArtifact("kin") && !string.IsNullOrEmpty(_run.StageKinType))
                    {
                        table = new[] { (_run.StageKinType, 16f) };
                    }
                    var aliveAnchors = new List<Vector3>(4);
                    if (!IsSpectating) aliveAnchors.Add(_run.PlayerPos);
                    foreach (var rp in _remotePlayers.Values)
                        if (rp.IsAliveCombatant) aliveAnchors.Add(rp.Position);

                    float eliteChance = Math.Clamp(activeDiff.BaseEliteChance + MathF.Max(0f, coeff - 1.35f) * 0.045f, 0f, 0.55f);

                    for (int s = 0; s < packLimit && CombatManager.Enemies.Count < maxEnemies && aliveAnchors.Count > 0; s++)
                    {
                        var affordable = new List<(string et, float cost)>();
                        foreach (var pair in table)
                            if (_run.DirectorCredits >= pair.cost) affordable.Add(pair);

                        if (affordable.Count == 0) break;

                        var chosen = affordable[rng.Next(affordable.Count)];
                        _run.DirectorCredits -= chosen.cost;

                        bool rollElite = _run.DirectorCredits >= chosen.cost * 0.45f && rng.NextDouble() < eliteChance;
                        if (rollElite)
                            _run.DirectorCredits -= chosen.cost * 0.45f;

                        // Спавним врагов рядом с любым из живых участников забега
                        Vector3 anchorPos = aliveAnchors[rng.Next(aliveAnchors.Count)];

                        float ang = (float)rng.NextDouble() * MathF.PI * 2f;
                        float dist = 15.0f + (float)rng.NextDouble() * 13.0f;
                        Vector3 spawnBase = Physics.FindFreeActorPos(new Vector3(
                            anchorPos.X + MathF.Cos(ang) * dist, anchorPos.Y, anchorPos.Z + MathF.Sin(ang) * dist), 1.0f, 10f, 4f);
                        int swarmSpawnCount = GameData.HasArtifact("swarm") ? 2 : 1;
                        for (int swi = 0; swi < swarmSpawnCount && CombatManager.Enemies.Count < maxEnemies; swi++)
                        {
                            Vector3 swPos = swi == 0 ? spawnBase
                                : Physics.FindFreeActorPos(spawnBase + new Vector3(swi * 1.6f, 0f, -swi * 1.6f), 1.0f, 6f, 4f);
                            float osx = swPos.X, osy = swPos.Y, osz = swPos.Z;
                            var ne = new CombatTarget(new Vector3(osx, osy, osz), chosen.et, coeff);
                            if (rollElite && !ne.IsElite)
                                ne.PromoteToElite();
                            CombatManager.Enemies.Add(ne);
                            _run.StageEntities.Add(ne);

                            if (_net.Connected)
                            {
                                _net.Broadcast(new System.Text.Json.Nodes.JsonObject
                                {
                                    ["type"] = "enemy_spawn",
                                    ["id"] = ne.NetId,
                                    ["et"] = chosen.et,
                                    ["coeff"] = MathF.Round(coeff, 3),
                                    ["pos"] = new System.Text.Json.Nodes.JsonArray(MathF.Round(osx, 2), MathF.Round(osy, 2), MathF.Round(osz, 2)),
                                    ["boss"] = false,
                                    ["elite"] = ne.IsElite
                                });
                            }
                        }
                    }
                }
            }

            var p = _run.PlayerEntity;
            var c = _run.Combat;

            if (IsSpectating)
            {
                _spectateStageGraceTimer = MathF.Max(0f, _spectateStageGraceTimer - dt);
                p.Position = new Vector3(0f, -500f, 0f);
                _run.Parts.Root.Enabled = false;
                if (c.FpRoot != null) c.FpRoot.Enabled = false;

                var aliveList = GetAliveSpectateTargets();
                if (aliveList.Count == 0)
                {
                    if (_spectateStageGraceTimer > 0f || (_isDedicatedSpectator && _run.StageElapsedTime < 5.0f))
                    {
                        Engine.Camera.FovY = _fov;
                        Engine.Camera.Position = new Vector3(0f, 9.5f, -16.0f);
                        Engine.Camera.Target = new Vector3(0f, 1.8f, 0f);
                        Engine.ObstructionFadeEnabled = false;
                        Data.ReplayRecorder.Tick(dt, _run, _remotePlayers, IsSpectating);
                        TickNetworkStateInGame(dt, isMoving: false);
                        return;
                    }

                    _isSpectating = false;
                    FinishRunWithSummary(victory: false);
                    ShowBanner("ВСЕ ИГРОКИ ПОГИБЛИ! ЗАБЕГ ОКОНЧЕН", Color.Red, 5.0f);
                    return;
                }

                if (!_paused && !HasAnyInGameModalOpen() && !Raylib.IsCursorHidden())
                {
                    Raylib.DisableCursor();
                    _skipMouseDeltaFrames = 2;
                    rawMouseDelta = Vector2.Zero;
                }

                if (!_paused && (Raylib.IsKeyPressed(KeyboardKey.C) || Raylib.IsKeyPressed(KeyboardKey.V)))
                {
                    _spectateFreeCam = !_spectateFreeCam;
                    if (_spectateFreeCam)
                    {
                        var curTarget = aliveList[_spectateTargetIdx % aliveList.Count].Player;
                        _spectateFreePos = curTarget.Position + new Vector3(0f, 4.2f, -7.2f);
                    }
                    SoundManager.Play("ui_click", 0.75f);
                }

                bool prevPressed = Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.Q)
                    || (!_spectateFreeCam && (Raylib.IsKeyPressed(KeyboardKey.A) || (!suppressResumeLmb && Raylib.IsMouseButtonPressed(MouseButton.Left))));
                bool nextPressed = Raylib.IsKeyPressed(KeyboardKey.Right)
                    || (!_spectateFreeCam && (Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsMouseButtonPressed(MouseButton.Right)));

                if (!_paused)
                {
                    if (prevPressed)
                        _spectateTargetIdx = (_spectateTargetIdx - 1 + aliveList.Count) % aliveList.Count;
                    if (nextPressed)
                        _spectateTargetIdx = (_spectateTargetIdx + 1) % aliveList.Count;

                    float wheel = Raylib.GetMouseWheelMove();
                    if (MathF.Abs(wheel) > 0.01f)
                        _spectateZoomDist = Math.Clamp(_spectateZoomDist - wheel * 1.35f, 3.2f, 28f);
                }

                Vector2 mdSpec = _paused ? Vector2.Zero : rawMouseDelta;
                _spectateYaw += mdSpec.X * _mouseSens;
                _spectatePitch = Math.Clamp(_spectatePitch + mdSpec.Y * _mouseSens, -78f, 78f);

                float yawRad = _spectateYaw * (MathF.PI / 180f);
                float pitchRad = _spectatePitch * (MathF.PI / 180f);
                Vector3 lookDir = Vector3.Normalize(new Vector3(
                    MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                    -MathF.Sin(pitchRad),
                    MathF.Cos(yawRad) * MathF.Cos(pitchRad)
                ));

                Engine.Camera.FovY = _fov;
                if (_spectateFreeCam)
                {
                    Vector3 flatRight = new(MathF.Cos(yawRad), 0f, -MathF.Sin(yawRad));
                    float flySpd = (Raylib.IsKeyDown(KeyboardKey.LeftShift) ? 36f : 16f) * dt;
                    if (!_paused)
                    {
                        if (Raylib.IsKeyDown(KeyboardKey.W)) _spectateFreePos += lookDir * flySpd;
                        if (Raylib.IsKeyDown(KeyboardKey.S)) _spectateFreePos -= lookDir * flySpd;
                        if (Raylib.IsKeyDown(KeyboardKey.D)) _spectateFreePos += flatRight * flySpd;
                        if (Raylib.IsKeyDown(KeyboardKey.A)) _spectateFreePos -= flatRight * flySpd;
                        if (Raylib.IsKeyDown(KeyboardKey.Space) || Raylib.IsKeyDown(KeyboardKey.E)) _spectateFreePos.Y += flySpd;
                        if (Raylib.IsKeyDown(KeyboardKey.LeftControl)) _spectateFreePos.Y -= flySpd;
                    }
                    float wbSpec = Physics.WorldBound + 24f;
                    _spectateFreePos.X = Math.Clamp(_spectateFreePos.X, -wbSpec, wbSpec);
                    _spectateFreePos.Z = Math.Clamp(_spectateFreePos.Z, -wbSpec, wbSpec);
                    float minFloorY = Physics.GetSurfaceHeight(_spectateFreePos.X, _spectateFreePos.Z, _spectateFreePos.Y) + 0.50f;
                    _spectateFreePos.Y = Math.Clamp(_spectateFreePos.Y, minFloorY, 175f);

                    Engine.Camera.Position = new Vector3(-_spectateFreePos.X, _spectateFreePos.Y, _spectateFreePos.Z);
                    Vector3 lookAt = _spectateFreePos + lookDir * 10f;
                    Engine.Camera.Target = new Vector3(-lookAt.X, lookAt.Y, lookAt.Z);
                    Engine.ObstructionFadeEnabled = false;
                }
                else
                {
                    var specTarget = aliveList[_spectateTargetIdx % aliveList.Count].Player;
                    Vector3 desiredPivot = specTarget.Position + new Vector3(0f, 1.95f, 0f);
                    float snapT = Vector3.Distance(_spectateSmoothPivot, desiredPivot) > 25f ? 1f : Math.Clamp(dt * 14f, 0f, 1f);
                    _spectateSmoothPivot = Vector3.Lerp(_spectateSmoothPivot, desiredPivot, snapT);

                    Vector3 camPos = Physics.ResolveCameraCollision(
                        specTarget.Position + new Vector3(0, 1.65f, 0),
                        _spectateSmoothPivot,
                        _spectateSmoothPivot - lookDir * _spectateZoomDist,
                        0.45f);
                    float camFloor = Physics.GetSurfaceHeight(camPos.X, camPos.Z, camPos.Y) + 0.55f;
                    if (camPos.Y < camFloor) camPos.Y = camFloor;

                    Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                    Vector3 lookAt = _spectateSmoothPivot + lookDir * 8.0f;
                    Engine.Camera.Target = new Vector3(-lookAt.X, lookAt.Y, lookAt.Z);
                    Engine.ObstructionFadeEnabled = true;
                    Engine.PlayerWorldPos = specTarget.Position;
                }

                Data.ReplayRecorder.Tick(dt, _run, _remotePlayers, IsSpectating);
                TickNetworkStateInGame(dt, isMoving: false);
                return;
            }

            float prevY = p.Y;
            bool isMoba = SurvivorDatabase.IsMobaCharacter(_run.CharKey);
            bool isMoving = false;
            float animStrafe = 0f;
            float animFwd = 0f;
            bool modalOpen = HasAnyInGameModalOpen();
            bool tabHeld = Raylib.IsKeyDown(KeyboardKey.Tab);
            if (Raylib.IsKeyPressed(KeyboardKey.Tab) && Raylib.IsCursorHidden())
                Raylib.EnableCursor();
            else if (Raylib.IsKeyReleased(KeyboardKey.Tab))
                RestoreInGameCursorIfNeeded();

            bool nearHubNpc = _run.IsSafeTravels && SafeTravelsNpc.IsPlayerNearAnyHubNpc(_run.PlayerPos);

            if (isMoba)
            {
                if (Raylib.IsCursorHidden())
                    Raylib.EnableCursor();

                Ray rlRay = Raylib.GetMouseRay(Engine.GetUiMousePosition(), Engine.Camera);
                Vector3 rayOrig = new(-rlRay.Position.X, rlRay.Position.Y, rlRay.Position.Z);
                Vector3 rayDir = Vector3.Normalize(new Vector3(-rlRay.Direction.X, rlRay.Direction.Y, rlRay.Direction.Z));
                if (!modalOpen && !tabHeld)
                {
                    float wb = Physics.WorldBound;
                    if (Physics.RaycastAimWorld(rayOrig, rayDir, 0.5f, 220f, out Vector3 worldHit))
                    {
                        c.MobaCursorPos = new Vector3(
                            Math.Clamp(worldHit.X, -wb, wb),
                            Physics.GetSurfaceHeight(worldHit.X, worldHit.Z, worldHit.Y + 1.5f),
                            Math.Clamp(worldHit.Z, -wb, wb)
                        );
                    }
                    else if (MathF.Abs(rayDir.Y) > 0.001f)
                    {
                        float tHit = (p.Y - rayOrig.Y) / rayDir.Y;
                        if (tHit > 0f && tHit < 220f)
                        {
                            Vector3 hitP = rayOrig + rayDir * tHit;
                            c.MobaCursorPos = new Vector3(
                                Math.Clamp(hitP.X, -wb, wb),
                                Physics.GetSurfaceHeight(hitP.X, hitP.Z, p.Y),
                                Math.Clamp(hitP.Z, -wb, wb)
                            );
                        }
                    }
                }

                if (!modalOpen && !tabHeld && Raylib.IsMouseButtonPressed(MouseButton.Right))
                {
                    CombatTarget? clickedEnemy = null;
                    float bestD = 2.6f;
                    foreach (var e in CombatManager.Enemies)
                    {
                        if (IsValid(e) && !e.IsAlly)
                        {
                            float d = Physics.Distance2D(c.MobaCursorPos, e.Position);
                            if (d <= e.HitRadius + bestD) { bestD = d; clickedEnemy = e; }
                        }
                    }
                    if (clickedEnemy != null)
                    {
                        c.MobaAttackTarget = clickedEnemy;
                        c.MobaMoveTarget = null;
                        new VisualFX(clickedEnemy.Position + new Vector3(0, 0.25f, 0), Color.Red, 1.6f, 0.20f);
                    }
                    else
                    {
                        c.MobaAttackTarget = null;
                        c.MobaMoveTarget = c.MobaCursorPos;
                        new VisualFX(c.MobaCursorPos + new Vector3(0, 0.15f, 0), Color.Lime, 1.3f, 0.20f);
                    }
                }
                else if (!modalOpen && !tabHeld && Raylib.IsMouseButtonDown(MouseButton.Right) && c.MobaAttackTarget == null)
                {
                    c.MobaMoveTarget = c.MobaCursorPos;
                }

                if (modalOpen)
                {
                    c.MobaMoveTarget = null;
                    c.MobaAttackTarget = null;
                }

                Vector3 moveDir = Vector3.Zero;
                if (IsValid(c.MobaAttackTarget) && !c.MobaAttackTarget!.IsAlly)
                {
                    Vector3 toEnemy = new(c.MobaAttackTarget.X - p.X, 0f, c.MobaAttackTarget.Z - p.Z);
                    float distE = toEnemy.Length();
                    if (distE > 3.6f)
                        moveDir = toEnemy / distE;
                    else
                    {
                        p.LookAt2D(c.MobaAttackTarget.Position);
                        if (c.M1Cd <= 0f) { c.UseM1Skill(); RecordSkillCast("m1"); }
                    }
                }
                else if (c.MobaMoveTarget.HasValue)
                {
                    Vector3 toPoint = new(c.MobaMoveTarget.Value.X - p.X, 0f, c.MobaMoveTarget.Value.Z - p.Z);
                    if (toPoint.Length() > 0.45f)
                        moveDir = Vector3.Normalize(toPoint);
                    else
                        c.MobaMoveTarget = null;
                }

                isMoving = moveDir.LengthSquared() > 0.01f;
                if (isMoving && !c.GrappleActive)
                {
                    p.Position += moveDir * c.MoveSpeed * dt;
                    p.LookAt2D(p.Position + moveDir);
                    animFwd = 1f;
                }

                c.HasCameraAimRay = false;
                c.Update(dt, isMoving);
                if (!modalOpen && !tabHeld)
                {
                    if (((!suppressResumeLmb && Raylib.IsMouseButtonDown(MouseButton.Left)) || Raylib.IsKeyDown(KeyboardKey.Q)) && c.M1Cd <= 0f)
                    {
                        p.LookAt2D(c.MobaCursorPos);
                        c.UseM1Skill();
                        RecordSkillCast("m1");
                    }
                    if ((Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsMouseButtonPressed(MouseButton.Middle)) && c.M2Cd <= 0f)
                    {
                        p.LookAt2D(c.MobaCursorPos);
                        c.UseM2Skill();
                        RecordSkillCast("m2");
                    }
                    if (Raylib.IsKeyPressed(KeyboardKey.LeftShift) && c.ShiftCd <= 0f)
                    {
                        c.MobaMoveTarget = null;
                        p.LookAt2D(c.MobaCursorPos);
                        c.UseShiftSkill();
                        RecordSkillCast("shift");
                    }
                    if (!nearHubNpc && Raylib.IsKeyPressed(KeyboardKey.R) && c.RCd <= 0f)
                    {
                        p.LookAt2D(c.MobaCursorPos);
                        c.UseRSkill();
                        RecordSkillCast("r");
                    }
                }
            }
            else
            {
                Vector2 md = (modalOpen || tabHeld) ? Vector2.Zero : rawMouseDelta;
                bool emotingFreeCam = !string.IsNullOrEmpty(_run.Parts.ActiveEmote) && !_rpsOpen && !_dialogueOpen;
                if (c.MedicTauntTimer <= 0f && !modalOpen && !tabHeld)
                {
                    if (emotingFreeCam)
                        _emoteCamYawOffset += md.X * _mouseSens;
                    else
                        p.RotationY += md.X * _mouseSens;
                }
                bool isFp = SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey) && c.MedicTauntTimer <= 0f && string.IsNullOrEmpty(_run.Parts.ActiveEmote) && !_dialogueOpen;
                float maxPitch = isFp ? 82f : 48f;
                float minPitch = isFp ? -82f : -42f;
                _run.CamPitch = Math.Clamp(_run.CamPitch + md.Y * _mouseSens, minPitch, maxPitch);

                float inX = (Raylib.IsKeyDown(KeyboardKey.D) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.A) ? 1f : 0f);
                float inZ = (Raylib.IsKeyDown(KeyboardKey.W) ? 1f : 0f) - (Raylib.IsKeyDown(KeyboardKey.S) ? 1f : 0f);
                animStrafe = inX;
                animFwd = inZ;
                isMoving = !modalOpen && (MathF.Abs(inX) > 0.01f || MathF.Abs(inZ) > 0.01f) && c.MedicTauntTimer <= 0f;

                if (isMoving && !c.GrappleActive)
                {
                    Vector3 moveDir = _run.CharKey == "mindustry"
                        ? Vector3.Normalize(new Vector3(inX, 0f, inZ))
                        : Vector3.Normalize(p.Forward * inZ + p.Right * inX);
                    p.Position += moveDir * c.MoveSpeed * dt;
                }

                // Предварительно синхронизируем луч прицела камеры перед выстрелами текущего кадра (без задержки в 1 кадр)
                if (_run.CharKey != "mindustry" && !emotingFreeCam && !_dialogueOpen && !_rpsOpen)
                {
                    Vector3 preLook = c.GetCameraLookDirection();
                    if (isFp)
                    {
                        c.LastCameraWorldPos = p.Position + new Vector3(0f, 1.72f, 0f);
                    }
                    else
                    {
                        Vector3 prePivot = p.Position + new Vector3(0f, 2.25f, 0f) + p.Right * 0.75f;
                        Vector3 preCam = Physics.ResolveCameraCollision(p.Position + new Vector3(0f, 1.85f, 0f), prePivot, prePivot - preLook * 8.6f, 0.48f);
                        float preFloor = Physics.GetSurfaceHeight(preCam.X, preCam.Z, preCam.Y) + 0.55f;
                        if (preCam.Y < preFloor) preCam.Y = preFloor;
                        c.LastCameraWorldPos = preCam;
                    }
                    c.LastCameraLookDir = preLook;
                    c.HasCameraAimRay = true;
                }
                else
                {
                    c.HasCameraAimRay = false;
                }

                c.Update(dt, isMoving);
                if (c.MedicTauntTimer <= 0f && !modalOpen && !tabHeld)
                {
                    Vector2 uiMp = Engine.GetUiMousePosition();
                    bool mouseOverBuildMenu = _run.CharKey == "mindustry" &&
                        uiMp.X >= Raylib.GetScreenWidth() - 525 &&
                        uiMp.Y >= Raylib.GetScreenHeight() - 285;

                    if (!suppressResumeLmb && !mouseOverBuildMenu && (_run.CharKey == "mindustry" && c.MindustryBuildMode && c.MindustrySelectedBuild >= 0
                        ? Raylib.IsMouseButtonPressed(MouseButton.Left)
                        : Raylib.IsMouseButtonDown(MouseButton.Left)) && c.M1Cd <= 0f)
                    {
                        c.UseM1Skill();
                        RecordSkillCast("m1");
                    }
                    if ((_run.CharKey is "sol" or "mindustry" or "freeman" ? Raylib.IsMouseButtonPressed(MouseButton.Right) : Raylib.IsMouseButtonDown(MouseButton.Right)) && c.M2Cd <= 0f)
                    {
                        c.UseM2Skill();
                        RecordSkillCast("m2");
                    }
                    if (Raylib.IsKeyPressed(KeyboardKey.LeftShift) && c.ShiftCd <= 0f)
                    {
                        c.UseShiftSkill();
                        RecordSkillCast("shift");
                        if (!_run.IsSafeTravels && _run.ShiftCastCount >= 15)
                            _run.UnlockAchievement("ach_item_ender_pearl");
                    }
                    if (!nearHubNpc && Raylib.IsKeyPressed(KeyboardKey.R) && c.RCd <= 0f)
                    {
                        c.UseRSkill();
                        RecordSkillCast("r");
                    }
                }
            }

            if (isMoving || c.GrappleActive)
            {
                if (!string.IsNullOrEmpty(_run.Parts.ActiveEmote) && !_rpsOpen)
                    SetLocalPlayerEmote("");
                _run.MoveStreakTimer += dt;
                if (!_run.IsSafeTravels && _run.MoveStreakTimer >= 20f) _run.UnlockAchievement("ach_marathon");
            }
            else
            {
                _run.MoveStreakTimer = 0f;
            }
            if (!_run.IsSafeTravels && c.StationaryTimer >= 12f) _run.UnlockAchievement("ach_stationary");
            if (!_run.IsSafeTravels && c.MoveSpeed >= c.Cfg.Speed * 1.65f) _run.UnlockAchievement("ach_supersonic");

            float floorH = Physics.GetSurfaceHeight(p.X, p.Z, p.Y, prevY);

            if (_run.CharKey == "mindustry")
            {
                if (Raylib.IsCursorHidden()) Raylib.EnableCursor();
                c.MindustryOverdriveTimer = MathF.Max(0f, c.MindustryOverdriveTimer - dt);

                Ray rlRay = Raylib.GetMouseRay(Engine.GetUiMousePosition(), Engine.Camera);
                Vector3 rayOrig = new(-rlRay.Position.X, rlRay.Position.Y, rlRay.Position.Z);
                Vector3 rayDir = Vector3.Normalize(new Vector3(-rlRay.Direction.X, rlRay.Direction.Y, rlRay.Direction.Z));
                if (!tabHeld)
                {
                    float wb = Physics.WorldBound;
                    if (Physics.RaycastAimWorld(rayOrig, rayDir, 0.5f, 260f, out Vector3 mHit))
                    {
                        c.MobaCursorPos = new Vector3(
                            Math.Clamp(mHit.X, -wb, wb),
                            Physics.GetSurfaceHeight(mHit.X, mHit.Z, mHit.Y + 2f),
                            Math.Clamp(mHit.Z, -wb, wb)
                        );
                    }
                    else if (MathF.Abs(rayDir.Y) > 0.001f)
                    {
                        float groundRefY = Physics.GetSurfaceHeight(p.X, p.Z, p.Y);
                        float tHit = (groundRefY - rayOrig.Y) / rayDir.Y;
                        if (tHit > 0f && tHit < 260f)
                        {
                            Vector3 hitP = rayOrig + rayDir * tHit;
                            c.MobaCursorPos = new Vector3(
                                Math.Clamp(hitP.X, -wb, wb),
                                Physics.GetSurfaceHeight(hitP.X, hitP.Z, groundRefY + 4f),
                                Math.Clamp(hitP.Z, -wb, wb)
                            );
                        }
                    }
                }
                p.LookAt2D(c.MobaCursorPos);

                if (Raylib.IsKeyPressed(KeyboardKey.B))
                    c.ToggleMindustryMode();

                if (c.MindustryBuildMode)
                {
                    KeyboardKey[] buildKeys = { KeyboardKey.One, KeyboardKey.Two, KeyboardKey.Three, KeyboardKey.Four, KeyboardKey.Five, KeyboardKey.Six, KeyboardKey.Seven, KeyboardKey.Eight, KeyboardKey.Nine };
                    for (int bi = 0; bi < buildKeys.Length; bi++)
                        if (Raylib.IsKeyPressed(buildKeys[bi])) c.MindustrySelectedBuild = bi;
                }

                float targetHoverY = c.MindustryBuildMode
                    ? (p.Y >= 105f ? Physics.ColosseumY + 14.5f : MathF.Min(floorH + 16.5f, Physics.ColosseumY - 18.0f))
                    : floorH + 0.85f;
                p.Y = Lerp(p.Y, targetHoverY, dt * 7.5f);
                _run.PlayerVelY = 0f;
                _run.PlayerGrounded = !c.MindustryBuildMode;
            }
            else if (!c.GrappleActive)
            {
                if (_run.DashTimer > 0f)
                {
                    p.Position += _run.DashVector * dt;
                    _run.DashTimer -= dt;
                }

                if (!HasAnyInGameModalOpen() && Raylib.IsKeyPressed(KeyboardKey.Space) && _run.PlayerJumpsLeft > 0)
                {
                    if (!string.IsNullOrEmpty(_run.Parts.ActiveEmote))
                        SetLocalPlayerEmote("");
                    SoundManager.Play(_run.PlayerGrounded ? "jump" : "double_jump");
                    _run.PlayerVelY = 12.0f;
                    _run.PlayerGrounded = false;
                    _run.PlayerJumpsLeft--;
                    int quill = CombatManager.GetItemCount("wax_quill");
                    if (quill > 0 && isMoving)
                        _run.ApplyDash(p.Forward * (14.0f + 4.0f * quill), 0.16f);
                    new VisualFX(p.Position, Color.SkyBlue, 1.8f, 0.22f);
                }

                if (isMoving && _run.PlayerGrounded)
                {
                    SoundManager.Play("footstep", 0.50f);
                }

                if (_run.CharKey == "reimu" && !_run.PlayerGrounded && !c.ReimuFlightBroken && c.ReimuFlightFuel > 0f && Raylib.IsKeyDown(KeyboardKey.Space) && _run.PlayerVelY <= 4.0f)
                {
                    c.ReimuIsFlying = true;
                    c.ReimuFlightFuel = MathF.Max(0f, c.ReimuFlightFuel - dt);
                    bool descend = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.C);
                    float targetVy = descend ? -8.5f : 6.5f;
                    _run.PlayerVelY = Lerp(_run.PlayerVelY, targetVy, dt * 10f);
                }
                else if (CombatManager.GetItemCount("elytra_curse") > 0 && !_run.PlayerGrounded && Raylib.IsKeyDown(KeyboardKey.Space) && _run.PlayerVelY < 0f)
                {
                    c.ReimuIsFlying = false;
                    int elCnt = CombatManager.GetItemCount("elytra_curse");
                    _run.PlayerVelY = MathF.Max(_run.PlayerVelY - 4.5f * dt, -3.2f);
                    p.Position += p.Forward * (4.8f + 1.8f * (elCnt - 1)) * dt;
                }
                else
                {
                    c.ReimuIsFlying = false;
                    _run.PlayerVelY -= 26.0f * dt;
                }

                p.Y += _run.PlayerVelY * dt;
                if (p.Y <= floorH)
                {
                    p.Y = floorH;
                    _run.PlayerVelY = 0f;
                    _run.PlayerGrounded = true;
                    _run.PlayerJumpsLeft = _run.PlayerMaxJumps;
                    c.ReimuFlightBroken = false;
                    c.ReimuFlightFuel = MathF.Min(4.5f, c.ReimuFlightFuel + 1.8f * dt);
                }
            }

            if (c.MedicTauntTimer <= 0f)
            {
                float spdRatio = c.MoveSpeed / MathF.Max(1f, c.Cfg.Speed);
                _run.Parts.UpdateAnimations(
                    _run.CharKey, dt, isMoving, _run.PlayerGrounded,
                    _run.PlayerVelY, spdRatio, animStrafe, animFwd,
                    c.ShieldStance, c.ReimuIsFlying, c.OrgiaTimer > 0f);
            }

            Vector3 resolved;
            Vector3 sweepDelta = p.Position - _lastResolvedPlayerPos;
            float sweepDist2D = MathF.Sqrt(sweepDelta.X * sweepDelta.X + sweepDelta.Z * sweepDelta.Z);
            if (_playerSweepValid && sweepDist2D <= 40f && MathF.Abs(sweepDelta.Y) <= 10f)
                resolved = Physics.SweepPlayerMovement(_lastResolvedPlayerPos, p.Position, 0.72f, p.Y);
            else
                resolved = Physics.ResolveHorizontalCollision(p.Position, 0.72f, p.Y);
            float b = Physics.WorldBound;
            p.X = Math.Clamp(resolved.X, -b, b);
            p.Z = Math.Clamp(resolved.Z, -b, b);
            _lastResolvedPlayerPos = p.Position;
            _playerSweepValid = true;

            if (Physics.SpecialMode == "moon" && _run.MithrixSpawned && _run.IsBossAlive && p.Y >= 105.0f)
            {
                float arenaR = MathF.Sqrt(p.X * p.X + p.Z * p.Z);
                if (arenaR > 52.5f)
                {
                    float ang = MathF.Atan2(p.Z, p.X);
                    p.X = MathF.Cos(ang) * 52.5f;
                    p.Z = MathF.Sin(ang) * 52.5f;
                }
                if (p.Y < 120.0f)
                {
                    p.Y = 120.0f;
                    _run.PlayerVelY = 0f;
                    _run.PlayerGrounded = true;
                    _run.PlayerJumpsLeft = _run.PlayerMaxJumps;
                }
            }

            Data.ReplayRecorder.Tick(dt, _run, _remotePlayers, IsSpectating);
            TickNetworkStateInGame(dt, isMoving);

            Engine.Camera.FovY = _fov;
            Vector3 camLookDir = c.GetCameraLookDirection();
            bool isEmotingNow = !string.IsNullOrEmpty(_run.Parts.ActiveEmote);
            bool inSideShotScene = (_dialogueOpen && IsValid(_dialogueNpcEntity)) || (_rpsOpen && IsValid(_rpsOppEntity));

            if (inSideShotScene)
            {
                c.HasCameraAimRay = false;
            }
            else if (isEmotingNow)
            {
                float yawRad = (p.RotationY + 12f + _emoteCamYawOffset) * (MathF.PI / 180f);
                Vector3 orbitDir = new(MathF.Sin(yawRad), 0f, MathF.Cos(yawRad));
                Vector3 pivot = p.Position + new Vector3(0f, _run.CharKey == "mindustry" ? 0.85f : 1.50f, 0f);
                float dist = _run.CharKey == "mindustry" ? 6.8f : 4.6f;
                Vector3 desired = pivot + orbitDir * dist + new Vector3(0f, 1.05f, 0f);
                Vector3 camPos = Physics.ResolveCameraCollision(pivot, pivot, desired, 0.42f);
                float camFloor = Physics.GetSurfaceHeight(camPos.X, camPos.Z, camPos.Y) + 0.45f;
                if (camPos.Y < camFloor) camPos.Y = camFloor;
                Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                Engine.Camera.Target = new Vector3(-pivot.X, pivot.Y, pivot.Z);
                c.HasCameraAimRay = false;
            }
            else if (_run.CharKey == "mindustry")
            {
                float camHeight = c.MindustryBuildMode ? 38.0f : 28.0f;
                Vector3 camPos = new(p.X, p.Y + camHeight, p.Z - 3.5f);
                Vector3 target = new(p.X, p.Y, p.Z);
                Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                Engine.Camera.Target = new Vector3(-target.X, target.Y, target.Z);
                c.HasCameraAimRay = false;
            }
            else if (isMoba)
            {
                Vector3 camPos = p.Position + new Vector3(0f, 22.0f, -14.5f);
                Vector3 target = p.Position + new Vector3(0f, 1.0f, 1.5f);
                Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                Engine.Camera.Target = new Vector3(-target.X, target.Y, target.Z);
                c.HasCameraAimRay = false;
            }
            else if (SurvivorCombatController.IsFirstPersonCharacter(_run.CharKey) && c.MedicTauntTimer <= 0f)
            {
                Vector3 eye = p.Position + new Vector3(0, 1.72f, 0);
                if (c.FpRoot != null && !c.FpRoot.IsDestroyed)
                {
                    c.FpRoot.Position = eye;
                    c.FpRoot.Rotation = new Vector3(_run.CamPitch, p.RotationY, 0f);
                }
                Engine.Camera.Position = new Vector3(-eye.X, eye.Y, eye.Z);
                Vector3 target = eye + camLookDir * 25f;
                Engine.Camera.Target = new Vector3(-target.X, target.Y, target.Z);
                c.LastCameraWorldPos = eye;
                c.LastCameraLookDir = camLookDir;
                c.HasCameraAimRay = true;
            }
            else
            {
                Vector3 pivot = p.Position + new Vector3(0, 2.25f, 0) + p.Right * 0.75f;
                Vector3 camPos = Physics.ResolveCameraCollision(p.Position + new Vector3(0, 1.85f, 0), pivot, pivot - camLookDir * 8.6f, 0.48f);
                float camFloor = Physics.GetSurfaceHeight(camPos.X, camPos.Z, camPos.Y) + 0.55f;
                if (camPos.Y < camFloor) camPos.Y = camFloor;

                Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                // Точка взгляда камеры строго по оси camPos + camLookDir: центр экрана (прицел) на 100% совпадает с лучом выстрела!
                Vector3 target = camPos + camLookDir * 25f;
                Engine.Camera.Target = new Vector3(-target.X, target.Y, target.Z);
                c.LastCameraWorldPos = camPos;
                c.LastCameraLookDir = camLookDir;
                c.HasCameraAimRay = true;
            }
        }

        private static void DrawPauseOverlay()
        {
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            Raylib.DrawRectangle(0, 0, sw, sh, Rgb(6, 8, 14, 165));

            int pw = Math.Clamp(sw - 60, 390, 468);
            int ph = Math.Clamp(sh - 36, 360, 444);
            int px = (sw - pw) / 2, py = (sh - ph) / 2;
            int btnX = px + 36, btnW = pw - 72;
            bool compact = ph < 405;
            int mainBtnH = compact ? 44 : 50;
            int gap = compact ? 10 : 14;

            Raylib.DrawRectangle(px, py, pw, ph, Rgb(14, 18, 30, 248));
            Raylib.DrawRectangle(px, py, pw, 4, Color.SkyBlue);
            Raylib.DrawRectangleLines(px, py, pw, ph, Rgb(70, 115, 175, 190));
            Engine.DrawTextCentered("ПАУЗА", sw * 0.5f, py + (compact ? 28f : 34f), compact ? 23f : 26f, Color.SkyBlue);

            int ry = py + (compact ? 56 : 66);
            if (Engine.DrawButton(new Rectangle(btnX, ry, btnW, mainBtnH), "ПРОДОЛЖИТЬ ИГРУ [ESC]", Rgb(38, 110, 70), Rgb(55, 155, 95), Color.White, 17.5f, Color.Lime))
            {
                _paused = false;
                ConsumeGameplayClickOnResume();
                RestoreInGameCursorIfNeeded();
                return;
            }
            ry += mainBtnH + gap;

            int volRowH = compact ? 32 : 36;
            Engine.DrawTextLeft($"Звук: {(SoundManager.Muted ? "ВЫКЛ" : $"{(int)MathF.Round(SoundManager.MasterVolume * 100f)}%")}", btnX + 2, ry + 8, 15.5f, Color.SkyBlue);
            int ctrlRight = btnX + btnW;
            if (Engine.DrawButton(new Rectangle(ctrlRight - 148, ry, 42, volRowH), "-", Rgb(42, 52, 74), Rgb(65, 80, 110), Color.White, 18f))
            {
                SoundManager.SetMasterVolume(SoundManager.MasterVolume - 0.10f);
                PersistRuntimeUserSettings();
            }
            if (Engine.DrawButton(new Rectangle(ctrlRight - 102, ry, 42, volRowH), "+", Rgb(42, 52, 74), Rgb(65, 80, 110), Color.White, 18f))
            {
                SoundManager.SetMasterVolume(SoundManager.MasterVolume + 0.10f);
                PersistRuntimeUserSettings();
            }
            if (Engine.DrawButton(new Rectangle(ctrlRight - 56, ry, 56, volRowH), SoundManager.Muted ? "ВКЛ" : "MUTE",
                SoundManager.Muted ? Rgb(115, 42, 48) : Rgb(42, 65, 92), Rgb(75, 100, 138), Color.White, 12f))
            {
                SoundManager.ToggleMute();
                PersistRuntimeUserSettings();
            }
            ry += volRowH + gap;

            bool isFs = Engine.IsFullscreenActive();
            int fsBtnH = compact ? 34 : 38;
            if (Engine.DrawButton(new Rectangle(btnX, ry, btnW, fsBtnH),
                $"ПОЛНЫЙ ЭКРАН [F11]: {(isFs ? "ВКЛ" : "ВЫКЛ")}",
                Rgb(38, 56, 86), Rgb(58, 84, 126), Color.White, 14f))
            {
                Data.UserSettings.Instance.ToggleFullscreenAndApply();
                PersistRuntimeUserSettings();
                return;
            }
            ry += fsBtnH + gap + 2;

            if (Engine.DrawButton(new Rectangle(btnX, ry, btnW, mainBtnH), "ВЫЙТИ В ЛОББИ ПЕРСОНАЖЕЙ", Rgb(55, 75, 115), Rgb(80, 105, 155), Color.White, 17f))
            {
                QuitRunToLobby(toMainMenu: false);
                return;
            }
            ry += mainBtnH + gap;

            if (Engine.DrawButton(new Rectangle(btnX, ry, btnW, mainBtnH), "ВЫЙТИ В ГЛАВНОЕ МЕНЮ", Rgb(135, 40, 48), Rgb(185, 60, 68), Color.White, 17f))
            {
                QuitRunToLobby(toMainMenu: true);
                return;
            }
        }

        private static void DrawInGameHUD()
        {
            if (_run == null) return;
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            var c = _run.Combat;

            if (_cutsceneTimer > 0f)
            {
                DrawCharacterUnlockCutsceneOverlay(sw, sh);
                return;
            }

            // Красная виньетка по краям экрана при получении урона (в стиле CS)
            if (_damageFlashTimer > 0f)
            {
                byte flashA = (byte)Math.Clamp((int)(_damageFlashTimer * 240f), 0, 165);
                Color flashCol = Rgb(235, 25, 35, flashA);
                Raylib.DrawRectangle(0, 0, sw, 18, flashCol);
                Raylib.DrawRectangle(0, sh - 18, sw, 18, flashCol);
                Raylib.DrawRectangle(0, 0, 18, sh, flashCol);
                Raylib.DrawRectangle(sw - 18, 0, 18, sh, flashCol);
            }

            var specAliveList = IsSpectating ? GetAliveSpectateTargets() : null;
            RemotePlayer? specTarget = null;
            string specTargetId = "";
            if (specAliveList != null && specAliveList.Count > 0)
            {
                var pair = specAliveList[_spectateTargetIdx % specAliveList.Count];
                specTargetId = pair.Id;
                specTarget = pair.Player;
            }

            // Лаконичный точный прицел в центре + Хитмаркер (CS-style) + Шкала заряда Тау-пушки Фримана
            int cx = sw / 2, cy = sh / 2;
            bool isMobaHud = SurvivorDatabase.IsMobaCharacter(_run.CharKey);
            bool tabOpenNow = !_paused && Raylib.IsKeyDown(KeyboardKey.Tab);
            Vector2 hudMousePos = Engine.GetUiMousePosition();
            bool cursorVisibleNow = !Raylib.IsCursorHidden() && !_paused;

            if (!IsSpectating && !isMobaHud && _run.CharKey != "mindustry" && !HasAnyInGameModalOpen() && !tabOpenNow)
            {
                Raylib.DrawCircleLines(cx, cy, 4.5f, Rgb(90, 230, 255, 200));
                Raylib.DrawLine(cx - 8, cy, cx - 5, cy, Rgb(90, 230, 255, 190));
                Raylib.DrawLine(cx + 5, cy, cx + 8, cy, Rgb(90, 230, 255, 190));
                Raylib.DrawLine(cx, cy - 8, cx, cy - 5, Rgb(90, 230, 255, 190));
                Raylib.DrawLine(cx, cy + 5, cx, cy + 8, Rgb(90, 230, 255, 190));
                Raylib.DrawRectangle(cx - 1, cy - 1, 2, 2, Color.White);
            }
            if (!IsSpectating && _hitMarkerTimer > 0f)
            {
                int hmx = (isMobaHud || _run.CharKey == "mindustry") ? (int)hudMousePos.X : cx;
                int hmy = (isMobaHud || _run.CharKey == "mindustry") ? (int)hudMousePos.Y : cy;
                Color hmCol = _hitMarkerCrit ? Color.Red : Color.White;
                int d1 = 5, d2 = _hitMarkerCrit ? 13 : 10;
                Raylib.DrawLine(hmx - d2, hmy - d2, hmx - d1, hmy - d1, hmCol);
                Raylib.DrawLine(hmx + d2, hmy - d2, hmx + d1, hmy - d1, hmCol);
                Raylib.DrawLine(hmx - d2, hmy + d2, hmx - d1, hmy + d1, hmCol);
                Raylib.DrawLine(hmx + d2, hmy + d2, hmx + d1, hmy + d1, hmCol);
            }
            if (!IsSpectating && _run.CharKey == "freeman" && c.TauCharging)
            {
                float tauPct = Math.Clamp(c.TauCharge / 2.5f, 0f, 1f);
                Color tauCol = tauPct >= 0.85f ? Color.Red : Color.Gold;
                Raylib.DrawRectangle(cx - 42, cy + 16, 84, 8, Rgb(18, 22, 32, 220));
                Raylib.DrawRectangle(cx - 40, cy + 18, (int)(80 * tauPct), 4, tauCol);
                Engine.DrawTextCentered($"ТАУ {(int)(tauPct * 100f)}%", cx, cy + 32, 12f, tauCol);
            }

            // 1. КОМПАКТНАЯ ИКОНОЧНАЯ ЛЕНТА ПРЕДМЕТОВ СЛЕВА СВЕРХУ (с всплывающим окном статов при наведении курсора)
            Data.ItemInfo? hoveredHudItem = null;
            int hoveredHudItemCount = 0;

            var specItems = (IsSpectating && specTarget != null)
                ? Data.ReplayRecorder.ParseCompactItems(specTarget.SyncedItemsCompact)
                : null;
            int hudItemCount = specItems != null ? specItems.Count : _run.ItemOrder.Count;
            if (hudItemCount == 0)
            {
                Raylib.DrawRectangle(16, 14, 370, 26, Rgb(12, 16, 26, 135));
                string emptyHint = IsSpectating
                    ? (specTarget != null ? $"У наблюдаемого игрока нет предметов  [TAB — Сводка]" : "Ожидание синхронизации союзников...")
                    : "Инвентарь пуст  [E — Сундуки | TAB — Иконки и статы]";
                Engine.DrawTextLeft(emptyHint, 24, 19, 13f, Rgb(155, 175, 200));
            }
            else
            {
                int maxStripW = Math.Max(220, sw - 304);
                int perRow = Math.Clamp((maxStripW - 12) / 46, 4, 16);
                int rows = (hudItemCount - 1) / perRow + 1;
                int stripW = Math.Min(hudItemCount, perRow) * 46 + 12;
                Raylib.DrawRectangle(16, 12, stripW, rows * 38 + 8, Rgb(12, 16, 26, 165));
                for (int i = 0; i < hudItemCount; i++)
                {
                    string k = specItems != null ? specItems[i].Key : _run.ItemOrder[i];
                    int cnt = specItems != null ? specItems[i].Count : _run.Items[k];
                    if (!GameData.ItemPool.TryGetValue(k, out var info)) continue;
                    int r = i / perRow, colIdx = i % perRow;
                    Rectangle cellRect = new(22 + colIdx * 46, 16 + r * 38, 42, 34);
                    bool hov = DrawItemIconCell(cellRect, k, info, cnt, cursorVisibleNow ? hudMousePos : new Vector2(-999f, -999f), compactHud: true);
                    if (hov && cursorVisibleNow)
                    {
                        hoveredHudItem = info;
                        hoveredHudItemCount = cnt;
                    }
                }
            }

            // 2. КОМПАКТНЫЙ ВИДЖЕТ ВРЕМЕНИ И СЛОЖНОСТИ СПРАВА СВЕРХУ (256x58)
            var (diffLabel, diffCol) = _run.GetDifficultyInfo();
            int mins = (int)(_run.ElapsedTime / 60f);
            int secs = (int)(_run.ElapsedTime % 60f);
            int rx = sw - 272, ry = 12;
            Raylib.DrawRectangle(rx, ry, 256, 58, Rgb(12, 16, 26, 185));
            Raylib.DrawRectangle(rx, ry, 256, 3, diffCol);
            Engine.DrawTextLeft($"{mins:00}:{secs:00}", rx + 12, ry + 8, 22f, Color.White);
            Engine.DrawTextLeft($"{diffLabel} (x{_run.DifficultyCoeff:F2})", rx + 82, ry + 11, 15f, diffCol);
            string detStr = _run.MoonDetonationTimer > 0f ? $" | ЭВАКУАЦИЯ: {_run.MoonDetonationTimer:F0}с!" : "";
            var activeDiffDef = GameData.GetActiveDifficulty();
            Engine.DrawTextLeft($"[{activeDiffDef.Name}] Этап {_run.Stage}: {_run.StageName}{detStr}", rx + 12, ry + 35, 13f, activeDiffDef.Color);
            var effHudArts = GameData.EffectiveArtifacts;
            if (effHudArts.Count > 0)
            {
                int ax = rx;
                int ay = ry + 64;
                foreach (var ak in GameData.OrderedArtifactKeys)
                {
                    if (!effHudArts.Contains(ak)) continue;
                    var aDef = GameData.RunArtifacts[ak];
                    Raylib.DrawRectangle(ax, ay, 46, 22, Rgb(14, 18, 30, 215));
                    Raylib.DrawRectangle(ax, ay, 46, 2, aDef.Color);
                    Engine.DrawTextCentered(aDef.Short, ax + 23, ay + 11, 11f, aDef.Color);
                    ax += 50;
                    if (ax > sw - 56) { ax = rx; ay += 26; }
                }
            }

            // 3. ТОНКАЯ ПОЛОСКА ЗДОРОВЬЯ БОССА ИЛИ ОРДЫ МНОГИХ ВВЕРХУ ПО ЦЕНТРУ (не мешает прицелу!)
            if (IsValid(_run.ActiveBoss) && !_run.ActiveBoss!.IsAlly)
            {
                var boss = _run.ActiveBoss!;
                string bTitle = boss.EnemyType == "mithrix"
                    ? $"МИТРИКС [ФАЗА {boss.Phase}/4] :: КОРОЛЬ НИЧЕГО"
                    : _run.BossTitle;
                Color bCol = boss.EnemyType == "mithrix"
                    ? (boss.Phase == 4 ? Color.Magenta : Color.SkyBlue)
                    : Color.Red;
                int bw = 500, bx = (sw - bw) / 2, by = 52;
                Engine.DrawTextCentered(bTitle, sw * 0.5f, by - 14f, 17f, bCol);
                float bRatio = Math.Clamp(boss.Hp / MathF.Max(1f, boss.MaxHp), 0f, 1f);
                Raylib.DrawRectangle(bx, by, bw, 14, Rgb(35, 12, 16, 210));
                Raylib.DrawRectangle(bx, by, (int)(bw * bRatio), 14, bCol);
                Engine.DrawTextCentered($"{(int)MathF.Max(0, boss.Hp)} / {(int)boss.MaxHp}", sw * 0.5f, by + 7f, 13f, Color.White);
            }
            else if (_run.IsHordeActive)
            {
                float curHordeHp = 0f;
                int aliveHorde = 0;
                foreach (var hb in _run.ActiveHordeBosses)
                {
                    if (IsValid(hb) && hb.Hp > 0f && !hb.IsAlly)
                    {
                        curHordeHp += hb.Hp;
                        aliveHorde++;
                    }
                }
                Color hCol = Rgb(255, 145, 45);
                int bw = 520, bx = (sw - bw) / 2, by = 52;
                Engine.DrawTextCentered($"{_run.HordeTitle} [{aliveHorde}/{_run.ActiveHordeBosses.Count}]", sw * 0.5f, by - 14f, 16.5f, hCol);
                float hRatio = Math.Clamp(curHordeHp / MathF.Max(1f, _run.HordeTotalMaxHp), 0f, 1f);
                Raylib.DrawRectangle(bx, by, bw, 14, Rgb(38, 20, 10, 215));
                Raylib.DrawRectangle(bx, by, (int)(bw * hRatio), 14, hCol);
                Engine.DrawTextCentered($"{(int)MathF.Max(0, curHordeHp)} / {(int)MathF.Max(1f, _run.HordeTotalMaxHp)}", sw * 0.5f, by + 7f, 13f, Color.White);
            }

            if (!_run.IsSafeTravels)
                World.CharacterUnlockSystems.DrawUnlockHudOverlays(_run, sw, sh);

            // 4. КОМПАКТНЫЙ БЛОК ЗДОРОВЬЯ И ВАЛЮТ СЛЕВА ВНИЗУ (300x60) + БЕЙДЖ КОЛЕСА ЭМОЦИЙ [L]
            int hx = 20, hy = sh - 78;
            if (!IsSpectating)
            {
                bool emoteActive = !string.IsNullOrEmpty(_run.Parts.ActiveEmote);
                string emoteBadge = emoteActive ? "[L] Эмоции  |  [Space/WASD] Стоп" : "[L] Колесо эмоций";
                int ebW = emoteActive ? 248 : 154;
                Raylib.DrawRectangle(hx, hy - 24, ebW, 21, Rgb(12, 16, 26, 185));
                Engine.DrawTextLeft(emoteBadge, hx + 8, hy - 20, 12f, emoteActive ? Color.Lime : Color.Gold);
            }

            Raylib.DrawRectangle(hx, hy, 300, 58, Rgb(12, 16, 26, 190));
            if (IsSpectating && specTarget != null)
            {
                var specCfg = SurvivorDatabase.Classes.GetValueOrDefault(specTarget.CharKey) ?? SurvivorDatabase.Classes["commando"];
                string specHeroName = specCfg.Name;
                if (specCfg.Skins.Count > 0)
                {
                    var specSk = specCfg.Skins[Math.Clamp(specTarget.SkinIdx, 0, specCfg.Skins.Count - 1)];
                    if (specSk.CustomSkillVariants != null) specHeroName = specSk.Name;
                }
                Engine.DrawTextLeft($"${specTarget.Gold}", hx + 12, hy + 7, 18f, Color.Gold);
                Engine.DrawTextLeft($"{specTarget.LunarCoins}М", hx + 95, hy + 7, 17f, Color.SkyBlue);
                Engine.DrawTextLeft(specHeroName, hx + 155, hy + 8, 15f, Rgb(190, 210, 235));

                float sMaxHp = MathF.Max(1f, specTarget.MaxHp);
                float hpRatio = Math.Clamp(specTarget.Hp / sMaxHp, 0f, 1f);
                float shRatio = Math.Clamp(specTarget.Shield / sMaxHp, 0f, 1f);
                Raylib.DrawRectangle(hx + 10, hy + 30, 280, 20, Rgb(42, 18, 22, 230));
                Raylib.DrawRectangle(hx + 10, hy + 30, (int)(280 * hpRatio), 20, hpRatio > 0.3f ? Rgb(80, 215, 95) : Rgb(240, 65, 60));
                if (shRatio > 0.01f)
                    Raylib.DrawRectangle(hx + 10, hy + 30, (int)(280 * shRatio), 6, Rgb(85, 210, 255));

                string hpTxt = specTarget.Shield >= 1f
                    ? $"{(int)specTarget.Hp} (+{(int)specTarget.Shield}) / {(int)sMaxHp}"
                    : $"{(int)specTarget.Hp} / {(int)sMaxHp}";
                Engine.DrawTextCentered(hpTxt, hx + 150, hy + 40, 15f, Color.White);
            }
            else
            {
                Engine.DrawTextLeft($"${_run.Gold}", hx + 12, hy + 7, 18f, Color.Gold);
                Engine.DrawTextLeft($"{_run.LunarCoins}М", hx + 95, hy + 7, 17f, Color.SkyBlue);
                var activeSurvCfg = SurvivorDatabase.Classes[_run.CharKey];
                var activeSurvSkin = activeSurvCfg.GetActiveSkin();
                string hudHeroName = activeSurvSkin.CustomSkillVariants != null ? activeSurvSkin.Name : activeSurvCfg.Name;
                Engine.DrawTextLeft(hudHeroName, hx + 155, hy + 8, 15f, Rgb(190, 210, 235));

                float hpRatio = Math.Clamp(_run.PlayerHp / _run.PlayerMaxHp, 0f, 1f);
                float shRatio = Math.Clamp(c.Shield / _run.PlayerMaxHp, 0f, 1f);
                Raylib.DrawRectangle(hx + 10, hy + 30, 280, 20, Rgb(42, 18, 22, 230));
                Raylib.DrawRectangle(hx + 10, hy + 30, (int)(280 * hpRatio), 20, hpRatio > 0.3f ? Rgb(80, 215, 95) : Rgb(240, 65, 60));
                if (shRatio > 0.01f)
                    Raylib.DrawRectangle(hx + 10, hy + 30, (int)(280 * shRatio), 6, Rgb(85, 210, 255));

                string hpTxt = c.Shield >= 1f
                    ? $"{(int)_run.PlayerHp} (+{(int)c.Shield}) / {(int)_run.PlayerMaxHp}"
                    : $"{(int)_run.PlayerHp} / {(int)_run.PlayerMaxHp}";
                Engine.DrawTextCentered(hpTxt, hx + 150, hy + 40, 15f, Color.White);
            }

            // 5. АККУРАТНЫЙ ДОК СПОСОБНОСТЕЙ СПРАВА ВНИЗУ (4 иконки 56x56)
            string hudDockCharKey = (IsSpectating && specTarget != null) ? specTarget.CharKey : _run.CharKey;
            var hudDockCfg = SurvivorDatabase.Classes.GetValueOrDefault(hudDockCharKey) ?? SurvivorDatabase.Classes["commando"];
            if (!IsSpectating && isMobaHud)
            {
                Vector2 mp = Engine.GetUiMousePosition();
                Raylib.DrawCircleLines((int)mp.X, (int)mp.Y, 10f, Color.Lime);
                Engine.DrawTextLeft("MOBA-РЕЖИМ: ПКМ — Идти/Атака | ЛКМ/Q — Удар | W/СКМ — Навык 2 | Shift — Рывок | R — Ульта | Space — Прыжок",
                    sw - 760, sh - 98, 13f, Rgb(120, 245, 200));
            }
            bool watchedIsMoba = SurvivorDatabase.IsMobaCharacter(hudDockCharKey);
            float cdM1 = (IsSpectating && specTarget != null) ? specTarget.SkillCds[0] : c.M1Cd;
            float cdM2 = (IsSpectating && specTarget != null) ? specTarget.SkillCds[1] : c.M2Cd;
            float cdSh = (IsSpectating && specTarget != null) ? specTarget.SkillCds[2] : c.ShiftCd;
            float cdR  = (IsSpectating && specTarget != null) ? specTarget.SkillCds[3] : c.RCd;
            (string key, string bind, float cd, float maxCd, Color col)[] skBoxes =
            {
                ("m1", watchedIsMoba ? "ЛКМ/Q" : "ЛКМ", cdM1, MathF.Max(0.1f, hudDockCfg.Skills["m1"].Cd), Color.Yellow),
                ("m2", watchedIsMoba ? "W/СКМ" : "ПКМ", cdM2, MathF.Max(0.1f, hudDockCfg.Skills["m2"].Cd), Color.Orange),
                ("shift", "Shift", cdSh, MathF.Max(0.1f, hudDockCfg.Skills["shift"].Cd), Color.SkyBlue),
                ("r", "R", cdR, MathF.Max(0.1f, hudDockCfg.Skills["r"].Cd), Color.Magenta)
            };
            for (int i = 0; i < skBoxes.Length; i++)
            {
                var (_, bind, cd, maxCd, col) = skBoxes[i];
                int sx = sw - 268 + i * 62;
                int sy = sh - 76;
                Raylib.DrawRectangle(sx, sy, 56, 56, cd <= 0f ? Rgb(26, 38, 56, 215) : Rgb(16, 18, 26, 215));
                if (cd > 0f)
                {
                    int fillH = (int)(56 * Math.Clamp(cd / maxCd, 0f, 1f));
                    Raylib.DrawRectangle(sx, sy + (56 - fillH), 56, fillH, Rgb(220, 45, 55, 95));
                }
                Raylib.DrawRectangle(sx, sy, 56, 3, cd <= 0f ? col : Color.Gray);
                Engine.DrawTextCentered(bind, sx + 28, sy + 16, 13f, col);
                Engine.DrawTextCentered(cd <= 0f ? "ГОТ" : $"{cd:F1}", sx + 28, sy + 38, 15f, cd <= 0f ? Color.White : Color.Red);
            }

            if (IsSpectating)
            {
                int sbW = 760, sbX = (sw - sbW) / 2, sbY = 82;
                Color specAccent = _isDedicatedSpectator ? Color.SkyBlue : Color.Orange;
                Raylib.DrawRectangle(sbX, sbY, sbW, 56, Rgb(10, 14, 26, 226));
                Raylib.DrawRectangle(sbX, sbY, sbW, 3, specAccent);

                string watchedLine;
                if (specTarget != null)
                {
                    string pDisp = string.IsNullOrWhiteSpace(specTarget.PlayerName)
                        ? specTargetId.ToUpper()
                        : $"{specTarget.PlayerName} [{specTargetId.ToUpper()}]";
                    string cDisp = (SurvivorDatabase.Classes.GetValueOrDefault(specTarget.CharKey) ?? SurvivorDatabase.Classes["commando"]).Name;
                    string idxBadge = specAliveList != null && specAliveList.Count > 1
                        ? $" ({_spectateTargetIdx % specAliveList.Count + 1}/{specAliveList.Count})"
                        : "";
                    string modeBadge = _isDedicatedSpectator ? "РЕЖИМ ЗРИТЕЛЯ" : "ВОЗРОЖДЕНИЕ НА СЛЕД. ЭТАПЕ";
                    watchedLine = $"НАБЛЮДЕНИЕ ЗА: {pDisp} — {cDisp}{idxBadge}  |  {modeBadge}";
                }
                else
                {
                    watchedLine = "РЕЖИМ НАБЛЮДАТЕЛЯ :: ОЖИДАНИЕ ДАННЫХ ОТ ИГРОКОВ НА ЭТАПЕ...";
                }
                Engine.DrawTextCentered(watchedLine, sw * 0.5f, sbY + 18f, 15.5f, specAccent);

                string camHint = _spectateFreeCam
                    ? "КАМЕРА: СВОБОДНЫЙ ПОЛЁТ [C]  |  WASD + Space/Ctrl + Shift — Полёт  |  [<- / ->] Игрок"
                    : "КАМЕРА: ОРБИТА [C]  |  [A/D, ЛКМ/ПКМ] Сменить игрока  |  [Колесо] Зум  |  [TAB] Предметы";
                Engine.DrawTextCentered(camHint, sw * 0.5f, sbY + 40f, 12.5f, Rgb(185, 215, 245));

                if (!_isDedicatedSpectator)
                {
                    int killerY = GameData.ActiveArtifacts.Count > 0 ? 124 : 78;
                    DrawKillerMobTopRightWidget(sw - 446, killerY, 430, allow3DToggle: false);
                }
            }
            else if (_run.CharKey == "mindustry")
            {
                Vector2 mp = Engine.GetUiMousePosition();
                Raylib.DrawCircleLines((int)mp.X, (int)mp.Y, c.MindustryBuildMode && c.MindustrySelectedBuild >= 0 ? 14f : 8f,
                    c.MindustryBuildMode ? Color.SkyBlue : Color.Gold);

                // Панель 3 ресурсов, здоровья Главного Ядра и текущего режима
                int pnlX = sw - 520, pnlY = sh - 242;
                Raylib.DrawRectangle(pnlX, pnlY, 504, 34, Rgb(12, 16, 26, 225));
                Raylib.DrawRectangle(pnlX, pnlY, 504, 2, c.MindustryBuildMode ? Color.SkyBlue : Color.Gold);
                string coreHpStr = IsValid(c.MindustryMainCore)
                    ? $" | ЯДРО: {(int)c.MindustryMainCore!.Hp} HP"
                    : $" | ПОСТРОЙТЕ ЯДРО [5]: {c.MindustryCoreRebuildTimer:F0}с!";
                Color resCol = (!IsValid(c.MindustryMainCore) && c.MindustryCoreRebuildTimer > 0f) ? Color.Red : Color.White;
                Engine.DrawTextLeft($"МЕДЬ: {(int)c.MindustryCopper} | КРЕМНИЙ: {(int)c.MindustrySilicon} | ТОРИЙ: {(int)c.MindustryThorium}{coreHpStr}",
                    pnlX + 10, pnlY + 8, 14f, resCol);

                string modeBtnTxt = c.MindustryBuildMode
                    ? "РЕЖИМ: СТРОИТЕЛЬСТВО (НЕУЯЗВИМ) -> В БОЙ [B/Shift]"
                    : "РЕЖИМ: БОЕВОЙ (УЯЗВИМ, СТРЕЛЬБА И [E]) -> СТРОИТЬ [B/Shift]";
                if (!_paused && Engine.DrawButton(new Rectangle(pnlX, pnlY - 38, 504, 34), modeBtnTxt,
                    c.MindustryBuildMode ? Rgb(32, 85, 135) : Rgb(125, 75, 25), Rgb(65, 130, 195), Color.White, 14f,
                    c.MindustryBuildMode ? Color.SkyBlue : Color.Gold))
                {
                    c.ToggleMindustryMode();
                }

                if (c.MindustryBuildMode)
                {
                    Raylib.DrawRectangle(pnlX, pnlY + 38, 504, 122, Rgb(10, 14, 24, 235));
                    for (int bi = 0; bi < SurvivorCombatController.MindustryBlueprints.Length; bi++)
                    {
                        var bp = SurvivorCombatController.MindustryBlueprints[bi];
                        int r = bi / 5, col = bi % 5;
                        Rectangle bRect = new(pnlX + 5 + col * 99, pnlY + 44 + r * 56, 95, 50);
                        bool sel = c.MindustrySelectedBuild == bi;
                        bool canAfford = c.MindustryCopper >= bp.Copper && c.MindustrySilicon >= bp.Silicon && c.MindustryThorium >= bp.Thorium;
                        string costStr = $"{(int)bp.Copper}М" + (bp.Silicon > 0 ? $" {(int)bp.Silicon}Кр" : "") + (bp.Thorium > 0 ? $" {(int)bp.Thorium}Т" : "");
                        if (!_paused && Engine.DrawButton(bRect, $"{bp.ShortName}\n{costStr}",
                            sel ? Rgb(45, 95, 155) : Rgb(22, 28, 42), Rgb(60, 115, 180),
                            canAfford ? (sel ? Color.Yellow : Color.White) : Color.Gray, 11.5f, sel ? Color.Yellow : null))
                        {
                            c.MindustrySelectedBuild = sel ? -1 : bi;
                        }
                    }
                }
                else
                {
                    Engine.DrawTextLeft("БОЕВОЙ РЕЖИМ: ЛКМ — Огонь | [E] — Сундуки и Портал | [B / Shift] — Режим Строительства",
                        pnlX, pnlY + 42, 13f, Color.Gold);
                }
            }
            else if (_run.CharKey == "reimu")
            {
                string grazeStr = $"ГРЕЙЗ ДАНМАКУ: x{c.ReimuGrazeStacks} (+{c.ReimuGrazeStacks * 3}% Урон)";
                Engine.DrawTextLeft($"{grazeStr}  |  ПОЛЁТ [Space]: {c.ReimuFlightFuel:F1}с {(c.ReimuFlightBroken ? "[СБИТ!]" : "")}",
                    sw - 585, sh - 98, 13.5f, c.ReimuFlightBroken ? Color.Red : (c.ReimuGrazeStacks > 0 ? Color.Gold : Color.SkyBlue));
            }
            else if (_run.CharKey == "freeman")
            {
                string gTxt = IsValid(c.FreemanGrabbedEnemy)
                    ? "★ МОНСТР ЗАХВАЧЕН [ЛКМ/ПКМ — ШВЫРНУТЬ!]"
                    : (c.IsFreemanBlueGravityGunActive()
                        ? $"★ СИНЯЯ ГРАВИ-ПУШКА ({c.FreemanBlueGravTimer:F1}с) [ПКМ — СХВАТИТЬ ВРАГА]"
                        : (c.HasGrabbedObject ? "ОБЪЕКТ ЗАХВАЧЕН [ПКМ — БРОСИТЬ]" : "ГРАВИ-ПУШКА ГОТОВА [ПКМ]"));
                string tTxt = c.TauCharging ? $"ТАУ: {(int)(c.TauCharge / 2.5f * 100f)}%" : (c.FreemanMorphineCd > 0f ? $"МОРФИЙ КД: {c.FreemanMorphineCd:F0}с" : "МОРФИЙ HEV: ГОТОВ");
                Engine.DrawTextLeft($"{gTxt}  |  {tTxt}", sw - 600, sh - 98, 13.5f, c.IsFreemanBlueGravityGunActive() ? Color.SkyBlue : Color.Orange);
            }
            else if (_run.CharKey == "medic")
            {
                int mx = sw - 540, my = sh - 118;
                float uPct = Math.Clamp(c.MedicUberCharge / 100f, 0f, 1f);
                Raylib.DrawRectangle(mx, my, 522, 36, Rgb(12, 20, 26, 228));
                Raylib.DrawRectangle(mx, my, (int)(522 * uPct), 4, uPct >= 1f ? Color.Gold : Color.Lime);
                string uberStr = c.UberTimer > 0f
                    ? $"★ УБЕР-ЗАРЯД АКТИВЕН ({c.UberTimer:F1}с)!"
                    : $"УБЕР-ЗАРЯД: {(int)c.MedicUberCharge}%  |  [Зажать ПКМ] Лечебный Луч  |  [G] «Oktoberfest!»";
                Engine.DrawTextLeft(uberStr, mx + 10, my + 11, 13f, uPct >= 1f || c.UberTimer > 0f ? Color.Gold : Color.Lime);
            }
            else if (_run.CharKey == "cheburashka")
            {
                string standTxt = c.GenaStandTimer > 0f
                    ? $"СТЕНД 『КРОКОДИЛ ГЕНА』 АКТИВЕН: {c.GenaStandTimer:F1}с"
                    : "ПАССИВКА: Ударная волна при падении + Гармошка Гены в полёте";
                Engine.DrawTextLeft(standTxt, sw - 520, sh - 98, 13.5f, c.GenaStandTimer > 0f ? Color.Gold : Color.Orange);
            }
            else if (_run.CharKey == "ahri")
            {
                string ahriTxt = $"КРАЖА СУЩНОСТИ: {c.AhriEssenceStacks}/9  |  ПРИЗРАЧНЫЙ РЫВОК [Shift]: {c.AhriDashCharges}/3";
                Engine.DrawTextLeft(ahriTxt, sw - 540, sh - 116, 14f, Rgb(105, 235, 255));
            }
            else if (_run.CharKey == "steve")
            {
                int sx = sw - 560, sy = sh - 134;
                Raylib.DrawRectangle(sx, sy, 544, 52, Rgb(12, 18, 28, 225));
                Raylib.DrawRectangle(sx, sy, 544, 3, Color.SkyBlue);
                string gearStr = $"Меч: {(c.SteveSwordTier >= 2 ? "Незерит" : (c.SteveSwordTier == 1 ? "Алмаз" : "Баз."))} | Броня: {(c.SteveArmorTier >= 3 ? "Незерит" : (c.SteveArmorTier >= 1 ? "Железо" : "Нет"))}{(c.SteveHasTotem ? " | ★ТОТЕМ" : "")}{(IsValid(c.SteveActiveGolem) ? " | ★ГОЛЕМ" : "")}";
                Engine.DrawTextLeft($"ДЕРЕВО: {c.SteveWood}  |  КАМЕНЬ: {c.SteveStone}  |  ЖЕЛЕЗО: {c.SteveIron}  |  АЛМАЗЫ: {c.SteveDiamond}   ({gearStr})",
                    sx + 10, sy + 7, 13f, Color.SkyBlue);
                Engine.DrawTextLeft("ЛКМ вниз / ПКМ — ломать блоки  |  [R] Маяк / Железный Голем и Верстак",
                    sx + 10, sy + 29, 12f, Color.Gold);
            }
            else if (_run.CharKey == "gold_ship")
            {
                int gx = sw - 520, gy = sh - 118;
                float momPct = Math.Clamp(c.GoldShipMomentum, 0f, 1f);
                Raylib.DrawRectangle(gx, gy, 504, 36, Rgb(14, 18, 30, 225));
                Raylib.DrawRectangle(gx, gy, (int)(504 * momPct), 4, Color.Gold);
                string gsTxt = $"РАЗБЕГ ГОЛД ШИП: {(int)(momPct * 100f)}%  |  МНОЖИТЕЛЬ УРОНА ОТ СКОРОСТИ: x{c.GoldShipSpeedScale:F2}{(c.GoldShipSpurtTimer > 0f ? $"  |  ГОЛШИ-ВАРП: {c.GoldShipSpurtTimer:F1}с" : "")}";
                Engine.DrawTextLeft(gsTxt, gx + 10, gy + 11, 13f, momPct > 0.7f ? Color.Gold : Color.White);
            }
            else if (_run.CharKey == "jotaro")
            {
                string jTxt = c.ZaWarudoTimer > 0f
                    ? $"★ STAR PLATINUM: THE WORLD :: УРОН НАКАПЛИВАЕТСЯ ({c.ZaWarudoTimer:F1}с) ★"
                    : "СТЕНД 『STAR PLATINUM』 (18% Авто-Блок) :: [ЛКМ] ORA ORA | [R] Остановка Времени";
                Engine.DrawTextLeft(jTxt, sw - 590, sh - 104, 13.5f, c.ZaWarudoTimer > 0f ? Color.Gold : Rgb(195, 130, 255));
            }
            else if (_run.CharKey == "sol")
            {
                int solSkin = Data.ProgressData.Instance.GetSelectedSkin("sol");
                if (solSkin == 2 && c.KyStunEdgeCharging)
                {
                    int kx = sw - 560, ky = sh - 122;
                    float chPct = Math.Clamp(c.KyStunEdgeCharge / SurvivorCombatController.KyStunEdgeMaxCharge, 0f, 1f);
                    Raylib.DrawRectangle(kx, ky, 542, 38, Rgb(12, 18, 32, 236));
                    Color barCol = chPct >= 0.75f ? Rgb(255, 75, 95) : (chPct >= 0.35f ? Color.Gold : Color.SkyBlue);
                    Raylib.DrawRectangle(kx, ky, (int)(542 * chPct), 5, barCol);
                    string chTitle = chPct >= 0.75f ? "★ CHARGED STUN EDGE ГОТОВ!" : "ЗАРЯД STUN EDGE (УДЕРЖИВАЙТЕ ПКМ)";
                    Engine.DrawTextLeft($"{chTitle} :: {(int)(chPct * 100f)}%  |  TENSION: {(int)c.SolTension}%", kx + 10, ky + 12, 13f, barCol);
                }
                else
                {
                    int sx = sw - 560, sy = sh - 120;
                    float tPct = Math.Clamp(c.SolTension / 100f, 0f, 1f);
                    Raylib.DrawRectangle(sx, sy, 542, 36, Rgb(18, 14, 24, 232));
                    Raylib.DrawRectangle(sx, sy, (int)(542 * tPct), 4, c.SolTension >= 50f ? Rgb(255, 75, 165) : Color.Orange);
                    string rcTag = c.SolRomanCancelBuff > 0f
                        ? $"★ ROMAN CANCEL АКТИВЕН ({c.SolRomanCancelBuff:F1}с)!"
                        : (c.SolTension >= 50f ? "[Q] ROMAN CANCEL ГОТОВ (50%)" : "[Q] Roman Cancel (нужно 50%)");
                    string heroTag = solSkin == 2 ? "КАЙ КИСКЕ" : "СОЛ БЭДГАЙ";
                    Engine.DrawTextLeft($"{heroTag} :: TENSION {(int)c.SolTension}%  |  {rcTag}", sx + 10, sy + 11, 13f, c.SolTension >= 50f ? Rgb(255, 140, 210) : Color.Orange);
                }
            }
            else if (_run.CharKey == "jack_o")
            {
                int jx = sw - 575, jy = sh - 122;
                int aliveSv = 0;
                foreach (var sv in c.JackOServants) if (IsValid(sv)) aliveSv++;
                JackOCommandFlag? activeFl = null;
                foreach (var fl in c.JackOFlags) if (IsValid(fl)) { activeFl = fl; break; }

                Raylib.DrawRectangle(jx, jy, 558, 38, Rgb(22, 14, 24, 232));
                Color flCol = activeFl != null
                    ? (activeFl.BuffMode == "attack" ? Rgb(255, 95, 65) : (activeFl.BuffMode == "defense" ? Color.SkyBlue : Color.Gold))
                    : Rgb(155, 255, 75);
                float barRatio = activeFl != null ? Math.Clamp(activeFl.Lifetime / 12.0f, 0f, 1f) : Math.Clamp(aliveSv / 4.0f, 0f, 1f);
                Raylib.DrawRectangle(jx, jy, (int)(558 * barRatio), 4, flCol);

                string flagStatus = activeFl != null
                    ? $"★ ФЛАГ ({(activeFl.BuffMode == "attack" ? "+65% УРОН" : (activeFl.BuffMode == "defense" ? "+65% ЗАЩИТА" : "УРОН+ЗАЩИТА"))}): {MathF.Max(0f, activeFl.Lifetime):F1}с"
                    : $"[Shift] Флаг ({(c.JackONextFlagDefense ? "Защита" : "Урон")})";
                Engine.DrawTextLeft($"ДЖЕК-О :: СЛУГИ: {aliveSv}/4 [F — Бросок]  |  {flagStatus}", jx + 10, jy + 12, 12.5f, flCol);
            }
            else if (_run.CharKey == "dmc_trio")
            {
                int skin = Data.ProgressData.Instance.GetSelectedSkin("dmc_trio");
                string formName;
                if (skin == 1)
                {
                    formName = $"НЕРО :: Exceed {c.NeroExceedCharges}/3 {(c.NeroMaxActWindow > 0f ? "★[Q — MAX-ACT СЕЙЧАС!]★" : "[Q после удара — MAX-Act]")}";
                }
                else if (skin == 2)
                {
                    formName = $"ВЕРГИЛИЙ :: Концентрация {(int)c.VergilConcentration}% (+{(int)(c.VergilConcentration * 0.35f)}% Урон)";
                }
                else
                {
                    string stName = c.DanteStyle switch { 0 => "TRICKSTER", 1 => "SWORDMASTER", 2 => "GUNSLINGER", _ => $"ROYAL GUARD ({(int)c.DanteRoyalGauge}%)" };
                    formName = $"ДАНТЕ [{stName}] :: Стили [1-4]";
                }
                string dtStr = c.DmcDevilTriggerTimer > 0f ? $"  |  ★ DEVIL TRIGGER: {c.DmcDevilTriggerTimer:F1}с" : "";
                Engine.DrawTextLeft($"{formName}{dtStr}", sw - 575, sh - 104, 13.5f, c.DmcDevilTriggerTimer > 0f ? Color.Magenta : Color.Gold);
            }
            else if (_run.CharKey == "sanabi")
            {
                int sx = sw - 535, sy = sh - 122;
                float momPct = Math.Clamp(c.SanabiMomentum, 0f, 1f);
                Raylib.DrawRectangle(sx, sy, 518, 38, Rgb(14, 18, 30, 232));
                Raylib.DrawRectangle(sx, sy, (int)(518 * momPct), 4, Rgb(255, 195, 55));
                string ovStr = c.SanabiOverdriveTimer > 0f ? $"  |  ★ ПРОТОКОЛ SANABI (МАРИ): {c.SanabiOverdriveTimer:F1}с" : "  |  [ПКМ/Shift] Цепной Захват";
                string snTxt = $"ИМПУЛЬС ЦЕПИ: {(int)(momPct * 100f)}%  |  УРОН ОТ РАЗГОНА: x{c.SanabiSpeedScale:F2}{ovStr}";
                Engine.DrawTextLeft(snTxt, sx + 10, sy + 12, 13f, c.SanabiOverdriveTimer > 0f ? Rgb(80, 235, 255) : Color.Gold);
            }
            else if (_run.CharKey is "lappland" or "w_arknights" or "pushnoy" or "freddy" or "vyper" or "haze")
            {
                string specTxt = _run.CharKey switch
                {
                    "lappland" => Data.ProgressData.Instance.GetSelectedSkin("lappland") == 2
                        ? $"ЛАППЛАНД ДЕКАДЕНЦИЯ :: Духи-Волки: {c.DecadenzaFamiliars.Count}/2  |  Удары накладывают Безмолвие (4с)"
                        : (c.LapplandFormTimer > 0f ? $"★ ЗАТМЕНИЕ БЕЗУМИЯ АКТИВНО: {c.LapplandFormTimer:F1}с" : "ПАССИВКА: Удары накладывают Безмолвие (4с) и раскол брони"),
                    "w_arknights" => Data.ProgressData.Instance.GetSelectedSkin("w_arknights") == 2
                        ? $"ВИШ'АДЕЛЬ :: Заряды Предков: {c.WisadelAncestralCharges}  |  Тени Ревенанта: {c.WisadelShadows.Count}/3"
                        : $"НАЁМНИЦА W :: Установлено мин C4: {c.WMines.Count}/6  |  [Shift] Прыжок + Детонатор",
                    "pushnoy" => c.PushnoyRockBuffTimer > 0f ? $"★ МЕГА-РОК СОЛО ГАЛИЛЕО: {c.PushnoyRockBuffTimer:F1}с" : "ГАЛИЛЕО: Рифф + Опыты + Неньютоновская броня (-65% от тяж. ударов)",
                    "freddy" => c.FreddyPowerOutageTimer > 0f
                        ? $"★ 0% ЭНЕРГИИ — МАРШ ТОРЕАДОРА ({c.FreddyPowerOutageTimer:F1}с :: +35% УРОН И СКОРОСТЬ)!"
                        : $"БАТАРЕЯ FAZBEAR: {(int)c.FreddyBattery}%{(c.FreddyCamStealthTimer > 0f ? $"  |  ПОМЕХИ КАМЕР (СКРЫТНОСТЬ): {c.FreddyCamStealthTimer:F1}с" : "  |  Навыки тратят заряд до Тьмы 0%")}",
                    "vyper" => c.VyperSlideTimer > 0f ? $"★ СИЛОВОЙ ПОДКЛАД ({c.VyperSlideTimer:F1}с): +35% ОГОНЬ, 25% УКЛОНЕНИЕ И ЯД!" : "ПАССИВКА: Скольжение оставляет Ядовитый След и даёт 25% уклонение",
                    _ => c.HazeDanceTimer > 0f ? $"★ ТАНЕЦ ПУЛЬ (BULLET DANCE): {c.HazeDanceTimer:F1}с" : (c.HazeSmokeTimer > 0f ? $"СКРЫТНОСТЬ ({c.HazeSmokeTimer:F1}с): ВРАГИ ПОТЕРЯЛИ ЦЕЛЬ (+16 ФИКСАЦИИ)" : "ПАССИВКА: Фиксация (до 20 стаков на цели -> взрыв сновидений)")
                };
                Engine.DrawTextLeft(specTxt, sw - 590, sh - 104, 13f, Color.Gold);
            }
            else if (_run.CharKey == "commando")
            {
                Engine.DrawTextLeft($"ТАКТИЧЕСКАЯ ОБОЙМА: {c.CommandoHeavyRounds}/4 крупнокалиберных патронов (+35% Урон и Пробитие)  |  [Shift] Перекат + Перезарядка",
                    sw - 640, sh - 98, 12.5f, c.CommandoHeavyRounds > 0 ? Color.Gold : Color.Gray);
            }
            else if (_run.CharKey == "buzz")
            {
                bool isZurg = Data.ProgressData.Instance.GetSelectedSkin("buzz") == 2;
                if (isZurg)
                {
                    int zx = sw - 545, zy = sh - 120;
                    float hPct = Math.Clamp(c.ZurgIonHeat, 0f, 1f);
                    Raylib.DrawRectangle(zx, zy, 528, 36, Rgb(22, 12, 32, 232));
                    Raylib.DrawRectangle(zx, zy, (int)(528 * hPct), 4, Rgb(215, 75, 255));
                    Engine.DrawTextLeft($"ИМПЕРАТОР ЗУРГ :: Ионный Перегрев {(int)(hPct * 100f)}% (+{(int)(hPct * 30f)}% Урон)  |  Шершни: {c.ZurgHornetBots.Count}/3",
                        zx + 10, zy + 11, 13f, Rgb(235, 135, 255));
                }
                else
                {
                    Engine.DrawTextLeft("БАЗЗ ЛАЙТЕР :: [Space в воздухе] Крылья-Планер  |  [Ctrl в полёте] Метеор Звёздного Командования",
                        sw - 610, sh - 98, 13f, Color.Lime);
                }
            }
            else if (_run.CharKey == "scout")
            {
                string flStr = c.ScoutFlareCd <= 0f ? "ГОТОВА [Нажмите F]" : $"Перезарядка {c.ScoutFlareCd:F1}с";
                Engine.DrawTextLeft($"СКАУТ DRG :: Сигнальная Ракетница (+25% Крит по целям): {flStr}",
                    sw - 545, sh - 98, 13.5f, c.ScoutFlareCd <= 0f ? Color.Yellow : Color.SkyBlue);
            }
            else if (_run.CharKey == "huntress")
            {
                string balStr = c.HuntressBallistaShots > 0
                    ? $"  |  ★ БАЛЛИСТА [ЛКМ]: {c.HuntressBallistaShots}/3 ВЫСТРЕЛА ({c.HuntressBallistaTimer:F1}с)"
                    : "  |  Стрельба на бегу заряжает Импульс";
                Engine.DrawTextLeft($"ОХОТНИЦА :: Импульс: {(int)(c.HuntressMomentumCharge * 100f)}%{balStr}",
                    sw - 565, sh - 98, 13.5f, c.HuntressBallistaShots > 0 ? Color.SkyBlue : Color.Gold);
            }
            else if (_run.CharKey == "aigis")
            {
                string st = c.OrgiaTimer > 0f
                    ? $"★ РЕЖИМ ОРГИЯ: {c.OrgiaTimer:F1}с"
                    : (c.AigisOverheatTimer > 0f ? $"ОХЛАЖДЕНИЕ ЯДРА (-50% Урона): {c.AigisOverheatTimer:F1}с" : "Персона Афина [R] и Щит Тетракарн [ПКМ]");
                Engine.DrawTextLeft($"ЭГИДА :: Тетракарн: {c.AigisTetrakarnCharges} зар.  |  {st}",
                    sw - 560, sh - 98, 13.5f, Color.Gold);
            }
            else if (_run.CharKey == "tidehunter")
            {
                int thresh = Math.Max(1, (int)(_run.PlayerMaxHp * 0.28f));
                Engine.DrawTextLeft($"ТАЙДХАНТЕР :: Панцирь Кракена {(int)c.TideKrakenDamageAccum}/{thresh} ед. до авто-удара Якорем и очищения",
                    sw - 585, sh - 98, 13f, Rgb(75, 245, 240));
            }
            else if (_run.CharKey == "daitaku_helios")
            {
                int hxBeat = sw - 545, hyBeat = sh - 122;
                float beatPh = c.GetHeliosBeatPhase();
                bool onBeat = c.IsHeliosOnBeat();
                Raylib.DrawRectangle(hxBeat, hyBeat, 528, 38, Rgb(14, 16, 30, 234));
                Raylib.DrawRectangle(hxBeat, hyBeat, (int)(528 * Math.Clamp(c.HeliosHype, 0f, 1f)), 4, onBeat ? Rgb(0, 255, 240) : Rgb(255, 85, 195));
                int markerX = hxBeat + 12 + (int)(504 * beatPh);
                Raylib.DrawRectangle(hxBeat + 12, hyBeat + 28, 504, 5, Rgb(35, 42, 62));
                Raylib.DrawRectangle(hxBeat + 12, hyBeat + 27, 96, 7, Rgb(0, 255, 240, 120));
                Raylib.DrawRectangle(hxBeat + 420, hyBeat + 27, 96, 7, Rgb(0, 255, 240, 120));
                Raylib.DrawRectangle(markerX - 3, hyBeat + 24, 6, 12, onBeat ? Color.Yellow : Color.White);
                string beatTxt = onBeat ? "♪ ПОПАДИ В БИТ 128 BPM (+45% МОЩЬ НАВЫКОВ)! ♪" : $"ХАЙП ТАНЦПОЛА: {(int)(c.HeliosHype * 100f)}%  |  Ловите края шкалы ритма!";
                Engine.DrawTextLeft(beatTxt, hxBeat + 12, hyBeat + 7, 13f, onBeat ? Rgb(0, 255, 240) : Color.White);
            }
            else if (_run.CharKey == "spamton")
            {
                bool isNeo = Data.ProgressData.Instance.GetSelectedSkin("spamton") == 2;
                string dealStr = c.SpamtonDealCd <= 0f ? "[F] Горячая Сделка ($25)" : $"[F] Сделка КД: {c.SpamtonDealCd:F1}с";
                string neoStr = isNeo ? $"КУКОЛЬНЫЕ НИТИ NEO: {c.SpamtonNeoStrings}/6 [Space — Парение]  |  " : "СПАМТОН G. СПАМТОН :: ";
                Engine.DrawTextLeft($"{neoStr}{dealStr}", sw - 575, sh - 98, 13.5f, isNeo ? Rgb(255, 115, 235) : Color.Gold);
            }
            else if (_run.CharKey == "susie" && IsValid(c.SusieActiveBusterProj) && !c.SusieBusterBoosted)
            {
                Engine.DrawTextLeft("★ НАЖМИТЕ [Q] ИЛИ [ПКМ] ПЕРЕД ПОПАДАНИЕМ ДЛЯ УСИЛЕНИЯ RUDE BUSTER (+60%)! ★",
                    sw - 620, sh - 102, 13.5f, Rgb(255, 115, 225));
            }
            else if (_run.CharKey == "apocalypse_bird")
            {
                string eggName = c.ApocEggPhase switch
                {
                    0 => "ЯЙЦО БОЛЬШИХ ГЛАЗ (Авто-Скан +25% Крит)",
                    1 => "ЯЙЦО ДЛИННЫХ РУК (+Бледный урон от макс. HP)",
                    _ => "ЯЙЦО МАЛОГО КЛЮВА (+22% Скорость и Вампиризм)"
                };
                Engine.DrawTextLeft($"ПТИЦА АПОКАЛИПСИСА :: {eggName} [Shift — Смена Фазы]", sw - 600, sh - 98, 13f, Color.Gold);
            }
            else if (_run.CharKey == "komeiji")
            {
                bool isKoishi = Data.ProgressData.Instance.GetSelectedSkin("komeiji") == 1;
                string kStr = isKoishi
                    ? $"КОИШИ :: Незаметность {(int)(c.GetKoishiStealthValue() * 100f)}% (≥82% — Истинная Скрытность)  |  Бутоны Роз: {c.KoishiRosebuds.Count}/6"
                    : $"САТОРИ :: Чтение Разума — Скопирована травма [{c.SatoriCopiedArchetype.ToUpper()}]";
                Engine.DrawTextLeft(kStr, sw - 605, sh - 98, 13f, isKoishi ? Rgb(115, 255, 175) : Rgb(255, 120, 185));
            }

            // 6. КОМПАКТНЫЙ ULTRAKILL STYLE METER + ПАНЕЛЬ 5 ОРУЖИЙ V1 / V2
            if (!IsSpectating && _run.CharKey == "v1")
            {
                var (rName, rCol, rMult) = c.V1GetStyleRankInfo();
                int vx = sw - 252, vy = 84;
                Raylib.DrawRectangle(vx, vy, 236, 138, Rgb(10, 12, 20, 175));
                Raylib.DrawRectangle(vx, vy, 236, 3, c.IsV2Persona ? Color.Red : rCol);
                Engine.DrawTextLeft($"{rName} (x{rMult:F2})", vx + 10, vy + 8, 18f, rCol);
                Raylib.DrawRectangle(vx + 10, vy + 32, 216, 7, Rgb(35, 38, 50));
                Raylib.DrawRectangle(vx + 10, vy + 32, (int)(216 * (c.V1StylePoints / 1000f)), 7, rCol);
                for (int i = 0; i < Math.Min(4, c.V1StyleLog.Count); i++)
                    Engine.DrawTextLeft(c.V1StyleLog[i].Text, vx + 10, vy + 46 + i * 19, 14f, c.V1StyleLog[i].Col);

                string[] wNames = { "1.РЕВОЛЬВ", "2.ДРОБОВ", "3.ГВОЗДИ", "4.РЕЛЬСА", "5.РАКЕТЫ" };
                for (int wi = 0; wi < 5; wi++)
                {
                    bool wActive = c.V1Weapon == wi + 1;
                    int wx = sw - 535 + wi * 104, wy = sh - 122;
                    Raylib.DrawRectangle(wx, wy, 98, 28, wActive ? (c.IsV2Persona ? Rgb(155, 35, 45, 230) : Rgb(42, 90, 165, 230)) : Rgb(14, 18, 28, 195));
                    Raylib.DrawRectangle(wx, wy, 98, 2, wActive ? Color.Gold : Color.Gray);
                    Engine.DrawTextCentered(wNames[wi], wx + 49, wy + 14, 12f, wActive ? Color.Gold : Color.White);
                }
                if (c.IsV2Persona)
                {
                    string v2Info = c.V2WingEnrageTimer > 0f
                        ? $"★ КРЫЛЬЯ В ЯРОСТИ ({c.V2WingEnrageTimer:F1}с :: +30% СКОР. / +35% ОГОНЬ)! ★"
                        : $"V2 ТИТАНОВАЯ БРОНЯ | МОНЕТЫ: {(int)c.V1Coins} | РЕЛЬСА: {(int)c.V1RailCharge}% | РЫВОК: {(int)c.V1Stamina}";
                    Engine.DrawTextLeft(v2Info, sw - 535, sh - 92, 13f, c.V2WingEnrageTimer > 0f ? Color.Red : Rgb(255, 135, 135));
                }
                else
                {
                    string armStr = c.V1ActiveArm == "feedbacker" ? "РУКА [V]: СИНЯЯ [F]" : "РУКА [V]: КРАСНАЯ [F/G]";
                    Engine.DrawTextLeft($"{armStr} | МОНЕТЫ: {(int)c.V1Coins} | РЕЛЬСА: {(int)c.V1RailCharge}% | РЫВОК: {(int)c.V1Stamina}",
                        sw - 535, sh - 92, 13f, Color.SkyBlue);
                }
            }

            // 7. ОКНО ДЕТАЛЬНОЙ СТАТИСТИКИ И СЕТКИ ИКОНОК ПРЕДМЕТОВ ПО УДЕРЖАНИЮ [TAB]
            if (tabOpenNow)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.C))
                {
                    Data.UserSettings.Instance.CompactItemTooltips = !Data.UserSettings.Instance.CompactItemTooltips;
                    PersistRuntimeUserSettings();
                    SoundManager.Play("ui_click");
                }

                int tw = Math.Clamp(sw - 80, 640, 880);
                int th = Math.Clamp(sh - 90, 410, 540);
                int tx = (sw - tw) / 2, ty = (sh - th) / 2;
                Raylib.DrawRectangle(tx, ty, tw, th, Rgb(12, 16, 28, 246));
                Raylib.DrawRectangle(tx, ty, tw, 4, Color.SkyBlue);
                Raylib.DrawRectangleLines(tx, ty, tw, th, Rgb(80, 145, 220, 180));

                var effTabArts = GameData.EffectiveArtifacts;
                string netInfo = _net.Connected ? $"Сеть [{_net.GameType.ToUpper()}]: {_net.RoomCode} ({1 + _remotePlayers.Count} уч.)" : "Одиночная игра";

                bool compactTipMode = Data.UserSettings.Instance.CompactItemTooltips;
                string modeBtnTxt = compactTipMode ? "[C] ПОДСКАЗКИ: КРАТКО" : "[C] ПОДСКАЗКИ: ПОЛНЫЕ СТАТЫ";
                if (Engine.DrawButton(new Rectangle(tx + tw - 246, ty + 12, 226, 28), modeBtnTxt,
                    compactTipMode ? Rgb(48, 58, 92) : Rgb(32, 78, 126), Rgb(64, 102, 164), Color.Yellow, 12f, Color.SkyBlue))
                {
                    Data.UserSettings.Instance.CompactItemTooltips = !compactTipMode;
                    PersistRuntimeUserSettings();
                }

                List<(string Key, int Count)> tabItemsList;
                if (IsSpectating && specTarget != null && specItems != null)
                {
                    string pTitle = string.IsNullOrWhiteSpace(specTarget.PlayerName) ? specTargetId.ToUpper() : specTarget.PlayerName;
                    Engine.DrawTextLeft($"НАБЛЮДЕНИЕ: {pTitle} ({hudDockCfg.Name})  |  {netInfo}", tx + 20, ty + 16, 17.5f, Color.Gold);
                    Engine.DrawTextLeft($"HP: {(int)specTarget.Hp} (+{(int)specTarget.Shield}) / {(int)specTarget.MaxHp}  |  Золото: ${specTarget.Gold}  |  Лунные: {specTarget.LunarCoins}",
                        tx + 20, ty + 44, 14.5f, Color.White);
                    Engine.DrawTextLeft($"Ваша сводка — Убито: {RunKills}  |  Урон: {(int)RunDamageDealt}  |  Золото: ${RunGoldEarned}",
                        tx + 20, ty + 68, 13.5f, Rgb(150, 195, 235));
                    tabItemsList = specItems;
                }
                else
                {
                    Engine.DrawTextLeft($"{SurvivorDatabase.Classes[_run.CharKey].Name}  |  {netInfo}", tx + 20, ty + 16, 18.5f, Color.Gold);
                    Engine.DrawTextLeft($"Урон: {c.BaseDamage:F1}  |  Атака: {c.FireRate:F1}/с  |  Скорость: {c.MoveSpeed:F1}  |  Крит: {c.CritChance * 100f:F0}%  |  Блок: {c.BlockChance * 100f:F0}%",
                        tx + 20, ty + 44, 15f, Color.White);
                    Engine.DrawTextLeft($"Убито врагов: {RunKills}  |  Нанесено урона: {(int)RunDamageDealt}  |  Собрано золота: ${RunGoldEarned}",
                        tx + 20, ty + 68, 13.5f, Rgb(150, 195, 235));
                    tabItemsList = new List<(string Key, int Count)>(_run.ItemOrder.Count);
                    foreach (var ik in _run.ItemOrder)
                        if (_run.Items.TryGetValue(ik, out int ic) && ic > 0)
                            tabItemsList.Add((ik, ic));
                }

                string tabMods = $"Сложность: {GameData.GetActiveDifficulty().Name}  |  Артефакты ({effTabArts.Count}): " +
                    (effTabArts.Count > 0 ? string.Join(", ", System.Linq.Enumerable.Select(effTabArts, ak => GameData.RunArtifacts[ak].Short)) : "Нет");
                Engine.DrawTextWrapped(tabMods, tx + 20, ty + 90, tw - 40, 12.5f, Color.Gold, 2f, maxLines: 1);

                // Сетка иконок предметов (никакие длинные описания больше не вылезают за края окна!)
                int gridX = tx + 20, gridY = ty + 116;
                int gridW = tw - 40;
                int bottomHintH = 56;
                int gridH = Math.Max(140, th - 116 - bottomHintH - 12);
                Raylib.DrawRectangle(gridX, gridY, gridW, gridH, Rgb(16, 22, 36, 225));
                Raylib.DrawRectangleLines(gridX, gridY, gridW, gridH, Rgb(54, 78, 118, 165));

                if (tabItemsList.Count == 0)
                {
                    Engine.DrawTextCentered("Инвентарь пока пуст — открывайте сундуки клавишей [E]", tx + tw * 0.5f, gridY + gridH * 0.5f, 15f, Rgb(155, 175, 205));
                }
                else
                {
                    int cellW = 60, cellH = 48, gap = 8;
                    int cols = Math.Max(5, (gridW - 16 + gap) / (cellW + gap));
                    int rows = Math.Max(2, (gridH - 16 + gap) / (cellH + gap));
                    int perPage = cols * rows;
                    int totalPages = Math.Max(1, (tabItemsList.Count - 1) / perPage + 1);
                    _tabItemPage = Math.Clamp(_tabItemPage, 0, totalPages - 1);

                    float tabWheel = Raylib.GetMouseWheelMove();
                    if (MathF.Abs(tabWheel) > 0.05f && totalPages > 1)
                        _tabItemPage = Math.Clamp(_tabItemPage - (tabWheel > 0f ? 1 : -1), 0, totalPages - 1);

                    int startIdx = _tabItemPage * perPage;
                    for (int idx = 0; idx < perPage && startIdx + idx < tabItemsList.Count; idx++)
                    {
                        var (ik, icnt) = tabItemsList[startIdx + idx];
                        if (!GameData.ItemPool.TryGetValue(ik, out var info)) continue;
                        int r = idx / cols, col = idx % cols;
                        Rectangle cRect = new(gridX + 10 + col * (cellW + gap), gridY + 10 + r * (cellH + gap), cellW, cellH);
                        if (DrawItemIconCell(cRect, ik, info, icnt, hudMousePos, compactHud: false))
                        {
                            hoveredHudItem = info;
                            hoveredHudItemCount = icnt;
                        }
                    }

                    if (totalPages > 1)
                    {
                        Engine.DrawTextLeft($"Колесо мыши — страница предметов {_tabItemPage + 1}/{totalPages}", gridX + 10, gridY + gridH - 20, 12f, Color.SkyBlue);
                    }
                }

                // Нижняя строка быстрой справки внутри окна TAB
                int barY = ty + th - bottomHintH - 6;
                Raylib.DrawRectangle(gridX, barY, gridW, bottomHintH, Rgb(18, 24, 40, 242));
                if (hoveredHudItem != null)
                {
                    Raylib.DrawRectangle(gridX, barY, 4, bottomHintH, hoveredHudItem.Color);
                    Engine.DrawTextLeft($"{hoveredHudItem.Name} [x{hoveredHudItemCount}] ({hoveredHudItem.TierName})", gridX + 12, barY + 6, 14.5f, hoveredHudItem.Color);
                    Engine.DrawTextWrapped(hoveredHudItem.GetAbbreviatedDesc(96), gridX + 12, barY + 26, gridW - 24, 13f, Color.White, 2f, maxLines: 1);
                }
                else
                {
                    Raylib.DrawRectangle(gridX, barY, 4, bottomHintH, Color.SkyBlue);
                    Engine.DrawTextLeft("Наведите курсор мыши на иконку любого предмета выше для просмотра статов!", gridX + 12, barY + 10, 13.5f, Rgb(190, 215, 245));
                    Engine.DrawTextLeft("Нажмите [C] (или кнопку вверху справа), чтобы переключить подробные / краткие описания.", gridX + 12, barY + 31, 12.5f, Color.Gold);
                }
            }

            // 8. ИНТЕРАКТИВНОЕ ОКНО АРТЕФАКТА КОМАНДОВАНИЯ (АДАПТИВНЫЕ ЯЧЕЙКИ И ОБРЕЗКА ОПИСАНИЙ В РАМКАХ ОКНА)
            if (!IsSpectating && _commandPickerQueue.Count > 0 && !_paused)
            {
                if (Raylib.IsCursorHidden()) Raylib.EnableCursor();
                string pickTier = _commandPickerQueue.Peek();
                var tierItems = new List<string>();
                foreach (var kv in GameData.ItemPool)
                {
                    if (kv.Key == "tonic_affliction") continue;
                    if (!ProgressData.Instance.UnlockedItems.Contains(kv.Key)) continue;
                    if (kv.Value.Tier == pickTier) tierItems.Add(kv.Key);
                }
                if (tierItems.Count == 0)
                {
                    foreach (var kv in GameData.ItemPool)
                        if (kv.Value.Tier == "white") tierItems.Add(kv.Key);
                }

                Color tierCol = pickTier switch
                {
                    "green" => Rgb(80, 235, 95),
                    "red" => Rgb(250, 65, 65),
                    "lunar" => Rgb(75, 185, 255),
                    "void" => Rgb(195, 90, 255),
                    _ => Color.White
                };
                string tierTitle = pickTier switch
                {
                    "green" => "НЕОБЫЧНАЯ ЭССЕНЦИЯ (ЗЕЛЁНЫЙ ПРЕДМЕТ)",
                    "red" => "ЛЕГЕНДАРНАЯ ЭССЕНЦИЯ (КРАСНЫЙ ПРЕДМЕТ)",
                    "lunar" => "ЛУННАЯ ЭССЕНЦИЯ (СИНИЙ ПРЕДМЕТ)",
                    "void" => "ЭССЕНЦИЯ ПУСТОТЫ (ФИОЛЕТОВЫЙ ПРЕДМЕТ)",
                    _ => "ОБЫЧНАЯ ЭССЕНЦИЯ (БЕЛЫЙ ПРЕДМЕТ)"
                };

                int mw = Math.Min(840, sw - 40);
                int mh = Math.Min(520, sh - 40);
                int mx = (sw - mw) / 2, my = (sh - mh) / 2;
                Raylib.DrawRectangle(0, 0, sw, sh, Rgb(6, 8, 16, 145));
                Raylib.DrawRectangle(mx, my, mw, mh, Rgb(12, 16, 28, 248));
                Raylib.DrawRectangle(mx, my, mw, 4, tierCol);
                Engine.DrawTextCentered($"АРТЕФАКТ КОМАНДОВАНИЯ :: {tierTitle}", sw * 0.5f, my + 22f, 19f, tierCol);
                Engine.DrawTextCentered($"Выберите предмет кликом по иконке (В очереди эссенций: {_commandPickerQueue.Count})", sw * 0.5f, my + 45f, 13.5f, Color.Gold);

                Vector2 mp = Engine.GetUiMousePosition();
                string hName = "Наведите курсор на иконку предмета";
                string hDesc = "Нажмите ЛКМ по любому предмету ниже, чтобы немедленно добавить его в инвентарь.";
                Color hCol = tierCol;

                int cols = Math.Max(4, (mw - 48) / 154);
                int cellGap = 8;
                int cellW = (mw - 50 - cellGap * (cols - 1)) / cols;
                int rowsNeeded = Math.Max(1, (tierItems.Count - 1) / cols + 1);
                int gridAvailH = mh - 182;
                int cellH = Math.Clamp((gridAvailH - cellGap * (rowsNeeded - 1)) / rowsNeeded, 42, 56);

                for (int idx = 0; idx < tierItems.Count; idx++)
                {
                    string ik = tierItems[idx];
                    var inf = GameData.ItemPool[ik];
                    int r = idx / cols, col = idx % cols;
                    Rectangle iRect = new(mx + 25 + col * (cellW + cellGap), my + 68 + r * (cellH + cellGap), cellW, cellH);
                    int haveCnt = _run.Items.TryGetValue(ik, out int hc) ? hc : 0;
                    string countBadge = haveCnt > 0 ? $" (x{haveCnt})" : "";

                    if (Raylib.CheckCollisionPointRec(mp, iRect))
                    {
                        hName = $"{inf.Name} [{inf.Short}]{countBadge}";
                        hDesc = inf.Desc;
                        hCol = inf.Color;
                    }

                    string shortNm = inf.Name.Length > 18 ? inf.Name[..16] + ".." : inf.Name;
                    if (Engine.DrawButton(iRect, $"[{inf.Short}]{countBadge}\n{shortNm}",
                        Rgb(22, 30, 46), Rgb(48, 78, 128), Color.White, 11.5f, inf.Color))
                    {
                        _commandPickerQueue.Dequeue();
                        _run.CollectItem(ik);
                        ConsumeGameplayClickOnResume();
                        RestoreInGameCursorIfNeeded();
                        break;
                    }
                }

                int descBoxY = my + mh - 98;
                Raylib.DrawRectangle(mx + 25, descBoxY, mw - 50, 80, Rgb(18, 24, 40, 245));
                Raylib.DrawRectangle(mx + 25, descBoxY, 4, 80, hCol);
                Engine.DrawTextLeft(hName, mx + 38, descBoxY + 8, 16.5f, hCol);
                Engine.DrawTextWrapped(hDesc, mx + 38, descBoxY + 32, mw - 76, 13.5f, Color.White, 3f, maxLines: 2);
            }

            // 9. ИНТЕРАКТИВНОЕ ОКНО ВЕРСТАКА СТИВА (MINECRAFT WORKBENCH CRAFTING POOL)
            if (_workbenchOpen && !_paused)
            {
                DrawWorkbenchModalUI(sw, sh);
            }

            // 10. ХАБ UES «SAFE TRAVELS», КОЛЕСО ЭМОЦИЙ [L], ДИАЛОГИ ЭКИПАЖА И К-Н-Б
            if (!_paused)
            {
                if (_run.IsSafeTravels)
                    DrawSafeTravelsHubOverlay(sw, sh);

                DrawSafeTravelsDialogueModal(sw, sh);
                DrawRpsMiniGameModal(sw, sh);
                DrawEmoteWheelOverlay(sw, sh);
            }

            // 11. ВСПЛЫВАЮЩЕЕ ОКНО ДЕТАЛЬНЫХ СТАТОВ ПРЕДМЕТА ПОВЕРХ HUD (ВСЕГДА В РАМКАХ ЭКРАНА!)
            if (hoveredHudItem != null && !_paused && _commandPickerQueue.Count == 0 && !_workbenchOpen)
            {
                DrawClampedItemTooltip(hoveredHudItem, hoveredHudItemCount, hudMousePos);
            }
        }

        private static int CountRunItemsByTier(string? tierFilter = null)
        {
            if (_run == null) return 0;
            int total = 0;
            foreach (var kv in _run.Items)
            {
                if (kv.Value <= 0 || kv.Key == "tonic_affliction") continue;
                if (tierFilter == null || (GameData.ItemPool.TryGetValue(kv.Key, out var inf) && inf.Tier == tierFilter))
                    total += kv.Value;
            }
            return total;
        }

        private static bool ConsumeRunItems(int count, string? tierFilter = null)
        {
            if (_run == null || CountRunItemsByTier(tierFilter) < count) return false;
            int left = count;
            for (int i = _run.ItemOrder.Count - 1; i >= 0 && left > 0; i--)
            {
                string k = _run.ItemOrder[i];
                if (k == "tonic_affliction" || !_run.Items.TryGetValue(k, out int have) || have <= 0) continue;
                if (tierFilter != null && (!GameData.ItemPool.TryGetValue(k, out var inf) || inf.Tier != tierFilter)) continue;

                int take = Math.Min(have, left);
                _run.Items[k] -= take;
                left -= take;
                if (_run.Items[k] <= 0)
                {
                    _run.Items[k] = 0;
                    _run.ItemOrder.RemoveAt(i);
                }
            }
            return true;
        }

        private static void DrawWorkbenchModalUI(int sw, int sh)
        {
            if (_run == null) return;
            if (Raylib.IsCursorHidden()) Raylib.EnableCursor();
            var c = _run.Combat;

            int mw = 920, mh = 590, mx = (sw - mw) / 2, my = (sh - mh) / 2;
            Raylib.DrawRectangle(0, 0, sw, sh, Rgb(6, 8, 16, 165));
            Raylib.DrawRectangle(mx, my, mw, mh, Rgb(14, 18, 30, 250));
            Raylib.DrawRectangle(mx, my, mw, 4, Rgb(80, 235, 245));

            Engine.DrawTextLeft("⛏ ВЕРСТАК И НАКОВАЛЬНЯ СТИВА :: ПУЛ КРАФТОВ MINECRAFT", mx + 24, my + 16, 20f, Rgb(80, 235, 245));
            if (Engine.DrawButton(new Rectangle(mx + mw - 184, my + 12, 160, 32), "ЗАКРЫТЬ [ESC]",
                Rgb(110, 38, 46), Rgb(155, 55, 65), Color.White, 14f, Color.Red))
            {
                _workbenchOpen = false;
                ConsumeGameplayClickOnResume();
                RestoreInGameCursorIfNeeded();
                return;
            }

            // Плашка текущих добытых ресурсов с карты и предметов в инвентаре
            int whiteItems = CountRunItemsByTier("white");
            int greenItems = CountRunItemsByTier("green");
            int allItems = CountRunItemsByTier(null);
            Raylib.DrawRectangle(mx + 24, my + 52, mw - 48, 44, Rgb(20, 28, 44, 240));
            Raylib.DrawRectangle(mx + 24, my + 52, 4, 44, Color.Gold);
            Engine.DrawTextLeft(
                $"РЕСУРСЫ С КАРТЫ:   Дерево: {c.SteveWood}   |   Камень: {c.SteveStone}   |   Железо: {c.SteveIron}   |   Алмазы: {c.SteveDiamond}",
                mx + 38, my + 58, 15f, Color.SkyBlue);
            Engine.DrawTextLeft(
                $"ПРЕДМЕТЫ В ИНВЕНТАРЕ:   Обычные (Белые): {whiteItems}   |   Необычные (Зелёные): {greenItems}   |   Всего предметов: {allItems}",
                mx + 38, my + 77, 13f, Color.Gold);

            var recipes = new (string Title, string CostText, string Desc, Color Col, bool CanCraft, Action DoCraft)[]
            {
                (
                    "1. Алмазный Меч Остроты V + Очки Творца",
                    "Цена: 2 Дерева + 3 Алмаза",
                    "Повышает ранг меча Стива до Алмазного (+35% базового урона навсегда) и создаёт предмет «Очки Творца» (+10% крита).",
                    Rgb(80, 240, 250),
                    c.SteveWood >= 2 && c.SteveDiamond >= 3,
                    () =>
                    {
                        c.SteveWood -= 2; c.SteveDiamond -= 3;
                        c.SteveSwordTier = Math.Max(c.SteveSwordTier, 1);
                        _run.CollectItem("lens");
                        ShowBanner("СКРАФЧЕНО: Алмазный Меч Остроты V (+35% урона) + Очки Творца!", Rgb(80, 240, 250), 3.5f);
                    }
                ),
                (
                    "2. Незеритовый Клинок + Грань Бесконечности",
                    "Цена: 4 Железа + 5 Алмазов + 1 Белый предмет",
                    "Максимальный ранг меча Стива (+75% базового урона) + перековывает 1 обычный предмет в «Грань Бесконечности»!",
                    Rgb(255, 95, 75),
                    c.SteveIron >= 4 && c.SteveDiamond >= 5 && whiteItems >= 1,
                    () =>
                    {
                        if (!ConsumeRunItems(1, "white")) return;
                        c.SteveIron -= 4; c.SteveDiamond -= 5;
                        c.SteveSwordTier = 2;
                        _run.CollectItem("infinity_edge");
                        ShowBanner("СКРАФЧЕНО: Незеритовый Клинок (+75% урона) + Грань Бесконечности!", Color.Gold, 4.0f);
                    }
                ),
                (
                    "3. Железный Доспех Защиты + Бронепластина",
                    "Цена: 5 Камня + 4 Железа",
                    "Надевает на Стива Железный Доспех (-22% получаемого урона, +50 Щита) и создаёт предмет «Бронепластина».",
                    Rgb(215, 225, 240),
                    c.SteveStone >= 5 && c.SteveIron >= 4,
                    () =>
                    {
                        c.SteveStone -= 5; c.SteveIron -= 4;
                        c.SteveArmorTier = Math.Max(c.SteveArmorTier, 1);
                        c.Shield = MathF.Min(_run.PlayerMaxHp, c.Shield + 50f);
                        _run.CollectItem("repulsion_armor");
                        ShowBanner("СКРАФЧЕНО: Железный Доспех Защиты + Бронепластина!", Color.SkyBlue, 3.5f);
                    }
                ),
                (
                    "4. Незеритовый Доспех + Сердце Тарраски",
                    "Цена: 6 Железа + 6 Алмазов + 1 Предмет",
                    "Максимальная Незеритовая Броня (-54% получаемого урона, +120 Щита) и легендарное «Сердце Тарраски» (+100 HP и реген)!",
                    Rgb(195, 90, 255),
                    c.SteveIron >= 6 && c.SteveDiamond >= 6 && allItems >= 1,
                    () =>
                    {
                        if (!ConsumeRunItems(1, null)) return;
                        c.SteveIron -= 6; c.SteveDiamond -= 6;
                        c.SteveArmorTier = 3;
                        c.Shield = MathF.Min(_run.PlayerMaxHp, c.Shield + 120f);
                        _run.CollectItem("tarrasque");
                        ShowBanner("СКРАФЧЕНО: Незеритовый Доспех + Сердце Тарраски!", Color.Magenta, 4.0f);
                    }
                ),
                (
                    "5. Тотем Бессмертия (Totem of Undying)",
                    "Цена: 4 Дерева + 3 Железа + 2 Алмаза",
                    "Даёт Тотем Бессмертия в левую руку: при смертельном ударе спасает жизнь, восстанавливает 65% HP и взрывает врагов!",
                    Color.Gold,
                    c.SteveWood >= 4 && c.SteveIron >= 3 && c.SteveDiamond >= 2 && !c.SteveHasTotem,
                    () =>
                    {
                        c.SteveWood -= 4; c.SteveIron -= 3; c.SteveDiamond -= 2;
                        c.SteveHasTotem = true;
                        _run.HealPlayer(_run.PlayerMaxHp * 0.50f);
                        ShowBanner("СКРАФЧЕНО: Тотем Бессмертия экипирован!", Color.Gold, 3.8f);
                    }
                ),
                (
                    "6. Зачарованное Золотое Яблоко Нотча",
                    "Цена: 3 Дерева + 2 Камня + 2 Алмаза",
                    "Мгновенно восстанавливает 100% HP, даёт +80 Щита и создаёт «Копыто Пауля» (+скорость) и «Шприц Солдата» (+атака).",
                    Rgb(255, 215, 55),
                    c.SteveWood >= 3 && c.SteveStone >= 2 && c.SteveDiamond >= 2,
                    () =>
                    {
                        c.SteveWood -= 3; c.SteveStone -= 2; c.SteveDiamond -= 2;
                        _run.HealPlayer(_run.PlayerMaxHp);
                        c.Shield = MathF.Min(_run.PlayerMaxHp, c.Shield + 80f);
                        _run.CollectItem("hoof");
                        _run.CollectItem("syringe");
                        ShowBanner("СКРАФЧЕНО: Зачарованное Золотое Яблоко (+100% HP, Копыто и Шприц)!", Color.Gold, 3.8f);
                    }
                ),
                (
                    "7. Шахтёрская Ковка: Необычный Предмет (Зелёный)",
                    "Цена: 4 Дерева + 4 Камня + 2 Железа",
                    "Выковывает из добытой руды случайный зелёный предмет (Ракета AT-G, Укулеле, Кольцо Кьяро/Рунальда, Перо Хопу или Коса).",
                    Rgb(80, 235, 95),
                    c.SteveWood >= 4 && c.SteveStone >= 4 && c.SteveIron >= 2,
                    () =>
                    {
                        c.SteveWood -= 4; c.SteveStone -= 4; c.SteveIron -= 2;
                        string[] greenPool = { "atg_missile", "ukulele", "kjaro_band", "runald_band", "harvester_scythe", "feather" };
                        string rolled = greenPool[Random.Shared.Next(0, greenPool.Length)];
                        _run.CollectItem(rolled);
                    }
                ),
                (
                    "8. Перековка на Наковальне: Легендарный Предмет (Красный)",
                    "Цена: 2 Белых предмета + 4 Камня + 3 Железа + 2 Алмаза",
                    "Переплавляет 2 обычных предмета из инвентаря и ресурсы карты в случайный Легендарный (Красный) артефакт!",
                    Rgb(250, 65, 65),
                    whiteItems >= 2 && c.SteveStone >= 4 && c.SteveIron >= 3 && c.SteveDiamond >= 2,
                    () =>
                    {
                        if (!ConsumeRunItems(2, "white")) return;
                        c.SteveStone -= 4; c.SteveIron -= 3; c.SteveDiamond -= 2;
                        string[] redPool = { "clover", "behemoth", "tesla_coil", "alien_head", "rabadons_deathcap", "blade_ruined_king" };
                        string rolled = redPool[Random.Shared.Next(0, redPool.Length)];
                        _run.CollectItem(rolled);
                    }
                )
            };

            Vector2 mp = Engine.GetUiMousePosition();
            string hoverTitle = "НАВЕДИТЕ КУРСОР НА РЕЦЕПТ ВЕРСТАКА";
            string hoverCost = "Ломайте пол (ЛКМ вниз / ПКМ), стены, скалы, сундуки и алтари киркой Стива для добычи ресурсов";
            string hoverDesc = "После победы над Боссом Телепорта и зарядки портала рядом всегда спавнится Верстак для спокойного крафта!";
            Color hoverCol = Rgb(80, 235, 245);

            for (int i = 0; i < recipes.Length; i++)
            {
                var r = recipes[i];
                int row = i / 2, col = i % 2;
                int bx = mx + 24 + col * 442;
                int by = my + 108 + row * 90;
                Rectangle rect = new(bx, by, 430, 80);

                if (Raylib.CheckCollisionPointRec(mp, rect))
                {
                    hoverTitle = r.Title;
                    hoverCost = $"{r.CostText}   —   {(r.CanCraft ? "ДОСТУПНО ДЛЯ КРАФТА ✓" : "НЕДОСТАТОЧНО РЕСУРСОВ ИЛИ ПРЕДМЕТОВ")}";
                    hoverDesc = r.Desc;
                    hoverCol = r.CanCraft ? r.Col : Color.Red;
                }

                string btnTxt = $"{r.Title}\n{r.CostText}";
                Color bgCol = r.CanCraft ? Rgb(24, 38, 58, 245) : Rgb(18, 20, 28, 230);
                Color brdCol = r.CanCraft ? r.Col : Color.Gray;

                if (Engine.DrawButton(rect, btnTxt, bgCol, Rgb(46, 78, 126),
                    r.CanCraft ? Color.White : Color.Gray, 13f, brdCol))
                {
                    if (r.CanCraft)
                    {
                        SoundManager.Play("item_legendary", 0.95f);
                        r.DoCraft();
                    }
                    else
                    {
                        SoundManager.Play("ui_error", 0.85f);
                        ShowBanner($"НЕ ХВАТАЕТ РЕСУРСОВ! ({r.CostText})", Color.Red, 2.5f);
                    }
                }
            }

            // Нижняя карточка подробного описания рецепта
            int descY = my + mh - 110;
            Raylib.DrawRectangle(mx + 24, descY, mw - 48, 92, Rgb(18, 24, 40, 248));
            Raylib.DrawRectangle(mx + 24, descY, 4, 92, hoverCol);
            Engine.DrawTextLeft(hoverTitle, mx + 38, descY + 10, 17f, hoverCol);
            Engine.DrawTextLeft(hoverCost, mx + 38, descY + 34, 14f, Color.Gold);
            DrawWrappedText(hoverDesc, mx + 38, descY + 58, 95, 14f, Color.White, maxLines: 2, lineStep: 18f);
        }

        

        private static void DrawLogbookUI()
        {
            if (_logDetailOpen && _logCategory != "history")
            {
                UpdateAndDrawLogbookDetailModal();
                return;
            }

            DrawHeader();
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            (string id, string title)[] tabs =
            {
                ("items", "АРТЕФАКТЫ"), ("survivors", "ПЕРСОНАЖИ"), ("monsters", "ВРАГИ"),
                ("bosses", "БОССЫ"), ("stages", "ЛОКАЦИИ"), ("achievements", "ДОСТИЖЕНИЯ"),
                ("modifiers", "МОДИФИКАТОРЫ"), ("history", "ЗАБЕГИ И РЕПЛЕИ")
            };

            int tabGap = 6;
            int tabStartX = 24;
            int tabW = Math.Max(104, (sw - tabStartX * 2 - (tabs.Length - 1) * tabGap) / tabs.Length);
            float tabFont = tabW < 142 ? 11.5f : 13.5f;

            for (int i = 0; i < tabs.Length; i++)
            {
                var (cid, title) = tabs[i];
                bool active = _logCategory == cid;
                if (Engine.DrawButton(new Rectangle(tabStartX + i * (tabW + tabGap), 70, tabW, 36), title,
                    active ? Rgb(75, 115, 185) : Rgb(32, 40, 62), Rgb(95, 135, 210),
                    cid == "history" && !active ? Color.Gold : Color.White, tabFont,
                    cid == "history" ? (active ? Color.Yellow : Color.SkyBlue) : null))
                {
                    _logCategory = cid;
                    _logDetailOpen = false;
                    if (cid == "history")
                    {
                        _historyPage = 0;
                        if (string.IsNullOrEmpty(_selectedHistoryRunId) && Data.RunHistoryStorage.Entries.Count > 0)
                            _selectedHistoryRunId = Data.RunHistoryStorage.Entries[0].RunId;
                    }
                    else
                    {
                        _logPage = 0;
                        var list = GetLogEntries(cid);
                        if (list.Count > 0) SelectLogbookEntry(cid, list[0].Key);
                    }
                }
            }

            if (_logCategory == "history")
            {
                DrawRunHistoryLogbookView();
                return;
            }

            int px = 24, py = 114;
            int pw = Math.Clamp((int)(sw * 0.48f), 468, 700);
            int ph = Math.Clamp(sh - py - 16, 460, 700);

            Raylib.DrawRectangle(px, py, pw, ph, Rgb(12, 16, 28, 240));
            Engine.DrawTextWrapped("Двойной клик по ячейке (или Enter) — открыть полную страницу с лором и 3D-моделью!",
                px + 16, py + 6, pw - 32, 12f, Rgb(145, 215, 255), 2f, maxLines: 1);

            var entries = GetLogEntries(_logCategory);
            int cols = 6, rows = 3;
            int perPage = cols * rows;
            int totalPages = Math.Max(1, (entries.Count - 1) / perPage + 1);
            _logPage = Math.Clamp(_logPage, 0, totalPages - 1);

            int gridX = px + 16, gridY = py + 28;
            int gridW = pw - 32;
            int gapX = 8, gapY = Math.Clamp((ph - 255) / 14, 6, 12);
            int cellW = (gridW - (cols - 1) * gapX) / cols;
            int cellH = Math.Clamp((ph - 275) / rows, 48, 74);

            Vector2 mp = Engine.GetUiMousePosition();
            Data.ItemInfo? hoveredLogItem = null;

            int startIdx = _logPage * perPage;
            for (int i = 0; i < perPage && startIdx + i < entries.Count; i++)
            {
                var e = entries[startIdx + i];
                int r = i / cols, c = i % cols;
                Rectangle box = new(gridX + c * (cellW + gapX), gridY + r * (cellH + gapY), cellW, cellH);
                bool isSel = e.Key == _logSelectedKey;

                if (_logCategory == "items" && e.Unlocked && GameData.ItemPool.TryGetValue(e.Key, out var itemInf))
                {
                    if (DrawItemIconCell(box, e.Key, itemInf, 1, mp, compactHud: false))
                        hoveredLogItem = itemInf;
                    if (isSel)
                        Raylib.DrawRectangleLinesEx(box, 2f, Color.Yellow);
                    if (Raylib.CheckCollisionPointRec(mp, box) && Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        SoundManager.Play("ui_click");
                        SelectLogbookEntry(_logCategory, e.Key);
                        RegisterLogbookTileClick(_logCategory, e.Key);
                    }
                }
                else
                {
                    string tileLabel = _logCategory == "achievements"
                        ? (e.Unlocked ? "✓ ACH" : "ACH")
                        : (e.Unlocked ? e.Short : $"[{e.Short}]");
                    if (Engine.DrawButton(box, tileLabel,
                        e.Unlocked ? Rgb(24, 32, 48) : Rgb(18, 20, 30), e.Color,
                        e.Unlocked ? e.Color : Rgb(165, 178, 198), cellW < 84 ? 13f : 15f, isSel ? Color.Yellow : e.Color))
                    {
                        SelectLogbookEntry(_logCategory, e.Key);
                        RegisterLogbookTileClick(_logCategory, e.Key);
                    }
                }
            }

            int navY = gridY + rows * (cellH + gapY) + 4;
            int navBtnW = Math.Min(126, (gridW - 140) / 2);
            if (Engine.DrawButton(new Rectangle(gridX + 24, navY, navBtnW, 32), "<- ПРЕД.", Rgb(40, 52, 78), Rgb(65, 85, 120), Color.White, 14f))
                _logPage = Math.Clamp(_logPage - 1, 0, totalPages - 1);
            Engine.DrawTextCentered($"Стр. {_logPage + 1} / {totalPages}", px + pw * 0.5f, navY + 16f, 16f, Color.White);
            if (Engine.DrawButton(new Rectangle(px + pw - 16 - 24 - navBtnW, navY, navBtnW, 32), "СЛЕД. ->", Rgb(40, 52, 78), Rgb(65, 85, 120), Color.White, 14f))
                _logPage = Math.Clamp(_logPage + 1, 0, totalPages - 1);

            int btnY = py + ph - 48;
            int leftBtnW = Math.Max(195, (gridW - 10) * 44 / 100);
            int rightBtnW = gridW - leftBtnW - 10;
            if (Engine.DrawButton(new Rectangle(gridX, btnY, leftBtnW, 38), "<- В МЕНЮ [ESC]", Rgb(65, 72, 95), Rgb(95, 105, 135), Color.White, 14f))
            {
                _screen = "main";
                SelectCharacter(_selectedChar);
            }

            // Нижняя карточка описания выбранной записи + кнопка открытия полной страницы с лором и 3D-вращением
            var cur = entries.Find(x => x.Key == _logSelectedKey);
            if (cur.Key != null)
            {
                if (Engine.DrawButton(new Rectangle(gridX + leftBtnW + 10, btnY, rightBtnW, 38), "СТРАНИЦА И ЛОР [2x КЛИК / ENTER]",
                    Rgb(42, 86, 148), Rgb(64, 122, 198), Color.Yellow, rightBtnW < 280 ? 12f : 13.5f, Color.SkyBlue)
                    || Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    OpenLogbookDetailPage(_logCategory, cur.Key, 0);
                    return;
                }

                int cardY = navY + 40;
                int cardH = Math.Max(96, btnY - 8 - cardY);
                Raylib.DrawRectangle(gridX, cardY, gridW, cardH, Rgb(18, 24, 40, 245));
                Raylib.DrawRectangle(gridX, cardY, gridW, 4, cur.Color);
                Engine.DrawTextWrapped(cur.Title, gridX + 14, cardY + 8, gridW - 28, 19f, cur.Color, 2f, maxLines: 1);
                Engine.DrawTextWrapped(cur.Sub, gridX + 14, cardY + 32, gridW - 28, 14f, Color.Gold, 2f, maxLines: 1);
                int maxDescLines = Math.Max(2, (cardH - 56) / 19);
                Engine.DrawTextWrapped(cur.Desc, gridX + 14, cardY + 54, gridW - 28, 14f, Color.White, 3f, maxLines: maxDescLines);
            }

            if (hoveredLogItem != null)
            {
                DrawClampedItemTooltip(hoveredLogItem, 1, mp);
            }
        }

        private static void DrawRunHistoryLogbookView()
        {
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            var runs = Data.RunHistoryStorage.Entries;
            if ((string.IsNullOrEmpty(_selectedHistoryRunId) || !runs.Exists(r => r.RunId == _selectedHistoryRunId)) && runs.Count > 0)
                _selectedHistoryRunId = runs[0].RunId;

            int lx = 24, ly = 114;
            int totalW = sw - 48, gap = 14;
            int lw = Math.Clamp((int)(totalW * 0.45f), 430, 684);
            int rx = lx + lw + gap;
            int rw = Math.Max(460, sw - 24 - rx);
            int lh = Math.Max(460, sh - ly - 16), rh = lh;

            Raylib.DrawRectangle(lx, ly, lw, lh, Rgb(12, 16, 28, 242));
            Raylib.DrawRectangle(lx, ly, lw, 3, Color.SkyBlue);
            int ry = ly;
            Raylib.DrawRectangle(rx, ry, rw, rh, Rgb(12, 16, 28, 245));
            Raylib.DrawRectangle(rx, ry, rw, 3, Color.Gold);

            int wins = 0, losses = 0, abandoned = 0, replaysAvail = 0;
            foreach (var r in runs)
            {
                if (r.Outcome == "victory") wins++;
                else if (r.Outcome == "abandoned") abandoned++;
                else losses++;
                if (Data.RunHistoryStorage.HasReplayFile(r)) replaysAvail++;
            }

            Engine.DrawTextWrapped(
                $"Всего: {runs.Count}  |  Побед: {wins}  |  Поражений: {losses}  |  Прервано: {abandoned}  |  Реплеев: {replaysAvail}",
                lx + 16, ly + 10, lw - 32, 13f, Rgb(175, 215, 250), 2f, maxLines: 1);

            int cardStep = 76;
            int perPage = Math.Clamp((lh - 132) / cardStep, 4, 8);
            int totalPages = Math.Max(1, (runs.Count - 1) / perPage + 1);
            float wheel = Raylib.GetMouseWheelMove();
            Vector2 mp = Engine.GetUiMousePosition();
            if (MathF.Abs(wheel) > 0.01f && Raylib.CheckCollisionPointRec(mp, new Rectangle(lx, ly, lw, lh)))
                _historyPage = Math.Clamp(_historyPage + (wheel < 0 ? 1 : -1), 0, totalPages - 1);
            _historyPage = Math.Clamp(_historyPage, 0, totalPages - 1);

            if (runs.Count == 0)
            {
                Engine.DrawTextCentered("ИСТОРИЯ ЗАБЕГОВ ПОКА ПУСТА", lx + lw * 0.5f, ly + lh * 0.38f, 20f, Color.Gold);
                Engine.DrawTextCentered("Завершите или прервите любой забег — его полный отчёт", lx + lw * 0.5f, ly + lh * 0.44f, 14f, Rgb(180, 200, 225));
                Engine.DrawTextCentered("появится здесь (реплеи сохраняются по вашей настройке).", lx + lw * 0.5f, ly + lh * 0.49f, 14f, Rgb(180, 200, 225));
            }
            else
            {
                int startIdx = _historyPage * perPage;
                for (int i = 0; i < perPage && startIdx + i < runs.Count; i++)
                {
                    var item = runs[startIdx + i];
                    bool isSel = item.RunId == _selectedHistoryRunId;
                    int cy = ly + 36 + i * cardStep;
                    Rectangle cardRect = new(lx + 14, cy, lw - 28, 70);

                    Color outCol = item.Outcome switch
                    {
                        "victory" => Color.Gold,
                        "abandoned" => Color.SkyBlue,
                        _ => Rgb(245, 75, 75)
                    };
                    string outTag = item.Outcome switch
                    {
                        "victory" => "ПОБЕДА",
                        "abandoned" => "ПРЕРВАН",
                        _ => "ПОРАЖЕНИЕ"
                    };
                    if (item.WasSpectatorOnly) outTag += " · ЗРИТЕЛЬ";

                    bool hovered = Raylib.CheckCollisionPointRec(mp, cardRect);
                    Color cardBg = isSel ? Rgb(34, 52, 86, 250) : (hovered ? Rgb(24, 36, 58, 245) : Rgb(18, 24, 40, 235));
                    Raylib.DrawRectangleRec(cardRect, cardBg);
                    Raylib.DrawRectangle((int)cardRect.X, (int)cardRect.Y, 5, (int)cardRect.Height, outCol);
                    if (isSel)
                        Raylib.DrawRectangleLinesEx(cardRect, 2f, Color.Yellow);

                    var surv = SurvivorDatabase.Classes.GetValueOrDefault(item.CharKey) ?? SurvivorDatabase.Classes["commando"];
                    string heroTitle = surv.Name;
                    if (surv.Skins.Count > 0)
                    {
                        var sk = surv.Skins[Math.Clamp(item.SkinIdx, 0, surv.Skins.Count - 1)];
                        if (sk.CustomSkillVariants != null) heroTitle = sk.Name;
                    }

                    int durM = (int)(item.DurationSec / 60f), durS = (int)(item.DurationSec % 60f);
                    string modeShort = item.GameMode == "coop" ? "КООП" : (item.GameMode == "pvp" ? "PvP" : "СОЛО");
                    Engine.DrawTextWrapped($"[{outTag}]  {heroTitle} ({modeShort})", (int)cardRect.X + 12, cy + 7, cardRect.Width - 156f, 14.5f, outCol, 1f, maxLines: 1);
                    Engine.DrawTextLeft(item.PlayedAt, (int)(cardRect.X + cardRect.Width - 140), cy + 9, 12f, Rgb(165, 190, 220));

                    Engine.DrawTextWrapped(
                        $"Э{item.FinalStageNum}: {item.FinalStageName} | {durM:00}:{durS:00} | Убито: {item.Kills} | Предм.: {item.TotalItemsCount}",
                        (int)cardRect.X + 12, cy + 29, cardRect.Width - 24f, 12.5f, Color.White, 1f, maxLines: 1);

                    var diffDef = GameData.Difficulties.GetValueOrDefault(item.DifficultyKey) ?? GameData.Difficulties["rainstorm"];
                    Engine.DrawTextLeft($"Сложность: {diffDef.Name}", (int)cardRect.X + 12, cy + 49, 11.5f, diffDef.Color);

                    bool hasRpl = Data.RunHistoryStorage.HasReplayFile(item);
                    if (hasRpl)
                    {
                        Raylib.DrawRectangle((int)(cardRect.X + cardRect.Width - 108), cy + 44, 98, 20, Rgb(26, 92, 68, 235));
                        Engine.DrawTextCentered("▶ 3D-РЕПЛЕЙ", cardRect.X + cardRect.Width - 59f, cy + 54f, 11f, Color.Lime);
                    }

                    if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        SoundManager.Play("ui_click");
                        _selectedHistoryRunId = item.RunId;
                    }
                }
            }

            int navBtnW = Math.Min(118, (lw - 180) / 2);
            if (Engine.DrawButton(new Rectangle(lx + 14, ly + lh - 84, navBtnW, 32), "<- ПРЕД.", Rgb(40, 52, 78), Rgb(65, 85, 120), Color.White, 13.5f))
                _historyPage = Math.Clamp(_historyPage - 1, 0, totalPages - 1);
            Engine.DrawTextCentered($"Стр. {_historyPage + 1} / {totalPages}", lx + lw * 0.5f, ly + lh - 68f, 15f, Color.White);
            if (Engine.DrawButton(new Rectangle(lx + lw - 14 - navBtnW, ly + lh - 84, navBtnW, 32), "СЛЕД. ->", Rgb(40, 52, 78), Rgb(65, 85, 120), Color.White, 13.5f))
                _historyPage = Math.Clamp(_historyPage + 1, 0, totalPages - 1);

            if (Engine.DrawButton(new Rectangle(lx + 14, ly + lh - 44, Math.Min(290, lw - 28), 36), "<- В ГЛАВНОЕ МЕНЮ [ESC]", Rgb(65, 72, 95), Rgb(95, 105, 135), Color.White, 14f))
            {
                _screen = "main";
                SelectCharacter(_selectedChar);
                return;
            }

            // Правая панель детального отчёта выбранного забега
            var sel = runs.Find(r => r.RunId == _selectedHistoryRunId);
            if (sel == null)
            {
                Engine.DrawTextCentered("Выберите забег слева для просмотра сводки и 3D-реплея", rx + rw * 0.5f, ry + rh * 0.5f, 15.5f, Color.Gray);
                return;
            }

            Color selCol = sel.Outcome switch
            {
                "victory" => Color.Gold,
                "abandoned" => Color.SkyBlue,
                _ => Rgb(245, 75, 75)
            };
            string selTitle = sel.Outcome switch
            {
                "victory" => "ПОБЕДА :: ЗАБЕГ УСПЕШНО ЗАВЕРШЁН",
                "abandoned" => "ЗАБЕГ ПРЕРВАН ДОСРОЧНО",
                _ => "ПОРАЖЕНИЕ :: ОТЧЁТ О ГИБЕЛИ"
            };
            if (sel.WasSpectatorOnly) selTitle += " (ЗРИТЕЛЬ)";

            int padX = rx + 20, innerW = rw - 40;
            Engine.DrawTextWrapped(selTitle, padX, ry + 14, innerW, 19f, selCol, 2f, maxLines: 1);
            string modeLabel = sel.GameMode switch
            {
                "coop" => "Кооператив",
                "pvp" => "PvP-Дуэль",
                _ => "Одиночная игра"
            };
            Engine.DrawTextWrapped($"Дата: {sel.PlayedAt}  |  Игрок: {sel.PlayerName}  |  Режим: {modeLabel}", padX, ry + 40, innerW, 13.5f, Rgb(180, 205, 235), 2f, maxLines: 1);

            var selSurv = SurvivorDatabase.Classes.GetValueOrDefault(sel.CharKey) ?? SurvivorDatabase.Classes["commando"];
            string selSkinName = selSurv.Skins.Count > 0 ? selSurv.Skins[Math.Clamp(sel.SkinIdx, 0, selSurv.Skins.Count - 1)].Name : "Базовый";
            var selDiff = GameData.Difficulties.GetValueOrDefault(sel.DifficultyKey) ?? GameData.Difficulties["rainstorm"];
            string artsTxt = sel.Artifacts.Count > 0
                ? string.Join(", ", System.Linq.Enumerable.Select(sel.Artifacts, ak => GameData.RunArtifacts.TryGetValue(ak, out var ad) ? ad.Name : ak))
                : "Без артефактов";
            Engine.DrawTextWrapped($"Герой: {selSurv.Name} (Облик: {selSkinName})", padX, ry + 62, innerW, 15f, selSurv.BodyColor, 2f, maxLines: 1);
            Engine.DrawTextWrapped($"Сложность: {selDiff.Name}  |  Артефакты: {artsTxt}", padX, ry + 84, innerW, 13f, selDiff.Color, 2f, maxLines: 1);

            int curY = ry + 106;
            if (sel.CoopPeers.Count > 0)
            {
                var peerParts = new List<string>(sel.CoopPeers.Count);
                foreach (var peer in sel.CoopPeers)
                {
                    string pcName = (SurvivorDatabase.Classes.GetValueOrDefault(peer.CharKey) ?? SurvivorDatabase.Classes["commando"]).Name;
                    peerParts.Add($"{peer.PlayerName} ({pcName})");
                }
                Engine.DrawTextWrapped($"Союзники / Соперники: {string.Join(", ", peerParts)}", padX, curY, innerW, 13f, Color.SkyBlue, 2f, maxLines: 1);
                curY += 20;
            }

            // Статистика боя
            Raylib.DrawRectangle(padX, curY, innerW, 102, Rgb(18, 24, 40, 235));
            int sm = (int)(sel.DurationSec / 60f), ss = (int)(sel.DurationSec % 60f);
            Engine.DrawTextWrapped($"Длительность: {sm:00}:{ss:00} (Этап {sel.FinalStageNum}: {sel.FinalStageName})", padX + 12, curY + 8, innerW - 24, 13.5f, Color.White, 1f, maxLines: 1);
            Engine.DrawTextWrapped($"Убито врагов: {sel.Kills} (Боссов: {sel.BossKills})  |  Нанесено урона: {(int)sel.DamageDealt}", padX + 12, curY + 30, innerW - 24, 13.5f, Color.Lime, 1f, maxLines: 1);
            Engine.DrawTextWrapped($"Получено урона: {(int)sel.DamageTaken} ({sel.HitsTaken} уд.)  |  Исцелено: {(int)sel.TotalHealed} HP", padX + 12, curY + 52, innerW - 24, 13.5f, Color.SkyBlue, 1f, maxLines: 1);
            Engine.DrawTextWrapped($"Золото: ${sel.GoldEarned}  |  Сундуков: {sel.ChestsOpened}  |  Навыков: {sel.AbilityCasts}",
                padX + 12, curY + 74, innerW - 24, 13f, Color.Gold, 1f, maxLines: 1);
            curY += 110;

            if (sel.Outcome == "defeat" && !string.IsNullOrWhiteSpace(sel.KillerName))
            {
                Raylib.DrawRectangle(padX, curY, innerW, 30, Rgb(46, 16, 22, 235));
                Raylib.DrawRectangle(padX, curY, 4, 30, Color.Red);
                string kTag = sel.KillerIsDoppel ? " [ТЕНЕВОЙ ДВОЙНИК]" : (sel.KillerIsBoss ? " [БОСС]" : (sel.KillerIsElite ? " [ЭЛИТНЫЙ]" : ""));
                Engine.DrawTextWrapped($"Причина гибели: {sel.KillerName}{kTag} ({sel.KillerRank}) — удар {(int)sel.KillerDamage} ед.",
                    padX + 12, curY + 7, innerW - 24, 13f, Rgb(255, 130, 130), 1f, maxLines: 1);
                curY += 36;
            }

            if (sel.StagesVisited.Count > 0)
            {
                var routeParts = new List<string>(sel.StagesVisited.Count);
                foreach (var st in sel.StagesVisited)
                {
                    int em = (int)(st.EnterTimeSec / 60f), es = (int)(st.EnterTimeSec % 60f);
                    routeParts.Add($"Э{st.StageNum} {st.StageName} ({em:00}:{es:00})");
                }
                string routeStr = "Маршрут: " + string.Join(" -> ", routeParts);
                Engine.DrawTextWrapped(routeStr, padX, curY, innerW, 12.5f, Rgb(185, 220, 250), 2f, maxLines: 2);
                curY += 34;
            }

            // Собранные предметы в виде иконок с всплывающим окном статов при наведении
            Engine.DrawTextLeft($"СОБРАННЫЙ ИНВЕНТАРЬ ({sel.TotalItemsCount} предм. — наведите на иконку):", padX, curY, 13.5f, Color.Yellow);
            curY += 20;

            Data.ItemInfo? hoveredHistItem = null;
            int hoveredHistCount = 1;
            int bottomBtnY = ry + rh - 52;
            int availInvH = Math.Max(48, bottomBtnY - 10 - curY);

            if (sel.Items.Count == 0)
            {
                Engine.DrawTextLeft("Предметы за забег не собирались.", padX, curY + 6, 13.5f, Color.Gray);
            }
            else
            {
                int cellW = 56, cellH = 44, iGap = 6;
                int cols = Math.Max(6, (innerW + iGap) / (cellW + iGap));
                int rows = Math.Max(1, (availInvH + iGap) / (cellH + iGap));
                int maxShow = Math.Min(sel.Items.Count, cols * rows);
                for (int i = 0; i < maxShow; i++)
                {
                    var ir = sel.Items[i];
                    if (!GameData.ItemPool.TryGetValue(ir.Key, out var info)) continue;
                    int row = i / cols, col = i % cols;
                    Rectangle ib = new(padX + col * (cellW + iGap), curY + row * (cellH + iGap), cellW, cellH);
                    if (DrawItemIconCell(ib, ir.Key, info, ir.Count, mp, compactHud: false))
                    {
                        hoveredHistItem = info;
                        hoveredHistCount = ir.Count;
                    }
                }
            }

            bool selHasReplay = Data.RunHistoryStorage.HasReplayFile(sel);
            int delBtnW = Math.Min(195, innerW * 32 / 100);
            int playBtnW = innerW - delBtnW - 12;
            Rectangle playBtnRect = new(padX, bottomBtnY, playBtnW, 40);
            if (selHasReplay)
            {
                if (Engine.DrawButton(playBtnRect, "▶ СМОТРЕТЬ 3D-РЕПЛЕЙ ЗАБЕГА [ENTER]",
                    Rgb(32, 112, 72), Rgb(48, 155, 98), Color.White, playBtnW < 340 ? 13.5f : 15.5f, Color.Lime)
                    || Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    SoundManager.Play("ui_click");
                    StartReplayPlayback(sel.ReplayFile, "logbook");
                    return;
                }
            }
            else
            {
                Engine.DrawButton(playBtnRect, "3D-РЕПЛЕЙ НЕ СОХРАНЁН ДЛЯ ЭТОГО МАТЧА",
                    Rgb(28, 32, 42), Rgb(28, 32, 42), Color.Gray, 12.5f);
            }

            if (Engine.DrawButton(new Rectangle(padX + playBtnW + 12, bottomBtnY, delBtnW, 40), "УДАЛИТЬ ЗАПИСЬ",
                Rgb(115, 34, 42), Rgb(160, 50, 60), Color.White, 13.5f))
            {
                SoundManager.Play("ui_click");
                Data.RunHistoryStorage.DeleteEntry(sel.RunId);
                _selectedHistoryRunId = Data.RunHistoryStorage.Entries.Count > 0 ? Data.RunHistoryStorage.Entries[0].RunId : "";
            }

            if (hoveredHistItem != null)
            {
                DrawClampedItemTooltip(hoveredHistItem, hoveredHistCount, mp);
            }
        }

        private static void DrawRunSummaryUI()
        {
            DrawHeader();
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            Color titleCol = _lastRunVictory ? Color.Gold : Rgb(245, 65, 65);
            string titleStr = _lastRunVictory ? "ПОБЕДА :: ЗАБЕГ УСПЕШНО ЗАВЕРШЁН!" : "ПОРАЖЕНИЕ :: ОТЧЁТ О ЗАБЕГЕ";

            int killerWidgetW = Math.Clamp((int)(sw * 0.29f), 330, 444);
            int maxLeftW = !_lastRunVictory ? Math.Max(450, sw - killerWidgetW - 76) : 680;
            int px = 36, py = 92;
            int pw = Math.Clamp((int)(sw * 0.46f), 450, Math.Min(680, maxLeftW));
            int ph = Math.Clamp(sh - py - 18, 468, 688);
            int innerX = px + 24, innerW = pw - 48;

            Raylib.DrawRectangle(px, py, pw, ph, Rgb(12, 16, 28, 242));
            Raylib.DrawRectangle(px, py, pw, 4, titleCol);
            Engine.DrawTextWrapped(titleStr, innerX, py + 18, innerW, pw < 540 ? 21f : 25f, titleCol, 2f, maxLines: 1);
            if (!_lastRunVictory)
            {
                DrawKillerMobTopRightWidget(sw - killerWidgetW - 24, 74, killerWidgetW, allow3DToggle: true);
            }

            var cfg = SurvivorDatabase.Classes[_selectedChar];
            int mins = (int)(_lastRunTime / 60f), secs = (int)(_lastRunTime % 60f);
            Engine.DrawTextWrapped($"Выживший: {cfg.Name} ({cfg.Subtitle})", innerX, py + 58, innerW, 18f, cfg.BodyColor, 2f, maxLines: 1);
            Engine.DrawTextWrapped($"Время выживания: {mins:00}:{secs:00}   |   Пройдено этапов: {_lastRunStage}", innerX, py + 90, innerW, 16.5f, Color.White, 2f, maxLines: 1);
            Engine.DrawTextLeft($"Уничтожено врагов: {RunKills}", innerX, py + 120, 16.5f, Color.Lime);
            Engine.DrawTextLeft($"Всего нанесено урона: {(int)RunDamageDealt}", innerX, py + 148, 16.5f, Color.SkyBlue);
            Engine.DrawTextLeft($"Собрано золота за забег: ${RunGoldEarned}", innerX, py + 176, 16.5f, Color.Gold);

            int invTitleY = py + 214;
            Engine.DrawTextLeft("СОБРАННЫЕ АРТЕФАКТЫ (наведите курсор для статов):", innerX, invTitleY, 16f, Color.Yellow);

            Vector2 mp = Engine.GetUiMousePosition();
            Data.ItemInfo? hoveredSummaryItem = null;
            int hoveredSummaryCount = 1;

            int row1Y = py + ph - 114;
            int row2Y = py + ph - 60;
            int invStartY = invTitleY + 26;
            int availInvH = Math.Max(52, row1Y - 12 - invStartY);

            if (_lastRunItems.Count == 0)
            {
                Engine.DrawTextLeft("Предметы не были собраны", innerX, invStartY + 8, 15f, Color.Gray);
            }
            else
            {
                int cellW = 58, cellH = 46, gap = 8;
                int cols = Math.Max(5, (innerW + gap) / (cellW + gap));
                int rows = Math.Max(1, (availInvH + gap) / (cellH + gap));
                int maxItems = Math.Min(_lastRunItems.Count, cols * rows);
                for (int i = 0; i < maxItems; i++)
                {
                    var (k, cnt) = _lastRunItems[i];
                    if (!GameData.ItemPool.TryGetValue(k, out var info)) continue;
                    int r = i / cols, c = i % cols;
                    Rectangle box = new(innerX + c * (cellW + gap), invStartY + r * (cellH + gap), cellW, cellH);
                    if (DrawItemIconCell(box, k, info, cnt, mp, compactHud: false))
                    {
                        hoveredSummaryItem = info;
                        hoveredSummaryCount = cnt;
                    }
                }
            }

            bool hasLatestReplay = !string.IsNullOrWhiteSpace(_lastCompletedReplayPath)
                && System.IO.File.Exists(Data.RunHistoryStorage.GetFullReplayPath(_lastCompletedReplayPath));
            bool canSavePendingReplay = !hasLatestReplay && Data.ReplayRecorder.CanSavePendingReplay;

            if (hasLatestReplay)
            {
                int playW = (innerW - 12) * 60 / 100;
                int histW = innerW - playW - 12;
                if (Engine.DrawButton(new Rectangle(innerX, row1Y, playW, 44), "▶ СМОТРЕТЬ 3D-РЕПЛЕЙ ЗАБЕГА",
                    Rgb(28, 92, 138), Rgb(46, 128, 190), Color.Yellow, playW < 300 ? 13.5f : 15.5f, Color.SkyBlue))
                {
                    SoundManager.Play("ui_click");
                    StartReplayPlayback(_lastCompletedReplayPath, "run_summary");
                    return;
                }
                if (Engine.DrawButton(new Rectangle(innerX + playW + 12, row1Y, histW, 44),
                    "ИСТОРИЯ ЗАБЕГОВ", Rgb(44, 58, 92), Rgb(66, 88, 136), Color.White, 14.5f, Color.Gold))
                {
                    SoundManager.Play("ui_click");
                    _logDetailOpen = false;
                    _logCategory = "history";
                    _historyPage = 0;
                    _screen = "logbook";
                    return;
                }
            }
            else if (canSavePendingReplay)
            {
                int saveW = (innerW - 12) * 62 / 100;
                int histW = innerW - saveW - 12;
                if (Engine.DrawButton(new Rectangle(innerX, row1Y, saveW, 44), "СОХРАНИТЬ 3D-РЕПЛЕЙ МАТЧА",
                    Rgb(28, 108, 72), Rgb(44, 148, 96), Color.White, saveW < 300 ? 13.5f : 15f, Color.Lime))
                {
                    string savedRel = Data.ReplayRecorder.SavePendingReplayNow();
                    if (!string.IsNullOrWhiteSpace(savedRel))
                    {
                        _lastCompletedReplayPath = savedRel;
                        SoundManager.Play("ui_ready");
                        ShowBanner("3D-РЕПЛЕЙ ЗАБЕГА СОХРАНЁН НА ДИСК!", Color.Lime, 3.2f);
                    }
                    else
                    {
                        SoundManager.Play("ui_error");
                    }
                }
                if (Engine.DrawButton(new Rectangle(innerX + saveW + 12, row1Y, histW, 44),
                    "ИСТОРИЯ ЗАБЕГОВ", Rgb(44, 58, 92), Rgb(66, 88, 136), Color.White, 14.5f, Color.Gold))
                {
                    SoundManager.Play("ui_click");
                    _logDetailOpen = false;
                    _logCategory = "history";
                    _historyPage = 0;
                    _screen = "logbook";
                    return;
                }
            }
            else
            {
                if (Engine.DrawButton(new Rectangle(innerX, row1Y, innerW, 44),
                    "ИСТОРИЯ ЗАБЕГОВ", Rgb(44, 58, 92), Rgb(66, 88, 136), Color.White, 15.5f, Color.Gold))
                {
                    SoundManager.Play("ui_click");
                    _logDetailOpen = false;
                    _logCategory = "history";
                    _historyPage = 0;
                    _screen = "logbook";
                    return;
                }
            }

            int lobbyBtnW = (innerW - 14) * 53 / 100;
            int mainBtnW = innerW - lobbyBtnW - 14;
            if (Engine.DrawButton(new Rectangle(innerX, row2Y, lobbyBtnW, 48), "В ЛОББИ ВЫЖИВШИХ [ENTER]",
                Rgb(38, 115, 70), Rgb(55, 160, 95), Color.White, lobbyBtnW < 270 ? 14.5f : 16.5f, Color.Lime)
                || Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                _screen = "lobby";
                SelectCharacter(_selectedChar);
            }
            if (Engine.DrawButton(new Rectangle(innerX + lobbyBtnW + 14, row2Y, mainBtnW, 48), "В ГЛАВНОЕ МЕНЮ",
                Rgb(65, 72, 95), Rgb(95, 105, 135), Color.White, mainBtnW < 230 ? 14.5f : 16.5f))
            {
                _net.StopSession();
                _lobbyPlayers.Clear();
                _screen = "main";
            }

            if (hoveredSummaryItem != null)
            {
                DrawClampedItemTooltip(hoveredSummaryItem, hoveredSummaryCount, mp);
            }
        }

        private struct LogEntryView
        {
            public string Key, Short, Title, Sub, Desc;
            public Color Color;
            public bool Unlocked;
        }

        private static System.Collections.Generic.List<LogEntryView> GetLogEntries(string cat)
        {
            var list = new System.Collections.Generic.List<LogEntryView>();
            var prog = Data.ProgressData.Instance;

            if (cat == "items")
            {
                foreach (var kv in Data.GameData.ItemPool)
                {
                    if (kv.Key == "tonic_affliction") continue;
                    var v = kv.Value;
                    bool unl = prog.UnlockedItems.Contains(kv.Key);
                    var itemAch = Data.GameData.GetItemUnlockAchievement(kv.Key);
                    string lockedHow = itemAch != null
                        ? $"{v.Desc}\nКак открыть: «{itemAch.Name}» — {itemAch.Desc}"
                        : v.Desc;
                    list.Add(new LogEntryView
                    {
                        Key = kv.Key, Short = v.Short, Color = v.Color,
                        Unlocked = unl,
                        Title = unl ? v.Name : $"{v.Name} [ЗАКРЫТО]",
                        Sub = unl ? $"Редкость: {v.TierName}  |  Код: [{v.Short}]  |  ОТКРЫТО ✓" : $"Редкость: {v.TierName}  |  Статус: НЕ ОТКРЫТО",
                        Desc = unl ? v.Desc : lockedHow
                    });
                }
            }
            else if (cat == "survivors")
            {
                foreach (var k in SurvivorDatabase.GetVisibleRosterKeys())
                {
                    var v = SurvivorDatabase.Classes[k];
                    bool isUnl = prog.IsCharacterUnlocked(k);
                    var charAch = Data.GameData.GetCharacterUnlockAchievement(k);
                    string howTxt = charAch != null ? $"Испытание: {charAch.Desc}\nНаграда: {charAch.RewardText}" : "Секретный персонаж корабля Safe Travels.";
                    list.Add(new LogEntryView
                    {
                        Key = k, Short = v.Icon, Color = v.BodyColor, Unlocked = isUnl,
                        Title = isUnl ? $"{v.Name} — {v.Subtitle}" : $"{v.Name} [ЗАКРЫТО]",
                        Sub = isUnl
                            ? $"HP: {v.Hp}  |  Скорость: {v.Speed}  |  Урон: {v.Damage}  |  Атака: {v.FireRate}/с"
                            : $"Статус: НЕ ОТКРЫТ  |  {v.Subtitle}",
                        Desc = isUnl
                            ? $"{v.Skills["m1"].Name} / {v.Skills["m2"].Name} / {v.Skills["shift"].Name} / {v.Skills["r"].Name}"
                            : howTxt
                    });
                }
            }
            else if (cat is "monsters" or "bosses")
            {
                foreach (var kv in Data.GameData.MonsterLog)
                {
                    bool isBoss = Data.GameData.IsKnownBossType(kv.Key);
                    if ((cat == "bosses") != isBoss) continue;
                    var m = kv.Value;
                    bool isTrialBoss = Array.IndexOf(Data.GameData.TrialBossTypes, kv.Key) >= 0;
                    string rankTag = kv.Key == "mithrix" ? " [ФИНАЛЬНЫЙ БОСС]" : (isTrialBoss ? " [БОСС ИСПЫТАНИЯ]" : (isBoss ? " [СТРАЖ ТЕЛЕПОРТА]" : ""));
                    list.Add(new LogEntryView
                    {
                        Key = kv.Key, Short = kv.Key[..Math.Min(5, kv.Key.Length)].ToUpper(),
                        Color = kv.Key == "mithrix" ? Color.SkyBlue : (isBoss ? Color.Red : Rgb(255, 140, 60)),
                        Unlocked = true, Title = m.Name,
                        Sub = $"Базовое HP: {m.Hp}   |   Базовый урон: {m.Damage}{rankTag}", Desc = m.Desc
                    });
                }
            }
            else if (cat == "stages")
            {
                var all = new System.Collections.Generic.List<Data.StagePalette>(Data.GameData.StagePalettes);
                foreach (var spKey in new[] { "safe_travels", "bazaar", "moon", "pizzeria", "black_mesa", "qliphoth", "hell_cybergrind", "tartarus", "hakurei_shrine", "tracen_derby", "dark_sanctuary", "terminal_lucidity", "extraction_floor", "originium_core", "penacony_theater", "prime_meridian" })
                {
                    if (Data.GameData.SpecialPalettes.TryGetValue(spKey, out var spPal))
                        all.Add(spPal);
                }
                foreach (var st in all)
                {
                    bool disc = prog.DiscoveredStages.Contains(st.Id) || (st.Id == "safe_travels" && IsSafeTravelsUnlocked());
                    int layerNum = Data.GameData.GetStageBiomeLayerNumber(st.Id);
                    string poolTag = layerNum > 0 ? $"Пул Этапа {layerNum}" : "Внецикловая / Особая";
                    list.Add(new LogEntryView
                    {
                        Key = st.Id, Short = st.Id[..Math.Min(5, st.Id.Length)].ToUpper(),
                        Color = st.Accent, Unlocked = disc,
                        Title = st.Name,
                        Sub = $"{poolTag}  |  Статус: {(disc ? "ИССЛЕДОВАНА ✓" : "ЕЩЁ НЕ ПОСЕЩЕНА")}",
                        Desc = st.Desc
                    });
                }
            }
            else if (cat == "achievements")
            {
                foreach (var kv in Data.GameData.Achievements)
                {
                    bool unl = prog.UnlockedAchievements.Contains(kv.Key);
                    string rewSummary = GetAchievementRewardSummary(kv.Value, _selectedChar);
                    list.Add(new LogEntryView
                    {
                        Key = kv.Key, Short = "ACH", Color = unl ? Color.Lime : Color.Orange,
                        Unlocked = unl, Title = kv.Value.Name,
                        Sub = $"{(unl ? "ВЫПОЛНЕНО ✓" : "НЕ ВЫПОЛНЕНО")}  |  Награда: {rewSummary}",
                        Desc = $"Как получить: {kv.Value.Desc}\nНаграда: {rewSummary}"
                    });
                }
            }
            else if (cat == "modifiers")
            {
                foreach (var dk in Data.GameData.OrderedDifficultyKeys)
                {
                    var d = Data.GameData.Difficulties[dk];
                    list.Add(new LogEntryView
                    {
                        Key = dk, Short = d.Icon, Color = d.Color, Unlocked = true,
                        Title = $"СЛОЖНОСТЬ: {d.Name}",
                        Sub = $"{d.Subtitle}  |  Множитель роста сложности: x{d.ScaleMult:F2}",
                        Desc = d.Desc
                    });
                }
                foreach (var ak in Data.GameData.OrderedArtifactKeys)
                {
                    var a = Data.GameData.RunArtifacts[ak];
                    bool isOn = Data.GameData.HasArtifact(ak);
                    list.Add(new LogEntryView
                    {
                        Key = ak, Short = a.Short, Color = a.Color, Unlocked = true,
                        Title = a.Name,
                        Sub = $"{a.Subtitle}  |  В лобби: {(isOn ? "ВКЛЮЧЁН ✓" : "ВЫКЛЮЧЕН")}",
                        Desc = a.Desc
                    });
                }
            }
            return list;
        }
    }
}