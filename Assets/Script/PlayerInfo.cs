using StarterAssets;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum InfoState
{
    Info,
    Skill,
    Inventory,
    Map
}

public class PlayerInfo: MonoBehaviour
{
    private PlayerInput _playerInput;
    private CharacterController _controller;
    private StarterAssetsInputs _input;

    [SerializeField]
    private Transform playerUI;
    private Transform toggleGroup;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<StarterAssetsInputs>();
        _playerInput = GetComponent<PlayerInput>();
        playerUI = transform.Find("PlayerUI");
        toggleGroup = playerUI.Find("ToggleGroup");
        toggleGroup.GetChild(0).GetComponent<Toggle>().isOn = true;
        playerUI.gameObject.SetActive(false);

        StarterAssetsInputs.OnInfoKey += Info;
    }

    private void Info()
    {
        switch(_input.infoState)
        {
            case InfoState.Info:
                toggleGroup.GetChild(0).GetComponent<Toggle>().isOn = true;
                break;

            case InfoState.Skill:
                toggleGroup.GetChild(1).GetComponent<Toggle>().isOn = true;
                break;

            case InfoState.Inventory:
                toggleGroup.GetChild(2).GetComponent<Toggle>().isOn = true;
                break;

            case InfoState.Map:
                toggleGroup.GetChild(3).GetComponent<Toggle>().isOn = true;
                break;

            default:
                toggleGroup.GetChild(4).GetComponent<Toggle>().isOn = true;
                break;
        }
        playerUI.gameObject.SetActive(_input.info);

    }


}
