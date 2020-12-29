using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Libraries.AspNetCore.Extensions
{
    public static class ControllerExtensions
    {
        private static ILookup<ImageFormat, string> _contentTypes = new List<(ImageFormat ImageFormat, string Extension, string ContentType)>
        {
            ( ImageFormat.Gif, "gif", "image/gif" ),
            ( ImageFormat.Jpeg, "jpg", "image/jpeg" ),
            ( ImageFormat.Jpeg, "jpeg", "image/jpeg" ),
            ( ImageFormat.Png, "png", "image/png" ),
            ( ImageFormat.Tiff, "tiff", "image/tiff" ),
            ( ImageFormat.Tiff, "tif", "image/tiff" )
        }.ToLookup(p => p.ImageFormat, p => p.ContentType);

        public static IActionResult Image(this ControllerBase controller, Image image)
        {Host.CreateDefaultBuilder().UseEnvironment()
            if (image == null) throw new ArgumentNullException(nameof(image));

            var contentType = _contentTypes[image.RawFormat].FirstOrDefault();
            if (contentType == null) return new UnsupportedMediaTypeResult();

            var stream = new MemoryStream();
            image.Save(stream, image.RawFormat);
            stream.Position = 0;

            return controller.File(stream, contentType);
        }
    }
}
