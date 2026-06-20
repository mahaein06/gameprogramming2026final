using UnityEngine;
using UnityEngine.SceneManagement;

public class TigerSelectingScript : MonoBehaviour
{
    private void OnMouseDown()
    {
        SceneManager.LoadScene("TigerScene");
    }
}
