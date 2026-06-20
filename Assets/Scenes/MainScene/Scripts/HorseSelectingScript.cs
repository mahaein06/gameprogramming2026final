using UnityEngine;
using UnityEngine.SceneManagement;

public class HorseSelectingScript : MonoBehaviour
{
    private void OnMouseDown()
    {
        SceneManager.LoadScene("HorseScene");
    }
}
