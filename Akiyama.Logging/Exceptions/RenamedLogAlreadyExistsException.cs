using System;

namespace Akiyama.Logging.Exceptions
{
    public class RenamedLogAlreadyExistsException : Exception
    {

        public RenamedLogAlreadyExistsException() : base() { }
        public RenamedLogAlreadyExistsException(string message) : base(message) { }

    }
}
