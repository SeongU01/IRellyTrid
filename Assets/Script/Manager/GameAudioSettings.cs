using UnityEngine;

[CreateAssetMenu(
    fileName = "GameAudioSettings",
    menuName = "Game/Audio Settings")]
public sealed class GameAudioSettings : ScriptableObject
{
    public AudioClip dayOneToFiveBgm;
    public AudioClip daySixToNineBgm;
    public AudioClip dayStart;
    public AudioClip uiButtonClick;
    public AudioClip mouseClick;
    public AudioClip miniGameMistake;
    [Min(0f)] public float miniGameMistakeStartOffset = 0.15f;
    public AudioClip miniGameFailure;
    public AudioClip typing;
    public AudioClip sortingCorrect;
}
