using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Library.Imaging;
using Library.Imaging.ComputerVision;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using OpenCvSharp;

namespace Library.Hosting.AspNetCore
{
    public class ImageContentResult : FileContentResult
    {
        public ImageContentResult(Mat image, ImageFormat imageFormat = ImageFormat.Jpeg, float quality = 0.75f) : base(GetFileContents(image, imageFormat, quality), Imaging.ContentType.GetMediaType(imageFormat))
        {
            Image = image;
        }

        public Mat Image { get; }

        private static byte[] GetFileContents(Mat image, ImageFormat imageFormat, float quality)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (imageFormat != ImageFormat.Jpeg && imageFormat != ImageFormat.Jpg && imageFormat != ImageFormat.Jpe) throw new NotSupportedException("Currently, only JPEG images are supported.");
            if (quality <= 0) throw new ArgumentOutOfRangeException(nameof(quality), "Argument must be a positive, non-zero value.");

            var jpgQuality = Convert.ToInt32(100 * quality);
            var extension = Imaging.ContentType.GetFileExtension(imageFormat);
            var bytes = image.ToBytes(extension, new ImageEncodingParam(ImwriteFlags.JpegQuality, jpgQuality));

            return bytes;
        }
    }
}
