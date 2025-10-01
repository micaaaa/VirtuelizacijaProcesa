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
        public string LinearAccelerationX { get; set; } = "LinearAccelerationX";

        [DataMember]
        public string LinearAccelerationY { get; set; } = "LinearAccelerationY";

        [DataMember]
        public string LinearAccelerationZ { get; set; } = "LinearAccelerationZ";

        [DataMember]
        public string WindSpeed { get; set; } = "WindSpeed";

        [DataMember]
        public string WindAngle { get; set; } = "WindAngle";

        [DataMember]
        public string Time { get; set; } = "Time";


    }
}
