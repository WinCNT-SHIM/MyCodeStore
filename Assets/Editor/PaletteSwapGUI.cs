using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomShaderGUI
{
    public class PaletteSwapGUI : ShaderGUI
    {
        #region Material Styles

        private static class Styles
        {
            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceType));
            
            // Categories
            public static readonly GUIContent ShaderSettingsText = EditorGUIUtility.TrTextContent("Shader Settings", "");
            public static readonly GUIContent MainSettingsText = EditorGUIUtility.TrTextContent("Main Settings", "");
            public static readonly GUIContent AdvancedText = EditorGUIUtility.TrTextContent("Advanced Settings", "");

            // Properties
            public static readonly GUIContent BaseColorText = EditorGUIUtility.TrTextContent("Base Color", "Albedo(rgb)");
            public static readonly GUIContent PaletteSwapText = EditorGUIUtility.TrTextContent("On / Off", "");
            public static readonly GUIContent PaletteSwapMaskText = EditorGUIUtility.TrTextContent("Palette Swap Mask", "");
            public static readonly GUIContent PaletteSwapMask1Text = EditorGUIUtility.TrTextContent("Mask 1", "");
            public static readonly GUIContent PaletteSwapMask2Text = EditorGUIUtility.TrTextContent("Mask 2", "");
            public static readonly GUIContent PaletteSwapMask3Text = EditorGUIUtility.TrTextContent("Mask 3", "");
        }

        #endregion

        #region Material Properties
        private MaterialProperty BaseColor { get; set; }
        private MaterialProperty BaseMap { get; set; }
        private MaterialProperty PaletteSwap { get; set; }
        private MaterialProperty PaletteSwapMask { get; set; }
        private MaterialProperty PaletteSwapMask1Color { get; set; }
        private MaterialProperty PaletteSwapMask2Color { get; set; }
        private MaterialProperty PaletteSwapMask3Color { get; set; }
        private MaterialProperty PaletteSwapMask1ColorMode { get; set; }
        private MaterialProperty PaletteSwapMask2ColorMode { get; set; }
        private MaterialProperty PaletteSwapMask3ColorMode { get; set; }
        #endregion

        #region Header Scope Properties

        [Flags]
        private enum Expandable
        {
            ShaderSettings = 1 << 0,
            MainSettings = 1 << 1,
            Advanced = 1 << 2,
        }

        private enum SurfaceType
        {
            Opaque,
            Cutoff
        }
        
        private uint _materialFilter => uint.MaxValue;

        private readonly MaterialHeaderScopeList _materialScopeList =
            new MaterialHeaderScopeList(uint.MaxValue & ~(uint)Expandable.Advanced);

        #endregion

        private bool _defaultInspector = false;
        private bool _firstTimeApply = true;
        private MaterialEditor _materialEditor;
        private readonly float _defaultFieldWidth = EditorGUIUtility.fieldWidth;
        private readonly float _defaultLabelWidth = EditorGUIUtility.labelWidth;
        
        public override void ValidateMaterial(Material material)
        {
        }
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (materialEditor == null)
                throw new ArgumentNullException("materialEditor");

            _materialEditor = materialEditor;
            Material material = materialEditor.target as Material;

            FindProperties(properties);

            if (_firstTimeApply)
            {
                // 折りたたみメニューのHeaderと中身描画を設定
                RegisterHeader(material, _materialEditor);
                _defaultInspector = false;
                _firstTimeApply = false;
            }
            
            // Default UIを表示する
            if (_defaultInspector)
            {
                if (GUILayout.Button("Change Custom UI"))
                {
                    _defaultInspector = false;
                }

                EditorGUILayout.Space();
                CoreEditorUtils.DrawSplitter();
                base.OnGUI(materialEditor, properties);
            }
            // Custom UIを表示する（初期値）
            else
            {
                if (GUILayout.Button("Change Default UI"))
                {
                    _defaultInspector = true;
                }

                EditorGUILayout.Space();
                materialEditor.SetDefaultGUIWidths();
                _materialScopeList.DrawHeaders(materialEditor, material);
                materialEditor.SetDefaultGUIWidths();
            }
        }

        void FindProperties(MaterialProperty[] props)
        {
            BaseColor = FindProperty(PaletteSwapProperty.BaseColor, props);
            BaseMap = FindProperty(PaletteSwapProperty.BaseMap, props);
            PaletteSwap = FindProperty(PaletteSwapProperty.PaletteSwap, props);
            PaletteSwapMask = FindProperty(PaletteSwapProperty.PaletteSwapMask, props);
            PaletteSwapMask1Color = FindProperty(PaletteSwapProperty.PaletteSwapMask1Color, props);
            PaletteSwapMask2Color = FindProperty(PaletteSwapProperty.PaletteSwapMask2Color, props);
            PaletteSwapMask3Color = FindProperty(PaletteSwapProperty.PaletteSwapMask3Color, props);
            PaletteSwapMask1ColorMode = FindProperty(PaletteSwapProperty.PaletteSwapMask1ColorMode, props);
            PaletteSwapMask2ColorMode = FindProperty(PaletteSwapProperty.PaletteSwapMask2ColorMode, props);
            PaletteSwapMask3ColorMode = FindProperty(PaletteSwapProperty.PaletteSwapMask3ColorMode, props);
        }

        private void RegisterHeader(Material material, MaterialEditor materialEditor)
        {
            var filter = (Expandable)_materialFilter;

            if (filter.HasFlag(Expandable.ShaderSettings))
                _materialScopeList.RegisterHeaderScope(Styles.ShaderSettingsText, (uint)Expandable.ShaderSettings, DrawShaderSettings);

            if (filter.HasFlag(Expandable.MainSettings))
                _materialScopeList.RegisterHeaderScope(Styles.MainSettingsText, (uint)Expandable.MainSettings, DrawMainSettings);
            
            if (filter.HasFlag(Expandable.Advanced))
                _materialScopeList.RegisterHeaderScope(Styles.AdvancedText, (uint)Expandable.Advanced, DrawAdvanced);
        }

        #region Draw Category
        private void DrawShaderSettings(Material material)
        {
            EditorGUIUtility.labelWidth = 0f;
            
            // Palette Swap
            if (PaletteSwap != null)
            {
                _materialEditor.ShaderProperty(PaletteSwap, Styles.PaletteSwapText);
                if (PaletteSwap.floatValue > 0.0f)
                {
                    EditorGUI.indentLevel++;
                    if (PaletteSwapMask != null)
                        _materialEditor.TexturePropertySingleLine(Styles.PaletteSwapMaskText, PaletteSwapMask);
                    
                    EditorGUI.indentLevel++;
                    if (PaletteSwapMask1ColorMode != null && PaletteSwapMask1Color != null)
                        DrawPropertyPaletteSwap(PaletteSwapMask1ColorMode, PaletteSwapMask1Color, Styles.PaletteSwapMask1Text);
                    if (PaletteSwapMask2ColorMode != null && PaletteSwapMask2Color != null)
                        DrawPropertyPaletteSwap(PaletteSwapMask2ColorMode, PaletteSwapMask2Color, Styles.PaletteSwapMask2Text);
                    if (PaletteSwapMask3ColorMode != null && PaletteSwapMask3Color != null)
                        DrawPropertyPaletteSwap(PaletteSwapMask3ColorMode, PaletteSwapMask3Color, Styles.PaletteSwapMask3Text);
                    EditorGUI.indentLevel--;

                    EditorGUI.indentLevel--;
                }
                else
                {
                    if (PaletteSwapMask != null)
                        PaletteSwapMask.textureValue = null;
                }
            }
            ResetGUIWidths();
        }

        private void DrawMainSettings(Material material)
        {
            GUILayout.Label("Texture", EditorStyles.boldLabel);
            
            // Base
            if (material.HasProperty(PaletteSwapProperty.BaseMap) && material.HasProperty(PaletteSwapProperty.BaseColor))
                _materialEditor.TexturePropertySingleLine(Styles.BaseColorText, BaseMap, BaseColor);
            // Tilling and Offset
            EditorGUI.indentLevel += 1;
            if (material.HasProperty(PaletteSwapProperty.BaseMap))
                _materialEditor.TextureScaleOffsetProperty(BaseMap);
            EditorGUI.indentLevel -= 1;
            
            // 区分線
            CoreEditorUtils.DrawSplitter();
        }
        
        private void DrawAdvanced(Material material)
        {
            if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
                _materialEditor.RenderQueueField();
            _materialEditor.EnableInstancingField();
            _materialEditor.DoubleSidedGIField();
        }

        #endregion

        #region Private Methods
        private void ResetGUIWidths()
        {
            EditorGUIUtility.fieldWidth = _defaultFieldWidth;
            EditorGUIUtility.labelWidth = _defaultLabelWidth;
        }

        private void DrawPropertyPaletteSwap(MaterialProperty paletteSwapMaskColorMode, MaterialProperty paletteSwapMaskColor, GUIContent label)
        {
            Rect rectForSingleLine = EditorGUILayout.GetControlRect(false);
            MaterialEditor.BeginProperty(rectForSingleLine, paletteSwapMaskColorMode);
            MaterialEditor.BeginProperty(rectForSingleLine, paletteSwapMaskColor);
            
            EditorGUI.BeginChangeCheck();
            rectForSingleLine.width /= 3;
            EditorGUI.LabelField(rectForSingleLine, label);
            rectForSingleLine.x += rectForSingleLine.width;
            rectForSingleLine.x += EditorGUIUtility.fieldWidth * 0.5f - 10f;
            PaletteSwapMode selected = (PaletteSwapMode)EditorGUI.EnumPopup(rectForSingleLine, GUIContent.none, (PaletteSwapMode)paletteSwapMaskColorMode.floatValue);
            rectForSingleLine.x -= EditorGUIUtility.fieldWidth * 0.5f - 10f;
            rectForSingleLine.x += rectForSingleLine.width;
            Color color = EditorGUI.ColorField(rectForSingleLine, GUIContent.none, paletteSwapMaskColor.colorValue);
            if (EditorGUI.EndChangeCheck())
            {
                paletteSwapMaskColorMode.floatValue = (float)selected;
                paletteSwapMaskColor.colorValue = color;
            }
            
            MaterialEditor.EndProperty();
            MaterialEditor.EndProperty();
        }
        #endregion
    }
}