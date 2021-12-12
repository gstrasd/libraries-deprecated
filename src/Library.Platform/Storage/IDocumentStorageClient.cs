using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Platform.Storage
{
    public interface IDocumentStorageClient
    {
        string Store { get; }
        Task<bool> DocumentExistsAsync(string path, CancellationToken token = default);
        IAsyncEnumerable<string> ListDocumentsAsync(string path, CancellationToken token = default);
        Task<Stream> LoadDocumentAsync(string path, CancellationToken token = default);
        Task SaveDocumentAsync(string path, Stream stream, CancellationToken token = default);
        Task DeleteDocumentAsync(string path, CancellationToken token = default);
        Task DeleteDocumentsAsync(string path, CancellationToken token = default);
    }
}
