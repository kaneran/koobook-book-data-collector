using Goodreads.Models.Response;
using KoobookServiceConsoleApp.Amazon;
using KoobookServiceConsoleApp.GoodReadsApi;
using KoobookServiceConsoleApp.GoogleBooksApi;
using KoobookServiceConsoleApp.GoolgeBooksApi;
using KoobookServiceConsoleApp.TCP;
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
        public string MyProperty { get; set; }
        public BookModel bookModel;
        GoogleBookModel googleBookModel;
        string googleBooksApiKey;
        GoogleBookApi googleBookApi;
        string goodreadsApiKey;
        string goodreadsApiSecret;
        GoodreadsApi goodreadsApi;
        TCPServer server;
        Task<Book> task;

        public BookDataController()
        {
            googleBooksApiKey = ConfigurationManager.AppSettings.Get("googleBooksApiKey");
            googleBookApi = new GoogleBookApi(googleBooksApiKey);
            goodreadsApiKey = ConfigurationManager.AppSettings.Get("goodreadsApiKey");
            goodreadsApiSecret = ConfigurationManager.AppSettings.Get("goodreadsApiSecret");
            goodreadsApi = new GoodReadsApi.GoodreadsApi(goodreadsApiKey, goodreadsApiSecret);
        }
        public async void CollectDataFromSources(string data)
        {
            bookModel = new BookModel();
            server = new TCPServer();


            //If the data doesnt contain the isbn then the isbn will be initially retrieved
            //from the google books api which is then passed into the good reads api solution
            var dataContainsIsbn = data.All(char.IsDigit);

            if (dataContainsIsbn.Equals(false))
            {
                googleBookModel = googleBookApi.CollectDataForBook(data);
                var isbn = GetIsbn(googleBookModel);
                task = goodreadsApi.SearchByIsbn(isbn);
            }
            else
            {
                googleBookModel = googleBookApi.CollectDataForBook(data);
                task = goodreadsApi.SearchByIsbn(data);
            }

            task.Wait();
            var book = task.Result;
            var goodreadsModel = goodreadsApi.CollectDataForBook(book);

            SetDescription(goodreadsModel, googleBookModel);

            AmazonWebScraper amazonWebScraper = new AmazonWebScraper();
            AmazonModel amazonBookData = null;

            if (!String.IsNullOrEmpty(goodreadsModel.Authors.First().ToString()))
            {
                amazonBookData = amazonWebScraper.CollectDataForBook(data, goodreadsModel.Authors.First().Name);
            }
            else if (!String.IsNullOrEmpty(googleBookModel.Authors.First()))
            {
                amazonBookData = amazonWebScraper.CollectDataForBook(data, googleBookModel.Authors.First());
            }


            SetBookTitle(goodreadsModel, googleBookModel);
            SetAuthorsOfBook(goodreadsModel, googleBookModel);

            SetAverageRatings(goodreadsModel, googleBookModel, amazonBookData);
            SetPageCount(goodreadsModel, googleBookModel);
            SetAmazonRatings(amazonBookData);
            SetAmazonReviews(amazonBookData);
            SetReviewCount(amazonBookData);
            SetGenres(googleBookModel);
            SetSubtitle(googleBookModel);
            SetIsbn(goodreadsModel, googleBookModel, data);
            SetThumbnailUrl(googleBookModel);
        }

        public string ConcatBooksData(List<GoogleBookModel> books)
        {
            //Loop through each author and add it to the string builder and is split by the "#" symbol
            String authorsString = "";
            StringBuilder sb = new StringBuilder();



            foreach (var book in books)
            {


                Append(sb, book.Title, "$");
                string isbn = GetIsbn(book);
                Append(sb, isbn, "$");

                if (book.Authors.Count > 1)
                {
                    for (int i = 0; i < book.Authors.Count; i++)
                    {
                        Append(sb, book.Authors[i], "#");

                    }
                    sb.Append("$");

                }
                else
                {
                    Append(sb, book.Authors.First(), "$");
                }

                Append(sb, book.ThumbnailUrl, "|");
            }

            return sb.ToString();
        }

        private static string GetIsbn(GoogleBookModel book)
        {
            return book.IndustryIdentifiersDatas.Where(iid => iid.Type.Equals("ISBN_13")).Select(d => d.Identifier).SingleOrDefault();
        }

        //Each data attribute is separated by "$"
        public string ConcatBookData()
        {
            try
            {

                StringBuilder sb = new StringBuilder();
                Append(sb, bookModel.Title, "$");
                if (bookModel.Authors.Count > 1)
                {
                    for (int i = 0; i < bookModel.Authors.Count; i++)
                    {
                        Append(sb, bookModel.Authors[i], "#");

                    }
                    sb.Append("$");

                }
                else
                {
                    Append(sb, bookModel.Authors.First(), "$");
                }

                Append(sb, bookModel.Description, "$");
                Append(sb, bookModel.Subtitle, "$");
                Append(sb, bookModel.Isbn, "$");
                Append(sb, bookModel.GoogleBooksAverageRating.ToString(), "$");
                Append(sb, bookModel.GoodreadsAverageRating.ToString(), "$");
                Append(sb, bookModel.AmazonAverageRating.ToString(), "$");
                Append(sb, bookModel.AmazonFiveStarRatingPercentage.ToString(), "$");
                Append(sb, bookModel.AmazonFourStarRatingPercentage.ToString(), "$");
                Append(sb, bookModel.AmazonThreeStarRatingPercentage.ToString(), "$");
                Append(sb, bookModel.AmazonTwoStarRatingPercentage.ToString(), "$");
                Append(sb, bookModel.AmazonOneStarRatingPercentage.ToString(), "$");

                if (!(bookModel.AmazonReviews.Count == 0))
                {
                    if (bookModel.AmazonReviews.Count > 1)
                    {
                        for (int i = 0; i < bookModel.AmazonReviews.Count; i++)
                        {
                            Append(sb, bookModel.AmazonReviews[i], "#");

                        }
                        sb.Append("$");

                    }
                    else
                    {
                        Append(sb, bookModel.AmazonReviews.First(), "$");
                    }

                }
                else {
                    Append(sb, "*", "$");
                }

                Append(sb, bookModel.AmazonReviewsCount.ToString(), "$");

                if (bookModel.Genres.Count > 1)
                {
                    for (int i = 0; i < bookModel.Genres.Count; i++)
                    {
                        Append(sb, bookModel.Genres[i], "#");

                    }
                    sb.Append("$");

                }
                else if (bookModel.Genres.Count == 1)
                {
                    Append(sb, bookModel.Genres.First(), "$");
                }


                Append(sb, bookModel.PageCount.ToString(), "$");
                Append(sb, bookModel.ThumbnailUrl.ToString(), "$");
                return sb.ToString();
            }
            catch (Exception e)
            {
                var msg = e.Message;
                return null;
            }
        }

        //If the string is empty then write a "*". This makes it easier to know which strings were empty when the android application decodes it
        public void Append(StringBuilder sb, string s, string writeChar = "")
        {
            if (!String.IsNullOrEmpty(s))
            {
                sb.Append(s + writeChar);
            }
            else
            {
                sb.Append("*" + writeChar);
            }

        }

        public void SetThumbnailUrl(GoogleBookModel googleBooksBookData)
        {
            if (!String.IsNullOrEmpty(googleBooksBookData.ThumbnailUrl))
            {
                bookModel.ThumbnailUrl = googleBooksBookData.ThumbnailUrl;
            }
            else
            {
                bookModel.ThumbnailUrl = "";
            }
        }
        private void SetIsbn(GoodreadsModel goodreadsBookData, GoogleBookModel googleBookModel, string retrievedIsbn)
        {
            if (!String.IsNullOrEmpty(goodreadsBookData.Isbn))
            {
                bookModel.Isbn = goodreadsBookData.Isbn;
            }
            else if (!String.IsNullOrEmpty(GetIsbn(googleBookModel)))
            {

                bookModel.Isbn = GetIsbn(googleBookModel);
            }
            else
            {
                bookModel.Isbn = retrievedIsbn;
            }
        }

        private void SetSubtitle(GoogleBookModel googleBooksBookData)
        {
            if (!String.IsNullOrEmpty(googleBooksBookData.Subtitle))
            {
                bookModel.Subtitle = googleBooksBookData.Subtitle;
            }
        }

        private void SetGenres(GoogleBookModel googleBooksBookData)
        {
            bookModel.Genres = googleBooksBookData.Genres;

        }

        private void SetReviewCount(AmazonModel amazonBookData)
        {
            if (amazonBookData.ReviewCount != 0)
            {
                bookModel.AmazonReviewsCount = amazonBookData.ReviewCount;
            }
            else {
                bookModel.AmazonReviewsCount = 0;
            }
        }

        private void SetAmazonReviews(AmazonModel amazonBookData)
        {
            if (amazonBookData.Reviews.Count != 0)
            {
                bookModel.AmazonReviews = amazonBookData.Reviews;
            }
            else {
                bookModel.AmazonReviews = new List<string>();
            }
        }

        private void SetAmazonRatings(AmazonModel amazonBookData)
        {
            if (amazonBookData.AverageRating != 0)
            {
                bookModel.AmazonFiveStarRatingPercentage = amazonBookData.FiveStarRatingPercentage;
                bookModel.AmazonFourStarRatingPercentage = amazonBookData.FourStarRatingPercentage;
                bookModel.AmazonThreeStarRatingPercentage = amazonBookData.ThreeStarRatingPercentage;
                bookModel.AmazonTwoStarRatingPercentage = amazonBookData.TwoStarRatingPercentage;
                bookModel.AmazonOneStarRatingPercentage = amazonBookData.OneStarRatingPercentage;
                bookModel.AmazonAverageRating = amazonBookData.AverageRating;
            }
        }

        private void SetPageCount(GoodreadsModel goodreadsBookData, GoolgeBooksApi.GoogleBookModel googleBooksBookData)
        {
            if (goodreadsBookData.PageCount != null)
            {
                bookModel.PageCount = goodreadsBookData.PageCount;
            }
            else if (googleBooksBookData.PageCount != 0)
            {
                bookModel.PageCount = googleBooksBookData.PageCount;
            }

        }

        private void SetAverageRatings(GoodreadsModel goodreadsBookData, GoolgeBooksApi.GoogleBookModel googleBooksBookData, AmazonModel amazonBookData)
        {
            if (googleBooksBookData.AverageRating != 0)
            {
                bookModel.GoogleBooksAverageRating = googleBooksBookData.AverageRating;
            }

            if (goodreadsBookData.AverageRating != 0)
            {
                bookModel.GoodreadsAverageRating = goodreadsBookData.AverageRating;
            }

            if (amazonBookData.AverageRating != 0)
            {
                bookModel.GoodreadsAverageRating = amazonBookData.AverageRating;
            }
        }

        private void SetDescription(GoodreadsModel goodreadsBookData, GoolgeBooksApi.GoogleBookModel googleBooksBookData)
        {
            string summarisedBookDescription;
            if (goodreadsBookData.Description.Length > 0 && googleBooksBookData.Description.Length > 0)
            {
                if (goodreadsBookData.Description.Length > googleBooksBookData.Description.Length)
                {
                    summarisedBookDescription = SummariseDescription(goodreadsBookData.Description);
                    bookModel.Description = summarisedBookDescription;
                }
                else
                {
                    summarisedBookDescription = SummariseDescription(googleBooksBookData.Description);
                    bookModel.Description = summarisedBookDescription;
                }

            }
            else
            {
                if (!String.IsNullOrEmpty(goodreadsBookData.Description))
                {
                    summarisedBookDescription = SummariseDescription(goodreadsBookData.Description);
                    bookModel.Description = summarisedBookDescription;
                }

                else if (!String.IsNullOrEmpty(googleBooksBookData.Description))
                {
                    summarisedBookDescription = SummariseDescription(googleBooksBookData.Description);
                    bookModel.Description = summarisedBookDescription;
                }
            }
        }

        private void SetAuthorsOfBook(GoodreadsModel goodreadsBookData, GoolgeBooksApi.GoogleBookModel googleBooksBookData)
        {
            if (googleBooksBookData.Authors.Count != 0)
            {
                bookModel.Authors = googleBooksBookData.Authors;
            }
            else if (goodreadsBookData.Authors.Count != 0)
            {
                List<string> authors = new List<string>();
                foreach (var author in goodreadsBookData.Authors)
                {
                    authors.Add(author.Name);
                }
                bookModel.Authors = authors;
            }
        }

        private void SetBookTitle(GoodreadsModel goodreadsBookData, GoolgeBooksApi.GoogleBookModel googleBooksBookData)
        {
            if (!String.IsNullOrEmpty(goodreadsBookData.Title))
            {
                bookModel.Title = goodreadsBookData.Title;
            }

            else if (!String.IsNullOrEmpty(googleBooksBookData.Title))
            {
                bookModel.Title = googleBooksBookData.Title;
            }

        }

        //To provide a way for the server to know that the entire book description has been retrieved, the "#" will be used to mark the end of the string
        public string SummariseDescription(string bookDescription)
        {
            TCPClient client = new TCPClient();
            string summarisedBookDescription = "";
            summarisedBookDescription = client.Connect(bookDescription);
            while (String.IsNullOrEmpty(summarisedBookDescription))
            {
                //Do nothing
            }
            return summarisedBookDescription;
        }


    }
}
