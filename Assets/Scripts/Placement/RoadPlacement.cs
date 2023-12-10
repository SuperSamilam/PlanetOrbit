using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using JetBrains.Annotations;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class RoadPlacement : MonoBehaviour
{
    [SerializeField] List<Roads> roads = new List<Roads>();

    public bool drawPoints = false;
    public bool drawLines;
    public bool drawRoadNodecolorCoded;
    public bool withCurves;
    [SerializeField] GameObject roadObj;
    [SerializeField] GameObject roadMaster;

    [SerializeField] GameObject forwardRoad;
    [SerializeField] GameObject RoadT;
    [SerializeField] GameObject Road4;
    [SerializeField] GameObject curvedRoad;

    [SerializeField] LayerMask mask;
    [SerializeField] float distanceBetweenRoadVertices = 0.05f;
    [SerializeField] float snappingDistance = 0.05f;
    [SerializeField] int curveStrenght = 3;
    [SerializeField] float curveThresholdOfsset = 0.01f;
    [SerializeField] bool draw = false;
    Ray ray;
    RaycastHit hit;
    Vector3 firstPos;
    public bool firstClick = false;

    Dictionary<Roads, List<RoadNode>> clickedNodes = new Dictionary<Roads, List<RoadNode>>();

    Roads road;
    RoadNode roadNode;

    Vector3 mousePos;
    public bool snaping = false;

    void Update()
    {
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
                        else if (firstClick) // does it register?
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
                if (shouldSnap)
                {
                    snaping = true;
                    if (clickedNodes.ContainsKey(road))
                        clickedNodes[road].Add(roadNode);
                    else
                        clickedNodes.Add(road, new List<RoadNode> { roadNode });
                }
                //The list reperesting the points between the 2 points;
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

                    foreach (KeyValuePair<Roads, List<RoadNode>> kvp in clickedNodes)
                    {
                        roads.Remove(kvp.Key);
                    }

                    roads.Add(new Roads(combinedPoints));
                    for (int g = 0; g < roads.Count; g++)
                    {
                        roads[g].curvedpoints = new List<RoadNode>(roads[g].points);
                        roads[g].curvedpoints = UpdateCurve(roads[g].curvedpoints);
                    }
                }
                else
                {
                    roads.Add(new Roads(points));
                }

                firstPos = Vector3.zero;
                firstClick = false;
                snaping = false;
                clickedNodes.Clear();
                road = null;
                roadNode = null;
            }
        }
    }

    List<RoadNode> UpdateCurve(List<RoadNode> points)
    {
        List<RoadNode> extraPoint = new List<RoadNode>();
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
                    //Removes negibours connections to this node
                    RoadNode roadnode1 = new RoadNode(node1);
                    RoadNode roadnode2 = new RoadNode(node2);

                    float dist = float.MaxValue;
                    Vector3 closestPos = Vector3.zero;
                    for (int c = 0; c < extraPoint[extraPoint.Count - 1].neigbours.Count; c++)
                    {
                        if (dist > Vector3.Distance(points[i].pos, extraPoint[extraPoint.Count - 1].neigbours[c]))
                        {
                            dist = Vector3.Distance(points[i].pos, extraPoint[extraPoint.Count - 1].neigbours[c]);
                            closestPos = extraPoint[extraPoint.Count - 1].neigbours[c];
                        }
                    }
                    for (int c = 0; c < extraPoint[extraPoint.Count - 1].neigbours.Count; c++)
                    {
                        if (closestPos != extraPoint[extraPoint.Count - 1].neigbours[c])
                        {
                            roadnode2.neigbours.Add(extraPoint[extraPoint.Count - 1].neigbours[c]);
                        }
                    }

                    dist = float.MaxValue;
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

                    extraPoint[extraPoint.Count - 1] = roadnode2;
                    points[i + 1] = roadnode1;

                    for (int s = 1; s < curveStrenght; s++)
                    {
                        Vector3 lerp1 = Vector3.Lerp(points[i].pos, node1, s / (float)curveStrenght);
                        Vector3 lerp2 = Vector3.Lerp(node2, points[i].pos, s / (float)curveStrenght);
                        if (s == curveStrenght - 1)
                        {
                            extraPoint.Add(new RoadNode(Vector3.Lerp(lerp2, lerp1, s / (float)curveStrenght), new List<Vector3>() { points[i + 1].pos, extraPoint[extraPoint.Count - 1].pos }));
                            extraPoint[extraPoint.Count - 1].curve = true;
                            extraPoint[extraPoint.Count - 2].neigbours.Add(Vector3.Lerp(lerp2, lerp1, s / (float)curveStrenght));
                            points[i + 1].neigbours.Add(Vector3.Lerp(lerp2, lerp1, s / (float)curveStrenght));
                        }
                        else
                        {
                            extraPoint.Add(new RoadNode(Vector3.Lerp(lerp2, lerp1, s / (float)curveStrenght), new List<Vector3>() { extraPoint[extraPoint.Count - 1].pos }));
                            extraPoint[extraPoint.Count - 1].curve = true;
                            extraPoint[extraPoint.Count - 2].neigbours.Add(Vector3.Lerp(lerp2, lerp1, s / (float)curveStrenght));
                        }
                    }
                }
                else
                {
                    extraPoint.Add(points[i]);
                }
            }
            else
            {
                extraPoint.Add(points[i]);
            }
        }
        return extraPoint;
    }

    void DrawRoads(List<Roads> roads)
    {
        for (int i = roadMaster.transform.childCount - 1; i > 0; i--)
        {
            Destroy(roadMaster.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < roads.Count; i++)
        {
            DrawRoad(roads[i].curvedpoints);
        }
    }

    void DrawRoad(List<RoadNode> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            GameObject obj = null;
            Vector3 dir = (nodes[i].neigbours[0] - nodes[i].pos).normalized;
            if (nodes[i].neigbours.Count == 1)
            {
                obj = Instantiate(forwardRoad, nodes[i].pos, quaternion.identity, roadMaster.transform);
            }
            else if (nodes[i].neigbours.Count == 2)
            {
                dir = (nodes[i].neigbours[1] - nodes[i].pos).normalized;
                if (!nodes[i].curve)
                    obj = Instantiate(forwardRoad, nodes[i].pos, quaternion.identity, roadMaster.transform);
                else
                    obj = Instantiate(curvedRoad, nodes[i].pos, quaternion.identity, roadMaster.transform);
            }
            else if (nodes[i].neigbours.Count == 3)
            {
                obj = Instantiate(RoadT, nodes[i].pos, quaternion.identity, roadMaster.transform);
            }
            else if (nodes[i].neigbours.Count == 4)
            {
                obj = Instantiate(Road4, nodes[i].pos, quaternion.identity, roadMaster.transform);
            }
            obj.transform.up = nodes[i].pos;
            obj.transform.rotation = Quaternion.FromToRotation(obj.transform.forward, dir) * obj.transform.rotation;
        }
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

        if (!drawPoints)
            return;

        List<Color> colors = new List<Color>() { Color.blue, Color.black, Color.cyan, Color.gray, Color.green, Color.grey, Color.magenta, Color.white };
        List<Roads> drawing = new List<Roads>();

        drawing = roads;

        if (withCurves)
        {
            colors = new List<Color>() { Color.magenta, Color.black, Color.cyan, Color.gray, Color.green, Color.grey, Color.magenta, Color.white };
            if (!drawRoadNodecolorCoded)
            {
                for (int i = 0; i < drawing.Count; i++)
                {
                    Gizmos.color = colors[i % colors.Count];
                    for (int j = 0; j < drawing[i].curvedpoints.Count; j++)
                    {
                        Gizmos.DrawSphere(drawing[i].curvedpoints[j].pos, 0.01f);
                    }
                }
            }
            else
            {
                for (int i = 0; i < drawing.Count; i++)
                {
                    for (int j = 0; j < drawing[i].curvedpoints.Count; j++)
                    {
                        Gizmos.color = colors[j % colors.Count];
                        Gizmos.DrawSphere(drawing[i].curvedpoints[j].pos, 0.01f);
                    }
                }
            }

            if (drawLines)
            {
                Gizmos.color = Color.black;
                for (int i = 0; i < drawing.Count; i++)
                {
                    for (int j = 0; j < drawing[i].curvedpoints.Count; j++)
                    {
                        for (int k = 0; k < drawing[i].curvedpoints[j].neigbours.Count; k++)
                        {
                            Gizmos.DrawLine(drawing[i].curvedpoints[j].pos, drawing[i].curvedpoints[j].neigbours[k]);
                        }
                    }
                }

                bool foundNode = false;
                for (int i = 0; i < drawing.Count && !foundNode; i++)
                {
                    for (int j = 0; j < drawing[i].curvedpoints.Count; j++)
                    {
                        if (Vector3.Distance(drawing[i].curvedpoints[j].pos, hit.point) < snappingDistance)
                        {
                            RoadNode node = drawing[i].curvedpoints[j];
                            Gizmos.color = Color.yellow;
                            for (int k = 0; k < node.neigbours.Count; k++)
                            {
                                Gizmos.DrawLine(node.pos, node.neigbours[k]);
                            }
                            foundNode = true;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            colors = new List<Color>() { Color.blue, Color.black, Color.cyan, Color.gray, Color.green, Color.grey, Color.magenta, Color.white };
            if (!drawRoadNodecolorCoded)
            {
                for (int i = 0; i < drawing.Count; i++)
                {
                    Gizmos.color = colors[i % colors.Count];
                    for (int j = 0; j < drawing[i].points.Count; j++)
                    {
                        Gizmos.DrawSphere(drawing[i].points[j].pos, 0.01f);
                    }
                }
            }
            else
            {
                for (int i = 0; i < drawing.Count; i++)
                {
                    for (int j = 0; j < drawing[i].points.Count; j++)
                    {
                        Gizmos.color = colors[j % colors.Count];
                        Gizmos.DrawSphere(drawing[i].points[j].pos, 0.01f);
                    }
                }
            }

            if (drawLines)
            {
                Gizmos.color = Color.black;
                for (int i = 0; i < drawing.Count; i++)
                {
                    for (int j = 0; j < drawing[i].points.Count; j++)
                    {
                        for (int k = 0; k < drawing[i].points[j].neigbours.Count; k++)
                        {
                            Gizmos.DrawLine(drawing[i].points[j].pos, drawing[i].points[j].neigbours[k]);
                        }
                    }
                }

                bool foundNode = false;
                for (int i = 0; i < drawing.Count && !foundNode; i++)
                {
                    for (int j = 0; j < drawing[i].points.Count; j++)
                    {
                        if (Vector3.Distance(drawing[i].points[j].pos, hit.point) < snappingDistance)
                        {
                            RoadNode node = drawing[i].points[j];
                            Gizmos.color = Color.yellow;
                            for (int k = 0; k < node.neigbours.Count; k++)
                            {
                                Gizmos.DrawLine(node.pos, node.neigbours[k]);
                            }
                            foundNode = true;
                            break;
                        }
                    }
                }
            }
        }

    }

    void OnValidate()
    {
        if (draw)
        {
            DrawRoads(roads);
        }
    }
}
