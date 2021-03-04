using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Library.Resources
{
    public class JsonStringResource
    {
        private readonly IDictionary<string, string> _resource;

        public JsonStringResource(string filename) : this(Assembly.GetExecutingAssembly(), filename)
        {
        }

        public JsonStringResource(Assembly assembly, string filename)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            
            var resource = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("." + filename));
            if (resource == null) throw new ArgumentException($"Assembly does not contain a resource file named {filename}.", nameof(filename));

            using var stream = assembly.GetManifestResourceStream(resource);
            using var document = JsonDocument.Parse(stream, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });

            _resource = document.RootElement.EnumerateObject().ToDictionary(j => j.Name, j => j.Value.GetString(), StringComparer.Ordinal);
        }

        public string this[string key] => _resource[key];
        public IEnumerable<string> Keys => _resource.Keys;
        public string Format(string key, params object[] args) => String.Format(_resource[key], args);
    }
}
