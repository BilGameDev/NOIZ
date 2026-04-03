using Hanzzz.MeshSlicerFree;
using UnityEngine;

public class NoteSlicer : MonoBehaviour
{
    public static NoteSlicer Instance;

    [SerializeField] private Material cutMaterial;
    [SerializeField] private float splitForce = 2.5f;
    [SerializeField] private float upwardForce = 1f;
    [SerializeField] private float torqueForce = 5f;
    [SerializeField] private string debrisLayerName = "Default";

    private MeshSlicer meshSlicer;

    private void Awake()
    {
        Instance = this;
        meshSlicer = new MeshSlicer();
    }

    public bool Slice(NoteMover note, Vector3 swipeWorldStart, Vector3 swipeWorldEnd, Vector3 swipeWorldDir)
    {
        if (note == null)
            return false;

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("No main camera found");
            return false;
        }

        Vector3 planeNormal = Vector3.Cross(swipeWorldDir, cam.transform.forward).normalized;

        if (planeNormal.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("Plane normal too small");
            return false;
        }

        Vector3 planePoint = note.transform.position;
        Vector3 planeTangent = Vector3.Cross(planeNormal, cam.transform.forward).normalized;

        Vector3 p1 = planePoint;
        Vector3 p2 = planePoint + planeTangent;
        Vector3 p3 = planePoint + cam.transform.forward;

        var result = meshSlicer.Slice(note.gameObject, (p1, p2, p3), cutMaterial);

        if (result.Item1 == null || result.Item2 == null)
        {
            Debug.Log("Slice failed or plane did not intersect mesh");
            return false;
        }

        PrepareHalf(result.Item1, -planeNormal);
        PrepareHalf(result.Item2, planeNormal);

        return true;
    }

    private void PrepareHalf(GameObject half, Vector3 dir)
    {
        if (half == null)
            return;

        NoteMover mover = half.GetComponent<NoteMover>();
        if (mover != null) Destroy(mover);

        NoteVisual visual = half.GetComponent<NoteVisual>();
        if (visual != null) Destroy(visual);

        Collider[] oldColliders = half.GetComponents<Collider>();
        for (int i = 0; i < oldColliders.Length; i++)
        {
            Destroy(oldColliders[i]);
        }

        MeshCollider col = half.GetComponent<MeshCollider>();
        if (col == null)
            col = half.AddComponent<MeshCollider>();

        col.convex = true;

        Rigidbody rb = half.GetComponent<Rigidbody>();
        if (rb == null)
            rb = half.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        int debrisLayer = LayerMask.NameToLayer(debrisLayerName);
        if (debrisLayer >= 0)
            half.layer = debrisLayer;

        rb.AddForce((dir * splitForce) + Vector3.up * upwardForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);

        Destroy(half, 2f);
    }
}