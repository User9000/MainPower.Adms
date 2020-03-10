using System;
using System.Runtime.Serialization;

namespace MainPower.Adms.ExtractManager
{
    [Serializable]
    internal class EnricherException : Exception
    {
        public EnricherException()
        {
        }

        public EnricherException(string message) : base(message)
        {
        }

        public EnricherException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EnricherException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}