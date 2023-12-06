using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class RoadPlacement : MonoBehaviour
{
    [SerializeField] List<Roads> roads;


    [SerializeField] GameObject roadObj;
    [SerializeField] LayerMask mask;
    [SerializeField] float distanceBetweenRoadVertices = 0.05f;
    [SerializeField] float roadWitdh = 0.05f;

    [SerializeField] MeshFilter meshMaster;
    Ray ray;
    RaycastHit hit;
    Vector3 firstPos;
    bool firstClick;
    List<GameObject> list = new List<GameObject>();


    Mesh mesh;

    void Start()
    {
        roads = new List<Roads>();
        mesh = new Mesh();
    }
    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
        {
            if (!firstClick && Input.GetMouseButtonDown(0))
            {
                firstClick = true;
                firstPos = hit.point;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                List<Vector3> pos1 = new List<Vector3>();
                List<Vector3> pos2 = new List<Vector3>();

                //Spline 
                float dot = Vector3.Dot(firstPos, hit.point);
                dot = dot / (firstPos.magnitude * hit.point.magnitude);
                float acos = Mathf.Acos(dot);
                float angle = acos * 180 / Mathf.PI;
                float archLenght = angle / 360 * 2 * Mathf.PI * Vector3.Distance(Vector3.zero, firstPos);
                int numberOfVertices = Mathf.CeilToInt(archLenght / distanceBetweenRoadVertices);

                for (int i = 0; i <= numberOfVertices; i++)
                {
                    Vector3 mainPoint = Vector3.Slerp(firstPos, hit.point, i / (float)numberOfVertices) * 1.005f;
                    Vector3 secondPoint = Vector3.Slerp(firstPos, hit.point, (i + 1) / (float)numberOfVertices) * 1.005f;

                    if (i == numberOfVertices)
                        secondPoint = Vector3.Slerp(firstPos, hit.point, (i - 1) / (float)numberOfVertices);

                    Vector3 dir = (secondPoint - mainPoint).normalized;

                    Ray roadRay = new Ray(mainPoint * 2, -mainPoint * 0.5f);
                    RaycastHit roadHit;
                    if (Physics.Raycast(roadRay, out roadHit, Mathf.Infinity))
                    {
                        Vector3 side = Vector3.Cross(dir, roadHit.normal);
                        Vector3 p1 = mainPoint + (side * roadWitdh);
                        Vector3 p2 = mainPoint + (-side * roadWitdh);

                        pos1.Add(p1);
                        pos2.Add(p2);
                    }

                }




                //WeirdCode

                // float dot = Vector3.Dot(firstPos, hit.point);
                // dot = dot / (firstPos.magnitude * hit.point.magnitude);
                // float acos = Mathf.Acos(dot);
                // float angle = acos * 180 / Mathf.PI;
                // float archLenght = angle / 360 * 2 * Mathf.PI * Vector3.Distance(Vector3.zero, firstPos);
                // int numberOfVertices = Mathf.CeilToInt(archLenght / distanceBetweenRoadVertices);

                // for (int i = 0; i <= numberOfVertices; i++)
                // {
                //     Vector3 mainPoint = Vector3.Slerp(firstPos, hit.point, i / (float)numberOfVertices);
                //     Vector3 secondPoint = Vector3.Slerp(firstPos, hit.point, (i + 1) / (float)numberOfVertices);

                //     if (i == numberOfVertices)
                //         secondPoint = Vector3.Slerp(firstPos, hit.point, (i - 1) / (float)numberOfVertices);

                //     Ray roadRay = new Ray(mainPoint * 2, -mainPoint * 0.5f);
                //     RaycastHit roadHit;

                //     Vector3 dir = (secondPoint - mainPoint).normalized;

                //     if (Physics.Raycast(roadRay, out roadHit, Mathf.Infinity))
                //     {
                //         GameObject road = Instantiate(roadObj, mainPoint * 1.005f, Quaternion.identity);

                //         road.transform.up = roadHit.normal;
                //         road.transform.rotation = Quaternion.FromToRotation(road.transform.forward, dir) * road.transform.rotation;




                //         list.Add(road);
                //     }
                // }
                roads.Add(new Roads(pos1, pos2));
                makeMesh();
                firstClick = false;
                firstPos = Vector3.zero;
            }
            else
            {
                // hand.transform.position = hit.point;
                // prefab.transform.up = hit.normal;
                // prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
                // return;
            }
        }
    }

    void makeMesh()
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        int offsetExisting = 0;
        for (int i = 0; i < roads.Count; i++)
        {
            for (int j = 1; j <= roads[i].line1.Count; j++)
            {
                Vector3 p1 = roads[i].line1[j - 1];
                Vector3 p2 = roads[i].line2[j - 1];
                Vector3 p3;
                Vector3 p4;

                if (j == roads[i].line1.Count)
                {
                    p3 = roads[i].line1[0];
                    p4 = roads[i].line2[0];
                }
                else
                {
                    p3 = roads[i].line1[j];
                    p4 = roads[i].line2[j];
                }

                int offset = offsetExisting + 4 * (j - 1);

                int t1 = offset + 0;
                int t2 = offset + 2;
                int t3 = offset + 3;

                int t4 = offset + 3;
                int t5 = offset + 1;
                int t6 = offset + 0;

                verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
                tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });
            }
            offsetExisting = verts.Count;
        }

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        meshMaster.mesh = mesh;
    }

    void OnDrawGizmos()
    {
        // for (int i = 0; i < pos1.Count; i++)
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawSphere(pos1[i], 0.01f);
        //     Gizmos.DrawSphere(pos2[i], 0.01f);

        // }
    }
}
