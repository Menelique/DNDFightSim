# FightSim Credit program

## Specification

This project is a simplified, graphically minimal simulator for turn-based combat modeled on Dungeons & Dragons 5th Edition. It implements a unified data model for any combatant (player character or monster), a command-pattern action system for attacks, movement, and other combat actions, and a small MonoGame front end for running and watching a fight play out on a grid.

The scope is deliberately narrow: this is a combat resolver, not a full tabletop simulator. There is no spellcasting system, no reactions, no death saves, and only a small subset of conditions and their effects are implemented. See the "What Was Not Implemented" section of the programmer documentation for the full list of cuts and the reasoning behind each one.

## Installation and use

### Requirements

- .NET 9 SDK or later
- MonoGame (Desktop GL template)
- Visual Studio 2022+ or Visual Studio Code with the C# extension

### Installation

Clone or download the repository, then open the solution file in Visual Studio (or restore/build via `dotnet build` from the project root). MonoGame's NuGet dependencies restore automatically on first build.

### Running the Program

From Visual Studio, press Run/F5. From the command line, run `dotnet run` from the project directory. A window will open showing the battle grid, an initiative-order sidebar on the left, an action sidebar on the right, and an event log at the bottom. The fight begins immediately with a fixed set of combatants defined in `CombatantSetup.cs`.

## Documentation

* [User documentation](docs/user.md)
* [Examples](docs/examples.md)
* [Programmer documentation](docs/programmer.md)
