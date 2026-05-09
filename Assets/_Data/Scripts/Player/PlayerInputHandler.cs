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
        public bool Chop { get; private set; }   // thêm
        public float Scroll { get; private set; }

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

            // CHOP INPUT
            input.Player.Chop.performed += ctx => Chop = true;
        }

        void Update()
        {
            Scroll = Mouse.current.scroll.ReadValue().y;
        }

        void LateUpdate()
        {
            Interact = false;
            Chop = false; // reset mỗi frame
        }

        void OnDisable()
        {
            input.Disable();
        }
    }
}