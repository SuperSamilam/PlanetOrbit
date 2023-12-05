using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadPlacement : MonoBehaviour
{
    [SerializeField] GameObject roadObj;
    [SerializeField] LayerMask mask;
    [SerializeField] float distanceBetweenRoadVertices = 0.05f;
    [SerializeField] float roadWitdh = 0.05f;
    Ray ray;
    RaycastHit hit;
    Vector3 firstPos;
    bool firstClick;
    List<(Vector3, Vector3)> list = new List<(Vector3, Vector3)>();

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
                list.Clear();
                //Vector3 midPos = (firstPos + hit.point)/2;
                //Instantiate(roadObj, midPos + hit.transform.up*5, Quaternion.identity);
                //for (int i = 0; i < )

                //Calculation to find the points on the line to make a obejct on
                float dot = Vector3.Dot(firstPos, hit.point);
                dot = dot / (firstPos.magnitude * hit.point.magnitude);
                float acos = Mathf.Acos(dot);
                float angle = acos * 180 / Mathf.PI;
                float archLenght = angle / 360 * 2 * Mathf.PI * Vector3.Distance(Vector3.zero, firstPos);
                int numberOfVertices = Mathf.CeilToInt(archLenght / distanceBetweenRoadVertices);



                for (int i = 0; i <= numberOfVertices; i++)
                {
                    Vector3 mainPoint = Vector3.Slerp(firstPos, hit.point, i / (float)numberOfVertices);

                    Ray roadRay = Camera.main.ScreenPointToRay(mainPoint*2);
                    RaycastHit roadHit;
                    if (Physics.Raycast(roadRay, out roadHit, Mathf.Infinity, mask))
                    {
                        Vector3 pos = roadHit.point;
                        Vector3 rot = roadHit.normal;
                        list.Add((pos, rot));
                    }


                    // Vector3 zeroPoint = Vector3.zero;

                    // Vector3 relativeProd = Vector3.Cross(mainPoint, Vector3.up).normalized;


                    // float normal = Vector3.Angle(mainPoint, zeroPoint);
                    // Debug.Log(mainPoint + " " + relativeProd);
                    //Vector3 right = Vector3.Cross(mainPoint);
                    //Vector3 left = -right;

                    //list.Add(mainPoint);
                    // list.Add(relativeProd);
                    // list.Add(-relativeProd);
                    //break;


                    //list.Add(Vector3.Slerp(firstPos, hit.point, i / (float)numberOfVertices));
                    //list.Add(right);
                    //list.Add(left);

                    //Debug.Log(mainPoint + " " + right);

                }





                //Instantiate(roadObj, firstPos, Quaternion.identity);
                // Instantiate(roadObj, hit.point, Quaternion.identity);



                firstClick = false;
                firstPos = Vector3.zero;
            }
        }
        else
        {
            // hand.transform.position = hit.point;
            // prefab.transform.up = hit.normal;
            // prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
            // return;
        }
    }

    void OnDrawGizmos()
    {
        for (int i = 0; i < list.Count; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(list[i].Item1, 0.01f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(list[i].Item1, Vector3.up);
            Gizmos.DrawRay(list[i].Item1, -Vector3.up);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(list[i].Item1, -Vector3.right);
            Gizmos.DrawRay(list[i].Item1, Vector3.right);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(list[i].Item1, list[i].Item2);
            Gizmos.DrawRay(list[i].Item1, list[i].Item2);
            //Gizmos.DrawLine(list[i], Vector3.Cross(list[i], Vector3.up).normalized);
            //Gizmos.DrawLine(list[i], Vector3.Cross(list[i], Vector3.back));
            //Gizmos.DrawLine(list[i], Vector3.up);
            // Gizmos.DrawLine(list[i], Vector3.right);
            // Gizmos.DrawLine(list[i], Vector3.left);
            // Gizmos.DrawLine(list[i], Vector3.down);
            // Gizmos.DrawLine(list[i], Vector3.forward);
            // Gizmos.DrawLine(list[i], Vector3.back);
        }
    }
}
