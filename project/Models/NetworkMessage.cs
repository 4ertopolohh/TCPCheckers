using System.Text.Json;
using System.Text.Json.Serialization;

namespace project.Models;

public class NetworkMessage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("fromRow")]
    public int? FromRow { get; set; }

    [JsonPropertyName("fromCol")]
    public int? FromCol { get; set; }

    [JsonPropertyName("toRow")]
    public int? ToRow { get; set; }

    [JsonPropertyName("toCol")]
    public int? ToCol { get; set; }

    [JsonPropertyName("winner")]
    public PlayerColor? Winner { get; set; }

    [JsonPropertyName("errorText")]
    public string? ErrorText { get; set; }

    [JsonPropertyName("playerColor")]
    public PlayerColor? PlayerColor { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public static NetworkMessage? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<NetworkMessage>(json, JsonOptions);
    }

    public static NetworkMessage CreateMoveMessage(Move move)
    {
        return new NetworkMessage
        {
            Type = "move",
            FromRow = move.FromRow,
            FromCol = move.FromCol,
            ToRow = move.ToRow,
            ToCol = move.ToCol
        };
    }

    public static NetworkMessage CreateGameOverMessage(PlayerColor winner)
    {
        return new NetworkMessage
        {
            Type = "gameOver",
            Winner = winner
        };
    }

    public static NetworkMessage CreateErrorMessage(string text)
    {
        return new NetworkMessage
        {
            Type = "error",
            ErrorText = text
        };
    }

    public static NetworkMessage CreateConnectMessage(PlayerColor playerColor)
    {
        return new NetworkMessage
        {
            Type = "connect",
            PlayerColor = playerColor
        };
    }

    public static NetworkMessage CreateDisconnectMessage(string reason)
    {
        return new NetworkMessage
        {
            Type = "disconnect",
            Reason = reason
        };
    }

    public static NetworkMessage CreateNewGameMessage()
    {
        return new NetworkMessage
        {
            Type = "newGame"
        };
    }

    public Move? ToMove()
    {
        if (!FromRow.HasValue || !FromCol.HasValue || !ToRow.HasValue || !ToCol.HasValue)
        {
            return null;
        }

        return new Move(FromRow.Value, FromCol.Value, ToRow.Value, ToCol.Value);
    }
}

