﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class ValidationFault
    {
        public ValidationFault(string message)
        {
            Message = message;
        }

        [DataMember]
        public string Message { get; set; }
    }
}
