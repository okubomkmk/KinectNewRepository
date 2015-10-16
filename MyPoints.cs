using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    class MyPoints
    {
        
        public double X { set; get; }
        public double Y { set; get; }

        public MyPoints()
        {
            this.X = 0;
            this.Y = 0;
        }

        public MyPoints(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
    }
    
}
