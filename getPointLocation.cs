using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace Microsoft.Samples.Kinect.DepthBasics
{
    class getPointLocation
    {
        public double X { set; get ;}
        public double Y { set; get ;}
        public ushort status { set; get; }
        public getPointLocation(Point p)
        {
            X = System.Windows.Forms.Control.MousePosition.X - p.X;
            Y = System.Windows.Forms.Control.MousePosition.Y - p.Y - 1;            
        }
        public getPointLocation()
        {
            X = 200;
            Y = 200;
        }
    }
}
