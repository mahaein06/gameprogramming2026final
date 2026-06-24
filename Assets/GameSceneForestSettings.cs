using UnityEngine;

public class GameSceneForestSettings : MonoBehaviour
{
    [Header("Tree Amount")]
    public int seed = 20260624;
    public int targetTreeCount = 180;
    public int clusterCount = 9;

    [Header("Spacing")]
    public float minTreeSpacing = 6.5f;
    public float terrainEdgeMargin = 5f;
    public float houseAvoidRadius = 6f;
    public float animalStartAvoidRadius = 1f;

    [Header("Tree Scale")]
    public float minHeightScale = 0.55f;
    public float maxHeightScale = 0.9f;
    public float minWidthScale = 0.55f;
    public float maxWidthScale = 0.9f;

    [Header("Near Animals")]
    public float nearAnimalRadius = 22f;
    public float nearAnimalMaxHeightScale = 0.7f;
}



