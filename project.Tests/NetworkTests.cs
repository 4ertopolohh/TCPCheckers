using System.Net;
using System.Net.Sockets;
using project.Models;
using project.Network;
using Xunit;

namespace project.Tests;

public class NetworkTests
{
    [Fact]
    public void MoveMessageShouldSerializeAndDeserializeWithFlatFields()
    {
        var message = NetworkMessage.CreateMoveMessage(new Move(5, 0, 4, 1));
        var restored = NetworkMessage.FromJson(message.ToJson());

        Assert.NotNull(restored);
        Assert.Equal("move", restored!.Type);
        Assert.Equal(5, restored.FromRow);
        Assert.Equal(0, restored.FromCol);
        Assert.Equal(4, restored.ToRow);
        Assert.Equal(1, restored.ToCol);
        Assert.Equal(new Move(5, 0, 4, 1).FromRow, restored.ToMove()!.FromRow);
    }

    [Fact]
    public void GameOverMessageShouldContainWinner()
    {
        var restored = NetworkMessage.FromJson(NetworkMessage.CreateGameOverMessage(PlayerColor.White).ToJson());

        Assert.NotNull(restored);
        Assert.Equal("gameOver", restored!.Type);
        Assert.Equal(PlayerColor.White, restored.Winner);
    }

    [Fact]
    public void ErrorMessageShouldContainText()
    {
        var restored = NetworkMessage.FromJson(NetworkMessage.CreateErrorMessage("Ошибка").ToJson());

        Assert.NotNull(restored);
        Assert.Equal("error", restored!.Type);
        Assert.Equal("Ошибка", restored.ErrorText);
    }

    [Fact]
    public void DisconnectMessageShouldContainReason()
    {
        var restored = NetworkMessage.FromJson(NetworkMessage.CreateDisconnectMessage("Соединение закрыто").ToJson());

        Assert.NotNull(restored);
        Assert.Equal("disconnect", restored!.Type);
        Assert.Equal("Соединение закрыто", restored.Reason);
    }

    [Fact]
    public void ConnectMessageShouldContainPlayerColor()
    {
        var restored = NetworkMessage.FromJson(NetworkMessage.CreateConnectMessage(PlayerColor.Black).ToJson());

        Assert.NotNull(restored);
        Assert.Equal("connect", restored!.Type);
        Assert.Equal(PlayerColor.Black, restored.PlayerColor);
    }

    [Fact]
    public async Task ServerShouldReceiveMoveOverLoopbackAsync()
    {
        using var server = new TcpGameServer();
        using var client = new TcpGameClient();
        var port = GetFreePort();
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = new TaskCompletionSource<NetworkMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.ClientConnected += () => connected.TrySetResult();
        server.MessageReceived += message => received.TrySetResult(message);

        server.Start(port);
        await client.Connect(IPAddress.Loopback.ToString(), port);
        await WaitAsync(connected.Task);

        await client.SendMessage(NetworkMessage.CreateMoveMessage(new Move(5, 0, 4, 1)));

        var message = await WaitAsync(received.Task);
        Assert.Equal("move", message.Type);
        Assert.Equal(5, message.FromRow);
        Assert.Equal(0, message.FromCol);
        Assert.Equal(4, message.ToRow);
        Assert.Equal(1, message.ToCol);
    }

    [Fact]
    public async Task ServerShouldRaiseConnectionLostWhenClientDisconnectsAsync()
    {
        using var server = new TcpGameServer();
        using var client = new TcpGameClient();
        var port = GetFreePort();
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lost = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        server.ClientConnected += () => connected.TrySetResult();
        server.ConnectionLost += () => lost.TrySetResult();

        server.Start(port);
        await client.Connect(IPAddress.Loopback.ToString(), port);
        await WaitAsync(connected.Task);

        client.Disconnect();

        await WaitAsync(lost.Task);
        Assert.False(server.IsConnected);
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task WaitAsync(Task task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(task, completed);
        await task;
    }

    private static async Task<T> WaitAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(task, completed);
        return await task;
    }
}
