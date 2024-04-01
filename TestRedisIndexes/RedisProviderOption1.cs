using StackExchange.Redis;

public class RedisProviderOption1(IDatabase db) : IRedisProvider
{
    public void Save(IEnumerable<Maintenance> maintenanceItems)
    {
        var chunks = maintenanceItems.Chunk(300);

        foreach (var items in chunks)
        {
            var batch = db.CreateBatch();

            foreach (var maintenance in items)
            {

                var primaryKey = $"maintenance:{maintenance.Domain}:{maintenance.ClientCode}:{maintenance.Environment}";

                HashEntry[] entries = [
                    new("Id", maintenance.Id),
                    new("Domain", maintenance.Domain),
                    new("ClientCode", maintenance.ClientCode),
                    new("Environment", maintenance.Environment),
                    new("StartTime", maintenance.StartTime.ToString()),
                    new("ExpiryTime", maintenance.ExpiryTime.ToString()),
                    new("Message", maintenance.Message)
                    ];

                batch.HashSetAsync(primaryKey, entries);

                var secondaryKey = $"maintenance_client_environments:{maintenance.Domain}:{maintenance.ClientCode}";
                batch.SetAddAsync(secondaryKey, primaryKey);

                var tertiaryKey = $"maintenance_environments";
                batch.SetAddAsync(tertiaryKey, primaryKey);
            }

            batch.Execute();
        }
    }

    public IEnumerable<Maintenance> Get(string domain, string clientCode)
    {
        var secondaryKey = $"maintenance_client_environments:{domain}:{clientCode}";
        var keys = db.SetMembersAsync(secondaryKey).Result;
        foreach (var key in keys)
        {
            var data = db.HashGetAllAsync(key.ToString()).Result;
            var map = data.ToDictionary(x => x.Name.ToString().ToLower(), x => x.Value.ToString());
            yield return new Maintenance
            {
                Id = map["id"],
                Domain = map["domain"],
                ClientCode = map["clientcode"],
                StartTime = DateTimeOffset.Parse(map["starttime"]),
                ExpiryTime = DateTimeOffset.Parse(map["expirytime"]),
                Message = map["message"]
            };
        }
    }

    public IEnumerable<Maintenance> GetAll()
    {
        var secondaryKey = $"maintenance_environments";
        var keys = db.SetMembersAsync(secondaryKey).Result;
        foreach (var key in keys)
        {
            var data = db.HashGetAllAsync(key.ToString()).Result;
            var map = data.ToDictionary(x => x.Name.ToString().ToLower(), x => x.Value.ToString());
            yield return new Maintenance
            {
                Id = map["id"],
                Domain = map["domain"],
                ClientCode = map["clientcode"],
                StartTime = DateTimeOffset.Parse(map["starttime"]),
                ExpiryTime = DateTimeOffset.Parse(map["expirytime"]),
                Message = map["message"]
            };
        }
    }
}
