using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace YogeshBen.CloudImage
{
    // Enum for component attached to the game object
    public enum CloudImageType
    {
        UIImage,
        SpriteRenderer,
        MeshRenderer
    }

    public enum CloudImageErrorType
    {
        CloudFail,
        LocalFail
    }

    public class CloudImage : MonoBehaviour
    {
        [Header("Image Data")]
        [Tooltip("Media url you want to cache")]
        public string mediaUrl;
        [Tooltip("Select which component you want CloudImage to effect")]
        public CloudImageType ObjectType;

        [Header("Cache Data")]
        [Tooltip("Toggel if you want to reset the cache data on start")]
        public bool resetOnStart = false;
        [Tooltip("Toggel if you want to cache data localy")]
        public bool enableCaching = true;
        [Tooltip("Time in hours you want the media to keep cached")]
        public int cacheTimeOut = 72;

        [HideInInspector]
        // image Name
        public string mediaName;

        // delegate and event for successful image download
        public delegate void CloudImageDownloadAction(CloudImage cloudImage);
        public static event CloudImageDownloadAction OnCloudImageDownloadSuccessful;

        // delegate and event for Unsuccessful image download
        public delegate void CloudImageDownloadFaileAction(CloudImage cloudImage, CloudImageErrorType errorType, string errorMessage);
        public static event CloudImageDownloadFaileAction OnCloudImageDownloadFailed;

        private string lastUpdateTime
        {
            get => PlayerPrefs.GetString(mediaName, "");

            set => PlayerPrefs.SetString(mediaName, value);
        }

        void Start()
        {
            if (resetOnStart)
                DeleteCachedData();

            RefreshImage();
        }

        /// <summary>
        /// This method Downloads image from internet if cache is expired or caching is
        /// disabled else loads data from local storage
        /// </summary>
        public void RefreshImage()
        {
            if(mediaUrl == null)
            {
                Debug.LogError("You forget to set media URL");
                return;
            }

            //Get name of the media
            mediaName = Path.GetFileNameWithoutExtension(mediaUrl);

            //if caching is enabled check if cache is valid or not then load
            //data from local storage else download from internet
            if (enableCaching && IsCacheValid())
            {
                LoadCachedImage();
            }
            else
            {
                StartCoroutine(DownloadImage(mediaUrl));
            }
        }

        /// <summary>
        /// Method to redownload image from the internet
        /// </summary>
        /// <param name="url"></param>
        public void ReDownloadImage(string url = null)
        {
            if (url != null)
            {
                StartCoroutine(DownloadImage(mediaUrl));
            }
            else
            {
                StartCoroutine(DownloadImage(url));
            }
        }

        /// <summary>
        /// Method used to load cached data
        /// </summary>
        private void LoadCachedImage()
        {
            byte[] bytes = LoadTexture();
            if (bytes == null)
            {
                if (OnCloudImageDownloadFailed != null)
                    OnCloudImageDownloadFailed(this, CloudImageErrorType.LocalFail, "Failed to load saved data!");
            }
            else
            {
                Texture2D texture;
                texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                //Set Loaded texture into the component 
                SetComponent(texture);
            }
        }

        /// <summary>
        /// Method to download a Image from Internet and assign to Image attached the the gameobject
        /// </summary>
        /// <param name="MediaUrl"></param>
        /// <returns></returns>
        private IEnumerator DownloadImage(string MediaUrl)
        {
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(MediaUrl))
            {

                // Request and wait for the desired media.
                yield return webRequest.SendWebRequest();

                // Check to see if we don't have any errors.
                if (string.IsNullOrEmpty(webRequest.error))
                {
                    Texture2D texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;

                    //If caching is enabled save data to local storage
                    if (enableCaching)
                        SaveTexture(texture);

                    //Set downloaded texture into the component 
                    SetComponent(texture);
                }
                else
                {
                    if (OnCloudImageDownloadFailed != null)
                        OnCloudImageDownloadFailed(this, CloudImageErrorType.CloudFail, webRequest.error);

                    Debug.LogError(webRequest.error);
                }
            }
        }

        private void SetComponent(Texture2D texture)
        {
            //Create sprite from texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            switch (ObjectType)
            {
                case CloudImageType.UIImage:
                    // assign sprite to the Image attached to this gameObject
                    this.GetComponent<Image>().sprite = sprite;
                    if (OnCloudImageDownloadSuccessful != null)
                        OnCloudImageDownloadSuccessful(this);
                    break;
                case CloudImageType.SpriteRenderer:
                    // assign sprite to the SpriteRenderer attached to this gameObject
                    this.GetComponent<SpriteRenderer>().sprite = sprite;
                    if (OnCloudImageDownloadSuccessful != null)
                        OnCloudImageDownloadSuccessful(this);
                    break;
                case CloudImageType.MeshRenderer:
                    // assign texture to the Smaterial attached to this gameObject
                    this.GetComponent<MeshRenderer>().material.mainTexture = texture;
                    if (OnCloudImageDownloadSuccessful != null)
                        OnCloudImageDownloadSuccessful(this);
                    break;
            }
        }

        /// <summary>
        /// This method compairs last update time and current time and checked if cache is expired or not
        /// </summary>
        /// <returns></returns>
        private bool IsCacheValid()
        {
            string lastUpdate = lastUpdateTime;

            if (lastUpdate == "")
            {
                return false;
            }

            string[] split = lastUpdate.Split('/');

            int pastDate = int.Parse(split[0]);
            int pastHour = int.Parse(split[1]);
            int pastMonth = int.Parse(split[2]);

            int currentDate = DateTime.Now.Day;
            int currentHour = DateTime.Now.Hour;
            int currentMonth = DateTime.Now.Month;

            int monthDiff = currentMonth - pastMonth;
            int dateDiff = currentDate - pastDate;

            if (monthDiff > 0)
            {
                return false;
            }
            else if (dateDiff > 0)
            {
                if (((24 - pastHour) + currentHour) >= cacheTimeOut)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if ((currentHour - pastHour) >= cacheTimeOut)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Function for deleting a perticular cached data based on there url
        /// </summary>
        /// <param name="url"></param>
        public void DeleteCachedData(string url = null)
        {
            string fName = url == null ? Path.GetFileNameWithoutExtension(mediaUrl) : Path.GetFileNameWithoutExtension(url);

            //Reset PlayerPref
            PlayerPrefs.DeleteKey(fName);

            string path = Application.persistentDataPath + fName;

            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Debug.LogWarning("Directory does not exist");
                return;
            }

            if (!File.Exists(path))
            {
                Debug.LogError("File does not exist");
                return;
            }

            //Delete file
            File.Delete(path);
            Debug.Log("File delete successfully");

        }


        /// <summary>
        /// Function used to save data to local storage
        /// </summary>
        /// <param name="texture"></param>
        private void SaveTexture(Texture2D texture)
        {
            string path = Application.persistentDataPath + mediaName;
            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            if(File.Exists(path))
            {
                //Delete cached file
                File.Delete(path);
            }

            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
                lastUpdateTime = DateTime.Now.Day + "/" + DateTime.Now.Hour + "/" + DateTime.Now.Month;
                Debug.Log("Saved Data to: " + path.Replace("/", "\\"));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed To Save Data to: " + path.Replace("/", "\\"));
                Debug.LogWarning("Error: " + e.Message);
            }
        }

        /// <summary>
        /// Function used to load data from local storage.
        /// </summary>
        /// <returns></returns>
        private byte[] LoadTexture()
        {
            string path = Application.persistentDataPath + mediaName;

            byte[] dataByte = null;

            //Exit if Directory or File does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Debug.LogWarning("Directory does not exist");
                return null;
            }

            if (!File.Exists(path))
            {
                Debug.LogError("File does not exist");
                return null;
            }

            try
            {
                dataByte = File.ReadAllBytes(path);
                Debug.Log("Loaded Data from: " + path.Replace("/", "\\"));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed To Load Data from: " + path.Replace("/", "\\"));
                Debug.LogWarning("Error: " + e.Message);
            }
            return dataByte;
        }
    }
}
