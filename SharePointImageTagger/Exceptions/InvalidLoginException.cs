using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePointImageTagger.Exceptions
{
    public class InvalidLoginException : Exception
    {
        public InvalidLoginException()
        {

        }

        public InvalidLoginException(string Message) : base(Message)
        {

        }

        public InvalidLoginException(string Message, Exception Inner)
            : base(Message, Inner)
        {

        }
    }
}
