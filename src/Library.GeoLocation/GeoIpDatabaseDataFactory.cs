using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	internal class GeoIpDatabaseDataFactory : IIpDatabaseDataFactory<GeoIpData>
	{
		public GeoIpData Read(IIpDatabaseDataReader reader, long row)
		{
			var data = new GeoIpData
			{
				CountryShort = reader.ReadCountryShort(row),
				CountryLong = reader.ReadCountryLong(row),
				Region = reader.ReadRegion(row),
				City = reader.ReadCity(row),
				Isp = reader.ReadIsp(row),
				Latitude = reader.ReadLatitude(row),
				Longitude = reader.ReadLongitude(row),
				GeoCoordinate = reader.ReadGeoCoordinate(row),
				Domain = reader.ReadDomain(row),
				ZipCode = reader.ReadZipCode(row),
				TimeZoneOffset = reader.ReadTimeZoneOffset(row),
				NetSpeed = reader.ReadNetSpeed(row),
				IddCode = reader.ReadIddCode(row),
				AreaCode = reader.ReadAreaCode(row),
				WeatherStationCode = reader.ReadWeatherStationCode(row),
				WeatherStationName = reader.ReadWeatherStationName(row),
				Mcc = reader.ReadMcc(row),
				Mnc = reader.ReadMnc(row),
				MobileBrand = reader.ReadMobileBrand(row),
				Elevation = reader.ReadElevation(row),
				UsageType = reader.ReadUsageType(row),
				AddressType = reader.ReadAddressType(row),
				Category = reader.ReadCategory(row)
			};

			return data;
		}
	}
}