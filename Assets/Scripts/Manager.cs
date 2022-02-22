using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    // ゲームの状態を保存する
    // true = 動いている
    // false = 中断・休止中
    public static bool isRunningGame = false;
    // カウントダウンに使う時間を保存する
    float timer;

    // GetComponentするためにプレイヤーとステージを取得
    public GameObject Player, Stage;

    // Start is called before the first frame update
    void Start()
    {
        timer = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        // タイマーが3.3秒以上で、isRunningGameがfalseの時
        if(timer > 3.3f && !isRunningGame)
        {
            isRunningGame = true;
            Player.SetActive(true);
        }
    }
}
