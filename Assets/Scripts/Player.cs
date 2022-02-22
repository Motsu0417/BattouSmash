using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    // 断面のマテリアル
    public Material capMaterial;
    // プレイヤーが持つ剣のオブジェクト
    public GameObject blade;
    // 剣の長さ
    public float bladeLength = 4.0f;
    // 切るオブジェクトの切り始めと切り終わりを保存する変数
    Vector3 cutStartPos, cutEndPos;
    // 剣の向きを調整するためにマウスのポジションを保存しておく変数
    Vector3 mousePos, lastMousePos;
    // 前の状態を保存する変数
    // 0 = 当っていない,1 = 当たった
    int touch = 0;
    // 切るオブジェクトを入れる変数
    GameObject target;
    // 直前のRaycastHitを保存する変数
    RaycastHit lastHit;
    // クォータニオンの初期値と現在値を保存する変数
    Quaternion startQuat,nowQuat;

    // AudioSourceを保存する
    AudioSource audioSource;
    // カウントダウン、BGM、斬撃音を保存する変数
    public AudioClip[] sounds;

    public GameObject scoreText;
    public GameObject pointText;
    public GameObject localCanvas;
    public GameObject countDownText;

    // Start is called before the first frame update
    void Start()
    {
        // 初期の剣のクォータニオンを記憶しておく
        startQuat = blade.transform.rotation;
        // 現在値も設定する
        nowQuat = startQuat;
        // AudioSourceを取得
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0.02f;
        audioSource.PlayOneShot(sounds[0]);
        audioSource.clip = sounds[1];
        audioSource.PlayDelayed(3.3f);
    }

    // Update is called once per frame
    void Update()
    {
        // ゲームが実行中でなければ
        //if (!Manager.isRunningGame) return;

        // 左クリックを押している間true(実行)
        if (Input.GetMouseButton(0))
        {
            // マウスがクリックした座標を入れておく変数
            // 画面左下を(0,0,0)とした、xy座標が代入される(zは0)
            Vector3 mousePos = Input.mousePosition;
            // 画面左下を(0,0,0)とした座標をワールド座標(Transformの絶対座標)に返還して、剣のポジションに設定
            blade.transform.position = Camera.main.ScreenToWorldPoint(mousePos);
            // カメラからマウスクリックの場所までRayを設定
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            // rayを剣の長さ分照射して当たったらhitに保存する
            if (Physics.Raycast(ray, out RaycastHit hit, bladeLength))
            {
                // もし、前まで当たってなかったら
                if (touch == 0)
                {
                    // Debug.Log("今当たった！");
                    // 当たったに状態を変更
                    touch = 1;
                    // 切り始めポイントをセット
                    cutStartPos = hit.point;
                    // 切る対象のオブジェクトを保存
                    target = hit.collider.gameObject;
                }

                // 最後のヒットを更新
                lastHit = hit;
            }
            // もし今回rayが当たらなくて、今まで当たっていたなら（切り終わった状況）
            else if (touch == 1)
            {
                // Debug.Log("今当たり終わった");
                // 当たってないに状態を変更
                touch = 0;
                // 切り終わりポイントをセット
                cutEndPos = lastHit.point;
                // もし切り始めと切り終わりが同じなら、今のマウスポジションを切り終わりのポジションにする
                if (cutStartPos == cutEndPos)
                {
                    cutEndPos = mousePos;
                }

                // 剣を切った方向に傾かせる（本番切る用）
                // 切り始めと切り終わりのポジションの差から、アークタンジェントで角度を求める
                Vector3 range = cutEndPos - cutStartPos;
                float angle = Mathf.Atan2(range.y, range.x) * Mathf.Rad2Deg + 90;
                blade.transform.rotation = nowQuat * Quaternion.AngleAxis(angle, Vector3.forward);

                // targetが不正な場合、終了
                if (target == null) return;
                // 二つに切り分け、leftObjectとrightObjectで受け取る
                MeshCut.Cut(target, blade.transform.position, blade.transform.right, capMaterial, out GameObject leftObject, out GameObject rightObject);
                Debug.Log("cut");
                // 斬撃音を鳴らす
                audioSource.PlayOneShot(sounds[2]);

                //pointTextを表示してスコアを追加
                if(target.tag != "Untagged") // タグが設定されていない場合無視
                {
                    Vector3 textPos = target.transform.position + Vector3.up*2;
                    GameObject tmpPointText = Instantiate(pointText, textPos, Quaternion.identity, localCanvas.transform);
                    if (target.tag == "Enemy")
                    {
                        tmpPointText.GetComponent<Text>().text = "40pt";
                        scoreText.GetComponent<Score>().score += 40;
                    }else if(target.tag == "Fluit")
                    {
                        tmpPointText.GetComponent<Text>().text = "回復";
                    }
                    Destroy(tmpPointText, 1);
                }

                //元のオブジェクトを削除
                Destroy(target);

                //// もし返ってきたオブジェクトが存在しなければ終了
                //if (rightObject == null || leftObject == null) return;

                // 1秒後に消える様に設定
                Destroy(rightObject, 1);
                Destroy(leftObject, 1);
            }
            
            // マウスポジションを最新に更新
            lastMousePos = mousePos;

            // ブレードの位置によって角度を変える処理--------------------------------------------
            // 横方向
            float Pos = ray.direction.x;
            float angleDef = Pos * 60;
            blade.transform.rotation = startQuat * Quaternion.AngleAxis(angleDef, Vector3.up);
            // 縦方向
            Pos = ray.direction.y;
            angleDef = -Pos * 60;
            blade.transform.rotation *= Quaternion.AngleAxis(angleDef, Vector3.right);
            nowQuat = blade.transform.rotation;
            // -------------------------------------------------------------------------------
        }

        // マウスボタンを押すのをやめたとき初期化する
        if (Input.GetMouseButtonUp(0))
        {
            nowQuat = startQuat;
            cutStartPos = Vector3.zero;
            cutEndPos = Vector3.zero;
            // 剣の向きを調整するためにマウスのポジションを保存しておく変数
            mousePos = Vector3.zero;
            lastMousePos = Vector3.zero;
            touch = 0;
            // 切るオブジェクトを入れる変数
            target = null;
        }
    }


}
