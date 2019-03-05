﻿using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundImage : MonoBehaviour
{
    const string IMAGE_EXTENSION = ".jpg";
    const int WIDTH = 3264;
    const int HEIGHT = 2448;

    public string ImagesDir;

    RawImage image;
    Texture2D tex;

    void SetRawImageSize()
    {
        /*
         * image and screen dimensions as floats
         */
        float i_width = (float)WIDTH;
        float i_height = (float)HEIGHT;
        float s_width = (float)Screen.width;
        float s_height = (float)Screen.height;

        float img_aspect = i_width / i_height;
        float screen_aspect = s_width / s_height;

        int scaled_w, scaled_h;
        if (img_aspect > screen_aspect)
        {
            float scale = s_width / i_width;

            scaled_w = Screen.width;
            scaled_h = (int)(i_height * scale);

        }
        else
        {
            float scale = s_height / i_height;

            scaled_w = (int)(i_width * scale);
            scaled_h = Screen.height;
        }

        image.rectTransform.sizeDelta = new Vector2(scaled_w, scaled_h);
    }

    void Start()
    {
        tex = new Texture2D(WIDTH, HEIGHT);
        image = gameObject.GetComponent<RawImage>();
        image.texture = tex;

        SetRawImageSize();
    }

    public void ShowImage(string imageName)
    {
        var imgPath = Path.Combine(ImagesDir, imageName + IMAGE_EXTENSION);

        Debug.LogFormat("show '{0}", imgPath);
        tex.LoadImage(File.ReadAllBytes(imgPath));

    }
}