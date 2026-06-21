using UnityEngine;

public class AnimalDialogue : MonoBehaviour
{
    [Header("Dialogue Data")]
    public string animalName = "Chicken";
    public Sprite portrait;

    [Header("Prompt")]
    public Transform promptTarget;
    public Vector3 promptOffset = new Vector3(0f, 1.8f, 0f);

    public Vector3 GetPromptWorldPosition()
    {
        Transform target = promptTarget;
        if (target == null && transform.parent != null)
        {
            target = transform.parent;
        }

        if (target == null)
        {
            target = transform;
        }

        return target.position + promptOffset;
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

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowPrompt(this);
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
