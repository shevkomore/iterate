using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iterate
{
    public interface IIDGenerator
    {
        string Generate();
        string Hash(Stream stream);
    }
}
