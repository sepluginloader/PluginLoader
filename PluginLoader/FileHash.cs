using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace avaness.PluginLoader
{
    public static class FileHash
    {
        public static string GetHash(string file)
        {
			using (FileStream fileStream = new FileStream(file, FileMode.Open))
			{
				using (BufferedStream bufferedStream = new BufferedStream(fileStream))
				{
					using (SHA1Managed sha = new SHA1Managed())
					{
						byte[] hash = sha.ComputeHash(bufferedStream);
						StringBuilder sb = new StringBuilder(2 * hash.Length);
						foreach (byte b in hash)
							sb.AppendFormat("{0:x2}", b);
						return sb.ToString();
					}
				}
			}
		}
    }
}
