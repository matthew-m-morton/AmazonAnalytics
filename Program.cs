using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AmazonOrderAnalyzer
{
    internal static class Program
    {
        // ── Colours ───────────────────────────────────────────────────────────
        static readonly ConsoleColor HeaderBg = ConsoleColor.DarkBlue;
        static readonly ConsoleColor HeaderFg = ConsoleColor.White;
        static readonly ConsoleColor AccentFg  = ConsoleColor.Cyan;
        static readonly ConsoleColor MutedFg   = ConsoleColor.DarkGray;
        static readonly ConsoleColor HighFg    = ConsoleColor.Yellow;
        static readonly ConsoleColor PromptFg  = ConsoleColor.Green;
        static readonly ConsoleColor WarnFg    = ConsoleColor.Magenta;

        // ── State ─────────────────────────────────────────────────────────────
        static List<OrderRecord> _orders = new();
        static string? _addressFilter = null;   // null = no filter (all addresses)

        // Active orders after applying the address filter
        static IEnumerable<OrderRecord> Filtered =>
            _addressFilter == null
                ? _orders
                : _orders.Where(o => o.ShippingAddress == _addressFilter);

        // ── Entry ─────────────────────────────────────────────────────────────
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Amazon Order Analyzer";

            PrintBanner();

            string filePath = args.Length > 0 ? args[0] : PromptForFile();
            LoadFile(filePath);
            RunMainMenu();
        }

        // ── File loading ──────────────────────────────────────────────────────
        static string PromptForFile()
        {
            while (true)
            {
                Write("\n  Enter path to your Amazon order CSV file\n  ", PromptFg);
                Write("> ", HighFg);
                string? input = Console.ReadLine()?.Trim().Trim('"');
                if (!string.IsNullOrEmpty(input) && File.Exists(input))
                    return input;
                Write("  File not found. Please try again.\n", ConsoleColor.Red);
            }
        }

        static void LoadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Write($"\n  Error: file not found: {filePath}\n", ConsoleColor.Red);
                filePath = PromptForFile();
            }

            Write($"\n  Loading {Path.GetFileName(filePath)} ...", MutedFg);
            _orders = CsvParser.Parse(filePath);
            _addressFilter = null;  // reset filter on new file
            Write($" {_orders.Count:N0} orders loaded.\n", HighFg);
        }

        // ── Main menu ─────────────────────────────────────────────────────────
        static void RunMainMenu()
        {
            while (true)
            {
                Console.WriteLine();
                PrintRule();
                PrintLine("  MAIN MENU", AccentFg);
                PrintRule();
                WriteOption("1", "Monthly Totals");
                WriteOption("2", "Yearly Summary");
                WriteOption("3", "Monthly Trends  (all years combined)");
                WriteOption("4", "Filter by Shipping Address");
                WriteOption("5", "Open different file");
                WriteOption("Q", "Quit");
                PrintRule();

                // Show active filter hint
                if (_addressFilter != null)
                {
                    Write("  Active filter: ", WarnFg);
                    Write(TruncateAddress(_addressFilter, 55) + "\n", ConsoleColor.White);
                    PrintRule();
                }

                Write("  Choice: ", PromptFg);

                switch (Console.ReadLine()?.Trim().ToUpper())
                {
                    case "1": ShowMonthlyMenu();     break;
                    case "2": ShowYearlySummary();   break;
                    case "3": ShowMonthTrends();     break;
                    case "4": ShowAddressFilter();   break;
                    case "5":
                        string path = PromptForFile();
                        LoadFile(path);
                        break;
                    case "Q": case "QUIT": case "EXIT":
                        Write("\n  Goodbye!\n\n", MutedFg);
                        return;
                    default:
                        Write("  Invalid choice.\n", ConsoleColor.Red);
                        break;
                }
            }
        }

        // ── Address filter ────────────────────────────────────────────────────
        static void ShowAddressFilter()
        {
            var addresses = OrderAggregator.GetShippingAddresses(_orders);
            if (addresses.Count == 0)
            {
                Write("  No shipping addresses found in data.\n", ConsoleColor.Red);
                return;
            }

            Console.WriteLine();
            PrintRule();
            PrintLine("  SHIPPING ADDRESSES", AccentFg);
            PrintRule();
            WriteOption("0", "Clear filter (show all addresses)");

            for (int i = 0; i < addresses.Count; i++)
            {
                int count = _orders.Count(o => o.ShippingAddress == addresses[i]);
                string label = $"{TruncateAddress(addresses[i], 52)}  ({count} orders)";
                WriteOption((i + 1).ToString(), label);
            }

            PrintRule();
            Write("  Choice: ", PromptFg);

            string? input = Console.ReadLine()?.Trim();
            if (!int.TryParse(input, out int choice))
            {
                Write("  Invalid choice.\n", ConsoleColor.Red);
                return;
            }

            if (choice == 0)
            {
                _addressFilter = null;
                Write("  Filter cleared — showing all addresses.\n", HighFg);
            }
            else if (choice >= 1 && choice <= addresses.Count)
            {
                _addressFilter = addresses[choice - 1];
                Write("  Filter set to: ", HighFg);
                Write(TruncateAddress(_addressFilter, 60) + "\n", ConsoleColor.White);
            }
            else
            {
                Write("  Number out of range.\n", ConsoleColor.Red);
            }
        }

        // ── Monthly totals ────────────────────────────────────────────────────
        static void ShowMonthlyMenu()
        {
            var src   = Filtered.ToList();
            var years = OrderAggregator.GetAvailableYears(src);

            Console.WriteLine();
            PrintLine("  Filter by year (or press Enter for all years):", AccentFg);
            Write("  Available: ", MutedFg);
            Write(string.Join("  ", years) + "\n", HighFg);
            Write("  Year: ", PromptFg);

            string? input = Console.ReadLine()?.Trim();
            int? year = int.TryParse(input, out int y) ? y : null;

            var monthly = OrderAggregator.GetMonthlyTotals(src, year);
            if (monthly.Count == 0) { Write("  No data found.\n", ConsoleColor.Red); return; }

            Console.WriteLine();
            string scope = year.HasValue ? year.Value.ToString() : "ALL YEARS";
            string title = $"MONTHLY TOTALS — {scope}{AddressTag()}";

            PrintTableHeader(title,
                ("Month",      -22),
                ("Orders",       7),
                ("Total Spent", 14),
                ("Avg / Order", 13));

            decimal topTotal    = monthly.Max(m => m.Total);
            decimal grandTotal  = 0;
            int     grandOrders = 0;

            foreach (var m in monthly)
            {
                grandTotal  += m.Total;
                grandOrders += m.OrderCount;
                PrintTableRow(m.Total == topTotal ? HighFg : ConsoleColor.White,
                    ($"{m.MonthName} {m.Year}", -22),
                    ($"{m.OrderCount}",            7),
                    ($"{m.Total:C2}",             14),
                    ($"{m.Average:C2}",           13));
            }

            PrintTableFooter(
                ("TOTAL",           -22),
                ($"{grandOrders}",    7),
                ($"{grandTotal:C2}", 14),
                ("",                 13));
        }

        // ── Yearly summary ────────────────────────────────────────────────────
        static void ShowYearlySummary()
        {
            var yearly = OrderAggregator.GetYearlyTotals(Filtered);
            if (yearly.Count == 0) { Write("  No data found.\n", ConsoleColor.Red); return; }

            Console.WriteLine();
            PrintTableHeader($"YEARLY SUMMARY{AddressTag()}",
                ("Year",        6),
                ("Orders",      7),
                ("Total Spent", 14),
                ("Avg / Order", 13),
                ("Best Month",  12),
                ("Best Mo.$",   12));

            decimal grandTotal  = 0;
            int     grandOrders = 0;
            foreach (var yr in yearly)
            {
                grandTotal  += yr.Total;
                grandOrders += yr.OrderCount;
                PrintTableRow(ConsoleColor.White,
                    ($"{yr.Year}",              6),
                    ($"{yr.OrderCount}",        7),
                    ($"{yr.Total:C2}",         14),
                    ($"{yr.Average:C2}",       13),
                    ($"{yr.HighestMonthName}", 12),
                    ($"{yr.HighestMonth:C2}",  12));
            }

            PrintTableFooter(
                ("TOTAL",             6),
                ($"{grandOrders}",    7),
                ($"{grandTotal:C2}", 14),
                ("", 13), ("", 12), ("", 12));
        }

        // ── Monthly trends (all years combined) ───────────────────────────────
        static void ShowMonthTrends()
        {
            var src    = Filtered.ToList();
            var trends = OrderAggregator.GetMonthTrends(src);
            if (trends.Count == 0) { Write("  No data found.\n", ConsoleColor.Red); return; }

            int totalYears = OrderAggregator.GetAvailableYears(src).Length;

            Console.WriteLine();
            PrintTableHeader($"MONTHLY TRENDS — ALL YEARS COMBINED{AddressTag()}",
                ("Month",       -13),
                ("Orders",        7),
                ("Total (All)",  14),
                ("Avg / Year",   13),
                ("Avg / Order",  13));

            decimal topTotal = trends.Max(t => t.Total);

            foreach (var t in trends)
            {
                PrintTableRow(t.Total == topTotal ? HighFg : ConsoleColor.White,
                    ($"{t.MonthName}",    -13),
                    ($"{t.OrderCount}",     7),
                    ($"{t.Total:C2}",      14),
                    ($"{t.AvgPerYear:C2}", 13),
                    ($"{t.AvgPerOrder:C2}",13));
            }

            PrintTableFooter(
                ($"{totalYears} yrs",       -13),
                ($"{trends.Sum(t => t.OrderCount)}", 7),
                ($"{trends.Sum(t => t.Total):C2}", 14),
                ("", 13), ("", 13));

            Write($"  Tip: 'Avg / Year' shows typical spend for that month averaged across {totalYears} year(s).\n\n", MutedFg);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        // Short tag appended to table titles when a filter is active
        static string AddressTag() =>
            _addressFilter == null ? "" : $"  [{TruncateAddress(_addressFilter, 30)}]";

        static string TruncateAddress(string addr, int max) =>
            addr.Length <= max ? addr : addr[..(max - 1)] + "…";

        // ── Table helpers ─────────────────────────────────────────────────────
        static void PrintTableHeader(string title, params (string text, int width)[] cols)
        {
            int totalWidth = cols.Sum(c => Math.Abs(c.width)) + cols.Length + 1;
            string bar = new string('─', totalWidth);

            Console.BackgroundColor = HeaderBg;
            Console.ForegroundColor = HeaderFg;
            Console.Write("  " + title.PadRight(totalWidth) + "  ");
            Console.ResetColor();
            Console.WriteLine();

            Write("  " + bar + "\n", MutedFg);

            Console.ForegroundColor = AccentFg;
            Console.Write("  ");
            foreach (var (text, width) in cols)
            {
                int w = Math.Abs(width);
                string cell = width < 0 ? text.PadRight(w) : text.PadLeft(w);
                Console.Write(cell + " ");
            }
            Console.ResetColor();
            Console.WriteLine();
            Write("  " + bar + "\n", MutedFg);
        }

        static void PrintTableRow(ConsoleColor fg, params (string text, int width)[] cols)
        {
            Console.ForegroundColor = fg;
            Console.Write("  ");
            foreach (var (text, width) in cols)
            {
                int w = Math.Abs(width);
                string cell = width < 0 ? text.PadRight(w) : text.PadLeft(w);
                if (cell.Length > w + 1) cell = cell[..(w - 1)] + "…";
                Console.Write(cell + " ");
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        static void PrintTableFooter(params (string text, int width)[] cols)
        {
            int totalWidth = cols.Sum(c => Math.Abs(c.width)) + cols.Length + 1;
            Write("  " + new string('─', totalWidth) + "\n", MutedFg);
            PrintTableRow(HighFg, cols);
            Write("  " + new string('─', totalWidth) + "\n\n", MutedFg);
        }

        // ── Low-level print helpers ───────────────────────────────────────────
        static void PrintBanner()
        {
            Console.Clear();
            Console.ForegroundColor = AccentFg;
            Console.WriteLine(@"
  ╔═══════════════════════════════════════════╗
  ║       AMAZON ORDER HISTORY ANALYZER       ║
  ╚═══════════════════════════════════════════╝");
            Console.ResetColor();
        }

        static void PrintRule() =>
            Write("  " + new string('─', 60) + "\n", MutedFg);

        static void PrintLine(string text, ConsoleColor fg)
        {
            Console.ForegroundColor = fg;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        static void WriteOption(string key, string label)
        {
            Write($"    [{key}]", HighFg);
            Write($"  {label}\n", ConsoleColor.White);
        }

        static void Write(string text, ConsoleColor fg)
        {
            Console.ForegroundColor = fg;
            Console.Write(text);
            Console.ResetColor();
        }
    }
}