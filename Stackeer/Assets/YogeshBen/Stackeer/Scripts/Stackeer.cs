using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace YogeshBen.Stackeer
{
    public enum WEB_REQUEST_TYPE
    {
        NONE,
        HTTP_GET,
        GET_TEXTURE
    }

    public enum RENDERER_TYPE
    {
        NONE,
        UI_IMAGE,
        RENDERER
    }

    public enum IMAGE_ENCODE_FORMET
    {
        JPEG,
        PNG
    }

    /// <summary>
    /// Stackeer - A unity library for loading files and images from cloud and cache them./// Developed by ShamsDEV.com
    /// Licensed under the MIT License.
    /// https://github.com/YogeshKumarBeniwal/Stackeer/
    /// </summary>
    public class Stackeer : MonoBehaviour
    {
        private static bool enableGlobalLogs = true;

        private bool enableLog = false;
        private bool cached = true;
        private WEB_REQUEST_TYPE contentType = WEB_REQUEST_TYPE.NONE;
        private IMAGE_ENCODE_FORMET imageEncodeFormet = IMAGE_ENCODE_FORMET.PNG;


        private RENDERER_TYPE rendererType = RENDERER_TYPE.NONE;
        private GameObject targetObj;
        private string url = null;

        private Texture2D loadingPlaceholder, errorPlaceholder;

        private UnityAction onStartAction = null,
            onDownloadedAction = null,
            OnLoadedAction = null,
            OnAlreadyCachedAction = null,
            onEndAction = null;

        private UnityAction<string> onResponseLoadedAction;
        private UnityAction<int> onDownloadProgressChange;
        private UnityAction<string> onErrorAction;

        private static Dictionary<string, Stackeer> underProcessStackeers
            = new Dictionary<string, Stackeer>();

        private string uniqueHash;
        private int progress;

        private bool success = false;

        static readonly string filePath = Application.persistentDataPath + "/" +
                 "Stackeer" + "/";

        /// <summary>
        /// Get instance of Stackeer class
        /// </summary>
        public static Stackeer Get()
        {
            return new GameObject("Stackeer").AddComponent<Stackeer>();
        }

        /// <summary>
        /// Set image url for download.
        /// </summary>
        /// <param name="url">Web Request Url</param>
        /// <returns></returns>
        public Stackeer Load(string url)
        {
            if (enableLog)
                Debug.Log("Url set : " + url);

            this.url = url;
            return this;
        }

        /// <summary>
        /// Set target Image component.
        /// </summary>
        /// <param name="image">target Unity UI image component</param>
        /// <returns></returns>
        public Stackeer Into(Image image)
        {
            if (enableLog)
                Debug.Log("[Stackeer] Target as UIImage set : " + image);

            rendererType = RENDERER_TYPE.UI_IMAGE;
            this.targetObj = image.gameObject;
            return this;
        }

        /// <summary>
        /// Set target Renderer component.
        /// </summary>
        /// <param name="renderer">target renderer component</param>
        /// <returns></returns>
        public Stackeer Into(Renderer renderer)
        {
            if (enableLog)
                Debug.Log("[Stackeer] Target as Renderer set : " + renderer);

            rendererType = RENDERER_TYPE.RENDERER;
            this.targetObj = renderer.gameObject;
            return this;
        }

        #region Actions
        public Stackeer WithStartAction(UnityAction action)
        {
            this.onStartAction = action;

            if (enableLog)
                Debug.Log("[Stackeer] On start action set : " + action);

            return this;
        }

        public Stackeer WithJsonLoadedAction(UnityAction<string> action)
        {
            this.onResponseLoadedAction = action;

            if (enableLog)
                Debug.Log("[Stackeer] On JSON Downloaded action set : " + action);

            return this;
        }

        public Stackeer WithDownloadedAction(UnityAction action)
        {
            this.onDownloadedAction = action;

            if (enableLog)
                Debug.Log("[Stackeer] On downloaded action set : " + action);

            return this;
        }

        public Stackeer WithDownloadProgressChangedAction(UnityAction<int> action)
        {
            this.onDownloadProgressChange = action;

            if (enableLog)
                Debug.Log("[Stackeer] On download progress changed action set : " + action);

            return this;
        }

        public Stackeer WithLoadedAction(UnityAction action)
        {
            this.OnLoadedAction = action;

            if (enableLog)
                Debug.Log("[Stackeer] On loaded action set : " + action);

            return this;
        }

        public Stackeer WithErrorAction(UnityAction<string> action)
        {
            this.onErrorAction = action;

            if (enableLog)
                Debug.Log("[Stackeer] On error action set : " + action);

            return this;
        }

        public Stackeer WithEndAction(UnityAction action)
        {
            this.onEndAction = action;

            if (enableLog)
                Debug.Log("[Stackeer] On end action set : " + action);

            return this;
        }
        #endregion

        /// <summary>
        /// Set web result content type
        /// </summary>
        /// <param name="contentType">Type type of content you want to load from the web.</param>
        /// <returns></returns>
        public Stackeer SetWebRequestType(WEB_REQUEST_TYPE contentType)
        {
            this.contentType = contentType;

            if (enableLog)
                Debug.Log("[Stackeer] Content type set to : " + contentType);

            return this;
        }

        /// <summary>
        /// Show or hide logs in console.
        /// </summary>
        /// <param name="enable">'true' for show logs in console.</param>
        /// <returns></returns>
        public Stackeer SetEnableLog(bool enableLog)
        {
            this.enableLog = enableLog;

            if (enableLog)
                Debug.Log("[Stackeer] Logging enabled : " + enableLog);

            return this;
        }

        /// <summary>
        /// Set the sprite of image when Stackeer is downloading and loading image
        /// </summary>
        /// <param name="loadingPlaceholder">loading texture</param>
        /// <returns></returns>
        public Stackeer SetLoadingPlaceholder(Texture2D loadingPlaceholder)
        {
            this.loadingPlaceholder = loadingPlaceholder;

            if (enableLog)
                Debug.Log("[Stackeer] Loading placeholder has been set.");

            return this;
        }

        /// <summary>
        /// Set image sprite when some error occurred during downloading or loading image
        /// </summary>
        /// <param name="errorPlaceholder">error texture</param>
        /// <returns></returns>
        public Stackeer SetErrorPlaceholder(Texture2D errorPlaceholder)
        {
            this.errorPlaceholder = errorPlaceholder;

            if (enableLog)
                Debug.Log("[Stackeer] Error placeholder has been set.");

            return this;
        }

        /// <summary>
        /// Enable cache
        /// </summary>
        /// <returns></returns>
        public Stackeer SetCached(bool cached)
        {
            this.cached = cached;

            if (enableLog)
                Debug.Log("[Stackeer] Cache enabled : " + cached);

            return this;
        }

        /// <summary>
        /// Start Stackeer process.
        /// </summary>
        public void StartStackeer()
        {
            if (url == null)
            {
                Error("[Stackeer] Url has not been set. Use 'Load' funtion to set image url.");
                return;
            }

            try
            {
                Uri uri = new Uri(url);
                this.url = uri.AbsoluteUri;
            }
            catch
            {
                Error("[Stackeer] Url is not correct.");
                return;
            }

            if (contentType == WEB_REQUEST_TYPE.NONE)
            {
                Error("[Stackeer] Download Content Type has not been set. Use 'SetContentType' function to set target component.");
                return;
            }

            if (contentType == WEB_REQUEST_TYPE.GET_TEXTURE)
            {
                if (rendererType == RENDERER_TYPE.NONE || targetObj == null)
                {
                    Error("[Stackeer] Target has not been set. Use 'Into' function to set target component.");
                    return;
                }
                SetImageEncodeType();
            }

            if (enableLog)
                Debug.Log("[Stackeer] Start Working.");

            if (loadingPlaceholder != null)
                SetLoadingImage();

            if (onStartAction != null)
                onStartAction.Invoke();

            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            uniqueHash = CreateMD5(url);

            if (underProcessStackeers.ContainsKey(uniqueHash))
            {
                Stackeer sameProcess = underProcessStackeers[uniqueHash];
                sameProcess.onDownloadedAction += () =>
                {
                    if (onDownloadedAction != null)
                        onDownloadedAction.Invoke();

                    CheckDownloadContentTypeAndInvokeRespectiveAction(
                    () => LoadSpriteToImage(),
                    () => LoadResponseData());
                };
            }
            else
            {
                if (File.Exists(filePath + uniqueHash))
                {
                    if (OnAlreadyCachedAction != null)
                        OnAlreadyCachedAction.Invoke();

                    CheckDownloadContentTypeAndInvokeRespectiveAction(
                    () => LoadSpriteToImage(),
                    () => LoadResponseData());
                }
                else
                {
                    underProcessStackeers.Add(uniqueHash, this);
                    StopAllCoroutines();
                    CheckDownloadContentTypeAndInvokeRespectiveAction(
                    () => StartCoroutine(ImageDownloader()),
                    () => StartCoroutine(GetRequest()));
                }
            }
        }

        public static bool IsFileAlreadyExists(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                url = uri.AbsoluteUri;
            }
            catch
            {
                if (enableGlobalLogs)
                    Debug.LogError("[Stackeer] Url is not correct.");
                return false;
            }

            var uniqueHash = CreateMD5(url);

            return File.Exists(filePath + uniqueHash);
        }

        private void SetImageEncodeType()
        {
            imageEncodeFormet = Path.GetExtension(url) == ".png" ? IMAGE_ENCODE_FORMET.PNG : IMAGE_ENCODE_FORMET.JPEG;

            if (enableLog)
                Debug.Log("[Stackeer] Image encode formet set to : " + imageEncodeFormet);
        }

        private IEnumerator ImageDownloader()
        {
            if (enableLog)
                Debug.Log("[Stackeer] Download started.");

            UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url);
            UnityWebRequestAsyncOperation op1 = uwr.SendWebRequest();

            while (!op1.isDone)
            {
                if (uwr.error != null)
                {
                    Error("Error while downloading the image : " + uwr.error);
                    yield break;
                }

                progress = Mathf.FloorToInt(op1.progress * 100);
                if (onDownloadProgressChange != null)
                    onDownloadProgressChange.Invoke(progress);

                if (enableLog)
                    Debug.Log("[Stackeer] Downloading progress : " + progress + "%");

                yield return null;
            }

            if (uwr.error == null)
            {
                Texture2D texture = ((DownloadHandlerTexture)uwr.downloadHandler).texture;

                switch (imageEncodeFormet)
                {
                    case IMAGE_ENCODE_FORMET.JPEG:
                        File.WriteAllBytes(filePath + uniqueHash, texture.EncodeToJPG());
                        break;
                    case IMAGE_ENCODE_FORMET.PNG:
                        File.WriteAllBytes(filePath + uniqueHash, texture.EncodeToPNG());
                        break;
                }
            }
            else
            {
                Error("Error while downloading the image : " + uwr.error);
                yield break;
            }

            uwr.Dispose();
            uwr = null;

            if (onDownloadedAction != null)
                onDownloadedAction.Invoke();

            LoadSpriteToImage();

            underProcessStackeers.Remove(uniqueHash);
        }

        private IEnumerator GetRequest()
        {
            if (enableLog)
                Debug.Log("[Stackeer] Download started.");

            UnityWebRequest uwr = UnityWebRequest.Get(url);
            UnityWebRequestAsyncOperation op1 = uwr.SendWebRequest();

            while (!op1.isDone)
            {
                if (uwr.error != null)
                {
                    Error("Error while downloading the GET response : " + uwr.error);
                    yield break;
                }

                progress = Mathf.FloorToInt(op1.progress * 100);
                if (onDownloadProgressChange != null)
                    onDownloadProgressChange.Invoke(progress);

                if (enableLog)
                    Debug.Log("[Stackeer] Downloading progress : " + progress + "%");

                yield return null;
            }

            if (uwr.error == null)
            {
                File.WriteAllText(filePath + uniqueHash, uwr.downloadHandler.text);
            }
            else
            {
                Error("Error while downloading the GET response : " + uwr.error);
                yield break;
            }

            uwr.Dispose();
            uwr = null;

            if (onDownloadedAction != null)
                onDownloadedAction.Invoke();

            LoadResponseData();

            underProcessStackeers.Remove(uniqueHash);
        }

        private void LoadResponseData()
        {
            progress = 100;
            if (onDownloadProgressChange != null)
                onDownloadProgressChange.Invoke(progress);

            if (enableLog)
                Debug.Log("[Stackeer] Downloading progress : " + progress + "%");

            if (!File.Exists(filePath + uniqueHash))
            {
                Error("Loading Response file has been failed.");
                return;
            }

            ResponseLoader();
        }

        private void ResponseLoader()
        {
            if (enableLog)
                Debug.Log("[Stackeer] Start loading Response.");

            if (onResponseLoadedAction != null)
                onResponseLoadedAction.Invoke(File.ReadAllText(filePath + uniqueHash));

            if (OnLoadedAction != null)
                OnLoadedAction.Invoke();

            if (enableLog)
                Debug.Log("[Stackeer] Response has been loaded.");

            success = true;
            Finish();
        }

        private void LoadSpriteToImage()
        {
            progress = 100;
            if (onDownloadProgressChange != null)
                onDownloadProgressChange.Invoke(progress);

            if (enableLog)
                Debug.Log("[Stackeer] Downloading progress : " + progress + "%");

            if (!File.Exists(filePath + uniqueHash))
            {
                Error("Loading image file has been failed.");
                return;
            }

            ImageLoader();
        }

        private void SetLoadingImage()
        {
            switch (rendererType)
            {
                case RENDERER_TYPE.RENDERER:
                    Renderer renderer = targetObj.GetComponent<Renderer>();
                    renderer.material.mainTexture = loadingPlaceholder;
                    break;

                case RENDERER_TYPE.UI_IMAGE:
                    Image image = targetObj.GetComponent<Image>();
                    Sprite sprite = Sprite.Create(loadingPlaceholder,
                         new Rect(0, 0, loadingPlaceholder.width, loadingPlaceholder.height),
                         new Vector2(0.5f, 0.5f));
                    image.sprite = sprite;

                    break;
            }

        }

        private void ImageLoader(Texture2D texture = null)
        {
            if (enableLog)
                Debug.Log("[Stackeer] Start loading image.");

            if (texture == null)
            {
                byte[] fileData;
                fileData = File.ReadAllBytes(filePath + uniqueHash);

                if (fileData == null)
                    Error("[Stackeer] Failed to load image data.");

                texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);

                //Give name to texture for memory refrence
                texture.name = uniqueHash;
            }

            if (targetObj != null)
                switch (rendererType)
                {
                    case RENDERER_TYPE.RENDERER:
                        Renderer renderer = targetObj.GetComponent<Renderer>();

                        if (renderer == null || renderer.material == null)
                            break;

                        renderer.material.mainTexture = texture;
                        break;

                    case RENDERER_TYPE.UI_IMAGE:
                        Image image = targetObj.GetComponent<Image>();

                        if (image == null)
                            break;

                        Sprite sprite = Sprite.Create(texture,
                             new Rect(0, 0, texture.width, texture.height), Vector2.zero);

                        image.sprite = sprite;
                        break;
                }

            if (OnLoadedAction != null)
                OnLoadedAction.Invoke();

            if (enableLog)
                Debug.Log("[Stackeer] Image has been loaded.");

            success = true;
            Finish();
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private void Error(string message)
        {
            success = false;

            if (enableLog)
                Debug.LogError("[Stackeer] Error : " + message);

            if (onErrorAction != null)
                onErrorAction.Invoke(message);

            if (errorPlaceholder != null)
                ImageLoader(errorPlaceholder);
            else Finish();
        }

        private void Finish()
        {
            if (enableLog)
                Debug.Log("[Stackeer] Operation has been finished.");

            if (!cached)
            {
                try
                {
                    File.Delete(filePath + uniqueHash);
                }
                catch (Exception ex)
                {
                    if (enableLog)
                        Debug.LogError($"[Stackeer] Error while removing cached file: {ex.Message}");
                }
            }

            if (onEndAction != null)
                onEndAction.Invoke();

            Invoke(nameof(Destroyer), 0.5f);
        }

        private void Destroyer()
        {
            if (enableLog)
                Debug.Log("[Stackeer] Destroying gameobject.");

            Destroy(gameObject);
        }


        /// <summary>
        /// Clear a certain cached file with its url
        /// </summary>
        /// <param name="url">Cached file url.</param>
        /// <returns></returns>
        public static void ClearCache(string url)
        {
            try
            {
                File.Delete(filePath + CreateMD5(url));

                if (enableGlobalLogs)
                    Debug.Log($"[Stackeer] Cached file has been cleared: {url}");
            }
            catch (Exception ex)
            {
                if (enableGlobalLogs)
                    Debug.LogError($"[Stackeer] Error while removing cached file: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all Stackeer cached files
        /// </summary>
        /// <returns></returns>
        public static void ClearAllCachedFiles()
        {
            try
            {
                Directory.Delete(filePath, true);

                if (enableGlobalLogs)
                    Debug.Log("[Stackeer] All Stackeer cached files has been cleared.");
            }
            catch (Exception ex)
            {
                if (enableGlobalLogs)
                    Debug.LogError($"[Stackeer] Error while removing cached file: {ex.Message}");
            }
        }

        private void CheckDownloadContentTypeAndInvokeRespectiveAction(Action imageAction = null, Action jsonAction = null)
        {
            switch (contentType)
            {
                case WEB_REQUEST_TYPE.GET_TEXTURE:
                    imageAction?.Invoke();
                    break;
                case WEB_REQUEST_TYPE.HTTP_GET:
                    jsonAction?.Invoke();
                    break;
                case WEB_REQUEST_TYPE.NONE:
                    Error("Forget to set Download content type!");
                    break;
            }
        }
    }
}