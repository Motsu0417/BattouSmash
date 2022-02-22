using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Goal : MonoBehaviour
{
    void OnTriggerEnter(Collider collider)
    {
        Debug.Log("enter");
        if(collider.transform.name == "Player")
        {
            SceneManager.LoadScene("Goal");
        }
    }
}
