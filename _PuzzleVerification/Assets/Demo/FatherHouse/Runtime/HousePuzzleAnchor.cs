using UnityEngine;
namespace Hakaton.Demo.FatherHouse
{
    public sealed class HousePuzzleAnchor : MonoBehaviour
    {
        public string puzzleId;
        [TextArea]public string design;
        void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(1,.7f,.2f,.7f);Gizmos.DrawWireCube(transform.position,Vector3.one*.4f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position+Vector3.up*.3f,puzzleId);
#endif
        }
    }
}
