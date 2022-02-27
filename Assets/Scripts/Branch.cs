using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Branch
{
    public Vector3 head;
    public string type;
	public float radius;
    public float length;
    public Branch parent;
	public Branch child1;
	public Branch child0;
	public IList<int> root;
	public Vector3 tangent;
	public IList<int> ring0;
	public IList<int> ring1;
	public IList<int> ring2;
	public int? end;	

	// Upgrades Pedro
	public int level;
	public Branch anchor;

	public Branch(Vector3 head) 
	{
		this.head = head;
		this.child0 = null;
		this.child1 = null;
		this.parent = null;
		this.anchor = null;
		this.length = 1;
	}
    public Branch(Vector3 head, Branch parent) : this(head) {
		this.parent = parent;
    }

    public Vector3 MirrorBranch(Vector3 vec, Vector3 norm, TreeInputProperties properties) {
        var v = Vector3.Cross(norm,Vector3.Cross(vec,norm));
		var s = properties.branchFactor * Vector3.Dot(v,vec);
		return new Vector3(vec[0]-v[0]*s,vec[1]-v[1]*s,vec[2]-v[2]*s);
    }

	public static System.Random rand;
    public void Split(int? level, int? steps, TreeInputProperties properties, int? l1, int? l2){
		
        l1 = l1 ?? 1;
        l2 = l2 ?? 1;

		rand = rand ?? new System.Random((int) properties.seed);

        // remaining levels??
		int cLevel = level ?? properties.levels;
		int rLevel = properties.levels - cLevel;

		int cSteps = steps ?? properties.treeSteps;
		
		// guardar el level actual de la branch para estimar cuan lejos esta del tronco del arbol
		this.level = cLevel + 1;

        // point of origin?
        Vector3 po;
		if(this.parent != null){
			po = this.parent.head;
		}else{
			po = new Vector3(0,0,0);
			this.type="trunk";
		}

		// El punto de este branch
		var so = this.head;

		// La nueva direccion es simplemente el vector que une el punto del branch actual, con el del padre
		// La hoja, de haber una, se va a orientar en esta direccion
		var dir = Vector3.Normalize(so - po);

        // Ok, podria entender que lo que busca es un vector que sea "normal" a otro vector? Osea que su producto dot sea 0
		var normal = Vector3.Cross(dir, new Vector3(dir[2],dir[0],dir[1]));
        // What the orto es esto? https://www.youtube.com/watch?v=y4zDK3byeTQ&ab_channel=MuPrimeMath??
		var tangent = Vector3.Cross(dir,normal);

		var randomVal1 =  (float) rand.Next(0, 1000000) / 1000000;
		var randomVal2 =  (float) rand.Next(0, 1000000) / 1000000;
		var clumpmax = properties.clumpMax;
		var clumpmin= properties.clumpMin;
		
        // Teoria: el adj define un vector aleatorio condicionado por la normal y tangente, que se usa para desviar la Branch de la nueva direccion,
		// agregando aleatoriedad. Este Adj va a ser mas prominente cuanto menor sea el Clump
		// Este Adj agrega variabilidad para forzar a que la Branch que esta siendo creada no avance en linea recta respecto a la direccion de la anterior
		Vector3 adj = randomVal1 * normal + (1-randomVal1) * tangent;
		
        // 50% de que el Adj vaya en la direccion contraria
        if ( randomVal1 > 0.5 ) adj = -1 * adj;
		
        // Definir un clump que este lerpeado entre el minimo y maximo de manera uniformemente aleatoria
		float clump= (clumpmax - clumpmin) * randomVal1 + clumpmin;
		
        // En base al clump, entonces definir que va a ser mas significativo para la nueva direccion:
		// la variabilidad de Adj o la direccion deterministica Dir
        Vector3 newdir= Vector3.Normalize((1-clump) * adj + clump * dir);
			
        // Espejala en funcion del eje Dir (el vector Dir es la direccion entre el Branch.head del pardre y el Branch.head actual)
		Vector3 newdir2 = this.MirrorBranch(newdir,dir,properties);

        // 50% de usar el branch espejado como el Branch que va a seguir generando bifurcaciones
        if(randomVal1>0.5){
			Vector3 tmp=newdir;
			newdir=newdir2;
			newdir2=tmp;
		}

        // Si este branch es un tronco (quedan steps), entonces se le aplica un Twist rate a la rama "no tronco" que va a generar
		if ( cSteps > 0 ){
			float angle = cSteps / properties.treeSteps * 2 * Mathf.PI * properties.twistRate;
			newdir2 = Vector3.Normalize(new Vector3(Mathf.Sin(angle),randomVal1,Mathf.Cos(angle)));
		}

		// A medida que menos levels van quedando, porque el tronco dejo de crecer, entonces el drop amount se achica, al igual que el grow amount		
		float growAmount = Mathf.Pow(cLevel, 2) / (properties.levels*properties.levels) * properties.growAmount;
		float dropAmount = rLevel * properties.dropAmount;
		
		// Segun el remaining level (no se si es correct) desplaza las cosas
		// Esto puede ser util para modelar arboles que viven en areas con presion constante de fuerzas como el viento
		// O donde estan obligados a crecer en una direccion del plano para alcanzar la luz solar
		float sweepAmount = rLevel * properties.sweepAmount;

		// El grow/drop amount define si la proxima direccion va a subir o bajar en el eje Y. Si se configuran muchos levels, pero el grow amount 
		// de input no es suficientemente alto, el arbol formaria arcos
		// Lo que si es raro, es que el sweep amount solo afecte el movimeinto en el eje X y no en el Z, pero eso no es un problema en este scope
		// Edit: el sweep efectivamente podria ser un input que desplace las direcciones tanto en x como Z.
		newdir = Vector3.Normalize(newdir + new Vector3(sweepAmount,dropAmount+growAmount,0));
		newdir2 = Vector3.Normalize(newdir2 + new Vector3(sweepAmount,dropAmount+growAmount,0));
		
		Vector3 head0 = so + this.length * newdir;
		Vector3 head1 = so + this.length * newdir2;

		this.child0 = new Branch(head0,this);
		this.child1 = new Branch(head1,this);
		
		this.child0.anchor = (type == "trunk") ? this : this.anchor;
		this.child1.anchor = (type == "trunk") ? this : this.anchor;
		
		// Acortar las proximas branches en funcion del fall off power/factor
		this.child0.length = Mathf.Pow(this.length,properties.lengthFalloffPower) * properties.lengthFalloffFactor;
		this.child1.length = Mathf.Pow(this.length,properties.lengthFalloffPower) * properties.lengthFalloffFactor;
    
		// Interpretacion: 
		// El arbol primero se va bifurcando generando una rama que va a tener X cantidad de niveles restantes, pero
		// la otra, siempre que sigan quedando Steps, va a seguir siendo un tronco.
		// Las branches de tipo Trunk difieren en que se les aplica el Taper Rate, Climb rate, y Trunk Kink
		if(cLevel > 0){
			if(cSteps > 0) {
				// La bifurcacion que va a seguir generando mas troncos, se le agrega algo de aleatoriedad en cuanto:
				//	- la velocidad con la que asciende (climb rate), que aumenta el valor del eje Y
				//  - cuanto se desalinea del eje x y z el branch "principal" de lo que ya estaba establecido, en funcion al TrunkKink. Basicamente cuanto
				//    se desplaza el tronco entre cada step
				this.child0.head=this.head + new Vector3((randomVal1 - 0.5f) * 2f * properties.trunkKink, properties.climbRate, (randomVal1 - 0.5f) * 2f * properties.trunkKink);
				// Una branch se define como tronco solo si aun tiene steps que le queden. Los steps indican cuanto mas va a avanzar una branch, indistintamente
				// de la cantidad de niveles que va a tener un arbol. Sirve para que no de la bifurcacion, solo 1 de las ramas siga avanzando mientras que la otra 
				// se vuelve una Twig
				this.child0.type = "trunk";
				// El largo de las ramas se va achicando en funcion al TaperRate si esta es un tronco
				this.child0.length=this.length * properties.taperRate;
				// Se intenta bajar los steps primero
				this.child0.Split(cLevel,cSteps-1,properties,l1+1,l2);
			} else{
				// Si no hay mas steps, las bifurcaciones continuan pero no se lo denomina un trunk, la longitud se mantiene igual, y no se le suma el climb rate
				// No tengo realmente una interpretacion de esto...
				this.child0.Split(cLevel-1,0,properties,l1+1,l2);
			}
			this.child1.Split(cLevel-1,0,properties,l1,l2+1);
		} 
	}

}
