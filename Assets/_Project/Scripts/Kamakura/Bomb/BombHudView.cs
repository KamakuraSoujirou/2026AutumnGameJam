using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 爆弾本体の状態（問題文・持ち手・勝ち数・勝敗）と爆弾の位置を画面に出す
/// 日本語フォントが入るまでは英語で表示する
/// </summary>
public class BombHudView : MonoBehaviour
{
    [SerializeField] private BombManager _bombManager;
    [SerializeField] private TMP_Text _questionText;
    [SerializeField] private TMP_Text _holderText;
    [SerializeField] private TMP_Text _winsText;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private RectTransform _bomb;
    [SerializeField] private Image _bombImage;
    [SerializeField] private Vector2[] _playerPositions = { new Vector2(-450f, -80f), new Vector2(450f, -80f) };
    [SerializeField] private Color _normalColor = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] private Color _stunColor = new Color(0.3f, 0.4f, 1f);
    [SerializeField] private Color _explodeColor = new Color(1f, 0.45f, 0f);

    /// <summary>
    /// 進行役の知らせを受け取れるようにする
    /// </summary>
    private void OnEnable()
    {
        _bombManager.OnChanged += Refresh;
    }

    /// <summary>
    /// 進行役の知らせを受け取るのをやめる
    /// </summary>
    private void OnDisable()
    {
        _bombManager.OnChanged -= Refresh;
    }

    /// <summary>
    /// 最初の表示を作る
    /// </summary>
    private void Start()
    {
        Refresh();
    }

    /// <summary>
    /// 爆弾が飛んでいる間だけ、位置を動かす
    /// </summary>
    private void Update()
    {
        if (_bombManager.State != BombState.Flying)
        {
            return;
        }

        Vector2 from = _playerPositions[_bombManager.Holder];
        Vector2 to = _playerPositions[_bombManager.FlightTarget];
        _bomb.anchoredPosition = Vector2.Lerp(from, to, _bombManager.PhaseProgress);
    }

    /// <summary>
    /// 状態に合わせて、文字と爆弾の見た目をまとめて書き換える
    /// </summary>
    private void Refresh()
    {
        BombState state = _bombManager.State;
        int holderNumber = _bombManager.Holder + 1;
        int roundNumber = _bombManager.GetWins(0) + _bombManager.GetWins(1) + 1;

        _winsText.text = $"P1  {_bombManager.GetWins(0)} - {_bombManager.GetWins(1)}  P2";
        _questionText.text = state == BombState.Answering ? _bombManager.QuestionText : string.Empty;
        _holderText.text = state == BombState.Answering ? $"P{holderNumber} HAS THE BOMB!  (LV {_bombManager.Stage})" : string.Empty;

        if (state != BombState.Flying)
        {
            _bomb.anchoredPosition = _playerPositions[_bombManager.Holder];
        }

        _bombImage.color = _normalColor;

        switch (state)
        {
            case BombState.RoundStart:
                _statusText.text = $"ROUND {roundNumber}  -  P{holderNumber} STARTS";
                break;
            case BombState.Answering:
                _statusText.text = _bombManager.IsStunned ? "MISS!  -2.00" : string.Empty;
                _bombImage.color = _bombManager.IsStunned ? _stunColor : _normalColor;
                break;
            case BombState.Flying:
                _statusText.text = "PASS!";
                break;
            case BombState.Exploding:
                // 爆発したラウンドは勝ち数がもう増えているので、表示中のラウンド番号を1つ戻す
                _statusText.text = $"BOOM!  P{_bombManager.RoundWinner + 1} TAKES ROUND {roundNumber - 1}";
                _bombImage.color = _explodeColor;
                break;
            case BombState.MatchOver:
                _statusText.text = $"P{_bombManager.MatchWinner + 1} WINS THE MATCH!\n[SPACE] REMATCH";
                _bombImage.color = _explodeColor;
                break;
        }
    }
}
