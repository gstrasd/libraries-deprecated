using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	internal sealed class IpDatabaseHeaderReader : IDisposable
	{
		private readonly MemoryMappedViewStream _stream;
		private bool _disposed;

		public IpDatabaseHeaderReader(MemoryMappedFile file)
		{
			_stream = file.CreateViewStream();
		}

		~IpDatabaseHeaderReader() => Dispose(false);

		public IpDatabaseHeader Read()
		{
			using var reader = new BinaryReader(_stream);
			var dbType = reader.ReadByte();
			var dbColumn = reader.ReadByte();
			var dbYear = reader.ReadByte();
			var dbMonth = reader.ReadByte();
			var dbDay = reader.ReadByte();
			var dbCount = reader.ReadInt32();
			var baseAddress = reader.ReadInt32();
			var dbCountIPv6 = reader.ReadInt32();
			var baseAddressIPv6 = reader.ReadInt32();
			var indexedBaseAddress = reader.ReadInt32();
			var indexBaseAddressIPv6 = reader.ReadInt32();
			var productCode = reader.ReadByte();
			var productType = reader.ReadByte();
			var fileSize = reader.ReadInt32();

			var header = new IpDatabaseHeader
			{
				DbType = dbType,
				DbColumn = dbColumn,
				DbYear = dbYear,
				DbMonth = dbMonth,
				DbDay = dbDay,
				DbCount = dbCount,
				BaseAddress = baseAddress,
				DbCountIPv6 = dbCountIPv6,
				BaseAddressIPv6 = baseAddressIPv6,
				IndexedBaseAddress = indexedBaseAddress,
				IndexBaseAddressIPv6 = indexBaseAddressIPv6,
				ProductCode = productCode,
				ProductType = productType,
				FileSize = fileSize
			};

			return header;
		}

		public void Dispose() => Dispose(true);

		private void Dispose(bool disposing)
		{
			if (_disposed) return;

			_stream.Dispose();
			_disposed = true;

			if (disposing) GC.SuppressFinalize(this);
		}
	}
}