using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	public sealed class IpDatabaseReader : IDisposable		// TODO: Add interface to make it interchangeable with other ip databases in the future and place interface in another project?
	{
		private readonly MemoryMappedViewAccessor _view;
		private readonly IpDatabaseDataReader _reader;
		private bool _disposed;

		public IpDatabaseReader(IpDatabase database)
		{
			if (database == null) throw new ArgumentNullException(nameof(database));
			if (database.IsDisposed) throw new ObjectDisposedException(nameof(database), "Cannot read a disposed database.");

			// Create view accessor and data reader
			_view = database.File.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
			_reader = new IpDatabaseDataReader(_view, database.Version, database.Header.IndexedBaseAddress - 1, database.Header.BaseAddress - 1);
		}

		~IpDatabaseReader() => Dispose(false);

		public Task<T> ReadAsync<T>(string address) where T : new()
		{
			AssertNotDisposed();

			if (address == null) throw new ArgumentNullException(nameof(address));
			if (!IPAddress.TryParse(address, out var ipAddress)) throw new ArgumentException("Invalid IP address.");

			var row = _reader.SeekRow(ipAddress);
			var factory = new ReflectingIpDatabaseDataFactory(ipAddress);
			
			_reader.CurrentRow = row;
			var value = factory.ReadAsync<T>(_reader);

			return value;
		}

		public Task<T> ReadAsync<T>(IPAddress ipAddress) where T : new()
		{
			AssertNotDisposed();
			// TODO: Account for scenario where a non-null ipAddress doesn't have any data, like "...:1"
			// TODO: Account for local ip address scenario
			if (ipAddress == null) throw new ArgumentNullException(nameof(ipAddress));

			var row = _reader.SeekRow(ipAddress);
			var factory = new ReflectingIpDatabaseDataFactory(ipAddress);
			
			_reader.CurrentRow = row;
			return factory.ReadAsync<T>(_reader);
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