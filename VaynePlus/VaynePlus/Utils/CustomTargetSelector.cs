using System;
using System.Drawing;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using System.Collections.Generic;
using System.Linq;
using EloBuddy.SDK.Rendering;

namespace VaynePlus.Utils
{

    internal class CustomTargetSelector
    {
        #region

        public static AIHeroClient selectedHero { get; set; }
        public static AIHeroClient scriptSelectedHero { get; set; }
        private static Circle SelectedTCircle;

        private static readonly List<PriorityClass> priorityList = new List<PriorityClass>()
        {
            new PriorityClass()
            {
                Name = "Highest",
                Champions = new []
                {
                    "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia", "Corki", "Draven",
                    "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus", "Katarina", "Kennen", "KogMaw", "Leblanc",
                    "Lucian", "Lux", "Malzahar", "MasterYi", "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra", "Talon",
                    "Teemo", "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "VelKoz", "Viktor", "Xerath",
                    "Zed", "Ziggs"
                },
                Priority = Priority.Highest
            },

            new PriorityClass()
            {
                Name = "High",
                Champions = new []
                {
                    "Akali", "Diana", "Ekko", "Fiddlesticks", "Fiora", "Fizz", "Heimerdinger", "Jayce", "Kassadin",
                    "Kayle", "Kha'Zix", "Lissandra", "Mordekaiser", "Nidalee", "Riven", "Shaco", "Vladimir", "Yasuo",
                    "Zilean"
                },
                Priority = Priority.Highest
            },

            new PriorityClass()
            {
                Name = "Medium",
                Champions = new []
                {
                    "Aatrox", "Darius", "Elise", "Evelynn", "Galio", "Gangplank", "Gragas", "Irelia", "Jax",
                    "Lee Sin", "Maokai", "Morgana", "Nocturne", "Pantheon", "Poppy", "Rengar", "Rumble", "Ryze", "Swain",
                    "Trundle", "Tryndamere", "Udyr", "Urgot", "Vi", "XinZhao", "RekSai"
                },
                Priority = Priority.Medium
            },

            new PriorityClass()
            {
                Name = "Low",
                Champions = new []
                {
                    "Alistar", "Amumu", "Bard", "Blitzcrank", "Braum", "Cho'Gath", "Dr. Mundo", "Garen", "Gnar",
                    "Hecarim", "Janna", "Jarvan IV", "Leona", "Lulu", "Malphite", "Nami", "Nasus", "Nautilus", "Nunu",
                    "Olaf", "Rammus", "Renekton", "Sejuani", "Shen", "Shyvana", "Singed", "Sion", "Skarner", "Sona",
                    "Soraka", "Taric", "Thresh", "Volibear", "Warwick", "MonkeyKing", "Yorick", "Zac", "Zyra"
                },
                Priority = Priority.Low
            }
        };
        #endregion

        private static Menu ctsMenu;

        internal static void OnLoad(Menu mainMenu)
        {
            ctsMenu = mainMenu.AddSubMenu("Custom TargetSelector", "vayneplus.cts");
            ctsMenu.AddGroupLabel("TargetSelector");
            {
                var selector = ctsMenu.Add("vayneplus.cts.selector", new Slider("Custom", 0, 0, 1));
                selector.OnValueChange += (sender, args) =>
                {
                    switch (args.NewValue)
                    {
                        case 0:
                        {
                            selector.DisplayName = "Custom";
                            RegisterEvents();
                            break;
                        }
                        case 1:
                        {
                            selector.DisplayName = "Default";
                            UnSubscribeEvents();
                            break;
                        }
                    }
                };
                ctsMenu.AddSeparator();
            }
            
            ctsMenu.AddSeparator();
            ctsMenu.AddGroupLabel("Custom Settings");
            ctsMenu.Add("vayneplus.cts.custom.forcetarget", new CheckBox("Focus Selected Target", false));
            ctsMenu.Add("vayneplus.cts.custom.drawcircle", new CheckBox("Draw Circle"));
            ConstructCustomMenu();
            SelectedTCircle = new Circle
            {
                Color = Color.Red,
                Radius = 150,
                BorderWidth = 16
            };
        }
        internal static void RegisterEvents()
        {
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        internal static void UnSubscribeEvents()
        {
            Game.OnWndProc -= Game_OnWndProc;
            Drawing.OnDraw -= Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (ctsMenu["vayneplus.cts.custom.drawcircle"].Cast<CheckBox>().CurrentValue && selectedHero.IsValidTarget())
            {
                SelectedTCircle.Draw(selectedHero.Position);
            }
        }

        static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowMessages.LeftButtonDown)
            {
                return;
            }
            selectedHero =
                HeroManager.Enemies
                    .FindAll(hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000) // 200 * 200
                    .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();
        }

        internal static void ConstructCustomMenu()
        {
            var Enemies = HeroManager.Enemies;
            var priorityDictionary = Enemies.ToDictionary(Enemy => Enemy, Enemy => GetPriorityByName(Enemy.ChampionName));
            foreach (var entry in priorityDictionary)
            {
                ctsMenu.Add("vayneplus.cts.custom.heros." + entry.Key.ChampionName.ToLowerInvariant(),
                    new Slider(entry.Key.ChampionName, (int) entry.Value, 1, 5));
            }
        }

        internal static AIHeroClient GetTarget(float Range)
        {
            if (IsActive())
            {
                var EnemiesInRange = ObjectManager.Player.GetEnemiesInRange(Range);
                if (EnemiesInRange.Any())
                {
                    var priorityDictionary = EnemiesInRange.Where(en => en.IsValidTarget(Range)).ToDictionary(Enemy => Enemy, Enemy => GetPriorityByName(Enemy.ChampionName));
                    if (priorityDictionary.Any())
                    {
                        var HighestPriorityTarget = priorityDictionary.OrderByDescending(pair => pair.Value).First().Key;
                        if (HighestPriorityTarget != null && HighestPriorityTarget.IsValidTarget(Range))
                        {
                            var HighestPriority = ctsMenu["vayneplus.cts.custom.heroes." + HighestPriorityTarget.ChampionName.ToLowerInvariant()].Cast<Slider>().CurrentValue;

                            var numberOfAttacks = HighestPriorityTarget.Health / ObjectManager.Player.GetAutoAttackDamage(HighestPriorityTarget);

                            foreach (var Item in priorityDictionary.Where(item => item.Key != HighestPriorityTarget))
                            {
                                var attacksNumber = HighestPriorityTarget.Health / ObjectManager.Player.GetAutoAttackDamage(Item.Key);
                                if ((attacksNumber <= 1 && Item.Key.IsValidTarget(Range)) || ((numberOfAttacks - attacksNumber) > 4 && Item.Key.IsValidTarget(Range)))
                                {
                                    return Item.Key;
                                }

                                if ((int)Item.Value >= HighestPriority)
                                {
                                    if (attacksNumber < numberOfAttacks && Item.Key.IsValidTarget(Range))
                                    {
                                        numberOfAttacks = attacksNumber;
                                        HighestPriorityTarget = Item.Key;
                                        HighestPriority = (int)Item.Value;
                                    }
                                }

                                if (!priorityDictionary.Any(m => (int)m.Value >= HighestPriority))
                                {
                                    HighestPriority -= 1;
                                }
                            }
                            if (selectedHero != null && selectedHero.IsDead)
                            {
                                selectedHero = null;
                            }
                            else if (selectedHero.IsValidTarget(Range))
                            {
                                return selectedHero;
                            }

                            if (scriptSelectedHero != null && scriptSelectedHero.IsDead)
                            {
                                scriptSelectedHero = null;
                            }
                            else if (scriptSelectedHero.IsValidTarget(Range))
                            {
                                return scriptSelectedHero;
                            }
                            return HighestPriorityTarget;
                        }
                    }
                }
            }
            return TargetSelector.GetTarget(Range, DamageType.Physical);
        }

        internal static bool IsActive()
        {
            return ctsMenu["vayneplus.cts.selector"].Cast<Slider>().CurrentValue == 0;
        }

        internal static Priority GetPriorityByName(string name)
        {
            if (priorityList.Any(m => m.Champions.Contains(name)))
            {
                return priorityList.First(m => m.Champions.Contains(name)).Priority;
            }

            return Priority.Low;
        }
    }

    internal class PriorityClass
    {
        public string Name { get; set; }

        public Priority Priority { get; set; }

        public string[] Champions { get; set; }
    }

    enum Priority
    {
        Highest = 4,
        High = 3,
        Medium = 2,
        Low = 1
    }
}
