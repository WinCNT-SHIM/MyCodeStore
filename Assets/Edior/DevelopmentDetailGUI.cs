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
    public class DevelopmentDetailGUI : ShaderGUI
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
            public static readonly GUIContent SurfaceTypeText = EditorGUIUtility.TrTextContent("Surface Type", "Opaque / Cutoff");
            public static readonly GUIContent CutoffText = EditorGUIUtility.TrTextContent("Cutoff", "Culling Mode");
            public static readonly GUIContent CullModeText = EditorGUIUtility.TrTextContent("Cull Mode", "Culling Mode");
            public static readonly GUIContent BaseColorText = EditorGUIUtility.TrTextContent("Base Color", "Albedo(rgb)");
            public static readonly GUIContent BumpMapText = EditorGUIUtility.TrTextContent("BumpMap", "BumpMap");
            public static readonly GUIContent EmissionMap = EditorGUIUtility.TrTextContent("EmissionMap", "EmissionMap");
            
            public static readonly GUIContent ScrollText = EditorGUIUtility.TrTextContent("UV Scroll Settings", "");
            public static readonly GUIContent IsScrollText = EditorGUIUtility.TrTextContent("Is Scroll", "");
            public static readonly GUIContent SpeedText = EditorGUIUtility.TrTextContent("Scroll Speed", "");
        }

        #endregion

        #region Material Properties
        private MaterialProperty Cutoff { get; set; }
        private MaterialProperty CullMode { get; set; }
        private MaterialProperty BaseColor { get; set; }
        private MaterialProperty BaseMap { get; set; }
        private MaterialProperty BumpMap { get; set; }
        private MaterialProperty EmissionColor { get; set; }
        private MaterialProperty EmissionMask { get; set; }
        private MaterialProperty IsScroll { get; set; }
        private MaterialProperty SpeedX { get; set; }
        private MaterialProperty SpeedY { get; set; }
        private MaterialProperty Surface { get; set; }
        private MaterialProperty SrcBlend { get; set; }
        private MaterialProperty DstBlend { get; set; }
        private MaterialProperty AlphaToMask { get; set; }
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
            new MaterialHeaderScopeList(uint.MaxValue & ~(uint)DevelopmentDetailGUI.Expandable.Advanced);

        #endregion

        private bool _defaultInspector = false;
        private bool _firstTimeApply = true;
        private MaterialEditor _materialEditor;
        private readonly float _defaultFieldWidth = EditorGUIUtility.fieldWidth;
        private readonly float _defaultLabelWidth = EditorGUIUtility.labelWidth;

        private static bool exitStatic = false;
        
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material);
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
            
            bool preValue = exitStatic;
            // Staticに設定したGameObjectがあるかチェック
            exitStatic = CheckStaticExist();
            // GameObjectのStaticに設定がOff->Onになった場合
            if (exitStatic && preValue != exitStatic)
                ValidateScrollSettings(material);

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
            Cutoff = FindProperty(DevelopmentDetailProperty.Cutoff, props);
            CullMode = FindProperty(DevelopmentDetailProperty.CullMode, props);
            BaseColor = FindProperty(DevelopmentDetailProperty.BaseColor, props);
            BaseMap = FindProperty(DevelopmentDetailProperty.BaseMap, props);
            BumpMap = FindProperty(DevelopmentDetailProperty.BumpMap, props);
            EmissionColor = FindProperty(DevelopmentDetailProperty.EmissionColor, props);
            EmissionMask = FindProperty(DevelopmentDetailProperty.EmissionMask, props);
            IsScroll = FindProperty(DevelopmentDetailProperty.IsScroll, props);
            SpeedX = FindProperty(DevelopmentDetailProperty.SpeedX, props);
            SpeedY = FindProperty(DevelopmentDetailProperty.SpeedY, props);
            Surface = FindProperty(DevelopmentDetailProperty.Surface, props);
            SrcBlend = FindProperty(DevelopmentDetailProperty.SrcBlend, props);
            DstBlend = FindProperty(DevelopmentDetailProperty.DstBlend, props);
            AlphaToMask = FindProperty(DevelopmentDetailProperty.AlphaToMask, props);
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
            
            // Surface TypeのDrop-down list
            SurfaceType surfaceType = (SurfaceType)_materialEditor.PopupShaderProperty(Surface, Styles.SurfaceTypeText, Styles.surfaceTypeNames);
            // Surface TypeがCutoffの場合はプロパティを追加表示
            if (surfaceType == SurfaceType.Cutoff)
                _materialEditor.ShaderProperty(Cutoff, Styles.CutoffText);
            
            // Cull Mode
            if (material.HasProperty(DevelopmentDetailProperty.CullMode))
                _materialEditor.ShaderProperty(CullMode, Styles.CullModeText);

            ResetGUIWidths();
        }

        private void DrawMainSettings(Material material)
        {
            GUILayout.Label("Texture", EditorStyles.boldLabel);
            
            // Base
            if (material.HasProperty(DevelopmentDetailProperty.BaseMap) && material.HasProperty(DevelopmentDetailProperty.BaseColor))
                _materialEditor.TexturePropertySingleLine(Styles.BaseColorText, BaseMap, BaseColor);
            // Bump
            if (material.HasProperty(DevelopmentDetailProperty.BumpMap))
                _materialEditor.TexturePropertySingleLine(Styles.BumpMapText, BumpMap);
            // Emission
            var emissive = _materialEditor.EmissionEnabledProperty();
            using (new EditorGUI.DisabledScope(!emissive))
            {
                if ((EmissionMask == null) || (EmissionColor == null))
                    return;
                using (new EditorGUI.IndentLevelScope(2))
                {
                    _materialEditor.TexturePropertyWithHDRColor(Styles.EmissionMap, EmissionMask, EmissionColor, true);
                }
                _materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, emissive);
            }
            // Tilling and Offset
            EditorGUI.indentLevel += 1;
            if (material.HasProperty(DevelopmentDetailProperty.BaseMap))
                _materialEditor.TextureScaleOffsetProperty(BaseMap);
            EditorGUI.indentLevel -= 1;
            
            // 区分線
            CoreEditorUtils.DrawSplitter();
            
            // UV Scroll
            if (material.HasProperty(DevelopmentDetailProperty.IsScroll))
            {
                // Label
                GUILayout.Label(Styles.ScrollText, EditorStyles.boldLabel);
                
                // Is Scrollの表示（UV ScrollのToggle）
                bool isScroll = (IsScroll.floatValue != 0.0f);
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = IsScroll.hasMixedValue;
                isScroll = EditorGUI.Toggle(EditorGUILayout.GetControlRect(), Styles.IsScrollText, isScroll);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    IsScroll.floatValue = isScroll ? 1.0f : 0.0f;
                    // Staticとの妥当性チェック
                    ValidateScrollSettings(material);
                }
                // UV Scrollのスピードのプロパティ
                using (new EditorGUI.DisabledScope(!isScroll))
                {
                    float width = EditorGUIUtility.labelWidth;
                    float height = EditorGUIUtility.singleLineHeight;
                    Rect position = EditorGUILayout.GetControlRect(true);
                    Rect labelPosition = new Rect(position.x, position.y, width, height);
                    
                    MaterialEditor.BeginProperty(position, SpeedX);
                    EditorGUI.BeginChangeCheck();
                    // Label
                    EditorGUI.PrefixLabel(labelPosition, Styles.SpeedText);
                    // Property
                    Rect propPosition = new Rect(position.x + width, position.y, position.width - width, height);
                    var value = EditorGUI.Vector2Field(propPosition, GUIContent.none, new Vector2(SpeedX.floatValue, SpeedY.floatValue));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SpeedX.floatValue = value.x;
                        SpeedY.floatValue = value.y;
                    }
                    MaterialEditor.EndProperty();
                }
            }
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

        private bool CheckStaticExist()
        {
            // Staticに設定したGameObjectがあるかチェックする
            bool exitStatic = false;
            foreach (var go in Selection.gameObjects)
            {
                if (go.isStatic)
                {
                    exitStatic = go.isStatic;
                    break;
                }
            }
            return exitStatic;
        }
        
        private void ValidateScrollSettings(Material material)
        {
            // Staticに設定したGameObjectに「Is Scroll」を設定しようとした場合以外はスキップ
            if (!(exitStatic && material.GetFloat(DevelopmentDetailProperty.IsScroll) != 0f))
                return;
            
            // 警告を出す
            if (EditorUtility.DisplayDialog(
                    "警告",
                    "UV Scrollの動きはLight Mapに反映されません。" +
                    "\nStatic設定やUV Scrollの設定にご注意ください。",
                    "OK"))
            {
                // 処理なし
            }
        }

        private void SetMaterialKeywords(Material material)
        {
            // Surface Type
            SurfaceType surfaceType = (SurfaceType)material.GetFloat(DevelopmentDetailProperty.Surface);
            switch (surfaceType)
            {
                case SurfaceType.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetFloat(DevelopmentDetailProperty.SrcBlend, (float)BlendMode.One);
                    material.SetFloat(DevelopmentDetailProperty.DstBlend, (float)BlendMode.Zero);
                    material.SetFloat(DevelopmentDetailProperty.AlphaToMask, 0.0f);
                    CoreUtils.SetKeyword(material, "_ALPHATEST_ON", false);
                    material.renderQueue = (int)RenderQueue.Geometry; 
                    break;
                case SurfaceType.Cutoff:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetFloat(DevelopmentDetailProperty.SrcBlend, (float)BlendMode.One);
                    material.SetFloat(DevelopmentDetailProperty.DstBlend, (float)BlendMode.Zero);
                    material.SetFloat(DevelopmentDetailProperty.AlphaToMask, 1.0f);
                    CoreUtils.SetKeyword(material, "_ALPHATEST_ON", true);
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    break;
            }
            
            // Normal Map
            CoreUtils.SetKeyword(material, "_NORMALMAP_ON", material.GetTexture(DevelopmentDetailProperty.BumpMap));
            
            // Emission
            if (material.HasProperty(DevelopmentDetailProperty.EmissionColor))
                MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            CoreUtils.SetKeyword(material, "_EMISSION_ON", shouldEmissionBeEnabled);
        }
        #endregion
    }
}