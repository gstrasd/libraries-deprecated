using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Library.Imaging.Extensions
{
    public static class BitmapExtensions
    {
        public static Stream ToStream(this Bitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            var stream = new MemoryStream();
            bitmap.Save(stream, bitmap.RawFormat);
            stream.Position = 0;

            return stream;
        }

        public static Mat ToImage(this Bitmap bitmap)
        {
            var stream = bitmap.ToStream();

            return Mat.FromStream(stream, ImreadModes.AnyColor);
        }
    }
}