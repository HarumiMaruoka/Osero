// 日本語対応
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class CurrentUserTextPresenter : MonoBehaviour
{
    [SerializeField]
    private InterpersonalGameSystem _inGameSystem = default;
    [SerializeField]
    private Text _text = default;

    private void Awake()
    {
        _inGameSystem.CurrentUser.Subscribe(value =>
        {
            if (value == InterpersonalGameSystem.UserColor.Black)
            {
                _text.text = "現在 黒 のターン";
            }
            else if (value == InterpersonalGameSystem.UserColor.White)
            {
                _text.text = "現在 白 のターン";
            }
        });
    }
}
