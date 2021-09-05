using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	public record GeoIpData
	{
		public string CountryShort { get; set; }
		public string CountryLong { get; set; }
		public string Region { get; set; }
		public string City { get; set; }
		public string Isp { get; set; }
		public float? Latitude { get; set; }
		public float? Longitude { get; set; }
		public GeoCoordinate? GeoCoordinate { get; set; }
		public string Domain { get; set; }
		public string ZipCode { get; set; }
		public TimeSpan? TimeZoneOffset { get; set; }
		public string NetSpeed { get; set; }
		public string IddCode { get; set; }
		public string AreaCode { get; set; }
		public string WeatherStationCode { get; set; }
		public string WeatherStationName { get; set; }
		public string Mcc { get; set; }
		public string Mnc { get; set; }
		public string MobileBrand { get; set; }
		public float? Elevation { get; set; }
		public string UsageType { get; set; }
		public string AddressType { get; set; }
		public string Category { get; set; }
	}
}
