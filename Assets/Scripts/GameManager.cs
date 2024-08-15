using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private Mouth s_Mouth;
    [SerializeField] private int score = 0;
    [SerializeField] private int currentSpawned = 0;
    [SerializeField] private int maxSpawns = 50;
    [SerializeField] private Mouth.ShapeType currentMouthShapeType;
    [SerializeField] private float shapeScoreThreshold = 0.6f;

    private float currentSpawnTime;
    [SerializeField] private float nextSpawnTime;

    [SerializeField] private GameObject pf_ball;
    [SerializeField] private Vector3 spawnPos;

    [SerializeField] private List<Mouth.ShapeType> typesConsidered;
    [SerializeField] private TextMeshProUGUI shapeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI remainingText;
    [SerializeField] private GameObject gameEndUI;
    [SerializeField] private TextMeshProUGUI gameEndText;

    private GameObject lastBallSpawned;
    private bool isGameEnded = false;

    // Start is called before the first frame update
    void Start()
    {
        s_Mouth = Mouth.Instance;
        currentSpawnTime = 0;
        nextSpawnTime = Random.Range(0.5f, 2f);

        scoreText.text = "score: 0";
        remainingText.text = "remaining: " + maxSpawns;
    }

    // Update is called once per frame
    void Update()
    {
        //Handle shape scores
        var scores = s_Mouth.GetAllShapeAvgScore();
        float maxScore = shapeScoreThreshold;
        Mouth.ShapeType maxShape = Mouth.ShapeType.None;
        for (int i = 0; i < scores.Count; i++)
        {
            if (!typesConsidered.Contains(scores[i].Item1)) continue;
            if (scores[i].Item2 >= maxScore)
            {
                maxScore = scores[i].Item2;
                maxShape = scores[i].Item1;
            }
        }
        currentMouthShapeType = maxShape;
        shapeText.text = "Mouth Shape: " + currentMouthShapeType.ToString().ToUpper();
        if (isGameEnded) return;

        //Spawn Time
        currentSpawnTime -= Time.deltaTime;
        if (currentSpawnTime <= 0 && currentSpawned < maxSpawns)
        {
            GameObject ball = Instantiate(pf_ball, spawnPos, Quaternion.identity);
            ball.GetComponent<Ball>().setBallShape(Random.Range(0, 3));
            lastBallSpawned = ball;

            currentSpawnTime = nextSpawnTime;
            nextSpawnTime = Random.Range(1f, 2.5f);

            currentSpawned++;
            remainingText.text = (maxSpawns - currentSpawned).ToString();
        }
        
        if (currentSpawned >= maxSpawns && lastBallSpawned == null)
        {
            EndGame();
        }
    }

    public void addScore()
    {
        score++;
        scoreText.text = "Score: " + score.ToString().ToUpper();
    }
    public bool check(Ball ball)
    {
        return ball.ballShape == currentMouthShapeType;
    }
    public void EndGame()
    {
        isGameEnded = true;
        gameEndUI.SetActive(true);
        gameEndText.text = "Game End\nScore: " + score;
        Debug.Log("Game End\nScore: " + score);
    }
}
