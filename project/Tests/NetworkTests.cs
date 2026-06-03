using project.Models;

namespace project.Tests;

public class NetworkTests
{
    public bool MoveMessageShouldSerializeAndDeserialize()
    {
        var message = NetworkMessage.CreateMoveMessage(new Move(5, 0, 4, 1));
        var json = message.ToJson();
        var restored = NetworkMessage.FromJson(json);

        return restored is not null
            && restored.Type == "move"
            && restored.Move is not null
            && restored.Move.FromRow == 5
            && restored.Move.FromCol == 0
            && restored.Move.ToRow == 4
            && restored.Move.ToCol == 1;
    }

    public bool GameOverMessageShouldContainWinner()
    {
        var message = NetworkMessage.CreateGameOverMessage(PlayerColor.White);
        var json = message.ToJson();
        var restored = NetworkMessage.FromJson(json);

        return restored is not null
            && restored.Type == "gameOver"
            && restored.Winner == PlayerColor.White;
    }

    public bool ErrorMessageShouldContainText()
    {
        var message = NetworkMessage.CreateErrorMessage("Ошибка");
        var json = message.ToJson();
        var restored = NetworkMessage.FromJson(json);

        return restored is not null
            && restored.Type == "error"
            && restored.ErrorText == "Ошибка";
    }

    public bool DisconnectMessageShouldContainReason()
    {
        var message = NetworkMessage.CreateDisconnectMessage("Соединение закрыто");
        var json = message.ToJson();
        var restored = NetworkMessage.FromJson(json);

        return restored is not null
            && restored.Type == "disconnect"
            && restored.ErrorText == "Соединение закрыто";
    }

    public bool UnknownMessageTypeShouldStayReadable()
    {
        var json = "{\"type\":\"unknown\",\"errorText\":\"x\"}";
        var restored = NetworkMessage.FromJson(json);

        return restored is not null
            && restored.Type == "unknown"
            && restored.ErrorText == "x";
    }
}

