using TicTacToe.Core.Entities;
using TicTacToe.Core.Enums;
using TicTacToe.Core.Services;

namespace TicTacToe.UnitTests
{
    public class GameEngineTests
    {
        private readonly GameEngine _engine;

        public GameEngineTests()
        {
            _engine = new GameEngine();
        }

        private Game CreateTestGame()
        {
            return new Game
            {
                Id = Guid.NewGuid(),
                PlayerXId = Guid.NewGuid(),
                PlayerOId = Guid.NewGuid(),
                Status = GameStatus.InProgress,
                CurrentTurn = CellState.X,
                Board = new CellState[9],
                LastUpdatedAt = DateTime.UtcNow.AddMinutes(-5) // to test if it gets updated
            };
        }

        [Fact]
        public void MakeMove_ValidMoveByX_ShouldUpdateBoardAndSwitchTurnToO()
        {
            var game = CreateTestGame();
            var previousUpdateTime = game.LastUpdatedAt;

            var result = _engine.MakeMove(game, game.PlayerXId, 0);

            Assert.True(result.IsSuccess);
            Assert.Equal(CellState.X, game.Board[0]);
            Assert.Equal(CellState.O, game.CurrentTurn);
            Assert.Equal(GameStatus.InProgress, game.Status);
            Assert.True(game.LastUpdatedAt > previousUpdateTime);
        }

        [Fact]
        public void MakeMove_ValidMoveByO_ShouldUpdateBoardAndSwitchTurnToX()
        {
            var game = CreateTestGame();
            game.CurrentTurn = CellState.O;

            var result = _engine.MakeMove(game, game.PlayerOId, 4);

            Assert.True(result.IsSuccess);
            Assert.Equal(CellState.O, game.Board[4]);
            Assert.Equal(CellState.X, game.CurrentTurn);
            Assert.Equal(GameStatus.InProgress, game.Status);
        }

        [Fact]
        public void MakeMove_InvalidPositionNegative_ShouldReturnFailure()
        {
            var game = CreateTestGame();
            var result = _engine.MakeMove(game, game.PlayerXId, -1);

            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid position. Must be between 0 and 8.", result.ErrorMessage);
        }

        [Fact]
        public void MakeMove_InvalidPositionTooLarge_ShouldReturnFailure()
        {
            var game = CreateTestGame();
            var result = _engine.MakeMove(game, game.PlayerXId, 9);

            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid position. Must be between 0 and 8.", result.ErrorMessage);
        }

        [Fact]
        public void MakeMove_NotPlayersTurn_ShouldReturnFailure()
        {
            var game = CreateTestGame();
            var result = _engine.MakeMove(game, game.PlayerOId, 0); // O's move on X's turn

            Assert.False(result.IsSuccess);
            Assert.Equal("It's not the player's turn.", result.ErrorMessage);
        }

        [Fact]
        public void MakeMove_PlayerNotPartOfGame_ShouldReturnFailure()
        {
            var game = CreateTestGame();
            var randomPlayerId = Guid.NewGuid();

            var result = _engine.MakeMove(game, randomPlayerId, 0);

            Assert.False(result.IsSuccess);
            Assert.Equal("Player is not part of this game.", result.ErrorMessage);
        }

        [Fact]
        public void MakeMove_CellAlreadyOccupied_ShouldReturnFailure()
        {
            var game = CreateTestGame();
            game.Board[0] = CellState.O;

            var result = _engine.MakeMove(game, game.PlayerXId, 0);

            Assert.False(result.IsSuccess);
            Assert.Equal("Cell is already occupied.", result.ErrorMessage);
        }

        [Fact]
        public void MakeMove_GameWaitingForOpponent_ShouldReturnFailure()
        {
            var game = CreateTestGame();
            game.Status = GameStatus.WaitingForOpponent;

            var result = _engine.MakeMove(game, game.PlayerXId, 0);

            Assert.False(result.IsSuccess);
            Assert.Equal("Game is not in progress.", result.ErrorMessage);
        }

        [Fact]
        public void MakeMove_GameAlreadyFinished_ShouldReturnFailure()
        {
            var game = CreateTestGame();
            game.Status = GameStatus.Finished;

            var result = _engine.MakeMove(game, game.PlayerXId, 0);

            Assert.False(result.IsSuccess);
            Assert.Equal("Game is not in progress.", result.ErrorMessage);
        }

        [Fact]
        public void MakeMove_WinningMoveHorizontal_ShouldEndGameWithWinner()
        {
            var game = CreateTestGame();
            game.Board[0] = CellState.X;
            game.Board[1] = CellState.X;
            // X is about to play at 2 (Top row: 0, 1, 2)

            var result = _engine.MakeMove(game, game.PlayerXId, 2);

            Assert.True(result.IsSuccess);
            Assert.Equal(GameStatus.Finished, game.Status);
            Assert.Equal(game.PlayerXId, game.WinnerId);
            Assert.True(result.IsGameFinished);
            Assert.Equal(game.PlayerXId, result.WinnerId);
            Assert.NotNull(game.FinishedAt);
        }

        [Fact]
        public void MakeMove_WinningMoveVertical_ShouldEndGameWithWinner()
        {
            var game = CreateTestGame();
            game.CurrentTurn = CellState.O;
            game.Board[1] = CellState.O;
            game.Board[4] = CellState.O;
            // O is about to play at 7 (Middle column: 1, 4, 7)

            var result = _engine.MakeMove(game, game.PlayerOId, 7);

            Assert.True(result.IsSuccess);
            Assert.Equal(GameStatus.Finished, game.Status);
            Assert.Equal(game.PlayerOId, game.WinnerId);
            Assert.True(result.IsGameFinished);
            Assert.Equal(game.PlayerOId, result.WinnerId);
        }

        [Fact]
        public void MakeMove_WinningMoveDiagonal1_ShouldEndGameWithWinner()
        {
            var game = CreateTestGame();
            game.Board[0] = CellState.X;
            game.Board[4] = CellState.X;
            // X is about to play at 8 (Diagonal: 0, 4, 8)

            var result = _engine.MakeMove(game, game.PlayerXId, 8);

            Assert.True(result.IsSuccess);
            Assert.Equal(GameStatus.Finished, game.Status);
            Assert.Equal(game.PlayerXId, game.WinnerId);
        }

        [Fact]
        public void MakeMove_WinningMoveDiagonal2_ShouldEndGameWithWinner()
        {
            var game = CreateTestGame();
            game.CurrentTurn = CellState.O;
            game.Board[2] = CellState.O;
            game.Board[4] = CellState.O;
            // O is about to play at 6 (Diagonal: 2, 4, 6)

            var result = _engine.MakeMove(game, game.PlayerOId, 6);

            Assert.True(result.IsSuccess);
            Assert.Equal(GameStatus.Finished, game.Status);
            Assert.Equal(game.PlayerOId, game.WinnerId);
        }

        [Fact]
        public void MakeMove_Draw_ShouldEndGameWithNoWinnerAndSetFinishedAt()
        {
            var game = CreateTestGame();
            // Setup a board that is 1 move away from a draw
            // X O X
            // X O O
            // O X _ (index 8 is empty)
            game.Board = new[]
            {
                CellState.X, CellState.O, CellState.X,
                CellState.X, CellState.O, CellState.O,
                CellState.O, CellState.X, CellState.Empty
            };
            
            var result = _engine.MakeMove(game, game.PlayerXId, 8);

            Assert.True(result.IsSuccess);
            Assert.Equal(GameStatus.Finished, game.Status);
            Assert.Null(game.WinnerId);
            Assert.True(result.IsGameFinished);
            Assert.Null(result.WinnerId);
            Assert.NotNull(game.FinishedAt);
        }
    }
}
