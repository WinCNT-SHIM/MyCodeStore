using System.Collections.Generic;
using System.Linq;
using Editor.EditorWindows.CustomToolsWindow.Custom;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomToolsWindow : CustomEditorWindow
{
    // Common
    private readonly int LIST_HEADER_HEIGHT = 48;
    private readonly int LIST_LINE_HEIGHT = 20;
    private readonly int LIST_MAX_DISPLAY_LINE = 10;
    
    // Count Selected Object Vertices
    [SerializeField] private List<GameObject> selectedGameObjects = new List<GameObject>();
    
    // Count Scene Vertices
    private Vector2 _scrollPositionCurrentSkins = Vector2.zero;
    private Vector2 _scrollPositioncurrentMeshes = Vector2.zero;
    private readonly List<GameObject> _rootGameObjects = new List<GameObject>();
    private const int MaxMeshVertexCount = 100000;
    private const int MaxSkinMeshVertexCount = 20000;
    private int _meshCount = 0;
    private int _skinMeshCount = 0;
    private int _sceneMeshCount = 0;
    private int _sceneSkinMeshCount = 0;
    private int _reductionMeshCount = 0;
    private int _reductionSkinMeshCount = 0;
    private bool _countActiveSceneOnly = true;
    [HideInInspector] [SerializeField] private List<SkinnedMeshRenderer> currentSkins = new List<SkinnedMeshRenderer>();
    [HideInInspector] [SerializeField] private List<MeshFilter> currentMeshes = new List<MeshFilter>();
    private List<SkinnedMeshRenderer> _reductionSkins = new List<SkinnedMeshRenderer>();
    private List<MeshFilter> _reductionMeshes = new List<MeshFilter>();
    
    // Select Collider
    private Vector2 _scrollPositionFindCollider = Vector2.zero;
    [SerializeField] private List<GameObject> colliderGameObjectList = new List<GameObject>();
    private bool _findCollidersInActiveSceneOnly = true;
    private bool _findOnlyActiveColliders = true;

    [MenuItem("Custom Tools/My Custom Tools")]
    private static void OpenWindow()
    {
        GetWindow<CustomToolsWindow>("My Custom Tools").Close();
        GetWindow<CustomToolsWindow>("My Custom Tools").Show();
    }

    #region Count Selected Object Vertices
    [FoldoutGroup("Count Selected Object Vertices")]
    [OnInspectorGUI]
    private void DrawSelectionVertexCounter()
    {
        selectedGameObjects = Selection.gameObjects.ToList();
        
        EditorGUILayout.IntField("Skin", CountMeshVertex(GetAllSkinnedMeshRenderers(selectedGameObjects)));
        EditorGUILayout.IntField("mesh", CountMeshVertex(GetAllMeshFilters(selectedGameObjects)));
        
        // LOD
        var rootSelectedGameObjects = selectedGameObjects.Select(go => go.transform.root.gameObject).Distinct().ToList();
        var lodObjects = GetLod1OverGameObjects(rootSelectedGameObjects);
        
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Vertices of LOD1 above", style: "BoldLabel");
        EditorGUILayout.IntField("Skin", CountMeshVertex(GetAllSkinnedMeshRenderers(lodObjects)));
        EditorGUILayout.IntField("mesh", CountMeshVertex(GetAllMeshFilters(lodObjects)));
        EditorGUI.indentLevel--;
        
        Repaint();
    }
    #endregion

    #region Count Scene Vertices
    [FoldoutGroup("Count Scene Vertices")]
    [OnInspectorGUI]
    private void DrawSceneVertexCounter()
    {
        if (_meshCount > MaxMeshVertexCount)
        {
            EditorGUILayout.HelpBox(
                "Vertices Over(Exclude LOD1 above) : MeshRenderer " + (_meshCount - MaxMeshVertexCount) + "over(Max : " + MaxMeshVertexCount + ")",
                MessageType.Error);
        }
    
        if (_skinMeshCount > MaxSkinMeshVertexCount)
        {
            EditorGUILayout.HelpBox(
                "Vertices Over(Exclude LOD1 above) : SkinMeshRenderer " + (_skinMeshCount - MaxSkinMeshVertexCount) + "over(Max : " + MaxSkinMeshVertexCount + ")",
                MessageType.Error);
        }
        
        _countActiveSceneOnly = GUILayout.Toggle(_countActiveSceneOnly, "Active Scene Only");
        EditorGUILayout.Space();
    
        if (GUILayout.Button("Count Scene Vertices"))
        {
            UpdateSceneVertexList(_countActiveSceneOnly);
            currentMeshes.Where(m => m.sharedMesh !=null).ToList().Sort((a, b) => b.sharedMesh.vertexCount - a.sharedMesh.vertexCount);
            currentSkins.Where(m => m.sharedMesh !=null).ToList().Sort((a, b) => b.sharedMesh.vertexCount - a.sharedMesh.vertexCount);
            Repaint();
        }
        EditorGUILayout.Space();
        CoreEditorUtils.DrawSplitter(true);

        EditorGUILayout.LabelField("Count of Rendering Target(Include Non-Active)", style: "BoldLabel");
        EditorGUILayout.IntField("SkinnedMesh", currentSkins.Count);
        EditorGUILayout.IntField("Mesh", currentMeshes.Count);
        
        EditorGUILayout.Space();
        CoreEditorUtils.DrawSplitter(true);
        
        // Icon
        var skinLabel = new GUIContent("Skin", EditorGUIUtility.IconContent("d_SkinnedMeshRenderer Icon").image);
        var meshLabel = new GUIContent("Mesh", EditorGUIUtility.IconContent("d_Mesh Icon").image);
        
        EditorGUILayout.LabelField("Vertices in Scenes(Include Non-Active)", style: "BoldLabel");
        _sceneSkinMeshCount = EditorGUILayout.IntField(skinLabel, CountMeshVertex(currentSkins));
        _sceneMeshCount = EditorGUILayout.IntField(meshLabel, CountMeshVertex(currentMeshes));
        
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Active Only", style: "BoldLabel");
        EditorGUILayout.IntField(skinLabel, CountMeshVertex(currentSkins, true));
        EditorGUILayout.IntField(meshLabel, CountMeshVertex(currentMeshes, true));
        EditorGUI.indentLevel--;

        // LOD
        EditorGUILayout.LabelField("Vertices of LOD1 above", style: "BoldLabel");
        _reductionSkinMeshCount = EditorGUILayout.IntField("Skin", CountMeshVertex(_reductionSkins));
        _reductionMeshCount = EditorGUILayout.IntField("mesh", CountMeshVertex(_reductionMeshes));
        
        _skinMeshCount = _sceneSkinMeshCount - _reductionSkinMeshCount;
        _meshCount = _sceneMeshCount - _reductionMeshCount;

        EditorGUILayout.Space();
        CoreEditorUtils.DrawSplitter(true);
        
        EditorGUILayout.LabelField("Select Renderer's GameObject(Include Non-Active)", style: "BoldLabel");
        if (base.So != null)
        {
            bool isScroll1 = currentSkins.Count > 0 && base.So.FindProperty("currentSkins").isExpanded;
            if (isScroll1)
            {
                int maxDisplayLine = currentSkins.Count < LIST_MAX_DISPLAY_LINE ? currentSkins.Count : LIST_MAX_DISPLAY_LINE;
                _scrollPositionCurrentSkins = EditorGUILayout.BeginScrollView(_scrollPositionCurrentSkins, GUILayout.Height(LIST_HEADER_HEIGHT + LIST_LINE_HEIGHT * maxDisplayLine));
            }
            
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(base.So.FindProperty("currentSkins"), new GUIContent("All Skinned Mesh(Decs Vertices)"), true);
            
            if (isScroll1)
                EditorGUILayout.EndScrollView();
            
            
            bool isScroll2 = currentMeshes.Count > 0 && base.So.FindProperty("currentMeshes").isExpanded;
            if (isScroll2)
            {
                int maxDisplayLine = currentMeshes.Count < LIST_MAX_DISPLAY_LINE ? currentMeshes.Count : LIST_MAX_DISPLAY_LINE;
                _scrollPositioncurrentMeshes = EditorGUILayout.BeginScrollView(_scrollPositioncurrentMeshes, GUILayout.Height(LIST_HEADER_HEIGHT + LIST_LINE_HEIGHT * maxDisplayLine));
            }
            
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(base.So.FindProperty("currentMeshes"), new GUIContent("All Mesh(Decs Vertices)"), true);
            
            if (isScroll2)
                EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("SerializedObject is Null!", MessageType.Error);
        }
    }
    #endregion

    #region Select Collider
    [FoldoutGroup("Select Collider")]
    [OnInspectorGUI]
    private void DrawSelectColliderMenu()
    {
        Rect rect = EditorGUILayout.BeginHorizontal();
        _findCollidersInActiveSceneOnly = GUILayout.Toggle(_findCollidersInActiveSceneOnly, "Active Scene Only");
        _findOnlyActiveColliders = GUILayout.Toggle(_findOnlyActiveColliders, "Active Collider Only");
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Find"))
        {
            colliderGameObjectList.Clear();

            foreach (var gameObject in GetAllGameObjects(_findCollidersInActiveSceneOnly))
            {
                var target = gameObject.GetComponent<Collider>();
                if (target == null || (!target.enabled && _findOnlyActiveColliders))
                    continue;
                colliderGameObjectList.Add(gameObject);
            }
        }
        colliderGameObjectList = colliderGameObjectList.Where(o => o != null).ToList();

        if (GUILayout.Button("Select All"))
        {
            Selection.instanceIDs = colliderGameObjectList.Select(target => target.GetInstanceID()).ToArray();
        }

        if (base.So != null)
        {
            bool isScroll = colliderGameObjectList.Count > 0 && base.So.FindProperty("colliderGameObjectList").isExpanded;
            if (isScroll)
            {
                int maxDisplayLine = colliderGameObjectList.Count < LIST_MAX_DISPLAY_LINE ? colliderGameObjectList.Count : LIST_MAX_DISPLAY_LINE;
                _scrollPositionFindCollider = EditorGUILayout.BeginScrollView(_scrollPositionFindCollider, GUILayout.Height(LIST_HEADER_HEIGHT + LIST_LINE_HEIGHT * maxDisplayLine));
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(base.So.FindProperty("colliderGameObjectList"), new GUIContent("Colliders Found"), true);
            
            if (isScroll)
                EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("SerializedObject is Null!", MessageType.Error);
        }
    }
    #endregion
    
    #region Private Methods
    private List<SkinnedMeshRenderer> GetAllSkinnedMeshRenderers(List<GameObject> rootGameObject)
    {
        var meshes = new List<SkinnedMeshRenderer>();
        foreach (var gameObject in rootGameObject)
            meshes.AddRange(gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true));
        meshes = meshes.Distinct().ToList();
        return meshes.OrderByDescending(x => x.sharedMesh != null ? x.sharedMesh.vertexCount : -1).ToList();
    }
    
    private List<MeshFilter> GetAllMeshFilters(List<GameObject> rootGameObject)
    {
        var meshes = new List<MeshFilter>();
        foreach (var gameObject in rootGameObject)
            meshes.AddRange(gameObject.GetComponentsInChildren<MeshFilter>(true));
        meshes = meshes.Distinct().ToList();
        return meshes.OrderByDescending(x => x.sharedMesh != null ? x.sharedMesh.vertexCount : -1).ToList();
    }

    private static int CountMeshVertex(List<SkinnedMeshRenderer> list, bool activeOnly = false)
    {
        if (activeOnly)
        {
            return list
                .Where(i => i != null && i.sharedMesh != null)
                .Where(i => i.gameObject.activeInHierarchy)
                .Sum(i => i.sharedMesh.vertices.Length);
        }
        else
        {
            return list
                .Where(i => i != null && i.sharedMesh != null)
                .Sum(i => i.sharedMesh.vertices.Length);
        }
    }
    
    private static int CountMeshVertex(List<MeshFilter> list, bool  activeOnly = false)
    {
        if (activeOnly)
        {
            return list
                .Where(i => i != null && i.sharedMesh != null)
                .Where(i => i.gameObject.activeInHierarchy)
                .Sum(i => i.sharedMesh.vertices.Length);
        }
        else
        {
            return list
                .Where(i => i != null && i.sharedMesh != null)
                .Sum(i => i.sharedMesh.vertices.Length);
        }
    }
    
    private void UpdateSceneVertexList(bool countActiveSceneOnly = false)
    {
        GetAllGameObjects(countActiveSceneOnly);
        currentSkins = GetAllSkinnedMeshRenderers(_rootGameObjects);
        currentMeshes = GetAllMeshFilters(_rootGameObjects);
        
        var lodObjects = GetLod1OverGameObjects(_rootGameObjects);
        _reductionSkins = GetAllSkinnedMeshRenderers(lodObjects);
        _reductionMeshes = GetAllMeshFilters(lodObjects);
    }

    private List<GameObject> GetAllGameObjects(bool searchActiveSceneOnly)
    {
        _rootGameObjects.Clear();

        var gameObjects = new List<GameObject>();
        var loadedScenes = GetAllLoadedScene(searchActiveSceneOnly);

        foreach (var loadedScene in loadedScenes)
        {
            _rootGameObjects.AddRange(loadedScene.GetRootGameObjects());
            foreach (var gameObject in _rootGameObjects)
            {
                foreach (var transform in gameObject.GetComponentsInChildren<Transform>())
                {
                    gameObjects.Add(transform.gameObject);
                }
            }
        }
        return gameObjects;
    }

    private List<GameObject> GetLod1OverGameObjects(List<GameObject> rootTargets)
    {
        var lodObject = new List<GameObject>();
        foreach (var target in rootTargets)
        {
            // Null Check
            if (target == null) continue;
            
            var lodGroupComps = target.GetComponentsInChildren<LODGroup>();
            foreach (var lodGroupComp in lodGroupComps)
            {
                LOD[] lodComps = lodGroupComp.GetLODs();
                for (int i = 1; i < lodComps.Length; i++)
                {
                    lodObject.AddRange(lodComps[i].renderers.Select(x => x.gameObject));
                }
            }
        }
        return lodObject;
    }

    private List<Scene> GetAllLoadedScene(bool activeSceneOnly)
    {
        var loadedScene = new List<Scene>();
        if (activeSceneOnly)
        {
            loadedScene.Add(SceneManager.GetActiveScene());
        }
        else
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var targetScene = SceneManager.GetSceneAt(i);
                if (targetScene.isLoaded)
                    loadedScene.Add(targetScene);
            }
        }
        return loadedScene;
    }
    #endregion
}
