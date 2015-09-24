using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace CassiopeiaPlus
{
    internal class Cassiopeia
    {
        private static readonly Spell Q = new Spell(SpellSlot.Q, 850);
        private static readonly Spell W = new Spell(SpellSlot.W, 850);
        private static readonly Spell E = new Spell(SpellSlot.E, 700);
        private static readonly Spell R = new Spell(SpellSlot.R, 800);
        private static Circle QCircle { get; set; }
        private static Circle ECircle { get; set; }
        private static Circle WCircle { get; set; }
        private static Circle RCircle { get; set; }
        private static Menu CassioMenu;
        private static Menu ComboCassioMenu;
        private static Menu HarassCassioMenu;
        private static Menu ClearCassioMenu;
        private static Menu LasthitCassioMenu;
        private static Menu DrawingsCassioMenu;
        private static Menu OtherCassioMenu;

        public static void OnLoad()
        {
            if (Player.Instance.ChampionName != "Cassiopeia")
            {
                return;
            }

            //SetSkillshots
            Q.SetSkillshot(600, 150, -1, Spell.SkillshotType.Circular);
            W.SetSkillshot(500, 250, 2500, Spell.SkillshotType.Circular);
            E.SetTargetted(200, -1);
            R.SetSkillshot(600, (int)(80*Math.PI/180), -1, Spell.SkillshotType.Cone);
            
            //Menu
            BuildMenu(); 

            //Circles
            QCircle = new Circle
            {
                Color = Color.AntiqueWhite,
                Radius = Q.Range
            };
            ECircle = new Circle
            {
                Color = Color.AntiqueWhite,
                Radius = E.Range
            };
            WCircle = new Circle
            {
                Color = Color.AntiqueWhite,
                Radius = W.Range
            };
            RCircle = new Circle
            {
                Color = Color.AntiqueWhite,
                Radius = R.Range
            };

            //Events
            Drawing.OnDraw += OnDraw;
            Game.OnTick += OnTick;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Interrupter.OnInterruptableSpell += InterrupterOnOnInterruptableSpell;
            Spellbook.OnCastSpell += SpellbookOnOnCastSpell;
        }

        private static void SpellbookOnOnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot != SpellSlot.R || !ComboCassioMenu["cassioplus.combo.keyr"].Cast<KeyBind>().CurrentValue)
            {
                return;
            }

            args.Process = false;
        }

        private static void InterrupterOnOnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs interruptableSpellEventArgs)
        {
            if (!R.Instance.IsReady ||
                interruptableSpellEventArgs.DangerLevel < (DangerLevel)OtherCassioMenu["cassioplus.other.interrupterlevel"].Cast<Slider>().CurrentValue ||
                !OtherCassioMenu["cassioplus.other.interrupter"].Cast<CheckBox>().CurrentValue || !sender.IsFacing(Player.Instance))
            {
                return;
            }

            R.Cast(sender);
        }

        private static void LastHitting()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                return;
            }

            if (Player.Instance.ManaPercent < LasthitCassioMenu["cassioplus.lasthit.mana"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            if (Q.Instance.IsReady && LasthitCassioMenu["cassioplus.lasthit.q"].Cast<CheckBox>().CurrentValue)
            {
                var minions =
                    EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition.To2D(), Q.Range)
                        .Where(h => !h.HasBuffOfType(BuffType.Poison))
                        .ToList();
                var farmLocation = GetCircularFarmLocation(minions.ToArray(), Q);
                if (farmLocation.MinionsHit >= 3)
                {
                    Player.CastSpell(Q.Slot, farmLocation.Position);
                }
            }

            if (W.Instance.IsReady && LasthitCassioMenu["cassioplus.lasthit.w"].Cast<CheckBox>().CurrentValue)
            {
                var minions =
                    EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition.To2D(), W.Range)
                        .Where(h => !h.HasBuffOfType(BuffType.Poison))
                        .ToList();

                var farmLocation = GetCircularFarmLocation(minions.ToArray(), W);
                if (farmLocation.MinionsHit >= 3)
                {
                    Player.CastSpell(W.Slot, farmLocation.Position);
                }
            }

            if (E.Instance.IsReady && LasthitCassioMenu["cassioplus.lasthit.e"].Cast<CheckBox>().CurrentValue)
            {
                var target = EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                    Player.Instance.ServerPosition.To2D(), E.Range)
                    .Where(
                        h =>
                            h.HasBuffOfType(BuffType.Poison) &&
                            h.Health < GetSpellDamage(SpellSlot.Q, h) &&
                            GetPoisonBuffEndTime(h) < Game.Time + E.Delay && h.HasBuffOfType(BuffType.Poison) &&
                            h.IsTargetable && Prediction.Health.GetPrediction(h, (int)E.Delay) > 0)
                    .OrderBy(h => h.Health)
                    .FirstOrDefault();

                if (target != null)
                {
                    Player.CastSpell(E.Slot, target);
                }
            }
        }

        private static void Combo()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                return;
            }

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target == null)
            {
                return;
            }

            if (Q.Instance.IsReady && ComboCassioMenu["cassioplus.combo.useq"].Cast<CheckBox>().CurrentValue)
            {
                var pred = Q.GetPrediction(target);
                if (pred.HitChancePercent >= ComboCassioMenu["cassioplus.combo.hitchanceq"].Cast<Slider>().CurrentValue)
                {
                    Q.Cast(pred.CastPosition);
                }
            }
            if (W.Instance.IsReady && ComboCassioMenu["cassioplus.combo.usew"].Cast<CheckBox>().CurrentValue)
            {
                var pred = W.GetPrediction(target);
                if (pred.HitChancePercent >= ComboCassioMenu["cassioplus.combo.hitchancew"].Cast<Slider>().CurrentValue)
                {
                    W.Cast(pred.CastPosition);
                }
            }

            if (ComboCassioMenu["cassioplus.combo.usee"].Cast<CheckBox>().CurrentValue)
            {
                if (GetPoisonBuffEndTime(target) < Game.Time + E.Delay && target.HasBuffOfType(BuffType.Poison))
                {
                    E.Cast(target);
                }
                else if (Player.Instance.ServerPosition.Distance(target) > E.Range ||
                         GetPoisonBuffEndTime(target) >= Game.Time + E.Delay &&
                            ComboCassioMenu["cassioplus.combo.changete"].Cast<CheckBox>().CurrentValue)
                {
                    var targetE = HeroManager.Enemies
                        .Where(
                            h =>
                                h.IsValid && !h.IsInvulnerable &&
                                h.ServerPosition.Distance(Player.Instance.ServerPosition) < E.Range &&
                                GetPoisonBuffEndTime(h) < Game.Time + E.Delay &&
                                target.HasBuffOfType(BuffType.Poison))
                        .OrderBy(h => h.Health)
                        .OrderBy(h => TargetSelector.GetPriority(h)).FirstOrDefault();
                    if (targetE != null)
                    {
                        E.Cast(targetE);
                    }
                }
            }

            if (R.Instance.IsReady && ComboCassioMenu["cassioplus.combo.user"].Cast<CheckBox>().CurrentValue)
            {
                if (ComboDmg(target) >= target.Health || (target.IsFacing(Player.Instance) && 
                    ComboDmg(target) + (GetSpellDamage(SpellSlot.E, target)*3) >= target.Health))
                {
                    var result = R.GetPrediction(target);
                    if (result.HitChancePercent >= ComboCassioMenu["cassioplus.combo.hitchancer"].Cast<Slider>().CurrentValue)
                    {
                        R.Cast(result.CastPosition);
                    }
                }
                else
                {
                    var heros = HeroManager.Enemies.Where(h => h.IsValidTarget(R.Range)).ToArray();
                    var pred = R.GetPredictionAoe(heros).OrderBy(h => h.CollisionObjects.Count(x => x is AIHeroClient)).FirstOrDefault();
                    if (pred.CollisionObjects.Count(x => x is AIHeroClient) >=
                        ComboCassioMenu["cassioplus.combo.minhitr"].Cast<Slider>().CurrentValue)
                    {
                        R.Cast(pred.CastPosition);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                return;
            }
            if (Player.Instance.ManaPercent < HarassCassioMenu["cassioplus.harass.mana"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target == null)
            {
                return;
            }

            if (Q.Instance.IsReady && HarassCassioMenu["cassioplus.harass.q"].Cast<CheckBox>().CurrentValue)
            {
                var pred = Q.GetPrediction(target);
                if (pred.HitChance >= HitChance.High)
                {
                    Q.Cast(pred.CastPosition);
                }
            }

            if (W.Instance.IsReady && HarassCassioMenu["cassioplus.harass.q"].Cast<CheckBox>().CurrentValue)
            {
                var pred = W.GetPrediction(target);
                if (pred.HitChance >= HitChance.High)
                {
                    W.Cast(pred.CastPosition);
                }
            }

            if (E.Instance.IsReady && HarassCassioMenu["cassioplus.harass.q"].Cast<CheckBox>().CurrentValue)
            {
                if (GetPoisonBuffEndTime(target) < Game.Time + E.Delay &&
                    target.HasBuffOfType(BuffType.Poison))
                {
                    Player.Instance.Spellbook.CastSpell(E.Slot, target);
                }
                else if (Player.Instance.ServerPosition.Distance(target) > E.Range ||
                         GetPoisonBuffEndTime(target) >= Game.Time + E.Delay)
                {
                    var targetE = ObjectManager.Get<AIHeroClient>()
                        .Where(
                            h =>
                                h.IsValid && !h.IsInvulnerable && h.IsEnemy &&
                                h.ServerPosition.Distance(Player.Instance.ServerPosition) < E.Range &&
                                GetPoisonBuffEndTime(h) < Game.Time + E.Delay &&
                                target.HasBuffOfType(BuffType.Poison))
                        .OrderBy(h => h.Health)
                        .OrderBy(h => TargetSelector.GetPriority(h)).FirstOrDefault();
                    if (targetE != null)
                    {
                        E.Cast(targetE);
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                return;
            }
            if (Player.Instance.ManaPercent < ClearCassioMenu["cassioplus.jclear.mana"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            if (Q.Instance.IsReady && ClearCassioMenu["cassioplus.jclear.q"].Cast<CheckBox>().CurrentValue)
            {
                var minion =
                    EntityManager.GetJungleMonsters(Player.Instance.ServerPosition.To2D(), Q.Range)
                        .OrderByDescending(h => h.Health)
                        .FirstOrDefault();
                if (minion != null)
                {
                    Q.Cast(minion);
                }
            }

            if (W.Instance.IsReady && ClearCassioMenu["cassioplus.jclear.q"].Cast<CheckBox>().CurrentValue)
            {
                var minion =
                    EntityManager.GetJungleMonsters(Player.Instance.ServerPosition.To2D(), W.Range)
                        .OrderByDescending(h => h.Health)
                        .FirstOrDefault();
                if (minion != null)
                {
                    W.Cast(minion);
                }
            }

            if (E.Instance.IsReady && ClearCassioMenu["cassioplus.jclear.q"].Cast<CheckBox>().CurrentValue)
            {
                var minion =
                    EntityManager.GetJungleMonsters(Player.Instance.ServerPosition.To2D(), E.Range)
                        .Where(
                            h =>
                                GetPoisonBuffEndTime(h) < Game.Time + E.Delay &&
                                h.HasBuffOfType(BuffType.Poison))
                        .OrderByDescending(h => h.Health)
                        .FirstOrDefault();
                if (minion != null)
                {
                    E.Cast(minion);
                }
            }
        }

        private static void LaneClear()
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                return;
            }
            if (Player.Instance.ManaPercent < ClearCassioMenu["cassioplus.clear.mana"].Cast<Slider>().CurrentValue)
            {
                return;
            }

            if (Q.Instance.IsReady && ClearCassioMenu["cassioplus.clear.q"].Cast<CheckBox>().CurrentValue)
            {
                var minions =
                    EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition.To2D(), Q.Range);

                var farmLocation = GetCircularFarmLocation(minions.ToArray(), Q);
                if (farmLocation.MinionsHit >= 3)
                {
                    Q.Cast(farmLocation.Position);
                }
            }

            if (W.Instance.IsReady && ClearCassioMenu["cassioplus.clear.w"].Cast<CheckBox>().CurrentValue)
            {
                var minions =
                    EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition.To2D(), W.Range).ToList();

                var farmLocation = GetCircularFarmLocation(minions.ToArray(), W);
                if (farmLocation.MinionsHit >= 3)
                {
                    W.Cast(farmLocation.Position);
                }
            }

            if (E.Instance.IsReady && ClearCassioMenu["cassioplus.clear.e"].Cast<CheckBox>().CurrentValue)
            {
                if (ClearCassioMenu["cassioplus.clear.laste"].Cast<CheckBox>().CurrentValue)
                {
                    var target = EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition.To2D(), E.Range)
                        .Where(
                            h =>
                                h.HasBuffOfType(BuffType.Poison) &&
                                GetPoisonBuffEndTime(h) < Game.Time + E.Delay &&
                                h.HasBuffOfType(BuffType.Poison) &&
                                h.IsTargetable &&
                                Prediction.Health.GetPrediction(h, E.Delay*2) <= GetSpellDamage(SpellSlot.E, h) &&
                                Prediction.Health.GetPrediction(h, E.Delay*2 + (int)Player.Instance.AttackCastDelay) > 0)
                        .OrderBy(h => h.Health)
                        .FirstOrDefault();

                    if (target != null)
                    {
                        E.Cast(target);
                    }
                }
                else
                {
                    var target = EntityManager.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.ServerPosition.To2D(), E.Range)
                        .Where(
                            h =>
                                h.HasBuffOfType(BuffType.Poison) &&
                                GetPoisonBuffEndTime(h) < Game.Time + E.Delay &&
                                h.HasBuffOfType(BuffType.Poison) &&
                                h.IsTargetable &&
                                (Prediction.Health.GetPrediction(h, E.Delay*2) < GetSpellDamage(SpellSlot.E, h) ||
                                 Prediction.Health.GetPrediction(h, E.Delay*2) >=
                                 Damage.GetAutoAttackDamage(Player.Instance, h)))
                        .OrderBy(h => h.Health)
                        .FirstOrDefault();

                    if (target != null)
                    {
                        E.Cast(target);
                    }
                }
            }
        }

        private static double GetSpellDamage(SpellSlot slot, Obj_AI_Base target)
        {
            switch (slot)
            {
                case SpellSlot.Q:
                {
                    var spellLevel = Player.GetSpell(slot).Level;
                    var dmg = new[] {75, 115, 155, 195, 235}[spellLevel - 1] + Player.Instance.TotalMagicalDamage*0.45;
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Magical, (float) dmg);
                }
                case SpellSlot.W:
                {
                    var spellLevel = Player.GetSpell(slot).Level;
                    var dmg = new[] {90, 135, 180, 225, 270}[spellLevel - 1] + Player.Instance.TotalMagicalDamage*0.9;
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Magical, (float)dmg);
                }
                case SpellSlot.E:
                {
                    var spellLevel = Player.GetSpell(slot).Level;
                    var dmg = new[] {55, 80, 105, 130, 155}[spellLevel - 1] + Player.Instance.TotalMagicalDamage*0.55;
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Magical, (float)dmg);
                }
                case SpellSlot.R:
                {
                    var spellLevel = Player.GetSpell(slot).Level;
                    var dmg = new[] {150, 250, 350}[spellLevel - 1] + Player.Instance.TotalMagicalDamage*0.5;
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Magical, (float)dmg);
                }
            }

            return 0;
        }

        private static float ComboDmg(Obj_AI_Base enemy)
        {
            var qDmg = Q.Instance.IsReady ? GetSpellDamage(SpellSlot.Q, enemy) : 0;
            var wDmg = W.Instance.IsReady ? GetSpellDamage(SpellSlot.W, enemy) : 0;
            var eDmg = E.Instance.IsReady ? GetSpellDamage(SpellSlot.E, enemy) : 0;
            var rDmg = R.Instance.IsReady ? GetSpellDamage(SpellSlot.R, enemy) : 0;
            var dmg = 0d;

            dmg += qDmg;
            dmg += wDmg;
            dmg += eDmg*3;

            if (R.Instance.IsReady)
            {
                dmg += qDmg;
                dmg += eDmg;
                dmg += rDmg;
            }

            return (float) dmg;
        }

        private static void BuildMenu()
        {
            CassioMenu = MainMenu.AddMenu("Cassiopeia+", "cassioplus");

            ComboCassioMenu = CassioMenu.AddSubMenu("Combo", "cassoplus.combo");
            ComboCassioMenu.AddGroupLabel("Combo");
            ComboCassioMenu.AddLabel("Q Settings");
            ComboCassioMenu.Add("cassioplus.combo.useq", new CheckBox("Use Q"));
            ComboCassioMenu.Add("cassioplus.combo.hitchanceq", new Slider("Q HitChance %", 75));
            CassioMenu.AddSeparator();
            ComboCassioMenu.AddLabel("W Settings");
            ComboCassioMenu.Add("cassioplus.combo.usew", new CheckBox("Use W"));
            ComboCassioMenu.Add("cassioplus.combo.hitchancew", new Slider("W HitChance %", 75));
            CassioMenu.AddSeparator();
            ComboCassioMenu.AddLabel("E Settings");
            ComboCassioMenu.Add("cassioplus.combo.usee", new CheckBox("Use E"));
            ComboCassioMenu.Add("cassioplus.combo.changete", new CheckBox("Change E Target if Main Target has no PoisonBuff"));
            CassioMenu.AddSeparator();
            ComboCassioMenu.AddLabel("R Settings");
            ComboCassioMenu.Add("cassioplus.combo.user", new CheckBox("Use R"));
            ComboCassioMenu.Add("cassioplus.combo.hitchancer", new Slider("R HitChance %", 75));
            ComboCassioMenu.Add("cassioplus.combo.minhitr", new Slider("MinimumHit by R", 3, 1, 5));
            ComboCassioMenu.Add("cassioplus.combo.keyr", new KeyBind("Assisted Ult", false, KeyBind.BindTypes.HoldActive, 'R'));
            CassioMenu.AddSeparator();
            ComboCassioMenu.AddLabel("Other Settings");
            ComboCassioMenu.Add("cassioplus.combo.disableaa", new CheckBox("Disable AA while Casting E"));

            HarassCassioMenu = CassioMenu.AddSubMenu("Harass", "cassoplus.harass");
            HarassCassioMenu.AddGroupLabel("Harass");
            HarassCassioMenu.Add("cassioplus.harass.q", new CheckBox("Use Q"));
            HarassCassioMenu.Add("cassioplus.harass.w", new CheckBox("Use W"));
            HarassCassioMenu.Add("cassioplus.harass.e", new CheckBox("Use E"));
            HarassCassioMenu.Add("cassioplus.harass.mana", new Slider("Minimum Mana%", 30));

            ClearCassioMenu = CassioMenu.AddSubMenu("MinionClear", "cassoplus.clear");
            ClearCassioMenu.AddGroupLabel("LaneClear");
            ClearCassioMenu.Add("cassioplus.clear.q", new CheckBox("Use Q"));
            ClearCassioMenu.Add("cassioplus.clear.w", new CheckBox("Use W"));
            ClearCassioMenu.Add("cassioplus.clear.e", new CheckBox("Use E"));
            ClearCassioMenu.Add("cassioplus.clear.laste", new CheckBox("Only LastHit with E", false));
            ClearCassioMenu.Add("cassioplus.clear.mana", new Slider("Minimum Mana%", 30));

            ClearCassioMenu.AddSeparator();
            ClearCassioMenu.AddGroupLabel("JungleClear");
            ClearCassioMenu.Add("cassioplus.jclear.q", new CheckBox("Use Q"));
            ClearCassioMenu.Add("cassioplus.jclear.w", new CheckBox("Use W"));
            ClearCassioMenu.Add("cassioplus.jclear.e", new CheckBox("Use E"));
            ClearCassioMenu.Add("cassioplus.jclear.mana", new Slider("Minimum Mana%", 30));

            LasthitCassioMenu = CassioMenu.AddSubMenu("LastHit", "cassoplus.lasthit");
            LasthitCassioMenu.AddGroupLabel("LastHit");
            LasthitCassioMenu.Add("cassioplus.lasthit.q", new CheckBox("Use Q", false));
            LasthitCassioMenu.Add("cassioplus.lasthit.W", new CheckBox("Use Q", false));
            LasthitCassioMenu.Add("cassioplus.lasthit.e", new CheckBox("Use E", false));
            LasthitCassioMenu.Add("cassioplus.lasthit.mana", new Slider("Minimum Mana%", 30));

            DrawingsCassioMenu = CassioMenu.AddSubMenu("Drawings", "cassoplus.drawings");
            DrawingsCassioMenu.AddGroupLabel("Drawings");
            DrawingsCassioMenu.Add("cassioplus.draw.q", new CheckBox("Draw Q"));
            DrawingsCassioMenu.Add("cassioplus.draw.w", new CheckBox("Draw W"));
            DrawingsCassioMenu.Add("cassioplus.draw.e", new CheckBox("Draw E"));
            DrawingsCassioMenu.Add("cassioplus.draw.r", new CheckBox("Draw R"));

            OtherCassioMenu = CassioMenu.AddSubMenu("Other", "cassoplus.other");
            OtherCassioMenu.AddGroupLabel("Other");
            OtherCassioMenu.Add("cassioplus.other.interrupter", new CheckBox("Use Interrupter (R)"));
            OtherCassioMenu.AddLabel("Minimum Interrupter DangerLevel");
            var level = OtherCassioMenu.Add("cassioplus.other.interrupterlevel", new Slider("High", 2, 0, 2));
            level.OnValueChange += (sender, args) =>
            {
                switch (args.NewValue)
                {
                    case 0:
                    {
                        level.DisplayName = "Low";
                        break;
                    }
                    case 1:
                    {
                        level.DisplayName = "Medium";
                        break;
                    }
                    case 2:
                    {
                        level.DisplayName = "High";
                        break;
                    }
                }
            };
        }

        private static void OnDraw(EventArgs args)
        {
            //Spell Ranges
            if (DrawingsCassioMenu["cassioplus.draw.q"].Cast<CheckBox>().CurrentValue)
            {
                QCircle.Draw(Player.Instance.Position);
            }
            if (DrawingsCassioMenu["cassioplus.draw.e"].Cast<CheckBox>().CurrentValue)
            {
                ECircle.Draw(Player.Instance.Position);
            }
            if (DrawingsCassioMenu["cassioplus.draw.w"].Cast<CheckBox>().CurrentValue)
            {
                WCircle.Draw(Player.Instance.Position);
            }
            if (DrawingsCassioMenu["cassioplus.draw.r"].Cast<CheckBox>().CurrentValue)
            {
                RCircle.Draw(Player.Instance.Position);
            }
        }

        private static void OnTick(EventArgs args)
        {
            Combo();
            LastHitting();
            LaneClear();
            JungleClear();
            Harass();

            if (ComboCassioMenu["cassioplus.combo.keyr"].Cast<KeyBind>().CurrentValue)
            {
                CastAssistedUlt();
            }
        }

        private static void CastAssistedUlt()
        {
            if (!R.Instance.IsReady)
            {
                return;
            }

            var heros = HeroManager.Enemies.Where(h => h.IsValidTarget(R.Range)).ToArray();
            if (!heros.Any())
            {
                return;
            }

            var pred = R.GetPredictionAoe(heros).OrderBy(h => h.CollisionObjects.Count(x => x is AIHeroClient)).FirstOrDefault();
            if (pred == null)
            {
                return;
            }

            if (pred.CollisionObjects.Count(x => x is AIHeroClient) >= ComboCassioMenu["cassioplus.combo.minhitr"].Cast<Slider>().CurrentValue)
            {
                Console.WriteLine("cast");
                R.Cast(pred.CastPosition);
            }
        }

        private static FarmLocation GetCircularFarmLocation(Obj_AI_Base[] units, Spell spell)
        {
            if (!units.Any() || units == null)
            {
                return new FarmLocation
                {
                    MinionsHit = 0,
                    Position = new Vector3()
                };
            }

            var farmlocation =
                Prediction.Position.PredictCircularMissileAoe(units, spell.Range, spell.Width, spell.Delay, spell.Speed)
                    .OrderBy(h => h.CollisionObjects.Count(x => x.IsMinion)).FirstOrDefault();

            return new FarmLocation
            {
                MinionsHit = farmlocation.CollisionObjects.Count(h => h.IsMinion),
                Position = farmlocation.CastPosition
            };
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo && !E.Instance.IsReady &&
                ComboCassioMenu["cassioplus.combo.disableaa"].Cast<CheckBox>().CurrentValue)
            {
                args.Process = false;
            }
            //braumbuff //lichbane
        }

        public static float GetPoisonBuffEndTime(Obj_AI_Base target)
        {
            var buffEndTime = target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Type == BuffType.Poison)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();
            return buffEndTime;
        }
    }

    public class FarmLocation
    {
        public int MinionsHit { get; set; }
        public Vector3 Position { get; set; }
    }
}
