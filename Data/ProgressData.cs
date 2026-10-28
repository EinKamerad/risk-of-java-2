using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RiskOfCSharp2.Data
{
    public class SaveFileDto
    {
        [JsonPropertyName("kills")] public int Kills { get; set; }
        [JsonPropertyName("boss_kills")] public int BossKills { get; set; }
        [JsonPropertyName("max_stage")] public int MaxStage { get; set; } = 1;
        [JsonPropertyName("unlocked_items")] public List<string> UnlockedItems { get; set; } = new();
        [JsonPropertyName("unlocked_achievements")] public List<string> UnlockedAchievements { get; set; } = new();
        [JsonPropertyName("discovered_stages")] public List<string> DiscoveredStages { get; set; } = new();
        [JsonPropertyName("unlocked_skins")] public List<string> UnlockedSkins { get; set; } = new();
        [JsonPropertyName("unlocked_skills")] public List<string> UnlockedSkills { get; set; } = new();
        [JsonPropertyName("selected_skins")] public Dictionary<string, int> SelectedSkins { get; set; } = new();
        [JsonPropertyName("selected_loadouts")] public Dictionary<string, int[]> SelectedLoadouts { get; set; } = new();
        [JsonPropertyName("selected_difficulty")] public string SelectedDifficulty { get; set; } = "rainstorm";
        [JsonPropertyName("active_artifacts")] public List<string> ActiveArtifacts { get; set; } = new();
    }

    public class ProgressData
    {
        public static readonly ProgressData Instance = new();
        public static readonly string SavePath = Path.Combine(Directory.GetCurrentDirectory(), "save_logbook.json");

        public int Kills = 0;
        public int BossKills = 0;
        public int MaxStage = 1;
        public HashSet<string> UnlockedItems = new();
        public HashSet<string> UnlockedAchievements = new();
        public HashSet<string> DiscoveredStages = new() { "roost" };
        public HashSet<string> UnlockedSkins = new();
        public HashSet<string> UnlockedSkills = new();
        public Dictionary<string, int> SelectedSkins = new();
        public Dictionary<string, int[]> SelectedLoadouts = new();

        private ProgressData()
        {
            foreach (var kv in GameData.ItemPool)
            {
                if (kv.Value.DefaultUnlocked)
                    UnlockedItems.Add(kv.Key);
            }
            Load();
        }

        public int GetSelectedSkin(string charKey)
        {
            return SelectedSkins.TryGetValue(charKey, out int idx) ? Math.Max(0, idx) : 0;
        }

        public void SetSelectedSkin(string charKey, int skinIdx)
        {
            SelectedSkins[charKey] = Math.Max(0, skinIdx);
            Save();
        }

        public int[] GetLoadout(string charKey)
        {
            if (!SelectedLoadouts.TryGetValue(charKey, out var arr) || arr == null || arr.Length < 4)
            {
                arr = new int[] { 0, 0, 0, 0 };
                SelectedLoadouts[charKey] = arr;
            }
            return arr;
        }

        public int GetSkillVariant(string charKey, string slot)
        {
            int[] arr = GetLoadout(charKey);
            int idx = slot switch
            {
                "m1" => 0,
                "m2" => 1,
                "shift" => 2,
                "r" => 3,
                _ => 0
            };
            return IsSkillUnlocked(charKey, slot, arr[idx]) ? arr[idx] : 0;
        }

        public void SetSkillVariant(string charKey, string slot, int variantIdx)
        {
            if (!IsSkillUnlocked(charKey, slot, variantIdx)) return;
            int[] arr = GetLoadout(charKey);
            int idx = slot switch
            {
                "m1" => 0,
                "m2" => 1,
                "shift" => 2,
                "r" => 3,
                _ => 0
            };
            arr[idx] = Math.Max(0, variantIdx);
            Save();
        }

        public bool IsSkillUnlocked(string charKey, string slot, int variantIdx)
        {
            if (variantIdx <= 0) return true;
            return UnlockedSkills.Contains($"{charKey}_{slot}_{variantIdx}");
        }

        public bool IsSkinUnlocked(string charKey, int skinIdx, string? unlockAchKey)
        {
            if (skinIdx <= 0) return true;
            return UnlockedSkins.Contains($"{charKey}_{skinIdx}");
        }

        public void Load()
        {
            if (!File.Exists(SavePath)) return;
            try
            {
                string json = File.ReadAllText(SavePath);
                var dto = JsonSerializer.Deserialize<SaveFileDto>(json);
                if (dto == null) return;
                Kills = dto.Kills;
                BossKills = dto.BossKills;
                MaxStage = Math.Max(1, dto.MaxStage);
                foreach (var it in dto.UnlockedItems) UnlockedItems.Add(it);
                foreach (var ach in dto.UnlockedAchievements) UnlockedAchievements.Add(ach);
                foreach (var st in dto.DiscoveredStages) DiscoveredStages.Add(st);
                if (dto.UnlockedSkins != null)
                    foreach (var sk in dto.UnlockedSkins) UnlockedSkins.Add(sk);
                if (dto.UnlockedSkills != null)
                    foreach (var skl in dto.UnlockedSkills) UnlockedSkills.Add(skl);
                if (dto.SelectedSkins != null)
                    foreach (var kv in dto.SelectedSkins) SelectedSkins[kv.Key] = kv.Value;
                if (dto.SelectedLoadouts != null)
                    foreach (var kv in dto.SelectedLoadouts)
                        if (kv.Value != null && kv.Value.Length >= 4)
                            SelectedLoadouts[kv.Key] = kv.Value;
                if (!string.IsNullOrEmpty(dto.SelectedDifficulty) && GameData.Difficulties.ContainsKey(dto.SelectedDifficulty))
                    GameData.SelectedDifficulty = dto.SelectedDifficulty;
                if (dto.ActiveArtifacts != null)
                {
                    GameData.ActiveArtifacts.Clear();
                    foreach (var ak in dto.ActiveArtifacts)
                        if (GameData.RunArtifacts.ContainsKey(ak))
                            GameData.ActiveArtifacts.Add(ak);
                }
            }
            catch
            {
                // Игнорируем поврежденный JSON
            }
        }

        public void Save()
        {
            try
            {
                var dto = new SaveFileDto
                {
                    Kills = Kills,
                    BossKills = BossKills,
                    MaxStage = MaxStage,
                    UnlockedItems = UnlockedItems.ToList(),
                    UnlockedAchievements = UnlockedAchievements.ToList(),
                    DiscoveredStages = DiscoveredStages.ToList(),
                    UnlockedSkins = UnlockedSkins.ToList(),
                    UnlockedSkills = UnlockedSkills.ToList(),
                    SelectedSkins = SelectedSkins,
                    SelectedLoadouts = SelectedLoadouts,
                    SelectedDifficulty = GameData.SelectedDifficulty,
                    ActiveArtifacts = GameData.ActiveArtifacts.ToList()
                };
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(SavePath, JsonSerializer.Serialize(dto, opts));
            }
            catch
            {
                // Игнорируем ошибки записи
            }
        }

        public void UnlockAll()
        {
            foreach (var k in GameData.ItemPool.Keys) UnlockedItems.Add(k);
            foreach (var k in GameData.Achievements.Keys) UnlockedAchievements.Add(k);
            foreach (var p in GameData.StagePalettes) DiscoveredStages.Add(p.Id);
            foreach (var sp in GameData.SpecialPalettes.Keys) DiscoveredStages.Add(sp);
            foreach (var ck in Models.SurvivorDatabase.OrderedKeys)
            {
                UnlockedSkins.Add($"{ck}_1");
                UnlockedSkins.Add($"{ck}_2");
                UnlockedSkills.Add($"{ck}_m1_1");
                UnlockedSkills.Add($"{ck}_m2_1");
                UnlockedSkills.Add($"{ck}_shift_1");
                UnlockedSkills.Add($"{ck}_r_1");
            }
            Save();
        }
    }
}