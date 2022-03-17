---
services: active-directory
platforms: dotnet
author: jmprieur
level: 400
client: ASP.NET Web App
service: Microsoft Graph
endpoint: Microsoft identity platform
page_type: sample
languages:
  - csharp  
products:
  - azure
  - azure-active-directory  
  - dotnet
  - office-ms-graph
description: "This sample showcases how to develop a ASP.NET MVC web application that handles sign using the Microsoft identity platform and ASP.NET OpenId Connect OWIN middleware."
---
# Use OpenID Connect to sign in users to Microsoft identity platform and execute Microsoft Graph operations using incremental consent

![Build Badge](https://identitydivision.visualstudio.com/_apis/public/build/definitions/a7934fdd-dcde-4492-a406-7fad6ac00e17/514/badge)

## About this sample

### Overview

This sample showcases how to develop a web application that handles sign using the Microsoft identity platform. It shows you how to use the new unified signing-in model that can be used to sign-in users to the app with both their [work/school account  (Azure AD account) or Microsoft account (MSA)](https://docs.microsoft.com/azure/active-directory/develop/azure-ad-endpoint-comparison). The application is implemented as an ASP.NET MVC project, while the web sign-on functionality is implemented via ASP.NET OpenId Connect OWIN middleware.

The sample shows how to use [MSAL.NET (Microsoft Authentication Library)](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) to obtain an access token for [Microsoft Graph](https://graph.microsoft.com). Specifically, the sample shows how to retrieve the last email messages received by the signed in user, and how to send a mail message as the user using Microsoft Graph.

The sample also showcases a new capability introduced by the new Microsoft identity platform - the ability for one app to seek consent for new permissions [incrementally](https://docs.microsoft.com/azure/active-directory/develop/azure-ad-endpoint-comparison#incremental-and-dynamic-consent).

Finally, the sample demonstrates how to use MSAL.js v2 and MSAL .Net together in a "hybrid" application that performs both server-side and client-side authenication. Some applications like SharePoint and OWA are built as "hybrid" web applications, which are built with server-side and client-side components (e.g. an ASP.net web application hosting a React single-page application). In these scenarios, the application will likely need authentication both client-side (e.g. a public client using MSAL.js) and server-side (e.g. a confidential client using MSAL.net ), and each application context will need to acquire its own tokens.

It shows how to use two new APIs, WithSpaAuthorizationCode on AcquireTokenByAuthorizationCode in MSAL .Net and acquireTokenByCode in MSAL.js v2, to authenticate a user server-side using a confidential client, and then SSO that user client-side using a second authorization code that is returned to the confidential client and redeemed by the public client client-side. This helps mitigate user experience and performance concerns that arise when performing server-side and client-side authentication for the same user, especially when third-party cookies are blocked by the browser.

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

For more information about Microsoft Graph, please visit the [Microsoft Graph homepage](https://graph.microsoft.io/).

## Topology

<img alt="Overview" src="./ReadmeFiles/Topology.png" style="width:70%, height:70%" width="70%" />

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

### Scenario

You sign in using your personal Microsoft Account in the app. During this flow, the app asks for consent to read your email only. Then, using this app, you can get the contents of your email's inbox using [Microsoft Graph Api](https://developer.microsoft.com/graph/docs/api-reference/v1.0/api/user_list_messages).

When you want to send an email, the app then proceeds to ask for an additional permission to send emails on your behalf. Once you provide that, it presents you with a screen using which you can send emails. The emails are sent using [Microsoft Graph API](https://developer.microsoft.com/graph/docs/api-reference/v1.0/api/message_send).

## How To Run This Sample

To run this sample, you'll need:

- [Visual Studio](https://aka.ms/vsdownload)
- An Internet connection
- At least one of the following accounts:
- A Microsoft Account with access to an outlook.com enabled mailbox
- An Azure AD account with access to an Office 365 mailbox

You can get a Microsoft Account and outlook.com mailbox for free by choosing the Sign-up option while visiting [https://www.microsoft.com/outlook-com/](https://www.microsoft.com/outlook-com/).
You can get an Office365 office subscription, which will give you both an Azure AD account and a mailbox, at [https://products.office.com/try](https://products.office.com/try).

### Step 1:  Clone or download this repository

From your shell or command line:

```Shell
git clone https://github.com/Azure-Samples/ms-identity-aspnet-webapp-openidconnect.git
```

or download and extract the repository .zip file.

> Given that the name of the sample is quiet long, and so are the names of the referenced NuGet packages, you might want to clone it in a folder close to the root of your hard drive, to avoid file size limitations on Windows.

### Step 2:  Register the sample application with your Azure Active Directory tenant

There is one project in this sample. To register it, you can:

- either follow the steps [Step 2: Register the sample with your Azure Active Directory tenant](#step-2-register-the-sample-with-your-azure-active-directory-tenant) and [Step 3:  Configure the sample to use your Azure AD tenant](#choose-the-azure-ad-tenant-where-you-want-to-create-your-applications)
- or use PowerShell scripts that:
  - **automatically** creates the Azure AD applications and related objects (passwords, permissions, dependencies) for you
  - modify the Visual Studio projects' configuration files.

If you want to use this automation:

1. On Windows run PowerShell and navigate to the root of the cloned directory
1. In PowerShell run:

   ```PowerShell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process -Force
   ```

1. Run the script to create your Azure AD application and configure the code of the sample application accordinly. 

   ```PowerShell
   .\AppCreationScripts\Configure.ps1
   ```

   > Remember to make the manual change in the manifest for the `signInAudience` as explained below.

   > Other ways of running the scripts are described in [App Creation Scripts](./AppCreationScripts/AppCreationScripts.md)

1. Open the Visual Studio solution and click start

If you don't want to use this automation, follow the steps below

#### Choose the Azure AD tenant where you want to create your applications

As a first step you'll need to:

1. Sign in to the [Azure portal](https://portal.azure.com) using either a work or school account or a personal Microsoft account.
1. If your account is present in more than one Azure AD tenant, select your profile at the top right corner in the menu on top of the page, and then **switch directory**.
   Change your portal session to the desired Azure AD tenant.

As a first step you'll need to:

1. Sign in to the [Azure portal](https://portal.azure.com) using either a work or school account or a personal Microsoft account.
1. If your account is present in more than one Azure AD tenant, select your profile at the top right corner in the menu on top of the page, and then **switch directory**.
   Change your portal session to the desired Azure AD tenant.

#### Register the service app (MailApp-openidconnect-v2)

1. Navigate to the Microsoft identity platform for developers [App registrations](https://go.microsoft.com/fwlink/?linkid=2083908) page.
1. Select **New registration**.
1. When the **Register an application page** appears, enter your application's registration information:
   - In the **Name** section, enter a meaningful application name that will be displayed to users of the app, for example `MailApp-openidconnect-v2`.
   - Change **Supported account types** to **Accounts in any organizational directory and personal Microsoft accounts (e.g. Skype, Xbox, Outlook.com)**.
   - In the Redirect URI (optional) section, select **Web** in the combo-box and enter the following redirect URIs: `https://localhost:44326/`.
1. Select **Register** to create the application.
1. On the app **Overview** page, find the **Application (client) ID** value and record it for later. You'll need it to configure the Visual Studio configuration file for this project.
1. From the app's Overview page, select the **Authentication** section.
   - In the **Advanced settings** | **Implicit grant** section, check **ID tokens** as this sample requires
     the [Implicit grant flow](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-implicit-grant-flow) to be enabled to
     sign-in the user, and call an API.
1. Select **Save**.
1. From the **Certificates & secrets** page, in the **Client secrets** section, choose **New client secret**:

   - Type a key description (of instance `app secret`),
   - Select a key duration of either **In 1 year**, **In 2 years**, or **Never Expires**.
   - When you press the **Add** button, the key value will be displayed, copy, and save the value in a safe location.
   - You'll need this key later to configure the project in Visual Studio. This key value will not be displayed again, nor retrievable by any other means,
     so record it as soon as it is visible from the Azure portal.
1. Select the **API permissions** section
   - Click the **Add a permission** button and then,
   - Ensure that the **Microsoft APIs** tab is selected
   - In the *Commonly used Microsoft APIs* section, click on **Microsoft Graph**
   - In the **Delegated permissions** section, ensure that the right permissions are checked: **openid**, **profile**, **offline_access**, **Mail.Read**, **User.Read**. Use the search box if necessary.
   - Select the **Add permissions** button

#### Change the application's manifest to enable both Work and School and Microsoft Accounts 

1. Select the **Manifest** section for your app.
1. Search for **signInAudience** and make sure it's set to **AzureADandPersonalMicrosoftAccount**

     ```JSON
          "signInUrl": null,
          "signInAudience": "AzureADandPersonalMicrosoftAccount",
     ```

1. Click **Save** to save the app manifest.

#### Configure the service project

> Note: if you used the setup scripts, the changes below will have been applied for you

1. Open the solution in Visual Studio.
1. Open the `web.config` file.
1. Find the app key `ida:ClientId` and replace the existing value with the application ID (clientId) of the `MailApp-openidconnect-v2` application copied from the Azure portal.
1. Find the app key `ida:ClientSecret` and replace the existing value with the key you saved during the creation of the `MailApp-openidconnect-v2` app, in the Azure portal.

### Step 5:  Run the sample

Clean the solution, rebuild the solution, and run it.

Once you run the `MailApp` web application, you are presented with the standard ASP.NET home page.
Click on the **Sign-in with Microsoft** link on top-right to trigger the log-in flow.
![Sign-in](./ReadmeFiles/Sign-in.JPG)

On the sign-in page, enter the name and password of a personal Microsoft account or a work/school account. The sample works exactly in the same way regardless of the account type you choose, apart from some visual differences in the authentication and consent experience. During the sign-in process, you will be prompted to grant various permissions - including the ability for the app to read the user's email.

![First Consent](./ReadmeFiles/FirstConsent.jpg)

> Remember, the account you choose must have access to an email inbox. If you are using a MSA and the email features don't work, your account might not have been migrated to the new API. The fastest workaround is to create a new test *@outlook.com account. Please refer to the beginning of this readme for instructions.

As you sign in, the app will change the sign-in button into a greeting to the current user - and two new menu commands will appear: `Read Mail` and `Send Mail`.

![Post sign-in](./ReadmeFiles/Postsign-in.JPG)

Click on **Read Mail**: the app will show a dump of the last few messages from the current user's inbox, as they are received from the Microsoft Graph.

Click on **View Profile**: the app will show the profile of the current user, as they are received from the Microsoft Graph.

> The sample redeems the Spa Auth Code from the initial token aquisition. You will need to sign-out and sign back in to request the SPA Auth Code.
> If you want to add more client side functionallity, please refer to the [MSAL JS Browser Sample for Hybrid SPA](https://github.com/AzureAD/microsoft-authentication-library-for-js/tree/dev/samples/msal-browser-samples/HybridSample)

Click on **Send Mail**. As it is the first time you do so, you will receive a message informing you that for the app to receive the permissions to send mail as the user, the user needs to grant additional consent. The message offers a link to initiate the process.

![Incremental Consent Link](./ReadmeFiles/IncrementalConsentLink.jpg)

Click it, and you will be transported back to the consent experience, this time it lists just one permission, which is **Send mail as you**.

![Incremental Consent prompt](./ReadmeFiles/Incrementalconsent.JPG)

Once you have consented to this permission, you will be transported back to the application: but this time, you will be presented with a simple experience for authoring an email. Use it to compose and send an email to a mailbox you have access to. Send the message and verify you receive it correctly.

Hit the **sign-out** link on the top right corner.

Sign in again with the same user, and follow the exact same steps described so far. You will notice that the send mail experience appears right away and no longer forces you to grant extra consent, as your decision has been recorded in your previous session.

> Did the sample not work for you as expected? Did you encounter issues trying this sample? Then please reach out to us using the [GitHub Issues](../issues) page.

## About the code

Here there's a quick guide to the most interesting authentication-related bits of the sample.

### Sign in

As it is standard practice for ASP.NET MVC apps, the sign-in functionality is implemented with the OpenID Connect OWIN middleware. Here there's a relevant snippet from the middleware initialization:

```CSharp
app.UseOpenIdConnectAuthentication(
    new OpenIdConnectAuthenticationOptions
    {
        // The `Authority` represents the v2.0 endpoint - https://login.microsoftonline.com/common/v2.0
        Authority = Globals.Authority,
        ClientId = Globals.ClientId,
        RedirectUri = Globals.RedirectUri,
        PostLogoutRedirectUri = Globals.RedirectUri,
        Scope = Globals.BasicSignInScopes + " Mail.Read User.Read", // a basic set of permissions for user sign in & profile access "openid profile offline_access"
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
            AuthorizationCodeReceived = OnAuthorizationCodeReceived,
            AuthenticationFailed = OnAuthenticationFailed,
        }
    });
```

Important things to notice:

- The Authority points to the new authentication endpoint, which supports both personal and work and school accounts.
- the list of scopes includes both entries that are used for the sign-in function (`openid, email, profile`) and for the token acquisition function (`offline_access` is required to obtain refresh_tokens as well; `Mail.Read` is required for getting access tokens that can be used when requesting to read the user's mail).
- In this sample, the issuer validation is turned off, which means that anybody with an account can access the application. Real life applications would likely be more restrictive, limiting access only to those Azure AD tenants or Microsoft accounts associated to customers of the application itself. In other words, real life applications would likely also have a sign-up function - and the sign-in would enforce that only the users who previously signed up have access. For simplicity, this sample does not include sign up features.

### Initial token acquisition

This sample makes use of OpenId Connect hybrid flow, where at authentication time the app receives both sign in info, the  [id_token](https://docs.microsoft.com/azure/active-directory/develop/id-tokens)  and artifacts (in this case, an  [authorization code](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow)) that the app can use for obtaining an [access token](https://docs.microsoft.com/azure/active-directory/develop/access-tokens). This access token can be used to access other resources - in this sample, the Microsoft Graph, for the purpose of reading the user's mailbox.

This sample shows how to use MSAL to redeem the authorization code into an access token, which is saved in a cache along with any other useful artifact (such as associated  [refresh_tokens](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow#refresh-the-access-token)) so that it can be used later on in the application from the controllers' actions to fetch access tokens after they are expired.

The redemption takes place in the `AuthorizationCodeReceived` notification of the authorization middleware. This is the section where the new MSAL.Net `WithSpaAuthorizationCode` API is used to get the `SpaAuthCode`. Here there's the relevant code:

```CSharp
        private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedNotification context)
        {
            // Upon successful sign in, get the access token & cache it using MSAL
            IConfidentialClientApplication clientApp = MsalAppBuilder.BuildConfidentialClientApplication();
            AuthenticationResult result = await clientApp.AcquireTokenByAuthorizationCode(new[] { "Mail.Read User.Read" }, context.Code)
                .WithSpaAuthorizationCode() //Request an authcode for the front end
                .ExecuteAsync();

            HttpContext.Current.Session.Add("Spa_Auth_Code", result.SpaAuthCode);
        }
```

Important things to notice:

- The  `IConfidentialClientApplication`  is the primitive that MSAL uses to model the Web application. As such, it is initialized with the main application's coordinates.
- The scope requested by  `AcquireTokenByAuthorizationCode`  is just the one required for invoking the API targeted by the application as part of its essential features. We'll see later that the app allows for extra scopes, but you can ignore those at this point.
- The instance of `IConfidentialClientApplication` is created and attached to an instance of `MSALPerUserMemoryTokenCache`, which is a custom cache implementation that uses a shared instance of a [MemoryCache](https://docs.microsoft.com/dotnet/api/system.runtime.caching.memorycache?view=netframework-4.8) to cache tokens. When it acquires the access token, MSAL also saves this token in its token cache. When any code in the rest of the project tries to acquire an access token for Microsoft Graph with the same scope (Mail.Read), MSAL will return the cached token.

The IConfidentialClientApplication is created in a separate function in the `MsalAppBuilder` class.

```Csharp
        public static IConfidentialClientApplication BuildConfidentialClientApplication(ClaimsPrincipal currentUser)
        {
            IConfidentialClientApplication clientapp = ConfidentialClientApplicationBuilder.Create(Globals.ClientId)
                  .WithClientSecret(Globals.ClientSecret)
                  .WithRedirectUri(Globals.RedirectUri)
                  .WithAuthority(new Uri(Globals.Authority))
                  .Build();

            MSALPerUserMemoryTokenCache userTokenCache = new MSALPerUserMemoryTokenCache(clientapp.UserTokenCache, currentUser ?? ClaimsPrincipal.Current);
            return clientapp;
        }
```

Important things to notice:

- The method builds an instance of the IConfidentialClientApplication using the new [builder pattern introduced by MSAL v3.X](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-Applications).

- `MSALPerUserMemoryTokenCache` is a sample implementation of a custom MSAL token cache, which saves tokens in a [MemoryCache](https://docs.microsoft.com/dotnet/framework/performance/caching-in-net-framework-applications) instance shared across the web app. In a real-life application, you would likely want to save tokens in a long lived store instead, so that you don't need to retrieve new ones more often than necessary.

### Using access tokens in the app, handling token expiration

The `ReadMail` action in the `HomeController` class demonstrates how to take advantage of MSAL for getting access to protected API easily and securely. It also introduces you to the recommended [token acquisition pattern](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token) where you should first attempt to seek an access token in the cache.

Here is the relevant code:

```CSharp
    IConfidentialClientApplication app = MsalAppBuilder.BuildConfidentialClientApplication();
    AuthenticationResult result = null;
    var accounts = await app.GetAccountsAsync();
    string[] scopes = { "Mail.Read" };

    try
    {
        // try to get token silently
        result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync().ConfigureAwait(false);
    }
    catch (MsalUiRequiredException)
    {
        ViewBag.Relogin = "true";
        return View();
    }
    catch (Exception eee)
    {
        ViewBag.Error = "An error has occurred. Details: " + eee.Message;
        return View();
    }

    if (result != null)
    {
        // Use the token to read email
        HttpClient hc = new HttpClient();
        hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);
        HttpResponseMessage hrm = await hc.GetAsync("https://graph.microsoft.com/v1.0/me/messages");

        string rez = await hrm.Content.ReadAsStringAsync();
        ViewBag.Message = rez;
    }

    return View();
}
```

The idea is simple. The code creates a new instance of `IConfidentialClientApplication` with the exact same coordinates as the ones used when redeeming the authorization code at authentication time. In particular, note that the exact same cache is used.
That done, all you need to do is to invoke `AcquireTokenSilent`, asking for the scopes you need. MSAL will look up the cache and return any cached token, which matches with the requirement. If such access tokens are expired or no suitable access tokens are present, but there is an associated refresh token, MSAL will automatically use that to get a new access token and return it transparently.

In the case in which refresh tokens are not present or they fail to obtain a new access token, MSAL will throw `MsalUiRequiredException`. That means that in order to obtain the requested token, the user must go through an interactive sign-in experience.

In the case of this sample, the `Mail.Read` permission is obtained as part of the login process - hence we need to trigger a new login; however we can't just redirect the user without warning, as it might be disorienting (what is happening, or why, would not be obvious to the user) and there might still be things they can do with the app that do not entail accessing mail. For that reason, the sample simply signals to the view to show a warning - and to offer a link to an action (`RefreshSession`) that the user can leverage for explicitly initiating the re-authentication process.


### Using Spa Auth Code in the Front End

First, configure a new PublicClientApplication from MSAL.js in your single-page application:

```JS
const msalInstance = new msal.PublicClientApplication({
    auth: {
        clientId: "Enter the Client ID from the Web.Config file",
        redirectUri: "https://localhost:44326/",
        authority: "https://login.microsoftonline.com/organizations/"
    }
})
```

Next, render the code that was acquired server-side, and provide it to the acquireTokenByCode API on the MSAL.js PublicClientApplication instance. Be sure to not include any additional scopes that were not included in the first login request, otherwise the user may be prompted for consent.

```js
    var code = spaCode;
    const scopes = ["user.read"];

    console.log('MSAL: acquireTokenByCode hybrid parameters present');

    var authResult = msalInstance.acquireTokenByCode({
        code,
        scopes
    })
```

Once the Access Token is retrieved using the new MSAL.js `acquireTokenByCode` api, the token is then used to read the user's profile 

```js
function callMSGraph(endpoint, token, callback) {
    const headers = new Headers();
    const bearer = `Bearer ${token}`;
    headers.append("Authorization", bearer);

    const options = {
        method: "GET",
        headers: headers
    };

    console.log('request made to Graph API at: ' + new Date().toString());

    fetch(endpoint, options)
        .then(response => response.json())
        .then(response => callback(response, endpoint))
        .then(result => {
            console.log('Successfully Fetched Data from Graph API:', result);
        })
        .catch(error => console.log(error))
}
```

### Handling incremental consent and OAuth2 code redemption

The `SendMail` action demonstrates how to perform operations that require incremental consent.
Observe the structure of the GET overload of that action. The code follows the same structure as the one you saw in `ReadMail`: the difference is in how `MsalUiRequiredException` is handled.
The application did not ask for `Mail.Send` during sign-in, hence the failure to obtain a token silently could have been caused by the fact that the user did not yet grant consent for the app to use this permission. Instead of triggering a new sign-in as we have done in `ReadMail`, here we can craft a specific authorization request for this permission. The call to the utility function `GenerateAuthorizationRequestUrl` does precisely that, leveraging MSAL to generate an OAuth2/OpenId Connect request for an authorization code for the Mail.Send permission.
That request, which is in fact a URL, is injected in the view as a hyperlink: once again, the user sees that link as part of a warning that the current operation requires leaving the app and going back to the authentication and consent pages.
When the user clicks that link, they are brought through the authorization flow that eventually leads to the app receiving an authorization code that can be redeemed for an access token containing the scope requested. However, the standard collection of OWIN middleware doesn't include anything that can be used for redeeming an authorization code for access and refresh tokens outside of a sign-in flow.
This sample works around that limitation by providing a simple custom middleware, **which takes care of intercepting messages containing authorization codes, validating them, redeeming the code and saving the resulting tokens in an MSAL cache, and finally redirecting to the URL that originated the request.**

Back in Startup.Auth.cs, you can see the custom middleware initialization logic right between the cookie middleware and the OpenId Connect middleware. **The position in the pipeline is important**, as in order to save the tokens in the correct cache the custom middleware needs to know who the current user is.

```CSharp
    app.UseCookieAuthentication(new CookieAuthenticationOptions());

    app.UseOAuth2CodeRedeemer(
        new OAuth2CodeRedeemerOptions
        {
            ClientId = Globals.ClientId,
            ClientSecret = Globals.ClientSecret,
            RedirectUri = Globals.RedirectUri
        }
        );

app.UseOpenIdConnectAuthentication(

```

Note that the custom middleware is provided only as an example, and it has numerous limitations (like a hard dependency on `MSALPerUserMemoryTokenCache`) that limit its applicability outside of this scenario.

## How to deploy this sample to Azure

This project has one WebApp / Web API projects. To deploy them to Azure Web Sites, you'll need, for each one, to:

- create an Azure Web Site
- publish the Web App / Web APIs to the web site, and
- update its client(s) to call the web site instead of IIS Express.

### Create and publish the `openidconnect-v2` to an Azure Web Site

1. Sign in to the [Azure portal](https://portal.azure.com).
1. Click `Create a resource` in the top left-hand corner, select **Web** --> **Web App**, and give your web site a name, for example, `openidconnect-v2-contoso.azurewebsites.net`.
1. Thereafter select the `Subscription`, `Resource Group`, `App service plan and Location`. `OS` will be **Windows** and `Publish` will be **Code**.
1. Click `Create` and wait for the App Service to be created.
1. Once you get the `Deployment succeeded` notification, then click on `Go to resource` to navigate to the newly created App service.
1. Once the web site is created, locate it it in the **Dashboard** and click it to open **App Services** **Overview** screen.
1. From the **Overview** tab of the App Service, download the publish profile by clicking the **Get publish profile** link and save it.  Other deployment mechanisms, such as from source control, can also be used.
1. Switch to Visual Studio and go to the openidconnect-v2 project.  Right click on the project in the Solution Explorer and select **Publish**.  Click **Import Profile** on the bottom bar, and import the publish profile that you downloaded earlier.
1. Click on **Configure** and in the `Connection tab`, update the Destination URL so that it is a `https` in the home page url, for example [https://openidconnect-v2-contoso.azurewebsites.net](https://openidconnect-v2-contoso.azurewebsites.net). Click **Next**.
1. On the Settings tab, make sure `Enable Organizational Authentication` is NOT selected.  Click **Save**. Click on **Publish** on the main screen.
1. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.

### Update the Active Directory tenant application registration for `openidconnect-v2`

1. Navigate back to to the [Azure portal](https://portal.azure.com).
In the left-hand navigation pane, select the **Azure Active Directory** service, and then select **App registrations (Preview)**.
1. In the resultant screen, select the `openidconnect-v2` application.
1. From the *Branding* menu, update the **Home page URL**, to the address of your service, for example [https://openidconnect-v2-contoso.azurewebsites.net](https://openidconnect-v2-contoso.azurewebsites.net). Save the configuration.
1. Add the same URL in the list of values of the *Authentication -> Redirect URIs* menu. If you have multiple redirect urls, make sure that there a new entry using the App service's Uri for each redirect url.

## Community Help and Support

Use [Stack Overflow](http://stackoverflow.com/questions/tagged/msal) to get support from the community.
Ask your questions on Stack Overflow first and browse existing issues to see if someone has asked your question before.
Make sure that your questions or comments are tagged with [`msal` `dotnet` `microsoft-graph`].

If you find a bug in the sample, please raise the issue on [GitHub Issues](../../issues).

To provide a recommendation, visit the following [User Voice page](https://feedback.azure.com/forums/169401-azure-active-directory).

## Contributing

If you'd like to contribute to this sample, see [CONTRIBUTING.MD](/CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## More information

For more information, visit the following links:

- [Add sign-in with Microsoft to an ASP.NET web app (V2 endpoint)](https://docs.microsoft.com/azure/active-directory/develop/guidedsetups/active-directory-aspnetwebapp) explains how to re-create the sign-in part of this sample from scratch.
- To learn more about the code, visit [Conceptual documentation for MSAL.NET](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki#conceptual-documentation) and in particular:

  - [Acquiring tokens with authorization codes on web apps](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Acquiring-tokens-with-authorization-codes-on-web-apps)
  - [Customizing Token cache serialization](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/token-cache-serialization)
  - [Acquiring a token on behalf of a user Service to Services calls](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/on-behalf-of) 

- Articles about the Azure AD V2 endpoint [http://aka.ms/aaddevv2](http://aka.ms/aaddevv2), with a focus on:

  - [Azure Active Directory v2.0 and OAuth 2.0 On-Behalf-Of flow](https://docs.microsoft.com/azure/active-directory/develop/active-directory-v2-protocols-oauth-on-behalf-of)
  - [Incremental and dynamic consent](https://docs.microsoft.com/azure/active-directory/develop/active-directory-v2-compare#incremental-and-dynamic-consent)

- Articles about the Microsoft Graph
  - [Overview of Microsoft Graph](https://developer.microsoft.com/graph/docs/concepts/overview)
  - [Get access tokens to call Microsoft Graph](https://developer.microsoft.com/graph/docs/concepts/auth_overview)
  - [Use the Microsoft Graph API](https://developer.microsoft.com/graph/docs/concepts/use_the_api)
