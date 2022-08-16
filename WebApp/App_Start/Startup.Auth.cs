using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApp.Utils;
using System.Web;

namespace WebApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // Custom middleware initialization. This is activated when the code obtained from a code_grant is present in the querystring (&code=<code>).
            app.UseOAuth2CodeRedeemer(
                new OAuth2CodeRedeemerOptions
                {
                    ClientId = AuthenticationConfig.ClientId,
                    ClientSecret = AuthenticationConfig.ClientSecret,
                    RedirectUri = AuthenticationConfig.RedirectUri
                });

            app.UseOpenIdConnectAuthentication(
                
                new OpenIdConnectAuthenticationOptions
                {
                    // This is needed for PKCE and resposne type must be set to 'code'
                    UsePkce = true,
                    ResponseType = OpenIdConnectResponseType.Code,

                    // The `Authority` represents the v2.0 endpoint - https://login.microsoftonline.com/common/v2.0
                    Authority = AuthenticationConfig.Authority,
                    ClientId = AuthenticationConfig.ClientId,
                    RedirectUri = AuthenticationConfig.RedirectUri,
                    PostLogoutRedirectUri = AuthenticationConfig.RedirectUri,
                    Scope = AuthenticationConfig.BasicSignInScopes + " Mail.Read User.Read", // a basic set of permissions for user sign in & profile access "openid profile offline_access"
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
                        //NameClaimType = "name",
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        // When we get an auth_code, use MSAL to fetch tokens and to cache them
                        AuthorizationCodeReceived = OnAuthorizationCodeReceived,
                        AuthenticationFailed = OnAuthenticationFailed,      
                        
                        // Before making the /authorize call, make sure to request client_info for MSAL caching
                        RedirectToIdentityProvider = OnRedirectToIdentityProvider,

                        // At this point we'll have an id_token / ClaimsPrincipal which we'll enhance with 2 extra claims: home object id and home tenant id. These are used by MSAL for caching.
                        SecurityTokenValidated = OnSecurityTokenValidated,                              
                    },
                    // Handling SameSite cookie according to https://docs.microsoft.com/en-us/aspnet/samesite/owin-samesite
                    CookieManager = new SameSiteCookieManager(
                                     new SystemWebCookieManager())
                });
        }


        /// <summary>
        /// When OWIN has the id_token and creates the ClaimsPrincipal from it, enhance it with 2 extra claims: home object id and home tenant id. These are used by MSAL for caching.
        /// </summary>
        private Task OnSecurityTokenValidated(SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            string clientInfo = HttpContext.Current.Session[ClientInfo.ClientInfoParamName]?.ToString();


            if (!string.IsNullOrEmpty(clientInfo))
            {
                ClientInfo clientInfoFromServer = ClientInfo.CreateFromJson(clientInfo);

                if (clientInfoFromServer != null && clientInfoFromServer.UniqueTenantIdentifier != null && clientInfoFromServer.UniqueObjectIdentifier != null)
                {
                    context.AuthenticationTicket.Identity.AddClaim(new Claim(ClientInfo.UniqueTenantIdentifierName, clientInfoFromServer.UniqueTenantIdentifier));
                    context.AuthenticationTicket.Identity.AddClaim(new Claim(ClientInfo.UniqueObjectIdentifierName, clientInfoFromServer.UniqueObjectIdentifier));
                }

                HttpContext.Current.Session.Remove(ClientInfo.ClientInfoParamName);
            }

            return Task.CompletedTask;
        }


        private  Task OnRedirectToIdentityProvider(RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> arg)
        {
            // client_info ensures that the response will contain a base64 encoded 
            arg.ProtocolMessage.SetParameter("client_info", "1");
            return Task.CompletedTask;
        }

        private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification context)
        {
            context.TokenEndpointRequest.Parameters.TryGetValue("code_verifier", out var codeVerifier);

            // Upon successful sign in, get the access token & cache it using MSAL
            IConfidentialClientApplication clientApp = MsalAppBuilder.BuildConfidentialClientApplication();
            AuthenticationResult result = await clientApp.AcquireTokenByAuthorizationCode(new[] { "Mail.Read User.Read" }, context.Code)
                .WithSpaAuthorizationCode() //Optional: Request an authorization code for the front end - this is faster than having front end get tokens on its own
                .WithPkceCodeVerifier(codeVerifier) // Code verifier for PKCE
                .ExecuteAsync();

            HttpContext.Current.Session.Add("Spa_Auth_Code", result.SpaAuthCode);

            // This continues the authentication flow using the access token and id token retrieved by the clientApp object after
            // redeeming an access token using the access code.
            //
            // This is needed to ensure the middleware does not try and redeem the received access code a second time.

            // persist the client_info until we have the ClaimsPrincipal, i.e. until OnSecurityTokenValidated is invoked
            HttpContext.Current.Session.Add("client_info", context.ProtocolMessage.GetParameter(ClientInfo.ClientInfoParamName));

            context.HandleCodeRedemption(null, result.IdToken);
        }

        private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            notification.HandleResponse();
            notification.Response.Redirect("/Error?message=" + notification.Exception.Message);
            return Task.FromResult(0);
        }
    }
}