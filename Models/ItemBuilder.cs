using System.Numerics;
using Raylib_cs;
using RiskOfCSharp2.Core;
using RiskOfCSharp2.Data;
using static RiskOfCSharp2.Core.Utils;

namespace RiskOfCSharp2.Models
{
    public static class ItemBuilder
    {
        public static Entity BuildItemMesh(Entity parentEntity, string itemKey)
        {
            var root = new Entity(parent: parentEntity);
            Color itemCol = GameData.ItemPool.TryGetValue(itemKey, out var info)
                ? info.MeshColor
                : Color.Yellow;

            if (itemKey == "syringe")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 35));
                new Entity(parent: tilt, model: "cube", scale: new(0.28f, 0.75f, 0.28f), color: Rgb(220, 235, 250));
                new Entity(parent: tilt, model: "cube", scale: new(0.22f, 0.55f, 0.30f), color: Color.Yellow, unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.05f, 0.45f, 0.05f), position: new(0, 0.55f, 0), color: Color.LightGray);
                new Entity(parent: tilt, model: "cube", scale: new(0.42f, 0.08f, 0.42f), position: new(0, -0.42f, 0), color: Color.Gray);
            }
            else if (itemKey == "hoof")
            {
                new Entity(parent: root, model: "cube", scale: new(0.26f, 0.65f, 0.26f), position: new(0, 0.15f, 0), rotation: new(0, 0, -15), color: Rgb(235, 240, 250));
                new Entity(parent: root, model: "diamond", scale: new(0.44f, 0.35f, 0.55f), position: new(0.1f, -0.22f, 0.08f), color: Color.SkyBlue, unlit: true);
            }
            else if (itemKey is "bear" or "safer_spaces")
            {
                Color brown = itemKey == "safer_spaces" ? Rgb(185, 85, 245) : Rgb(215, 130, 55);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.52f, position: new(0, -0.12f, 0), color: brown);
                var head = new Entity(parent: root, model: "sphere", scaleUniform: 0.42f, position: new(0, 0.28f, 0), color: brown);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.38f, position: new(-0.38f, 0.38f, 0), color: brown);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.38f, position: new(0.38f, 0.38f, 0), color: brown);
                new Entity(parent: head, model: "sphere", scaleUniform: 0.28f, position: new(0, -0.05f, 0.42f), color: Rgb(245, 210, 170));
                if (itemKey == "safer_spaces")
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.88f, color: Rgb(200, 85, 255, 95), unlit: true);
            }
            else if (itemKey is "ukulele" or "polylute")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -30));
                Color bodyC = itemKey == "polylute" ? Rgb(155, 55, 225) : Rgb(210, 125, 45);
                new Entity(parent: tilt, model: "sphere", scale: new(0.48f, 0.55f, 0.18f), position: new(0, -0.18f, 0), color: bodyC);
                new Entity(parent: tilt, model: "sphere", scale: new(0.36f, 0.40f, 0.19f), position: new(0, 0.05f, 0), color: bodyC);
                new Entity(parent: tilt, model: "sphere", scale: new(0.18f, 0.18f, 0.22f), position: new(0, -0.05f, 0), color: Color.Magenta, unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.10f, 0.65f, 0.08f), position: new(0, 0.42f, 0), color: Rgb(110, 65, 25));
            }
            else if (itemKey == "gasoline")
            {
                new Entity(parent: root, model: "cube", scale: new(0.52f, 0.65f, 0.32f), position: new(0, -0.05f, 0), color: Color.Red);
                new Entity(parent: root, model: "cube", scale: new(0.32f, 0.14f, 0.16f), position: new(0, 0.34f, 0), color: Rgb(180, 30, 30));
                new Entity(parent: root, model: "cube", scale: new(0.10f, 0.28f, 0.10f), position: new(0.18f, 0.38f, 0), rotation: new(0, 0, -35), color: Color.Yellow, unlit: true);
            }
            else if (itemKey == "lens")
            {
                new Entity(parent: root, model: "cube", scale: new(0.75f, 0.08f, 0.10f), position: new(0, 0.10f, 0), color: Rgb(40, 45, 55));
                new Entity(parent: root, model: "sphere", scale: new(0.32f, 0.28f, 0.14f), position: new(-0.20f, 0.04f, 0.04f), color: Rgb(255, 35, 75), unlit: true);
                new Entity(parent: root, model: "sphere", scale: new(0.32f, 0.28f, 0.14f), position: new(0.20f, 0.04f, 0.04f), color: Rgb(255, 35, 75), unlit: true);
            }
            else if (itemKey == "feather")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 25));
                new Entity(parent: tilt, model: "cube", scale: new(0.06f, 0.85f, 0.06f), color: Color.Gold, unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.38f, 0.68f, 0.12f), position: new(0, 0.12f, 0), color: Rgb(90, 255, 150), unlit: true);
            }
            else if (itemKey == "bungus")
            {
                new Entity(parent: root, model: "cube", scale: new(0.24f, 0.48f, 0.24f), position: new(0, -0.15f, 0), color: Rgb(230, 240, 225));
                new Entity(parent: root, model: "sphere", scale: new(0.75f, 0.42f, 0.75f), position: new(0, 0.18f, 0), color: Rgb(85, 245, 120), unlit: true);
                foreach (var (sx, sz) in new[] { (-0.18f, 0.15f), (0.18f, -0.15f), (0.12f, 0.18f) })
                    new Entity(parent: root, model: "sphere", scaleUniform: 0.14f, position: new(sx, 0.32f, sz), color: Color.SkyBlue, unlit: true);
            }
            else if (itemKey == "tarrasque")
            {
                new Entity(parent: root, model: "diamond", scale: new(0.65f, 0.78f, 0.55f), color: Rgb(235, 35, 60), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.45f, position: new(-0.14f, 0.18f, 0), color: Rgb(255, 85, 95), unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.45f, position: new(0.14f, 0.18f, 0), color: Rgb(255, 85, 95), unlit: true);
            }
            else if (itemKey == "tesla_coil")
            {
                new Entity(parent: root, model: "cube", scale: new(0.45f, 0.22f, 0.45f), position: new(0, -0.25f, 0), color: Rgb(55, 65, 82));
                new Entity(parent: root, model: "cube", scale: new(0.18f, 0.55f, 0.18f), position: new(0, 0.05f, 0), color: Rgb(180, 120, 60));
                new Entity(parent: root, model: "sphere", scale: new(0.48f, 0.28f, 0.48f), position: new(0, 0.38f, 0), color: Color.SkyBlue, unlit: true);
            }
            else if (itemKey == "aegis")
            {
                new Entity(parent: root, model: "diamond", scale: new(0.68f, 0.78f, 0.22f), color: Color.Gold, unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.42f, 0.52f, 0.26f), color: Color.White, unlit: true);
            }
            else if (itemKey is "shattering_justice" or "guillotine")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 25));
                new Entity(parent: tilt, model: "cube", scale: new(0.12f, 0.85f, 0.12f), color: Rgb(80, 85, 95));
                new Entity(parent: tilt, model: "cube", scale: new(0.55f, 0.28f, 0.28f), position: new(0, 0.35f, 0), color: itemCol, unlit: true);
            }
            else if (itemKey == "crowbar")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 30));
                new Entity(parent: tilt, model: "cube", scale: new(0.12f, 0.85f, 0.12f), color: Rgb(210, 45, 45));
                new Entity(parent: tilt, model: "cube", scale: new(0.32f, 0.12f, 0.12f), position: new(0.1f, 0.42f, 0), color: Rgb(200, 210, 225));
            }
            else if (itemKey is "tri_tip" or "harvester_scythe" or "ceremonial_dagger")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, -35));
                new Entity(parent: tilt, model: "diamond", scale: new(0.28f, 0.85f, 0.14f), position: new(0, 0.12f, 0), color: itemCol, unlit: true);
                new Entity(parent: tilt, model: "cube", scale: new(0.12f, 0.35f, 0.12f), position: new(0, -0.35f, 0), color: Rgb(60, 65, 75));
            }
            else if (itemKey is "ap_rounds" or "backup_mag" or "bandolier" or "afterburner" or "lysate_cell")
            {
                new Entity(parent: root, model: "cube", scale: new(0.48f, 0.58f, 0.28f), color: Rgb(65, 75, 90));
                foreach (float bx in new[] { -0.14f, 0.0f, 0.14f })
                    new Entity(parent: root, model: "diamond", scale: new(0.12f, 0.45f, 0.12f), position: new(bx, 0.32f, 0), color: itemCol, unlit: true);
            }
            else if (itemKey is "energy_drink" or "will_o_wisp" or "infusion" or "plasma_shrimp")
            {
                new Entity(parent: root, model: "cube", scale: new(0.38f, 0.68f, 0.38f), color: itemCol, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.42f, 0.12f, 0.42f), position: new(0, 0.35f, 0), color: Color.White);
            }
            else if (itemKey is "bison_steak" or "monster_tooth" or "leeching_seed" or "alien_head")
            {
                new Entity(parent: root, model: "sphere", scale: new(0.62f, 0.52f, 0.48f), color: itemCol);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.22f, position: new(-0.12f, 0.05f, 0.18f), color: Color.Black, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.22f, position: new(0.12f, 0.05f, 0.18f), color: Color.Black, unlit: true);
            }
            else if (itemKey is "medkit" or "repulsion_armor" or "rose_buckler")
            {
                new Entity(parent: root, model: "cube", scale: new(0.64f, 0.52f, 0.24f), color: Rgb(235, 240, 250));
                new Entity(parent: root, model: "cube", scale: new(0.42f, 0.14f, 0.28f), color: itemCol, unlit: true);
                new Entity(parent: root, model: "cube", scale: new(0.14f, 0.42f, 0.28f), color: itemCol, unlit: true);
            }
            else if (itemKey is "topaz_brooch" or "opal" or "focus_crystal" or "chronobauble" or "frost_relic" or "razorwire")
            {
                new Entity(parent: root, model: "diamond", scale: new(0.58f, 0.78f, 0.58f), color: itemCol, unlit: true);
                new Entity(parent: root, model: "diamond", scale: new(0.82f, 0.16f, 0.82f), color: Color.Gold, unlit: true);
            }
            else if (itemKey is "kjaro_band" or "runald_band")
            {
                new Entity(parent: root, model: "diamond", scale: new(0.72f, 0.22f, 0.72f), color: itemCol, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.32f, position: new(0, 0.22f, 0), color: Color.White, unlit: true);
            }
            else if (itemKey is "atg_missile" or "bundle_fireworks" or "behemoth" or "sticky_bomb" or "stun_grenade")
            {
                var tilt = new Entity(parent: root, rotation: new(0, 0, 25));
                new Entity(parent: tilt, model: "cube", scale: new(0.32f, 0.72f, 0.32f), color: itemCol, unlit: true);
                new Entity(parent: tilt, model: "diamond", scale: new(0.42f, 0.42f, 0.42f), position: new(0, 0.45f, 0), color: Color.Yellow, unlit: true);
            }
            else
            {
                new Entity(parent: root, model: "diamond", scale: new(0.55f, 0.72f, 0.55f), color: itemCol, unlit: true);
                new Entity(parent: root, model: "sphere", scaleUniform: 0.38f, color: Color.White, unlit: true);
            }

            return root;
        }
    }
}