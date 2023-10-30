using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BlendShapePostprocessor : AssetPostprocessor
{
    struct BlendShape
    {
        public string Name;
        public Vector3[] Verts;
        public Vector3[] Tangents;
        public Vector3[] Normals;
    }
    
    private void OnPostprocessModel(GameObject g)
    {
        var rs = g.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var r in rs)
        {
            Process(r);
        }
    }

    private void Process(SkinnedMeshRenderer r)
    {
        var mesh = r.sharedMesh;

        var shapes = new List<BlendShape>();

        for (var i = 0; i < mesh.blendShapeCount; i++)
        {
            if (mesh.GetBlendShapeFrameCount(i) != 1)
            {
                return;
            }

            var verts = new Vector3[mesh.vertexCount];
            var normals = new Vector3[mesh.vertexCount];
            var tangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(i, 0, verts, normals, tangents);
            shapes.Add(new BlendShape()
            {
                Name = mesh.GetBlendShapeName(i),
                Verts = verts,
                Normals = normals,
                Tangents = tangents
            });
        }

        mesh.ClearBlendShapes();
        var groups = new List<string>();

        foreach (var shape in shapes)
        {
            if (GetChildrenBlendShape(shapes, shape.Name)
                    .Count > 0)
            {
                Debug.Log("Group/"+shape.Name);
                groups.Add(shape.Name);
            }
        }

        foreach (var shape in shapes)
        {
            if (groups.Contains(shape.Name))
            {
                // parent
                continue;
            }
            var n = new Regex("(.*)_(\\d+)").Match(shape.Name);
            if (!n.Success)
            {
                // Group not found
                mesh.AddBlendShapeFrame(shape.Name, 1f, shape.Verts, shape.Normals, shape.Tangents);
                continue;
            }

            if (shapes.All(s => s.Name != n.Groups[1]
                    .Value))
            {
                // Group not found
                mesh.AddBlendShapeFrame(shape.Name, 1f, shape.Verts, shape.Normals, shape.Tangents);
                continue;
            }

            var groupName = n.Groups[1]
                .Value;
            var groupWeight = int.Parse(n.Groups[2]
                .Value) / 100f;
            Debug.Log($"{groupName} / {groupWeight}");
            mesh.AddBlendShapeFrame(groupName,groupWeight , shape.Verts, shape.Normals, shape.Tangents);
        }

        mesh.UploadMeshData(false);
    }

    private List<int> GetChildrenBlendShape(List<BlendShape> shapes, string prefix)
    {
        var children = new List<int>();

        for (var i = 0; i < shapes.Count; i++)
        {
            var name = shapes[i].Name;
            if (name.StartsWith(prefix + "_") && name != prefix)
            {
                children.Add(i);
            }
        }
        return children;
    }
}