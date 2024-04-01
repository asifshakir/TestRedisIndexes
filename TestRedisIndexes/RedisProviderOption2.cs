using StackExchange.Redis;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Security;
using System.Text.Json;

public class RedisProviderOption2(IDatabase db) : IRedisProvider
{
    public void Save(IEnumerable<Maintenance> maintenanceItems)
    {
        var primaryKey = $"maintenance2";
        Dictionary<string, Dictionary<string, Maintenance>> map = [];
        var currentValues = db.HashGetAll(primaryKey);
        foreach(var currentValue in currentValues)
        {
            var clientKey = currentValue.Name.ToString();
            var json = currentValue.Value.ToString();
            var maintenance = JsonSerializer.Deserialize<Maintenance>(json);
            if (maintenance == null) continue;
            if(!map.ContainsKey(clientKey)) { map.Add(clientKey, []); }
            if(!map[clientKey].TryAdd(maintenance.Environment, maintenance))
            {
                map[clientKey][maintenance.Environment] = maintenance;
            }
        }
        IBatch batch = db.CreateBatch();
        foreach (var clientItem in maintenanceItems)
        {
            string clientKey = $"{clientItem.Domain}:{clientItem.ClientCode}";
            if (!map.ContainsKey(clientKey))
            {
                List<Maintenance> itemList = [clientItem];
                var json = JsonSerializer.Serialize(itemList);
                HashEntry[] entries = [new(clientKey, json)];
                batch.HashSetAsync("maintenance2", entries);
            } else
            {
                Dictionary<string, Maintenance> itemMap = map[clientKey];
                if(!itemMap.TryAdd(clientItem.Domain, clientItem))
                {
                    itemMap[clientItem.Domain] = clientItem;
                }
                var json = JsonSerializer.Serialize(itemMap.Values);
                HashEntry[] entries = [new(clientKey, json)];
                batch.HashSetAsync("maintenance2", entries);
            }
        }
        batch.Execute();
    }

    public IEnumerable<Maintenance> Get(string domain, string clientCode)
    {
        var key = $"maintenance2";
        var hashKey = $"{domain}:{clientCode}";
        Stopwatch stopwatch1 = Stopwatch.StartNew();
        string? hashValue = db.HashGetAsync(key, hashKey).Result;
        stopwatch1.Stop();
        //Console.WriteLine($"Hash Set fetch in {stopwatch1.Elapsed}");
        if(hashValue == null) return [];
        IEnumerable<Maintenance>? maintenances = JsonSerializer.Deserialize<IEnumerable<Maintenance>>(hashValue);
        if (maintenances == null) return [];
        return maintenances;
    }

    public IEnumerable<Maintenance> GetAll()
    {
        var key = $"maintenance2";
        var allValues = db.HashGetAll(key);
        List<Maintenance> results = [];
        foreach (var item in allValues)
        {
            string? json = item.Value;
            if (json == null) continue;
            var temp = JsonSerializer.Deserialize<IEnumerable<Maintenance>>(json);
            if(temp == null) continue;
            results.AddRange(temp);
        }
        return results;
    }
}
