using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Editor.EditorWindows.CustomToolsWindow.Custom
{
    class DrawPropertyInfo
    {
        internal SerializedProperty Property { get; }
        internal CustomPropertyAttribute PropertyAttribute { get; }
        public DrawPropertyInfo(SerializedProperty serializedProperty, CustomPropertyAttribute propertyAttribute)
        {
            this.Property = serializedProperty;
            this.PropertyAttribute = propertyAttribute;
        }
    }

    class DrawMethodInfo
    {
        [CanBeNull] internal FoldoutGroupAttribute FoldoutGroupAttribute { get; }
        internal List<Action> ActionList { get; private set; }

        public DrawMethodInfo(FoldoutGroupAttribute foldoutGroupAttribute, Action action)
        {
            ActionList = new List<Action>();
            this.FoldoutGroupAttribute = foldoutGroupAttribute;
            ActionList.Add(action);
            ActionList = ActionList.Distinct().ToList();
        }

        public void Add(Action action)
        {
            ActionList.Add(action);
            ActionList = ActionList.Distinct().ToList();
        }
    }

    public class CustomPropertyAttributeDrawer
    {
        private Vector2 _scrollPosition = Vector2.zero;
        private SerializedObject serializedObject = null;
        private readonly GUIContent label = new GUIContent();
        private readonly List<DrawPropertyInfo> targetList = new List<DrawPropertyInfo>();
        private Dictionary<string, DrawMethodInfo> targetFuncList = new Dictionary<string, DrawMethodInfo>();

        public void Initialize(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            this.serializedObject.Update();
            var iterator = this.serializedObject.GetIterator();
            while (iterator.NextVisible(true))
            {
                GetDrawTarget(iterator);
            }
            this.serializedObject.ApplyModifiedProperties();
        }

        public void Draw()
        {
            this.BeginDraw();
            this.DrawProperties();
            this.DrawMethods();
            this.EndDraw();
        }
        private void BeginDraw()
        {
            serializedObject.Update();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        }

        private void DrawProperties()
        {
            foreach (var item in targetList)
            {
                using (new EditorGUI.DisabledScope(item.PropertyAttribute.IsReadOnly))
                {
                    label.text = item.Property.displayName;
                    EditorGUILayout.PropertyField(item.Property, label);
                }
            }
        }

        private void DrawMethods()
        {
            foreach (var item in targetFuncList)
            {
                if (item.Key != "")
                {
                    if (item.Value.FoldoutGroupAttribute is null) continue;
                    
                    EditorGUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"));
                    item.Value.FoldoutGroupAttribute.Expanded = EditorGUILayout.Foldout(item.Value.FoldoutGroupAttribute.Expanded, item.Key, true, EditorStyles.foldout);
                    CoreEditorUtils.DrawSplitter(true);
                    EditorGUILayout.Separator();
                    if (item.Value.FoldoutGroupAttribute.Expanded)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var action in item.Value.ActionList) 
                            action.Invoke();
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    foreach (var action in item.Value.ActionList)
                    {
                        action.Invoke();
                    }
                }
            }
        }

        private void EndDraw()
        {
            EditorGUILayout.EndScrollView();
            serializedObject.ApplyModifiedProperties();
        }
        
        private void GetDrawTarget(SerializedProperty property)
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
                        // Finde CustomPropertyAttribute
                        if (attr.GetType() == typeof(ListDrawerSettingsAttribute))
                            targetList.Add(new DrawPropertyInfo(property.Copy(), attr as ListDrawerSettingsAttribute));
                    }
                }

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (methods.Length != 0)
                {
                    foreach (var method in methods)
                    {
                        var attributes = method.GetCustomAttributes(true);

                        if (attributes.Any(attr => attr.GetType() == typeof(OnInspectorGUIAttribute)))
                        {
                            string groupID = "";
                            Action action = (Action) Delegate.CreateDelegate(typeof(Action), serializedObject.targetObject, method);
                            
                            if (attributes.Any(attr => attr.GetType() == typeof(FoldoutGroupAttribute)))
                            {
                                var res = attributes.Where(attr => attr.GetType() == typeof(FoldoutGroupAttribute));
                                foreach (FoldoutGroupAttribute attr in res)
                                {
                                    groupID = attr.GroupID;
                                    if (targetFuncList.ContainsKey(groupID))
                                        targetFuncList[groupID].Add(action);
                                    else
                                        targetFuncList[groupID] = new DrawMethodInfo(attr, action);
                                }
                            }
                            else
                            {
                                if (targetFuncList.ContainsKey(groupID))
                                    targetFuncList[groupID].Add(action);
                                else
                                    targetFuncList[groupID] = new DrawMethodInfo(null, action);
                            }
                        }
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
}
