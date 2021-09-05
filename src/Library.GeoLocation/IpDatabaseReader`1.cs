using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Library.GeoLocation.GeoFieldName;

namespace Library.GeoLocation
{
	public class IpDatabaseReader<T> : IDisposable
	{
		private readonly MemoryMappedViewAccessor _view;
		private readonly IIpDatabaseDataReader _reader;
		private readonly IIpDatabaseDataFactory<T> _factory;
		private bool _disposed;

		public IpDatabaseReader(IpDatabase database, IIpDatabaseDataFactory<T> factory)
		{
			if (database == null) throw new ArgumentNullException(nameof(database));
			if (database.IsDisposed) throw new ObjectDisposedException(nameof(database), "Cannot read a disposed database.");
			_factory = factory;

			// Create view accessor and data reader
			_view = database.File.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
			_reader = new IpDatabaseDataReader(_view, database.Version, database.Header.IndexedBaseAddress - 1, database.Header.BaseAddress - 1);
		}

		public Task<T> ReadAsync(string address)
		{
			AssertNotDisposed();

			if (address == null) throw new ArgumentNullException(nameof(address));
			if (!IPAddress.TryParse(address, out var ipAddress)) throw new ArgumentException("Invalid IP address.");

			var row = _reader.SeekRow(ipAddress);
			var value = _factory.Read(_reader, row);

			return Task.FromResult(value);
		}

		public Task<T> ReadAsync(IPAddress ipAddress)
		{
			AssertNotDisposed();

			if (ipAddress == null) throw new ArgumentNullException(nameof(ipAddress));

			var row = _reader.SeekRow(ipAddress);
			var value = _factory.Read(_reader, row);

			return Task.FromResult(value);
		}

		private void AssertNotDisposed()
		{
			if (_disposed) throw new InvalidOperationException("Cannot perform operations using a disposed reader.");
		}

		[SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
		public void Dispose() => Dispose(true);

		[SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
		private void Dispose(bool disposing)
		{
			if (_disposed) return;

			_view.Dispose();
			_disposed = true;

			if (disposing) GC.SuppressFinalize(this);
		}
	}
}