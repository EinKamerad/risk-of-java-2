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
    public partial class CharacterQuestMonument
    {
        private enum ExpNodeBehavior
        {
            Breakable,
            InteractE,
            TouchCollect
        }

        private sealed class ExpansionQuestNode : QuestSecondaryNode
        {
            private readonly ExpNodeBehavior _behavior;
            private readonly string _title;
            private readonly Color _accentCol;
            private readonly string _soundKey;
            private readonly Entity _decorPivot;
            private readonly Entity _customLabel;

            public ExpansionQuestNode(
                Vector3 pos,
                CharacterQuestMonument parentQuest,
                int nodeIndex,
                string visualStyle,
                string title,
                Color accentCol,
                ExpNodeBehavior behavior,
                float maxHp = 240f,
                string soundKey = "shrine_use")
                : base(pos, parentQuest, nodeIndex, "", accentCol, QuestNodeMode.Touch, isTrueOrb: true)
            {
                _behavior = behavior;
                _title = title;
                _accentCol = accentCol;
                _soundKey = soundKey;
                Hp = MaxHp = maxHp;

                _decorPivot = new Entity(parent: this, position: new(0, 1.5f, 0));
                BuildStyleMesh(visualStyle, accentCol);

                string initText = behavior == ExpNodeBehavior.Breakable
                    ? $"{title} [{(int)MathF.Ceiling(Hp)}/{(int)MaxHp} HP]\n[Разбейте атаками!]"
                    : title;

                _customLabel = new Entity(parent: this, position: new(0, 3.35f, 0), color: accentCol)
                {
                    BillboardText = initText
                };
            }

            private void BuildStyleMesh(string style, Color col)
            {
                switch (style)
                {
                    case "dark_seal":
                        // Готический кристалл печати Тёмного Фонтана
                        new Entity(parent: this, model: "cube", scale: new(1.45f, 2.3f, 1.45f), position: new(0, 1.15f, 0), color: Rgb(32, 24, 62));
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(1.55f, 2.55f, 1.55f), color: Rgb(75, 245, 245), unlit: true);
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(2.35f, 0.18f, 2.35f), rotation: new(20f, 0, -18f), color: col, unlit: true);
                        break;

                    case "kromer_deal":
                        // Неоновый терминал контракта [[BIG SHOT]]
                        new Entity(parent: this, model: "cube", scale: new(1.25f, 1.85f, 0.85f), position: new(0, 0.95f, 0), color: Rgb(26, 22, 42));
                        new Entity(parent: _decorPivot, model: "sphere", scale: new(0.62f, 0.62f, 0.28f), position: new(-0.34f, 0.45f, 0.38f), color: Rgb(255, 95, 205), unlit: true);
                        new Entity(parent: _decorPivot, model: "sphere", scale: new(0.62f, 0.62f, 0.28f), position: new(0.34f, 0.45f, 0.38f), color: Rgb(255, 235, 65), unlit: true);
                        break;

                    case "mega_pipis":
                        // Гигантская бронированная синяя сфера Мега-[Pipis]
                        new Entity(parent: _decorPivot, model: "sphere", scale: new(1.95f, 1.52f, 1.68f), color: Rgb(48, 195, 255), unlit: true);
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(2.35f, 0.18f, 2.35f), rotation: new(16f, 25f, -14f), color: Rgb(255, 235, 65), unlit: true);
                        break;

                    case "arbiter_lock":
                        // Чёрно-золотая клеть Сингулярности [Замок] Бины
                        foreach (float lx in new[] { -0.68f, 0.68f })
                            new Entity(parent: this, model: "cube", scale: new(0.26f, 2.65f, 0.26f), position: new(lx, 1.35f, 0), color: Rgb(22, 20, 26));
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(2.25f, 0.18f, 2.25f), rotation: new(28f, 0, -24f), color: Rgb(255, 208, 68), unlit: true);
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(2.10f, 0.16f, 2.10f), rotation: new(-26f, 65f, 22f), color: Rgb(255, 208, 68), unlit: true);
                        break;

                    case "prts_anchor":
                        // Нейронный якорь Саркофага PRTS ◇
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(1.65f, 2.35f, 0.28f), color: Rgb(255, 58, 72), unlit: true);
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(0.95f, 1.35f, 0.36f), color: Rgb(20, 20, 28));
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(0.45f, 0.65f, 0.44f), color: Color.White, unlit: true);
                        break;

                    case "closure_drone":
                        // Ремонтный дрон Родос Айленд
                        new Entity(parent: _decorPivot, model: "cube", scale: new(1.15f, 0.78f, 1.15f), color: Rgb(232, 238, 248));
                        foreach (float rx in new[] { -0.78f, 0.78f })
                            new Entity(parent: _decorPivot, model: "diamond", scale: new(0.65f, 0.12f, 0.65f), position: new(rx, 0.42f, 0), color: Rgb(72, 215, 255), unlit: true);
                        new Entity(parent: _decorPivot, model: "sphere", scaleUniform: 0.42f, position: new(0, 0, 0.58f), color: Color.Orange, unlit: true);
                        break;

                    case "harmony_fork":
                        // Золотой камертон Гармонии Пенаконии ♫
                        foreach (float fx in new[] { -0.34f, 0.34f })
                            new Entity(parent: _decorPivot, model: "cube", scale: new(0.16f, 1.55f, 0.16f), position: new(fx, 0.35f, 0), color: Color.Gold, unlit: true);
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(2.15f, 0.16f, 2.15f), rotation: new(18f, 0, -14f), color: Rgb(205, 165, 255), unlit: true);
                        break;

                    case "subconscious_rose":
                        // Роза Подсознания Комейдзи
                        new Entity(parent: _decorPivot, model: "sphere", scale: new(1.18f, 0.92f, 1.18f), color: col, unlit: true);
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(1.85f, 0.22f, 1.85f), rotation: new(15f, 35f, -15f), color: Rgb(245, 78, 148), unlit: true);
                        break;

                    case "halcyon_geode":
                        // Осквернённая грозовая жеода Алкиона на Главном Меридиане
                        new Entity(parent: this, model: "sphere", scale: new(2.15f, 1.05f, 2.05f), position: new(0, 0.48f, 0), color: Rgb(42, 52, 74));
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(1.35f, 2.45f, 1.35f), rotation: new(8f, 25f, -8f), color: Color.Gold, unlit: true);
                        new Entity(parent: _decorPivot, model: "diamond", scale: new(2.15f, 0.20f, 2.15f), rotation: new(-18f, 40f, 16f), color: Rgb(90, 222, 255), unlit: true);
                        break;
                }
            }

            public override void OnUpdate(float dt)
            {
                var g = WorldContext.Game;
                if (Collected || g == null || IsDestroyed || ParentQuest.Completed) return;

                float t = (float)Raylib.GetTime();
                float dist = Vector3.Distance(Position, g.PlayerPos);
                _customLabel.Enabled = dist < 30.0f;

                _decorPivot.RotationY += 92f * dt;
                _decorPivot.Y = 1.5f + MathF.Sin(t * 3.6f + NodeIndex) * 0.14f;

                if (_behavior == ExpNodeBehavior.Breakable)
                {
                    if (ParentQuest.CheckPlayerAttackHit(Position + new Vector3(0, 1.45f, 0), 2.55f, out float dmg))
                    {
                        float dealt = MathF.Max(48f, dmg);
                        Hp = MathF.Max(0f, Hp - dealt);
                        new VisualFX(Position + new Vector3(0, 1.45f, 0), _accentCol, 3.0f, 0.22f);
                        _customLabel.BillboardText = $"{_title} [{(int)MathF.Ceiling(Hp)}/{(int)MaxHp} HP]\n[Разбейте атаками!]";
                        if (Hp <= 0f)
                        {
                            Collected = true;
                            Enabled = false;
                            SoundManager.Play(_soundKey, 1.0f, 1.12f);
                            new VisualFX(Position + new Vector3(0, 1.5f, 0), _accentCol, 5.8f, 0.36f);
                            ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                        }
                    }
                }
                else if (_behavior == ExpNodeBehavior.InteractE)
                {
                    if (dist < 3.65f && Raylib.IsKeyPressed(KeyboardKey.E) && g.CanPlayerInteract)
                    {
                        Collected = true;
                        Enabled = false;
                        SoundManager.Play(_soundKey, 1.0f, 1.08f + NodeIndex * 0.06f);
                        new VisualFX(Position + new Vector3(0, 1.5f, 0), _accentCol, 4.6f, 0.32f);
                        ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                    }
                }
                else
                {
                    if (dist < 3.35f)
                    {
                        Collected = true;
                        Enabled = false;
                        SoundManager.Play(_soundKey, 0.95f, 1.15f + NodeIndex * 0.06f);
                        new VisualFX(Position + new Vector3(0, 1.45f, 0), _accentCol, 4.5f, 0.32f);
                        ParentQuest.OnSecondaryNodeTouched(NodeIndex);
                    }
                }
            }
        }

        private Entities.CombatTarget? SpawnScaledExpansionBoss(IPlayerContext g, string bossType, string fallbackTitle, float hpScale = 0.80f)
        {
            var boss = SpawnTrialBoss(g, bossType, fallbackTitle);
            if (boss != null && MathF.Abs(hpScale - 1.0f) > 0.01f)
            {
                boss.MaxHp *= hpScale;
                boss.Hp = boss.MaxHp;
                boss.UpdateHpBillboard();
            }
            return boss;
        }

        private void ResetFailedExpansionTrial(IPlayerContext g, string idlePrompt, string failBanner)
        {
            _state = 0;
            _subPhase = 0;
            _stepCount = 0;
            if (_zoneRing != null) _zoneRing.Enabled = false;
            ClearSecondaryNodes();
            ClearSpawnedGuards(g);
            _labelEnt.Color = _themeCol;
            _labelEnt.BillboardText = idlePrompt;
            SoundManager.Play("ui_error");
            g.ShowBanner(failBanner, Color.Red, 2.5f);
        }

        private bool TryBuildExpansionMonumentVisuals(string key, out string prompt)
        {
            switch (key)
            {
                case "susie":
                    // Готический постамент Тёмного Святилища + ало-пурпурная Секира Грубого Разрушителя (Rude Buster)
                    new Entity(parent: this, model: "cube", scale: new(2.0f, 1.45f, 2.0f), position: new(0, 0.9f, 0), color: Rgb(38, 28, 68));
                    new Entity(parent: this, model: "diamond", scale: new(2.2f, 0.22f, 2.2f), position: new(0, 1.62f, 0), color: Rgb(75, 245, 245), unlit: true);
                    new Entity(parent: _focalPart, model: "cube", scale: new(0.24f, 2.75f, 0.24f), position: new(0, 0.55f, 0), rotation: new(0, 0, -22f), color: Rgb(118, 68, 96));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.85f, 1.35f, 0.34f), position: new(0.42f, 1.25f, 0), rotation: new(0, 0, -22f), color: Rgb(235, 58, 148), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(2.15f, 0.16f, 2.15f), position: new(0, 0.45f, 0), rotation: new(18f, 0, 16f), color: Rgb(185, 75, 255), unlit: true);
                    prompt = "[E] Алтарь Секиры Тёмного Мира (3 Печати Фонтана + Босс: Рокочущий Рыцарь!)";
                    return true;

                case "spamton":
                    // Кибер-таксофон [[1997]] с розово-жёлтыми очками [[BIG SHOT]] и сферой [Pipis]
                    new Entity(parent: this, model: "cube", scale: new(1.65f, 2.45f, 1.25f), position: new(0, 1.38f, 0), color: Rgb(34, 96, 62));
                    new Entity(parent: this, model: "cube", scale: new(1.38f, 1.05f, 0.18f), position: new(0, 1.85f, 0.58f), color: Rgb(22, 18, 36));
                    new Entity(parent: _focalPart, model: "sphere", scale: new(0.68f, 0.68f, 0.26f), position: new(-0.38f, 0.35f, 0.62f), color: Rgb(255, 95, 205), unlit: true);
                    new Entity(parent: _focalPart, model: "sphere", scale: new(0.68f, 0.68f, 0.26f), position: new(0.38f, 0.35f, 0.62f), color: Rgb(255, 235, 65), unlit: true);
                    new Entity(parent: _focalPart, model: "sphere", scale: new(0.78f, 0.62f, 0.68f), position: new(0, 1.28f, 0), color: Rgb(48, 195, 255), unlit: true);
                    prompt = "[E] Телефонный Автомат [[1997]] (4 Сделки Кромера + Штурм Мега-[Pipis]!)";
                    return true;

                case "binah":
                    // Чёрно-золотой Столп Арбитра Главы с кольцами Сингулярности [Замок]
                    new Entity(parent: this, model: "cube", scale: new(1.75f, 2.75f, 1.75f), position: new(0, 1.52f, 0), color: Rgb(20, 18, 24));
                    new Entity(parent: this, model: "cube", scale: new(1.86f, 0.18f, 1.86f), position: new(0, 2.86f, 0), color: Rgb(255, 208, 68), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(0.68f, 2.45f, 0.68f), position: new(0, 0.95f, 0), color: Rgb(28, 24, 32));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(2.25f, 0.18f, 2.25f), position: new(0, 0.95f, 0), rotation: new(26f, 15f, -20f), color: Rgb(255, 208, 68), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(2.05f, 0.16f, 2.05f), position: new(0, 0.95f, 0), rotation: new(-24f, 65f, 22f), color: Rgb(255, 208, 68), unlit: true);
                    prompt = "[E] Чёрно-Золотой Столп Арбитра (Сорвать 3 Замка Главы + Босс: Бина!)";
                    return true;

                case "don_quixote":
                    // Карусельный постамент Ла-Манчаленда и Золотое Рыцарское Копьё Мечты
                    new Entity(parent: this, model: "cube", scale: new(1.95f, 1.25f, 1.95f), position: new(0, 0.8f, 0), color: Rgb(145, 26, 44));
                    foreach (float px in new[] { -1.1f, 1.1f })
                        new Entity(parent: this, model: "cube", scale: new(0.18f, 3.2f, 0.18f), position: new(px, 1.75f, -0.55f), color: Color.Gold);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(0.58f, 3.05f, 0.58f), position: new(0, 0.95f, 0), rotation: new(14f, 0, -12f), color: Color.Gold, unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.55f, 0.22f, 1.55f), position: new(0, 0.45f, 0), color: Rgb(225, 38, 58), unlit: true);
                    prompt = "[E] Копьё Ла-Манчаленда (Рыцарский Турнир: 5 Барьеров + Поединок!)";
                    return true;

                case "priestess":
                    // Рамка Саркофага Первой Цивилизации и парящий багровый ромб «Глаза Пристэсс» PRTS ◇
                    foreach (float sx in new[] { -1.05f, 1.05f })
                        new Entity(parent: this, model: "cube", scale: new(0.38f, 3.25f, 0.55f), position: new(sx, 1.78f, -0.35f), color: Rgb(44, 52, 66));
                    new Entity(parent: this, model: "cube", scale: new(2.55f, 0.38f, 0.62f), position: new(0, 3.35f, -0.35f), color: Rgb(44, 52, 66));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.75f, 2.45f, 0.28f), position: new(0, 0.55f, 0), color: Rgb(255, 58, 72), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(0.98f, 1.42f, 0.36f), position: new(0, 0.55f, 0), color: Rgb(18, 18, 24));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(0.42f, 0.65f, 0.44f), position: new(0, 0.55f, 0), color: Color.White, unlit: true);
                    prompt = "[E] Алтарь «Глаза Пристэсс» ◇ (4 Узла PRTS + Босс: Источник Ориджиниума!)";
                    return true;

                case "closure":
                    // Инженерная стойка «Родос Айленд» и парящий сервисный дрон Кложур
                    new Entity(parent: this, model: "cube", scale: new(2.05f, 1.55f, 1.55f), position: new(0, 0.95f, 0), color: Rgb(42, 50, 64));
                    new Entity(parent: this, model: "cube", scale: new(1.75f, 0.18f, 1.25f), position: new(0, 1.76f, 0), color: Rgb(72, 215, 255), unlit: true);
                    new Entity(parent: _focalPart, model: "cube", scale: new(1.05f, 0.72f, 1.05f), position: new(0, 0.85f, 0), color: Rgb(235, 240, 248));
                    foreach (float wx in new[] { -0.85f, 0.85f })
                        new Entity(parent: _focalPart, model: "diamond", scale: new(0.75f, 0.28f, 0.24f), position: new(wx, 0.95f, 0), rotation: new(0, 0, wx < 0 ? 22f : -22f), color: Rgb(235, 72, 115), unlit: true);
                    prompt = "[E] Мастерская Кложур (Оборона Сервера + Починка 3 Дронов [E]!)";
                    return true;

                case "daitaku_helios":
                    // Неоновый турникет и диджейские колонки Дайтаку Хелиос «WEI~★!»
                    foreach (float sx in new[] { -1.55f, 1.55f })
                    {
                        new Entity(parent: this, model: "cube", scale: new(0.55f, 3.2f, 0.55f), position: new(sx, 1.75f, 0), color: Rgb(34, 48, 86));
                        new Entity(parent: this, model: "sphere", scaleUniform: 0.48f, position: new(sx, 2.55f, 0.30f), color: sx < 0 ? Rgb(72, 225, 255) : Rgb(255, 95, 205), unlit: true);
                    }
                    new Entity(parent: this, model: "cube", scale: new(3.75f, 0.48f, 0.58f), position: new(0, 3.45f, 0), color: Rgb(255, 95, 205), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.35f, 1.35f, 0.36f), position: new(0, 0.55f, 0), color: Rgb(72, 225, 255), unlit: true);
                    prompt = "[E] Турникет «WEI~★!» (Ритм-Спринт через 6 Неоновых Арок на Драйве!)";
                    return true;

                case "robin":
                    // Золотой оперный пюпитр Пенаконии с крыльями Галовианцев и микрофоном Гармонии ♫
                    new Entity(parent: this, model: "cube", scale: new(2.15f, 1.15f, 1.75f), position: new(0, 0.75f, 0), color: Rgb(238, 230, 218));
                    for (int p = -2; p <= 2; p++)
                        new Entity(parent: this, model: "cube", scale: new(0.28f, 2.8f - MathF.Abs(p) * 0.35f, 0.28f), position: new(p * 0.45f, 2.1f, -0.65f), color: Color.Gold);
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 0.55f, position: new(0, 0.85f, 0.2f), color: Color.Gold, unlit: true);
                    foreach (float wx in new[] { -0.95f, 0.95f })
                        new Entity(parent: _focalPart, model: "diamond", scale: new(1.15f, 0.42f, 0.22f), position: new(wx, 0.85f, 0.2f), rotation: new(0, 0, wx < 0 ? 24f : -24f), color: Rgb(210, 168, 255), unlit: true);
                    prompt = "[E] Сцена Пенаконии ♫ (Ария Гармонии + Босс: Великий Септимий!)";
                    return true;

                case "komeiji":
                    // Витражный алтарь Дворца Духов Земли и переплетённый Третий Глаз Сатори и Коиши
                    new Entity(parent: this, model: "cube", scale: new(1.85f, 1.65f, 1.85f), position: new(0, 1.0f, 0), color: Rgb(54, 32, 72));
                    new Entity(parent: _focalPart, model: "sphere", scale: new(1.35f, 0.95f, 0.85f), position: new(0, 0.65f, 0), color: Rgb(242, 72, 138), unlit: true);
                    new Entity(parent: _focalPart, model: "sphere", scaleUniform: 0.48f, position: new(0, 0.65f, 0.38f), color: Rgb(78, 245, 185), unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(2.25f, 0.16f, 2.25f), position: new(0, 0.65f, 0), rotation: new(22f, 0, -22f), color: Rgb(78, 245, 185), unlit: true);
                    prompt = "[E] Алтарь Третьего Глаза (Данмаку Роз Подсознания + 4 Воспоминания!)";
                    return true;

                case "false_son":
                    // Расколотый грозовой обелиск Главного Меридиана и золотое Сердце Аурелионита
                    foreach (float hx in new[] { -0.95f, 0.95f })
                        new Entity(parent: this, model: "cube", scale: new(0.68f, 2.95f, 1.15f), position: new(hx, 1.62f, 0), rotation: new(0, 0, hx < 0 ? -12f : 12f), color: Rgb(44, 54, 76));
                    new Entity(parent: _focalPart, model: "diamond", scale: new(1.25f, 2.35f, 1.25f), position: new(0, 0.65f, 0), color: Color.Gold, unlit: true);
                    new Entity(parent: _focalPart, model: "diamond", scale: new(2.35f, 0.20f, 2.35f), position: new(0, 0.65f, 0), rotation: new(18f, 35f, -16f), color: Rgb(90, 222, 255), unlit: true);
                    prompt = "[E] Обелиск Меридиана (Очистить 3 Жеоды Алкиона + Босс: Ложный Сын!)";
                    return true;

                default:
                    prompt = "";
                    return false;
            }
        }

        private void StartExpansionQuestTrial(IPlayerContext g)
        {
            float curShield = Entities.CombatManager.Controller?.Shield ?? 0f;

            switch (CharKey)
            {
                case "susie":
                {
                    _state = 1;
                    _subPhase = 0;
                    _stepCount = 0;
                    _timer = 26.0f;
                    _auxTimer = 3.5f;
                    ClearSecondaryNodes();
                    SoundManager.Play("slam_land", 1.1f, 0.92f);
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = i * (MathF.PI * 2f / 3f) + 0.35f;
                        Vector3 sp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(ang) * 9.5f, 0f, MathF.Sin(ang) * 9.5f));
                        var seal = new ExpansionQuestNode(sp, this, i, "dark_seal", $"Печать Фонтана #{i + 1}/3", Rgb(235, 72, 168), ExpNodeBehavior.Breakable, 240f, "slam_land");
                        _secondaryNodes.Add(seal);
                        g.StageEntities.Add(seal);
                    }
                    SpawnTrialEnemies(g, 2, "hollow_shadow", 9.0f);
                    g.ShowBanner("ТЁМНОЕ СВЯТИЛИЩЕ: РАЗБЕЙТЕ АТАКАМИ 3 ПЕЧАТИ ФОНТАНА И ВЫЗОВИТЕ РОКОЧУЩЕГО РЫЦАРЯ!", Rgb(235, 72, 168), 3.6f);
                    break;
                }

                case "spamton":
                {
                    _state = 1;
                    _subPhase = 0;
                    _stepCount = 0;
                    _timer = 22.0f;
                    _auxTimer = 2.2f;
                    ClearSecondaryNodes();
                    SoundManager.Play("coin_flip", 1.05f, 1.15f);
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = i * (MathF.PI * 0.5f) + 0.45f;
                        Vector3 dp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(ang) * 10.5f, 0f, MathF.Sin(ang) * 10.5f));
                        var deal = new ExpansionQuestNode(dp, this, i, "kromer_deal", $"[E] Контракт [[КРОМЕР]] #{i + 1}/4", Rgb(255, 235, 65), ExpNodeBehavior.InteractE, 100f, "coin_flip");
                        _secondaryNodes.Add(deal);
                        g.StageEntities.Add(deal);
                    }
                    SpawnTrialEnemies(g, 2, "hell_drone", 9.5f);
                    g.ShowBanner("ГОРЯЧАЯ ЛИНИЯ [[1997]]: ПОДПИШИТЕ [E] ВСЕ 4 КОНТРАКТА КРОМЕРА ПОД ОБСТРЕЛОМ [PIPIS]!", Rgb(255, 95, 205), 3.6f);
                    break;
                }

                case "binah":
                {
                    _state = 1;
                    _subPhase = 0;
                    _stepCount = 0;
                    _timer = 22.0f;
                    ClearSecondaryNodes();
                    SoundManager.Play("dante_clock", 1.05f, 0.90f);
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = i * (MathF.PI * 2f / 3f) + 0.5f;
                        Vector3 lp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(ang) * 8.8f, 0f, MathF.Sin(ang) * 8.8f));
                        var lockNode = new ExpansionQuestNode(lp, this, i, "arbiter_lock", $"Золотая Цепь Замка #{i + 1}/3", Rgb(255, 208, 68), ExpNodeBehavior.Breakable, 240f, "parry");
                        _secondaryNodes.Add(lockNode);
                        g.StageEntities.Add(lockNode);
                    }
                    SpawnTrialEnemies(g, 2, "hollow_shadow", 9.0f);
                    g.ShowBanner("СИНГУЛЯРНОСТЬ ГЛАВЫ [ЗАМОК]: РАЗБЕЙТЕ 3 ЗОЛОТЫЕ ЦЕПИ И ПРИМИТЕ БОЙ С АРБИТРОМ БИНОЙ!", Rgb(255, 208, 68), 3.6f);
                    break;
                }

                case "don_quixote":
                {
                    _state = 1;
                    _subPhase = 0;
                    _stepCount = 0;
                    _timer = 22.0f;
                    ClearSecondaryNodes();
                    ClearSpawnedGuards(g);
                    SoundManager.Play("ui_ready", 1.05f, 1.18f);
                    Vector3 raceDir = Position.Length() > 5f ? Vector3.Normalize(new Vector3(-Position.X, 0f, -Position.Z)) : -Vector3.UnitZ;
                    Vector3 sideDir = new(-raceDir.Z, 0f, raceDir.X);
                    for (int i = 0; i < 5; i++)
                    {
                        float d = (i + 1) * 9.5f;
                        float curve = MathF.Sin((i + 1) * 1.10f) * 7.2f;
                        Vector3 gp = Physics.FindSafeSpawnPoint(Position + raceDir * d + sideDir * curve);
                        string lbl = i == 4 ? "ФИНАЛЬНЫЙ БАРЬЕР МЕЧТЫ #5" : $"ТУРНИРНЫЙ БАРЬЕР #{i + 1}/5";
                        var gate = new QuestSecondaryNode(gp, this, i, lbl, Color.Gold, QuestNodeMode.RaceGate);
                        _secondaryNodes.Add(gate);
                        g.StageEntities.Add(gate);
                    }
                    g.ApplyDash(g.PlayerForward * 21.0f, 0.24f);
                    g.ShowBanner("ВПЕРЁД, РОСИНАНТ! ПРОМЧИТЕСЬ СКВОЗЬ 5 ТУРНИРНЫХ БАРЬЕРОВ И ВЫИГРАЙТЕ ПОЕДИНОК!", Color.Gold, 3.6f);
                    break;
                }

                case "priestess":
                {
                    _state = 1;
                    _subPhase = 0;
                    _stepCount = 0;
                    _timer = 22.0f;
                    ClearSecondaryNodes();
                    SoundManager.Play("portal_open", 1.0f, 1.12f);
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = i * (MathF.PI * 0.5f) + 0.35f;
                        Vector3 np = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(ang) * 10.0f, 0f, MathF.Sin(ang) * 10.0f));
                        var node = new ExpansionQuestNode(np, this, i, "prts_anchor", $"[E] Нейронный Узел PRTS ◇ #{i + 1}/4", Rgb(255, 58, 72), ExpNodeBehavior.InteractE, 100f, "ui_ready");
                        _secondaryNodes.Add(node);
                        g.StageEntities.Add(node);
                    }
                    SpawnTrialEnemies(g, 2, "lunar_chimera", 9.5f);
                    g.ShowBanner("ПРОТОКОЛ PRTS ◇: СИНХРОНИЗИРУЙТЕ [E] ВСЕ 4 НЕЙРОННЫХ УЗЛА САРКОФАГА!", Rgb(255, 58, 72), 3.6f);
                    break;
                }

                case "closure":
                {
                    _state = 1;
                    _subPhase = 0;
                    _stepCount = 0;
                    _progress = 0f;
                    _timer = 360f;
                    ClearSecondaryNodes();
                    if (_zoneRing != null) _zoneRing.Enabled = true;
                    SoundManager.Play("build_place", 1.05f, 1.05f);
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = i * (MathF.PI * 2f / 3f) + 0.55f;
                        Vector3 dp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(ang) * 8.2f, 0f, MathF.Sin(ang) * 8.2f));
                        var drone = new ExpansionQuestNode(dp, this, i, "closure_drone", $"[E] Починить Дрон Кложур #{i + 1}/3", Rgb(72, 215, 255), ExpNodeBehavior.InteractE, 100f, "heal");
                        _secondaryNodes.Add(drone);
                        g.StageEntities.Add(drone);
                    }
                    SpawnTrialEnemies(g, 2, "hell_drone", 10.0f);
                    AddTrialEnemies(g, 1, "combine_soldier", 10.5f);
                    g.ShowBanner("МАСТЕРСКАЯ РОДОСА: ДЕРЖИТЕ ПЕРИМЕТР СЕРВЕРА (360 HP) И ЧИНИТЕ ДРОНОВ [E] (+20%)!", Rgb(72, 215, 255), 3.6f);
                    break;
                }

                case "daitaku_helios":
                {
                    _state = 1;
                    _subPhase = 0;
                    _stepCount = 0;
                    _timer = 24.0f;
                    _progress = 100f;
                    _anchorPlayerPos = g.PlayerPos;
                    ClearSecondaryNodes();
                    ClearSpawnedGuards(g);
                    SoundManager.Play("bounce_pad", 1.0f, 1.15f);
                    Vector3 baseDir = Position.Length() > 5f ? Vector3.Normalize(new Vector3(-Position.X, 0f, -Position.Z)) : -Vector3.UnitX;
                    Vector3 perpDir = new(-baseDir.Z, 0f, baseDir.X);
                    for (int i = 0; i < 6; i++)
                    {
                        float d = (i + 1) * 9.2f;
                        float wave = MathF.Sin((i + 1) * 1.18f) * 8.5f;
                        Vector3 gp = Physics.FindSafeSpawnPoint(Position + baseDir * d + perpDir * wave);
                        string lbl = i == 5 ? "ФИНИШ «WEI~★!» #6" : $"НЕОНОВАЯ АРКА #{i + 1}/6";
                        var gate = new QuestSecondaryNode(gp, this, i, lbl, Rgb(72, 225, 255), QuestNodeMode.RaceGate);
                        _secondaryNodes.Add(gate);
                        g.StageEntities.Add(gate);
                    }
                    g.ApplyDash(g.PlayerForward * 22.0f, 0.25f);
                    g.ShowBanner("ГЯРУ-СПРИНТ «WEI~★!»: ПРОЛЕТИТЕ 6 НЕОНОВЫХ АРОК И НЕ УРОНИТЕ ШКАЛУ ДРАЙВА!", Rgb(72, 225, 255), 3.6f);
                    break;
                }

                case "robin":
                {
                    _state = 1;
                    _subPhase = 0;
                    _stepCount = 0;
                    _progress = 0f;
                    ClearSecondaryNodes();
                    if (_zoneRing != null) _zoneRing.Enabled = true;
                    SoundManager.Play("magic_cast", 1.05f, 1.12f);
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = i * (MathF.PI * 2f / 3f) + 0.4f;
                        Vector3 fp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(ang) * 8.0f, 0f, MathF.Sin(ang) * 8.0f));
                        var fork = new ExpansionQuestNode(fp, this, i, "harmony_fork", $"[Камертон Гармонии ♫ #{i + 1}/3]", Rgb(225, 185, 255), ExpNodeBehavior.TouchCollect, 100f, "magic_cast");
                        _secondaryNodes.Add(fork);
                        g.StageEntities.Add(fork);
                    }
                    SpawnTrialEnemies(g, 3, "wisp", 9.5f);
                    AddTrialEnemies(g, 1, "brass_contraption", 10.0f);
                    g.ShowBanner("КОНЦЕРТ ГАРМОНИИ ♫: ПОЙТЕ В КРУГЕ СЦЕНЫ, СОБЕРИТЕ КАМЕРТОНЫ И ОДОЛЕЙТЕ ВЕЛИКОГО СЕПТИМИЯ!", Color.Gold, 3.8f);
                    break;
                }

                case "komeiji":
                {
                    _state = 1;
                    _subPhase = 0;
                    _stepCount = 0;
                    _timer = 14.0f;
                    _auxTimer = 0.55f;
                    _progress = 0f;
                    _cipherCode[0] = 3;
                    _cipherCode[1] = 0;
                    _lastPlayerTotalHp = g.PlayerHp + curShield;
                    ClearSecondaryNodes();
                    ClearSpawnedGuards(g);
                    if (_zoneRing != null) _zoneRing.Enabled = true;
                    SoundManager.Play("charm_heart", 1.05f, 1.08f);
                    for (int i = 0; i < 4; i++)
                    {
                        float ang = i * (MathF.PI * 0.5f) + 0.4f;
                        Vector3 rp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(ang) * 7.8f, 0f, MathF.Sin(ang) * 7.8f));
                        var rose = new ExpansionQuestNode(rp, this, i, "subconscious_rose", $"[Роза Подсознания #{i + 1}/4]", Rgb(78, 245, 185), ExpNodeBehavior.TouchCollect, 100f, "charm_heart");
                        _secondaryNodes.Add(rose);
                        g.StageEntities.Add(rose);
                    }
                    g.ShowBanner("СПЕЛЛ-КАРТА ТРЕТЬЕГО ГЛАЗА: УКЛОНЯЙТЕСЬ ОТ ДАНМАКУ РОЗ И СОБИРАЙТЕ РОЗЫ ПОДСОЗНАНИЯ!", Rgb(245, 95, 165), 3.6f);
                    break;
                }

                case "false_son":
                {
                    _state = 1;
                    _subPhase = 0;
                    _stepCount = 0;
                    _auxTimer = 2.8f;
                    _cipherCode[0] = 0;
                    ClearSecondaryNodes();
                    SoundManager.Play("zap", 1.05f, 0.88f);
                    new LightningBeam(Position + new Vector3(0, 18f, 0), Position + new Vector3(0, 1.5f, 0), Color.Gold, 0.42f);
                    new VisualFX(Position + new Vector3(0, 1.8f, 0), Color.Gold, 7.5f, 0.38f);
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = i * (MathF.PI * 2f / 3f) + 0.35f;
                        Vector3 gp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(ang) * 9.5f, 0f, MathF.Sin(ang) * 9.5f));
                        var geode = new ExpansionQuestNode(gp, this, i, "halcyon_geode", $"Жеода Алкиона #{i + 1}/3", Color.Gold, ExpNodeBehavior.Breakable, 250f, "slam_land");
                        _secondaryNodes.Add(geode);
                        g.StageEntities.Add(geode);
                    }
                    SpawnTrialEnemies(g, 2, "golem", 9.5f);
                    g.ShowBanner("ГЛАВНЫЙ МЕРИДИАН: РАЗБЕЙТЕ 3 ЖЕОДЫ АЛКИОНА ПОД МОЛНИЯМИ БУРИ И СРАЗИТЕ ЛОЖНОГО СЫНА!", Color.Gold, 3.6f);
                    break;
                }
            }
        }

        private void OnExpansionQuestNodeTouched(IPlayerContext g, int idx)
        {
            if (_state != 1) return;

            switch (CharKey)
            {
                case "susie":
                    if (_subPhase == 0)
                    {
                        new LightningBeam(Position + new Vector3(0, 2.2f, 0), g.PlayerPos + new Vector3(0, 1.2f, 0), Rgb(235, 65, 165), 0.24f);
                        if (_stepCount >= 3)
                        {
                            _subPhase = 1;
                            _auxTimer = 4.2f;
                            ClearSecondaryNodes();
                            SoundManager.Play("boss_roar", 1.1f, 0.90f);
                            SpawnScaledExpansionBoss(g, "roaring_knight", "РОКОЧУЩИЙ РЫЦАРЬ", 0.80f);
                            g.ShowBanner("ПЕЧАТИ ФОНТАНА СОРВАНЫ! ПОБЕДИТЕ БОССА — РОКОЧУЩЕГО РЫЦАРЯ!", Rgb(235, 72, 168), 3.6f);
                        }
                        else
                        {
                            g.ShowBanner($"ПЕЧАТЬ ТЁМНОГО ФОНТАНА РАЗРУШЕНА ({_stepCount}/3)!", Rgb(235, 72, 168), 1.8f);
                        }
                    }
                    break;

                case "spamton":
                    if (_subPhase == 0)
                    {
                        g.ApplyDash(g.PlayerForward * 14.5f, 0.16f);
                        _timer = MathF.Min(24.0f, _timer + 3.0f);
                        if (_stepCount >= 4)
                        {
                            _subPhase = 1;
                            _stepCount = 0;
                            _timer = 24.0f;
                            ClearSecondaryNodes();
                            SoundManager.Play("ult_cast", 1.0f, 1.15f);
                            for (int i = 0; i < 3; i++)
                            {
                                float ang = i * (MathF.PI * 2f / 3f) + 0.65f;
                                Vector3 pp = Physics.FindSafeSpawnPoint(Position + new Vector3(MathF.Cos(ang) * 8.5f, 0f, MathF.Sin(ang) * 8.5f));
                                var pipis = new ExpansionQuestNode(pp, this, i, "mega_pipis", $"Мега-[Pipis] #{i + 1}/3", Rgb(48, 195, 255), ExpNodeBehavior.Breakable, 210f, "explosion");
                                _secondaryNodes.Add(pipis);
                                g.StageEntities.Add(pipis);
                            }
                            AddTrialEnemies(g, 2, "combine_soldier", 9.5f, elite: true);
                            g.ShowBanner("КОНТРАКТ [[НЕО]] АКТИВИРОВАН! РАЗБЕЙТЕ 3 МЕГА-[PIPIS] И КИБЕР-ОХРАНУ!", Rgb(255, 235, 65), 3.5f);
                        }
                        else
                        {
                            g.ShowBanner($"ВКУСНЯТИНА [[КРОМЕР]] ПОЛУЧЕНА ({_stepCount}/4)!", Rgb(255, 235, 65), 1.8f);
                        }
                    }
                    else
                    {
                        _timer = MathF.Min(24.0f, _timer + 3.5f);
                        if (_stepCount >= 3)
                        {
                            ClearSecondaryNodes();
                            g.ShowBanner("ВСЕ МЕГА-[PIPIS] ВЗОРВАНЫ! ДОБЕЙТЕ КИБЕР-ОХРАНУ!", Rgb(48, 195, 255), 2.5f);
                        }
                        else
                        {
                            g.ShowBanner($"МЕГА-[PIPIS] ВЗОРВАН ({_stepCount}/3)!", Rgb(48, 195, 255), 1.8f);
                        }
                    }
                    break;

                case "binah":
                    if (_subPhase == 0)
                    {
                        new LightningBeam(Position + new Vector3(0, 2.4f, 0), g.PlayerPos + new Vector3(0, 1.2f, 0), Rgb(255, 208, 68), 0.22f);
                        if (_stepCount >= 3)
                        {
                            _subPhase = 1;
                            ClearSecondaryNodes();
                            SoundManager.Play("dante_clock", 1.1f, 0.95f);
                            SoundManager.Play("boss_roar", 1.05f, 0.88f);
                            SpawnScaledExpansionBoss(g, "binah_arbiter", "АРБИТР БИНА", 0.80f);
                            g.ShowBanner("ОКОВЫ ЗАМКА РАСКОЛОТЫ! ВЫДЕРЖИТЕ СУД БОССА — АРБИТРА БИНЫ!", Rgb(255, 208, 68), 3.6f);
                        }
                        else
                        {
                            g.ShowBanner($"ЗОЛОТАЯ ЦЕПЬ ЗАМКА СОРВАНА ({_stepCount}/3)!", Rgb(255, 208, 68), 1.8f);
                        }
                    }
                    break;

                case "don_quixote":
                    if (_subPhase == 0)
                    {
                        if (_stepCount >= 5)
                        {
                            _subPhase = 1;
                            _timer = 24.0f;
                            ClearSecondaryNodes();
                            SoundManager.Play("boss_roar", 1.0f, 1.10f);
                            SpawnTrialEnemies(g, 1, "sarkaz_mercenary", 8.5f, elite: true, hpMultiplier: 2.30f);
                            AddTrialEnemies(g, 3, "imp", 7.5f);
                            g.ShowBanner("ГАЛОП РОСИНАНТА ЗАВЕРШЁН! СРАЗИТЕ РЫЦАРЯ-СОПЕРНИКА В ФИНАЛЕ ТУРНИРА!", Color.Gold, 3.5f);
                        }
                        else
                        {
                            g.ShowBanner($"ТУРНИРНЫЙ БАРЬЕР ПРОЙДЕН ({_stepCount}/5)! ВПЕРЁД, К МЕЧТЕ!", Color.Gold, 1.8f);
                        }
                    }
                    break;

                case "priestess":
                    if (_subPhase == 0)
                    {
                        _timer = MathF.Min(24.0f, _timer + 3.5f);
                        new LightningBeam(Position + new Vector3(0, 2.5f, 0), g.PlayerPos + new Vector3(0, 1.3f, 0), Rgb(255, 58, 72), 0.22f);
                        if (_stepCount >= 4)
                        {
                            _subPhase = 1;
                            ClearSecondaryNodes();
                            SoundManager.Play("portal_open", 1.05f, 0.92f);
                            SoundManager.Play("boss_roar", 1.0f, 0.95f);
                            SpawnScaledExpansionBoss(g, "prts_source", "ИСТОЧНИК ОРИДЖИНИУМА PRTS", 0.80f);
                            g.ShowBanner("ВСЕ 4 УЗЛА PRTS ◇ СИНХРОНИЗИРОВАНЫ! ПРОБУДИЛСЯ ИСТОЧНИК ОРИДЖИНИУМА!", Rgb(255, 58, 72), 3.6f);
                        }
                        else
                        {
                            g.ShowBanner($"НЕЙРОННЫЙ УЗЕЛ PRTS ◇ СИНХРОНИЗИРОВАН ({_stepCount}/4)!", Rgb(255, 58, 72), 1.8f);
                        }
                    }
                    break;

                case "closure":
                    _progress = MathF.Min(100f, _progress + 20f);
                    _timer = MathF.Min(360f, _timer + 65f);
                    g.HealPlayer(g.PlayerMaxHp * 0.15f);
                    g.ShowBanner($"ДРОН КЛОЖУР ПОЧИНЕН ({_stepCount}/3): ПРОШИВКА +20%, СЕРВЕР +65 HP!", Rgb(72, 215, 255), 2.2f);
                    break;

                case "daitaku_helios":
                    _progress = MathF.Min(100f, _progress + 42f);
                    SoundManager.Play("bounce_pad", 0.9f, 1.05f + _stepCount * 0.06f);
                    if (_stepCount == 3)
                        AddTrialEnemies(g, 2, "lemurian", 9.0f);
                    if (_stepCount >= 6)
                        CompleteCharacterQuest(g);
                    else
                        g.ShowBanner($"АРКА «WEI~★!» #{_stepCount}/6 ПРОЙДЕНА! ДЕРЖИТЕ ДРАЙВ!", Rgb(72, 225, 255), 1.8f);
                    break;

                case "robin":
                    if (_subPhase == 0)
                    {
                        _progress = MathF.Min(100f, _progress + 18f);
                        g.HealPlayer(g.PlayerMaxHp * 0.15f);
                        g.ShowBanner($"КАМЕРТОН ГАРМОНИИ ♫ СОБРАН ({_stepCount}/3): РЕЗОНАНС +18%!", Rgb(225, 185, 255), 1.9f);
                    }
                    break;

                case "komeiji":
                    if (_subPhase == 0)
                    {
                        _timer = MathF.Max(0.2f, _timer - 2.2f);
                        g.HealPlayer(g.PlayerMaxHp * 0.12f);
                        _lastPlayerTotalHp = g.PlayerHp + (Entities.CombatManager.Controller?.Shield ?? 0f);
                        g.ShowBanner($"РОЗА ПОДСОЗНАНИЯ СОБРАНА ({_stepCount}/4): ДАНМАКУ УСКОРЕНО (-2.2С)!", Rgb(78, 245, 185), 1.9f);
                    }
                    break;

                case "false_son":
                    if (_subPhase == 0)
                    {
                        new LightningBeam(Position + new Vector3(0, 14f, 0), Position + new Vector3(0, 1.5f, 0), Color.Gold, 0.30f);
                        if (_stepCount >= 3)
                        {
                            _subPhase = 1;
                            _auxTimer = 3.4f;
                            _cipherCode[0] = 0;
                            ClearSecondaryNodes();
                            SoundManager.Play("slam_land", 1.15f, 0.85f);
                            SoundManager.Play("boss_roar", 1.1f, 0.88f);
                            SpawnScaledExpansionBoss(g, "false_son_boss", "ЛОЖНЫЙ СЫН, ДИТЯ БУРИ", 0.80f);
                            g.ShowBanner("ЖЕОДЫ АЛКИОНА ОЧИЩЕНЫ! СОКРУШИТЕ БОССА — ЛОЖНОГО СЫНА!", Color.Gold, 3.8f);
                        }
                        else
                        {
                            g.ShowBanner($"ЖЕОДА АЛКИОНА ОЧИЩЕНА ({_stepCount}/3)!", Color.Gold, 1.9f);
                        }
                    }
                    break;
            }
        }

        private void UpdateExpansionQuestTrial(IPlayerContext g, float dt, float dist, float curTotalHp)
        {
            switch (CharKey)
            {
                case "susie":
                    if (_subPhase == 0)
                    {
                        _timer -= dt;
                        _labelEnt.BillboardText = $"Печати Тёмного Фонтана: {_stepCount}/3  |  До Рокота: {_timer:F1}с";
                        if (_timer <= 0f)
                        {
                            ResetFailedExpansionTrial(g,
                                "[E] Алтарь Секиры Тёмного Мира (Рокот поглотил зал! Нажмите [E] снова)",
                                "ТЁМНЫЙ ФОНТАН ПЕРЕПОЛНИЛСЯ! РАЗБЕЙТЕ ВСЕ 3 ПЕЧАТИ БЫСТРЕЕ!");
                        }
                    }
                    else
                    {
                        _auxTimer -= dt;
                        if (_auxTimer <= 0f)
                        {
                            _auxTimer = 4.5f;
                            SoundManager.Play("magic_cast", 0.65f, 0.92f);
                            float spin = (float)Raylib.GetTime() * 1.4f;
                            for (int i = 0; i < 6; i++)
                            {
                                float ang = spin + i * (MathF.PI * 2f / 6f);
                                Vector3 dir = new(MathF.Cos(ang), 0f, MathF.Sin(ang));
                                new Entities.Projectile(Position + new Vector3(0, 1.4f, 0), dir, 13.0f, 8.5f, 0.38f,
                                    Rgb(75, 245, 245), isPlayer: false, canProc: false, maxLifetime: 2.3f);
                            }
                        }

                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Сразите Рокочущего Рыцаря [{(int)MathF.Max(1f, _spawnedGuards[0].Hp)} HP]!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "spamton":
                    _timer -= dt;
                    if (_subPhase == 0)
                    {
                        _auxTimer -= dt;
                        if (_auxTimer <= 0f)
                        {
                            _auxTimer = 2.4f;
                            SoundManager.Play("coin_flip", 0.65f, 1.25f);
                            float baseAng = (float)Raylib.GetTime() * 1.8f;
                            for (int i = 0; i < 4; i++)
                            {
                                float ang = baseAng + i * (MathF.PI * 0.5f);
                                Vector3 dir = new(MathF.Cos(ang), 0f, MathF.Sin(ang));
                                new Entities.Projectile(Position + new Vector3(0, 1.5f, 0), dir, 13.5f, 8.0f, 0.42f,
                                    Rgb(48, 195, 255), isPlayer: false, canProc: false, maxLifetime: 2.2f);
                            }
                        }
                        _labelEnt.BillboardText = $"Горячая линия [[1997]]: Сделки [E] {_stepCount}/4  |  Таймер: {_timer:F1}с";
                    }
                    else
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_stepCount >= 3 && _spawnedGuards.Count == 0)
                        {
                            CompleteCharacterQuest(g);
                            return;
                        }
                        _labelEnt.BillboardText = $"Перегрузка [[НЕО]]: Мега-[Pipis] {_stepCount}/3  |  Охрана: {_spawnedGuards.Count} ({_timer:F1}с)";
                    }

                    if (_timer <= 0f)
                    {
                        ResetFailedExpansionTrial(g,
                            "[E] Телефонный Автомат [[1997]] (Линия оборвалась! Нажмите [E] снова)",
                            "СДЕЛКА СОРВАЛАСЬ — [[СВЯЗЬ ПОТЕРЯНА]]! ПОПРОБУЙТЕ СНОВА!");
                    }
                    break;

                case "binah":
                    if (_subPhase == 0)
                    {
                        _timer -= dt;
                        if (dist > 2.4f && dist < 14.0f)
                        {
                            Vector3 pull = new(Position.X - g.PlayerPos.X, 0f, Position.Z - g.PlayerPos.Z);
                            if (pull.LengthSquared() > 0.01f)
                            {
                                pull = Vector3.Normalize(pull);
                                g.PlayerPos = Physics.ResolveHorizontalCollision(g.PlayerPos + pull * 2.8f * dt, 0.72f, g.PlayerPos.Y);
                            }
                        }
                        _labelEnt.BillboardText = $"Оковы Сингулярности [Замок]: {_stepCount}/3  |  До сжатия: {_timer:F1}с";
                        if (_timer <= 0f)
                        {
                            ResetFailedExpansionTrial(g,
                                "[E] Чёрно-Золотой Столп Арбитра (Замок сомкнулся! Нажмите [E] снова)",
                                "ЗОЛОТЫЕ ОКОВЫ ЗАМКА ПОЛНОСТЬЮ СОМКНУЛИСЬ! РАЗБЕЙТЕ ИХ БЫСТРЕЕ!");
                        }
                    }
                    else
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Поединок с Арбитром Главы — Бина [{(int)MathF.Max(1f, _spawnedGuards[0].Hp)} HP]!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "don_quixote":
                    _timer -= dt;
                    if (_subPhase == 0)
                    {
                        _labelEnt.BillboardText = $"Рыцарский Турнир Ла-Манчаленда: Барьеры {_stepCount}/5 ({_timer:F1}с)";
                    }
                    else
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count == 0)
                        {
                            CompleteCharacterQuest(g);
                            return;
                        }
                        _labelEnt.BillboardText = $"Финал Турнира Мечты: осталось соперников {_spawnedGuards.Count} ({_timer:F1}с)!";
                    }

                    if (_timer <= 0f)
                    {
                        ResetFailedExpansionTrial(g,
                            "[E] Копьё Ла-Манчаленда (Турнир окончен! Нажмите [E] для нового заезда)",
                            "ВРЕМЯ РЫЦАРСКОГО ТУРНИРА ИСТЕКЛО! ПОПРОБУЙТЕ СНОВА!");
                    }
                    break;

                case "priestess":
                    if (_subPhase == 0)
                    {
                        _timer -= dt;
                        _labelEnt.BillboardText = $"Синхронизация Узлов PRTS ◇ [E]: {_stepCount}/4  |  Связь: {_timer:F1}с";
                        if (_timer <= 0f)
                        {
                            ResetFailedExpansionTrial(g,
                                "[E] Алтарь «Глаза Пристэсс» ◇ (Сбой связи! Нажмите [E] снова)",
                                "НЕЙРОННАЯ СИНХРОНИЗАЦИЯ PRTS ПРЕРВАНА! ПОПРОБУЙТЕ СНОВА!");
                        }
                    }
                    else
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Усмирите Источник Ориджиниума PRTS [{(int)MathF.Max(1f, _spawnedGuards[0].Hp)} HP]!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "closure":
                    _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                    if (dist <= 11.2f)
                    {
                        float prev = _progress;
                        _progress = MathF.Min(100f, _progress + (100f / 14.0f) * dt);
                        if (prev < 55f && _progress >= 55f && _subPhase == 0)
                        {
                            _subPhase = 1;
                            AddTrialEnemies(g, 2, "combine_soldier", 10.5f, elite: true);
                            AddTrialEnemies(g, 1, "brass_contraption", 10.0f);
                            g.ShowBanner("55% ПРОШИВКИ ЗАГРУЖЕНО! НАЛЁТ МАРОДЁРОВ НА МАСТЕРСКУЮ КЛОЖУР!", Color.Orange, 2.6f);
                        }
                    }
                    else
                    {
                        _progress = MathF.Max(0f, _progress - 6.0f * dt);
                    }

                    foreach (var e in Entities.CombatManager.Enemies)
                    {
                        if (IsValid(e) && !e.IsAlly && !e.IsTrainingDummy && Vector3.Distance(e.Position, Position) <= 3.8f)
                        {
                            _timer -= 24f * dt;
                            if ((int)((_timer + 24f * dt) / 25f) != (int)(_timer / 25f))
                                new VisualFX(Position + new Vector3(0, 1.4f, 0), Color.Red, 2.8f, 0.2f);
                        }
                    }

                    if (_timer <= 0f)
                    {
                        ResetFailedExpansionTrial(g,
                            "[E] Мастерская Кложур (Сервер повреждён! Нажмите [E] для перезапуска)",
                            "СЕРВЕР ИНЖЕНЕРНОГО ОТСЕКА КЛОЖУР РАЗБИТ! ПОПРОБУЙТЕ СНОВА!");
                        return;
                    }

                    _labelEnt.BillboardText = _progress < 100f
                        ? $"Прошивка Кложур: {(int)_progress}%  |  Сервер: {(int)_timer}/360 HP  |  Дроны [E]: {_stepCount}/3"
                        : $"Прошивка 100%! Добейте мародёров у мастерской ({_spawnedGuards.Count})!";

                    if (_progress >= 100f && _spawnedGuards.Count == 0)
                        CompleteCharacterQuest(g);
                    break;

                case "daitaku_helios":
                    _timer -= dt;
                    float moved = Vector3.Distance(g.PlayerPos, _anchorPlayerPos);
                    _anchorPlayerPos = g.PlayerPos;
                    if (moved > 4.2f * dt || !g.PlayerGrounded)
                        _progress = MathF.Max(0f, _progress - 8.5f * dt);
                    else
                        _progress = MathF.Max(0f, _progress - 26.0f * dt);

                    _labelEnt.BillboardText = $"Гяру-Спринт «WEI~★!»: Арки {_stepCount}/6  |  Драйв: {(int)_progress}% ({_timer:F1}с)";
                    if (_timer <= 0f || _progress <= 0f)
                    {
                        ResetFailedExpansionTrial(g,
                            "[E] Турникет «WEI~★!» (Драйв упал! Нажмите [E] для нового старта)",
                            _progress <= 0f
                                ? "ДРАЙВ ВЕЧЕРИНКИ УПАЛ ДО 0%! БЕГИТЕ БЕЗ ОСТАНОВОК ЧЕРЕЗ АРКИ!"
                                : "ВРЕМЯ ГЯРУ-СПРИНТА «WEI~★!» ИСТЕКЛО!");
                    }
                    break;

                case "robin":
                    if (_subPhase == 0)
                    {
                        if (dist <= 11.2f)
                            _progress = MathF.Min(100f, _progress + (100f / 12.0f) * dt);
                        else
                            _progress = MathF.Max(0f, _progress - 9.0f * dt);

                        _labelEnt.BillboardText = dist <= 11.2f
                            ? $"Ария Гармонии ♫: {(int)_progress}%  |  Камертоны: {_stepCount}/3"
                            : $"ВЕРНИТЕСЬ В КРУГ СЦЕНЫ ПЕНАКОНИИ! ({(int)_progress}%)";

                        if (_progress >= 100f)
                        {
                            _subPhase = 1;
                            ClearSecondaryNodes();
                            if (_zoneRing != null) _zoneRing.Enabled = false;
                            SoundManager.Play("ult_cast", 1.05f, 1.10f);
                            SoundManager.Play("boss_roar", 1.05f, 0.90f);
                            SpawnScaledExpansionBoss(g, "great_septimus", "«ВЕЛИКИЙ СЕПТИМИЙ», ХОР ПОРЯДКА", 0.80f);
                            g.ShowBanner("КУЛЬМИНАЦИЯ ОПЕРЫ! СРАЗИТЕ БОССА — ДИРИЖЁРА «ВЕЛИКОГО СЕПТИМИЯ»!", Color.Gold, 3.8f);
                        }
                    }
                    else
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Финальный Акт Пенаконии — Великий Септимий [{(int)MathF.Max(1f, _spawnedGuards[0].Hp)} HP]!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "komeiji":
                    if (_subPhase == 0)
                    {
                        _progress = MathF.Max(0f, _progress - dt);
                        if (dist > 14.0f)
                        {
                            ResetFailedExpansionTrial(g,
                                "[E] Алтарь Третьего Глаза (Вы покинули круг! Нажмите [E] снова)",
                                "ВЫ ПОКИНУЛИ КРУГ ТРЕТЬЕГО ГЛАЗА! ДАНМАКУ СБРОШЕНО!");
                            return;
                        }

                        if (curTotalHp < _lastPlayerTotalHp - 0.5f && _progress <= 0f)
                        {
                            _cipherCode[0]--;
                            _progress = 0.85f;
                            SoundManager.Play("ui_error", 0.85f);
                            if (_cipherCode[0] <= 0)
                            {
                                ResetFailedExpansionTrial(g,
                                    "[E] Алтарь Третьего Глаза (Разум прочитан! Нажмите [E] снова)",
                                    "ТРИ ПОПАДАНИЯ В ДАНМАКУ РОЗ! ПОПРОБУЙТЕ СНОВА!");
                                return;
                            }
                            g.ShowBanner($"КАСАНИЕ ЛЕПЕСТКА! ОСТАЛОСЬ НИТЕЙ ПОДСОЗНАНИЯ: {_cipherCode[0]}/3!", Rgb(245, 95, 165), 1.8f);
                        }
                        _lastPlayerTotalHp = curTotalHp;

                        _timer -= dt;
                        _auxTimer -= dt;
                        if (_auxTimer <= 0f)
                        {
                            _auxTimer = 0.85f;
                            _cipherCode[1]++;
                            SoundManager.Play("magic_cast", 0.60f, 1.22f);
                            bool koishiWave = (_cipherCode[1] % 2) == 0;
                            float offsetAng = _timer * (koishiWave ? -2.0f : 2.0f);
                            Color bCol = koishiWave ? Rgb(78, 245, 185) : Rgb(245, 68, 138);
                            for (int i = 0; i < 8; i++)
                            {
                                float ang = offsetAng + i * (MathF.PI * 2f / 8f);
                                Vector3 dir = new(MathF.Cos(ang), 0f, MathF.Sin(ang));
                                new Entities.Projectile(Position + new Vector3(0, 1.38f, 0), dir, 12.0f, 8.0f, 0.36f,
                                    bCol, isPlayer: false, canProc: false, maxLifetime: 2.3f);
                            }
                        }

                        _labelEnt.BillboardText = $"Данмаку Роз Комейдзи: {_timer:F1}с  |  Нити разума: {_cipherCode[0]}/3  |  Розы: {_stepCount}/4";
                        if (_timer <= 0f)
                        {
                            _subPhase = 1;
                            ClearSecondaryNodes();
                            if (_zoneRing != null) _zoneRing.Enabled = false;
                            SoundManager.Play("charm_heart", 1.05f, 1.0f);
                            SpawnTrialEnemies(g, 2, "hollow_shadow", 8.5f, elite: true, hpMultiplier: 1.35f);
                            g.ShowBanner("СПЕЛЛ-КАРТА ПРОЙДЕНА! УСМИРИТЕ ДУХОВ ДВОРЦА ЗЕМЛИ!", Rgb(245, 95, 165), 3.4f);
                        }
                    }
                    else
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Усмирите Духов Дворца Земли: осталось {_spawnedGuards.Count}!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;

                case "false_son":
                    _auxTimer -= dt;
                    if (_cipherCode[0] == 0 && _auxTimer <= 0f)
                    {
                        _cipherCode[0] = 1;
                        _auxTimer = 0.85f;
                        _anchorPlayerPos = g.PlayerPos;
                        SoundManager.Play("ui_tick", 0.75f, 1.35f);
                        new VisualFX(_anchorPlayerPos + new Vector3(0, 0.25f, 0), Rgb(90, 222, 255), 3.4f, 0.75f);
                    }
                    else if (_cipherCode[0] == 1 && _auxTimer <= 0f)
                    {
                        _cipherCode[0] = 0;
                        _auxTimer = _subPhase == 0 ? 3.2f : 4.2f;
                        SoundManager.Play("zap", 0.90f, 0.92f);
                        new LightningBeam(_anchorPlayerPos + new Vector3(0, 18f, 0), _anchorPlayerPos + new Vector3(0, 0.3f, 0), Rgb(90, 222, 255), 0.34f);
                        new VisualFX(_anchorPlayerPos + new Vector3(0, 0.6f, 0), Color.Gold, 4.2f, 0.30f);
                        if (Physics.Distance2D(g.PlayerPos, _anchorPlayerPos) <= 2.6f)
                            g.PlayerHp = MathF.Max(1f, g.PlayerHp - MathF.Max(8f, g.PlayerMaxHp * 0.10f));
                    }

                    if (_subPhase == 0)
                    {
                        _labelEnt.BillboardText = $"Очистите Жеоды Алкиона атаками: {_stepCount}/3  |  Уклоняйтесь от молний Бури!";
                    }
                    else
                    {
                        _spawnedGuards.RemoveAll(m => !IsValid(m) || m.Hp <= 0f || m.IsAlly);
                        if (_spawnedGuards.Count > 0)
                            _labelEnt.BillboardText = $"Дуэль на Главном Меридиане — Ложный Сын [{(int)MathF.Max(1f, _spawnedGuards[0].Hp)} HP]!";
                        else
                            CompleteCharacterQuest(g);
                    }
                    break;
            }
        }
    }
}
