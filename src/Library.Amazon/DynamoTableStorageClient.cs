using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DataModel;
using Library.Platform.Storage;

namespace Library.Amazon
{
    public class DynamoTableStorageClient : ITableStorageClient
    {
        private readonly IDynamoDBContext _context;

        public DynamoTableStorageClient(IDynamoDBContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsAsync(object[] keys, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync<T>(T entity, CancellationToken token = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            return _context.SaveAsync(entity, token);
        }

        public Task<T> FindAsync<T>(object[] keys, CancellationToken token = default)
        {
            if (keys == null || keys.Length == 0 || keys.Length > 2) throw new ArgumentException("Exactly one or two keys must be used to find the requested entity.", nameof(keys));

            if (keys.Length == 1)
            {
                return _context.LoadAsync<T>(keys[0], token);
            }

            return _context.LoadAsync<T>(keys[0], keys[1], token);
        }

        public Task RemoveAsync(object[] keys, CancellationToken token = default)
        {
            if (keys == null || keys.Length == 0 || keys.Length > 2) throw new ArgumentException("Exactly one or two keys must be used to delete the requested entity.", nameof(keys));

            if (keys.Length == 1)
            {
                return _context.DeleteAsync(keys[0], token);
            }

            // TODO: This must be tested. If it works then the method info needs to be static to avoid reflection on each call
            var method = _context.GetType().GetMethod("DeleteAsync", new[] {typeof(object), typeof(object), typeof(CancellationToken)});
            return (Task)method!.Invoke(keys[0], new[] {keys[1], token});
        }
    }
}
