using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class TreeMeshGenerartor : MonoBehaviour
{
    private ProcTree _tree;
    private TreeInputProperties properties;
    public ProcTree Tree { 
        get { return _tree ?? CreateTree(); } 
        private set { this._tree = value; }
    }
    
    private ProcTree CreateTree() {
        _tree = new ProcTree(properties);
        return _tree;
    }
    void Start()
    {   
        properties = new TreeInputProperties();

        properties.seed = 263;
        // Cantidad de vertices que va a definir la circumferencia de cada rama
        properties.segments = 6;
        // Cantidad de pasos de bifurcaciones que el tronco va a tener
        properties.levels = 5;
        properties.vMultiplier = 4.36f;
        // Tamaño de la hoja
        properties.twigScale = 0.62f;
        properties.initialBranchLength = 0.49f;
        properties.lengthFalloffFactor = 0.85f;
        properties.lengthFalloffPower = 0.99f;
        properties.clumpMax = 0.454f;
        properties.clumpMin = 0.404f;
        properties.branchFactor = 2.45f;
        // Cuan rapido van a empezar a crecer las ramas para abajo a medida que mas se alejan del tronco
        properties.dropAmount = -0.1f;
        // Cuan rapido suben las ramas sobre el eje Y en funcion de cuan cerca estan del suelo
        properties.growAmount = 0.235f;
        // Arrastra la direccion de bifuracion de las ramas en una unica direccion del eje X
        properties.sweepAmount = 0.01f;
        properties.maxRadius = 0.139f;
        properties.climbRate = 0.371f;
        properties.trunkKink = 0.093f;
        properties.treeSteps = 5;
        // Cuanto se va achicando la longitud de una Branch a medida que avanza cada nivel
        properties.taperRate = 0.947f;
        // A que ritmo (velocidad) el radio de las ramas se va achicando a medida que el tronco se bifurca y pasan los "levels"
        properties.radiusFalloffRate = 0.73f;
        properties.twistRate = 3.02f;
        properties.trunkLength = 2.4f;

        // nuevas
        properties.foliageAmountMult = 0f;
        properties.leafLengthWidthRatio = 2.5f;
        properties.leafCrossSectionRatio = 0.75f;
        properties.leafSegments = 5;
        properties.leafParentBranchOffset = 0.3f;
        properties.leafDownwardTip = 0f;
        properties.leavesPerBranchLine = 1;
        properties.leavesPerBranchRing = 1;
    }
    

}

