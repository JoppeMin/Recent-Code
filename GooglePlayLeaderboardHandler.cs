using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using TMPro;
using DG.Tweening;

public class GooglePlayLeaderboardHandler : MonoBehaviour
{
    int highscore;
    public TextMeshProUGUI highScoreText;
    public List<TextMeshProUGUI> ranks = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> names = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> scores = new List<TextMeshProUGUI>();

    void Start()
    {
        highscore = PlayerPrefs.GetInt("HighScore");
        if (UiScript.oldScore != 0)
        {
            StartCoroutine(UpdateHighscoreText(UiScript.oldScore));
        }
        else
        {
            StartCoroutine(UpdateHighscoreText(highscore));
        }

        UpdateLeaderboardScreen();
 
    }

    IEnumerator UpdateHighscoreText(int score)
    {
        int tempscore = score;
            
//Ticks up when your achieved score is higher than your previous highscore
        if (tempscore != highscore)
        {
            highScoreText.SetText(tempscore.ToString());
            highScoreText.transform.DOPunchScale(new Vector3(0.1f, 1f), 0.1f);
            AudioManager.SP.Play("PointSound");
            yield return new WaitForSeconds(0.1f);
            tempscore++;
            StartCoroutine(UpdateHighscoreText(tempscore));
        }
        else
        {
            highScoreText.SetText(highscore.ToString());
        }
    }


    public void UpdateLeaderboardScreen()
    {
    //Fetches users from Google Play leaderboard and displays the top 6 users. if you're not in the top 6 it will always display you at 6
        List<string> topSix = new List<string>();

        Social.LoadScores(GPGSIds.leaderboard_highscore, leaderboard =>
        {
            for (int i = 0; i < 5; i++)
            {
                if (leaderboard.Length > i)
                {
                    var score = leaderboard[i];  

                    ranks[i].text = score.rank.ToString() + ".";
                    scores[i].text = score.value.ToString();
                }
            }
            foreach (IScore position in leaderboard)
            {
                topSix.Add(position.userID);

                string localid = Social.localUser.id;
                Color32 localColor = new Color32(255,200,0,255);

                if (localid == position.userID)
                {
                    if (position.rank-1 >= 5)
                    {
                        ranks[5].text = position.rank.ToString() + ".";
                        names[5].text = Social.localUser.userName == null ? "(You)-Hidden-" : Social.localUser.userName;
                        scores[5].text = position.value.ToString();
                        ranks[5].color = localColor;
                        names[5].color = localColor;
                        scores[5].color = localColor;
                    } else
                    {
                        ranks[position.rank-1].color = localColor;
                        names[position.rank-1].color = localColor;
                        scores[position.rank-1].color = localColor;
                    }
                    continue;
                }
            }
            Social.LoadUsers(topSix.ToArray(), userIDs =>
            {
                for (int i = 0; i < 5; i++)
                {
                    if (i < userIDs.Length)
                    {
                        names[i].text = userIDs[i].userName ?? "-Hidden-";
                    }
                }
            });
        });
    }
}
