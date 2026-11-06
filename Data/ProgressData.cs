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
        [JsonPropertyName("player_name")] public string PlayerName { get; set; } = "";
        [JsonPropertyName("kills")] public int Kills { get; set; }
        [JsonPropertyName("boss_kills")] public int BossKills { get; set; }
        [JsonPropertyName("max_stage")] public int MaxStage { get; set; } = 1;
        [JsonPropertyName("unlocked_items")] public List<string> UnlockedItems { get; set; } = new();
        [JsonPropertyName("unlocked_characters")] public List<string> UnlockedCharacters { get; set; } = new();
        [JsonPropertyName("unlocked_achievements")] public List<string> UnlockedAchievements { get; set; } = new();
        [JsonPropertyName("discovered_stages")] public List<string> DiscoveredStages { get; set; } = new();
        [JsonPropertyName("unlocked_skins")] public List<string> UnlockedSkins { get; set; } = new();
        [JsonPropertyName("unlocked_skills")] public List<string> UnlockedSkills { get; set; } = new();
        [JsonPropertyName("selected_skins")] public Dictionary<string, int> SelectedSkins { get; set; } = new();
        [JsonPropertyName("selected_loadouts")] public Dictionary<string, int[]> SelectedLoadouts { get; set; } = new();
        [JsonPropertyName("selected_difficulty")] public string SelectedDifficulty { get; set; } = "rainstorm";
        [JsonPropertyName("active_artifacts")] public List<string> ActiveArtifacts { get; set; } = new();
        [JsonPropertyName("selected_character")] public string SelectedCharacter { get; set; } = "commando";
        [JsonPropertyName("settings")] public UserSettingsDto? Settings { get; set; }
        [JsonPropertyName("mithrix_defeated")] public bool MithrixDefeated { get; set; } = false;
        [JsonPropertyName("victories")] public int Victories { get; set; } = 0;
        [JsonPropertyName("lunar_coins")] public int LunarCoins { get; set; } = 15;
    }

    public class ProgressData
    {
        public static readonly string AppRootDir = ResolveAppRootDir();
        public static readonly string SavePath = Path.Combine(AppRootDir, "save_logbook.json");

        private static string ResolveAppRootDir()
        {
            try
            {
                string? procPath = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(procPath))
                {
                    string exeName = Path.GetFileNameWithoutExtension(procPath);
                    if (!string.Equals(exeName, "dotnet", StringComparison.OrdinalIgnoreCase))
                    {
                        string? dir = Path.GetDirectoryName(procPath);
                        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                            return dir;
                    }
                }
            }
            catch
            {
            }
            return Directory.GetCurrentDirectory();
        }

        // Список избранных игроков, которым доступен секретный персонаж SANABI (Бригадный Генерал).
        // Вы можете добавить сюда любые новые никнеймы прямо в коде:
        public static readonly string[] SanabiAllowedUsers =
        {
            "lddrakon",
            "EinKamerad"
        };

        // Список избранных игроков, которым доступен секретный персонаж «Трио Птиц» (Большая, Длинная и Маленькая Птицы):
        public static readonly string[] BirdTrioAllowedUsers =
        {
            "lddrakon",
            "EinKamerad",
            "pame127",
            "ShaD",
            "kris"
        };

        // Стартовый персонаж, доступный с самого первого забега (остальные 29 открываются уникальными испытаниями):
        public static readonly HashSet<string> DefaultCharacters = new()
        {
            "commando"
        };

        public static readonly ProgressData Instance = new();

        public string PlayerName = "";
        public int Kills = 0;
        public int BossKills = 0;
        public int MaxStage = 1;
        public bool MithrixDefeated = false;
        public int Victories = 0;
        public int LunarCoins = 15;
        public HashSet<string> UnlockedItems = new();
        public HashSet<string> UnlockedCharacters = new();
        public HashSet<string> UnlockedAchievements = new();
        public HashSet<string> DiscoveredStages = new() { "roost" };
        public HashSet<string> UnlockedSkins = new();
        public HashSet<string> UnlockedSkills = new();
        public Dictionary<string, int> SelectedSkins = new();
        public Dictionary<string, int[]> SelectedLoadouts = new();
        public string SelectedCharacter = "commando";

        private ProgressData()
        {
            SyncDefaultUnlocks();
            Load();
        }

        public void SyncDefaultUnlocks()
        {
            UnlockedItems.Clear();
            foreach (var kv in GameData.ItemPool)
            {
                if (kv.Value.DefaultUnlocked)
                    UnlockedItems.Add(kv.Key);
            }
            foreach (var ch in DefaultCharacters)
                UnlockedCharacters.Add(ch);

            foreach (var achKey in UnlockedAchievements)
            {
                if (GameData.Achievements.TryGetValue(achKey, out var ach))
                {
                    if (!string.IsNullOrEmpty(ach.UnlocksItem))
                        UnlockedItems.Add(ach.UnlocksItem);
                    if (!string.IsNullOrEmpty(ach.UnlocksCharacter))
                        UnlockedCharacters.Add(ach.UnlocksCharacter);
                }
            }
        }

        public bool IsSanabiAuthorized()
        {
            if (string.IsNullOrWhiteSpace(PlayerName)) return false;
            string trimmed = PlayerName.Trim();
            foreach (var allowed in SanabiAllowedUsers)
            {
                if (string.Equals(trimmed, allowed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public bool IsBirdTrioAuthorized()
        {
            if (string.IsNullOrWhiteSpace(PlayerName)) return false;
            string trimmed = PlayerName.Trim();
            foreach (var allowed in BirdTrioAllowedUsers)
            {
                if (string.Equals(trimmed, allowed, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public bool IsCharacterVisible(string charKey)
        {
            if (charKey == "apocalypse_bird")
                return IsCharacterUnlocked("apocalypse_bird") || IsCharacterUnlocked("bird_trio");
            return true;
        }

        public bool IsCharacterUnlocked(string charKey)
        {
            if (charKey == "sanabi")
                return IsSanabiAuthorized() || UnlockedCharacters.Contains("sanabi") || UnlockedAchievements.Contains("ach_char_sanabi");
            if (charKey == "bird_trio")
                return IsBirdTrioAuthorized() || UnlockedCharacters.Contains("bird_trio") || UnlockedAchievements.Contains("ach_char_bird_trio");
            if (charKey == "apocalypse_bird")
                return UnlockedCharacters.Contains("apocalypse_bird") || UnlockedAchievements.Contains("ach_char_apocalypse_bird");
            return DefaultCharacters.Contains(charKey) || UnlockedCharacters.Contains(charKey);
        }

        public void SetPlayerName(string name)
        {
            string nextName = (name ?? "").Trim();
            if (string.Equals(PlayerName, nextName, StringComparison.Ordinal))
            {
                Save();
                return;
            }
            // Сохраняем открытыми секретных персонажей, если они уже были активированы в профиле,
            // чтобы смена никнейма не сбрасывала выбранного героя, облики, навыки и конфигурацию.
            if (IsSanabiAuthorized()) UnlockedCharacters.Add("sanabi");
            if (IsBirdTrioAuthorized()) UnlockedCharacters.Add("bird_trio");
            PlayerName = nextName;
            Save();
            UserSettings.Instance.Save();
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

        public string GetLoadoutProfileKey(string charKey)
        {
            int skinIdx = GetSelectedSkin(charKey);
            if (skinIdx > 0 &&
                Models.SurvivorDatabase.Classes.TryGetValue(charKey, out var cfg) &&
                skinIdx < cfg.Skins.Count &&
                cfg.Skins[skinIdx].CustomSkillVariants != null)
            {
                return $"{charKey}_s{skinIdx}";
            }
            return charKey;
        }

        public int[] GetLoadout(string charKey)
        {
            string profileKey = GetLoadoutProfileKey(charKey);
            if (!SelectedLoadouts.TryGetValue(profileKey, out var arr) || arr == null || arr.Length < 4)
            {
                arr = new int[] { 0, 0, 0, 0 };
                SelectedLoadouts[profileKey] = arr;
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
            if (charKey == "sanabi" && IsCharacterUnlocked("sanabi")) return true;
            if (charKey == "bird_trio" && IsCharacterUnlocked("bird_trio")) return true;
            if (charKey == "apocalypse_bird" && IsCharacterUnlocked("apocalypse_bird")) return true;
            string profileKey = GetLoadoutProfileKey(charKey);
            return UnlockedSkills.Contains($"{profileKey}_{slot}_{variantIdx}");
        }

        public bool IsSkinUnlocked(string charKey, int skinIdx, string? unlockAchKey)
        {
            if (skinIdx <= 0) return true;
            if (charKey == "sanabi" && IsCharacterUnlocked("sanabi")) return true;
            if (charKey == "bird_trio" && IsCharacterUnlocked("bird_trio")) return true;
            if (!string.IsNullOrEmpty(unlockAchKey) &&
                unlockAchKey.StartsWith("ach_persona_", StringComparison.Ordinal) &&
                UnlockedAchievements.Contains(unlockAchKey))
            {
                return true;
            }
            if (Models.SurvivorDatabase.Classes.TryGetValue(charKey, out var cfg) &&
                skinIdx < cfg.Skins.Count &&
                cfg.Skins[skinIdx].UnlockedByDefault)
            {
                return true;
            }
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
                PlayerName = (dto.PlayerName ?? "").Trim();
                Kills = dto.Kills;
                BossKills = dto.BossKills;
                MaxStage = Math.Max(1, dto.MaxStage);
                MithrixDefeated = dto.MithrixDefeated || dto.Victories > 0;
                Victories = Math.Max(0, dto.Victories);
                LunarCoins = dto.LunarCoins > 0 ? dto.LunarCoins : 15;
                if (dto.UnlockedAchievements != null)
                {
                    foreach (var ach in dto.UnlockedAchievements) UnlockedAchievements.Add(ach);
                    if (UnlockedAchievements.Contains("ach_safe_travels"))
                        MithrixDefeated = true;
                }
                if (dto.UnlockedCharacters != null)
                    foreach (var ch in dto.UnlockedCharacters) UnlockedCharacters.Add(ch);
                SyncDefaultUnlocks();
                if (dto.DiscoveredStages != null)
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
                if (!string.IsNullOrWhiteSpace(dto.SelectedCharacter))
                    SelectedCharacter = dto.SelectedCharacter.Trim();
                if (!File.Exists(UserSettings.SettingsPath))
                {
                    if (dto.Settings != null)
                        UserSettings.Instance.ApplyDto(dto.Settings);
                    else if (!string.IsNullOrWhiteSpace(dto.SelectedCharacter))
                        UserSettings.Instance.SelectedCharacter = dto.SelectedCharacter;
                    UserSettings.Instance.Save();
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
                    PlayerName = PlayerName,
                    Kills = Kills,
                    BossKills = BossKills,
                    MaxStage = MaxStage,
                    UnlockedItems = UnlockedItems.ToList(),
                    UnlockedCharacters = UnlockedCharacters.ToList(),
                    UnlockedAchievements = UnlockedAchievements.ToList(),
                    DiscoveredStages = DiscoveredStages.ToList(),
                    UnlockedSkins = UnlockedSkins.ToList(),
                    UnlockedSkills = UnlockedSkills.ToList(),
                    SelectedSkins = SelectedSkins,
                    SelectedLoadouts = SelectedLoadouts,
                    SelectedDifficulty = GameData.SelectedDifficulty,
                    ActiveArtifacts = GameData.ActiveArtifacts.ToList(),
                    SelectedCharacter = UserSettings.Instance.SelectedCharacter,
                    Settings = UserSettings.Instance.ToDto(),
                    MithrixDefeated = MithrixDefeated,
                    Victories = Victories,
                    LunarCoins = LunarCoins
                };
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(SavePath, JsonSerializer.Serialize(dto, opts));
                UserSettings.Instance.Save();
            }
            catch
            {
                // Игнорируем ошибки записи
            }
        }
    }
}
