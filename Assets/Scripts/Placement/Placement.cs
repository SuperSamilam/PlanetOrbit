using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Networking;

public class Placement : MonoBehaviour
{
    [SerializeField] GameObject placeObject;
    [SerializeField] GameObject hand;
    [SerializeField] LayerMask mask;
    [SerializeField] LayerMask buildingMask;
    [SerializeField] Material canPlaceMat;
    [SerializeField] Material cantPlaceMat;

    int rotation = 0;
    GameObject prefab;
    GameObject country;
    Ray ray;
    RaycastHit hit;
    bool canBuild = false;

    void Start()
    {
        setUpHand();
    }
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
        // if (Input.GetKey(KeyCode.R))
        // {
        //     rotation++;
        //     prefab.transform.up = Vector3.up;
        //     prefab.transform.rotation = Quaternion.Euler(new Vector3(prefab.transform.rotation.x, rotation, prefab.transform.rotation.z));
        // }
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~mask))
        {
            hand.transform.position = hit.point;
            prefab.transform.up = hit.normal;
            prefab.transform.GetComponent<MeshRenderer>().material = canPlaceMat;
            if (hit.transform.tag != "Country")
            {
                prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
                return;
            }
        }
        else
        {
            hand.transform.position = hit.point;
            prefab.transform.up = hit.normal;
            prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
            return;
        }

        BoxCollider buildingCollider = prefab.GetComponent<BoxCollider>();
        if (Physics.OverlapBox(prefab.transform.position, buildingCollider.size/2, prefab.transform.rotation, buildingMask).Length != 0)
        {
            Debug.Log("colliding");
            canBuild = false;
            prefab.transform.GetComponent<MeshRenderer>().material = cantPlaceMat;
        }
        else
            canBuild = true;


        Debug.Log("Canbuild: " + canBuild);

        if (canBuild && Input.GetMouseButtonDown(0))
        {
            GameObject obj = Instantiate(placeObject, prefab.transform.position, prefab.transform.rotation);
            obj.layer = LayerMask.NameToLayer("Building");
        }
    }
}
