using UnityEngine;

/// <summary>
/// 침대. Space로 1회 사용하면 심박수가 감소하고, 사용 후에는 약간 어두워진다 (아트: 흰색 ver / 어두운 ver).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Bed : Interactable
{
    bool used;

    public override bool CanInteract => !used;

    public override void Interact(PlayerStatus player)
    {
        if (used) return;
        used = true;

        player.ChangeHeartRate(-GameBalance.Current.bedHeartRateReduce);

        var sr = GetComponent<SpriteRenderer>();
        var c = sr.color;
        sr.color = new Color(c.r * 0.65f, c.g * 0.65f, c.b * 0.65f, c.a);
    }
}
