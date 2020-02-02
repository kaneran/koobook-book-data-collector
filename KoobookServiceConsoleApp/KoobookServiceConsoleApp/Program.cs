using KoobookServiceConsoleApp.Amazon;
using KoobookServiceConsoleApp.GoodReadsApi;
using KoobookServiceConsoleApp.GoogleBooksApi;
using KoobookServiceConsoleApp.TCP;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {

            TCPServer server = new TCPServer();
            var fileName = Assembly.GetExecutingAssembly().Location;
            server.Listen(fileName);

            //AmazonWebScraper amazonWebScraper = new AmazonWebScraper();
            //amazonWebScraper.CollectDataForBook("9781434227904", "Melinda Melton Crow");
  
        }
    }
}
