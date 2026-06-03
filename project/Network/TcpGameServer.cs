using System.Net;
using System.Net.Sockets;
using System.Text;
using project.Models;

namespace project.Network;

public class TcpGameServer : IDisposable
{
    private int port;
    private TcpListener? listener;
    private TcpClient? client;
    private StreamReader? reader;
    private StreamWriter? writer;
    private CancellationTokenSource? cancellationTokenSource;
    private bool isConnected;

    public event Action<NetworkMessage>? MessageReceived;
    public event Action? ConnectionLost;
    public event Action? ClientConnected;

    public bool IsConnected => isConnected;

    public void Start(int port)
    {
        Stop();

        this.port = port;
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        cancellationTokenSource = new CancellationTokenSource();

        _ = WaitForClient();
    }

    public async Task WaitForClient()
    {
        if (listener is null || cancellationTokenSource is null)
        {
            return;
        }

        try
        {
            client = await listener.AcceptTcpClientAsync(cancellationTokenSource.Token);
            var stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            isConnected = true;
            ClientConnected?.Invoke();

            await ReceiveLoop(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
            OnClientDisconnected();
        }
        catch (IOException)
        {
            OnClientDisconnected();
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
            OnClientDisconnected();
        }
        catch (SocketException)
        {
            OnClientDisconnected();
        }
        catch (IOException)
        {
            OnClientDisconnected();
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
                OnClientDisconnected();
                return null;
            }

            return NetworkMessage.FromJson(line);
        }
        catch (ObjectDisposedException)
        {
            OnClientDisconnected();
        }
        catch (SocketException)
        {
            OnClientDisconnected();
        }
        catch (IOException)
        {
            OnClientDisconnected();
        }

        return null;
    }

    public void Stop()
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

        try
        {
            listener?.Stop();
        }
        catch
        {
        }

        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        reader = null;
        writer = null;
        client = null;
        listener = null;
    }

    public void OnClientDisconnected()
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
        Stop();
    }
}

