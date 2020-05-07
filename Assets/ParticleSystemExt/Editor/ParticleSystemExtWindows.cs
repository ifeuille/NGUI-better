using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Object = UnityEngine.Object;

public class ParticleSystemExtWindows : EditorWindow
{
    string searchPath = "Assets";
    List<string> atlasPath = new List<string>();
    UnityEngine.Object[] atlasAssetObject = new UnityEngine.Object[1];

    static ParticleSystemExtWindows widnow;
    [MenuItem("性能优化/粒子系统/批量刷新图集关联的粒子")]
    static void ShowEditor()
    {
        // Get window reference.

        ParticleSystemExtWindows window =
            GetWindow<ParticleSystemExtWindows>(false, "批量刷新图集关联的粒子");

        // Static init.

        // ...

        // Invoke non-static init.

        window.Initialize();
    }

    void Initialize()
    {

    }

    private void OnGUI()
    {
        Event ev = Event.current;
        GUILayout.BeginVertical();

        GUILayout.Label("选择需要刷新引用的图集");
        int num = EditorGUILayout.DelayedIntField("图集数", atlasAssetObject.Length);
        if (num != atlasAssetObject.Length && num > 0)
        {
            UnityEngine.Object[] tatlasValueList = new Object[num];
            for (int i = 0; i < num && i < atlasAssetObject.Length; ++i)
            {
                tatlasValueList[i] = atlasAssetObject[i];
            }
            atlasAssetObject = tatlasValueList;
        }

        GUILayout.BeginVertical();
        for (int i = 0; i < atlasAssetObject.Length; ++i)
        {
            GUILayout.BeginVertical();
            atlasAssetObject[i] = EditorGUILayout.ObjectField(string.Format("第{0}个", i),atlasAssetObject[i], typeof(UnityEngine.Object),false);
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();
        GUILayout.Space(10);
        {
            GUILayout.Label(searchPath);
            if (GUILayout.Button("选择要刷新的资源的顶级目录"))
            {
                var path = "";
                int index = -1;
                while (index < 0)
                {
                    path = EditorUtility.OpenFolderPanel("选择要刷新的资源的顶级目录", "", "");
                    index = path.IndexOf("Assets");
                    if (index < 0)
                    {
                        bool result = EditorUtility.DisplayDialog("提醒", "请选择工程Assets目录下的文件夹", "重试", "退出");
                        if (!result)
                        {
                            path = "Assets";
                            break;
                        }
                    }
                    else
                    {
                        path = path.Substring(index);
                    }
                }
                searchPath = path;
            }
        }
        GUILayout.Space(10);
        if(GUILayout.Button("开始刷新"))
        {
            if(!RefreshAllRefs())
            {
                EditorUtility.DisplayDialog("提示", "没什么可做的", "明白了");
            }
        }

        GUILayout.EndVertical();
    }

    void OnSelectionChange()
    {        
        Repaint();
    }


    bool RefreshAllRefs()
    {
        atlasPath.Clear();
        foreach (var atlasObj in atlasAssetObject)
        {
            try
            {
                var path = AssetDatabase.GetAssetPath(atlasObj);
                if (path != "" && hasAtlas(path))
                {
                    if(atlasPath.Contains(path))
                    {
                        continue;
                    }
                    atlasPath.Add(path);
                }
            }catch(Exception e)
            {
                Debug.LogError(e.Message);
                continue;
            }
        }
        if (atlasPath.Count == 0) return false;
        EditorUtility.DisplayProgressBar("进度", string.Format("步骤1/2:搜集引用中.."),0);
        var dependencies = FindReferenceAssets(atlasPath, searchPath);// AssetDatabase.GetDependencies(atlasPath.ToArray(), false);
        float cur = 0;
        float all = dependencies.Count;
        if (all == 0) return false;
        for(int i = 0; i < all; ++i)
        {
            EditorUtility.DisplayProgressBar("进度", string.Format("步骤2/2:刷新[{0}/{1}]", cur, all), cur / all);
            string asset = dependencies[i];
            if (asset.EndsWith(".prefab"))
            {
                var go = (GameObject)AssetDatabase.LoadMainAssetAtPath(asset);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(go);
                bool changed = false;
                var pses = instance.GetComponentsInChildren<ParticleSystemExt>(true);
                foreach (var pse in pses)
                {
                    pse.RefreshParticleSystem();
                    EditorUtility.SetDirty(pse);
                    changed = true;
                }
                if (changed)
                {
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                    Debug.Log("replace success:" + asset);
                }
                else
                {
                    Debug.Log("ignore(no ref sprite):" + asset);
                }
                GameObject.DestroyImmediate(instance);
            }
            else
            {
                Debug.Log("ignore(not prefab):" + asset);
            }
        }

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("成功", "ok了", "好的");
        System.GC.Collect();
        return true;
    }

    public static List<string> FindReferenceAssets(List<string> paths,string root)
    {
        List<string> retList = new List<string>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { root });
        int maxCount = guids.Length;
        int curIndex = 0;
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);
            float percent = (float)++curIndex / (float)maxCount;
            int maxCount2 = dependencies.Length;
            int curIndex2 = 0;
            foreach (var dependency in dependencies)
            {
                EditorUtility.DisplayProgressBar("引用资源搜寻中" + "...", percent.ToString("0%") + ":" + (++curIndex2).ToString() + "/" + maxCount2.ToString(), percent);
                if (paths.Contains(dependency) && !retList.Contains(assetPath))
                {
                    retList.Add(assetPath);
                }
            }
        }
        EditorUtility.ClearProgressBar();
        return retList;
    }


    bool hasAtlas(string atlasPath)
    {
        //if (!AssetDatabase.IsValidFolder(atlasPath)) return false;
        var go = (GameObject)AssetDatabase.LoadMainAssetAtPath(atlasPath);
        if (go == null) return false;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(go);
        if (instance == null) return false;
        var haveatlas = instance.GetComponentInChildren<UIAtlas>() != null;
        GameObject.DestroyImmediate(instance);
        return haveatlas;
    }
}
