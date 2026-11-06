using System;
using System.Numerics;
using System.Text.Json.Nodes;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Models;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Entities
{
    public class RemotePlayer : Entity
    {
        public readonly string PlayerId;
        public string CharKey;
        public int SkinIdx = 0;
        public Vector3 TargetPos = new(2, 0, 2);
        public float TargetRotY = 0f;
        public bool IsMoving = false;
        public bool Grounded = true;
        public float VelY = 0f;
        public bool ShieldStance = false;
        public int LastAnimStep = 0;
        public float Hp = 110f;
        public float MaxHp = 110f;
        public float Shield = 0f;
        public int Gold = 15;
        public int LunarCoins = 2;
        public string PlayerName = "";
        public bool IsSpectator = false;
        public readonly float[] SkillCds = new float[4];
        public string SyncedItemsCompact = "";
        public string ActiveEmote = "";
        public string RpsChoiceText = "";
        public float RpsChoiceTimer = 0f;

        public bool IsAliveCombatant => !IsSpectator && Hp > 0f && Enabled && !IsDestroyed;

        private SurvivorParts _parts;
        private readonly Entity _labelEnt;
        private Entity? _chairProp;
        public SurvivorParts Parts => _parts;

        public RemotePlayer(string playerId, string charKey = "commando", int skinIdx = 0) : base(position: new(2, 0, 2))
        {
            PlayerId = playerId;
            CharKey = SurvivorDatabase.Classes.ContainsKey(charKey) ? charKey : "commando";
            int maxSk = SurvivorDatabase.Classes.TryGetValue(CharKey, out var initCfg) && initCfg.Skins.Count > 0 ? initCfg.Skins.Count - 1 : 2;
            SkinIdx = Math.Clamp(skinIdx, 0, maxSk);
            _parts = SurvivorDatabase.BuildSurvivorMesh(this, CharKey, SkinIdx);
            _labelEnt = new Entity(parent: this, position: new(0, 2.75f, 0), color: Color.SkyBlue);
            UpdateBillboard();
        }

        private void EnsureChairProp(bool visible)
        {
            if (visible)
            {
                if (_chairProp == null || _chairProp.IsDestroyed)
                    _chairProp = BuildPlasticChairEntity(this);
                _chairProp.Enabled = true;
            }
            else if (_chairProp != null && !_chairProp.IsDestroyed)
            {
                _chairProp.Enabled = false;
            }
        }

        public static Entity BuildPlasticChairEntity(Entity parent)
        {
            // Знаменитый белый садовый пластиковый стул-моноблок (Мотивация!)
            var chair = new Entity(parent: parent, position: new(0, 0f, -0.10f));
            Color whitePlastic = Rgb(242, 245, 250);
            Color shadePlastic = Rgb(212, 218, 228);
            // Сиденье
            new Entity(parent: chair, model: "cube", scale: new(0.76f, 0.07f, 0.72f), position: new(0, 0.48f, 0.02f), color: whitePlastic);
            // 4 слегка расставленные пластиковые ножки
            foreach (float lx in new[] { -0.32f, 0.32f })
                foreach (float lz in new[] { -0.28f, 0.30f })
                    new Entity(parent: chair, model: "cube", scale: new(0.07f, 0.50f, 0.07f),
                        position: new(lx, 0.24f, lz), rotation: new(lz * -14f, 0f, lx * 14f), color: shadePlastic);
            // Изогнутая спинка с вертикальными прорезями моноблока
            var back = new Entity(parent: chair, position: new(0, 0.88f, -0.32f), rotation: new(-10f, 0, 0));
            new Entity(parent: back, model: "cube", scale: new(0.78f, 0.14f, 0.06f), position: new(0, 0.30f, 0), color: whitePlastic);
            new Entity(parent: back, model: "cube", scale: new(0.76f, 0.10f, 0.06f), position: new(0, -0.26f, 0), color: whitePlastic);
            foreach (float sx in new[] { -0.34f, -0.17f, 0f, 0.17f, 0.34f })
                new Entity(parent: back, model: "cube", scale: new(0.07f, 0.56f, 0.06f), position: new(sx, 0.02f, 0), color: sx is -0.34f or 0.34f ? whitePlastic : shadePlastic);
            // Подлокотники
            foreach (int s in new[] { -1, 1 })
            {
                new Entity(parent: chair, model: "cube", scale: new(0.09f, 0.06f, 0.68f), position: new(s * 0.40f, 0.72f, 0.02f), color: whitePlastic);
                new Entity(parent: chair, model: "cube", scale: new(0.07f, 0.26f, 0.07f), position: new(s * 0.38f, 0.59f, 0.28f), color: shadePlastic);
            }
            return chair;
        }

        private void UpdateBillboard()
        {
            if (IsSpectator || Hp <= 0f)
            {
                _labelEnt.Enabled = false;
                return;
            }
            _labelEnt.Enabled = true;
            string cName = SurvivorDatabase.Classes[CharKey].Name;
            string dispId = string.IsNullOrWhiteSpace(PlayerName) ? PlayerId.ToUpper() : $"{PlayerName} [{PlayerId.ToUpper()}]";
            string extra = "";
            if (RpsChoiceTimer > 0f && !string.IsNullOrEmpty(RpsChoiceText))
                extra = $"\n{RpsChoiceText}";
            else if (ActiveEmote == "rps_wait")
                extra = "\n[E] СЫГРАТЬ В КАМЕНЬ-НОЖНИЦЫ-БУМАГУ!";
            string shTxt = Shield >= 1f ? $" (+{(int)Shield})" : "";
            _labelEnt.BillboardText = $"{dispId} — {cName}\n{(int)Hp}{shTxt}/{(int)MaxHp} HP{extra}";
        }

        public void SetRpsOutcome(string emote, string badgeText, float duration = 3.2f)
        {
            ActiveEmote = emote;
            _parts.SetEmote(emote);
            EnsureChairProp(emote == "chair");
            RpsChoiceText = badgeText;
            RpsChoiceTimer = duration;
            UpdateBillboard();
        }

        public void MarkDead()
        {
            Hp = 0f;
            _parts.Root.Enabled = false;
            EnsureChairProp(false);
            UpdateBillboard();
        }

        public void RebuildClass(string newCharKey, int newSkinIdx = 0)
        {
            if (!SurvivorDatabase.Classes.TryGetValue(newCharKey, out var newCfg)) return;
            int maxSk = newCfg.Skins.Count > 0 ? newCfg.Skins.Count - 1 : 2;
            newSkinIdx = Math.Clamp(newSkinIdx, 0, maxSk);
            if (newCharKey == CharKey && newSkinIdx == SkinIdx) return;
            CharKey = newCharKey;
            SkinIdx = newSkinIdx;
            Destroy(_parts.Root);
            _parts = SurvivorDatabase.BuildSurvivorMesh(this, newCharKey, newSkinIdx);
            _parts.SetEmote(ActiveEmote);
            UpdateBillboard();
        }

        public void ApplyNetState(JsonObject msg)
        {
            string netChar = msg["char_key"]?.GetValue<string>() ?? CharKey;
            int netSkin = msg["skin_idx"]?.GetValue<int>() ?? SkinIdx;
            RebuildClass(netChar, netSkin);
            if (msg["pos"] is JsonArray arr && arr.Count >= 3)
            {
                TargetPos = new Vector3(
                    arr[0]?.GetValue<float>() ?? 0f,
                    arr[1]?.GetValue<float>() ?? 0f,
                    arr[2]?.GetValue<float>() ?? 0f
                );
            }
            TargetRotY = msg["rot_y"]?.GetValue<float>() ?? 0f;
            IsMoving = msg["moving"]?.GetValue<bool>() ?? false;
            Grounded = msg["grounded"]?.GetValue<bool>() ?? true;
            VelY = msg["vel_y"]?.GetValue<float>() ?? 0f;
            ShieldStance = msg["shield_stance"]?.GetValue<bool>() ?? false;
            Hp = msg["hp"]?.GetValue<float>() ?? Hp;
            MaxHp = MathF.Max(1f, msg["max_hp"]?.GetValue<float>() ?? MaxHp);
            Shield = msg["shield"]?.GetValue<float>() ?? Shield;
            Gold = msg["gold"]?.GetValue<int>() ?? Gold;
            LunarCoins = msg["lunar"]?.GetValue<int>() ?? LunarCoins;
            string? netName = msg["name"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(netName)) PlayerName = netName!;
            IsSpectator = msg["spectator"]?.GetValue<bool>() ?? false;
            if (msg["cds"] is JsonArray cdArr && cdArr.Count >= 4)
            {
                for (int i = 0; i < 4; i++)
                    SkillCds[i] = cdArr[i]?.GetValue<float>() ?? 0f;
            }
            string? netItems = msg["items"]?.GetValue<string>();
            if (netItems != null) SyncedItemsCompact = netItems;

            bool bodyVis = !IsSpectator && Hp > 0f;
            _parts.Root.Enabled = bodyVis;
            if (!bodyVis) EnsureChairProp(false);

            bool ultForm = msg["ult_form"]?.GetValue<bool>() ?? false;
            _parts.SetUltForm(ultForm);

            string netEmote = bodyVis ? (msg["emote"]?.GetValue<string>() ?? "") : "";
            if (RpsChoiceTimer <= 0f)
            {
                ActiveEmote = netEmote;
                _parts.SetEmote(ActiveEmote);
                EnsureChairProp(bodyVis && ActiveEmote == "chair");
            }

            int animStep = msg["anim_step"]?.GetValue<int>() ?? LastAnimStep;
            string animSlot = msg["anim_slot"]?.GetValue<string>() ?? "";
            if (animStep != LastAnimStep && !string.IsNullOrEmpty(animSlot))
            {
                LastAnimStep = animStep;
                _parts.TriggerSkillAnim(animSlot);
            }

            UpdateBillboard();
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            if (RpsChoiceTimer > 0f)
            {
                RpsChoiceTimer = MathF.Max(0f, RpsChoiceTimer - dt);
                if (RpsChoiceTimer <= 0f)
                {
                    RpsChoiceText = "";
                    UpdateBillboard();
                }
            }
            Position = Lerp(Position, TargetPos, MathF.Min(1.0f, dt * 14.0f));
            RotationY = LerpAngleDeg(RotationY, TargetRotY, MathF.Min(1.0f, dt * 14.0f));
            _parts.UpdateAnimations(CharKey, dt, IsMoving, isGrounded: Grounded, velY: VelY, moveSpeedRatio: 1.0f, strafeInput: 0f, forwardInput: IsMoving ? 1f : 0f, shieldStance: ShieldStance);
        }
    }
}