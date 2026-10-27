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
        public Vector3 TargetPos = new(2, 0, 2);
        public float TargetRotY = 0f;
        public bool IsMoving = false;
        public float Hp = 110f;
        public float MaxHp = 110f;

        private SurvivorParts _parts;
        private readonly Entity _labelEnt;

        public RemotePlayer(string playerId, string charKey = "commando") : base(position: new(2, 0, 2))
        {
            PlayerId = playerId;
            CharKey = SurvivorDatabase.Classes.ContainsKey(charKey) ? charKey : "commando";
            _parts = SurvivorDatabase.BuildSurvivorMesh(this, CharKey);
            _labelEnt = new Entity(parent: this, position: new(0, 2.75f, 0), color: Color.SkyBlue);
            UpdateBillboard();
        }

        private void UpdateBillboard()
        {
            string cName = SurvivorDatabase.Classes[CharKey].Name;
            _labelEnt.BillboardText = $"[{PlayerId.ToUpper()}] {cName}\n{(int)Hp}/{(int)MaxHp} HP";
        }

        public void RebuildClass(string newCharKey)
        {
            if (newCharKey == CharKey || !SurvivorDatabase.Classes.ContainsKey(newCharKey)) return;
            CharKey = newCharKey;
            Destroy(_parts.Root);
            _parts = SurvivorDatabase.BuildSurvivorMesh(this, newCharKey);
            UpdateBillboard();
        }

        public void ApplyNetState(JsonObject msg)
        {
            RebuildClass(msg["char_key"]?.GetValue<string>() ?? CharKey);
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
            Hp = msg["hp"]?.GetValue<float>() ?? Hp;
            MaxHp = MathF.Max(1f, msg["max_hp"]?.GetValue<float>() ?? MaxHp);
            bool ultForm = msg["ult_form"]?.GetValue<bool>() ?? false;
            _parts.SetUltForm(ultForm);
            UpdateBillboard();
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed) return;
            Position = Lerp(Position, TargetPos, MathF.Min(1.0f, dt * 14.0f));
            RotationY = Lerp(RotationY, TargetRotY, MathF.Min(1.0f, dt * 14.0f));
            _parts.UpdateAnimations(CharKey, dt, IsMoving, isGrounded: true, velY: 0f, moveSpeedRatio: 1.0f, strafeInput: 0f, forwardInput: IsMoving ? 1f : 0f);

            if (IsMoving && CharKey != "mindustry")
            {
                float swing = MathF.Sin((float)Raylib.GetTime() * 11.5f) * 36.0f;
                _parts.LeftLeg.RotationX = swing;
                _parts.RightLeg.RotationX = -swing;
            }
            else
            {
                _parts.LeftLeg.RotationX = Lerp(_parts.LeftLeg.RotationX, 0f, dt * 10f);
                _parts.RightLeg.RotationX = Lerp(_parts.RightLeg.RotationX, 0f, dt * 10f);
            }

            if (_parts.Halo != null)
                _parts.Halo.RotationZ += 95f * dt;
        }
    }
}