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
        /// The internal handle to the client's instance of the Cache
        /// </summary>
        private ITokenCache UserTokenCache;

        /// <summary>
        /// Once the user signes in, this will not be null and can be obtained via a call to Thread.CurrentPrincipal
        /// </summary>
        internal ClaimsPrincipal SignedInUser;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSALPerUserMemoryTokenCache"/> class.
        /// </summary>
        /// <param name="tokenCache">The client's instance of the token cache.</param>
        public MSALPerUserMemoryTokenCache(ITokenCache tokenCache)
        {
            Initialize(tokenCache, ClaimsPrincipal.Current);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSALPerUserMemoryTokenCache"/> class.
        /// </summary>
        /// <param name="tokenCache">The client's instance of the token cache.</param>
        /// <param name="user">The signed-in user for whom the cache needs to be established.</param>
        public MSALPerUserMemoryTokenCache(ITokenCache tokenCache, ClaimsPrincipal user)
        {
            Initialize(tokenCache, user);
        }
        
        /// <summary>Initializes the cache instance</summary>
        /// <param name="tokenCache">The ITokenCache passed through the constructor</param>
        /// <param name="user">The signed-in user for whom the cache needs to be established..</param>
        private void Initialize(ITokenCache tokenCache, ClaimsPrincipal user)
        {
            SignedInUser = user;

            UserTokenCache = tokenCache;
            UserTokenCache.SetBeforeAccess(UserTokenCacheBeforeAccessNotification);
            UserTokenCache.SetAfterAccess(UserTokenCacheAfterAccessNotification);
            UserTokenCache.SetBeforeWrite(UserTokenCacheBeforeWriteNotification);

            if (SignedInUser == null)
            {
                // No users signed in yet, so we return
                return;
            }

            LoadUserTokenCacheFromMemory();
        }

        /// <summary>
        /// Explores the Claims of a signed-in user (if available) to populate the unique Id of this cache's instance.
        /// </summary>
        /// <returns>The signed in user's object.tenant Id , if available in the ClaimsPrincipal.Current instance</returns>
        internal string GetMsalAccountId()
        {
            if (SignedInUser != null)
            {
                return SignedInUser.GetMsalAccountId();
            }
            return null;
        }

        /// <summary>
        /// Loads the user token cache from memory.
        /// </summary>
        private void LoadUserTokenCacheFromMemory()
        {
            string cacheKey = GetMsalAccountId();

            if (string.IsNullOrWhiteSpace(cacheKey))
                return;

            // Ideally, methods that load and persist should be thread safe. MemoryCache.Get() is thread safe.
            byte[] tokenCacheBytes = (byte[])memoryCache.Get(GetMsalAccountId());
            UserTokenCache.DeserializeMsalV3(tokenCacheBytes);
        }

        /// <summary>
        /// Persists the user token blob to the memoryCache.
        /// </summary>
        private void PersistUserTokenCache()
        {
            string cacheKey = GetMsalAccountId();

            if (string.IsNullOrWhiteSpace(cacheKey))
                return;

            // Ideally, methods that load and persist should be thread safe.MemoryCache.Get() is thread safe.
            memoryCache.Set(GetMsalAccountId(), UserTokenCache.SerializeMsalV3(), cacheDuration);
        }

        /// <summary>
        /// Clears the TokenCache's copy of this user's cache.
        /// </summary>
        public void Clear()
        {
            memoryCache.Remove(GetMsalAccountId());

            // Nulls the currently deserialized instance
            LoadUserTokenCacheFromMemory();
        }

        /// <summary>
        /// Triggered right after MSAL accessed the cache.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void UserTokenCacheAfterAccessNotification(TokenCacheNotificationArgs args)
        {
            SetSignedInUserFromNotificationArgs(args);

            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                PersistUserTokenCache();
            }
        }

        /// <summary>
        /// Triggered right before MSAL needs to access the cache. Reload the cache from the persistence store in case it changed since the last access.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void UserTokenCacheBeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            LoadUserTokenCacheFromMemory();
        }

        /// <summary>
        /// if you want to ensure that no concurrent write take place, use this notification to place a lock on the entry
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void UserTokenCacheBeforeWriteNotification(TokenCacheNotificationArgs args)
        {
            // Since we are using a MemoryCache ,whose methods are threads safe, we need not to do anything in this handler.
        }

        /// <summary>
        /// To keep the cache, ClaimsPrincipal and Sql in sync, we ensure that the user's object Id we obtained by MSAL after
        /// successful sign-in is set as the key for the cache.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void SetSignedInUserFromNotificationArgs(TokenCacheNotificationArgs args)
        {
            if (SignedInUser == null && args.Account != null)
            {
                SignedInUser = args.Account.ToClaimsPrincipal();
            }
        }
    }
}