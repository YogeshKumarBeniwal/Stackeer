using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace YogeshBen.CloudImage
{
    public enum CloudImageType
    {
        UIImage,
        SpriteRenderer,
        MeshRenderer
    }

    public class CloudImage : MonoBehaviour
    {
        [Header("Image Data")]
        [Tooltip("Media url you want to cache")]
        public string MediaUrl;
        [Tooltip("Select which component you want CloudImage to effect")]
        public CloudImageType ObjectType;

        [Header("Cache Data")]
        [Tooltip("Toggel if you want to cache data localy")]
        public bool enableCaching = true;
        [Tooltip("Time in hours you want the media to keep cached")]
        public int cacheTimeOut = 72;

        //image Name
        private string mediaName;

        // delegate and event for successful image download
        public delegate void CloudImageDownloadAction(CloudImage cloudImage);
        public static event CloudImageDownloadAction OnCloudImageDownloaded;

        // delegate and event for Unsuccessful image download
        public delegate void CloudImageDownloadFaileAction(CloudImage cloudImage, string errorMessage);
        public static event CloudImageDownloadFaileAction OnCloudImageDownloadFailed;

        private string lastUpdateTime
        {
            get => PlayerPrefs.GetString(mediaName, "");

            set => PlayerPrefs.SetString(mediaName, value);
        }

        void Start()
        {
            RefreshImage();
        }

        /// <summary>
        /// This method Downloads image from internet if cache is expired or caching is
        /// disabled else loads data from local storage
        /// </summary>
        public void RefreshImage()
        {
            mediaName = Path.GetFileNameWithoutExtension(MediaUrl);
            if (enableCaching && IsCacheValid())
            {
                LoadCachedImage();
            }
            else
            {
                StartCoroutine(DownloadImage(MediaUrl));
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
                StartCoroutine(DownloadImage(MediaUrl));
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
                OnCloudImageDownloadFailed(this, "Failed to load saved data!");
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

                    if (enableCaching)
                        SaveTexture(texture);

                    //Set downloaded texture into the component 
                    SetComponent(texture);

                    if (OnCloudImageDownloaded != null)
                        OnCloudImageDownloaded(this);
                }
                else
                {
                    if (OnCloudImageDownloadFailed != null)
                        OnCloudImageDownloadFailed(this, webRequest.error);

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
                    break;
                case CloudImageType.SpriteRenderer:
                    // assign sprite to the SpriteRenderer attached to this gameObject
                    this.GetComponent<SpriteRenderer>().sprite = sprite;
                    break;
                case CloudImageType.MeshRenderer:
                    // assign texture to the Smaterial attached to this gameObject
                    this.GetComponent<MeshRenderer>().material.mainTexture = texture;
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

            int pastSDate = int.Parse(split[0]);
            int pastHour = int.Parse(split[1]);

            int currentDate = DateTime.Now.Day;
            int currentHour = DateTime.Now.Hour;

            int dateDiff = currentDate - pastSDate;

            if (dateDiff < 0)
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

        private void SaveTexture(Texture2D texture)
        {
            string path = Application.persistentDataPath + Path.GetFileNameWithoutExtension(MediaUrl);
            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
                lastUpdateTime = DateTime.Now.Day + "/" + DateTime.Now.Hour;
                Debug.Log("Saved Data to: " + path.Replace("/", "\\"));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed To Save Data to: " + path.Replace("/", "\\"));
                Debug.LogWarning("Error: " + e.Message);
            }
        }

        private byte[] LoadTexture()
        {
            string path = Application.persistentDataPath + Path.GetFileNameWithoutExtension(MediaUrl);

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
