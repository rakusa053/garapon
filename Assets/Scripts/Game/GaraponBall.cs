using UnityEngine;

namespace Garapon.Game
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GaraponBall : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public Renderer BallRenderer { get; private set; }

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            BallRenderer = GetComponent<Renderer>();
        }
    }
}
