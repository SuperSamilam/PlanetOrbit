using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float scrollSensitivity = 1f;
    [SerializeField] Camera myCamera;
    [SerializeField] float closestPos;
    [SerializeField] float furthestPos;

    Material countyMat;
    [SerializeField] Material selectedMat;
    GameObject country = null;
    Vector3 oldPos;
    float distanceFromTarget;

    Ray ray;
    RaycastHit hit;
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            oldPos = myCamera.ScreenToViewportPoint(Input.mousePosition);
        }
        if (Input.GetMouseButton(1))
        {
            Vector3 direction = oldPos - myCamera.ScreenToViewportPoint(Input.mousePosition);


            myCamera.transform.Rotate(new Vector3(1, 0, 0), direction.y * 180);
            myCamera.transform.Rotate(new Vector3(0, 1, 0), -direction.x * 180, Space.World);

            myCamera.transform.position = target.position - myCamera.transform.forward * distanceFromTarget;

            oldPos = myCamera.ScreenToViewportPoint(Input.mousePosition);
        }
        else
            myCamera.transform.position = target.position - myCamera.transform.forward * distanceFromTarget;
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        distanceFromTarget = Mathf.Clamp(distanceFromTarget + scrollInput * -scrollSensitivity, closestPos, furthestPos);

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.gameObject.tag == "Country")
            {
                if (country != hit.transform.gameObject)
                {
                    if (country == null)
                    {
                        country = hit.transform.gameObject;
                        countyMat = hit.transform.gameObject.GetComponent<MeshRenderer>().material;
                    }

                    country.transform.gameObject.GetComponent<MeshRenderer>().material = countyMat;
                    countyMat = hit.transform.gameObject.GetComponent<MeshRenderer>().material;
                    country = hit.transform.gameObject;
                }
                hit.transform.gameObject.GetComponent<MeshRenderer>().material = selectedMat;


            }
            else
            {
                if (country != null)
                    country.transform.gameObject.GetComponent<MeshRenderer>().material = countyMat;
            }
        }
    }
}
