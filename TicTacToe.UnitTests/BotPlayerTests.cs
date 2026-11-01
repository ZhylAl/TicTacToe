using TicTacToe.Core.Enums;
using TicTacToe.Core.Services;

namespace TicTacToe.UnitTests;

public class BotPlayerTests
{
    private readonly BotPlayer _bot = new();

    [Fact]
    public void GetMove_Easy_ShouldReturnAnEmptyCell()
    {
        // Arrange
        var board = new[]
        {
            CellState.X,     CellState.O,     CellState.X,
            CellState.Empty, CellState.Empty, CellState.Empty,
            CellState.Empty, CellState.Empty, CellState.Empty
        };

        // Act
        var move = _bot.GetMove(board, CellState.O, BotDifficulty.Easy);

        // Assert: Move must be in an empty cell
        Assert.True(move >= 3 && move <= 8);
        Assert.Equal(CellState.Empty, board[move]);
    }

    [Fact]
    public void GetMove_Medium_WhenBotCanWinImmediately_ShouldChooseWinningMove()
    {
        // Arrange: Bot (O) has 2 in a row [0, 1]. Cell 2 wins!
        // O O _
        // X X _
        // _ _ _
        var board = new[]
        {
            CellState.O,     CellState.O,     CellState.Empty,
            CellState.X,     CellState.X,     CellState.Empty,
            CellState.Empty, CellState.Empty, CellState.Empty
        };

        // Act
        var move = _bot.GetMove(board, CellState.O, BotDifficulty.Medium);

        // Assert: Bot must take cell 2 to win
        Assert.Equal(2, move);
    }

    [Fact]
    public void GetMove_Medium_WhenOpponentCanWin_ShouldBlockOpponent()
    {
        // Arrange: Human (X) has [0, 4] and threatens to win at 8
        // X _ _
        // _ X _
        // _ _ _ (index 8 must be blocked by O)
        var board = new[]
        {
            CellState.X,     CellState.Empty, CellState.Empty,
            CellState.Empty, CellState.X,     CellState.Empty,
            CellState.Empty, CellState.Empty, CellState.Empty
        };

        // Act
        var move = _bot.GetMove(board, CellState.O, BotDifficulty.Medium);

        // Assert: Bot must block diagonal at index 8
        Assert.Equal(8, move);
    }

    [Fact]
    public void GetMove_Medium_WhenCenterIsFree_ShouldTakeCenter()
    {
        // Arrange: Center (index 4) is free
        var board = new[]
        {
            CellState.X,     CellState.Empty, CellState.Empty,
            CellState.Empty, CellState.Empty, CellState.Empty,
            CellState.Empty, CellState.Empty, CellState.Empty
        };

        // Act
        var move = _bot.GetMove(board, CellState.O, BotDifficulty.Medium);

        // Assert: Bot should take center (4)
        Assert.Equal(4, move);
    }

    [Fact]
    public void GetMove_Hard_WhenBotCanWin_ShouldChooseWinningMove()
    {
        // Arrange: Bot (O) can win on column [1, 4, 7]
        // _ O _
        // _ O _
        // _ _ _ (cell 7 is winning)
        var board = new[]
        {
            CellState.Empty, CellState.O,     CellState.Empty,
            CellState.Empty, CellState.O,     CellState.Empty,
            CellState.X,     CellState.Empty, CellState.X
        };

        // Act
        var move = _bot.GetMove(board, CellState.O, BotDifficulty.Hard);

        // Assert
        Assert.Equal(7, move);
    }

    [Fact]
    public void GetMove_Hard_BotVsBot_ShouldAlwaysEndInDraw()
    {
        // Arrange: Two unbeatable bots playing against each other from an empty board
        var board = new CellState[9];
        var engine = new GameEngine();
        var game = new TicTacToe.Core.Entities.Game
        {
            PlayerXId = Guid.NewGuid(),
            PlayerOId = Guid.NewGuid(),
            Status = GameStatus.InProgress,
            Board = board
        };

        // Act: Play full match until finished
        while (game.Status == GameStatus.InProgress)
        {
            var currentSymbol = game.CurrentTurn;
            var currentUserId = currentSymbol == CellState.X ? game.PlayerXId : game.PlayerOId.Value;

            var botMove = _bot.GetMove(game.Board, currentSymbol, BotDifficulty.Hard);
            var result = engine.MakeMove(game, currentUserId, botMove);

            Assert.True(result.IsSuccess);
        }

        // Assert: Minimax vs Minimax MUST ALWAYS be a draw!
        Assert.Equal(GameStatus.Finished, game.Status);
        Assert.Null(game.WinnerId);
    }

    [Fact]
    public void GetMove_FullBoard_ShouldThrowInvalidOperationException()
    {
        // Arrange: Board has no empty cells
        var board = new[]
        {
            CellState.X, CellState.O, CellState.X,
            CellState.X, CellState.O, CellState.O,
            CellState.O, CellState.X, CellState.X
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _bot.GetMove(board, CellState.X, BotDifficulty.Easy));
    }
}