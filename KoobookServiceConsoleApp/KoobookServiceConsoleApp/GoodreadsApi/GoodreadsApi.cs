using Goodreads;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp.GoodReadsApi
{
    //Credit to https://github.com/adamkrogh/goodreads-dotnet for the API wrapper
    public class GoodreadsApi
    {
        public readonly IGoodreadsClient client; 
        public GoodreadsApi(string apiKey, string apiSecret)
        {
            client = GoodreadsClient.Create(apiKey, apiSecret);
        }

        public async Task Search(string isbn) {
            var book = await client.Books.GetByIsbn(isbn);
            var title = book.Title;
        }
    }
}
