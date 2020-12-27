using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchCheck : MonoBehaviour
{
    int touch = 0;
    public GameObject pointBall;
    public GameObject parent;
    RaycastHit lastHit;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 10.0f))
            {
                if(touch == 0)
                {
                    Debug.Log("今触った！");
                    touch = 1;
                    Instantiate(pointBall, hit.point,Quaternion.identity,parent.transform);
                }
                Debug.Log(hit.transform.name);
                lastHit = hit;
            }
            else if (touch == 1)
            {
                Debug.Log("今触るのやめた！");
                touch = 0;
                Instantiate(pointBall, lastHit.point, Quaternion.identity,parent.transform);
            }
        }
        
    }
}
