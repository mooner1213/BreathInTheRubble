# 여진 고조·붕괴·감쇠와 정면 여유 — 2026-11-10

사용자가 지정한 시간 흐름으로 기존 여진을 수정했다. 기존 개별 잔해의 서로 다른 반응은 유지하고 공통 강도 곡선을 곱한다. HMD나 컨트롤러의 추적 Transform을 흔드는 방식은 추가하지 않았다.

| 구간 | 동작 |
| --- | --- |
| 0–5초 | 진동과 구조물 저음이 서서히 커지고, 이음부에서 잔가루가 점차 많이 떨어짐 |
| 5–10초 | 최대 진동, 주된 파손·낙하, 분진과 빛을 산란하는 부유 먼지 |
| 10–20초 | 진동과 저음 감쇠, 20초에 진동 종료·Isolation 전환 |
| 10–30초 | 새 큰 먼지 공급을 멈추고 남은 입자·안개를 서서히 감소, 30초에 제거 |

## 배치와 낙하 방향

- 정면 층과 부착 조각을 **8cm 뒤로** 이동했다. 기준 눈 위치의 정면 표면 거리는 약 **0.650m → 0.730m**다. 카메라 위치·높이는 그대로다.
- 새 구조의 물리 파편 101개 해제 시점을 5.15–9.45초에 모았다. 별도 보조 파편 풀 84개도 5.2–9.3초에 맞췄다. 붙어 있던 정면 보조 조각 58개는 이동한 표면에 맞춰 함께 옮겼다.
- 큰 파편의 초기 속도는 플레이어를 겨냥하는 방향에서 좌우·바깥쪽으로 바꿨다. 초기 회전도 줄였고, 해제 이후에는 PhysX 중력·접촉이 이동을 결정한다. 정해진 착지 위치로 순간 이동시키지 않는다.
- 낙하용 천장 조각 4개는 크기를 기존의 72%로 줄이고 좌우·앞뒤로 5cm 분산했다. 그 뒤의 기존 매몰층은 유지했다. 중앙 수직 ray가 더 깊은 매몰층까지 닿는 부수 효과가 있으며 최종 상부 ray는 약 0.613m다. 정면 8cm 이동과 구분한다.
- 큰 파편 29개에는 기존 `AftershockFallingChunk`의 머리 주변 회피력 반경을 22cm로 설정했다. 파편에 힘을 가하는 기능이며 HMD 위치를 이동시키는 기능이 아니다. 기존 물리 머리 접촉면 자체는 바꾸지 않았다.

## 먼지

기존에 비활성 계층 아래 있던 잔가루 방출기 10개만 활성 계층으로 옮겼다. 숨겨둔 옛 잔해 메시나 XR 오브젝트를 다시 활성화하지 않았다. 작은 입자의 낙하 횟수는 진동 강도에 따라 증가한다.

부유 먼지는 기존 Light Scattering Dust 셰이더를 사용하는 새 파생 재질 `Assets/_Project/Art/AftershockTiming/PeakDust.mat`을 사용한다. 각성 단계의 공용 먼지 재질은 수정하지 않았다. 가까운 실제 공간에 입자를 분포시키고 과도한 광산란을 줄였다. 7초 무렵 최대 농도에 도달하고, 10초부터 입자 재질 불투명도와 공간 안개 농도를 함께 줄인다. 종료는 여진 시작을 기준으로 30초다.

## 실제 PC 재생 검증

- 약 32초 재생에서 새 구조 101개가 5초 이전에는 해제되지 않았고, 9.8초 표본에는 101개가 해제됐다.
- 초반 실제 잔가루 입자는 1초 10개, 3초 39개, 4.85초 94개로 관측됐다. 표본 시점의 수치이며 연출 전체 입자 총량은 아니다.
- 가까운 지지 부재 16개의 실제 이동·회전을 표본 측정했다. 최대 구간의 변화량이 초반보다 컸고, 마지막 감쇠 구간에서 줄었다. 20.3초 이후 지지 부재의 추가 움직임은 0이었다.
- 먼지 농도는 7.5초 1.65, 15초 약 1.37로 감소했고 30.3초에는 활성 먼지와 입자가 모두 0이었다. 저음은 강도에 따라 커지고 20초 후 AudioSource 재생이 정지했다.
- 카메라 위치·회전 변화 0, 큰 파편 내부의 머리 위치 표본 0. 실제 헤드셋 검증을 뜻하지 않는다.
- 활성 ParticleSystem 수는 재생 전후 **49 → 49**로 유지됐다. 기존 39개에 이번에 활성화한 잔가루 방출기 10개가 포함된다. 사건마다 시스템을 새로 생성하지 않는다.
- 파편·먼지 리셋 및 선 비활성 복원 통과. 구조 단계 API의 완료·개방·채광 검사 통과, 화이트아웃 0. 구조 60초 실제 대기나 전체 통화부터 구조까지의 검증은 아니다.
- 컴파일 오류와 최종 콘솔 오류는 0건이다. 기존 미사용 필드·XR 시뮬레이터 관련 경고는 유지했고 콘솔을 지우지 않았다.

[31.5초 전체 영상](ReviewImages/AftershockTiming/Episode.mp4) · [실제 재생 JSON](ReviewImages/AftershockTiming/Runtime.json) · [거리·물품 여유](ReviewImages/AftershockTiming/Audit.json) · [잔가루 방출기](ReviewImages/AftershockTiming/FineRain.json) · [리셋](ReviewImages/AftershockTiming/Reset.json) · [구조](ReviewImages/AftershockTiming/Rescue.json)

[초기 고조](ReviewImages/AftershockTiming/At_02.png) · [최대 구간](ReviewImages/AftershockTiming/At_05.png) · [감쇠 중](ReviewImages/AftershockTiming/At_09.png) · [먼지 소멸 뒤](ReviewImages/AftershockTiming/At_14.png)

영상은 실제 PC 카메라 위치·화각과 휴대폰 조명으로 저장한 **무음 20fps 파일**이다. 저장 프레임 수는 기기 성능 측정이 아니다. 마지막 영상은 기존 PC 머리 보호 미리보기를 켠 상태라 붉은 피격 테두리가 억제된다. 보호하지 않은 초기 검토 자료는 `Runtime_UnguardedDraft.json`, `Peak_UnguardedDraft.png`로 구분한다. 피격 효과나 보호 조건 자체는 변경하지 않았다.

## 파일과 복구 지점

- 신규 런타임: `Assets/_Project/Scripts/AftershockEnvelope.cs`와 메타 파일.
- 수정 런타임: `AftershockDirector.cs`, `RubbleStructuralCollapse.cs`, `AftershockDustEffect.cs`, `AftershockRubbleRain.cs`, `AftershockAudio.cs`.
- 씬: `Assets/_Project/Scenes/ConfinedPrototype.unity`.
- 신규 파생 재질: `Assets/_Project/Art/AftershockTiming/PeakDust.mat`과 관련 메타 파일.
- 도구·기록: `AgentScripts/BuildAftershockTiming.cs`, `InspectAftershockTiming.cs`, `VerifyAftershockTiming.cs`, 이 보고서 및 `ReviewImages/AftershockTiming/` 자료.
- 수정 전 코드·씬: `AgentScripts/Backups/AftershockTiming_20261110/`. 빌더의 초기 이동 단계와 후속 일회성 이동은 이미 적용됐으므로 재실행하지 않는다.

`AftershockRubbleRain.reconstructed-before.cs.txt`는 이 작업에서 바꾼 Emit 횟수 한 줄을 역적용해 복원한 이전 텍스트이며, 원본 파일의 바이트 단위 복사본과 구분한다. [최종 콘솔](ReviewImages/AftershockTiming/Console.json)과 [종료 상태](ReviewImages/AftershockTiming/EndState.json)도 보존했다. 종료 시 씬 dirty=false, 재생 중지, XR Origin과 Input Action Manager는 각각 1개다.

이번 작업은 UI·폰트·테마, XR Origin·Input Action Manager, 패키지·SDK를 수정하지 않았다. 실제 헤드셋의 시야·손 접근·기기 성능은 미검증이다. 좁은 공간의 실제 물리 접촉으로 최종 파편 자세가 달라지며, PC 휴대폰의 자동 거리 보정과 가까운 파편이 시야를 크게 차지하는 구간은 남는다. 이전 작업에서 확인한 파이프의 간헐적인 접근 범위 문제를 이번 변경으로 해결했다고 주장하지 않는다.

`unity-cli` 재생 확인 절차와 `craft-game-floors`의 vfx·visual-scorecard를 적용했다. 초기 캡처를 검토한 뒤 파편 분산, 먼지 깊이, 휴면 상태였던 잔가루 방출기를 보완했다.

palette discipline: 4/5 — 기존 회색 콘크리트·붉은 벽돌·회갈색 분진 계열 유지.

style uniformity (scoped): 3/5 — 원본 메시·불규칙 분리 면과 기존 먼지 셰이더 사용. 근접 과노출은 남는다.

feedback visibility (episode scope): 2/5 → 3/5 — 고조·주 붕괴·감쇠·먼지 소멸을 시간별 영상과 실제 상태로 구분했다. 공포감이나 VR 체감의 객관적 점수는 아니다.
