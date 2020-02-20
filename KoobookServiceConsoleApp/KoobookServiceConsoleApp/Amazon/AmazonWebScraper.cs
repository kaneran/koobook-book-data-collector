using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using KoobookServiceConsoleApp.Helper;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace KoobookServiceConsoleApp.Amazon
{
    public class AmazonWebScraper
    {
        public AmazonModel amazonModel;

        //This method works by using Selenium WebDriver to navigate to Amazon search results url which is appended with the isbn(passed into the method's argument)
        //It then clicks on the first product from the search result which is presumbably the book with the corresponding isbn. It then proceeds to scrap the relevent data including ratings and reviews
        //It then uses the scrapped data to assign it to the AmazonModel instance which is then returned by this method. 
        public AmazonModel CollectDataForBook(string isbn, string author)
        {
            ChromeOptions options = new ChromeOptions();
            //options.AddArguments("headless");
            IWebDriver driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            SeleniumHelper helper = new SeleniumHelper();
            amazonModel = new AmazonModel();
            driver.Navigate().GoToUrl("https://www.amazon.co.uk/s?k="+isbn+"&ref=nb_sb_noss");
            //SearchBook(isbn, driver, helper);
            var bookProductPageAcessed = AccessBookFromSearchResults(driver, helper, author);
            try
            {
                var popupCloseButton = helper.WaitForElementToBeClickable(driver, By.ClassName("a-icon-close"),2);
                popupCloseButton.Click();
            }
            catch (Exception e) {

            }
            var averageRating = 0.0;
            int fiveStarRatingPercentage = 0;
            int fourStarRatingPercentage = 0;
            int threeStarRatingPercentage = 0;
            int twoStarRatingPercentage = 0;
            int oneStarRatingPercentage = 0;

            if (bookProductPageAcessed.Equals(false))
            {
                amazonModel.AverageRating = averageRating;
                amazonModel.FiveStarRatingPercentage = fiveStarRatingPercentage;
                amazonModel.FourStarRatingPercentage = fourStarRatingPercentage;
                amazonModel.ThreeStarRatingPercentage = threeStarRatingPercentage;
                amazonModel.TwoStarRatingPercentage = twoStarRatingPercentage;
                amazonModel.OneStarRatingPercentage = oneStarRatingPercentage;
                amazonModel.Reviews = new List<string>();
            }
            else
            {
                var ratingsDictionary = GetCustomerRatingsData(driver, helper);

                if (ratingsDictionary != null)
                {
                    ratingsDictionary.TryGetValue("fiveStar", out fiveStarRatingPercentage);
                    ratingsDictionary.TryGetValue("fourStar", out fourStarRatingPercentage);
                    ratingsDictionary.TryGetValue("threeStar", out threeStarRatingPercentage);
                    ratingsDictionary.TryGetValue("twoStar", out twoStarRatingPercentage);
                    ratingsDictionary.TryGetValue("oneStar", out oneStarRatingPercentage);
                    averageRating = CalculateAverageRating(oneStarRatingPercentage, twoStarRatingPercentage, threeStarRatingPercentage, fourStarRatingPercentage, fiveStarRatingPercentage);
                }

                var reviewsCount = GetReviewsCount(driver);
                var reviews = GetReviews(driver);


                if (ratingsDictionary != null)
                {
                    amazonModel.AverageRating = averageRating;
                    amazonModel.FiveStarRatingPercentage = fiveStarRatingPercentage;
                    amazonModel.FourStarRatingPercentage = fourStarRatingPercentage;
                    amazonModel.ThreeStarRatingPercentage = threeStarRatingPercentage;
                    amazonModel.TwoStarRatingPercentage = twoStarRatingPercentage;
                    amazonModel.OneStarRatingPercentage = oneStarRatingPercentage;
                }

                if (reviews != null)
                {
                    amazonModel.Reviews = reviews;
                }
                else {
                    amazonModel.Reviews = new List<string>();
                }
                amazonModel.ReviewCount = reviewsCount;
                driver.Quit();
            }
            return amazonModel;
        }

        //I mainly want the reviews to be from the UK as it will be written in the English language
        //However if there are international reviews then translate these to English
        private List<string> GetReviews(IWebDriver driver)
        {
            try
            {
                //Uses the ClassName web locator to get all the Reviews from Amazon product details page. If then executes LINQ query to get all the reviews that are written in a foreign language. If the LINQ query returns
                //foreign reviews then interate through each foreign review and use the LinkText web locator to locate the link labelled "Translate review to English". After it locates it, it then clicks it. After iterating through all
                //the foreign which should now be translated to english, use the ClassName web locator to get all the review titles from the reviews section of the page. Because some of the titles are not currently displayed on the page, I used
                //LINQ to only get the review titles that were displayed and created a list from it. This method returns this list. If an exception was thrown during the main execution then the method will return null.
                var reviews = driver.FindElements(By.ClassName("review"));
                var internationalReviews = reviews.Where(review => review.Text.Contains("Translate review to English")).ToList();

                if (internationalReviews.Count > 0)
                {
                    //Translate each review to english
                    foreach (var internationalReview in internationalReviews)
                    {
                        var translateReviewToEnglishLink = internationalReview.FindElement(By.LinkText("Translate review to English"));
                        translateReviewToEnglishLink.Click();
                    }
                }
                var reviewTitles = driver.FindElements(By.ClassName("review-title")).Where(review => review.Displayed.Equals(true)).Select(review => review.Text).ToList();
                return reviewTitles;
            }
            catch (Exception e) {
                return null;
            }
        }

        //This method works by using the Id web locator to locate the reviews count section of the page and extract the text from the element. It uses this to check whether it contains the sub string "ratings" and if it does
        //then it will remove that substring from the text. If it doesn't contain this sub string then it check if it contains the sub string "rating" and if it does then remove it from the text. After should be left is the actual number
        //which represent the number of ratings. Before converting this text to an integer, it checks if the text is not empty. If this check is met then convert the string to an integer and return that integer value. If
        //it is not met then it will return 0. If an exception was caught during the main execution then the method will return 0.
        private int GetReviewsCount(IWebDriver driver)
        {
            try
            {
                var reviewsCountText = driver.FindElement(By.Id("acrCustomerReviewText")).Text;
                var reviewsCountFormatted = "";
                var reviewsCount = 0;
                //Contains more than one more rating
                if (reviewsCountText.Contains("ratings"))
                    reviewsCountFormatted = reviewsCountText.Replace("ratings", "");

                //Contains only one rating
                else if (reviewsCountText.Contains("rating"))
                    reviewsCountFormatted = reviewsCountText.Replace("rating", "");

                //If the reviews count is empty then that implies that there were no reviews made against the book
                if (reviewsCountFormatted != "")
                    reviewsCount = Int32.Parse(reviewsCountFormatted);
                return reviewsCount;
            }
            catch (Exception e) {
                return 0;
            }
        }

        //This method works by using the Selenium helper(passed into the method's arguments) to scroll to the customer ratings section which was located using the Id. It then uses the ClassName locator to get all the customer
        //rating bars as a list. For each rating bar, it gets the rating value from it and along with an approrpriate key was added to the dictionary which holds all the ratings values for 5,4,3,2 and 1 star ratings.
        //This dictionary is returned by this method. If an exception was caught during the main execution then the method will return null.  
        private Dictionary<string,int> GetCustomerRatingsData(IWebDriver driver, SeleniumHelper helper)
        {
            try
            {
                Dictionary<string, int> ratingsDictionary = new Dictionary<string, int>();
                helper.ScrollToElement(driver, By.Id("reviewsMedley"));
                var customerRatingBars = driver.FindElements(By.ClassName("a-meter")).ToList();
                var fiveStarRatingRow = customerRatingBars[0];
                var fiveStarRatingValue = GetRating(fiveStarRatingRow);
                ratingsDictionary.Add("fiveStar", fiveStarRatingValue);

                var fourStarRatingRow = customerRatingBars[1];
                var fourStarRatingValue = GetRating(fourStarRatingRow);
                ratingsDictionary.Add("fourStar", fourStarRatingValue);

                var threeStarRatingRow = customerRatingBars[2];
                var threeStarRatingValue = GetRating(threeStarRatingRow);
                ratingsDictionary.Add("threeStar", threeStarRatingValue);

                var twoStarRatingRow = customerRatingBars[3];
                var twoStarRatingValue = GetRating(twoStarRatingRow);
                ratingsDictionary.Add("twoStar", twoStarRatingValue);

                var oneStarRatingRow = customerRatingBars[4];
                var oneStarRatingValue = GetRating(oneStarRatingRow);
                ratingsDictionary.Add("oneStar", oneStarRatingValue);

                return ratingsDictionary;
            }
            catch (Exception e) {
                return null;
            }
        }

        //This method works by first calculating the total rating which is then divided by 100 and rounded to 1 decimal place. This final value is returned by this method.
        private double CalculateAverageRating(int oneStarRatingPercentage, int twoStarRatingPercentage, int threeStarRatingPercentage, int fourStarRatingPercentage, int fiveStarRatingPercentage)
        {
            double totalRating = (oneStarRatingPercentage * 1) + (twoStarRatingPercentage * 2) + (threeStarRatingPercentage * 3) + (fourStarRatingPercentage * 4) + (fiveStarRatingPercentage * 5);
            double averageRating = Math.Round(totalRating / 100, 1);
            return averageRating;
        }

        //This method works by getting the label value from the rating element(passed into this method's argument) which should return something like "34%". It then remove the "%" from the label and converts it to an integer
        //and this is returned by the method
        private int GetRating(IWebElement ratingRow)
        {
            var ratingValue = ratingRow.GetAttribute("aria-label");
            var ratingValueFormatted = ratingValue.Replace("%", "");
            return Int32.Parse(ratingValueFormatted);
        }

        //It first waits untl the search results are visible in the page. It then gets the list of web elements which were located by ClassName and these elements are the individual result items.
        //It then selects the first item from that list and uses it locate the thumbnail image within that particular result item and this was located by using ClassName. After retrieving the thumbnail image, it then clicks it
        //and the method returns true. 

        //However, if the initial wiating of the results list times out, this means that there were no results returned and this will thrown an tiemout exception which will be caught. After it's caught, the selenium webdriver will quit and
        //the method returns false. 
        private bool AccessBookFromSearchResults(IWebDriver driver, SeleniumHelper helper, String author)
        {
            try
            {

                var searchResults = driver.FindElements(By.ClassName("s-result-list"))[1];
                var searchResultItems = searchResults.FindElements(By.ClassName("s-result-item")).ToList();
                var targetResultItem = searchResultItems.First();
                var targetResultThumbnailImage = targetResultItem.FindElement(By.ClassName("s-image-fixed-height"));
                targetResultThumbnailImage.Click();
                return true;
            }
         
            catch (Exception e) {
                driver.Quit();
                return false;
            }
        }


    }
}
