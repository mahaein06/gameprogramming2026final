using UnityEngine;
using UnityEngine.SceneManagement;

public class DogSelectingScript : MonoBehaviour
{
    private void OnMouseDown()
    {
        SceneManager.LoadScene("DogScene");
    }
}
