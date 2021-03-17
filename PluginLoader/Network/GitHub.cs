using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace avaness.PluginLoader.Network
{
    public static class GitHub
    {
        private const string githubApi = "https://api.github.com/repos/austinvaness/PluginLoader/";

        private static JsonData GetResponse(string path)
        {
            Uri uri = new Uri(githubApi + path, UriKind.Absolute);
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.UserAgent = "Space-Engineers-Plugin-Loader";
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return JsonMapper.ToObject(reader);
            }
        }

        public static IEnumerable<GitHubFile> GetFiles(string path)
        {
            JsonData json = GetResponse("contents/" + path.TrimStart('/'));
            if (json.IsArray)
            {
                GitHubFile temp;
                foreach(JsonData fileJson in json)
                {
                    if (GitHubFile.TryGet(fileJson, out temp))
                        yield return temp;
                }
            }
        }
    }
}
