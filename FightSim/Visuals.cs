using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace FightSim.Graphics
{
    public static class DrawHelpers
    {
        static Texture2D pixel;

        public static Texture2D GetPixel(SpriteBatch spriteBatch)
        {
            if (pixel == null)
            {
                pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                pixel.SetData(new[] { Color.White });
            }
            return pixel;
        }

        public static Vector2 GridToScreen((int x, int y) gridPos, int squareSize, Camera camera, Vector2 localOrigin)
        {
            return camera.BoardOrigin + localOrigin + new Vector2(gridPos.x * squareSize, gridPos.y * squareSize);
        }

        public static (int x, int y) ScreenToGrid(Point screenPos, int squareSize, Camera camera, Vector2 localOrigin)
        {
            Vector2 origin = camera.BoardOrigin + localOrigin;

            int gridX = (int)Math.Floor((screenPos.X - origin.X) / squareSize);
            int gridY = (int)Math.Floor((screenPos.Y - origin.Y) / squareSize);

            return (gridX, gridY);
        }

        public static Texture2D GetCircle(GraphicsDevice device, int diameter, Color color)
        {
            Texture2D texture = new Texture2D(device, diameter, diameter);
            Color[] data = new Color[diameter * diameter];
            float radius = diameter / 2f;
            Vector2 center = new Vector2(radius, radius);
            for (int i = 0; i < data.Length; i++)
            {
                int x = i % diameter;
                int y = i / diameter;
                float dist = Vector2.Distance(new Vector2(x, y), center);
                data[i] = dist <= radius ? color : Color.Transparent;
            }
            texture.SetData(data);
            return texture;
        }

        public static void DrawCircle(this SpriteBatch spriteBatch, Texture2D circleTexture, Vector2 position, Color color)
        {
            Vector2 origin = new Vector2(circleTexture.Width / 2f, circleTexture.Height / 2f);
            spriteBatch.Draw(circleTexture, position, null, color, 0f, origin, 1f, SpriteEffects.None, 0f);
        }

        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color, float thickness = 1f)
        {
            var distance = Vector2.Distance(point1, point2);
            var angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            var origin = new Vector2(0f, 0.5f);
            var scale = new Vector2(distance, thickness);
            spriteBatch.Draw(GetPixel(spriteBatch), point1, null, color, angle, origin, scale, SpriteEffects.None, 0);
        }

        public static void DrawGrid(this SpriteBatch spriteBatch, Camera camera, int squareSize, Color color, Rectangle bounds)
        {
            float startX = (camera.BoardOrigin.X + bounds.Left) % squareSize;
            if (startX < 0) startX += squareSize;

            for (float x = startX; x <= bounds.Right; x += squareSize)
                if (x >= bounds.Left)
                    spriteBatch.DrawLine(new Vector2(x, bounds.Top), new Vector2(x, bounds.Bottom), color);

            float startY = (camera.BoardOrigin.Y + bounds.Top) % squareSize;
            if (startY < 0) startY += squareSize;

            for (float y = startY; y <= bounds.Bottom; y += squareSize)
                if (y >= bounds.Top)
                    spriteBatch.DrawLine(new Vector2(bounds.Left, y), new Vector2(bounds.Right, y), color);
        }

        public static void DrawCoords(this SpriteBatch spriteBatch, Camera camera, int squareSize, SpriteFont font, Rectangle bounds)
        {
            int firstCol = (int)Math.Floor((-camera.BoardOrigin.X - bounds.Left) / squareSize);
            int lastCol = (int)Math.Ceiling((bounds.Right - camera.BoardOrigin.X - bounds.Left) / squareSize);
            int firstRow = (int)Math.Floor((-camera.BoardOrigin.Y - bounds.Top) / squareSize);
            int lastRow = (int)Math.Ceiling((bounds.Bottom - camera.BoardOrigin.Y - bounds.Top) / squareSize);

            for (int x = firstCol; x <= lastCol; x++)
            {
                for (int y = firstRow; y <= lastRow; y++)
                {
                    Vector2 pos = GridToScreen((x, y), squareSize, camera, new Vector2(bounds.Left, bounds.Top));
                    if (bounds.Contains(pos.ToPoint()))
                        spriteBatch.DrawString(font, $"{x},{y}", pos, Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                }
            }
        }
    }

    public class Camera
    {
        public float XShift { get; set; } = 0;
        public float YShift { get; set; } = 0;
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }

        public Camera(int windowWidth, int windowHeight)
        {
            WindowWidth = windowWidth;
            WindowHeight = windowHeight;
        }

        public Vector2 BoardOrigin => new Vector2(WindowWidth / 2f + XShift, WindowHeight / 2f + YShift);
    }

    public interface IDrawable
    {
        void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Vector2 parentOrigin);
    }

    public class Button : IDrawable
    {
        public Vector2 Offset;
        public Point Size;
        public string Label;
        public Color BackgroundColor = Color.LightSlateGray;
        public Color TextColor = Color.Black;

        public Button(Vector2 offset, Point size, string label)
        {
            Offset = offset;
            Size = size;
            Label = label;
        }

        public bool Contains(Point point, Vector2 parentOrigin)
        {
            Vector2 absolute = parentOrigin + Offset;
            return new Rectangle(absolute.ToPoint(), Size).Contains(point);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Vector2 parentOrigin)
        {
            Vector2 absolute = parentOrigin + Offset;
            Rectangle bounds = new Rectangle(absolute.ToPoint(), Size);
            spriteBatch.Draw(pixel, bounds, BackgroundColor);
            Vector2 textSize = font.MeasureString(Label);
            Vector2 textPos = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(font, Label, textPos, TextColor);
        }
    }

    public class Bar : IDrawable
    {
        public Vector2 Offset;
        public Point Size;
        public Color FilledColor;
        public Color UnfilledColor;
        public float Fill; // 0 to 1

        public Bar(Vector2 offset, Point size, Color filledColor, Color unfilledColor, float fill)
        {
            Offset = offset;
            Size = size;
            FilledColor = filledColor;
            UnfilledColor = unfilledColor;
            Fill = fill;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Vector2 parentOrigin)
        {
            Vector2 absolute = parentOrigin + Offset;
            Rectangle bounds = new Rectangle(absolute.ToPoint(), Size);
            spriteBatch.Draw(pixel, bounds, UnfilledColor);
            Rectangle filledRect = new Rectangle(bounds.X, bounds.Y, (int)(bounds.Width * Fill), bounds.Height);
            spriteBatch.Draw(pixel, filledRect, FilledColor);
        }
    }

    public class Grid : IDrawable
    {
        public Camera Camera;
        public int SquareSize;
        public Color LineColor;
        public Vector2 Offset;
        public Point Size;
        public bool ShowCoords = false;

        public Grid(Camera camera, int squareSize, Color lineColor, Vector2 offset, Point size)
        {
            Camera = camera;
            SquareSize = squareSize;
            LineColor = lineColor;
            Offset = offset;
            Size = size;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Vector2 parentOrigin)
        {
            Vector2 absolute = parentOrigin + Offset;
            Rectangle bounds = new Rectangle(absolute.ToPoint(), Size);
            spriteBatch.DrawGrid(Camera, SquareSize, LineColor, bounds);
            if (ShowCoords)
                spriteBatch.DrawCoords(Camera, SquareSize, font, bounds);
        }
    }

    public class Token : IDrawable
    {
        public StatBlock Combatant;
        public Texture2D Circle;
        public int SquareSize;
        public Camera Camera;
        public string Initial;
        public Rectangle MapBounds;

        public Token(StatBlock combatant, Texture2D circle, int squareSize, Camera camera, Rectangle mapBounds)
        {
            Combatant = combatant;
            Circle = circle;
            SquareSize = squareSize;
            Camera = camera;
            Initial = combatant.Name?[..1];
            MapBounds = mapBounds;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Vector2 parentOrigin)
        {
            Vector2 screenPos = DrawHelpers.GridToScreen(Combatant.Position, SquareSize, Camera, parentOrigin);
            Vector2 alignment = new Vector2(SquareSize / 2f, SquareSize / 2f);
            if (!MapBounds.Contains((screenPos + alignment).ToPoint()))
                return;

            Color tint = Combatant.CurrentHP > 0 ? Color.White : Color.Gray;
            spriteBatch.DrawCircle(Circle, screenPos + alignment, tint);
            spriteBatch.DrawString(font, Initial, screenPos + alignment / 2f, Color.Black);
        }
    }

    public class TextLine : IDrawable 
    {
        public string Text;
        public Vector2 Offset;
        public Color Color = Color.White;

        public TextLine(string text, Vector2 offset)
        {
            Text = text;
            Offset = offset;

        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Vector2 parentOrigin)
        {
            spriteBatch.DrawString(font, Text, parentOrigin + Offset, Color);
        }
    }

    public class Box : IDrawable
    {
        public Vector2 Offset;
        public Point Size;
        public Color BackgroundColor = Color.Transparent;
        public List<IDrawable> Children = new();

        public Box(Vector2 offset, Point size)
        {
            Offset = offset;
            Size = size;
        }

        public bool Contains(Point point, Vector2 parentOrigin)
        {
            Vector2 absolute = parentOrigin + Offset;
            return new Rectangle(absolute.ToPoint(), Size).Contains(point);
        }

        public virtual void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Vector2 parentOrigin)
        {
            Vector2 absolute = parentOrigin + Offset;
            if (BackgroundColor != Color.Transparent)
                spriteBatch.Draw(pixel, new Rectangle(absolute.ToPoint(), Size), BackgroundColor);

            foreach (var child in Children)
                child.Draw(spriteBatch, pixel, font, absolute);
        }
    }

    public enum Side { Left, Right }

    public class Sidebar : Box
    {
        public Sidebar(int width, int windowWidth, int windowHeight, Side side)
            : base(
                new Vector2(side == Side.Left ? 0 : windowWidth - width, 0),
                new Point(width, windowHeight))
        { }
    }

    public class LogBox : Box
    {
        public int ScrollOffset = 0;
        const int VisibleLines = 3;
        const int LeftPadding = 5;

        public LogBox(Vector2 offset, Point size) : base(offset, size) { }

        public void AddLine(string line)
        {
            Children.Add(new TextLine(line, Vector2.Zero));
            ScrollOffset = 0;
        }

        public void Scroll(int direction)
        {
            ScrollOffset = Math.Clamp(ScrollOffset - direction, 0, Math.Max(0, Children.Count - VisibleLines));
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Vector2 parentOrigin)
        {
            Vector2 absolute = parentOrigin + Offset;
            Rectangle bounds = new Rectangle(absolute.ToPoint(), Size);
            spriteBatch.Draw(pixel, bounds, BackgroundColor);

            int startIndex = Math.Max(0, Children.Count - VisibleLines - ScrollOffset);
            int count = Math.Min(VisibleLines, Children.Count - startIndex);

            for (int i = 0; i < count; i++)
            {
                var line = (TextLine)Children[startIndex + i];
                line.Offset = new Vector2(LeftPadding, i * (Size.Y / (float)VisibleLines));
                line.Draw(spriteBatch, pixel, font, absolute);
            }
        }
    }

    public class BattleMap : Box
    {
        public Camera Camera;
        public Grid Grid;

        public BattleMap(Vector2 offset, Point size, Camera camera, Grid grid) : base(offset, size)
        {
            Camera = camera;
            Grid = grid;
            Children.Add(grid);
        }

        public void AddToken(Token token)
        {
            Children.Add(token);
        }
        
    }

    public class CombatantInitiativeBox : Box
    {
        public StatBlock Combatant;
        TextLine NameLine;
        TextLine InitLine;
        TextLine HpLine;
        Bar HpBar;

        public CombatantInitiativeBox(Vector2 offset, Point size, StatBlock combatant) : base(offset, size)
        {
            Combatant = combatant;

            NameLine = new TextLine(combatant.Name, new Vector2(5, 0)) { Color = Color.Black };
            InitLine = new TextLine("", new Vector2(size.X - 30, 0)) { Color = Color.Black };
            HpLine = new TextLine("", new Vector2(5, 20)) { Color = Color.Black };
            HpBar = new Bar(new Vector2(5, 40), new Point(size.X - 10, 8), Color.Red, Color.DarkRed, 1f);

            Children.Add(NameLine);
            Children.Add(InitLine);
            Children.Add(HpLine);
            Children.Add(HpBar);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Vector2 parentOrigin)
        {
            NameLine.Text = Combatant.Name;
            InitLine.Text = Combatant.InitiativeRoll?.ToString() ?? "";
            HpLine.Text = $"{Combatant.CurrentHP}/{Combatant.MaxHP}";
            HpBar.Fill = Combatant.MaxHP > 0 ? Combatant.CurrentHP / (float)Combatant.MaxHP : 0;


            base.Draw(spriteBatch, pixel, font, parentOrigin);
        }

        //Parts:
        // Name Line                    Initiative Roll
        // HPLine
        // -----------------HPBar----------------------- 

        
    }
}