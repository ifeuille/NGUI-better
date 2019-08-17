using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class UISpriteFillScreen : MonoBehaviour
//{
//    void Start()
//    {
//        UIStretch s = gameObject.AddComponent<UIStretch>();
//        s.initialSize = new Vector2(Screen.width, Screen.height);
//        s.style = UIStretch.Style.FitInternalKeepingRatio;
//    }

//}

public class UISpriteFillScreen : MonoBehaviour
{

    private float width;
    private float height;
    public UIWidget BG;

    // Use this for initialization
    void Start()
    {
        //SetBasicValues();
        // BG.aspectRatio = width / height;
        int height = 100;
        int width = 100;
        UIRoot root = GameObject.FindObjectOfType<UIRoot>();
        //默认是1.775比例
        float aspectRatio = 1.775f;
        if (root != null)
        {
            float s = (float)root.activeHeight / Screen.height;
            height = Mathf.CeilToInt(Screen.height * s);
            width = Mathf.CeilToInt(Screen.width * s);
            aspectRatio = ((float)width / height);
        }

        if (aspectRatio > 1.775f)
        {
            BG.keepAspectRatio = UIWidget.AspectRatioSource.BasedOnWidth;
            BG.aspectRatio = 1.775f;
            BG.SetDimensions(width, height);
        }
        else
        {
            BG.keepAspectRatio = UIWidget.AspectRatioSource.BasedOnHeight;
            BG.aspectRatio = 1.775f;
            BG.SetDimensions(width, height);
        }
        //BG.aspectRatio = 1.775f;
        //BG.updateAnchors = UIRect.AnchorUpdate.OnUpdate;
        //UIRect.CustomUpdate();
        //BG.updateAnchors = UIRect.AnchorUpdate.OnEnable;
    }

    // Update is called once per frame
    void Update()
    {

    }


}