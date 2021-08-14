using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Imaging.ComputerVision;
using OpenCvSharp;
using Bitmap = System.Drawing.Bitmap ;

namespace Library.Imaging.Extensions
{
    public static class MatExtensions
    {
        private const int _100k = 100 * 1024;

        public static bool IsPortrait(this Mat image) => image.Height > image.Width;

        public static bool IsLandscape(this Mat image) => image.Width > image.Height;

        public static bool IsSquare(this Mat image) => image.Width == image.Height;

        public static float GetAspectRatio(this Mat image) => image.Width / (float)image.Height;

        public static Mat ToGrayscale(this Mat image)
        {
            var processor = new ImageProcessor();
            return processor.ConvertToGrayscale(image);
        }

        public static Mat Trim(this Mat image, float thresholdVariance = 0.1f, float whitespaceVariance = 0.01f, float colorVariance = 0.15f)
        {
            var processor = new ImageProcessor();
            return processor.TrimImage(image, thresholdVariance, whitespaceVariance, colorVariance);
        }

        public static Mat Pad(this Mat image, float targetAspectRatio = default)
        {
            var processor = new ImageProcessor();
            return processor.PadImage(image, targetAspectRatio);
        }

        public static Rect DetectWhitespace(this Mat image, float thresholdVariance = 0.1f, float whitespaceVariance = 0.01f, float colorVariance = 0.15f)
        {
            var processor = new ImageProcessor();
            return processor.DetectWhitespace(image, thresholdVariance, whitespaceVariance, colorVariance);
        }

        public static byte[] EncodeAsJpeg(this Mat image, float quality = 0.75f, bool progressive = false, int maxSize = _100k)
        {
            var processor = new ImageProcessor();
            return processor.EncodeAsJpeg(image, quality, progressive, maxSize);
        }

        public static Mat Resize(this Mat image, int width, int height)
        {
            var processor = new ImageProcessor();
            return processor.ResizeImage(image, width, height);
        }

        public static Mat ResizeWithAspectRatio(this Mat image, int maxWidth, int maxHeight)
        {
            var processor = new ImageProcessor();
            return processor.ResizeImageWithAspectRatio(image, maxWidth, maxHeight);
        }

        public static byte[] CalculateChecksum(this Mat image)
        {
            var processor = new ImageProcessor();
            return processor.CalculateMd5Checksum(image);
        }
    }
}