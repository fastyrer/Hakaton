using UnityEngine;

namespace Hakaton.LocationKit
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LocationShelter : MonoBehaviour
    {
        void Reset()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0, 1.5f, 0);
            box.size = new Vector3(4, 3, 4);
        }

        void OnValidate() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>();
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.3f, 0.75f, 1, 0.12f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.3f, 0.75f, 1, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
