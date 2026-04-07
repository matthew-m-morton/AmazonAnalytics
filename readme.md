# Amazon Order History Analyzer

A C# console application that parses and analyzes Amazon order history CSV files.
View spending broken down by month, year, or seasonal trends — and filter by shipping address.

## Instructions for Build and Use

Steps to build and/or run the software:

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Clone or download this repository
3. Open a terminal in the project folder and run `dotnet build`

Instructions for using the software:

1. Run `dotnet run` from the project folder, or pass the CSV path directly: `dotnet run -- testData.csv`
2. When prompted, type the path to your Amazon order history CSV file (downloadable from Amazon's "Order History Reports" page) or try out the test data (testData.csv)
3. Use the numbered menu to navigate: Monthly Totals, Yearly Summary, Monthly Trends, or filter by Shipping Address

## Development Environment

To recreate the development environment, you need the following software and/or libraries with the specified versions:

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (v8.0.414 or later)
* C# 12
* Any text editor or IDE (Visual Studio, VS Code, Rider, etc.)

## Useful Websites to Learn More

I found these websites useful in developing this software:

* [Microsoft C# Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/)
* [LINQ Query Expressions](https://learn.microsoft.com/en-us/dotnet/csharp/linq/)
* [Amazon Order History Reports](https://www.amazon.com/hz/privacy-central/data-requests/preview.html)

## Future Work

The following items I plan to fix, improve, and/or add to this project in the future:

* [ ] Show totals based on shipping address
* [ ] removing extremes from the data set(less than $4 and $500+)
* [ ] comparing shipping address results side by side.