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

    [SerializeField] Material selectedMat;
    Vector3 oldPos;
    float distanceFromTarget;

    void Update()
    {
        //check if i click the first time
        if (Input.GetMouseButtonDown(1))
        {
            oldPos = myCamera.ScreenToViewportPoint(Input.mousePosition);
        }
        if (Input.GetMouseButton(1))
        {
            //get the directions from the 2 vectors 
            Vector3 direction = oldPos - myCamera.ScreenToViewportPoint(Input.mousePosition);

            //rotating on the axis 1,0,0 by rotation amount whatever
            myCamera.transform.Rotate(new Vector3(1, 0, 0), direction.y * 180);
            myCamera.transform.Rotate(new Vector3(0, 1, 0), -direction.x * 180, Space.World);

            myCamera.transform.position = target.position - myCamera.transform.forward * distanceFromTarget;

            oldPos = myCamera.ScreenToViewportPoint(Input.mousePosition);
        }
        else
            myCamera.transform.position = target.position - myCamera.transform.forward * distanceFromTarget;

        //make it zom in and out
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        distanceFromTarget = Mathf.Clamp(distanceFromTarget + scrollInput * -scrollSensitivity, closestPos, furthestPos);
    }
}
