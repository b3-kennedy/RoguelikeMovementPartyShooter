using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class PathGenerator : NetworkBehaviour
{
    public Transform pathStartMin;
    public Transform pathStartMax;

    public Transform pathEndMin;
    public Transform pathEndMax;

    public Material pathMaterial;

    public int iterations;
    public float displacementAmount;

    private NetworkVariable<int> pathSeed = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    
    public void OnSpawned()
    {
        pathSeed.OnValueChanged += (_, newSeed) => GenerateAndBuildPath(newSeed);

        if (IsServer)
        {
            pathSeed.Value = Random.Range(int.MinValue, int.MaxValue);
        }
        else if (pathSeed.Value != 0)
        {
            // covers the case where the client spawns after the value's already set
            GenerateAndBuildPath(pathSeed.Value);
        }
    }

    void GenerateAndBuildPath(int seed)
    {
        var rng = new System.Random(seed);

        Vector3 minPos = pathStartMin.localPosition;
        Vector3 maxPos = pathStartMax.localPosition;
        Vector3 endMinPos = pathEndMin.localPosition;
        Vector3 endMaxPos = pathEndMax.localPosition;

        Vector3 start = new Vector3(Lerp(rng, minPos.x, maxPos.x), minPos.y, minPos.z);
        Vector3 end = new Vector3(Lerp(rng, endMinPos.x, endMaxPos.x), endMinPos.y, endMinPos.z);

        float displaceAmount = Vector3.Distance(start, end) * displacementAmount;

        Bounds bounds = new Bounds(pathStartMin.localPosition, Vector3.zero);
        bounds.Encapsulate(pathEndMax.localPosition);
        
        List<Vector3> path = GeneratePath(rng, start, end, iterations, displaceAmount, bounds);
        Mesh mesh = BuildPathMesh(path, 3f);

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = pathMaterial;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    float Lerp(System.Random rng, float min, float max) => (float)(rng.NextDouble() * (max - min) + min);

    List<Vector3> GeneratePath(System.Random rng, Vector3 start, Vector3 end, int iterations, float displaceAmount, Bounds bounds)
    {
        var points = new List<Vector3> { start, end };

        for (int i = 0; i < iterations; i++)
        {
            var newPoints = new List<Vector3> { points[0] };
            for (int j = 0; j < points.Count - 1; j++)
            {
                Vector3 a = points[j];
                Vector3 b = points[j + 1];
                Vector3 mid = (a + b) * 0.5f;

                Vector3 dir = (b - a).normalized;
                Vector3 perp = Vector3.Cross(dir, Vector3.up);
                mid += perp * (float)(rng.NextDouble() * 2 - 1) * displaceAmount;

                mid = bounds.ClosestPoint(mid);

                newPoints.Add(mid);
                newPoints.Add(b);
            }
            points = newPoints;
            displaceAmount *= 0.5f;
        }
        return points;
    }

    Mesh BuildPathMesh(List<Vector3> points, float width)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 dirIn = i > 0 ? (points[i] - points[i - 1]).normalized : Vector3.zero;
            Vector3 dirOut = i < points.Count - 1 ? (points[i + 1] - points[i]).normalized : Vector3.zero;
            Vector3 dir = (dirIn + dirOut).sqrMagnitude > 0.0001f
                ? (dirIn + dirOut).normalized
                : (dirIn != Vector3.zero ? dirIn : dirOut);

            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            float miter = dirIn != Vector3.zero && dirOut != Vector3.zero
                ? 1f / Mathf.Max(0.3f, Vector3.Dot(right, Vector3.Cross(Vector3.up, dirOut).normalized))
                : 1f;

            verts.Add(points[i] - right * width * 0.5f * miter);
            verts.Add(points[i] + right * width * 0.5f * miter);

            if (i > 0)
            {
                int a = (i - 1) * 2, b = a + 1, c = i * 2, d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        return mesh;
    }
}