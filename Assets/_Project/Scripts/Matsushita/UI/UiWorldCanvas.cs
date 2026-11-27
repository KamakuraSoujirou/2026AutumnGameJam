using UnityEngine;

/// <summary>
/// 画面の一番上（Canvas）に付ける。カメラに映す形にして、UI の後ろに 2D オブジェクト（UiWorldSprite）を置けるようにする
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UiWorldCanvas : MonoBehaviour
{
    private const float PlaneDistance = 10f;

    [SerializeField, Tooltip("UI を映すカメラ。空なら MainCamera を探す")] private Camera _camera;
    [SerializeField, Tooltip("UI 全体の描画順。2D オブジェクトの描画順より大きくする")] private int _sortingOrder = 100;

    /// <summary>
    /// カメラが見つかれば、Canvas をそのカメラに映す形にする（見つからなければ今のまま）
    /// </summary>
    private void Awake()
    {
        Camera target = _camera != null ? _camera : Camera.main;

        if (target == null)
        {
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = target;
        canvas.planeDistance = PlaneDistance;
        canvas.sortingOrder = _sortingOrder;
    }
}
