using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectMenuController : MonoBehaviour
{

    public static LevelSelectMenuController Instance;

    [Header("View")]
    public ScrollRect ScrollView;
    private CanvasGroup scrollViewGroup;

    [Header("Containers")]
    public GameObject ContentContainer;

    [Header("Prefabs")]
    public LevelSelectItemController LevelSelectItemPrefab;

    private List<LevelSelectItemController> LevelSelectItems;

    // Use this for initialization
    void Start()
    {
        Instance = this;
        SpawnLevelSelectMenuItems();
        scrollViewGroup = ScrollView.GetComponent<CanvasGroup>();

        LevelManager.Instance.LevelCompleteEvent += LevelCompletedEvent;
    }

    void Update()
    {
        if (CameraController.Instance != null)
        {
            scrollViewGroup.alpha = CameraController.Instance.LocationCurveValue;
            if (CameraController.Instance.LocationCurveValue < 0.5f)
            {
                scrollViewGroup.blocksRaycasts = false;
                scrollViewGroup.interactable = false;
            }
            else
            {
                scrollViewGroup.blocksRaycasts = true;
                scrollViewGroup.interactable = false;
            }
        }
    }

    private void OnDestroy()
    {
        //TODO assert is not null
        LevelManager.Instance.LevelCompleteEvent -= LevelCompletedEvent;
    }

    private void SpawnLevelSelectMenuItems()
    {
        LevelSelectItems = new List<LevelSelectItemController>();

        foreach (var level in LevelProgressor.Instance.LevelCollection.Levels)
        {
            var levelSelectItem = Instantiate(LevelSelectItemPrefab, ContentContainer.transform);
            levelSelectItem.SetUpLevelSelectItem(level);
            LevelSelectItems.Add(levelSelectItem);
        }
    }

    private void LevelCompletedEvent()
    {
        foreach (var levelSelectItem in LevelSelectItems)
        {
            levelSelectItem.UpdateLevel();
        }
    }
}
