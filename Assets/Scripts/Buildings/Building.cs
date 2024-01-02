using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Building", menuName = "Buildings/Building", order = 1)]
public class Building : ScriptableObject
{
    public GameObject gameObject;
    public float connectionRadius;
}
