using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;


public class RoadPlacement : MonoBehaviour
{
    public Drawmode drawMode;
    public RoadDisplayMode displayMode;

    List<Roads> roads = new List<Roads>();

    [SerializeField] Camera camera;

    [Header("Road Settings")]
    [SerializeField] float distBetweenPoints = 0.03f;
    [SerializeField] float roadWitdh = 0.03f;

    [Header("CurveStrenght")]
    [SerializeField] float curveThres = 0.03f;
    [SerializeField] float MaxCurveThres = 0.03f;
    [SerializeField] int curveStrenght = 3;
    [SerializeField] float roadCurveStrenght = 0.016f;

    [Header("Bridge")]
    [SerializeField] MeshFilter bridgeObject;
    [SerializeField] float maxBridgeLenght = 0.3f;

    [Header("Colliders")]
    [SerializeField] MeshCollider roadCollider;
    [SerializeField] MeshCollider interSectionCollider;
    [SerializeField] MeshCollider bridgeCollider;

    [Header("Filters")]
    [SerializeField] MeshFilter roadMesh;
    [SerializeField] MeshFilter tempMesh;
    [SerializeField] MeshFilter bridgeMesh;
    [SerializeField] MeshFilter intersectionMesh;

    [Header("LayerMasks")]
    [SerializeField] LayerMask countryMask;
    [SerializeField] LayerMask waterMask;
    [SerializeField] LayerMask roadMask;

    [Header("General")]

    [SerializeField] GameManager manager;
    [SerializeField] float snapingDist = 0.03f;
    [SerializeField] GameObject starter;
    [SerializeField] Material canPlaceMat;
    [SerializeField] Material cantPlaceMat;
    [SerializeField] MeshRenderer tempRenderer;

    Vector3 mousePos;
    bool haveClicked = false;

    Vector3 firstPos;
    Roads road1;
    int index1;
    Vector3 key1;
    Roads road2;
    int index2;
    Vector3 key2;

    //snapping
    bool snaping = false;
    bool snappingIntersection1 = false;

    bool canPlace = true;

    //debug
    List<RoadNode> tempPoints = new List<RoadNode>();
    Dictionary<Vector2, List<Vector3>> crossings = new();


    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            resetClick();

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10f, ~roadMask))
        {
            mousePos = hit.point;
            canPlace = true;

            //check for snaping
            bool shouldSnap = false;
            float internalSnapinDist = snapingDist;
            for (int r = 0; r < roads.Count && !shouldSnap; r++)
            {
                for (int p = 0; p < roads[r].points.Count; p++)
                {
                    if (Vector3.Distance(roads[r].points[p].pos, hit.point) < internalSnapinDist)
                    {
                        internalSnapinDist = Vector3.Distance(roads[r].points[p].pos, hit.point);
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
                        shouldSnap = true;
                    }
                }
            }
            bool intersectionSnap = false;
            foreach (KeyValuePair<Vector3, List<Roads>> kvp in Roads.intersection)
            {
                if (Vector3.Distance(kvp.Key, hit.point) < internalSnapinDist)
                {
                    if (!haveClicked)
                    {
                        intersectionSnap = true;
                        key1 = kvp.Key;
                    }
                    else
                    {
                        shouldSnap = true;
                        intersectionSnap = true;
                        key2 = kvp.Key;
                    }

                    mousePos = kvp.Key;
                }
            }

            if (!haveClicked)
            {
                starter.SetActive(true);
                starter.transform.position = mousePos;
            }

            if (Input.GetMouseButtonDown(0) && !haveClicked && hit.transform.gameObject.tag == "Country")
            {
                firstPos = mousePos;
                haveClicked = true;
                if (shouldSnap)
                {
                    if (road1.points[index1].brigde)
                    {
                        Debug.Log("Cant build a road on a bridge");
                        resetClick();
                        return;
                    }
                    snaping = true;
                }
                if (intersectionSnap)
                {
                    snappingIntersection1 = true;
                    snaping = true;
                }
            }
            else if (haveClicked)
            {
                starter.SetActive(false);
                tempPoints.Clear();

                float dot = Vector3.Dot(firstPos, mousePos);
                dot = dot / (firstPos.magnitude * mousePos.magnitude);
                float acos = Mathf.Acos(dot);
                float angle = acos * 180 / Mathf.PI;
                float archLenght = angle / 360 * 2 * Mathf.PI * Vector3.Distance(Vector3.zero, firstPos);
                int numberOfVertices = Mathf.CeilToInt(archLenght / distBetweenPoints);



                bool isBridge = false;
                Vector3 brigeStart = Vector3.zero;

                for (int i = 0; i <= numberOfVertices; i++)
                {
                    Vector3 pos = Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 3f;
                    Ray tempRay = new Ray(pos, -pos);
                    RaycastHit tempHit;

                    if (Physics.Raycast(tempRay, out tempHit, 1000f, ~roadMask))
                    {
                        if (tempHit.transform.gameObject.tag == "Country")
                        {
                            if (Vector3.Distance(Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices), tempHit.point * 1.005f) > 0.005f)
                            {
                                tempPoints.Add(new RoadNode(Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 1.005f, tempHit.transform.gameObject.name));
                                if (isBridge)
                                {
                                    isBridge = false;
                                    if (Vector3.Distance(Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 1.005f, brigeStart) > maxBridgeLenght)
                                    {
                                        canPlace = false;
                                    }
                                }
                            }
                            else
                            {
                                tempPoints.Add(new RoadNode(tempHit.point * 1.005f, tempHit.transform.gameObject.name));

                                if (isBridge)
                                {
                                    isBridge = false;
                                    if (Vector3.Distance(Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 1.005f, brigeStart) > maxBridgeLenght)
                                    {
                                        canPlace = false;
                                    }
                                }
                            }
                        }
                        else if (tempHit.transform.gameObject.tag == "Water")
                        {
                            if (!isBridge)
                            {
                                isBridge = true;
                                brigeStart = Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 1.005f;
                            }
                            else if (Vector3.Distance(Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 1.005f, brigeStart) > maxBridgeLenght)
                            {
                                canPlace = false;
                            }

                            if (i == numberOfVertices)
                                canPlace = false;

                            RoadNode node = new RoadNode(Vector3.Slerp(firstPos, mousePos, i / (float)numberOfVertices) * 1.005f, "Water");
                            node.brigde = true;
                            tempPoints.Add(node);
                        }
                    }
                }
                if (hit.transform.gameObject.tag == "Water")
                    canPlace = false;

                if (canPlace)
                {
                    for (int i = 1; i < tempPoints.Count - 1; i++)
                    {
                        Collider[] colliders = Physics.OverlapBox(tempPoints[i].pos, new Vector3(roadWitdh, roadCurveStrenght, roadWitdh), Quaternion.LookRotation(tempPoints[i + 1].pos, tempPoints[i].pos), ~waterMask);

                        for (int j = 0; j < colliders.Length; j++)
                        {
                            if (colliders[j].gameObject.tag != "Country")
                            {
                                Debug.Log(colliders[j].gameObject.name);
                                canPlace = false;
                                break;
                            }
                        }
                    }
                }


                if (tempPoints.Count > 1)
                {
                    Roads tempRoad = new Roads(tempPoints);
                    BuildTempMesh(tempRoad);
                }

                if (!canPlace)
                {
                    tempRenderer.material = cantPlaceMat;
                    return;
                }
                else
                {
                    tempRenderer.material = canPlaceMat;
                }
                if (Input.GetMouseButtonDown(0))
                {
                    List<RoadNode> combPoints = new List<RoadNode>();
                    //Not snapping to anything
                    if (!snaping && !shouldSnap && !snappingIntersection1 && !intersectionSnap)
                    {
                        roads.Add(new Roads(tempPoints));
                        resetClick();
                    }
                    else if (snaping && shouldSnap)
                    {
                        //if the second is conencted to a intersectionPoint, curve or an intersection
                        if (snappingIntersection1)
                        {
                            //to intersectionpoint, to curve, to intersection
                            if (intersectionSnap)
                            {
                                tempPoints.RemoveAt(0);
                                tempPoints.RemoveAt(tempPoints.Count - 1);

                                Roads r1 = new Roads(tempPoints);
                                Roads.intersection[key1].Add(r1);
                                Roads.intersection[key2].Add(r1);
                                roads.Add(r1);
                                resetClick();
                            }
                            else if (index2 == 0 || index2 == road2.points.Count - 1)
                            {
                                if (road2.loop)
                                {
                                    road2.loop = false;
                                    Vector3 pos = road2.points[index2].pos;
                                    road2.points.RemoveAt(index2);

                                    tempPoints.RemoveAt(0);
                                    tempPoints.RemoveAt(tempPoints.Count - 1);
                                    Roads comb = new Roads(tempPoints);
                                    Roads.intersection[key1].Add(comb);
                                    Roads.intersection.Add(pos, new List<Roads>() { comb, road1 });
                                    roads.Add(comb);
                                    resetClick();
                                    return;
                                }
                                tempPoints.RemoveAt(0);
                                tempPoints[tempPoints.Count - 1].curve = true;
                                road2.points.RemoveAt(index2);

                                if (index2 == 0)
                                {
                                    combPoints.AddRange(tempPoints);
                                    combPoints.AddRange(road2.points);
                                }
                                else
                                {
                                    road2.points.Reverse();
                                    combPoints.AddRange(tempPoints);
                                    combPoints.AddRange(road2.points);
                                }

                                Roads r1 = new Roads(combPoints);
                                Roads.intersection[key1].Add(r1);
                                roads.Add(r1);
                                resetClick();
                            }
                            else
                            {
                                if (road2.loop)
                                {
                                    road2.loop = false;
                                    Vector3 pos1 = road2.points[index2].pos;
                                    road2.points.RemoveAt(index2);

                                    List<Vector3> list1 = new List<Vector3>();
                                    List<Vector3> list2 = new List<Vector3>();
                                    list1.AddRange(road2.points.GetRange(0, index2));
                                    list2.AddRange(road2.points.GetRange(index2, road2.points.Count - index2));

                                    road2.points.Clear();
                                    road2.points.AddRange(list1);
                                    list2.Reverse();
                                    road2.points.AddRange(list2);

                                    tempPoints.RemoveAt(0);
                                    tempPoints.RemoveAt(tempPoints.Count - 1);
                                    Roads comb = new Roads(tempPoints);
                                    Roads.intersection[key1].Add(comb);
                                    Roads.intersection.Add(pos1, new List<Roads>() { comb, road1 });
                                    roads.Add(comb);
                                    resetClick();
                                    return;
                                }
                                tempPoints.RemoveAt(0);
                                tempPoints.RemoveAt(tempPoints.Count - 1);
                                Roads r1 = new Roads(tempPoints);
                                Roads.intersection[key1].Add(r1);

                                Vector3 pos = road2.points[index2].pos;
                                road2.points.RemoveAt(index2);

                                List<RoadNode> r2 = new List<RoadNode>();
                                r2.AddRange(road2.points.GetRange(0, index2));
                                List<RoadNode> r3 = new List<RoadNode>();
                                r3.AddRange(road2.points.GetRange(index2, road2.points.Count - index2));

                                Roads intersetionRoad1 = new Roads(r2);
                                Roads intersetionRoad2 = new Roads(r3);

                                roads.Remove(road2);

                                roads.Add(r1);
                                roads.Add(intersetionRoad1);
                                roads.Add(intersetionRoad2);
                                Roads.intersection.Add(pos, new List<Roads>() { r1, intersetionRoad1, intersetionRoad2 });
                                resetClick();
                            }
                        }
                        else if (index1 == 0 || index1 == road1.points.Count - 1)
                        {
                            if (intersectionSnap)
                            {
                                road1.points.RemoveAt(index1);
                                tempPoints[0].curve = true;
                                tempPoints.RemoveAt(tempPoints.Count - 1);

                                if (index1 == 0)
                                {
                                    tempPoints.Reverse();
                                    combPoints.AddRange(tempPoints);
                                    combPoints.AddRange(road1.points);
                                }
                                else
                                {
                                    tempPoints.Reverse();
                                    road1.points.Reverse();
                                    combPoints.AddRange(tempPoints);
                                    combPoints.AddRange(road1.points);
                                }

                                roads.Remove(road1);
                                Roads r1 = new Roads(combPoints);
                                Roads.intersection[key2].Add(r1);
                                roads.Add(r1);
                                resetClick();
                            }
                            else if (index2 == 0 || index2 == road2.points.Count - 1)
                            {
                                if (road1.loop)
                                {
                                    road1.loop = false;
                                    Vector3 pos = road1.points[index1].pos;
                                    road1.points.RemoveAt(index1);

                                    tempPoints.RemoveAt(0);
                                    tempPoints.RemoveAt(tempPoints.Count - 1);

                                    road2.points[road2.points.Count - 1].curve = true;
                                    combPoints.AddRange(tempPoints);
                                    road2.points.Reverse();
                                    combPoints.AddRange(road2.points);

                                    Roads combRoad = new Roads(combPoints);
                                    roads.Remove(road2);

                                    Roads.intersection.Add(pos, new List<Roads>() { combRoad, road1 });
                                    roads.Add(combRoad);
                                    resetClick();
                                    return;
                                }
                                if (road2.loop)
                                {
                                    Debug.Log("hej2");
                                    road2.loop = false;
                                    Vector3 pos = road2.points[index2].pos;
                                    road2.points.RemoveAt(index2);

                                    tempPoints.RemoveAt(0);
                                    tempPoints.RemoveAt(tempPoints.Count - 1);

                                    road1.points[road1.points.Count - 1].curve = true;
                                    combPoints.AddRange(road1.points);
                                    combPoints.AddRange(tempPoints);

                                    Roads combRoad = new Roads(combPoints);
                                    roads.Remove(road1);

                                    Roads.intersection.Add(pos, new List<Roads>() { combRoad, road2 });
                                    roads.Add(combRoad);
                                    resetClick();
                                    return;
                                }

                                road1.points.RemoveAt(index1);
                                road2.points.RemoveAt(index2);
                                tempPoints[0].curve = true;
                                tempPoints[tempPoints.Count - 1].curve = true;

                                if (road1 == road2)
                                {
                                    combPoints.AddRange(road1.points);
                                    combPoints.AddRange(tempPoints);
                                }
                                else if (index1 == 0)
                                {
                                    if (index2 == 0)
                                    {
                                        //0,0
                                        road1.points.Reverse();
                                        combPoints.AddRange(road1.points);
                                        combPoints.AddRange(tempPoints);
                                        combPoints.AddRange(road2.points);
                                    }
                                    else
                                    {
                                        //0,1
                                        road1.points.Reverse();
                                        road2.points.Reverse();
                                        combPoints.AddRange(road1.points);
                                        combPoints.AddRange(tempPoints);
                                        combPoints.AddRange(road2.points);
                                    }
                                }
                                else
                                {
                                    if (index2 == 0)
                                    {
                                        //1,0
                                        combPoints.AddRange(road1.points);
                                        combPoints.AddRange(tempPoints);
                                        combPoints.AddRange(road2.points);
                                    }
                                    else
                                    {
                                        //1,1
                                        road2.points.Reverse();
                                        combPoints.AddRange(road1.points);
                                        combPoints.AddRange(tempPoints);
                                        combPoints.AddRange(road2.points);
                                    }
                                }

                                roads.Remove(road1);
                                roads.Remove(road2);
                                Roads comb = new Roads(combPoints);
                                roads.Add(comb);

                                if (road1 == road2)
                                    comb.loop = true;
                                resetClick();
                            }
                            else
                            {
                                if (road2.loop)
                                {
                                    road2.loop = false;
                                    Vector3 pos1 = road2.points[index2].pos;
                                    road2.points.RemoveAt(index2);

                                    List<RoadNode> list1 = new List<RoadNode>();
                                    List<RoadNode> list2 = new List<RoadNode>();
                                    list1.AddRange(road2.points.GetRange(0, index2));
                                    list2.AddRange(road2.points.GetRange(index2, road2.points.Count - index2));

                                    road2.points.Clear();
                                    road2.points.AddRange(list1);
                                    list2.Reverse();
                                    road2.points.AddRange(list2);

                                    tempPoints.RemoveAt(0);
                                    tempPoints.RemoveAt(tempPoints.Count - 1);

                                    road1.points[road1.points.Count - 1].curve = true;
                                    combPoints.AddRange(road1.points);
                                    combPoints.AddRange(tempPoints);

                                    Roads comb = new Roads(combPoints);
                                    Roads.intersection.Add(pos1, new List<Roads>() { comb, road1 });
                                    roads.Add(comb);
                                    resetClick();
                                    return;
                                }

                                Vector3 pos = road2.points[index2].pos;
                                //curveRoad
                                road1.points.RemoveAt(index1);
                                tempPoints[0].curve = true;
                                //intersectionRoad
                                road2.points.RemoveAt(index2);
                                tempPoints.RemoveAt(tempPoints.Count - 1);

                                if (index1 == 0)
                                {
                                    road1.points.Reverse();
                                    combPoints.AddRange(road1.points);
                                    combPoints.AddRange(tempPoints);
                                }
                                else
                                {
                                    combPoints.AddRange(road1.points);
                                    combPoints.AddRange(tempPoints);
                                }

                                List<RoadNode> r1 = new List<RoadNode>();
                                r1.AddRange(road2.points.GetRange(0, index2));
                                List<RoadNode> r2 = new List<RoadNode>();
                                r2.AddRange(road2.points.GetRange(index2, road2.points.Count - index2));

                                roads.Remove(road1);
                                roads.Remove(road2);

                                Roads combinedRoad = new Roads(combPoints);
                                Roads intersetionRoad1 = new Roads(r1);
                                Roads intersetionRoad2 = new Roads(r2);

                                roads.Add(combinedRoad);
                                roads.Add(intersetionRoad1);
                                roads.Add(intersetionRoad2);
                                Roads.intersection.Add(pos, new List<Roads>() { combinedRoad, intersetionRoad1, intersetionRoad2 });
                                resetClick();
                            }
                        }
                        else
                        {
                            if (intersectionSnap)
                            {
                                Vector3 pos = road1.points[index1].pos;
                                road1.points.RemoveAt(index1);

                                List<RoadNode> r1 = new List<RoadNode>();
                                r1.AddRange(road1.points.GetRange(0, index1));
                                List<RoadNode> r2 = new List<RoadNode>();
                                r2.AddRange(road1.points.GetRange(index1, road1.points.Count - index1));

                                tempPoints.RemoveAt(0);
                                tempPoints.RemoveAt(tempPoints.Count - 1);
                                Roads tempRoad = new Roads(tempPoints);
                                Roads.intersection[key2].Add(tempRoad);

                                roads.Remove(road1);

                                Roads intersetionRoad1 = new Roads(r1);
                                Roads intersetionRoad2 = new Roads(r2);

                                roads.Add(tempRoad);
                                roads.Add(intersetionRoad1);
                                roads.Add(intersetionRoad2);

                                Roads.intersection.Add(pos, new List<Roads>() { tempRoad, intersetionRoad1, intersetionRoad2 });
                                resetClick();
                            }
                            else if (index2 == 0 || index2 == road2.points.Count - 1)
                            {
                                if (road2.loop)
                                {
                                    road2.loop = false;
                                    Vector3 pos1 = tempPoints[0].pos;
                                    Vector3 pos2 = tempPoints[tempPoints.Count - 1].pos;

                                    tempPoints.RemoveAt(0);
                                    tempPoints.RemoveAt(tempPoints.Count - 1);

                                    road1.points.RemoveAt(index1);

                                    List<RoadNode> list1 = new List<RoadNode>();
                                    list1.AddRange(road1.points.GetRange(0, index1));
                                    List<RoadNode> list2 = new List<RoadNode>();
                                    list2.AddRange(road1.points.GetRange(index1, road1.points.Count - index1));

                                    Roads intersetionRoad11 = new Roads(list1);
                                    Roads intersetionRoad22 = new Roads(list2);
                                    Roads r = new Roads(tempPoints);
                                    Roads.intersection.Add(pos1, new List<Roads>() { r, intersetionRoad11, intersetionRoad22 });

                                    roads.Add(r);
                                    roads.Remove(road1);
                                    roads.Add(intersetionRoad11);
                                    roads.Add(intersetionRoad22);

                                    road2.points.RemoveAt(index2);

                                    Roads.intersection.Add(pos2, new List<Roads>() { r, road2 });
                                    resetClick();
                                    return;
                                }

                                Vector3 pos = road1.points[index1].pos;
                                road1.points.RemoveAt(index1);

                                List<RoadNode> r1 = new List<RoadNode>();
                                r1.AddRange(road1.points.GetRange(0, index1));
                                List<RoadNode> r2 = new List<RoadNode>();
                                r2.AddRange(road1.points.GetRange(index1, road1.points.Count - index1));

                                road2.points.RemoveAt(index2);
                                tempPoints.RemoveAt(0);

                                if (index2 == 0)
                                {
                                    tempPoints[0].curve = true;
                                    tempPoints.Reverse();
                                    road2.points.Reverse();
                                    combPoints.AddRange(road2.points);
                                    combPoints.AddRange(tempPoints);
                                }
                                else
                                {
                                    tempPoints[tempPoints.Count - 1].curve = true;
                                    tempPoints.Reverse();
                                    combPoints.AddRange(road2.points);
                                    combPoints.AddRange(tempPoints);
                                }

                                roads.Remove(road1);
                                roads.Remove(road2);

                                Roads combinedRoad = new Roads(combPoints);
                                Roads intersetionRoad1 = new Roads(r1);
                                Roads intersetionRoad2 = new Roads(r2);

                                roads.Add(combinedRoad);
                                roads.Add(intersetionRoad1);
                                roads.Add(intersetionRoad2);
                                Roads.intersection.Add(pos, new List<Roads>() { combinedRoad, intersetionRoad1, intersetionRoad2 });
                                resetClick();
                            }
                            else
                            {
                                if (road2.loop)
                                {
                                    road2.loop = false;
                                    Vector3 pos11 = tempPoints[0].pos;
                                    Vector3 pos22 = tempPoints[tempPoints.Count - 1].pos;

                                    tempPoints.RemoveAt(0);
                                    tempPoints.RemoveAt(tempPoints.Count - 1);
                                    road1.points.RemoveAt(index1);
                                    road2.points.RemoveAt(index2);

                                    List<RoadNode> list1 = new List<RoadNode>();
                                    list1.AddRange(road1.points.GetRange(0, index1));
                                    List<RoadNode> list2 = new List<RoadNode>();
                                    list2.AddRange(road1.points.GetRange(index1, road1.points.Count - index1));

                                    Roads intersetionRoad11 = new Roads(list1);
                                    Roads intersetionRoad22 = new Roads(list2);
                                    Roads r = new Roads(tempPoints);
                                    Roads.intersection.Add(pos11, new List<Roads>() { r, intersetionRoad11, intersetionRoad22 });

                                    roads.Add(r);
                                    roads.Remove(road1);
                                    roads.Add(intersetionRoad11);
                                    roads.Add(intersetionRoad22);

                                    list1 = new List<RoadNode>();
                                    list2 = new List<RoadNode>();
                                    list1.AddRange(road2.points.GetRange(0, index2));
                                    list2.AddRange(road2.points.GetRange(index2, road2.points.Count - index2));

                                    road2.points.Clear();
                                    list1.Reverse();
                                    road2.points.AddRange(list1);
                                    list2.Reverse();
                                    road2.points.AddRange(list2);

                                    Roads.intersection.Add(pos22, new List<Roads>() { r, road2 });
                                    resetClick();
                                    return;
                                }
                                if (road1.loop)
                                {
                                    road1.loop = false;
                                    Vector3 pos11 = tempPoints[0].pos;
                                    Vector3 pos22 = tempPoints[tempPoints.Count - 1].pos;

                                    tempPoints.RemoveAt(0);
                                    tempPoints.RemoveAt(tempPoints.Count - 1);
                                    road1.points.RemoveAt(index1);
                                    road2.points.RemoveAt(index2);

                                    List<RoadNode> list1 = new List<RoadNode>();
                                    list1.AddRange(road2.points.GetRange(0, index2));
                                    List<RoadNode> list2 = new List<RoadNode>();
                                    list2.AddRange(road2.points.GetRange(index2, road2.points.Count - index2));

                                    Roads intersetionRoad11 = new Roads(list1);
                                    Roads intersetionRoad22 = new Roads(list2);
                                    Roads r = new Roads(tempPoints);
                                    Roads.intersection.Add(pos11, new List<Roads>() { r, intersetionRoad11, intersetionRoad22 });

                                    roads.Add(r);
                                    roads.Remove(road2);
                                    roads.Add(intersetionRoad11);
                                    roads.Add(intersetionRoad22);

                                    list1 = new List<RoadNode>();
                                    list2 = new List<RoadNode>();
                                    list1.AddRange(road1.points.GetRange(0, index1));
                                    list2.AddRange(road1.points.GetRange(index1, road1.points.Count - index1));

                                    road1.points.Clear();
                                    list1.Reverse();
                                    road1.points.AddRange(list1);
                                    list2.Reverse();
                                    road1.points.AddRange(list2);

                                    Roads.intersection.Add(pos22, new List<Roads>() { r, road1 });
                                    resetClick();
                                    return;
                                }
                                Vector3 pos1 = road1.points[index1].pos;
                                road1.points.RemoveAt(index1);

                                List<RoadNode> r1 = new List<RoadNode>();
                                r1.AddRange(road1.points.GetRange(0, index1));
                                List<RoadNode> r2 = new List<RoadNode>();
                                r2.AddRange(road1.points.GetRange(index1, road1.points.Count - index1));

                                Vector3 pos2 = road2.points[index2].pos;
                                road2.points.RemoveAt(index2);

                                List<RoadNode> r3 = new List<RoadNode>();
                                r3.AddRange(road2.points.GetRange(0, index2));
                                List<RoadNode> r4 = new List<RoadNode>();
                                r4.AddRange(road2.points.GetRange(index2, road2.points.Count - index2));

                                tempPoints.RemoveAt(0);
                                tempPoints.RemoveAt(tempPoints.Count - 1);

                                roads.Remove(road1);
                                roads.Remove(road2);

                                Roads comb = new Roads(tempPoints);
                                Roads intersetionRoad1 = new Roads(r1);
                                Roads intersetionRoad2 = new Roads(r2);
                                Roads intersetionRoad3 = new Roads(r3);
                                Roads intersetionRoad4 = new Roads(r4);

                                roads.Add(comb);

                                roads.Add(intersetionRoad1);
                                roads.Add(intersetionRoad2);
                                Roads.intersection.Add(pos1, new List<Roads>() { comb, intersetionRoad1, intersetionRoad2 });

                                roads.Add(intersetionRoad3);
                                roads.Add(intersetionRoad4);
                                Roads.intersection.Add(pos2, new List<Roads>() { comb, intersetionRoad3, intersetionRoad4 });

                                resetClick();
                            }
                        }
                    }
                    else if (snaping)
                    {
                        if (snappingIntersection1)
                        {
                            tempPoints.RemoveAt(0);
                            Roads r1 = new Roads(tempPoints);
                            Roads.intersection[key1].Add(r1);
                            roads.Add(r1);
                            resetClick();
                        }
                        else if (index1 == 0 || index1 == road1.points.Count - 1)
                        {
                            if (road1.loop)
                            {
                                road1.loop = false;

                                Vector3 pos = tempPoints[0].pos;
                                tempPoints.RemoveAt(0);
                                Roads r = new Roads(tempPoints);
                                roads.Add(r);

                                road1.points.RemoveAt(index1);
                                List<RoadNode> list1 = new List<RoadNode>();
                                List<RoadNode> list2 = new List<RoadNode>();
                                list1.AddRange(road1.points.GetRange(0, index1));
                                list2.AddRange(road1.points.GetRange(index1, road1.points.Count - index1));

                                road1.points = new List<RoadNode>();
                                road1.points.AddRange(list1);
                                road1.points.AddRange(list2);
                                Roads.intersection.Add(pos, new List<Roads>() { r, road1 });

                                resetClick();
                                return;
                            }
                            road1.points.RemoveAt(index1);
                            tempPoints[0].curve = true;

                            if (index1 == 0)
                            {
                                road1.points.Reverse();
                                combPoints.AddRange(road1.points);
                                combPoints.AddRange(tempPoints);
                            }
                            else
                            {
                                combPoints.AddRange(road1.points);
                                combPoints.AddRange(tempPoints);
                            }
                            Roads comb = new Roads(combPoints);
                            roads.Remove(road1);
                            roads.Add(comb);
                            resetClick();
                        }
                        else
                        {
                            if (road1.loop)
                            {
                                road1.loop = false;

                                Vector3 pos1 = road1.points[index1].pos;
                                tempPoints.RemoveAt(0);
                                Roads r = new Roads(tempPoints);
                                roads.Add(r);

                                road1.points.RemoveAt(index1);

                                List<RoadNode> list1 = new List<RoadNode>();
                                List<RoadNode> list2 = new List<RoadNode>();
                                list1.AddRange(road1.points.GetRange(0, index1));
                                list2.AddRange(road1.points.GetRange(index1, road1.points.Count - index1));
                                road1.points = new List<RoadNode>();

                                list1.Reverse();
                                list2.Reverse();

                                road1.points.AddRange(list1);
                                road1.points.AddRange(list2);
                                Roads.intersection.Add(pos1, new List<Roads>() { r, road1 });

                                resetClick();
                                return;
                            }
                            Vector3 pos = road1.points[index1].pos;
                            road1.points.RemoveAt(index1);
                            tempPoints.RemoveAt(0);

                            List<RoadNode> r1 = new List<RoadNode>();
                            r1.AddRange(road1.points.GetRange(0, index1));
                            List<RoadNode> r2 = new List<RoadNode>();
                            r2.AddRange(road1.points.GetRange(index1, road1.points.Count - index1));

                            roads.Remove(road1);

                            Roads comb = new Roads(tempPoints);
                            Roads intersetionRoad1 = new Roads(r1);
                            Roads intersetionRoad2 = new Roads(r2);

                            roads.Add(comb);
                            roads.Add(intersetionRoad1);
                            roads.Add(intersetionRoad2);
                            Roads.intersection.Add(pos, new List<Roads>() { comb, intersetionRoad1, intersetionRoad2 });
                            resetClick();
                        }
                    }
                    else if (shouldSnap)
                    {
                        if (intersectionSnap)
                        {
                            tempPoints.RemoveAt(tempPoints.Count - 1);
                            Roads r1 = new Roads(tempPoints);
                            Roads.intersection[key2].Add(r1);
                            roads.Add(r1);
                            resetClick();
                        }
                        else if (index2 == 0 || index2 == road2.points.Count - 1)
                        {
                            if (road2.loop)
                            {
                                road2.loop = false;

                                Vector3 pos = tempPoints[tempPoints.Count - 1].pos;
                                tempPoints.RemoveAt(tempPoints.Count - 1);
                                Roads r = new Roads(tempPoints);
                                roads.Add(r);

                                road2.points.RemoveAt(index2);
                                List<RoadNode> list1 = new List<RoadNode>();
                                List<RoadNode> list2 = new List<RoadNode>();
                                list1.AddRange(road2.points.GetRange(0, index2));
                                list2.AddRange(road2.points.GetRange(index2, road2.points.Count - index2));

                                road2.points = new List<RoadNode>();
                                road2.points.AddRange(list1);
                                road2.points.AddRange(list2);
                                Roads.intersection.Add(pos, new List<Roads>() { r, road2 });

                                resetClick();
                                return;
                            }
                            road2.points.RemoveAt(index2);
                            tempPoints[tempPoints.Count - 1].curve = true;

                            if (index2 == 0)
                            {
                                road2.points.Reverse();
                                tempPoints.Reverse();
                                combPoints.AddRange(road2.points);
                                combPoints.AddRange(tempPoints);
                            }
                            else
                            {
                                tempPoints.Reverse();
                                combPoints.AddRange(road2.points);
                                combPoints.AddRange(tempPoints);
                            }
                            Roads comb = new Roads(combPoints);
                            roads.Remove(road2);
                            roads.Add(comb);
                            resetClick();
                        }
                        else
                        {
                            if (road2.loop)
                            {
                                road2.loop = false;

                                Vector3 pos1 = road2.points[index2].pos;
                                tempPoints.RemoveAt(tempPoints.Count - 1);
                                Roads r = new Roads(tempPoints);
                                roads.Add(r);

                                road2.points.RemoveAt(index2);

                                List<RoadNode> list1 = new List<RoadNode>();
                                List<RoadNode> list2 = new List<RoadNode>();
                                list1.AddRange(road2.points.GetRange(0, index2));
                                list2.AddRange(road2.points.GetRange(index2, road2.points.Count - index2));
                                road2.points = new List<RoadNode>();

                                list1.Reverse();
                                list2.Reverse();

                                road2.points.AddRange(list1);
                                road2.points.AddRange(list2);
                                Roads.intersection.Add(pos1, new List<Roads>() { r, road2 });

                                resetClick();
                                return;
                            }

                            Vector3 pos = road2.points[index2].pos;
                            road2.points.RemoveAt(index2);
                            tempPoints.RemoveAt(tempPoints.Count - 1);

                            List<RoadNode> r1 = new List<RoadNode>();
                            r1.AddRange(road2.points.GetRange(0, index2));
                            List<RoadNode> r2 = new List<RoadNode>();
                            r2.AddRange(road2.points.GetRange(index2, road2.points.Count - index2));

                            roads.Remove(road2);

                            Roads comb = new Roads(tempPoints);
                            Roads intersetionRoad1 = new Roads(r1);
                            Roads intersetionRoad2 = new Roads(r2);

                            roads.Add(comb);
                            roads.Add(intersetionRoad1);
                            roads.Add(intersetionRoad2);
                            Roads.intersection.Add(pos, new List<Roads>() { comb, intersetionRoad1, intersetionRoad2 });
                            resetClick();
                        }
                    }
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
        snappingIntersection1 = false;
        road1 = null;
        road2 = null;
        index1 = -1;
        index2 = -1;
        tempMesh.mesh = null;
        GetRoadEdges();
        if (displayMode == RoadDisplayMode.AllRoads)
            BuildMesh();
        else
        {
            BuildMesh(manager.currentPlayer.countys);
        }
        BuildBridges();
    }
    void GetRoadEdges()
    {
        for (int r = 0; r < roads.Count; r++)
        {
            roads[r].list1.Clear();
            roads[r].list2.Clear();

            if (roads[r].points.Count == 1)
            {
                continue;
            }
            for (int p = 0; p < roads[r].points.Count; p++)
            {
                Vector3 right;

                if (p != 0 && p + 1 != roads[r].points.Count)
                {
                    float dist = Vector3.Distance(roads[r].points[p - 1].pos, roads[r].points[p + 1].pos);
                    if (roads[r].points[p].curve)
                    {
                        for (int c = 1; c < curveStrenght; c++)
                        {
                            Vector3 lerp1 = Vector3.Lerp(roads[r].points[p - 1].pos, roads[r].points[p].pos, c / (float)curveStrenght);
                            Vector3 lerp2 = Vector3.Lerp(roads[r].points[p].pos, roads[r].points[p + 1].pos, c / (float)curveStrenght);
                            Vector3 pos = Vector3.Lerp(lerp1, lerp2, c / (float)curveStrenght);
                            right = Vector3.Cross(roads[r].points[p - 1].pos, roads[r].points[p + 1].pos).normalized;
                            Vector3 pos1 = pos + (right * roadWitdh);
                            Vector3 pos2 = pos + (-right * roadWitdh);
                            roads[r].list1.Add(new RoadNode(pos1, roads[r].points[p].country));
                            roads[r].list2.Add(new RoadNode(pos2, roads[r].points[p].country));
                        }

                        continue;
                    }

                    right = Vector3.Cross(roads[r].points[p - 1].pos, roads[r].points[p + 1].pos).normalized;
                    Vector3 p1 = roads[r].points[p].pos + (right * roadWitdh);
                    Vector3 p2 = roads[r].points[p].pos + (-right * roadWitdh);

                    roads[r].list1.Add(new RoadNode(p1, roads[r].points[p].country));
                    roads[r].list2.Add(new RoadNode(p2, roads[r].points[p].country));
                }
                else if (p == 0)
                {
                    right = Vector3.Cross(roads[r].points[p].pos, roads[r].points[p + 1].pos).normalized;
                    Vector3 p1 = roads[r].points[p].pos + (right * roadWitdh);
                    Vector3 p2 = roads[r].points[p].pos + (-right * roadWitdh);

                    roads[r].list1.Add(new RoadNode(p1, roads[r].points[p].country));
                    roads[r].list2.Add(new RoadNode(p2, roads[r].points[p].country));
                }
                else
                {
                    right = Vector3.Cross(roads[r].points[p].pos, roads[r].points[p - 1].pos).normalized;
                    Vector3 p1 = roads[r].points[p].pos + (right * roadWitdh);
                    Vector3 p2 = roads[r].points[p].pos + (-right * roadWitdh);

                    roads[r].list1.Add(new RoadNode(p2, roads[r].points[p].country));
                    roads[r].list2.Add(new RoadNode(p1, roads[r].points[p].country));
                }
            }
        }
    }
    void BuildMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        int offset = 0;
        int extraOffset = 0;

        int totalCount = 0;
        for (int r = 0; r < roads.Count; r++)
        {
            for (int p = 1; p < roads[r].list1.Count; p++)
            {
                Vector3 p1 = roads[r].list1[p - 1].pos;
                Vector3 p2 = roads[r].list2[p - 1].pos;
                Vector3 p3 = roads[r].list1[p].pos;
                Vector3 p4 = roads[r].list2[p].pos;

                offset = 4 * totalCount + extraOffset;

                int t1 = offset + 0;
                int t2 = offset + 1;
                int t3 = offset + 2;

                int t4 = offset + 3;
                int t5 = offset + 2;
                int t6 = offset + 1;

                verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
                tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });

                uvs.AddRange(new List<Vector2> { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) });
                totalCount++;
            }
            if (roads[r].loop)
            {
                Vector3 p1 = roads[r].list1[roads[r].list1.Count - 1].pos;
                Vector3 p2 = roads[r].list2[roads[r].list2.Count - 1].pos;
                Vector3 p3 = roads[r].list1[0].pos;
                Vector3 p4 = roads[r].list2[0].pos;

                offset = 4 * totalCount + extraOffset;

                int t1 = offset + 0;
                int t2 = offset + 1;
                int t3 = offset + 2;

                int t4 = offset + 3;
                int t5 = offset + 2;
                int t6 = offset + 1;
                verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
                tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });
                uvs.AddRange(new List<Vector2> { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) });
                extraOffset += 4;

            }
        }
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        roadMesh.mesh = mesh;
        roadCollider.sharedMesh = roadMesh.mesh;


        verts = new List<Vector3>();
        tris = new List<int>();
        uvs = new List<Vector2>();
        mesh = new Mesh();
        crossings.Clear();

        foreach (KeyValuePair<Vector3, List<Roads>> kvp in Roads.intersection)
        {
            List<(Vector3, Vector2)> pointIndex = new();
            List<(float, Vector3, int)> anglePosIndex = new();

            for (int i = 0; i < kvp.Value.Count; i++)
            {
                if (Vector3.Distance(kvp.Value[i].points[0].pos, kvp.Key) > Vector3.Distance(kvp.Value[i].points[kvp.Value[i].points.Count - 1].pos, kvp.Key))
                {
                    pointIndex.Add((kvp.Value[i].list1[kvp.Value[i].list1.Count - 1].pos, new Vector2(i, kvp.Value[i].list1.Count - 1)));
                    pointIndex.Add((kvp.Value[i].list2[kvp.Value[i].list2.Count - 1].pos, new Vector2(i, kvp.Value[i].list1.Count - 1)));
                }
                else
                {
                    pointIndex.Add((kvp.Value[i].list1[0].pos, new Vector2(i, 0)));
                    pointIndex.Add((kvp.Value[i].list2[0].pos, new Vector2(i, 0)));
                }
            }

            Dictionary<Vector2, List<Vector3>> crossings = new Dictionary<Vector2, List<Vector3>>();
            for (int i = 0; i < pointIndex.Count; i += 2)
            {
                List<Vector3> roadCrossings = new List<Vector3>();
                if (pointIndex[i].Item2.y == 0)
                {
                    roadCrossings.Add(Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list1[(int)pointIndex[i].Item2.y + 1].pos, pointIndex[i].Item1, 1.5f));
                    roadCrossings.Add(Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list2[(int)pointIndex[i].Item2.y + 1].pos, pointIndex[i + 1].Item1, 1.5f));
                    roadCrossings.Add(pointIndex[i + 1].Item1);
                    roadCrossings.Add(pointIndex[i].Item1);
                    anglePosIndex.Add((0, Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list1[(int)pointIndex[i].Item2.y + 1].pos, pointIndex[i].Item1, 1.5f), (int)pointIndex[i].Item2.x));
                    anglePosIndex.Add((0, Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list2[(int)pointIndex[i].Item2.y + 1].pos, pointIndex[i + 1].Item1, 1.5f), (int)pointIndex[i].Item2.x));
                }
                else
                {
                    roadCrossings.Add(pointIndex[i].Item1);
                    roadCrossings.Add(pointIndex[i + 1].Item1);
                    roadCrossings.Add(Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list2[(int)pointIndex[i].Item2.y - 1].pos, pointIndex[i + 1].Item1, 1.5f));
                    roadCrossings.Add(Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list1[(int)pointIndex[i].Item2.y - 1].pos, pointIndex[i].Item1, 1.5f));
                    anglePosIndex.Add((0, Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list2[(int)pointIndex[i].Item2.y - 1].pos, pointIndex[i + 1].Item1, 1.5f), (int)pointIndex[i].Item2.x));
                    anglePosIndex.Add((0, Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list1[(int)pointIndex[i].Item2.y - 1].pos, pointIndex[i].Item1, 1.5f), (int)pointIndex[i].Item2.x));
                }
                crossings.Add(pointIndex[i].Item2, roadCrossings);
            }

            foreach (KeyValuePair<Vector2, List<Vector3>> crossing in crossings)
            {
                int vertOffset = verts.Count;
                verts.AddRange(crossing.Value);
                tris.AddRange(new List<int>() { 0 + vertOffset, 1 + vertOffset, 2 + vertOffset, 0 + vertOffset, 2 + vertOffset, 3 + vertOffset });
                uvs.AddRange(new List<Vector2>() { new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f) });
            }
            //new Vector2(0f,0f),new Vector2(0.5f,0f),new Vector2(0.5f,1f),new Vector2(0f,1f)});
            Vector3 center = Vector3.zero;
            for (int i = 0; i < anglePosIndex.Count; i++)
            {
                center += anglePosIndex[i].Item2;
            }
            center = center / anglePosIndex.Count;

            //Sorting the points
            for (int i = 0; i < anglePosIndex.Count; i++)
            {
                Vector3 dir = anglePosIndex[i].Item2 - center;
                float angle = Mathf.Atan2(dir.y, dir.x);
                anglePosIndex[i] = (angle, anglePosIndex[i].Item2, anglePosIndex[i].Item3);
            }
            anglePosIndex.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            int pointsOffsets = verts.Count;
            for (int j = 1; j <= anglePosIndex.Count; j++)
            {
                verts.Add(center);
                verts.Add(anglePosIndex[j - 1].Item2);
                if (j == anglePosIndex.Count)
                    verts.Add(anglePosIndex[0].Item2);
                else
                    verts.Add(anglePosIndex[j].Item2);

                tris.Add(pointsOffsets + ((j - 1) * 3) + 2);
                tris.Add(pointsOffsets + ((j - 1) * 3) + 1);
                tris.Add(pointsOffsets + ((j - 1) * 3) + 0);
                uvs.AddRange(new List<Vector2>() { new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0) });
            }
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        intersectionMesh.mesh = mesh;
        interSectionCollider.sharedMesh = mesh;
    }

    public void BuildMesh(List<County> counties)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        int offset = 0;
        int extraOffset = 0;

        int totalCount = 0;
        for (int r = 0; r < roads.Count; r++)
        {
            for (int p = 1; p < roads[r].list1.Count; p++)
            {
                bool found = false;
                for (int i = 0; i < counties.Count; i++)
                {
                    if (counties[i].name == roads[r].list2[p - 1].country)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found && roads[r].list2[p - 1].country != "Water")
                    continue;

                Vector3 p1 = roads[r].list1[p - 1].pos;
                Vector3 p2 = roads[r].list2[p - 1].pos;
                Vector3 p3 = roads[r].list1[p].pos;
                Vector3 p4 = roads[r].list2[p].pos;

                offset = 4 * totalCount + extraOffset;

                int t1 = offset + 0;
                int t2 = offset + 1;
                int t3 = offset + 2;

                int t4 = offset + 3;
                int t5 = offset + 2;
                int t6 = offset + 1;

                verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
                tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });

                uvs.AddRange(new List<Vector2> { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) });
                totalCount++;
            }
            if (roads[r].loop)
            {
                Vector3 p1 = roads[r].list1[roads[r].list1.Count - 1].pos;
                Vector3 p2 = roads[r].list2[roads[r].list2.Count - 1].pos;
                Vector3 p3 = roads[r].list1[0].pos;
                Vector3 p4 = roads[r].list2[0].pos;

                offset = 4 * totalCount + extraOffset;

                int t1 = offset + 0;
                int t2 = offset + 1;
                int t3 = offset + 2;

                int t4 = offset + 3;
                int t5 = offset + 2;
                int t6 = offset + 1;
                verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
                tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });
                uvs.AddRange(new List<Vector2> { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) });
                extraOffset += 4;

            }
        }
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        roadMesh.mesh = mesh;
        roadCollider.sharedMesh = roadMesh.mesh;


        verts = new List<Vector3>();
        tris = new List<int>();
        uvs = new List<Vector2>();
        mesh = new Mesh();
        crossings.Clear();

        foreach (KeyValuePair<Vector3, List<Roads>> kvp in Roads.intersection)
        {
            List<(Vector3, Vector2)> pointIndex = new();
            List<(float, Vector3, int)> anglePosIndex = new();

            for (int i = 0; i < kvp.Value.Count; i++)
            {
                if (Vector3.Distance(kvp.Value[i].points[0].pos, kvp.Key) > Vector3.Distance(kvp.Value[i].points[kvp.Value[i].points.Count - 1].pos, kvp.Key))
                {
                    pointIndex.Add((kvp.Value[i].list1[kvp.Value[i].list1.Count - 1].pos, new Vector2(i, kvp.Value[i].list1.Count - 1)));
                    pointIndex.Add((kvp.Value[i].list2[kvp.Value[i].list2.Count - 1].pos, new Vector2(i, kvp.Value[i].list1.Count - 1)));
                }
                else
                {
                    pointIndex.Add((kvp.Value[i].list1[0].pos, new Vector2(i, 0)));
                    pointIndex.Add((kvp.Value[i].list2[0].pos, new Vector2(i, 0)));
                }
            }

            Dictionary<Vector2, List<Vector3>> crossings = new Dictionary<Vector2, List<Vector3>>();
            for (int i = 0; i < pointIndex.Count; i += 2)
            {
                List<Vector3> roadCrossings = new List<Vector3>();
                if (pointIndex[i].Item2.y == 0)
                {
                    roadCrossings.Add(Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list1[(int)pointIndex[i].Item2.y + 1].pos, pointIndex[i].Item1, 1.5f));
                    roadCrossings.Add(Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list2[(int)pointIndex[i].Item2.y + 1].pos, pointIndex[i + 1].Item1, 1.5f));
                    roadCrossings.Add(pointIndex[i + 1].Item1);
                    roadCrossings.Add(pointIndex[i].Item1);
                    anglePosIndex.Add((0, Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list1[(int)pointIndex[i].Item2.y + 1].pos, pointIndex[i].Item1, 1.5f), (int)pointIndex[i].Item2.x));
                    anglePosIndex.Add((0, Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list2[(int)pointIndex[i].Item2.y + 1].pos, pointIndex[i + 1].Item1, 1.5f), (int)pointIndex[i].Item2.x));
                }
                else
                {
                    roadCrossings.Add(pointIndex[i].Item1);
                    roadCrossings.Add(pointIndex[i + 1].Item1);
                    roadCrossings.Add(Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list2[(int)pointIndex[i].Item2.y - 1].pos, pointIndex[i + 1].Item1, 1.5f));
                    roadCrossings.Add(Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list1[(int)pointIndex[i].Item2.y - 1].pos, pointIndex[i].Item1, 1.5f));
                    anglePosIndex.Add((0, Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list2[(int)pointIndex[i].Item2.y - 1].pos, pointIndex[i + 1].Item1, 1.5f), (int)pointIndex[i].Item2.x));
                    anglePosIndex.Add((0, Vector3.LerpUnclamped(kvp.Value[(int)pointIndex[i].Item2.x].list1[(int)pointIndex[i].Item2.y - 1].pos, pointIndex[i].Item1, 1.5f), (int)pointIndex[i].Item2.x));
                }
                crossings.Add(pointIndex[i].Item2, roadCrossings);
            }

            foreach (KeyValuePair<Vector2, List<Vector3>> crossing in crossings)
            {
                int vertOffset = verts.Count;
                verts.AddRange(crossing.Value);
                tris.AddRange(new List<int>() { 0 + vertOffset, 1 + vertOffset, 2 + vertOffset, 0 + vertOffset, 2 + vertOffset, 3 + vertOffset });
                uvs.AddRange(new List<Vector2>() { new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f) });
            }
            //new Vector2(0f,0f),new Vector2(0.5f,0f),new Vector2(0.5f,1f),new Vector2(0f,1f)});
            Vector3 center = Vector3.zero;
            for (int i = 0; i < anglePosIndex.Count; i++)
            {
                center += anglePosIndex[i].Item2;
            }
            center = center / anglePosIndex.Count;

            //Sorting the points
            for (int i = 0; i < anglePosIndex.Count; i++)
            {
                Vector3 dir = anglePosIndex[i].Item2 - center;
                float angle = Mathf.Atan2(dir.y, dir.x);
                anglePosIndex[i] = (angle, anglePosIndex[i].Item2, anglePosIndex[i].Item3);
            }
            anglePosIndex.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            int pointsOffsets = verts.Count;
            for (int j = 1; j <= anglePosIndex.Count; j++)
            {
                verts.Add(center);
                verts.Add(anglePosIndex[j - 1].Item2);
                if (j == anglePosIndex.Count)
                    verts.Add(anglePosIndex[0].Item2);
                else
                    verts.Add(anglePosIndex[j].Item2);

                tris.Add(pointsOffsets + ((j - 1) * 3) + 2);
                tris.Add(pointsOffsets + ((j - 1) * 3) + 1);
                tris.Add(pointsOffsets + ((j - 1) * 3) + 0);
                uvs.AddRange(new List<Vector2>() { new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0) });
            }
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        intersectionMesh.mesh = mesh;
        interSectionCollider.sharedMesh = mesh;
    }
    void BuildTempMesh(Roads road)
    {
        for (int p = 0; p < road.points.Count; p++)
        {
            Vector3 right;
            if (p != road.points.Count - 1)
            {
                right = Vector3.Cross(road.points[p].pos, road.points[p + 1].pos).normalized;
                Vector3 p1 = road.points[p].pos + (right * roadWitdh);
                Vector3 p2 = road.points[p].pos + (-right * roadWitdh);

                road.list1.Add(new RoadNode(p1, road.points[p].country));
                road.list2.Add(new RoadNode(p2, road.points[p].country));
            }
            else
            {
                right = Vector3.Cross(road.points[p].pos, road.points[p - 1].pos).normalized;
                Vector3 p1 = road.points[p].pos + (right * roadWitdh);
                Vector3 p2 = road.points[p].pos + (-right * roadWitdh);

                road.list1.Add(new RoadNode(p2, road.points[p].country));
                road.list2.Add(new RoadNode(p1, road.points[p].country));
            }
        }

        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        int offset = 0;

        for (int p = 1; p < road.list1.Count; p++)
        {
            Vector3 p1 = road.list1[p - 1].pos;
            Vector3 p2 = road.list2[p - 1].pos;
            Vector3 p3 = road.list1[p].pos;
            Vector3 p4 = road.list2[p].pos;

            offset = 4 * (p - 1);

            int t1 = offset + 0;
            int t2 = offset + 1;
            int t3 = offset + 2;

            int t4 = offset + 3;
            int t5 = offset + 2;
            int t6 = offset + 1;

            verts.AddRange(new List<Vector3> { p1, p2, p3, p4 });
            tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });
            uvs.AddRange(new List<Vector2> { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) });
        }
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        tempMesh.mesh = mesh;
    }
    public void BuildBridges()
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        int offset = 0;

        for (int r = 0; r < roads.Count; r++)
        {
            for (int c = 0; c < roads[r].points.Count; c++)
            {
                if (roads[r].points[c].brigde)
                {
                    offset = verts.Count;
                    Quaternion bridgeRotation = Quaternion.LookRotation(roads[r].points[c].pos, roads[r].points[c + 1].pos);
                    Quaternion bridgeRotation1 = Quaternion.LookRotation(roads[r].points[c + 1].pos);
                    Quaternion combinedRotation = new Quaternion(0, bridgeRotation.y, bridgeRotation1.z, bridgeRotation.w);

                    //bridgeObject.transform.right = roads[r].points[c+1].pos;
                    for (int i = 0; i < bridgeObject.sharedMesh.vertices.Length; i++)
                    {
                        verts.Add((bridgeRotation * bridgeObject.sharedMesh.vertices[i]) + roads[r].points[c].pos);
                    }
                    for (int i = 0; i < bridgeObject.sharedMesh.triangles.Length; i++)
                    {
                        tris.Add(bridgeObject.sharedMesh.triangles[i] + offset);
                    }
                }
            }
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        //mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        bridgeMesh.mesh = mesh;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        for (int i = 1; i < tempPoints.Count - 1; i++)
        {
            //Collider[] colliders = Physics.OverlapBox(tempPoints[i].pos, new Vector3(distBetweenPoints/2f, roadWitdh/2f, roadWitdh/2f));
            Gizmos.DrawWireCube(tempPoints[i].pos, new Vector3(distBetweenPoints, roadWitdh, roadWitdh));
        }


        int count = 0;
        float size = 0.005f;
        foreach (KeyValuePair<Vector2, List<Vector3>> kvp in crossings)
        {
            if (count == 0)
                Gizmos.color = Color.yellow;
            if (count == 1)
                Gizmos.color = Color.black;
            if (count == 2)
                Gizmos.color = Color.blue;
            if (count == 3)
                Gizmos.color = Color.white;

            //Debug.Log(kvp.Value.Count);
            for (int i = 0; i < kvp.Value.Count; i++)
            {
                Gizmos.DrawSphere(kvp.Value[i], size);
            }
            count++;
            size = size - 0.001f;
        }



        Gizmos.color = Color.red;
        if (haveClicked && tempPoints.Count != 0)
        {
            Gizmos.color = Color.yellow;
            for (int j = 0; j < tempPoints.Count; j++)
            {
                if (canPlace)
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.red;
                Gizmos.DrawSphere(tempPoints[j].pos, 0.005f);
            }
        }
        Gizmos.DrawSphere(mousePos, 0.01f);

        List<Color> colors = new List<Color>() { Color.blue, Color.cyan, Color.gray, Color.green, Color.magenta };
        if (drawMode == Drawmode.Points)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                for (int j = 0; j < roads[i].points.Count; j++)
                {
                    Gizmos.color = colors[i % colors.Count];

                    if (roads[i].points[j].curve)
                        Gizmos.color = Color.black;


                    Gizmos.DrawSphere(roads[i].points[j].pos, 0.005f);
                }
            }
            foreach (KeyValuePair<Vector3, List<Roads>> kvp in Roads.intersection)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(kvp.Key, 0.0025f);
            }
        }
        if (drawMode == Drawmode.Curves)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                for (int j = 0; j < roads[i].points.Count; j++)
                {
                    Gizmos.color = colors[j % colors.Count];
                    Gizmos.DrawSphere(roads[i].points[j].pos, 0.005f);
                    if (j != roads[i].points.Count - 1)
                    {
                        Gizmos.DrawLine(roads[i].points[j].pos, roads[i].points[j + 1].pos);
                    }
                }
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(roads[i].points[0].pos, 0.0025f);
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(roads[i].points[roads[i].points.Count - 1].pos, 0.0025f);
            }
            foreach (KeyValuePair<Vector3, List<Roads>> kvp in Roads.intersection)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(kvp.Key, 0.0025f);
            }
        }
        else if (drawMode == Drawmode.Edges)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                Gizmos.color = colors[i % colors.Count];
                for (int j = 0; j < roads[i].list1.Count; j++)
                {
                    Gizmos.DrawSphere(roads[i].list1[j].pos, 0.005f);
                    Gizmos.DrawSphere(roads[i].list2[j].pos, 0.005f);
                }
            }
        }
        else if (drawMode == Drawmode.EdgesLineColor)
        {
            for (int i = 0; i < roads.Count; i++)
            {
                Gizmos.color = colors[i % colors.Count];
                for (int j = 0; j < roads[i].list1.Count; j++)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(roads[i].list1[j].pos, 0.005f);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(roads[i].list2[j].pos, 0.005f);
                }
            }
        }
    }

}
public enum Drawmode { None, Points, Curves, Edges, EdgesLineColor };

public enum RoadDisplayMode { OnlyCountysRoads, AllRoads };