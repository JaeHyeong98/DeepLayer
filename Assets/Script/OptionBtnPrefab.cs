using UnityEngine;
using UnityEngine.UI;

public class OptionBtnPrefab : MonoBehaviour
{
    private enum options
    {
        check,
        rangeN,
        rangeP,
        index
    };

    [Header("Essential")]
    [SerializeField]
    private Text text_;
    [SerializeField]
    private Image bg;
    [SerializeField]
    private Toggle toggle; 
    [SerializeField]
    private Slider slider;
    [SerializeField]
    private GameObject index;

    [Header("Option")]
    [SerializeField]
    private string text;
    [SerializeField]
    private Color textColor;
    [SerializeField]
    private Image bgImg;
    [SerializeField]
    private Color bgColor;
    [SerializeField]
    private options btnOption;

    private int preBtnOption;


    void OnValidate()
    {
        UpdateState();
    }

    private void UpdateState()
    {
        bg.sprite = bgImg.sprite;
        bg.color = bgColor;
        text_.color = textColor;
        text_.text = text;

        switch(preBtnOption)
        {
            case 0:
                toggle.gameObject.SetActive(false);
                break;
            case 1:
                slider.gameObject.SetActive(false);
                break;
            case 2:
                index.SetActive(false);
                break;
        }

        switch(btnOption)
        {
            case options.check:
                preBtnOption = 0;
                toggle.gameObject.SetActive(true);
                break;
            case options.rangeN:
                preBtnOption = 1;
                slider.gameObject.SetActive(true);
                break;
            case options.rangeP:
                preBtnOption = 1;
                slider.gameObject.SetActive(true);
                break;
            case options.index:
                preBtnOption = 2;
                index.SetActive(true);
                break;
        }
    }


}
