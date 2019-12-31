using KoobookServiceConsoleApp.GoodReadsApi;
using KoobookServiceConsoleApp.GoogleBooksApi;
using KoobookServiceConsoleApp.TCP;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var apiKey = ConfigurationManager.AppSettings.Get("googleBooksApiKey");
            //GoogleBookApi googleBookApi = new GoogleBookApi(apiKey);
            //var books = googleBookApi.CollectDataForBook("9780007354771");
            //TCPServer server = new TCPServer();
            //server.Listen();
            var apiKey = ConfigurationManager.AppSettings.Get("goodreadsApiKey");
           var apiSecret = ConfigurationManager.AppSettings.Get("goodreadsApiSecret");
            GoodreadsApi goodreadsApi = new GoodreadsApi(apiKey, apiSecret);
            await goodreadsApi.Search("9780007354771");
        }
    }
}
