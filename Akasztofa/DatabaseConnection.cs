using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Akasztofa
{
    internal class DatabaseConnection
    {
        private HttpClient client;

        public DatabaseConnection(string databaseURI)
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All
            };

            client = new HttpClient();
            client.BaseAddress = new Uri(databaseURI);
        }

        public string GetRequest(string query)
        {
            Task<HttpResponseMessage> response = client.GetAsync(query);
            while (!response.IsCompleted);

            Task<string> result = response.Result.Content.ReadAsStringAsync();
            while (!result.IsCompleted);

            string res = result.Result;
            int start = res.IndexOf("<body>") + "<body>".Length;
            int end = res.IndexOf("</body>");
            while (res[start] != '{') start++;
            while (res[end - 1] != '}') end--;

            string jsonPart = res.Substring(start, end - start);
            return jsonPart;
        }
    }
}
