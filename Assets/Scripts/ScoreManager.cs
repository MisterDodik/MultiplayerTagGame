using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private int score;

    [SerializeField] private GameObject scoreUI;

    [SerializeField] private TextMeshProUGUI scoreTMPro;
    [SerializeField] private TextMeshProUGUI scoreUpdateTMPro;
    [SerializeField] private Animator scoreUpdateAnimator;
    
    [SerializeField] private GameObject leaderBoardUI;
    [SerializeField] private TextMeshProUGUI gameDurationTMPro;
    [SerializeField] private TextMeshProUGUI leaderBoardTMPro;

    private void Start()
    {
        HideScoreUI();
    }
    public void ShowScoreUI()
    {
        scoreUI.SetActive(true);
        leaderBoardUI.SetActive(false);
        score = 0;
        scoreTMPro.text = "0";
        scoreUpdateTMPro.text = "";
    }
    public void HideScoreUI()
    {
        scoreUI.SetActive(false);
        leaderBoardUI.SetActive(false);
    }

    private void UpdateScoreHandler(object data)
    {
        if (data == null)
        {
            UnityEngine.Debug.LogError("update score sent null value");
        }
        ScoreUpdate scoreData = data as ScoreUpdate;
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            scoreUpdateAnimator.ResetTrigger("update_score");
            scoreUpdateTMPro.text = "+"+ scoreData.amount.ToString();
            scoreUpdateAnimator.SetTrigger("update_score");

            score = scoreData.score;
            scoreTMPro.text = score.ToString();
        });
    }

    private void ShowLeaderBoard(object data)
    {
        List<EndGameScore> scores = data as List<EndGameScore>;
        if (scores.Count == 0)
        {
            print("something went wrong, final score list is empty");
        }
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            gameDurationTMPro.text = "Game duration: " + scores[0].gameDuration + "s";
            leaderBoardTMPro.text = "";
            int counter = 1;
            foreach(EndGameScore score in scores)
            {
                leaderBoardTMPro.text += counter.ToString() +". "+score.username + " " + score.score + "\n";
                counter++;
            }
            leaderBoardUI.SetActive(true);
        });
    }
    private void OnEnable()
    {
        EventSystem.Subscribe(MessageType.UpdateScore, UpdateScoreHandler);
        EventSystem.Subscribe(MessageType.EndGameUpdateScore, ShowLeaderBoard);
    }
    private void OnDisable()
    {
        EventSystem.Unsubscribe(MessageType.UpdateScore, UpdateScoreHandler);
        EventSystem.Unsubscribe(MessageType.EndGameUpdateScore, ShowLeaderBoard);
    }


    //private void Update()
    //{
    //    if (Input.GetKey(KeyCode.RightArrow))
    //    {
    //        ShowScoreUI();
    //    }
    //    if (Input.GetKey(KeyCode.UpArrow))
    //    {
    //        score += 5;
    //        EventSystem.Emit(MessageType.UpdateScore, new ScoreUpdate
    //        {
    //            id = "",
    //            score = score,
    //            amount = 5
    //        });
    //    }

    //    if (Input.GetKey(KeyCode.LeftArrow))
    //    {
    //        EventSystem.Emit(MessageType.EndGameUpdateScore, new List<EndGameScore>
    //        {
    //           new EndGameScore{  
    //                username = "prvi",
    //                score = 123,
    //                gameDuration = "199"
    //           },
    //           new EndGameScore{
    //                username = "drugi",
    //                score = 234,
    //                gameDuration = "199"
    //           },
    //           new EndGameScore{
    //                username = "treci",
    //                score = 345,
    //                gameDuration = "199"
    //           },
    //        });
    //    }
   
    //}
}

[System.Serializable]
public class ScoreUpdate
{
    public string id;
    public int score;
    public int amount;
}

[System.Serializable]
public class EndGameScore
{
    public string id;
    public string username;
    public int score;
    public string gameDuration;
}