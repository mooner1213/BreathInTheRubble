# 여진 강도 상승 보정 — 2026-11-10

이전 버전은 최대 공통 변위 2cm, 회전 0.12도로, 강도가 증가해도 화면상의 차이가 작았다. 최대 공통 변위를 5.5cm, 회전을 0.7도로 높였다. 0–5초 상승 곡선은 SmoothStep의 제곱으로 바꿔 처음에는 작게 시작하고 5초까지 더 큰 차이로 증가하도록 했다. 5–10초 최대, 10–20초 감쇠는 유지했다.

[최종 고정 시점 영상](ReviewImages/AftershockStrength/Episode.mp4) · [수치 요약](ReviewImages/AftershockStrength/Summary.json) · [전체 재생 측정](ReviewImages/AftershockStrength/Runtime.json)

## 확인한 변화

| 항목 | 이전 | 변경 후 |
| --- | --- | --- |
| 최대 설정 변위 | 2cm | 5.5cm |
| 최대 설정 회전 | 0.12도 | 0.7도 |
| 바닥 5곳 중 실제 최대 변위 | 2.56cm | 7.68cm |

바닥의 실제 변위는 회전과 이동을 합친 결과이며, 같은 월드 기준 측정에서 약 3배로 증가했다. 초반 0–2초 공통 변위 RMS는 1.56mm, 2–5초는 22.43mm, 최대 구간 5–10초는 35.55mm였다. 마지막 18–20초는 1.74mm로 감소했고 20초 이후 공통 운동은 0이었다. 이는 맵 움직임의 측정값이며 VR 체감 강도를 객관적으로 보증하는 수치는 아니다.

강도 증가가 파괴를 초반으로 당기지 않도록 하중의 가속도 보정 기준을 1.4에서 3.85m/s²로 조정했다. 하중은 계속 실제 입력 가속도에서 계산하며, 시간 비교로 파편을 해제하지 않는다. 정상 재생에서는 첫 파괴 6.02초, 마지막 9.67초로 5–10초 범위를 유지했고 5초 전 큰 파괴는 없었다. 진동 0 대조군의 20.3초 재생에서는 파괴·보조 파편 낙하·강관 해제가 모두 없었다.

## 검증과 한계

- Unity Play Mode의 실제 프레임 4,407개가 진행된 최종 재생 검증 통과. 먼지 30초 이후 소멸, 리셋 통과, 큰 파편 접촉체 내부의 머리 위치 표본 0.
- 고정 시점 PC 검사에서 머리·손 위치 변화 0, 머리 회전 변화 0. 물리 HMD·양손 추적을 인위적으로 흔드는 코드는 추가하지 않았다.
- 첫 재생은 편집기 시선 입력이 섞여 실패했다. 이 결과를 통과로 바꾸지 않고 `Runtime_InputAffected.json`, `Episode_InputAffected.mp4`로 보존했다. 최종 재생은 기존 데스크톱 어댑터가 GameView에서만 마우스 입력을 받는 조건을 이용해 SceneView에 포커스를 두고 측정했으며, 종료 후 기존 포커스를 복원했다.
- 영상은 960×720, 20fps, 630프레임, 31.5초 무음 MP4다. 저장 후 디코딩을 확인했다. 녹화 프레임 수는 VR 기기 성능 수치가 아니다.
- 최종 컴파일·콘솔 오류 0. 콘솔 경고 2개는 보존했다. 실제 헤드셋의 체감·멀미·기기 성능은 미검증이다.
- 낙하물의 최종 자세와 강관 접근성, 가까운 휴대폰 조명의 과노출 등 이전 보고서의 남은 한계를 해결했다고 주장하지 않는다.

[진동 0 대조군](ReviewImages/AftershockStrength/ZeroExcitation.json) · [리셋](ReviewImages/AftershockStrength/Reset.json) · [영상 정보](ReviewImages/AftershockStrength/Movie.json) · [컴파일](ReviewImages/AftershockStrength/Compilation.json) · [콘솔](ReviewImages/AftershockStrength/ConsoleErrors.json) · [종료 상태](ReviewImages/AftershockStrength/EndState.json)

## 변경 파일

- `Assets/_Project/Scripts/AftershockEnvironmentMotion.cs`: 최대 이동·회전 범위, 하중 보정 기준.
- `Assets/_Project/Scripts/AftershockEnvelope.cs`: 0–5초 상승 곡선.
- `Assets/_Project/Scenes/ConfinedPrototype.unity`: 실제 적용값 저장.
- `AgentScripts/InspectAftershockStrength.cs`, `BuildAftershockStrength.cs`, `VerifyAftershockStrength.cs`, `CheckAftershockStrengthMovie.cs`, 이 보고서와 검증 자료.

변경 전 두 소스와 씬은 `AgentScripts/Backups/AftershockStrength_20261110/`에 복사했다. 기존 에셋·UI·XR 구성·패키지는 보존했다. Unity CLI와 기존 Play Mode 검증 경로를 사용했으며, 작업은 이 진동 강도 조정으로 한정했다.
