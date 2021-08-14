using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Library.Configuration;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Library.Redis
{
    public static class ConfigurationOptionsExtensions
    {
        public static void Configure(this RedisCacheOptions options, IConfiguration configuration)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var redis = configuration.GetSection("Redis");
            if (redis.Exists()) configuration = redis;

            options.ConfigurationOptions = new ConfigurationOptions();
            configuration.Bind(options.ConfigurationOptions);

            // Get endpoints
            var endpoints = configuration.GetSection("ConnectionEndpoints").Get<List<string>>();
            if (endpoints == default) throw new Exception("No connection endpoints were found.");
            endpoints.ForEach(e => options.ConfigurationOptions.EndPoints.Add(e));

            // Get channel prefix
            var channelPrefix = configuration["ChannelPrefix:Name"];
            if (channelPrefix != default)
            {
                var patternMode = configuration.GetValue<RedisChannel.PatternMode>("ChannelPrefix:PatternMode");
                options.ConfigurationOptions.ChannelPrefix = new RedisChannel(channelPrefix, patternMode);
            }

            // Get SSL protocol
            var sslProtocolList = configuration.GetSection("SslProtocols").Get<List<string>>();
            if (sslProtocolList != null)
            {
                var sslProtocols = SslProtocols.None;
                if (sslProtocolList.Any())
                {
                    sslProtocols = sslProtocolList.Aggregate(SslProtocols.None, (acc, ssl) => acc | Enum.Parse<SslProtocols>(ssl));
                }
                options.ConfigurationOptions.SslProtocols = sslProtocols;
            }

            // Get reconnect retry policy
            if (Int32.TryParse(configuration["ReconnectRetryPolicy:Linear:MaxRetryElapsedTimeAllowedMilliseconds"], out var maxRetryElapsedTimeAllowedMilliseconds))
            {
                options.ConfigurationOptions.ReconnectRetryPolicy = new LinearRetry(maxRetryElapsedTimeAllowedMilliseconds);
            }
            else if (Int32.TryParse(configuration["ReconnectRetryPolicy:Exponential:DeltaBackOffMilliseconds"], out var deltaBackOffMilliseconds))
            {
                if (Int32.TryParse(configuration["ReconnectRetryPolicy:Exponential:MaxDeltaBackOffMilliseconds"], out var maxDeltaBackOffMilliseconds))
                {
                    options.ConfigurationOptions.ReconnectRetryPolicy = new ExponentialRetry(deltaBackOffMilliseconds, maxDeltaBackOffMilliseconds);
                }
                else
                {
                    options.ConfigurationOptions.ReconnectRetryPolicy = new ExponentialRetry(deltaBackOffMilliseconds);
                }
            }

            // Get socket manager
            var socketManagerName = configuration["Sockets:Manager"];
            if (socketManagerName != default)
            {
                var useHighPrioritySocketThreads = configuration.GetValue<bool>("Sockets:UseHighPrioritySocketThreads");
                options.ConfigurationOptions.SocketManager = new SocketManager(socketManagerName, useHighPrioritySocketThreads);
            }
        }
    }
}
