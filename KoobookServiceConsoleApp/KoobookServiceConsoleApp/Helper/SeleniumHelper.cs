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
        //This method works by using a waiting for a certain period of time(specified in the method's arguments) until a given element is clickable. The locator of this element is also passed into the method's arguments.
        //After waiting, this element is returned by this method.
        public IWebElement WaitForElementToBeClickable(IWebDriver driver, By by, int time) {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(time));
            var element = wait.Until(ExpectedConditions.ElementToBeClickable(by));   
            return element;
        }

        //This method works by using a waiting for at most 10 seconds until all elements, based on the web locator passed into the method's arguments, are clickable.
        //After waiting, the elements are returned as a list by this method.
        public List<IWebElement> WaitForElementsToBeVisible(IWebDriver driver, By by)
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            var elements = wait.Until(ExpectedConditions.VisibilityOfAllElementsLocatedBy(by));
            return elements.ToList();
        }

        //Credit to Amith from https://stackoverflow.com/questions/3401343/scroll-element-into-view-with-selenium for the solution
        //This method works by using driver and web locator passed into the method's arguments to find the target web element. Then it uses the JavaScript executor to scroll to specific element 
        public void ScrollToElement(IWebDriver driver, By by) {
            var element = driver.FindElement(by);
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);

        }
    }
}
