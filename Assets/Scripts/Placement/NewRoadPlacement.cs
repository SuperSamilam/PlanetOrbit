using System.Collections.Generic;
using UnityEngine;

public class NewRoadPlacement : MonoBehaviour
{
    [SerializeField] DrawMode mode;
    [SerializeField] MeshFilter roadMaster;
    [SerializeField] MeshFilter tempRoadFilter;
    List<TempRoad> roads = new List<TempRoad>();


    Ray ray;
    RaycastHit hit;


    Vector3 firstPos;
    TempRoad firstRoad;
    int firstIndex = -1;
    TempRoad secondRoad;
    int secondIndex = -1;
    bool clicked;
    bool snapClicked;
    bool snappingCurveFirst = false;
    Vector3 mousePos;

    TempRoad tempPoints;


    [SerializeField] float roadWitdh;
    [SerializeField] float disBetweenVerts;
    [SerializeField] float snapingDist;

    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            mousePos = hit.point;

            bool snap1 = false;
            bool snap2 = false;
            bool snapping = false;
            for (int r = 0; r < roads.Count && !snapping; r++)
            {
                for (int p = 0; p < roads[r].points.Count; p++)
                {
                    if (Vector3.Distance(roads[r].points[p], hit.point) < snapingDist)
                    {
                        if (!clicked)
                        {
                            firstRoad = roads[r];
                            firstIndex = p;
                        }
                        else
                        {
                            secondRoad = roads[r];
                            secondIndex = p;
                        }

                        mousePos = roads[r].points[p];
                        snapping = true;
                        break;
                    }
                }
            }

            for (int i = 0; i < TempRoad.curvePoints.Count && !snapping; i++)
            {
                if (Vector3.Distance(TempRoad.curvePoints[i], hit.point) < snapingDist)
                {
                    if (!clicked)
                    {
                        firstRoad = null;
                        firstIndex = 0;
                        snapping = true;
                        snap1 = true;
                    }
                    else
                    {
                        snap2 = true;
                        secondRoad = null;
                        secondIndex = 0;
                    }

                    mousePos = TempRoad.curvePoints[i];
                    break;
                }
            }

            if (Input.GetMouseButtonDown(0) && !clicked)
            {
                firstPos = mousePos;
                clicked = true;
                if (snapping)
                    snapClicked = true;
                if (snap1)
                    snappingCurveFirst = true;
            }
            else if (clicked)
            {
                List<Vector3> points = new List<Vector3>();

                float dot = Vector3.Dot(firstPos, mousePos);
                dot = dot / (firstPos.magnitude * mousePos.magnitude);
                float acos = Mathf.Acos(dot);
                float angle = acos * 180 / Mathf.PI;
                float archLenght = angle / 360 * 2 * Mathf.PI * Vector3.Distance(Vector3.zero, firstPos);
                int numberOfVertices = Mathf.CeilToInt(archLenght / disBetweenVerts);

                for (int i = 0; i <= numberOfVertices; i++)
                {
                    points.Add(Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 1.005f);
                }

                tempPoints = new TempRoad();
                tempPoints.points = points;
                if (points.Count > 1)
                {
                    GetEdges(tempPoints);
                    DrawTempMesh();
                }

                if (Input.GetMouseButtonDown(0))
                {
                    //check if the first pos was snaped to
                    if (snapClicked && !snappingCurveFirst)
                    {
                        if (firstIndex != 0 || firstIndex != firstRoad.points.Count - 1)
                        {
                            List<Vector3> road1 = new List<Vector3>();
                            Vector3 road1Extra = Vector3.zero;
                            List<Vector3> road2 = new List<Vector3>();
                            Vector3 road2Extra = Vector3.zero;
                            for (int i = 0; i < firstIndex; i++)
                            {
                                road1.Add(firstRoad.points[i]);
                                if (i + 1 == firstIndex && road1.Count == 1)
                                    road1Extra = firstRoad.points[i + 1];
                            }
                            for (int i = firstIndex + 1; i < firstRoad.points.Count; i++)
                            {
                                road2.Add(firstRoad.points[i]);
                                if (i + 1 == firstRoad.points.Count && road2.Count == 1)
                                    road2Extra = firstRoad.points[i - 1];
                            }
                            roads.Remove(firstRoad);
                            roads.Add(new TempRoad(road1));
                            roads[roads.Count - 1].extraPos = road1Extra;
                            roads.Add(new TempRoad(road2));
                            roads[roads.Count - 1].extraPos = road2Extra;
                        }


                        points.RemoveAt(0);
                        TempRoad.curvePoints.Add(firstPos);
                    }
                    if (snapping && !snap2)
                    {
                        if (secondIndex != 0 || secondIndex != secondRoad.points.Count - 1)
                        {
                            List<Vector3> road1 = new List<Vector3>();
                            Vector3 road1Extra = Vector3.zero;
                            List<Vector3> road2 = new List<Vector3>();
                            Vector3 road2Extra = Vector3.zero;
                            for (int i = 0; i < secondIndex; i++)
                            {
                                road1.Add(secondRoad.points[i]);
                                if (i + 1 == firstIndex && road1.Count == 1)
                                    road1Extra = firstRoad.points[i + 1];
                            }
                            for (int i = secondIndex + 1; i < secondRoad.points.Count; i++)
                            {
                                road2.Add(secondRoad.points[i]);
                                if (i + 1 == firstRoad.points.Count && road2.Count == 1)
                                    road2Extra = firstRoad.points[i - 1];
                            }
                            roads.Remove(secondRoad);
                            roads.Add(new TempRoad(road1));
                            roads[roads.Count - 1].extraPos = road1Extra;
                            roads.Add(new TempRoad(road2));
                            roads[roads.Count - 1].extraPos = road2Extra;
                        }
                        points.RemoveAt(points.Count - 1);
                        TempRoad.curvePoints.Add(mousePos);
                    }

                    if (snappingCurveFirst)
                    {
                        points.RemoveAt(0);
                    }
                    if (snap2)
                    {
                        points.RemoveAt(points.Count - 1);
                    }

                    roads.Add(new TempRoad(points));

                    clicked = false;
                    firstPos = Vector3.zero;
                    firstRoad = null;
                    snapClicked = false;
                    snappingCurveFirst = false;

                    GetAllEdges();
                    DrawBigMesh();
                }
            }
        }
    }

    void GetAllEdges()
    {
        for (int i = 0; i < roads.Count; i++)
        {
            GetEdges(roads[i]);
        }
    }
    void GetEdges(TempRoad tempRoad)
    {
        tempRoad.list1.Clear();
        tempRoad.list2.Clear();

        if (tempRoad.points.Count == 1)
        {
            Vector3 right = Vector3.Cross(tempRoad.points[0], tempRoad.extraPos).normalized;
            Vector3 p1 = tempRoad.points[0] + (right * roadWitdh);
            Vector3 p2 = tempRoad.points[0] + (-right * roadWitdh);

            tempRoad.list1.Add(p2);
            tempRoad.list2.Add(p1);
            return;
        }

        for (int j = 0; j < tempRoad.points.Count; j++)
        {
            Vector3 right;
            if (j + 1 == tempRoad.points.Count)
            {
                right = Vector3.Cross(tempRoad.points[j], tempRoad.points[j - 1]).normalized;
                Vector3 p1 = tempRoad.points[j] + (right * roadWitdh);
                Vector3 p2 = tempRoad.points[j] + (-right * roadWitdh);

                tempRoad.list1.Add(p2);
                tempRoad.list2.Add(p1);
            }
            else
            {
                right = Vector3.Cross(tempRoad.points[j], tempRoad.points[j + 1]).normalized;

                Vector3 p1 = tempRoad.points[j] + (right * roadWitdh);
                Vector3 p2 = tempRoad.points[j] + (-right * roadWitdh);

                tempRoad.list1.Add(p1);
                tempRoad.list2.Add(p2);
            }
        }
    }

    void DrawBigMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        int offset = 0;
        int totalCount = 0;

        for (int i = 0; i < roads.Count; i++)
        {
            int lenght = roads[i].list1.Count;
            for (int j = 1; j < lenght; j++)
            {
                Vector3 p1 = roads[i].list1[j - 1];
                Vector3 p2 = roads[i].list2[j - 1];
                Vector3 p3 = roads[i].list1[j];
                Vector3 p4 = roads[i].list2[j];

                offset = 4 * totalCount;

                int t1 = offset + 0;
                int t2 = offset + 1;
                int t3 = offset + 2;

                int t4 = offset + 3;
                int t5 = offset + 2;
                int t6 = offset + 1;

                verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
                tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });
                totalCount++;
            }
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        roadMaster.mesh = mesh;
    }
    void DrawTempMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        int offset = 0;
        int totalCount = 0;


        int lenght = tempPoints.list1.Count;
        for (int j = 1; j < lenght; j++)
        {
            Vector3 p1 = tempPoints.list1[j - 1];
            Vector3 p2 = tempPoints.list2[j - 1];
            Vector3 p3 = tempPoints.list1[j];
            Vector3 p4 = tempPoints.list2[j];

            offset = 4 * totalCount;

            int t1 = offset + 0;
            int t2 = offset + 1;
            int t3 = offset + 2;

            int t4 = offset + 3;
            int t5 = offset + 2;
            int t6 = offset + 1;

            verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
            tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });
            totalCount++;
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        tempRoadFilter.mesh = mesh;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (clicked && tempPoints != null)
        {
            Gizmos.DrawSphere(firstPos, 0.01f);
            Gizmos.color = Color.yellow;
            for (int j = 0; j < tempPoints.points.Count; j++)
            {
                Gizmos.DrawSphere(tempPoints.points[j], 0.005f);
            }
        }
        Gizmos.DrawSphere(mousePos, 0.01f);


        List<Color> colors = new List<Color>() { Color.blue, Color.cyan, Color.gray, Color.green, Color.magenta, Color.white };
        if (mode == DrawMode.Points)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                Gizmos.color = colors[i % colors.Count];
                for (int j = 0; j < roads[i].points.Count; j++)
                {
                    Gizmos.DrawSphere(roads[i].points[j], 0.005f);
                }
            }
        }
        else if (mode == DrawMode.EdgePoints)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                Gizmos.color = colors[i % colors.Count];
                for (int j = 0; j < roads[i].list1.Count; j++)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(roads[i].list1[j], 0.005f);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(roads[i].list2[j], 0.005f);
                }
            }
        }


        Gizmos.color = Color.yellow;
        for (int j = 0; j < TempRoad.curvePoints.Count; j++)
        {
            Gizmos.DrawSphere(TempRoad.curvePoints[j], 0.003f);
        }

    }

    void OnValidate()
    {
        GetAllEdges();
        DrawBigMesh();
    }
}


public class TempRoad
{
    public static List<Vector3> curvePoints = new List<Vector3>();
    public List<Vector3> points;
    public List<Vector3> roadCurvePoints;
    public List<Vector3> list1;
    public List<Vector3> list2;
    public Vector3 extraPos;

    public TempRoad(List<Vector3> points)
    {
        this.points = points;
        roadCurvePoints = new List<Vector3>();
        list1 = new List<Vector3>();
        list2 = new List<Vector3>();
    }
    public TempRoad()
    {
        points = new List<Vector3>();
        roadCurvePoints = new List<Vector3>();
        list1 = new List<Vector3>();
        list2 = new List<Vector3>();
    }
}

public enum DrawMode { None, Points, EdgePoints }

