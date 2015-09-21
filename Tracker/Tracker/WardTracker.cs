using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;

namespace Tracker
{
    class WardTracker
    {
        private static List<Ward> Wards = new List<Ward>();
        private static Circle WardCircle;
        private static Circle WardBorderCircle;

        public static void Init()
        {
            WardCircle = new Circle
            {
                Filled = true
            };
            WardBorderCircle = new Circle
            {
                BorderWidth = 5,
                Filled = false
            };

            GameObject.OnCreate += MissleOnOnCreate;
            GameObject.OnCreate += GameObjectOnOnCreate;
            GameObject.OnDelete += GameObjectOnOnDelete;
            Obj_AI_Base.OnProcessSpellCast += AiHeroClientOnOnProcessSpellCast;
            Game.OnTick += GameOnOnTick;
            Drawing.OnDraw += DrawingOnOnDraw;
        }

        private static void GameObjectOnOnDelete(GameObject sender, EventArgs args)
        {
            var wardObj = sender as Obj_AI_Base;
            if (wardObj == null || wardObj.IsMinion || sender.IsAlly)
            {
                return;
            }

            if (GetDurationbyBaseSkin(wardObj.BaseSkinName) != 0)
            {
                Wards.Remove(Wards.Find(h => h.Position == wardObj.Position));
            }
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {
            foreach (var ward in Wards)
            {
                var color = ward.Type.ToString().Contains("Vision") ? Color.Purple : Color.Green;
                //filled
                {
                    var actuallColor = Color.FromArgb(100, color.R, color.G, color.B);
                    WardCircle.Color = actuallColor;
                    WardCircle.Radius = 150;
                    WardCircle.Draw(new Vector3(ward.Position.X, ward.Position.Y, NavMesh.GetHeightForPosition(ward.Position.X, ward.Position.Y)));
                }
                {
                    WardBorderCircle.Color = color;
                    WardBorderCircle.Radius = 150;
                    WardBorderCircle.Draw(new Vector3(ward.Position.X, ward.Position.Y, NavMesh.GetHeightForPosition(ward.Position.X, ward.Position.Y)));
                }
            }
        }

        private static void GameOnOnTick(EventArgs args)
        {
            foreach (var ward in Wards)
            {
                if (ward.CreationTime + ward.Duration > Game.Time)
                {
                    continue;
                }

                Wards.Remove(ward);
            }
        }

        private static void AiHeroClientOnOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var wardType = GetWardTypebySpellName(args.SData.Name);
            if (sender.IsAlly || wardType == WardType.Unknown)
            {
                return;
            }

            var endPosition = ObjectManager.Player.GetPath(args.End).ToList().Last();
            var ward = Wards.Find(h => h.Position.Distance(endPosition) < 100);
            if (ward != null)
            {
                ward.Type = wardType;
            }
            else
            {
                Wards.Add(new Ward
                {
                    CreationTime = Game.Time,
                    Duration = 0,
                    Position = endPosition,
                    Type = wardType
                });
            }
        }

        private static void MissleOnOnCreate(GameObject sender, EventArgs args)
        {
            var missile = sender as Obj_SpellMissile;

            if (missile == null || missile.SData.Name != "itemplacementmissile" || missile.SpellCaster.IsVisible || missile.SpellCaster.IsAlly)
            {
                return;
            }

            Core.DelayAction(() =>
            {
                Wards.Add(new Ward
                {
                    Type = WardType.Unknown,
                    CreationTime = Game.Time,
                    Position = missile.EndPosition
                });
            }, 1000);
        }

        private static void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            var wardObj = sender as Obj_AI_Base;
            if (wardObj == null || wardObj.IsMinion || sender.IsAlly)
            {
                return;
            }

            var ward = Wards.Find(h => h.Position == wardObj.Position);
            if (ward != null)
            {
                ward.Duration = GetDurationbyBaseSkin(wardObj.BaseSkinName);
            }
            else
            {
                Wards.Add(new Ward
                {
                    CreationTime = Game.Time,
                    Duration = GetDurationbyBaseSkin(wardObj.BaseSkinName),
                    Position = wardObj.Position,
                    Type = WardType.Unknown
                });
            }
        }

        private static int GetDurationbyBaseSkin(string baseskinname)
        {
            switch (baseskinname)
            {
                case "YellowTrinket":
                {
                    return 60;
                }
                case "YellowTrinketUpgrade":
                {
                    return 60*3;
                }
                case "SightWard":
                {
                    return 60*3;
                }
                case "VisionWard":
                {
                    return Int32.MaxValue;
                }
                case "CaitlynTrap":
                {
                    return 60 * 4;
                }
                case "TeemoMushroom":
                {
                    return 60 * 10 ;
                }
                case "ShacoBox":
                {
                    return 60 * 1;
                }
                case "Nidalee_Spear":
                {
                    return 60 * 2;
                }
            }
            return 0;
        }

        private static WardType GetWardTypebySpellName(string spellname)
        {
            switch (spellname)
            {
                case "TrinketTotemLvl1":
                {
                    return WardType.WardingTotem;
                }
                case "TrinketTotemLvl2":
                {
                    return WardType.GreaterTotem;
                }
                case "TrinketTotemLvl3":
                {
                    return WardType.GreaterStealthTotem;
                }
                case "SightWard":
                {
                    return WardType.StealthWard;
                }
                case "wrigglelantern":
                {
                    return WardType.WrigglesLantern;
                }
                case "TrinketTotemLvl3B":
                {
                    return WardType.GreaterVisionTotem;
                }
                case "VisionWard":
                {
                    return WardType.VisionWard;
                }
                case "CaitlynYordleTrap":
                {
                    return WardType.Unknown;
                }
                case "BantamTrap":
                {
                    return WardType.Unknown;
                }
                case "JackInTheBox":
                {
                    return WardType.Unknown;
                }
                case "Bushwhack":
                {
                    return WardType.Unknown;
                }
            }

            return WardType.Unknown;
        }

        class Ward
        {
            public WardType Type { get; set; }
            public float CreationTime { get; set; }
            public Vector3 Position { get; set; }
            public int Duration { get; set; }
        }
    }
}
