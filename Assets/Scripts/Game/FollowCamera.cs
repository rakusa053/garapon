using UnityEngine;

namespace Garapon.Game
{
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 11f, -9f);
        [SerializeField] float smoothTime = 0.15f;

        Vector3 velocity;

        void LateUpdate()
        {
            transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref velocity, smoothTime);
            transform.rotation = Quaternion.LookRotation(-offset);
        }
    }
}
