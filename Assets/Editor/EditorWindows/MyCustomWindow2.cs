using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

public class MyCustomWindow2 : EditorWindow
{
    [MenuItem("Custom Tools/My Custom Window2")]
    public static void ShowWindow()
    {
        GetWindow<MyCustomWindow2>("My Custom Window");
    }

    [SerializeField, CustomDisable] private List<SceneInfoData> autoLightmapTargetSceneInfo = new List<SceneInfoData>();
    [SerializeField, ListDrawerSettings] private List<SceneInfoData> autoLightmapTargetSceneInfo2 = new List<SceneInfoData>();
    SerializedObject targetObject;

    private void OnEnable()
    {
        targetObject = new SerializedObject(this);
        
        autoLightmapTargetSceneInfo.Add(new SceneInfoData("111"));
        autoLightmapTargetSceneInfo.Add(new SceneInfoData("333"));
        
        // autoLightmapTargetSceneInfo2.Add(new SceneInfoData("555"));
        // autoLightmapTargetSceneInfo2.Add(new SceneInfoData("777"));
        
        // // Load the target object (your script) for editing
        // GameObject selectedObject = Selection.activeGameObject;
        // if (selectedObject != null)
        // {
        //     YourScript yourScript = selectedObject.GetComponent<YourScript>();
        //     if (yourScript != null)
        //     {
        //         targetObject = new SerializedObject(yourScript);
        //     }
        // }
    }

    private void OnGUI()
    {
        if (targetObject != null)
        {
            // Draw the inspector GUI for the target object
            targetObject.Update();

            // SerializedProperty yourProperty = targetObject.FindProperty("autoLightmapTargetSceneInfo");
            //
            // if (CustomAttributeExists<DisableAttribute>(yourProperty))
            // {
            //     EditorGUILayout.LabelField("Your Property has a " + "DisableAttribute");
            // }

            // EditorGUILayout.PropertyField(yourProperty, new GUIContent("Your Property Name"));
            
            var iterator = targetObject.GetIterator();
            while (iterator.NextVisible(true))
            {
                if (CustomAttributeExists<CustomDisableAttribute>(iterator))
                {
                    autoLightmapTargetSceneInfo.Clear();
                    autoLightmapTargetSceneInfo.Add(new SceneInfoData("111"));
                    autoLightmapTargetSceneInfo.Add(new SceneInfoData("333"));
                    
                    // EditorGUILayout.LabelField(iterator.name);
                    EditorGUILayout.LabelField(iterator.displayName + " has a DisableAttribute");
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(iterator, new GUIContent("Disable"));
                    EditorGUI.EndDisabledGroup();
                }
                else if (CustomAttributeExists<ListDrawerSettingsAttribute>(iterator))
                {
                    EditorGUILayout.LabelField(iterator.displayName + " has a ListDrawerSettingsAttribute");
                    EditorGUILayout.PropertyField(iterator, new GUIContent("List Drawer Settings"));
                }
                else
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField(iterator.displayName + " : Don't!!!");
                    EditorGUI.indentLevel--;
                }
            }

            targetObject.ApplyModifiedProperties();
        }
        else
        {
            EditorGUILayout.LabelField("Select a GameObject with YourScript attached.");
        }
    }

    // Method to check if a specific Custom Attribute exists on a SerializedProperty
    private bool CustomAttributeExists<T>(SerializedProperty property) where T : Attribute
    {
        try
        {
            // FieldInfo field = targetObject.targetObject.GetType().GetField(property.propertyPath);
            var type = this.GetType();
            FieldInfo field = type.GetField(property.propertyPath, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                object[] attributes = field.GetCustomAttributes(typeof(T), true);
                return attributes.Length > 0;
            }
            return false;
        }
        catch (Exception e)
        {
            // Console.WriteLine(e);
            Debug.Log(e);
            throw;
        }
    }
}