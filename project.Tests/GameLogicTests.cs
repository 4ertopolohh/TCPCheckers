using project.GameLogic;
using project.Models;
using Xunit;

namespace project.Tests;

public class GameLogicTests
{
    [Fact]
    public void InitialBoardShouldContainTwelvePiecesForEachPlayer()
    {
        var game = new CheckersGame();
        var board = game.GetBoardSnapshot();

        Assert.Equal(12, CountPieces(board, PlayerColor.White));
        Assert.Equal(12, CountPieces(board, PlayerColor.Black));
    }

    [Fact]
    public void StartNewGameShouldLeaveWaitingForConnection()
    {
        var game = new CheckersGame();

        game.StartNewGame();

        Assert.Equal(GameState.WaitingForConnection, game.GetState());
        Assert.Equal(PlayerColor.White, game.GetCurrentPlayer());
    }

    [Fact]
    public void BeginConnectedGameShouldSwitchStateToPlaying()
    {
        var game = new CheckersGame();

        game.BeginConnectedGame();

        Assert.Equal(GameState.Playing, game.GetState());
        Assert.Equal(PlayerColor.White, game.GetCurrentPlayer());
    }

    [Fact]
    public void SetConnectionErrorShouldSwitchState()
    {
        var game = new CheckersGame();

        game.BeginConnectedGame();
        game.SetConnectionError();

        Assert.Equal(GameState.ConnectionError, game.GetState());
    }

    [Fact]
    public void InvalidMoveShouldNotChangeBoard()
    {
        var game = new CheckersGame();
        game.BeginConnectedGame();
        var before = game.GetBoardSnapshot();

        var result = game.TryMakeMove(new Move(5, 0, 5, 1));
        var after = game.GetBoardSnapshot();

        Assert.False(result.Success);
        AssertBoardsEqual(before, after);
    }

    [Fact]
    public void MandatoryCaptureShouldBlockSimpleMove()
    {
        var board = CreateEmptyBoard();
        board.SetPiece(5, 2, new Piece(PlayerColor.White, PieceType.Man));
        board.SetPiece(4, 3, new Piece(PlayerColor.Black, PieceType.Man));
        var game = CreateGame(board);

        var simpleMove = game.TryMakeMove(new Move(5, 2, 4, 1));
        var captureMove = game.TryMakeMove(new Move(5, 2, 3, 4));

        Assert.False(simpleMove.Success);
        Assert.True(captureMove.Success);
        Assert.True(captureMove.IsCapture);
    }

    [Fact]
    public void ChainCaptureShouldRequireSamePieceToContinue()
    {
        var board = CreateEmptyBoard();
        board.SetPiece(5, 0, new Piece(PlayerColor.White, PieceType.Man));
        board.SetPiece(5, 6, new Piece(PlayerColor.White, PieceType.Man));
        board.SetPiece(4, 1, new Piece(PlayerColor.Black, PieceType.Man));
        board.SetPiece(2, 3, new Piece(PlayerColor.Black, PieceType.Man));
        var game = CreateGame(board);

        var firstCapture = game.TryMakeMove(new Move(5, 0, 3, 2));

        Assert.True(firstCapture.Success);
        Assert.True(firstCapture.RequiresAdditionalCapture);
        Assert.Equal(new CellPosition(3, 2), game.GetRequiredCaptureCell());

        var wrongPieceMove = game.TryMakeMove(new Move(5, 6, 4, 5));
        Assert.False(wrongPieceMove.Success);

        var secondCapture = game.TryMakeMove(new Move(3, 2, 1, 4));
        Assert.True(secondCapture.Success);
        Assert.False(secondCapture.RequiresAdditionalCapture);
        Assert.Equal(PlayerColor.Black, game.GetCurrentPlayer());
    }

    [Fact]
    public void PieceShouldPromoteToKing()
    {
        var board = CreateEmptyBoard();
        board.SetPiece(1, 2, new Piece(PlayerColor.White, PieceType.Man));
        var game = CreateGame(board);

        var result = game.TryMakeMove(new Move(1, 2, 0, 3));
        var piece = game.GetBoardSnapshot()[0, 3];

        Assert.True(result.Success);
        Assert.NotNull(piece);
        Assert.Equal(PieceType.King, piece!.Type);
    }

    [Fact]
    public void PlayerShouldWinWhenOpponentHasNoPieces()
    {
        var board = CreateEmptyBoard();
        board.SetPiece(5, 0, new Piece(PlayerColor.White, PieceType.Man));
        var game = CreateGame(board);

        var result = game.TryMakeMove(new Move(5, 0, 4, 1));

        Assert.True(result.Success);
        Assert.Equal(PlayerColor.White, result.Winner);
        Assert.Equal(GameState.WhiteWon, game.GetState());
    }

    [Fact]
    public void PlayerShouldWinWhenOpponentHasNoAvailableMoves()
    {
        var board = CreateEmptyBoard();
        board.SetPiece(0, 1, new Piece(PlayerColor.White, PieceType.Man));
        board.SetPiece(6, 1, new Piece(PlayerColor.Black, PieceType.Man));
        var game = CreateGame(board, PlayerColor.Black);

        var result = game.TryMakeMove(new Move(6, 1, 7, 0));

        Assert.True(result.Success);
        Assert.Equal(PlayerColor.Black, result.Winner);
        Assert.Equal(GameState.BlackWon, game.GetState());
    }

    private static CheckersGame CreateGame(
        GameBoard board,
        PlayerColor currentPlayer = PlayerColor.White,
        GameState state = GameState.Playing,
        CellPosition? requiredCaptureFrom = null)
    {
        return new CheckersGame(board, currentPlayer, state, requiredCaptureFrom);
    }

    private static GameBoard CreateEmptyBoard()
    {
        var board = new GameBoard();
        board.Initialize();

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                board.RemovePiece(row, col);
            }
        }

        return board;
    }

    private static int CountPieces(Piece?[,] board, PlayerColor color)
    {
        var count = 0;

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                if (board[row, col]?.Color == color)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void AssertBoardsEqual(Piece?[,] expected, Piece?[,] actual)
    {
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var left = expected[row, col];
                var right = actual[row, col];

                if (left is null && right is null)
                {
                    continue;
                }

                Assert.NotNull(left);
                Assert.NotNull(right);
                Assert.Equal(left!.Color, right!.Color);
                Assert.Equal(left.Type, right.Type);
            }
        }
    }
}
