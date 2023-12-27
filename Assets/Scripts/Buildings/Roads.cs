using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Roads
{
    public static Dictionary<Vector3, List<Roads>> intersection = new();
    public List<RoadNode> points;
    public List<Vector3> roadCurvePoints;
    public List<Vector3> list1;
    public List<Vector3> list2;
    public Vector3 extraPos;
    public bool loop;

    public Roads(List<RoadNode> points)
    {
        this.points = points;
        roadCurvePoints = new List<Vector3>();
        list1 = new List<Vector3>();
        list2 = new List<Vector3>();
    }
    public Roads()
    {
        points = new List<RoadNode>();
        roadCurvePoints = new List<Vector3>();
        list1 = new List<Vector3>();
        list2 = new List<Vector3>();
    }
}

public class RoadNode
{
    public Vector3 pos;
    public List<Vector3> neighbours;
    public bool curve;

    public RoadNode(Vector3 pos, List<Vector3> neighbours)
    {
        this.pos = pos;
        this.neighbours = neighbours;
    }
    public RoadNode(Vector3 pos)
    {
        this.pos = pos;
        neighbours = new List<Vector3>();
    }
    public RoadNode()
    {
        neighbours = new List<Vector3>();
    }
}

