using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Library.Resources
{
    public class Resource
    {
        private readonly Assembly _assembly;
        private readonly string _namespace;

        public Resource(Assembly assembly, string @namespace = default)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            _assembly = assembly;
            _namespace = @namespace;
        }

        public Stream GetStream(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var resourceName = _namespace == default ? name : $"{_namespace}.{name}";
            var resource = _assembly.GetManifestResourceStream(resourceName);
            return resource;
        }

        public byte[] GetBytes(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var resourceName = _namespace == default ? name : $"{_namespace}.{name}";
            using var resource = _assembly.GetManifestResourceStream(resourceName);
            using var stream = new MemoryStream();
            resource.CopyTo(stream);

            return stream.ToArray();
        }

        public string GetBase64String(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var resourceName = _namespace == default ? name : $"{_namespace}.{name}";
            using var resource = _assembly.GetManifestResourceStream(resourceName);
            using var stream = new MemoryStream();
            resource.CopyTo(stream);
            var base64 = Convert.ToBase64String(stream.ToArray());
            
            return base64;
        }
    }
}
