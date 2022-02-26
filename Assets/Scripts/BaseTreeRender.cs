using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseTreeRender : MonoBehaviour
{
    [SerializeField]
    protected Material material;
    protected Mesh mesh;
    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;
    protected Vector3[] vertices;
    protected int[] triangles;
    protected ProcTree Tree;
    private TreeMeshGenerartor _treeMeshGenerator;
    
    protected void Start()
    {
        _treeMeshGenerator = GetComponentInParent<TreeMeshGenerartor>();
        
        this.mesh = new Mesh();
        this.meshFilter = gameObject.GetComponent<MeshFilter>();
        this.meshRenderer = gameObject.GetComponent<MeshRenderer>();
        
        this.meshRenderer.material = material;
    }

    protected void Update() {
        Tree = Tree ?? _treeMeshGenerator.Tree;
    }
}
