namespace TopsonGames
{
    using UnityEngine;

    public abstract class RangedWeaponSO : ScriptableObject, IRangedWeapon
    {
        public abstract void Attack(RangedWeapon rangedWeapon, Unit currentTarget, Formation formation, Formation parentFormation);
    }

}