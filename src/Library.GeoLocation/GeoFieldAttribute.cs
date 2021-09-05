using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public sealed class GeoFieldAttribute : Attribute
	{
		public GeoFieldAttribute(GeoFieldName geoFieldName)
		{
			GeoFieldName = geoFieldName;
		}

		public GeoFieldName GeoFieldName { get; }
	}
}