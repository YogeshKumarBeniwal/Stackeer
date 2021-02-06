using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YogeshBen.Stackeer;
using UnityEngine.Events;
using System;

public class Demo1 : MonoBehaviour
{
    [SerializeField]
    private Image image;
    [SerializeField]
    private TextMeshProUGUI jsonText;
    [SerializeField]
    private string imageUrl = "";
    [SerializeField]
    private string jsonUrl = "";

    private void Start()
    {
        Stackeer.Get().Load(imageUrl).SetContentType(CONTENT_TYPE.IMAGE).SetEnableLog(true).Into(image).StartStackeer();
        Stackeer.Get().Load(jsonUrl).SetContentType(CONTENT_TYPE.JSON).SetEnableLog(true)
            .WithJsonLoadedAction(OnJsonLoaded).StartStackeer();
    }

    private void OnJsonLoaded(string data)
    {
        jsonText.text = data;
    }
}
