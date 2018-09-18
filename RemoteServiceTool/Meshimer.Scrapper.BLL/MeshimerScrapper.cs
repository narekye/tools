﻿using Meshimer.Common;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using System;
using System.Linq;
using System.Threading;

namespace Meshimer.Scrapper.BLL
{
    /// <summary>
    /// Author: Narek Yegoryan
    /// </summary>
    public class MeshimerScrapper : IDisposable
    {
        #region Fields

        private IWebDriver _webDriver;
        private FirefoxOptions _fireFoxOptions;
        private ChromeOptions _chromeOptions;
        private InternetExplorerOptions _explorerOptions;
        private readonly BrowserTypeEnum _broserType;

        #endregion

        #region Constructors

        public MeshimerScrapper(BrowserTypeEnum browserType = BrowserTypeEnum.Chrome)
        {
            _broserType = browserType;
            InitializeWebDriver(browserType);
        }

        #endregion

        #region Methods

        public string GetUserNameFromMeshimerPageAndHandle(Action executeOnMismatch = null)
        {
            var parsedUsername = string.Empty;
            if (_webDriver != null)
            {
                _webDriver.Navigate().GoToUrl("https://www.youtube.com");
                IWebElement blockElement = null;

                if (_broserType != BrowserTypeEnum.IE)
                    blockElement = _webDriver.FindElement(By.Id(Constants.wb_BlockDetails));

                if (blockElement != null)
                {
                    var elementText = blockElement.Text;
                    var index = elementText.IndexOf('\r');
                    parsedUsername = elementText.Substring(5, index - 5).Trim();
                }
            }

            string curentMachineUserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            if (curentMachineUserName.Split('\\').LastOrDefault() != parsedUsername)
                executeOnMismatch?.Invoke();

            return parsedUsername;
        }

        void InitializeWebDriver(BrowserTypeEnum browserType)
        {
            switch (browserType)
            {
                case BrowserTypeEnum.Chrome:
                    InitializeChromeDriverOptions();
                    _webDriver = new ChromeDriver(_chromeOptions);
                    break;
                case BrowserTypeEnum.Firefox:
                    InitializeFirefoxDriverOptions();
                    _webDriver = new FirefoxDriver(_fireFoxOptions);
                    break;
                default:
                    InitializeIEDriverOptions();
                    _webDriver = new InternetExplorerDriver(_explorerOptions);
                    break;
            }
        }

        void InitializeChromeDriverOptions()
        {
            _chromeOptions = new ChromeOptions();
            _chromeOptions.AddArgument("disable-infobars");
        }

        void InitializeFirefoxDriverOptions()
        {
            _fireFoxOptions = new FirefoxOptions();
            _fireFoxOptions.AcceptInsecureCertificates = true;
        }

        void InitializeIEDriverOptions()
        {
            _explorerOptions = new InternetExplorerOptions { EnableNativeEvents = false };
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// IDisposable support
        /// </summary>
        public void Dispose()
        {
            _webDriver?.Quit();
            _webDriver?.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
