using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Library.Http;

namespace Library.Amazon.Resources
{
    internal static class StringResources
    {
        internal static readonly IDictionary<string, string> ExceptionMessages;

        static StringResources()
        {
            ExceptionMessages = BuildStringResource("ExceptionMessages.json");
        }

        private static IDictionary<string, string> BuildStringResource(string filename)
        {
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Library.Amazon.Resources.{filename}");
            using var document = JsonDocument.Parse(stream, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            
            var enumerator = document.RootElement.EnumerateObject();
            while (enumerator.MoveNext())
            {
                dictionary.Add(enumerator.Current.Name, enumerator.Current.Value.GetString());
            }

            return dictionary;
        }
    }
}
