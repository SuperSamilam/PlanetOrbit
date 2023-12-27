using System;
using System.Collections.Generic;
using System.Data.Common;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class RoadPlacement : MonoBehaviour
{
    public DrawMode drawMode;

    List<Roads> roads = new List<Roads>();

    [SerializeField] Camera camera;

    [Header("Road Settings")]
    [SerializeField] float distBetweenPoints = 0.03f;
    [SerializeField] float roadWitdh = 0.03f;
    [SerializeField] float curveThres = 0.03f;

    [Header("General")]
    [SerializeField] float snapingDist = 0.03f;

    Vector3 mousePos;
    bool haveClicked = false;

    Vector3 firstPos;
    Roads road1;
    int index1;
    Roads road2;
    int index2;

    //snapping
    bool snaping = false;

    //debug
    RoadNode snapPoint = new RoadNode();
    List<RoadNode> tempPoints = new List<RoadNode>();

    public void Update()
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            mousePos = hit.point;

            //check for snaping
            bool shouldSnap = false;
            for (int r = 0; r < roads.Count && !shouldSnap; r++)
            {
                for (int p = 0; p < roads[r].points.Count; p++)
                {
                    if (Vector3.Distance(roads[r].points[p].pos, hit.point) < snapingDist)
                    {
                        if (!haveClicked)
                        {
                            road1 = roads[r];
                            index1 = p;
                        }
                        else
                        {
                            road2 = roads[r];
                            index2 = p;
                        }

                        mousePos = roads[r].points[p].pos;
                        snapPoint = roads[r].points[p];
                        shouldSnap = true;
                    }
                }
            }

            if (Input.GetMouseButtonDown(0) && !haveClicked)
            {
                firstPos = mousePos;
                haveClicked = true;
                if (shouldSnap)
                    snaping = true;
            }
            else if (haveClicked)
            {
                tempPoints.Clear();

                float dot = Vector3.Dot(firstPos, mousePos);
                dot = dot / (firstPos.magnitude * mousePos.magnitude);
                float acos = Mathf.Acos(dot);
                float angle = acos * 180 / Mathf.PI;
                float archLenght = angle / 360 * 2 * Mathf.PI * Vector3.Distance(Vector3.zero, firstPos);
                int numberOfVertices = Mathf.CeilToInt(archLenght / distBetweenPoints);

                for (int i = 0; i <= numberOfVertices; i++)
                {
                    RoadNode node = new RoadNode();
                    node.pos = Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 1.005f;

                    if (i != 0)
                    {
                        node.neighbours.Add(tempPoints[tempPoints.Count - 1].pos);
                        tempPoints[tempPoints.Count - 1].neighbours.Add(node.pos);
                    }

                    tempPoints.Add(node);
                }


                if (Input.GetMouseButtonDown(0))
                {
                    bool curved = false;
                    Roads tempRoad = new Roads(tempPoints);
                    if (snaping)
                    {
                        //Snaping to the end of a road creating a curve
                        if (index1 == 0 || index1 == road1.points.Count - 1)
                        {
                            Vector3 mainDown = new Vector3();
                            Vector3 up = tempRoad.points[1].pos;

                            if (index1 == 0)
                                mainDown = road1.points[index1 + 1].pos;
                            if (index1 == road1.points.Count - 1)
                                mainDown = road1.points[index1 - 1].pos;

                            if (Vector3.Distance(mainDown, up) > curveThres)
                            {
                                if (road1 == road2 && shouldSnap)
                                {
                                    List<RoadNode> combinedPoints = new List<RoadNode>();
                                    tempPoints[1].neighbours.Remove(tempPoints[0].pos);
                                    tempPoints.RemoveAt(0);

                                    road1.points[0].curve = true;
                                    road1.points[road1.points.Count - 1].curve = true;

                                    tempPoints[0].neighbours.Add(road1.points[0].pos);
                                    road1.points[0].neighbours.Add(tempPoints[0].pos);

                                    tempPoints[tempPoints.Count - 2].neighbours.Remove(tempPoints[tempPoints.Count - 1].pos);
                                    tempPoints.RemoveAt(tempPoints.Count - 1);
                                    tempPoints[tempPoints.Count - 1].neighbours.Add(road1.points[road1.points.Count - 1].pos);
                                    road1.points[road1.points.Count - 1].neighbours.Add(tempPoints[tempPoints.Count - 1].pos);

                                    tempPoints.Reverse();
                                    combinedPoints.AddRange(tempPoints);
                                    combinedPoints.AddRange(road1.points);

                                    roads.Remove(road1);
                                    Roads road = new Roads(combinedPoints);
                                    roads.Add(road);
                                    resetClick();
                                    return;
                                }

                                curved = true;
                                tempPoints[1].neighbours.Remove(tempPoints[0].pos);
                                tempPoints.RemoveAt(0);
                                List<RoadNode> combined = new List<RoadNode>();
                                road1.points[index1].neighbours.Add(tempPoints[0].pos);
                                tempPoints[0].neighbours.Add(road1.points[index1].pos);

                                if (index1 == 0)
                                {
                                    tempPoints.Reverse();
                                    combined.AddRange(tempPoints);
                                    combined.AddRange(road1.points);
                                    road1.points[0].curve = true;
                                }
                                else if (index1 == road1.points.Count - 1)
                                {
                                    tempPoints.Reverse();
                                    road1.points.Reverse();
                                    combined.AddRange(tempPoints);
                                    combined.AddRange(road1.points);
                                    road1.points[0].curve = true;
                                }
                                roads.Remove(road1);
                                Roads combinedRoad = new Roads(combined);
                                roads.Add(combinedRoad);
                                tempRoad = combinedRoad;
                            }
                            else
                            {
                                resetClick();
                                return;
                            }
                        }
                        else
                        {
                            tempRoad.points[1].neighbours.Remove(tempRoad.points[0].pos);
                            tempRoad.points.RemoveAt(0);
                            List<RoadNode> road1Points = new List<RoadNode>();
                            road1Points.AddRange(road1.points.GetRange(0, index1));
                            road1Points[road1Points.Count - 1].neighbours.Remove(firstPos);

                            List<RoadNode> road2Points = new List<RoadNode>();
                            road2Points.AddRange(road1.points.GetRange(index1 + 1, road1.points.Count - index1 - 1));
                            road2Points[0].neighbours.Remove(firstPos);

                            if (road1Points.Count != 0 && road2Points.Count != 0)
                            {
                                Roads r1 = new Roads(road1Points);
                                Roads r2 = new Roads(road2Points);
                                if (road1Points.Count == 1)
                                    r1.extraPos = firstPos;
                                if (road2Points.Count == 1)
                                    r2.extraPos = firstPos;
                                Roads.intersection.Add(firstPos, new List<Roads>() { r1, r2, tempRoad });
                                roads.Add(r1);
                                roads.Add(r2);
                            }
                            else if (road1Points.Count != 0)
                            {
                                Roads r1 = new Roads(road1Points);
                                Roads.intersection.Add(firstPos, new List<Roads>() { r1, tempRoad });
                                roads.Add(r1);
                            }
                            else if (road2Points.Count != 0)
                            {
                                Roads r2 = new Roads(road1Points);
                                Roads.intersection.Add(firstPos, new List<Roads>() { r2, tempRoad });
                                roads.Add(r2);
                            }
                            roads.Remove(road1);
                            roads.Add(tempRoad);
                        }
                    }
                    if (shouldSnap)
                    {
                        //Snaping to the end of a road creating a curve
                        if (index2 == 0 || index2 == road2.points.Count - 1)
                        {
                            if (curved)
                            {
                                Vector3 mainDown = new Vector3();
                                Vector3 up = tempRoad.points[tempRoad.points.Count - 1].pos;

                                if (index2 == 0)
                                    mainDown = road2.points[index2 + 1].pos;
                                if (index2 == road2.points.Count - 1)
                                    mainDown = road2.points[index2 - 1].pos;

                                if (Vector3.Distance(mainDown, up) > curveThres)
                                {
                                    tempRoad.points[1].neighbours.Remove(tempRoad.points[0].pos);
                                    tempRoad.points.RemoveAt(0);
                                    road2.points[road2.points.Count - 1].curve = true;
                                    road2.points[index2].neighbours.Add(tempRoad.points[0].pos);
                                    tempRoad.points[0].neighbours.Add(road2.points[index2].pos);

                                    List<RoadNode> combined = new List<RoadNode>();

                                    if (index2 == 0)
                                    {
                                        tempRoad.points.Reverse();
                                        combined.AddRange(tempRoad.points);
                                        combined.AddRange(road2.points);
                                    }
                                    else if (index2 == road2.points.Count - 1)
                                    {
                                        tempRoad.points.Reverse();
                                        road2.points.Reverse();
                                        combined.AddRange(tempRoad.points);
                                        combined.AddRange(road2.points);
                                    }
                                    roads.Remove(road2);
                                    roads.Remove(tempRoad);
                                    Roads combinedRoad = new Roads(combined);
                                    roads.Add(combinedRoad);
                                }
                                else
                                {
                                    resetClick();
                                    return;
                                }
                            }
                            else
                            {
                                Vector3 mainDown = new Vector3();
                                Vector3 up = tempPoints[1].pos;

                                if (index2 == 0)
                                    mainDown = road2.points[index2 + 1].pos;
                                if (index2 == road2.points.Count - 1)
                                    mainDown = road2.points[index2 - 1].pos;

                                if (Vector3.Distance(mainDown, up) > curveThres)
                                {
                                    tempPoints[tempPoints.Count-2].neighbours.Remove(tempPoints[tempPoints.Count-1].pos);
                                    tempPoints.RemoveAt(tempPoints.Count-1);
                                    //road2.points[road2.points.Count - 1].curve = true;
                                    List<RoadNode> combined = new List<RoadNode>();
                                    road2.points[index2].neighbours.Add(tempPoints[0].pos);
                                    tempPoints[tempPoints.Count-1].neighbours.Add(road2.points[index2].pos);

                                    Debug.Log(tempPoints.Count);
                                    if (index2 == 0)
                                    {
                                        tempPoints.Reverse();
                                        combined.AddRange(tempPoints);
                                        combined.AddRange(road2.points);
                                    }
                                    else if (index2 == road2.points.Count - 1)
                                    {
                                        tempPoints.Reverse();
                                        road2.points.Reverse();
                                        combined.AddRange(tempPoints);
                                        combined.AddRange(road2.points);
                                    }
                                    roads.Remove(road2);
                                    Roads combinedRoad = new Roads(combined);
                                    roads.Add(combinedRoad);

                                }
                                else
                                {
                                    Debug.Log(Vector3.Distance(mainDown, up));
                                    resetClick();
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if (curved)
                            {
                                tempRoad.points[1].neighbours.Remove(tempRoad.points[0].pos);
                                tempRoad.points.RemoveAt(0);

                                List<RoadNode> road1Points = new List<RoadNode>();
                                road1Points.AddRange(road2.points.GetRange(0, index2));
                                road1Points[road1Points.Count - 1].neighbours.Remove(mousePos);

                                List<RoadNode> road2Points = new List<RoadNode>();
                                road2Points.AddRange(road2.points.GetRange(index2 + 1, road2.points.Count - index2 - 1));
                                road2Points[0].neighbours.Remove(mousePos);

                                if (road1Points.Count != 0 && road2Points.Count != 0)
                                {
                                    Roads r1 = new Roads(road1Points);
                                    Roads r2 = new Roads(road2Points);
                                    if (road1Points.Count == 1)
                                        r1.extraPos = mousePos;
                                    if (road2Points.Count == 1)
                                        r2.extraPos = mousePos;
                                    Roads.intersection.Add(mousePos, new List<Roads>() { r1, r2, tempRoad });
                                    roads.Add(r1);
                                    roads.Add(r2);
                                }
                                else if (road1Points.Count != 0)
                                {
                                    Roads r1 = new Roads(road1Points);
                                    Roads.intersection.Add(mousePos, new List<Roads>() { r1, tempRoad });
                                    roads.Add(r1);
                                }
                                else if (road2Points.Count != 0)
                                {
                                    Roads r2 = new Roads(road1Points);
                                    Roads.intersection.Add(mousePos, new List<Roads>() { r2, tempRoad });
                                    roads.Add(r2);
                                }

                                roads.Remove(road2);
                                roads.Add(tempRoad);

                                resetClick();
                                return;
                            }
                            else
                            {
                                tempRoad.points[tempRoad.points.Count - 2].neighbours.Remove(tempRoad.points[tempRoad.points.Count - 1].pos);
                                tempRoad.points.RemoveAt(tempRoad.points.Count - 1);

                                List<RoadNode> road1Points = new List<RoadNode>();
                                road1Points.AddRange(road2.points.GetRange(0, index2));
                                road1Points[road1Points.Count - 1].neighbours.Remove(mousePos);

                                List<RoadNode> road2Points = new List<RoadNode>();
                                road2Points.AddRange(road2.points.GetRange(index2 + 1, road2.points.Count - index2 - 1));
                                road2Points[0].neighbours.Remove(mousePos);

                                if (road1Points.Count != 0 && road2Points.Count != 0)
                                {
                                    Roads r1 = new Roads(road1Points);
                                    Roads r2 = new Roads(road2Points);
                                    if (road1Points.Count == 1)
                                        r1.extraPos = mousePos;
                                    if (road2Points.Count == 1)
                                        r2.extraPos = mousePos;
                                    Roads.intersection.Add(mousePos, new List<Roads>() { r1, r2, tempRoad });
                                    roads.Add(r1);
                                    roads.Add(r2);
                                }
                                else if (road1Points.Count != 0)
                                {
                                    Roads r1 = new Roads(road1Points);
                                    Roads.intersection.Add(mousePos, new List<Roads>() { r1, tempRoad });
                                    roads.Add(r1);
                                }
                                else if (road2Points.Count != 0)
                                {
                                    Roads r2 = new Roads(road1Points);
                                    Roads.intersection.Add(mousePos, new List<Roads>() { r2, tempRoad });
                                    roads.Add(r2);
                                }
                                roads.Remove(road2);
                                roads.Add(tempRoad);
                            }
                        }
                    }

                    if (!snaping && !shouldSnap)
                        roads.Add(tempRoad);
                    resetClick();
                }
            }


        }
    }

    void resetClick()
    {
        haveClicked = false;
        firstPos = Vector3.zero;
        tempPoints = new List<RoadNode>();
        snaping = false;
        road1 = null;
        road2 = null;
        index1 = -1;
        index2 = -1;
        GetRoadEdges();
    }

    void GetRoadEdges()
    {
        for (int r = 0; r < roads.Count; r++)
        {
            roads[r].list1.Clear();
            roads[r].list2.Clear();

            if (roads[r].points.Count == 1)
            {
                Vector3 right = Vector3.Cross(roads[r].points[0].pos, roads[r].extraPos).normalized;
                Vector3 p1 = roads[r].points[0].pos + (right * roadWitdh);
                Vector3 p2 = roads[r].points[0].pos + (-right * roadWitdh);
                roads[r].list1.Add(p1);
                roads[r].list2.Add(p2);
                continue;
            }
            for (int p = 0; p < roads[r].points.Count; p++)
            {
                Vector3 right;
                if (roads[r].points[p].curve)
                {
                    // Vector3 forward = new Vector3();
                    // if (p + 1 == roads[r].points.Count)
                    //     forward = roads[r].points[0].pos;
                    // else 
                    //     forward = roads[r].points[p + 1].pos;

                    // Vector3 back = roads[r].points[p - 1].pos;



                    // roads[r].list1.Add(roads[r].points[p].pos);
                    // roads[r].list2.Add(Vector3.Lerp(back, forward, 0.5f));


                }
                else if (p + 1 == roads[r].points.Count)
                {
                    right = Vector3.Cross(roads[r].points[p].pos, roads[r].points[p - 1].pos).normalized;
                    Vector3 p1 = roads[r].points[p].pos + (right * roadWitdh);
                    Vector3 p2 = roads[r].points[p].pos + (-right * roadWitdh);

                    roads[r].list1.Add(p2);
                    roads[r].list2.Add(p1);
                }
                else
                {
                    right = Vector3.Cross(roads[r].points[p].pos, roads[r].points[p + 1].pos).normalized;
                    Vector3 p1 = roads[r].points[p].pos + (right * roadWitdh);
                    Vector3 p2 = roads[r].points[p].pos + (-right * roadWitdh);

                    roads[r].list1.Add(p1);
                    roads[r].list2.Add(p2);
                }
            }
        }
    }





    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (haveClicked && tempPoints.Count != 0)
        {
            Gizmos.color = Color.yellow;
            for (int j = 0; j < tempPoints.Count; j++)
            {
                Gizmos.DrawSphere(tempPoints[j].pos, 0.005f);
            }
        }
        Gizmos.DrawSphere(mousePos, 0.01f);

        List<Color> colors = new List<Color>() { Color.blue, Color.cyan, Color.gray, Color.green, Color.magenta, Color.white };
        if (drawMode == DrawMode.Points)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                for (int j = 0; j < roads[i].points.Count; j++)
                {
                    Gizmos.color = colors[i % colors.Count];

                    if (roads[i].points[j].curve)
                        Gizmos.color = Color.black;

                    Gizmos.DrawSphere(roads[i].points[j].pos, 0.005f);

                    for (int k = 0; k < roads[i].points[j].neighbours.Count; k++)
                    {
                        Gizmos.DrawLine(roads[i].points[j].pos, roads[i].points[j].neighbours[k]);
                    }
                }
            }
            foreach (KeyValuePair<Vector3, List<Roads>> kvp in Roads.intersection)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(kvp.Key, 0.005f);
            }
            for (int k = 0; k < snapPoint.neighbours.Count; k++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(snapPoint.pos, snapPoint.neighbours[k]);
            }
        }
        else if (drawMode == DrawMode.EdgePoints)
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
    }

    public enum Drawmode { None, Points, Curves, Edges };
}
