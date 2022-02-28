using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ProcTree
{
    private TreeInputProperties properties;
    private Branch root;
    public readonly List<Vector3> verts;
	public readonly	List<Vector3> faces;
	public readonly	List<Vector2> UV;
    public readonly	List<Vector3> normals;
    public readonly	List<Vector3> anchors;

	public readonly	List<Vector3>  vertsTwig;
	public readonly	List<Vector3>  normalsTwig;
	public readonly	List<Vector3> facesTwig;
	public readonly	List<Vector2>  uvsTwig;
	public readonly	List<Vector4> tangentsTwig;

    public ProcTree(TreeInputProperties properties) {
        this.properties = properties;
		this.properties.rseed=this.properties.seed;
		this.root=new Branch(new Vector3(0, this.properties.trunkLength, 0));
		
		rand = rand ?? new System.Random((int) properties.seed);
		this.root.length= this.properties.initialBranchLength;
		this.verts= new List<Vector3>();
		this.faces= new List<Vector3>();
		this.normals= new List<Vector3>();
		this.UV= new List<Vector2>();
		this.anchors = new List<Vector3>();
		this.vertsTwig= new List<Vector3>();
		this.normalsTwig= new List<Vector3>();
		this.facesTwig= new List<Vector3>();
		this.uvsTwig= new List<Vector2>();
		this.tangentsTwig= new List<Vector4>();
		this.root.Split(null,null,this.properties, 1, 1);
		this.CreateForks(null, null);
		this.DoFaces(null);
		this.CreateTwigs(null, null, null);
		this.CalcNormals();
    }
    
	// Interpretacion: rota el vector Vec sobre el eje Axis, por el angulo Angle
    private Vector3 vecAxisAngle(Vector3 vec, Vector3 axis, float angle) {
        float cosR = Mathf.Cos(angle);
        float sinR = Mathf.Sin(angle);
		
        return cosR * vec + sinR * Vector3.Cross(axis, vec) + (Vector3.Dot(axis, vec) * (1 - cosR)) * axis;
    }

    private Vector3 scaleInDirection(Vector3 vector, Vector3 direction, float scale) {
		float currentMag = Vector3.Dot(vector,direction);
		
		var change = direction * (currentMag * scale - currentMag);
		return vector + change;
	}

    public void CreateForks(Branch branch, float? radiusInput) 
    {
		branch = branch ?? this.root;
		float radius = radiusInput ?? this.properties.maxRadius;

		branch.radius = radius;
		
        radius = Mathf.Min(radius, branch.length);

		int segments= this.properties.segments;
		 
		float segmentAngle= Mathf.PI * 2/ segments;

		if(branch.parent == null) {
			// Crear la root del arbol. Esta lista enumera el Index de los vertices que va creando
			branch.root= new List<int>();
			Vector3 axis=new Vector3(0,1,0);

			// Crear el anillo de puntos que define el centro del cual se arma el modelo del  tronco arbol
			for(int i=0; i<segments; i++) {
				Vector3 vec= vecAxisAngle(new Vector3(-1,0,0), axis, -segmentAngle*i);
                // Agregar el indice del nuevo vertice (longitud de la lista de vertices) al root de esta branch
				branch.root.Add(verts.Count);
				// Contempla el ritmo de achicamiento del radio de las ramas
				verts.Add((radius/this.properties.radiusFalloffRate) * vec);
				anchors.Add(Vector3.zero);
			}
		}
		
		Vector3 anchor = branch.anchor != null ? branch.anchor.head : Vector3.zero;
		
		//Vector3.Cross the branches to get the left
		//add the branches to get the up
		// Si hay un child0, por definicion hay un child1, entonces esta rama no es una Twig
		if(branch.child0 != null) {
			Vector3 axis;
            if (branch.parent != null) {
				axis= Vector3.Normalize(branch.head - branch.parent.head);
			}else{
				axis = Vector3.Normalize(branch.head);
			}
			
			// Calculamos los ejes al centro de cada bifurcacion
			Vector3 axis1 = Vector3.Normalize(branch.head - branch.child0.head);
			Vector3 axis2 = Vector3.Normalize(branch.head - branch.child1.head);
			// Vector ortogonal al plano formado por las ramas. No se porque el codigo original lo define como tangente
			Vector3 tangent = Vector3.Normalize(Vector3.Cross(axis1,axis2));
			branch.tangent=tangent;
			
			// Axis3?? Ortogonal al plano definido por el vector Tangent y la resta de ambos ejes
			// Teoria: forma un vector ortogonal al plano de axis1 y axis2, pero rotado a 90 grados
			Vector3 axis3 = Vector3.Normalize(Vector3.Cross(tangent,Vector3.Normalize(-axis1 -axis2)));
			Vector3 dir = new Vector3(axis2[0], 0, axis2[2]);
						
			Vector3 centerloc = branch.head + dir * (-this.properties.maxRadius/2);

			IList<int> ring0 = branch.ring0 = new List<int>();
			IList<int> ring1 = branch.ring1 = new List<int>();
			IList<int> ring2 = branch.ring2 = new List<int>();
			
			float scale = this.properties.radiusFalloffRate;
			
			// Si el hijo es tambien parte del tronco entonces aplica la escala de TaperRate
			if(branch.child0.type == "trunk" || branch.type == "trunk") {
				scale = (float) 1/this.properties.taperRate;
			}
			
			//main segment ring
			int linch0 = verts.Count;
			ring0.Add(linch0);
			ring2.Add(linch0);
			verts.Add(centerloc + tangent * radius * scale);
			anchors.Add(anchor);
			
			int start = verts.Count - 1;			
			Vector3 d1 = vecAxisAngle(tangent, axis2, 1.57f);
			Vector3 d2 = Vector3.Normalize(Vector3.Cross(tangent,axis));
			float s = (float)1 / Vector3.Dot(d1,d2);

			for (var i=1; i<segments/2; i++) {
				var vec = vecAxisAngle(tangent,axis2,segmentAngle*i);
				ring0.Add(start+i);
				ring2.Add(start+i);
				vec = scaleInDirection(vec,d2,s);
				verts.Add(centerloc + vec * radius * scale);
				anchors.Add(anchor);
			}
			
            int linch1 = verts.Count;
			ring0.Add(linch1);
			ring1.Add(linch1);
			verts.Add(centerloc + tangent * -radius * scale);
			anchors.Add(anchor);

			for (var i = segments/2 +1; i < segments; i++) {
				Vector3 vec = vecAxisAngle(tangent, axis1, segmentAngle*i);
				ring0.Add(verts.Count);
				ring1.Add(verts.Count);
				verts.Add(centerloc + vec * radius * scale);
				anchors.Add(anchor);

			}

			ring1.Add(linch0);
			ring2.Add(linch1);
			
            start = verts.Count - 1;
			
            for (int i = 1; i < segments/2; i++) {
				Vector3 vec = vecAxisAngle(tangent,axis3,segmentAngle*i);
				ring1.Add(start+i);
				ring2.Add(start+(segments/2-i));
				Vector3 v = vec * radius * scale;
				verts.Add(centerloc + v);
				anchors.Add(anchor);
			}
			
			//child radius is related to the brans direction and the length of the branch
			float length0 = Vector3.Magnitude(branch.head - branch.child0.head);
			float length1 = Vector3.Magnitude(branch.head - branch.child1.head);
			
			float radius0 = 1 * radius * this.properties.radiusFalloffRate;
			float radius1 = 1 * radius * this.properties.radiusFalloffRate;

			if (branch.child0.type=="trunk") radius0 = radius * this.properties.taperRate;
			
			CreateForks(branch.child0, radius0);
			CreateForks(branch.child1, radius1);
			
		}else{
			//La branch termina en un punto unico
			branch.end = verts.Count;
			//branch.head=addVec(branch.head,scaleVec([this.properties.xBias,this.properties.yBias,this.properties.zBias],branch.length*3));
			verts.Add(branch.head);
			anchors.Add(anchor);
		}	
	}

	public static System.Random rand;
    public void CreateTwigs(Branch branch, int? l1, int? l2) {
		branch = branch ?? this.root;
		l1 = l1 ?? 1;
		l2 = l2 ?? 1;

		// Esta logica solo agrega la posibilidad de que aparezca follaje en ramas que no son parte de la copa 
		if (branch.type != "trunk") {
			// Valor entre 0 y 1 para definir la proximidad al tronco de esta Branch
			// FolliageAmountMult multiplica la probabilidad de que una ramita aparezca en este branch
			float likelyHoodOfTwig = ((float) branch.level / properties.levels) / Mathf.Max(properties.foliageAmountMult, float.Epsilon); 
			float randomVal1 = (float) rand.Next(0, 1000000) / 1000000;
			
			if (randomVal1 >= likelyHoodOfTwig) {
				
				Vector3 cross= Vector3.Normalize(Vector3.Cross(branch.parent.child0.head - branch.parent.head, branch.parent.child1.head - branch.parent.head));
				Vector3 direction = Vector3.Normalize(branch.head - branch.parent.head);		
				float scale = this.properties.twigScale * branch.length;
				Vector3 origin = branch.head - direction * properties.leafParentBranchOffset * branch.length;

				AddTwig(origin, scale, direction, cross);
					
				// Multiples hojas en la rama (asumiendo que esta branch es un End)
				// Idealmente estaria bueno hacer esta funcionalidad para branchas no troncales pero que no son End
				IList<int> ring;
				if (branch.parent.child0 == branch) {
					ring = branch.parent.ring1;
				} else {
					ring = branch.parent.ring2;
				} 

				int leavesPerBranchLine = properties.leavesPerBranchLine;
				float likelyHoodOfLeaf = (float) properties.leavesPerBranchRing / properties.segments;

				for (int i = 0; i < ring.Count; i++) {
					Vector3 rVert = verts[ring[i]];
					Vector3 lineDir = branch.head - rVert;
					float lineLength = Vector3.Magnitude(lineDir);
					float leafDistance = lineLength / leavesPerBranchLine;
					for (int k = 1; k < leavesPerBranchLine; k++) {
						Vector3 twigPosition = rVert + lineDir * (float) k/leavesPerBranchLine;
						cross= Vector3.Normalize(Vector3.Cross(rVert - branch.parent.head, branch.head - branch.parent.head));
						direction = Vector3.Normalize(vecAxisAngle(lineDir, cross, -Mathf.PI / 2));

						randomVal1 = properties.Random((float) rand.Next(0, 100) / 100);
						if (randomVal1 < likelyHoodOfLeaf) AddTwig(twigPosition, scale, direction, cross);
					}
				}
			}
		}

        if(branch.child0 != null) {	
            CreateTwigs(branch.child0, l1+1, l2);
			CreateTwigs(branch.child1, l1, l2+1);
		} 
    }

	// Variables globales a la clase para facilitar cosas (si, se que es una practica horrible de programacion y me siento sucio haciendo esto)
	private Vector3 lastCornerVertex;
	private int lastCornerVertexIndex;
	// Vertice esquina de la cara contraria
	private Vector3 lastCornerVertexMirrored;
	private int lastCornerVertexMirroredIndex;
	// Vertice de lado que se va a rotar
	private Vector3 lastSideVertex;
	private int lastSideVertexIndex;
	// Vertice de lado de la cara contraria
	private Vector3 lastSideVertexMirrored;
	private int lastSideVertexMirroredIndex;

	private Vector4 calcTangent(Vector3 normal) {

		Vector4 tangent1 = Vector3.Cross(normal, Vector3.up);
		Vector4 tangent2 = Vector3.Cross(normal, Vector3.up);
		return tangent1.magnitude > tangent2.magnitude ? tangent1 : tangent2;
	}
	private void AddTwig( Vector3 origin, float scale, Vector3 binormal, Vector3 cross) {
	
		int leafSegments = properties.leafSegments; 

		float leafParentBranchOffset = properties.leafParentBranchOffset;
		Vector3 binormalRotated = vecAxisAngle(binormal, cross, Mathf.PI / 2);
		Vector3 v1 = origin;
		Vector3 v2 = v1 + properties.leafDownwardTip * properties.leafCrossSectionRatio * cross + (binormalRotated) * (scale / this.properties.leafLengthWidthRatio * properties.leafCrossSectionRatio) + binormal * scale * properties.leafLengthWidthRatio * properties.leafCrossSectionRatio;
		Vector3 v3 = v1 + properties.leafDownwardTip * cross + binormal * (scale * this.properties.leafLengthWidthRatio);
		Vector3 v4 = v1 + properties.leafDownwardTip * properties.leafCrossSectionRatio * cross - (binormalRotated) * (scale / this.properties.leafLengthWidthRatio * properties.leafCrossSectionRatio) + binormal * scale * properties.leafLengthWidthRatio * properties.leafCrossSectionRatio;
	
		Vector3 normal = cross;		

		int vert1 = vertsTwig.Count;
		vertsTwig.Add(v1);
		int vert2 = vertsTwig.Count;
		vertsTwig.Add(v2);
		int vert3 = vertsTwig.Count;
		vertsTwig.Add(v3);
		int vert4 = vertsTwig.Count;
		vertsTwig.Add(v4);
		int vert8 = vertsTwig.Count;
		vertsTwig.Add(new Vector3(v1[0], v1[1], v1[2]));
		int vert7 = vertsTwig.Count;
		vertsTwig.Add(new Vector3(v2[0], v2[1], v2[2]));
		int vert6 = vertsTwig.Count;
		vertsTwig.Add(new Vector3(v3[0], v3[1], v3[2]));
		int vert5 = vertsTwig.Count;
		vertsTwig.Add(new Vector3(v4[0], v4[1], v4[2]));

		
		normal = Vector3.Normalize(Vector3.Cross(v3 - v1, v2 - v1));

		facesTwig.Add(new Vector3(vert1,vert2,vert3));
		facesTwig.Add(new Vector3(vert4,vert1,vert3));
		
		facesTwig.Add(new Vector3(vert6,vert7,vert8));
		facesTwig.Add(new Vector3(vert6,vert8,vert5));

		normal= Vector3.Normalize(Vector3.Cross(vertsTwig[vert1] - vertsTwig[vert3], vertsTwig[vert2] - vertsTwig[vert3]));
		var normal2= Vector3.Normalize(Vector3.Cross(vertsTwig[vert7] - vertsTwig[vert6],vertsTwig[vert8] - vertsTwig[vert6]));
		
		Vector4 tangent1 = calcTangent(normal);
		Vector4 tangent2 = calcTangent(normal2);
		
		normalsTwig.Add(normal);
		normalsTwig.Add(normal);
		normalsTwig.Add(normal);
		normalsTwig.Add(normal);

		normalsTwig.Add(normal2);
		normalsTwig.Add(normal2);
		normalsTwig.Add(normal2);
		normalsTwig.Add(normal2);

		tangentsTwig.Add(tangent1);
		tangentsTwig.Add(tangent1);
		tangentsTwig.Add(tangent1);
		tangentsTwig.Add(tangent1);
		tangentsTwig.Add(tangent2);
		tangentsTwig.Add(tangent2);
		tangentsTwig.Add(tangent2);
		tangentsTwig.Add(tangent2);
		
		uvsTwig.Add(new Vector2(0.5f,0));
		uvsTwig.Add(new Vector2(0,0.5f));
		uvsTwig.Add(new Vector2(0.5f,1f));
		uvsTwig.Add(new Vector2(1,0.5f));
		
		uvsTwig.Add(new Vector2(0.5f,0));
		uvsTwig.Add(new Vector2(0,0.5f));
		uvsTwig.Add(new Vector2(0.5f,1f));
		uvsTwig.Add(new Vector2(1,0.5f));

		// Multiples segmentos en la hoja
		// Codigo nuevo: mas segmentos de hoja!
		lastCornerVertex = v3;
		lastCornerVertexIndex = vert3;
		// Vertice esquina de la cara contraria
		lastCornerVertexMirrored = v3;
		lastCornerVertexMirroredIndex = vert6;
		// Vertice de lado que se va a rotar
		lastSideVertex = v2;
		lastSideVertexIndex = vert2;
		// Vertice de lado de la cara contraria
		Vector3 lastSideVertexMirrored = v2;
		lastSideVertexMirroredIndex = vert7;

		float cosAngle = Vector3.Dot(Vector3.Normalize(v2 - v1), Vector3.Normalize(v4 - v1));
		float angle = Mathf.Acos(cosAngle);
		
		// Por ahora asumimos numeros impares de segmentos..
		for (int i = 1; i < leafSegments; i+=2) {
			CreateLeafSegments(v1, vert1, cross, angle, normal, new Vector2(0,0.5f), false);
		}

		lastCornerVertex = v3;
		lastCornerVertexIndex = vert3;
		lastCornerVertexMirrored = v3;
		lastCornerVertexMirroredIndex = vert6;
		lastSideVertex = v4;
		lastSideVertexIndex = vert4;
		lastSideVertexMirrored = v4;
		lastSideVertexMirroredIndex = vert5;
		angle = -angle;

		for (int i = 2; i < leafSegments; i+=2) {
			CreateLeafSegments(v1, vert1, cross, angle, normal, new Vector2(0,0.5f), true);
		}		 
	}

	private void CreateLeafSegments(Vector3 v1, int v1Index, Vector3 tangent, float angle, Vector3 normal, Vector2 sideUvCoord, bool clockwise) {
		//primer triangulo extra
		lastCornerVertex = vecAxisAngle(lastCornerVertex - v1, tangent, angle) + v1;
		lastCornerVertexIndex = vertsTwig.Count;
		vertsTwig.Add(lastCornerVertex);
		Vector3 face = new Vector3(v1Index, lastCornerVertexIndex, lastSideVertexIndex );
		if (clockwise) face = new Vector3(face[0], face[2], face[1]);
		facesTwig.Add(face);
		normalsTwig.Add(normal);
		uvsTwig.Add(new Vector2(0.5f,1f));
		tangentsTwig.Add(calcTangent(normal));
		
		// Cara contraria
		lastCornerVertexMirrored = lastCornerVertex;
		lastCornerVertexMirroredIndex = vertsTwig.Count;
		vertsTwig.Add(lastCornerVertexMirrored);
		face = new Vector3(lastCornerVertexMirroredIndex, v1Index + 4, lastSideVertexMirroredIndex);
		if (clockwise) face = new Vector3(face[0], face[2], face[1]);
		facesTwig.Add(face);
		normalsTwig.Add(-normal);
		tangentsTwig.Add(calcTangent(-normal));
		uvsTwig.Add(new Vector2(0.5f,1f));

		// segundo triangulo extra
		lastSideVertex = vecAxisAngle(lastSideVertex - v1, tangent, angle) + v1;
		lastSideVertexIndex = vertsTwig.Count;
		vertsTwig.Add(lastSideVertex);
		face = new Vector3(lastSideVertexIndex, lastCornerVertexIndex, v1Index);
		if (clockwise) face = new Vector3(face[0], face[2], face[1]);
		facesTwig.Add(face);
		uvsTwig.Add(sideUvCoord);
		normalsTwig.Add(normal);
		tangentsTwig.Add(calcTangent(normal));

		// Cara contraria
		lastSideVertexMirrored = lastSideVertex;
		lastSideVertexMirroredIndex = vertsTwig.Count;
		vertsTwig.Add(lastSideVertex);
		face = new Vector3(lastCornerVertexMirroredIndex, lastSideVertexMirroredIndex, v1Index + 4);
		if (clockwise) face = new Vector3(face[0], face[2], face[1]);
		facesTwig.Add(face);
		uvsTwig.Add(sideUvCoord);
		normalsTwig.Add(-normal);
		tangentsTwig.Add(calcTangent(-normal));
	}
    public void DoFaces(Branch branch) {
		
        branch = branch ?? this.root;
		
        int segments = this.properties.segments;
		
		if (branch.parent == null) {
			for(int i=0; i < verts.Count; i++){ 
				UV.Add(new Vector2(0,0));
			}

			// for (int i=0; i < segments -1; i++) {			
			// 	int v1=branch.root[0];
			// 	int v2=branch.root[i];
			// 	int v3=branch.root[i+1];			
			// 	faces.Add(new Vector3(v1,v2,v3));
			// }

			Vector3 tangent = Vector3.Normalize(Vector3.Cross(branch.child0.head - branch.head, branch.child1.head - branch.head));
			Vector3 normal = Vector3.Normalize(branch.head);
			float angle = Mathf.Acos(Vector3.Dot(tangent, new Vector3(-1,0,0)));
            // Ok se que esto es espejar el angulo. Porque? pues obviamente porqueee el vector ortogonal al plano de x=-1 y la tangente, da la cara para afuera?
			if(Vector3.Dot(Vector3.Cross(new Vector3(-1,0,0), tangent), normal) > 0) angle = 2 * Mathf.PI - angle;

			int segOffset = (int) Mathf.Round((angle / Mathf.PI / 2 * segments));
			
			for (int i=0; i < segments; i++) {			
				int v1=branch.ring0[i];
				int v2=branch.root[mod((i+segOffset+1), segments)];
				int v3=branch.root[mod((i+segOffset), segments)];
				int v4=branch.ring0[mod((i+1), segments)];
				
				faces.Add(new Vector3(v1,v4,v3));
				faces.Add(new Vector3(v4,v2,v3));
				UV[mod((i+segOffset), segments)] = new Vector2(Mathf.Abs((float) i/(float) segments- 0.5f) * 2, 0);
				float len = Vector3.Magnitude(verts[branch.ring0[i]] - verts[branch.root[mod((i+segOffset), segments)]]) * this.properties.vMultiplier;
				UV[branch.ring0[i]] = new Vector2(Mathf.Abs((float) i/(float)segments-0.5f) * 2, len);
				UV[branch.ring2[i]] = new Vector2(Mathf.Abs((float) 1/(float)segments-0.5f) * 2, len);
			}
		}
		
		// Hacer Faces es definir 3 anillos por branch, cada uno con K puntos siendo K la cantidad de segmentos
		// El ring0 es la lista de vertices de la cual nace este branch, o esta conectada al branch parent
		// Ring1 y Ring2 son la lista de vertices que se definen para las uniones con el branch hijo
		if(branch.child0.ring0 != null) {
			int segOffset0 = 0, segOffset1 = -1;
			float match0 = 0f, match1 = 0f;
			
			Vector3 vector1 = Vector3.Normalize(verts[branch.ring1[0]] - branch.head);
			Vector3 vector2 = Vector3.Normalize(verts[branch.ring2[0]] - branch.head);
			
			vector1 = scaleInDirection(vector1, Vector3.Normalize(branch.child0.head - branch.head), 0);
			vector2 = scaleInDirection(vector2, Vector3.Normalize(branch.child1.head - branch.head), 0);
			
			for (int i = 0; i < segments; i++) {
				Vector3 d = Vector3.Normalize(verts[branch.child0.ring0[i]] - branch.child0.head);
				float l = Vector3.Dot(d, vector1);
				if (segOffset0 == -1 || l > match0){
					match0=l;
					segOffset0=segments-i;
				}
				d = Vector3.Normalize(verts[branch.child1.ring0[i]] - branch.child1.head);
				l = Vector3.Dot(d, vector2);
				if (segOffset1== -1 || l > match1){
					match1=l;
					segOffset1=segments-i;
				}
			}
			
			float UVScale=this.properties.maxRadius/branch.radius;			

			
			for (int i = 0; i < segments; i++) {
				int v1 = branch.child0.ring0[i];
				int v2 = branch.ring1[mod((i+segOffset0+1), segments)];
				int v3 = branch.ring1[mod((i+segOffset0), segments)];
				int v4 = branch.child0.ring0[mod((i+1), segments)];
				faces.Add(new Vector3(v1,v4,v3));
				faces.Add(new Vector3(v4,v2,v3));
				v1=branch.child1.ring0[i];
				v2=branch.ring2[mod( (i + segOffset1 + 1), segments)];
				v3=branch.ring2[mod( (i + segOffset1), segments)];
				v4=branch.child1.ring0[mod( (i+1), segments)];
				faces.Add(new Vector3(v1, v2, v3));
				faces.Add(new Vector3(v1, v4, v2));
				
				float len1 = Vector3.Magnitude(verts[branch.child0.ring0[i]] - verts[branch.ring1[mod((i+segOffset0), segments)]]) * UVScale;

				Vector2 uv1 = UV[branch.ring1[mod( (i + segOffset0 - 1), segments)]];
				
				UV[branch.child0.ring0[i]] = new Vector2(uv1[0], uv1[1] + len1 * this.properties.vMultiplier);
				UV[branch.child0.ring2[i]] = new Vector2(uv1[0], uv1[1] + len1 * this.properties.vMultiplier);
				
				float len2 = Vector3.Magnitude(verts[branch.child1.ring0[i]] - verts[branch.ring2[mod( (i + segOffset1), segments)]]) * UVScale;
				Vector2 uv2 = UV[branch.ring2[mod( (i+ segOffset1 - 1), segments)]];
				
				UV[branch.child1.ring0[i]] = new Vector2(uv2[0],uv2[1]+len2*this.properties.vMultiplier);
				UV[branch.child1.ring2[i]] = new Vector2(uv2[0],uv2[1]+len2*this.properties.vMultiplier);
			}

			DoFaces(branch.child0);
			DoFaces(branch.child1);
		} else {
			// Crea las caras que unen los anillos de ambos branches, con el end de cada child. Itera por cada segmento que define la circumferencia
			// de las Branches
			for(int i = 0; i < segments; i++){
				faces.Add(new Vector3(branch.child0.end.Value,branch.ring1[mod((i+1), segments)],branch.ring1[i]));
				faces.Add(new Vector3(branch.child1.end.Value,branch.ring2[mod((i+1), segments)],branch.ring2[i]));
				
				
				float len1 = Vector3.Magnitude(verts[branch.child0.end.Value] - verts[branch.ring1[i]]);
				UV[branch.child0.end.Value]= new Vector2(Mathf.Abs((float)i / (float) segments-1f-0.5f) * 2, len1 * this.properties.vMultiplier);
				float len2= Vector3.Magnitude(verts[branch.child1.end.Value] - verts[branch.ring2[i]]);
				UV[branch.child1.end.Value]= new Vector2(Mathf.Abs((float)i / (float) segments-0.5f) * 2, len2 * this.properties.vMultiplier);
			}
		}
	}

    public void CalcNormals() {
		IList<List<Vector3>> allNormals = new List<List<Vector3>>();

		for (int i = 0; i < verts.Count; i++) {
			allNormals.Add(new List<Vector3>());
		}
		for (int i = 0; i < faces.Count; i++) {
			Vector3 face = faces[i];
			Vector3 norm = Vector3.Normalize(Vector3.Cross(verts[(int) face[1]] - verts[(int) face[2]], verts[(int) face[1]] - verts[(int) face[0]]));		
			allNormals[(int) face[0]].Add(norm);
			allNormals[(int) face[1]].Add(norm);
			allNormals[(int) face[2]].Add(norm);
		}
		for (int i = 0; i < allNormals.Count; i++) {
			Vector3 total = new Vector3(0,0,0);
			int l = allNormals[i].Count;
			for (int j = 0; j < l; j++) {
				total= total + allNormals[i][j] * ((float)1/l);
			}
			normals.Add(total);
		}
	}

	int mod(int x, int m) {
    	return (x%m + m)%m;
	}
}
