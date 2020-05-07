using Object = UnityEngine.Object;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ParticleSystemSortingSetterWindows : EditorWindow
{
    string m_SortingOrderMapPathName = "ParticleSystemExt/Editor/SortingOrderMapping.json";
    
    string searchRefsPath = "Assets";

    Dictionary<string, int> matSoringMap = new Dictionary<string, int>();
    int currentMaxSorting = 0;
    List<string> searchedMaterials = new List<string>();
    bool checkGPUInstancing = false;

    static ParticleSystemSortingSetterWindows widnow;
    [MenuItem("优化工具/粒子系统/粒子sorting设置")]
    static void ShowEditor()
    {
        // Get window reference.

        ParticleSystemSortingSetterWindows window =
            GetWindow<ParticleSystemSortingSetterWindows>(false, "粒子sorting设置");

        // Static init.

        // ...

        // Invoke non-static init.

        window.Initialize();
    }

    void Initialize()
    {
        LoadMappingFile();
    }

    private void OnGUI()
    {
        Event ev = Event.current;
        GUILayout.BeginVertical();

        GUILayout.Space(10);
        {
            GUILayout.Label("粒子预制搜索目录");
            GUILayout.Label(searchRefsPath);
            if (GUILayout.Button("选择要刷新的粒子预制的顶级目录"))
            {
                var path = "";
                int index = -1;
                while (index < 0)
                {
                    path = EditorUtility.OpenFolderPanel("选择要刷新的粒子预制的顶级目录", "", "");
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
                searchRefsPath = path;
            }
        }

        /*GUILayout.Space(10);
        if (GUILayout.Button("搜集材质信息(must)"))
        {
            SearchMaterials();
        }*/
        /*GUILayout.Space(10);
        if (GUILayout.Button("检查重复材质"))
        {
            FindDupMaterials();
        }*/
        checkGPUInstancing = EditorGUILayout.Toggle("使用GPU Instancing?", checkGPUInstancing);
        GUILayout.Space(10);
        if (GUILayout.Button("开始刷新"))
        {
            if(!RefreshRefs())
            {
                EditorUtility.DisplayDialog("提示", "没什么可做的", "明白了");
            }
            else
            {
                //有必要就刷新
                RefreshMappingFile();
            }
        }
        GUILayout.EndVertical();
    }

    void OnSelectionChange()
    {        
        Repaint();
    }

    void SearchMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { searchRefsPath });
        float maxCount = guids.Length;
        float curIndex = 0;
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            EditorUtility.DisplayProgressBar("材质搜集中", string.Format("{0}/{1}", curIndex + 1, maxCount), (curIndex + 1) / maxCount);

            string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);
            float percent = (float)++curIndex / (float)maxCount;
            int maxCount2 = dependencies.Length;
            int curIndex2 = 0;
            foreach (var dependency in dependencies)
            {
                EditorUtility.DisplayProgressBar("材质搜集中" + "...", percent.ToString("0%") + ":" + (++curIndex2).ToString() + "/" + maxCount2.ToString(), percent);
                //是材质
                if(dependency.EndsWith(".mat"))
                {
                    if (!searchedMaterials.Contains(assetPath))
                    {
                        searchedMaterials.Add(assetPath);
                    }
                }
            }
        }
        EditorUtility.DisplayProgressBar("材质搜集完成", "100%", 1);
        EditorUtility.ClearProgressBar();
    }

    void FindDupMaterials()
    {
        string outputFile =EditorUtility.SaveFilePanel("保存至", "", "检查结果", ".txt");



    }


    bool RefreshRefs()
    {
        EditorUtility.DisplayProgressBar("进度", string.Format("步骤1/2:搜集引用中.."),0);
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { searchRefsPath });
        float cur = 0;
        float all = guids.Length;
        if (all == 0) 
        {
            EditorUtility.ClearProgressBar();
            return false;
        }
        for(int i = 0; i < all; ++i)
        {
            EditorUtility.DisplayProgressBar("进度", string.Format("[{0}/{1}]", i+1, all), (i + 1) / all);
            string guid = guids[i];
            string asset = AssetDatabase.GUIDToAssetPath(guid);
            if (asset.EndsWith(".prefab"))
            {
                var go = (GameObject)AssetDatabase.LoadMainAssetAtPath(asset);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(go);
                bool changed = false;
                var pses = instance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var pse in pses)
                {
                    var psr = pse.GetComponent<ParticleSystemRenderer>();
                    if(psr)
                    {
                        Material mat = null;
                        foreach (var m in psr.sharedMaterials)
                        {
                            if (m != null)
                            {
                                mat = m;
                                break;
                            }
                        }

                        if (mat)
                        {
                            if (psr.renderMode == ParticleSystemRenderMode.Billboard
                                || psr.renderMode == ParticleSystemRenderMode.HorizontalBillboard
                                || psr.renderMode == ParticleSystemRenderMode.VerticalBillboard
                                || psr.renderMode == ParticleSystemRenderMode.Stretch)
                            {
                                string filename = AssetDatabase.GetAssetPath(mat);

                                int sortingOrder = 0;
                                if (!matSoringMap.TryGetValue(filename, out sortingOrder))
                                {
                                    sortingOrder = ++currentMaxSorting;
                                    matSoringMap.Add(filename, sortingOrder);
                                }
                                psr.sortingOrder = sortingOrder;
                                EditorUtility.SetDirty(pse);
                                changed = true;
                            }
                            else if (psr.renderMode == ParticleSystemRenderMode.Mesh 
                                && psr.enableGPUInstancing != checkGPUInstancing)
                            {
                                psr.enableGPUInstancing = checkGPUInstancing;
                                EditorUtility.SetDirty(pse);
                                changed = true;
                            }
                        }
                    }
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


    void RefreshMappingFile()
    {
        try
        {
            JObject root = new JObject();
            JArray jlist = new JArray();
            foreach (var it in matSoringMap)
            {
                JObject jo = new JObject();
                jo["mat"] = it.Key;
                jo["order"] = it.Value.ToString();
                jlist.Add(jo);
            }
            root["mapping"] = jlist;
            string bytes = Newtonsoft.Json.JsonConvert.SerializeObject(root);
            string path = Application.dataPath + "/" + m_SortingOrderMapPathName;
            try
            {
                FileStream f = new FileStream(path, FileMode.Truncate);
                f.Close();
            }
            catch (Exception e)
            {
                
            }
            FileStream file = new FileStream(path, FileMode.OpenOrCreate);
            if (!file.CanWrite)
            {
                Debug.LogError("找不到文件: " + m_SortingOrderMapPathName);
                return;
            }
            using (TextWriter streamwriter = new StreamWriter(file))
            {
                streamwriter.Write(bytes);
            }
            file.Close();
        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    void LoadMappingFile()
    {
        currentMaxSorting = 0;
        matSoringMap.Clear();

        string path = Application.dataPath + "/" + m_SortingOrderMapPathName;
        FileStream file = new FileStream(path, FileMode.Open);
        if (file == null || !file.CanRead)
        {
            Debug.LogWarningFormat("找不到文件: ", m_SortingOrderMapPathName);
            return;
        }

        long fileend = file.Seek(0, SeekOrigin.End);

        file.Seek(0, SeekOrigin.Begin);

        byte[] byData = new byte[fileend + 1];
        file.Read(byData, 0, (int)fileend + 1);
        string jsonString = System.Text.Encoding.UTF8.GetString(byData);
        file.Close();
        JObject jo = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
        if (jo == null) return;
        var it = jo.GetEnumerator();
        try
        {
            JArray jlist = JArray.Parse(jo["mapping"].ToString());
            for (int i = 0; i < jlist.Count; ++i)
            {
                JObject tempo = JObject.Parse(jlist[i].ToString());
                string mat = tempo["mat"].ToString();
                string sorder = tempo["order"].ToString();
                int order = int.Parse(sorder);
                if(!matSoringMap.ContainsKey(mat))
                {
                    matSoringMap.Add(mat, order);
                }
                else
                {
                    matSoringMap[mat] = order;
                }
                if(order > currentMaxSorting)
                {
                    currentMaxSorting = order;
                }
            }

        }
        catch (Exception e)
        {
            Debug.LogErrorFormat("Error:{0} " , e.Message);
        }

    }
}
