using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Raylib_cs;
using RiskOfCSharp2.Core;

namespace RiskOfCSharp2.Data
{
    public class UserSettingsDto
    {
        [JsonPropertyName("mouse_sens")] public float MouseSens { get; set; } = 0.18f;
        [JsonPropertyName("fov")] public float Fov { get; set; } = 60f;
        [JsonPropertyName("master_volume")] public float MasterVolume { get; set; } = 0.65f;
        [JsonPropertyName("sfx_volume")] public float SfxVolume { get; set; } = 0.85f;
        [JsonPropertyName("ambient_volume")] public float AmbientVolume { get; set; } = 0.30f;
        [JsonPropertyName("muted")] public bool Muted { get; set; } = false;
        [JsonPropertyName("fullscreen")] public bool Fullscreen { get; set; } = false;
        [JsonPropertyName("window_width")] public int WindowWidth { get; set; } = 1536;
        [JsonPropertyName("window_height")] public int WindowHeight { get; set; } = 864;
        [JsonPropertyName("replay_save_mode")] public string ReplaySaveMode { get; set; } = "manual";
        [JsonPropertyName("compact_item_tooltips")] public bool CompactItemTooltips { get; set; } = false;
        [JsonPropertyName("selected_character")] public string SelectedCharacter { get; set; } = "commando";
        [JsonPropertyName("preferred_net_transport")] public string PreferredNetTransport { get; set; } = "steam";
    }

    /// <summary>
    /// Глобальные настройки игры и клиента (не сбрасываются при смене никнейма и сохраняются между сессиями).
    /// </summary>
    public sealed class UserSettings
    {
        public static readonly string SettingsPath = Path.Combine(ProgressData.AppRootDir, "user_settings.json");
        public static readonly string[] ReplayModesOrdered = { "manual", "victories", "always", "off" };

        public static readonly UserSettings Instance = new();

        public float MouseSens = 0.18f;
        public float Fov = 60f;
        public float MasterVolume = 0.65f;
        public float SfxVolume = 0.85f;
        public float AmbientVolume = 0.30f;
        public bool Muted = false;
        public bool Fullscreen = false;
        public int WindowWidth = 1536;
        public int WindowHeight = 864;
        /// <summary>
        /// "manual" — не сохранять на диск автоматически (доступна кнопка в итогах матча),
        /// "victories" — автосохранение только победных забегов,
        /// "always" — сохранять реплей каждого матча,
        /// "off" — полностью отключить запись кадров 3D-реплея.
        /// </summary>
        public string ReplaySaveMode = "manual";
        public bool CompactItemTooltips = false;
        public string SelectedCharacter = "commando";
        public string PreferredNetTransport = "steam";

        private UserSettings()
        {
            Load();
        }

        public static string NormalizeReplayMode(string? mode)
        {
            return mode switch
            {
                "off" => "off",
                "victories" => "victories",
                "always" => "always",
                _ => "manual"
            };
        }

        public string GetReplayModeDisplayLabel() => ReplaySaveMode switch
        {
            "off" => "ВЫКЛ (БЕЗ ЗАПИСИ)",
            "victories" => "ТОЛЬКО ПОБЕДЫ",
            "always" => "КАЖДЫЙ МАТЧ",
            _ => "ВРУЧНУЮ (В ИТОГАХ)"
        };

        public void CycleReplaySaveMode()
        {
            int idx = Array.IndexOf(ReplayModesOrdered, NormalizeReplayMode(ReplaySaveMode));
            ReplaySaveMode = ReplayModesOrdered[(idx + 1) % ReplayModesOrdered.Length];
            Save();
        }

        public bool ShouldCaptureReplayFrames()
            => NormalizeReplayMode(ReplaySaveMode) != "off";

        public bool ShouldAutoWriteReplayDisk(string outcome)
        {
            string m = NormalizeReplayMode(ReplaySaveMode);
            if (m == "always") return true;
            if (m == "victories") return outcome == "victory";
            return false;
        }

        public void Load()
        {
            if (!File.Exists(SettingsPath)) return;
            try
            {
                string json = File.ReadAllText(SettingsPath);
                var dto = JsonSerializer.Deserialize<UserSettingsDto>(json);
                if (dto == null) return;
                ApplyDto(dto);
            }
            catch
            {
                // Игнорируем повреждённый файл настроек
            }
        }

        public void ApplyDto(UserSettingsDto dto)
        {
            MouseSens = Math.Clamp(dto.MouseSens, 0.04f, 0.50f);
            Fov = Math.Clamp(dto.Fov, 45f, 95f);
            MasterVolume = Math.Clamp(dto.MasterVolume, 0f, 1f);
            SfxVolume = Math.Clamp(dto.SfxVolume, 0f, 1f);
            AmbientVolume = Math.Clamp(dto.AmbientVolume, 0f, 1f);
            Muted = dto.Muted;
            Fullscreen = dto.Fullscreen;
            WindowWidth = Math.Clamp(dto.WindowWidth > 0 ? dto.WindowWidth : 1536, 1024, 7680);
            WindowHeight = Math.Clamp(dto.WindowHeight > 0 ? dto.WindowHeight : 864, 600, 4320);
            ReplaySaveMode = NormalizeReplayMode(dto.ReplaySaveMode);
            CompactItemTooltips = dto.CompactItemTooltips;
            if (!string.IsNullOrWhiteSpace(dto.SelectedCharacter))
                SelectedCharacter = dto.SelectedCharacter.Trim();
            PreferredNetTransport = dto.PreferredNetTransport == "lan" ? "lan" : "steam";
        }

        public UserSettingsDto ToDto()
        {
            return new UserSettingsDto
            {
                MouseSens = MathF.Round(MouseSens, 4),
                Fov = MathF.Round(Fov, 1),
                MasterVolume = MathF.Round(MasterVolume, 3),
                SfxVolume = MathF.Round(SfxVolume, 3),
                AmbientVolume = MathF.Round(AmbientVolume, 3),
                Muted = Muted,
                Fullscreen = Fullscreen,
                WindowWidth = WindowWidth,
                WindowHeight = WindowHeight,
                ReplaySaveMode = NormalizeReplayMode(ReplaySaveMode),
                CompactItemTooltips = CompactItemTooltips,
                SelectedCharacter = SelectedCharacter,
                PreferredNetTransport = PreferredNetTransport == "lan" ? "lan" : "steam"
            };
        }

        public void ApplyToAudioAndWindow()
        {
            SoundManager.SetMasterVolume(MasterVolume);
            SoundManager.SetSfxVolume(SfxVolume);
            SoundManager.SetAmbientVolume(AmbientVolume);
            if (SoundManager.Muted != Muted)
                SoundManager.ToggleMute();

            try
            {
                Engine.SetFullscreenMode(Fullscreen, ref WindowWidth, ref WindowHeight);
            }
            catch
            {
            }
        }

        public void ToggleFullscreenAndApply()
        {
            try
            {
                bool nextFs = !Engine.IsFullscreenActive();
                Fullscreen = nextFs;
                Engine.SetFullscreenMode(nextFs, ref WindowWidth, ref WindowHeight);
                Save();
            }
            catch
            {
            }
        }

        public void SyncFromRuntime(float mouseSens, float fov, string selectedChar)
        {
            MouseSens = Math.Clamp(mouseSens, 0.04f, 0.50f);
            Fov = Math.Clamp(fov, 45f, 95f);
            MasterVolume = SoundManager.MasterVolume;
            SfxVolume = SoundManager.SfxVolume;
            AmbientVolume = SoundManager.AmbientVolume;
            Muted = SoundManager.Muted;
            try
            {
                if (Raylib.IsWindowReady())
                {
                    Fullscreen = Engine.IsFullscreenActive();
                    if (!Fullscreen)
                    {
                        int sw = Raylib.GetScreenWidth();
                        int sh = Raylib.GetScreenHeight();
                        if (sw >= 1024 && sh >= 600)
                        {
                            WindowWidth = sw;
                            WindowHeight = sh;
                        }
                    }
                }
            }
            catch
            {
            }
            if (!string.IsNullOrWhiteSpace(selectedChar))
                SelectedCharacter = selectedChar;
            Save();
        }

        public void Save()
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(ToDto(), opts));
            }
            catch
            {
                // Игнорируем ошибки записи
            }
        }
    }
}
