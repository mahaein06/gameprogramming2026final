using UnityEngine;
using UnityEngine.SceneManagement;

public class TigerSelectingScript : MonoBehaviour
{
    private void OnMouseDown()
    {
        CharacterSelectionState.Select(SelectableAnimal.Tiger);
        SceneManager.LoadScene("GameScene");
    }
}
