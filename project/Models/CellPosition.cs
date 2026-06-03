using System.Text.Json.Serialization;

namespace project.Models;

public class CellPosition : IEquatable<CellPosition>
{
    [JsonPropertyName("row")]
    public int Row { get; set; }

    [JsonPropertyName("col")]
    public int Col { get; set; }

    public CellPosition()
    {
    }

    public CellPosition(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public bool IsValid()
    {
        return Row >= 0 && Row < 8 && Col >= 0 && Col < 8;
    }

    public bool Equals(CellPosition? other)
    {
        if (other is null)
        {
            return false;
        }

        return Row == other.Row && Col == other.Col;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CellPosition);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Row, Col);
    }
}

