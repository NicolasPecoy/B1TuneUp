using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<LicenseStore>();
builder.Services.AddSingleton<LicenseSigner>();
var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { product = "B1TuneUp License Portal", status = "OK" }));

app.MapGet("/api/license/public-key", (LicenseSigner signer) => Results.Text(signer.PublicKeyXml, "text/xml"));

app.MapPost("/api/license/activate", (ActivationRequest request, LicenseStore store, LicenseSigner signer) =>
{
    var activation = store.CreateActivation(request);
    var token = signer.Sign(activation.Payload);
    store.SaveToken(activation.ActivationId, token);
    return Results.Ok(new ActivationResponse(activation.ActivationId, token, activation.Payload.ExpiresOn, signer.PublicKeyXml));
});

app.MapPost("/api/license/offline/request/decode", (OfflineRequest request) =>
{
    var json = Encoding.UTF8.GetString(Base64Url.Decode(request.Request));
    return Results.Text(json, "application/json");
});

app.MapPost("/api/license/offline/response", (OfflineActivationRequest request, LicenseStore store, LicenseSigner signer) =>
{
    var json = Encoding.UTF8.GetString(Base64Url.Decode(request.OfflineRequest));
    var decoded = JsonSerializer.Deserialize<ActivationRequest>(json, JsonOptions.Options) ?? new ActivationRequest();
    decoded.Customer = string.IsNullOrWhiteSpace(request.Customer) ? decoded.Customer : request.Customer;
    decoded.Edition = string.IsNullOrWhiteSpace(request.Edition) ? decoded.Edition : request.Edition;
    decoded.Months = request.Months <= 0 ? 12 : request.Months;
    var activation = store.CreateActivation(decoded);
    var token = signer.Sign(activation.Payload);
    store.SaveToken(activation.ActivationId, token);
    return Results.Ok(new ActivationResponse(activation.ActivationId, token, activation.Payload.ExpiresOn, signer.PublicKeyXml));
});

app.MapPost("/api/license/revoke/{activationId}", (string activationId, LicenseStore store) =>
{
    store.Revoke(activationId);
    return Results.Ok(new { activationId, status = "Revoked" });
});

app.MapPost("/api/license/renew/{activationId}", (string activationId, int months, LicenseStore store, LicenseSigner signer) =>
{
    var activation = store.Renew(activationId, months <= 0 ? 12 : months);
    var token = signer.Sign(activation.Payload);
    store.SaveToken(activation.ActivationId, token);
    return Results.Ok(new ActivationResponse(activation.ActivationId, token, activation.Payload.ExpiresOn, signer.PublicKeyXml));
});

app.MapGet("/api/license/activations", (LicenseStore store) => Results.Ok(store.GetAll()));

app.Run();

public sealed class ActivationRequest
{
    public string? Customer { get; set; }
    public string? Edition { get; set; } = "Premium";
    public string? RequestedEdition { get; set; }
    public string? CompanyDb { get; set; }
    public string? InstallationNumber { get; set; }
    public string? HardwareKey { get; set; }
    public string[]? Modules { get; set; }
    public string[]? RequestedModules { get; set; }
    public int Months { get; set; } = 12;
    public int MaxUsers { get; set; } = 25;
    public string? Notes { get; set; }
}

public sealed record OfflineRequest(string Request);
public sealed record OfflineActivationRequest(string OfflineRequest, string? Customer, string? Edition, int Months);
public sealed record ActivationResponse(string ActivationId, string Token, string ExpiresOn, string PublicKeyXml);

public sealed class LicensePayload
{
    public string Product { get; set; } = "B1TuneUp";
    public string LicenseId { get; set; } = Guid.NewGuid().ToString("N").ToUpperInvariant();
    public string? Customer { get; set; }
    public string Edition { get; set; } = "Premium";
    public string? CompanyDb { get; set; }
    public string? InstallationNumber { get; set; }
    public string? HardwareKey { get; set; }
    public string IssuedOn { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    public string ExpiresOn { get; set; } = DateTime.UtcNow.AddMonths(12).ToString("yyyy-MM-dd");
    public List<string> Modules { get; set; } = new();
    public int MaxUsers { get; set; }
    public string? Notes { get; set; }
}

public sealed class ActivationRecord
{
    public string ActivationId { get; set; } = Guid.NewGuid().ToString("N").ToUpperInvariant();
    public string Status { get; set; } = "Active";
    public LicensePayload Payload { get; set; } = new();
    public string? Token { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class LicenseStore
{
    private readonly string _path = Path.Combine(AppContext.BaseDirectory, "license-store.json");
    private readonly object _sync = new();

    public IList<ActivationRecord> GetAll()
    {
        lock (_sync) return Load();
    }

    public ActivationRecord CreateActivation(ActivationRequest request)
    {
        lock (_sync)
        {
            var all = Load();
            var record = new ActivationRecord
            {
                Payload = new LicensePayload
                {
                    Customer = request.Customer,
                    Edition = string.IsNullOrWhiteSpace(request.Edition) ? (string.IsNullOrWhiteSpace(request.RequestedEdition) ? "Premium" : request.RequestedEdition!) : request.Edition!,
                    CompanyDb = request.CompanyDb,
                    InstallationNumber = request.InstallationNumber,
                    HardwareKey = request.HardwareKey,
                    IssuedOn = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    ExpiresOn = DateTime.UtcNow.AddMonths(request.Months <= 0 ? 12 : request.Months).ToString("yyyy-MM-dd"),
                    Modules = (request.Modules ?? request.RequestedModules ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                    MaxUsers = request.MaxUsers <= 0 ? 25 : request.MaxUsers,
                    Notes = request.Notes
                }
            };
            all.Add(record);
            Save(all);
            return record;
        }
    }

    public void SaveToken(string activationId, string token)
    {
        lock (_sync)
        {
            var all = Load();
            var record = all.First(x => x.ActivationId.Equals(activationId, StringComparison.OrdinalIgnoreCase));
            record.Token = token;
            record.UpdatedUtc = DateTime.UtcNow;
            Save(all);
        }
    }

    public void Revoke(string activationId)
    {
        lock (_sync)
        {
            var all = Load();
            var record = all.First(x => x.ActivationId.Equals(activationId, StringComparison.OrdinalIgnoreCase));
            record.Status = "Revoked";
            record.UpdatedUtc = DateTime.UtcNow;
            Save(all);
        }
    }

    public ActivationRecord Renew(string activationId, int months)
    {
        lock (_sync)
        {
            var all = Load();
            var record = all.First(x => x.ActivationId.Equals(activationId, StringComparison.OrdinalIgnoreCase));
            record.Status = "Active";
            record.Payload.ExpiresOn = DateTime.UtcNow.AddMonths(months).ToString("yyyy-MM-dd");
            record.UpdatedUtc = DateTime.UtcNow;
            Save(all);
            return record;
        }
    }

    private List<ActivationRecord> Load()
    {
        if (!File.Exists(_path)) return new List<ActivationRecord>();
        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<List<ActivationRecord>>(json, JsonOptions.Options) ?? new List<ActivationRecord>();
    }

    private void Save(List<ActivationRecord> records)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(records, JsonOptions.Options));
    }
}

public sealed class LicenseSigner
{
    private const string Prefix = "B1TRSA";
    private readonly string _privateKeyPath = Path.Combine(AppContext.BaseDirectory, "license-private.xml");
    private readonly string _publicKeyPath = Path.Combine(AppContext.BaseDirectory, "license-public.xml");

    public LicenseSigner()
    {
        EnsureKeys();
    }

    public string PublicKeyXml => File.ReadAllText(_publicKeyPath);

    public string Sign(LicensePayload payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions.Options);
        var encodedPayload = Base64Url.Encode(Encoding.UTF8.GetBytes(json));
        using var rsa = new RSACryptoServiceProvider(2048);
        rsa.FromXmlString(File.ReadAllText(_privateKeyPath));
        var sha256 = CryptoConfig.MapNameToOID("SHA256") ?? "SHA256";
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(encodedPayload), sha256);
        return Prefix + "." + encodedPayload + "." + Base64Url.Encode(signature);
    }

    private void EnsureKeys()
    {
        if (File.Exists(_privateKeyPath) && File.Exists(_publicKeyPath)) return;
        using var rsa = new RSACryptoServiceProvider(2048);
        File.WriteAllText(_privateKeyPath, rsa.ToXmlString(true));
        File.WriteAllText(_publicKeyPath, rsa.ToXmlString(false));
    }
}

public static class Base64Url
{
    public static string Encode(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] Decode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
