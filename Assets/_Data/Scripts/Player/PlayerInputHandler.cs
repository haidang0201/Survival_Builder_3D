using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerInputActions input;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Interact { get; private set; }

        void Awake()
        {
            input = new PlayerInputActions();
        }

        void OnEnable()
        {
            input.Enable();

            input.Player.Move.performed += ctx => Move = ctx.ReadValue<Vector2>();
            input.Player.Move.canceled += ctx => Move = Vector2.zero;

            input.Player.Look.performed += ctx => Look = ctx.ReadValue<Vector2>();
            input.Player.Look.canceled += ctx => Look = Vector2.zero;

            input.Player.Interact.performed += ctx => Interact = true;
        }

        void LateUpdate()
        {
            Interact = false;
        }

        void OnDisable()
        {
            input.Disable();
        }
    }
}