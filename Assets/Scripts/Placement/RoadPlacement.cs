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
    List<Vector3> list = new List<Vector3>();

    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~mask))
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

                Vector3 dir = (firstPos-hit.point).normalized;

                for (int i = 0; i <= numberOfVertices; i++)
                {
                    Vector3 mainPoint = Vector3.Slerp(firstPos, hit.point, i/(float)numberOfVertices);
                    Vector3 right = Vector3.Cross(mainPoint.normalized, new Vector3(0,1,0));
                    Vector3 left = -right;

                    list.Add(Vector3.Slerp(firstPos, hit.point, i/(float)numberOfVertices));
                    list.Add(right);
                    list.Add(left);
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
        Gizmos.color = Color.red;
        for (int i = 0; i < list.Count; i++)
        {
            Gizmos.DrawSphere(list[i], 0.01f);
        }
    }
}
