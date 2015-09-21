using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePointImageTagger
{
    /// <summary>
    /// Value object representing a Photo.  
    /// </summary>
    public class Photo
    {
        public byte[] Image { get; set; }
        public string ID { get; set; }
        
        public List<string> TextInPhoto { get; set; }
        public string LanguageDetectedInPhoto { get; set; }

        public int NumberOfMatchedFaces { get; set;  }

        public int NumberOfUnmatchedFaces { get; set; }

        public List<PhotoPerson> PeopleInPhoto { get; set; }

        public Photo()
        {
            PeopleInPhoto = new List<PhotoPerson>();
            TextInPhoto = new List<string>();
        }

    }
}
