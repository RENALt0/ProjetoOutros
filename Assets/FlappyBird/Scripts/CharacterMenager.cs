using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class CharacterMenager : MonoBehaviour
{
    [SerializeField] private float flapImpulse = 5.5f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float animationFrameDuration = 0.12f;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private FlappyBirdGame game;
    private Sprite[] animationFrames;
    private float animationTimer;
    private int animationFrame;
    private bool alive;

    public void Initialize(FlappyBirdGame gameController, Sprite[] frames)
    {
        game = gameController;
        animationFrames = frames;
        alive = true;

        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D hitbox = GetComponent<CircleCollider2D>();
        hitbox.radius = 0.72f;
        hitbox.isTrigger = false;
    }

    public void StartPlaying()
    {
        body.gravityScale = 1.5f;
    }

    private void Update()
    {
        if (!alive)
        {
            return;
        }

        if (WasFlapPressed())
        {
            if (!game.IsPlaying)
            {
                game.StartGame();
            }

            if (game.IsPlaying)
            {
                Flap();
            }
        }

        Animate();
        RotateTowardsVelocity();

        if (transform.position.y > game.ScreenTop - 0.3f)
        {
            game.EndGame("O passaro ultrapassou o limite superior da tela.");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (alive)
        {
            string otherCollider = collision.otherCollider != null
                ? collision.otherCollider.name
                : "colisor nao identificado";
            game.EndGame("Colisao com " + collision.gameObject.name + " (" + otherCollider + ").");
        }
    }

    public void Stop()
    {
        alive = false;
        body.velocity = Vector2.zero;
        body.simulated = false;
    }

    private void Flap()
    {
        body.velocity = new Vector2(0f, flapImpulse);
    }

    private void Animate()
    {
        if (animationFrames == null || animationFrames.Length == 0 || !game.IsPlaying)
        {
            return;
        }

        animationTimer += Time.deltaTime;
        if (animationTimer >= animationFrameDuration)
        {
            animationTimer = 0f;
            animationFrame = (animationFrame + 1) % animationFrames.Length;
            spriteRenderer.sprite = animationFrames[animationFrame];
        }
    }

    private void RotateTowardsVelocity()
    {
        if (!game.IsPlaying)
        {
            return;
        }

        float angle = Mathf.Clamp(body.velocity.y * 5f, -65f, 35f);
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private static bool WasFlapPressed()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetMouseButtonDown(0)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }
}
