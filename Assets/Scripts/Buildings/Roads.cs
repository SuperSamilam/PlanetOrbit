using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Roads
{
    public List<Vector3> line1;
    public List<Vector3> line2;

    public Roads() { }

    public Roads(List<Vector3> line1, List<Vector3> line2)
    {
        this.line1 = line1;
        this.line2 = line2;
    }
}
