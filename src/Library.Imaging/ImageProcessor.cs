using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Library.Cryptography;
using OpenCvSharp;

namespace Library.Imaging
{
    internal class ImageProcessor
    {
        private const int _100k = 100 * 1024;

        public Mat ResizeImage(Mat image, int width, int height)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Argument must be a positive, non-zero value.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Argument must be a positive, non-zero value.");

            var clone = image.Clone();
            return clone.Resize(new Size(width, height), interpolation: InterpolationFlags.Area);
        }

        public Mat ResizeImageWithAspectRatio(Mat image, int maxWidth, int maxHeight)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (maxWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maxWidth), "Argument must be a positive, non-zero value.");
            if (maxHeight <= 0) throw new ArgumentOutOfRangeException(nameof(maxHeight), "Argument must be a positive, non-zero value.");

            var aspectRatio = image.Width / (float)image.Height;

            // Scale image to fit within viewport while still maintaining its aspect ratio
            var resizedWidth = maxWidth;
            var resizedHeight = (int)(maxWidth / aspectRatio);
            if (resizedHeight > maxHeight)
            {
                resizedHeight = maxHeight;
                resizedWidth = (int)(maxHeight * aspectRatio);
            }

            return image.Resize(new Size(resizedWidth, resizedHeight), interpolation: InterpolationFlags.Area);
        }

        public byte[] EncodeAsJpeg(Mat image, float quality = 0.75f, bool progressive = false, int maxSize = _100k)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (quality <= 0 || quality > 1) throw new ArgumentOutOfRangeException(nameof(quality), "Argument must be a value between 0 and 1.");
            if (maxSize < 1024) throw new ArgumentOutOfRangeException(nameof(maxSize), "Argument must be a value of at least 1KB.");

            byte[] buffer;
            var jpgQuality = Convert.ToInt32(100 * quality);

            do
            {
                var encoding = new List<ImageEncodingParam>
                {
                    new ImageEncodingParam(ImwriteFlags.JpegOptimize, 1),
                    new ImageEncodingParam(ImwriteFlags.JpegQuality, jpgQuality)
                };
                if (progressive) encoding.Add(new ImageEncodingParam(ImwriteFlags.JpegProgressive, 1));
                buffer = image.ImEncode(".jpg", encoding.ToArray());
                jpgQuality -= 5;

                if (jpgQuality <= 0) throw new Exception($"Unable to encode image to a jpg stream less than { maxSize / 1024 }KB in size.");
            }
            while (buffer.Length > maxSize);

            return buffer;
        }

        public Rect DetectWhitespace(Mat image, float thresholdVariance = 0.1f, float whitespaceVariance = 0.01f, float colorVariance = 0.15f)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            // TODO: Determine image depth
            var color = new Mat(); // image.To8BitBgr();
            var grayscale = new Mat(); // image.ToGrayscale();

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

        public Mat PadImage(Mat image, float targetAspectRatio = default)
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
                var layered = Overlay(blurred, image, new Point(x, y));
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
                var layered = Overlay(blurred, image, new Point(x, y));
                var padded = layered.SubMat(y, y + image.Height - 1, 0, blurred.Width - 1);

                return padded;
            }

            return image;
        }

        public Mat TrimImage(Mat image, float thresholdVariance = 0.1f, float whitespaceVariance = 0.01f, float colorVariance = 0.15f)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            var bounds = DetectWhitespace(image, thresholdVariance, whitespaceVariance, colorVariance);
            var trimmed = image.SubMat(bounds);
            return trimmed;
        }

        // TODO: This may only work if the image is originally BGR
        public Mat ConvertToGrayscale(Mat image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            var grayscale = new Mat();
            Cv2.CvtColor(image, grayscale, ColorConversionCodes.BGR2GRAY);

            return grayscale;
        }

        public byte[] CalculateMd5Checksum(Mat image)
        {
            // Compute checksum based on a JPEG encoding of the image
            var matrix = image.ImEncode(".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, 40));
            var bytes = HashAlgorithm.Md5.ComputeHash(matrix);

            return bytes;
        }

        // TODO: Redo this by creating a canvas and adding layers
        private Mat Overlay(Mat image, Mat layer, int x, int y) => Overlay(image, layer, new Point(x, y));

        // TODO: Redo this by creating a canvas and adding layers
        private Mat Overlay(Mat image, Mat layer, Point location)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (layer == null) throw new ArgumentNullException(nameof(layer));

            var clone = image.Clone();
            var roi = new Mat(clone, new Rect(location, new Size(layer.Width, layer.Height)));
            layer.CopyTo(roi);

            return clone;
        }
    }
}
