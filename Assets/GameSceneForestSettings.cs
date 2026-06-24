using UnityEngine;

public class GameSceneForestSettings : MonoBehaviour
{
    [Header("Tree Amount")]
    public int seed = 20260624;
    public int targetTreeCount = 90;
    public int clusterCount = 6;

    [Header("Spacing")]
    public float minTreeSpacing = 8f;
    public float terrainEdgeMargin = 5f;
    public float houseAvoidRadius = 18f;
    public float animalStartAvoidRadius = 10f;

    [Header("Tree Scale")]
    public float minHeightScale = 0.55f;
    public float maxHeightScale = 0.9f;
    public float minWidthScale = 0.75f;
    public float maxWidthScale = 1.05f;

    [Header("Near Animals")]
    public float nearAnimalRadius = 22f;
    public float nearAnimalMaxHeightScale = 0.7f;
}
