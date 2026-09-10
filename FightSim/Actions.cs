using FightSim.SimMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FightSim.Actions.ConditionRules;

namespace FightSim.Actions
{
    public enum RollType { FLAT, ADV, DIS }
    public struct ActionResult
    {
        public string ActionName;
        public bool Success;
        public string FailureReason;
        public string AttackerName;
        public string TargetName;
        public int? TargetNumber;
        public int? Magnitude;

        public static ActionResult Fail(string actionName, string reason, int? targetNumber = null)
        {
            return new ActionResult
            {
                ActionName = actionName,
                Success = false,
                FailureReason = reason,
                TargetNumber = targetNumber
            };
        }

        public static ActionResult Ok(string actionName, string attacker, string target, int? magnitude = null, int? targetNumber = null)
        {
            return new ActionResult
            {
                ActionName = actionName,
                Success = true,
                AttackerName = attacker,
                TargetName = target,
                TargetNumber = targetNumber,
                Magnitude = magnitude
            };
        }
    }
    static class Resolution
    {

        public static RollType CombineAdvantage(bool hasAdvantage, bool hasDisadvantage)
        {
            if (hasAdvantage && hasDisadvantage) return RollType.FLAT;
            if (hasAdvantage) return RollType.ADV;
            if (hasDisadvantage) return RollType.DIS;
            return RollType.FLAT;
        }
        public static int RollAgainst(int bonus, int targetNumber, out bool success, RollType alteration = RollType.FLAT)
        {
            int roll;
            if (alteration == RollType.ADV) roll = int.Max(Dice.D20(), Dice.D20());
            else if (alteration == RollType.DIS) roll = int.Min(Dice.D20(), Dice.D20());
            else roll = Dice.D20();
            success = (roll + bonus) >= targetNumber;
            return roll + bonus;
        }
        public static bool InReachMelee((int x, int y) attackerPos, (int x, int y) targetPos, int reach)
        {
            return Geometry.ChebyshevDist(attackerPos, targetPos) <= reach;
        }

        public static bool InReachRanged((int x, int y) attackerPos, (int x, int y) targetPos, int reach)
        {
            return Geometry.EuclidDist(attackerPos, targetPos) <= reach;
        }

        public static int RollMagnitude(string diceExpr, int modifier)
        {
            return Dice.Roll(diceExpr) + modifier; // e.g. "1d8"
        }

        public static void ApplyDamage(StatBlock target, int amount)
        {
            target.CurrentHP = Math.Max(0, target.CurrentHP - amount);
        }

        public static void ApplyHealing(StatBlock target, int amount)
        {
            target.CurrentHP = Math.Min(target.MaxHP, target.CurrentHP + amount);
        }


        public static void AddMovement(StatBlock target, int amount)
        {
            target.MovementRemaining += amount;
        }

        public static void Move(StatBlock target, (int, int) destination)
        {
            target.Position = destination;
        }

        public static void ProneSwitch(StatBlock target)
        {
            if (target.Conditions.Contains(Condition.Prone)) target.Conditions.Remove(Condition.Prone);
            else target.Conditions.Add(Condition.Prone);
        }
    }


    public static class ConditionRules
    {
        public enum RollContext { MeleeAttackAsAttacker, MeleeAttackAsTarget, RangedAttackAsAttacker, RangedAttackAsTarget }

        public static readonly Dictionary<(Condition condition, RollContext context), RollType> ConditionEffects = new()
        {
            { (Condition.Prone, RollContext.MeleeAttackAsTarget), RollType.ADV },     // attacker gets advantage vs prone target in melee
            { (Condition.Prone, RollContext.RangedAttackAsTarget), RollType.DIS }, // attacker gets disadvantage vs prone target at range
            { (Condition.Prone, RollContext.MeleeAttackAsAttacker), RollType.DIS }, // attacker gets disadvantage when prone
            { (Condition.Prone, RollContext.RangedAttackAsAttacker), RollType.DIS },
            { (Condition.Poisoned, RollContext.MeleeAttackAsAttacker), RollType.DIS }, // attacker gets disadvantage when poisoned
            { (Condition.Poisoned, RollContext.RangedAttackAsAttacker), RollType.DIS },
            { (Condition.Blinded, RollContext.MeleeAttackAsTarget), RollType.ADV },     // attacker gets advantage vs blinded target
            { (Condition.Blinded, RollContext.RangedAttackAsTarget), RollType.ADV },
            { (Condition.Blinded, RollContext.MeleeAttackAsAttacker), RollType.DIS }, // attacker gets disadvantage when blinded
            { (Condition.Blinded, RollContext.RangedAttackAsAttacker), RollType.DIS },
            // ...
        };

        public static RollType GetAttackRollType(StatBlock attacker, StatBlock target, RollContext attackerContext)
        {
            // Shove acts like a Melee Attack for target lookup context
            RollContext targetContext = (attackerContext == RollContext.RangedAttackAsAttacker)
                ? RollContext.RangedAttackAsTarget
                : RollContext.MeleeAttackAsTarget;

            bool hasAdv = false;
            bool hasDis = false;

            // Check attacker's conditions
            foreach (var cond in attacker.Conditions)
            {
                if (ConditionEffects.TryGetValue((cond, attackerContext), out var effect))
                {
                    if (effect == RollType.ADV) hasAdv = true;
                    if (effect == RollType.DIS) hasDis = true;
                }
            }

            // Check target's conditions
            foreach (var cond in target.Conditions)
            {
                if (ConditionEffects.TryGetValue((cond, targetContext), out var effect))
                {
                    if (effect == RollType.ADV) hasAdv = true;
                    if (effect == RollType.DIS) hasDis = true;
                }
            }

            return Resolution.CombineAdvantage(hasAdv, hasDis);
        }
    }
    public abstract class CombatAction
    {
        public string Name;
        public abstract ActionResult Resolve(StatBlock attacker, StatBlock target);
    }
    public static class GenericActions
    {
        public static readonly CombatAction Dash = new Dash("Dash");
        public static readonly CombatAction Shove = new Shove("Shove");


    }


    public class Dash : CombatAction
    {
        public Dash(string name)
        {
            Name = name;
        }
        public override ActionResult Resolve(StatBlock attacker, StatBlock target)
        {
            Resolution.AddMovement(attacker, attacker.WalkSpeed);
            return new ActionResult { ActionName = "Dash", Success = true, AttackerName = attacker.Name, TargetName = attacker.Name, };
        }
    }

    public class Shove : CombatAction
    {
        public int Reach = 1;
        public Shove(string name)
        {
            Name = name;
        }
        public override ActionResult Resolve(StatBlock attacker, StatBlock target)
        {
            if (!Resolution.InReachMelee(attacker.Position, target.Position, Reach))
            {
                return ActionResult.Fail("Shove", "Out of reach.");
            }
            int DC = 8 + target.STRMod + target.PB;

            RollType rollType = ConditionRules.GetAttackRollType(attacker, target, RollContext.MeleeAttackAsAttacker);
            Resolution.RollAgainst(attacker.STRMod + attacker.PB, DC, out bool hit, rollType);
            if (!hit)
                return ActionResult.Fail("Shove", "Missed.", DC);
            (int x, int y) direction = Geometry.SignedDist(target.Position, attacker.Position);
            (int, int) destination = TupleComponentMath.Add(target.Position, direction);
            Resolution.Move(target, destination);
            return ActionResult.Ok("Shove", attacker.Name, target.Name, 1, DC);
        }
    }

    public class MeleeAttack : CombatAction
    {
        public int AttackBonus;
        public int Reach;
        public string DamageDice;
        public int DamageModifier;

        public MeleeAttack(string Name, int reach, int atk, string diceExpr, int dmgMod)
        {
            this.Name = Name;
            Reach = reach;
            AttackBonus = atk;
            DamageDice = diceExpr;
            DamageModifier = dmgMod;
        }

        public override ActionResult Resolve(StatBlock attacker, StatBlock target)
        {
            if (!Resolution.InReachMelee(attacker.Position, target.Position, Reach))
                return ActionResult.Fail(Name, "Out of reach.");

            RollType rollType = ConditionRules.GetAttackRollType(attacker, target, RollContext.MeleeAttackAsAttacker);
            Resolution.RollAgainst(AttackBonus, target.AC, out bool hit, rollType);
            if (!hit)
                return ActionResult.Fail(Name, "Missed.", target.AC);
            int dmg = Resolution.RollMagnitude(DamageDice, DamageModifier);
            Resolution.ApplyDamage(target, dmg);
            return ActionResult.Ok(Name, attacker.Name, target.Name, dmg, target.AC);
        }
    }
    public class RangedAttack : CombatAction
    {
        public int AttackBonus;
        public int Reach;
        public string DamageDice;
        public int DamageModifier;

        public RangedAttack(string Name, int reach, int atk, string diceExpr, int dmgMod)
        {
            this.Name = Name;
            Reach = reach;
            AttackBonus = atk;
            DamageDice = diceExpr;
            DamageModifier = dmgMod;
        }
        public override ActionResult Resolve(StatBlock attacker, StatBlock target)
        {
            if (!Resolution.InReachRanged(attacker.Position, target.Position, Reach))
                return ActionResult.Fail(Name, "Out of reach.");

            RollType rollType = ConditionRules.GetAttackRollType(attacker, target, RollContext.RangedAttackAsAttacker);
            Actions.Resolution.RollAgainst(AttackBonus, target.AC, out bool hit, rollType);
            if (!hit)
                return ActionResult.Fail(Name, "Missed.", target.AC);
            int dmg = Resolution.RollMagnitude(DamageDice, DamageModifier);
            Resolution.ApplyDamage(target, dmg);
            return ActionResult.Ok(Name, attacker.Name, target.Name, dmg, target.AC);
        }
    }
    public class SingleTargetHeal : CombatAction
    {
        public string HealDice;
        public int HealModifier;
        public int Reach;

        public SingleTargetHeal(string Name, int reach, string diceExpr, int mod)
        {
            this.Name = Name;
            Reach = reach;
            HealDice = diceExpr;
            HealModifier = mod;
        }

        public override ActionResult Resolve(StatBlock attacker, StatBlock target)
        {
            if (!Resolution.InReachRanged(attacker.Position, target.Position, Reach))
                return ActionResult.Fail(Name, "Out of reach.");
            int heal = Resolution.RollMagnitude(HealDice, HealModifier);
            Resolution.ApplyHealing(target, heal);
            return ActionResult.Ok(Name, attacker.Name, target.Name, heal);
        }
    }
}
