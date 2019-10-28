using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;

namespace FarmTogetherAutoTools
{
    /// <summary>
    /// GameSaveCodeCreator.xaml 的交互逻辑
    /// </summary>
    public partial class GameSaveCodeCreator : Window
    {
        string CropName = "";
        public static string ChunkX = "";
        public static string ChunkY = "";
        string Tile0X = "";
        string Tile0Y = "";
        string Tile1X = "";
        string Tile1Y = "";
        public static string SaveCode = "";//即将不再使用，本来是显示要替换进游戏的代码字符串
        public static string TheSaveNumber = "NULL";
        public static string SaveAddress = "";//游戏存档完整地址
        public static string UserDataAddress = "";//用户信息完整地址
        public List<Image> AllMapChunk = new List<Image>();
        public static string gameinfopath = "";//游戏用户信息（farms.data）所在目录
        public static string gamesavepathNoName = "";//游戏存档所在目录
        List<string> farmnames = new List<string>();
        List<string> farmnums = new List<string>();
        List<string> ChunkIdDict = new List<string>();
        List<int> ChunkNowIdNum = new List<int>();//存储当前每个地块的实际Id在ChunkIdDict中的序号
        List<Key> ChunkIdKeys = new List<Key>();
        public static bool CbIsLoading = false;//因为加载ComboBox过程中可能会数次更换SelectedItem导致触发SelectionChanged，为了只在需要的时候进行，设定开始加载是置CbIsLoading为true，阻止Changed函数执行，直到遇到想要加载的存档再置false以允许触发Changed

        public static SData RootData = new SData(null, "");
        public static SData FarmInfoRootData = new SData(null, "");

        public static string DefaultItemCodes ="[{\"id\":\"AnimalHorseMagicArcane\"},{\"id\":\"AnimalHorseMagicFire\"},{\"id\":\"AnimalHorseMagicIce\"},{\"id\":\"FenceFlyingCandles\"},{\"id\":\"PetBirdOwlB\"},{\"id\":\"PetBirdOwlC\"},{\"id\":\"BackpackBackpackEaster\"},{\"id\":\"FarmhandWorkerMonoEasterB\"},{\"id\":\"BodyBoyCasualHalloween\"},{\"id\":\"BodyGirlCasualHalloween\"},{\"id\":\"DecorationEasterEggs\"},{\"id\":\"DecorationEasterBunny\"},{\"id\":\"HairHairBoyEaster\"},{\"id\":\"HairHairGirlEaster\"},{\"id\":\"TagPumpkin\"},{\"id\":\"FenceHalloweenLow\"},{\"id\":\"TreeHalloweenTree\"},{\"id\":\"HairHairGirlWitch\"},{\"id\":\"HairHairBoyWitch\"},{\"id\":\"FenceLifePreservers\"},{\"id\":\"HairHairBoyFisherman\"},{\"id\":\"HairHairGirlFisherman\"},{\"id\":\"PondBass\"},{\"id\":\"BodyBoyFisherman\"},{\"id\":\"BodyGirlFisherman\"},{\"id\":\"DecorationSubmarine\"},{\"id\":\"HairHairBoyFishermanB\"},{\"id\":\"HairHairGirlFishermanB\"},{\"id\":\"BodyBoyFishermanB\"},{\"id\":\"BodyGirlFishermanB\"},{\"id\":\"TreeXmasTree\"},{\"id\":\"HairHairBoyXmas\"},{\"id\":\"HairHairGirlXmas\"},{\"id\":\"TagCandyCane\"},{\"id\":\"HairHairBoyXmasGreen\"},{\"id\":\"HairHairGirlXmasGreen\"},{\"id\":\"DecorationGiftSleigh\"},{\"id\":\"DecorationSnowman\"},{\"id\":\"BodyBoyFiremanSanta\"},{\"id\":\"BodyGirlFiremanSanta\"},{\"id\":\"BodyGirlSkirtSanta\"},{\"id\":\"FenceXmasFence\"},{\"id\":\"FenceXmasFenceDoor\"},{\"id\":\"FenceXmasArch\"},{\"id\":\"GlassesGlassesStarGlow\"},{\"id\":\"AppliancePlanetarium\"},{\"id\":\"DecorationTelescope\"},{\"id\":\"TagHeart\"},{\"id\":\"AnimalUnicorn\"},{\"id\":\"DecorationYew\"},{\"id\":\"RoadPergolaValentines\"},{\"id\":\"DecorationBenchLovers\"},{\"id\":\"HairHairBoySanValentin\"},{\"id\":\"HairHairGirlSanValentin\"},{\"id\":\"FenceValentinesBalloonsPurple\"},{\"id\":\"FenceValentinesBalloonsPink\"},{\"id\":\"FlowerFoxglovePink\"},{\"id\":\"HairHairBoyStPatricks\"},{\"id\":\"HairHairGirlStPatricks\"},{\"id\":\"FlowerFoxglovePurple\"},{\"id\":\"FlowerFoxgloveYellow\"},{\"id\":\"BodyBoySuitPatricks\"},{\"id\":\"BodyGirlSuitPatricks\"},{\"id\":\"BodyGirlSkirtPatricks\"},{\"id\":\"BuildingPotOfGold\"},{\"id\":\"TagHorseshoe\"},{\"id\":\"RoadGemPath\"},{\"id\":\"AnimalRabbitEaster\"},{\"id\":\"RoadBrickChoco\"},{\"id\":\"FenceEasterLow\"},{\"id\":\"TagFlower\"},{\"id\":\"TreeFloweringCherry\"},{\"id\":\"FarmhandWorkerHawaii\"},{\"id\":\"HairHairGirlHanami\"},{\"id\":\"HairHairBoyHanami\"},{\"id\":\"FenceHanamiLow\"},{\"id\":\"FenceHanamiLights\"},{\"id\":\"HairHairGirlWitchB\"},{\"id\":\"HairHairBoyWitchB\"},{\"id\":\"HairHairBoyEasterB\"},{\"id\":\"HairHairGirlEasterB\"},{\"id\":\"DecorationBonfireLarge\"},{\"id\":\"HairHairBoySolsticio\"},{\"id\":\"HairHairGirlSolsticio\"},{\"id\":\"TreePistachio\"},{\"id\":\"BodyBoyCasualSolstice\"},{\"id\":\"BodyGirlCasualSolstice\"},{\"id\":\"RoadHotCoal\"},{\"id\":\"DecorationSunStele\"},{\"id\":\"FenceBoneArch\"},{\"id\":\"HairHairBoyExplorer\"},{\"id\":\"HairHairGirlExplorer\"},{\"id\":\"AccessoryPlushDino\"},{\"id\":\"DecorationSurfBoard\"},{\"id\":\"FlowerDandelion\"},{\"id\":\"DecorationCampingTent\"},{\"id\":\"BackpackBackpackButterfly\"},{\"id\":\"FarmhandWorkerCamo\"},{\"id\":\"FenceGlowingFungi\"},{\"id\":\"BackpackBackpackButterflyB\"},{\"id\":\"DecorationMerryGoRound\"},{\"id\":\"DecorationFerrisWheel\"},{\"id\":\"RoadXylophonePath\"},{\"id\":\"TreeChestnutTree\"},{\"id\":\"HairHairBoyCowboyCountry\"},{\"id\":\"HairHairGirlCowboyCountry\"},{\"id\":\"BodyBoyCowboyCountry\"},{\"id\":\"BodyGirlCowboyCountry\"},{\"id\":\"DecorationMusicStage\"},{\"id\":\"EmoteEmoteBaile3\"},{\"id\":\"HairHairBoyCrown\"},{\"id\":\"HairHairGirlCrown\"},{\"id\":\"DecorationCarpetSakuraBig\"},{\"id\":\"DecorationWindowTallLightsBlue\"},{\"id\":\"DecorationWindowTallLightsGreen\"},{\"id\":\"AccessoryCake\"},{\"id\":\"DecorationWindowTallLightsPink\"},{\"id\":\"DecorationWindowTallLightsYellow\"},{\"id\":\"DecorationCarpetStar\"},{\"id\":\"DecorationCarpetStarBig\"},{\"id\":\"FenceAnniversaryFence\"}]";
        public GameSaveCodeCreator()
        {
            InitializeComponent();
            AllMapChunk.Clear();
            //列表中加入所有Image控件
            {
                AllMapChunk.Add(ImgChunk_0_0);
                AllMapChunk.Add(ImgChunk_0_1);
                AllMapChunk.Add(ImgChunk_0_2);
                AllMapChunk.Add(ImgChunk_0_3);
                AllMapChunk.Add(ImgChunk_0_4);
                AllMapChunk.Add(ImgChunk_0_5);
                AllMapChunk.Add(ImgChunk_0_6);
                AllMapChunk.Add(ImgChunk_1_0);
                AllMapChunk.Add(ImgChunk_1_1);
                AllMapChunk.Add(ImgChunk_1_2);
                AllMapChunk.Add(ImgChunk_1_3);
                AllMapChunk.Add(ImgChunk_1_4);
                AllMapChunk.Add(ImgChunk_1_5);
                AllMapChunk.Add(ImgChunk_1_6);
                AllMapChunk.Add(ImgChunk_2_0);
                AllMapChunk.Add(ImgChunk_2_1);
                AllMapChunk.Add(ImgChunk_2_2);
                AllMapChunk.Add(ImgChunk_2_3);
                AllMapChunk.Add(ImgChunk_2_4);
                AllMapChunk.Add(ImgChunk_2_5);
                AllMapChunk.Add(ImgChunk_2_6);
                AllMapChunk.Add(ImgChunk_3_0);
                AllMapChunk.Add(ImgChunk_3_1);
                AllMapChunk.Add(ImgChunk_3_2);
                AllMapChunk.Add(ImgChunk_3_3);
                AllMapChunk.Add(ImgChunk_3_4);
                AllMapChunk.Add(ImgChunk_3_5);
                AllMapChunk.Add(ImgChunk_3_6);
                AllMapChunk.Add(ImgChunk_4_0);
                AllMapChunk.Add(ImgChunk_4_1);
                AllMapChunk.Add(ImgChunk_4_2);
                AllMapChunk.Add(ImgChunk_4_3);
                AllMapChunk.Add(ImgChunk_4_4);
                AllMapChunk.Add(ImgChunk_4_5);
                AllMapChunk.Add(ImgChunk_4_6);
                AllMapChunk.Add(ImgChunk_5_0);
                AllMapChunk.Add(ImgChunk_5_1);
                AllMapChunk.Add(ImgChunk_5_2);
                AllMapChunk.Add(ImgChunk_5_3);
                AllMapChunk.Add(ImgChunk_5_4);
                AllMapChunk.Add(ImgChunk_5_5);
                AllMapChunk.Add(ImgChunk_5_6);
                AllMapChunk.Add(ImgChunk_6_0);
                AllMapChunk.Add(ImgChunk_6_1);
                AllMapChunk.Add(ImgChunk_6_2);
                AllMapChunk.Add(ImgChunk_6_3);
                AllMapChunk.Add(ImgChunk_6_4);
                AllMapChunk.Add(ImgChunk_6_5);
                AllMapChunk.Add(ImgChunk_6_6);
            }
            foreach (var item in AllMapChunk)
            {
                item.Source = new BitmapImage(new Uri("/Resources/Flat_Chunk.png", UriKind.Relative));
            }
            ChunkIdDict.Clear();
            ChunkIdKeys.Clear();
            //列表中加入所有已知的ChunkId类型
            {
                ChunkIdDict.Add("Flat_Chunk");
                ChunkIdDict.Add("BigHill");
                ChunkIdDict.Add("BigRiver");
                ChunkIdDict.Add("CrackCross");
                ChunkIdDict.Add("Cracked");
                ChunkIdDict.Add("CrackMountain");
                ChunkIdDict.Add("CrackRiver");
                ChunkIdDict.Add("FiveMountains");
                ChunkIdDict.Add("HighMountain");
                ChunkIdDict.Add("Hills");
                ChunkIdDict.Add("Lake");
                ChunkIdDict.Add("Latifundio");
                ChunkIdDict.Add("Mountain_Hills");
                ChunkIdDict.Add("Mountain_MountainHill");
                ChunkIdDict.Add("Mountain_MountainHill2");
                ChunkIdDict.Add("Mountain_MountainRock");
                ChunkIdDict.Add("Mountain_Starting");
                ChunkIdDict.Add("Mountain_ThreeMountains");
                ChunkIdDict.Add("Mountain_Valley");
                ChunkIdDict.Add("Mountain_Valley2");
                ChunkIdDict.Add("MountainBig");
                ChunkIdDict.Add("MountainMiddle");
                ChunkIdDict.Add("MountainRiver");
                ChunkIdDict.Add("Ponds");
                ChunkIdDict.Add("Prairie");
                ChunkIdDict.Add("RiverCross");
                ChunkIdDict.Add("RocksRiver");
                ChunkIdDict.Add("RockyChunk");
                ChunkIdDict.Add("SplitMountain");
                ChunkIdDict.Add("StartingChunkA");
                ChunkIdDict.Add("TripleHill");
                ChunkIdDict.Add("TwoHills");
                ChunkIdDict.Add("TwoMountains");
                ChunkIdDict.Add("Valley");
                ChunkIdDict.Add("Water_Cracks");
                ChunkIdDict.Add("Water_Hill");
                ChunkIdDict.Add("Water_Hill2");
                ChunkIdDict.Add("Water_Hills");
                ChunkIdDict.Add("Water_Lake1");
                ChunkIdDict.Add("Water_Lake2");
                ChunkIdDict.Add("Water_MountainLake");
                ChunkIdDict.Add("Water_Ponds");
                ChunkIdDict.Add("Water_River1");
                ChunkIdDict.Add("Water_River2");
                ChunkIdDict.Add("Water_River3");
                ChunkIdDict.Add("Water_River4");
                ChunkIdDict.Add("Water_Starting");
                ChunkIdDict.Add("Water_TwoRivers");
            }
            TxtItemCodes.Text = DefaultItemCodes;

            for (int i = 0; i < 49; i++)
            {
                ChunkNowIdNum.Add(0);
            }
        }
        


        private void OpenFileAndLoad(object sender, RoutedEventArgs e)//打开存档文件并将其同目录下的所有文档添加到备选下拉菜单，同时加载选择的存档到内存中
        {
            CbIsLoading = true;
            Microsoft.Win32.OpenFileDialog op = new Microsoft.Win32.OpenFileDialog();
            op.Filter = "游戏存档文件（farm_*.data）|farm_*.data";
            op.ShowDialog();
            if (op.FileName == "" || op.FileName == null)
                return;
            SaveAddress = op.FileName;
            string[] splt = SaveAddress.Split('_');
            string savenum = splt[splt.Length - 1];
            savenum = savenum.Split('.')[0];
            TheSaveNumber = savenum;//剥离出存档编码并存储
            try
            {
                string savedir = System.IO.Path.GetDirectoryName(SaveAddress);
                gamesavepathNoName = savedir;
                DirectoryInfo savedirInfo = new DirectoryInfo(savedir);
                string farminfodir = savedirInfo.Parent.FullName;
                if(File.Exists(farminfodir + "\\farms.data"))
                {
                    gameinfopath = farminfodir;
                    if(!InitComboBox())
                        SData.LoadFromSave(SaveAddress, RootData);
                    BtSaveTheChange.Background = new SolidColorBrush(Color.FromRgb(162,243,139));
                    BtAutoFlat.Background = new SolidColorBrush(Color.FromRgb(162, 243, 139));
                }
                else
                {
                    Cb_FileNum.IsEnabled = false;
                    SData.LoadFromSave(SaveAddress, RootData);
                    BtSaveTheChange.Background = new SolidColorBrush(Color.FromRgb(162, 243, 139));
                    BtAutoFlat.Background = new SolidColorBrush(Color.FromRgb(162, 243, 139));
                }

            }
            catch { }
        }

        private void SaveGame()
        {
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\DataGenerated"))
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\DataGenerated");
            StreamWriter SW0 = new StreamWriter("DataGenerated\\farm_" + TheSaveNumber);
            SData.SaveFromSData(RootData, SW0);
            SW0.Close();
            if (SData.GzipPack())
                MessageBox.Show("修改成功！修改后第一次进入游戏并载入存档可能会有较长时间卡顿，请耐心等待。");
            else
                MessageBox.Show("修改失败。");
        }

        private void SetFlatAuto(object sender, RoutedEventArgs e)
        {
            if (RootData.Sons.Count <= 0 || gamesavepathNoName == "")
                OpenFileAndLoad(null, null);
            if (RootData.Sons.Count <= 0 || gamesavepathNoName == "")
                return;
            SData chunks = RootData.TurnTo("Chunks");
            foreach (var chunk in chunks.Sons)
            {
                Console.WriteLine(chunk.TurnTo("ChunkPosition").Value);
                chunk.TurnTo("ChunkId").Value = "\"Flat_Chunk\"";
            }
            RefreshChunks();
            SaveGame();
        }

        public static bool MainWindowSetFlat()//从主窗口过来的一键设置平原
        {
            Microsoft.Win32.OpenFileDialog op = new Microsoft.Win32.OpenFileDialog();
            op.Filter = "游戏存档文件（farm_*.data）|farm_*.data";
            op.ShowDialog();
            if (op.FileName == "" || op.FileName == null)
                return false;
            SaveAddress = op.FileName;
            SData.LoadFromSave(SaveAddress, RootData);

            SData chunks = RootData.TurnTo("Chunks");
            foreach (var chunk in chunks.Sons)
            {
                Console.WriteLine(chunk.TurnTo("ChunkPosition").Value);
                chunk.TurnTo("ChunkId").Value = "\"Flat_Chunk\"";
            }
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\DataGenerated"))
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\DataGenerated");
            StreamWriter SW0 = new StreamWriter("DataGenerated\\farm_" + TheSaveNumber);
            SData.SaveFromSData(RootData, SW0);
            SW0.Close();
            return SData.GzipPack();
        }

        private void ActivityItemsCodeChange(object sender, RoutedEventArgs e)
        {
            if(FarmInfoRootData.Sons.Count() <= 0)
            {
                Microsoft.Win32.OpenFileDialog op = new Microsoft.Win32.OpenFileDialog();
                op.Title = "请选择farms.data文件，这通常在remote文件夹中";
                op.Filter = "farms.data文件|farms.data";
                op.ShowDialog();
                string addr = op.FileName;
                if (addr == "" || addr == null)
                    return;
                SData.TotalLoadFromUserData(addr, FarmInfoRootData);
            }

            //下面是修改过程
            string itemcodes = TxtItemCodes.Text;
            string legalcode = itemcodes.Replace("\"Rewards\":", "").TrimEnd(',');
            if (legalcode[0] != '[' || legalcode[legalcode.Length-1] != ']')
            {
                MessageBox.Show("代码格式错误。请检查输入的代码。");
                return;
            }
            SData dt = FarmInfoRootData.TurnTo("\"Rewards\"");
            dt.sonKind = 0;
            dt.Value = legalcode;

            if (SData.TotalSaveUserData())
                MessageBox.Show("修改成功！");
        }

        private void AutoActivityItemsCodeChange(object sender, RoutedEventArgs e)
        {
            Button obj = (Button)sender;
            if (FarmInfoRootData.Sons.Count() > 0)
                ActivityItemsCodeChange(null, null);
            else if(AutoSearch())
            {
                SData.TotalLoadFromUserData(gameinfopath + "\\farms.data", FarmInfoRootData);
                string itemcodes = TxtItemCodes.Text;
                string legalcode = itemcodes.Replace("\"Rewards\":", "").TrimEnd(',');
                Console.WriteLine(legalcode);

                SData dt = FarmInfoRootData.TurnTo("\"Rewards\"");
                dt.sonKind = 0;
                dt.Value = legalcode;

                if (SData.TotalSaveUserData())
                    MessageBox.Show("修改成功！");
            }
            else
            {
                obj.Content = "自动搜索失败，请使用下面的按钮手动选择存档";
            }
        }

        private bool AutoSearch()
        {
            List<string> dirs = new List<string>();
            dirs.Add("C:\\Program Files\\Steam\\userdata");
            dirs.Add("C:\\Program Files(x86)\\Steam\\userdata");
            dirs.Add("C:\\Steam\\userdata");
            dirs.Add("D:\\Program Files\\Steam\\userdata");
            dirs.Add("D:\\Program Files(x86)\\Steam\\userdata");
            dirs.Add("D:\\Steam\\userdata");
            dirs.Add("E:\\Program Files\\Steam\\userdata");
            dirs.Add("E:\\Program Files(x86)\\Steam\\userdata");
            dirs.Add("E:\\Steam\\userdata");
            dirs.Add("F:\\Program Files\\Steam\\userdata");
            dirs.Add("F:\\Program Files(x86)\\Steam\\userdata");
            dirs.Add("F:\\Steam\\userdata");
            dirs.Add("G:\\Program Files\\Steam\\userdata");
            dirs.Add("G:\\Program Files(x86)\\Steam\\userdata");
            dirs.Add("G:\\Steam\\userdata");

            FileInfo[] allsaves = null;//所有的农场存档文件，真的有用不要删
            bool found = false;//找到可用的存档目录后跳出循环 或是没有找到直接return 的标志

            //下面根据（默认的）存档的可能位置依次寻找确实有存档的文件夹，并提取出所有的存档文件到 allsaves
            foreach (var dir in dirs)
            {
                //下面是自动从默认的几个目录下寻找游戏存档文件所在位置
                if (Directory.Exists(dir))
                {
                    DirectoryInfo usersdir = new DirectoryInfo(dir);
                    DirectoryInfo[] users = usersdir.GetDirectories();
                    foreach (var user in users)
                    {
                        if (Directory.Exists(user.FullName + "\\673950"))
                        {
                            if (!Directory.Exists(user.FullName + "\\673950\\remote"))
                                continue;
                            if (!Directory.Exists(user.FullName + "\\673950\\remote\\Farms"))
                                continue;
                            DirectoryInfo farmsdir = new DirectoryInfo(user.FullName + "\\673950\\remote\\Farms");
                            allsaves = farmsdir.GetFiles("farm_*.data");
                            if (allsaves.Length > 0)
                            {
                                gameinfopath = user.FullName + "\\673950\\remote";
                                gamesavepathNoName = user.FullName + "\\673950\\remote\\Farms";
                                found = true;
                                break;
                            }
                        }

                    }
                }

                if (found)
                    break;
            }
            if (found)
                return true;
            else
                return false;
        }//自动搜寻存档位置并返回成功或失败的bool

        private bool InitComboBox()//按照选好的或者搜到的位置配置下拉列表供选择
        {
            //下面是选择存档号
            Cb_FileNum.Items.Clear();
            SData.TotalLoadFromUserData(gameinfopath + "\\farms.data", FarmInfoRootData);
            farmnames = new List<string>();
            farmnums = new List<string>();
            int count = 0;//有效存档个数
            foreach (var farm in FarmInfoRootData.TurnTo("\"Farms\"").Sons)
            {
                
                string thenum = farm.TurnTo("\"slot\"").Value;
                if (File.Exists(gamesavepathNoName + "\\farm_" + thenum + ".data"))
                {
                    string thename = farm.TurnTo("\"name\"").Value;
                    if (farmnames.Contains(thename))
                    {
                        thename += "_" + thenum;
                    }
                    farmnames.Add(thename);
                    Cb_FileNum.Items.Add(thename);
                    farmnums.Add(thenum);

                    if (count == 0)
                    {
                        Cb_FileNum.SelectedItem = thename;
                        Console.WriteLine("usecount=0 at count" + count.ToString());
                    }
                    if(thenum == TheSaveNumber)
                    {
                        CbIsLoading = false;
                        Cb_FileNum.SelectedItem = thename;
                        Console.WriteLine("use num " + TheSaveNumber + " at count" + count.ToString());
                    }
                    count++;
                }
            }
            if (count > 0)
            {
                CbIsLoading = false;
                Cb_FileNum.IsEnabled = true;
                return true;
            }
            else
            {
                CbIsLoading = false;
                return false;
            }
        }

        private void SearchSaveDir(object sender, RoutedEventArgs e)//自动搜寻存档位置并加载
        {
            
            if (!AutoSearch())
            {
                BtSearchSave.Content = "未搜索到，请手动加载";
                BtSearchSave.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                BtSearchSave.Background = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                return;
            }
            if(!InitComboBox())
            {
                BtSearchSave.Content = "未搜索到，请手动加载";
                BtSearchSave.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                BtSearchSave.Background = new SolidColorBrush(Color.FromRgb(221, 221, 221));
                return;
            }
            BtSaveTheChange.Background = new SolidColorBrush(Color.FromRgb(162, 243, 139));
            BtAutoFlat.Background = new SolidColorBrush(Color.FromRgb(162, 243, 139));
            RefreshChunks();
        }

        public void RefreshChunks()
        {
            try
            {
                SData chunkdata = RootData.TurnTo("Chunks");
                foreach (var chunk in chunkdata.Sons)
                {
                    string coord = chunk.TurnTo("ChunkPosition").Value.Trim('\"');
                    string ckid = chunk.TurnTo("ChunkId").Value.Trim('\"');
                    int chunkpos = Convert.ToInt32(coord.Split(',')[0]) * 7 + Convert.ToInt32(coord.Split(',')[1]);
                    if (ChunkIdDict.Contains(ckid))
                    {
                        ChunkNowIdNum[chunkpos] = ChunkIdDict.IndexOf(ckid);
                        AllMapChunk[chunkpos].Source = new BitmapImage(new Uri("/Resources/" + ckid + ".png", UriKind.Relative));
                    }
                    else
                    {
                        ChunkNowIdNum[chunkpos] = 0;
                        AllMapChunk[chunkpos].Source = new BitmapImage(new Uri("/Resources/Flat_Chunk.png", UriKind.Relative));
                    }
                }
            }
            catch { }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Cb_FileNum.Items.Count <= 0)
                return;
            if (CbIsLoading)
                return;
            TheSaveNumber = farmnums[farmnames.IndexOf(Cb_FileNum.SelectedItem.ToString())];
            SaveAddress = gamesavepathNoName + "\\farm_" + TheSaveNumber + ".data";
            SData.LoadFromSave(SaveAddress, RootData);
            RefreshChunks();
        }

        private void HideChunkLines(object sender, RoutedEventArgs e)
        {
            Line0.Visibility = Visibility.Hidden;
            Line1.Visibility = Visibility.Hidden;
            Line2.Visibility = Visibility.Hidden;
            Line3.Visibility = Visibility.Hidden;
            Line4.Visibility = Visibility.Hidden;
            Line5.Visibility = Visibility.Hidden;
            Line6.Visibility = Visibility.Hidden;
            Line7.Visibility = Visibility.Hidden;
            Line8.Visibility = Visibility.Hidden;
            Line9.Visibility = Visibility.Hidden;
            Line10.Visibility = Visibility.Hidden;
            Line11.Visibility = Visibility.Hidden;
        }

        private void ShowChunkLines(object sender, RoutedEventArgs e)
        {
            Line0.Visibility = Visibility.Visible;
            Line1.Visibility = Visibility.Visible;
            Line2.Visibility = Visibility.Visible;
            Line3.Visibility = Visibility.Visible;
            Line4.Visibility = Visibility.Visible;
            Line5.Visibility = Visibility.Visible;
            Line6.Visibility = Visibility.Visible;
            Line7.Visibility = Visibility.Visible;
            Line8.Visibility = Visibility.Visible;
            Line9.Visibility = Visibility.Visible;
            Line10.Visibility = Visibility.Visible;
            Line11.Visibility = Visibility.Visible;
        }

        private void ChangeChunkIdByWheel(object sender, MouseWheelEventArgs e)
        {
            if (RootData.Sons.Count() <= 0)
                return;
            Image obj = (Image)sender;
            string x = obj.Name.Split('_')[1];
            string y = obj.Name.Split('_')[2];
            int num = Convert.ToInt32(x) * 7 + Convert.ToInt32(y);
            int inc = 1;
            if (e.Delta > 0)
                inc = -1;
            ChunkNowIdNum[num] = (ChunkNowIdNum[num] + inc + ChunkIdDict.Count()) % ChunkIdDict.Count();
            if (ChunkNowIdNum[num] < 0)
                return;
            obj.Source = new BitmapImage(new Uri("/Resources/" + ChunkIdDict[ChunkNowIdNum[num]] + ".png", UriKind.Relative));
            SData thechunk = RootData.TurnTo("Chunks").Search("ChunkPosition", "\"" + x + "," + y + "\"");
            thechunk.TurnTo("ChunkId").Value = "\"" + ChunkIdDict[ChunkNowIdNum[num]] + "\"";
            Console.WriteLine(thechunk.TurnTo("ChunkPosition").Value);
        }

        private void ChangeChunIdByKey(object sender, KeyEventArgs e)
        {
        }

        private void SaveMapFinal(object sender, RoutedEventArgs e)
        {
            if(RootData.Sons.Count() <=0 || TheSaveNumber == "NULL" || gamesavepathNoName == "")
            {
                MessageBox.Show("请先加载游戏存档");
                return;
            }
            else
            {
                SaveGame();
            }
        }

        
    }
}
