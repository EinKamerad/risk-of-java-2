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

        private ProgressData()
        {
            foreach (var kv in GameData.ItemPool)
            {
                if (kv.Value.DefaultUnlocked)
                    UnlockedItems.Add(kv.Key);
            }
            Load();
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
                    DiscoveredStages = DiscoveredStages.ToList()
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
            Save();
        }
    }
}