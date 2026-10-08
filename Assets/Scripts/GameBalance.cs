using System;
using UnityEngine;

/// <summary>
/// 게임 밸런스 수치 모음. Chapter1 씬의 GameManager 인스펙터에서 수정한다.
/// </summary>
[Serializable]
public class GameBalance
{
    static GameBalance current;
    public static GameBalance Current
    {
        get => current ??= new GameBalance();
        set => current = value;
    }

    [Header("플레이어")]
    [Tooltip("이동 속도 (유닛/초)")]
    public float playerMoveSpeed = 4f;
    [Tooltip("서랍/침대와 상호작용 가능한 거리 (오브젝트 가장자리 기준)")]
    public float interactRange = 0.9f;

    [Header("심박수")]
    public float startHeartRate = 80f;
    public float minHeartRate = 60f;
    [Tooltip("심박수 최대치. 여기서 더 오르지 않고, 이 상태가 유지되면 게임오버")]
    public float dangerHeartRate = 180f;
    [Tooltip("최대 심박수를 몇 초 유지하면 게임오버인지")]
    public float gameOverHoldSeconds = 3f;
    [Tooltip("허기가 0일 때 초당 심박수 증가량")]
    public float starvingPerSecond = 2f;
    [Tooltip("적 범위 밖에 있을 때 초당 심박수 감소량 (허기가 0이면 감소하지 않음)")]
    public float recoveryPerSecond = 1.5f;
    [Tooltip("적 범위에서 벗어난 뒤 심박수가 내려가기 시작할 때까지 기다리는 시간 (초)")]
    public float recoveryDelay = 2f;
    [Tooltip("적을 피해서 내려갈 수 있는 최저 심박수")]
    public float restingHeartRate = 80f;

    [Header("허기 / 음식")]
    [Tooltip("허기 최대치 (반 칸 단위). 10 = 닭다리 5개")]
    public int maxHungerHalves = 10;
    [Tooltip("몇 초마다 허기가 반 칸씩 줄어드는지")]
    public float hungerDecayInterval = 15f;
    [Tooltip("음식 1개를 먹으면 회복되는 허기 (반 칸 단위)")]
    public int foodRestoreHalves = 4;
    [Tooltip("주머니에 보관 가능한 음식 수")]
    public int pocketCapacity = 2;

    [Header("적")]
    [Tooltip("바깥(초록) 범위 반지름")]
    public float enemyOuterRadius = 2.5f;
    [Tooltip("안쪽(주황) 범위 반지름")]
    public float enemyInnerRadius = 1.2f;
    [Tooltip("바깥 범위 안에 있을 때 초당 심박수 증가량")]
    public float enemyOuterPerSecond = 4f;
    [Tooltip("안쪽 범위 안에 있을 때 초당 심박수 증가량")]
    public float enemyInnerPerSecond = 10f;
    public float wanderSpeed = 1.8f;
    public float chaseSpeed = 1.2f;
    [Tooltip("랜덤 이동 적이 방향을 바꾸는 간격 (최소~최대 초)")]
    public Vector2 wanderChangeInterval = new Vector2(1f, 3f);

    [Header("오브젝트")]
    [Tooltip("문이 열리기까지 시간 (초)")]
    public float doorOpenSeconds = 300f;
    [Tooltip("음식서랍에서 음식을 꺼낸 뒤 다시 꺼낼 수 있을 때까지 (초)")]
    public float drawerCooldownSeconds = 90f;
    [Tooltip("침대 사용 시 심박수 감소량")]
    public float bedHeartRateReduce = 15f;
}
