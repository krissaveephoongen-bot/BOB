// Phase 2 of the twin-island kitbash (docs 24 section 15.3): a flat-normal anchor on the curved backdrop wall.
// The scene builder puts five of them on the tall half of the 135-degree wall; a stage can swap the prop in front of
// a socket without moving the wall. The socket's +Z points away from the wall, into the field.
using UnityEngine;

namespace Bob.TaskRace
{
    [DisallowMultipleComponent]
    public class WallMountSocket : MonoBehaviour
    {
        [Tooltip("1 = left (west) end of the wall as the field sees it, 5 = right (east) end.")]
        [Range(1, 8)] public int index = 1;
        [Tooltip("Prefab name under Resources/Props that sits on this socket, e.g. SM_tv_flat.")]
        public string prop = "";
        [Tooltip("Where the wall surface is relative to the socket, in the socket's own frame.")]
        public Vector3 intoWall = new Vector3(0f, 0f, -1f);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.55f, 0.25f, 0.9f);
            Gizmos.DrawWireCube(transform.position, new Vector3(0.45f, 0.45f, 0.08f));
            Gizmos.color = new Color(0.3f, 0.85f, 0.95f, 0.9f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.6f);
        }
    }
}
