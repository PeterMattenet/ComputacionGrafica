using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TrunkRender : BaseTreeRender
{
    new void Update() {
        base.Update();
        if (this.meshFilter.mesh.vertexCount == 0) {
            newTree = false;
            this.meshFilter.mesh = mesh;

            this.mesh.vertices = Tree.verts.ToArray();
            this.mesh.triangles = Tree.faces.SelectMany(v3 => new int[] { (int) v3[0], (int) v3[1], (int) v3[2]}).ToArray();
            this.mesh.normals = Tree.normals.ToArray();
            
            this.mesh.uv = Tree.UV.ToArray();
            this.mesh.SetUVs(1, Tree.anchors);
        }

        if (newTree) {
            this.mesh.Clear();
            this.mesh = new Mesh();
            this.meshFilter.mesh = mesh;

            this.mesh.vertices = Tree.verts.ToArray();
            this.mesh.triangles = Tree.faces.SelectMany(v3 => new int[] { (int) v3[0], (int) v3[1], (int) v3[2]}).ToArray();
            this.mesh.normals = Tree.normals.ToArray();
            
            this.mesh.uv = Tree.UV.ToArray();
            this.mesh.SetUVs(1, Tree.anchors);
        }
    }


    public override void AdjustWindStrength(float windStrength) {
        gameObject.GetComponent<Renderer>().sharedMaterial.SetFloat("WindStrength_Float", windStrength * 0.4f);
        this.meshRenderer.material = material;
    }
}
