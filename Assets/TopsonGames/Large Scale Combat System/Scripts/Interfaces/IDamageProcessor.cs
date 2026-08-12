namespace TopsonGames
{
    public interface IDamageProcessor
    {
        float ProcessDamage(float rawDamage, Unit attacker, bool shieldHit = false);
    }
}