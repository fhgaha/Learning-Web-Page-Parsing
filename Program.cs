using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Learning_Web_Page_Parsing
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var baseUrl = "https://www.toy.ru/";
            var categoryUrl = "catalog/boy_transport/";
            var additionUrl = "?filterseccode%5B0%5D=transport&PAGEN_8=";
            var logPath = "products.csv";
            //var requiredCity = "Санкт-Петербург";
            var requiredCity = "Ростов-на-Дону";

            if (string.IsNullOrWhiteSpace(File.ReadAllText(logPath)))
                File.AppendAllText(logPath, "Название региона, Хлебные крошки, Название товара, Цена, Цена старая, " +
                    "Раздел с наличием, Ссылки на картинки, Ссылка на товар\n", Encoding.UTF8);

            IHtmlDocument doc_ = await ParsePage(baseUrl + categoryUrl, requiredCity);

            int temp;
            var pageAmount = doc_.QuerySelectorAll(@"a.page-link")
                .Where(e => e.GetAttribute("href") != null)
                .Where(e => e.GetAttribute("href").Contains("boy_transport"))
                .Where(e => int.TryParse(e.TextContent, out temp))
                .Select(e => int.Parse(e.TextContent))
                .OrderBy(e => e)
                .Last();

            for (int i = 1; i <= pageAmount; i++)
            {
                string urlString = baseUrl + categoryUrl;
                if (i > 1) urlString = baseUrl + categoryUrl + additionUrl + i.ToString();

                IHtmlDocument doc = await ParsePage(urlString, requiredCity);

                if (doc is null) break;

                var cards = doc.GetElementsByClassName("h-100 product-card");

                await foreach (var product in GenerateSequence(cards, requiredCity))
                {
                    Console.WriteLine($"Page {i}: " + product.Name);

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

                    await File.AppendAllTextAsync(logPath, line + '\n', Encoding.UTF8);
                }
            }
        }



        public static async Task<IHtmlDocument> ParsePage(string urlString, string requiredCity)
        {
            Dictionary<string, string> cityIdMap = new()
            {
                ["Санкт-Петербург"] = "78000000000",
                ["Ростов-на-Дону"] = "61000001000"
            };

            var doc = default(IHtmlDocument);
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Cookie", "BITRIX_SM_city=" + cityIdMap[requiredCity]);

            using (var stream = await client.GetStreamAsync(urlString))
            {
                var parser = new HtmlParser();
                doc = await parser.ParseDocumentAsync(stream);
            }

            return doc;
        }

        public static async IAsyncEnumerable<Product> GenerateSequence(IHtmlCollection<IElement> cards, string requiredCity)
        {
            foreach (var card in cards)
            {
                var productUrl = "https://www.toy.ru" + card.QuerySelectorAll("a")
                    .Single(m => m.GetAttribute("class") != null
                        && m.GetAttribute("class") == "d-block p-1 product-name gtm-click")
                    .GetAttribute("href");

                var cardParsed = await ParsePage(productUrl, requiredCity);

                if (cardParsed is null) continue;

                var product = await Product.Get(cardParsed, productUrl);

                yield return product;
            };
        }

        #region sitemap doesn't contain "catalog/boy_transport/" or "boy_transport"
        private static List<string> GetLinksFromSitemap(string baseUrl, string requiredSubString, string requiredCity)
        {
            Console.WriteLine("Collecting links from sitemap...");

            var doc = ParsePage(baseUrl + "/sitemap.xml", requiredCity).Result;
            var currentLinks = doc.QuerySelectorAll("loc")
                .Select(e => e.TextContent);

            List<string> linksWithRequiredString = new();

            foreach (var link in currentLinks)
            {
                if (link.Contains(requiredSubString))
                    linksWithRequiredString.Add(link);
            }

            linksWithRequiredString.AddRange(FilterDecendantsLinks(currentLinks, requiredSubString, requiredCity));

            Console.WriteLine("Links collected");
            return linksWithRequiredString;
        }

        private static List<string> FilterDecendantsLinks(IEnumerable<string> baseLinks, string requiredSubString, string requiredCity)
        {
            List<string> links = new();

            foreach (var link in baseLinks)
            {
                if (link.Contains(requiredSubString))
                    links.Add(link);

                var doc = ParsePage(link, requiredCity).Result;
                var currentLinks = doc.QuerySelectorAll("loc")
                    .Select(e => e.TextContent);

                if (currentLinks.Count() == 0) return links;

                links.AddRange(FilterDecendantsLinks(currentLinks, requiredSubString, requiredCity));
            }

            return links;
        }
        #endregion
    }
}

