using System;

namespace NewWidgets.UI
{
    public enum SpecialKey
    {
        None = 0,
        Menu = 1,
        Back = 2,
        Left = 3,
        Right = 4,
        Up = 5,
        Down = 6,
        Select = 7,
        Enter = 8,
        Tab = 9,

        Home = 10,
        End = 11,

        Slash,
        BackSlash,
        Semicolon,
        Quote,
        Comma,
        Period,
        Minus,
        Plus,
        BracketLeft,
        BracketRight,
        Tilde,
        Grave,
        Backspace,
        Delete,
        EraseLine, // combination of Ctrl+Backspace or Cmd+Backspace

        Letter,

        A,
        B,
        C,
        D,
        E,
        F,
        G,
        H,
        I,
        J,
        K,
        L,
        M,
        N,
        O,
        P,
        Q,
        R,
        S,
        T,
        U,
        V,
        W,
        X,
        Y,
        Z,

        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Zero,

        Shift,
        Paste,
        Control,

        Joystick_Up,
        Joystick_Down,
        Joystick_Left,
        Joystick_Right,
        Joystick_A,
        Joystick_B,
        Joystick_X,
        Joystick_Y,
        Joystick_LBumper,
        Joystick_RBumper,
        Joystick_Start,
        Joystick_Back,
        Joystick_RTrigger,
        Joystick_LTrigger,

        Accelerate,
        TurnLeft,
        TurnRight,
        Brake,

        Max
    }

    [Flags]
    public enum WindowObjectFlags
    {
        None = 0x00,
        Removing = 0x01,
        Visible = 0x02,
        Enabled = 0x04,
        Changed = 0x08,
        Selected = 0x10,
        Hovered = 0x20,

        Default = Visible | Enabled | Changed
    }

    public enum LabelAlign
    {
        Start = 0,
        Center = 1,
        End = 2
    }

    [Flags]
    public enum WindowFlags
    {
        None = 0,
        FullScreen = 0x01,
        CloseButton = 0x02,
        HelpButton = 0x04,
        MiscButton = 0x08,
        Controlling = 0x10,
        CustomAnim = 0x20,
        Blackout = 0x40,
        Focusable = 0x80,
        Focused = 0x100,
    }

}
