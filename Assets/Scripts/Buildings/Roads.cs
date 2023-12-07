using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Roads
{
    public List<RoadNode> points;

    public Roads() { }

    public Roads(List<RoadNode> points)
    {
        this.points = points;
    }
}

public class RoadNode
{
    public Vector3 pos;
    public List<RoadNode> neigbours;

    public RoadNode(Vector3 pos, List<RoadNode> neigbours)
    {
        this.pos = pos;
        this.neigbours = neigbours;
    }
    public RoadNode(Vector3 pos)
    {
        this.pos = pos;
        neigbours = new List<RoadNode>();
    }
}
