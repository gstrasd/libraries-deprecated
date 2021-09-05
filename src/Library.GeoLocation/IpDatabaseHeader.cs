using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	internal sealed class IpDatabaseHeader
	{
		public byte DbType { get; set; }
		public byte DbColumn { get; set; }
		public byte DbYear { get; set; }
		public byte DbMonth { get; set; }
		public byte DbDay { get; set; }
		public int DbCount { get; set; }
		public int BaseAddress { get; set; }
		public int DbCountIPv6 { get; set; }
		public int BaseAddressIPv6 { get; set; }
		public int IndexedBaseAddress { get; set; }
		public int IndexBaseAddressIPv6 { get; set; }
		public byte ProductCode { get; set; }
		public byte ProductType { get; set; }
		public int FileSize { get; set; }
	}
}