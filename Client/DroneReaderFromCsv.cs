using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Client
{
    public class DroneReaderFromCsv : IDisposable
    {
        private StreamReader _reader;
        private bool _disposed = false;

        public DroneReaderFromCsv(string csvFilePath)
        {
            _reader = new StreamReader(csvFilePath);
        }

        public List<DroneSample> ReadSamples(int maxRows)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DroneReaderFromCsv));

            var samples = new List<DroneSample>();
            var invalidRows = new List<string>();

            int rowCount = 0;
            string headerLine = _reader.ReadLine();
            if (headerLine == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("CSV fajl je prazan!");
                Console.ResetColor();
                return samples;
            }

            var headers = headerLine.Split(',');

            // Relevantne kolone za DroneSample (mora biti u istom redosledu kao u zaglavlju log fajla)
            var relevantHeaders = new[]
            {
        "time",
        "wind_speed",
        "wind_angle",
        "linear_acceleration_x",
        "linear_acceleration_y",
        "linear_acceleration_z"
    };

            // Mapa zaglavlja na indeks
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
                headerMap[headers[i].Trim().ToLower()] = i;

            // Dodaj zaglavlje u invalidRows (da log fajl ima nazive kolona)
            invalidRows.Add(string.Join(",", relevantHeaders));

            while (!_reader.EndOfStream)
            {
                string line = _reader.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (rowCount >= maxRows)
                {
                    // Ovde možeš zapisati kompletan red ili samo relevantne kolone
                    invalidRows.Add(FilterRelevantColumns(line, headerMap, relevantHeaders));
                    continue;
                }

                var values = line.Split(',');

                try
                {
                    var sample = new DroneSample();

                    for (int i = 0; i < headers.Length && i < values.Length; i++)
                    {
                        string header = headers[i].Trim().ToLower();
                        string value = values[i].Trim();

                        switch (header)
                        {
                            case "time":
                                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedTime))
                                {
                                    sample.Time = parsedTime;  // Spasi sekunde kao double
                                }
                                else
                                {
                                    Console.WriteLine($"[WARNING] Invalid time value: {value}");
                                    continue; // Skip invalid entry
                                }
                                break;


                            case "wind_speed":
                                if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double windSpeed))
                                {
                                    Console.WriteLine($"[WARNING] Invalid wind_speed value: {value}");
                                    continue; // Skip invalid entry
                                }
                                sample.WindSpeed = windSpeed;
                                break;
                            case "wind_angle":
                                if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double windAngle))
                                {
                                    Console.WriteLine($"[WARNING] Invalid wind_angle value: {value}");
                                    continue; // Skip invalid entry
                                }
                                sample.WindAngle = windAngle;
                                break;
                            case "linear_acceleration_x":
                                if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double accX))
                                {
                                    Console.WriteLine($"[WARNING] Invalid linear_acceleration_x value: {value}");
                                    continue; // Skip invalid entry
                                }
                                sample.LinearAccelerationX = accX;
                                break;
                            case "linear_acceleration_y":
                                if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double accY))
                                {
                                    Console.WriteLine($"[WARNING] Invalid linear_acceleration_y value: {value}");
                                    continue; // Skip invalid entry
                                }
                                sample.LinearAccelerationY = accY;
                                break;
                            case "linear_acceleration_z":
                                if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double accZ))
                                {
                                    Console.WriteLine($"[WARNING] Invalid linear_acceleration_z value: {value}");
                                    continue; // Skip invalid entry
                                }
                                sample.LinearAccelerationZ = accZ;
                                break;
                        }
                    }

                    samples.Add(sample);
                    rowCount++;
                }
                catch (Exception ex)
                {
                    // U slučaju greške, upiši samo relevantne kolone u log
                    invalidRows.Add(FilterRelevantColumns(line, headerMap, relevantHeaders));
                    Console.WriteLine($"[ERROR] Failed to parse row: {ex.Message}");
                }
            }

            if (invalidRows.Count > 1) // jer je prvi red zaglavlje
            {
                File.WriteAllLines("drone_log.csv", invalidRows);
                Console.WriteLine($"Nevalidni redovi: {invalidRows.Count - 1} (zapisani u drone_log.csv)");
            }

            return samples;
        }


        // Metoda za izdvajanje samo relevantnih kolona iz reda CSV fajla
        private string FilterRelevantColumns(string line, Dictionary<string, int> headerMap, string[] relevantHeaders)
        {
            var values = line.Split(',');

            var filteredValues = new List<string>();

            foreach (var col in relevantHeaders)
            {
                if (headerMap.TryGetValue(col.ToLower(), out int idx) && idx < values.Length)
                    filteredValues.Add(values[idx].Trim());
                else
                    filteredValues.Add(""); // prazno ako kolona ne postoji
            }

            return string.Join(",", filteredValues);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _reader?.Close();
                    _reader?.Dispose();
                }

                _disposed = true;
            }
        }

        ~DroneReaderFromCsv()
        {
            Dispose(false);
        }
    }
}
