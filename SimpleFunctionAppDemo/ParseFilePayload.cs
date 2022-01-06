using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFunctionAppDemo
{
    public class ParseFilePayload
    {
        public string filePath { get; set; }
        public string containerName { get; set; }
        public string blobName { get; set; }
    }
}
