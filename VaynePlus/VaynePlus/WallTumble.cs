using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using VaynePlus.Utils;
using Rendering=EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;

namespace VaynePlus
{
    class WallTumble
    {
        private static Rendering.Circle SpotCircle;
        private static Menu TumbleMenu;
        public static void OnLoad(Menu menu)
        {
            TumbleMenu = menu.AddSubMenu("Wall Tumble", "vayneplus.walltumble");
            TumbleMenu.AddGroupLabel("Wall Tumble");
            TumbleMenu.Add("vayneplus.walltumble.key", new KeyBind("HotKey", false, KeyBind.BindTypes.HoldActive, 'Y'));
            TumbleMenu.AddGroupLabel("Drawing");
            TumbleMenu.Add("vayneplus.walltumble.drawing", new CheckBox("Draw Sports"));

            SpotCircle = new Rendering.Circle
            {
                Color = Color.AliceBlue,
                Radius = 65
            };
            Drawing.OnDraw += DrawingOnOnDraw;
            Game.OnTick += GameOnOnTick;
        }

        private static void GameOnOnTick(EventArgs args)
        {
            if (!TumbleMenu["vayneplus.walltumble.key"].Cast<KeyBind>().CurrentValue || !Player.GetSpell(SpellSlot.Q).IsReady)
            {
                return;
            }

            Vector2 drakeWallQPos = new Vector2(11514, 4462);
            Vector2 midWallQPos = new Vector2(6667, 8794);

            if (Player.Instance.Distance(midWallQPos) >= Player.Instance.Distance(drakeWallQPos))
            {

                if (Player.Instance.Position.X < 12000 || Player.Instance.Position.X > 12070 || Player.Instance.Position.Y < 4800 ||
                    Player.Instance.Position.Y > 4872)
                {
                    Helpers.MoveToLimited(new Vector2(12050, 4827).To3D());
                }
                else
                {
                    Helpers.MoveToLimited(new Vector2(12050, 4827).To3D());
                    Core.DelayAction(() =>
                    {
                        Player.CastSpell(SpellSlot.Q, drakeWallQPos.To3D());

                    }, (int)(106 + Game.Ping / 2f));
                }
            }
            else
            {
                if (Player.Instance.Position.X < 6908 || Player.Instance.Position.X > 6978 || Player.Instance.Position.Y < 8917 ||
                    Player.Instance.Position.Y > 8989)
                {
                    Helpers.MoveToLimited(new Vector2(6962, 8952).To3D());
                }
                else
                {
                    Helpers.MoveToLimited(new Vector2(6962, 8952).To3D());
                    Core.DelayAction(() =>
                    {
                        Player.CastSpell(SpellSlot.Q, midWallQPos.To3D());

                    }, (int)(106 + Game.Ping / 2f));
                }
            }
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {
            var drakeWallQPos = new Vector2(11514, 4462);
            var midWallQPos = new Vector2(6962, 8952);

            if (TumbleMenu["vayneplus.walltumble.drawing"].Cast<CheckBox>().CurrentValue)
            {
                if (ObjectManager.Player.Distance(drakeWallQPos) <= 1500f && Helpers.IsSummonersRift())
                {
                    SpotCircle.Draw(new Vector2(12050, 4827).To3D());
                }
                if (ObjectManager.Player.Distance(midWallQPos) <= 1500f && Helpers.IsSummonersRift())
                {
                    SpotCircle.Draw(new Vector2(6962, 8952).To3D());
                }
            }
        }
    }
}
