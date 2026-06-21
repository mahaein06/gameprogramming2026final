using UnityEngine;

public class AnimalDialogue : MonoBehaviour
{
    [Header("Dialogue Data")]
    public string animalName = "Chicken";
    public Sprite portrait;

    [Header("Prompt")]
    public Vector3 promptOffset = new Vector3(0f, 1.8f, 0f);

    public Vector3 GetPromptWorldPosition()
    {
        return transform.position + promptOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowPrompt(this);
        }
        else
        {
            Debug.LogWarning("AnimalDialogue: DialogueManager instance was not found.", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.HidePrompt(this);
        }
    }
}
