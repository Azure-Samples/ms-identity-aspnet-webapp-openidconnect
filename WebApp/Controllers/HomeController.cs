using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApp.Utils;

namespace WebApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult About()
        {
            ViewBag.Name = ClaimsPrincipal.Current.FindFirst("name").Value;
            ViewBag.AuthorizationRequest = string.Empty;

            // The object ID claim will only be emitted for work or school accounts at this time.
            Claim oid = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");
            ViewBag.ObjectId = oid == null ? string.Empty : oid.Value;

            // The 'preferred_username' claim can be used for showing the user's primary way of identifying themselves
            ViewBag.Username = ClaimsPrincipal.Current.FindFirst("preferred_username").Value;

            // The subject or nameidentifier claim can be used to uniquely identify the user
            ViewBag.Subject = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
            return View();
        }

        public async Task<ActionResult> SendMail()
        {
            ITokenAcquirer tokenAcquirer = TokenAcquirerFactory.GetDefaultInstance().GetTokenAcquirer();
            var result = await tokenAcquirer.GetTokenForUserAsync(new[] { "Mail.Send" });

            return View();
        }

        public async Task<ActionResult> SendMail(string recipient, string subject, string body)
        {
            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = body
                },
                ToRecipients = new List<Recipient>()
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = recipient
                    }
                }
            },
                        CcRecipients = new List<Recipient>()
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = recipient
                    }
                }
            }
            };
            try
            {
                GraphServiceClient graphServiceClient = this.GetGraphServiceClient();
                await graphServiceClient.Me
                    .SendMail(message, true)
                    .Request()
                    .WithScopes("Mail.Send").PostAsync();
                return View();
            }
            catch (ServiceException graphEx) when (graphEx.InnerException is MicrosoftIdentityWebChallengeUserException)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                return View();
            }
            catch(Exception ex)
            {
                return View();
            }
        }

        public async Task<ActionResult> ReadMail()
        {
            try
            {
                GraphServiceClient graphServiceClient = this.GetGraphServiceClient();
                var messages = await graphServiceClient.Me.Messages
                    .Request()
                    .WithScopes("Mail.Read").GetAsync();
                ViewBag.Message = messages.Count.ToString();

                return View();
            }
            catch (ServiceException graphEx) when (graphEx.InnerException is MicrosoftIdentityWebChallengeUserException)
            {
                var exc = graphEx.InnerException as MicrosoftIdentityWebChallengeUserException;
                var authenticationProperties = new AuthenticationProperties();
                if (exc.Scopes != null)
                {
                    authenticationProperties.Dictionary.Add("scopes", string.Join(" ", exc.Scopes));
                }
                if (!string.IsNullOrEmpty(exc.MsalUiRequiredException.Claims))
                {
                    authenticationProperties.Dictionary.Add("claims", exc.MsalUiRequiredException.Claims);
                }
                authenticationProperties.Dictionary.Add("login_hint", (HttpContext.User as ClaimsPrincipal).GetDisplayName());
                authenticationProperties.Dictionary.Add("domain_hint", (HttpContext.User as ClaimsPrincipal).GetDomainHint());

                HttpContext.GetOwinContext().Authentication.Challenge(authenticationProperties, OpenIdConnectAuthenticationDefaults.AuthenticationType);
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
                return View();
            }
        }

        public async Task<ActionResult> ViewProfile()
        {
            var spaAuthCode = HttpContext.Session["Spa_Auth_Code"];

            ViewBag.SpaAuthCode = spaAuthCode as string;

            return await Task.Run(() => View());
        }

        public void RefreshSession()
        {
            HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties { RedirectUri = "/Home/ReadMail" },
                OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }
    }
}