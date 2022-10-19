using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Owin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.IdentityModel.Validators;

namespace WebApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.AddMicrosoftIdentityWebApp(configureServices: services =>
            {
                services.Configure<ConfidentialClientApplicationOptions>(options => { options.RedirectUri = "https://localhost:44326/"; });
                services.AddMicrosoftGraph();
                services.AddInMemoryTokenCaches();
            },
            updateOptions: options=>
            {
                options.TokenValidationParameters.IssuerValidator = AadIssuerValidator.GetAadIssuerValidator(options.Authority).Validate;
            });

        }
    }
}