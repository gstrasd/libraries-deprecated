using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Bitmap = System.Drawing.Bitmap ;

namespace Library.Imaging.Extensions
{
    public static class MatExtensions
    {
        public static bool IsPortrait(this Mat image) => image.Height > image.Width;

        public static bool IsLandscape(this Mat image) => image.Width > image.Height;

        public static bool IsSquare(this Mat image) => image.Width == image.Height;

        public static float GetAspectRatio(this Mat image) => image.Width / (float)image.Height;

        public static Stream ToStream(this Mat image, OpenCvImageFormat format)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (!Enum.IsDefined(typeof(OpenCvImageFormat), format)) throw new InvalidEnumArgumentException(nameof(format), (int) format, typeof(OpenCvImageFormat));

            var extension = new OpenCvContentType(format).Extension;

            return image.ToMemoryStream(extension);
        }

        public static Mat ToGrayscale(this Mat image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            var grayscale = new Mat();
            Cv2.CvtColor(image, grayscale, ColorConversionCodes.BGR2GRAY);

            return grayscale;
        }

        public static Mat To8BitBrg(this Mat image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            var color = new Mat();
            Cv2.CvtColor(image, color, ColorConversionCodes.BGRA2BGR);

            return color;
        }

        public static Mat Trim(this Mat image, float thresholdVariance = 0.1f, float whitespaceVariance = 0.01f, float colorVariance = 0.15f)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            var bounds = DetectWhitespace(image, thresholdVariance, whitespaceVariance, colorVariance);
            var trimmed = image.SubMat(bounds);
            return trimmed;
        }

        public static Mat Pad(this Mat image, float targetAspectRatio = default)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (targetAspectRatio < 0) throw new ArgumentOutOfRangeException(nameof(targetAspectRatio), "Value must be greater than 0 for custom aspect ratios or 0 for a default 3:1 landscape or 1:3 portrait aspect ratio.");

            var aspectRatio = (float)image.Width / image.Height;
            if (targetAspectRatio.Equals(default)) targetAspectRatio = aspectRatio >= 1 ? 1.333f : 0.75f;

            if (aspectRatio > targetAspectRatio)    // make taller
            {
                var blurredHeight = (int)Math.Ceiling(image.Width / targetAspectRatio);
                var blurredWidth = (int)Math.Ceiling(blurredHeight * aspectRatio);
                var blurRadius = image.Width * 0.1;
                var blurred = image.Resize(new Size(blurredWidth, blurredHeight)).Blur(new Size(blurRadius, blurRadius));
                var x = (blurred.Width - image.Width) / 2;
                var y = (blurred.Height - image.Height) / 2;
                var layered = blurred.Overlay(image, new Point(x, y));
                var padded = layered.SubMat(0, blurred.Height - 1, x, x + image.Width - 1);

                return padded;
            }

            if (aspectRatio < targetAspectRatio)    // make wider
            {
                var blurredWidth = (int)Math.Ceiling(image.Height * targetAspectRatio);
                var blurredHeight = (int)Math.Ceiling(blurredWidth / aspectRatio);
                var blurRadius = image.Height * 0.1;
                var blurred = image.Resize(new Size(blurredWidth, blurredHeight)).Blur(new Size(blurRadius, blurRadius));
                var x = (blurred.Width - image.Width) / 2;
                var y = (blurred.Height - image.Height) / 2;
                var layered = blurred.Overlay(image, new Point(x, y));
                var padded = layered.SubMat(y, y + image.Height - 1, 0, blurred.Width - 1);

                return padded;
            }

            return image;
        }

        public static Mat Pad(this Mat image, float aspectRatio, Scalar color)
        {
            throw new NotImplementedException();
        }

        public static Mat Overlay(this Mat image, Mat layer, int x, int y) => Overlay(image, layer, new Point(x, y));

        public static Mat Overlay(this Mat image, Mat layer, OpenCvSharp.Point location)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (layer == null) throw new ArgumentNullException(nameof(layer));

            var clone = image.Clone();
            var roi = new Mat(clone, new Rect(location, new Size(layer.Width, layer.Height)));
            layer.CopyTo(roi);

            return clone;
        }

        public static Rect DetectWhitespace(this Mat image, float thresholdVariance = 0.1f, float whitespaceVariance = 0.01f, float colorVariance = 0.15f)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            var color = image.To8BitBrg();
            var grayscale = image.ToGrayscale();

            // Remove iPhone slider
            grayscale.Rectangle(new Point(image.Width * 0.3, image.Height - image.Height * 0.02), new Point(image.Width * 0.7, image.Height - 1), new Scalar(0d), -1);
            var threshold = grayscale.Threshold(thresholdVariance * Byte.MaxValue, Byte.MaxValue, ThresholdTypes.Binary);

            var top = 0;
            var bottom = image.Height - 1;
            var left = 0;
            var right = image.Width - 1;

            bool Trimmable(Mat colorRow, Mat thresholdRow)
            {
                var size = Math.Max(colorRow.Width, colorRow.Height);
                var sum = colorRow.Sum();
                var min = Math.Min(sum.Val0, Math.Min(sum.Val1, sum.Val2)) / (size * Byte.MaxValue);
                var max = Math.Max(sum.Val0, Math.Max(sum.Val1, sum.Val2)) / (size * Byte.MaxValue);
                var colorRange = max - min;
                var whitespaceRange = thresholdRow.Sum().Val0 / (size * Byte.MaxValue);

                return whitespaceRange < whitespaceVariance && colorRange < colorVariance;
            }

            // Determine whitespace bounds
            Parallel.Invoke(
                () => { while (Trimmable(color.RowRange(top, top + 1), threshold.RowRange(top, top + 1))) top++; },
                () => { while (Trimmable(color.RowRange(bottom - 1, bottom), threshold.RowRange(bottom - 1, bottom))) bottom--; },
                () => { while (Trimmable(color.ColRange(left, left + 1), threshold.ColRange(left, left + 1))) left++; },
                () => { while (Trimmable(color.ColRange(right - 1, right), threshold.ColRange(right - 1, right))) right--; });

            var width = right - left + 1;
            var height = bottom - top + 1;
            var bounds = new Rect(left, top, width, height);

            return bounds;
        }
    }
}