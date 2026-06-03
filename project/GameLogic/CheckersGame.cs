using project.Models;

namespace project.GameLogic;

public class CheckersGame
{
    private readonly GameBoard board;
    private readonly MoveValidator validator;
    private CellPosition? requiredCaptureFrom;

    private PlayerColor currentPlayer;
    private GameState state;

    public event Action? BoardChanged;
    public event Action<GameState>? GameStateChanged;

    public CheckersGame()
    {
        board = new GameBoard();
        validator = new MoveValidator(board);
        currentPlayer = PlayerColor.White;
        state = GameState.WaitingForConnection;
        StartNewGame();
    }

    public void StartNewGame()
    {
        board.Initialize();
        currentPlayer = PlayerColor.White;
        requiredCaptureFrom = null;
        state = GameState.Playing;
        BoardChanged?.Invoke();
        GameStateChanged?.Invoke(state);
    }

    public MoveResult TryMakeMove(Move move)
    {
        if (state != GameState.Playing)
        {
            return MoveResult.Failed("Игра сейчас недоступна.");
        }

        if (requiredCaptureFrom is not null)
        {
            if (move.FromRow != requiredCaptureFrom.Row || move.FromCol != requiredCaptureFrom.Col)
            {
                return MoveResult.Failed("Необходимо продолжить взятие той же шашкой.");
            }
        }

        if (!validator.IsValidMove(move, currentPlayer))
        {
            return MoveResult.Failed("Ход невозможен: нарушены правила игры.");
        }

        var result = ApplyMove(move);
        if (!result.Success)
        {
            return result;
        }

        var winner = CheckWinner();
        if (winner.HasValue)
        {
            result.Winner = winner;
            state = winner.Value == PlayerColor.White ? GameState.WhiteWon : GameState.BlackWon;
            GameStateChanged?.Invoke(state);
        }

        BoardChanged?.Invoke();
        return result;
    }

    public void SwitchTurn()
    {
        currentPlayer = currentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
    }

    public PlayerColor? CheckWinner()
    {
        var whiteCount = board.CountPieces(PlayerColor.White);
        var blackCount = board.CountPieces(PlayerColor.Black);

        if (whiteCount == 0)
        {
            return PlayerColor.Black;
        }

        if (blackCount == 0)
        {
            return PlayerColor.White;
        }

        if (!validator.CanPlayerMove(currentPlayer))
        {
            return currentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
        }

        return null;
    }

    public List<Move> GetAvailableMoves()
    {
        if (requiredCaptureFrom is not null)
        {
            return validator
                .GetAvailableMovesForPlayer(currentPlayer)
                .Where(m => m.FromRow == requiredCaptureFrom.Row && m.FromCol == requiredCaptureFrom.Col)
                .ToList();
        }

        return validator.GetAvailableMovesForPlayer(currentPlayer);
    }

    public MoveResult ApplyMove(Move move)
    {
        var piece = board.GetPiece(move.FromRow, move.FromCol);
        if (piece is null)
        {
            return MoveResult.Failed("Шашка для хода не найдена.");
        }

        Piece? capturedPiece = null;
        var capturedPosition = validator.GetCapturedPiecePosition(move);
        var isCapture = validator.IsCaptureMove(move);

        board.RemovePiece(move.FromRow, move.FromCol);
        board.SetPiece(move.ToRow, move.ToCol, piece);

        if (isCapture && capturedPosition is not null)
        {
            capturedPiece = board.GetPiece(capturedPosition.Row, capturedPosition.Col)?.Clone();
            board.RemovePiece(capturedPosition.Row, capturedPosition.Col);
        }

        if (piece.Type == PieceType.Man)
        {
            var promoteRow = piece.Color == PlayerColor.White ? 0 : 7;
            if (move.ToRow == promoteRow)
            {
                piece.PromoteToKing();
            }
        }

        var toPosition = new CellPosition(move.ToRow, move.ToCol);
        var additionalCapture = isCapture && validator.CanPieceCapture(toPosition, currentPlayer);

        if (additionalCapture)
        {
            requiredCaptureFrom = toPosition;
        }
        else
        {
            requiredCaptureFrom = null;
            SwitchTurn();
        }

        return new MoveResult
        {
            Success = true,
            Message = additionalCapture ? "Продолжите взятие той же шашкой." : "Ход выполнен.",
            CapturedPiece = capturedPiece,
            IsCapture = isCapture,
            RequiresAdditionalCapture = additionalCapture
        };
    }

    public void SetConnectionError()
    {
        state = GameState.ConnectionError;
        GameStateChanged?.Invoke(state);
    }

    public Piece?[,] GetBoardSnapshot()
    {
        return board.GetSnapshot();
    }

    public PlayerColor GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public GameState GetState()
    {
        return state;
    }

    public CellPosition? GetRequiredCaptureCell()
    {
        return requiredCaptureFrom is null ? null : new CellPosition(requiredCaptureFrom.Row, requiredCaptureFrom.Col);
    }
}

