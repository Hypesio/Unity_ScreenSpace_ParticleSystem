using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public static class MeshUtils
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct QuadsVertex
    {
        public Vector3 position;
    }

    public static Mesh GetQuadMesh()
    {
        Mesh mesh = new Mesh();
        // specify vertex count and layout
        VertexAttributeDescriptor[] layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        };
        int vertexCount = 4;
        mesh.SetVertexBufferParams(vertexCount, layout);

        // set vertex data
        NativeArray<QuadsVertex> verts = new NativeArray<QuadsVertex>(vertexCount, Allocator.Temp);
        verts[0] = new QuadsVertex() { position = new Vector3(-0.5f, -0.5f)};
        verts[1] = new QuadsVertex() { position = new Vector3(0.5f, -0.5f)};
        verts[2] = new QuadsVertex() { position = new Vector3(0.5f, 0.5f)};
        verts[3] = new QuadsVertex() { position = new Vector3(-0.5f, 0.5f)};

        mesh.SetVertexBufferData(verts, 0, 0, vertexCount);
        
        return mesh;
    }
}
