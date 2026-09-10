using FightSim.Actions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using FightSim.Graphics;
using FightSim.States;
using FightSim.SimMath;
using System.Threading.Tasks;


namespace FightSim
{

    public partial class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        List<StatBlock> combatants;
        Texture2D pixel;
        Dictionary<StatBlock, Texture2D> tokens = new();
        const int TOKEN_NEGATIVE_PADDING = 2;
        Random colorRng = new();
        const int SQUARE_SIZE = 40;
        int sidebarWidth;
        Camera camera;
        SpriteFont font;
        Box root;
        BattleMap battleMap;
        private CombatStateMachine _stateMachine;
        Box actionBox;
        Button moveButton;
        Button endTurnButton;
        Vector2 rightSidebarOrigin;
        Vector2 ActionBoxOrigin;
        CombatantInitiativeBox rightSidebarStatBox;
        TextLine remainingMovement;


        void RebuildActionButtons(StatBlock combatant)
        {
            actionBox.Children.Clear();

            int buttonHeight = 30;

            if (!combatant.ActionSpent)
            {
                for (int i = 0; i < combatant.Actions.Count; i++)
                {
                    var action = combatant.Actions[i];
                    var button = new Button(new Vector2(0, i * (buttonHeight + 5)), new Point(sidebarWidth - 20, buttonHeight), action.Name);
                    actionBox.Children.Add(button);
                }
            }
        }
        void HandleActionSidebarClicks()
        {
            if (!MouseInput.WasLeftJustClicked())
                return;

            Point clickPos = MouseInput.Position;

            if (endTurnButton.Contains(clickPos, rightSidebarOrigin))
            {
                _stateMachine.TransitionTo(new EndTurnState());
                return;
            }

            if (moveButton.Contains(clickPos, rightSidebarOrigin))
            {
                _stateMachine.TransitionTo(new AwaitingTargetTileState());
                return;
            }

            for (int i = 0; i < actionBox.Children.Count; i++)
            {
                var button = (Button)actionBox.Children[i];
                if (button.Contains(clickPos, ActionBoxOrigin))
                {
                    var selectedAction = _stateMachine.ActiveCombatant.Actions[i];
                    _stateMachine.SelectedAction = selectedAction;

                    if (selectedAction is Dash || selectedAction.Name.Equals("Dash", StringComparison.OrdinalIgnoreCase))
                    {
                        _stateMachine.SelectedTargetCombatant = _stateMachine.ActiveCombatant;
                        _stateMachine.TransitionTo(new ResolvingActionState());
                    }
                    else
                    {
                        _stateMachine.TransitionTo(new AwaitingTargetCombatantState());
                    }
                    return;
                }
            }
        }


        Color RandomColor() => new Color(colorRng.Next(256), colorRng.Next(256), colorRng.Next(256));
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        void HandleMapPan()
        {
            if (MouseInput.IsLeftHeld() && battleMap.Contains(MouseInput.Position, Vector2.Zero))
            {
                Point delta = MouseInput.DeltaPosition;
                if (delta.X != 0 || delta.Y != 0)
                {
                    camera.XShift += delta.X;
                    camera.YShift += delta.Y;
                }
            }
        }
        void HandleLogboxScroll(LogBox LogBox)
        {
            if (LogBox.Contains(MouseInput.Position, Vector2.Zero))
            {
                int delta = MouseInput.ScrollWheelDelta;
                if (delta != 0)
                    LogBox.Scroll(delta > 0 ? -1 : 1);
            }
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            
            InitializeCombatants();
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();

            int windowWidth = _graphics.PreferredBackBufferWidth;
            int windowHeight = _graphics.PreferredBackBufferHeight;
            sidebarWidth = 250;
            camera = new Camera(windowWidth - sidebarWidth * 2, windowHeight);

            root = new Box(Vector2.Zero, new Point(windowWidth, windowHeight));

            var leftSidebar = new Sidebar(sidebarWidth, windowWidth, windowHeight, Side.Left);
            List<CombatantInitiativeBox> initiativeBoxes = new();
            int entryHeight = 60;

            for (int i = 0; i < combatants.Count; i++)
            {
                var box = new CombatantInitiativeBox(
                    new Vector2(0, i * entryHeight),
                    new Point(sidebarWidth, entryHeight),
                    combatants[i]);

                initiativeBoxes.Add(box);
                leftSidebar.Children.Add(box);

            }
            leftSidebar.BackgroundColor = Color.White;

            var grid = new Grid(camera, SQUARE_SIZE, Color.Black,
                Vector2.Zero,
                new Point(windowWidth - sidebarWidth * 2, windowHeight));

            battleMap = new BattleMap(new Vector2(sidebarWidth, 0),
                new Point(windowWidth - sidebarWidth * 2, windowHeight), camera, grid);

            var logBox = new LogBox(new Vector2(0, windowHeight - 80), new Point(windowWidth, 80));
            logBox.BackgroundColor = Color.DarkGray;

            _stateMachine = new CombatStateMachine(combatants, logBox);
            _stateMachine.BattleMapBounds = new Rectangle(sidebarWidth, 0, windowWidth - sidebarWidth * 2, windowHeight);
            _stateMachine.BattleMapLocalOrigin = new Vector2(sidebarWidth, 0);
            _stateMachine.Camera = camera;
            _stateMachine.SquareSize = SQUARE_SIZE;
            _stateMachine.TransitionTo(new TurnStartState());

            var rightSidebar = new Sidebar(sidebarWidth, windowWidth, windowHeight, Side.Right);
            actionBox = new Box(new Vector2(10, 200), new Point(sidebarWidth - 20, 200)); // position/size, tune to taste
            remainingMovement = new TextLine("", new Vector2(0, windowHeight - 190)) { Color = Color.Black };
            moveButton = new Button(new Vector2(10, windowHeight - 160), new Point(sidebarWidth - 20, 30), "Move");
            endTurnButton = new Button(new Vector2(10, windowHeight - 120), new Point(sidebarWidth - 20, 30), "End Turn");
            rightSidebarStatBox = new CombatantInitiativeBox(new Vector2(0, 0), new Point(sidebarWidth, entryHeight), _stateMachine.ActiveCombatant);
            rightSidebar.Children.Add(rightSidebarStatBox);

            rightSidebar.Children.Add(actionBox);
            rightSidebar.Children.Add(moveButton);
            rightSidebar.Children.Add(remainingMovement);
            rightSidebar.Children.Add(endTurnButton);

            root.Children.Add(leftSidebar);
            root.Children.Add(rightSidebar);
            root.Children.Add(battleMap);
            root.Children.Add(logBox);

            rightSidebarOrigin = rightSidebar.Offset;
            ActionBoxOrigin = rightSidebarOrigin + actionBox.Offset;

            rightSidebar.BackgroundColor = Color.White;

            base.Initialize();
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            pixel = DrawHelpers.GetPixel(_spriteBatch);

            // TODO: use this.Content to load your game content here
            font = Content.Load<SpriteFont>("YourFontName");
            foreach (var c in combatants) // assign colors to combatants
            {
                tokens[c] = DrawHelpers.GetCircle(GraphicsDevice, SQUARE_SIZE - TOKEN_NEGATIVE_PADDING, RandomColor());
                battleMap.AddToken(new Token(c, tokens[c], SQUARE_SIZE, camera, new Rectangle(sidebarWidth, 0, _graphics.PreferredBackBufferWidth - sidebarWidth * 2, _graphics.PreferredBackBufferHeight)));
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            MouseInput.Update();

            

            _stateMachine.Update();
            rightSidebarStatBox.Combatant = _stateMachine.ActiveCombatant;
            remainingMovement.Text = $"Movement: {_stateMachine.ActiveCombatant.MovementRemaining * Geometry.FEET_PER_SQUARE}";

            var alive = combatants.FindAll(c => c.CurrentHP > 0);
            if (alive.Count == 1 && !(_stateMachine.CurrentState is GameEndState))
            {
                _stateMachine.TransitionTo(new GameEndState(alive[0]));
            }

            if (_stateMachine.CurrentState is AwaitingActionState)
            {
                RebuildActionButtons(_stateMachine.ActiveCombatant);
                HandleActionSidebarClicks();
            }

            HandleMapPan();
            HandleLogboxScroll(_stateMachine.LogBox);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (_stateMachine.CurrentState is GameEndState endState)
            {
                GraphicsDevice.Clear(Color.LimeGreen);
                _spriteBatch.Begin();
                _spriteBatch.DrawString(font, "GAME END", new Vector2(400, 300), Color.Black, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(font, $"Winner: {endState.Winner.Name}", new Vector2(400, 340), Color.Black);
                _spriteBatch.End();
                base.Draw(gameTime);
                return;
            }


            _spriteBatch.Begin();
            root.Draw(_spriteBatch, pixel, font, Vector2.Zero);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}