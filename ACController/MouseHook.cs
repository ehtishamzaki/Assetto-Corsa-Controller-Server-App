using System.Runtime.InteropServices;
using System.Threading;

namespace ACController
{
    internal static class MouseHook
    {
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        #region API

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        #endregion

        #region Public Methods

        /// <summary>
        /// This simulates a left mouse click
        /// </summary>
        public static void LeftMouseClick(int xpos, int ypos)
        {
            GetCursorPos(out POINT point);
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(1000);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            SetCursorPos(point.X, point.Y);
        }

        /// <summary>
        /// This simulates a left mouse click
        /// </summary>
        public static void LeftMouseClick(System.Windows.Point pos)
        {
            GetCursorPos(out POINT point);
            SetCursorPos((int)pos.X, (int)pos.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(1000);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            SetCursorPos(point.X, point.Y);
        }

        #endregion

    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int X;
        public int Y;
    }

}
