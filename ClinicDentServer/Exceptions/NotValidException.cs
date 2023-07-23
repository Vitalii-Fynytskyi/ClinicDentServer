using System;

namespace ClinicDentServer.Exceptions
{
    public class NotValidException : Exception
    {
        public NotValidException() { }
        public NotValidException(string message) : base(message) { }
    }
}
