using System;
using System.IO;
using System.Text;
using SHA3.Net;

namespace SHA_3
{
    public static class SHA3Helper
    {
        public enum SHA3Algorithm
        {
            SHA3_224,
            SHA3_256,
            SHA3_384,
            SHA3_512
        }

        public static string ComputeHash(string input, SHA3Algorithm algorithm)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = ComputeHashBytes(bytes, algorithm);
            return BytesToHex(hashBytes);
        }

        public static string ComputeFileHash(string filePath, SHA3Algorithm algorithm)
        {
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            var hashBytes = ComputeHashBytes(buffer, algorithm);
            return BytesToHex(hashBytes);
        }

        private static byte[] ComputeHashBytes(byte[] input, SHA3Algorithm algorithm)
        {
            return algorithm switch
            {
                SHA3Algorithm.SHA3_224 => Sha3.Sha3224().ComputeHash(input),
                SHA3Algorithm.SHA3_256 => Sha3.Sha3256().ComputeHash(input),
                SHA3Algorithm.SHA3_384 => Sha3.Sha3384().ComputeHash(input),
                SHA3Algorithm.SHA3_512 => Sha3.Sha3512().ComputeHash(input),
                _ => Sha3.Sha3256().ComputeHash(input)
            };
        }

        private static string BytesToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
