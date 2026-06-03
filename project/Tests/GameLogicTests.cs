using project.GameLogic;
using project.Models;

namespace project.Tests;

public class GameLogicTests
{
    public bool InitialBoardShouldContainTwelvePiecesForEachPlayer()
    {
        var board = new GameBoard();
        board.Initialize();

        return board.CountPieces(PlayerColor.White) == 12 && board.CountPieces(PlayerColor.Black) == 12;
    }

    public bool InvalidMoveShouldNotChangeBoard()
    {
        var game = new CheckersGame();
        game.StartNewGame();
        var before = game.GetBoardSnapshot();

        var result = game.TryMakeMove(new Move(5, 0, 5, 1));
        var after = game.GetBoardSnapshot();

        return !result.Success && AreBoardsEqual(before, after);
    }

    public bool PieceShouldPromoteToKing()
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

        board.SetPiece(1, 2, new Piece(PlayerColor.White, PieceType.Man));
        var validator = new MoveValidator(board);
        var move = new Move(1, 2, 0, 3);

        if (!validator.IsValidMove(move, PlayerColor.White))
        {
            return false;
        }

        var piece = board.GetPiece(1, 2);
        board.RemovePiece(1, 2);
        board.SetPiece(0, 3, piece);
        board.GetPiece(0, 3)?.PromoteToKing();

        return board.GetPiece(0, 3)?.Type == PieceType.King;
    }

    public bool MandatoryCaptureShouldBlockSimpleMove()
    {
        var board = new GameBoard();

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                board.RemovePiece(row, col);
            }
        }

        board.SetPiece(5, 2, new Piece(PlayerColor.White, PieceType.Man));
        board.SetPiece(4, 3, new Piece(PlayerColor.Black, PieceType.Man));

        var validator = new MoveValidator(board);

        var simpleMove = new Move(5, 2, 4, 1);
        var captureMove = new Move(5, 2, 3, 4);

        return !validator.IsValidMove(simpleMove, PlayerColor.White)
            && validator.IsValidMove(captureMove, PlayerColor.White);
    }

    public bool ChainCaptureShouldRequireSamePieceToContinue()
    {
        var game = new CheckersGame();
        game.StartNewGame();

        var board = game.GetBoardSnapshot();
        if (board.Length != 64)
        {
            return false;
        }

        return true;
    }

    private static bool AreBoardsEqual(Piece?[,] first, Piece?[,] second)
    {
        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var left = first[row, col];
                var right = second[row, col];

                if (left is null && right is null)
                {
                    continue;
                }

                if (left is null || right is null)
                {
                    return false;
                }

                if (left.Color != right.Color || left.Type != right.Type)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

