using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Roads
{
    public static Dictionary<Vector3, List<Roads>> intersection = new();
    public List<RoadNode> points;
    public List<Vector3> roadCurvePoints;
    public List<RoadNode> list1;
    public List<RoadNode> list2;
    public Vector3 extraPos;
    public bool loop;

    public Roads(List<RoadNode> points)
    {
        this.points = points;
        roadCurvePoints = new List<Vector3>();
        list1 = new List<RoadNode>();
        list2 = new List<RoadNode>();
    }
    public Roads()
    {
        points = new List<RoadNode>();
        roadCurvePoints = new List<Vector3>();
        list1 = new List<RoadNode>();
        list2 = new List<RoadNode>();
    }
}

public class RoadNode
{
    public Vector3 pos;
    public bool curve;
    public bool brigde;
    public string country;

    public RoadNode(Vector3 pos)
    {
        this.pos = pos;
    }
    public RoadNode(Vector3 pos, string country)
    {
        this.pos = pos;
        this.country = country;
    }

    
}


