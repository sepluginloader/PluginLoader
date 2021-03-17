using LitJson;
using System;

namespace avaness.PluginLoader.Network
{
    public class GitHubFile
    {
        public string Name { get; }
        public Uri DownloadUrl { get; }
        public bool IsDirectory { get; }

        public GitHubFile(string name, Uri downloadUrl, bool isDirectory)
        {
            Name = name;
            DownloadUrl = downloadUrl;
            IsDirectory = isDirectory;
        }

        public static bool TryGet(JsonData data, out GitHubFile file)
        {
            file = null;
            if(data.IsObject && data.ContainsKey("name") && data.ContainsKey("download_url") && data.ContainsKey("type"))
            {
                JsonData name = data["name"];
                if (name == null || !name.IsString)
                    return false;

                JsonData type = data["type"];
                if (type == null || !type.IsString)
                    return false;

                JsonData downloadUrl = data["download_url"];
                Uri uri = null;
                if (downloadUrl != null && downloadUrl.IsString && !Uri.TryCreate((string)downloadUrl, UriKind.Absolute, out uri))
                    return false;

                file = new GitHubFile((string)name, uri, (string)type != "dir");
                return true;

            }
            return false;
        }
    }
}
