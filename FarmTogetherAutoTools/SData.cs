using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarmTogetherAutoTools
{
    public class SData
    {
        public string Key;
        public string Value;
        public int sonKind;//0代表Value值，1代表有大括号表示的儿子，2代表有中括号表示的儿子
        public List<SData> Sons;
        public SData Father;
        public SData(SData fatherIN, string keyIN)
        {
            Key = keyIN;
            Value = "";
            sonKind = 0;
            Sons = new List<SData>();
            Father = fatherIN;
        }

        public SData TurnTo(string targetkey)
        {
            foreach (var son in Sons)
            {
                if (son.Key == targetkey)
                    return son;
            }
            return null;
        }

        public SData Search(string keyIN, string valueIN)//主要针对[]创造的树，从this的sons里面找具有grandson：的key为keyIN，值为ValueIN的那个son，返回这个son(即item)
        {
            foreach (var item in Sons)
            {
                foreach (var grandson in item.Sons)
                {
                    if (grandson.Key == keyIN && grandson.Value == valueIN)
                        return item;
                }
            }
            return null;
        }

        public static bool LoadFromSave(string saveaddr, SData root)//root作为根结点存储整个树
        {
            if (!GzipUnpack(saveaddr))//解压
                return false;

            StreamReader sr = new StreamReader("SaveGenerated\\farm_" + GameSaveCodeCreator.TheSaveNumber);
            root.sonKind = 0;
            root.Sons = new List<SData>();
            SData p = root;
            p.sonKind = 1;
            List<char> readchrs = new List<char>();
            char nowread = (char)sr.Read();
            char lastread = '0';
            bool realsign = true;//忽视在双引号之间的符号
            bool atend = false;//在结尾时会出现一堆逗号，哪个是不需要退至上层的，而到这个结尾的标志就是
            while(!sr.EndOfStream)
            {
                lastread = nowread;
                nowread = (char)sr.Read();
                if (string.Join("", readchrs.ToArray()) == "ViewedItems")
                {
                    atend = true;
                    //Console.WriteLine(string.Join("", readchrs.ToArray()) + "6666666666666666666");
                }
                if(atend && nowread != ']' && nowread != ':')//对于ViewedItems的特殊处理
                {
                    readchrs.Add(nowread);
                }
                else if(atend && nowread == ']')
                {
                    readchrs.Add(nowread);
                    atend = false;
                }
                else if (nowread == ':' && realsign)
                {
                    p = new SData(p, string.Join("", readchrs.ToArray()));
                    p.Father.Sons.Add(p);
                    //Console.WriteLine(string.Join("", readchrs.ToArray()) + "→");
                    readchrs.Clear();
                }
                else if ((nowread == ',' && realsign && (!atend)) || (nowread == '}' && realsign) || (nowread == ']' && (!atend) && lastread != '[' && realsign))
                {
                    atend = false;
                    if (readchrs.Count > 0)
                    {
                        p.Value = string.Join("", readchrs.ToArray());
                        //Console.WriteLine(string.Join("", readchrs.ToArray()) + "←");
                    }
                    readchrs.Clear();
                    p = p.Father;
                }
                else if (nowread == '[' && realsign)
                {
                    p.sonKind = 2;
                }
                else if (nowread == '{' && realsign)
                {
                    if (p.sonKind == 2)
                    {
                        p = new SData(p, "");
                        //Console.WriteLine(p.Father.Key + "[的{ →");
                        p.Father.Sons.Add(p);
                        p.sonKind = 1;
                    }
                    else
                    {
                        p.sonKind = 1;
                    }
                }
                else if (nowread == '\"')//会出现"144,23"这种影像数据读取的逗号，该逗号实际上属于value而不是分隔符，这些符号由于在""引号中间，通过识别复数个引号（数次反转realsign）来达到忽略这种逗号（或冒号等）的目的
                {
                    realsign = !realsign;
                    readchrs.Add(nowread);
                }
                else
                {
                    readchrs.Add(nowread);
                }

            }
            sr.Close();
            if (p == root && root.Sons.Count() > 0)
                Console.WriteLine("成功加载存档");
            return true;
        }

        public static bool SaveFromSData(SData dt, StreamWriter sw)
        {
            if(dt.Key != "" && dt.Key != null)
            {
                sw.Write(dt.Key + ":");
            }
            

            if(dt.sonKind ==0)
            {
                sw.Write(dt.Value);
                if(dt.Father.Sons.IndexOf(dt) != (dt.Father.Sons.Count()-1))
                {
                    sw.Write(",");
                }
            }
            else
            {
                if (dt.sonKind == 1)
                {
                    sw.Write('{');
                }
                else if (dt.sonKind == 2)
                {
                    sw.Write('[');
                }

                foreach (var son in dt.Sons)
                {
                    SaveFromSData(son, sw);
                }
                if (dt.sonKind == 1)
                {
                    sw.Write('}');
                }
                else if (dt.sonKind == 2)
                {
                    sw.Write(']');
                }
                if(dt.Father != null)
                {
                    if (dt.Father.Sons.IndexOf(dt) != (dt.Father.Sons.Count() - 1))
                    {
                        sw.Write(",");
                    }
                }
                
            }
            
            return true;
        }

        public static bool GzipUnpack(string fileName)//同时会提取出存档代号并储存，仅用于存档嗷，不用于用户农场信息的解压
        {
            string[] splt = fileName.Split('_');
            string savenum = splt[splt.Length - 1];
            savenum = savenum.Split('.')[0];
            GameSaveCodeCreator.TheSaveNumber = savenum;
            GameSaveCodeCreator.SaveAddress = fileName;
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (GZipStream decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    StreamReader reader = new StreamReader(decompressedStream);
                    string result = reader.ReadToEnd();
                    reader.Close();
                    if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\SaveGenerated"))
                        Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\SaveGenerated");
                    StreamWriter sw = new StreamWriter("SaveGenerated\\farm_"+savenum);
                    sw.Write(result);
                    sw.Close();
                    return true;
                }
            }
        }

        public static bool GzipPack()
        {
            string source = "DataGenerated\\farm_" + GameSaveCodeCreator.TheSaveNumber;
            string target = GameSaveCodeCreator.SaveAddress;
            if (File.Exists(target))
                File.Delete(target);
            using (FileStream fsRead = new FileStream(source, FileMode.OpenOrCreate, FileAccess.Read))
            {
                //要将读取到的内容压缩，就是要写入到一个新的文件；所以创建一个新的写入文件的文件流
                using (FileStream fsWrite = new FileStream(target, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    //因为在写入的时候要压缩后写入，所以需要创建压缩流来写入(因此在压缩写入前需要先创建写入流)
                    //压缩的时候就是要将压缩好的数据写入到指定流中，通过fsWrite写入到新的路径下
                    using (GZipStream zip = new GZipStream(fsWrite, CompressionMode.Compress))
                    {
                        //循环读取，每次从fsRead读取一部分，压缩就写入一部分
                        byte[] buffer = new byte[1024 * 1024 * 3];
                        //读取流每次读取buffer数组的大小
                        int r = fsRead.Read(buffer, 0, buffer.Length);
                        while (r > 0)
                        {
                            zip.Write(buffer, 0, r);
                            r = fsRead.Read(buffer, 0, buffer.Length);
                        }
                        zip.Close();
                    }
                    fsWrite.Close();
                }
                fsRead.Close();
            }
            Console.WriteLine("压缩成功");
            return true;
        }

        public static bool TotalLoadFromUserData(string saveaddr, SData root)
        {
            //下面是解压
            GameSaveCodeCreator.UserDataAddress = saveaddr;
            using (FileStream fileStream = new FileStream(saveaddr, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (GZipStream decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    StreamReader reader = new StreamReader(decompressedStream);
                    string result = reader.ReadToEnd();
                    reader.Close();
                    if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\SaveGenerated"))
                        Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\SaveGenerated");
                    StreamWriter sw = new StreamWriter("SaveGenerated\\farms");
                    sw.Write(result);
                    sw.Close();
                }
            }

            //下面是读取
            StreamReader sr = new StreamReader("SaveGenerated\\farms");
            root.sonKind = 0;
            root.Sons = new List<SData>();
            SData p = root;
            p.sonKind = 1;
            List<char> readchrs = new List<char>();
            char nowread = (char)sr.Read();
            char lastread = '0';
            bool realsign = true;//忽视在双引号之间的符号
            bool atend = false;//在结尾时会出现一堆逗号，哪个是不需要退至上层的，而到这个结尾的标志就是
            while (!sr.EndOfStream)
            {
                lastread = nowread;
                nowread = (char)sr.Read();
                if (string.Join("", readchrs.ToArray()) == "ViewedItems")
                {
                    atend = true;
                    //Console.WriteLine(string.Join("", readchrs.ToArray()) + "6666666666666666666");
                }
                if (atend && nowread != ']' && nowread != ':')//对于ViewedItems的特殊处理
                {
                    readchrs.Add(nowread);
                }
                else if (atend && nowread == ']')
                {
                    readchrs.Add(nowread);
                    atend = false;
                }
                else if (nowread == ':' && realsign)
                {
                    p = new SData(p, string.Join("", readchrs.ToArray()));
                    p.Father.Sons.Add(p);
                    //Console.WriteLine(string.Join("", readchrs.ToArray()) + "→");
                    readchrs.Clear();
                }
                else if ((nowread == ',' && realsign && (!atend)) || (nowread == '}' && realsign) || (nowread == ']' && (!atend) && lastread != '[' && realsign))
                {
                    atend = false;
                    if (readchrs.Count > 0)
                    {
                        p.Value = string.Join("", readchrs.ToArray());
                        //Console.WriteLine(string.Join("", readchrs.ToArray()) + "←");
                    }
                    readchrs.Clear();
                    p = p.Father;
                }
                else if (nowread == '[' && realsign)
                {
                    p.sonKind = 2;
                }
                else if (nowread == '{' && realsign)
                {
                    if (p.sonKind == 2)
                    {
                        p = new SData(p, "");
                        //Console.WriteLine(p.Father.Key + "[的{ →");
                        p.Father.Sons.Add(p);
                        p.sonKind = 1;
                    }
                    else
                    {
                        p.sonKind = 1;
                    }
                }
                else if (nowread == '\"')//会出现"144,23"这种影像数据读取的逗号，该逗号实际上属于value而不是分隔符，这些符号由于在""引号中间，通过识别复数个引号（数次反转realsign）来达到忽略这种逗号（或冒号等）的目的
                {
                    realsign = !realsign;
                    readchrs.Add(nowread);
                }
                else
                {
                    readchrs.Add(nowread);
                }

            }
            sr.Close();
            if (p == root && root.Sons.Count() > 0)
                Console.WriteLine("成功读取用户数据！");
            return true;

        }

        public static bool TotalSaveUserData()
        {
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\DataGenerated"))
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\DataGenerated");
            StreamWriter sw = new StreamWriter("DataGenerated\\farms");

            SaveFromSData(GameSaveCodeCreator.FarmInfoRootData, sw);
            sw.Close();
            string source = "DataGenerated\\farms";
            string target = GameSaveCodeCreator.UserDataAddress;
            if (File.Exists(target))
                File.Delete(target);
            using (FileStream fsRead = new FileStream(source, FileMode.OpenOrCreate, FileAccess.Read))
            {
                //要将读取到的内容压缩，就是要写入到一个新的文件；所以创建一个新的写入文件的文件流
                using (FileStream fsWrite = new FileStream(target, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    //因为在写入的时候要压缩后写入，所以需要创建压缩流来写入(因此在压缩写入前需要先创建写入流)
                    //压缩的时候就是要将压缩好的数据写入到指定流中，通过fsWrite写入到新的路径下
                    using (GZipStream zip = new GZipStream(fsWrite, CompressionMode.Compress))
                    {
                        //循环读取，每次从fsRead读取一部分，压缩就写入一部分
                        byte[] buffer = new byte[1024 * 1024 * 3];
                        //读取流每次读取buffer数组的大小
                        int r = fsRead.Read(buffer, 0, buffer.Length);
                        while (r > 0)
                        {
                            zip.Write(buffer, 0, r);
                            r = fsRead.Read(buffer, 0, buffer.Length);
                        }
                        zip.Close();
                    }
                    fsWrite.Close();
                }
                fsRead.Close();
            }
            Console.WriteLine("压缩成功");
            return true;

        }
    }

    
}
