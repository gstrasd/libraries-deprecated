using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Library.Imaging.ComputerVision;

namespace Library.Imaging
{
    public readonly struct ContentType
    {
        private static readonly Dictionary<ImageFormat, (string Extension, string MediaType)> _contentTypes = new Dictionary<ImageFormat, (string Extension, string MediaType)>
        {
            { ImageFormat.Bmp, (".bmp", "image/bmp") },
            { ImageFormat.Pbm, (".pbm", "image/x-portable-bitmap") },
            { ImageFormat.Pgm, (".pgm", "image/x-portable-graymap") },
            { ImageFormat.Ppm, (".ppm", "image/x-portable-pixmap") },
            { ImageFormat.Jpeg, (".jpeg", "image/jpeg") },
            { ImageFormat.Jpg, (".jpg", "image/jpeg") },
            { ImageFormat.Jpe, (".jpe", "image/jpeg") },
            { ImageFormat.Tiff, (".tif", "image/tif") },
            { ImageFormat.Tif, (".tiff", "image/tiff") },
            { ImageFormat.Png, (".png", "image/png") }
        };

        public ContentType(ImageFormat imageFormat)
        {
            var (extension, mediaType) = _contentTypes[imageFormat];
            ImageFormat = imageFormat;
            Extension = extension;
            MediaType = mediaType;
        }

        internal ContentType(ImageFormat imageFormat, string extension, string mediaType)
        {
            ImageFormat = imageFormat;
            Extension = extension;
            MediaType = mediaType;
        }

        public static string GetMediaType(ImageFormat imageFormat)
        {
            return _contentTypes[imageFormat].MediaType;
        }

        public static string GetFileExtension(ImageFormat imageFormat)
        {
            return _contentTypes[imageFormat].Extension;
        }

        public static ContentType Parse(string extensionOrMediaType)
        {
            if (extensionOrMediaType == null) throw new ArgumentNullException(nameof(extensionOrMediaType));

            var (format, (extension, mediaType)) = _contentTypes.FirstOrDefault(i => i.Value.Extension.Equals(extensionOrMediaType, StringComparison.OrdinalIgnoreCase) || i.Value.MediaType.Equals(extensionOrMediaType, StringComparison.OrdinalIgnoreCase));

            if (extension == null) throw new ArgumentException("Value is not a supported extension or media type.", nameof(extensionOrMediaType));

            return new ContentType(format, extension, mediaType);
        }

        public static bool TryParse(string extensionOrMediaType, out ContentType contentType)
        {
            var (format, (extension, mediaType)) = _contentTypes.FirstOrDefault(i => i.Value.Extension.Equals(extensionOrMediaType, StringComparison.OrdinalIgnoreCase) || i.Value.MediaType.Equals(extensionOrMediaType, StringComparison.OrdinalIgnoreCase));

            if (extension == null)
            {
                contentType = default;
                return false;
            }

            contentType = new ContentType(format, extension, mediaType);
            return true;
        }

        public static bool IsSupported(string extensionOrMediaType)
        {
            return !String.IsNullOrWhiteSpace(extensionOrMediaType) && _contentTypes.Any(i => i.Value.Extension.Equals(extensionOrMediaType, StringComparison.OrdinalIgnoreCase) || i.Value.MediaType.Equals(extensionOrMediaType, StringComparison.OrdinalIgnoreCase));
        }

        public readonly ImageFormat ImageFormat;
        public readonly string Extension;
        public readonly string MediaType;
    }
}