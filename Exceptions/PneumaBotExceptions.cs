using System;

namespace PneumaBot2
{
    public class PneumaBotExceptions : Exception
    {
        public PneumaBotExceptions()
        {
        }

        public PneumaBotExceptions(string message)
            : base(message)
        {
        }

        public PneumaBotExceptions(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
