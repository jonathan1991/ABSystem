﻿using UnityEditor;
using UnityEngine;
using System.IO;
using LitJson;

namespace ABSystem
{
    public class CreateAssetBundles : EditorWindow
    {
        private static bool ISCreateVersionInfo = true;
        private static string Version;
        private static bool IsCreateResourceList = true;
        private static string OutputPath = "AssetBundles";

        [MenuItem("ABSystem/Create AssetBundles")]
        static void ShowWindow()
        {
            GetWindow(typeof(CreateAssetBundles), true, "Create AssetBundles");
        }

        private void OnGUI()
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            GUILayout.Label(string.Format("Current Version: {0}", GetCurrentVersion()), EditorStyles.helpBox);
            ISCreateVersionInfo = EditorGUILayout.Toggle("Create Version Info", ISCreateVersionInfo);
            if (ISCreateVersionInfo)
            {
                Version = EditorGUILayout.TextField("Version", Version);
            }
            IsCreateResourceList = EditorGUILayout.Toggle("Create Resource List", IsCreateResourceList);
            if (GUILayout.Button("Create"))
            {
                Create();
            }
            if (GUILayout.Button("Clear"))
            {
                Clear();
            }

        }

        private string GetCurrentVersion()
        {
            string filePath = Path.Combine(OutputPath, "Version.json");
            if (!File.Exists(filePath))
            {
                return "UnKnow";
            }
            else
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    return JsonMapper.ToObject<VersionInfo>(sr.ReadToEnd()).Version;
                }
            }
        }

        private void Create()
        {
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
                var manifest = BuildPipeline.BuildAssetBundles(OutputPath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
                if(manifest)
                {
                    CreateResourceListJsonFile(manifest);
                    CreateVersionJsonFile();
                }
                else
                {
                    Clear();
                }
                
            }
            else
            {
                var ab = AssetBundle.LoadFromFile(Path.Combine(OutputPath, "AssetBundles"));
                var oldManifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                var oldABList = ABUtility.CreateABListFromManifest(oldManifest);
                ab.Unload(true);
                var newManifest = BuildPipeline.BuildAssetBundles(OutputPath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
                if(newManifest)
                {
                    CreateResourceListJsonFile(newManifest);
                    CreateVersionJsonFile();
                    var newABList = ABUtility.CreateABListFromManifest(newManifest);
                    var deleteList = ABUtility.GetDeleteABList(oldABList, newABList);
                    foreach (var abinfo in deleteList)
                    {
                        File.Delete(Path.Combine(OutputPath,abinfo.Name));
                        File.Delete(Path.Combine(OutputPath, abinfo.Name + ".manifest"));
                    }
                }
                else
                {
                    Clear();
                }
            }
            
        }

        /// <summary>
        /// 生成Version.json信息
        /// </summary>
        private void CreateVersionJsonFile()
        {
            if (ISCreateVersionInfo)
            {
                JsonData versionJson = new JsonData();
                versionJson["Version"] = Version;
                string versionJsonStr = JsonMapper.ToJson(versionJson);
                using (StreamWriter sw = new StreamWriter(Path.Combine(OutputPath, "Version.json")))
                {
                    sw.Write(versionJsonStr);
                }
            }
        }

        /// <summary>
        /// 生成ResourceList.json信息
        /// </summary>
        private void CreateResourceListJsonFile(AssetBundleManifest manifest)
        {
            if (IsCreateResourceList)
            {
                var abList = ABUtility.CreateABListFromManifest(manifest);
                string jsonStr = JsonMapper.ToJson(abList);
                using (StreamWriter sw = new StreamWriter(Path.Combine(OutputPath, "ResourceList.json")))
                {
                    sw.Write(jsonStr);
                }
            }
        }

        /// <summary>
        /// 清空输出目录
        /// </summary>
        private void Clear()
        {
            Directory.Delete(OutputPath, true);
        }
    }

}
