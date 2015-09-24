using System;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using VaynePlus.Utils;

namespace VaynePlus
{
    class VaynePlus
    {
        public static Menu VayneMenu { get; set; }
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += LoadingOnOnLoadingComplete;
        }

        private static void LoadingOnOnLoadingComplete(EventArgs args)
        {
            SetMenu();
        }

        private static void SetMenu()
        {
            if (Player.Instance.ChampionName != "Vayne")
            {
                return;
            }

            VayneMenu = MainMenu.AddMenu("Vayne+", "vayneplus");
            VayneMenu.AddGroupLabel("Introduction");
            VayneMenu.AddLabel("Welcome to my first EloBuddy addon!");
            //CustomTargetSelector.OnLoad(VayneMenu);
            OrbwalkerModes.OnLoad(VayneMenu);
            Condemn.OnLoad(VayneMenu);
            PotionManager.OnLoad(VayneMenu);
            ItemManager.OnLoad(VayneMenu);
            AntiGapCloser.OnLoad(VayneMenu);
            Cleanse.OnLoad(VayneMenu);
            WallTumble.OnLoad(VayneMenu);
        }
    }
}
