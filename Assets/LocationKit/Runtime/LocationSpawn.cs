using UnityEngine;

namespace Hakaton.LocationKit
{
    public sealed class LocationSpawn : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.75f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.15f, 0.25f);
            Vector3 p = transform.position + Vector3.up * 0.25f;
            Vector3 tip = p + transform.forward * 1.2f;
            Gizmos.DrawLine(p, tip);
            Gizmos.DrawLine(tip, tip - transform.forward * 0.3f + transform.right * 0.2f);
            Gizmos.DrawLine(tip, tip - transform.forward * 0.3f - transform.right * 0.2f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, "СТАРТ ИГРОКА");
#endif
        }
    }
}
