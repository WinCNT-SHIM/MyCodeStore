using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[Serializable]
public class SceneInfoData
{
    [SerializeField] private string assetPath = default;
    [SerializeField] private string AAAAAAAAA = default;
    [SerializeField] private string BBBBBBBBB = default;
    public SceneInfoData() { }

    public SceneInfoData(string assetPath)
    {
        this.assetPath = assetPath;
        this.AAAAAAAAA = "AAAAAAAAA";
        this.BBBBBBBBB = "BBBBBBBBB";
    }
}

public class MyCustomWindow : EditorWindow
{
    [MenuItem("Custom Tools/My Custom Window")]
    public static void ShowWindow()
    {
        GetWindow<MyCustomWindow>("My Custom Window");
    }

    [CustomDisable, SerializeField] private List<SceneInfoData> sceneInfoList = new List<SceneInfoData>();
    SerializedObject targetObject;
    
    private void OnEnable()
    {
        targetObject = new SerializedObject(this);
        sceneInfoList.Clear();
        sceneInfoList.Add(new SceneInfoData("111"));
        sceneInfoList.Add(new SceneInfoData("222"));
        sceneInfoList.Add(new SceneInfoData("333"));
        sceneInfoList.Add(new SceneInfoData("444"));
        sceneInfoList.Add(new SceneInfoData("555"));
    }

    private void OnGUI()
    {
        if (targetObject != null)
        {
            targetObject.Update();
            
            // List
            SerializedProperty prop = targetObject.FindProperty("sceneInfoList");
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(prop, new GUIContent(prop.displayName));
            EditorGUI.EndDisabledGroup();
            
            targetObject.ApplyModifiedProperties();
        }
    }
}