using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learning_Web_Page_Parsing
{
    class Product
    {
        public string Region { get; set; } = "Region: No Data";
        public string Breadcrumbs { get; set; } = "Breadcrumbs: No Data";
        public string Name { get; set; } = "Name: No Data";
        public string CurrentPrice { get; set; } = "CurrentPrice: No Data";
        public string OldPrice { get; set; } = "OldPrice: No Data";
        public string IsInStock { get; set; } = "IsInStock: No Data";
        public string ImgUrls { get; set; } = "ImgUrl: No Data";
        public string Url { get; set; } = "Url: No Data";

        public async static Task<Product> Get(IHtmlDocument doc, string productUrl)
        {
            Product product = new();

            var region = doc.QuerySelector(@"a[data-src=""#region""]");

            if (region is not null)
                product.Region = region
                        .TextContent
                        .Trim('\t', ' ', '\n');

            var breadcrumbs = doc.QuerySelectorAll("[class=breadcrumb] span")
                .Select(e => e.TextContent)
                .Select(e => e.Trim('\n'))
                .Distinct()
                .SkipLast(1)
                ;

            product.Breadcrumbs = "\"" + string.Join('/', breadcrumbs) + "\"";

            product.Name = "\"" + product.Breadcrumbs.Split('/').Last().Trim();

            if (doc.QuerySelector(@"[class=""price""]") is var price && price is not null)
                product.CurrentPrice = price.TextContent;

            var possibleOldPrices = doc.QuerySelectorAll("span")
                .Where(e => e.GetAttribute("class") is not null && e.GetAttribute("class") == "old-price")
                .FirstOrDefault();
            if (possibleOldPrices is not null)
                product.OldPrice = possibleOldPrices.TextContent;

            if (doc.QuerySelector(@"[class =""ok""]") is var inStock && inStock is not null)
                product.IsInStock = inStock.TextContent.Trim();
            else if (doc.QuerySelector(@"[class =""net-v-nalichii""]") is var notInStock && notInStock is not null)
                product.IsInStock = notInStock.TextContent.Trim();

            var imgElements =
                doc.QuerySelector(@"[class=""card-slider-for""]")
                .Children
                .SelectMany(e => e.Children);

            List<string> imgLinks = new();
            foreach (var e in imgElements)
            {
                var link = e.GetAttribute("href");
                imgLinks.Add(link);
            }

            product.ImgUrls = "\"" + string.Join("\n", imgLinks) + '\"';

            product.Url = productUrl;

            return product;
        }

        public override string ToString()
        {
            return "{" +
                    "region: " + Region +
                    ", breadcrumbs: " + Breadcrumbs +
                    ", product name: " + Name +
                    ", price: " + CurrentPrice +
                    ", old price: " + OldPrice +
                    ", is in stock: " + IsInStock +
                    ", img url: " + ImgUrls +
                    ", product url: " + Url +
                     "}";
        }
    }
}
