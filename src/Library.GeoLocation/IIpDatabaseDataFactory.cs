using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	public interface IIpDatabaseDataFactory<out T>
	{
		T Read(IIpDatabaseDataReader reader, long row);
	}
}