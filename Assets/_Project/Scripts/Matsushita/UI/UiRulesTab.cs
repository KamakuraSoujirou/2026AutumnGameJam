using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// タイトル画面の「ルール」タブ。画面の右端から飛び出すパネルで、開いている間は後ろを暗くする。
/// 閉じているときにタブへマウスを乗せると、少しだけ顔を出す
/// </summary>
public class UiRulesTab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float FollowSpeed = 20f;
    private const float PeekDistance = 22f;
    private const float SnapDistance = 0.1f;

    [SerializeField, Tooltip("飛び出すパネル（タブはこの子）。左上を基準に置く")] private RectTransform _drawer;
    [SerializeField, Tooltip("後ろを暗くする幕。開いているときだけクリックを受けて、閉じる")] private CanvasGroup _dim;
    [SerializeField, Tooltip("タブのボタン（押すと開閉）")] private Button _tabButton;
    [SerializeField, Tooltip("暗い幕のボタン（押すと閉じる）")] private Button _dimButton;
    [SerializeField, Tooltip("閉じているときのパネルの左端の位置（画面の右端の外）")] private float _closedX = 1920f;
    [SerializeField, Tooltip("開いているときのパネルの左端の位置")] private float _openX = 1200f;

    private bool _isOpen;
    private bool _isHovered;

    /// <summary>
    /// ルールを開いているか
    /// </summary>
    public bool IsOpen => _isOpen;

    /// <summary>
    /// ボタンの押下を受け取れるようにして、閉じた位置に置く
    /// </summary>
    private void Awake()
    {
        _tabButton.onClick.AddListener(OnTabClicked);
        _dimButton.onClick.AddListener(Close);
        ApplyPosition(_closedX);
    }

    /// <summary>
    /// パネルを目標の位置へ滑らかに近づける（時間が止まっていても動くよう、止まらない時間で進める）
    /// </summary>
    private void Update()
    {
        float currentX = _drawer.anchoredPosition.x;
        float targetX = GetTargetX();

        if (Mathf.Approximately(currentX, targetX))
        {
            return;
        }

        float nextX = Mathf.Lerp(currentX, targetX, 1f - Mathf.Exp(-FollowSpeed * Time.unscaledDeltaTime));

        if (Mathf.Abs(nextX - targetX) < SnapDistance)
        {
            nextX = targetX;
        }

        ApplyPosition(nextX);
    }

    /// <summary>
    /// ルールを開く
    /// </summary>
    public void Open()
    {
        _isOpen = true;
        _dim.blocksRaycasts = true;
    }

    /// <summary>
    /// ルールを閉じる
    /// </summary>
    public void Close()
    {
        _isOpen = false;
        _dim.blocksRaycasts = false;
        UiEventSystem.ClearSelection();
    }

    /// <summary>
    /// 開いていれば閉じ、閉じていれば開く
    /// </summary>
    public void Toggle()
    {
        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    /// <summary>
    /// マウスが乗った。閉じているときだけ、タブが少し顔を出す
    /// </summary>
    /// <param name="eventData">ポインターの情報</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
    }

    /// <summary>
    /// マウスが外れた
    /// </summary>
    /// <param name="eventData">ポインターの情報</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
    }

    /// <summary>
    /// タブが押されたので開閉する
    /// </summary>
    private void OnTabClicked()
    {
        Toggle();
        UiEventSystem.ClearSelection();
    }

    /// <summary>
    /// パネルが今向かうべき左端の位置を返す
    /// </summary>
    /// <returns>開いていれば開いた位置、閉じていれば閉じた位置（マウスが乗っていれば少し手前）</returns>
    private float GetTargetX()
    {
        if (_isOpen)
        {
            return _openX;
        }

        return _isHovered ? _closedX - PeekDistance : _closedX;
    }

    /// <summary>
    /// パネルを動かし、位置に合わせて幕の濃さを決める
    /// </summary>
    /// <param name="x">パネルの左端の位置</param>
    private void ApplyPosition(float x)
    {
        Vector2 position = _drawer.anchoredPosition;
        position.x = x;
        _drawer.anchoredPosition = position;
        _dim.alpha = Mathf.InverseLerp(_closedX - PeekDistance, _openX, x);
    }
}
