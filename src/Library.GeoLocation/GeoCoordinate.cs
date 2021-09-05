using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.GeoLocation
{
	public readonly struct GeoCoordinate
	{
		private const double _tolerance = Double.Epsilon * 2;

		public GeoCoordinate(double latitude, double longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
		}

		public double Latitude { get; }

		public double Longitude { get; }

		public static readonly GeoCoordinate Default = new(0d, 0d);

		public override bool Equals(object? obj)
		{
			return obj is GeoCoordinate other && Equals(other);
		}

		public bool Equals(GeoCoordinate other)
		{
			return Math.Abs(Latitude - other.Latitude) < _tolerance && Math.Abs(Longitude - other.Longitude) < _tolerance;
		}

		public static bool operator ==(GeoCoordinate left, GeoCoordinate right) => left.Equals(right);

		public static bool operator !=(GeoCoordinate left, GeoCoordinate right) => !left.Equals(right);

		public override int GetHashCode()
		{
			return HashCode.Combine(Latitude, Longitude);
		}
	}
}
