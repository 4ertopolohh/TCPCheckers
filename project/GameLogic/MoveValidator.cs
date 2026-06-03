using project.Models;

namespace project.GameLogic;

public class MoveValidator
{
    private readonly GameBoard board;

    public MoveValidator(GameBoard board)
    {
        this.board = board;
    }

    public bool IsValidMove(Move move, PlayerColor player)
    {
        if (!board.IsInsideBoard(move.FromRow, move.FromCol) || !board.IsInsideBoard(move.ToRow, move.ToCol))
        {
            return false;
        }

        var piece = board.GetPiece(move.FromRow, move.FromCol);
        if (piece is null)
        {
            return false;
        }

        if (!ValidatePieceOwner(move, player))
        {
            return false;
        }

        if (!board.IsCellEmpty(move.ToRow, move.ToCol))
        {
            return false;
        }

        if (!move.IsDiagonal())
        {
            return false;
        }

        return piece.IsKing() ? ValidateKingMove(move, piece.Color) : ValidateManMove(move, piece.Color);
    }

    public bool IsSimpleMove(Move move)
    {
        var piece = board.GetPiece(move.FromRow, move.FromCol);
        if (piece is null)
        {
            return false;
        }

        if (!move.IsDiagonal())
        {
            return false;
        }

        return piece.IsKing() ? IsKingSimpleMove(move) : IsManSimpleMove(move, piece.Color);
    }

    public bool IsCaptureMove(Move move)
    {
        var piece = board.GetPiece(move.FromRow, move.FromCol);
        if (piece is null)
        {
            return false;
        }

        if (!move.IsDiagonal())
        {
            return false;
        }

        return piece.IsKing() ? IsKingCaptureMove(move, piece.Color) : IsManCaptureMove(move, piece.Color);
    }

    public bool CanPlayerMove(PlayerColor color)
    {
        return GetAvailableMovesForPlayer(color).Count > 0;
    }

    public CellPosition? GetCapturedPiecePosition(Move move)
    {
        var piece = board.GetPiece(move.FromRow, move.FromCol);
        if (piece is null)
        {
            return null;
        }

        if (piece.IsKing())
        {
            var rowStep = Math.Sign(move.ToRow - move.FromRow);
            var colStep = Math.Sign(move.ToCol - move.FromCol);
            var row = move.FromRow + rowStep;
            var col = move.FromCol + colStep;
            CellPosition? captured = null;

            while (row != move.ToRow && col != move.ToCol)
            {
                var current = board.GetPiece(row, col);
                if (current is not null)
                {
                    if (current.Color == piece.Color)
                    {
                        return null;
                    }

                    if (captured is not null)
                    {
                        return null;
                    }

                    captured = new CellPosition(row, col);
                }

                row += rowStep;
                col += colStep;
            }

            return captured;
        }

        if (Math.Abs(move.GetRowDelta()) == 2 && Math.Abs(move.GetColDelta()) == 2)
        {
            var capturedRow = (move.FromRow + move.ToRow) / 2;
            var capturedCol = (move.FromCol + move.ToCol) / 2;
            return new CellPosition(capturedRow, capturedCol);
        }

        return null;
    }

    public bool ValidatePieceOwner(Move move, PlayerColor player)
    {
        var piece = board.GetPiece(move.FromRow, move.FromCol);
        return piece is not null && piece.Color == player;
    }

    public bool HasAnyCapture(PlayerColor player)
    {
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(row, col);
                if (piece is null || piece.Color != player)
                {
                    continue;
                }

                if (CanPieceCapture(new CellPosition(row, col), player))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool CanPieceCapture(CellPosition position, PlayerColor player)
    {
        var piece = board.GetPiece(position.Row, position.Col);
        if (piece is null || piece.Color != player)
        {
            return false;
        }

        return piece.IsKing() ? CanKingCapture(position, player) : CanManCapture(position, player);
    }

    public List<Move> GetAvailableMovesForPlayer(PlayerColor player)
    {
        var captureMoves = new List<Move>();
        var simpleMoves = new List<Move>();

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var piece = board.GetPiece(row, col);
                if (piece is null || piece.Color != player)
                {
                    continue;
                }

                var moves = piece.IsKing()
                    ? GetKingMoves(row, col, player)
                    : GetManMoves(row, col, player);

                foreach (var move in moves)
                {
                    if (IsCaptureMove(move))
                    {
                        captureMoves.Add(move);
                    }
                    else
                    {
                        simpleMoves.Add(move);
                    }
                }
            }
        }

        return captureMoves.Count > 0 ? captureMoves : simpleMoves;
    }

    private bool ValidateManMove(Move move, PlayerColor player)
    {
        if (IsManCaptureMove(move, player))
        {
            var captured = GetCapturedPiecePosition(move);
            if (captured is null)
            {
                return false;
            }

            var capturedPiece = board.GetPiece(captured.Row, captured.Col);
            return capturedPiece is not null && capturedPiece.Color != player;
        }

        if (IsManSimpleMove(move, player))
        {
            return !HasAnyCapture(player);
        }

        return false;
    }

    private bool ValidateKingMove(Move move, PlayerColor player)
    {
        if (IsKingCaptureMove(move, player))
        {
            return true;
        }

        if (IsKingSimpleMove(move))
        {
            return !HasAnyCapture(player);
        }

        return false;
    }

    private bool IsManSimpleMove(Move move, PlayerColor player)
    {
        var rowDelta = move.GetRowDelta();
        var colDelta = Math.Abs(move.GetColDelta());

        if (Math.Abs(rowDelta) != 1 || colDelta != 1)
        {
            return false;
        }

        if (player == PlayerColor.White)
        {
            return rowDelta == -1;
        }

        return rowDelta == 1;
    }

    private bool IsManCaptureMove(Move move, PlayerColor player)
    {
        if (Math.Abs(move.GetRowDelta()) != 2 || Math.Abs(move.GetColDelta()) != 2)
        {
            return false;
        }

        var capturedRow = (move.FromRow + move.ToRow) / 2;
        var capturedCol = (move.FromCol + move.ToCol) / 2;
        var capturedPiece = board.GetPiece(capturedRow, capturedCol);

        return capturedPiece is not null && capturedPiece.Color != player;
    }

    private bool IsKingSimpleMove(Move move)
    {
        var rowStep = Math.Sign(move.ToRow - move.FromRow);
        var colStep = Math.Sign(move.ToCol - move.FromCol);

        if (rowStep == 0 || colStep == 0)
        {
            return false;
        }

        var row = move.FromRow + rowStep;
        var col = move.FromCol + colStep;

        while (row != move.ToRow && col != move.ToCol)
        {
            if (!board.IsCellEmpty(row, col))
            {
                return false;
            }

            row += rowStep;
            col += colStep;
        }

        return true;
    }

    private bool IsKingCaptureMove(Move move, PlayerColor player)
    {
        var rowStep = Math.Sign(move.ToRow - move.FromRow);
        var colStep = Math.Sign(move.ToCol - move.FromCol);

        if (rowStep == 0 || colStep == 0)
        {
            return false;
        }

        var row = move.FromRow + rowStep;
        var col = move.FromCol + colStep;
        var encounteredOpponent = 0;

        while (row != move.ToRow && col != move.ToCol)
        {
            var piece = board.GetPiece(row, col);
            if (piece is not null)
            {
                if (piece.Color == player)
                {
                    return false;
                }

                encounteredOpponent++;
                if (encounteredOpponent > 1)
                {
                    return false;
                }
            }

            row += rowStep;
            col += colStep;
        }

        return encounteredOpponent == 1;
    }

    private bool CanManCapture(CellPosition position, PlayerColor player)
    {
        var directions = new[]
        {
            new CellPosition(-2, -2),
            new CellPosition(-2, 2),
            new CellPosition(2, -2),
            new CellPosition(2, 2)
        };

        foreach (var direction in directions)
        {
            var targetRow = position.Row + direction.Row;
            var targetCol = position.Col + direction.Col;
            var move = new Move(position.Row, position.Col, targetRow, targetCol);

            if (!board.IsInsideBoard(targetRow, targetCol) || !board.IsCellEmpty(targetRow, targetCol))
            {
                continue;
            }

            if (IsManCaptureMove(move, player))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanKingCapture(CellPosition position, PlayerColor player)
    {
        var directions = new[]
        {
            new CellPosition(-1, -1),
            new CellPosition(-1, 1),
            new CellPosition(1, -1),
            new CellPosition(1, 1)
        };

        foreach (var direction in directions)
        {
            var row = position.Row + direction.Row;
            var col = position.Col + direction.Col;
            var foundOpponent = false;

            while (board.IsInsideBoard(row, col))
            {
                var piece = board.GetPiece(row, col);
                if (piece is null)
                {
                    if (foundOpponent)
                    {
                        return true;
                    }

                    row += direction.Row;
                    col += direction.Col;
                    continue;
                }

                if (piece.Color == player)
                {
                    break;
                }

                if (foundOpponent)
                {
                    break;
                }

                foundOpponent = true;
                row += direction.Row;
                col += direction.Col;
            }
        }

        return false;
    }

    private List<Move> GetManMoves(int row, int col, PlayerColor player)
    {
        var moves = new List<Move>();
        var simpleRowStep = player == PlayerColor.White ? -1 : 1;
        var simpleTargets = new[]
        {
            new CellPosition(row + simpleRowStep, col - 1),
            new CellPosition(row + simpleRowStep, col + 1)
        };

        foreach (var target in simpleTargets)
        {
            if (board.IsInsideBoard(target.Row, target.Col) && board.IsCellEmpty(target.Row, target.Col))
            {
                moves.Add(new Move(row, col, target.Row, target.Col));
            }
        }

        var captureTargets = new[]
        {
            new CellPosition(row - 2, col - 2),
            new CellPosition(row - 2, col + 2),
            new CellPosition(row + 2, col - 2),
            new CellPosition(row + 2, col + 2)
        };

        foreach (var target in captureTargets)
        {
            if (!board.IsInsideBoard(target.Row, target.Col) || !board.IsCellEmpty(target.Row, target.Col))
            {
                continue;
            }

            var move = new Move(row, col, target.Row, target.Col);
            if (IsManCaptureMove(move, player))
            {
                moves.Add(move);
            }
        }

        return moves;
    }

    private List<Move> GetKingMoves(int row, int col, PlayerColor player)
    {
        var moves = new List<Move>();
        var directions = new[]
        {
            new CellPosition(-1, -1),
            new CellPosition(-1, 1),
            new CellPosition(1, -1),
            new CellPosition(1, 1)
        };

        foreach (var direction in directions)
        {
            var currentRow = row + direction.Row;
            var currentCol = col + direction.Col;

            while (board.IsInsideBoard(currentRow, currentCol))
            {
                if (!board.IsCellEmpty(currentRow, currentCol))
                {
                    break;
                }

                moves.Add(new Move(row, col, currentRow, currentCol));
                currentRow += direction.Row;
                currentCol += direction.Col;
            }

            currentRow = row + direction.Row;
            currentCol = col + direction.Col;
            var foundOpponent = false;

            while (board.IsInsideBoard(currentRow, currentCol))
            {
                var piece = board.GetPiece(currentRow, currentCol);

                if (piece is null)
                {
                    if (foundOpponent)
                    {
                        moves.Add(new Move(row, col, currentRow, currentCol));
                    }

                    currentRow += direction.Row;
                    currentCol += direction.Col;
                    continue;
                }

                if (piece.Color == player)
                {
                    break;
                }

                if (foundOpponent)
                {
                    break;
                }

                foundOpponent = true;
                currentRow += direction.Row;
                currentCol += direction.Col;
            }
        }

        return moves;
    }
}

