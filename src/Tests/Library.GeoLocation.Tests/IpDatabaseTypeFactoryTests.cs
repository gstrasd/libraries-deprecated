using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Library.GeoLocation.Tests
{
	public class IpDatabaseTypeFactoryTests
	{
		private readonly ITestOutputHelper _output;

		public IpDatabaseTypeFactoryTests(ITestOutputHelper output)
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
			var db = IpDatabase.Open(@"C:\GitHub\libraries\src\Tests\Library.GeoLocation.Tests\data\IpDatabaseTypeFactoryTests\IP2LOCATION-LITE-DB11.BIN");
			var reader = new GeoIpDatabaseReader(db);
			var foo = await reader.ReadAsync(ipAddress);
			_output.WriteLine(foo.ToString());
			db.Dispose();
		}
	}
}