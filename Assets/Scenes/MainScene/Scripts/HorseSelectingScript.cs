using UnityEngine;
using UnityEngine.SceneManagement;

public class HorseSelectingScript : MonoBehaviour
{
    private void OnMouseDown()
    {
        CharacterSelectionState.Select(SelectableAnimal.Horse);
        SceneManager.LoadScene("GameScene");
    }
}
