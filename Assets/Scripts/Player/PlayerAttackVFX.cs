using UnityEngine;

public class PlayerAttackVFX : MonoBehaviour
{
    [Header("出生点（Player 子物体）")]
    [SerializeField] Transform attackPoint;

    [Header("特效图")]
    [SerializeField] Sprite sAttackSprite;
    [SerializeField] Sprite jAttackSprite;

    [Header("显示")]
    [SerializeField] float effectDuration = 0.25f;
    [SerializeField] int sortingOrderOffset = 1;

    [Header("相对 AttackPoint 的偏移（角色朝右时）")]
    [SerializeField] Vector2 sAttackOffset = new(0.35f, 0.05f);
    [SerializeField] Vector2 mAttackOffset = new(0.2f, -0.1f);
    [SerializeField] Vector2 jAttackOffset = new(0.25f, 0.2f);

    [Header("S 攻击")]
    [SerializeField] float sAttackScaleY = 0.55f;

    [Header("空攻额外旋转（度）")]
    [SerializeField] float jAttackRotation = -35f;

    [Header("魔法飞行")]
    [SerializeField] GameObject mAttackPrefab;

    [Header("弓箭飞行")]
    [SerializeField] GameObject bAttackPrefab;
    [SerializeField] Vector2 bAttackOffset = new(0.3f, 0.1f);

    SpriteRenderer playerSprite;

    void Awake()
    {
        playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();
    }

    public void PlaySAttack() => Spawn(sAttackSprite, sAttackOffset, 0f, sAttackScaleY);

    public void PlayMAttack()
    {
        if (mAttackPrefab == null || attackPoint == null)
            return;

        bool facingRight = playerSprite != null && playerSprite.flipX;
        Vector3 spawnPos = attackPoint.TransformPoint(mAttackOffset);

        GameObject go = Instantiate(mAttackPrefab, spawnPos, Quaternion.identity);

        AttackMover mover = go.GetComponent<AttackMover>();
        if (mover == null)
            return;

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        int layerId = playerSprite != null ? playerSprite.sortingLayerID : 0;
        int order = playerSprite != null ? playerSprite.sortingOrder + sortingOrderOffset : sortingOrderOffset;

        mover.Init(dir, !facingRight, layerId, order);
    }

    public void PlayBAttack()
    {
        if (bAttackPrefab == null || attackPoint == null)
            return;

        bool facingRight = playerSprite != null && playerSprite.flipX;
        Vector3 spawnPos = attackPoint.TransformPoint(bAttackOffset);

        GameObject go = Instantiate(bAttackPrefab, spawnPos, Quaternion.identity);

        AttackMover mover = go.GetComponent<AttackMover>();
        if (mover == null)
            return;

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        int layerId = playerSprite != null ? playerSprite.sortingLayerID : 0;
        int order = playerSprite != null ? playerSprite.sortingOrder + sortingOrderOffset : sortingOrderOffset;

        mover.Init(dir, !facingRight, layerId, order);
    }

    public void PlayJAttack() => Spawn(jAttackSprite, jAttackOffset, jAttackRotation, 1f);

    void Spawn(Sprite sprite, Vector2 localOffset, float extraRotation, float scaleY)
    {
        if (sprite == null || attackPoint == null)
            return;

        bool facingRight = playerSprite != null && playerSprite.flipX;
        Vector2 offset = localOffset;
        float rotation = facingRight ? -extraRotation : extraRotation;

        var effect = new GameObject("AttackEffect");
        effect.transform.SetParent(attackPoint, false);
        effect.transform.localPosition = offset;
        effect.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        effect.transform.localScale = new Vector3(1f, scaleY, 1f);

        var sr = effect.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        if (playerSprite != null)
        {
            sr.sortingLayerID = playerSprite.sortingLayerID;
            sr.sortingOrder = playerSprite.sortingOrder + sortingOrderOffset;
        }

        if (!facingRight)
            sr.flipX = true;

        Destroy(effect, effectDuration);
    }
}
