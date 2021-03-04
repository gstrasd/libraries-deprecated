using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Library.Imaging
{
    public class ImageFeatures
    {
        public Mat Descriptors { get; set; }
        public KeyPoint[] KeyPoints { get; set; }
    }
}
