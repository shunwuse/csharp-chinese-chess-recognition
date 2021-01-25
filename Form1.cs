using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chinese_chess_recognition
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)//主程式
        {
            //宣告
            int i, nc, nw, numberOfRings = 5, numberOfAngles = 4, width = 150; //nc = 群組數量, nw = 總字數量, width = 將圖片縮放至寬度, numberOfRings = 圈數, numberOfAngles = 角度切割數
            Bitmap s = new Bitmap("0.原圖.bmp");          //原圖
            Bitmap w = new Bitmap(s.Width, s.Height);     //白圖
            Bitmap br = new Bitmap(s.Width, s.Height);    //灰階圖
            Bitmap th = new Bitmap(br.Width, br.Height);  //二值圖
            Bitmap di = new Bitmap(th.Width, th.Height);  //膨脹圖
            Bitmap er = new Bitmap(th.Width, th.Height);  //侵蝕圖
            Bitmap cl = new Bitmap(di.Width, di.Height);  //閉合圖
            Bitmap op = new Bitmap(er.Width, er.Height);  //段開圖
            Bitmap fc = new Bitmap(th.Width, th.Height);  //為找群
            Bitmap otc = new Bitmap(th.Width, th.Height); //先閉後斷
            Bitmap cto = new Bitmap(th.Width, th.Height); //先斷後閉            
            List<string> savePoint = new List<string>();  //群組記錄點
            List<string> xyPoint = new List<string>();

            //初始化

            //測試區

            Del();
            CreateTxt();
            //製作空白圖
            w = Whitening(w);
            br.Save("1.灰階圖.bmp");
            w.Save("0.白圖.bmp");
            //灰階化            
            br = GrayScale(s);
            br.Save("1.灰階圖.bmp");
        
            //二值化
            th = Thresholding(br);
            th.Save("2.二值化.bmp");

            //膨脹
            di = Dilate(th);
            di.Save("3.膨脹.bmp");

            //侵蝕
            er = Erode(th);
            er.Save("4.侵蝕.bmp");

            //閉合 ===> 膨脹->侵蝕
            cl = Erode(di);
            cl.Save("5.閉合.bmp");

            //段開 ===> 侵蝕->膨脹
            op = Dilate(er);
            op.Save("6.段開.bmp");

            //先閉後段 ===> 膨脹->侵蝕->侵蝕->膨脹
            cto = Dilate(Erode(cl)); 
            cto.Save("7.先閉後斷.bmp");

            //先斷後閉 ===> 侵蝕->膨脹->膨脹->侵蝕
            otc = Erode(Dilate(op));
            otc.Save("8.先斷後閉.bmp");

            //尋找群組
            fc = cto;
            nc = FindComponent(fc, savePoint);

            //尋找每個群組最邊四點
            int[,,] limxy = new int[nc, 4, 2];        //  int[nc, 4, 2] --> nc -  為總群組數量
            limxy = FindMarginalC(fc, savePoint, nc); //                    4  -  0 = 最左, 1 = 最右, 2 = 最上, 3 = 最下
                                                      //                    2  -  0 = X的值, 1 = Y的值
                                                      //尋找圓圈
            List<int> isCircle = FindCircle(fc, limxy, nc);  //可能是圓圈的群組號碼

            //取出字
            nw = GetWord(nc, savePoint, limxy, isCircle); //nw = 符合圓圈條件並在內部有其他群組之個數

            for (i = 0; i < nw; i++)
            {
                //正規化 
                Normalization(i, width);

                //字輪廓
                Contour(i);
            }
            DeleteADirectory("./Feature"); //刪除特徵數據
            for (i = 0; i < nw; i++)
            {
                //獲取圓心x, y、總點數
                Tuple<double, double, int> d = CircleCentera(i);

                //獲取 距離資料、角度資料、最遠距離
                Tuple<double[], double[], double> Info = GetData(i, d.Item1, d.Item2, d.Item3, xyPoint);

                //做圓環
                double[,] line = Ring(numberOfRings, Info.Item1, Info.Item3, i, (int)d.Item1, (int)d.Item2, xyPoint);//圈數, 距離資料, 最遠距離, 總點數, 第n字, 圓心

                //做角度
                double[] feature = Angle(numberOfAngles, numberOfRings, Info.Item2, Info.Item1, line, i, (int)d.Item1, (int)d.Item2); //角度切割數, 角度資料, 群聚資料, 總點數, 第n字, 圓心

                //紀錄特徵                
                Record(feature, numberOfRings, numberOfAngles);

                //比對、判斷
                //Compute(feature, numberOfRings, numberOfAngles, i);
            }
            Close();
        }
        //--副程式--
        public static bool DeleteADirectory(string strPath)//刪除資料夾
        {
            string[] strTemp;
            try
            {
                //刪除目錄中文件
                strTemp = System.IO.Directory.GetFiles(strPath);
                foreach (string str in strTemp)
                {
                    System.IO.File.Delete(str);
                }
                //刪除子目錄(遞迴)
                strTemp = System.IO.Directory.GetDirectories(strPath);
                foreach (string str in strTemp)
                {
                    DeleteADirectory(str);
                }
                //刪除此目錄
                System.IO.Directory.Delete(strPath);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }        
        public void CreateTxt()//創立txt資料夾
        {
            System.IO.Directory.CreateDirectory("./txt");
            // cd.Close();   //切記開了要關,不然會被佔用而無法修改喔!!!
        }
        public void Del()//刪除資料夾
        {
            DeleteADirectory("./Components");
            DeleteADirectory("./Word");
            DeleteADirectory("./Normalization");
            DeleteADirectory("./Contour");
            DeleteADirectory("./Circle");
            DeleteADirectory("./txt");
            DeleteADirectory("./Line");
            DeleteADirectory("./Angle");
        }
        public Bitmap GrayScale(Bitmap s)//灰階化
        {
            int i, j, Gray;
            Bitmap n = new Bitmap(s.Width, s.Height);
            for (i = 0; i < s.Width; i++)
            {
                for (j = 0; j < s.Height; j++)
                {
                    Gray = (int)(0.299 * s.GetPixel(i, j).R + 0.587 * s.GetPixel(i, j).G + 0.114 * s.GetPixel(i, j).B);
                    n.SetPixel(i, j, Color.FromArgb(Gray, Gray, Gray));
                }
            }
            return n;
        }        
        public Bitmap Thresholding(Bitmap s)//二值化
        {
            int i, j, T, tT = 0;
            double size, s1, w1, w2, u1, u2, c1, c2, cw, minC;
            double[] NoG = new double[256];//儲存灰階各值數量            
            Bitmap n = new Bitmap(s.Width, s.Height);

            Array.Clear(NoG, 0, NoG.Length);
            //計算灰階各值數量
            for (i = 0; i < s.Width; i++)
                for (j = 0; j < s.Height; j++)
                    NoG[s.GetPixel(i, j).R]++;
            //OTSU
            size = s.Width * s.Height;
            minC = 0;
            for (T = 0; T < 256; T++)
            {
                s1 = 0;
                for (i = 0; i < T; i++) s1 += NoG[i]; //前景灰階總和
                if (s1 > 0 && s1 < size)
                {
                    w1 = w2 = u1 = u2 = c1 = c2 = 0;
                    w1 = s1 / size; //前景灰階比率
                    w2 = 1 - w1;  //背景灰階比率

                    for (i = 0; i <= T; i++) u1 += ((NoG[i] / size) / w1 * i); //前景平均值
                    for (i = T + 1; i < 256; i++) u2 += ((NoG[i] / size) / w2 * i); //背景平均值

                    for (i = 0; i <= T; i++) c1 += (i - u1) * (i - u1) * (NoG[i] / size) / w1; //前景變異數
                    for (i = T + 1; i < 256; i++) c2 += (i - u2) * (i - u2) * (NoG[i] / size) / w2;// 背景變異數

                    cw = w1 * c1 + w2 * c2;

                    if (minC == 0)
                    {
                        minC = cw;
                        tT = T;
                    }
                    if (cw < minC)
                    {
                        minC = cw;
                        tT = T;
                    }
                }
            }
            Console.WriteLine("閥值：" + tT);
            for (i = 0; i < s.Width; i++)
            {
                for (j = 0; j < s.Height; j++)
                {
                    if (s.GetPixel(i, j).R <= tT) n.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                    else n.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                }
            }
            return n;
        }        
        public Bitmap Whitening(Bitmap s)//白話
        {
            int i, j;
            for (i = 0; i < s.Width; i++)
                for (j = 0; j < s.Height; j++)
                    s.SetPixel(i, j, Color.FromArgb(255, 255, 255));
            return s;
        }        
        public Bitmap Dilate(Bitmap s)//膨脹
        {
            int i, j, k, l;
            Bitmap n = new Bitmap("0.白圖.bmp"); //空白圖

            for (i = 0; i < s.Width; i++)
                for (j = 0; j < s.Height; j++)
                    if (s.GetPixel(i, j).R == 0)
                        for (k = i - 1; k <= i + 1; k++)
                            for (l = j - 1; l <= j + 1; l++)
                                if (k >= 0 && k < s.Width && l >= 0 && l < s.Height)
                                    n.SetPixel(k, l, Color.FromArgb(0, 0, 0));
            return n;
        }        
        public Bitmap Erode(Bitmap s)//侵蝕
        {
            int i, j, k, l, np;
            Bitmap n = new Bitmap("0.白圖.bmp"); //空白圖
            for (i = 0; i < s.Width; i++)
                for (j = 0; j < s.Height; j++)
                {
                    np = 9;
                    for (k = i - 1; k <= i + 1; k++)
                        for (l = j - 1; l <= j + 1; l++)
                            if (k >= 0 && k < s.Width && l >= 0 && l < s.Height)
                                if (s.GetPixel(k, l).R == 255)
                                    --np;
                    if(np == 9) n.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                }
            return n;
        }        
        public int FindComponent(Bitmap s, List<string> sp)//尋找群組
        {
            int i, j, nc = 0, row, col;
            List<string>  point = new List<string>();

            System.IO.Directory.CreateDirectory("./Components");

            for (i = 0; i < s.Width; i++)
            {
                for (j = 0; j < s.Height; j++)
                {
                    if (s.GetPixel(i, j).R == 0)
                    {
                        nc++;
                        Bitmap n = new Bitmap("0.白圖.bmp"); //空白圖
                        s.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                        n.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                        point.Add(nc + ";" + i + ";" + j );
                        sp.Add(nc + ";" + i + ";" + j);
                        while (point.Count != 0)
                        {
                            row = int.Parse((point[0].Split(';')[1]));
                            col = int.Parse((point[0].Split(';')[2]));
                            point.RemoveAt(0);
                            if (row - 1 >= 0)
                                if (s.GetPixel(row - 1, col).R == 0)//左
                                {
                                    s.SetPixel(row - 1, col, Color.FromArgb(255, 255, 255));
                                    n.SetPixel(row - 1, col, Color.FromArgb(0, 0, 0));
                                    point.Add(nc + ";" + (row - 1) + ";" + col);
                                    sp.Add(nc + ";" + (row - 1) + ";" + col);
                                }
                            if (row + 1 < s.Width)
                                if (s.GetPixel(row + 1, col).R == 0)//右
                                {
                                    s.SetPixel(row + 1, col, Color.FromArgb(255, 255, 255));
                                    n.SetPixel(row + 1, col, Color.FromArgb(0, 0, 0));
                                    point.Add(nc + ";" + (row + 1) + ";" + col);
                                    sp.Add(nc + ";" + (row + 1) + ";" + col);
                                }
                            if (col - 1 >= 0)
                                if (s.GetPixel(row, col - 1).R == 0)//上
                                {
                                    s.SetPixel(row, col - 1, Color.FromArgb(255, 255, 255));
                                    n.SetPixel(row, col - 1, Color.FromArgb(0, 0, 0));
                                    point.Add(nc + ";" + row + ";" + (col - 1));
                                    sp.Add(nc + ";" + row + ";" + (col - 1));
                                }
                            if (col + 1 < s.Height)
                                if (s.GetPixel(row, col + 1).R == 0)//下
                                {
                                    s.SetPixel(row, col + 1, Color.FromArgb(255, 255, 255));
                                    n.SetPixel(row, col + 1, Color.FromArgb(0, 0, 0));
                                    point.Add(nc + ";" + row + ";" + (col + 1));
                                    sp.Add(nc + ";" + row + ";" + (col + 1));
                                }
                        }
                        n.Save("./Components/群組：" + nc + ".bmp");
                    }
                }
            }
            return nc;
        }        
        public int[,,] FindMarginalC(Bitmap s, List<string> savePoint, int nc)//尋找每個群組最邊緣
        {
            StreamWriter cd = new StreamWriter("./txt/ComponentData.txt");
            int i, com, x, y;
            int[,,] limxy = new int[nc, 4, 2];
            double[] npc = new double[nc];
            for (i = 0; i < nc; i++) npc[i] = 0;
            for (i = 0; i < savePoint.Count; i++)
            {
                com = int.Parse((savePoint[i].Split(';')[0]));
                x = int.Parse((savePoint[i].Split(';')[1]));
                y = int.Parse((savePoint[i].Split(';')[2]));
                if (npc[com - 1]++ == 0)
                {
                    limxy[com - 1, 0, 0] = limxy[com - 1, 1, 0] = limxy[com - 1, 2, 0] = limxy[com - 1, 3, 0] = x;
                    limxy[com - 1, 0, 1] = limxy[com - 1, 1, 1] = limxy[com - 1, 2, 1] = limxy[com - 1, 3, 1] = y;
                }
                else
                {
                    npc[com - 1]++;
                    if (x < limxy[com - 1, 0, 0])
                    {
                        limxy[com - 1, 0, 0] = x;
                        limxy[com - 1, 0, 1] = y;
                    }
                    if (x > limxy[com - 1, 1, 0])
                    {
                        limxy[com - 1, 1, 0] = x;
                        limxy[com - 1, 1, 1] = y;
                    }
                    if (y < limxy[com - 1, 2, 1])
                    {
                        limxy[com - 1, 2, 0] = x;
                        limxy[com - 1, 2, 1] = y;
                    }
                    if (y > limxy[com - 1, 3, 1])
                    {
                        limxy[com - 1, 3, 0] = x;
                        limxy[com - 1, 3, 1] = y;
                    }
                }
            }
            for (i = 0; i < nc; i++)
            {
                cd.WriteLine("群組" + (i + 1) + "點數量：" + npc[i]);
                cd.WriteLine("最左: x = " + limxy[i, 0, 0] + ", y = " + limxy[i, 0, 1]);
                cd.WriteLine("最右: x = " + limxy[i, 1, 0] + ", y = " + limxy[i, 1, 1]);
                cd.WriteLine("最上: x = " + limxy[i, 2, 0] + ", y = " + limxy[i, 2, 1]);
                cd.WriteLine("最下: x = " + limxy[i, 3, 0] + ", y = " + limxy[i, 3, 1]);
                cd.WriteLine();
            }
            cd.Close();
            return limxy;
        }
        public List<int> FindCircle(Bitmap s, int[,,] xy, int nc)//尋找圓圈
        {
            StreamWriter fcd = new StreamWriter("./txt/FindCircleData.txt");
            
            int i, setd = 230; // setd = 圓心之間距離平方
            double a, b, c;
            double[] nx = new double[4]; // 0 = 垂直左點, 1 = 垂直右點, 2 = 水平上點, 3 = 水平下點
            double[] ny = new double[4];
            double[] d = new double[6];
            List<int> isCircle = new List<int>();
            for (i = 0; i < nc; i++)
            {
                if ((xy[i, 1, 0] - xy[i, 0, 0] > (s.Width / 20)) && (xy[i, 3, 1] - xy[i, 2, 1] > (s.Height / 20)) && (xy[i, 1, 0] - xy[i, 0, 0] < (s.Width / 4)) && (xy[i, 3, 1] - xy[i, 2, 1] < (s.Height / 4)))//小於圖片大小1/4 及 大於1/20 則進行處理
                {
                    
                    if ((xy[i, 2, 0] == xy[i, 3, 0]))
                    {
                        nx[0] = nx[1] = xy[i, 2, 0];
                        ny[0] = xy[i, 0, 1];
                        ny[1] = xy[i, 1, 1];                        
                    }
                    else
                    {
                        a = (xy[i, 3, 1] - xy[i, 2, 1]) / (xy[i, 3, 0] - xy[i, 2, 0]);
                        b = xy[i, 3, 1] - (a * xy[i, 3, 0]);
                        nx[0] = xy[i, 0, 0] - (a * (((a * xy[i, 0, 0]) - xy[i, 0, 1] + b) / ((a * a) + 1)));
                        ny[0] = xy[i, 0, 1] + ((a * xy[i, 0, 0]) - xy[i, 0, 1] + b) / ((a * a) + 1);
                        nx[1] = xy[i, 1, 0] - (a * (((a * xy[i, 1, 0]) - xy[i, 1, 1] + b) / ((a * a) + 1)));
                        ny[1] = xy[i, 1, 1] + ((a * xy[i, 1, 0]) - xy[i, 1, 1] + b) / ((a * a) + 1);
                    }

                    a = (xy[i, 1, 1] - xy[i, 0, 1]) / (xy[i, 1, 0] - xy[i, 0, 0]);
                    b = xy[i, 1, 1] - (a * xy[i, 1, 0]);
                    nx[2] = xy[i, 2, 0] - (a * (((a * xy[i, 2, 0]) - xy[i, 2, 1] + b) / ((a * a) + 1)));
                    ny[2] = xy[i, 2, 1] + ((a * xy[i, 2, 0]) - xy[i, 2, 1] + b) / ((a * a) + 1);
                    nx[3] = xy[i, 3, 0] - (a * (((a * xy[i, 3, 0]) - xy[i, 3, 1] + b) / ((a * a) + 1)));
                    ny[3] = xy[i, 3, 1] + ((a * xy[i, 3, 0]) - xy[i, 3, 1] + b) / ((a * a) + 1);
                    
                    /*
                    //向量法
                    a = (xy[i, 1, 1] - xy[i, 0, 1]);
                    b = (xy[i, 1, 0] - xy[i, 0, 0]);
                    c = -(((xy[i, 1, 1] - xy[i, 0, 1]) * xy[i, 0, 0]) + ((xy[i, 1, 0] - xy[i, 0, 0]) * xy[i, 0, 1]));
                    nx[2] = xy[i, 2, 0] - a * (((a * xy[i, 2, 0]) + (b * xy[i, 2, 1]) + c) / ((a * a) + (b * b)));
                    ny[2] = xy[i, 2, 1] - b * (((a * xy[i, 2, 0]) + (b * xy[i, 2, 1]) + c) / ((a * a) + (b * b)));
                    nx[3] = xy[i, 3, 0] - a * (((a * xy[i, 3, 0]) + (b * xy[i, 3, 1]) + c) / ((a * a) + (b * b)));
                    ny[3] = xy[i, 3, 1] - b * (((a * xy[i, 3, 0]) + (b * xy[i, 3, 1]) + c) / ((a * a) + (b * b)));

                    a = (xy[i, 3, 1] - xy[i, 2, 1]);
                    b = (xy[i, 3, 0] - xy[i, 2, 0]);
                    c = -(((xy[i, 3, 1] - xy[i, 2, 1]) * xy[i, 2, 0]) + ((xy[i, 3, 0] - xy[i, 2, 0]) * xy[i, 2, 1]));
                    nx[0] = xy[i, 0, 0] - a * (((a * xy[i, 0, 0]) + (b * xy[i, 0, 1]) + c) / ((a * a) + (b * b)));
                    ny[0] = xy[i, 0, 1] - b * (((a * xy[i, 0, 0]) + (b * xy[i, 0, 1]) + c) / ((a * a) + (b * b)));
                    nx[1] = xy[i, 1, 0] - a * (((a * xy[i, 1, 0]) + (b * xy[i, 1, 1]) + c) / ((a * a) + (b * b)));
                    ny[1] = xy[i, 1, 1] - b * (((a * xy[i, 0, 0]) + (b * xy[i, 1, 1]) + c) / ((a * a) + (b * b)));
                    */
                    fcd.WriteLine((i + 1) + "圓心：C1= " + (int)nx[0] + ", " + (int)ny[0] + " | C2= " + (int)nx[1] + ", " + (int)ny[1] + " | C3= " + (int)nx[2] + ", " + (int)ny[2] + " | C4= " + (int)nx[3] + ", " + (int)ny[3]);

                    d[0] = ((nx[0] - nx[1]) * (nx[0] - nx[1])) + ((ny[0] - ny[1]) * (ny[0] - ny[1]));
                    d[1] = ((nx[0] - nx[2]) * (nx[0] - nx[2])) + ((ny[0] - ny[2]) * (ny[0] - ny[2]));
                    d[2] = ((nx[0] - nx[3]) * (nx[0] - nx[3])) + ((ny[0] - ny[3]) * (ny[0] - ny[3]));
                    d[3] = ((nx[1] - nx[2]) * (nx[1] - nx[2])) + ((ny[1] - ny[2]) * (ny[1] - ny[3]));
                    d[4] = ((nx[1] - nx[3]) * (nx[1] - nx[3])) + ((ny[1] - ny[3]) * (ny[1] - ny[3]));
                    d[5] = ((nx[2] - nx[3]) * (nx[2] - nx[3])) + ((ny[2] - ny[3]) * (ny[2] - ny[3]));

                    fcd.WriteLine("群組" + (i + 1) + "--六段距離 D1 = " + Math.Round(d[0], 2) + " | D2 = " + Math.Round(d[1], 2) + " | D3 = " + Math.Round(d[2], 2) + " | D4 = " + Math.Round(d[3], 2) + " | D5 = " + Math.Round(d[4], 2) + " | D6 = " + Math.Round(d[5], 2));
                    if (d[0] < setd && d[1] < setd && d[2] < setd && d[3] < setd && d[4] < setd && d[5] < setd) isCircle.Add(i + 1);

                }
                else fcd.WriteLine((i + 1) + "圓心：不符合進入判斷條件");
                fcd.WriteLine();
            }
            fcd.WriteLine("\n符合是圓圈條件的數量為：" + isCircle.Count);
            for (i = 0; i < isCircle.Count; i++) fcd.WriteLine("群組" + isCircle[i] + "符合是圓圈條件");
            fcd.Close();
            return isCircle;
            
        }
        public int GetWord(int nc, List<string> p, int[,,] xy, List<int> c)//取出字
        {
            int i, j, k, n, q = 0, com, x, y, run = 0, col, total = c.Count, s = 0; //i = 圓圈數量, j = 群組數量, k = list總數
            int[,] wxy = new int[c.Count, 4];

            System.IO.Directory.CreateDirectory("./Word");

            for (i = 0; i < c.Count; i++)
            {
                Bitmap w = new Bitmap("0.白圖.bmp"); //空白圖
                n = c[i] - 1;
                wxy[i, 1] = wxy[i, 3] = 0;
                wxy[i, 0] = w.Width;
                wxy[i, 2] = w.Height;
                for (j = 0; j < nc; j++)
                {
                    if ((xy[j, 0, 0] > xy[n, 0, 0]) && (xy[j, 1, 0] < xy[n, 1, 0]) && (xy[j, 2, 1] > xy[n, 2, 1]) && (xy[j, 3, 1] < xy[n, 3, 1])) //尋找符合在圓圈範圍內的群組
                    {
                        if (xy[j, 0, 0] < wxy[i, 0]) wxy[i, 0] = xy[j, 0, 0];
                        if (xy[j, 1, 0] > wxy[i, 1]) wxy[i, 1] = xy[j, 1, 0];
                        if (xy[j, 2, 1] < wxy[i, 2]) wxy[i, 2] = xy[j, 2, 1];
                        if (xy[j, 3, 1] > wxy[i, 3]) wxy[i, 3] = xy[j, 3, 1];

                        q = 0;
                        k = 0;
                        while (q < 2 && k <p.Count)
                        {
                            com = int.Parse((p[k].Split(';')[0])) - 1;
                            if (com == j)
                            {
                                q = 1;
                                x = int.Parse((p[k].Split(';')[1]));
                                y = int.Parse((p[k].Split(';')[2]));
                                w.SetPixel(x, y, Color.FromArgb(0, 0, 0));
                            }
                            else if (q == 1) q = 2;
                            k++;
                        }
                        run = 1;
                    }
                }
                if (run == 1)
                {
                    total--;
                    s++;
                    run = 0;

                    Bitmap ns = new Bitmap((wxy[i, 1] - wxy[i, 0] + 1), (wxy[i, 3] - wxy[i, 2] + 1));
                    for (j = 0; j < ns.Width; j++)
                        for (k = 0; k < ns.Height; k++)
                        {
                            col = w.GetPixel((j + wxy[i, 0]), (k + wxy[i, 2])).R;
                            ns.SetPixel(j, k, Color.FromArgb(col, col, col));
                        }
                    ns.Save("./Word/字" + s + ".bmp");
                }
            }
            return (c.Count - total);
        }
        public void Normalization(int c, int width)//正規化
        {
            int j, k, height;

            System.IO.Directory.CreateDirectory("./Normalization");

            Bitmap s = new Bitmap("./Word/字" + (c + 1) + ".bmp");
            height = s.Height * width / s.Width;
            Bitmap r = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(r);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(s, new Rectangle(0, 0, width, height), new Rectangle(0, 0, s.Width, s.Height), GraphicsUnit.Pixel);

            for (j = 0; j < r.Width; j++)
                for (k = 0; k < r.Height; k++)
                {
                    if(r.GetPixel(j, k).R <128) r.SetPixel(j, k, Color.FromArgb(0, 0, 0));
                    else r.SetPixel(j, k, Color.FromArgb(255, 255, 255));
                }
            r.Save("./Normalization/正字" + (c + 1) + ".bmp");
            
        }
        public void Contour(int c) //字輪廓
        {
            int i, j, col;

            System.IO.Directory.CreateDirectory("./Contour");
            System.IO.Directory.CreateDirectory("./Contour/Whitening");

            Bitmap s = new Bitmap("./Normalization/正字" + (c + 1) + ".bmp");
            Bitmap w = new Bitmap(s.Width + 2, s.Height + 1);
            w = Whitening(w);
            w.Save("./Contour/Whitening/白圖" + (c + 1) + ".bmp");
            Bitmap ld = new Bitmap("./Contour/Whitening/白圖" + (c + 1) + ".bmp");
            Bitmap m = new Bitmap("./Contour/Whitening/白圖" + (c + 1) + ".bmp");
            Bitmap rd = new Bitmap("./Contour/Whitening/白圖" + (c + 1) + ".bmp");
            Bitmap nl = new Bitmap("./Contour/Whitening/白圖" + (c + 1) + ".bmp");
            Bitmap nr = new Bitmap("./Contour/Whitening/白圖" + (c + 1) + ".bmp");
            Bitmap n = new Bitmap("./Contour/Whitening/白圖" + (c + 1) + ".bmp");

            //進行圖片位移
            for (i = 0; i < s.Width; i++)
                for (j = 0; j < s.Height; j++)
                {
                    col = s.GetPixel(i, j).R;
                    ld.SetPixel(i, (j + 1), Color.FromArgb(col, col, col));
                    m.SetPixel((i + 1), j, Color.FromArgb(col, col, col));
                    rd.SetPixel((i + 2), (j + 1), Color.FromArgb(col, col, col));
                }

            for (i = 0; i < m.Width; i++)
                for (j = 0; j < m.Height; j++)
                {
                    if (ld.GetPixel(i, j).R == m.GetPixel(i, j).R) nl.SetPixel(i, j, Color.FromArgb(255, 255, 255)); //原圖--左下 XOR
                    else nl.SetPixel(i, j, Color.FromArgb(0, 0, 0));

                    if (rd.GetPixel(i, j).R == m.GetPixel(i, j).R) nr.SetPixel(i, j, Color.FromArgb(255, 255, 255)); //原圖--右下 XOR
                    else nr.SetPixel(i, j, Color.FromArgb(0, 0, 0));

                    if (nl.GetPixel(i, j).R == 0 || nr.GetPixel(i, j).R == 0) n.SetPixel(i, j, Color.FromArgb(0, 0, 0)); //上面兩張圖做OR
                    else n.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                }
            n.Save("./Contour/字輪" + (c + 1) + ".bmp");
        }
        public Tuple<double, double, int> CircleCentera(int c)//獲取圓心x, y  總點數
        {
            int i, j, k = 0;
            double x = 0, y = 0;
            Bitmap s = new Bitmap("./Contour/字輪" + (c + 1) + ".bmp");
            //Bitmap s = new Bitmap("./ZengWen/字輪" + (c + 1) + ".bmp"); //正文輪廓圖

            for (i = 0; i < s.Width; i++) //累加所有x, y值 除以總點數
                for (j = 0; j < s.Height; j++)
                    if (s.GetPixel(i, j).R == 0)
                    {
                        k++;
                        x += i;
                        y += j;
                    }
            x = x / k;
            y = y / k;
            using (StreamWriter wd = new StreamWriter("./txt/WordData-" + (c + 1) + ".txt"))
            {
                wd.WriteLine("字(double)" + (c + 1) + "： 圓心 (" + x + ", " + y + ")  總點數：" + k);
            }
                
            return new Tuple<double, double, int>(x, y, k); //圓心座標(x, y) K = 總點數
        }
        public Tuple<double[], double[], double> GetData(int c, double x, double y, int t, List<String> xyp)//獲取點至圓心 距離、角度
        {
            int i, j, k = 0, fx = 0, fy = 0 ;
            double fd = 0;
            Bitmap s = new Bitmap("./Contour/字輪" + (c + 1) + ".bmp");
            //Bitmap s = new Bitmap("./ZengWen/字輪" + (c + 1) + ".bmp"); //正文輪廓圖
            double[] dis = new double[t];
            double[] ang = new double[t];
            System.IO.Directory.CreateDirectory("./Line");
            StreamWriter wd = new StreamWriter("./txt/Ang-" + (c + 1) + ".txt"); //創立txt(覆蓋原有)
            for (i = 0; i < s.Width; i++)
                for (j = 0; j < s.Height; j++)
                    if (s.GetPixel(i, j).R == 0)
                    {
                        xyp.Add(i + ":" + j);
                        dis[k] = Math.Sqrt(((i - x) * (i - x)) + ((j - y) * (j - y)));
                        ang[k] = Math.Asin((double)(y - j) / dis[k]) * 180.0 / Math.PI;
                        if ((y - j) >= 0)
                        {
                            if ((i - x) < 0)
                            {
                                ang[k] = 180 - ang[k];
                                s.SetPixel(i, j, Color.FromArgb(255, 0, 0));
                            }
                        }
                        else
                        {
                            s.SetPixel(i, j, Color.FromArgb(0, 255, 0));
                            if ((i - x) < 0)
                            {
                                ang[k] = -180 - ang[k];
                                s.SetPixel(i, j, Color.FromArgb(0, 0, 255));
                            }
                            ang[k] += 360;
                        }
                        wd.WriteLine((k + 1) + ":" + ang[k] + "(" + i +", " + j + ")");
                        if (dis[k] > fd)
                        {
                            fd = dis[k];
                            fx = i;
                            fy = j;
                        }
                        k++;
                    }
            Graphics g = Graphics.FromImage(s);
            g.DrawLine(new Pen(Color.Pink, 2), (int)x, (int)y, fx, fy);
            s.Save("./Line/線" + (c + 1) + ".bmp");
            wd.Close();
            return new Tuple<double[], double[], double>(dis, ang, fd);
        }
        public string DoubleToString4(double value)//取消數後4位
        {
            return Convert.ToDouble(value).ToString("0.0000");
        }
        public string DoubleToString3(double value)//取消數後3位
        {
            return Convert.ToDouble(value).ToString("0.000");
        }
        public double[,] Ring(int nr, double[] d, double f, int c, int x, int y, List<String> xyp) //圈數, 距離資料, 最遠距離, 第n字, 圓心, 座標
        {
            int i, j, k = 0, px, py;
            double spacing, temp = 0, judge = 1, ojudge; //spaing = 邊界, temp = 占有比例佔存, judge = 疊代條件, ojudge = 前次疊代條件
            double[] a = new double[nr]; //點數分配值
            double[] b = new double[nr]; //群聚中心
            double[] r = new double[nr];
            double[] l = new double[nr];
            double[] bp = new double[nr];
            double[] nbp = new double[nr];
            double[] or = new double[nr];
            double[] ol = new double[nr];
            double[] obp = new double[nr];
            double[] onbp = new double[nr];
            double[] np = new double[nr]; //點數平均值
            double[] onp = new double[nr]; //前次的點數平均值
            double[] ob = new double[nr]; //前次的群聚中心
            double[,] line = new double[nr + 1, 3]; //邊界
            string cc = null, pt = null, rpt = null, lpt = null, nbpt = null, bpt = null, tow = null; //紀錄字串
            Pen linePen = new Pen(Color.Red, 3); //畫邊界用筆
            Brush circleBush = new SolidBrush(Color.Red); //畫圓心用筆刷
            
            FileStream fs = new FileStream("./txt/WordData-" + (c + 1) + ".txt", FileMode.Append);
            StreamWriter wd = new StreamWriter(fs); //創立txt(覆蓋原有)
            wd.WriteLine();

            Array.Clear(ob, 0, ob.Length); //陣列歸零

            System.IO.Directory.CreateDirectory("./Circle");
            System.IO.Directory.CreateDirectory("./Circle/字" + (c + 1));

            spacing = f / nr / 20; //模糊區域大小
            for(i = 0; i < 3; i++) //中心及最邊緣 初始化
            {
                line[0, i] = 0; //圓心點設定為0
                line[nr, i] = f; //最外圍設為最遠
            }

            for (i = 1; i < nr; i++) //設立邊界值
            {
                line[i, 0] = f * i / nr;
                line[i, 1] = line[i, 0] - (spacing / 2);
                line[i, 2] = line[i, 0] + (spacing / 2);
            }
            i = 0;
            while (k++ < 50 && judge > 0.01) //進入疊代
            {
                wd.WriteLine("第" + (k - 1) + "次-結果");
                Bitmap s = new Bitmap("./Contour/字輪" + (c + 1) + ".bmp");
                Graphics g = Graphics.FromImage(s); //令圖片能做編輯
                g.FillEllipse(circleBush, x - 1, y - 1, 2, 2); //塗圓心
                for (i = 1; i <= nr; i++) //畫圈
                {
                    g.DrawEllipse(linePen, (x - (int)line[i, 0]), (y - (int)line[i, 0]), ((float)line[i, 0] * 2), ((float)line[i, 0] * 2));
                    wd.Write("輪" + i + " = " + DoubleToString4(line[i, 0]) + " | ");
                }
                wd.WriteLine();

                for (i = 0; i < nr; i++) //群中心線
                {
                    g.DrawEllipse(new Pen(Color.Cyan, 1), (x - (int)((line[i, 0] + line[i + 1, 0]) / 2)), (y - (int)((line[i, 0] + line[i + 1, 0]) / 2)), ((float)((line[i, 0] + line[i + 1, 0]) / 2) * 2), ((float)((line[i, 0] + line[i + 1, 0]) / 2) * 2));
                    g.DrawEllipse(new Pen(Color.Green, 1), (x - (int)ob[i]), (y - (int)ob[i]), ((float)ob[i] * 2), ((float)ob[i] * 2));
                }
                s.Save("./Circle/字" + (c + 1) + "/字" + (c + 1) + "輪" + (k - 1) + ".bmp");
                for (i = 0; i < d.Length; i++)
                {
                    for (j = 0; j < nr; j++) //判斷一般區域
                        if (d[i] >= line[j, 2] && d[i] <= line[(j + 1), 1])
                        {
                            np[j]++;
                            a[j] += d[i];
                            nbp[j]++;
                        }
                    for (j = 1; j < nr; j++) //判斷模糊區域
                    {
                        if (d[i] > line[j, 1] && d[i] < line[j, 2]) 
                        {
                            temp = (d[i] - line[j, 1]) / spacing;
                            np[j - 1] += (1 - temp);
                            a[j - 1] += d[i] * (1 - temp);
                            np[j] += temp;
                            a[j] += d[i] * temp;
                            bp[j - 1]++;
                        }
                    }
                    for (j = 0; j < nr; j++) //計算每群內外之點數個數
                    {
                        if (d[i] > line[j, 2] && d[i] < (line[j, 0] + line[j + 1, 0]) / 2)
                            l[j]++;
                        else if (d[i] > (line[j, 0] + line[j + 1, 0]) / 2 && d[i] < line[j + 1, 1])
                            r[j]++;
                    }
                }
                ojudge = judge;
                judge = 0;
                bpt = nbpt = lpt = rpt = pt = cc = null;
                for (i = 0; i < nr; i++) 
                {
                    if (np[i] != 0)
                    {
                        b[i] = a[i] / np[i]; //計算群聚中心
                    }                    
                    cc += ("群聚中心" + (i + 1) + " = " + DoubleToString4(ob[i]) + "  ");
                    pt += ("環" + (i + 1) + "點個數 = " + DoubleToString4(onp[i]) + "  ");
                    nbpt += ("環" + (i + 1) + "(非模糊)點個數 = " + onbp[i] + "  ");
                    if(i < (nr-1))
                        bpt += ("環" + (i + 1) + "-" + (i + 2) + "(模糊)點個數 = " + obp[i] + "  ");
                    lpt += ("環" + (i + 1) + "(內)點個數 = " + ol[i] + "  ");
                    rpt += ("環" + (i + 1) + "(外)點個數 = " + or[i] + "  ");
                    judge += Math.Abs(b[i] - ob[i]);
                    ob[i] = b[i];
                    onp[i] = np[i];
                    or[i] = r[i];
                    ol[i] = l[i];
                    onbp[i] =nbp[i];
                    obp[i] = bp[i];
                    nbp[i] = bp[i] = r[i] = l[i] = a[i] = np[i] = 0;
                }
                    
                if (k == 1)
                {
                    cc = null;                    
                    for(i = 0; i < nr; i++) 
                    {
                        temp = (line[i, 0] + line[i + 1, 0]) / 2;
                        cc += ("群聚中心" + (i + 1) + " = " + DoubleToString4(temp) + "  ");                        
                    }
                }
                if(k != 0)
                {
                    if( k > 1)
                    {
                        wd.WriteLine(pt);
                        wd.WriteLine(nbpt);
                        wd.WriteLine(bpt);
                        wd.WriteLine(lpt);
                        wd.WriteLine(rpt);
                    }
                    wd.WriteLine(cc);
                }
                if (k > 2) wd.WriteLine("與前次總群聚相差為 " + DoubleToString4(ojudge));
                wd.WriteLine();
                for (i = 1; i < nr; i++) //計算新邊界
                {
                    line[i, 0] = (b[i - 1] + b[i]) / 2;
                    line[i, 1] = line[i, 0] - (spacing / 2);
                    line[i, 2] = line[i, 0] + (spacing / 2);
                }
            }
            Bitmap ls = new Bitmap("./Contour/字輪" + (c + 1) + ".bmp");
            Graphics lg = Graphics.FromImage(ls);
            lg.FillEllipse(circleBush, x - 3, y - 3, 6, 6);
            wd.WriteLine();
            wd.WriteLine("最後結果--疊代第" + (k - 1)+ "次");
            cc = null;
            for (i = 0; i < nr; i++)
            {
                cc += ("群聚中心" + (i + 1) + " = " + DoubleToString4(b[i]) + "  ");
                
            }
            
            for (i = 1; i <= nr; i++)
            {
                lg.DrawEllipse(linePen, (x - (int)line[i, 0]), (y - (int)line[i, 0]), ((float)line[i, 0] * 2), ((float)line[i, 0] * 2));
                wd.Write("輪" + i + " = " + DoubleToString4(line[i, 0]) + " | ");
            }
            wd.WriteLine();
            wd.WriteLine(cc);
            wd.WriteLine("與前次總群聚相差為 " + DoubleToString4(judge));
            ls.Save("./Circle/字" + (c + 1) + "/字" + (c + 1) + "輪" + (k - 1) + "-Final.bmp");
            
            wd.Close();
            return line;
        }
        public double AngleCorrection(double value)//角度修正
        {
            if (value < 0)
                return AngleCorrection(value += 360);
            else
                return (value % 360);
        }
        public double[] Angle(int na, int nr, double[] ang, double[] dis, double[,] line, int c, int x, int y) //角度切割數, 圈數, 角度資料, 距離資料, 群聚資料, 第n字, 圓心
        {
            int i, j, k, n, index;
            int[] fa = new int[nr];
            double[] x1 = new double[na];
            double[] y1 = new double[na];
            double[] x2 = new double[na];
            double[] y2 = new double[na];
            double[] zx1 = new double[na];
            double[] zy1 = new double[na];
            double[] zx2 = new double[na];
            double[] zy2 = new double[na];
            double spacing, nv, temp, judge = 1, ojudge, fz, t, minSide, minValue;
            double[] a = new double[na];
            double[] oa = new double[na];
            double[] b = new double[na];
            double[] ob = new double[na];
            double[] np = new double[na];
            double[] onp = new double[na];
            double[] m = new double[na];
            double[] fm = new double[na];
            double[] fmid = new double[na];
            double[] feature = new double[(nr * na)];
            double[,] side = new double[na + 1, 3];
            double[,] result = new double[nr, na];
            string cc = null, pt = null, ca = null, scc = null; //紀錄字串
            List<double> pir = new List<double>();
            Array.Clear(fa, 0, fa.Length);
            spacing =(double) 360 / na / 20;

            System.IO.Directory.CreateDirectory("./Angle");
            System.IO.Directory.CreateDirectory("./Angle/Original");
            Bitmap s = new Bitmap("./Contour/字輪" + (c + 1) + ".bmp");
            Graphics g = Graphics.FromImage(s); //令圖片能做編輯
            g.FillEllipse(new SolidBrush(Color.Red), x - 1, y - 1, 2, 2); //塗圓心
            for (i = 1; i <=nr; i++)
                g.DrawEllipse(new Pen(Color.Red, 1), (x - (int)line[i, 0]), (y - (int)line[i, 0]), ((float)line[i, 0] * 2), ((float)line[i, 0] * 2));
            s.Save("./Angle/Original/字" + (c + 1) + "輪.bmp");

            
            for (i = 0; i < 3; i++)
                side[0, i] = 0;

            for (i = 0; i < nr; i++)
            {
                judge = 1;
                Array.Clear(ob, 0, ob.Length); //陣列歸零
                pir.Clear(); //清除環點數List

                System.IO.Directory.CreateDirectory("./Angle/字" + (c + 1) + "環" + (i + 1));

                StreamWriter wd = new StreamWriter("./txt/WordAngle-" + (c + 1) + "_" + (i + 1) + ".txt"); //創立txt(覆蓋原有)
                StreamWriter swd = new StreamWriter("./txt/simplifyWordAngle-" + (c + 1) + "_" + (i + 1) + ".txt"); //創立txt(覆蓋原有)

                for (j = 0; j < dis.Length; j++) //尋找符合環內點數
                    if (dis[j] <= line[i + 1, 0])
                    {
                        pir.Add(ang[j]);
                        dis[j] = line[nr, 0] + 1;
                    }
                wd.WriteLine("總點數：" + pir.Count);

                for (j = 0; j < na; j++) //預設群聚中心
                    b[j] = (360 * j) / na;
                for (j = 1; j <= na; j++) //預設邊界
                {
                    side[j, 0] = (b[j - 1] + b[j % na]) / 2;
                    if (j == na) side[j, 0] += 180;
                    side[j, 1] = side[j, 0] - (spacing / 2);
                    side[j, 2] = side[j, 0] + (spacing / 2);
                    fmid[j - 1] = side[j, 0];
                }
                k = 0;
                while (k++ < 50 && judge > 0.01) //進入疊代
                {
                    Bitmap fs = new Bitmap("./Angle/Original/字" + (c + 1) + "輪.bmp");
                    Graphics fg = Graphics.FromImage(fs); //令圖片能做編輯
                    for (j = 1; j <= na; j++)
                        for (n = 0; n < 3; n++)
                            side[j, n] = AngleCorrection(side[j, n]);

                    Array.Clear(m, 0, m.Length);
                    Array.Clear(fm, 0, fm.Length);

                    for (j = 0; j < 3; j++)
                        side[0, j] = side[na, j];

                    for (n = 0; n < na; n++)//判斷一般區域是否越界
                    {
                        if (side[n + 1, 1] < side[n, 2])
                            fm[n] = 1;
                    }
                    minSide = 360;
                    for (n = 1; n <= na; n++)//判斷模糊區域是否越界
                    {
                        if (side[n, 1] < minSide)
                            minSide = side[n, 1];
                        if (side[n, 2] < side[n, 1])
                        {
                            m[n - 1] = 1;
                            minSide = 0;
                        }                        
                    }

                    for (j = 0; j < 3; j++)
                        side[0, j] = 0;


                    for (j = 1; j <= na; j++) //計算畫邊界線座標
                    {
                        x1[j - 1] = x + line[i, 0] * Math.Cos(side[j, 0] * Math.PI / 180);
                        y1[j - 1] = y - line[i, 0] * Math.Sin(side[j, 0] * Math.PI / 180);
                        x2[j - 1] = x + line[i + 1, 0] * Math.Cos(side[j, 0] * Math.PI / 180);
                        y2[j - 1] = y - line[i + 1, 0] * Math.Sin(side[j, 0] * Math.PI / 180);
                    }

                    for (j = 0; j < na; j++) //計算畫中心線座標
                    {
                        temp = (fmid[j] + fmid[(j + 1) % na]) / 2;
                        if (fmid[(j + 1) % na] < fmid[j])
                            temp += 180;
                        zx1[j] = x + line[i, 0] * Math.Cos(temp * Math.PI / 180);
                        zy1[j] = y - line[i, 0] * Math.Sin(temp * Math.PI / 180);
                        zx2[j] = x + line[i + 1, 0] * Math.Cos(temp * Math.PI / 180);
                        zy2[j] = y - line[i + 1, 0] * Math.Sin(temp * Math.PI / 180);
                    }

                    wd.WriteLine("第" + (k - 1) + "次-結果");

                    for (j = 0; j < na; j++)
                    {
                        fg.DrawLine(new Pen(Color.Blue, 1), (float)x1[j], (float)y1[j], (float)x2[j], (float)y2[j]); //畫邊界線
                        fg.DrawLine(new Pen(Color.Cyan, 1), (float)zx1[j], (float)zy1[j], (float)zx2[j], (float)zy2[j]); //畫中心線線
                        wd.Write("角" + (j + 1) + " = " + Convert.ToDouble(side[j + 1, 0]).ToString("000.0000") + "度 | ");
                    }
                    wd.WriteLine();
                    fs.Save("./Angle/字" + (c + 1) + "環" + (i + 1) + "/字" + (c + 1) + "輪" + (i + 1) + "角" + (k - 1) + ".bmp");

                    fz = -side[na, 2]; //取得歸零修正值
                    for (j = 1; j <= na; j++)
                        for (n = 0; n < 3; n++)
                            side[j, n] = AngleCorrection(side[j, n] + fz);
                    for (j = 0; j < 3; j++)
                        if (side[na, j] == 0) side[na, j] = 360;

                    minSide = AngleCorrection(minSide + fz);
                    for (j = 0; j < pir.Count; j++)
                    {
                        nv = AngleCorrection(pir[j] + fz);

                        for (n = 0; n < na; n++)//判斷一般區域
                        {
                            if (nv >= side[n, 2] && nv <= side[n + 1, 1])
                            {
                                np[n]++;
                                a[n] += pir[j];
                                t = AngleCorrection(side[n, 2] - fz);
                                if (fm[n] == 1 && pir[j] < t) a[n] += 360;
                            }
                        }
                        for (n = 1; n <= na; n++)//判斷模糊區域
                        {
                            if (nv > side[n, 1] && nv < side[n, 2])
                            {
                                temp = (nv - side[n, 1]) / spacing;
                                np[n - 1] += (1 - temp);
                                np[n % na] += temp;
                                if (m[n - 1] == 1)
                                {
                                    t = AngleCorrection(side[n, 1] - fz);
                                    if (pir[j] < t)
                                    {
                                        a[n - 1] += (pir[j] + 360) * (1 - temp);
                                        a[n % na] += pir[j] * temp;
                                    }
                                    else
                                    {
                                        a[n - 1] += pir[j] * (1 - temp);
                                        a[n % na] += (pir[j] - 360) * temp;
                                    }
                                }
                                else if (side[n, 1] == minSide)
                                {
                                    a[n - 1] += (pir[j] + 360) * (1 - temp);
                                    a[n % na] += pir[j] * temp;
                                }
                                else
                                {
                                    a[n - 1] += pir[j] * (1 - temp);
                                    a[n % na] += pir[j] * temp;
                                }
                            }
                        }
                    }
                    ojudge = judge;
                    judge = 0;
                    for (j = 0; j < na; j++)
                    {
                        if (np[j] != 0)
                        {
                            b[j] = a[j] / np[j]; //計算群聚中心
                        }

                        ca += ("角總角度" + (j + 1) + " = " + DoubleToString4(oa[j]) + "  ");
                        cc += ("角群聚中心" + (j + 1) + " = " + DoubleToString4(AngleCorrection(ob[j])) + "  ");
                        if (scc != null) scc += " ";
                        scc += (DoubleToString4(AngleCorrection(ob[j])));
                        pt += ("角" + (j + 1) + "點個數" + DoubleToString4(onp[j]) + "  ");
                        judge += Math.Abs(b[j] - ob[j]);
                        oa[j] = a[j];
                        ob[j] = b[j];
                        onp[j] = np[j];
                        a[j] = np[j] = 0;
                    }

                    if (k == 1)
                    {
                        scc = cc = null;
                        for (j = 0; j < na; j++)
                        {
                            temp = (fmid[(j + na - 1) % na] + fmid[j]) / 2;
                            if (fmid[j] < fmid[(j + na - 1) % na])
                                temp += 180;
                            cc += ("群聚中心" + (j + 1) + " = " + DoubleToString4(AngleCorrection(temp)) + "  ");
                            if (scc != null) scc += " ";
                            scc += (DoubleToString4(AngleCorrection(temp)));
                        }
                    }
                    if (k != 0)
                    {
                        if (k != 1)
                        {
                            wd.WriteLine(ca);
                            wd.WriteLine(pt);
                        }
                        wd.WriteLine(cc);
                        swd.WriteLine(scc);
                    }
                    if (k > 2) wd.WriteLine("與前次總群聚相差為 " + DoubleToString4(ojudge));
                    ca = pt = cc = scc = null;
                    for (j = 1; j <= na; j++) //計算新邊界
                    {
                        side[j, 0] = (b[j - 1] + b[j % na]) / 2;
                        if (b[j % na] < b[j - 1])
                            side[j, 0] += 180;
                        side[j, 1] = side[j, 0] - (spacing / 2);
                        side[j, 2] = side[j, 0] + (spacing / 2);
                        fmid[j - 1] = side[j, 0];
                    }
                    wd.WriteLine();
                }

                wd.WriteLine();
                wd.WriteLine("最後結果--疊代第" + (k - 1) + "次");
                Bitmap ffs = new Bitmap("./Angle/Original/字" + (c + 1) + "輪.bmp");
                Graphics ffg = Graphics.FromImage(ffs); //令圖片能做編輯
                for (j = 1; j <= na; j++)
                    for (n = 0; n < 3; n++)
                        side[j, n] = AngleCorrection(side[j, n]);
                for (j = 1; j <= na; j++) //計算畫邊界線座標
                {
                    x1[j - 1] = x + line[i, 0] * Math.Cos(side[j, 0] * Math.PI / 180);
                    y1[j - 1] = y - line[i, 0] * Math.Sin(side[j, 0] * Math.PI / 180);
                    x2[j - 1] = x + line[i + 1, 0] * Math.Cos(side[j, 0] * Math.PI / 180);
                    y2[j - 1] = y - line[i + 1, 0] * Math.Sin(side[j, 0] * Math.PI / 180);
                }
                for (j = 0; j < na; j++)
                {
                    ffg.DrawLine(new Pen(Color.Blue, 1), (float)x1[j], (float)y1[j], (float)x2[j], (float)y2[j]); //畫邊界線
                    wd.Write("角" + (j + 1) + " = " + Convert.ToDouble(side[j + 1, 0]).ToString("000.0000") + "度 | ");
                }
                wd.WriteLine();

                scc = cc = null;
                for (j = 0; j < na; j++)
                {
                    cc += ("角群聚中心" + (j + 1) + " = " + DoubleToString4(AngleCorrection(b[j])) + "  ");
                    if (scc != null) scc += " ";
                    scc += (DoubleToString4(AngleCorrection(b[j])));
                }
                wd.WriteLine(cc);
                swd.WriteLine(scc);
                wd.WriteLine("與前次總群聚相差為 " + DoubleToString4(judge));
                ffs.Save("./Angle/字" + (c + 1) + "環" + (i + 1) + "/字" + (c + 1) + "輪" + (i + 1) + "角" + (k - 1) + "-Final.bmp");
                wd.Close();
                swd.Close();

                for (j = 1; j <= na; j++)
                    side[j, 0] = AngleCorrection(side[j, 0]);
                fz = -side[na, 0]; //取得歸零修正值
                for (j = 1; j <= na; j++)
                    side[j, 0] = AngleCorrection(side[j, 0] + fz);
                if (side[na, 0] == 0) side[na, 0] = 360;

                for (j = 0; j < na; j++)
                    np[j] = side[j + 1, 0] - side[j, 0];
                index = 0;
                minValue = np[0];
                for (j = 0; j < na; j++)
                    if (np[j] < minValue)
                    {
                        index = j;
                        minValue = np[j];
                    }
                for (j = 0; j < na; j++)
                    feature[(i * na) + j] = np[(j + index) % na];
                Array.Clear(np, 0, np.Length);
            }
            return feature;
        }
        public void Record(double[] f, int nr, int na)//紀錄特徵
        {
            
            System.IO.Directory.CreateDirectory("./Feature");
            FileStream fs = new FileStream("./Feature/Feature.txt", FileMode.Append);
            StreamWriter wd = new StreamWriter(fs); //創立txt(覆蓋原有)

            int i;
            string str = null;

            for (i = 0; i < (nr * na); i++)
            {
                if (i > 0)
                    str += ";";
                str += f[i];
            }

            wd.WriteLine(str);
            wd.Close();
        }
        public void Compute(double[] f, int nr, int na, int c)//對照特徵
        {
            int i, index;    
            double sum, minValue;
            string str;
            List<double> data = new List<double>();
            string[] words = new string[] { "帥", "仕", "相", "人車", "炮", "兵", "將", "士", "象", "車", "馬", "卒"};

            System.IO.StreamReader file = new System.IO.StreamReader("./Feature/Feature.txt");
            while ((str = file.ReadLine()) != null)
            {
                sum = 0;
                for (i = 0; i < (nr * na); i++)
                {
                    sum += Math.Abs(f[i] - double.Parse((str.Split(';')[i])));
                }
                data.Add(sum);
            }
            file.Close();

            minValue = data[0];
            index = 0;
            for (i = 1; i < data.Count; i++)
            {
                if (data[i] < minValue)
                {
                    minValue = data[i];
                    index = i;
                }
            }

            Console.WriteLine("字" + (c + 1) + "可能是" + " " + words[index]);
        }
    }
}
