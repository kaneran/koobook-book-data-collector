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

        //If the isbn retrieved from the GoogleBookModel returns null then get the Goodreads api to get the book based on title as an attempt to find the book. If the isbn is not null then use the Goodreads api to find the
        //book based on Isbn. In either scenario, the output from Goodreads api will be returned by this method
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


        //This method will create a new instance of the GoodreadsModel and uses the book data(passed into the method's arguments) to assign some of the data to the attributes of the GoodreadsModel. After assigning the data to the model,
        //the method returns this.
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

        //Because the description returned from the api contains special characters such as "<i>" and "</b>
        //I created a method to remove those special characters and anything that's between the "<" and ">"
        //This method works by converting the description(passed into the method's arguments) to a char array. It then iterates through each character in the array. For each character, it checks whether it equals to the symbol "<".
        //if it does then get the index of the character ">". This index is assigned to the tracker in the for loop such that it ignore the characters "<" and ">" and the strings in between those tags.

        //If the character does not equal "<" append it to the stirng builder. 
        //After processing the entire char array, the output of the string builder is what is returned by this method
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

        //This returns the index of the ">" character
        //This index is then used to allow the for loop, in the CleanUpDescription method, to exclude the characters including and between "<" and ">"
        public int RemoveSpecialCharacters(char[] chars, int index) {
            var specialChar = chars[index];
            while (chars[index] != '>') {
                index++;
            } return index;
        }
    }
}
