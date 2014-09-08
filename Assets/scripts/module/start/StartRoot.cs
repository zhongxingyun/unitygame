﻿using SDK.Lib;
using UnityEngine;
using System.Collections;

namespace SDK.Lib
{
    /**
     * @brief 这个模块主要是加载代码基础模块，然后加载游戏功能模块，然后加载资源
     */
    public class StartRoot : MonoBehaviour
    {
        private string m_appURL = "http://127.0.0.1/StreamingAssets/Module/App.unity3d";
        private string m_appName = "App";
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnGUI()
        {
            //设置按钮中文字的颜色  
            GUI.color = Color.green;
            //设置按钮的背景色  
            GUI.backgroundColor = Color.red;

            if (GUI.Button(new Rect(100, 100, 300, 40), "点击加载游戏代码，开始游戏"))
            {
                StartCoroutine(DownloadAppAsset());
            }
        }

        // 下载 app 模块
        IEnumerator DownloadAppAsset()
        {
            //下载场景，加载场景
            WWW app3w = WWW.LoadFromCacheOrDownload(m_appURL, 15);
            yield return app3w;

            // 使用预设加载
            AssetBundle bundle = app3w.assetBundle;
            Object bt = bundle.Load(m_appName);
            Instantiate(bt);
            bundle.Unload(false);
            yield return null;
        }
    }
}