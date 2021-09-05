using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	internal sealed class IpDatabaseDataReader : IIpDatabaseDataReader
	{
		private static readonly int[][] _columnIndexes =
		{
			new [] { 0,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0,-1,-1, 1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2,-1, 3, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },

			new [] { 0, 1, 2, 5, 3, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2, 3,-1,-1, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2, 5, 3, 4, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2,-1, 3, 4,-1, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2, 6, 3, 4, 7, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },

			new [] { 0, 1, 2,-1, 3, 4,-1, 5, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2, 7, 3, 4, 8, 5, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2,-1, 3, 4,-1,-1, 5, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2, 7, 3, 4, 8, 5, 6, 9,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2,-1, 3, 4,-1, 5, 6,-1, 7, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1 },

			new [] { 0, 1, 2, 7, 3, 4, 8, 5, 6, 9,10,11,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2,-1, 3, 4,-1,-1, 5, 6,-1,-1, 7, 8,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2, 7, 3, 4, 8, 5, 6, 9,10,11,12,13,-1,-1,-1,-1,-1,-1,-1 },
			new [] { 0, 1, 2, 5, 3, 4, 6,-1,-1,-1,-1,-1,-1,-1, 7, 8, 9,-1,-1,-1,-1 },
			new [] { 0, 1, 2, 7, 3, 4, 8, 5, 6, 9,10,11,12,13,14,15,16,-1,-1,-1,-1 },

			new [] { 0, 1, 2,-1, 3, 4,-1, 5, 6,-1, 7, 8,-1,-1,-1,-1,-1, 9,-1,-1,-1 },
			new [] { 0, 1, 2, 7, 3, 4, 8, 5, 6, 9,10,11,12,13,14,15,16,17,-1,-1,-1 },
			new [] { 0, 1, 2, 5, 3, 4, 6,-1,-1,-1,-1,-1,-1,-1, 7, 8, 9,-1,10,-1,-1 },
			new [] { 0, 1, 2, 7, 3, 4, 8, 5, 6, 9,10,11,12,13,14,15,16,17,18,-1,-1 },
			new [] { 0, 1, 2, 7, 3, 4, 8, 5, 6, 9,10,11,12,13,14,15,16,17,18,19,20 }
		};
		private readonly int[] _column;
		private readonly int _width;
		private readonly MemoryMappedViewAccessor _view;
		private readonly long _indexBaseAddress;
		private readonly long _baseAddress;

		public IpDatabaseDataReader(MemoryMappedViewAccessor view, int version, long indexBaseAddress, long baseAddress)
		{
			_view = view;
			_indexBaseAddress = indexBaseAddress;
			_baseAddress = baseAddress;
			_column = _columnIndexes[version - 1];
			_width = ((_column.Max() + 1) << 2) + 4;
		}

		public long SeekRow(IPAddress ipAddress)
		{
			// Convert IP address to ulong for value comparisons
			var bytes = ipAddress.GetAddressBytes();
			if (bytes.Length != 4) throw new NotSupportedException("Only IPv4 addresses are supported.");
			if (BitConverter.IsLittleEndian) bytes = bytes.Reverse().ToArray();
			var ip = (long)BitConverter.ToUInt32(bytes);

			// Use the two high-order network bytes as an index into the ip range table
			const long maxIp = 4294967295;
			var indexRow = (ip >> 16) * 8;
			if (ip >= maxIp) ip = maxIp - 1;

			// Start with how and low ranges for this ip range index 
			var low = (long)_view.ReadUInt32(_indexBaseAddress + indexRow);
			var high = (long)_view.ReadUInt32(_indexBaseAddress + indexRow + 4);

			// Perform a successive approximation search to identify the ip range that contains the given ip address
			long row = 0;
			while (low <= high)
			{
				row = (long)Math.Round((double)(low + high) / 2, MidpointRounding.ToEven);
				var from = (long)_view.ReadUInt32(_baseAddress + row * _width);
				var to = (long)_view.ReadUInt32(_baseAddress + row * _width + _width);

				if (ip >= from && ip < to) break;
				if (ip < from) high = row - 1;
				else if (ip >= to) low = row + 1;
				else return default;
			}

			return row;
		}

		public string ReadCountryShort(long row) => ReadString(GetRowBytes(row), GeoFieldName.CountryShort);
		public string ReadCountryLong(long row) => ReadStringReference(ReadInt32(GetRowBytes(row), GeoFieldName.CountryShort) + 3);
		public string ReadRegion(long row) => ReadString(GetRowBytes(row), GeoFieldName.Region);
		public string ReadCity(long row) => ReadString(GetRowBytes(row), GeoFieldName.City);
		public string ReadIsp(long row) => ReadString(GetRowBytes(row), GeoFieldName.Isp);
		public float? ReadLatitude(long row) => ReadSingle(GetRowBytes(row), GeoFieldName.Latitude);
		public float? ReadLongitude(long row) => ReadSingle(GetRowBytes(row), GeoFieldName.Longitude);
		public GeoCoordinate? ReadGeoCoordinate(long row)
		{
			var latitude = ReadLatitude(row);
			var longitude = ReadLongitude(row);

			if (latitude is null || longitude is null) return null;
			return new GeoCoordinate(latitude.Value, longitude.Value);
		}
		public string ReadDomain(long row) => ReadString(GetRowBytes(row), GeoFieldName.Domain);
		public string ReadZipCode(long row) => ReadString(GetRowBytes(row), GeoFieldName.ZipCode);
		public TimeSpan? ReadTimeZoneOffset(long row)
		{
			var offset = ReadString(GetRowBytes(row), GeoFieldName.TimeZone);
			if (offset is null) return null;
			if (!TimeSpan.TryParse(offset, out var timeZone)) return null;
			return timeZone;
		}
		public string ReadNetSpeed(long row) => ReadString(GetRowBytes(row), GeoFieldName.NetSpeed);
		public string ReadIddCode(long row) => ReadString(GetRowBytes(row), GeoFieldName.IddCode);
		public string ReadAreaCode(long row) => ReadString(GetRowBytes(row), GeoFieldName.AreaCode);
		public string ReadWeatherStationCode(long row) => ReadString(GetRowBytes(row), GeoFieldName.WeatherStationCode);
		public string ReadWeatherStationName(long row) => ReadString(GetRowBytes(row), GeoFieldName.WeatherStationName);
		public string ReadMcc(long row) => ReadString(GetRowBytes(row), GeoFieldName.Mcc);
		public string ReadMnc(long row) => ReadString(GetRowBytes(row), GeoFieldName.Mnc);
		public string ReadMobileBrand(long row) => ReadString(GetRowBytes(row), GeoFieldName.MobileBrand);
		public float? ReadElevation(long row) => ReadSingle(GetRowBytes(row), GeoFieldName.Elevation);
		public string ReadUsageType(long row) => ReadString(GetRowBytes(row), GeoFieldName.UsageType);
		public string ReadAddressType(long row) => ReadString(GetRowBytes(row), GeoFieldName.AddressType);
		public string ReadCategory(long row) => ReadString(GetRowBytes(row), GeoFieldName.Category);

		private byte[] GetRowBytes(long row)
		{
			var bytes = new byte[_width - 4];
			_view.ReadArray(row * _width + _baseAddress + 4, bytes, 0, _width - 4);

			return bytes;
		}

		private int? GetRowOffset(GeoFieldName field)
		{
			var column = (int)field - 1;
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

		//private int ReadInt32(byte[] row, GeoFieldName field)
		//{
		//	var column = (int)field - 1;
		//	if (column >= _column.Length) return default;

		//	var offset = _column[column] << 2;
		//	return BitConverter.ToInt32(row, offset);
		//}

		private float? ReadSingle(byte[] row, GeoFieldName field)
		{
			var offset = GetRowOffset(field);
			return offset.HasValue ? BitConverter.ToSingle(row, offset.Value) : default;
		}

		//private float ReadSingle(byte[] row, GeoFieldName field)
		//{
		//	var column = (int)field - 1;
		//	if (column >= _column.Length) return default;

		//	var offset = _column[column] << 2;
		//	return BitConverter.ToSingle(row, offset);
		//}

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

		//private string ReadString(byte[] row, GeoFieldName field)
		//{
		//	var column = (int)field - 1;
		//	if (column >= _column.Length) return default;

		//	var columnIndex = _column[column];
		//	if (columnIndex < 0) return null;

		//	var offset = _column[column] << 2;
		//	var index = BitConverter.ToInt32(row, offset);
		//	if (index <= 0) return null;

		//	var length = _view.ReadByte(index);
		//	var bytes = new byte[length];
		//	_view.ReadArray(index + 1, bytes, 0, length);

		//	var value = Encoding.Default.GetString(bytes);
		//	return value;
		//}

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