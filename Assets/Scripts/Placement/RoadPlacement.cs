using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

public class RoadPlacement : MonoBehaviour
{
    [SerializeField] List<Roads> roads = new List<Roads>();

    public bool drawLines;
    [SerializeField] GameObject roadObj;
    [SerializeField] GameObject roadMaster;

    [SerializeField] LayerMask mask;
    [SerializeField] float distanceBetweenRoadVertices = 0.05f;
    [SerializeField] float snappingDistance = 0.05f;
    Ray ray;
    RaycastHit hit;
    Vector3 firstPos;
    public bool firstClick = false;

    Roads road1;
    RoadNode node1;
    Roads road2;
    RoadNode node2;

    Vector3 mousePos;
    public bool snaping = false;
    public bool shouldSnap = false;

    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
        {
            shouldSnap = false;
            for (int i = 0; i < roads.Count && !shouldSnap; i++)
            {
                for (int j = 0; j < roads[i].points.Count; j++)
                {
                    if (Vector3.Distance(roads[i].points[j].pos, hit.point) < snappingDistance)
                    {
                        if (!firstClick)
                        {
                            road1 = roads[i];
                            node1 = roads[i].points[j];
                        }
                        if (firstClick)
                        {
                            road2 = roads[i];
                            node2 = roads[i].points[j];
                        }

                        mousePos = roads[i].points[j].pos;
                        shouldSnap = true;
                        break;
                    }
                }
            }
            if (!shouldSnap)
                mousePos = hit.point;

            if (Input.GetMouseButtonDown(0) && !firstClick)
            {
                firstClick = true;
                firstPos = mousePos;
                if (shouldSnap)
                {
                    snaping = true;
                }
            }
            else if (Input.GetMouseButtonDown(0) && firstClick)
            {
                firstClick = false;
                List<RoadNode> points = new List<RoadNode>();

                float dot = Vector3.Dot(firstPos, mousePos);
                dot = dot / (firstPos.magnitude * mousePos.magnitude);
                float acos = Mathf.Acos(dot);
                float angle = acos * 180 / Mathf.PI;
                float archLenght = angle / 360 * 2 * Mathf.PI * Vector3.Distance(Vector3.zero, firstPos);

                int numberOfVertices = Mathf.CeilToInt(archLenght / distanceBetweenRoadVertices);

                for (int i = 0; i <= numberOfVertices; i++)
                {
                    RoadNode node = new RoadNode(Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 1.005f);
                    if (i != 0)
                    {
                        node.neigbours.Add(points[i - 1]);
                        points[i - 1].neigbours.Add(node);
                    }
                    points.Add(node);
                }

                if (snaping)
                {
                    List<RoadNode> combinedPoints = new List<RoadNode>();

                    // points[0].neigbours.Add(node1);
                    // points[points.Count - 1].neigbours.Add(node2);
                    // node1.neigbours.Add(points[0]);
                    // node2.neigbours.Add(points[points.Count - 1]);

                    combinedPoints.AddRange(points);
                    combinedPoints.AddRange(road1.points);
                    combinedPoints.AddRange(road2.points);

                    combinedPoints = combinedPoints.Distinct().ToList();

                    roads.Remove(road1);
                    roads.Remove(road2);
                    roads.Add(new Roads(combinedPoints));
                    snaping = false;
                }
                else
                    roads.Add(new Roads(points));

                node1 = null;
                node2 = null;
                road1 = null;
                road2 = null;

                roads.TrimExcess();
                Debug.Log(roads.Count);
                //drawRoads();
            }
        }

        void drawRoads()
        {
            foreach (Transform child in roadMaster.transform)
            {
                Destroy(child.gameObject);
            }
            for (int i = 0; i < roads.Count; i++)
            {
                Debug.Log("print");
                for (int j = 0; j < roads[i].points.Count; j++)
                {
                    Vector3 dir = (roads[i].points[j].neigbours[0].pos - roads[i].points[j].pos).normalized;

                    GameObject road = Instantiate(roadObj, roads[i].points[j].pos * 1.005f, Quaternion.identity, roadMaster.transform);

                    //road.transform.up = roadHit.normal;
                    road.transform.up = roads[i].points[j].pos;
                    road.transform.rotation = Quaternion.FromToRotation(road.transform.forward, dir) * road.transform.rotation;
                }
            }
        }




        // ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
        // {
        //     if (!firstClick && Input.GetMouseButtonDown(0))
        //     {
        //         firstClick = true;
        //         firstPos = hit.point;
        //     }
        //     else if (Input.GetMouseButtonDown(0))
        //     {
        //         List<Vector3> pos1 = new List<Vector3>();
        //         List<Vector3> pos2 = new List<Vector3>();

        //         //Spline 
        //         float dot = Vector3.Dot(firstPos, hit.point);
        //         dot = dot / (firstPos.magnitude * hit.point.magnitude);
        //         float acos = Mathf.Acos(dot);
        //         float angle = acos * 180 / Mathf.PI;
        //         float archLenght = angle / 360 * 2 * Mathf.PI * Vector3.Distance(Vector3.zero, firstPos);
        //         int numberOfVertices = Mathf.CeilToInt(archLenght / distanceBetweenRoadVertices);

        //         for (int i = 0; i <= numberOfVertices; i++)
        //         {
        //             Vector3 mainPoint = Vector3.Slerp(firstPos, hit.point, i / (float)numberOfVertices) * 1.005f;
        //             Vector3 secondPoint = Vector3.Slerp(firstPos, hit.point, (i + 1) / (float)numberOfVertices) * 1.005f;

        //             if (i == numberOfVertices)
        //                 secondPoint = Vector3.Slerp(firstPos, hit.point, (i - 1) / (float)numberOfVertices);

        //             Vector3 dir = (secondPoint - mainPoint).normalized;

        //             Ray roadRay = new Ray(mainPoint * 2, -mainPoint * 0.5f);
        //             RaycastHit roadHit;
        //             if (Physics.Raycast(roadRay, out roadHit, Mathf.Infinity))
        //             {

        //             }

        //         }




        //         //WeirdCode

        //         // float dot = Vector3.Dot(firstPos, hit.point);
        //         // dot = dot / (firstPos.magnitude * hit.point.magnitude);
        //         // float acos = Mathf.Acos(dot);
        //         // float angle = acos * 180 / Mathf.PI;
        //         // float archLenght = angle / 360 * 2 * Mathf.PI * Vector3.Distance(Vector3.zero, firstPos);
        //         // int numberOfVertices = Mathf.CeilToInt(archLenght / distanceBetweenRoadVertices);

        //         // for (int i = 0; i <= numberOfVertices; i++)
        //         // {
        //         //     Vector3 mainPoint = Vector3.Slerp(firstPos, hit.point, i / (float)numberOfVertices);
        //         //     Vector3 secondPoint = Vector3.Slerp(firstPos, hit.point, (i + 1) / (float)numberOfVertices);

        //         //     if (i == numberOfVertices)
        //         //         secondPoint = Vector3.Slerp(firstPos, hit.point, (i - 1) / (float)numberOfVertices);

        //         //     Ray roadRay = new Ray(mainPoint * 2, -mainPoint * 0.5f);
        //         //     RaycastHit roadHit;

        //         //     Vector3 dir = (secondPoint - mainPoint).normalized;

        //         //     if (Physics.Raycast(roadRay, out roadHit, Mathf.Infinity))
        //         //     {
        //         //         GameObject road = Instantiate(roadObj, mainPoint * 1.005f, Quaternion.identity);

        //         //         road.transform.up = roadHit.normal;
        //         //         road.transform.rotation = Quaternion.FromToRotation(road.transform.forward, dir) * road.transform.rotation;




        //         //         list.Add(road);
        //         //     }
        //         // }
        //         roads.Add(new Roads(pos1, pos2));
        //         firstClick = false;
        //         firstPos = Vector3.zero;
        //     }
        //     else
        //     {
        //         // hand.transform.position = hit.point;
        //         // prefab.transform.up = hit.normal;
        //         // prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
        //         // return;
        //     }
        // }
    }


    void OnDrawGizmos()
    {
        List<Color> colors = new List<Color>() { Color.black, Color.blue, Color.cyan, Color.gray, Color.green, Color.grey, Color.magenta, Color.red, Color.white, Color.yellow };
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mousePos, 0.01f);
        for (int i = 0; i < roads.Count; i++)
        {
            for (int j = 0; j < roads[i].points.Count; j++)
            {
                Gizmos.color = colors[i % colors.Count];
                Gizmos.DrawSphere(roads[i].points[j].pos, 0.008f);
                for (int k = 0; k < roads[i].points[j].neigbours.Count; k++)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(roads[i].points[j].pos, roads[i].points[j].neigbours[k].pos);
                }
                Gizmos.DrawLine(roads[i].points[j].pos, roads[i].points[j].pos * 10f);
            }
        }
        Gizmos.color = Color.green;
        for (int i = 0; i < roads.Count; i++)
        {
            for (int j = 0; j < roads[i].points.Count; j++)
            {
                    
            }
        }
    }
}
