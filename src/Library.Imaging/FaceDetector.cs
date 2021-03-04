using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Imaging.Extensions;
using OpenCvSharp;

namespace Library.Imaging
{
    public class FaceDetector : ImageDetector
    {
        public FaceDetector() : base(@"\data\haarcascades\haarcascade_frontalface_default.xml")
        {
        }

        public FaceDetector(string filename) : base(filename)
        {
        }

        public FaceDetector(CascadeClassifier classifier) : base(classifier)
        {
        }

        protected override async Task<List<Rect>> DetectInternalAsync(Mat image, CancellationToken token)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            // Create grayscale image
            var grayscale = image.ToGrayscale();

            // Prevent erroneous small face mismatches
            var minSize = image.Width * Math.Max(Convert.ToInt32(25 - (image.Width - 150) / 120d), 8) / 100;
            var portrait = image.Height >= image.Width;

            // Detect faces
            var faces = await Task<Rect[]>.Factory.StartNew(
                () =>
                {
                    lock (this)     // TODO: This does not appear to be thread-safe. Concurrent invocations lead to memory access violation exceptions. Find a way around this.
                    {
                        return Classifier.DetectMultiScale(grayscale, 1.1d, 3, HaarDetectionTypes.DoCannyPruning, new Size(minSize, minSize));
                    }
                },
                token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);

            if (!portrait || image.Width >= 400) return faces.ToList();

            // The faces in portrait photos tend to be located near the top.
            // So, assume a face detection below the the center on a lower resolution photo is erroneous.
            return faces.Where(rect => rect.Top <= image.Height / 2).ToList();
        }
    }
}