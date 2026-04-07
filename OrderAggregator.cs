using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AmazonOrderAnalyzer
{
    public class MonthlyTotal
    {
        public int Year        { get; set; }
        public int Month       { get; set; }
        public string MonthName => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month);
        public decimal Total   { get; set; }
        public int OrderCount  { get; set; }
        public decimal Average => OrderCount > 0 ? Math.Round(Total / OrderCount, 2) : 0;
    }

    public class YearlyTotal
    {
        public int Year       { get; set; }
        public decimal Total  { get; set; }
        public int OrderCount { get; set; }
        public decimal Average => OrderCount > 0 ? Math.Round(Total / OrderCount, 2) : 0;
        public decimal HighestMonth { get; set; }
        public string HighestMonthName { get; set; } = "";
    }

    public class MonthTrend
    {
        public int Month           { get; set; }
        public string MonthName    => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month);
        public decimal Total       { get; set; }
        public int OrderCount      { get; set; }
        public int YearCount       { get; set; }
        public decimal AvgPerYear  => YearCount > 0  ? Math.Round(Total / YearCount,  2) : 0;
        public decimal AvgPerOrder => OrderCount > 0 ? Math.Round(Total / OrderCount, 2) : 0;
    }

    public static class OrderAggregator
    {
        public static List<MonthlyTotal> GetMonthlyTotals(
            IEnumerable<OrderRecord> records, int? filterYear = null)
        {
            var query = records.AsEnumerable();
            if (filterYear.HasValue)
                query = query.Where(r => r.OrderDate.Year == filterYear.Value);

            return query
                .GroupBy(r => new { r.OrderDate.Year, r.OrderDate.Month })
                .Select(g => new MonthlyTotal
                {
                    Year       = g.Key.Year,
                    Month      = g.Key.Month,
                    Total      = g.Sum(r => r.TotalOwed),
                    OrderCount = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();
        }

        public static List<YearlyTotal> GetYearlyTotals(IEnumerable<OrderRecord> records)
        {
            return records
                .GroupBy(r => r.OrderDate.Year)
                .Select(g =>
                {
                    var monthlyGroups = g
                        .GroupBy(r => r.OrderDate.Month)
                        .Select(mg => new
                        {
                            Month = mg.Key,
                            Total = mg.Sum(r => r.TotalOwed)
                        })
                        .OrderByDescending(mg => mg.Total)
                        .FirstOrDefault();

                    return new YearlyTotal
                    {
                        Year             = g.Key,
                        Total            = g.Sum(r => r.TotalOwed),
                        OrderCount       = g.Count(),
                        HighestMonth     = monthlyGroups?.Total ?? 0,
                        HighestMonthName = monthlyGroups != null
                            ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthlyGroups.Month)
                            : "N/A"
                    };
                })
                .OrderBy(y => y.Year)
                .ToList();
        }

        // Groups by month number only — collapses all years to show seasonal patterns
        public static List<MonthTrend> GetMonthTrends(IEnumerable<OrderRecord> records)
        {
            return records
                .GroupBy(r => r.OrderDate.Month)
                .Select(g => new MonthTrend
                {
                    Month      = g.Key,
                    Total      = g.Sum(r => r.TotalOwed),
                    OrderCount = g.Count(),
                    YearCount  = g.Select(r => r.OrderDate.Year).Distinct().Count()
                })
                .OrderBy(m => m.Month)
                .ToList();
        }

        public static int[] GetAvailableYears(IEnumerable<OrderRecord> records) =>
            records.Select(r => r.OrderDate.Year).Distinct().OrderBy(y => y).ToArray();

        // Returns distinct addresses sorted by order frequency (most-used first)
        public static List<string> GetShippingAddresses(IEnumerable<OrderRecord> records) =>
            records
                .Where(r => !string.IsNullOrWhiteSpace(r.ShippingAddress))
                .GroupBy(r => r.ShippingAddress)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();
    }
}