using System;
using UnityEditor;
using UnityEngine;

namespace Editor.EditorWindows.CustomToolsWindow.Custom
{
    #region OnInspectorGUI
    public class OnInspectorGUIAttribute : Attribute { }
    #endregion

    #region FoldoutGroup
    public class FoldoutGroupAttribute : Attribute
    {
        public string GroupID;
        private bool expanded;
        public bool Expanded
        {
            get => this.expanded;
            set => this.expanded = value;
        }
        
        public FoldoutGroupAttribute(string groupName = "", bool expanded = true)
        {
            GroupID = groupName;
            Expanded = expanded;
        }
    }
    #endregion
    
    #region ListDrawerSettingsAttribute
    public class ListDrawerSettingsAttribute : CustomPropertyAttribute{ }
    
    [CustomPropertyDrawer(typeof(ListDrawerSettingsAttribute))]
    public class ListDrawerSettingsDrawer : PropertyDrawer
    {
        private static GUIStyle style = new GUIStyle(EditorStyles.label);
        private static Texture2D texture = new Texture2D(1, 1);
        private bool isEven = false;
        private static GUIContent guiContent = new GUIContent();
        
        private ListDrawerSettingsAttribute attr { get { return (ListDrawerSettingsAttribute)attribute; } }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = 0f;
            // Height of Property(Padding)
            var singleLineHeight = base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing;
            // Count of Display Properties. Note) property.Copy().CountInProperty() may return 1, consider adjusting the code accordingly
            int propertyCount = 0;
            var enumerator = property.GetEnumerator();
            while (enumerator.MoveNext()) 
                propertyCount++;
        
            totalHeight = propertyCount * singleLineHeight;
        
            return totalHeight;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!attr.IsInitialized)
            {
                isEven = false;
                if (texture != null)
                {
                    texture.SetPixel(0, 0, Color.gray);
                    texture.Apply();
                }
                attr.IsInitialized = true;
                return;
            }
            
            if (isEven)
                style.normal.background = texture;
            else
                style.normal.background = null;
            isEven = !isEven;
            
            // Padding
            position.y -= EditorGUIUtility.standardVerticalSpacing / 2;
            EditorGUI.LabelField(position, GUIContent.none, style);
            
            EditorGUI.BeginProperty(position, label, property);
            // Padding
            position.y += EditorGUIUtility.standardVerticalSpacing;
            
            var enumerator = property.GetEnumerator();
            int depth = property.depth;
            
            while (enumerator.MoveNext())
            {
                var prop = enumerator.Current as SerializedProperty;
                if (prop == null || prop.depth > depth + 1) 
                    continue;
                
                position.height = base.GetPropertyHeight(prop, null);
                guiContent.text = prop.displayName;
                EditorGUI.PropertyField(position, prop, guiContent);

                position.y += base.GetPropertyHeight(prop, null) + EditorGUIUtility.standardVerticalSpacing;
            }
            
            EditorGUI.EndProperty();
        }
    }
    #endregion

    #region BasePropertyAttribute
    public class CustomPropertyAttribute : PropertyAttribute
    {
        private bool isReadOnly;
        private bool isReadOnlyHasValue;
        
        public bool IsReadOnly
        {
            get => this.isReadOnly;
            set
            {
                this.isReadOnly = value;
                this.isReadOnlyHasValue = true;
            }
        }
        public bool IsReadOnlyHasValue => this.isReadOnlyHasValue;

        public bool IsInitialized { get; set; }
    }
    #endregion
}