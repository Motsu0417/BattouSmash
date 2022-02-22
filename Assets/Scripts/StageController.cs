using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageController : MonoBehaviour
{
    // ステージの進むスピード
    public float speed = 2.0f;

    // Update is called once per frame
    void Update()
    {
        // 実行中でなければ
        if (!Manager.isRunningGame) return;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            // ステージをプレイヤー方向に動かす
            gameObject.transform.position += Vector3.back * speed * Time.deltaTime;
        }
        // ステージをプレイヤー方向に動かす
        gameObject.transform.position += Vector3.back * speed * Time.deltaTime;
    }
}
