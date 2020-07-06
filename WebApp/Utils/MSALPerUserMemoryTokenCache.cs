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

using Microsoft.Identity.Client;
using System;
using System.Runtime.Caching;
using System.Security.Claims;

namespace WebApp.Utils
{
    /// <summary>
    /// An implementation of token cache for both Confidential and Public clients backed by MemoryCache.
    /// MemoryCache is useful in Api scenarios where there is no HttpContext to cache data.
    /// </summary>
    /// <seealso cref="https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/token-cache-serialization"/>
    public class MSALPerUserMemoryTokenCache
    {
        /// <summary>
        /// The backing MemoryCache instance
        /// </summary>
        internal readonly MemoryCache memoryCache = MemoryCache.Default;

        /// <summary>
        /// The duration till the tokens are kept in memory cache. In production, a higher value, upto 90 days is recommended.
        /// </summary>
        private readonly DateTimeOffset cacheDuration = DateTimeOffset.Now.AddHours(48);

        /// <summary>
        /// Initializes a new instance of the <see cref="MSALPerUserMemoryTokenCache"/> class.
        /// </summary>
        /// <param name="tokenCache">The client's instance of the token cache.</param>
        public MSALPerUserMemoryTokenCache(ITokenCache tokenCache)
        {
            Initialize(tokenCache);
        }

        /// <summary>Initializes the cache instance</summary>
        /// <param name="tokenCache">The ITokenCache passed through the constructor</param>
        /// <param name="user">The signed-in user for whom the cache needs to be established..</param>
        private void Initialize(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(UserTokenCacheBeforeAccessNotification);
            tokenCache.SetAfterAccess(UserTokenCacheAfterAccessNotification);
            tokenCache.SetBeforeWrite(UserTokenCacheBeforeWriteNotification);
        }

        /// <summary>
        /// Clears the TokenCache's copy of this user's cache.
        /// </summary>
        public void Clear(string key)
        {
            memoryCache.Remove(key);
        }

        /// <summary>
        /// Triggered right after MSAL accessed the cache.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void UserTokenCacheAfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                string cacheKey = args.SuggestedCacheKey ?? args.Account?.HomeAccountId?.Identifier;

                if (args.HasTokens)
                {

                    if (string.IsNullOrWhiteSpace(cacheKey))
                        return;

                    // Ideally, methods that load and persist should be thread safe.MemoryCache.Get() is thread safe.
                    this.memoryCache.Set(cacheKey, args.TokenCache.SerializeMsalV3(), cacheDuration);
                }
                else
                {
                    this.memoryCache.Remove(cacheKey);
                }
            }
        }

        /// <summary>
        /// Triggered right before MSAL needs to access the cache. Reload the cache from the persistence store in case it changed since the last access.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void UserTokenCacheBeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            string cacheKey = args.SuggestedCacheKey ?? args.Account?.HomeAccountId?.Identifier;
            if (string.IsNullOrEmpty(cacheKey))
            {
                return;
            }

            byte[] tokenCacheBytes = (byte[])this.memoryCache.Get(cacheKey);
            args.TokenCache.DeserializeMsalV3(tokenCacheBytes, shouldClearExistingCache: true);
        }

        /// <summary>
        /// if you want to ensure that no concurrent write take place, use this notification to place a lock on the entry
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void UserTokenCacheBeforeWriteNotification(TokenCacheNotificationArgs args)
        {
            // Since we are using a MemoryCache ,whose methods are threads safe, we need not to do anything in this handler.
        }
    }
}