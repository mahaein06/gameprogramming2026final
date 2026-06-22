using UnityEngine;
using UnityEngine.SceneManagement;

public class DeerSelectingScript : MonoBehaviour
{
    private void OnMouseDown()
    {
        CharacterSelectionState.Select(SelectableAnimal.Deer);
        SceneManager.LoadScene("GameScene");
    }
}
