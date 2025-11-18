using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public GameObject breakDustEffect; // 파괴 이펙트
    private const float STAND_UP_COOLDOWN_DURATION = 0.5f; // 점프 오인식 방지
    // --- 설정 가능한 변수 --- 
    public float jumpForce = 8f; // 점프 작용 힘(위로)
    public float slideDuration = 0.6f; // 슬라이드 지속 시간 

    public float baseUpdateRate = 5.0f; // 일반 업데이트 비율

    [Header("인식 임계값 (미터) - HipCenter 기준")]
    // 🚨 최종 수정: 점프 임계값을 2cm로 낮춰 인식률 극대화
    public float jumpOffset;
    public float slideOffset;
    public float standUpOffset; // 각각 임계값


    private float baseKneeY; // 무릎 Y 좌표 (보조용)
    public float kneeLiftOffset; // 무릎이 5cm 이상 올라가면 점프 보조 조건 충족

    // --- 상태 및 기준 변수 ---
    private KinectManager km; // 해당 API를 가져와서 사용
    private Rigidbody2D rb; // 리지드 바디 컴포넌트를 가져와서 사용

    private bool isGrounded = true; // 플레이어가 땅인지 확인용
    private bool isSliding = false; // 플레이어가 슬라이드 중인지 확인용
    private bool jumpTriggered = false; // 점프 트리거
    private bool calibrated = false; // 캘리브레이션인지 확인용
    private bool isExecutingAction = false; // 액션 실행여부
    private bool isJumpCooldown = false; // 점프 쿨다운
    private bool isStandUpCooldown = false;
    private float baseHipY = 0f; // 엉덩이 중앙 좌표

    private Vector3 originalScale; // 플레이어 기존 크기
    // 슬라이드 하고 원래 크기로 돌아가기 위함

    private int debugFrameCount = 0;
    // 디버깅 : 프레임 카운트 
    private const int DebugLogInterval = 15;
    // 디버깅 :로그 띄우는 간격

    private bool isGameOver = false; 
    // 게임 오버 했는지 장애물에 닿으면 true 
    public GameObject gameOverUI;
    // 게임 오버 UI 
    public ScoreManager scoreManager;
    // 점수 관리자 스크립트 참조
    public bool isBoosted = false;
    // 부스트 중인지 확인(부스트 아이템 아직 x)
   
    // 오디오 클립(오디오 출력 용도)
    public AudioClip jumpSound; // 점프할때나는 소리
    public AudioClip slideSound; // 슬라이드할때 나는 소리
    public AudioClip itemSound; // 아이템 획득 소리
    public AudioClip gameOverSound; // 게임 오버 소리
    private AudioSource audioSource; // 오디오 소스 참조


    public bool isInvincible = false; // 플레이어가 무적인지
    public float invincibleDuration = 5f; // 무적 지속시간 5초

    private Coroutine invincibleRoutine; // 무적 코루틴

    public GameObject powerAuraEffect; // 거대화 먹고 이펙트 발생

    public GameObject scorePopup; // 스코어 팝업 텍스트 띄우기 
    // 코인 먹을때 점수 오르는거 볼 수 있음

    public GameObject itemSparkEffect; // 아이템 먹을때 이펙트

    public bool hasShield = false; // 쉴드를 가지고 있는지
    public GameObject shieldEffectPrefab; // 쉴드 효과 프리팹
    private GameObject currentShieldEffect; //현재 쉴드 이펙트

    private Vector3 powerUpScale; // 거대화하고 난 크기
    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); // 게임 시작하면 바로 리지드바디 가져오기
        originalScale = transform.localScale; // 현재 크기를 여기다가 저장
        // 이는 슬라이드나 거대화때 원래 크기로 돌아오기 위함
        powerUpScale = originalScale * 1.5f;
        // 거대화 크기 초기화
        km = KinectManager.Instance;
        // 키넥트 매니저 참조 싱글톤


        StartCoroutine(CalibrateStandingPose());
        // 코루틴으로 서있는 자세 캘리브레이션
        audioSource = GetComponent<AudioSource>();
        // 오디오소스 컴포넌트 가져오기

    }

    IEnumerator CalibrateStandingPose()
    { // 캘리브레이션 코루틴 함수
        Time.timeScale = 0f; // 시간 크기(0 : 멈춤 상태)
        Debug.Log("🔄 캘리브레이션 시작: 1초 동안 움직이지 말고 서 주세요. (게임 일시 정지)");
        // 시작하자마자 감지
        calibrated = false;
        // 일단은 비활성화
        yield return new WaitForSecondsRealtime(0.1f);
        
        while (km == null || !km.IsInitialized())
        { // 키넥트가 초기화 안되면 if블록들어옴
            Debug.Log("키넥트 초기화 대기 중...");
            km = KinectManager.Instance; // 안되니 다시 가져오기
            yield return null;
        }

        uint userId = 0; // 유저 아이디(유저 식별용)
        // 식별되는 인원 없으니 바로 0
        while (userId == 0)  // 식별되는 인원없으면 실행
        { 
            userId = km.GetPlayer1ID(); // 키넥트 api함수에서 이걸 가져옴
            // 첫번째플레이어 아이디를 가져오는 용도
            string userDetectedStatus = (km != null && km.IsUserDetected()) ? "감지됨" : "미감지";
            // 문자열 변수에 감지됨 또는 미감지
            // 인원이 식별되면 감지됨, 없으면 미감지
            Debug.LogFormat("유저 ID 감지 대기 중... 현재 ID: {0}. 사용자 상태: {1}", userId, userDetectedStatus);
            // 디버깅용
            yield return null;
        }

        // 기준 높이 초기화
        baseHipY = 0f; // 기준 높이: 엉덩이 중앙
        baseKneeY = 0f; //무릎 기준 높이도 초기화

        const int calibrationFrames = 20; // 캘리브레이션 프레임
        List<float> hipYValues = new List<float>(); // 배열 말고 리스트 사용 
        List<float> kneeYValues = new List<float>(); 
        // 여러 관절 데이터를 여기다가 담음
        int count = 0; 

        Debug.LogFormat("✅ 유저 감지 완료. ID: {0}. HipY 데이터 수집 시작.", userId);
        // 감지완료하면 관절 데이터를 추적하기 시작함
        while (count < calibrationFrames) // 캘리브레이션 프레임이 카운트보다 높으면
        {
            Vector3 hip = km.GetJointPosition(userId, (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter); // 벡터3 엉덩이 중앙 관절변수에 스켈레톤 데이터 가져옴
            Vector3 knee = km.GetJointPosition(userId, (int)KinectWrapper.NuiSkeletonPositionIndex.KneeLeft); // 위와 동일

            if (hip.y > 0.01f && knee.y > 0.01f) // 엉덩이 중앙 무릎이 범위에 들어오면
            {
                hipYValues.Add(hip.y); 
                kneeYValues.Add(knee.y); // 엉덩이 중앙이랑 무릎 값 리스트에 추가 , 키넥트 수업 코드랑 비슷
                count++; // 카운트는 1씩 증가
            }
            yield return null;
        }

        if (hipYValues.Count > 0) // 크기가 0보다 커지게되면
        {
            float sumHip = 0; // 총 엉덩이 스켈레톤 
            foreach (float y in hipYValues) sumHip += y; // 탐지되는만큼 더함
            baseHipY = sumHip / hipYValues.Count; // 엉덩이 중앙 기준 높이 계산

            float sumKnee = 0;
            foreach (float y in kneeYValues) sumKnee += y;
            baseKneeY = sumKnee / kneeYValues.Count; // 🚨 NEW: 무릎 기준 높이 계산

            calibrated = true; // 엉덩이가 잡히면 인식은 한거니 캘리브레이션 온

            isGrounded = true; // 땅인지 확인
            isExecutingAction = false; // 액션 실행 비활성화
            jumpTriggered = false; // 점프 트리거 비활성화 (아직 인식 상태)

            Debug.Log("✅ 캘리브레이션 완료.");
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "기준 HipY = {0:F3}m | 기준 KneeY = {1:F3}m", baseHipY, baseKneeY)); // 캘리브레이션 좌표 가져옴
            // 대부분 값은 1.xxx

        }
        else
        {
            Debug.LogError("캘리브레이션 실패: 유효한 스켈레톤 데이터를 수집하지 못했습니다."); // 실패시 디버깅
            yield return new WaitForSecondsRealtime(1f); // 1초단위로 다시 서있는 자세 캘리브레이션 호출
            StartCoroutine(CalibrateStandingPose());
        }

        Time.timeScale = 1f; // 인식 완료했으니 게임 재개
        Debug.Log("게임 재개: 캘리브레이션 완료."); // 디버깅
    }


    void Update()
    {

        if (!calibrated || km == null || !km.IsInitialized() || !km.IsUserDetected()) // 캘리브레이션안되고, 키넥트 초기화안되고, 유저가 감지안되면
            return; // 게임 실행 x

        uint user = km.GetPlayer1ID(); // 실시간으로 플레이어 id를 가져옴
        Vector3 hip = km.GetJointPosition(user, (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter); // 엉덩이 중앙 스켈레톤 관절 좌표
        Vector3 knee = km.GetJointPosition(user, (int)KinectWrapper.NuiSkeletonPositionIndex.KneeLeft); // 무릎 조인트 위치

        float hipY = hip.y; // 점프 슬라이드만 구현이므로 y만 잡아옴
        float kneeY = knee.y; // 무릎 y

        // 트래킹 지연/실패 시 동작 감지 방지
        if (hipY < 0.1f || kneeY < 0.1f) // 기준값보다 낮으면 게임 실행안함
        {
            return;
        }

        // 기준 높이 부드럽게 업데이트 (현재 동작 중이 아닐 때만) - 시작할 때 비활성화라서 여기서 실행
        if (!isExecutingAction)
        {
            // Hip과 Knee의 기준 높이를 모두 현재 위치에 적응시킵니다.
            baseHipY = Mathf.Lerp(baseHipY, hipY, Time.deltaTime * baseUpdateRate); // Lerp는 보정함수라서 위치 보정
            baseKneeY = Mathf.Lerp(baseKneeY, kneeY, Time.deltaTime * baseUpdateRate); 
        }

        // ==========================================================
        // 디버그 로깅
        // ==========================================================
        if (calibrated && !isSliding && !jumpTriggered) //캘리브레이션됬지만 점프 슬라이드 모두 비활성화면
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
        if (isSliding) // 슬라이드 상태라면
        {
            if (hipY > baseHipY - standUpOffset) // 기준 엉덩이 관절보다 높아지면
            {
                StopSlide(); // 슬라이드 상태가 아니라서 멈춤
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

        if (isExecutingAction || isJumpCooldown || isStandUpCooldown) return;

        // --- 슬라이드 시작 조건 --- (땅에 닿아있고, Hip이 기준보다 낮을 때)
        float squatDifference = baseHipY - hipY;
        if (isGrounded && squatDifference > slideOffset)
        {
            StartCoroutine(StartSlideCoroutine()); // 슬라이드 실행
            return;
        }

        // --- 점프 조건 --- (Hip 상승 + 무릎 상승 보조)
        float liftDifference = hipY - baseHipY;
        float kneeLiftDifference = kneeY - baseKneeY; 

        //  최종 점프 로직: Hip이 2cm 상승 '또는' (Hip이 1cm 상승 + 무릎이 5cm 상승)
        bool hipJumpCondition = liftDifference > jumpOffset;
        bool kneeAssistedJumpCondition = (liftDifference > 0.01f && kneeLiftDifference > kneeLiftOffset);

        if (isGrounded && !jumpTriggered && (hipJumpCondition || kneeAssistedJumpCondition))
        {
            ExecuteJump();
        }

        // ==========================================================
        // 3. 점프 중복 방지 초기화
        // ==========================================================

        if (jumpTriggered && hipY < baseHipY + 0.05f)
            jumpTriggered = false;
    }

    private void ExecuteJump() // 점프 실행 함수
    {
        audioSource.PlayOneShot(jumpSound); // 오디오소스 실행/ 단한번 실행

        isExecutingAction = true; // 액션 실행 완
        rb.velocity = new Vector2(rb.velocity.x, 0); // 현재 벡터  속도 좌표 x를 저장
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);  // 점프 5만큼

        isGrounded = false; // 점프라면 땅 접지 상태 x
        jumpTriggered = true; // 점프 트리거는 o

        float currentHipY = km.GetJointPosition(km.GetPlayer1ID(), (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter).y; // 점프 시 관절데이터 가져와서 
        Debug.Log(string.Format(CultureInfo.InvariantCulture, "⬆ 점프 감지: HipY={0:F3}m", currentHipY)); // 점프가 되었는지 감지 디버깅

        StartCoroutine(JumpCooldownRoutine(0.3f)); // 코루틴으로 점프 쿨다운 호출(연속 점프 x)
    }

    IEnumerator JumpCooldownRoutine(float duration) 
    {
        isJumpCooldown = true; // 점프 쿨다운 o

        yield return new WaitForSeconds(duration);
        isJumpCooldown = false;// 시간 지나면 다시 비활성화되서 
        isExecutingAction = false; // 다시 점프 가능
    }


    IEnumerator StartSlideCoroutine() // 슬라이드 코루틴
    {
        isExecutingAction = true; // 동작 인식(개별적으로하는게 점프 중이면 슬라이드 x 슬라이ㅣ드 중이면 점프 x)
        isSliding = true; // 슬라이드 ㅇ
        jumpTriggered = true; // 점프 트리거 온 슬라이드 상태에서 점프 준비는 가능

        float currentHipY = km.GetJointPosition(km.GetPlayer1ID(), (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter).y; // 관절 데이터 받고
        Debug.Log(string.Format(CultureInfo.InvariantCulture, "⬇ 슬라이드 시작: HipY={0:F3}m", currentHipY)); // 디버깅 위치

        audioSource.PlayOneShot(slideSound); // 오디오 한번 재생

        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.5f, transform.localScale.z);
        yield break;
    }

    private void StopSlide()
    {
        if (isInvincible)
        {
            transform.localScale = powerUpScale; // 무적 상태라면 거대화된 크기(1.5배)로 복구
        }
        else
        {
            transform.localScale = originalScale; // 일반 상태라면 원래 크기로 복구
        } // 슬라이드 종료 후, 시작함수에서 가져온 기본 크기 가져옴 (복귀
        isSliding = false; // 슬라이딩 비활성
        jumpTriggered = false; // 점프 트리거 비활성
        isExecutingAction = false; // 행위 비활성
        Debug.Log("▶ 슬라이드 종료: 다시 서 있는 상태로 복귀");
        StartCoroutine(StandUpCooldownRoutine(STAND_UP_COOLDOWN_DURATION));
        // 일시적으로 점프 감지 방지
    }

    IEnumerator StandUpCooldownRoutine(float duration)
    {
        isStandUpCooldown = true;
        Debug.LogFormat("🕒 일어서기 쿨다운 시작 ({0:F2}s)", duration);
        yield return new WaitForSeconds(duration);
        isStandUpCooldown = false;
        Debug.Log("✔ 일어서기 쿨다운 종료. 점프 감지 재개.");
    }
    // --- 물리 충돌 처리 ---

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (c.gameObject.CompareTag("Ground")) // 땅이랑 닿으면
        {
            isGrounded = true; // 접지 상태 온

            if (jumpTriggered) // 점프 트리거 상태라면
            {
                jumpTriggered = false; // 비활성화
            } // 땅에서만 점프 가능
        }

        else if (c.gameObject.CompareTag("Obstacle"))
        { // 장애물이랑 부딪히면
            if (isInvincible) // 무적상태라면
            {
                Instantiate(breakDustEffect, c.transform.position, Quaternion.identity); // 장애물 부시는 효과 생성

                Destroy(c.gameObject); // 파.괴.
            } else if(hasShield) // 쉴드 상태라면
            {
                Debug.Log("쉴드 사용");
                UseShield(c.gameObject.transform.position); // 쉴드 사용
                // 매개변수로 위치 
                Instantiate(breakDustEffect, c.transform.position, Quaternion.identity);
                Destroy(c.gameObject); // 충돌한 장애물만 파괴
            }
            else // 무적상태가 아니면
            {
                isGameOver = true; // 게임 오버
                GameOver(); // 게임오버 호출 UI
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item")) // 아이템에 닿으면
        {
            Vector3 popupPos = transform.position + Vector3.up * 1f;
            // 팝업 텍스트 위로 업
            Instantiate(scorePopup, popupPos, Quaternion.identity);
            // 위로 생성
            Instantiate(itemSparkEffect, transform.position, Quaternion.identity);
            // 아이템 획득 효과
            Destroy(other.gameObject); // 아이템먹었으니 파괴처리
            scoreManager.score += 50f; // 점수 50점
            Debug.Log("아이템 획득!"); // 디버깅
            audioSource.PlayOneShot(itemSound); // 아이템 획득 소리
        }
    }

    void GameOver()
    {
        Debug.Log("GAME OVER!"); // 게임 오버
        Time.timeScale = 0f; // 멈춤

        gameOverUI.SetActive(true); // 게임 오버 UI 활성화
        scoreManager.StopScore(); // 점수 오르는거 멈춤
        scoreManager.SaveBestScore(); // 최고 점수 갱신
        audioSource.PlayOneShot(gameOverSound); // 끝날때나는소리
    }


    public void RestartGame()
    {
        Time.timeScale = 1f; // 게임 재개
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }   // 다시 로드

    public void ActivatePowerUp()
    { // 무적 파워업 활성화
        if (invincibleRoutine != null) // 무적상태가 아니라면
            StopCoroutine(invincibleRoutine); // 코루틴 멈춤

        invincibleRoutine = StartCoroutine(InvincibleMode());
        // 무적 5초 지속
    }

    IEnumerator InvincibleMode()
    { // 무적 상태
        isInvincible = true; // true
        transform.localScale = powerUpScale; // 기존 크기보다 1.5배 증가 거대화

        GameObject aura = Instantiate(powerAuraEffect, transform.position, Quaternion.identity, transform);
        Destroy(aura, invincibleDuration);
        // 오오라
        yield return new WaitForSeconds(invincibleDuration);
        if (!isSliding)
        {
            transform.localScale = originalScale; // 지속시간 지나면 되돌림
        } // 지속시간 지나면 되돌림
        isInvincible = false; // false.
    }

    public void ActivateShield() // 쉴드 활성
    {
        if (hasShield)
        {
            Debug.Log("쉴드가 이미 있습니다. 추가 점수로 변환");
            // 이미 쉴드가 있을 경우, 점수를 제공하거나 다른 보상을 줍니다.
            scoreManager.score += 100f;
        }
        else
        {
            hasShield = true;
            // 쉴드 시각 효과 생성 (플레이어에게 자식으로)
            if (shieldEffectPrefab != null)
            {
                currentShieldEffect = Instantiate(shieldEffectPrefab, transform.position, Quaternion.identity, transform);
                Debug.Log(" 쉴드 획득! 1회 방어 가능.");
            }
        }


    }

    private void UseShield(Vector3 obstaclePosition)
    {
        hasShield = false;

        // 쉴드 파괴 효과 (선택 사항: 충돌 위치에 폭발 파티클 등)
        if (currentShieldEffect != null)
        {
            // 쉴드 이펙트를 파괴 위치에서 분리
            currentShieldEffect.transform.SetParent(null);
            // 쉴드 파괴 시각 효과 실행 (예: 폭발 파티클)
            // 현재는 간단히 이펙트 오브젝트만 파괴합니다.
            Destroy(currentShieldEffect);
        }
     }
}