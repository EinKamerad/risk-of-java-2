using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Entities;
using RiskOfCSharp2.Models;
using RiskOfCSharp2.World;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Data
{
    // =================================================================================================
    // 1. МОДЕЛИ И ХРАНИЛИЩЕ ИСТОРИИ (ЛОГА) ПРОШЛЫХ ЗАБЕГОВ (run_history.json)
    // =================================================================================================
    public class RunStageRecord
    {
        public int StageNum { get; set; }
        public string StageName { get; set; } = "";
        public string? SpecialMode { get; set; }
        public int Seed { get; set; }
        public float EnterTimeSec { get; set; }
    }

    public class RunItemRecord
    {
        public string Key { get; set; } = "";
        public int Count { get; set; }
    }

    public class RunCoopPeerRecord
    {
        public string PlayerId { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public string CharKey { get; set; } = "commando";
        public int SkinIdx { get; set; }
    }

    public class RunLogEntry
    {
        public string RunId { get; set; } = "";
        public string PlayedAt { get; set; } = "";
        public string PlayerName { get; set; } = "Survivor";
        public string CharKey { get; set; } = "commando";
        public int SkinIdx { get; set; }
        public int[] SkillVariants { get; set; } = new int[4];
        public string DifficultyKey { get; set; } = "rainstorm";
        public List<string> Artifacts { get; set; } = new();
        public string GameMode { get; set; } = "solo"; // "solo", "coop", "pvp"
        public string Outcome { get; set; } = "defeat"; // "victory", "defeat", "abandoned"
        public bool WasSpectatorOnly { get; set; }

        public float DurationSec { get; set; }
        public int FinalStageNum { get; set; } = 1;
        public string FinalStageName { get; set; } = "";
        public List<RunStageRecord> StagesVisited { get; set; } = new();
        public List<RunCoopPeerRecord> CoopPeers { get; set; } = new();

        public int Kills { get; set; }
        public int BossKills { get; set; }
        public float DamageDealt { get; set; }
        public float DamageTaken { get; set; }
        public int HitsTaken { get; set; }
        public float TotalHealed { get; set; }
        public int GoldEarned { get; set; }
        public int ChestsOpened { get; set; }
        public int AbilityCasts { get; set; }
        public int TotalItemsCount { get; set; }
        public List<RunItemRecord> Items { get; set; } = new();

        public string KillerKey { get; set; } = "";
        public string KillerName { get; set; } = "";
        public string KillerRank { get; set; } = "";
        public float KillerDamage { get; set; }
        public bool KillerIsBoss { get; set; }
        public bool KillerIsElite { get; set; }
        public bool KillerIsDoppel { get; set; }

        public string ReplayFile { get; set; } = "";
    }

    public static class RunHistoryStorage
    {
        private const int MaxHistoryEntries = 100;
        private const int MaxReplayFiles = 35;

        private static readonly string HistoryPath = Path.Combine(ProgressData.AppRootDir, "run_history.json");
        public static readonly string ReplaysDir = Path.Combine(ProgressData.AppRootDir, "replays");

        private static List<RunLogEntry>? _entries;
        public static List<RunLogEntry> Entries
        {
            get
            {
                if (_entries == null) Load();
                return _entries!;
            }
        }

        public static void Load()
        {
            _entries = new List<RunLogEntry>();
            if (!File.Exists(HistoryPath)) return;
            try
            {
                string json = File.ReadAllText(HistoryPath);
                var loaded = JsonSerializer.Deserialize<List<RunLogEntry>>(json);
                if (loaded != null)
                    _entries = loaded;
            }
            catch
            {
                // Повреждённый лог не должен мешать запуску игры
            }
        }

        public static void Save()
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(HistoryPath, JsonSerializer.Serialize(Entries, opts));
            }
            catch
            {
            }
        }

        public static void AddEntry(RunLogEntry entry)
        {
            Entries.RemoveAll(e => e.RunId == entry.RunId);
            Entries.Insert(0, entry);
            if (Entries.Count > MaxHistoryEntries)
            {
                for (int i = MaxHistoryEntries; i < Entries.Count; i++)
                    TryDeleteReplayDiskFile(Entries[i].ReplayFile);
                Entries.RemoveRange(MaxHistoryEntries, Entries.Count - MaxHistoryEntries);
            }
            PruneOldReplayFiles();
            Save();
        }

        public static void DeleteEntry(string runId)
        {
            var found = Entries.Find(e => e.RunId == runId);
            if (found != null)
            {
                TryDeleteReplayDiskFile(found.ReplayFile);
                Entries.Remove(found);
                Save();
            }
        }

        public static bool HasReplayFile(RunLogEntry? entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ReplayFile)) return false;
            string full = GetFullReplayPath(entry.ReplayFile);
            return File.Exists(full);
        }

        public static string GetFullReplayPath(string relativeOrFull)
        {
            if (string.IsNullOrWhiteSpace(relativeOrFull)) return "";
            return Path.IsPathRooted(relativeOrFull)
                ? relativeOrFull
                : Path.Combine(ProgressData.AppRootDir, relativeOrFull);
        }

        private static void TryDeleteReplayDiskFile(string relPath)
        {
            if (string.IsNullOrWhiteSpace(relPath)) return;
            try
            {
                string full = GetFullReplayPath(relPath);
                if (File.Exists(full)) File.Delete(full);
            }
            catch
            {
            }
        }

        private static void PruneOldReplayFiles()
        {
            try
            {
                if (!Directory.Exists(ReplaysDir)) return;
                int kept = 0;
                foreach (var e in Entries)
                {
                    if (string.IsNullOrWhiteSpace(e.ReplayFile)) continue;
                    string full = GetFullReplayPath(e.ReplayFile);
                    if (!File.Exists(full))
                    {
                        e.ReplayFile = "";
                        continue;
                    }
                    kept++;
                    if (kept > MaxReplayFiles)
                    {
                        TryDeleteReplayDiskFile(e.ReplayFile);
                        e.ReplayFile = "";
                    }
                }
            }
            catch
            {
            }
        }
    }

    // =================================================================================================
    // 2. СТРУКТУРЫ ДАННЫХ 3D-РЕПЛЕЯ (.rpl.gz) И ЗАПИСЫВАЮЩИЙ МОДУЛЬ (ReplayRecorder)
    // =================================================================================================
    public class ReplayData
    {
        public string RunId { get; set; } = "";
        public string PlayedAt { get; set; } = "";
        public string PlayerName { get; set; } = "Survivor";
        public string CharKey { get; set; } = "commando";
        public int SkinIdx { get; set; }
        public string DifficultyKey { get; set; } = "rainstorm";
        public List<string> Artifacts { get; set; } = new();
        public string GameMode { get; set; } = "solo";
        public string Outcome { get; set; } = "defeat";
        public float DurationSec { get; set; }
        public string KillerName { get; set; } = "";

        public List<ReplayStageInfo> Stages { get; set; } = new();
        public List<ReplayFrame> Frames { get; set; } = new();
    }

    public class ReplayStageInfo
    {
        public int StageIdx { get; set; }
        public float EnterTime { get; set; }
        public int StageNum { get; set; }
        public int Seed { get; set; }
        public string? SpecialMode { get; set; }
        public string StageName { get; set; } = "";
    }

    public class ReplayActorFrame
    {
        public string Id { get; set; } = "local";
        public string Name { get; set; } = "";
        public string CharKey { get; set; } = "commando";
        public int SkinIdx { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotY { get; set; }
        public float Pitch { get; set; }
        public float Hp { get; set; }
        public float MaxHp { get; set; }
        public float Shield { get; set; }
        public int Gold { get; set; }
        public int Lunar { get; set; }
        public bool Moving { get; set; }
        public bool Grounded { get; set; } = true;
        public float VelY { get; set; }
        public bool ShieldStance { get; set; }
        public bool UltForm { get; set; }
        public bool Visible { get; set; } = true;
        public string Emote { get; set; } = "";
        public string AnimSlot { get; set; } = "";
        public int AnimStep { get; set; }
        public float[]? Cds { get; set; }
    }

    public class ReplayEnemyFrame
    {
        public int Id { get; set; }
        public string Et { get; set; } = "lemurian";
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotY { get; set; }
        public float Hp { get; set; }
        public float MaxHp { get; set; }
        public bool Boss { get; set; }
        public bool Elite { get; set; }
        public bool Ally { get; set; }
        public bool Doppel { get; set; }
        public int Phase { get; set; } = 1;
    }

    public class ReplayProjFrame
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float S { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    public class ReplayFrame
    {
        public float T { get; set; }
        public int StageIdx { get; set; }
        public float DiffCoeff { get; set; } = 1f;
        public string TpState { get; set; } = "";
        public float TpCharge { get; set; }
        public bool TpAligned { get; set; }
        public float MoonDetTimer { get; set; }
        public string BossTitle { get; set; } = "";
        public float BossHp { get; set; }
        public float BossMaxHp { get; set; }
        public bool BossIsMithrix { get; set; }
        public int BossPhase { get; set; } = 1;
        public int Kills { get; set; }
        public float DmgDealt { get; set; }
        public string? ItemsCompact { get; set; }

        public ReplayActorFrame Local { get; set; } = new();
        public List<ReplayActorFrame>? Remotes { get; set; }
        public List<ReplayEnemyFrame>? Enemies { get; set; }
        public List<ReplayProjFrame>? Projs { get; set; }
        public int[]? OpenedChests { get; set; }
        public string[]? DonePillars { get; set; }
    }

    public static class ReplayRecorder
    {
        private const float SampleInterval = 0.10f; // 10 Гц + плавная интерполяция при просмотре (120 FPS)
        private const int MaxFrames = 36000;        // До 60 минут полного 3D-забега

        private static ReplayData? _active;
        private static ReplayData? _lastPendingReplay;
        private static string _lastPendingEntryRunId = "";
        private static float _sampleAccum = 0f;
        private static string _lastItemsSig = "";
        private static float _damageTakenAccum = 0f;
        private static bool _runLogged = false;
        private static readonly Dictionary<string, RunCoopPeerRecord> _seenPeers = new();

        public static bool IsRecording => _active != null && !_runLogged;
        public static string CurrentRunId => _active?.RunId ?? "";
        public static bool CanSavePendingReplay => _lastPendingReplay != null
            && _lastPendingReplay.Stages.Count > 0
            && _lastPendingReplay.Frames.Count > 0
            && !string.IsNullOrEmpty(_lastPendingEntryRunId);

        public static string SavePendingReplayNow()
        {
            if (!CanSavePendingReplay || _lastPendingReplay == null) return "";
            try
            {
                Directory.CreateDirectory(RunHistoryStorage.ReplaysDir);
                string relReplay = Path.Combine("replays", $"{_lastPendingReplay.RunId}.rpl.gz");
                string fullReplay = RunHistoryStorage.GetFullReplayPath(relReplay);
                using (var fs = File.Create(fullReplay))
                using (var gz = new GZipStream(fs, CompressionLevel.Fastest))
                {
                    JsonSerializer.Serialize(gz, _lastPendingReplay);
                }

                var entry = RunHistoryStorage.Entries.Find(e => e.RunId == _lastPendingEntryRunId);
                if (entry != null)
                {
                    entry.ReplayFile = relReplay;
                    RunHistoryStorage.Save();
                }

                _lastPendingReplay = null;
                _lastPendingEntryRunId = "";
                return relReplay;
            }
            catch
            {
                return "";
            }
        }

        public static void BeginRun(string charKey, string gameMode, bool isSpectatorOnly)
        {
            _lastPendingReplay = null;
            _lastPendingEntryRunId = "";

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string randSuffix = Random.Shared.Next(1000, 9999).ToString();
            string runId = $"{stamp}_{randSuffix}";

            _active = new ReplayData
            {
                RunId = runId,
                PlayedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                PlayerName = string.IsNullOrWhiteSpace(ProgressData.Instance.PlayerName) ? "Survivor" : ProgressData.Instance.PlayerName,
                CharKey = charKey,
                SkinIdx = ProgressData.Instance.GetSelectedSkin(charKey),
                DifficultyKey = GameData.EffectiveDifficultyKey,
                Artifacts = GameData.EffectiveArtifacts.OrderBy(x => x).ToList(),
                GameMode = gameMode,
                Outcome = "abandoned"
            };
            _sampleAccum = SampleInterval; // Сразу записать стартовый кадр
            _lastItemsSig = "";
            _damageTakenAccum = 0f;
            _runLogged = false;
            _seenPeers.Clear();
        }

        public static void CancelWithoutSaving()
        {
            _active = null;
            _runLogged = true;
            _seenPeers.Clear();
        }

        public static void AddDamageTaken(float dmg)
        {
            if (dmg > 0f && _active != null && !_runLogged)
                _damageTakenAccum += dmg;
        }

        public static void RecordStage(int stageNum, int seed, string? specialMode, string stageName, float elapsedSec)
        {
            if (_active == null || _runLogged) return;
            if (specialMode == "safe_travels")
            {
                CancelWithoutSaving();
                return;
            }
            _active.Stages.Add(new ReplayStageInfo
            {
                StageIdx = _active.Stages.Count,
                EnterTime = MathF.Round(elapsedSec, 2),
                StageNum = stageNum,
                Seed = seed,
                SpecialMode = specialMode,
                StageName = stageName
            });
            _sampleAccum = SampleInterval; // Мгновенный сэмпл при входе на этап
        }

        public static void Tick(
            float dt,
            GameRunContext run,
            IReadOnlyDictionary<string, RemotePlayer> remotePlayers,
            bool localIsSpectating)
        {
            if (_active == null || _runLogged || run.IsSafeTravels) return;
            if (_active.Frames.Count >= MaxFrames) return;

            foreach (var kv in remotePlayers)
            {
                var rp = kv.Value;
                if (!IsValid(rp)) continue;
                _seenPeers[kv.Key] = new RunCoopPeerRecord
                {
                    PlayerId = kv.Key,
                    PlayerName = string.IsNullOrWhiteSpace(rp.PlayerName) ? kv.Key.ToUpper() : rp.PlayerName,
                    CharKey = rp.CharKey,
                    SkinIdx = rp.SkinIdx
                };
            }

            if (!UserSettings.Instance.ShouldCaptureReplayFrames()) return;

            _sampleAccum += dt;
            if (_sampleAccum < SampleInterval) return;
            _sampleAccum = 0f;

            int curStageIdx = Math.Max(0, _active.Stages.Count - 1);
            var p = run.PlayerEntity;
            var c = run.Combat;

            string curItemsSig = BuildCompactItems(run);
            string? itemsDelta = curItemsSig != _lastItemsSig ? curItemsSig : null;
            _lastItemsSig = curItemsSig;

            string tpState = "";
            float tpCharge = 0f;
            bool tpAligned = false;
            if (run.ActiveTeleporterOrPad is Teleporter tp && IsValid(tp))
            {
                tpState = tp.State;
                tpCharge = MathF.Round(tp.Charge, 1);
                tpAligned = tp.AlignedToMoon;
            }

            string bTitle = "";
            float bHp = 0f, bMax = 0f;
            bool bMithrix = false;
            int bPhase = 1;
            if (IsValid(run.ActiveBoss) && !run.ActiveBoss!.IsAlly)
            {
                var boss = run.ActiveBoss!;
                bMithrix = boss.EnemyType == "mithrix";
                bPhase = boss.Phase;
                bTitle = bMithrix ? $"МИТРИКС [ФАЗА {boss.Phase}/4] :: КОРОЛЬ НИЧЕГО" : run.BossTitle;
                bHp = MathF.Round(boss.Hp, 1);
                bMax = MathF.Round(boss.MaxHp, 1);
            }
            else if (run.IsHordeActive)
            {
                float curH = 0f;
                int aliveH = 0;
                foreach (var hb in run.ActiveHordeBosses)
                {
                    if (IsValid(hb) && hb.Hp > 0f && !hb.IsAlly)
                    {
                        curH += hb.Hp;
                        aliveH++;
                    }
                }
                bTitle = $"{run.HordeTitle} [{aliveH}/{run.ActiveHordeBosses.Count}]";
                bHp = MathF.Round(curH, 1);
                bMax = MathF.Round(run.HordeTotalMaxHp, 1);
            }

            bool movingNow = run.DashTimer > 0f
                || Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsKeyDown(KeyboardKey.A)
                || Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsKeyDown(KeyboardKey.D);

            var localFrame = new ReplayActorFrame
            {
                Id = "local",
                Name = _active.PlayerName,
                CharKey = run.CharKey,
                SkinIdx = ProgressData.Instance.GetSelectedSkin(run.CharKey),
                X = MathF.Round(p.X, 2),
                Y = MathF.Round(p.Y, 2),
                Z = MathF.Round(p.Z, 2),
                RotY = MathF.Round(p.RotationY, 1),
                Pitch = MathF.Round(run.CamPitch, 1),
                Hp = MathF.Round(MathF.Max(0f, run.PlayerHp), 1),
                MaxHp = MathF.Round(run.PlayerMaxHp, 1),
                Shield = MathF.Round(c.Shield, 1),
                Gold = run.Gold,
                Lunar = run.LunarCoins,
                Moving = !localIsSpectating && movingNow,
                Grounded = run.PlayerGrounded,
                VelY = MathF.Round(run.PlayerVelY, 1),
                ShieldStance = c.ShieldStance,
                UltForm = run.Parts.UltFormActive,
                Visible = !localIsSpectating && run.PlayerHp > 0f,
                Emote = localIsSpectating ? "" : run.Parts.ActiveEmote,
                AnimSlot = run.Parts.ActiveAnim,
                AnimStep = run.Parts.AnimStep,
                Cds = new[]
                {
                    MathF.Round(MathF.Max(0f, c.M1Cd), 2),
                    MathF.Round(MathF.Max(0f, c.M2Cd), 2),
                    MathF.Round(MathF.Max(0f, c.ShiftCd), 2),
                    MathF.Round(MathF.Max(0f, c.RCd), 2)
                }
            };

            List<ReplayActorFrame>? remList = null;
            if (remotePlayers.Count > 0)
            {
                remList = new List<ReplayActorFrame>(remotePlayers.Count);
                foreach (var kv in remotePlayers)
                {
                    var rp = kv.Value;
                    if (!IsValid(rp)) continue;
                    remList.Add(new ReplayActorFrame
                    {
                        Id = kv.Key,
                        Name = string.IsNullOrWhiteSpace(rp.PlayerName) ? kv.Key.ToUpper() : rp.PlayerName,
                        CharKey = rp.CharKey,
                        SkinIdx = rp.SkinIdx,
                        X = MathF.Round(rp.X, 2),
                        Y = MathF.Round(rp.Y, 2),
                        Z = MathF.Round(rp.Z, 2),
                        RotY = MathF.Round(rp.RotationY, 1),
                        Hp = MathF.Round(MathF.Max(0f, rp.Hp), 1),
                        MaxHp = MathF.Round(rp.MaxHp, 1),
                        Shield = MathF.Round(rp.Shield, 1),
                        Gold = rp.Gold,
                        Lunar = rp.LunarCoins,
                        Moving = rp.IsMoving,
                        Grounded = rp.Grounded,
                        VelY = MathF.Round(rp.VelY, 1),
                        ShieldStance = rp.ShieldStance,
                        UltForm = rp.Parts.UltFormActive,
                        Visible = !rp.IsSpectator && rp.Hp > 0f && rp.Enabled,
                        Emote = rp.ActiveEmote,
                        AnimSlot = rp.Parts.ActiveAnim,
                        AnimStep = rp.LastAnimStep
                    });
                }
            }

            List<ReplayEnemyFrame>? enemyList = null;
            if (CombatManager.Enemies.Count > 0)
            {
                enemyList = new List<ReplayEnemyFrame>(CombatManager.Enemies.Count);
                foreach (var e in CombatManager.Enemies)
                {
                    if (!IsValid(e) || e.Hp <= 0f) continue;
                    enemyList.Add(new ReplayEnemyFrame
                    {
                        Id = e.NetId,
                        Et = e.EnemyType,
                        X = MathF.Round(e.X, 2),
                        Y = MathF.Round(e.Y, 2),
                        Z = MathF.Round(e.Z, 2),
                        RotY = MathF.Round(e.RotationY, 1),
                        Hp = MathF.Round(e.Hp, 1),
                        MaxHp = MathF.Round(e.MaxHp, 1),
                        Boss = e.IsBoss,
                        Elite = e.IsElite,
                        Ally = e.IsAlly,
                        Doppel = e.IsDoppelganger,
                        Phase = e.Phase
                    });
                }
            }

            List<ReplayProjFrame>? projList = null;
            int projCap = 52;
            for (int i = 0; i < Engine.AllEntities.Count && (projList == null || projList.Count < projCap); i++)
            {
                var ent = Engine.AllEntities[i];
                if (!IsValid(ent)) continue;
                if (ent is Projectile pr)
                {
                    projList ??= new List<ReplayProjFrame>(16);
                    projList.Add(new ReplayProjFrame
                    {
                        X = MathF.Round(pr.X, 2),
                        Y = MathF.Round(pr.Y, 2),
                        Z = MathF.Round(pr.Z, 2),
                        S = MathF.Round(pr.ScaleX, 2),
                        R = pr.Color.R,
                        G = pr.Color.G,
                        B = pr.Color.B
                    });
                }
                else if (ent is RemoteVisualProjectile rvp)
                {
                    projList ??= new List<ReplayProjFrame>(16);
                    projList.Add(new ReplayProjFrame
                    {
                        X = MathF.Round(rvp.X, 2),
                        Y = MathF.Round(rvp.Y, 2),
                        Z = MathF.Round(rvp.Z, 2),
                        S = MathF.Round(rvp.ScaleX, 2),
                        R = rvp.Color.R,
                        G = rvp.Color.G,
                        B = rvp.Color.B
                    });
                }
            }

            List<int>? openedChests = null;
            List<string>? donePillars = null;
            for (int i = 0; i < run.StageEntities.Count; i++)
            {
                var se = run.StageEntities[i];
                if (!IsValid(se)) continue;
                if (se is Chest ch && ch.Opened)
                {
                    openedChests ??= new List<int>();
                    openedChests.Add(ch.ChestId);
                }
                else if (se is MoonPillar mp && mp.Completed)
                {
                    donePillars ??= new List<string>();
                    donePillars.Add(mp.PillarType);
                }
            }

            _active.Frames.Add(new ReplayFrame
            {
                T = MathF.Round(run.ElapsedTime, 2),
                StageIdx = curStageIdx,
                DiffCoeff = MathF.Round(run.DifficultyCoeff, 2),
                TpState = tpState,
                TpCharge = tpCharge,
                TpAligned = tpAligned,
                MoonDetTimer = MathF.Round(run.MoonDetonationTimer, 1),
                BossTitle = bTitle,
                BossHp = bHp,
                BossMaxHp = bMax,
                BossIsMithrix = bMithrix,
                BossPhase = bPhase,
                Kills = Program.RunKills,
                DmgDealt = MathF.Round(Program.RunDamageDealt, 0),
                ItemsCompact = itemsDelta,
                Local = localFrame,
                Remotes = remList,
                Enemies = enemyList,
                Projs = projList,
                OpenedChests = openedChests?.ToArray(),
                DonePillars = donePillars?.ToArray()
            });
        }

        public static RunLogEntry? FinalizeAndSave(
            GameRunContext? run,
            string outcome,
            bool wasDedicatedSpectator,
            string killerKey,
            string killerName,
            string killerRank,
            float killerDamage,
            bool killerIsBoss,
            bool killerIsElite,
            bool killerIsDoppel)
        {
            if (_active == null || _runLogged || run == null || run.IsSafeTravels)
                return null;

            // Если игрок вышел в первую же секунду до любых действий — не засоряем историю призрачными прерванными стартами
            if (outcome == "abandoned" && run.ElapsedTime < 2.0f && Program.RunKills == 0 && run.ItemOrder.Count == 0)
            {
                CancelWithoutSaving();
                return null;
            }

            // Финальный контрольный кадр состояния
            _sampleAccum = SampleInterval;
            Tick(0.01f, run, Program.GetRemotePlayersReadOnly(), Program.IsSpectating);

            _runLogged = true;
            _active.Outcome = outcome;
            _active.DurationSec = MathF.Round(run.ElapsedTime, 2);
            _active.KillerName = outcome == "defeat" ? killerName : "";

            string relReplay = "";
            bool hasFrames = _active.Stages.Count > 0 && _active.Frames.Count > 0;
            bool shouldWriteNow = hasFrames && UserSettings.Instance.ShouldAutoWriteReplayDisk(outcome);
            if (shouldWriteNow)
            {
                try
                {
                    Directory.CreateDirectory(RunHistoryStorage.ReplaysDir);
                    relReplay = Path.Combine("replays", $"{_active.RunId}.rpl.gz");
                    string fullReplay = RunHistoryStorage.GetFullReplayPath(relReplay);
                    using var fs = File.Create(fullReplay);
                    using var gz = new GZipStream(fs, CompressionLevel.Fastest);
                    JsonSerializer.Serialize(gz, _active);
                    _lastPendingReplay = null;
                    _lastPendingEntryRunId = "";
                }
                catch
                {
                    relReplay = "";
                }
            }
            else if (hasFrames && outcome != "abandoned")
            {
                _lastPendingReplay = _active;
                _lastPendingEntryRunId = _active.RunId;
            }
            else
            {
                _lastPendingReplay = null;
                _lastPendingEntryRunId = "";
            }

            string profileKey = ProgressData.Instance.GetLoadoutProfileKey(run.CharKey);
            int[] skillVars = new int[4];
            string[] slots = { "m1", "m2", "shift", "r" };
            for (int i = 0; i < 4; i++)
                skillVars[i] = ProgressData.Instance.GetSkillVariant(run.CharKey, slots[i]);

            var itemRecords = new List<RunItemRecord>();
            int totalItems = 0;
            foreach (var ik in run.ItemOrder)
            {
                if (run.Items.TryGetValue(ik, out int cnt) && cnt > 0)
                {
                    itemRecords.Add(new RunItemRecord { Key = ik, Count = cnt });
                    totalItems += cnt;
                }
            }

            var stageRecords = _active.Stages.Select(s => new RunStageRecord
            {
                StageNum = s.StageNum,
                StageName = s.StageName,
                SpecialMode = s.SpecialMode,
                Seed = s.Seed,
                EnterTimeSec = s.EnterTime
            }).ToList();

            var entry = new RunLogEntry
            {
                RunId = _active.RunId,
                PlayedAt = _active.PlayedAt,
                PlayerName = _active.PlayerName,
                CharKey = run.CharKey,
                SkinIdx = ProgressData.Instance.GetSelectedSkin(run.CharKey),
                SkillVariants = skillVars,
                DifficultyKey = _active.DifficultyKey,
                Artifacts = new List<string>(_active.Artifacts),
                GameMode = _active.GameMode,
                Outcome = outcome,
                WasSpectatorOnly = wasDedicatedSpectator,
                DurationSec = MathF.Round(run.ElapsedTime, 1),
                FinalStageNum = run.Stage,
                FinalStageName = run.StageName,
                StagesVisited = stageRecords,
                CoopPeers = _seenPeers.Values.ToList(),
                Kills = Program.RunKills,
                BossKills = Program.RunBossKills,
                DamageDealt = MathF.Round(Program.RunDamageDealt, 0),
                DamageTaken = MathF.Round(_damageTakenAccum, 0),
                HitsTaken = run.HitsTakenCount,
                TotalHealed = MathF.Round(run.TotalHealed, 0),
                GoldEarned = Program.RunGoldEarned,
                ChestsOpened = run.ChestsOpened,
                AbilityCasts = run.TotalAbilityCasts,
                TotalItemsCount = totalItems,
                Items = itemRecords,
                KillerKey = outcome == "defeat" ? killerKey : "",
                KillerName = outcome == "defeat" ? killerName : "",
                KillerRank = outcome == "defeat" ? killerRank : "",
                KillerDamage = outcome == "defeat" ? MathF.Round(killerDamage, 1) : 0f,
                KillerIsBoss = outcome == "defeat" && killerIsBoss,
                KillerIsElite = outcome == "defeat" && killerIsElite,
                KillerIsDoppel = outcome == "defeat" && killerIsDoppel,
                ReplayFile = relReplay
            };

            RunHistoryStorage.AddEntry(entry);
            _active = null;
            return entry;
        }

        public static string BuildCompactItems(GameRunContext run)
        {
            if (run.ItemOrder.Count == 0) return "";
            var parts = new List<string>(run.ItemOrder.Count);
            foreach (var k in run.ItemOrder)
            {
                if (run.Items.TryGetValue(k, out int cnt) && cnt > 0)
                    parts.Add($"{k}:{cnt}");
            }
            return string.Join(",", parts);
        }

        public static List<(string Key, int Count)> ParseCompactItems(string? compact)
        {
            var res = new List<(string Key, int Count)>();
            if (string.IsNullOrWhiteSpace(compact)) return res;
            foreach (var token in compact.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                int sep = token.IndexOf(':');
                if (sep <= 0) continue;
                string k = token[..sep];
                if (int.TryParse(token[(sep + 1)..], out int cnt) && cnt > 0 && GameData.ItemPool.ContainsKey(k))
                    res.Add((k, cnt));
            }
            return res;
        }
    }

    // =================================================================================================
    // 3. ИНТЕРАКТИВНЫЙ 3D-ПЛЕЕР РЕПЛЕЕВ (ReplayViewer) — ВОСПРОИЗВЕДЕНИЕ, ПЕРЕМОТКА И КАМЕРЫ
    // =================================================================================================
    public static class ReplayViewer
    {
        private sealed class ActorPuppet
        {
            public string Id = "";
            public string CharKey = "commando";
            public int SkinIdx = 0;
            public Entity Root = null!;
            public SurvivorParts Parts = null!;
            public Entity Label = null!;
            public Entity? ChairProp;
            public int LastAnimStep = 0;
        }

        private sealed class EnemyPuppet
        {
            public int Id;
            public string EnemyType = "lemurian";
            public Entity Root = null!;
            public EnemyParts Parts = null!;
            public Entity Label = null!;
            public Entity? EliteAura;
        }

        private static ReplayData? _data;
        private static string[] _resolvedItemsPerFrame = Array.Empty<string>();
        private static string _returnScreen = "logbook";

        private static float _playTime = 0f;
        private static float _totalDuration = 1f;
        private static bool _paused = false;
        private static float _speed = 1.0f;
        private static bool _hideHud = false;
        private static bool _scrubbingTimeline = false;

        // 0 = Орбита за игроком, 1 = От лица игрока (POV), 2 = Свободная камера (Free Fly)
        private static int _camMode = 0;
        private static int _followTargetIdx = 0; // 0 = локальный игрок, 1..N = кооп-союзники
        private static float _orbitYaw = 0f;
        private static float _orbitPitch = 16f;
        private static float _orbitDist = 9.2f;
        private static Vector3 _smoothedPivot = new(0, 2f, 0);
        private static Vector3 _freeCamPos = new(0, 8f, -14f);
        private static float _freeCamYaw = 0f;
        private static float _freeCamPitch = 12f;

        private static int _loadedStageIdx = -1;
        private static readonly List<Entity> _stageEntities = new();
        private static Entity? _stageTpOrPad;
        private static ActorPuppet? _localPuppet;
        private static readonly Dictionary<string, ActorPuppet> _remotePuppets = new();
        private static readonly Dictionary<int, EnemyPuppet> _enemyPuppets = new();
        private static readonly List<Entity> _projPool = new();

        public static bool IsActive => _data != null;
        public static string ReturnScreen => _returnScreen;

        public static bool StartPlayback(string replayRelOrFullPath, string returnScreen, out string errorMsg)
        {
            errorMsg = "";
            string full = RunHistoryStorage.GetFullReplayPath(replayRelOrFullPath);
            if (string.IsNullOrWhiteSpace(full) || !File.Exists(full))
            {
                errorMsg = "Файл 3D-реплея не найден на диске.";
                return false;
            }

            ReplayData? loaded;
            try
            {
                using var fs = File.OpenRead(full);
                using var gz = new GZipStream(fs, CompressionMode.Decompress);
                loaded = JsonSerializer.Deserialize<ReplayData>(gz);
            }
            catch (Exception ex)
            {
                errorMsg = $"Не удалось прочитать файл реплея: {ex.Message}";
                return false;
            }

            if (loaded == null || loaded.Stages.Count == 0 || loaded.Frames.Count == 0)
            {
                errorMsg = "Запись реплея пуста или повреждена.";
                return false;
            }

            CleanupSceneEntities();
            _data = loaded;
            _returnScreen = returnScreen;
            _resolvedItemsPerFrame = new string[_data.Frames.Count];
            string curItems = "";
            for (int i = 0; i < _data.Frames.Count; i++)
            {
                if (_data.Frames[i].ItemsCompact != null)
                    curItems = _data.Frames[i].ItemsCompact!;
                _resolvedItemsPerFrame[i] = curItems;
            }

            _playTime = 0f;
            _totalDuration = MathF.Max(0.2f, MathF.Max(_data.DurationSec, _data.Frames[^1].T));
            _paused = false;
            _speed = 1.0f;
            _hideHud = false;
            _scrubbingTimeline = false;
            _camMode = 0;
            _followTargetIdx = 0;
            _orbitYaw = _data.Frames[0].Local.RotY;
            _orbitPitch = 16f;
            _orbitDist = 9.2f;
            _loadedStageIdx = -1;

            WorldContext.Game = null;
            CombatManager.Controller = null;
            Raylib.EnableCursor();

            EnsureStageLoaded(_data.Frames[0].StageIdx);
            var f0 = _data.Frames[0].Local;
            _smoothedPivot = new Vector3(f0.X, f0.Y + 1.9f, f0.Z);
            _freeCamPos = _smoothedPivot + new Vector3(0f, 5f, -11f);
            _freeCamYaw = f0.RotY;
            _freeCamPitch = 15f;
            ApplyInterpolatedWorld(0, 0, 0f, 0.016f);
            return true;
        }

        public static void StopPlayback()
        {
            CleanupSceneEntities();
            _data = null;
            _resolvedItemsPerFrame = Array.Empty<string>();
            _loadedStageIdx = -1;
            Raylib.EnableCursor();
        }

        private static void CleanupSceneEntities()
        {
            if (_localPuppet != null)
            {
                if (IsValid(_localPuppet.Root)) Destroy(_localPuppet.Root);
                _localPuppet = null;
            }
            foreach (var rp in _remotePuppets.Values)
                if (IsValid(rp.Root)) Destroy(rp.Root);
            _remotePuppets.Clear();

            foreach (var ep in _enemyPuppets.Values)
                if (IsValid(ep.Root)) Destroy(ep.Root);
            _enemyPuppets.Clear();

            foreach (var pr in _projPool)
                if (IsValid(pr)) Destroy(pr);
            _projPool.Clear();

            foreach (var se in _stageEntities)
                if (IsValid(se)) Destroy(se);
            _stageEntities.Clear();
            _stageTpOrPad = null;
        }

        private static void EnsureStageLoaded(int stageIdx)
        {
            if (_data == null || _data.Stages.Count == 0) return;
            stageIdx = Math.Clamp(stageIdx, 0, _data.Stages.Count - 1);
            if (_loadedStageIdx == stageIdx) return;

            CleanupSceneEntities();
            _loadedStageIdx = stageIdx;
            var st = _data.Stages[stageIdx];
            var (ents, tpOrPad, _) = StageBuilder.BuildStageWorld(st.StageNum, st.Seed, st.SpecialMode);
            _stageEntities.AddRange(ents);
            _stageTpOrPad = tpOrPad;
        }

        public static void Update(float dt)
        {
            if (_data == null || _data.Frames.Count == 0) return;

            // Горячие клавиши управления плеером
            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                _paused = !_paused;
                SoundManager.Play("ui_click");
            }
            if (Raylib.IsKeyPressed(KeyboardKey.H))
            {
                _hideHud = !_hideHud;
                SoundManager.Play("ui_click");
            }
            if (Raylib.IsKeyPressed(KeyboardKey.C))
            {
                CycleCameraMode();
                SoundManager.Play("ui_click");
            }
            float jumpSec = (Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift)) ? 15f : 5f;
            if (Raylib.IsKeyPressed(KeyboardKey.Left))
                _playTime = Math.Clamp(_playTime - jumpSec, 0f, _totalDuration);
            if (Raylib.IsKeyPressed(KeyboardKey.Right))
                _playTime = Math.Clamp(_playTime + jumpSec, 0f, _totalDuration);

            if (Raylib.IsKeyPressed(KeyboardKey.LeftBracket))
                StepSpeed(-1);
            if (Raylib.IsKeyPressed(KeyboardKey.RightBracket))
                StepSpeed(1);

            if (Raylib.IsKeyPressed(KeyboardKey.Q)) _followTargetIdx = Math.Max(0, _followTargetIdx - 1);
            if (Raylib.IsKeyPressed(KeyboardKey.E) && _camMode != 2) _followTargetIdx++;

            if (!_paused && !_scrubbingTimeline)
            {
                _playTime += dt * _speed;
                if (_playTime >= _totalDuration)
                {
                    _playTime = _totalDuration;
                    _paused = true;
                }
            }

            var (idxA, idxB, alpha) = FindFramePair(_playTime);
            var frameA = _data.Frames[idxA];
            EnsureStageLoaded(frameA.StageIdx);
            if (_data.Frames[idxB].StageIdx != frameA.StageIdx)
            {
                idxB = idxA;
                alpha = 0f;
            }

            float animDt = _paused ? 0f : dt * _speed;
            ApplyInterpolatedWorld(idxA, idxB, alpha, animDt);
            UpdateReplayCamera(dt, frameA, _data.Frames[idxB], alpha);
        }

        private static void CycleCameraMode()
        {
            _camMode = (_camMode + 1) % 3;
            if (_camMode == 2)
            {
                _freeCamPos = _smoothedPivot + new Vector3(0f, 3.5f, -8.5f);
                _freeCamYaw = _orbitYaw;
                _freeCamPitch = _orbitPitch;
            }
        }

        private static void StepSpeed(int dir)
        {
            float[] speeds = { 0.25f, 0.5f, 1.0f, 2.0f, 4.0f };
            int cur = 2;
            for (int i = 0; i < speeds.Length; i++)
                if (MathF.Abs(speeds[i] - _speed) < 0.05f) { cur = i; break; }
            _speed = speeds[Math.Clamp(cur + dir, 0, speeds.Length - 1)];
            SoundManager.Play("ui_click");
        }

        private static (int idxA, int idxB, float alpha) FindFramePair(float t)
        {
            var frames = _data!.Frames;
            if (frames.Count == 1 || t <= frames[0].T) return (0, 0, 0f);
            if (t >= frames[^1].T) return (frames.Count - 1, frames.Count - 1, 0f);

            int lo = 0, hi = frames.Count - 1;
            while (lo + 1 < hi)
            {
                int mid = (lo + hi) / 2;
                if (frames[mid].T <= t) lo = mid;
                else hi = mid;
            }
            float span = frames[hi].T - frames[lo].T;
            float a = span > 0.0001f ? Math.Clamp((t - frames[lo].T) / span, 0f, 1f) : 0f;
            return (lo, hi, a);
        }

        private static ActorPuppet EnsureActorPuppet(ActorPuppet? existing, string id, ReplayActorFrame st, bool isLocal)
        {
            string ck = SurvivorDatabase.Classes.ContainsKey(st.CharKey) ? st.CharKey : "commando";
            int maxSk = SurvivorDatabase.Classes[ck].Skins.Count > 0 ? SurvivorDatabase.Classes[ck].Skins.Count - 1 : 0;
            int sk = Math.Clamp(st.SkinIdx, 0, maxSk);

            if (existing == null || !IsValid(existing.Root))
            {
                var root = new Entity(position: new Vector3(st.X, st.Y, st.Z));
                var parts = SurvivorDatabase.BuildSurvivorMesh(root, ck, sk);
                var lbl = new Entity(parent: root, position: new(0, 2.75f, 0), color: isLocal ? Color.Gold : Color.SkyBlue);
                existing = new ActorPuppet
                {
                    Id = id,
                    CharKey = ck,
                    SkinIdx = sk,
                    Root = root,
                    Parts = parts,
                    Label = lbl,
                    LastAnimStep = st.AnimStep
                };
            }
            else if (existing.CharKey != ck || existing.SkinIdx != sk)
            {
                existing.CharKey = ck;
                existing.SkinIdx = sk;
                Destroy(existing.Parts.Root);
                existing.Parts = SurvivorDatabase.BuildSurvivorMesh(existing.Root, ck, sk);
            }
            return existing;
        }

        private static void UpdateActorPuppet(ActorPuppet pup, ReplayActorFrame a, ReplayActorFrame b, float alpha, float animDt, bool isLocal, bool hideForPov)
        {
            Vector3 posA = new(a.X, a.Y, a.Z);
            Vector3 posB = new(b.X, b.Y, b.Z);
            pup.Root.Position = Vector3.Distance(posA, posB) > 28f ? posA : Lerp(posA, posB, alpha);
            pup.Root.RotationY = LerpAngleDeg(a.RotY, b.RotY, alpha);

            bool vis = a.Visible && !hideForPov;
            pup.Parts.Root.Enabled = vis;
            pup.Label.Enabled = a.Visible && !hideForPov;

            pup.Parts.SetUltForm(a.UltForm);
            pup.Parts.SetEmote(a.Emote);

            if (a.Emote == "chair" && vis)
            {
                if (pup.ChairProp == null || pup.ChairProp.IsDestroyed)
                    pup.ChairProp = RemotePlayer.BuildPlasticChairEntity(pup.Root);
                pup.ChairProp.Enabled = true;
            }
            else if (pup.ChairProp != null && !pup.ChairProp.IsDestroyed)
            {
                pup.ChairProp.Enabled = false;
            }

            if (a.AnimStep != pup.LastAnimStep)
            {
                pup.LastAnimStep = a.AnimStep;
                if (!string.IsNullOrEmpty(a.AnimSlot) && animDt > 0f)
                    pup.Parts.TriggerSkillAnim(a.AnimSlot);
            }

            if (animDt > 0f)
            {
                pup.Parts.UpdateAnimations(
                    pup.CharKey, animDt, a.Moving,
                    isGrounded: a.Grounded, velY: a.VelY,
                    moveSpeedRatio: 1.0f, strafeInput: 0f,
                    forwardInput: a.Moving ? 1f : 0f,
                    shieldStance: a.ShieldStance);
            }

            float curHp = Lerp(a.Hp, b.Hp, alpha);
            string heroTitle = SurvivorDatabase.Classes[pup.CharKey].Name;
            string tag = isLocal ? $"[ЗАПИСЬ: {a.Name}]" : $"[{a.Name}]";
            pup.Label.BillboardText = $"{tag} {heroTitle}\n{(int)curHp}/{(int)a.MaxHp} HP";
        }

        private static void ApplyInterpolatedWorld(int idxA, int idxB, float alpha, float animDt)
        {
            var fA = _data!.Frames[idxA];
            var fB = _data!.Frames[idxB];

            // 1. Записанный локальный игрок
            _localPuppet = EnsureActorPuppet(_localPuppet, "local", fA.Local, isLocal: true);
            bool hideLocalForPov = _camMode == 1 && (_followTargetIdx == 0);
            UpdateActorPuppet(_localPuppet, fA.Local, fB.Local, alpha, animDt, isLocal: true, hideForPov: hideLocalForPov);

            // 2. Кооп-союзники / соперники
            var activeRemoteIds = new HashSet<string>();
            if (fA.Remotes != null)
            {
                foreach (var rA in fA.Remotes)
                {
                    activeRemoteIds.Add(rA.Id);
                    var rB = fB.Remotes?.Find(x => x.Id == rA.Id) ?? rA;
                    _remotePuppets.TryGetValue(rA.Id, out var rPup);
                    rPup = EnsureActorPuppet(rPup, rA.Id, rA, isLocal: false);
                    _remotePuppets[rA.Id] = rPup;
                    UpdateActorPuppet(rPup, rA, rB, alpha, animDt, isLocal: false, hideForPov: false);
                }
            }
            foreach (var staleId in _remotePuppets.Keys.Where(k => !activeRemoteIds.Contains(k)).ToArray())
            {
                if (IsValid(_remotePuppets[staleId].Root)) Destroy(_remotePuppets[staleId].Root);
                _remotePuppets.Remove(staleId);
            }

            // 3. Враги и боссы
            var activeEnemyIds = new HashSet<int>();
            if (fA.Enemies != null)
            {
                var mapB = new Dictionary<int, ReplayEnemyFrame>(fB.Enemies?.Count ?? 0);
                if (fB.Enemies != null)
                    foreach (var eb in fB.Enemies) mapB[eb.Id] = eb;

                foreach (var eA in fA.Enemies)
                {
                    activeEnemyIds.Add(eA.Id);
                    var eB = mapB.TryGetValue(eA.Id, out var foundB) ? foundB : eA;
                    if (!_enemyPuppets.TryGetValue(eA.Id, out var ePup) || !IsValid(ePup.Root) || ePup.EnemyType != eA.Et)
                    {
                        if (ePup != null && IsValid(ePup.Root)) Destroy(ePup.Root);
                        float scaleMult = eA.Et == "mithrix" ? 1.0f : (eA.Boss ? 1.85f : 1.0f);
                        var root = new Entity(position: new Vector3(eA.X, eA.Y, eA.Z), scaleUniform: scaleMult);
                        var parts = EnemyAndBossBuilder.BuildEnemyMesh(root, eA.Et);
                        float lblH = (GameData.MonsterLog.TryGetValue(eA.Et, out var mDef) ? 2.4f : 2.3f);
                        var lbl = new Entity(parent: root, position: new(0, eA.Et == "mithrix" ? 4.8f : lblH, 0),
                            color: eA.Boss ? Color.Gold : Rgb(255, 115, 85));
                        ePup = new EnemyPuppet
                        {
                            Id = eA.Id,
                            EnemyType = eA.Et,
                            Root = root,
                            Parts = parts,
                            Label = lbl
                        };
                        _enemyPuppets[eA.Id] = ePup;
                    }

                    Vector3 epA = new(eA.X, eA.Y, eA.Z);
                    Vector3 epB = new(eB.X, eB.Y, eB.Z);
                    ePup.Root.Position = Vector3.Distance(epA, epB) > 30f ? epA : Lerp(epA, epB, alpha);
                    ePup.Root.RotationY = LerpAngleDeg(eA.RotY, eB.RotY, alpha);

                    if ((eA.Elite || eA.Doppel || eA.Ally) && ePup.EliteAura == null)
                    {
                        Color auraCol = eA.Ally
                            ? Rgb(75, 255, 130, 85)
                            : (eA.Doppel ? Rgb(185, 65, 255, 95) : Rgb(255, 175, 45, 75));
                        ePup.EliteAura = new Entity(parent: ePup.Root, model: "sphere", scale: new(1.6f, 0.14f, 1.6f), position: new(0, 0.08f, 0), color: auraCol, unlit: true);
                    }

                    if (animDt > 0f)
                    {
                        float t = _playTime * 7.5f + eA.Id;
                        bool moving = Vector3.Distance(epA, epB) > 0.04f;
                        float legAmp = moving ? 26f : 4f;
                        if (ePup.Parts.LeftLeg != null) ePup.Parts.LeftLeg.RotationX = MathF.Sin(t) * legAmp;
                        if (ePup.Parts.RightLeg != null) ePup.Parts.RightLeg.RotationX = -MathF.Sin(t) * legAmp;
                        if (ePup.Parts.Crown != null) ePup.Parts.Crown.RotationY += 85f * animDt;
                        if (ePup.Parts.Ring1 != null) ePup.Parts.Ring1.RotationY += 110f * animDt;
                        if (ePup.Parts.Ring2 != null) ePup.Parts.Ring2.RotationX += 125f * animDt;
                        if (ePup.Parts.Tentacles != null) ePup.Parts.Tentacles.RotationY += 70f * animDt;
                    }

                    float curEHp = MathF.Max(0f, Lerp(eA.Hp, eB.Hp, alpha));
                    string eName = GameData.MonsterLog.TryGetValue(eA.Et, out var ml) ? ml.Name : eA.Et;
                    string prefix = eA.Ally ? "[СОЮЗНИК] " : (eA.Doppel ? "[ДВОЙНИК] " : (eA.Elite && !eA.Boss ? "★ " : ""));
                    ePup.Label.Color = eA.Ally ? Color.Lime : (eA.Boss ? Color.Gold : (eA.Elite ? Color.Orange : Rgb(255, 125, 95)));
                    ePup.Label.BillboardText = eA.Boss
                        ? $"{prefix}{eName}: {(int)curEHp}/{(int)eA.MaxHp}"
                        : $"{prefix}{(int)curEHp}/{(int)eA.MaxHp}";
                }
            }
            foreach (var staleEid in _enemyPuppets.Keys.Where(id => !activeEnemyIds.Contains(id)).ToArray())
            {
                if (IsValid(_enemyPuppets[staleEid].Root)) Destroy(_enemyPuppets[staleEid].Root);
                _enemyPuppets.Remove(staleEid);
            }

            // 4. Снаряды (пул визуальных сфер)
            int projCount = fA.Projs?.Count ?? 0;
            while (_projPool.Count < projCount)
            {
                var pEnt = new Entity(model: "sphere", scaleUniform: 0.25f, color: Color.Yellow, unlit: true);
                new Entity(parent: pEnt, model: "diamond", scaleUniform: 1.4f, color: Color.White, unlit: true);
                _projPool.Add(pEnt);
            }
            for (int i = 0; i < _projPool.Count; i++)
            {
                var pEnt = _projPool[i];
                if (!IsValid(pEnt)) continue;
                if (i < projCount && fA.Projs != null)
                {
                    var pr = fA.Projs[i];
                    pEnt.Enabled = true;
                    pEnt.Position = new Vector3(pr.X, pr.Y, pr.Z);
                    pEnt.Scale = new Vector3(MathF.Max(0.12f, pr.S));
                    pEnt.Color = Rgb(pr.R, pr.G, pr.B);
                    pEnt.RotationY += 300f * animDt;
                }
                else
                {
                    pEnt.Enabled = false;
                }
            }

            // 5. Состояние сундуков, телепорта и столпов Луны (корректно обновляется даже при перемотке назад!)
            var openedSet = fA.OpenedChests != null ? new HashSet<int>(fA.OpenedChests) : null;
            var donePillarsSet = fA.DonePillars != null ? new HashSet<string>(fA.DonePillars) : null;
            for (int i = 0; i < _stageEntities.Count; i++)
            {
                var se = _stageEntities[i];
                if (!IsValid(se)) continue;
                if (se is Chest ch)
                {
                    ch.SetReplayVisualOpen(openedSet != null && openedSet.Contains(ch.ChestId));
                }
                else if (se is MoonPillar mp)
                {
                    mp.SetReplayVisualDone(donePillarsSet != null && donePillarsSet.Contains(mp.PillarType));
                }
            }
            if (_stageTpOrPad is Teleporter tp && IsValid(tp))
            {
                tp.ApplyReplayState(fA.TpState, Lerp(fA.TpCharge, fB.TpCharge, alpha), fA.TpAligned);
            }
        }

        private static void UpdateReplayCamera(float dt, ReplayFrame fA, ReplayFrame fB, float alpha)
        {
            int sh = Raylib.GetScreenHeight();
            Vector2 mp = Raylib.GetMousePosition();
            bool overControls = !_hideHud && (mp.Y >= sh - 118f || mp.Y <= 52f);

            // Собираем список доступных целей наблюдения (0 = основной игрок, 1..N = союзники)
            var followTargets = new List<(Vector3 Pos, float RotY, float Pitch, string Label)>
            {
                (
                    _localPuppet != null && IsValid(_localPuppet.Root) ? _localPuppet.Root.Position : new Vector3(fA.Local.X, fA.Local.Y, fA.Local.Z),
                    LerpAngleDeg(fA.Local.RotY, fB.Local.RotY, alpha),
                    Lerp(fA.Local.Pitch, fB.Local.Pitch, alpha),
                    $"{fA.Local.Name} ({SurvivorDatabase.Classes.GetValueOrDefault(fA.Local.CharKey)?.Name ?? fA.Local.CharKey})"
                )
            };
            if (fA.Remotes != null)
            {
                foreach (var rm in fA.Remotes)
                {
                    if (_remotePuppets.TryGetValue(rm.Id, out var rp) && IsValid(rp.Root))
                    {
                        followTargets.Add((
                            rp.Root.Position,
                            rp.Root.RotationY,
                            12f,
                            $"{rm.Name} ({SurvivorDatabase.Classes.GetValueOrDefault(rm.CharKey)?.Name ?? rm.CharKey})"
                        ));
                    }
                }
            }

            var targetInfo = followTargets[_followTargetIdx % followTargets.Count];
            _smoothedPivot = Lerp(_smoothedPivot, targetInfo.Pos + new Vector3(0f, 1.95f, 0f), MathF.Min(1f, dt * 16f));

            bool rmbRotate = Raylib.IsMouseButtonDown(MouseButton.Right) && !overControls && !_scrubbingTimeline;
            if (rmbRotate)
            {
                if (!Raylib.IsCursorHidden()) Raylib.DisableCursor();
                Vector2 md = Raylib.GetMouseDelta();
                if (_camMode == 2)
                {
                    _freeCamYaw += md.X * 0.20f;
                    _freeCamPitch = Math.Clamp(_freeCamPitch + md.Y * 0.20f, -82f, 82f);
                }
                else
                {
                    _orbitYaw += md.X * 0.22f;
                    _orbitPitch = Math.Clamp(_orbitPitch + md.Y * 0.22f, -38f, 68f);
                }
            }
            else if (Raylib.IsCursorHidden())
            {
                Raylib.EnableCursor();
            }

            float wheel = Raylib.GetMouseWheelMove();
            if (MathF.Abs(wheel) > 0.01f && !overControls && _camMode == 0)
                _orbitDist = Math.Clamp(_orbitDist - wheel * 1.35f, 3.2f, 32.0f);

            Engine.Camera.FovY = 62f;

            if (_camMode == 2)
            {
                // Свободная летающая камера зрителя
                float yawRad = _freeCamYaw * (MathF.PI / 180f);
                float pitchRad = _freeCamPitch * (MathF.PI / 180f);
                Vector3 fwd = Vector3.Normalize(new Vector3(
                    MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                    -MathF.Sin(pitchRad),
                    MathF.Cos(yawRad) * MathF.Cos(pitchRad)
                ));
                Vector3 right = Vector3.Normalize(new Vector3(MathF.Cos(yawRad), 0f, -MathF.Sin(yawRad)));

                float flySpd = (Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift)) ? 42f : 18f;
                if (Raylib.IsKeyDown(KeyboardKey.W)) _freeCamPos += fwd * flySpd * dt;
                if (Raylib.IsKeyDown(KeyboardKey.S)) _freeCamPos -= fwd * flySpd * dt;
                if (Raylib.IsKeyDown(KeyboardKey.D)) _freeCamPos += right * flySpd * dt;
                if (Raylib.IsKeyDown(KeyboardKey.A)) _freeCamPos -= right * flySpd * dt;
                if (Raylib.IsKeyDown(KeyboardKey.E) || Raylib.IsKeyDown(KeyboardKey.PageUp)) _freeCamPos.Y += flySpd * dt;
                if (Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.PageDown)) _freeCamPos.Y -= flySpd * dt;

                Engine.Camera.Position = new Vector3(-_freeCamPos.X, _freeCamPos.Y, _freeCamPos.Z);
                Vector3 lookAt = _freeCamPos + fwd * 10f;
                Engine.Camera.Target = new Vector3(-lookAt.X, lookAt.Y, lookAt.Z);
                Engine.ObstructionFadeEnabled = false;
            }
            else if (_camMode == 1)
            {
                // Записанный ракурс от лица игрока (Recorded POV)
                float yawRad = targetInfo.RotY * (MathF.PI / 180f);
                float pitchRad = targetInfo.Pitch * (MathF.PI / 180f);
                Vector3 lookDir = Vector3.Normalize(new Vector3(
                    MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                    -MathF.Sin(pitchRad),
                    MathF.Cos(yawRad) * MathF.Cos(pitchRad)
                ));
                bool isFpHero = SurvivorCombatController.IsFirstPersonCharacter(fA.Local.CharKey) && _followTargetIdx == 0;
                if (isFpHero)
                {
                    Vector3 eye = targetInfo.Pos + new Vector3(0f, 1.72f, 0f);
                    Engine.Camera.Position = new Vector3(-eye.X, eye.Y, eye.Z);
                    Vector3 lookAt = eye + lookDir * 10f;
                    Engine.Camera.Target = new Vector3(-lookAt.X, lookAt.Y, lookAt.Z);
                    Engine.ObstructionFadeEnabled = false;
                }
                else
                {
                    Vector3 pivot = _smoothedPivot + new Vector3(0f, 0.30f, 0f);
                    Vector3 desired = pivot - lookDir * 8.6f;
                    Vector3 camPos = Physics.ResolveCameraCollision(targetInfo.Pos + new Vector3(0, 1.7f, 0), pivot, desired, 0.45f);
                    float floorY = Physics.GetSurfaceHeight(camPos.X, camPos.Z, camPos.Y) + 0.50f;
                    if (camPos.Y < floorY) camPos.Y = floorY;
                    Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                    Vector3 lookAt = pivot + lookDir * 10f;
                    Engine.Camera.Target = new Vector3(-lookAt.X, lookAt.Y, lookAt.Z);
                    Engine.ObstructionFadeEnabled = true;
                    Engine.PlayerWorldPos = targetInfo.Pos;
                }
            }
            else
            {
                // Свободная орбита вокруг выбранного игрока
                float yawRad = _orbitYaw * (MathF.PI / 180f);
                float pitchRad = _orbitPitch * (MathF.PI / 180f);
                Vector3 lookDir = Vector3.Normalize(new Vector3(
                    MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                    -MathF.Sin(pitchRad),
                    MathF.Cos(yawRad) * MathF.Cos(pitchRad)
                ));
                Vector3 desired = _smoothedPivot - lookDir * _orbitDist;
                Vector3 camPos = Physics.ResolveCameraCollision(targetInfo.Pos + new Vector3(0, 1.6f, 0), _smoothedPivot, desired, 0.45f);
                float floorY = Physics.GetSurfaceHeight(camPos.X, camPos.Z, camPos.Y) + 0.50f;
                if (camPos.Y < floorY) camPos.Y = floorY;
                Engine.Camera.Position = new Vector3(-camPos.X, camPos.Y, camPos.Z);
                Vector3 lookAt = _smoothedPivot + lookDir * 8f;
                Engine.Camera.Target = new Vector3(-lookAt.X, lookAt.Y, lookAt.Z);
                Engine.ObstructionFadeEnabled = true;
                Engine.PlayerWorldPos = targetInfo.Pos;
            }
        }

        public static bool DrawOverlayUI()
        {
            if (_data == null || _data.Frames.Count == 0) return true;
            int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
            var (idxA, idxB, alpha) = FindFramePair(_playTime);
            var fA = _data.Frames[idxA];
            var fB = _data.Frames[idxB];
            var stInfo = _data.Stages[Math.Clamp(fA.StageIdx, 0, _data.Stages.Count - 1)];

            if (_hideHud)
            {
                Raylib.DrawRectangle(sw - 290, 10, 278, 28, Rgb(10, 14, 24, 175));
                Engine.DrawTextCentered("[H] Показать интерфейс реплея  |  [ESC] Выход", sw - 151f, 24f, 12f, Color.SkyBlue);
                return false;
            }

            // 1. ВЕРХНЯЯ ПАНЕЛЬ ЗАПИСИ
            Raylib.DrawRectangle(0, 0, sw, 46, Rgb(10, 14, 26, 228));
            Raylib.DrawRectangle(0, 46, sw, 3, Color.SkyBlue);

            Color outCol = _data.Outcome == "victory" ? Color.Gold : (_data.Outcome == "abandoned" ? Color.SkyBlue : Rgb(255, 85, 85));
            string outBadge = _data.Outcome == "victory" ? "ПОБЕДА" : (_data.Outcome == "abandoned" ? "ПРЕРВАН" : "ПОРАЖЕНИЕ");
            Engine.DrawTextLeft($"▶ 3D-РЕПЛЕЙ ЗАБЕГА [{outBadge}]  ::  {_data.PlayedAt}  ::  Этап {stInfo.StageNum}: {stInfo.StageName} (x{fA.DiffCoeff:F2})",
                18, 13, 16f, outCol);

            string camName = _camMode switch
            {
                1 => "КАМЕРА: ОТ ЛИЦА ИГРОКА [C]",
                2 => "КАМЕРА: СВОБОДНЫЙ ПОЛЁТ [C]",
                _ => "КАМЕРА: ОРБИТА ВОКРУГ ГЕРОЯ [C]"
            };
            if (Engine.DrawButton(new Rectangle(sw - 492, 7, 290, 32), camName, Rgb(28, 52, 88), Rgb(48, 84, 138), Color.Yellow, 13f, Color.SkyBlue))
                CycleCameraMode();

            if (Engine.DrawButton(new Rectangle(sw - 192, 7, 176, 32), "<- ЗАКРЫТЬ [ESC]", Rgb(125, 38, 46), Rgb(170, 56, 66), Color.White, 13.5f))
                return true;

            // 2. ИНВЕНТАРЬ ЗАПИСАННОГО ИГРОКА СЛЕВА СВЕРХУ
            var itemsNow = ReplayRecorder.ParseCompactItems(_resolvedItemsPerFrame[idxA]);
            if (itemsNow.Count > 0)
            {
                int perRow = 14;
                int rows = (itemsNow.Count - 1) / perRow + 1;
                int stripW = Math.Min(itemsNow.Count, perRow) * 46 + 12;
                Raylib.DrawRectangle(16, 58, stripW, rows * 36 + 8, Rgb(12, 16, 26, 170));
                for (int i = 0; i < itemsNow.Count; i++)
                {
                    var (k, cnt) = itemsNow[i];
                    var info = GameData.ItemPool[k];
                    int r = i / perRow, c = i % perRow;
                    int bx = 22 + c * 46, by = 62 + r * 36;
                    Raylib.DrawRectangle(bx, by, 42, 32, Rgb(26, 34, 52, 215));
                    Raylib.DrawRectangle(bx, by, 42, 3, info.Color);
                    Engine.DrawTextCentered(info.Short, bx + 21, by + 10, 11.5f, info.Color);
                    Engine.DrawTextCentered($"x{cnt}", bx + 21, by + 23, 11.5f, Color.White);
                }
            }

            // 3. ПОЛОСКА БОССА ПО ЦЕНТРУ
            if (!string.IsNullOrEmpty(fA.BossTitle) && fA.BossMaxHp > 0f)
            {
                Color bCol = fA.BossIsMithrix ? (fA.BossPhase == 4 ? Color.Magenta : Color.SkyBlue) : Color.Red;
                int bw = 480, bx = (sw - bw) / 2, by = 74;
                Engine.DrawTextCentered(fA.BossTitle, sw * 0.5f, by - 13f, 16f, bCol);
                float curBHp = MathF.Max(0f, Lerp(fA.BossHp, fB.BossHp, alpha));
                float bRatio = Math.Clamp(curBHp / MathF.Max(1f, fA.BossMaxHp), 0f, 1f);
                Raylib.DrawRectangle(bx, by, bw, 13, Rgb(35, 12, 16, 215));
                Raylib.DrawRectangle(bx, by, (int)(bw * bRatio), 13, bCol);
                Engine.DrawTextCentered($"{(int)curBHp} / {(int)fA.BossMaxHp}", sw * 0.5f, by + 6.5f, 12f, Color.White);
            }

            // 4. СТАТУС ЗАПИСАННОГО ИГРОКА СЛЕВА НАД ТАЙМЛАЙНОМ
            int hx = 20, hy = sh - 184;
            Raylib.DrawRectangle(hx, hy, 324, 62, Rgb(12, 16, 28, 218));
            var survCfg = SurvivorDatabase.Classes.GetValueOrDefault(fA.Local.CharKey) ?? SurvivorDatabase.Classes["commando"];
            Raylib.DrawRectangle(hx, hy, 4, 62, survCfg.BodyColor);
            Engine.DrawTextLeft($"{fA.Local.Name} — {survCfg.Name}", hx + 12, hy + 7, 14.5f, Color.Gold);
            Engine.DrawTextLeft($"${fA.Local.Gold}  |  Убито: {fA.Kills}  |  Урон: {(int)fA.DmgDealt}", hx + 12, hy + 24, 12.5f, Color.SkyBlue);

            float curHp = MathF.Max(0f, Lerp(fA.Local.Hp, fB.Local.Hp, alpha));
            float hpRatio = Math.Clamp(curHp / MathF.Max(1f, fA.Local.MaxHp), 0f, 1f);
            Raylib.DrawRectangle(hx + 12, hy + 42, 300, 14, Rgb(42, 18, 22, 230));
            Raylib.DrawRectangle(hx + 12, hy + 42, (int)(300 * hpRatio), 14, hpRatio > 0.3f ? Rgb(80, 215, 95) : Rgb(240, 65, 60));
            string hpLbl = fA.Local.Shield >= 1f
                ? $"{(int)curHp} (+{(int)fA.Local.Shield}) / {(int)fA.Local.MaxHp} HP"
                : $"{(int)curHp} / {(int)fA.Local.MaxHp} HP";
            Engine.DrawTextCentered(hpLbl, hx + 162, hy + 49, 12f, Color.White);

            // Кулдауны навыков справа над таймлайном
            if (fA.Local.Cds != null && fA.Local.Cds.Length >= 4)
            {
                (string slot, string bind, Color col)[] slots =
                {
                    ("m1", "ЛКМ", Color.Yellow), ("m2", "ПКМ", Color.Orange),
                    ("shift", "Shift", Color.SkyBlue), ("r", "R", Color.Magenta)
                };
                for (int i = 0; i < 4; i++)
                {
                    float cd = fA.Local.Cds[i];
                    float maxCd = survCfg.Skills.TryGetValue(slots[i].slot, out var sk) ? MathF.Max(0.1f, sk.Cd) : 4f;
                    int sx = sw - 266 + i * 62, sy = sh - 180;
                    Raylib.DrawRectangle(sx, sy, 54, 54, cd <= 0.05f ? Rgb(26, 38, 56, 215) : Rgb(16, 18, 26, 215));
                    if (cd > 0.05f)
                    {
                        int fillH = (int)(54 * Math.Clamp(cd / maxCd, 0f, 1f));
                        Raylib.DrawRectangle(sx, sy + (54 - fillH), 54, fillH, Rgb(220, 45, 55, 95));
                    }
                    Raylib.DrawRectangle(sx, sy, 54, 3, cd <= 0.05f ? slots[i].col : Color.Gray);
                    Engine.DrawTextCentered(slots[i].bind, sx + 27, sy + 15, 12f, slots[i].col);
                    Engine.DrawTextCentered(cd <= 0.05f ? "ГОТ" : $"{cd:F1}", sx + 27, sy + 36, 13.5f, cd <= 0.05f ? Color.White : Color.Red);
                }
            }

            // 5. НИЖНИЙ ПУЛЬТ УПРАВЛЕНИЯ ТАЙМЛАЙНОМ И СКОРОСТЬЮ
            int barY = sh - 112;
            Raylib.DrawRectangle(0, barY, sw, 112, Rgb(10, 14, 24, 242));
            Raylib.DrawRectangle(0, barY, sw, 3, Color.Gold);

            // Полоса перемотки (Timeline Scrubber)
            int tlX = 28, tlY = barY + 16, tlW = sw - 56, tlH = 22;
            Rectangle tlRect = new(tlX, tlY, tlW, tlH);
            Vector2 mp = Raylib.GetMousePosition();
            if (Raylib.IsMouseButtonPressed(MouseButton.Left) && Raylib.CheckCollisionPointRec(mp, new Rectangle(tlX - 6, tlY - 6, tlW + 12, tlH + 12)))
                _scrubbingTimeline = true;
            if (!Raylib.IsMouseButtonDown(MouseButton.Left))
                _scrubbingTimeline = false;
            if (_scrubbingTimeline)
            {
                float pct = Math.Clamp((mp.X - tlX) / tlW, 0f, 1f);
                _playTime = pct * _totalDuration;
            }

            Raylib.DrawRectangle(tlX, tlY, tlW, tlH, Rgb(24, 32, 48, 250));
            float curPct = Math.Clamp(_playTime / _totalDuration, 0f, 1f);
            Raylib.DrawRectangle(tlX, tlY, (int)(tlW * curPct), tlH, Rgb(65, 155, 245, 220));
            Raylib.DrawRectangleLines(tlX, tlY, tlW, tlH, Color.SkyBlue);

            // Маркеры переходов между этапами на таймлайне
            foreach (var st in _data.Stages)
            {
                float stPct = Math.Clamp(st.EnterTime / _totalDuration, 0f, 1f);
                int mx = tlX + (int)(tlW * stPct);
                Raylib.DrawRectangle(mx, tlY - 3, 3, tlH + 6, Color.Gold);
                if (tlW > 500)
                    Engine.DrawTextLeft($"Э{st.StageNum}", mx + 4, tlY + 4, 11f, Color.Yellow);
            }

            // Ползунок текущей секунды
            int knobX = tlX + (int)(tlW * curPct);
            Raylib.DrawRectangle(knobX - 4, tlY - 4, 8, tlH + 8, Color.White);

            // Кнопки управления воспроизведением
            int btnY = barY + 50;
            if (Engine.DrawButton(new Rectangle(28, btnY, 78, 36), "|< 0:00", Rgb(32, 42, 64), Rgb(54, 72, 108), Color.White, 13f))
                _playTime = 0f;
            if (Engine.DrawButton(new Rectangle(114, btnY, 68, 36), "-5с", Rgb(32, 42, 64), Rgb(54, 72, 108), Color.White, 13.5f))
                _playTime = Math.Clamp(_playTime - 5f, 0f, _totalDuration);

            string playLbl = _paused ? "▶ ИГРАТЬ [SPACE]" : "|| ПАУЗА [SPACE]";
            if (Engine.DrawButton(new Rectangle(190, btnY, 174, 36), playLbl,
                _paused ? Rgb(36, 115, 68) : Rgb(135, 85, 28),
                _paused ? Rgb(52, 155, 92) : Rgb(175, 115, 42), Color.White, 14f, _paused ? Color.Lime : Color.Gold))
            {
                _paused = !_paused;
            }

            if (Engine.DrawButton(new Rectangle(372, btnY, 68, 36), "+5с", Rgb(32, 42, 64), Rgb(54, 72, 108), Color.White, 13.5f))
                _playTime = Math.Clamp(_playTime + 5f, 0f, _totalDuration);

            float[] speeds = { 0.25f, 0.5f, 1.0f, 2.0f, 4.0f };
            for (int si = 0; si < speeds.Length; si++)
            {
                bool activeSpd = MathF.Abs(_speed - speeds[si]) < 0.05f;
                string sLbl = speeds[si] < 1f ? $"{speeds[si]:0.##}x" : $"{(int)speeds[si]}x";
                if (Engine.DrawButton(new Rectangle(456 + si * 58, btnY, 52, 36), sLbl,
                    activeSpd ? Rgb(52, 98, 168) : Rgb(26, 34, 52), Rgb(72, 124, 200),
                    activeSpd ? Color.Yellow : Color.White, 13f, activeSpd ? Color.Yellow : null))
                {
                    _speed = speeds[si];
                }
            }

            int curM = (int)(_playTime / 60f), curS = (int)(_playTime % 60f);
            int totM = (int)(_totalDuration / 60f), totS = (int)(_totalDuration % 60f);
            Engine.DrawTextLeft($"ВРЕМЯ: {curM:00}:{curS:00} / {totM:00}:{totS:00}", 760, btnY + 9, 16f, Color.White);

            string hint = _camMode == 2
                ? "УПРАВЛЕНИЕ: Зажмите ПКМ — Обзор | WASD + E/Ctrl + Shift — Полёт | [H] Скрыть UI"
                : "УПРАВЛЕНИЕ: Зажмите ПКМ — Вращать камеру | Колесо — Зум | [C] Сменить камеру | [H] Скрыть UI";
            Engine.DrawTextLeft(hint, 965, btnY + 11, 12.5f, Rgb(165, 205, 245));

            return false;
        }
    }
}
