using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;

public class CountyNormal : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(this.transform.position, GetComponent<MeshFilter>().mesh.normals[0]*30);
        Debug.Log( GetComponent<MeshFilter>().mesh.normals[0]);
    }
}
