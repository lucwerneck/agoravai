using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Font = EloBuddy.SDK.Rendering.Text;
using Sprite = EloBuddy.SDK.Rendering.Sprite;

namespace BaseUltPlus
{
    public class OfficialAddon
    {
        private static readonly List<Recall> Recalls = new List<Recall>();
        private static readonly List<BaseUltUnit> BaseUltUnits = new List<BaseUltUnit>();
        private static readonly List<BaseUltSpell> BaseUltSpells = new List<BaseUltSpell>();
        private static readonly AIHeroClient Player = ObjectManager.Player;
        private static Font Text;
        private static Sprite Hud;
        private static Sprite Bar;
        private static Sprite Warning;
        private static Sprite Underline;
        private static int X = (int) TacticalMap.X- 20;
        private static int Y = (int) TacticalMap.Y - 200;
        private const int Length = 260;
        private const int Height = 25;
        private const int LineThickness = 4;

        public static void Initialize()
        {
            Program.BaseUltMenu["x"].Cast<Slider>().OnValueChange += OffsetOnOnValueChange;
            Program.BaseUltMenu["y"].Cast<Slider>().OnValueChange += OffsetOnOnValueChange;
            UpdateOffset(Program.BaseUltMenu["x"].Cast<Slider>().CurrentValue, Program.BaseUltMenu["y"].Cast<Slider>().CurrentValue);

            Text = new EloBuddy.SDK.Rendering.Text("", new FontDescription
            {
                FaceName = "Calibri",
                Height = (Height/30)*23,
                OutputPrecision = FontPrecision.Default,
                Quality = FontQuality.ClearType
            });

            Hud = new Sprite(Texture.FromMemory(
                Drawing.Direct3DDevice, (byte[]) new ImageConverter().ConvertTo(Resources.baseulthud, typeof (byte[])),
                285,
                44, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0));

            Bar = new Sprite(Texture.FromMemory(
                Drawing.Direct3DDevice, (byte[]) new ImageConverter().ConvertTo(Resources.bar, typeof (byte[])), 260,
                66, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0));

            Warning = new Sprite(Texture.FromMemory(
                Drawing.Direct3DDevice, (byte[]) new ImageConverter().ConvertTo(Resources.warning, typeof (byte[])), 40,
                40, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0));

            Underline = new Sprite(Texture.FromMemory(
                Drawing.Direct3DDevice,
                (byte[]) new ImageConverter().ConvertTo(Resources.underline_red, typeof (byte[])), 355,
                89, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0));

            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                Recalls.Add(new Recall(hero, RecallStatus.Inactive));
            }

            {
                BaseUltSpells.Add(new BaseUltSpell("Ezreal", SpellSlot.R, 1000, 2000, 160, false));
                BaseUltSpells.Add(new BaseUltSpell("Jinx", SpellSlot.R, 600, 1700, 140, true));
                BaseUltSpells.Add(new BaseUltSpell("Ashe", SpellSlot.R, 250, 1600, 130, true));
                BaseUltSpells.Add(new BaseUltSpell("Draven", SpellSlot.R, 400, 2000, 160, true));
                BaseUltSpells.Add(new BaseUltSpell("Karthus", SpellSlot.R, 3125, 0, 0, false));
                BaseUltSpells.Add(new BaseUltSpell("Ziggs", SpellSlot.Q, 250, 3100, 0, false));
                BaseUltSpells.Add(new BaseUltSpell("Lux", SpellSlot.R, 1375, 0, 0, false));
                BaseUltSpells.Add(new BaseUltSpell("Xerath", SpellSlot.R, 700, 600, 0, false));
            }
        }

        private static void OffsetOnOnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            if (sender.SerializationId == Program.BaseUltMenu["x"].Cast<Slider>().SerializationId)
            {
                UpdateOffset(args.NewValue);
            }
            else
            {
                UpdateOffset(0, args.NewValue);
            }
        }

        private static void UpdateOffset(int x, int y = 0)
        {
            X = (int)TacticalMap.X + (-20 + x);
            Y = (int)TacticalMap.Y + (-200 + y);
        }

        public static void OnUpdate()
        {
            foreach (var recall in Recalls)
            {
                if (recall.Status != RecallStatus.Inactive)
                {
                    var recallDuration = recall.Duration;
                    var cd = recall.Started + recallDuration - Game.Time;
                    var percent = (cd > 0 && Math.Abs(recallDuration) > float.Epsilon)
                        ? 1f - (cd/recallDuration)
                        : 1f;
                    var textLength = (recall.Unit.ChampionName.Length + 6)*7;
                    var myLength = percent*Length;
                    var freeSpaceLength = myLength + textLength;
                    var freeSpacePercent = freeSpaceLength/Length > 1 ? 1 : freeSpaceLength/Length;

                    if (
                        Recalls.Any(
                            h =>
                                GetRecallPercent(h) > percent && GetRecallPercent(h) < freeSpacePercent &&
                                h.TextPos == recall.TextPos && recall.Started > h.Started))
                    {
                        recall.TextPos += 1;
                    }
                    if (recall.Status == RecallStatus.Finished &&
                        Recalls.Any(
                            h =>
                                h.Started > recall.Started && h.TextPos == recall.TextPos &&
                                recall.Started + 3 < h.Started + recall.Duration))
                    {
                        recall.TextPos += 1;
                    }
                }

                if (recall.Status == RecallStatus.Active)
                {
                    var compatibleChamps = new[] {"Jinx", "Ezreal", "Ashe", "Draven", "Karthus"}; //Ziggs, Xerath, Lux
                    if (recall.Unit.IsEnemy && compatibleChamps.Any(h => h == Player.ChampionName) &&
                        !BaseUltUnits.Any(h => h.Unit.NetworkId == recall.Unit.NetworkId))
                    {
                        var spell = BaseUltSpells.Find(h => h.Name == Player.ChampionName);
                        if (Player.Spellbook.GetSpell(spell.Slot).IsReady &&
                            Player.Spellbook.GetSpell(spell.Slot).Level > 0)
                        {
                            BaseUltCalcs(recall);
                        }
                    }
                }
                if (recall.Status != RecallStatus.Active)
                {
                    var baseultUnit = BaseUltUnits.Find(h => h.Unit.NetworkId == recall.Unit.NetworkId);
                    if (baseultUnit != null)
                    {
                        BaseUltUnits.Remove(baseultUnit);
                    }
                }
            }

            foreach (var unit in BaseUltUnits)
            {
                if (Program.BaseUltMenu["checkcollision"].Cast<CheckBox>().CurrentValue && unit.Collision)
                {
                    continue;
                }

                if (unit.Unit.IsVisible)
                {
                    unit.LastSeen = Game.Time;
                }
                var timeLimit = Program.BaseUltMenu["timeLimit"].Cast<Slider>().CurrentValue;
                if (Math.Round(unit.FireTime, 2) == Math.Round(Game.Time, 2) && Game.Time - timeLimit >= unit.LastSeen)
                {
                    var spell = Player.Spellbook.GetSpell(BaseUltSpells.Find(h => h.Name == Player.ChampionName).Slot);
                    if (spell.IsReady)
                    {
                        Player.Spellbook.CastSpell(spell.Slot, GetFountainPos());
                    }
                }
            }
        }

        public static void OnEndScene()
        {
            if (!Program.BaseUltMenu["showrecalls"].Cast<CheckBox>().CurrentValue && !BaseUltUnits.Any())
            {
                return;
            }

            {
                Drawing.DrawLine(X, Y, X + Length, Y, Height, ColorTranslator.FromHtml("#080d0a"));
            }

            if (BaseUltUnits.Any())
            {
                Warning.Draw(new Vector2(Player.HPBarPosition.X + 40, Player.HPBarPosition.Y - 40));
                Underline.Draw(new Vector2(X - 50, Y + 30));
            }

            foreach (var recall in Recalls.OrderBy(h => h.Started))
            {
                if ((recall.Unit.IsAlly && !Program.BaseUltMenu["showallies"].Cast<CheckBox>().CurrentValue) ||
                    (recall.Unit.IsAlly && !Program.BaseUltMenu["showenemies"].Cast<CheckBox>().CurrentValue))
                {
                    continue;
                }

                var recallDuration = recall.Duration;

                if (recall.Status == RecallStatus.Active)
                {
                    var isBaseUlt = BaseUltUnits.Any(h => h.Unit.NetworkId == recall.Unit.NetworkId);
                    var percent = GetRecallPercent(recall);
                    var colorIndicator = isBaseUlt
                        ? Color.OrangeRed
                        : (recall.Unit.IsAlly
                            ? Color.DeepSkyBlue
                            : Color.DarkViolet);
                    var colorText = isBaseUlt
                        ? Color.OrangeRed
                        : (recall.Unit.IsAlly
                            ? Color.DeepSkyBlue
                            : Color.PaleVioletRed);
                    var colorBar = isBaseUlt
                        ? Color.Red
                        : (recall.Unit.IsAlly
                            ? Color.DodgerBlue
                            : Color.MediumVioletRed);

                    Bar.Color = colorBar;
                    Bar.Rectangle = new SharpDX.Rectangle(0, 0,
                        (int) (260*percent), 22);
                    Bar.Draw(new Vector2(X, Y + 4));

                    Drawing.DrawLine(
                        (int) (percent*Length) + X - (float) (LineThickness*0.5) +4,
                        Y - (float) (Height*0.5),
                        (int) (percent*Length) + X - (float) (LineThickness*0.5) +4,
                        Y + (float) (Height*0.5) + recall.TextPos*20,
                        LineThickness,
                        colorIndicator);

                    Text.Color = colorText;
                    Text.TextValue = "(" + (int) (percent*100) + "%) " + recall.Unit.ChampionName;
                    Text.Position = new Vector2((int) (percent*Length) + X - LineThickness, Y + (int) (Height*0.5 + 20 + LineThickness) + recall.TextPos*20);
                    Text.Draw();
                }

                if (recall.Status == RecallStatus.Abort || recall.Status == RecallStatus.Finished)
                {
                    const int fadeoutTime = 3;
                    var colorIndicator = recall.Status == RecallStatus.Abort
                        ? Color.OrangeRed
                        : Color.GreenYellow;
                    var colorText = recall.Status == RecallStatus.Abort
                        ? Color.Orange
                        : Color.GreenYellow;
                    var colorBar = recall.Status == RecallStatus.Abort
                        ? Color.Yellow
                        : Color.LawnGreen;

                    var fadeOutPercent = (recall.Ended + fadeoutTime - Game.Time)/fadeoutTime;

                    if (recall.Ended + fadeoutTime > Game.Time)
                    {
                        var timeUsed = recall.Ended - recall.Started;
                        var percent = timeUsed > recallDuration ? 1 : timeUsed/recallDuration;

                        Bar.Color = colorBar;
                        Bar.Rectangle = new SharpDX.Rectangle(0, 0, (int) (260*percent), 22);
                        Bar.Draw(new Vector2(X, Y + 4));

                        Drawing.DrawLine(
                            (int) (percent*Length) + X - (float) (LineThickness*0.5) +4,
                            Y - (float) (Height*0.5),
                            (int) (percent*Length) + X - (float) (LineThickness*0.5) +4,
                            Y + (float) (Height*0.5), LineThickness,
                            Color.FromArgb((int) (254*fadeOutPercent),
                                colorIndicator));

                        Text.Color = colorText;
                        Text.TextValue = recall.Unit.ChampionName;
                        Text.Position = new Vector2((int) (percent*Length) + X - LineThickness,
                            Y + (int) (Height*0.5 + 20 + LineThickness) + recall.TextPos*20);
                        Text.Draw();
                    }
                    else
                    {
                        recall.Status = RecallStatus.Inactive;
                        recall.TextPos = 0;
                    }
                }
            }

            foreach (var unit in BaseUltUnits)
            {
                var duration = Recalls.Find(h => h.Unit.NetworkId == unit.Unit.NetworkId).Duration;
                var barPos =
                    (unit.FireTime - Recalls.Find(h => unit.Unit.NetworkId == h.Unit.NetworkId).Started)
                    /duration;

                Drawing.DrawLine(
                    (int) (barPos*Length) + X - (float) (LineThickness*0.5),
                    Y - (float) (Height*0.5 + LineThickness),
                    (int) (barPos*Length) + X - (float) (LineThickness*0.5),
                    Y + (float) (Height*0.5 + LineThickness),
                    LineThickness,
                    Color.Lime);
            }

            Hud.Draw(new Vector2(X - 8, Y - 5));
        }

        private static Vector3 GetFountainPos()
        {
            switch (Game.MapId)
            {
                case GameMapId.SummonersRift:
                {
                    return Player.Team == GameObjectTeam.Order
                        ? new Vector3(14296, 14362, 171)
                        : new Vector3(408, 414, 182);
                }
                case GameMapId.CrystalScar:
                {
                    return Player.Team == GameObjectTeam.Order
                        ? new Vector3(524, 4164, 35)
                        : new Vector3(13323, 4105, 36);
                }
                case GameMapId.TwistedTreeline:
                {
                    return Player.Team == GameObjectTeam.Order 
                        ? new Vector3(1060, 7297, 150) 
                        : new Vector3(14353, 7297, 150);
                }
            }

            return new Vector3();
        }

        private static double GetRecallPercent(Recall recall)
        {
            var recallDuration = recall.Duration;
            var cd = recall.Started + recallDuration - Game.Time;
            var percent = (cd > 0 && Math.Abs(recallDuration) > float.Epsilon)
                ? 1f - (cd/recallDuration)
                : 1f;
            return percent;
        }

        private static float GetBaseUltTravelTime(float delay, float speed)
        {
            if (Player.ChampionName == "Karthus")
            {
                return delay/1000;
            }

            var distance = Vector3.Distance(Player.ServerPosition, GetFountainPos());
            var missilespeed = speed;

            if (Player.ChampionName == "Jinx" && distance > 1350)
            {
                const float accelerationrate = 0.3f;

                var acceldifference = distance - 1350f;

                if (acceldifference > 150f)
                {
                    acceldifference = 150f;
                }

                var difference = distance - 1500f;

                missilespeed = (1350f*speed + acceldifference*(speed + accelerationrate*acceldifference) + difference*2200f)/distance;
            }

            return (distance/missilespeed + ((delay - 65)/1000));
        }

        struct UltSpellDataS
        {
            public int SpellStage;
            public float DamageMultiplicator;
            public float Width;
            public float Delay;
            public float Speed;
            public bool Collision;
        }

        Dictionary<String, UltSpellDataS> UltSpellData = new Dictionary<string, UltSpellDataS>
        {
            {"Jinx",    new UltSpellDataS { SpellStage = 1, DamageMultiplicator = 1.0f, Width = 140f, Delay = 0600f/1000f, Speed = 1700f, Collision = true}},
            {"Ashe",    new UltSpellDataS { SpellStage = 0, DamageMultiplicator = 1.0f, Width = 130f, Delay = 0250f/1000f, Speed = 1600f, Collision = true}},
            {"Draven",  new UltSpellDataS { SpellStage = 0, DamageMultiplicator = 0.7f, Width = 160f, Delay = 0400f/1000f, Speed = 2000f, Collision = true}},
            {"Ezreal",  new UltSpellDataS { SpellStage = 0, DamageMultiplicator = 0.7f, Width = 160f, Delay = 1000f/1000f, Speed = 2000f, Collision = false}},
            {"Karthus", new UltSpellDataS { SpellStage = 0, DamageMultiplicator = 1.0f, Width = 000f, Delay = 3125f/1000f, Speed = 0000f, Collision = false}}
        };

        bool CanUseUlt(Obj_AI_Hero hero) //use for allies when fixed: champ.Spellbook.GetSpell(SpellSlot.R) = Ready
        {
            return hero.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready || 
                (hero.Spellbook.GetSpell(SpellSlot.R).Level > 0 && hero.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Surpressed && hero.Mana >= hero.Spellbook.GetSpell(SpellSlot.R).ManaCost);
        }

        void HandleUltTarget(EnemyInfo enemyInfo)
        {
            bool ultNow = false;
            bool me = false;

            foreach (Obj_AI_Hero champ in Allies.Where(x => //gathering the damage from allies should probably be done once only with timers
                            x.IsValid<Obj_AI_Hero>() &&
                            !x.IsDead && 
                            ((x.IsMe && !x.IsStunned) || TeamUlt.Items.Any(item => item.GetValue<bool>() && item.Name == x.ChampionName)) &&
                            CanUseUlt(x)))
            {
                if (Menu.Item("checkCollision").GetValue<bool>() && UltSpellData[champ.ChampionName].Collision && IsCollidingWithChamps(champ, EnemySpawnPos, UltSpellData[champ.ChampionName].Width))
                {
                    enemyInfo.RecallInfo.IncomingDamage[champ.NetworkId] = 0;
                    continue;
                }

                //increase timeneeded if it should arrive earlier, decrease if later
                var timeneeded = GetUltTravelTime(champ, UltSpellData[champ.ChampionName].Speed, UltSpellData[champ.ChampionName].Delay, EnemySpawnPos) - 65;

                if (enemyInfo.RecallInfo.GetRecallCountdown() >= timeneeded)
                    enemyInfo.RecallInfo.IncomingDamage[champ.NetworkId] = (float)champ.GetSpellDamage(enemyInfo.Player, SpellSlot.R, UltSpellData[champ.ChampionName].SpellStage) * UltSpellData[champ.ChampionName].DamageMultiplicator;
                else if (enemyInfo.RecallInfo.GetRecallCountdown() < timeneeded - (champ.IsMe ? 0 : 125)) //some buffer for allies so their damage isnt getting reset
                {
                    enemyInfo.RecallInfo.IncomingDamage[champ.NetworkId] = 0;
                    continue;
                }

                if (champ.IsMe)
                {
                    me = true;

                    enemyInfo.RecallInfo.EstimatedShootT = timeneeded;

                    if(enemyInfo.RecallInfo.GetRecallCountdown() - timeneeded < 60)
                        ultNow = true;
                }
            }

            if(me)
            {
                if(!IsTargetKillable(enemyInfo))
                {
                    enemyInfo.RecallInfo.LockedTarget = false;
                    return;
                }

                enemyInfo.RecallInfo.LockedTarget = true;

                if (!ultNow || Menu.Item("panicKey").GetValue<KeyBind>().Active)
                    return;

                Ultimate.Cast(EnemySpawnPos, true);
                LastUltCastT = Utils.TickCount;
            }
            else
            {
                enemyInfo.RecallInfo.LockedTarget = false;
                enemyInfo.RecallInfo.EstimatedShootT = 0;
            }
        }

        bool IsTargetKillable(EnemyInfo enemyInfo)
        {
            float totalUltDamage = enemyInfo.RecallInfo.IncomingDamage.Values.Sum();

            float targetHealth = GetTargetHealth(enemyInfo, enemyInfo.RecallInfo.GetRecallCountdown());

            if (Utils.TickCount - enemyInfo.LastSeen > 20000 && !Menu.Item("regardlessKey").GetValue<KeyBind>().Active)
            {
                if (totalUltDamage < enemyInfo.Player.MaxHealth)
                    return false;
            }
            else if (totalUltDamage < targetHealth)
                return false;

            return true;
        }

        float GetTargetHealth(EnemyInfo enemyInfo, int additionalTime)
        {
            if (enemyInfo.Player.IsVisible)
                return enemyInfo.Player.Health;

            float predictedHealth = enemyInfo.Player.Health + enemyInfo.Player.HPRegenRate * ((Utils.TickCount - enemyInfo.LastSeen + additionalTime) / 1000f);

            return predictedHealth > enemyInfo.Player.MaxHealth ? enemyInfo.Player.MaxHealth : predictedHealth;
        }

        float GetUltTravelTime(Obj_AI_Hero source, float speed, float delay, Vector3 targetpos)
        {
            if (source.ChampionName == "Karthus")
                return delay * 1000;

            float distance = Vector3.Distance(source.ServerPosition, targetpos);

            float missilespeed = speed;

            if(source.ChampionName == "Jinx" && distance > 1350)
            {
                const float accelerationrate = 0.3f; //= (1500f - 1350f) / (2200 - speed), 1 unit = 0.3units/second

                var acceldifference = distance - 1350f;

                if (acceldifference > 150f) //it only accelerates 150 units
                    acceldifference = 150f;

                var difference = distance - 1500f;

                missilespeed = (1350f * speed + acceldifference * (speed + accelerationrate * acceldifference) + difference * 2200f) / distance;
            }

            return (distance / missilespeed + delay) * 1000;
        }

        bool IsCollidingWithChamps(Obj_AI_Hero source, Vector3 targetpos, float width)
        {
            var input = new PredictionInput
            {
                Radius = width,
                Unit = source,
            };

            input.CollisionObjects[0] = CollisionableObjects.Heroes;

            return Collision.GetCollision(new List<Vector3> { targetpos }, input).Any(); //x => x.NetworkId != targetnetid, hard to realize with teamult
        }

        void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            var unit = sender as Obj_AI_Hero;

            if (unit == null || !unit.IsValid || unit.IsAlly)
            {
                return;
            }

            var recall = Packet.S2C.Teleport.Decoded(unit, args);
            var enemyInfo = EnemyInfo.Find(x => x.Player.NetworkId == recall.UnitNetworkId).RecallInfo.UpdateRecall(recall);

            if(recall.Type == Packet.S2C.Teleport.Type.Recall)
            {
                switch(recall.Status)
                {
                    case Packet.S2C.Teleport.Status.Abort:
                        if(Menu.Item("notifRecAborted").GetValue<bool>())
                        {
                            ShowNotification(enemyInfo.Player.ChampionName + ": Recall ABORTED", System.Drawing.Color.Orange, 4000);
                        }
                        
                        break;
                    case Packet.S2C.Teleport.Status.Finish:
                        if (Menu.Item("notifRecFinished").GetValue<bool>())
                        {
                            ShowNotification(enemyInfo.Player.ChampionName + ": Recall FINISHED", NotificationColor, 4000);
                        }

                        break;
                }
            }
        }

        void Drawing_OnPostReset(EventArgs args)
        {
            Text.OnResetDevice();
        }

        void Drawing_OnPreReset(EventArgs args)
        {
            Text.OnLostDevice();
        }

        void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            Text.Dispose();
        }

        void Drawing_OnDraw(EventArgs args)
        {
            if (!Menu.Item("showRecalls").GetValue<bool>() || Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed)
                return;

            bool indicated = false;

            float fadeout = 1f;
            int count = 0;

            foreach (EnemyInfo enemyInfo in EnemyInfo.Where(x =>
                x.Player.IsValid<Obj_AI_Hero>() &&
                x.RecallInfo.ShouldDraw() &&
                !x.Player.IsDead && //maybe redundant
                x.RecallInfo.GetRecallCountdown() > 0).OrderBy(x => x.RecallInfo.GetRecallCountdown()))
            {
                if (!enemyInfo.RecallInfo.LockedTarget)
                {
                    fadeout = 1f;
                    Color color = System.Drawing.Color.White;

                    if (enemyInfo.RecallInfo.WasAborted())
                    {
                        fadeout = (float)enemyInfo.RecallInfo.GetDrawTime() / (float)enemyInfo.RecallInfo.FADEOUT_TIME;
                        color = System.Drawing.Color.Yellow;
                    }

                    DrawRect(BarX, BarY, (int)(Scale * (float)enemyInfo.RecallInfo.GetRecallCountdown()), BarHeight, 1, System.Drawing.Color.FromArgb((int)(100f * fadeout), System.Drawing.Color.White));
                    DrawRect(BarX + Scale * (float)enemyInfo.RecallInfo.GetRecallCountdown() - 1, BarY - SeperatorHeight, 0, SeperatorHeight + 1, 1, System.Drawing.Color.FromArgb((int)(255f * fadeout), color));

                    Text.DrawText(null, enemyInfo.Player.ChampionName, (int)BarX + (int)(Scale * (float)enemyInfo.RecallInfo.GetRecallCountdown() - (float)(enemyInfo.Player.ChampionName.Length * Text.Description.Width) / 2), (int)BarY - SeperatorHeight - Text.Description.Height - 1, new ColorBGRA(color.R, color.G, color.B, (byte)((float)color.A * fadeout)));
                }
                else
                {
                    if(!indicated && enemyInfo.RecallInfo.EstimatedShootT != 0)
                    {
                        indicated = true;
                        DrawRect(BarX + Scale * enemyInfo.RecallInfo.EstimatedShootT, BarY + SeperatorHeight + BarHeight - 3, 0, SeperatorHeight*2, 2, System.Drawing.Color.Orange);
                    }

                    DrawRect(BarX, BarY, (int)(Scale * (float)enemyInfo.RecallInfo.GetRecallCountdown()), BarHeight, 1, System.Drawing.Color.FromArgb(255, System.Drawing.Color.Red));
                    DrawRect(BarX + Scale * (float)enemyInfo.RecallInfo.GetRecallCountdown() - 1, BarY + SeperatorHeight + BarHeight - 3, 0, SeperatorHeight + 1, 1, System.Drawing.Color.IndianRed);

                    Text.DrawText(null, enemyInfo.Player.ChampionName, (int)BarX + (int)(Scale * (float)enemyInfo.RecallInfo.GetRecallCountdown() - (float)(enemyInfo.Player.ChampionName.Length * Text.Description.Width) / 2), (int)BarY + SeperatorHeight + Text.Description.Height / 2, new ColorBGRA(255, 92, 92, 255));
                }

                count++;
            }

            /*
             * Show in a red rectangle right next to the normal bar the names of champs which can be killed (when they are not recalling yet)
             * Requires calculating the damages (make more functions!)
             * 
             * var BaseUltableEnemies = EnemyInfo.Where(x =>
                x.Player.IsValid<Obj_AI_Hero>() &&
                !x.RecallInfo.ShouldDraw() &&
                !x.Player.IsDead && //maybe redundant
                x.RecallInfo.GetRecallCountdown() > 0 && x.RecallInfo.LockedTarget).OrderBy(x => x.RecallInfo.GetRecallCountdown());*/

            if(count > 0)
            {
                if (count != 1) //make the whole bar fadeout when its only 1
                    fadeout = 1f;

                DrawRect(BarX, BarY, BarWidth, BarHeight, 1, System.Drawing.Color.FromArgb((int)(40f * fadeout), System.Drawing.Color.White));

                DrawRect(BarX - 1, BarY + 1, 0, BarHeight, 1, System.Drawing.Color.FromArgb((int)(255f * fadeout), System.Drawing.Color.White));
                DrawRect(BarX - 1, BarY - 1, BarWidth + 2, 1, 1, System.Drawing.Color.FromArgb((int)(255f * fadeout), System.Drawing.Color.White));
                DrawRect(BarX - 1, BarY + BarHeight, BarWidth + 2, 1, 1, System.Drawing.Color.FromArgb((int)(255f * fadeout), System.Drawing.Color.White));
                DrawRect(BarX + 1 + BarWidth, BarY + 1, 0, BarHeight, 1, System.Drawing.Color.FromArgb((int)(255f * fadeout), System.Drawing.Color.White));
            }
        }

        public void DrawRect(float x, float y, int width, int height, float thickness, System.Drawing.Color color)
        {
            for (int i = 0; i < height; i++)
                Drawing.DrawLine(x, y + i, x + width, y + i, thickness, color);
        }
    }

    class EnemyInfo
    {
        public Obj_AI_Hero Player;
        public int LastSeen;

        public RecallInfo RecallInfo;

        public EnemyInfo(Obj_AI_Hero player)
        {
            Player = player;
            RecallInfo = new RecallInfo(this);
        }
    }

    class RecallInfo
    {
        public EnemyInfo EnemyInfo;
        public Dictionary<int, float> IncomingDamage; //from, damage
        public Packet.S2C.Teleport.Struct Recall;
        public Packet.S2C.Teleport.Struct AbortedRecall;
        public bool LockedTarget;
        public float EstimatedShootT;
        public int AbortedT;
        public int FADEOUT_TIME = 3000;

        public RecallInfo(EnemyInfo enemyInfo)
        {
            EnemyInfo = enemyInfo;
            Recall = new Packet.S2C.Teleport.Struct(EnemyInfo.Player.NetworkId, Packet.S2C.Teleport.Status.Unknown, Packet.S2C.Teleport.Type.Unknown, 0);
            IncomingDamage = new Dictionary<int, float>(); 
        }

        public bool ShouldDraw()
        {
            return IsPorting() || (WasAborted() && GetDrawTime() > 0);
        }

        public bool IsPorting()
        {
            return Recall.Type == Packet.S2C.Teleport.Type.Recall && Recall.Status == Packet.S2C.Teleport.Status.Start;
        }

        public bool WasAborted()
        {
            return Recall.Type == Packet.S2C.Teleport.Type.Recall && Recall.Status == Packet.S2C.Teleport.Status.Abort;
        }

        public EnemyInfo UpdateRecall(Packet.S2C.Teleport.Struct newRecall)
        {
            IncomingDamage.Clear();
            LockedTarget = false;
            EstimatedShootT = 0;

            if (newRecall.Type == Packet.S2C.Teleport.Type.Recall && newRecall.Status == Packet.S2C.Teleport.Status.Abort)
            {
                AbortedRecall = Recall;
                AbortedT = Utils.TickCount;
            }   
            else
                AbortedT = 0;

            Recall = newRecall;
            return EnemyInfo;
        }

        public int GetDrawTime()
        {
            int drawtime = 0;

            if(WasAborted())
                drawtime = FADEOUT_TIME - (Utils.TickCount - AbortedT);
            else
                drawtime = GetRecallCountdown();

            return drawtime < 0 ? 0 : drawtime;
        }

        public int GetRecallCountdown()
        {
            int time = Utils.TickCount;
            int countdown = 0;

            if (time - AbortedT < FADEOUT_TIME)
                countdown = AbortedRecall.Duration - (AbortedT - AbortedRecall.Start);
            else if(AbortedT > 0)
                countdown = 0; //AbortedT = 0
            else
                countdown = Recall.Start + Recall.Duration - time;

            return countdown < 0 ? 0 : countdown;
        }

        public override string ToString()
        {
            String drawtext = EnemyInfo.Player.ChampionName + ": " + Recall.Status; //change to better string and colored

            float countdown = GetRecallCountdown() / 1000f;

            if (countdown > 0)
                drawtext += " (" + countdown.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "s)";

            return drawtext;
        }
    }
}
