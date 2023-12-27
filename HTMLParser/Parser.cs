using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using RestSharp;
using System.Diagnostics;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace HTMLParser
{
    public static class Parser
    {
        //Время работы вебдрайвера
        static int SeleniumTimer = 10;

        //Перечесление для обработки определенных сайтов
        public enum pageType { HH }

        //Перечень для определения возвразаймого значения при работе с XPath
        enum XPathType { Id, SingleNode, Nodes };

        //Класс, для хранения правил парсинга
        record XPathOptions(string Domain, XPathType Type, string Value);

        /// <summary>
        ///     Правила для парсинга страниц
        /// </summary>
        static List<XPathOptions> domainsSettings = new () {
            new XPathOptions("career.habr.com", XPathType.SingleNode, "//div[@class='description']"),
            new XPathOptions("career.habr.com", XPathType.Nodes, "//div[@class='contact']"),
            new XPathOptions("career.habr.com", XPathType.Nodes, "//div[@class='links']"),
            new XPathOptions("career.habr.com", XPathType.Id, "company_sidebar_buttons"),
            new XPathOptions("career.habr.com", XPathType.Id, "company_sidebar_buttons"),
            new XPathOptions("spb.hh.ru", XPathType.SingleNode, "//script[@type='application/ld+json']"),
            new XPathOptions("hh.ru", XPathType.SingleNode, "//script[@type='application/ld+json']"),
        };

        /// <summary>
        ///     Функция для считывания html по ссылке
        /// </summary>
        /// <param name="url">Ссылка на сайт</param>
        /// <returns>Возвращает html страницу</returns>
        private static (bool resultOfReading, string htmlPage) HTMLReader(string url)
        {
            string htmlPage = string.Empty;

            try
            {
                var client = new RestClient(url);
                var request = new RestRequest(string.Empty, Method.Get);
                RestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    htmlPage = response.Content??string.Empty;
                }
                else
                {
                    ChromeOptions options = new ChromeOptions();
                    options.AddArgument("--headless");

                    using (IWebDriver driver = new ChromeDriver(options))
                    {
                        driver.Navigate().GoToUrl(url);

                        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(SeleniumTimer));
                        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

                        // Подключение скриптов
                        // ((IJavaScriptExecutor)driver).ExecuteScript("...");

                        htmlPage = driver.PageSource;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return (false, htmlPage);
            }

            return (true, htmlPage);
        }

        /// <summary>
        ///     Функция для поиска для парсинга сайта
        /// </summary>
        /// <param name="url">Ссылка на сайт</param>
        /// <returns>Возвращает перечень ключ-значения</returns>
        public static List<(string key, List<string> values)> DataFromUrl(string url)
        {
            var reader = HTMLReader(url);

            var settings = domainsSettings.Where(x => x.Domain == (new Uri(url)).Host);

            if (reader.resultOfReading)
                return XPathParse(reader.htmlPage, settings);
            return new();
        }

        /// <summary>
        ///     Функция для парсинга страницы
        /// </summary>
        /// <param name="htmlPage">html документ</param>
        /// <param name="type">Тип, необходим для маппинга данных с определенных сайтов</param>
        /// <returns>Возвращает перечень ключ-значения</returns>
        public static List<(string key, List<string> values)> DataFromHTML(string htmlPage, pageType type)
        {
            List<XPathOptions> settings = type switch
            {
                pageType.HH => domainsSettings.Where(x => x.Domain == "hh.ru").ToList(),
                _ => new()
            };

            var resultOfParse = XPathParse(htmlPage, settings);

            try
            {
                switch (type)
                {
                    case pageType.HH:
                        {
                            var keys = new List<string>() { "title", "description", "datePosted" };

                            if (resultOfParse is not null && resultOfParse.Count > 0 && resultOfParse[0].values is not null && resultOfParse[0].values.Count > 0)
                            {
                                JObject o = JObject.Parse(resultOfParse[0].values[0]);

                                return keys.Select(x => (x, new List<string>() { (string)o.SelectToken(x)??"Не найдено" }))
                                    .ToList();
                            }
                            return resultOfParse;
                        }
                    default: return resultOfParse;
                }
            }
            catch
            {
                return resultOfParse;
            }
        }

        /// <summary>
        ///     Функция для обработки html страницы средствами HtmlAgilityPack и XPath
        /// </summary>
        /// <param name="htmlPage">html страницы</param>
        /// <param name="settings">Правила для парсинга XPath</param>
        /// <returns>Возвращает перечень ключ-значения</returns>
        private static List<(string key, List<string> values)> XPathParse(string htmlPage, IEnumerable<XPathOptions> settings)
        {
            List<(string key, List<string> values)> parse = new();

            try
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlPage);

                foreach (var selector in settings)
                {
                    switch (selector.Type)
                    {
                        case XPathType.Id:
                            {
                                var element = doc.GetElementbyId(selector.Value);

                                if (element != null)
                                    parse.Add((selector.Value, new() { element.InnerText }));

                                break;
                            }
                        case XPathType.Nodes:
                            {
                                var elements = doc.DocumentNode.SelectNodes(selector.Value);

                                if (elements != null)
                                    parse.Add((selector.Value, elements.Select(x => x.InnerText).ToList()));

                                break;
                            }
                        case XPathType.SingleNode:
                            {
                                var element = doc.DocumentNode.SelectSingleNode(selector.Value);

                                if (element != null)
                                    parse.Add((selector.Value, new() { element.InnerText }));

                                break;
                            }
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return parse;
        }

    }
}
