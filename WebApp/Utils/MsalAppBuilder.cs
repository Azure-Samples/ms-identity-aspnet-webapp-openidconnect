/************************************************************************************************
The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
***********************************************************************************************/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApp.Utils
{
    public static class MsalAppBuilder
    {
        private static IConfidentialClientApplication clientapp;

        public static IConfidentialClientApplication BuildConfidentialClientApplication()
        {
            if (clientapp == null)
            {
                clientapp = ConfidentialClientApplicationBuilder.Create(AuthenticationConfig.ClientId)
                      .WithClientSecret(AuthenticationConfig.ClientSecret)
                      .WithRedirectUri(AuthenticationConfig.RedirectUri)
                      .WithAuthority(new Uri(AuthenticationConfig.Authority))
                      .Build();

                clientapp.AddDistributedTokenCache(services =>
                {
                    // Do not use DistributedMemoryCache in production!
                    // This is a memory cache which is not distributed and is not persisted.
                    // It's useful for testing and samples, but in production use a durable distributed cache,
                    // such as Redis.
                    services.AddDistributedMemoryCache();

                    // The setting below shows encryption which works on a single machine. 
                    // In a distributed system, the encryption keys must be shared between all machines
                    // For details see https://github.com/AzureAD/microsoft-identity-web/wiki/L1-Cache-in-Distributed-(L2)-Token-Cache#distributed-systems
                    services.Configure<MsalDistributedTokenCacheAdapterOptions>(o =>
                    {
                        o.Encrypt = true;
                    });
                });
/*
                // Could also use other forms of cache, like Redis
                // See https://aka.ms/ms-id-web/token-cache-serialization
                clientapp.AddDistributedTokenCache(services =>
                {
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = "localhost";
                        options.InstanceName = "SampleInstance";
                    });
                });
*/
            }
            return clientapp;
        }

        public static async Task RemoveAccount()
        {
            BuildConfidentialClientApplication();

            var userAccount = await clientapp.GetAccountAsync(ClaimsPrincipal.Current.GetMsalAccountId());
            if (userAccount != null)
            {
                await clientapp.RemoveAsync(userAccount);
            }
        }
    }
}