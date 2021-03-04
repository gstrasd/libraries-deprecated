using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Collections
{
    public static class ArrayExtensions
    {
        public static T[,] ToDimensionalArray<T>(this T[][] array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            var rank0 = array.GetUpperBound(0) + 1;
            if (rank0 == 0) return new T[0,0];
            
            var rank1 = array.Max(r => r.Length);
            if (rank0 == 1)
            {
                var row = new T[1, rank1];
                for (var index = 0; index < rank1; index++) row[0, index] = array[0][index];
                return row;
            }

            if (array.Any(r => r.Length != rank1)) throw new ArgumentException("The jagged array contains an inconsistent number of dimensional elements.", nameof(array));

            var dim = new T[rank0, rank1];
            for (var i0 = 0; i0 < rank0; i0++)
            for (var i1 = 0; i1 < rank1; i1++)
                dim[i0, i1] = array[i0][i1];

            return dim;
        }

        public static T[,] ToPartitionedArray<T>(this T[,] array, int rank0StartIndex, int rank1StartIndex, int rank0Length = Int32.MaxValue, int rank1Length = Int32.MaxValue)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            var rank0 = array.GetUpperBound(0);
            if (rank0StartIndex < 0 || rank0StartIndex > rank0) throw new ArgumentOutOfRangeException(nameof(rank0StartIndex), $"Index must be a value between {0} and {rank0}.");
            
            var rank1 = array.GetUpperBound(1);
            if (rank1StartIndex < 0 || rank1StartIndex > rank1) throw new ArgumentOutOfRangeException(nameof(rank1StartIndex), $"Index must be a value between {0} and {rank1}.");
            if (rank0Length < 0) throw new ArgumentOutOfRangeException(nameof(rank0Length), "Length must be a positive value.");
            if (rank1Length < 0) throw new ArgumentOutOfRangeException(nameof(rank1Length), "Length must be a positive value.");

            var partition = new T[rank0Length, rank1Length];
            var index0Length = Math.Min(rank0Length, rank0 - rank0StartIndex + 1);
            var index1Length = Math.Min(rank1Length, rank1 - rank1StartIndex + 1);
            for (var i0= 0; i0 < index0Length; i0++)
            for (var i1 = 0; i1 < index1Length; i1++)
                partition[i0, i1] = array[rank0StartIndex + i0, rank1StartIndex + i1];

            return partition;
        }

        public static IEnumerable<IEnumerable<T>> AsEnumerable<T>(this T[,] array, int rank = 0, int outerDirection = 1, int innerDirection = 1)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (rank < 0 || rank > 1) throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be either 0 to enumerate rows or 1 to enumerate columns.");
            if (outerDirection != -1 && outerDirection != 1) throw new ArgumentOutOfRangeException(nameof(outerDirection), "Direction must be either 1 to enumerate forward or -1 to enumerate backwards.");
            if (innerDirection != -1 && innerDirection != 1) throw new ArgumentOutOfRangeException(nameof(innerDirection), "Direction must be either 1 to enumerate forward or -1 to enumerate backwards.");

            var outerUpperBound = array.GetUpperBound(rank);
            var innerUpperBound = array.GetUpperBound(Math.Abs(rank - 1));

            if (outerDirection > 0)
            {
                for (var outerIndex = 0; outerIndex <= outerUpperBound; outerIndex++)
                {
                    yield return innerDirection > 0 ? EnumerateInnerDimensionForward(outerIndex) : EnumerateInnerDimensionBackwards(outerIndex);
                }
            }
            else
            {
                for (var outerIndex = outerUpperBound; outerIndex >= 0; outerIndex--)
                {
                    yield return innerDirection > 0 ? EnumerateInnerDimensionForward(outerIndex) : EnumerateInnerDimensionBackwards(outerIndex);
                }
            }

            IEnumerable<T> EnumerateInnerDimensionForward(int outerIndex)
            {
                for (var innerIndex = 0; innerIndex <= innerUpperBound; innerIndex++)
                {
                    yield return rank == 0 ? array[outerIndex, innerIndex] : array[innerIndex, outerIndex];
                }
            }

            IEnumerable<T> EnumerateInnerDimensionBackwards(int outerIndex) 
            {
                for (var innerIndex = innerUpperBound; innerIndex >= 0; innerIndex--)
                {
                    yield return rank == 0 ? array[outerIndex, innerIndex] : array[innerIndex, outerIndex];
                }
            }
        }

        public static IEnumerable<T[]> AsSlidingWindows<T>(this IEnumerable<T> array, int width, T defaultValue = default)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Argument must be a positive, non-zero value.");

            var buffer = new Queue<T>(Enumerable.Repeat(defaultValue, width));
            
            foreach (var value in array)
            {
                buffer.Dequeue();
                buffer.Enqueue(value);
                yield return buffer.ToArray();
            }

            for (var _ = 0; _ < width - 1; _++)
            {
                buffer.Dequeue();
                buffer.Enqueue(defaultValue);
                yield return buffer.ToArray();
            }
        }

        public static T[,][,] AsSlidingWindows<T>(this T[,] array, int width, int height, T defaultValue = default)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Argument must be a positive, non-zero value.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Argument must be a positive, non-zero value.");

            var arrayHeight = array.GetUpperBound(0) + height;
            var arrayWidth = array.GetUpperBound(1) + width;
            var rows = array.AsEnumerable().Select(row => row.AsSlidingWindows(width, defaultValue).ToArray()).ToArray();
            var defaultRow = Enumerable.Repeat(Enumerable.Repeat(defaultValue, width).ToArray(), arrayWidth + width - 1).ToArray();
            var buffer = new Queue<T[][]>(Enumerable.Repeat(defaultRow, height));
            var windows = new T[arrayHeight, arrayWidth][,];
            int rowIndex;

            for (rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var row = rows[rowIndex];
                buffer.Dequeue();
                buffer.Enqueue(row);
                for (var colIndex = 0; colIndex < arrayWidth; colIndex++)
                {
                    windows[rowIndex, colIndex] = buffer.Select(w => w[colIndex]).ToArray().ToDimensionalArray();
                }
            }

            for (var _ = 0; _ < height - 1; _++)
            {
                buffer.Dequeue();
                buffer.Enqueue(defaultRow);
                for (var colIndex = 0; colIndex < arrayWidth; colIndex++)
                {
                    windows[rowIndex, colIndex] = buffer.Select(w => w[colIndex]).ToArray().ToDimensionalArray();
                }

                rowIndex++;
            }

            return windows;
        }
    }
}
