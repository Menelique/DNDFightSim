using FightSim.Actions;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FightSim
{
    public partial class Game1 : Game
    {
        void InitializeCombatants()
        {
            StatBlock Goblin = new("Goblin", (0, 0), 7, 15, 2, 8, 14, 10, 10, 8, 8);
            Goblin.AddFinesseAttack("Scimitar", 1, "1d6");
            Goblin.AddRangedAttack("Shortbow", 16, "1d6");

            StatBlock Bugbear = new("Bugbear", (5, 5), 27, 16, 2, 15, 14, 13, 8, 11, 9);
            Bugbear.AddMeleeAttack("Morningstar", 2, "2d8");
            Bugbear.AddMeleeAttack("JavelinM", 2, "2d6");
            Bugbear.AddRangedAttack("JavelinR", 5, "1d6");
             
            StatBlock Acolyte = new("Acolyte", (2, 2), 9, 10, 2, 10, 10, 10, 10, 14, 11);
            Acolyte.AddMeleeAttack("Club", 1, "1d4");
            Acolyte.AddAction(new SingleTargetHeal("Cure Wounds", 2, "1d8", 2));

            StatBlock Allosaurus = new("Allosaurus", (-1, 0), 51, 13, 2, 19, 13, 17, 2, 12, 5, 12);
            Allosaurus.AddMeleeAttack("Bite", 1, "2d10");
            Allosaurus.AddMeleeAttack("Claws", 1, "1d8");

            StatBlock Baboon = new("Baboon", (3, 2), 3, 12, 2, 8, 14, 11, 4, 12, 6, 6);
            Baboon.AddMeleeAttack("Bite", 1, "1d4");

            StatBlock DraftHorse = new("Draft Horse", (10, 10), 15, 10, 2, 18, 10, 15, 2, 11, 7, 8);
            DraftHorse.AddMeleeAttack("Hooves", 1, "1d4");




            // To add more instances of the same monster, add a line like this:
            // formula: Statblock <variable name of instance> = new StatBlock(<Template StatBlock>, <Starting Position>, <string name of instance>);
            // example: StatBlock Gob2 = new StatBlock(Goblin, (-2, 0), "Second goblin");

            // adding new unique statblocks is explained in the docs.



            
            combatants = Program.OrderByInitiative(new List<StatBlock> {
            // Edit THIS LINE RIGHT BELOW to change what combatants show up.
                Goblin, Acolyte, Bugbear, DraftHorse
            });

            foreach (StatBlock combatant in combatants)
            {
                combatant.AddAction(GenericActions.Dash);
                combatant.AddAction(GenericActions.Shove);
            }
        }
    }
}
