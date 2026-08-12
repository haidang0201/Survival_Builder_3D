using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace TopsonGames
{
    public class FlagController : MonoBehaviour
    {
        [Header("World Space UI (Optional)")]
        [Tooltip("Should this flag display UI?")]
        [SerializeField] private bool showWorldSpaceUI = true;
        [SerializeField] private GameObject healthBarContainer;
        [SerializeField] private Transform healthBarFill;
        [SerializeField] private GameObject moraleBarContainer;
        [SerializeField] private Transform moraleBarFill;
        [SerializeField] private SpriteRenderer[] effectIcons;

        private Formation parentFormation;
        private float lastHealthPercentage = -1f;
        private float lastMoralePercentage = -1f;

        private void OnEnable()
        {
            if (FlagManager.Instance != null)
            {
                FlagManager.Instance.RegisterFlag(this);
            }

            parentFormation = GetComponentInParent<Formation>();

            bool uiReady = showWorldSpaceUI &&
                           parentFormation != null &&
                           healthBarContainer != null &&
                           healthBarFill != null &&
                           moraleBarContainer != null &&
                           moraleBarFill != null;

            if (showWorldSpaceUI)
            {
                if (!uiReady)
                {
                    showWorldSpaceUI = false;
                    if (healthBarContainer) healthBarContainer.SetActive(false);
                    if (moraleBarContainer) moraleBarContainer.SetActive(false);
                }
                else
                {
                    healthBarContainer.SetActive(true);
                    moraleBarContainer.SetActive(true);
                }
            }
        }

        private void OnDestroy()
        {
            if (FlagManager.Instance != null)
            {
                FlagManager.Instance.UnregisterFlag(this);
            }
        }

        private void LateUpdate()
        {
            if (!showWorldSpaceUI || parentFormation == null)
            {
                return;
            }

            // Check ob die Formation noch existiert/lebt
            if (parentFormation.armyData == null || parentFormation.armyData.Troops <= 0)
            {
                healthBarContainer.SetActive(false);
                moraleBarContainer.SetActive(false);
                return;
            }

            healthBarContainer.SetActive(true);
            moraleBarContainer.SetActive(true);

            // --- HEALTH LOGIC ---
            float currentHealth = 0;
            var units = parentFormation.GetUnits();
            if (units == null) return;

            foreach (var unit in units)
            {
                if (unit != null)
                    currentHealth += unit.GetCurrentHealth();
            }

            float maxHealth = parentFormation.UnitData.Troops * parentFormation.UnitData.maxHealth;
            float healthPercentage = (maxHealth > 0) ? currentHealth / maxHealth : 0f;

            if (!Mathf.Approximately(healthPercentage, lastHealthPercentage))
            {
                healthBarFill.localScale = new Vector3(healthPercentage, 1f, 1f);
                lastHealthPercentage = healthPercentage;
            }
            float currentMorale = parentFormation.currentMorale;
            float maxMorale = parentFormation.maxMorale;
            float moralePercentage = (maxMorale > 0) ? currentMorale / maxMorale : 0f;

            if (!Mathf.Approximately(moralePercentage, lastMoralePercentage))
            {
                moraleBarFill.localScale = new Vector3(moralePercentage, 1f, 1f);
                lastMoralePercentage = moralePercentage;
            }
            for (int i = 0; i < effectIcons.Length; i++)
            {
                effectIcons[i].gameObject.SetActive(false);
            }

            int count = Mathf.Min(effectIcons.Length, parentFormation.Effects.Count);
            for (int i = 0; i < count; i++)
            {
                if (parentFormation.Effects[i] != null)
                {
                    effectIcons[i].sprite = parentFormation.Effects[i].EffectIcon;
                    effectIcons[i].gameObject.SetActive(true);
                }
            }
        }
    }
}