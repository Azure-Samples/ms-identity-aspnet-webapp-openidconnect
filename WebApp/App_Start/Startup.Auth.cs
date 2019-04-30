using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Configuration;
using System.Globalization;
using System.IdentityModel.Claims;
using System.IdentityModel.Tokens;
using System.Threading.Tasks;
using System.Web;
using WebApp.Utils;
using WebApp_OpenIDConnect_DotNet.Models;

namespace WebApp
{
    public partial class Startup
    {
        public static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static readonly string appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        public static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static readonly string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // Custom middleware initialization
            app.UseOAuth2CodeRedeemer(
                new OAuth2CodeRedeemerOptions
                {
                    ClientId = clientId,
                    ClientSecret = appKey,
                    RedirectUri = redirectUri
                }
                );

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    // The `Authority` represents the v2.0 endpoint - https://login.microsoftonline.com/common/v2.0
                    // The `Scope` describes the initial permissions that your app will need.  See https://azure.microsoft.com/documentation/articles/active-directory-v2-scopes/
                    ClientId = clientId,
                    Authority = string.Format(CultureInfo.InvariantCulture, aadInstance, "common", "/v2.0"),
                    RedirectUri = redirectUri,
                    Scope = "openid profile offline_access Mail.Read",
                    PostLogoutRedirectUri = redirectUri,
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        // In a real application you would use IssuerValidator for additional checks, like making sure the user's organization has signed up for your app.
                        //     IssuerValidator = (issuer, token, tvp) =>
                        //     {
                        //        //if(MyCustomTenantValidation(issuer))
                        //        return issuer;
                        //        //else
                        //        //    throw new SecurityTokenInvalidIssuerException("Invalid issuer");
                        //    },
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthorizationCodeReceived = OnAuthorization,
                        AuthenticationFailed = OnAuthenticationFailed
                    }
                });
        }

        private async Task OnAuthorization(AuthorizationCodeReceivedNotification context)
        {
            IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithRedirectUri(redirectUri)
                .WithClientSecret(appKey)
                .Build();

            var code = context.Code;
            string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;
            new MSALSessionCache(cca.UserTokenCache, signedInUserID, context.OwinContext.Environment["System.Web.HttpContextBase"] as HttpContextBase);

            string[] scopes = { "Mail.Read" };

            try
            {
                AuthenticationResult result = await cca
                    .AcquireTokenByAuthorizationCode(scopes, code)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                context.Response.Write(ex.Message);
            }
        }

        private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.
            {
                notification.HandleResponse();
                notification.Response.Redirect("/Error?message=" + notification.Exception.Message);
                return Task.FromResult(0);
            }
        }
    }
}