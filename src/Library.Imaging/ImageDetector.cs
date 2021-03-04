using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using OpenCvSharp;

namespace Library.Imaging
{
    public abstract class ImageDetector : IDisposable
    {
        private bool _disposed;

        protected ImageDetector(string filename)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            if (!Path.IsPathFullyQualified(filename))
            {
                var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                filename = Path.Join(path, filename);
            }

            if (!File.Exists(filename)) throw new FileNotFoundException("Cascade classifier file not found.", filename);

            Classifier = Task<CascadeClassifier>.Factory.StartNew(() =>
                {
                    try
                    {
                        return new CascadeClassifier(filename);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("The cascade classifier could not be loaded. See inner exception for further details.", e);
                    }
                })
                .GetAwaiter()
                .GetResult();
        }

        protected ImageDetector(CascadeClassifier classifier)
        {
            if (classifier == null) throw new ArgumentNullException(nameof(classifier));

            Classifier = classifier;
        }

        ~ImageDetector()
        {
            Dispose(false);
        }

        protected CascadeClassifier Classifier { get; }

        public async Task<List<RectangleF>> DetectAsync([NotNull] Mat image, CancellationToken token = default)        // TODO: add parameter attributes and arg validation to everything in library
        {
            EnsureNotDisposed();
            var detections = await DetectInternalAsync(image, token) ?? new List<Rect>();

            return detections.Select(d => new RectangleF(
                d.X / (float)image.Width,
                d.Y / (float)image.Height,
                d.Width / (float)image.Width,
                d.Height / (float)image.Height)
            ).ToList();
        }

        protected abstract Task<List<Rect>> DetectInternalAsync(Mat image, CancellationToken token);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (Classifier != null && !Classifier.IsDisposed) Classifier.Dispose();
            }

            _disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException($"Cannot perform operations on a disposed {nameof(ImageComparer)}.");
            if (Classifier.IsDisposed) throw new ObjectDisposedException($"Cannot perform operations on a disposed {nameof(CascadeClassifier)}.");
        }
    }
}
