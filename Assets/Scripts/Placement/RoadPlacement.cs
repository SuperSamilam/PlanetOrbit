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
    List<GameObject> list = new List<GameObject>();

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

                Vector3 start = Vector3.Slerp(firstPos, hit.point, 0);
                Vector3 end = Vector3.Slerp(firstPos, hit.point, 1);

                Vector3 dir = (end-start).normalized;
                Debug.DrawLine (start, start + dir * 10, Color.yellow, Mathf.Infinity);
                float dirAngel = Vector3.Angle(start, dir);
                Debug.Log(dirAngel);

                for (int i = 0; i <= numberOfVertices; i++)
                {
                    Vector3 mainPoint = Vector3.Slerp(firstPos, hit.point, i / (float)numberOfVertices);
                    Vector3 secondPoint = Vector3.Slerp(firstPos, hit.point, (i + 1) / (float)numberOfVertices);
                    Ray roadRay = new Ray(mainPoint * 2, -mainPoint * 0.5f);
                    RaycastHit roadHit;
                    if (Physics.Raycast(roadRay, out roadHit, Mathf.Infinity))
                    {
                        GameObject road = Instantiate(roadObj, mainPoint * 1.005f, Quaternion.identity);
                        road.transform.forward = roadHit.normal;
                        Vector3 crossAngle = Vector3.Cross(mainPoint, secondPoint);
                        //Debug.DrawLine(crossAngle, crossAngle * 20f, Color.blue, 60f);
                        float dotAngle = Vector3.Dot(mainPoint, secondPoint);
                        //Debug.Log(dotAngle);



                        road.transform.Rotate(Vector3.forward, dirAngel);

                        list.Add(road);
                    }





                    //Instantiate(roadObj, firstPos, Quaternion.identity);
                    // Instantiate(roadObj, hit.point, Quaternion.identity);


                }

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

    void OnDrawGizmos()
    {
        for (int i = 0; i < list.Count; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(list[i].transform.position, 0.01f);

            Gizmos.color = Color.blue;
            //Gizmos.DrawLine(list[i].transform.position, list[i].transform.forward);
            Gizmos.color = Color.green;
            //Gizmos.DrawLine(list[i].transform.position, list[i].transform.right);
            Gizmos.color = Color.yellow;
            //Gizmos.DrawLine(list[i].transform.position, list[i].transform.forward);

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
