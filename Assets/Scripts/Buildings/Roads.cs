using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Roads
{
    public List<RoadNode> points;
    public List<RoadNode> curvedpoints;
    public List<RoadNode> listOfNode1;
    public List<RoadNode> listOfNode2;
    public List<RoadNode> drawPoints;

    public Roads()
    {
        points = new List<RoadNode>();
        curvedpoints = new List<RoadNode>();
        listOfNode1 = new List<RoadNode>();
        listOfNode2 = new List<RoadNode>();
    }
    public Roads(List<RoadNode> points)
    {
        this.points = points;
        curvedpoints = new List<RoadNode>();
        listOfNode1 = new List<RoadNode>();
        listOfNode2 = new List<RoadNode>();
    }
    public Roads(List<RoadNode> points, List<RoadNode> curvedpoints)
    {
        this.points = points;
        this.curvedpoints = curvedpoints;
        listOfNode1 = new List<RoadNode>();
        listOfNode2 = new List<RoadNode>();
    }

    public void ResetConnections()
    {
        for (int i = 0; i < curvedpoints.Count; i++)
        {
            curvedpoints[i].ClearConnections();
        }
    }
    public void DrawNewConnectionTree(List<Vector3> visited, RoadNode node)
    {
        visited.Add(node.pos);
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

    public void ClearConnections()
    {
        neigbours.Clear();
    }
}
