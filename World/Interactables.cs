using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using RiskOfCSharp2.Models;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.World
{
    public interface IPlayerContext
    {
        Vector3 PlayerPos { get; set; }
        Vector3 PlayerForward { get; }
        float PlayerVelY { get; set; }
        bool PlayerGrounded { get; set; }
        int PlayerJumpsLeft { get; set; }
        int PlayerMaxJumps { get; }
        float PlayerHp { get; set; }
        float PlayerMaxHp { get; }
        int Gold { get; set; }
        int LunarCoins { get; set; }
        bool BlueOrbSpawned { get; set; }
        bool IsBossAlive { get; }
        bool CanPlayerInteract { get; }
        float MoonDetonationTimer { get; }
        Dictionary<string, int> Items { get; }
        List<string> ItemOrder { get; }
        List<Entity> StageEntities { get; }

        void HealPlayer(float amount);
        void ApplyDash(Vector3 dashVec, float duration);
        void CollectItem(string itemKey);
        void SpawnItemDrop(Vector3 pos, string? itemKey = null, string poolType = "small_chest");
        void ShowBanner(string msg, Color col, float duration = 2.5f);
        void OnTeleporterActivated();
        void NextStage();
        void EnterBazaar();
        void EnterMoonStage();
        void OnMoonPillarCharged();
        void StartMithrixEncounter();
        void OnMithrixDefeated(Vector3 deathPos);
        void OnRunVictory();
        void UnlockAchievement(string achKey);
        Vector3 GetNearestPlayerPos(Vector3 fromPos, out bool isLocalPlayer);
        bool IsNearRemotePlayer(Vector3 pos, float radius);
        void NotifyEnemyDamaged(int netId, float amount, float currentHp, int phase);
        void NotifyEnemyKilled(int netId);
        void NotifyTeleporterState(string action, bool alignedToMoon);
        void NotifyChestOpened(int chestId, string itemKey);
        void NotifyMoonPillarState(string pillarType, string state);
        void OpenCommandPicker(string tier);
    }

    public static class WorldContext
    {
        public static IPlayerContext? Game;
    }

    public class LightningBeam : Entity
    {
        public LightningBeam(Vector3 startPos, Vector3 endPos, Color beamColor, float thickness = 0.18f)
            : base(model: "cube", position: (startPos + endPos) * 0.5f, color: beamColor, unlit: true)
        {
            float len = MathF.Max(0.1f, Vector3.Distance(startPos, endPos));
            Scale = new Vector3(thickness, thickness, len);
            LookAt(endPos);
            AnimateScale(new Vector3(0.01f, 0.01f, len), 0.25f);
            Destroy(this, 0.26f);
        }
    }

    public class ItemDrop : Entity
    {
        public readonly string ItemKey;
        public readonly ItemInfo Info;
        private Vector3 _vel;
        private bool _landed = false;
        private float _lockTimer = 1.35f;
        private readonly Entity _visual;
        private readonly Entity _ring;
        private readonly Entity _labelEnt;
        private static readonly Random _rng = new();

        public ItemDrop(Vector3 startPos, string itemKey)
            : base(position: startPos + new Vector3(0, 1.2f, 0))
        {
            ItemKey = itemKey;
            Info = GameData.ItemPool[itemKey];
            float ang = (float)(_rng.NextDouble() * Math.PI * 2.0);
            _vel = new Vector3(MathF.Cos(ang) * 4.2f, 9.5f, MathF.Sin(ang) * 4.2f);

            _visual = ItemBuilder.BuildItemMesh(this, itemKey);
            float beamH = Info.Tier is "red" or "void" or "lunar" ? 6.5f : 4.5f;
            float beamW = Info.Tier is "red" or "void" or "lunar" ? 0.13f : 0.08f;
            new Entity(parent: this, model: "cube", scale: new(beamW, beamH, beamW), position: new(0, beamH * 0.5f, 0), color: Info.Color, unlit: true);
            _ring = new Entity(parent: this, model: "diamond", scale: new(1.25f, 0.08f, 1.25f), position: new(0, -0.35f, 0), color: Info.Color, unlit: true);
            bool isCmd = GameData.HasArtifact("cmd");
            _labelEnt = new Entity(parent: this, position: new(0, 1.85f, 0), color: Info.Color)
            {
                BillboardText = isCmd
                    ? $"[ЭССЕНЦИЯ КОМАНДОВАНИЯ: {Info.TierName}]\nНажмите [E] для выбора любого предмета!"
                    : $"[{Info.TierName}] {Info.Name}\n{Info.Desc}"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            if (!_landed)
            {
                float prevY = Y;
                _vel.Y -= 22.0f * dt;
                Position += _vel * dt;
                float floorH = Physics.GetSurfaceHeight(X, Z, Y, prevY);
                float targetY = floorH + 0.85f;
                if (Y <= targetY)
                {
                    Y = targetY;
                    _landed = true;
                }
            }
            else
            {
                _visual.Y = MathF.Sin((float)Raylib.GetTime() * 3.5f) * 0.15f;
                if (_lockTimer > 0f)
                {
                    _lockTimer -= dt;
                    if (_lockTimer <= 0f)
                    {
                        bool isCmd = GameData.HasArtifact("cmd");
                        _labelEnt.BillboardText = isCmd
                            ? $"[E] ЭССЕНЦИЯ КОМАНДОВАНИЯ [{Info.TierName}]\nВыбрать любой предмет этой редкости"
                            : $"[E] {Info.Name} [{Info.TierName}]\n{Info.Desc}";
                    }
                }
            }

            _visual.RotationY += 95f * dt;
            _ring.RotationY -= 60f * dt;

            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 8.5f;
            if (_landed && _lockTimer <= 0f && g.CanPlayerInteract)
            {
                if (dist < 2.0f || (dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E)))
                {
                    if (GameData.HasArtifact("cmd"))
                        g.OpenCommandPicker(Info.Tier);
                    else
                        g.CollectItem(ItemKey);
                    new VisualFX(Position, Info.Color, 2.8f, 0.3f);
                    g.StageEntities.Remove(this);
                    Destroy(this);
                }
            }
        }
    }

    public class Chest : Entity
    {
        public readonly int ChestId;
        public readonly string ChestType;
        public readonly int Cost;
        public readonly string PoolType;
        public bool Opened = false;

        private readonly Entity _lid;
        private readonly Entity _lockGem;
        private readonly Entity _labelEnt;

        public Chest(Vector3 pos, int baseCost, int chestId = 0, string chestType = "small")
            : base(position: pos)
        {
            ChestId = chestId;
            ChestType = chestType;

            Color bodyCol, trimCol, lidCol, lblCol;
            float sc;
            string titleStr;

            if (chestType == "large")
            {
                Cost = (int)(baseCost * 2.0f);
                PoolType = "large_chest";
                bodyCol = Rgb(40, 115, 95); trimCol = Rgb(85, 235, 95); lidCol = Rgb(55, 135, 110);
                sc = 1.25f;
                titleStr = $"[E] Большой Сундук (${Cost}) [Зелёный/Красный]";
                lblCol = Rgb(85, 235, 95);
            }
            else if (chestType == "legendary")
            {
                Cost = (int)(baseCost * 6.0f);
                PoolType = "legendary_chest";
                bodyCol = Rgb(145, 35, 45); trimCol = Color.Gold; lidCol = Rgb(185, 50, 60);
                sc = 1.45f;
                titleStr = $"[E] Легендарный Сундук (${Cost}) [100% Легендарный!]";
                lblCol = Rgb(255, 75, 85);
            }
            else if (chestType == "lunar_pod")
            {
                Cost = 1;
                PoolType = "lunar_pod";
                bodyCol = Rgb(38, 52, 88); trimCol = Rgb(165, 110, 255); lidCol = Rgb(65, 175, 255);
                sc = 1.15f;
                titleStr = "[E] Лунный Бутон (1 Лунная Монета) [Лунный / Пустота]";
                lblCol = Rgb(150, 195, 255);
            }
            else
            {
                Cost = baseCost;
                PoolType = "small_chest";
                bodyCol = Rgb(55, 105, 175); trimCol = Color.SkyBlue; lidCol = Rgb(75, 85, 105);
                sc = 1.0f;
                titleStr = $"[E] Сундук (${Cost})";
                lblCol = Color.Yellow;
            }

            if (chestType == "lunar_pod")
            {
                new Entity(parent: this, model: "diamond", scale: new(1.5f * sc, 0.55f * sc, 1.5f * sc), position: new(0, 0.25f, 0), color: bodyCol);
                new Entity(parent: this, model: "diamond", scale: new(1.62f * sc, 0.14f * sc, 1.62f * sc), position: new(0, 0.42f, 0), color: trimCol, unlit: true);
                _lid = new Entity(parent: this, position: new(0, 0.9f * sc, 0));
                new Entity(parent: _lid, model: "sphere", scale: new(1.15f * sc, 1.35f * sc, 1.15f * sc), color: Rgb(75, 165, 255, 150), unlit: true);
                _lockGem = new Entity(parent: _lid, model: "diamond", scale: new(0.65f * sc, 0.95f * sc, 0.65f * sc), color: trimCol, unlit: true);
            }
            else
            {
                new Entity(parent: this, model: "cube", scale: new(1.5f * sc, 0.2f * sc, 1.1f * sc), position: new(0, 0.1f * sc, 0), color: Rgb(45, 50, 62));
                new Entity(parent: this, model: "cube", scale: new(1.35f * sc, 0.7f * sc, 0.95f * sc), position: new(0, 0.45f * sc, 0), color: bodyCol);
                new Entity(parent: this, model: "cube", scale: new(1.39f * sc, 0.12f * sc, 0.99f * sc), position: new(0, 0.55f * sc, 0), color: trimCol, unlit: true);
                // Угловые броне-накладки и боковые ручки (без Z-fighting)
                foreach (float sx in new[] { -0.66f * sc, 0.66f * sc })
                    new Entity(parent: this, model: "cube", scale: new(0.10f * sc, 0.68f * sc, 1.02f * sc), position: new(sx, 0.45f * sc, 0), color: Rgb(50, 58, 72));

                _lid = new Entity(parent: this, position: new(0, 0.85f * sc, 0));
                new Entity(parent: _lid, model: "cube", scale: new(1.42f * sc, 0.25f * sc, 1.02f * sc), color: lidCol);
                new Entity(parent: _lid, model: "cube", scale: new(1.24f * sc, 0.12f * sc, 0.86f * sc), position: new(0, 0.16f * sc, 0), color: bodyCol);
                _lockGem = new Entity(parent: _lid, model: "diamond", scaleUniform: 0.38f * sc, position: new(0, 0, 0.54f * sc), color: Color.Gold, unlit: true);
            }

            _labelEnt = new Entity(parent: this, position: new(0, 2.2f * sc + 0.2f, 0), color: lblCol)
            {
                BillboardText = titleStr
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 0.72f * sc, 1.05f * sc, pos.Y));
        }

        public void OpenVisual(string? itemKey = null)
        {
            if (Opened) return;
            Opened = true;
            _labelEnt.Enabled = false;
            _lockGem.Color = Color.Gray;
            if (ChestType == "lunar_pod")
            {
                _lid.Enabled = false;
            }
            else
            {
                _lid.AnimatePosition(new Vector3(0, _lid.Y + 0.4f, -0.45f), 0.28f);
                _lid.AnimateRotation(new Vector3(-70f, 0, 0), 0.28f);
            }

            new VisualFX(Position + new Vector3(0, 1.0f, 0), ChestType == "lunar_pod" ? Color.SkyBlue : Color.Gold, 3.2f, 0.35f);
            WorldContext.Game?.SpawnItemDrop(Position, itemKey, PoolType);

            int fwStacks = Entities.CombatManager.GetItemCount("bundle_fireworks");
            if (fwStacks > 0 && Entities.CombatManager.Controller != null)
            {
                int rockets = Math.Min(16, 4 + fwStacks * 3);
                for (int i = 0; i < rockets; i++)
                {
                    float ang = (i / (float)rockets) * MathF.PI * 2f;
                    Vector3 d = Vector3.Normalize(new Vector3(MathF.Cos(ang) * 0.5f, 1.0f, MathF.Sin(ang) * 0.5f));
                    new Entities.Projectile(Position + new Vector3(0, 1.6f, 0), d, 42f,
                        Entities.CombatManager.Controller.BaseDamage * 1.35f, 0.30f, Rgb(255, 95, 215),
                        isPlayer: true, aoe: 2.2f, homing: true, canProc: false);
                }
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (Opened || g == null || IsDestroyed) return;

            float t = (float)Raylib.GetTime();
            if (ChestType == "lunar_pod")
            {
                _lockGem.RotationY += 80f * dt;
                _lockGem.Y = MathF.Sin(t * 3.0f) * 0.08f;
            }
            else
            {
                _lockGem.RotationZ = MathF.Sin(t * 3.5f) * 12f;
            }

            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 7.2f;

            if (dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                if (ChestType == "lunar_pod")
                {
                    if (g.LunarCoins >= Cost)
                    {
                        g.LunarCoins -= Cost;
                        string rolled = GameData.RollItemFromPool(PoolType);
                        OpenVisual(rolled);
                        g.NotifyChestOpened(ChestId, rolled);
                    }
                    else
                    {
                        g.ShowBanner("НУЖНА 1 ЛУННАЯ МОНЕТА ДЛЯ ОТКРЫТИЯ БУТОНА!", Color.Red, 1.8f);
                    }
                }
                else
                {
                    if (g.Gold >= Cost)
                    {
                        g.Gold -= Cost;
                        string rolled = GameData.RollItemFromPool(PoolType);
                        OpenVisual(rolled);
                        g.NotifyChestOpened(ChestId, rolled);
                    }
                    else
                    {
                        g.ShowBanner($"НЕДОСТАТОЧНО ЗОЛОТА (НУЖНО ${Cost})!", Color.Orange, 1.5f);
                    }
                }
            }
        }
    }

    public class HealingShrine : Entity
    {
        private int _cost;
        private float _cooldown = 0f;
        private readonly Entity _orb;
        private readonly Entity _labelEnt;

        public HealingShrine(Vector3 pos, int cost) : base(position: pos)
        {
            _cost = cost;
            new Entity(parent: this, model: "cube", scale: new(1.8f, 0.4f, 1.8f), position: new(0, 0.2f, 0), color: Rgb(65, 75, 70));
            new Entity(parent: this, model: "diamond", scale: new(1.1f, 2.8f, 1.1f), position: new(0, 1.6f, 0), color: Rgb(75, 95, 85));
            _orb = new Entity(parent: this, model: "diamond", scale: new(0.8f, 1.2f, 0.8f), position: new(0, 3.4f, 0), color: Color.Lime, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 4.5f, 0), color: Color.Lime)
            {
                BillboardText = $"[E] Алтарь Леса (${_cost} -> Лечение 60%)"
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 0.95f, 3.6f, pos.Y));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            float t = (float)Raylib.GetTime();
            _orb.RotationY += 70f * dt;
            _orb.Y = 3.4f + MathF.Sin(t * 3.0f) * 0.16f;
            _cooldown = MathF.Max(0f, _cooldown - dt);
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 7.5f && _cooldown <= 0f;

            if (dist < 4.5f && _cooldown <= 0f && Raylib.IsKeyPressed(KeyboardKey.E))
            {
                if (g.Gold >= _cost)
                {
                    g.Gold -= _cost;
                    _cooldown = 2.0f;
                    _cost = (int)(_cost * 1.5f);
                    _labelEnt.BillboardText = $"[E] Алтарь Леса (${_cost} -> Лечение 60%)";
                    g.HealPlayer(g.PlayerMaxHp * 0.6f);
                    new VisualFX(Position + new Vector3(0, 1.5f, 0), Color.Lime, 8.0f, 0.45f);
                    g.ShowBanner("БЛАГОСЛОВЕНИЕ АЛТАРЯ: +60% HP", Color.Lime, 2.0f);
                }
            }
        }
    }

    public class NewtAltar : Entity
    {
        private bool _used = false;
        private readonly Entity _crystal;
        private readonly Entity _ring;
        private readonly Entity _labelEnt;

        public NewtAltar(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "diamond", scale: new(1.5f, 0.6f, 1.5f), position: new(0, 0.3f, 0), color: Rgb(38, 48, 72));
            _crystal = new Entity(parent: this, model: "diamond", scale: new(0.85f, 2.2f, 0.85f), position: new(0, 1.5f, 0), color: Rgb(60, 175, 255), unlit: true);
            _ring = new Entity(parent: this, model: "diamond", scale: new(1.6f, 0.12f, 1.6f), position: new(0, 1.4f, 0), color: Color.SkyBlue, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 3.1f, 0), color: Rgb(80, 200, 255))
            {
                BillboardText = "[E] Алтарь Тритона (1 Лунная Монета -> Синий Портал)"
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 0.85f, 2.6f, pos.Y));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (_used || g == null || IsDestroyed) return;
            float t = (float)Raylib.GetTime();
            _crystal.RotationY += 65f * dt;
            _crystal.Y = 1.5f + MathF.Sin(t * 2.8f) * 0.14f;
            _ring.RotationY -= 90f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 7.5f;

            if (dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E))
            {
                if (g.LunarCoins >= 1)
                {
                    g.LunarCoins -= 1;
                    _used = true;
                    _labelEnt.Enabled = false;
                    _crystal.Color = Color.Gray;
                    g.BlueOrbSpawned = true;
                    new VisualFX(Position + new Vector3(0, 1.5f, 0), Color.SkyBlue, 6.0f, 0.4f);
                    g.ShowBanner("ПОЯВИЛАСЬ СИНЯЯ СФЕРА! У ТЕЛЕПОРТА ОТКРОЕТСЯ ПОРТАЛ НА БАЗАР", Rgb(80, 210, 255), 3.5f);
                }
                else
                {
                    g.ShowBanner("НЕДОСТАТОЧНО ЛУННЫХ МОНЕТ (НУЖНА 1 МОНЕТА)", Color.Red, 2.0f);
                }
            }
        }
    }

    public class BluePortal : Entity
    {
        public readonly string TargetMode;
        private bool _used = false;
        private float _warmupTimer = 0.65f;
        private readonly Entity _ring;
        private readonly Entity _labelEnt;

        public BluePortal(Vector3 pos, string targetMode = "bazaar") : base(position: pos)
        {
            TargetMode = targetMode;
            _ring = new Entity(parent: this, model: "diamond", scale: new(4.2f, 5.2f, 0.35f), position: new(0, 2.7f, 0), color: Rgb(50, 150, 255), unlit: true);
            new Entity(parent: this, model: "sphere", scale: new(3.2f, 4.2f, 0.25f), position: new(0, 2.7f, 0), color: Rgb(130, 230, 255, 190), unlit: true);
            string txt = targetMode == "bazaar"
                ? "[E] Войти в Синий Портал -> Базар Между Временем"
                : "[E] Покинуть Базар -> Следующий Этап";
            _labelEnt = new Entity(parent: this, position: new(0, 5.8f, 0), color: Color.SkyBlue) { BillboardText = txt };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (_used || g == null || IsDestroyed) return;
            _ring.RotationZ += 75f * dt;
            if (_warmupTimer > 0f)
            {
                _warmupTimer -= dt;
                return;
            }

            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 11.0f;
            if (dist < 5.2f && Raylib.IsKeyPressed(KeyboardKey.E))
            {
                _used = true;
                if (TargetMode == "bazaar") g.EnterBazaar();
                else g.NextStage();
            }
        }
    }

    public class Teleporter : Entity
    {
        public string State = "idle";
        public float Charge = 0f;
        public readonly float ZoneRadius = 18.0f;
        public bool AlignedToMoon = false;
        private BluePortal? _spawnedBluePortal;

        private readonly List<Entity> _prongs = new();
        private readonly Entity _core;
        private readonly Entity _zoneRing;
        private readonly Entity _labelEnt;

        public Teleporter(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(6.2f, 0.3f, 6.2f), position: new(0, 0.15f, 0), color: Rgb(50, 55, 65));
            new Entity(parent: this, model: "cube", scale: new(4.4f, 0.45f, 4.4f), position: new(0, 0.38f, 0), color: Rgb(70, 75, 88));

            foreach (float angle in new[] { 0f, 90f, 180f, 270f })
            {
                float rad = angle * (MathF.PI / 180f);
                float px = MathF.Cos(rad) * 2.2f, pz = MathF.Sin(rad) * 2.2f;
                new Entity(parent: this, model: "cube", scale: new(0.7f, 1.6f, 0.7f), position: new(px, 0.9f, pz), color: Rgb(45, 50, 60));
                _prongs.Add(new Entity(parent: this, model: "diamond", scale: new(0.55f, 2.4f, 0.55f), position: new(px, 2.3f, pz), color: Color.Red, unlit: true));
                Physics.Obstacles.Add(new ObstacleCircle(pos.X + px, pos.Z + pz, 0.55f, 3.4f, pos.Y));
            }
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 2.4f, 0.55f, pos.Y));

            _core = new Entity(parent: this, model: "diamond", scale: new(1.1f, 1.6f, 1.1f), position: new(0, 2.6f, 0), color: Color.Red, unlit: true);
            _zoneRing = new Entity(parent: this, model: "sphere", scale: new(ZoneRadius * 2f, 0.05f, ZoneRadius * 2f), position: new(0, 0.05f, 0), color: Rgb(255, 60, 60, 45), unlit: true, enabled: false);
            _labelEnt = new Entity(parent: this, position: new(0, 4.8f, 0), color: Color.Red)
            {
                BillboardText = "[E] Активировать Телепорт\n[F] Цель: ПЛАНЕТА (Продолжить Цикл)"
            };
        }

        public void SetAlignment(bool alignedVal)
        {
            AlignedToMoon = alignedVal;
            var g = WorldContext.Game;
            Color col = AlignedToMoon ? Rgb(80, 215, 255) : Color.Red;
            foreach (var p in _prongs) p.Color = col;
            _core.Color = col;
            _labelEnt.Color = col;

            if (AlignedToMoon)
            {
                _labelEnt.BillboardText = State == "ready"
                    ? "[E] Войти в Телепорт -> ЛУНА (К МИТРИКСУ!)"
                    : "[E] Активировать Изначальный Телепорт\n[F] Цель: ЛУНА (Отправиться к Митриксу!)";
                g?.ShowBanner("ГРАНИ ТЕЛЕПОРТА СМЕСТИЛИСЬ И СМОТРЯТ НА ЛУНУ...", col, 3.0f);
            }
            else
            {
                _labelEnt.BillboardText = State == "ready"
                    ? "[E] Войти в Телепорт -> СЛЕДУЮЩИЙ ЭТАП"
                    : "[E] Активировать Телепорт\n[F] Цель: ПЛАНЕТА (Продолжить Цикл)";
                g?.ShowBanner("ГРАНИ ТЕЛЕПОРТА ВЫРОВНЕНЫ С ПЛАНЕТОЙ", Color.Orange, 2.5f);
            }
        }

        private void TrySpawnBluePortal(IPlayerContext g)
        {
            if (g.BlueOrbSpawned && _spawnedBluePortal == null)
            {
                g.BlueOrbSpawned = false;
                Vector3 toCenter = new(-X, 0, -Z);
                Vector3 baseDir = toCenter.Length() > 1f ? Vector3.Normalize(toCenter) : new Vector3(-1, 0, 0);
                float rad = 32f * (MathF.PI / 180f);
                Vector3 rotDir = Vector3.Normalize(new Vector3(
                    baseDir.X * MathF.Cos(rad) - baseDir.Z * MathF.Sin(rad),
                    0,
                    baseDir.X * MathF.Sin(rad) + baseDir.Z * MathF.Cos(rad)
                ));
                Vector3 bpPos = Physics.ResolveHorizontalCollision(Position + rotDir * 26.0f, 2.5f, 0f);
                _spawnedBluePortal = new BluePortal(new Vector3(bpPos.X, 0f, bpPos.Z), "bazaar");
                g.StageEntities.Add(_spawnedBluePortal);
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            float t = (float)Raylib.GetTime();
            float speedMult = State == "charging" ? 2.4f : 1.0f;
            _core.Y = 2.6f + MathF.Sin(t * 3.2f * speedMult) * 0.35f;
            _core.RotationY += 75f * speedMult * dt;
            for (int i = 0; i < _prongs.Count; i++)
            {
                _prongs[i].Y = 2.3f + MathF.Sin(t * 2.8f * speedMult + i * 1.57f) * 0.18f;
                _prongs[i].RotationY += 55f * speedMult * dt;
            }
            float dist = Vector3.Distance(Position, g.PlayerPos);

            if (State == "idle")
            {
                _labelEnt.Enabled = dist < 12.5f;
                if (dist < 5.8f && Raylib.IsKeyPressed(KeyboardKey.F) && g.CanPlayerInteract)
                {
                    SetAlignment(!AlignedToMoon);
                    g.NotifyTeleporterState("align", AlignedToMoon);
                }
                if (dist < 5.0f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    State = "charging";
                    _zoneRing.Enabled = true;
                    g.NotifyTeleporterState("activate", AlignedToMoon);
                    g.OnTeleporterActivated();
                }
            }
            else if (State == "charging")
            {
                _labelEnt.Enabled = true;
                if (dist <= ZoneRadius)
                {
                    Charge = MathF.Min(100f, Charge + 5.5f * dt);
                    _zoneRing.Color = Rgb(255, 80, 80, 55);
                }
                else
                {
                    _zoneRing.Color = Rgb(120, 120, 120, 35);
                }

                string status = g.IsBossAlive ? "БОСС ЖИВ!" : "ЗАРЯДКА";
                _labelEnt.BillboardText = $"Телепорт: {(int)Charge}% ({status})";

                if (Charge >= 100f && !g.IsBossAlive)
                {
                    State = "ready";
                    _core.Color = Color.SkyBlue;
                    foreach (var p in _prongs) p.Color = Color.SkyBlue;
                    _zoneRing.Color = Rgb(60, 255, 220, 55);
                    _labelEnt.Color = Color.SkyBlue;
                    string destStr = AlignedToMoon ? "ЛУНА (К МИТРИКСУ!)" : "СЛЕДУЮЩИЙ ЭТАП";
                    _labelEnt.BillboardText = $"[E] Войти в Телепорт -> {destStr}";
                    g.ShowBanner("ТЕЛЕПОРТ ЗАРЯЖЕН! НАЖМИТЕ [E] ДЛЯ ПРЫЖКА", Color.SkyBlue, 3.5f);
                    TrySpawnBluePortal(g);
                }
            }
            else if (State == "ready")
            {
                TrySpawnBluePortal(g);
                if (_spawnedBluePortal != null && !_spawnedBluePortal.IsDestroyed)
                {
                    float bpDist = Vector3.Distance(_spawnedBluePortal.Position, g.PlayerPos);
                    if (bpDist < 5.5f && bpDist < dist) return;
                }

                if (dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    if (AlignedToMoon) g.EnterMoonStage();
                    else g.NextStage();
                }
            }
        }
    }

    public class LunarShopBud : Entity
    {
        private readonly string _itemKey;
        private readonly int _cost;
        private bool _bought = false;
        private readonly Entity _pod;
        private readonly Entity _visual;
        private readonly Entity _labelEnt;

        public LunarShopBud(Vector3 pos, string itemKey, int cost = 2) : base(position: pos)
        {
            _itemKey = itemKey;
            _cost = cost;
            var info = GameData.ItemPool[itemKey];
            new Entity(parent: this, model: "diamond", scale: new(1.6f, 0.8f, 1.6f), position: new(0, 0.4f, 0), color: Rgb(35, 50, 82));
            _pod = new Entity(parent: this, model: "sphere", scale: new(1.3f, 1.6f, 1.3f), position: new(0, 1.5f, 0), color: Rgb(70, 175, 255, 85), unlit: true);
            _visual = ItemBuilder.BuildItemMesh(this, itemKey);
            _visual.Y = 1.5f;
            _labelEnt = new Entity(parent: this, position: new(0, 3.1f, 0), color: Rgb(110, 215, 255))
            {
                BillboardText = $"[E] {info.Name} ({_cost} Лунные Монеты)\n{info.Desc}"
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 0.85f, 2.3f, pos.Y));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (_bought || g == null || IsDestroyed) return;
            _visual.RotationY += 85f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 7.0f;
            if (dist < 3.6f && Raylib.IsKeyPressed(KeyboardKey.E))
            {
                if (g.LunarCoins >= _cost)
                {
                    g.LunarCoins -= _cost;
                    _bought = true;
                    _labelEnt.Enabled = false;
                    _pod.Enabled = false;
                    _visual.Enabled = false;
                    g.CollectItem(_itemKey);
                    new VisualFX(Position + new Vector3(0, 1.5f, 0), Color.SkyBlue, 3.5f, 0.35f);
                }
                else
                {
                    g.ShowBanner("НЕ ХВАТАЕТ ЛУННЫХ МОНЕТ!", Color.Red, 1.8f);
                }
            }
        }
    }

    public class LunarCauldron : Entity
    {
        private readonly string _rewardKey;
        private readonly int _reqCount;
        private float _cooldown = 0f;
        private readonly Entity _visual;
        private readonly Entity _labelEnt;
        private static readonly Random _rng = new();

        public LunarCauldron(Vector3 pos, string rewardKey, int reqCount = 3) : base(position: pos)
        {
            _rewardKey = rewardKey;
            _reqCount = reqCount;
            var info = GameData.ItemPool[rewardKey];
            new Entity(parent: this, model: "cube", scale: new(2.2f, 1.2f, 2.2f), position: new(0, 0.6f, 0), color: Rgb(48, 54, 68));
            new Entity(parent: this, model: "sphere", scale: new(1.9f, 0.4f, 1.9f), position: new(0, 1.25f, 0), color: reqCount >= 5 ? Color.Red : Color.Lime, unlit: true);
            _visual = ItemBuilder.BuildItemMesh(this, rewardKey);
            _visual.Y = 2.2f;
            _labelEnt = new Entity(parent: this, position: new(0, 3.5f, 0), color: reqCount >= 5 ? Color.Gold : Color.Lime)
            {
                BillboardText = $"[E] Котёл Обмена: Отдать {reqCount} предмета -> {info.Name}"
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.15f, 2.4f, pos.Y));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _visual.RotationY += 80f * dt;
            _cooldown = MathF.Max(0f, _cooldown - dt);
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 7.5f && _cooldown <= 0f;

            if (dist < 4.0f && _cooldown <= 0f && Raylib.IsKeyPressed(KeyboardKey.E))
            {
                var nonLunar = new List<string>();
                int totalAvail = 0;
                foreach (var kv in g.Items)
                {
                    if (kv.Value > 0 && !GameData.LunarItems.ContainsKey(kv.Key) && kv.Key != "tonic_affliction")
                    {
                        nonLunar.Add(kv.Key);
                        totalAvail += kv.Value;
                    }
                }
                if (totalAvail >= _reqCount)
                {
                    _cooldown = 1.5f;
                    for (int i = 0; i < _reqCount; i++)
                    {
                        var avail = nonLunar.FindAll(k => g.Items[k] > 0);
                        string takeK = avail[_rng.Next(avail.Count)];
                        g.Items[takeK]--;
                        if (g.Items[takeK] <= 0) g.ItemOrder.Remove(takeK);
                    }
                    g.CollectItem(_rewardKey);
                    new VisualFX(Position + new Vector3(0, 1.8f, 0), Color.Gold, 4.5f, 0.4f);
                }
                else
                {
                    g.ShowBanner($"НУЖНО МИНИМУМ {_reqCount} ОБЫЧНЫХ ПРЕДМЕТА ДЛЯ ОБМЕНА!", Color.Orange, 2.0f);
                }
            }
        }
    }

    public class LunarBouncePad : Entity
    {
        private readonly float _jumpVel;
        private readonly float _boostForward;
        private float _cooldown = 0f;
        private readonly Entity _ring;

        public LunarBouncePad(Vector3 pos, float jumpVel = 20.0f, float boostForward = 10.0f) : base(position: pos)
        {
            _jumpVel = jumpVel;
            _boostForward = boostForward;
            new Entity(parent: this, model: "cube", scale: new(2.8f, 0.25f, 2.8f), position: new(0, 0.12f, 0), color: Rgb(45, 58, 84));
            _ring = new Entity(parent: this, model: "diamond", scale: new(2.4f, 0.2f, 2.4f), position: new(0, 0.32f, 0), color: Color.SkyBlue, unlit: true);
            new Entity(parent: this, model: "sphere", scale: new(1.2f, 0.4f, 1.2f), position: new(0, 0.25f, 0), color: Rgb(140, 235, 255), unlit: true);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _ring.RotationY += 120f * dt;
            _cooldown = MathF.Max(0f, _cooldown - dt);
            if (_cooldown <= 0f && Physics.Distance2D(Position, g.PlayerPos) <= 2.1f && MathF.Abs(g.PlayerPos.Y - Y) <= 2.0f)
            {
                _cooldown = 0.65f;
                g.PlayerVelY = _jumpVel;
                g.PlayerGrounded = false;
                g.PlayerJumpsLeft = Math.Max(g.PlayerJumpsLeft, g.PlayerMaxJumps);
                if (_boostForward > 0f)
                    g.ApplyDash(g.PlayerForward * _boostForward, 0.28f);
                new VisualFX(Position + new Vector3(0, 0.5f, 0), Color.SkyBlue, 3.8f, 0.3f);
            }
        }
    }

    public class MoonPillar : Entity
    {
        public readonly string PillarType;
        public string State = "idle";
        public bool Completed = false;
        public float Charge = 0f;
        public readonly float Radius = 13.5f;
        private float _pulseTimer = 2.2f;

        private readonly string _title;
        private readonly Color _col;
        private readonly Entity _core;
        private readonly Entity _beacon;
        private readonly Entity _zoneRing;
        private readonly Entity _labelEnt;

        public MoonPillar(Vector3 pos, string pillarType = "mass") : base(position: pos)
        {
            PillarType = pillarType;
            (_title, _col) = pillarType switch
            {
                "design" => ("Столп Дизайна (Импульсы)", Rgb(110, 180, 255)),
                "blood" => ("Столп Крови (Поглощает HP)", Rgb(245, 65, 85)),
                "soul" => ("Столп Души (Не выходите из зоны)", Rgb(165, 110, 255)),
                _ => ("Столп Массы", Rgb(75, 210, 255))
            };

            new Entity(parent: this, model: "cube", scale: new(3.2f, 0.6f, 3.2f), position: new(0, 0.3f, 0), color: Rgb(50, 62, 84));
            new Entity(parent: this, model: "diamond", scale: new(1.6f, 5.8f, 1.6f), position: new(0, 3.2f, 0), color: Rgb(215, 225, 242));
            _core = new Entity(parent: this, model: "diamond", scale: new(2.1f, 1.5f, 2.1f), position: new(0, 3.2f, 0), color: _col, unlit: true);
            _beacon = new Entity(parent: this, model: "cube", scale: new(0.22f, 32.0f, 0.22f), position: new(0, 18.0f, 0), color: _col, unlit: true);
            _zoneRing = new Entity(parent: this, model: "sphere", scale: new(Radius * 2f, 0.08f, Radius * 2f), position: new(0, 0.08f, 0), color: Rgb(70, 200, 255, 50), unlit: true, enabled: false);
            _labelEnt = new Entity(parent: this, position: new(0, 6.8f, 0), color: _col)
            {
                BillboardText = $"[E] Активировать {_title}"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            _core.RotationY += 75f * dt;
            float dist = Physics.Distance2D(Position, g.PlayerPos);
            float heightDiff = MathF.Abs(g.PlayerPos.Y - Y);

            if (State == "idle")
            {
                _labelEnt.Enabled = dist < 22.0f;
                if (dist < 5.2f && heightDiff < 5.0f && Raylib.IsKeyPressed(KeyboardKey.E))
                {
                    ActivatePillar(fromNetwork: false);
                }
            }
            else if (State == "charging")
            {
                _labelEnt.Enabled = true;
                bool anyPlayerInside = (dist <= Radius && heightDiff <= 7.5f) || g.IsNearRemotePlayer(Position, Radius);
                if (anyPlayerInside)
                {
                    float rate = 18.0f;
                    if (PillarType == "blood")
                    {
                        rate = 25.0f;
                        if (dist <= Radius && heightDiff <= 7.5f)
                            g.PlayerHp = MathF.Max(1.0f, g.PlayerHp - 7.5f * dt);
                    }
                    else if (PillarType == "design")
                    {
                        _pulseTimer -= dt;
                        if (_pulseTimer <= 0f)
                        {
                            _pulseTimer = 2.4f;
                            new VisualFX(Position + new Vector3(0, 0.6f, 0), _col, 14.0f, 0.35f);
                            if (dist <= Radius && g.PlayerPos.Y <= Y + 1.2f)
                            {
                                Vector3 pushDir = Vector3.Normalize(g.PlayerPos - Position);
                                g.ApplyDash(new Vector3(pushDir.X * 16f, 0, pushDir.Z * 16f), 0.20f);
                            }
                        }
                    }
                    Charge = MathF.Min(100f, Charge + rate * dt);
                }
                else if (PillarType == "soul")
                {
                    Charge = MathF.Max(0f, Charge - 10.0f * dt);
                }

                _labelEnt.BillboardText = $"{_title}: {(int)Charge}%";
                if (Charge >= 100f && !Completed)
                {
                    CompletePillar(fromNetwork: false);
                }
            }
        }

        public void ActivatePillar(bool fromNetwork)
        {
            if (State != "idle") return;
            State = "charging";
            _zoneRing.Enabled = true;
            WorldContext.Game?.ShowBanner($"ЗАРЯДКА: {_title.ToUpper()}!", _col, 2.5f);
            if (!fromNetwork)
                WorldContext.Game?.NotifyMoonPillarState(PillarType, "charging");
        }

        public void CompletePillar(bool fromNetwork)
        {
            if (Completed) return;
            Completed = true;
            Charge = 100f;
            State = "done";
            _zoneRing.Enabled = false;
            _core.Color = Color.Lime;
            _beacon.Color = Color.Lime;
            _labelEnt.Color = Color.Lime;
            _labelEnt.BillboardText = $"{_title} ЗАРЯЖЕН ✓";
            new LightningBeam(Position + new Vector3(0, 4.5f, 0), new Vector3(0, Physics.ColosseumY, 0), Color.SkyBlue, 0.65f);
            if (!fromNetwork)
                WorldContext.Game?.NotifyMoonPillarState(PillarType, "done");
            WorldContext.Game?.OnMoonPillarCharged();
        }
    }

    public class LunarJumpPad : Entity
    {
        public bool Active = false;
        private bool _launching = false;
        private float _launchTimer = 0f;
        private readonly Entity _beam;
        private readonly Entity _ring;
        private readonly Entity _labelEnt;

        public LunarJumpPad(Vector3 pos) : base(position: pos)
        {
            new Entity(parent: this, model: "cube", scale: new(8.5f, 0.6f, 8.5f), position: new(0, 0.3f, 0), color: Rgb(45, 58, 82));
            new Entity(parent: this, model: "diamond", scale: new(7.2f, 0.3f, 7.2f), position: new(0, 0.65f, 0), color: Rgb(65, 85, 120));
            _beam = new Entity(parent: this, model: "cube", scale: new(3.2f, Physics.ColosseumY, 3.2f), position: new(0, Physics.ColosseumY * 0.5f, 0), color: Rgb(70, 225, 255, 110), unlit: true, enabled: false);
            _ring = new Entity(parent: this, model: "diamond", scale: new(5.8f, 0.3f, 5.8f), position: new(0, 0.8f, 0), color: Color.Gray, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 5.2f, 0), color: Color.Yellow)
            {
                BillboardText = "Зарядите все 4 Столпа Луны (0/4) для активации Грави-Лифта к Митриксу"
            };
        }

        public void UpdateProgressLabel(int done, int total = 4)
        {
            if (!Active)
                _labelEnt.BillboardText = $"Зарядите все {total} Столпа Луны ({done}/{total}) для активации Грави-Лифта к Митриксу";
        }

        public void UnlockPad()
        {
            Active = true;
            _beam.Enabled = true;
            _ring.Color = Color.SkyBlue;
            _labelEnt.Color = Color.SkyBlue;
            _labelEnt.BillboardText = "[ВСТАНЬТЕ В ЛУЧ] Грави-лифт к Небесному Колизею Митрикса (Y=120м)!";
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            if (g.MoonDetonationTimer > 0f)
            {
                Active = false;
                _launching = false;
                _beam.Enabled = false;
                _labelEnt.Enabled = false;
                _ring.Color = Color.Gray;
                return;
            }

            _ring.RotationY += 95f * dt;
            if (_launching)
            {
                _launchTimer -= dt;
                float newY = Lerp(Physics.ColosseumY + 2.5f, g.PlayerPos.Y, MathF.Max(0f, _launchTimer / 0.85f));
                g.PlayerPos = new Vector3(0, newY, -36.0f);
                g.PlayerVelY = 0f;
                if (_launchTimer <= 0f)
                {
                    _launching = false;
                    g.PlayerPos = new Vector3(0, Physics.ColosseumY + 1.5f, -36.0f);
                    g.PlayerVelY = 0f;
                    g.PlayerGrounded = true;
                    new VisualFX(new Vector3(0, Physics.ColosseumY + 1.5f, -36.0f), Color.SkyBlue, 8.0f, 0.45f);
                    g.StartMithrixEncounter();
                }
                return;
            }

            if (Active && Physics.Distance2D(Position, g.PlayerPos) < 4.0f && g.PlayerPos.Y < 15.0f)
            {
                _launching = true;
                _launchTimer = 0.85f;
                new VisualFX(g.PlayerPos + new Vector3(0, 1.5f, 0), Color.SkyBlue, 7.5f, 0.4f);
                g.ShowBanner("ПОДЪЁМ В НЕБЕСНЫЙ КОЛИЗЕЙ МИТРИКСА...", Color.SkyBlue, 2.5f);
            }
        }
    }

    public class LunarEscapePad : Entity
    {
        private readonly Vector3 _targetPos;
        private float _cooldown = 0f;
        private readonly Entity _ring;

        public LunarEscapePad(Vector3 pos, Vector3 targetPos) : base(position: pos)
        {
            _targetPos = targetPos;
            _ring = new Entity(parent: this, model: "diamond", scale: new(5.5f, 0.35f, 5.5f), position: new(0, 0.4f, 0), color: Color.Lime, unlit: true);
            new Entity(parent: this, model: "cube", scale: new(2.2f, 28.0f, 2.2f), position: new(0, 14.0f, 0), color: Rgb(80, 255, 140, 100), unlit: true);
            new Entity(parent: this, position: new(0, 5.2f, 0), color: Color.Lime)
            {
                BillboardText = "[ВСТАНЬТЕ В ЗЕЛЁНЫЙ ЛУЧ] Эвакуационный спуск к Кораблю!"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _ring.RotationY += 120f * dt;
            _cooldown = MathF.Max(0f, _cooldown - dt);

            if (_cooldown <= 0f && g.PlayerPos.Y >= Physics.ColosseumY - 10.0f)
            {
                if (Physics.Distance2D(Position, g.PlayerPos) < 3.8f && MathF.Abs(g.PlayerPos.Y - Y) < 6.0f)
                {
                    _cooldown = 3.0f;
                    new VisualFX( g.PlayerPos, Color.Lime, 6.0f, 0.35f);
                    Vector3 landPos = _targetPos + new Vector3(0, 2.0f, 14.0f);
                    g.PlayerPos = landPos;
                    g.PlayerVelY = 0f;
                    g.PlayerGrounded = true;
                    new VisualFX(landPos, Color.Lime, 6.0f, 0.4f);
                    g.ShowBanner("СПУСК ЗАВЕРШЁН! УДЕРЖИВАЙТЕ ЗОНУ СПАСАТЕЛЬНОГО КОРАБЛЯ ВПЕРЕДИ!", Color.Lime, 4.0f);
                }
            }
        }
    }

    public class EscapeShip : Entity
    {
        public float Charge = 0f;
        public readonly float Radius = 15.0f;
        private readonly Entity _labelEnt;

        public EscapeShip(Vector3 pos) : base(position: pos)
        {
            var hull = new Entity(parent: this, position: new(0, 2.0f, 0));
            new Entity(parent: hull, model: "cube", scale: new(5.5f, 2.6f, 9.0f), color: Rgb(215, 225, 240));
            new Entity(parent: hull, model: "diamond", scale: new(5.0f, 2.1f, 4.2f), position: new(0, 0, 5.0f), color: Rgb(235, 130, 35));
            new Entity(parent: hull, model: "cube", scale: new(3.8f, 1.1f, 3.0f), position: new(0, 0.7f, 3.0f), color: Color.SkyBlue, unlit: true);
            new Entity(parent: hull, model: "cube", scale: new(9.5f, 0.35f, 3.4f), position: new(0, -0.4f, 0), color: Rgb(65, 75, 92));

            new Entity(parent: this, model: "cube", scale: new(0.45f, 90.0f, 0.45f), position: new(0, 45.0f, 0), color: Rgb(80, 255, 120, 95), unlit: true);
            new Entity(parent: this, model: "sphere", scale: new(Radius * 2f, 0.10f, Radius * 2f), position: new(0, 0.10f, 0), color: Rgb(80, 255, 120, 75), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 6.5f, 0), color: Color.Lime)
            {
                BillboardText = "ЭВАКУАЦИЯ: УДЕРЖИВАЙТЕ ЗОНУ КОРАБЛЯ СПАСЕНИЯ!"
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 3.0f, 3.6f, pos.Y));
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z + 3.2f, 2.4f, 3.2f, pos.Y));
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z - 2.8f, 2.6f, 3.2f, pos.Y));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            if (Vector3.Distance(Position, g.PlayerPos) <= Radius || g.IsNearRemotePlayer(Position, Radius))
            {
                Charge = MathF.Min(100f, Charge + 14.0f * dt);
                _labelEnt.BillboardText = $"ЗАПУСК ДВИГАТЕЛЕЙ СПАСЕНИЯ: {(int)Charge}%";
                if (Charge >= 100f)
                    g.OnRunVictory();
            }
        }
    }
}