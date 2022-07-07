using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.Collections;
using System.Collections.Generic;

namespace SuperUltra.Container
{

    public class AddressableManager : MonoBehaviour
    {
        [SerializeField]
        bool shouldDownload;
        [SerializeField]
        bool _deleteCache;
        [SerializeField]
        MenuUIManager _menuUIManager;
        [SerializeField]
        Map<string, string> _gameList;
        AsyncOperationHandle _op;
        static AsyncOperationHandle _currentSceneHandle;

        void Start()
        {
            // PrintProfile();
            ContainerInterface.OnReturnMenu += UnloadScene;
            Debug.Log($"Caching.cacheCount {Caching.cacheCount}");
            if (Caching.cacheCount > 0 && _deleteCache)
            {
                Debug.Log($"deleteing cache");
                Caching.ClearCache();
            }

            Addressables.InitializeAsync().Completed += (obj) =>
            {
                foreach (Map<string, string>.KeyPair item in _gameList.list)
                {
                    DownloadRemoteCatalog(item.key, item.value);
                }
            };
        }

        void PrintProfile()
        {
            Addressables.InternalIdTransformFunc += location =>
            {
                if (location.InternalId.StartsWith("http://") && location.InternalId.EndsWith(".json"))
                {
                    //Do something with remote catalog location.
                }
                Debug.Log("location.InternalId  " + location.InternalId);

                return location.InternalId;
            };
        }

        // Update is called once per frame
        void Update()
        {

        }

        void DownloadScene(string key)
        {
            if (!shouldDownload)
            {
                return;
            }

            AsyncOperationHandle<IList<IResourceLocation>> operationHandle = Addressables.LoadResourceLocationsAsync(key);
            operationHandle.Completed += (obj) =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    CreateButtons(obj.Result);
                }
            };
        }

        void CreateButtons(IList<IResourceLocation> locations)
        {
            foreach (IResourceLocation item in locations)
            {
                Addressables.GetDownloadSizeAsync(item.PrimaryKey).Completed += (obj) =>
                {
                    _menuUIManager.CreateButtons(
                        item.PrimaryKey,
                        obj.Result,
                        () =>
                        {
                            DownloadDependeny(item);
                        }
                    );
                };
            }
        }

        void DownloadDependeny(IResourceLocation item)
        {
            AsyncOperationHandle operationHandle = Addressables.DownloadDependenciesAsync(item.PrimaryKey);
            StartCoroutine(UpdateProgress(operationHandle, "Downloading dependencies..."));
            operationHandle.Completed += (obj2) =>
            {
                if (obj2.Status == AsyncOperationStatus.Succeeded)
                {
                    LoadGameScene(item);
                    _menuUIManager.UpdateResult("Downloading dependencies...", true);
                }
            };
        }

        void LoadGameScene(IResourceLocation item)
        {
            AsyncOperationHandle operationHandle = Addressables.LoadSceneAsync(item.PrimaryKey);
            operationHandle.Completed += (AsyncOperationHandle obj) =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    _currentSceneHandle = obj;
                    Debug.Log("Load Success");
                }
                else
                {
                    Debug.Log("Load Failed");
                }
            };
        }

        void DownloadRemoteCatalog(string gameName, string catalogName)
        {
            AsyncOperationHandle operationHandle = Addressables.LoadContentCatalogAsync(
                $"{Config.RemoteStagingCatalogUrl}/{gameName}/{Config.BuildTarget}/{catalogName}", true
            );
            Debug.Log($"{Config.RemoteStagingCatalogUrl}/{gameName}/{Config.BuildTarget}/{catalogName}");
            StartCoroutine(UpdateProgress(operationHandle, $"Retrive {gameName} {catalogName} data from aws"));
            operationHandle.Completed += (obj) =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    DownloadScene("MainScene");
                    _menuUIManager.UpdateResult($"Retrive {gameName} {catalogName} data from aws", true);
                }
                else
                {
                    _menuUIManager.UpdateResult($"Retrive {gameName} {catalogName} data from aws", false);
                }
            };
        }

        IEnumerator UpdateProgress(AsyncOperationHandle op, string taskName)
        {
            while (op.IsValid() && op.PercentComplete < 1)
            {
                _menuUIManager.UpdateProgress(op.PercentComplete, taskName);
                yield return null;
            }
        }

        void UnloadScene()
        {
            if (_currentSceneHandle.IsValid())
            {
                Addressables.UnloadSceneAsync(_currentSceneHandle);
            }
        }

    }

}