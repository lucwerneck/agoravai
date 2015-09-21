#region References

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using SharpDX.Direct3D9;
using Tracker.Properties;
using Color = System.Drawing.Color;
using Font = EloBuddy.SDK.Rendering.Text;
using Sprite = EloBuddy.SDK.Rendering.Sprite;
#endregion

namespace Tracker
{
    public static class OfficialAddon
    {
        #region Offsets
        private const int OFFSET_HUD_X = -31; //31
        private const int OFFSET_HUD_Y = 16; //11

        private const int OFFSET_SPELLS_X = OFFSET_HUD_X + 22;
        private const int OFFSET_SPELLS_Y = OFFSET_HUD_Y + 23;

        private const int OFFSET_SUMMONERS_X = OFFSET_HUD_X + 4; //9
        private const int OFFSET_SUMMONERS_Y = OFFSET_HUD_Y + 2; //5

        private const int OFFSET_XP_X = OFFSET_HUD_X + 40; //44
        private const int OFFSET_XP_Y = OFFSET_HUD_Y + -49; //53
        #endregion
        #region Sprites
        private static Sprite HUDTexture;
        private static Sprite OnCd;
        private static Sprite IsReady;
        private static Sprite XpBar;
        private static Sprite SummonerCd;

        private static readonly Dictionary<int, Sprite> Summoner1 = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> Summoner2 = new Dictionary<int, Sprite>();
        #endregion
        #region Text
        private static Font AbilityText;
        #endregion
        #region Abilities
        private static readonly SpellSlot[] Abilities = { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
        private static readonly SpellSlot[] Summoners = { SpellSlot.Summoner1, SpellSlot.Summoner2 };
        #endregion

        public static void Initialize()
        {
            #region LoadTextures

            HUDTexture = new Sprite(Texture.FromMemory(
                Drawing.Direct3DDevice,
                (byte[])new ImageConverter().ConvertTo(Resources.hud, typeof(byte[])), 153,
                36, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0));
            OnCd = new Sprite(Texture.FromMemory(
                Drawing.Direct3DDevice, (byte[])new ImageConverter().ConvertTo(Resources.OnCD, typeof(byte[])), 22,
                9, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0));
            IsReady = new Sprite(Texture.FromMemory(
                Drawing.Direct3DDevice, (byte[])new ImageConverter().ConvertTo(Resources.IsReady, typeof(byte[])), 22,
                9, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0));
            XpBar = new Sprite(Texture.FromMemory(
                Drawing.Direct3DDevice, (byte[])new ImageConverter().ConvertTo(Resources.xpBar, typeof(byte[])), 104,
                3, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0));
            SummonerCd = new Sprite(Texture.FromMemory(
                            Drawing.Direct3DDevice, (byte[])new ImageConverter().ConvertTo(Resources.summonercd, typeof(byte[])),
                            13, 13, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0));
            
            #region SummonerTextures
            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                foreach (var summoner in Summoners)
                {
                    var spell = hero.Spellbook.GetSpell(summoner);
                    var texture = GetSummonerSprite(spell);

                    if (spell.Slot == SpellSlot.Summoner1)
                    {

                        Summoner1.Add(hero.NetworkId, new Sprite(Texture.FromMemory(
                            Drawing.Direct3DDevice, (byte[])new ImageConverter().ConvertTo(texture, typeof(byte[])),
                            13,
                            13, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0)));
                    }
                    else
                    {
                        Summoner2.Add(hero.NetworkId, new Sprite(Texture.FromMemory(
                            Drawing.Direct3DDevice, (byte[])new ImageConverter().ConvertTo(texture, typeof(byte[])),
                            13,
                            13, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0)));
                    }
                }
            }
            #endregion
            #endregion
            #region SetText

            AbilityText = new Text("", new FontDescription
            {
                FaceName = "Calibri",
                Height = 13,
                OutputPrecision = FontPrecision.Default,
                Quality = FontQuality.ClearType
            });

            #endregion
        }

        public static void DrawingOnOnEndScene(EventArgs args)
        {

            #region MenuSettings
            var showAllies = Program.TrackerMenu["showAllies"].Cast<CheckBox>().CurrentValue;
            var showEnemies = Program.TrackerMenu["showEnemies"].Cast<CheckBox>().CurrentValue;
            var showXp = Program.TrackerMenu["showXp"].Cast<CheckBox>().CurrentValue;
            var showTimer = Program.TrackerMenu["showTimer"].Cast<CheckBox>().CurrentValue;
            #endregion

            foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(h => h.IsHPBarRendered))
            {
                //Return if Ally or Enemy tracking isnt enabled in menu
                if (hero.IsAlly && !showAllies || hero.IsEnemy && !showEnemies || hero.IsMe)
                    continue;

                #region Sprite

                //HUD Sprite
                HUDTexture.Draw(new Vector2(hero.HPBarPosition.X + OFFSET_HUD_X, hero.HPBarPosition.Y + OFFSET_HUD_Y));

                //Summonor Sprites
                foreach (var summoner in Summoners)
                {
                    var spell = hero.Spellbook.GetSpell(summoner);
                    var cd = spell.CooldownExpires - Game.Time;
                    var spellPos = hero.GetSummoneroffset(spell.Slot);
                    var texture = spell.Slot == SpellSlot.Summoner1
                        ? Summoner1[hero.NetworkId]
                        : Summoner2[hero.NetworkId];
                    texture.Draw(new Vector2(spellPos.X, spellPos.Y));
                    if (cd > 0)
                    {
                        SummonerCd.Draw(new Vector2(spellPos.X, spellPos.Y));
                    }
                }

                //Ability Sprites
                foreach (var ability in Abilities)
                {
                    if (hero.Spellbook.CanUseSpell(ability) != SpellState.NotLearned)
                    {
                        var cd = hero.Spellbook.GetSpell(ability).CooldownExpires - Game.Time;
                        var spellPos = hero.GetSpelloffset(ability);
                        var percent = (cd > 0 && Math.Abs(hero.Spellbook.GetSpell(ability).Cooldown) > float.Epsilon)
                            ? 1f - (cd / hero.Spellbook.GetSpell(ability).Cooldown)
                            : 1f;

                        if (cd > 0)
                        {
                            OnCd.Rectangle = new SharpDX.Rectangle(0, 0, (int)(percent * 22), 9);
                            OnCd.Draw(new Vector2(spellPos.X, spellPos.Y));
                        }
                        else
                        {
                            IsReady.Rectangle = new SharpDX.Rectangle(0, 0, (int)(percent * 22), 9);
                            IsReady.Draw(new Vector2(spellPos.X, spellPos.Y));
                        }
                    }
                }

                //XP Sprite (xp bar)
                if (showXp)
                {
                    XpBar.Rectangle = new SharpDX.Rectangle(0, 0, (int)(104 * (hero.Experience.XPPercentage / 100)), 3);
                    XpBar.Draw(new Vector2(hero.HPBarPosition.X - OFFSET_XP_X, hero.HPBarPosition.Y - OFFSET_XP_Y));
                }

                //Sprite.End();
                #endregion
                #region Text
                //CoolDown Timers
                if (showTimer)
                {
                    //Ability Timers
                    foreach (var ability in Abilities)
                    {
                        if (hero.Spellbook.CanUseSpell(ability) != SpellState.NotLearned)
                        {
                            var cd = hero.Spellbook.GetSpell(ability).CooldownExpires - Game.Time;
                            var spellPos = hero.GetSpelloffset(ability);
                            if (cd > 0)
                            {
                                var cdFrom = cd < 1 ? cd.ToString("0.0") : cd.ToString("0");
                                AbilityText.TextValue = cdFrom;
                                AbilityText.Color = Color.AntiqueWhite;
                                AbilityText.Position = new Vector2((int)spellPos.X + 10 - cdFrom.Length * 2,
                                    (int)spellPos.Y + 12);
                                AbilityText.Draw();
                            }
                        }
                    }

                    //Summoner Timers
                    foreach (var summoner in Summoners)
                    {
                        var cd = hero.Spellbook.GetSpell(summoner).CooldownExpires - Game.Time;
                        var spellPos = hero.GetSummoneroffset(summoner);
                        if (cd > 0)
                        {
                            var cdFrom = cd < 1 ? cd.ToString("0.0") : cd.ToString("0");
                            AbilityText.TextValue = cdFrom;
                            AbilityText.Color = Color.AntiqueWhite;
                            AbilityText.Position = new Vector2((int)spellPos.X - 27 + cdFrom.Length,
                                (int)spellPos.Y - 1);
                            AbilityText.Draw();
                        }
                    }
                }
                #endregion
            }
        }

        private static Vector2 GetSpelloffset(this Obj_AI_Base hero, SpellSlot slot)
        {
            var normalPos = new Vector2(hero.HPBarPosition.X + OFFSET_SPELLS_X, hero.HPBarPosition.Y + OFFSET_SPELLS_Y);
            switch (slot)
            {
                case SpellSlot.Q:
                    return normalPos;
                case SpellSlot.W:
                    return new Vector2(normalPos.X + 27, normalPos.Y);
                case SpellSlot.E:
                    return new Vector2(normalPos.X + 2 * 27, normalPos.Y);
                case SpellSlot.R:
                    return new Vector2(normalPos.X + 3 * 27, normalPos.Y);
            }

            return normalPos;
        }

        private static Vector2 GetSummoneroffset(this Obj_AI_Base hero, SpellSlot slot)
        {
            var normalPos = new Vector2(hero.HPBarPosition.X + OFFSET_SUMMONERS_X, hero.HPBarPosition.Y + OFFSET_SUMMONERS_Y);
            switch (slot)
            {
                case SpellSlot.Summoner1:
                    return normalPos;
                case SpellSlot.Summoner2:
                    return new Vector2(normalPos.X, normalPos.Y + 17);
            }

            return normalPos;
        }

        private static Bitmap GetSummonerSprite(this SpellDataInst spell)
        {
            switch (spell.Name)
            {
                case "itemsmiteaoe":
                    return Resources.itemsmiteaoe;
                case "s5_summonersmiteduel":
                    return Resources.s5_summonersmiteduel;
                case "s5_summonersmiteplayerganker":
                    return Resources.s5_summonersmiteplayerganker;
                case "s5_summonersmitequick":
                    return Resources.s5_summonersmitequick;
                case "summonerbarrier":
                    return Resources.summonerbarrier;
                case "summonerboost":
                    return Resources.summonerboost;
                case "summonerclairvoyance":
                    return Resources.summonerclairvoyance;
                case "summonerdot":
                    return Resources.summonerdot;
                case "summonerexhaust":
                    return Resources.summonerexhaust;
                case "summonerflash":
                    return Resources.summonerflash;
                case "summonerhaste":
                    return Resources.summonerhaste;
                case "summonerheal":
                    return Resources.summonerheal;
                case "summonermana":
                    return Resources.summonermana;
                case "summonerodinGarrison":
                    return Resources.summonerodingarrison;
                case "summonerrevive":
                    return Resources.summonerrevive;
                case "summonersmite":
                    return Resources.summonersmite;
                case "summonerteleport":
                    return Resources.summonerteleport;
            }

            return Resources.summonerdot;
        }
    }
}
