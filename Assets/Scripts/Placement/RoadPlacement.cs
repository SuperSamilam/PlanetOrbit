using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadPlacement : MonoBehaviour
{
    [SerializeField] GameObject roadObj;
    [SerializeField] LayerMask mask;
    Ray ray;
    RaycastHit hit;
    Vector3 firstPos;
    bool firstClick;

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
                Vector3 midPos = (firstPos + hit.point)/2;
                Instantiate(roadObj, midPos + hit.transform.up*5, Quaternion.identity);


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
}
