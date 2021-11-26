using System.IO;
using Microsoft.Identity.Client;

//No idea whats really going on here I just got it from the microsoft docs
namespace TaskToEvent {
    static class TokenCacheHelper {
        public static void EnableSerialization(ITokenCache tokenCache) {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        private static readonly string CacheFilePath =
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\tasktoevent\\.msalcache.bin3";

        private static readonly object FileLock = new();

        private static void BeforeAccessNotification(TokenCacheNotificationArgs args) {
            lock (FileLock) {
                args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                    ? File.ReadAllBytes(CacheFilePath)
                    : null);
            }
        }

        private static void AfterAccessNotification(TokenCacheNotificationArgs args) {
            // if the access operation resulted in a cache update
            if (!args.HasStateChanged) return;
            lock (FileLock) {
                // reflect changes in the persistent store
                File.WriteAllBytes(CacheFilePath,
                    args.TokenCache.SerializeMsalV3());
            }
        }
    }
}