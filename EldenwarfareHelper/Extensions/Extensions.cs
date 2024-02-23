
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace EldenwarfareHelper
{
    public static class Extensions
    {
        public static async Task DownloadFile(this HttpClient client, string address, string fileName)
        {
            using (var response = await client.GetAsync(address))
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var file = File.OpenWrite(fileName))
            {
                stream.CopyTo(file);
            }
        }
    }
}
