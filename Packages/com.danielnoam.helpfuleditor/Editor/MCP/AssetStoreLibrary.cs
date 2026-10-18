using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DNExtensions.HelpfulEditor.MCP
{
    /// <summary>
    /// The user's Asset Store library — everything they own, downloaded or not — read and downloaded
    /// through the Package Manager window's own Asset Store services, signed in as whoever is signed
    /// in to the editor. Unity exposes none of this publicly, so it is reached by reflection into
    /// UnityEditor.PackageManager.UI.Internal; every entry point is looked up once and checked, and a
    /// Unity version that renames one reports itself unavailable rather than throwing mid-call.
    ///
    /// The library is fetched page by page, then once per category so each asset can be filed under
    /// its deepest one, and kept in Library so it survives restarts. Downloads go through the same
    /// download manager as the window's Download button, which also decrypts them into the cache.
    /// </summary>
    internal static class AssetStoreLibrary
    {
        private const string Namespace = "UnityEditor.PackageManager.UI.Internal.";
        private const int PageSize = 100;

        private static readonly string CachePath = Path.Combine("Library", "DNExtensionsMCP", "AssetStoreLibrary.json");

        private const BindingFlags AnyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        private static bool _resolved;
        private static string _unavailableReason;

        private static Assembly _assembly;
        private static object _container;
        private static MethodInfo _resolve;

        private static Type _purchasesType;
        private static Type _purchaseInfoType;
        private static Type _errorType;
        private static Type _queryType;
        private static Type _filtersType;
        private static MethodInfo _getPurchases;
        private static MethodInfo _download;
        private static MethodInfo _getDownloadOperation;
        private static MethodInfo _getLocalPackageList;
        private static MethodInfo _parseLocalInfo;

        private static Library _library;
        private static List<string> _categoryQueue;
        private static int _categoryStart;
        private static Dictionary<long, Item> _fetched;

        /// <summary>Whatever the last refresh is doing, for the status line: idle, fetching, failed.</summary>
        public static string RefreshState { get; private set; } = "idle";

        public static bool IsRefreshing { get; private set; }

        [Serializable]
        public class Item
        {
            public long id;
            public string name;
            public string category = string.Empty;
            public string purchased;
            public bool hidden;
        }

        [Serializable]
        public class Category
        {
            public string name;
            public long count;
        }

        [Serializable]
        private class Library
        {
            public string fetchedAt;
            public string user;
            public long total;
            public List<Item> items = new List<Item>();
            public List<Category> categories = new List<Category>();
        }

        public class LocalPackage
        {
            public long id;
            public string title;
            public string version;
            public string path;
        }

        public class DownloadState
        {
            public long id;
            public string state;
            public float progress;
            public string error;
            public string path;
            public bool inProgress;
        }

        public static IReadOnlyList<Item> Items => LoadLibrary().items;
        public static IReadOnlyList<Category> Categories => LoadLibrary().categories;
        public static string FetchedAt => LoadLibrary().fetchedAt;
        public static long Total => LoadLibrary().total;

        public static bool IsAvailable(out string reason)
        {
            Resolve();

            reason = _unavailableReason;
            return _unavailableReason == null;
        }

        public static bool IsSignedIn(out string user)
        {
            user = null;
            if (!IsAvailable(out _)) return false;

            object connect = Service("IUnityConnectProxy");
            Type connectType = _assembly.GetType(Namespace + "IUnityConnectProxy");

            user = connectType.GetProperty("displayName")?.GetValue(connect) as string;
            return connectType.GetProperty("isUserLoggedIn")?.GetValue(connect) is true;
        }

        /// <summary>Starts fetching the library in the background. Returns false, with the reason, when it cannot start.</summary>
        public static bool StartRefresh(out string reason)
        {
            if (IsRefreshing)
            {
                reason = "A refresh is already running.";
                return false;
            }

            if (!IsAvailable(out reason)) return false;

            if (!IsSignedIn(out string user))
            {
                reason = "Nobody is signed in to the Unity editor, and the Asset Store library is per account. Sign in from the editor's account menu.";
                return false;
            }

            IsRefreshing = true;
            RefreshState = "fetching purchases";

            _fetched = new Dictionary<long, Item>();
            _categoryQueue = null;
            _library = new Library { user = user };

            RequestPage(0, null);

            reason = null;
            return true;
        }

        /// <summary>Product id to the local package, for everything already downloaded into the Asset Store cache.</summary>
        public static Dictionary<long, LocalPackage> LocalPackages()
        {
            Dictionary<long, LocalPackage> packages = new Dictionary<long, LocalPackage>();
            if (!IsAvailable(out _)) return packages;

            object utils = Service("IAssetStoreUtils");

            if (!(_getLocalPackageList.Invoke(utils, null) is IEnumerable list)) return packages;

            foreach (object info in list)
            {
                object local = _parseLocalInfo.Invoke(null, new[] { info });
                if (local == null) continue;

                Type localType = local.GetType();
                long id = (long)localType.GetField("productId").GetValue(local);
                if (id <= 0) continue;

                packages[id] = new LocalPackage
                {
                    id = id,
                    title = localType.GetField("title").GetValue(local) as string,
                    version = localType.GetField("versionString").GetValue(local) as string,
                    path = localType.GetField("packagePath").GetValue(local) as string
                };
            }

            return packages;
        }

        /// <summary>Hands the products to the Package Manager's download manager, which queues, downloads and decrypts them.</summary>
        public static bool Download(IReadOnlyCollection<long> productIds, out string reason)
        {
            if (!IsAvailable(out reason)) return false;

            if (!IsSignedIn(out _))
            {
                reason = "Nobody is signed in to the Unity editor, so nothing can be downloaded.";
                return false;
            }

            object manager = Service("IAssetStoreDownloadManager");
            bool started = _download.Invoke(manager, new object[] { productIds.ToList() }) is true;

            reason = started ? null : "The Package Manager refused the download. Its console messages have the reason.";
            return started;
        }

        /// <summary>The download manager's view of one product, or null when it has never been asked to download it.</summary>
        public static DownloadState GetDownloadState(long productId)
        {
            if (!IsAvailable(out _)) return null;

            object manager = Service("IAssetStoreDownloadManager");
            object operation = _getDownloadOperation.Invoke(manager, new object[] { (long?)productId });
            if (operation == null) return null;

            Type operationType = operation.GetType();

            return new DownloadState
            {
                id = productId,
                state = operationType.GetProperty("state")?.GetValue(operation)?.ToString(),
                progress = operationType.GetProperty("progressPercentage")?.GetValue(operation) is float progress ? progress : 0f,
                error = operationType.GetProperty("errorMessage")?.GetValue(operation) as string,
                path = operationType.GetProperty("packageNewPath")?.GetValue(operation) as string,
                inProgress = operationType.GetProperty("isInProgress")?.GetValue(operation) is true
            };
        }

        private static void RequestPage(int startIndex, string category)
        {
            object filters = null;

            if (category != null)
            {
                filters = Activator.CreateInstance(_filtersType, true);
                _filtersType.GetMethod("UpdateCategories", AnyMember)?.Invoke(filters, new object[] { new List<string> { category } });
            }

            object query = _queryType.GetConstructors(AnyMember)[0].Invoke(new[] { startIndex, (object)PageSize, string.Empty, filters });
            object api = Service("IAssetStoreRestAPI");

            Delegate onSuccess = Callback(_getPurchases.GetParameters()[1].ParameterType, _purchasesType, purchases => OnPage(purchases, startIndex, category));
            Delegate onError = Callback(_getPurchases.GetParameters()[2].ParameterType, _errorType, OnError);

            _getPurchases.Invoke(api, new object[] { query, onSuccess, onError });
        }

        /// <summary>
        /// One page in. The unfiltered pass collects the items and the category counts; each filtered
        /// pass files its items under that category when it is deeper than the one they already have,
        /// so a tool that is also "tools/utilities" ends up there rather than under "tools".
        /// </summary>
        private static void OnPage(object purchases, int startIndex, string category)
        {
            try
            {
                long total = (long)_purchasesType.GetField("total").GetValue(purchases);
                int received = 0;

                foreach (object info in (IEnumerable)_purchasesType.GetField("list").GetValue(purchases))
                {
                    received++;

                    long id = (long)_purchaseInfoType.GetField("productId").GetValue(info);

                    if (category == null)
                    {
                        _fetched[id] = new Item
                        {
                            id = id,
                            name = _purchaseInfoType.GetField("displayName").GetValue(info) as string,
                            purchased = _purchaseInfoType.GetField("purchasedTime").GetValue(info) as string,
                            hidden = _purchaseInfoType.GetField("isHidden").GetValue(info) is true
                        };

                        continue;
                    }

                    if (_fetched.TryGetValue(id, out Item item) && Depth(category) > Depth(item.category))
                    {
                        item.category = category;
                    }
                }

                if (category == null && startIndex == 0)
                {
                    _library.total = total;

                    foreach (object entry in (IEnumerable)_purchasesType.GetField("categories").GetValue(purchases))
                    {
                        Type entryType = entry.GetType();

                        _library.categories.Add(new Category
                        {
                            name = entryType.GetField("name").GetValue(entry) as string,
                            count = (long)entryType.GetField("count").GetValue(entry)
                        });
                    }

                    _library.categories.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
                }

                int next = startIndex + received;

                if (received > 0 && next < total)
                {
                    RefreshState = category == null ? $"fetching purchases ({next}/{total})" : $"sorting into categories ({category})";
                    RequestPage(next, category);

                    return;
                }

                NextCategory();
            }
            catch (Exception e)
            {
                Fail(e.Message);
            }
        }

        private static void NextCategory()
        {
            if (_categoryQueue == null)
            {
                _categoryQueue = _library.categories.Select(entry => entry.name).ToList();
                _categoryStart = _categoryQueue.Count;
            }

            if (_categoryQueue.Count == 0)
            {
                Finish();
                return;
            }

            string category = _categoryQueue[0];
            _categoryQueue.RemoveAt(0);

            RefreshState = $"sorting into categories ({_categoryStart - _categoryQueue.Count}/{_categoryStart})";
            RequestPage(0, category);
        }

        private static void Finish()
        {
            _library.items = _fetched.Values.OrderBy(item => item.name, StringComparer.OrdinalIgnoreCase).ToList();
            _library.fetchedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            SaveLibrary(_library);

            IsRefreshing = false;
            RefreshState = "idle";
            _fetched = null;
            _categoryQueue = null;
        }

        private static void OnError(object error)
        {
            Fail(_errorType.GetProperty("message")?.GetValue(error) as string ?? "unknown error");
        }

        private static void Fail(string message)
        {
            IsRefreshing = false;
            RefreshState = $"failed: {message}";
            _fetched = null;
            _categoryQueue = null;
            _library = null;
        }

        private static int Depth(string category)
        {
            return string.IsNullOrEmpty(category) ? 0 : category.Count(character => character == '/') + 1;
        }

        private static Library LoadLibrary()
        {
            if (_library != null && !IsRefreshing) return _library;

            string fullPath = Path.Combine(GameView.GameViewScreenshot.ProjectRoot, CachePath);

            Library loaded = File.Exists(fullPath) ? JsonUtility.FromJson<Library>(File.ReadAllText(fullPath)) : null;
            loaded ??= new Library();

            if (!IsRefreshing) _library = loaded;

            return loaded;
        }

        private static void SaveLibrary(Library library)
        {
            string fullPath = Path.Combine(GameView.GameViewScreenshot.ProjectRoot, CachePath);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? GameView.GameViewScreenshot.ProjectRoot);
            File.WriteAllText(fullPath, JsonUtility.ToJson(library, true));
        }

        private static object Service(string interfaceName)
        {
            return _resolve.MakeGenericMethod(_assembly.GetType(Namespace + interfaceName)).Invoke(_container, null);
        }

        /// <summary>A delegate of the internal callback type that hands its single argument on as an object.</summary>
        private static Delegate Callback(Type delegateType, Type argumentType, Action<object> handler)
        {
            ParameterExpression argument = Expression.Parameter(argumentType, "value");
            Expression body = Expression.Invoke(Expression.Constant(handler), Expression.Convert(argument, typeof(object)));

            return Expression.Lambda(delegateType, body, argument).Compile();
        }

        /// <summary>Looks up every internal entry point once. Any one missing leaves the reason behind for the caller to report.</summary>
        private static void Resolve()
        {
            if (_resolved && _unavailableReason == null && _container != null) return;

            _resolved = true;
            _unavailableReason = null;

            try
            {
                _assembly = typeof(UnityEditor.PackageManager.UI.Window).Assembly;

                Type containerType = Require(Namespace + "ServicesContainer");
                _container = containerType.BaseType?.GetProperty("instance", AnyMember)?.GetValue(null);
                _resolve = containerType.GetMethod("Resolve", AnyMember);

                _purchasesType = Require(Namespace + "AssetStorePurchases");
                _purchaseInfoType = Require(Namespace + "AssetStorePurchaseInfo");
                _errorType = Require(Namespace + "UIError");
                _queryType = Require(Namespace + "PurchasesQueryArgs");
                _filtersType = Require(Namespace + "PageFilters");

                _getPurchases = Require(Namespace + "IAssetStoreRestAPI").GetMethod("GetPurchases");
                _download = Require(Namespace + "IAssetStoreDownloadManager").GetMethod("Download");
                _getDownloadOperation = Require(Namespace + "IAssetStoreDownloadManager").GetMethod("GetDownloadOperation");
                _getLocalPackageList = Require(Namespace + "IAssetStoreUtils").GetMethod("GetLocalPackageList");
                _parseLocalInfo = Require(Namespace + "AssetStoreLocalInfo").GetMethod("ParseLocalInfo", AnyMember);

                if (_container == null || _resolve == null || _getPurchases == null || _download == null ||
                    _getDownloadOperation == null || _getLocalPackageList == null || _parseLocalInfo == null)
                {
                    _unavailableReason = "This Unity version's Package Manager no longer has the Asset Store entry points these tools rely on.";
                }
            }
            catch (Exception e)
            {
                _unavailableReason = $"The Package Manager's Asset Store services could not be reached: {e.Message}";
            }
        }

        private static Type Require(string fullName)
        {
            return _assembly.GetType(fullName) ?? throw new MissingMemberException($"{fullName} was not found");
        }
    }
}
