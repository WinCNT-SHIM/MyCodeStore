using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

[Serializable]
public class SceneInfoData
{
    [SerializeField] private string assetPath = default;
    // [SerializeField] private string AAAAAAAAA = default;
    // [SerializeField] private string BBBBBBBBB = default;

    public SceneInfoData(string assetPath)
    {
        this.assetPath = assetPath;
        // this.AAAAAAAAA = "AAAAAAAAA";
        // this.BBBBBBBBB = "BBBBBBBBB";
    }
}

public class MyCustomWindow : EditorWindow
{
    private readonly List<SerializedProperty> targetList = new List<SerializedProperty>();
    
    [MenuItem("Custom Tools/My Custom Window")]
    public static void ShowWindow()
    {
        GetWindow<MyCustomWindow>("My Custom Window");
    }

    [CustomDisable, SerializeField] private List<SceneInfoData> sceneInfoList = new List<SceneInfoData>();
    [CustomDisable][SerializeField] private List<SceneInfoData> sceneInfoList2 = new List<SceneInfoData>();
    [CustomDisable(IsInitialized = false), SerializeField] private List<SceneInfoData> sceneInfoList3 = new List<SceneInfoData>();
    SerializedObject targetObject;
    
    private void OnEnable()
    {
        targetObject = new SerializedObject(this);
        targetObject.Update();
        
        var iterator = targetObject.GetIterator();
        while (iterator.NextVisible(true))
        {
            GetTargetProperty(targetObject, iterator);
        }
        targetObject.ApplyModifiedProperties();
        
        sceneInfoList.Clear();
        sceneInfoList.Add(new SceneInfoData("111"));
        sceneInfoList.Add(new SceneInfoData("222"));
        sceneInfoList.Add(new SceneInfoData("333"));
        sceneInfoList.Add(new SceneInfoData("444"));
        sceneInfoList.Add(new SceneInfoData("555"));
        
        sceneInfoList2.Clear();
        sceneInfoList2.Add(new SceneInfoData("222"));
        sceneInfoList2.Add(new SceneInfoData("444"));
        sceneInfoList2.Add(new SceneInfoData("666"));
        
        sceneInfoList3.Clear();
        sceneInfoList3.Add(new SceneInfoData("111"));
        sceneInfoList3.Add(new SceneInfoData("333"));
        sceneInfoList3.Add(new SceneInfoData("555"));
    }

    private void OnGUI()
    {
        if (targetObject != null)
        {
            targetObject.Update();
            
            // // List
            // SerializedProperty prop = targetObject.FindProperty("sceneInfoList");
            //
            // EditorGUI.BeginDisabledGroup(true);
            // EditorGUILayout.PropertyField(prop, new GUIContent(prop.displayName));
            // EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(true);
            foreach (var item in targetList)
            {
                EditorGUILayout.PropertyField(item, new GUIContent(item.displayName));
            }
            EditorGUI.EndDisabledGroup();
            
            targetObject.ApplyModifiedProperties();
        }
    }
    
    private void GetTargetProperty(SerializedObject serializedObject, SerializedProperty property)
    {
        try
        {
            var type = serializedObject.targetObject.GetType();
            FieldInfo field = type.GetField(property.propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                object[] attributes = field.GetCustomAttributes(true);
                foreach (var attr in attributes)
                {
                    // CBPropertyAttributeを継承したAttributeを持っているプロパティを描画対象にする
                    if (attr.GetType() == typeof(CustomDisableAttribute))
                        targetList.Add(property.Copy());
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
            throw;
        }
    }
}