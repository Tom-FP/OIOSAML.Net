using System;
using System.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

using dk.nita.saml20.Schema.BasicPrivilegeProfile;
using dk.nita.saml20.Utils;

using Xunit;

namespace IntegrationTest
{
    public class WebsiteDemoTest
    {
        [Fact (Skip = "Chrome driver is no longer supported")]
        public void LoginTest()
        {
            var serviceProviderEndpoint = ConfigurationManager.AppSettings["ServiceProviderEndpoint"];
            var username = ConfigurationManager.AppSettings["IdpUsername"];
            var password = ConfigurationManager.AppSettings["IdpPassword"];
            
            var options = new ChromeOptions();
            options.AddArguments("headless");
            
            var driver = new ChromeDriver(options);
            
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                
                //Navigate to login page
                driver.Navigate().GoToUrl(serviceProviderEndpoint);
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.CssSelector("[href*='/login.aspx/mitidsim']"))).Click();
                
                //Log in
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id("ContentPlaceHolder_MitIdSimulatorControl_txtUsername")))
                    .SendKeys(username);
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id("ContentPlaceHolder_MitIdSimulatorControl_txtPassword")))
                    .SendKeys(password);
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(By.Id("ContentPlaceHolder_MitIdSimulatorControl_btnSubmit"))).Click();
                
                //Verify response
                wait.Until(d => d.FindElement(By.XPath("//*[text()='SAML attributes']")));
            }
            finally
            {
                driver.Quit();
            }
        }

        // Copy of NUnit test  -  which can't be discovered!
        [Fact]
        public void CanDeserializeXml()
        {
            const string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<bpp:PrivilegeList xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:bpp=\"http://digst.dk/oiosaml/basic_privilege_profile\">\n\t<PrivilegeGroup Scope=\"urn:dk:gov:saml:cvrNumberIdentifier:93825592\">\n\t\t<Privilege>urn:dk:nemrefusion:privilegium1</Privilege>\n\t</PrivilegeGroup>\n</bpp:PrivilegeList>";
            var privilegeList = Serialization.DeserializeFromXmlString<PrivilegeListType>(xml);

            Assert.NotNull(privilegeList);
            Assert.Single(privilegeList.PrivilegeGroups);

            var privilegeGroup = privilegeList.PrivilegeGroups[0];
            Assert.Equal("urn:dk:gov:saml:cvrNumberIdentifier:93825592", privilegeGroup.Scope);
            Assert.Null(privilegeGroup.Constraint);
            Assert.Single(privilegeGroup.Privilege);

            var privilege = privilegeGroup.Privilege[0];
            Assert.Equal("urn:dk:nemrefusion:privilegium1", privilege);
        }

        [Fact]
        public void CanDeserializeXml_fail()
        {
            // Wrong namespace
            const string xml  = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<bpp:PrivilegeList xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:bpp=\"http://itst.dk/oiosaml/basic_privilege_profile\">\n\t<PrivilegeGroup Scope=\"urn:dk:gov:saml:cvrNumberIdentifier:12345678\">\n\t\t<Constraint Name=\"urn:ServiceProvider:Constraint1\">25.*</Constraint>\n<Constraint Name=\"urn:ServiceProvider:Constraint2\">3</Constraint>\n<Privilege>urn:ServiceProvider:Rights:Privilege1</Privilege>\n<Privilege>urn:ServiceProvider:Rights:Privilege2</Privilege>\n</PrivilegeGroup>\n</bpp:PrivilegeList>";

            Assert.Throws<InvalidOperationException>(() => Serialization.DeserializeFromXmlString<PrivilegeListType>(xml));
        }

    }

}
