---
services: active-directory
platforms: dotnet
author: dstrockis
---

# Integrate Microsoft identity and the Microsoft Graph into a web application using OpenID Connect

This sample showcases how to develop a web application that handles sign on via the unified Azure AD and MSA endpoint, so that users can sign in to the app using both their work/school account or Microsoft account. The application is implemented with ASP.NET MVC 4.6, while the web sign on functionality is implemented via ASP.NET OpenId Connect OWIN middleware.  
The sample also shows how to use MSAL (Microsoft Authentication Library) to obtain a token for invoking the Microsoft Graph. Specifically, the sample shows how to visualize the last email messages received by the signed in user, and how to send a mail message as the user. 
Finally, the sample showcases a new capability introduced by the new authentication endpoint - the ability for one app to ask for new permissions incrementally. 

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).
For more information about Microsoft Graph, please visit [the Microsoft Graph homepage](https://graph.microsoft.io/en-us/).

## How To Run This Sample

To run this sample you will need:
- Visual Studio 2017
- An Internet connection
- At least one of the following accounts:
- A Microsoft Account with access to an outlook.com enabled mailbox
- An Azure AD account with access to an Office 365 mailbox

You can get a Microsoft Account and outlook.com mailbox for free by choosing the Sign up option while visiting [https://www.microsoft.com/en-us/outlook-com/](https://www.microsoft.com/en-us/outlook-com/). 
You can get an Office365 office subscription, which will give you both an Azure AD account and a mailbox, at [https://products.office.com/en-us/try](https://products.office.com/en-us/try). 


### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/Azure-Samples/active-directory-dotnet-webapp-openidconnect-v2.git`

### Step 2:  Register the sample on the app registration portal

Create a new app at [apps.dev.microsoft.com](https://apps.dev.microsoft.com), or follow these [detailed steps](https://azure.microsoft.com/en-us/documentation/articles/active-directory-v2-app-registration/).  Make sure to:

- Copy down the **Application Id** assigned to your app, you'll need it soon.
- Add the **Web** platform for your app.
- Enter the correct **Redirect URI**. The redirect uri indicates to Azure AD where authentication responses should be directed - the default for this tutorial is `https://localhost:44326/`.
- Add a new **application secret** via the "Generate new password", and save the result in a temporary location - you'll need it in the next step.

### Step 3:  Configure the Visual Studio project with your app coordinates

1. Open the solution in Visual Studio 2015.
2. Open the `web.config` file.
3. Find the app key `ida:ClientSecret` and replace the value with the application secret you saved from step 2.
4. Find the app key `ida:ClientId` and replace the value with the Application ID from the app registration portal, again in Step 2.
5. If you changed the base URL of the sample, find the app key `ida:RedirectUri` and replace the value with the new base URL of the sample.

### Step 5:  Run the sample

Clean the solution, rebuild the solution, and run it.

Click the sign-in link on the homepage of the application to sign-in.  On the sign-in page, enter the name and password of a personal Microsoft account or a work/school account. The sample works exactly in the same way regardless of the account type you choose, apart from some visual differences in the authentication and consent experience. During the sign in process, you will be prompted to grant various permissions - including the ability for the app to read the user's email.   

> Remember, the account you choose must have access to an email inbox. If you are using an MSA and the email features don't work, your account might still not have been migrated to the new API. The fastest workaround is to create a new test *@outlook.com account, see the beginning of the readme for instructions. 

As you sign in, the app will change the sign in button into a greeting to the current user - and two new menu commands will appear: Read Mail and Send Mail.
Click on Read Mail: the app will show a dump of the last few messages from the current user's inbox, as they are received from the Microsoft Graph.
Click on Send Mail. As it is the first time you do so, you will receive a message informing you that for the app to receive the permissions to send mail as the user, the user needs to grant additional consent. The message offers a link to initiate the process. Click it, and you will be transported back to the consent experience - where you will be prompted to grant send mail permissions to the app.  
If you do so, you will be transported back to the application: but this time, you will be presented with a simple experience for authoring an email. Use it to compose and send an email to a mailbox you have access to. Send the message and verify you receive it correctly.
Hit the sign out link on the top right corner.
Sign in again with the same user, and follow the exact same steps described so far. You will notice that the send mail experience appears right away and no longer forces you to grant extra consent, as your decision has been recorded in your previous session. 

## Deploy this sample to Azure
Coming soon...
## About the code
Here there's a quick guide to the most interesting authentication related bits of the sample.
###Sign in 
As it is standard practice for ASP.NET MVC apps, the sign in functionality is implemented with the OpenID Connect OWIN middleware. Here there's a relevant snippet from the middleware initialization:  

```
app.UseOpenIdConnectAuthentication(
    new OpenIdConnectAuthenticationOptions
    {
        // The `Authority` represents the v2.0 endpoint - https://login.microsoftonline.com/common/v2.0
        ClientId = clientId,
        Authority = String.Format(CultureInfo.InvariantCulture, aadInstance, "common", "/v2.0"),
        RedirectUri = redirectUri,
        Scope = "openid email profile offline_access Mail.Read",
        PostLogoutRedirectUri = redirectUri,
        TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            // In a real application you would use IssuerValidator for additional checks, like making s
            // IssuerValidator = (issuer, token, tvp) =>
            // {
            ////if(MyCustomTenantValidation(issuer)) 
            //return issuer;
            ////else
            ////throw new SecurityTokenInvalidIssuerException("Invalid issuer");
        //},
},
```
Important things to notice:
- The Authority points to the new authentication endpoint which supports both personal and work&school accounts.
- the list of scopes includes both entries that are used for the sign in function (`openid, email, profile`) and for the token acquisition function (`offline_access` is required to obtain refresh_tokens as well; `Mail.Read` is required for getting access tokens that can be used when requesting tor ead the user's mail). 
- In this sample the issuer validation is turned off, which means that anybody with an account can access the application. Real life applications would likely be more restrictive, limiting access only to those Azure AD tenants or Microsoft accounts associated to customers of the application itself. In other words, real life applications would likely also have a sign up function - and the sign in would enforce that only the users who previously signed up have access. For simplicity, this sample does not include sign up features.     

###Initial token acquisition
This sample makes use of OpenId Connect hybrid flow, where at authentication time the app receives both sign in info (the id_token) and artifacts (in this case, an authorization code) that the app can use for obtaining an access token. That token can be used to access other resources - in this sample, the Microsoft Graph, for the purpose of reading the user's mailbox.
This sample shows how to use MSAL to redeem the authorization code into an access token, which is saved in a cache along with any other useful artifact (such as associated refresh_tokens) so that it can be used later on in the application.
The redemption takes place in the `AuthorizationCodeReceived` notification of the authorization middleware. Here there's the relevant code:

```
AuthorizationCodeReceived = async (context) =>
{
    var code = context.Code;
    string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;
    TokenCache userTokenCache = new MSALSessionCache(signedInUserID, 
        context.OwinContext.Environment["System.Web.HttpContextBase"] as HttpContextBase).GetMsalCacheInstance();    
    ConfidentialClientApplication cca =
        new ConfidentialClientApplication(clientId, redirectUri, new ClientCredential(appKey), userTokenCache,null);
    string[] scopes = { "Mail.Read" };
    try
    {
        AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(code, scopes);
    }

```

Important things to notice:
- The `ConfidentialClientApplication` is the primitive that MSAL uses to model the application.As such, it is initialized with the main application's coordinates.
- `MSALSessionCache` is a sample implementation of a custom MSAL token cache, which saves tokens in the current HTTP session. In a real-life application, you would likely want to save tokens in a long lived store instead, so that you don't need to retrieve new ones more often than necessary.
- The scope requested by `AcquireTokenByAuthorizationCodeAsync` is just the one required for invoking the API targeted by the application as part of its essential features. We'll see later that the app allows for extra scopes, but you can ignore those at this point. 

###Using access tokens in the app, handling token expiration
The `ReadMail` action in the `HomeController` class demonstrates how to take advantage of MSAL for getting access to protected API easily and securely. Here there's the relevant code:

```
 string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
 TokenCache userTokenCache = new MSALSessionCache(signedInUserID, this.HttpContext).GetMsalCacheInstance();

 ConfidentialClientApplication cca = 
     new ConfidentialClientApplication(clientId, redirectUri, new ClientCredential(appKey), userTokenCache, null);
 if (cca.Users.Count() > 0)
 {
     string[] scopes = { "Mail.Read" };
     AuthenticationResult result = await cca.AcquireTokenSilentAsync(scopes, cca.Users.First());

```
The idea is very simple. The code creates a new instance of `ConfidentialClientApplication` with the exact same coordinates as the ones used when redeeming the authorization code at authentication time.In particular, note that the exact same cache is used.
That done, all you need to do is to invoke `AcquireTokenSilentAsync`, asking for the scopes you need. MSAL will look up the cache and return any cached token which match with the requirement. If such access tokens are expired or no suitable access tokens are present, but there is an associated refresh token, MSAL will automatically use that to get a new access token and return it transparently.    

In the case in which refresh tokens are not present or they fail to obtain a new access token, MSAL will throw `MsalUiRequiredException`. That means that in order to obtain the requested token, the user must go through an interactive experience.
In the case of this sample, the Mail.Read permission is obtained as part of the login process - hence we need to trigger a new login; however we can't just redirect the user without warning, as it might be disorienting (what is happening, or why, would not be obvious to the user) and there might still be things they can do with the app that do not entail accessing mail. For that reason, the sample simply signals to the view to show a warning - and to offer a link to an action (`RefreshSession`) that the user can leverage for explicitly initiating the re-authentication process. 

###Handling incremental consent and OAuth2 code redemption 
The `SendMail` action demonstrates how to perform operations that require incremental consent. 
Observe the structure of the GET overload of that action. The code follows the same structure as the one you saw in `ReadMail`: the difference is in how `MsalUiRequiredException` is handled.
The application did not ask for Mail.Send during sign in, hence the failure to obtain a token silently could have been caused by the fact that the user did not yet granted consent for the app to use this permission. Instead of triggering a new sign in as we have done in `ReadMail`, here we can craft a specific authorization request for this permission. The call to the utility function `GenerateAuthorizationRequestUrl` does precisely that, leveraging MSAL to generate an OAuth2/OpenId Connect request for an authorization code for the Mail.Send permission.
That request, which is in fact a URL, is injected in the view as a hyperlink: once again, the user sees that link as part of a warning that the current operation requires leaving the app and going back to the authentication and consent pages.   
When the user clicks that link, they are brought through the authorization flow that eventually leads to the app receiving an authorization code that can be redeemed for an access token containing the scope requested. However, the standard collection of OWIN middlewares doesn't include anything that can be used for redeeming an authorization code for access and refresh tokens outside of a sign in flow.
This sample works around that limitation by providing a simple custom middleware, which takes care of intercepting messages containing authorization codes, validating them, redeeming the code and saving the resulting tokens in a MSAL cache, and finally redirecting to the URL that originated the request.
Back in Startup.Auth.cs, you can see the custom middleware initialization logic right between the cookie middleware and the OpenId Connect middleware. The position in the pipeline is very important, as in order to saver the tokens in the correct cache the custom middleware needs to know who the current user is.   

```
app.UseCookieAuthentication(new CookieAuthenticationOptions());

app.UseOAuth2CodeRedeemer(
    new OAuth2CodeRedeemerOptions
    {
       ClientId = clientId,
       ClientSecret = appKey,
       RedirectUri = redirectUri
    }
);

app.UseOpenIdConnectAuthentication(

```

Please note that the custom middleware is provided only as an example, and it has numerous limitations (like a hard dependency on `MSALSessionCache`) that limit its applicability outside of this scenario. 

## More information
For more information, please visit the [new documentation homepage for Microsoft identity](http://aka.ms/aaddevv2). 
