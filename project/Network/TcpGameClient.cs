using System.Net.Sockets;
using System.Text;
using project.Models;

namespace project.Network;

public class TcpGameClient : IDisposable
{
    private string ipAddress = string.Empty;
    private int port;
    private TcpClient? client;
    private StreamReader? reader;
    private StreamWriter? writer;
    private CancellationTokenSource? cancellationTokenSource;
    private bool isConnected;

    public event Action<NetworkMessage>? MessageReceived;
    public event Action? ConnectionLost;
    public event Action? Connected;

    public bool IsConnected => isConnected;

    public async Task Connect(string ip, int port)
    {
        Disconnect();

        ipAddress = ip;
        this.port = port;
        client = new TcpClient();
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await client.ConnectAsync(ip, port);
            var stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            isConnected = true;
            Connected?.Invoke();

            _ = ReceiveLoop(cancellationTokenSource.Token);
        }
        catch (SocketException)
        {
            OnConnectionLost();
            throw;
        }
        catch (IOException)
        {
            OnConnectionLost();
            throw;
        }
        catch (ObjectDisposedException)
        {
            OnConnectionLost();
            throw;
        }
    }

    public async Task SendMessage(NetworkMessage message)
    {
        if (!isConnected || writer is null)
        {
            return;
        }

        try
        {
            await writer.WriteLineAsync(message.ToJson());
        }
        catch (ObjectDisposedException)
        {
            OnConnectionLost();
        }
        catch (SocketException)
        {
            OnConnectionLost();
        }
        catch (IOException)
        {
            OnConnectionLost();
        }
    }

    public async Task<NetworkMessage?> ReceiveMessage()
    {
        if (!isConnected || reader is null)
        {
            return null;
        }

        try
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                OnConnectionLost();
                return null;
            }

            return NetworkMessage.FromJson(line);
        }
        catch (ObjectDisposedException)
        {
            OnConnectionLost();
        }
        catch (SocketException)
        {
            OnConnectionLost();
        }
        catch (IOException)
        {
            OnConnectionLost();
        }

        return null;
    }

    public void Disconnect()
    {
        isConnected = false;

        try
        {
            cancellationTokenSource?.Cancel();
        }
        catch
        {
        }

        try
        {
            reader?.Dispose();
        }
        catch
        {
        }

        try
        {
            writer?.Dispose();
        }
        catch
        {
        }

        try
        {
            client?.Close();
        }
        catch
        {
        }

        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        reader = null;
        writer = null;
        client = null;
    }

    public void OnConnectionLost()
    {
        if (!isConnected)
        {
            return;
        }

        isConnected = false;
        ConnectionLost?.Invoke();
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && isConnected)
        {
            var message = await ReceiveMessage();
            if (message is null)
            {
                break;
            }

            MessageReceived?.Invoke(message);
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}

