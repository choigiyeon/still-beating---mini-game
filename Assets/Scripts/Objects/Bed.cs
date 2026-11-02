using UnityEngine;

/// <summary>
/// 침대. Space로 1회 사용하면 심박수가 감소하고, 사용 후에는 약간 어두워진다 (아트: 흰색 ver / 어두운 ver).
/// 사용 후 스프라이트를 넣으면 스프라이트를 바꾸고, 비워두면 색을 어둡게 한다.
/// </summary>
public class Bed : Interactable
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite usedSprite;

    bool used;

    public override bool CanInteract => !used;

    public override void Interact(PlayerStatus player)
    {
        if (used) return;
        used = true;

        player.ChangeHeartRate(-GameBalance.Current.bedHeartRateReduce);

        if (spriteRenderer == null) return;
        if (usedSprite != null)
        {
            spriteRenderer.sprite = usedSprite;
        }
        else
        {
            var c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r * 0.65f, c.g * 0.65f, c.b * 0.65f, c.a);
        }
    }
}
