using UnityEngine;

public class AttackMover : MonoBehaviour
{
    [SerializeField] float speed = 8f;
    [SerializeField] float lifeTime = 2f;

    Vector2 direction;

    public void Init(Vector2 dir, bool flipX, int sortingLayerId, int sortingOrder)
    {
        direction = dir.normalized;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = flipX;
            sr.sortingLayerID = sortingLayerId;
            sr.sortingOrder = sortingOrder;
        }

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }
}