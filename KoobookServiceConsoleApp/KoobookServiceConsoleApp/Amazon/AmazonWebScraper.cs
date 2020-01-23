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
        public AmazonModel CollectDataForBook(string isbn, string author)
        {
            ChromeOptions options = new ChromeOptions();
            //options.AddArguments("headless");
            IWebDriver driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            SeleniumHelper helper = new SeleniumHelper();
            amazonModel = new AmazonModel();
            driver.Navigate().GoToUrl("https://www.amazon.co.uk/");
            SearchBook(isbn, driver, helper);
            var bookProductPageAcessed = AccessBookFromSearchResults(driver, helper, author);

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

        private double CalculateAverageRating(int oneStarRatingPercentage, int twoStarRatingPercentage, int threeStarRatingPercentage, int fourStarRatingPercentage, int fiveStarRatingPercentage)
        {
            double totalRating = (oneStarRatingPercentage * 1) + (twoStarRatingPercentage * 2) + (threeStarRatingPercentage * 3) + (fourStarRatingPercentage * 4) + (fiveStarRatingPercentage * 5);
            double averageRating = Math.Round(totalRating / 100, 1);
            return averageRating;
        }

        private int GetRating(IWebElement ratingRow)
        {
            var ratingValue = ratingRow.GetAttribute("aria-label");
            var ratingValueFormatted = ratingValue.Replace("%", "");
            return Int32.Parse(ratingValueFormatted);
        }

        //I selected the result which contains the author that was retrived from the Googlebooks/Goodreads sources
        //to ensure that this solution doesnt click on the incorrect result item which leads to collecting information about 
        // a different book. 
        private bool AccessBookFromSearchResults(IWebDriver driver, SeleniumHelper helper, String author)
        {
            try
            {
                var searchResults = helper.WaitForElementsToBeVisible(driver, By.ClassName("s-result-list"))[0];
                var searchResultItems = searchResults.FindElements(By.ClassName("s-result-item")).ToList();
                var targetResultItem = searchResultItems.Where(item => item.Text.Contains(author)).First();
                var targetResultThumbnailImage = targetResultItem.FindElement(By.ClassName("s-image-fixed-height"));
                targetResultThumbnailImage.Click();
                return true;
            }
         
            catch (Exception e) {
                driver.Quit();
                return false;
            }
        }

        private void SearchBook(string isbn, IWebDriver driver, SeleniumHelper helper)
        {
            try
            {
                IWebElement searchBox = helper.WaitForElementToBeClickable(driver, By.Id("twotabsearchtextbox"));
                searchBox.SendKeys(isbn);
                searchBox.SendKeys(Keys.Enter);
            }
            catch (Exception e) {
                driver.Quit();
            }
        }

    }
}
