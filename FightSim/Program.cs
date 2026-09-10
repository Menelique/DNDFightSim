using FightSim.Actions;
using FightSim.SimMath;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using static FightSim.Actions.Resolution;

namespace FightSim.SimMath
{
    
    static class TupleComponentMath
    {
        public static (int, int) Add((int x, int y) t1, (int x, int y )t2)
        {
            return (t1.x + t2.x, t1.y + t2.y);
        }
    }
    static class Geometry
    {
        public const int FEET_PER_SQUARE = 5;
        public static (int dx, int dy) ComponentDist((int x, int y) pos1, (int x, int y) pos2)
        {
            return (Math.Abs(pos1.x - pos2.x), Math.Abs(pos1.y - pos2.y));
        }

        public static (int dx, int dy) SignedDist((int x, int y) pos1, (int x, int y) pos2)
        {
            return (pos1.x - pos2.x, pos1.y - pos2.y);
        }

        public static int ChebyshevDist((int x, int y) pos1, (int x, int y) pos2)
        {
            (int dx, int dy) compdist = ComponentDist(pos1, pos2);
            return int.Max(compdist.dx, compdist.dy);
        }

        public static double EuclidDist((int x, int y) pos1, (int x, int y) pos2)
        {
            (int dx, int dy) compdist = ComponentDist(pos1, pos2);
            return Math.Sqrt((compdist.dx * compdist.dx) + (compdist.dy * compdist.dy));
        }

        public static int EuclidDistWhole((int x, int y) pos1, (int x, int y) pos2)
        {
            return (int)double.Floor(EuclidDist(pos1, pos2));
        }
    }
    static class Dice
    {
        static Random rng = new Random();
        public static int D20()
        {
            return rng.Next(1, 21);
        }

        public static int Roll(string diceExpr)
        {
            string[] groups = diceExpr.Split('+');
            int total = 0;
            foreach (string die in groups)
            {
                var parts = die.Split("d");
                int count = int.Parse(parts[0]);
                int sides = int.Parse(parts[1]);
                for (int i = 0; i < count; i++) total += rng.Next(1, sides + 1);
            }
            return total;
        }
    }

}



namespace FightSim
{
    public enum Ability { STR, DEX, CON, INT, WIS, CHA }
    public enum Condition { Prone, Restrained, Poisoned, Blinded, Grappled, Incapacitated }

    public class StatBlock
    {
        public string Name;
        public (int x, int y) Position { get; set; }

        public int WalkSpeed { get; set; } // counted in grid squares, not feet
        public int MovementRemaining { get; set; }
        public int MaxHP {  get; set; }
        public int CurrentHP { get; set; }
        public int BaseAC { get; set;  }
        public List<int> ACModifiers { get; set; } = new();
        public int AC => BaseAC + ACModifiers.Sum();
        public List<CombatAction> Actions { get; set; }
        public int PB { get; set; }
        public enum Ability { STR, DEX, CON, INT, WIS, CHA }
        public bool ActionSpent { get; set; }

        public Dictionary<Ability, int> Scores { get; set; } = new();

        public int STRMod => (int)Math.Floor((Scores[Ability.STR] - 10) / 2.0);
        public int DEXMod => (int)Math.Floor((Scores[Ability.DEX] - 10) / 2.0);
        public int CONMod => (int)Math.Floor((Scores[Ability.CON] - 10) / 2.0);
        public int INTMod => (int)Math.Floor((Scores[Ability.INT] - 10) / 2.0);
        public int WISMod => (int)Math.Floor((Scores[Ability.WIS] - 10) / 2.0);
        public int CHAMod => (int)Math.Floor((Scores[Ability.CHA] - 10) / 2.0);

        public int BaseInitiativeBonus => DEXMod; // default, rolled fresh each combat anyway
        public List<int> InitiativeModifiers { get; set; } = new();
        public int InitiativeBonus => BaseInitiativeBonus + InitiativeModifiers.Sum();

        public int? InitiativeRoll { get; set; }
        public void RollInitiative()
        {
            InitiativeRoll = Dice.D20() + InitiativeBonus;
        }
        public void ClearInitiative()
        {
            InitiativeRoll = null;
        }
        public HashSet<Condition> Conditions { get; set; } = new();

        public StatBlock(string name, (int, int) pos, int hp, int ac, int pb,
    int str, int dex, int con, int intl, int wis, int cha, int speed = 6)
        {
            Name = name;
            Position = pos;
            MaxHP = hp;
            CurrentHP = hp;
            BaseAC = ac;
            PB = pb;
            Scores = new Dictionary<Ability, int>
            {
                { Ability.STR, str }, { Ability.DEX, dex }, { Ability.CON, con },
                { Ability.INT, intl }, { Ability.WIS, wis }, { Ability.CHA, cha }
            };
            Actions = new List<CombatAction>();
            WalkSpeed = speed;

        }
        public StatBlock(StatBlock template, (int, int) position, string newName = "")
        {
            if (newName != "") Name = newName;
            else Name = template.Name;
            Position = position;
            MaxHP = template.MaxHP;
            CurrentHP = template.MaxHP;
            BaseAC = template.BaseAC;
            ACModifiers = new List<int>(template.ACModifiers);
            PB = template.PB;
            Scores = new Dictionary<Ability, int>(template.Scores);
            Actions = new List<CombatAction>(template.Actions);
            WalkSpeed = template.WalkSpeed;
            Conditions = new HashSet<Condition>();
        }

        public void AddAction(CombatAction action)
        {
            Actions.Add(action);
        }

        public void AddMeleeAttack(string Name, int reach, string DamageDice)
        {
            Actions.Add(new MeleeAttack(Name, reach, STRMod + PB, DamageDice, STRMod));
        }

        public void AddFinesseAttack(string Name, int reach, string DamageDice)
        {
            Actions.Add(new MeleeAttack(Name, reach, DEXMod + PB, DamageDice, DEXMod));
        }

        public void AddRangedAttack(string Name, int reach, string DamageDice)
        {
            Actions.Add(new RangedAttack(Name, reach, DEXMod + PB, DamageDice, DEXMod));
        }
    }


    static class UserInterface
    {
        public static string ReportResult(ActionResult result)
        {
            if (!result.Success)
            {
                return $"{result.AttackerName}'s {result.ActionName} failed. Reason: {result.FailureReason}";
            }
            else
            {
                return $"{result.AttackerName}'s {result.ActionName} successfully worked for {result.Magnitude}";
            }
        }
        public static string PassTurn(StatBlock combatant)
        {
            return $"{combatant.Name} is out of movement and actions or ended on their own, passing turn.";
        }
        public static string ReportMove(StatBlock combatant, MoveResult result)
        {
            if (!result.Success)
            {
                return $"{combatant.Name} failed to move. {result.FailureReason}.";
            }
            return $"{combatant.Name} moved {result.Distance * Geometry.FEET_PER_SQUARE} feet to {result.Destination}.";
        }
    }
    public struct MoveResult
    {
        public bool Success;
        public (int x, int y) Destination;
        public int Distance;
        public string FailureReason;
    }
    class Program
    {
        public static MoveResult TryMove(StatBlock combatant, (int, int) destination)
        {
            if (combatant.Position == destination)
            {
                int proneCost = combatant.WalkSpeed / 2;
                if (combatant.MovementRemaining < proneCost)
                    return new MoveResult { Success = false, FailureReason = "Not enough movement." };

                Resolution.ProneSwitch(combatant);
                combatant.MovementRemaining -= proneCost;
                return new MoveResult { Success = true, Destination = destination, Distance = 0 };
            }

            int distance = Geometry.ChebyshevDist(combatant.Position, destination);
            if (combatant.Conditions.Contains(Condition.Prone)) distance *= 2;
            if (distance > combatant.MovementRemaining)
                return new MoveResult { Success = false, FailureReason = "Not enough movement." };

            combatant.Position = destination;
            combatant.MovementRemaining -= distance;
            return new MoveResult { Success = true, Destination = destination, Distance = distance };
        }

        public static List<StatBlock> OrderByInitiative(List<StatBlock> combatants)
        {
            foreach (StatBlock combatant in combatants) combatant.RollInitiative();
            return combatants.OrderByDescending(c => c.InitiativeRoll)
                  .ThenByDescending(c => c.DEXMod)
                  .ToList();
        }

        static void Main(string[] args)
        {
            using var game = new Game1();
            game.Run();
        }
    }
}

