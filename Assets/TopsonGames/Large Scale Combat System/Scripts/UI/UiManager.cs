namespace TopsonGames
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class UiManager : MonoBehaviour
    {
        public static UiManager Instance;
        [SerializeField] Image strenghtSlider;
        [SerializeField] GameObject FormationUI;
        [SerializeField] Transform FormationUIParent;
        [SerializeField] HoverUI hoverUI;
        public EffectSO[] effectIcons;

        [Header("Debugging")]
        [SerializeField] List<FormationUI> activeFormationUI;

        private FormationUI lastSelectedUI;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);
        }
        public void SetUp()
        {
            activeFormationUI.Clear();
            foreach (var formation in FormationController.instance.GetFormations())
            {
                if (formation.TeamID == FormationController.instance.TeamID)
                {
                    var ui = GameObject.Instantiate(FormationUI, FormationUIParent).GetComponent<FormationUI>();
                    ui.SetUp(formation);
                    activeFormationUI.Add(ui);
                }
            }
        }
        private void LateUpdate()
        {
            UpdateStrengthSlider();
            UpdateFormationUI();
        }
        void UpdateStrengthSlider()
        {
            int totalStrength = GameManager.instance.GetPlayerStrength() + GameManager.instance.GetEnemyStrength();

            if (totalStrength == 0)
            {
                strenghtSlider.fillAmount = 0.5f;
                return;
            }
            strenghtSlider.fillAmount = Mathf.Lerp(strenghtSlider.fillAmount, (float)GameManager.instance.GetPlayerStrength() / totalStrength, Time.deltaTime * 2f);
        }
        void UpdateFormationUI()
        {
            var selected = FormationController.instance.GetSelectedFormations();
            if (selected == null || activeFormationUI == null || activeFormationUI.Count == 0) 
                return;

            for(int i = 0; i< activeFormationUI.Count; i++)
            {
                var formationUI = activeFormationUI[i];
                if (formationUI != null)
                {
                    if (formationUI.assignedFormation == null)
                    {
                        if (activeFormationUI.Contains(formationUI) || formationUI.assignedFormation.gameObject.activeSelf == false)
                        {
                            activeFormationUI.Remove(formationUI);
                            Destroy(formationUI.gameObject);
                            i--;
                            continue;
                        }
                    }
                    
                    if (selected.Contains(formationUI.assignedFormation))
                        formationUI.OnSelect(true);
                    else
                        formationUI.OnSelect(false);

                    formationUI.OnUpdateUI();
                }
            }
        }

        public void OnFormationUIClicked(FormationUI clickedUI)
        {
            if(clickedUI.assignedFormation == null)
            {
                if(activeFormationUI.Contains(clickedUI))
                {
                    activeFormationUI.Remove(clickedUI);
                    Destroy(clickedUI.gameObject);
                    return;
                }    
            }
            else if (clickedUI.assignedFormation.gameObject.activeSelf == false)
            {

                if (activeFormationUI.Contains(clickedUI))
                {
                    activeFormationUI.Remove(clickedUI);
                    Destroy(clickedUI.gameObject);
                }
            }
            bool isMultiSelectPressed = FormationController.instance.multiSelectAction.action.IsPressed();
            var selectedFormations = FormationController.instance.GetSelectedFormations();
            bool isCustomMultiSelectPressed = FormationController.instance.UiCustomMultiSelectAction.action.IsPressed();

            if (isMultiSelectPressed)
            {
                if (selectedFormations != null && selectedFormations.Count > 0 && lastSelectedUI != null)
                {
                    int clickedIndex = activeFormationUI.IndexOf(clickedUI);
                    int lastSelectedIndex = activeFormationUI.IndexOf(lastSelectedUI);

                    int startIndex = Mathf.Min(clickedIndex, lastSelectedIndex);
                    int endIndex = Mathf.Max(clickedIndex, lastSelectedIndex);

                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        FormationController.instance.AddSelectedFormation(activeFormationUI[i].assignedFormation);
                    }
                }
                else
                {
                    FormationController.instance.AddSelectedFormation(clickedUI.assignedFormation);
                }
            }
            else if(isCustomMultiSelectPressed) 
            {
                FormationController.instance.AddSelectedFormation(clickedUI.assignedFormation);
            }
            else
            {
                FormationController.instance.ClearSelectedFormations();
                FormationController.instance.AddSelectedFormation(clickedUI.assignedFormation);
            }
            lastSelectedUI = clickedUI;
        }
        public void RemoveFormationUI(FormationUI formationUI)
        {
            if(activeFormationUI.Contains(formationUI))
            {
                activeFormationUI.Remove(formationUI);
                Destroy(formationUI.gameObject);
            }

        }
        public void OnHoverUI(bool show, Formation formation)
        {
            if (hoverUI == null)
                return;

            if(show)
            {
                if (formation == null)
                    return;

                var Unitdata = formation.UnitData;
                hoverUI.health.text = Unitdata.maxHealth.ToString();
                hoverUI.armor.text = Unitdata.Armor.ToString();
                hoverUI.baseDamge.text = Unitdata.baseDamage.ToString();
                hoverUI.piercingDamage.text = Unitdata.armorPenetration.ToString();
                hoverUI.unitName.text = Unitdata.name.ToString();
                hoverUI.unitDescription.text = Unitdata.description.ToString();
                hoverUI.unitType.sprite = Unitdata.unitType.typeIcon;
                hoverUI.chargeDamage.text = (Unitdata.baseDamage * Unitdata.chargeDamageMultiplier).ToString();

                if (formation.armyData.Troops > 0)
                {
                    float strength = 0;
                    foreach (var unit in formation.GetUnits())
                    {
                        if (unit != null)
                            strength += unit.GetCurrentHealth();
                    }
                    hoverUI.Healthbar.fillAmount = strength / (formation.UnitData.Troops * formation.UnitData.maxHealth);
                    hoverUI.Moralebar.fillAmount = formation.currentMorale / formation.maxMorale;
                }

                foreach (var goodagainst in hoverUI.goodAgainst)
                    goodagainst.gameObject.SetActive(false);
                foreach (var badagainst in hoverUI.badAgainst)
                    badagainst.gameObject.SetActive(false);

                var good = GameManager.instance.damageModifier.GetGoodAgainst(Unitdata.unitType);
                var bad = GameManager.instance.damageModifier.GetBadAgainst(Unitdata.unitType);

                for (int i = 0; i< good.Count; i++)
                {
                    hoverUI.goodAgainst[i].sprite = good[i].typeIcon;
                    hoverUI.goodAgainst[i].gameObject.SetActive(true);
                }
                for (int i = 0; i < bad.Count; i++)
                {
                    hoverUI.badAgainst[i].sprite = bad[i].typeIcon;
                    hoverUI.badAgainst[i].gameObject.SetActive(true);
                }
                for (int i = 0; i < hoverUI.effects.Length; i++)
                {
                    if (formation.Effects != null && i < formation.Effects.Count && formation.Effects[i] != null)
                    {
                        hoverUI.effects[i].sprite = formation.Effects[i].EffectIcon;
                        hoverUI.effects[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        hoverUI.effects[i].gameObject.SetActive(false);
                    }
                }

                hoverUI.gameObject.SetActive(true);
            }
            else
                hoverUI.gameObject.SetActive(false);
        }
        public EffectSO GetEffect(EffectType effectType)
        {
            foreach(var e in effectIcons)
            {
                if (e.effectType == effectType)
                    return e;
            }
            return null;
        }
    }
}