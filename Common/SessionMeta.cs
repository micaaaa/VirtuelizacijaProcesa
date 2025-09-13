using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SessionMeta
    {
        [DataMember]
        public double LinearAccelerationX { get; set; }

        [DataMember]
        public double LinearAccelerationY { get; set; }

        [DataMember]
        public double LinearAccelerationZ { get; set; }

        [DataMember]
        public double WindSpeed { get; set; }

        [DataMember]
        public double WindAngle { get; set; }

        [DataMember]
        public TimeSpan Time { get; set; } 

        public override string ToString()
        {
            return $"{Time.TotalSeconds},{WindSpeed}, {WindAngle}, {LinearAccelerationX}, {LinearAccelerationY}, {LinearAccelerationZ}";
        }
    }
}
