using UnityEngine;

namespace Hakaton.LocationKit
{
    public enum KitCategory { Architecture, Props, Exterior, Lights, Gameplay }

    [DisallowMultipleComponent]
    public sealed class LocationPiece : MonoBehaviour
    {
        [HideInInspector] public string kitId;
        [HideInInspector] public KitCategory category;
        [HideInInspector] public Bounds localBounds;
        [Tooltip("Поверхности, которые перекрашивает конструктор. Материалы самого префаба не меняются.")]
        public Renderer[] paintTargets;
    }
}
