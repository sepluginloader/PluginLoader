using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace avaness.PluginLoader.Tools
{
    public class PostHttpContent : HttpContent
    {
        private readonly byte[] content;

        public PostHttpContent(string content)
        {
            this.content = content == null ? null : Tools.Utf8.GetBytes(content);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            if (content != null && content.Length > 0)
                await stream.WriteAsync(content, 0, content.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = content.Length;
            return true;
        }
    }
}