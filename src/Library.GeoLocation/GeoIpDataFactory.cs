using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	internal class GeoIpDataFactory : IIpDatabaseDataFactory<GeoIpData>
	{
		public GeoIpData Read(IIpDatabaseDataReader reader)
		{
			var data = new GeoIpData
			{
				CountryShort = reader.ReadCountryShort(),
				CountryLong = reader.ReadCountryLong(),
				Region = reader.ReadRegion(),
				City = reader.ReadCity(),
				Isp = reader.ReadIsp(),
				Latitude = reader.ReadLatitude(),
				Longitude = reader.ReadLongitude(),
				GeoCoordinate = reader.ReadGeoCoordinate(),
				Domain = reader.ReadDomain(),
				ZipCode = reader.ReadZipCode(),
				TimeZoneOffset = reader.ReadTimeZone(),
				NetSpeed = reader.ReadNetSpeed(),
				IddCode = reader.ReadIddCode(),
				AreaCode = reader.ReadAreaCode(),
				WeatherStationCode = reader.ReadWeatherStationCode(),
				WeatherStationName = reader.ReadWeatherStationName(),
				Mcc = reader.ReadMcc(),
				Mnc = reader.ReadMnc(),
				MobileBrand = reader.ReadMobileBrand(),
				Elevation = reader.ReadElevation(),
				UsageType = reader.ReadUsageType(),
				AddressType = reader.ReadAddressType(),
				Category = reader.ReadCategory()
			};

			return data;
		}
	}
}