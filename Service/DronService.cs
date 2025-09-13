using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using Common;

namespace Service
{
    public class DroneService : IDroneService
    {
        private static List<DroneSample> _currentSessionSamples = new List<DroneSample>();
        private static SessionMeta _currentMeta;
        private static bool _sessionActive = false;

        private readonly double W_THRESHOLD = double.Parse(ConfigurationManager.AppSettings["W_threshold"], CultureInfo.InvariantCulture);
        private readonly double A_THRESHOLD = double.Parse(ConfigurationManager.AppSettings["A_threshold"], CultureInfo.InvariantCulture);

        public ServiceResponse StartSession(SessionMeta meta)
        {
            if (meta == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault("Session meta cannot be null"));

            if (!IsValidMeta(meta))
            {
                throw new FaultException<ValidationFault>(new ValidationFault("Session meta contains invalid or missing data"), new FaultReason("Invalid session meta data."));
            }

            if (_sessionActive)
            {
                return new ServiceResponse { IsAck = false, Status = SessionStatus.IN_PROGRESS };
            }

            _currentMeta = meta;
            _currentSessionSamples.Clear();
            _sessionActive = true;

            return new ServiceResponse { IsAck = true, Status = SessionStatus.IN_PROGRESS };
        }

        public ServiceResponse PushSample(DroneSample sample)
        {
            if (!_sessionActive)
            {
                return new ServiceResponse { IsAck = false, Status = SessionStatus.COMPLETED };
            }

            if (sample == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault("Sample cannot be null"));

            if (!IsValidSample(sample))
                throw new FaultException<ValidationFault>(new ValidationFault("Sample contains invalid or missing data"));

            double avgAx = 0, avgAy = 0, avgAz = 0, avgW = 0;
            int count = _currentSessionSamples.Count;

            if (count > 0)
            {
                foreach (var s in _currentSessionSamples)
                {
                    avgAx += s.LinearAccelerationX;
                    avgAy += s.LinearAccelerationY;
                    avgAz += s.LinearAccelerationZ;
                    avgW += s.WindSpeed;
                }

                avgAx /= count;
                avgAy /= count;
                avgAz /= count;
                avgW /= count;
            }
            else
            {
                avgAx = _currentMeta.LinearAccelerationX;
                avgAy = _currentMeta.LinearAccelerationY;
                avgAz = _currentMeta.LinearAccelerationZ;
                avgW = _currentMeta.WindSpeed;
            }

            bool windValid = IsWithinThreshold(sample.WindSpeed, avgW, W_THRESHOLD);
            bool accValid =
                IsWithinThreshold(sample.LinearAccelerationX, avgAx, A_THRESHOLD) &&
                IsWithinThreshold(sample.LinearAccelerationY, avgAy, A_THRESHOLD) &&
                IsWithinThreshold(sample.LinearAccelerationZ, avgAz, A_THRESHOLD);

            if (windValid && accValid)
            {
                _currentSessionSamples.Add(sample);

                return new ServiceResponse { IsAck = true, Status = SessionStatus.IN_PROGRESS };
            }
            else
            {
                return new ServiceResponse { IsAck = false, Status = SessionStatus.IN_PROGRESS };
            }
        }

        public ServiceResponse EndSession()
        {
            if (!_sessionActive)
            {
                return new ServiceResponse { IsAck = false, Status = SessionStatus.COMPLETED };
            }

            try
            {
                SaveSessionToDisk();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving session: " + ex.Message);
            }

            _sessionActive = false;
            _currentSessionSamples.Clear();
            _currentMeta = null;

            return new ServiceResponse { IsAck = true, Status = SessionStatus.COMPLETED };
        }

        private bool IsValidMeta(SessionMeta meta)
        {
            if (meta.WindSpeed <= 0)
            {
                Console.WriteLine("[DEBUG] InvalidMeta: WindSpeed <= 0");
                return false;
            }

            if (meta.WindAngle < 0 || meta.WindAngle > 360)
            {
                Console.WriteLine("[DEBUG] InvalidMeta: WindAngle out of range");
                return false;
            }

            if (!IsValidAcceleration(meta.LinearAccelerationX))
            {
                Console.WriteLine($"[DEBUG] InvalidMeta: AccX out of range: {meta.LinearAccelerationX}");
                return false;
            }
            if (!IsValidAcceleration(meta.LinearAccelerationY))
            {
                Console.WriteLine($"[DEBUG] InvalidMeta: AccY out of range: {meta.LinearAccelerationY}");
                return false;
            }
            if (!IsValidAcceleration(meta.LinearAccelerationZ))
            {
                Console.WriteLine($"[DEBUG] InvalidMeta: AccZ out of range: {meta.LinearAccelerationZ}");
                return false;
            }

            return true;
        }

        private bool IsValidSample(DroneSample sample)
        {
            if (sample.WindSpeed <= 0)
            {
                Console.WriteLine("[DEBUG] InvalidSample: WindSpeed <= 0");
                return false;
            }

            if (sample.WindAngle < 0 || sample.WindAngle > 360)
            {
                Console.WriteLine("[DEBUG] InvalidSample: WindAngle out of range");
                return false;
            }

            if (!IsValidAcceleration(sample.LinearAccelerationX))
            {
                Console.WriteLine($"[DEBUG] InvalidSample: AccX out of range: {sample.LinearAccelerationX}");
                return false;
            }
            if (!IsValidAcceleration(sample.LinearAccelerationY))
            {
                Console.WriteLine($"[DEBUG] InvalidSample: AccY out of range: {sample.LinearAccelerationY}");
                return false;
            }
            if (!IsValidAcceleration(sample.LinearAccelerationZ))
            {
                Console.WriteLine($"[DEBUG] InvalidSample: AccZ out of range: {sample.LinearAccelerationZ}");
                return false;
            }

            return true;
        }

        private bool IsValidAcceleration(double value)
        {
            return value >= -10 && value <= 10;
        }

        private bool IsWithinThreshold(double value, double average, double threshold)
        {
            return value >= average - threshold && value <= average + threshold;
        }

        private void SaveSessionToDisk()
        {
            string path = ConfigurationManager.AppSettings["SessionDataPath"];

            if (string.IsNullOrWhiteSpace(path))
                path = Directory.GetCurrentDirectory();

            string fileName = $"session_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string filePath = Path.Combine(path, fileName);

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Time,WindSpeed,WindAngle,AccX,AccY,AccZ");

                foreach (var sample in _currentSessionSamples)
                {
                    writer.WriteLine($"{sample.Time.TotalSeconds.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{sample.WindSpeed.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{sample.WindAngle.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{sample.LinearAccelerationX.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{sample.LinearAccelerationY.ToString(CultureInfo.InvariantCulture)}," +
                                     $"{sample.LinearAccelerationZ.ToString(CultureInfo.InvariantCulture)}");
                }
            }
        }
    }
}
