using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Models
{
    public class SkillDef
    {
        public string Name = "";
        public float Cd;
        public string Desc = "";
        public string? UnlockAchKey = null;
        public string UnlockReqText = "Открыт по умолчанию";
    }

    public class SkinDef
    {
        public string Name = "";
        public string Desc = "";
        public string? UnlockAchKey = null;
        public string UnlockReqText = "Открыт по умолчанию";
        public bool UnlockedByDefault = false;
        public bool ChangesModel = false;
        public Color BodyColor;
        public Color ArmorColor;
        public Color VisorColor;
        public string? CustomSubtitle = null;
        public string? CustomPassiveName = null;
        public string? CustomPassiveDesc = null;
        public Dictionary<string, List<SkillDef>>? CustomSkillVariants = null;
        public Color PrimaryColor => BodyColor;
        public Color AccentColor => VisorColor;
        public string? UnlockAchievementKey => UnlockAchKey;
        public string UnlockConditionText => UnlockReqText;
    }

    public class SurvivorConfig
    {
        public string Key = "";
        public string Name = "";
        public string Subtitle = "";
        public string BaseSubtitle = "";
        public string Icon = "";
        public int Hp;
        public float Speed;
        public float Damage;
        public float FireRate;
        public float ProjSpeed = 62f;
        public float ProjScale = 0.22f;
        public float Aoe = 0f;
        public Color BodyColor;
        public Color ArmorColor;
        public Color VisorColor;
        public Color BaseBodyColor;
        public Color BaseArmorColor;
        public Color BaseVisorColor;
        public string OverviewLore = "";
        public string PassiveName = "";
        public string PassiveDesc = "";
        public string BasePassiveName = "";
        public string BasePassiveDesc = "";
        public Dictionary<string, SkillDef> Skills = new();
        public Dictionary<string, List<SkillDef>> SkillVariants = new();
        public Dictionary<string, List<SkillDef>> BaseSkillVariants = new();
        public List<SkinDef> Skins = new();

        public void SyncLoadout()
        {
            if (Skins.Count > 0)
            {
                var skin = GetActiveSkin();
                if (skin.CustomSkillVariants != null)
                {
                    SkillVariants = skin.CustomSkillVariants;
                    if (!string.IsNullOrEmpty(skin.CustomSubtitle)) Subtitle = skin.CustomSubtitle!;
                    if (!string.IsNullOrEmpty(skin.CustomPassiveName)) PassiveName = skin.CustomPassiveName!;
                    if (!string.IsNullOrEmpty(skin.CustomPassiveDesc)) PassiveDesc = skin.CustomPassiveDesc!;
                }
                else if (BaseSkillVariants.Count > 0)
                {
                    SkillVariants = BaseSkillVariants;
                    if (!string.IsNullOrEmpty(BaseSubtitle)) Subtitle = BaseSubtitle;
                    if (!string.IsNullOrEmpty(BasePassiveName)) PassiveName = BasePassiveName;
                    if (!string.IsNullOrEmpty(BasePassiveDesc)) PassiveDesc = BasePassiveDesc;
                }
            }
            foreach (var slot in new[] { "m1", "m2", "shift", "r" })
            {
                if (SkillVariants.TryGetValue(slot, out var list) && list.Count > 0)
                {
                    int idx = Math.Clamp(Data.ProgressData.Instance.GetSkillVariant(Key, slot), 0, list.Count - 1);
                    Skills[slot] = list[idx];
                }
            }
        }

        public SkinDef GetActiveSkin(int? skinOverride = null)
        {
            if (Skins.Count == 0)
                return new SkinDef { Name = "Классический", BodyColor = BodyColor, ArmorColor = ArmorColor, VisorColor = VisorColor };
            int idx = Math.Clamp(skinOverride ?? Data.ProgressData.Instance.GetSelectedSkin(Key), 0, Skins.Count - 1);
            if (!skinOverride.HasValue && !Data.ProgressData.Instance.IsSkinUnlocked(Key, idx, Skins[idx].UnlockAchKey))
                idx = 0;
            return Skins[idx];
        }
    }

    public partial class SurvivorParts
    {
        public Entity Root = null!;
        public Entity Torso = null!;
        public Entity LeftLeg = null!;
        public Entity RightLeg = null!;
        public Entity LeftArm = null!;
        public Entity RightArm = null!;
        public Entity? Halo;
        public Entity? Bow;
        public Entity? Shield;
        public Entity? ShieldAura;
        public Entity? Head;
        public readonly List<Entity> Tails = new();
        public readonly List<Entity> NormalFormNodes = new();
        public readonly List<Entity> UltFormNodes = new();
        public bool UltFormActive = false;
        public bool IsKyKiske = false;

        // Базовые координаты и углы сборки конкретной модели (Bind Pose)
        public Vector3 BaseTorsoPos, BaseTorsoRot;
        public Vector3 BaseLeftArmPos, BaseLeftArmRot, BaseRightArmPos, BaseRightArmRot;
        public Vector3 BaseLeftLegPos, BaseLeftLegRot, BaseRightLegPos, BaseRightLegRot;
        public Vector3 BaseHeadPos, BaseHeadRot;
        public readonly List<Vector3> BaseTailRots = new();
        private bool _bindPoseCaptured = false;
        private bool _wasGrounded = true;
        private bool _emoteSpawnedGena = false;
        public float LandingSquash = 0f;

        // Список эмоций Колеса [L] (ID, название, подзаголовок)
        public static readonly (string Id, string Title, string Subtitle)[] EmoteWheelDefinitions =
        {
            ("chair", "Пластиковый стул", "Мотивация Вергилия"),
            ("fortnite", "Танец Фортнайта", "Стандартный танец"),
            ("california", "Калифорния гёрлс", "California Gurls"),
            ("caramelldansen", "Карамеллдансен", "Евробит с ушками"),
            ("sun", "Восхвали солнце", "Praise the Sun \\[T]/"),
            ("kazotsky", "Танец вприсядку", "Казачок / Kazotsky"),
            ("gangnam", "Gangnam Style", "Скачка и лассо"),
            ("jojo", "JoJo Поза", "Угрожающая аура!"),
            ("distraction", "Танец Отвлечения", "Henry Stickmin"),
            ("rps_wait", "Камень-Ножницы-Бумага", "Парная дуэль [E]")
        };

        // Состояние процедурной и боевой анимации + Колесо эмоций [L]
        public string ActiveAnim = "";
        public float AnimTimer = 0f;
        public float AnimDuration = 0.25f;
        public int AnimStep = 0;
        public float WalkPhase = 0f;
        public float IdleTime = 0f;
        public string ActiveEmote = "";
        public float EmoteTime = 0f;

        public void CaptureBindPose()
        {
            if (Torso != null) { BaseTorsoPos = Torso.Position; BaseTorsoRot = Torso.Rotation; }
            if (LeftArm != null) { BaseLeftArmPos = LeftArm.Position; BaseLeftArmRot = LeftArm.Rotation; }
            if (RightArm != null) { BaseRightArmPos = RightArm.Position; BaseRightArmRot = RightArm.Rotation; }
            if (LeftLeg != null) { BaseLeftLegPos = LeftLeg.Position; BaseLeftLegRot = LeftLeg.Rotation; }
            if (RightLeg != null) { BaseRightLegPos = RightLeg.Position; BaseRightLegRot = RightLeg.Rotation; }
            if (Head != null) { BaseHeadPos = Head.Position; BaseHeadRot = Head.Rotation; }
            BaseTailRots.Clear();
            foreach (var tail in Tails)
                BaseTailRots.Add(tail != null ? tail.Rotation : Vector3.Zero);
            _bindPoseCaptured = true;
        }

        public void TriggerSkillAnim(string slot, float customDuration = 0f)
        {
            AnimStep++;
            ActiveAnim = slot;
            AnimDuration = customDuration > 0f ? customDuration : slot switch
            {
                "m1" => 0.24f,
                "m2" => 0.42f,
                "shift" => 0.36f,
                "r" => 0.65f,
                "ky_charge" => 0.25f,
                "ky_dipper" => 0.34f,
                "ky_rtl" => 0.48f,
                "jack_r_lunge" => 0.30f,
                "jack_r_rise" => 0.42f,
                "jack_r_slam" => 0.36f,
                _ => 0.25f
            };
            AnimTimer = AnimDuration;
        }

        public void SetEmote(string emoteId)
        {
            if (ActiveEmote == emoteId) return;
            ActiveEmote = emoteId ?? "";
            EmoteTime = 0f;
        }

        public void UpdateAnimations(
            string charKey, float dt, bool isMoving, bool isGrounded,
            float velY, float moveSpeedRatio, float strafeInput, float forwardInput,
            bool shieldStance = false, bool isFlying = false, bool orgiaMode = false)
        {
            if (!_bindPoseCaptured) CaptureBindPose();
            float t = (float)Raylib.GetTime();

            if (!_wasGrounded && isGrounded)
                LandingSquash = Math.Clamp(0.08f + MathF.Abs(velY) * 0.011f, 0.08f, 0.24f);
            _wasGrounded = isGrounded;
            if (LandingSquash > 0f)
                LandingSquash = MathF.Max(0f, LandingSquash - dt * 2.5f);

            if (AnimTimer > 0f)
                AnimTimer = MathF.Max(0f, AnimTimer - dt);

            if (!string.IsNullOrEmpty(ActiveEmote))
                EmoteTime += dt;
            else
                EmoteTime = 0f;

            // Вращение нимбов/ореолов (у Данте — с эффектом часового механизма, у Mindustry — горизонтальный радар)
            if (Halo != null)
            {
                float haloMul = !string.IsNullOrEmpty(ActiveEmote)
                    ? (ActiveEmote is "sun" or "praise_the_sun" or "praise_sun" ? 2.8f : 1.5f)
                    : 1.0f;
                if (charKey == "dante")
                {
                    Halo.RotationZ += (35f + MathF.Pow(MathF.Max(0f, MathF.Sin(t * 6.5f)), 6f) * 260f) * haloMul * dt;
                }
                else if (charKey == "mindustry")
                {
                    Halo.RotationZ = 0f;
                    Halo.RotationY += 110f * haloMul * dt;
                }
                else
                {
                    Halo.RotationZ += 95f * haloMul * dt;
                }
            }

            // Корабль Ядра «Гамма» (Mindustry) — кастомная негуманоидная хореография полёта и всех эмоций Колеса [L]
            if (charKey == "mindustry")
            {
                UpdateMindustryAnimations(t, dt, isMoving, strafeInput, forwardInput);
                return;
            }

            Vector3 targetTorsoPos = BaseTorsoPos;
            Vector3 targetTorsoRot = BaseTorsoRot;
            Vector3 targetLArmPos = BaseLeftArmPos;
            Vector3 targetRArmPos = BaseRightArmPos;
            Vector3 targetLLegPos = BaseLeftLegPos;
            Vector3 targetRLegPos = BaseRightLegPos;
            Vector3 targetLArmRot = BaseLeftArmRot;
            Vector3 targetRArmRot = BaseRightArmRot;
            Vector3 targetLLegRot = BaseLeftLegRot;
            Vector3 targetRLegRot = BaseRightLegRot;

            // 1. БАЗОВЫЕ СТОЙКИ ПОКОЯ (IDLE), ПЕРЕНОС ВЕСА И ПЕРИОДИЧЕСКИЕ IDLE-ЖЕСТЫ (FLOURISH)
            if (!isMoving && isGrounded && AnimTimer <= 0f && string.IsNullOrEmpty(ActiveEmote))
                IdleTime += dt;
            else
                IdleTime = 0f;

            float breath = MathF.Sin(t * 2.8f) * 0.032f;
            float weightShift = MathF.Sin(t * 1.45f);
            targetTorsoPos.Y += breath;
            targetTorsoPos.X = BaseTorsoPos.X + weightShift * 0.022f;
            targetTorsoRot.Z = BaseTorsoRot.Z + weightShift * 1.8f;
            targetLLegRot.X = BaseLeftLegRot.X + weightShift * 4.5f;
            targetRLegRot.X = BaseRightLegRot.X - weightShift * 4.5f;

            // Цикл характерного idle-действия (проигрывается каждые ~6 секунд стояния на месте, в т.ч. в лобби)
            float idleCycle = IdleTime % 5.8f;
            float fProg = (idleCycle is >= 1.5f and <= 4.3f) ? (idleCycle - 1.5f) / 2.8f : 0f;
            float fWave = fProg > 0f ? MathF.Sin(fProg * MathF.PI) : 0f;
            float fOsc = fProg > 0f ? MathF.Sin(fProg * MathF.PI * 3.0f) : 0f;

            switch (charKey)
            {
                case "commando":
                    targetLArmRot.Y = MathF.Sin(t * 1.7f) * 6f + fWave * 16f;
                    targetRArmRot.Y = -MathF.Sin(t * 1.7f) * 6f - fWave * 16f;
                    targetLArmRot.X = -8f + MathF.Cos(t * 2.2f) * 4f - fWave * 42f + fOsc * 24f;
                    targetRArmRot.X = -8f + MathF.Sin(t * 2.2f) * 4f - fWave * 42f - fOsc * 24f;
                    targetTorsoRot.Y = MathF.Sin(t * 1.1f) * 5f + fOsc * 8f;
                    break;

                case "artificer":
                    targetTorsoPos.Y = BaseTorsoPos.Y + 0.14f + MathF.Sin(t * 2.6f) * 0.085f + fWave * 0.10f;
                    targetTorsoPos.X = BaseTorsoPos.X + MathF.Cos(t * 1.3f) * 0.045f;
                    targetLLegRot.X = 14f + MathF.Sin(t * 2.2f) * 6f + fWave * 8f;
                    targetRLegRot.X = 20f - MathF.Sin(t * 2.2f) * 6f + fWave * 8f;
                    targetLArmRot.X = -18f + MathF.Sin(t * 3.0f) * 10f - fWave * 48f;
                    targetRArmRot.X = -18f + MathF.Cos(t * 3.0f) * 10f - fWave * 48f;
                    targetLArmRot.Z = 12f + MathF.Cos(t * 2.4f) * 5f + fWave * 18f;
                    targetRArmRot.Z = -12f - MathF.Sin(t * 2.4f) * 5f - fWave * 18f;
                    targetTorsoRot.Y = fOsc * 10f;
                    break;

                case "huntress":
                    targetTorsoRot.Y = -14f + MathF.Sin(t * 1.4f) * 4f + fWave * 18f;
                    targetLArmRot.X = -18f + MathF.Sin(t * 2.4f) * 5f - fWave * 36f;
                    targetLArmRot.Y = 12f + fWave * 14f;
                    targetRArmRot.X = -10f + MathF.Cos(t * 2.4f) * 4f - fWave * 34f + fOsc * 12f;
                    break;

                case "buzz":
                    targetTorsoRot.X = -4f + MathF.Sin(t * 2.2f) * 2.5f - fWave * 8f;
                    targetTorsoRot.Y = MathF.Sin(t * 1.2f) * 6f - fWave * 16f;
                    targetLArmRot.Z = 8f + MathF.Abs(MathF.Sin(t * 2.0f)) * 4f;
                    targetRArmRot.Z = -8f - MathF.Abs(MathF.Sin(t * 2.0f)) * 4f;
                    // Idle-жест Базза: подносит левое запястье к шлему («Звёздный Командир на связи!») и расправляет грудь
                    targetLArmRot.X = -8f - fWave * 68f;
                    targetLArmRot.Y = fWave * 38f;
                    targetRArmRot.X = -6f - fWave * 24f + fOsc * 14f;
                    break;

                case "sol":
                    if (IsKyKiske)
                    {
                        // Благородная фехтовальная стойка Короля Кая Киске со шпагой-мечом Thunderseal
                        targetTorsoPos.Y = BaseTorsoPos.Y + MathF.Sin(t * 2.6f) * 0.035f;
                        targetTorsoRot.Y = 22f + MathF.Sin(t * 1.8f) * 4f - fWave * 18f;
                        targetLLegRot.X = -10f;
                        targetRLegRot.X = 12f;
                        targetLArmRot = new Vector3(-22f - fWave * 45f, 24f, 12f);
                        targetRArmRot = new Vector3(-34f - fWave * 62f + fOsc * 14f, -18f + fWave * 28f, -12f);
                    }
                    else
                    {
                        float fightBounce = MathF.Abs(MathF.Sin(t * 5.2f));
                        targetTorsoPos.Y = BaseTorsoPos.Y + fightBounce * 0.068f;
                        targetLLegRot.X = -8f + MathF.Sin(t * 5.2f) * 6f;
                        targetRLegRot.X = 8f - MathF.Sin(t * 5.2f) * 6f;
                        if (UltFormActive)
                        {
                            targetTorsoRot.X = 18f - fWave * 16f;
                            targetLArmRot = new Vector3(-22f + MathF.Sin(t * 5f) * 8f - fWave * 45f, -10f, 18f + fWave * 16f);
                            targetRArmRot = new Vector3(-22f - MathF.Sin(t * 5f) * 8f - fWave * 45f, 10f, -18f - fWave * 16f);
                        }
                        else
                        {
                            targetTorsoRot.Y = -12f + MathF.Sin(t * 2.6f) * 5f - fWave * 18f;
                            targetLArmRot.X = -14f + fightBounce * 9f - fWave * 28f;
                            // Idle-жест Сола: вскидывает меч Junkyard Dog на плечо и разминает шею
                            targetRArmRot.X = -18f - fWave * 54f + fOsc * 14f;
                            targetRArmRot.Y = -fWave * 22f;
                        }
                    }
                    break;

                case "gwen":
                    targetTorsoRot.Z = MathF.Sin(t * 2.6f) * 4.2f + fOsc * 6f;
                    targetTorsoRot.Y = MathF.Cos(t * 1.8f) * 6f + fWave * 24f;
                    targetRArmRot = new Vector3(-16f + MathF.Sin(t * 3.2f) * 6f - fWave * 44f + fOsc * 18f, -16f + fOsc * 16f, -8f);
                    targetLArmRot = new Vector3(-10f + MathF.Cos(t * 3.2f) * 5f - fWave * 26f, 12f, 12f + fWave * 14f);
                    break;

                case "enforcer":
                    if (shieldStance)
                    {
                        targetTorsoRot.Y = 16f + MathF.Sin(t * 1.5f) * 3f;
                        targetTorsoRot.X = 6f;
                        targetLArmRot = new Vector3(-14f, 26f, 0f);
                        targetRArmRot = new Vector3(-22f + fOsc * 8f, -10f, 0f);
                    }
                    else
                    {
                        targetTorsoRot.Y = MathF.Sin(t * 1.3f) * 5f;
                        targetLArmRot.X = -8f + MathF.Sin(t * 2.0f) * 4f - fWave * 22f;
                        // Idle-жест Силовика: постукивание дробовиком о край щита
                        targetRArmRot.X = -12f - fWave * 36f + fOsc * 18f;
                        targetRArmRot.Y = fWave * 26f;
                    }
                    break;

                case "scout":
                case "m4a1":
                case "freeman":
                case "medic":
                    targetTorsoRot.Y = MathF.Sin(t * 1.4f) * 5.5f + fWave * 14f;
                    // Idle-жест: осмотр оружия / проверка затвора и датчиков
                    targetLArmRot = new Vector3(-16f + MathF.Sin(t * 2.4f) * 4f - fWave * 38f + fOsc * 10f, 22f + fWave * 16f, fWave * 10f);
                    targetRArmRot = new Vector3(-12f + MathF.Cos(t * 2.4f) * 4f - fWave * 32f, -6f - fWave * 14f, -fWave * 8f);
                    break;

                case "v1":
                    targetTorsoPos.Y = BaseTorsoPos.Y + MathF.Sin(t * 3.6f) * 0.038f;
                    targetTorsoRot.Y = MathF.Sin(t * 1.8f) * 6f + fOsc * 10f;
                    // Idle-жест V1: подбрасывание монетки левой рукой и прокрутка револьвера правой
                    targetLArmRot = new Vector3(-12f - fWave * 64f + fOsc * 18f, 10f * fWave, 8f + fWave * 14f);
                    targetRArmRot = new Vector3(-16f - fWave * 42f - fOsc * 22f, -8f * fWave, -8f - fWave * 14f);
                    break;

                case "aigis":
                    if (orgiaMode)
                    {
                        targetTorsoPos.Y = BaseTorsoPos.Y + 0.18f + MathF.Sin(t * 4.5f) * 0.06f;
                        targetTorsoRot.X = 18f;
                        targetLLegRot.X = 22f;
                        targetRLegRot.X = 28f;
                    }
                    else
                    {
                        targetTorsoRot.Y = MathF.Sin(t * 1.5f) * 5f;
                        // Idle-жест Айгис: калибровка орудийных пальцев и тактическое сканирование
                        targetLArmRot = new Vector3(-10f - fWave * 42f, fOsc * 14f, 6f + fWave * 22f);
                        targetRArmRot = new Vector3(-10f - fWave * 42f, -fOsc * 14f, -6f - fWave * 22f);
                    }
                    break;

                case "reimu":
                    targetTorsoRot.Z = MathF.Sin(t * 2.0f) * 3.2f;
                    targetLArmRot.Z = 12f + MathF.Sin(t * 2.5f) * 6f + fWave * 18f;
                    targetLArmRot.X = -6f - fWave * 24f;
                    // Idle-жест Рейму: взмах жезлом гохэй восьмёркой для очищения духов
                    targetRArmRot.Z = -12f - MathF.Sin(t * 2.5f) * 6f + fOsc * 26f;
                    targetRArmRot.X = -14f + MathF.Cos(t * 2.5f) * 6f - fWave * 52f;
                    break;

                case "slark":
                    targetTorsoPos.Y = BaseTorsoPos.Y - 0.05f + MathF.Sin(t * 4.2f) * 0.045f;
                    targetTorsoRot.X = BaseTorsoRot.X + 5f - fWave * 10f;
                    targetTorsoRot.Y = MathF.Sin(t * 2.4f) * 8f + fOsc * 14f;
                    targetLArmRot = new Vector3(-18f - fWave * 28f, 14f, 12f + fWave * 16f);
                    // Idle-жест Сларка: крутит заточку в лапе и принюхивается к добыче
                    targetRArmRot = new Vector3(16f - fWave * 68f + fOsc * 24f, -18f + fOsc * 18f, -12f);
                    break;

                case "tidehunter":
                    targetTorsoPos.Y = BaseTorsoPos.Y + MathF.Sin(t * 2.1f) * 0.055f + MathF.Abs(fOsc) * 0.05f;
                    targetTorsoRot.Z = MathF.Sin(t * 1.6f) * 4.0f + fOsc * 5f;
                    // Idle-жест Тайдхантера: перехватывает гигантский якорь и глухо бьёт им о землю
                    targetRArmRot.X = -16f - fWave * 46f + fOsc * 18f;
                    targetLArmRot.X = -6f - fWave * 28f;
                    break;

                case "dante":
                    targetTorsoRot.Y = MathF.Sin(t * 1.3f) * 4.5f + fWave * 14f;
                    // Idle-жест Данте: сверяется с карманными часами и поправляет пальто Менеджера
                    targetLArmRot = new Vector3(14f - fWave * 68f, fWave * 26f, -8f + fWave * 12f);
                    targetRArmRot = new Vector3(-20f + MathF.Sin(t * 2.4f) * 6f - fWave * 24f, -10f + fOsc * 12f, 0f);
                    break;

                case "cheburashka":
                    targetTorsoPos.Y = BaseTorsoPos.Y + MathF.Abs(MathF.Sin(t * 3.4f)) * 0.048f + MathF.Abs(fOsc) * 0.04f;
                    targetTorsoRot.Z = MathF.Sin(t * 3.4f) * 5.5f + fOsc * 8f;
                    // Idle-жест Чебурашки: радостно машет лапкой и жонглирует апельсином
                    targetRArmRot.X = -18f + MathF.Sin(t * 3.4f) * 7f - fWave * 72f;
                    targetRArmRot.Z = -fWave * 18f + fOsc * 24f;
                    targetLArmRot.X = -8f - MathF.Sin(t * 3.4f) * 7f - fWave * 42f;
                    targetLArmRot.Z = fWave * 20f;
                    break;

                case "ahri":
                    targetTorsoPos.Y = BaseTorsoPos.Y + 0.03f + MathF.Sin(t * 2.8f) * 0.048f + fWave * 0.04f;
                    targetTorsoRot.Y = -10f + MathF.Sin(t * 1.9f) * 5.5f + fWave * 20f;
                    targetTorsoRot.Z = MathF.Cos(t * 1.9f) * 3.8f + fOsc * 4.5f;
                    // Idle-жест Ари: подносит Сферу Обмана к лицу, любуясь её сиянием, и распушает 9 хвостов
                    targetLArmRot = new Vector3(-16f + MathF.Sin(t * 2.5f) * 6f - fWave * 36f, 14f + fWave * 18f, 12f + fWave * 14f);
                    targetRArmRot = new Vector3(-28f + MathF.Cos(t * 3.1f) * 8f - fWave * 48f, -14f - fWave * 20f, -10f + fOsc * 12f);
                    break;

                case "lappland":
                    targetTorsoRot.Y = 12f + MathF.Sin(t * 2.1f) * 4f + fWave * 16f;
                    // Лаппланд скрещивает парные клинки и ухмыляется
                    targetLArmRot = new Vector3(-18f - fWave * 42f, 22f * fWave, 14f);
                    targetRArmRot = new Vector3(-22f - fWave * 42f, -22f * fWave, -14f);
                    break;

                case "w_arknights":
                    targetTorsoRot.Y = -10f + MathF.Sin(t * 2.2f) * 5f;
                    // W играет с детонатором в левой руке
                    targetLArmRot = new Vector3(-34f - fWave * 38f, 16f, 8f + fOsc * 12f);
                    targetRArmRot = new Vector3(-18f + MathF.Sin(t * 2.0f) * 5f, -10f, -8f);
                    break;

                case "gold_ship":
                    targetTorsoPos.Y = BaseTorsoPos.Y + MathF.Abs(MathF.Sin(t * 3.6f)) * 0.05f;
                    // Голд Шип разминается перед забегом и отдаёт честь
                    targetRArmRot = new Vector3(-24f - fWave * 75f, -12f * fWave, -12f + fOsc * 18f);
                    targetLArmRot = new Vector3(-16f + MathF.Sin(t * 3.6f) * 10f, 0f, 12f);
                    break;

                case "pushnoy":
                    targetTorsoRot.Z = MathF.Sin(t * 3.2f) * 4.5f + fOsc * 8f;
                    // Александр Пушной берёт рок-аккорд на электрогитаре
                    targetLArmRot = new Vector3(-32f, 24f, 12f + fOsc * 8f);
                    targetRArmRot = new Vector3(-28f + MathF.Sin(t * 8.5f) * (10f + fWave * 22f), -18f, -10f);
                    break;

                case "freddy":
                    // Механические подёргивания аниматроника Фредди Фазбера
                    targetTorsoRot.Y = MathF.Round(MathF.Sin(t * 1.8f) * 2f) * 6f + fOsc * 14f;
                    targetTorsoRot.Z = fWave * 9f;
                    targetRArmRot = new Vector3(-32f - fWave * 28f, -8f, 0f);
                    targetLArmRot = new Vector3(-12f + fOsc * 16f, 0f, 8f);
                    break;

                case "steve":
                    // Классическое квадратное дыхание Стива из Minecraft
                    targetLArmRot = new Vector3(MathF.Sin(t * 1.8f) * 4f, 0f, 4f);
                    targetRArmRot = new Vector3(-18f - fWave * 36f, 0f, -4f);
                    break;

                case "jotaro":
                    targetTorsoRot.Y = -16f + fWave * 12f;
                    // Фирменная поза Джотаро Куджо: поправляет козырёк кепки и указывает пальцем вперёд ("Yare Yare Daze")
                    targetLArmRot = new Vector3(-14f - fWave * 78f, 18f * fWave, 10f);
                    targetRArmRot = new Vector3(-22f - fWave * 64f, 0f, -8f);
                    break;

                case "vyper":
                    targetTorsoPos.Y = BaseTorsoPos.Y - 0.08f + MathF.Sin(t * 3.0f) * 0.04f;
                    targetTorsoRot.X = 12f;
                    targetTorsoRot.Y = MathF.Sin(t * 2.6f) * 8f;
                    targetLArmRot = new Vector3(-24f, 14f, 8f);
                    targetRArmRot = new Vector3(-26f - fWave * 25f, -12f, -8f);
                    break;

                case "haze":
                    targetTorsoRot.Y = 10f + MathF.Sin(t * 1.8f) * 5f;
                    targetLArmRot = new Vector3(-26f - fWave * 30f, 14f, 10f);
                    targetRArmRot = new Vector3(-26f + fWave * 20f, -14f, -10f);
                    break;

                case "dmc_trio":
                    targetTorsoRot.Y = -12f + MathF.Sin(t * 2.0f) * 5f + fWave * 16f;
                    targetLArmRot = new Vector3(-22f - fWave * 38f, 12f, 10f);
                    targetRArmRot = new Vector3(-28f - fWave * 45f, -16f, -12f);
                    break;

                case "sanabi":
                    // Бригадный Генерал SANABI: калибрует гигантскую Механическую Руку-Крюк и переглядывается с парящим дроном Мари
                    targetTorsoPos.Y = BaseTorsoPos.Y + MathF.Sin(t * 2.8f) * 0.045f;
                    targetTorsoRot.Y = -15f + MathF.Sin(t * 1.9f) * 6f + fWave * 18f;
                    targetTorsoRot.X = 5f + MathF.Cos(t * 2.2f) * 2.5f;
                    targetLArmRot = new Vector3(-20f - fWave * 35f, 16f + fOsc * 10f, 10f);
                    targetRArmRot = new Vector3(-32f - fWave * 58f + fOsc * 22f, -18f + fWave * 24f, -14f - fOsc * 16f);
                    break;

                case "jack_o":
                    // Игривая стойка Jack-O' из Guilty Gear -Strive-: покачивание бёдрами, проверка маски и цепной Тыквы!
                    targetTorsoPos.Y = BaseTorsoPos.Y + MathF.Sin(t * 3.4f) * 0.045f + fWave * 0.05f;
                    targetTorsoRot.Z = MathF.Sin(t * 1.9f) * 6.5f;
                    targetTorsoRot.Y = 12f + MathF.Cos(t * 1.7f) * 7f - fWave * 18f;
                    targetLArmRot = new Vector3(-28f - fWave * 64f, 18f, 18f + MathF.Sin(t * 2.5f) * 6f);
                    targetRArmRot = new Vector3(-24f + fOsc * 26f, -16f, -18f - MathF.Cos(t * 2.5f) * 6f);
                    targetLLegRot.X = -6f + MathF.Sin(t * 1.9f) * 5f;
                    targetRLegRot.X = 6f - MathF.Sin(t * 1.9f) * 5f;
                    break;
            }

            // 2. ДВИЖЕНИЕ (БЕГ / ШАГ / СТРЕЙФЫ) И ПОЗЫ В ВОЗДУХЕ
            if (!isGrounded)
            {
                if (charKey == "reimu" && isFlying)
                {
                    targetTorsoRot.X = 26f;
                    targetTorsoRot.Z = -strafeInput * 18f;
                    targetLLegRot.X = 24f + MathF.Sin(t * 6f) * 6f;
                    targetRLegRot.X = 30f - MathF.Sin(t * 6f) * 6f;
                    targetLArmRot = new Vector3(-12f, 0f, 24f);
                    targetRArmRot = new Vector3(-18f, 0f, -24f);
                }
                else if (charKey == "cheburashka" && velY < -3.0f)
                {
                    // Поза «белки-летяги» перед сокрушительным приземлением Чебурашки!
                    targetTorsoRot.X = 28f;
                    targetLLegRot.X = 32f;
                    targetRLegRot.X = 32f;
                    targetLArmRot = new Vector3(-45f, 0f, 35f);
                    targetRArmRot = new Vector3(-45f, 0f, -35f);
                }
                else
                {
                    float airPitch = Math.Clamp(-velY * 1.1f, -16f, 22f);
                    targetTorsoRot.X += airPitch;
                    targetTorsoRot.Z = -strafeInput * 10f;
                    targetLLegRot.X = -28f;
                    targetRLegRot.X = 22f;
                    targetLArmRot.X -= 18f;
                    targetRArmRot.X -= 14f;
                    targetLArmRot.Z += 12f;
                    targetRArmRot.Z -= 12f;
                }
            }
            else if (isMoving)
            {
                float freq = charKey switch
                {
                    "cheburashka" => 14.5f,
                    "tidehunter" => 8.2f,
                    "enforcer" => shieldStance ? 7.5f : 9.8f,
                    "slark" => 13.5f,
                    _ => 11.2f
                };
                WalkPhase += dt * (freq + Math.Clamp(moveSpeedRatio - 1f, -0.3f, 1.2f) * 3.5f);

                float strideAmp = charKey switch
                {
                    "artificer" => 10f,
                    "enforcer" => shieldStance ? 20f : 34f,
                    "cheburashka" => 32f,
                    "tidehunter" => 30f,
                    "slark" => 44f,
                    _ => 38f
                };

                float swing = MathF.Sin(WalkPhase) * strideAmp;
                float bounce = MathF.Abs(MathF.Cos(WalkPhase));

                if (charKey == "artificer")
                {
                    // Изобретатель плавно скользит в воздухе при движении
                    targetTorsoPos.Y = BaseTorsoPos.Y + 0.16f + MathF.Sin(t * 4f) * 0.06f;
                    targetTorsoRot.X = 16f * (forwardInput >= 0f ? 1f : -0.6f);
                    targetTorsoRot.Z = -strafeInput * 12f;
                    targetLLegRot.X = 22f + swing * 0.4f;
                    targetRLegRot.X = 26f - swing * 0.4f;
                    targetLArmRot.X = 18f;
                    targetRArmRot.X = 18f;
                }
                else
                {
                    targetLLegRot.X = swing;
                    targetRLegRot.X = -swing;
                    float bounceHeight = charKey == "tidehunter" ? 0.11f : (charKey == "cheburashka" ? 0.06f : 0.08f);
                    targetTorsoPos.Y = BaseTorsoPos.Y + bounce * bounceHeight;

                    // Наклон вперёд при беге, крен при стрейфе и скручивание корпуса
                    float fwdLean = charKey switch
                    {
                        "slark" => 26f,
                        "sol" => UltFormActive ? 24f : 14f,
                        "huntress" => 14f,
                        "enforcer" => shieldStance ? 6f : 10f,
                        _ => 10f
                    };
                    targetTorsoRot.X = BaseTorsoRot.X + (forwardInput >= -0.1f ? fwdLean : -8f);
                    targetTorsoRot.Z = -strafeInput * 8f;

                    if (charKey == "cheburashka")
                        targetTorsoRot.Z += MathF.Sin(WalkPhase) * 11f; // Забавная походка вразвалочку
                    else if (charKey == "tidehunter")
                        targetTorsoRot.Z += MathF.Sin(WalkPhase) * 7.5f;
                    else if (!shieldStance)
                        targetTorsoRot.Y += MathF.Sin(WalkPhase) * 6.5f;

                    // Работа рук при беге (с учётом оружия в руках)
                    if (charKey is "scout" or "m4a1" or "freeman" or "medic")
                    {
                        targetLArmRot.X = -18f + bounce * 6f;
                        targetRArmRot.X = -14f + bounce * 6f;
                    }
                    else if (charKey == "enforcer" && shieldStance)
                    {
                        targetLArmRot = new Vector3(-14f, 26f, 0f);
                        targetRArmRot = new Vector3(-20f + bounce * 4f, -10f, 0f);
                    }
                    else if (charKey == "huntress")
                    {
                        targetLArmRot.X = -20f + bounce * 5f;
                        targetRArmRot.X = -swing * 0.45f;
                    }
                    else
                    {
                        targetLArmRot.X += -swing * 0.55f;
                        targetRArmRot.X += swing * 0.55f;
                    }
                }
            }

            // 3. БОЕВЫЕ АНИМАЦИИ НАВЫКОВ (ПЕРЕКРЫВАЮТ БАЗОВУЮ ПОЗУ ПРИ АТАКЕ)
            if (AnimTimer > 0f && !string.IsNullOrEmpty(ActiveAnim))
            {
                float p = Math.Clamp(1f - (AnimTimer / MathF.Max(0.01f, AnimDuration)), 0f, 1f);
                float pulse = MathF.Sin(p * MathF.PI); // 0 -> 1 -> 0
                float side = (AnimStep % 2 == 0) ? 1f : -1f;

                if (ActiveAnim == "m1")
                {
                    switch (charKey)
                    {
                        case "commando":
                        case "reimu":
                            if (AnimStep % 2 == 0)
                            {
                                targetLArmRot.X = -38f * pulse;
                                targetTorsoRot.Y = 8f * pulse;
                            }
                            else
                            {
                                targetRArmRot.X = -38f * pulse;
                                targetTorsoRot.Y = -8f * pulse;
                            }
                            break;

                        case "artificer":
                            if (AnimStep % 2 == 0)
                            {
                                targetLArmRot.X = -52f * pulse;
                                targetTorsoRot.Y = 14f * pulse;
                            }
                            else
                            {
                                targetRArmRot.X = -52f * pulse;
                                targetTorsoRot.Y = -14f * pulse;
                            }
                            break;

                        case "huntress":
                            targetTorsoRot.Y = -18f;
                            targetLArmRot = new Vector3(-30f, 16f, 0f);
                            targetRArmRot = new Vector3(-28f - 24f * pulse, -12f * pulse, 0f);
                            break;

                        case "buzz":
                            targetRArmRot = new Vector3(-48f * pulse, 24f * pulse, -10f);
                            targetTorsoRot.Y = -12f * pulse;
                            break;

                        case "sol":
                            if (IsKyKiske)
                            {
                                // Фехтовальный перекрёстный разрез мечом Thunderseal с громовым выпадом!
                                targetTorsoRot.X += 18f * pulse;
                                targetTorsoRot.Y = side * 34f * pulse;
                                targetRArmRot = new Vector3(-76f + 98f * p, side * 38f * pulse, -14f * pulse);
                                targetLArmRot = new Vector3(-28f * pulse, 26f, 16f);
                            }
                            else
                            {
                                // Вонзающий удар Junkyard Dog в землю для GunFlame!
                                targetTorsoRot.X += 24f * pulse;
                                targetTorsoRot.Y = -28f + 52f * p;
                                targetRArmRot.X = -85f + 125f * p;
                                targetRArmRot.Y = -18f * pulse;
                            }
                            break;

                        case "gwen":
                            // Быстрые перекрёстные взмахи ножницами Чик-Чик!
                            targetTorsoRot.Y = side * 26f * pulse;
                            targetTorsoRot.X += 12f * pulse;
                            targetRArmRot.X = -55f + 70f * p;
                            targetRArmRot.Y = side * 34f * pulse;
                            targetLArmRot.X = -25f * pulse;
                            break;

                        case "enforcer":
                        case "scout":
                        case "m4a1":
                        case "aigis":
                        case "freeman":
                        case "medic":
                            targetRArmRot.X -= 26f * pulse;
                            if (!shieldStance) targetLArmRot.X -= 18f * pulse;
                            targetTorsoRot.X -= 7f * pulse;
                            break;

                        case "slark":
                            targetTorsoRot.X += 20f * pulse;
                            targetTorsoRot.Y = side * 28f * pulse;
                            targetRArmRot.X = -65f + 85f * p;
                            targetRArmRot.Y = -24f * pulse;
                            break;

                        case "tidehunter":
                            // Круговой взмах гигантским якорем на 360 градусов (Anchor Smash)!
                            targetTorsoRot.Y += p * 360f;
                            targetRArmRot.X = -35f;
                            targetRArmRot.Z = -28f * pulse;
                            break;

                        case "dante":
                            targetTorsoRot.Y = side * 18f * pulse;
                            targetRArmRot.X = -62f * pulse;
                            targetRArmRot.Y = side * 30f * pulse;
                            break;

                        case "cheburashka":
                            targetTorsoRot.X += 18f * pulse;
                            targetLArmRot.X = -52f * pulse;
                            targetRArmRot.X = -52f * pulse;
                            targetLArmRot.Y = 30f * pulse;
                            targetRArmRot.Y = -30f * pulse;
                            break;

                        case "ahri":
                            // Выброс Сферы Обмана правой рукой с грациозным разворотом
                            targetTorsoRot.Y = -24f + 48f * p;
                            targetTorsoRot.X += 14f * pulse;
                            targetRArmRot = new Vector3(-72f * pulse, -22f + 40f * p, -16f * pulse);
                            targetLArmRot = new Vector3(18f * pulse, 24f * pulse, 20f * pulse);
                            break;

                        case "lappland":
                        case "dmc_trio":
                            targetTorsoRot.Y = side * 28f * pulse;
                            targetTorsoRot.X += 16f * pulse;
                            targetRArmRot = new Vector3(-78f + 95f * p, side * 34f * pulse, -14f * pulse);
                            targetLArmRot = new Vector3(-48f * pulse, -side * 24f * pulse, 12f * pulse);
                            break;

                        case "w_arknights":
                        case "vyper":
                        case "haze":
                            targetRArmRot.X -= 32f * pulse;
                            targetLArmRot.X -= 22f * pulse;
                            targetTorsoRot.Y = side * 12f * pulse;
                            break;

                        case "gold_ship":
                            // Стремительный удар с разбега (нога/плечо)
                            targetTorsoRot.X += 24f * pulse;
                            targetTorsoRot.Y = side * 22f * pulse;
                            targetRLegRot.X = -68f * pulse;
                            targetRArmRot.X = -62f * pulse;
                            break;

                        case "pushnoy":
                            // Мощный удар по струнам электрогитары!
                            targetTorsoRot.Z = side * 14f * pulse;
                            targetLArmRot = new Vector3(-38f, 26f, 14f);
                            targetRArmRot = new Vector3(-55f + 70f * p, -24f, -18f * pulse);
                            break;

                        case "freddy":
                            targetTorsoRot.Y = -22f + 44f * p;
                            targetRArmRot = new Vector3(-92f + 110f * p, -18f * pulse, 0f);
                            break;

                        case "steve":
                            // Классический взмах алмазным мечом / киркой из Minecraft!
                            targetRArmRot = new Vector3(-95f + 115f * p, -16f * pulse, -8f);
                            targetTorsoRot.Y = -14f * pulse;
                            break;

                        case "jotaro":
                            // Джотаро указывает пальцем, пока Star Platinum бьёт ORA ORA!
                            targetTorsoRot.Y = -18f * pulse;
                            targetRArmRot = new Vector3(-78f * pulse, 0f, -10f);
                            targetLArmRot = new Vector3(-24f * pulse, 16f, 12f);
                            break;

                        case "sanabi":
                            // Сокрушительный прямой выпад и апперкот гигантской Механической Рукой-Крюком!
                            targetTorsoRot.X += 22f * pulse;
                            targetTorsoRot.Y = -34f + 68f * p;
                            targetRArmRot = new Vector3(-96f * pulse, -28f + 56f * p, -20f * pulse);
                            targetLArmRot = new Vector3(24f * pulse, 18f, 16f * pulse);
                            break;

                        case "jack_o":
                            // Фирменный высокий пинок Jack-O' каблуком с откидыванием корпуса назад (5H / 6H из GGST)!
                            targetTorsoPos.Y = BaseTorsoPos.Y + 0.05f * pulse;
                            targetTorsoRot.X = -38f * pulse;
                            targetTorsoRot.Y = 24f * pulse * side;
                            targetTorsoRot.Z = 14f * pulse;
                            targetLLegRot = new Vector3(-124f * pulse, -14f * pulse, 10f * pulse);
                            targetRLegRot = new Vector3(18f * pulse, 0f, -6f);
                            targetLArmRot = new Vector3(36f * pulse, 24f, 34f * pulse);
                            targetRArmRot = new Vector3(-56f * pulse, -22f, -32f * pulse);
                            break;
                    }
                }
                else if (ActiveAnim == "m2")
                {
                    switch (charKey)
                    {
                        case "jack_o":
                            // Summon Servant (GGST): подъём обеих рук над головой в магическом круге призыва Слуги!
                            targetTorsoPos.Y = BaseTorsoPos.Y + 0.08f * pulse;
                            targetTorsoRot.X = -16f * pulse;
                            targetLArmRot = new Vector3(-152f * pulse, 18f * pulse, 24f * pulse);
                            targetRArmRot = new Vector3(-152f * pulse, -18f * pulse, -24f * pulse);
                            targetLLegRot.X = -26f * pulse;
                            targetRLegRot.X = 14f * pulse;
                            break;

                        case "sanabi":
                            // Прицельный залп Цепным Гарпуном-Якорем вперёд с рывком лебёдки!
                            targetTorsoRot.X += 26f * pulse;
                            targetTorsoRot.Y = -22f * pulse;
                            targetRArmRot = new Vector3(-92f * pulse, 8f * pulse, -8f);
                            targetLArmRot = new Vector3(-42f * pulse, 28f * pulse, 12f);
                            break;

                        case "sol":
                            if (IsKyKiske)
                            {
                                // Stun Edge / Dire Eclat: широкий диагональный запуск креста молнии!
                                targetTorsoRot.Y = 38f - 76f * p;
                                targetTorsoRot.X += 16f * pulse;
                                targetRArmRot = new Vector3(-92f * pulse, 42f - 84f * p, -20f * pulse);
                                targetLArmRot = new Vector3(-42f * pulse, 28f, 18f);
                            }
                            else
                            {
                                // Bandit Revolver: удар ногой с разворота в прыжке!
                                targetTorsoRot.Y += p * 360f;
                                targetTorsoRot.X = -18f * pulse;
                                targetRLegRot.X = -85f * pulse;
                                targetLLegRot.X = 25f * pulse;
                            }
                            break;

                        case "gwen":
                            // Смыкание гигантских ножниц обеими руками
                            targetTorsoRot.X += 18f * pulse;
                            targetLArmRot = new Vector3(-55f * pulse, 36f * pulse, 0f);
                            targetRArmRot = new Vector3(-55f * pulse, -36f * pulse, 0f);
                            break;

                        case "enforcer":
                            // Мощный таранный удар щитом левой рукой
                            targetTorsoRot.Y = 32f * pulse;
                            targetTorsoRot.X += 18f * pulse;
                            targetLArmRot = new Vector3(-42f * pulse, 38f * pulse, 0f);
                            break;

                        case "huntress":
                            targetTorsoRot.Y = -40f + 80f * p;
                            targetRArmRot.X = -75f + 95f * p;
                            break;

                        case "slark":
                            // Dark Pact: группировка и резкий выброс рук с тёмной волной
                            float pact = p < 0.4f ? -1f : 1f;
                            targetTorsoRot.X += -pact * 22f * pulse;
                            targetLArmRot.X = -45f * pulse;
                            targetRArmRot.X = -45f * pulse;
                            targetLArmRot.Z = 32f * pulse;
                            targetRArmRot.Z = -32f * pulse;
                            break;

                        case "cheburashka":
                            // Бросок взрывного апельсина из-за головы
                            targetRArmRot.X = -115f + 145f * p;
                            targetTorsoRot.X += 16f * pulse;
                            break;

                        case "ahri":
                            // Изящный пируэт призыва трёх Лисьих Огней
                            targetTorsoRot.Y += p * 360f;
                            targetLArmRot = new Vector3(-48f * pulse, 20f, 34f * pulse);
                            targetRArmRot = new Vector3(-48f * pulse, -20f, -34f * pulse);
                            break;

                        case "gold_ship":
                            // Дропкик с разбега обеими ногами вперёд!
                            targetTorsoRot.X = -38f * pulse;
                            targetLLegRot.X = -82f * pulse;
                            targetRLegRot.X = -82f * pulse;
                            targetLArmRot.X = 35f * pulse;
                            targetRArmRot.X = 35f * pulse;
                            break;

                        case "steve":
                            // Добыча киркой (взмах сверху вниз)
                            targetRArmRot = new Vector3(-125f + 150f * p, 0f, 0f);
                            targetTorsoRot.X += 22f * pulse;
                            break;

                        case "freddy":
                            // Выпад скримера (Jumpscare)!
                            targetTorsoRot.X += 28f * pulse;
                            targetLArmRot = new Vector3(-85f * pulse, -25f * pulse, 24f * pulse);
                            targetRArmRot = new Vector3(-85f * pulse, 25f * pulse, -24f * pulse);
                            break;

                        default:
                            targetLArmRot.X = -42f * pulse;
                            targetRArmRot.X = -46f * pulse;
                            targetTorsoRot.X -= 12f * pulse;
                            break;
                    }
                }
                else if (ActiveAnim == "shift")
                {
                    switch (charKey)
                    {
                        case "commando":
                        case "m4a1":
                        case "slark":
                        case "cheburashka":
                            // Сальто / тактический кувырок на 360 градусов вперёд!
                            targetTorsoRot.X += p * 360f;
                            targetLLegRot.X = -45f * pulse;
                            targetRLegRot.X = -45f * pulse;
                            break;

                        case "gwen":
                        case "reimu":
                        case "lappland":
                        case "haze":
                        case "dmc_trio":
                            // Изящный пируэт на 360 градусов
                            targetTorsoRot.Y += p * 360f;
                            targetLArmRot.Z = 32f * pulse;
                            targetRArmRot.Z = -32f * pulse;
                            break;

                        case "vyper":
                            // Низкое скольжение рептилии (Slither Slide)
                            targetTorsoPos.Y = BaseTorsoPos.Y - 0.35f * pulse;
                            targetTorsoRot.X = 38f * pulse;
                            targetLLegRot.X = -55f * pulse;
                            targetRLegRot.X = 35f * pulse;
                            break;

                        case "ahri":
                            // Призрачный Рывок (Spirit Rush): стремительный полёт девятихвостой лисы
                            targetTorsoRot.X += 38f * pulse;
                            targetLArmRot = new Vector3(34f * pulse, 0f, 26f * pulse);
                            targetRArmRot = new Vector3(-65f * pulse, 0f, -18f * pulse);
                            targetLLegRot.X = 32f * pulse;
                            targetRLegRot.X = -22f * pulse;
                            break;

                        case "buzz":
                            // «Бесконечность — не предел!»: горизонтальный полёт с вытянутым вперёд кулаком!
                            targetTorsoRot.X = 52f * pulse;
                            targetRArmRot.X = -95f * pulse;
                            targetLArmRot.X = 30f * pulse;
                            targetLLegRot.X = 28f * pulse;
                            targetRLegRot.X = 28f * pulse;
                            break;

                        case "sol":
                            if (IsKyKiske)
                            {
                                // Stun Dipper (GGST): низкий скользящий подкат по земле с вытянутой вперёд правой ногой!
                                float slideHold = p < 0.78f ? MathF.Min(1f, p / 0.16f) : (1f - p) / 0.22f;
                                targetTorsoPos.Y = BaseTorsoPos.Y - 0.44f * slideHold;
                                targetTorsoRot.X = -42f * slideHold;
                                targetTorsoRot.Y = -22f * slideHold;
                                targetTorsoRot.Z = 8f * slideHold;
                                targetRLegPos = BaseRightLegPos + new Vector3(0.04f * slideHold, 0.08f * slideHold, 0.26f * slideHold);
                                targetRLegRot = new Vector3(-84f * slideHold, -8f * slideHold, -6f * slideHold);
                                targetLLegPos = BaseLeftLegPos + new Vector3(-0.04f * slideHold, 0.12f * slideHold, -0.08f * slideHold);
                                targetLLegRot = new Vector3(58f * slideHold, 14f * slideHold, 12f * slideHold);
                                targetRArmRot = new Vector3(44f * slideHold, -36f * slideHold, -28f * slideHold);
                                targetLArmRot = new Vector3(-52f * slideHold, 28f * slideHold, 24f * slideHold);
                            }
                            else
                            {
                                // Bandit Bringer: взлёт с апперкотом и пикирующий огненный кулак вниз
                                targetTorsoRot.X = p < 0.45f ? -18f : 36f;
                                targetRArmRot.X = p < 0.45f ? -145f : 42f;
                            }
                            break;

                        case "jack_o":
                            // Прыжок с Командным Флагом в правой руке и вонзание древка в землю при приземлении!
                            if (p < 0.68f)
                            {
                                targetTorsoRot.X = -14f * pulse;
                                targetTorsoRot.Y = -16f * pulse;
                                targetRArmRot = new Vector3(-138f, -12f, -14f);
                                targetLArmRot = new Vector3(-34f, 22f, 28f);
                                targetLLegRot.X = -42f * pulse;
                                targetRLegRot.X = 28f * pulse;
                            }
                            else
                            {
                                float slamP = (p - 0.68f) / 0.32f;
                                targetTorsoPos.Y = BaseTorsoPos.Y - 0.16f * slamP;
                                targetTorsoRot.X = 28f * slamP;
                                targetRArmRot = new Vector3(-42f, -8f, -6f);
                                targetLArmRot = new Vector3(24f, 16f, 22f);
                            }
                            break;

                        case "sanabi":
                            // Маятниковый свинг на Цепном Крюке с акробатическим переворотом в воздухе!
                            targetTorsoRot.X = -28f + p * 360f;
                            targetRArmRot = new Vector3(-135f * pulse, 0f, -16f);
                            targetLArmRot = new Vector3(32f * pulse, 0f, 24f);
                            targetLLegRot.X = -52f * pulse;
                            targetRLegRot.X = 38f * pulse;
                            break;

                        default:
                            targetTorsoRot.X += 32f * pulse;
                            targetLArmRot.X = 28f * pulse;
                            targetRArmRot.X = 28f * pulse;
                            break;
                    }
                }
                else if (ActiveAnim == "r")
                {
                    switch (charKey)
                    {
                        case "sol":
                        case "jotaro":
                        case "dmc_trio":
                        case "pushnoy":
                        case "sanabi":
                            // Пробуждение силы / ZA WARUDO / Sin Devil Trigger / Протокол САННАБИ!
                            targetTorsoRot.X = -26f * pulse;
                            targetLArmRot = new Vector3(-85f * pulse, 0f, 38f * pulse);
                            targetRArmRot = new Vector3(-85f * pulse, 0f, -38f * pulse);
                            break;

                        case "tidehunter":
                            // Ravage: сокрушительный удар якорем и кулаками о землю
                            targetTorsoRot.X = p < 0.4f ? -28f : 36f;
                            targetLArmRot.X = p < 0.4f ? -130f : 38f;
                            targetRArmRot.X = p < 0.4f ? -130f : 38f;
                            break;

                        case "huntress":
                        case "artificer":
                        case "aigis":
                        case "reimu":
                        case "dante":
                        case "haze":
                        case "lappland":
                            targetTorsoRot.Y += p * 360f;
                            targetLArmRot.X = -110f * pulse;
                            targetRArmRot.X = -110f * pulse;
                            break;

                        case "cheburashka":
                            // Эпичная JoJo-поза призыва стенда Крокодила Гены!
                            targetTorsoRot.Y = -22f * pulse;
                            targetRArmRot.X = -70f * pulse;
                            targetLArmRot.Z = 25f * pulse;
                            break;

                        case "ahri":
                            // Воздушный поцелуй Очарования (Charm) с грациозным наклоном
                            targetTorsoRot.Y = -18f * (1f - p) + 22f * p;
                            targetTorsoRot.Z = 10f * pulse;
                            targetLArmRot = new Vector3(-88f * pulse, 28f * (1f - p), 14f * pulse);
                            targetRArmRot = new Vector3(-42f * pulse, -24f * pulse, -24f * pulse);
                            break;

                        default:
                            targetLArmRot.X = -55f * pulse;
                            targetRArmRot.X = -55f * pulse;
                            targetTorsoRot.X -= 10f * pulse;
                            break;
                    }
                }
                else if (ActiveAnim == "ky_charge")
                {
                    // Зарядка Stun Edge Каем Киске: отводит Thunderseal к плечу, концентрируя золотисто-алую молнию!
                    float tremor = MathF.Sin(t * 42f) * 2.2f;
                    targetTorsoPos.Y = BaseTorsoPos.Y - 0.08f;
                    targetTorsoRot = new Vector3(-8f, 38f + tremor, 0f);
                    targetRArmRot = new Vector3(-116f + tremor, 42f, -24f);
                    targetLArmRot = new Vector3(-52f, 34f, 18f);
                    targetLLegRot.X = -24f;
                    targetRLegRot.X = 22f;
                }
                else if (ActiveAnim == "ky_dipper")
                {
                    // Низкий подкат Stun Dipper по референсу GGST_Stun_Dipper_1.webp
                    float slideHold = p < 0.80f ? MathF.Min(1f, p / 0.14f) : (1f - p) / 0.20f;
                    targetTorsoPos.Y = BaseTorsoPos.Y - 0.46f * slideHold;
                    targetTorsoRot = new Vector3(-44f * slideHold, -20f * slideHold, 8f * slideHold);
                    targetRLegPos = BaseRightLegPos + new Vector3(0.04f * slideHold, 0.09f * slideHold, 0.28f * slideHold);
                    targetRLegRot = new Vector3(-85f * slideHold, -8f * slideHold, -6f * slideHold);
                    targetLLegPos = BaseLeftLegPos + new Vector3(-0.04f * slideHold, 0.12f * slideHold, -0.08f * slideHold);
                    targetLLegRot = new Vector3(60f * slideHold, 14f * slideHold, 12f * slideHold);
                    targetRArmRot = new Vector3(46f * slideHold, -36f * slideHold, -28f * slideHold);
                    targetLArmRot = new Vector3(-54f * slideHold, 28f * slideHold, 24f * slideHold);
                }
                else if (ActiveAnim == "ky_rtl")
                {
                    // Горизонтальный полёт Ride the Lightning с вытянутым вперёд клинком в магический барьер!
                    targetTorsoPos.Y = BaseTorsoPos.Y + 0.14f;
                    targetTorsoRot = new Vector3(68f, -10f, 0f);
                    targetRArmRot = new Vector3(-158f, -8f, 0f);
                    targetLArmRot = new Vector3(36f, 18f, 24f);
                    targetLLegRot.X = 34f;
                    targetRLegRot.X = 28f;
                }
                else if (ActiveAnim == "jack_r_lunge")
                {
                    // Фаза 1 Forever Elysion Driver: стремительный прыжок вперёд с Железной Тыквой в вытянутой руке!
                    targetTorsoRot = new Vector3(34f, -22f, 0f);
                    targetRArmRot = new Vector3(-102f, -14f, -12f);
                    targetLArmRot = new Vector3(28f, 18f, 24f);
                    targetLLegRot.X = -46f;
                    targetRLegRot.X = 34f;
                }
                else if (ActiveAnim == "jack_r_rise")
                {
                    // Фаза 2 Forever Elysion Driver: захватывает насаженного на шар врага обеими руками и взмывает с вращением в небо!
                    targetTorsoRot = new Vector3(-18f, p * 720f, 0f);
                    targetLArmRot = new Vector3(-128f, 26f, 16f);
                    targetRArmRot = new Vector3(-128f, -26f, -16f);
                    targetLLegRot.X = -42f;
                    targetRLegRot.X = -32f;
                }
                else if (ActiveAnim == "jack_r_slam")
                {
                    // Фаза 3 Forever Elysion Driver: сокрушительное пикирование головой/шаром с врагом вниз в землю!
                    targetTorsoRot = new Vector3(148f * MathF.Min(1f, p * 1.5f), 0f, 0f);
                    targetLArmRot = new Vector3(-142f, 22f, 12f);
                    targetRArmRot = new Vector3(-142f, -22f, -12f);
                    targetLLegRot.X = -24f;
                    targetRLegRot.X = -24f;
                }
            }

            // Показ командного флага в руке Jack-O' во время прыжка Shift!
            if (charKey == "jack_o" && Shield != null && !Shield.IsDestroyed)
            {
                bool showHandFlag = AnimTimer > 0f && ActiveAnim == "shift";
                if (Shield.Enabled != showHandFlag) Shield.Enabled = showHandFlag;
            }

            // Амортизация приземления (Landing Squash) — живое сгибание коленей и корпуса после прыжка
            if (LandingSquash > 0.001f && string.IsNullOrEmpty(ActiveEmote))
            {
                float sq = MathF.Sin((LandingSquash / 0.24f) * MathF.PI * 0.5f);
                targetTorsoPos.Y -= sq * 0.14f;
                targetTorsoRot.X += sq * 9f;
                targetLLegRot.X -= sq * 22f;
                targetRLegRot.X += sq * 16f;
            }

            // 4. КОЛЕСО ЭМОЦИЙ [L] — ПОЛНЫЙ НАБОР ПРОЦЕДУРНЫХ АНИМАЦИЙ + ПАРНАЯ «КАМЕНЬ-НОЖНИЦЫ-БУМАГА»
            float emoteHeadX = 0f, emoteHeadY = 0f, emoteHeadZ = 0f;
            bool hasEmoteHead = false;
            bool isCheb = charKey == "cheburashka";
            bool isTide = charKey == "tidehunter";
            float crouchScale = isCheb ? 0.44f : 1.0f;

            if (!string.IsNullOrEmpty(ActiveEmote))
            {
                float et = EmoteTime;
                switch (ActiveEmote)
                {
                    case "chair":
                    case "plastic_chair":
                    case "vergil_chair":
                    {
                        // 1. Сидение на белом пластиковом стуле (Мотивация Вергилия)
                        if (isCheb)
                        {
                            // Коротконогий Чебурашка уютно сидит НА сиденье стула и болтает лапками
                            float pawSwing = MathF.Sin(t * 3.4f) * 11f;
                            targetTorsoPos = BaseTorsoPos + new Vector3(0f, -0.03f + MathF.Sin(t * 2.2f) * 0.012f, -0.08f);
                            targetLLegPos = BaseLeftLegPos + new Vector3(-0.02f, 0.05f, 0.12f);
                            targetRLegPos = BaseRightLegPos + new Vector3(0.02f, 0.05f, 0.12f);
                            targetLLegRot = new Vector3(-72f + pawSwing, -10f, 4f);
                            targetRLegRot = new Vector3(-72f - pawSwing, 10f, -4f);
                        }
                        else if (isTide)
                        {
                            targetTorsoPos = BaseTorsoPos + new Vector3(0f, -0.24f + MathF.Sin(t * 2.0f) * 0.015f, -0.08f);
                            targetLLegPos = BaseLeftLegPos + new Vector3(-0.03f, -0.18f, 0.12f);
                            targetRLegPos = BaseRightLegPos + new Vector3(0.03f, -0.18f, 0.12f);
                            targetLLegRot = new Vector3(-68f, -12f, 5f);
                            targetRLegRot = new Vector3(-66f, 12f, -5f);
                        }
                        else
                        {
                            targetTorsoPos = BaseTorsoPos + new Vector3(0f, -0.34f + MathF.Sin(t * 2.0f) * 0.015f, -0.08f);
                            targetLLegPos = BaseLeftLegPos + new Vector3(-0.03f, -0.20f, 0.10f);
                            targetRLegPos = BaseRightLegPos + new Vector3(0.03f, -0.20f, 0.10f);
                            targetLLegRot = new Vector3(-70f, -10f, 4f);
                            targetRLegRot = new Vector3(-66f, 10f, -4f);
                        }
                        targetTorsoRot = new Vector3(-11f, MathF.Sin(t * 0.8f) * 4f, 0f);
                        targetLArmRot = new Vector3(-38f, 26f, 10f);
                        targetRArmRot = new Vector3(-42f, -28f, -10f);
                        emoteHeadX = 6f + MathF.Sin(t * 1.4f) * 3f;
                        emoteHeadY = MathF.Sin(t * 0.7f) * 10f;
                        emoteHeadZ = MathF.Cos(t * 0.7f) * 3.5f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "california":
                    case "california_girls":
                    case "california_gurls":
                    {
                        // 2. Калифорния гёрлс — мемный танец с покачиванием бёдер и руками за головой
                        float beat = et * 6.8f;
                        float sway = MathF.Sin(beat);
                        float bounce = MathF.Abs(MathF.Cos(beat));
                        bool handsUpPhase = (et % 3.6f) < 1.8f;
                        targetTorsoPos = BaseTorsoPos + new Vector3(sway * 0.15f * crouchScale, bounce * 0.07f * crouchScale, 0f);
                        targetTorsoRot = new Vector3(MathF.Sin(beat * 0.5f) * 6f, sway * 24f, sway * 13f);
                        targetLLegRot = new Vector3(sway * 26f, 0f, sway * 9f);
                        targetRLegRot = new Vector3(-sway * 26f, 0f, sway * 9f);
                        if (handsUpPhase)
                        {
                            targetLArmRot = new Vector3(-136f + bounce * 12f, 28f, 34f + sway * 10f);
                            targetRArmRot = new Vector3(-136f + bounce * 12f, -28f, -34f + sway * 10f);
                        }
                        else
                        {
                            targetLArmRot = new Vector3(-42f + sway * 28f, 32f, 24f);
                            targetRArmRot = new Vector3(-42f - sway * 28f, -32f, -24f);
                        }
                        emoteHeadZ = -sway * 12f;
                        emoteHeadY = -sway * 10f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "caramelldansen":
                    case "caramell":
                    {
                        // 3. Карамеллдансен — быстрый евробит-танец с «кроличьими ушками» над головой и вилянием бёдрами!
                        float beat = et * 11.4f;
                        float sway = MathF.Sin(beat);
                        float bounce = MathF.Abs(MathF.Cos(beat));
                        float lPop = MathF.Max(0f, sway);
                        float rPop = MathF.Max(0f, -sway);

                        targetTorsoPos = BaseTorsoPos + new Vector3(-sway * 0.16f * crouchScale, (-0.03f + bounce * 0.10f) * crouchScale, 0f);
                        targetTorsoRot = new Vector3(-4f + bounce * 6f, sway * 22f, -sway * 14f);
                        // Ладони сложены «ушками» над макушкой и синхронно качаются влево-вправо в такт голове
                        targetLArmRot = new Vector3(-128f + bounce * 14f, 22f - sway * 10f, 26f + sway * 18f);
                        targetRArmRot = new Vector3(-128f + bounce * 14f, -22f - sway * 10f, -26f + sway * 18f);
                        // Пружинящие колени с поочерёдным подбросом пяток назад по канону аниме-опенинга
                        targetLLegRot = new Vector3(10f + lPop * 36f - rPop * 14f, -6f, -sway * 11f);
                        targetRLegRot = new Vector3(10f + rPop * 36f - lPop * 14f, 6f, -sway * 11f);

                        emoteHeadZ = -sway * 15f;
                        emoteHeadY = -sway * 10f;
                        emoteHeadX = -4f + bounce * 6f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "fortnite":
                    case "default_dance":
                    {
                        // 4. Стандартный танец Фортнайта (Dance Moves / Default Dance) — 3 фазы с хлопком и указанием пальцем!
                        float cyc = et % 3.2f;
                        float step = MathF.Sin(et * 10.5f);
                        float hop = MathF.Abs(MathF.Cos(et * 10.5f));
                        if (cyc < 1.3f)
                        {
                            // Фаза 1: Энергичные перекрёстные взмахи руками и чечётка коленями
                            targetTorsoPos = BaseTorsoPos + new Vector3(step * 0.08f * crouchScale, hop * 0.09f * crouchScale, 0f);
                            targetTorsoRot = new Vector3(6f, step * 26f, step * 8f);
                            targetLArmRot = new Vector3(-68f + step * 44f, 28f - step * 34f, 18f);
                            targetRArmRot = new Vector3(-68f - step * 44f, -28f - step * 34f, -18f);
                            targetLLegRot = new Vector3(MathF.Max(0f, step) * -48f, 0f, 8f);
                            targetRLegRot = new Vector3(MathF.Max(0f, -step) * -48f, 0f, -8f);
                            emoteHeadX = hop * 7f - 3f;
                            emoteHeadY = -step * 10f;
                            emoteHeadZ = step * 7f;
                            hasEmoteHead = true;
                        }
                        else if (cyc < 2.2f)
                        {
                            // Фаза 2: Разворот на 360° и фирменный хлопок над головой!
                            float sp = (cyc - 1.3f) / 0.9f;
                            float clap = MathF.Sin(sp * MathF.PI);
                            targetTorsoPos = BaseTorsoPos + new Vector3(0f, clap * 0.16f * crouchScale, 0f);
                            targetTorsoRot = new Vector3(-10f * clap, sp * 360f, 0f);
                            targetLArmRot = new Vector3(-148f * clap - 20f, 36f * clap, 14f);
                            targetRArmRot = new Vector3(-148f * clap - 20f, -36f * clap, -14f);
                            targetLLegRot = new Vector3(-24f * clap, 0f, 10f);
                            targetRLegRot = new Vector3(22f * clap, 0f, -10f);
                            emoteHeadX = -14f * clap;
                            hasEmoteHead = true;
                        }
                        else
                        {
                            // Фаза 3: Скрещённые руки + дерзкий жест указательным пальцем вперёд под бит!
                            float pt = (cyc - 2.2f) / 1.0f;
                            float groove = MathF.Sin(pt * MathF.PI * 2f);
                            targetTorsoPos = BaseTorsoPos + new Vector3(0f, MathF.Abs(groove) * 0.06f * crouchScale, 0f);
                            targetTorsoRot = new Vector3(-6f, -18f + groove * 8f, groove * 6f);
                            targetLArmRot = new Vector3(-52f, 44f, 12f);
                            targetRArmRot = new Vector3(-86f + groove * 8f, -8f, -6f);
                            targetLLegRot = new Vector3(-14f, 0f, 8f);
                            targetRLegRot = new Vector3(14f, 0f, -8f);
                            emoteHeadZ = 10f;
                            emoteHeadY = 14f;
                            hasEmoteHead = true;
                        }
                        break;
                    }
                    case "rps_wait":
                    {
                        // 5a. Ожидание партнёра для «Камень-Ножницы-Бумага»: постукивание кулаком о ладонь
                        float tap = MathF.Abs(MathF.Sin(et * 5.8f));
                        targetTorsoPos = BaseTorsoPos + new Vector3(0f, tap * 0.04f * crouchScale, 0f);
                        targetTorsoRot = new Vector3(4f, MathF.Sin(et * 2.0f) * 8f, 0f);
                        targetLArmRot = new Vector3(-56f, 28f, 8f);
                        targetRArmRot = new Vector3(-60f - tap * 28f, -24f, -8f);
                        emoteHeadY = MathF.Sin(et * 2.2f) * 16f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "rps":
                    case "rps_count":
                    {
                        // 5b. Синхронный отсчёт «Камень... Ножницы... Бумага... РАЗ-ДВА-ТРИ!»
                        float pump = MathF.Abs(MathF.Sin(et * MathF.PI * 2.6f));
                        targetTorsoPos = BaseTorsoPos + new Vector3(0f, (1f - pump) * 0.06f * crouchScale, 0f);
                        targetTorsoRot = new Vector3(12f * pump, -8f, 0f);
                        targetLArmRot = new Vector3(-54f, 26f, 8f);
                        targetRArmRot = new Vector3(-52f - pump * 62f, -18f, -8f);
                        targetLLegRot = new Vector3(-12f * pump, 0f, 0f);
                        targetRLegRot = new Vector3(12f * pump, 0f, 0f);
                        emoteHeadX = 10f * pump;
                        hasEmoteHead = true;
                        break;
                    }
                    case "rps_win":
                    {
                        // 5c. Победа в КНБ — триумфальные прыжки с поднятыми кулаками!
                        float jmp = MathF.Abs(MathF.Sin(et * 8.5f));
                        targetTorsoPos = BaseTorsoPos + new Vector3(0f, jmp * 0.22f * crouchScale, 0f);
                        targetTorsoRot = new Vector3(-12f, MathF.Sin(et * 8.5f) * 14f, 0f);
                        targetLArmRot = new Vector3(-148f + jmp * 18f, 0f, 28f);
                        targetRArmRot = new Vector3(-148f + jmp * 18f, 0f, -28f);
                        targetLLegRot = new Vector3(-26f * jmp, 0f, 8f);
                        targetRLegRot = new Vector3(22f * jmp, 0f, -8f);
                        emoteHeadX = -10f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "rps_lose":
                    {
                        // 5d. Поражение в КНБ — хватается за голову в отчаянии
                        targetTorsoPos = BaseTorsoPos + new Vector3(0f, -0.22f * crouchScale, 0f);
                        targetTorsoRot = new Vector3(34f, MathF.Sin(et * 9f) * 8f, 0f);
                        targetLArmRot = new Vector3(-128f, 28f, 18f);
                        targetRArmRot = new Vector3(-128f, -28f, -18f);
                        targetLLegRot = new Vector3(-28f, 0f, 0f);
                        targetRLegRot = new Vector3(-28f, 0f, 0f);
                        emoteHeadX = 22f;
                        emoteHeadY = MathF.Sin(et * 11f) * 18f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "rps_draw":
                    {
                        // 5e. Боевая ничья в КНБ — разводит руками в стороны
                        targetTorsoRot = new Vector3(-4f, MathF.Sin(et * 4f) * 8f, MathF.Sin(et * 4f) * 6f);
                        targetLArmRot = new Vector3(-48f, 0f, 44f);
                        targetRArmRot = new Vector3(-48f, 0f, -44f);
                        emoteHeadZ = 14f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "kazotsky":
                    case "squat_dance":
                    case "prisyadka":
                    {
                        // 6. Танец вприсядку (Казачок / Kazotsky Kick) — присядка со скрещёнными руками, «ХЭЙ!» и выбросом ног!
                        float kickSin = MathF.Sin(et * 9.6f);
                        float hop = MathF.Abs(MathF.Cos(et * 9.6f));
                        float lKick = MathF.Max(0f, kickSin);
                        float rKick = MathF.Max(0f, -kickSin);
                        bool heyFlourish = (et % 4.0f) >= 3.0f;

                        targetTorsoPos = BaseTorsoPos + new Vector3(kickSin * 0.05f * crouchScale, (-0.28f + hop * 0.11f) * crouchScale, -0.04f);
                        targetTorsoRot = new Vector3(-8f, kickSin * 12f, -kickSin * 7f);
                        if (heyFlourish)
                        {
                            // Задорный взмах руками в стороны на каждый 4-й такт («ХЭЙ!»)
                            targetLArmRot = new Vector3(-122f + hop * 12f, -10f, 38f);
                            targetRArmRot = new Vector3(-122f + hop * 12f, 10f, -38f);
                        }
                        else
                        {
                            targetLArmRot = new Vector3(-66f, 48f, 12f);
                            targetRArmRot = new Vector3(-66f, -48f, -12f);
                        }
                        targetLLegPos = BaseLeftLegPos + new Vector3(0f, -0.10f * crouchScale, 0.08f);
                        targetRLegPos = BaseRightLegPos + new Vector3(0f, -0.10f * crouchScale, 0.08f);
                        targetLLegRot = new Vector3(-36f - 54f * lKick, 0f, 8f);
                        targetRLegRot = new Vector3(-36f - 54f * rKick, 0f, -8f);
                        emoteHeadZ = kickSin * 8f;
                        emoteHeadX = heyFlourish ? -10f : 2f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "gangnam":
                    case "gangnam_style":
                    {
                        // 7. Gangnam Style — скачка на невидимом коне + вращение лассо над головой!
                        float gallop = MathF.Abs(MathF.Sin(et * 8.6f));
                        float sideStep = MathF.Sin(et * 4.3f);
                        bool lassoPhase = (et % 3.2f) >= 1.6f;
                        targetTorsoPos = BaseTorsoPos + new Vector3(sideStep * 0.09f * crouchScale, (-0.10f + gallop * 0.16f) * crouchScale, 0f);
                        targetTorsoRot = new Vector3(6f - gallop * 8f, sideStep * 14f, sideStep * 6f);
                        targetLLegRot = new Vector3(-22f - MathF.Max(0f, sideStep) * 28f, -12f, 16f);
                        targetRLegRot = new Vector3(-22f - MathF.Max(0f, -sideStep) * 28f, 12f, -16f);
                        if (!lassoPhase)
                        {
                            targetLArmRot = new Vector3(-72f - gallop * 18f, 34f, 10f);
                            targetRArmRot = new Vector3(-76f - gallop * 18f, -34f, -10f);
                        }
                        else
                        {
                            targetLArmRot = new Vector3(-56f, 28f, 14f);
                            targetRArmRot = new Vector3(-148f, MathF.Sin(et * 18f) * 36f, -22f + MathF.Cos(et * 18f) * 34f);
                        }
                        emoteHeadX = -gallop * 8f;
                        emoteHeadY = sideStep * 10f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "sun":
                    case "praise_the_sun":
                    case "praise_sun":
                    {
                        // 8. Восхвали солнце! (Praise the Sun \[T]/)
                        float glow = MathF.Sin(et * 2.4f) * 0.03f;
                        targetTorsoPos = BaseTorsoPos + new Vector3(0f, 0.05f + glow, 0f);
                        targetTorsoRot = new Vector3(-18f, 0f, 0f);
                        targetLArmRot = new Vector3(-148f, -6f, 36f);
                        targetRArmRot = new Vector3(-148f, 6f, -36f);
                        targetLLegRot = new Vector3(5f, -6f, 7f);
                        targetRLegRot = new Vector3(5f, 6f, -7f);
                        emoteHeadX = -26f;
                        emoteHeadY = MathF.Sin(et * 1.2f) * 3f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "jojo":
                    case "jojo_pose":
                    {
                        // 9. JoJo Поза (Menacing) — драматичный прогиб Араки с ладонью у лица!
                        float rumble = MathF.Sin(et * 22f) * 1.2f;
                        targetTorsoPos = BaseTorsoPos + new Vector3(-0.05f * crouchScale, -0.04f * crouchScale, 0f);
                        targetTorsoRot = new Vector3(-20f, -26f + rumble, 16f);
                        targetLArmRot = new Vector3(-114f, 40f, 18f);
                        targetRArmRot = new Vector3(-68f, -32f, -42f);
                        targetLLegRot = new Vector3(-22f, -12f, 12f);
                        targetRLegRot = new Vector3(18f, 14f, -10f);
                        emoteHeadX = 12f;
                        emoteHeadY = 24f;
                        emoteHeadZ = -14f;
                        hasEmoteHead = true;
                        break;
                    }
                    case "distraction":
                    case "stickmin":
                    {
                        // 10. Танец Отвлечения (Henry Stickmin Distraction Dance): хлопок -> наклон влево с разведёнными руками -> хлопок -> наклон вправо!
                        float cyc = et % 1.6f;
                        if (cyc < 0.40f || (cyc >= 0.80f && cyc < 1.20f))
                        {
                            float p = (cyc % 0.40f) / 0.40f;
                            float clap = MathF.Sin(p * MathF.PI);
                            targetTorsoPos = BaseTorsoPos + new Vector3(0f, clap * 0.08f * crouchScale, 0f);
                            targetTorsoRot = new Vector3(6f * clap, 0f, 0f);
                            targetLArmRot = new Vector3(-76f, 24f + 32f * clap, 10f);
                            targetRArmRot = new Vector3(-76f, -24f - 32f * clap, -10f);
                            targetLLegRot = new Vector3(-18f * (1f - clap), 0f, 6f);
                            targetRLegRot = new Vector3(-18f * (1f - clap), 0f, -6f);
                            emoteHeadX = 10f * clap;
                            hasEmoteHead = true;
                        }
                        else
                        {
                            bool leanLeft = cyc < 0.80f;
                            float dir = leanLeft ? -1f : 1f;
                            float p = (leanLeft ? (cyc - 0.40f) : (cyc - 1.20f)) / 0.40f;
                            float pose = MathF.Pow(MathF.Max(0f, MathF.Sin(p * MathF.PI)), 0.65f);

                            targetTorsoPos = BaseTorsoPos + new Vector3(dir * 0.17f * pose * crouchScale, -0.13f * pose * crouchScale, 0f);
                            targetTorsoRot = new Vector3(8f * pose, dir * 20f * pose, -dir * 18f * pose);
                            if (leanLeft)
                            {
                                targetLArmRot = new Vector3(-124f * pose - 22f * (1f - pose), -14f * pose, 48f * pose);
                                targetRArmRot = new Vector3(-56f * pose - 22f * (1f - pose), 14f * pose, -50f * pose);
                                targetLLegRot = new Vector3(-38f * pose, -10f * pose, 16f * pose);
                                targetRLegRot = new Vector3(16f * pose, 12f * pose, -24f * pose);
                            }
                            else
                            {
                                targetLArmRot = new Vector3(-56f * pose - 22f * (1f - pose), -14f * pose, 50f * pose);
                                targetRArmRot = new Vector3(-124f * pose - 22f * (1f - pose), 14f * pose, -48f * pose);
                                targetLLegRot = new Vector3(16f * pose, -12f * pose, 24f * pose);
                                targetRLegRot = new Vector3(-38f * pose, 10f * pose, -16f * pose);
                            }
                            emoteHeadZ = -dir * 18f * pose;
                            emoteHeadY = -dir * 16f * pose;
                            emoteHeadX = -6f * pose;
                            hasEmoteHead = true;
                        }
                        break;
                    }
                    default:
                    {
                        // Универсальный бодрый танец на случай нестандартного ID эмоции
                        float groove = MathF.Sin(et * 8.0f);
                        float bounce = MathF.Abs(MathF.Cos(et * 8.0f));
                        targetTorsoPos = BaseTorsoPos + new Vector3(groove * 0.10f * crouchScale, bounce * 0.08f * crouchScale, 0f);
                        targetTorsoRot = new Vector3(0f, groove * 18f, -groove * 10f);
                        targetLArmRot = new Vector3(-85f + groove * 30f, 18f, 24f);
                        targetRArmRot = new Vector3(-85f - groove * 30f, -18f, -24f);
                        targetLLegRot = new Vector3(groove * 22f, 0f, 6f);
                        targetRLegRot = new Vector3(-groove * 22f, 0f, -6f);
                        emoteHeadZ = -groove * 12f;
                        hasEmoteHead = true;
                        break;
                    }
                }
            }

            // Призыв Стенда Крокодила Гены у Чебурашки на время эмоций, чтобы Гена с гармошкой тоже танцевал и позировал!
            if (isCheb && ShieldAura != null && !ShieldAura.IsDestroyed)
            {
                if (!string.IsNullOrEmpty(ActiveEmote))
                {
                    if (!ShieldAura.Enabled)
                    {
                        ShieldAura.Enabled = true;
                        _emoteSpawnedGena = true;
                    }
                }
                else if (_emoteSpawnedGena)
                {
                    ShieldAura.Enabled = false;
                    _emoteSpawnedGena = false;
                }
            }

            // Выразительная анимация Стенда Крокодила Гены (у Чебурашки), Star Platinum (у Джотаро) и Дрона Мари (у Генерала SANABI)
            if (charKey is "cheburashka" or "jotaro" && ShieldAura != null && ShieldAura.Enabled)
            {
                float baseX = charKey == "jotaro" ? 0.68f : 0.85f;
                float baseY = charKey == "jotaro" ? 0.24f : 0.68f;
                Vector3 standPos;
                Vector3 standRot;

                if (!string.IsNullOrEmpty(ActiveEmote))
                {
                    float et = EmoteTime;
                    switch (ActiveEmote)
                    {
                        case "chair":
                        case "plastic_chair":
                        case "vergil_chair":
                            // Стенд невозмутимо парит за спинкой пластикового стула как верный телохранитель
                            standPos = new Vector3(baseX + 0.08f, baseY + 0.10f + MathF.Sin(t * 2.4f) * 0.05f, -0.56f);
                            standRot = new Vector3(-6f, MathF.Sin(t * 1.1f) * 14f, 0f);
                            break;
                        case "jojo":
                        case "jojo_pose":
                            // Синхронная грозная JoJo-стойка плечом к плечу с хозяином!
                            standPos = new Vector3(baseX + 0.10f, baseY + 0.16f + MathF.Sin(t * 3.0f) * 0.04f, -0.24f);
                            standRot = new Vector3(-18f, 22f + MathF.Sin(et * 22f) * 2.5f, -14f);
                            break;
                        case "sun":
                        case "praise_the_sun":
                        case "praise_sun":
                            // Стенд возносится за спиной, тоже восхваляя Солнце!
                            standPos = new Vector3(0f, baseY + 0.56f + MathF.Sin(et * 2.4f) * 0.05f, -0.62f);
                            standRot = new Vector3(-22f, 0f, 0f);
                            break;
                        case "rps_win":
                            float winHop = MathF.Abs(MathF.Sin(et * 8.5f));
                            standPos = new Vector3(baseX, baseY + winHop * 0.28f, -0.32f);
                            standRot = new Vector3(-14f, MathF.Sin(et * 12f) * 20f, 0f);
                            break;
                        case "rps_lose":
                            standPos = new Vector3(baseX, baseY - 0.18f, -0.48f);
                            standRot = new Vector3(28f, MathF.Sin(et * 10f) * 14f, 0f);
                            break;
                        default:
                            // Бодрый танец Стенда (и игра Гены на гармошке) в такт любой танцевальной эмоции!
                            float beatSpd = ActiveEmote is "caramelldansen" or "caramell" ? 11.4f : 8.4f;
                            float sBeat = MathF.Sin(et * beatSpd);
                            float sHop = MathF.Abs(MathF.Cos(et * beatSpd));
                            standPos = new Vector3(baseX + sBeat * 0.22f, baseY + sHop * 0.18f, -0.42f);
                            standRot = new Vector3(MathF.Cos(et * beatSpd) * 10f, sBeat * 24f, -sBeat * 14f);
                            break;
                    }
                }
                else
                {
                    float standPulse = (AnimTimer > 0f && ActiveAnim is "m1" or "m2" or "r")
                        ? MathF.Sin((1f - AnimTimer / MathF.Max(0.01f, AnimDuration)) * MathF.PI)
                        : 0f;
                    standPos = new Vector3(
                        baseX + MathF.Sin(t * 3.5f) * 0.10f - standPulse * 0.45f,
                        baseY + MathF.Cos(t * 2.8f) * 0.06f,
                        -0.58f + standPulse * 1.35f);
                    standRot = new Vector3(
                        standPulse * 18f,
                        standPulse > 0.01f ? MathF.Sin(t * 38f) * 22f : MathF.Sin(t * 2.5f) * 6f,
                        0f);
                }

                float standBlend = MathF.Min(1f, dt * 14f);
                ShieldAura.Position = Lerp(ShieldAura.Position, standPos, standBlend);
                ShieldAura.Rotation = LerpEulerDeg(ShieldAura.Rotation, standRot, standBlend);
            }
            else if (charKey == "sanabi" && ShieldAura != null && ShieldAura.Enabled)
            {
                Vector3 mariPos;
                Vector3 mariRot;
                if (!string.IsNullOrEmpty(ActiveEmote))
                {
                    float et = EmoteTime;
                    switch (ActiveEmote)
                    {
                        case "chair":
                        case "plastic_chair":
                        case "vergil_chair":
                            // Дрон Мари уютно устраивается над левым подлокотником стула Генерала
                            mariPos = new Vector3(-0.44f, 0.24f + MathF.Sin(t * 3.0f) * 0.035f, 0.22f);
                            mariRot = new Vector3(-6f, 22f + MathF.Sin(t * 1.5f) * 10f, MathF.Sin(t * 2.5f) * 6f);
                            break;
                        case "sun":
                        case "praise_the_sun":
                        case "praise_sun":
                            // Мари взмывает прямо над головой Генерала как сияющая путеводная звезда!
                            mariPos = new Vector3(0f, 1.10f + MathF.Sin(et * 4.0f) * 0.06f, 0.08f);
                            mariRot = new Vector3(-12f, et * 180f, 0f);
                            break;
                        case "caramelldansen":
                        case "caramell":
                            // Мари восторженно прыгает и качает неоновыми ушками-антеннами под евробит!
                            float mSway = MathF.Sin(et * 11.4f);
                            float mHop = MathF.Abs(MathF.Cos(et * 11.4f));
                            mariPos = new Vector3(-0.56f + mSway * 0.18f, 0.64f + mHop * 0.18f, 0.26f);
                            mariRot = new Vector3(-8f + mHop * 10f, 18f - mSway * 20f, -mSway * 26f);
                            break;
                        case "rps_win":
                            // Победные круги радости вокруг Генерала!
                            mariPos = new Vector3(MathF.Cos(et * 7.5f) * 0.68f, 0.84f + MathF.Abs(MathF.Sin(et * 12f)) * 0.14f, MathF.Sin(et * 7.5f) * 0.68f);
                            mariRot = new Vector3(-10f, et * 360f, 12f);
                            break;
                        case "rps_lose":
                            // Грустно поникший дрон Мари
                            mariPos = new Vector3(-0.54f, 0.20f, 0.24f);
                            mariRot = new Vector3(32f, MathF.Sin(et * 10f) * 18f, 0f);
                            break;
                        default:
                            // Весёлый танец Мари «восьмёркой» рядом с плечом Генерала
                            float dBeat = MathF.Sin(et * 8.2f);
                            float dHop = MathF.Abs(MathF.Cos(et * 8.2f));
                            mariPos = new Vector3(-0.62f + dBeat * 0.22f, 0.58f + dHop * 0.16f, 0.22f + MathF.Cos(et * 4.1f) * 0.14f);
                            mariRot = new Vector3(MathF.Sin(et * 4.1f) * 10f, 16f + dBeat * 28f, -dBeat * 20f);
                            break;
                    }
                }
                else
                {
                    float hackPulse = (AnimTimer > 0f && ActiveAnim == "r") ? 0.45f : 0f;
                    mariPos = new Vector3(
                        -0.68f + MathF.Sin(t * 3.2f) * 0.12f,
                        0.56f + MathF.Cos(t * 4.4f) * 0.09f + hackPulse,
                        0.10f + MathF.Sin(t * 2.6f) * 0.08f);
                    mariRot = new Vector3(0f, MathF.Sin(t * 2.2f) * 18f, MathF.Cos(t * 3.5f) * 8f);
                }

                float mariBlend = MathF.Min(1f, dt * 14f);
                ShieldAura.Position = Lerp(ShieldAura.Position, mariPos, mariBlend);
                ShieldAura.Rotation = LerpEulerDeg(ShieldAura.Rotation, mariRot, mariBlend);
            }

            // Плавная интерполяция позиций и кратчайших дуг углов Эйлера (без обратного проворота после 360°)
            float blend = MathF.Min(1f, dt * (AnimTimer > 0f ? 20f : (!string.IsNullOrEmpty(ActiveEmote) ? 15f : 12f)));
            Torso.Position = Lerp(Torso.Position, targetTorsoPos, blend);
            Torso.Rotation = LerpEulerDeg(Torso.Rotation, targetTorsoRot, blend);
            LeftArm.Position = Lerp(LeftArm.Position, targetLArmPos, blend);
            RightArm.Position = Lerp(RightArm.Position, targetRArmPos, blend);
            LeftLeg.Position = Lerp(LeftLeg.Position, targetLLegPos, blend);
            RightLeg.Position = Lerp(RightLeg.Position, targetRLegPos, blend);
            LeftArm.Rotation = LerpEulerDeg(LeftArm.Rotation, targetLArmRot, blend);
            RightArm.Rotation = LerpEulerDeg(RightArm.Rotation, targetRArmRot, blend);
            LeftLeg.Rotation = LerpEulerDeg(LeftLeg.Rotation, targetLLegRot, blend);
            RightLeg.Rotation = LerpEulerDeg(RightLeg.Rotation, targetRLegRot, blend);

            // Вторичная анимация головы (дыхание, оглядывание по сторонам в покое, стабилизация взгляда и повороты при стрейфах)
            if (Head != null)
            {
                Vector3 targetHeadRot = BaseHeadRot;
                if (hasEmoteHead)
                {
                    targetHeadRot.X += emoteHeadX;
                    targetHeadRot.Y += emoteHeadY;
                    targetHeadRot.Z += emoteHeadZ;
                }
                else
                {
                    float idleLookY = !isMoving ? (MathF.Sin(t * 1.15f) * 9.5f + fOsc * 12f) : 0f;
                    float idleTiltZ = !isMoving ? (MathF.Cos(t * 1.6f) * 3.5f + (charKey is "cheburashka" or "ahri" or "gwen" ? fWave * 11f : fWave * 5f)) : 0f;
                    float idleNodX = !isMoving ? (fWave * (charKey is "dante" or "m4a1" or "buzz" ? 12f : -8f)) : 0f;
                    if (charKey is "v1" or "aigis" && !isMoving)
                        idleLookY = MathF.Round(MathF.Sin(t * 1.6f) * 2.0f) * 7.5f + fOsc * 14f; // Роботизированные повороты визора

                    targetHeadRot.X += MathF.Sin(t * 2.8f - 0.6f) * 3.2f + idleNodX - Math.Clamp((targetTorsoRot.X - BaseTorsoRot.X) * 0.35f, -14f, 14f);
                    targetHeadRot.Y += -Math.Clamp((targetTorsoRot.Y - BaseTorsoRot.Y) * 0.32f, -20f, 20f) + strafeInput * 8.5f + idleLookY;
                    targetHeadRot.Z += -strafeInput * 5.0f + idleTiltZ;
                }
                Head.Rotation = LerpEulerDeg(Head.Rotation, targetHeadRot, blend);
            }

            // Вторичная волновая анимация хвостов, косичек, плащей и больших ушей (Ари, Сларк, Гвен, Чебурашка, Сол, Генерал SANABI)
            if (Tails.Count > 0)
            {
                bool calmEmote = ActiveEmote is "chair" or "plastic_chair" or "vergil_chair";
                bool fastEurobeat = ActiveEmote is "caramelldansen" or "caramell";
                float skillFlare = AnimTimer > 0f
                    ? MathF.Sin((1f - AnimTimer / MathF.Max(0.01f, AnimDuration)) * MathF.PI)
                    : (!string.IsNullOrEmpty(ActiveEmote) ? (calmEmote ? 0.10f : 0.55f) : fWave * 0.75f);
                float waveSpd = fastEurobeat
                    ? 11.4f
                    : (calmEmote ? 2.6f : (isMoving || !string.IsNullOrEmpty(ActiveEmote) ? 6.4f : 3.2f) + skillFlare * 5.0f);
                float waveAmp = fastEurobeat
                    ? 18.0f
                    : (calmEmote ? 6.0f : (isMoving || !string.IsNullOrEmpty(ActiveEmote) ? 13.5f : 8.5f) + skillFlare * 9.5f);
                for (int i = 0; i < Tails.Count; i++)
                {
                    var tail = Tails[i];
                    if (tail == null || tail.IsDestroyed) continue;
                    Vector3 bRot = i < BaseTailRots.Count ? BaseTailRots[i] : Vector3.Zero;
                    float phase = t * waveSpd + i * 0.68f;
                    Vector3 tRot = new(
                        bRot.X + MathF.Sin(phase) * waveAmp + (isMoving ? -10f : 0f) - skillFlare * 14f,
                        bRot.Y + MathF.Cos(phase * 0.85f) * (waveAmp * 1.15f) - strafeInput * 10f,
                        bRot.Z + MathF.Sin(phase * 0.65f + 0.8f) * (waveAmp * 0.55f)
                    );
                    tail.Rotation = LerpEulerDeg(tail.Rotation, tRot, MathF.Min(1f, dt * 14f));
                }
            }

            UpdateExpansionPack1Anims(charKey, t, dt, isMoving, isGrounded, strafeInput, forwardInput);
            UpdateExpansionPack2Anims(charKey, t, dt, isMoving, isGrounded, strafeInput, forwardInput);
        }

        // Специализированная хореография звездолёта Ядра «Гамма» (Mindustry): полёт, стрельба и все эмоции Колеса [L]
        private void UpdateMindustryAnimations(float t, float dt, bool isMoving, float strafeInput, float forwardInput)
        {
            Vector3 targetShipPos = BaseTorsoPos;
            Vector3 targetShipRot = BaseTorsoRot;
            Vector3 targetLArmPos = BaseLeftArmPos;
            Vector3 targetRArmPos = BaseRightArmPos;
            Vector3 targetLArmRot = BaseLeftArmRot;
            Vector3 targetRArmRot = BaseRightArmRot;
            Vector3 localLLegOffset = Vector3.Zero;
            Vector3 localRLegOffset = Vector3.Zero;
            Vector3 localLLegRot = Vector3.Zero;
            Vector3 localRLegRot = Vector3.Zero;

            if (string.IsNullOrEmpty(ActiveEmote))
            {
                float hoverBob = MathF.Sin(t * 3.8f) * 0.08f;
                targetShipPos = new Vector3(BaseTorsoPos.X, BaseTorsoPos.Y + hoverBob, BaseTorsoPos.Z);
                targetShipRot = new Vector3(forwardInput * 12f, BaseTorsoRot.Y, -strafeInput * 16f);

                float enginePulse = isMoving ? MathF.Sin(t * 24f) * 8f : MathF.Sin(t * 6f) * 3f;
                localLLegRot = new Vector3(enginePulse, 0f, 0f);
                localRLegRot = new Vector3(-enginePulse, 0f, 0f);

                if (AnimTimer > 0f && !string.IsNullOrEmpty(ActiveAnim))
                {
                    float p = Math.Clamp(1f - AnimTimer / MathF.Max(0.01f, AnimDuration), 0f, 1f);
                    float pulse = MathF.Sin(p * MathF.PI);
                    if (ActiveAnim == "m1")
                    {
                        float kick = pulse * -0.15f;
                        if (AnimStep % 2 == 0) targetLArmPos.Z += kick;
                        else targetRArmPos.Z += kick;
                    }
                    else if (ActiveAnim == "m2")
                    {
                        targetLArmPos.Z -= 0.20f * pulse;
                        targetRArmPos.Z -= 0.20f * pulse;
                        targetShipRot.X -= 8f * pulse;
                    }
                    else if (ActiveAnim == "shift")
                    {
                        targetShipRot.Z += p * 360f;
                        targetShipRot.X += 14f * pulse;
                    }
                    else if (ActiveAnim == "r")
                    {
                        targetShipRot.X -= 16f * pulse;
                        targetLArmRot.Y = -18f * pulse;
                        targetRArmRot.Y = 18f * pulse;
                    }
                }
            }
            else
            {
                float et = EmoteTime;
                switch (ActiveEmote)
                {
                    case "chair":
                    case "plastic_chair":
                    case "vergil_chair":
                    {
                        // Кораблик «Гамма» снижается и гордо восседает на белом пластиковом стуле Вергилия, сложив пушки!
                        float purr = MathF.Sin(t * 4.2f);
                        targetShipPos = new Vector3(0f, 0.68f + MathF.Sin(t * 2.2f) * 0.016f, -0.08f);
                        targetShipRot = new Vector3(-10f, MathF.Sin(t * 0.75f) * 5f, MathF.Sin(t * 1.5f) * 1.5f);
                        targetLArmPos = new Vector3(-0.54f, 0.02f, 0.28f);
                        targetRArmPos = new Vector3(0.54f, 0.02f, 0.28f);
                        targetLArmRot = new Vector3(8f, 18f, 6f);
                        targetRArmRot = new Vector3(8f, -18f, -6f);
                        localLLegOffset = new Vector3(0.05f, 0.38f, 0.06f);
                        localRLegOffset = new Vector3(-0.05f, 0.38f, 0.06f);
                        localLLegRot = new Vector3(purr * 2.5f, 0f, 0f);
                        localRLegRot = new Vector3(-purr * 2.5f, 0f, 0f);
                        break;
                    }
                    case "caramelldansen":
                    case "caramell":
                    {
                        // Меха-ушки: передние лазерные пушки поднимаются вверх над рубкой и синхронно машут влево-вправо под евробит!
                        float beat = et * 11.4f;
                        float sway = MathF.Sin(beat);
                        float bounce = MathF.Abs(MathF.Cos(beat));
                        targetShipPos = BaseTorsoPos + new Vector3(-sway * 0.22f, -0.06f + bounce * 0.16f, 0f);
                        targetShipRot = new Vector3(-6f + bounce * 8f, sway * 16f, -sway * 22f);
                        targetLArmPos = new Vector3(-0.38f + sway * 0.08f, 0.28f, 0.16f);
                        targetRArmPos = new Vector3(0.38f + sway * 0.08f, 0.28f, 0.16f);
                        targetLArmRot = new Vector3(-64f + bounce * 14f, 12f + sway * 38f, 0f);
                        targetRArmRot = new Vector3(-64f + bounce * 14f, -12f + sway * 38f, 0f);
                        localLLegRot = new Vector3(12f + MathF.Max(0f, sway) * 26f, -sway * 12f, 0f);
                        localRLegRot = new Vector3(12f + MathF.Max(0f, -sway) * 26f, -sway * 12f, 0f);
                        break;
                    }
                    case "fortnite":
                    case "default_dance":
                    {
                        // 3-фазное аэробатическое шоу Fortnite: качание пушками -> бочка 360° с подскоком -> дерзкий прицел!
                        float cyc = et % 3.2f;
                        float step = MathF.Sin(et * 10.5f);
                        float hop = MathF.Abs(MathF.Cos(et * 10.5f));
                        if (cyc < 1.3f)
                        {
                            targetShipPos = BaseTorsoPos + new Vector3(step * 0.16f, hop * 0.14f, 0f);
                            targetShipRot = new Vector3(5f, step * 20f, -step * 22f);
                            targetLArmPos = BaseLeftArmPos + new Vector3(0f, 0f, MathF.Max(0f, step) * 0.18f);
                            targetRArmPos = BaseRightArmPos + new Vector3(0f, 0f, MathF.Max(0f, -step) * 0.18f);
                            targetLArmRot = new Vector3(-18f + step * 26f, 16f - step * 20f, 0f);
                            targetRArmRot = new Vector3(-18f - step * 26f, -16f - step * 20f, 0f);
                            localLLegRot = new Vector3(step * 22f, 0f, 0f);
                            localRLegRot = new Vector3(-step * 22f, 0f, 0f);
                        }
                        else if (cyc < 2.2f)
                        {
                            // Чёткая авиационная бочка 360° (Barrel Roll) со сведением орудий!
                            float sp = (cyc - 1.3f) / 0.9f;
                            float clap = MathF.Sin(sp * MathF.PI);
                            targetShipPos = BaseTorsoPos + new Vector3(0f, clap * 0.34f, 0f);
                            targetShipRot = new Vector3(-12f * clap, 0f, sp * 360f);
                            targetLArmRot = new Vector3(-48f * clap, 30f * clap, 0f);
                            targetRArmRot = new Vector3(-48f * clap, -30f * clap, 0f);
                        }
                        else
                        {
                            // Финальная фаза: гордый разворот и указание правой лазерной пушкой вперёд в такт биту!
                            float pt = (cyc - 2.2f) / 1.0f;
                            float groove = MathF.Sin(pt * MathF.PI * 2f);
                            targetShipPos = BaseTorsoPos + new Vector3(0f, MathF.Abs(groove) * 0.10f, 0f);
                            targetShipRot = new Vector3(-8f, -16f + groove * 8f, 14f + groove * 6f);
                            targetLArmPos = new Vector3(-0.52f, 0.04f, 0.34f);
                            targetLArmRot = new Vector3(6f, 38f, 0f);
                            targetRArmPos = BaseRightArmPos + new Vector3(-0.08f, 0.04f, 0.22f + MathF.Abs(groove) * 0.08f);
                            targetRArmRot = new Vector3(-6f + groove * 8f, -6f, 0f);
                        }
                        break;
                    }
                    case "california":
                    case "california_girls":
                    case "california_gurls":
                    {
                        // Плавный полёт «восьмёркой» с покачиванием крыльев и ритмичным вилянием сопел!
                        float beat = et * 6.8f;
                        float sway = MathF.Sin(beat);
                        float fig8 = MathF.Sin(beat * 2f);
                        bool handsUpPhase = (et % 3.6f) < 1.8f;
                        targetShipPos = BaseTorsoPos + new Vector3(sway * 0.28f, fig8 * 0.10f, MathF.Cos(beat) * 0.08f);
                        targetShipRot = new Vector3(fig8 * 7f, sway * 24f, -sway * 20f);
                        if (handsUpPhase)
                        {
                            targetLArmPos = new Vector3(-0.52f, 0.20f, 0.22f);
                            targetRArmPos = new Vector3(0.52f, 0.20f, 0.22f);
                            targetLArmRot = new Vector3(-48f, 22f + sway * 16f, 0f);
                            targetRArmRot = new Vector3(-48f, -22f + sway * 16f, 0f);
                        }
                        else
                        {
                            targetLArmRot = new Vector3(-8f + sway * 18f, 14f + sway * 16f, 0f);
                            targetRArmRot = new Vector3(-8f - sway * 18f, -14f + sway * 16f, 0f);
                        }
                        localLLegRot = new Vector3(sway * 18f, -sway * 26f, 0f);
                        localRLegRot = new Vector3(-sway * 18f, -sway * 26f, 0f);
                        break;
                    }
                    case "sun":
                    case "praise_the_sun":
                    case "praise_sun":
                    {
                        // Вертикальная свеча к Солнцу \[T]/: нос задран на -70°, пушки широко распахнуты, радар вращается на максимуме!
                        targetShipPos = BaseTorsoPos + new Vector3(0f, 0.22f + MathF.Sin(et * 2.4f) * 0.05f, 0f);
                        targetShipRot = new Vector3(-70f, 0f, 0f);
                        targetLArmPos = BaseLeftArmPos + new Vector3(-0.14f, 0.08f, 0.08f);
                        targetRArmPos = BaseRightArmPos + new Vector3(0.14f, 0.08f, 0.08f);
                        targetLArmRot = new Vector3(-12f, -34f, 20f);
                        targetRArmRot = new Vector3(-12f, 34f, -20f);
                        float sunPulse = MathF.Sin(et * 14f) * 5f;
                        localLLegRot = new Vector3(sunPulse, 0f, 0f);
                        localRLegRot = new Vector3(-sunPulse, 0f, 0f);
                        break;
                    }
                    case "kazotsky":
                    case "squat_dance":
                    case "prisyadka":
                    {
                        // Бреющее зависание у самой земли и поочерёдные залихватские «пинки» вперед соплами двигателей!
                        float kickSin = MathF.Sin(et * 9.6f);
                        float hop = MathF.Abs(MathF.Cos(et * 9.6f));
                        float lKick = MathF.Max(0f, kickSin);
                        float rKick = MathF.Max(0f, -kickSin);
                        bool heyFlourish = (et % 4.0f) >= 3.0f;

                        targetShipPos = new Vector3(kickSin * 0.09f, 0.74f + hop * 0.14f, -0.05f);
                        targetShipRot = new Vector3(-16f, kickSin * 14f, -kickSin * 12f);
                        if (heyFlourish)
                        {
                            targetLArmRot = new Vector3(-36f, -34f, 16f);
                            targetRArmRot = new Vector3(-36f, 34f, -16f);
                        }
                        else
                        {
                            targetLArmPos = new Vector3(-0.46f, 0.06f, 0.44f);
                            targetRArmPos = new Vector3(0.46f, 0.06f, 0.44f);
                            targetLArmRot = new Vector3(0f, 44f, 0f);
                            targetRArmRot = new Vector3(0f, -44f, 0f);
                        }
                        localLLegOffset = new Vector3(0f, 0.18f + lKick * 0.10f, 0.12f + lKick * 0.72f);
                        localRLegOffset = new Vector3(0f, 0.18f + rKick * 0.10f, 0.12f + rKick * 0.72f);
                        localLLegRot = new Vector3(-28f * lKick, 0f, 0f);
                        localRLegRot = new Vector3(-28f * rKick, 0f, 0f);
                        break;
                    }
                    case "gangnam":
                    case "gangnam_style":
                    {
                        // Воздушный галоп и раскрутка правой пушки как «лассо»!
                        float gallop = MathF.Abs(MathF.Sin(et * 8.6f));
                        float sideStep = MathF.Sin(et * 4.3f);
                        bool lassoPhase = (et % 3.2f) >= 1.6f;
                        targetShipPos = BaseTorsoPos + new Vector3(sideStep * 0.16f, -0.08f + gallop * 0.24f, 0f);
                        targetShipRot = new Vector3(10f - gallop * 22f, sideStep * 18f, -sideStep * 12f);
                        if (!lassoPhase)
                        {
                            targetLArmPos = new Vector3(-0.46f, 0.04f, 0.46f);
                            targetRArmPos = new Vector3(0.46f, 0.04f, 0.46f);
                            targetLArmRot = new Vector3(-12f - gallop * 22f, 34f, 0f);
                            targetRArmRot = new Vector3(-12f - gallop * 22f, -34f, 0f);
                        }
                        else
                        {
                            targetLArmPos = new Vector3(-0.50f, 0.04f, 0.40f);
                            targetLArmRot = new Vector3(0f, 28f, 0f);
                            targetRArmPos = new Vector3(0.44f, 0.28f, 0.24f);
                            targetRArmRot = new Vector3(-62f + MathF.Cos(et * 18f) * 28f, MathF.Sin(et * 18f) * 44f, 0f);
                        }
                        localLLegRot = new Vector3(-18f * gallop, 0f, 0f);
                        localRLegRot = new Vector3(-18f * gallop, 0f, 0f);
                        break;
                    }
                    case "jojo":
                    case "jojo_pose":
                    {
                        // Драматичный угрожающий вираж 42° с вибрацией реактора Ядра!
                        float rumble = MathF.Sin(et * 22f) * 1.4f;
                        targetShipPos = BaseTorsoPos + new Vector3(-0.08f, 0.06f, 0f);
                        targetShipRot = new Vector3(-24f, -28f + rumble, 42f);
                        targetLArmPos = new Vector3(-0.44f, 0.20f, 0.42f);
                        targetLArmRot = new Vector3(-28f, 38f, 18f);
                        targetRArmPos = BaseRightArmPos + new Vector3(0.10f, -0.06f, 0f);
                        targetRArmRot = new Vector3(16f, -32f, -24f);
                        localLLegRot = new Vector3(-16f, 0f, 0f);
                        localRLegRot = new Vector3(14f, 0f, 0f);
                        break;
                    }
                    case "distraction":
                    case "stickmin":
                    {
                        // Танец Отвлечения: «хлопок» сведением пушек перед носом -> резкий крен влево с роспуском крыльев -> хлопок -> крен вправо!
                        float cyc = et % 1.6f;
                        if (cyc < 0.40f || (cyc >= 0.80f && cyc < 1.20f))
                        {
                            float p = (cyc % 0.40f) / 0.40f;
                            float clap = MathF.Sin(p * MathF.PI);
                            targetShipPos = BaseTorsoPos + new Vector3(0f, clap * 0.14f, 0f);
                            targetShipRot = new Vector3(8f * clap, 0f, 0f);
                            targetLArmPos = new Vector3(-0.70f + 0.32f * clap, 0.08f, 0.44f);
                            targetRArmPos = new Vector3(0.70f - 0.32f * clap, 0.08f, 0.44f);
                            targetLArmRot = new Vector3(0f, 38f * clap, 0f);
                            targetRArmRot = new Vector3(0f, -38f * clap, 0f);
                        }
                        else
                        {
                            bool leanLeft = cyc < 0.80f;
                            float dir = leanLeft ? -1f : 1f;
                            float p = (leanLeft ? (cyc - 0.40f) : (cyc - 1.20f)) / 0.40f;
                            float pose = MathF.Pow(MathF.Max(0f, MathF.Sin(p * MathF.PI)), 0.65f);

                            targetShipPos = BaseTorsoPos + new Vector3(dir * 0.28f * pose, -0.10f * pose, 0f);
                            targetShipRot = new Vector3(6f * pose, dir * 22f * pose, -dir * 28f * pose);
                            targetLArmRot = new Vector3((leanLeft ? -34f : 14f) * pose, -38f * pose, 0f);
                            targetRArmRot = new Vector3((leanLeft ? 14f : -34f) * pose, 38f * pose, 0f);
                            localLLegRot = new Vector3(-dir * 20f * pose, 0f, 0f);
                            localRLegRot = new Vector3(dir * 20f * pose, 0f, 0f);
                        }
                        break;
                    }
                    case "rps_wait":
                    {
                        float tap = MathF.Abs(MathF.Sin(et * 5.8f));
                        targetShipPos = BaseTorsoPos + new Vector3(0f, tap * 0.06f, 0f);
                        targetShipRot = new Vector3(4f, MathF.Sin(et * 2.2f) * 16f, MathF.Sin(et * 2.2f) * 6f);
                        targetLArmPos = new Vector3(-0.46f, 0.04f, 0.42f);
                        targetLArmRot = new Vector3(0f, 24f, 0f);
                        targetRArmPos = new Vector3(0.56f - tap * 0.16f, 0.04f + tap * 0.10f, 0.42f);
                        targetRArmRot = new Vector3(-18f * tap, -28f, 0f);
                        break;
                    }
                    case "rps":
                    case "rps_count":
                    {
                        float pump = MathF.Abs(MathF.Sin(et * MathF.PI * 2.6f));
                        targetShipPos = BaseTorsoPos + new Vector3(0f, (1f - pump) * 0.10f, 0f);
                        targetShipRot = new Vector3(16f * pump - 6f, 0f, 0f);
                        targetLArmRot = new Vector3(0f, 22f, 0f);
                        targetRArmRot = new Vector3(-38f * pump, -18f, 0f);
                        break;
                    }
                    case "rps_win":
                    {
                        // Победные виражи и вертикальные прыжки на форсаже!
                        float jmp = MathF.Abs(MathF.Sin(et * 8.5f));
                        targetShipPos = BaseTorsoPos + new Vector3(0f, jmp * 0.32f, 0f);
                        targetShipRot = new Vector3(-18f, et * 360f, MathF.Sin(et * 8.5f) * 18f);
                        targetLArmRot = new Vector3(-52f + jmp * 14f, -18f, 0f);
                        targetRArmRot = new Vector3(-52f + jmp * 14f, 18f, 0f);
                        break;
                    }
                    case "rps_lose":
                    {
                        // Аварийное проседание тяги — кораблик грустно опускает нос и пушки к земле
                        targetShipPos = new Vector3(0f, 0.72f, 0f);
                        targetShipRot = new Vector3(32f, MathF.Sin(et * 10f) * 14f, 0f);
                        targetLArmRot = new Vector3(26f, 16f, 0f);
                        targetRArmRot = new Vector3(26f, -16f, 0f);
                        localLLegOffset = new Vector3(0f, 0.26f, 0f);
                        localRLegOffset = new Vector3(0f, 0.26f, 0f);
                        break;
                    }
                    case "rps_draw":
                    {
                        // Озадаченное покачивание с крыла на крыло
                        float shrug = MathF.Sin(et * 5f);
                        targetShipRot = new Vector3(-4f, shrug * 10f, shrug * 18f);
                        targetLArmRot = new Vector3(-10f, -38f, 0f);
                        targetRArmRot = new Vector3(-10f, 38f, 0f);
                        break;
                    }
                    default:
                    {
                        float waggle = MathF.Sin(et * 8.0f);
                        targetShipPos = BaseTorsoPos + new Vector3(waggle * 0.18f, MathF.Abs(MathF.Cos(et * 8.0f)) * 0.12f, 0f);
                        targetShipRot = new Vector3(0f, waggle * 16f, -waggle * 20f);
                        targetLArmRot = new Vector3(-24f + waggle * 18f, 0f, 0f);
                        targetRArmRot = new Vector3(-24f - waggle * 18f, 0f, 0f);
                        break;
                    }
                }
            }

            float shipBlend = MathF.Min(1f, dt * (!string.IsNullOrEmpty(ActiveEmote) ? 14f : 9f));
            float armBlend = MathF.Min(1f, dt * (!string.IsNullOrEmpty(ActiveEmote) ? 16f : 12f));
            Torso.Position = Lerp(Torso.Position, targetShipPos, shipBlend);
            Torso.Rotation = LerpEulerDeg(Torso.Rotation, targetShipRot, shipBlend);

            LeftArm.Position = Lerp(LeftArm.Position, targetLArmPos, armBlend);
            RightArm.Position = Lerp(RightArm.Position, targetRArmPos, armBlend);
            LeftArm.Rotation = LerpEulerDeg(LeftArm.Rotation, targetLArmRot, armBlend);
            RightArm.Rotation = LerpEulerDeg(RightArm.Rotation, targetRArmRot, armBlend);

            // Гондолы задних двигателей привязаны к root, поэтому трансформируем их синхронно с корпусом корабля
            const float deg2rad = MathF.PI / 180f;
            Matrix4x4 shipRotMat = Matrix4x4.CreateFromYawPitchRoll(
                Torso.Rotation.Y * deg2rad,
                Torso.Rotation.X * deg2rad,
                Torso.Rotation.Z * deg2rad);
            Vector3 targetLLegWorld = Torso.Position + Vector3.Transform((BaseLeftLegPos - BaseTorsoPos) + localLLegOffset, shipRotMat);
            Vector3 targetRLegWorld = Torso.Position + Vector3.Transform((BaseRightLegPos - BaseTorsoPos) + localRLegOffset, shipRotMat);
            float legBlend = MathF.Min(1f, dt * (!string.IsNullOrEmpty(ActiveEmote) ? 18f : 12f));
            LeftLeg.Position = Lerp(LeftLeg.Position, targetLLegWorld, legBlend);
            RightLeg.Position = Lerp(RightLeg.Position, targetRLegWorld, legBlend);
            LeftLeg.Rotation = LerpEulerDeg(LeftLeg.Rotation, Torso.Rotation + localLLegRot, legBlend);
            RightLeg.Rotation = LerpEulerDeg(RightLeg.Rotation, Torso.Rotation + localRLegRot, legBlend);
        }

        public void SetUltForm(bool active)
        {
            if (UltFormActive == active || UltFormNodes.Count == 0) return;
            UltFormActive = active;
            foreach (var n in NormalFormNodes)
                if (n != null && !n.IsDestroyed) n.Enabled = !active;
            foreach (var n in UltFormNodes)
                if (n != null && !n.IsDestroyed) n.Enabled = active;
        }
    }

    public static partial class SurvivorDatabase
    {
        public static readonly string[] OrderedKeys =
        {
            "commando", "artificer", "huntress", "buzz", "v1", "sol", "gwen",
            "enforcer", "scout", "m4a1", "medic", "aigis", "reimu", "mindustry",
            "slark", "tidehunter", "freeman", "dante", "cheburashka", "ahri",
            "lappland", "w_arknights", "gold_ship", "pushnoy", "freddy",
            "steve", "jotaro", "vyper", "haze", "dmc_trio", "sanabi", "jack_o",
            "susie", "spamton", "binah", "don_quixote", "priestess", "closure",
            "daitaku_helios", "robin", "bird_trio", "apocalypse_bird", "komeiji", "false_son"
        };

        public static List<string> GetVisibleRosterKeys()
        {
            var list = new List<string>(OrderedKeys.Length);
            foreach (var k in OrderedKeys)
            {
                if (Data.ProgressData.Instance.IsCharacterVisible(k))
                    list.Add(k);
            }
            return list;
        }

        public static bool IsMobaCharacter(string charKey)
            => charKey is "gwen" or "slark" or "tidehunter" or "ahri";

        public static readonly Dictionary<string, SurvivorConfig> Classes = new()
        {
            ["commando"] = new()
            {
                Key = "commando", Name = "КОММАНДО", Icon = "CMD", Subtitle = "Универсальный боец UES",
                Hp = 125, Speed = 10.4f, Damage = 13.5f, FireRate = 6.8f, ProjSpeed = 66f, ProjScale = 0.22f,
                BodyColor = Rgb(235, 130, 35), ArmorColor = Rgb(70, 78, 95), VisorColor = Rgb(60, 245, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Двойной выстрел", Cd = 0.14f, Desc = "[ЛКМ] Скорострельная стрельба из двух пистолетов." },
                    ["m2"] = new() { Name = "Фазовый снаряд", Cd = 3.0f, Desc = "[ПКМ] Мощный плазменный заряд (300% урона), прошивающий врагов насквозь." },
                    ["shift"] = new() { Name = "Тактический кувырок", Cd = 3.8f, Desc = "[Shift] Стремительный тактический рывок вперёд." },
                    ["r"] = new() { Name = "Подавляющий огонь", Cd = 7.5f, Desc = "[R] Шквальная очередь из 8 выстрелов повышенной точности." }
                }
            },
            ["artificer"] = new()
            {
                Key = "artificer", Name = "ИЗОБРЕТАТЕЛЬ", Icon = "ART", Subtitle = "Высокотехнологичный Архимаг",
                Hp = 115, Speed = 9.8f, Damage = 36.0f, FireRate = 2.15f, ProjSpeed = 46f, ProjScale = 0.52f, Aoe = 4.2f,
                BodyColor = Rgb(155, 55, 230), ArmorColor = Rgb(225, 195, 105), VisorColor = Rgb(255, 235, 65),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Плазменный разряд", Cd = 0.46f, Desc = "[ЛКМ] Взрывные плазменные сферы, наносящие урон по площади." },
                    ["m2"] = new() { Name = "Заряженная нано-бомба", Cd = 4.5f, Desc = "[ПКМ] Сверхтяжёлая грави-сфера (280% урона в огромном радиусе)." },
                    ["shift"] = new() { Name = "Морозный взлёт", Cd = 4.2f, Desc = "[Shift] Реактивный взлёт вверх с ледяным взрывом под ногами." },
                    ["r"] = new() { Name = "Орбитальный ионный удар", Cd = 9.5f, Desc = "[R] Обрушивает 5 орбитальных ионных лучей на ближайших врагов." }
                }
            },
            ["huntress"] = new()
            {
                Key = "huntress", Name = "ОХОТНИЦА", Icon = "HNT", Subtitle = "Стремительный Снайпер",
                Hp = 110, Speed = 11.4f, Damage = 20.5f, FireRate = 3.9f, ProjSpeed = 58f, ProjScale = 0.28f,
                BodyColor = Rgb(215, 45, 70), ArmorColor = Rgb(45, 50, 64), VisorColor = Rgb(255, 240, 90),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Стрельба в движении", Cd = 0.25f, Desc = "[ЛКМ] Выпускает энергетические стрелы с автонаведением." },
                    ["m2"] = new() { Name = "Лазерная глефа", Cd = 4.6f, Desc = "[ПКМ] Бросает лазерную глефу, рикошетящую до 5 раз между врагами!" },
                    ["shift"] = new() { Name = "Фазовый скачок", Cd = 3.4f, Desc = "[Shift] Мгновенная телепортация в направлении взгляда." },
                    ["r"] = new() { Name = "Дождь стрел", Cd = 8.8f, Desc = "[R] Прыжок в небо и обстрел зоны градом энергетических стрел." }
                }
            },
            ["buzz"] = new()
            {
                Key = "buzz", Name = "БАЗЗ ЛАЙТЕР", Icon = "BUZ", Subtitle = "Космический Рейнджер Звёздного Командования",
                Hp = 135, Speed = 10.3f, Damage = 17.2f, FireRate = 5.0f, ProjSpeed = 66f, ProjScale = 0.25f, Aoe = 1.2f,
                BodyColor = Rgb(125, 235, 65), ArmorColor = Rgb(240, 245, 250), VisorColor = Rgb(155, 65, 245),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Наручный лазер", Cd = 0.21f, Desc = "[ЛКМ] Скорострельный наручный лазер Звёздного Командования." },
                    ["m2"] = new() { Name = "Гипер-луч", Cd = 4.0f, Desc = "[ПКМ] Пробивной луч полной мощности с взрывным импульсом." },
                    ["shift"] = new() { Name = "Бесконечность — не предел!", Cd = 4.5f, Desc = "[Shift] Раскрывает крылья и взмывает в воздух на реактивном ранце." },
                    ["r"] = new() { Name = "Залп Звёздного Командования", Cd = 9.0f, Desc = "[R] Залп из 6 самонаводящихся фотонных ракет по врагам." }
                }
            },
            ["v1"] = new()
            {
                Key = "v1", Name = "V1 (ULTRAKILL)", Icon = "V1", Subtitle = "ВИД ОТ 1-ГО ЛИЦА | 5 ОРУЖИЙ [1-5] | 3 РУКИ [F/G/R] | СТИЛЬ",
                Hp = 100, Speed = 12.6f, Damage = 19.0f, FireRate = 5.2f, ProjSpeed = 85f, ProjScale = 0.22f,
                BodyColor = Rgb(45, 115, 235), ArmorColor = Rgb(32, 38, 52), VisorColor = Rgb(255, 225, 40),
                Skills = new()
                {
                    ["m1"] = new() { Name = "[1-5 / Колёсико] 5 Оружий", Cd = 0.15f, Desc = "1: Револьвер (Монетки) | 2: Дробовик (Ядро) | 3: Гвоздемёт | 4: Рельсотрон | 5: Ракетница." },
                    ["m2"] = new() { Name = "[F] Фидбекер и [G] Наклбластер", Cd = 0.45f, Desc = "[F] Синяя рука: Парирование, Ускорение снаряда и удар по монетке. [G] Красная рука." },
                    ["shift"] = new() { Name = "[Shift] 3x Рывок выносливости", Cd = 1.0f, Desc = "3 заряда рывка. Быстрое перемещение и уклонение от атак." },
                    ["r"] = new() { Name = "[R] Крюк-хлыст (Зелёная рука)", Cd = 2.2f, Desc = "Притягивает лёгких врагов к вам, а вас — к тяжёлым врагам и боссам. Кровь лечит HP!" }
                }
            },
            ["sol"] = new()
            {
                Key = "sol", Name = "СОЛ БЭДГАЙ", Icon = "SOL", Subtitle = "Пламя Порчи [GUNFLAME И BANDIT REVOLVER]",
                Hp = 160, Speed = 10.6f, Damage = 30.0f, FireRate = 2.6f, ProjSpeed = 42f, ProjScale = 0.48f, Aoe = 3.5f,
                BodyColor = Rgb(210, 35, 45), ArmorColor = Rgb(40, 35, 38), VisorColor = Rgb(255, 165, 30),
                Skills = new()
                {
                    ["m1"] = new() { Name = "GunFlame (Огненная волна)", Cd = 0.38f, Desc = "[ЛКМ] Вонзает меч в землю и пускает вперёд волну столпов пламени!" },
                    ["m2"] = new() { Name = "Bandit Revolver (Двойной пинок)", Cd = 4.0f, Desc = "[ПКМ] Рывок с пинком («BANDIT!»). Повторное нажатие ПКМ проводит второй удар («REVOLVER!»)!" },
                    ["shift"] = new() { Name = "Bandit Bringer (Пикирующий кулак)", Cd = 4.2f, Desc = "[Shift] Взмывает вверх по диагонали и обрушивается вниз с пылающим ударом кулака!" },
                    ["r"] = new() { Name = "ДРАКОНИЙ ИНСТАЛЛ", Cd = 11.0f, Desc = "[R] Пробуждение силы Гира: огненный взрыв + ускорение боя и регенерация на 6 сек." }
                }
            },
            ["gwen"] = new()
            {
                Key = "gwen", Name = "ГВЕН", Icon = "GWN", Subtitle = "Благая Швея [MOBA-РЕЖИМ | ВИД СВЕРХУ]",
                Hp = 135, Speed = 10.8f, Damage = 18.5f, FireRate = 4.8f, ProjSpeed = 52f, ProjScale = 0.30f, Aoe = 1.8f,
                BodyColor = Rgb(65, 195, 255), ArmorColor = Rgb(32, 28, 46), VisorColor = Rgb(195, 240, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Чик-чик! (4 удара -> 6 разрезов)", Cd = 0.21f, Desc = "[ЛКМ / Q] Каждый 4-й удар ножницами мгновенно проводит серию из 6 быстрых разрезов!" },
                    ["m2"] = new() { Name = "Точный разрез!", Cd = 3.5f, Desc = "[W / СКМ] Мощный смыкающий разрез гигантскими ножницами (320% урона + лечение)." },
                    ["shift"] = new() { Name = "Прыжок и Священный Туман", Cd = 5.2f, Desc = "[Shift] Рывок (+скорость атаки) и Туман: стрелки снаружи НЕ ВИДЯТ Гвен внутри тумана!" },
                    ["r"] = new() { Name = "Вышивание (1 -> 2 -> 3 иглы)", Cd = 9.0f, Desc = "[R] Три волны игл с интервалом 0.5с (1, затем 2, затем 3 иглы) с ростом урона и замедлением!" }
                }
            },
            ["enforcer"] = new()
            {
                Key = "enforcer", Name = "СИЛОВИК", Icon = "ENF", Subtitle = "Тяжёлый Бастион [ОТРАЖЕНИЕ И ПАРИРОВАНИЕ x3]",
                Hp = 185, Speed = 9.6f, Damage = 16.5f, FireRate = 1.85f, ProjSpeed = 64f, ProjScale = 0.20f,
                BodyColor = Rgb(55, 115, 195), ArmorColor = Rgb(38, 48, 68), VisorColor = Rgb(110, 225, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Штурмовой дробовик", Cd = 0.54f, Desc = "[ЛКМ] Мощный залп картечью из 6 пробивных дробин." },
                    ["m2"] = new() { Name = "Удар щитом", Cd = 4.0f, Desc = "[ПКМ] Оглушающий удар щитом перед собой (стан 2с и +20 щита)." },
                    ["shift"] = new() { Name = "Щит и Парирование (x3)", Cd = 1.0f, Desc = "[Shift] Щит отражает лобовые атаки. Выход из щита ПАРИРУЕТ ЛЮБУЮ атаку с возвратом урона x3!" },
                    ["r"] = new() { Name = "Слезоточивый газ", Cd = 9.5f, Desc = "[R] Бросает гранату со слезоточивым газом (замедляет врагов и бьёт по области)." }
                }
            },
            ["scout"] = new()
            {
                Key = "scout", Name = "РАЗВЕДЧИК (DRG)", Icon = "SCT", Subtitle = "СКАЛА И КАМЕНЬ! Разведчик Хоксеса IV",
                Hp = 120, Speed = 11.4f, Damage = 14.5f, FireRate = 6.8f, ProjSpeed = 72f, ProjScale = 0.22f,
                BodyColor = Rgb(35, 115, 215), ArmorColor = Rgb(55, 65, 82), VisorColor = Rgb(255, 210, 55),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Винтовка Deepcore GK2", Cd = 0.14f, Desc = "[ЛКМ] Скорострельная штурмовая винтовка с высокой точностью." },
                    ["m2"] = new() { Name = "Двустволка (Особый порох)", Cd = 3.6f, Desc = "[ПКМ] Дуплет из обреза + реактивный прыжок от отдачи в воздухе!" },
                    ["shift"] = new() { Name = "Крюк-кошка", Cd = 3.2f, Desc = "[Shift] Выстреливает крюком-кошкой на 45м и притягивает Разведчика на тросе." },
                    ["r"] = new() { Name = "Поле IFG и ракета", Cd = 9.0f, Desc = "[R] Поле IFG: замедляет врагов на 65% и бьёт током по зоне." }
                }
            },
            ["m4a1"] = new()
            {
                Key = "m4a1", Name = "M4A1 (GFL)", Icon = "M4", Subtitle = "Лидер отряда AR [АВТОНОМНЫЕ ИИ-КЛОНЫ]",
                Hp = 115, Speed = 10.4f, Damage = 15.0f, FireRate = 5.5f, ProjSpeed = 68f, ProjScale = 0.23f,
                BodyColor = Rgb(75, 95, 78), ArmorColor = Rgb(42, 46, 48), VisorColor = Rgb(95, 255, 135),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Штурмовой карабин M4A1", Cd = 0.18f, Desc = "[ЛКМ] Огонь из карабина. Клоны самостоятельно зачищают карту и собирают лут!" },
                    ["m2"] = new() { Name = "Пушка «Юпитер» (MOD3)", Cd = 4.5f, Desc = "[ПКМ] Пробивной энерго-выстрел рельсовой пушки с взрывом (340% ур.)." },
                    ["shift"] = new() { Name = "Тактическое уклонение", Cd = 3.8f, Desc = "[Shift] Тактический рывок с получением +25 щита и залпом клонов." },
                    ["r"] = new() { Name = "Автономные клоны (До 4 шт.)", Cd = 9.5f, Desc = "[R] Спавнит ИИ-клона: они сами бегают по карте, открывают сундуки, берут лут и юзают скиллы!" }
                }
            },
            ["medic"] = new()
            {
                Key = "medic", Name = "МЕДИК (TF2)", Icon = "MED", Subtitle = "ВИД ОТ 1-ГО ЛИЦА | НАСМЕШКА [G] | УБЕР-ЗАРЯД",
                Hp = 130, Speed = 10.2f, Damage = 14.0f, FireRate = 6.2f, ProjSpeed = 64f, ProjScale = 0.22f,
                BodyColor = Rgb(235, 238, 245), ArmorColor = Rgb(205, 38, 45), VisorColor = Rgb(255, 75, 75),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Шприцемёт (От 1-го лица)", Cd = 0.16f, Desc = "[ЛКМ] Наносит урон врагам и лечит союзников. [G] — Насмешка «Oktoberfest!» с видом от 3-го лица!" },
                    ["m2"] = new() { Name = "Лечебный луч Крицкрига", Cd = 3.4f, Desc = "[ПКМ] Лечит и усиливает союзника (+барьер) или выкачивает HP из врага." },
                    ["shift"] = new() { Name = "Целебный рывок Ампутатора", Cd = 4.2f, Desc = "[Shift] Рывок с ударом убер-пилой и целебным импульсом (+25% HP)." },
                    ["r"] = new() { Name = "Убер-заряд / Контроль разума", Cd = 10.5f, Desc = "[R] На врага (<50% HP) — ПЕРЕМАНИВАЕТ его! Иначе — УБЕР-ЗАРЯД на 6.5с!" }
                }
            },
            ["aigis"] = new()
            {
                Key = "aigis", Name = "АЙГИС (PERSONA 3)", Icon = "AIG", Subtitle = "Оружие против Теней [ОРГИЯ И ПАЛЛАДИОН]",
                Hp = 135, Speed = 10.5f, Damage = 12.0f, FireRate = 7.6f, ProjSpeed = 76f, ProjScale = 0.20f,
                BodyColor = Rgb(240, 242, 250), ArmorColor = Rgb(235, 195, 55), VisorColor = Rgb(65, 195, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Пальцевые пулемёты «Вулкан»", Cd = 0.13f, Desc = "[ЛКМ] Ураганный пулемётный огонь из пальцев-стволов правой руки." },
                    ["m2"] = new() { Name = "Гранатомёт «Пандора»", Cd = 4.0f, Desc = "[ПКМ] Тяжёлый фугасный залп из гранатомёта (300% урона, AoE 5.2м)." },
                    ["shift"] = new() { Name = "Режим перегрузки «Оргия»", Cd = 6.2f, Desc = "[Shift] Взлёт и режим Оргии на 5.5 сек (+50% атаки, +45% скорости, +35% урона)." },
                    ["r"] = new() { Name = "Персона: Удар Палладиона", Cd = 10.0f, Desc = "[R] Обрушивает копья света на врагов вокруг и даёт +45 щита." }
                }
            },
            ["reimu"] = new()
            {
                Key = "reimu", Name = "ХАКУРЕЙ РЕЙМУ", Icon = "RMU", Subtitle = "Жрица Хакурей [ПОЛЁТ НА ЗАЖАТИЕ ПРОБЕЛА]",
                Hp = 115, Speed = 11.2f, Damage = 18.0f, FireRate = 4.8f, ProjSpeed = 58f, ProjScale = 0.26f, Aoe = 1.2f,
                BodyColor = Rgb(225, 42, 52), ArmorColor = Rgb(245, 245, 250), VisorColor = Rgb(255, 230, 80),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Амулеты Офуда и Полёт", Cd = 0.21f, Desc = "[ЛКМ] Парные амулеты. [Прыжок + Зажать Space] Свободный полёт (сбивается при получении урона)!" },
                    ["m2"] = new() { Name = "Сфера Инь-Ян", Cd = 4.0f, Desc = "[ПКМ] Сфера Инь-Ян: рикошетит до 6 раз между врагами с AoE!" },
                    ["shift"] = new() { Name = "Парение Природы Фантазии", Cd = 3.8f, Desc = "[Shift] Воздушный рывок (+щит и выпуск 4 защитных талисманов)." },
                    ["r"] = new() { Name = "Спелл-карта: Печать Фантазии", Cd = 9.5f, Desc = "[R] Град из 10 радужных сфер Данмаку с самонаведением!" }
                }
            },
            ["mindustry"] = new()
            {
                Key = "mindustry", Name = "КОРАБЛЬ ЯДРА «ГАММА»", Icon = "MND", Subtitle = "ВИД СВЕРХУ | 2 РЕЖИМА: СТРОИТЕЛЬСТВО [B/Shift] И БОЙ",
                Hp = 160, Speed = 11.8f, Damage = 16.5f, FireRate = 3.8f, ProjSpeed = 60f, ProjScale = 0.20f, Aoe = 0f,
                BodyColor = Rgb(245, 185, 45), ArmorColor = Rgb(62, 68, 82), VisorColor = Rgb(255, 235, 95),
                Skills = new()
                {
                    ["m1"] = new() { Name = "ЛКМ: Построить чертёж / Огонь", Cd = 0.30f, Desc = "[ЛКМ] В режиме строительства возводит выбранное строение (или стреляет, если выбор сброшен). В боевом режиме — стреляет." },
                    ["m2"] = new() { Name = "ПКМ: Сбросить выбор чертежа", Cd = 0.15f, Desc = "[ПКМ / 1-8] Меню справа снизу: Буры, Плавильщики, Переработчики, Пилон, Ядро и 3 Турели (Медь, Кремний, Торий)." },
                    ["shift"] = new() { Name = "[Shift / B] Смена Режима (Стройка / Бой)", Cd = 0.8f, Desc = "В Стройке корабль парит высоко и НЕУЯЗВИМ. В Боевом режиме — уязвим, стреляет и открывает сундуки/портал на [E]!" },
                    ["r"] = new() { Name = "Главное Ядро и Перегрузка", Cd = 8.0f, Desc = "[R] Ускоряет работу всех буров, заводов и турелей. Берегите Главное Ядро в PvP!" }
                }
            },
            ["slark"] = new()
            {
                Key = "slark", Name = "СЛАРК (DOTA 2)", Icon = "SLK", Subtitle = "Ночной Ужас Тёмного Рифа [MOBA-РЕЖИМ | ВИД СВЕРХУ]",
                Hp = 125, Speed = 11.6f, Damage = 17.5f, FireRate = 5.0f, ProjSpeed = 55f, ProjScale = 0.28f, Aoe = 1.6f,
                BodyColor = Rgb(45, 135, 165), ArmorColor = Rgb(28, 42, 62), VisorColor = Rgb(110, 255, 215),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Кража Сущности (Essence Shift)", Cd = 0.20f, Desc = "[ЛКМ / Q / ПКМ] Удары заточкой крадут сущность врагов (+урон за стак и вампиризм)." },
                    ["m2"] = new() { Name = "Тёмный Пакт (Dark Pact)", Cd = 4.0f, Desc = "[W / СКМ] Взрыв волн тёмной воды вокруг Сларка, снимающий дебаффы и наносящий 320% урона." },
                    ["shift"] = new() { Name = "Прыжок хищника (Pounce)", Cd = 4.2f, Desc = "[Shift] Стремительный прыжок к курсору, оглушающий и привязывающий врагов на пути." },
                    ["r"] = new() { Name = "Танец Теней (Shadow Dance)", Cd = 11.0f, Desc = "[R] Облако чёрного дыма на 5.5с: неуязвимость к урону, +45% скорости и мощное лечение!" }
                }
            },
            ["tidehunter"] = new()
            {
                Key = "tidehunter", Name = "ТАЙДХАНТЕР (DOTA 2)", Icon = "TID", Subtitle = "Левиафан Бездны [MOBA-РЕЖИМ | ВИД СВЕРХУ]",
                Hp = 190, Speed = 9.4f, Damage = 26.0f, FireRate = 2.4f, ProjSpeed = 48f, ProjScale = 0.55f, Aoe = 3.8f,
                BodyColor = Rgb(62, 155, 115), ArmorColor = Rgb(135, 78, 52), VisorColor = Rgb(110, 245, 200),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Удар Якорем (Anchor Smash)", Cd = 0.42f, Desc = "[ЛКМ / Q / ПКМ] Круговой сокрушительный взмах гигантским морским якорем с замедлением." },
                    ["m2"] = new() { Name = "Океанский Поток (Gush)", Cd = 3.8f, Desc = "[W / СКМ] Струя бездны в сторону курсора: 280% урона, замедление и раскол брони." },
                    ["shift"] = new() { Name = "Панцирь Кракена (Kraken Shell)", Cd = 4.5f, Desc = "[Shift] Таранный рывок вперёд, дающий +40 щита и сбивающий врагов с ног." },
                    ["r"] = new() { Name = "Опустошение (Ravage)", Cd = 12.0f, Desc = "[R] Шипы и щупальца бездны подбрасывают и оглушают ВСЕХ врагов в радиусе 18 метров!" }
                }
            },
            ["freeman"] = new()
            {
                Key = "freeman", Name = "ГОРДОН ФРИМАН", Icon = "hl2", Subtitle = "ВИД ОТ 1-ГО ЛИЦА | ГРАВИ-ПУШКА И ТАУ-ПУШКА",
                Hp = 140, Speed = 10.6f, Damage = 16.5f, FireRate = 5.6f, ProjSpeed = 74f, ProjScale = 0.24f, Aoe = 1.4f,
                BodyColor = Rgb(235, 125, 30), ArmorColor = Rgb(48, 54, 62), VisorColor = Rgb(255, 185, 45),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Монтировка и SMG (От 1-го лица)", Cd = 0.18f, Desc = "[ЛКМ] Огонь из автомата и сокрушительные удары монтировкой вблизи." },
                    ["m2"] = new() { Name = "Грави-пушка (Захват и Бросок)", Cd = 0.8f, Desc = "[ПКМ] Притягивает сундуки, кристаллы и камни с карты; повторное нажатие швыряет их во врагов!" },
                    ["shift"] = new() { Name = "Модуль длинных прыжков HEV", Cd = 3.8f, Desc = "[Shift] Длинный реактивный прыжок костюма HEV Mark IV и зарядка +25 щита." },
                    ["r"] = new() { Name = "Тау-пушка (Заряд при удержании R)", Cd = 5.0f, Desc = "[Удерживать R] Накапливает заряд Гаусса для колоссального выстрела и реактивного прыжка от отдачи!" }
                }
            },
            ["dante"] = new()
            {
                Key = "dante", Name = "ДАНТЕ (LIMBUS)", Icon = "DNT", Subtitle = "Менеджер №10 [БАФФЫ ОТРЯДА И ЖЕРТВЕННОЕ ВОЗРОЖДЕНИЕ]",
                Hp = 135, Speed = 10.5f, Damage = 18.0f, FireRate = 4.6f, ProjSpeed = 66f, ProjScale = 0.28f, Aoe = 2.0f,
                BodyColor = Rgb(185, 32, 42), ArmorColor = Rgb(28, 26, 34), VisorColor = Rgb(255, 205, 55),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Приказ: Цепи Грешников", Cd = 0.22f, Desc = "[ЛКМ] Единственная прямая атака Данте: пылающие золотые цепи, прошивающие врагов." },
                    ["m2"] = new() { Name = "Резонанс Ветви (Бафф Атаки)", Cd = 6.0f, Desc = "[ПКМ] Усиливает всех союзников и игроков (+45% урона и +35% скорости атаки на 7 сек)." },
                    ["shift"] = new() { Name = "Защитный Протокол (Бафф Щита)", Cd = 5.5f, Desc = "[Shift] Исцеляет отряд, даёт +45 щита и ускоряет передвижение всех союзников." },
                    ["r"] = new() { Name = "Поворот Часов Вспять (Жертва)", Cd = 12.0f, Desc = "[R] ВОЗРОЖДАЕТ всех погибших игроков и лечит союзников ЦЕНОЙ ЖИЗНИ самого Данте!" }
                }
            },
            ["cheburashka"] = new()
            {
                Key = "cheburashka", Name = "ЧЕБУРАШКА", Icon = "ЧБР", Subtitle = "Ушастый Друг [ПАДЕНИЕ С ВЫСОТЫ | СТЕНД: КРОКОДИЛ ГЕНА]",
                Hp = 130, Speed = 10.6f, Damage = 18.0f, FireRate = 4.2f, ProjSpeed = 48f, ProjScale = 0.38f, Aoe = 2.4f,
                BodyColor = Rgb(115, 68, 38), ArmorColor = Rgb(225, 185, 135), VisorColor = Rgb(255, 150, 25),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Удар ушами / Кулаки Гены", Cd = 0.24f, Desc = "[ЛКМ] Хлопок гигантскими ушами (со стендом — серия сокрушительных ударов и укусов Гены) + Пассивка: Удар при падении с высоты!" },
                    ["m2"] = new() { Name = "Взрывной апельсин / Гармошка Гены", Cd = 3.2f, Desc = "[ПКМ] Бросает апельсин, разрывающийся на 8 долек-шрапнелин (со стендом — звуковая волна гармошки и залп апельсинов)!" },
                    ["shift"] = new() { Name = "Бросок Шапокляк / Рывок Стенда", Cd = 4.2f, Desc = "[Shift] Шапокляк швыряет Чебурашку далеко вперёд и вверх для мощного приземления!" },
                    ["r"] = new() { Name = "СТЕНД: «КРОКОДИЛ ГЕНА» (30 сек.)", Cd = 60.0f, Desc = "[R] На 30 сек. призывает стенд Крокодила Гену, заменяя все атаки на сокрушительные комбо Гены!" }
                }
            },
            ["ahri"] = new()
            {
                Key = "ahri", Name = "АРИ (LEAGUE OF LEGENDS)", Icon = "AHR", Subtitle = "Девятихвостая Лиса [MOBA-РЕЖИМ | ОБМАН, ОГОНЬ И ОЧАРОВАНИЕ]",
                Hp = 120, Speed = 11.4f, Damage = 21.0f, FireRate = 3.6f, ProjSpeed = 56f, ProjScale = 0.36f, Aoe = 2.4f,
                BodyColor = Rgb(225, 42, 62), ArmorColor = Rgb(245, 245, 252), VisorColor = Rgb(95, 215, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Сфера Обмана (Orb of Deception)", Cd = 0.28f, Desc = "[ЛКМ / Q] Сфера летит вперёд (маг. урон) и возвращается к Ари (чистый урон x1.35 + Кража Сущности)!" },
                    ["m2"] = new() { Name = "Лисий Огонь (Fox-Fire)", Cd = 3.6f, Desc = "[W / СКМ] Призывает 3 вращающихся духовных огонька (+40% скорости) с самонаведением во врагов." },
                    ["shift"] = new() { Name = "Призрачный Рывок (3 Заряда)", Cd = 4.5f, Desc = "[Shift] Стремительный рывок к курсору (до 3 зарядов), выпускающий 3 духовных разряда в ближайших врагов!" },
                    ["r"] = new() { Name = "Очарование и Всплеск Души", Cd = 9.0f, Desc = "[R] Посылает воздушный поцелуй (Очарование на 3.5с, +25% урона по цели) и взрыв духовной энергии!" }
                }
            },
            ["lappland"] = new()
            {
                Key = "lappland", Name = "ЛАППЛАНД (ARKNIGHTS)", Icon = "LAP", Subtitle = "Белая Волчица Сиракуз [3-й скин: ЛАППЛАНД ДЕКАДЕНЦА]",
                Hp = 135, Speed = 11.4f, Damage = 22.0f, FireRate = 4.2f, ProjSpeed = 62f, ProjScale = 0.30f, Aoe = 1.8f,
                BodyColor = Rgb(235, 238, 246), ArmorColor = Rgb(34, 36, 46), VisorColor = Rgb(225, 45, 70),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Парные Клинки Сиракуз", Cd = 0.24f, Desc = "[ЛКМ] Рубящие взмахи парными мечами + дальние серебряные полумесяцы искусств (накладывают Безмолвие)." },
                    ["m2"] = new() { Name = "Солнечные Часы (Sundial)", Cd = 3.5f, Desc = "[ПКМ] Блок-парирование (+35 щита) и мгновенный двойной контрудар на 260% магического урона!" },
                    ["shift"] = new() { Name = "Волчий Выпад (Lupine Rush)", Cd = 3.6f, Desc = "[Shift] Стремительный рывок сквозь врагов с широким кроваво-серебряным разрезом." },
                    ["r"] = new() { Name = "Дух Волчицы (Wolf Spirit)", Cd = 9.0f, Desc = "[R] Призывает серебряные призрачные мечи вокруг Лаппланд и выпускает шквал диагональных волн!" }
                }
            },
            ["w_arknights"] = new()
            {
                Key = "w_arknights", Name = "W / ВИШАДЕЛЬ (ARKNIGHTS)", Icon = "W", Subtitle = "Подрывница Каздела [3-й скин: ВИШАДЕЛЬ]",
                Hp = 120, Speed = 10.6f, Damage = 28.0f, FireRate = 2.6f, ProjSpeed = 54f, ProjScale = 0.38f, Aoe = 3.8f,
                BodyColor = Rgb(42, 40, 48), ArmorColor = Rgb(225, 42, 58), VisorColor = Rgb(255, 195, 55),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Гранатомёт Сарказа", Cd = 0.38f, Desc = "[ЛКМ] Выпускает разрывные осколочные гранаты с большим радиусом поражения." },
                    ["m2"] = new() { Name = "Валет Червей (Мина-ловушка)", Cd = 3.8f, Desc = "[ПКМ] Бросает клейкую мину C4, которая детонирует при приближении врагов и оглушает их!" },
                    ["shift"] = new() { Name = "Взрывной Прыжок", Cd = 4.0f, Desc = "[Shift] Отпрыгивает на взрывной волне (+30 щита), оставляя под ногами мину." },
                    ["r"] = new() { Name = "D12 (Дистанционный Детонатор)", Cd = 9.5f, Desc = "[R] Нажимает на детонатор, прикрепляя бомбы C4 ко всем врагам вокруг с цепным взрывом и оглушением!" }
                }
            },
            ["gold_ship"] = new()
            {
                Key = "gold_ship", Name = "ГОЛД ШИП (UMAMUSUME)", Icon = "GLD", Subtitle = "Непотопляемый Золотой Корабль [УРОН ОТ СКОРОСТИ БЕГА!]",
                Hp = 150, Speed = 12.2f, Damage = 18.5f, FireRate = 4.1f, ProjSpeed = 60f, ProjScale = 0.32f, Aoe = 2.2f,
                BodyColor = Rgb(225, 42, 55), ArmorColor = Rgb(242, 240, 248), VisorColor = Rgb(255, 215, 55),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Удар на Скаку (Gallop Strike)", Cd = 0.22f, Desc = "[ЛКМ] Удары на ходу и плечевой таран: чем выше текущая скорость бега и разгон, тем выше урон и радиус волны!" },
                    ["m2"] = new() { Name = "Фирменный Дропкик Голд Шип!", Cd = 3.4f, Desc = "[ПКМ] Легендарный прыжок двумя ногами вперёд с разбега! Множитель урона и отбрасывания растёт от скорости!" },
                    ["shift"] = new() { Name = "Финишный Рывок с Конца (Last Spurt)", Cd = 4.2f, Desc = "[Shift] Включает турбо-галоп (+65% к скорости бега на 4.5 сек., сбивает врагов с ног при беге)!" },
                    ["r"] = new() { Name = "Бросок Золотого Якоря (Warning Shot!)", Cd = 9.0f, Desc = "[R] Раскручивает и обрушивает гигантский золотой корабельный якорь (урон масштабируется от скорости разгона)!" }
                }
            },
            ["pushnoy"] = new()
            {
                Key = "pushnoy", Name = "АЛЕКСАНДР ПУШНОЙ", Icon = "GAL", Subtitle = "Ведущий «Галилео» и Рок-Музыкант [ЭКСПЕРИМЕНТЫ + МЕТАЛ!]",
                Hp = 135, Speed = 10.8f, Damage = 23.0f, FireRate = 3.8f, ProjSpeed = 58f, ProjScale = 0.34f, Aoe = 2.6f,
                BodyColor = Rgb(245, 145, 32), ArmorColor = Rgb(42, 46, 58), VisorColor = Rgb(95, 235, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Тяжёлый Рифф «Галилео!»", Cd = 0.26f, Desc = "[ЛКМ] Мощные метал-аккорды на электрогитаре Гибсон, пускающие пробивные звуковые волны и молнии!" },
                    ["m2"] = new() { Name = "А сейчас — ЭКСПЕРИМЕНТ!", Cd = 3.4f, Desc = "[ПКМ] Бросает колбу с научным опытом (чередует: Жидкий Азот -> Термитная смесь -> Взрыв с ментосом)!" },
                    ["shift"] = new() { Name = "Кресло на Огнетушителе!", Cd = 3.8f, Desc = "[Shift] Реактивный рывок верхом на офисном кресле с огнетушителем, оставляющий морозный пенный след!" },
                    ["r"] = new() { Name = "Катушка Теслы + МЕГА-РОК СОЛО", Cd = 9.5f, Desc = "[R] Ставит генератор молний Теслы и исполняет убойное рок-соло, поражая всех врагов вокруг током!" }
                }
            },
            ["freddy"] = new()
            {
                Key = "freddy", Name = "МИШКА ФРЕДДИ (FNAF)", Icon = "FNF", Subtitle = "Главный Аниматроник Fazbear's Pizza [ТОРЕАДОР И СКРИМЕР]",
                Hp = 170, Speed = 10.0f, Damage = 25.0f, FireRate = 3.2f, ProjSpeed = 56f, ProjScale = 0.34f, Aoe = 2.2f,
                BodyColor = Rgb(145, 88, 42), ArmorColor = Rgb(32, 32, 38), VisorColor = Rgb(85, 225, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Бросок Микрофона (Mic Toss)", Cd = 0.30f, Desc = "[ЛКМ] Бросает тяжёлый концертный микрофон (возвращается бумерангом) и бьёт лапой вблизи." },
                    ["m2"] = new() { Name = "СКРИМЕР (Укус 87-го!)", Cd = 3.8f, Desc = "[ПКМ] Резкий рывок к жертве с оглушающим скримером Фредди, наносящий 320% урона и восстанавливающий HP!" },
                    ["shift"] = new() { Name = "0% Энергии — Марш Тореадора", Cd = 5.0f, Desc = "[Shift] Выключает свет на 3.5 сек.: Фредди становится неуязвимым, играет шкатулку Тореадора, а его глаза светятся!" },
                    ["r"] = new() { Name = "Ночная Смена (Шоу Fazbear Band)", Cd = 10.0f, Desc = "[R] Призывает эндоскелетов-аниматроников на помощь и обрушивает пицца-фейерверк по всей арене!" }
                }
            },
            ["steve"] = new()
            {
                Key = "steve", Name = "СТИВ (MINECRAFT)", Icon = "MC", Subtitle = "Шахтёр и Крафтер [ЛОМАЕТ ПОЛ/СТЕНЫ/АЛТАРИ + ВЕРСТАК]",
                Hp = 150, Speed = 10.6f, Damage = 21.0f, FireRate = 3.8f, ProjSpeed = 60f, ProjScale = 0.30f, Aoe = 1.6f,
                BodyColor = Rgb(45, 175, 195), ArmorColor = Rgb(62, 75, 165), VisorColor = Rgb(85, 245, 235),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Алмазный Меч и Лук", Cd = 0.26f, Desc = "[ЛКМ] Рубящий удар мечом с отбрасыванием вблизи + выстрел стрелой из лука на дистанции." },
                    ["m2"] = new() { Name = "Кирка: КОПАТЬ КАРТУ (Пол / Стены / Алтари)", Cd = 0.85f, Desc = "[ПКМ] Ломает кусок пола (создавая реальную 3D-яму!), стены, скалы, деревья и алтари, добывая Дерево, Камень, Железо и Алмазы!" },
                    ["shift"] = new() { Name = "Жемчуг Края (Ender Pearl)", Cd = 3.8f, Desc = "[Shift] Бросает Эндер-жемчуг для мгновенной телепортации и получает Золотые сердца щита!" },
                    ["r"] = new() { Name = "Поставить Верстак / ТНТ", Cd = 6.0f, Desc = "[R] Ставит Верстак перед собой (также спавнится у портала после босса!) и бросает динамит ТНТ, пробивающий пол!" }
                }
            },
            ["jotaro"] = new()
            {
                Key = "jotaro", Name = "ДЖОТАРО КУДЖО (JOJO)", Icon = "JOJ", Subtitle = "Обладатель Стенда Star Platinum [ORA ORA + ОСТАНОВКА ВРЕМЕНИ!]",
                Hp = 155, Speed = 10.6f, Damage = 20.5f, FireRate = 4.8f, ProjSpeed = 66f, ProjScale = 0.32f, Aoe = 2.0f,
                BodyColor = Rgb(34, 38, 56), ArmorColor = Rgb(165, 95, 245), VisorColor = Rgb(255, 215, 65),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Шквал Ударов «ORA ORA ORA!»", Cd = 0.20f, Desc = "[ЛКМ] Star Platinum наносит ураганную серию кулачных ударов перед Джотаро с высокой скоростью!" },
                    ["m2"] = new() { Name = "Звёздный Палец (Star Finger!)", Cd = 3.8f, Desc = "[ПКМ] Удлиняет пальцы Star Platinum в дальнобойный пробивной луч (320% урона + оглушение по линии)!" },
                    ["shift"] = new() { Name = "Прыжок Стенда (Stand Leap)", Cd = 4.2f, Desc = "[Shift] Мощный рывок на силе Star Platinum с отражением снарядов и +40 щита." },
                    ["r"] = new() { Name = "STAR PLATINUM: THE WORLD! (Остановка Времени)", Cd = 13.5f, Desc = "[R] «ZA WARUDO!» — полностью ОСТАНАВЛИВАЕТ ВРЕМЯ для всех врагов и боссов на 4.0 секунды!" }
                }
            },
            ["vyper"] = new()
            {
                Key = "vyper", Name = "ВАЙПЕР (DEADLOCK)", Icon = "VYP", Subtitle = "Скользящая Рептилия-Преступница [СКОЛЬЖЕНИЕ И ОКАМЕНЕНИЕ]",
                Hp = 125, Speed = 11.8f, Damage = 13.0f, FireRate = 7.0f, ProjSpeed = 66f, ProjScale = 0.22f, Aoe = 1.4f,
                BodyColor = Rgb(65, 185, 85), ArmorColor = Rgb(185, 145, 75), VisorColor = Rgb(165, 255, 75),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Гадючий ПП (Огонь в Скольжении)", Cd = 0.14f, Desc = "[ЛКМ] Ураганный огонь из ПП. Во время движения и скольжения не тратит боезапас и стреляет на 35% быстрее!" },
                    ["m2"] = new() { Name = "Смертельный Яд (Lethal Venom)", Cd = 3.8f, Desc = "[ПКМ] Отравляющий дротик: наносит взрывной урон токсином тем сильнее, чем меньше HP осталось у цели!" },
                    ["shift"] = new() { Name = "Бронебойное Скольжение (Slither)", Cd = 3.6f, Desc = "[Shift] Стремительный низкий слайд рептилии (+45 щита, +50% скорости и бонус к скорострельности)!" },
                    ["r"] = new() { Name = "Окаменяющая Бола (Petrifying Bola)", Cd = 10.0f, Desc = "[R] Бросает проклятую болу, превращающую всех врагов в радиусе взрыва в камень на 3.5 сек.!" }
                }
            },
            ["haze"] = new()
            {
                Key = "haze", Name = "ХЕЙЗ (DEADLOCK)", Icon = "HAZ", Subtitle = "Оперативник Снов OSIC [ФИКСАЦИЯ, КИНЖАЛ СНА И ТАНЕЦ ПУЛЬ]",
                Hp = 115, Speed = 11.4f, Damage = 13.5f, FireRate = 7.2f, ProjSpeed = 68f, ProjScale = 0.20f, Aoe = 1.2f,
                BodyColor = Rgb(42, 44, 62), ArmorColor = Rgb(215, 145, 55), VisorColor = Rgb(255, 195, 75),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Парные ПП + Фиксация (Fixation)", Cd = 0.14f, Desc = "[ЛКМ] Каждое попадание накладывает стаки Фиксации, увеличивая урон по цели и взрываясь сном на 20 стаках!" },
                    ["m2"] = new() { Name = "Усыпляющий Кинжал (Sleep Dagger)", Cd = 3.6f, Desc = "[ПКМ] Мгновенный бросок кинжала снов: усыпляет/оглушает цель, накладывает 10 стаков Фиксации и наносит крит!" },
                    ["shift"] = new() { Name = "Дымовая Шашка (Smoke Bomb)", Cd = 4.2f, Desc = "[Shift] Скрывается в тумане снов: невидимость, неуязвимость, +50% скорости бега и +35 щита!" },
                    ["r"] = new() { Name = "Танец Пуль (Bullet Dance)", Cd = 9.5f, Desc = "[R] Хейз кружится в вихревом танце, выпуская шквал точных очередей с Фиксацией по всем врагам вокруг!" }
                }
            },
            ["dmc_trio"] = new()
            {
                Key = "dmc_trio", Name = "ТРИО DMC (ДАНТЕ / НЕРО / ВЕРГИЛИЙ)", Icon = "DMC", Subtitle = "Охотники на Демонов [1: ДАНТЕ | 2: НЕРО | 3: ВЕРГИЛИЙ]",
                Hp = 145, Speed = 11.4f, Damage = 20.5f, FireRate = 4.3f, ProjSpeed = 65f, ProjScale = 0.30f, Aoe = 2.0f,
                BodyColor = Rgb(195, 34, 46), ArmorColor = Rgb(36, 38, 48), VisorColor = Rgb(255, 85, 95),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Мятежник + Эбони и Айвори", Cd = 0.23f, Desc = "[ЛКМ] Комбо демоническим мечом Rebellion вблизи + скорострельная стрельба из парных пистолетов Ebony & Ivory." },
                    ["m2"] = new() { Name = "Жало (Stinger) + Drive", Cd = 3.5f, Desc = "[ПКМ] Молниеносный пробивной выпад мечом вперёд с демонической ударной волной Drive!" },
                    ["shift"] = new() { Name = "Стиль Ловкача / Королевский Страж", Cd = 3.8f, Desc = "[Shift] Телепортационный рывок Trickster с блоком Royal Guard (+40 щита и отражение урона)." },
                    ["r"] = new() { Name = "Истинный Демонический Облик (Sin Devil Trigger)", Cd = 10.5f, Desc = "[R] Пробуждает крылатую форму Sin Devil Trigger, исцеляя Данте и обрушивая адский взрыв Judgment!" }
                }
            },
            ["sanabi"] = new()
            {
                Key = "sanabi", Name = "ГЕНЕРАЛ САННАБИ [ЭКСКЛЮЗИВ]", Icon = "SNB", Subtitle = "Бригадный Генерал Чосона и Мари [ЦЕПНОЙ КРЮК, МАЯТНИК И ИМПУЛЬС]",
                Hp = 145, Speed = 12.0f, Damage = 22.0f, FireRate = 4.2f, ProjSpeed = 88f, ProjScale = 0.32f, Aoe = 2.4f,
                BodyColor = Rgb(52, 68, 56), ArmorColor = Rgb(44, 48, 58), VisorColor = Rgb(65, 235, 255),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Рывок Цепного Крюка (Chain-Arm Breach)", Cd = 0.23f, Desc = "[ЛКМ] Мгновенный захват цели гигантской Рукой-Крюком и сокрушительный таранный удар в упор! Урон растёт от набранного Импульса скорости!" },
                    ["m2"] = new() { Name = "Гарпун-Якорь Маго (Chain Grapple)", Cd = 2.6f, Desc = "[ПКМ] Выстреливает стальным 3D-тросом в точку прицела или врага, притягивая Генерала на сверхзвуковой скорости (+100% Импульса и сброс КД Shift)!" },
                    ["shift"] = new() { Name = "Маятниковый Свинг-Импульс (Pendulum Boost)", Cd = 3.0f, Desc = "[Shift] Акробатический маятниковый рывок на цепи в воздухе или над землёй (+45 щита, взрывная волна и набор максимального Импульса)!" },
                    ["r"] = new() { Name = "Протокол «САННАБИ»: Перегрузка Мари (Overdrive)", Cd = 10.5f, Desc = "[R] Мари взламывает поле боя: время вокруг замедляется, КД крюка обнуляется, а Генерал обрушивает казнь цепными молниями по всем врагам!" }
                }
            },
            ["jack_o"] = new()
            {
                Key = "jack_o", Name = "ДЖЕК-О ВАЛЕНТАЙН", Icon = "JKO", Subtitle = "Повелительница Слуг и Железной Тыквы [GUILTY GEAR -STRIVE-]",
                Hp = 135, Speed = 10.9f, Damage = 24.5f, FireRate = 3.2f, ProjSpeed = 58f, ProjScale = 0.42f, Aoe = 2.8f,
                BodyColor = Rgb(245, 245, 250), ArmorColor = Rgb(198, 32, 48), VisorColor = Rgb(135, 255, 65),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Пинок Железной Тыквой (Iron Pumpkin Kick)", Cd = 0.31f, Desc = "[ЛКМ] Высокий пинок каблуком запускает прикованный к ноге шар на цепи вперёд по широкой дуге, сбивая врагов!" },
                    ["m2"] = new() { Name = "Призыв Слуги (Summon Servant)", Cd = 3.2f, Desc = "[ПКМ] Спавнит маленького миньончика-рыцаря (до 4 шт.), который атакует врагов леденцом. При малом HP миньон взрывается!" },
                    ["shift"] = new() { Name = "Командный Флаг (Attack / Defend Cheer)", Cd = 4.2f, Desc = "[Shift] Прыгает с флагом в руке и ставит его в точке приземления: флаг живёт 12 сек. и баффает миньонов на УРОН или ЗАЩИТУ!" },
                    ["r"] = new() { Name = "Forever Elysion Driver!", Cd = 9.5f, Desc = "[R] Прыгает вперёд с шаром в руке, насаживает его на врага, взмывает с ним в небо и жёстко вбивает вниз с колоссальным уроном!" }
                }
            }
        };

        static SurvivorDatabase()
        {
            RegisterExpansionPack1Classes();
            RegisterExpansionPack2Classes();
            InitializeLoadoutsAndSkins();
            SetupExpansionPack1Loadouts();
            SetupExpansionPack2Loadouts();
        }

        internal static Dictionary<string, List<SkillDef>> BuildCustomSkinSkillSet(
            string formDisplayName,
            SkillDef stdM1, SkillDef altM1,
            SkillDef stdM2, SkillDef altM2,
            SkillDef stdShift, SkillDef altShift,
            SkillDef stdR, SkillDef altR)
        {
            altM1.UnlockAchKey = "ach_slayer";
            altM1.UnlockReqText = $"Играя за {formDisplayName}: Уничтожьте 12 монстров за забег (Истребитель)";
            altM2.UnlockAchKey = "ach_boss";
            altM2.UnlockReqText = $"Играя за {formDisplayName}: Победите Босса Телепорта (Убийца Титанов)";
            altShift.UnlockAchKey = "ach_traveler";
            altShift.UnlockReqText = $"Играя за {formDisplayName}: Перейдите на 2-й этап (Исследователь миров)";
            altR.UnlockAchKey = "ach_hoarder";
            altR.UnlockReqText = $"Играя за {formDisplayName}: Соберите 6 предметов за забег (Коллекционер)";

            return new Dictionary<string, List<SkillDef>>
            {
                ["m1"] = new List<SkillDef> { stdM1, altM1 },
                ["m2"] = new List<SkillDef> { stdM2, altM2 },
                ["shift"] = new List<SkillDef> { stdShift, altShift },
                ["r"] = new List<SkillDef> { stdR, altR }
            };
        }

        internal static void SetupCharLoadout(
            string key, string lore, string passiveName, string passiveDesc,
            SkillDef altM1, SkillDef altM2, SkillDef altShift, SkillDef altR,
            SkinDef reskin, SkinDef masterySkin)
        {
            if (!Classes.TryGetValue(key, out var cfg)) return;
            cfg.BaseBodyColor = cfg.BodyColor;
            cfg.BaseArmorColor = cfg.ArmorColor;
            cfg.BaseVisorColor = cfg.VisorColor;
            cfg.OverviewLore = lore;
            cfg.PassiveName = passiveName;
            cfg.PassiveDesc = passiveDesc;
            cfg.BaseSubtitle = cfg.Subtitle;
            cfg.BasePassiveName = passiveName;
            cfg.BasePassiveDesc = passiveDesc;

            altM1.UnlockAchKey = "ach_slayer";
            altM1.UnlockReqText = $"Играя за {cfg.Name}: Уничтожьте 12 монстров за забег (Истребитель)";

            altM2.UnlockAchKey = "ach_boss";
            altM2.UnlockReqText = $"Играя за {cfg.Name}: Победите Босса Телепорта (Убийца Титанов)";

            altShift.UnlockAchKey = "ach_traveler";
            altShift.UnlockReqText = $"Играя за {cfg.Name}: Перейдите на 2-й этап (Исследователь миров)";

            altR.UnlockAchKey = "ach_hoarder";
            altR.UnlockReqText = $"Играя за {cfg.Name}: Соберите 6 предметов за забег (Коллекционер)";

            if (!reskin.UnlockedByDefault)
            {
                if (string.IsNullOrEmpty(reskin.UnlockAchKey) || !reskin.UnlockAchKey.StartsWith("ach_persona_", StringComparison.Ordinal))
                {
                    reskin.UnlockAchKey = "ach_stage3";
                    reskin.UnlockReqText = $"Играя за {cfg.Name}: Доберитесь до 3-го этапа (Покоритель Глубин)";
                }
            }

            if (!masterySkin.UnlockedByDefault)
            {
                if (!string.IsNullOrEmpty(masterySkin.UnlockAchKey) && masterySkin.UnlockAchKey.StartsWith("ach_persona_", StringComparison.Ordinal))
                {
                    // Уникальное персональное испытание альтернативной личности — сохраняем как есть
                }
                else if (masterySkin.UnlockAchKey == "ach_stage3")
                {
                    masterySkin.UnlockAchKey = "ach_stage4";
                    masterySkin.UnlockReqText = $"Играя за {cfg.Name}: Доберитесь до 4-го этапа или победите Митрикса";
                }
                else
                {
                    masterySkin.UnlockReqText = $"Играя за {cfg.Name}: {masterySkin.UnlockReqText}";
                }
            }

            cfg.SkillVariants["m1"] = new List<SkillDef> { cfg.Skills["m1"], altM1 };
            cfg.SkillVariants["m2"] = new List<SkillDef> { cfg.Skills["m2"], altM2 };
            cfg.SkillVariants["shift"] = new List<SkillDef> { cfg.Skills["shift"], altShift };
            cfg.SkillVariants["r"] = new List<SkillDef> { cfg.Skills["r"], altR };
            foreach (var kv in cfg.SkillVariants)
                cfg.BaseSkillVariants[kv.Key] = kv.Value;

            cfg.Skins.Add(new SkinDef
            {
                Name = key == "dmc_trio" ? "Данте [Слот 1]" : "Классический",
                Desc = key == "dmc_trio"
                    ? "Сын Спарды Данте: красный плащ, демонический меч Rebellion и пистолеты Ebony & Ivory."
                    : $"Стандартное снаряжение и классический облик выжившего ({cfg.Name}).",
                UnlockAchKey = null,
                UnlockReqText = "Открыт по умолчанию",
                UnlockedByDefault = true,
                ChangesModel = false,
                BodyColor = cfg.BaseBodyColor,
                ArmorColor = cfg.BaseArmorColor,
                VisorColor = cfg.BaseVisorColor
            });
            cfg.Skins.Add(reskin);
            cfg.Skins.Add(masterySkin);
            cfg.SyncLoadout();
        }

        private static void InitializeLoadoutsAndSkins()
        {
            SetupCharLoadout("commando",
                "Закалённый в боях оперативник корабля UES «Contact Light». Полагается на высокую скорострельность, мобильность и надёжное тактическое вооружение.",
                "Боевая выучка UES (Тактический Темп)", "Обойма на 12 тяжёлых патронов: перезарядка через [Shift] заряжает первые 4 выстрела бронебойным трассером (x1.35 урона) и ускоряет стрельбу!",
                new SkillDef { Name = "Картечный залп", Cd = 0.24f, Desc = "[ЛКМ] Выпускает веер из 4 бронебойных пуль на ближней и средней дистанции." },
                new SkillDef { Name = "Фазовый дробовик", Cd = 3.0f, Desc = "[ПКМ] Мощный веер из 5 пробивных плазменных зарядов (по 90% урона каждый)." },
                new SkillDef { Name = "Тактический подкат", Cd = 3.6f, Desc = "[Shift] Стремительный подкат вперёд с перезарядкой Тяжёлой Обоймы и +22 щита." },
                new SkillDef { Name = "Осколочная граната (Рикошет)", Cd = 6.5f, Desc = "[R] Бросает 2 фугасные гранаты, рикошетящие от стен по баллистической дуге и оглушающие врагов!" },
                new SkinDef { Name = "Шершень", Desc = "Чёрно-багровая расцветка элитного штурмового подразделения UES.", UnlockAchKey = "ach_slayer", UnlockReqText = "Достижение: Истребитель (12 монстров)", BodyColor = Rgb(215, 42, 48), ArmorColor = Rgb(30, 34, 42), VisorColor = Rgb(255, 215, 55) },
                new SkinDef { Name = "Сёгун UES [Модель]", Desc = "Кибер-самурайская броня с рогатым гребнем кабуто, наплечниками содэ и плазменной катаной.", UnlockAchKey = "ach_carnage", UnlockReqText = "Испытание: Кровавая Жатва (25 врагов за забег)", ChangesModel = true, BodyColor = Rgb(238, 240, 248), ArmorColor = Rgb(185, 35, 48), VisorColor = Rgb(255, 210, 65) });

            SetupCharLoadout("artificer",
                "Верховный учёный-архимаг Высшего Суда. Управляет плазмой, солнечным пламенем и криогенной материей с помощью нано-костюма ENV.",
                "Костюм ENV и Элементальный Резонанс", "Удержание [Space] включает реактивное парение (+20% урона сверху). Смешивание Огня, Льда и Молнии вызывает взрывные реакции Таяния, Перегрузки и Сверхпроводимости!",
                new SkillDef { Name = "Солнечные дротики", Cd = 0.34f, Desc = "[ЛКМ] Выпускает парные сгустки солнечной плазмы, поджигающие врагов для реакций стихий." },
                new SkillDef { Name = "Литое нано-копьё", Cd = 4.2f, Desc = "[ПКМ] Сверхскоростное ледяное копьё, прошивающее и замораживающее врагов на 3.5 сек." },
                new SkillDef { Name = "Криогенный барьер", Cd = 4.0f, Desc = "[Shift] Рывок с ледяной волной вокруг себя и получением +35 щита." },
                new SkillDef { Name = "Штормовая Сфера Ионов", Cd = 9.0f, Desc = "[R Альт.] Взмывает вверх и создаёт парящую грозовую сферу, бьющую цепными молниями по площади!" },
                new SkinDef { Name = "Хромированный Архимаг", Desc = "Изумрудно-серебряная мантия исследователя далёких звёздных систем.", UnlockAchKey = "ach_traveler", UnlockReqText = "Достижение: Исследователь миров (2-й этап)", BodyColor = Rgb(42, 165, 135), ArmorColor = Rgb(210, 225, 240), VisorColor = Rgb(115, 255, 195) },
                new SkinDef { Name = "Верховный Серафим [Модель]", Desc = "Небесный облик с 4 парящими золотыми крыльями серафима и короной света.", UnlockAchKey = "ach_arsenal", UnlockReqText = "Испытание: Вооружён до зубов (12 предметов)", ChangesModel = true, BodyColor = Rgb(245, 242, 252), ArmorColor = Rgb(245, 195, 65), VisorColor = Rgb(105, 235, 255) });

            SetupCharLoadout("huntress",
                "Молниеносный следопыт, способный вести прицельный огонь из энергетического лука прямо на бегу и мгновенно менять позицию.",
                "Ловкость и Метка Охотницы", "Бег заряжает критический выстрел Хищника, а Глефа вешает Метку Охоты, заставляя стрелы [ЛКМ] рикошетить в помеченные цели!",
                new SkillDef { Name = "Шквал стрел (Тройной)", Cd = 0.38f, Desc = "[ЛКМ] Выпускает сразу 3 быстрые энергетические стрелы веером." },
                new SkillDef { Name = "Магнитное копьё", Cd = 4.8f, Desc = "[ПКМ] Пробивной выстрел, стягивающий, оглушающий и помечающий врагов взрывом." },
                new SkillDef { Name = "Фазовый шаг (Быстрый)", Cd = 2.1f, Desc = "[Shift] Короткий мгновенный скачок с быстрой перезарядкой и +15 щита." },
                new SkillDef { Name = "Баллиста (3 ручных залпа)", Cd = 8.5f, Desc = "[R] Зависает в воздухе (2.8 сек.) и открывает 3 ручных снайперских выстрела на [ЛКМ]!" },
                new SkinDef { Name = "Лесной фантом", Desc = "Изумрудно-золотой камуфляж элитного снайпера джунглей.", UnlockAchKey = "ach_slayer", UnlockReqText = "Достижение: Истребитель (12 монстров)", BodyColor = Rgb(48, 175, 95), ArmorColor = Rgb(32, 46, 38), VisorColor = Rgb(255, 225, 75) },
                new SkinDef { Name = "Арктический Следопыт [Модель]", Desc = "Белоснежный меховой капюшон с волчьими ушами, плащ и ледяной кристальный лук.", UnlockAchKey = "ach_stage3", UnlockReqText = "Испытание: Покоритель Глубин (3-й этап)", ChangesModel = true, BodyColor = Rgb(215, 235, 255), ArmorColor = Rgb(55, 85, 130), VisorColor = Rgb(95, 240, 255) });

            SetupCharLoadout("buzz",
                "Легендарный Космический Рейнджер Звёздного Командования. Оснащён рикошетящим наручным лазером и раскрывающимися крыльями. В 3-м слоте скина превращается в заклятого врага — ИМПЕРАТОРА ЗУРГА с 8 собственными навыками!",
                "Крылья Star Command («Падение со стилем!»)", "После взлёта [Shift] зажмите [Space] для планирования на крыльях или нажмите [Ctrl], чтобы обрушиться вниз метеором! Лазер рикошетит от стен с ростом урона.",
                new SkillDef { Name = "Двойной рикошет-бластер", Cd = 0.25f, Desc = "[ЛКМ] Выпускает парные рикошетящие от стен лазеры зелёной плазмы (+25% урона за отскок)." },
                new SkillDef { Name = "Световой клинок рейнджера", Cd = 3.5f, Desc = "[ПКМ] Широкий энергетический взмах клинком перед собой с оглушением и +25 щита." },
                new SkillDef { Name = "Сверхзвуковой таран", Cd = 3.8f, Desc = "[Shift] Стремительный пролёт сквозь врагов с раскрытием крыльев для планирования!" },
                new SkillDef { Name = "Лазерный Спутник Star Command", Cd = 8.5f, Desc = "[R] Вызывает движущийся сходящийся луч орбитального спутника Командования и даёт +50 щита!" },
                new SkinDef { Name = "Стелс-Рейнджер", Desc = "Тёмно-синий тактический скафандр для секретных операций в глубоком космосе.", UnlockAchKey = "ach_traveler", UnlockReqText = "Достижение: Исследователь миров (2-й этап)", BodyColor = Rgb(55, 145, 245), ArmorColor = Rgb(35, 42, 58), VisorColor = Rgb(255, 195, 50) },
                new SkinDef
                {
                    Name = "Император Зург [Альт. Личность]",
                    Desc = "Заклятый враг Звёздного Альянса — Император Зург! Парящая грави-платформа, трёхствольный Ионный Бластер, дроны-шершни и 8 собственных навыков Тёмной Империи!",
                    UnlockedByDefault = false,
                    UnlockAchKey = "ach_persona_buzz_zurg",
                    UnlockReqText = "Играя за Базза Лайтера: Победите Дредноут Императора Зурга или 2 Боссов за один забег",
                    ChangesModel = true,
                    BodyColor = Rgb(145, 42, 195),
                    ArmorColor = Rgb(36, 32, 46),
                    VisorColor = Rgb(255, 55, 65),
                    CustomSubtitle = "Император Зург [АЛЬТ. ЛИЧНОСТЬ | ПОВЕЛИТЕЛЬ ИОННОГО АРСЕНАЛА]",
                    CustomPassiveName = "Антигравитационная Платформа Зурга",
                    CustomPassiveDesc = "Зург плавно скользит над поверхностью без трения, не теряет скорость при стрельбе и накапливает перегрузку Ионного Ядра (+30% урона на пике)!",
                    CustomSkillVariants = BuildCustomSkinSkillSet(
                        "Император Зург",
                        new SkillDef { Name = "Трёхствольный Ионный Бластер", Cd = 0.22f, Desc = "[ЛКМ Зург] Вращающийся барабан выпускает поочерёдные разрывные плазменные сферы Тёмной Материи!" },
                        new SkillDef { Name = "Копьё Тёмной Материи", Cd = 0.34f, Desc = "[ЛКМ Альт. Зург] Пробивной пурпурный луч высокой плотности, накладывающий распад брони!" },
                        new SkillDef { Name = "Магнитный Отражатель Императора", Cd = 3.6f, Desc = "[ПКМ Зург] Включает пурпурный щит, поглощающий вражеские снаряды и выстреливающий их обратно залпом!" },
                        new SkillDef { Name = "Гравитационная Воронка", Cd = 4.0f, Desc = "[ПКМ Альт. Зург] Бросает сферу сингулярности, стягивающую всех монстров под огонь бластера!" },
                        new SkillDef { Name = "Эскадрилья Ботов-Шершней", Cd = 4.2f, Desc = "[Shift Зург] Рывок на грави-тяге с выпуском 2 автономных летающих Дронов-Шершней (+35 щита)!" },
                        new SkillDef { Name = "Телепорт Тёмного Плаща", Cd = 3.5f, Desc = "[Shift Альт. Зург] Мгновенный квантовый перенос сквозь врагов с ионным взрывом на месте исчезновения!" },
                        new SkillDef { Name = "Орудие Разрушителя Планет", Cd = 9.5f, Desc = "[R Зург] Заряжает и выпускает колоссальный багрово-фиолетовый луч смерти, выжигающий всё по курсу!" },
                        new SkillDef { Name = "Ковровая Бомбардировка Флота Зурга", Cd = 10.0f, Desc = "[R Альт. Зург] Призывает залп ионных торпед Дредноута и усиливает всех Дронов-Шершней!" })
                });

            SetupCharLoadout("v1",
                "Экспериментальная боевая машина V1, работающая на свежей крови. Переключается между 5 видами оружия [1-5], оседлывает ракеты и бьёт 3 руками [F/G/V]. В 3-м слоте скина переключается на багрового соперника — V2 с Титановой Бронёй и 8 уникальными приёмами!",
                "Кровь — это топливо + Rocket Riding", "Убийства и атаки вблизи чинят корпус V1, ранг Стиля умножает урон, а замороженную ракету [5] можно оседлать в полёте!",
                new SkillDef { Name = "Альтернативный Slab-Арсенал [1-5]", Cd = 0.20f, Desc = "[ЛКМ] Тяжёлые Slab-модификации всех 5 пушек: +35% к урону и взрывные попадания." },
                new SkillDef { Name = "Револьвер Sharpshooter (Рикошет)", Cd = 0.85f, Desc = "[ПКМ Альт.] Раскручивает револьвер и выпускает багровый луч, 3 раза рикошетящий от стен сквозь врагов!" },
                new SkillDef { Name = "Слэм-пикирование (Slam)", Cd = 0.80f, Desc = "[Shift] В воздухе мгновенно обрушивается вниз с ударной волной, на земле — длинный слайд-рывок." },
                new SkillDef { Name = "Кровавая Сверхновая", Cd = 4.5f, Desc = "[R] Выпускает 6 самонаводящихся кровавых копий во врагов, мгновенно исцеляя V1." },
                new SkinDef { Name = "Золотой Серафим (Габриэль)", Desc = "Бело-золотая расцветка Высшего Ангела Совета с лазурными крыльями.", UnlockAchKey = "ach_boss", UnlockReqText = "Достижение: Убийца Титанов (Победа над Боссом)", BodyColor = Rgb(242, 244, 250), ArmorColor = Rgb(235, 190, 55), VisorColor = Rgb(85, 225, 255) },
                new SkinDef
                {
                    Name = "V2 Высшая Машина [Альт. Личность]",
                    Desc = "Вторая модель высшего класса — V2! Не впитывает кровь от обычных попаданий, но защищена Титановой Бронёй (+80 Щита), отпрыгивает веером из 3 монет и впадает в Багровую Ярость!",
                    UnlockedByDefault = false,
                    UnlockAchKey = "ach_persona_v1_v2",
                    UnlockReqText = "Играя за V1: Достигните ранга стиля ULTRAKILL / SSS (800+ очков стиля) в бою",
                    ChangesModel = true,
                    BodyColor = Rgb(215, 38, 48),
                    ArmorColor = Rgb(42, 32, 36),
                    VisorColor = Rgb(255, 225, 55),
                    CustomSubtitle = "V2 [АЛЬТ. ЛИЧНОСТЬ | ТИТАНОВАЯ БРОНЯ И БАГРОВАЯ ДУЭЛЬ]",
                    CustomPassiveName = "Титановая Обшивка V2 (Hardened Plating)",
                    CustomPassiveDesc = "+80 к прочной Титановой Броне, но НЕТ обычного вампиризма от крови вблизи! Починка корпуса и щита идёт через +PARRY, Рикошеты монет, Core Nuke и убийства на ранге S+, а пробитие брони включает Багровый Овердрайв!",
                    CustomSkillVariants = BuildCustomSkinSkillSet(
                        "V2 (Supreme Machine)",
                        new SkillDef { Name = "Тяжёлый Пирсер V2 [1-5]", Cd = 0.19f, Desc = "[ЛКМ V2] Скоростной дуэльный огонь V2 с повышенной кучностью и багровыми трассерами!" },
                        new SkillDef { Name = "Бронебойный Slab-Залп V2 [1-5]", Cd = 0.23f, Desc = "[ЛКМ Альт. V2] Утяжелённые разрывные патроны V2, раскалывающие броню при прямом попадании!" },
                        new SkillDef { Name = "Сальто и Веер 3 Монет!", Cd = 1.35f, Desc = "[ПКМ V2] V2 делает дуэльное сальто назад и подбрасывает сразу 3 золотые монеты веером для тройного Ricoshot!" },
                        new SkillDef { Name = "Заряженный Луч Пирсера / Ядро", Cd = 1.10f, Desc = "[ПКМ Альт. V2] Мгновенный пробивной багровый луч с 3 рикошетами от стен (или выброс 2 ядер на дробовике)!" },
                        new SkillDef { Name = "Багровый Реактивный Слайд", Cd = 0.85f, Desc = "[Shift V2] Сверхскоростное скольжение на крыльях-лезвиях с выстрелом микро-ракетами и +25 брони!" },
                        new SkillDef { Name = "Пикирующий Удар Наклбластера", Cd = 1.10f, Desc = "[Shift Альт. V2] Взрывной прыжок и таран красной рукой Knuckleblaster с ударной волной!" },
                        new SkillDef { Name = "Гарпун Whiplash + Взрыв Кулака", Cd = 3.2f, Desc = "[R V2] Стремительное сближение на зелёном тросе Whiplash с автоматическим разрывным ударом Knuckleblaster!" },
                        new SkillDef { Name = "Протокол «Клэр де Люн» (Ярость Крыльев)", Cd = 8.0f, Desc = "[R Альт. V2] Раскрывает золотые крылья V2: восстанавливает Титановую Броню, бросает 4 монеты и ускоряет стрельбу на 40%!" })
                });

            SetupCharLoadout("sol",
                "Охотник за головами и прототип Гира с огненным мечом Fireseal. Нажмите [Q] при 50% шкалы Напряжения (Tension), чтобы активировать ROMAN CANCEL и замедлить время вокруг! В 3-м слоте скина — «Кай Киске»!",
                "Пламя Порчи и Roman Cancel [Q]", "Удары и рывки копят шкалу Tension (0-100%). Нажатие [Q] за 50% Tension мгновенно отменяет анимацию, замедляет врагов вокруг и усиливает комбо на +25%!",
                new SkillDef { Name = "Fafnir (Сокрушительный выпад)", Cd = 0.44f, Desc = "[ЛКМ Альт.] Тяжёлый прямой удар огненным кулаком с взрывом, пробитием брони и отбрасыванием." },
                new SkillDef { Name = "Volcanic Viper (Апперкот)", Cd = 3.6f, Desc = "[ПКМ Альт.] Взмывает вверх с пылающим восходящим ударом меча, оглушая врагов вокруг." },
                new SkillDef { Name = "Night Raid Vortex (Прорыв)", Cd = 3.8f, Desc = "[Shift Альт.] Стремительный прорыв по земле сквозь врагов с мощным огненным апперкотом." },
                new SkillDef { Name = "Tyrant Rave (Тиранский Взрыв)", Cd = 10.0f, Desc = "[R Альт.] Пробуждает силу Гира и выпускает колоссальный огненный взрыв на 650% урона перед собой!" },
                new SkinDef { Name = "Орденский Сол [Модель]", Desc = "Облик Святого Рыцаря Ордена: бело-золотой плащ, корона и каменная плита меча Fireseal.", UnlockAchKey = "ach_stage3", UnlockReqText = "Достижение: Покоритель Глубин (3-й этап)", ChangesModel = true, BodyColor = Rgb(240, 238, 245), ArmorColor = Rgb(185, 38, 48), VisorColor = Rgb(255, 205, 55) },
                new SkinDef
                {
                    Name = "Кай Киске (Гром Иллирии) [Альт. Версия]",
                    Desc = "Альтернативная версия Кая Киске — Первого Короля Иллирии и вечного соперника Сола! Меч Громовая Печать (Thunderseal), бело-лазурный королевский плащ и 8 новых способностей молнии!",
                    UnlockedByDefault = false,
                    UnlockAchKey = "ach_persona_sol_ky",
                    UnlockReqText = "Играя за Сола Бэдгая: Кодекс Священного Ордена — победите Босса, не имея Лунных и Пустотных предметов",
                    ChangesModel = true,
                    BodyColor = Rgb(240, 244, 255),
                    ArmorColor = Rgb(42, 108, 225),
                    VisorColor = Rgb(95, 238, 255),
                    CustomSubtitle = "Кай Киске [АЛЬТ. ВЕРСИЯ | ПЕРВЫЙ КОРОЛЬ ИЛЛИРИИ]",
                    CustomPassiveName = "Знак Грома (Shock State)",
                    CustomPassiveDesc = "Громовые атаки Кая Киске накладывают Шоковый Заряд, усиливающий урон его умений на +25% и вызывающий цепные дуги молний между врагами!",
                    CustomSkillVariants = BuildCustomSkinSkillSet(
                        "Кай Киске",
                        new SkillDef { Name = "Громовая Печать (Thunderseal Slash)", Cd = 0.25f, Desc = "[ЛКМ Кай] Благородные фехтовальные взмахи мечом Thunderseal с выпуском дугового разряда молнии и цепным Шоком." },
                        new SkillDef { Name = "Священный Выпад (Vapor Thrust)", Cd = 0.34f, Desc = "[ЛКМ Альт. Кай] Пробивной молниеносный выпад клинком, высекающий столп небесной молнии на цели!" },
                        new SkillDef { Name = "Лезвие Грома (Stun Edge — Заряд)", Cd = 3.2f, Desc = "[ПКМ Кай — ЗАРЯД] Удерживайте ПКМ, чтобы заряжать золотой Stun Edge! Чем дольше держите кнопку, тем мощнее и крупнее снаряд!" },
                        new SkillDef { Name = "Священный Крест (Charged Stun Edge + Dire Eclat)", Cd = 3.6f, Desc = "[ПКМ Альт. Кай — ЗАРЯД] Удерживайте ПКМ для многоударного Charged Stun Edge + разряд Dire Eclat вокруг (+ щит от заряда)!" },
                        new SkillDef { Name = "Скользящий Выпад (Stun Dipper)", Cd = 3.4f, Desc = "[Shift Кай] Стремительный низкий подкат по земле + мощный электрический разряд впереди, оглушающий врагов (+40 щита)!" },
                        new SkillDef { Name = "Дуга Грома (Foudre Arc)", Cd = 3.2f, Desc = "[Shift Альт. Кай] Акробатическое колесо в прыжке с ударом молнией сверху вниз, сбивающее врагов с ног!" },
                        new SkillDef { Name = "Оседлать Молнию! (Ride the Lightning)", Cd = 9.5f, Desc = "[R Кай] Увеличенная дальность полёта копьём молнии со светящимся магическим барьером спереди, пронзающим врагов!" },
                        new SkillDef { Name = "Драконий Инсталл: Весы Юноны (Sacred Edge)", Cd = 11.0f, Desc = "[R Альт. Кай] Пробуждает глаз Гира и крыло молнии Весов Юноны (10 сек.) + выпускает веер из 5 Священных Крестов!" })
                });

            SetupCharLoadout("gwen",
                "Ожившая кукла, вооружённая Священными Ножницами, иглами и благословенным Священным Туманом. Сражается в изометрическом MOBA-режиме.",
                "Тысяча порезов", "Удары ножницами восстанавливают здоровье, а каждые 4 атаки открывают вихревую серию разрезов.",
                new SkillDef { Name = "Священный стежок", Cd = 0.24f, Desc = "[ЛКМ / Q] Каждый взмах ножниц дополнительно выпускает вперёд пробивную призрачную иглу." },
                new SkillDef { Name = "Вихревые ножницы", Cd = 3.8f, Desc = "[W / СКМ] Круговой разрез ножницами вокруг Гвен, притягивающий врагов и дающий +30 щита." },
                new SkillDef { Name = "Сквозной крой", Cd = 4.2f, Desc = "[Shift] Длинный рывок сквозь врагов с мгновенной активацией серии из 6 разрезов!" },
                new SkillDef { Name = "Королевская Вышивка (Веер 7 игл)", Cd = 8.5f, Desc = "[R] Мгновенно выпускает широкий веер из 7 священных игл с мощным вампиризмом." },
                new SkinDef { Name = "Готическая Роза", Desc = "Багрово-чёрное бархатное платье с рубиновыми ножницами и золотым шитьём.", UnlockAchKey = "ach_hoarder", UnlockReqText = "Достижение: Коллекционер (6 предметов)", BodyColor = Rgb(225, 45, 95), ArmorColor = Rgb(28, 22, 34), VisorColor = Rgb(255, 165, 205) },
                new SkinDef { Name = "Звёздная Хранительница [Модель]", Desc = "Корона Звёздной Хранительницы, парящий фамильяр-звезда и золотые крылья-банты.", UnlockAchKey = "ach_arsenal", UnlockReqText = "Испытание: Вооружён до зубов (12 предметов)", ChangesModel = true, BodyColor = Rgb(255, 135, 215), ArmorColor = Rgb(248, 245, 255), VisorColor = Rgb(255, 230, 95) });

            SetupCharLoadout("enforcer",
                "Несокрушимый боец спецназа UES со штурмовым дробовиком и энергетическим щитом. Способен отражать и парировать любые атаки врагов.",
                "Бастион Порядка", "В стойке щита снижает входящий урон на 65% и отражает лобовые снаряды обратно во врагов.",
                new SkillDef { Name = "Тяжёлый пулемёт подавления", Cd = 0.16f, Desc = "[ЛКМ] Скорострельный огонь крупнокалиберными бронебойными пулями." },
                new SkillDef { Name = "Подствольная светошумовая", Cd = 3.8f, Desc = "[ПКМ] Выпускает разрывную гранату, оглушающую группу врагов на дистанции." },
                new SkillDef { Name = "Таранный прорыв со щитом", Cd = 3.5f, Desc = "[Shift] Стремительный таран вперёд, сбивающий врагов с ног и дающий +45 щита." },
                new SkillDef { Name = "Ударная волна Бастиона", Cd = 9.0f, Desc = "[R] Сотрясает землю вокруг себя, оглушая всех врагов и призывая 3 орбитальных удара." },
                new SkinDef { Name = "Золотой Гвардеец", Desc = "Парадная черно-золотая броня преторианской гвардии.", UnlockAchKey = "ach_boss", UnlockReqText = "Достижение: Убийца Титанов (Победа над Боссом)", BodyColor = Rgb(235, 185, 45), ArmorColor = Rgb(32, 34, 44), VisorColor = Rgb(255, 235, 95) },
                new SkinDef { Name = "Судья Мега-Сити [Модель]", Desc = "Шлем Судьи с крестовым визором, золотой орлиный наплечник и силовые шипы на щите.", UnlockAchKey = "ach_boss_hunter", UnlockReqText = "Испытание: Гроза Стражей (3 Босса)", ChangesModel = true, BodyColor = Rgb(42, 52, 78), ArmorColor = Rgb(235, 185, 45), VisorColor = Rgb(255, 55, 55) });

            SetupCharLoadout("scout",
                "Бесстрашный дварф-разведчик корпорации Deep Rock Galactic. Освещает путь Ракетницей [F], летает на крюке-кошке и подбрасывает себя отдачей двустволки.",
                "Скала и Камень! + Ракетница [F]", "Крюк-кошка перезаряжает прыжок и ускоряет бег, а Сигнальная Ракетница на [F] освещает пещеру и помечает уязвимые точки врагов (+25% к шансу крита)!",
                new SkillDef { Name = "Винтовка M1000 Classic", Cd = 0.32f, Desc = "[ЛКМ] Снайперские пробивные выстрелы повышенной мощности (220% урона)." },
                new SkillDef { Name = "Арбалет Болтшарк (Крио-болты)", Cd = 3.4f, Desc = "[ПКМ] Выпускает 2 рикошетящие ледяные стрелы, замораживающие врагов." },
                new SkillDef { Name = "Реактивные ботинки Хоксеса", Cd = 3.5f, Desc = "[Shift] Высокий взлёт на реактивных ботинках с огненной волной под ногами." },
                new SkillDef { Name = "Крио-граната глубокой заморозки", Cd = 8.5f, Desc = "[R] Мгновенно замораживает всех врагов вокруг и наносит 350% урона холодом." },
                new SkinDef { Name = "Магмовый Шахтёр", Desc = "Термостойкий оранжево-обсидиановый скафандр для вулканических биомов.", UnlockAchKey = "ach_traveler", UnlockReqText = "Достижение: Исследователь миров (2-й этап)", BodyColor = Rgb(235, 95, 35), ArmorColor = Rgb(42, 38, 44), VisorColor = Rgb(255, 215, 65) },
                new SkinDef { Name = "Лорд Седой Бороды [Модель]", Desc = "Золотой рогатый шлем дварфа-ветерана, плащ и рунный кристалл на ранце.", UnlockAchKey = "ach_stage3", UnlockReqText = "Испытание: Покоритель Глубин (3-й этап)", ChangesModel = true, BodyColor = Rgb(235, 190, 55), ArmorColor = Rgb(55, 65, 86), VisorColor = Rgb(95, 245, 255) });

            SetupCharLoadout("m4a1",
                "Командир элитного тактического отряда AR (Girls' Frontline). Управляет звеном автономных клонов Dummy Link и осадным комплексом «Юпитер».",
                "Система Dummy Link", "При гибели M4A1 автоматически переносит своё сознание в ближайшего живого клона.",
                new SkillDef { Name = "Импульсный карабин MOD3", Cd = 0.24f, Desc = "[ЛКМ] Выпускает пробивные энергетические снаряды с взрывом по площади." },
                new SkillDef { Name = "Миномётный залп Грифона", Cd = 4.2f, Desc = "[ПКМ] Обрушивает 3 артиллерийских удара по врагам перед собой." },
                new SkillDef { Name = "Маскировочный рывок", Cd = 3.5f, Desc = "[Shift] Рывок с целебным импульсом (+20% HP себе и всем клонам)." },
                new SkillDef { Name = "Осадное Орудие «Юпитер MOD3»", Cd = 8.0f, Desc = "[R Альт.] Развёртывает рельсовую пушку «Юпитер»: все клоны соединяются лучами с M4A1 для 3 колоссальных залпов!" },
                new SkinDef { Name = "Ночной Оперативник", Desc = "Бело-голубой арктический камуфляж спецподразделения Грифона.", UnlockAchKey = "ach_hoarder", UnlockReqText = "Достижение: Коллекционер (6 предметов)", BodyColor = Rgb(225, 235, 248), ArmorColor = Rgb(48, 62, 85), VisorColor = Rgb(85, 225, 255) },
                new SkinDef { Name = "MOD3 Валькирия [Модель]", Desc = "Тактические крылья-антенны MOD3 за спиной, кибер-нимб и золотая отделка брони.", UnlockAchKey = "ach_carnage", UnlockReqText = "Испытание: Кровавая Жатва (25 врагов за забег)", ChangesModel = true, BodyColor = Rgb(45, 52, 64), ArmorColor = Rgb(235, 190, 65), VisorColor = Rgb(115, 255, 165) });

            SetupCharLoadout("medic",
                "Гениальный полевой врач из Team Fortress 2. Зажмите [ПКМ], чтобы связать союзника непрерывным Лечебным Лучом и накопить 100% Убер-заряда!",
                "Лечебный Луч [Удерж. ПКМ] и Насмешка [G]", "Удержание [ПКМ] тянет луч Medi-Gun к союзнику/рекруту, исцеляя его и заряжая Убер-шкалу (0-100%) для бесплатного двойного Убер-заряда!",
                new SkillDef { Name = "Арбалет Крестоносца", Cd = 0.34f, Desc = "[ЛКМ] Мощные дальнобойные болты (210% урона врагам и мгновенное лечение союзников)." },
                new SkillDef { Name = "Импульс Вакцинатора", Cd = 3.2f, Desc = "[ПКМ] Даёт огромный барьер +50 щита и выпускает взрывную волну вокруг Медика." },
                new SkillDef { Name = "Прыжок с Убер-пилой", Cd = 3.8f, Desc = "[Shift] Высокий прыжок вперёд с ударом пилой (+25% к шкале Убер-заряда!)." },
                new SkillDef { Name = "Крицкриг-Шторм", Cd = 9.5f, Desc = "[R] Включает Убер-заряд и обрушивает серию критических молний на всех врагов вокруг!" },
                new SkinDef { Name = "Синяя команда (BLU)", Desc = "Классический синий халат полевого врача корпорации BLU.", UnlockAchKey = "ach_slayer", UnlockReqText = "Достижение: Истребитель (12 монстров)", BodyColor = Rgb(75, 145, 235), ArmorColor = Rgb(35, 75, 145), VisorColor = Rgb(110, 225, 255) },
                new SkinDef { Name = "Чумной Доктор и Архимед [Модель]", Desc = "Золотая маска Чумного Доктора с клювом, цилиндр и парящий голубь Архимед над плечом!", UnlockAchKey = "ach_arsenal", UnlockReqText = "Испытание: Вооружён до зубов (12 предметов)", ChangesModel = true, BodyColor = Rgb(38, 42, 54), ArmorColor = Rgb(235, 190, 60), VisorColor = Rgb(95, 255, 145) });

            SetupCharLoadout("aigis",
                "Боевой андроид 7-го поколения из группы S.E.E.S. (Persona 3). Летает в режиме перегрузки «Оргия» и призывает 3D-Персону Палладион / Афина!",
                "Сердце Папирус и Режим Оргии", "Во время Оргии зажмите [Space] для реактивного полёта! По окончании Оргии корпус выпускает паровой взрыв (+50% брони на 1.8 сек., мгновенно снимается на [R]).",
                new SkillDef { Name = "Крупнокалиберный рельс", Cd = 0.24f, Desc = "[ЛКМ] Пробивные бронебойные выстрелы с повышенным уроном (195%)." },
                new SkillDef { Name = "Ракетный рой «Орион»", Cd = 3.6f, Desc = "[ПКМ] Выпускает 4 самонаводящиеся микро-ракеты по ближайшим врагам." },
                new SkillDef { Name = "Копьё Эгиды (Рывок + Щит)", Cd = 4.2f, Desc = "[Shift] Стремительный выпад вперёд с получением +50 щита и ударной волной." },
                new SkillDef { Name = "Персона: Афина (Тетракарн и Акаша)", Cd = 10.0f, Desc = "[R] Материализует за спиной 3D-Персону Афину с копьём, лечит 40% HP и ставит отражающий щит Тетракарн!" },
                new SkinDef { Name = "Полуночный Страж S.E.E.S.", Desc = "Тёмно-кобальтовый корпус для битв в Тартаре в Тёмный Час.", UnlockAchKey = "ach_traveler", UnlockReqText = "Достижение: Исследователь миров (2-й этап)", BodyColor = Rgb(48, 72, 135), ArmorColor = Rgb(215, 225, 245), VisorColor = Rgb(95, 245, 255) },
                new SkinDef { Name = "Метис / Крылья Персоны [Модель]", Desc = "Алая броня с механическими крыльями-бабочками, боевым визором и золотым нимбом.", UnlockAchKey = "ach_stage4", UnlockReqText = "Испытание: Ветеран Petrichor V (4-й этап)", ChangesModel = true, BodyColor = Rgb(36, 34, 46), ArmorColor = Rgb(225, 45, 65), VisorColor = Rgb(255, 215, 65) });

            SetupCharLoadout("reimu",
                "Жрица храма Хакурей из Генсокё (Touhou Project). Свободно парит на [Space] и накапливает мощь Грейза при пролёте вражеских пуль впритирку!",
                "Парение и Грейз Данмаку (Graze)", "Удержание [Space] даёт полёт, а вражеские снаряды в радиусе 2.5м копят Грейз (до +30% урона), восполняют топливо полёта и выпускают ответную иглу!",
                new SkillDef { Name = "Иглы Истребления Ёкаев", Cd = 0.16f, Desc = "[ЛКМ] Сверхскоростные пробивные алые иглы с высоким прямым уроном." },
                new SkillDef { Name = "Очищающий взмах Гохэй", Cd = 3.2f, Desc = "[ПКМ] Круговой удар жезлом, отбрасывающий врагов и дающий +30 щита." },
                new SkillDef { Name = "Пространственный разлом", Cd = 3.5f, Desc = "[Shift] Мгновенная телепортация вперёд с выпуском 6 самонаводящихся амулетов." },
                new SkillDef { Name = "Дуплекс-Барьер Хакурей", Cd = 9.0f, Desc = "[R] Исцеляет Рейму, даёт +60 щита и обрушивает столпы света на всех врагов вокруг." },
                new SkinDef { Name = "Синяя Жрица Чудес", Desc = "Лазурно-белое облачение жрицы горного святилища.", UnlockAchKey = "ach_hoarder", UnlockReqText = "Достижение: Коллекционер (6 предметов)", BodyColor = Rgb(55, 135, 235), ArmorColor = Rgb(245, 248, 255), VisorColor = Rgb(125, 240, 255) },
                new SkinDef { Name = "Божественная Хакурей [Модель]", Desc = "Парящее кольцо из 6 золотых сфер Инь-Ян за спиной и небесная корона святилища.", UnlockAchKey = "ach_arsenal", UnlockReqText = "Испытание: Вооружён до зубов (12 предметов)", ChangesModel = true, BodyColor = Rgb(235, 45, 75), ArmorColor = Rgb(255, 215, 75), VisorColor = Rgb(255, 245, 150) });

            SetupCharLoadout("mindustry",
                "Автономный командный корабль «Гамма» с планеты Серпуло. Возводит базу из 9 чертежей [1-9], связывает турели Энергосетью и выпускает Штурмовых Дронов!",
                "Энергосеть Серпуло и Дроны [1-9]", "Турели рядом с Ядром или Пилоном стреляют на +30% быстрее, а Чертёж [9.ДРОН] выпускает автономных боевых юнитов!",
                new SkillDef { Name = "Стройка / Тройной лазер Альфы", Cd = 0.28f, Desc = "[ЛКМ] В стройке возводит чертёж, а в боевом режиме стреляет веером из 3 лазеров!" },
                new SkillDef { Name = "Сброс / Энерго-импульс", Cd = 1.8f, Desc = "[ПКМ] В стройке сбрасывает чертёж, а в боевом режиме бьёт врагов вокруг током." },
                new SkillDef { Name = "Форсажная смена режима [Shift/B]", Cd = 0.6f, Desc = "[Shift] Переключает режим Стройки/Боя и даёт кораблю +35 щита." },
                new SkillDef { Name = "Экстренный синтез ресурсов", Cd = 9.0f, Desc = "[R] Мгновенно синтезирует +45 Меди, +30 Кремния, +15 Тория и чинит все постройки!" },
                new SkinDef { Name = "Кобальтовый Флот", Desc = "Сине-титановая обшивка орбитального инженерного корпуса.", UnlockAchKey = "ach_traveler", UnlockReqText = "Достижение: Исследователь миров (2-й этап)", BodyColor = Rgb(65, 165, 255), ArmorColor = Rgb(38, 48, 68), VisorColor = Rgb(140, 245, 255) },
                new SkinDef { Name = "Дредноут «Эклипс» [Модель]", Desc = "Шестикрылый тяжёлый флагман с двумя реакторами Тория и силовым кольцом.", UnlockAchKey = "ach_stage4", UnlockReqText = "Испытание: Ветеран Petrichor V (4-й этап)", ChangesModel = true, BodyColor = Rgb(225, 55, 65), ArmorColor = Rgb(42, 44, 56), VisorColor = Rgb(255, 215, 65) });

            SetupCharLoadout("slark",
                "Неуловимый беглец из подводной тюрьмы Тёмный Риф (Dota 2). С каждым ударом крадёт сущность врагов, становясь сильнее на протяжении всего забега.",
                "Кража Сущности", "Каждое попадание по врагу навсегда (на текущий забег) повышает урон Сларка.",
                new SkillDef { Name = "Ядовитая заточка Рифа", Cd = 0.22f, Desc = "[ЛКМ / Q] Удары крадут сущность и выпускают вперёд водный клинок." },
                new SkillDef { Name = "Всплеск Бездны", Cd = 3.6f, Desc = "[W / СКМ] Взрыв тёмной воды без потери HP, замедляющий врагов и дающий +2 стака сущности." },
                new SkillDef { Name = "Водоворот Хищника", Cd = 3.8f, Desc = "[Shift] Стремительный рывок с круговым ударом и получением +30 щита." },
                new SkillDef { Name = "Глубинный Ужас", Cd = 10.5f, Desc = "[R] Танец Теней (неуязвимость и регенерация) + мгновенное получение 5 стаков сущности!" },
                new SkinDef { Name = "Кровавый Риф", Desc = "Багрово-обсидиановая чешуя глубинного охотника.", UnlockAchKey = "ach_slayer", UnlockReqText = "Достижение: Истребитель (12 монстров)", BodyColor = Rgb(195, 38, 58), ArmorColor = Rgb(32, 24, 36), VisorColor = Rgb(255, 95, 125) },
                new SkinDef { Name = "Акулий Принц Бездны [Модель]", Desc = "Золотая корона-плавник, коралловые наплечники и второй клинок в левой лапе.", UnlockAchKey = "ach_carnage", UnlockReqText = "Испытание: Кровавая Жатва (25 врагов за забег)", ChangesModel = true, BodyColor = Rgb(45, 195, 225), ArmorColor = Rgb(235, 185, 55), VisorColor = Rgb(125, 255, 235) });

            SetupCharLoadout("tidehunter",
                "Древний морской исполин Левиафан (Dota 2). Сбрасывает контроль Панцирем Кракена и подбрасывает всю карту настоящими 3D-щупальцами Бездны!",
                "Панцирь Кракена (Kraken Shell)", "Блокирует плоский урон от каждого удара, а при потере 25% HP автоматически снимает оглушения и выпускает бесплатную круговую волну Якоря!",
                new SkillDef { Name = "Волна Прибоя", Cd = 0.45f, Desc = "[ЛКМ / Q] Удар якорем, дополнительно пускающий вперёд пробивную волну воды." },
                new SkillDef { Name = "Бросок Якоря", Cd = 3.6f, Desc = "[W / СКМ] Мощный бросок водяного якоря, оглушающий и раскалывающий броню врагов." },
                new SkillDef { Name = "Прыжок Левиафана", Cd = 4.2f, Desc = "[Shift] Прыжок вверх с сокрушительным обрушением на землю и +50 щита." },
                new SkillDef { Name = "Гнев Мальстрема (Щупальца Бездны)", Cd = 11.0f, Desc = "[R] Пробуждает из-под земли кольцо гигантских 3D-щупалец, подбрасывающих врагов в воздух, +80 щита!" },
                new SkinDef { Name = "Владыка Вулканов", Desc = "Пепельно-магмовый панцирь древнего доисторического левиафана.", UnlockAchKey = "ach_boss", UnlockReqText = "Достижение: Убийца Титанов (Победа над Боссом)", BodyColor = Rgb(95, 45, 42), ArmorColor = Rgb(225, 95, 35), VisorColor = Rgb(255, 195, 55) },
                new SkinDef { Name = "Дредноут Глубин [Модель]", Desc = "Латунный водолазный шлем с иллюминатором, пушечные наплечники и золотой трезубец.", UnlockAchKey = "ach_boss_hunter", UnlockReqText = "Испытание: Гроза Стражей (3 Босса)", ChangesModel = true, BodyColor = Rgb(42, 115, 135), ArmorColor = Rgb(225, 175, 55), VisorColor = Rgb(95, 245, 255) });

            SetupCharLoadout("freeman",
                "Физик-теоретик центра «Чёрная Меза» в костюме HEV Mark IV. Вблизи Телепорта Грави-пушка перегружается Синей Энергией Цитадели и хватает живых монстров!",
                "Костюм HEV (Морфий + Синяя Грави-пушка)", "Авто-инъекция морфия при HP < 25% (раз в 25 сек.), а при активном Телепорте или рывке Грави-пушка захватывает живых врагов и швыряет их во взрыв!",
                new SkillDef { Name = "Импульсная винтовка AR2", Cd = 0.20f, Desc = "[ЛКМ] Тяжёлые энергетические выстрелы Альянса + удар монтировкой вблизи." },
                new SkillDef { Name = "Энергетический шар Альянса", Cd = 3.2f, Desc = "[ПКМ] Выпускает дезинтегрирующую сферу AR2, рикошетящую до 6 раз между врагами!" },
                new SkillDef { Name = "Инъекция морфия HEV", Cd = 4.2f, Desc = "[Shift] Реактивный рывок с мгновенным лечением +25% HP, +35 щита и перегрузкой Грави-пушки!" },
                new SkillDef { Name = "Глюонный излучатель", Cd = 6.5f, Desc = "[R] Мгновенный квантовый залп чистой энергии без необходимости долгой зарядки!" },
                new SkinDef { Name = "Спецназ HECU (Адриан Шепард)", Desc = "Зелёно-камуфляжный бронежилет PCV корпуса морской пехоты HECU.", UnlockAchKey = "ach_slayer", UnlockReqText = "Достижение: Истребитель (12 монстров)", BodyColor = Rgb(75, 145, 65), ArmorColor = Rgb(38, 46, 38), VisorColor = Rgb(115, 255, 95) },
                new SkinDef { Name = "Шлем HEV + Хедкраб Ламарр [Модель]", Desc = "Золотой костюм HEV Mk.V с приручённым хедкрабом Ламарр на голове!", UnlockAchKey = "ach_stage3", UnlockReqText = "Испытание: Покоритель Глубин (3-й этап)", ChangesModel = true, BodyColor = Rgb(245, 155, 35), ArmorColor = Rgb(42, 46, 56), VisorColor = Rgb(255, 225, 75) });

            SetupCharLoadout("dante",
                "Исполнительный Менеджер №10 компании Limbus Company с пылающими часами вместо головы. Призывает фантомные проекции Грешников в бою и перематывает смерть!",
                "Резонанс Золотой Ветви и Грешников", "Умения призывают фантомов И Сана, Фауст, Хитклиффа и Отис для совместных ударов, а в кооперативе усиливают весь отряд!",
                new SkillDef { Name = "Стрелы Полуночи (Тройной)", Cd = 0.28f, Desc = "[ЛКМ] Выпускает веер из 3 пылающих часовых стрел, пробивающих врагов." },
                new SkillDef { Name = "Карающие Цепи Грешников", Cd = 5.0f, Desc = "[ПКМ] Призывает фантомов Грешников и сковывает врагов вокруг золотыми цепями!" },
                new SkillDef { Name = "Скачок Стрелки Времени", Cd = 4.5f, Desc = "[Shift] Телепортация вперёд с проекцией И Сана, +50 щита и исцелением." },
                new SkillDef { Name = "Э.Г.О. Золотой Ветви (Парад Грешников)", Cd = 11.0f, Desc = "[R] Без потери жизни: призывает 4 проекции Грешников вокруг арены и даёт бафф атаки и щита!" },
                new SkinDef { Name = "Синдикат Пальцев", Desc = "Бело-золотой костюм высшего офицера Города.", UnlockAchKey = "ach_hoarder", UnlockReqText = "Достижение: Коллекционер (6 предметов)", BodyColor = Rgb(242, 240, 248), ArmorColor = Rgb(42, 38, 52), VisorColor = Rgb(255, 215, 65) },
                new SkinDef { Name = "Апокалипсис Полуночи [Модель]", Desc = "4 пылающих золотых крыла серафима за спиной и корона Золотой Ветви.", UnlockAchKey = "ach_stage4", UnlockReqText = "Испытание: Ветеран Petrichor V (4-й этап)", ChangesModel = true, BodyColor = Rgb(215, 35, 52), ArmorColor = Rgb(245, 195, 55), VisorColor = Rgb(255, 240, 120) });

            SetupCharLoadout("cheburashka",
                "Неизвестный науке зверёк из ящика с апельсинами. Наносит сокрушительный урон при падении с высоты и призывает Стенд «Крокодил Гена»!",
                "Чебурахнуться с высоты!", "Чем выше точка падения Чебурашки, тем мощнее ударная волна и радиус взрыва при приземлении.",
                new SkillDef { Name = "Уши-вертолёты!", Cd = 0.28f, Desc = "[ЛКМ] Круговой вихревой удар ушами вокруг себя, подбрасывающий Чебурашку вверх!" },
                new SkillDef { Name = "Ящик апельсинов (Тройной)", Cd = 3.8f, Desc = "[ПКМ] Бросает сразу 3 взрывных шрапнельных апельсина веером!" },
                new SkillDef { Name = "Прыжок на Голубом Вагоне", Cd = 4.0f, Desc = "[Shift] Сверхвысокий вертикальный взлёт в небо для сокрушительного удара при падении!" },
                new SkillDef { Name = "СТЕНД ГЕНА + Апельсиновый дождь", Cd = 50.0f, Desc = "[R] Призывает Стенд Крокодила Гену на 30 сек и запускает салют из 6 взрывных апельсинов!" },
                new SkinDef { Name = "Золотой Апельсин", Desc = "Солнечно-рыжая плюшевая шёрстка с золотистой грудкой.", UnlockAchKey = "ach_traveler", UnlockReqText = "Достижение: Исследователь миров (2-й этап)", BodyColor = Rgb(215, 115, 35), ArmorColor = Rgb(255, 225, 155), VisorColor = Rgb(255, 200, 45) },
                new SkinDef { Name = "Кибер-Чебуратор 3000 [Модель]", Desc = "Неоновый кибер-визор, золотая корона и реактивный ранец на апельсиновой тяге!", UnlockAchKey = "ach_carnage", UnlockReqText = "Испытание: Кровавая Жатва (25 врагов за забег)", ChangesModel = true, BodyColor = Rgb(75, 85, 110), ArmorColor = Rgb(235, 195, 65), VisorColor = Rgb(85, 245, 255) });

            SetupCharLoadout("ahri",
                "Девятихвостая лиса-вастайи из Первозданной Ионии (League of Legends). Плетёт духовную магию, очаровывает врагов и стремительно порхает по полю боя.",
                "Кража Сущности [9/9]", "Каждое попадание умениями копит заряды Сущности. На 9 зарядах Ари исцеляется и получает заряд Рывка.",
                new SkillDef { Name = "Парные Сферы Духа", Cd = 0.28f, Desc = "[ЛКМ / Q] Выпускает 2 быстрые самонаводящиеся духовные сферы с вампиризмом." },
                new SkillDef { Name = "Пылающий Девятихвост (5 огоньков)", Cd = 4.2f, Desc = "[W / СКМ] Призывает сразу 5 духовных огоньков Лисьего Огня и ускоряет бег на +40%." },
                new SkillDef { Name = "Астральный Прыжок Лисы", Cd = 3.5f, Desc = "[Shift] Высокий акробатический прыжок с +30 щита и выпуском 4 духовных разрядов." },
                new SkillDef { Name = "Парад Девяти Хвостов (3 Сердца)", Cd = 9.5f, Desc = "[R] Выпускает веер из 3 сердец Очарования и полностью восстанавливает все 3 заряда Рывка!" },
                new SkinDef { Name = "Полуночная Ари", Desc = "Тёмно-фиолетовое облачение зимней ночи с серебристыми хвостами и лунной сферой.", UnlockAchKey = "ach_hoarder", UnlockReqText = "Достижение: Коллекционер (6 предметов)", BodyColor = Rgb(95, 52, 165), ArmorColor = Rgb(230, 235, 250), VisorColor = Rgb(185, 135, 255) },
                new SkinDef { Name = "Дух Цветения Ари [Модель]", Desc = "Сакуровое кимоно Духа Цветения, золотая диадема-рожки и парящий фамильяр-лисёнок!", UnlockAchKey = "ach_arsenal", UnlockReqText = "Испытание: Вооружён до зубов (12 предметов)", ChangesModel = true, BodyColor = Rgb(255, 130, 195), ArmorColor = Rgb(245, 248, 255), VisorColor = Rgb(110, 240, 255) });

            // =========================================================================================
            // 21. ЛАППЛАНД (ARKNIGHTS) + 3-Й СКИН: АЛЬТ. ВЕРСИЯ «ЛАППЛАНД ДЕКАДЕНЦА» (8 ОТДЕЛЬНЫХ СКИЛЛОВ)
            // =========================================================================================
            SetupCharLoadout("lappland",
                "Изгнанная наследница мафиозной семьи Сальваторе из Сиракуз (Arknights). В 3-м слоте скина переключается на свою альтернативную версию «Лаппланд Декаденца» с новой 3D-моделью и 8 новыми скиллами!",
                "Духовное Разрушение (Безмолвие)", "Удары Лаппланд накладывают Безмолвие, снижают защиту врагов и наносят бонусный магический урон (Arts).",
                new SkillDef { Name = "Вихрь Крови Сиракуз", Cd = 0.22f, Desc = "[ЛКМ Альт.] Двойные перекрёстные волны мечей с вампиризмом и повышенным шансом крита." },
                new SkillDef { Name = "Казнь Семьи Сальваторе", Cd = 3.2f, Desc = "[ПКМ Альт.] Выпускает 3 огромных пробивных креста серебряного света с оглушением." },
                new SkillDef { Name = "Призрачный Шаг Волчицы", Cd = 3.2f, Desc = "[Shift Альт.] Мгновенная телепортация за спину врагу с +40 щита и взрывом Искусств." },
                new SkillDef { Name = "Кровавая Луна Сиракуз", Cd = 8.5f, Desc = "[R Альт.] Вызывает 8 падающих с неба гигантских мечей и ускоряет атаку Лаппланд на 50%!" },
                new SkinDef { Name = "Утончённый Кошмар", Desc = "Парадный багрово-золотой костюм серии Bloodline of Combat.", UnlockAchKey = "ach_slayer", UnlockReqText = "Достижение: Истребитель (12 монстров)", BodyColor = Rgb(195, 38, 52), ArmorColor = Rgb(242, 238, 245), VisorColor = Rgb(255, 205, 65) },
                new SkinDef
                {
                    Name = "Лаппланд Декаденца [Альт. Версия]",
                    Desc = "Альтернативная версия Lappland the Decadenza (Caster): маскарадное пальто, парящие призрачные волки Сиракуз и совершенно новый набор из 8 навыков!",
                    UnlockedByDefault = false,
                    UnlockAchKey = "ach_persona_lappland_decadenza",
                    UnlockReqText = "Играя за Лаппланд: Истребите 16 врагов за 14 сек. или казните 3 Элитных врагов под Безмолвием",
                    ChangesModel = true,
                    BodyColor = Rgb(28, 26, 38),
                    ArmorColor = Rgb(215, 175, 65),
                    VisorColor = Rgb(185, 85, 255),
                    CustomSubtitle = "Лаппланд Декаденца [АЛЬТ. ВЕРСИЯ | ПРИЗРАЧНЫЕ ВОЛКИ СИРАКУЗ]",
                    CustomPassiveName = "Вожак Стаи Декаденцы",
                    CustomPassiveDesc = "Атаки призывают парящих Призрачных Волков (Signori dei Lupi), которые преследуют и терзают врагов магическим уроном.",
                    CustomSkillVariants = BuildCustomSkinSkillSet(
                        "Лаппланд Декаденца",
                        new SkillDef { Name = "Призрачная Стая (Feral Pack)", Cd = 0.24f, Desc = "[ЛКМ Декаденца] Направляет парящих призрачных волков и золотые клинки, разрывающие врагов на дистанции." },
                        new SkillDef { Name = "Багровый Реквием Волков", Cd = 0.28f, Desc = "[ЛКМ Альт. Декаденца] Выпускает сразу 3 самонаводящихся призрачных волчьих черепа с взрывом по площади!" },
                        new SkillDef { Name = "Печать Молчания Карнавала", Cd = 3.4f, Desc = "[ПКМ Декаденца] Стягивает врагов в магический водоворот Декаденцы, накладывая страх и 300% урона." },
                        new SkillDef { Name = "Челюсти Синьоров Волка", Cd = 3.6f, Desc = "[ПКМ Альт. Декаденца] Гигантская призрачная пасть волка смыкается на врагах, исцеляя Лаппланд и давая +45 щита!" },
                        new SkillDef { Name = "Вальс Маскарада", Cd = 3.6f, Desc = "[Shift Декаденца] Грациозный полёт сквозь тени Сиракуз (+40 щита и 3 волчьих духа вокруг)." },
                        new SkillDef { Name = "Шаг Сквозь Чистилище", Cd = 3.2f, Desc = "[Shift Альт. Декаденца] Телепортация с оставлением взрывной иллюзии и +50% скорости бега." },
                        new SkillDef { Name = "Гранд-Финал: Il Silenzio!", Cd = 9.5f, Desc = "[R Декаденца] Открывает купол Карнавала Декаденцы: шквал призрачных волков и золотых мечей по всей арене!" },
                        new SkillDef { Name = "Апофеоз Сиракуз (Вечная Охота)", Cd = 10.0f, Desc = "[R Альт. Декаденца] Призывает 2 огромных бессмертных Волчьих Фамильяра и обрушивает 10 лучей Искусств!" })
                });

            // =========================================================================================
            // 22. W (ARKNIGHTS) + 3-Й СКИН: АЛЬТ. ВЕРСИЯ «ВИШАДЕЛЬ / WIŠ'ADEL» (8 ОТДЕЛЬНЫХ СКИЛЛОВ)
            // =========================================================================================
            SetupCharLoadout("w_arknights",
                "Легендарная наёмница-сарказка из Каздела (Arknights). В 3-м слоте скина переключается на свою королевскую альт. версию «Вишадель (Wiš'adel)» с установками «Тень Восставших» и 8 новыми скиллами!",
                "Засада и Тактический Подрыв", "Враги, задетые взрывами или оглушением W, получают на 25% больше урона от всех источников.",
                new SkillDef { Name = "Кассетный Гранатомёт", Cd = 0.42f, Desc = "[ЛКМ Альт.] Выпускает гранату, распадающуюся на 3 прыгающие суб-гранаты." },
                new SkillDef { Name = "Король Пик (Тройное Минное Поле)", Cd = 4.0f, Desc = "[ПКМ Альт.] Разбрасывает веером сразу 3 клейкие мины C4 с увеличенным радиусом взрыва!" },
                new SkillDef { Name = "Дымовой Камуфляж Наёмницы", Cd = 3.6f, Desc = "[Shift Альт.] Взрывной отскок с получением невидимости, +45 щита и ускорения." },
                new SkillDef { Name = "Симфония Разрушения (Мега-D12)", Cd = 9.0f, Desc = "[R Альт.] Мгновенная ковровая бомбардировка из 10 зарядов C4 с цепной детонацией!" },
                new SkinDef { Name = "Фуга (Fugue)", Desc = "Элегантный парадный костюм серии EPOQUE с золотым гранатомётом.", UnlockAchKey = "ach_boss", UnlockReqText = "Достижение: Убийца Титанов (Победа над Боссом)", BodyColor = Rgb(235, 232, 240), ArmorColor = Rgb(185, 34, 48), VisorColor = Rgb(255, 215, 65) },
                new SkinDef
                {
                    Name = "Вишадель (Wiš'adel) [Альт. Версия]",
                    Desc = "Альтернативная версия Wiš'adel: парящие артиллерийские пилоны «Тень Восставших (Revenant's Shadow)», королевский плащ Каздела и 8 новых навыков!",
                    UnlockedByDefault = false,
                    UnlockAchKey = "ach_persona_w_wisadel",
                    UnlockReqText = "Играя за W: Нанесите 450+ урона одним взрывом или сокрушите Босса быстрее чем за 30 секунд",
                    ChangesModel = true,
                    BodyColor = Rgb(225, 38, 52),
                    ArmorColor = Rgb(245, 242, 250),
                    VisorColor = Rgb(255, 215, 55),
                    CustomSubtitle = "Вишадель (Wiš'adel) [АЛЬТ. ВЕРСИЯ | ПОВЕЛИТЕЛЬНИЦА РЕВЕНАНТОВ]",
                    CustomPassiveName = "Тень Восставших (Revenant's Shadow)",
                    CustomPassiveDesc = "Оружие предков Каздела оставляет на врагах взрывные метки Душ и автоматически обстреливает цели вокруг.",
                    CustomSkillVariants = BuildCustomSkinSkillSet(
                        "Вишадель",
                        new SkillDef { Name = "Залп Предков Каздела", Cd = 0.34f, Desc = "[ЛКМ Вишадель] Мощный выстрел из тяжёлого орудия с последующим вторичным взрывом Души Ревенанта." },
                        new SkillDef { Name = "Сдвоенный Импульс Ревенанта", Cd = 0.30f, Desc = "[ЛКМ Альт. Вишадель] Выпускает 2 самонаводящихся снаряда багровой энергии предков подряд!" },
                        new SkillDef { Name = "Призыв Тени Восставших (Пилон)", Cd = 4.0f, Desc = "[ПКМ Вишадель] Устанавливает автономный артиллерийский пилон Каздела, обстреливающий и оглушающий врагов!" },
                        new SkillDef { Name = "Багровый Обелиск Душ", Cd = 3.8f, Desc = "[ПКМ Альт. Вишадель] Вызывает взрывной обелиск, стягивающий врагов и дающий Вишадель +50 щита." },
                        new SkillDef { Name = "Крылья Короны Каздела", Cd = 3.6f, Desc = "[Shift Вишадель] Реактивный взлёт с камуфляжем и сбросом 3 кассетных бомб под себя!" },
                        new SkillDef { Name = "Фазовый Сдвиг Сарказов", Cd = 3.4f, Desc = "[Shift Альт. Вишадель] Мгновенный рывок с перезарядкой орудий и исцелением 20% HP." },
                        new SkillDef { Name = "Сумерки Взрывного Рассвета (Explosive Dawn)", Cd = 10.0f, Desc = "[R Вишадель] Взлетает в воздух и обрушивает 6 ядерных залпов Ревенанта по всей карте!" },
                        new SkillDef { Name = "Приговор Королей Сарказов", Cd = 9.5f, Desc = "[R Альт. Вишадель] Призывает 3 пилона «Тень Восставших» и выжигает арену орбитальным лучом Каздела!" })
                });

            // =========================================================================================
            // 23. ГОЛД ШИП (UMAMUSUME: PRETTY DERBY) — ИГРА ОТ СКОРОСТИ БЕГА И РАЗБЕГА
            // =========================================================================================
            SetupCharLoadout("gold_ship",
                "Самая эксцентричная и непредсказуемая девушка-пони Академии Трейсен (Umamusume: Pretty Derby). Вся её боевая мощь напрямую зависит от текущей скорости бега и набранного разбега!",
                "Непотопляемый Разгон (Speed Scaling)", "Во время бега Голд Шип набирает Разгон (до +100% скорости). Чем быстрее она движется в момент удара, тем сильнее урон, радиус ударной волны и бонусный щит!",
                new SkillDef { Name = "Таран Золотого Корабля", Cd = 0.24f, Desc = "[ЛКМ Альт.] Широкий удар на бегу с золотыми ударными волнами, наносящий до x3.2 урона на полном ходу!" },
                new SkillDef { Name = "Торпедный Дропкик в Прыжке!", Cd = 3.2f, Desc = "[ПКМ Альт.] Стремительный полёт вперёд ногами (дальность и взрыв растут от скорости движения)!" },
                new SkillDef { Name = "Второе Дыхание Арима Кинен", Cd = 3.8f, Desc = "[Shift Альт.] Мгновенно набирает 100% Разгона, снимает замедления и восстанавливает 25% HP!" },
                new SkillDef { Name = "Врата Уорп-Голдшипа (Якорный Шторм)", Cd = 8.5f, Desc = "[R Альт.] Открывает золотые порталы и обрушивает 5 корабельных якорей и ракет по врагам вокруг!" },
                new SkinDef { Name = "Курортный Капитан", Desc = "Бело-золотая капитанская форма Голд Шип с солнцезащитными очками.", UnlockAchKey = "ach_traveler", UnlockReqText = "Достижение: Исследователь миров (2-й этап)", BodyColor = Rgb(245, 246, 252), ArmorColor = Rgb(35, 65, 145), VisorColor = Rgb(255, 215, 55) },
                new SkinDef { Name = "Легенда Гран-При G1 [Модель]", Desc = "Золотая корона Чемпиона Тройной Короны, капитанский плащ и золотой якорь за спиной!", UnlockAchKey = "ach_carnage", UnlockReqText = "Испытание: Кровавая Жатва (25 врагов за забег)", ChangesModel = true, BodyColor = Rgb(235, 185, 45), ArmorColor = Rgb(185, 28, 42), VisorColor = Rgb(255, 245, 130) });

            // =========================================================================================
            // 24. АЛЕКСАНДР ПУШНОЙ (ГАЛИЛЕО)
            // =========================================================================================
            SetupCharLoadout("pushnoy",
                "Легендарный телеведущий программы «Галилео», физик и рок-музыкант. Сокрушает монстров убойными метал-риффами на электрогитаре и безумными научными экспериментами в прямом эфире!",
                "Наука и Рок-н-Ролл!", "Каждое применение научного эксперимента усиливает следующий гитарный рифф цепными молниями и даёт защитный барьер.",
                new SkillDef { Name = "Ультразвуковой Даунстрок", Cd = 0.22f, Desc = "[ЛКМ Альт.] Скоростной чёс на электрогитаре: выпускает 3 широкие звуковые волны с оглушением!" },
                new SkillDef { Name = "Неньютоновская Жидкость + Азот", Cd = 3.2f, Desc = "[ПКМ Альт.] Создаёт взрывную лужу неньютоновской жидкости, замораживающую врагов и дающую Пушному +50 щита!" },
                new SkillDef { Name = "Турбо-Огнетушитель X2", Cd = 3.4f, Desc = "[Shift Альт.] Сверхдальний пролёт на двух огнетушителях с ледяным взрывом на старте и финише!" },
                new SkillDef { Name = "Мегаваттный Концерт «ГАЛИЛЕО!»", Cd = 9.0f, Desc = "[R Альт.] Вызывает грозовой шторм из 8 разрядов молний Теслы и усиливает урон на +40%!" },
                new SkinDef { Name = "Лабораторный Профессор", Desc = "Белый научный халат поверх рок-футболки и защитные очки для опасных опытов.", UnlockAchKey = "ach_slayer", UnlockReqText = "Достижение: Истребитель (12 монстров)", BodyColor = Rgb(242, 245, 252), ArmorColor = Rgb(225, 95, 32), VisorColor = Rgb(85, 245, 255) },
                new SkinDef { Name = "Повелитель Молний Теслы [Модель]", Desc = "Двухгрифовая золотая электрогитара, ранец с катушками Теслы за спиной и неоновые очки!", UnlockAchKey = "ach_arsenal", UnlockReqText = "Испытание: Вооружён до зубов (12 предметов)", ChangesModel = true, BodyColor = Rgb(35, 38, 48), ArmorColor = Rgb(245, 195, 55), VisorColor = Rgb(105, 245, 255) });

            // =========================================================================================
            // 25. МИШКА ФРЕДДИ (FIVE NIGHTS AT FREDDY'S)
            // =========================================================================================
            SetupCharLoadout("freddy",
                "Главный аниматроник пиццерии Freddy Fazbear's Pizza. В бою использует тяжёлый микрофон, смертоносный Скример и отключение электричества под мелодию «Марша Тореадора»!",
                "Ночной Протокол Fazbear", "В темноте и после Скримера Фредди получает повышенную броню, а его атаки пугают и замедляют врагов.",
                new SkillDef { Name = "Горячая Пицца Пепперони!", Cd = 0.26f, Desc = "[ЛКМ Альт.] Мечет вращающиеся огненные пиццы-диски, рикошетящие между врагами!" },
                new SkillDef { Name = "Золотой Укус 1983-го", Cd = 3.5f, Desc = "[ПКМ Альт.] Телепортационный скример с круговым звуковым взрывом на 380% урона!" },
                new SkillDef { Name = "Сбой Камер Наблюдения", Cd = 4.2f, Desc = "[Shift Альт.] Мгновенный скачок сквозь помехи камер с получением +60 щита и невидимости." },
                new SkillDef { Name = "Пробуждение ГОЛДЕН ФРЕДДИ (IT'S ME)", Cd = 9.5f, Desc = "[R Альт.] Вызывает гигантскую золотую проекцию Golden Freddy, сокрушающую всех врагов вокруг психическим взрывом!" },
                new SkinDef { Name = "Фредбер (Fredbear)", Desc = "Золотисто-жёлтый корпус оригинального аниматроника с фиолетовым цилиндром и бабочкой.", UnlockAchKey = "ach_boss", UnlockReqText = "Достижение: Убийца Титанов (Победа над Боссом)", BodyColor = Rgb(235, 190, 52), ArmorColor = Rgb(135, 45, 185), VisorColor = Rgb(185, 105, 255) },
                new SkinDef { Name = "Глэмрок Фредди [Модель]", Desc = "Неоновый панк-рок облик Glamrock Freddy: наплечники со шипами, очки-звёзды и голубой корпус!", UnlockAchKey = "ach_boss_hunter", UnlockReqText = "Испытание: Гроза Стражей (3 Босса)", ChangesModel = true, BodyColor = Rgb(235, 125, 42), ArmorColor = Rgb(55, 205, 245), VisorColor = Rgb(255, 75, 165) });

            // =========================================================================================
            // 26. СТИВ (MINECRAFT) — ДИНАМИЧЕСКАЯ ДОБЫЧА КАРТЫ (ЯМЫ В ПОЛУ, СТЕНЫ, АЛТАРИ) + ВЕРСТАК
            // =========================================================================================
            SetupCharLoadout("steve",
                "Легендарный покоритель кубического мира Minecraft. Способен буквально ломать киркой пол (создавая настоящие 3D-ямы!), стены, деревья и алтари, а затем крафтить на Верстаке мощную экипировку и предметы!",
                "Шахтёрское Ремесло и Верстак [E]", "Добывает Дерево, Камень, Железо и Алмазы прямо из геометрии карты. После победы над боссом и зарядки портала рядом появляется Верстак для спокойного крафта!",
                new SkillDef { Name = "Незеритовый Топор + Арбалет", Cd = 0.30f, Desc = "[ЛКМ Альт.] Сокрушительный удар топором по площади + тройной залп фейерверками из арбалета!" },
                new SkillDef { Name = "Буровая Кирка «Эффективность V»", Cd = 0.65f, Desc = "[ПКМ Альт.] Ускоренная копка пола/стен/алтарей с удвоенной добычей ресурсов и ударной волной!" },
                new SkillDef { Name = "Полёт на Элитрах с Фейерверком", Cd = 3.6f, Desc = "[Shift Альт.] Реактивный взлёт в воздух на крыльях Элитрах с бомбардировкой врагов под собой!" },
                new SkillDef { Name = "Мега-ТНТ + Поставить Верстак", Cd = 5.5f, Desc = "[R Альт.] Ставит Верстак и бросает связку из 3 блоков ТНТ, вырывающих огромный кратер в полу!" },
                new SkinDef { Name = "Алмазный Стив", Desc = "Полный комплект сверкающей зачарованной алмазной брони.", UnlockAchKey = "ach_hoarder", UnlockReqText = "Достижение: Коллекционер (6 предметов)", BodyColor = Rgb(65, 225, 240), ArmorColor = Rgb(42, 145, 175), VisorColor = Rgb(175, 255, 255) },
                new SkinDef { Name = "Незеритовый Лорд и Херобрин [Модель]", Desc = "Тёмная незеритовая броня с золотой короной, крыльями Элитрами и светящимися белыми глазами Херобрина!", UnlockAchKey = "ach_stage3", UnlockReqText = "Испытание: Покоритель Глубин (3-й этап)", ChangesModel = true, BodyColor = Rgb(58, 52, 62), ArmorColor = Rgb(235, 185, 55), VisorColor = Rgb(255, 255, 255) });

            // =========================================================================================
            // 27. ДЖОТАРО КУДЖО (JOJO'S BIZARRE ADVENTURE)
            // =========================================================================================
            SetupCharLoadout("jotaro",
                "Непоколебимый герой Stardust Crusaders и обладатель непобедимого Стенда ближнего боя «Star Platinum». Сокрушает врагов шквалом «ORA ORA ORA!» и останавливает само время!",
                "Стенд: Star Platinum", "Star Platinum постоянно парит за плечом Джотаро, увеличивая скорость атаки и автоматически блокируя часть урона.",
                new SkillDef { Name = "Шквал «ORA!» + Подшипники", Cd = 0.20f, Desc = "[ЛКМ Альт.] Удары кулаками Star Platinum + дальнобойный щелчок стальными шариками с рикошетом!" },
                new SkillDef { Name = "Звёздный Раскол Земли", Cd = 3.2f, Desc = "[ПКМ Альт.] Star Platinum бьёт обоими кулаками в землю, поднимая каменную гряду и оглушая всех врагов!" },
                new SkillDef { Name = "Шаги Вне Времени (Time Skip)", Cd = 3.4f, Desc = "[Shift Альт.] Мгновенная телепортация в микро-остановке времени с +45 щита и ударом с разворота!" },
                new SkillDef { Name = "ZA WARUDO: 5 Секунд + Финальный ORA!", Cd = 11.5f, Desc = "[R Альт.] Останавливает время на 5.0 сек. и выпускает сокрушительную серию из 12 ударов Star Platinum!" },
                new SkinDef { Name = "Океан Камня (Часть 6)", Desc = "Фиолетово-золотой плащ Джотаро из Stone Ocean с эмблемами звезды.", UnlockAchKey = "ach_boss", UnlockReqText = "Достижение: Убийца Титанов (Победа над Боссом)", BodyColor = Rgb(88, 48, 138), ArmorColor = Rgb(235, 195, 55), VisorColor = Rgb(95, 235, 255) },
                new SkinDef { Name = "Морио (Часть 4) + Star Platinum Over Heaven [Модель]", Desc = "Белоснежный плащ Джотаро из Diamond is Unbreakable и бело-золотой Стенд Star Platinum Over Heaven!", UnlockAchKey = "ach_boss_hunter", UnlockReqText = "Испытание: Гроза Стражей (3 Босса)", ChangesModel = true, BodyColor = Rgb(242, 244, 252), ArmorColor = Rgb(245, 195, 65), VisorColor = Rgb(115, 245, 255) });

            // =========================================================================================
            // 28. ВАЙПЕР (DEADLOCK)
            // =========================================================================================
            SetupCharLoadout("vyper",
                "Неуловимая рептилия-преступница из Нью-Йорка (Deadlock). Непрерывно скользит по полю боя, поливая врагов свинцом и нейротоксинами, а её ульта превращает толпы монстров в камень!",
                "Змеиное Скольжение (Slither)", "Во время движения и скольжения Вайпер получает +30% к скорострельности и +25% к уклонению/броне.",
                new SkillDef { Name = "Рикошетящие Клыки (Разрывной ПП)", Cd = 0.15f, Desc = "[ЛКМ Альт.] Токсичные пули рикошетят в соседних врагов и отравляют их!" },
                new SkillDef { Name = "Кислотный Выброс Кобры", Cd = 3.2f, Desc = "[ПКМ Альт.] Выплёскивает широкий веер кислоты, плавящий броню и наносящий урон от недостающего HP!" },
                new SkillDef { Name = "Вихревой Бросок Гадюки", Cd = 3.0f, Desc = "[Shift Альт.] Сверхбыстрый зигзагообразный слайд сквозь врагов с отравлением и +55 щита." },
                new SkillDef { Name = "Взгляд Медузы (Каменный Шторм)", Cd = 9.0f, Desc = "[R Альт.] Выпускает 3 окаменяющие болы веером, мгновенно превращая всю группу врагов в статуи!" },
                new SkinDef { Name = "Неоновая Гадюка", Desc = "Ярко-бирюзовая чешуя и фиолетовый плащ ночного синдиката.", UnlockAchKey = "ach_slayer", UnlockReqText = "Достижение: Истребитель (12 монстров)", BodyColor = Rgb(42, 215, 185), ArmorColor = Rgb(95, 45, 155), VisorColor = Rgb(255, 225, 65) },
                new SkinDef { Name = "Золотая Королевская Кобра [Модель]", Desc = "Золотой капюшон королевской кобры, корона и спаренные плазменные ПП!", UnlockAchKey = "ach_carnage", UnlockReqText = "Испытание: Кровавая Жатва (25 врагов за забег)", ChangesModel = true, BodyColor = Rgb(235, 185, 48), ArmorColor = Rgb(36, 38, 48), VisorColor = Rgb(115, 255, 95) });

            // =========================================================================================
            // 29. ХЕЙЗ (DEADLOCK)
            // =========================================================================================
            SetupCharLoadout("haze",
                "Специальный агент песочных снов бюро OSIC (Deadlock). Каждое попадание накапливает на цели стаки Фиксации, усыпляет опасных врагов кинжалом и расстреливает всё вокруг в «Танце Пуль»!",
                "Фиксация (Fixation)", "Каждое попадание по врагу накладывает стак Фиксации (до 20). При достижении 20 стаков происходит Взрыв Снов с огромным уроном!",
                new SkillDef { Name = "Калибр Кошмаров (Пробивные ПП)", Cd = 0.16f, Desc = "[ЛКМ Альт.] Выстрелы накладывают по 2 стака Фиксации сразу и прошивают врагов насквозь!" },
                new SkillDef { Name = "Веер Сонных Кинжалов (3 шт.)", Cd = 3.8f, Desc = "[ПКМ Альт.] Бросает веером 3 усыпляющих кинжала снов, оглушающих группу врагов!" },
                new SkillDef { Name = "Шаг Песочного Человека", Cd = 3.6f, Desc = "[Shift Альт.] Телепортация в облаке золотого песка с мгновенным взрывом стаков Фиксации вокруг!" },
                new SkillDef { Name = "Абсолютный Танец Пуль (Жатва Снов)", Cd = 9.0f, Desc = "[R Альт.] Расширенный вихрь стрельбы с неуязвимостью, вампиризмом и двойным уроном Фиксации!" },
                new SkinDef { Name = "Багровый Кошмар OSIC", Desc = "Чёрно-алый плащ старшего инспектора оккультного бюро.", UnlockAchKey = "ach_hoarder", UnlockReqText = "Достижение: Коллекционер (6 предметов)", BodyColor = Rgb(185, 34, 52), ArmorColor = Rgb(28, 28, 36), VisorColor = Rgb(255, 215, 75) },
                new SkinDef { Name = "Архонт Вечных Снов [Модель]", Desc = "Золотая корона-нимб Песочного Человека, парящие часы снов и золотые пистолеты-пулемёты!", UnlockAchKey = "ach_arsenal", UnlockReqText = "Испытание: Вооружён до зубов (12 предметов)", ChangesModel = true, BodyColor = Rgb(242, 240, 248), ArmorColor = Rgb(235, 185, 55), VisorColor = Rgb(110, 235, 255) });

            // =========================================================================================
            // 30. ТРИО ИЗ DEVIL MAY CRY (1: ДАНТЕ | 2: НЕРО | 3: ВЕРГИЛИЙ — У КАЖДОГО 8 СКИЛЛОВ!)
            // =========================================================================================
            SetupCharLoadout("dmc_trio",
                "Легендарное трио охотников на демонов из Devil May Cry! Переключайте слоты скинов, чтобы менять персонажа и все 8 способностей: Слот 1 — ДАНТЕ (Смена стилей [1-4]!), Слот 2 — НЕРО (Тайминг MAX-Act [Q]!), Слот 3 — ВЕРГИЛИЙ (Шкала Концентрации и Just-Cut)!",
                "Кровь Спарды: Смена 4 Стилей [1-4]", "Нажимайте [1] Trickster, [2] Swordmaster, [3] Gunslinger или [4] Royal Guard в реальном времени, меняя бонусы и накапливая шкалу Royal Release!",
                new SkillDef { Name = "Дьявольский Меч Данте + Койот-А", Cd = 0.25f, Desc = "[ЛКМ Альт. Данте] Призывает парящие призрачные клинки DSD и бьёт картечью из дробовика!" },
                new SkillDef { Name = "Танец Макабр + Багровый Дождь", Cd = 3.4f, Desc = "[ПКМ Альт. Данте] Серия молниеносных выпадов с круговым расстрелом из Ebony & Ivory!" },
                new SkillDef { Name = "Королевский Страж (Royal Block & Release)", Cd = 3.6f, Desc = "[Shift Альт. Данте] Парирует урон в шкалу Royal Gauge, а при повторном нажатии высвобождает Royal Release!" },
                new SkillDef { Name = "Греховный Дьявольский Облик: Суд SDT!", Cd = 9.5f, Desc = "[R Альт. Данте] Превращается в Sin Devil Trigger и разрывает пространство демоническим вихрем The Luce!" },
                new SkinDef
                {
                    Name = "Неро (Nero) [Слот 2]",
                    Desc = "Молодой охотник на демонов Неро из Фортуны: меч «Алая Королева (Red Queen)» с зажиганием Exceed [Q], револьвер «Синяя Роза» и механический протез Devil Breaker!",
                    UnlockedByDefault = false,
                    UnlockAchKey = "ach_persona_dmc_nero",
                    UnlockReqText = "Играя за Трио DMC: Победите Босса Телепорта, применив навык стиля/рывка [Shift] не менее 8 раз за бой",
                    ChangesModel = true,
                    BodyColor = Rgb(38, 68, 145),
                    ArmorColor = Rgb(185, 34, 48),
                    VisorColor = Rgb(85, 225, 255),
                    CustomSubtitle = "Неро (Nero) [СЛОТ 2 | АЛАЯ КОРОЛЕВА, MAX-ACT [Q] И DEVIL BREAKER]",
                    CustomPassiveName = "Зажигание Exceed и MAX-Act [Q]",
                    CustomPassiveDesc = "Нажмите [Q] сразу после взмаха меча (окно 0.26 сек.), чтобы поймать MAX-Act и мгновенно зарядить все 3/3 бака огненного двигателя Exceed!",
                    CustomSkillVariants = BuildCustomSkinSkillSet(
                        "Неро (DMC)",
                        new SkillDef { Name = "Алая Королева + Синяя Роза", Cd = 0.23f, Desc = "[ЛКМ Неро] Рубящие удары мечом с огненным двигателем Exceed + двуствольный выстрел из револьвера Blue Rose." },
                        new SkillDef { Name = "MAX-Act Стрик + Заряженный Выстрел 3", Cd = 0.26f, Desc = "[ЛКМ Альт. Неро] Пылающий круговой разрез Red Queen и разрывной выстрел Blue Rose с задержанной детонацией!" },
                        new SkillDef { Name = "Бич Дьявола: Увертюра (Overture)", Cd = 3.2f, Desc = "[ПКМ Неро] Мощная электрическая ладонь протеза Devil Breaker, оглушающая и отбрасывающая врагов!" },
                        new SkillDef { Name = "Реактивный Кулак (Punch Line)", Cd = 3.4f, Desc = "[ПКМ Альт. Неро] Запускает самонаводящуюся ракетную руку-протез, взрывающуюся в гуще врагов!" },
                        new SkillDef { Name = "Трос-Захват (Wire Snatch) + Гербера", Cd = 3.4f, Desc = "[Shift Неро] Стремительный рывок к цели на демоническом тросе с ударной волной лепестков Gerbera (+40 щита)!" },
                        new SkillDef { Name = "Верховой Полёт на Протезе", Cd = 3.5f, Desc = "[Shift Альт. Неро] Пролетает верхом на реактивной руке Punch Line, сбивая врагов с ног!" },
                        new SkillDef { Name = "Дьявольский Облик: Buster Showdown!", Cd = 9.5f, Desc = "[R Неро] Пробуждает призрачные крылья Неро и обрушивает гигантские демонические кулаки на всех врагов вокруг!" },
                        new SkillDef { Name = "Поломка Протеза: Луч Герберы GP01", Cd = 9.0f, Desc = "[R Альт. Неро] Перегружает протез Devil Breaker, выжигая арену колоссальным отражающимся лазером!" })
                },
                new SkinDef
                {
                    Name = "Вергилий (Vergil) [Слот 3]",
                    Desc = "Альфа и Омега — старший брат-близнец Данте в синем плаще с пространственной катаной «Ямато (Yamato)» и Призрачными Клинками!",
                    UnlockedByDefault = false,
                    UnlockAchKey = "ach_persona_dmc_vergil",
                    UnlockReqText = "Играя за Трио DMC: Истинная Мотивация — победите Босса быстрее чем за 25 сек. или одолейте Митрикса",
                    ChangesModel = true,
                    BodyColor = Rgb(32, 65, 138),
                    ArmorColor = Rgb(235, 238, 248),
                    VisorColor = Rgb(85, 215, 255),
                    CustomSubtitle = "Вергилий (Vergil) [СЛОТ 3 | КАТАНА ЯМАТО И РАЗРЕЗ ПРАВОСУДИЯ]",
                    CustomPassiveName = "Концентрация (Motivation) и Just-Cut",
                    CustomPassiveDesc = "Хладнокровная точность копит шкалу Концентрации (0-100%, до +35% урона и радиуса Ямато), а ритмичное нажатие [ЛКМ] выпускает идеальный тройной Just Judgement Cut!",
                    CustomSkillVariants = BuildCustomSkinSkillSet(
                        "Вергилий (DMC)",
                        new SkillDef { Name = "Катана Ямато + Разрез Правосудия", Cd = 0.21f, Desc = "[ЛКМ Вергилий] Молниеносные взмахи Ямато + пространственная сфера Judgement Cut прямо по цели!" },
                        new SkillDef { Name = "Беовульф + Грань Силы (Mirage Edge)", Cd = 0.24f, Desc = "[ЛКМ Альт. Вергилий] Сокрушительные удары светом Беовульфа и парные комбо Ямато + Mirage Edge!" },
                        new SkillDef { Name = "Шторм Призрачных Клинков (Spiral Swords)", Cd = 3.4f, Desc = "[ПКМ Вергилий] Окружает Вергилия кольцом лазурных клинков (+35 щита) и выпускает их во врагов!" },
                        new SkillDef { Name = "Тяжёлый Дождь Клинков (Blistering Swords)", Cd = 3.5f, Desc = "[ПКМ Альт. Вергилий] Обрушивает с неба ливень из 8 пространственных мечей, пригвождающих врагов к земле!" },
                        new SkillDef { Name = "Стремительный Разрез (Rapid Slash)", Cd = 3.2f, Desc = "[Shift Вергилий] Телепортационный прорыв сквозь врагов, оставляющий за спиной серию задержанных разрезов!" },
                        new SkillDef { Name = "Тёмный Убийца (Air Trick + Восходящий Дракон)", Cd = 3.2f, Desc = "[Shift Альт. Вергилий] Мгновенная телепортация к цели с восходящим драконьим апперкотом и +45 щита!" },
                        new SkillDef { Name = "ФИНАЛЬНЫЙ РАЗРЕЗ ПРАВОСУДИЯ (Judgement Cut End!)", Cd = 10.0f, Desc = "[R Вергилий] «I AM THE STORM THAT IS APPROACHING!» — рассекает реальность по всей арене десятками пространственных разрезов!" },
                        new SkillDef { Name = "Двойник SDT + Ад на Земле (Hell on Earth)", Cd = 10.0f, Desc = "[R Альт. Вергилий] Призывает теневого Двойника (Doppelganger) и бьёт в землю кулаком Sin Devil Trigger!" })
                });

            // =========================================================================================
            // 31. СЕКРЕТНЫЙ ЭКСКЛЮЗИВНЫЙ ПЕРСОНАЖ: БРИГАДНЫЙ ГЕНЕРАЛ (SANABI) + МАРИ
            // (Доступен только игрокам из белого списка ProgressData.SanabiAllowedUsers: lddrakon, EinKamerad)
            // =========================================================================================
            SetupCharLoadout("sanabi",
                "Легендарный Бригадный Генерал Чосона (Проект №17) и его юная напарница-хакер Мари из киберпанк-мегаполиса Маго (SANABI). Сражается исполинской механической Рукой-Крюком на тросе: набирает колоссальный Импульс скорости в полёте, притягивается к врагам и поверхностям, отталкивается от целей в воздухе и уничтожает целые армии за доли секунды!",
                "Кинетический Маятник и Навигатор Мари", "Полёты на крюке и движение разгоняют шкалу Импульса (до 200%), умножая урон всех ударов Руки-Крюка до x2.5 и сокращая перезарядку навыков при попадании!",
                new SkillDef { Name = "Казнь Военного Трибунала (Chain-Slam)", Cd = 0.24f, Desc = "[ЛКМ Альт.] Захватывает цель крюком, подбрасывает Генерала над ней и с размаху вбивает врага в землю с ударной волной!" },
                new SkillDef { Name = "Сквозной Гарпун «Заводной Механизм»", Cd = 2.4f, Desc = "[ПКМ Альт.] Выпускает раскалённый плазменный гарпун, прошивающий строй врагов насквозь и стягивающий их в одну точку перед рывком Генерала!" },
                new SkillDef { Name = "Реактивный Подброс Цепи (Sky-Vault)", Cd = 2.2f, Desc = "[Shift Альт.] Мгновенный вертикально-диагональный взлёт на цепи с гравитационным ударом вниз и +60 щита!" },
                new SkillDef { Name = "Финальная Гармоника: Взрыв Поезда Маго", Cd = 9.5f, Desc = "[R Альт.] Полная синхронизация Генерала и Мари: призывает кибер-разряд по всей карте, даёт неуязвимость, +100 щита и 200% Импульса!" },
                new SkinDef
                {
                    Name = "Призрак Мегаполиса Маго (Неон)",
                    Desc = "Ночной киберпанк-облик из залитого неоном Маго: угольный плащ, пурпурно-малиновое ядро Руки-Крюка и розовая голограмма Мари.",
                    UnlockedByDefault = true,
                    UnlockReqText = "Подарочный эксклюзив (Открыт сразу)",
                    BodyColor = Rgb(28, 26, 38),
                    ArmorColor = Rgb(58, 42, 72),
                    VisorColor = Rgb(255, 65, 165)
                },
                new SkinDef
                {
                    Name = "Высший Генерал Чосона [Мастер-Модель]",
                    Desc = "Парадный бело-золотой китель Бригадного Генерала Королевской Гвардии, плазменный венец, золотая Рука-Крюк и крылатый дрон Мари!",
                    UnlockedByDefault = true,
                    UnlockReqText = "Подарочный эксклюзив (Открыт сразу)",
                    ChangesModel = true,
                    BodyColor = Rgb(240, 242, 250),
                    ArmorColor = Rgb(235, 185, 55),
                    VisorColor = Rgb(75, 235, 255)
                });

            // =========================================================================================
            // 32. ДЖЕК-О ВАЛЕНТАЙН (GUILTY GEAR -STRIVE-)
            // =========================================================================================
            SetupCharLoadout("jack_o",
                "Искусственная форма жизни Валентайн, хранящая частицу души Арии Хейл (Guilty Gear -Strive-). Пинками шара запускает Слуг в цепной бильярд-рикошет, подхватывает и бросает их на [F], усиливает Командным Флагом и вбивает врагов с небес приёмом Forever Elysion Driver!",
                "Хоровод Слуг, Бросок [F] и Бильярд", "Каждый активный Слуга даёт +6% к скорости и броне. Пинки [ЛКМ] рикошетят миньонов друг от друга с ростом урона, а клавиша [F] позволяет подхватить ближайшего Слугу и метнуть навесом!",
                new SkillDef { Name = "Двойной Вихрь Тыквы (Shoot Command)", Cd = 0.34f, Desc = "[ЛКМ Альт.] Круговой пинок шаром на цепи вокруг себя, запускающий ближайших миньонов во врагов как живые снаряды!" },
                new SkillDef { Name = "Двойной Призыв Слуг (Twin Servants)", Cd = 4.0f, Desc = "[ПКМ Альт.] Спавнит сразу 2 маленьких Слуг (лимит до 5 шт.) с увеличенным радиусом предсмертного взрыва!" },
                new SkillDef { Name = "Королевский Штандарт Элизиона", Cd = 4.4f, Desc = "[Shift Альт.] Прыжок с золотым флагом: в точке приземления знамя одновременно баффает миньонов и на УРОН, и на ЗАЩИТУ (+50 щита)!" },
                new SkillDef { Name = "Сверхновая Forever Elysion Driver!", Cd = 10.0f, Desc = "[R Альт.] Насаживает врага на шар, взлетает ещё выше и при жёстком приземлении оставляет в кратере 2 взрывных Слуг!" },
                new SkinDef
                {
                    Name = "Карнавал Хэллоуина",
                    Desc = "Готический чёрно-тыквенный комбинезон с фиолетовыми ремнями и неоново-оранжевым свечением Железной Тыквы.",
                    UnlockAchKey = "ach_slayer",
                    UnlockReqText = "Достижение: Истребитель (12 монстров)",
                    BodyColor = Rgb(34, 30, 44),
                    ArmorColor = Rgb(245, 115, 32),
                    VisorColor = Rgb(255, 195, 45)
                },
                new SkinDef
                {
                    Name = "Крылья Арии и Задворков [Модель]",
                    Desc = "Сияющий бело-золотой наряд с восстановленным венцом-нимбом, крыльями света Задворков и коронованной Железной Тыквой!",
                    UnlockAchKey = "ach_boss_hunter",
                    UnlockReqText = "Испытание: Гроза Стражей (3 Босса)",
                    ChangesModel = true,
                    BodyColor = Rgb(248, 248, 255),
                    ArmorColor = Rgb(235, 185, 55),
                    VisorColor = Rgb(115, 255, 195)
                });
        }

        private static bool ColorClose(Color a, Color b, int tol = 42)
            => Math.Abs(a.R - b.R) <= tol && Math.Abs(a.G - b.G) <= tol && Math.Abs(a.B - b.B) <= tol;

        private static void ApplySkinColorRemap(Entity node, SurvivorConfig cfg, SkinDef skin)
        {
            foreach (var ch in node.Children)
            {
                if (ColorClose(ch.Color, cfg.BaseBodyColor, 45))
                    ch.Color = new Color(skin.BodyColor.R, skin.BodyColor.G, skin.BodyColor.B, ch.Color.A);
                else if (ColorClose(ch.Color, cfg.BaseArmorColor, 45))
                    ch.Color = new Color(skin.ArmorColor.R, skin.ArmorColor.G, skin.ArmorColor.B, ch.Color.A);
                else if (ColorClose(ch.Color, cfg.BaseVisorColor, 45))
                    ch.Color = new Color(skin.VisorColor.R, skin.VisorColor.G, skin.VisorColor.B, ch.Color.A);

                ApplySkinColorRemap(ch, cfg, skin);
            }
        }

        private static void ApplyMasterySkinGeometry(string charKey, Entity root, SurvivorParts parts, SkinDef skin)
        {
            var head = parts.Head ?? parts.Torso;
            var torso = parts.Torso;
            if (torso == null) return;

            switch (charKey)
            {
                case "commando":
                    // Сёгун UES: рогатый гребень кабуто на шлеме, пластинчатые наплечники содэ и плазменная катана на поясе
                    if (head != null)
                    {
                        new Entity(parent: head, model: "diamond", scale: new(0.46f, 0.28f, 0.10f), position: new(0, 0.34f, 0.24f), rotation: new(-12, 0, 0), color: Color.Gold, unlit: true);
                        foreach (int s in new[] { -1, 1 })
                            new Entity(parent: head, model: "diamond", scale: new(0.12f, 0.38f, 0.12f), position: new(s * 0.22f, 0.38f, 0.18f), rotation: new(0, 0, s * -28f), color: Color.Gold, unlit: true);
                    }
                    foreach (var arm in new[] { parts.LeftArm, parts.RightArm })
                        if (arm != null)
                            new Entity(parent: arm, model: "cube", scale: new(0.48f, 0.32f, 0.48f), position: new(0, 0.14f, 0), color: skin.ArmorColor);
                    var katana = new Entity(parent: torso, position: new(-0.38f, -0.28f, -0.05f), rotation: new(24, 0, 12));
                    new Entity(parent: katana, model: "cube", scale: new(0.06f, 0.08f, 1.15f), color: Color.Gold, unlit: true);
                    break;

                case "artificer":
                    // Верховный Серафим: 4 парящих золотых крыла-серафима и корона света
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.52f, 0.26f, 0.12f), position: new(0, 0.38f, 0.16f), color: Color.Gold, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                    {
                        new Entity(parent: torso, model: "diamond", scale: new(1.25f, 0.24f, 0.12f), position: new(s * 0.72f, 0.36f, -0.32f), rotation: new(0, s * -15f, s * 26f), color: Color.Gold, unlit: true);
                        new Entity(parent: torso, model: "diamond", scale: new(1.05f, 0.20f, 0.12f), position: new(s * 0.64f, 0.04f, -0.32f), rotation: new(0, s * -15f, s * -16f), color: skin.VisorColor, unlit: true);
                    }
                    break;

                case "huntress":
                    // Арктический Следопыт: волчьи ушки на капюшоне и кристаллическая корона
                    if (head != null)
                    {
                        foreach (int s in new[] { -1, 1 })
                            new Entity(parent: head, model: "diamond", scale: new(0.16f, 0.36f, 0.16f), position: new(s * 0.20f, 0.36f, 0.02f), rotation: new(-8, 0, s * -18f), color: skin.BodyColor);
                        new Entity(parent: head, model: "diamond", scale: new(0.24f, 0.24f, 0.14f), position: new(0, 0.34f, 0.24f), color: skin.VisorColor, unlit: true);
                    }
                    break;

                case "buzz":
                    // Император Зург: золотые рога на шлеме и высокий королевский воротник-мантия
                    if (head != null)
                    {
                        foreach (int s in new[] { -1, 1 })
                            new Entity(parent: head, model: "diamond", scale: new(0.16f, 0.48f, 0.16f), position: new(s * 0.26f, 0.42f, 0), rotation: new(10, 0, s * -26f), color: Color.Gold, unlit: true);
                    }
                    new Entity(parent: torso, model: "diamond", scale: new(0.95f, 0.36f, 0.56f), position: new(0, 0.44f, -0.10f), color: skin.BodyColor);
                    break;

                case "v1":
                    // V2 Высшая Машина: золотая корона-гребень на камере и дополнительные верхние крылья
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.38f, 0.28f, 0.52f), position: new(0, 0.26f, 0.08f), color: Color.Gold, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        new Entity(parent: torso, model: "diamond", scale: new(1.45f, 0.18f, 0.10f), position: new(s * 0.85f, 0.48f, -0.28f), rotation: new(0, 0, s * 32f), color: Color.Gold, unlit: true);
                    break;

                case "sol":
                    // Орденский Сол: золотой ореол Святого Ордена и белый рыцарский плащ
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.48f, 0.24f, 0.14f), position: new(0, 0.34f, 0.22f), color: Color.Gold, unlit: true);
                    new Entity(parent: torso, model: "diamond", scale: new(0.88f, 1.15f, 0.18f), position: new(0, -0.18f, -0.28f), rotation: new(14, 0, 0), color: skin.BodyColor);
                    break;

                case "gwen":
                    // Звёздная Хранительница: золотая корона, парящий фамильяр-звезда над плечом и крылья-банты
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.42f, 0.28f, 0.16f), position: new(0, 0.38f, 0.14f), color: Color.Gold, unlit: true);
                    var starFam = new Entity(parent: torso, position: new(-0.68f, 0.68f, 0.15f));
                    new Entity(parent: starFam, model: "sphere", scaleUniform: 0.28f, color: Rgb(255, 235, 115), unlit: true);
                    new Entity(parent: starFam, model: "diamond", scale: new(0.48f, 0.14f, 0.14f), color: Color.Gold, unlit: true);
                    parts.Tails.Add(starFam);
                    break;

                case "enforcer":
                    // Судья Мега-Сити: массивный золотой орлиный наплечник и гребень шлема
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.24f, 0.28f, 0.68f), position: new(0, 0.36f, 0.04f), color: Color.Gold, unlit: true);
                    if (parts.RightArm != null)
                        new Entity(parent: parts.RightArm, model: "diamond", scale: new(0.58f, 0.42f, 0.52f), position: new(0.08f, 0.18f, 0), color: Color.Gold, unlit: true);
                    break;

                case "scout":
                    // Лорд Седой Бороды: рогатый золотой шлем дварфа и рунный фонарь
                    if (head != null)
                    {
                        foreach (int s in new[] { -1, 1 })
                            new Entity(parent: head, model: "diamond", scale: new(0.18f, 0.44f, 0.18f), position: new(s * 0.32f, 0.24f, 0.04f), rotation: new(12, 0, s * -36f), color: Color.Gold, unlit: true);
                    }
                    new Entity(parent: torso, model: "diamond", scale: new(0.32f, 0.48f, 0.32f), position: new(0, 0.52f, -0.34f), color: skin.VisorColor, unlit: true);
                    break;

                case "m4a1":
                    // MOD3 Валькирия: механические крылья-антенны за спиной и золотой нимб
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.52f, 0.10f, 0.52f), position: new(0, 0.42f, 0), color: Color.Gold, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        new Entity(parent: torso, model: "diamond", scale: new(1.15f, 0.22f, 0.12f), position: new(s * 0.65f, 0.28f, -0.30f), rotation: new(0, 0, s * 24f), color: skin.VisorColor, unlit: true);
                    break;

                case "medic":
                    // Чумной Доктор и голубь Архимед: золотой клюв маски, цилиндр и парящий белый голубь над плечом
                    if (head != null)
                    {
                        new Entity(parent: head, model: "diamond", scale: new(0.22f, 0.20f, 0.52f), position: new(0, -0.04f, 0.38f), rotation: new(14, 0, 0), color: Color.Gold);
                        new Entity(parent: head, model: "cube", scale: new(0.46f, 0.34f, 0.46f), position: new(0, 0.36f, 0), color: Rgb(28, 30, 38));
                    }
                    var dove = new Entity(parent: torso, position: new(-0.62f, 0.58f, 0.12f));
                    new Entity(parent: dove, model: "sphere", scale: new(0.24f, 0.20f, 0.32f), color: Color.White);
                    new Entity(parent: dove, model: "diamond", scale: new(0.48f, 0.08f, 0.20f), position: new(0, 0.06f, 0), color: Color.White);
                    parts.Tails.Add(dove);
                    break;

                case "aigis":
                    // Метис: механические крылья-бабочки за спиной и боевая корона-визор
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.58f, 0.22f, 0.24f), position: new(0, 0.18f, 0.22f), color: Color.Gold, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        new Entity(parent: torso, model: "diamond", scale: new(1.18f, 0.34f, 0.10f), position: new(s * 0.68f, 0.24f, -0.28f), rotation: new(0, 0, s * 22f), color: skin.ArmorColor, unlit: true);
                    break;

                case "reimu":
                    // Божественная Хакурей: кольцо из 6 золотых сфер Инь-Ян за спиной
                    var orbRing = new Entity(parent: torso, position: new(0, 0.28f, -0.34f));
                    for (int i = 0; i < 6; i++)
                    {
                        float ang = i * MathF.PI / 3f;
                        new Entity(parent: orbRing, model: "sphere", scaleUniform: 0.22f,
                            position: new(MathF.Cos(ang) * 0.72f, MathF.Sin(ang) * 0.72f, 0),
                            color: i % 2 == 0 ? Color.Gold : Rgb(255, 65, 85), unlit: true);
                    }
                    parts.Halo = orbRing;
                    break;

                case "mindustry":
                    // Дредноут Эклипс: дополнительные тяжёлые крылья и энергетическое кольцо
                    foreach (int s in new[] { -1, 1 })
                        new Entity(parent: torso, model: "diamond", scale: new(1.35f, 0.16f, 0.55f), position: new(s * 0.95f, 0.08f, -0.10f), color: Color.Gold);
                    var mRing = new Entity(parent: torso, model: "diamond", scale: new(1.65f, 0.10f, 1.65f), position: new(0, 0.22f, 0), color: skin.VisorColor, unlit: true);
                    parts.Halo = mRing;
                    break;

                case "slark":
                    // Акулий Принц: золотая корона на гребне и второй клинок в левой руке
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.32f, 0.42f, 0.48f), position: new(0, 0.36f, 0.02f), color: Color.Gold, unlit: true);
                    if (parts.LeftArm != null)
                        new Entity(parent: parts.LeftArm, model: "diamond", scale: new(0.14f, 0.22f, 0.95f), position: new(0, -0.28f, 0.65f), color: Color.Gold, unlit: true);
                    break;

                case "tidehunter":
                    // Дредноут Глубин: золотая корона Посейдона и наплечные пушки
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.62f, 0.38f, 0.32f), position: new(0, 0.38f, 0.14f), color: Color.Gold, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        new Entity(parent: torso, model: "cube", scale: new(0.28f, 0.28f, 0.58f), position: new(s * 0.55f, 0.42f, 0.12f), color: Color.Gold);
                    break;

                case "freeman":
                    // Приручённый хедкраб Ламарр на макушке Фримана и золотой ореол HEV Mk.V!
                    if (head != null)
                    {
                        var lamarr = new Entity(parent: head, position: new(0, 0.34f, 0.02f));
                        new Entity(parent: lamarr, model: "sphere", scale: new(0.48f, 0.28f, 0.46f), color: Rgb(215, 145, 85));
                        foreach (int s in new[] { -1, 1 })
                            new Entity(parent: lamarr, model: "diamond", scale: new(0.10f, 0.26f, 0.12f), position: new(s * 0.22f, -0.10f, 0.16f), color: Rgb(185, 75, 55));
                    }
                    break;

                case "dante":
                    // Апокалипсис Полуночи: 4 золотых крыла серафима за спиной
                    foreach (int s in new[] { -1, 1 })
                    {
                        new Entity(parent: torso, model: "diamond", scale: new(1.30f, 0.24f, 0.12f), position: new(s * 0.72f, 0.38f, -0.28f), rotation: new(0, 0, s * 28f), color: Color.Gold, unlit: true);
                        new Entity(parent: torso, model: "diamond", scale: new(1.05f, 0.20f, 0.12f), position: new(s * 0.62f, 0.05f, -0.28f), rotation: new(0, 0, s * -16f), color: Rgb(255, 95, 45), unlit: true);
                    }
                    break;

                case "cheburashka":
                    // Кибер-Чебуратор 3000: неоновый кибер-визор на глазах, золотая корона и реактивный ранец!
                    if (head != null)
                    {
                        new Entity(parent: head, model: "cube", scale: new(0.56f, 0.14f, 0.16f), position: new(0, 0.06f, 0.28f), color: skin.VisorColor, unlit: true);
                        new Entity(parent: head, model: "diamond", scale: new(0.34f, 0.28f, 0.34f), position: new(0, 0.38f, 0.02f), color: Color.Gold, unlit: true);
                    }
                    new Entity(parent: torso, model: "sphere", scaleUniform: 0.38f, position: new(0, 0.08f, -0.28f), color: Color.Orange, unlit: true);
                    break;

                case "ahri":
                    // Дух Цветения Ари: золотые рожки-диадема Духа Цветения и парящий призрачный лисёнок над плечом!
                    if (head != null)
                    {
                        new Entity(parent: head, model: "diamond", scale: new(0.28f, 0.22f, 0.12f), position: new(0, 0.28f, 0.24f), color: Color.Gold, unlit: true);
                        foreach (int s in new[] { -1, 1 })
                            new Entity(parent: head, model: "diamond", scale: new(0.10f, 0.32f, 0.12f), position: new(s * 0.18f, 0.38f, 0.14f), rotation: new(12, 0, s * -22f), color: Rgb(255, 165, 215), unlit: true);
                    }
                    var foxSpirit = new Entity(parent: torso, position: new(-0.64f, 0.62f, 0.14f));
                    new Entity(parent: foxSpirit, model: "sphere", scale: new(0.26f, 0.22f, 0.32f), color: Rgb(135, 240, 255), unlit: true);
                    new Entity(parent: foxSpirit, model: "diamond", scale: new(0.18f, 0.18f, 0.36f), position: new(0, 0.06f, -0.24f), color: Rgb(255, 155, 215), unlit: true);
                    parts.Tails.Add(foxSpirit);
                    break;

                case "gold_ship":
                    // Легенда Гран-При G1: золотая корона Чемпиона, капитанский плащ и золотой якорь за спиной!
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.38f, 0.28f, 0.38f), position: new(0, 0.44f, 0.02f), color: Color.Gold, unlit: true);
                    var gAnchor = new Entity(parent: torso, position: new(0, 0.18f, -0.34f));
                    new Entity(parent: gAnchor, model: "cube", scale: new(0.14f, 0.95f, 0.14f), color: Color.Gold, unlit: true);
                    new Entity(parent: gAnchor, model: "diamond", scale: new(0.85f, 0.26f, 0.16f), position: new(0, -0.38f, 0), color: Color.Gold, unlit: true);
                    break;

                case "pushnoy":
                    // Повелитель Молний Теслы: ранец с катушкой Теслы за спиной и неоновые очки!
                    if (head != null)
                        new Entity(parent: head, model: "cube", scale: new(0.52f, 0.12f, 0.14f), position: new(0, 0.06f, 0.27f), color: skin.VisorColor, unlit: true);
                    var teslaPack = new Entity(parent: torso, position: new(0, 0.28f, -0.32f));
                    new Entity(parent: teslaPack, model: "cube", scale: new(0.36f, 0.56f, 0.24f), color: Rgb(50, 56, 70));
                    new Entity(parent: teslaPack, model: "sphere", scaleUniform: 0.34f, position: new(0, 0.44f, 0), color: skin.VisorColor, unlit: true);
                    break;

                case "freddy":
                    // Глэмрок Фредди: панк-наплечники со шипами и неоновые очки-звёзды!
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.62f, 0.18f, 0.14f), position: new(0, 0.10f, 0.34f), color: skin.VisorColor, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        new Entity(parent: torso, model: "diamond", scale: new(0.38f, 0.26f, 0.38f), position: new(s * 0.52f, 0.38f, 0), color: Rgb(235, 45, 75), unlit: true);
                    break;

                case "steve":
                    // Незеритовый Лорд и Херобрин: крылья Элитры за спиной и золотая корона!
                    if (head != null)
                        new Entity(parent: head, model: "cube", scale: new(0.56f, 0.16f, 0.56f), position: new(0, 0.34f, 0), color: Color.Gold, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        new Entity(parent: torso, model: "diamond", scale: new(0.52f, 1.05f, 0.10f), position: new(s * 0.28f, -0.05f, -0.26f), rotation: new(12, 0, s * 18f), color: Rgb(95, 85, 115));
                    break;

                case "jotaro":
                    // Star Platinum Over Heaven: золотой ореол над Стендом и золотые наплечники
                    if (parts.ShieldAura != null)
                        new Entity(parent: parts.ShieldAura, model: "diamond", scale: new(0.75f, 0.12f, 0.75f), position: new(0, 0.95f, 0), color: Color.Gold, unlit: true);
                    break;

                case "vyper":
                    // Золотая Королевская Кобра: раскрытый золотой капюшон кобры за головой!
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.88f, 0.68f, 0.14f), position: new(0, 0.08f, -0.14f), color: Color.Gold, unlit: true);
                    break;

                case "haze":
                    // Архонт Вечных Снов: парящий золотой нимб часов за спиной
                    var dreamHalo = new Entity(parent: torso, model: "diamond", scale: new(1.25f, 1.25f, 0.08f), position: new(0, 0.42f, -0.32f), color: Color.Gold, unlit: true);
                    parts.Halo = dreamHalo;
                    break;

                case "sanabi":
                    // Высший Генерал Чосона: золотой кибер-ореол над плечами, парадные эполеты и серафим-крылья у дрона Мари
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.54f, 0.22f, 0.14f), position: new(0, 0.34f, 0.22f), color: Color.Gold, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        new Entity(parent: torso, model: "diamond", scale: new(0.46f, 0.18f, 0.36f), position: new(s * 0.44f, 0.38f, 0), color: Color.Gold, unlit: true);
                    if (parts.ShieldAura != null)
                    {
                        foreach (int s in new[] { -1, 1 })
                            new Entity(parent: parts.ShieldAura, model: "diamond", scale: new(0.65f, 0.16f, 0.10f), position: new(s * 0.38f, 0.12f, -0.08f), rotation: new(0, 0, s * 28f), color: Color.Gold, unlit: true);
                    }
                    break;

                case "jack_o":
                    // Крылья Арии и Задворков: световые крылья серафима Задворков за спиной и золотая диадема!
                    if (head != null)
                        new Entity(parent: head, model: "diamond", scale: new(0.46f, 0.24f, 0.14f), position: new(0, 0.38f, 0.18f), color: Color.Gold, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                    {
                        new Entity(parent: torso, model: "diamond", scale: new(1.22f, 0.24f, 0.10f), position: new(s * 0.68f, 0.36f, -0.28f), rotation: new(0, s * -12f, s * 26f), color: Color.Gold, unlit: true);
                        new Entity(parent: torso, model: "diamond", scale: new(0.96f, 0.18f, 0.10f), position: new(s * 0.58f, 0.06f, -0.28f), rotation: new(0, s * -12f, s * -18f), color: skin.VisorColor, unlit: true);
                    }
                    break;
                default:
                    ApplyExpansionPack1Mastery(charKey, root, parts, skin);
                    ApplyExpansionPack2Mastery(charKey, root, parts, skin);
                    break;
            }
        }

        public static SurvivorParts BuildSurvivorMesh(Entity parentEntity, string charKey, int? skinOverride = null)
        {
            if (!Classes.TryGetValue(charKey, out var cfg))
                cfg = Classes["commando"];

            cfg.SyncLoadout();
            var skin = cfg.GetActiveSkin(skinOverride);
            int skinIdx = skinOverride ?? Data.ProgressData.Instance.GetSelectedSkin(charKey);
            if (!skinOverride.HasValue && !Data.ProgressData.Instance.IsSkinUnlocked(charKey, skinIdx, skin.UnlockAchKey))
                skinIdx = 0;

            Color savedBody = cfg.BodyColor;
            Color savedArmor = cfg.ArmorColor;
            Color savedVisor = cfg.VisorColor;
            cfg.BodyColor = skin.BodyColor;
            cfg.ArmorColor = skin.ArmorColor;
            cfg.VisorColor = skin.VisorColor;

            var root = new Entity(parent: parentEntity);
            var parts = new SurvivorParts { Root = root };

            switch (charKey)
            {
                case "commando": BuildCommando(root, cfg, parts); break;
                case "artificer": BuildArtificer(root, cfg, parts); break;
                case "huntress": BuildHuntress(root, cfg, parts); break;
                case "buzz": BuildBuzz(root, cfg, parts); break;
                case "v1": BuildV1(root, cfg, parts); break;
                case "sol": BuildSol(root, cfg, parts, skinIdx); break;
                case "gwen": BuildGwen(root, cfg, parts); break;
                case "enforcer": BuildEnforcer(root, cfg, parts); break;
                case "scout": BuildScout(root, cfg, parts); break;
                case "m4a1": BuildM4A1(root, cfg, parts); break;
                case "medic": BuildMedic(root, cfg, parts); break;
                case "aigis": BuildAigis(root, cfg, parts); break;
                case "reimu": BuildReimu(root, cfg, parts); break;
                case "mindustry": BuildMindustry(root, cfg, parts); break;
                case "slark": BuildSlark(root, cfg, parts); break;
                case "tidehunter": BuildTidehunter(root, cfg, parts); break;
                case "freeman": BuildFreeman(root, cfg, parts); break;
                case "dante": BuildDante(root, cfg, parts); break;
                case "cheburashka": BuildCheburashka(root, cfg, parts); break;
                case "ahri": BuildAhri(root, cfg, parts); break;
                case "lappland": BuildLappland(root, cfg, parts, skinIdx); break;
                case "w_arknights": BuildWArknights(root, cfg, parts, skinIdx); break;
                case "gold_ship": BuildGoldShip(root, cfg, parts); break;
                case "pushnoy": BuildPushnoy(root, cfg, parts); break;
                case "freddy": BuildFreddy(root, cfg, parts); break;
                case "steve": BuildSteve(root, cfg, parts); break;
                case "jotaro": BuildJotaro(root, cfg, parts, skinIdx); break;
                case "vyper": BuildVyper(root, cfg, parts); break;
                case "haze": BuildHaze(root, cfg, parts); break;
                case "dmc_trio": BuildDmcTrio(root, cfg, parts, skinIdx); break;
                case "sanabi": BuildSanabi(root, cfg, parts, skinIdx); break;
                case "jack_o": BuildJackO(root, cfg, parts, skinIdx); break;
                default:
                    if (!TryBuildExpansionPack1(charKey, root, cfg, parts, skinIdx) &&
                        !TryBuildExpansionPack2(charKey, root, cfg, parts, skinIdx))
                    {
                        BuildCommando(root, cfg, parts);
                    }
                    break;
            }

            cfg.BodyColor = savedBody;
            cfg.ArmorColor = savedArmor;
            cfg.VisorColor = savedVisor;

            bool customSkinModel = charKey is "lappland" or "w_arknights" or "dmc_trio" or "bird_trio" or "don_quixote" or "komeiji"
                                   || (charKey == "sol" && skinIdx == 2)
                                   || (charKey == "spamton" && skinIdx == 2);
            if (skinIdx > 0 && !customSkinModel)
                ApplySkinColorRemap(root, cfg, skin);
            if (skin.ChangesModel && !customSkinModel)
                ApplyMasterySkinGeometry(charKey, root, parts, skin);

            parts.CaptureBindPose();
            return parts;
        }

        // ==========================================
        // 1. COMMANDO (Risk of Rain 2)
        // ==========================================
        private static void BuildCommando(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.22f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.22f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.44f, 0.26f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.08f, 0.40f, 0.24f), position: new(-side * 0.10f, -0.20f, 0), color: Rgb(32, 36, 44));
                new Entity(parent: hip, model: "cube", scale: new(0.12f, 0.26f, 0.22f), position: new(side * 0.15f, -0.18f, 0.02f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.04f, 0.28f), position: new(0, -0.14f, 0), color: Rgb(32, 36, 44));
                new Entity(parent: hip, model: "sphere", scale: new(0.22f, 0.20f, 0.22f), position: new(0, -0.42f, 0.02f), color: Rgb(35, 40, 50));
                new Entity(parent: hip, model: "diamond", scale: new(0.26f, 0.26f, 0.28f), position: new(0, -0.42f, 0.10f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.10f, 0.10f, 0.08f), position: new(0, -0.42f, 0.20f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.42f, 0.24f), position: new(0, -0.62f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.32f, 0.10f), position: new(0, -0.60f, 0.09f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.04f, 0.22f, 0.04f), position: new(0, -0.60f, 0.14f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.18f, 0.42f), position: new(0, -0.80f, 0.06f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.06f, 0.44f), position: new(0, -0.87f, 0.06f), color: Rgb(25, 28, 35));
                new Entity(parent: hip, model: "diamond", scale: new(0.24f, 0.14f, 0.18f), position: new(0, -0.80f, 0.24f), color: Rgb(35, 40, 50));
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.06f, 0.12f), position: new(0, -0.76f, -0.14f), color: cfg.VisorColor, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.62f, 0.20f, 0.42f), position: new(0, -0.32f, 0), color: Rgb(32, 36, 44));
            new Entity(parent: torso, model: "cube", scale: new(0.18f, 0.14f, 0.45f), position: new(0, -0.32f, 0), color: cfg.ArmorColor);
            foreach (float px in new[] { -0.18f, 0.18f })
                new Entity(parent: torso, model: "cube", scale: new(0.12f, 0.14f, 0.10f), position: new(px, -0.32f, 0.21f), color: cfg.ArmorColor);
            foreach (float px in new[] { -0.31f, 0.31f })
                new Entity(parent: torso, model: "cube", scale: new(0.10f, 0.18f, 0.24f), position: new(px, -0.31f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.70f, 0.62f, 0.44f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.64f, 0.48f, 0.20f), position: new(0, 0.10f, 0.14f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.58f, 0.42f, 0.28f), position: new(0, 0.12f, 0.18f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.20f, 0.24f, 0.32f), position: new(0, 0.14f, 0.24f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.72f, 0.08f, 0.48f), position: new(0, 0.08f, 0.02f), rotation: new(0, 0, 26f), color: Rgb(35, 40, 50));
            new Entity(parent: torso, model: "cube", scale: new(0.44f, 0.14f, 0.42f), position: new(0, 0.39f, 0), color: cfg.ArmorColor);

            var backpack = new Entity(parent: torso, position: new(0, 0.10f, -0.30f));
            new Entity(parent: backpack, model: "cube", scale: new(0.52f, 0.56f, 0.26f), color: cfg.ArmorColor);
            new Entity(parent: backpack, model: "cube", scale: new(0.38f, 0.38f, 0.16f), position: new(0, 0, -0.12f), color: Rgb(30, 35, 45));
            new Entity(parent: backpack, model: "cube", scale: new(0.12f, 0.30f, 0.20f), position: new(0, 0, -0.14f), color: cfg.VisorColor, unlit: true);
            foreach (float sideX in new[] { -0.18f, 0.18f })
            {
                var thruster = new Entity(parent: backpack, model: "cube", scale: new(0.16f, 0.26f, 0.16f), position: new(sideX, -0.14f, -0.06f), rotation: new(14, 0, 0), color: Rgb(40, 45, 55));
                new Entity(parent: thruster, model: "diamond", scale: new(0.20f, 0.18f, 0.20f), position: new(0, -0.16f, 0), color: cfg.ArmorColor);
                new Entity(parent: thruster, model: "diamond", scale: new(0.14f, 0.26f, 0.14f), position: new(0, -0.28f, 0), color: cfg.VisorColor, unlit: true);
            }
            new Entity(parent: backpack, model: "sphere", scaleUniform: 0.08f, position: new(0, 0.32f, -0.06f), color: Color.Red, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.58f, 0.56f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.36f, 0.54f), position: new(0, 0.14f, -0.02f), color: cfg.BodyColor);
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.42f, 0.44f), position: new(0, 0.02f, 0.15f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "cube", scale: new(0.56f, 0.08f, 0.38f), position: new(0, 0.18f, 0.12f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "cube", scale: new(0.24f, 0.16f, 0.22f), position: new(0, -0.18f, 0.22f), color: Rgb(35, 40, 50));
            foreach (float cx in new[] { -0.16f, 0.16f })
                new Entity(parent: head, model: "sphere", scale: new(0.12f, 0.12f, 0.10f), position: new(cx, -0.18f, 0.20f), color: Rgb(60, 68, 80));
            new Entity(parent: head, model: "cube", scale: new(0.04f, 0.38f, 0.04f), position: new(-0.28f, 0.28f, -0.06f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.08f, position: new(-0.28f, 0.48f, -0.06f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "cube", scale: new(0.08f, 0.08f, 0.16f), position: new(0.28f, 0.14f, 0.10f), color: Rgb(35, 40, 50));
            new Entity(parent: head, model: "sphere", scaleUniform: 0.07f, position: new(0.28f, 0.14f, 0.18f), color: Color.White, unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));
            for (int idx = 0; idx < 2; idx++)
            {
                var sh = idx == 0 ? lShoulder : rShoulder;
                float side = idx == 0 ? -1f : 1f;

                // Наплечник Commando
                new Entity(parent: sh, model: "diamond", scale: new(0.44f, 0.40f, 0.44f), position: new(0, 0.08f, 0), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "cube", scale: new(0.42f, 0.06f, 0.42f), position: new(0, 0.08f, 0), color: cfg.BodyColor);

                // Верхняя часть руки (бицепс)
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.06f, 0.24f, 0.20f), position: new(side * 0.08f, -0.16f, 0.02f), color: cfg.ArmorColor);

                // Локтевой сустав
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);

                // Предплечье и бронещиток наруча
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.22f, 0.20f, 0.22f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "cube", scale: new(0.04f, 0.14f, 0.16f), position: new(side * 0.10f, -0.30f, 0.22f), color: cfg.VisorColor, unlit: true);

                // Кисть / Перчатка
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.16f, 0.14f), position: new(0, -0.30f, 0.40f), color: Rgb(32, 36, 44));

                // Dual UES Phase Pistols в обеих руках
                var gun = new Entity(parent: sh, position: new(0, -0.30f, 0.44f));
                new Entity(parent: gun, model: "cube", scale: new(0.11f, 0.14f, 0.65f), position: new(0, 0.06f, 0.18f), color: Rgb(35, 40, 52));
                new Entity(parent: gun, model: "cube", scale: new(0.06f, 0.04f, 0.58f), position: new(0, 0.14f, 0.18f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: gun, model: "cube", scale: new(0.12f, 0.12f, 0.48f), position: new(0, -0.04f, 0.15f), color: cfg.ArmorColor);
                new Entity(parent: gun, model: "diamond", scale: new(0.08f, 0.08f, 0.22f), position: new(0, -0.10f, 0.28f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: gun, model: "cube", scale: new(0.10f, 0.22f, 0.14f), position: new(0, -0.16f, -0.02f), rotation: new(-20, 0, 0), color: cfg.BodyColor);
                new Entity(parent: gun, model: "cube", scale: new(0.11f, 0.04f, 0.16f), position: new(0, -0.27f, -0.06f), color: Rgb(25, 28, 35));
                new Entity(parent: gun, model: "cube", scale: new(0.10f, 0.10f, 0.08f), position: new(0, 0.06f, 0.52f), color: Rgb(70, 78, 92));
                new Entity(parent: gun, model: "sphere", scaleUniform: 0.06f, position: new(0, 0.06f, 0.56f), color: cfg.VisorColor, unlit: true);
            }
            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 2. ARTIFICER (Risk of Rain 2)
        // ==========================================
        private static void BuildArtificer(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.44f, 0.22f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.04f, 0.40f, 0.20f), position: new(side * 0.11f, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.26f, 0.28f, 0.28f), position: new(0, -0.42f, 0.06f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "sphere", scaleUniform: 0.08f, position: new(0, -0.42f, 0.16f), color: Color.SkyBlue, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.40f, 0.20f), position: new(0, -0.62f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.32f, 0.12f), position: new(0, -0.60f, 0.08f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.14f, 0.36f), position: new(0, -0.80f, 0.06f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.04f, 0.38f), position: new(0, -0.86f, 0.06f), color: Color.SkyBlue, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.20f, 0.38f), position: new(0, -0.32f, 0), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.22f, 0.22f, 0.42f), position: new(0, -0.32f, 0), color: cfg.VisorColor, unlit: true);
            // 4 раздельных подола мантии с золотой окантовкой
            foreach (float sx in new[] { -0.28f, 0.28f })
            {
                new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.85f, 0.44f), position: new(sx, -0.52f, 0), rotation: new(0, 0, sx * -16f), color: cfg.BodyColor);
                new Entity(parent: torso, model: "diamond", scale: new(0.28f, 0.22f, 0.46f), position: new(sx, -0.82f, 0), color: cfg.ArmorColor);
            }
            new Entity(parent: torso, model: "diamond", scale: new(0.58f, 0.90f, 0.20f), position: new(0, -0.54f, -0.22f), rotation: new(12, 0, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.54f, 0.70f, 0.16f), position: new(0, -0.48f, 0.20f), rotation: new(-10, 0, 0), color: cfg.BodyColor);

            new Entity(parent: torso, model: "cube", scale: new(0.64f, 0.60f, 0.40f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.66f, 0.52f, 0.36f), position: new(0, 0.12f, 0.12f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.22f, 0.30f, 0.32f), position: new(0, 0.14f, 0.20f), color: Color.Yellow, unlit: true);
            new Entity(parent: torso, model: "diamond", scale: new(0.72f, 0.32f, 0.72f), position: new(0, 0.38f, 0), color: cfg.ArmorColor);

            // Реактивный парящий ранец Архимага
            var jet = new Entity(parent: torso, position: new(0, 0.12f, -0.28f));
            new Entity(parent: jet, model: "diamond", scale: new(0.72f, 0.68f, 0.24f), color: cfg.ArmorColor);
            foreach (float sideX in new[] { -0.24f, 0.24f })
            {
                new Entity(parent: jet, model: "diamond", scale: new(0.18f, 0.72f, 0.18f), position: new(sideX, -0.16f, 0), rotation: new(14, 0, 0), color: cfg.BodyColor);
                new Entity(parent: jet, model: "sphere", scaleUniform: 0.14f, position: new(sideX, -0.48f, 0), color: Color.SkyBlue, unlit: true);
            }

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.56f, 0.52f), color: cfg.BodyColor);
            new Entity(parent: head, model: "diamond", scale: new(0.68f, 0.32f, 0.68f), position: new(0, 0.20f, 0), color: cfg.ArmorColor);
            new Entity(parent: head, model: "diamond", scale: new(0.22f, 0.52f, 0.38f), position: new(0, 0.38f, 0.06f), color: cfg.BodyColor);
            new Entity(parent: head, model: "cube", scale: new(0.48f, 0.14f, 0.36f), position: new(0, 0.04f, 0.16f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "diamond", scale: new(0.32f, 0.18f, 0.24f), position: new(0, -0.16f, 0.16f), color: cfg.ArmorColor);
            parts.Head = head;
            parts.Torso = torso;

            // Плазменное вращающееся гало (Halo)
            var halo = new Entity(parent: torso, position: new(0, 0.42f, -0.42f));
            new Entity(parent: halo, model: "diamond", scale: new(1.35f, 1.35f, 0.08f), color: Color.Gold, unlit: true);
            new Entity(parent: halo, model: "diamond", scale: new(1.35f, 1.35f, 0.08f), rotation: new(0, 0, 45f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: halo, model: "sphere", scaleUniform: 0.32f, color: Color.Magenta, unlit: true);
            new Entity(parent: halo, model: "sphere", scaleUniform: 0.16f, color: Color.Yellow, unlit: true);
            parts.Halo = halo;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));
            for (int idx = 0; idx < 2; idx++)
            {
                var sh = idx == 0 ? lShoulder : rShoulder;
                float side = idx == 0 ? -1f : 1f;

                // Наплечник мантии Архимага
                new Entity(parent: sh, model: "diamond", scale: new(0.46f, 0.44f, 0.46f), position: new(0, 0.08f, 0), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "diamond", scale: new(0.24f, 0.24f, 0.24f), position: new(side * 0.12f, 0.16f, 0), color: Color.Gold, unlit: true);

                // Верхняя часть руки и локоть
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);

                // Предплечье и золотой наруч
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "diamond", scale: new(0.24f, 0.24f, 0.28f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: cfg.BodyColor);

                if (idx == 0) // Левая рука: Charged Nano-Bomb
                {
                    var orb = new Entity(parent: sh, position: new(0, -0.30f, 0.52f));
                    new Entity(parent: orb, model: "sphere", scaleUniform: 0.46f, color: Color.Magenta, unlit: true);
                    new Entity(parent: orb, model: "sphere", scaleUniform: 0.26f, color: Color.Yellow, unlit: true);
                    foreach (float ang in new[] { 0f, 90f, 180f, 270f })
                        new Entity(parent: orb, model: "diamond", scale: new(0.16f, 0.16f, 0.16f), position: new(MathF.Cos(ang * 0.0174f) * 0.35f, MathF.Sin(ang * 0.0174f) * 0.35f, 0), color: Color.SkyBlue, unlit: true);
                }
                else // Правая рука: Plasma Bolt Gauntlet
                {
                    var plasma = new Entity(parent: sh, position: new(0, -0.30f, 0.50f));
                    new Entity(parent: plasma, model: "sphere", scaleUniform: 0.38f, color: Color.Yellow, unlit: true);
                    new Entity(parent: plasma, model: "diamond", scale: new(0.55f, 0.14f, 0.55f), color: Color.SkyBlue, unlit: true);
                    new Entity(parent: plasma, model: "diamond", scale: new(0.18f, 0.18f, 0.65f), position: new(0, 0, 0.30f), color: Color.White, unlit: true);
                }
            }
            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 3. HUNTRESS (Risk of Rain 2)
        // ==========================================
        private static void BuildHuntress(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.44f, 0.22f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.38f, 0.20f), position: new(side * 0.10f, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.26f, 0.26f, 0.28f), position: new(0, -0.42f, 0.06f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.08f, 0.08f, 0.06f), position: new(0, -0.42f, 0.16f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.20f), position: new(0, -0.62f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.04f, 0.22f), position: new(0, -0.54f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.16f, 0.38f), position: new(0, -0.80f, 0.06f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.18f, 0.12f, 0.16f), position: new(0, -0.80f, 0.20f), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.16f, 0.04f, 0.08f), position: new(0, -0.76f, -0.12f), color: Color.SkyBlue, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.20f, 0.38f), position: new(0, -0.32f, 0), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.18f, 0.18f, 0.42f), position: new(0, -0.32f, 0), color: cfg.VisorColor, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.62f, 0.60f, 0.38f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.54f, 0.48f, 0.26f), position: new(0, 0.12f, 0.14f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.18f, 0.24f, 0.28f), position: new(0, 0.12f, 0.20f), color: Color.Gold, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.08f, 0.42f), position: new(0, 0.06f, 0.02f), rotation: new(0, 0, -28f), color: cfg.ArmorColor);

            // Знаменитый развевающийся алый шарф Охотницы
            var scarf = new Entity(parent: torso, position: new(0, 0.38f, -0.06f));
            new Entity(parent: scarf, model: "cube", scale: new(0.54f, 0.16f, 0.42f), color: cfg.BodyColor);
            new Entity(parent: scarf, model: "diamond", scale: new(0.32f, 0.98f, 0.14f), position: new(-0.16f, -0.52f, -0.26f), rotation: new(20, 8, 0), color: cfg.BodyColor);
            new Entity(parent: scarf, model: "diamond", scale: new(0.28f, 0.85f, 0.14f), position: new(0.16f, -0.46f, -0.24f), rotation: new(22, -8, 0), color: cfg.BodyColor);

            // Колчан с энергетическими стрелами на спине
            var quiver = new Entity(parent: torso, position: new(0.22f, 0.16f, -0.28f), rotation: new(0, 0, -22f));
            new Entity(parent: quiver, model: "cube", scale: new(0.20f, 0.68f, 0.20f), color: cfg.ArmorColor);
            new Entity(parent: quiver, model: "cube", scale: new(0.22f, 0.08f, 0.22f), position: new(0, 0.26f, 0), color: cfg.BodyColor);
            foreach (float ax in new[] { -0.05f, 0.0f, 0.05f })
            {
                new Entity(parent: quiver, model: "cube", scale: new(0.04f, 0.40f, 0.04f), position: new(ax, 0.38f, 0), color: Rgb(35, 40, 50));
                new Entity(parent: quiver, model: "diamond", scale: new(0.08f, 0.18f, 0.08f), position: new(ax, 0.54f, 0), color: Color.SkyBlue, unlit: true);
            }

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.56f, 0.54f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "diamond", scale: new(0.66f, 0.60f, 0.66f), position: new(0, 0.08f, -0.04f), color: cfg.BodyColor);
            // Визор-сенсор Охотницы с горизонтальной световой полосой
            new Entity(parent: head, model: "cube", scale: new(0.46f, 0.10f, 0.38f), position: new(0, 0.04f, 0.18f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(0, 0.04f, 0.26f), color: Color.Yellow, unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.46f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.46f, 0.25f, 0));

            // Левая рука: Композитный Лазерный Лук (Strafe Bow)
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.38f, 0.34f, 0.38f), position: new(0, 0.06f, 0), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: cfg.BodyColor);

            var bow = new Entity(parent: lShoulder, position: new(-0.06f, -0.30f, 0.44f), rotation: new(0, 0, 16f));
            new Entity(parent: bow, model: "diamond", scale: new(0.12f, 1.45f, 0.24f), color: cfg.ArmorColor);
            new Entity(parent: bow, model: "diamond", scale: new(0.15f, 0.55f, 0.30f), color: cfg.BodyColor);
            foreach (float py in new[] { -0.68f, 0.68f })
            {
                new Entity(parent: bow, model: "sphere", scaleUniform: 0.15f, position: new(0, py, 0), color: Color.Gold, unlit: true);
                new Entity(parent: bow, model: "diamond", scale: new(0.10f, 0.24f, 0.18f), position: new(0, py + (py > 0 ? 0.08f : -0.08f), -0.05f), color: cfg.ArmorColor);
            }
            new Entity(parent: bow, model: "cube", scale: new(0.03f, 1.35f, 0.03f), position: new(0, 0, -0.10f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: bow, model: "diamond", scale: new(0.07f, 0.07f, 0.95f), position: new(0, 0, 0.22f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: bow, model: "diamond", scale: new(0.14f, 0.14f, 0.22f), position: new(0, 0, 0.72f), color: Color.Gold, unlit: true);
            parts.Bow = bow;

            // Правая рука: Лазерная Глефа (Laser Glaive)
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.38f, 0.34f, 0.38f), position: new(0, 0.06f, 0), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: cfg.BodyColor);

            var glaive = new Entity(parent: rShoulder, position: new(0.04f, -0.30f, 0.44f));
            new Entity(parent: glaive, model: "sphere", scaleUniform: 0.22f, color: Color.Gold, unlit: true);
            new Entity(parent: glaive, model: "sphere", scaleUniform: 0.12f, color: Color.White, unlit: true);
            for (int i = 0; i < 3; i++)
            {
                var blade = new Entity(parent: glaive, rotation: new(0, 0, i * 120f));
                new Entity(parent: blade, model: "diamond", scale: new(0.14f, 0.52f, 0.09f), position: new(0, 0.26f, 0), color: Color.SkyBlue, unlit: true);
                new Entity(parent: blade, model: "sphere", scaleUniform: 0.09f, position: new(0, 0.46f, 0), color: Color.White, unlit: true);
            }

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 4. BUZZ LIGHTYEAR (Star Command)
        // ==========================================
        private static void BuildBuzz(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.24f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.24f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.44f, 0.28f), position: new(0, -0.20f, 0), color: Color.White);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.12f, 0.30f), position: new(0, -0.42f, 0), color: Rgb(145, 55, 225));
                new Entity(parent: hip, model: "diamond", scale: new(0.28f, 0.26f, 0.32f), position: new(0, -0.42f, 0.08f), color: Rgb(125, 235, 65));
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.40f, 0.26f), position: new(0, -0.62f, 0), color: Color.White);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.08f, 0.28f), position: new(0, -0.74f, 0.04f), color: Rgb(125, 235, 65));
                new Entity(parent: hip, model: "cube", scale: new(0.30f, 0.18f, 0.46f), position: new(0, -0.80f, 0.08f), color: Color.White);
                new Entity(parent: hip, model: "cube", scale: new(0.32f, 0.06f, 0.48f), position: new(0, -0.87f, 0.08f), color: Rgb(125, 235, 65));
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.04f, 0.20f), position: new(0, -0.88f, 0.12f), color: Color.SkyBlue, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.66f, 0.20f, 0.44f), position: new(0, -0.32f, 0), color: Rgb(125, 235, 65));
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.32f, 0.22f), color: Color.Red, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.64f, 0.16f, 0.42f), position: new(0, -0.16f, 0), color: Rgb(35, 38, 44));
            new Entity(parent: torso, model: "cube", scale: new(0.74f, 0.54f, 0.48f), position: new(0, 0.12f, 0), color: Color.White);
            new Entity(parent: torso, model: "cube", scale: new(0.76f, 0.36f, 0.50f), position: new(0, 0.22f, 0.02f), color: Rgb(125, 235, 65));
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.12f, 0.52f), position: new(0, 0.36f, 0.02f), color: Rgb(145, 55, 225));

            // Кнопки Star Command на груди
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.10f, position: new(-0.20f, 0.24f, 0.28f), color: Color.Blue, unlit: true);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.10f, position: new(-0.08f, 0.24f, 0.28f), color: Color.Lime, unlit: true);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.10f, position: new(0.04f, 0.24f, 0.28f), color: Color.Red, unlit: true);
            // Красная треугольная кнопка лазера
            new Entity(parent: torso, model: "diamond", scale: new(0.18f, 0.14f, 0.10f), position: new(0.20f, 0.24f, 0.28f), color: Color.Red, unlit: true);
            // Эмблема Star Command
            new Entity(parent: torso, model: "diamond", scale: new(0.22f, 0.12f, 0.08f), position: new(0, 0.32f, 0.28f), color: Color.Gold, unlit: true);

            // Реактивный ранец и крылья Базза Лайтера
            var pack = new Entity(parent: torso, position: new(0, 0.12f, -0.30f));
            new Entity(parent: pack, model: "cube", scale: new(0.54f, 0.60f, 0.26f), color: Color.White);
            new Entity(parent: pack, model: "cube", scale: new(0.40f, 0.40f, 0.14f), position: new(0, 0, -0.12f), color: Rgb(145, 55, 225));
            foreach (float sideX in new[] { -0.18f, 0.18f })
            {
                var cone = new Entity(parent: pack, model: "cube", scale: new(0.14f, 0.24f, 0.14f), position: new(sideX, -0.22f, -0.06f), rotation: new(12, 0, 0), color: Rgb(145, 55, 225));
                new Entity(parent: cone, model: "diamond", scale: new(0.18f, 0.24f, 0.18f), position: new(0, -0.16f, 0), color: Color.Yellow, unlit: true);
            }
            // Развёрнутые знаменитые крылья Косморейнджера с габаритными огнями!
            foreach (int side in new[] { -1, 1 })
            {
                var wing = new Entity(parent: pack, model: "cube", scale: new(1.35f, 0.32f, 0.08f), position: new(side * 0.85f, 0.14f, -0.06f), rotation: new(0, 0, side * -8f), color: Rgb(155, 65, 245));
                new Entity(parent: wing, model: "cube", scale: new(1.15f, 0.42f, 0.12f), position: new(0, 0.32f, 0), color: Color.White);
                new Entity(parent: wing, model: "cube", scale: new(1.12f, 0.20f, 0.14f), position: new(0, 0.42f, 0), color: Color.Red, unlit: true);
                new Entity(parent: wing, model: "sphere", scale: new(0.20f, 0.72f, 0.38f), position: new(side * 0.58f, 0, 0), color: side > 0 ? Color.Lime : Color.Red, unlit: true);
            }

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.58f, 0.56f), color: Rgb(145, 55, 225));
            new Entity(parent: head, model: "sphere", scale: new(0.44f, 0.44f, 0.40f), position: new(0, -0.02f, 0.12f), color: Rgb(245, 205, 175));
            new Entity(parent: head, model: "cube", scale: new(0.24f, 0.14f, 0.20f), position: new(0, -0.16f, 0.22f), color: Rgb(235, 190, 160));
            // Подбородочная завитушка Базза
            new Entity(parent: head, model: "sphere", scaleUniform: 0.09f, position: new(0, -0.19f, 0.28f), color: Rgb(215, 165, 135));
            // Прозрачный сферический купол шлема
            new Entity(parent: head, model: "sphere", scale: new(0.78f, 0.78f, 0.78f), color: Rgb(190, 240, 255, 80), unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.52f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.52f, 0.25f, 0));
            for (int idx = 0; idx < 2; idx++)
            {
                var sh = idx == 0 ? lShoulder : rShoulder;

                // Сферический плечевой сустав Косморейнджера с фиолетовым кольцом
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.40f, position: new(0, 0.06f, 0), color: Color.White);
                new Entity(parent: sh, model: "cube", scale: new(0.42f, 0.08f, 0.42f), position: new(0, 0.06f, 0), color: Rgb(145, 55, 225));

                // Бицепс
                new Entity(parent: sh, model: "cube", scale: new(0.22f, 0.28f, 0.22f), position: new(0, -0.16f, 0.02f), color: Color.White);
                // Локоть
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.20f, position: new(0, -0.30f, 0.02f), color: Rgb(145, 55, 225));

                // Предплечье (Зелёная бронерукав Star Command)
                new Entity(parent: sh, model: "cube", scale: new(0.24f, 0.22f, 0.38f), position: new(0, -0.30f, 0.22f), color: Rgb(125, 235, 65));
                new Entity(parent: sh, model: "cube", scale: new(0.26f, 0.24f, 0.12f), position: new(0, -0.30f, 0.12f), color: Color.White);

                // Кисть / Белая перчатка с фиолетовыми костяшками
                new Entity(parent: sh, model: "cube", scale: new(0.20f, 0.18f, 0.16f), position: new(0, -0.30f, 0.42f), color: Color.White);
                new Entity(parent: sh, model: "cube", scale: new(0.21f, 0.08f, 0.12f), position: new(0, -0.26f, 0.42f), color: Rgb(145, 55, 225));

                if (idx == 1) // Правая рука: Наручный лазер с открывающейся крышкой
                {
                    var laser = new Entity(parent: sh, position: new(0, -0.18f, 0.28f));
                    new Entity(parent: laser, model: "cube", scale: new(0.18f, 0.09f, 0.30f), color: Color.Yellow);
                    new Entity(parent: laser, model: "sphere", scaleUniform: 0.14f, position: new(0, 0.06f, 0.16f), color: Color.Red, unlit: true);
                }
                else // Левая рука: Коммуникатор
                {
                    var comm = new Entity(parent: sh, position: new(0, -0.18f, 0.28f));
                    new Entity(parent: comm, model: "cube", scale: new(0.18f, 0.08f, 0.26f), color: Rgb(35, 38, 44));
                    new Entity(parent: comm, model: "cube", scale: new(0.14f, 0.04f, 0.18f), position: new(0, 0.04f, 0), color: Color.SkyBlue, unlit: true);
                }
            }
            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 5. V1 (ULTRAKILL)
        // ==========================================
        private static void BuildV1(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.44f, 0.20f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.38f, 0.06f), position: new(side * 0.10f, -0.20f, 0), color: Color.LightGray);
                new Entity(parent: hip, model: "diamond", scale: new(0.24f, 0.24f, 0.24f), position: new(0, -0.42f, 0.04f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.16f, 0.44f, 0.18f), position: new(0, -0.62f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.12f, 0.36f, 0.08f), position: new(0, -0.60f, 0.08f), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.16f, 0.20f, 0.42f), position: new(0, -0.80f, 0.10f), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.12f, 0.18f, 0.16f), position: new(0, -0.80f, -0.12f), color: Color.LightGray);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.48f, 0.18f, 0.34f), position: new(0, -0.32f, 0), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.12f, 0.22f, 0.12f), position: new(0, -0.22f, 0), color: Color.LightGray);
            foreach (float sideX in new[] { -0.14f, 0.14f })
                new Entity(parent: torso, model: "cube", scale: new(0.06f, 0.22f, 0.06f), position: new(sideX, -0.22f, 0.04f), color: Color.LightGray);
            new Entity(parent: torso, model: "cube", scale: new(0.58f, 0.54f, 0.38f), position: new(0, 0.08f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.52f, 0.44f, 0.26f), position: new(0, 0.14f, 0.14f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.12f, 0.24f, 0.08f), position: new(0, 0.12f, 0.22f), color: Color.Red, unlit: true);

            // Крылья V1 (8 излучателей плазменных перьев)
            var wingHub = new Entity(parent: torso, position: new(0, 0.16f, -0.26f));
            new Entity(parent: wingHub, model: "cube", scale: new(0.34f, 0.44f, 0.18f), color: cfg.ArmorColor);
            foreach (int side in new[] { -1, 1 })
            {
                foreach (float ang in new[] { 44f, 18f, -8f, -34f })
                {
                    var w = new Entity(parent: wingHub, rotation: new(0, side * -12f, side * ang));
                    new Entity(parent: w, model: "cube", scale: new(0.42f, 0.06f, 0.10f), position: new(side * 0.28f, 0, 0), color: cfg.BodyColor);
                    new Entity(parent: w, model: "diamond", scale: new(1.25f, 0.14f, 0.08f), position: new(side * 0.76f, 0, 0), color: cfg.VisorColor, unlit: true);
                    new Entity(parent: w, model: "sphere", scaleUniform: 0.09f, position: new(side * 1.38f, 0, 0), color: Color.White, unlit: true);
                }
            }

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            var camHead = new Entity(parent: head, model: "cube", scale: new(0.32f, 0.30f, 0.64f), position: new(0, 0.02f, 0.06f), color: cfg.BodyColor);
            foreach (float sx in new[] { -0.19f, 0.19f })
                new Entity(parent: camHead, model: "diamond", scale: new(0.10f, 0.38f, 0.82f), position: new(sx, 0.04f, -0.05f), color: Rgb(35, 90, 195));
            new Entity(parent: camHead, model: "cube", scale: new(0.38f, 0.38f, 0.14f), position: new(0, 0, 0.46f), color: Rgb(24, 28, 38));
            // Одиночный жёлтый оптический глаз V1
            new Entity(parent: camHead, model: "sphere", scale: new(0.34f, 0.36f, 0.20f), position: new(0, 0, 0.52f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: camHead, model: "sphere", scaleUniform: 0.14f, position: new(0, 0, 0.60f), color: Color.White, unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.44f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.44f, 0.25f, 0));

            // Левая рука: Feedbacker (Синяя рука с подброшенной монеткой)
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.32f, 0.32f, 0.32f), position: new(0, 0.06f, 0), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.26f, 0.14f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.30f, 0.02f), color: Color.LightGray);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.15f, 0.15f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.40f), color: Color.LightGray);
            // Парящая золотая монетка (+RICOSHOT)
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.24f, 0.24f, 0.07f), position: new(0, -0.06f, 0.46f), color: Color.Gold, unlit: true);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.10f, position: new(0, -0.06f, 0.46f), color: Color.White, unlit: true);

            // Правая рука: Револьвер Marksman
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.32f, 0.32f, 0.32f), position: new(0, 0.06f, 0), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.26f, 0.14f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.30f, 0.02f), color: Color.LightGray);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.15f, 0.15f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.14f), position: new(0, -0.30f, 0.40f), color: Color.LightGray);

            var rev = new Entity(parent: rShoulder, position: new(0, -0.30f, 0.44f));
            new Entity(parent: rev, model: "cube", scale: new(0.11f, 0.20f, 0.78f), position: new(0, 0.04f, 0.24f), color: Rgb(30, 36, 48));
            new Entity(parent: rev, model: "sphere", scale: new(0.20f, 0.20f, 0.26f), position: new(0, 0.04f, 0.06f), color: Rgb(75, 85, 105));
            new Entity(parent: rev, model: "cube", scale: new(0.09f, 0.05f, 0.70f), position: new(0, 0.16f, 0.24f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: rev, model: "cube", scale: new(0.09f, 0.22f, 0.14f), position: new(0, -0.14f, -0.04f), rotation: new(-20, 0, 0), color: Rgb(35, 40, 52));
            new Entity(parent: rev, model: "diamond", scale: new(0.08f, 0.08f, 0.12f), position: new(0, 0.14f, 0.62f), color: Color.Gold, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 6. SOL BADGUY / KY KISKE [АЛЬТ. ВЕРСИЯ] (Guilty Gear)
        // ==========================================
        private static void BuildSol(Entity root, SurvivorConfig cfg, SurvivorParts parts, int skinIdx = 0)
        {
            if (skinIdx == 2)
            {
                // =========================================================================
                // КАЙ КИСКЕ (ПЕРВЫЙ КОРОЛЬ ИЛЛИРИИ) — АЛЬТЕРНАТИВНАЯ 3D-ВЕРСИЯ СОЛА!
                // Бело-лазурный королевский редингот Святого Ордена с золотыми эполетами,
                // светлые золотистые волосы с хвостиком, крестообразный меч Thunderseal
                // и форма Dragon Install: Весы Юноны (крыло молнии + лазурный глаз Гира)!
                // =========================================================================
                parts.IsKyKiske = true;
                Color kySkin = Rgb(250, 224, 204);
                Color kyWhite = Rgb(242, 245, 255);
                Color kyBlue = Rgb(38, 102, 218);
                Color kyDark = Rgb(24, 30, 46);
                Color kyGold = Rgb(248, 204, 68);
                Color kyBlonde = Rgb(248, 218, 95);
                Color kyCyan = Rgb(95, 238, 255);

                var kyLHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
                var kyRHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
                foreach (var (side, hip) in new[] { (-1, kyLHip), (1, kyRHip) })
                {
                    new Entity(parent: hip, model: "cube", scale: new(0.21f, 0.44f, 0.23f), position: new(0, -0.20f, 0), color: kyDark);
                    new Entity(parent: hip, model: "diamond", scale: new(0.25f, 0.24f, 0.26f), position: new(0, -0.42f, 0.05f), color: kyGold);
                    new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.42f, 0.22f), position: new(0, -0.62f, 0), color: kyWhite);
                    new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.38f, 0.23f), position: new(side * 0.09f, -0.62f, 0), color: kyBlue);
                    new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.16f, 0.40f), position: new(0, -0.80f, 0.06f), color: kyWhite);
                    new Entity(parent: hip, model: "diamond", scale: new(0.20f, 0.10f, 0.16f), position: new(0, -0.80f, 0.22f), color: kyGold);
                }
                parts.LeftLeg = kyLHip;
                parts.RightLeg = kyRHip;

                var kyTorso = new Entity(parent: root, position: new(0, 1.26f, 0));
                // Королевский пояс с золотым крестом Ордена
                new Entity(parent: kyTorso, model: "cube", scale: new(0.58f, 0.18f, 0.38f), position: new(0, -0.32f, 0), color: kyBlue);
                new Entity(parent: kyTorso, model: "diamond", scale: new(0.22f, 0.26f, 0.42f), position: new(0, -0.32f, 0.02f), color: kyGold, unlit: true);
                // Бело-синий мундир Иллирии с золотыми перевязями
                new Entity(parent: kyTorso, model: "cube", scale: new(0.58f, 0.62f, 0.38f), position: new(0, 0.06f, 0), color: kyWhite);
                new Entity(parent: kyTorso, model: "cube", scale: new(0.28f, 0.58f, 0.40f), position: new(0, 0.06f, 0.02f), color: kyDark);
                new Entity(parent: kyTorso, model: "diamond", scale: new(0.18f, 0.42f, 0.42f), position: new(0, 0.10f, 0.03f), color: kyCyan, unlit: true);
                new Entity(parent: kyTorso, model: "cube", scale: new(0.56f, 0.22f, 0.42f), position: new(0, 0.38f, -0.02f), color: kyBlue);
                new Entity(parent: kyTorso, model: "cube", scale: new(0.58f, 0.05f, 0.44f), position: new(0, 0.48f, -0.02f), color: kyGold, unlit: true);

                // Длинные развевающиеся полы рыцарского редингота и плащ Короля Иллирии
                foreach (int s in new[] { -1, 1 })
                {
                    var coatFlap = new Entity(parent: kyTorso, position: new(s * 0.22f, -0.28f, -0.14f), rotation: new(14f, s * 6f, s * -6f));
                    new Entity(parent: coatFlap, model: "diamond", scale: new(0.42f, 0.96f, 0.16f), position: new(0, -0.36f, -0.06f), color: kyWhite);
                    new Entity(parent: coatFlap, model: "diamond", scale: new(0.44f, 0.18f, 0.18f), position: new(0, -0.74f, -0.06f), color: kyBlue);
                    new Entity(parent: coatFlap, model: "cube", scale: new(0.32f, 0.05f, 0.18f), position: new(0, -0.66f, -0.06f), color: kyGold, unlit: true);
                    parts.Tails.Add(coatFlap);
                }

                // Голова Кая Киске со светлыми золотистыми волосами и хвостиком
                var kyHead = new Entity(parent: kyTorso, position: new(0, 0.60f, 0));
                new Entity(parent: kyHead, model: "sphere", scale: new(0.46f, 0.49f, 0.46f), color: kySkin);
                new Entity(parent: kyHead, model: "sphere", scale: new(0.54f, 0.44f, 0.54f), position: new(0, 0.13f, -0.02f), color: kyBlonde);
                foreach (float bx in new[] { -0.15f, 0f, 0.15f })
                    new Entity(parent: kyHead, model: "diamond", scale: new(0.18f, 0.28f, 0.16f), position: new(bx, 0.16f, 0.22f), rotation: new(-14f, 0, bx * -24f), color: kyBlonde);
                foreach (int s in new[] { -1, 1 })
                    new Entity(parent: kyHead, model: "sphere", scaleUniform: 0.09f, position: new(s * 0.11f, 0.04f, 0.22f), color: kyCyan, unlit: true);
                var ponytail = new Entity(parent: kyHead, position: new(0, -0.02f, -0.24f), rotation: new(22f, 0, 0));
                new Entity(parent: ponytail, model: "diamond", scale: new(0.24f, 0.64f, 0.22f), position: new(0, -0.22f, -0.10f), color: kyBlonde);
                new Entity(parent: ponytail, model: "cube", scale: new(0.16f, 0.06f, 0.16f), position: new(0, -0.02f, -0.04f), color: kyBlue);
                parts.Tails.Add(ponytail);

                // Элементы Dragon Install Кая Киске (Весы Юноны): сияющее левое крыло молнии, корона грома и пылающий глаз Гира!
                var kyDiNode = new Entity(parent: kyTorso, enabled: false);
                new Entity(parent: kyDiNode, model: "diamond", scale: new(0.48f, 0.26f, 0.14f), position: new(0, 0.94f, 0.18f), color: kyGold, unlit: true);
                new Entity(parent: kyDiNode, model: "sphere", scaleUniform: 0.15f, position: new(-0.12f, 0.64f, 0.25f), color: Color.Gold, unlit: true);
                foreach (float ang in new[] { 46f, 20f, -6f, -32f })
                {
                    var wingBolt = new Entity(parent: kyDiNode, position: new(-0.34f, 0.26f, -0.24f), rotation: new(0, 14f, ang));
                    new Entity(parent: wingBolt, model: "diamond", scale: new(1.45f, 0.18f, 0.10f), position: new(-0.72f, 0, 0), color: kyCyan, unlit: true);
                    new Entity(parent: wingBolt, model: "diamond", scale: new(0.95f, 0.10f, 0.12f), position: new(-0.56f, 0, 0), color: kyGold, unlit: true);
                }
                parts.UltFormNodes.Add(kyDiNode);

                parts.Head = kyHead;
                parts.Torso = kyTorso;

                var kyLSh = new Entity(parent: kyTorso, position: new(-0.46f, 0.25f, 0));
                var kyRSh = new Entity(parent: kyTorso, position: new(0.46f, 0.25f, 0));
                foreach (var (side, sh) in new[] { (-1, kyLSh), (1, kyRSh) })
                {
                    // Золотые эполеты Святого Ордена
                    new Entity(parent: sh, model: "diamond", scale: new(0.40f, 0.28f, 0.38f), position: new(0, 0.10f, 0), color: kyBlue);
                    new Entity(parent: sh, model: "diamond", scale: new(0.44f, 0.12f, 0.42f), position: new(0, 0.14f, 0), color: kyGold, unlit: true);
                    new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.15f, 0.02f), color: kyWhite);
                    new Entity(parent: sh, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.29f, 0.02f), color: kyBlue);
                    new Entity(parent: sh, model: "cube", scale: new(0.17f, 0.17f, 0.36f), position: new(0, -0.29f, 0.22f), color: kyWhite);
                    new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.16f, 0.15f), position: new(0, -0.29f, 0.41f), color: kyBlue);
                }

                // Правая рука: Священный Меч-Реликвия «Thunderseal» (Громовая Печать / Magnolia Éclair)
                var thunderSword = new Entity(parent: kyRSh, position: new(0, -0.28f, 0.44f), rotation: new(12f, 0, 0));
                // Широкая золотая крестовина с бирюзовым кристаллом Грома
                new Entity(parent: thunderSword, model: "diamond", scale: new(0.48f, 0.12f, 0.20f), position: new(0, 0, 0.10f), color: kyGold, unlit: true);
                new Entity(parent: thunderSword, model: "sphere", scaleUniform: 0.15f, position: new(0, 0, 0.10f), color: kyCyan, unlit: true);
                // Изящный серебряно-лазурный клинок с продольным энергетическим желобом
                new Entity(parent: thunderSword, model: "diamond", scale: new(0.10f, 0.18f, 1.72f), position: new(0, 0, 0.94f), color: kyWhite);
                new Entity(parent: thunderSword, model: "diamond", scale: new(0.12f, 0.07f, 1.62f), position: new(0, 0, 0.90f), color: kyCyan, unlit: true);

                parts.LeftArm = kyLSh;
                parts.RightArm = kyRSh;
                return;
            }

            Color diCrimson = Rgb(125, 22, 36);
            Color diGlow = Rgb(255, 175, 65);
            Color diFlame = Rgb(255, 245, 165);
            Color diJacket = Rgb(145, 34, 32);
            Color diObsidian = Rgb(24, 20, 26);

            var lHip = new Entity(parent: root, position: new(-0.22f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.22f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                // Обычная форма ног
                var normLeg = new Entity(parent: hip);
                new Entity(parent: normLeg, model: "cube", scale: new(0.24f, 0.44f, 0.26f), position: new(0, -0.20f, 0), color: Color.White);
                new Entity(parent: normLeg, model: "cube", scale: new(0.06f, 0.42f, 0.26f), position: new(side * 0.11f, -0.20f, 0), color: Rgb(40, 35, 38));
                new Entity(parent: normLeg, model: "cube", scale: new(0.26f, 0.24f, 0.28f), position: new(0, -0.42f, 0.06f), color: Rgb(40, 35, 38));
                new Entity(parent: normLeg, model: "cube", scale: new(0.12f, 0.08f, 0.06f), position: new(0, -0.42f, 0.18f), color: Color.LightGray);
                new Entity(parent: normLeg, model: "cube", scale: new(0.22f, 0.42f, 0.24f), position: new(0, -0.62f, 0), color: cfg.BodyColor);
                new Entity(parent: normLeg, model: "cube", scale: new(0.26f, 0.18f, 0.42f), position: new(0, -0.80f, 0.06f), color: Rgb(35, 30, 32));
                new Entity(parent: normLeg, model: "diamond", scale: new(0.24f, 0.12f, 0.18f), position: new(0, -0.80f, 0.24f), color: Color.LightGray);
                parts.NormalFormNodes.Add(normLeg);

                // Форма Dragon Install: багрово-огненные мускулистые ноги и когтистые лапы
                var diLeg = new Entity(parent: hip, enabled: false);
                new Entity(parent: diLeg, model: "sphere", scale: new(0.28f, 0.46f, 0.30f), position: new(0, -0.20f, 0), color: diCrimson);
                new Entity(parent: diLeg, model: "diamond", scale: new(0.10f, 0.38f, 0.32f), position: new(0, -0.20f, 0.02f), color: diGlow, unlit: true);
                new Entity(parent: diLeg, model: "sphere", scale: new(0.24f, 0.22f, 0.26f), position: new(0, -0.42f, 0.04f), color: diGlow, unlit: true);
                new Entity(parent: diLeg, model: "diamond", scale: new(0.24f, 0.44f, 0.26f), position: new(0, -0.62f, 0), color: diGlow, unlit: true);
                new Entity(parent: diLeg, model: "diamond", scale: new(0.30f, 0.16f, 0.46f), position: new(0, -0.80f, 0.10f), color: diCrimson);
                foreach (float cx in new[] { -0.10f, -0.03f, 0.03f, 0.10f })
                    new Entity(parent: diLeg, model: "diamond", scale: new(0.05f, 0.08f, 0.20f), position: new(cx, -0.83f, 0.34f), rotation: new(-15, 0, 0), color: diFlame, unlit: true);
                parts.UltFormNodes.Add(diLeg);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));

            // Обычный торс и голова Сола
            var normTorso = new Entity(parent: torso);
            new Entity(parent: normTorso, model: "cube", scale: new(0.62f, 0.20f, 0.42f), position: new(0, -0.32f, 0), color: Rgb(50, 42, 38));
            new Entity(parent: normTorso, model: "cube", scale: new(0.28f, 0.16f, 0.46f), position: new(0, -0.32f, 0.02f), color: Color.Gold, unlit: true);
            new Entity(parent: normTorso, model: "cube", scale: new(0.64f, 0.60f, 0.40f), position: new(0, 0.06f, 0), color: Rgb(235, 185, 155));
            new Entity(parent: normTorso, model: "cube", scale: new(0.68f, 0.58f, 0.20f), position: new(0, 0.08f, -0.16f), color: cfg.BodyColor);
            foreach (float sideX in new[] { -0.32f, 0.32f })
                new Entity(parent: normTorso, model: "cube", scale: new(0.14f, 0.58f, 0.42f), position: new(sideX, 0.08f, 0), color: cfg.BodyColor);
            new Entity(parent: normTorso, model: "cube", scale: new(0.58f, 0.22f, 0.46f), position: new(0, 0.38f, -0.04f), color: cfg.BodyColor);
            new Entity(parent: normTorso, model: "cube", scale: new(0.72f, 0.54f, 0.14f), position: new(0, -0.38f, -0.20f), rotation: new(12, 0, 0), color: cfg.BodyColor);

            var head = new Entity(parent: normTorso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(245, 200, 170));
            new Entity(parent: head, model: "cube", scale: new(0.56f, 0.14f, 0.56f), position: new(0, 0.14f, 0), color: cfg.BodyColor);
            new Entity(parent: head, model: "cube", scale: new(0.26f, 0.12f, 0.08f), position: new(0, 0.14f, 0.27f), color: Color.LightGray);
            foreach (float lx in new[] { -0.12f, 0.12f })
                new Entity(parent: head, model: "diamond", scale: new(0.12f, 0.45f, 0.10f), position: new(lx, 0.02f, -0.32f), rotation: new(28, 0, 0), color: cfg.BodyColor);
            foreach (var (hx, hy, hz, rx) in new[] {
                (0f, 0.34f, -0.16f, 35f), (-0.24f, 0.22f, -0.20f, 30f), (0.24f, 0.22f, -0.20f, 30f),
                (0f, -0.06f, -0.36f, 42f), (-0.20f, 0.28f, 0.08f, -15f), (0.20f, 0.28f, 0.08f, -15f) })
            {
                new Entity(parent: head, model: "diamond", scale: new(0.30f, 0.56f, 0.38f), position: new(hx, hy, hz), rotation: new(rx, 0, 0), color: Rgb(52, 32, 28));
            }
            parts.NormalFormNodes.Add(normTorso);

            // Торс и рогатая голова Dragon Install (по скриншоту)
            var diTorso = new Entity(parent: torso, enabled: false);
            // Багровый рельефный торс со светящимися прожилками пресса
            new Entity(parent: diTorso, model: "cube", scale: new(0.64f, 0.76f, 0.42f), position: new(0, -0.02f, 0), color: diCrimson);
            new Entity(parent: diTorso, model: "cube", scale: new(0.05f, 0.68f, 0.44f), position: new(0, -0.02f, 0.01f), color: diGlow, unlit: true);
            foreach (float ay in new[] { -0.22f, -0.06f, 0.10f })
                new Entity(parent: diTorso, model: "cube", scale: new(0.48f, 0.04f, 0.44f), position: new(0, ay, 0.01f), color: diGlow, unlit: true);
            // Укороченная рваная куртка на груди и плечах с шипами на лацканах и цепью
            new Entity(parent: diTorso, model: "cube", scale: new(0.76f, 0.38f, 0.48f), position: new(0, 0.22f, -0.02f), color: diJacket);
            new Entity(parent: diTorso, model: "diamond", scale: new(0.84f, 0.22f, 0.52f), position: new(0, 0.40f, -0.04f), color: diJacket);
            foreach (float sx in new[] { -0.26f, 0.26f })
            {
                new Entity(parent: diTorso, model: "diamond", scale: new(0.10f, 0.10f, 0.14f), position: new(sx, 0.26f, 0.24f), color: Color.LightGray);
                new Entity(parent: diTorso, model: "diamond", scale: new(0.10f, 0.10f, 0.14f), position: new(sx * 0.75f, 0.12f, 0.24f), color: Color.LightGray);
            }
            // Массивная тёмная цепь на груди слева
            for (int c = 0; c < 3; c++)
                new Entity(parent: diTorso, model: "cube", scale: new(0.12f, 0.16f, 0.08f), position: new(0.12f + c * 0.04f, 0.28f - c * 0.12f, 0.24f), rotation: new(0, 0, -20f), color: diObsidian);
            // Демонический хвост сзади
            var diTail = new Entity(parent: diTorso, position: new(0, -0.32f, -0.22f), rotation: new(-28, 0, 0));
            new Entity(parent: diTail, model: "diamond", scale: new(0.22f, 0.22f, 0.95f), position: new(0, 0, -0.40f), color: diCrimson);
            new Entity(parent: diTail, model: "diamond", scale: new(0.16f, 0.24f, 0.42f), position: new(0, 0.04f, -0.88f), color: diGlow, unlit: true);
            parts.Tails.Add(diTail);
            parts.Head = head;

            // Голова Dragon Install с большими чёрными рогами, пламенем и гривой
            var diHead = new Entity(parent: diTorso, position: new(0, 0.58f, 0.04f));
            new Entity(parent: diHead, model: "diamond", scale: new(0.46f, 0.52f, 0.50f), color: diObsidian);
            new Entity(parent: diHead, model: "cube", scale: new(0.36f, 0.24f, 0.38f), position: new(0, -0.08f, 0.08f), color: diCrimson);
            foreach (float ex in new[] { -0.11f, 0.11f })
                new Entity(parent: diHead, model: "sphere", scaleUniform: 0.10f, position: new(ex, 0.04f, 0.22f), color: diFlame, unlit: true);
            // Главные длинные чёрные рога + бело-жёлтое пламя на вершинах рогов
            foreach (int s in new[] { -1, 1 })
            {
                var horn = new Entity(parent: diHead, position: new(s * 0.16f, 0.26f, 0.02f), rotation: new(18f, 0, s * -22f));
                new Entity(parent: horn, model: "diamond", scale: new(0.18f, 0.88f, 0.24f), position: new(0, 0.34f, 0), color: diObsidian);
                new Entity(parent: horn, model: "diamond", scale: new(0.22f, 0.52f, 0.26f), position: new(s * 0.04f, 0.68f, -0.06f), color: diFlame, unlit: true);
                // Боковые малые рога
                new Entity(parent: diHead, model: "diamond", scale: new(0.12f, 0.42f, 0.14f), position: new(s * 0.28f, 0.18f, -0.04f), rotation: new(-12f, 0, s * -42f), color: diObsidian);
            }
            // Тёмно-багровая грива до пояса
            new Entity(parent: diHead, model: "diamond", scale: new(0.48f, 0.92f, 0.36f), position: new(0, -0.22f, -0.28f), rotation: new(24, 0, 0), color: Rgb(85, 16, 26));
            parts.UltFormNodes.Add(diTorso);
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));

            // Левая рука (Обычная + Dragon Install с кандалами на плече и когтями)
            var normLArm = new Entity(parent: lShoulder);
            new Entity(parent: normLArm, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0.02f), color: Rgb(235, 185, 155));
            new Entity(parent: normLArm, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: Rgb(235, 185, 155));
            new Entity(parent: normLArm, model: "cube", scale: new(0.20f, 0.20f, 0.38f), position: new(0, -0.30f, 0.22f), color: Rgb(40, 35, 38));
            new Entity(parent: normLArm, model: "cube", scale: new(0.22f, 0.10f, 0.20f), position: new(0, -0.30f, 0.22f), color: cfg.BodyColor);
            new Entity(parent: normLArm, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.42f), color: Rgb(40, 35, 38));
            new Entity(parent: normLArm, model: "diamond", scale: new(0.12f, 0.12f, 0.12f), position: new(0, -0.22f, 0.42f), color: Color.Gold, unlit: true);
            parts.NormalFormNodes.Add(normLArm);

            var diLArm = new Entity(parent: lShoulder, enabled: false);
            new Entity(parent: diLArm, model: "cube", scale: new(0.30f, 0.32f, 0.30f), position: new(0, 0.02f, 0), color: diJacket);
            // Металлическое кольцо-кандалы на плече
            new Entity(parent: diLArm, model: "diamond", scale: new(0.36f, 0.10f, 0.36f), position: new(-0.06f, -0.06f, 0), color: Color.LightGray);
            new Entity(parent: diLArm, model: "cube", scale: new(0.24f, 0.28f, 0.24f), position: new(0, -0.20f, 0.04f), color: diJacket);
            new Entity(parent: diLArm, model: "cube", scale: new(0.26f, 0.14f, 0.26f), position: new(0, -0.30f, 0.24f), color: diObsidian);
            new Entity(parent: diLArm, model: "diamond", scale: new(0.10f, 0.10f, 0.14f), position: new(-0.12f, -0.30f, 0.24f), color: Color.LightGray);
            new Entity(parent: diLArm, model: "sphere", scale: new(0.20f, 0.18f, 0.22f), position: new(0, -0.30f, 0.42f), color: diCrimson);
            foreach (float cx in new[] { -0.06f, 0f, 0.06f })
                new Entity(parent: diLArm, model: "diamond", scale: new(0.04f, 0.05f, 0.16f), position: new(cx, -0.32f, 0.54f), color: diFlame, unlit: true);
            parts.UltFormNodes.Add(diLArm);

            // Правая рука: Меч Junkyard Dog (обычный и тёмный монолитный в форме Dragon Install)
            var normRArm = new Entity(parent: rShoulder);
            new Entity(parent: normRArm, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0.02f), color: Rgb(235, 185, 155));
            new Entity(parent: normRArm, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: Rgb(235, 185, 155));
            new Entity(parent: normRArm, model: "cube", scale: new(0.20f, 0.20f, 0.38f), position: new(0, -0.30f, 0.22f), color: Rgb(40, 35, 38));
            new Entity(parent: normRArm, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.42f), color: Rgb(40, 35, 38));

            var sword = new Entity(parent: normRArm, position: new(0, -0.28f, 0.44f), rotation: new(16, 0, 0));
            new Entity(parent: sword, model: "cube", scale: new(0.18f, 0.28f, 0.34f), position: new(0, 0, 0.06f), color: Rgb(55, 60, 70));
            new Entity(parent: sword, model: "cube", scale: new(0.16f, 0.32f, 1.65f), position: new(0, 0, 0.85f), color: cfg.BodyColor);
            new Entity(parent: sword, model: "cube", scale: new(0.18f, 0.14f, 1.58f), position: new(0, 0, 0.56f), color: Rgb(255, 165, 30), unlit: true);
            new Entity(parent: sword, model: "diamond", scale: new(0.14f, 0.22f, 1.70f), position: new(0, -0.14f, 0.88f), color: Color.LightGray);
            foreach (float ez in new[] { 0.45f, 0.85f, 1.25f })
                new Entity(parent: sword, model: "cube", scale: new(0.20f, 0.18f, 0.12f), position: new(0, 0.20f, ez), color: Rgb(40, 42, 50));
            parts.NormalFormNodes.Add(normRArm);

            var diRArm = new Entity(parent: rShoulder, enabled: false);
            new Entity(parent: diRArm, model: "cube", scale: new(0.30f, 0.32f, 0.30f), position: new(0, 0.02f, 0), color: diJacket);
            new Entity(parent: diRArm, model: "cube", scale: new(0.24f, 0.28f, 0.24f), position: new(0, -0.20f, 0.04f), color: diJacket);
            new Entity(parent: diRArm, model: "cube", scale: new(0.26f, 0.14f, 0.26f), position: new(0, -0.30f, 0.24f), color: diObsidian);
            new Entity(parent: diRArm, model: "sphere", scale: new(0.20f, 0.18f, 0.22f), position: new(0, -0.30f, 0.42f), color: diCrimson);

            var diSword = new Entity(parent: diRArm, position: new(0, -0.28f, 0.44f), rotation: new(16, 0, 0));
            new Entity(parent: diSword, model: "cube", scale: new(0.20f, 0.36f, 1.75f), position: new(0, 0, 0.88f), color: diObsidian);
            new Entity(parent: diSword, model: "cube", scale: new(0.22f, 0.10f, 1.68f), position: new(0, 0, 0.88f), color: diGlow, unlit: true);
            parts.UltFormNodes.Add(diRArm);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 7. GWEN (League of Legends)
        // ==========================================
        private static void BuildGwen(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.44f, 0.20f), position: new(0, -0.20f, 0), color: side < 0 ? Rgb(32, 28, 46) : Rgb(248, 225, 220));
                new Entity(parent: hip, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.42f, 0.02f), color: Rgb(248, 225, 220));
                new Entity(parent: hip, model: "cube", scale: new(0.16f, 0.42f, 0.18f), position: new(0, -0.62f, 0), color: Rgb(32, 28, 46));
                new Entity(parent: hip, model: "diamond", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.80f, 0.06f), color: Rgb(32, 28, 46));
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.06f, 0.06f), position: new(0, -0.78f, 0.18f), color: Color.Gold, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            // Готическое платье с пышным кринолином
            new Entity(parent: torso, model: "diamond", scale: new(1.18f, 0.72f, 1.18f), position: new(0, -0.36f, 0), color: Rgb(32, 28, 46));
            new Entity(parent: torso, model: "diamond", scale: new(1.26f, 0.24f, 1.26f), position: new(0, -0.56f, 0), color: Rgb(195, 240, 255), unlit: true);
            new Entity(parent: torso, model: "diamond", scale: new(0.68f, 0.38f, 0.26f), position: new(0, -0.18f, -0.28f), color: cfg.BodyColor, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.52f, 0.54f, 0.36f), position: new(0, 0.08f, 0), color: Rgb(32, 28, 46));
            new Entity(parent: torso, model: "diamond", scale: new(0.44f, 0.38f, 0.22f), position: new(0, 0.12f, 0.12f), color: Rgb(240, 245, 255));
            new Entity(parent: torso, model: "diamond", scale: new(0.16f, 0.16f, 0.16f), position: new(0, 0.16f, 0.20f), color: Color.Gold, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 225, 220));
            // Основная причёска и чёлка Гвен
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.46f, 0.56f), position: new(0, 0.12f, -0.02f), color: Rgb(75, 205, 255));
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.48f, 0.44f), position: new(0, -0.02f, -0.10f), color: Rgb(60, 180, 240));
            foreach (float bx in new[] { -0.14f, 0.0f, 0.14f })
                new Entity(parent: head, model: "diamond", scale: new(0.18f, 0.22f, 0.14f), position: new(bx, 0.16f, 0.22f), rotation: new(-15, 0, bx * -25f), color: Rgb(75, 205, 255));
            // Гетерохромные кукольные глазки (лазурный и пурпурный)
            new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(-0.12f, 0.04f, 0.22f), color: Rgb(90, 220, 255), unlit: true);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(0.12f, 0.04f, 0.22f), color: Rgb(255, 110, 195), unlit: true);
            new Entity(parent: head, model: "diamond", scale: new(0.68f, 0.26f, 0.32f), position: new(0, 0.28f, 0.05f), color: Rgb(32, 28, 46));
            new Entity(parent: head, model: "diamond", scale: new(0.24f, 0.28f, 0.14f), position: new(0, 0.32f, 0.18f), color: Color.Gold, unlit: true);
            // Знаменитые огромные бирюзовые спиральные хвостики Гвен
            foreach (int side in new[] { -1, 1 })
            {
                var drill = new Entity(parent: head, position: new(side * 0.38f, 0.08f, -0.10f), rotation: new(0, 0, side * -18f));
                new Entity(parent: drill, model: "diamond", scale: new(0.32f, 0.20f, 0.24f), position: new(0, 0.05f, 0), color: Rgb(215, 55, 110));
                new Entity(parent: drill, model: "diamond", scale: new(0.38f, 1.05f, 0.38f), position: new(0, -0.40f, 0), color: Rgb(75, 205, 255));
                new Entity(parent: drill, model: "diamond", scale: new(0.44f, 0.52f, 0.44f), position: new(0, -0.66f, 0), color: Rgb(60, 180, 240));
                new Entity(parent: drill, model: "diamond", scale: new(0.28f, 0.48f, 0.28f), position: new(0, -0.92f, 0), color: Rgb(185, 240, 255), unlit: true);
                parts.Tails.Add(drill);
            }
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.44f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.44f, 0.25f, 0));

            // Левая рука: Фарфоровая рука и Парящая Игла
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.28f, position: new(0, 0.04f, 0), color: Rgb(32, 28, 46));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.26f, 0.14f), position: new(0, -0.16f, 0.02f), color: Rgb(248, 225, 220));
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.30f, 0.02f), color: Rgb(248, 225, 220));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(248, 225, 220));
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.20f, 0.20f, 0.16f), position: new(0, -0.30f, 0.36f), color: Rgb(32, 28, 46));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.12f, 0.12f, 0.12f), position: new(0, -0.30f, 0.42f), color: Rgb(248, 225, 220));
            // Игла со священной нитью
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.06f, 0.65f, 0.06f), position: new(0, -0.16f, 0.48f), rotation: new(30, 0, 0), color: Color.Gold, unlit: true);

            // Правая рука: The Hallowed Scissors (Священные Ножницы)
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.28f, position: new(0, 0.04f, 0), color: Rgb(32, 28, 46));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.26f, 0.14f), position: new(0, -0.16f, 0.02f), color: Rgb(248, 225, 220));
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.30f, 0.02f), color: Rgb(248, 225, 220));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(248, 225, 220));
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.20f, 0.20f, 0.16f), position: new(0, -0.30f, 0.36f), color: Rgb(32, 28, 46));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.12f, 0.12f, 0.12f), position: new(0, -0.30f, 0.42f), color: Rgb(248, 225, 220));

            var scis = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f), rotation: new(12, 0, 0));
            new Entity(parent: scis, model: "sphere", scaleUniform: 0.32f, position: new(0, 0, 0.06f), color: Color.Gold, unlit: true);
            foreach (float sy in new[] { -0.18f, 0.18f })
                new Entity(parent: scis, model: "diamond", scale: new(0.12f, 0.32f, 0.34f), position: new(0, sy, -0.14f), color: Color.Gold, unlit: true);
            new Entity(parent: scis, model: "diamond", scale: new(0.12f, 0.26f, 1.75f), position: new(0, 0.16f, 0.85f), rotation: new(-14, 0, 0), color: Rgb(110, 225, 255), unlit: true);
            new Entity(parent: scis, model: "diamond", scale: new(0.12f, 0.26f, 1.75f), position: new(0, -0.12f, 0.85f), rotation: new(14, 0, 0), color: Rgb(195, 245, 255), unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 8. ENFORCER (Risk of Rain 1 / Modded)
        // ==========================================
        private static void BuildEnforcer(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.25f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.25f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.44f, 0.28f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.08f, 0.38f, 0.28f), position: new(side * 0.12f, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.26f, 0.30f), position: new(0, -0.42f, 0.08f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.12f, 0.10f, 0.08f), position: new(0, -0.42f, 0.22f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.42f, 0.26f), position: new(0, -0.62f, 0), color: Rgb(30, 36, 48));
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.34f, 0.12f), position: new(0, -0.60f, 0.11f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.30f, 0.18f, 0.46f), position: new(0, -0.80f, 0.08f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.32f, 0.06f, 0.48f), position: new(0, -0.87f, 0.08f), color: Rgb(20, 24, 32));
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.22f, 0.46f), position: new(0, -0.32f, 0), color: Rgb(28, 34, 46));
            new Entity(parent: torso, model: "cube", scale: new(0.76f, 0.64f, 0.48f), position: new(0, 0.08f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.72f, 0.52f, 0.22f), position: new(0, 0.12f, 0.18f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.88f, 0.28f, 0.58f), position: new(0, 0.34f, -0.02f), color: cfg.ArmorColor);
            // Проблесковые полицейские маячки на плечах
            new Entity(parent: torso, model: "sphere", scale: new(0.22f, 0.16f, 0.22f), position: new(-0.38f, 0.52f, 0), color: Color.SkyBlue, unlit: true);
            new Entity(parent: torso, model: "sphere", scale: new(0.22f, 0.16f, 0.22f), position: new(0.38f, 0.52f, 0), color: Color.Red, unlit: true);

            var pack = new Entity(parent: torso, position: new(0, 0.10f, -0.30f));
            new Entity(parent: pack, model: "cube", scale: new(0.56f, 0.58f, 0.26f), color: cfg.ArmorColor);
            new Entity(parent: pack, model: "cube", scale: new(0.38f, 0.40f, 0.16f), position: new(0, 0, -0.12f), color: Rgb(25, 30, 40));

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "cube", scale: new(0.64f, 0.58f, 0.64f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "cube", scale: new(0.66f, 0.16f, 0.66f), position: new(0, 0.28f, 0), color: cfg.BodyColor);
            new Entity(parent: head, model: "cube", scale: new(0.56f, 0.18f, 0.40f), position: new(0, 0.04f, 0.16f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "cube", scale: new(0.58f, 0.05f, 0.42f), position: new(0, 0.04f, 0.17f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "cube", scale: new(0.28f, 0.16f, 0.22f), position: new(0, -0.20f, 0.22f), color: Rgb(30, 36, 48));
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.52f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.52f, 0.25f, 0));

            // Левая рука: Тяжёлый Бастионный Щит
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.58f, 0.52f, 0.58f), position: new(0, 0.08f, 0), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.22f, 0.30f, 0.22f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.20f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.22f, 0.22f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.20f, 0.20f, 0.16f), position: new(0, -0.30f, 0.42f), color: Rgb(30, 36, 48));

            var shield = new Entity(parent: lShoulder, position: new(-0.28f, -0.10f, 0.42f), rotation: new(0, -22f, 0));
            new Entity(parent: shield, model: "cube", scale: new(1.45f, 1.95f, 0.18f), color: cfg.ArmorColor);
            new Entity(parent: shield, model: "cube", scale: new(1.52f, 2.02f, 0.08f), position: new(0, 0, -0.04f), color: cfg.BodyColor);
            new Entity(parent: shield, model: "cube", scale: new(0.95f, 0.32f, 0.20f), position: new(0, 0.44f, 0), color: cfg.VisorColor, unlit: true);
            new Entity(parent: shield, model: "diamond", scale: new(0.58f, 0.80f, 0.22f), position: new(0, -0.24f, 0), color: cfg.VisorColor, unlit: true);
            foreach (float cy in new[] { -0.82f, 0.82f })
                new Entity(parent: shield, model: "cube", scale: new(1.48f, 0.16f, 0.22f), position: new(0, cy, 0), color: Rgb(85, 95, 115));
            var aura = new Entity(parent: shield, model: "cube", scale: new(1.85f, 2.25f, 0.08f), position: new(0, 0, 0.14f), color: Rgb(70, 210, 255, 110), unlit: true, enabled: false);
            parts.Shield = shield;
            parts.ShieldAura = aura;

            // Правая рука: Дробовик Riot Shotgun
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.52f, 0.46f, 0.52f), position: new(0, 0.08f, 0), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.20f, 0.20f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.42f), color: Rgb(30, 36, 48));

            var sg = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: sg, model: "cube", scale: new(0.20f, 0.28f, 1.25f), position: new(0, 0.04f, 0.42f), color: Rgb(30, 36, 46));
            new Entity(parent: sg, model: "cube", scale: new(0.24f, 0.18f, 0.70f), position: new(0, -0.06f, 0.52f), color: cfg.BodyColor);
            new Entity(parent: sg, model: "cube", scale: new(0.16f, 0.10f, 1.10f), position: new(0, 0.18f, 0.42f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: sg, model: "cube", scale: new(0.12f, 0.22f, 0.16f), position: new(0, -0.14f, 0.05f), rotation: new(-20, 0, 0), color: Rgb(25, 30, 38));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 9. SCOUT (Deep Rock Galactic)
        // ==========================================
        private static void BuildScout(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.22f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.22f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.44f, 0.28f), position: new(0, -0.20f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.22f, 0.30f), position: new(0, -0.42f, 0.06f), color: Rgb(55, 65, 82));
                new Entity(parent: hip, model: "cube", scale: new(0.12f, 0.08f, 0.06f), position: new(0, -0.42f, 0.20f), color: Color.Yellow, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.42f, 0.26f), position: new(0, -0.62f, 0), color: Rgb(42, 48, 60));
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.18f, 0.44f), position: new(0, -0.80f, 0.08f), color: Rgb(65, 48, 35));
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.22f, 0.46f), position: new(0, -0.32f, 0), color: Rgb(75, 52, 38));
            new Entity(parent: torso, model: "cube", scale: new(0.24f, 0.16f, 0.48f), position: new(0, -0.32f, 0.02f), color: Color.Gold, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.74f, 0.62f, 0.48f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.48f, 0.22f), position: new(0, 0.10f, 0.18f), color: cfg.ArmorColor);
            // Патронташ со светящимися сигнальными шашками (Flares)
            foreach (float bx in new[] { -0.24f, -0.08f, 0.08f, 0.24f })
                new Entity(parent: torso, model: "cube", scale: new(0.08f, 0.22f, 0.10f), position: new(bx, -0.16f, 0.26f), color: Color.Yellow, unlit: true);

            var pack = new Entity(parent: torso, position: new(0, 0.10f, -0.30f));
            new Entity(parent: pack, model: "cube", scale: new(0.52f, 0.54f, 0.26f), color: cfg.ArmorColor);
            new Entity(parent: pack, model: "sphere", scaleUniform: 0.16f, position: new(0, 0, -0.14f), color: Color.SkyBlue, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.48f, 0.56f), position: new(0, 0.08f, 0), color: Rgb(245, 185, 40));
            new Entity(parent: head, model: "cube", scale: new(0.50f, 0.08f, 0.36f), position: new(0, 0.16f, 0.20f), color: Rgb(55, 65, 82));
            // Мощный налобный фонарь шахтёра
            new Entity(parent: head, model: "sphere", scale: new(0.24f, 0.24f, 0.22f), position: new(0, 0.24f, 0.28f), color: Color.White, unlit: true);
            new Entity(parent: head, model: "cube", scale: new(0.50f, 0.14f, 0.32f), position: new(0, 0.02f, 0.16f), color: Rgb(60, 225, 255), unlit: true);

            // Пышная рыжая борода Скаута с золотыми кольцами
            var beard = new Entity(parent: head, position: new(0, -0.22f, 0.18f));
            new Entity(parent: beard, model: "diamond", scale: new(0.66f, 0.76f, 0.46f), color: Rgb(225, 95, 25));
            new Entity(parent: beard, model: "diamond", scale: new(0.42f, 0.62f, 0.38f), position: new(-0.22f, -0.08f, -0.04f), color: Rgb(205, 80, 20));
            new Entity(parent: beard, model: "diamond", scale: new(0.42f, 0.62f, 0.38f), position: new(0.22f, -0.08f, -0.04f), color: Rgb(205, 80, 20));
            new Entity(parent: beard, model: "cube", scale: new(0.22f, 0.08f, 0.22f), position: new(0, -0.26f, 0), color: Color.Gold, unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));

            // Левая рука: Крюк-кошка (Grappling Hook)
            new Entity(parent: lShoulder, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.20f, 0.20f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.40f), color: Rgb(75, 52, 38));

            var grap = new Entity(parent: lShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: grap, model: "cube", scale: new(0.20f, 0.24f, 0.72f), position: new(0, 0.04f, 0.22f), color: Rgb(235, 175, 40));
            new Entity(parent: grap, model: "sphere", scale: new(0.26f, 0.26f, 0.24f), position: new(0, 0.04f, 0.06f), color: Rgb(65, 75, 90));
            new Entity(parent: grap, model: "diamond", scale: new(0.42f, 0.18f, 0.42f), position: new(0, 0.04f, 0.60f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: grap, model: "diamond", scale: new(0.18f, 0.42f, 0.42f), position: new(0, 0.04f, 0.60f), color: Color.SkyBlue, unlit: true);

            // Правая рука: Штурмовая винтовка Deepcore GK2
            new Entity(parent: rShoulder, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.20f, 0.20f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.16f), position: new(0, -0.30f, 0.40f), color: Rgb(75, 52, 38));

            var gk2 = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: gk2, model: "cube", scale: new(0.18f, 0.26f, 1.15f), position: new(0, 0.04f, 0.38f), color: Rgb(45, 52, 68));
            new Entity(parent: gk2, model: "cube", scale: new(0.14f, 0.32f, 0.22f), position: new(0, -0.18f, 0.30f), color: Rgb(245, 185, 40));
            new Entity(parent: gk2, model: "cube", scale: new(0.20f, 0.12f, 0.95f), position: new(0, 0.16f, 0.40f), color: Rgb(35, 130, 235), unlit: true);
            new Entity(parent: gk2, model: "cube", scale: new(0.12f, 0.22f, 0.14f), position: new(0, -0.14f, 0.04f), rotation: new(-20, 0, 0), color: Rgb(35, 40, 52));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 10. M4A1 (Girls' Frontline)
        // ==========================================
        private static void BuildM4A1(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.44f, 0.20f), position: new(0, -0.20f, 0), color: Rgb(35, 38, 42));
                new Entity(parent: hip, model: "cube", scale: new(0.16f, 0.42f, 0.18f), position: new(0, -0.62f, 0), color: Rgb(35, 38, 42));
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.18f, 0.38f), position: new(0, -0.80f, 0.06f), color: Rgb(48, 52, 58));
                new Entity(parent: hip, model: "cube", scale: new(0.04f, 0.12f, 0.04f), position: new(0, -0.74f, 0.20f), color: cfg.VisorColor, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "diamond", scale: new(0.98f, 0.72f, 0.92f), position: new(0, -0.38f, 0), color: Rgb(42, 46, 50));
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.54f, 0.38f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.66f, 0.22f, 0.48f), position: new(0, 0.32f, 0), color: Rgb(65, 110, 82));
            new Entity(parent: torso, model: "cube", scale: new(0.50f, 0.42f, 0.18f), position: new(0, 0.08f, 0.16f), color: cfg.ArmorColor);

            // Тактический рюкзак с пушкой Jupiter Cannon на спине
            var pack = new Entity(parent: torso, position: new(0, 0.10f, -0.28f));
            new Entity(parent: pack, model: "cube", scale: new(0.48f, 0.54f, 0.24f), color: cfg.ArmorColor);
            var jup = new Entity(parent: pack, position: new(0.28f, 0.32f, -0.14f), rotation: new(-18, 0, 0));
            new Entity(parent: jup, model: "cube", scale: new(0.26f, 1.15f, 0.30f), position: new(0, 0.28f, 0), color: Rgb(48, 54, 62));
            new Entity(parent: jup, model: "cube", scale: new(0.14f, 1.05f, 0.34f), position: new(0, 0.30f, 0), color: cfg.VisorColor, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 222, 205));
            new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(-0.12f, 0.03f, 0.22f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(0.12f, 0.03f, 0.22f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.42f, 0.56f), position: new(0, 0.14f, 0), color: Rgb(155, 135, 115));
            new Entity(parent: head, model: "diamond", scale: new(0.14f, 0.48f, 0.16f), position: new(-0.14f, -0.05f, 0.24f), rotation: new(0, 0, 12f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "diamond", scale: new(0.26f, 0.95f, 0.26f), position: new(0.26f, -0.24f, -0.16f), rotation: new(18, 0, 0), color: Rgb(145, 125, 105));
            // Тактическая гарнитура на ушах
            foreach (int side in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "cube", scale: new(0.12f, 0.26f, 0.24f), position: new(side * 0.28f, 0.04f, 0.02f), color: Rgb(40, 44, 52));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.09f, position: new(side * 0.33f, 0.04f, 0.02f), color: cfg.VisorColor, unlit: true);
            }
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.46f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.46f, 0.25f, 0));

            // Левая рука: Зелёная тактическая повязка AR Team и перчатка
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.10f, 0.18f), position: new(0, -0.12f, 0.02f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.15f, position: new(0, -0.30f, 0.02f), color: Rgb(35, 38, 42));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(35, 38, 42));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: Rgb(35, 38, 42));

            // Правая рука: Карабин M4A1
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.15f, position: new(0, -0.30f, 0.02f), color: Rgb(35, 38, 42));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(35, 38, 42));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: Rgb(35, 38, 42));

            var rifle = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: rifle, model: "cube", scale: new(0.14f, 0.24f, 1.25f), position: new(0, 0.04f, 0.40f), color: Rgb(32, 35, 40));
            new Entity(parent: rifle, model: "cube", scale: new(0.10f, 0.32f, 0.20f), position: new(0, -0.18f, 0.34f), color: Rgb(50, 56, 52));
            new Entity(parent: rifle, model: "cube", scale: new(0.16f, 0.15f, 0.34f), position: new(0, 0.18f, 0.32f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: rifle, model: "cube", scale: new(0.10f, 0.22f, 0.14f), position: new(0, -0.14f, 0.06f), rotation: new(-20, 0, 0), color: Rgb(35, 38, 42));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 11. MEDIC (Team Fortress 2)
        // ==========================================
        private static void BuildMedic(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.44f, 0.24f), position: new(0, -0.20f, 0), color: Rgb(45, 48, 55));
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.42f, 0.24f), position: new(0, -0.62f, 0), color: Rgb(30, 32, 36));
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.18f, 0.40f), position: new(0, -0.80f, 0.06f), color: Rgb(25, 26, 30));
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.06f, 0.06f), position: new(0, -0.74f, 0.18f), color: Color.LightGray);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            // Белый медицинский халат с красным галстуком и фалдами
            new Entity(parent: torso, model: "cube", scale: new(0.78f, 0.72f, 0.48f), position: new(0, -0.38f, 0), color: Rgb(235, 238, 245));
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.62f, 0.44f), position: new(0, 0.06f, 0), color: Rgb(235, 238, 245));
            new Entity(parent: torso, model: "cube", scale: new(0.32f, 0.48f, 0.16f), position: new(0, 0.08f, 0.18f), color: Rgb(45, 48, 55));
            new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.44f, 0.46f), position: new(0, 0.10f, 0.03f), color: Color.Red, unlit: true);

            // Знаменитый лечебный ранец Медика (Über-Pack)
            var pack = new Entity(parent: torso, position: new(0, 0.10f, -0.30f));
            new Entity(parent: pack, model: "cube", scale: new(0.38f, 0.72f, 0.38f), position: new(0.16f, 0.08f, 0), color: Rgb(220, 45, 45), unlit: true);
            new Entity(parent: pack, model: "cube", scale: new(0.26f, 0.65f, 0.30f), position: new(-0.16f, 0.06f, 0), color: Rgb(255, 195, 55), unlit: true);
            new Entity(parent: pack, model: "sphere", scaleUniform: 0.18f, position: new(0.16f, 0.34f, -0.16f), color: Color.White, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.52f, 0.48f), color: Rgb(245, 205, 180));
            new Entity(parent: head, model: "cube", scale: new(0.52f, 0.18f, 0.52f), position: new(0, 0.22f, 0), color: Rgb(35, 30, 32));
            new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.24f, 0.22f), position: new(0.08f, 0.32f, 0.20f), rotation: new(0, 0, -20f), color: Rgb(35, 30, 32));
            // Круглые очки Медика
            new Entity(parent: head, model: "cube", scale: new(0.46f, 0.04f, 0.10f), position: new(0, 0.05f, 0.22f), color: Rgb(40, 40, 45));
            new Entity(parent: head, model: "sphere", scale: new(0.18f, 0.16f, 0.10f), position: new(-0.12f, 0.04f, 0.24f), color: Color.White, unlit: true);
            new Entity(parent: head, model: "sphere", scale: new(0.18f, 0.16f, 0.10f), position: new(0.12f, 0.04f, 0.24f), color: Color.White, unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));

            // Левая рука: Medi Gun
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.16f, 0.02f), color: Rgb(235, 238, 245));
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: Rgb(205, 38, 45));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(205, 38, 45));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.14f), position: new(0, -0.30f, 0.40f), color: Rgb(205, 38, 45));

            var medigun = new Entity(parent: lShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: medigun, model: "cube", scale: new(0.24f, 0.26f, 1.05f), position: new(0, 0.04f, 0.32f), color: Rgb(70, 75, 85));
            new Entity(parent: medigun, model: "cube", scale: new(0.28f, 0.28f, 0.28f), position: new(0, 0.04f, 0.68f), color: Rgb(185, 195, 210));
            new Entity(parent: medigun, model: "sphere", scaleUniform: 0.34f, position: new(0, 0.04f, 0.86f), color: Color.Red, unlit: true);

            // Правая рука: Syringe Gun
            new Entity(parent: rShoulder, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.16f, 0.02f), color: Rgb(235, 238, 245));
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.30f, 0.02f), color: Rgb(205, 38, 45));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.30f, 0.22f), color: Rgb(205, 38, 45));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.14f), position: new(0, -0.30f, 0.40f), color: Rgb(205, 38, 45));

            var syrGun = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: syrGun, model: "cube", scale: new(0.20f, 0.24f, 0.90f), position: new(0, 0.04f, 0.30f), color: Rgb(60, 65, 75));
            new Entity(parent: syrGun, model: "sphere", scale: new(0.32f, 0.32f, 0.54f), position: new(0, 0.20f, 0.30f), color: Color.Red, unlit: true);
            new Entity(parent: syrGun, model: "diamond", scale: new(0.08f, 0.20f, 0.68f), position: new(0, -0.12f, 0.58f), color: Rgb(220, 230, 245));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 12. AIGIS (Persona 3)
        // ==========================================
        private static void BuildAigis(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.44f, 0.20f), position: new(0, -0.20f, 0), color: Color.White);
                new Entity(parent: hip, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.42f, 0.02f), color: Color.Gold, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.16f, 0.42f, 0.18f), position: new(0, -0.62f, 0), color: Color.White);
                new Entity(parent: hip, model: "diamond", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.80f, 0.06f), color: Color.Gold, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.50f, 0.18f, 0.36f), position: new(0, -0.32f, 0), color: Color.Gold, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.58f, 0.56f, 0.38f), position: new(0, 0.06f, 0), color: Color.White);
            // Красный бант Айгис
            new Entity(parent: torso, model: "diamond", scale: new(0.58f, 0.24f, 0.28f), position: new(0, 0.34f, 0.20f), color: Rgb(235, 45, 60), unlit: true);
            // Золотые пластины Orgia Mode на спине
            foreach (int side in new[] { -1, 1 })
                new Entity(parent: torso, model: "diamond", scale: new(0.88f, 0.26f, 0.18f), position: new(side * 0.48f, 0.14f, -0.22f), rotation: new(0, 0, side * 18f), color: Rgb(235, 195, 55));

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 228, 212));
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.45f, 0.56f), position: new(0, 0.10f, 0), color: Rgb(245, 205, 65));
            new Entity(parent: head, model: "cube", scale: new(0.58f, 0.08f, 0.56f), position: new(0, 0.16f, 0), color: Rgb(55, 65, 85));
            new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(-0.12f, 0.02f, 0.22f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.11f, position: new(0.12f, 0.02f, 0.22f), color: Color.SkyBlue, unlit: true);
            // Механические датчики-наушники Айгис
            foreach (int side in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.38f, 0.38f), position: new(side * 0.30f, 0.02f, 0), color: Rgb(225, 230, 240));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.14f, position: new(side * 0.35f, 0.02f, 0), color: Color.SkyBlue, unlit: true);
            }
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.46f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.46f, 0.25f, 0));

            // Левая рука: Пушка Pandora
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.26f, position: new(0, 0.04f, 0), color: Color.Gold, unlit: true);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.26f, 0.16f), position: new(0, -0.16f, 0.02f), color: Color.White);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: Color.Gold, unlit: true);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: Color.White);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: Color.Gold, unlit: true);

            var cannon = new Entity(parent: lShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: cannon, model: "cube", scale: new(0.30f, 0.30f, 0.95f), position: new(0, 0.04f, 0.30f), color: Rgb(235, 195, 55));
            new Entity(parent: cannon, model: "sphere", scaleUniform: 0.28f, position: new(0, 0.04f, 0.78f), color: Color.Orange, unlit: true);

            // Правая рука: Пулемётные пальцы Finger Vulcan
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.26f, position: new(0, 0.04f, 0), color: Color.Gold, unlit: true);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.26f, 0.16f), position: new(0, -0.16f, 0.02f), color: Color.White);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: Color.Gold, unlit: true);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: Color.White);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.14f), position: new(0, -0.30f, 0.40f), color: Color.Gold, unlit: true);

            var barrel = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: barrel, model: "cube", scale: new(0.26f, 0.20f, 0.70f), position: new(0, 0.04f, 0.25f), color: Color.SkyBlue, unlit: true);
            foreach (float fx in new[] { -0.09f, -0.04f, 0.0f, 0.04f, 0.09f })
                new Entity(parent: barrel, model: "cube", scale: new(0.04f, 0.06f, 0.42f), position: new(fx, 0.04f, 0.62f), color: Rgb(235, 195, 55));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 13. HAKUREI REIMU (Touhou Project)
        // ==========================================
        private static void BuildReimu(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.44f, 0.20f), position: new(0, -0.20f, 0), color: Rgb(248, 225, 215));
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.20f), position: new(0, -0.62f, 0), color: Color.White);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.16f, 0.36f), position: new(0, -0.80f, 0.06f), color: cfg.BodyColor);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            // Красно-белое традиционное жреческое кимоно Рейму
            new Entity(parent: torso, model: "diamond", scale: new(1.14f, 0.76f, 1.14f), position: new(0, -0.38f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "diamond", scale: new(1.18f, 0.20f, 1.18f), position: new(0, -0.64f, 0), color: Color.White, unlit: true);
            new Entity(parent: torso, model: "cube", scale: new(0.52f, 0.52f, 0.38f), position: new(0, 0.04f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.14f, 0.42f), position: new(0, -0.18f, 0), color: Color.White);
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.28f, 0.48f), position: new(0, 0.22f, 0), color: Color.White);
            new Entity(parent: torso, model: "diamond", scale: new(0.40f, 0.20f, 0.26f), position: new(0, 0.28f, 0.22f), color: Color.Yellow, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: Rgb(248, 225, 215));
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.44f, 0.56f), position: new(0, 0.12f, 0), color: Rgb(38, 28, 36));
            // Боковые пряди в белой бумажной обёртке (сараси)
            foreach (int side in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "cube", scale: new(0.14f, 0.40f, 0.14f), position: new(side * 0.28f, -0.10f, 0.08f), color: Color.White);
                new Entity(parent: head, model: "diamond", scale: new(0.16f, 0.26f, 0.16f), position: new(side * 0.28f, -0.34f, 0.08f), color: Rgb(38, 28, 36));
            }
            // Огромный фирменный красный бант Рейму
            var bowKnot = new Entity(parent: head, position: new(0, 0.20f, -0.26f), rotation: new(15, 0, 0));
            new Entity(parent: bowKnot, model: "diamond", scale: new(1.15f, 0.50f, 0.24f), color: cfg.BodyColor, unlit: true);
            new Entity(parent: bowKnot, model: "diamond", scale: new(0.45f, 0.70f, 0.20f), position: new(0, -0.30f, 0), color: cfg.BodyColor, unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            // Вращающиеся сферы Инь-Ян Хакурей
            var halo = new Entity(parent: torso, position: new(0, 0.35f, -0.36f));
            foreach (var (side, colOrb) in new[] { (-1, cfg.BodyColor), (1, Color.White) })
            {
                var orb = new Entity(parent: halo, model: "sphere", scaleUniform: 0.42f, position: new(side * 0.72f, 0, 0), color: colOrb, unlit: true);
                new Entity(parent: orb, model: "diamond", scale: new(1.50f, 0.20f, 1.50f), color: Color.Gold, unlit: true);
            }
            parts.Halo = halo;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));

            // Левая рука: Белый пышный отдельный рукав и Офуда
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.44f, 0.52f, 0.44f), position: new(0, -0.10f, 0.08f), color: Color.White);
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.36f, 0.68f, 0.46f), position: new(0, -0.32f, 0.12f), color: Color.White);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.24f, 0.14f), position: new(0, -0.16f, 0.02f), color: Rgb(248, 225, 215));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.36f), position: new(0, -0.28f, 0.20f), color: Rgb(248, 225, 215));
            new Entity(parent: lShoulder, model: "cube", scale: new(0.12f, 0.12f, 0.12f), position: new(0, -0.28f, 0.38f), color: Rgb(248, 225, 215));
            // Карты Офуда
            foreach (float oy in new[] { -0.10f, 0.0f, 0.10f })
            {
                new Entity(parent: lShoulder, model: "cube", scale: new(0.24f, 0.03f, 0.46f), position: new(0, -0.26f + oy * 0.5f, 0.52f), rotation: new(0, oy * 120f, 0), color: Color.White, unlit: true);
                new Entity(parent: lShoulder, model: "cube", scale: new(0.10f, 0.04f, 0.38f), position: new(0, -0.26f + oy * 0.5f, 0.52f), rotation: new(0, oy * 120f, 0), color: Rgb(245, 50, 65), unlit: true);
            }

            // Правая рука: Белый рукав и Жезл Гохэй с лентами сидэ
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.44f, 0.52f, 0.44f), position: new(0, -0.10f, 0.08f), color: Color.White);
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.36f, 0.68f, 0.46f), position: new(0, -0.32f, 0.12f), color: Color.White);
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.24f, 0.14f), position: new(0, -0.16f, 0.02f), color: Rgb(248, 225, 215));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.14f, 0.14f, 0.36f), position: new(0, -0.28f, 0.20f), color: Rgb(248, 225, 215));
            new Entity(parent: rShoulder, model: "cube", scale: new(0.12f, 0.12f, 0.12f), position: new(0, -0.28f, 0.38f), color: Rgb(248, 225, 215));

            var gohei = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f), rotation: new(20, 0, 0));
            new Entity(parent: gohei, model: "cube", scale: new(0.08f, 0.08f, 1.45f), position: new(0, 0, 0.45f), color: Rgb(140, 85, 45));
            new Entity(parent: gohei, model: "diamond", scale: new(0.46f, 0.58f, 0.46f), position: new(0, 0, 1.15f), color: Color.White, unlit: true);
            new Entity(parent: gohei, model: "diamond", scale: new(0.36f, 0.44f, 0.36f), position: new(0, -0.22f, 1.05f), color: cfg.BodyColor, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 14. MINDUSTRY GAMMA CORE SHIP
        // ==========================================
        private static void BuildMindustry(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var leftHip = new Entity(parent: root, position: new(-0.36f, 0.85f, -0.28f));
            var rightHip = new Entity(parent: root, position: new(0.36f, 0.85f, -0.28f));
            foreach (var hip in new[] { leftHip, rightHip })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.30f, 0.28f, 0.56f), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.34f, 0.12f, 0.38f), position: new(0, 0.14f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.32f, 0.32f, 0.68f), position: new(0, 0, -0.38f), color: Color.SkyBlue, unlit: true);
                new Entity(parent: hip, model: "diamond", scale: new(0.20f, 0.20f, 0.90f), position: new(0, 0, -0.54f), color: Color.White, unlit: true);
            }
            parts.LeftLeg = leftHip;
            parts.RightLeg = rightHip;

            var mTorso = new Entity(parent: root, position: new(0, 1.35f, 0));
            new Entity(parent: mTorso, model: "diamond", scale: new(1.45f, 0.48f, 1.88f), color: cfg.BodyColor);
            new Entity(parent: mTorso, model: "diamond", scale: new(1.02f, 0.52f, 2.05f), position: new(0, 0, 0.10f), color: cfg.ArmorColor);
            new Entity(parent: mTorso, model: "cube", scale: new(0.78f, 0.42f, 1.15f), position: new(0, 0.12f, 0), color: cfg.ArmorColor);
            new Entity(parent: mTorso, model: "cube", scale: new(0.56f, 0.18f, 1.45f), position: new(0, -0.18f, 0), color: Rgb(40, 45, 58));
            // Реактор ядра и мостик
            new Entity(parent: mTorso, model: "diamond", scale: new(0.55f, 0.42f, 0.85f), position: new(0, 0.30f, 0.16f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: mTorso, model: "sphere", scaleUniform: 0.32f, position: new(0, 0.34f, 0.20f), color: Color.White, unlit: true);
            foreach (float sx in new[] { -0.24f, 0.24f })
            {
                new Entity(parent: mTorso, model: "diamond", scale: new(0.16f, 0.18f, 0.72f), position: new(sx, -0.02f, 0.96f), color: cfg.BodyColor);
                new Entity(parent: mTorso, model: "sphere", scaleUniform: 0.14f, position: new(sx, -0.02f, 1.24f), color: Color.Yellow, unlit: true);
            }

            // Стреловидные крылья с солнечными батареями и пушками
            foreach (int side in new[] { -1, 1 })
            {
                var wing = new Entity(parent: mTorso, model: "diamond", scale: new(1.38f, 0.20f, 1.15f),
                    position: new(side * 0.76f, -0.02f, -0.08f), rotation: new(0, 0, side * -10f), color: cfg.BodyColor);
                new Entity(parent: wing, model: "cube", scale: new(0.64f, 0.16f, 0.80f), position: new(side * 0.14f, 0.05f, 0.05f), color: cfg.ArmorColor);
                new Entity(parent: wing, model: "cube", scale: new(0.16f, 0.18f, 1.02f), position: new(side * 0.30f, 0.02f, 0.38f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: wing, model: "diamond", scale: new(0.24f, 0.46f, 0.52f), position: new(side * 0.50f, 0.12f, -0.18f), rotation: new(0, 0, side * 22f), color: cfg.ArmorColor);
            }

            var mHalo = new Entity(parent: mTorso, position: new(0, 0.56f, -0.18f));
            new Entity(parent: mHalo, model: "diamond", scale: new(1.15f, 1.15f, 0.08f), rotation: new(90, 0, 0), color: Color.Yellow, unlit: true);
            new Entity(parent: mHalo, model: "diamond", scale: new(0.85f, 0.85f, 0.08f), rotation: new(90, 45, 0), color: Color.SkyBlue, unlit: true);
            parts.Halo = mHalo;
            parts.Torso = mTorso;

            var mLeftSh = new Entity(parent: mTorso, position: new(-0.70f, 0.06f, 0.38f));
            var mRightSh = new Entity(parent: mTorso, position: new(0.70f, 0.06f, 0.38f));
            foreach (var sh in new[] { mLeftSh, mRightSh })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.36f, 0.28f, 0.56f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.16f, 0.98f), position: new(-0.10f, 0, 0.42f), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.16f, 0.98f), position: new(0.10f, 0, 0.42f), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.30f, position: new(0, 0, 0.88f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: sh, model: "diamond", scale: new(0.20f, 0.20f, 0.30f), position: new(0, 0, 0.96f), color: Color.White, unlit: true);
            }
            parts.LeftArm = mLeftSh;
            parts.RightArm = mRightSh;
        }

        // ==========================================
        // 15. SLARK (Dota 2)
        // ==========================================
        private static void BuildSlark(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color deepTeal = Rgb(32, 105, 132);
            Color reefGlow = Rgb(95, 255, 215);
            Color bellyTone = Rgb(155, 225, 215);
            Color leatherDark = Rgb(24, 34, 52);

            var lHip = new Entity(parent: root, position: new(-0.26f, 0.82f, 0));
            var rHip = new Entity(parent: root, position: new(0.26f, 0.82f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                // Мощное мускулистое бедро с бронепластиной
                new Entity(parent: hip, model: "sphere", scale: new(0.32f, 0.46f, 0.34f), position: new(0, -0.18f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.34f, 0.28f, 0.36f), position: new(side * 0.08f, -0.18f, 0.04f), color: cfg.ArmorColor);
                // Наколенник с шипом
                new Entity(parent: hip, model: "diamond", scale: new(0.28f, 0.26f, 0.32f), position: new(0, -0.40f, 0.08f), color: leatherDark);
                new Entity(parent: hip, model: "diamond", scale: new(0.12f, 0.20f, 0.24f), position: new(0, -0.38f, 0.18f), color: reefGlow, unlit: true);
                // Голень и плавник на икре
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.38f, 0.24f), position: new(0, -0.58f, 0.02f), color: deepTeal);
                new Entity(parent: hip, model: "diamond", scale: new(0.08f, 0.28f, 0.24f), position: new(0, -0.56f, -0.14f), rotation: new(-20, 0, 0), color: reefGlow, unlit: true);
                // Когтистая перепончатая лапа
                new Entity(parent: hip, model: "diamond", scale: new(0.34f, 0.16f, 0.50f), position: new(0, -0.76f, 0.14f), color: deepTeal);
                foreach (float cx in new[] { -0.09f, 0f, 0.09f })
                    new Entity(parent: hip, model: "diamond", scale: new(0.06f, 0.08f, 0.18f), position: new(cx, -0.79f, 0.36f), color: Color.White);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.24f, 0), rotation: new(18f, 0, 0));
            // Широкая атлетичная спина и грудная клетка
            new Entity(parent: torso, model: "sphere", scale: new(0.86f, 0.78f, 0.62f), position: new(0, 0.10f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.68f, 0.34f, 0.46f), position: new(0, -0.24f, 0), color: leatherDark);
            new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.24f, 0.52f), position: new(0, -0.24f, 0.04f), color: Color.Gold, unlit: true);
            // Рельефный пресс и светящиеся жабры по бокам
            new Entity(parent: torso, model: "sphere", scale: new(0.62f, 0.64f, 0.28f), position: new(0, 0.04f, 0.22f), color: bellyTone);
            foreach (int side in new[] { -1, 1 })
            {
                foreach (float gy in new[] { -0.04f, 0.08f, 0.20f })
                    new Entity(parent: torso, model: "cube", scale: new(0.06f, 0.04f, 0.18f), position: new(side * 0.38f, gy, 0.12f), rotation: new(0, 0, side * 18f), color: reefGlow, unlit: true);
            }
            // Портупея и броневоротник Тёмного Рифа
            new Entity(parent: torso, model: "diamond", scale: new(0.96f, 0.42f, 0.66f), position: new(0, 0.30f, -0.02f), color: cfg.ArmorColor);
            // Мощный гребень из 6 биолюминесцентных плавников и хвост
            for (int i = 0; i < 6; i++)
                new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.54f - i * 0.05f, 0.30f), position: new(0, 0.38f - i * 0.14f, -0.32f), rotation: new(-36, 0, 0), color: reefGlow, unlit: true);
            var tail = new Entity(parent: torso, position: new(0, -0.34f, -0.24f), rotation: new(-24f, 0, 0));
            new Entity(parent: tail, model: "diamond", scale: new(0.30f, 0.26f, 0.75f), position: new(0, 0, -0.30f), color: cfg.BodyColor);
            new Entity(parent: tail, model: "diamond", scale: new(0.16f, 0.42f, 0.48f), position: new(0, 0.06f, -0.68f), color: reefGlow, unlit: true);
            parts.Tails.Add(tail);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0.16f), rotation: new(-16f, 0, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.62f, 0.54f, 0.72f), color: cfg.BodyColor);
            // Глубокий зубчатый капюшон и нижняя челюсть с клыками
            new Entity(parent: head, model: "diamond", scale: new(0.76f, 0.58f, 0.78f), position: new(0, 0.14f, -0.04f), color: cfg.ArmorColor);
            new Entity(parent: head, model: "cube", scale: new(0.46f, 0.16f, 0.48f), position: new(0, -0.18f, 0.14f), color: bellyTone);
            foreach (float tx in new[] { -0.14f, -0.05f, 0.05f, 0.14f })
                new Entity(parent: head, model: "diamond", scale: new(0.06f, 0.14f, 0.06f), position: new(tx, -0.10f, 0.34f), color: Color.White);
            foreach (float ex in new[] { -0.17f, 0.17f })
            {
                new Entity(parent: head, model: "sphere", scaleUniform: 0.16f, position: new(ex, 0.06f, 0.30f), color: reefGlow, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.07f, position: new(ex, 0.06f, 0.36f), color: Color.White, unlit: true);
            }
            // Боковые жаберные плавники на голове
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "diamond", scale: new(0.20f, 0.42f, 0.28f), position: new(s * 0.36f, 0.08f, -0.02f), rotation: new(0, 0, s * -32f), color: reefGlow, unlit: true);
                new Entity(parent: head, model: "diamond", scale: new(0.14f, 0.32f, 0.22f), position: new(s * 0.32f, -0.08f, -0.06f), rotation: new(0, 0, s * -48f), color: cfg.BodyColor);
            }
            parts.Head = head;
            parts.Torso = torso;

            // Рельефные раскачанные руки с наплечниками, кандалами и плавниками
            var lShoulder = new Entity(parent: torso, position: new(-0.54f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.54f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "diamond", scale: new(0.50f, 0.42f, 0.50f), position: new(0, 0.08f, 0), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "diamond", scale: new(0.20f, 0.36f, 0.22f), position: new(side * 0.18f, 0.24f, 0), rotation: new(0, 0, side * -24f), color: reefGlow, unlit: true);
                // Крупный бицепс и предплечье
                new Entity(parent: sh, model: "sphere", scale: new(0.28f, 0.36f, 0.28f), position: new(0, -0.16f, 0.04f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "sphere", scale: new(0.26f, 0.26f, 0.44f), position: new(0, -0.30f, 0.22f), color: deepTeal);
                // Тюремные кандалы с обрывками цепи и плавники на локтях
                new Entity(parent: sh, model: "cube", scale: new(0.28f, 0.28f, 0.14f), position: new(0, -0.30f, 0.34f), color: Rgb(65, 75, 90));
                new Entity(parent: sh, model: "diamond", scale: new(0.08f, 0.26f, 0.34f), position: new(side * 0.14f, -0.30f, 0.18f), color: reefGlow, unlit: true);
            }
            // Зазубренный клинок-заточка из челюсти акулы в правой руке
            var dagger = new Entity(parent: rShoulder, position: new(0, -0.30f, 0.46f), rotation: new(10f, 0, 0));
            new Entity(parent: dagger, model: "cube", scale: new(0.12f, 0.14f, 0.34f), color: Rgb(75, 52, 38));
            new Entity(parent: dagger, model: "diamond", scale: new(0.32f, 0.12f, 0.18f), position: new(0, 0, 0.16f), color: Color.Gold, unlit: true);
            new Entity(parent: dagger, model: "diamond", scale: new(0.18f, 0.34f, 1.15f), position: new(0, 0, 0.66f), color: reefGlow, unlit: true);
            foreach (float tz in new[] { 0.38f, 0.58f, 0.78f, 0.96f })
                new Entity(parent: dagger, model: "diamond", scale: new(0.10f, 0.18f, 0.14f), position: new(0, 0.16f, tz), rotation: new(-25, 0, 0), color: Color.White, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 16. TIDEHUNTER (Dota 2)
        // ==========================================
        private static void BuildTidehunter(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color shellDark = Rgb(34, 86, 68);
            Color bellyLight = Rgb(182, 222, 168);
            Color abyssGlow = Rgb(95, 255, 205);
            Color ironDark = Rgb(56, 66, 80);

            var lHip = new Entity(parent: root, position: new(-0.38f, 0.88f, 0));
            var rHip = new Entity(parent: root, position: new(0.38f, 0.88f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                // Колоссальные слоновьи ноги Левиафана
                new Entity(parent: hip, model: "sphere", scale: new(0.46f, 0.52f, 0.48f), position: new(0, -0.22f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.40f, 0.44f, 0.42f), position: new(0, -0.58f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "diamond", scale: new(0.44f, 0.24f, 0.46f), position: new(0, -0.42f, 0.08f), color: shellDark);
                new Entity(parent: hip, model: "cube", scale: new(0.46f, 0.22f, 0.58f), position: new(0, -0.80f, 0.10f), color: shellDark);
                foreach (float cx in new[] { -0.14f, 0f, 0.14f })
                    new Entity(parent: hip, model: "diamond", scale: new(0.10f, 0.12f, 0.20f), position: new(cx, -0.84f, 0.40f), color: bellyLight);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.38f, 0));
            // Огромный мускулистый корпус с сегментированным брюхом
            new Entity(parent: torso, model: "sphere", scale: new(1.52f, 1.28f, 1.18f), color: cfg.BodyColor);
            new Entity(parent: torso, model: "sphere", scale: new(1.18f, 1.02f, 0.54f), position: new(0, -0.04f, 0.40f), color: bellyLight);
            foreach (float by in new[] { -0.28f, -0.06f, 0.16f })
                new Entity(parent: torso, model: "cube", scale: new(0.96f, 0.05f, 0.36f), position: new(0, by, 0.48f), color: cfg.BodyColor);

            // Массивный бронированный Панцирь Кракена с шипами и наростами
            var shell = new Entity(parent: torso, position: new(0, 0.10f, -0.34f));
            new Entity(parent: shell, model: "sphere", scale: new(1.48f, 1.32f, 0.82f), color: shellDark);
            new Entity(parent: shell, model: "diamond", scale: new(1.62f, 1.42f, 0.44f), position: new(0, 0, -0.10f), color: Rgb(28, 72, 58));
            foreach (var (sx, sy, sz) in new[] { (0f, 0.42f, -0.36f), (0f, 0.04f, -0.42f), (0f, -0.34f, -0.34f), (-0.48f, 0.18f, -0.28f), (0.48f, 0.18f, -0.28f), (-0.42f, -0.22f, -0.26f), (0.42f, -0.22f, -0.26f) })
                new Entity(parent: shell, model: "diamond", scale: new(0.28f, 0.48f, 0.46f), position: new(sx, sy, sz), rotation: new(-35, 0, sx * -30f), color: abyssGlow, unlit: true);

            // Голова Левиафана с массивной челюстью, клыками и светящимся рогом-приманкой
            var head = new Entity(parent: torso, position: new(0, 0.68f, 0.30f));
            new Entity(parent: head, model: "sphere", scale: new(0.94f, 0.70f, 0.88f), color: cfg.BodyColor);
            new Entity(parent: head, model: "diamond", scale: new(0.86f, 0.34f, 0.76f), position: new(0, 0.24f, -0.04f), color: shellDark);
            new Entity(parent: head, model: "cube", scale: new(0.74f, 0.26f, 0.62f), position: new(0, -0.22f, 0.22f), color: bellyLight);
            foreach (float tx in new[] { -0.26f, 0.26f })
                new Entity(parent: head, model: "diamond", scale: new(0.12f, 0.28f, 0.12f), position: new(tx, -0.06f, 0.46f), color: Color.White);
            new Entity(parent: head, model: "diamond", scale: new(0.22f, 0.52f, 0.22f), position: new(0, 0.38f, 0.28f), rotation: new(28, 0, 0), color: abyssGlow, unlit: true);
            foreach (float ex in new[] { -0.28f, 0.28f })
            {
                new Entity(parent: head, model: "sphere", scaleUniform: 0.18f, position: new(ex, 0.10f, 0.36f), color: Color.Yellow, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.09f, position: new(ex, 0.10f, 0.42f), color: Color.White, unlit: true);
            }
            parts.Head = head;
            parts.Torso = torso;

            // Мощные раскачанные руки-клешни с морскими браслетами и цепями
            var lShoulder = new Entity(parent: torso, position: new(-0.82f, 0.28f, 0.04f));
            var rShoulder = new Entity(parent: torso, position: new(0.82f, 0.28f, 0.04f));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "sphere", scale: new(0.54f, 0.50f, 0.54f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "diamond", scale: new(0.60f, 0.36f, 0.58f), position: new(side * 0.10f, 0.12f, 0), color: shellDark);
                new Entity(parent: sh, model: "diamond", scale: new(0.22f, 0.38f, 0.22f), position: new(side * 0.26f, 0.30f, 0), rotation: new(0, 0, side * -28f), color: abyssGlow, unlit: true);
                new Entity(parent: sh, model: "sphere", scale: new(0.42f, 0.48f, 0.42f), position: new(0, -0.24f, 0.06f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.42f, 0.38f, 0.52f), position: new(0, -0.40f, 0.28f), color: shellDark);
                new Entity(parent: sh, model: "cube", scale: new(0.46f, 0.16f, 0.22f), position: new(0, -0.40f, 0.36f), color: ironDark);
            }

            // Колоссальный Адмиралтейский Якорь Бездны в правой руке
            var anchor = new Entity(parent: rShoulder, position: new(0, -0.38f, 0.52f), rotation: new(16f, 0, 0));
            // Рым-кольцо и поперечный деревянный шток
            new Entity(parent: anchor, model: "diamond", scale: new(0.42f, 0.42f, 0.14f), position: new(0, 0, -0.18f), color: Color.Gold, unlit: true);
            new Entity(parent: anchor, model: "cube", scale: new(0.95f, 0.22f, 0.22f), position: new(0, 0, 0.18f), color: cfg.ArmorColor);
            // Веретено якоря, обвитое цепью
            new Entity(parent: anchor, model: "cube", scale: new(0.24f, 0.24f, 2.15f), position: new(0, 0, 0.82f), color: ironDark);
            foreach (float cz in new[] { 0.45f, 0.85f, 1.25f })
                new Entity(parent: anchor, model: "diamond", scale: new(0.32f, 0.32f, 0.16f), position: new(0, 0, cz), color: abyssGlow, unlit: true);
            // Рога и лапы якоря
            new Entity(parent: anchor, model: "diamond", scale: new(1.75f, 0.38f, 0.62f), position: new(0, 0, 1.75f), color: ironDark);
            new Entity(parent: anchor, model: "diamond", scale: new(0.42f, 0.42f, 0.55f), position: new(0, 0, 2.02f), color: abyssGlow, unlit: true);
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: anchor, model: "diamond", scale: new(0.44f, 0.38f, 0.72f), position: new(s * 0.74f, 0, 1.56f), rotation: new(0, s * -32f, 0), color: abyssGlow, unlit: true);
                new Entity(parent: anchor, model: "diamond", scale: new(0.26f, 0.28f, 0.46f), position: new(s * 0.88f, 0, 1.32f), rotation: new(0, s * -42f, 0), color: Color.White, unlit: true);
            }

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 17. GORDON FREEMAN (Half-Life)
        // ==========================================
        private static void BuildFreeman(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.22f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.22f, 0.86f, 0));
            foreach (var hip in new[] { lHip, rHip })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.44f, 0.26f), position: new(0, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.22f, 0.28f), position: new(0, -0.42f, 0.04f), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.42f, 0.24f), position: new(0, -0.62f, 0), color: cfg.BodyColor);
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.18f, 0.42f), position: new(0, -0.80f, 0.06f), color: cfg.ArmorColor);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.62f, 0.20f, 0.42f), position: new(0, -0.32f, 0), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "cube", scale: new(0.70f, 0.62f, 0.44f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.56f, 0.46f, 0.22f), position: new(0, 0.10f, 0.15f), color: cfg.ArmorColor);
            // Знак Лямбды (λ) на груди костюма HEV Mark IV
            new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.26f, 0.08f), position: new(0, 0.14f, 0.26f), color: cfg.VisorColor, unlit: true);
            // Модуль длинных прыжков на спине
            new Entity(parent: torso, model: "cube", scale: new(0.48f, 0.52f, 0.24f), position: new(0, 0.10f, -0.28f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.18f, position: new(0, 0.12f, -0.40f), color: cfg.VisorColor, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.52f, 0.48f), color: Rgb(245, 205, 178));
            new Entity(parent: head, model: "cube", scale: new(0.52f, 0.20f, 0.52f), position: new(0, 0.20f, -0.02f), color: Rgb(72, 46, 32));
            // Эспаньолка (бородка) Фримана
            new Entity(parent: head, model: "cube", scale: new(0.22f, 0.14f, 0.14f), position: new(0, -0.16f, 0.20f), color: Rgb(72, 46, 32));
            // Очки в чёрной оправе
            new Entity(parent: head, model: "cube", scale: new(0.46f, 0.05f, 0.10f), position: new(0, 0.05f, 0.22f), color: Color.Black);
            foreach (float gx in new[] { -0.12f, 0.12f })
                new Entity(parent: head, model: "cube", scale: new(0.16f, 0.12f, 0.06f), position: new(gx, 0.04f, 0.24f), color: Rgb(190, 235, 255), unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.48f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.48f, 0.25f, 0));
            foreach (var sh in new[] { lShoulder, rShoulder })
            {
                new Entity(parent: sh, model: "diamond", scale: new(0.42f, 0.36f, 0.42f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.16f, 0.02f), color: cfg.ArmorColor);
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.BodyColor);
            }

            // Левая рука: Красная монтировка (Crowbar)
            var crowbar = new Entity(parent: lShoulder, position: new(0, -0.28f, 0.42f), rotation: new(25f, 0, 0));
            new Entity(parent: crowbar, model: "cube", scale: new(0.08f, 0.82f, 0.08f), position: new(0, 0.28f, 0), color: Rgb(220, 38, 38));
            new Entity(parent: crowbar, model: "cube", scale: new(0.08f, 0.12f, 0.24f), position: new(0, 0.68f, 0.08f), color: Color.LightGray);

            // Правая рука: Грави-пушка (Zero-Point Energy Field Manipulator)
            var gravGun = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: gravGun, model: "cube", scale: new(0.22f, 0.24f, 0.85f), position: new(0, 0.04f, 0.28f), color: Rgb(55, 62, 72));
            new Entity(parent: gravGun, model: "sphere", scaleUniform: 0.26f, position: new(0, 0.06f, 0.58f), color: cfg.VisorColor, unlit: true);
            foreach (float cx in new[] { -0.12f, 0.12f })
                new Entity(parent: gravGun, model: "diamond", scale: new(0.08f, 0.16f, 0.32f), position: new(cx, 0.04f, 0.72f), color: cfg.BodyColor);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 18. DANTE (Limbus Company)
        // ==========================================
        private static void BuildDante(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            var lHip = new Entity(parent: root, position: new(-0.19f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.19f, 0.86f, 0));
            foreach (var hip in new[] { lHip, rHip })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.44f, 0.22f), position: new(0, -0.20f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.20f), position: new(0, -0.62f, 0), color: cfg.ArmorColor);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.16f, 0.38f), position: new(0, -0.80f, 0.06f), color: Rgb(18, 18, 24));
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            // Алый плащ Менеджера Limbus Company поверх чёрного жилета
            new Entity(parent: torso, model: "diamond", scale: new(0.88f, 0.78f, 0.62f), position: new(0, -0.38f, -0.06f), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.58f, 0.60f, 0.38f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
            new Entity(parent: torso, model: "cube", scale: new(0.36f, 0.56f, 0.18f), position: new(0, 0.06f, 0.14f), color: cfg.ArmorColor);
            new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.38f, 0.08f), position: new(0, 0.12f, 0.24f), color: cfg.VisorColor, unlit: true);

            // Голова-Часы Данте в языках пламени
            var head = new Entity(parent: torso, position: new(0, 0.64f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.62f, 0.62f, 0.26f), color: Rgb(35, 28, 32));
            new Entity(parent: head, model: "diamond", scale: new(0.72f, 0.72f, 0.18f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.48f, 0.28f), position: new(0, 0, 0.04f), color: Rgb(22, 18, 24));
            // Золотые стрелки на циферблате (разнесены по Z без ряби)
            new Entity(parent: head, model: "cube", scale: new(0.05f, 0.22f, 0.06f), position: new(0, 0.08f, 0.16f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "cube", scale: new(0.18f, 0.05f, 0.06f), position: new(0.06f, 0, 0.18f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.07f, position: new(0, 0, 0.20f), color: Color.White, unlit: true);
            // Огненная корона над часами
            new Entity(parent: head, model: "diamond", scale: new(0.48f, 0.55f, 0.24f), position: new(0, 0.42f, 0), color: Rgb(255, 95, 25), unlit: true);
            new Entity(parent: head, model: "diamond", scale: new(0.28f, 0.68f, 0.18f), position: new(0, 0.52f, 0), color: Color.Yellow, unlit: true);

            var halo = new Entity(parent: torso, position: new(0, 0.64f, -0.22f));
            new Entity(parent: halo, model: "diamond", scale: new(1.18f, 1.18f, 0.08f), color: cfg.VisorColor, unlit: true);
            parts.Halo = halo;
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.46f, 0.25f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.46f, 0.25f, 0));
            foreach (var sh in new[] { lShoulder, rShoulder })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.22f, 0.24f, 0.22f), position: new(0, 0.06f, 0), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.06f, 0.24f), position: new(0, 0.16f, 0), color: cfg.VisorColor, unlit: true);
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.16f, 0.02f), color: cfg.BodyColor);
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.30f, 0.22f), color: cfg.ArmorColor);
            }
            // Ветвь Золотой Ветви (Golden Bough) в правой руке
            var bough = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
            new Entity(parent: bough, model: "diamond", scale: new(0.18f, 0.18f, 0.85f), position: new(0, 0.04f, 0.28f), color: cfg.VisorColor, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 19. ЧЕБУРАШКА + СТЕНД КРОКОДИЛ ГЕНА
        // ==========================================
        private static void BuildCheburashka(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color furCol = cfg.BodyColor;       // Тёплый шоколадный мех
            Color beigeCol = cfg.ArmorColor;    // Светло-бежевая мордочка и животик
            Color orangeCol = cfg.VisorColor;   // Спелый апельсин

            var lHip = new Entity(parent: root, position: new(-0.20f, 0.55f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.55f, 0));
            foreach (var hip in new[] { lHip, rHip })
            {
                new Entity(parent: hip, model: "sphere", scale: new(0.26f, 0.34f, 0.28f), position: new(0, -0.15f, 0), color: furCol);
                new Entity(parent: hip, model: "sphere", scale: new(0.28f, 0.18f, 0.38f), position: new(0, -0.36f, 0.08f), color: furCol);
                new Entity(parent: hip, model: "sphere", scale: new(0.20f, 0.10f, 0.12f), position: new(0, -0.38f, 0.24f), color: beigeCol);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            // Пухлое уютное туловище Чебурашки
            var torso = new Entity(parent: root, position: new(0, 0.92f, 0));
            new Entity(parent: torso, model: "sphere", scale: new(0.76f, 0.82f, 0.66f), color: furCol);
            new Entity(parent: torso, model: "sphere", scale: new(0.56f, 0.62f, 0.28f), position: new(0, -0.02f, 0.24f), color: beigeCol);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.20f, position: new(0, -0.22f, -0.32f), color: furCol);

            // Голова с огромными фирменными ушами
            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.68f, 0.62f, 0.62f), color: furCol);
            new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.46f, 0.26f), position: new(0, -0.02f, 0.22f), color: beigeCol);

            // Большие добрые глаза, бровки и носик-пуговка
            foreach (float ex in new[] { -0.14f, 0.14f })
            {
                new Entity(parent: head, model: "sphere", scaleUniform: 0.15f, position: new(ex, 0.04f, 0.30f), color: Color.White);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.10f, position: new(ex, 0.04f, 0.34f), color: Rgb(28, 20, 18));
                new Entity(parent: head, model: "sphere", scaleUniform: 0.04f, position: new(ex + 0.03f, 0.07f, 0.38f), color: Color.White, unlit: true);
                new Entity(parent: head, model: "cube", scale: new(0.12f, 0.03f, 0.04f), position: new(ex, 0.16f, 0.31f), rotation: new(0, 0, ex * -25f), color: furCol);
            }
            new Entity(parent: head, model: "sphere", scale: new(0.08f, 0.06f, 0.08f), position: new(0, -0.04f, 0.35f), color: Rgb(40, 26, 22));

            // ГИГАНТСКИЕ КРУГЛЫЕ УШИ ЧЕБУРАШКИ
            foreach (int side in new[] { -1, 1 })
            {
                var ear = new Entity(parent: head, position: new(side * 0.56f, 0.04f, -0.02f), rotation: new(0, side * -12f, 0));
                new Entity(parent: ear, model: "sphere", scale: new(0.62f, 0.66f, 0.16f), color: furCol);
                new Entity(parent: ear, model: "sphere", scale: new(0.46f, 0.50f, 0.18f), position: new(0, 0, 0.02f), color: beigeCol);
                parts.Tails.Add(ear);
            }
            parts.Head = head;
            parts.Torso = torso;

            // Лапки (в правой лапке — яркий апельсин)
            var lShoulder = new Entity(parent: torso, position: new(-0.42f, 0.16f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.42f, 0.16f, 0));
            foreach (var sh in new[] { lShoulder, rShoulder })
            {
                new Entity(parent: sh, model: "sphere", scale: new(0.22f, 0.38f, 0.22f), position: new(0, -0.14f, 0.08f), rotation: new(-25f, 0, 0), color: furCol);
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.18f, position: new(0, -0.28f, 0.22f), color: beigeCol);
            }
            var orange = new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.28f, position: new(0, -0.28f, 0.38f), color: orangeCol, unlit: true);
            new Entity(parent: orange, model: "diamond", scale: new(0.10f, 0.08f, 0.10f), position: new(0, 0.15f, 0), color: Color.Lime, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;

            // ==========================================================
            // СТЕНД [КРОКОДИЛ ГЕНА] (привязан к parts.ShieldAura, скрыт до ульты)
            // ==========================================================
            var genaStand = new Entity(parent: torso, position: new(0.85f, 0.85f, -0.65f), enabled: false);
            Color crocGreen = Rgb(55, 165, 75);
            Color coatRed = Rgb(205, 38, 48);

            // Золотая JoJo-аура стенда
            new Entity(parent: genaStand, model: "diamond", scale: new(1.85f, 2.75f, 1.85f), position: new(0, 0.4f, 0), color: Rgb(255, 215, 55, 75), unlit: true);

            // Торс Гены в красном пиджаке, белой манишке и чёрной бабочке
            new Entity(parent: genaStand, model: "cube", scale: new(0.82f, 1.15f, 0.52f), position: new(0, 0.35f, 0), color: coatRed);
            new Entity(parent: genaStand, model: "diamond", scale: new(0.36f, 0.65f, 0.56f), position: new(0, 0.52f, 0.04f), color: Color.White);
            new Entity(parent: genaStand, model: "diamond", scale: new(0.28f, 0.14f, 0.60f), position: new(0, 0.78f, 0.06f), color: Rgb(25, 25, 30));
            // Мощный крокодилий хвост
            new Entity(parent: genaStand, model: "diamond", scale: new(0.34f, 0.32f, 1.05f), position: new(0, -0.18f, -0.52f), rotation: new(-25f, 0, 0), color: crocGreen);

            // Голова Крокодила Гены с длинной пастью, зубами и шляпой-федорой
            var gHead = new Entity(parent: genaStand, position: new(0, 1.15f, 0.10f));
            new Entity(parent: gHead, model: "cube", scale: new(0.56f, 0.46f, 0.58f), color: crocGreen);
            new Entity(parent: gHead, model: "cube", scale: new(0.46f, 0.24f, 0.92f), position: new(0, -0.04f, 0.52f), color: crocGreen);
            new Entity(parent: gHead, model: "cube", scale: new(0.42f, 0.12f, 0.84f), position: new(0, -0.18f, 0.48f), color: Rgb(140, 215, 130));
            foreach (float tx in new[] { -0.16f, 0.16f })
            {
                new Entity(parent: gHead, model: "diamond", scale: new(0.07f, 0.14f, 0.07f), position: new(tx, -0.12f, 0.82f), color: Color.White);
                new Entity(parent: gHead, model: "sphere", scaleUniform: 0.14f, position: new(tx, 0.16f, 0.24f), color: Color.Yellow, unlit: true);
            }
            // Фирменная шляпа Гены
            new Entity(parent: gHead, model: "cylinder" == "cube" ? "cube" : "diamond", scale: new(0.86f, 0.10f, 0.86f), position: new(0, 0.28f, -0.02f), color: Rgb(65, 45, 35));
            new Entity(parent: gHead, model: "cube", scale: new(0.52f, 0.32f, 0.52f), position: new(0, 0.42f, -0.02f), color: Rgb(80, 55, 42));

            // Мускулистые руки Гены и гармошка в руках
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: genaStand, model: "cube", scale: new(0.28f, 0.68f, 0.28f), position: new(s * 0.56f, 0.42f, 0.22f), rotation: new(-40f, 0, 0), color: coatRed);
                new Entity(parent: genaStand, model: "sphere", scaleUniform: 0.26f, position: new(s * 0.52f, 0.22f, 0.54f), color: crocGreen);
            }
            // Гармошка спереди
            var accordion = new Entity(parent: genaStand, position: new(0, 0.26f, 0.58f));
            new Entity(parent: accordion, model: "cube", scale: new(0.68f, 0.42f, 0.26f), color: Rgb(35, 38, 46));
            new Entity(parent: accordion, model: "cube", scale: new(0.42f, 0.36f, 0.30f), color: Color.Gold, unlit: true);

            parts.ShieldAura = genaStand;
        }

        // ==========================================
        // 20. AHRI (League of Legends — Девятихвостая Лиса)
        // ==========================================
        private static void BuildAhri(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color crimsonSilk = Rgb(215, 36, 56);
            Color pureWhite = Rgb(248, 248, 255);
            Color skinTone = Rgb(250, 224, 212);
            Color goldTrim = Rgb(245, 198, 68);
            Color hairDark = Rgb(26, 22, 34);
            Color orbCyan = Rgb(95, 220, 255);

            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                // Стройное бедро и высокий алый сапог с золотым кантом
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.20f), position: new(0, -0.20f, 0), color: skinTone);
                new Entity(parent: hip, model: "diamond", scale: new(0.22f, 0.14f, 0.24f), position: new(0, -0.38f, 0.02f), color: goldTrim, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.17f, 0.42f, 0.19f), position: new(0, -0.60f, 0), color: crimsonSilk);
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.36f, 0.21f), position: new(0, -0.60f, 0.01f), color: pureWhite);
                new Entity(parent: hip, model: "diamond", scale: new(0.19f, 0.16f, 0.36f), position: new(0, -0.80f, 0.06f), color: crimsonSilk);
                new Entity(parent: hip, model: "cube", scale: new(0.08f, 0.06f, 0.08f), position: new(0, -0.78f, 0.20f), color: goldTrim, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            // Пышная бело-алая юбка ионийского одеяния с золотым поясом оби и кисточками
            new Entity(parent: torso, model: "diamond", scale: new(0.96f, 0.68f, 0.88f), position: new(0, -0.38f, 0), color: pureWhite);
            new Entity(parent: torso, model: "diamond", scale: new(0.46f, 0.72f, 0.92f), position: new(0, -0.40f, 0.02f), color: crimsonSilk);
            new Entity(parent: torso, model: "cube", scale: new(0.58f, 0.16f, 0.42f), position: new(0, -0.20f, 0), color: crimsonSilk);
            new Entity(parent: torso, model: "diamond", scale: new(0.64f, 0.12f, 0.46f), position: new(0, -0.20f, 0.02f), color: goldTrim, unlit: true);
            // Золотой узел оби и колокольчик на поясе спереди
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.20f, 0.24f), color: goldTrim, unlit: true);
            new Entity(parent: torso, model: "diamond", scale: new(0.10f, 0.28f, 0.08f), position: new(-0.08f, -0.36f, 0.25f), rotation: new(0, 0, 12f), color: goldTrim, unlit: true);
            new Entity(parent: torso, model: "diamond", scale: new(0.10f, 0.28f, 0.08f), position: new(0.08f, -0.36f, 0.25f), rotation: new(0, 0, -12f), color: goldTrim, unlit: true);

            // Верх кимоно (алый лиф с белым воротником и золотым узором)
            new Entity(parent: torso, model: "cube", scale: new(0.52f, 0.52f, 0.36f), position: new(0, 0.08f, 0), color: crimsonSilk);
            new Entity(parent: torso, model: "diamond", scale: new(0.42f, 0.42f, 0.22f), position: new(0, 0.14f, 0.12f), color: pureWhite);
            new Entity(parent: torso, model: "diamond", scale: new(0.24f, 0.26f, 0.18f), position: new(0, 0.22f, 0.16f), color: skinTone);
            new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.14f, 0.12f), position: new(0, 0.10f, 0.22f), color: goldTrim, unlit: true);

            // ВЕЕР ИЗ 9 ПУШИСТЫХ БЕЛОСНЕЖНЫХ ЛИСЬИХ ХВОСТОВ (анимируются в parts.Tails!)
            var tailHub = new Entity(parent: torso, position: new(0, -0.28f, -0.20f));
            for (int i = 0; i < 9; i++)
            {
                float norm = (i - 4f) / 4f; // от -1 до +1
                float yaw = norm * 68f;
                float pitch = -22f - (1f - MathF.Abs(norm)) * 22f + (i % 2) * 8f;
                float roll = norm * -18f;

                var tail = new Entity(parent: tailHub, rotation: new(pitch, yaw, roll));
                // Основание, пушистая середина и сияющий кончик хвоста
                new Entity(parent: tail, model: "diamond", scale: new(0.26f, 0.26f, 0.72f), position: new(0, 0.04f, -0.34f), color: pureWhite);
                new Entity(parent: tail, model: "sphere", scale: new(0.34f, 0.34f, 0.76f), position: new(0, 0.10f, -0.78f), rotation: new(14f, 0, 0), color: pureWhite);
                new Entity(parent: tail, model: "diamond", scale: new(0.24f, 0.24f, 0.56f), position: new(0, 0.22f, -1.22f), rotation: new(26f, 0, 0), color: Rgb(230, 246, 255), unlit: true);
                parts.Tails.Add(tail);
            }

            // Голова Ари: тёмные волосы, лисьи ушки с белым пушком, золотые глаза и усики на щеках
            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: skinTone);
            new Entity(parent: head, model: "sphere", scale: new(0.56f, 0.48f, 0.56f), position: new(0, 0.12f, -0.03f), color: hairDark);
            // Длинная коса и распущенные пряди волос сзади
            new Entity(parent: head, model: "diamond", scale: new(0.42f, 0.96f, 0.34f), position: new(0, -0.28f, -0.24f), rotation: new(16f, 0, 0), color: hairDark);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.14f, position: new(0, -0.68f, -0.36f), color: goldTrim, unlit: true);
            // Боковые пряди у лица с золотыми кольцами
            foreach (int side in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "diamond", scale: new(0.14f, 0.48f, 0.14f), position: new(side * 0.25f, -0.10f, 0.12f), rotation: new(-8f, 0, side * 8f), color: hairDark);
                new Entity(parent: head, model: "cube", scale: new(0.15f, 0.05f, 0.15f), position: new(side * 0.25f, -0.22f, 0.12f), color: goldTrim, unlit: true);
            }
            // Светящиеся золотые лисьи глаза и фирменные полоски-усики на щеках
            foreach (int side in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "sphere", scale: new(0.12f, 0.10f, 0.08f), position: new(side * 0.12f, 0.04f, 0.22f), color: Color.Gold, unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.05f, position: new(side * 0.12f, 0.04f, 0.25f), color: Color.White, unlit: true);
                // По две тонкие полоски на каждой щеке
                new Entity(parent: head, model: "cube", scale: new(0.10f, 0.02f, 0.04f), position: new(side * 0.16f, -0.04f, 0.22f), rotation: new(0, side * 24f, side * -8f), color: Rgb(195, 75, 95));
                new Entity(parent: head, model: "cube", scale: new(0.10f, 0.02f, 0.04f), position: new(side * 0.16f, -0.09f, 0.21f), rotation: new(0, side * 24f, side * -12f), color: Rgb(195, 75, 95));
            }
            // Высокие пушистые лисьи уши на макушке
            foreach (int side in new[] { -1, 1 })
            {
                var foxEar = new Entity(parent: head, position: new(side * 0.20f, 0.34f, 0.02f), rotation: new(-6f, 0, side * -18f));
                new Entity(parent: foxEar, model: "diamond", scale: new(0.24f, 0.48f, 0.18f), position: new(0, 0.14f, 0), color: hairDark);
                new Entity(parent: foxEar, model: "diamond", scale: new(0.14f, 0.34f, 0.14f), position: new(0, 0.12f, 0.04f), color: pureWhite, unlit: true);
            }
            parts.Head = head;
            parts.Torso = torso;

            // Широкие струящиеся бело-алые рукава кимоно и парящая Сфера Обмана
            var lShoulder = new Entity(parent: torso, position: new(-0.44f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.44f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.22f, position: new(0, 0.04f, 0), color: skinTone);
                // Широкий колоколообразный рукав с золотой каймой
                new Entity(parent: sh, model: "diamond", scale: new(0.36f, 0.48f, 0.42f), position: new(0, -0.22f, 0.10f), color: pureWhite);
                new Entity(parent: sh, model: "diamond", scale: new(0.38f, 0.22f, 0.44f), position: new(0, -0.34f, 0.16f), color: crimsonSilk);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.14f, 0.36f), position: new(0, -0.28f, 0.22f), color: skinTone);
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.13f, position: new(0, -0.28f, 0.40f), color: skinTone);
            }

            // Левая рука: Розовый огонёк Очарования (Charm spark)
            var charmSpark = new Entity(parent: lShoulder, position: new(0, -0.22f, 0.50f));
            new Entity(parent: charmSpark, model: "diamond", scale: new(0.22f, 0.22f, 0.22f), color: Rgb(255, 105, 195), unlit: true);
            new Entity(parent: charmSpark, model: "sphere", scaleUniform: 0.12f, color: Color.White, unlit: true);

            // Правая рука: Парящая лазурная Сфера Обмана (Orb of Deception)
            var orb = new Entity(parent: rShoulder, position: new(0, -0.18f, 0.56f));
            new Entity(parent: orb, model: "sphere", scaleUniform: 0.42f, color: orbCyan, unlit: true);
            new Entity(parent: orb, model: "sphere", scaleUniform: 0.24f, color: Color.White, unlit: true);
            new Entity(parent: orb, model: "diamond", scale: new(0.56f, 0.12f, 0.56f), rotation: new(22f, 0, 22f), color: Rgb(165, 240, 255), unlit: true);
            new Entity(parent: orb, model: "diamond", scale: new(0.56f, 0.12f, 0.56f), rotation: new(-22f, 45f, -22f), color: Rgb(115, 195, 255), unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 21. ЛАППЛАНД / ЛАППЛАНД ДЕКАДЕНЦА (ARKNIGHTS)
        // ==========================================
        private static void BuildLappland(Entity root, SurvivorConfig cfg, SurvivorParts parts, int skinIdx)
        {
            bool isDecadenza = skinIdx == 2;
            Color skinTone = Rgb(250, 232, 222);
            Color hairSilver = Rgb(238, 242, 250);
            Color coatCol = isDecadenza ? Rgb(26, 24, 36) : (skinIdx == 1 ? Rgb(175, 32, 46) : Rgb(32, 34, 44));
            Color trimCol = isDecadenza ? Rgb(235, 190, 65) : cfg.VisorColor;
            Color bladeCol = isDecadenza ? Rgb(245, 205, 80) : Rgb(235, 242, 255);

            var lHip = new Entity(parent: root, position: new(-0.19f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.19f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.20f), position: new(0, -0.20f, 0), color: Rgb(28, 30, 38));
                new Entity(parent: hip, model: "cube", scale: new(0.17f, 0.42f, 0.19f), position: new(0, -0.60f, 0), color: Rgb(24, 26, 34));
                new Entity(parent: hip, model: "cube", scale: new(0.19f, 0.05f, 0.21f), position: new(0, -0.44f, 0), color: trimCol, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.15f, 0.36f), position: new(0, -0.80f, 0.05f), color: coatCol);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.50f, 0.58f, 0.34f), position: new(0, 0.06f, 0), color: coatCol);
            new Entity(parent: torso, model: "diamond", scale: new(0.32f, 0.50f, 0.36f), position: new(0, 0.10f, 0.02f), color: hairSilver);
            new Entity(parent: torso, model: "cube", scale: new(0.54f, 0.10f, 0.38f), position: new(0, -0.24f, 0), color: trimCol);

            // Плащ Сиракуз и пушистый волчий хвост Лупо
            new Entity(parent: torso, model: "diamond", scale: new(0.76f, 0.96f, 0.22f), position: new(0, -0.38f, -0.18f), rotation: new(14, 0, 0), color: coatCol);
            var wolfTail = new Entity(parent: torso, position: new(0, -0.26f, -0.22f), rotation: new(-28, 0, 0));
            new Entity(parent: wolfTail, model: "diamond", scale: new(0.30f, 0.30f, 0.88f), position: new(0, 0, -0.38f), color: hairSilver);
            new Entity(parent: wolfTail, model: "diamond", scale: new(0.22f, 0.22f, 0.42f), position: new(0, 0.04f, -0.68f), color: coatCol);
            parts.Tails.Add(wolfTail);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.46f, 0.48f, 0.46f), color: skinTone);
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.42f, 0.52f), position: new(0, 0.10f, -0.04f), color: hairSilver);
            new Entity(parent: head, model: "diamond", scale: new(0.44f, 0.85f, 0.28f), position: new(0, -0.24f, -0.22f), rotation: new(14, 0, 0), color: hairSilver);
            foreach (int s in new[] { -1, 1 })
            {
                // Серебристые волчьи ушки Лупо
                var ear = new Entity(parent: head, position: new(s * 0.18f, 0.34f, 0.02f), rotation: new(-6, 0, s * -16f));
                new Entity(parent: ear, model: "diamond", scale: new(0.20f, 0.42f, 0.16f), position: new(0, 0.12f, 0), color: hairSilver);
                new Entity(parent: ear, model: "diamond", scale: new(0.12f, 0.26f, 0.12f), position: new(0, 0.10f, 0.03f), color: coatCol);
                new Entity(parent: head, model: "sphere", scale: new(0.10f, 0.09f, 0.06f), position: new(s * 0.11f, 0.04f, 0.21f), color: Rgb(235, 225, 240), unlit: true);
            }

            if (isDecadenza)
            {
                // Лаппланд Декаденца: карнавальная шляпа Сиракуз с золотым пером и 2 парящих Призрачных Волка (Signori dei Lupi)!
                new Entity(parent: head, model: "diamond", scale: new(0.86f, 0.12f, 0.86f), position: new(0, 0.32f, 0), rotation: new(-8, 0, 8), color: coatCol);
                new Entity(parent: head, model: "diamond", scale: new(0.18f, 0.54f, 0.14f), position: new(0.26f, 0.52f, -0.06f), rotation: new(0, 0, -24f), color: trimCol, unlit: true);

                foreach (int s in new[] { -1, 1 })
                {
                    var lupoSpirit = new Entity(parent: torso, position: new(s * 0.78f, 0.62f, -0.18f));
                    new Entity(parent: lupoSpirit, model: "diamond", scale: new(0.32f, 0.28f, 0.58f), color: Rgb(175, 95, 255), unlit: true);
                    new Entity(parent: lupoSpirit, model: "diamond", scale: new(0.22f, 0.34f, 0.24f), position: new(0, 0.16f, 0.12f), color: trimCol, unlit: true);
                    foreach (int es in new[] { -1, 1 })
                        new Entity(parent: lupoSpirit, model: "sphere", scaleUniform: 0.07f, position: new(es * 0.09f, 0.05f, 0.24f), color: Color.Gold, unlit: true);
                    parts.Tails.Add(lupoSpirit);
                }
            }

            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.42f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.42f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "diamond", scale: new(0.34f, 0.32f, 0.34f), position: new(0, 0.06f, 0), color: coatCol);
                new Entity(parent: sh, model: "cube", scale: new(0.15f, 0.26f, 0.15f), position: new(0, -0.15f, 0), color: coatCol);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.14f, 0.34f), position: new(0, -0.28f, 0.20f), color: skinTone);

                // Изящный меч Сиракуз в каждой руке
                var sword = new Entity(parent: sh, position: new(0, -0.28f, 0.42f));
                new Entity(parent: sword, model: "diamond", scale: new(0.28f, 0.08f, 0.18f), color: trimCol, unlit: true);
                new Entity(parent: sword, model: "diamond", scale: new(0.06f, 0.12f, 0.95f), position: new(0, 0, 0.52f), color: bladeCol, unlit: true);
            }
            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 22. W / ВИШАДЕЛЬ (ARKNIGHTS)
        // ==========================================
        private static void BuildWArknights(Entity root, SurvivorConfig cfg, SurvivorParts parts, int skinIdx)
        {
            bool isWisadel = skinIdx == 2;
            Color skinTone = Rgb(250, 232, 224);
            Color hairAsh = Rgb(218, 214, 222);
            Color redAcc = Rgb(225, 38, 54);
            Color darkCoat = isWisadel ? Rgb(195, 32, 46) : (skinIdx == 1 ? Rgb(235, 232, 240) : Rgb(34, 34, 42));

            var lHip = new Entity(parent: root, position: new(-0.19f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.19f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.20f), position: new(0, -0.20f, 0), color: Rgb(32, 32, 40));
                new Entity(parent: hip, model: "cube", scale: new(0.17f, 0.42f, 0.19f), position: new(0, -0.60f, 0), color: Rgb(24, 24, 30));
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.16f, 0.36f), position: new(0, -0.80f, 0.05f), color: redAcc);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.50f, 0.58f, 0.34f), position: new(0, 0.06f, 0), color: darkCoat);
            new Entity(parent: torso, model: "cube", scale: new(0.52f, 0.10f, 0.36f), position: new(0, -0.22f, 0), color: redAcc);
            // Тонкий хвост Сарказа
            var sarkazTail = new Entity(parent: torso, position: new(0, -0.28f, -0.20f), rotation: new(-20, 0, 0));
            new Entity(parent: sarkazTail, model: "cube", scale: new(0.06f, 0.06f, 0.78f), position: new(0, 0, -0.36f), color: Rgb(28, 26, 34));
            new Entity(parent: sarkazTail, model: "diamond", scale: new(0.18f, 0.08f, 0.24f), position: new(0, 0, -0.78f), color: redAcc, unlit: true);
            parts.Tails.Add(sarkazTail);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.46f, 0.48f, 0.46f), color: skinTone);
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.42f, 0.52f), position: new(0, 0.10f, -0.04f), color: hairAsh);
            new Entity(parent: head, model: "diamond", scale: new(0.42f, 0.82f, 0.28f), position: new(0, -0.22f, -0.22f), rotation: new(14, 0, 0), color: hairAsh);
            foreach (int s in new[] { -1, 1 })
            {
                // Рога Сарказа с багровыми кончиками
                var horn = new Entity(parent: head, position: new(s * 0.18f, 0.34f, 0.04f), rotation: new(16, 0, s * -22f));
                new Entity(parent: horn, model: "diamond", scale: new(0.10f, 0.42f, 0.10f), position: new(0, 0.14f, 0), color: Rgb(28, 26, 34));
                new Entity(parent: horn, model: "diamond", scale: new(0.08f, 0.18f, 0.08f), position: new(0, 0.32f, 0), color: redAcc, unlit: true);
                new Entity(parent: head, model: "sphere", scale: new(0.10f, 0.09f, 0.06f), position: new(s * 0.11f, 0.04f, 0.21f), color: redAcc, unlit: true);
            }

            if (isWisadel)
            {
                // Вишадель (Wiš'adel): корона Каздела и 2 парящих артиллерийских пилона «Тень Восставших» за спиной!
                new Entity(parent: head, model: "diamond", scale: new(0.38f, 0.22f, 0.14f), position: new(0, 0.38f, 0.16f), color: Color.Gold, unlit: true);
                foreach (int s in new[] { -1, 1 })
                {
                    var pylon = new Entity(parent: torso, position: new(s * 0.76f, 0.46f, -0.28f), rotation: new(0, 0, s * -14f));
                    new Entity(parent: pylon, model: "diamond", scale: new(0.28f, 1.15f, 0.28f), color: Rgb(36, 32, 44));
                    new Entity(parent: pylon, model: "diamond", scale: new(0.34f, 0.42f, 0.34f), position: new(0, 0.12f, 0), color: redAcc, unlit: true);
                    new Entity(parent: pylon, model: "sphere", scaleUniform: 0.18f, position: new(0, 0.46f, 0), color: Color.Gold, unlit: true);
                    parts.Tails.Add(pylon);
                }
            }

            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.42f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.42f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.14f, 0), color: darkCoat);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.14f, 0.34f), position: new(0, -0.28f, 0.20f), color: skinTone);
            }
            // В левой руке — Детонатор D12
            var det = new Entity(parent: lShoulder, position: new(0, -0.24f, 0.40f));
            new Entity(parent: det, model: "cube", scale: new(0.12f, 0.20f, 0.10f), color: Rgb(38, 38, 46));
            new Entity(parent: det, model: "sphere", scaleUniform: 0.09f, position: new(0, 0.12f, 0), color: redAcc, unlit: true);

            // В правой руке — Гранатомёт / Орудие Предков Вишадель
            var launcher = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.46f));
            new Entity(parent: launcher, model: "cube", scale: new(0.22f, 0.24f, 0.78f), position: new(0, 0.04f, 0.18f), color: isWisadel ? redAcc : Rgb(48, 50, 60));
            new Entity(parent: launcher, model: "sphere", scaleUniform: 0.18f, position: new(0, 0.04f, 0.58f), color: Color.Gold, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 23. ГОЛД ШИП (UMAMUSUME: PRETTY DERBY)
        // ==========================================
        private static void BuildGoldShip(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color skinTone = Rgb(252, 232, 220);
            Color hairSilver = Rgb(220, 225, 238);
            Color coatRed = cfg.BodyColor;
            Color whiteTrim = cfg.ArmorColor;
            Color goldCol = cfg.VisorColor;

            var lHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.19f, 0.42f, 0.20f), position: new(0, -0.20f, 0), color: whiteTrim);
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.19f), position: new(0, -0.60f, 0), color: coatRed);
                new Entity(parent: hip, model: "cube", scale: new(0.21f, 0.16f, 0.38f), position: new(0, -0.80f, 0.06f), color: Rgb(42, 34, 38));
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.05f, 0.40f), position: new(0, -0.86f, 0.06f), color: goldCol, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.52f, 0.60f, 0.36f), position: new(0, 0.06f, 0), color: coatRed);
            new Entity(parent: torso, model: "cube", scale: new(0.26f, 0.56f, 0.38f), position: new(0, 0.06f, 0.02f), color: whiteTrim);
            // Золотые пуговицы мундира и ремни
            foreach (float py in new[] { -0.08f, 0.08f, 0.24f })
                foreach (float px in new[] { -0.14f, 0.14f })
                    new Entity(parent: torso, model: "sphere", scaleUniform: 0.07f, position: new(px, py, 0.20f), color: goldCol, unlit: true);

            // Длинный серебристый конский хвост
            var horseTail = new Entity(parent: torso, position: new(0, -0.24f, -0.20f), rotation: new(-24, 0, 0));
            new Entity(parent: horseTail, model: "diamond", scale: new(0.28f, 0.28f, 0.92f), position: new(0, 0, -0.40f), color: hairSilver);
            parts.Tails.Add(horseTail);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.46f, 0.48f, 0.46f), color: skinTone);
            new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.42f, 0.52f), position: new(0, 0.10f, -0.04f), color: hairSilver);
            new Entity(parent: head, model: "diamond", scale: new(0.48f, 0.92f, 0.28f), position: new(0, -0.26f, -0.22f), rotation: new(14, 0, 0), color: hairSilver);
            // Фирменный красно-золотой головной убор-ободок Голд Шип и лошадиные ушки!
            new Entity(parent: head, model: "cube", scale: new(0.56f, 0.14f, 0.48f), position: new(0, 0.20f, 0.02f), color: coatRed);
            new Entity(parent: head, model: "cube", scale: new(0.58f, 0.05f, 0.50f), position: new(0, 0.20f, 0.02f), color: goldCol, unlit: true);
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "sphere", scaleUniform: 0.14f, position: new(s * 0.28f, 0.16f, 0.04f), color: goldCol, unlit: true);
                var uEar = new Entity(parent: head, position: new(s * 0.16f, 0.36f, 0.02f), rotation: new(-4, 0, s * -12f));
                new Entity(parent: uEar, model: "diamond", scale: new(0.14f, 0.38f, 0.14f), position: new(0, 0.12f, 0), color: hairSilver);
                new Entity(parent: head, model: "sphere", scale: new(0.10f, 0.09f, 0.06f), position: new(s * 0.11f, 0.04f, 0.21f), color: goldCol, unlit: true);
            }
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.42f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.42f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "diamond", scale: new(0.32f, 0.28f, 0.32f), position: new(0, 0.06f, 0), color: goldCol);
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.28f, 0.16f), position: new(0, -0.15f, 0), color: coatRed);
                new Entity(parent: sh, model: "cube", scale: new(0.15f, 0.15f, 0.34f), position: new(0, -0.28f, 0.20f), color: whiteTrim);
            }
            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 24. АЛЕКСАНДР ПУШНОЙ (ГАЛИЛЕО)
        // ==========================================
        private static void BuildPushnoy(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color skinTone = Rgb(245, 215, 192);
            Color hairBrown = Rgb(78, 52, 34);
            Color galileoOrange = cfg.BodyColor;
            Color jacketCol = cfg.ArmorColor;

            var lHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.21f, 0.42f, 0.22f), position: new(0, -0.20f, 0), color: Rgb(48, 62, 92));
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.42f, 0.21f), position: new(0, -0.60f, 0), color: Rgb(42, 54, 82));
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.15f, 0.38f), position: new(0, -0.80f, 0.06f), color: Rgb(32, 34, 40));
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.58f, 0.60f, 0.38f), position: new(0, 0.06f, 0), color: jacketCol);
            new Entity(parent: torso, model: "cube", scale: new(0.34f, 0.56f, 0.40f), position: new(0, 0.06f, 0.02f), color: galileoOrange);
            // Светящаяся эмблема атома «Галилео» на груди и колба с жидким азотом на поясе
            new Entity(parent: torso, model: "diamond", scale: new(0.26f, 0.26f, 0.08f), position: new(0, 0.14f, 0.22f), color: cfg.VisorColor, unlit: true);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.20f, position: new(-0.32f, -0.22f, 0.16f), color: Rgb(95, 240, 255), unlit: true);

            // Огнетушитель за спиной для реактивных рывков
            var ext = new Entity(parent: torso, position: new(0, 0.08f, -0.28f));
            new Entity(parent: ext, model: "cube", scale: new(0.24f, 0.52f, 0.24f), color: Rgb(215, 38, 42));

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: skinTone);
            // Фирменная пышная шевелюра и бородка Александра Пушного!
            new Entity(parent: head, model: "sphere", scale: new(0.58f, 0.46f, 0.56f), position: new(0, 0.12f, -0.04f), color: hairBrown);
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "diamond", scale: new(0.22f, 0.56f, 0.26f), position: new(s * 0.26f, -0.06f, -0.04f), color: hairBrown);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.08f, position: new(s * 0.11f, 0.05f, 0.22f), color: Color.White, unlit: true);
            }
            new Entity(parent: head, model: "cube", scale: new(0.26f, 0.14f, 0.16f), position: new(0, -0.16f, 0.20f), color: hairBrown);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.44f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.44f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.15f, 0), color: jacketCol);
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.16f, 0.34f), position: new(0, -0.28f, 0.20f), color: skinTone);
            }

            // Рок-электрогитара Gibson Explorer в руках!
            var guitar = new Entity(parent: rShoulder, position: new(-0.18f, -0.26f, 0.42f), rotation: new(0, -25f, 18f));
            new Entity(parent: guitar, model: "diamond", scale: new(0.54f, 0.42f, 0.12f), color: Rgb(225, 38, 48));
            new Entity(parent: guitar, model: "cube", scale: new(0.78f, 0.10f, 0.08f), position: new(-0.46f, 0.08f, 0), color: Rgb(62, 42, 28));
            new Entity(parent: guitar, model: "cube", scale: new(0.82f, 0.04f, 0.10f), position: new(-0.32f, 0.08f, 0.04f), color: cfg.VisorColor, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 25. МИШКА ФРЕДДИ (FIVE NIGHTS AT FREDDY'S)
        // ==========================================
        private static void BuildFreddy(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color furBrown = cfg.BodyColor;
            Color bellyTan = Rgb(205, 155, 98);
            Color endoDark = Rgb(36, 38, 44);
            Color hatBlack = cfg.ArmorColor;

            var lHip = new Entity(parent: root, position: new(-0.24f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.24f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.38f, 0.26f), position: new(0, -0.18f, 0), color: furBrown);
                new Entity(parent: hip, model: "sphere", scaleUniform: 0.20f, position: new(0, -0.40f, 0), color: endoDark);
                new Entity(parent: hip, model: "cube", scale: new(0.25f, 0.36f, 0.25f), position: new(0, -0.60f, 0), color: furBrown);
                new Entity(parent: hip, model: "cube", scale: new(0.28f, 0.16f, 0.42f), position: new(0, -0.80f, 0.06f), color: furBrown);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "sphere", scale: new(0.76f, 0.78f, 0.58f), position: new(0, 0.06f, 0), color: furBrown);
            new Entity(parent: torso, model: "sphere", scale: new(0.56f, 0.58f, 0.26f), position: new(0, 0.04f, 0.22f), color: bellyTan);
            // Чёрный галстук-бабочка и 2 пуговицы на груди Фредди
            new Entity(parent: torso, model: "diamond", scale: new(0.34f, 0.14f, 0.12f), position: new(0, 0.34f, 0.28f), color: hatBlack);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.07f, position: new(0, 0.18f, 0.32f), color: hatBlack);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.07f, position: new(0, 0.04f, 0.33f), color: hatBlack);

            var head = new Entity(parent: torso, position: new(0, 0.64f, 0));
            new Entity(parent: head, model: "cube", scale: new(0.58f, 0.54f, 0.54f), color: furBrown);
            new Entity(parent: head, model: "cube", scale: new(0.42f, 0.22f, 0.26f), position: new(0, -0.10f, 0.26f), color: bellyTan);
            new Entity(parent: head, model: "sphere", scaleUniform: 0.12f, position: new(0, -0.02f, 0.38f), color: Rgb(20, 20, 24));
            // Светящиеся глаза аниматроника и круглые медвежьи уши
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "sphere", scaleUniform: 0.13f, position: new(s * 0.14f, 0.08f, 0.26f), color: cfg.VisorColor, unlit: true);
                new Entity(parent: head, model: "sphere", scale: new(0.22f, 0.22f, 0.10f), position: new(s * 0.32f, 0.30f, 0), color: furBrown);
            }
            // Фирменный чёрный цилиндр Фредди Фазбера!
            new Entity(parent: head, model: "cube", scale: new(0.42f, 0.06f, 0.42f), position: new(0, 0.30f, 0), color: hatBlack);
            new Entity(parent: head, model: "cube", scale: new(0.28f, 0.32f, 0.28f), position: new(0, 0.46f, 0), color: hatBlack);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.50f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.50f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.24f, position: new(0, 0.04f, 0), color: furBrown);
                new Entity(parent: sh, model: "cube", scale: new(0.20f, 0.28f, 0.20f), position: new(0, -0.16f, 0), color: furBrown);
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.04f), color: endoDark);
                new Entity(parent: sh, model: "cube", scale: new(0.20f, 0.20f, 0.36f), position: new(0, -0.30f, 0.22f), color: furBrown);
            }
            // Концертный микрофон в правой лапе Фредди
            var mic = new Entity(parent: rShoulder, position: new(0, -0.24f, 0.46f));
            new Entity(parent: mic, model: "cube", scale: new(0.08f, 0.28f, 0.08f), color: Rgb(40, 42, 48));
            new Entity(parent: mic, model: "sphere", scaleUniform: 0.18f, position: new(0, 0.18f, 0), color: Rgb(195, 205, 220));

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 26. СТИВ (MINECRAFT) — ВОКСЕЛЬНАЯ МОДЕЛЬ
        // ==========================================
        private static void BuildSteve(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color skinBlock = Rgb(186, 134, 102);
            Color hairBlock = Rgb(62, 40, 26);
            Color cyanShirt = cfg.BodyColor;
            Color indigoPants = cfg.ArmorColor;
            Color diamondCol = cfg.VisorColor;

            // Истинно кубические ноги Стива
            var lHip = new Entity(parent: root, position: new(-0.14f, 0.84f, 0));
            var rHip = new Entity(parent: root, position: new(0.14f, 0.84f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.26f, 0.72f, 0.26f), position: new(0, -0.38f, 0), color: indigoPants);
                new Entity(parent: hip, model: "cube", scale: new(0.27f, 0.16f, 0.27f), position: new(0, -0.78f, 0), color: Rgb(92, 94, 98));
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            // Кубический торс Стива
            var torso = new Entity(parent: root, position: new(0, 1.24f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.54f, 0.74f, 0.28f), position: new(0, 0.02f, 0), color: cyanShirt);

            // Кубическая голова 8x8x8 пикселей
            var head = new Entity(parent: torso, position: new(0, 0.64f, 0));
            new Entity(parent: head, model: "cube", scale: new(0.52f, 0.52f, 0.52f), color: skinBlock);
            new Entity(parent: head, model: "cube", scale: new(0.54f, 0.18f, 0.54f), position: new(0, 0.19f, 0), color: hairBlock);
            new Entity(parent: head, model: "cube", scale: new(0.54f, 0.36f, 0.16f), position: new(0, 0.08f, -0.20f), color: hairBlock);
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "cube", scale: new(0.12f, 0.07f, 0.04f), position: new(s * 0.12f, 0.02f, 0.26f), color: Color.White, unlit: true);
                new Entity(parent: head, model: "cube", scale: new(0.06f, 0.07f, 0.05f), position: new(s * 0.10f, 0.02f, 0.27f), color: Rgb(58, 72, 175), unlit: true);
            }
            new Entity(parent: head, model: "cube", scale: new(0.20f, 0.08f, 0.04f), position: new(0, -0.14f, 0.26f), color: hairBlock);
            parts.Head = head;
            parts.Torso = torso;

            // Кубические руки Стива + Алмазная Кирка в правой руке!
            var lShoulder = new Entity(parent: torso, position: new(-0.40f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.40f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.26f, 0.24f, 0.26f), position: new(0, 0.02f, 0), color: cyanShirt);
                new Entity(parent: sh, model: "cube", scale: new(0.24f, 0.50f, 0.24f), position: new(0, -0.32f, 0.06f), rotation: new(-25, 0, 0), color: skinBlock);
            }

            // Алмазная кирка / меч в правой руке Стива
            var pick = new Entity(parent: rShoulder, position: new(0, -0.42f, 0.36f));
            new Entity(parent: pick, model: "cube", scale: new(0.07f, 0.07f, 0.78f), position: new(0, 0, 0.22f), color: Rgb(135, 92, 48));
            new Entity(parent: pick, model: "cube", scale: new(0.09f, 0.62f, 0.14f), position: new(0, 0, 0.58f), color: diamondCol, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 27. ДЖОТАРО КУДЖО + STAR PLATINUM (JOJO)
        // ==========================================
        private static void BuildJotaro(Entity root, SurvivorConfig cfg, SurvivorParts parts, int skinIdx)
        {
            Color skinTone = Rgb(245, 218, 198);
            Color coatCol = cfg.BodyColor;
            Color standCol = skinIdx == 2 ? Rgb(242, 245, 255) : cfg.ArmorColor;
            Color goldCol = cfg.VisorColor;

            var lHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.21f, 0.42f, 0.22f), position: new(0, -0.20f, 0), color: coatCol);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.42f, 0.21f), position: new(0, -0.60f, 0), color: coatCol);
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.15f, 0.38f), position: new(0, -0.80f, 0.06f), color: Rgb(28, 28, 34));
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.58f, 0.62f, 0.38f), position: new(0, 0.06f, 0), color: coatCol);
            // Длинные полы плаща Гакуран и массивная золотая цепь на воротнике!
            new Entity(parent: torso, model: "diamond", scale: new(0.74f, 0.98f, 0.24f), position: new(0, -0.42f, -0.16f), rotation: new(12, 0, 0), color: coatCol);
            new Entity(parent: torso, model: "cube", scale: new(0.10f, 0.38f, 0.10f), position: new(0.22f, 0.22f, 0.22f), rotation: new(0, 0, -18f), color: goldCol, unlit: true);

            var head = new Entity(parent: torso, position: new(0, 0.60f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.46f, 0.48f, 0.46f), color: skinTone);
            // Знаменитая фуражка Джотаро, переходящая в волосы, с золотым значком ладони
            new Entity(parent: head, model: "cube", scale: new(0.52f, 0.26f, 0.54f), position: new(0, 0.20f, 0), color: coatCol);
            new Entity(parent: head, model: "cube", scale: new(0.48f, 0.06f, 0.26f), position: new(0, 0.12f, 0.28f), color: Rgb(22, 24, 30));
            new Entity(parent: head, model: "cube", scale: new(0.54f, 0.06f, 0.55f), position: new(0, 0.15f, 0), color: goldCol, unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.44f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.44f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.20f, 0.30f, 0.20f), position: new(0, -0.15f, 0), color: coatCol);
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.18f, 0.36f), position: new(0, -0.28f, 0.20f), color: coatCol);
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.15f, position: new(0, -0.28f, 0.40f), color: skinTone);
            }
            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;

            // 3D-СТЕНД «STAR PLATINUM» за правым плечом Джотаро!
            var stand = new Entity(parent: torso, position: new(0.68f, 0.24f, -0.55f));
            new Entity(parent: stand, model: "diamond", scale: new(0.66f, 0.68f, 0.42f), position: new(0, 0.14f, 0), color: standCol, unlit: true);
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: stand, model: "sphere", scaleUniform: 0.26f, position: new(s * 0.40f, 0.34f, 0), color: goldCol, unlit: true);
                new Entity(parent: stand, model: "cube", scale: new(0.20f, 0.20f, 0.48f), position: new(s * 0.42f, 0.08f, 0.32f), color: standCol, unlit: true);
                new Entity(parent: stand, model: "sphere", scaleUniform: 0.22f, position: new(s * 0.42f, 0.08f, 0.58f), color: goldCol, unlit: true);
            }
            var sHead = new Entity(parent: stand, position: new(0, 0.62f, 0));
            new Entity(parent: sHead, model: "sphere", scaleUniform: 0.42f, color: standCol, unlit: true);
            new Entity(parent: sHead, model: "cube", scale: new(0.46f, 0.08f, 0.46f), position: new(0, 0.12f, 0), color: goldCol, unlit: true);
            new Entity(parent: sHead, model: "diamond", scale: new(0.48f, 0.56f, 0.44f), position: new(0, 0.26f, -0.10f), rotation: new(-24, 0, 0), color: Rgb(25, 25, 35));
            parts.ShieldAura = stand;
        }

        // ==========================================
        // 28. ВАЙПЕР (DEADLOCK)
        // ==========================================
        private static void BuildVyper(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color scaleGreen = cfg.BodyColor;
            Color vestCol = cfg.ArmorColor;
            Color venomGlow = cfg.VisorColor;

            var lHip = new Entity(parent: root, position: new(-0.19f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.19f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.40f, 0.20f), position: new(0, -0.20f, 0), color: Rgb(42, 46, 38));
                new Entity(parent: hip, model: "cube", scale: new(0.17f, 0.40f, 0.19f), position: new(0, -0.58f, 0), color: scaleGreen);
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.14f, 0.36f), position: new(0, -0.78f, 0.06f), color: vestCol);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.24f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.48f, 0.56f, 0.34f), position: new(0, 0.06f, 0), color: vestCol);
            new Entity(parent: torso, model: "sphere", scaleUniform: 0.22f, position: new(0, 0.12f, -0.22f), color: venomGlow, unlit: true);

            // Длинный змеиный хвост Вайпер
            var viperTail = new Entity(parent: torso, position: new(0, -0.28f, -0.18f), rotation: new(-14, 0, 0));
            new Entity(parent: viperTail, model: "diamond", scale: new(0.24f, 0.22f, 1.05f), position: new(0, -0.08f, -0.48f), color: scaleGreen);
            parts.Tails.Add(viperTail);

            var head = new Entity(parent: torso, position: new(0, 0.56f, 0.04f));
            new Entity(parent: head, model: "diamond", scale: new(0.44f, 0.42f, 0.56f), color: scaleGreen);
            new Entity(parent: head, model: "diamond", scale: new(0.56f, 0.12f, 0.56f), position: new(0, 0.20f, 0), color: vestCol);
            foreach (int s in new[] { -1, 1 })
                new Entity(parent: head, model: "sphere", scaleUniform: 0.10f, position: new(s * 0.13f, 0.05f, 0.22f), color: Color.Yellow, unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.40f, 0.22f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.40f, 0.22f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.15f, 0.26f, 0.15f), position: new(0, -0.14f, 0), color: vestCol);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.14f, 0.34f), position: new(0, -0.26f, 0.20f), color: scaleGreen);
            }
            var smg = new Entity(parent: rShoulder, position: new(0, -0.26f, 0.44f));
            new Entity(parent: smg, model: "cube", scale: new(0.12f, 0.18f, 0.58f), color: Rgb(36, 40, 46));
            new Entity(parent: smg, model: "sphere", scaleUniform: 0.12f, position: new(0, 0.06f, 0.30f), color: venomGlow, unlit: true);

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 29. ХЕЙЗ (DEADLOCK)
        // ==========================================
        private static void BuildHaze(Entity root, SurvivorConfig cfg, SurvivorParts parts)
        {
            Color suitDark = cfg.BodyColor;
            Color vestGold = cfg.ArmorColor;
            Color dreamGlow = cfg.VisorColor;

            var lHip = new Entity(parent: root, position: new(-0.18f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.18f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.42f, 0.19f), position: new(0, -0.20f, 0), color: suitDark);
                new Entity(parent: hip, model: "cube", scale: new(0.17f, 0.42f, 0.18f), position: new(0, -0.60f, 0), color: suitDark);
                new Entity(parent: hip, model: "cube", scale: new(0.19f, 0.14f, 0.35f), position: new(0, -0.80f, 0.05f), color: vestGold);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.48f, 0.58f, 0.32f), position: new(0, 0.06f, 0), color: suitDark);
            new Entity(parent: torso, model: "diamond", scale: new(0.36f, 0.52f, 0.34f), position: new(0, 0.08f, 0.02f), color: vestGold);

            // Голова Хейз из золотистого тумана снов OSIC со светящимся оком Фиксации!
            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.46f, 0.50f, 0.46f), color: Rgb(32, 28, 44));
            new Entity(parent: head, model: "diamond", scale: new(0.38f, 0.56f, 0.38f), position: new(0, 0.22f, -0.06f), color: dreamGlow, unlit: true);
            new Entity(parent: head, model: "diamond", scale: new(0.28f, 0.10f, 0.10f), position: new(0, 0.06f, 0.24f), color: Color.White, unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.40f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.40f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.15f, 0.28f, 0.15f), position: new(0, -0.14f, 0), color: suitDark);
                new Entity(parent: sh, model: "cube", scale: new(0.14f, 0.14f, 0.34f), position: new(0, -0.28f, 0.20f), color: vestGold);
                // Парные пистолеты-пулемёты Хейз в обеих руках
                var gun = new Entity(parent: sh, position: new(0, -0.28f, 0.42f));
                new Entity(parent: gun, model: "cube", scale: new(0.10f, 0.16f, 0.48f), color: Rgb(34, 36, 44));
                new Entity(parent: gun, model: "cube", scale: new(0.06f, 0.04f, 0.44f), position: new(0, 0.09f, 0), color: dreamGlow, unlit: true);
            }
            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // ==========================================
        // 30. ТРИО DMC (0: ДАНТЕ | 1: НЕРО | 2: ВЕРГИЛИЙ)
        // ==========================================
        private static void BuildDmcTrio(Entity root, SurvivorConfig cfg, SurvivorParts parts, int skinIdx)
        {
            // skinIdx: 0 = Dante, 1 = Nero, 2 = Vergil
            Color skinTone = Rgb(248, 224, 210);
            Color hairWhite = Rgb(240, 244, 252);
            Color coatCol = skinIdx == 0 ? Rgb(195, 32, 46) : (skinIdx == 1 ? Rgb(36, 62, 132) : Rgb(28, 58, 128));
            Color innerCol = skinIdx == 0 ? Rgb(34, 36, 44) : (skinIdx == 1 ? Rgb(175, 32, 46) : Rgb(235, 238, 248));
            Color glowCol = skinIdx == 0 ? Rgb(255, 75, 85) : Rgb(75, 220, 255);

            var lHip = new Entity(parent: root, position: new(-0.20f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.20f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.42f, 0.21f), position: new(0, -0.20f, 0), color: Rgb(30, 32, 40));
                new Entity(parent: hip, model: "cube", scale: new(0.19f, 0.42f, 0.20f), position: new(0, -0.60f, 0), color: Rgb(24, 26, 34));
                new Entity(parent: hip, model: "cube", scale: new(0.21f, 0.15f, 0.38f), position: new(0, -0.80f, 0.06f), color: Rgb(20, 22, 28));
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.54f, 0.60f, 0.36f), position: new(0, 0.06f, 0), color: coatCol);
            new Entity(parent: torso, model: "cube", scale: new(0.26f, 0.56f, 0.38f), position: new(0, 0.06f, 0.02f), color: innerCol);
            new Entity(parent: torso, model: "diamond", scale: new(0.76f, 0.96f, 0.22f), position: new(0, -0.40f, -0.18f), rotation: new(14, 0, 0), color: coatCol);

            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.46f, 0.48f, 0.46f), color: skinTone);
            if (skinIdx == 2)
            {
                // Зачёсанные назад белые волосы Вергилия
                new Entity(parent: head, model: "diamond", scale: new(0.52f, 0.48f, 0.56f), position: new(0, 0.20f, -0.06f), rotation: new(-22, 0, 0), color: hairWhite);
            }
            else
            {
                // Причёска Данте / Неро
                new Entity(parent: head, model: "sphere", scale: new(0.52f, 0.44f, 0.52f), position: new(0, 0.12f, -0.02f), color: hairWhite);
            }
            foreach (int s in new[] { -1, 1 })
                new Entity(parent: head, model: "sphere", scaleUniform: 0.08f, position: new(s * 0.11f, 0.05f, 0.21f), color: glowCol, unlit: true);
            parts.Head = head;
            parts.Torso = torso;

            var lShoulder = new Entity(parent: torso, position: new(-0.44f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.44f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.15f, 0), color: coatCol);
                Color armCol = (skinIdx == 1 && side == 1) ? glowCol : coatCol;
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.16f, 0.36f), position: new(0, -0.28f, 0.20f), color: armCol, unlit: skinIdx == 1 && side == 1);
            }

            if (skinIdx == 0)
            {
                // ДАНТЕ: Меч Rebellion в правой руке и пистолет Ebony в левой
                var reb = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
                new Entity(parent: reb, model: "diamond", scale: new(0.36f, 0.10f, 0.18f), color: Color.Gold);
                new Entity(parent: reb, model: "diamond", scale: new(0.10f, 0.16f, 1.15f), position: new(0, 0, 0.62f), color: Rgb(220, 228, 240), unlit: true);
                var ebony = new Entity(parent: lShoulder, position: new(0, -0.26f, 0.42f));
                new Entity(parent: ebony, model: "cube", scale: new(0.09f, 0.14f, 0.38f), color: Rgb(28, 30, 36));
            }
            else if (skinIdx == 1)
            {
                // НЕРО: Меч Red Queen с двигателем Exceed и револьвер Blue Rose
                var rq = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
                new Entity(parent: rq, model: "cube", scale: new(0.16f, 0.20f, 0.28f), color: Rgb(215, 42, 48));
                new Entity(parent: rq, model: "diamond", scale: new(0.09f, 0.16f, 1.08f), position: new(0, 0, 0.58f), color: Rgb(255, 145, 55), unlit: true);
                var br = new Entity(parent: lShoulder, position: new(0, -0.26f, 0.42f));
                new Entity(parent: br, model: "cube", scale: new(0.10f, 0.16f, 0.42f), color: Rgb(215, 225, 240));
            }
            else
            {
                // ВЕРГИЛИЙ: Катана Ямато + ножны и парящее кольцо Призрачных Клинков (Spiral Swords)
                var scabbard = new Entity(parent: lShoulder, position: new(0, -0.28f, 0.36f));
                new Entity(parent: scabbard, model: "cube", scale: new(0.06f, 0.08f, 0.96f), color: Rgb(28, 34, 48));
                var yamato = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.44f));
                new Entity(parent: yamato, model: "diamond", scale: new(0.24f, 0.08f, 0.16f), color: Color.Gold);
                new Entity(parent: yamato, model: "diamond", scale: new(0.05f, 0.10f, 1.12f), position: new(0, 0, 0.58f), color: Rgb(155, 235, 255), unlit: true);

                var mRing = new Entity(parent: torso, position: new(0, 0.20f, -0.24f));
                for (int i = 0; i < 6; i++)
                {
                    float ang = i * (MathF.PI / 3f);
                    new Entity(parent: mRing, model: "diamond", scale: new(0.08f, 0.48f, 0.08f),
                        position: new(MathF.Cos(ang) * 0.72f, MathF.Sin(ang) * 0.72f, 0),
                        rotation: new(0, 0, i * 60f), color: glowCol, unlit: true);
                }
                parts.Halo = mRing;
            }

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }

        // =========================================================================
        // 31. СЕКРЕТНЫЙ ПОДАРОЧНЫЙ ПЕРСОНАЖ: БРИГАДНЫЙ ГЕНЕРАЛ (SANABI) + МАРИ
        // Особо детализированная 3D-модель: армейский плащ с развевающимися полами,
        // тактический разгрузочный жилет, кибер-повязка со световым шлейфом,
        // гигантская сегментированная Механическая Рука-Крюк с реактором, лебёдкой
        // и трёхпалым гарпунным захватом, а также парящий дрон-голограмма Мари!
        // =========================================================================
        private static void BuildSanabi(Entity root, SurvivorConfig cfg, SurvivorParts parts, int skinIdx)
        {
            Color skinTone = Rgb(242, 208, 184);
            Color coatCol = cfg.BodyColor;
            Color armorCol = cfg.ArmorColor;
            Color neonCol = cfg.VisorColor;
            Color brassGold = Rgb(245, 192, 62);
            Color coreOrange = skinIdx == 1 ? Rgb(255, 55, 155) : Rgb(255, 145, 35);
            Color darkSteel = Rgb(30, 34, 42);

            // 1. Ноги в армейских тактических брюках с наколенниками, экзо-скобами и реактивными ботинками
            var lHip = new Entity(parent: root, position: new(-0.21f, 0.86f, 0));
            var rHip = new Entity(parent: root, position: new(0.21f, 0.86f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                // Бедро и кобура/подсумки на ремнях
                new Entity(parent: hip, model: "cube", scale: new(0.22f, 0.44f, 0.24f), position: new(0, -0.20f, 0), color: Rgb(36, 40, 48));
                new Entity(parent: hip, model: "cube", scale: new(0.07f, 0.24f, 0.18f), position: new(side * 0.13f, -0.18f, 0.02f), color: armorCol);
                // Бронированный наколенник с неоновым индикатором
                new Entity(parent: hip, model: "diamond", scale: new(0.26f, 0.26f, 0.28f), position: new(0, -0.42f, 0.06f), color: armorCol);
                new Entity(parent: hip, model: "cube", scale: new(0.10f, 0.06f, 0.06f), position: new(0, -0.42f, 0.18f), color: neonCol, unlit: true);
                // Голень с экзоскелетной шиной
                new Entity(parent: hip, model: "cube", scale: new(0.20f, 0.42f, 0.22f), position: new(0, -0.62f, 0), color: Rgb(28, 32, 40));
                new Entity(parent: hip, model: "cube", scale: new(0.06f, 0.36f, 0.08f), position: new(side * 0.11f, -0.60f, 0), color: brassGold);
                // Тяжёлые десантные ботинки с магнитными подошвами и микро-соплами
                new Entity(parent: hip, model: "cube", scale: new(0.24f, 0.18f, 0.42f), position: new(0, -0.80f, 0.06f), color: darkSteel);
                new Entity(parent: hip, model: "diamond", scale: new(0.22f, 0.12f, 0.18f), position: new(0, -0.80f, 0.24f), color: armorCol);
                new Entity(parent: hip, model: "cube", scale: new(0.18f, 0.05f, 0.10f), position: new(0, -0.78f, -0.15f), color: neonCol, unlit: true);
            }
            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            // 2. Торс: Офицерский плащ Бригадного Генерала, тактический нагрудник и пояс с боеприпасами
            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            // Пояс с пряжкой Генерала и тактическими подсумками
            new Entity(parent: torso, model: "cube", scale: new(0.62f, 0.18f, 0.42f), position: new(0, -0.32f, 0), color: darkSteel);
            new Entity(parent: torso, model: "diamond", scale: new(0.20f, 0.16f, 0.46f), position: new(0, -0.32f, 0.02f), color: brassGold, unlit: true);
            foreach (float px in new[] { -0.24f, 0.24f })
                new Entity(parent: torso, model: "cube", scale: new(0.12f, 0.16f, 0.12f), position: new(px, -0.32f, 0.20f), color: armorCol);

            // Основа плаща и бронежилет
            new Entity(parent: torso, model: "cube", scale: new(0.66f, 0.64f, 0.42f), position: new(0, 0.06f, 0), color: coatCol);
            new Entity(parent: torso, model: "cube", scale: new(0.54f, 0.52f, 0.22f), position: new(0, 0.08f, 0.14f), color: armorCol);
            // Лацканы офицерского плаща и золотой знак отличия Бригадного Генерала
            foreach (int s in new[] { -1, 1 })
                new Entity(parent: torso, model: "diamond", scale: new(0.22f, 0.48f, 0.14f), position: new(s * 0.20f, 0.14f, 0.22f), rotation: new(0, 0, s * -14f), color: coatCol);
            new Entity(parent: torso, model: "diamond", scale: new(0.14f, 0.14f, 0.08f), position: new(-0.20f, 0.26f, 0.27f), color: brassGold, unlit: true);
            // Высокий стоячий воротник плаща со светящимся контуром
            new Entity(parent: torso, model: "cube", scale: new(0.58f, 0.22f, 0.44f), position: new(0, 0.40f, -0.04f), color: coatCol);
            new Entity(parent: torso, model: "cube", scale: new(0.60f, 0.05f, 0.46f), position: new(0, 0.49f, -0.04f), color: brassGold, unlit: true);

            // Спинной энергоблок и гидравлический привод Цепной Руки
            var backRig = new Entity(parent: torso, position: new(0.08f, 0.12f, -0.28f));
            new Entity(parent: backRig, model: "cube", scale: new(0.46f, 0.50f, 0.22f), color: armorCol);
            new Entity(parent: backRig, model: "sphere", scaleUniform: 0.24f, position: new(0.10f, 0.06f, -0.10f), color: coreOrange, unlit: true);
            new Entity(parent: backRig, model: "cube", scale: new(0.32f, 0.10f, 0.14f), position: new(0.22f, 0.18f, 0), rotation: new(0, 0, -24f), color: brassGold);

            // Две динамически развевающиеся полы офицерского плаща (анимируются через parts.Tails!)
            foreach (int s in new[] { -1, 1 })
            {
                var coatTail = new Entity(parent: torso, position: new(s * 0.19f, -0.26f, -0.16f), rotation: new(16f, s * 6f, s * -5f));
                new Entity(parent: coatTail, model: "diamond", scale: new(0.42f, 0.88f, 0.14f), position: new(0, -0.34f, -0.06f), color: coatCol);
                new Entity(parent: coatTail, model: "diamond", scale: new(0.44f, 0.14f, 0.15f), position: new(0, -0.70f, -0.06f), color: brassGold, unlit: true);
                parts.Tails.Add(coatTail);
            }

            // 3. Голова: суровое лицо ветерана с бородой, взъерошенные волосы и кибер-визор со световым шлейфом
            var head = new Entity(parent: torso, position: new(0, 0.60f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.48f, 0.50f, 0.48f), color: skinTone);
            // Густая короткая борода и усы Генерала
            new Entity(parent: head, model: "cube", scale: new(0.42f, 0.22f, 0.36f), position: new(0, -0.14f, 0.10f), color: Rgb(34, 32, 38));
            new Entity(parent: head, model: "diamond", scale: new(0.32f, 0.14f, 0.20f), position: new(0, -0.06f, 0.22f), color: Rgb(42, 40, 48));
            // Взъерошенные тёмные волосы с седыми прядями
            new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.44f, 0.54f), position: new(0, 0.14f, -0.02f), color: Rgb(28, 28, 34));
            foreach (var (hx, hy, hz, rx) in new[] {
                (0f, 0.34f, -0.12f, 28f), (-0.22f, 0.24f, -0.14f, 24f), (0.22f, 0.24f, -0.14f, 24f),
                (-0.16f, 0.28f, 0.14f, -18f), (0.16f, 0.28f, 0.14f, -18f) })
            {
                new Entity(parent: head, model: "diamond", scale: new(0.22f, 0.42f, 0.28f), position: new(hx, hy, hz), rotation: new(rx, 0, 0), color: Rgb(34, 34, 42));
            }
            // Кибернетическая боевая повязка-визор на глазах со светящимся прицелом
            new Entity(parent: head, model: "cube", scale: new(0.54f, 0.12f, 0.46f), position: new(0, 0.06f, 0.06f), color: darkSteel);
            new Entity(parent: head, model: "cube", scale: new(0.48f, 0.08f, 0.14f), position: new(0, 0.06f, 0.24f), color: neonCol, unlit: true);
            new Entity(parent: head, model: "diamond", scale: new(0.16f, 0.14f, 0.10f), position: new(0.12f, 0.06f, 0.28f), color: Color.White, unlit: true);
            // Развевающаяся неоновая лента повязки за головой
            var headbandTail = new Entity(parent: head, position: new(0, 0.06f, -0.24f), rotation: new(22f, 0, 0));
            new Entity(parent: headbandTail, model: "diamond", scale: new(0.16f, 0.68f, 0.08f), position: new(-0.08f, -0.26f, -0.14f), color: neonCol, unlit: true);
            new Entity(parent: headbandTail, model: "diamond", scale: new(0.12f, 0.56f, 0.08f), position: new(0.10f, -0.22f, -0.12f), color: coreOrange, unlit: true);
            parts.Tails.Add(headbandTail);

            parts.Head = head;
            parts.Torso = torso;

            // 4. Левая рука: Офицерский рукав, тактический наруч-коммуникатор Мари и табельный револьвер Чосона
            var lShoulder = new Entity(parent: torso, position: new(-0.46f, 0.25f, 0));
            new Entity(parent: lShoulder, model: "diamond", scale: new(0.38f, 0.34f, 0.38f), position: new(0, 0.06f, 0), color: coatCol);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.18f, 0.28f, 0.18f), position: new(0, -0.16f, 0.02f), color: coatCol);
            new Entity(parent: lShoulder, model: "sphere", scaleUniform: 0.16f, position: new(0, -0.30f, 0.02f), color: armorCol);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.17f, 0.17f, 0.36f), position: new(0, -0.30f, 0.22f), color: coatCol);
            // Голографический наруч связи с Мари на левом предплечье
            new Entity(parent: lShoulder, model: "cube", scale: new(0.21f, 0.12f, 0.20f), position: new(0, -0.26f, 0.24f), color: darkSteel);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.16f, 0.04f, 0.16f), position: new(0, -0.19f, 0.24f), color: neonCol, unlit: true);
            new Entity(parent: lShoulder, model: "cube", scale: new(0.15f, 0.15f, 0.14f), position: new(0, -0.30f, 0.40f), color: darkSteel);

            // 5. Правая рука: ИСПОЛИНСКАЯ МЕХАНИЧЕСКАЯ РУКА-КРЮК (SANABI CHAIN-ARM)
            var rShoulder = new Entity(parent: torso, position: new(0.52f, 0.26f, 0));
            // Массивный бронированный наплечник с гидравликой и сигнальными полосами
            new Entity(parent: rShoulder, model: "cube", scale: new(0.46f, 0.42f, 0.48f), position: new(0.06f, 0.08f, 0), color: armorCol);
            new Entity(parent: rShoulder, model: "diamond", scale: new(0.52f, 0.22f, 0.52f), position: new(0.08f, 0.18f, 0), color: brassGold);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.22f, position: new(0.16f, 0.08f, 0.18f), color: coreOrange, unlit: true);
            // Плечевой гидроцилиндр и титановый локоть
            new Entity(parent: rShoulder, model: "cube", scale: new(0.26f, 0.32f, 0.26f), position: new(0.04f, -0.16f, 0.02f), color: darkSteel);
            foreach (float pz in new[] { -0.09f, 0.09f })
                new Entity(parent: rShoulder, model: "cube", scale: new(0.07f, 0.30f, 0.07f), position: new(0.16f, -0.16f, pz), color: brassGold);
            new Entity(parent: rShoulder, model: "sphere", scaleUniform: 0.26f, position: new(0.04f, -0.32f, 0.04f), color: brassGold);

            // Гигантский корпус предплечья с барабаном стальной цепи, реактором и поршнями
            var chainForearm = new Entity(parent: rShoulder, position: new(0.05f, -0.32f, 0.32f));
            new Entity(parent: chainForearm, model: "cube", scale: new(0.36f, 0.34f, 0.62f), color: armorCol);
            new Entity(parent: chainForearm, model: "cube", scale: new(0.40f, 0.14f, 0.48f), position: new(0, 0.14f, -0.02f), color: darkSteel);
            // Светящееся плазменное ядро лебёдки
            new Entity(parent: chainForearm, model: "sphere", scale: new(0.42f, 0.26f, 0.26f), position: new(0, 0, -0.04f), color: coreOrange, unlit: true);
            new Entity(parent: chainForearm, model: "cube", scale: new(0.41f, 0.06f, 0.56f), position: new(0, 0.08f, 0.02f), color: neonCol, unlit: true);
            // Барабан с намотанным тросом
            new Entity(parent: chainForearm, model: "sphere", scale: new(0.32f, 0.32f, 0.24f), position: new(0, -0.06f, 0.18f), color: brassGold);
            // Массивный трёхпалый гарпунный захват (Когти Крюка-Кошки) на конце руки!
            var clawHub = new Entity(parent: chainForearm, position: new(0, -0.02f, 0.42f));
            new Entity(parent: clawHub, model: "sphere", scaleUniform: 0.30f, color: darkSteel);
            new Entity(parent: clawHub, model: "sphere", scaleUniform: 0.20f, position: new(0, 0, 0.10f), color: neonCol, unlit: true);
            // 3 стальных когтя гарпуна + центральное остриё
            new Entity(parent: clawHub, model: "diamond", scale: new(0.14f, 0.14f, 0.58f), position: new(0, 0, 0.28f), color: Color.White, unlit: true);
            for (int c = 0; c < 3; c++)
            {
                float ang = c * 120f * (MathF.PI / 180f);
                float cx = MathF.Cos(ang) * 0.18f;
                float cy = MathF.Sin(ang) * 0.18f;
                var claw = new Entity(parent: clawHub, position: new(cx, cy, 0.14f), rotation: new(0, 0, c * 120f));
                new Entity(parent: claw, model: "diamond", scale: new(0.12f, 0.24f, 0.52f), position: new(0, 0.06f, 0.18f), rotation: new(-18f, 0, 0), color: brassGold);
                new Entity(parent: claw, model: "diamond", scale: new(0.08f, 0.14f, 0.30f), position: new(0, 0.02f, 0.40f), rotation: new(22f, 0, 0), color: coreOrange, unlit: true);
            }

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;

            // 6. Парящий компаньон МАРИ (Хакерский Дрон + Голограмма Мари) рядом с левым плечом Генерала!
            var mariDrone = new Entity(parent: torso, position: new(-0.68f, 0.56f, 0.10f));
            // Корпус дрона Мари с ушками-антеннами и голографическим экраном-смайликом
            new Entity(parent: mariDrone, model: "sphere", scale: new(0.34f, 0.32f, 0.34f), color: Rgb(240, 244, 252));
            new Entity(parent: mariDrone, model: "cube", scale: new(0.28f, 0.20f, 0.12f), position: new(0, 0.02f, 0.16f), color: darkSteel);
            // Светящееся экранное личико/сердечко Мари
            new Entity(parent: mariDrone, model: "diamond", scale: new(0.18f, 0.12f, 0.08f), position: new(0, 0.02f, 0.21f), color: Rgb(255, 105, 195), unlit: true);
            foreach (int s in new[] { -1, 1 })
            {
                // Неоновые ушки-антенны и боковые стабилизаторы дрона Мари
                new Entity(parent: mariDrone, model: "diamond", scale: new(0.09f, 0.26f, 0.10f), position: new(s * 0.12f, 0.24f, 0), rotation: new(0, 0, s * -20f), color: neonCol, unlit: true);
                new Entity(parent: mariDrone, model: "diamond", scale: new(0.32f, 0.08f, 0.20f), position: new(s * 0.26f, 0, -0.04f), rotation: new(0, 0, s * 16f), color: brassGold);
            }
            // Вращающееся голографическое кольцо взлома вокруг Генерала
            var hackHalo = new Entity(parent: torso, position: new(0, 0.22f, -0.24f));
            for (int i = 0; i < 4; i++)
            {
                float ang = i * (MathF.PI * 0.5f);
                new Entity(parent: hackHalo, model: "diamond", scale: new(0.12f, 0.32f, 0.08f),
                    position: new(MathF.Cos(ang) * 0.76f, MathF.Sin(ang) * 0.76f, 0),
                    rotation: new(0, 0, i * 90f), color: neonCol, unlit: true);
            }
            parts.Halo = hackHalo;
            parts.ShieldAura = mariDrone;
        }

        // =========================================================================
        // 32. ДЖЕК-О ВАЛЕНТАЙН (GUILTY GEAR -STRIVE-)
        // Белоснежный расклешённый комбинезон с чёрными ремнями и золотой пряжкой-тыквой,
        // чёрные туфли на шпильке с ярко-красной подошвой, двухцветные ало-белые волосы,
        // расколотый золотой нимб, трёхглазая железная маска, кандалы на левой лодыжке
        // с цепным шаром «Железная Тыква» и Командный Флаг для призыва/баффа Слуг!
        // =========================================================================
        private static void BuildJackO(Entity root, SurvivorConfig cfg, SurvivorParts parts, int skinIdx)
        {
            Color skinTone = Rgb(252, 228, 216);
            Color suitCol = cfg.BodyColor;                       // Белоснежный комбинезон Strive
            Color crimsonHair = skinIdx == 1 ? Rgb(235, 110, 32) : Rgb(198, 28, 46); // Внешний алый слой волос
            Color innerWhiteHair = Rgb(246, 248, 255);           // Внутренний белоснежный слой волос
            Color strapDark = Rgb(28, 30, 38);                   // Чёрные ремни, полосы и каблуки
            Color redSole = Rgb(225, 32, 44);                    // Фирменная алая подошва каблуков и кресты
            Color goldTrim = Rgb(245, 196, 62);                  // Золотая пряжка-тыква и расколотый нимб
            Color ironDark = Rgb(44, 48, 58);                    // Тёмный чугун маски и Железной Тыквы
            Color limeGlow = cfg.VisorColor;                     // Светящаяся лаймовая ухмылка Тыквы и глаз маски

            // 1. Ноги: облегающие бёдра, расклешённые книзу брюки-клёш с чёрными лампасами и каблуки с красной подошвой!
            var lHip = new Entity(parent: root, position: new(-0.19f, 0.88f, 0));
            var rHip = new Entity(parent: root, position: new(0.19f, 0.88f, 0));
            foreach (var (side, hip) in new[] { (-1, lHip), (1, rHip) })
            {
                // Верх бедра с чёрным ремешком
                new Entity(parent: hip, model: "cube", scale: new(0.19f, 0.42f, 0.21f), position: new(0, -0.20f, 0), color: suitCol);
                new Entity(parent: hip, model: "cube", scale: new(0.21f, 0.05f, 0.23f), position: new(0, -0.14f, 0), color: strapDark);
                // Вертикальный чёрный лампас сбоку штанины
                new Entity(parent: hip, model: "cube", scale: new(0.05f, 0.72f, 0.09f), position: new(side * 0.09f, -0.44f, 0), color: strapDark);
                // Расклешённый низ брючины (Bell-bottom flare из GGST) + золотая пуговица
                new Entity(parent: hip, model: "diamond", scale: new(0.32f, 0.46f, 0.34f), position: new(0, -0.62f, 0.01f), color: suitCol);
                new Entity(parent: hip, model: "sphere", scaleUniform: 0.06f, position: new(side * 0.11f, -0.66f, 0.08f), color: goldTrim, unlit: true);
                // Остроносая чёрная туфля на высоком каблуке с ЯРКО-КРАСНОЙ ПОДОШВОЙ (по референсу пинка!)
                new Entity(parent: hip, model: "diamond", scale: new(0.18f, 0.14f, 0.38f), position: new(0, -0.81f, 0.08f), color: strapDark);
                new Entity(parent: hip, model: "diamond", scale: new(0.17f, 0.05f, 0.36f), position: new(0, -0.85f, 0.08f), color: redSole, unlit: true);
                new Entity(parent: hip, model: "cube", scale: new(0.05f, 0.13f, 0.05f), position: new(0, -0.82f, -0.07f), color: redSole, unlit: true);
            }

            // Тяжёлый чугунный браслет-кандалы на левой лодыжке + цепь и покоящаяся «Железная Тыква»!
            new Entity(parent: lHip, model: "cube", scale: new(0.26f, 0.10f, 0.26f), position: new(0, -0.73f, 0), color: ironDark);
            new Entity(parent: lHip, model: "sphere", scaleUniform: 0.08f, position: new(-0.13f, -0.73f, 0.04f), color: goldTrim, unlit: true);

            // Покоящийся шар на цепи рядом с ногой (скрывается через ShieldAura во время броска/пинка шара!)
            var restingBallRig = new Entity(parent: root, position: new(-0.62f, 0.32f, -0.18f));
            // Звенья цепи от левой лодыжки к шару
            new Entity(parent: restingBallRig, model: "cube", scale: new(0.10f, 0.07f, 0.22f), position: new(0.32f, -0.12f, 0.12f), rotation: new(0, -32f, 12f), color: ironDark);
            new Entity(parent: restingBallRig, model: "cube", scale: new(0.10f, 0.07f, 0.22f), position: new(0.18f, -0.08f, 0.06f), rotation: new(0, -28f, -8f), color: Rgb(68, 74, 88));
            // Чугунный шар «Железная Тыква» с лаймовыми светящимися глазами, зубастой ухмылкой и золотым черенком-крестом
            new Entity(parent: restingBallRig, model: "sphere", scale: new(0.64f, 0.58f, 0.64f), color: ironDark);
            new Entity(parent: restingBallRig, model: "diamond", scale: new(0.68f, 0.20f, 0.68f), color: Rgb(34, 38, 46));
            // Золотой черенок-крест сверху тыквы
            new Entity(parent: restingBallRig, model: "cube", scale: new(0.10f, 0.20f, 0.10f), position: new(0, 0.32f, 0), color: goldTrim, unlit: true);
            new Entity(parent: restingBallRig, model: "cube", scale: new(0.22f, 0.07f, 0.08f), position: new(0, 0.35f, 0), color: goldTrim, unlit: true);
            // Светящееся зелёное лицо тыквы (2 треугольных глаза + зигзагообразная улыбка)
            foreach (int es in new[] { -1, 1 })
                new Entity(parent: restingBallRig, model: "diamond", scale: new(0.13f, 0.12f, 0.08f), position: new(es * 0.13f, 0.08f, 0.29f), rotation: new(0, 0, es * -12f), color: limeGlow, unlit: true);
            for (int m = -2; m <= 2; m++)
                new Entity(parent: restingBallRig, model: "diamond", scale: new(0.09f, 0.08f, 0.08f), position: new(m * 0.07f, -0.08f + (Math.Abs(m) % 2) * 0.03f, 0.29f), color: limeGlow, unlit: true);
            parts.ShieldAura = restingBallRig;

            parts.LeftLeg = lHip;
            parts.RightLeg = rHip;

            // 2. Торс: облегающий белый комбинезон, чёрные ремни на поясе, золотая пряжка-череп тыквы и красные кресты
            var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
            new Entity(parent: torso, model: "cube", scale: new(0.46f, 0.56f, 0.32f), position: new(0, 0.06f, 0), color: suitCol);
            new Entity(parent: torso, model: "sphere", scale: new(0.48f, 0.30f, 0.34f), position: new(0, 0.16f, 0.04f), color: suitCol);
            // Тёмная центральная молния и высокий воротник с золотой застёжкой
            new Entity(parent: torso, model: "cube", scale: new(0.05f, 0.52f, 0.06f), position: new(0, 0.08f, 0.16f), color: strapDark);
            new Entity(parent: torso, model: "cube", scale: new(0.28f, 0.12f, 0.28f), position: new(0, 0.36f, 0), color: strapDark);
            new Entity(parent: torso, model: "diamond", scale: new(0.12f, 0.12f, 0.08f), position: new(0, 0.34f, 0.16f), color: redSole, unlit: true);
            // Перекрёстные чёрные ремни на талии и бёдрах + золотая пряжка-тыква спереди!
            new Entity(parent: torso, model: "cube", scale: new(0.52f, 0.10f, 0.36f), position: new(0, -0.22f, 0), rotation: new(0, 0, -6f), color: strapDark);
            new Entity(parent: torso, model: "cube", scale: new(0.50f, 0.07f, 0.35f), position: new(0, -0.30f, 0), rotation: new(0, 0, 8f), color: strapDark);
            new Entity(parent: torso, model: "sphere", scale: new(0.18f, 0.16f, 0.10f), position: new(0.04f, -0.23f, 0.19f), color: goldTrim, unlit: true);
            // Красные кресты Валентайн по бокам плеч/воротника
            foreach (int s in new[] { -1, 1 })
                new Entity(parent: torso, model: "diamond", scale: new(0.10f, 0.10f, 0.08f), position: new(s * 0.20f, 0.26f, 0.16f), color: redSole, unlit: true);

            // 3. Голова: изумрудные глаза, двухслойные ало-белые длинные волосы, расколотый золотой нимб и 3-глазая маска!
            var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
            new Entity(parent: head, model: "sphere", scale: new(0.46f, 0.48f, 0.46f), color: skinTone);
            // Изумрудно-зелёные глаза Jack-O'
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "sphere", scale: new(0.11f, 0.10f, 0.07f), position: new(s * 0.11f, 0.04f, 0.21f), color: Rgb(65, 215, 110), unlit: true);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.04f, position: new(s * 0.11f + 0.02f, 0.06f, 0.24f), color: Color.White, unlit: true);
            }

            // Шапка алых волос сверху + чёлка и боковые пряди с белой внутренней подкладкой!
            new Entity(parent: head, model: "sphere", scale: new(0.54f, 0.46f, 0.54f), position: new(0, 0.12f, -0.03f), color: crimsonHair);
            new Entity(parent: head, model: "diamond", scale: new(0.18f, 0.26f, 0.14f), position: new(-0.04f, 0.18f, 0.22f), rotation: new(12f, 0, 8f), color: crimsonHair);
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: head, model: "diamond", scale: new(0.15f, 0.52f, 0.16f), position: new(s * 0.24f, -0.10f, 0.10f), rotation: new(-6f, 0, s * 8f), color: crimsonHair);
                new Entity(parent: head, model: "diamond", scale: new(0.11f, 0.44f, 0.12f), position: new(s * 0.20f, -0.12f, 0.08f), rotation: new(-6f, 0, s * 6f), color: innerWhiteHair);
            }

            // Длинные развевающиеся двухцветные пряди волос сзади (алый верх + белоснежный внутренний слой, анимируются в Tails!)
            foreach (int s in new[] { -1, 0, 1 })
            {
                var hairLock = new Entity(parent: head, position: new(s * 0.16f, -0.06f, -0.20f), rotation: new(16f, s * 8f, 0));
                new Entity(parent: hairLock, model: "diamond", scale: new(0.32f, 0.96f, 0.20f), position: new(0, -0.38f, -0.06f), color: crimsonHair);
                new Entity(parent: hairLock, model: "diamond", scale: new(0.26f, 0.88f, 0.16f), position: new(0, -0.40f, 0.03f), color: innerWhiteHair);
                parts.Tails.Add(hairLock);
            }

            // РАСКОЛОТЫЙ ЗОЛОТОЙ НИМБ (Broken Halo) над/за головой (вращается через parts.Halo)
            var brokenHalo = new Entity(parent: head, position: new(0, 0.34f, -0.18f), rotation: new(24f, 0, 0));
            for (int i = 0; i < 6; i++)
            {
                if (skinIdx != 2 && i == 2) continue; // Характерный скол нимба у классической Jack-O'!
                float ang = i * (MathF.PI / 3f);
                new Entity(parent: brokenHalo, model: "diamond", scale: new(0.24f, 0.10f, 0.08f),
                    position: new(MathF.Cos(ang) * 0.44f, MathF.Sin(ang) * 0.44f, 0),
                    rotation: new(0, 0, i * 60f + 90f), color: goldTrim, unlit: true);
            }
            parts.Halo = brokenHalo;

            // ФИРМЕННАЯ ТРЁХГЛАЗАЯ ЖЕЛЕЗНАЯ МАСКА JACK-O':
            // а) В обычном состоянии — сдвинута набок на макушке (NormalFormNodes)
            var maskSide = new Entity(parent: head, position: new(0.22f, 0.24f, 0.10f), rotation: new(-22f, 34f, -26f));
            new Entity(parent: maskSide, model: "sphere", scale: new(0.36f, 0.38f, 0.18f), color: ironDark);
            foreach (float mx in new[] { -0.08f, 0f, 0.08f })
                new Entity(parent: maskSide, model: "diamond", scale: new(0.05f, 0.11f, 0.06f), position: new(mx, 0.04f, 0.09f), color: limeGlow, unlit: true);
            new Entity(parent: maskSide, model: "cube", scale: new(0.18f, 0.03f, 0.06f), position: new(0, -0.07f, 0.09f), color: limeGlow, unlit: true);
            parts.NormalFormNodes.Add(maskSide);

            // б) В боевом/ульт-режиме — надета прямо на лицо (UltFormNodes)!
            var maskFace = new Entity(parent: head, position: new(0, 0.03f, 0.22f), enabled: false);
            new Entity(parent: maskFace, model: "sphere", scale: new(0.46f, 0.48f, 0.18f), color: ironDark);
            foreach (float mx in new[] { -0.10f, 0f, 0.10f })
                new Entity(parent: maskFace, model: "diamond", scale: new(0.06f, 0.14f, 0.08f), position: new(mx, 0.05f, 0.09f), color: limeGlow, unlit: true);
            new Entity(parent: maskFace, model: "cube", scale: new(0.22f, 0.04f, 0.08f), position: new(0, -0.09f, 0.09f), color: limeGlow, unlit: true);
            parts.UltFormNodes.Add(maskFace);

            parts.Head = head;
            parts.Torso = torso;

            // 4. Руки в белых рукавах с тёмными манжетами и красными крестами + Командный Флаг в правой руке (для Shift)!
            var lShoulder = new Entity(parent: torso, position: new(-0.38f, 0.24f, 0));
            var rShoulder = new Entity(parent: torso, position: new(0.38f, 0.24f, 0));
            foreach (var (side, sh) in new[] { (-1, lShoulder), (1, rShoulder) })
            {
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.19f, position: new(0, 0.03f, 0), color: suitCol);
                new Entity(parent: sh, model: "cube", scale: new(0.15f, 0.28f, 0.15f), position: new(0, -0.14f, 0), color: suitCol);
                new Entity(parent: sh, model: "diamond", scale: new(0.22f, 0.16f, 0.22f), position: new(0, -0.26f, 0.14f), color: suitCol);
                new Entity(parent: sh, model: "cube", scale: new(0.16f, 0.06f, 0.16f), position: new(0, -0.27f, 0.24f), color: strapDark);
                new Entity(parent: sh, model: "sphere", scaleUniform: 0.12f, position: new(0, -0.28f, 0.34f), color: strapDark);
            }

            // Командный Флаг в правой руке (отображается во время прыжка Shift через parts.Shield!)
            var handFlag = new Entity(parent: rShoulder, position: new(0, -0.28f, 0.38f), rotation: new(75f, 0, 0), enabled: false);
            new Entity(parent: handFlag, model: "cube", scale: new(0.06f, 1.45f, 0.06f), position: new(0, 0.55f, 0), color: goldTrim, unlit: true);
            new Entity(parent: handFlag, model: "diamond", scale: new(0.18f, 0.22f, 0.18f), position: new(0, 1.32f, 0), color: goldTrim, unlit: true);
            new Entity(parent: handFlag, model: "diamond", scale: new(0.72f, 0.46f, 0.08f), position: new(0.36f, 1.02f, 0), color: redSole, unlit: true);
            new Entity(parent: handFlag, model: "diamond", scale: new(0.26f, 0.26f, 0.10f), position: new(0.34f, 1.02f, 0), color: limeGlow, unlit: true);
            parts.Shield = handFlag;

            parts.LeftArm = lShoulder;
            parts.RightArm = rShoulder;
        }
    }
}