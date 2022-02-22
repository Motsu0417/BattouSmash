using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    public int score;
    public int showScore;
    public Text scoreText;

    void Start()
    {
        score = 0;
        showScore = 0;
    }

    // Update is called once per frame
    void Update()
    {
       if(score > showScore)
        {
            showScore += 1 + (score-showScore)/20;
            if (showScore > score) showScore = score;
            scoreText.text = $"SCORE: {showScore:000000}";
        }
    }
}
