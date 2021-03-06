﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


namespace Microsoft.Samples.Kinect.DepthBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows.Threading;
    /// check updated
    /// <summary> 
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Map depth range to byte range
        /// </summary>
        private const int MapDepthToByte = 8000 / 256;
        private int RECORD_SIZE = 1024;
        private int counter = 0;
        private int writeDownedCounter = 0;
        private int fps_graph = 1;
        private bool cursol_locked = true;
        private Point p = new Point();
        private getPointLocation mouse = new getPointLocation();
        private List<KeyValuePair<string, ushort>> MyTimeValue = new List<KeyValuePair<string, ushort>>();
        private System.IO.StreamWriter writingSw = new System.IO.StreamWriter(@"C:\Users\mkuser\Documents\test.dat", true, System.Text.Encoding.GetEncoding("shift_jis"));
        private System.IO.StreamWriter writingCenter = new System.IO.StreamWriter(@"C:\Users\mkuser\Documents\CenterCheck.dat", true, System.Text.Encoding.GetEncoding("shift_jis"));
        private bool TimeStampFrag = false;
        private bool TimeStampWriteFlag = true;
        private bool WritingFlag = false;
        private bool NinePointFlag = false;
        private int WaitForStartingRecord = 1;
        private ushort[] fukuisan = new ushort[1];
        private ushort[] old_fukuisan = new ushort[1];
        private int distance_fukuisan_horizonal = 128;
        private int distance_fukuisan_vertial = 104;
        private System.Windows.Controls.Label[] ValueLabels;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reader for depth frames
        /// </summary>
        private DepthFrameReader depthFrameReader = null;

        /// <summary> merge sareru?s
        /// Description of the data contained in the depth frame
        /// </summary>
        private FrameDescription depthFrameDescription = null;
            
        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap depthBitmap = null;
        
        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private byte[] depthPixels = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        /// 


        public MainWindow()
        {
            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // open the reader for the depth frames
            this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();

            // wire handler for frame arrival
            this.depthFrameReader.FrameArrived += this.Reader_FrameArrived;

            // get FrameDescription from DepthFrameSource
            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // allocate space to put the pixels being received and converted
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];

            // create the bitmap to display
            this.depthBitmap = new WriteableBitmap(this.depthFrameDescription.Width, this.depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // use the window object as the view model in this simple example
            this.DataContext = this;



            // initialize the components (controls) of the window
            this.InitializeComponent();
            this.ButtonWriteDown.IsEnabled = false;
            Array.Resize(ref fukuisan,RECORD_SIZE * 9);
            Array.Resize(ref old_fukuisan, RECORD_SIZE);
            this.ValueLabels = new System.Windows.Controls.Label[9];
          
            this.ValueLabels[0] = this.Label0;
            this.ValueLabels[1] = this.Label1;
            this.ValueLabels[2] = this.Label2;
            this.ValueLabels[3] = this.Label3;
            this.ValueLabels[4] = this.Label4;
            this.ValueLabels[5] = this.Label5;
            this.ValueLabels[6] = this.Label6;
            this.ValueLabels[7] = this.Label7;
            this.ValueLabels[8] = this.Label8;
            
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.depthBitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.depthFrameReader != null)
            {
                // DepthFrameReader is IDisposable
                this.depthFrameReader.Dispose();
                this.depthFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }

            DateTime dtend = DateTime.Now;
            if (TimeStampWriteFlag)
            {
                writingSw.Write("\r\n" + dtend.ToString() + " closed\r\n"); //write time stamp
                writingCenter.Write("\r\n" + dtend.ToString() + " closed\r\n");
            }
            writingSw.Close();
            writingCenter.Close();
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>

        /// <summary>
        /// Handles the depth frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            bool depthFrameProcessed = false;

            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame()) //こいつを調べる
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                            (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue; // ushort.MaxValue is 65535

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance
                            
                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;
                        }
                    }
                }
            }
            
            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();
            }
        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {

            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;
            TextGenerate(frameData);
           
            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        private unsafe void TextGenerate(ushort* ProcessData)
        {

            string Resolution = "Resolution " + this.depthFrameDescription.Width.ToString() + "x" + this.depthFrameDescription.Height.ToString();
            string CursorLocation;
            ushort Value;            
            p = this.Viewbox1.PointToScreen(new Point(0, 0));
            getPointLocation mouseInPicture = new getPointLocation(p);
            
            if (cursol_locked)
            {
                mouseInPicture = (bool)(this.CheckLockCenter.IsChecked) ? getLockPosition() : mouse;
                if (WritingFlag)
                {
                    writeToArray(ProcessData, mouseInPicture);
                }
                    


                else
                {
                    TimeStampFrag = false;
                }
                CursorLocation = " Cursor Location " + (mouseInPicture.X.ToString() + " " + mouseInPicture.Y.ToString());
                Value = shiburinkawaiiyoo(ProcessData, mouseInPicture);
                  
            }
            else
            {
                if (Viewbox1.IsMouseOver)
                {
                    CursorLocation = " Cursor Location " + (mouseInPicture.X.ToString() + " " + mouseInPicture.Y.ToString());
                    Value = shiburinkawaiiyoo(ProcessData, mouseInPicture);
                }
                else
                {
                    CursorLocation = "out of image";
                    Value = 9000;
                    if (counter % (int)(30 / fps_graph) == 0)
                    {
                        //graphGenerateHorizonal(ProcessData, mouseInPicture);
                        //graphGenerateTimeDomain(ProcessData, mouseInPicture);
                    }
                }
            }

            this.StatusText = Resolution + CursorLocation + " cursor lock is " + cursol_locked.ToString() + " " + Value.ToString() + " Writing is "  + " Writed sample number =" + writeDownedCounter.ToString();
        }
        
        private unsafe ushort shiburinkawaiiyoo(ushort* ProcessData, double X,double Y)
        {
            return ProcessData[(int)(Y * this.depthFrameDescription.Width + X)];
        }
        private unsafe ushort shiburinkawaiiyoo(ushort* ProcessData, getPointLocation location)
        {
            return ProcessData[(int)(location.Y * this.depthFrameDescription.Width + location.X)];
        }

 
        private void Viewbox1_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            getPointLocation temp = new getPointLocation(this.Viewbox1.PointToScreen(new Point(0, 0)));

            if (cursol_locked)
            {
                mouse = temp;
            }

            cursol_locked = !cursol_locked;

            this.ButtonWriteDown.IsEnabled = cursol_locked;
        }

        private unsafe void writeToArray(ushort* ProcessData, getPointLocation location)
        {
            int index_value = 0;
            if (!TimeStampFrag && TimeStampWriteFlag)
            {
                DateTime dtnow = DateTime.Now;
                writingSw.Write("\nwriting start\n" + dtnow.ToString() + "\r\n"); //time stamp
                writingCenter.Write("\nwriting start\n" + dtnow.ToString() + "\r\n"); //time stamp
            }
            TimeStampFrag = true;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    index_value = i * 3 + j;
                    fukuisan[index_value + writeDownedCounter * 9] = shiburinkawaiiyoo(ProcessData, location.X - distance_fukuisan_horizonal + j * distance_fukuisan_horizonal, location.Y - distance_fukuisan_vertial + i * distance_fukuisan_vertial);
                    this.ValueLabels[index_value].Content = fukuisan[index_value + writeDownedCounter * 9];
                }
            }
            old_fukuisan[writeDownedCounter] = shiburinkawaiiyoo(ProcessData, location.X,location.Y);

            writeDownedCounter++;
            if (writeDownedCounter == fukuisan.Length / 9)
            {
                WritingFlag = false;
                writeToText();
                ButtonWriteDown.IsEnabled = true;
            }
        }

        private void writeToText()
        {
            for (int i = 0; i < fukuisan.Length; i++)
            {
                writingSw.Write(fukuisan[i] + "\r\n");
            }
            for (int j = 0; j < old_fukuisan.Length; j++ )
            {
                writingCenter.Write(old_fukuisan[j].ToString() + "\r\n");
            }
            
            if (TimeStampWriteFlag)
            {
                DateTime dtnow = DateTime.Now;
                writingSw.Write(dtnow.ToString() + "redord ended\r\n");
                writingCenter.Write(dtnow.ToString() + "redord ended\r\n");
            }

        }
        private getPointLocation getLockPosition()
        {
            double temp;
            getPointLocation LockPosition = new getPointLocation();
            if (double.TryParse(this.textXlock.Text, out temp))
            {
                LockPosition.X = temp;
            }
            else
            {
                return mouse;
            }

            if (double.TryParse(this.textYlock.Text, out temp))
            {
                LockPosition.Y = temp;
            }
            else
            {
                return mouse;
            }
            if((0 <= LockPosition.X && LockPosition.X < this.depthFrameDescription.Width) && (0 <= LockPosition.Y && LockPosition.Y < this.depthFrameDescription.Height))
            {
                return LockPosition;
            }
            else
            {
                return mouse;
            }
            
        }

        private void CheckNonTimeStamp_Checked(object sender, RoutedEventArgs e)
        {
            TimeStampWriteFlag = false;
        }

        private void CheckNonTimeStamp_Unchecked(object sender, RoutedEventArgs e)
        {
            TimeStampWriteFlag = true;
        }

        private void ButtonWriteDown_Click(object sender, RoutedEventArgs e)
        {
            DispatcherTimer  ButtonEditorTimer = new DispatcherTimer(DispatcherPriority.Normal);
            ButtonEditorTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            ButtonEditorTimer.Tick += new EventHandler(ButtonEdit);
            ButtonEditorTimer.Start();
            writeDownedCounter = 0;
            ButtonWriteDown.IsEnabled = false;
            
        }

        private void ButtonEdit(object sender, EventArgs e)
        {
            
            this.ButtonWriteDown.Content = (WaitForStartingRecord).ToString();
            WaitForStartingRecord--;
            if (WaitForStartingRecord == -1)
            {
                WritingFlag = true;
            }
        }

        private void CheckNinePoints_Checked(object sender, RoutedEventArgs e)
        {
            NinePointFlag = true;
        }

    }
}
