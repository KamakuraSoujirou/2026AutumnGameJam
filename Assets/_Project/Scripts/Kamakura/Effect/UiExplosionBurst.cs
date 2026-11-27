using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 爆発のエフェクト。奥の煙から手前の光まで、重ねた絵を少しずつずらして弾けさせ、最後に「BOOM」の文字を叩きつける
/// </summary>
/// <remarks>
/// 絵はすべて画面と同じ 1920×1080 で、同じ位置に重ねる前提。拡大の中心は RectTransform の pivot（爆発の中心に合わせておく）
/// </remarks>
public class UiExplosionBurst : MonoBehaviour
{
    private const float LayerPopSeconds = 0.28f;
    private const float LayerStartScale = 0.2f;
    private const float LayerOvershootScale = 1.12f;
    private const float WordSlamSeconds = 0.22f;
    private const float WordStartScale = 2.6f;
    private const float WordSettleScale = 1f;
    private const float ShakeSeconds = 0.35f;

    [SerializeField, Tooltip("爆発の絵。奥（煙）から手前（光）の順に入れる")] private Image[] _layers;
    [SerializeField, Tooltip("「BOOM」の文字の絵")] private Image _word;
    [SerializeField, Min(0f), Tooltip("絵を1枚ずつ出す間隔（秒）")] private float _layerInterval = 0.05f;
    [SerializeField, Min(0f), Tooltip("最後の絵が出てから文字を出すまでの間（秒）")] private float _wordDelay = 0.05f;
    [SerializeField, Tooltip("絵ごとの回転の揺れ（度）。出るときにこの角度から 0 に戻る")] private float _layerSpinDegrees = 12f;
    [SerializeField, Min(0f), Tooltip("文字が叩きつけられたときの揺れの大きさ")] private float _shakeStrength = 18f;
    [SerializeField, Min(0f), Tooltip("出し終わってからも、ゆっくり膨らみ続ける速さ（1秒あたりの拡大率）")] private float _breatheSpeed = 0.04f;

    private Coroutine _routine;
    private Vector2 _wordBasePosition;
    private bool _hasWordBasePosition;
    private float _playStartTime;
    private bool _isPlaying;

    /// <summary>
    /// 出し終わったあと、絵全体をゆっくり膨らませて止まって見えないようにする
    /// </summary>
    private void Update()
    {
        if (!_isPlaying || _routine != null)
        {
            return;
        }

        float scale = 1f + (Time.unscaledTime - _playStartTime) * _breatheSpeed;
        transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// 爆発のエフェクトを最初から出す
    /// </summary>
    public void Play()
    {
        Clear();
        gameObject.SetActive(true);
        _isPlaying = true;
        _playStartTime = Time.unscaledTime;

        if (Application.isPlaying)
        {
            _routine = StartCoroutine(PlayBurst());
        }
        else
        {
            ShowAllAtRest();
        }
    }

    /// <summary>
    /// エフェクトを消して、最初の状態に戻す
    /// </summary>
    public void Clear()
    {
        RememberWordBasePosition();

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _isPlaying = false;
        transform.localScale = Vector3.one;

        foreach (Image layer in _layers)
        {
            ResetGraphic(layer);
        }

        ResetGraphic(_word);

        if (_word != null)
        {
            _word.rectTransform.anchoredPosition = _wordBasePosition;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 絵を奥から順に弾けさせ、最後に文字を叩きつけて画面を揺らす
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator PlayBurst()
    {
        float duration = (_layers.Length - 1) * _layerInterval + LayerPopSeconds;

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            for (int i = 0; i < _layers.Length; i++)
            {
                float rate = (elapsed - i * _layerInterval) / LayerPopSeconds;
                float spin = (i % 2 == 0 ? 1f : -1f) * _layerSpinDegrees;
                ApplyLayerPop(_layers[i], rate, spin);
            }

            yield return null;
        }

        foreach (Image layer in _layers)
        {
            ApplyLayerPop(layer, 1f, 0f);
        }

        yield return WaitUnscaled(_wordDelay);

        if (_word != null)
        {
            yield return SlamWord();
            yield return ShakeWord();
        }

        _routine = null;
        _playStartTime = Time.unscaledTime;
    }

    /// <summary>
    /// 文字を大きい状態から叩きつけるように縮めて出す
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator SlamWord()
    {
        Transform word = _word.transform;
        SetAlpha(_word, 0f);
        _word.gameObject.SetActive(true);

        for (float elapsed = 0f; elapsed < WordSlamSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float rate = elapsed / WordSlamSeconds;
            float eased = rate * rate;
            word.localScale = Vector3.one * Mathf.Lerp(WordStartScale, WordSettleScale, eased);
            SetAlpha(_word, Mathf.Clamp01(rate * 2f));
            yield return null;
        }

        word.localScale = Vector3.one * WordSettleScale;
        SetAlpha(_word, 1f);
    }

    /// <summary>
    /// 叩きつけた直後に、文字をガタガタ揺らす（だんだん弱める）
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator ShakeWord()
    {
        RectTransform word = _word.rectTransform;

        for (float elapsed = 0f; elapsed < ShakeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float power = _shakeStrength * (1f - elapsed / ShakeSeconds);
            word.anchoredPosition = _wordBasePosition + Random.insideUnitCircle * power;
            yield return null;
        }

        word.anchoredPosition = _wordBasePosition;
    }

    /// <summary>
    /// 文字の元の位置を、最初の1回だけ覚えておく（揺らしたあとに戻すため）
    /// </summary>
    private void RememberWordBasePosition()
    {
        if (_hasWordBasePosition || _word == null)
        {
            return;
        }

        _wordBasePosition = _word.rectTransform.anchoredPosition;
        _hasWordBasePosition = true;
    }

    /// <summary>
    /// 絵1枚の出方を、進み具合に合わせて決める（小さい状態から膨らみすぎて落ち着く）
    /// </summary>
    /// <param name="layer">動かす絵</param>
    /// <param name="rate">進み具合（0 未満ならまだ出さない、1 以上なら出し終わり）</param>
    /// <param name="spin">出始めの回転（度）</param>
    private static void ApplyLayerPop(Image layer, float rate, float spin)
    {
        if (layer == null)
        {
            return;
        }

        if (rate <= 0f)
        {
            layer.gameObject.SetActive(false);
            return;
        }

        layer.gameObject.SetActive(true);
        rate = Mathf.Clamp01(rate);

        float scale = rate < 0.6f
            ? Mathf.Lerp(LayerStartScale, LayerOvershootScale, EaseOutCubic(rate / 0.6f))
            : Mathf.Lerp(LayerOvershootScale, 1f, (rate - 0.6f) / 0.4f);

        layer.transform.localScale = Vector3.one * scale;
        layer.transform.localRotation = Quaternion.Euler(0f, 0f, spin * (1f - rate));
        SetAlpha(layer, Mathf.Clamp01(rate * 3f));
    }

    /// <summary>
    /// エディターの確認用に、動かさずに出し終わった形で全部出す
    /// </summary>
    private void ShowAllAtRest()
    {
        foreach (Image layer in _layers)
        {
            ApplyLayerPop(layer, 1f, 0f);
        }

        if (_word != null)
        {
            _word.gameObject.SetActive(true);
            SetAlpha(_word, 1f);
        }
    }

    /// <summary>
    /// 絵を隠して、大きさ・回転・透明度を元に戻す
    /// </summary>
    /// <param name="graphic">戻す絵</param>
    private static void ResetGraphic(Image graphic)
    {
        if (graphic == null)
        {
            return;
        }

        graphic.transform.localScale = Vector3.one;
        graphic.transform.localRotation = Quaternion.identity;
        SetAlpha(graphic, 1f);
        graphic.gameObject.SetActive(false);
    }

    /// <summary>
    /// 絵の透明度だけを変える
    /// </summary>
    /// <param name="graphic">変える絵</param>
    /// <param name="alpha">透明度（0〜1）</param>
    private static void SetAlpha(Image graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    /// <summary>
    /// 勢いよく動き出して、最後はゆっくり止まる曲線
    /// </summary>
    /// <param name="t">0〜1</param>
    /// <returns>0〜1</returns>
    private static float EaseOutCubic(float t)
    {
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }

    /// <summary>
    /// 時間の進み（Time.timeScale）に関係なく待つ
    /// </summary>
    /// <param name="seconds">待つ秒数</param>
    /// <returns>コルーチン</returns>
    private static IEnumerator WaitUnscaled(float seconds)
    {
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            yield return null;
        }
    }
}
