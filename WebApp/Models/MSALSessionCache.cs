using Microsoft.Identity.Client;
using System.Threading;
using System.Web;

namespace WebApp_OpenIDConnect_DotNet.Models
{
    public class MSALSessionCache
    {
        private static readonly ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly string UserId = string.Empty;
        private readonly string CacheId = string.Empty;
        private readonly HttpContextBase httpContext = null;
        private readonly ITokenCache cache = null;

        public MSALSessionCache(ITokenCache tokenCache, string userId, HttpContextBase httpcontext)
        {
            // not object, we want the SUB
            cache = tokenCache;
            UserId = userId;
            CacheId = UserId + "_TokenCache";
            httpContext = httpcontext;

            cache.SetBeforeAccess(BeforeAccessNotification);
            cache.SetAfterAccess(AfterAccessNotification);
            Load();
        }

        public void SaveUserStateValue(string state)
        {
            SessionLock.EnterWriteLock();
            httpContext.Session[CacheId + "_state"] = state;
            SessionLock.ExitWriteLock();
        }

        public string ReadUserStateValue()
        {
            string state = string.Empty;
            SessionLock.EnterReadLock();
            state = (string)httpContext.Session[CacheId + "_state"];
            SessionLock.ExitReadLock();
            return state;
        }

        public void Load()
        {
            SessionLock.EnterReadLock();
            byte[] blob = (byte[])httpContext.Session[CacheId];
            if (blob != null)
            {
                cache.DeserializeMsalV3(blob);
            }
            SessionLock.ExitReadLock();
        }

        public void Persist()
        {
            SessionLock.EnterWriteLock();

            // Reflect changes in the persistent store
            httpContext.Session[CacheId] = cache.SerializeMsalV3();
            SessionLock.ExitWriteLock();
        }

        // Triggered right before MSAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered right after MSAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                Persist();
            }
        }
    }
}
