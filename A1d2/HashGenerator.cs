using System.Security.Cryptography;
using System.Text;

namespace A1d2
{
    static class HashGenerator
    {
        public static string GetHash(byte[] input)
        {
            using (SHA1Managed sha = new SHA1Managed())
            {
                var hash = sha.ComputeHash(input);
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }
    }
}
