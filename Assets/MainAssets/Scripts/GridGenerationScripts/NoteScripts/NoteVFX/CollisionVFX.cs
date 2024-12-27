using UnityEngine;

public class CollisionVFX : MonoBehaviour
{
    [Header("VFX Settings")]
    public GameObject vfxPrefab;
    public float destroyDelay = 2f;
    public Transform vfxParent;
    public float minCollisionSpeed = 5f; // Minimum collision speed to trigger VFX
    public float vfxScale = 1f; // Scale of the VFX prefab

    private Vector3? lastCollisionPoint;

    private void OnCollisionEnter(Collision collision)
    {
        // Calculate relative velocity
        float collisionSpeed = collision.relativeVelocity.magnitude;

        if (collisionSpeed >= minCollisionSpeed)
        {
            ContactPoint contact = collision.GetContact(0);
            Vector3 collisionPoint = contact.point;

            if (vfxPrefab != null)
            {
                Transform parent = vfxParent != null ? vfxParent : null;
                GameObject vfxInstance = Instantiate(vfxPrefab, collisionPoint, Quaternion.identity, parent);

                // Set the scale of the VFX instance
                vfxInstance.transform.localScale = Vector3.one * vfxScale;

                Destroy(vfxInstance, destroyDelay);
            }
            else
            {
                Debug.LogError("VFX prefab not assigned! Please assign a VFX prefab in the Inspector.");
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        ContactPoint contact = collision.GetContact(0);
        lastCollisionPoint = contact.point;
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && lastCollisionPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lastCollisionPoint.Value, 0.1f);
        }
    }
}