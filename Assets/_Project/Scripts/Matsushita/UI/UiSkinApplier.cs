using UnityEngine;

/// <summary>
/// 画面の一番上に付ける。子にある素材とフォントの差し込み口へ、UiSkin の中身を配る
/// </summary>
public class UiSkinApplier : MonoBehaviour
{
    [SerializeField, Tooltip("素材の表")] private UiSkin _skin;

    /// <summary>
    /// 素材の表
    /// </summary>
    public UiSkin Skin => _skin;

    /// <summary>
    /// 起動したときに素材を配る
    /// </summary>
    private void Awake()
    {
        ApplyAll();
    }

    /// <summary>
    /// 子にある差し込み口すべてに、素材とフォントを配る
    /// </summary>
    public void ApplyAll()
    {
        foreach (UiArtSlot slot in GetComponentsInChildren<UiArtSlot>(true))
        {
            slot.Apply(_skin);
        }

        foreach (UiFontSlot slot in GetComponentsInChildren<UiFontSlot>(true))
        {
            slot.Apply(_skin);
        }
    }
}
