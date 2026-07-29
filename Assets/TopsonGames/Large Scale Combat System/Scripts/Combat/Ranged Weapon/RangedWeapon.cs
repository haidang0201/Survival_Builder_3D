using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace TopsonGames
{
    public class RangedWeapon : MonoBehaviour
    {
        public GameObject Weapon;
        public Animator animator;
        public Transform ShootPoint;
        public RangedWeaponSO rangedWeapon;
        public Projectile[] Arrows;
        private int arrowIndex = 0;

        public UnityEvent OnShoot;

        public Projectile GetNextArrow()
        {
            if (Arrows == null || Arrows.Length == 0)
            {
                return null;
            }

            Projectile arrow = Arrows[arrowIndex];
            arrowIndex = (arrowIndex + 1) % Arrows.Length;

            arrow.transform.position = ShootPoint.position;
            arrow.transform.rotation = Quaternion.identity;
            arrow.gameObject.SetActive(true);
            arrow.rigidBody.linearVelocity = Vector3.zero;
            arrow.rigidBody.angularVelocity = Vector3.zero;

            return arrow;
        }

        public void Attack(Unit currentTarget, Formation targetformation, Formation parentFormation)
        {
            if (currentTarget == null || targetformation == null)
                return;
            rangedWeapon.Attack(this, currentTarget, targetformation, parentFormation);
            OnShoot.Invoke();


        }

       
    }
}
