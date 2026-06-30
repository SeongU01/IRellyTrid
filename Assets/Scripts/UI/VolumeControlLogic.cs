using UnityEngine;

// 전체 음향 제어 로직 클래스
public class VolumeControlLogic : MonoBehaviour
{
    // 글로벌 볼륨 수치 설정 모듈
    public void SetGlobalVolume(float volumeValue)
    {
        // AudioListener의 마스터 볼륨 값 변경
        AudioListener.volume = volumeValue;
    }
}