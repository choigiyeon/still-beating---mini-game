using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 가까이에서 Space로 사용하는 오브젝트의 공통 부모 (서랍, 침대 등).
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    static readonly List<Interactable> all = new();

    public abstract bool CanInteract { get; }
    public abstract void Interact(PlayerStatus player);

    protected virtual void OnEnable() => all.Add(this);
    protected virtual void OnDisable() => all.Remove(this);

    /// <summary>position에서 range 안에 있는, 사용 가능한 가장 가까운 오브젝트.</summary>
    public static Interactable FindNearest(Vector2 position, float range)
    {
        Interactable nearest = null;
        float best = range;
        foreach (var item in all)
        {
            if (!item.CanInteract) continue;
            var col = item.GetComponent<Collider2D>();
            Vector2 point = col != null ? col.ClosestPoint(position) : (Vector2)item.transform.position;
            float distance = Vector2.Distance(position, point);
            if (distance <= best)
            {
                best = distance;
                nearest = item;
            }
        }
        return nearest;
    }
}
