using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AmazonOrderAnalyzer
{
    public class OrderRecord
    {
        public DateTime OrderDate       { get; set; }
        public string OrderId           { get; set; } = "";
        public string ProductName       { get; set; } = "";
        public string OrderStatus       { get; set; } = "";
        public decimal TotalOwed        { get; set; }
        public decimal UnitPrice        { get; set; }
        public int Quantity             { get; set; }
        public string ShippingAddress   { get; set; } = "";
    }

    public static class CsvParser
    {
        public static List<OrderRecord> Parse(string filePath)
        {
            var records = new List<OrderRecord>();
            var lines = File.ReadAllLines(filePath);

            if (lines.Length < 2)
                return records;

            // Parse header to find column indices dynamically
            var headers = SplitCsvLine(lines[0]);
            int idxDate       = IndexOf(headers, "Order Date");
            int idxOrderId    = IndexOf(headers, "Order ID");
            int idxProduct    = IndexOf(headers, "Product Name");
            int idxStatus     = IndexOf(headers, "Order Status");
            int idxTotalOwed  = IndexOf(headers, "Total Owed");
            int idxUnitPrice  = IndexOf(headers, "Unit Price");
            int idxQuantity   = IndexOf(headers, "Quantity");
            int idxShipAddr   = IndexOf(headers, "Shipping Address");

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var cols = SplitCsvLine(lines[i]);
                if (cols.Count <= idxDate) continue;

                if (!DateTime.TryParse(
                        SafeGet(cols, idxDate),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal,
                        out var date))
                    continue;

                decimal.TryParse(SafeGet(cols, idxTotalOwed),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out var totalOwed);
                decimal.TryParse(SafeGet(cols, idxUnitPrice),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice);
                int.TryParse(SafeGet(cols, idxQuantity), out var quantity);

                records.Add(new OrderRecord
                {
                    OrderDate       = date,
                    OrderId         = SafeGet(cols, idxOrderId),
                    ProductName     = SafeGet(cols, idxProduct),
                    OrderStatus     = SafeGet(cols, idxStatus),
                    TotalOwed       = totalOwed,
                    UnitPrice       = unitPrice,
                    Quantity        = quantity,
                    ShippingAddress = SafeGet(cols, idxShipAddr)
                });
            }

            return records;
        }

        private static int IndexOf(List<string> headers, string name)
        {
            for (int i = 0; i < headers.Count; i++)
                if (string.Equals(headers[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private static string SafeGet(List<string> cols, int idx) =>
            idx >= 0 && idx < cols.Count ? cols[idx].Trim() : "";

        // RFC-4180 compliant CSV line splitter
        private static List<string> SplitCsvLine(string line)
        {
            var fields = new List<string>();
            int i = 0;
            while (i <= line.Length)
            {
                if (i == line.Length) { fields.Add(""); break; }

                if (line[i] == '"')
                {
                    // Quoted field
                    var sb = new System.Text.StringBuilder();
                    i++; // skip opening quote
                    while (i < line.Length)
                    {
                        if (line[i] == '"' && i + 1 < line.Length && line[i + 1] == '"')
                        { sb.Append('"'); i += 2; }
                        else if (line[i] == '"')
                        { i++; break; }
                        else
                        { sb.Append(line[i++]); }
                    }
                    fields.Add(sb.ToString());
                    if (i < line.Length && line[i] == ',') i++;
                }
                else
                {
                    int start = i;
                    while (i < line.Length && line[i] != ',') i++;
                    fields.Add(line.Substring(start, i - start));
                    if (i < line.Length) i++; // skip comma
                }
            }
            return fields;
        }
    }
}