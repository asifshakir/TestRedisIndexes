using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Diagnostics;
using System.Net;

var builder = new ConfigurationBuilder()
            .AddUserSecrets<Program>();
var configuration = builder.Build();

string connectionString = configuration.GetSection("CacheConnection").Value ?? throw new ArgumentNullException("ConnectionString Not Found");

var multiplexer = ConnectionMultiplexer.Connect(connectionString);
var db = multiplexer.GetDatabase();

var options = ConfigurationOptions.Parse(connectionString);
var endpoint = options.EndPoints.First();
string serverAndPort = "";
if (endpoint is IPEndPoint ipEndPoint)
{
    serverAndPort = $"{ipEndPoint.Address}:{ipEndPoint.Port}";
}
else if (endpoint is DnsEndPoint dnsEndPoint)
{
    serverAndPort = $"{dnsEndPoint.Host}:{dnsEndPoint.Port}";
}

Stopwatch stopwatch = Stopwatch.StartNew();
multiplexer.GetServer(serverAndPort).FlushDatabase();
stopwatch.Stop();
Console.WriteLine($"Flushed DB in {stopwatch.Elapsed} time");

Console.WriteLine("------- OPTION 1 --------------");
IRedisProvider provider1 = new RedisProviderOption1(db);
TestProviderPerformance testOption1 = new(provider1);
testOption1.Test(100);

Console.WriteLine("------- OPTION 2 --------------");
IRedisProvider provider2 = new RedisProviderOption2(db);
TestProviderPerformance testOption2 = new(provider2);
testOption2.Test(100);

Console.WriteLine("------- OPTION 3 --------------");
IRedisProvider provider3 = new RedisProviderOption3(db);
TestProviderPerformance testOption3 = new(provider3);
testOption3.Test(100);

multiplexer.Dispose();