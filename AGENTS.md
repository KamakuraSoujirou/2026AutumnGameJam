# 2026AutumnGameJam — AIエージェント向けガイド

このファイルは Claude Code / Cursor / Copilot / Gemini など、すべてのAIツール共通の指示書です。
（CLAUDE.md と GEMINI.md はこのファイルを読み込んでいるだけなので、編集はここだけでOK）

## プロジェクト概要
- ゲーム名: 2026AutumnGameJam
- ジャンル / 概要: （ここに書く）
- Unity バージョン: 6000.6.7f1
- 作成日: 2026-11-24

## フォルダ構成
このリポジトリは**公開**です。画像・音声などのアセットは、非公開リポジトリ
（`2026AutumnGameJam-assets`）を Git サブモジュールとして `Assets/_PrivateAssets/` に入れています。

公開（このリポジトリ）
- `Assets/Scripts/` … 自作の C# スクリプト
- `Assets/Scenes/` … シーン
- `Assets/Prefabs/` … プレハブ

非公開（サブモジュール `Assets/_PrivateAssets/`）
- `Art/` … 画像・テクスチャ・マテリアル・UI素材
- `Audio/` … BGM・効果音
- `Models/` … 3Dモデル・アニメーション
- `ThirdParty/` … アセットストア等の購入・配布アセット（再配布不可）

## コーディング規約（C#）
Notion「コード規約」の内容です。短期間のゲームジャムなので、完璧さより「みんなが読める・壊さない」を優先します。決まっていないことはチームで相談して追記してください。

### 命名規則
| 対象 | 書き方 | 例 |
| --- | --- | --- |
| クラス・構造体・enum | PascalCase | `PlayerController`, `GameState` |
| メソッド | PascalCase（動詞から始める） | `Jump()`, `TakeDamage()` |
| プロパティ | PascalCase | `CurrentHp`, `IsDead` |
| privateフィールド | _camelCase | `_moveSpeed`, `_rigidbody` |
| ローカル変数・引数 | camelCase | `damage`, `targetPosition` |
| 定数（const） | PascalCase | `MaxHp`, `DefaultSpeed` |
| インターフェース | 先頭に I | `IDamageable` |
| enumの値 | PascalCase | `GameState.Playing` |

- bool は `is` / `has` / `can` などで始め、Yes/Noで答えられる名前にする（`isGrounded`, `canJump`）
- 省略しすぎない（`pc` ではなく `playerController`）。ただし `i`, `x`, `y` などの慣習的なものはOK
- **ファイル名とクラス名を必ず一致させる**（一致しないと MonoBehaviour がアタッチできない）

### フォーマット
- インデントはスペース4つ
- 波括弧 `{ }` は改行して書く（Unity・C#の標準スタイル）
- if / for の中身が1行でも `{ }` を省略しない
- 1ファイル1クラス
- 使っていない `using` や、空の `Start()` / `Update()` は消す

### MonoBehaviour のルール
- インスペクターに出す変数は `public` ではなく `[SerializeField] private` を使う。外から読みたい値はプロパティで公開する

```cs
[SerializeField] private float _moveSpeed = 5f;

public float MoveSpeed => _moveSpeed;
```

- 初期化の順番
  - `Awake()`：自分自身の準備（`GetComponent` のキャッシュなど）
  - `Start()`：他のオブジェクトを参照する準備
- 重い処理を `Update()` に書かない
  - `GetComponent` / `GameObject.Find` / `FindObjectOfType` を毎フレーム呼ばない → `Awake()` で一度取得して変数に入れておく
  - `GameObject.Find`（名前で探す）はなるべく使わず、インスペクターで参照を入れる
  - 物理（Rigidbody）の処理は `FixedUpdate()` に書く

### コメント
- クラスとメソッドにはサマリー（`/// <summary>`）を日本語で書く

```cs
using UnityEngine;

/// <summary>
/// プレイヤーの左右移動とジャンプを管理する
/// </summary>
public class PlayerController : MonoBehaviour
{
    private const float GroundCheckDistance = 0.1f;

    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpPower = 7f;
    [SerializeField] private LayerMask _groundLayer;

    private Rigidbody2D _rigidbody;
    private Collider2D _collider;
    private float _moveInput;
    private bool _isGrounded;
    private bool _isJumpRequested;

    /// <summary>
    /// 地面に接しているか
    /// </summary>
    public bool IsGrounded => _isGrounded;

    /// <summary>
    /// 自分のコンポーネントを取得して保存しておく
    /// </summary>
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// 入力を受け取る（物理の処理は FixedUpdate で行う）
    /// </summary>
    private void Update()
    {
        _moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _isJumpRequested = true;
        }
    }

    /// <summary>
    /// 接地判定と、移動・ジャンプの物理処理を行う
    /// </summary>
    private void FixedUpdate()
    {
        _isGrounded = CheckGrounded();
        Move();

        if (_isJumpRequested)
        {
            Jump();
            _isJumpRequested = false;
        }
    }

    /// <summary>
    /// 上方向に力を加えてジャンプする
    /// </summary>
    public void Jump()
    {
        _rigidbody.AddForce(Vector2.up * _jumpPower, ForceMode2D.Impulse);
    }

    /// <summary>
    /// 入力に合わせて左右に移動する
    /// </summary>
    private void Move()
    {
        _rigidbody.linearVelocity = new Vector2(_moveInput * _moveSpeed, _rigidbody.linearVelocity.y);
    }

    /// <summary>
    /// 足元に地面があるかを調べる
    /// </summary>
    /// <returns>地面に接していれば true</returns>
    private bool CheckGrounded()
    {
        Bounds bounds = _collider.bounds;
        RaycastHit2D hit = Physics2D.BoxCast(bounds.center, bounds.size, 0f, Vector2.down, GroundCheckDistance, _groundLayer);
        return hit.collider != null;
    }
}
```

### チーム開発での注意（Unity / Git）
- 同じシーンを複数人で同時に編集しない（マージがほぼ不可能）。自分用のテストシーンを作って作業する
- 使い回すオブジェクトはプレハブにして、プレハブ単位で編集する
- `.meta` ファイルも必ず一緒にコミットする
- 全員同じUnityバージョンを使う（上の「プロジェクト概要」参照）
- コミット前にエラーが出ていないか（Consoleが赤くないか）確認する
- 自分のスクリプトは `Assets/Scripts/` の下にまとめる

## AIへのルール
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` の中身は読まない・編集しない
- アセットを作成・移動・削除するときは、対応する `.meta` ファイルも必ず一緒に扱う（GUIDが壊れるため）
- `.unity` / `.prefab` / `.asset` のYAMLは、頼まれない限り直接編集しない
- 画像・音声・モデル・購入アセットは必ず `Assets/_PrivateAssets/` 以下に置く（公開リポジトリに入れない）
- `Assets/_PrivateAssets/` 内の変更は、サブモジュール側でコミット & push してから、本体側でサブモジュールの更新をコミットする
- パッケージの追加（`Packages/manifest.json` の変更）は事前に確認すること
- 変更後はコンパイルエラーが出ないか確認し、変更点を簡潔に説明すること
