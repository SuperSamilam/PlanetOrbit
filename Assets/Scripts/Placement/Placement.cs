using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Networking;

public class Placement : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] GameObject placeObject;
    [SerializeField] GameObject hand;
    [SerializeField] LayerMask mask;
    [SerializeField] LayerMask water;
    [SerializeField] LayerMask buildingMask;
    [SerializeField] Material canPlaceMat;
    [SerializeField] Material cantPlaceMat;

    float rotation = 0;
    GameObject prefab;
    GameObject country;
    Ray ray;
    RaycastHit hit;
    bool canBuild = false;

    void Start()
    {
        setUpHand();
    }
    //assign the gameobject to place
    void setUpHand()
    {
        if (hand.transform.childCount > 0)
        {
            Destroy(hand.transform.GetChild(0));
        }

        prefab = Instantiate(placeObject, hand.transform.position, Quaternion.identity);
        prefab.transform.parent = hand.transform;
        //handOutline.transform.localScale = 
    }
    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~mask))
        {
            //set the object and the current position and asign teh right mat
            hand.transform.position = hit.point;
            prefab.transform.up = hit.normal;
            prefab.transform.Rotate(new Vector3(0,1,0), rotation);
            prefab.transform.GetComponent<MeshRenderer>().material = canPlaceMat;
            if (hit.transform.tag != "Country")
            {
                prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
                return;
            }
        }
        else
        {
            //dit not hit correctly and assign the fauly material
            hand.transform.position = hit.point;
            prefab.transform.up = hit.normal;
            prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
            return;
        }

        //Check if it is colliding with an object
        BoxCollider buildingCollider = prefab.GetComponent<BoxCollider>();
        if (Physics.OverlapBox(prefab.transform.position, buildingCollider.size / 2, prefab.transform.rotation, buildingMask).Length != 0)
        {
            canBuild = false;
            prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
            return;
        }
        else
            canBuild = true;

        //change the rotation
        if (Input.GetKey(KeyCode.R))
        {
            rotation += 0.5f;
        }

        Ray sideRay;

        //Shoot 4 rays to the side seeing if anything is placed to the side
        sideRay = new Ray(prefab.transform.position, prefab.transform.right);
        if (Physics.Raycast(sideRay, 5, water))
        {
            canBuild = false;
            prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
            return;
        }
        sideRay = new Ray(prefab.transform.position, prefab.transform.right * -1);
        if (Physics.Raycast(sideRay, 5, water))
        {
            canBuild = false;
            prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
            return;
        }
        sideRay = new Ray(prefab.transform.position, prefab.transform.forward);
        if (Physics.Raycast(sideRay, 5, water))
        {
            canBuild = false;
            prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
            return;
        }
        sideRay = new Ray(prefab.transform.position, prefab.transform.forward * -1);
        if (Physics.Raycast(sideRay, 5, water))
        {
            canBuild = false;
            prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
            return;
        }

        //place the building
        if (canBuild && Input.GetMouseButtonDown(0))
        {
            GameObject obj = Instantiate(placeObject, prefab.transform.position, prefab.transform.rotation);
            obj.layer = LayerMask.NameToLayer("Building");
        }

    }
}
