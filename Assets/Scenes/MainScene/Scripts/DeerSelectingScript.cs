using UnityEngine;
using UnityEngine.SceneManagement;

public class DeerSelectingScript : MonoBehaviour
{
    private void OnMouseDown()
    {
        SceneManager.LoadScene("DeerScene");
    }
}
