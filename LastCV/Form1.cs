﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using System.Drawing;
using System.Windows.Forms;

namespace LastCV
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            CaptureCamera();
        }
        private Thread _cameraThread;
        public static int thre1,thre2;
        public static int cannyThre1, cannyThre2;
        public static int[,] Status=new int[3,3];

        private void CaptureCamera()
        {
            thre1 = threshold1.Value;
            thre2 = threshold2.Value;
            cannyThre1 = cannyBar1.Value;
            cannyThre2 = cannyBar2.Value;
            _cameraThread = new Thread(new ThreadStart(CaptureCameraCallback));

            _cameraThread.Start();
        }
    
        private void CaptureCameraCallback()
        {
            using (CvCapture cap = CvCapture.FromCamera(CaptureDevice.Any, -1))
            {
                while (CvWindow.WaitKey(10) < 0)
                {
                    int MARGINW, MARGINH;
                    int[] HORI = new int[2];
                    int[] VERTI = new int[2];

                    for (int i = 0; i < 3; i++)
                        for (int j = 0; j < 3; j++)
                            Status[i, j] = 0;
                   // IplImage converted = new IplImage(cap.QueryFrame().Size, BitDepth.U8, 1);
                    IplImage mainImage = cap.QueryFrame();
                    //IplImage mainImage = new IplImage("test.png");
                    IplImage gray = new IplImage(mainImage.Size, BitDepth.U8, 1);
                    int WIDTH = (cap.QueryFrame().Width);
                    int HEIGHT = (cap.QueryFrame().Height);
                    MARGINW = WIDTH / 3;
                    MARGINH = HEIGHT / 3;
                    HORI[0] = MARGINW;
                    HORI[1] = 2 * HORI[0];
                    VERTI[0] = MARGINH;
                    VERTI[1] = 2 * VERTI[0];
                  
                    try
                    {  
                        #region BLOCK_DETECTION

                        mainImage.Smooth(mainImage, SmoothType.Blur, 5, 5);
                        mainImage.CvtColor(gray, ColorConversion.BgrToGray);
                       
                        
                        System.Diagnostics.Debug.WriteLine("" + cannyThre1);
                        Cv.Canny(gray, gray, 35, 35, ApertureSize.Size3);
                        gray.Smooth(gray, SmoothType.BlurNoScale, 3,3);
                        CvSeq<CvPoint> contours;
                        CvMemStorage _storage = new CvMemStorage();
                        Cv.FindContours(gray, _storage, out contours, CvContour.SizeOf, ContourRetrieval.Tree, ContourChain.ApproxSimple);
                       // Cv.DrawContours(mainImage, contours, CvColor.Blue, CvColor.Green, 1,1, LineType.Link8);
                        //CvLineSegmentPolar[] lines= gray.HoughLinesStandard(1, Cv.PI / 180, 50, 0, 0);
                        CvMemStorage storage = new CvMemStorage();
                        storage.Clear();

#if DEBUG
                        setupDebug(mainImage);
#endif
                        // gray.HoughLines2(storage, HoughLinesMethod.Standard, Cv.PI / 180, 0, 0);
                        CvSeq lines = gray.HoughLines2(storage, HoughLinesMethod.Probabilistic, 1, Math.PI / 180, 50,100,1);
                        for (int i = 0; i < lines.Total; i++)
                        {
                            CvLineSegmentPoint elem = lines.GetSeqElem<CvLineSegmentPoint>(i).Value;
                            mainImage.Line(elem.P1, elem.P2, CvColor.Navy, 2);
                            try
                            {
                                if (elem.P1.Y <= HEIGHT -10 && elem.P2.Y <= HEIGHT -10)
                                {
                                    int i1=elem.P1.X / (WIDTH / 3);
                                    int j1=elem.P1.Y / (HEIGHT / 3);
                                    Status[i1, j1]++;
                                    int i2 = elem.P2.X / (WIDTH / 3);
                                    int j2 = elem.P2.Y / (HEIGHT / 3);
                                    Status[i2, j2]++;
                                    if (elem.P1.X != elem.P2.X)
                                    {
                                        double slope = 1.0d*(elem.P2.Y - elem.P1.Y) / (elem.P2.X - elem.P1.X);
                                        double c = elem.P1.Y - slope * elem.P1.X;
                                        List<CvPoint> points = new List<CvPoint>(Math.Abs(i1 - i2) + Math.Abs(j1 - j2)+2);
                                       

                                        for (int p = Math.Min(i1, i2); p !=Math.Max(i1,i2); p++)
                                            points.Add(new CvPoint(HORI[p], (int)(slope * HORI[p]+ c)));
                                        for (int p = Math.Min(j1, j2); p != Math.Max(j1, j2); p++)
                                            points.Add(new CvPoint((int) ((VERTI[p] - c)/slope),VERTI[p] ));

                                        points.Add(elem.P1);
                                        points.Add(elem.P2);
                                      
                                        points.Sort((a, b) =>
                                        {
                                            int result = a.X.CompareTo(b.X);
                                            if (result == 0) result = a.Y.CompareTo(b.Y);
                                            return result;
                                        });

                                        for (int p = 0; p < points.Capacity - 1; p++)
                                        {
                                            CvPoint mid = new CvPoint((points[p].X + points[p + 1].X) / 2,
                                                                     (points[p].Y + points[p + 1].Y) / 2);
                                          
                                            Status[(int)(mid.X/HORI[0]),(int)(mid.Y/VERTI[0])]++;
                                        }
                                        
                                        // CvPoint point = new CvPoint((2*WIDTH / 3),(int)( slope * (WIDTH / 1.5) + c));
                                        //mainImage.Line(point, point, CvColor.Yellow, 5);
                                    
                                    }
                                  
                                }
                            }
                            catch (IndexOutOfRangeException e)
                            {
                            }

                        }
                    #endregion
#if DEBUG
                        for(int i=0; i<3;i++)
                            for(int j=0;j<3;j++)
                            {
                                if (Status[i, j] > 0)
                                    Cv.Rectangle(mainImage, new CvRect(i * HORI[0], j * VERTI[0], HORI[0], VERTI[0]),
                                        new CvScalar(255, 255, 0, 0),10);
                                

                            }
#endif 

                        int leftD = 0, topD = 0, bottomD = 0, rightD = 0;
                        if (Status[0, 1] > 0) leftD = 3; if (Status[1, 0] > 0) topD = 4; if (Status[1, 2] > 1) bottomD = 5; if (Status[2, 1] > 0) rightD = 6;
                        int sum = leftD + topD + bottomD + rightD;
                        int DIRECTION = 0;
                        if (sum == 18)
                        {
                            DIRECTION = 0;
                        }
                        else if (sum > 11)
                        {
                            DIRECTION = sum - 9;
                        }
                        else if (sum == 9)
                        {
                            DIRECTION = 111;
                        }
                        else if (sum > 6)
                        {
                            if (sum > 9) DIRECTION = 6;
                            else DIRECTION = 3;
                        }
                        else if (sum > 0)
                        {
                            if (Status[1, 1] > 0)
                                DIRECTION = -111;
                            else DIRECTION = sum;
                        }
                        mainImage.PutText("" + DIRECTION, new CvPoint(1 * WIDTH / 3 + (WIDTH / 6), 1 * HEIGHT / 3 + Height / 6), new CvFont(FontFace.HersheyDuplex, 1.3, 1.3), CvColor.Orange);
                      
                    }
                    catch (OpenCvSharp.OpenCvSharpException e)
                    {

                    }
                    catch (OpenCVException e) { }

                    Bitmap bm = BitmapConverter.ToBitmap(mainImage);
                    pictureBox.Width = mainImage.Width;
                    pictureBox.Height = mainImage.Height;

                    bm.SetResolution(pictureBox.Width, pictureBox.Height);
                    pictureBox.Image = bm;
#if DEBUG
                    Bitmap bm2 = BitmapConverter.ToBitmap(gray);
                    bm2.SetResolution(pictureBoxDebug.Width, pictureBoxDebug.Height);
                    pictureBoxDebug.Image = bm2;
#endif 
                }

            }


        }

        private void setupDebug(IplImage mainImage)
        {
#if DEBUG
            CvPoint p01 = new CvPoint(mainImage.Width / 3, 0);
            CvPoint p31 = new CvPoint(mainImage.Width / 3, mainImage.Height);
            CvPoint p02 = new CvPoint(2 * mainImage.Width / 3, 0);
            CvPoint p32 = new CvPoint(2 * mainImage.Width / 3, mainImage.Height);
            //Horizatial
            CvPoint p10 = new CvPoint(0, mainImage.Height / 3);
            CvPoint p13 = new CvPoint(mainImage.Width, mainImage.Height / 3);
            CvPoint p20 = new CvPoint(0, 2 * mainImage.Height / 3);
            CvPoint p23 = new CvPoint(mainImage.Width, 2 * mainImage.Height / 3);

            mainImage.Line(p01, p31, CvColor.LightGreen, 1, LineType.AntiAlias, 0);
            mainImage.Line(p02, p32, CvColor.LightGreen, 1, LineType.AntiAlias, 0);
            mainImage.Line(p10, p13, CvColor.LightGreen, 1, LineType.AntiAlias, 0);
            mainImage.Line(p20, p23, CvColor.LightGreen, 1, LineType.AntiAlias, 0);
            int WIDTH = mainImage.Width;
            int HEIGHT = mainImage.Height;
         
#endif
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void cannyBar1_Scroll(object sender, EventArgs e)
        {
            cannyThre1 = cannyBar1.Value;
        }

        private void cannyBar2_Scroll(object sender, EventArgs e)
        {
            cannyThre2 = cannyBar2.Value;
        }

        private void threshold1_Scroll(object sender, EventArgs e)
        {
            thre1 = threshold1.Value;
        }

        private void threshold2_Scroll(object sender, EventArgs e)
        {
            thre2 = threshold1.Value;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_cameraThread != null && _cameraThread.IsAlive)
            {
                _cameraThread.Abort();
            }
          
        }
    }
}