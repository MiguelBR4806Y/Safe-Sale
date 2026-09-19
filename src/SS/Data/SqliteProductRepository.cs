using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using SS.Models;

namespace SS.Data;

public class SqliteProductRepository
{
    private readonly string _dbPath;

    public SqliteProductRepository(string dbPath)
    {
        _dbPath = dbPath;
        Initialize();
    }

    private void Initialize()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                barcode TEXT UNIQUE,
                price DECIMAL(10,2) NOT NULL,
                stock INTEGER DEFAULT 0,
                category_id INTEGER,
                min_stock INTEGER DEFAULT 5,
                FOREIGN KEY (category_id) REFERENCES categories(id)
            )";
        cmd.ExecuteNonQuery();
    }

    public ObservableCollection<Product> GetAll()
    {
        var result = new ObservableCollection<Product>();
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT p.id, p.name, p.barcode, p.price, p.stock, p.category_id, c.name AS category_name FROM products p LEFT JOIN categories c ON p.category_id = c.id";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var product = new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Barcode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4),
                CategoryId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                // Note: we're not mapping category name to the product model for simplicity
            };
            result.Add(product);
        }
        return result;
    }

    public void Add(Product product)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO products (name, barcode, price, stock, category_id, min_stock)
            VALUES (@name, @barcode, @price, @stock, @category_id, @min_stock)";

        cmd.Parameters.AddWithValue("@name", product.Name);
        cmd.Parameters.AddWithValue("@barcode", product.Barcode);
        cmd.Parameters.AddWithValue("@price", product.Price);
        cmd.Parameters.AddWithValue("@stock", product.Stock);
        cmd.Parameters.AddWithValue("@category_id", product.CategoryId == 0 ? (object)DBNull.Value : product.CategoryId);
        cmd.Parameters.AddWithValue("@min_stock", product.MinStock);

        cmd.ExecuteNonQuery();
    }

    public void Update(Product product)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE products SET name = @name, barcode = @barcode, price = @price, stock = @stock, category_id = @category_id, min_stock = @min_stock WHERE id = @id";

        cmd.Parameters.AddWithValue("@id", product.Id);
        cmd.Parameters.AddWithValue("@name", product.Name);
        cmd.Parameters.AddWithValue("@barcode", product.Barcode);
        cmd.Parameters.AddWithValue("@price", product.Price);
        cmd.Parameters.AddWithValue("@stock", product.Stock);
        cmd.Parameters.AddWithValue("@category_id", product.CategoryId == 0 ? (object)DBNull.Value : product.CategoryId);
        cmd.Parameters.AddWithValue("@min_stock", product.MinStock);

        cmd.ExecuteNonQuery();
    }

    public bool Delete(int id)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM sale_items WHERE product_id = @id; DELETE FROM products WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        var rows = cmd.ExecuteNonQuery();
        return rows > 0;
    }

    public void DeleteAll()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM sale_items WHERE product_id IN (SELECT id FROM products); DELETE FROM products";
        cmd.ExecuteNonQuery();
    }

    public Product? GetById(int id)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, barcode, price, stock, category_id, min_stock FROM products WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Barcode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4),
                CategoryId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                MinStock = reader.GetInt32(6),
            };
        }
        return null;
    }

    public Product? GetByBarcode(string barcode)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, barcode, price, stock, category_id, min_stock FROM products WHERE barcode = @barcode";
        cmd.Parameters.AddWithValue("@barcode", barcode);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Barcode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4),
                CategoryId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                MinStock = reader.GetInt32(6),
            };
        }
        return null;
    }

    public ObservableCollection<Product> GetLowStock()
    {
        var result = new ObservableCollection<Product>();
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT p.id, p.name, p.barcode, p.price, p.stock, p.min_stock FROM products p WHERE p.stock <= p.min_stock";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var product = new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Barcode = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Price = reader.GetDecimal(3),
                Stock = reader.GetInt32(4),
                MinStock = reader.GetInt32(5),
            };
            result.Add(product);
        }
        return result;
    }
}