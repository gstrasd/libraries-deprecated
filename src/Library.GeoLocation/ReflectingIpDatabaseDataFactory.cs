﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Library.GeoLocation.GeoFieldName;

namespace Library.GeoLocation
{
	internal class ReflectingIpDatabaseDataFactory
	{
		private static readonly ConcurrentDictionary<Type, List<(GeoFieldName Field, PropertyInfo Property)>> _types = new();
		private readonly IPAddress _ipAddress;

		public ReflectingIpDatabaseDataFactory(IPAddress ipAddress)
		{
			_ipAddress = ipAddress;
		}

		public async Task<T> ReadAsync<T>(IIpDatabaseDataReader reader, long row) where T : new()
		{
			var properties = await GetTypePropertiesAsync<T>();
			var value = new T();
			
			foreach (var (field, property) in properties)
			{
				switch (field)
				{
					case Unspecified: continue;
					case IpAddress: property.SetValue(value, _ipAddress); continue;
					case CountryShort: property.SetValue(value, reader.ReadCountryShort(row)); continue;
					case CountryLong: property.SetValue(value, reader.ReadCountryLong(row)); continue;
					case Region: property.SetValue(value, reader.ReadRegion(row)); continue;
					case City: property.SetValue(value, reader.ReadCity(row)); continue;
					case Isp: property.SetValue(value, reader.ReadIsp(row)); continue;
					case Latitude: property.SetValue(value, reader.ReadLatitude(row)); continue;
					case Longitude: property.SetValue(value, reader.ReadLongitude(row)); continue;
					case GeoFieldName.GeoCoordinate: property.SetValue(value, reader.ReadGeoCoordinate(row)); continue;
					case Domain: property.SetValue(value, reader.ReadDomain(row)); continue;
					case ZipCode: property.SetValue(value, reader.ReadZipCode(row)); continue;
					case GeoFieldName.TimeZone: property.SetValue(value, reader.ReadTimeZoneOffset(row)); continue;
					case NetSpeed: property.SetValue(value, reader.ReadNetSpeed(row)); continue;
					case IddCode: property.SetValue(value, reader.ReadIddCode(row)); continue;
					case AreaCode: property.SetValue(value, reader.ReadAreaCode(row)); continue;
					case WeatherStationCode: property.SetValue(value, reader.ReadWeatherStationCode(row)); continue;
					case WeatherStationName: property.SetValue(value, reader.ReadWeatherStationName(row)); continue;
					case Mcc: property.SetValue(value, reader.ReadMcc(row)); continue;
					case Mnc: property.SetValue(value, reader.ReadMnc(row)); continue;
					case MobileBrand: property.SetValue(value, reader.ReadMobileBrand(row)); continue;
					case Elevation: property.SetValue(value, reader.ReadElevation(row)); continue;
					case UsageType: property.SetValue(value, reader.ReadUsageType(row)); continue;
					case AddressType: property.SetValue(value, reader.ReadAddressType(row)); continue;
					case Category: property.SetValue(value, reader.ReadCategory(row)); continue;
					default: continue;
				}
			}

			return value;
		}

		private static Task<List<(GeoFieldName Field, PropertyInfo Property)>> GetTypePropertiesAsync<T>()
		{
			var type = typeof(T);
			if (_types.TryGetValue(type, out var properties)) return Task.FromResult(properties);

			// Get all type properties
			properties = (
					from p in type.GetProperties()
					let a = p.GetCustomAttribute<GeoFieldAttribute>()
					join pf in Enum.GetValues<GeoFieldName>() on p.Name equals pf.ToString() into pfs
					from pfn in pfs.DefaultIfEmpty()
					join af in Enum.GetValues<GeoFieldName>() on a?.GeoFieldName equals af into afs
					from afn in afs.DefaultIfEmpty()
					select (afn != default ? afn : pfn, p))
				.ToList();

			// Validate each property type
			foreach (var (field, property) in properties)
			{
				switch (field)
				{
					case Unspecified: continue;

					case IpAddress:
						if (property.PropertyType != typeof(IPAddress)) throw new InvalidCastException($"Property {property.Name} must be an {nameof(IPAddress)}");
						continue;

					case Latitude:
					case Longitude:
						if (property.PropertyType != typeof(float?)) throw new InvalidCastException($"Property {property.Name} must be a nullable {nameof(Single)}");
						continue;

					case GeoFieldName.GeoCoordinate:
						if (property.PropertyType != typeof(GeoCoordinate?)) throw new InvalidCastException($"Property {property.Name} must be a nullable {nameof(GeoCoordinate)}");
						continue;

					case GeoFieldName.TimeZone:
						if (property.PropertyType != typeof(TimeSpan?)) throw new InvalidCastException($"Property {property.Name} must be a nullable {nameof(TimeSpan)}");
						continue;

					default:
						if (property.PropertyType != typeof(string)) throw new InvalidCastException($"Property {property.Name} must be a {nameof(String)}");
						continue;
				}
			}

			_types.TryAdd(type, properties);

			return Task.FromResult(properties);
		}
	}
}
