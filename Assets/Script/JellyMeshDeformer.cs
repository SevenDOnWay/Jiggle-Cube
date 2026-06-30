using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class JellyMeshDeformer : MonoBehaviour {

    [Header("Source")]
    [SerializeField] Transform movementRoot;

    [Header("Spring")]
    [SerializeField] float velocityBend = 0.025f;
    [SerializeField] float stiffness = 45f;
    [SerializeField] float damping = 7f;
    [SerializeField] float maxBend = 0.35f;

    [Header("Shape")]
    [SerializeField] float heightFalloff = 1.4f;
    [SerializeField] float squashFromBend = 0.25f;
    [SerializeField] float maxSquash = 0.18f;
    [SerializeField] bool recalculateNormals = true;

    Mesh meshInstance;
    Vector3[] baseVertices;
    Vector3[] deformedVertices;
    Bounds baseBounds;

    Vector3 previousRootPosition;
    Vector3 bend;
    Vector3 bendVelocity;

    void Start() {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        meshInstance = Instantiate(meshFilter.sharedMesh);
        meshInstance.name = $"{meshFilter.sharedMesh.name} Jelly Instance";
        meshFilter.sharedMesh = meshInstance;

        baseVertices = meshInstance.vertices;
        deformedVertices = new Vector3[baseVertices.Length];
        baseBounds = meshInstance.bounds;

        if(movementRoot == null) {
            movementRoot = transform.parent != null ? transform.parent : transform;
        }

        previousRootPosition = movementRoot.position;
    }

    void LateUpdate() {
        float deltaTime = Time.deltaTime;

        if(deltaTime <= 0f || movementRoot == null) {
            return;
        }

        Vector3 worldVelocity = (movementRoot.position - previousRootPosition) / deltaTime;
        previousRootPosition = movementRoot.position;

        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);
        Vector3 targetBend = -localVelocity * velocityBend;
        targetBend = Vector3.ClampMagnitude(targetBend, maxBend);

        UpdateSpring(targetBend, deltaTime);
        DeformMesh();
    }

    public void AddWorldImpulse(Vector3 worldImpulse) {
        Vector3 localImpulse = transform.InverseTransformDirection(worldImpulse);
        bendVelocity += localImpulse;
    }

    void UpdateSpring(Vector3 targetBend, float deltaTime) {
        Vector3 springForce = (targetBend - bend) * stiffness;
        bendVelocity += springForce * deltaTime;
        bendVelocity *= Mathf.Exp(-damping * deltaTime);
        bend += bendVelocity * deltaTime;
        bend = Vector3.ClampMagnitude(bend, maxBend);
    }

    void DeformMesh() {
        float squash = Mathf.Clamp(bend.magnitude * squashFromBend, 0f, maxSquash);

        for(int i = 0; i < baseVertices.Length; i++) {
            Vector3 vertex = baseVertices[i];

            float height01 = Mathf.InverseLerp(baseBounds.max.z, baseBounds.min.z, vertex.z);
            float bendWeight = Mathf.Pow(height01, heightFalloff);

            Vector3 fromCenter = vertex - baseBounds.center;
            Vector3 squashedVertex = baseBounds.center + new Vector3(
                fromCenter.x * (1f + squash),
                fromCenter.y * (1f - squash),
                fromCenter.z * (1f + squash)
            );

            deformedVertices[i] = squashedVertex + bend * bendWeight;
        }

        meshInstance.vertices = deformedVertices;

        if(recalculateNormals) {
            meshInstance.RecalculateNormals();
        }

        meshInstance.RecalculateBounds();
    }

    void OnDestroy() {
        if(meshInstance != null) {
            Destroy(meshInstance);
        }
    }
}
