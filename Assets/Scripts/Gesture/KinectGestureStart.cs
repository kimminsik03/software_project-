using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // SceneManagement 네임스페이스 추가

public class KinectGestureStart : MonoBehaviour
{
    private KinectManager kinectManager;
    private bool gameStarted = false;

    void Start()
    {
        // Kinect 초기화 및 사용자 감지 루틴을 코루틴으로 시작
        StartCoroutine(WaitForKinectAndDetectGesture());
    }

    IEnumerator WaitForKinectAndDetectGesture()
    {
        // 1. KinectManager 인스턴스가 존재하고 초기화될 때까지 대기
        while (KinectManager.Instance == null || !KinectManager.Instance.IsInitialized())
        {
            Debug.Log("⏳ Waiting for KinectManager to initialize...");
            // 0.5초 간격으로 확인
            yield return new WaitForSeconds(0.5f);
        }

        kinectManager = KinectManager.Instance;
        Debug.Log("✅ KinectManager initialized. Starting detection loop.");

        // 2. 사용자 감지 및 제스처 인식 루프 시작
        while (!gameStarted) // 게임이 시작되지 않았을 때만 반복
        {
            uint userId = (uint)kinectManager.GetPlayer1ID();

            if (userId == 0)
            {
                Debug.Log("🕒 No user detected yet...");
            }
            else
            {
                // 사용자 감지 확인
                Debug.Log("✅ User is tracked. ID: " + userId);

                // 3. RaiseRightHand 제스처 감지
                if (kinectManager.IsGestureDetected(userId, KinectGestures.Gestures.RaiseRightHand))
                {
                    Debug.Log("🎮 RaiseRightHand gesture detected! Starting game...");

                    // 게임 시작 플래그 설정
                    gameStarted = true;

                    // 씬 전환 함수 호출
                    StartGame();

                    // 코루틴 종료
                    yield break;
                }
            }

            // 0.5초 간격으로 다음 프레임 대기
            yield return new WaitForSeconds(0.5f);
        }
    }

    void StartGame()
    {
        // "Play" 씬으로 전환
        SceneManager.LoadScene("Play");
    }
}