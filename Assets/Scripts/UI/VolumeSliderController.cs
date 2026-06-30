using UnityEngine;
using UnityEngine.UI;

// 볼륨 슬라이더 UI 제어 클래스
public class VolumeSliderController : MonoBehaviour
{
    // 제어 대상 슬라이더 컴포넌트
    public Slider volumeSlider;

    // 음향 제어 로직 참조
    public VolumeControlLogic logic;

    void Start()
    {
        RegisterSliderEvent();
    }

    // 슬라이더 값 변경 이벤트 등록 모듈
    private void RegisterSliderEvent()
    {
        if (volumeSlider == null || logic == null)
        {
            // 컴포넌트 누락 경고 출력
            Debug.LogWarning("필수 컴포넌트 누락");
            return;
        }

        // 기존 리스너 초기화
        volumeSlider.onValueChanged.RemoveAllListeners();

        // 람다식을 이용한 볼륨 조절 액션 추가 및 변경된 float 값 전달
        volumeSlider.onValueChanged.AddListener((float value) => logic.SetGlobalVolume(value));
    }
}