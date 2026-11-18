using UnityEngine;

public class PipePair : MonoBehaviour
{
    private FlappyBirdGame game;
    private bool scored;

    public void Initialize(FlappyBirdGame gameController, Sprite upperSprite, Sprite lowerSprite,
        float gapSize, float pipeScale)
    {
        game = gameController;

        float pipeHeight = upperSprite.bounds.size.y * pipeScale;
        float offset = gapSize * 0.5f + pipeHeight * 0.5f;
        CreatePipe("Upper Pipe", upperSprite, offset, pipeScale);
        CreatePipe("Lower Pipe", lowerSprite, -offset, pipeScale);

        BoxCollider2D scoreTrigger = gameObject.AddComponent<BoxCollider2D>();
        scoreTrigger.isTrigger = true;
        scoreTrigger.size = new Vector2(0.12f, gapSize);
    }

    private void Update()
    {
        if (!game.IsPlaying)
        {
            return;
        }

        transform.Translate(Vector3.left * game.GetPipeSpeed() * Time.deltaTime, Space.World);
        if (transform.position.x < -game.ScreenTop * 2f)
        {
            game.RemovePipe(this);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!scored && other.GetComponent<CharacterMenager>() != null)
        {
            scored = true;
            game.AddPoint();
        }
    }

    private void CreatePipe(string pipeName, Sprite sprite, float yOffset, float pipeScale)
    {
        GameObject pipe = new GameObject(pipeName);
        pipe.transform.SetParent(transform, false);
        pipe.transform.localPosition = new Vector3(0f, yOffset, 0f);
        pipe.transform.localScale = new Vector3(pipeScale, pipeScale, 1f);

        SpriteRenderer renderer = pipe.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 3;

        BoxCollider2D collider = pipe.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(sprite.bounds.size.x * 0.72f, sprite.bounds.size.y * 0.92f);
    }
}
