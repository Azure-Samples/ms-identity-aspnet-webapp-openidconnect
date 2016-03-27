using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace WebApp_OpenIDConnect_DotNet.Models
{
    public class MSALSessionCache : TokenCache
    {
        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        string UserId = string.Empty;
        string CacheId = string.Empty;
        HttpContextBase httpContext = null;


        public MSALSessionCache(string userId, HttpContextBase httpcontext)
        {
            // not object, we want the SUB
            UserId = userId;
            CacheId = UserId + "_TokenCache";
            httpContext = httpcontext;
            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            Load();
        }

        public void SaveUserStateValue(string state)
        {
            SessionLock.EnterWriteLock();
            httpContext.Session[CacheId+"_state"] = state;
            SessionLock.ExitWriteLock();
        }
        public string ReadUserStateValue()
        {
            string state = string.Empty;
            SessionLock.EnterReadLock();
            //this.Deserialize((byte[])HttpContext.Current.Session[CacheId]);
            state = (string) httpContext.Session[CacheId + "_state"];
            SessionLock.ExitReadLock();
            return state;
        }
        public void Load()
        {
            SessionLock.EnterReadLock();
            //this.Deserialize((byte[])HttpContext.Current.Session[CacheId]);
            this.Deserialize((byte[])httpContext.Session[CacheId]);
            SessionLock.ExitReadLock();
        }

        public void Persist()
        {
            SessionLock.EnterWriteLock();

            // Optimistically set HasStateChanged to false. We need to do it early to avoid losing changes made by a concurrent thread.
            this.HasStateChanged = false;

            // Reflect changes in the persistent store
            httpContext.Session[CacheId] = this.Serialize();
            SessionLock.ExitWriteLock();
        }

        // Empties the persistent store.
        public override void Clear(string cliendId)
        {
            base.Clear(cliendId);
            httpContext.Session.Remove(CacheId);
        }

        // Triggered right before ADAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered right after ADAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (this.HasStateChanged)
            {
                Persist();
            }
        }
    }
}
