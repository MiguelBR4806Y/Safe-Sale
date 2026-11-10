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
    public int RegisterSale(int? userId, int cashSessionId, string paymentMethod, decimal ivaRate,
        IReadOnlyCollection<SaleLine> items)
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
        cmd.Parameters.AddWithValue("@p_iva_rate", ivaRate);
        cmd.Parameters.Add(new MySqlParameter("@p_items", MySqlDbType.JSON) { Value = payload });

        var saleIdParam = new MySqlParameter("@p_sale_id", MySqlDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        cmd.Parameters.Add(saleIdParam);

        cmd.ExecuteNonQuery();

        return Convert.ToInt32(saleIdParam.Value);
    }

    public ObservableCollection<SaleItem> GetItems(int saleId)
    {
        var result = new ObservableCollection<SaleItem>();
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT si.id, si.sale_id, si.product_id, COALESCE(p.name, 'Producto eliminado'),
                   si.quantity, si.price_sold
            FROM sale_items si
            LEFT JOIN products p ON p.id = si.product_id
            WHERE si.sale_id = @sale_id
            ORDER BY si.id";
        cmd.Parameters.AddWithValue("@sale_id", saleId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new SaleItem
            {
                Id = reader.GetInt32(0),
                SaleId = reader.GetInt32(1),
                ProductId = reader.GetInt32(2),
                ProductName = reader.GetString(3),
                Quantity = reader.GetInt32(4),
                PriceSold = reader.GetDecimal(5)
            });
        }
        return result;
    }

    public ObservableCollection<Sale> GetAll()
    {
        var result = new ObservableCollection<Sale>();
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT s.id, s.user_id, COALESCE(u.username, 'Sistema') AS cashier,
                   s.subtotal, s.iva_rate, s.iva_amount, s.total,
                   s.payment_method, s.created_at,
                   COUNT(si.id) AS line_count,
                   COALESCE(SUM(si.quantity), 0) AS units_count
            FROM sales s
            LEFT JOIN users u ON u.id = s.user_id
            LEFT JOIN sale_items si ON si.sale_id = s.id
            GROUP BY s.id
            ORDER BY s.created_at DESC";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadSale(reader));
        return result;
    }

    public ObservableCollection<Sale> GetByDateRange(DateTime from, DateTime to)
    {
        var result = new ObservableCollection<Sale>();
        using var conn = new MySqlConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT s.id, s.user_id, COALESCE(u.username, 'Sistema') AS cashier,
                   s.subtotal, s.iva_rate, s.iva_amount, s.total,
                   s.payment_method, s.created_at,
                   COUNT(si.id) AS line_count,
                   COALESCE(SUM(si.quantity), 0) AS units_count
            FROM sales s
            LEFT JOIN users u ON u.id = s.user_id
            LEFT JOIN sale_items si ON si.sale_id = s.id
            WHERE s.created_at BETWEEN @from AND @to
            GROUP BY s.id
            ORDER BY s.created_at DESC";
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(ReadSale(reader));
        return result;
    }

    private static Sale ReadSale(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        UserId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
        UserName = reader.IsDBNull(2) ? "Sistema" : reader.GetString(2),
        Subtotal = reader.GetDecimal(3),
        IvaRate = reader.GetDecimal(4),
        IvaAmount = reader.GetDecimal(5),
        Total = reader.GetDecimal(6),
        PaymentMethod = reader.IsDBNull(7) ? "" : reader.GetString(7),
        CreatedAt = reader.GetDateTime(8),
        LineCount = Convert.ToInt32(reader.GetInt64(9)),
        UnitsCount = Convert.ToInt32(reader.GetDecimal(10))
    };

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
