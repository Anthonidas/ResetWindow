using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using static System.Collections.Specialized.BitVector32;
using System.Reflection;

namespace ResetWindow
{
    class Program
    {
        // My Handle
        [DllImport("Kernel32")]
        private static extern IntPtr GetConsoleWindow();

        // GET WINDOW TEXT
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // GET CLASS NAME
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        // ENUM WINDOWS AND CHILDS
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, ref SearchData data);
        private delegate bool EnumWindowsProc(IntPtr hWnd, ref SearchData data);
        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowProc lpEnumFunc, IntPtr lParam);

        // GET WINDOW RECTANGLE
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        // IS WINDOW VISIBLE
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        // SET WINDOW POSITION
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        // SHOW WINDOW
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        // SET FOREGROUND WINDOW
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // SEND MESSAGES
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        // set Text
        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, string lParam);

        // Register/Unregister Hotkeys
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // ---------------------------------------------------
        // Get Text
        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        static extern int SendMessage3(IntPtr hwndControl, uint Msg, int wParam, StringBuilder strBuffer); // get text

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        static extern int SendMessage4(IntPtr hwndControl, uint Msg, int wParam, int lParam);  // text length

        // CONSTANTS DEFINITION
        const int SWP_NOMOVE = 0x0002;
        const int SWP_NOSIZE = 0x0001;
        const int SWP_NOZORDER = 0x0004;
        const int SWP_SHOWWINDOW = 0x0040;
        const int SWP_NOACTIVATE = 0x0010;

        const int SW_RESTORE = 0x0009;
        const int SW_NORMAL = 0x0001;
        const int SW_SHOW = 0x0005;
        const int SW_HIDE = 0x0000;


        const int WM_SYSCOMMAND = 274;
        const int SC_MINIMIZE = 0xF020;
        const uint WM_SETTEXT = 0x000C;
        const uint WM_GETTEXT = 0x000D;

        const int WM_CLOSE = 0x10;
        const int WM_LBUTTONDOWN = 0x201;
        const int WM_LBUTTONUP = 0x202;
        const int BM_CLICK = 0x00F5;

        static readonly List<KeyValuePair<string, int>> actions = new List<KeyValuePair<string, int>>()
            {
                new KeyValuePair<string, int>("SW_HIDE", 0),
                new KeyValuePair<string, int>("SW_SHOWNORMAL", 1),
                new KeyValuePair<string, int>("SW_NORMAL", 1),
                new KeyValuePair<string, int>("SW_SHOWMINIMIZED", 2),
                new KeyValuePair<string, int>("SW_SHOWMAXIMIZED", 3),
                new KeyValuePair<string, int>("SW_MAXIMIZE", 3),
                new KeyValuePair<string, int>("SW_SHOWNOACTIVATE", 4),
                new KeyValuePair<string, int>("SW_SHOW", 5),
                new KeyValuePair<string, int>("SW_MINIMIZE", 6),
                new KeyValuePair<string, int>("SW_SHOWMINNOACTIVE", 7),
                new KeyValuePair<string, int>("SW_SHOWNA", 8),
                new KeyValuePair<string, int>("SW_RESTORE", 9),
                new KeyValuePair<string, int>("SW_SHOWDEFAULT", 10),
                new KeyValuePair<string, int>("SW_FORCEMINIMIZE", 11)
            };

        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        static string abstand = "";
        static int counter = 0;

        public class SearchData
        {
            public string Wndclass;
            public string Title;
            public IntPtr hWnd;
        }
        struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        public class LogWriter
        {
            private static LogWriter instance;
            private static Boolean active;
            private static FileInfo logFile = new FileInfo(Environment.SpecialFolder.DesktopDirectory + "\\ResetWindow.log");

            private static void instantiate()
            {
                if (instance == null)
                {
                    instance = new LogWriter();
                    active = false;
                }
            }

            public bool ActiveLog
            {
                get
                {
                    instantiate();
                    return active;
                }
                set
                {
                    instantiate();
                    active = bool.Parse(value.ToString());
                }
            }
            public void WriteToLog(String message)
            {
                try
                {
                    if (active == true)
                    {
                        //var temp = Environment.SpecialFolder.DesktopDirectory;
                        using (StreamWriter log = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\ResetWindow.log", true))
                        {
                            log.WriteLine(string.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + ":", message));
                        }
                    }
                    else
                        return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  >>ERROR: " + ex.Message + "\n-------------------------------------\n");
                }
            }
        }

        static List<SearchData> handleAll = new List<SearchData>();
        static List<SearchData> everything = new List<SearchData>();
        static LogWriter log = new LogWriter();
        static Timer t;
        static String tmp = "";

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var currentAssembly = Assembly.GetExecutingAssembly();
            var requireDllName = $"{(new AssemblyName(args.Name).Name)}.dll";
            var resource = currentAssembly.GetManifestResourceNames().Where(s => s.EndsWith(requireDllName)).FirstOrDefault();

            if (resource != null)
            {
                using (var stream = currentAssembly.GetManifestResourceStream(resource))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    var block = new byte[stream.Length];
                    stream.Read(block, 0, block.Length);
                    return Assembly.Load(block);
                }
            }
            else { return null; }
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Title = "ResetWindowApp";
            Console.WindowHeight = 28;
            Console.Clear();
            //Console.TreatControlCAsInput = true; /// --> it is buggy!
            //logFile = null;

            if (args.Length <= 0)
            {
                PreventClosure(true);
            }
            else if (args.Length == 1)
            {
                ReadConfig(args[0]);
            }
            else
            {
                AutoResetWindow(args);
            }
        }

        public static IntPtr SearchForWindow(string wndclass, string title)
        {
            SearchData sd = new SearchData { Wndclass = wndclass, Title = title };
            EnumWindows(new EnumWindowsProc(EnumProc), ref sd);
            return sd.hWnd;
        }
        public static bool EnumProc(IntPtr hWnd, ref SearchData data)
        {
            StringBuilder sbClass = new StringBuilder(1024);
            GetClassName(hWnd, sbClass, sbClass.Capacity);

            if (!String.IsNullOrWhiteSpace(data.Wndclass) && sbClass.ToString().ToUpper().StartsWith(data.Wndclass.ToUpper()))
            {
                if (data.Title != null)
                {
                    StringBuilder sbTitle = new StringBuilder(1024);
                    GetWindowText(hWnd, sbTitle, sbTitle.Capacity);

                    if (data.Title == "*")
                    {
                        if (!String.IsNullOrWhiteSpace(sbTitle.ToString()))
                        {
                            Console.WriteLine("ClassName: " + sbClass.ToString());
                            Console.WriteLine("Title: " + sbTitle.ToString());
                            data.hWnd = hWnd;
                            return false;    // Found the wnd, halt enumeration
                        }
                    }
                    else
                    {
                        if (sbTitle.ToString().ToUpper().StartsWith(data.Title.ToUpper()))
                        {
                            Console.WriteLine("ClassName: " + sbClass.ToString());
                            Console.WriteLine("Title: " + sbTitle.ToString());
                            data.hWnd = hWnd;
                            return false;    // Found the wnd, halt enumeration
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ClassName: " + sbClass.ToString());
                    data.hWnd = hWnd;
                    return false;
                }
            }
            else if (String.IsNullOrWhiteSpace(data.Wndclass))
            {
                if (data.Title != null)
                {
                    StringBuilder sbTitle = new StringBuilder(1024);
                    GetWindowText(hWnd, sbTitle, sbTitle.Capacity);
                    if (sbTitle.ToString().ToUpper().StartsWith(data.Title.ToUpper()))
                    {
                        Console.WriteLine("Title: " + sbTitle.ToString());
                        data.hWnd = hWnd;
                        return false;    // Found the wnd, halt enumeration
                    }
                }
            }
            return true;
        }
        public static bool EnumProcAll(IntPtr hWnd, ref SearchData data)
        {
            if (!String.IsNullOrWhiteSpace(data.Wndclass) && !String.IsNullOrWhiteSpace(data.Title))
            {
                StringBuilder sb = new StringBuilder(1024);
                GetClassName(hWnd, sb, sb.Capacity);

                if (data.Wndclass != null && sb.ToString().ToUpper().Contains(data.Wndclass.ToUpper()))
                {
                    StringBuilder sbText = new StringBuilder(1024);
                    GetWindowText(hWnd, sbText, sbText.Capacity);
                    if (data.Title != null && sbText.ToString().ToUpper().Contains(data.Title.ToUpper()))
                        handleAll.Add(new SearchData { hWnd = hWnd, Title = sbText.ToString(), Wndclass = sb.ToString() });
                }
            }
            else if (!String.IsNullOrWhiteSpace(data.Wndclass))
            {
                StringBuilder sb = new StringBuilder(1024);
                GetClassName(hWnd, sb, sb.Capacity);

                if (data.Wndclass != null && sb.ToString().ToUpper().Contains(data.Wndclass.ToUpper()))
                {
                    StringBuilder sbText = new StringBuilder(1024);
                    GetWindowText(hWnd, sbText, sbText.Capacity);
                    handleAll.Add(new SearchData { hWnd = hWnd, Title = sbText.ToString(), Wndclass = sb.ToString() });
                }
            }
            else if (!String.IsNullOrWhiteSpace(data.Title))
            {
                StringBuilder sbText = new StringBuilder(1024);
                GetWindowText(hWnd, sbText, sbText.Capacity);

                StringBuilder sball = new StringBuilder(1024);
                GetClassName(hWnd, sball, sball.Capacity);
                everything.Add(new SearchData { hWnd = hWnd, Title = sbText.ToString(), Wndclass = sball.ToString() });


                if (data.Title != null && sbText.ToString().ToUpper().Contains(data.Title.ToUpper()))
                {
                    StringBuilder sb = new StringBuilder(1024);
                    GetClassName(hWnd, sb, sb.Capacity);

                    handleAll.Add(new SearchData { hWnd = hWnd, Title = sbText.ToString(), Wndclass = sb.ToString() });
                }
            }
            return true;
        }
        static void GetRectangle(bool all = false)
        {
            try
            {
                if (all)
                {
                    Console.Write("ClassName: ");
                    string classAll = Console.ReadLine();
                    Console.Write("Title: ");
                    string titleAll = Console.ReadLine();
                    handleAll.Clear();

                    SearchData sdAll = new SearchData { Wndclass = classAll, Title = titleAll };
                    EnumWindows(new EnumWindowsProc(EnumProcAll), ref sdAll);

                    for (int i = 0; i < handleAll.Count; i++)
                    {
                        Console.WriteLine(i + 1 + "] " + handleAll[i].Wndclass + ": " + handleAll[i].hWnd + " | " + handleAll[i].Title);
                        log.WriteToLog(i + 1 + "] " + handleAll[i].Wndclass + ": " + handleAll[i].hWnd + " | " + handleAll[i].Title);
                    }
                    return;
                }

                string process = null;
                string classnm = null;
                string title = null;
                IntPtr handle = IntPtr.Zero;

                Console.Write("Handle: ");
                string hWnd = Console.ReadLine();
                //Console.WriteLine("Window Visibility: " + IsWindowVisible(new IntPtr(int.Parse(hWnd))));
                if (String.IsNullOrWhiteSpace(hWnd))
                {
                    Console.Write("Process name: ");
                    process = Console.ReadLine();

                    if (String.IsNullOrWhiteSpace(process))
                    {
                        Console.Write("ClassName: ");
                        classnm = Console.ReadLine();
                        Console.Write("Window Title: ");
                        title = Console.ReadLine();
                    }
                }
                else
                    handle = new IntPtr(int.Parse(hWnd));

                Console.WriteLine("");

                if (!String.IsNullOrWhiteSpace(process))
                {
                    Process[] processes = Process.GetProcessesByName(process.Trim());
                    if (processes.Length > 0)
                    {
                        Console.WriteLine("Process Count: " + processes.Length);
                        Process p = processes[0];
                        handle = p.MainWindowHandle;
                        title = p.MainWindowTitle;
                    }
                    else
                    {
                        Console.WriteLine("--------------------------------------------------");
                        Console.WriteLine(" Es wurde kein Prozess mit diesem Namen gefunden. ");
                        Console.WriteLine("--------------------------------------------------");
                    }
                }

                if (handle == IntPtr.Zero && String.IsNullOrWhiteSpace(classnm) && String.IsNullOrWhiteSpace(title))
                {
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine("    Es wurde keine Klasse und Titel definiert!    ");
                    Console.WriteLine("--------------------------------------------------");
                    return;
                }
                else if (handle == IntPtr.Zero && !String.IsNullOrWhiteSpace(classnm) && !String.IsNullOrWhiteSpace(title))
                {
                    handle = SearchForWindow(classnm, title);
                }
                else if (handle == IntPtr.Zero && !String.IsNullOrWhiteSpace(classnm))
                {
                    handle = SearchForWindow(classnm, null);
                }
                else if (handle == IntPtr.Zero && !String.IsNullOrWhiteSpace(title))
                {
                    handle = SearchForWindow(null, title);
                }

                Rect CurrentPosition = new Rect();
                StringBuilder sbC = new StringBuilder(1024);
                StringBuilder sbT = new StringBuilder(1024);
                GetClassName(handle, sbC, sbC.Capacity);
                GetWindowText(handle, sbT, sbC.Capacity);
                GetWindowRect(handle, ref CurrentPosition);
                Console.WriteLine("\r\n" + title + " - RECTANGLE");
                Console.WriteLine("Title: " + sbT.ToString());
                Console.WriteLine("ID: " + handle);
                Console.WriteLine("ClassName: " + sbC.ToString());
                Console.WriteLine("X: " + CurrentPosition.Left);
                Console.WriteLine("Y: " + CurrentPosition.Top);
                Console.WriteLine("RX: " + CurrentPosition.Right);
                Console.WriteLine("RY: " + CurrentPosition.Bottom);
                Console.WriteLine("W: " + (CurrentPosition.Right - CurrentPosition.Left));
                Console.WriteLine("H: " + (CurrentPosition.Bottom - CurrentPosition.Top));

                log.WriteToLog(title + " - RECTANGLE\n" +
                               "Title: " + sbT.ToString() + "\n" +
                               "ID: " + handle + "\n" +
                               "ClassName: " + sbC.ToString() + "\n" +
                               "X: " + CurrentPosition.Left + "\n" +
                               "Y: " + CurrentPosition.Top + "\n" +
                               "RX: " + CurrentPosition.Right + "\n" +
                               "RY: " + CurrentPosition.Bottom + "\n" +
                               "W: " + (CurrentPosition.Right - CurrentPosition.Left) + "\n" +
                               "H: " + (CurrentPosition.Bottom - CurrentPosition.Top)
                );

                string errorMessage2 = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                Console.WriteLine("----------------- Win32Exception -----------------");
                Console.WriteLine(errorMessage2);
                Console.WriteLine("--------------------------------------------------");
            }
            catch (Win32Exception wex)
            {
                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                Console.WriteLine("------------ ERROR ------------");
                Console.WriteLine(wex.Message);
                Console.WriteLine("------------ ERROR2 ------------");
                Console.WriteLine(errorMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("------------ ERROR ------------");
                Console.WriteLine(ex.Message);
            }
        }
        static List<IntPtr> GetChilds(IntPtr parent)
        {
            List<IntPtr> childHandles = new List<IntPtr>();
            GCHandle gcChildHandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildHandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(parent, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildHandlesList.Free();
            }

            return childHandles;
        }
        static void GetChildsFunc(string hwnd, bool all = false)
        {
            List<IntPtr> tempList = new List<IntPtr>();
            tempList.Clear();
            tempList = GetChilds(new IntPtr(int.Parse(hwnd)));

            abstand += "   ";
            foreach (IntPtr i in tempList)
            {
                Console.WriteLine(abstand + i.ToString() + "] " + GetTextBoxText(i));
                log.WriteToLog(abstand + i.ToString() + "] " + GetTextBoxText(i));

                if (all)
                    GetChildsFunc(i.ToString(), all);
            }
            abstand = abstand.Substring(0, abstand.Length - 3);

        }
        static bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildHandlesList = GCHandle.FromIntPtr(lParam);
            if (gcChildHandlesList == null || gcChildHandlesList.Target == null)
                return false;

            List<IntPtr> childHandles = gcChildHandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }
        static void SetRectangle()
        {
            try
            {
                IntPtr handle = IntPtr.Zero;
                string Wclass = null;
                string process = null;
                string Wtitle = null;

                Console.WriteLine("What do you have? (Write: Handle, Process, Classname)");
                string whatDoYouHave = Console.ReadLine();

                switch (whatDoYouHave)
                {
                    case "handle":
                    case "Handle":
                        Console.Write("Give Me Your Handle: ");
                        handle = new IntPtr(int.Parse(Console.ReadLine()));
                        break;
                    case "process":
                    case "Process":
                        Console.Write("Process name: ");
                        process = Console.ReadLine();
                        break;
                    case "Classname":
                    case "classname":
                    case "Class":
                    case "class":
                        Console.Write("Class: ");
                        Wclass = Console.ReadLine();
                        Console.Write("Title: ");
                        Wtitle = Console.ReadLine();
                        break;
                    default:
                        Console.WriteLine("You did not choose any option!");
                        return;
                }

                Console.WriteLine("Fill next fields with \"0\" if you dont want to change them.");
                Console.Write("X-Value: ");
                int x = int.Parse(Console.ReadLine());
                Console.Write("Y-Value: ");
                int y = int.Parse(Console.ReadLine());
                Console.Write("Width: ");
                int w = int.Parse(Console.ReadLine());
                Console.Write("Height: ");
                int h = int.Parse(Console.ReadLine());

                if (handle != IntPtr.Zero)
                {
                    if (w <= 0)
                        SetWindowPos(handle, 0, x, y, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_SHOWWINDOW);
                    else
                        SetWindowPos(handle, 0, x, y, w, h, SWP_NOZORDER | SWP_SHOWWINDOW);
                }

                if (!String.IsNullOrWhiteSpace(process))
                {
                    Process[] processes = Process.GetProcessesByName(process.Trim());

                    if (processes.Length > 0)
                    {
                        Process p = processes[0];
                        handle = p.MainWindowHandle;
                        SetWindowPos(handle, 0, x, y, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_SHOWWINDOW);
                    }
                }

                if (!String.IsNullOrWhiteSpace(Wclass))
                {
                    if (String.IsNullOrWhiteSpace(Wtitle))
                        handle = SearchForWindow(Wclass, "*");
                    else
                        handle = SearchForWindow(Wclass, Wtitle);

                    if (w <= 0)
                        SetWindowPos(handle, 0, x, y, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_SHOWWINDOW);
                    else
                        SetWindowPos(handle, 0, x, y, w, h, SWP_NOZORDER | SWP_SHOWWINDOW);
                }

                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                Console.WriteLine("----------------- Win32Exception -----------------");
                Console.WriteLine(errorMessage);
                Console.WriteLine("--------------------------------------------------");
            }
            catch (Win32Exception wex)
            {
                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                Console.WriteLine("------------ ERROR ------------");
                Console.WriteLine(wex.Message);
                Console.WriteLine("------------ ERROR2 ------------");
                Console.WriteLine(errorMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("------------ ERROR ------------");
                Console.WriteLine(ex.Message);
            }
        }
        static void AutoResetWindow(string[] args)
        {
            if (args.Length == 3 || args.Length >= 5)
            {
                string window = args[0];
                string title = "";
                int widthArg = 0;
                int heightArg = 0;
                Rect CurrentPosition = new Rect();

                int.TryParse(args[1], out int xArg);
                int.TryParse(args[2], out int yArg);

                if (args.Length >= 5)
                {
                    int.TryParse(args[3], out widthArg);
                    int.TryParse(args[4], out heightArg);

                    if (args.Length == 6)
                    {
                        title = args[5];
                    }
                }

                if (xArg.ToString() == args[1] && yArg.ToString() == args[2])
                {
                    Console.WriteLine("Trying to set position for " + window);

                    IntPtr handle;
                    if (String.IsNullOrWhiteSpace(title))
                        handle = SearchForWindow(window, "*");
                    else
                        handle = SearchForWindow(window, title);

                    if (handle == IntPtr.Zero)
                    {
                        Console.WriteLine("Waiting 3s for window startup...");
                        Thread.Sleep(3000);
                        AutoResetWindow(args);
                        return;
                    }
                    SetForegroundWindow(handle);
                    GetWindowRect(handle, ref CurrentPosition);

                    /*Console.WriteLine("####################");
                    Console.WriteLine("Handle: " + handle);
                    Console.ReadLine();*/

                    if ((CurrentPosition.Left != xArg || CurrentPosition.Top != yArg || ((widthArg > 0 && heightArg > 0) && ((CurrentPosition.Right - CurrentPosition.Left) != widthArg || (CurrentPosition.Bottom - CurrentPosition.Top) != heightArg))) && (CurrentPosition.Left > -30000 && CurrentPosition.Top > -30000))
                    {
                        if ((widthArg > 0 && heightArg > 0) && ((CurrentPosition.Right - CurrentPosition.Left) != widthArg || (CurrentPosition.Bottom - CurrentPosition.Top) != heightArg))
                        {
                            /*Console.WriteLine("####################");
                            Console.WriteLine("SET POS AND SIZE");
                            Console.ReadLine();*/

                            ShowWindow(handle, SW_SHOW);
                            SetForegroundWindow(handle);
                            SetWindowPos(handle, 0, xArg, yArg, widthArg, heightArg, SWP_NOZORDER | SWP_SHOWWINDOW);

                            Console.WriteLine("Check if everything is OK...");
                            Thread.Sleep(3000);
                            AutoResetWindow(args);
                            return;
                        }
                        else
                        {
                            ShowWindow(handle, SW_SHOW);
                            SetForegroundWindow(handle);
                            SetWindowPos(handle, 0, xArg, yArg, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_SHOWWINDOW);

                            Console.WriteLine("Check if everything is OK...");
                            Thread.Sleep(3000);
                            AutoResetWindow(args);
                            return;

                            /*Console.WriteLine("####################");
                            Console.WriteLine("SET ONLY POS");
                            Console.ReadLine();*/
                        }
                    }
                    Console.WriteLine("... done!\r\n");
                    //Console.WriteLine("Handle: " + handle);
                    //foreach (string arg in args)
                    //{
                    //    Console.WriteLine(arg);
                    //}
                    //Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("X und Y Werte müssen numerisch sein! Bitte kontrollieren Sie Ihre Eingabe.");
                    Console.ReadLine();
                    PreventClosure(false);
                }
            }
            else
            {
                if (args.Length == 2 && args[1].ToLower() == "minimized")
                {
                    string window = args[0];
                    IntPtr handle = IntPtr.Zero;
                    Rect CurrentPosition = new Rect();

                    if (window == "rctrl_renwnd32")
                        handle = SearchForWindow(window, "Posteingang");
                    else
                        handle = SearchForWindow(window, "*");

                    Console.WriteLine("Trying to set position for " + window);
                    if (handle == IntPtr.Zero)
                    {
                        Console.WriteLine("Waiting 3s for window startup...");
                        Thread.Sleep(3000);
                        AutoResetWindow(args);
                        return;
                    }

                    //ShowWindow(handle, SW_SHOW);
                    SetForegroundWindow(handle);
                    GetWindowRect(handle, ref CurrentPosition);
                    if (CurrentPosition.Left > -30000 && CurrentPosition.Top > -30000)
                    {
                        SendMessage(handle, WM_SYSCOMMAND, SC_MINIMIZE, 0);
                        Console.WriteLine("...done!");
                    }
                    else
                        Console.WriteLine("nothing to do...");
                    //Console.ReadLine();
                }
                else if (args.Length == 4 && args[1].ToLower() == "click")
                {
                    /* 0 = class
                     * 1 = click action
                     * 2 = button text
                     * 3 = window title
                     */

                    string window = args[0];
                    string wTitle = args[3];
                    string wButton = args[2];
                    IntPtr hWnd = IntPtr.Zero;
                    IntPtr hChld = IntPtr.Zero;

                    hWnd = SearchForWindow(window, wTitle);

                    Console.WriteLine("Send Action to " + window);
                    if (hWnd == IntPtr.Zero)
                    {
                        counter++;
                        if (counter >= 4)
                        {
                            Console.WriteLine("Window is not open!\r\nClosing...");
                            PreventClosure(false);
                            return;
                        }
                        Console.WriteLine(counter + "] Waiting 3s for window startup...");
                        Thread.Sleep(3000);
                        AutoResetWindow(args);
                        return;
                    }

                    List<IntPtr> tempList = new List<IntPtr>();
                    tempList.Clear();
                    tempList = GetChilds(hWnd);

                    foreach (IntPtr i in tempList)
                    {
                        string tmpTxt = GetTextBoxText(i);
                        if (!String.IsNullOrWhiteSpace(tmpTxt) && tmpTxt.ToUpper().EndsWith(wButton.ToUpper()))
                        {
                            SendMessage(i, WM_LBUTTONDOWN, 0, 0);
                            SendMessage(i, WM_LBUTTONUP, 0, 0);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Es müssen zusätzliche Parameter definiert werden!");
                    Console.WriteLine("Nur " + args.Length + " Parameter sind definiert");
                    Console.ReadLine();
                    PreventClosure(false);
                }
            }
        }
        static void SendWindowAction(bool click = false)
        {
            try
            {
                if (click)
                {
                    Console.Write("Handle: ");
                    IntPtr hwnd = new IntPtr(int.Parse(Console.ReadLine()));

                    SendMessage(hwnd, WM_LBUTTONDOWN, 0, 0);
                    SendMessage(hwnd, WM_LBUTTONUP, 0, 0);
                }
                else
                {
                    Console.Write("Class: ");
                    string Wclass = Console.ReadLine();
                    Console.Write("Title: ");
                    string Wtitle = Console.ReadLine();
                    Console.Write("Text: ");
                    string txt = Console.ReadLine();

                    if (!String.IsNullOrWhiteSpace(Wclass))
                    {
                        IntPtr handle = IntPtr.Zero;
                        if (String.IsNullOrWhiteSpace(Wtitle))
                            handle = SearchForWindow(Wclass, "*");
                        else
                            handle = SearchForWindow(Wclass, Wtitle);

                        SetForegroundWindow(handle);

                    }
                }

                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                Console.WriteLine("----------------- Win32Exception -----------------");
                Console.WriteLine(errorMessage);
                Console.WriteLine("--------------------------------------------------");
            }
            catch (Win32Exception wex)
            {
                string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                Console.WriteLine("------------ ERROR ------------");
                Console.WriteLine(wex.Message);
                Console.WriteLine("------------ ERROR2 ------------");
                Console.WriteLine(errorMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("------------ ERROR ------------");
                Console.WriteLine(ex.Message);
            }
        }
        static int GetTextBoxTextLength(IntPtr hTextBox)
        {
            // helper for GetTextBoxText
            uint WM_GETTEXTLENGTH = 0x000E;
            int result = SendMessage4(hTextBox, WM_GETTEXTLENGTH, 0, 0);
            return result;
        }
        static string GetTextBoxText(IntPtr hTextBox)
        {
            int len = GetTextBoxTextLength(hTextBox);
            if (len <= 0) return null;  // no text
            StringBuilder sb = new StringBuilder(len + 1);
            SendMessage3(hTextBox, WM_GETTEXT, len + 1, sb);
            return sb.ToString();
        }
        static string SetTextBoxText(IntPtr hTextBox, string txt)
        {
            StringBuilder sb = new StringBuilder(txt);
            SendMessage3(hTextBox, WM_SETTEXT, sb.Capacity, sb);
            return sb.ToString();
        }

        /**
         * Possible values for 'value' are:
            SW_HIDE
            0 	Hides the window and activates another window.
            SW_SHOWNORMAL
            SW_NORMAL
            1 	Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
            SW_SHOWMINIMIZED
            2 	Activates the window and displays it as a minimized window.
            SW_SHOWMAXIMIZED
            SW_MAXIMIZE
            3 	Activates the window and displays it as a maximized window.
            SW_SHOWNOACTIVATE
            4 	Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL, except that the window is not activated.
            SW_SHOW
            5 	Activates the window and displays it in its current size and position.
            SW_MINIMIZE
            6 	Minimizes the specified window and activates the next top-level window in the Z order.
            SW_SHOWMINNOACTIVE
            7 	Displays the window as a minimized window. This value is similar to SW_SHOWMINIMIZED, except the window is not activated.
            SW_SHOWNA
            8 	Displays the window in its current size and position. This value is similar to SW_SHOW, except that the window is not activated.
            SW_RESTORE
            9 	Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
            SW_SHOWDEFAULT
            10 	Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
            SW_FORCEMINIMIZE
            11 	Minimizes a window, even if the thread that owns the window is not responding. This flag should only be used when minimizing windows from a different thread.
         */
        static void ShowMyWindow()
        {
            Console.Write("Handle: ");
            IntPtr hwnd = new IntPtr(int.Parse(Console.ReadLine()));
            Console.Write("Action: ");
            string action = Console.ReadLine();

            if (String.IsNullOrWhiteSpace(action))
            {
                Console.WriteLine("------------ ERROR ------------");
                Console.WriteLine(" Action not valid!");
                Console.WriteLine("-------------------------------");
            }
            else
            {
                int iAction = actions.First(kvp => kvp.Key.Equals(action.ToUpper())).Value;
                ShowWindow(hwnd, iAction);
            }
        }
        static void ShowMyWindow(bool show)
        {
            Console.Write("Handle: ");
            IntPtr hwnd = new IntPtr(int.Parse(Console.ReadLine()));

            if (show)
                ShowWindow(hwnd, SW_SHOW);
            else
                ShowWindow(hwnd, SW_HIDE);
        }
        static void ShowAllWindows(bool show)
        {
            Console.Write("ClassName: ");
            string classAll = Console.ReadLine();
            Console.Write("Title: ");
            string titleAll = Console.ReadLine();
            handleAll.Clear();

            SearchData sdAll = new SearchData { Wndclass = classAll, Title = titleAll };
            EnumWindows(new EnumWindowsProc(EnumProcAll), ref sdAll);

            for (int i = 0; i < handleAll.Count; i++)
            {
                if (show)
                    ShowWindow(handleAll[i].hWnd, SW_SHOW);
                else
                    ShowWindow(handleAll[i].hWnd, SW_HIDE);
            }

        }
        static void SetForever(Object state)
        {
            SetForegroundWindow(new IntPtr(int.Parse(tmp)));
            //Console.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message);
        }

        // CMD-Commands
        static void GetCommand(string cmd)
        {
            bool prevent = true;
            // do something with cmd...
            switch (cmd.Trim())
            {
                case "get":
                    prevent = true;
                    Console.WriteLine("-- Missing Parameter! --");
                    PreventClosure(prevent);
                    break;
                case "get window":
                    GetRectangle();
                    break;
                case "get childs":
                    Console.Write("Handle: ");
                    string hwnd = Console.ReadLine();
                    GetChildsFunc(hwnd);
                    break;
                case "get childs all":
                    Console.Write("Handle: ");
                    string hwndAll = Console.ReadLine();
                    GetChildsFunc(hwndAll, true);
                    break;
                case "get all":
                    GetRectangle(true);
                    break;
                case "set":
                    prevent = true;
                    Console.WriteLine("-- Missing Parameter! --");
                    PreventClosure(prevent);
                    break;
                case "set rect":
                    SetRectangle();
                    break;
                case "set text":
                    Console.Write("Handle: ");
                    string hwndText = Console.ReadLine();
                    Console.Write("Text: ");
                    string txt = Console.ReadLine();
                    SetTextBoxText(new IntPtr(int.Parse(hwndText)), txt);
                    break;
                case "set foreground":
                    Console.Write("Handle: ");
                    string handle = Console.ReadLine();
                    SetForegroundWindow(new IntPtr(int.Parse(handle)));
                    Console.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message);
                    break;
                case "set forever":
                    Console.Write("Handle: ");
                    tmp = Console.ReadLine();
                    if (!String.IsNullOrWhiteSpace(tmp))
                    {
                        t = new Timer(SetForever, null, 0, 500);
                        Console.WriteLine(new Win32Exception(Marshal.GetLastWin32Error()).Message);
                    }
                    break;
                case "set config":
                    Console.Write("Path: ");
                    tmp = Console.ReadLine();
                    if (!String.IsNullOrWhiteSpace(tmp))
                    {
                        ReadConfig(tmp);
                    }
                    break;
                case "stop forever":
                    t.Dispose();
                    break;
                /*case "register":
                    registerHotKeys();
                    break;
                case "unregister":
                    unregisterHotKeys();
                    break;*/
                case "send":
                    SendWindowAction();
                    break;
                case "send click":
                    SendWindowAction(true);
                    break;
                case "action":
                    ShowMyWindow();
                    break;
                case "show":
                    ShowMyWindow(true);
                    break;
                case "hide":
                    ShowMyWindow(false);
                    break;
                case "show all":
                    ShowAllWindows(true);
                    break;
                case "hide all":
                    ShowAllWindows(false);
                    break;
                case "log on":
                    log.ActiveLog = true;
                    break;
                case "log off":
                    log.ActiveLog = false;
                    break;
                case "cls":
                    Console.Clear();
                    break;
                case "exit":
                    prevent = false;
                    break;
                case "quit":
                    prevent = false;
                    break;
                case "help":
                    Console.WriteLine("------------------------");
                    Console.WriteLine("---- Help: Commands ----");
                    Console.WriteLine("------------------------");
                    Console.WriteLine("");
                    Console.WriteLine("GET Information:");
                    Console.WriteLine("\t- get all:\t  Get All Main Window Handles");
                    Console.WriteLine("\t- get window:\t  Get Window Information");
                    Console.WriteLine("\t- get childs:\t  Get Child Handles Of A Window");
                    Console.WriteLine("\t- get childs all: Get Child Handles Incl. Childs Of Childs Of A Window");
                    Console.WriteLine("");
                    Console.WriteLine("SET Information:");
                    Console.WriteLine("\t- set foreground: Set Window in Foreground");
                    Console.WriteLine("\t- set forever:\t  Check if window is still in foreground and activate");
                    Console.WriteLine("\t- set rect:\t  Change Position And Dimension Of A Window");
                    Console.WriteLine("\t- set text:\t  Change The Text Of A Window Or Its Childs");
                    //Console.WriteLine("\t- set visible:\t  Set Window Visibility (Foreground or Background)");
                    Console.WriteLine("\t- action:\t  Send an action to the window");
                    Console.WriteLine("");
                    Console.WriteLine("OTHER:");
                    Console.WriteLine("\t- log on:\t  Activate Logging Into File");
                    Console.WriteLine("\t- log off:\t  Stop Logging Into File");
                    Console.WriteLine("\t- send click:\t  Perform MouseClick");
                    Console.WriteLine("\t- show:\t\t  Show Window");
                    Console.WriteLine("\t- hide:\t\t  Hide Window");
                    Console.WriteLine("\t- cls:\t\t  Clear Screen");
                    Console.WriteLine("\t- help:\t\t  Show This Help");
                    Console.WriteLine("\t- exit / quit:\t  Close This Window");
                    PreventClosure(prevent);
                    break;
                default:
                    prevent = true;
                    Console.WriteLine("-- Comand not valid! --\n");
                    GetCommand("help");
                    PreventClosure(prevent);
                    break;
            }

            // avoid closure
            PreventClosure(prevent);
        }
        static void PreventClosure(bool prevent)
        {
            if (prevent)
            {
                //Console.WriteLine("--------------------------------------------------");
                Console.Write("\r\n>> Command: ");
                string cmd = Console.ReadLine();
                GetCommand(cmd);
            }
            else
            {
                Environment.Exit(0);
            }
        }

        private static void ReadConfig(string path)
        {
            List<AppBean> apps;

            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                apps = JsonConvert.DeserializeObject<List<AppBean>>(json);
            }

            apps.ForEach(delegate (AppBean app)
            {
                handleAll.Clear();
                SearchData sdAll = new SearchData { Wndclass = app.Class, Title = app.Title };
                EnumWindows(new EnumWindowsProc(EnumProcAll), ref sdAll);

                handleAll.ForEach(delegate (SearchData handle)
                {
                    ShowWindow(handle.hWnd, SW_SHOW);
                    ShowWindow(handle.hWnd, SW_NORMAL);
                    SetForegroundWindow(handle.hWnd);
                    SetWindowPos(handle.hWnd, 0, app.X, app.Y, app.Width, app.Height, SWP_NOZORDER | SWP_SHOWWINDOW);

                    int iAction = actions.First(kvp => kvp.Key.Equals(app.State.ToUpper())).Value;
                    ShowWindow(handle.hWnd, iAction);
                });
            });
        }
    }
}
