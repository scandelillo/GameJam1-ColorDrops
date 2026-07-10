using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
   public void NextScene(string sceneName)
    {
        Debug.Log("¡El botón funciona! Intentando cargar: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
