using System.Text.Json.Serialization;

namespace project.Models;

public class Move
{
    [JsonPropertyName("fromRow")]
    public int FromRow { get; set; }

    [JsonPropertyName("fromCol")]
    public int FromCol { get; set; }

    [JsonPropertyName("toRow")]
    public int ToRow { get; set; }

    [JsonPropertyName("toCol")]
    public int ToCol { get; set; }

    public Move()
    {
    }

    public Move(int fromRow, int fromCol, int toRow, int toCol)
    {
        FromRow = fromRow;
        FromCol = fromCol;
        ToRow = toRow;
        ToCol = toCol;
    }

    public int GetRowDelta()
    {
        return ToRow - FromRow;
    }

    public int GetColDelta()
    {
        return ToCol - FromCol;
    }

    public bool IsDiagonal()
    {
        return Math.Abs(GetRowDelta()) == Math.Abs(GetColDelta());
    }
}

