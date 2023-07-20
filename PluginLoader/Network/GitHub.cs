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

        public static void Init()
        {
            // Fix tls 1.2 not supported on Windows 7 - github.com is tls 1.2 only
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch (NotSupportedException e)
            {
                LogFile.Error("An error occurred while setting up networking, web requests will probably fail: " + e);
            }
        }

        public static Stream GetStream(Uri uri)
        {
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            Config.PluginConfig config = Main.Instance.Config;
            request.Timeout = config.NetworkTimeout;
            if(!config.AllowIPv6)
                request.ServicePoint.BindIPEndPointDelegate = BlockIPv6;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            MemoryStream output = new MemoryStream();
            using (Stream responseStream = response.GetResponseStream())
                responseStream.CopyTo(output);
            output.Position = 0;
            return output;
        }

        private static IPEndPoint BlockIPv6(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount)
        {
            if (remoteEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return new IPEndPoint(IPAddress.Any, 0);

            throw new InvalidOperationException("No IPv4 address");
        }

        public static Stream DownloadRepo(string name, string commit)
        {
            Uri uri = new Uri(string.Format(repoZipUrl, name, commit), UriKind.Absolute);
            LogFile.WriteLine("Downloading " + uri);
            return GetStream(uri);
        }

        public static Stream DownloadFile(string name, string commit, string path)
        {
            Uri uri = new Uri(string.Format(rawUrl, name, commit) + path.TrimStart('/'), UriKind.Absolute);
            LogFile.WriteLine("Downloading " + uri);
            return GetStream(uri);
        }

    }
}
