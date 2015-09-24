using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace VaynePlus.Utils
{
    class ItemManager
    {
        private static readonly List<DzItem> ItemList = new List<DzItem>
        {
            new DzItem
            {
                Id=3144,
                Name = "Bilgewater Cutlass",
                Range = 600f,
                Class = ItemClass.Offensive,
                Mode = ItemMode.Targeted
            },
            new DzItem
            {
                Id= 3153,
                Name = "Blade of the Ruined King",
                Range = 600f,
                Class = ItemClass.Offensive,
                Mode = ItemMode.Targeted
            },
            new DzItem
            {
                Id= 3142,
                Name = "Youmuu",
                Range = float.MaxValue,
                Class = ItemClass.Offensive,
                Mode = ItemMode.NoTarget
            }
        };

        private static Menu ItemMenu;

        public static void OnLoad(Menu menu)
        {
            ItemMenu = menu.AddSubMenu("Item Manager", "vayneplus.itemmanager");
            ItemMenu.AddGroupLabel("Offensive Items");
            ItemMenu.Add("vayneplus.itemmanager.enabledalways", new CheckBox("Enabled Always?"));
            ItemMenu.Add("vayneplus.itemmanager.enabledcombo", new KeyBind("Enabled On Press?", false, KeyBind.BindTypes.HoldActive, 32));
            ItemMenu.AddSeparator();
            var offensiveItems = ItemList.FindAll(item => item.Class == ItemClass.Offensive);
            foreach (var item in offensiveItems)
            {
                ItemMenu.AddGroupLabel(item.Name);
                ItemMenu.Add("vayneplus.itemmanager." + item.Id + ".always", new CheckBox("Always"));
                ItemMenu.Add("vayneplus.itemmanager." + item.Id + ".onmyhp", new Slider("On my HP < then %", 30));
                ItemMenu.Add("vayneplus.itemmanager." + item.Id + ".ontghpgreater", new Slider("On Target HP > then %", 40));
                ItemMenu.Add("vayneplus.itemmanager." + item.Id + ".ontghplesser", new Slider("On Target HP < then %", 40));
                ItemMenu.Add("vayneplus.itemmanager." + item.Id + ".ontgkill", new CheckBox("On Target Killable"));
                ItemMenu.Add("vayneplus.itemmanager." + item.Id + ".displaydmg", new CheckBox("Display Damage"));
                ItemMenu.AddSeparator();
            }
            Game.OnTick += Game_OnTick;
        }

        static void Game_OnTick(EventArgs args)
        {
            if (!ItemMenu["vayneplus.itemmanager.enabledalways"].Cast<CheckBox>().CurrentValue &&
                !ItemMenu["vayneplus.itemmanager.enabledcombo"].Cast<KeyBind>().CurrentValue)
            {
                return;
            }

            UseOffensive();
        }

        static void UseOffensive()
        {
            var offensiveItems = ItemList.FindAll(item => item.Class == ItemClass.Offensive);
            foreach (var item in offensiveItems)
            {
                var selectedTarget = Hud.SelectedTarget as Obj_AI_Base ?? TargetSelector.GetTarget(item.Range, DamageType.True);
                if (!selectedTarget.IsValidTarget(item.Range) && item.Mode != ItemMode.NoTarget)
                {
                    return;
                }
                if (ItemMenu["vayneplus.itemmanager." + item.Id + ".always"].Cast<CheckBox>().CurrentValue)
                {
                    UseItem(selectedTarget, item);
                }
                if (Player.Instance.HealthPercent < ItemMenu["vayneplus.itemmanager." + item.Id + ".onmyhp"].Cast<Slider>().CurrentValue)
                {
                    UseItem(selectedTarget, item);
                }
                if (selectedTarget.HealthPercent < ItemMenu["vayneplus.itemmanager." + item.Id + ".ontghplesser"].Cast<Slider>().CurrentValue && !ItemMenu["vayneplus.itemmanager." + item.Id + ".ontgkill"].Cast<CheckBox>().CurrentValue)
                {
                    UseItem(selectedTarget, item);
                }
                if (selectedTarget.HealthPercent > ItemMenu["vayneplus.itemmanager." + item.Id + ".ontghpgreater"].Cast<Slider>().CurrentValue)
                {
                    UseItem(selectedTarget, item);
                }
                if (selectedTarget.Health < ObjectManager.Player.GetSpellDamage(selectedTarget, GetItemSpellSlot(item)) && ItemMenu["vayneplus.itemmanager." + item.Id + ".ontgkill"].Cast<CheckBox>().CurrentValue)
                {
                    UseItem(selectedTarget, item);
                }
            }
        }


        static void UseItem(Obj_AI_Base target, DzItem item)
        {
            if (!Item.HasItem(item.Id) || !Item.CanUseItem(item.Id))
            {
                return;
            }
            switch (item.Mode)
            {
                case ItemMode.Targeted:
                    Item.UseItem(item.Id, target);
                    break;
                case ItemMode.NoTarget:
                    Item.UseItem(item.Id, ObjectManager.Player);
                    break;
            }
        }

        static SpellSlot GetItemSpellSlot(DzItem item)
        {
            foreach (var it in ObjectManager.Player.InventoryItems.Where(it => (int)it.Id == item.Id))
            {
                return it.SpellSlot != SpellSlot.Unknown ? it.SpellSlot : SpellSlot.Unknown;
            }
            return SpellSlot.Unknown;
        }

        internal static float GetItemsDamage(AIHeroClient target)
        {
            var items = ItemList.Where(item => Item.HasItem(item.Id) && Item.CanUseItem(item.Id) && ItemMenu["vayneplus.itemmanager." + item.Id + ".displaydmg"].Cast<CheckBox>().CurrentValue);
            return items.Sum(item => (float)ObjectManager.Player.GetSpellDamage(target, GetItemSpellSlot(item)));
        }
    }

    internal class DzItem
    {
        public String Name { get; set; }
        public int Id { get; set; }
        public float Range { get; set; }
        public ItemClass Class { get; set; }
        public ItemMode Mode { get; set; }
    }

    enum ItemMode
    {
        Targeted, Skillshot, NoTarget
    }

    enum ItemClass
    {
        Offensive, Defensive
    }
}
