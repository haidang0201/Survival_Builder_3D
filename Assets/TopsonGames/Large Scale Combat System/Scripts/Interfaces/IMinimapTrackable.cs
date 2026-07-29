using UnityEngine;

namespace TopsonGames.Minimap
{
    public interface IMinimapTrackable
    {
        Vector3 GetWorldPosition();
        float GetWorldRotationY();
        bool IsVisibleOnMap();
        int GetTeamID();
        Sprite GetMinimapIcon();
    }
}