using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadCollisionCheck : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Road")
        {
            Debug.Log("road");
        }
    }
}
