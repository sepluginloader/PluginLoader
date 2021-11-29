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
        private const string releaseUrl = "https://github.com/{0}/releases/download/{1}/{2}";

        public static Stream DownloadRepo(string name, string commit, out string fileName)
        {
            Uri uri = new Uri(string.Format(repoZipUrl, name, commit), UriKind.Absolute);
            HttpWebResponse response = Download(uri);
            GetFileNameFromHeader(response, out fileName);
            return response.GetResponseStream();
        }

        public static Stream DownloadFile(string name, string commit, string path)
        {
            Uri uri = new Uri(string.Format(rawUrl, name, commit) + path.TrimStart('/'), UriKind.Absolute);
            HttpWebResponse response = Download(uri);
            return response.GetResponseStream();
        }

        public static Stream DownloadRelease(string name, string tag, string filename)
        {
            Uri uri = new Uri(string.Format(releaseUrl, name, tag, filename), UriKind.Absolute);
            HttpWebResponse response = Download(uri);
            return response.GetResponseStream();
        }

        private static HttpWebResponse Download(Uri uri)
        {
            LogFile.WriteLine("Downloading " + uri);
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response;
        }

        private static void GetFileNameFromHeader(HttpWebResponse response, out string fileName)
        {
            fileName = response.Headers["Content-Disposition"];
            if (fileName == null)
                return;

            int index = fileName.IndexOf("filename=", StringComparison.InvariantCulture);
            if (index < 0)
                return;

            index += "filename=".Length;
            fileName = fileName.Substring(index).Trim('"');
        }
    }
}