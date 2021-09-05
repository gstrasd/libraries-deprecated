using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Library.GeoLocation.GeoFieldName;

namespace Library.GeoLocation.Tests
{
    public class IpDatabaseReaderTests
    {
	    private readonly ITestOutputHelper _output;

	    public IpDatabaseReaderTests(ITestOutputHelper output)
	    {
		    _output = output;
	    }

        [Theory]
		[InlineData("67.167.127.76")]
		[InlineData("63.78.118.218")]
        [InlineData("192.252.201.210")]
        [InlineData("174.208.229.38")]
		public async Task CanLocateIpAddress(string ipAddress)
		{
			var db = IpDatabase.Open(@"C:\GitHub\libraries\src\Tests\Library.GeoLocation.Tests\data\IpDatabaseReaderTests\IP2LOCATION-LITE-DB11.BIN");
			var reader = new IpDatabaseReader(db);
			var foo = await reader.ReadAsync<GeoData>(ipAddress);
			_output.WriteLine(foo.ToString());
			db.Dispose();
		}
    }

    public record GeoData
    {
		[GeoField(CountryShort)]
		public string Country { get; set; }
		[GeoField(CountryLong)]
		public string CountryName { get; set; }
		public string Region { get; set; }
		public string City { get; set; }
		public float? Latitude { get; set; }
		public float? Longitude { get; set; }
		[GeoField(GeoFieldName.GeoCoordinate)]
		public GeoCoordinate? Coordinates { get; set; }
		public string ZipCode { get; set; }
		public TimeSpan? TimeZone { get; set; }
    }
}
