using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;

namespace DravenPlus
{
    internal class Draven
    {
        private static int AxeCount
        {
            get
            {
                const string buffName = "dravenspinningattack";
                if (Player.HasBuff(buffName))
                {
                    return Player.Instance.Buffs.First(x => x.Name == buffName).Count + Axes.Count;
                }

                return 0;
            }
        }

        private static List<Axe> Axes { get; set; }

        private static Spell.Active Q { get; set; }

        private static Spell.Active W { get; set; }

        private static Spell.Skillshot E { get; set; }

        private static Spell.Skillshot R { get; set; }

        private static Circle AxeCatchRange { get; set; }

        private static Circle AxeLocation { get; set; }

        private static Circle ERange { get; set; }

        private static Menu DravenMenu { get; set; }

        private static readonly Dictionary<SubMenus, Menu> GetSubMenu = new Dictionary<SubMenus, Menu>(); 

        private static float LastAxeCatch { get; set; }


        public static void OnLoad()
        {
            if (Player.Instance.ChampionName != "Draven")
            {
                return;
            }

            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 1050, SkillShotType.Linear, 250, 1400, 130);
            R = new Spell.Skillshot(SpellSlot.R, 20000, SkillShotType.Linear, 400, 2000, 160);

            AxeCatchRange = new Circle();
            AxeLocation = new Circle();
            ERange = new Circle();

            Axes = new List<Axe>();

            CreateMenu();

            GameObject.OnCreate += GameObjectOnOnCreate;
            GameObject.OnDelete += GameObjectOnOnDelete;
            Interrupter.OnInterruptableSpell += InterrupterOnOnInterruptableSpell;
            Orbwalker.OnPreAttack += OrbwalkerOnOnPreAttack;
            Gapcloser.OnGapcloser += GapcloserOnOnGapcloser;
            Drawing.OnDraw += DrawingOnOnDraw;
            Game.OnTick += args =>
            {
                CatchAxe();
                Harass();
                LaneClear();
                Combo();
            };
        }

        private static void CreateMenu()
        {
            DravenMenu = MainMenu.AddMenu("Draven+", "dravenplus");

            var comboMenu = DravenMenu.AddSubMenu("Combo", "dravenplus.combo");
            comboMenu.AddGroupLabel("Combo");
            comboMenu.Add("dravenplus.combo.useq", new CheckBox("Use Q"));
            comboMenu.Add("dravenplus.combo.usew", new CheckBox("Use W"));
            comboMenu.Add("dravenplus.combo.usee", new CheckBox("Use E"));
            comboMenu.Add("dravenplus.combo.user", new CheckBox("Use R"));
            GetSubMenu.Add(SubMenus.Combo, comboMenu);

            var harassMenu = DravenMenu.AddSubMenu("Harass", "dravenplus.harass");
            harassMenu.AddGroupLabel("Harass");
            harassMenu.Add("dravenplus.harass.usee", new CheckBox("Use E"));
            harassMenu.Add("dravenplus.harass.mana", new Slider("Minimum Mana%", 30));
            GetSubMenu.Add(SubMenus.Harass, harassMenu);

            var clearMenu = DravenMenu.AddSubMenu("MinionClear", "dravenplus.clear");
            clearMenu.AddGroupLabel("MinionClear");
            clearMenu.Add("dravenplus.clear.useq", new CheckBox("Use Q"));
            clearMenu.Add("dravenplus.clear.usew", new CheckBox("Use W"));
            //clearMenu.Add("dravenplus.clear.usee", new CheckBox("Use E")); //todo need linefarmloc
            clearMenu.Add("dravenplus.clear.mana", new Slider("Minimum Mana%", 30));
            GetSubMenu.Add(SubMenus.LaneClear, clearMenu);

            var axeMenu = DravenMenu.AddSubMenu("Axe Settings", "dravenplus.axe");
            axeMenu.Add("dravenplus.axe.usew", new CheckBox("Use W if necessary"));
            axeMenu.Add("dravenplus.axe.undertower", new CheckBox("Do not Catch under Tower"));
            axeMenu.Add("dravenplus.axe.maxaxe", new Slider("Maximum Axes", 2, 1, 3));
            axeMenu.Add("dravenplus.axe.catchrange", new Slider("Catch Range", 800, 120, 1500));
            axeMenu.AddLabel("Catch Axes if:");
            var mode = axeMenu.Add("dravenplus.axe.mode", new Slider("Orbwalking", 1, 0, 2));
            mode.OnValueChange += (sender, args) =>
            {
                switch (args.NewValue)
                {
                    case 0:
                        {
                            mode.DisplayName = "Combo";
                            break;
                        }
                    case 1:
                        {
                            mode.DisplayName = "Orbwalking";
                            break;
                        }
                    case 2:
                        {
                            mode.DisplayName = "Always";
                            break;
                        }
                }
            };
            GetSubMenu.Add(SubMenus.AxeSettings, axeMenu);

            var drawingMenu = DravenMenu.AddSubMenu("Drawings", "dravenplus.draw");
            drawingMenu.AddGroupLabel("Drawings");
            drawingMenu.Add("dravenplus.draw.e", new CheckBox("Draw E"));
            drawingMenu.Add("dravenplus.draw.axe", new CheckBox("Draw Axes"));
            drawingMenu.Add("dravenplus.draw.catchrange", new CheckBox("Draw Catch Range"));
            GetSubMenu.Add(SubMenus.Drawings, drawingMenu);

            var otherMenu = DravenMenu.AddSubMenu("Other", "dravenplus.other");
            otherMenu.AddGroupLabel("Other");
            otherMenu.Add("dravenplus.other.antigap", new CheckBox("Use E as AntiGapCloser"));
            otherMenu.Add("dravenplus.other.interrupter", new CheckBox("Use E as Interrupter"));
            GetSubMenu.Add(SubMenus.Other, otherMenu);
        }

        private static void OrbwalkerOnOnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (!Q.IsReady())
            {
                return;
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if (GetSubMenu[SubMenus.Combo]["dravenplus.combo.useq"].Cast<CheckBox>().CurrentValue &&
                    AxeCount < GetSubMenu[SubMenus.AxeSettings]["dravenplus.axe.maxaxe"].Cast<Slider>().CurrentValue &&
                    target is AIHeroClient)
                {
                    Q.Cast();
                }
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                if (Player.Instance.ManaPercent < GetSubMenu[SubMenus.LaneClear]["dravenplus.clear.mana"].Cast<Slider>().CurrentValue)
                {
                    return;
                }

                if (GetSubMenu[SubMenus.LaneClear]["dravenplus.clear.useq"].Cast<CheckBox>().CurrentValue &&
                    AxeCount < GetSubMenu[SubMenus.AxeSettings]["dravenplus.axe.maxaxe"].Cast<Slider>().CurrentValue &&
                    (target as Obj_AI_Base).IsMinion)
                {
                    Q.Cast();
                }
            }
        }

        private static void InterrupterOnOnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs interruptableSpellEventArgs)
        {
            if (!GetSubMenu[SubMenus.Other]["dravenplus.other.interrupter"].Cast<CheckBox>().CurrentValue || 
                !E.IsReady() || 
                !sender.IsValidTarget(E.Range))
            {
                return;
            }

            if (interruptableSpellEventArgs.DangerLevel >= DangerLevel.Medium)
            {
                E.Cast(sender);
            }
        }

        private static void GapcloserOnOnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapcloserEventArgs)
        {
            if (!GetSubMenu[SubMenus.Other]["dravenplus.other.antigap"].Cast<CheckBox>().CurrentValue || 
                !E.IsReady() || 
                !sender.IsValidTarget(E.Range))
            {
                return;
            }

            E.Cast(sender);
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {
            if (GetSubMenu[SubMenus.Drawings]["dravenplus.draw.catchrange"].Cast<CheckBox>().CurrentValue)
            {
                AxeCatchRange.Color = Color.AntiqueWhite;
                AxeCatchRange.Radius =
                    GetSubMenu[SubMenus.AxeSettings]["dravenplus.axe.catchrange"].Cast<Slider>().CurrentValue;
                AxeCatchRange.Draw(Game.CursorPos);
            }

            if (GetSubMenu[SubMenus.Drawings]["dravenplus.draw.axe"].Cast<CheckBox>().CurrentValue)
            {
                var bestAxe = GetBestAxe;

                if (bestAxe != null)
                {
                    AxeLocation.Color = Color.Red;
                    AxeLocation.Radius = 120;
                    AxeLocation.Draw(bestAxe.Object.Position);
                }

                foreach (var axe in Axes.Where(x => x.Object.NetworkId != (bestAxe != null ? bestAxe.Object.NetworkId : 0)))
                {
                    AxeLocation.Color = Color.Yellow;
                    AxeLocation.Radius = 120;
                    AxeLocation.Draw(axe.Object.Position);
                }
            }

            if (GetSubMenu[SubMenus.Drawings]["dravenplus.draw.e"].Cast<CheckBox>().CurrentValue)
            {
                ERange.Color = Color.AntiqueWhite;
                ERange.Radius = E.Range;
                ERange.Draw(Player.Instance.Position);
            }
        }

        private static void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                return;
            }

            Axes.Add(new Axe
            {
                Object = sender,
                ExpireTime = Game.Time + 1.8
            });

            Core.DelayAction(() => Axes.RemoveAll(x => x.Object.NetworkId == sender.NetworkId), 1800);
        }

        private static void GameObjectOnOnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                return;
            }

            Axes.RemoveAll(x => x.Object.NetworkId == sender.NetworkId);
        }

        private static void Combo()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                return;
            }

            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (GetSubMenu[SubMenus.Combo]["dravenplus.combo.usew"].Cast<CheckBox>().CurrentValue &&
                W.IsReady() &&
                !Player.HasBuff("dravenfurybuff"))
            {

                W.Cast();
            }

            if (GetSubMenu[SubMenus.Combo]["dravenplus.combo.usee"].Cast<CheckBox>().CurrentValue
                && E.IsReady())
            {
                E.Cast(target);
            }

            if (GetSubMenu[SubMenus.Combo]["dravenplus.combo.user"].Cast<CheckBox>().CurrentValue &&
                R.IsReady())
            {
                var targetR =
                    HeroManager.Enemies.Where(h => h.IsValidTarget(2000))
                        .FirstOrDefault(
                            h =>
                                GetSpellDamage(h, SpellSlot.R) * 2 > h.Health
                                &&
                                (!Player.Instance.IsInAutoAttackRange(h) ||
                                 Player.Instance.CountEnemiesInRange(E.Range) > 2));

                if (targetR != null)
                {
                    R.Cast(targetR);
                }
            }
        }

        private static void Harass()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                return;
            }

            if (Player.Instance.ManaPercent < GetSubMenu[SubMenus.Harass]["dravenplus.harass.mana"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (GetSubMenu[SubMenus.Harass]["dravenplus.harass.usee"].Cast<CheckBox>().CurrentValue && 
                E.IsReady())
            {
                E.Cast(target);
            }
        }

        private static void LaneClear()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                return;
            }

            if (Player.Instance.ManaPercent < GetSubMenu[SubMenus.LaneClear]["dravenplus.clear.mana"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            if (GetSubMenu[SubMenus.LaneClear]["dravenplus.clear.usew"].Cast<CheckBox>().CurrentValue && 
                W.IsReady() &&
                !Player.HasBuff("dravenfurybuff"))
            {
                W.Cast();
            }
        }

        private static void CatchAxe()
        {
            var mode = GetSubMenu[SubMenus.AxeSettings]["dravenplus.axe.mode"].Cast<Slider>().CurrentValue;
            if (mode == 0 && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                return;
            }
            if (mode == 1 && Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.None)
            {
                return;
            }

            if (Game.Time - LastAxeCatch < 0.05)
            {
                return;
            }

            var bestAxe = GetBestAxe;

            if (bestAxe != null && bestAxe.Object.Position.Distance(Player.Instance.ServerPosition) > 110)
            {
                var catchTime = Player.Instance.Distance(bestAxe.Object.Position)/Player.Instance.MoveSpeed;
                var expireTime = bestAxe.ExpireTime - Game.Time;

                if (catchTime >= expireTime &&
                    GetSubMenu[SubMenus.AxeSettings]["dravenplus.axe.usew"].Cast<CheckBox>().CurrentValue)
                {
                    W.Cast();
                }

                if (GetSubMenu[SubMenus.AxeSettings]["dravenplus.axe.undertower"].Cast<CheckBox>().CurrentValue)
                {
                    if (IsUnderTurret(Player.Instance.ServerPosition) && IsUnderTurret(bestAxe.Object.Position))
                    {
                        LastAxeCatch = Game.Time;

                        Orbwalker.OrbwalkTo(bestAxe.Object.Position);
                    }
                    else if (!IsUnderTurret(bestAxe.Object.Position))
                    {
                        LastAxeCatch = Game.Time;

                        Orbwalker.OrbwalkTo(bestAxe.Object.Position);
                    }
                }
                else
                {
                    LastAxeCatch = Game.Time;

                    Orbwalker.OrbwalkTo(bestAxe.Object.Position);
                }
            }
            else
            {
                Orbwalker.OrbwalkTo(Game.CursorPos);
            }
        }

        private static float GetSpellDamage(Obj_AI_Base target, SpellSlot slot)
        {
            var level = Player.GetSpell(slot).Level - 1;
            switch (slot)
            {
                case SpellSlot.Q:
                {
                    var damage = new float[] {45, 55, 65, 75, 85}[level]/100*
                                 (Player.Instance.BaseAttackDamage + Player.Instance.FlatPhysicalDamageMod);
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Physical, damage);
                }
                case SpellSlot.E:
                {
                    var damage = new float[] { 70, 105, 140, 175, 210 }[level] + (float)(0.5 * Player.Instance.FlatPhysicalDamageMod);
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Physical, damage);
                }
                case SpellSlot.R:
                {
                    var damage = new float[] { 175, 275, 375 }[level] + (float)(1.1 * Player.Instance.FlatPhysicalDamageMod);
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Physical, damage);
                }
            }

            return 0;
        }

        private static Axe GetBestAxe
        {
            get
            {
                return
                    Axes.Where(
                        h =>
                            h.Object.Position.Distance(Game.CursorPos) <=
                            GetSubMenu[SubMenus.AxeSettings]["dravenplus.axe.catchrange"].Cast<Slider>().CurrentValue)
                        .
                        OrderBy(h => h.Object.Position.Distance(Player.Instance.ServerPosition)).
                        ThenBy(x => x.Object.Distance(Game.CursorPos)).
                        FirstOrDefault();
            }
        }

        public static bool IsUnderTurret(Vector3 position)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(turret => turret.IsValidTarget(950) && turret.IsEnemy);
        }

        public class Axe
        {
            public double ExpireTime { get; set; }

            public GameObject Object { get; set; }
        }

        enum SubMenus
        {
            Combo,
            LaneClear,
            Harass,
            AxeSettings,
            Drawings,
            Other
        }
    }
}