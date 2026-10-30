using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Raylib_cs;

namespace RiskOfCSharp2.Core
{
    /// <summary>
    /// Процедурный генератор и менеджер звука (100% синтез на лету в оперативной памяти без внешних файлов).
    /// Генерирует 16-битные PCM-волны (44100 Гц) при запуске с использованием осцилляторов,
    /// частотной модуляции (FM), фильтрации нижних частот, шумовых огибающих и аккордов.
    /// </summary>
    public static unsafe class SoundManager
    {
        private const int SampleRate = 44100;
        private const int PoolSize = 4; // Пул алиасов для полифонии без обрывания звука

        private class SoundEntry
        {
            public Sound BaseSound;
            public Sound[] Aliases = new Sound[PoolSize];
            public int NextIdx;
            public float LastPlayTime;
            public float MinInterval;
            public float BaseVolume;
        }

        private static readonly Dictionary<string, SoundEntry> _sounds = new();
        private static readonly Random _rng = new(1337);
        private static bool _initialized = false;

        // Настройки громкости (доступны в меню настроек и паузы)
        public static float MasterVolume { get; private set; } = 0.85f;
        public static float SfxVolume { get; private set; } = 0.80f;
        public static float AmbientVolume { get; private set; } = 0.35f;
        public static bool Muted { get; private set; } = false;

        // Фоновые процедурные эмбиент-петли
        private static Sound _ambientExplore;
        private static Sound _ambientBoss;
        private static bool _hasAmbient = false;
        private static float _ambientTimer = 0f;
        private static bool _lastBossMode = false;

        public static void Init()
        {
            if (_initialized) return;
            try
            {
                Raylib.InitAudioDevice();
                if (!Raylib.IsAudioDeviceReady())
                {
                    Console.WriteLine("[SoundManager] Аудиоустройство недоступно — работа в беззвучном режиме.");
                    return;
                }
                Raylib.SetMasterVolume(MasterVolume);
                _initialized = true;
                GenerateAllSounds();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SoundManager] Ошибка инициализации аудио: {ex.Message}");
                _initialized = false;
            }
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            try
            {
                foreach (var kv in _sounds.Values)
                {
                    for (int i = 0; i < PoolSize; i++)
                        Raylib.UnloadSoundAlias(kv.Aliases[i]);
                    Raylib.UnloadSound(kv.BaseSound);
                }
                _sounds.Clear();
                if (_hasAmbient)
                {
                    Raylib.UnloadSound(_ambientExplore);
                    Raylib.UnloadSound(_ambientBoss);
                    _hasAmbient = false;
                }
                if (Raylib.IsAudioDeviceReady())
                    Raylib.CloseAudioDevice();
            }
            catch { }
            _initialized = false;
        }

        public static void SetMasterVolume(float vol)
        {
            MasterVolume = Math.Clamp(vol, 0f, 1f);
            if (_initialized && Raylib.IsAudioDeviceReady())
                Raylib.SetMasterVolume(Muted ? 0f : MasterVolume);
        }

        public static void SetSfxVolume(float vol)
        {
            SfxVolume = Math.Clamp(vol, 0f, 1f);
        }

        public static void SetAmbientVolume(float vol)
        {
            AmbientVolume = Math.Clamp(vol, 0f, 1f);
            if (_initialized && _hasAmbient)
            {
                Raylib.SetSoundVolume(_ambientExplore, AmbientVolume * 0.45f);
                Raylib.SetSoundVolume(_ambientBoss, AmbientVolume * 0.55f);
            }
        }

        public static void ToggleMute()
        {
            Muted = !Muted;
            SetMasterVolume(MasterVolume);
        }

        /// <summary>
        /// Воспроизводит процедурный звук по ключу с лёгкой вариацией высоты тона.
        /// </summary>
        public static void Play(string key, float volumeScale = 1.0f, float pitch = 1.0f, float pitchVariance = 0.05f)
        {
            if (!_initialized || Muted || SfxVolume <= 0.001f) return;
            if (!_sounds.TryGetValue(key, out var entry)) return;

            float now = (float)Raylib.GetTime();
            if (now - entry.LastPlayTime < entry.MinInterval) return;
            entry.LastPlayTime = now;

            Sound snd = entry.Aliases[entry.NextIdx];
            entry.NextIdx = (entry.NextIdx + 1) % PoolSize;

            float varPitch = pitch + ((float)_rng.NextDouble() * 2f - 1f) * pitchVariance;
            varPitch = Math.Clamp(varPitch, 0.35f, 2.5f);
            float finalVol = Math.Clamp(entry.BaseVolume * volumeScale * SfxVolume, 0f, 1f);

            Raylib.SetSoundPitch(snd, varPitch);
            Raylib.SetSoundVolume(snd, finalVol);
            Raylib.PlaySound(snd);
        }

        /// <summary>
        /// Воспроизводит звук в 3D-пространстве с затуханием от расстояния до игрока.
        /// </summary>
        public static void Play3D(string key, Vector3 sourcePos, Vector3 listenerPos, float maxDist = 55f, float volumeScale = 1.0f, float pitch = 1.0f)
        {
            if (!_initialized || Muted || SfxVolume <= 0.001f) return;
            float dist = Vector3.Distance(sourcePos, listenerPos);
            if (dist >= maxDist) return;
            float atten = 1.0f - (dist / maxDist);
            atten = atten * atten; // Квадратичное затухание
            if (atten <= 0.03f) return;
            Play(key, volumeScale * atten, pitch);
        }

        /// <summary>
        /// Обновляет фоновый процедурный эмбиент (космический синт-дрон или боевой пульс босса).
        /// </summary>
        public static void UpdateAmbient(float dt, bool inGame, bool bossActive)
        {
            if (!_initialized || !_hasAmbient) return;
            if (Muted || AmbientVolume <= 0.005f || !inGame)
            {
                if (Raylib.IsSoundPlaying(_ambientExplore)) Raylib.StopSound(_ambientExplore);
                if (Raylib.IsSoundPlaying(_ambientBoss)) Raylib.StopSound(_ambientBoss);
                return;
            }

            if (bossActive != _lastBossMode)
            {
                _lastBossMode = bossActive;
                if (bossActive)
                {
                    if (Raylib.IsSoundPlaying(_ambientExplore)) Raylib.StopSound(_ambientExplore);
                }
                else
                {
                    if (Raylib.IsSoundPlaying(_ambientBoss)) Raylib.StopSound(_ambientBoss);
                }
            }

            _ambientTimer -= dt;
            if (bossActive)
            {
                if (!Raylib.IsSoundPlaying(_ambientBoss))
                {
                    Raylib.SetSoundVolume(_ambientBoss, AmbientVolume * 0.55f);
                    Raylib.PlaySound(_ambientBoss);
                }
            }
            else
            {
                if (!Raylib.IsSoundPlaying(_ambientExplore))
                {
                    Raylib.SetSoundVolume(_ambientExplore, AmbientVolume * 0.45f);
                    Raylib.PlaySound(_ambientExplore);
                }
            }
        }

        private static void Register(string key, float[] samples, float baseVolume = 0.65f, float minInterval = 0.025f)
        {
            int count = samples.Length;
            short* pcm = (short*)Marshal.AllocHGlobal(count * sizeof(short));
            try
            {
                // Мягкая фильтрация и нормализация без клиппинга
                for (int i = 0; i < count; i++)
                {
                    float s = MathF.Tanh(samples[i]);
                    pcm[i] = (short)Math.Clamp((int)(s * 32000f), short.MinValue, short.MaxValue);
                }

                Wave wave = new Wave
                {
                    SampleCount = (uint)count,
                    SampleRate = SampleRate,
                    SampleSize = 16,
                    Channels = 1,
                    Data = pcm
                };

                Sound baseSound = Raylib.LoadSoundFromWave(wave);
                var entry = new SoundEntry
                {
                    BaseSound = baseSound,
                    BaseVolume = baseVolume,
                    MinInterval = minInterval,
                    LastPlayTime = -10f
                };
                for (int i = 0; i < PoolSize; i++)
                    entry.Aliases[i] = Raylib.LoadSoundAlias(baseSound);

                _sounds[key] = entry;
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)pcm);
            }
        }

        private static Sound CreateRawSound(float[] samples)
        {
            int count = samples.Length;
            short* pcm = (short*)Marshal.AllocHGlobal(count * sizeof(short));
            try
            {
                for (int i = 0; i < count; i++)
                {
                    float s = MathF.Tanh(samples[i]);
                    pcm[i] = (short)Math.Clamp((int)(s * 32000f), short.MinValue, short.MaxValue);
                }

                Wave wave = new Wave
                {
                    SampleCount = (uint)count,
                    SampleRate = SampleRate,
                    SampleSize = 16,
                    Channels = 1,
                    Data = pcm
                };
                return Raylib.LoadSoundFromWave(wave);
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)pcm);
            }
        }

        // =================== БАЗОВЫЕ ОСЦИЛЛЯТОРЫ И ФИЛЬТРЫ ===================

        private static float Sine(float phase) => MathF.Sin(phase * MathF.PI * 2f);
        private static float Square(float phase) => MathF.Sin(phase * MathF.PI * 2f) >= 0f ? 0.8f : -0.8f;
        private static float Saw(float phase) => 2f * (phase - MathF.Floor(phase + 0.5f));
        private static float Triangle(float phase) => 2f * MathF.Abs(2f * (phase - MathF.Floor(phase + 0.5f))) - 1f;

        private static float EnvADSR(float t, float dur, float attack, float decay, float sustainLevel, float release)
        {
            if (t < 0f || t >= dur) return 0f;
            if (t < attack) return t / MathF.Max(0.0005f, attack);
            if (t < attack + decay)
            {
                float d = (t - attack) / MathF.Max(0.0005f, decay);
                return 1f + (sustainLevel - 1f) * d;
            }
            float releaseStart = MathF.Max(attack + decay, dur - release);
            if (t < releaseStart) return sustainLevel;
            float r = (t - releaseStart) / MathF.Max(0.0005f, dur - releaseStart);
            return sustainLevel * (1f - r);
        }

        private static void LowPassFilter(float[] buf, float alpha)
        {
            if (buf.Length == 0) return;
            float prev = buf[0];
            for (int i = 1; i < buf.Length; i++)
            {
                prev = prev + alpha * (buf[i] - prev);
                buf[i] = prev;
            }
        }

        // =================== СИНТЕЗ ВСЕХ ЗВУКОВ ИГРЫ ===================

        private static void GenerateAllSounds()
        {
            var noiseRng = new Random(42);
            float NextNoise() => (float)noiseRng.NextDouble() * 2f - 1f;

            // 1. UI: Клик кнопки (чёткий sci-fi импульс)
            Register("ui_click", Synth(0.065f, (t, dur) =>
            {
                float env = MathF.Pow(1f - t / dur, 2.2f);
                float freq = 1150f + (t < 0.018f ? 0f : 420f);
                return (Sine(t * freq) * 0.7f + Triangle(t * freq * 2f) * 0.3f) * env;
            }), baseVolume: 0.45f, minInterval: 0.02f);

            // 2. UI: Выбор персонажа в лобби (аккорд-перелив)
            Register("ui_select_char", Synth(0.28f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.01f, 0.08f, 0.55f, 0.14f);
                float f1 = 440f, f2 = 554.37f, f3 = 659.25f, f4 = 880f;
                float w = Sine(t * f1) * 0.35f + Sine(t * f2) * 0.30f + Sine(t * f3) * 0.30f;
                if (t > 0.06f) w += Sine(t * f4) * 0.35f;
                return w * env;
            }), baseVolume: 0.50f, minInterval: 0.05f);

            // 3. UI: Ошибка / нехватка золота / перезарядка
            Register("ui_error", Synth(0.22f, (t, dur) =>
            {
                float env = t < 0.09f || (t > 0.11f && t < 0.21f) ? 0.8f : 0.05f;
                float fade = 1f - (t / dur) * 0.4f;
                float f = t < 0.10f ? 195f : 155f;
                return (Saw(t * f) * 0.55f + Square(t * f * 0.5f) * 0.35f) * env * fade;
            }, lowPass: 0.25f), baseVolume: 0.48f, minInterval: 0.12f);

            // 4. UI: Готовность / Подтверждение
            Register("ui_ready", Synth(0.32f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.008f, 0.07f, 0.6f, 0.15f);
                float f = t < 0.10f ? 523.25f : (t < 0.20f ? 659.25f : 783.99f);
                return (Sine(t * f) * 0.65f + Triangle(t * f * 2f) * 0.25f) * env;
            }), baseVolume: 0.52f, minInterval: 0.05f);

            // 5. UI: Тик таймера отсчёта
            Register("ui_tick", Synth(0.08f, (t, dur) =>
            {
                float env = MathF.Pow(1f - t / dur, 2.5f);
                return (Sine(t * 880f) * 0.75f + Sine(t * 1760f) * 0.25f) * env;
            }), baseVolume: 0.45f, minInterval: 0.05f);

            // 6. Старт забега / телепортация на локацию
            Register("run_start", Synth(0.75f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.05f, 0.20f, 0.65f, 0.35f);
                float sweep = 110f + 330f * MathF.Pow(t / dur, 1.5f);
                float chord = Sine(t * 220f) * 0.3f + Sine(t * 329.6f) * 0.25f + Sine(t * 440f) * 0.25f + Saw(t * sweep) * 0.25f;
                float whoosh = NextNoise() * 0.25f * MathF.Sin((t / dur) * MathF.PI);
                return (chord + whoosh) * env;
            }, lowPass: 0.30f), baseVolume: 0.62f, minInterval: 0.2f);

            // 7. Прыжок
            Register("jump", Synth(0.16f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Sin(p * MathF.PI) * (1f - p * 0.4f);
                float phase = 160f * t + 260f * t * t / dur;
                return (Sine(phase) * 0.7f + NextNoise() * 0.18f) * env;
            }, lowPass: 0.35f), baseVolume: 0.42f, minInterval: 0.06f);

            // 8. Двойной прыжок (Перо Хопу)
            Register("double_jump", Synth(0.20f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.4f);
                float phase = 320f * t + 480f * t * t / dur;
                return (Sine(phase) * 0.65f + Triangle(phase * 1.5f) * 0.30f + NextNoise() * 0.15f) * env;
            }, lowPass: 0.45f), baseVolume: 0.46f, minInterval: 0.06f);

            // 9. Прыжковая платформа (Гейзер / BouncePad)
            Register("bounce_pad", Synth(0.38f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.3f);
                float phase = 120f * t + 620f * MathF.Pow(p, 1.7f) * t;
                float vib = 1f + 0.15f * MathF.Sin(t * 55f);
                return (Sine(phase * vib) * 0.65f + Saw(phase * 0.5f) * 0.25f + NextNoise() * 0.25f * (1f - p)) * env;
            }, lowPass: 0.35f), baseVolume: 0.60f, minInterval: 0.10f);

            // 10. Рывок (Dash / Shift)
            Register("dash", Synth(0.22f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.5f);
                float sub = Sine(180f * t - 90f * t * p) * 0.65f;
                float wind = NextNoise() * 0.65f * MathF.Sin(p * MathF.PI);
                return (sub + wind) * env;
            }, lowPass: 0.22f), baseVolume: 0.55f, minInterval: 0.06f);

            // 11. Крюк-кошка (Скаут / V1 Whiplash)
            Register("grapple", Synth(0.26f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.3f);
                float zip = Sine(420f * t + 950f * t * p) * 0.55f;
                float click = MathF.Sin(t * 280f) > 0.3f ? 0.35f : -0.35f;
                return (zip + click * 0.4f + NextNoise() * 0.25f) * env;
            }, lowPass: 0.45f), baseVolume: 0.52f, minInterval: 0.08f);

            // 12. Шаги игрока (мягкий низкочастотный стук)
            Register("footstep", Synth(0.065f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 2.8f);
                return (Sine(95f * t - 40f * t * p) * 0.7f + NextNoise() * 0.4f) * env;
            }, lowPass: 0.18f), baseVolume: 0.24f, minInterval: 0.16f);

            // 13. Тяжёлое приземление / удар по земле (Чебурашка, Сол)
            Register("slam_land", Synth(0.42f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.6f);
                float sub = Sine(115f * t - 70f * t * p) * 0.85f;
                float crunch = NextNoise() * 0.75f * MathF.Pow(1f - p, 1.2f);
                return (sub + crunch) * env;
            }, lowPass: 0.20f), baseVolume: 0.72f, minInterval: 0.08f);

            // 14. Лёгкий выстрел (Commando, M4A1, Scout, Турель)
            Register("shoot_light", Synth(0.11f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 2.2f);
                float punch = Sine(480f * t - 310f * t * p) * 0.70f;
                float snap = NextNoise() * 0.65f * MathF.Pow(1f - p, 3.0f);
                return (punch + snap) * env;
            }, lowPass: 0.38f), baseVolume: 0.42f, minInterval: 0.032f);

            // 15. Тяжёлый выстрел / Дробовик / Револьвер (V1, Силовик, Айгис)
            Register("shoot_heavy", Synth(0.24f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.7f);
                float body = Sine(220f * t - 140f * t * p) * 0.80f + Saw(110f * t) * 0.35f;
                float blast = NextNoise() * 0.85f * MathF.Pow(1f - p, 2.0f);
                return (body + blast) * env;
            }, lowPass: 0.28f), baseVolume: 0.58f, minInterval: 0.045f);

            // 16. Рельсотрон / Гаусс / Луч Эгона / Орбитальный удар
            Register("railgun", Synth(0.48f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.35f);
                float beam = Saw(880f * t - 520f * t * p) * 0.50f + Sine(140f * t - 60f * t * p) * 0.65f;
                float zap = Sine(t * 2400f * (1f - 0.5f * p)) * 0.28f * (1f - p);
                float noise = NextNoise() * 0.50f * MathF.Pow(1f - p, 1.8f);
                return (beam + zap + noise) * env;
            }, lowPass: 0.42f), baseVolume: 0.68f, minInterval: 0.06f);

            // 17. Запуск ракеты (V1 Rocket, AtG, Plasma Shrimp, Фейерверк)
            Register("rocket", Synth(0.26f, (t, dur) =>
            {
                float p = t / dur;
                float env = EnvADSR(t, dur, 0.015f, 0.06f, 0.65f, 0.14f);
                float tone = Sine(210f * t + 180f * t * p) * 0.45f;
                float hiss = NextNoise() * 0.70f;
                return (tone + hiss) * env;
            }, lowPass: 0.26f), baseVolume: 0.48f, minInterval: 0.045f);

            // 18. Лазер / Энергетический выстрел (Базз, Изобретательница, Рейму, Медик)
            Register("laser", Synth(0.14f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.8f);
                float phase = 980f * t - 620f * t * p;
                return (Sine(phase) * 0.65f + Saw(phase * 0.5f) * 0.35f) * env;
            }, lowPass: 0.50f), baseVolume: 0.42f, minInterval: 0.035f);

            // 19. Взмах оружием ближнего боя (Меч Сола, Кинжал Сларка, Якорь, Монтировка)
            Register("melee_swing", Synth(0.16f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Sin(p * MathF.PI) * (1f - p * 0.3f);
                float swoop = Sine(290f * t - 160f * t * p) * 0.55f;
                float air = NextNoise() * 0.65f;
                return (swoop + air) * env;
            }, lowPass: 0.24f), baseVolume: 0.50f, minInterval: 0.04f);

            // 20. Ножницы Гвен (металлический лязг «Чик-чик!»)
            Register("scissor_snip", Synth(0.11f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 2.4f);
                float metal = Sine(t * 1850f) * 0.45f + Sine(t * 2740f) * 0.35f + Sine(t * 3620f) * 0.25f;
                float scrape = NextNoise() * 0.45f * MathF.Pow(1f - p, 3.0f);
                return (metal + scrape) * env;
            }, lowPass: 0.68f), baseVolume: 0.46f, minInterval: 0.03f);

            // 21. Огонь / Пламя (Огненный шар, Gun Flame, Огнемёт)
            Register("flame", Synth(0.28f, (t, dur) =>
            {
                float p = t / dur;
                float env = EnvADSR(t, dur, 0.02f, 0.08f, 0.6f, 0.14f);
                float roar = Sine(135f * t - 45f * t * p) * 0.55f + NextNoise() * 0.75f;
                return roar * env;
            }, lowPass: 0.20f), baseVolume: 0.52f, minInterval: 0.05f);

            // 22. Заморозка / Крио-взрыв (Крио-сфера, Крио-граната, Ледяная реликвия)
            Register("ice_freeze", Synth(0.32f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.5f);
                float crystal = Sine(t * 1560f) * 0.35f + Sine(t * 2340f) * 0.30f + Sine(t * 3120f) * 0.25f;
                float crack = NextNoise() * 0.45f * MathF.Pow(1f - p, 2.2f);
                return (crystal + crack) * env;
            }, lowPass: 0.60f), baseVolume: 0.52f, minInterval: 0.06f);

            // 23. Магический каст (Ари, Рейму, Сфера Искажения)
            Register("magic_cast", Synth(0.24f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.5f);
                float f = 520f + 380f * p + MathF.Sin(t * 45f) * 35f;
                return (Sine(t * f) * 0.6f + Triangle(t * f * 1.5f) * 0.3f) * env;
            }), baseVolume: 0.46f, minInterval: 0.045f);

            // 24. Очарование Ари (Charm)
            Register("charm_heart", Synth(0.36f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.01f, 0.08f, 0.65f, 0.18f);
                float f = t < 0.10f ? 587.33f : (t < 0.20f ? 739.99f : 880f);
                float vibrato = 1f + 0.015f * MathF.Sin(t * 38f);
                return (Sine(t * f * vibrato) * 0.65f + Sine(t * f * 2f) * 0.25f) * env;
            }), baseVolume: 0.54f, minInterval: 0.08f);

            // 25. Подбрасывание монетки V1 (Marksman Coin Flip)
            Register("coin_flip", Synth(0.24f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.8f);
                float ring = Sine(t * 2480f) * 0.50f + Sine(t * 3720f) * 0.35f + Sine(t * 4960f) * 0.20f;
                return ring * env;
            }), baseVolume: 0.54f, minInterval: 0.03f);

            // 26. Рикошет от монетки V1 (+RICOSHOT PING)
            Register("coin_ricoshot", Synth(0.34f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.5f);
                float f = t < 0.04f ? 1980f : 2960f;
                float ping = Sine(t * f) * 0.60f + Sine(t * f * 1.5f) * 0.30f + Triangle(t * 880f) * 0.25f;
                return ping * env;
            }), baseVolume: 0.65f, minInterval: 0.03f);

            // 27. Парирование / Блок щитом / Мишка (+PARRY)
            Register("parry", Synth(0.35f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.4f);
                float clang = Sine(t * 920f) * 0.45f + Sine(t * 1380f) * 0.38f + Square(t * 460f) * 0.25f;
                float impact = NextNoise() * 0.55f * MathF.Pow(1f - p, 3.2f);
                return (clang + impact) * env;
            }, lowPass: 0.55f), baseVolume: 0.68f, minInterval: 0.05f);

            // 28. Исцеление (Аптечка, Медик, Святилище Леса, Bungus)
            Register("heal", Synth(0.28f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.015f, 0.07f, 0.55f, 0.14f);
                float f = t < 0.12f ? 523.25f : 659.25f;
                return (Sine(t * f) * 0.65f + Sine(t * f * 1.5f) * 0.25f) * env;
            }), baseVolume: 0.42f, minInterval: 0.09f);

            // 29. Активация Ультимейта [R]
            Register("ult_cast", Synth(0.62f, (t, dur) =>
            {
                float p = t / dur;
                float env = EnvADSR(t, dur, 0.02f, 0.14f, 0.70f, 0.28f);
                float sweep = 140f + 420f * MathF.Pow(p, 1.3f);
                float chord = Saw(t * 220f) * 0.30f + Sine(t * 330f) * 0.30f + Sine(t * 440f) * 0.30f + Sine(t * sweep) * 0.35f;
                float sub = Sine(95f * t) * 0.45f * (1f - p);
                return (chord + sub) * env;
            }, lowPass: 0.38f), baseVolume: 0.70f, minInterval: 0.12f);

            // 30. Часы Данте (Бой курантов / Поворот часов)
            Register("dante_clock", Synth(0.55f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.4f);
                float bell = Sine(t * 440f) * 0.45f + Sine(t * 880f) * 0.30f + Sine(t * 1188f) * 0.25f + Sine(t * 220f) * 0.40f;
                float tick = t < 0.03f ? NextNoise() * 0.5f : 0f;
                return (bell + tick) * env;
            }), baseVolume: 0.64f, minInterval: 0.08f);

            // 31. Постройка здания Mindustry
            Register("build_place", Synth(0.18f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.8f);
                float f = t < 0.07f ? 280f : 420f;
                return (Square(t * f) * 0.45f + Sine(t * f * 2f) * 0.35f + NextNoise() * 0.3f * (1f - p)) * env;
            }, lowPass: 0.35f), baseVolume: 0.50f, minInterval: 0.04f);

            // 32. Попадание по врагу (Hitmarker)
            Register("hit", Synth(0.055f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 2.8f);
                return (Sine(t * 680f - 280f * t * p) * 0.65f + NextNoise() * 0.45f) * env;
            }, lowPass: 0.42f), baseVolume: 0.34f, minInterval: 0.025f);

            // 33. Критическое попадание (Crit Crunch)
            Register("crit", Synth(0.10f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 2.1f);
                float ring = Sine(t * 1240f) * 0.50f + Sine(t * 1860f) * 0.35f;
                float punch = Sine(320f * t - 160f * t * p) * 0.55f + NextNoise() * 0.45f * (1f - p);
                return (ring + punch) * env;
            }, lowPass: 0.52f), baseVolume: 0.48f, minInterval: 0.03f);

            // 34. Взрыв (AoE, Канистра, Огонёк, Бегемот, Бочка, Ядерный взрыв)
            Register("explosion", Synth(0.46f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.45f);
                float sub = Sine(95f * t - 58f * t * p) * 0.90f;
                float rumble = NextNoise() * 0.95f * MathF.Pow(1f - p, 1.2f);
                return (sub + rumble) * env;
            }, lowPass: 0.16f), baseVolume: 0.64f, minInterval: 0.05f);

            // 35. Электрический разряд (Укулеле, Полилютня, Катушка Теслы, Заточка Статикка)
            Register("zap", Synth(0.17f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.8f);
                float mod = MathF.Sin(t * 190f) * 420f;
                float spark = Saw(t * (950f + mod)) * 0.55f + NextNoise() * 0.45f;
                return spark * env;
            }, lowPass: 0.48f), baseVolume: 0.44f, minInterval: 0.04f);

            // 36. Получение урона игроком
            Register("player_hurt", Synth(0.22f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.6f);
                float thud = Saw(165f * t - 85f * t * p) * 0.70f + Sine(85f * t) * 0.55f;
                float crack = NextNoise() * 0.60f * MathF.Pow(1f - p, 2.2f);
                return (thud + crack) * env;
            }, lowPass: 0.25f), baseVolume: 0.62f, minInterval: 0.08f);

            // 37. Золотой Стазис (Песочные Часы Жони)
            Register("zhonyas", Synth(0.55f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.25f);
                float chime = Sine(t * 1046.5f) * 0.45f + Sine(t * 1318.5f) * 0.35f + Sine(t * 1568.0f) * 0.35f + Sine(t * 2093.0f) * 0.25f;
                return chime * env;
            }), baseVolume: 0.68f, minInterval: 0.1f);

            // 38. Поражение / Смерть игрока
            Register("player_death", Synth(0.85f, (t, dur) =>
            {
                float p = t / dur;
                float env = EnvADSR(t, dur, 0.02f, 0.20f, 0.65f, 0.40f);
                float f = 260f * MathF.Pow(1f - p * 0.72f, 1.4f);
                return (Saw(t * f) * 0.55f + Sine(t * f * 0.5f) * 0.55f + NextNoise() * 0.25f * (1f - p)) * env;
            }, lowPass: 0.22f), baseVolume: 0.72f, minInterval: 0.2f);

            // 39. Появление врага
            Register("enemy_spawn", Synth(0.24f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Sin(p * MathF.PI);
                float f = 120f + 160f * p;
                return (Sine(t * f) * 0.65f + Triangle(t * f * 1.5f) * 0.25f) * env;
            }, lowPass: 0.28f), baseVolume: 0.35f, minInterval: 0.08f);

            // 40. Замах врага перед атакой
            Register("enemy_windup", Synth(0.18f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Sin(p * MathF.PI);
                float f = 240f + 280f * p;
                return (Triangle(t * f) * 0.60f + Saw(t * f * 0.5f) * 0.30f) * env;
            }, lowPass: 0.35f), baseVolume: 0.38f, minInterval: 0.08f);

            // 41. Гибель врага
            Register("enemy_death", Synth(0.22f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.7f);
                float drop = Sine(230f * t - 150f * t * p) * 0.70f;
                float pop = NextNoise() * 0.45f * MathF.Pow(1f - p, 2.4f);
                return (drop + pop) * env;
            }, lowPass: 0.26f), baseVolume: 0.46f, minInterval: 0.04f);

            // 42. Рёв Босса / Смена фазы Митрикса
            Register("boss_roar", Synth(0.92f, (t, dur) =>
            {
                float p = t / dur;
                float env = EnvADSR(t, dur, 0.06f, 0.20f, 0.75f, 0.35f);
                float f = 82f + MathF.Sin(p * MathF.PI) * 32f - p * 24f;
                float horn = Saw(t * f) * 0.60f + Square(t * f * 0.5f) * 0.45f + Sine(t * f * 1.5f) * 0.30f;
                float growl = NextNoise() * 0.40f;
                return (horn + growl) * env;
            }, lowPass: 0.18f), baseVolume: 0.78f, minInterval: 0.25f);

            // 43. Удар молота Митрикса (Волна 3-й фазы)
            Register("mithrix_slam", Synth(0.68f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Pow(1f - p, 1.35f);
                float sub = Sine(78f * t - 42f * t * p) * 0.95f;
                float metallic = Sine(t * 320f) * 0.40f * MathF.Pow(1f - p, 2.5f);
                float shock = NextNoise() * 0.85f * MathF.Pow(1f - p, 1.5f);
                return (sub + metallic + shock) * env;
            }, lowPass: 0.20f), baseVolume: 0.80f, minInterval: 0.15f);

            // 44. Открытие сундука / Капсулы / Принтера
            Register("chest_open", Synth(0.28f, (t, dur) =>
            {
                float p = t / dur;
                float env = EnvADSR(t, dur, 0.01f, 0.07f, 0.6f, 0.14f);
                float latch = t < 0.07f ? Square(t * 310f) * 0.55f : Sine(t * (440f + p * 240f)) * 0.55f;
                float hiss = NextNoise() * 0.40f * (1f - p);
                return (latch + hiss) * env;
            }, lowPass: 0.38f), baseVolume: 0.54f, minInterval: 0.05f);

            // 45. Подбор обычного / необычного предмета
            Register("item_pickup", Synth(0.32f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.01f, 0.07f, 0.65f, 0.15f);
                float f = t < 0.09f ? 523.25f : (t < 0.18f ? 659.25f : 783.99f);
                return (Sine(t * f) * 0.65f + Triangle(t * f * 2f) * 0.28f) * env;
            }), baseVolume: 0.56f, minInterval: 0.05f);

            // 46. Подбор Легендарного / Бездны / Мифического предмета (торжественный аккорд)
            Register("item_legendary", Synth(0.68f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.015f, 0.15f, 0.72f, 0.32f);
                float f = t < 0.11f ? 523.25f : (t < 0.22f ? 659.25f : (t < 0.33f ? 783.99f : 1046.50f));
                float pad = Sine(t * 261.63f) * 0.30f + Sine(t * 392.00f) * 0.25f;
                return (Sine(t * f) * 0.55f + Triangle(t * f * 2f) * 0.25f + pad) * env;
            }), baseVolume: 0.68f, minInterval: 0.08f);

            // 47. Активация святилища (Шанс, Кровь, Битва, Гора, Порядок)
            Register("shrine_use", Synth(0.44f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.02f, 0.10f, 0.6f, 0.22f);
                float chord = Sine(t * 293.66f) * 0.40f + Sine(t * 440.0f) * 0.35f + Triangle(t * 587.33f) * 0.30f;
                return chord * env;
            }), baseVolume: 0.58f, minInterval: 0.08f);

            // 48. Открытие Портала / Алтарь Тритона
            Register("portal_open", Synth(0.75f, (t, dur) =>
            {
                float p = t / dur;
                float env = EnvADSR(t, dur, 0.05f, 0.18f, 0.70f, 0.32f);
                float shimmer = Sine(t * (330f + MathF.Sin(t * 18f) * 14f)) * 0.38f
                              + Sine(t * (495f + MathF.Cos(t * 22f) * 18f)) * 0.34f
                              + Sine(t * (660f + p * 220f)) * 0.30f;
                return shimmer * env;
            }), baseVolume: 0.65f, minInterval: 0.15f);

            // 49. Импульс зарядки телепорта / столпа Луны
            Register("teleporter_charge", Synth(0.18f, (t, dur) =>
            {
                float p = t / dur;
                float env = MathF.Sin(p * MathF.PI) * (1f - p * 0.4f);
                return (Sine(t * 440f) * 0.65f + Sine(t * 880f) * 0.25f) * env;
            }), baseVolume: 0.30f, minInterval: 0.25f);

            // 50. Полная зарядка телепорта / столпа (100%)
            Register("teleporter_ready", Synth(0.72f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.02f, 0.16f, 0.72f, 0.34f);
                float c1 = Sine(t * 440f) * 0.35f + Sine(t * 554.37f) * 0.30f + Sine(t * 659.25f) * 0.30f + Sine(t * 880f) * 0.35f;
                return c1 * env;
            }), baseVolume: 0.68f, minInterval: 0.2f);

            // 51. Победа / Эвакуация / Открытие достижения
            Register("victory", Synth(1.10f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.02f, 0.20f, 0.75f, 0.45f);
                float f = t < 0.16f ? 523.25f : (t < 0.32f ? 659.25f : (t < 0.48f ? 783.99f : 1046.50f));
                float bass = Sine(t * 261.63f) * 0.35f + Saw(t * 130.81f) * 0.20f;
                return (Sine(t * f) * 0.50f + Triangle(t * f * 1.5f) * 0.25f + bass) * env;
            }, lowPass: 0.55f), baseVolume: 0.72f, minInterval: 0.15f);

            // 51b. Колесо эмоций: музыкальный бит танца
            Register("emote_beat", Synth(0.16f, (t, dur) =>
            {
                float env = MathF.Pow(1f - t / dur, 2.0f);
                float kick = Sine(t * (150f - t * 620f)) * 0.65f;
                float chord = Triangle(t * 523.25f) * 0.22f + Sine(t * 659.25f) * 0.18f;
                return (kick + chord) * env;
            }, lowPass: 0.48f), baseVolume: 0.42f, minInterval: 0.14f);

            // 51c. Камень-Ножницы-Бумага: отсчёт «Раз-Два-Три!»
            Register("rps_tick", Synth(0.11f, (t, dur) =>
            {
                float env = MathF.Pow(1f - t / dur, 2.3f);
                return (Sine(t * 680f) * 0.65f + Triangle(t * 1360f) * 0.25f) * env;
            }), baseVolume: 0.52f, minInterval: 0.05f);

            Register("rps_win", Synth(0.55f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.01f, 0.10f, 0.70f, 0.25f);
                float f = t < 0.14f ? 587.33f : (t < 0.28f ? 739.99f : 880.0f);
                return (Sine(t * f) * 0.55f + Triangle(t * f * 2f) * 0.25f) * env;
            }), baseVolume: 0.62f, minInterval: 0.1f);

            Register("rps_lose", Synth(0.52f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.02f, 0.12f, 0.65f, 0.24f);
                float f = t < 0.22f ? 329.63f : 233.08f - (t - 0.22f) * 90f;
                return (Saw(t * f) * 0.45f + Sine(t * f) * 0.40f) * env;
            }, lowPass: 0.30f), baseVolume: 0.55f, minInterval: 0.1f);

            Register("rps_draw", Synth(0.28f, (t, dur) =>
            {
                float env = EnvADSR(t, dur, 0.01f, 0.08f, 0.55f, 0.14f);
                float f = t < 0.13f ? 493.88f : 493.88f;
                return (Sine(t * f) * 0.55f + Triangle(t * f * 1.5f) * 0.25f) * env;
            }), baseVolume: 0.50f, minInterval: 0.08f);

            // 51d. Диалоговый блип на корабле Safe Travels и катсценах
            Register("dialogue_blip", Synth(0.055f, (t, dur) =>
            {
                float env = MathF.Pow(1f - t / dur, 1.9f);
                return (Sine(t * 740f) * 0.60f + Triangle(t * 1110f) * 0.25f) * env;
            }), baseVolume: 0.34f, minInterval: 0.045f);

            Register("cutscene_whoosh", Synth(0.85f, (t, dur) =>
            {
                float p = t / dur;
                float env = EnvADSR(t, dur, 0.08f, 0.22f, 0.68f, 0.38f);
                float sweep = Sine(t * (110f + p * 220f)) * 0.45f + Saw(t * (55f + p * 55f)) * 0.28f + NextNoise() * 0.15f * (1f - p);
                return sweep * env;
            }, lowPass: 0.28f), baseVolume: 0.64f, minInterval: 0.2f);

            // 52. Процедурный фоновый космо-эмбиент (8-секундная бесшовная синт-петля)
            _ambientExplore = CreateRawSound(Synth(8.0f, (t, dur) =>
            {
                // Плавное затухание на краях петли для бесшовного цикла
                float edge = Math.Clamp(MathF.Min(t / 0.4f, (dur - t) / 0.4f), 0f, 1f);
                // Космический минорный аккорд A-minor9 (A1, E2, G2, B2, C3)
                float lfo1 = 0.5f + 0.5f * MathF.Sin(t * MathF.PI * 0.5f);
                float lfo2 = 0.5f + 0.5f * MathF.Cos(t * MathF.PI * 0.25f);
                float pad = Sine(t * 55.0f) * 0.32f
                          + Sine(t * 82.41f) * 0.24f
                          + Sine(t * 110.0f) * 0.22f * lfo1
                          + Triangle(t * 164.81f) * 0.14f * lfo2
                          + Sine(t * 220.0f) * 0.12f * (1f - lfo1);
                // Мягкий арпеджиатор на фоне
                float[] arpNotes = { 220.0f, 261.63f, 329.63f, 392.0f, 329.63f, 261.63f, 246.94f, 196.0f };
                int step = (int)(t * 2.0f) % arpNotes.Length;
                float stepTime = (t * 2.0f) - MathF.Floor(t * 2.0f);
                float arpEnv = MathF.Pow(1f - stepTime, 2.2f) * 0.16f;
                float arp = Sine(t * arpNotes[step]) * arpEnv;
                return (pad + arp) * edge * 0.55f;
            }, lowPass: 0.18f));

            // 53. Процедурный боевой эмбиент во время битвы с Боссом / Митриксом (6-секундная петля)
            _ambientBoss = CreateRawSound(Synth(6.0f, (t, dur) =>
            {
                float edge = Math.Clamp(MathF.Min(t / 0.3f, (dur - t) / 0.3f), 0f, 1f);
                float beat = (t * 4.0f) - MathF.Floor(t * 4.0f);
                float bassEnv = MathF.Pow(1f - beat, 1.8f);
                float[] bassNotes = { 55.0f, 55.0f, 65.41f, 58.27f, 55.0f, 55.0f, 49.0f, 51.91f };
                int bIdx = (int)(t * 2.0f) % bassNotes.Length;
                float f = bassNotes[bIdx];
                float bass = (Saw(t * f) * 0.38f + Sine(t * f) * 0.42f) * bassEnv;
                float drone = Sine(t * 110.0f) * 0.18f + Triangle(t * 164.81f) * 0.12f;
                return (bass + drone) * edge * 0.55f;
            }, lowPass: 0.20f));

            _hasAmbient = true;
        }

        private static float[] Synth(float durationSec, Func<float, float, float> generator, float lowPass = 0f)
        {
            int count = Math.Max(64, (int)(durationSec * SampleRate));
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                samples[i] = generator(t, durationSec);
            }
            if (lowPass > 0.001f && lowPass < 0.999f)
                LowPassFilter(samples, lowPass);
            return samples;
        }
    }
}
