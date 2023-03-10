using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace translator
{
    /// <summary>
    /// Contains the values passed by the MouseHook to the internal MouseProc,
    /// repackaged as .NET types
    /// </summary>
    public class MouseHookEventArgs
    {
        /// <summary>
        /// The button that is associated with the event
        /// </summary>
        public readonly MouseButtons Button = MouseButtons.None;

        /// <summary>
        /// The X location of the mouse when the event occured
        /// </summary>
        public readonly int X = 0;

        /// <summary>
        /// The Y location of the mouse when the event occured
        /// </summary>
        public readonly int Y = 0;

        /// <summary>
        /// The control that will ultimately receive the hooked message
        /// </summary>
        public readonly Control Control = null;

        /// <summary>
        /// The window area that the mouse is over
        /// </summary>
        public readonly HitTestCode HitTestCode = HitTestCode.HTNOWHERE;

        private bool isNonClientArea = false;

        public MouseHookEventArgs(MouseButtons button, int x, int y, Control control, HitTestCode hitTestCode)
        {
            Button = button;
            X = x;
            Y = y;
            Control = control;
            HitTestCode = hitTestCode;
        }

        /// <summary>
        /// Did the event occur over a non-client area
        /// </summary>
        public bool IsNonClientArea
        {
            get { return isNonClientArea; }
        }

        internal void SetNonClient() { isNonClientArea = true; }
    }

    #region Class HookEventArgs
    public class HookEventArgs : EventArgs
    {
        public int HookCode;	// Hook code
        public IntPtr wParam;	// WPARAM argument
        public IntPtr lParam;	// LPARAM argument
    }
    #endregion

    #region Enum HookType
    // Hook Types
    // For other hook types, you can obtain these values from Winuser.h in the Microsoft SDK.
    public enum HookType : int
    {
        WH_JOURNALRECORD = 0,
        WH_JOURNALPLAYBACK = 1,
        WH_KEYBOARD = 2,
        WH_GETMESSAGE = 3,
        WH_CALLWNDPROC = 4,
        WH_CBT = 5,
        WH_SYSMSGFILTER = 6,
        WH_MOUSE = 7,
        WH_HARDWARE = 8,
        WH_DEBUG = 9,
        WH_SHELL = 10,
        WH_FOREGROUNDIDLE = 11,
        WH_CALLWNDPROCRET = 12,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }
    #endregion
    public class WinHook
    {
        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        //Declare the hook handle as an int.
        protected int hHook = 0;
        //Declare MouseHookProcedure as a HookProc type.
        protected HookProc hookProc = null;
        protected HookType hookType;

        #region event handler
        public delegate void HookEventHandler(object sender, HookEventArgs e);
        // ************************************************************************

        // ************************************************************************
        // Event: HookInvoked 
        public event HookEventHandler HookInvoked;
        protected void OnHookInvoked(HookEventArgs e)
        {
            if (HookInvoked != null)
                HookInvoked(this, e);
        }
        #endregion event handler
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hookType">Hook type</param>
        public WinHook(HookType hookType)
        {
            this.hookType = hookType;
            hookProc = new HookProc(this.CoreHookProc);
        }
        public WinHook(HookType hookType, HookProc func)
        {
            this.hookType = hookType;
            hookProc = func;
        }

        protected int CoreHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code < 0)
            {
                return CallNextHookEx(hHook, code, wParam, lParam);
            }
            // Let clients determine what to do
            HookEventArgs e = new HookEventArgs();
            e.HookCode = code;
            e.wParam = wParam;
            e.lParam = lParam;
            OnHookInvoked(e);

            // Yield to the next hook in the chain
            return CallNextHookEx(hHook, code, wParam, lParam);
        }
        /// <summary>
        /// install the hook function, real code is handled in HookInvoked event. 
        /// </summary>
        public void Install()
        {
            //var handle = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
            var handle = LoadLibrary("user32.dll");
            hHook = SetWindowsHookEx(
                     hookType,
                     hookProc,
                     handle,//Marshal.GetHINSTANCE( Assembly.GetExecutingAssembly().GetModules()[0])
                     0); //Thread.CurrentThread.ManagedThreadId
            //If SetWindowsHookEx fails.
            if (hHook == 0)
            {
                //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                int errorCode = Marshal.GetLastWin32Error();
                //do cleanup

                //Initializes and throws a new instance of the Win32Exception class with the specified error. 

                throw new Win32Exception(errorCode);

            }
            Debug.WriteLine("Install");
        }

        public void Uninstall()
        {
            UnhookWindowsHookEx(hHook);
            hHook = 0;
        }

        public bool IsInstalled
        {
            get { return hHook != 0; }
        }

        //Declare the wrapper managed POINT class.
        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;
        }

        //Declare the wrapper managed MouseHookStruct class.
        [StructLayout(LayoutKind.Sequential)]
        public class MouseHookStruct
        {
            public POINT pt;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        #region Win32 API imports
        //This is the Import for the SetWindowsHookEx function.
        //Use this function to install a thread-specific hook.
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int SetWindowsHookEx(HookType idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        //This is the Import for the UnhookWindowsHookEx function.
        //Call this function to uninstall the hook.
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);

        //This is the Import for the CallNextHookEx function.
        //Use this function to pass the hook information to the next hook procedure in chain.
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion Win32 API imports
    }
    /// <summary>
    /// .NET version of the NCHHITEST Win32 API enum
    /// </summary>
    public enum HitTestCode : int
    {
        HTERROR = -2,
        HTTRANSPARENT = -1,
        HTNOWHERE = 0,
        HTCLIENT = 1,
        HTCAPTION = 2,
        HTSYSMENU = 3,
        HTGROWBOX = 4,
        HTSIZE = HTGROWBOX,
        HTMENU = 5,
        HTHSCROLL = 6,
        HTVSCROLL = 7,
        HTMINBUTTON = 8,
        HTMAXBUTTON = 9,
        HTLEFT = 10,
        HTRIGHT = 11,
        HTTOP = 12,
        HTTOPLEFT = 13,
        HTTOPRIGHT = 14,
        HTBOTTOM = 15,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17,
        HTBORDER = 18,
        HTREDUCE = HTMINBUTTON,
        HTZOOM = HTMAXBUTTON,
        HTSIZEFIRST = HTLEFT,
        HTSIZELAST = HTBOTTOMRIGHT,
        HTOBJECT = 19,
        HTCLOSE = 20,
        HTHELP = 21,
    }

    public class MouseHook : WinHook, IDisposable
    {
        #region Construction
        /// <summary>
        /// This constructor does not AutoInstall istelf.
        /// Client code must call Install in order to begin receiving MouseEvents
        /// </summary>
        public MouseHook()
            : base(HookType.WH_MOUSE_LL)
        {
            // we provide our own callback function
            hookProc = new HookProc(this.MouseProc);
        }
        #endregion

        #region Disposal

        ~MouseHook()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (IsInstalled)
                Uninstall();

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region Events

        /// <summary>
        /// Delegate used when notifying clients of calls to the MouseProc
        /// </summary>
        public delegate void MouseHookEventHandler(object sender, MouseHookEventArgs e);

        public event MouseHookEventHandler MouseDown;
        protected void OnMouseDown(MouseHookEventArgs e)
        {
            if (MouseDown != null)
                MouseDown(this, e);
        }

        public event MouseHookEventHandler MouseUp;
        protected void OnMouseUp(MouseHookEventArgs e)
        {
            if (MouseUp != null)
                MouseUp(this, e);
        }

        public event MouseHookEventHandler MouseMove;
        protected void OnMouseMove(MouseHookEventArgs e)
        {
            if (MouseMove != null)
                MouseMove(this, e);
        }

        public event MouseHookEventHandler MouseDoubleClick;
        protected void OnMouseDoubleClick(MouseHookEventArgs e)
        {
            if (MouseDoubleClick != null)
                MouseDoubleClick(this, e);
        }
        #endregion
        #region variables fields
        private bool holdingMsg = false;
        // whether to prevent mouse down message broadcase out 
        public bool isHoldingMouseDownMsg
        {
            get { return holdingMsg; }
            set { holdingMsg = value; }
        }
        private bool holdingMoveMsg = false;
        // whether to prevent mouse move message broadcase out 
        public bool isHoldingMouseMoveMsg
        {
            get { return holdingMoveMsg; }
            set { holdingMoveMsg = value; }
        }
        #endregion
        #region Mouse Hook specific code
        /// <summary>
        /// The callback passed to SetWindowsHookEx
        /// </summary>
        /// <param name="code">The action code passed from the hook chain</param>
        /// <param name="wParam">The mouse message being received</param>
        /// <param name="lParam">Message paramters</param>
        /// <returns>return code to hook chain</returns>
        protected int MouseProc(int code, IntPtr wParam, IntPtr lParam)
        {

            // by convention a code < 0 means skip
            if (code < 0)
            {
                return CallNextHookEx(hHook, code, wParam, lParam);
            }

            if (isHoldingMouseDownMsg && (wParam.ToInt32() == Win32.WM_LBUTTONDOWN || wParam.ToInt32() == Win32.WM_RBUTTONDOWN || wParam.ToInt32() == Win32.WM_MBUTTONDOWN || wParam.ToInt32() == Win32.WM_XBUTTONDOWN))
            {
                RaiseMouseHookEvent(wParam, CrackHookMsg(wParam, (Win32.MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.MOUSEHOOKSTRUCT))));
                return 1;
            }
            if (isHoldingMouseMoveMsg && (wParam.ToInt32() == Win32.WM_MOUSEMOVE /*|| wParam.ToInt32() == Win32.WM_NCMOUSEMOVE*/))
            {
                RaiseMouseHookEvent(wParam, CrackHookMsg(wParam, (Win32.MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.MOUSEHOOKSTRUCT))));
                return 1;
            }
            // we're gonna ignore peek messages
            if (code == Win32.HC_ACTION)
            {
                RaiseMouseHookEvent(wParam, CrackHookMsg(wParam, (Win32.MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.MOUSEHOOKSTRUCT))));
            }
            // Yield to the next hook in the chain

            return CallNextHookEx(hHook, code, wParam, lParam);
        }

        /// <summary>
        /// Raises the appropriate MouseHook event
        /// </summary>
        /// <param name="wParam">The message ID passed to MouseProc</param>
        /// <param name="args">The repackaged arguments passed to MouseProc</param>
        private void RaiseMouseHookEvent(IntPtr wParam, MouseHookEventArgs args)
        {
            switch (wParam.ToInt32())
            {
                // mousemove messages
                case Win32.WM_MOUSEMOVE:
                    OnMouseMove(args);
                    break;
                case Win32.WM_NCMOUSEMOVE:
                    args.SetNonClient();
                    OnMouseMove(args);
                    break;

                // button down messages
                case Win32.WM_LBUTTONDOWN:
                case Win32.WM_RBUTTONDOWN:
                case Win32.WM_MBUTTONDOWN:
                case Win32.WM_XBUTTONDOWN:
                    OnMouseDown(args);
                    break;

                case Win32.WM_NCLBUTTONDOWN:
                case Win32.WM_NCRBUTTONDOWN:
                case Win32.WM_NCMBUTTONDOWN:
                case Win32.WM_NCXBUTTONDOWN:
                    args.SetNonClient();
                    OnMouseDown(args);
                    break;

                // button up messages
                case Win32.WM_LBUTTONUP:
                case Win32.WM_RBUTTONUP:
                case Win32.WM_MBUTTONUP:
                case Win32.WM_XBUTTONUP:
                    OnMouseUp(args);
                    break;

                case Win32.WM_NCLBUTTONUP:
                case Win32.WM_NCRBUTTONUP:
                case Win32.WM_NCMBUTTONUP:
                case Win32.WM_NCXBUTTONUP:
                    args.SetNonClient();
                    OnMouseUp(args);
                    break;

                // double click messages
                case Win32.WM_LBUTTONDBLCLK:
                case Win32.WM_RBUTTONDBLCLK:
                case Win32.WM_MBUTTONDBLCLK:
                case Win32.WM_XBUTTONDBLCLK:
                    OnMouseDoubleClick(args);
                    break;

                case Win32.WM_NCLBUTTONDBLCLK:
                case Win32.WM_NCRBUTTONDBLCLK:
                case Win32.WM_NCMBUTTONDBLCLK:
                case Win32.WM_NCXBUTTONDBLCLK:
                    args.SetNonClient();
                    OnMouseDoubleClick(args);
                    break;
            }
        }


        /// <summary>
        /// Digs the arguments that got passed to the MouseProc
        /// out of the HookEventArgs object, and repackages them
        /// as .NET types
        /// </summary>
        /// <param name="wParam">The mouse message id</param>
        /// <param name="Win32.MOUSEHOOKSTRUCT">Mouse message arguments</param>
        /// <returns>A repacked set of MouseProc arguments</returns>
        private MouseHookEventArgs CrackHookMsg(IntPtr wParam, Win32.MOUSEHOOKSTRUCT hookStruct)
        {
            //default the mouse button
            MouseButtons button = MouseButtons.None;

            //figure out which button the message belongs to
            switch (wParam.ToInt32())
            {
                //left button messages
                case Win32.WM_LBUTTONDBLCLK:
                case Win32.WM_LBUTTONDOWN:
                case Win32.WM_LBUTTONUP:
                case Win32.WM_NCLBUTTONDBLCLK:
                case Win32.WM_NCLBUTTONDOWN:
                case Win32.WM_NCLBUTTONUP:
                    button = MouseButtons.Left;
                    break;

                //right button messages
                case Win32.WM_RBUTTONDBLCLK:
                case Win32.WM_RBUTTONDOWN:
                case Win32.WM_RBUTTONUP:
                case Win32.WM_NCRBUTTONDBLCLK:
                case Win32.WM_NCRBUTTONDOWN:
                case Win32.WM_NCRBUTTONUP:
                    button = MouseButtons.Right;
                    break;

                //middle button messages
                case Win32.WM_MBUTTONDBLCLK:
                case Win32.WM_MBUTTONDOWN:
                case Win32.WM_MBUTTONUP:
                case Win32.WM_NCMBUTTONDBLCLK:
                case Win32.WM_NCMBUTTONDOWN:
                case Win32.WM_NCMBUTTONUP:
                    button = MouseButtons.Middle;
                    break;

                //x button messages
                case Win32.WM_XBUTTONDBLCLK:
                case Win32.WM_XBUTTONDOWN:
                case Win32.WM_XBUTTONUP:
                case Win32.WM_NCXBUTTONDBLCLK:
                case Win32.WM_NCXBUTTONDOWN:
                case Win32.WM_NCXBUTTONUP:
                    button = GetXButton();
                    break;
            }

            // create and return a MouseHookEventArgs
            Control control = Control.FromChildHandle(hookStruct.hwnd);
            return new MouseHookEventArgs(button, hookStruct.pt.x, hookStruct.pt.y, control, (HitTestCode)hookStruct.wHitTestCode);
        }


        /// <summary>
        /// The arguments sent to MouseProc don't specify which x button 
        /// was pressed, so we need to explicitly determine which button is down
        /// </summary>
        /// <returns>XButton1, XButton2, or None</returns>
        private MouseButtons GetXButton()
        {
            // there's a subtle bug in here in that if both
            // x buttons are pressed this will always return button1
            // even if button2 is the one that caused the message
            if (Win32.IsKeyDown(Win32.VK_XBUTTON2))
                return MouseButtons.XButton1;
            else
                if (Win32.IsKeyDown(Win32.VK_XBUTTON1))
                return MouseButtons.XButton2;

            return MouseButtons.None;
        }

        #endregion

        #region Win32 Imports
        private struct Win32
        {
            public struct MOUSEHOOKSTRUCT
            {
                public POINT pt;
                public IntPtr hwnd;
                public uint wHitTestCode;
                public IntPtr dwExtraInfo;
            }

            public struct POINT
            {
                public int x;
                public int y;
            }

            public static bool IsKeyDown(short KeyCode)
            {
                short state = GetKeyState(KeyCode);
                //Debug.WriteLine("state " + KeyCode + "," + (((ushort)state )));
                //return ((state & 0x10000) == 0x10000);
                return (state >> 7) != 0;
            }

            [DllImport("user32.dll")]
            public static extern short GetAsyncKeyState(int nVirtKey);

            [DllImport("user32.dll")]
            public static extern short GetKeyState(int nVirtKey);

            public static ushort LOWORD(int l) { return (ushort)(l); }
            public static ushort HIWORD(int l) { return (ushort)(((int)(l) >> 16) & 0xFFFF); }

            public const int WM_MOUSEMOVE = 0x0200;   // 512

            public const int WM_LBUTTONDOWN = 0x0201; // 513
            public const int WM_LBUTTONUP = 0x0202;
            public const int WM_LBUTTONDBLCLK = 0x0203;

            public const int WM_RBUTTONDOWN = 0x0204;
            public const int WM_RBUTTONUP = 0x0205;
            public const int WM_RBUTTONDBLCLK = 0x0206;

            public const int WM_MBUTTONDOWN = 0x0207;
            public const int WM_MBUTTONUP = 0x0208;
            public const int WM_MBUTTONDBLCLK = 0x0209;

            public const int WM_XBUTTONDOWN = 0x020B;
            public const int WM_XBUTTONUP = 0x020C;
            public const int WM_XBUTTONDBLCLK = 0x020D;

            public const int WM_NCMOUSEMOVE = 0x00A0;
            public const int WM_NCLBUTTONDOWN = 0x00A1;
            public const int WM_NCLBUTTONUP = 0x00A2;
            public const int WM_NCLBUTTONDBLCLK = 0x00A3;
            public const int WM_NCRBUTTONDOWN = 0x00A4;
            public const int WM_NCRBUTTONUP = 0x00A5;
            public const int WM_NCRBUTTONDBLCLK = 0x00A6;
            public const int WM_NCMBUTTONDOWN = 0x00A7;
            public const int WM_NCMBUTTONUP = 0x00A8;
            public const int WM_NCMBUTTONDBLCLK = 0x00A9;
            public const int WM_NCXBUTTONDOWN = 0x00AB;
            public const int WM_NCXBUTTONUP = 0x00AC;
            public const int WM_NCXBUTTONDBLCLK = 0x00AD;

            public const int VK_XBUTTON1 = 0x05;
            public const int VK_XBUTTON2 = 0x06;

            public const int HC_ACTION = 0;
            public const int HC_NOREMOVE = 3;
        }
        #endregion
    }
}