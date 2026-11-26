using TMPro;
using UnityEngine;

/// <summary>
/// 文字のフォントの差し込み口。UiSkin のフォントを使い、なければ実行中だけPCの日本語フォントを使う
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class UiFontSlot : MonoBehaviour
{
    [SerializeField, Tooltip("文字の役割")] private UiFontRole _role = UiFontRole.Body;

    /// <summary>
    /// 文字の役割
    /// </summary>
    public UiFontRole Role => _role;

    /// <summary>
    /// UiSkin からフォントを受け取って差し替える
    /// </summary>
    /// <param name="skin">素材の表</param>
    public void Apply(UiSkin skin)
    {
        TMP_FontAsset font = skin != null ? skin.GetFont(_role) : null;

        if (font == null && Application.isPlaying)
        {
            font = UiOsFont.GetJapaneseFont();
        }

        if (font != null)
        {
            GetComponent<TMP_Text>().font = font;
        }
    }
}
