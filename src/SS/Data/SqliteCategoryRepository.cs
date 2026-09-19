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
    }

    public ObservableCollection<Category> GetAll()
    {
        var result = new ObservableCollection<Category>();
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, icon, color FROM categories ORDER BY id";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Icon = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Color = reader.IsDBNull(3) ? "#7B1FA2" : reader.GetString(3)
            });
        }
        return result;
    }

    public Category? GetById(int id)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, icon, color FROM categories WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Icon = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Color = reader.IsDBNull(3) ? "#7B1FA2" : reader.GetString(3)
            };
        }
        return null;
    }

    public void Add(Category category)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO categories (name, icon, color)
            VALUES (@name, @icon, @color)";

        cmd.Parameters.AddWithValue("@name", category.Name);
        cmd.Parameters.AddWithValue("@icon", category.Icon);
        cmd.Parameters.AddWithValue("@color", category.Color);

        cmd.ExecuteNonQuery();
    }

    public void Update(Category category)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE categories SET name = @name, icon = @icon, color = @color WHERE id = @id";

        cmd.Parameters.AddWithValue("@id", category.Id);
        cmd.Parameters.AddWithValue("@name", category.Name);
        cmd.Parameters.AddWithValue("@icon", category.Icon);
        cmd.Parameters.AddWithValue("@color", category.Color);

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

    public int GetProductCount(int categoryId)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM products WHERE category_id = @id";
        cmd.Parameters.AddWithValue("@id", categoryId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
