using StarterAssets;
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
    private Transform info;
    private Transform toggleGroup;
    private Transform gatheringKey;
    private Transform inventory;

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
        info = playerUI.Find("Info");
        toggleGroup = info.Find("ToggleGroup");
        toggleGroup.GetChild(0).GetComponent<Toggle>().isOn = true;
        gatheringKey = playerUI.Find("GatheringKey");
        inventory = playerUI.Find("Inventory");

        info.gameObject.SetActive(false);
        gatheringKey.gameObject.SetActive(false);
        inventory.gameObject.SetActive(false);

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
                //inventory.gameObject.SetActive(true);
                toggleGroup.GetChild(2).GetComponent<Toggle>().isOn = true;
                break;

            case InfoState.Map:
                toggleGroup.GetChild(3).GetComponent<Toggle>().isOn = true;
                break;

            default:
                toggleGroup.GetChild(4).GetComponent<Toggle>().isOn = true;
                break;
        }
        info.gameObject.SetActive(_input.info);
        if(_input.info)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void GatheringKeyActive(bool val)
    {
        if (gatheringKey.gameObject.activeSelf && !val)
            gatheringKey.gameObject.SetActive(false);
        else if(!gatheringKey.gameObject.activeSelf && val)
            gatheringKey.gameObject.SetActive(val);
    }

    public void GatheringItem()
    {

    }
}
