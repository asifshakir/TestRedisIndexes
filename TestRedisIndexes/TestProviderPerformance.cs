
using Bogus;
using System.Diagnostics;
using System.Text.Json;

public class TestProviderPerformance(IRedisProvider provider)
{
    const string domain = "MYDOMAIN";

    public void Test(int count)
    {
        var stopwatch = Stopwatch.StartNew();
        
        stopwatch.Restart();
        var maintenanceItems = Generate(count);
        stopwatch.Stop();
        Console.WriteLine($"Generated {count} maintenance items in {stopwatch.Elapsed} time");

        stopwatch.Restart();
        provider.Save(maintenanceItems);
        stopwatch.Stop();
        Console.WriteLine($"Submit {count} items to Redis cache using Option 1 in {stopwatch.Elapsed} time");

        stopwatch.Restart();
        var clientCodes = maintenanceItems.Select(x => x.ClientCode).Distinct();
        Dictionary<string, List<Maintenance>> responses = [];

        foreach (var clientCode in clientCodes)
        {
            var items = provider.Get(domain, clientCode);
            if (!responses.ContainsKey(clientCode))
            {
                responses.Add(clientCode, []);
            }
            responses[clientCode].AddRange(items);
        }
        stopwatch.Stop();
        Console.WriteLine($"Fetched data for {clientCodes.Count()} clients from Redis cache using Option 1 in {stopwatch.Elapsed} time");

        stopwatch.Restart();
        foreach (var key in responses.Keys)
        {
            JsonSerializer.Serialize(responses[key]);
        }
        stopwatch.Stop();
        Console.WriteLine($"Serialized responses using Option 1 in {stopwatch.Elapsed} time");

        stopwatch.Restart();
        List<Maintenance> allItems = [];
        allItems = provider.GetAll().ToList();
        stopwatch.Stop();
        Console.WriteLine($"Fetched all one time from Redis cache using Option 1 in {stopwatch.Elapsed} time");

        stopwatch.Restart();
        var json = JsonSerializer.Serialize(allItems);
        stopwatch.Stop();
        Console.WriteLine($"Serialized one set of all responses using Option 1 in {stopwatch.Elapsed} time");
    }

    private IEnumerable<Maintenance> Generate(int count)
    {
        var faker = new Faker();

        int totalClients = (int)(count * 0.3);
        if (totalClients == 0) totalClients = count;

        string[] environments = ["PRD", "UAT", "DEV"];

        for (int i = 0; i < count; i++)
        {
            int clientCode = new Random().Next(totalClients - 1) + 1;
            int environment = new Random().Next(environments.Length - 1);

            yield return new Maintenance
            {
                Domain = domain,
                ClientCode = $"Client {clientCode}",
                Environment = environments[environment],
                StartTime = DateTimeOffset.UtcNow,
                ExpiryTime = DateTimeOffset.UtcNow.AddHours(2),
                Message = faker.Lorem.Paragraph(1)
            };
        }
    }

}