using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ひし形ボタンへのマウスの重なり・押下を UiWire に渡す。
/// Button と同じオブジェクトに付ける（Button が先にイベントを受け取るので、親の UiWire では受けられないため）
/// </summary>
public class UiWirePointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private UiWire _wire;

    /// <summary>
    /// 渡し先の配線（親にある）を取得しておく
    /// </summary>
    private void Awake()
    {
        _wire = GetComponentInParent<UiWire>();
    }

    /// <summary>
    /// マウスが重なった
    /// </summary>
    /// <param name="eventData">マウスの情報</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _wire.PointerEntered();
    }

    /// <summary>
    /// マウスが外れた
    /// </summary>
    /// <param name="eventData">マウスの情報</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        _wire.PointerExited();
    }

    /// <summary>
    /// マウスの左ボタンを押した
    /// </summary>
    /// <param name="eventData">マウスの情報</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _wire.PointerPressed();
        }
    }

    /// <summary>
    /// マウスの左ボタンを離した
    /// </summary>
    /// <param name="eventData">マウスの情報</param>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _wire.PointerReleased();
        }
    }
}
