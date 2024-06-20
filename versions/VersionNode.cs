using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iterate.versions
{
    public class VersionNode
    {
        public string Id {  get; set; }
        public VersionNode()
        {
            Children = new List<string>();
        }
        public List<string> Children { get; set; }
        public string TimeCreated { get; set; }
        public string Image { get; set; }
        public string Description { get; set; }
    }
}
