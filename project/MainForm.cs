using System.Net;
using project.GameLogic;
using project.Models;
using project.Network;

namespace project;

public partial class MainForm : Form
{
    private readonly CheckersGame checkersGame;
    private readonly TcpGameServer tcpServer;
    private readonly TcpGameClient tcpClient;
    private readonly Button[,] boardButtons;
    private Image? whiteCheckerImage;
    private Image? blackCheckerImage;
    private Image? whiteKingCheckerImage;
    private Image? blackKingCheckerImage;
    private CellPosition? selectedCell;
    private bool isServerMode;
    private PlayerColor localPlayerColor;
    private bool isConnected;

    public MainForm()
    {
        InitializeComponent();

        checkersGame = new CheckersGame();
        tcpServer = new TcpGameServer();
        tcpClient = new TcpGameClient();
        boardButtons = new Button[8, 8];

        localPlayerColor = PlayerColor.White;
        isConnected = false;
        isServerMode = false;

        InitializeBoardButtons();
        LoadCheckerImages();
        BindEvents();
        SetDefaultValues();
        checkersGame.StartNewGame();

        ShowMessage("Выберите режим: создать сервер или подключиться к игре.");
        UpdateGameView();
    }

    private void BindEvents()
    {
        checkersGame.BoardChanged += OnBoardChanged;
        checkersGame.GameStateChanged += OnGameStateChanged;

        tcpServer.ClientConnected += OnServerClientConnected;
        tcpServer.MessageReceived += OnMessageReceived;
        tcpServer.ConnectionLost += HandleConnectionLost;

        tcpClient.Connected += OnClientConnected;
        tcpClient.MessageReceived += OnMessageReceived;
        tcpClient.ConnectionLost += HandleConnectionLost;

        FormClosing += MainForm_FormClosing;
    }

    private void SetDefaultValues()
    {
        ipAddressTextBox.Text = "127.0.0.1";
        portTextBox.Text = "5000";
        connectionStatusLabel.Text = "Состояние: не подключено";
        localPlayerLabel.Text = "Ваш цвет: не назначен";
        gameStateLabel.Text = "Состояние игры: ожидание";
    }

    private void InitializeBoardButtons()
    {
        boardPanel.Controls.Clear();

        for (var row = 0; row < 8; row++)
        {
            for (var col = 0; col < 8; col++)
            {
                var button = new Button
                {
                    Dock = DockStyle.Fill,
                    Margin = Padding.Empty,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(FontFamily.GenericSansSerif, 10F, FontStyle.Bold),
                    ImageAlign = ContentAlignment.MiddleCenter,
                    Tag = new CellPosition(row, col),
                    TextImageRelation = TextImageRelation.Overlay,
                    TabStop = false
                };

                button.FlatAppearance.BorderSize = 0;
                button.Click += BoardCell_Click;
                boardButtons[row, col] = button;
                boardPanel.Controls.Add(button, col, row);
            }
        }
    }

    public void StartServer()
    {
        if (!TryParsePort(out var port))
        {
            ShowMessage("Некорректный порт.");
            return;
        }

        try
        {
            isServerMode = true;
            localPlayerColor = PlayerColor.White;
            isConnected = false;
            selectedCell = null;

            checkersGame.StartNewGame();
            tcpServer.Start(port);

            startServerButton.Enabled = false;
            connectButton.Enabled = false;
            disconnectButton.Enabled = true;
            connectionStatusLabel.Text = "Состояние: сервер запущен";
            localPlayerLabel.Text = "Ваш цвет: Белые";
            gameStateLabel.Text = "Состояние игры: ожидание подключения";

            ShowMessage("Сервер запущен. Ожидание второго игрока.");
            UpdateGameView();
        }
        catch
        {
            ShowMessage("Не удалось запустить сервер.");
        }
    }

    public async Task ConnectToServer()
    {
        if (!IPAddress.TryParse(ipAddressTextBox.Text.Trim(), out _))
        {
            ShowMessage("Некорректный IP-адрес.");
            return;
        }

        if (!TryParsePort(out var port))
        {
            ShowMessage("Некорректный порт.");
            return;
        }

        try
        {
            isServerMode = false;
            localPlayerColor = PlayerColor.Black;
            selectedCell = null;
            checkersGame.StartNewGame();

            await tcpClient.Connect(ipAddressTextBox.Text.Trim(), port);

            startServerButton.Enabled = false;
            connectButton.Enabled = false;
            disconnectButton.Enabled = true;
            connectionStatusLabel.Text = "Состояние: подключено";
            localPlayerLabel.Text = "Ваш цвет: Черные";
            gameStateLabel.Text = "Состояние игры: ожидание подключения";

            await tcpClient.SendMessage(NetworkMessage.CreateConnectMessage(localPlayerColor));
            ShowMessage("Подключение выполнено. Ожидание подтверждения игры.");
            UpdateGameView();
        }
        catch
        {
            ShowMessage("Не удалось подключиться к серверу.");
        }
    }

    public void DrawBoard()
    {
        var snapshot = checkersGame.GetBoardSnapshot();

        for (var screenRow = 0; screenRow < 8; screenRow++)
        {
            for (var screenCol = 0; screenCol < 8; screenCol++)
            {
                var screenPosition = new CellPosition(screenRow, screenCol);
                var boardPosition = ScreenToBoardPosition(screenPosition);
                var button = boardButtons[screenRow, screenCol];
                var darkCell = (boardPosition.Row + boardPosition.Col) % 2 == 1;
                button.BackColor = darkCell ? Color.SaddleBrown : Color.Bisque;
                button.ForeColor = Color.Black;
                button.Text = string.Empty;
                button.Image = null;

                var piece = snapshot[boardPosition.Row, boardPosition.Col];
                if (piece is not null)
                {
                    var checkerImage = GetCheckerImage(piece);
                    if (checkerImage is not null)
                    {
                        button.Image = checkerImage;
                    }
                    else
                    {
                        button.Text = GetPieceFallbackText(piece);
                        button.ForeColor = piece.Color == PlayerColor.White ? Color.White : Color.Black;
                    }
                }

                if (selectedCell is not null)
                {
                    var selectedScreenCell = BoardToScreenPosition(selectedCell);
                    if (selectedScreenCell.Row == screenRow && selectedScreenCell.Col == screenCol)
                    {
                        button.BackColor = Color.Gold;
                    }
                }
            }
        }
    }

    public void SelectPiece(CellPosition position)
    {
        if (!position.IsValid())
        {
            return;
        }

        var piece = checkersGame.GetBoardSnapshot()[position.Row, position.Col];
        if (piece is null)
        {
            ShowMessage("Выберите шашку для хода.");
            return;
        }

        if (piece.Color != localPlayerColor)
        {
            ShowMessage("Нельзя ходить чужой шашкой.");
            return;
        }

        if (checkersGame.GetCurrentPlayer() != localPlayerColor)
        {
            ShowMessage("Ожидание хода соперника.");
            return;
        }

        selectedCell = position;
        DrawBoard();
    }

    public async Task MakeMove(CellPosition target)
    {
        if (selectedCell is null)
        {
            return;
        }

        var move = new Move(selectedCell.Row, selectedCell.Col, target.Row, target.Col);
        var result = checkersGame.TryMakeMove(move);

        if (!result.Success)
        {
            selectedCell = null;
            ShowMessage(result.Message);
            DrawBoard();
            UpdateGameView();
            return;
        }

        if (result.RequiresAdditionalCapture)
        {
            selectedCell = new CellPosition(target.Row, target.Col);
            ShowMessage("Продолжите взятие той же шашкой.");
        }
        else
        {
            selectedCell = null;
            ShowMessage("Ход выполнен.");
        }

        await SendNetworkMessage(NetworkMessage.CreateMoveMessage(move));

        if (result.Winner.HasValue)
        {
            await SendNetworkMessage(NetworkMessage.CreateGameOverMessage(result.Winner.Value));
            ShowMessage($"Партия завершена. Победитель: {GetColorText(result.Winner.Value)}.");
        }

        DrawBoard();
        UpdateGameView();
    }

    public void ShowMessage(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => ShowMessage(message)));
            return;
        }

        messagesListBox.Items.Add($"{DateTime.Now:HH:mm:ss} {message}");
        messagesListBox.TopIndex = messagesListBox.Items.Count - 1;
    }

    public void UpdateGameView()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(UpdateGameView));
            return;
        }

        DrawBoard();

        var currentPlayer = checkersGame.GetCurrentPlayer();
        currentPlayerLabel.Text = $"Текущий игрок: {GetColorText(currentPlayer)}";
        localPlayerLabel.Text = isConnected || isServerMode
            ? $"Ваш цвет: {GetColorText(localPlayerColor)}"
            : "Ваш цвет: не назначен";

        if (!isConnected)
        {
            if (isServerMode)
            {
                connectionStatusLabel.Text = "Состояние: ожидание подключения";
            }
            else
            {
                connectionStatusLabel.Text = "Состояние: не подключено";
            }
        }
        else
        {
            connectionStatusLabel.Text = "Состояние: подключено";
        }

        var gameState = checkersGame.GetState();
        gameStateLabel.Text = $"Состояние игры: {GetGameStateText(gameState)}";

        var canMove = isConnected && gameState == GameState.Playing && currentPlayer == localPlayerColor;
        boardPanel.Enabled = canMove;

        if (isConnected && gameState == GameState.Playing)
        {
            ShowTurnMessage(canMove);
        }
    }

    public void HandleConnectionLost()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(HandleConnectionLost));
            return;
        }

        isConnected = false;
        selectedCell = null;
        checkersGame.SetConnectionError();

        SafeStopNetwork();
        startServerButton.Enabled = true;
        connectButton.Enabled = true;
        disconnectButton.Enabled = false;
        boardPanel.Enabled = false;

        ShowMessage("Соединение со вторым игроком потеряно.");
        UpdateGameView();
    }

    private async Task SendNetworkMessage(NetworkMessage message)
    {
        if (!isConnected)
        {
            return;
        }

        try
        {
            if (isServerMode)
            {
                await tcpServer.SendMessage(message);
            }
            else
            {
                await tcpClient.SendMessage(message);
            }
        }
        catch
        {
            HandleConnectionLost();
        }
    }

    private void OnMessageReceived(NetworkMessage message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnMessageReceived(message)));
            return;
        }

        switch (message.Type)
        {
            case "connect":
                HandleConnectMessage(message);
                break;
            case "move":
                ApplyRemoteMove(message);
                break;
            case "gameOver":
                if (message.Winner.HasValue)
                {
                    ShowMessage($"Партия завершена. Победитель: {GetColorText(message.Winner.Value)}.");
                }
                UpdateGameView();
                break;
            case "error":
                ShowMessage(message.ErrorText ?? "Получена сетевая ошибка.");
                break;
            case "disconnect":
                HandleConnectionLost();
                break;
            default:
                ShowMessage("Получено неизвестное сетевое сообщение.");
                break;
        }
    }

    private void ApplyRemoteMove(NetworkMessage message)
    {
        var move = message.ToMove();
        if (move is null)
        {
            ShowMessage("Получено неизвестное сетевое сообщение.");
            return;
        }

        var result = checkersGame.TryMakeMove(move);
        if (!result.Success)
        {
            ShowMessage("Получен некорректный ход соперника.");
            return;
        }

        selectedCell = null;

        if (result.Winner.HasValue)
        {
            ShowMessage($"Партия завершена. Победитель: {GetColorText(result.Winner.Value)}.");
        }

        UpdateGameView();
    }

    private void OnServerClientConnected()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(OnServerClientConnected));
            return;
        }

        isConnected = true;
        localPlayerColor = PlayerColor.White;
        connectionStatusLabel.Text = "Состояние: подключено";
        localPlayerLabel.Text = "Ваш цвет: Белые";
        ShowMessage("TCP-подключение установлено. Ожидание сообщения connect.");
        UpdateGameView();
    }

    private void OnClientConnected()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(OnClientConnected));
            return;
        }

        isConnected = true;
        localPlayerColor = PlayerColor.Black;
        connectionStatusLabel.Text = "Состояние: подключено";
        localPlayerLabel.Text = "Ваш цвет: Черные";
        UpdateGameView();
    }

    private void OnBoardChanged()
    {
        UpdateGameView();
    }

    private void OnGameStateChanged(GameState state)
    {
        UpdateGameView();
    }

    private void ShowTurnMessage(bool canMove)
    {
        var expected = canMove ? "Ваш ход." : "Ожидание хода соперника.";
        if (messagesListBox.Items.Count == 0)
        {
            ShowMessage(expected);
            return;
        }

        var last = messagesListBox.Items[messagesListBox.Items.Count - 1]?.ToString() ?? string.Empty;
        if (!last.Contains(expected, StringComparison.Ordinal))
        {
            ShowMessage(expected);
        }
    }

    private void LoadCheckerImages()
    {
        var whiteImagePath = Path.Combine(AppContext.BaseDirectory, "design", "whiteChecker.png");
        var blackImagePath = Path.Combine(AppContext.BaseDirectory, "design", "blackChecker.png");
        var whiteKingImagePath = Path.Combine(AppContext.BaseDirectory, "design", "whiteKingChecker.png");
        var blackKingImagePath = Path.Combine(AppContext.BaseDirectory, "design", "blackKingChecker.png");

        try
        {
            if (!File.Exists(whiteImagePath) ||
                !File.Exists(blackImagePath) ||
                !File.Exists(whiteKingImagePath) ||
                !File.Exists(blackKingImagePath))
            {
                ShowMessage("Не удалось загрузить изображения шашек. Будет использовано текстовое отображение.");
                return;
            }

            using var whiteSource = Image.FromFile(whiteImagePath);
            using var blackSource = Image.FromFile(blackImagePath);
            using var whiteKingSource = Image.FromFile(whiteKingImagePath);
            using var blackKingSource = Image.FromFile(blackKingImagePath);

            var targetWidth = GetCheckerImageWidth();
            var targetHeight = GetCheckerImageHeight();

            whiteCheckerImage = ResizeCheckerImage(whiteSource, targetWidth, targetHeight);
            blackCheckerImage = ResizeCheckerImage(blackSource, targetWidth, targetHeight);
            whiteKingCheckerImage = ResizeCheckerImage(whiteKingSource, targetWidth, targetHeight);
            blackKingCheckerImage = ResizeCheckerImage(blackKingSource, targetWidth, targetHeight);
        }
        catch
        {
            DisposeCheckerImages();
            ShowMessage("Не удалось загрузить изображения шашек. Будет использовано текстовое отображение.");
        }
    }

    private Image? GetCheckerImage(Piece piece)
    {
        if (piece.Type == PieceType.King)
        {
            return piece.Color == PlayerColor.White ? whiteKingCheckerImage : blackKingCheckerImage;
        }

        return piece.Color == PlayerColor.White ? whiteCheckerImage : blackCheckerImage;
    }

    private CellPosition ScreenToBoardPosition(CellPosition screenPosition)
    {
        if (!ShouldFlipBoard())
        {
            return new CellPosition(screenPosition.Row, screenPosition.Col);
        }

        return new CellPosition(7 - screenPosition.Row, 7 - screenPosition.Col);
    }

    private CellPosition BoardToScreenPosition(CellPosition boardPosition)
    {
        if (!ShouldFlipBoard())
        {
            return new CellPosition(boardPosition.Row, boardPosition.Col);
        }

        return new CellPosition(7 - boardPosition.Row, 7 - boardPosition.Col);
    }

    private bool ShouldFlipBoard()
    {
        return isConnected && !isServerMode && localPlayerColor == PlayerColor.Black;
    }

    private int GetCheckerImageWidth()
    {
        return Math.Max(1, (int)boardPanel.ColumnStyles[0].Width - 12);
    }

    private int GetCheckerImageHeight()
    {
        return Math.Max(1, (int)boardPanel.RowStyles[0].Height - 12);
    }

    private static Image ResizeCheckerImage(Image source, int width, int height)
    {
        var resizedImage = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(resizedImage);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);
        graphics.DrawImage(source, 0, 0, width, height);
        return resizedImage;
    }

    private void DisposeCheckerImages()
    {
        whiteCheckerImage?.Dispose();
        blackCheckerImage?.Dispose();
        whiteKingCheckerImage?.Dispose();
        blackKingCheckerImage?.Dispose();
        whiteCheckerImage = null;
        blackCheckerImage = null;
        whiteKingCheckerImage = null;
        blackKingCheckerImage = null;
    }

    private static string GetPieceFallbackText(Piece piece)
    {
        if (piece.Color == PlayerColor.White)
        {
            return piece.Type == PieceType.King ? "WK" : "W";
        }

        return piece.Type == PieceType.King ? "BK" : "B";
    }

    private static string GetColorText(PlayerColor color)
    {
        return color == PlayerColor.White ? "Белые" : "Черные";
    }

    private static string GetGameStateText(GameState state)
    {
        return state switch
        {
            GameState.WaitingForConnection => "ожидание подключения",
            GameState.Playing => "идет партия",
            GameState.WhiteWon => "победа белых",
            GameState.BlackWon => "победа черных",
            GameState.ConnectionError => "ошибка соединения",
            _ => "неизвестно"
        };
    }

    private bool TryParsePort(out int port)
    {
        port = 0;
        return int.TryParse(portTextBox.Text.Trim(), out port) && port > 0 && port <= 65535;
    }

    private async void startServerButton_Click(object? sender, EventArgs e)
    {
        StartServer();
        await Task.CompletedTask;
    }

    private async void connectButton_Click(object? sender, EventArgs e)
    {
        await ConnectToServer();
    }

    private async void disconnectButton_Click(object? sender, EventArgs e)
    {
        if (isConnected)
        {
            await SendNetworkMessage(NetworkMessage.CreateDisconnectMessage("Игрок отключился."));
        }

        SafeStopNetwork();
        isConnected = false;
        isServerMode = false;
        selectedCell = null;
        checkersGame.SetConnectionError();

        startServerButton.Enabled = true;
        connectButton.Enabled = true;
        disconnectButton.Enabled = false;

        ShowMessage("Соединение завершено.");
        UpdateGameView();
    }

    private void newGameButton_Click(object? sender, EventArgs e)
    {
        if (isConnected)
        {
            ShowMessage("Для новой игры сначала отключитесь.");
            return;
        }

        selectedCell = null;
        checkersGame.StartNewGame();
        ShowMessage("Новая игра подготовлена.");
        UpdateGameView();
    }

    private async void BoardCell_Click(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.Tag is not CellPosition screenPosition)
        {
            return;
        }

        if (!isConnected || checkersGame.GetState() != GameState.Playing)
        {
            return;
        }

        if (checkersGame.GetCurrentPlayer() != localPlayerColor)
        {
            ShowMessage("Ожидание хода соперника.");
            return;
        }

        var boardPosition = ScreenToBoardPosition(screenPosition);

        if (selectedCell is null)
        {
            SelectPiece(boardPosition);
            return;
        }

        if (selectedCell.Row == boardPosition.Row && selectedCell.Col == boardPosition.Col)
        {
            selectedCell = null;
            DrawBoard();
            return;
        }

        await MakeMove(boardPosition);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        SafeStopNetwork();
        DisposeCheckerImages();
    }

    private void HandleConnectMessage(NetworkMessage message)
    {
        if (!message.PlayerColor.HasValue)
        {
            ShowMessage("Получено некорректное сообщение connect.");
            return;
        }

        if (checkersGame.GetState() != GameState.WaitingForConnection)
        {
            return;
        }

        if (isServerMode)
        {
            localPlayerColor = PlayerColor.White;
            checkersGame.BeginConnectedGame();
            _ = SendNetworkMessage(NetworkMessage.CreateConnectMessage(localPlayerColor));
        }
        else
        {
            localPlayerColor = PlayerColor.Black;
            checkersGame.BeginConnectedGame();
        }

        ShowMessage("Подключение выполнено. Игра начинается.");
        UpdateGameView();
    }

    private void SafeStopNetwork()
    {
        try
        {
            tcpServer.Stop();
        }
        catch
        {
        }

        try
        {
            tcpClient.Disconnect();
        }
        catch
        {
        }
    }
}
