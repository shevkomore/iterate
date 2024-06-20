using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace iterate.versions
{
    public class VersionTreeData
    {
        public string RootId {  get; set; }
        public List<VersionNode> Nodes { get; set; }
        public string CurrentNode {  get; set; }
        public VersionTreeData() { Nodes = new List<VersionNode>(); }

    }
}
