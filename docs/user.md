# User documentation - FightSim
 
## Overview
 
FightSim is a turn-based tactical combat simulator modeled on Dungeons & Dragons 5th Edition. It runs a fight between a fixed set of combatants (a mix of player-character-style and monster-style statblocks) on a square grid, and lets you control each combatant's actions and movement in turn until only one side is left standing.
 
This is not a full tabletop simulator or campaign tool — it only handles the combat encounter itself, from the moment initiative is rolled to the moment one combatant remains.
 
## Requirements
 
- .NET 9 SDK or later
- No other software is required to run a pre-built copy of the program. Building from source requires Visual Studio or the .NET SDK, see the README.
## Running the game
 
Run the program (via Visual Studio's Run button, or `dotnet run` from the project folder). A window opens immediately and the fight begins — there is no menu or setup screen. Which combatants take part in the fight is fixed in advance; see "Changing the combatants" below if you want to edit this yourself.
 
## Screen layout
 
- **Left sidebar:** the initiative order. Every combatant is listed top to bottom in the order they act, each showing their name, rolled initiative, and a current/max HP display with an HP bar underneath.
- **Center (battle map):** a grid showing every combatant as a colored circle with their initial letter inside it. Combatants who have dropped to 0 HP are shown grayed out.
- **Right sidebar:** information and controls for whoever's turn it currently is — their name/HP/initiative at the top, remaining movement, a list of buttons for their available actions, a Move button, and an End Turn button.
- **Bottom bar:** an event log showing the three most recent things that happened (attacks, misses, movement, heals, turn changes).
## Controls
 
- **Action buttons (right sidebar):** click one of the listed actions to select it. If the action needs a target (an attack, a heal, Shove), you will then be asked to click the combatant you want to target on the battle map — click their token. Right-click at any point while choosing a target to cancel and go back to the action list.
- **Move button:** click it, then click a square on the battle grid to move toward. The move only succeeds if the combatant has enough movement remaining; moving costs one square of movement per grid square of distance travelled (diagonal moves also cost one square, matching standard D&D movement rules). Right-click to cancel before choosing a square.
- **Standing up / dropping prone:** clicking the square a combatant is already standing on toggles their prone status, costing half their movement.
- **End Turn button:** ends the current combatant's turn immediately, regardless of remaining movement or actions.
- **Panning the map:** click and drag anywhere on the battle map (away from a token) to scroll the view.
- **Scrolling the log:** use the mouse wheel while hovering over the event log at the bottom to scroll through older messages.
- Each combatant gets exactly one action per turn. Once an action is used, the action buttons disappear for the rest of that turn, but movement can still be used (and the turn does not end automatically unless movement also runs out).
## Combat flow
 
Combatants act in initiative order (highest first, ties broken by Dexterity). On a combatant's turn, you (playing every side) choose their action and movement freely — there is no separate "AI" for monsters versus player characters, since the whole point of the underlying design is that every combatant, whether a goblin or a wizard, is handled identically by the game's rules engine. When only one combatant remains with HP above 0, the screen changes to a green "GAME END" screen announcing the winner.
 
## Changing the combatants
 
The set of combatants in each fight, their stats, and their actions are defined in the source file `CombatantSetup.cs`. This file is intentionally kept separate from the rest of the program specifically so it can be edited without needing to understand how the simulator itself works. Near the bottom of that file is a clearly marked line where the list of combatants taking part in the fight is chosen — editing which names appear in that list changes who shows up. Adding a new monster or character means declaring a new `StatBlock` following the same pattern as the existing examples (name, starting position, HP, AC, proficiency bonus, and the six ability scores, followed by lines adding attacks or other actions). Comments in the file give a worked example for creating a second instance of an existing template (e.g. a second goblin) without redefining all of its stats from scratch.
