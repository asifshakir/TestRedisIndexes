using StackExchange.Redis;

public interface IRedisProvider
{
    IEnumerable<Maintenance> GetAll();
    IEnumerable<Maintenance> Get(string domain, string clientCode);
    void Save(IEnumerable<Maintenance> maintenanceItems);
}