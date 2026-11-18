using System.Collections.Generic;
using UnityEngine;

public class FlappyBirdGame : MonoBehaviour
{
    private enum GameState
    {
        Waiting,
        Playing,
        GameOver
    }

    [Header("Sprites do pacote")]
    [SerializeField] private Sprite[] birdFrames;
    [SerializeField] private Sprite upperPipeSprite;
    [SerializeField] private Sprite lowerPipeSprite;
    [SerializeField] private Sprite skySprite;
    [SerializeField] private Sprite groundSprite;
    [SerializeField] private Transform environmentRoot;
    [SerializeField] private Transform gameplayRoot;

    [Header("Configuracao")]
    [SerializeField] private float pipeSpeed = 3.5f;
    [SerializeField] private float pipeSpawnInterval = 2.1f;
    [SerializeField] private float gapSize = 2.6f;
    [SerializeField] private float pipeScale = 0.72f;
    [SerializeField] private float birdScale = 0.42f;
    [SerializeField, Min(0f)] private float groundColliderInset = 0.5f;

    private readonly List<PipePair> pipes = new List<PipePair>();
    private Camera gameCamera;
    private CharacterMenager bird;
    private Transform pipeContainer;
    private GameState state;
    private float spawnTimer;
    private int score;
    private int bestScore;
    private string gameOverReason;

    public bool IsPlaying => state == GameState.Playing;
    public float ScreenTop => gameCamera.orthographicSize;

    private void Start()
    {
        gameCamera = GetComponent<Camera>();
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }

        if (!HasRequiredSprites())
        {
            enabled = false;
            return;
        }

        if (gameCamera == null)
        {
            Debug.LogError("FlappyBirdGame precisa de uma Camera para iniciar.", this);
            enabled = false;
            return;
        }

        gameCamera.orthographic = true;
        gameCamera.orthographicSize = 5f;
        gameCamera.backgroundColor = new Color(0.72f, 0.91f, 0.94f);
        bestScore = PlayerPrefs.GetInt("FlappyBirdBestScore", 0);

        if (environmentRoot == null)
        {
            environmentRoot = new GameObject("Environment").transform;
        }

        if (gameplayRoot == null)
        {
            gameplayRoot = new GameObject("Gameplay").transform;
        }

        gameplayRoot.gameObject.SetActive(true);

        CreateSceneArt();
        pipeContainer = new GameObject("Pipes").transform;
        pipeContainer.SetParent(gameplayRoot);
        ResetRound();
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnPipe();
            spawnTimer = pipeSpawnInterval;
        }
    }

    private void OnGUI()
    {
        if (!enabled || gameCamera == null)
        {
            return;
        }

        float scale = Mathf.Clamp(Screen.width / 900f, 0.75f, 1.5f);
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(48f * scale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.12f, 0.21f, 0.24f) }
        };
        GUIStyle recordStyle = new GUIStyle(scoreStyle)
        {
            fontSize = Mathf.RoundToInt(22f * scale)
        };

        GUI.Label(new Rect(0f, 20f, Screen.width, 70f), score.ToString(), scoreStyle);
        GUI.Label(new Rect(0f, 82f, Screen.width, 32f), "Recorde: " + bestScore, recordStyle);

        if (state == GameState.Waiting)
        {
            DrawMessage("FLAPPY BIRD", "Toque, clique ou aperte ESPACO para voar");
        }
        else if (state == GameState.GameOver)
        {
            DrawMessage("FIM DE JOGO", gameOverReason + "\nPontuacao: " + score + "   |   Recorde: " + bestScore);
            float buttonWidth = 190f * scale;
            float buttonHeight = 48f * scale;
            Rect button = new Rect((Screen.width - buttonWidth) * 0.5f,
                Screen.height * 0.68f, buttonWidth, buttonHeight);
            if (GUI.Button(button, "Jogar novamente"))
            {
                ResetRound();
            }
        }
    }

    public void StartGame()
    {
        if (state == GameState.Waiting)
        {
            state = GameState.Playing;
            spawnTimer = 0.7f;
            bird.StartPlaying();
        }
    }

    public void EndGame(string reason)
    {
        if (state != GameState.Playing)
        {
            return;
        }

        state = GameState.GameOver;
        gameOverReason = reason;
        Debug.LogWarning("Fim de jogo: " + reason, bird);
        bird.Stop();
    }

    public void AddPoint()
    {
        if (!IsPlaying)
        {
            return;
        }

        score++;
        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("FlappyBirdBestScore", bestScore);
            PlayerPrefs.Save();
        }
    }

    public float GetPipeSpeed()
    {
        return Mathf.Min(pipeSpeed + score * 0.06f, 5.5f);
    }

    public void RemovePipe(PipePair pipe)
    {
        pipes.Remove(pipe);
    }

    private bool HasRequiredSprites()
    {
        if (birdFrames == null || birdFrames.Length == 0 || upperPipeSprite == null ||
            lowerPipeSprite == null || skySprite == null || groundSprite == null)
        {
            Debug.LogError("Atribua os sprites do passaro, canos, ceu e chao no componente FlappyBirdGame.", this);
            return false;
        }

        return true;
    }

    private void CreateSceneArt()
    {
        float viewWidth = gameCamera.orthographicSize * 2f * gameCamera.aspect;
        CreateBackground(viewWidth);
        CreateGround(viewWidth);
    }

    private void CreateBackground(float viewWidth)
    {
        GameObject background = new GameObject("Sky Background");
        background.transform.SetParent(environmentRoot, false);
        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = skySprite;
        renderer.sortingOrder = -10;

        Vector2 spriteSize = skySprite.bounds.size;
        background.transform.position = new Vector3(0f, 0f, 5f);
        background.transform.localScale = new Vector3(viewWidth / spriteSize.x,
            gameCamera.orthographicSize * 2f / spriteSize.y, 1f);
    }

    private void CreateGround(float viewWidth)
    {
        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(environmentRoot, false);
        SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = groundSprite;
        renderer.sortingOrder = 2;

        float groundWidth = groundSprite.bounds.size.x;
        float groundHeight = groundSprite.bounds.size.y;
        ground.transform.position = new Vector3(0f, -4.35f, 0f);
        ground.transform.localScale = new Vector3(viewWidth / groundWidth, 1f, 1f);

        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(groundWidth, groundHeight * 0.22f);
        collider.offset = new Vector2(0f, groundHeight * 0.39f - groundColliderInset);
    }

    private void ResetRound()
    {
        state = GameState.Waiting;
        score = 0;
        spawnTimer = 0.7f;
        gameOverReason = string.Empty;

        foreach (PipePair pipe in pipes)
        {
            if (pipe != null)
            {
                Destroy(pipe.gameObject);
            }
        }
        pipes.Clear();

        if (bird != null)
        {
            Destroy(bird.gameObject);
        }

        GameObject birdObject = new GameObject("Bird");
        birdObject.transform.SetParent(gameplayRoot, false);
        birdObject.transform.position = new Vector3(-2.5f, 0f, 0f);
        birdObject.transform.localScale = Vector3.one * birdScale;
        SpriteRenderer renderer = birdObject.AddComponent<SpriteRenderer>();
        renderer.sprite = birdFrames[0];
        renderer.sortingOrder = 4;
        bird = birdObject.AddComponent<CharacterMenager>();
        bird.Initialize(this, birdFrames);
    }

    private void SpawnPipe()
    {
        float gapCenter = Random.Range(-0.9f, 0.9f);
        GameObject pipeObject = new GameObject("Pipe Pair");
        pipeObject.transform.SetParent(pipeContainer);
        pipeObject.transform.position = new Vector3(gameCamera.orthographicSize * gameCamera.aspect + 1f,
            gapCenter, 0f);

        PipePair pipe = pipeObject.AddComponent<PipePair>();
        pipe.Initialize(this, upperPipeSprite, lowerPipeSprite, gapSize, pipeScale);
        pipes.Add(pipe);
    }

    private static void DrawMessage(string title, string subtitle)
    {
        float width = Mathf.Min(Screen.width * 0.86f, 640f);
        float height = 154f;
        Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.32f, width, height);
        GUI.Box(panel, GUIContent.none);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.width / 22f, 24f, 40f)),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.width / 48f, 14f, 22f)),
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(panel.x + 12f, panel.y + 18f, panel.width - 24f, 55f), title, titleStyle);
        GUI.Label(new Rect(panel.x + 20f, panel.y + 78f, panel.width - 40f, 58f), subtitle, subtitleStyle);
    }
}
