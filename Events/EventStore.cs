using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GilDelta.Wallet;

namespace GilDelta.Events;

/// <summary>
/// Append-only JSONL event log. One file per character, keyed by content ID
/// at construction time (the path).
/// </summary>
public sealed class EventStore
{
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    public EventStore(string path)
    {
        _path = path;
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    public void Append(GilEvent ev)
    {
        var record = StoredEvent.From(ev, CurrentSchemaVersion);
        var line = JsonSerializer.Serialize(record, JsonOpts);
        File.AppendAllText(_path, line + "\n");
    }

    public IEnumerable<GilEvent> LoadAll()
    {
        if (!File.Exists(_path)) yield break;

        foreach (var line in File.ReadLines(_path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            StoredEvent? record;
            try
            {
                record = JsonSerializer.Deserialize<StoredEvent>(line, JsonOpts);
            }
            catch
            {
                continue;  // production logs Warning here
            }

            if (record is null) continue;
            if (record.V != CurrentSchemaVersion) continue;

            yield return record.ToDomain();
        }
    }

    /// <summary>Internal DTO for JSON (de)serialization.</summary>
    private sealed record StoredEvent(
        [property: JsonPropertyName("v")]        int V,
        [property: JsonPropertyName("ts")]       DateTimeOffset Ts,
        [property: JsonPropertyName("wallet")]   StoredWallet Wallet,
        [property: JsonPropertyName("amount")]   long Amount,
        [property: JsonPropertyName("category")] GilEventCategory Category,
        [property: JsonPropertyName("note")]     string? Note)
    {
        public static StoredEvent From(GilEvent e, int version) =>
            new(version, e.Timestamp,
                new StoredWallet(e.Wallet.Kind, e.Wallet.Identifier),
                e.Amount, e.Category, e.Note);

        public GilEvent ToDomain() =>
            new(Ts, new WalletId(Wallet.Kind, Wallet.Id), Amount, Category, Note);
    }

    private sealed record StoredWallet(
        [property: JsonPropertyName("kind")] WalletKind Kind,
        [property: JsonPropertyName("id")]   string Id);
}
