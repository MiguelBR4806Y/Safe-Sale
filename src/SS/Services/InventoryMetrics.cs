using System.Collections.Generic;
using System.Linq;
using SS.Models;

namespace SS.Services;

public class InventoryMetrics
{
    public int ProductCount { get; set; }
    public int TotalUnits { get; set; }
    public int LowStockCount { get; set; }
    public decimal EstimatedValue { get; set; }

    public static InventoryMetrics Calculate(IEnumerable<Product> products, int? categoryId = null)
    {
        var filtered = categoryId.HasValue
            ? products.Where(p => p.CategoryId == categoryId.Value)
            : products;

        var list = filtered.ToList();

        return new InventoryMetrics
        {
            ProductCount = list.Count,
            TotalUnits = list.Sum(p => p.Stock),
            LowStockCount = list.Count(p => p.Stock <= p.MinStock),
            EstimatedValue = list.Sum(p => p.Price * p.Stock)
        };
    }
}
