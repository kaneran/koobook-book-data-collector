using Goodreads;
using Goodreads.Models.Response;
using KoobookServiceConsoleApp.GoodReadsApi;
using System;
using System.Text;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp.GoodReadsApi
{
    //Credit to https://github.com/adamkrogh/goodreads-dotnet for the API wrapper
    public class GoodreadsApi
    {
        public GoodreadsModel goodreadsModel { get; set; }
        public Book book { get; set; }
        public readonly IGoodreadsClient client; 
        public GoodreadsApi(string apiKey, string apiSecret)
        {
            client = GoodreadsClient.Create(apiKey, apiSecret);
        }

        //If the isbn retrieved from the GoogleBookModel reutrns null then get the Goodreads api to get the book based on title as an attempt to find the book.
        public async Task<Book> SearchByIsbn(string data) {
            BookDataController bookDataController = new BookDataController();
            if (!String.IsNullOrEmpty(data))
            {
                var book = await client.Books.GetByIsbn(data);
                return book;
            }
            else {
                var book = await client.Books.GetByTitle(data);
                return book;
            }
            
        }



        public GoodreadsModel CollectDataForBook(Book book) {
            goodreadsModel = new GoodreadsModel() {
                Title = book.Title,
                Isbn = book.Isbn13,
                Description = CleanUpDescription(book.Description),
                AverageRating = (double)book.AverageRating,
                Authors = book.Authors,
                PageCount = book.Pages
            };
            return goodreadsModel;
        }

        //Because the description return from the api contains special characters such as "<i>" and "</b>
        //I created a method to remove those special characters and anything that's between the "<" and ">"
        public string CleanUpDescription(string description) {
            int newIndex = 0;
            StringBuilder stringBuilder = new StringBuilder();
            var chars = description.ToCharArray();
            for (int i = 0; i < chars.Length; i++) {
                if (chars[i] != '<')
                {
                    stringBuilder.Append(chars[i]);
                }

                else {
                    newIndex = RemoveSpecialCharacters(chars, i);
                    i = newIndex;
                }
                    
            } return stringBuilder.ToString();

        }

        //This returns the index of the character before the ">" character
        //This index is then used to allow the for loop, in the CleanUpDescription method, to exclude the characters including and between "<" and ">"
        public int RemoveSpecialCharacters(char[] chars, int index) {
            var specialChar = chars[index];
            while (chars[index] != '>') {
                index++;
            } return index;
        }
    }
}
