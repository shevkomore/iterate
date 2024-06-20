using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace iterate
{
    public class BasicIdGenerator : IIDGenerator
    {
        private readonly int ID_LEN = 16;
        public string Generate()
        {
             return AsSafeString(RandomNumberGenerator.GetBytes(ID_LEN));
        }
        public string Hash(Stream stream)
        {
            long pos = stream.Position;
            HashAlgorithm hash = SHA1.Create();
            string res = AsSafeString(hash.ComputeHash(stream));
            stream.Position = pos;
            return res;
            
        }
        private static string AsSafeString(byte[] id)
        {
            return Convert.ToBase64String(id).Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }
    }
}
