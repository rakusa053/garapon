using Garapon.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Garapon.Game
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        const float BaseSpeed = 6f;
        const float InteractReach = 3f;

        [SerializeField] InputActionAsset actions;
        [SerializeField] Transform cameraTransform;

        CharacterController controller;
        InputActionMap playerMap;
        InputAction moveAction;
        InputAction interactAction;
        bool interactWasHeld;

        public Interactable Target { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerMap = actions.FindActionMap("Player", true);
            moveAction = playerMap.FindAction("Move", true);
            interactAction = playerMap.FindAction("Interact", true);
        }

        void OnEnable()
        {
            playerMap.Enable();
        }

        void OnDisable()
        {
            playerMap.Disable();
        }

        void Update()
        {
            Move();
            Target = Interactable.FindNearest(transform.position, InteractReach);

            var interactHeld = interactAction.IsPressed();
            if (Target != null && interactHeld && !interactWasHeld)
            {
                Target.Interact();
            }

            interactWasHeld = interactHeld;
        }

        void Move()
        {
            var input = moveAction.ReadValue<Vector2>();
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            var direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);

            var speed = BaseSpeed * (1 + (float)GameSession.Instance.Upgrades.GetValue(UpgradeStat.MoveSpeed));
            controller.SimpleMove(direction * speed);
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}
