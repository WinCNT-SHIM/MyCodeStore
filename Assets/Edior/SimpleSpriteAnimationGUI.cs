using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomShaderGUI
{
    public class SimpleSpriteAnimationGUI : ShaderGUI
    {
        #region Material Styles
        private static class Styles
        {
            public static readonly string[] SurfaceTypeNames = { "Opaque", "Cutout", "Fake Transparent" };
            public static readonly string[] BlendingModeNames = Enum.GetNames(typeof(BlendingMode));
            
            // Categories
            public static readonly GUIContent ShaderSettingsText = EditorGUIUtility.TrTextContent("Shader Settings", "");
            public static readonly GUIContent SpriteSettingsText = EditorGUIUtility.TrTextContent("Sprite Settings", "");
            public static readonly GUIContent AnimationSettingsText = EditorGUIUtility.TrTextContent("Animation Settings", "");
            public static readonly GUIContent AdvancedText = EditorGUIUtility.TrTextContent("Advanced Settings", "");

            // Properties
            public static readonly GUIContent SurfaceTypeText = EditorGUIUtility.TrTextContent("Surface Type", "Opaque / Cutoff / Transparent");
            public static readonly GUIContent CutoutText = EditorGUIUtility.TrTextContent("Cutout", "");
            public static readonly GUIContent BlendingModeText = EditorGUIUtility.TrTextContent("Blending Mode", "");
            public static readonly GUIContent CullModeText = EditorGUIUtility.TrTextContent("Cull Mode", "Culling Mode");
            public static readonly GUIContent PaddingText = EditorGUIUtility.TrTextContent("Use Edge Padding", "チェックすると、出力するスプライトの枠１ピクセルを切り捨てます。");
            public static readonly GUIContent BaseMapText = EditorGUIUtility.TrTextContent("Sprite Sheet", "Sprite Sheet");
            public static readonly GUIContent BaseColorText = EditorGUIUtility.TrTextContent("Tint Color", "Tint Color");
            public static readonly GUIContent ColumnText = EditorGUIUtility.TrTextContent("Col", "Column");
            public static readonly GUIContent RowText = EditorGUIUtility.TrTextContent("Row", "");
            public static readonly GUIContent MaxFrameCountText = EditorGUIUtility.TrTextContent("Max Animation Frame", "Sprite Sheetの最大フレーム数を設定してください。");
            public static readonly GUIContent AutoLoopText = EditorGUIUtility.TrTextContent("Auto Loop", "");
            public static readonly GUIContent AnimationSpeedText = EditorGUIUtility.TrTextContent("Animation Speed", "");
            public static readonly GUIContent AnimationIndexText = EditorGUIUtility.TrTextContent("Animation Index Control", "再生したいスプライトアニメーションのフレームを設定できます。\n値が設定されると、Auto Loopのチェックが外されます。");
            public static readonly GUIContent QueueSliderText = EditorGUIUtility.TrTextContent("Sorting Priority", "マテリアルの描画順を調整します。値の小さいマテリアルから描画されます。");
            public static readonly GUIContent ZWriteText = EditorGUIUtility.TrTextContent("Z-Write", "描画時にオブジェクトが深度バッファを更新するかどうかを設定します。");
        }
        #endregion
        
        #region Material Properties
        MaterialProperty Cutoff { get; set; }
        MaterialProperty CullMode { get; set; }
        MaterialProperty BaseMap { get; set; }
        MaterialProperty BaseColor { get; set; }
        MaterialProperty Padding { get; set; }
        MaterialProperty Column { get; set; }
        MaterialProperty Row { get; set; }
        MaterialProperty MaxFrameCount { get; set; }
        MaterialProperty AutoLoop { get; set; }
        MaterialProperty AnimationSpeed { get; set; }
        MaterialProperty AnimationIndex { get; set; }
        MaterialProperty Surface { get; set; }
        MaterialProperty Blend { get; set; }
        MaterialProperty SrcBlend { get; set; }
        MaterialProperty DstBlend { get; set; }
        MaterialProperty ZWrite { get; set; }
        MaterialProperty AlphaToMask { get; set; }
        MaterialProperty QueueOffset { get; set; }
        #endregion
        
        #region Header Scope Properties
        [Flags]
        private enum Expandable
        {
            ShaderSettings = 1 << 0,
            SpriteSettings = 1 << 1,
            AnimationSettings = 1 << 2,
            Advanced = 1 << 3,
        }

        private enum SurfaceType
        {
            Opaque,
            Cutout,
            FakeTransparent
        }
        
        private enum BlendingMode
        {
            Alpha,
            Premultiply,
            Additive,
            Multiply
        }
        
        private uint _materialFilter => uint.MaxValue;

        private readonly MaterialHeaderScopeList _materialScopeList =
            new MaterialHeaderScopeList(uint.MaxValue & ~(uint)SimpleSpriteAnimationGUI.Expandable.Advanced);
        #endregion
        
        // 変数
        private MaterialEditor _materialEditor;
        private bool _defaultInspector = false;
        private bool _firstTimeApply = true;
        private readonly float _defaultFieldWidth = EditorGUIUtility.fieldWidth;
        private readonly float _defaultLabelWidth = EditorGUIUtility.labelWidth;
        private const int QueueOffsetRange = 50;
        
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
                _materialScopeList.DrawHeaders(materialEditor, material);
            }
        }
        
        void FindProperties(MaterialProperty[] props)
        {
            Cutoff = FindProperty(SimpleSpriteAnimationProperty.Cutoff, props);
            CullMode = FindProperty(SimpleSpriteAnimationProperty.CullMode, props);
            BaseMap = FindProperty(SimpleSpriteAnimationProperty.BaseMap, props);
            BaseColor = FindProperty(SimpleSpriteAnimationProperty.BaseColor, props);
            Padding = FindProperty(SimpleSpriteAnimationProperty.Padding, props);
            Column = FindProperty(SimpleSpriteAnimationProperty.Column, props);
            Row = FindProperty(SimpleSpriteAnimationProperty.Row, props);
            MaxFrameCount = FindProperty(SimpleSpriteAnimationProperty.MaxFrameCount, props);
            AutoLoop = FindProperty(SimpleSpriteAnimationProperty.AutoLoop, props);
            AnimationSpeed = FindProperty(SimpleSpriteAnimationProperty.AnimationSpeed, props);
            AnimationIndex = FindProperty(SimpleSpriteAnimationProperty.AnimationIndex, props);
            Surface = FindProperty(SimpleSpriteAnimationProperty.Surface, props);
            Blend = FindProperty(SimpleSpriteAnimationProperty.Blend, props);
            SrcBlend = FindProperty(SimpleSpriteAnimationProperty.SrcBlend, props);
            DstBlend = FindProperty(SimpleSpriteAnimationProperty.DstBlend, props);
            ZWrite = FindProperty(SimpleSpriteAnimationProperty.ZWrite, props);
            AlphaToMask = FindProperty(SimpleSpriteAnimationProperty.AlphaToMask, props);
            QueueOffset = FindProperty(SimpleSpriteAnimationProperty.QueueOffset, props);
        }
        
        private void RegisterHeader(Material material, MaterialEditor materialEditor)
        {
            var filter = (Expandable)_materialFilter;
            
            if (filter.HasFlag(Expandable.ShaderSettings))
                _materialScopeList.RegisterHeaderScope(Styles.ShaderSettingsText, (uint)Expandable.ShaderSettings, DrawShaderSettings);

            if (filter.HasFlag(Expandable.SpriteSettings))
                _materialScopeList.RegisterHeaderScope(Styles.SpriteSettingsText, (uint)Expandable.SpriteSettings, DrawSpriteSettings);
            
            if (filter.HasFlag(Expandable.AnimationSettings))
                _materialScopeList.RegisterHeaderScope(Styles.AnimationSettingsText, (uint)Expandable.SpriteSettings, DrawAnimationSettings);
            
            if (filter.HasFlag(Expandable.Advanced))
                _materialScopeList.RegisterHeaderScope(Styles.AdvancedText, (uint)Expandable.Advanced, DrawAdvanced);
        }
        
        #region Draw Category
        private void DrawShaderSettings(Material material)
        {
            EditorGUIUtility.labelWidth = 0f;
            
            // Surface TypeのDrop-down list
            SurfaceType surfaceType = (SurfaceType)_materialEditor.PopupShaderProperty(Surface, Styles.SurfaceTypeText, Styles.SurfaceTypeNames);
            if (surfaceType == SurfaceType.FakeTransparent)
                _materialEditor.PopupShaderProperty(Blend, Styles.BlendingModeText, Styles.BlendingModeNames);
            // Surface TypeがCutout、またはTransparentの場合はCutoffプロパティを表示
            if (surfaceType == SurfaceType.Cutout || surfaceType == SurfaceType.FakeTransparent)
                _materialEditor.ShaderProperty(Cutoff, Styles.CutoutText);

            // Cull Mode
            if (material.HasProperty(DevelopmentDetailProperty.CullMode))
                _materialEditor.ShaderProperty(CullMode, Styles.CullModeText);

            ResetGUIWidths();
        }
        private void DrawSpriteSettings(Material material)
        {
            _materialEditor.SetDefaultGUIWidths();
            
            // Padding
            if (Padding != null)
            {
                var usePadding = Padding.floatValue != 0.0f;
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = Padding.hasMixedValue;
                usePadding = EditorGUI.Toggle(EditorGUILayout.GetControlRect(), Styles.PaddingText, usePadding);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                    Padding.floatValue = usePadding ? 1.0f : 0.0f;
            }
            
            // Sprite SheetとTint Color
            if (BaseMap != null)
                _materialEditor.ShaderProperty(BaseMap, Styles.BaseMapText);
            if (BaseColor != null) 
                _materialEditor.ShaderProperty(BaseColor, Styles.BaseColorText);
            
            // RowとColumn
            if (Row != null && Column != null)
            {
                GUILayout.Label("Column and Row", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                _materialEditor.IntShaderProperty(Column, Styles.ColumnText);
                if (Column.floatValue < 1) Column.floatValue = 1;
                _materialEditor.IntShaderProperty(Row, Styles.RowText);
                if (Row.floatValue < 1) Row.floatValue = 1;
                
                EditorGUI.indentLevel--;
            }

            // Animation Max Frame
            if (MaxFrameCount != null)
            {
                _materialEditor.IntShaderProperty(MaxFrameCount, Styles.MaxFrameCountText);
                if (MaxFrameCount.floatValue < 1) MaxFrameCount.floatValue = 1;
            }
        }
        private void DrawAnimationSettings(Material material)
        {
            _materialEditor.SetDefaultGUIWidths();
            
            // Auto Loop
            var isAutoLoop = AutoLoop.floatValue != 0.0f;
            if (AutoLoop != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = Padding.hasMixedValue;
                isAutoLoop = EditorGUI.Toggle(EditorGUILayout.GetControlRect(), Styles.AutoLoopText, isAutoLoop);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                    AutoLoop.floatValue = isAutoLoop ? 1.0f : 0.0f;
            }
            
            // Animation Speed
            using (new EditorGUI.DisabledScope(!isAutoLoop))
            {
                if (AnimationSpeed != null)
                    _materialEditor.ShaderProperty(AnimationSpeed, Styles.AnimationSpeedText);
            }
            
            // Animation Index Control
            using (new EditorGUI.DisabledScope(isAutoLoop))
            {
                if (AnimationIndex != null)
                {
                    // Auto Loopにチェックすると、Animation Indexを0に初期化する
                    if (isAutoLoop)
                        AnimationIndex.floatValue = 0.0f;
                    
                    if (AnimationIndex.floatValue < 0) AnimationIndex.floatValue = 0;
                    _materialEditor.IntShaderProperty(AnimationIndex, Styles.AnimationIndexText);
                }
            }
        }
        
        private void DrawAdvanced(Material material)
        {
            EditorGUIUtility.labelWidth = 0f;
            var surfaceType = (SurfaceType)material.GetFloat(SimpleSpriteAnimationProperty.Surface);
            if (QueueOffset != null)
            {;
                var maxVal = QueueOffsetRange;
                if (surfaceType == SurfaceType.FakeTransparent)
                {
                    if (QueueOffset.floatValue > 0) QueueOffset.floatValue = 0f; 
                    maxVal = 0;
                }
                _materialEditor.IntSliderShaderProperty(QueueOffset, -QueueOffsetRange, maxVal, Styles.QueueSliderText);
            }
            
            ResetGUIWidths();
            // ZWrite
            if (ZWrite != null)
            {
                using (new EditorGUI.DisabledScope(surfaceType != SurfaceType.FakeTransparent))
                    _materialEditor.ShaderProperty(ZWrite, Styles.ZWriteText);
            }
        }
        #endregion

        #region Private Methods
        private void ResetGUIWidths()
        {
            EditorGUIUtility.fieldWidth = _defaultFieldWidth;
            EditorGUIUtility.labelWidth = _defaultLabelWidth;
        }
        
        private void SetMaterialKeywords(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            
            bool alphaClip = false;
            if (material.HasProperty(SimpleSpriteAnimationProperty.Cutoff))
                alphaClip = material.GetFloat(SimpleSpriteAnimationProperty.Cutoff) >= 0.5;
            
            // クリア処理 
            int renderQueue = material.shader.renderQueue;
            material.SetOverrideTag("RenderType", "");
            if (SrcBlend != null) SrcBlend.floatValue = (float)BlendMode.One;
            if (DstBlend != null) DstBlend.floatValue = (float)BlendMode.Zero;
            if (AlphaToMask != null) AlphaToMask.floatValue = 0.0f;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");

            if (material.HasProperty(SimpleSpriteAnimationProperty.Surface))
            {
                SurfaceType surfaceType = (SurfaceType)material.GetFloat(SimpleSpriteAnimationProperty.Surface);

                if (surfaceType == SurfaceType.Opaque)
                {
                    material.SetOverrideTag("RenderType", "");
                    if (SrcBlend != null) SrcBlend.floatValue = (float)BlendMode.One;
                    if (DstBlend != null) DstBlend.floatValue = (float)BlendMode.Zero;
                    if (ZWrite != null) ZWrite.floatValue = 1.0f;
                    if (AlphaToMask != null) AlphaToMask.floatValue = 0.0f;
                    material.renderQueue = (int)RenderQueue.Geometry;
                }
                else if (surfaceType == SurfaceType.Cutout)
                {
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    if (SrcBlend != null) SrcBlend.floatValue = (float)BlendMode.One;
                    if (DstBlend != null) DstBlend.floatValue = (float)BlendMode.Zero;
                    if (ZWrite != null) ZWrite.floatValue = 1.0f;
                    if (AlphaToMask != null) AlphaToMask.floatValue = 1.0f;
                    CoreUtils.SetKeyword(material, "_ALPHATEST_ON", alphaClip);
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                }
                else
                {
                    // Blending Modeの設定
                    BlendingMode blendMode = (BlendingMode)material.GetFloat(SimpleSpriteAnimationProperty.Blend);

                    var srcBlend = UnityEngine.Rendering.BlendMode.One;
                    var dstBlend = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                    
                    // Blending Modeの設定設定に合わせてSrcAlphaとDstBlendを設定
                    switch (blendMode)
                    {
                        case BlendingMode.Alpha:
                            srcBlend = UnityEngine.Rendering.BlendMode.SrcAlpha;
                            dstBlend = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                            break;
                        case BlendingMode.Premultiply:
                            srcBlend = UnityEngine.Rendering.BlendMode.One;
                            dstBlend = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                            break;
                        case BlendingMode.Additive:
                            srcBlend = UnityEngine.Rendering.BlendMode.One;
                            dstBlend = UnityEngine.Rendering.BlendMode.One;
                            break;
                        case BlendingMode.Multiply:
                            srcBlend = UnityEngine.Rendering.BlendMode.DstColor;
                            dstBlend = UnityEngine.Rendering.BlendMode.Zero;
                            break;
                    }
                    
                    material.SetOverrideTag("RenderType", "Transparent");
                    if (SrcBlend != null) SrcBlend.floatValue = (float)srcBlend;
                    if (DstBlend != null) DstBlend.floatValue = (float)dstBlend;
                    if (AlphaToMask != null) AlphaToMask.floatValue = 1.0f;
                    CoreUtils.SetKeyword(material, "_SURFACE_TYPE_TRANSPARENT", true);
                    CoreUtils.SetKeyword(material, "_ALPHATEST_ON", alphaClip);
                    // Geometry, AlphaTest < GeometryLast < Transparent
                    material.renderQueue = (int)RenderQueue.GeometryLast;
                }
                if (QueueOffset != null)
                    material.renderQueue += (int)QueueOffset.floatValue;
            }
        }
        #endregion
    }
}