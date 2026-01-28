using System;
using System.Collections.Generic;
using System.Text;

namespace Czemi.DataMethods
{
    public class DataReadException : Exception
    {
        public DataReadException()
        {
        }
        public DataReadException(string message)
            : base(message)
        {
        }
        public DataReadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
