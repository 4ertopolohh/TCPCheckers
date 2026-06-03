using System.Text.Json.Serialization;

namespace project.Models;

public class Piece
{
    [JsonPropertyName("color")]
    public PlayerColor Color { get; set; }

    [JsonPropertyName("type")]
    public PieceType Type { get; set; }

    public Piece()
    {
    }

    public Piece(PlayerColor color, PieceType type)
    {
        Color = color;
        Type = type;
    }

    public bool IsKing()
    {
        return Type == PieceType.King;
    }

    public void PromoteToKing()
    {
        Type = PieceType.King;
    }

    public Piece Clone()
    {
        return new Piece(Color, Type);
    }
}

