# 상부 압박 잔해와 중력 낙하 개선 — 2026-11-06

## 최종 변경

- 분리 파편의 임의 피벗 회전을 제거했다. 부착 상태를 잠깐 유지한 뒤 Rigidbody를 분리하고 중력·충돌로 이동한다. 분리 후에는 Transform으로 위치를 계속 지정하지 않는다.
- 기존 5개와 추가 4개, 총 9개의 큰 분리 파편을 구성했다. 낙하 통로를 기존 받침의 끝에 맞추고 바닥 잔해를 보강하여 천장 부근에 걸리거나 바깥으로 굴러 이탈하는 경우를 줄였다. 최종 설정의 초기 직선 속도는 0이며 작은 초기 회전만 준다.
- 천장 바로 위에 유료 에셋의 콘크리트·블록·벽돌 112개를 맞물려 배치했다. 이 중 4개는 분리 파편으로 사용하고, 108개는 상부 하중층으로 유지한다. 하중층은 기존 천장과 함께 내려와 갈라진 틈 너머의 빈 공간을 채운다.
- 상부 틈 4곳에서 유료 파편 메시가 짧게 쏟아진다. 월드 좌표 입자, 중력, 3D 충돌을 사용하며 발생기는 재사용한다. 확인한 최대 동시 입자는 32개다.
- 유료 잔해로 착지면 4곳을 추가하고 기존 안쪽 외곽 잔해 12곳의 실제 메시 충돌면을 연결했다. 넓은 가짜 충돌 상자를 추가하지 않았다.
- 기존 천장 하강, 3곳의 균열, 파이프 경로와 네 장을 순서대로 제거하는 구조 엔딩을 유지했다. 원본 유료 에셋과 UI·XR 입력은 수정하지 않았다.

## 변경 파일

- `Assets/_Project/Scenes/ConfinedPrototype.unity`
- `Assets/_Project/Scripts/AftershockFallingChunk.cs`
- `Assets/_Project/Scripts/AftershockRubbleRain.cs` 및 메타 파일 — 새 컴포넌트
- `Assets/_Project/Art/StructuralAftershock/LooseConcreteContact.physicMaterial` 및 메타 파일 — 새 접촉 재질
- `AgentScripts/InspectPressureCollapse.cs`
- `AgentScripts/BuildPressureCollapse.cs`
- `AgentScripts/ProbePressureContacts.cs` — 재생 중 임시 접촉 기록기를 붙였다가 제거하는 진단
- `AgentScripts/VerifyPressureCollapse.cs`

씬 백업: `Assets/_Project/Scenes/Backups/BeforePressureCollapse_20261106_175240.unity`

빌더의 `Run`은 새 구성에 한 번만 사용한다. 현재 씬에는 이미 적용되어 있다. 작업 중 사용했던 중간 위치 조정은 정리했으며, 빌더는 최종 낙하 통로·착지면·외곽 충돌면·입자 발생 높이를 함께 구성한다.

## PC 검증

| 검사 | 결과 |
| --- | --- |
| 최종 낙하 배치 반복 재생 | 연속 2회 PASS, 9개 모두 분리·중력 적용·바닥 정착·초기화 |
| 큰 파편 하강량 | 약 1.1–1.35m, 천장 부근에 남는 조각 없음 |
| 낙하 검사 기준 | 모든 조각 70cm 이상 하강, 중심 높이 -20cm~40cm, 수면 또는 속도 0.08m/s 미만 |
| 상부 메시 입자 | 4개 발생기 작동, 최대 동시 32개, 초기화 후 0개, 발생기 수 증가 없음 |
| 천장 하강 | 최대 14cm 유지 |
| 기본 PC 착석 위치 머리 간격 | 최소 약 27.05cm |
| 기존 소형 파편 | 28개 표시 유지, 28개 정착 |
| 파이프 | 경로 연속성 PASS, 1/30초 단위 최대 이동 약 10.66cm |
| 구조 엔딩 | 실제 119 통화·신호 조건·개별 4장 제거·완료·초기화 PASS |
| 빛 차단 | 하강 전·중·후 총 18뷰, 빛 누출 방향 0 |
| XR 구성 | PC 검사에서 카메라·컨트롤러 Transform 보존, XR Origin 1개 |

최종 컴파일과 새 재생 세션의 Console 오류는 0개다. 편집기는 재생을 멈췄고, 백그라운드 실행 임시 설정을 복구했다. 원래 켜져 있던 자동 틱은 유지했다.

중간 재컴파일에서 XR Interaction Toolkit 초기화 예외 이력이 발생했다. 새 Play Mode 세션에서 최종 검증할 때는 재발하지 않았으며 XR 패키지 자체는 수정하지 않았다. 새 물리 재질의 확장자 오류는 `.physicMaterial`로 정리하고 4개 참조를 확인했다. 이전 로그를 직접 삭제하지 않았다.

## 화면과 범위

- 변경 전: `Temp/PressureCollapseReview/Before_Up.png`
- 상부 잔해가 채워진 화면: `Temp/PressureCollapseReview/Packed_Before_Up.png`
- 여진 진행 중: `Temp/PressureCollapseReview/At_07.8_Up.png`
- 먼지를 숨긴 구조 확인 화면: `Temp/StructuralAftershockReview/At_07.8_Up.png`
- 낙하 측정: `Temp/PressureCollapseReview/Verification.json`
- 기존 흐름 회귀 검사: `Temp/GroundedCollapseReview/Verification.json`
- 구조 완료 화면: `Temp/SealedBurialReview/Rescue_60_Ceiling.png`

잔해 확인 화면에는 임시 점광원을 사용했다. 게임의 기존 조명 설정은 유지된다. 상부 하중층과 지지 골격의 침하는 연출된 움직임이며, 건물 전체의 구조역학을 계산하는 시뮬레이션은 아니다. 분리된 9개 파편은 실제 Rigidbody 중력과 충돌을 사용한다.

실제 헤드셋에서의 공간감·편안함·성능 검증은 남아 있다. PC의 머리 간격 수치를 모든 실제 착석 자세에 대한 검증으로 해석하지 않는다.

## 시각 검토 및 참고

기존 회색 콘크리트·녹·벽돌의 재질을 유지하고 유료 에셋으로 상부 밀도를 보강했다. 새 UI나 팔레트를 만들지 않았다. 검토 범위는 잔해의 밀도·지지 관계·낙하 피드백이다.

- palette discipline: 4/5 — 기존 유료 재질과 URP 재질 사본 유지
- style uniformity: 4/5 — 상부·낙하·착지면에 같은 에셋군 사용
- feedback visibility: 2/5 → 4/5 — 고정 회전 대신 실제 낙하와 틈에서 떨어지는 메시 파편
- support density: 2/5 → 4/5 — 천장 뒤 빈 배경을 가까운 하중 잔해로 채움

적용 스킬: `unity-cli`, `craft-game-floors`, `physics-3d-collision`. 읽은 참조는 `playmode-verification-loop.md`, `vfx.md`, `visual-scorecard.md`다. 엔진 동작은 Unity 6000.3의 [Rigidbody.useGravity](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody-useGravity.html), [Continuous collision detection](https://docs.unity3d.com/6000.3/Documentation/Manual/ContinuousCollisionDetection.html), [ParticleSystem.CollisionModule](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.CollisionModule.html) 문서를 확인했다.
