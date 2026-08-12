namespace TopsonGames.AI
{
    using UnityEngine;

    public class SiegeChokePoint : MonoBehaviour
    {
        [Tooltip("How many formations fit side by side here? (e.g., 1 for narrow alley, 3 for main street)")]
        public int capacity = 1;

        [Tooltip("Is this access intended for attackers or defenders?")]
        public bool isAttackerEntry = true;

        [Header("Runtime")]
        public int currentUserCount = 0;

        private void OnEnable() => currentUserCount = 0;

        public bool HasCapacity()
        {
            return currentUserCount < capacity;
        }

        public void RegisterUser()
        {
            currentUserCount++;
        }

        public void ResetUsage()
        {
            currentUserCount = 0;
        }

        private void OnDrawGizmos()
        {
            var mn = FindAnyObjectByType<SiegeManager>();
            if (mn != null && mn.DrawZoneGizmos == false)
                return;

            Gizmos.color = isAttackerEntry ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, 1f * capacity);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
    }
}