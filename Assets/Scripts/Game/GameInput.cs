using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace TargetToss.Game
{
    public enum PointerSource
    {
        Mouse,
        Touch,
        Gamepad,
    }

    /// <summary>
    /// Mouse, touch and gamepad through the Input System. The mouse and every new touch click where
    /// they are; a gamepad moves a cursor with either stick (or the d-pad) and clicks with A or a
    /// trigger. The keyboard and the gamepad also drive the menus.
    /// </summary>
    public sealed class GameInput
    {
        const float StickDeadZone = 0.15f;
        const float StickStep = 0.6f;

        readonly List<Vector2> clicks = new List<Vector2>();
        int stickVertical;
        int stickHorizontal;
        Vector2 lastMouse = new Vector2(float.NaN, float.NaN);
        int lastFrame = -1;

        static Keyboard Keys => Keyboard.current;

        /// <summary>Where the reticle is, in screen pixels.</summary>
        public Vector2 Cursor { get; private set; } = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        /// <summary>What moved the cursor or clicked last (the HUD hints follow it).</summary>
        public PointerSource Source { get; private set; } = PointerSource.Mouse;

        public bool UsingGamepad => Source == PointerSource.Gamepad;

        /// <summary>Gamepad cursor speed in screen heights per second.</summary>
        public float CursorSpeed { get; set; } = 1.3f;

        /// <summary>
        /// Reads the pointers once per frame: moves the cursor and returns the clicks (in screen
        /// pixels) made this frame.
        /// </summary>
        public IReadOnlyList<Vector2> ReadClicks(float deltaTime)
        {
            if (lastFrame == Time.frameCount)
            {
                return clicks;
            }
            lastFrame = Time.frameCount;
            clicks.Clear();

            // Touches first: some platforms also report a tap as a mouse click.
            bool touched = false;
            Touchscreen touch = Touchscreen.current;
            if (touch != null)
            {
                foreach (TouchControl finger in touch.touches)
                {
                    if (finger.press.wasPressedThisFrame)
                    {
                        Vector2 position = finger.position.ReadValue();
                        Cursor = position;
                        Source = PointerSource.Touch;
                        clicks.Add(position);
                        touched = true;
                    }
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 position = mouse.position.ReadValue();
                if (float.IsNaN(lastMouse.x))
                {
                    lastMouse = position;
                }
                if (!touched && (position - lastMouse).sqrMagnitude > 4f)
                {
                    Cursor = position;
                    Source = PointerSource.Mouse;
                }
                lastMouse = position;
                if (mouse.leftButton.wasPressedThisFrame && !touched)
                {
                    Cursor = position;
                    Source = PointerSource.Mouse;
                    clicks.Add(position);
                }
            }

            foreach (Gamepad pad in Gamepad.all)
            {
                Vector2 stick = pad.leftStick.ReadValue() + pad.rightStick.ReadValue() + pad.dpad.ReadValue();
                if (stick.sqrMagnitude > StickDeadZone * StickDeadZone)
                {
                    stick = Vector2.ClampMagnitude(stick, 1f);
                    // A gentle curve keeps small corrections precise.
                    stick *= stick.magnitude;
                    Vector2 moved = Cursor + stick * (CursorSpeed * Screen.height * deltaTime);
                    Cursor = new Vector2(Mathf.Clamp(moved.x, 0f, Screen.width), Mathf.Clamp(moved.y, 0f, Screen.height));
                    Source = PointerSource.Gamepad;
                }
                if (pad.buttonSouth.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame ||
                    pad.leftTrigger.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)
                {
                    Source = PointerSource.Gamepad;
                    clicks.Add(Cursor);
                }
            }
            return clicks;
        }

        /// <summary>Puts the gamepad cursor in the middle of the screen.</summary>
        public void CenterCursor() => Cursor = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        public bool Pause => (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.pKey.wasPressedThisFrame)) ||
            AnyPad(pad => pad.startButton.wasPressedThisFrame);

        public bool Mute => Keys != null && Keys.mKey.wasPressedThisFrame;

        public bool Confirm =>
            (Keys != null && (Keys.enterKey.wasPressedThisFrame || Keys.numpadEnterKey.wasPressedThisFrame || Keys.spaceKey.wasPressedThisFrame)) ||
            AnyPad(pad => pad.buttonSouth.wasPressedThisFrame);

        public bool Back =>
            (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.backspaceKey.wasPressedThisFrame)) ||
            AnyPad(pad => pad.buttonEast.wasPressedThisFrame);

        /// <summary>-1 for a step up a menu, +1 for a step down, 0 otherwise.</summary>
        public int VerticalStep()
        {
            int step = 0;
            if (Keys != null)
            {
                if (Keys.upArrowKey.wasPressedThisFrame || Keys.wKey.wasPressedThisFrame) step = -1;
                if (Keys.downArrowKey.wasPressedThisFrame || Keys.sKey.wasPressedThisFrame) step = 1;
            }
            if (step != 0)
            {
                return step;
            }
            return PadStep(true, ref stickVertical);
        }

        /// <summary>-1 for a step left through a setting, +1 for a step right, 0 otherwise.</summary>
        public int HorizontalStep()
        {
            int step = 0;
            if (Keys != null)
            {
                if (Keys.leftArrowKey.wasPressedThisFrame || Keys.aKey.wasPressedThisFrame) step = -1;
                if (Keys.rightArrowKey.wasPressedThisFrame || Keys.dKey.wasPressedThisFrame) step = 1;
            }
            return step != 0 ? step : PadStep(false, ref stickHorizontal);
        }

        int PadStep(bool vertical, ref int held)
        {
            int step = 0;
            int stickSign = 0;
            foreach (Gamepad pad in Gamepad.all)
            {
                if (vertical)
                {
                    if (pad.dpad.up.wasPressedThisFrame) step = -1;
                    if (pad.dpad.down.wasPressedThisFrame) step = 1;
                }
                else
                {
                    if (pad.dpad.left.wasPressedThisFrame) step = -1;
                    if (pad.dpad.right.wasPressedThisFrame) step = 1;
                }
                Vector2 stick = pad.leftStick.ReadValue();
                float axis = vertical ? -stick.y : stick.x;
                if (Mathf.Abs(axis) > StickStep)
                {
                    stickSign = axis > 0f ? 1 : -1;
                }
            }
            if (step == 0 && stickSign != 0 && stickSign != held)
            {
                step = stickSign;
            }
            held = stickSign;
            if (step != 0)
            {
                Source = PointerSource.Gamepad;
            }
            return step;
        }

        static bool AnyPad(System.Func<Gamepad, bool> test)
        {
            foreach (Gamepad pad in Gamepad.all)
            {
                if (test(pad))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
