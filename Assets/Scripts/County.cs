using System;
using UnityEngine;

[System.Serializable]
public class County 
{
    public string name;
    public GameObject obj;
    public int neighbours;

    [Range(0,1)]
    public float treeResources = 0.5f;

    [Range(0,1)]
    public float ironResources = 0.5f;

    [Range(0,1)]
    public float coalResources = 0.5f;

    [Range(0,1)]
    public float uraniumResources = 0.5f;


}

