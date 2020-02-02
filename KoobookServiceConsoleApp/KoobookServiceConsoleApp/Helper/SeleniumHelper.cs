using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoobookServiceConsoleApp.Helper
{
    public class SeleniumHelper
    {

        //Explicit wait, credit to Anuja J from https://stackoverflow.com/questions/20077860/selenium-webdriver-explicit-wait for the solution
        public IWebElement WaitForElementToBeClickable(IWebDriver driver, By by, int time) {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(time));
            var element = wait.Until(ExpectedConditions.ElementToBeClickable(by));   
            return element;
        }

        public List<IWebElement> WaitForElementsToBeVisible(IWebDriver driver, By by)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var elements = wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(by));
            return elements.ToList();
        }

        //Credit to Amith from https://stackoverflow.com/questions/3401343/scroll-element-into-view-with-selenium for the solution
        public void ScrollToElement(IWebDriver driver, By by) {
            var element = driver.FindElement(by);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);

        }
    }
}
