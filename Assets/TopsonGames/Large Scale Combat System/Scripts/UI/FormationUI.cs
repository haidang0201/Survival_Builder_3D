namespace TopsonGames
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class FormationUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Button selectButton;
        public GameObject selectedImage;
        public Image unitIcon;
        public Image unitTypeIcon;
        public Image strengthSlider;
        public Image moraleSlider;
        public TextMeshProUGUI projectiles;
        public Button freeShooting;
        public GameObject freeShootingActive;
        public GameObject freeShootingInactive;
        public Image[] effectIcons;
        [HideInInspector]
        public Formation assignedFormation;

        bool isFreeShooting = true;
        public void SetUp(Formation formation)
        {
            assignedFormation = formation;
            unitIcon.sprite = formation.UnitData.unitIcon;
            unitTypeIcon.sprite = formation.UnitData.unitType.typeIcon;
            if (formation.UnitData.Arrows <= 0)
            {
                projectiles.gameObject.SetActive(false);
                freeShooting.gameObject.SetActive(false);
            }
            else
                projectiles.text = formation.UnitData.Arrows.ToString();

            if (assignedFormation.armyData.Troops > 0)
            {
                float strength = 0;
                foreach (var unit in assignedFormation.GetUnits())
                {
                    if (unit != null)
                        strength += unit.GetCurrentHealth();
                }
                strengthSlider.fillAmount = strength / (assignedFormation.UnitData.Troops * assignedFormation.UnitData.maxHealth);
                moraleSlider.fillAmount = formation.currentMorale / formation.maxMorale;
            }

            freeShooting.onClick.AddListener(OnChangeFreeShooting);
            selectButton.onClick.AddListener(SelectFormation);
        }

        public void OnUpdateUI()
        {
            if (assignedFormation == null || assignedFormation.gameObject == null)
            {
                UiManager.Instance.RemoveFormationUI(this);
                return;
            }
            var activeUnits = assignedFormation.GetCachedUnits();
            if (activeUnits.Count == 0)
            {
                UiManager.Instance.RemoveFormationUI(this);
                return;
            }

            // Update Archer UI
            if (assignedFormation.UnitData.Arrows <= 0)
            {
                projectiles.gameObject.SetActive(false);
                freeShooting.gameObject.SetActive(false);
            }
            else
                projectiles.text = assignedFormation.currentArrows.ToString();

            // Update Effects
            for (int i = 0; i < effectIcons.Length; i++)
                foreach (var icon in effectIcons)
                {
                    icon.gameObject.SetActive(false);
                }
            int count = Mathf.Min(effectIcons.Length, assignedFormation.Effects.Count);

            for (int i = 0; i < count; i++)
            {
                if (assignedFormation.Effects[i] != null)
                {
                    effectIcons[i].sprite = assignedFormation.Effects[i].EffectIcon;
                    effectIcons[i].gameObject.SetActive(true);
                }
            }

            // Update Healthbar Slider
            float currentTotalHealth = 0;
            foreach (var unit in activeUnits)
            {
                if (unit != null)
                    currentTotalHealth += unit.GetCurrentHealth();
            }

            float maxTotalHealth = assignedFormation.UnitData.Troops * assignedFormation.UnitData.maxHealth;
            // Prevent division by 0
            if (maxTotalHealth > 0)
                strengthSlider.fillAmount = currentTotalHealth / maxTotalHealth;
            else
                strengthSlider.fillAmount = 0;
            moraleSlider.fillAmount = assignedFormation.currentMorale / assignedFormation.maxMorale;
        }
        public void OnChangeFreeShooting()
        {
            isFreeShooting = !isFreeShooting;
            freeShootingActive.SetActive(isFreeShooting);
            freeShootingInactive.SetActive(!isFreeShooting);

            if (assignedFormation)
                assignedFormation.ChangeFreeShooting(isFreeShooting);
            else
                UiManager.Instance.RemoveFormationUI(this);
        }
        public void OnSelect(bool select)
        {
            selectedImage.SetActive(select);
        }
        public void SelectFormation()
        {
            UiManager.Instance.OnFormationUIClicked(this);
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            UiManager.Instance.OnHoverUI(true, assignedFormation);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            UiManager.Instance.OnHoverUI(false, assignedFormation);
        }
    }
}