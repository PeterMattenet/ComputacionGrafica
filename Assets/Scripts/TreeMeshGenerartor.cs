using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class TreeMeshGenerartor : MonoBehaviour
{
    private ProcTree _tree;
    private System.Random rand;
    private TreeInputProperties properties;
    public ProcTree Tree { 
        get { return _tree ?? CreateTree(); } 
        private set { this._tree = value; }
    }
    
    private ProcTree CreateTree() {
        _tree = new ProcTree(properties);
        return _tree;
    }
    public void CreateNewRandomTree() {
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

        

        double clumpMax = 0.55f;
        double clumpMin = 0.35f;
        properties.clumpMax = (float) (clumpMin + (clumpMax - clumpMin) * rand.NextDouble());

        double clumpMinMin = 0.25f;
        properties.clumpMin = (float) (clumpMinMin + (properties.clumpMax - clumpMinMin) * rand.NextDouble());

        double bfMax = 2f;
        double bfMin = 0.8f;
        properties.branchFactor = (float) (bfMin + (bfMax - bfMin) * rand.NextDouble());
        // Cuan rapido van a empezar a crecer las ramas para abajo a medida que mas se alejan del tronco
        double dropAmountMin = -0.4f;
        double dropAmountMax = 0f;
        properties.dropAmount = (float) (dropAmountMin + (dropAmountMax - dropAmountMin) * rand.NextDouble());

        // Cuan rapido suben las ramas sobre el eje Y en funcion de cuan cerca estan del suelo
        double growAmountMax = 0.3f;
        double growAmountMin = 0.1f;
        properties.growAmount = (float) (growAmountMin + (growAmountMax - growAmountMin) * rand.NextDouble());
        
        // Arrastra la direccion de bifuracion de las ramas en una unica direccion del eje X
        
        double sweepMax = 0.2f;
        double sweepMin = -0.2f;
        properties.sweepAmount = (float) (sweepMin + (sweepMax - sweepMin) * rand.NextDouble());

        properties.maxRadius = 0.139f;
        properties.climbRate = 0.371f;
        properties.trunkKink = 0.093f;
        properties.treeSteps = 5;
        // Cuanto se va achicando la longitud de una Branch a medida que avanza cada nivel
        properties.taperRate = 0.947f;
        // A que ritmo (velocidad) el radio de las ramas se va achicando a medida que el tronco se bifurca y pasan los "levels"
        properties.radiusFalloffRate = 0.73f;
        properties.twistRate = 0.02f;
        properties.trunkLength = 2.4f;

        // nuevas
        properties.foliageAmountMult = 0f;
        properties.leafLengthWidthRatio = 2.5f;
        properties.leafCrossSectionRatio = 0.75f;
        properties.leafSegments = 5;
        properties.leafParentBranchOffset = 0.3f;
        properties.leafDownwardTip = 0f;
        properties.leavesPerBranchLine = 3;
        properties.leavesPerBranchRing = 2;

        ProcTree tree = new ProcTree(properties);
        
        GetComponentInChildren<TwigRender>().NewTree(tree);
        GetComponentInChildren<TrunkRender>().NewTree(tree);
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
        properties.twistRate = 0.02f;
        properties.trunkLength = 2.4f;

        // nuevas
        properties.foliageAmountMult = 0f;
        properties.leafLengthWidthRatio = 2.5f;
        properties.leafCrossSectionRatio = 0.75f;
        properties.leafSegments = 5;
        properties.leafParentBranchOffset = 0.3f;
        properties.leafDownwardTip = 0f;
        properties.leavesPerBranchLine = 3;
        properties.leavesPerBranchRing = 2;
        
        this.rand = new System.Random((int) properties.seed);
    }
    

}

