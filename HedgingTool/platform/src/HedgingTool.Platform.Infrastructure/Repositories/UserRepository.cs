using HedgingTool.Platform.Application.Interfaces;
using HedgingTool.Platform.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HedgingTool.Platform.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HedgingDb")
            ?? throw new InvalidOperationException("Connection string 'HedgingDb' was not found.");
    }

    public async Task<User?> GetActiveByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1)
                id,
                email,
                name,
                role,
                password_hash,
                is_active
            FROM dbo.users
            WHERE email = @email AND is_active = 1;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@email", email.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new User
        {
            Id = reader.GetGuid(0),
            Email = reader.GetString(1),
            Name = reader.GetString(2),
            Role = reader.GetString(3),
            PasswordHash = reader.GetString(4),
            IsActive = reader.GetBoolean(5)
        };
    }
}
