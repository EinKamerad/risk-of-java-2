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
        void EnterSecretStage(string modeKey);
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
        void OpenWorkbench();
        void OpenNpcDialogue(string npcCharKey, int npcSkinIdx, Entity? npcEntity) { }
        void OpenNpcDialogue(SafeTravelsNpc npc) => OpenNpcDialogue(npc.CharKey, npc.SkinIdx, npc);
        void StartNpcRps(string npcCharKey, int npcSkinIdx, Entity? npcEntity) { }
        void StartNpcRps(SafeTravelsNpc npc) => StartNpcRps(npc.CharKey, npc.SkinIdx, npc);
        void TriggerCharacterUnlockCutscene(string charKey) { }
        void ExitSafeTravelsToMenu() { }
        void StartRunFromSafeTravels() { }
        void SwitchSafeTravelsCharacter(string charKey, int skinIdx = 0) { }
        void OpenSafeTravelsCharacterSelector() { }
        void ClearSafeTravelsTestItems() { }
        void ToggleSafeTravelsZeroCooldowns() { }
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
            SoundManager.Play("chest_open");
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
                SoundManager.Play("rocket", 0.75f, 1.15f);
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
                        SoundManager.Play("ui_error");
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
                        SoundManager.Play("ui_error");
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
                    SoundManager.Play("shrine_use");
                    g.Gold -= _cost;
                    _cooldown = 2.0f;
                    _cost = (int)(_cost * 1.5f);
                    _labelEnt.BillboardText = $"[E] Алтарь Леса (${_cost} -> Лечение 60%)";
                    g.HealPlayer(g.PlayerMaxHp * 0.6f);
                    Program.RecordInteractableOpened();
                    new VisualFX(Position + new Vector3(0, 1.5f, 0), Color.Lime, 8.0f, 0.45f);
                    g.ShowBanner("БЛАГОСЛОВЕНИЕ АЛТАРЯ: +60% HP", Color.Lime, 2.0f);
                }
                else
                {
                    SoundManager.Play("ui_error");
                    g.ShowBanner($"НЕДОСТАТОЧНО ЗОЛОТА (НУЖНО ${_cost})!", Color.Orange, 1.5f);
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
                    SoundManager.Play("portal_open");
                    g.LunarCoins -= 1;
                    _used = true;
                    _labelEnt.Enabled = false;
                    _crystal.Color = Color.Gray;
                    g.BlueOrbSpawned = true;
                    g.UnlockAchievement("ach_lunar_stone");
                    new VisualFX(Position + new Vector3(0, 1.5f, 0), Color.SkyBlue, 6.0f, 0.4f);
                    g.ShowBanner("ПОЯВИЛАСЬ СИНЯЯ СФЕРА! У ТЕЛЕПОРТА ОТКРОЕТСЯ ПОРТАЛ НА БАЗАР", Rgb(80, 210, 255), 3.5f);
                }
                else
                {
                    SoundManager.Play("ui_error");
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
                SoundManager.Play("portal_open");
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
        private MinecraftWorkbench? _spawnedWorkbench;

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
            SoundManager.Play("shrine_use", 0.85f, alignedVal ? 1.15f : 0.90f);
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
                    int prevStep = (int)(Charge / 10f);
                    Charge = MathF.Min(100f, Charge + 5.5f * dt);
                    if ((int)(Charge / 10f) > prevStep && Charge < 100f)
                        SoundManager.Play("teleporter_charge", 0.65f, 0.80f + Charge * 0.006f);
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
                    SoundManager.Play("teleporter_ready");
                    _core.Color = Color.SkyBlue;
                    foreach (var p in _prongs) p.Color = Color.SkyBlue;
                    _zoneRing.Color = Rgb(60, 255, 220, 55);
                    _labelEnt.Color = Color.SkyBlue;
                    string destStr = AlignedToMoon ? "ЛУНА (К МИТРИКСУ!)" : "СЛЕДУЮЩИЙ ЭТАП";
                    _labelEnt.BillboardText = $"[E] Войти в Телепорт -> {destStr}";
                    g.ShowBanner("ТЕЛЕПОРТ ЗАРЯЖЕН! РЯДОМ ПОЯВИЛСЯ ВЕРСТАК [E] ДЛЯ СПОКОЙНОГО КРАФТА", Color.SkyBlue, 4.0f);
                    TrySpawnBluePortal(g);
                    if (_spawnedWorkbench == null)
                    {
                        float wbY = Physics.GetSurfaceHeight(Position.X + 4.4f, Position.Z, Position.Y + 2f);
                        _spawnedWorkbench = new MinecraftWorkbench(new Vector3(Position.X + 4.4f, wbY, Position.Z));
                        g.StageEntities.Add(_spawnedWorkbench);
                    }
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
                if (_spawnedWorkbench != null && !_spawnedWorkbench.IsDestroyed)
                {
                    float wbDist = Vector3.Distance(_spawnedWorkbench.Position, g.PlayerPos);
                    if (wbDist < 3.6f && wbDist < dist) return;
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
                    SoundManager.Play("ui_error");
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
                    SoundManager.Play("ui_error");
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
                SoundManager.Play("bounce_pad");
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
            SoundManager.Play("shrine_use");
            _zoneRing.Enabled = true;
            WorldContext.Game?.ShowBanner($"ЗАРЯДКА: {_title.ToUpper()}!", _col, 2.5f);
            if (!fromNetwork)
                WorldContext.Game?.NotifyMoonPillarState(PillarType, "charging");
        }

        public void CompletePillar(bool fromNetwork)
        {
            if (Completed) return;
            Completed = true;
            SoundManager.Play("teleporter_ready");
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
                SoundManager.Play("bounce_pad", 1.1f, 0.85f);
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
                    SoundManager.Play("portal_open");
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
                int prevStep = (int)(Charge / 10f);
                Charge = MathF.Min(100f, Charge + 14.0f * dt);
                if ((int)(Charge / 10f) > prevStep && Charge < 100f)
                    SoundManager.Play("teleporter_charge", 0.70f, 0.85f + Charge * 0.007f);
                _labelEnt.BillboardText = $"ЗАПУСК ДВИГАТЕЛЕЙ СПАСЕНИЯ: {(int)Charge}%";
                if (Charge >= 100f)
                    g.OnRunVictory();
            }
        }
    }

    public class MinecraftWorkbench : Entity
    {
        private readonly Entity _holoCube;
        private readonly Entity _labelEnt;

        public MinecraftWorkbench(Vector3 pos) : base(position: pos)
        {
            // Деревянный блок верстака из Minecraft (дубовые доски + тёмная окантовка + сетка крафта 3x3 сверху)
            Color oak = Rgb(168, 122, 68);
            Color darkOak = Rgb(105, 72, 38);
            Color topRed = Rgb(175, 58, 48);
            Color gridLine = Rgb(62, 40, 22);

            new Entity(parent: this, model: "cube", scale: new(1.25f, 1.15f, 1.25f), position: new(0, 0.58f, 0), color: oak);
            new Entity(parent: this, model: "cube", scale: new(1.30f, 0.22f, 1.30f), position: new(0, 1.08f, 0), color: topRed);
            new Entity(parent: this, model: "cube", scale: new(1.16f, 0.04f, 1.16f), position: new(0, 1.20f, 0), color: Rgb(212, 168, 108));

            // Сетка 3x3 на крышке верстака
            for (int i = -1; i <= 1; i++)
            {
                new Entity(parent: this, model: "cube", scale: new(1.12f, 0.05f, 0.04f), position: new(0, 1.21f, i * 0.32f), color: gridLine);
                new Entity(parent: this, model: "cube", scale: new(0.04f, 0.05f, 1.12f), position: new(i * 0.32f, 1.21f, 0), color: gridLine);
            }

            // Боковые инструменты (пила и молоток) и угловые стойки
            foreach (float sx in new[] { -0.58f, 0.58f })
                foreach (float sz in new[] { -0.58f, 0.58f })
                    new Entity(parent: this, model: "cube", scale: new(0.14f, 1.16f, 0.14f), position: new(sx, 0.58f, sz), color: darkOak);
            new Entity(parent: this, model: "cube", scale: new(0.32f, 0.52f, 1.28f), position: new(-0.22f, 0.55f, 0), color: Rgb(185, 195, 210));
            new Entity(parent: this, model: "cube", scale: new(1.28f, 0.45f, 0.28f), position: new(0, 0.55f, 0.22f), color: darkOak);

            _holoCube = new Entity(parent: this, model: "diamond", scaleUniform: 0.48f, position: new(0, 1.75f, 0), color: Rgb(80, 240, 255), unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 2.55f, 0), color: Color.Gold)
            {
                BillboardText = "[E] ВЕРСТАК (Крафт из добытых ресурсов карты и предметов)"
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 0.72f, 1.25f, pos.Y));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;
            _holoCube.RotationY += 85f * dt;
            _holoCube.Y = 1.72f + MathF.Sin((float)Raylib.GetTime() * 3.2f) * 0.12f;

            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 8.5f;
            if (dist < 3.6f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                SoundManager.Play("chest_open", 0.85f, 1.2f);
                g.OpenWorkbench();
            }
        }
    }

    public class SecretStagePortal : Entity
    {
        public readonly string SecretMode;
        private bool _used = false;
        private readonly Entity _outerRing;
        private readonly Entity _innerCore;
        private readonly Entity _labelEnt;

        public SecretStagePortal(Vector3 pos, string secretMode) : base(position: pos)
        {
            SecretMode = secretMode;
            (Color ringCol, Color coreCol, string title) = secretMode switch
            {
                "pizzeria" => (Rgb(215, 145, 45), Rgb(145, 55, 225), "[E] АНОМАЛЬНЫЙ ПОРТАЛ: ПИЦЦЕРИЯ ФАЗБЕРА"),
                "black_mesa" => (Rgb(255, 145, 35), Rgb(85, 225, 255), "[E] ПРОСТРАНСТВЕННЫЙ РАЗЛОМ: ЧЁРНАЯ МЕЗА"),
                "qliphoth" => (Rgb(225, 35, 55), Rgb(95, 205, 255), "[E] БАГРОВЫЙ РАЗЛОМ: ДРЕВО КЛИПОТ"),
                "hell_cybergrind" => (Rgb(255, 42, 48), Rgb(255, 215, 55), "[E] КРОВАВЫЙ ТЕРМИНАЛ: КИБЕР-ГРАЙНД"),
                "tartarus" => (Rgb(55, 240, 125), Rgb(22, 95, 58), "[E] ВРАТА ТЁМНОГО ЧАСА: БАШНЯ ТАРТАР"),
                "hakurei_shrine" => (Rgb(235, 48, 58), Rgb(255, 225, 165), "[E] БАРЬЕР ГЕНСОКЁ: ХРАМ ХАКУРЕЙ"),
                "tracen_derby" => (Rgb(85, 225, 115), Rgb(255, 210, 55), "[E] ТУРНИРНЫЙ ШЛЮЗ: ИППОДРОМ ТРЕЙСЕН"),
                _ => (Color.Gold, Color.SkyBlue, "[E] Таинственный Портал")
            };

            new Entity(parent: this, model: "cube", scale: new(3.8f, 0.36f, 2.0f), position: new(0, 0.18f, 0), color: Rgb(42, 46, 56));
            foreach (float sx in new[] { -1.85f, 1.85f })
                new Entity(parent: this, model: "cube", scale: new(0.36f, 4.4f, 0.42f), position: new(sx, 2.2f, 0), color: Rgb(56, 62, 76));
            new Entity(parent: this, model: "cube", scale: new(4.3f, 0.38f, 0.52f), position: new(0, 4.45f, 0), color: ringCol, unlit: true);

            _outerRing = new Entity(parent: this, model: "diamond", scale: new(3.6f, 4.6f, 0.35f), position: new(0, 2.55f, 0), color: ringCol, unlit: true);
            _innerCore = new Entity(parent: this, model: "sphere", scale: new(2.7f, 3.7f, 0.25f), position: new(0, 2.55f, 0), color: coreCol, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 5.55f, 0), color: ringCol)
            {
                BillboardText = title
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (_used || g == null || IsDestroyed) return;
            _outerRing.RotationZ += 85f * dt;
            _innerCore.RotationY += 65f * dt;

            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 14.5f;
            if (dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _used = true;
                SoundManager.Play("portal_open", 1.0f, 0.92f);
                g.EnterSecretStage(SecretMode);
            }
        }
    }

    // =========================================================================================
    // ИНТЕРАКТИВНЫЕ ОБЪЕКТЫ И ЭКИПАЖ КРЕЙСЕРА UES «SAFE TRAVELS» (ХАБ ПОСЛЕ ПОБЕДЫ НАД МИТРИКСОМ)
    // =========================================================================================

    public class SafeTravelsNpc : Entity
    {
        public static readonly List<SafeTravelsNpc> ActiveHubNpcs = new();

        public readonly string PersonaKey;
        public readonly string CharKey;
        public string BaseCharKey => CharKey;
        public readonly int SkinIdx;
        public readonly string DisplayName;
        public readonly string ZoneTitle;
        public readonly SurvivorParts Parts;
        public readonly bool SeatedOnChair;
        public float BaseYaw;
        public bool InConversation = false;

        public static bool IsPlayerNearAnyHubNpc(Vector3 playerPos, float maxDist = 3.9f)
        {
            for (int i = 0; i < ActiveHubNpcs.Count; i++)
            {
                var n = ActiveHubNpcs[i];
                if (n != null && !n.IsDestroyed && n.Enabled && Vector3.Distance(n.Position, playerPos) <= maxDist)
                    return true;
            }
            return false;
        }

        private readonly Entity? _chairEnt;
        private readonly Entity _pedestalRing;
        private readonly Entity _labelEnt;
        private readonly Color _visorCol;
        private readonly string _ambientEmote;
        private float _ambientCycleOffset;
        private bool _usingAmbientEmote = false;
        private bool _talkedThisVisit = false;

        public SafeTravelsNpc(Vector3 pos, string personaKey, float facingYaw = 180f, string zoneTitle = "", string ambientEmote = "")
            : base(position: pos)
        {
            PersonaKey = personaKey;
            (CharKey, SkinIdx) = SafeTravelsDialogues.SplitPersonaKey(personaKey);
            DisplayName = SafeTravelsDialogues.GetPersonaDisplayName(personaKey);
            ZoneTitle = zoneTitle;
            BaseYaw = facingYaw;
            RotationY = facingYaw;
            SeatedOnChair = personaKey == "dmc_vergil";
            _ambientEmote = ambientEmote;
            _ambientCycleOffset = (MathF.Abs(personaKey.GetHashCode() % 1000) / 1000f) * 24.0f;

            _visorCol = SurvivorDatabase.Classes.TryGetValue(CharKey, out var cfg) ? cfg.VisorColor : Color.SkyBlue;
            if (SkinIdx > 0 && cfg != null && SkinIdx < cfg.Skins.Count)
                _visorCol = cfg.Skins[SkinIdx].AccentColor;

            Parts = SurvivorDatabase.BuildSurvivorMesh(this, CharKey, SkinIdx);
            Parts.IdleTime = (MathF.Abs(personaKey.GetHashCode() % 580) / 100f);

            if (SeatedOnChair)
            {
                _chairEnt = Entities.RemotePlayer.BuildPlasticChairEntity(this);
                Parts.SetEmote("chair");
            }

            _pedestalRing = new Entity(parent: this, model: "diamond", scale: new(1.25f, 0.05f, 1.25f),
                position: new(0, 0.03f, 0), color: Rgb(_visorCol.R, _visorCol.G, _visorCol.B, 95), unlit: true);

            _labelEnt = new Entity(parent: this, position: new(0, 2.75f, 0), color: Color.White)
            {
                BillboardText = $"[E] Поговорить: {DisplayName}\n[K / R] Сыграть в Камень-Ножницы-Бумага"
            };

            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 0.52f, 2.1f, pos.Y));
            ActiveHubNpcs.RemoveAll(n => n == null || n.IsDestroyed);
            ActiveHubNpcs.Add(this);
        }

        public void SetNpcEmote(string emote)
        {
            _usingAmbientEmote = false;
            if (SeatedOnChair && string.IsNullOrEmpty(emote))
                Parts.SetEmote("chair");
            else
                Parts.SetEmote(emote ?? "");

            if (_chairEnt != null)
                _chairEnt.Enabled = SeatedOnChair && Parts.ActiveEmote == "chair";
        }

        private bool IsClosestHubNpcTo(Vector3 playerPos)
        {
            float myDist = Vector3.Distance(Position, playerPos);
            for (int i = 0; i < ActiveHubNpcs.Count; i++)
            {
                var other = ActiveHubNpcs[i];
                if (other != null && other != this && !other.IsDestroyed && other.Enabled)
                {
                    if (Vector3.Distance(other.Position, playerPos) < myDist - 0.02f)
                        return false;
                }
            }
            return true;
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            float t = (float)Raylib.GetTime();
            _pedestalRing.RotationY += 32f * dt;

            float dist = Vector3.Distance(Position, g.PlayerPos);
            bool inRpsEmote = !string.IsNullOrEmpty(Parts.ActiveEmote) && Parts.ActiveEmote.StartsWith("rps", StringComparison.Ordinal);

            if (SeatedOnChair)
            {
                if (!inRpsEmote && Parts.ActiveEmote != "chair")
                    Parts.SetEmote("chair");
                if (_chairEnt != null)
                    _chairEnt.Enabled = Parts.ActiveEmote == "chair";

                if (inRpsEmote && dist < 8.0f)
                {
                    float lookYaw = MathF.Atan2(g.PlayerPos.X - X, g.PlayerPos.Z - Z) * (180f / MathF.PI);
                    RotationY = LerpAngleDeg(RotationY, lookYaw, MathF.Min(1f, dt * 6.5f));
                }
                else
                {
                    RotationY = LerpAngleDeg(RotationY, BaseYaw, MathF.Min(1f, dt * 6.0f));
                }
            }
            else
            {
                if (!inRpsEmote && !string.IsNullOrEmpty(_ambientEmote))
                {
                    bool shouldDance = dist > 5.2f && ((t + _ambientCycleOffset) % 26.0f) < 6.2f;
                    if (shouldDance && !_usingAmbientEmote)
                    {
                        _usingAmbientEmote = true;
                        Parts.SetEmote(_ambientEmote);
                    }
                    else if (!shouldDance && _usingAmbientEmote)
                    {
                        _usingAmbientEmote = false;
                        Parts.SetEmote("");
                    }
                }

                if (dist < 7.2f || InConversation)
                {
                    float lookYaw = MathF.Atan2(g.PlayerPos.X - X, g.PlayerPos.Z - Z) * (180f / MathF.PI);
                    RotationY = LerpAngleDeg(RotationY, lookYaw, MathF.Min(1f, dt * 6.5f));
                }
                else
                {
                    RotationY = LerpAngleDeg(RotationY, BaseYaw, MathF.Min(1f, dt * 3.5f));
                }
            }

            Parts.UpdateAnimations(CharKey, dt, isMoving: false, isGrounded: true, velY: 0f, moveSpeedRatio: 1.0f, strafeInput: 0f, forwardInput: 0f);

            bool isClosest = dist < 4.2f && IsClosestHubNpcTo(g.PlayerPos);
            _labelEnt.Enabled = dist < 9.2f;
            if (_labelEnt.Enabled)
            {
                bool isMirror = false;
                if (g is GameRunContext run)
                {
                    string playerPersona = SafeTravelsDialogues.GetPersonaKey(run.CharKey, ProgressData.Instance.GetSelectedSkin(run.CharKey));
                    isMirror = playerPersona == PersonaKey;
                }

                bool talkedBefore = _talkedThisVisit || ProgressData.Instance.UnlockedAchievements.Contains($"talk_{PersonaKey}");
                _labelEnt.Color = isClosest ? Color.Gold : Rgb(220, 238, 255);
                string mirrorTag = isMirror ? " [ЗЕРКАЛЬНЫЙ ДВОЙНИК]" : "";
                string zonePrefix = !string.IsNullOrEmpty(ZoneTitle) && dist >= 4.2f ? $"{DisplayName}\n<{ZoneTitle}>"
                    : $"[E] Поговорить: {DisplayName}{mirrorTag}{(talkedBefore ? " ✓" : "")}\n[K / R] Сыграть в Камень-Ножницы-Бумага";
                _labelEnt.BillboardText = zonePrefix;
            }

            if (dist < 3.85f && isClosest && g.CanPlayerInteract)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.E))
                {
                    _talkedThisVisit = true;
                    SoundManager.Play("dialogue_blip", 0.95f, 1.0f);
                    g.OpenNpcDialogue(CharKey, SkinIdx, this);
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.K) || Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    SoundManager.Play("rps_tick", 0.95f, 1.0f);
                    g.StartNpcRps(CharKey, SkinIdx, this);
                }
            }
        }
    }

    public class SafeTravelsRoomProp : Entity
    {
        public readonly string PropTitle;
        public readonly string LoreText;
        public readonly Color AccentColor;
        public readonly string SoundKey;
        private readonly Entity? _holoSpin;
        private readonly Entity _labelEnt;
        private float _cooldown = 0f;

        public SafeTravelsRoomProp(
            Vector3 pos, string propTitle, string loreText, Color accentColor,
            string soundKey = "ui_select_char", bool spawnHoloMarker = true, float labelHeight = 2.6f)
            : base(position: pos)
        {
            PropTitle = propTitle;
            LoreText = loreText;
            AccentColor = accentColor;
            SoundKey = soundKey;

            if (spawnHoloMarker)
            {
                _holoSpin = new Entity(parent: this, model: "diamond", scale: new(0.38f, 0.56f, 0.38f),
                    position: new(0, labelHeight - 0.65f, 0), color: accentColor, unlit: true);
            }

            _labelEnt = new Entity(parent: this, position: new(0, labelHeight, 0), color: accentColor)
            {
                BillboardText = $"[E] Осмотреть: {propTitle}"
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            _cooldown = MathF.Max(0f, _cooldown - dt);
            float t = (float)Raylib.GetTime();
            if (_holoSpin != null)
            {
                _holoSpin.RotationY += 95f * dt;
                _holoSpin.Y = (_labelEnt.Y - 0.65f) + MathF.Sin(t * 3.1f + X) * 0.10f;
            }

            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 7.8f;

            if (dist < 3.8f && _cooldown <= 0f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _cooldown = 0.65f;
                SoundManager.Play(SoundKey, 0.90f, 1.0f);
                new VisualFX(Position + new Vector3(0, 1.3f, 0), AccentColor, 3.2f, 0.28f);
                g.ShowBanner($"{PropTitle.ToUpper()}: {LoreText}", AccentColor, 4.8f);
            }
        }
    }

    public class SafeTravelsDpsConsole : Entity
    {
        private readonly Entity _holoOrb;
        private readonly Entity _labelEnt;
        private readonly Dictionary<int, float> _prevTotals = new();
        private float _latestHit = 0f;

        public SafeTravelsDpsConsole(Vector3 pos, float facingYaw = 270f) : base(position: pos)
        {
            RotationY = facingYaw;

            // Стойка тактической телеметрии огневого рубежа UES
            new Entity(parent: this, model: "cube", scale: new(2.4f, 0.28f, 1.2f), position: new(0, 0.14f, 0), color: Rgb(36, 44, 58));
            new Entity(parent: this, model: "cube", scale: new(0.45f, 1.45f, 0.45f), position: new(0, 0.85f, 0), color: Rgb(52, 64, 84));
            new Entity(parent: this, model: "cube", scale: new(2.3f, 1.15f, 0.18f), position: new(0, 1.75f, 0), rotation: new(-16f, 0, 0), color: Rgb(22, 32, 48));
            new Entity(parent: this, model: "cube", scale: new(2.1f, 0.95f, 0.20f), position: new(0, 1.75f, 0.02f), rotation: new(-16f, 0, 0), color: Rgb(45, 195, 245, 195), unlit: true);
            _holoOrb = new Entity(parent: this, model: "diamond", scale: new(0.55f, 0.75f, 0.55f), position: new(0, 2.65f, 0), color: Color.Gold, unlit: true);

            _labelEnt = new Entity(parent: this, position: new(0, 3.55f, 0), color: Rgb(95, 235, 255))
            {
                BillboardText = "ТЕЛЕМЕТРИЯ ОГНЕВОГО РУБЕЖА UES\nУРОН В СЕКУНДУ (DPS): 0 | ПОСЛЕДНИЙ УДАР: 0\nМАКС. КРИТ: 0 | ВСЕГО НАНЕСЕНО: 0\n[E] Сбросить счётчик урона и манекены"
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 0.95f, 2.3f, pos.Y));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            _holoOrb.RotationY += 110f * dt;

            float totalDps = 0f;
            float maxHit = 0f;
            float sessionTotal = 0f;

            var enemies = Entities.CombatManager.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var d = enemies[i];
                if (!IsValid(d) || !d.IsTrainingDummy) continue;

                totalDps += d.GetCurrentDps();
                if (d.MaxHitDmg > maxHit) maxHit = d.MaxHitDmg;
                sessionTotal += d.SessionTotalDmg;

                float prev = _prevTotals.TryGetValue(d.NetId, out float pt) ? pt : 0f;
                if (d.SessionTotalDmg > prev + 0.01f)
                {
                    _latestHit = d.LastHitDmg;
                    _prevTotals[d.NetId] = d.SessionTotalDmg;
                }
                else if (d.SessionTotalDmg < prev)
                {
                    _prevTotals[d.NetId] = d.SessionTotalDmg;
                }
            }

            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 26.0f;
            _labelEnt.Color = totalDps > 250f ? Color.Gold : Rgb(95, 235, 255);
            _labelEnt.BillboardText =
                $"══ ТЕЛЕМЕТРИЯ ПОЛИГОНА UES ══\n" +
                $"УРОН В СЕКУНДУ (DPS): {totalDps:F0}  |  ПОСЛЕДНИЙ УДАР: {_latestHit:F0}\n" +
                $"МАКС. КРИТ / УДАР: {maxHit:F0}  |  ВСЕГО НАНЕСЕНО: {sessionTotal:F0}\n" +
                $"[E] Сбросить замер урона и обновить манекены";

            if (dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _latestHit = 0f;
                _prevTotals.Clear();
                for (int i = 0; i < enemies.Count; i++)
                {
                    var d = enemies[i];
                    if (IsValid(d) && d.IsTrainingDummy)
                        d.ResetDummyStats();
                }
                SoundManager.Play("ui_ready", 1.0f, 1.15f);
                new VisualFX(Position + new Vector3(0, 2.2f, 0), Color.SkyBlue, 4.2f, 0.30f);
                g.ShowBanner("ТЕЛЕМЕТРИЯ ПОЛИГОНА ОБНУЛЕНА! ВСЕ ТРЕНИРОВОЧНЫЕ МАНЕКЕНЫ ОБНОВЛЕНЫ.", Color.SkyBlue, 2.8f);
            }
        }
    }

    public class SafeTravelsSandboxStation : Entity
    {
        public readonly string StationMode;
        private readonly string _tierArg;
        private readonly Color _accentCol;
        private readonly Entity _spinCore;
        private readonly Entity _labelEnt;
        private Entity? _podPreviewRoot;
        private SurvivorParts? _podPreviewParts;
        private string _podPreviewCharKey = "commando";
        private int _podPreviewIdx = 0;
        private readonly List<string> _availablePersonas = new();

        public SafeTravelsSandboxStation(Vector3 pos, string stationMode, float facingYaw = 180f) : base(position: pos)
        {
            StationMode = stationMode;
            RotationY = facingYaw;

            (_tierArg, _accentCol, string baseTitle) = stationMode switch
            {
                "item_white" => ("white", Rgb(235, 240, 248), "[E] Терминал Снаряжения: БЕЛЫЕ ПРЕДМЕТЫ (Бесплатно)"),
                "item_green" => ("green", Rgb(85, 235, 105), "[E] Терминал Снаряжения: ЗЕЛЁНЫЕ ПРЕДМЕТЫ (Бесплатно)"),
                "item_red" => ("red", Rgb(245, 65, 65), "[E] Терминал Снаряжения: ЛЕГЕНДАРНЫЕ ПРЕДМЕТЫ (Бесплатно)"),
                "item_void" => ("void", Rgb(195, 75, 255), "[E] Терминал Снаряжения: ПРЕДМЕТЫ БЕЗДНЫ (Бесплатно)"),
                "item_lunar" => ("lunar", Rgb(85, 195, 255), "[E] Терминал Снаряжения: ЛУННЫЕ РЕЛИКВИИ (Бесплатно)"),
                "clear_items" => ("", Rgb(255, 145, 45), "[E] Деконструктор UES: ОЧИСТИТЬ ТЕСТОВЫЙ ИНВЕНТАРЬ"),
                "recharge" => ("", Rgb(75, 255, 185), "[E] Станция Перезарядки: СБРОС КД + 100% HP/ЩИТ/ЗАРЯДЫ"),
                "zero_cd" => ("", Rgb(145, 255, 95), "[E] Режим Песочницы: 0 СЕК КД НАВЫКОВ (ВКЛ / ВЫКЛ)"),
                "char_pod" => ("", Rgb(255, 210, 65), "[E] Капсула Смены Героя на Борту"),
                _ => ("white", Color.SkyBlue, "[E] Терминал Полигона")
            };

            if (stationMode == "char_pod")
            {
                // Крио-капсула смены выжившего с 3D-голограммой внутри
                new Entity(parent: this, model: "cube", scale: new(2.6f, 0.32f, 2.6f), position: new(0, 0.16f, 0), color: Rgb(38, 46, 62));
                new Entity(parent: this, model: "diamond", scale: new(2.2f, 0.16f, 2.2f), position: new(0, 0.34f, 0), color: _accentCol, unlit: true);
                foreach (float sx in new[] { -1.1f, 1.1f })
                    new Entity(parent: this, model: "cube", scale: new(0.24f, 3.4f, 0.32f), position: new(sx, 1.85f, -0.55f), color: Rgb(56, 68, 88));
                new Entity(parent: this, model: "cube", scale: new(2.5f, 0.30f, 1.6f), position: new(0, 3.60f, -0.15f), color: Rgb(46, 56, 74));
                new Entity(parent: this, model: "diamond", scale: new(2.0f, 0.18f, 1.2f), position: new(0, 3.42f, -0.10f), color: _accentCol, unlit: true);
                _spinCore = new Entity(parent: this, model: "diamond", scale: new(1.65f, 0.10f, 1.65f), position: new(0, 0.42f, 0), color: Color.SkyBlue, unlit: true);

                RefreshAvailablePersonas();
                RebuildPodHologram();
            }
            else if (stationMode == "recharge")
            {
                new Entity(parent: this, model: "cube", scale: new(2.4f, 0.26f, 2.4f), position: new(0, 0.13f, 0), color: Rgb(36, 52, 54));
                new Entity(parent: this, model: "sphere", scale: new(2.0f, 0.14f, 2.0f), position: new(0, 0.28f, 0), color: _accentCol, unlit: true);
                new Entity(parent: this, model: "cube", scale: new(0.95f, 0.28f, 0.28f), position: new(0, 1.55f, 0), color: _accentCol, unlit: true);
                new Entity(parent: this, model: "cube", scale: new(0.28f, 0.95f, 0.28f), position: new(0, 1.55f, 0), color: _accentCol, unlit: true);
                _spinCore = new Entity(parent: this, model: "diamond", scale: new(1.35f, 0.22f, 1.35f), position: new(0, 1.55f, 0), color: Color.White, unlit: true);
            }
            else
            {
                // Стойка выдачи предметов / очистки инвентаря
                new Entity(parent: this, model: "cube", scale: new(1.65f, 0.26f, 1.45f), position: new(0, 0.13f, 0), color: Rgb(38, 44, 58));
                new Entity(parent: this, model: "cube", scale: new(1.25f, 1.15f, 1.05f), position: new(0, 0.78f, 0), color: Rgb(48, 56, 74));
                new Entity(parent: this, model: "cube", scale: new(1.05f, 0.14f, 0.85f), position: new(0, 1.38f, 0), color: _accentCol, unlit: true);
                _spinCore = new Entity(parent: this, model: stationMode == "clear_items" ? "sphere" : "diamond",
                    scale: new(0.68f, 0.92f, 0.68f), position: new(0, 2.05f, 0), color: _accentCol, unlit: true);
            }

            _labelEnt = new Entity(parent: this, position: new(0, stationMode == "char_pod" ? 4.15f : 3.05f, 0), color: _accentCol)
            {
                BillboardText = baseTitle
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, stationMode == "char_pod" ? 1.15f : 0.82f, 2.4f, pos.Y));
        }

        private void RefreshAvailablePersonas()
        {
            _availablePersonas.Clear();
            var prog = ProgressData.Instance;
            foreach (var pk in SafeTravelsDialogues.AllPersonaKeys)
            {
                var (baseKey, skinIdx) = SafeTravelsDialogues.SplitPersonaKey(pk);
                if (!prog.IsCharacterUnlocked(baseKey)) continue;
                if (skinIdx > 0 && SurvivorDatabase.Classes.TryGetValue(baseKey, out var cfg) && skinIdx < cfg.Skins.Count)
                {
                    if (!prog.IsSkinUnlocked(baseKey, skinIdx, cfg.Skins[skinIdx].UnlockAchKey))
                        continue;
                }
                _availablePersonas.Add(pk);
            }
            if (_availablePersonas.Count == 0)
                _availablePersonas.Add("commando");
            _podPreviewIdx = Math.Clamp(_podPreviewIdx, 0, _availablePersonas.Count - 1);
        }

        private void RebuildPodHologram()
        {
            if (_podPreviewRoot != null && !_podPreviewRoot.IsDestroyed)
                Destroy(_podPreviewRoot);

            string pk = _availablePersonas[Math.Clamp(_podPreviewIdx, 0, _availablePersonas.Count - 1)];
            var (baseKey, skinIdx) = SafeTravelsDialogues.SplitPersonaKey(pk);
            _podPreviewCharKey = baseKey;
            _podPreviewRoot = new Entity(parent: this, position: new(0, 0.48f, 0), scale: new(0.72f, 0.72f, 0.72f));
            _podPreviewParts = SurvivorDatabase.BuildSurvivorMesh(_podPreviewRoot, baseKey, skinIdx);
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            float t = (float)Raylib.GetTime();
            _spinCore.RotationY += 95f * dt;
            if (StationMode != "char_pod" && StationMode != "recharge")
                _spinCore.Y = 2.05f + MathF.Sin(t * 3.2f + X) * 0.12f;

            if (StationMode == "char_pod" && _podPreviewRoot != null && _podPreviewParts != null)
            {
                _podPreviewRoot.RotationY += 42f * dt;
                _podPreviewParts.UpdateAnimations(_podPreviewCharKey, dt, false, true, 0f, 0f, 0f, 1f);
            }

            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 9.5f;

            if (StationMode == "char_pod" && _labelEnt.Enabled)
            {
                if (_availablePersonas.Count == 0) RefreshAvailablePersonas();
                string pk = _availablePersonas[Math.Clamp(_podPreviewIdx, 0, _availablePersonas.Count - 1)];
                string disp = SafeTravelsDialogues.GetPersonaDisplayName(pk);
                _labelEnt.BillboardText =
                    $"КАПСУЛА СМЕНЫ ГЕРОЯ [{_podPreviewIdx + 1}/{_availablePersonas.Count}]: {disp}\n" +
                    $"[K] Листать голограмму  |  [E] Перевоплотиться  |  [H] Весь список";
            }
            else if (StationMode == "zero_cd" && _labelEnt.Enabled)
            {
                bool active = g is GameRunContext rCtx && rCtx.ZeroCooldownSandbox;
                _labelEnt.Color = active ? Color.Lime : _accentCol;
                _labelEnt.BillboardText = active
                    ? "[E] ПЕСОЧНИЦА КД: АКТИВНА (0 СЕК НА ВСЕ НАВЫКИ)"
                    : "[E] ПЕСОЧНИЦА КД: СТАНДАРТНАЯ (НАЖМИТЕ ДЛЯ 0 СЕК КД)";
            }

            if (dist < 3.65f && g.CanPlayerInteract)
            {
                if (StationMode == "char_pod" && Raylib.IsKeyPressed(KeyboardKey.K))
                {
                    RefreshAvailablePersonas();
                    _podPreviewIdx = (_podPreviewIdx + 1) % _availablePersonas.Count;
                    RebuildPodHologram();
                    SoundManager.Play("ui_select_char", 0.85f, 1.05f);
                }

                if (Raylib.IsKeyPressed(KeyboardKey.E))
                {
                    if (StationMode.StartsWith("item_", StringComparison.Ordinal))
                    {
                        SoundManager.Play("chest_open", 0.95f, 1.05f);
                        new VisualFX(Position + new Vector3(0, 2.0f, 0), _accentCol, 3.2f, 0.28f);
                        g.OpenCommandPicker(_tierArg);
                    }
                    else if (StationMode == "clear_items")
                    {
                        SoundManager.Play("shrine_use", 0.95f, 0.92f);
                        new VisualFX(Position + new Vector3(0, 2.0f, 0), _accentCol, 4.2f, 0.32f);
                        g.ClearSafeTravelsTestItems();
                    }
                    else if (StationMode == "zero_cd")
                    {
                        new VisualFX(Position + new Vector3(0, 2.0f, 0), Color.Lime, 3.8f, 0.30f);
                        g.ToggleSafeTravelsZeroCooldowns();
                    }
                    else if (StationMode == "recharge")
                    {
                        g.HealPlayer(g.PlayerMaxHp * 2.0f);
                        g.PlayerHp = g.PlayerMaxHp;
                        if (g is GameRunContext runCtx)
                            runCtx.ShiftCd = 0f;
                        var ctrl = Entities.CombatManager.Controller;
                        if (ctrl != null)
                        {
                            ctrl.M1Cd = 0f;
                            ctrl.M2Cd = 0f;
                            ctrl.ShiftCd = 0f;
                            ctrl.RCd = 0f;
                            ctrl.V1ArmCd = 0f;
                            ctrl.V1WhiplashCd = 0f;
                            ctrl.SheenCd = 0f;
                            ctrl.ZhonyasCooldownTimer = 0f;
                            ctrl.SteveTotemCd = 0f;
                            ctrl.OpalTimer = 0f;
                            ctrl.SaferSpacesTimer = 0f;
                            ctrl.Shield = MathF.Max(ctrl.Shield, g.PlayerMaxHp * 0.50f);
                            ctrl.GoldShipMomentum = 1.0f;
                            ctrl.SanabiMomentum = 1.0f;
                            ctrl.V1Coins = 4.0f;
                            ctrl.V1RailCharge = 100.0f;
                            ctrl.V1Stamina = 3.0f;
                            ctrl.V1StylePoints = MathF.Max(ctrl.V1StylePoints, 1000.0f);
                            ctrl.ReimuFlightFuel = 4.5f;
                            ctrl.ReimuFlightBroken = false;
                            ctrl.AhriDashCharges = 3;
                            ctrl.SlarkEssenceStacks = Math.Max(ctrl.SlarkEssenceStacks, 20);
                            ctrl.NeroExceedCharges = 3;
                            ctrl.LudensCharges = 100.0f;
                            ctrl.SheenProcReady = true;
                            ctrl.MindustryCopper = MathF.Max(ctrl.MindustryCopper, 999f);
                            ctrl.MindustrySilicon = MathF.Max(ctrl.MindustrySilicon, 999f);
                            ctrl.MindustryThorium = MathF.Max(ctrl.MindustryThorium, 999f);
                            ctrl.SteveWood = Math.Max(ctrl.SteveWood, 99);
                            ctrl.SteveStone = Math.Max(ctrl.SteveStone, 99);
                            ctrl.SteveIron = Math.Max(ctrl.SteveIron, 99);
                            ctrl.SteveDiamond = Math.Max(ctrl.SteveDiamond, 99);
                        }
                        SoundManager.Play("heal", 1.0f, 1.1f);
                        new VisualFX(g.PlayerPos + new Vector3(0, 1.2f, 0), _accentCol, 4.5f, 0.35f);
                        g.ShowBanner("ПОЛНАЯ ПЕРЕЗАРЯДКА: ВСЕ НАВЫКИ, 100% ЗДОРОВЬЯ, ИНЕРЦИЯ И БОЕВЫЕ РЕСУРСЫ ГОТОВЫ!", _accentCol, 3.2f);
                    }
                    else if (StationMode == "char_pod")
                    {
                        RefreshAvailablePersonas();
                        string curPlayerPk = "";
                        if (g is GameRunContext run)
                            curPlayerPk = SafeTravelsDialogues.GetPersonaKey(run.CharKey, ProgressData.Instance.GetSelectedSkin(run.CharKey));

                        string targetPk = _availablePersonas[Math.Clamp(_podPreviewIdx, 0, _availablePersonas.Count - 1)];
                        if (targetPk == curPlayerPk && _availablePersonas.Count > 1)
                        {
                            _podPreviewIdx = (_podPreviewIdx + 1) % _availablePersonas.Count;
                            targetPk = _availablePersonas[_podPreviewIdx];
                            RebuildPodHologram();
                        }

                        var (newChar, newSkin) = SafeTravelsDialogues.SplitPersonaKey(targetPk);
                        SoundManager.Play("portal_open", 0.95f, 1.15f);
                        new VisualFX(Position + new Vector3(0, 1.6f, 0), Color.Gold, 4.6f, 0.35f);
                        g.SwitchSafeTravelsCharacter(newChar, newSkin);
                    }
                }
            }
        }
    }

    public class SafeTravelsDropPod : Entity
    {
        private readonly Entity _beaconRing;
        private readonly Entity _labelEnt;
        private bool _triggered = false;

        public SafeTravelsDropPod(Vector3 pos) : base(position: pos)
        {
            // Полноразмерная десантная капсула UES в Южном Ангаре
            new Entity(parent: this, model: "cube", scale: new(5.4f, 0.32f, 5.4f), position: new(0, 0.16f, 0), color: Rgb(36, 42, 54));
            new Entity(parent: this, model: "diamond", scale: new(4.8f, 0.18f, 4.8f), position: new(0, 0.34f, 0), color: Rgb(245, 165, 35), unlit: true);

            var body = new Entity(parent: this, position: new(0, 2.5f, 0));
            new Entity(parent: body, model: "cube", scale: new(2.8f, 4.2f, 2.8f), color: Rgb(210, 220, 236));
            new Entity(parent: body, model: "diamond", scale: new(3.1f, 1.8f, 3.1f), position: new(0, 2.5f, 0), color: Rgb(235, 130, 35));
            new Entity(parent: body, model: "cube", scale: new(1.65f, 2.35f, 0.25f), position: new(0, -0.35f, 1.42f), color: Rgb(42, 54, 72));
            new Entity(parent: body, model: "cube", scale: new(1.25f, 0.85f, 0.28f), position: new(0, 0.65f, 1.44f), color: Color.SkyBlue, unlit: true);
            foreach (float sx in new[] { -1.55f, 1.55f })
                new Entity(parent: body, model: "cube", scale: new(0.38f, 3.4f, 0.85f), position: new(sx, -0.4f, 0), color: Rgb(62, 74, 94));

            _beaconRing = new Entity(parent: this, model: "diamond", scale: new(4.2f, 0.22f, 4.2f), position: new(0, 5.8f, 0), color: Color.Lime, unlit: true);
            _labelEnt = new Entity(parent: this, position: new(0, 6.75f, 0), color: Color.Lime)
            {
                BillboardText = "[E] ДЕСАНТНАЯ КАПСУЛА UES\nНачать орбитальную высадку в новый забег!"
            };

            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.85f, 5.2f, pos.Y));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (_triggered || g == null || IsDestroyed) return;

            _beaconRing.RotationY += 95f * dt;
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 16.0f;

            if (dist < 4.8f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _triggered = true;
                SoundManager.Play("run_start", 1.0f, 1.0f);
                new VisualFX(Position + new Vector3(0, 2.2f, 0), Color.Lime, 6.5f, 0.40f);
                g.StartRunFromSafeTravels();
            }
        }
    }

    public class SafeTravelsExitAirlock : Entity
    {
        private readonly Entity _statusLight;
        private readonly Entity _labelEnt;
        private bool _triggered = false;

        public SafeTravelsExitAirlock(Vector3 pos, float facingYaw = 0f) : base(position: pos)
        {
            RotationY = facingYaw;
            new Entity(parent: this, model: "cube", scale: new(3.6f, 4.2f, 0.65f), position: new(0, 2.1f, 0), color: Rgb(44, 52, 68));
            new Entity(parent: this, model: "cube", scale: new(2.6f, 3.4f, 0.72f), position: new(0, 1.7f, 0), color: Rgb(26, 32, 44));
            foreach (float sx in new[] { -1.45f, 1.45f })
                new Entity(parent: this, model: "cube", scale: new(0.22f, 3.6f, 0.76f), position: new(sx, 1.8f, 0), color: Rgb(235, 165, 40), unlit: true);
            _statusLight = new Entity(parent: this, model: "sphere", scale: new(0.75f, 0.35f, 0.35f), position: new(0, 3.85f, 0.35f), color: Color.SkyBlue, unlit: true);

            _labelEnt = new Entity(parent: this, position: new(0, 4.75f, 0), color: Color.SkyBlue)
            {
                BillboardText = "[E] ГЕРМОШЛЮЗ КОРАБЛЯ\nВернуться в Главное Меню"
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.45f, 4.0f, pos.Y));
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (_triggered || g == null || IsDestroyed) return;

            float pulse = 0.65f + MathF.Abs(MathF.Sin((float)Raylib.GetTime() * 3.0f)) * 0.35f;
            _statusLight.Scale = new Vector3(0.75f * pulse, 0.35f, 0.35f);

            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 13.0f;

            if (dist < 4.2f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                _triggered = true;
                SoundManager.Play("ui_click", 1.0f, 0.95f);
                g.ExitSafeTravelsToMenu();
            }
        }
    }

    public enum QuestNodeMode
    {
        Touch,
        AerialRing,
        RelayButton,
        FleeingFox,
        BombDefuse,
        RaceGate,
        DestructibleBlock,
        OrbitLock,
        MirageOrb,
        PushCart,
        AstralFirefly,
        MistThread,
        GoldenOrange
    }

    public class QuestSecondaryNode : Entity
    {
        public readonly CharacterQuestMonument ParentQuest;
        public readonly int NodeIndex;
        public readonly QuestNodeMode Mode;
        public bool Collected = false;
        public bool IsTrueOrb = false;
        public float Hp = 350f;
        public float MaxHp = 350f;
        private int _foxCatches = 0;
        private float _steerSide = 1f;
        private readonly string _baseLabel;
        private readonly Entity _orb;
        private readonly Entity _labelEnt;

        public QuestSecondaryNode(
            Vector3 pos, CharacterQuestMonument parentQuest, int nodeIndex,
            string label, Color col, QuestNodeMode mode = QuestNodeMode.Touch, bool isTrueOrb = false)
            : base(position: pos)
        {
            ParentQuest = parentQuest;
            NodeIndex = nodeIndex;
            Mode = mode;
            IsTrueOrb = isTrueOrb;
            _baseLabel = label;

            if (mode == QuestNodeMode.AerialRing)
            {
                _orb = new Entity(parent: this, model: "diamond", scale: new(3.2f, 3.2f, 0.45f), color: col, unlit: true);
                new Entity(parent: _orb, model: "sphere", scaleUniform: 0.45f, color: Color.White, unlit: true);
            }
            else if (mode == QuestNodeMode.RelayButton)
            {
                new Entity(parent: this, model: "cube", scale: new(1.2f, 1.8f, 1.2f), position: new(0, 0.9f, 0), color: Rgb(45, 52, 65));
                _orb = new Entity(parent: this, model: "sphere", scaleUniform: 1.05f, position: new(0, 2.2f, 0), color: col, unlit: true);
            }
            else if (mode == QuestNodeMode.FleeingFox)
            {
                _orb = new Entity(parent: this, position: new(0, 0.9f, 0));
                new Entity(parent: _orb, model: "sphere", scale: new(0.85f, 0.75f, 1.25f), color: Rgb(120, 235, 255), unlit: true);
                new Entity(parent: _orb, model: "diamond", scale: new(0.95f, 0.65f, 1.15f), position: new(0, 0.25f, -0.75f), color: Rgb(255, 115, 205), unlit: true);
            }
            else if (mode == QuestNodeMode.BombDefuse)
            {
                new Entity(parent: this, model: "cube", scale: new(1.1f, 0.65f, 0.85f), position: new(0, 0.35f, 0), color: Rgb(48, 42, 42));
                _orb = new Entity(parent: this, model: "sphere", scaleUniform: 0.65f, position: new(0, 0.95f, 0), color: Color.Red, unlit: true);
            }
            else if (mode == QuestNodeMode.RaceGate)
            {
                foreach (float sx in new[] { -1.8f, 1.8f })
                    new Entity(parent: this, model: "cube", scale: new(0.35f, 3.6f, 0.35f), position: new(sx, 1.8f, 0), color: Rgb(230, 235, 245));
                _orb = new Entity(parent: this, model: "cube", scale: new(4.0f, 0.5f, 0.5f), position: new(0, 3.5f, 0), color: col, unlit: true);
            }
            else if (mode == QuestNodeMode.DestructibleBlock)
            {
                Hp = MaxHp = 350f;
                _orb = new Entity(parent: this, model: "cube", scale: new(1.6f, 2.6f, 1.6f), position: new(0, 1.3f, 0), color: Rgb(38, 22, 58));
                new Entity(parent: _orb, model: "diamond", scale: new(1.05f, 1.05f, 1.05f), color: Rgb(185, 75, 255), unlit: true);
            }
            else if (mode == QuestNodeMode.OrbitLock)
            {
                Hp = MaxHp = 90f;
                _orb = new Entity(parent: this, model: "diamond", scale: new(1.1f, 1.1f, 1.1f), color: Color.Lime, unlit: true);
                new Entity(parent: _orb, model: "sphere", scaleUniform: 0.6f, color: Color.White, unlit: true);
            }
            else if (mode == QuestNodeMode.PushCart)
            {
                _orb = new Entity(parent: this, position: new(0, 0.8f, 0));
                new Entity(parent: _orb, model: "cube", scale: new(1.5f, 0.9f, 1.9f), color: Rgb(75, 82, 95));
                new Entity(parent: _orb, model: "diamond", scale: new(0.95f, 1.45f, 0.95f), position: new(0, 0.95f, 0), color: Color.Orange, unlit: true);
            }
            else if (mode == QuestNodeMode.AstralFirefly)
            {
                _orb = new Entity(parent: this, model: "sphere", scaleUniform: 0.95f, color: Rgb(255, 75, 115), unlit: true);
                new Entity(parent: _orb, model: "diamond", scale: new(1.75f, 0.35f, 0.55f), color: Color.SkyBlue, unlit: true);
            }
            else if (mode == QuestNodeMode.MistThread)
            {
                new Entity(parent: this, model: "diamond", scale: new(1.3f, 0.18f, 1.3f), position: new(0, 0.10f, 0), color: Rgb(42, 56, 78));
                _orb = new Entity(parent: this, model: "cube", scale: new(0.22f, 2.8f, 0.22f), position: new(0, 1.4f, 0), color: Rgb(95, 230, 255), unlit: true);
                new Entity(parent: _orb, model: "diamond", scale: new(3.6f, 0.25f, 0.55f), position: new(0, 0.1f, 0), rotation: new(0, 0, 28), color: Color.Gold, unlit: true);
            }
            else if (mode == QuestNodeMode.GoldenOrange)
            {
                new Entity(parent: this, model: "cube", scale: new(0.85f, 0.12f, 0.85f), position: new(0, 0.06f, 0), color: Rgb(125, 82, 42));
                _orb = new Entity(parent: this, model: "sphere", scaleUniform: 0.92f, position: new(0, 0.72f, 0), color: Rgb(255, 150, 25), unlit: true);
                new Entity(parent: _orb, model: "diamond", scale: new(0.42f, 0.16f, 0.24f), position: new(0.12f, 0.52f, 0), rotation: new(0, 0, -22), color: Color.Lime, unlit: true);
            }
            else
            {
                new Entity(parent: this, model: "diamond", scale: new(1.4f, 0.25f, 1.4f), position: new(0, 0.12f, 0), color: Rgb(50, 58, 72));
                _orb = new Entity(parent: this, model: "diamond", scale: new(1.05f, 1.65f, 1.05f), position: new(0, 1.5f, 0), color: col, unlit: true);
                new Entity(parent: this, model: "sphere", scaleUniform: 0.65f, position: new(0, 1.5f, 0), color: isTrueOrb ? Color.White : Rgb(185, 95, 255), unlit: true);
            }

            _labelEnt = new Entity(parent: this, position: new(0, mode == QuestNodeMode.RaceGate ? 4.4f : (mode == QuestNodeMode.AstralFirefly ? 1.5f : 3.1f), 0), color: col)
            {
                BillboardText = label
            };
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (Collected || g == null || IsDestroyed || ParentQuest.Completed) return;
            float t = (float)Raylib.GetTime();
            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 28.0f;

            if (Mode == QuestNodeMode.AerialRing)
            {
                _orb.RotationZ += 120f * dt;
                if (Vector3.Distance(Position, g.PlayerPos + new Vector3(0, 1.0f, 0)) <= 3.8f)
                {
                    Collected = true;
                    _labelEnt.Enabled = false;
                    _orb.Enabled = false;
                    SoundManager.Play("bounce_pad", 1.0f, 1.15f);
                    g.PlayerVelY = 15.5f;
                    g.PlayerGrounded = false;
                    g.PlayerJumpsLeft = Math.Max(1, g.PlayerMaxJumps);
                    new VisualFX(Position, Color.SkyBlue, 4.8f, 0.32f);
                    ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                }
            }
            else if (Mode == QuestNodeMode.RelayButton)
            {
                _orb.RotationY += 90f * dt;
                if (dist < 3.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    new VisualFX(Position + new Vector3(0, 2.0f, 0), _labelEnt.Color, 3.5f, 0.25f);
                    ParentQuest.OnRelayPressed(NodeIndex);
                }
            }
            else if (Mode == QuestNodeMode.FleeingFox)
            {
                Vector3 awayFromPlayer = Position - g.PlayerPos;
                awayFromPlayer.Y = 0f;
                Vector3 toHome = ParentQuest.Position - Position;
                toHome.Y = 0f;

                Vector3 moveDir = awayFromPlayer.LengthSquared() > 0.01f ? Vector3.Normalize(awayFromPlayer) : Vector3.UnitZ;
                if (toHome.Length() > 24.0f)
                    moveDir = Vector3.Normalize(moveDir * 0.4f + Vector3.Normalize(toHome) * 0.9f);

                Vector3 steered = Physics.ComputeSmartSteerDirection(Position, moveDir, 0.8f, Y, ref _steerSide);
                float foxSpeed = 8.4f + _foxCatches * 0.95f;
                Vector3 nextP = Position + steered * foxSpeed * dt;
                nextP.X = Math.Clamp(nextP.X, -Physics.WorldBound + 6f, Physics.WorldBound - 6f);
                nextP.Z = Math.Clamp(nextP.Z, -Physics.WorldBound + 6f, Physics.WorldBound - 6f);
                nextP.Y = Physics.GetTerrainSpawnHeight(nextP.X, nextP.Z);
                Position = nextP;
                LookAt2D(Position + steered * 3f);

                if (dist < 3.1f)
                {
                    _foxCatches++;
                    SoundManager.Play("charm_heart", 1.0f, 1.0f + _foxCatches * 0.10f);
                    new VisualFX(Position + new Vector3(0, 1.2f, 0), Rgb(255, 115, 210), 4.5f, 0.32f);
                    ParentQuest.OnSecondaryNodeTouched(_foxCatches);
                    if (_foxCatches >= 4)
                    {
                        Collected = true;
                        Enabled = false;
                    }
                    else
                    {
                        float ang = _foxCatches * 1.95f;
                        Vector3 jumpP = ParentQuest.Position + new Vector3(MathF.Cos(ang) * 16f, 0, MathF.Sin(ang) * 16f);
                        jumpP.X = Math.Clamp(jumpP.X, -Physics.WorldBound + 6f, Physics.WorldBound - 6f);
                        jumpP.Z = Math.Clamp(jumpP.Z, -Physics.WorldBound + 6f, Physics.WorldBound - 6f);
                        jumpP.Y = Physics.GetTerrainSpawnHeight(jumpP.X, jumpP.Z);
                        Position = jumpP;
                        _labelEnt.BillboardText = $"[ДОГОНИТЕ ЛИСЁНКА ({_foxCatches}/4)]";
                    }
                }
            }
            else if (Mode == QuestNodeMode.BombDefuse)
            {
                float pulse = 0.55f + MathF.Abs(MathF.Sin(t * 9.0f)) * 0.35f;
                _orb.Scale = new Vector3(pulse);
                if (dist < 3.6f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    Collected = true;
                    _labelEnt.Enabled = false;
                    _orb.Color = Color.Lime;
                    SoundManager.Play("ui_ready", 1.0f, 1.2f);
                    new VisualFX(Position + new Vector3(0, 1.0f, 0), Color.Lime, 4.0f, 0.3f);
                    ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                }
            }
            else if (Mode == QuestNodeMode.RaceGate)
            {
                bool isNext = ParentQuest.CurrentStep == NodeIndex;
                _orb.Color = isNext ? Color.Gold : Rgb(90, 95, 115);
                _labelEnt.Color = isNext ? Color.Gold : Color.Gray;
                _labelEnt.BillboardText = isNext ? $"▶ {_baseLabel} [АКТИВНЫ] ◀" : _baseLabel;

                if (isNext && Physics.Distance2D(Position, g.PlayerPos) < 4.4f)
                {
                    Collected = true;
                    _labelEnt.Enabled = false;
                    _orb.Color = Color.Lime;
                    SoundManager.Play("ui_ready", 1.0f, 1.05f + NodeIndex * 0.08f);
                    g.ApplyDash(g.PlayerForward * 18.0f, 0.20f);
                    new VisualFX(Position + new Vector3(0, 1.8f, 0), Color.Gold, 5.5f, 0.35f);
                    ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                }
            }
            else if (Mode == QuestNodeMode.DestructibleBlock)
            {
                if (ParentQuest.CheckPlayerAttackHit(Position + new Vector3(0, 1.4f, 0), 2.5f, out float dmg))
                {
                    Hp -= dmg;
                    new VisualFX(Position + new Vector3(0, 1.4f, 0), Rgb(185, 75, 255), 3.0f, 0.22f);
                    _labelEnt.BillboardText = $"Обсидиановый Монолит [{(int)MathF.Max(0, Hp)}/{(int)MaxHp} HP]\n[Разбейте атаками!]";
                    if (Hp <= 0f)
                    {
                        Collected = true;
                        Enabled = false;
                        SoundManager.Play("slam_land", 1.0f, 1.15f);
                        new VisualFX(Position + new Vector3(0, 1.4f, 0), Rgb(195, 85, 255), 6.0f, 0.38f);
                        ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                    }
                }
            }
            else if (Mode == QuestNodeMode.OrbitLock)
            {
                float ang = t * (1.75f + NodeIndex * 0.28f) + NodeIndex * (MathF.PI * 0.5f);
                float r = 5.6f;
                Position = ParentQuest.Position + new Vector3(MathF.Cos(ang) * r, 2.0f + MathF.Sin(t * 3.0f + NodeIndex) * 0.75f, MathF.Sin(ang) * r);
                _orb.RotationY += 180f * dt;

                if (ParentQuest.CheckPlayerAttackHit(Position, 2.15f, out float _))
                {
                    Collected = true;
                    Enabled = false;
                    SoundManager.Play("explosion", 0.85f, 1.25f);
                    new VisualFX(Position, Color.Lime, 4.5f, 0.32f);
                    ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                }
            }
            else if (Mode == QuestNodeMode.AstralFirefly)
            {
                float ang = t * (1.45f + NodeIndex * 0.22f) + NodeIndex * (MathF.PI * 0.5f);
                float r = 8.2f + MathF.Sin(t * 2.1f + NodeIndex) * 2.2f;
                float yOff = 2.4f + MathF.Sin(t * 3.2f + NodeIndex * 1.3f) * 0.95f;
                Position = ParentQuest.Position + new Vector3(MathF.Cos(ang) * r, yOff, MathF.Sin(ang) * r);
                _orb.RotationY += 220f * dt;

                if (ParentQuest.CheckPlayerAttackHit(Position, 2.35f, out float _))
                {
                    Collected = true;
                    Enabled = false;
                    SoundManager.Play("magic_cast", 0.95f, 1.25f);
                    new VisualFX(Position, Rgb(255, 85, 135), 4.5f, 0.32f);
                    ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                }
            }
            else if (Mode == QuestNodeMode.MistThread)
            {
                _orb.RotationY += 95f * dt;
                if (dist < 3.6f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                {
                    Collected = true;
                    Enabled = false;
                    SoundManager.Play("scissor_snip", 1.0f, 1.05f + NodeIndex * 0.08f);
                    new VisualFX(Position + new Vector3(0, 1.5f, 0), Rgb(95, 235, 255), 4.5f, 0.32f);
                    ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                }
            }
            else if (Mode == QuestNodeMode.GoldenOrange)
            {
                _orb.RotationY += 120f * dt;
                _orb.Y = 0.72f + MathF.Abs(MathF.Sin(t * 4.2f + NodeIndex)) * 0.22f;
                if (dist < 3.6f && (Raylib.IsKeyPressed(KeyboardKey.E) || dist < 1.95f) && g.CanPlayerInteract)
                {
                    Collected = true;
                    Enabled = false;
                    SoundManager.Play("item_pickup", 0.95f, 1.10f + NodeIndex * 0.06f);
                    new VisualFX(Position + new Vector3(0, 0.9f, 0), Rgb(255, 165, 35), 4.2f, 0.30f);
                    ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                }
            }
            else if (Mode == QuestNodeMode.MirageOrb)
            {
                if (IsTrueOrb)
                {
                    _orb.RotationY += 75f * dt;
                    _orb.Y = 1.5f + MathF.Sin(t * 2.5f) * 0.14f;
                    _orb.Scale = new Vector3(1.1f, 1.7f, 1.1f);
                }
                else
                {
                    // Ложный мираж заметно дрожит и меняет масштаб
                    _orb.RotationY -= 240f * dt;
                    _orb.Y = 1.5f + MathF.Sin(t * 19.0f + NodeIndex) * 0.28f;
                    float glitch = 0.80f + MathF.Abs(MathF.Sin(t * 14.0f + NodeIndex)) * 0.45f;
                    _orb.Scale = new Vector3(glitch, 1.5f * glitch, glitch);
                }

                if (dist < 2.8f && (Raylib.IsKeyPressed(KeyboardKey.E) || dist < 1.85f))
                {
                    ParentQuest.OnMirageOrbTouched(this, IsTrueOrb);
                }
            }
            else if (Mode == QuestNodeMode.PushCart)
            {
                Vector3 toCenter = ParentQuest.Position - Position;
                toCenter.Y = 0f;
                float remDist = toCenter.Length();
                _labelEnt.BillboardText = $"[ТОЛКАЙТЕ ТЕЛЕЖКУ К ЛУЧУ ({remDist:F1}м)]";
                if (dist < 2.9f && remDist > 1.6f)
                {
                    Vector3 pushDir = Vector3.Normalize(toCenter);
                    Position += pushDir * 5.8f * dt;
                    Y = Physics.GetTerrainSpawnHeight(X, Z);
                    if ((int)((t + dt) * 5f) != (int)(t * 5f))
                        new VisualFX(Position + new Vector3(0, 1.6f, 0), Color.Orange, 1.8f, 0.18f);
                }
                if (remDist <= 1.85f)
                {
                    Collected = true;
                    Enabled = false;
                    ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                }
            }
            else
            {
                _orb.RotationY += 110f * dt;
                _orb.Y = 1.5f + MathF.Sin(t * 3.8f + NodeIndex) * 0.22f;
                if (dist < 3.4f)
                {
                    Collected = true;
                    _labelEnt.Enabled = false;
                    _orb.Enabled = false;
                    SoundManager.Play("shrine_use", 0.9f, 1.25f);
                    new VisualFX(Position + new Vector3(0, 1.4f, 0), _labelEnt.Color, 4.5f, 0.35f);
                    ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                }
            }
        }
    }

    public class CharacterQuestMonument : Entity
    {
        public readonly string CharKey;
        public readonly string AchKey;
        public bool Completed = false;
        private int _state = 0; // 0 = ожидание, 1 = испытание активно, 2 = завершено
        private int _subPhase = 0;
        private bool _paidOnce = false;
        private float _timer = 0f;
        private float _progress = 0f;
        private float _auxTimer = 0f;
        private float _meleeHitCooldown = 0f;
        private float _peakAirY = 0f;
        private float _lastPlayerTotalHp = 0f;
        private Vector3 _anchorPlayerPos = Vector3.Zero;
        private int _stepCount = 0;
        private int _startKills = 0;
        private float _startDamageDealt = 0f;
        private readonly int[] _cipherCode = new int[4];
        private readonly List<Entities.CombatTarget> _spawnedGuards = new();
        private readonly List<QuestSecondaryNode> _secondaryNodes = new();
        private readonly Entity _focalPart;
        private readonly Entity? _zoneRing;
        private readonly Entity _labelEnt;
        private readonly Color _themeCol;

        public int CurrentStep => _stepCount;

        public CharacterQuestMonument(Vector3 pos, string charKey) : base(position: pos)
        {
            CharKey = charKey;
            AchKey = $"ach_char_{charKey}";
            _themeCol = SurvivorDatabase.Classes.TryGetValue(charKey, out var cfg) ? cfg.VisorColor : Color.Gold;

            new Entity(parent: this, model: "cube", scale: new(2.8f, 0.35f, 2.8f), position: new(0, 0.18f, 0), color: Rgb(45, 52, 66));
            new Entity(parent: this, model: "diamond", scale: new(2.4f, 0.22f, 2.4f), position: new(0, 0.38f, 0), color: _themeCol, unlit: true);
            _focalPart = new Entity(parent: this, position: new(0, 1.6f, 0));

            string prompt = BuildUniqueMonumentVisuals(charKey);
            if (charKey is "commando" or "gwen" or "enforcer" or "reimu" or "mindustry" or "jotaro" or "jack_o")
            {
                float rad = charKey == "enforcer" ? 3.8f
                    : (charKey == "jotaro" ? 6.2f
                    : (charKey == "commando" ? 9.8f : (charKey == "jack_o" ? 11.0f : 12.0f)));
                _zoneRing = new Entity(parent: this, model: "sphere", scale: new(rad * 2f, 0.08f, rad * 2f), position: new(0, 0.08f, 0), color: Rgb(_themeCol.R, _themeCol.G, _themeCol.B, 55), unlit: true, enabled: false);
            }

            _labelEnt = new Entity(parent: this, position: new(0, 4.2f, 0), color: _themeCol)
            {
                BillboardText = prompt
            };
            Physics.Obstacles.Add(new ObstacleCircle(pos.X, pos.Z, 1.25f, 3.2f, pos.Y));
        }

        private string BuildUniqueMonumentVisuals(string key)
        {
            switch (key)
            {
                case "commando":
                    new Entity(parent: this, model: "cube", scale: new(1.9f, 2.4f, 1.9f), position: new(0, 1.3f, 0), rotation: new(12, 18, -8), color: Rgb(62, 72, 92));
                    new Entity(parent: this, model: "diamond", scale: new(2.0f, 1.1f, 2.0f), position: new(0.15f, 2.5f, 0), rotation: new(12, 18, -8), color: Rgb(235, 145, 35));
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 0.72f, position: new(0, 1.25f, 0), color: Rgb(255, 190, 55), unlit: true);
                    return "[E] Чёрный Ящик UES «Contact Light» (Удержать связь и отбить штурм!)";

                case "huntress":
                    new Entity(parent: this, model: "cube", scale: new(1.2f, 2.6f, 1.2f), position: new(0, 1.4f, 0), color: Rgb(42, 48, 64));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(2.4f, 0.65f, 0.35f), position: new(0, 0.65f, 0), color: Rgb(245, 55, 85), unlit: true);
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 0.65f, position: new(0, 0.65f, 0), color: Color.SkyBlue, unlit: true);
                    return "[E] Тотем Артемиды (Сбить 4 Светлячка в движении + Вожак!)";

                case "sol":
                    new Entity(parent: this, model: "cube", scale: new(2.3f, 1.3f, 1.6f), position: new(0, 0.85f, 0), color: Rgb(58, 36, 36));
                    new Entity(parent: _focalPart, model: "cube", scale: new(0.55f, 2.6f, 0.28f), position: new(0, 0.55f, 0), rotation: new(0, 0, -18), color: Rgb(245, 75, 35), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.45f, 0.32f, 1.45f), position: new(0, 0.15f, 0), color: Color.Gold, unlit: true);
                    return "[E] Наковальня Священного Ордена (Босс: Прототип «Джастис»!)";

                case "gwen":
                    new Entity(parent: this, model: "sphere", scale: new(1.3f, 2.4f, 1.1f), position: new(0, 1.5f, 0), color: Rgb(48, 62, 88));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(0.38f, 2.6f, 0.25f), position: new(-0.25f, 0.5f, 0.35f), rotation: new(0, 0, 26), color: Rgb(95, 235, 255), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(0.38f, 2.6f, 0.25f), position: new(0.25f, 0.5f, 0.35f), rotation: new(0, 0, -26), color: Rgb(95, 235, 255), unlit: true);
                    return "[E] Манекен Изольды (Перерезать 4 Нити Тумана + Теневой Жнец!)";

                case "v1":
                    new Entity(parent: this, model: "cube", scale: new(1.65f, 2.4f, 1.25f), position: new(0, 1.35f, 0), color: Rgb(45, 28, 34));
                    new Entity(parent: _focalPart, model: "cube", scale: new(1.35f, 0.95f, 0.18f), position: new(0, 0.35f, 0.55f), color: Rgb(255, 45, 55), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scaleUniform: 0.72f, position: new(0, 1.15f, 0), color: Color.Gold, unlit: true);
                    return "[E] Терминал Кибер-Грайнда (Зачистить 3 волны на топливе крови!)";

                case "cheburashka":
                    new Entity(parent: this, model: "cube", scale: new(2.3f, 1.65f, 2.0f), position: new(0, 1.0f, 0), color: Rgb(158, 108, 56));
                    new Entity(parent: this, model: "cube", scale: new(2.38f, 0.22f, 2.08f), position: new(0, 1.75f, 0), color: Rgb(118, 76, 36));
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 0.78f, position: new(0, 0.45f, 0), color: Rgb(255, 150, 25), unlit: true);
                    return "[E] Ящик с Апельсинами (Собрать 5 Золотых Апельсинов под огнём!)";

                case "artificer":
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.8f, 3.2f, 1.8f), color: Rgb(90, 215, 255, 165), unlit: true);
                    new Entity(parent: _focalPart, model: "cube", scale: new(0.55f, 1.4f, 0.45f), color: Rgb(145, 85, 225));
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 0.42f, position: new(0, 0.95f, 0), color: Color.White);
                    return "[E] Замёрзший Кристалл Времени (5 Лунных Монет + Лунные Химеры)";

                case "buzz":
                    new Entity(parent: this, model: "diamond", scale: new(2.2f, 1.3f, 3.8f), position: new(0, 1.1f, 0), rotation: new(14, 0, -10), color: Rgb(225, 230, 240));
                    new Entity(parent: this, model: "cube", scale: new(4.4f, 0.22f, 1.1f), position: new(0, 1.1f, -0.2f), rotation: new(0, 0, -10), color: Rgb(135, 55, 210));
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 0.85f, position: new(0, 0.5f, 0), color: Color.Lime, unlit: true);
                    return "[E] Звездолёт XP-38 (Прыжок >12м + Босс: Дредноут Зурга!)";

                case "enforcer":
                    new Entity(parent: this, model: "cube", scale: new(2.0f, 2.6f, 0.45f), position: new(0, 1.5f, 0.6f), color: Rgb(42, 68, 115));
                    new Entity(parent: this, model: "cube", scale: new(1.6f, 0.7f, 0.52f), position: new(0, 2.1f, 0.6f), color: Color.SkyBlue, unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scaleUniform: 0.9f, color: Color.SkyBlue, unlit: true);
                    return "[E] Щитовой Блокпост UES (10 сек обороны под огнём Альянса!)";

                case "scout":
                    new Entity(parent: this, model: "cube", scale: new(2.0f, 1.0f, 1.2f), position: new(0, 0.7f, 0), color: Rgb(75, 55, 42));
                    new Entity(parent: _focalPart, model: "cube", scale: new(0.95f, 0.65f, 0.32f), color: Rgb(225, 50, 50), unlit: true);
                    new Entity(parent: _focalPart, model: "cube", scale: new(0.35f, 0.16f, 0.16f), position: new(0, 0.4f, 0), color: Color.Gold);
                    return "[E] Кейс Разведданных (Воздушный паркур по 4 кольцам)";

                case "m4a1":
                    new Entity(parent: this, model: "cube", scale: new(1.5f, 2.2f, 0.9f), position: new(0, 1.2f, 0), color: Rgb(48, 56, 52));
                    new Entity(parent: _focalPart, model: "cube", scale: new(1.3f, 0.9f, 0.15f), position: new(0, 0.3f, 0.45f), color: Rgb(95, 255, 135), unlit: true);
                    return "[E] Терминал «Грифон» (Ввод 4-значного шифра под огнём)";

                case "medic":
                    new Entity(parent: this, model: "sphere", scale: new(1.7f, 2.6f, 1.7f), position: new(0, 1.6f, 0), color: Rgb(230, 235, 242));
                    new Entity(parent: _focalPart, model: "cube", scale: new(1.1f, 0.35f, 0.35f), color: Color.Red, unlit: true);
                    new Entity(parent: _focalPart, model: "cube", scale: new(0.35f, 1.1f, 0.35f), color: Color.Red, unlit: true);
                    return "[E] Хирургическая Капсула (Выжить 10 сек на 1 HP!)";

                case "aigis":
                    new Entity(parent: this, model: "cube", scale: new(1.4f, 3.0f, 1.1f), position: new(0, 1.7f, 0), color: Rgb(235, 195, 65));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(0.95f, 1.4f, 0.45f), position: new(0, 0.2f, 0.5f), color: Color.SkyBlue, unlit: true);
                    return "[E] Золотой Саркофаг S.E.E.S. (Босс: Жнец Тартара!)";

                case "reimu":
                    foreach (float sx in new[] { -1.4f, 1.4f })
                        new Entity(parent: this, model: "cube", scale: new(0.32f, 3.6f, 0.32f), position: new(sx, 1.8f, -0.6f), color: Rgb(215, 38, 42));
                    new Entity(parent: this, model: "cube", scale: new(3.8f, 0.35f, 0.45f), position: new(0, 3.5f, -0.6f), color: Rgb(215, 38, 42));
                    new Entity(parent: this, model: "cube", scale: new(1.5f, 1.0f, 1.1f), position: new(0, 0.7f, 0.4f), color: Rgb(135, 85, 45));
                    new Entity(parent: _focalPart, model: "diamond", scaleUniform: 0.75f, color: Color.Gold, unlit: true);
                    return "[E] Врата Тории Хакурей (12 сек Данмаку без урона!)";

                case "mindustry":
                    new Entity(parent: this, model: "cube", scale: new(2.2f, 1.8f, 2.2f), position: new(0, 1.1f, 0), color: Rgb(65, 70, 82));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.3f, 2.4f, 1.3f), position: new(0, 0.2f, 0), color: Rgb(245, 115, 185), unlit: true);
                    return "[E] Ториевый Бур (Оборона 350 HP + сбор энергоячеек)";

                case "slark":
                    _progress = 1200f;
                    new Entity(parent: this, model: "cube", scale: new(2.1f, 2.5f, 2.1f), position: new(0, 1.5f, 0), color: Rgb(35, 68, 78));
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 0.95f, color: Rgb(65, 245, 205), unlit: true);
                    return "Клетка Тёмного Рифа [1200/1200 HP]\n[Разбейте цепи атаками -> Босс: Надзиратель Рифа!]";

                case "tidehunter":
                    new Entity(parent: _focalPart, model: "cube", scale: new(0.45f, 2.8f, 0.45f), position: new(0, 0.4f, 0), color: Rgb(55, 165, 135));
                    new Entity(parent: _focalPart, model: "sphere", scale: new(2.4f, 0.65f, 0.55f), position: new(0, -0.5f, 0), color: Rgb(75, 225, 185), unlit: true);
                    return "[E] Якорь Левиафана (Жертва 1 предмета + Стражи Впадины)";

                case "freeman":
                    new Entity(parent: this, model: "diamond", scale: new(1.8f, 3.2f, 1.8f), position: new(0, 1.8f, 0), color: Rgb(75, 82, 95));
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 1.15f, position: new(0, 0.4f, 0), color: Rgb(255, 165, 35), unlit: true);
                    new Entity(parent: _focalPart, model: "cube", scale: new(0.14f, 1.45f, 0.14f), position: new(0, 0.6f, 0.65f), rotation: new(0, 0, 25), color: Rgb(225, 35, 35), unlit: true);
                    return "[E] Антимасс-Спектрометр (Кристалл GG-3883 + Десант Альянса)";

                case "dante":
                    new Entity(parent: this, model: "cube", scale: new(1.6f, 3.4f, 1.2f), position: new(0, 1.9f, 0), color: Rgb(145, 28, 35));
                    new Entity(parent: _focalPart, model: "sphere", scale: new(1.5f, 1.5f, 0.35f), position: new(0, 0.8f, 0.55f), color: Color.Gold, unlit: true);
                    return "[E] Часы Судного Дня (3x Полночь 12:00 под атаками Теней)";

                case "ahri":
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 1.25f, color: Rgb(105, 225, 255), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.8f, 0.35f, 1.8f), color: Rgb(255, 105, 195), unlit: true);
                    return "[E] Святилище Ионии (Поймать Лисёнка 4 раза + Духи)";

                case "lappland":
                    new Entity(parent: this, model: "cube", scale: new(0.22f, 2.8f, 0.45f), position: new(-0.4f, 1.8f, 0), rotation: new(0, 0, 24), color: Color.White, unlit: true);
                    new Entity(parent: this, model: "cube", scale: new(0.22f, 2.8f, 0.45f), position: new(0.4f, 1.8f, 0), rotation: new(0, 0, -24), color: Rgb(225, 45, 65), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scaleUniform: 0.9f, color: Rgb(235, 55, 75), unlit: true);
                    return "[E] Алтарь Двух Клинков (Отряд Сарказов за 16 сек!)";

                case "w_arknights":
                    new Entity(parent: this, model: "cube", scale: new(2.1f, 1.3f, 1.6f), position: new(0, 0.85f, 0), color: Rgb(55, 48, 45));
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 0.72f, position: new(0, 0.95f - 0.1f, 0), color: Rgb(255, 45, 55), unlit: true);
                    return "[E] Склад C4 Каздела (Обезвредить 4 бомбы за 16 сек!)";

                case "gold_ship":
                    foreach (float sx in new[] { -1.6f, 1.6f })
                        new Entity(parent: this, model: "cube", scale: new(0.35f, 3.6f, 0.35f), position: new(sx, 1.8f, 0), color: Rgb(235, 240, 250));
                    new Entity(parent: this, model: "cube", scale: new(3.8f, 0.55f, 0.55f), position: new(0, 3.6f, 0), color: Rgb(225, 45, 65));
                    new Entity(parent: _focalPart, model: "diamond", scaleUniform: 0.95f, color: Color.Gold, unlit: true);
                    return "[E] Стартовые Ворота (Спринт через 5 ворот за 20 сек)";

                case "pushnoy":
                    new Entity(parent: this, model: "cube", scale: new(1.9f, 2.2f, 1.1f), position: new(0, 1.3f, -0.3f), color: Rgb(35, 38, 45));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.1f, 1.8f, 0.35f), position: new(0, 0.3f, 0.4f), rotation: new(0, 0, -25), color: Rgb(255, 125, 25), unlit: true);
                    return "Гитарный Генератор «Галилео» [0 / 10 000 В]\n[Атакуйте генератор, чтобы набрать 10 000 Вольт!]";

                case "freddy":
                    new Entity(parent: this, model: "cube", scale: new(1.7f, 2.5f, 1.4f), position: new(0, 1.4f, 0), color: Rgb(125, 45, 195));
                    new Entity(parent: _focalPart, model: "cube", scale: new(1.3f, 1.0f, 0.2f), position: new(0, 0.4f, 0.65f), color: Color.Gold, unlit: true);
                    return "[E] Музыкальная Шкатулка (Смена до 06:00 AM + Золотой Эндоскелет!)";

                case "steve":
                    foreach (float sx in new[] { -1.35f, 1.35f })
                        new Entity(parent: this, model: "cube", scale: new(0.65f, 3.6f, 0.65f), position: new(sx, 1.9f, 0), color: Rgb(32, 22, 48));
                    new Entity(parent: this, model: "cube", scale: new(3.3f, 0.65f, 0.65f), position: new(0, 3.6f, 0), color: Rgb(32, 22, 48));
                    new Entity(parent: _focalPart, model: "cube", scale: new(2.0f, 2.8f, 0.18f), position: new(0, 0.3f, 0), color: Rgb(175, 65, 255, 180), unlit: true);
                    return "[E] Портал Незера (3 обсидиановых монолита + Ифриты Незера!)";

                case "jotaro":
                    new Entity(parent: this, model: "sphere", scale: new(2.4f, 1.5f, 2.2f), position: new(0, 0.8f, 0), color: Rgb(68, 52, 82));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(0.55f, 2.4f, 0.25f), position: new(0, 0.6f, 0), rotation: new(0, 0, 20), color: Color.Gold, unlit: true);
                    return "[E] Стрела Стенда (Идти 7 сек против ураганного ветра!)";

                case "vyper":
                    new Entity(parent: this, model: "cube", scale: new(2.2f, 2.8f, 2.2f), position: new(0, 1.6f, 0), color: Rgb(42, 65, 48));
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 0.9f, color: Rgb(85, 255, 95), unlit: true);
                    return "[E] Бронекапсула OSIC (Отстрел 4 вращающихся замков)";

                case "haze":
                    new Entity(parent: _focalPart, model: "diamond", scale: new(2.2f, 2.8f, 0.35f), color: Rgb(255, 195, 70), unlit: true);
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 1.15f, color: Rgb(145, 95, 255), unlit: true);
                    return "[E] Разлом OSIC (Найти 3 истинные сферы + Тень-перехватчик)";

                case "jack_o":
                    new Entity(parent: this, model: "cube", scale: new(0.14f, 3.4f, 0.14f), position: new(0.75f, 1.85f, 0), color: Color.Gold);
                    new Entity(parent: this, model: "cube", scale: new(1.25f, 0.80f, 0.10f), position: new(1.38f, 2.95f, 0), color: Rgb(225, 38, 52), unlit: true);
                    new Entity(parent: _focalPart, model: "sphere", scale: new(1.45f, 1.32f, 1.45f), color: Rgb(36, 34, 44));
                    foreach (float ex in new[] { -0.32f, 0f, 0.32f })
                        new Entity(parent: _focalPart, model: "diamond", scale: new(0.22f, 0.34f, 0.18f), position: new(ex, 0.12f, 0.68f), color: Rgb(155, 255, 75), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.65f, 0.14f, 1.65f), position: new(0, 0.95f, 0), rotation: new(16f, 0, -14f), color: Color.Gold, unlit: true);
                    return "[E] Алтарь Железной Тыквы (Удержать Знамя 14 сек и отразить Задний Двор!)";

                case "dmc_trio":
                default:
                    new Entity(parent: this, model: "diamond", scale: new(2.0f, 1.8f, 2.0f), position: new(0, 1.0f, 0), color: Rgb(135, 25, 38));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(0.55f, 2.8f, 0.22f), position: new(0, 0.9f, 0), color: Rgb(255, 55, 75), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.2f, 0.35f, 0.35f), position: new(0, 1.6f, 0), color: Color.Gold, unlit: true);
                    return "[E] Пьедестал Спарды (Ранг SSS и Босс: Мираж Вергилия!)";
            }
        }

        public bool CheckPlayerAttackHit(Vector3 targetPos, float hitRadius, out float damage)
        {
            damage = 0f;
            var g = WorldContext.Game;
            var ctrl = Entities.CombatManager.Controller;
            if (g == null || ctrl == null) return false;

            foreach (var ent in Engine.AllEntities.ToArray())
            {
                if (ent is Entities.Projectile proj && proj.IsPlayer && !proj.IsDestroyed)
                {
                    if (Vector3.Distance(proj.Position, targetPos) <= hitRadius + proj.ScaleX * 0.45f)
                    {
                        damage = MathF.Max(15f, proj.Damage);
                        proj.SafeDestroy();
                        SoundManager.Play("hit", 0.75f);
                        return true;
                    }
                }
            }

            if (_meleeHitCooldown <= 0f && (Raylib.IsMouseButtonDown(MouseButton.Left) || Raylib.IsMouseButtonPressed(MouseButton.Right)))
            {
                Vector3 eyePos = g.PlayerPos + new Vector3(0, 1.5f, 0);
                float d = Vector3.Distance(eyePos, targetPos);
                Vector3 aimDir = ctrl.GetAimDirection();
                bool meleeClose = d <= hitRadius + 4.2f;
                bool hitscanAim = d <= 34.0f && d > 0.2f && Vector3.Dot(aimDir, Vector3.Normalize(targetPos - eyePos)) >= 0.92f;
                if (meleeClose || hitscanAim)
                {
                    _meleeHitCooldown = 0.20f;
                    damage = MathF.Max(22f, ctrl.BaseDamage);
                    SoundManager.Play("hit", 0.75f);
                    return true;
                }
            }

            return false;
        }

        private void ClearSecondaryNodes()
        {
            foreach (var n in _secondaryNodes)
                if (IsValid(n)) Destroy(n);
            _secondaryNodes.Clear();
        }

        private void ClearSpawnedGuards(IPlayerContext? g = null)
        {
            var run = (g ?? WorldContext.Game) as GameRunContext;
            foreach (var m in _spawnedGuards)
            {
                if (run != null && run.ActiveBoss == m)
                    run.ActiveBoss = null;
                if (IsValid(m) && !m.IsAlly)
                    Destroy(m);
            }
            _spawnedGuards.Clear();
        }

        private void CompleteCharacterQuest(IPlayerContext g)
        {
            if (Completed) return;
            Completed = true;
            _state = 2;
            ClearSecondaryNodes();
            ClearSpawnedGuards(g);
            if (_zoneRing != null) _zoneRing.Enabled = false;
            _focalPart.Color = Color.Lime;
            _labelEnt.Color = Color.Lime;
            string cName = SurvivorDatabase.Classes.TryGetValue(CharKey, out var cfg) ? cfg.Name : CharKey.ToUpper();
            _labelEnt.BillboardText = $"ИСПЫТАНИЕ ЗАВЕРШЕНО ✓\nОТКРЫТ ПЕРСОНАЖ: {cName}!";
            new VisualFX(Position + new Vector3(0, 1.8f, 0), Color.Gold, 9.5f, 0.50f);
            g.UnlockAchievement(AchKey);
            g.SpawnItemDrop(Position + new Vector3(0, 1.2f, 0), null, CharKey == "commando" ? "legendary_chest" : "large_chest");
            g.TriggerCharacterUnlockCutscene(CharKey);
        }

        public void OnSecondaryNodeTouched(int idx)
        {
            var g = WorldContext.Game;
            if (g == null || Completed) return;
            _stepCount++;

            if (CharKey == "scout")
            {
                if (_stepCount >= 4) CompleteCharacterQuest(g);
                else g.ShowBanner($"ВОЗДУШНОЕ КОЛЬЦО ПРОЙДЕНО ({_stepCount}/4)! НЕ КАСАЙТЕСЬ ЗЕМЛИ!", Color.SkyBlue, 1.8f);
            }
            else if (CharKey == "huntress")
            {
                if (_stepCount >= 4)
                {
                    _stepCount = 5;
                    ClearSecondaryNodes();
                    SoundManager.Play("boss_roar", 0.95f, 1.15f);
                    AddTrialEnemies(g, 1, "bison", 9.0f, elite: true, hpMultiplier: 1.85f);
                    AddTrialEnemies(g, 2, "lemurian", 8.0f);
                    g.ShowBanner("СВЕТЛЯЧКИ СБИТЫ! СРАЗИТЕ ЭЛИТНОГО ВОЖАКА ПОЛНОЛУНИЯ!", Rgb(255, 75, 115), 3.2f);
                }
                else
                {
                    g.ShowBanner($"АСТРАЛЬНЫЙ СВЕТЛЯЧОК СБИТ ({_stepCount}/4)! ПРОДОЛЖАЙТЕ ДВИГАТЬСЯ!", Color.SkyBlue, 1.8f);
                }
            }
            else if (CharKey == "gwen")
            {
                if (_stepCount >= 4)
                {
                    _stepCount = 5;
                    ClearSecondaryNodes();
                    SoundManager.Play("boss_roar", 1.0f, 0.95f);
                    SpawnTrialBoss(g, "shadow_reaper", "ТЕНЕВОЙ ЖНЕЦ ПОГИБЕЛИ");
                    g.ShowBanner("НИТИ ЧЁРНОГО ТУМАНА ПЕРЕРЕЗАНЫ! ИЗГОНИТЕ ТЕНЕВОГО ЖНЕЦА ПОГИБЕЛИ!", Rgb(95, 235, 255), 3.4f);
                }
                else
                {
                    g.ShowBanner($"НИТЬ ЧЁРНОГО ТУМАНА ПЕРЕРЕЗАНА ({_stepCount}/4)!", Rgb(95, 235, 255), 1.8f);
                }
            }
            else if (CharKey == "cheburashka")
            {
                if (_stepCount >= 5) CompleteCharacterQuest(g);
                else g.ShowBanner($"ЗОЛОТОЙ АПЕЛЬСИН СОБРАН ({_stepCount}/5)! ОСТАЛОСЬ {_timer:F1} СЕК!", Color.Orange, 1.8f);
            }
            else if (CharKey == "ahri")
            {
                if (_stepCount >= 4)
                {
                    _stepCount = 5;
                    AddTrialEnemies(g, 2, "hollow_shadow", 8.5f, elite: true);
                    g.ShowBanner("ДУХ ЛИСЁНКА СПАСЁН! ПОБЕДИТЕ ПОХИТИТЕЛЕЙ ЭССЕНЦИИ!", _themeCol, 2.8f);
                }
                else
                {
                    g.ShowBanner($"ДУХ ЛИСЁНКА ПОЙМАН ({_stepCount}/4)! ДОГОНИТЕ ЕГО СНОВА!", _themeCol, 1.8f);
                }
            }
            else if (CharKey == "w_arknights")
            {
                if (_stepCount >= 4) CompleteCharacterQuest(g);
                else g.ShowBanner($"ЗАРЯД C4 ОБЕЗВРЕЖЕН ({_stepCount}/4)! ОСТАЛОСЬ {_timer:F1} СЕК!", Color.Lime, 1.8f);
            }
            else if (CharKey == "gold_ship")
            {
                if (_stepCount >= 5) CompleteCharacterQuest(g);
                else g.ShowBanner($"ВОРОТА #{_stepCount}/5 ПРОЙДЕНЫ! МЧИТЕСЬ К СЛЕДУЮЩИМ!", Color.Gold, 1.8f);
            }
            else if (CharKey == "steve")
            {
                if (_stepCount >= 3)
                {
                    _labelEnt.BillboardText = "[E] Обсидиан собран (3/3)! Нажмите [E] у рамки, чтобы зажечь Портал Незера!";
                    g.ShowBanner("ВСЕ 3 БЛОКА ОБСИДИАНА ДОБЫТЫ! ЗАЖГИТЕ ПОРТАЛ НЕЗЕРА [E]!", Rgb(195, 85, 255), 2.8f);
                }
                else
                {
                    g.ShowBanner($"ОБСИДИАНОВЫЙ МОНОЛИТ РАЗРУШЕН ({_stepCount}/3)!", Rgb(195, 85, 255), 1.8f);
                }
            }
            else if (CharKey == "vyper")
            {
                if (_stepCount >= 4) CompleteCharacterQuest(g);
                else g.ShowBanner($"МАГНИТНЫЙ ЗАМОК СБИТ ({_stepCount}/4)!", Color.Lime, 1.8f);
            }
            else if (CharKey == "mindustry")
            {
                _progress = MathF.Min(100f, _progress + 18f);
                g.ShowBanner("ЭНЕРГОЯЧЕЙКА ПОДОБРАНА: БУРЕНИЕ УСКОРЕНО (+18%)!", Color.SkyBlue, 1.8f);
            }
            else if (CharKey == "freeman")
            {
                _stepCount = 2;
                SoundManager.Play("railgun", 1.15f, 0.90f);
                new LightningBeam(Position + new Vector3(0, 8.0f, 0), Position + new Vector3(0, 1.2f, 0), Color.Orange, 0.55f);
                new VisualFX(Position + new Vector3(0, 1.8f, 0), Color.Orange, 14.0f, 0.50f);
                SpawnTrialEnemies(g, 4, "combine_soldier", 9.5f);
                g.ShowBanner("КАСКАДНЫЙ РЕЗОНАНС! ОТРАЗИТЕ ДЕСАНТ ШТУРМОВИКОВ АЛЬЯНСА!", Color.Orange, 3.5f);
            }
        }

        public void OnRelayPressed(int colorIdx)
        {
            var g = WorldContext.Game;
            if (g == null || Completed || _state != 1 || CharKey != "m4a1") return;

            if (_cipherCode[_stepCount] == colorIdx)
            {
                _stepCount++;
                SoundManager.Play("ui_ready", 1.0f, 1.0f + _stepCount * 0.12f);
                if (_stepCount >= 4)
                {
                    CompleteCharacterQuest(g);
                }
                else
                {
                    UpdateM4A1CipherLabel();
                    g.ShowBanner($"ВЕРНЫЙ СИГНАЛ ({_stepCount}/4)!", Color.Lime, 1.5f);
                }
            }
            else
            {
                _stepCount = 0;
                SoundManager.Play("ui_error");
                GenerateM4A1Cipher();
                UpdateM4A1CipherLabel();
                g.ShowBanner("ОШИБКА ДЕШИФРОВКИ! СГЕНЕРИРОВАН НОВЫЙ КОД!", Color.Red, 2.2f);
            }
        }

        private void GenerateM4A1Cipher()
        {
            for (int i = 0; i < 4; i++)
                _cipherCode[i] = Random.Shared.Next(0, 3);
        }

        private void UpdateM4A1CipherLabel()
        {
            string[] names = { "КРАСНЫЙ", "ЗЕЛЁНЫЙ", "СИНИЙ" };
            string seq = $"{names[_cipherCode[0]]} -> {names[_cipherCode[1]]} -> {names[_cipherCode[2]]} -> {names[_cipherCode[3]]}";
            _labelEnt.BillboardText = $"ШИФР «ГРИФОН» (Шаг {_stepCount + 1}/4):\n[ {seq} ]";
        }

        public void OnMirageOrbTouched(QuestSecondaryNode node, bool isTrueOrb)
        {
            var g = WorldContext.Game;
            if (g == null || Completed || _state != 1 || CharKey != "haze") return;

            if (isTrueOrb)
            {
                _stepCount++;
                SoundManager.Play("magic_cast", 1.0f, 1.1f + _stepCount * 0.12f);
                new VisualFX(node.Position + new Vector3(0, 1.5f, 0), Color.Gold, 4.5f, 0.32f);
                if (_stepCount >= 3)
                {
                    _stepCount = 4;
                    ClearSecondaryNodes();
                    _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                    if (_spawnedGuards.Count == 0)
                        CompleteCharacterQuest(g);
                    else
                        g.ShowBanner("ИСТИННЫЕ СФЕРЫ СОБРАНЫ! УСТРАНИТЕ АРКАННУЮ ТЕНЬ-ПЕРЕХВАТЧИКА!", Color.Gold, 2.8f);
                }
                else
                {
                    SpawnHazeMirageRound(g);
                    g.ShowBanner($"ИСТИННЫЙ СОН РАСПОЗНАН ({_stepCount}/3)!", Color.Gold, 1.8f);
                }
            }
            else
            {
                _stepCount = 0;
                SoundManager.Play("zap", 0.95f, 0.9f);
                new VisualFX(node.Position + new Vector3(0, 1.5f, 0), Color.Magenta, 4.5f, 0.32f);
                g.PlayerHp = MathF.Max(1f, g.PlayerHp - 12f);
                SpawnHazeMirageRound(g);
                g.ShowBanner("ЭТО ЛОЖНЫЙ МИРАЖ! СЧЁТЧИК СБРОШЕН (0/3)!", Color.Red, 2.2f);
            }
        }

        private void SpawnHazeMirageRound(IPlayerContext g)
        {
            ClearSecondaryNodes();
            int trueIdx = Random.Shared.Next(0, 5);
            for (int i = 0; i < 5; i++)
            {
                float ang = (i / 5.0f) * MathF.PI * 2f + _stepCount * 0.7f;
                Vector3 p = Position + new Vector3(MathF.Cos(ang) * 10.5f, 0, MathF.Sin(ang) * 10.5f);
                p = Physics.FindSafeSpawnPoint(p);
                bool isTrue = i == trueIdx;
                var orb = new QuestSecondaryNode(p, this, i, "[E] Сфера Сновидений",
                    isTrue ? Color.Gold : Rgb(215, 165, 95), QuestNodeMode.MirageOrb, isTrueOrb: isTrue);
                _secondaryNodes.Add(orb);
                g.StageEntities.Add(orb);
            }
            _labelEnt.BillboardText = $"Разлом Снов OSIC: Найдите стабильную Золотую Сферу ({_stepCount}/3)";
        }

        private void RespawnHuntressFireflies(IPlayerContext g)
        {
            ClearSecondaryNodes();
            for (int i = 0; i < 4; i++)
            {
                var fly = new QuestSecondaryNode(Position + new Vector3(7f, 2.5f, 0), this, i,
                    $"[СВЕТЛЯЧОК #{i + 1}/4]", Rgb(255, 75, 115), QuestNodeMode.AstralFirefly);
                _secondaryNodes.Add(fly);
                g.StageEntities.Add(fly);
            }
        }

        public override void OnUpdate(float dt)
        {
            var g = WorldContext.Game;
            if (g == null || IsDestroyed) return;

            bool alreadyDone = ProgressData.Instance.UnlockedAchievements.Contains(AchKey)
                || (CharKey != "commando" && ProgressData.Instance.IsCharacterUnlocked(CharKey));
            if (!Completed && alreadyDone)
            {
                Completed = true;
                _labelEnt.Enabled = false;
                return;
            }
            if (Completed) return;

            _meleeHitCooldown = MathF.Max(0f, _meleeHitCooldown - dt);
            float t = (float)Raylib.GetTime();
            _focalPart.RotationY += 75f * dt;
            _focalPart.Y = 1.6f + MathF.Sin(t * 3.0f) * 0.16f;

            float dist = Vector3.Distance(Position, g.PlayerPos);
            _labelEnt.Enabled = dist < 16.5f || _state == 1;

            // СЛАРК: Клетка Тёмного Рифа разбивается напрямую атаками оружия (1200 HP) -> выходит босс Надзиратель Рифа!
            if (CharKey == "slark" && _state == 0)
            {
                if (CheckPlayerAttackHit(Position + new Vector3(0, 1.5f, 0), 2.6f, out float dmg))
                {
                    _progress = MathF.Max(0f, _progress - dmg);
                    new VisualFX(Position + new Vector3(0, 1.5f, 0), _themeCol, 3.4f, 0.22f);
                    _labelEnt.BillboardText = $"Клетка Тёмного Рифа [{(int)_progress} / 1200 HP]\n[Разбейте оковы своими атаками!]";
                    if (_progress <= 0f)
                    {
                        _state = 1;
                        SoundManager.Play("explosion", 1.0f, 1.15f);
                        SoundManager.Play("boss_roar", 1.05f, 0.90f);
                        SpawnTrialBoss(g, "warden_reef", "НАДЗИРАТЕЛЬ ТЁМНОГО РИФА");
                        g.ShowBanner("ОКОВЫ СОРВАНЫ! ПОБЕДИТЕ ТЮРЕМНОГО БОССА — НАДЗИРАТЕЛЯ ТЁМНОГО РИФА!", _themeCol, 3.6f);
                        return;
                    }
                }
            }

            // АЛЕКСАНДР ПУШНОЙ: Раскачка напряжения генератора атаками до 10 000 В (со спавном элементалей)
            if (CharKey == "pushnoy")
            {
                if (_progress > 0f)
                {
                    _progress = MathF.Max(0f, _progress - 1550f * dt);
                    _labelEnt.BillboardText = $"Гитарный Генератор «Галилео»: {(int)_progress} / 10 000 В\n[Быстро атакуйте генератор!]";
                }
                if (CheckPlayerAttackHit(Position + new Vector3(0, 1.6f, 0), 2.8f, out float dmg))
                {
                    float prevP = _progress;
                    _progress += MathF.Max(1180f, dmg * 22f);
                    SoundManager.Play("zap", 0.85f, 0.85f + (_progress / 10000f) * 0.5f);
                    new LightningBeam(Position + new Vector3(0, 3.6f, 0), g.PlayerPos + new Vector3(0, 1.3f, 0), Color.SkyBlue, 0.24f);
                    if (prevP < 3800f && _progress >= 3800f)
                        AddTrialEnemies(g, 2, "jellyfish", 9.0f);
                    if (prevP < 7200f && _progress >= 7200f)
                        AddTrialEnemies(g, 2, "wisp", 9.5f);
                    _labelEnt.BillboardText = $"Гитарный Генератор «Галилео»: {(int)MathF.Min(10000f, _progress)} / 10 000 В!";
                    if (_progress >= 10000f)
                    {
                        SoundManager.Play("ult_cast", 1.1f, 1.0f);
                        CompleteCharacterQuest(g);
                        return;
                    }
                }
            }

            // ЧЕБУРАШКА: Ящик апельсинов можно открыть как нажатием [E], так и ударом сверху с высоты >10м!
            if (CharKey == "cheburashka" && _state == 0)
            {
                if (!g.PlayerGrounded)
                {
                    if (g.PlayerPos.Y > _peakAirY) _peakAirY = g.PlayerPos.Y;
                }
                else
                {
                    if (_peakAirY - Y >= 10.0f && Physics.Distance2D(Position, g.PlayerPos) <= 4.8f)
                    {
                        SoundManager.Play("slam_land", 1.15f, 0.95f);
                        StartOrAdvanceTrial(g);
                        return;
                    }
                    _peakAirY = g.PlayerPos.Y;
                }
            }

            if (_state == 1)
            {
                UpdateActiveTrial(g, dt, dist);
                return;
            }

            if (dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
            {
                StartOrAdvanceTrial(g);
            }
        }

        private void StartOrAdvanceTrial(IPlayerContext g)
        {
            float curShield = Entities.CombatManager.Controller?.Shield ?? 0f;

            switch (CharKey)
            {
                case "commando":
                    _state = 1;
                    _progress = 0f;
                    _stepCount = 0;
                    if (_zoneRing != null) _zoneRing.Enabled = true;
                    SoundManager.Play("shrine_use");
                    SpawnTrialEnemies(g, 3, "combine_soldier", 9.5f);
                    g.ShowBanner("МАЯК «CONTACT LIGHT» АКТИВИРОВАН! УДЕРЖИВАЙТЕ ПЕРИМЕТР И ОТБЕЙТЕ ШТУРМ АЛЬЯНСА!", Color.Orange, 3.5f);
                    break;

                case "huntress":
                    _state = 1;
                    _stepCount = 0;
                    _auxTimer = 0f;
                    _anchorPlayerPos = g.PlayerPos;
                    SoundManager.Play("magic_cast", 1.0f, 1.15f);
                    RespawnHuntressFireflies(g);
                    g.ShowBanner("ОХОТА ПОЛНОЛУНИЯ: СБЕЙТЕ 4 СВЕТЛЯЧКА В НЕПРЕРЫВНОМ ДВИЖЕНИИ (НЕ СТОЙТЕ >1.2С)!", Rgb(255, 75, 115), 3.5f);
                    break;

                case "sol":
                    _state = 1;
                    _stepCount = 0;
                    SoundManager.Play("flame", 1.15f, 0.9f);
                    SoundManager.Play("boss_roar", 1.1f, 0.85f);
                    new VisualFX(Position + new Vector3(0, 2.0f, 0), Rgb(255, 85, 35), 9.0f, 0.45f);
                    SpawnTrialBoss(g, "justice_gear", "ПРОТОТИП «ДЖАСТИС»");
                    g.ShowBanner("ОГРАНИЧИТЕЛЬ ПЛАМЕНИ СОРВАН! СОКРУШИТЕ ПРОБУДИВШЕГОСЯ ПРОТОТИПА «ДЖАСТИС»!", Rgb(255, 95, 35), 3.6f);
                    break;

                case "gwen":
                    _state = 1;
                    _stepCount = 0;
                    ClearSecondaryNodes();
                    if (_zoneRing != null) _zoneRing.Enabled = true;
                    SoundManager.Play("scissor_snip", 1.05f, 1.0f);
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = i * (MathF.PI * 0.5f) + 0.35f;
                        Vector3 tp = Position + new Vector3(MathF.Cos(ang) * 8.5f, 0, MathF.Sin(ang) * 8.5f);
                        tp = Physics.FindSafeSpawnPoint(tp);
                        var thNode = new QuestSecondaryNode(tp, this, i, $"[E] Перерезать Нить Тумана #{i + 1}/4", Rgb(95, 235, 255), QuestNodeMode.MistThread);
                        _secondaryNodes.Add(thNode);
                        g.StageEntities.Add(thNode);
                    }
                    SpawnTrialEnemies(g, 2, "hollow_shadow", 9.0f);
                    g.ShowBanner("СВЯЩЕННЫЙ ТУМАН: ПЕРЕРЕЖЬТЕ [E] ВСЕ 4 НИТИ ЧЁРНОГО ТУМАНА И ИЗГОНИТЕ ЖНЕЦА!", Rgb(95, 235, 255), 3.5f);
                    break;

                case "v1":
                    _state = 1;
                    _stepCount = 1;
                    _timer = 18.0f;
                    SoundManager.Play("coin_ricoshot", 1.0f, 0.95f);
                    SpawnTrialEnemies(g, 2, "hell_drone", 9.0f);
                    AddTrialEnemies(g, 2, "imp", 7.5f);
                    _auxTimer = _spawnedGuards.Count;
                    g.ShowBanner("КИБЕР-ГРАЙНД [ВОЛНА 1/3]: ЗАЧИСТИТЕ АДСКИХ ДРОНОВ И БЕСОВ ДО ПОТЕРИ ТОПЛИВА КРОВИ!", Color.Red, 3.6f);
                    break;

                case "cheburashka":
                    _state = 1;
                    _stepCount = 0;
                    _timer = 22.0f;
                    ClearSecondaryNodes();
                    SoundManager.Play("chest_open", 1.05f, 1.1f);
                    new VisualFX(Position + new Vector3(0, 1.4f, 0), Color.Orange, 6.5f, 0.38f);
                    for (int i = 0; i < 5; i++)
                    {
                        float ang = (i / 5.0f) * MathF.PI * 2f + 0.25f;
                        Vector3 op = Position + new Vector3(MathF.Cos(ang) * 9.5f, 0, MathF.Sin(ang) * 9.5f);
                        op = Physics.FindSafeSpawnPoint(op);
                        var orange = new QuestSecondaryNode(op, this, i, $"[E] Золотой Апельсин #{i + 1}/5", Color.Orange, QuestNodeMode.GoldenOrange);
                        _secondaryNodes.Add(orange);
                        g.StageEntities.Add(orange);
                    }
                    SpawnTrialEnemies(g, 3, "combine_soldier", 10.5f);
                    g.ShowBanner("ЯЩИК ВСКРЫТ! СОБЕРИТЕ [E] ВСЕ 5 ЗОЛОТЫХ АПЕЛЬСИНОВ ПОД ОБСТРЕЛОМ НАЛЁТЧИКОВ!", Color.Orange, 3.5f);
                    break;

                case "artificer":
                    if (_paidOnce || g.LunarCoins >= 5)
                    {
                        if (!_paidOnce)
                        {
                            g.LunarCoins -= 5;
                            _paidOnce = true;
                        }
                        _state = 1;
                        SoundManager.Play("ice_freeze", 1.1f, 0.95f);
                        new VisualFX(Position + new Vector3(0, 1.8f, 0), Color.SkyBlue, 8.0f, 0.42f);
                        SpawnTrialEnemies(g, 2, "lunar_chimera", 9.0f, elite: true);
                        g.ShowBanner("ТЕМПОРАЛЬНАЯ ПЕЧАТЬ СНЯТА! ПЕРЕЖИВИТЕ ВСПЛЕСК ЛУННЫХ ХИМЕР!", Color.SkyBlue, 3.4f);
                    }
                    else
                    {
                        SoundManager.Play("ui_error");
                        g.ShowBanner("НУЖНО 5 ЛУННЫХ МОНЕТ ДЛЯ РАЗРУШЕНИЯ ТЕМПОРАЛЬНОЙ ПЕЧАТИ!", Color.SkyBlue, 2.2f);
                    }
                    break;

                case "buzz":
                    _state = 1;
                    _stepCount = 0;
                    _peakAirY = g.PlayerPos.Y;
                    SoundManager.Play("shrine_use");
                    Vector3 buzzPadPos = Physics.FindSafeSpawnPoint(new Vector3(Position.X + 4.5f, Position.Y, Position.Z), 6f, 1.6f, 2.0f);
                    g.StageEntities.Add(new LunarBouncePad(buzzPadPos, 23.5f, 0f));
                    _labelEnt.BillboardText = "Взлетите на гейзере (>12м) и приземлитесь сверху на Звездолёт XP-38!";
                    g.ShowBanner("МАЯК XP-38 АКТИВИРОВАН! ВЗЛЕТИТЕ НА ГЕЙЗЕРЕ И УПАДИТЕ СВЕРХУ НА КОРАБЛЬ!", Color.Lime, 3.2f);
                    break;

                case "enforcer":
                    _state = 1;
                    _progress = 0f;
                    _anchorPlayerPos = g.PlayerPos;
                    if (_zoneRing != null) _zoneRing.Enabled = true;
                    SoundManager.Play("shrine_use");
                    SpawnTrialEnemies(g, 3, "combine_soldier", 9.5f);
                    g.ShowBanner("НИ ШАГУ НАЗАД! СТОЙТЕ НА ПОСТУ 10 СЕКУНД ПОД ОГНЁМ ШТУРМОВИКОВ АЛЬЯНСА!", Color.SkyBlue, 3.4f);
                    break;

                case "scout":
                    _state = 1;
                    _stepCount = 0;
                    _auxTimer = 0.85f; // Короткий иммунитет отрыва от земли на старте
                    ClearSecondaryNodes();
                    SoundManager.Play("bounce_pad");
                    g.PlayerVelY = 18.5f;
                    g.PlayerGrounded = false;
                    g.PlayerJumpsLeft = Math.Max(1, g.PlayerMaxJumps);
                    Vector3 baseDir = g.PlayerForward.LengthSquared() > 0.01f ? g.PlayerForward : Vector3.UnitZ;
                    Vector3 rightDir = new(-baseDir.Z, 0, baseDir.X);
                    Vector3[] ringOffsets =
                    {
                        baseDir * 10.0f + new Vector3(0, 6.0f, 0),
                        baseDir * 19.5f + rightDir * 5.0f + new Vector3(0, 9.0f, 0),
                        baseDir * 29.0f - rightDir * 3.5f + new Vector3(0, 7.5f, 0),
                        baseDir * 38.5f + rightDir * 2.0f + new Vector3(0, 8.5f, 0)
                    };
                    for (int i = 0; i < 4; i++)
                    {
                        var rNode = new QuestSecondaryNode(Position + ringOffsets[i], this, i, $"[КОЛЬЦО #{i + 1}/4]", Color.SkyBlue, QuestNodeMode.AerialRing);
                        _secondaryNodes.Add(rNode);
                        g.StageEntities.Add(rNode);
                    }
                    SpawnTrialEnemies(g, 2, "combine_soldier", 11.0f);
                    g.ShowBanner("ВОЗДУШНЫЙ ПАРКУР: ПРОЛЕТИТЕ СКВОЗЬ 4 КОЛЬЦА В НЕБЕ, НЕ КАСАЯСЬ ЗЕМЛИ!", Color.SkyBlue, 3.2f);
                    break;

                case "m4a1":
                    _state = 1;
                    _stepCount = 0;
                    ClearSecondaryNodes();
                    GenerateM4A1Cipher();
                    UpdateM4A1CipherLabel();
                    Color[] rCols = { Color.Red, Color.Lime, Color.SkyBlue };
                    string[] rNames = { "[E] КРАСНЫЙ РЕТРАНСЛЯТОР", "[E] ЗЕЛЁНЫЙ РЕТРАНСЛЯТОР", "[E] СИНИЙ РЕТРАНСЛЯТОР" };
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = (i - 1) * 1.1f;
                        Vector3 rp = Position + new Vector3(MathF.Sin(ang) * 7.5f, 0, MathF.Cos(ang) * 7.5f);
                        rp = Physics.FindSafeSpawnPoint(rp);
                        var node = new QuestSecondaryNode(rp, this, i, rNames[i], rCols[i], QuestNodeMode.RelayButton);
                        _secondaryNodes.Add(node);
                        g.StageEntities.Add(node);
                    }
                    SpawnTrialEnemies(g, 2, "hell_drone", 9.5f);
                    SoundManager.Play("ui_ready");
                    g.ShowBanner("ПРОЧИТАЙТЕ ЦВЕТОВОЙ ШИФР НА ТЕРМИНАЛЕ И ВВЕДИТЕ ЕГО [E] НА 3 РЕТРАНСЛЯТОРАХ!", Color.Lime, 3.2f);
                    break;

                case "medic":
                    _state = 1;
                    _timer = 10.0f;
                    g.PlayerHp = 1.0f;
                    if (Entities.CombatManager.Controller != null)
                        Entities.CombatManager.Controller.Shield = 0f;
                    SoundManager.Play("player_hurt", 1.0f, 0.9f);
                    SpawnTrialEnemies(g, 3, "imp", 8.5f);
                    g.ShowBanner("ЭКСТРЕННАЯ ОПЕРАЦИЯ! ВАШЕ ЗДОРОВЬЕ СНИЖЕНО ДО 1 HP — ВЫЖИВИТЕ 10 СЕКУНД!", Color.Red, 3.5f);
                    break;

                case "aigis":
                    _state = 1;
                    SoundManager.Play("boss_roar", 1.1f, 0.85f);
                    SpawnTrialBoss(g, "shadow_reaper", "ЖНЕЦ ТАРТАРА");
                    AddTrialEnemies(g, 2, "hollow_shadow", 8.0f);
                    g.ShowBanner("ТЁМНЫЙ ЧАС ТАРТАРА! ЗАЧИСТИТЕ АРКАННЫХ ТЕНЕЙ И ПОБЕДИТЕ БОССА — ЖНЕЦА ТАРТАРА!", Color.Gold, 3.6f);
                    break;

                case "reimu":
                    _state = 1;
                    _timer = 12.0f;
                    _auxTimer = 0f;
                    _lastPlayerTotalHp = g.PlayerHp + curShield;
                    if (_zoneRing != null) _zoneRing.Enabled = true;
                    SoundManager.Play("magic_cast", 1.0f, 1.1f);
                    g.ShowBanner("ИСПЫТАНИЕ ДАНМАКУ: УКЛОНЯЙТЕСЬ ОТ ПУЛЬ 12 СЕКУНД БЕЗ ЕДИНОГО ПОПАДАНИЯ!", Color.Red, 3.5f);
                    break;

                case "mindustry":
                    _state = 1;
                    _progress = 0f;
                    _stepCount = 0;
                    _timer = 350f; // Здоровье бура = 350 HP
                    _auxTimer = 0.4f;
                    ClearSecondaryNodes();
                    if (_zoneRing != null) _zoneRing.Enabled = true;
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = (i / 3.0f) * MathF.PI * 2f + 0.5f;
                        Vector3 cp = Position + new Vector3(MathF.Cos(ang) * 9.0f, 0, MathF.Sin(ang) * 9.0f);
                        cp = Physics.FindSafeSpawnPoint(cp);
                        var cell = new QuestSecondaryNode(cp, this, i, "[ЭНЕРГОЯЧЕЙКА БУРА (+18%)]", Color.SkyBlue, QuestNodeMode.Touch);
                        _secondaryNodes.Add(cell);
                        g.StageEntities.Add(cell);
                    }
                    SoundManager.Play("build_place", 1.0f, 0.9f);
                    g.ShowBanner("ОБОРОНА БУРА (350 HP): СОБИРАЙТЕ ЭНЕРГОЯЧЕЙКИ И ОТРАЗИТЕ ВОЛНЫ ЖУКОВ И ГОЛЕМОВ!", Color.Gold, 3.5f);
                    break;

                case "slark":
                    SoundManager.Play("ui_error");
                    g.ShowBanner("КЛЕТКА ЗАПЕРТА! РАЗБЕЙТЕ ЕЁ ЦЕПИ СВОИМИ АТАКАМИ И ОРУЖИЕМ!", _themeCol, 2.5f);
                    break;

                case "tidehunter":
                    string? sacrificeKey = null;
                    if (!_paidOnce)
                    {
                        foreach (var kv in g.Items)
                        {
                            if (kv.Value > 0 && kv.Key != "tonic_affliction")
                            {
                                sacrificeKey = kv.Key;
                                break;
                            }
                        }
                    }
                    if (_paidOnce || sacrificeKey != null)
                    {
                        string itemName = "Дар Левиафана";
                        if (!_paidOnce && sacrificeKey != null)
                        {
                            g.Items[sacrificeKey]--;
                            if (g.Items[sacrificeKey] <= 0) g.ItemOrder.Remove(sacrificeKey);
                            itemName = GameData.ItemPool.TryGetValue(sacrificeKey, out var inf) ? inf.Name : sacrificeKey;
                            _paidOnce = true;
                        }
                        _state = 1;
                        SoundManager.Play("slam_land", 1.15f, 0.82f);
                        new VisualFX(Position + new Vector3(0, 1.8f, 0), Rgb(65, 235, 195), 14.0f, 0.50f);
                        SpawnTrialEnemies(g, 2, "jellyfish", 8.5f);
                        AddTrialEnemies(g, 1, "golem", 9.5f, elite: true, hpMultiplier: 1.5f);
                        g.ShowBanner($"БЕЗДНА ПРИНЯЛА «{itemName}»! СОКРУШИТЕ ПРОБУДИВШИХСЯ СТРАЖЕЙ ОКЕАНСКОЙ ВПАДИНЫ!", Rgb(75, 245, 200), 3.5f);
                    }
                    else
                    {
                        SoundManager.Play("ui_error");
                        g.ShowBanner("ДЛЯ ПОДНОШЕНИЯ ЛЕВИАФАНУ НУЖЕН ХОТЯ БЫ 1 ПРЕДМЕТ В ИНВЕНТАРЕ!", Color.Red, 2.5f);
                    }
                    break;

                case "freeman":
                    _state = 1;
                    _stepCount = 0;
                    ClearSecondaryNodes();
                    Vector3 cartDir = Position.Length() > 5f ? Vector3.Normalize(new Vector3(-Position.X, 0, -Position.Z)) : Vector3.UnitZ;
                    Vector3 cartStart = Position + cartDir * 14.0f;
                    cartStart = Physics.FindSafeSpawnPoint(cartStart);
                    var cart = new QuestSecondaryNode(cartStart, this, 0, "[ТОЛКАЙТЕ ТЕЛЕЖКУ К ЛУЧУ]", Color.Orange, QuestNodeMode.PushCart);
                    _secondaryNodes.Add(cart);
                    g.StageEntities.Add(cart);
                    SoundManager.Play("shrine_use");
                    _labelEnt.BillboardText = "Закатите тележку с кристаллом GG-3883 в луч Спектрометра!";
                    g.ShowBanner("ПОДОЙДИТЕ К ТЕЛЕЖКЕ С КРИСТАЛЛОМ GG-3883 И ЗАКАТИТЕ ЕЁ В ЛУЧ СПЕКТРОМЕТРА!", Color.Orange, 3.2f);
                    break;

                case "dante":
                    if (_state == 0)
                    {
                        _state = 1;
                        _stepCount = 0;
                        _progress = 6.0f;
                        SoundManager.Play("dante_clock", 1.0f, 1.0f);
                        SpawnTrialEnemies(g, 2, "hollow_shadow", 9.0f);
                        g.ShowBanner("ЧАСЫ ЗАПУЩЕНЫ! ЖМИТЕ [E] РОВНО НА 12:00 ПОЛНОЧЬ (3 РАЗА), ОТБИВАЯСЬ ОТ ТЕНЕЙ!", Color.Gold, 3.4f);
                    }
                    break;

                case "ahri":
                    _state = 1;
                    _stepCount = 0;
                    ClearSecondaryNodes();
                    ClearSpawnedGuards(g);
                    SoundManager.Play("charm_heart", 1.0f, 1.15f);
                    Vector3 foxPos = Position + new Vector3(8.0f, 0f, 8.0f);
                    foxPos = Physics.FindSafeSpawnPoint(foxPos);
                    var fox = new QuestSecondaryNode(foxPos, this, 0, "[ДОГОНИТЕ ДУХ ЛИСЁНКА (0/4)]", Rgb(255, 115, 205), QuestNodeMode.FleeingFox);
                    _secondaryNodes.Add(fox);
                    g.StageEntities.Add(fox);
                    _labelEnt.BillboardText = "Догоните и коснитесь убегающего Призрачного Лисёнка (4 раза)!";
                    g.ShowBanner("ПРИЗРАЧНЫЙ ЛИСЁНОК УБЕГАЕТ! ДОГОНИТЕ ЕГО НА СПРИНТЕ И РЫВКАХ 4 РАЗА!", Rgb(255, 115, 205), 3.2f);
                    break;

                case "lappland":
                    _state = 1;
                    _timer = 16.0f;
                    SoundManager.Play("boss_roar", 0.95f, 1.2f);
                    SpawnTrialEnemies(g, 3, "sarkaz_mercenary", 8.0f, elite: true);
                    AddTrialEnemies(g, 2, "lemurian", 7.0f);
                    g.ShowBanner("КРОВАВАЯ ВЕНДЕТТА СИРАКУЗ: СОКРУШИТЕ ОТРЯД НАЁМНИКОВ САРКАЗОВ ЗА 16 СЕКУНД!", _themeCol, 3.4f);
                    break;

                case "w_arknights":
                    _state = 1;
                    _stepCount = 0;
                    _timer = 16.0f;
                    ClearSecondaryNodes();
                    SoundManager.Play("ui_tick", 1.0f, 1.2f);
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = i * (MathF.PI * 0.5f) + 0.4f;
                        Vector3 bp = Position + new Vector3(MathF.Cos(ang) * 13.0f, 0, MathF.Sin(ang) * 13.0f);
                        bp = Physics.FindSafeSpawnPoint(bp);
                        var bNode = new QuestSecondaryNode(bp, this, i, $"[E] Обезвредить Бомбу C4 #{i + 1}", Color.Red, QuestNodeMode.BombDefuse);
                        _secondaryNodes.Add(bNode);
                        g.StageEntities.Add(bNode);
                    }
                    SpawnTrialEnemies(g, 2, "sarkaz_mercenary", 9.0f);
                    g.ShowBanner("ВЗВЕДЕНЫ 4 БОМБЫ C4 (16 СЕК)! ОБЕЗВРЕДЬТЕ ВСЕ 4 [E] ПОД ОГНЁМ САРКАЗОВ!", Color.Red, 3.5f);
                    break;

                case "gold_ship":
                    _state = 1;
                    _stepCount = 0;
                    _timer = 20.0f;
                    ClearSecondaryNodes();
                    SoundManager.Play("ui_ready", 1.0f, 1.2f);
                    Vector3 raceDir = Position.Length() > 5f ? Vector3.Normalize(new Vector3(-Position.X, 0, -Position.Z)) : Vector3.UnitX;
                    Vector3 sideDir = new(-raceDir.Z, 0, raceDir.X);
                    for (int i = 0; i < 5; i++)
                    {
                        float d = (i + 1) * 11.5f;
                        float curve = MathF.Sin((i + 1) * 1.05f) * 7.5f;
                        Vector3 gp = Position + raceDir * d + sideDir * curve;
                        gp = Physics.FindSafeSpawnPoint(gp);
                        string gLbl = i == 4 ? "ФИНИШНАЯ АРКА #5" : $"ВОРОТА #{i + 1}/5";
                        var gate = new QuestSecondaryNode(gp, this, i, gLbl, Color.Gold, QuestNodeMode.RaceGate);
                        _secondaryNodes.Add(gate);
                        g.StageEntities.Add(gate);
                    }
                    g.ApplyDash(g.PlayerForward * 22.0f, 0.25f);
                    g.ShowBanner("ГРАН-ПРИ АРИМА КИНЭН! ПРОМЧИТЕСЬ ЧЕРЕЗ ВСЕ 5 ВОРОТ ЗА 20 СЕКУНД!", Color.Gold, 3.4f);
                    break;

                case "pushnoy":
                    g.ShowBanner("АТАКУЙТЕ ГЕНЕРАТОР ОРУЖИЕМ, ЧТОБЫ НАБРАТЬ 10 000 ВОЛЬТ!", Color.Orange, 2.5f);
                    break;

                case "freddy":
                    _state = 1;
                    _stepCount = 0;
                    _timer = 0f;       // Часы от 0 до 18 сек (12 AM -> 6 AM)
                    _progress = 100f;  // Завод шкатулки 100%
                    _auxTimer = 2.0f;
                    SoundManager.Play("dante_clock", 1.0f, 1.15f);
                    SpawnTrialEnemies(g, 2, "beetle", 8.0f);
                    g.ShowBanner("НОЧНАЯ СМЕНА (12:00 AM -> 06:00 AM): ЖМИТЕ [E] У ШКАТУЛКИ И ОДОЛЕЙТЕ ЗОЛОТОГО ЭНДОСКЕЛЕТА!", Color.Gold, 3.8f);
                    break;

                case "steve":
                    if (_stepCount == 3)
                    {
                        _state = 1;
                        _stepCount = 4;
                        SoundManager.Play("portal_open", 1.0f, 1.1f);
                        SpawnTrialEnemies(g, 3, "nether_blaze", 8.5f);
                        g.ShowBanner("ПОРТАЛ НЕЗЕРА ЗАЖЖЁН! ОТРАЗИТЕ ВТОРЖЕНИЕ РОЯ ИФРИТОВ НЕЗЕРА!", Color.Orange, 3.5f);
                    }
                    else if (_stepCount < 3)
                    {
                        _state = 1;
                        _stepCount = 0;
                        ClearSecondaryNodes();
                        SoundManager.Play("build_place", 1.0f, 0.9f);
                        for (int i = 0; i < 3; i++)
                        {
                            float ang = i * (MathF.PI * 2f / 3f);
                            Vector3 mp = Position + new Vector3(MathF.Cos(ang) * 7.5f, 0, MathF.Sin(ang) * 7.5f);
                            mp = Physics.FindSafeSpawnPoint(mp);
                            var block = new QuestSecondaryNode(mp, this, i, "Обсидиановый Монолит [350 HP]\n[Разбейте атаками!]", Rgb(185, 75, 255), QuestNodeMode.DestructibleBlock);
                            _secondaryNodes.Add(block);
                            g.StageEntities.Add(block);
                        }
                        _labelEnt.BillboardText = "Разбейте атаками все 3 Обсидиановых Монолита вокруг рамки!";
                        g.ShowBanner("РАЗБЕЙТЕ СВОИМИ АТАКАМИ 3 ОБСИДИАНОВЫХ МОНОЛИТА И ЗАЖГИТЕ ПОРТАЛ [E]!", Rgb(195, 85, 255), 3.2f);
                    }
                    break;

                case "jotaro":
                    _state = 1;
                    _progress = 0f;
                    if (_zoneRing != null) _zoneRing.Enabled = true;
                    SoundManager.Play("dante_clock", 1.0f, 0.9f);
                    g.ShowBanner("УРАГАН СТРЕЛЫ ОТТАЛКИВАЕТ ВАС! ИДИТЕ И ДЕЛАЙТЕ РЫВКИ К МЕТЕОРИТУ 7 СЕКУНД!", Color.Gold, 3.5f);
                    break;

                case "vyper":
                    _state = 1;
                    _stepCount = 0;
                    _timer = 20.0f;
                    _auxTimer = 1.6f;
                    ClearSecondaryNodes();
                    SoundManager.Play("shrine_use");
                    for (int i = 0; i < 4; i++)
                    {
                        var lockNode = new QuestSecondaryNode(Position + new Vector3(5f, 2f, 0), this, i, $"[МАГНИТНЫЙ ЗАМОК #{i + 1}/4]", Color.Lime, QuestNodeMode.OrbitLock);
                        _secondaryNodes.Add(lockNode);
                        g.StageEntities.Add(lockNode);
                    }
                    g.ShowBanner("СБЕЙТЕ ВЫСТРЕЛАМИ ВСЕ 4 ВРАЩАЮЩИХСЯ ЗАМКА ЗА 20 СЕКУНД ПОД ЛАЗЕРАМИ ОХРАНЫ!", Color.Lime, 3.4f);
                    break;

                case "haze":
                    _state = 1;
                    _stepCount = 0;
                    SoundManager.Play("magic_cast");
                    SpawnTrialEnemies(g, 1, "hollow_shadow", 9.5f, elite: true);
                    SpawnHazeMirageRound(g);
                    g.ShowBanner("ПРОТОКОЛ СНОВИДЦА: НАЙДИТЕ СТАБИЛЬНУЮ ЗОЛОТУЮ СФЕРУ (3 РАЗА) И УСТРАНИТЕ ТЕНЬ!", Color.Gold, 3.5f);
                    break;

                case "dmc_trio":
                    _state = 1;
                    _stepCount = 0;
                    _progress = 0f;
                    _timer = 45.0f;
                    _startKills = Program.RunKills;
                    _startDamageDealt = Program.RunDamageDealt;
                    _lastPlayerTotalHp = g.PlayerHp + curShield;
                    SoundManager.Play("boss_roar", 1.05f, 1.0f);
                    SpawnTrialBoss(g, "vergil_mirage", "МИРАЖ ВЕРГИЛИЯ");
                    AddTrialEnemies(g, 2, "imp", 7.5f);
                    g.ShowBanner("ВЕРШИНА КЛИПОТА: НАБЕРИТЕ РАНГ СТИЛЯ SSS И ПОБЕДИТЕ В ДУЭЛИ МИРАЖА ВЕРГИЛИЯ!", Color.Red, 3.8f);
                    break;

                case "jack_o":
                    _state = 1;
                    _subPhase = 0;
                    _progress = 0f;
                    if (_zoneRing != null) _zoneRing.Enabled = true;
                    SoundManager.Play("magic_cast", 1.0f, 1.15f);
                    SpawnTrialEnemies(g, 3, "imp", 8.5f);
                    AddTrialEnemies(g, 1, "hollow_shadow", 8.5f);
                    g.ShowBanner("КОНФЕТНЫЙ ЛЕГИОН ЭЛИЗИОНА: УДЕРЖИВАЙТЕ ЗОНУ ЗНАМЕНИ 14 СЕК И ОТРАЗИТЕ ЗАДНИЙ ДВОР!", Rgb(155, 255, 75), 3.6f);
                    break;
            }
        }

        private void AddTrialEnemies(IPlayerContext g, int count, string enemyType, float radius = 7.5f, bool elite = false, float hpMultiplier = 1.0f)
        {
            if (g is not GameRunContext run) return;
            for (int i = 0; i < count; i++)
            {
                float ang = (i / (float)Math.Max(1, count)) * MathF.PI * 2f + _spawnedGuards.Count * 0.65f;
                Vector3 sp = Position + new Vector3(MathF.Cos(ang) * radius, 0, MathF.Sin(ang) * radius);
                // Стража испытания появляется только на свободной земле — не внутри скал, колонн, стен и мебели
                Vector3 mobPos = Physics.FindFreeActorPos(new Vector3(sp.X, Position.Y, sp.Z), 0.9f, 10f, 5f);
                var mob = new Entities.CombatTarget(mobPos, enemyType, run.DifficultyCoeff * 0.92f);
                if (elite) mob.IsElite = true;
                if (hpMultiplier > 1.01f)
                {
                    mob.MaxHp *= hpMultiplier;
                    mob.Hp = mob.MaxHp;
                    mob.Scale = new Vector3(1.30f);
                    mob.UpdateHpBillboard();
                }
                Entities.CombatManager.Enemies.Add(mob);
                g.StageEntities.Add(mob);
                _spawnedGuards.Add(mob);
            }
        }

        private void SpawnTrialEnemies(IPlayerContext g, int count, string enemyType, float radius = 7.5f, bool elite = false, float hpMultiplier = 1.0f)
        {
            ClearSpawnedGuards(g);
            AddTrialEnemies(g, count, enemyType, radius, elite, hpMultiplier);
        }

        private Entities.CombatTarget? SpawnTrialBoss(IPlayerContext g, string bossType, string fallbackTitle)
        {
            if (g is not GameRunContext run) return null;
            ClearSpawnedGuards(g);
            Vector3 toCenter = Position.Length() > 5f ? Vector3.Normalize(new Vector3(-Position.X, 0, -Position.Z)) : Vector3.UnitZ;
            Vector3 sp = Position + toCenter * 9.0f;
            // Босс испытания (крупный) появляется в свободной точке, а не внутри структуры или стены
            Vector3 bossPos = Physics.FindFreeActorPos(new Vector3(sp.X, Position.Y, sp.Z), 2.0f, 14f, 6f);
            var boss = new Entities.CombatTarget(bossPos, bossType, run.DifficultyCoeff * 0.90f);
            Entities.CombatManager.Enemies.Add(boss);
            g.StageEntities.Add(boss);
            _spawnedGuards.Add(boss);
            if (!run.IsBossAlive)
            {
                run.ActiveBoss = boss;
                run.BossTitle = GameData.BossTitles.TryGetValue(bossType, out var bt) ? bt : fallbackTitle;
            }
            return boss;
        }

        private void UpdateActiveTrial(IPlayerContext g, float dt, float dist)
        {
            float curShield = Entities.CombatManager.Controller?.Shield ?? 0f;
            float curTotalHp = g.PlayerHp + curShield;

            switch (CharKey)
            {
                case "commando":
                    _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                    if (dist <= 9.8f)
                        _progress = MathF.Min(100f, _progress + (100f / 10.0f) * dt);
                    else
                        _progress = MathF.Max(0f, _progress - 6.0f * dt);

                    if (_progress >= 50f && _stepCount == 0)
                    {
                        _stepCount = 1;
                        AddTrialEnemies(g, 2, "combine_soldier", 10.5f, elite: true);
                        g.ShowBanner("50% СИГНАЛА ЗАГРУЖЕНО! ВЫСАДКА ЭЛИТНОГО ОТРЯДА АЛЬЯНСА!", Color.Orange, 2.5f);
                    }
                    _labelEnt.BillboardText = dist <= 9.8f
                        ? $"Передача сигнала UES: {(int)_progress}%  |  Штурмовиков: {_spawnedGuards.Count}"
                        : $"ВЕРНИТЕСЬ В ПЕРИМЕТР МАЯКА! ({(int)_progress}%  |  Врагов: {_spawnedGuards.Count})";
                    if (_progress >= 100f && _spawnedGuards.Count == 0)
                        CompleteCharacterQuest(g);
                    break;

                case "huntress":
                    if (_stepCount < 4)
                    {
                        if (Vector3.Distance(g.PlayerPos, _anchorPlayerPos) > 0.10f || !g.PlayerGrounded)
                        {
                            _anchorPlayerPos = g.PlayerPos;
                            _auxTimer = 0f;
                        }
                        else
                        {
                            _auxTimer += dt;
                        }

                        if (_auxTimer > 1.25f && _stepCount > 0)
                        {
                            _stepCount = 0;
                            _auxTimer = 0f;
                            RespawnHuntressFireflies(g);
                            SoundManager.Play("ui_error");
                            g.ShowBanner("ВЫ ОСТАНОВИЛИСЬ! АСТРАЛЬНЫЕ СВЕТЛЯЧКИ ВОЗРОДИЛИСЬ (0/4)!", Color.Red, 2.2f);
                        }
                        _labelEnt.BillboardText = $"Светлячки: {_stepCount}/4  |  Двигайтесь! (Стоп: {_auxTimer:F1}/1.2с)";
                    }
                    else if (_stepCount >= 5)
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Сразите Вожака Полнолуния! Осталось врагов: {_spawnedGuards.Count}";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "sol":
                case "artificer":
                case "slark":
                case "tidehunter":
                case "aigis":
                    _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                    if (_spawnedGuards.Count > 0)
                    {
                        var lead = _spawnedGuards[0];
                        _labelEnt.BillboardText = $"Сокрушите стража испытания: [{(int)MathF.Max(1f, lead.Hp)} HP] (Врагов: {_spawnedGuards.Count})!";
                    }
                    else
                    {
                        CompleteCharacterQuest(g);
                    }
                    break;

                case "gwen":
                    if (_stepCount < 4)
                    {
                        _labelEnt.BillboardText = $"Перережьте Нити Чёрного Тумана [E]: {_stepCount}/4";
                    }
                    else if (_stepCount >= 5)
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Изгоните Теневого Жнеца Погибели [{(int)MathF.Max(1f, _spawnedGuards[0].Hp)} HP]!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "v1":
                    _timer -= dt;
                    _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                    int prevAlive = (int)MathF.Round(_auxTimer);
                    if (_spawnedGuards.Count < prevAlive)
                    {
                        int killedNow = prevAlive - _spawnedGuards.Count;
                        _timer = MathF.Min(20.0f, _timer + 4.5f * killedNow);
                        new VisualFX(g.PlayerPos + new Vector3(0, 1.2f, 0), Color.Red, 2.2f, 0.22f);
                    }
                    _auxTimer = _spawnedGuards.Count;

                    if (_spawnedGuards.Count == 0)
                    {
                        if (_stepCount < 3)
                        {
                            _stepCount++;
                            _timer = 18.0f;
                            SoundManager.Play("coin_ricoshot", 1.0f, 1.0f + _stepCount * 0.08f);
                            AddTrialEnemies(g, 3, "hell_drone", 9.5f);
                            AddTrialEnemies(g, 2, "imp", 8.0f, elite: _stepCount == 3);
                            _auxTimer = _spawnedGuards.Count;
                            g.ShowBanner($"КИБЕР-ГРАЙНД [ВОЛНА {_stepCount}/3]: ТОПЛИВО ПОПОЛНЕНО КРОВЬЮ!", Color.Red, 2.6f);
                        }
                        else
                        {
                            CompleteCharacterQuest(g);
                            return;
                        }
                    }
                    _labelEnt.BillboardText = $"КИБЕР-ГРАЙНД [Волна {_stepCount}/3]  |  Топливо крови: {_timer:F1}с  |  Врагов: {_spawnedGuards.Count}";
                    if (_timer <= 0f)
                    {
                        _state = 0;
                        ClearSpawnedGuards(g);
                        SoundManager.Play("ui_error");
                        _labelEnt.BillboardText = "[E] Терминал Кибер-Грайнда (Топливо иссякло! Нажмите [E] снова)";
                        g.ShowBanner("ТОПЛИВО КРОВИ ИССЯКЛО! ТЕРМИНАЛ КИБЕР-ГРАЙНДА ПЕРЕЗАГРУЖЕН!", Color.Red, 2.5f);
                    }
                    break;

                case "cheburashka":
                    _timer -= dt;
                    _labelEnt.BillboardText = $"Золотые Апельсины [E]: {_stepCount}/5  |  Осталось {_timer:F1} сек!";
                    if (_timer <= 0f)
                    {
                        _state = 0;
                        ClearSecondaryNodes();
                        ClearSpawnedGuards(g);
                        SoundManager.Play("ui_error");
                        _labelEnt.BillboardText = "[E] Ящик «Апельсины» (Время вышло! Нажмите [E] или ударьте сверху)";
                        g.ShowBanner("НАЛЁТЧИКИ РАСТАЩИЛИ АПЕЛЬСИНЫ! ПОПРОБУЙТЕ СНОВА!", Color.Orange, 2.4f);
                    }
                    break;

                case "buzz":
                    if (_stepCount == 0)
                    {
                        if (!g.PlayerGrounded)
                        {
                            if (g.PlayerPos.Y > _peakAirY) _peakAirY = g.PlayerPos.Y;
                            float curGain = _peakAirY - Y;
                            _labelEnt.BillboardText = $"Набрана высота: {curGain:F1} / 12.0м — Приземлитесь на корабль XP-38!";
                        }
                        else
                        {
                            float fallDrop = _peakAirY - Y;
                            if (fallDrop >= 12.0f && Physics.Distance2D(Position, g.PlayerPos) <= 4.8f)
                            {
                                _stepCount = 1;
                                SoundManager.Play("slam_land", 1.2f, 0.9f);
                                SoundManager.Play("boss_roar", 1.1f, 0.88f);
                                new VisualFX(Position + new Vector3(0, 1.5f, 0), Color.Lime, 10.0f, 0.45f);
                                SpawnTrialBoss(g, "zurg_mech", "ДРЕДНОУТ ИМПЕРИИ ЗУРГА");
                                g.ShowBanner("ПОСАДКА УСПЕШНА! НА ПЕРЕХВАТ ВЫШЕЛ БОСС — ДРЕДНОУТ ИМПЕРАТОРА ЗУРГА!", Color.Lime, 3.6f);
                                return;
                            }
                            _peakAirY = g.PlayerPos.Y;
                        }
                    }
                    else
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Одолейте Дредноут Императора Зурга [{(int)MathF.Max(1f, _spawnedGuards[0].Hp)} HP]!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "enforcer":
                    if (Vector3.Distance(g.PlayerPos, _anchorPlayerPos) > 0.75f || !g.PlayerGrounded)
                    {
                        _progress = 0f;
                        _anchorPlayerPos = g.PlayerPos;
                        SoundManager.Play("ui_error", 0.6f);
                        _labelEnt.BillboardText = "ВЫ СДВИНУЛИСЬ! Стойте неподвижно 10 сек (0%)!";
                    }
                    else
                    {
                        _progress += (100f / 10.0f) * dt;
                        _labelEnt.BillboardText = $"Неподвижная оборона поста: {(int)Math.Min(100f, _progress)}%";
                        if (_progress >= 100f)
                        {
                            ClearSpawnedGuards(g);
                            CompleteCharacterQuest(g);
                        }
                    }
                    break;

                case "scout":
                    _auxTimer = MathF.Max(0f, _auxTimer - dt);
                    _labelEnt.BillboardText = $"Воздушные кольца: {_stepCount}/4 (Не касайтесь земли!)";
                    if (_auxTimer <= 0f && g.PlayerGrounded)
                    {
                        _state = 0;
                        ClearSecondaryNodes();
                        ClearSpawnedGuards(g);
                        SoundManager.Play("ui_error");
                        _labelEnt.BillboardText = "[E] Вы коснулись земли! Нажмите [E], чтобы начать полёт заново";
                        g.ShowBanner("ВЫ КОСНУЛИСЬ ЗЕМЛИ! ПАРКУР РАЗВЕДЧИКА СБРОШЕН!", Color.Red, 2.2f);
                    }
                    break;

                case "medic":
                    if (g.PlayerHp > 1.0f) g.PlayerHp = 1.0f;
                    if (Entities.CombatManager.Controller != null)
                        Entities.CombatManager.Controller.Shield = 0f;
                    _timer -= dt;
                    _labelEnt.BillboardText = $"Операция на 1 HP: продержитесь ещё {_timer:F1} сек!";
                    if (_timer <= 0f && g.PlayerHp > 0f)
                    {
                        ClearSpawnedGuards(g);
                        g.HealPlayer(g.PlayerMaxHp);
                        if (Entities.CombatManager.Controller != null)
                            Entities.CombatManager.Controller.Shield = 60f;
                        CompleteCharacterQuest(g);
                    }
                    break;

                case "reimu":
                    if (curTotalHp < _lastPlayerTotalHp - 0.5f || dist > 14.5f)
                    {
                        _state = 0;
                        if (_zoneRing != null) _zoneRing.Enabled = false;
                        SoundManager.Play("ui_error");
                        _labelEnt.BillboardText = "[E] Обряд Данмаку прерван попаданием! Нажмите [E] снова";
                        g.ShowBanner(dist > 14.5f ? "ВЫ ПОКИНУЛИ КРУГ СВЯТИЛИЩА!" : "ПОПАДАНИЕ! ИСПЫТАНИЕ ДАНМАКУ СБРОШЕНО!", Color.Red, 2.4f);
                        return;
                    }
                    _lastPlayerTotalHp = curTotalHp;
                    _timer -= dt;
                    _auxTimer -= dt;
                    if (_auxTimer <= 0f)
                    {
                        _auxTimer = 0.72f;
                        SoundManager.Play("magic_cast", 0.65f, 1.2f);
                        float offsetAng = _timer * 2.1f;
                        for (int i = 0; i < 10; i++)
                        {
                            float ang = offsetAng + i * (MathF.PI * 2f / 10f);
                            Vector3 dir = new(MathF.Cos(ang), 0f, MathF.Sin(ang));
                            new Entities.Projectile(Position + new Vector3(0, 1.35f, 0), dir, 13.5f, 8.0f, 0.36f,
                                i % 2 == 0 ? Color.Red : Color.Gold, isPlayer: false, canProc: false, maxLifetime: 2.2f);
                        }
                    }
                    _labelEnt.BillboardText = $"Испытание Данмаку (Без урона): осталось {_timer:F1} сек!";
                    if (_timer <= 0f)
                        CompleteCharacterQuest(g);
                    break;

                case "mindustry":
                    _auxTimer -= dt;
                    if (_auxTimer <= 0f)
                    {
                        _auxTimer = 4.4f;
                        if (g is GameRunContext mRun)
                        {
                            float ang = Random.Shared.NextSingle() * MathF.PI * 2f;
                            Vector3 sp = Position + new Vector3(MathF.Cos(ang) * 11f, 0, MathF.Sin(ang) * 11f);
                            sp = Physics.FindFreeActorPos(new Vector3(sp.X, Position.Y, sp.Z), 1.0f, 10f, 5f);
                            string mType = (_stepCount++ % 3 == 2) ? "golem" : "beetle";
                            var bug = new Entities.CombatTarget(sp, mType, mRun.DifficultyCoeff * 0.85f);
                            Entities.CombatManager.Enemies.Add(bug);
                            g.StageEntities.Add(bug);
                            _spawnedGuards.Add(bug);
                        }
                    }
                    foreach (var e in Entities.CombatManager.Enemies)
                    {
                        if (IsValid(e) && !e.IsAlly && !e.IsTrainingDummy && Vector3.Distance(e.Position, Position) <= 3.6f)
                        {
                            _timer -= 26f * dt; // Враги ломают бур
                            if ((int)((_timer + 26f * dt) / 25f) != (int)(_timer / 25f))
                                new VisualFX(Position + new Vector3(0, 1.4f, 0), Color.Red, 2.8f, 0.2f);
                        }
                    }
                    if (_timer <= 0f)
                    {
                        _state = 0;
                        if (_zoneRing != null) _zoneRing.Enabled = false;
                        ClearSecondaryNodes();
                        ClearSpawnedGuards(g);
                        SoundManager.Play("explosion");
                        _labelEnt.BillboardText = "[E] Бур разрушен врагами! Нажмите [E] для повторного запуска";
                        g.ShowBanner("ТОРИЕВЫЙ БУР РАЗРУШЕН ВРАГАМИ! ПОПРОБУЙТЕ СНОВА!", Color.Red, 2.5f);
                        return;
                    }
                    _progress += (100f / 16.0f) * dt;
                    _labelEnt.BillboardText = $"Бурение ядра: {(int)_progress}%  |  Прочность Бура: {(int)_timer}/350 HP";
                    if (_progress >= 100f)
                    {
                        ClearSpawnedGuards(g);
                        CompleteCharacterQuest(g);
                    }
                    break;

                case "freeman":
                    if (_stepCount >= 2)
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Каскадный Резонанс! Отразите десант Альянса: осталось {_spawnedGuards.Count}";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "dante":
                    _progress = (_progress + 9.2f * dt) % 12.0f;
                    int hour = ((int)MathF.Floor(_progress) % 12) + 1;
                    bool inMidnightZone = _progress >= 10.7f || _progress <= 1.3f;
                    _labelEnt.Color = inMidnightZone ? Color.Lime : Color.Gold;
                    _labelEnt.BillboardText = $"Стрелка Часов: [ {hour:00}:00 ] {(inMidnightZone ? "★ ЖМИТЕ [E] СЕЙЧАС! ★" : "(Ждите 12:00)")}\nТочных попаданий в Полночь: {_stepCount}/3";

                    if (dist < 4.8f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                    {
                        if (inMidnightZone)
                        {
                            _stepCount++;
                            SoundManager.Play("dante_clock", 1.1f, 0.9f + _stepCount * 0.12f);
                            new VisualFX(Position + new Vector3(0, 2.2f, 0), Color.Gold, 5.5f, 0.35f);
                            if (_stepCount >= 3)
                                CompleteCharacterQuest(g);
                            else
                                g.ShowBanner($"ТОЧНО В ПОЛНОЧЬ ({_stepCount}/3)!", Color.Gold, 1.5f);
                        }
                        else
                        {
                            _stepCount = Math.Max(0, _stepCount - 1);
                            SoundManager.Play("ui_error");
                            g.ShowBanner($"ПРОМАХ ({hour:00}:00)! ЖМИТЕ [E] ТОЛЬКО НА 12:00!", Color.Red, 1.8f);
                        }
                    }
                    break;

                case "ahri":
                    if (_stepCount >= 5)
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Победите Арканных Теней-похитителей: осталось {_spawnedGuards.Count}!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "lappland":
                    _timer -= dt;
                    _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                    _labelEnt.BillboardText = $"Вендетта Сиракуз: осталось {_spawnedGuards.Count}/5 врагов ({_timer:F1} сек)!";
                    if (_spawnedGuards.Count == 0)
                    {
                        CompleteCharacterQuest(g);
                    }
                    else if (_timer <= 0f)
                    {
                        _state = 0;
                        ClearSpawnedGuards(g);
                        SoundManager.Play("ui_error");
                        _labelEnt.BillboardText = "[E] Время истекло! Нажмите [E], чтобы начать охоту заново";
                        g.ShowBanner("16 СЕКУНД ИСТЕКЛИ! ВЫ НЕ УСПЕЛИ ДОБИТЬ ОТРЯД САРКАЗОВ!", Color.Red, 2.4f);
                    }
                    break;

                case "w_arknights":
                    _timer -= dt;
                    _labelEnt.BillboardText = $"Обезврежено бомб C4: {_stepCount}/4 — ВЗРЫВ ЧЕРЕЗ {_timer:F1} СЕК!";
                    if (_timer <= 0f)
                    {
                        _state = 0;
                        ClearSecondaryNodes();
                        ClearSpawnedGuards(g);
                        SoundManager.Play("explosion", 1.2f, 0.85f);
                        new VisualFX(Position + new Vector3(0, 1.5f, 0), Rgb(255, 65, 45), 14.0f, 0.50f);
                        if (dist <= 14.0f)
                            g.PlayerHp = MathF.Max(1f, g.PlayerHp - g.PlayerMaxHp * 0.25f);
                        _labelEnt.BillboardText = "[E] Склад C4 (Взрыв произошёл! Нажмите [E] для новой попытки)";
                        g.ShowBanner("БОМБЫ C4 СДЕТОНИРОВАЛИ! ВЫ НЕ УСПЕЛИ ОБЕЗВРЕДИТЬ ВСЕ 4 ЗАРЯДА!", Color.Red, 2.5f);
                    }
                    break;

                case "gold_ship":
                    _timer -= dt;
                    _labelEnt.BillboardText = $"Дерби Трейсен: Ворота {_stepCount}/5 ({_timer:F1} сек)";
                    if (_timer <= 0f)
                    {
                        _state = 0;
                        ClearSecondaryNodes();
                        SoundManager.Play("ui_error");
                        _labelEnt.BillboardText = "[E] Время гонки вышло! Нажмите [E] для нового старта";
                        g.ShowBanner("ВРЕМЯ ЗАБЕГА ИСТЕКЛО! ПОПРОБУЙТЕ ПРОЙТИ ТРАССУ БЫСТРЕЕ!", Color.Orange, 2.2f);
                    }
                    break;

                case "freddy":
                    _timer += dt;
                    if (_timer < 18.0f)
                    {
                        _progress -= 15.5f * dt; // Завод шкатулки падает до 06:00 AM
                        _auxTimer -= dt;
                        if (_auxTimer <= 0f && _stepCount == 0)
                        {
                            _auxTimer = 5.5f;
                            AddTrialEnemies(g, 1, "beetle", 7.5f);
                        }
                        if (dist < 4.8f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                        {
                            _progress = MathF.Min(100f, _progress + 38f);
                            SoundManager.Play("ui_tick", 1.0f, 1.25f);
                            new VisualFX(Position + new Vector3(0, 1.8f, 0), Color.Gold, 3.0f, 0.22f);
                        }
                        if (_timer >= 14.5f && _stepCount == 0)
                        {
                            _stepCount = 1;
                            SoundManager.Play("boss_roar", 1.15f, 1.0f);
                            SpawnTrialBoss(g, "animatronic_endo", "ЗОЛОТОЙ ЭНДОСКЕЛЕТ 06:00");
                            g.ShowBanner("05:00 AM! НА СЦЕНУ ВЫШЕЛ БОСС — ЗОЛОТОЙ ЭНДОСКЕЛЕТ!", Color.Gold, 3.4f);
                        }
                        int nightHour = Math.Clamp((int)(_timer / 3.0f), 0, 6);
                        string hourStr = nightHour == 0 ? "12:00 AM" : $"0{nightHour}:00 AM";
                        _labelEnt.Color = _progress < 30f ? Color.Red : Color.Gold;
                        _labelEnt.BillboardText = $"Ночная Смена: [ {hourStr} / 06:00 AM ]\nЗавод Шкатулки: {(int)MathF.Max(0, _progress)}% — Жмите [E] для подзавода!";

                        if (_progress <= 0f)
                        {
                            _state = 0;
                            ClearSpawnedGuards(g);
                            SoundManager.Play("boss_roar", 1.15f, 1.35f);
                            new VisualFX(g.PlayerPos + new Vector3(0, 1.4f, 0), Color.Red, 8.0f, 0.4f);
                            g.PlayerHp = MathF.Max(1f, g.PlayerHp - g.PlayerMaxHp * 0.25f);
                            _labelEnt.BillboardText = "[E] Шкатулка остановилась (СКРИМЕР)! Нажмите [E] для новой смены";
                            g.ShowBanner("МУЗЫКАЛЬНАЯ ШКАТУЛКА РАЗРЯДИЛАСЬ — СКРИМЕР ФРЕДДИ!", Color.Red, 2.8f);
                            return;
                        }
                    }
                    else
                    {
                        if (_stepCount == 0)
                        {
                            _stepCount = 1;
                            SpawnTrialBoss(g, "animatronic_endo", "ЗОЛОТОЙ ЭНДОСКЕЛЕТ 06:00");
                        }
                        _labelEnt.Color = Color.Gold;
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                        {
                            _labelEnt.BillboardText = $"[ 06:00 AM ] Одолейте Золотого Эндоскелета [{(int)MathF.Max(1f, _spawnedGuards[0].Hp)} HP]!";
                        }
                        else
                        {
                            SoundManager.Play("dante_clock", 1.1f, 1.3f);
                            CompleteCharacterQuest(g);
                        }
                    }
                    break;

                case "steve":
                    if (_stepCount == 3 && dist < 4.5f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                    {
                        _stepCount = 4;
                        SoundManager.Play("portal_open", 1.0f, 1.1f);
                        SpawnTrialEnemies(g, 3, "nether_blaze", 8.5f);
                        g.ShowBanner("ПОРТАЛ НЕЗЕРА ЗАЖЖЁН! ОТРАЗИТЕ ВТОРЖЕНИЕ РОЯ ИФРИТОВ НЕЗЕРА!", Color.Orange, 3.5f);
                    }
                    else if (_stepCount >= 4)
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Отразите вторжение Ифритов Незера: осталось {_spawnedGuards.Count}!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "jotaro":
                    if (dist <= 13.0f)
                    {
                        // Духовный ураган Стрелы физически отталкивает игрока от метеорита!
                        Vector3 pushDir = g.PlayerPos - Position;
                        pushDir.Y = 0f;
                        pushDir = pushDir.LengthSquared() > 0.01f ? Vector3.Normalize(pushDir) : Vector3.UnitZ;
                        g.PlayerPos = Physics.ResolveHorizontalCollision(g.PlayerPos + pushDir * 7.6f * dt, 0.72f, g.PlayerPos.Y);
                    }
                    if (dist <= 6.4f)
                    {
                        _progress += (100f / 7.0f) * dt;
                        if ((int)((_progress + 16f * dt) / 20f) != (int)(_progress / 20f))
                            new LightningBeam(Position + new Vector3(0, 2.5f, 0), g.PlayerPos + new Vector3(0, 1.3f, 0), Color.Gold, 0.24f);
                        _labelEnt.BillboardText = $"Сопротивление урагану Стрелы: {(int)Math.Min(100f, _progress)}%";
                        if (_progress >= 100f)
                            CompleteCharacterQuest(g);
                    }
                    else
                    {
                        _progress = MathF.Max(0f, _progress - 20f * dt);
                        _labelEnt.BillboardText = $"ВАС СДУВАЕТ ВЕТРОМ! Идите к Стреле ({(int)_progress}%)!";
                    }
                    break;

                case "vyper":
                    _timer -= dt;
                    _auxTimer -= dt;
                    if (_auxTimer <= 0f)
                    {
                        _auxTimer = 2.2f;
                        Vector3 muzzle = Position + new Vector3(0, 2.1f, 0);
                        Vector3 aim = (g.PlayerPos + new Vector3(0, 1.0f, 0)) - muzzle;
                        if (aim.LengthSquared() > 0.05f)
                        {
                            SoundManager.Play("laser", 0.55f, 1.25f);
                            new Entities.Projectile(muzzle, Vector3.Normalize(aim), 19.0f, 9.0f, 0.34f,
                                Color.Lime, isPlayer: false, canProc: false, maxLifetime: 2.0f);
                        }
                    }
                    _labelEnt.BillboardText = $"Сбито вращающихся замков: {_stepCount}/4 ({_timer:F1} сек)";
                    if (_timer <= 0f)
                    {
                        _state = 0;
                        ClearSecondaryNodes();
                        SoundManager.Play("ui_error");
                        _labelEnt.BillboardText = "[E] Замки перезагрузились! Нажмите [E] для новой попытки";
                        g.ShowBanner("ВРЕМЯ ВЫШЛО! СБЕЙТЕ ВСЕ 4 ЗАМКА ВЫСТРЕЛАМИ!", Color.Orange, 2.2f);
                    }
                    break;

                case "haze":
                    if (_stepCount >= 4)
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Устраните Арканную Тень-перехватчика: осталось {_spawnedGuards.Count}!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "dmc_trio":
                    _timer -= dt;
                    _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                    var vergilBoss = _spawnedGuards.Find(m => m.EnemyType == "vergil_mirage");

                    if (_stepCount == 0 && curTotalHp < _lastPlayerTotalHp - 0.5f)
                    {
                        _progress = MathF.Max(0f, _progress - 45f);
                        _startKills = Program.RunKills;
                        _startDamageDealt = Program.RunDamageDealt;
                        SoundManager.Play("ui_error", 0.8f);
                        g.ShowBanner("ПОЛУЧЕН УРОН! РАНГ СТИЛЯ УПАЛ!", Color.Red, 1.6f);
                    }
                    _lastPlayerTotalHp = curTotalHp;

                    int newKills = Math.Max(0, Program.RunKills - _startKills);
                    float newDmg = MathF.Max(0f, Program.RunDamageDealt - _startDamageDealt);
                    _startKills = Program.RunKills;
                    _startDamageDealt = Program.RunDamageDealt;

                    if (vergilBoss == null)
                    {
                        _progress = 100f;
                        _stepCount = 1;
                    }
                    else if (_stepCount == 0)
                    {
                        _progress = MathF.Max(0f, _progress + newKills * 24f + newDmg * 0.14f - 3.5f * dt);
                        if (_progress >= 100f)
                        {
                            _progress = 100f;
                            _stepCount = 1;
                            SoundManager.Play("ult_cast", 1.0f, 1.15f);
                            g.ShowBanner("РАНГ СТИЛЯ [SSS] ЗАФИКСИРОВАН! ДОБЕЙТЕ МИРАЖА ВЕРГИЛИЯ!", Color.Gold, 2.8f);
                        }
                    }

                    string rankStr = _progress >= 100f ? "SSS"
                        : (_progress >= 82f ? "SS"
                        : (_progress >= 65f ? "S"
                        : (_progress >= 48f ? "A"
                        : (_progress >= 30f ? "B"
                        : (_progress >= 15f ? "C" : "D")))));
                    string bossHpInfo = vergilBoss != null ? $"  |  Мираж Вергилия: {(int)MathF.Max(1f, vergilBoss.Hp)} HP" : "  |  Мираж повержен!";
                    _labelEnt.BillboardText = $"РАНГ СТИЛЯ СПАРДЫ: [ {rankStr} ] ({(int)Math.Min(100f, _progress)}%){bossHpInfo}\nОсталось времени: {_timer:F1}с";

                    if (_stepCount == 1 && vergilBoss == null)
                    {
                        ClearSpawnedGuards(g);
                        CompleteCharacterQuest(g);
                    }
                    else if (_timer <= 0f)
                    {
                        _state = 0;
                        ClearSpawnedGuards(g);
                        SoundManager.Play("ui_error");
                        _labelEnt.BillboardText = "[E] Пьедестал Спарды (Нажмите [E], чтобы испытать Стиль снова)";
                        g.ShowBanner("ВРЕМЯ ДУЭЛИ НА ВЕРШИНЕ КЛИПОТА ИСТЕКЛО!", Color.Orange, 2.2f);
                    }
                    break;

                case "jack_o":
                    _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                    if (dist <= 11.5f)
                    {
                        float prev = _progress;
                        _progress = MathF.Min(100f, _progress + (100f / 14.0f) * dt);
                        if (prev < 55f && _progress >= 55f && _subPhase == 0)
                        {
                            _subPhase = 1;
                            AddTrialEnemies(g, 2, "greater_wisp", 10.0f, elite: true, hpMultiplier: 1.35f);
                            g.ShowBanner("ВТОРАЯ ВОЛНА СТРАЖЕЙ ЗАДНЕГО ДВОРА! ДЕРЖИТЕ ПЕРИМЕТР ЗНАМЕНИ!", Rgb(255, 115, 75), 2.5f);
                        }
                    }
                    else
                    {
                        _progress = MathF.Max(0f, _progress - 12f * dt);
                    }
                    _labelEnt.BillboardText = _progress < 100f
                        ? $"Оборона Знамени Элизиона: {(int)_progress}%  |  Врагов у знамени: {_spawnedGuards.Count}"
                        : $"Знамя удержано (100%) — добейте стражей Заднего Двора ({_spawnedGuards.Count})!";
                    if (_progress >= 100f && _spawnedGuards.Count == 0)
                        CompleteCharacterQuest(g);
                    break;
            }
        }
    }
}