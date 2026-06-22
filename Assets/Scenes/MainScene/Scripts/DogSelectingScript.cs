using UnityEngine;
using UnityEngine.SceneManagement;

public class DogSelectingScript : MonoBehaviour
{
    private void OnMouseDown()
    {
        CharacterSelectionState.Select(SelectableAnimal.Dog);
        SceneManager.LoadScene("GameScene");
    }
}
