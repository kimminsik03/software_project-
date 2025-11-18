using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour {

	public Text scoreText; // 점수 올리는 텍스트 받아오기
	public float score = 0f; // 점수 변수

	public float scoreRate = 10f; // 초당 점수 증가량
	private bool isGameOver = false; // 게임 오버 전까진 계속 오름

    public Text bestScoreText; // UI에 표시할 텍스트
                               // Update is called once per frame


    void Start()
    {
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        bestScoreText.text = "Best: " + bestScore.ToString();
    }

    void Update () {
        if (!isGameOver)
        {
            score += scoreRate * Time.deltaTime;
            scoreText.text = "Score: " + Mathf.FloorToInt(score).ToString();
        }

    }

    public void StopScore()
    {
        isGameOver = true;
    }

    // 최고 점수 저장용 함수
    public void SaveBestScore()
    {
        int finalScore = Mathf.FloorToInt(score);
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);

        if (finalScore > bestScore)
        {
            PlayerPrefs.SetInt("BestScore", finalScore);
            PlayerPrefs.Save();
            Debug.Log("New Best Score: " + finalScore);
        }
    }

}
