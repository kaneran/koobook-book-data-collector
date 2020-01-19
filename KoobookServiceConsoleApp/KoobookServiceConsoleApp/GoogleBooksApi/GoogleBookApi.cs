using Google.Apis.Books.v1;
using Google.Apis.Services;
using KoobookServiceConsoleApp.GoolgeBooksApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp.GoogleBooksApi
{
    //Credit to Meysam N for the book api implementation , link- https://www.c-sharpcorner.com/code/3301/google-books-search-with-c-sharp.aspx
    class GoogleBookApi
    {
        public GoogleBookModel googleBookModel;
        public readonly BooksService _bookService;
            public GoogleBookApi(string apiKey)
        {
            _bookService = new BooksService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = this.GetType().ToString()
            });
        }

        //This method will query the google books api using the query that was passed
        //in as an arugment and return a tuple of books and its total volumes found count
        public Tuple<int?, List<GoogleBookModel>> Search(string query, int offset, int count) {
            try
            {
                var listQuery = _bookService.Volumes.List(query);
                listQuery.MaxResults = count;
                listQuery.StartIndex = offset;
                var result = listQuery.Execute();
                var books = result.Items.Select(book => new GoogleBookModel()
                {
                    Title = (string)HandleNull(book.VolumeInfo.Title, DataType.String),
                    Description = (string)HandleNull(book.VolumeInfo.Description, DataType.String),
                    Subtitle = (string)HandleNull(book.VolumeInfo.Subtitle, DataType.String),
                    Genres = (List<string>)HandleNull(book.VolumeInfo.Categories, DataType.StringList),
                    AverageRating = (double)HandleNull(book.VolumeInfo.AverageRating, DataType.Double),
                    Authors = (List<string>)HandleNull(book.VolumeInfo.Authors, DataType.StringList),
                    ThumbnailUrl = (string)HandleNull(book.VolumeInfo.ImageLinks.SmallThumbnail, DataType.String),
                    PageCount = (int)HandleNull(book.VolumeInfo.PageCount, DataType.Int)
                }).ToList();
                return new Tuple<int?, List<GoogleBookModel>>(result.TotalItems, books);
            }
            catch (Exception e) {
                return null;
            }
        }

        public GoogleBookModel CollectDataForBook(string isbn) {
            var bookResults = Search(isbn, 0, 1);
            if (bookResults != null)
            {
                googleBookModel = bookResults.Item2.First();
            }
            else {
                googleBookModel = new GoogleBookModel() {
                    Title = "",
                    Description= "",
                    Subtitle= "",
                    AverageRating = 0,
                    Authors = new List<string>(),
                    Genres = new List<string>(),
                    ThumbnailUrl = "",
                    PageCount = 0
                };
            }
            
            return googleBookModel;
        }

        public Object HandleNull(Object obj, DataType dataType)
        {
            if (obj == null)
            {
                if (dataType.Equals(DataType.Int))
                    return 0;
                else if (dataType.Equals(DataType.String))
                    return "";
                else if (dataType.Equals(DataType.Double))
                    return (double)0;

                else
                    return new List<string>();

            } 
            else
            {
                return obj;
            }
        }

        public enum DataType {
            String, Int, StringList, Double
        }

    }
}
