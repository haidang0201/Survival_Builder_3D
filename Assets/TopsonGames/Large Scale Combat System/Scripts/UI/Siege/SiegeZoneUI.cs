namespace TopsonGames.UI
{
    using UnityEngine;
    using UnityEngine.UI;
    using TopsonGames.AI;

    public class SiegeZoneUI : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("The zone to which this UI belongs.")]
        public SiegeCaptureZone zone;
        [Tooltip("Height above ground.")]
        public float heightOffset = 8f;

        [Header("UI Elements")]
        public Image iconImage;
        public Image progressImage;
        public Image backgroundImage;

        [Header("Colors")]
        public Color friendlyColor = new Color(0.2f, 0.6f, 1f); 
        public Color enemyColor = new Color(0.9f, 0.1f, 0.1f);   
        public Color neutralColor = Color.gray;

        private Camera mainCam;
        private int playerTeamID = -1;

        void Start()
        {
            mainCam = Camera.main;
            if (zone == null) zone = GetComponentInParent<SiegeCaptureZone>();
            if (FormationController.instance != null) playerTeamID = FormationController.instance.TeamID;
            transform.SetParent(null);
        }

        void LateUpdate()
        {
            if (zone == null || !zone.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            if (mainCam != null)
            {
                transform.position = zone.transform.position + Vector3.up * heightOffset;
                Vector3 cameraDirection = mainCam.transform.forward;
                cameraDirection.y = 0;
                if (cameraDirection != Vector3.zero) transform.rotation = Quaternion.LookRotation(cameraDirection);
            }

            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            int owner = zone.GetControllingTeam();
            int attackerTeamID = SiegeManager.instance.attackerTeamID;
            int defenderTeamID = SiegeManager.instance.defenderTeamID;

            Color ownerColor;
            if (owner == -1) ownerColor = neutralColor;
            else ownerColor = (owner == playerTeamID) ? friendlyColor : enemyColor;

            iconImage.color = ownerColor;
            backgroundImage.color = ownerColor;

            var status = zone.GetStatus();
            float progressRaw = zone.GetCaptureProgressNormalized(); // 0 (Def) 1 (Att)

            progressImage.fillAmount = 0f;

            if (status == SiegeCaptureZone.CaptureStatus.CapturingAttacker)
            {
                Color attackerColorVis = (attackerTeamID == playerTeamID) ? friendlyColor : enemyColor;
                progressImage.color = attackerColorVis;
                progressImage.fillAmount = progressRaw;
            }
            else if (status == SiegeCaptureZone.CaptureStatus.CapturingDefender)
            {
                Color defenderColorVis = (defenderTeamID == playerTeamID) ? friendlyColor : enemyColor;
                progressImage.color = defenderColorVis;
                progressImage.fillAmount = 1f - progressRaw; 
            }
        }
    }
}