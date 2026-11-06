using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Models
{
    public partial class SurvivorParts
    {
        private void UpdateExpansionPack1Anims(string charKey, float t, float dt, bool isMoving, bool isGrounded, float strafeInput, float forwardInput)
        {
            float p = AnimDuration > 0.001f ? Math.Clamp(1f - AnimTimer / AnimDuration, 0f, 1f) : 1f;
            float swing = MathF.Sin(p * MathF.PI);
            bool inEmote = !string.IsNullOrEmpty(ActiveEmote);

            switch (charKey)
            {
                case "susie":
                {
                    if (Halo != null)
                        Halo.Rotation = new Vector3(0f, t * 85f, MathF.Sin(t * 3.2f) * 8f);
                    if (AnimTimer > 0f && !inEmote)
                    {
                        if (ActiveAnim is "m1" or "m2" && RightArm != null)
                        {
                            float dir = (AnimStep % 2 == 0) ? 1f : -1f;
                            RightArm.Rotation = BaseRightArmRot + new Vector3(-96f * swing, -24f * dir * swing, 22f * dir * swing);
                            if (Torso != null) Torso.Rotation = BaseTorsoRot + new Vector3(14f * swing, -18f * dir * swing, 0f);
                        }
                        else if (ActiveAnim == "shift" && Torso != null && RightArm != null)
                        {
                            Torso.Rotation = BaseTorsoRot + new Vector3(28f * swing, 0f, 0f);
                            RightArm.Rotation = BaseRightArmRot + new Vector3(36f * swing, 0f, 16f);
                        }
                    }
                    for (int i = 0; i < Tails.Count; i++)
                    {
                        Vector3 baseR = i < BaseTailRots.Count ? BaseTailRots[i] : Vector3.Zero;
                        Tails[i].Rotation = baseR + new Vector3((isMoving ? 14f : 4f) + MathF.Sin(t * 4.5f + i) * 6f, MathF.Cos(t * 3.1f + i) * 5f, 0f);
                    }
                    break;
                }

                case "spamton":
                {
                    if (Head != null && !inEmote)
                    {
                        if (MathF.Sin(t * 15.7f) > 0.90f)
                            Head.Rotation = BaseHeadRot + new Vector3(MathF.Sin(t * 47f) * 8f, MathF.Cos(t * 39f) * 11f, MathF.Sin(t * 53f) * 14f);
                    }
                    if (Halo != null)
                        Halo.Rotation = new Vector3(MathF.Sin(t * 2.4f) * 4f, MathF.Cos(t * 1.9f) * 6f, MathF.Sin(t * 3.1f) * 4f);
                    if (ShieldAura != null)
                        ShieldAura.Rotation = new Vector3(0f, 0f, MathF.Sin(t * 5.2f) * 18f);
                    if (AnimTimer > 0f && !inEmote && RightArm != null && ActiveAnim is "m1" or "m2")
                        RightArm.Rotation = BaseRightArmRot + new Vector3(-68f * swing, 0f, 0f);
                    for (int i = 0; i < Tails.Count; i++)
                    {
                        float side = (i % 2 == 0) ? -1f : 1f;
                        float flap = MathF.Sin(t * (UltFormActive ? 7.2f : 3.8f) + i * 0.5f) * (UltFormActive ? 24f : 15f);
                        Tails[i].Rotation = new Vector3(6f, side * (18f + flap), side * (8f + flap * 0.35f));
                    }
                    break;
                }

                case "binah":
                {
                    if (Halo != null)
                        Halo.Rotation = new Vector3(MathF.Sin(t * 1.4f) * 6f, t * 52f, MathF.Cos(t * 1.2f) * 6f);
                    if (ShieldAura != null)
                        ShieldAura.Rotation = new Vector3(MathF.Sin(t * 2.2f) * 12f, t * 68f, 10f);
                    if (AnimTimer > 0f && !inEmote && RightArm != null && ActiveAnim is "m1" or "m2" or "r")
                        RightArm.Rotation = BaseRightArmRot + new Vector3(-72f * swing, -14f * swing, 12f * swing);
                    for (int i = 0; i < Tails.Count; i++)
                    {
                        Vector3 baseR = i < BaseTailRots.Count ? BaseTailRots[i] : Vector3.Zero;
                        Tails[i].Rotation = baseR + new Vector3((isMoving ? 14f : 4f) + MathF.Sin(t * (isMoving ? 6.5f : 2.2f) + i * 0.8f) * (isMoving ? 9f : 3.5f), 0f, 0f);
                    }
                    break;
                }

                case "don_quixote":
                {
                    if (Halo != null)
                        Halo.Rotation = new Vector3(MathF.Sin(t * 2.8f) * 6f, t * 54f, MathF.Cos(t * 2.4f) * 6f);
                    if (ShieldAura != null)
                        ShieldAura.Rotation = new Vector3(MathF.Sin(t * 3.8f) * 8f, MathF.Cos(t * 2.5f) * 6f, MathF.Sin(t * 3.1f) * 6f);
                    if (AnimTimer > 0f && !inEmote && RightArm != null)
                    {
                        if (ActiveAnim is "m1" or "m2")
                        {
                            RightArm.Rotation = BaseRightArmRot + new Vector3(-78f * swing, -8f * swing, 0f);
                            if (Torso != null) Torso.Rotation = BaseTorsoRot + new Vector3(12f * swing, -14f * swing, 0f);
                        }
                        else if (ActiveAnim == "shift")
                        {
                            RightArm.Rotation = BaseRightArmRot + new Vector3(-84f, 0f, 0f);
                            if (Torso != null) Torso.Rotation = BaseTorsoRot + new Vector3(22f * swing, 0f, 0f);
                        }
                    }
                    for (int i = 0; i < Tails.Count; i++)
                    {
                        Vector3 baseR = i < BaseTailRots.Count ? BaseTailRots[i] : Vector3.Zero;
                        Tails[i].Rotation = baseR + new Vector3((isMoving ? 18f : 5f) + MathF.Sin(t * (isMoving ? 8.0f : 2.4f) + i * 0.7f) * (isMoving ? 11f : 4f), 0f, 0f);
                    }
                    break;
                }

                case "priestess":
                {
                    if (Halo != null)
                        Halo.Rotation = new Vector3(MathF.Sin(t * 1.5f) * 9f, t * 48f, MathF.Cos(t * 1.3f) * 9f);
                    if (ShieldAura != null)
                        ShieldAura.Rotation = new Vector3(MathF.Sin(t * 2.6f) * 12f, t * 80f, 0f);
                    if (AnimTimer > 0f && !inEmote && RightArm != null && ActiveAnim is "m1" or "m2")
                        RightArm.Rotation = BaseRightArmRot + new Vector3(-64f * swing, 10f * swing, 0f);
                    for (int i = 0; i < Tails.Count; i++)
                    {
                        Vector3 baseR = i < BaseTailRots.Count ? BaseTailRots[i] : Vector3.Zero;
                        Tails[i].Rotation = baseR + new Vector3((isMoving ? 14f : 4f) + MathF.Sin(t * 2.5f + i * 0.9f) * 5f, 0f, 0f);
                    }
                    break;
                }

                case "closure":
                {
                    if (Halo != null)
                    {
                        Halo.Position = new Vector3(-0.54f, 0.52f + MathF.Sin(t * 3.6f) * 0.06f, 0.08f);
                        Halo.Rotation = new Vector3(MathF.Sin(t * 3.0f) * 6f, MathF.Sin(t * 1.8f) * 22f, MathF.Cos(t * 2.7f) * 6f);
                    }
                    if (ShieldAura != null)
                        ShieldAura.Rotation = new Vector3(0f, t * 540f, 0f);
                    if (AnimTimer > 0f && !inEmote && RightArm != null && ActiveAnim is "m1" or "m2")
                        RightArm.Rotation = BaseRightArmRot + new Vector3(-66f * swing, -10f * swing, 8f * swing);
                    for (int i = 0; i < Tails.Count; i++)
                    {
                        float phase = t * (isMoving ? 7.2f : 3.4f) - i * 0.75f;
                        Tails[i].Rotation = new Vector3(14f + MathF.Cos(phase) * 8f, MathF.Sin(phase) * 20f, MathF.Sin(phase * 0.5f) * 8f);
                    }
                    break;
                }
            }
        }
    }

    public static partial class SurvivorDatabase
    {
        private static Entity Pack1Box(Entity parent, Vector3 scale, Vector3 pos, Color color, Vector3 rot = default, bool unlit = false, bool enabled = true)
            => new Entity(parent: parent, model: "cube", scale: scale, position: pos, rotation: rot, color: color, unlit: unlit, enabled: enabled);

        private static Entity Pack1Sphere(Entity parent, Vector3 scale, Vector3 pos, Color color, Vector3 rot = default, bool unlit = false, bool enabled = true)
            => new Entity(parent: parent, model: "sphere", scale: scale, position: pos, rotation: rot, color: color, unlit: unlit, enabled: enabled);

        private static Entity Pack1Diamond(Entity parent, Vector3 scale, Vector3 pos, Color color, Vector3 rot = default, bool unlit = false, bool enabled = true)
            => new Entity(parent: parent, model: "diamond", scale: scale, position: pos, rotation: rot, color: color, unlit: unlit, enabled: enabled);

        private static void RegisterExpansionPack1Classes()
        {
            Classes["susie"] = new SurvivorConfig
            {
                Key = "susie", Name = "СЬЮЗИ", Icon = "СЬЮЗ", Subtitle = "Свирепая Воительница Тёмного Мира",
                Hp = 175, Speed = 10.2f, Damage = 25.0f, FireRate = 2.8f, ProjSpeed = 54f, ProjScale = 0.34f, Aoe = 4.2f,
                BodyColor = Rgb(178, 88, 198), ArmorColor = Rgb(42, 22, 54), VisorColor = Rgb(255, 228, 72),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Секира Гривы", Cd = 0f, Desc = "Тяжёлые рубящие взмахи двуручной секирой Тёмного Мира с широкой дугой поражения." },
                    ["m2"] = new() { Name = "Rude Buster (Грубый Разрушитель)", Cd = 4.2f, Desc = "Выпускает пурпурную волну грубой магии TP, взрывающуюся при ударе." },
                    ["shift"] = new() { Name = "Плечом Напролом", Cd = 4.8f, Desc = "Свирепый таран вперёд сквозь врагов с отбрасыванием и временной бронёй." },
                    ["r"] = new() { Name = "UltimateHeal?! и Ярость", Cd = 10.5f, Desc = "Исцеляет ОЗ «целебной» магией Сьюзи и включает ярость крушителя с ускоренным Rude Buster." }
                }
            };

            Classes["spamton"] = new SurvivorConfig
            {
                Key = "spamton", Name = "СПАМТОН", Icon = "[[$]]", Subtitle = "Продавец #1 1997 Года [[БОЛЬШАЯ ШИШКА]]",
                Hp = 125, Speed = 10.5f, Damage = 17.5f, FireRate = 4.6f, ProjSpeed = 64f, ProjScale = 0.26f, Aoe = 3.6f,
                BodyColor = Rgb(245, 245, 248), ArmorColor = Rgb(22, 22, 28), VisorColor = Rgb(255, 95, 195),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Поток [$!$!] Слова", Cd = 0f, Desc = "Скорострельный обстрел спам-словами, Кроммером и рекламными снарядами." },
                    ["m2"] = new() { Name = "Синий [[ПИПИС]]", Cd = 4.0f, Desc = "Запускает прыгучие синие сферы Пипис, разлетающиеся взрывом выгодных сделок!" },
                    ["shift"] = new() { Name = "Глитч-Телепорт [[СДЕЛКА]]", Cd = 4.4f, Desc = "Рывок сквозь помехи киберсети с неуязвимостью и разрядом статики." },
                    ["r"] = new() { Name = "[[ГИПЕРССЫЛКА ЗАБЛОКИРОВАНА]]", Cd = 10.5f, Desc = "Безумный шквал рекламных окон, сердец и взрывных сделок вокруг врагов!" }
                }
            };

            Classes["binah"] = new SurvivorConfig
            {
                Key = "binah", Name = "БИНА", Icon = "БИНА", Subtitle = "Арбитр Главы и Покровительница Философии",
                Hp = 140, Speed = 10.0f, Damage = 22.0f, FireRate = 3.5f, ProjSpeed = 66f, ProjScale = 0.28f, Aoe = 4.5f,
                BodyColor = Rgb(22, 20, 26), ArmorColor = Rgb(218, 175, 68), VisorColor = Rgb(255, 212, 92),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Фея (Degraded Fairy)", Cd = 0f, Desc = "Росчерки золотых ключей Сингулярности, вскрывающие броню и оставляющие эффект Феи." },
                    ["m2"] = new() { Name = "Столп (Degraded Pillar)", Cd = 4.5f, Desc = "Призывает чёрно-золотой монолит и обрушивает его вперёд, сметая врагов на пути." },
                    ["shift"] = new() { Name = "Шаг Коридора", Cd = 4.6f, Desc = "Пространственный сдвиг сквозь золотой разлом с короткой неуязвимостью." },
                    ["r"] = new() { Name = "Замок и Ударная Волна", Cd = 11.0f, Desc = "Сковывает врагов золотыми замками и сотрясает поле боя всесокрушающей волной Арбитра." }
                }
            };

            Classes["don_quixote"] = new SurvivorConfig
            {
                Key = "don_quixote", Name = "ДОН КИХОТ", Icon = "ДОН★", Subtitle = "Доблестная Грешница #3 во Имя Справедливости!",
                Hp = 150, Speed = 11.0f, Damage = 20.5f, FireRate = 4.0f, ProjSpeed = 60f, ProjScale = 0.28f, Aoe = 3.8f,
                BodyColor = Rgb(245, 212, 85), ArmorColor = Rgb(32, 34, 44), VisorColor = Rgb(235, 45, 55),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Копьё Правосудия!", Cd = 0f, Desc = "Стремительная серия рыцарских выпадов турнирным копьём с набором Скорости." },
                    ["m2"] = new() { Name = "За Справедливость! (Рипост)", Cd = 4.0f, Desc = "Трёхкратный пробивной выпад с бонусом урона от текущей скорости бега!" },
                    ["shift"] = new() { Name = "Галоп Росинанта!", Cd = 4.2f, Desc = "Героический рыцарский рывок на полной скорости сквозь строй злодеев!" },
                    ["r"] = new() { Name = "La Sangre de Sancho", Cd = 10.5f, Desc = "Пронзает врагов гигантским багровым копьём E.G.O, исцеляясь пролитой кровью!" }
                }
            };

            Classes["priestess"] = new SurvivorConfig
            {
                Key = "priestess", Name = "ПРИСТЕСС", Icon = "◇PR◇", Subtitle = "Создательница Ориджиниума и Голос PRTS",
                Hp = 125, Speed = 10.1f, Damage = 20.5f, FireRate = 3.8f, ProjSpeed = 68f, ProjScale = 0.26f, Aoe = 4.6f,
                BodyColor = Rgb(238, 240, 246), ArmorColor = Rgb(34, 36, 46), VisorColor = Rgb(165, 105, 235),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Кванты Ориджиниума ◇", Cd = 0f, Desc = "Выпускает чёрно-алые кристаллы-ромбы ◇, преломляющиеся между соседними целями." },
                    ["m2"] = new() { Name = "Свёртка Реальности", Cd = 4.4f, Desc = "Сжимает пространство в ромбовидный узел Ориджиниума, стягивая и дезинтегрируя врагов." },
                    ["shift"] = new() { Name = "Горизонт Событий", Cd = 4.6f, Desc = "Фазовое смещение сквозь информационный слой Ориджиниума с квантовым эхом." },
                    ["r"] = new() { Name = "EYESOFPRIESTESS: Ассимиляция", Cd = 11.0f, Desc = "Переписывает поле боя кристаллическим морем Ориджиниума ◇, поражая всех врагов." }
                }
            };

            Classes["closure"] = new SurvivorConfig
            {
                Key = "closure", Name = "КЛОЖЕР", Icon = "КЛОЖ", Subtitle = "Главный Вампир-Инженер «Родос Айленд»",
                Hp = 130, Speed = 10.4f, Damage = 17.5f, FireRate = 4.4f, ProjSpeed = 62f, ProjScale = 0.25f, Aoe = 3.9f,
                BodyColor = Rgb(36, 48, 82), ArmorColor = Rgb(245, 132, 42), VisorColor = Rgb(238, 52, 78),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Импульсный Ключ", Cd = 0f, Desc = "Стреляет высоковольтными разрядами из контроллера с поддержкой мини-дрона." },
                    ["m2"] = new() { Name = "Вызов Тактического Дрона", Cd = 4.2f, Desc = "Отправляет боевой дрон Родоса выжигать врагов лазером и укреплять щиты Кложер." },
                    ["shift"] = new() { Name = "Реактивный Кабель-Крюк", Cd = 4.4f, Desc = "Стремительный рывок на механическом дата-кабеле с электрошоковым следом." },
                    ["r"] = new() { Name = "Овердрайв Инженерного Отсека!", Cd = 10.5f, Desc = "Разгоняет Lancet-2 и лазерные дроны до предела: шквал лучей, щиты и вампирская подпитка!" }
                }
            };
        }

        private static void SetupExpansionPack1Loadouts()
        {
            SetupCharLoadout("susie",
                "Бунтарка из Светлого Мира, обретшая сокрушительную мощь в Тёмном Королевстве. Любую преграду решает взмахом Секиры Гривы и залпом Rude Buster.",
                "Непокорный Нрав", "Чем ниже здоровье Сьюзи, тем выше её урон и скорость взмахов секирой.",
                new SkillDef { Name = "Бросок Разрушительницы", Cd = 0.45f, Desc = "Метает вращающийся энергетический слепок секиры на среднюю дистанцию." },
                new SkillDef { Name = "Red Buster (Красный Разрушитель)", Cd = 7.5f, Desc = "Усиленный алый заряд Души и Сьюзи с огромным взрывным уроном по области." },
                new SkillDef { Name = "Сокрушительный Прыжок", Cd = 6.5f, Desc = "Высокий прыжок с обрушением секиры в землю и ударной волной." },
                new SkillDef { Name = "Буря Тёмного Фонтана", Cd = 15.0f, Desc = "Вызывает вихрь багрово-пурпурных волн Разрушителя вокруг Сьюзи." },
                new SkinDef { Name = "Светлый Мир (Куртка)", Desc = "Повседневный образ хулиганки из родного города.", UnlockedByDefault = true, BodyColor = Rgb(192, 118, 182), ArmorColor = Rgb(92, 40, 74), VisorColor = Rgb(255, 232, 95) },
                new SkinDef { Name = "Рыцарь Рёва Тьмы", Desc = "Доспехи Тёмного Фонтана и багровое пламя Разрушительницы.", UnlockAchKey = "ach_boss", UnlockReqText = "Победите Босса Телепорта", UnlockedByDefault = false, ChangesModel = true, BodyColor = Rgb(145, 58, 180), ArmorColor = Rgb(26, 20, 38), VisorColor = Rgb(255, 70, 120) });

            SetupCharLoadout("spamton",
                "Некогда лучший делец Кибермира 1997 года, жаждущий сорвать кукольные нити и стать [[БОЛЬШОЙ ШИШКОЙ]] любой ценой!",
                "Колебания Курса Кроммера", "Каждый выстрел случайно меняет множитель урона от скидки до критического [[ДЖЕКПОТА]]!",
                new SkillDef { Name = "Мини-Спамтоны", Cd = 0.28f, Desc = "Выпускает кусачих мини-кукол Спамтона, преследующих ближайшую цель." },
                new SkillDef { Name = "Телефонный Звонок", Cd = 6.0f, Desc = "Звонок из пустоты выпускает звуковые кольца статики и замедляет врагов." },
                new SkillDef { Name = "Прыжок из Мусорного Бака", Cd = 6.0f, Desc = "Взрывной вылет вверх с разбрасыванием Кроммера и помех под собой." },
                new SkillDef { Name = "Джекпот 1997!", Cd = 14.0f, Desc = "Лихорадка Кроммера: максимальная скорострельность и дождь из [[ПИПИС]]." },
                new SkinDef { Name = "Кроммер-Магнат 1997", Desc = "Золотой пиджак лучшего продавца 1997 года!", UnlockedByDefault = true, BodyColor = Rgb(252, 246, 238), ArmorColor = Rgb(198, 148, 36), VisorColor = Rgb(255, 95, 195) },
                new SkinDef
                {
                    Name = "Спамтон НЕО [Альт. Версия]", Desc = "Кибернетическое тело [[НЕО]] с гигантскими крыльями и собственным арсеналом!",
                    UnlockedByDefault = false, ChangesModel = true,
                    UnlockAchKey = "ach_persona_spamton_neo",
                    UnlockReqText = "Играя за Спамтона: Накопите $400 золота или соберите 10 предметов за забег, став [[БОЛЬШОЙ ШИШКОЙ]]",
                    BodyColor = Rgb(245, 90, 185), ArmorColor = Rgb(35, 28, 52), VisorColor = Rgb(255, 235, 65),
                    CustomSubtitle = "Кибер-Воплощение [[БОЛЬШОЙ ШИШКИ]]",
                    CustomPassiveName = "Нити Свободы НЕО",
                    CustomPassiveDesc = "Крылья НЕО повышают манёвренность, а попадания заряжают Сердце-Ядро и призывают Пипис.",
                    CustomSkillVariants = BuildCustomSkinSkillSet("Спамтон НЕО",
                        new SkillDef { Name = "Ручная Пушка НЕО", Cd = 0f, Desc = "Мощные плазменные залпы из кибернетической пушки руки НЕО." },
                        new SkillDef { Name = "Тройной Заряд [[BIG SHOT]]", Cd = 0.36f, Desc = "Веер из розово-жёлтых энергетических сфер повышенного калибра." },
                        new SkillDef { Name = "Шквал Мега-Пипис", Cd = 5.2f, Desc = "Выстреливает каскадом взрывных синих Пипис по навесной дуге." },
                        new SkillDef { Name = "Цепное Сердце НЕО", Cd = 6.2f, Desc = "Выпускает светящееся ядро-сердце на цепи, прошивающее врагов и лечащее НЕО." },
                        new SkillDef { Name = "Реактивный Полёт Крыльев", Cd = 5.5f, Desc = "Стремительный полёт на асимметричных крыльях НЕО с энергетическим следом." },
                        new SkillDef { Name = "Рывок Сквозь Нити", Cd = 6.0f, Desc = "Разрывает зелёные нити со вспышкой урона вокруг и рывком в небо." },
                        new SkillDef { Name = "Сила [[НЕО]] — Финальный Залп", Cd = 14.0f, Desc = "Полный овердрайв костюма НЕО: гигантский луч [[БОЛЬШОЙ ШИШКИ]] и вихрь снарядов!" },
                        new SkillDef { Name = "Ангельский Прожектор Свободы", Cd = 15.0f, Desc = "Взмывает на крыльях и обрушивает каскад лучей и Пипис по всей арене!" })
                });

            SetupCharLoadout("binah",
                "Бывший Арбитр Главы Гаррион, ныне спокойная хранительница Этажа Философии с чашечкой чая. Повелевает Сингулярностями Феи, Замка и Столпа.",
                "Сингулярность «Фея»", "Попадания накладывают стаки Феи: враги получают дополнительный урон при движении и последующих ударах.",
                new SkillDef { Name = "Золотые Ключи", Cd = 0.32f, Desc = "Веер из трёх самонаводящихся золотых ключей, вскрывающих защиту цели." },
                new SkillDef { Name = "Цепь (Degraded Chain)", Cd = 5.8f, Desc = "Золотые цепи пронзают область, стягивая и сковывая противников." },
                new SkillDef { Name = "Чайная Пауза Философии", Cd = 7.5f, Desc = "Глоток горячего чая даёт золотой барьер и ускоряет перезарядку Сингулярностей." },
                new SkillDef { Name = "Суд Арбитра Главы", Cd = 16.0f, Desc = "Обрушивает с небес серию гигантских чёрно-золотых Столпов по отмеченным Феей врагам." },
                new SkinDef { Name = "Покровительница Философии", Desc = "Мантия Библиотекаря Этажа Философии с тёплым золотым шитьём.", UnlockedByDefault = true, BodyColor = Rgb(42, 34, 28), ArmorColor = Rgb(224, 180, 76), VisorColor = Rgb(255, 220, 100) },
                new SkinDef { Name = "Гаррион — Арбитр Главы", Desc = "Первозданное облачение Арбитра А-Корпорации с короной Сингулярностей.", UnlockAchKey = "ach_boss", UnlockReqText = "Победите Босса Телепорта", UnlockedByDefault = false, ChangesModel = true, BodyColor = Rgb(16, 16, 20), ArmorColor = Rgb(248, 205, 88), VisorColor = Rgb(255, 238, 135) });

            SetupCharLoadout("don_quixote",
                "Неугомонная Грешница #3 компании Лимбус, сражающаяся во имя благородной Справедливости. Под кроссовками «Росинант» дремлет древняя кровь Санчо.",
                "Рыцарский Пыл Росинанта", "Движение и атаки накапливают Скорость, увеличивая урон выпадов и шанс крита.",
                new SkillDef { Name = "Турнирный Натиск", Cd = 0.30f, Desc = "Мощный колющий удар двумя руками с ударной волной воздуха впереди." },
                new SkillDef { Name = "Знамя Корректировщика", Cd = 6.0f, Desc = "Воодушевляющий клич героя: повышает скорость атаки и наносит урон вокруг." },
                new SkillDef { Name = "Прыжок Странствующего Рыцаря", Cd = 6.0f, Desc = "Высокий прыжок с пикирующим ударом копья остриём вниз." },
                new SkillDef { Name = "Sueno Imposible!", Cd = 15.0f, Desc = "Несбыточная Мечта: вихрь сияющих рыцарских копий и стремительный финальный таран!" },
                new SkinDef
                {
                    Name = "Lobotomy E.G.O::Lantern Дон Кихот", Desc = "Пушистое снаряжение Фонаря Феи: приманивает добычу огнём и исцеляется укусами!",
                    UnlockedByDefault = false, ChangesModel = true,
                    UnlockAchKey = "ach_persona_don_lantern",
                    UnlockReqText = "Играя за Дон Кихот: Восстановите суммарно 300 HP в бою за один забег",
                    BodyColor = Rgb(78, 118, 58), ArmorColor = Rgb(32, 42, 28), VisorColor = Rgb(195, 255, 95),
                    CustomSubtitle = "Охотница Лесного Фонаря E.G.O",
                    CustomPassiveName = "Приманка Хищного Фонаря",
                    CustomPassiveDesc = "Атаки накладывают Разрыв, а удары по раненым врагам восстанавливают здоровье.",
                    CustomSkillVariants = BuildCustomSkinSkillSet("Фонарь Дон Кихот",
                        new SkillDef { Name = "Зубастое Копьё Фонаря", Cd = 0f, Desc = "Рваные уколы костяным копьём Феи-Фонаря, накладывающие Разрыв." },
                        new SkillDef { Name = "Жадные Челюсти Леса", Cd = 0.34f, Desc = "Двойной укус меховых челюстей с вампиризмом от раненых целей." },
                        new SkillDef { Name = "Мерцающий Огонёк Приманки", Cd = 5.0f, Desc = "Фонарь над головой притягивает врагов и взрывается изумрудным светом." },
                        new SkillDef { Name = "Хруст Костей!", Cd = 5.8f, Desc = "Яростный укус в прыжке: огромный бонус урона и лечения по целям с Разрывом." },
                        new SkillDef { Name = "Засада в Чаще", Cd = 5.5f, Desc = "Ныряет в тень мха и выпрыгивает позади жертвы с барьером ОЗ." },
                        new SkillDef { Name = "Тропа Светлячка", Cd = 6.0f, Desc = "Быстрый рывок с оставлением следа целебной зелёной пыльцы." },
                        new SkillDef { Name = "Пиршество Феи-Фонаря", Cd = 13.5f, Desc = "Гигантская пасть из подземелья смыкается на врагах, исцеляя Дон Кихот!" },
                        new SkillDef { Name = "Вечный Свет Подлеска", Cd = 14.5f, Desc = "Аура хищного фонаря вокруг героини: непрерывный Разрыв и регенерация." })
                },
                new SkinDef
                {
                    Name = "Управляющая Ла-Манчаленда Дон Кихот", Desc = "Истинный облик Второго Сородича Санчо с алым копьём Затвердевшей Крови!",
                    UnlockedByDefault = false, ChangesModel = true,
                    UnlockAchKey = "ach_persona_don_manager",
                    UnlockReqText = "Играя за Дон Кихот: Пробуждение Санчо — победите 2 Боссов за забег или доберитесь до 4-го этапа",
                    BodyColor = Rgb(168, 24, 42), ArmorColor = Rgb(28, 16, 22), VisorColor = Rgb(255, 65, 85),
                    CustomSubtitle = "Второй Сородич Ла-Манчаленда",
                    CustomPassiveName = "Пиршество Затвердевшей Крови",
                    CustomPassiveDesc = "Поглощает Кровавый Пир при попаданиях, усиливая Копьё и алые крылья Санчо.",
                    CustomSkillVariants = BuildCustomSkinSkillSet("Санчо Ла-Манчаленда",
                        new SkillDef { Name = "Алое Копьё Затвердевшей Крови", Cd = 0f, Desc = "Аристократичные выпады кристаллическим копьём Второго Сородича." },
                        new SkillDef { Name = "Плеть Кровавого Парада", Cd = 0.30f, Desc = "Рассекающие багровые дуги большой дальности, накапливающие Кровавый Пир." },
                        new SkillDef { Name = "Расцветающая Кровь Ла-Манчи", Cd = 5.0f, Desc = "Взрывает кристаллы Затвердевшей Крови вокруг цели и дарует Кровавую Броню." },
                        new SkillDef { Name = "Вознесение Санчо", Cd = 6.0f, Desc = "Могучий восходящий прокол с расправлением алых крыльев и каскадом осколков." },
                        new SkillDef { Name = "Шаг Багрового Вальса", Cd = 5.2f, Desc = "Мгновенный кровавый росчерк сквозь врагов, оставляющий взрывные розы крови." },
                        new SkillDef { Name = "Полёт Второго Сородича", Cd = 6.0f, Desc = "Взмывает на крыльях Затвердевшей Крови и пикирует в гущу битвы." },
                        new SkillDef { Name = "Финал Парада Ла-Манчаленда!", Cd = 14.0f, Desc = "Пробуждает полную мощь Санчо: исполинское копьё Затвердевшей Крови сокрушает арену!" },
                        new SkillDef { Name = "Сфера Вечной Жажды", Cd = 15.0f, Desc = "Вихрь Кровавого Пира вытягивает жизнь всех врагов вокруг в пользу Дон Кихот." })
                });
            Classes["don_quixote"].Skins[0].Name = "Грешница #3 LCB";

            SetupCharLoadout("priestess",
                "Исследовательница Первой Цивилизации, стоявшая у истоков Ориджиниума и PRTS. Её фиалковые глаза с ромбами ◇ хранят память сквозь эпохи.",
                "Ассимилированная Вселенная ◇", "Поражённые враги кристаллизуются и при гибели рассыпаются резонансными осколками Ориджиниума.",
                new SkillDef { Name = "Луч Сингулярности PRTS", Cd = 0.34f, Desc = "Точечный векторный импульс информации, мгновенно прошивающий линию врагов." },
                new SkillDef { Name = "Призма Первой Цивилизации", Cd = 6.2f, Desc = "Размещает парящий ромб ◇, дублирующий выстрелы Пристесс по ближайшим врагам." },
                new SkillDef { Name = "Фазовый Ковчег", Cd = 6.8f, Desc = "Окружает себя геометрической сферой защиты и плавно воспаряет." },
                new SkillDef { Name = "Протокол Ковчега", Cd = 15.5f, Desc = "Синхронизация с PRTS: орбитальная решётка ромбов ◇ непрерывно выжигает врагов." },
                new SkinDef { Name = "Архив Саркофага", Desc = "Белоснежный костюм исследовательницы Первой Цивилизации.", UnlockedByDefault = true, BodyColor = Rgb(244, 246, 252), ArmorColor = Rgb(62, 68, 88), VisorColor = Rgb(192, 128, 255) },
                new SkinDef { Name = "Ассимилированная Вселенная ◇", Desc = "Кристаллический облик хозяйки Ориджиниума на пороге вечности.", UnlockAchKey = "ach_stage3", UnlockReqText = "Достигните 3-го этапа", UnlockedByDefault = false, ChangesModel = true, BodyColor = Rgb(26, 22, 34), ArmorColor = Rgb(215, 45, 78), VisorColor = Rgb(255, 85, 125) });

            SetupCharLoadout("closure",
                "Главный вампир-инженер «Родос Айленд», гениальный хакер, создательница Lancet-2 и хозяйка снабженческого отсека!",
                "Вампирский Оверклокинг", "Дрон Кложер подсвечивает уязвимости врагов, а электрические криты восстанавливают здоровье.",
                new SkillDef { Name = "Сварочная Дуга", Cd = 0.25f, Desc = "Короткозамкнутая плазменная дуга, перескакивающая между несколькими целями." },
                new SkillDef { Name = "Орбитальный Лазер Родоса", Cd = 6.4f, Desc = "Наводит с палубы корабля сфокусированный оранжево-алый лазерный удар." },
                new SkillDef { Name = "Форсаж Щитов", Cd = 6.0f, Desc = "Включает магнитный импульс Lancet-2, отталкивая врагов и ускоряя бег." },
                new SkillDef { Name = "Секретная Скидка 99%", Cd = 15.0f, Desc = "Сбрасывает экспериментальный контейнер снабжения со взрывом дронов и баффов!" },
                new SkinDef { Name = "Ночная Смена Родоса", Desc = "Неоново-синяя рабочая куртка для ночных испытаний с дронами.", UnlockedByDefault = true, BodyColor = Rgb(30, 38, 64), ArmorColor = Rgb(65, 225, 245), VisorColor = Rgb(255, 75, 115) },
                new SkinDef { Name = "Главный Вампир-Инженер ★", Desc = "Парадный кибер-плащ Каздела с золотыми дронами VIP-магазина.", UnlockAchKey = "ach_carnage", UnlockReqText = "Уничтожьте орду врагов", UnlockedByDefault = false, ChangesModel = true, BodyColor = Rgb(48, 20, 36), ArmorColor = Rgb(255, 158, 45), VisorColor = Rgb(255, 55, 85) });
        }

        private static void Pack1BuildStandardLimbs(
            Entity root, SurvivorParts parts,
            Color torsoCol, Color pantsCol, Color bootCol, Color skinCol, Color sleeveCol,
            float torsoY = 1.24f, Vector3? torsoScale = null, float headSize = 0.45f, float legY = 0.86f)
        {
            parts.Root = root;
            Vector3 tScale = torsoScale ?? new Vector3(0.46f, 0.54f, 0.28f);
            float hipX = MathF.Max(0.15f, tScale.X * 0.36f);
            float legLen = legY;

            // Ноги — пустые пивоты с масштабом (1, 1, 1), чтобы дочерние сегменты не сжимались!
            var leftLeg = new Entity(parent: root, position: new(-hipX, legY, 0f));
            var rightLeg = new Entity(parent: root, position: new(hipX, legY, 0f));
            foreach (var leg in new[] { leftLeg, rightLeg })
            {
                Pack1Sphere(leg, new(0.18f, 0.18f, 0.18f), Vector3.Zero, pantsCol);
                Pack1Box(leg, new(0.18f, legLen * 0.44f, 0.18f), new(0f, -legLen * 0.23f, 0f), pantsCol);
                Pack1Sphere(leg, new(0.16f, 0.16f, 0.16f), new(0f, -legLen * 0.46f, 0.01f), pantsCol);
                Pack1Box(leg, new(0.16f, legLen * 0.42f, 0.16f), new(0f, -legLen * 0.68f, 0f), pantsCol);
                Pack1Diamond(leg, new(0.20f, 0.14f, 0.32f), new(0f, -legLen + 0.06f, 0.05f), bootCol);
                Pack1Box(leg, new(0.19f, 0.06f, 0.30f), new(0f, -legLen + 0.03f, 0.05f), bootCol);
            }

            // Торс и голова — пустые пивоты с масштабом (1, 1, 1)!
            var torso = new Entity(parent: root, position: new(0f, torsoY, 0f));
            Pack1Box(torso, tScale, new(0f, 0.03f, 0f), torsoCol);
            Pack1Box(torso, new(tScale.X * 0.88f, 0.16f, tScale.Z * 0.92f), new(0f, -tScale.Y * 0.5f, 0f), pantsCol);
            Pack1Box(torso, new(0.14f, 0.14f, 0.14f), new(0f, tScale.Y * 0.5f + 0.05f, 0f), skinCol);

            float headY = tScale.Y * 0.5f + headSize * 0.56f;
            var head = new Entity(parent: torso, position: new(0f, headY, 0f));
            Pack1Sphere(head, new(headSize, headSize, headSize * 0.96f), Vector3.Zero, skinCol);

            // Руки — пустые пивоты с масштабом (1, 1, 1)!
            float armX = tScale.X * 0.5f + 0.10f;
            float armY = tScale.Y * 0.5f - 0.05f;
            var leftArm = new Entity(parent: torso, position: new(-armX, armY, 0f));
            var rightArm = new Entity(parent: torso, position: new(armX, armY, 0f));
            foreach (var arm in new[] { leftArm, rightArm })
            {
                Pack1Sphere(arm, new(0.18f, 0.18f, 0.18f), Vector3.Zero, sleeveCol);
                Pack1Box(arm, new(0.15f, 0.25f, 0.15f), new(0f, -0.14f, 0f), sleeveCol);
                Pack1Box(arm, new(0.135f, 0.23f, 0.135f), new(0f, -0.35f, 0f), sleeveCol);
                Pack1Sphere(arm, new(0.12f, 0.12f, 0.12f), new(0f, -0.46f, 0f), skinCol);
            }

            parts.Torso = torso;
            parts.Head = head;
            parts.LeftArm = leftArm;
            parts.RightArm = rightArm;
            parts.LeftLeg = leftLeg;
            parts.RightLeg = rightLeg;
        }

        private static bool TryBuildExpansionPack1(string charKey, Entity root, SurvivorConfig cfg, SurvivorParts parts, int skinIdx)
        {
            switch (charKey)
            {
                case "susie":
                {
                    Color skinCol = skinIdx == 1 ? Rgb(192, 118, 182) : (skinIdx == 2 ? Rgb(155, 72, 192) : Rgb(178, 88, 198));
                    Color vestCol = skinIdx == 1 ? Rgb(92, 40, 74) : (skinIdx == 2 ? Rgb(24, 18, 36) : Rgb(36, 20, 48));
                    Color shirtCol = skinIdx == 1 ? Rgb(235, 225, 238) : Rgb(185, 42, 128);
                    Color pantsCol = skinIdx == 1 ? Rgb(58, 72, 112) : Rgb(48, 24, 62);
                    Color maneCol = skinIdx == 2 ? Rgb(48, 16, 62) : Rgb(72, 22, 84);
                    Color goldCol = Rgb(248, 208, 65);
                    Color busterCol = skinIdx == 2 ? Rgb(255, 58, 108) : Rgb(238, 68, 215);
                    Color armSkin = skinIdx == 1 ? vestCol : skinCol;

                    Pack1BuildStandardLimbs(root, parts, vestCol, pantsCol, Rgb(26, 20, 32), skinCol, armSkin, 1.28f, new Vector3(0.52f, 0.56f, 0.32f), 0.45f, 0.90f);
                    var torso = parts.Torso!;
                    var head = parts.Head!;

                    // Жилет Тёмного Мира с малиновой майкой, ремнём с пряжкой-сердцем и золотыми шипами на плечах
                    Pack1Box(torso, new(0.22f, 0.52f, 0.34f), new(0f, 0.03f, 0.01f), shirtCol);
                    Pack1Box(torso, new(0.55f, 0.10f, 0.35f), new(0f, -0.22f, 0f), Rgb(24, 18, 30));
                    Pack1Diamond(torso, new(0.16f, 0.16f, 0.10f), new(0f, -0.22f, 0.18f), goldCol, unlit: true);
                    foreach (var (s, arm) in new[] { (-1f, parts.LeftArm!), (1f, parts.RightArm!) })
                    {
                        Pack1Box(arm, new(0.22f, 0.12f, 0.22f), new(0f, 0.04f, 0f), vestCol);
                        Pack1Diamond(arm, new(0.09f, 0.18f, 0.09f), new(s * 0.07f, 0.15f, 0f), goldCol, new(0f, 0f, s * -22f), unlit: true);
                        Pack1Box(arm, new(0.17f, 0.09f, 0.17f), new(0f, -0.39f, 0f), Rgb(24, 18, 30));
                        Pack1Diamond(arm, new(0.08f, 0.08f, 0.08f), new(s * 0.09f, -0.39f, 0f), goldCol);
                    }

                    // Вытянутая морда рептилии, клык, густая сливовая грива на левый глаз и горящий жёлтый правый глаз
                    Pack1Box(head, new(0.26f, 0.17f, 0.24f), new(0f, -0.07f, 0.20f), skinCol);
                    Pack1Box(head, new(0.22f, 0.03f, 0.16f), new(0f, -0.11f, 0.25f), Rgb(42, 16, 38));
                    Pack1Diamond(head, new(0.05f, 0.09f, 0.05f), new(-0.07f, -0.13f, 0.30f), Color.White);
                    Pack1Box(head, new(0.10f, 0.07f, 0.05f), new(0.10f, 0.03f, 0.22f), Rgb(255, 235, 65), unlit: true);
                    Pack1Box(head, new(0.035f, 0.06f, 0.06f), new(0.10f, 0.03f, 0.23f), Rgb(25, 15, 20));
                    Pack1Sphere(head, new(0.52f, 0.44f, 0.52f), new(0f, 0.10f, -0.03f), maneCol);
                    Pack1Diamond(head, new(0.26f, 0.34f, 0.24f), new(-0.11f, 0.03f, 0.18f), maneCol, new(10f, 0f, 16f));
                    foreach (int s in new[] { -1, 1 })
                    {
                        var manePivot = new Entity(parent: head, position: new(s * 0.12f, -0.04f, -0.18f), rotation: new(16f, 0f, s * -6f));
                        Pack1Diamond(manePivot, new(0.28f, 0.62f, 0.20f), new(0f, -0.24f, -0.04f), maneCol);
                        parts.Tails.Add(manePivot);
                    }

                    // Секира Гривы (Mane Axe) на пустом пивоте с масштабом (1, 1, 1)!
                    var axePivot = new Entity(parent: parts.RightArm!, position: new(0f, -0.45f, 0.10f), rotation: new(58f, 0f, 0f));
                    Pack1Box(axePivot, new(0.065f, 1.32f, 0.065f), new(0f, 0.42f, 0f), Rgb(58, 42, 68));
                    Pack1Box(axePivot, new(0.11f, 0.12f, 0.11f), new(0f, 0.78f, 0f), goldCol);
                    Pack1Sphere(axePivot, new(0.11f, 0.11f, 0.11f), new(0f, -0.22f, 0f), goldCol);
                    Pack1Diamond(axePivot, new(0.10f, 0.54f, 0.48f), new(0f, 0.86f, 0.18f), Rgb(140, 38, 128));
                    Pack1Diamond(axePivot, new(0.07f, 0.64f, 0.56f), new(0f, 0.88f, 0.24f), busterCol, unlit: true);
                    Pack1Diamond(axePivot, new(0.08f, 0.28f, 0.24f), new(0f, 0.86f, -0.14f), maneCol);

                    // Аура ульты Rude Buster (пустой пивот!)
                    var ultGroup = new Entity(parent: torso, position: Vector3.Zero, enabled: false);
                    Pack1Diamond(ultGroup, new(1.05f, 0.16f, 1.05f), new(0f, 0.05f, 0f), busterCol, new(0f, 25f, 14f), unlit: true);
                    Pack1Diamond(ultGroup, new(0.92f, 0.14f, 0.92f), new(0f, 0.16f, 0f), Rgb(255, 145, 235), new(0f, -25f, -14f), unlit: true);
                    var ultBlade = new Entity(parent: axePivot, position: Vector3.Zero, enabled: false);
                    Pack1Diamond(ultBlade, new(0.14f, 0.82f, 0.72f), new(0f, 0.88f, 0.28f), Rgb(255, 95, 195), unlit: true);
                    parts.UltFormNodes.Add(ultGroup);
                    parts.UltFormNodes.Add(ultBlade);
                    return true;
                }

                case "spamton":
                {
                    Color pinkLens = Rgb(255, 95, 195);
                    Color yellowLens = Rgb(255, 235, 65);
                    Color puppetWhite = Rgb(248, 248, 252);
                    Color hairBlack = Rgb(18, 18, 24);

                    if (skinIdx == 2)
                    {
                        // SPAMTON NEO: Высокий кибер-доспех, огромные асимметричные крылья, зелёные нити и Рука-Бластер!
                        Color neoDark = Rgb(28, 22, 42);
                        Color neoPink = Rgb(238, 72, 175);
                        Color neoViolet = Rgb(128, 52, 195);
                        Color wireGreen = Rgb(65, 245, 98);

                        Pack1BuildStandardLimbs(root, parts, neoDark, neoDark, neoPink, puppetWhite, neoViolet, 1.34f, new Vector3(0.48f, 0.60f, 0.30f), 0.44f, 0.94f);
                        var torso = parts.Torso!;
                        var head = parts.Head!;

                        // Острые наплечники и нагрудные V-пластины НЕО
                        Pack1Diamond(torso, new(0.62f, 0.32f, 0.34f), new(0f, 0.18f, 0.02f), neoPink);
                        Pack1Diamond(parts.LeftArm!, new(0.24f, 0.42f, 0.20f), new(-0.08f, 0.12f, 0f), neoPink, new(0f, 0f, 32f));
                        Pack1Diamond(parts.RightArm!, new(0.24f, 0.42f, 0.20f), new(0.08f, 0.12f, 0f), yellowLens, new(0f, 0f, -32f), unlit: true);
                        Pack1Diamond(parts.LeftLeg!, new(0.16f, 0.24f, 0.20f), new(0f, -0.42f, 0.09f), yellowLens, unlit: true);
                        Pack1Diamond(parts.RightLeg!, new(0.16f, 0.24f, 0.20f), new(0f, -0.42f, 0.09f), neoPink, unlit: true);

                        // Светящееся Сердце на Цепи в груди НЕО (пустой пивот!)
                        var heartPivot = new Entity(parent: torso, position: new(0f, 0.12f, 0.19f));
                        Pack1Diamond(heartPivot, new(0.24f, 0.24f, 0.14f), Vector3.Zero, Rgb(95, 245, 255), unlit: true);
                        Pack1Diamond(heartPivot, new(0.14f, 0.14f, 0.18f), Vector3.Zero, pinkLens, unlit: true);
                        parts.ShieldAura = heartPivot;

                        // Голова НЕО: зачёсанные чёрные волосы, кукольная челюсть, длинный острый нос и очки DealMaker
                        Pack1Sphere(head, new(0.48f, 0.36f, 0.50f), new(0f, 0.14f, -0.04f), hairBlack);
                        Pack1Diamond(head, new(0.32f, 0.22f, 0.38f), new(0f, 0.22f, -0.14f), hairBlack, new(-20f, 0f, 0f));
                        Pack1Box(head, new(0.24f, 0.13f, 0.22f), new(0f, -0.18f, 0.06f), puppetWhite);
                        Pack1Box(head, new(0.24f, 0.035f, 0.18f), new(0f, -0.11f, 0.12f), hairBlack);
                        Pack1Diamond(head, new(0.08f, 0.08f, 0.36f), new(0f, 0.0f, 0.32f), puppetWhite, new(90f, 0f, 0f));
                        Pack1Sphere(head, new(0.15f, 0.14f, 0.07f), new(-0.10f, 0.05f, 0.22f), pinkLens, unlit: true);
                        Pack1Sphere(head, new(0.15f, 0.14f, 0.07f), new(0.10f, 0.05f, 0.22f), yellowLens, unlit: true);
                        Pack1Box(head, new(0.34f, 0.035f, 0.05f), new(0f, 0.05f, 0.21f), hairBlack);

                        // Исполинские асимметричные крылья Спамтона НЕО (пустые пивоты!)
                        var wingL = new Entity(parent: torso, position: new(-0.18f, 0.22f, -0.16f));
                        Pack1Diamond(wingL, new(1.05f, 0.34f, 0.09f), new(-0.54f, 0.32f, -0.06f), neoPink, new(0f, 0f, 24f), unlit: true);
                        Pack1Diamond(wingL, new(0.82f, 0.24f, 0.09f), new(-0.44f, 0.04f, -0.04f), yellowLens, new(0f, 0f, 8f), unlit: true);
                        Pack1Diamond(wingL, new(0.62f, 0.20f, 0.09f), new(-0.34f, -0.22f, -0.04f), neoViolet, new(0f, 0f, -14f));

                        var wingR = new Entity(parent: torso, position: new(0.18f, 0.22f, -0.16f));
                        Pack1Diamond(wingR, new(1.22f, 0.38f, 0.09f), new(0.62f, 0.38f, -0.06f), yellowLens, new(0f, 0f, -28f), unlit: true);
                        Pack1Diamond(wingR, new(0.92f, 0.26f, 0.09f), new(0.48f, 0.06f, -0.04f), neoPink, new(0f, 0f, -10f), unlit: true);
                        Pack1Diamond(wingR, new(0.68f, 0.22f, 0.09f), new(0.36f, -0.24f, -0.04f), neoViolet, new(0f, 0f, 16f));
                        parts.Tails.Add(wingL);
                        parts.Tails.Add(wingR);

                        // Зелёные кукольные нити, уходящие в небо (пустой пивот!)
                        var stringsRig = new Entity(parent: torso, position: new(0f, 0.42f, -0.06f));
                        foreach (float wx in new[] { -0.34f, -0.12f, 0.12f, 0.34f })
                            Pack1Box(stringsRig, new(0.025f, 1.45f, 0.025f), new(wx, 0.72f, -0.04f), wireGreen, new(0f, 0f, wx * 12f), unlit: true);
                        parts.Halo = stringsRig;

                        // Рука-пушка НЕО на правой руке (пустой пивот!)
                        var cannonPivot = new Entity(parent: parts.RightArm!, position: new(0f, -0.38f, 0.06f), rotation: new(65f, 0f, 0f));
                        Pack1Box(cannonPivot, new(0.24f, 0.56f, 0.24f), new(0f, 0.16f, 0f), neoPink);
                        Pack1Box(cannonPivot, new(0.20f, 0.22f, 0.20f), new(0f, 0.44f, 0f), neoDark);
                        Pack1Sphere(cannonPivot, new(0.17f, 0.14f, 0.17f), new(0f, 0.54f, 0f), yellowLens, unlit: true);

                        var ultCrown = new Entity(parent: torso, position: new(0f, 0.88f, 0f), enabled: false);
                        Pack1Diamond(ultCrown, new(1.20f, 0.18f, 1.20f), Vector3.Zero, yellowLens, unlit: true);
                        parts.UltFormNodes.Add(ultCrown);
                        return true;
                    }

                    // Базовый и Золотой 1997 кукольный Спамтон (скины 0 и 1): компактный пиджак, большая голова куклы-чревовещателя!
                    Color suitCol = skinIdx == 1 ? Rgb(198, 148, 36) : Rgb(20, 20, 26);
                    Pack1BuildStandardLimbs(root, parts, suitCol, Rgb(22, 22, 28), Rgb(16, 16, 20), puppetWhite, suitCol, 1.06f, new Vector3(0.42f, 0.46f, 0.26f), 0.48f, 0.72f);
                    var bTorso = parts.Torso!;
                    var bHead = parts.Head!;

                    // Белый ворот рубашки и нагрудный платок
                    Pack1Diamond(bTorso, new(0.18f, 0.28f, 0.08f), new(0f, 0.12f, 0.13f), puppetWhite);
                    Pack1Box(bTorso, new(0.08f, 0.05f, 0.04f), new(-0.12f, 0.10f, 0.14f), pinkLens, unlit: true);

                    // Чёрный зачёс волос, откидная челюсть куклы, румянец, длинный нос и розово-жёлтые очки DealMaker!
                    Pack1Sphere(bHead, new(0.54f, 0.38f, 0.54f), new(0f, 0.14f, -0.03f), hairBlack);
                    Pack1Box(bHead, new(0.46f, 0.16f, 0.42f), new(0f, 0.22f, -0.05f), hairBlack, new(-10f, 0f, 0f));
                    Pack1Box(bHead, new(0.26f, 0.14f, 0.24f), new(0f, -0.20f, 0.06f), puppetWhite);
                    Pack1Box(bHead, new(0.24f, 0.045f, 0.18f), new(0f, -0.12f, 0.13f), hairBlack);
                    Pack1Sphere(bHead, new(0.08f, 0.08f, 0.05f), new(-0.17f, -0.05f, 0.20f), Rgb(255, 105, 140));
                    Pack1Sphere(bHead, new(0.08f, 0.08f, 0.05f), new(0.17f, -0.05f, 0.20f), Rgb(255, 105, 140));
                    Pack1Box(bHead, new(0.075f, 0.075f, 0.34f), new(0f, -0.01f, 0.32f), puppetWhite);
                    Pack1Sphere(bHead, new(0.16f, 0.15f, 0.07f), new(-0.105f, 0.05f, 0.23f), pinkLens, unlit: true);
                    Pack1Sphere(bHead, new(0.16f, 0.15f, 0.07f), new(0.105f, 0.05f, 0.23f), yellowLens, unlit: true);
                    Pack1Box(bHead, new(0.36f, 0.035f, 0.05f), new(0f, 0.05f, 0.22f), hairBlack);

                    // Синий [Pipis] в правой руке и красная телефонная трубка в левой руке
                    Pack1Sphere(parts.RightArm!, new(0.22f, 0.18f, 0.22f), new(0f, -0.48f, 0.10f), Rgb(45, 165, 255), unlit: true);
                    var phonePivot = new Entity(parent: parts.LeftArm!, position: new(0f, -0.46f, 0.08f), rotation: new(35f, 0f, 0f));
                    Pack1Box(phonePivot, new(0.07f, 0.24f, 0.07f), Vector3.Zero, Rgb(225, 42, 58));
                    Pack1Box(phonePivot, new(0.11f, 0.07f, 0.10f), new(0f, 0.10f, 0.03f), Rgb(225, 42, 58));
                    Pack1Box(phonePivot, new(0.11f, 0.07f, 0.10f), new(0f, -0.10f, 0.03f), Rgb(225, 42, 58));

                    var ultPipis = new Entity(parent: bTorso, position: new(0f, 0.20f, 0f), enabled: false);
                    Pack1Diamond(ultPipis, new(0.95f, 0.16f, 0.95f), Vector3.Zero, pinkLens, unlit: true);
                    parts.UltFormNodes.Add(ultPipis);
                    return true;
                }

                case "binah":
                {
                    Color coatBlack = skinIdx == 1 ? Rgb(38, 30, 26) : Rgb(18, 18, 24);
                    Color goldTrim = skinIdx == 2 ? Rgb(250, 210, 92) : Rgb(218, 175, 68);
                    Color goldGlow = Rgb(255, 222, 98);
                    Color skinPale = Rgb(246, 236, 228);
                    Color hairDark = Rgb(26, 24, 30);

                    Pack1BuildStandardLimbs(root, parts, coatBlack, Rgb(22, 20, 26), Rgb(16, 15, 20), skinPale, coatBlack, 1.25f, new Vector3(0.44f, 0.54f, 0.28f), 0.44f, 0.88f);
                    var torso = parts.Torso!;
                    var head = parts.Head!;

                    // Пальто Арбитра Главы: высокий чёрно-золотой воротник, золотые кант-линии и раздвоенные полы плаща (на пустых пивотах!)
                    Pack1Box(torso, new(0.48f, 0.14f, 0.30f), new(0f, 0.28f, -0.02f), coatBlack);
                    Pack1Box(torso, new(0.50f, 0.045f, 0.32f), new(0f, 0.34f, -0.02f), goldTrim, unlit: true);
                    Pack1Box(torso, new(0.055f, 0.54f, 0.30f), new(-0.11f, 0.02f, 0.01f), goldTrim);
                    Pack1Box(torso, new(0.055f, 0.54f, 0.30f), new(0.11f, 0.02f, 0.01f), goldTrim);
                    Pack1Diamond(torso, new(0.12f, 0.16f, 0.08f), new(0f, 0.18f, 0.15f), goldGlow, unlit: true);

                    foreach (int s in new[] { -1, 1 })
                    {
                        var coatTail = new Entity(parent: torso, position: new(s * 0.13f, -0.22f, -0.08f), rotation: new(10f, 0f, s * -5f));
                        Pack1Box(coatTail, new(0.24f, 0.64f, 0.10f), new(0f, -0.30f, -0.02f), coatBlack);
                        Pack1Box(coatTail, new(0.25f, 0.06f, 0.12f), new(0f, -0.60f, -0.02f), goldTrim, unlit: true);
                        Pack1Box(coatTail, new(0.04f, 0.60f, 0.11f), new(s * -0.09f, -0.30f, -0.02f), goldTrim);
                        parts.Tails.Add(coatTail);
                    }

                    // Тёмные длинные волосы с золотой внутренней подкладкой, заколка и спокойные золотые глаза
                    Pack1Sphere(head, new(0.51f, 0.45f, 0.51f), new(0f, 0.10f, -0.03f), hairDark);
                    var backHair = new Entity(parent: head, position: new(0f, -0.04f, -0.18f), rotation: new(12f, 0f, 0f));
                    Pack1Box(backHair, new(0.44f, 0.68f, 0.16f), new(0f, -0.30f, -0.02f), hairDark);
                    Pack1Box(backHair, new(0.34f, 0.62f, 0.12f), new(0f, -0.32f, 0.03f), goldTrim);
                    parts.Tails.Add(backHair);
                    foreach (int s in new[] { -1, 1 })
                        Pack1Diamond(head, new(0.13f, 0.36f, 0.15f), new(s * 0.21f, -0.06f, 0.08f), hairDark);
                    Pack1Box(head, new(0.12f, 0.03f, 0.04f), new(0.18f, 0.14f, 0.18f), goldGlow, new(0f, 0f, -15f), unlit: true);
                    foreach (float ex in new[] { -0.10f, 0.10f })
                        Pack1Box(head, new(0.095f, 0.05f, 0.05f), new(ex, 0.02f, 0.21f), goldGlow, unlit: true);

                    // Нимб из 4 золотых ключей Феи (пустые пивоты!)
                    var fairyHalo = new Entity(parent: torso, position: new(0f, 0.42f, 0f));
                    for (int k = 0; k < 4; k++)
                    {
                        float ang = k * MathF.PI * 0.5f;
                        var keyPivot = new Entity(parent: fairyHalo, position: new(MathF.Cos(ang) * 0.62f, 0.05f, MathF.Sin(ang) * 0.62f), rotation: new(0f, k * 90f, 20f));
                        Pack1Box(keyPivot, new(0.04f, 0.24f, 0.04f), Vector3.Zero, goldGlow, unlit: true);
                        Pack1Diamond(keyPivot, new(0.12f, 0.12f, 0.05f), new(0f, 0.14f, 0f), goldTrim, unlit: true);
                    }
                    parts.Halo = fairyHalo;

                    // Чайная чашка в левой руке и парящий чёрно-золотой Столп Сингулярности у правой руки (пустые пивоты!)
                    var saucerPivot = new Entity(parent: parts.LeftArm!, position: new(0f, -0.46f, 0.12f));
                    Pack1Sphere(saucerPivot, new(0.20f, 0.04f, 0.20f), Vector3.Zero, coatBlack);
                    Pack1Box(saucerPivot, new(0.11f, 0.10f, 0.11f), new(0f, 0.06f, 0f), goldTrim);

                    var monolithPivot = new Entity(parent: parts.RightArm!, position: new(0.06f, -0.46f, 0.24f));
                    Pack1Box(monolithPivot, new(0.16f, 0.40f, 0.16f), Vector3.Zero, coatBlack);
                    Pack1Box(monolithPivot, new(0.19f, 0.05f, 0.19f), new(0f, 0.10f, 0f), goldGlow, unlit: true);
                    Pack1Box(monolithPivot, new(0.19f, 0.05f, 0.19f), new(0f, -0.10f, 0f), goldGlow, unlit: true);
                    parts.ShieldAura = monolithPivot;

                    // Аура ульты: Золотая печать Замка и парные Столпы Арбитра (пустой пивот!)
                    var ultGroup = new Entity(parent: torso, position: Vector3.Zero, enabled: false);
                    Pack1Diamond(ultGroup, new(1.15f, 0.12f, 1.15f), new(0f, -0.10f, 0f), goldGlow, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                    {
                        Pack1Box(ultGroup, new(0.20f, 0.96f, 0.20f), new(s * 0.72f, 0.24f, -0.22f), coatBlack);
                        Pack1Box(ultGroup, new(0.23f, 0.12f, 0.23f), new(s * 0.72f, 0.24f, -0.22f), goldGlow, unlit: true);
                    }
                    parts.UltFormNodes.Add(ultGroup);
                    return true;
                }

                case "don_quixote":
                {
                    Color blonde = Rgb(248, 214, 82);
                    Color skinPale = Rgb(252, 236, 224);

                    if (skinIdx == 1)
                    {
                        // Skin 1: Lobotomy E.G.O::Lantern Дон Кихот
                        Color mossGreen = Rgb(78, 122, 54);
                        Color barkDark = Rgb(28, 36, 24);
                        Color lureGlow = Rgb(202, 255, 92);
                        Color boneCol = Rgb(238, 232, 212);

                        Pack1BuildStandardLimbs(root, parts, barkDark, barkDark, Rgb(22, 28, 18), skinPale, mossGreen, 1.24f, new Vector3(0.48f, 0.54f, 0.28f), 0.45f, 0.86f);
                        var torso = parts.Torso!;
                        var head = parts.Head!;

                        // Пушистая мшистая шуба Мясоруба (Faelantern), костяные зубы по подолу и светящиеся огоньки
                        Pack1Sphere(torso, new(0.62f, 0.34f, 0.42f), new(0f, 0.22f, -0.02f), mossGreen);
                        Pack1Box(torso, new(0.54f, 0.44f, 0.33f), new(0f, -0.02f, 0f), mossGreen);
                        for (int i = -2; i <= 2; i++)
                            Pack1Diamond(torso, new(0.07f, 0.14f, 0.07f), new(i * 0.095f, -0.24f, 0.17f), boneCol);
                        Pack1Sphere(torso, new(0.09f, 0.09f, 0.06f), new(-0.14f, 0.12f, 0.18f), lureGlow, unlit: true);
                        Pack1Sphere(torso, new(0.09f, 0.09f, 0.06f), new(0.14f, 0.06f, 0.18f), lureGlow, unlit: true);

                        var furMantle = new Entity(parent: torso, position: new(0f, -0.18f, -0.12f), rotation: new(12f, 0f, 0f));
                        Pack1Box(furMantle, new(0.50f, 0.54f, 0.14f), new(0f, -0.22f, -0.02f), mossGreen);
                        parts.Tails.Add(furMantle);

                        // Светлые короткие волосы, гипнотические салатовые глаза и свисающий надо лбом фонарь-приманка удильщика!
                        Pack1Sphere(head, new(0.52f, 0.46f, 0.52f), new(0f, 0.10f, -0.03f), blonde);
                        foreach (int s in new[] { -1, 1 })
                        {
                            Pack1Diamond(head, new(0.14f, 0.30f, 0.14f), new(s * 0.22f, -0.02f, 0.08f), blonde, new(0f, 0f, s * 10f));
                            Pack1Sphere(head, new(0.105f, 0.10f, 0.06f), new(s * 0.105f, 0.02f, 0.21f), lureGlow, unlit: true);
                        }

                        var lurePivot = new Entity(parent: head, position: new(0f, 0.26f, 0.06f));
                        Pack1Box(lurePivot, new(0.045f, 0.34f, 0.045f), new(0f, 0.14f, 0.06f), barkDark, new(28f, 0f, 0f));
                        Pack1Box(lurePivot, new(0.045f, 0.045f, 0.26f), new(0f, 0.29f, 0.24f), barkDark);
                        Pack1Sphere(lurePivot, new(0.18f, 0.20f, 0.18f), new(0f, 0.20f, 0.36f), lureGlow, unlit: true);
                        parts.ShieldAura = lurePivot;

                        // Зазубренное костяное копьё Faelantern на пустом пивоте!
                        var spearPivot = new Entity(parent: parts.RightArm!, position: new(0f, -0.45f, 0.08f), rotation: new(68f, 0f, 0f));
                        Pack1Box(spearPivot, new(0.06f, 1.45f, 0.06f), new(0f, 0.42f, 0f), barkDark);
                        Pack1Diamond(spearPivot, new(0.18f, 0.52f, 0.16f), new(0f, 1.18f, 0f), boneCol);
                        for (int b = 0; b < 3; b++)
                            Pack1Diamond(spearPivot, new(0.22f, 0.10f, 0.10f), new(0f, 0.72f + b * 0.15f, 0.04f), boneCol);
                        Pack1Sphere(spearPivot, new(0.12f, 0.12f, 0.12f), new(0f, 1.42f, 0f), lureGlow, unlit: true);

                        var ultJaws = new Entity(parent: torso, position: new(0f, 0.05f, 0f), enabled: false);
                        Pack1Diamond(ultJaws, new(1.02f, 0.22f, 1.02f), Vector3.Zero, lureGlow, unlit: true);
                        parts.UltFormNodes.Add(ultJaws);
                        return true;
                    }

                    if (skinIdx == 2)
                    {
                        // Skin 2: Управляющая Ла-Манчаленда Дон Кихот (Manager of La Manchaland)
                        Color bloodRed = Rgb(168, 22, 40);
                        Color hardbloodGlow = Rgb(255, 48, 72);
                        Color velvetDark = Rgb(26, 15, 22);
                        Color goldTrim = Rgb(238, 195, 78);

                        Pack1BuildStandardLimbs(root, parts, bloodRed, velvetDark, velvetDark, skinPale, bloodRed, 1.25f, new Vector3(0.45f, 0.56f, 0.28f), 0.45f, 0.87f);
                        var torso = parts.Torso!;
                        var head = parts.Head!;

                        // Аристократическое багровое платье Кровавого Дьявола, белое жабо, золотые эполеты и пышные фалды
                        Pack1Box(torso, new(0.18f, 0.26f, 0.07f), new(0f, 0.15f, 0.15f), Color.White);
                        Pack1Diamond(torso, new(0.10f, 0.14f, 0.07f), new(0f, 0.17f, 0.18f), hardbloodGlow, unlit: true);
                        Pack1Box(torso, new(0.52f, 0.08f, 0.30f), new(0f, 0.26f, 0f), goldTrim);
                        foreach (int s in new[] { -1, 1 })
                        {
                            var gownPivot = new Entity(parent: torso, position: new(s * 0.14f, -0.22f, -0.08f), rotation: new(12f, 0f, s * -6f));
                            Pack1Diamond(gownPivot, new(0.32f, 0.68f, 0.22f), new(0f, -0.30f, -0.02f), bloodRed);
                            Pack1Box(gownPivot, new(0.28f, 0.06f, 0.16f), new(0f, -0.58f, -0.02f), goldTrim, unlit: true);
                            parts.Tails.Add(gownPivot);
                        }

                        // Длинные развевающиеся светлые волосы Санчо и пылающие алые глаза
                        Pack1Sphere(head, new(0.52f, 0.46f, 0.52f), new(0f, 0.10f, -0.03f), blonde);
                        var longHair = new Entity(parent: head, position: new(0f, -0.05f, -0.18f), rotation: new(14f, 0f, 0f));
                        Pack1Diamond(longHair, new(0.46f, 0.82f, 0.20f), new(0f, -0.36f, -0.04f), blonde);
                        parts.Tails.Add(longHair);
                        foreach (int s in new[] { -1, 1 })
                        {
                            Pack1Diamond(head, new(0.15f, 0.38f, 0.15f), new(s * 0.22f, -0.06f, 0.08f), blonde);
                            Pack1Sphere(head, new(0.105f, 0.095f, 0.06f), new(s * 0.105f, 0.02f, 0.21f), hardbloodGlow, unlit: true);
                        }

                        // Орбитальные осколки Кристаллизованной Крови (пустой пивот!)
                        var bloodHalo = new Entity(parent: torso, position: new(0f, 0.44f, 0f));
                        for (int k = 0; k < 4; k++)
                        {
                            float a = k * MathF.PI * 0.5f;
                            Pack1Diamond(bloodHalo, new(0.09f, 0.20f, 0.09f), new(MathF.Cos(a) * 0.56f, 0f, MathF.Sin(a) * 0.56f), hardbloodGlow, unlit: true);
                        }
                        parts.Halo = bloodHalo;

                        // Массивное турнирное копьё Кристаллизованной Крови на пустом пивоте!
                        var hbLancePivot = new Entity(parent: parts.RightArm!, position: new(0f, -0.45f, 0.08f), rotation: new(68f, 0f, 0f));
                        Pack1Box(hbLancePivot, new(0.07f, 1.55f, 0.07f), new(0f, 0.46f, 0f), velvetDark);
                        Pack1Diamond(hbLancePivot, new(0.36f, 0.26f, 0.36f), new(0f, 0.42f, 0f), bloodRed);
                        Pack1Diamond(hbLancePivot, new(0.26f, 0.98f, 0.26f), new(0f, 1.02f, 0f), hardbloodGlow, unlit: true);

                        // Крылья Кристаллизованной Крови в ульте (пустой пивот!)
                        var ultWings = new Entity(parent: torso, position: Vector3.Zero, enabled: false);
                        Pack1Diamond(ultWings, new(0.96f, 0.34f, 0.09f), new(-0.58f, 0.24f, -0.20f), hardbloodGlow, new(0f, 0f, 26f), unlit: true);
                        Pack1Diamond(ultWings, new(0.96f, 0.34f, 0.09f), new(0.58f, 0.24f, -0.20f), hardbloodGlow, new(0f, 0f, -26f), unlit: true);
                        parts.UltFormNodes.Add(ultWings);
                        return true;
                    }

                    // Skin 0: Базовая Грешница #3 LCB (значки Корректировщиков, красный галстук, кроссовки «Росинант», копьё Sueño Imposible)
                    Color lcbCoat = Rgb(28, 30, 38);
                    Color rocinanteYellow = Rgb(245, 204, 45);
                    Color tieRed = Rgb(220, 36, 46);
                    Color silverCol = Rgb(214, 222, 232);

                    Pack1BuildStandardLimbs(root, parts, lcbCoat, Rgb(26, 28, 34), rocinanteYellow, skinPale, lcbCoat, 1.24f, new Vector3(0.45f, 0.54f, 0.28f), 0.45f, 0.86f);
                    var bTorso = parts.Torso!;
                    var bHead = parts.Head!;

                    // Красные полоски на жёлтых кроссовках «Росинант», белая рубашка, красный галстук и цветные значки Корректировщиков!
                    Pack1Box(parts.LeftLeg!, new(0.21f, 0.045f, 0.22f), new(0f, -0.80f, 0.06f), tieRed);
                    Pack1Box(parts.RightLeg!, new(0.21f, 0.045f, 0.22f), new(0f, -0.80f, 0.06f), tieRed);
                    Pack1Box(bTorso, new(0.20f, 0.48f, 0.30f), new(0f, 0.04f, 0.01f), Rgb(244, 246, 250));
                    Pack1Box(bTorso, new(0.065f, 0.36f, 0.05f), new(0f, 0.05f, 0.16f), tieRed);
                    Color[] badgeCols = { Rgb(255, 210, 55), Rgb(65, 215, 255), Rgb(255, 85, 165), Rgb(95, 235, 115) };
                    for (int b = 0; b < 4; b++)
                        Pack1Sphere(bTorso, new(0.065f, 0.065f, 0.04f), new((b < 2 ? -0.15f : 0.15f), 0.18f - (b % 2) * 0.11f, 0.15f), badgeCols[b], unlit: true);

                    // Пышные короткие светлые волосы и сияющие жёлтые глаза
                    Pack1Sphere(bHead, new(0.52f, 0.46f, 0.52f), new(0f, 0.10f, -0.03f), blonde);
                    foreach (int s in new[] { -1, 1 })
                    {
                        Pack1Diamond(bHead, new(0.15f, 0.32f, 0.15f), new(s * 0.22f, -0.02f, 0.08f), blonde, new(0f, 0f, s * 12f));
                        Pack1Sphere(bHead, new(0.11f, 0.105f, 0.06f), new(s * 0.105f, 0.02f, 0.21f), Rgb(255, 228, 55), unlit: true);
                    }

                    // Серебряно-алое рыцарское турнирное копьё Sueño Imposible на пустом пивоте!
                    var lancePivot = new Entity(parent: parts.RightArm!, position: new(0f, -0.45f, 0.08f), rotation: new(68f, 0f, 0f));
                    Pack1Box(lancePivot, new(0.06f, 1.48f, 0.06f), new(0f, 0.44f, 0f), silverCol);
                    Pack1Diamond(lancePivot, new(0.34f, 0.24f, 0.34f), new(0f, 0.36f, 0f), silverCol);
                    Pack1Box(lancePivot, new(0.15f, 0.12f, 0.15f), new(0f, 0.50f, 0f), tieRed);
                    Pack1Diamond(lancePivot, new(0.20f, 0.88f, 0.20f), new(0f, 0.96f, 0f), silverCol);

                    var ultSancho = new Entity(parent: lancePivot, position: Vector3.Zero, enabled: false);
                    Pack1Diamond(ultSancho, new(0.30f, 1.08f, 0.30f), new(0f, 0.98f, 0f), tieRed, unlit: true);
                    parts.UltFormNodes.Add(ultSancho);
                    return true;
                }

                case "priestess":
                {
                    Color coatWhite = skinIdx == 2 ? Rgb(28, 24, 36) : Rgb(240, 242, 248);
                    Color innerDark = Rgb(30, 32, 42);
                    Color origBlack = Rgb(16, 14, 22);
                    Color origRed = skinIdx == 2 ? Rgb(255, 62, 98) : Rgb(225, 42, 76);
                    Color hairBrown = Rgb(92, 66, 56);
                    Color eyeViolet = Rgb(172, 108, 245);
                    Color skinPale = Rgb(250, 238, 232);

                    Pack1BuildStandardLimbs(root, parts, coatWhite, innerDark, origBlack, skinPale, coatWhite, 1.25f, new Vector3(0.43f, 0.54f, 0.27f), 0.44f, 0.88f);
                    var torso = parts.Torso!;
                    var head = parts.Head!;

                    // Белый плащ Исследовательницы Предтеч поверх тёмного платья с высоким воротом (фалды на пустых пивотах!)
                    Pack1Box(torso, new(0.19f, 0.52f, 0.29f), new(0f, 0.03f, 0.01f), innerDark);
                    Pack1Diamond(torso, new(0.10f, 0.15f, 0.06f), new(0f, 0.17f, 0.16f), origRed, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                    {
                        var coatBack = new Entity(parent: torso, position: new(s * 0.12f, -0.22f, -0.08f), rotation: new(9f, 0f, s * -4f));
                        Pack1Box(coatBack, new(0.22f, 0.60f, 0.09f), new(0f, -0.28f, -0.02f), coatWhite);
                        parts.Tails.Add(coatBack);
                    }

                    // Длинные каштановые волосы и фиолетовые глаза с белыми ромбовидными зрачками ◇!
                    Pack1Sphere(head, new(0.51f, 0.45f, 0.51f), new(0f, 0.10f, -0.03f), hairBrown);
                    var hairFlow = new Entity(parent: head, position: new(0f, -0.04f, -0.18f), rotation: new(12f, 0f, 0f));
                    Pack1Diamond(hairFlow, new(0.42f, 0.72f, 0.18f), new(0f, -0.32f, -0.04f), hairBrown);
                    parts.Tails.Add(hairFlow);
                    foreach (int s in new[] { -1, 1 })
                    {
                        Pack1Diamond(head, new(0.13f, 0.36f, 0.14f), new(s * 0.21f, -0.05f, 0.08f), hairBrown);
                        Pack1Sphere(head, new(0.10f, 0.09f, 0.05f), new(s * 0.10f, 0.02f, 0.205f), eyeViolet, unlit: true);
                        Pack1Diamond(head, new(0.045f, 0.065f, 0.05f), new(s * 0.10f, 0.02f, 0.225f), Color.White, unlit: true);
                    }

                    // Орбитальный венец из 6 чёрно-багровых ромбов Ориджиниума ◇ (все узлы на пустых пивотах!)
                    var origHalo = new Entity(parent: torso, position: new(0f, 0.40f, 0f));
                    for (int k = 0; k < 6; k++)
                    {
                        float ang = k * MathF.PI / 3f;
                        var shardPivot = new Entity(parent: origHalo, position: new(MathF.Cos(ang) * 0.66f, (k % 2 == 0) ? 0.06f : -0.06f, MathF.Sin(ang) * 0.66f));
                        Pack1Diamond(shardPivot, new(0.12f, 0.26f, 0.12f), Vector3.Zero, origBlack);
                        Pack1Diamond(shardPivot, new(0.15f, 0.09f, 0.15f), Vector3.Zero, origRed, unlit: true);
                    }
                    parts.Halo = origHalo;

                    // Голографический ромб PRTS у левой руки и кристалл Ориджиниума в правой руке (пустой пивот!)
                    var prtsPivot = new Entity(parent: parts.LeftArm!, position: new(0f, -0.46f, 0.18f));
                    Pack1Diamond(prtsPivot, new(0.20f, 0.26f, 0.06f), Vector3.Zero, Rgb(165, 225, 255), unlit: true);
                    parts.ShieldAura = prtsPivot;
                    Pack1Diamond(parts.RightArm!, new(0.15f, 0.26f, 0.15f), new(0f, -0.46f, 0.16f), origRed, unlit: true);

                    // Контур ассимиляции Ориджиниума в ульте (пустой пивот!)
                    var ultGroup = new Entity(parent: torso, position: new(0f, 0.10f, 0f), enabled: false);
                    Pack1Diamond(ultGroup, new(1.28f, 0.16f, 1.28f), Vector3.Zero, origRed, new(16f, 0f, 16f), unlit: true);
                    Pack1Diamond(ultGroup, new(1.28f, 0.16f, 1.28f), Vector3.Zero, origBlack, new(-16f, 45f, -16f));
                    parts.UltFormNodes.Add(ultGroup);
                    return true;
                }

                case "closure":
                {
                    Color jacketNavy = skinIdx == 1 ? Rgb(28, 38, 66) : (skinIdx == 2 ? Rgb(48, 20, 36) : Rgb(36, 48, 82));
                    Color strapOrange = skinIdx == 1 ? Rgb(65, 225, 245) : (skinIdx == 2 ? Rgb(255, 162, 45) : Rgb(245, 132, 42));
                    Color hairPurple = Rgb(118, 66, 154);
                    Color vampEye = Rgb(245, 48, 78);
                    Color skinPale = Rgb(250, 238, 234);

                    Pack1BuildStandardLimbs(root, parts, jacketNavy, Rgb(28, 30, 40), Rgb(24, 26, 36), skinPale, jacketNavy, 1.23f, new Vector3(0.44f, 0.52f, 0.28f), 0.44f, 0.86f);
                    var torso = parts.Torso!;
                    var head = parts.Head!;

                    // Тёмно-синяя инженерная куртка, ярко-оранжевый ремень и бейдж «Родос Айленд»
                    Pack1Box(torso, new(0.20f, 0.48f, 0.30f), new(0f, 0.02f, 0.01f), Rgb(24, 26, 34));
                    Pack1Box(torso, new(0.065f, 0.52f, 0.31f), new(-0.08f, 0.02f, 0.01f), strapOrange, new(0f, 0f, -14f), unlit: true);
                    Pack1Box(torso, new(0.085f, 0.11f, 0.04f), new(0.13f, 0.12f, 0.16f), Color.White);
                    Pack1Box(torso, new(0.48f, 0.14f, 0.31f), new(0f, -0.24f, 0f), Rgb(30, 32, 44));

                    // Фиолетовые волосы, сдвинутые на лоб инженерные очки, алые глаза вампира и клычок
                    Pack1Sphere(head, new(0.51f, 0.45f, 0.51f), new(0f, 0.10f, -0.03f), hairPurple);
                    foreach (int s in new[] { -1, 1 })
                        Pack1Diamond(head, new(0.14f, 0.34f, 0.14f), new(s * 0.21f, -0.04f, 0.08f), hairPurple, new(0f, 0f, s * 10f));
                    Pack1Box(head, new(0.46f, 0.08f, 0.42f), new(0f, 0.17f, 0.03f), Rgb(42, 46, 56));
                    Pack1Sphere(head, new(0.14f, 0.11f, 0.08f), new(-0.10f, 0.18f, 0.22f), strapOrange, unlit: true);
                    Pack1Sphere(head, new(0.14f, 0.11f, 0.08f), new(0.10f, 0.18f, 0.22f), strapOrange, unlit: true);
                    Pack1Sphere(head, new(0.10f, 0.09f, 0.05f), new(-0.10f, 0.02f, 0.21f), vampEye, unlit: true);
                    Pack1Sphere(head, new(0.10f, 0.09f, 0.05f), new(0.10f, 0.02f, 0.21f), vampEye, unlit: true);
                    Pack1Diamond(head, new(0.035f, 0.06f, 0.035f), new(0.06f, -0.09f, 0.21f), Color.White);

                    // Механический дата-кабель-хвост (на пустых пивотах!)
                    var cable1 = new Entity(parent: torso, position: new(0f, -0.20f, -0.14f));
                    Pack1Box(cable1, new(0.055f, 0.055f, 0.30f), new(0f, 0f, -0.15f), Rgb(28, 30, 38));
                    var cable2 = new Entity(parent: cable1, position: new(0f, -0.02f, -0.28f));
                    Pack1Box(cable2, new(0.05f, 0.05f, 0.30f), new(0f, 0f, -0.15f), strapOrange);
                    Pack1Diamond(cable2, new(0.12f, 0.12f, 0.16f), new(0f, 0f, -0.32f), vampEye, unlit: true);
                    parts.Tails.Add(cable1);
                    parts.Tails.Add(cable2);

                    // Импульсный гаечный ключ / пульт управления дронами на пустом пивоте!
                    var wrenchPivot = new Entity(parent: parts.RightArm!, position: new(0f, -0.44f, 0.08f), rotation: new(60f, 0f, 0f));
                    Pack1Box(wrenchPivot, new(0.08f, 0.48f, 0.08f), new(0f, 0.14f, 0f), strapOrange);
                    Pack1Box(wrenchPivot, new(0.20f, 0.14f, 0.07f), new(0f, 0.40f, 0f), Rgb(185, 195, 210));
                    Pack1Box(wrenchPivot, new(0.11f, 0.12f, 0.04f), new(0f, 0.18f, 0.05f), Rgb(85, 235, 255), unlit: true);

                    // Парящий мини-дрон Lancet-2 / Лазерный Дрон над плечом (все узлы на пустых пивотах!)
                    var dronePivot = new Entity(parent: torso, position: new(-0.54f, 0.52f, 0.08f));
                    Pack1Box(dronePivot, new(0.26f, 0.16f, 0.30f), Vector3.Zero, Rgb(238, 242, 248));
                    Pack1Box(dronePivot, new(0.28f, 0.05f, 0.18f), new(0f, 0.04f, 0f), strapOrange);
                    Pack1Sphere(dronePivot, new(0.10f, 0.10f, 0.06f), new(0f, 0f, 0.16f), vampEye, unlit: true);
                    var rotorPivot = new Entity(parent: dronePivot, position: new(0f, 0.12f, 0f));
                    Pack1Box(rotorPivot, new(0.42f, 0.025f, 0.06f), Vector3.Zero, Rgb(85, 235, 255), unlit: true);
                    Pack1Box(rotorPivot, new(0.06f, 0.025f, 0.42f), Vector3.Zero, Rgb(85, 235, 255), unlit: true);
                    parts.Halo = dronePivot;
                    parts.ShieldAura = rotorPivot;

                    // Лазерные крылья Овердрайва Инженерного Отсека в ульте (пустые пивоты!)
                    var ultDroneWings = new Entity(parent: dronePivot, position: Vector3.Zero, enabled: false);
                    Pack1Diamond(ultDroneWings, new(0.48f, 0.10f, 0.10f), new(-0.28f, 0f, 0f), vampEye, unlit: true);
                    Pack1Diamond(ultDroneWings, new(0.48f, 0.10f, 0.10f), new(0.28f, 0f, 0f), vampEye, unlit: true);
                    var ultGrid = new Entity(parent: torso, position: new(0f, 0.05f, 0f), enabled: false);
                    Pack1Diamond(ultGrid, new(1.08f, 0.14f, 1.08f), Vector3.Zero, strapOrange, unlit: true);
                    parts.UltFormNodes.Add(ultDroneWings);
                    parts.UltFormNodes.Add(ultGrid);
                    return true;
                }

                default:
                    return false;
            }
        }

        private static void ApplyExpansionPack1Mastery(string charKey, Entity root, SurvivorParts parts, SkinDef skin)
        {
            if (parts.Torso == null || parts.Head == null)
                return;

            switch (charKey)
            {
                case "susie":
                {
                    Pack1Diamond(parts.Head, new(0.14f, 0.24f, 0.12f), new(0f, 0.28f, 0.10f), skin.VisorColor, unlit: true);
                    var haloPivot = new Entity(parent: parts.Torso, position: new(0f, 0.68f, 0f));
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * (MathF.PI / 3f);
                        Pack1Diamond(haloPivot, new(0.14f, 0.20f, 0.14f), new(MathF.Cos(a) * 0.44f, 0f, MathF.Sin(a) * 0.44f), skin.VisorColor, unlit: true);
                    }
                    parts.Halo = haloPivot;
                    break;
                }

                case "binah":
                    for (int i = -1; i <= 1; i++)
                        Pack1Diamond(parts.Head, new(0.08f, 0.22f, 0.08f), new(i * 0.11f, 0.30f + (i == 0 ? 0.05f : 0f), 0.06f), skin.ArmorColor, unlit: true);
                    break;

                case "priestess":
                    Pack1Diamond(parts.Head, new(0.34f, 0.10f, 0.34f), new(0f, 0.32f, 0f), skin.ArmorColor, unlit: true);
                    break;

                case "closure":
                    Pack1Diamond(parts.Head, new(0.20f, 0.16f, 0.10f), new(0f, 0.32f, 0.08f), skin.ArmorColor, unlit: true);
                    break;
            }
        }
    }
}
