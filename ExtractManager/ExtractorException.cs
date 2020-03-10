using System;
using System.Runtime.Serialization;

namespace MainPower.Adms.ExtractManager
{
    [Serializable]
    internal class ExtractorException : Exception
    {
        public ExtractorException()
        {
        }

        public ExtractorException(string message) : base(message)
        {
        }

        public ExtractorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ExtractorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}