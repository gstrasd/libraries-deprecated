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
    public static class StreamExtensions
    {
        public static Bitmap ToBitmap(this Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var bitmap = new Bitmap(stream);
            stream.Position = 0;

            return bitmap;
        }

        public static Mat ToImage(this Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var image = Mat.FromStream(stream, ImreadModes.Unchanged);
            stream.Position = 0;

            return image;
        }
    }
}
