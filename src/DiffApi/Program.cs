// minimal diff api; b64(JSON{"input":...}) in; diff out
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DiffStore>(); // di reg before Build()
var app = builder.Build();


// POST left
app.MapPost("/v1/diff/{id}/left", async (HttpRequest req, string id, DiffStore store) =>
{
    var payload = await ReadAndDecodeAsync(req); // b64->json->dto
    if (payload is null) return Results.BadRequest(new Error("Body must be base64 of {\"input\":\"...\"}."));
    if (string.IsNullOrEmpty(payload.Input)) return Results.BadRequest(new Error("\"input\" required."));

    store.SetLeft(id, payload.Input); // save
    return Results.Accepted($"/v1/diff/{id}", new Ack(id, "left"));
})
.Accepts<string>("application/custom");

// POST right
app.MapPost("/v1/diff/{id}/right", async (HttpRequest req, string id, DiffStore store) =>
{
    var payload = await ReadAndDecodeAsync(req);
    if (payload is null) return Results.BadRequest(new Error("Body must be base64 of {\"input\":\"...\"}."));
    if (string.IsNullOrEmpty(payload.Input)) return Results.BadRequest(new Error("\"input\" required."));

    store.SetRight(id, payload.Input);
    return Results.Accepted($"/v1/diff/{id}", new Ack(id, "right"));
})
.Accepts<string>("application/custom");

// GET diff
app.MapGet("/v1/diff/{id}", (string id, DiffStore store) =>
{
    var pair = store.GetPair(id); // load
    if (pair.Left is null || pair.Right is null)
        return Results.BadRequest(new Error("Need both sides."));

    var left = pair.Left;
    var right = pair.Right;

    if (left == right) // eq
        return Results.Ok(new DiffResponse { Status = "Equal", Message = "inputs were equal" });

    if (left.Length != right.Length) // size diff
        return Results.Ok(new DiffResponse {
            Status = "SizeMismatch", Message = "inputs are of different size",
            LeftSize = left.Length, RightSize = right.Length });

    // same len -> compute segs
    var diffs = DiffEngine.ComputeSegments(left, right);
    return Results.Ok(new DiffResponse {
        Status = "ContentMismatch",
        Message = $"found {diffs.Count} differing segment(s)",
        Diffs = diffs });
});

//app.Services.AddSingleton<DiffStore>(); // di
app.Run();

// helpers --------------------------------------------------------

// read raw, trim quotes, b64->json->dto
static async Task<Decoded?> ReadAndDecodeAsync(HttpRequest req)
{
    using var reader = new StreamReader(req.Body, Encoding.UTF8);
    var raw = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(raw)) return null;

    var s = raw.Trim();
    if (s.Length >= 2 && s.StartsWith('\"') && s.EndsWith('\"')) s = s[1..^1];

    try
    {
        var bytes = Convert.FromBase64String(s);
        var json = Encoding.UTF8.GetString(bytes);
        var dto = JsonSerializer.Deserialize<InputDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return dto is null ? null : new Decoded(dto.Input);
    }
    catch { return null; }
}

// mem store (demo only)
public sealed class DiffStore
{
    private readonly ConcurrentDictionary<string, Entry> _map = new();

    public void SetLeft(string id, string v)  => _map.AddOrUpdate(id, _ => new Entry { Left = v }, (_, e) => e with { Left = v });
    public void SetRight(string id, string v) => _map.AddOrUpdate(id, _ => new Entry { Right = v }, (_, e) => e with { Right = v });
    public Entry GetPair(string id)           => _map.TryGetValue(id, out var e) ? e : new Entry();

    public record Entry { public string? Left { get; init; } public string? Right { get; init; } }
}

// diff segs over chars (same len only)
public static class DiffEngine
{
    public static List<DiffSegment> ComputeSegments(string left, string right)
    {
        if (left.Length != right.Length) throw new ArgumentException("Need same len.");

        var segs = new List<DiffSegment>();
        int i = 0;
        while (i < left.Length)
        {
            if (left[i] == right[i]) { i++; continue; }
            var start = i;
            while (i < left.Length && left[i] != right[i]) i++;
            var len = i - start;

            segs.Add(new DiffSegment { OffsetLeft = start, LengthLeft = len, OffsetRight = start, LengthRight = len });
        }
        return segs;
    }
}

// dtos
public sealed record InputDto([property: JsonPropertyName("input")] string Input);
public sealed record Decoded(string Input);
public sealed record Ack(string Id, string Side);
public sealed record Error(string Message);

public sealed record DiffSegment
{
    public int OffsetLeft { get; init; }
    public int LengthLeft { get; init; }
    public int OffsetRight { get; init; }
    public int LengthRight { get; init; }
}

public sealed record DiffResponse
{
    public string Status { get; init; } = default!;
    public string? Message { get; init; }
    public int? LeftSize { get; init; }
    public int? RightSize { get; init; }
    public List<DiffSegment>? Diffs { get; init; }
}
