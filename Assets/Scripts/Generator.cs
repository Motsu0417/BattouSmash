using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{

    List<Transform> children = new List<Transform>();
    // Start is called before the first frame update
    void Start()
    {
        // 見た目を消す
        Destroy(gameObject.GetComponent<MeshFilter>());

        // 子オブジェクトを非表示
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
            children.Add(child);
        }
    }

    // 子オブジェクトを再表示
    void OnTriggerEnter(Collider collider)
    { 
        foreach (Transform child in children)
        {
            // 再表示
            child.gameObject.SetActive(true);
            // 自分の子オブジェクトの親を自分の親オブジェクトへ
            child.parent = transform.parent;
        }
        Destroy(gameObject);
    }
}
