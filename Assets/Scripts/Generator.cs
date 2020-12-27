using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{
    public GameObject[] objects;
    public float generateTime = 8.0f;
    public float timer;

    // Start is called before the first frame update
    void Start()
    {
        timer = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if(timer > generateTime)
        {
            if(objects.Length != 0)
            {
                GameObject obj = Instantiate(objects[Random.Range(0, objects.Length)],transform);
                obj.AddComponent<Rigidbody>();
                obj.AddComponent<BoxCollider>();
            }
            timer -= generateTime;
        }
        timer += Time.deltaTime;
    }
}
