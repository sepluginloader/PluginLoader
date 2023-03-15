using System;
using System.IO;
using System.Net;

namespace avaness.PluginLoader.Network
{
    public static class GitHub
    {

        public const string listRepoName = "sepluginloader/PluginHub";
        public const string listRepoCommit = "main";
        public const string listRepoHash = "plugins.sha1";

        private const string repoZipUrl = "https://github.com/{0}/archive/{1}.zip";
        private const string rawUrl = "https://raw.githubusercontent.com/{0}/{1}/";

        public static Stream DownloadRepo(string name, string commit, out string fileName)
        {
            Uri uri = new Uri(string.Format(repoZipUrl, name, commit), UriKind.Absolute);
            LogFile.WriteLine("Downloading " + uri);
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = Main.Instance.Config.NetworkTimeout;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            fileName = response.Headers["Content-Disposition"];
            if(fileName != null)
            {
                int index = fileName.IndexOf("filename=");
                if(index >= 0)
                {
                    index += "filename=".Length;
                    fileName = fileName.Substring(index).Trim('"');
                }
            }

            MemoryStream output = new MemoryStream();
            using (Stream responseStream = response.GetResponseStream())
                responseStream.CopyTo(output);
            output.Position = 0;
            return output;
        }

        public static Stream DownloadFile(string name, string commit, string path)
        {
            Uri uri = new Uri(string.Format(rawUrl, name, commit) + path.TrimStart('/'), UriKind.Absolute);
            LogFile.WriteLine("Downloading " + uri);
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = Main.Instance.Config.NetworkTimeout;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            MemoryStream output = new MemoryStream();
            using (Stream responseStream = response.GetResponseStream())
                responseStream.CopyTo(output);
            output.Position = 0;
            return output;
        }

    }
}
