using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;
using TestMocksGenerator.Models;
using TestMocksGenerator.Models.Const;

namespace TestMocksGenerator
{
    public class WebHelper
    {
        private static WebHelper _context;
        private string AuthToken { get; set; }
        private string ApplicationToken { get; set; }

        internal static WebHelper GetWebHelper()
        {
            if (_context == null)
                _context = new WebHelper(WebConst.AuthRequestAddr, WebConst.AuthToken);

            return _context;
        }

        internal static Data GetEntity(string entityType, string entityId)
        {
            GetWebHelper();

            var result = _context.SendRequest(string.Format(WebConst.EntityRequestAddr, entityType, entityId), "", method: "GET");

            return JsonConvert.DeserializeObject<Data>(result);
        }

        internal static Data[] GetEntityWithQuery(string entityType, string query)
        {
            GetWebHelper();

            var result = _context.SendRequest(string.Format(WebConst.EntityQueryRequestAddr, entityType, query), "", method: "GET");

            return JsonConvert.DeserializeObject<Data[]>(result);
        }

        public WebHelper(string address, string appToken)
        {
            ApplicationToken = appToken;

            var request = new ElmaRequest();

            var response = SendRequest(address, JsonConvert.SerializeObject(request, Formatting.Indented), true);

            AuthToken = JsonConvert.DeserializeObject<B2BResponse>(response).Token;
        }

        public string SendRequest(string url, string content, bool auth = false, string method = "POST")
        {
            var request = WebRequest.Create(url);
            request.Method = method;
            request.ContentType = "application/json";
            request.Timeout = 180000;

            if (!string.IsNullOrEmpty(AuthToken))
                request.Headers.Add("AuthToken", AuthToken);

            if (auth)
                request.Headers.Add("ApplicationToken", ApplicationToken);

            if (method == "POST")
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    streamWriter.Write(content);
            }

            try
            {
                using (var response = request.GetResponse())
                {
                    if (response == null || ((HttpWebResponse)response).StatusCode != HttpStatusCode.OK)
                        throw new WebException("Сервис не отвечает");

                    using (var dataStream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(dataStream))
                        {
                            var result = reader.ReadToEnd();

                            return result;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    throw;
                }

                using (var responseStream = ex.Response.GetResponseStream())
                {
                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        throw new WebException("Произошла ошибка при отправке запроса: " + reader.ReadToEnd());
                    }
                }
            }
        }

        public class ElmaRequest
        {
        }

        public class B2BResponse
        {
            [JsonProperty("AuthToken")]
            public string Token { get; set; }
        }
    }
}
