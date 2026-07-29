namespace TopsonGames.UI
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using TopsonGames.AI;

    public class SiegeGlobalUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The image that fills up (e.g., the red attacker bar). Must be Image Type = Filled.")]
        public Image balanceFillImage;

        [Tooltip("The background image (e.g., the blue defender bar).")]
        public Image backgroundImage;

        public TextMeshProUGUI timerText;
        public TextMeshProUGUI statusText;

        [Header("Colors")]
        public Color attackerColor = new Color(0.8f, 0.1f, 0.1f); 
        public Color defenderColor = new Color(0.1f, 0.1f, 0.8f);

        [Header("Settings")]
        public float lerpSpeed = 2.0f;
        public string defenderOnTop = "Defender on top";
        public string attackerOnTop = "Attacker gains ground";
        public string contested = "Contested";

        void Start()
        {
            if (balanceFillImage != null)
                balanceFillImage.color = attackerColor;

            if (backgroundImage != null)
                backgroundImage.color = defenderColor;
        }

        void Update()
        {
            if (SiegeManager.instance == null) return;

            float currentPoints = SiegeManager.instance.GetCurrentAttackerScore();
            float maxPoints = SiegeManager.instance.GetPointsToWin();

            float targetFillAmount = Mathf.Clamp01(currentPoints / maxPoints);

            if (balanceFillImage != null)
            {
                balanceFillImage.fillAmount = Mathf.Lerp(balanceFillImage.fillAmount, targetFillAmount, Time.deltaTime * lerpSpeed);
            }

            if (timerText != null)
            {
                float timeRemaining = SiegeManager.instance.GetBattleTimer();
                timeRemaining = Mathf.Max(0, timeRemaining);

                int minutes = Mathf.FloorToInt(timeRemaining / 60F);
                int seconds = Mathf.FloorToInt(timeRemaining - minutes * 60);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            if (statusText != null)
            {
                float ptsPerSec = SiegeManager.instance.GetCurrentPointsPerSecond();

                if (ptsPerSec > 0.1f)
                    statusText.text = attackerOnTop;
                else if (ptsPerSec < -0.1f)
                    statusText.text = defenderOnTop;
                else
                    statusText.text = contested;
            }
        }
    }
}