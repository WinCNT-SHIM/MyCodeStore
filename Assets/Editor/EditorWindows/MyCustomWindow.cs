using System;
using System.Collections.Generic;
using System.Linq;
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
    private List<GameObject> _selectionGameObjects = new List<GameObject>();
    private readonly List<SerializedProperty> targetList = new List<SerializedProperty>();
    private List<Action> targetMethodList = new List<Action>();
    
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
            GetTargetMethod(targetObject, iterator);
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
        // DrawSelectionVertexCounter();
        
        if (targetObject != null)
        {
            targetObject.Update();

            foreach (var method in targetMethodList)
            {
                method.Invoke();
            }
            
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
    
    #region Selected GameObject Vertex-Count
    [OnInspectorGUI]
    private void DrawSelectionVertexCounter()
    {
        _selectionGameObjects = Selection.gameObjects.ToList();
        EditorGUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"));
        EditorGUILayout.LabelField("Selected GameObject Vertex-Count", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        {
            EditorGUILayout.IntField("Skin", CountMeshVertex(GetAllSkinnedMeshRenderers(_selectionGameObjects)));
            EditorGUILayout.IntField("mesh", CountMeshVertex(GetAllMeshFilter(_selectionGameObjects)));
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
        
        Repaint();
    }
    #endregion

    #region Private Methods
    private List<SkinnedMeshRenderer> GetAllSkinnedMeshRenderers(List<GameObject> rootGameObject)
    {
        var meshes = new List<SkinnedMeshRenderer>();
        foreach (var gameObject in rootGameObject)
            meshes.AddRange(gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        return meshes.OrderByDescending(x => x.sharedMesh != null ? x.sharedMesh.vertexCount : -1).ToList();
    }
    private List<MeshFilter> GetAllMeshFilter(List<GameObject> rootGameObject)
    {
        var meshes = new List<MeshFilter>();
        foreach (var gameObject in rootGameObject)
            meshes.AddRange(gameObject.GetComponentsInChildren<MeshFilter>(true));
        return meshes.OrderByDescending(x => x.sharedMesh != null ? x.sharedMesh.vertexCount : -1).ToList();
    }

    private static int CountMeshVertex(List<SkinnedMeshRenderer> list)
    {
        return list
            .Where(i => i is not null && i.sharedMesh is not null)
            .Where(i => i.gameObject.activeInHierarchy)
            .Sum(i => i.sharedMesh.vertices.Length);
    }
    private static int CountMeshVertex(List<MeshFilter> list)
    {
        return list
            .Where(i => i is not null && i.sharedMesh is not null)
            .Where(i => i.gameObject.activeInHierarchy)
            .Sum(i => i.sharedMesh.vertices.Length);
    }
    #endregion
    
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
    
    private void GetTargetMethod(SerializedObject serializedObject, SerializedProperty property)
    {
        try
        {
            var type = serializedObject.targetObject.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (methods.Length != 0)
            {
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(true);
                    foreach (var attr in attributes)
                    {
                        if (attr.GetType() == typeof(OnInspectorGUIAttribute))
                        {
                            Action action = (Action) Delegate.CreateDelegate(typeof(Action), serializedObject.targetObject, method);
                            targetMethodList.Add(action);
                        }
                    }
                }
            }
            // duplicates remove
            targetMethodList = targetMethodList.Distinct().ToList();
        }
        catch (Exception e)
        {
            Debug.Log(e);
            throw;
        }
    }
}