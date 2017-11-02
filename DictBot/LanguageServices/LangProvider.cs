using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Configuration.ConfigurationManager;

namespace LanguageServices
{
    public static class LangProvider
    {
        public async static Task<string> Translate(string str, string toLang)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", AppSettings["TransKey"]);

            string uri = $"https://api.microsofttranslator.com/V2/Http.svc/Translate?text={str}&to={toLang}&contentType=text/plain";
            var response = await client.GetAsync(uri);
            var xDoc = XDocument.Load(await response.Content.ReadAsStreamAsync());

            return xDoc.Document.Root.Value;
        }

        public async static Task<SpellingResponse> CheckSpelling(string str, string lang)
        {
            HttpClient client = new HttpClient();
            // TODO: hide the key from outside world
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", AppSettings["SpellingKey"]);

            // The following headers are optional, but it is recommended they be treated as required.
            // These headers help the service return more accurate results.
            //client.DefaultRequestHeaders.Add("X-Search-Location", ClientLocation);
            //client.DefaultRequestHeaders.Add("X-MSEdge-ClientID", ClientId);
            //client.DefaultRequestHeaders.Add("X-MSEdge-ClientIP", ClientIp);

            HttpResponseMessage response = new HttpResponseMessage();
            string uri = "https://api.cognitive.microsoft.com" + "/bing/v7.0/spellcheck?";

            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("mkt", lang));
            values.Add(new KeyValuePair<string, string>("mode", "proof"));
            values.Add(new KeyValuePair<string, string>("text", str));

            using (FormUrlEncodedContent content = new FormUrlEncodedContent(values))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                response = await client.PostAsync(uri, content);
            }

            string contentString = await response.Content.ReadAsStringAsync();
            SpellingResponse result = JsonConvert.DeserializeObject<SpellingResponse>(contentString);

            return result;
        }       
    }

    //class Instrumentation
    //{
    //    public string pingUrlBase { get; set; }
    //    public string pageLoadPingUrl { get; set; }
    //}

    public class Suggestion
    {
        public string suggestion { get; set; }
        public double score { get; set; }
        //public string pingUrlSuffix { get; set; }
    }

    public class FlaggedToken
    {
        public int offset { get; set; }
        public string token { get; set; }
        //public string type { get; set; }
        public List<Suggestion> suggestions { get; set; }
        //public string pingUrlSuffix { get; set; }
    }

    public class SpellingResponse
    {
        //public string _type { get; set; }
        //public Instrumentation instrumentation { get; set; }
        public List<FlaggedToken> flaggedTokens { get; set; }
    }
}
