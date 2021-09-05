using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	public sealed class GeoIpDatabaseReader : IpDatabaseReader<GeoIpData>
	{
		public GeoIpDatabaseReader(IpDatabase database) : base(database, new GeoIpDatabaseDataFactory())
		{
		}
	}
}