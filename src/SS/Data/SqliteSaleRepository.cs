using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SS.Models;

namespace SS.Data;

public class SqliteSaleRepository
{
    private readonly string _dbPath;

    public SqliteSaleRepository(string dbPath)
    {
        _dbPath = dbPath;
    }

    public int Add(Sale sale)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO sales (user_id, total, payment_method, created_at)
            VALUES (@user_id, @total, @payment_method, @created_at);
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("@user_id", sale.UserId == 0 ? (object)DBNull.Value : sale.UserId);
        cmd.Parameters.AddWithValue("@total", sale.Total);
        cmd.Parameters.AddWithValue("@payment_method", sale.PaymentMethod);
        cmd.Parameters.AddWithValue("@created_at", sale.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void AddItem(SaleItem item)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO sale_items (sale_id, product_id, quantity, price_sold)
            VALUES (@sale_id, @product_id, @quantity, @price_sold)";

        cmd.Parameters.AddWithValue("@sale_id", item.SaleId);
        cmd.Parameters.AddWithValue("@product_id", item.ProductId);
        cmd.Parameters.AddWithValue("@quantity", item.Quantity);
        cmd.Parameters.AddWithValue("@price_sold", item.PriceSold);

        cmd.ExecuteNonQuery();
    }

    public ObservableCollection<Sale> GetAll()
    {
        var result = new ObservableCollection<Sale>();
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, user_id, total, payment_method, created_at FROM sales ORDER BY created_at DESC";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Sale
            {
                Id = reader.GetInt32(0),
                UserId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                Total = reader.GetDecimal(2),
                PaymentMethod = reader.IsDBNull(3) ? "" : reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return result;
    }

    public ObservableCollection<Sale> GetByDateRange(DateTime from, DateTime to)
    {
        var result = new ObservableCollection<Sale>();
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, user_id, total, payment_method, created_at FROM sales WHERE created_at BETWEEN @from AND @to ORDER BY created_at DESC";
        cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd 00:00:00"));
        cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd 23:59:59"));

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Sale
            {
                Id = reader.GetInt32(0),
                UserId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                Total = reader.GetDecimal(2),
                PaymentMethod = reader.IsDBNull(3) ? "" : reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return result;
    }

    public decimal GetTotalByDateRange(DateTime from, DateTime to)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(total), 0) FROM sales WHERE created_at BETWEEN @from AND @to";
        cmd.Parameters.AddWithValue("@from", from.ToString("yyyy-MM-dd 00:00:00"));
        cmd.Parameters.AddWithValue("@to", to.ToString("yyyy-MM-dd 23:59:59"));

        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

    public decimal GetTodayTotal()
    {
        var today = DateTime.Today;
        return GetTotalByDateRange(today, today.AddDays(1).AddSeconds(-1));
    }

    public decimal GetMonthTotal()
    {
        var now = DateTime.Now;
        var firstDay = new DateTime(now.Year, now.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddSeconds(-1);
        return GetTotalByDateRange(firstDay, lastDay);
    }

    public int GetSaleCount()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sales";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
