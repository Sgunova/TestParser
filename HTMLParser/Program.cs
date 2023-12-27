using HtmlAgilityPack;
using HTMLParser;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using RestSharp;
using System.Diagnostics;

string url = "https://spb.hh.ru/vacancy/88925182?from=vacancy&hhtmFromLabel=similar_vacancies&hhtmFrom=vacancy";
var htmlPage = File.ReadAllText("C:\\Users\\feshu\\OneDrive\\Рабочий стол\\вакансия.html");

var fromFile = Parser.DataFromHTML(htmlPage, Parser.pageType.HH);
var fromUrl = Parser.DataFromUrl(url);

Console.ReadLine();


