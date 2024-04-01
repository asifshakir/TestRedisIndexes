using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

public class RedisProviderOption3(IDatabase db) : IRedisProvider
{
    public IEnumerable<Maintenance> Get(string domain, string clientCode)
    {
        string key = $"maintenance3:{domain}:{clientCode}";
        return GetByKey(key) ;
    }

    private IEnumerable<Maintenance> GetByKey(string key)
    {
        string? json = db.StringGet(key);
        if (json == null) return [];
        var environments = JsonSerializer.Deserialize<IEnumerable<Maintenance>>(json);
        return environments ?? [];
    }

    public IEnumerable<Maintenance> GetAll()
    {
        string allKeys = $"maintenance3";
        RedisValue[] keys = db.SetMembers(allKeys);
        if (keys != null)
        {
            foreach (var key in keys)
            {
                foreach (var item in GetByKey(key.ToString()))
                {
                    yield return item;
                }
            }
        }
    }

    public void Save(IEnumerable<Maintenance> maintenanceItems)
    {
        string allKeys = $"maintenance3";
        List<RedisValue> keys = db.SetMembers(allKeys).ToList();
        var batch = db.CreateBatch();
        foreach (var item in maintenanceItems)
        {
            string key = $"maintenance3:{item.Domain}:{item.ClientCode}";
            if(!keys.Contains(key)) keys.Add((RedisValue) key);
            string? json2 = db.StringGet(key);
            if(json2 == null)
            {
                batch.StringSetAsync(key, JsonSerializer.Serialize(new List<Maintenance> { item }));
            } else
            {
                var maintenances = JsonSerializer.Deserialize<List<Maintenance>>(json2) ?? new List<Maintenance>();
                var envItem = maintenances.Where(x => x.Environment == item.Environment).FirstOrDefault();
                if(envItem != null)
                {
                    maintenances.Remove(envItem);
                }
                maintenances.Add(item);
                batch.StringSetAsync(key, JsonSerializer.Serialize(maintenances));
            }
        }
        batch.Execute();
    }
}