using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using SS.Models;

namespace SS.Data;

public class SqliteCategoryRepository
{
    private readonly string _dbPath;

    public SqliteCategoryRepository(string dbPath)
    {
        _dbPath = dbPath;
        Initialize();
    }

    private void Initialize()
    {
        // Table is created via product repo init or separately
    }

    public ObservableCollection<Category> GetAll()
    {
        var result = new ObservableCollection<Category>();
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, description FROM categories";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }
        return result;
    }

    public void Add(Category category)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO categories (name, description) VALUES (@name, @description)";

        cmd.Parameters.AddWithValue("@name", category.Name);
        cmd.Parameters.AddWithValue("@description", (object?)category.Description ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public void Update(Category category)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE categories SET name = @name, description = @description WHERE id = @id";

        cmd.Parameters.AddWithValue("@id", category.Id);
        cmd.Parameters.AddWithValue("@name", category.Name);
        cmd.Parameters.AddWithValue("@description", (object?)category.Description ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM categories WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}