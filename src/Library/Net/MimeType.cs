using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Net
{
    public class MimeType
    {
        internal MimeType(string extension, string contentType)
        {
            Extension = extension;
            ContentType = contentType;
        }

        public string Extension { get; }
        public string ContentType { get; }
    }
}