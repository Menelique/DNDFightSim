using FightSim.Actions;
using FightSim.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FightSim.States
{
    public interface ICombatState
    {
        void Enter(CombatStateMachine machine);
        void Update(CombatStateMachine machine);
    }
    public class CombatStateMachine
    {
        public List<StatBlock> Combatants {  get; set; }
        public int CurrentTurnIndex { get; set; } = 0;
        public StatBlock ActiveCombatant => Combatants[CurrentTurnIndex];

        // Transient Action Selections
        public CombatAction SelectedAction { get; set; }
        public StatBlock SelectedTargetCombatant { get; set; }
        public (int x, int y)? SelectedTargetTile { get; set; }
        

        public Rectangle BattleMapBounds { get; set; }
        public Vector2 BattleMapLocalOrigin { get; set; }
        public Camera Camera { get; set; }
        public int SquareSize { get; set; }


         
        public LogBox LogBox { get; private set; }

        public ICombatState CurrentState { get; private set; }

        public CombatStateMachine(List<StatBlock> combatants, LogBox logBox)
        {
            Combatants = combatants;
            LogBox = logBox;
            TransitionTo(new TurnStartState());
        }

        public void TransitionTo(ICombatState newState)
        {
            CurrentState = newState;
            CurrentState?.Enter(this);
        }
        public void Update()
        {
            CurrentState?.Update(this);
        }
    }

    public class TurnStartState : ICombatState
    {
        public void Enter(CombatStateMachine machine)
        {
            var current = machine.ActiveCombatant;

            // Skip dead
            if (current.CurrentHP <= 0)
            {
                machine.LogBox.AddLine($"{current.Name} is unconscious and skips their turn.");
                AdvanceToNextTurn(machine);
                return;
            }

            // resetting
            current.MovementRemaining = current.WalkSpeed;
            current.ActionSpent = current.Conditions.Contains(Condition.Incapacitated);
            machine.LogBox.AddLine($"--- {current.Name}'s Turn ---");

            // move to awaiting Action
            machine.TransitionTo(new AwaitingActionState());

        }

        public void Update(CombatStateMachine machine) { }

        private void AdvanceToNextTurn(CombatStateMachine machine)
        {
            machine.CurrentTurnIndex = (machine.CurrentTurnIndex + 1) % machine.Combatants.Count;
            machine.TransitionTo(new TurnStartState());
        }
    }
    public class AwaitingActionState : ICombatState
    {
        public void Enter(CombatStateMachine machine)
        {
            machine.SelectedAction = null;
            machine.SelectedTargetCombatant = null;
            machine.SelectedTargetTile = null;
            machine.LogBox.AddLine($"Select an action for {machine.ActiveCombatant.Name}.");
            if (machine.ActiveCombatant.MovementRemaining == 0 && machine.ActiveCombatant.ActionSpent) machine.TransitionTo(new EndTurnState());
        }

        public void Update(CombatStateMachine machine) { }
    }
    public class AwaitingTargetCombatantState : ICombatState
    {
        public void Enter(CombatStateMachine machine)
        {
            machine.SelectedTargetCombatant = null;
            machine.LogBox.AddLine($"Select a target combatant for {machine.SelectedAction.Name}. (Right-click to cancel)");
        }

        public void Update(CombatStateMachine machine)
        {
            // Right-click to cancel back to action selection
            if (MouseInput.WasRightJustClicked())
            {
                machine.TransitionTo(new AwaitingActionState());
                return;
            }

            if (MouseInput.WasLeftJustClicked())
            {
                Point clickPos = MouseInput.Position;

                // Ensure click is inside the battle map area
                if (machine.BattleMapBounds.Contains(clickPos))
                {
                    (int x, int y) targetTile = DrawHelpers.ScreenToGrid(
                        clickPos,
                        machine.SquareSize,
                        machine.Camera,
                        machine.BattleMapLocalOrigin
                    );

                    // Find if a combatant exists on that tile
                    StatBlock targetCombatant = machine.Combatants.Find(c => c.Position == targetTile);

                    if (targetCombatant != null)
                    {
                        machine.SelectedTargetCombatant = targetCombatant;
                        machine.TransitionTo(new ResolvingActionState());
                    }
                    else
                    {
                        machine.LogBox.AddLine("No combatant on that tile. Click a valid target.");
                    }
                }
            }
        }
    }
    public class AwaitingTargetTileState : ICombatState
    {
        public void Enter(CombatStateMachine machine)
        {
            machine.SelectedTargetTile = null;
            machine.LogBox.AddLine("Click a tile on the map to move. (Right-click to cancel)");
        }

        public void Update(CombatStateMachine machine)
        {
            // Right-click to cancel back to action selection
            if (MouseInput.WasRightJustClicked())
            {
                machine.TransitionTo(new AwaitingActionState());
                return;
            }

            if (MouseInput.WasLeftJustClicked())
            {
                Point clickPos = MouseInput.Position;

                if (machine.BattleMapBounds.Contains(clickPos))
                {
                    (int x, int y) targetTile = DrawHelpers.ScreenToGrid(
                        clickPos,
                        machine.SquareSize,
                        machine.Camera,
                        machine.BattleMapLocalOrigin
                    );

                    // Store target tile position and resolve
                    machine.SelectedTargetTile = targetTile;
                    machine.TransitionTo(new ResolvingMoveState());
                }
            }
        }
    }
    public class ResolvingMoveState : ICombatState
    {
        public void Enter(CombatStateMachine machine)
        {
            var active = machine.ActiveCombatant;

            if (machine.SelectedTargetTile.HasValue)
            {
                (int x, int y) destination = machine.SelectedTargetTile.Value;


                MoveResult result = Program.TryMove(active, destination);

                machine.LogBox.AddLine(UserInterface.ReportMove(active, result));
            }

            machine.SelectedTargetTile = null;
            machine.TransitionTo(new AwaitingActionState());
        }

        public void Update(CombatStateMachine machine) { }
    }
    public class ResolvingActionState : ICombatState
    {
        public void Enter(CombatStateMachine machine)
        {
            var active = machine.ActiveCombatant;
            var target = machine.SelectedTargetCombatant;
            var action = machine.SelectedAction;

            if (action != null && target != null)
            {
                ActionResult result = action.Resolve(active, target);

                machine.LogBox.AddLine(UserInterface.ReportResult(result));

                active.ActionSpent = true;
            }

            machine.SelectedAction = null;
            machine.SelectedTargetCombatant = null;

            machine.TransitionTo(new AwaitingActionState());
        }
        public void Update(CombatStateMachine machine) { }
    }
    public class EndTurnState : ICombatState
    {
        public void Enter(CombatStateMachine machine)
        {
            var active = machine.ActiveCombatant;
            machine.LogBox.AddLine(UserInterface.PassTurn(active));

            machine.CurrentTurnIndex = (machine.CurrentTurnIndex + 1) % machine.Combatants.Count;
            machine.TransitionTo(new TurnStartState());
        }

        public void Update(CombatStateMachine machine) { }
    }
    public class GameEndState : ICombatState
    {
        public StatBlock Winner { get; set; }
        public GameEndState(StatBlock winner)
        {
            Winner = winner;
        }
        public void Enter(CombatStateMachine machine) { }
        public void Update(CombatStateMachine machine) { }
    }
}
