using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Library.Imaging.ComputerVision
{
    public abstract class ImageDetector
    {
        protected ImageDetector(string filename)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            if (!Path.IsPathFullyQualified(filename))
            {
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                filename = Path.Join(path, filename);
            }

            if (!File.Exists(filename)) throw new FileNotFoundException("Cascade classifier file not found.", filename);

            Classifier = new CascadeClassifier(filename);
        }

        protected ImageDetector(CascadeClassifier classifier)
        {
            if (classifier == null) throw new ArgumentNullException(nameof(classifier));

            Classifier = classifier;
        }

        ~ImageDetector()
        {
            GC.KeepAlive(Classifier);
        }

        protected CascadeClassifier Classifier { get; }

        public async Task<List<Rect>> DetectAsync([NotNull] Mat image, CancellationToken token = default)        // TODO: add parameter attributes and arg validation to everything in library
        {
            EnsureNotDisposed();
            var detections = await DetectInternalAsync(image, token) ?? new List<Rect>();

            return detections;
        }

        protected abstract Task<List<Rect>> DetectInternalAsync(Mat image, CancellationToken token);

        private void EnsureNotDisposed()
        {
            if (Classifier.IsDisposed) throw new ObjectDisposedException($"Cannot perform operations on a disposed {nameof(CascadeClassifier)}.");
        }
    }
}
