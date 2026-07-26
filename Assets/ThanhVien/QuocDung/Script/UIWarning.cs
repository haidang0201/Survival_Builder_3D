using UnityEngine;

public class UIWarning : UIEnemyWaveButton
{
    public void Initialize(Vector3 warningPos)
    {
        transform.position = warningPos;
    }
}
