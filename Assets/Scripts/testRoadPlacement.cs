using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph;
using UnityEngine;

public class testRoadPlacement : MonoBehaviour
{
    public bool DrawEdges = false;
    public bool autoUpdate = false;
    public MeshFilter filter;
    public List<Vector3> points = new List<Vector3>();
    List<Vector3> point1 = new List<Vector3>();
    List<Vector3> point2 = new List<Vector3>();


    void getEdges()
    {
        point1.Clear();
        point2.Clear();
        //for each point on the curvedPoints node list make a point to the left and right
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 right = Vector3.Cross(points[i], Vector3.forward).normalized;
            Vector3 p1 = points[i] + (right * 1f);
            Vector3 p2 = points[i] + (-right * 1f);

            point1.Add(p1);
            point2.Add(p2);
        }
        Debug.Log(point1.Count + " " + point2.Count);
    }

    void buildMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        int offset = 0;

        int lenght = point1.Count;

        for (int i = 0; i < lenght - 1; i++)
        {
            Vector3 p1 = point1[i];
            Vector3 p2 = point2[i];
            Vector3 p3 = point1[i + 1];
            Vector3 p4 = point2[i + 1];

            offset = 4 * i;

            int t1 = offset + 0;
            int t2 = offset + 1;
            int t3 = offset + 2;

            int t4 = offset + 3;
            int t5 = offset + 2;
            int t6 = offset + 1;

            verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
            tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });

        }


        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        Debug.Log("built");
        filter.mesh = mesh;
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (DrawEdges && point1.Count != 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < point1.Count; i++)
            {
                Gizmos.DrawSphere(point1[i], 0.1f);
                Gizmos.DrawSphere(point2[i], 0.1f);
            }
            return;
        }
        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.DrawSphere(points[i], 0.1f);
        }
    }

    void OnValidate()
    {
        if (!autoUpdate)
            return;

        getEdges();
        buildMesh();
    }
}
