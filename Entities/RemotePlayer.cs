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

        private SurvivorParts _parts;
        private readonly Entity _labelEnt;

        public RemotePlayer(string playerId, string charKey = "commando", int skinIdx = 0) : base(position: new(2, 0, 2))
        {
            PlayerId = playerId;
            CharKey = SurvivorDatabase.Classes.ContainsKey(charKey) ? charKey : "commando";
            SkinIdx = Math.Clamp(skinIdx, 0, 2);
            _parts = SurvivorDatabase.BuildSurvivorMesh(this, CharKey, SkinIdx);
            _labelEnt = new Entity(parent: this, position: new(0, 2.75f, 0), color: Color.SkyBlue);
            UpdateBillboard();
        }

        private void UpdateBillboard()
        {
            string cName = SurvivorDatabase.Classes[CharKey].Name;
            _labelEnt.BillboardText = $"[{PlayerId.ToUpper()}] {cName}\n{(int)Hp}/{(int)MaxHp} HP";
        }

        public void RebuildClass(string newCharKey, int newSkinIdx = 0)
        {
            newSkinIdx = Math.Clamp(newSkinIdx, 0, 2);
            if (!SurvivorDatabase.Classes.ContainsKey(newCharKey)) return;
            if (newCharKey == CharKey && newSkinIdx == SkinIdx) return;
            CharKey = newCharKey;
            SkinIdx = newSkinIdx;
            Destroy(_parts.Root);
            _parts = SurvivorDatabase.BuildSurvivorMesh(this, newCharKey, newSkinIdx);
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
            bool ultForm = msg["ult_form"]?.GetValue<bool>() ?? false;
            _parts.SetUltForm(ultForm);

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
            Position = Lerp(Position, TargetPos, MathF.Min(1.0f, dt * 14.0f));
            RotationY = Lerp(RotationY, TargetRotY, MathF.Min(1.0f, dt * 14.0f));
            _parts.UpdateAnimations(CharKey, dt, IsMoving, isGrounded: Grounded, velY: VelY, moveSpeedRatio: 1.0f, strafeInput: 0f, forwardInput: IsMoving ? 1f : 0f, shieldStance: ShieldStance);
        }
    }
}