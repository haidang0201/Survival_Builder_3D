using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BuildingTimer : MonoBehaviour
{
    [Header("Cài đặt thời gian")]
    public float buildTime = 5f; 
    
    [Header("Mô hình & Hiệu ứng")]
    public GameObject scaffoldingMesh; 
    public GameObject finishedMesh; 
    public ParticleSystem dustVFX; 
    
    [Header("UI")]
    public Image progressBar; 
    public GameObject uiCanvas; 

    void Start()
    {
        SetupInitialState();
        StartConstruction();
    }

    // --- CÁC HÀM KHỞI TẠO VÀ VÒNG LẶP ---

    private void SetupInitialState()
    {
        scaffoldingMesh.SetActive(true);
        finishedMesh.SetActive(false);
    }

    private void StartConstruction()
    {
        StartCoroutine(BuildRoutine());
    }

    private IEnumerator BuildRoutine()
    {
        float currentTime = 0f;
        
        while (currentTime < buildTime)
        {
            currentTime += Time.deltaTime;
            UpdateProgressBar(currentTime);
            yield return null;
        }

        FinishConstruction();
    }

    // --- CÁC HÀM XỬ LÝ LOGIC CHI TIẾT ---

    private void UpdateProgressBar(float currentTime)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = currentTime / buildTime;
        }
    }

    private void FinishConstruction()
    {
        PlayVFX();
        SwapToFinishedMesh();
        HideUI();
    }

    private void PlayVFX()
    {
        if (dustVFX != null) 
        {
            dustVFX.Play();
        }
    }

    private void SwapToFinishedMesh()
    {
        scaffoldingMesh.SetActive(false);
        finishedMesh.SetActive(true);
    }

    private void HideUI()
    {
        if (uiCanvas != null) 
        {
            uiCanvas.SetActive(false);
        }
    }
}