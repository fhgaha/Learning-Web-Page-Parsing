using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Learning_Web_Page_Parsing
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var baseUrlString = "https://www.toy.ru/catalog/boy_transport/";
            var additionUrlString = "?filterseccode%5B0%5D=transport&PAGEN_8=";

            var csv = new StringBuilder();
            csv.Append("Название региона, Хлебные крошки, Название товара, Цена, Цена старая, Раздел с наличием, Ссылки на картинки, Ссылка на товар\n");

            for (int i = 0; i < 11; i++) //11
            {
                //mock

                //string urlString = "https://boytransport.free.beeceptor.com/catalog/boy_transport/";
                //string urlString = "https://160efba4-87de-4a2f-80f1-ff9307e890e6.mock.pstmn.io/boy_transport/";

                string urlString = baseUrlString;
                if (i > 1)
                    urlString = baseUrlString + additionUrlString + i.ToString();

                IHtmlDocument doc = await ParsePage(urlString);

                if (doc is null) continue;

                //var cards = doc.QuerySelectorAll("div.h-100.product-card");
                var cards = doc.GetElementsByClassName("h-100 product-card");

                foreach (var card in cards)
                {
                    //mock
                    //var productUrl = "https://boytransport.free.beeceptor.com/catalog/modeli_mashin/welly_43710_velli_model_mashiny_1_34_39_kia_sorento/";
                    //var productUrl = "https://160efba4-87de-4a2f-80f1-ff9307e890e6.mock.pstmn.io/catalog/modeli_mashin/welly_43710_velli_model_mashiny_1_34_39_kia_sorento/";
                    var productUrl = "https://www.toy.ru" + card.QuerySelectorAll("a")
                        .Single(m => m.GetAttribute("class") != null
                            && m.GetAttribute("class") == "d-block p-1 product-name gtm-click")
                        .GetAttribute("href");

                    var cardParsed = await ParsePage(productUrl);

                    if (cardParsed is null) continue;

                    var product = await Product.Get(cardParsed, productUrl);

                    Console.WriteLine($"Page {i}: ");
                    //Console.WriteLine(DateTime.Now.Millisecond);

                    var line = string.Join(',', new List<string>
                    {
                        product.Region,
                        product.Breadcrumbs,
                        product.Name,
                        product.CurrentPrice,
                        product.OldPrice,
                        product.IsInStock,
                        product.ImgUrls,
                        product.Url
                    });

                    csv.AppendLine(line);


                    await File.AppendAllTextAsync("products.csv", csv.ToString(), Encoding.UTF8);
                    csv.Clear();

                    await Task.WhenAll(tasks);
                };

            }

            //File.AppendAllText("products.csv", csv.ToString(), Encoding.UTF8);
            //File.WriteAllText("products.csv", csv.ToString(), Encoding.UTF8);
        }

        public static async Task<IHtmlDocument> ParsePage(string urlString)
        {
            //ошибка 403 появляется, потому что сайт не желает впускать "не pеальные бpаузеpы".
            //
            //WebClient web = new WebClient { Encoding = Encoding.UTF8 };
            //web.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.82 Safari/537.36";
            //web.Encoding = Encoding.UTF8;
            //string html = await web.DownloadStringTaskAsync(urlString);
            //Console.WriteLine(html);

            //var parser = new HtmlParser();
            //var doc = await parser.ParseDocumentAsync(html);


            var doc = default(IHtmlDocument);

            var client = new HttpClient();

            //SetCustomHeaders(urlString, client);


            //5 500
            //System.Threading.Thread.Sleep(100);

            using (var stream = await client.GetStreamAsync(urlString))
            {
                //to parse the HTML to AngleSharp.Parser.Html.HtmlParser object 
                var parser = new HtmlParser();

                doc = await parser.ParseDocumentAsync(stream);
            }

            return doc;
        }

        private static void SetCustomHeaders(string urlString, HttpClient client)
        {
            client.DefaultRequestHeaders.Clear();

            client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");

            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.82 Safari/537.36");

            client.DefaultRequestHeaders.Add("Referer", urlString);

            client.DefaultRequestHeaders.Add("Origin", "https://www.toy.ru");
            client.DefaultRequestHeaders.Add("sec-ch-ua", @""" Not A;Brand"";v=""99"", ""Chromium"";v=""99"", ""Google Chrome"";v=""99""");
            client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            client.DefaultRequestHeaders.Add("sec-ch-ua-platform", @"""Windows""");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            client.DefaultRequestHeaders.Add("sec-gpc", "1");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Host", "www.toy.ru");
        }
    }
}

