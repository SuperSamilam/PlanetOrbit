using System.Collections.Generic;
using UnityEngine;

public class RoadPlacement : MonoBehaviour
{
    [SerializeField] List<Roads> roads = new List<Roads>();

    public DrawMode drawMode;
    [Header("General")]
    [SerializeField] GameObject roadMaster;
    [SerializeField] MeshFilter roadMasterFilter;
    [SerializeField] LayerMask mask;
    [SerializeField] float distanceBetweenRoadVertices = 0.05f;
    [SerializeField] float snappingDistance = 0.05f;

    [Header("Curves")]
    [SerializeField] float roadWitdh = 0.01f;
    [SerializeField] int curveStrenght = 3;
    [SerializeField] float curveThresholdOfsset = 0.01f;

    //misc
    Ray ray;
    RaycastHit hit;

    //Keeping track of clicks and positions
    bool firstClick = false;
    Vector3 firstPos;
    Vector3 mousePos;
    Dictionary<Roads, List<RoadNode>> clickedNodes = new Dictionary<Roads, List<RoadNode>>();
    Roads road;
    RoadNode roadNode;
    bool snaping = false;

    void Update()
    {
        BuildRoad();
    }
    void BuildRoad()
    {
        //check if the roads hits country
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
        {
            //calculates if the node should snap to any already existing road
            bool shouldSnap = false;
            for (int i = 0; i < roads.Count && !shouldSnap; i++)
            {
                for (int j = 0; j < roads[i].points.Count; j++)
                {
                    if (Vector3.Distance(roads[i].points[j].pos, hit.point) < snappingDistance)
                    {
                        //if i havent clicked add theese settings;
                        if (!firstClick)
                        {
                            road = roads[i];
                            roadNode = roads[i].points[j];
                            shouldSnap = true;
                        }
                        else if (firstClick)
                        {
                            road = roads[i];
                            roadNode = roads[i].points[j];
                            shouldSnap = true;
                        }
                        else
                            snaping = false;

                        mousePos = roads[i].points[j].pos;
                        shouldSnap = true;
                        break;
                    }
                }
            }

            //it should not snap give back the old position
            if (!shouldSnap)
                mousePos = hit.point;

            //checks for input on the mousebutton and depedning on what time it is clicked
            if (Input.GetMouseButtonDown(0) && !firstClick)
            {
                firstClick = true;
                firstPos = mousePos;
                if (shouldSnap)
                {
                    snaping = true;
                    clickedNodes.Add(road, new List<RoadNode> { roadNode });
                }
            }
            else if (Input.GetMouseButtonDown(0) && firstClick)
            {
                //Keep track on all the points it clicked and the road parent
                if (shouldSnap)
                {
                    snaping = true;
                    if (clickedNodes.ContainsKey(road))
                        clickedNodes[road].Add(roadNode);
                    else
                        clickedNodes.Add(road, new List<RoadNode> { roadNode });
                }

                List<RoadNode> points = new List<RoadNode>();

                //calculating the angle and archlenght for the points between the start and goal
                float dot = Vector3.Dot(firstPos, mousePos);
                dot = dot / (firstPos.magnitude * mousePos.magnitude);
                float acos = Mathf.Acos(dot);
                float angle = acos * 180 / Mathf.PI;
                float archLenght = angle / 360 * 2 * Mathf.PI * Vector3.Distance(Vector3.zero, firstPos);
                int numberOfVertices = Mathf.CeilToInt(archLenght / distanceBetweenRoadVertices);

                //building linkedlist kinda with all the points
                for (int i = 0; i <= numberOfVertices; i++)
                {
                    RoadNode node = new RoadNode(Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 1.005f);
                    //making sure the connections are right
                    if (i != 0)
                    {
                        node.neigbours.Add(points[i - 1].pos);
                        points[i - 1].neigbours.Add(node.pos);
                    }
                    points.Add(node);
                }

                //adds the roads to the total collection dependent on the snaping
                if (snaping)
                {
                    List<RoadNode> combinedPoints = new List<RoadNode>();

                    //make sure the roads merge with the correct connections
                    int itteration = 0;
                    foreach (KeyValuePair<Roads, List<RoadNode>> kvp in clickedNodes)
                    {
                        for (int i = kvp.Value.Count - 1; i >= 0; i--)
                        {
                            if (i == 0 && itteration == 0)
                                points[0].neigbours.AddRange(kvp.Value[i].neigbours);
                            else
                                points[points.Count - 1].neigbours.AddRange(kvp.Value[i].neigbours);

                            kvp.Key.points.Remove(kvp.Value[i]);
                        }
                        combinedPoints.AddRange(kvp.Key.points);
                        itteration++;
                    }
                    combinedPoints.AddRange(points);

                    //remove the other roads from the list so it shortens
                    foreach (KeyValuePair<Roads, List<RoadNode>> kvp in clickedNodes)
                    {
                        roads.Remove(kvp.Key);
                    }

                    roads.Add(new Roads(combinedPoints));
                    UpdateRoad(roads[roads.Count - 1]);
                }
                else
                {
                    roads.Add(new Roads(points));
                    UpdateRoad(roads[roads.Count - 1]);
                }

                //ressets all values so it can be used again for the next click
                firstPos = Vector3.zero;
                firstClick = false;
                snaping = false;
                clickedNodes.Clear();
                road = null;
                roadNode = null;
            }
        }
    }

    void UpdateRoad(Roads road)
    {
        road.curvedpoints = new List<RoadNode>(road.points);
        road.curvedpoints = UpdateCurve(road.curvedpoints);
        GetEdgesPoints(road);
    }
    void UpdateRoads()
    {
        for (int i = 0; i < roads.Count; i++)
        {
            roads[i].curvedpoints = new List<RoadNode>(roads[i].points);
            roads[i].curvedpoints = UpdateCurve(roads[i].curvedpoints);
            GetEdgesPoints(roads[i]);
        }
    }

    List<RoadNode> UpdateCurve(List<RoadNode> points)
    {
        List<RoadNode> curvedPoints = new List<RoadNode>();
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].neigbours.Count == 2)
            {
                //Calculates if the angel between the 2 neghbouring points are big enought using pytahgoras as the distance of a + b should be larger then c then adding offset
                Vector3 node1 = points[i].neigbours[0];
                Vector3 node2 = points[i].neigbours[1];
                float roadDist = Vector3.Distance(points[i].pos, node1) + Vector3.Distance(points[i].pos, node2);
                float shortDist = Vector3.Distance(node1, node2);

                //if it gets though it is a corner
                if (roadDist - curveThresholdOfsset > shortDist || shortDist > roadDist + curveThresholdOfsset)
                {
                    //makes new nodes so points and curvedPoints wont share the same nodes with different directions
                    RoadNode roadnode1 = new RoadNode(node1);
                    RoadNode roadnode2 = new RoadNode(node2);

                    //Finds the connections it should to the new node
                    float dist = float.MaxValue;
                    Vector3 closestPos = Vector3.zero;
                    for (int c = 0; c < curvedPoints[curvedPoints.Count - 1].neigbours.Count; c++)
                    {
                        if (dist > Vector3.Distance(points[i].pos, curvedPoints[curvedPoints.Count - 1].neigbours[c]))
                        {
                            dist = Vector3.Distance(points[i].pos, curvedPoints[curvedPoints.Count - 1].neigbours[c]);
                            closestPos = curvedPoints[curvedPoints.Count - 1].neigbours[c];
                        }
                    }
                    for (int c = 0; c < curvedPoints[curvedPoints.Count - 1].neigbours.Count; c++)
                    {
                        if (closestPos != curvedPoints[curvedPoints.Count - 1].neigbours[c])
                        {
                            roadnode2.neigbours.Add(curvedPoints[curvedPoints.Count - 1].neigbours[c]);
                        }
                    }
                    curvedPoints[curvedPoints.Count - 1] = roadnode2;

                    dist = float.MaxValue;
                    Debug.Log(points.Count + " count " + i);
                    if (points.Count != i + 1)
                    {
                        for (int c = 0; c < points[i + 1].neigbours.Count; c++)
                        {
                            if (dist > Vector3.Distance(points[i].pos, points[i + 1].neigbours[c]))
                            {
                                dist = Vector3.Distance(points[i].pos, points[i + 1].neigbours[c]);
                                closestPos = points[i + 1].neigbours[c];
                            }
                        }
                        for (int c = 0; c < points[i + 1].neigbours.Count; c++)
                        {
                            if (closestPos != points[i + 1].neigbours[c])
                            {
                                roadnode1.neigbours.Add(points[i + 1].neigbours[c]);
                            }
                        }
                        points[i + 1] = roadnode1;

                    }

                    //chaning the points
                    //for vertice i say exist make a new poin on the curve
                    for (int s = 1; s < curveStrenght; s++)
                    {
                        Vector3 lerp1 = Vector3.Lerp(points[i].pos, node1, s / (float)curveStrenght);
                        Vector3 lerp2 = Vector3.Lerp(node2, points[i].pos, s / (float)curveStrenght);
                        //adding the node and all its connections
                        if (s == curveStrenght - 1)
                        {
                            //fix it
                            curvedPoints.Add(new RoadNode(Vector3.Lerp(lerp2, lerp1, s / (float)curveStrenght), new List<Vector3>() { points[i + 1].pos, curvedPoints[curvedPoints.Count - 1].pos }));
                            curvedPoints[curvedPoints.Count - 1].curve = true;
                            curvedPoints[curvedPoints.Count - 2].neigbours.Add(Vector3.Lerp(lerp2, lerp1, s / (float)curveStrenght));
                            points[i + 1].neigbours.Add(Vector3.Lerp(lerp2, lerp1, s / (float)curveStrenght));
                        }
                        else
                        {
                            curvedPoints.Add(new RoadNode(Vector3.Lerp(lerp2, lerp1, s / (float)curveStrenght), new List<Vector3>() { curvedPoints[curvedPoints.Count - 1].pos }));
                            curvedPoints[curvedPoints.Count - 1].curve = true;
                            curvedPoints[curvedPoints.Count - 2].neigbours.Add(Vector3.Lerp(lerp2, lerp1, s / (float)curveStrenght));
                        }
                    }
                }
                else
                {
                    curvedPoints.Add(points[i]);
                }
            }
            else
            {
                curvedPoints.Add(points[i]);
            }
        }
        return curvedPoints;
    }
    void GetEdgesPoints(Roads road)
    {
        road.listOfNode1.Clear();
        road.listOfNode2.Clear();
        //for each point on the curvedPoints node list make a point to the left and right
        for (int i = 0; i < road.curvedpoints.Count; i++)
        {
            Vector3 right = Vector3.Cross(road.curvedpoints[i].neigbours[0] * 1.01f, road.curvedpoints[i].pos * 1.01f).normalized;
            Vector3 p1 = road.curvedpoints[i].pos + (right * roadWitdh);
            Vector3 p2 = road.curvedpoints[i].pos + (-right * roadWitdh);

            road.listOfNode1.Add(new RoadNode(p1));
            road.listOfNode2.Add(new RoadNode(p2));
        }
    }

    [ContextMenu("buildMesh")]
    void BuildRoadMeshes()
    {
        for (int i = 0; i < roads.Count; i++)
        {
            BuildRoadMesh(roads[i]);
        }
    }

    void BuildRoadMesh(Roads road)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        int offset = 0;

        int lenght = road.listOfNode1.Count;

        for (int i = 0; i < lenght - 1; i++)
        {
            Vector3 p1 = road.listOfNode1[i].pos;
            Vector3 p2 = road.listOfNode2[i].pos;
            Vector3 p3 = road.listOfNode1[i + 1].pos;
            Vector3 p4 = road.listOfNode2[i + 1].pos;

            offset = 4 * i;

            int t1 = offset + 0;
            int t2 = offset + 1;
            int t3 = offset + 2;

            int t4 = offset + 0;
            int t5 = offset + 1;
            int t6 = offset + 2;

            verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
            tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });

            if (i == 100)
                break;

        }


        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        roadMasterFilter.mesh = mesh;

    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mousePos, 0.015f);

        if (firstPos != Vector3.zero)
        {
            Gizmos.DrawSphere(firstPos, 0.015f);
            Gizmos.DrawLine(firstPos, mousePos);
        }
        List<Color> colors = new List<Color>() { Color.blue, Color.black, Color.cyan, Color.gray, Color.green, Color.grey, Color.magenta, Color.white };

        if (drawMode == DrawMode.DrawPoints)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                for (int j = 0; j < roads[i].points.Count; j++)
                {
                    Gizmos.color = colors[i % colors.Count];
                    Gizmos.DrawSphere(roads[i].points[j].pos, 0.01f);
                    for (int k = 0; k < roads[i].points[j].neigbours.Count; k++)
                    {
                        Gizmos.color = Color.black;
                        Gizmos.DrawLine(roads[i].points[j].pos, roads[i].points[j].neigbours[k]);
                    }

                    if (Vector3.Distance(roads[i].points[j].pos, hit.point) < snappingDistance)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawSphere(roads[i].points[j].pos, 0.01f);
                        for (int k = 0; k < roads[i].points[j].neigbours.Count; k++)
                        {
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawLine(roads[i].points[j].pos, roads[i].points[j].neigbours[k]);
                        }
                    }
                }
            }
        }
        else if (drawMode == DrawMode.DrawPointsCurved)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                Gizmos.color = colors[i % colors.Count];
                for (int j = 0; j < roads[i].curvedpoints.Count; j++)
                {
                    Gizmos.color = colors[i % colors.Count];
                    Gizmos.DrawSphere(roads[i].curvedpoints[j].pos, 0.01f);
                    for (int k = 0; k < roads[i].curvedpoints[j].neigbours.Count; k++)
                    {
                        Gizmos.color = Color.black;
                        Gizmos.DrawLine(roads[i].curvedpoints[j].pos, roads[i].curvedpoints[j].neigbours[k]);
                    }

                    if (Vector3.Distance(roads[i].curvedpoints[j].pos, hit.point) < snappingDistance)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawSphere(roads[i].curvedpoints[j].pos, 0.01f);
                        for (int k = 0; k < roads[i].curvedpoints[j].neigbours.Count; k++)
                        {
                            Gizmos.color = Color.yellow;
                            Gizmos.DrawLine(roads[i].curvedpoints[j].pos, roads[i].curvedpoints[j].neigbours[k]);
                        }
                    }
                }
            }
        }
        else if (drawMode == DrawMode.DrawEdges)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                Gizmos.color = colors[i % colors.Count];
                for (int j = 0; j < roads[i].listOfNode1.Count; j++)
                {
                    Gizmos.DrawSphere(roads[i].listOfNode1[j].pos, 0.005f);
                    Gizmos.DrawSphere(roads[i].listOfNode2[j].pos, 0.005f);
                }
            }
        }
    }
    void OnValidate()
    {
        UpdateRoads();
    }

    public enum DrawMode { None, DrawPoints, DrawPointsCurved, DrawEdges }
}
