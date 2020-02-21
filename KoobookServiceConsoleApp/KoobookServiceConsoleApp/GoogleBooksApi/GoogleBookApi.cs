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
        //The reason why this method was created in contrast to the above method is because the level of information per book when the android application requests for information about several books is more breif compared
        //to when the android application request for information about a single book.
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
        //This method works by interating through each book from the results(passed into the method's arguments) and creating a new GoogleBookModel instance where it populates its attributes(properties) with the book data
        //retrieved from the Google book api. If no exception were thrown while populating the attributes then it is added to the list of GoogleBookModels. If an exception is thrown then don't add to that list. After
        //iterating through each book, the method returns the list of GoogleBooksModel which may be empty. 
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

        //This method works by querying the Google book api to get book information using the isbn(passed into the method's arguments). It then checks to see if the query returned data. If it did then get the first book
        //from the results and return it. If the query returning null then it will return a GoogleBookModel with empty attributes. 
        public async Task<GoogleBookModel> CollectDataForBook(string isbn)
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

        //This method works by query the Google books api to get books based on the query string(passed into the method's argument). It then checks to see if the query returned any results. If it did then it will iterating through
        //each book from the results and add to the initialised list of GoogleBookModels. However, the query only returned one book then just simply add to the list and no need to do a for loop. After doing this, LINQ was used
        //to filtered the books where it did contain an ISBN number and this filtered list is returned by the method. However, if the query returned nothing then the method will return null.
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
            if (books != null)
            {
                //Get me all the books that actually has a valid book isbn 
                var booksWithIsbn = books.Where(b => !String.IsNullOrEmpty(b.IndustryIdentifiersDatas.Where(iid => iid.Type.Equals("ISBN_13")).Select(iid => iid.Identifier).SingleOrDefault())).ToList();
                return booksWithIsbn;
            }
            else {
                return books;
            }
        }

        //This method works by checking if the object(passed into the method's arguments) is null. If it is null then check what the data type(passed into the method's arguments) of the object. If it matches a certain data type
        //then that data type will be returned by the method. If it didn't match any of the data types then the method will return null. If the object is not null then the method will return that object. 
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

        //This was used to record the data type of a given object such that the HandleNull method can return the most apprproirate value if that object was null. 
        public enum DataType
        {
            String, Int, StringList, Double, IndustryIdentifiersDataList
        }

    }
}
