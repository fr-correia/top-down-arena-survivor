using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaSurvivor.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovementComponent : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private Rigidbody2D rb;
        private PlayerMovement playerMovement;
        private InputAction moveAction;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerMovement = new PlayerMovement();

            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
        }

        private void OnEnable()
        {
            moveAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
        }

        private void FixedUpdate()
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            Vector2 nextPosition = playerMovement.ComputeNextPosition(rb.position, input, moveSpeed, Time.fixedDeltaTime);
            rb.MovePosition(nextPosition);
        }

        public void IncreaseSpeed(float amount)
        {
            moveSpeed += amount;
        }
    }
}
