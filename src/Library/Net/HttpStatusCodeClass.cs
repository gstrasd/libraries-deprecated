using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Net
{
    public enum HttpStatusCodeClass
    {
        Informational = 100,
        Success = 200,
        Redirection = 300,
        ClientError = 400,
        ServerError = 500
    }
}