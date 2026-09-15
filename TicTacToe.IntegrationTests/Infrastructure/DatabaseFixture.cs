using Testcontainers.PostgreSql;

namespace TicTacToe.IntegrationTests.Infrastructure
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgreSqlContainer;

        public DatabaseFixture()
        {
            _postgreSqlContainer = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .WithDatabase("tictactoe_test")
                .WithUsername("test_user")
                .WithPassword("test_password")
                .Build();
        }

        public string GetConnectionString()
        {
            return _postgreSqlContainer.GetConnectionString();
        }

        public async Task InitializeAsync()
        {
            await _postgreSqlContainer.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgreSqlContainer.DisposeAsync();
        }
    }
}
