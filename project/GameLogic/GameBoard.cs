using project.Models;

namespace project.GameLogic;

public class GameBoard
{
    private readonly Piece?[,] cells = new Piece?[8, 8];

    public void Initialize()
    {
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                cells[row, col] = null;
            }
        }

        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                if (IsDarkCell(row, col))
                {
                    cells[row, col] = new Piece(PlayerColor.Black, PieceType.Man);
                }
            }
        }

        for (var row = 5; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                if (IsDarkCell(row, col))
                {
                    cells[row, col] = new Piece(PlayerColor.White, PieceType.Man);
                }
            }
        }
    }

    public Piece? GetPiece(int row, int col)
    {
        if (!IsInsideBoard(row, col))
        {
            return null;
        }

        return cells[row, col];
    }

    public void SetPiece(int row, int col, Piece? piece)
    {
        if (!IsInsideBoard(row, col))
        {
            return;
        }

        cells[row, col] = piece;
    }

    public void RemovePiece(int row, int col)
    {
        if (!IsInsideBoard(row, col))
        {
            return;
        }

        cells[row, col] = null;
    }

    public bool IsInsideBoard(int row, int col)
    {
        return row >= 0 && row < 8 && col >= 0 && col < 8;
    }

    public bool IsCellEmpty(int row, int col)
    {
        return IsInsideBoard(row, col) && cells[row, col] is null;
    }

    public int CountPieces(PlayerColor color)
    {
        var count = 0;

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var piece = cells[row, col];
                if (piece is not null && piece.Color == color)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public Piece?[,] GetSnapshot()
    {
        var snapshot = new Piece?[8, 8];

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                snapshot[row, col] = cells[row, col]?.Clone();
            }
        }

        return snapshot;
    }

    private static bool IsDarkCell(int row, int col)
    {
        return (row + col) % 2 == 1;
    }
}

