using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using VaynePlus.Utils;
using Color = System.Drawing.Color;

namespace VaynePlus
{
    class OrbwalkerModes
    {
        private static SpellDataInst QSpell;
        private static SpellDataInst WSpell;
        private static SpellDataInst ESpell;
        private static SpellDataInst RSpell;
        private static bool goingToTumble;
        private static Vector3 TumblePosition = Vector3.Zero;
        private static int harassAACounter = 0;
        private static float _lastCheckTick;
        private static float _lastCheckTick2;
        public static Menu ModesMenu;
        private static Circle ERangeCircle;

        public static void OnLoad(Menu menu)
        {
            QSpell = Player.GetSpell(SpellSlot.Q);
            WSpell = Player.GetSpell(SpellSlot.W);
            ESpell = Player.GetSpell(SpellSlot.E);
            RSpell = Player.GetSpell(SpellSlot.R);

            BuildMenu(menu);
            Game.OnUpdate += GameOnOnTick;
            Orbwalker.OnPostAttack += OrbwalkingAfterAttack;
            Drawing.OnDraw += DrawingOnOnDraw;
            /*if (CustomTargetSelector.IsActive())
            {
                CustomTargetSelector.RegisterEvents();
            }*/
            ERangeCircle = new Circle
            {
                Color = Color.Red,
                Radius = 590
            };
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {
            ERangeCircle.Draw(Player.Instance.Position);
        }

        private static void BuildMenu(Menu menu)
        {
            ModesMenu = menu.AddSubMenu("Orbwalker Modes", "vayneplus.modes");
            ModesMenu.AddGroupLabel("Tumble (Q)");
            ModesMenu.AddLabel("Orbwalker Modes");
            ModesMenu.Add("vayneplus.modes.q.combo", new CheckBox("Use in Combo"));
            ModesMenu.Add("vayneplus.modes.q.harass", new CheckBox("Use in Harass"));
            ModesMenu.Add("vayneplus.modes.q.harass.mana", new Slider("Minimum Mana% Harass", 25));
            ModesMenu.Add("vayneplus.modes.q.farm", new CheckBox("Use in Farm"));
            ModesMenu.Add("vayneplus.modes.q.farm.mana", new Slider("Minimum Mana% Farm", 40));
            ModesMenu.AddLabel("Other Settings");
            ModesMenu.Add("vayneplus.modes.q.smartq", new CheckBox("Try to QE when possible", false));
            ModesMenu.Add("vayneplus.modes.q.noaastealth", new CheckBox("Don't AA while stealthed", false));
            ModesMenu.Add("vayneplus.modes.q.noqenemies", new CheckBox("Don't Q into enemies", false));
            ModesMenu.Add("vayneplus.modes.q.dynamicqsafety", new CheckBox("Use dynamic Q Safety Distance", false));
            ModesMenu.Add("vayneplus.modes.q.qspam", new CheckBox("Ignore Q checks", false));
            ModesMenu.Add("vayneplus.modes.q.qinrange", new CheckBox("Q For KS"));
            ModesMenu.Add("vayneplus.modes.q.mirin", new CheckBox("Use old 'Don't Q into enemies' check (Mirin)"));
            var qlogic = ModesMenu.Add("vayneplus.modes.q.qlogic", new Slider("Normal", 0, 0, 1));
            qlogic.OnValueChange += (sender, args) =>
            {
                switch (args.NewValue)
                {
                    case 0:
                    {
                        qlogic.DisplayName = "Normal";
                        break;
                    }
                    case 1:
                    {
                        qlogic.DisplayName = "Kite melees";
                        break;
                    }
                }
            };

            ModesMenu.AddSeparator();
            ModesMenu.AddGroupLabel("Condemn (E)");
            ModesMenu.AddLabel("Orbwalker Modes");
            ModesMenu.Add("vayneplus.modes.e.combo", new CheckBox("Use in Combo"));
            ModesMenu.Add("vayneplus.modes.e.harass", new CheckBox("Use in Harass"));
            ModesMenu.Add("vayneplus.modes.e.harass.mana", new Slider("Minimum Mana% Harass", 20));
            ModesMenu.AddLabel("Other Settings");
            ModesMenu.Add("vayneplus.modes.e.lowlifepeel", new CheckBox("Peel with E when low health", false));
            ModesMenu.Add("vayneplus.modes.e.autoe", new CheckBox("Auto E", false));
            ModesMenu.Add("vayneplus.modes.e.eks", new CheckBox("Smart E KS", false));
            ModesMenu.Add("vayneplus.modes.e.ethird", new CheckBox("E 3rd proc in Harass", false));

            ModesMenu.AddGroupLabel("Final Hour (R)");
            ModesMenu.AddLabel("Orbwalker Modes");
            ModesMenu.Add("vayneplus.modes.r.combo", new CheckBox("Use in Combo", false));
            ModesMenu.Add("vayneplus.modes.r.minenemies", new Slider("Min R Enemies", 2, 1, 5));

            ModesMenu.AddSeparator();
            ModesMenu.AddGroupLabel("Other Settings");
            ModesMenu.Add("vayneplus.modes.specialfocus", new CheckBox("Focus targets with 2 W marks", false));
        }

        private static void GameOnOnTick(EventArgs args)
        {
            if (Player.Instance.IsDead || Player.Instance.IsRecalling() || Helpers.IsFountain(Player.Instance.ServerPosition))
            {
                return;
            }

            #region TS & WallTumble
            if (goingToTumble && TumblePosition != Vector3.Zero)
            {
                Vector2 drakeWallQPos = new Vector2(11514, 4462);
                Vector2 midWallQPos = new Vector2(6962, 8952);

                if (TumblePosition == drakeWallQPos.To3D())
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

                            goingToTumble = false;
                            TumblePosition = Vector3.Zero;
                        }, (int)(106 + Game.Ping / 2f));
                    }
                }

            }
            #endregion

            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Combo:
                    Combo();
                    break;
                case Orbwalker.ActiveModes.Harass:
                    Harrass();
                    break;
            }

            OnUpdateFunctions();
        }

        private static void OrbwalkingAfterAttack(AttackableUnit target, EventArgs args)
        {
            if (!(target is Obj_AI_Base))
            {
                return;
            }

            var tg = (Obj_AI_Base)target;
            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Combo:
                    if (QSpell.IsReady && ModesMenu["vayneplus.modes.q.combo"].Cast<CheckBox>().CurrentValue)
                    {
                        CastQ(tg);
                    }
                    break;
                case Orbwalker.ActiveModes.Harass:
                    if (QSpell.IsReady && ModesMenu["vayneplus.modes.q.harass"].Cast<CheckBox>().CurrentValue &&
                    (tg is AIHeroClient) &&
                    ((tg as AIHeroClient).GetWBuff() != null && (tg as AIHeroClient).GetWBuff().Count >= 1 &&
                     WSpell.Level > 0))
                    {
                        CastQ(tg);
                        harassAACounter = 0;
                    }
                    break;
                case Orbwalker.ActiveModes.LastHit:
                    Farm();
                    break;
                case Orbwalker.ActiveModes.LaneClear:
                    Farm();
                    break;
            }
        }

        private static void Combo()
        {
            if (Environment.TickCount - _lastCheckTick2 < 80)
            {
                return;
            }

            _lastCheckTick2 = Environment.TickCount;

            if (ESpell.IsReady && ModesMenu["vayneplus.modes.e.combo"].Cast<CheckBox>().CurrentValue)
            {
                AIHeroClient target;

                if (Condemn.CondemnCheck(ObjectManager.Player.ServerPosition, out target))
                {
                    if (target.IsValidTarget(590) && (target is AIHeroClient))
                    {
                        Player.CastSpell(SpellSlot.E, target);
                    }
                }
            }

            if (QSpell.IsReady && ModesMenu["vayneplus.modes.q.combo"].Cast<CheckBox>().CurrentValue)
            {
                CheckAndCastKSQ();
            }
        }

        private static void Harrass()
        {
            if (ESpell.IsReady && ModesMenu["vayneplus.modes.e.harass"].Cast<CheckBox>().CurrentValue)
            {
                var possibleTarget = HeroManager.Enemies.Find(enemy => enemy.IsValidTarget(590) && enemy.Has2WStacks());
                if (possibleTarget != null && ModesMenu["vayneplus.modes.e.ethird"].Cast<CheckBox>().CurrentValue && (possibleTarget is AIHeroClient) && !ObjectManager.Player.HasBuff("vaynetumblebonus"))
                {
                    Player.CastSpell(SpellSlot.E, possibleTarget);
                }

                AIHeroClient target;
                if (Condemn.CondemnCheck(ObjectManager.Player.ServerPosition, out target))
                {
                    if (target.IsValidTarget(590) && (target is AIHeroClient))
                    {
                        Player.CastSpell(SpellSlot.E, target);
                    }
                }
            }
        }

        private static void Farm()
        {
            if (!QSpell.IsReady && ModesMenu["vayneplus.modes.q.farm"].Cast<CheckBox>().CurrentValue)
            {
                return;
            }

            var minionsInRange = ObjectManager.Get<Obj_AI_Minion>().Where(h => h.Distance(ObjectManager.Player.ServerPosition) < Player.Instance.AttackRange).ToList().FindAll(m => m.Health <= Player.Instance.GetAutoAttackDamage(m) + Helpers.GetDamage(m, SpellSlot.Q)).ToList();

            if (!minionsInRange.Any())
            {
                return;
            }

            if (minionsInRange.Count > 1)
            {
                var firstMinion = minionsInRange.OrderBy(m => m.HealthPercent).First();
                CastTumble(firstMinion);
                Orbwalker.ForcedTarget = firstMinion;
            }
        }

        private static void CheckAndCastKSQ()
        {
            if (ModesMenu["vayneplus.modes.q.qinrange"].Cast<CheckBox>().CurrentValue && QSpell.IsReady)
            {
                var currentTarget = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 240f, DamageType.Physical);
                if (!currentTarget.IsValidTarget())
                {
                    return;
                }

                if (currentTarget.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <=
                    Player.Instance.GetAutoAttackRange())
                {
                    return;
                }

                if (currentTarget.Health + 15 <
                    ObjectManager.Player.GetAutoAttackDamage(currentTarget) + Helpers.GetDamage(currentTarget, SpellSlot.Q))
                {
                    var extendedPosition = ObjectManager.Player.ServerPosition.Extend(
                        currentTarget.ServerPosition, 300f);
                    if (extendedPosition.To3D().OkToQ())
                    {
                        Player.CastSpell(SpellSlot.Q, extendedPosition.To3D());
                        Orbwalker.ResetAutoAttack();
                        Orbwalker.ForcedTarget = currentTarget;
                        //CustomTargetSelector.scriptSelectedHero = currentTarget;
                    }
                }
            }
        }

        private static void CastQ(Obj_AI_Base target)
        {
            var myPosition = Game.CursorPos;
            AIHeroClient myTarget = null;
            if (ModesMenu["vayneplus.modes.q.smartq"].Cast<CheckBox>().CurrentValue && ESpell.IsReady)
            {
                const int currentStep = 30;
                var direction = ObjectManager.Player.Direction.To2D().Perpendicular();
                for (var i = 0f; i < 360f; i += currentStep)
                {
                    var angleRad = EloBuddy.SDK.Geometry.DegreeToRadian(i);
                    var rotatedPosition = ObjectManager.Player.Position.To2D() + (300f * direction.Rotated(angleRad));
                    if (Condemn.CondemnCheck(rotatedPosition.To3D(), out myTarget) && rotatedPosition.To3D().OkToQ())
                    {
                        myPosition = rotatedPosition.To3D();
                        break;
                    }
                }
            }

            if (RSpell.IsReady && ModesMenu["vayneplus.modes.r.combo"].Cast<CheckBox>().CurrentValue && (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo) && ObjectManager.Player.CountEnemiesInRange(Player.Instance.GetAutoAttackRange() + 200f) >= ModesMenu["vayneplus.modes.r.minenemies"].Cast<Slider>().CurrentValue)
            {
                Player.CastSpell(SpellSlot.R);
            }

            CastTumble(myPosition, target);

            if (myPosition != Game.CursorPos && myTarget != null && myTarget.IsValidTarget(300f + 590) && ESpell.IsReady)
            {
                Core.DelayAction(
                    () =>
                    {
                        if (!QSpell.IsReady)
                        {
                            Player.CastSpell(SpellSlot.E, myTarget);
                        }
                    }, (int)(Game.Ping / 2f + Player.GetSpell(SpellSlot.Q).SData.DelayTotalTimePercent * 1000 + 300f / 1500f + 50f));
            }
        }

        private static void CastTumble(Obj_AI_Base target)
        {
            if (!QSpell.IsReady)
            {
                return;
            }

            var posAfterTumble =
                ObjectManager.Player.ServerPosition.To2D().Extend(Game.CursorPos.To2D(), 300f).To3D();
            var distanceAfterTumble = Vector3.DistanceSquared(posAfterTumble, target.ServerPosition);
            if ((distanceAfterTumble <= 550 * 550 && distanceAfterTumble >= 100 * 100) || (ModesMenu["vayneplus.modes.q.qspam"].Cast<CheckBox>().CurrentValue))
            {
                if (!posAfterTumble.OkToQ2() && ModesMenu["vayneplus.modes.q.noqenemies"].Cast<CheckBox>().CurrentValue)
                {
                    if (!(ModesMenu["vayneplus.modes.q.qspam"].Cast<CheckBox>().CurrentValue))
                    {
                        return;
                    }
                }
                Player.CastSpell(SpellSlot.Q, Game.CursorPos);
            }
        }

        private static void CastTumble(Vector3 pos, Obj_AI_Base target)
        {
            if (!QSpell.IsReady)
            {
                return;
            }

            var posAfterTumble = ObjectManager.Player.ServerPosition.To2D().Extend(pos.To2D(), 300f).To3D();
            var distanceAfterTumble = Vector3.DistanceSquared(posAfterTumble, target.ServerPosition);
            if ((distanceAfterTumble < 550 * 550 && distanceAfterTumble > 100 * 100) || (ModesMenu["vayneplus.modes.q.qspam"].Cast<CheckBox>().CurrentValue))
            {
                switch (ModesMenu["vayneplus.modes.q.qlogic"].Cast<Slider>().CurrentValue)
                {
                    case 0:
                        if (!posAfterTumble.OkToQ2() && ModesMenu["vayneplus.modes.q.noqenemies"].Cast<CheckBox>().CurrentValue)
                        {
                            if (!(ModesMenu["vayneplus.modes.q.qspam"].Cast<CheckBox>().CurrentValue))
                            {
                                return;
                            }
                        }
                        Player.CastSpell(SpellSlot.Q, pos);
                        break;

                    case 1:
                        if (PositionalHelper.MeleeEnemiesTowardsMe.Any() && !PositionalHelper.MeleeEnemiesTowardsMe.All(m => m.HealthPercent <= 15))
                        {
                            var Closest = PositionalHelper.MeleeEnemiesTowardsMe.OrderBy(m => m.Distance(ObjectManager.Player)).First();

                            var whereToQ = Closest.ServerPosition.Extend(ObjectManager.Player.ServerPosition, Closest.Distance(ObjectManager.Player) + 300f);

                            if ((whereToQ.To3D().OkToQ2() || (!whereToQ.To3D().OkToQ2() && ModesMenu["vayneplus.modes.q.qspam"].Cast<CheckBox>().CurrentValue)) && !whereToQ.To3D().UnderTurret(true))
                            {
                                Player.CastSpell(SpellSlot.Q, whereToQ.To3D());
                                return;
                            }
                        }

                        if (!Helpers.OkToQ2(posAfterTumble) && ModesMenu["vayneplus.modes.q.noqenemies"].Cast<CheckBox>().CurrentValue)
                        {
                            if (!(ModesMenu["vayneplus.modes.q.qspam"].Cast<CheckBox>().CurrentValue))
                            {
                                return;
                            }
                        }

                        Player.CastSpell(SpellSlot.Q, pos);
                        break;
                }
            }
        }

        private static void OnUpdateFunctions()
        {
            if (Environment.TickCount - _lastCheckTick < 150)
            {
                return;
            }

            _lastCheckTick = Environment.TickCount;

            #region Auto E
            if (ModesMenu["vayneplus.modes.e.autoe"].Cast<CheckBox>().CurrentValue && ESpell.IsReady)
            {
                AIHeroClient target;
                if (Condemn.CondemnCheck(ObjectManager.Player.ServerPosition, out target))
                {
                    if (target.IsValidTarget(590) && (target is AIHeroClient))
                    {
                        Player.CastSpell(SpellSlot.E, target);
                    }
                }
            }
            #endregion

            #region Focus 2 W stacks
            if (ModesMenu["vayneplus.modes.specialfocus"].Cast<CheckBox>().CurrentValue)
            {
                var target = HeroManager.Enemies.Find(en => en.IsValidTarget(ObjectManager.Player.AttackRange) && en.Has2WStacks());
                if (target != null)
                {
                    Orbwalker.ForcedTarget = target;
                }
            }
            #endregion

            #region Condemn KS
            if (ModesMenu["vayneplus.modes.e.eks"].Cast<CheckBox>().CurrentValue && ESpell.IsReady && !ObjectManager.Player.HasBuff("vaynetumblebonus"))
            {
                var target = HeroManager.Enemies.Find(en => en.IsValidTarget(590) && en.Has2WStacks());
                if (target.IsValidTarget() && target.Health + 60 <= (Helpers.GetDamage(target, SpellSlot.E) + Helpers.GetDamage(target, SpellSlot.W)) && (target is AIHeroClient))
                {
                    Player.CastSpell(SpellSlot.E, target);
                }
            }
            #endregion

            #region Low Life Peel
            if (ModesMenu["vayneplus.modes.e.lowlifepeel"].Cast<CheckBox>().CurrentValue && ObjectManager.Player.HealthPercent <= 15 && ESpell.IsReady)
            {
                var meleeEnemies = HeroManager.Enemies.Where(h => h.Distance(Player.Instance) < 375f).ToList().FindAll(m => m.IsMelee);
                if (meleeEnemies.Any())
                {
                    var mostDangerous = meleeEnemies.OrderByDescending(m => m.GetAutoAttackDamage(ObjectManager.Player)).First();
                    if (mostDangerous is AIHeroClient)
                    {
                        Player.CastSpell(SpellSlot.E, mostDangerous);
                    }
                }
            }
            #endregion
        }
    }
}
