using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json;
using PostTracking.Entities;

namespace PostTracking.Domain
{
    internal class Tracking
    {
        private static readonly HttpClient Client = new HttpClient();
        private const string JapanPostUrl = "https://trackings.post.japanpost.jp/services/srv/search/?requestNo1={0}&search=追跡スタート";

        public async Task<(bool isFinish, string finishDate)> CheckPost(string inquiryNumber)
        {
            var response = await Client.GetAsync(string.Format(JapanPostUrl, inquiryNumber));
            var source = await response.Content.ReadAsStringAsync();
            try
            {
                var parser = new HtmlParser();
                var doc = await parser.ParseDocumentAsync(source);

                var tableElements = doc.QuerySelectorAll("table");

                var historyTable = default(IElement);

                foreach (var table in tableElements)
                {
                    foreach (var attr in table.Attributes)
                    {
                        if (attr.Name == "summary" && attr.Value == "履歴情報")
                        {
                            historyTable = table;
                            break;
                        }
                    }
                }

                //履歴情報の最後の行のうち、1つ目の要素が必要なデータとなる
                var history = historyTable?.QuerySelectorAll("tr").Skip(2).Reverse().Skip(1).Take(1).FirstOrDefault();

                var data = history?.GetElementsByTagName("td");

                return data?[1].TextContent == "お届け先にお届け済み" ? (true, data?[0].TextContent) : (false, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public async Task PostWebHook(string url, string inquiryNumber, string date)
        {
            var jsonData = new Discord
            {
                Content = $"追跡番号{inquiryNumber}の荷物は{date}に配達が完了しました。"
            };

            var json = JsonConvert.SerializeObject(jsonData);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Client.PostAsync(url, content);

            await using var stream = (await response.Content.ReadAsStreamAsync());
            using var reader = (new StreamReader(stream, Encoding.UTF8, true)) as TextReader;

            await reader.ReadToEndAsync();
        }
    }
}
