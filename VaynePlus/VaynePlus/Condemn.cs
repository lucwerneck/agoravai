using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using VaynePlus.Utils;

namespace VaynePlus
{
    class Condemn
    {
        private static Spell.Targeted ESpell;
        private static Spell.Targeted TrinketSpell;
        public static Menu CondemnMenu;
        private static float _lastCondemnCheck;

        public static void OnLoad(Menu menu)
        {
            CondemnMenu = menu.AddSubMenu("Condemn", "vayneplus.condemn");
            CondemnMenu.AddGroupLabel("Condemn Mode");
            var mode = CondemnMenu.Add("vaynemenu.condemn.mode", new Slider("VHReborn", 1, 0, 4));
            mode.OnValueChange += (sender, args) =>
            {
                switch (args.NewValue)
                {
                    case 0:
                    {
                        mode.DisplayName = "VHRevolution";
                        break;
                    }
                    case 1:
                    {
                        mode.DisplayName = "VHReborn";
                        break;
                    }
                    case 2:
                    {
                        mode.DisplayName = "Marksman/Gosu Condemn Code";
                        break;
                    }
                    case 3:
                    {
                        mode.DisplayName = "VHRework";
                        break;
                    }
                    case 4:
                    {
                        mode.DisplayName = "Shine";
                        break;
                    }
                }
            };
            CondemnMenu.AddSeparator();
            CondemnMenu.AddGroupLabel("Condemn Settings");
            CondemnMenu.Add("vaynemenu.condemn.onlystuncurrent", new CheckBox("Only stun current target", false));
            CondemnMenu.Add("vaynemenu.condemn.trinketbush", new CheckBox("Trinket Bush on Condemn"));
            CondemnMenu.Add("vaynemenu.condemn.condemnturret", new CheckBox("Try to Condemn to turret", false));
            CondemnMenu.Add("vaynemenu.condemn.antigp", new CheckBox("AntiGapClose"));
            CondemnMenu.Add("vaynemenu.condemn.interrupt", new CheckBox("Interrupter"));

            CondemnMenu.AddSeparator();
            CondemnMenu.Add("vaynemenu.condemn.pushdistance", new Slider("E Push Distance", 395, 350, 470));
            CondemnMenu.Add("vaynemenu.condemn.condemn.accuracy", new Slider("Accuracy (Revolution Only)", 33, 1));
            CondemnMenu.Add("vaynemenu.condemn.condemn.noeaa", new Slider("Don't E if Target can be killed in X AA", 1, 0, 4));

            ESpell = new Spell.Targeted(SpellSlot.E, 590);
            TrinketSpell = new Spell.Targeted(SpellSlot.Trinket, 0);

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            AntiGapCloser.OnEnemyGapcloser += AntiGapCloserOnOnEnemyGapcloser;
            Interrupter.OnInterruptableSpell += InterrupterOnOnInterruptableSpell;
            GameObject.OnCreate += GameObject_OnCreate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.E && Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                if (!(args.Target is AIHeroClient))
                {
                    args.Process = false;
                    return;
                }

                AIHeroClient tg;
                if (CondemnCheck(ObjectManager.Player.ServerPosition, out tg))
                {
                    var target = tg;
                    var pushDistance = CondemnMenu["vaynemenu.condemn.pushdistance"].Cast<Slider>().CurrentValue;
                    var targetPosition = target.ServerPosition;
                    var pushDirection = (targetPosition - ObjectManager.Player.ServerPosition).Normalized();
                    float checkDistance = pushDistance / 40f;
                    for (int i = 0; i < 40; i++)
                    {
                        Vector3 finalPosition = targetPosition + (pushDirection * checkDistance * i);
                        var collFlags = NavMesh.GetCollisionFlags(finalPosition);
                        if ((collFlags == CollisionFlags.Wall || collFlags == CollisionFlags.Building) && finalPosition.GetWallsInRange(target).Any())
                        {
                            return;
                        }
                    }

                    args.Process = false;
                    Console.WriteLine("Blocked Condemn");
                }

            }

        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender is AIHeroClient)
            {
                var s2 = (AIHeroClient)sender;
                if (s2.IsValidTarget() && s2.ChampionName == "Pantheon" && s2.Spellbook.Spells.Find(h => h.Name == args.SData.Name).Slot == SpellSlot.W)
                {
                    if (CondemnMenu["vaynemenu.condemn.antigp"].Cast<CheckBox>().CurrentValue && args.Target.IsMe)
                    {
                        if (s2.IsValidTarget(ESpell.Range))
                        {
                            Player.CastSpell(SpellSlot.E, s2);
                            //ESpell.Cast(s2);
                        }
                    }
                }
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (CondemnMenu["vaynemenu.condemn.antigp"].Cast<CheckBox>().CurrentValue && ESpell.IsReady())
            {
                if (sender.IsEnemy && sender.Name == "Rengar_LeapSound.troy")
                {
                    var rengarEntity = HeroManager.Enemies.Find(h => h.ChampionName.Equals("Rengar") && h.IsValidTarget(ESpell.Range));
                    if (rengarEntity != null)
                    {
                        Player.CastSpell(SpellSlot.E, rengarEntity);
                        //ESpell.Cast(rengarEntity);
                    }
                }
            }
        }
        
        private static void AntiGapCloserOnOnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (CondemnMenu["vaynemenu.condemn.antigp"].Cast<CheckBox>().CurrentValue)
            {
                var Sender = gapcloser.Sender;
                Core.DelayAction(
                    () =>
                    {
                        if (Sender.IsValidTarget(ESpell.Range)
                            && gapcloser.End.Distance(ObjectManager.Player.ServerPosition) <= 400f
                            && (Sender is AIHeroClient)
                            &&
                            AntiGapCloser.GapCloserMenu[
                                string.Format("vayneplus.agplist.{0}.{1}",
                                    Sender.ChampionName.ToLowerInvariant(), gapcloser.SpellName)]
                                .Cast<CheckBox>().CurrentValue)
                        {
                            Player.CastSpell(SpellSlot.E, Sender);
                            //ESpell.Cast(Sender);
                        }
                    }, AntiGapCloser.GapCloserMenu["vayneplus.agplist.delay"].Cast<Slider>().CurrentValue);
            }
        }

        private static void InterrupterOnOnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs interruptableSpellEventArgs)
        {
            if (CondemnMenu["vaynemenu.condemn.interrupt"].Cast<CheckBox>().CurrentValue)
            {
                if (interruptableSpellEventArgs.DangerLevel == DangerLevel.High && sender.IsValidTarget(ESpell.Range) && (sender is AIHeroClient))
                {
                    Player.CastSpell(SpellSlot.E, sender);
                    //ESpell.Cast(sender);
                }
            }
        }

        public static bool CondemnCheck(Vector3 fromPosition, out AIHeroClient tg)
        {
            if (Environment.TickCount - _lastCondemnCheck < 150)
            {
                tg = null;
                return false;
            }

            _lastCondemnCheck = Environment.TickCount;

            switch (CondemnMenu["vaynemenu.condemn.mode"].Cast<Slider>().CurrentValue)
            {
                #region VH revolution
                case 0:
                    {
                        var MinChecksPercent = CondemnMenu["vaynemenu.condemn.condemn.accuracy"].Cast<Slider>().CurrentValue;
                        var PushDistance = CondemnMenu["vaynemenu.condemn.pushdistance"].Cast<Slider>().CurrentValue;

                        foreach (var hero in HeroManager.Enemies.Where(h => h.IsValidTarget(ESpell.Range) && !h.HasBuffOfType(BuffType.SpellShield) && !h.HasBuffOfType(BuffType.SpellImmunity)))
                        {
                            var finalPosition = hero.ServerPosition.Extend(fromPosition, -PushDistance);
                            var prediction = Prediction.Position.PredictLinearMissile(hero, ESpell.Range, (int)hero.BoundingRadius, 250, 1250, -1);

                            if (CondemnMenu["vaynemenu.condemn.onlystuncurrent"].Cast<CheckBox>().CurrentValue && hero.NetworkId != Orbwalker.GetTarget().NetworkId)
                            {
                                continue;
                            }

                            if (hero.Health + 10 <= ObjectManager.Player.GetAutoAttackDamage(hero) * CondemnMenu["vaynemenu.condemn.condemn.noeaa"].Cast<Slider>().CurrentValue)
                            {
                                continue;
                            }

                            var PredictionsList = new List<Vector3>
                        {
                            hero.ServerPosition,
                            hero.Position,
                            prediction.CastPosition,
                            prediction.UnitPosition
                        };

                            if (hero.IsDashing())
                            {
                                PredictionsList.Add(Prediction.Position.GetDashPos(hero).To3D());
                            }



                            var ExtendedList = new List<Vector3>();
                            var wallsFound = 0;
                            foreach (var position in PredictionsList)
                            {
                                for (var i = 0; i < PushDistance; i += (int)hero.BoundingRadius)
                                {
                                    var cPos = position.Extend(fromPosition, -i);
                                    ExtendedList.Add(cPos.To3D());
                                    if (cPos.IsWall())
                                    {
                                        wallsFound++;
                                        break;
                                    }
                                }
                            }

                            if ((wallsFound / PredictionsList.Count) >= MinChecksPercent / 100f)
                            {
                                if (NavMesh.IsWallOfGrass(finalPosition.To3D(), 25) && CondemnMenu["vaynemenu.condemn.trinketbush"].Cast<CheckBox>().CurrentValue && TrinketSpell != null && TrinketSpell.IsReady())
                                {
                                    var wardPosition = Player.Instance.ServerPosition.Extend(finalPosition, ObjectManager.Player.ServerPosition.Distance(finalPosition) - 25f);
                                    Player.CastSpell(SpellSlot.Trinket, wardPosition.To3D());
                                }

                                tg = hero;
                                return true;
                            }
                        }
                        break;
                    }
                #endregion

                #region VHReborn
                case 1:
                    {
                        foreach (var target in HeroManager.Enemies.Where(h => h.IsValidTarget(ESpell.Range) && !h.HasBuffOfType(BuffType.SpellShield) && !h.HasBuffOfType(BuffType.SpellImmunity)))
                        {
                            var pushDistance = CondemnMenu["vaynemenu.condemn.pushdistance"].Cast<Slider>().CurrentValue;
                            var targetPosition = target.ServerPosition;
                            var numberOfChecks = (float)Math.Ceiling(pushDistance / 40f);
                            var finalPosition = targetPosition.Extend(fromPosition, -pushDistance);

                            if (CondemnMenu["vaynemenu.condemn.onlystuncurrent"].Cast<CheckBox>().CurrentValue && !target.Equals(Orbwalker.GetTarget()))
                            {
                                continue;
                            }

                            for (var i = 1; i <= 40; i++)
                            {
                                var v3 = (targetPosition - fromPosition).Normalized();
                                var extendedPosition = targetPosition + v3 * (numberOfChecks * i);
                                if (extendedPosition.IsWall() && !target.IsDashing())
                                {
                                    if (target.Health + 10 <= ObjectManager.Player.GetAutoAttackDamage(target) * CondemnMenu["vaynemenu.condemn.condemn.noeaa"].Cast<Slider>().CurrentValue)
                                    {
                                        tg = null;
                                        Console.WriteLine("lowhp");
                                        return false;
                                    }

                                    if (NavMesh.IsWallOfGrass(finalPosition.To3D(), 25) && CondemnMenu["vaynemenu.condemn.trinketbush"].Cast<CheckBox>().CurrentValue && TrinketSpell != null && TrinketSpell.IsReady())
                                    {
                                        var wardPosition = Player.Instance.ServerPosition.Extend(finalPosition, ObjectManager.Player.ServerPosition.Distance(finalPosition) - 25f);
                                        Player.CastSpell(SpellSlot.Trinket, wardPosition.To3D());
                                    }

                                    tg = target;
                                    return true;
                                }
                            }
                        }
                        break;
                    }
                #endregion

                #region Marksman/Gosu Condemn Code
                case 2:
                    {
                        foreach (var target in HeroManager.Enemies.Where(h => h.IsValidTarget(ESpell.Range)))
                        {
                            var pushDistance = CondemnMenu["vaynemenu.condemn.pushdistance"].Cast<Slider>().CurrentValue;
                            var targetPosition = Prediction.Position.PredictLinearMissile(target, ESpell.Range, (int)target.BoundingRadius, 250, 1250, -1).UnitPosition;
                            var finalPosition = targetPosition.Extend(fromPosition, -pushDistance);
                            var finalPosition2 = targetPosition.Extend(fromPosition, -(pushDistance / 2f));
                            var underTurret = CondemnMenu["vaynemenu.condemn.condemnturret"].Cast<CheckBox>().CurrentValue &&
                                (finalPosition.To3D().UnderTurret(false) ||
                                 Helpers.IsFountain(finalPosition.To3D()));

                            if (finalPosition.IsWall() || finalPosition2.IsWall() || underTurret)
                            {
                                if (CondemnMenu["vaynemenu.condemn.onlystuncurrent"].Cast<CheckBox>().CurrentValue && !target.Equals(Orbwalker.GetTarget()))
                                {
                                    tg = null;
                                    return false;
                                }

                                if (target.Health + 10 <= ObjectManager.Player.GetAutoAttackDamage(target) * CondemnMenu["vaynemenu.condemn.condemn.noeaa"].Cast<Slider>().CurrentValue)
                                {
                                    tg = null;
                                    return false;
                                }

                                if (NavMesh.IsWallOfGrass(finalPosition.To3D(), 25) && CondemnMenu["vaynemenu.condemn.trinketbush"].Cast<CheckBox>().CurrentValue && TrinketSpell != null && TrinketSpell.IsReady())
                                {
                                    var wardPosition = Player.Instance.ServerPosition.Extend(finalPosition, ObjectManager.Player.ServerPosition.Distance(finalPosition) - 25f);
                                    Player.CastSpell(SpellSlot.Trinket, wardPosition.To3D());
                                }

                                tg = target;
                                return true;
                            }
                        }
                        break;
                    }
                #endregion

                #region Vayne Hunter Rework
                case 3:
                    {
                        foreach (var en in HeroManager.Enemies.Where(h => h.IsValidTarget(ESpell.Range) && !h.HasBuffOfType(BuffType.SpellShield) && !h.HasBuffOfType(BuffType.SpellImmunity)))
                        {
                            var ePred = Prediction.Position.PredictLinearMissile(en, ESpell.Range, (int)en.BoundingRadius, 250, 1250, -1);
                            int pushDist = CondemnMenu["vaynemenu.condemn.pushdistance"].Cast<Slider>().CurrentValue;
                            var finalPosition = ePred.UnitPosition.Extend(fromPosition, -pushDist);
                            for (int i = 0; i < pushDist; i += (int)en.BoundingRadius)
                            {
                                Vector3 loc3 =
                                    ePred.UnitPosition.To2D().Extend(fromPosition.To2D(), -i).To3D();
                                var orTurret = CondemnMenu["vaynemenu.condemn.condemnturret"].Cast<CheckBox>().CurrentValue && Helpers.UnderAllyTurret(loc3);
                                if (loc3.IsWall() || orTurret)
                                {
                                    if (CondemnMenu["vaynemenu.condemn.onlystuncurrent"].Cast<CheckBox>().CurrentValue && !en.Equals(Orbwalker.GetTarget()))
                                    {
                                        tg = null;
                                        return false;
                                    }

                                    if (en.Health + 10 <= ObjectManager.Player.GetAutoAttackDamage(en) * CondemnMenu["vaynemenu.condemn.condemn.noeaa"].Cast<Slider>().CurrentValue)
                                    {
                                        tg = null;
                                        return false;
                                    }

                                    if (NavMesh.IsWallOfGrass(finalPosition.To3D(), 25) && CondemnMenu["vaynemenu.condemn.trinketbush"].Cast<CheckBox>().CurrentValue && TrinketSpell != null && TrinketSpell.IsReady())
                                    {
                                        var wardPosition = Player.Instance.ServerPosition.Extend(finalPosition, ObjectManager.Player.ServerPosition.Distance(finalPosition) - 25f);
                                        Player.CastSpell(SpellSlot.Trinket, wardPosition.To3D());
                                    }

                                    tg = en;
                                    return true;
                                }
                            }
                        }
                        break;
                    }
                #endregion

                #region Shine
                case 4:
                    {
                        foreach (var target in HeroManager.Enemies.Where(h => h.IsValidTarget(ESpell.Range)))
                        {
                            var pushDistance = CondemnMenu["vaynemenu.condemn.pushdistance"].Cast<Slider>().CurrentValue;
                            var targetPosition = Prediction.Position.PredictLinearMissile(target, ESpell.Range, (int)target.BoundingRadius, 250, 1250, -1).UnitPosition;
                            var pushDirection = (targetPosition - ObjectManager.Player.ServerPosition).Normalized();
                            float checkDistance = pushDistance / 40f;

                            for (int i = 0; i < 40; i++)
                            {
                                Vector3 finalPosition = targetPosition + (pushDirection * checkDistance * i);
                                var collFlags = NavMesh.GetCollisionFlags(finalPosition);
                                var underTurret = CondemnMenu["vaynemenu.condemn.condemnturret"].Cast<CheckBox>().CurrentValue && (finalPosition.UnderTurret(false) || Helpers.IsFountain(finalPosition));
                                if (collFlags == CollisionFlags.Wall || collFlags == CollisionFlags.Building || underTurret)
                                {
                                    if (CondemnMenu["vaynemenu.condemn.onlystuncurrent"].Cast<CheckBox>().CurrentValue && !target.Equals(Orbwalker.GetTarget()))
                                    {
                                        tg = null;
                                        return false;
                                    }

                                    if (target.Health + 10 <= ObjectManager.Player.GetAutoAttackDamage(target) * CondemnMenu["vaynemenu.condemn.condemn.noeaa"].Cast<Slider>().CurrentValue)
                                    {
                                        tg = null;
                                        return false;
                                    }

                                    if (NavMesh.IsWallOfGrass(finalPosition, 25) && CondemnMenu["vaynemenu.condemn.trinketbush"].Cast<CheckBox>().CurrentValue && TrinketSpell != null && TrinketSpell.IsReady())
                                    {
                                        var wardPosition = Player.Instance.ServerPosition.Extend(finalPosition, ObjectManager.Player.ServerPosition.Distance(finalPosition) - 25f);
                                        Player.CastSpell(SpellSlot.Trinket, wardPosition.To3D());
                                    }

                                    tg = target;
                                    return true;
                                }
                            }
                        }
                        break;
                    }
                #endregion
            }
            tg = null;
            return false;
        }
    }
}
