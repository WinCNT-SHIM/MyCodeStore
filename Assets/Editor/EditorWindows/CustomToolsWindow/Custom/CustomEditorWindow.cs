using System;
using UnityEditor;

namespace Editor.EditorWindows.CustomToolsWindow.Custom
{
    [System.Serializable]
    public class CustomEditorWindow : EditorWindow
    {
        protected SerializedObject So = null;
        
        [NonSerialized] private bool _isInitialized;
        private CustomPropertyAttributeDrawer _customPropertyAttributeDrawer = new CustomPropertyAttributeDrawer();

        protected virtual void OnGUI()
        {
            this.DrawEditors();
        }

        protected virtual void Initialize()
        {
            if (So == null)
                So = new SerializedObject(this);
            _customPropertyAttributeDrawer.Initialize(So);
        }

        protected virtual void DrawEditors()
        {
            _customPropertyAttributeDrawer.Draw();
        }

        private void InitializeIfNeeded()
        {
            if (this._isInitialized)
                return;
            this._isInitialized = true;
            if (this.titleContent != null && this.titleContent.text == this.GetType().FullName)
                // this.titleContent.text = TypeExtensions.GetNiceName(this.GetType()).SplitPascalCase();
                this.titleContent.text = this.GetType().ToString();
            this.wantsMouseMove = true;
            Selection.selectionChanged -= new Action(this.SelectionChanged);
            Selection.selectionChanged += new Action(this.SelectionChanged);
            this.Initialize();
        }
        private void SelectionChanged() => this.Repaint();
        protected virtual void OnEnable() => this.InitializeIfNeeded();
    }
}
