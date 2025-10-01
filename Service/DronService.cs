using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class DroneService : IDroneService, IDisposable
    {
        private StreamWriter measurementsWriter;
        private StreamWriter rejectsWriter;
        private bool sessionActive = false;
        private bool disposed = false;

        private double W_threshold;
        private double A_threshold;
        private double ThresholdPercentage;

        private DroneSample previousSample = null;
        private List<double> accelerationNormSamples = new List<double>();

        // Events
        public event EventHandler<TransferEventArgs> OnTransferStarted;
        public event EventHandler<SampleEventArgs> OnSampleReceived;
        public event EventHandler<WarningEventArgs> OnWarningRaised;
        public event EventHandler<TransferEventArgs> OnTransferCompleted;

        public DroneService()
        {
            LoadConfig();
        }

        ~DroneService()
        {
            Dispose(false);
        }

        public void LoadConfig()
        {
            W_threshold = double.Parse(ConfigurationManager.AppSettings["W_threshold"]);
            A_threshold = double.Parse(ConfigurationManager.AppSettings["A_threshold"]);
            ThresholdPercentage = double.Parse(ConfigurationManager.AppSettings["ThresholdPercentage"]);
        }

        public ServiceResponse StartSession(SessionMeta meta)
        {
            try
            {
                sessionActive = true;

                measurementsWriter = new StreamWriter("drone_measurements_session.csv");
                rejectsWriter = new StreamWriter("drone_rejects.csv");

                string header = "LinearAccelerationX,LinearAccelerationY,LinearAccelerationZ,WindSpeed,WindAngle,Time";
                measurementsWriter.WriteLine(header);
                rejectsWriter.WriteLine(header + ",ReasonReject");

                measurementsWriter.Flush();
                rejectsWriter.Flush();

                StartTransfer();

                Console.WriteLine("[INFO] Sesija je započeta");

                return new ServiceResponse
                {
                    ServiceType = ServiceType.ACK,
                    ServiceStatus = SessionStatus.IN_PROGRESS,
                    Message = "Sesija je započeta"
                };
            }
            catch (Exception ex)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault
                    {
                        Message = "Greška pri pokretanju sesije",
                        Details = ex.Message,
                        Field = "Session"
                    },
                    new FaultReason("Greška pri pokretanju sesije")
                );
            }
        }

        public void StartTransfer()
        {
            OnTransferStarted?.Invoke(this, new TransferEventArgs("Transfer je u toku..."));
        }

        // Nova metoda za obradu i prijem uzorka
        public void ReceiveSample(DroneSample sample)
        {
            OnSampleReceived?.Invoke(this, new SampleEventArgs(sample));

            // --- Analitika 1: Detekcija nagle promene ubrzanja (ΔA) ---
            double Anorm = Math.Sqrt(
                sample.LinearAccelerationX * sample.LinearAccelerationX +
                sample.LinearAccelerationY * sample.LinearAccelerationY +
                sample.LinearAccelerationZ * sample.LinearAccelerationZ);


            if (previousSample != null)
            {
                double Aprev_norm = Math.Sqrt(
                    previousSample.LinearAccelerationX * previousSample.LinearAccelerationX +
                    previousSample.LinearAccelerationY * previousSample.LinearAccelerationY +
                    previousSample.LinearAccelerationZ * previousSample.LinearAccelerationZ);

                double deltaA = Anorm - Aprev_norm;

                if (Math.Abs(deltaA) > A_threshold)
                {
                    string direction = deltaA > 0 ? "iznad očekivanog" : "ispod očekivanog";
                    OnWarningRaised?.Invoke(this, new WarningEventArgs($"[Acceleration Spike]: ΔA={deltaA:F2}, smer: {direction}"));
                }
            }

            // Računamo prosek ubrzanja
            accelerationNormSamples.Add(Anorm);
            double Amean = accelerationNormSamples.Average();

            double lowerBoundA = Amean * (1 - ThresholdPercentage / 100.0);
            double upperBoundA = Amean * (1 + ThresholdPercentage / 100.0);

            if (Anorm < lowerBoundA || Anorm > upperBoundA)
            {
                string direction = Anorm < lowerBoundA ? "ispod očekivane vrednosti" : "iznad očekivane vrednosti";
                OnWarningRaised?.Invoke(this, new WarningEventArgs($"[Out Of Bound Warning]: A={Anorm:F2}, Amean={Amean:F2}, smer: {direction}"));
            }

            double windAngleRad = sample.WindAngle * (Math.PI / 180.0);
            double Weffect = Math.Abs(sample.WindSpeed * Math.Sin(windAngleRad));

            if (Weffect > W_threshold)
            {
                string direction = Weffect > W_threshold ? "iznad očekivanog" : "ispod očekivanog";
                OnWarningRaised?.Invoke(this, new WarningEventArgs($"[Wind Spike]: Weffect={Weffect}, smer: {direction}"));
            }

            // Čuvanje prethodnog uzorka
            previousSample = new DroneSample { 
            LinearAccelerationX = sample.LinearAccelerationX,
            LinearAccelerationY = sample.LinearAccelerationY,
            LinearAccelerationZ = sample.LinearAccelerationZ,
            WindAngle = sample.WindAngle,
            WindSpeed = sample.WindSpeed,
            Time = sample.Time};
        }

        public ServiceResponse PushSample(DroneSample sample)
        {
            try
            {
                if (!sessionActive)
                    throw new FaultException<DataFormatFault>(
                        new DataFormatFault { Message = "Sesija nije aktivna", Field = "Session" },
                        new FaultReason("Sesija nije aktivna"));

                ValidateDroneSample(sample);

                Console.WriteLine("[INFO] Prenos je u toku...");

                ReceiveSample(sample);

                Console.WriteLine("[INFO] Zavrsen prenos...\n");

                return new ServiceResponse
                {
                    ServiceType = ServiceType.ACK,
                    ServiceStatus = SessionStatus.IN_PROGRESS,
                    Message = "Uzorak uspešno primljen"
                };
            }
            catch (FaultException<ValidationFault> ex)
            {
                return new ServiceResponse
                {
                    ServiceType = ServiceType.NACK,
                    ServiceStatus = SessionStatus.IN_PROGRESS,
                    Message = ex.Detail.Message
                };
            }
            catch (FaultException<DataFormatFault> ex)
            {
                return new ServiceResponse
                {
                    ServiceType = ServiceType.NACK,
                    ServiceStatus = SessionStatus.IN_PROGRESS,
                    Message = ex.Detail.Message
                };
            }
            catch (Exception ex)
            {
                WriteRejectSample(sample, "Neočekivana greška: " + ex.Message);
                return new ServiceResponse
                {
                    ServiceType = ServiceType.NACK,
                    ServiceStatus = SessionStatus.IN_PROGRESS,
                    Message = $"Neočekivana greška: {ex.Message}"
                };
            }
        }

        public ServiceResponse EndSession()
        {
            try
            {
                sessionActive = false;

                measurementsWriter?.Close();
                rejectsWriter?.Close();

                CompleteTransfer();

                Console.WriteLine("[INFO] Sesija je završena");

                return new ServiceResponse
                {
                    ServiceType = ServiceType.ACK,
                    ServiceStatus = SessionStatus.COMPLETED,
                    Message = "Sesija je završena"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    ServiceType = ServiceType.NACK,
                    ServiceStatus = SessionStatus.COMPLETED,
                    Message = ex.Message
                };
            }
        }

        public void CompleteTransfer()
        {
            OnTransferCompleted?.Invoke(this, new TransferEventArgs("Prenos završen."));
        }

        private void ValidateDroneSample(DroneSample sample)
        {
            if (sample == null)
                ThrowValidationAndLog(null, "Sample objekat je null");

            // Proverava numeričke vrednosti
            CheckFinite("LinearAccelerationX", sample.LinearAccelerationX, sample);
            CheckFinite("LinearAccelerationY", sample.LinearAccelerationY, sample);
            CheckFinite("LinearAccelerationZ", sample.LinearAccelerationZ, sample);
            CheckFinite("WindSpeed", sample.WindSpeed, sample);
            CheckFinite("WindAngle", sample.WindAngle, sample);
            CheckFinite("Time", sample.Time, sample);  // Provera Time-a kao numeričkog tipa

            if (sample.LinearAccelerationX < -1 || sample.LinearAccelerationX > 1)
                ThrowValidationAndLog(sample, "LinearAccelerationX mora biti u opsegu ((-1) - 1)");
            if (sample.LinearAccelerationY < -1 || sample.LinearAccelerationY > 1)
                ThrowValidationAndLog(sample, "LinearAccelerationY mora biti u opsegu ((-1) - 1)");
            if (sample.LinearAccelerationZ < -12 || sample.LinearAccelerationZ > 15)
                ThrowValidationAndLog(sample, "LinearAccelerationZ mora biti u opsegu ((-12) - 15)");

            if (sample.WindSpeed <= 0)
                ThrowValidationAndLog(sample, "WindSpeed mora biti veći od 0");

            if (sample.WindAngle < 150 || sample.WindAngle > 360)
                ThrowValidationAndLog(sample, "WindAngle mora biti u opsegu 150-360");

            if (sample.Time < 0 || sample.Time > 100) 
                ThrowValidationAndLog(sample, "Time mora biti unutar dozvoljenog opsega (0-100 sekundi)");
        }

        private void CheckFinite(string fieldName, double value, DroneSample sample)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                ThrowValidationAndLog(sample, $"{fieldName} nije validna numerička vrednost");
            }
        }

        private void ThrowValidationAndLog(DroneSample sample, string message)
        {
            WriteRejectSample(sample, message);
            throw new FaultException<ValidationFault>(
                new ValidationFault
                {
                    Message = message,
                    Field = "Sample",
                    Value = sample
                },
                new FaultReason(message));
        }

        private void WriteRejectSample(DroneSample sample, string reason)
        {
            if (rejectsWriter == null) return;

            if (sample == null)
            {
                rejectsWriter.WriteLine($"NULL_SAMPLE,{reason}");
                return;
            }

            rejectsWriter.WriteLine(
                $"{sample.LinearAccelerationX.ToString(CultureInfo.InvariantCulture)}," +
                $"{sample.LinearAccelerationY.ToString(CultureInfo.InvariantCulture)}," +
                $"{sample.LinearAccelerationZ.ToString(CultureInfo.InvariantCulture)}," +
                $"{sample.WindSpeed.ToString(CultureInfo.InvariantCulture)}," +
                $"{sample.WindAngle.ToString(CultureInfo.InvariantCulture)}," +
                $"{sample.Time},{reason}");
            rejectsWriter.Flush();

            Console.WriteLine($"[REJECT] Sample odbačen zbog: {reason}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    measurementsWriter?.Close();
                    measurementsWriter?.Dispose();

                    rejectsWriter?.Close();
                    rejectsWriter?.Dispose();
                }
                disposed = true;
            }
        }
    }
}
