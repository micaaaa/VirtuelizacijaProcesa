using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class ServiceResponse
    {
            [DataMember]
            public bool IsAck { get; set; } 

            [DataMember]
            public SessionStatus Status { get; set; } 
       
    }
}
