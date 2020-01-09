using KoobookServiceConsoleApp.Amazon;
using KoobookServiceConsoleApp.GoodreadsApi;
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
           
            TCPServer server = new TCPServer();
            server.Listen();
            
        }
    }
}
