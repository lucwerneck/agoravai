using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace VaynePlus.Utils
{
    internal class PotionManager
    {
        private static readonly List<Potion> Potions = new List<Potion>
        {
            new Potion
            {
                Name = "Health Potion",
                BuffName = "RegenerationPotion",
                ItemId = (ItemId)2003,
                Type =  PotionType.Health,
                Priority = 2
            },
            new Potion
            {
                Name = "Mana Potion",
                BuffName = "FlaskOfCrystalWater",
                ItemId = (ItemId)2004,
                Type =  PotionType.Mana,
                Priority = 2
            },
            new Potion
            {
                Name = "Crystal Flask",
                BuffName = "ItemCrystalFlask",
                ItemId = (ItemId)2041,
                Type =  PotionType.Flask,
                Priority = 3
            },
            new Potion
            {
                Name = "Biscuit",
                BuffName = "ItemMiniRegenPotion",
                ItemId = (ItemId)2010,
                Type =  PotionType.Flask,
                Priority = 1
            },
        };

        public static void OnLoad(Menu menu)
        {
            AddMenu(menu);
            Game.OnTick += Game_OnTick;
        }



        private static void Game_OnTick(EventArgs args)
        {
            UsePotion();
        }

        private static void UsePotion()
        {

            if (Player.Instance.IsDead || Player.Instance.IsRecalling() || Helpers.IsFountain(Player.Instance.ServerPosition))
                return;

            if (!HealthBuff() && ObjectManager.Player.HealthPercent < PotMenu["vayneplus.potionmanager.minhp"].Cast<Slider>().CurrentValue)
            {
                var hpSlot = GetHpSlot();
                if (hpSlot != SpellSlot.Unknown && Player.GetSpell(hpSlot).IsReady)
                {
                    ObjectManager.Player.Spellbook.CastSpell(hpSlot, ObjectManager.Player);
                }
            }

            if (!ManaBuff() && ObjectManager.Player.ManaPercent < PotMenu["vayneplus.potionmanager.minmana"].Cast<Slider>().CurrentValue)
            {
                var manaSlot = GetManaSlot();
                if (manaSlot != SpellSlot.Unknown && Player.GetSpell(manaSlot).IsReady)
                {
                    ObjectManager.Player.Spellbook.CastSpell(manaSlot, ObjectManager.Player);
                }
            }
        }

        private static Menu PotMenu;

        private static void AddMenu(Menu menu)
        {
            PotMenu = menu.AddSubMenu("Potion Manager", "vayneplus.potionmanager");
            PotMenu.AddGroupLabel("Potions");
            foreach (var potion in Potions)
            {
                PotMenu.Add("vayneplus.potionmanager." + potion.ItemId, new CheckBox(potion.Name));
            }
            PotMenu.AddSeparator();
            PotMenu.AddGroupLabel("Potion Options");
            PotMenu.Add("vayneplus.potionmanager.minhp", new Slider("Min Health %", 30));
            PotMenu.Add("vayneplus.potionmanager.minmana", new Slider("Min Mana %", 35));
        }

        private static bool ManaBuff()
        {
            return Potions.Any(pot => (pot.Type == PotionType.Mana || pot.Type == PotionType.Flask) && pot.IsRunning);
        }

        private static bool HealthBuff()
        {
            return Potions.Any(pot => (pot.Type == PotionType.Health || pot.Type == PotionType.Flask) && pot.IsRunning);
        }

        private static SpellSlot GetHpSlot()
        {
            var ordered = Potions.Where(p => p.Type == PotionType.Health || p.Type == PotionType.Flask).OrderByDescending(pot => pot.Priority);
            var potSlot = SpellSlot.Unknown;
            var lastPriority = ordered.First().Priority;

            foreach (
                var Item in
                    ObjectManager.Player.InventoryItems.Where(
                        item =>
                            GetHpIds().Contains((int)item.Id) &&
                            PotMenu["vayneplus.potionmanager." + item.Id].Cast<CheckBox>().CurrentValue))
            {
                var currentPriority = Potions.First(it => it.ItemId == Item.Id && Item.Stacks > 0).Priority;
                if (currentPriority <= lastPriority)
                {
                    potSlot = Item.SpellSlot;
                }
            }
            return potSlot;
        }


        private static SpellSlot GetManaSlot()
        {
            var ordered = Potions.Where(p => p.Type == PotionType.Mana || p.Type == PotionType.Flask).OrderByDescending(pot => pot.Priority);
            var potSlot = SpellSlot.Unknown;
            var lastPriority = ordered.First().Priority;

            foreach (
                var Item in
                    ObjectManager.Player.InventoryItems.Where(
                        item =>
                            GetManaIds().Contains((int)item.Id) && PotMenu["vayneplus.potionmanager."+item.Id].Cast<CheckBox>().CurrentValue))
            {
                var currentPriority = Potions.First(it => it.ItemId == Item.Id && Item.Stacks > 0).Priority;
                if (currentPriority <= lastPriority)
                {
                    potSlot = Item.SpellSlot;
                }
            }
            return potSlot;
        }

        private static List<int> GetHpIds()
        {
            var HPIds = new List<int>();
            foreach (var pot in Potions)
            {
                if (pot.Type == PotionType.Health || pot.Type == PotionType.Flask && Item.HasItem((int)pot.ItemId))
                {
                    HPIds.Add((int)pot.ItemId);
                }
            }
            return HPIds;
        }

        private static List<int> GetManaIds()
        {
            var ManaIds = new List<int>();
            foreach (var pot in Potions)
            {
                if (pot.Type == PotionType.Mana || pot.Type == PotionType.Flask && Item.HasItem((int)pot.ItemId))
                {
                    ManaIds.Add((int)pot.ItemId);
                }
            }
            return ManaIds;
        }
    }


    class Potion
    {
        public String Name { get; set; }
        public PotionType Type { get; set; }
        public String BuffName { get; set; }
        public ItemId ItemId { get; set; }
        public int Priority { get; set; }
        public bool IsRunning
        {
            get { return ObjectManager.Player.HasBuff(BuffName); }
        }
    }

    enum PotionType
    {
        Health,
        Mana,
        Flask
    }
}