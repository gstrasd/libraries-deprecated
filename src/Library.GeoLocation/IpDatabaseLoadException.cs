using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	public class IpDatabaseLoadException : Exception
	{
		public IpDatabaseLoadException(string message) : base(message)
		{
		}

		public IpDatabaseLoadException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}