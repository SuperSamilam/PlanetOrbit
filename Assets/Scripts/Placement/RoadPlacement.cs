using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Security.Cryptography;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class RoadPlacement : MonoBehaviour
{
    [SerializeField] List<Roads> roads = new List<Roads>();
    [SerializeField] List<Roads> curves = new List<Roads>();

    public bool drawLines;
    public bool drawRoadNodecolorCoded;
    public bool withCurves;
    [SerializeField] GameObject roadObj;
    [SerializeField] GameObject roadMaster;

    [SerializeField] LayerMask mask;
    [SerializeField] float distanceBetweenRoadVertices = 0.05f;
    [SerializeField] float snappingDistance = 0.05f;
    [SerializeField] int curveStrenght = 3;
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

    [ContextMenu("curves")]
    List<Roads> editCurves()
    {
        List<Roads> curvedRoads = new List<Roads>();
        Debug.Log(roads.Count);
        for (int i = 0; i < roads.Count; i++)
        {
            List<RoadNode> nodes = new List<RoadNode>();
            for (int j = 0; j < roads[i].points.Count; j++)
            {
                if (roads[i].points[j].neigbours.Count == 2)
                {
                    Vector3 node1 = roads[i].points[j].neigbours[0];
                    Vector3 node2 = roads[i].points[j].neigbours[1];

                    float roadDist = Vector3.Distance(roads[i].points[j].pos, node1) + Vector3.Distance(roads[i].points[j].pos, node2);
                    float shortDist = Vector3.Distance(node1, node2);
                    //Debug.Log(roadDist + " " + shortDist);
                    if (roadDist - 0.01f > shortDist || shortDist > roadDist + 0.01f)
                    {
                        Debug.Log(node1 + " " + node2);
                        for (int s = 0; s <= curveStrenght; s++)
                        {
                            Vector3 lerp1 = Vector3.Lerp(node1, roads[i].points[j].pos, s/(float)curveStrenght);
                            Vector3 lerp2 = Vector3.Lerp(roads[i].points[j].pos, node2, s/(float)curveStrenght);


                            // Debug.Log(s + " " + curveStrenght + " " + s / (float)curveStrenght);
                            // Debug.Log(Vector3.Slerp(node1, node2, s / (float)curveStrenght));
                            nodes.Add(new RoadNode(Vector3.Lerp(lerp1, lerp2, s / (float)curveStrenght)));
                        }
                    }
                    else
                    {
                        nodes.Add(roads[i].points[j]);
                    }
                }
                else
                {
                    nodes.Add(roads[i].points[j]);
                }
            }
            curvedRoads.Add(new Roads(nodes));
        }
        curves = curvedRoads;
        return curvedRoads;
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
        List<Roads> drawing = new List<Roads>();
        if (withCurves && curves != null)
        {
            drawing = curves;
        }
        else
        {
            drawing = roads;
        }


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
