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
            var listQuery = _bookService.Volumes.List(query);
            listQuery.MaxResults = count;
            listQuery.StartIndex = offset;
            var result = listQuery.Execute();
            var books = result.Items.Select(book => new GoogleBookModel()
            {
                Title = book.VolumeInfo.Title,
                Description = book.VolumeInfo.Description,
                Subtitle = book.VolumeInfo.Subtitle,
                Genres = book.VolumeInfo.Categories.ToList(),
                AverageRating = book.VolumeInfo.AverageRating,
                Authors = book.VolumeInfo.Authors.ToList(),
                ThumbnailUrl = book.VolumeInfo.ImageLinks.SmallThumbnail,
                PageCount = book.VolumeInfo.PageCount
            }).ToList();
            return new Tuple<int?, List<GoogleBookModel>>(result.TotalItems, books);
        }

        public GoogleBookModel CollectDataForBook(string isbn) {
            var bookResults = Search(isbn, 0, 1);
            googleBookModel = bookResults.Item2.First();
            return googleBookModel;
        }

    }
}
