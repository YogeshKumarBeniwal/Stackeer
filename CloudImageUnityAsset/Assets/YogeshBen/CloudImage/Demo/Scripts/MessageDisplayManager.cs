using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YogeshBen.CloudImage;

public class MessageDisplayManager : MonoBehaviour
{
    [SerializeField]
    private Text messageText;

    private void OnEnable()
    {
        CloudImage.OnCloudImageDownloadFailed += DisplayMessageAndRetryToLoad;
        CloudImage.OnCloudImageDownloadSuccessful += OnSuccessLoad;
    }

    private void OnDisable()
    {
        CloudImage.OnCloudImageDownloadFailed -= DisplayMessageAndRetryToLoad;
        CloudImage.OnCloudImageDownloadSuccessful -= OnSuccessLoad;
    }

    //Do something on image load failed
    private void OnSuccessLoad(CloudImage cloudImage)
    {
        messageText.text += cloudImage.mediaName + ": successfuly Loaded\n";
    }

    //Do something on image load failed
    private void DisplayMessageAndRetryToLoad(CloudImage cloudImage,CloudImageErrorType errorType,string message)
    {
        messageText.text = message;

        switch (errorType)
        {
            case CloudImageErrorType.CloudFail:
                cloudImage.RefreshImage();
                break;
            case CloudImageErrorType.LocalFail:
                cloudImage.RefreshImage();
                break;
        }
    }

}
