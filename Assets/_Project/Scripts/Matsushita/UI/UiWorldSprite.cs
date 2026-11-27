using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI の四角（UiArtSlot）の位置と大きさに合わせて、同じ画像を 2D オブジェクト（SpriteRenderer）として出す。
/// 素材があるときだけ 2D オブジェクトにして、UI 側の画像は隠す。Canvas が UiWorldCanvas でカメラに映っているときだけ働く
/// </summary>
[RequireComponent(typeof(UiArtSlot))]
public class UiWorldSprite : MonoBehaviour
{
    private const string WorldRootName = "UiWorld";

    private static Transform _worldRoot;
    private static Sprite _solidSprite;

    [SerializeField, Tooltip("2D オブジェクトの描画順（大きいほど手前。UI より奥になる）")] private int _sortingOrder;
    [SerializeField, Tooltip("素材がないときも、仮の色の四角を 2D オブジェクトとして出す（背景用）")] private bool _isSolidWhenEmpty;

    private UiArtSlot _slot;
    private Image _image;
    private RectTransform _rect;
    private Canvas _rootCanvas;
    private SpriteRenderer _renderer;
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>
    /// 2D オブジェクトとして出ているか
    /// </summary>
    public bool IsWorldActive => _renderer != null && _renderer.gameObject.activeSelf;

    /// <summary>
    /// 2D オブジェクトの本体（まだ出ていなければ null）
    /// </summary>
    public SpriteRenderer WorldRenderer => _renderer;

    /// <summary>
    /// 自分の部品を取得しておく
    /// </summary>
    private void Awake()
    {
        _slot = GetComponent<UiArtSlot>();
        _image = GetComponent<Image>();
        _rect = (RectTransform)transform;
    }

    /// <summary>
    /// 隠れたら 2D オブジェクトも隠し、UI の画像を元に戻す
    /// </summary>
    private void OnDisable()
    {
        Release();
    }

    /// <summary>
    /// 作った 2D オブジェクトを片付ける
    /// </summary>
    private void OnDestroy()
    {
        if (_renderer != null)
        {
            Destroy(_renderer.gameObject);
        }
    }

    /// <summary>
    /// 毎フレーム、UI の位置・大きさ・色に 2D オブジェクトを合わせる
    /// </summary>
    private void LateUpdate()
    {
        if (!TryGetSprite(out Sprite sprite, out Color color))
        {
            Release();
            return;
        }

        EnsureRenderer();
        _image.enabled = false;
        _renderer.gameObject.SetActive(true);
        _renderer.sprite = sprite;
        _renderer.color = color;
        _renderer.sortingOrder = _sortingOrder;
        FollowRect(sprite);
    }

    /// <summary>
    /// 2D オブジェクトにして出す画像を決める
    /// </summary>
    /// <param name="sprite">出す画像</param>
    /// <param name="color">かける色</param>
    /// <returns>2D オブジェクトにするなら true</returns>
    private bool TryGetSprite(out Sprite sprite, out Color color)
    {
        sprite = null;
        color = Color.white;

        if (_rootCanvas == null)
        {
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        }

        if (_rootCanvas == null || _rootCanvas.renderMode != RenderMode.ScreenSpaceCamera || _rootCanvas.worldCamera == null)
        {
            return false;
        }

        if (_slot.HasArt)
        {
            sprite = _image.sprite;
            color = _slot.ArtColor;
            return sprite != null;
        }

        if (!_isSolidWhenEmpty)
        {
            return false;
        }

        sprite = GetSolidSprite();
        color = _slot.PlaceholderColor;
        return true;
    }

    /// <summary>
    /// 2D オブジェクトを、UI の四角の位置・回転・大きさに合わせる
    /// </summary>
    /// <param name="sprite">出している画像</param>
    private void FollowRect(Sprite sprite)
    {
        _rect.GetWorldCorners(_corners);
        Vector3 center = (_corners[0] + _corners[2]) * 0.5f;
        float width = Vector3.Distance(_corners[0], _corners[3]);
        float height = Vector3.Distance(_corners[0], _corners[1]);

        Vector2 spriteSize = sprite.bounds.size;
        float scaleX = width / spriteSize.x;
        float scaleY = height / spriteSize.y;

        if (_slot.IsPreserveAspect)
        {
            scaleX = Mathf.Min(scaleX, scaleY);
            scaleY = scaleX;
        }

        Transform worldTransform = _renderer.transform;
        worldTransform.SetPositionAndRotation(center, _rect.rotation);
        worldTransform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    /// <summary>
    /// 2D オブジェクトがなければ作る
    /// </summary>
    private void EnsureRenderer()
    {
        if (_renderer != null)
        {
            return;
        }

        GameObject worldObject = new GameObject(gameObject.name + "_World");

        if (_worldRoot == null)
        {
            _worldRoot = new GameObject(WorldRootName).transform;
        }

        worldObject.transform.SetParent(_worldRoot, false);
        _renderer = worldObject.AddComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 2D オブジェクトを隠して、UI の画像を出し直す
    /// </summary>
    private void Release()
    {
        if (_renderer != null)
        {
            _renderer.gameObject.SetActive(false);
        }

        if (_image != null)
        {
            _image.enabled = true;
        }
    }

    /// <summary>
    /// 色の四角として使う白い画像を用意する
    /// </summary>
    /// <returns>白い画像</returns>
    private static Sprite GetSolidSprite()
    {
        if (_solidSprite == null)
        {
            _solidSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }

        return _solidSprite;
    }
}
