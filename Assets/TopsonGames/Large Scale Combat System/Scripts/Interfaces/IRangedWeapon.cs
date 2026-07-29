namespace TopsonGames
{
    using UnityEngine;

    public interface IRangedWeapon
    {
        void Attack(RangedWeapon rangedWeapon, Unit currentTarget, Formation formation, Formation parentFormation);
    }
}