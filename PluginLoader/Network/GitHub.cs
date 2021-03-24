using System;
using System.IO;
using System.Net;

namespace avaness.PluginLoader.Network
{
    public static class GitHub
    {

        public const string listRepoName = "austinvaness/PluginHub";
        public const string listRepoCommit = "main";
        public const string listRepoHash = "plugins.sha1";

        private const string repoZipUrl = "https://github.com/{0}/archive/{1}.zip";
        private const string rawUrl = "https://raw.githubusercontent.com/{0}/{1}/";

        public static Stream DownloadRepo(string name, string commit)
        {
            Uri uri = new Uri(string.Format(repoZipUrl, name, commit), UriKind.Absolute);
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
        }

        public static Stream DownloadFile(string name, string commit, string path)
        {
            Uri uri = new Uri(string.Format(rawUrl, name, commit) + path.TrimStart('/'), UriKind.Absolute);
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
        }

    }
}
