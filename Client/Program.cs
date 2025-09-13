using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string csvFilePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "6.csv");

                if (!System.IO.File.Exists(csvFilePath))
                {
                    Console.WriteLine($"File not found: {csvFilePath}");
                    Console.ReadKey();
                    return;
                }

                List<List<string>> records;
                using (var fileHandler = new FileHandler(csvFilePath))
                {
                    string fileContent = fileHandler.ReadFromFile();
                    records = ParseCsvContent(fileContent);
                }

                var invalidRows = new List<string>();
                var validSessionMeta = new List<SessionMeta>();

                foreach (var row in records)
                {
                    var meta = MapToSessionMeta(row);
                    if (meta == null || meta.Time.TotalSeconds <= 0)
                    {
                        invalidRows.Add(string.Join(",", row));
                    }
                    else
                    {
                        validSessionMeta.Add(meta);
                    }
                }

                if (validSessionMeta.Count == 0)
                {
                    Console.WriteLine("No valid session metadata found in the CSV file.");
                    Console.ReadKey();
                    return;
                }

                var rowsToProcess = validSessionMeta.Take(100).ToList();
                var extraRows = validSessionMeta.Skip(100)
                                .Select(m => $"{m.Time.TotalSeconds},{m.WindSpeed},{m.WindAngle},{m.LinearAccelerationX},{m.LinearAccelerationY},{m.LinearAccelerationZ}")
                                .ToList();

                LogInvalidAndExtraRows(invalidRows, extraRows);

                var initialMeta = rowsToProcess.First();

                ChannelFactory<IDroneService> factory = new ChannelFactory<IDroneService>("DroneServiceEndpoint");
                IDroneService proxy = factory.CreateChannel();

                var sessionResponse = proxy.StartSession(initialMeta);

                if (!sessionResponse.IsAck)
                {
                    Console.WriteLine($"Failed to start session. Status: {sessionResponse.Status}");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("Session started successfully!");

                foreach (var sessionMeta in rowsToProcess)
                {
                    Console.WriteLine($"{sessionMeta.Time.TotalSeconds},{sessionMeta.WindSpeed},{sessionMeta.WindAngle},{sessionMeta.LinearAccelerationX},{sessionMeta.LinearAccelerationY},{sessionMeta.LinearAccelerationZ}");
                }

                foreach (var sessionMeta in rowsToProcess)
                {
                    var sample = MapToDroneSample(sessionMeta);
                    if (sample != null)
                    {
                        proxy.PushSample(sample);
                    }
                }

                var endSessionResponse = proxy.EndSession();

                ((IClientChannel)proxy).Close();

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey();
            }
        }

        private static void LogInvalidAndExtraRows(List<string> invalidRows, List<string> extraRows)
        {
            string logFilePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "invalid_rows.log");

            using (var logHandler = new FileHandler(logFilePath))
            {
                logHandler.DeleteAllContent();

                logHandler.WriteToFile("=== Nevalidni redovi ===");
                foreach (var line in invalidRows)
                    logHandler.WriteToFile(line);

                logHandler.WriteToFile(string.Empty);

                logHandler.WriteToFile("=== Višak redovi preko 100 ===");
                foreach (var line in extraRows)
                    logHandler.WriteToFile(line);
            }
        }
        static List<List<string>> ParseCsvContent(string content)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(content)) return rows;

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (lines.Count <= 1) return rows;

            var dataLines = lines.Skip(1);

            foreach (var line in dataLines)
            {
                var row = line.Split(',').Select(v => v.Trim()).ToList();
                rows.Add(row);
            }

            return rows;
        }

        static SessionMeta MapToSessionMeta(List<string> row)
        {
            if (row.Count >= 20)
            {
                try
                {
                    if (!TryParseDouble(row[0], out double timeInSeconds) || timeInSeconds <= 0)
                        return null;

                    if (!TryParseDouble(row[1], out double windSpeed) ||
                        !TryParseDouble(row[2], out double windAngle) ||
                        !TryParseDouble(row[17], out double accX) ||
                        !TryParseDouble(row[18], out double accY) ||
                        !TryParseDouble(row[19], out double accZ))
                        return null;

                    return new SessionMeta
                    {
                        Time = TimeSpan.FromSeconds(timeInSeconds),
                        WindSpeed = windSpeed,
                        WindAngle = windAngle,
                        LinearAccelerationX = accX,
                        LinearAccelerationY = accY,
                        LinearAccelerationZ = accZ
                    };
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        static DroneSample MapToDroneSample(SessionMeta sessionMeta)
        {
            return new DroneSample
            {
                LinearAccelerationX = sessionMeta.LinearAccelerationX,
                LinearAccelerationY = sessionMeta.LinearAccelerationY,
                LinearAccelerationZ = sessionMeta.LinearAccelerationZ,
                WindSpeed = sessionMeta.WindSpeed,
                WindAngle = sessionMeta.WindAngle,
                Time = sessionMeta.Time
            };
        }

        static bool TryParseDouble(string s, out double result)
        {
            if (double.TryParse(s, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return true;
            if (double.TryParse(s, System.Globalization.NumberStyles.Any, new CultureInfo("fr-FR"), out result))
                return true;

            result = 0;
            return false;
        }
    }
}
