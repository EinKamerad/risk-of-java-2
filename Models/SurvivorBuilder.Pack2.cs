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
        private void UpdateExpansionPack2Anims(string charKey, float t, float dt, bool isMoving, bool isGrounded, float strafeInput, float forwardInput)
        {
            float p = AnimDuration > 0.001f ? Math.Clamp(1f - AnimTimer / AnimDuration, 0f, 1f) : 1f;
            float swing = MathF.Sin(p * MathF.PI);

            switch (charKey)
            {
                case "daitaku_helios":
                {
                    float beat = MathF.Sin(t * (UltFormActive ? 14f : 8.5f));
                    if (Torso != null && string.IsNullOrEmpty(ActiveEmote))
                        Torso.Position = BaseTorsoPos + new Vector3(0, MathF.Abs(beat) * (UltFormActive ? 0.06f : 0.025f), 0);
                    if (Head != null && string.IsNullOrEmpty(ActiveEmote))
                        Head.Rotation = BaseHeadRot + new Vector3(beat * 4f, 0, MathF.Cos(t * 4.25f) * 5f);
                    if (AnimTimer > 0f && RightLeg != null && ActiveAnim == "m1")
                        RightLeg.Rotation = BaseRightLegRot + new Vector3(-75f * swing, 0, 12f * swing);
                    if (AnimTimer > 0f && LeftArm != null && RightArm != null && ActiveAnim == "shift")
                    {
                        if (Torso != null) Torso.Rotation = BaseTorsoRot + new Vector3(26f * swing, 0, 0);
                        LeftArm.Rotation = BaseLeftArmRot + new Vector3(48f * swing, 0, -14f);
                        RightArm.Rotation = BaseRightArmRot + new Vector3(48f * swing, 0, 14f);
                    }
                    break;
                }
                case "robin":
                {
                    float sway = MathF.Sin(t * 2.8f);
                    if (RightArm != null && AnimTimer <= 0f && string.IsNullOrEmpty(ActiveEmote))
                        RightArm.Rotation = BaseRightArmRot + new Vector3(-34f + sway * 4f, -14f, -8f);
                    if (LeftArm != null && UltFormActive && AnimTimer <= 0f && string.IsNullOrEmpty(ActiveEmote))
                        LeftArm.Rotation = BaseLeftArmRot + new Vector3(-28f - sway * 6f, 12f, -22f);
                    if (Halo != null)
                        Halo.Rotation = new Vector3(18f, t * 48f, sway * 6f);
                    if (AnimTimer > 0f && LeftArm != null && ActiveAnim == "m1")
                        LeftArm.Rotation = BaseLeftArmRot + new Vector3(-68f * swing, 24f * swing, -30f * swing);
                    break;
                }
                case "bird_trio":
                {
                    float flap = MathF.Sin(t * (isMoving ? 11f : 4.2f)) * (isMoving ? 24f : 9f);
                    if (LeftArm != null && AnimTimer <= 0f)
                        LeftArm.Rotation = BaseLeftArmRot + new Vector3(0, 0, -flap);
                    if (RightArm != null && AnimTimer <= 0f)
                        RightArm.Rotation = BaseRightArmRot + new Vector3(0, 0, flap * 0.55f);
                    if (AnimTimer > 0f && Torso != null && (ActiveAnim == "m1" || ActiveAnim == "r"))
                        Torso.Rotation = BaseTorsoRot + new Vector3(28f * swing, 0, 0);
                    break;
                }
                case "apocalypse_bird":
                {
                    float wingWave = MathF.Sin(t * (UltFormActive ? 7.5f : 3.4f)) * (UltFormActive ? 22f : 12f);
                    if (LeftArm != null && AnimTimer <= 0f)
                        LeftArm.Rotation = BaseLeftArmRot + new Vector3(6f, -wingWave * 0.5f, -wingWave);
                    if (RightArm != null && AnimTimer <= 0f)
                        RightArm.Rotation = BaseRightArmRot + new Vector3(6f, wingWave * 0.5f, wingWave);
                    if (Halo != null)
                        Halo.Rotation = new Vector3(14f + MathF.Sin(t * 2.1f) * 8f, t * 32f, 16f * MathF.Cos(t * 1.7f));
                    for (int i = 0; i < Tails.Count; i++)
                        if (Tails[i] != null) Tails[i].Rotation = new Vector3(0, t * 65f + i * 120f, MathF.Sin(t * 3f + i) * 10f);
                    break;
                }
                case "komeiji":
                {
                    float pulse = MathF.Sin(t * 3.6f);
                    if (Halo != null)
                        Halo.Position = new Vector3(0, 0.08f + pulse * 0.03f, 0.30f);
                    if (AnimTimer > 0f && RightArm != null && LeftArm != null && (ActiveAnim == "m1" || ActiveAnim == "m2"))
                    {
                        RightArm.Rotation = BaseRightArmRot + new Vector3(-62f * swing, -12f, 10f);
                        LeftArm.Rotation = BaseLeftArmRot + new Vector3(-48f * swing, 12f, -10f);
                    }
                    break;
                }
                case "false_son":
                {
                    if (Halo != null)
                        Halo.Rotation = new Vector3(22f, t * 26f, MathF.Sin(t * 1.8f) * 6f);
                    if (AnimTimer > 0f && RightArm != null && ActiveAnim == "m1")
                        RightArm.Rotation = BaseRightArmRot + new Vector3(-105f * MathF.Sin(p * MathF.PI), -18f * swing, 14f * swing);
                    if (AnimTimer > 0f && LeftArm != null && ActiveAnim == "m2")
                        LeftArm.Rotation = BaseLeftArmRot + new Vector3(-76f * swing, 16f * swing, -12f);
                    if (UltFormActive && Torso != null && string.IsNullOrEmpty(ActiveEmote))
                        Torso.Rotation = BaseTorsoRot + new Vector3(-8f, 0, 0);
                    break;
                }
            }
        }
    }

    public static partial class SurvivorDatabase
    {
        private static Entity P2Box(Entity p, Vector3 s, Vector3 pos, Color col, Vector3 rot = default, bool unlit = false, bool enabled = true)
            => new Entity(parent: p, model: "cube", scale: s, position: pos, rotation: rot, color: col, unlit: unlit, enabled: enabled);

        private static Entity P2Sph(Entity p, Vector3 s, Vector3 pos, Color col, Vector3 rot = default, bool unlit = false, bool enabled = true)
            => new Entity(parent: p, model: "sphere", scale: s, position: pos, rotation: rot, color: col, unlit: unlit, enabled: enabled);

        private static Entity P2Dia(Entity p, Vector3 s, Vector3 pos, Color col, Vector3 rot = default, bool unlit = false, bool enabled = true)
            => new Entity(parent: p, model: "diamond", scale: s, position: pos, rotation: rot, color: col, unlit: unlit, enabled: enabled);

        private static void RegisterExpansionPack2Classes()
        {
            Classes["daitaku_helios"] = new SurvivorConfig
            {
                Key = "daitaku_helios", Name = "ДАЙТАКУ ГЕЛИОС", Icon = "HELI",
                Subtitle = "Тусовочный Фронт-Раннер ★ WEI~★!",
                Hp = 135, Speed = 11.4f, Damage = 18.0f, FireRate = 4.6f, ProjSpeed = 66f, ProjScale = 0.26f, Aoe = 2.6f,
                BodyColor = Rgb(34, 48, 112), ArmorColor = Rgb(0, 228, 255), VisorColor = Rgb(255, 62, 185),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Неоновый Бит-Кик", Cd = 0f, Desc = "Ритмичные хай-кики и звуковые кольца бита в такт вечеринке." },
                    ["m2"] = new() { Name = "WEI~★! Взрыв Сабвуфера", Cd = 4.0f, Desc = "Мощный басовый дроп вокруг себя, подбрасывающий и оглушающий врагов." },
                    ["shift"] = new() { Name = "Отрыв Фронт-Раннера!", Cd = 4.2f, Desc = "Турбо-рывок вперёд с неоновым шлейфом и ускорением бега." },
                    ["r"] = new() { Name = "Парипи-Фест до Утра!", Cd = 10.0f, Desc = "Включает вертушку DJ: взрывной темп атак, скорость и басовые волны!" }
                }
            };

            Classes["robin"] = new SurvivorConfig
            {
                Key = "robin", Name = "ЗАРЯНКА (РОБИН)", Icon = "ROBN",
                Subtitle = "Певица Вселенской Гармонии ♫",
                Hp = 125, Speed = 10.3f, Damage = 18.5f, FireRate = 4.2f, ProjSpeed = 68f, ProjScale = 0.25f, Aoe = 3.0f,
                BodyColor = Rgb(244, 242, 252), ArmorColor = Rgb(146, 128, 224), VisorColor = Rgb(255, 216, 108),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Взмах Белого Пера ♫", Cd = 0f, Desc = "Посылает сияющие ноты-перья Гармонии, резонирующие между целями." },
                    ["m2"] = new() { Name = "Ария Оперения", Cd = 4.4f, Desc = "Веер золотых аккордов: наносит урон по площади и дарует щит вдохновения." },
                    ["shift"] = new() { Name = "Полёт Иволги", Cd = 4.6f, Desc = "Изящный взлёт на крыльях света с исцеляющим музыкальным шлейфом." },
                    ["r"] = new() { Name = "Качайся в Ритме Космоса! ♫", Cd = 10.8f, Desc = "Концерт Гармонии: раскрывает золотые крылья, усиливает урон и бьёт аккордами!" }
                }
            };

            Classes["bird_trio"] = new SurvivorConfig
            {
                Key = "bird_trio", Name = "ТРИО ПТИЦ", Icon = "TRIO",
                Subtitle = "Стражи Чёрного Леса [Секрет]",
                Hp = 155, Speed = 10.2f, Damage = 21.0f, FireRate = 3.7f, ProjSpeed = 60f, ProjScale = 0.30f, Aoe = 3.2f,
                BodyColor = Rgb(20, 20, 26), ArmorColor = Rgb(88, 66, 28), VisorColor = Rgb(255, 220, 48),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Укус Дозорного", Cd = 0f, Desc = "Большая Птица вырывает тьму тяжёлым клювом и ослепляет взглядом." },
                    ["m2"] = new() { Name = "Свет Вечной Лампы", Cd = 4.4f, Desc = "Вспышка золотого фонаря завораживает врагов и отмечает их для кары." },
                    ["shift"] = new() { Name = "Обход Чёрного Леса", Cd = 4.6f, Desc = "Тяжёлый наскок вперёд со вспышкой десятков жёлтых глаз." },
                    ["r"] = new() { Name = "Спасительное Откусывание", Cd = 10.5f, Desc = "Распахивает зубастый клюв, казня околдованных врагов во имя их спасения!" }
                }
            };

            Classes["apocalypse_bird"] = new SurvivorConfig
            {
                Key = "apocalypse_bird", Name = "ПТИЦА АПОКАЛИПСИСА", Icon = "APOC",
                Subtitle = "Чудовище Чёрного Леса [Секрет]",
                Hp = 205, Speed = 10.0f, Damage = 24.5f, FireRate = 3.3f, ProjSpeed = 68f, ProjScale = 0.36f, Aoe = 4.6f,
                BodyColor = Rgb(18, 16, 22), ArmorColor = Rgb(175, 128, 34), VisorColor = Rgb(255, 208, 42),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Коготь Сумерек", Cd = 0f, Desc = "Исполинские золотые когти рассекают пространство волнами чёрно-золотого пламени." },
                    ["m2"] = new() { Name = "Огни Тысячи Глаз", Cd = 4.6f, Desc = "Глаза на крыльях выстреливают самонаводящимся залпом золотых лучей." },
                    ["shift"] = new() { Name = "Поступь Зверя Врат", Cd = 5.0f, Desc = "Сокрушительный прорыв сквозь врагов под защитой Трёх Яиц." },
                    ["r"] = new() { Name = "Приговор Тёмного Леса", Cd = 12.0f, Desc = "Склоняет Несправедливые Весы и обрушивает кару всех трёх птиц разом!" }
                }
            };

            Classes["komeiji"] = new SurvivorConfig
            {
                Key = "komeiji", Name = "САТОРИ И КОИШИ", Icon = "KOME",
                Subtitle = "Хозяйки Дворца Земных Духов",
                Hp = 125, Speed = 10.5f, Damage = 18.5f, FireRate = 4.3f, ProjSpeed = 64f, ProjScale = 0.24f, Aoe = 2.8f,
                BodyColor = Rgb(245, 196, 218), ArmorColor = Rgb(74, 118, 196), VisorColor = Rgb(235, 38, 78),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Иглы Чтения Разума", Cd = 0f, Desc = "Третий Глаз выпускает алые спиральные пули данмаку прямо в уязвимые точки." },
                    ["m2"] = new() { Name = "Воспоминание «Ужасный Гипноз»", Cd = 4.2f, Desc = "Волна гипноза раскрывает страхи врагов, замедляя и раня их разум." },
                    ["shift"] = new() { Name = "Проекция Травмы", Cd = 4.4f, Desc = "Мгновенное смещение сквозь мысли с остаточной ментальной ловушкой." },
                    ["r"] = new() { Name = "Знак Мозга «Отпечаток Души»", Cd = 10.5f, Desc = "Шторм скопированных спелл-карт вокруг открытого Третьего Глаза!" }
                }
            };

            Classes["false_son"] = new SurvivorConfig
            {
                Key = "false_son", Name = "ЛОЖНЫЙ СЫН", Icon = "FSON",
                Subtitle = "Освобождённый Наследник Бури",
                Hp = 190, Speed = 9.8f, Damage = 26.0f, FireRate = 3.0f, ProjSpeed = 70f, ProjScale = 0.32f, Aoe = 3.8f,
                BodyColor = Rgb(78, 96, 128), ArmorColor = Rgb(48, 60, 86), VisorColor = Rgb(255, 206, 64),
                Skills = new()
                {
                    ["m1"] = new() { Name = "Палица Отвергнутого", Cd = 0f, Desc = "Сокрушительные удары каменной палицей с золотыми ударными волнами." },
                    ["m2"] = new() { Name = "Лунные Шипы", Cd = 4.0f, Desc = "Выпускает орбитальные голубые осколки, накладывающие Порчу Луны." },
                    ["shift"] = new() { Name = "Шаг Братьев", Cd = 4.6f, Desc = "Молниеносный рывок сквозь бурю с сокрушающим толчком плечом." },
                    ["r"] = new() { Name = "Лазер Отца", Cd = 11.0f, Desc = "Выжигает поле боя ослепительным золотым лучом Аурелионита!" }
                }
            };
        }

        private static void SetupExpansionPack2Loadouts()
        {
            SetupCharLoadout("daitaku_helios",
                "Неугомонная гяру-бегунья с вечной улыбкой «WEI~★!». Врывается в гущу битвы первой, превращая забег в неоновую вечеринку с диджейским пультом и басами!",
                "Ритм Фронт-Раннера ★", "Набирает заряд Хайпа на бегу, повышая скорость перемещения и урон звуковых волн.",
                new SkillDef { Name = "Виниловые Диски DJ", Cd = 0f, Desc = "Метает рикошетящие неоновые пластинки с бирюзово-пурпурным следом." },
                new SkillDef { Name = "Фотовспышка Гяру", Cd = 4.8f, Desc = "Яркое селфи со вспышкой «WEI~★!», ослепляющее всех врагов перед собой." },
                new SkillDef { Name = "Дрифт на Вираже", Cd = 5.0f, Desc = "Дуговой спринт вокруг толпы врагов с разрядом статического баса." },
                new SkillDef { Name = "Солнечный Спурт Гелиоса", Cd = 14.0f, Desc = "Финишный рывок на пределе скорости с непрерывными взрывами звёзд!" },
                new SkinDef { Name = "Неоновый Гран-При Сибуи", Desc = "Уличная куртка цвета лайма и ультрафиолета для ночных рейвов.", UnlockAchKey = "ach_slayer", UnlockReqText = "Истребите толпу врагов", BodyColor = Rgb(28, 22, 58), ArmorColor = Rgb(155, 255, 52), VisorColor = Rgb(255, 92, 225) },
                new SkinDef { Name = "Звёздный Диджей Аримы ★", Desc = "Золотая корона чемпиона, сияющие виниловые крылья и платиновый неон.", UnlockAchKey = "ach_boss", UnlockReqText = "Победите босса", ChangesModel = true, BodyColor = Rgb(24, 30, 68), ArmorColor = Rgb(255, 216, 76), VisorColor = Rgb(0, 245, 255) });

            SetupCharLoadout("robin",
                "Галактическая дива с Пенаконии, чья песня настроена на струны Эона Гармонии. Её голос обращает свет звёзд в щиты и разящие аккорды.",
                "Резонанс Гармонии ♫", "Каждая 4-я атака вызывает дополнительный золотой аккорд с небес по ближайшим врагам.",
                new SkillDef { Name = "Аккорд Стаккато", Cd = 0f, Desc = "Быстрая тройная очередь пронзающих золотых нот высокой частоты." },
                new SkillDef { Name = "Серенада Хора", Cd = 6.0f, Desc = "Создаёт поющее кольцо света, лечащее героя и обжигающее врагов." },
                new SkillDef { Name = "Модуляция Ритма", Cd = 5.5f, Desc = "Телепортация назад в россыпи белых перьев с оглушающей нотой." },
                new SkillDef { Name = "Ода Вселенской Гармонии", Cd = 15.0f, Desc = "Возносит сцену света: мощнейший финальный крещендо-взрыв и золотая неуязвимость!" },
                new SkinDef { Name = "Ноктюрн Пенаконии", Desc = "Вечернее бархатное платье цвета полуночного сапфира и серебра.", UnlockAchKey = "ach_slayer", UnlockReqText = "Истребите толпу врагов", BodyColor = Rgb(36, 42, 92), ArmorColor = Rgb(188, 206, 255), VisorColor = Rgb(255, 226, 130) },
                new SkinDef { Name = "Крылья Эона Гармонии ♫", Desc = "Божественное бело-золотое облачение с короной-нимбом и звёздным шлейфом.", UnlockAchKey = "ach_boss", UnlockReqText = "Победите босса", ChangesModel = true, BodyColor = Rgb(255, 250, 238), ArmorColor = Rgb(255, 206, 86), VisorColor = Rgb(168, 236, 255) });

            SetupCharLoadout("bird_trio",
                "Три крылатых стража, решивших защитить Чёрный Лес от беды: всевидящая Большая Птица, судящая Длинная Птица и карающая Маленькая Птица.",
                "Вечная Лампа Дозора", "Атаки Большой Птицы накладывают метку Заворожённости, усиливая урон её челюстей.",
                new SkillDef { Name = "Пристальный Взгляд", Cd = 0f, Desc = "Десятки жёлтых глаз выжигают цель сфокусированным лучом тревоги." },
                new SkillDef { Name = "Зов Тёмной Чащи", Cd = 6.0f, Desc = "Погашает свет вокруг, стягивая врагов к золотому пламени лампы." },
                new SkillDef { Name = "Тяжёлое Крыло", Cd = 5.5f, Desc = "Размашистый скачок вперёд с отбрасывающим взмахом чёрных крыльев." },
                new SkillDef { Name = "Клетка Вечного Света", Cd = 14.0f, Desc = "Окружает поле боя золотыми глазами и смыкает исполинский клюв!" },
                new SkinDef
                {
                    Name = "Длинная Птица [Слот 2]", Desc = "Высокая забинтованная птица на длинных золотистых лапах с Несправедливыми Весами.",
                    UnlockedByDefault = false, ChangesModel = true,
                    UnlockAchKey = "ach_persona_bird_long",
                    UnlockReqText = "Играя за Трио Птиц: Беспристрастный Суд — победите Босса Телепорта, не опустившись ниже 70% HP",
                    BodyColor = Rgb(24, 24, 32), ArmorColor = Rgb(238, 234, 224), VisorColor = Rgb(242, 198, 68),
                    CustomSubtitle = "Судья Чёрного Леса [Длинная Птица]",
                    CustomPassiveName = "Несправедливые Весы",
                    CustomPassiveDesc = "Весы никогда не лгут: удары накапливают печати Греха и разряжаются золотой карой.",
                    CustomSkillVariants = BuildCustomSkinSkillSet("Длинная Птица",
                        new SkillDef { Name = "Взмах Чаши Весов", Cd = 0f, Desc = "Дальний удар золотыми цепями весов, накладывающий печать Греха." },
                        new SkillDef { Name = "Клюв Из-Под Бинтов", Cd = 0f, Desc = "Хлёсткие выпады длинной шеи и тёмных перьев по прямой линии." },
                        new SkillDef { Name = "Взвешивание Грехов", Cd = 5.5f, Desc = "Наклоняет золотые весы, обрушивая столб света на всех грешников вокруг." },
                        new SkillDef { Name = "Бремя Вины", Cd = 6.0f, Desc = "Сковывает врагов золотыми цепями правосудия и снижает их защиту." },
                        new SkillDef { Name = "Поступь Ходулей", Cd = 5.5f, Desc = "Широкий стремительный шаг над головами врагов с ударной волной." },
                        new SkillDef { Name = "Шествие Правосудия", Cd = 6.0f, Desc = "Скользящий рывок сквозь строй врагов, оставляющий печати Греха." },
                        new SkillDef { Name = "Вердикт: Виновны Все!", Cd = 14.0f, Desc = "Весы замирают на краю: мгновенный приговор бледного золота по всем целям!" },
                        new SkillDef { Name = "Колокол Судного Дня", Cd = 14.5f, Desc = "Три удара весов сотрясают арену нарастающими кольцами правосудия!" })
                },
                new SkinDef
                {
                    Name = "Маленькая Птица [Слот 3]", Desc = "Крохотная белоснежная пташка с алым пятнышком и гигантской зубастой пастью кары.",
                    UnlockedByDefault = false, ChangesModel = true,
                    UnlockAchKey = "ach_persona_bird_small",
                    UnlockReqText = "Играя за Трио Птиц: Карающий Клюв — уничтожьте 5 врагов в течение 6 секунд после получения урона",
                    BodyColor = Rgb(248, 248, 252), ArmorColor = Rgb(215, 28, 48), VisorColor = Rgb(255, 72, 92),
                    CustomSubtitle = "Каратель Чёрного Леса [Маленькая Птица]",
                    CustomPassiveName = "Карающий Клюв",
                    CustomPassiveDesc = "Получая урон или входя в ярость, пташка распахивает чудовищную алую пасть на груди!",
                    CustomSkillVariants = BuildCustomSkinSkillSet("Маленькая Птица",
                        new SkillDef { Name = "Стремительный Клёв", Cd = 0f, Desc = "Очень частые уколы острым клювиком в ближнем и среднем бою." },
                        new SkillDef { Name = "Алые Перья Кары", Cd = 0f, Desc = "Выпускает острые бело-алые перья веером на лету." },
                        new SkillDef { Name = "Вспышка Обиды", Cd = 4.5f, Desc = "Звонкий чирик ярости: ускоряет атаки и накладывает кровотечение." },
                        new SkillDef { Name = "Голодный Рывок", Cd = 5.0f, Desc = "Мгновенный бросок к цели с укусом алой пасти." },
                        new SkillDef { Name = "Порхающий Наскок", Cd = 4.8f, Desc = "Юркий воздушный перелёт с уклонением от снарядов." },
                        new SkillDef { Name = "Крылья Возмездия", Cd = 5.2f, Desc = "Отскок и встречный таран на огромной скорости." },
                        new SkillDef { Name = "Разверстая Пасть Наказания!", Cd = 13.0f, Desc = "Грудь разрывается исполинской алой пастью, пожирающей врагов с колоссальным уроном!" },
                        new SkillDef { Name = "Красное Безумие Леса", Cd = 13.5f, Desc = "Серия бешеных укусов гигантских челюстей с вампиризмом от каждого попадания!" })
                });
            Classes["bird_trio"].Skins[0].Name = "Большая Птица [Слот 1]";
            Classes["bird_trio"].Skins[0].Desc = "Круглая угольно-чёрная птица со множеством горящих жёлтых глаз и Вечной Лампой.";
            Classes["bird_trio"].Skins[0].UnlockedByDefault = true;
            Classes["bird_trio"].Skins[0].ChangesModel = true;

            SetupCharLoadout("apocalypse_bird",
                "Когда три птицы слились воедино, чтобы спасти Чёрный Лес от чудовища, сами врата сумерек содрогнулись — ибо чудовищем стали они сами.",
                "Три Яйца Апокалипсиса", "Вокруг титана вращаются Яйца Глаз, Длинных Рук и Малого Клюва, даруя броню, регенерацию и золотые разряды.",
                new SkillDef { Name = "Луч Вечной Лампы", Cd = 0f, Desc = "Непрерывные сгустки золотого пламени и теней из пасти титана." },
                new SkillDef { Name = "Наклон Сумеречных Весов", Cd = 6.0f, Desc = "Поражает всех врагов вокруг бледным уроном правосудия и замедлением." },
                new SkillDef { Name = "Затмение Врат", Cd = 6.5f, Desc = "Взмывает на гигантских крыльях и обрушивается метеоритом тьмы." },
                new SkillDef { Name = "Страж Тёмного Леса", Cd = 16.0f, Desc = "Пробуждает все три Яйца: шквал золотых лучей, укусов и сокрушающих волн!" },
                new SkinDef { Name = "Сумеречный Судья", Desc = "Багрово-золотые перья заката и багровые зрачки на крыльях.", UnlockAchKey = "ach_slayer", UnlockReqText = "Истребите толпу врагов", BodyColor = Rgb(32, 14, 24), ArmorColor = Rgb(218, 64, 68), VisorColor = Rgb(255, 224, 96) },
                new SkinDef { Name = "Корона Вечной Ночи", Desc = "Звёздно-обсидиановый силуэт с сияющим венцом Сумерек и платиновыми Яйцами.", UnlockAchKey = "ach_boss", UnlockReqText = "Победите босса", ChangesModel = true, BodyColor = Rgb(12, 12, 24), ArmorColor = Rgb(255, 218, 98), VisorColor = Rgb(255, 248, 196) });

            SetupCharLoadout("komeiji",
                "Две сестры-сатори из подземного Дворца Земных Духов: проницательная Сатори с открытым Третьим Глазом и беззаботная Коиши, запечатавшая свой разум.",
                "Открытый Третий Глаз", "Видит слабые места врагов: критические попадания выпускают дополнительные ментальные нити.",
                new SkillDef { Name = "Спираль Алых Роз", Cd = 0f, Desc = "Вращающиеся лепестки данмаку, расходящиеся двойной волной." },
                new SkillDef { Name = "Воспоминание «Пламя Уцухо»", Cd = 5.5f, Desc = "Воссоздаёт миниатюрное подземное солнце, взрывающееся по площади." },
                new SkillDef { Name = "Перешагивание Памяти", Cd = 5.2f, Desc = "Рывок сквозь подсознание с кратковременной неуязвимостью." },
                new SkillDef { Name = "Дворец Земных Духов", Cd = 14.0f, Desc = "Раскрывает гигантский узор Третьего Глаза, заливая поле боя данмаку!" },
                new SkinDef
                {
                    Name = "Коиши Комейдзи [Альт. Версия]", Desc = "Младшая сестра с мятно-зелёными волосами, чёрной шляпкой с бантом и закрытым Третьим Глазом.",
                    UnlockedByDefault = false, ChangesModel = true,
                    UnlockAchKey = "ach_persona_komeiji_koishi",
                    UnlockReqText = "Играя за Сатори Комейдзи: Закрытый Третий Глаз — проведите 30 сек. в бою с врагами без получения урона",
                    BodyColor = Rgb(138, 226, 184), ArmorColor = Rgb(42, 92, 68), VisorColor = Rgb(248, 218, 68),
                    CustomSubtitle = "Закрытые Глаза Любви [Коиши]",
                    CustomPassiveName = "Неуловимое Подсознание",
                    CustomPassiveDesc = "Закрытый Третий Глаз скрывает присутствие Коиши в рывке и распускает колючие розы.",
                    CustomSkillVariants = BuildCustomSkinSkillSet("Коиши Комейдзи",
                        new SkillDef { Name = "Шипы Подсознания", Cd = 0f, Desc = "Причудливые зелёно-фиолетовые сердца и кинжалы данмаку из ниоткуда." },
                        new SkillDef { Name = "Нити Дешифратора", Cd = 0f, Desc = "Режущие волны психо-линий, проходящие сквозь нескольких врагов." },
                        new SkillDef { Name = "Инстинкт «Высвобождение Ид»", Cd = 4.8f, Desc = "Вихрь алых и изумрудных сердец вырывается вокруг Коиши." },
                        new SkillDef { Name = "Звонок «Сейчас я за тобой!»", Cd = 5.2f, Desc = "Мгновенный удар из-за спины ближайшего врага с критическим уроном!" },
                        new SkillDef { Name = "Прогулка Вне Сознания", Cd = 5.0f, Desc = "Становится полупрозрачной и стремительно скользит сквозь любые угрозы." },
                        new SkillDef { Name = "Камешек у Дороги", Cd = 5.5f, Desc = "Сбрасывает агрессию врагов и оставляет взрывающийся бутон розы." },
                        new SkillDef { Name = "«Подземная Роза»", Cd = 13.5f, Desc = "Распускает сияющий шторм роз подсознания, кружась в безудержном танце!" },
                        new SkillDef { Name = "Философия Ненавидимой Личности", Cd = 14.0f, Desc = "Последовательно обрушивает четыре кольца разноцветного данмаку вокруг себя!" })
                },
                new SkinDef { Name = "Двойная Роза Тирэйден [Модель]", Desc = "Готическое парадное платье с двойным ало-изумрудным ореолом обеих сестёр.", UnlockAchKey = "ach_boss", UnlockReqText = "Победите босса", ChangesModel = true, BodyColor = Rgb(56, 34, 74), ArmorColor = Rgb(228, 48, 96), VisorColor = Rgb(92, 242, 178) });
            Classes["komeiji"].Skins[0].Name = "Сатори Комейдзи";
            Classes["komeiji"].Skins[0].Desc = "Хозяйка Тирэйдена в нежно-розовой блузе с открытым алым Третьим Глазом.";
            Classes["komeiji"].Skins[0].UnlockedByDefault = true;
            Classes["komeiji"].Skins[0].ChangesModel = true;

            SetupCharLoadout("false_son",
                "Каменный сосуд, изваянный по образу Провидения и оживлённый золотой душой Аурелионита. Разбив лунные оковы, он крушит врагов мощью бури.",
                "Искажение и Рост", "Запас здоровья усиливает мощь навыков, а потраченные Лунные Шипы ускоряют бег и регенерацию.",
                new SkillDef { Name = "Раскол Небесного Камня", Cd = 0f, Desc = "Тяжёлый удар палицей об землю с веером золотых осколков." },
                new SkillDef { Name = "Колья Полнолуния", Cd = 5.0f, Desc = "Крупный залп детонирующих лунных кристаллов, приковывающих цели." },
                new SkillDef { Name = "Врата Меридиана", Cd = 6.0f, Desc = "Грозовой скачок с ударом молнии Аурелионита в точке прибытия." },
                new SkillDef { Name = "Вспышка Лазера Отца", Cd = 14.5f, Desc = "Мгновенный сверхмощный золотой взрыв и каскад небесных молний вокруг!" },
                new SkinDef { Name = "Аурелионитовый Первородный", Desc = "Храмовый базальт, полностью пронизанный чистым сияющим золотом.", UnlockAchKey = "ach_slayer", UnlockReqText = "Истребите толпу врагов", BodyColor = Rgb(62, 54, 42), ArmorColor = Rgb(218, 168, 44), VisorColor = Rgb(255, 236, 128) },
                new SkinDef { Name = "Истинный Наследник Провидения", Desc = "Белый мрамор Бури, бирюзовый плащ и полный венец защитника Петрихора V.", UnlockAchKey = "ach_boss", UnlockReqText = "Победите босса", ChangesModel = true, BodyColor = Rgb(232, 236, 245), ArmorColor = Rgb(44, 188, 214), VisorColor = Rgb(255, 212, 82) });
        }

        private static bool TryBuildExpansionPack2(string charKey, Entity root, SurvivorConfig cfg, SurvivorParts parts, int skinIdx)
        {
            parts.Root = root;
            var skin = cfg.GetActiveSkin(skinIdx);
            switch (charKey)
            {
                case "daitaku_helios":
                {
                    Color hairBlue = skinIdx == 1 ? Rgb(36, 24, 72) : Rgb(26, 38, 92);
                    Color streakCyan = skinIdx == 1 ? Rgb(162, 255, 58) : Rgb(0, 238, 255);
                    Color streakPink = skinIdx == 2 ? Rgb(255, 218, 68) : Rgb(255, 58, 192);
                    Color jacketCol = skin.BodyColor;
                    Color skinTone = Rgb(246, 210, 182);

                    var lHip = new Entity(parent: root, position: new(-0.18f, 0.88f, 0));
                    var rHip = new Entity(parent: root, position: new(0.18f, 0.88f, 0));
                    foreach (var hip in new[] { lHip, rHip })
                    {
                        P2Box(hip, new(0.17f, 0.42f, 0.17f), new(0, -0.22f, 0), skinTone);
                        P2Box(hip, new(0.19f, 0.34f, 0.19f), new(0, -0.56f, 0), Rgb(245, 245, 252));
                        P2Box(hip, new(0.20f, 0.06f, 0.20f), new(0, -0.42f, 0), streakPink, unlit: true);
                        P2Dia(hip, new(0.21f, 0.15f, 0.36f), new(0, -0.80f, 0.06f), jacketCol);
                        P2Box(hip, new(0.22f, 0.05f, 0.38f), new(0, -0.85f, 0.06f), streakCyan, unlit: true);
                    }
                    parts.LeftLeg = lHip; parts.RightLeg = rHip;

                    var torso = new Entity(parent: root, position: new(0, 1.25f, 0));
                    P2Box(torso, new(0.44f, 0.50f, 0.28f), new(0, 0.06f, 0), Rgb(250, 248, 255));
                    P2Box(torso, new(0.52f, 0.46f, 0.34f), new(0, 0.10f, -0.01f), jacketCol);
                    P2Box(torso, new(0.18f, 0.44f, 0.36f), new(0, 0.09f, 0.02f), streakPink, unlit: true);
                    P2Box(torso, new(0.50f, 0.16f, 0.34f), new(0, -0.24f, 0), Rgb(32, 36, 56));
                    P2Box(torso, new(0.52f, 0.06f, 0.36f), new(0, -0.16f, 0), Rgb(255, 214, 64), unlit: true);
                    // DJ-наушники на шее
                    P2Box(torso, new(0.36f, 0.08f, 0.32f), new(0, 0.36f, 0.02f), Rgb(28, 30, 42));
                    foreach (int s in new[] { -1, 1 })
                    {
                        P2Sph(torso, new(0.14f, 0.15f, 0.12f), new(s * 0.20f, 0.36f, 0.10f), streakCyan, unlit: true);
                        P2Dia(torso, new(0.08f, 0.08f, 0.08f), new(s * 0.23f, 0.36f, 0.13f), streakPink, unlit: true);
                    }
                    // Лошадиный хвост с неоновой прядью!
                    var horseTail = new Entity(parent: torso, position: new(0, -0.20f, -0.18f), rotation: new(24f, 0, 0));
                    P2Dia(horseTail, new(0.24f, 0.72f, 0.22f), new(0, -0.30f, -0.08f), hairBlue);
                    P2Dia(horseTail, new(0.12f, 0.64f, 0.14f), new(0.05f, -0.34f, -0.10f), streakCyan, unlit: true);
                    parts.Tails.Add(horseTail);

                    var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
                    P2Sph(head, new(0.46f, 0.46f, 0.45f), Vector3.Zero, skinTone);
                    foreach (int s in new[] { -1, 1 })
                        P2Sph(head, new(0.11f, 0.11f, 0.07f), new(s * 0.11f, 0.03f, 0.20f), streakPink, unlit: true);
                    P2Sph(head, new(0.52f, 0.46f, 0.52f), new(0, 0.12f, -0.03f), hairBlue);
                    // Неоновые бирюзовые и маджента пряди гяру + стоячие конские ушки со звёздочками ★
                    P2Dia(head, new(0.14f, 0.34f, 0.14f), new(-0.14f, 0.06f, 0.20f), streakCyan, new(8f, 0, 14f), unlit: true);
                    P2Dia(head, new(0.14f, 0.34f, 0.14f), new(0.14f, 0.06f, 0.20f), streakPink, new(8f, 0, -14f), unlit: true);
                    foreach (int s in new[] { -1, 1 })
                    {
                        P2Dia(head, new(0.13f, 0.34f, 0.11f), new(s * 0.16f, 0.38f, 0.02f), hairBlue, new(-6f, 0, s * -10f));
                        P2Dia(head, new(0.07f, 0.22f, 0.07f), new(s * 0.16f, 0.37f, 0.05f), streakPink, new(-6f, 0, s * -10f), unlit: true);
                    }
                    P2Dia(head, new(0.14f, 0.14f, 0.06f), new(-0.22f, 0.24f, 0.16f), Rgb(255, 224, 52), new(0, 0, 22f), unlit: true);

                    var lArm = new Entity(parent: torso, position: new(-0.36f, 0.24f, 0));
                    var rArm = new Entity(parent: torso, position: new(0.36f, 0.24f, 0));
                    foreach (var (s, arm) in new[] { (-1, lArm), (1, rArm) })
                    {
                        P2Sph(arm, new(0.18f, 0.18f, 0.18f), Vector3.Zero, jacketCol);
                        P2Box(arm, new(0.15f, 0.36f, 0.15f), new(0, -0.20f, 0), jacketCol);
                        P2Box(arm, new(0.16f, 0.07f, 0.16f), new(0, -0.34f, 0), s < 0 ? streakCyan : streakPink, unlit: true);
                        P2Sph(arm, new(0.12f, 0.12f, 0.12f), new(0, -0.42f, 0), skinTone);
                    }
                    // Аура вертушки DJ в ульте (UltFormNodes)
                    var djRig = new Entity(parent: torso, position: new(0, -0.32f, 0), enabled: false);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * MathF.PI * 0.25f;
                        P2Dia(djRig, new(0.26f, 0.08f, 0.26f), new(MathF.Cos(a) * 0.95f, 0, MathF.Sin(a) * 0.95f),
                            i % 2 == 0 ? streakCyan : streakPink, new(0, i * 45f, 0), unlit: true);
                    }
                    parts.UltFormNodes.Add(djRig);
                    parts.Head = head; parts.Torso = torso; parts.LeftArm = lArm; parts.RightArm = rArm;
                    return true;
                }
                case "robin":
                {
                    Color gownWhite = skin.BodyColor;
                    Color lavTrim = skin.ArmorColor;
                    Color goldCol = skin.VisorColor;
                    Color hairPeri = skinIdx == 2 ? Rgb(232, 236, 255) : Rgb(182, 194, 246);
                    Color skinTone = Rgb(252, 232, 222);

                    var lHip = new Entity(parent: root, position: new(-0.15f, 0.88f, 0));
                    var rHip = new Entity(parent: root, position: new(0.15f, 0.88f, 0));
                    foreach (var hip in new[] { lHip, rHip })
                    {
                        P2Box(hip, new(0.15f, 0.66f, 0.15f), new(0, -0.36f, 0), skinTone);
                        P2Dia(hip, new(0.17f, 0.14f, 0.32f), new(0, -0.80f, 0.06f), gownWhite);
                        P2Box(hip, new(0.16f, 0.05f, 0.30f), new(0, -0.85f, 0.06f), goldCol, unlit: true);
                    }
                    parts.LeftLeg = lHip; parts.RightLeg = rHip;

                    var torso = new Entity(parent: root, position: new(0, 1.26f, 0));
                    P2Box(torso, new(0.42f, 0.48f, 0.28f), new(0, 0.08f, 0), gownWhite);
                    P2Dia(torso, new(0.68f, 0.48f, 0.56f), new(0, -0.26f, -0.02f), gownWhite);
                    P2Dia(torso, new(0.74f, 0.32f, 0.62f), new(0, -0.34f, -0.04f), lavTrim);
                    P2Dia(torso, new(0.18f, 0.22f, 0.10f), new(0, 0.22f, 0.16f), goldCol, unlit: true);
                    for (int i = -1; i <= 1; i++)
                        P2Dia(torso, new(0.16f, 0.52f, 0.10f), new(i * 0.18f, -0.44f, -0.26f), goldCol, new(18f, 0, i * 10f), unlit: true);

                    var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
                    P2Sph(head, new(0.45f, 0.46f, 0.45f), Vector3.Zero, skinTone);
                    foreach (int s in new[] { -1, 1 })
                        P2Sph(head, new(0.10f, 0.10f, 0.06f), new(s * 0.11f, 0.03f, 0.20f), Rgb(78, 218, 186), unlit: true);
                    P2Sph(head, new(0.52f, 0.46f, 0.52f), new(0, 0.12f, -0.04f), hairPeri);
                    var backHair = new Entity(parent: head, position: new(0, -0.06f, -0.20f), rotation: new(14f, 0, 0));
                    P2Dia(backHair, new(0.44f, 0.78f, 0.22f), new(0, -0.34f, -0.04f), hairPeri);
                    parts.Tails.Add(backHair);
                    // Пара маленьких белых крылышек галовианки за ушами!
                    foreach (int s in new[] { -1, 1 })
                    {
                        P2Dia(head, new(0.28f, 0.14f, 0.08f), new(s * 0.29f, 0.06f, -0.06f), Color.White, new(0, s * -22f, s * 24f), unlit: true);
                        P2Dia(head, new(0.20f, 0.10f, 0.07f), new(s * 0.26f, -0.03f, -0.08f), Rgb(232, 240, 255), new(0, s * -22f, s * 8f), unlit: true);
                    }
                    // Сияющий золотой Нимб Гармонии (parts.Halo)
                    var halo = new Entity(parent: head, position: new(0, 0.36f, -0.14f), rotation: new(18f, 0, 0));
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * MathF.PI * 0.25f;
                        P2Dia(halo, new(0.18f, 0.07f, 0.07f), new(MathF.Cos(a) * 0.36f, MathF.Sin(a) * 0.36f, 0), goldCol, new(0, 0, i * 45f + 90f), unlit: true);
                    }
                    P2Dia(halo, new(0.14f, 0.24f, 0.08f), new(0, 0.38f, 0), goldCol, unlit: true);
                    parts.Halo = halo;

                    var lArm = new Entity(parent: torso, position: new(-0.34f, 0.24f, 0));
                    var rArm = new Entity(parent: torso, position: new(0.34f, 0.24f, 0));
                    foreach (var arm in new[] { lArm, rArm })
                    {
                        P2Box(arm, new(0.13f, 0.24f, 0.13f), new(0, -0.12f, 0), skinTone);
                        P2Box(arm, new(0.14f, 0.22f, 0.14f), new(0, -0.32f, 0), gownWhite);
                        P2Sph(arm, new(0.11f, 0.11f, 0.11f), new(0, -0.44f, 0), gownWhite);
                    }
                    // Серебристо-золотой ретро-микрофон со стойкой в правой руке!
                    var mic = new Entity(parent: rArm, position: new(0, -0.44f, 0.12f), rotation: new(55f, 0, 0));
                    P2Box(mic, new(0.04f, 1.15f, 0.04f), new(0, 0.22f, 0), Rgb(220, 228, 242), unlit: true);
                    P2Sph(mic, new(0.16f, 0.20f, 0.16f), new(0, 0.84f, 0), goldCol, unlit: true);
                    P2Dia(mic, new(0.24f, 0.18f, 0.12f), new(0, 0.78f, 0), Rgb(235, 242, 255), unlit: true);

                    // Космические золотые крылья и нотное кольцо концерта (UltFormNodes)
                    var concertUlt = new Entity(parent: torso, position: Vector3.Zero, enabled: false);
                    foreach (int s in new[] { -1, 1 })
                        for (int w = 0; w < 3; w++)
                            P2Dia(concertUlt, new(0.85f - w * 0.16f, 0.20f, 0.08f), new(s * (0.58f + w * 0.14f), 0.26f - w * 0.18f, -0.22f),
                                w == 1 ? lavTrim : goldCol, new(0, s * -16f, s * (26f - w * 14f)), unlit: true);
                    for (int n = 0; n < 6; n++)
                    {
                        float a = n * (MathF.PI / 3f);
                        P2Dia(concertUlt, new(0.16f, 0.22f, 0.16f), new(MathF.Cos(a) * 1.05f, -0.18f, MathF.Sin(a) * 1.05f), goldCol, unlit: true);
                    }
                    parts.UltFormNodes.Add(concertUlt);
                    parts.Head = head; parts.Torso = torso; parts.LeftArm = lArm; parts.RightArm = rArm;
                    return true;
                }
                case "bird_trio":
                {
                    if (skinIdx == 1)
                    {
                        // ДЛИННАЯ ПТИЦА [Слот 2]: длинные золотистые лапы-ходули, забинтованная голова, Несправедливые Весы
                        Color bodyDark = Rgb(22, 22, 30);
                        Color bandage = Rgb(242, 238, 228);
                        Color bandageShade = Rgb(210, 204, 192);
                        Color stiltGold = Rgb(214, 168, 68);
                        Color scaleGold = Rgb(255, 212, 64);

                        var lHip = new Entity(parent: root, position: new(-0.18f, 1.32f, 0));
                        var rHip = new Entity(parent: root, position: new(0.18f, 1.32f, 0));
                        foreach (var hip in new[] { lHip, rHip })
                        {
                            P2Box(hip, new(0.09f, 0.68f, 0.09f), new(0, -0.34f, 0), stiltGold);
                            P2Sph(hip, new(0.13f, 0.13f, 0.13f), new(0, -0.68f, -0.02f), stiltGold);
                            P2Box(hip, new(0.07f, 0.64f, 0.07f), new(0, -0.98f, 0.02f), stiltGold);
                            foreach (int c in new[] { -1, 0, 1 })
                                P2Dia(hip, new(0.06f, 0.06f, 0.24f), new(c * 0.07f, -1.29f, 0.10f), stiltGold, new(0, c * 18f, 0));
                        }
                        parts.LeftLeg = lHip; parts.RightLeg = rHip;

                        var torso = new Entity(parent: root, position: new(0, 1.56f, 0));
                        P2Sph(torso, new(0.52f, 0.88f, 0.48f), Vector3.Zero, bodyDark);
                        P2Dia(torso, new(0.58f, 0.52f, 0.50f), new(0, 0.20f, 0), Rgb(34, 32, 46));
                        P2Dia(torso, new(0.32f, 0.78f, 0.22f), new(0, -0.46f, -0.22f), bodyDark, new(22f, 0, 0));
                        // Длинная шея с кольцами бинтов
                        P2Box(torso, new(0.18f, 0.56f, 0.18f), new(0, 0.56f, 0.04f), bodyDark, new(-8f, 0, 0));
                        for (int b = 0; b < 3; b++)
                            P2Box(torso, new(0.21f, 0.07f, 0.21f), new(0, 0.46f + b * 0.13f, 0.04f), b % 2 == 0 ? bandage : bandageShade, new(6f, b * 20f, -6f));

                        var head = new Entity(parent: torso, position: new(0, 0.90f, 0.08f));
                        P2Sph(head, new(0.38f, 0.44f, 0.42f), Vector3.Zero, bandage);
                        for (int b = 0; b < 4; b++)
                            P2Box(head, new(0.41f, 0.08f, 0.44f), new(0, -0.12f + b * 0.09f, 0), b % 2 == 0 ? bandageShade : bandage, new(b * 8f - 10f, b * 15f, (b % 2 == 0 ? 1 : -1) * 12f));
                        P2Dia(head, new(0.14f, 0.14f, 0.36f), new(0, -0.04f, 0.28f), stiltGold, new(8f, 0, 0));
                        var bandageTail = new Entity(parent: head, position: new(0, -0.06f, -0.20f), rotation: new(18f, 0, 0));
                        P2Dia(bandageTail, new(0.14f, 0.58f, 0.08f), new(0, -0.26f, -0.04f), bandage);
                        parts.Tails.Add(bandageTail);

                        var lWing = new Entity(parent: torso, position: new(-0.28f, 0.22f, 0));
                        var rWing = new Entity(parent: torso, position: new(0.28f, 0.22f, 0));
                        foreach (var (s, w) in new[] { (-1, lWing), (1, rWing) })
                            for (int f = 0; f < 4; f++)
                                P2Dia(w, new(0.18f, 0.72f - f * 0.08f, 0.26f), new(s * (0.12f + f * 0.08f), -0.28f - f * 0.06f, -0.04f * f),
                                    bodyDark, new(8f, 0, s * (16f + f * 8f)));

                        // Несправедливые Золотые Весы на правом крыле!
                        var scaleRig = new Entity(parent: rWing, position: new(0.22f, -0.16f, 0.42f));
                        P2Box(scaleRig, new(0.05f, 0.46f, 0.05f), new(0, 0.08f, 0), scaleGold, unlit: true);
                        var beam = new Entity(parent: scaleRig, position: new(0, 0.26f, 0), rotation: new(0, 0, -18f));
                        P2Box(beam, new(0.76f, 0.05f, 0.05f), Vector3.Zero, scaleGold, unlit: true);
                        foreach (int ps in new[] { -1, 1 })
                        {
                            P2Box(beam, new(0.03f, 0.34f, 0.03f), new(ps * 0.34f, -0.18f, 0), scaleGold, unlit: true);
                            P2Sph(beam, new(0.26f, 0.09f, 0.26f), new(ps * 0.34f, -0.36f, 0), scaleGold, unlit: true);
                        }

                        var judgeUlt = new Entity(parent: head, position: new(0, 0.34f, 0), enabled: false);
                        for (int i = 0; i < 6; i++)
                        {
                            float a = i * (MathF.PI / 3f);
                            P2Dia(judgeUlt, new(0.22f, 0.08f, 0.08f), new(MathF.Cos(a) * 0.52f, 0, MathF.Sin(a) * 0.52f), scaleGold, new(0, i * 60f, 0), unlit: true);
                        }
                        parts.UltFormNodes.Add(judgeUlt);
                        parts.Head = head; parts.Torso = torso; parts.LeftArm = lWing; parts.RightArm = rWing;
                        return true;
                    }
                    if (skinIdx == 2)
                    {
                        // МАЛЕНЬКАЯ ПТИЦА [Слот 3]: белоснежный комочек с алым пятнышком + гигантская алая пасть в UltFormNodes!
                        Color snowWhite = Rgb(250, 250, 255);
                        Color crimson = Rgb(218, 22, 46);
                        Color darkLeg = Rgb(36, 32, 38);

                        var lHip = new Entity(parent: root, position: new(-0.12f, 0.38f, 0));
                        var rHip = new Entity(parent: root, position: new(0.12f, 0.38f, 0));
                        foreach (var hip in new[] { lHip, rHip })
                        {
                            P2Box(hip, new(0.05f, 0.36f, 0.05f), new(0, -0.18f, 0), darkLeg);
                            P2Dia(hip, new(0.12f, 0.05f, 0.20f), new(0, -0.36f, 0.05f), darkLeg);
                        }
                        parts.LeftLeg = lHip; parts.RightLeg = rHip;

                        var torso = new Entity(parent: root, position: new(0, 0.68f, 0));
                        P2Sph(torso, new(0.68f, 0.64f, 0.72f), Vector3.Zero, snowWhite);
                        var tail = new Entity(parent: torso, position: new(0, 0.04f, -0.34f), rotation: new(28f, 0, 0));
                        P2Dia(tail, new(0.28f, 0.32f, 0.16f), new(0, 0.10f, -0.08f), snowWhite);
                        parts.Tails.Add(tail);

                        // Обычное милое алое пятнышко на грудке (NormalFormNodes)
                        var cuteSpot = new Entity(parent: torso, position: new(0, -0.02f, 0.34f));
                        P2Dia(cuteSpot, new(0.20f, 0.24f, 0.08f), Vector3.Zero, crimson, unlit: true);
                        parts.NormalFormNodes.Add(cuteSpot);

                        // ИСПОЛИНСКАЯ УРОДЛИВАЯ АЛАЯ ПАСТЬ С БЕЛЫМИ ЗУБАМИ из груди (UltFormNodes, изначально скрыта!)
                        var giantMaw = new Entity(parent: torso, position: new(0, 0.12f, 0.28f), enabled: false);
                        P2Sph(giantMaw, new(0.96f, 1.05f, 0.78f), new(0, 0, 0.14f), Rgb(125, 10, 24), unlit: true);
                        P2Dia(giantMaw, new(1.06f, 0.44f, 0.94f), new(0, 0.42f, 0.36f), crimson, new(-28f, 0, 0), unlit: true);
                        P2Dia(giantMaw, new(1.02f, 0.42f, 0.92f), new(0, -0.40f, 0.36f), crimson, new(28f, 0, 0), unlit: true);
                        for (int tIdx = -3; tIdx <= 3; tIdx++)
                        {
                            P2Dia(giantMaw, new(0.11f, 0.28f, 0.11f), new(tIdx * 0.12f, 0.22f, 0.68f - MathF.Abs(tIdx) * 0.05f), Color.White, unlit: true);
                            P2Dia(giantMaw, new(0.11f, 0.26f, 0.11f), new(tIdx * 0.12f, -0.22f, 0.68f - MathF.Abs(tIdx) * 0.05f), Color.White, unlit: true);
                        }
                        parts.UltFormNodes.Add(giantMaw);

                        var head = new Entity(parent: torso, position: new(0, 0.36f, 0.14f));
                        P2Sph(head, new(0.46f, 0.44f, 0.46f), Vector3.Zero, snowWhite);
                        foreach (int s in new[] { -1, 1 })
                            P2Sph(head, new(0.08f, 0.08f, 0.06f), new(s * 0.14f, 0.05f, 0.20f), Rgb(18, 18, 24));
                        P2Dia(head, new(0.11f, 0.10f, 0.20f), new(0, 0.01f, 0.26f), darkLeg);
                        P2Dia(head, new(0.10f, 0.18f, 0.10f), new(0, 0.26f, -0.02f), snowWhite, new(-16f, 0, 0));

                        var lWing = new Entity(parent: torso, position: new(-0.32f, 0.06f, 0));
                        var rWing = new Entity(parent: torso, position: new(0.32f, 0.06f, 0));
                        foreach (var (s, w) in new[] { (-1, lWing), (1, rWing) })
                            P2Dia(w, new(0.16f, 0.36f, 0.32f), new(s * 0.08f, -0.08f, 0), Rgb(238, 240, 248), new(0, 0, s * 22f));

                        parts.Head = head; parts.Torso = torso; parts.LeftArm = lWing; parts.RightArm = rWing;
                        return true;
                    }
                    else
                    {
                        // БОЛЬШАЯ ПТИЦА [Слот 1]: круглое угольно-чёрное тело в десятках жёлтых глаз + Вечная Лампа!
                        Color abyssBlack = Rgb(16, 16, 22);
                        Color eyeGold = Rgb(255, 224, 42);
                        Color eyeOrange = Rgb(255, 148, 24);
                        Color beakYellow = Rgb(238, 192, 54);

                        var lHip = new Entity(parent: root, position: new(-0.26f, 0.56f, 0));
                        var rHip = new Entity(parent: root, position: new(0.26f, 0.56f, 0));
                        foreach (var hip in new[] { lHip, rHip })
                        {
                            P2Box(hip, new(0.12f, 0.52f, 0.12f), new(0, -0.26f, 0), beakYellow);
                            foreach (int c in new[] { -1, 0, 1 })
                                P2Dia(hip, new(0.08f, 0.07f, 0.26f), new(c * 0.08f, -0.52f, 0.08f), beakYellow, new(0, c * 18f, 0));
                        }
                        parts.LeftLeg = lHip; parts.RightLeg = rHip;

                        var torso = new Entity(parent: root, position: new(0, 1.12f, 0));
                        P2Sph(torso, new(1.18f, 1.14f, 1.16f), Vector3.Zero, abyssBlack);
                        // Десятки горящих жёлтых глаз по всему круглому телу!
                        for (int ring = 0; ring < 3; ring++)
                        {
                            int cnt = ring == 1 ? 10 : 7;
                            float ry = (ring - 1) * 0.26f;
                            float rad = ring == 1 ? 0.57f : 0.50f;
                            for (int i = 0; i < cnt; i++)
                            {
                                float a = i * (MathF.Tau / cnt) + ring * 0.35f;
                                Vector3 ep = new(MathF.Sin(a) * rad, ry, MathF.Cos(a) * rad);
                                P2Sph(torso, new(0.14f, 0.11f, 0.08f), ep, (i + ring) % 3 == 0 ? eyeOrange : eyeGold, new(0, a * 57.3f, 0), unlit: true);
                                P2Sph(torso, new(0.05f, 0.07f, 0.09f), ep * 1.03f, abyssBlack);
                            }
                        }

                        var head = new Entity(parent: torso, position: new(0, 0.44f, 0.24f));
                        P2Sph(head, new(0.68f, 0.56f, 0.64f), Vector3.Zero, abyssBlack);
                        foreach (int s in new[] { -1, 1 })
                        {
                            P2Sph(head, new(0.18f, 0.16f, 0.10f), new(s * 0.20f, 0.08f, 0.26f), eyeGold, unlit: true);
                            P2Sph(head, new(0.11f, 0.10f, 0.08f), new(s * 0.10f, 0.20f, 0.22f), eyeOrange, unlit: true);
                        }
                        var normalBeak = P2Dia(head, new(0.24f, 0.18f, 0.36f), new(0, -0.06f, 0.36f), beakYellow);
                        parts.NormalFormNodes.Add(normalBeak);

                        // Распахнутая зубастая челюсть со свечением глаз в ульте (UltFormNodes)
                        var jawFlare = new Entity(parent: head, position: new(0, -0.06f, 0.34f), enabled: false);
                        P2Dia(jawFlare, new(0.46f, 0.22f, 0.54f), new(0, 0.16f, 0.12f), beakYellow, new(-26f, 0, 0), unlit: true);
                        P2Dia(jawFlare, new(0.44f, 0.22f, 0.52f), new(0, -0.18f, 0.12f), beakYellow, new(28f, 0, 0), unlit: true);
                        P2Sph(jawFlare, new(0.36f, 0.32f, 0.36f), new(0, 0, 0.04f), eyeGold, unlit: true);
                        parts.UltFormNodes.Add(jawFlare);

                        var lWing = new Entity(parent: torso, position: new(-0.54f, 0.08f, 0));
                        var rWing = new Entity(parent: torso, position: new(0.54f, 0.08f, 0));
                        foreach (var (s, w) in new[] { (-1, lWing), (1, rWing) })
                            P2Dia(w, new(0.22f, 0.58f, 0.48f), new(s * 0.12f, -0.14f, 0), Rgb(26, 26, 36), new(0, 0, s * 18f));

                        // Вечная Лампа (золотой фонарь на цепочке в правом крыле!)
                        var lamp = new Entity(parent: rWing, position: new(0.18f, -0.18f, 0.46f));
                        P2Box(lamp, new(0.04f, 0.26f, 0.04f), new(0, 0.14f, 0), Rgb(188, 142, 42));
                        P2Dia(lamp, new(0.28f, 0.12f, 0.28f), new(0, -0.02f, 0), Rgb(168, 122, 32));
                        P2Sph(lamp, new(0.22f, 0.28f, 0.22f), new(0, -0.16f, 0), eyeGold, unlit: true);
                        P2Box(lamp, new(0.26f, 0.06f, 0.26f), new(0, -0.32f, 0), Rgb(168, 122, 32));

                        parts.Head = head; parts.Torso = torso; parts.LeftArm = lWing; parts.RightArm = rWing;
                        return true;
                    }
                }
                case "apocalypse_bird":
                {
                    Color obsidian = skin.BodyColor;
                    Color trimGold = skin.ArmorColor;
                    Color eyeBlaze = skin.VisorColor;
                    Color mawRed = Rgb(212, 28, 52);

                    var lHip = new Entity(parent: root, position: new(-0.32f, 1.12f, 0));
                    var rHip = new Entity(parent: root, position: new(0.32f, 1.12f, 0));
                    foreach (var hip in new[] { lHip, rHip })
                    {
                        P2Dia(hip, new(0.32f, 0.58f, 0.32f), new(0, -0.28f, 0), obsidian);
                        P2Box(hip, new(0.14f, 0.56f, 0.14f), new(0, -0.78f, 0), trimGold);
                        foreach (int c in new[] { -1, 0, 1 })
                            P2Dia(hip, new(0.09f, 0.08f, 0.34f), new(c * 0.10f, -1.08f, 0.12f), eyeBlaze, new(0, c * 18f, 0), unlit: true);
                    }
                    parts.LeftLeg = lHip; parts.RightLeg = rHip;

                    var torso = new Entity(parent: root, position: new(0, 1.56f, 0));
                    P2Sph(torso, new(0.96f, 1.18f, 0.88f), Vector3.Zero, obsidian);
                    // Алая разверстая грудная щель Маленькой Птицы и золотые глаза на груди титана
                    P2Dia(torso, new(0.34f, 0.68f, 0.18f), new(0, 0.02f, 0.40f), mawRed, unlit: true);
                    for (int tIdx = -2; tIdx <= 2; tIdx++)
                        foreach (int side in new[] { -1, 1 })
                            P2Dia(torso, new(0.08f, 0.06f, 0.08f), new(side * 0.10f, tIdx * 0.11f, 0.46f), Color.White, unlit: true);

                    var head = new Entity(parent: torso, position: new(0, 0.72f, 0.12f));
                    P2Sph(head, new(0.56f, 0.54f, 0.58f), Vector3.Zero, obsidian);
                    P2Dia(head, new(0.28f, 0.24f, 0.56f), new(0, -0.04f, 0.42f), eyeBlaze, new(8f, 0, 0), unlit: true);
                    for (int c = -2; c <= 2; c++)
                        P2Dia(head, new(0.10f, 0.30f - MathF.Abs(c) * 0.04f, 0.10f), new(c * 0.10f, 0.34f, -0.02f), eyeBlaze, new(-10f, 0, c * -10f), unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        P2Sph(head, new(0.16f, 0.12f, 0.10f), new(s * 0.20f, 0.08f, 0.24f), eyeBlaze, unlit: true);

                    // Наклонённый Нимб-Весы Сумерек (parts.Halo)
                    var scaleHalo = new Entity(parent: head, position: new(0, 0.46f, -0.18f), rotation: new(16f, 0, -14f));
                    P2Box(scaleHalo, new(1.18f, 0.06f, 0.06f), Vector3.Zero, eyeBlaze, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        P2Dia(scaleHalo, new(0.26f, 0.14f, 0.26f), new(s * 0.56f, -0.18f, 0), trimGold, unlit: true);
                    parts.Halo = scaleHalo;

                    // Исполинские тёмные крылья с горящими золотыми глазами + золотые когтистые лапы!
                    var lArm = new Entity(parent: torso, position: new(-0.48f, 0.34f, 0));
                    var rArm = new Entity(parent: torso, position: new(0.48f, 0.34f, 0));
                    foreach (var (s, arm) in new[] { (-1, lArm), (1, rArm) })
                    {
                        // Когтистая золотая рука титана
                        P2Box(arm, new(0.16f, 0.52f, 0.16f), new(s * 0.08f, -0.24f, 0.24f), trimGold, new(-38f, 0, s * 12f));
                        for (int k = -1; k <= 1; k++)
                            P2Dia(arm, new(0.07f, 0.28f, 0.09f), new(s * 0.10f + k * 0.07f, -0.44f, 0.52f), eyeBlaze, new(65f, 0, 0), unlit: true);
                        // Огромное крыло в золотых глазах
                        for (int seg = 0; seg < 4; seg++)
                        {
                            float wx = s * (0.38f + seg * 0.36f);
                            float wy = 0.24f - seg * 0.14f;
                            P2Dia(arm, new(0.56f, 0.96f - seg * 0.12f, 0.14f), new(wx, wy, -0.20f), obsidian, new(10f, s * -12f, s * (24f + seg * 8f)));
                            P2Sph(arm, new(0.18f, 0.12f, 0.16f), new(wx, wy + 0.10f, -0.14f), eyeBlaze, unlit: true);
                            P2Sph(arm, new(0.14f, 0.10f, 0.16f), new(wx - s * 0.08f, wy - 0.18f, -0.14f), Rgb(255, 138, 28), unlit: true);
                        }
                    }

                    // ТРИ ПАРЯЩИХ ЯЙЦА (Яйцо Больших Глаз, Яйцо Длинных Рук, Яйцо Малого Клюва) на орбите вокруг торса!
                    var eggOrbit = new Entity(parent: torso, position: new(0, -0.10f, 0));
                    // 1. Яйцо Больших Глаз
                    var bigEgg = P2Sph(eggOrbit, new(0.28f, 0.36f, 0.28f), new(0.98f, 0, 0), obsidian);
                    for (int e = 0; e < 4; e++)
                        P2Sph(bigEgg, new(0.32f, 0.24f, 0.24f), new(MathF.Cos(e * 1.57f) * 0.42f, (e % 2 - 0.5f) * 0.32f, MathF.Sin(e * 1.57f) * 0.42f), eyeBlaze, unlit: true);
                    // 2. Яйцо Длинных Рук
                    var longEgg = P2Sph(eggOrbit, new(0.24f, 0.40f, 0.24f), new(-0.49f, 0.10f, 0.85f), Rgb(240, 236, 224));
                    P2Box(longEgg, new(1.15f, 0.18f, 1.15f), Vector3.Zero, eyeBlaze, new(18f, 0, -18f), unlit: true);
                    // 3. Яйцо Малого Клюва
                    var smallEgg = P2Sph(eggOrbit, new(0.26f, 0.34f, 0.26f), new(-0.49f, -0.08f, -0.85f), Color.White);
                    P2Dia(smallEgg, new(1.10f, 0.32f, 1.10f), Vector3.Zero, mawRed, unlit: true);
                    parts.Tails.Add(eggOrbit);

                    var apocUlt = new Entity(parent: torso, position: Vector3.Zero, enabled: false);
                    for (int r = 0; r < 6; r++)
                    {
                        float a = r * (MathF.PI / 3f);
                        P2Dia(apocUlt, new(0.24f, 0.95f, 0.24f), new(MathF.Cos(a) * 1.35f, 0.15f, MathF.Sin(a) * 1.35f), eyeBlaze, new(0, 0, 25f), unlit: true);
                    }
                    parts.UltFormNodes.Add(apocUlt);
                    parts.Head = head; parts.Torso = torso; parts.LeftArm = lArm; parts.RightArm = rArm;
                    return true;
                }
                case "komeiji":
                {
                    bool isKoishi = skinIdx == 1;
                    bool isDual = skinIdx == 2;
                    Color hairCol = isKoishi ? Rgb(134, 224, 178) : (isDual ? Rgb(218, 162, 212) : Rgb(242, 178, 208));
                    Color topCol = isKoishi ? Rgb(248, 222, 78) : (isDual ? Rgb(56, 32, 76) : Rgb(252, 214, 230));
                    Color skirtCol = isKoishi ? Rgb(36, 88, 62) : (isDual ? Rgb(34, 20, 48) : Rgb(66, 110, 192));
                    Color eyeMain = isKoishi ? Rgb(76, 56, 162) : Rgb(235, 32, 76);
                    Color cordA = isKoishi ? Rgb(92, 68, 186) : Rgb(228, 38, 72);
                    Color cordB = isKoishi ? Rgb(62, 192, 152) : Rgb(64, 126, 228);
                    Color skinTone = Rgb(252, 232, 224);

                    var lHip = new Entity(parent: root, position: new(-0.15f, 0.86f, 0));
                    var rHip = new Entity(parent: root, position: new(0.15f, 0.86f, 0));
                    foreach (var hip in new[] { lHip, rHip })
                    {
                        P2Box(hip, new(0.15f, 0.62f, 0.15f), new(0, -0.34f, 0), Color.White);
                        P2Dia(hip, new(0.18f, 0.14f, 0.30f), new(0, -0.78f, 0.05f), isKoishi ? Rgb(28, 28, 36) : (isDual ? Rgb(168, 28, 68) : Rgb(236, 146, 178)));
                    }
                    parts.LeftLeg = lHip; parts.RightLeg = rHip;

                    var torso = new Entity(parent: root, position: new(0, 1.24f, 0));
                    P2Box(torso, new(0.42f, 0.46f, 0.28f), new(0, 0.08f, 0), topCol);
                    P2Dia(torso, new(0.68f, 0.44f, 0.56f), new(0, -0.26f, 0), skirtCol);
                    P2Dia(torso, new(0.72f, 0.14f, 0.60f), new(0, -0.42f, 0), isDual ? Rgb(228, 48, 96) : Rgb(248, 242, 248));
                    for (int b = 0; b < 3; b++)
                        P2Dia(torso, new(0.08f, 0.08f, 0.06f), new(0, 0.22f - b * 0.11f, 0.15f), isKoishi ? Rgb(36, 88, 62) : Rgb(252, 218, 72), unlit: true);

                    var head = new Entity(parent: torso, position: new(0, 0.56f, 0));
                    P2Sph(head, new(0.45f, 0.45f, 0.44f), Vector3.Zero, skinTone);
                    foreach (int s in new[] { -1, 1 })
                        P2Sph(head, new(0.10f, 0.10f, 0.06f), new(s * 0.11f, 0.02f, 0.20f), isKoishi ? Rgb(52, 198, 152) : Rgb(225, 48, 92), unlit: true);
                    P2Sph(head, new(0.52f, 0.46f, 0.52f), new(0, 0.11f, -0.03f), hairCol);
                    foreach (int s in new[] { -1, 1 })
                        P2Dia(head, new(0.16f, 0.36f, 0.18f), new(s * 0.23f, -0.04f, 0.06f), isDual && s > 0 ? Rgb(134, 224, 178) : hairCol, new(0, 0, s * 10f));

                    if (isKoishi)
                    {
                        // Чёрная круглая шляпка Коиши с большим жёлтым бантом!
                        P2Sph(head, new(0.84f, 0.10f, 0.84f), new(0, 0.26f, 0), Rgb(24, 24, 32), new(-8f, 0, 0));
                        P2Sph(head, new(0.50f, 0.28f, 0.50f), new(0, 0.36f, -0.02f), Rgb(28, 28, 38), new(-8f, 0, 0));
                        P2Box(head, new(0.52f, 0.06f, 0.52f), new(0, 0.29f, -0.01f), Rgb(252, 218, 58), new(-8f, 0, 0), unlit: true);
                        foreach (int bs in new[] { -1, 1 })
                            P2Dia(head, new(0.18f, 0.14f, 0.10f), new(0.22f + bs * 0.09f, 0.31f, 0.16f), Rgb(252, 218, 58), new(0, -24f, bs * 22f), unlit: true);
                    }
                    else
                    {
                        // Ободок Сатори / готический венец Двойной Розы
                        P2Box(head, new(0.48f, 0.06f, 0.36f), new(0, 0.26f, 0.02f), Rgb(28, 24, 36));
                        P2Dia(head, new(0.14f, 0.14f, 0.10f), new(-0.22f, 0.24f, 0.12f), isDual ? Rgb(92, 242, 178) : Rgb(252, 218, 72), unlit: true);
                    }

                    var lArm = new Entity(parent: torso, position: new(-0.34f, 0.22f, 0));
                    var rArm = new Entity(parent: torso, position: new(0.34f, 0.22f, 0));
                    foreach (var arm in new[] { lArm, rArm })
                    {
                        P2Sph(arm, new(0.19f, 0.18f, 0.19f), new(0, -0.02f, 0), topCol);
                        P2Box(arm, new(0.14f, 0.30f, 0.14f), new(0, -0.20f, 0), topCol);
                        P2Sph(arm, new(0.11f, 0.11f, 0.11f), new(0, -0.38f, 0), skinTone);
                    }

                    // ТРЕТИЙ ГЛАЗ и извивающиеся сосуды/нити сердца перед грудью (parts.Halo)
                    var thirdEye = new Entity(parent: torso, position: new(0, 0.08f, 0.30f));
                    P2Sph(thirdEye, new(0.24f, 0.20f, 0.18f), Vector3.Zero, eyeMain, unlit: true);
                    if (isKoishi)
                        P2Box(thirdEye, new(0.22f, 0.04f, 0.10f), new(0, 0, 0.07f), Rgb(22, 14, 52));
                    else
                    {
                        P2Sph(thirdEye, new(0.11f, 0.11f, 0.08f), new(0, 0, 0.08f), Rgb(255, 196, 214), unlit: true);
                        P2Dia(thirdEye, new(0.05f, 0.10f, 0.06f), new(0, 0, 0.11f), Rgb(42, 8, 22));
                    }
                    if (isDual)
                    {
                        P2Sph(thirdEye, new(0.18f, 0.15f, 0.14f), new(0.24f, 0.12f, -0.04f), Rgb(86, 236, 174), unlit: true);
                        P2Box(thirdEye, new(0.16f, 0.03f, 0.08f), new(0.24f, 0.12f, 0.02f), Rgb(18, 48, 36));
                    }
                    // Скрученные артериальные / сердечные нити к плечам и запястьям
                    foreach (int s in new[] { -1, 1 })
                    {
                        P2Box(thirdEye, new(0.34f, 0.04f, 0.04f), new(s * 0.18f, 0.14f, -0.10f), cordA, new(0, s * 28f, s * 32f), unlit: true);
                        P2Box(thirdEye, new(0.38f, 0.04f, 0.04f), new(s * 0.22f, -0.16f, -0.08f), cordB, new(0, s * 22f, s * -36f), unlit: true);
                        P2Box(thirdEye, new(0.26f, 0.04f, 0.04f), new(s * 0.14f, -0.28f, 0.02f), cordA, new(0, 0, s * 42f), unlit: true);
                    }
                    parts.Halo = thirdEye;

                    // Распускающаяся аура Роз Подсознания / Спелл-карты (UltFormNodes)
                    var roseUlt = new Entity(parent: torso, position: new(0, -0.10f, 0), enabled: false);
                    for (int r = 0; r < 8; r++)
                    {
                        float a = r * MathF.PI * 0.25f;
                        Color rc = r % 2 == 0 ? Rgb(242, 42, 92) : Rgb(82, 238, 172);
                        P2Dia(roseUlt, new(0.28f, 0.24f, 0.12f), new(MathF.Cos(a) * 0.92f, 0.06f * (r % 2), MathF.Sin(a) * 0.92f), rc, new(24f, r * 45f, 0), unlit: true);
                    }
                    parts.UltFormNodes.Add(roseUlt);
                    parts.Head = head; parts.Torso = torso; parts.LeftArm = lArm; parts.RightArm = rArm;
                    return true;
                }
                case "false_son":
                {
                    Color stoneCol = skin.BodyColor;
                    Color darkSlate = skin.ArmorColor;
                    Color aurelGold = skin.VisorColor;
                    Color lunarCyan = Rgb(76, 232, 255);

                    var lHip = new Entity(parent: root, position: new(-0.24f, 1.02f, 0));
                    var rHip = new Entity(parent: root, position: new(0.24f, 1.02f, 0));
                    foreach (var (s, hip) in new[] { (-1, lHip), (1, rHip) })
                    {
                        P2Box(hip, new(0.26f, 0.48f, 0.26f), new(0, -0.24f, 0), stoneCol);
                        P2Dia(hip, new(0.28f, 0.20f, 0.30f), new(0, -0.50f, 0.04f), darkSlate);
                        P2Box(hip, new(0.24f, 0.44f, 0.26f), new(0, -0.76f, 0), darkSlate);
                        P2Box(hip, new(0.06f, 0.40f, 0.28f), new(s * 0.06f, -0.34f, 0.02f), aurelGold, new(0, 0, s * 10f), unlit: true);
                        P2Box(hip, new(0.26f, 0.12f, 0.36f), new(0, -0.96f, 0.05f), stoneCol);
                    }
                    parts.LeftLeg = lHip; parts.RightLeg = rHip;

                    var torso = new Entity(parent: root, position: new(0, 1.44f, 0));
                    P2Box(torso, new(0.68f, 0.62f, 0.42f), new(0, 0.12f, 0), stoneCol);
                    P2Box(torso, new(0.52f, 0.28f, 0.36f), new(0, -0.26f, 0), darkSlate);
                    // Светящиеся золотые прожилки Аурелионита в камне!
                    P2Dia(torso, new(0.22f, 0.32f, 0.14f), new(0, 0.16f, 0.18f), aurelGold, unlit: true);
                    foreach (int s in new[] { -1, 1 })
                        P2Box(torso, new(0.26f, 0.06f, 0.10f), new(s * 0.18f, 0.24f, 0.18f), aurelGold, new(0, 0, s * 28f), unlit: true);
                    // Храмовая набедренная повязка
                    var sash = new Entity(parent: torso, position: new(0, -0.34f, 0.16f));
                    P2Dia(sash, new(0.28f, 0.62f, 0.08f), new(0, -0.28f, 0), Rgb(36, 92, 138));
                    P2Box(sash, new(0.56f, 0.10f, 0.40f), new(0, 0.04f, -0.16f), aurelGold, unlit: true);
                    parts.Tails.Add(sash);

                    var head = new Entity(parent: torso, position: new(0, 0.58f, 0));
                    P2Box(head, new(0.42f, 0.46f, 0.42f), Vector3.Zero, stoneCol);
                    P2Dia(head, new(0.14f, 0.30f, 0.12f), new(0, 0.04f, 0.20f), aurelGold, unlit: true);
                    // Асимметричные рога: массивный каменно-золотой справа и лунно-голубой кристалл слева!
                    P2Dia(head, new(0.16f, 0.58f, 0.18f), new(0.28f, 0.28f, -0.02f), stoneCol, new(-12f, 0, -28f));
                    P2Dia(head, new(0.08f, 0.42f, 0.10f), new(0.30f, 0.32f, 0.02f), aurelGold, new(-12f, 0, -28f), unlit: true);
                    P2Dia(head, new(0.14f, 0.44f, 0.14f), new(-0.26f, 0.22f, 0.02f), lunarCyan, new(-8f, 0, 24f), unlit: true);

                    // Расколотый золотой Нимб Отца (parts.Halo)
                    var fHalo = new Entity(parent: head, position: new(0, 0.38f, -0.20f), rotation: new(22f, 0, 0));
                    for (int i = 0; i < 7; i++)
                    {
                        if (skinIdx != 2 && (i == 2 || i == 5)) continue;
                        float a = i * (MathF.Tau / 7f);
                        P2Dia(fHalo, new(0.24f, 0.09f, 0.09f), new(MathF.Cos(a) * 0.50f, MathF.Sin(a) * 0.50f, 0), aurelGold, new(0, 0, i * 51.4f + 90f), unlit: true);
                    }
                    parts.Halo = fHalo;

                    var lArm = new Entity(parent: torso, position: new(-0.48f, 0.28f, 0));
                    var rArm = new Entity(parent: torso, position: new(0.48f, 0.28f, 0));
                    foreach (var (s, arm) in new[] { (-1, lArm), (1, rArm) })
                    {
                        P2Dia(arm, new(0.32f, 0.28f, 0.32f), new(s * 0.04f, 0.06f, 0), darkSlate);
                        P2Box(arm, new(0.22f, 0.48f, 0.22f), new(0, -0.24f, 0), stoneCol);
                        P2Box(arm, new(0.06f, 0.36f, 0.24f), new(0, -0.24f, 0), s < 0 ? lunarCyan : aurelGold, unlit: true);
                        P2Box(arm, new(0.24f, 0.20f, 0.24f), new(0, -0.52f, 0), darkSlate);
                    }

                    // Светящиеся голубые Лунные Шипы на орбите вокруг левого плеча!
                    for (int sp = 0; sp < 4; sp++)
                    {
                        float sa = sp * 0.95f + 0.4f;
                        P2Dia(lArm, new(0.10f, 0.32f, 0.10f), new(-0.24f - MathF.Cos(sa) * 0.16f, 0.28f + sp * 0.09f, MathF.Sin(sa) * 0.22f),
                            lunarCyan, new(14f, sp * 30f, 26f), unlit: true);
                    }

                    // Массивная Палица Отвергнутого (Club of the Forsaken) в правой руке!
                    var club = new Entity(parent: rArm, position: new(0, -0.54f, 0.14f), rotation: new(72f, 0, 0));
                    P2Box(club, new(0.10f, 1.35f, 0.10f), new(0, 0.45f, 0), darkSlate);
                    P2Box(club, new(0.14f, 0.12f, 0.14f), new(0, 0.22f, 0), aurelGold, unlit: true);
                    P2Box(club, new(0.42f, 0.88f, 0.42f), new(0, 1.34f, 0), stoneCol);
                    P2Dia(club, new(0.50f, 0.44f, 0.50f), new(0, 1.52f, 0), darkSlate);
                    P2Dia(club, new(0.24f, 0.68f, 0.46f), new(0, 1.34f, 0), aurelGold, unlit: true);

                    // Грозовая вспышка Лазера Отца в UltFormNodes
                    var stormUlt = new Entity(parent: torso, position: Vector3.Zero, enabled: false);
                    P2Sph(stormUlt, new(0.44f, 0.44f, 0.30f), new(0, 0.16f, 0.22f), aurelGold, unlit: true);
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * (MathF.PI / 3f);
                        P2Dia(stormUlt, new(0.16f, 0.64f, 0.16f), new(MathF.Cos(a) * 0.96f, 0.10f, MathF.Sin(a) * 0.96f),
                            k % 2 == 0 ? aurelGold : lunarCyan, new(0, 0, 20f), unlit: true);
                    }
                    parts.UltFormNodes.Add(stormUlt);
                    parts.Head = head; parts.Torso = torso; parts.LeftArm = lArm; parts.RightArm = rArm;
                    return true;
                }
                default:
                    return false;
            }
        }

        private static void ApplyExpansionPack2Mastery(string charKey, Entity root, SurvivorParts parts, SkinDef skin)
        {
            switch (charKey)
            {
                case "daitaku_helios":
                    if (parts.Head != null)
                        for (int i = -1; i <= 1; i++)
                            P2Dia(parts.Head, new(0.10f, 0.22f - MathF.Abs(i) * 0.04f, 0.08f), new(i * 0.10f, 0.38f, 0.12f), Rgb(255, 224, 68), unlit: true);
                    if (parts.Torso != null)
                        foreach (int s in new[] { -1, 1 })
                            P2Sph(parts.Torso, new(0.46f, 0.46f, 0.06f), new(s * 0.46f, 0.20f, -0.24f), Rgb(0, 245, 255), new(0, s * -22f, 0), unlit: true);
                    break;

                case "robin":
                    if (parts.Head != null)
                        P2Dia(parts.Head, new(0.32f, 0.24f, 0.08f), new(0, 0.36f, 0.12f), Rgb(255, 226, 98), unlit: true);
                    if (parts.Torso != null)
                        foreach (int s in new[] { -1, 1 })
                            P2Dia(parts.Torso, new(0.56f, 0.16f, 0.08f), new(s * 0.48f, 0.30f, -0.20f), Rgb(168, 236, 255), new(0, s * -16f, s * 26f), unlit: true);
                    break;

                case "apocalypse_bird":
                    if (parts.Head != null)
                        for (int i = -2; i <= 2; i++)
                            P2Dia(parts.Head, new(0.12f, 0.38f - MathF.Abs(i) * 0.05f, 0.12f), new(i * 0.12f, 0.46f, 0.02f), Rgb(255, 246, 184), unlit: true);
                    break;

                case "false_son":
                    if (parts.Head != null)
                        P2Dia(parts.Head, new(0.62f, 0.36f, 0.12f), new(0, 0.38f, 0.06f), Rgb(255, 216, 84), unlit: true);
                    if (parts.Torso != null)
                    {
                        var cape = new Entity(parent: parts.Torso, position: new(0, 0.32f, -0.22f), rotation: new(12f, 0, 0));
                        P2Dia(cape, new(0.76f, 1.18f, 0.10f), new(0, -0.56f, -0.06f), Rgb(44, 188, 214));
                        parts.Tails.Add(cape);
                    }
                    break;
            }
        }
    }
}
