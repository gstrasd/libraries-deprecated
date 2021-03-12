using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Imaging.Extensions;
using OpenCvSharp;

namespace Library.Imaging.ComputerVision
{
    public class FaceDetector : ImageDetector
    {
        public int solution(string s)
        {
            var count = 0;
            var word = "BALLOON";
            var array = s.ToCharArray();

            while (true)
            {
                foreach (char letter in word)
                {
                    var index = s.IndexOf(letter);
                    if (index < 0) return count;
                    array[index] = '\0';
                    array = array.Where(c => c > '\0').Select(c => c).ToArray();
                    s = new String(array);
                }

                count++;
            }
        }

        private static readonly SemaphoreSlim _detect = new SemaphoreSlim(1, 1);

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
                    _detect.Wait(token);    // TODO: This does not appear to be thread-safe. Concurrent invocations lead to memory access violation exceptions. Find a way around this.

                    try
                    {
                        return Classifier.DetectMultiScale(grayscale, 1.1d, 3, HaarDetectionTypes.DoCannyPruning, new Size(minSize, minSize));
                    }
                    finally
                    {
                        _detect.Release();
                    }
                },
                token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);

            // Return largest faces first
            var sortedFaces = faces.OrderByDescending(f => f.Width * f.Height).ToList();

            if (!portrait || image.Width >= 400) return sortedFaces;

            // The faces in portrait photos tend to be located near the top.
            // So, assume a face detection below the the center on a lower resolution photo is erroneous.
            return sortedFaces.Where(rect => rect.Top <= image.Height / 2).ToList();
        }
    }
}