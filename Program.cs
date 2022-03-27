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
            var baseUrlString = "https://www.toy.ru/catalog/boy_transport/";
            var additionUrlString = "?filterseccode%5B0%5D=transport&PAGEN_8=";
            var logPath = "products.csv";

            if (string.IsNullOrWhiteSpace(File.ReadAllText(logPath)) )
                File.AppendAllText(logPath, "Название региона, Хлебные крошки, Название товара, Цена, Цена старая, " +
                    "Раздел с наличием, Ссылки на картинки, Ссылка на товар\n", Encoding.UTF8);

            for (int i = 0; i < 11; i++)
            {
                string urlString = baseUrlString;
                if (i > 1)
                    urlString = baseUrlString + additionUrlString + i.ToString();

                IHtmlDocument doc = await ParsePage(urlString);

                if (doc is null) continue;

                var cards = doc.GetElementsByClassName("h-100 product-card");

                await foreach (var product in GenerateSequence(cards))
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

        public static async Task<IHtmlDocument> ParsePage(string urlString)
        {
            var doc = default(IHtmlDocument);
            var client = new HttpClient();

            using (var stream = await client.GetStreamAsync(urlString))
            {
                var parser = new HtmlParser();
                doc = await parser.ParseDocumentAsync(stream);
            }

            return doc;
        }

        public static async IAsyncEnumerable<Product> GenerateSequence(IHtmlCollection<IElement> cards)
        {
            foreach (var card in cards)
            {
                var productUrl = "https://www.toy.ru" + card.QuerySelectorAll("a")
                    .Single(m => m.GetAttribute("class") != null
                        && m.GetAttribute("class") == "d-block p-1 product-name gtm-click")
                    .GetAttribute("href");

                var cardParsed = await ParsePage(productUrl);

                if (cardParsed is null) continue;

                var product = await Product.Get(cardParsed, productUrl);

                yield return product;
            };
        }
    }
}

