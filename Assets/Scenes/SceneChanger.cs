using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    private void OnMouseDown()
    {
        SceneManager.LoadScene("CharacterSelectScene");
    }
}
