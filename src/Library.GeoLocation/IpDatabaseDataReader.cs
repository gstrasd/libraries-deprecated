using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	internal sealed class IpDatabaseDataReader : IIpDatabaseDataReader
	{
		private static readonly int[][] _columnIndexes =
		{
			new[] {0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, -1, -1, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, -1, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},

			new[] {0, 1, 2, 5, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, 3, -1, -1, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, 5, 3, 4, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, -1, 3, 4, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, 6, 3, 4, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},

			new[] {0, 1, 2, -1, 3, 4, -1, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, 7, 3, 4, 8, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, -1, 3, 4, -1, -1, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, 7, 3, 4, 8, 5, 6, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, -1, 3, 4, -1, 5, 6, -1, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1},

			new[] {0, 1, 2, 7, 3, 4, 8, 5, 6, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, -1, 3, 4, -1, -1, 5, 6, -1, -1, 7, 8, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, 7, 3, 4, 8, 5, 6, 9, 10, 11, 12, 13, -1, -1, -1, -1, -1, -1, -1},
			new[] {0, 1, 2, 5, 3, 4, 6, -1, -1, -1, -1, -1, -1, -1, 7, 8, 9, -1, -1, -1, -1},
			new[] {0, 1, 2, 7, 3, 4, 8, 5, 6, 9, 10, 11, 12, 13, 14, 15, 16, -1, -1, -1, -1},

			new[] {0, 1, 2, -1, 3, 4, -1, 5, 6, -1, 7, 8, -1, -1, -1, -1, -1, 9, -1, -1, -1},
			new[] {0, 1, 2, 7, 3, 4, 8, 5, 6, 9, 10, 11, 12, 13, 14, 15, 16, 17, -1, -1, -1},
			new[] {0, 1, 2, 5, 3, 4, 6, -1, -1, -1, -1, -1, -1, -1, 7, 8, 9, -1, 10, -1, -1},
			new[] {0, 1, 2, 7, 3, 4, 8, 5, 6, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, -1, -1},
			new[] {0, 1, 2, 7, 3, 4, 8, 5, 6, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20}
		};

		private static readonly ThreadLocal<long> _row = new();
		private readonly int[] _column;
		private readonly int _width;
		private readonly MemoryMappedViewAccessor _view;
		private readonly long _indexBaseAddress;
		private readonly long _baseAddress;

		// TODO: Add CurrentRow property to allow for read methods without row parameter
		// TODO: Find a way to obtain culture
		public IpDatabaseDataReader(MemoryMappedViewAccessor view, int version, long indexBaseAddress, long baseAddress)
		{
			_view = view;
			_indexBaseAddress = indexBaseAddress;
			_baseAddress = baseAddress;
			_column = _columnIndexes[version - 1];
			_width = ((_column.Max() + 1) << 2) + 4;
		}

		public long CurrentRow
		{
			get => _row.Value;
			set => _row.Value = value;
		}

		public long SeekRow(IPAddress ipAddress)
		{
			// Convert IP address to ulong for value comparisons
			var bytes = ipAddress.GetAddressBytes();
			if (bytes.Length != 4) throw new NotSupportedException("Only IPv4 addresses are supported.");
			if (BitConverter.IsLittleEndian) bytes = bytes.Reverse().ToArray();
			var ip = (long) BitConverter.ToUInt32(bytes);

			// Use the two high-order network bytes as an index into the ip range table
			const long maxIp = 4294967295;
			var indexRow = (ip >> 16) * 8;
			if (ip >= maxIp) ip = maxIp - 1;

			// Start with how and low ranges for this ip range index 
			var low = (long) _view.ReadUInt32(_indexBaseAddress + indexRow);
			var high = (long) _view.ReadUInt32(_indexBaseAddress + indexRow + 4);

			// Perform a successive approximation search to identify the ip range that contains the given ip address
			long row = 0;
			while (low <= high)
			{
				row = (long) Math.Round((double) (low + high) / 2, MidpointRounding.ToEven);
				var from = (long) _view.ReadUInt32(_baseAddress + row * _width);
				var to = (long) _view.ReadUInt32(_baseAddress + row * _width + _width);

				if (ip >= from && ip < to) break;
				if (ip < from) high = row - 1;
				else if (ip >= to) low = row + 1;
				else return default;
			}

			return row;
		}

		public string ReadCountryShort() => ReadString(GetRowBytes(_row.Value), GeoFieldName.CountryShort);
		public string ReadCountryLong() => ReadStringReference(ReadInt32(GetRowBytes(_row.Value), GeoFieldName.CountryShort) + 3);
		public string ReadRegion() => ReadString(GetRowBytes(_row.Value), GeoFieldName.Region);
		public string ReadCity() => ReadString(GetRowBytes(_row.Value), GeoFieldName.City);
		public string ReadIsp() => ReadString(GetRowBytes(_row.Value), GeoFieldName.Isp);
		public float? ReadLatitude() => ReadSingle(GetRowBytes(_row.Value), GeoFieldName.Latitude);
		public float? ReadLongitude() => ReadSingle(GetRowBytes(_row.Value), GeoFieldName.Longitude);

		public GeoCoordinate? ReadGeoCoordinate()
		{
			var latitude = ReadLatitude();
			var longitude = ReadLongitude();

			if (latitude is null || longitude is null) return null;
			return new GeoCoordinate(latitude.Value, longitude.Value);
		}

		public string ReadDomain() => ReadString(GetRowBytes(_row.Value), GeoFieldName.Domain);

		public string ReadZipCode() => ReadString(GetRowBytes(_row.Value), GeoFieldName.ZipCode);

		// TODO: Find a way for this to return the actual TimeZoneInfo
		public TimeSpan? ReadTimeZone()
		{
			var offset = ReadString(GetRowBytes(_row.Value), GeoFieldName.TimeZone);
			if (offset is null) return null;
			if (!TimeSpan.TryParse(offset, out var timeZone)) return null;
			return timeZone;
		}

		public decimal? ReadTimeZoneOffset()
		{
			var timeZone = ReadTimeZone();
			if (timeZone == null) return null;

			var offset = (decimal) timeZone.Value.TotalMinutes / 60;
			return offset;
		}

		public string ReadNetSpeed() => ReadString(GetRowBytes(_row.Value), GeoFieldName.NetSpeed);
		public string ReadIddCode() => ReadString(GetRowBytes(_row.Value), GeoFieldName.IddCode);
		public string ReadAreaCode() => ReadString(GetRowBytes(_row.Value), GeoFieldName.AreaCode);
		public string ReadWeatherStationCode() => ReadString(GetRowBytes(_row.Value), GeoFieldName.WeatherStationCode);
		public string ReadWeatherStationName() => ReadString(GetRowBytes(_row.Value), GeoFieldName.WeatherStationName);
		public string ReadMcc() => ReadString(GetRowBytes(_row.Value), GeoFieldName.Mcc);
		public string ReadMnc() => ReadString(GetRowBytes(_row.Value), GeoFieldName.Mnc);
		public string ReadMobileBrand() => ReadString(GetRowBytes(_row.Value), GeoFieldName.MobileBrand);
		public float? ReadElevation() => ReadSingle(GetRowBytes(_row.Value), GeoFieldName.Elevation);
		public string ReadUsageType() => ReadString(GetRowBytes(_row.Value), GeoFieldName.UsageType);
		public string ReadAddressType() => ReadString(GetRowBytes(_row.Value), GeoFieldName.AddressType);
		public string ReadCategory() => ReadString(GetRowBytes(_row.Value), GeoFieldName.Category);

		private byte[] GetRowBytes(long row)
		{
			var bytes = new byte[_width - 4];
			_view.ReadArray(row * _width + _baseAddress + 4, bytes, 0, _width - 4);

			return bytes;
		}

		private int? GetRowOffset(GeoFieldName field)
		{
			var column = (int) field - 1;
			if (column >= _column.Length) return default;

			var unshift = _column[column];
			if (unshift < 0) return default;

			return unshift << 2;
		}

		private int? ReadInt32(byte[] row, GeoFieldName field)
		{
			var offset = GetRowOffset(field);
			return offset.HasValue ? BitConverter.ToInt32(row, offset.Value) : default;
		}

		private float? ReadSingle(byte[] row, GeoFieldName field)
		{
			var offset = GetRowOffset(field);
			return offset.HasValue ? BitConverter.ToSingle(row, offset.Value) : default;
		}

		private string ReadString(byte[] row, GeoFieldName field)
		{
			var offset = GetRowOffset(field);
			if (!offset.HasValue) return null;

			var index = BitConverter.ToInt32(row, offset.Value);
			if (index <= 0) return null;

			var length = _view.ReadByte(index);
			var bytes = new byte[length];
			_view.ReadArray(index + 1, bytes, 0, length);

			var value = Encoding.Default.GetString(bytes);
			return value;
		}

		private string ReadStringReference(int? index)
		{
			if (index is not > 0) return null;

			var length = _view.ReadByte(index.Value);
			var bytes = new byte[length];
			_view.ReadArray(index.Value + 1, bytes, 0, length);

			var value = Encoding.Default.GetString(bytes);
			return value;
		}
	}
}
