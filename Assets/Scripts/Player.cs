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
    public float bladeLength;
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
    // クォータニオンの初期値を保存する変数
    public Quaternion startQuat;

    public Text text;

    // Start is called before the first frame update
    void Start()
    {
        //bladeLength = 4;
        startQuat = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        // 左クリックを押している間true(実行)
        if (Input.GetMouseButton(0))
        {
            // マウスがクリックした座標を入れておく変数
            // 画面左下を(0,0,0)とした、xy座標が代入される(zは0)
            Vector3 clickPos = Input.mousePosition;
            mousePos = clickPos;
            // ｚ座標に数値を代入することでカメラより前に表示される
            clickPos.z = 2;

            // 画面左下を(0,0,0)とした座標をワールド座標(Transformの絶対座標)に返還して、剣のポジションに設定
            blade.transform.position = Camera.main.ScreenToWorldPoint(clickPos);
            //blade.transform.position.z += 2;

            // カメラからマウスクリックの場所までRayを設定
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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
                // Debug.Log(hit.transform.name);
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
                if(cutStartPos == cutEndPos)
                {
                    cutEndPos = mousePos;
                    Debug.Log("一緒やった");
                }
                // 剣を切った方向に傾かせる（本番切る用）
                Vector3 range = cutEndPos - cutStartPos;
                float angle = Mathf.Atan2(range.y, range.x) * Mathf.Rad2Deg + 90;
                blade.transform.rotation = startQuat * Quaternion.AngleAxis(angle, Vector3.forward);

                // 二つに切り分け、leftObjectとrightObjectで受け取る
                MeshCut.Cut(target, blade.transform.position, blade.transform.right, capMaterial, out GameObject leftObject, out GameObject rightObject);
                Debug.Log("cut");

                //元のオブジェクトを削除
                Destroy(target);

                if (rightObject == null || leftObject == null) return;

                rightObject.AddComponent<MeshCollider>();
                rightObject.GetComponent<MeshCollider>().convex = true;

                Destroy(leftObject.GetComponent<BoxCollider>());
                Destroy(leftObject.GetComponent<MeshCollider>());
                leftObject.AddComponent<MeshCollider>().convex = true;
                //leftObject.GetComponent<MeshCollider>().convex = true;

                // プレイヤーを過ぎる直前にオブジェクトをデストロイする
                float distance = rightObject.transform.position.z - transform.position.z - 1f;
                Destroy(rightObject, distance / GetComponent<PlayerController>().speed);
                Destroy(leftObject, distance / GetComponent<PlayerController>().speed);
            }
            // マウスポジションを最新に更新
            lastMousePos = mousePos;

            // ブレードの位置によって角度を変える処理
            float Pos = blade.transform.position.x + 1;
            float angleDef = 80 * Pos / 2 - 40;
            blade.transform.rotation = startQuat * Quaternion.AngleAxis(angleDef, Vector3.up);
            Pos = blade.transform.position.y + 0.2f;
            angleDef = -90 * Pos / 1.5f + 90;
            blade.transform.rotation *= Quaternion.AngleAxis(angleDef, Vector3.right);
        }

        // マウスボタンを押すのをやめたとき
        if (Input.GetMouseButtonUp(0))
        {
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
