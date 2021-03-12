using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Library.Imaging.ComputerVision
{
    public class ImageFeatures
    {
        public Mat Descriptors { get; set; }
        public KeyPoint[] KeyPoints { get; set; }
    }
}
