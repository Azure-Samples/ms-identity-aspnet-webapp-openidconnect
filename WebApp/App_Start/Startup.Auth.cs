using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.OWIN;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Validators;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;

namespace WebApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // Get an TokenAcquirerFactory specialized for OWIN
            OwinTokenAcquirerFactory owinTokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance<OwinTokenAcquirerFactory>();

            // Configure the web app.
            app.AddMicrosoftIdentityWebApp(owinTokenAcquirerFactory,
            updateOptions: options =>
            {
                options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(options.Authority).Validate;
            });

            // Add the services you need.
            owinTokenAcquirerFactory.Services
                 .Configure<ConfidentialClientApplicationOptions>(options => { options.RedirectUri = "https://localhost:44326/"; })
                .AddMicrosoftGraph()
                .AddInMemoryTokenCaches();

            owinTokenAcquirerFactory.Build();

              
        }
    }
}