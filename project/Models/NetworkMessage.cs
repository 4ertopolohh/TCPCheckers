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

    [JsonPropertyName("move")]
    public Move? Move { get; set; }

    [JsonPropertyName("winner")]
    public PlayerColor? Winner { get; set; }

    [JsonPropertyName("errorText")]
    public string? ErrorText { get; set; }

    [JsonPropertyName("playerColor")]
    public PlayerColor? PlayerColor { get; set; }

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
            Move = move
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
            ErrorText = reason
        };
    }
}

