namespace TopsonGames.MeshAnimationSystem.Demo
{
    using UnityEngine;
    using UnityEngine.Events;

    public class EventReciever : MonoBehaviour
    {
        public UnityEvent ValueZero, ValueOne;
        public void PlayEvent(int value)
        {
            if(value == 0)
                ValueZero.Invoke();
            else
                ValueOne.Invoke();
        }
    }
}
