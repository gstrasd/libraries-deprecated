using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Library.Cryptography
{
    public static class HashAlgorithm
    {
        public static System.Security.Cryptography.HashAlgorithm Sha1 => System.Security.Cryptography.HashAlgorithm.Create("SHA1");
        public static System.Security.Cryptography.HashAlgorithm Md5 => System.Security.Cryptography.HashAlgorithm.Create("MD5");
        public static System.Security.Cryptography.HashAlgorithm Sha256 => System.Security.Cryptography.HashAlgorithm.Create("SHA256");
        public static System.Security.Cryptography.HashAlgorithm Sha384 => System.Security.Cryptography.HashAlgorithm.Create("SHA384");
        public static System.Security.Cryptography.HashAlgorithm Sha512 => System.Security.Cryptography.HashAlgorithm.Create("SHA512");

        public static string ComputeBase64Hash(this System.Security.Cryptography.HashAlgorithm hashAlgorithm, byte[] buffer)
        {
            if (hashAlgorithm == null) throw new ArgumentNullException(nameof(hashAlgorithm));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            var hash = hashAlgorithm.ComputeHash(buffer);

            return Convert.ToBase64String(hash);
        }

        public static string ComputeBase64Hash(this System.Security.Cryptography.HashAlgorithm hashAlgorithm, Stream stream)
        {
            if (hashAlgorithm == null) throw new ArgumentNullException(nameof(hashAlgorithm));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var position = stream.Position;
            var hash = hashAlgorithm.ComputeHash(stream);
            stream.Position = position;

            return Convert.ToBase64String(hash);
        }

        public static Task<string> ComputeBase64HashAsync(this System.Security.Cryptography.HashAlgorithm hashAlgorithm, byte[] buffer)
        {
            if (hashAlgorithm == null) throw new ArgumentNullException(nameof(hashAlgorithm));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            var task = Task.Run(() =>
            {
                var hash = hashAlgorithm.ComputeHash(buffer);

                return Convert.ToBase64String(hash);
            });

            return task;
        }

        public static Task<string> ComputeBase64HashAsync(this System.Security.Cryptography.HashAlgorithm hashAlgorithm, Stream stream)
        {
            if (hashAlgorithm == null) throw new ArgumentNullException(nameof(hashAlgorithm));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var task = Task.Run(() =>
            {
                var position = stream.Position;
                var hash = hashAlgorithm.ComputeHash(stream);
                stream.Position = position;

                return Convert.ToBase64String(hash);
            });

            return task;
        }
    }
}
