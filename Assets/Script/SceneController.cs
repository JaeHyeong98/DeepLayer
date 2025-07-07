using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    private void Awake()
    {
        GSC.sceneCon = this;
    }

    public void MainSceneOpen()
    {
        SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
    }
}
