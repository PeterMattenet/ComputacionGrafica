using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TwigRender : BaseTreeRender
{
    new void Update() {
        base.Update();
        if (this.meshFilter.mesh.vertexCount == 0) {
            this.meshFilter.mesh = mesh;
            
            this.mesh.vertices = Tree.vertsTwig.ToArray();
            this.mesh.triangles = Tree.facesTwig.SelectMany(v3 => new int[] { (int) v3[0], (int) v3[1], (int) v3[2]}).ToArray();
            this.mesh.normals = Tree.normalsTwig.ToArray();
            this.mesh.uv = Tree.uvsTwig.ToArray();
            this.mesh.tangents = Tree.tangentsTwig.ToArray();
        }
    }
}
