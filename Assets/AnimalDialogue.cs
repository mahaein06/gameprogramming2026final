using UnityEngine;

public class AnimalDialogue : MonoBehaviour
{
    [Header("Dialogue Data")]
    public string animalName = "Chicken";
    public Sprite portrait;

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
