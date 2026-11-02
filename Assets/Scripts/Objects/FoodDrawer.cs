using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 음식서랍. 처음엔 00:00(사용 가능) 상태이고, 음식을 꺼내면 쿨타임(기본 1분 30초)이 돈다.
/// 주머니가 가득 차 있으면 꺼낸 음식은 버려지고 쿨타임은 그대로 시작된다.
/// </summary>
public class FoodDrawer : Interactable
{
    public static readonly List<FoodDrawer> All = new();

    [Tooltip("서랍 아래 디지털 타이머 텍스트")]
    [SerializeField] Text timerText;

    public float Remaining { get; private set; }
    public bool IsReady => Remaining <= 0f;

    public override bool CanInteract => IsReady;

    protected override void OnEnable()
    {
        base.OnEnable();
        All.Add(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        All.Remove(this);
    }

    void Update()
    {
        if (Remaining > 0f) Remaining = Mathf.Max(0f, Remaining - Time.deltaTime);
        if (timerText != null) timerText.text = GameHUD.FormatTime(Remaining);
    }

    public override void Interact(PlayerStatus player)
    {
        if (!IsReady) return;
        player.TryAddFood(); // 가득 차 있으면 버려짐
        Remaining = GameBalance.Current.drawerCooldownSeconds;
    }
}
