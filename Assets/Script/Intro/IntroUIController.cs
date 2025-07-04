using UnityEngine;
using UnityEngine.UI;

public class IntroUIController : MonoBehaviour
{
    private Transform intro;
    private GameObject introBtns;
    private GameObject gameStartBtns;

    private void Awake()
    {
        intro = transform.Find("Intro");
        introBtns = intro.Find("IntroBtns").gameObject;
        gameStartBtns = intro.Find("GameStartBtns").gameObject;
    }

    private void IntroStartBtn()
    {
        introBtns.SetActive(false);
        gameStartBtns.SetActive(true);
    }
}
