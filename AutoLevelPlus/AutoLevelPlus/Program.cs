using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace AutoLevelPlus
{
    class Program
    {
        private static readonly List<Level> SimpleLvLs = new List<Level>();  
        private static readonly List<Level> LoLBuilderLvLs = new List<Level>();

        private static string ChampionName;

        public static Menu AutoLevelMenu { get; set; }

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += LoadingOnOnLoadingComplete;
        }

        private static void LoadingOnOnLoadingComplete(EventArgs args)
        {
            new System.Threading.Thread(() =>
            {
                LoadLoLBuilder();
            }).Start();
            LoadEasy();

            ChampionName = Player.Instance.ChampionName.Replace(" ", "").Replace("'", "").ToLower();
            AutoLevelMenu = MainMenu.AddMenu("AutoLevel+", "autolevelplus." + ChampionName);
            AutoLevelMenu.AddGroupLabel("Mode");
            var slider = AutoLevelMenu.Add("autolevelplus." + ChampionName + ".mode", new Slider("LoLBuilder", 2, 1, 2));
            slider.OnValueChange += (sender, changeArgs) =>
            {
                switch (changeArgs.NewValue)
                {
                    case 1:
                        slider.DisplayName = "LoLBuilder";
                        return;
                    case 2:
                        slider.DisplayName = "Easy";
                        return;
                    case 3:
                        slider.DisplayName = "Advanced";
                        return;
                }
            };
            AutoLevelMenu.AddGroupLabel("General");
            AutoLevelMenu.Add("autolevelplus." + ChampionName + ".firstlvl", new Slider("Start Leveling at", 2, 1, 18));

            var easymenu = AutoLevelMenu.AddSubMenu("Easy", "autolevelplus." + ChampionName + ".easy");
            {
                easymenu.AddGroupLabel("General");
                easymenu.Add("autolevelplus." + ChampionName + ".easy.qwe", new CheckBox("Level QWE the first 3 lvl"));
                easymenu.AddGroupLabel("Q");
                easymenu.Add("autolevelplus." + ChampionName + ".easy.q", new Slider(" ", 2, 1, 4));
                easymenu.AddGroupLabel("W");
                easymenu.Add("autolevelplus." + ChampionName + ".easy.w", new Slider(" ", 3, 1, 4));
                easymenu.AddGroupLabel("E");
                easymenu.Add("autolevelplus." + ChampionName + ".easy.e", new Slider(" ", 4, 1, 4));
                easymenu.AddGroupLabel("R");
                easymenu.Add("autolevelplus." + ChampionName + ".easy.r", new Slider(" ", 1, 1, 4));
            }

            /*var advancedmenu = AutoLevelMenu.AddSubMenu("Advanced", "autolevelplus." + ChampionName + ".advanced");
            {
                for (int i = 1; i < 19; i++)
                {
                    advancedmenu.AddGroupLabel("Level " + i);
                    advancedmenu.Add("autolevelplus." + ChampionName + ".advanced." + i + ".q", new CheckBox("Q", false));
                    advancedmenu.Add("autolevelplus." + ChampionName + ".advanced." + i + ".e", new CheckBox("E", false));
                    advancedmenu.Add("autolevelplus." + ChampionName + ".advanced." + i + ".w", new CheckBox("W", false));
                    advancedmenu.Add("autolevelplus." + ChampionName + ".advanced." + i + ".r", new CheckBox("R", false));
                }
            }*/

            Game.OnTick += GameOnOnTick;
        }

        private static void GameOnOnTick(EventArgs args)
        {
            if (Player.Instance.SpellTrainingPoints == 0 ||
                Player.Instance.Level <
                AutoLevelMenu["autolevelplus." + ChampionName + ".firstlvl"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            if (AutoLevelMenu["autolevelplus." + ChampionName + ".mode"].Cast<Slider>().CurrentValue == 1)
            {
                Player.LevelSpell(LoLBuilderLvLs.Find(h => h.Priority == Player.Instance.Level).Slot);
                return;
            }

            if (AutoLevelMenu["autolevelplus." + ChampionName + ".mode"].Cast<Slider>().CurrentValue != 2)
            {
                return;
            }

            if (Player.Instance.Level <= 3)
            {
                foreach (var lvl in SimpleLvLs)
                {
                    if (Player.GetSpell(lvl.Slot).IsLearned || lvl.Slot == SpellSlot.R)
                    {
                        continue;
                    }

                    Player.LevelSpell(lvl.Slot);
                }

                return;
            }

            SimpleLvLs.OrderBy(h => h.Priority);

            SimpleLvLs[0].Priority = Player.GetSpell(SimpleLvLs[0].Slot).Level + 1;
            Player.LevelSpell(SimpleLvLs[0].Slot);
            if (SimpleLvLs[0].ExpectedLevel == Player.GetSpell(SimpleLvLs[0].Slot).Level)
            {
                return;
            }

            SimpleLvLs[1].Priority = Player.GetSpell(SimpleLvLs[1].Slot).Level + 1;
            Player.LevelSpell(SimpleLvLs[1].Slot);
            if (SimpleLvLs[1].ExpectedLevel == Player.GetSpell(SimpleLvLs[1].Slot).Level)
            {
                return;
            }

            SimpleLvLs[2].Priority = Player.GetSpell(SimpleLvLs[2].Slot).Level + 1;
            Player.LevelSpell(SimpleLvLs[2].Slot);
            if (SimpleLvLs[2].ExpectedLevel == Player.GetSpell(SimpleLvLs[2].Slot).Level)
            {
                return;
            }

            SimpleLvLs[3].Priority = Player.GetSpell(SimpleLvLs[3].Slot).Level + 1;
            Player.LevelSpell(SimpleLvLs[3].Slot);
        }

        private static void LoadLoLBuilder()
        {
            var skillSequence = LoLBuilder.GetSkillSequence(ChampionName);
            if (skillSequence == null)
            {
                Console.WriteLine("Error couldnt load LoLBuilder");
                return;
            }

            int count = 1;
            foreach (var level in skillSequence)
            {
                LoLBuilderLvLs.Add(new Level
                {
                    Priority = count,
                    Slot = (SpellSlot)(level - 1)
                });
                count++;
            }
        }

        private static void LoadEasy()
        {
            SimpleLvLs.Add(new Level
            {
                ExpectedLevel = -1,
                Priority = 1,
                Slot = SpellSlot.R
            });
            SimpleLvLs.Add(new Level
            {
                ExpectedLevel = -1,
                Priority = 2,
                Slot = SpellSlot.Q
            });
            SimpleLvLs.Add(new Level
            {
                ExpectedLevel = -1,
                Priority = 3,
                Slot = SpellSlot.W
            });
            SimpleLvLs.Add(new Level
            {
                ExpectedLevel = -1,
                Priority = 4,
                Slot = SpellSlot.E
            });
        }
    }

    class Level
    {
        public int ExpectedLevel { get; set; }
        public SpellSlot Slot { get; set; }
        public int Priority { get; set; }
    }
}
