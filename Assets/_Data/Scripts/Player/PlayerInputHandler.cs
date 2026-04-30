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
        // Thêm biến Scroll để lấy dữ liệu con lăn chuột
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
        }

        void Update()
        {
            // Đọc giá trị cuộn chuột trực tiếp từ Input System mỗi Frame
            // .y trả về > 0 khi cuộn lên, < 0 khi cuộn xuống
            Scroll = Mouse.current.scroll.ReadValue().y;
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