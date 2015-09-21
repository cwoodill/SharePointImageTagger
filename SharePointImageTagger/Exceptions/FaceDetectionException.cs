using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePointImageTagger.Exceptions
{
    public class FaceDetectionException : Exception
    {
        public FaceDetectionException()
        {

        }

        public FaceDetectionException(string Message) : base(Message)
        {

        }

        public FaceDetectionException(string Message, Exception Inner)
            : base(Message, Inner)
        {

        }
    }
}
