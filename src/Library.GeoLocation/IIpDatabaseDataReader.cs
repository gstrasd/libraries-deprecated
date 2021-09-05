using System;
using System.Net;

namespace Library.GeoLocation
{
	public interface IIpDatabaseDataReader
	{
		long SeekRow(IPAddress ipAddress);
		string ReadCountryShort(long row);
		string ReadCountryLong(long row);
		string ReadRegion(long row);
		string ReadCity(long row);
		string ReadIsp(long row);
		float? ReadLatitude(long row);
		float? ReadLongitude(long row);
		GeoCoordinate? ReadGeoCoordinate(long row);
		string ReadDomain(long row);
		string ReadZipCode(long row);
		TimeSpan? ReadTimeZoneOffset(long row);
		string ReadNetSpeed(long row);
		string ReadIddCode(long row);
		string ReadAreaCode(long row);
		string ReadWeatherStationCode(long row);
		string ReadWeatherStationName(long row);
		string ReadMcc(long row);
		string ReadMnc(long row);
		string ReadMobileBrand(long row);
		float? ReadElevation(long row);
		string ReadUsageType(long row);
		string ReadAddressType(long row);
		string ReadCategory(long row);
	}
}