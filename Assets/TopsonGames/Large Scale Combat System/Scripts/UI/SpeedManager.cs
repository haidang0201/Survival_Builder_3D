namespace TopsonGames
{
    using UnityEngine;

    public class SpeedManager : MonoBehaviour
    {
        public static SpeedManager Instance { get; private set; }

        [SerializeField] private float normalSpeed = 1.0f;
        [SerializeField] private float slowSpeed = 0.5f;
        [SerializeField] private float fastSpeed = 2.0f;
        [SerializeField] private float superFastSpeed = 4.0f;

        private float _currentSpeed = 1.0f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        void Start()
        {
            SetSpeed(normalSpeed);
        }

        public float GetCurrentSpeed()
        {
            return _currentSpeed;
        }

        public void SetSpeed(float newSpeed)
        {
            _currentSpeed = Mathf.Max(0.01f, newSpeed); 
            Time.timeScale = _currentSpeed;
        }

        public void SetNormalSpeed() 
        { 
            SetSpeed(normalSpeed); 
        }
        public void SetSlowSpeed() 
        { 
            SetSpeed(slowSpeed); 
        }
        public void SetFastSpeed() 
        { 
            SetSpeed(fastSpeed); 
        }
        public void SetSuperFastSpeed() 
        { 
            SetSpeed(superFastSpeed); 
        } 
    }
}