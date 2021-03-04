using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library
{
    public class UriComparer : IEqualityComparer<Uri>, IComparer<Uri>, IComparer
    {
        public static readonly UriComparer AbsoluteUri = new UriComparer(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase);
        public static readonly UriComparer HostAndPort = new UriComparer(UriComponents.HostAndPort, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase);
        public static readonly UriComparer HttpRequestUrl = new UriComparer(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase);
        public static readonly UriComparer PathAndQuery = new UriComparer(UriComponents.PathAndQuery, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase);
        public static readonly UriComparer SchemeAndServer = new UriComparer(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase);
        public static readonly UriComparer SchemeAgnosticHttpRequestUrl = new UriComparer(UriComponents.HostAndPort | UriComponents.PathAndQuery, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase);

        private readonly UriComponents _partsToCompare;
        private readonly UriFormat _compareFormat;
        private readonly StringComparison _comparisonType;
        private readonly int _hashCode;

        public UriComparer() : this(UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase)
        {
        }

        public UriComparer(UriComponents partsToCompare, UriFormat compareFormat, StringComparison comparisonType)
        {
            _partsToCompare = partsToCompare;
            _compareFormat = compareFormat;
            _comparisonType = comparisonType;
            _hashCode = HashCode.Combine(partsToCompare, compareFormat, comparisonType);
        }

        public bool Equals(Uri left, Uri right)
        {
            return Compare(left, right) == 0;
        }

        public int GetHashCode(Uri obj) => _hashCode;

        public int Compare(Uri left, Uri right)
        {
            if (left == null) return right == null ? 0 : -1;
            if (right == null) return 1;

            return Uri.Compare(left, right, _partsToCompare, _compareFormat, _comparisonType);
        }

        public int Compare(object left, object right)
        {
            return Compare(left as Uri, right as Uri);
        }
    }
}
