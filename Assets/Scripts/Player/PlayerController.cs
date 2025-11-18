using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public GameObject breakDustEffect;

    // --- 설정 가능한 변수 ---
    public float jumpForce = 6f;
    public float slideDuration = 0.6f;

    public float baseUpdateRate = 5.0f;

    [Header("인식 임계값 (미터) - HipCenter 기준")]
    // 🚨 최종 수정: 점프 임계값을 2cm로 낮춰 인식률 극대화
    public float jumpOffset = 0.02f;
    public float slideOffset = 0.06f;
    public float standUpOffset = 0.08f;

    // 🚨 NEW: 무릎 기준 높이 (점프 보조용)
    private float baseKneeY = 0f;
    public float kneeLiftOffset = 0.05f; // 무릎이 5cm 이상 올라가면 점프 보조 조건 충족

    // --- 상태 및 기준 변수 ---
    private KinectManager km;
    private Rigidbody2D rb;

    private bool isGrounded = true;
    private bool isSliding = false;
    private bool jumpTriggered = false;
    private bool calibrated = false;
    private bool isExecutingAction = false;
    private bool isJumpCooldown = false;

    private float baseHipY = 0f;

    private Vector3 originalScale;

    private int debugFrameCount = 0;
    private const int DebugLogInterval = 15;

    private bool isGameOver = false;
    public GameObject gameOverUI;

    public ScoreManager scoreManager;

    public bool isBoosted = false;

    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip itemSound;
    public AudioClip gameOverSound;
    private AudioSource audioSource;


    public bool isInvincible = false;
    public float invincibleDuration = 5f;

    private Coroutine invincibleRoutine;

    public GameObject powerAuraEffect;

    public GameObject scorePopup;

    public GameObject itemSparkEffect;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;

        km = KinectManager.Instance;

        StartCoroutine(CalibrateStandingPose());
        audioSource = GetComponent<AudioSource>();

    }

    IEnumerator CalibrateStandingPose()
    {
        Time.timeScale = 0f;
        Debug.Log("🔄 캘리브레이션 시작: 1초 동안 움직이지 말고 서 주세요. (게임 일시 정지)");

        calibrated = false;

        yield return new WaitForSecondsRealtime(0.1f);

        while (km == null || !km.IsInitialized())
        {
            Debug.Log("키넥트 초기화 대기 중...");
            km = KinectManager.Instance;
            yield return null;
        }

        uint userId = 0;

        while (userId == 0)
        {
            userId = km.GetPlayer1ID();
            string userDetectedStatus = (km != null && km.IsUserDetected()) ? "감지됨" : "미감지";
            Debug.LogFormat("유저 ID 감지 대기 중... 현재 ID: {0}. 사용자 상태: {1}", userId, userDetectedStatus);

            yield return null;
        }

        // 기준 높이 초기화
        baseHipY = 0f;
        baseKneeY = 0f; // 🚨 NEW: 무릎 기준 높이도 초기화

        const int calibrationFrames = 20;
        List<float> hipYValues = new List<float>();
        List<float> kneeYValues = new List<float>(); // 🚨 NEW: 무릎 Y 값 목록
        int count = 0;

        Debug.LogFormat("✅ 유저 감지 완료. ID: {0}. HipY 데이터 수집 시작.", userId);

        while (count < calibrationFrames)
        {
            Vector3 hip = km.GetJointPosition(userId, (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter);
            Vector3 knee = km.GetJointPosition(userId, (int)KinectWrapper.NuiSkeletonPositionIndex.KneeLeft); // 🚨 NEW: 무릎 조인트 위치

            if (hip.y > 0.01f && knee.y > 0.01f)
            {
                hipYValues.Add(hip.y);
                kneeYValues.Add(knee.y); // 🚨 NEW: 무릎 값 수집
                count++;
            }
            yield return null;
        }

        if (hipYValues.Count > 0)
        {
            float sumHip = 0;
            foreach (float y in hipYValues) sumHip += y;
            baseHipY = sumHip / hipYValues.Count;

            float sumKnee = 0;
            foreach (float y in kneeYValues) sumKnee += y;
            baseKneeY = sumKnee / kneeYValues.Count; // 🚨 NEW: 무릎 기준 높이 계산

            calibrated = true;

            isGrounded = true;
            isExecutingAction = false;
            jumpTriggered = false;

            Debug.Log("✅ 캘리브레이션 완료.");
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "기준 HipY = {0:F3}m | 기준 KneeY = {1:F3}m", baseHipY, baseKneeY));
        }
        else
        {
            Debug.LogError("캘리브레이션 실패: 유효한 스켈레톤 데이터를 수집하지 못했습니다.");
            yield return new WaitForSecondsRealtime(1f);
            StartCoroutine(CalibrateStandingPose());
        }

        Time.timeScale = 1f;
        Debug.Log("게임 재개: 캘리브레이션 완료.");
    }


    void Update()
    {

        if (!calibrated || km == null || !km.IsInitialized() || !km.IsUserDetected())
            return;

        uint user = km.GetPlayer1ID();
        Vector3 hip = km.GetJointPosition(user, (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter);
        Vector3 knee = km.GetJointPosition(user, (int)KinectWrapper.NuiSkeletonPositionIndex.KneeLeft); // 🚨 NEW: 무릎 조인트 위치

        float hipY = hip.y;
        float kneeY = knee.y; // 🚨 NEW: 무릎 Y 값

        // 트래킹 지연/실패 시 동작 감지 방지
        if (hipY < 0.1f || kneeY < 0.1f)
        {
            return;
        }

        // 0. 기준 높이 부드럽게 업데이트 (현재 동작 중이 아닐 때만)
        if (!isExecutingAction)
        {
            // Hip과 Knee의 기준 높이를 모두 현재 위치에 적응시킵니다.
            baseHipY = Mathf.Lerp(baseHipY, hipY, Time.deltaTime * baseUpdateRate);
            baseKneeY = Mathf.Lerp(baseKneeY, kneeY, Time.deltaTime * baseUpdateRate); // 🚨 NEW: 무릎 기준도 업데이트
        }

        // ==========================================================
        // 디버그 로깅
        // ==========================================================
        if (calibrated && !isSliding && !jumpTriggered)
        {
            debugFrameCount++;
            if (debugFrameCount >= DebugLogInterval)
            {
                debugFrameCount = 0;
                float liftDiff = hipY - baseHipY;
                float squatDiff = baseHipY - hipY;
                float kneeLiftDiff = kneeY - baseKneeY; // 🚨 NEW: 무릎 상승 차이

                string logMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "DEBUG - Hip Y: {0:F3}m | Lift Diff: {1:F3}m (Req: >{2:F3}m Jump) | Knee Diff: {3:F3}m (Req: >{4:F3}m) | Squat Diff: {5:F3}m (Req: >{6:F3}m Slide) | Action: {7}",
                    hipY,
                    liftDiff,
                    jumpOffset,
                    kneeLiftDiff,
                    kneeLiftOffset,
                    squatDiff,
                    slideOffset,
                    isExecutingAction
                );
                Debug.Log(logMessage);
            }
        }

        // ==========================================================
        // 1. 슬라이드 상태 관리 (연속 유지 및 해제)
        // ==========================================================
        if (isSliding)
        {
            if (hipY > baseHipY - standUpOffset)
            {
                StopSlide();
                return;
            }
            else
            {
                return;
            }
        }

        // ==========================================================
        // 2. 새로운 동작 감지 (슬라이드 시작 또는 점프)
        // ==========================================================

        if (isExecutingAction || isJumpCooldown) return;

        // --- 슬라이드 시작 조건 --- (땅에 닿아있고, Hip이 기준보다 낮을 때)
        float squatDifference = baseHipY - hipY;
        if (isGrounded && squatDifference > slideOffset)
        {
            StartCoroutine(StartSlideCoroutine());
            return;
        }

        // --- 점프 조건 --- (Hip 상승 + 무릎 상승 보조)
        float liftDifference = hipY - baseHipY;
        float kneeLiftDifference = kneeY - baseKneeY; // 🚨 NEW

        // 🚨 최종 점프 로직: Hip이 2cm 상승 '또는' (Hip이 1cm 상승 + 무릎이 5cm 상승)
        bool hipJumpCondition = liftDifference > jumpOffset;
        bool kneeAssistedJumpCondition = (liftDifference > 0.01f && kneeLiftDifference > kneeLiftOffset);

        if (isGrounded && !jumpTriggered && (hipJumpCondition || kneeAssistedJumpCondition) || Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteJump();
        }

        // ==========================================================
        // 3. 점프 중복 방지 초기화
        // ==========================================================

        if (jumpTriggered && hipY < baseHipY + 0.05f)
            jumpTriggered = false;
    }

    private void ExecuteJump()
    {
        audioSource.PlayOneShot(jumpSound);

        isExecutingAction = true;
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        isGrounded = false;
        jumpTriggered = true;

        float currentHipY = km.GetJointPosition(km.GetPlayer1ID(), (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter).y;
        Debug.Log(string.Format(CultureInfo.InvariantCulture, "⬆ 점프 감지: HipY={0:F3}m", currentHipY));

        StartCoroutine(JumpCooldownRoutine(0.3f));
    }

    IEnumerator JumpCooldownRoutine(float duration)
    {
        isJumpCooldown = true;

        yield return new WaitForSeconds(duration);
        isJumpCooldown = false;
        isExecutingAction = false;
    }


    IEnumerator StartSlideCoroutine()
    {
        isExecutingAction = true;
        isSliding = true;
        jumpTriggered = true;

        float currentHipY = km.GetJointPosition(km.GetPlayer1ID(), (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter).y;
        Debug.Log(string.Format(CultureInfo.InvariantCulture, "⬇ 슬라이드 시작: HipY={0:F3}m", currentHipY));

        audioSource.PlayOneShot(slideSound);

        transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.5f, originalScale.z);

        yield break;
    }

    private void StopSlide()
    {
        transform.localScale = originalScale;
        isSliding = false;
        jumpTriggered = false;
        isExecutingAction = false;
        Debug.Log("▶ 슬라이드 종료: 다시 서 있는 상태로 복귀");
    }


    // --- 물리 충돌 처리 ---

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (c.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;

            if (jumpTriggered)
            {
                jumpTriggered = false;
            }
        }

        else if (c.gameObject.CompareTag("Obstacle"))
        {
            if (isInvincible)
            {
                Instantiate(breakDustEffect, c.transform.position, Quaternion.identity);

                Destroy(c.gameObject);
            }
            else
            {
                isGameOver = true;
                GameOver();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item"))
        {
            Vector3 popupPos = transform.position + Vector3.up * 1f;
            Instantiate(scorePopup, popupPos, Quaternion.identity);
            Instantiate(itemSparkEffect, transform.position, Quaternion.identity);

            Destroy(other.gameObject);
            scoreManager.score += 50f;
            Debug.Log("아이템 획득!");
            audioSource.PlayOneShot(itemSound);
        }
    }

    void GameOver()
    {
        Debug.Log("GAME OVER!");
        Time.timeScale = 0f;

        gameOverUI.SetActive(true);
        scoreManager.StopScore();
        scoreManager.SaveBestScore();
        audioSource.PlayOneShot(gameOverSound);
    }


    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ActivatePowerUp()
    {
        if (invincibleRoutine != null)
            StopCoroutine(invincibleRoutine);

        invincibleRoutine = StartCoroutine(InvincibleMode());
    }

    IEnumerator InvincibleMode()
    {
        isInvincible = true;
        transform.localScale = originalScale * 1.5f;

        GameObject aura = Instantiate(powerAuraEffect, transform.position, Quaternion.identity, transform);
        Destroy(aura, invincibleDuration);

        yield return new WaitForSeconds(invincibleDuration);
        transform.localScale = originalScale;
        isInvincible = false;
    }
}