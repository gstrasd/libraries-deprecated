using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Imaging.Extensions;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.Flann;

namespace Library.Imaging
{
    public class ImageComparer : IDisposable
    {
        private readonly SIFT _sift;
        private readonly FlannBasedMatcher _matcher;
        private bool _disposed;

        public ImageComparer(int trees = 4)
        {
            _sift = SIFT.Create();
            var indexParams = new KDTreeIndexParams(trees);
            indexParams.SetAlgorithm(0);
            _matcher = new FlannBasedMatcher(indexParams, new SearchParams());
        }

        ~ImageComparer()
        {
            Dispose(false);
        }

        public ImageFeatures IdentifyImageFeatures(Mat image, Mat mask = default)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            return IdentifyImageFeaturesInternal(image, mask);
        }

        public Task<ImageFeatures> IdentifyImageFeaturesAsync(Mat image, Mat mask = default, CancellationToken token = default)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            return Task<ImageFeatures>.Factory.StartNew(() => IdentifyImageFeaturesInternal(image, mask),
                token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);
        }

        private ImageFeatures IdentifyImageFeaturesInternal(Mat image, Mat mask = default)
        {
            var descriptors = new Mat();

            _sift.DetectAndCompute(image, mask, out var keyPoints, descriptors);

            var features = new ImageFeatures
            {
                Descriptors = descriptors,
                KeyPoints = keyPoints
            };

            return features;
        }

        public float Compare(ImageFeatures first, ImageFeatures second, int k = 2, float ratio = 0.6f)
        {
            EnsureNotDisposed();

            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (k <= 1) throw new ArgumentOutOfRangeException(nameof(k), "k must be a value greater than 1.");
            if (ratio <= 0) throw new ArgumentOutOfRangeException(nameof(ratio), "Ratio must be a value greater than 0.");

            return CompareInternal(first, second, k, ratio);
        }

        public Task<float> CompareAsync(ImageFeatures first, ImageFeatures second, int k = 2, float ratio = 0.6f, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (k <= 1) throw new ArgumentOutOfRangeException(nameof(k), "k must be a value greater than 1.");
            if (ratio <= 0) throw new ArgumentOutOfRangeException(nameof(ratio), "Ratio must be a value greater than 0.");

            return Task<float>.Factory.StartNew(() => CompareInternal(first, second, k, ratio),
                token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);
        }

        public float Compare(Mat first, Mat second, int k = 2, float ratio = 0.6f)
        {
            EnsureNotDisposed();

            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (k <= 1) throw new ArgumentOutOfRangeException(nameof(k), "k must be a value greater than 1.");
            if (ratio <= 0) throw new ArgumentOutOfRangeException(nameof(ratio), "Ratio must be a value greater than 0.");

            ImageFeatures firstImageFeatures = null;
            ImageFeatures secondImageFeatures = null;

            Parallel.Invoke(
                () => firstImageFeatures = IdentifyImageFeaturesInternal(first),
                () => secondImageFeatures = IdentifyImageFeaturesInternal(second));

            return CompareInternal(firstImageFeatures, secondImageFeatures, k, ratio);
        }

        public async Task<float> CompareAsync(Mat first, Mat second, int k = 2, float ratio = 0.6f, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (k <= 1) throw new ArgumentOutOfRangeException(nameof(k), "k must be a value greater than 1.");
            if (ratio <= 0) throw new ArgumentOutOfRangeException(nameof(ratio), "Ratio must be a value greater than 0.");

            var (firstFeatures, secondFeatures) = await Task.Factory.StartNew(() =>
                {
                    ImageFeatures firstImageFeatures = null;
                    ImageFeatures secondImageFeatures = null;

                    Parallel.Invoke(
                        () => firstImageFeatures = IdentifyImageFeaturesInternal(first),
                        () => secondImageFeatures = IdentifyImageFeaturesInternal(second));

                    return (firstImageFeatures, secondImageFeatures);
                },
                token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);

            return await Task<float>.Factory.StartNew(
                () => CompareInternal(firstFeatures, secondFeatures, k, ratio),
                token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);
        }

        private float CompareInternal(ImageFeatures first, ImageFeatures second, int k = 2, float ratio = 0.6f)
        {
            var matches = _matcher.KnnMatch(first.Descriptors, second.Descriptors, k);
            var good = matches.Count(m => m[0].Distance < ratio * m[1].Distance);
            var similarity = (float)good / Math.Min(first.KeyPoints.Length, second.KeyPoints.Length);

            return similarity;
        }

        public async IAsyncEnumerable<float> CompareAsync(ImageFeatures first, IEnumerable<ImageFeatures> second, int k = 2, float ratio = 0.6f, [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (k <= 1) throw new ArgumentOutOfRangeException(nameof(k), "k must be a value greater than 1.");
            if (ratio <= 0) throw new ArgumentOutOfRangeException(nameof(ratio), "Ratio must be a value greater than 0.");

            var results = second
                .ToAsyncEnumerable()
                .SelectAwait(async item =>
                {
                    var similarity = await Task<float>.Factory.StartNew(
                        () => CompareInternal(first, item, k, ratio),
                        token,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously,
                        TaskScheduler.Default);

                    return similarity;
                });

            await foreach (var result in results.WithCancellation(token))
            {
                if (token.IsCancellationRequested) yield break;
                yield return result;
            }
        }

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
                if (_sift != null && !_sift.IsDisposed) _sift.Dispose();
                if (_matcher != null && !_matcher.IsDisposed) _matcher.Dispose();
            }

            _disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException($"Cannot perform operations on a disposed {nameof(ImageComparer)}.");
            if (_sift.IsDisposed) throw new ObjectDisposedException($"Cannot perform operations on a disposed {nameof(SIFT)}.");
            if (_matcher.IsDisposed) throw new ObjectDisposedException($"Cannot perform operations on a disposed {nameof(FlannBasedMatcher)}.");
        }
    }
}
