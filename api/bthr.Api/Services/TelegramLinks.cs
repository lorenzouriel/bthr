using System.Security.Cryptography;
using System.Text;

namespace bthr.Api.Services;

// Single API replica by design: all outstanding links expire on restart.
public sealed class TelegramLinks
{
    public sealed record Pending(int UserId, DateTime Expires, long? Sender = null, string? Phone = null);
    private readonly Dictionary<string, Pending> pending = new();
    private readonly string bootId = Guid.NewGuid().ToString("N");
    private readonly Dictionary<int, string> versions = new();
    public string Version(int userId) => versions.GetValueOrDefault(userId, bootId);
    public SemaphoreSlim Gate { get; } = new(1);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public string Create(int userId)
    {
        foreach (var key in pending.Where(x => (userId != 0 && x.Value.UserId == userId) || x.Value.Expires <= DateTime.UtcNow).Select(x => x.Key).ToArray()) pending.Remove(key);
        if (pending.Count >= 1000) throw new InvalidOperationException("Link capacity reached");
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        pending[Hash(token)] = new(userId, DateTime.UtcNow.AddMinutes(5));
        return token;
    }
    public Pending? Get(string token) => pending.TryGetValue(Hash(token), out var value) && value.Expires > DateTime.UtcNow ? value : null;
    public bool Claim(string token, long sender)
    {
        var value = Get(token);
        if (value == null || sender <= 0 || value.Sender.HasValue && value.Sender != sender) return false;
        pending[Hash(token)] = value with { Sender = sender };
        return true;
    }
    public string CreatePhone(long sender, string phone)
    {
        var token = Create(0);
        pending[Hash(token)] = new(0, DateTime.UtcNow.AddMinutes(5), sender, phone);
        return token;
    }
    public bool AttachPhone(string token, int userId, string? phone)
    {
        var value = Get(token);
        if (value?.UserId != 0 || value.Phone == null || phone == null || value.Phone != NormalizePhone(phone)) return false;
        pending[Hash(token)] = value with { UserId = userId, Phone = null };
        return true;
    }
    public static string NormalizePhone(string phone) => new string(phone.Where(char.IsAsciiDigit).ToArray());
    public void Remove(string token) => pending.Remove(Hash(token));
    public void Clear(int userId)
    {
        versions[userId] = Guid.NewGuid().ToString("N");
        foreach (var key in pending.Where(x => x.Value.UserId == userId).Select(x => x.Key).ToArray()) pending.Remove(key);
    }
    public static bool ServiceKey(HttpRequest request, IConfiguration config)
    {
        var expected = config["Bot:ApiKey"];
        var actual = request.Headers["X-Bot-Api-Key"].ToString();
        return !string.IsNullOrEmpty(expected) && expected.Length >= 32 &&
            CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(actual)));
    }
}
