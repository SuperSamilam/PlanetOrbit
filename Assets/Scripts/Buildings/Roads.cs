using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Roads
{
    public List<RoadNode> points;
    public List<RoadNode> curvedpoints;
    public List<RoadNode> drawPoints;

    public Roads()
    {
        points = new List<RoadNode>();
        curvedpoints = new List<RoadNode>();
    }
    public Roads(List<RoadNode> points)
    {
        this.points = points;
        curvedpoints = new List<RoadNode>();
    }
    public Roads(List<RoadNode> points, List<RoadNode> curvedpoints)
    {
        this.points = points;
        this.curvedpoints = curvedpoints;
    }
}

public class RoadNode
{
    public Vector3 pos;
    public bool curve = false;
    public List<Vector3> neigbours;

    public RoadNode(Vector3 pos, List<Vector3> neigbours)
    {
        this.pos = pos;
        this.neigbours = neigbours;
    }
    public RoadNode(Vector3 pos)
    {
        this.pos = pos;
        neigbours = new List<Vector3>();
    }
}
