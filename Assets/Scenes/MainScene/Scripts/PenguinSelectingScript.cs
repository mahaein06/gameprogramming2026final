using UnityEngine;
using UnityEngine.SceneManagement;

public class PenguinSelectingScript : MonoBehaviour
{
    private void OnMouseDown()
    {
        CharacterSelectionState.Select(SelectableAnimal.Penguin);
        SceneManager.LoadScene("GameScene");
    }
}
