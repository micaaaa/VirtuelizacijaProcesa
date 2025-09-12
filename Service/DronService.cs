using System;
using System.Collections.Generic;
using System.Configuration;
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

        private readonly double W_THRESHOLD = double.Parse(ConfigurationManager.AppSettings["W_threshold"]);
        private readonly double A_THRESHOLD = double.Parse(ConfigurationManager.AppSettings["A_threshold"]);

        public ServiceResponse StartSession(SessionMeta meta)
        {
            if (meta == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault("Session meta cannot be null"));

            if (!IsValidMeta(meta))
                throw new FaultException<ValidationFault>(new ValidationFault("Session meta contains invalid or missing data"));

            if (_sessionActive)
            {
                return new ServiceResponse
                {
                    IsAck = false,
                    Status = SessionStatus.IN_PROGRESS
                };
            }

            _currentMeta = meta;
            _currentSessionSamples.Clear();
            _sessionActive = true;

            return new ServiceResponse
            {
                IsAck = true,
                Status = SessionStatus.IN_PROGRESS
            };
        }

        public ServiceResponse PushSample(DroneSample sample)
        {
            if (!_sessionActive)
            {
                return new ServiceResponse
                {
                    IsAck = false,
                    Status = SessionStatus.COMPLETED
                };
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

                return new ServiceResponse
                {
                    IsAck = true,
                    Status = SessionStatus.IN_PROGRESS
                };
            }
            else
            {
                return new ServiceResponse
                {
                    IsAck = false,
                    Status = SessionStatus.IN_PROGRESS
                };
            }
        }

        public ServiceResponse EndSession()
        {
            if (!_sessionActive)
            {
                return new ServiceResponse
                {
                    IsAck = false,
                    Status = SessionStatus.COMPLETED
                };
            }

            try
            {
                SaveSessionToDisk();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Došlo je do greške prilikom završetka sesije: " + ex.Message);
            }

            _sessionActive = false;
            _currentSessionSamples.Clear();
            _currentMeta = null;

            return new ServiceResponse
            {
                IsAck = true,
                Status = SessionStatus.COMPLETED
            };
        }


        private bool IsValidMeta(SessionMeta meta)
        {
            if (meta.WindSpeed <= 0)
                return false;
            if (meta.Time == default)
                return false;

            if (!IsValidAcceleration(meta.LinearAccelerationX)) return false;
            if (!IsValidAcceleration(meta.LinearAccelerationY)) return false;
            if (!IsValidAcceleration(meta.LinearAccelerationZ)) return false;

            if (meta.WindAngle < 0 || meta.WindAngle > 360)
                return false;

            return true;
        }

        private bool IsValidSample(DroneSample sample)
        {
            if (sample.WindSpeed <= 0)
                return false;
            if (sample.Time == default)
                return false;

            if (!IsValidAcceleration(sample.LinearAccelerationX)) return false;
            if (!IsValidAcceleration(sample.LinearAccelerationY)) return false;
            if (!IsValidAcceleration(sample.LinearAccelerationZ)) return false;

            if (sample.WindAngle < 0 || sample.WindAngle > 360)
                return false;

            return true;
        }

        private bool IsValidAcceleration(double acc)
        {

            return acc >= -100 && acc <= 100;
        }

        private bool IsWithinThreshold(double value, double average, double threshold)
        {
            double lower = average * 0.75;
            double upper = average * 1.25;
            return value >= lower && value <= upper && Math.Abs(value - average) <= threshold;
        }

        private void SaveSessionToDisk()
        {
            string fileName = $"Session_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            try
            {
                using (var fileHandler = new FileHandler(filePath))
                {
                    fileHandler.WriteToFile("LinearAccelerationX, LinearAccelerationY, LinearAccelerationZ, WindSpeed, WindAngle, Time");

                    foreach (var sample in _currentSessionSamples)
                    {
                        fileHandler.WriteToFile($"{sample.LinearAccelerationX}, {sample.LinearAccelerationY}, {sample.LinearAccelerationZ}, {sample.WindSpeed}, {sample.WindAngle}, {sample.Time:O}");
                    }
                    throw new Exception("Simulacija greške tokom zapisivanja sesije!");
                }
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"Greška pri pisanju u fajl: {ex.Message}");
            }
        }

    }
}
