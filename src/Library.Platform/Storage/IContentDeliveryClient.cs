using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Platform.Storage
{
    public interface IContentDeliveryClient
    {
        Task<Stream> DownloadAsync(string path, string filename);
        Task<bool> UploadAsync(Stream stream, string path, string filename);
        Task<bool> DeleteAsync(string path, string filename);
        Task<List<string>> ListFilesAsync(string path);
    }
}