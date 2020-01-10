using Goodreads.Models.Response;
using KoobookServiceConsoleApp.Amazon;
using KoobookServiceConsoleApp.GoodReadsApi;
using KoobookServiceConsoleApp.GoogleBooksApi;
using KoobookServiceConsoleApp.GoolgeBooksApi;
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
        public async void CollectDataFromSources(string isbn)
        {

            var goodreadsApiKey = ConfigurationManager.AppSettings.Get("goodreadsApiKey");
            var goodreadsApiSecret = ConfigurationManager.AppSettings.Get("goodreadsApiSecret");
            GoodreadsApi goodreadsApi = new GoodReadsApi.GoodreadsApi(goodreadsApiKey, goodreadsApiSecret);
            Task<Book> task = goodreadsApi.Search(isbn);
            task.Wait();
            var book = task.Result;
            var goodreadsBookData = goodreadsApi.CollectDataForBook(book);

            var googleBooksApiKey = ConfigurationManager.AppSettings.Get("googleBooksApiKey");
            GoogleBookApi googleBookApi = new GoogleBookApi(googleBooksApiKey);
            var googleBooksBookData = googleBookApi.CollectDataForBook(isbn);

            AmazonWebScraper amazonWebScraper = new AmazonWebScraper();
            AmazonModel amazonBookData = null;

            if (!String.IsNullOrEmpty(goodreadsBookData.Authors.First().ToString()))
            {
                amazonBookData = amazonWebScraper.CollectDataForBook(isbn, goodreadsBookData.Authors.First().Name);
            }
            else if (!String.IsNullOrEmpty(googleBooksBookData.Authors.First()))
            {
                amazonBookData = amazonWebScraper.CollectDataForBook(isbn, googleBooksBookData.Authors.First());
            }

            bookModel = new BookModel();
            SetBookTitle(goodreadsBookData, googleBooksBookData);
            SetAuthorsOfBook(goodreadsBookData, googleBooksBookData);
            SetDescription(goodreadsBookData, googleBooksBookData);
            SetAverageRatings(goodreadsBookData, googleBooksBookData, amazonBookData);
            SetPageCount(goodreadsBookData, googleBooksBookData);
            SetAmazonRatings(amazonBookData);
            SetAmazonReviews(amazonBookData);
            SetReviewCount(amazonBookData);
            SetGenres(googleBooksBookData);
            SetSubtitle(googleBooksBookData);
            SetIsbn(goodreadsBookData);
        }
        //Each data attribute is separte by "$"
        public string ConcatBookData() {

            StringBuilder sb = new StringBuilder();
            Append(sb,bookModel.Title , "$");
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

            Append(sb, bookModel.Description , "$");
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

            Append(sb, bookModel.AmazonReviewsCount.ToString(), "$");

            if (bookModel.Genres.Count > 1)
            {
                for (int i = 0; i < bookModel.Genres.Count; i++)
                {
                    Append(sb, bookModel.Genres[i], "#");

                }
                sb.Append("$");

            }
            else
            {
                Append(sb, bookModel.Genres.First(), "$");
            }

            Append(sb, bookModel.PageCount.ToString(), "$");

            return sb.ToString();
        }

        //If the string is empty then write a "*". This makes it easier to know which strings were empty when the android application decodes it
        public void Append(StringBuilder sb, string s, string writeChar="") {
            if (s != null)
            {
                sb.Append(s + writeChar);
            }
            else {
                sb.Append("*" + writeChar);
            }

        }
        private void SetIsbn(GoodreadsModel goodreadsBookData)
        {
            if (goodreadsBookData.Isbn != null)
            {
                bookModel.Isbn = goodreadsBookData.Isbn;
            }
        }

        private void SetSubtitle(GoogleBookModel googleBooksBookData)
        {
            if (googleBooksBookData.Subtitle != null)
            {
                bookModel.Subtitle = googleBooksBookData.Subtitle;
            }
        }

        private void SetGenres(GoogleBookModel googleBooksBookData)
        {
            if (googleBooksBookData.Genres != null)
            {
                bookModel.Genres = googleBooksBookData.Genres;
            }
        }

        private void SetReviewCount(AmazonModel amazonBookData)
        {
            if (amazonBookData.ReviewCount != 0)
            {
                bookModel.AmazonReviewsCount = amazonBookData.ReviewCount;
            }
        }

        private void SetAmazonReviews(AmazonModel amazonBookData)
        {
            if (amazonBookData.Reviews != null)
            {
                bookModel.AmazonReviews = amazonBookData.Reviews;
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
            if (googleBooksBookData.PageCount != null)
            {
                bookModel.PageCount = googleBooksBookData.PageCount;
            }
            else if (goodreadsBookData.PageCount != null)
            {
                bookModel.PageCount = goodreadsBookData.PageCount;
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
            if (!String.IsNullOrEmpty(goodreadsBookData.Description))
            {
                bookModel.Description = goodreadsBookData.Description;
            }

            else if (!String.IsNullOrEmpty(googleBooksBookData.Description))
            {
                bookModel.Description = googleBooksBookData.Description;
            }           
        }

        private void SetAuthorsOfBook(GoodreadsModel goodreadsBookData, GoolgeBooksApi.GoogleBookModel googleBooksBookData)
        {
            if (googleBooksBookData.Authors != null)
            {
                bookModel.Authors = googleBooksBookData.Authors;
            }
            else if (goodreadsBookData.Authors != null)
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
            if (!String.IsNullOrEmpty(googleBooksBookData.Title))
            {
                bookModel.Title = googleBooksBookData.Title;
            }
            else if (!String.IsNullOrEmpty(goodreadsBookData.Title))
            {
                bookModel.Title = goodreadsBookData.Title;
            }
        }
    }
}
