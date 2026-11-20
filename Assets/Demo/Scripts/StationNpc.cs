using UnityEngine;

namespace Hakaton.Demo
{
    [SelectionBase]
    public sealed class StationNpc : MonoBehaviour
    {
        public string role;
        public Transform head, leftArm, rightArm, leftLeg, rightLeg;
        public Vector3[] route;
        public float speed = 0.7f, phase;
        public bool seated;
        int waypoint;
        void Update()
        {
            bool walking = route != null && route.Length > 1;
            float t = Time.time + phase;
            if (walking)
            {
                Vector3 destination = route[waypoint];
                Vector3 direction = destination - transform.position;
                if (direction.sqrMagnitude < 0.04f) waypoint = (waypoint + 1) % route.Length;
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 130 * Time.deltaTime);
                }
            }
            if (head != null) head.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.45f) * 9, 0);
            float swing = walking ? Mathf.Sin(t * 5.5f) * 22 : Mathf.Sin(t * 0.9f) * 3;
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(seated ? -65 : swing, 0, 0);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(seated ? -62 : -swing, 0, 0);
            if (!seated)
            {
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(swing, 0, 0);
            }
        }
    }
}
