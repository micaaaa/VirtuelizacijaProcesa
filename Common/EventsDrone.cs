using System;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class TransferEventArgs : EventArgs
    {
        [DataMember]
        public string Message { get; private set; }

        public TransferEventArgs(string message)
        {
            Message = message;
        }
    }

    [DataContract]
    public class SampleEventArgs : EventArgs
    {
        [DataMember]
        public double LinearAccelerationX { get; private set; }

        [DataMember]
        public double LinearAccelerationY { get; private set; }

        [DataMember]
        public double LinearAccelerationZ { get; private set; }

        [DataMember]
        public double WindSpeed { get; private set; }

        [DataMember]
        public double WindAngle { get; private set; }

        [DataMember]
        public double Time { get; private set; }

        public SampleEventArgs(
            double linearAccelerationX,
            double linearAccelerationY,
            double linearAccelerationZ,
            double windSpeed,
            double windAngle,
            double time)
        {
            LinearAccelerationX = linearAccelerationX;
            LinearAccelerationY = linearAccelerationY;
            LinearAccelerationZ = linearAccelerationZ;
            WindSpeed = windSpeed;
            WindAngle = windAngle;
            Time = time;
        }

        // Alternativno možeš dodati konstruktor iz DroneSample objekta ako postoji
        public SampleEventArgs(DroneSample sample)
        {
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));

            LinearAccelerationX = sample.LinearAccelerationX;
            LinearAccelerationY = sample.LinearAccelerationY;
            LinearAccelerationZ = sample.LinearAccelerationZ;
            WindSpeed = sample.WindSpeed;
            WindAngle = sample.WindAngle;
            Time = sample.Time;
        }
    }

    [DataContract]
    public class WarningEventArgs : EventArgs
    {
        [DataMember]
        public string Warning { get; private set; }

        public WarningEventArgs(string warning)
        {
            Warning = warning;
        }
    }

    [DataContract]
    public class TransferCompletedEventArgs : EventArgs
    {
        [DataMember]
        public string Message { get; private set; }

        public TransferCompletedEventArgs(string message)
        {
            Message = message;
        }
    }
}
