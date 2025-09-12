using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Common;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "6.csv");

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    Console.ReadKey();
                    return;
                }

                var records = LoadCsvFile(filePath);

                LogInvalidRows(records);

                var first100ValidRows = records.Where(row => row.All(c => !string.IsNullOrEmpty(c) && !string.IsNullOrWhiteSpace(c))).Take(100).ToList();

                Console.WriteLine("First 100 valid rows:");
                foreach (var row in first100ValidRows)
                {
                    Console.WriteLine(string.Join(", ", row));
                }

                LogExcessRows(records, first100ValidRows);

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey();
            }
        }

        static List<List<string>> LoadCsvFile(string filePath)
        {
            var rows = new List<List<string>>();
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var row = line.Split(',').Select(value => value.Trim()).ToList();
                        rows.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading CSV file: {ex.Message}");
            }
            return rows;
        }
        //Nevalidni
        static void LogInvalidRows(List<List<string>> records)
        {
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "log.txt");
            int expectedColumnCount = 21;  

            using (var writer = new StreamWriter(logFilePath, append: true))
            {
                foreach (var record in records)
                {
                    bool isValid = record.Count == expectedColumnCount &&
                                   record.All(c => !string.IsNullOrEmpty(c) && !string.IsNullOrWhiteSpace(c)); 

                    if (!isValid)
                    {
                        var validationFault = new ValidationFault($"Invalid row: {string.Join(", ", record)}");
                        writer.WriteLine($"{validationFault.Message}");
                    }
                }
            }
        }

        //Viska
        static void LogExcessRows(List<List<string>> allRows, List<List<string>> first100Rows)
        {
            var excessRows = allRows.Where(row => !first100Rows.Contains(row)).ToList();
            if (excessRows.Any())
            {
                string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "excess_log.txt");
                using (var writer = new StreamWriter(logFilePath, append: true))
                {
                    foreach (var row in excessRows)
                    {
                        writer.WriteLine($"Excess row: {string.Join(", ", row)}");
                    }
                }
            }
        }
    }
}
