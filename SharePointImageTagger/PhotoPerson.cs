using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePointImageTagger
{
    /// <summary>
    /// Value object representing a person with a collection of photos attached to them.
    /// </summary>
    public class PhotoPerson
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public List<Photo> Photos { get; set; }

        public PhotoPerson(int ID)
        {
            this.ID = ID;
            Photos = new List<Photo>();
        }

        public PhotoPerson()
        {
            Photos = new List<Photo>();
        }

        public PhotoPerson(int ID, string Name)
        {
            this.ID = ID;
            this.Name = Name;
            Photos = new List<Photo>();
        }

        
    }
}
