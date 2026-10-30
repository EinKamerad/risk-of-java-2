using System;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.World
{
    public class SafeTravelsAmbientDrone : Entity
    {
        private readonly Vector3[] _waypoints;
        private int _wpIdx = 0;
        private readonly float _bobSeed;
        private readonly Entity _body;
        private readonly Entity _rotor;

        public SafeTravelsAmbientDrone(Vector3[] waypoints, Color accent, string tag)
            : base(position: waypoints[0])
        {
            _waypoints = waypoints;
            _bobSeed = waypoints[0].X * 0.3f + waypoints[0].Z * 0.7f;
            _body = new Entity(parent: this, position: new(0, 2.05f, 0));
            new Entity(parent: _body, model: "sphere", scale: new(0.75f, 0.55f, 0.85f), color: Rgb(215, 224, 238));
            new Entity(parent: _body, model: "sphere", scaleUniform: 0.34f, position: new(0, 0.02f, 0.36f), color: accent, unlit: true);
            _rotor = new Entity(parent: _body, model: "diamond", scale: new(1.1f, 0.08f, 1.1f), position: new(0, 0.38f, 0), color: accent, unlit: true);
            new Entity(parent: this, position: new(0, 2.85f, 0), color: accent)
            {
                BillboardText = tag,
                Enabled = true
            };
        }

        public override void OnUpdate(float dt)
        {
            if (IsDestroyed || _waypoints.Length == 0) return;
            float t = (float)Raylib.GetTime() + _bobSeed;
            _rotor.RotationY += 360f * dt;
            _body.Y = 2.05f + MathF.Sin(t * 3.8f) * 0.14f;

            Vector3 target = _waypoints[_wpIdx];
            Vector3 diff = target - Position;
            diff.Y = 0f;
            float d = diff.Length();
            if (d < 1.2f)
            {
                _wpIdx = (_wpIdx + 1) % _waypoints.Length;
            }
            else
            {
                Vector3 dir = diff / d;
                Position += dir * 4.2f * dt;
                Position = new Vector3(Position.X, Physics.GetTerrainSpawnHeight(Position.X, Position.Z), Position.Z);
                float yaw = MathF.Atan2(dir.X, dir.Z) * (180f / MathF.PI);
                RotationY = Engine.LerpAngleDeg(RotationY, yaw, dt * 6.0f);
            }
        }
    }

    public class SafeTravelsCosmicBackdrop : Entity
    {
        private readonly Entity _planet;
        private readonly Entity _planetRings;
        private readonly Entity _shatteredMoon;
        private readonly Entity _escortFrigate;

        public SafeTravelsCosmicBackdrop(Entity? parent = null) : base(parent: parent)
        {
            Color cyanNeon = Rgb(75, 215, 255);

            // Вращающаяся планета Petrichor V и её кольца за панорамным носовым окном мостика
            _planet = new Entity(parent: this, position: new(-38f, 15f, 128f));
            new Entity(parent: _planet, model: "sphere", scaleUniform: 28f, color: Rgb(42, 118, 175), unlit: true);
            new Entity(parent: _planet, model: "sphere", scale: new(28.6f, 26.5f, 28.6f), rotation: new(12f, 28f, 0f), color: Rgb(68, 195, 165, 140), unlit: true);
            _planetRings = new Entity(parent: this, model: "diamond", scale: new(48f, 0.65f, 48f), position: new(-38f, 15f, 128f), rotation: new(22f, 0f, -16f), color: Rgb(125, 220, 255, 155), unlit: true);

            // Расколотая Луна Митрикса с осколками на орбите
            _shatteredMoon = new Entity(parent: this, position: new(36f, 24f, 134f));
            new Entity(parent: _shatteredMoon, model: "sphere", scaleUniform: 18f, color: Rgb(195, 210, 232), unlit: true);
            new Entity(parent: _shatteredMoon, model: "diamond", scale: new(9.5f, 3.2f, 9.5f), position: new(0f, 10.5f, 0f), color: cyanNeon, unlit: true);
            foreach (var (ox, oy, oz, sc) in new[] { (-13f, 6f, -4f, 4.2f), (12f, -5f, 3f, 3.6f), (-8f, -10f, 5f, 3.1f), (9f, 9f, -6f, 2.8f) })
                new Entity(parent: _shatteredMoon, model: "diamond", scaleUniform: sc, position: new(ox, oy, oz), rotation: new(25f, 40f, 15f), color: Rgb(165, 185, 215), unlit: true);

            // Сопровождающий фрегат эскорта UES за иллюминатором мостика
            _escortFrigate = new Entity(parent: this, position: new(0f, 22f, 112f), rotation: new(0f, 78f, -4f));
            new Entity(parent: _escortFrigate, model: "cube", scale: new(5.5f, 3.2f, 18.0f), color: Rgb(185, 196, 215), unlit: true);
            new Entity(parent: _escortFrigate, model: "cube", scale: new(5.8f, 0.6f, 14.0f), position: new(0, 0.6f, 0), color: Rgb(235, 130, 38), unlit: true);
            new Entity(parent: _escortFrigate, model: "sphere", scale: new(3.5f, 2.2f, 2.2f), position: new(0, 0, -9.5f), color: Rgb(85, 215, 255), unlit: true);

            // Звёздное поле дальнего космоса снаружи всех 5 крыльев крейсера (радиус 115..165 м)
            var rng = new Random(7742);
            for (int i = 0; i < 95; i++)
            {
                float ang = (float)(rng.NextDouble() * Math.PI * 2.0);
                float dist = 115f + (float)rng.NextDouble() * 50f;
                float sy = -10f + (float)rng.NextDouble() * 68f;
                float sz = 0.25f + (float)rng.NextDouble() * 0.52f;
                Color sCol = (i % 4 == 0) ? Rgb(145, 215, 255) : ((i % 7 == 0) ? Rgb(255, 215, 145) : Color.White);
                new Entity(parent: this, model: "sphere", scaleUniform: sz,
                    position: new(MathF.Cos(ang) * dist, sy, MathF.Sin(ang) * dist), color: sCol, unlit: true);
            }
        }

        public override void OnUpdate(float dt)
        {
            _planet.RotationY += 4.5f * dt;
            _planetRings.RotationY -= 7.0f * dt;
            _shatteredMoon.RotationY += 6.2f * dt;
            _escortFrigate.Y = 22f + MathF.Sin((float)Raylib.GetTime() * 0.85f) * 0.65f;
        }
    }
}
