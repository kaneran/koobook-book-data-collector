using Google.Apis.Books.v1;
using Google.Apis.Books.v1.Data;
using Google.Apis.Services;
using KoobookServiceConsoleApp.GoolgeBooksApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Google.Apis.Books.v1.Data.Volume.VolumeInfoData;

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
        public Tuple<int?, List<GoogleBookModel>> SearchBook(string query, int offset, int count)
        {
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
                    PageCount = (int)HandleNull(book.VolumeInfo.PageCount, DataType.Int),
                    IndustryIdentifiersDatas = (List<IndustryIdentifiersData>)HandleNull(book.VolumeInfo.IndustryIdentifiers, DataType.IndustryIdentifiersDataList)
                }).ToList();
                return new Tuple<int?, List<GoogleBookModel>>(result.TotalItems, books);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        //This method will query the google books api using the query that was passed
        //in as an arugment and return a tuple of books and its total volumes found count
        public Tuple<int?, List<GoogleBookModel>> SearchBooks(string query, int offset, int count)
        {
            try
            {
                var listQuery = _bookService.Volumes.List(query);
                listQuery.MaxResults = count;
                listQuery.StartIndex = offset;
                var result = listQuery.Execute();
                var bookList = GetBookListFromResults(result);
                return new Tuple<int?, List<GoogleBookModel>>(result.TotalItems, bookList);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        //This method will filter out the book results such that only the books where all the needed fields are non-null are returned such that it can be used to create a book list without encoutering any errors
        private List<GoogleBookModel> GetBookListFromResults(Volumes results)
        {
            List<GoogleBookModel> books = new List<GoogleBookModel>();
            foreach (var book in results.Items) {
                GoogleBookModel googleBookModel = new GoogleBookModel();
                try
                {
                    googleBookModel.Title = (string)HandleNull(book.VolumeInfo.Title, DataType.String);
                    googleBookModel.Authors = (List<string>)HandleNull(book.VolumeInfo.Authors, DataType.StringList);
                    googleBookModel.ThumbnailUrl = (string)HandleNull(book.VolumeInfo.ImageLinks.SmallThumbnail, DataType.String);
                    googleBookModel.IndustryIdentifiersDatas = (List<IndustryIdentifiersData>)HandleNull(book.VolumeInfo.IndustryIdentifiers, DataType.IndustryIdentifiersDataList);
                    books.Add(googleBookModel);
                }
                catch (Exception e) {
                    //Don't add that book to the list cause on the data attributes were null
                }
            }

            return books;
        }

        public GoogleBookModel CollectDataForBook(string isbn)
        {
            var bookResults = SearchBook(isbn, 0, 1);
            if (bookResults != null)
            {
                googleBookModel = bookResults.Item2.First();
            }
            else
            {
                googleBookModel = new GoogleBookModel()
                {
                    Title = "",
                    Description = "",
                    Subtitle = "",
                    AverageRating = 0,
                    Authors = new List<string>(),
                    Genres = new List<string>(),
                    ThumbnailUrl = "",
                    PageCount = 0,
                    IndustryIdentifiersDatas = new List<IndustryIdentifiersData>()
                };
            }

            return googleBookModel;
        }

        public List<GoogleBookModel> CollectDataForBooks(string query)
        {

            List<GoogleBookModel> books = new List<GoogleBookModel>();
            var bookResults = SearchBooks(query, 1, 12);

            if (bookResults != null)
            {
                //If the books results only returns one book then execute the SearchBook method

                if (bookResults.Item2.Count > 1)
                {

                    foreach (var book in bookResults.Item2)
                    {
                        books.Add(book);
                    }
                }
                else if (bookResults.Item2.Count == 1)
                {
                    var bookSearchResult = SearchBook(query, 1, 1);
                    var book = bookSearchResult.Item2.First();
                    books.Add(book);
                }
            }
            else
            {
                books = null;
            }
            //Get me all the books that actually has a valid book isbn 
            var booksWithIsbn = books.Where(b => !String.IsNullOrEmpty(b.IndustryIdentifiersDatas.Where(iid => iid.Type.Equals("ISBN_13")).Select(iid => iid.Identifier).SingleOrDefault())).ToList();
            return booksWithIsbn;
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

                else if (dataType.Equals(DataType.StringList))
                    return new List<string>();

                else if (dataType.Equals(DataType.IndustryIdentifiersDataList))
                    return new List<IndustryIdentifiersData>();

                else
                {
                    return null;
                }
            }
            else
            {
                return obj;
            }
        }

        public enum DataType
        {
            String, Int, StringList, Double, IndustryIdentifiersDataList
        }

    }
}
