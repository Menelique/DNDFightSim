using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;

namespace FightSim
{
    public static class MouseInput
    {
        private static MouseState _currentMouse;
        private static MouseState _previousMouse;

        public static Point Position => _currentMouse.Position;
        public static Vector2 PositionVector => _currentMouse.Position.ToVector2();
        public static Point DeltaPosition => _currentMouse.Position - _previousMouse.Position;

        public static int ScrollWheelDelta => _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

        public static void Update()
        {
            _previousMouse = _currentMouse;
            _currentMouse = Mouse.GetState();
        }

        public static bool WasLeftJustClicked()
        {
            return _currentMouse.LeftButton == ButtonState.Pressed &&
                _previousMouse.LeftButton == ButtonState.Released;
        }
        public static bool WasRightJustClicked()
        {
            return _currentMouse.RightButton == ButtonState.Pressed &&
                _previousMouse.RightButton == ButtonState.Released;
        }

        public static bool IsLeftDown()
        {
            return _currentMouse.LeftButton == ButtonState.Pressed;
        }

        public static bool IsLeftHeld()
        {
            return _currentMouse.LeftButton == ButtonState.Pressed &&
                _previousMouse.LeftButton == ButtonState.Pressed;
        }

        public static bool IsHovering(Rectangle bounds)
        {
            return bounds.Contains(Position);
        }
    }
}
