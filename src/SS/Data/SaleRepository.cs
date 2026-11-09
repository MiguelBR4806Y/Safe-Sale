using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text.Json;
using MySqlConnector;
using SS.Models;

namespace SS.Data;

public class SaleRepository
{
    private readonly string _connectionString;

    public SaleRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public sealed record SaleLine(int ProductId, int Quantity, decimal Price);

    /// <summary>
    /// Registra la venta completa en una sola operación atómica en el motor:
    /// inserta venta e ítems y descuenta stock. Si algo falla (por ejemplo stock
    /// insuficiente), el procedimiento hace ROLLBACK y no queda nada a medias.
    /// </summary>
    public int RegisterSale(int? userId, int cashSessionId, string paymentMethod, IReadOnlyCollection<SaleLine> items)
    {
        var payload = JsonSerializer.Serialize(items.Select(i => new
        {
            product_id = i.ProductId,
            quantity = i.Quantity,
            price = i.Price
        }));

        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_registrar_venta";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@p_user_id", userId.HasValue ? (object)userId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@p_cash_session_id", cashSessionId);
        cmd.Parameters.AddWithValue("@p_payment_method", paymentMethod);
        cmd.Parameters.Add(new MySqlParameter("@p_items", MySqlDbType.JSON) { Value = payload });

        var saleIdParam = new MySqlParameter("@p_sale_id", MySqlDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(saleIdParam);

        cmd.ExecuteNonQuery();

        return Convert.ToInt32(saleIdParam.Value);
    }

    public ObservableCollection<Sale> GetAll()
    {
        var result = new ObservableCollection<Sale>();
        using var conn = new MySqlConnection(_connectionString);
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
                CreatedAt = reader.GetDateTime(4)
            });
        }
        return result;
    }

    public ObservableCollection<Sale> GetByDateRange(DateTime from, DateTime to)
    {
        var result = new ObservableCollection<Sale>();
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, user_id, total, payment_method, created_at FROM sales WHERE created_at BETWEEN @from AND @to ORDER BY created_at DESC";
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Sale
            {
                Id = reader.GetInt32(0),
                UserId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                Total = reader.GetDecimal(2),
                PaymentMethod = reader.IsDBNull(3) ? "" : reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            });
        }
        return result;
    }

    public decimal GetTotalByDateRange(DateTime from, DateTime to)
    {
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(total), 0) FROM sales WHERE created_at BETWEEN @from AND @to";
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);

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
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sales";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
