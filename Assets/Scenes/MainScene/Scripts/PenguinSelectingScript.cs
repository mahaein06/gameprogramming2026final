using UnityEngine;
using UnityEngine.SceneManagement;

public class PenguinSelectingScript : MonoBehaviour
{
    private void OnMouseDown()
    {
        SceneManager.LoadScene("PenguinScene");
    }
}
