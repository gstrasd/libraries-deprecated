using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace Library.GeoLocation
{
	public sealed class IpDatabase : IDisposable
	{
		private static readonly Dictionary<string, (MemoryMappedFile File, IpDatabaseHeader Header, int Count)> _files = new(StringComparer.Ordinal);
		private bool _disposed;
		private string _path;

		private IpDatabase() { }

		~IpDatabase() => Dispose(false);

		public static IpDatabase Open(string filename)
		{
			if (filename == null) throw new ArgumentNullException(nameof(filename));

			var path = Path.GetFullPath(filename).ToLower();
			var info = new FileInfo(path);

			if (!info.Exists) throw new FileNotFoundException(filename);
			
			lock (((ICollection) _files).SyncRoot)
			{
				MemoryMappedFile file;
				IpDatabaseHeader header;

				if (_files.TryGetValue(path, out var entry))
				{
					file = entry.File;
					header = entry.Header;
					entry.Count++;
				}
				else
				{
					// Open file and read header
					file = MemoryMappedFile.CreateFromFile(path);
					using var reader = new IpDatabaseHeaderReader(file);
					header = reader.Read();

					// TODO: verify that other files wont load - like a text file or a word doc for example...
					// Verify proper database format
					if (header.ProductCode != 1) throw new IpDatabaseLoadException("Invalid database file format.");
					if (header.DbType > 25) throw new IpDatabaseLoadException("Invalid database file format.");
					if (header.IndexedBaseAddress <= 0) throw new IpDatabaseLoadException("Only indexed database formats are supported.");

					_files.Add(path, (file, header, 1));
				}

				// Build database
				var database = new IpDatabase
				{
					_path = path,
					File = file,
					Header = header,
					Date = new DateTime(header.DbYear, header.DbMonth, header.DbDay),
					Version = header.DbType,
					Count = header.DbCount,
					ProductCode = header.ProductCode,
				};

				return database;
			}
		}

		internal IpDatabaseHeader Header { get; private init; }
		internal MemoryMappedFile File { get; private init; }
		public DateTime Date { get; private init; }
		public int Version { get; private init; }
		public int Count { get; private init; }
		public int ProductCode { get; private init; }
		public bool IsDisposed => _disposed;

		public void Dispose() => Dispose(true);

		private void Dispose(bool disposing)
		{
			if (_disposed) return;

			lock (((ICollection) _files).SyncRoot)
			{
				var entry = _files.First(e => e.Key == _path);
				var value = entry.Value;
				if (value.Count > 1)
				{
					value.Count--;
				}
				else
				{
					File.Dispose();
					_files.Remove(_path);
				}

				_disposed = true;
			}

			if (disposing) GC.SuppressFinalize(this);
		}
	}
}