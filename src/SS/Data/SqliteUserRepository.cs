using Microsoft.Data.Sqlite;
using SS.Models;

namespace SS.Data;

public class SqliteUserRepository
{
    private readonly string _dbPath;

    public SqliteUserRepository(string dbPath)
    {
        _dbPath = dbPath;
    }

    public User? GetByUsername(string username)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, username, password_hash, role, created_at FROM users WHERE username = @username";
        cmd.Parameters.AddWithValue("@username", username);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                Role = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }

        return null;
    }

    public void Add(User user)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO users (username, password_hash, role)
            VALUES (@username, @password_hash, @role)";
        cmd.Parameters.AddWithValue("@username", user.Username);
        cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@role", user.Role);

        cmd.ExecuteNonQuery();
    }

    public bool HasUsers()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM users";

        long count = (long)cmd.ExecuteScalar()!;
        return count > 0;
    }
}
