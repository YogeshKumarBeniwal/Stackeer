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
        CloudImage.OnCloudImageDownloadFailed += DisplayMessage;
    }

    private void OnDisable()
    {
        CloudImage.OnCloudImageDownloadFailed -= DisplayMessage;
    }

    private void DisplayMessage(CloudImage cloudImage, string message)
    {
        messageText.text = message;
    }

}
