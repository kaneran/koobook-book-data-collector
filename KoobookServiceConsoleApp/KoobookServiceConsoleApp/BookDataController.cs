using Goodreads.Models.Response;
using KoobookServiceConsoleApp.Amazon;
using KoobookServiceConsoleApp.GoodReadsApi;
using KoobookServiceConsoleApp.GoogleBooksApi;
using KoobookServiceConsoleApp.GoolgeBooksApi;
using KoobookServiceConsoleApp.TCP;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
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
        AmazonWebScraper amazonWebScraper;
        DominantColorWebScraper dominantColorWebScraper;
        TCPServer server;
        Task<Book> goodreadsTask;
        Task<GoogleBookModel> googleBookTask;
        Task<AmazonModel> amazonTask;
        string dominantColorString; 

        public BookDataController()
        {
            googleBooksApiKey = ConfigurationManager.AppSettings.Get("googleBooksApiKey");
            googleBookApi = new GoogleBookApi(googleBooksApiKey);
            goodreadsApiKey = ConfigurationManager.AppSettings.Get("goodreadsApiKey");
            goodreadsApiSecret = ConfigurationManager.AppSettings.Get("goodreadsApiSecret");
            goodreadsApi = new GoodReadsApi.GoodreadsApi(goodreadsApiKey, goodreadsApiSecret);
        }

        //This method works by first checking with the data(passed into the method's argument) is an Isbn. If it is not an isbn number it will first collect book data from the Google books api and uses the output to retrieve the
        //isbn number and this is then used to get book data from the Goodreads api. If the data is an isbn number then get the book data from the Google books and Goodreads apis. Afterwards, it then uses the data retrieved from
        //the Goodreads api to initalise the GoodreadsModel. This model along with the GoogleBookModel is used to set the description to the BookModel object which involves working the Python text summariser to receive a
        //summarised description of the book which is what is assigned to the BookModel object. It then proceeds use the AmazonWebScraper to scrape the relevent book data from the relevent product page and this returns
        //and AmazonModel object. All three models( GoodreadsModel, GoogleBooksModel and AmazonModel) were used to initialise the attributes for the BookModel object. 
        public async void CollectDataFromSources(string data)
        {
            bookModel = new BookModel();
            server = new TCPServer();
            dominantColorWebScraper = new DominantColorWebScraper();
            amazonWebScraper = new AmazonWebScraper();
            AmazonModel amazonBookData = null;

            IWebDriver driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            //If the data doesnt contain the isbn then the isbn will be initially retrieved
            //from the google books api which is then passed into the good reads api solution
            var dataContainsIsbn = data.All(char.IsDigit);

            if (dataContainsIsbn.Equals(false))
            {
                googleBookTask = googleBookApi.CollectDataForBook(data);
                amazonTask = amazonWebScraper.CollectDataForBook(driver, data);
                var isbn = GetIsbn(googleBookModel);
                goodreadsTask = goodreadsApi.SearchByIsbn(isbn);
            }
            else
            {
                googleBookTask = googleBookApi.CollectDataForBook(data);
                amazonTask = amazonWebScraper.CollectDataForBook(driver, data);
                goodreadsTask = goodreadsApi.SearchByIsbn(data);
            }

            Task.WhenAll(amazonTask,googleBookTask, goodreadsTask);

            googleBookModel = googleBookTask.Result;

            //Check if model has a valid thumbnail url
            if (googleBookModel.ThumbnailUrl.Count() > 2)
            {
                DominantColorWebScraper dominantColorWebScraper = new DominantColorWebScraper();
                var dominantColour = dominantColorWebScraper.GetDominantColor(driver, googleBookModel.ThumbnailUrl);
                dominantColorString = dominantColour;
            }

            var book = goodreadsTask.Result;
            GoodreadsModel goodreadsModel = null;
            if (book != null)
            {
                goodreadsModel = goodreadsApi.CollectDataForBook(book);
            }
            
            amazonBookData = amazonTask.Result;
            SetDescription(goodreadsModel, googleBookModel);

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
            SetDominantColor(dominantColorString);
        }

       

        //This method works by iterating through each book from the list of books(passed into the method's arguments). For each book, the book's title, authors, thumnbail, isbn is appended using a string builder. 
        //Each data attribute is separated by the "$" symbol and if there are more than one author, then each author will be separated by the "#" symbol. After the book's details are appended, the string builder appends
        // a "|" symbol which primarily used to distiguish each books' data string. After it iterating through each book, the output of the string builder is returned by this method.
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

                if (book.Authors.Count != 0)
                {
                    if (book.Authors.Count > 1)
                    {
                        //For each author, append it followed by a "#" symbol 
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


                }
                else
                {
                    Append(sb, null, "$");
                }

                Append(sb, book.ThumbnailUrl, "|");
            }

            return sb.ToString();
        }

        //This method works by using LINQ to get the Isbn number from the GoogleBookModel(passed into the method's argument) and return it. 
        private static string GetIsbn(GoogleBookModel book)
        {
            return book.IndustryIdentifiersDatas.Where(iid => iid.Type.Equals("ISBN_13")).Select(d => d.Identifier).SingleOrDefault();
        }

        //This method works by using the string builder to append the book's title, authors, description, subtitle, isbn, average ratings from GoogleBooks, Goodreads and Amazon, Amazon rating percentages, Amazon reviews,
        //Amazon reviews count, genres, page, thumbnail. Each data attribute is separated by "$" and for if there is more than one author, genre or review, these will be separated by the "#" symbol. After appending all
        //the book data, the output of the string builder is returned by this method.
        public string ConcatBookData()
        {
            try
            {

                StringBuilder sb = new StringBuilder();
                Append(sb, bookModel.Title, "$");
                if (bookModel.Authors.Count > 1)
                {
                    //For each authors, append it followed by a "#" symbol 
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
                        //For each review, append it followed by a "#" symbol 
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
                else
                {
                    Append(sb, "*", "$");
                }

                Append(sb, bookModel.AmazonReviewsCount.ToString(), "$");

                if (bookModel.Genres.Count > 1)
                {
                    //For each genre, append it followed by a "#" symbol 
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
                Append(sb, bookModel.ThumbnailUrl, "$");
                Append(sb, bookModel.DominantColor, "$");
                return sb.ToString();
            }
            catch (Exception e)
            {
                var msg = e.Message;
                return null;
            }
        }

        //If the string is empty then write a "*" in the string builder. This makes it easier to know which strings were empty when the android application decodes it
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

        //This method would assign the dominant colour value to the book model iff it is not null
        private void SetDominantColor(string dominantColor)
        {
                if (!String.IsNullOrEmpty(dominantColor)) {
                    bookModel.DominantColor = dominantColor;
                }
        }

        //This method works by checking whether the GoogleBookModel object(passed into the method's argument) is not null. If it is not null then it will check whether the Thumbnail url attribute is null or empty. If it is
        //not then this thumbnail url will be assigned to the public BookModel object data attribute that holds the thumbnail url.
        public void SetThumbnailUrl(GoogleBookModel googleBooksBookData)
        {
            if (googleBooksBookData != null)
            {
                if (!String.IsNullOrEmpty(googleBooksBookData.ThumbnailUrl))
                {
                    bookModel.ThumbnailUrl = googleBooksBookData.ThumbnailUrl;
                }
            }
        }

        //This method works by checking whether the GoogleBookModel object and GoodreadsModel object (both passed into the method's argument) is not null. If it is not null then it will check whether the GoodreadsModel object's Isbn attribute is 
        //null or empty. If it is not then this isbn value url will be assigned to the public BookModel object data attribute that holds the isbn. If it is null or empty then it check whether the GoogleBooksModel object's Isbn attribute is 
        //null or empty. If it is then this isbn value url will be assigned to the public BookModel object's data attribute that holds the isbn. If it is null or empty then the isbn(passed into the method's arguments) will be checked to see
        //if it contains numbers to confirm that it is a valid isbn. If this check passes then this isbn will be assigned to the public BookModel object's data attribute that holds the isbn.

        //However, if the one or both of the GoogleBookModel and GoodreadsModel object is null, it will check each model object individually to see if it is null and if it isn't then it will proceed to check if it's Isbn data attribute is not null nor empty.
        //Similarly, once that passes then it will be assigned to the public BookModel object data attribute that holds the isbn. If it neither object is not null then it repeats the same process as for checking the isbn(passed into the method;s arguments)
        //to see if it contains number and if it does, it will assign that value to the public BookModel object's data attribute that holds the isbn.
        private void SetIsbn(GoodreadsModel goodreadsBookData, GoogleBookModel googleBookModel, string retrievedIsbn)
        {
            if (googleBookModel != null && goodreadsBookData != null)
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
                    if (retrievedIsbn.All(char.IsDigit).Equals(true)) {
                        bookModel.Isbn = retrievedIsbn;
                    }
                    
                }
            }
            else {
                if (googleBookModel != null)
                {
                    if (!String.IsNullOrEmpty(GetIsbn(googleBookModel)))
                    {
                        bookModel.Isbn = GetIsbn(googleBookModel);
                    }
                }
                else if (goodreadsBookData != null)
                {
                    if (!String.IsNullOrEmpty(goodreadsBookData.Isbn))
                    {
                        bookModel.Isbn = goodreadsBookData.Isbn;
                    }
                }
                else {

                    if (retrievedIsbn.All(char.IsDigit).Equals(true))
                    {
                        bookModel.Isbn = retrievedIsbn;
                    }
                }
            }
        }

        //This method works by checking whether the GoogleBookModel object(passed into the method's argument) is not null. If it is not null then it will check whether the Subtitle attribute is null or empty. If it is
        //not then this Subtitle data value will be assigned to the public BookModel object's data attribute that holds the subtitle.
        private void SetSubtitle(GoogleBookModel googleBooksBookData)
        {
            if (googleBooksBookData != null)
            {
                if (!String.IsNullOrEmpty(googleBooksBookData.Subtitle))
                {
                    bookModel.Subtitle = googleBooksBookData.Subtitle;
                }
            }
        }

        //This method works by checking whether the GoogleBookModel object(passed into the method's argument) is not null. If it is not null then it will check whether the Genres attribute is null. If it is
        //not then this will be assigned to the public BookModel object's data attribute that holds the genres. If the object is null then an empty list of strings will be assigned to the public BookModel object data attribute that holds the genres.
        private void SetGenres(GoogleBookModel googleBooksBookData)
        {
            if (googleBooksBookData != null)
            {
                bookModel.Genres = googleBooksBookData.Genres;
            }
            else {
                bookModel.Genres = new List<string>();
            }
            

        }

        //This method works by checking whether the AmazonModel object(passed into the method's argument) is not null. If it is not null then it will check whether the Reviews count attribute is not equal to 0. If it does not equal to 0
        //then this  data value will be assigned to the public BookModel object data attribute that holds the Amazon reviews count. If it does equal 0 then assign a 0 to the same data attribute on the BookModel object. However, if
        //the AmazonModel object was null then assign a 0 to the same data attribute on the BookModel object.
        private void SetReviewCount(AmazonModel amazonBookData)
        {
            if (amazonBookData != null)
            {
                if (amazonBookData.ReviewCount != 0)
                {
                    bookModel.AmazonReviewsCount = amazonBookData.ReviewCount;
                }
                else
                {
                    bookModel.AmazonReviewsCount = 0;
                }
            }
            else {
                bookModel.AmazonReviewsCount = 0;
            }
        }

        //This method works by checking whether the AmazonModel object(passed into the method's argument) is not null. If it is not null then it will check whether the Reviews attribute is actually contains one or more reviews. If it does
        //then this will be assigned to the public BookModel object data attribute that holds the reviews. If it contains no reviews then an empty list of strings will be assigned to the public BookModel object data attribute that holds the reviews. 
        //If the object is null then an empty list of strings will be assigned to the public BookModel object's data attribute that holds the reviews.
        private void SetAmazonReviews(AmazonModel amazonBookData)
        {
            if (amazonBookData != null)
            {
                if (amazonBookData.Reviews.Count != 0)
                {
                    bookModel.AmazonReviews = amazonBookData.Reviews;
                }
                else
                {
                    bookModel.AmazonReviews = new List<string>();
                }
            }
            else {
                bookModel.AmazonReviews = new List<string>();
            }
        }

        //This method works by checking whether the AmazonModel object(passed into the method's argument) is not null. If it is not null then it will check whether the Average rating attribute is not equal to 0. If it is
        //not equal to 0 then the average rating attribute along with the other atributes relating to amazon ratings will will be assigned to the relevant data attributes of the public BookModel object.
        private void SetAmazonRatings(AmazonModel amazonBookData)
        {
            if (amazonBookData != null)
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
        }

        //This method works by checking whether the GoogleBookModel object and GoodreadsModel object (both passed into the method's argument) is not null. If it is not null then it will check whether the GoodreadsModel object's page count attribute is 
        //null. If it is not null then assign this data value to the public BookModel object's data attribute that holds the page count. If it is null then check whether the GoogleBooksModel object's page count attribute value is equal to 0.
        //If it does not equal 0 then assign this data value to the public BookModel object's data attribute that holds the page count. 

        //However, if one or both of the objects(passed into the method's arguments) is not null then it first
        //checks whether the GoodreadsModel object is null. If it is not null then it will check whether the GoodreadsModel object's page count attribute is 
        //null. If it is not null then assign this data value to the public BookModel object's data attribute that holds the page count. If it is null then it will check whether the GoogleBooksModel object is null. If it is not null then it will check whether 
        //the GoogleBooksModel object's page count attribute value is equal to 0. If it does not equal 0 then assign this data value to the public BookModel object's data attribute that holds the page count.
        private void SetPageCount(GoodreadsModel goodreadsBookData, GoogleBookModel googleBooksBookData)
        {
            if (goodreadsBookData != null && googleBooksBookData != null)
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
            else
            {
                if (goodreadsBookData != null)
                {
                    if (goodreadsBookData.PageCount != null)
                    {
                        bookModel.PageCount = goodreadsBookData.PageCount;
                    }
                }
                else if (googleBooksBookData != null)
                {
                    if (googleBooksBookData.PageCount != 0)
                    {
                        bookModel.PageCount = googleBooksBookData.PageCount;
                    }
                }
            }

        }

        //For each object passed into the method's arguments, it will check whether it is null. If it is not null then it further check to see if the object's attribute for holding the average rating is not equal to 0. If is does not equal 
        //then it will assign this data value to the public BookModel object's corresponding data attribute.
        private void SetAverageRatings(GoodreadsModel goodreadsBookData, GoolgeBooksApi.GoogleBookModel googleBooksBookData, AmazonModel amazonBookData)
        {
            if (googleBooksBookData != null)
            {
                if (googleBooksBookData.AverageRating != 0)
                {
                    bookModel.GoogleBooksAverageRating = googleBooksBookData.AverageRating;
                }
            }

            if (goodreadsBookData != null)
            {
                if (goodreadsBookData.AverageRating != 0)
                {
                    bookModel.GoodreadsAverageRating = goodreadsBookData.AverageRating;
                }
            }

            if (amazonBookData != null)
            {
                if (amazonBookData.AverageRating != 0)
                {
                    bookModel.GoodreadsAverageRating = amazonBookData.AverageRating;
                }
            }
        }


        //This method by first checking whether both the GoodreadsModel and GoogleBooksModel objects(passed into the method's arguments) is not null. If both are not null then it will check whether the description attribute value
        //held by each object actually contains a description. If both objects hold information relating to the description then it will check the description held by the GoodreadsModel object is longer than the description held by 
        //the GoogleBookModel object. If it is longer than summarise the description held by the GoodreadsModel object and assign the summarised description to the public BookMode object's data attribute which holds the description.
        //If it is not longer than summarise the description held by the GoogleBookModel object and assign the summarised description to the public BookMode object's data attribute which holds the description.

        //However, if one or both objects do not contain a description then it will check whether the description held by the GoodreadsModel is null or empty. If it is not then it will summarise that description and assign the summarised
        //description to the public BookMode object's data attribute which holds the description. If it is null or empty then it check whether the description held by the GoogleBooksModel is null or empty. If it is not then it will summarise that description and assign the summarised
        //description to the public BookMode object's data attribute which holds the description. 
        //The second outer else statement is to deal with cases where the GoodreadsModel  and/or GoogleBookModel objects is null. If this else statement is satisfied then it will check if the GoogleBooksModel object is null
        //and if it is not null then it will check  whether the description held by the GoogleBookModel object is null or empty. It it's not null or empty then this description will be summarised and the summarised description will be assigned
        //to the public BookModel object's data attribute that holds the description. If the GoogleBookModel object is null then it will check if the GoodreadsModel object is null
        //and if it is not null then it will check  whether the description held by the GoodreadsModel object is null or empty. It it's not null or empty then this description will be summarised and the summarised description will be assigned
        //to the public BookModel object's data attribute that holds the description.
        private void SetDescription(GoodreadsModel goodreadsBookData, GoolgeBooksApi.GoogleBookModel googleBooksBookData)
        {

            string summarisedBookDescription;

            if (goodreadsBookData != null && googleBooksBookData != null)
            {
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
            else
            {
                if (googleBooksBookData != null)
                {
                    if (!String.IsNullOrEmpty(googleBooksBookData.Description))
                    {
                        summarisedBookDescription = SummariseDescription(googleBooksBookData.Description);
                        bookModel.Description = summarisedBookDescription;
                    }
                }
                else if (goodreadsBookData != null)
                {
                    if (!String.IsNullOrEmpty(goodreadsBookData.Description))
                    {
                        summarisedBookDescription = SummariseDescription(goodreadsBookData.Description);
                        bookModel.Description = summarisedBookDescription;
                    }
                }

            }
        }

        //This method by first checking whether both the GoodreadsModel and GoogleBooksModel objects(passed into the method's arguments) is not null. If both are not null then it will check whether the Authors attribute value
        //held by GoogleBookModel model actually contains one or more author. If this check is passed then it will assign these authors to the public BookMode object's data attribute which holds the authors. If the check was not met then
        //it will check whether the Authors attribute value held by GoodreadsModel model actually contains one or more author. If this check is passed then it will iterate through each author and add it to the initialised list of authors and then will assign this list
        //to the public BookModel object's data attribute which holds the authors. If neither objects contained authors then assign an empty string list to the public BookModel object's data attribute which holds the authors. 

        //However, if one or both objects are null then it will check if the GoogleBookModel object is null. If it isn't then it will check whether the Authors attribute value held by GoogleBookModel model actually contains one or more author. 
        //If this check is passed then it will assign these authors to the public BookMode object's data attribute which holds the authors. If the GoogleBookModel object is null then it will check whether the GoodreadModel is not null and if it is not null then
        //it will check whether the Authors attribute value held by GoodreadsModel model actually contains one or more author. If this check is passed then it will iterate through each author and add it to the initialised list of authors and then will assign this list
        //to the public BookModel object's data attribute which holds the authors. If neither objects are null then it will assign an empty string list to the public BookModel object's data attribute which holds the authors. 

        private void SetAuthorsOfBook(GoodreadsModel goodreadsBookData, GoogleBookModel googleBooksBookData)
        {
            if (goodreadsBookData != null && googleBooksBookData != null)
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
                else {
                    bookModel.Authors = new List<string>();
                }
            }
            else
            {
                if (googleBooksBookData != null)
                {
                    if (googleBooksBookData.Authors.Count != 0)
                    {
                        bookModel.Authors = googleBooksBookData.Authors;
                    }
                }
                else if (goodreadsBookData != null)
                {
                    if (goodreadsBookData.Authors.Count != 0)
                    {

                        List<string> authors = new List<string>();
                        foreach (var author in goodreadsBookData.Authors)
                        {
                            authors.Add(author.Name);
                        }
                        bookModel.Authors = authors;
                    }
                    else {
                        bookModel.Authors = new List<string>();
                    }

                }
            }
        }

        //Tnis method works by checking with the GoogleBookModel and GoodreadsModel(passed into the method's arguments) is not null. If both are not null then it check whether the title attribute held by the GoodreadsModel object is not null or empty. If it is
        //not null or empty then this data value will be assigned to the public BookModel object's data attribute which holds the title. If the object's title attribute value is null or empty then check whether the title attribute held by the GoodreadsModel object 
        //is not null or empty. If it is not null or empty then this data value will be assigned to the public BookModel object's data attribute which holds the title. 

        //However if one or both of the objects are null then check whether the GoogleBookModel object is null. If it is not null then check whether the title attribute held by the GoogleBookModel object is not null or empty. If it is
        //not null or empty then this data value will be assigned to the public BookModel object's data attribute which holds the title. If the GoogleBookModel object is null then check whether the GoodreadsModel object is null. If it is not null then check whether 
        //the title attribute held by the GoodreadsModel object is not null or empty. If it is not null or empty then this data value will be assigned to the public BookModel object's data attribute which holds the title.
        private void SetBookTitle(GoodreadsModel goodreadsBookData, GoogleBookModel googleBooksBookData)
        {
            if (goodreadsBookData != null && googleBooksBookData != null)
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
            else
            {
                if (googleBooksBookData != null)
                {
                    if (!String.IsNullOrEmpty(googleBooksBookData.Title))
                    {
                        bookModel.Title = googleBooksBookData.Title;
                    }
                }
                else if (goodreadsBookData != null)
                {
                    if (!String.IsNullOrEmpty(goodreadsBookData.Title))
                    {
                        bookModel.Title = goodreadsBookData.Title;
                    }
                }
            }

        }

        //This method works by checking whether the book description(passed into the method's arguments) is null or empty. If it is not null or empty then it will check whether contains more than 30 words. If it is then the it will connect to the Python text summariser through TCP and send
        //the book description to it such that it will summarise it and send it back to the client. After is receives it, this method will return this summarised description. If the descriptions contains less than 30 words
        //then the method will return the book description without it being summarised. If the book description(passed into the method's argument) is null or empty then the method will return an empty string.

        //Note- To provide a way for the server to know that the entire book description has been retrieved, the "#" will be used to mark the end of the string
        public string SummariseDescription(string bookDescription)
        {
            TCPClient client = new TCPClient();
            string summarisedBookDescription = "";
            if (!String.IsNullOrEmpty(bookDescription))
            {
                //Check if description contains more than 30 words
                if (bookDescription.Split(' ').Count() > 30)
                {
                    summarisedBookDescription = client.Connect(bookDescription);
                    while (String.IsNullOrEmpty(summarisedBookDescription))
                    {
                        //Do nothing
                    }
                    return summarisedBookDescription;
                }
                else {
                    return bookDescription;
                }

            }
            else {
                return "";
            }
            
        }


    }
}
