using System.Text.Json.Serialization;

namespace project.Models;

public class MoveResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("capturedPiece")]
    public Piece? CapturedPiece { get; set; }

    [JsonPropertyName("winner")]
    public PlayerColor? Winner { get; set; }

    [JsonPropertyName("isCapture")]
    public bool IsCapture { get; set; }

    [JsonPropertyName("requiresAdditionalCapture")]
    public bool RequiresAdditionalCapture { get; set; }

    public static MoveResult Failed(string message)
    {
        return new MoveResult
        {
            Success = false,
            Message = message
        };
    }

    public static MoveResult Completed(string message)
    {
        return new MoveResult
        {
            Success = true,
            Message = message
        };
    }
}

