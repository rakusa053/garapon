using UnityEngine;

namespace Garapon.Game
{
    public sealed class ExitSensor : MonoBehaviour
    {
        [SerializeField] GaraponMachine machine;

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<GaraponBall>(out var ball))
            {
                machine.OnBallExited(ball);
            }
        }
    }
}
