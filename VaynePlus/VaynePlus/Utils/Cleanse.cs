using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace VaynePlus.Utils
{
    class Cleanse
    {
        #region
        private static readonly BuffType[] Buffs = { BuffType.Blind, BuffType.Charm, BuffType.CombatDehancer, BuffType.Fear, BuffType.Flee, BuffType.Knockback, BuffType.Knockup, BuffType.Polymorph, BuffType.Silence, BuffType.Sleep, BuffType.Snare, BuffType.Stun, BuffType.Suppression, BuffType.Taunt };

        public static double HealthBuffer
        {
            get { return CleanseMenu["vayneplus.cleanse.hpbuffer"].Cast<Slider>().CurrentValue; }
        }
        public static float Delay
        {
            get { return CleanseMenu["vayneplus.cleanse.delay"].Cast<Slider>().CurrentValue; }
        }

        private static readonly List<QssSpell> QssSpells = new List<QssSpell>
        {
            new QssSpell
            {
                ChampName = "Warwick",
                IsEnabled = true,
                SpellBuff = "InfiniteDuress",
                SpellName = "Warwick R",
                RealName = "warwickR",
                OnlyKill = false,
                Slot = SpellSlot.R,
                Delay = 100f
            },
            new QssSpell
            {
                ChampName = "Zed",
                IsEnabled = true,
                SpellBuff = "zedulttargetmark",
                SpellName = "Zed R",
                RealName = "zedultimate",
                OnlyKill = true,
                Slot = SpellSlot.R,
                Delay = 800f
            },
            new QssSpell
            {
                ChampName = "Rammus",
                IsEnabled = true,
                SpellBuff = "PuncturingTaunt",
                SpellName = "Rammus E",
                RealName = "rammusE",
                OnlyKill = false,
                Slot = SpellSlot.E,
                Delay = 100f                
            },
            /** Danger Level 4 Spells*/
            new QssSpell
            {
                ChampName = "Skarner",
                IsEnabled = true,
                SpellBuff = "SkarnerImpale",
                SpellName = "Skaner R",
                RealName = "skarnerR",
                OnlyKill = false,
                Slot = SpellSlot.R,
                Delay = 100f
            },
            new QssSpell
            {
                ChampName = "Fizz",
                IsEnabled = true,
                SpellBuff = "FizzMarinerDoom",
                SpellName = "Fizz R",
                RealName = "FizzR",
                OnlyKill = false,
                Slot = SpellSlot.R,
                Delay = 100f
            },
            new QssSpell
            {
                ChampName = "Galio",
                IsEnabled = true,
                SpellBuff = "GalioIdolOfDurand",
                SpellName = "Galio R",
                RealName = "GalioR",
                OnlyKill = false,
                Slot = SpellSlot.R,
                Delay = 100f
            },
            new QssSpell
            {
                ChampName = "Malzahar",
                IsEnabled = true,
                SpellBuff = "AlZaharNetherGrasp",
                SpellName = "Malz R",
                RealName = "MalzaharR",
                OnlyKill = false,
                Slot = SpellSlot.R,
                Delay = 200f
            },
            /** Danger Level 3 Spells*/
            new QssSpell
            {
                ChampName = "Zilean",
                IsEnabled = false,
                SpellBuff = "timebombenemybuff",
                SpellName = "Zilean Q",
                OnlyKill = true,
                Slot = SpellSlot.Q,
                Delay = 700f
            },
            new QssSpell
            {
                ChampName = "Vladimir",
                IsEnabled = false,
                SpellBuff = "VladimirHemoplague",
                SpellName = "Vlad R",
                RealName = "VladimirR",
                OnlyKill = true,
                Slot = SpellSlot.R,
                Delay = 700f
            },
            new QssSpell
            {
                ChampName = "Mordekaiser",
                IsEnabled = true,
                SpellBuff = "MordekaiserChildrenOfTheGrave",
                SpellName = "Morde R",
                OnlyKill = true,
                 Slot = SpellSlot.R,
                Delay = 800f
            },
            /** Danger Level 2 Spells*/
            new QssSpell
            {
                ChampName = "Poppy",
                IsEnabled = true,
                SpellBuff = "PoppyDiplomaticImmunity",
                SpellName = "Poppy R",
                RealName = "PoppyR",
                OnlyKill = false,
                 Slot = SpellSlot.R,
                Delay = 100f
            }
        };
        #endregion

        private static Menu CleanseMenu;

        public static void OnLoad(Menu menu)
        {
            var cName = ObjectManager.Player.ChampionName;

            CleanseMenu = menu.AddSubMenu("Cleanse", "vayneplus.cleanse");
            CleanseMenu.AddGroupLabel("Cleanse - Spell Cleanser");
            foreach (var spell in QssSpells.Where(h => GetChampByName(h.ChampName) != null))
            {
                CleanseMenu.AddGroupLabel(spell.SpellName);
                CleanseMenu.Add("vayneplus.cleanse.spell." + spell.SpellBuff + "A",
                    new CheckBox("Always", !spell.OnlyKill));
                CleanseMenu.Add("vayneplus.cleanse.spell." + spell.SpellBuff + "K",
                    new CheckBox("Only if killed by it", !spell.OnlyKill));
                CleanseMenu.Add("vayneplus.cleanse.spell." + spell.SpellBuff + "D",
                    new Slider("Delay before cleanse", (int)spell.Delay, 0, 10000));
            }
            //Bufftype cleanser menu
            CleanseMenu.AddSeparator();
            CleanseMenu.AddGroupLabel("Cleanse - Bufftype Cleanser");
            foreach (var buffType in Buffs)
            {
                CleanseMenu.Add("vayneplus.cleanse.bufftype." + cName + buffType, new CheckBox(buffType.ToString()));
            }

            CleanseMenu.Add("vayneplus.cleanse.bufftype.minbuffs", new Slider("Min Buffs", 2, 1, 5));
            CleanseMenu.Add("vayneplus.cleanse.hpbuffer", new Slider("Health Buffer", 20));
            CleanseMenu.Add("vayneplus.cleanse.delay", new Slider("Health Buffer", 100, 0, 200));
            CleanseMenu.AddSeparator();
            CleanseMenu.AddGroupLabel("Cleanse - Items");
            CleanseMenu.Add("vayneplus.cleanse.items.qss", new CheckBox("Use QSS"));
            CleanseMenu.Add("vayneplus.cleanse.items.scimitar", new CheckBox("Use Mercurial Scimitar"));
            CleanseMenu.Add("vayneplus.cleanse.items.dervish", new CheckBox("Use Dervish Blade"));
            CleanseMenu.Add("vayneplus.cleanse.items.michael", new CheckBox("Use Mikael's Crucible"));

            //Subscribe the Events
            Game.OnTick += GameOnOnTick;
        }

        private static void GameOnOnTick(EventArgs args)
        {
            KillCleansing();
            SpellCleansing();
            BuffTypeCleansing();
        }

        #region BuffType Cleansing

        static void BuffTypeCleansing()
        {
            if (OneReady())
            {
                var buffCount = Buffs.Count(buff => ObjectManager.Player.HasBuffOfType(buff) && BuffTypeEnabled(buff));
                if (buffCount >= CleanseMenu["vayneplus.cleanse.bufftype.minbuffs"].Cast<Slider>().CurrentValue)
                {
                    CastCleanseItem(ObjectManager.Player);
                }
            }
        }
        #endregion

        #region Spell Cleansing

        static void SpellCleansing()
        {
            if (OneReady())
            {
                QssSpell mySpell = null;
                if (
                    QssSpells.Where(
                        spell => ObjectManager.Player.HasBuff(spell.SpellBuff) && SpellEnabledAlways(spell.SpellBuff))
                        .OrderBy(
                            spell => GetChampByName(spell.ChampName).GetSpellDamage(ObjectManager.Player, spell.Slot))
                        .Any())
                {
                    mySpell =
                        QssSpells.Where(
                            spell => ObjectManager.Player.HasBuff(spell.SpellBuff) && SpellEnabledAlways(spell.SpellBuff))
                            .OrderBy(
                                spell => GetChampByName(spell.ChampName).GetSpellDamage(ObjectManager.Player, spell.Slot))
                            .First();
                }

                if (mySpell != null)
                {
                    UseCleanser(mySpell, ObjectManager.Player);
                }
            }
        }
        #endregion

        #region Spell Will Kill Cleansing

        static void KillCleansing()
        {
            if (OneReady())
            {
                QssSpell mySpell = null;
                if (
                    QssSpells.Where(
                        spell => ObjectManager.Player.HasBuff(spell.SpellBuff) && SpellEnabledOnKill(spell.SpellBuff) && GetChampByName(spell.ChampName).GetSpellDamage(ObjectManager.Player, spell.Slot) > ObjectManager.Player.Health + HealthBuffer)
                        .OrderBy(
                            spell => GetChampByName(spell.ChampName).GetSpellDamage(ObjectManager.Player, spell.Slot))
                        .Any())
                {
                    mySpell =
                        QssSpells.Where(
                            spell => ObjectManager.Player.HasBuff(spell.SpellBuff) && SpellEnabledOnKill(spell.SpellBuff))
                            .OrderBy(
                                spell => GetChampByName(spell.ChampName).GetSpellDamage(ObjectManager.Player, spell.Slot))
                            .First();
                }
                if (mySpell != null)
                {
                    UseCleanser(mySpell, ObjectManager.Player);
                }
            }
        }


        #endregion

        #region Cleansing
        static void UseCleanser(QssSpell spell, AIHeroClient target)
        {
            Core.DelayAction(() => CastCleanseItem(target), SpellDelay(spell.RealName));
        }

        static void CastCleanseItem(AIHeroClient target)
        {
            if (target == null)
            {
                return;
            }

            if (CleanseMenu["vayneplus.cleanse.items.michael"].Cast<CheckBox>().CurrentValue && Item.HasItem(3222) &&
                   Item.CanUseItem(3222) && target.IsValidTarget(600f))
            {
                Item.UseItem(3222, target);
                return;
            }

            if (CleanseMenu["vayneplus.cleanse.items.qss"].Cast<CheckBox>().CurrentValue && Item.HasItem(3140) &&
                Item.CanUseItem(3140) && target.IsMe)
            {
                Item.UseItem(3140, ObjectManager.Player);
                return;
            }

            if (CleanseMenu["vayneplus.cleanse.items.scimitar"].Cast<CheckBox>().CurrentValue && Item.HasItem(3139) &&
                Item.CanUseItem(3139) && target.IsMe)
            {
                Item.UseItem(3139, ObjectManager.Player);
                return;
            }

            if (CleanseMenu["vayneplus.cleanse.items.dervish"].Cast<CheckBox>().CurrentValue && Item.HasItem(3137) &&
                Item.CanUseItem(3137) && target.IsMe)
            {
                Item.UseItem(3137, ObjectManager.Player);
            }
        }
        #endregion

        #region Utility Methods

        private static bool OneReady()
        {
            return (CleanseMenu["vayneplus.cleanse.items.qss"].Cast<CheckBox>().CurrentValue && Item.HasItem(3140) &&
                    Item.CanUseItem(3140)) ||
                   (CleanseMenu["vayneplus.cleanse.items.scimitar"].Cast<CheckBox>().CurrentValue && Item.HasItem(3139) &&
                    Item.CanUseItem(3139)) ||
                   (CleanseMenu["vayneplus.cleanse.items.dervish"].Cast<CheckBox>().CurrentValue && Item.HasItem(3137) &&
                    Item.CanUseItem(3137));
        }
        private static bool BuffTypeEnabled(BuffType buffType)
        {
            return CleanseMenu["vayneplus.cleanse.bufftype." + Player.Instance.ChampionName + buffType].Cast<CheckBox>().CurrentValue;
        }
        private static int SpellDelay(String sName)
        {
            return CleanseMenu["vayneplus.cleanse.spell." + sName + "D"].Cast<Slider>().CurrentValue;
        }
        private static bool SpellEnabledOnKill(String sName)
        {
            return CleanseMenu["vayneplus.cleanse.spell." + sName + "K"].Cast<CheckBox>().CurrentValue;
        }
        private static bool SpellEnabledAlways(String sName)
        {
            return CleanseMenu["vayneplus.cleanse.spell." + sName + "A"].Cast<CheckBox>().CurrentValue;
        }

        private static AIHeroClient GetChampByName(String enemyName)
        {
            return HeroManager.Enemies.Find(h => h.ChampionName == enemyName);
        }
        #endregion
    }

    internal class QssSpell
    {
        public String ChampName { get; set; }
        public String SpellName { get; set; }
        public String RealName { get; set; }
        public String SpellBuff { get; set; }
        public bool IsEnabled { get; set; }
        public bool OnlyKill { get; set; }
        public SpellSlot Slot { get; set; }
        public float Delay { get; set; }
    }
}
