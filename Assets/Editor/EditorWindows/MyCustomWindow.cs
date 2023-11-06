using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[Serializable]
public class SceneInfoData
{
    [SerializeField] private string assetPath = default;
    public SceneInfoData() { }
    public SceneInfoData(string assetPath) { this.assetPath = assetPath; }
}

public class MyCustomWindow : EditorWindow
{
    [MenuItem("Custom Tools/My Custom Window")]
    public static void ShowWindow()
    {
        GetWindow<MyCustomWindow>("My Custom Window");
    }

    [SerializeField] private List<SceneInfoData> sceneInfoList = new List<SceneInfoData>();
    SerializedObject targetObject;
    
    private void OnEnable()
    {
        targetObject = new SerializedObject(this);
        sceneInfoList.Clear();
        sceneInfoList.Add(new SceneInfoData("111"));
        sceneInfoList.Add(new SceneInfoData("333"));
    }

    private void OnGUI()
    {
        if (targetObject != null)
        {
            // Draw the inspector GUI for the target object
            targetObject.Update();
            SerializedProperty prop = targetObject.FindProperty("sceneInfoList");
            EditorGUILayout.PropertyField(prop, new GUIContent(prop.displayName));
            targetObject.ApplyModifiedProperties();
        }
    }
}