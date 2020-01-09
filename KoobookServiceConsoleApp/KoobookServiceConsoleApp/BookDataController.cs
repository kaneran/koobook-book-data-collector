using KoobookServiceConsoleApp.Amazon;
using KoobookServiceConsoleApp.GoogleBooksApi;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp
{
    class BookDataController
    {
        public BookModel bookModel;
        public async Task CollectDataFromSources(string isbn) {

            List<Task> tasks = new List<Task>();
            AmazonWebScraper amazonWebScraper = new AmazonWebScraper();
            tasks.Add(Task.Run(() => amazonWebScraper.CollectDataForBook(isbn)));

            var googleBooksApiKey = ConfigurationManager.AppSettings.Get("googleBooksApiKey");
            GoogleBookApi googleBookApi = new GoogleBookApi(googleBooksApiKey);
            tasks.Add(Task.Run(() => googleBookApi.CollectDataForBook(isbn)));

            var goodreadsApiKey = ConfigurationManager.AppSettings.Get("goodreadsApiKey");
            var goodreadsApiSecret = ConfigurationManager.AppSettings.Get("goodreadsApiSecret");
            GoodreadsApi.GoodreadsApi goodreadsApi = new GoodreadsApi.GoodreadsApi(goodreadsApiKey, goodreadsApiSecret);
            await goodreadsApi.Search(isbn);
            var goodreadsBookData = goodreadsApi.goodreadsModel;
            Task.WaitAll(tasks.ToArray());



            var amazonBookData = amazonWebScraper.amazonModel;
            var googleBooksBookData = googleBookApi.googleBookModel;

        }
    }
}
