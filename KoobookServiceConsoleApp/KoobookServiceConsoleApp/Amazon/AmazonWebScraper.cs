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
        public AmazonModel CollectDataForBook(string isbn)
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArguments("headless");
            IWebDriver driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), options);
            SeleniumHelper helper = new SeleniumHelper();
            driver.Navigate().GoToUrl("https://www.amazon.co.uk/");
            SearchBook(isbn, driver, helper);
            AccessBookFromSearchResults(driver, helper);
            var ratingsDictionary = GetCustomerRatingsData(driver, helper);
            ratingsDictionary.TryGetValue("fiveStar", out var fiveStarRatingPercentage);
            ratingsDictionary.TryGetValue("fourStar", out var fourStarRatingPercentage);
            ratingsDictionary.TryGetValue("threeStar", out var threeStarRatingPercentage);
            ratingsDictionary.TryGetValue("twoStar", out var twoStarRatingPercentage);
            ratingsDictionary.TryGetValue("oneStar", out var oneStarRatingPercentage);
            var averageRating = CalculateAverageRating(oneStarRatingPercentage, twoStarRatingPercentage, threeStarRatingPercentage, fourStarRatingPercentage, fiveStarRatingPercentage);
            var reviewsCount = GetReviewsCount(driver);
            var reviews = GetReviews(driver);
            amazonModel = new AmazonModel()
            {
                AverageRating = averageRating,
                FiveStarRatingPercentage = fiveStarRatingPercentage,
                FourStarRatingPercentage = fourStarRatingPercentage,
                ThreeStarRatingPercentage = threeStarRatingPercentage,
                TwoStarRatingPercentage = twoStarRatingPercentage,
                OneStarRatingPercentage = oneStarRatingPercentage,
                Reviews = reviews,
                ReviewCount = reviewsCount
            };
            return amazonModel;
        }

        private List<string> GetReviews(IWebDriver driver)
        {
            var links = driver.FindElements(By.ClassName("a-link-emphasis"));
            var seeAllReviewsFromTheUKLink = links.Where(link => link.Text.Equals("See all reviews from the United Kingdom")).SingleOrDefault();
            seeAllReviewsFromTheUKLink.Click();
            var reviews = driver.FindElements(By.ClassName("review-title")).Where(review => review.Displayed.Equals(true)).Select(review => review.Text).ToList();
            return reviews;
        }

        private int GetReviewsCount(IWebDriver driver)
        {
            var reviewsCountText = driver.FindElement(By.Id("acrCustomerReviewText")).Text;
            var reviewsCountFormatted = reviewsCountText.Replace("ratings", "");
            var reviewsCount = Int32.Parse(reviewsCountFormatted);
            return reviewsCount;
        }

        private Dictionary<string,int> GetCustomerRatingsData(IWebDriver driver, SeleniumHelper helper)
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

        private void AccessBookFromSearchResults(IWebDriver driver, SeleniumHelper helper)
        {
            var searchResults = helper.WaitForElementsToBeVisible(driver, By.ClassName("s-result-list"))[0];
            var searchResultItems = searchResults.FindElements(By.ClassName("s-result-item"));
            var targetResultItem = searchResultItems[0];
            var targetResultThumbnailImage = targetResultItem.FindElement(By.ClassName("s-image-fixed-height"));
            targetResultThumbnailImage.Click();
        }

        private void SearchBook(string isbn, IWebDriver driver, SeleniumHelper helper)
        {
            IWebElement searchBox = helper.WaitForElementToBeClickable(driver, By.Id("twotabsearchtextbox"));
            searchBox.SendKeys(isbn);
            searchBox.SendKeys(Keys.Enter);
        }

    }
}
