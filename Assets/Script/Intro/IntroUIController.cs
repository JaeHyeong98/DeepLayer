using UnityEngine;
using UnityEngine.UI;

public class IntroUIController : MonoBehaviour
{
    private Transform intro;
    private GameObject introBtns;
    private GameObject gameStartBtns;
    private GameObject gameJoinPopup;
    private InputField sessionCode;
    

    private void Awake()
    {
        intro = transform.Find("Intro");
        introBtns = intro.Find("IntroBtns").gameObject;
        gameStartBtns = intro.Find("GameStartBtns").gameObject;
        gameJoinPopup = intro.Find("GameJoinPopup").gameObject;
        sessionCode = gameJoinPopup.transform.Find("InputField (Legacy)").GetComponent<InputField>();

        Init();
        
    }

    private void Init() // UI Active Init
    {
        introBtns.SetActive(true);
        gameStartBtns.SetActive(false);
        gameJoinPopup.SetActive(false);

    }

    public void IntroStartBtn() // StartBtn Onclick
    {
        introBtns.SetActive(false);
        gameStartBtns.SetActive(true);
    }

    public void GameStart()
    {
        transform.gameObject.SetActive(false);
        GSC.sceneCon.MainSceneOpen();
    }

    public void JoinBtn() // JoinBtn Onclick
    {
        gameJoinPopup.SetActive(true);

    }

    public void SessionJoin() // JoinPopup
    {
        Debug.Log(sessionCode.text);

        if (sessionCode.text.Equals("start"))
            GameStart();
        else
        {

        }
    }
}
