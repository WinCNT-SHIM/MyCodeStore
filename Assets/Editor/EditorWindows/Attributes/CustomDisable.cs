using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CustomDisableAttribute : PropertyAttribute
{
    public bool IsInitialized { get; set; }
}

[CustomPropertyDrawer(typeof(CustomDisableAttribute))]
public class DisableDrawer : PropertyDrawer
{
    private static GUIStyle style = new GUIStyle(EditorStyles.label);
    private static Texture2D texture = new Texture2D(1, 1);
    private bool isEven = false;
    
    private CustomDisableAttribute attr { get { return (CustomDisableAttribute)attribute; } }

    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = 0f;
        // プロパティ１つの高さ（適切なPaddingあり）
        var singleLineHeight = base.GetPropertyHeight(property, label) + EditorGUIUtility.standardVerticalSpacing;
        // 表示するプロパティの数 - property.Copy().CountInProperty()は１を返すことがあり、修正
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
        
        // 奇数行、偶数行の背景の色を変える
        if (isEven)
            style.normal.background = texture;
        else
            style.normal.background = null;
        isEven = !isEven;
        
        position.y -= EditorGUIUtility.standardVerticalSpacing / 2;
        EditorGUI.LabelField(position, GUIContent.none, style);
        
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.BeginProperty(position, label, property);
        
        position.y += EditorGUIUtility.standardVerticalSpacing;
        
        var enumerator = property.GetEnumerator();
        int depth = property.depth;
        while (enumerator.MoveNext())
        {
            var prop = enumerator.Current as SerializedProperty;
            if (prop == null || prop.depth > depth + 1) 
                continue;

            position.height = base.GetPropertyHeight(prop, null);
            EditorGUI.PropertyField(position, prop, new GUIContent(prop.displayName));
            position.y += base.GetPropertyHeight(prop, new GUIContent(prop.displayName)) + EditorGUIUtility.standardVerticalSpacing;
        }
        EditorGUI.EndProperty();
        
        EditorGUI.EndDisabledGroup();
    }
}