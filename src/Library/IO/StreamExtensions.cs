using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.IO
{
    public static class StreamExtensions
    {
        public static Task<bool> ReadBooleanAsync(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 1, BitConverter.ToBoolean, token);
        }

        public static Task<byte> ReadByteAsync(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 1, (b, _) => b[0], token);
        }

        public static Task<char> ReadCharAsync(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 2, BitConverter.ToChar, token);
        }

        public static Task<short> ReadInt16Async(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 2, BitConverter.ToInt16, token);
        }

        public static Task<int> ReadInt32Async(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 4, BitConverter.ToInt32, token);
        }

        public static Task<long> ReadInt64Async(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 8, BitConverter.ToInt64, token);
        }

        public static Task<Guid> ReadGuidAsync(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 16, (b, _) => new Guid(b), token);
        }

        public static Task<ushort> ReadUInt16Async(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 2, BitConverter.ToUInt16, token);
        }

        public static Task<uint> ReadUInt32Async(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 4, BitConverter.ToUInt32, token);
        }

        public static Task<ulong> ReadUInt64Async(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 8, BitConverter.ToUInt64, token);
        }

        public static Task<float> ReadSingleAsync(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 4, BitConverter.ToSingle, token);
        }

        public static Task<double> ReadDoubleAsync(this Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return ReadAsync(stream, 8, BitConverter.ToDouble, token);
        }

        private static async Task<T> ReadAsync<T>(Stream stream, int size, Func<byte[], int, T> convert, CancellationToken token)
        {
            var buffer = new byte[size];
            await stream.ReadAsync(buffer, 0, size, token).ConfigureAwait(false);
            return convert(buffer, 0);
        }
    }
}
