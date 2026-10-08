using UnityEngine;

/// <summary>
/// 심박수, 허기, 주머니(음식) 관리.
/// </summary>
public class PlayerStatus : MonoBehaviour
{
    public float HeartRate { get; private set; }
    /// <summary>허기 (반 칸 단위, 0 ~ maxHungerHalves)</summary>
    public int HungerHalves { get; private set; }
    public int FoodCount { get; private set; }
    /// <summary>위험 심박수 이상으로 머문 시간</summary>
    public float OverLimitTime { get; private set; }
    public bool InEnemyRange { get; private set; }
    public bool IsStarving => HungerHalves <= 0;

    float hungerTimer;
    float safeTime; // 적 범위 밖에 머문 시간

    void Start()
    {
        var b = GameBalance.Current;
        HeartRate = b.startHeartRate;
        HungerHalves = b.maxHungerHalves;
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        var b = GameBalance.Current;
        float dt = Time.deltaTime;

        UpdateHunger(b, dt);

        // 적 범위: 겹치는 적이 여러 마리면 증가량이 합산된다
        float gain = 0f;
        foreach (var enemy in Enemy.All)
            gain += enemy.HeartRateGainAt(transform.position);
        InEnemyRange = gain > 0f;

        if (IsStarving) gain += b.starvingPerSecond;

        // 적 범위 밖 + 허기 있음 → 잠시 뒤 평상시 심박수까지 서서히 내려간다
        safeTime = gain > 0f ? 0f : safeTime + dt;
        if (gain > 0f)
            ChangeHeartRate(gain * dt);
        else if (safeTime >= b.recoveryDelay && HeartRate > b.restingHeartRate)
            HeartRate = Mathf.Max(b.restingHeartRate, HeartRate - b.recoveryPerSecond * dt);

        // 최대치(180)에 도달한 상태로 3초 유지되면 게임오버. 침대 등으로 내려가면 다시 0부터
        if (HeartRate >= b.dangerHeartRate)
        {
            OverLimitTime += dt;
            if (OverLimitTime >= b.gameOverHoldSeconds)
                GameManager.Instance.GameOver($"심박수 {b.dangerHeartRate:0}이 {b.gameOverHoldSeconds:0}초 동안 유지되었다...");
        }
        else
        {
            OverLimitTime = 0f;
        }
    }

    void UpdateHunger(GameBalance b, float dt)
    {
        if (IsStarving) return;
        hungerTimer += dt;
        if (hungerTimer < b.hungerDecayInterval) return;

        hungerTimer -= b.hungerDecayInterval;
        HungerHalves--;
    }

    public void ChangeHeartRate(float amount)
    {
        var b = GameBalance.Current;
        HeartRate = Mathf.Clamp(HeartRate + amount, b.minHeartRate, b.dangerHeartRate);
    }

    /// <summary>주머니에 음식 추가. 가득 차 있으면 false.</summary>
    public bool TryAddFood()
    {
        if (FoodCount >= GameBalance.Current.pocketCapacity) return false;
        FoodCount++;
        return true;
    }

    public bool TryEatFood()
    {
        if (!GameManager.Instance.IsPlaying || FoodCount <= 0) return false;

        var b = GameBalance.Current;
        FoodCount--;
        HungerHalves = Mathf.Min(b.maxHungerHalves, HungerHalves + b.foodRestoreHalves);
        return true;
    }
}
