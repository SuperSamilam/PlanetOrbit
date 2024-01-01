using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;

public class CountyNormal : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawRay(transform.position, transform.right);

        Gizmos.color = Color.blue;

        Gizmos.DrawRay(transform.position, transform.forward);

        Gizmos.color = Color.red;

        Gizmos.DrawRay(transform.position, transform.up);

        // Gizmos.DrawRay(transform.position, -transform.right);
        // Gizmos.DrawRay(transform.position, -transform.forward);

        //Gizmos.DrawRay(transform.position, transform.up);
    }
}
