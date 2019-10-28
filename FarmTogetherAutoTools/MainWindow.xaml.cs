using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;

namespace FarmTogetherAutoTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] public static extern IntPtr GetModuleHandle(string lpLibFileNmae);
        [DllImport("kernel32.dll")] public static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);
        [DllImport("kernel32.dll")]  public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, int[] lpBuffer, int nSize, IntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll")] public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, IntPtr lpNumberOfBytesRead);
        [DllImportAttribute("kernel32.dll", EntryPoint = "OpenProcess")] public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("User32.dll", EntryPoint = "FindWindow")] private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        //ShowWindow参数
        private const int SW_SHOWNORMAL = 1;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWNOACTIVATE = 4;
        //SendMessage参数
        private const int WM_KEYDOWN = 0X100;
        private const int WM_KEYUP = 0X101;
        private const int WM_SYSCHAR = 0X106;
        private const int WM_SYSKEYUP = 0X105;
        private const int WM_SYSKEYDOWN = 0X104;
        private const int WM_CHAR = 0X102;

        //玩家朝向对应的内存值
        int DirWest = 1034092544;//朝向正西方向的内存值，0x3DA30000，此外0x43B40000也可以得到，不过会被立刻变为0
        int DirNorth = 1119092736;//0x42B40000
        int DirEast = 1127481344;//0x43340000
        int DirSouth = 1132902832;//0x43870000，很有趣的是，他们不是等差的，西-北-东-南-西的差分别是(0x)511 0000, 80 0000, 53 0000, 2D 0000
        int DirNorthWest = 1110704128;
        int DirNorthEast = 1124524032;
        int DirSouthEast = 1130455040;
        int DirSouthWest = 1134403584;

        //存储礼物店的列数
        int StoreRow = 1;
        int storenums = 0;

        //正在运行的线程（自动化工作）
        Thread RunningThread = null;

        //找到的疑似存有当前Season的地址
        long TickBaseAddress = (long)0;

        IntPtr GameWindowPtr = IntPtr.Zero;


        private delegate void DelegateOfKeyEvent();//按键代理

        public MainWindow()
        {
            InitializeComponent();
            Thread SearchTrd = new Thread(SearchAddressThread);
            SearchTrd.IsBackground = true;
            SearchTrd.Start();
            if (File.Exists("Config.txt"))
            {
                try
                {
                    StreamReader sr = new StreamReader("Config.txt");
                    string srrdstr = sr.ReadLine();
                    if (srrdstr.Length <= 1)
                        return;
                    if (srrdstr.Split('#')[0] == "2")
                    {
                        RbChange_2.IsChecked = true;
                        RbChange_1.IsChecked = false;
                    }
                    storenums = Convert.ToInt32(srrdstr.Split('#')[0]);
                    TxtGiftStoreNum.Text = srrdstr.Split('#')[1];
                    sr.Close();
                }
                catch { }
            }
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\DataGenerated"))
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\DataGenerated");
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\SaveGenerated"))
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\SaveGenerated");
        }

        protected override void OnSourceInitialized(EventArgs e)//初始化热键
        {
            base.OnSourceInitialized(e);

            HotKey NextSeasonHK = new HotKey(this, HotKey.KeyFlags.MOD_ALT, System.Windows.Forms.Keys.R);
            NextSeasonHK.OnHotKey += new HotKey.OnHotKeyEventHandler(hotkey_NextSeason);

            HotKey AutoPlantDoubleHK = new HotKey(this, HotKey.KeyFlags.MOD_ALT, System.Windows.Forms.Keys.F);
            AutoPlantDoubleHK.OnHotKey += new HotKey.OnHotKeyEventHandler(hotkey_AutoPlantDouble);

            HotKey AutoPlantHK = new HotKey(this, HotKey.KeyFlags.MOD_ALT, System.Windows.Forms.Keys.G);
            AutoPlantHK.OnHotKey += new HotKey.OnHotKeyEventHandler(hotkey_AutoPlant);

            HotKey AutoCSpaceHK = new HotKey(this, HotKey.KeyFlags.MOD_ALT, System.Windows.Forms.Keys.H);
            AutoCSpaceHK.OnHotKey += new HotKey.OnHotKeyEventHandler(hotkey_AutoContinueSpace);

            HotKey AutoMedakHK = new HotKey(this, HotKey.KeyFlags.MOD_ALT, System.Windows.Forms.Keys.J);
            AutoMedakHK.OnHotKey += new HotKey.OnHotKeyEventHandler(hotkey_AutoMedal);

            HotKey StopAllHK = new HotKey(this, HotKey.KeyFlags.MOD_ALT, System.Windows.Forms.Keys.Z);
            StopAllHK.OnHotKey += new HotKey.OnHotKeyEventHandler(hotkey_StopAll);
        }

        private void hotkey_NextSeason()//下个季节快捷键
        {
            GotoNextSeason(null, null);
        }

        private void hotkey_AutoPlant()
        {
            AutoPlant(null, null);
        }
        private void hotkey_AutoPlantDouble()
        {
            AutoPlantDouble(null, null);
        }
        private void hotkey_AutoContinueSpace()
        {
            AutoContinueSpace(null, null);
        }
        private void hotkey_AutoMedal()
        {
            AutoMedal(null, null);
        }
        private void hotkey_StopAll()
        {
            StopAll(null, null);
        }
        

        public static int GetPidByProcessName(string processName)
        {
            Process[] arrayProcess = Process.GetProcessesByName(processName);
            foreach (Process p in arrayProcess)
            {
                return p.Id;
            }
            return 0;
        }

        public static long MRead(long addr, bool read64 = true)
        {
            if(read64)//读取64位的数，通常读内存中存储的地址要用这个
            {
                try
                {
                    byte[] buffer = new byte[8];

                    //打开一个已存在的进程对象  0x1F0FFF 最高权限
                    IntPtr hProcess = OpenProcess(0x1F0FFF, false, GetPidByProcessName("FarmTogether"));
                    //将制定内存中的值读入缓冲区
                    ReadProcessMemory(hProcess, (IntPtr)addr, buffer, 8, IntPtr.Zero);
                    return BitConverter.ToInt64(buffer, 0);
                }
                catch
                {
                    return 0;
                }
            }
            else//读取32位的数，通常用来读取数值，而非地址
            {
                try
                {
                    byte[] buffer = new byte[4];

                    //打开一个已存在的进程对象  0x1F0FFF 最高权限
                    IntPtr hProcess = OpenProcess(0x1F0FFF, false, GetPidByProcessName("FarmTogether"));
                    //将制定内存中的值读入缓冲区
                    ReadProcessMemory(hProcess, (IntPtr)addr, buffer, 4, IntPtr.Zero);
                    return BitConverter.ToInt32(buffer,0);
                }
                catch
                {
                    return 0;
                }
            }
            
        }

        public static void MWrite(long baseAddress, int value)//写32位int
        {
            try
            {
                //打开一个已存在的进程对象  0x1F0FFF 最高权限
                IntPtr hProcess = OpenProcess(0x1F0FFF, false, GetPidByProcessName("FarmTogether"));
                //从指定内存中写入字节集数据
                WriteProcessMemory(hProcess, (IntPtr)baseAddress, new int[] { value }, 4, IntPtr.Zero);
            }
            catch { }
        }

        //因为可能无法随时找到正确的地址，因此在程序开始时自动不断地寻找地址，找到可能正确的就保存下来以备后用。
        private void SearchAddressThread()
        {
            while(true)
            {
                Process[] process_search = Process.GetProcessesByName("FarmTogether");
                if (process_search.Length != 0)
                {
                    ProcessModuleCollection modules = process_search[0].Modules;
                    ProcessModule dll = null;
                    foreach (ProcessModule i in modules)
                    {
                        if (i.ModuleName == "UnityPlayer.dll")
                        {
                            dll = i;
                            break;
                        }
                    }
                    try
                    {

                        long DllAddrPlus = dll.BaseAddress.ToInt64() + ((IntPtr)0x151f010).ToInt64();
                        long nextaddr = MRead(DllAddrPlus);
                        nextaddr = MRead(nextaddr + 0x60);
                        nextaddr = MRead(nextaddr + 0x48);
                        nextaddr = MRead(nextaddr + 0x1c8);
                        nextaddr = MRead(nextaddr + 0x10);
                        nextaddr = MRead(nextaddr + 0x180);
                        nextaddr = MRead(nextaddr + 0x28);
                        nextaddr += 0x208;//此地址保存当前季节1,2,4,8
                        int NowSeason = (int)MRead(nextaddr, false);
                        if(NowSeason == 1 || NowSeason == 2 || NowSeason == 4 || NowSeason ==8)
                        {
                            if(MRead(nextaddr + 0x8,false) - MRead(nextaddr + 0xc,false) <= (long)1020 && MRead(nextaddr + 0x8, false) - MRead(nextaddr + 0xc, false) >= (long)0)
                            {
                                TickBaseAddress = nextaddr;
                            }
                        }


                        else//从另外一个dll处出发寻找，另一种途径
                        {
                            dll = null;
                            foreach (ProcessModule i in modules)
                            {
                                if (i.ModuleName == "mono.dll")
                                {
                                    dll = i;
                                    break;
                                }
                            }
                            try
                            {

                                DllAddrPlus = dll.BaseAddress.ToInt64() + ((IntPtr)0x265a20).ToInt64();
                                nextaddr = MRead(DllAddrPlus);
                                if (nextaddr == (long)0)//这些是因为CE找到的指针有时会变动，导致有可能找不到要找的地址
                                    continue;
                                nextaddr = MRead(nextaddr + 0xa0);//后续的偏移
                                if (nextaddr == (long)0)
                                    continue; 
                                nextaddr = MRead(nextaddr + 0xc8);
                                if (nextaddr == (long)0)
                                    continue;
                                nextaddr = MRead(nextaddr + 0x20);
                                if (nextaddr == (long)0)
                                    continue;
                                nextaddr = MRead(nextaddr + 0xe0);
                                if (nextaddr == (long)0)
                                    continue;
                                nextaddr = MRead(nextaddr + 0x68);
                                if (nextaddr == (long)0)
                                    continue;
                                nextaddr = MRead(nextaddr + 0x28);
                                if (nextaddr == (long)0)
                                    continue;
                                nextaddr += 0x208;//此地址保存当前季节1,2,4,8
                                NowSeason = (int)MRead(nextaddr, false);
                                if (NowSeason == 1 || NowSeason == 2 || NowSeason == 4 || NowSeason == 8)
                                {
                                    if (MRead(nextaddr + 0x8, false) - MRead(nextaddr + 0xc, false) <= (long)1020 && MRead(nextaddr + 0x8, false) - MRead(nextaddr + 0xc, false) >= (long)0)
                                    {
                                        TickBaseAddress = nextaddr;
                                    }
                                }

                            }
                            catch { }
                        }

                    }
                    catch { continue; }
                }
                Thread.Sleep(500);
            }
        }

        private void TurnFaceDirection(int TgtDir = 0)//转向
        {
            //决定玩家朝向的值存储在基于UnityPlayer.dll基址的多级偏移指向的内存中，按照其中的一个路径（借助CE得到）找下去即可找到该内存位置，并对其值进行修改

            Process[] process_search = Process.GetProcessesByName("FarmTogether");

            if (process_search.Length != 0)
            {
                ProcessModuleCollection modules = process_search[0].Modules;
                ProcessModule dll = null;
                foreach (ProcessModule i in modules)
                {
                    if (i.ModuleName == "UnityPlayer.dll")
                    {
                        dll = i;
                        break;
                    }
                }
                try
                {

                    long DllAddrPlus = dll.BaseAddress.ToInt64() + ((IntPtr)0x14d6118).ToInt64();//基址偏移0x14d6118
                    long nextaddr = MRead(DllAddrPlus);
                    nextaddr = MRead(nextaddr + 0x8);//后续的偏移
                    nextaddr = MRead(nextaddr + 0x10);
                    nextaddr = MRead(nextaddr + 0x30);
                    nextaddr = MRead(nextaddr + 0x28);
                    nextaddr = MRead(nextaddr + 0x28);//最终基址（偏移前）
                    int nowdir = (int)MRead(nextaddr + 0xac, false);
                    if(TgtDir != 0)
                    {
                        MWrite(nextaddr + 0xac, TgtDir);
                        return;
                    }
                    //下面自动调整至最近的正方向朝向
                    if(nowdir < DirNorthWest || nowdir >= DirSouthWest)
                    {
                        MWrite(nextaddr + 0xac, DirWest);
                    }
                    else if(nowdir >= DirNorthWest && nowdir < DirNorthEast)
                    {
                        MWrite(nextaddr + 0xac, DirNorth);
                    }
                    else if (nowdir >= DirNorthEast && nowdir < DirSouthEast)
                    {
                        MWrite(nextaddr + 0xac, DirEast);
                    }
                    else
                    {
                        MWrite(nextaddr + 0xac, DirSouth);
                    }
                }
                catch { }
            }
        }

        
        private void GotoNextSeason(object sender, RoutedEventArgs e)//跳至下个季节
        {
            SeasonChange();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F11)
                GotoNextSeason(null, null);
        }

        private void SeasonSelect(object sender, RoutedEventArgs e)
        {
            int targetS = Convert.ToInt32(((System.Windows.Controls.Button)sender).Name.Split('_')[1]);//目标季节代号
            SeasonChange(targetS);
        }

        private void SeasonChange(int TargetSeason = -1)
        {
            Process[] MyProcess = Process.GetProcessesByName("FarmTogether");
            if (MyProcess.Length <= 0)
                return;
            try
            {
                long baseaddr = TickBaseAddress;
                int NowSeason = (int)MRead(baseaddr, false);
                if (NowSeason == 4)
                    NowSeason = 3;
                if (baseaddr == (long)0)
                    return;
                long NextSeasonTick = MRead(baseaddr + 0x8, false);//注意要读的Tick是32位int，要读4位，但反回的还是64位，不影响
                if (TargetSeason == -1)//说明是跳至下个季节，而非选定的季节
                    MWrite(baseaddr + 0xc, (int)NextSeasonTick - 1);
                else//跳至特定季节
                    MWrite(baseaddr + 0xc, (int)NextSeasonTick - 1 + ((8 + TargetSeason - NowSeason) % 4) * 1020);
            }
            catch { }
            
        }

        private void StopAll(object sender, RoutedEventArgs e)
        {
            if (RunningThread != null)
            {
                RunningThread.Abort();
                RunningThread = null;
            }
            IntPtr FTPtr = FindWindow(null, "FarmTogether");
            DelegateOfKeyEvent dwu = WkeyUp;
            this.Dispatcher.Invoke(dwu);
            DelegateOfKeyEvent dspu = SpacekeyUp;
            this.Dispatcher.Invoke(dspu);
        }

        //自动种植
        private void AutoPlant(object sender, RoutedEventArgs e)
        {
            if (RunningThread != null)
                return;
            Process[] MyProcess = Process.GetProcessesByName("FarmTogether");
            if (MyProcess.Length <= 0)
                return;
            Thread atpTrd = new Thread(AutoPlantThread);
            atpTrd.IsBackground = true;
            RunningThread = atpTrd;
            atpTrd.Start();
        }
        private void AutoPlantThread()
        {
            try
            {
                IntPtr FTPtr = FindWindow(null, "FarmTogether");
                GameWindowPtr = FTPtr;
                ShowWindow(FTPtr, SW_RESTORE);
                SetForegroundWindow(FTPtr);
                while (true)
                {
                    TurnFaceDirection();
                    DelegateOfKeyEvent dsd = SpacekeyDown;
                    this.Dispatcher.Invoke(dsd);
                    DelegateOfKeyEvent dsu = SpacekeyUp;
                    this.Dispatcher.Invoke(dsu);
                    Thread.Sleep(1600);
                    DelegateOfKeyEvent dwd = WkeyDown;
                    this.Dispatcher.Invoke(dwd);
                    Thread.Sleep(1227);
                    DelegateOfKeyEvent dwu = WkeyUp;
                    this.Dispatcher.Invoke(dwu);
                    Thread.Sleep(500);
                }

            }
            catch { }
        }

        private void WkeyDown()
        {
            keybd_event(87, 0, 0, 0);
        }
        private void WkeyUp()
        {
            keybd_event(87, 0, 2, 0);
        }
        private void AkeyDown()
        {
            keybd_event(65, 0, 0, 0);
        }
        private void AkeyUp()
        {
            keybd_event(65, 0, 2, 0);
        }
        private void SkeyDown()
        {
            keybd_event(83, 0, 0, 0);
        }
        private void SkeyUp()
        {
            keybd_event(83, 0, 2, 0);
        }
        private void DkeyDown()
        {
            keybd_event(68, 0, 0, 0);
        }
        private void DkeyUp()
        {
            keybd_event(68, 0, 2, 0);
        }
        private void SpacekeyDown()
        {
            keybd_event(32, 0, 0, 0);
        }
        private void SpacekeyUp()
        {
            keybd_event(32, 0, 2, 0);
        }

        private void AutoPlantDouble(object sender, RoutedEventArgs e)
        {
            if (RunningThread != null)
                return;
            Process[] MyProcess = Process.GetProcessesByName("FarmTogether");
            if (MyProcess.Length <= 0)
                return;
            Thread atpdTrd = new Thread(AutoPlantDoubleThread);
            atpdTrd.IsBackground = true;
            RunningThread = atpdTrd;
            atpdTrd.Start();
        }
        private void AutoPlantDoubleThread()
        {
            try
            {
                IntPtr FTPtr = FindWindow(null, "FarmTogether");
                GameWindowPtr = FTPtr;
                ShowWindow(FTPtr, SW_RESTORE);
                SetForegroundWindow(FTPtr);
                while (true)
                {
                    TurnFaceDirection();
                    DelegateOfKeyEvent dsd = SpacekeyDown;
                    this.Dispatcher.Invoke(dsd);
                    Thread.Sleep(2200);
                    DelegateOfKeyEvent dsu = SpacekeyUp;
                    this.Dispatcher.Invoke(dsu);
                    Thread.Sleep(500);
                    DelegateOfKeyEvent dwd = WkeyDown;
                    this.Dispatcher.Invoke(dwd);
                    Thread.Sleep(1227);
                    DelegateOfKeyEvent dwu = WkeyUp;
                    this.Dispatcher.Invoke(dwu);
                    Thread.Sleep(500);
                }

            }
            catch { }
        }

        private void ToolHelp(object sender, RoutedEventArgs e)
        {
            NormalHelp nh = new NormalHelp();
            nh.ShowDialog();
        }

        private void ChangeGiftStoreRow(object sender, RoutedEventArgs e)//设定礼物店的列数
        {
            StoreRow = Convert.ToInt32(((System.Windows.Controls.RadioButton)sender).Name.Split('_')[1]);
        }

        private void AutoContinueSpace(object sender, RoutedEventArgs e)
        {
            if (RunningThread != null)
                return;
            Process[] MyProcess = Process.GetProcessesByName("FarmTogether");
            if (MyProcess.Length <= 0)
                return;
            IntPtr FTPtr = FindWindow(null, "FarmTogether");
            GameWindowPtr = FTPtr;
            ShowWindow(FTPtr, SW_RESTORE);
            SetForegroundWindow(FTPtr);
            DelegateOfKeyEvent dsd = SpacekeyDown;
            this.Dispatcher.Invoke(dsd);
        }

        private void AutoMedal(object sender, RoutedEventArgs e)//自动在礼物店换奖牌
        {
            if (RunningThread != null)
                return;
            Process[] MyProcess = Process.GetProcessesByName("FarmTogether");
            if (MyProcess.Length <= 0)
                return;
            Thread atmTrd = new Thread(AutoMedalThread);
            atmTrd.IsBackground = true;
            RunningThread = atmTrd;
            storenums = GetIntFromTxt(TxtGiftStoreNum.Text);
            if(storenums > 0)
            {
                StreamWriter sw = new StreamWriter("Config.txt");
                sw.Write(StoreRow.ToString() + "#" + storenums.ToString());
                sw.Close();
            }
            atmTrd.Start();
        }
        private void AutoMedalThread()
        {
            try
            {
                IntPtr FTPtr = FindWindow(null, "FarmTogether");
                GameWindowPtr = FTPtr;
                ShowWindow(FTPtr, SW_RESTORE);
                SetForegroundWindow(FTPtr);
                if (storenums <= 0)
                {
                    RunningThread = null;
                    return;
                }
                if (StoreRow == 1)//1列礼物店，往前走，互动，如此直到终点，折返，到起点，如此重复
                {
                    while (true)
                    {
                        TurnFaceDirection();
                        mouse_event(0x08, 0, 0, 1, 0);
                        mouse_event(0x10, 0, 0, 1, 0);
                        Thread.Sleep(100);
                        mouse_event(0x08, 0, 0, 1, 0);
                        mouse_event(0x10, 0, 0, 1, 0);
                        Thread.Sleep(100);
                        DelegateOfKeyEvent dsd = SpacekeyDown;
                        this.Dispatcher.Invoke(dsd);
                        DelegateOfKeyEvent dsu = SpacekeyUp;
                        this.Dispatcher.Invoke(dsu);
                        Thread.Sleep(2000);
                        for (int i = 0; i < storenums - 1; i++)
                        {
                            DelegateOfKeyEvent dwd1 = WkeyDown;
                            this.Dispatcher.Invoke(dwd1);
                            Thread.Sleep(732);
                            DelegateOfKeyEvent dwu1 = WkeyUp;
                            this.Dispatcher.Invoke(dwu1);
                            Thread.Sleep(500);
                            DelegateOfKeyEvent dspd1 = SpacekeyDown;
                            this.Dispatcher.Invoke(dspd1);
                            DelegateOfKeyEvent dspu1 = SpacekeyUp;
                            this.Dispatcher.Invoke(dspu1);
                            Thread.Sleep(2000);
                        }
                        for (int i = 0; i < storenums - 1; i++)
                        {
                            DelegateOfKeyEvent dsd2 = SkeyDown;
                            this.Dispatcher.Invoke(dsd2);
                            Thread.Sleep(732);
                            DelegateOfKeyEvent dsu2 = SkeyUp;
                            this.Dispatcher.Invoke(dsu2);
                            Thread.Sleep(500);
                        }
                    }
                }
                else//两列礼物店 环状互动
                {
                    if (storenums > 15)
                        storenums = 15;
                    while(true)
                    {
                        TurnFaceDirection();
                        mouse_event(0x08, 0, 0, 1, 0);
                        mouse_event(0x10, 0, 0, 1, 0);
                        Thread.Sleep(100);
                        mouse_event(0x08, 0, 0, 1, 0);
                        mouse_event(0x10, 0, 0, 1, 0);
                        Thread.Sleep(100);
                        DelegateOfKeyEvent dspd = SpacekeyDown;
                        this.Dispatcher.Invoke(dspd);
                        DelegateOfKeyEvent dspu = SpacekeyUp;
                        this.Dispatcher.Invoke(dspu);
                        Thread.Sleep(2000);
                        for (int i = 0; i < storenums - 1; i++)
                        {
                            DelegateOfKeyEvent dwd1 = WkeyDown;
                            this.Dispatcher.Invoke(dwd1);
                            Thread.Sleep(732);
                            DelegateOfKeyEvent dwu1 = WkeyUp;
                            this.Dispatcher.Invoke(dwu1);
                            Thread.Sleep(500);
                            DelegateOfKeyEvent dspd1 = SpacekeyDown;
                            this.Dispatcher.Invoke(dspd1);
                            DelegateOfKeyEvent dspu1 = SpacekeyUp;
                            this.Dispatcher.Invoke(dspu1);
                            Thread.Sleep(2000);
                        }
                        DelegateOfKeyEvent ddd2 = DkeyDown;
                        this.Dispatcher.Invoke(ddd2);
                        Thread.Sleep(250);
                        DelegateOfKeyEvent ddu2 = DkeyUp;
                        Thread.Sleep(200);
                        this.Dispatcher.Invoke(ddu2);
                        DelegateOfKeyEvent dspd2 = SpacekeyDown;
                        this.Dispatcher.Invoke(dspd2);
                        DelegateOfKeyEvent dspu2 = SpacekeyUp;
                        this.Dispatcher.Invoke(dspu2);
                        Thread.Sleep(2000);
                        for (int i = 0; i < storenums - 1; i++)
                        {
                            DelegateOfKeyEvent dsd3 = SkeyDown;
                            this.Dispatcher.Invoke(dsd3);
                            Thread.Sleep(732);
                            DelegateOfKeyEvent dsu3 = SkeyUp;
                            this.Dispatcher.Invoke(dsu3);
                            Thread.Sleep(500);
                            DelegateOfKeyEvent dspd3 = SpacekeyDown;
                            this.Dispatcher.Invoke(dspd3);
                            DelegateOfKeyEvent dspu3 = SpacekeyUp;
                            this.Dispatcher.Invoke(dspu3);
                            Thread.Sleep(2000);
                        }
                        DelegateOfKeyEvent dad4 = AkeyDown;
                        this.Dispatcher.Invoke(dad4);
                        Thread.Sleep(250);
                        DelegateOfKeyEvent dau4 = AkeyUp;
                        this.Dispatcher.Invoke(dau4);
                    }
                    
                }
            }
            catch { }
            
        }

        private int GetIntFromTxt(string txt)
        {
            try
            {
                return Convert.ToInt32(txt);
            }
            catch
            {
                return 0;
            }
        }

        private void ToolHelpMedal(object sender, RoutedEventArgs e)
        {
            GiftHelp gh = new GiftHelp();
            gh.ShowDialog();
        }

        private void OpenSaveGeneratorWindow(object sender, RoutedEventArgs e)
        {
            //System.Windows.MessageBox.Show("提示：使用前建议先备份存档！");
            GameSaveCodeCreator gsccw = new GameSaveCodeCreator();
            gsccw.ShowDialog();
        }

        private void GzipUnpack(object sender, RoutedEventArgs e)
        {
            
            string fileName = "farm_1.data";
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (GZipStream decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    StreamReader reader = new StreamReader(decompressedStream);
                    string result = reader.ReadToEnd();//重点
                    reader.Close();
                    StreamWriter wt = new StreamWriter("wt.txt");
                    wt.Write(result);
                    wt.Close();
                    return;
                }
            }
            
           

        }

        private void OneClickChangeFlat(object sender, RoutedEventArgs e)
        {
            if (GameSaveCodeCreator.MainWindowSetFlat())
                System.Windows.MessageBox.Show("修改成功！修改平原后第一次进入游戏可能会加载很久，请多等一会儿。");
            else
                System.Windows.MessageBox.Show("修改未成功");
        }
    }
}
