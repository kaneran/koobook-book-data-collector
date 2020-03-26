using KoobookServiceConsoleApp.Helper;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp
{
    class DominantColorWebScraper
    {

        public string GetDominantColor(IWebDriver driver,string thumbnailUrl) {

            ChromeOptions options = new ChromeOptions();
            //options.AddArguments("headless");
            SeleniumHelper helper = new SeleniumHelper();
            
            driver.Navigate().GoToUrl("https://labs.tineye.com/color/");
            helper.WaitForElementToBeClickable(driver, By.Id("image_url"), 5);

            //Enter the thumbnail url into the "Enter image url" input field
            var enterImageUrlInputField = driver.FindElement(By.Id("image_url"));
            enterImageUrlInputField.SendKeys(thumbnailUrl);
            enterImageUrlInputField.SendKeys(Keys.Enter);

            //Wait for class Results to appear...
            helper.WaitForElementToBeClickable(driver, By.ClassName("results"), 5);

            //Extract dominant colour from results 

            var extractedColors = driver.FindElements(By.ClassName("result-color"));
            var dominantColor = extractedColors.First();

            var dominantColorValue = dominantColor.FindElement(By.ClassName("hex-color")).Text;

            driver.Quit();
            return dominantColorValue;
        }

    }
}
