using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Library.Imaging
{
    public readonly struct OpenCvContentType
    {
        private static readonly Dictionary<OpenCvImageFormat, (string Extension, string MediaType)> _contentTypes = new Dictionary<OpenCvImageFormat, (string Extension, string MediaType)>
        {
            { OpenCvImageFormat.Bmp, (".bmp", "image/bmp") },
            { OpenCvImageFormat.Pbm, (".pbm", "image/x-portable-bitmap") },
            { OpenCvImageFormat.Pgm, (".pgm", "image/x-portable-graymap") },
            { OpenCvImageFormat.Ppm, (".ppm", "image/x-portable-pixmap") },
            { OpenCvImageFormat.Jpeg, (".jpeg", "image/jpeg") },
            { OpenCvImageFormat.Jpg, (".jpg", "image/jpeg") },
            { OpenCvImageFormat.Jpe, (".jpe", "image/jpeg") },
            { OpenCvImageFormat.Tiff, (".tif", "image/tif") },
            { OpenCvImageFormat.Tif, (".tiff", "image/tiff") },
            { OpenCvImageFormat.Png, (".png", "image/png") }
        };

        public OpenCvContentType(OpenCvImageFormat imageFormat)
        {
            var (extension, mediaType) = _contentTypes[imageFormat];
            ImageFormat = imageFormat;
            Extension = extension;
            MediaType = mediaType;
        }

        internal OpenCvContentType(OpenCvImageFormat imageFormat, string extension, string mediaType)
        {
            ImageFormat = imageFormat;
            Extension = extension;
            MediaType = mediaType;
        }

        public static OpenCvContentType Parse(string extensionOrType)
        {
            if (extensionOrType == null) throw new ArgumentNullException(nameof(extensionOrType));

            var (format, (extension, mediaType)) = _contentTypes.FirstOrDefault(i => i.Value.Extension.Equals(extensionOrType, StringComparison.OrdinalIgnoreCase) || i.Value.MediaType.Equals(extensionOrType, StringComparison.OrdinalIgnoreCase));

            if (extension == null) throw new ArgumentException("Value is not a supported extension or media type.", nameof(extensionOrType));

            return new OpenCvContentType(format, extension, mediaType);
        }

        public static bool TryParse(string extensionOrType, out OpenCvContentType contentType)
        {
            var (format, (extension, mediaType)) = _contentTypes.FirstOrDefault(i => i.Value.Extension.Equals(extensionOrType, StringComparison.OrdinalIgnoreCase) || i.Value.MediaType.Equals(extensionOrType, StringComparison.OrdinalIgnoreCase));

            if (extension == null)
            {
                contentType = default;
                return false;
            }

            contentType = new OpenCvContentType(format, extension, mediaType);
            return true;
        }

        public readonly OpenCvImageFormat ImageFormat;
        public readonly string Extension;
        public readonly string MediaType;
    }

    public enum OpenCvImageFormat : byte
    {
        Bmp,
        Pbm,
        Pgm,
        Ppm,
        Jpeg,
        Jpg,
        Jpe,
        Tiff,
        Tif,
        Png
    }
}