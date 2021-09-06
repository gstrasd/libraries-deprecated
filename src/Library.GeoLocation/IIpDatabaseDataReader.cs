using System;
using System.Net;

namespace Library.GeoLocation
{
	public interface IIpDatabaseDataReader
	{
		string ReadCountryShort();
		string ReadCountryLong();
		string ReadRegion();
		string ReadCity();
		string ReadIsp();
		float? ReadLatitude();
		float? ReadLongitude();
		GeoCoordinate? ReadGeoCoordinate();
		string ReadDomain();
		string ReadZipCode();
		TimeSpan? ReadTimeZone();
		decimal? ReadTimeZoneOffset();
		string ReadNetSpeed();
		string ReadIddCode();
		string ReadAreaCode();
		string ReadWeatherStationCode();
		string ReadWeatherStationName();
		string ReadMcc();
		string ReadMnc();
		string ReadMobileBrand();
		float? ReadElevation();
		string ReadUsageType();
		string ReadAddressType();
		string ReadCategory();
	}
}