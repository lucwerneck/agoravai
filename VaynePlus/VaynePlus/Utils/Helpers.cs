using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace VaynePlus.Utils
{
    static class Helpers
    {
        public static float LastMoveC;

        #region Utility Methods
        public static bool IsJ4FlagThere(Vector3 position, AIHeroClient target)
        {
            return ObjectManager.Get<Obj_AI_Base>().Any(m => m.Distance(position) <= target.BoundingRadius && m.Name == "Beacon");
        }

        public static bool IsFountain(Vector3 position)
        {
            float fountainRange = 750;
            if (Game.MapId == GameMapId.SummonersRift)
            {
                fountainRange = 1050;
            }
            return ObjectManager.Get<GameObject>().Where(spawnPoint => spawnPoint is Obj_SpawnPoint && spawnPoint.IsAlly).Any(spawnPoint => Vector2.Distance(position.To2D(), spawnPoint.Position.To2D()) < fountainRange);
        }

        public static bool IsSummonersRift()
        {
            if (Game.MapId == GameMapId.SummonersRift)
            {
                return true;
            }
            return false;
        }

        public static bool Has2WStacks(this AIHeroClient target)
        {
            return target.Buffs.Any(bu => bu.Name == "vaynesilvereddebuff" && bu.Count == 2);
        }

        public static BuffInstance GetWBuff(this AIHeroClient target)
        {
            return target.Buffs.FirstOrDefault(bu => bu.Name == "vaynesilvereddebuff");
        }

        public static bool IsPlayerFaded()
        {
            return (ObjectManager.Player.HasBuff("vaynetumblefade") && !ObjectManager.Player.UnderTurret(true));
        }
        public static void MoveToLimited(Vector3 where)
        {
            if (Environment.TickCount - LastMoveC < 80)
            {
                return;
            }
            LastMoveC = Environment.TickCount;
            Player.IssueOrder(GameObjectOrder.MoveTo, where);
        }

        public static List<Vector3> GetWallsInRange(this Vector3 pos, AIHeroClient target)
        {
            var list = new List<Vector3>();
            const int currentStep = 30;
            var direction = target.Direction.To2D().Perpendicular();
            for (var i = 0f; i < 360f; i += currentStep)
            {
                var angleRad = EloBuddy.SDK.Geometry.DegreeToRadian(i);
                var rotatedPosition = pos.To2D() + (target.BoundingRadius * 1.25f * direction.Rotated(angleRad));
                var collFlags = NavMesh.GetCollisionFlags(rotatedPosition.To3D());
                if (collFlags == CollisionFlags.Wall || collFlags == CollisionFlags.Building)
                {
                    list.Add(rotatedPosition.To3D());
                    break;
                }
            }
            return list;
        }

        public static bool OkToQ(this Vector3 position)
        {
            if (position.UnderTurret(true) && !ObjectManager.Player.UnderTurret(true))
                return false;
            var allies = position.CountAlliesInRange(ObjectManager.Player.AttackRange);
            var enemies = position.CountEnemiesInRange(ObjectManager.Player.AttackRange);
            var lhEnemies = GetLhEnemiesNearPosition(position, ObjectManager.Player.AttackRange).Count();

            if (enemies == 1) //It's a 1v1, safe to assume I can E
            {
                return true;
            }

            //Adding 1 for the Player
            return (allies + 1 > enemies - lhEnemies);
        }

        public static bool OkToQ2(this Vector3 Position)
        {
            if (!OrbwalkerModes.ModesMenu["vayneplus.modes.q.mirin"].Cast<CheckBox>().CurrentValue)
            {
                var closeEnemies =
                    HeroManager.Enemies.FindAll(en => en.IsValidTarget(1500f)).OrderBy(en => en.Distance(Position));
                if (closeEnemies.Any())
                {
                    return
                        closeEnemies.All(
                            enemy =>
                                Position.CountEnemiesInRange(OrbwalkerModes.ModesMenu["vayneplus.modes.q.dynamicqsafety"].Cast<CheckBox>().CurrentValue
                                        ? enemy.AttackRange
                                        : 405f) < 1);
                }
                return true;
            }
            else
            {
                if (Position.CountEnemiesInRange(360f) >= 1)
                {
                    return false;
                }
                return true;
            }

        }
        public static List<AIHeroClient> GetLhEnemiesNearPosition(Vector3 position, float range)
        {
            return HeroManager.Enemies.Where(hero => hero.IsValid && hero.Distance(position) < range  && hero.HealthPercent <= 15).ToList();
        }

        public static bool UnderAllyTurret(Vector3 Position)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(t => t.IsAlly && !t.IsDead);
        }

        public static bool UnderTurret(this AIHeroClient unit)
        {
            return UnderTurret(unit.Position, true);
        }

        public static bool UnderTurret(this Obj_AI_Base unit, bool enemyTurretsOnly)
        {
            return UnderTurret(unit.Position, enemyTurretsOnly);
        }

        public static bool UnderTurret(this Vector3 position, bool enemyTurretsOnly)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>().Any(turret => turret.IsValid && turret.Distance(position) < 950 && enemyTurretsOnly ? turret.IsEnemy : true);
        }

        public static int CountAlliesInRange(float range)
        {
            return ObjectManager.Player.CountAlliesInRange(range);
        }

        /// <summary>
        ///     Counts the allies in range of the Unit.
        /// </summary>
        public static int CountAlliesInRange(this Obj_AI_Base unit, float range)
        {
            return unit.ServerPosition.CountAlliesInRange(range, unit);
        }

        /// <summary>
        ///     Counts the allies in the range of the Point. 
        /// </summary>
        public static int CountAlliesInRange(this Vector3 point, float range, Obj_AI_Base originalunit = null)
        {
            if (originalunit != null)
            {
                return HeroManager.Allies
                    .Count(x => x.NetworkId != originalunit.NetworkId && x.IsValid && x.Distance(point) < range);
            }
            return HeroManager.Allies
             .Count(x => x.IsValid && x.Distance(point) < range);
        }

        public static List<AIHeroClient> GetAlliesInRange(this AIHeroClient unit, float range)
        {
            return GetAlliesInRange(unit.ServerPosition, range, unit);
        }

        public static List<AIHeroClient> GetAlliesInRange(this Vector3 point, float range, Obj_AI_Base originalunit = null)
        {
            if (originalunit != null)
            {
                return
                    HeroManager.Allies
                        .FindAll(x => x.NetworkId != originalunit.NetworkId && point.Distance(x.ServerPosition, true) <= range * range);
            }
            return
                   HeroManager.Allies
                       .FindAll(x => point.Distance(x.ServerPosition, true) <= range * range);
        }

        public static bool IsWall(this Vector3 position)
        {
            return NavMesh.GetCollisionFlags(position).HasFlag(CollisionFlags.Wall);
        }

        public static bool IsWall(this Vector2 position)
        {
            return position.To3D().IsWall();
        }

        public static bool CanCast(Obj_AI_Base unit, Spell.SpellBase spell)
        {
            return spell.IsReady() && unit.IsValidTarget(spell.Range);
        }

        public static List<AIHeroClient> GetEnemiesInRange(this Obj_AI_Base unit, float range)
        {
            return GetEnemiesInRange(unit.ServerPosition, range);
        }

        public static List<AIHeroClient> GetEnemiesInRange(this Vector3 point, float range)
        {
            return
                HeroManager.Enemies
                    .FindAll(x => point.Distance(x.ServerPosition, true) <= range * range);
        }

        public static float GetDamage(Obj_AI_Base target, SpellSlot slot)
        {
            var level = Player.GetSpell(slot).Level-1;
            switch (slot)
            {
                case SpellSlot.Q:
                {
                    var damage = new float[] {30, 35, 40, 45, 50}[level]/100*
                                 (Player.Instance.BaseAttackDamage + Player.Instance.FlatPhysicalDamageMod);
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Physical, damage);
                }
                    case SpellSlot.W:
                {
                    var damage = new float[] {20, 30, 40, 50, 60}[level] + (new float[] {4, 5, 6, 7, 8}[level]/100)*target.MaxHealth;
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.True, damage);
                }
                    case SpellSlot.E:
                {
                    var damage = new float[] {45, 80, 115, 150, 185}[level] + 0.5*Player.Instance.FlatPhysicalDamageMod;
                    return Damage.CalculateDamageOnUnit(Player.Instance, target, DamageType.Physical, (float)damage);
                }
            }

            return 0;
        }
        #endregion
    }
}
