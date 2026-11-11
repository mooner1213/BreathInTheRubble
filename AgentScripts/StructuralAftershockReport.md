# 여진 천장 하강 및 구조 파편 — 2026-11-06

## 적용 결과

- `ConfinedPrototype.unity`에서 비어 있던 `ceilingTargets`를 천장·받침 잔해 8곳에 다시 연결했다. 주요 충격 3회에 나누어 최대 14cm 내려온다. 마지막 상태는 Isolation까지 유지하며 초기화하면 원래 자리로 돌아온다.
- 프리팹 루트가 아닌 실제 하위 렌더러의 경계를 합산해 머리 간격을 계산한다. 상위 잔해의 흔들림이 중복 적용되는 부분과 회전된 부모 아래의 하강 방향도 수정했다.
- 유료 RubbleAndDebris 원본 프리팹으로 큰 콘크리트 3개와 중간 조각 2개를 추가했다. 처음부터 지지부에 존재하고, 부착 가장자리가 약간 벌어진 뒤 분리되어 실제 Rigidbody 중력·메시와 맞는 convex 충돌체로 떨어진다. 접촉음과 소량의 파쇄 분진을 연결했다.
- 기존 소형 파편 28개는 하강하는 받침에 부착되어 있다가 분리된다. 재생 종료 시 비활성화 중인 계층으로 다시 부모를 지정하던 오류를 방지했다.
- 천장 아랫면·앞쪽 받침·왼쪽 고정부에 3개의 균열 연출을 추가했다. 실제 메시 표면에 투영한 불규칙한 선과 가는 가지 균열이며, 조명을 받는 URP Lit 재질을 사용한다. 실제 메시를 실시간 분쇄하는 기능은 아니다.
- 천장 하강으로 생긴 외곽의 작은 틈은 외부 천장 층에 유료 콘크리트 조각 8개를 덧대어 막았다. 기존의 네 장을 바깥부터 운반하는 구조 엔딩은 유지된다.

## 변경 파일

- 씬: `Assets/_Project/Scenes/ConfinedPrototype.unity`
- 수정 컴포넌트: `AftershockEnvironmentMotion.cs`, `AftershockDebris.cs`, `AftershockFallingChunk.cs`, `AftershockFractureSection.cs`
- 새 컴포넌트: `Assets/_Project/Scripts/RubbleStressCrack.cs`, `RubbleImpactAudio.cs` 및 Unity 메타 파일
- 새 재질: `Assets/_Project/Art/StructuralAftershock/ConcreteStressCrack.mat` 및 Unity 메타 파일
- 작업 스크립트: `InspectStructuralAftershock.cs`, `BuildStructuralAftershock.cs`, `RefineStructuralCracks.cs`, `SealStructuralGaps.cs`
- 검증 스크립트: `VerifyStructuralAftershock.cs`, `VerifyStructuralDarkness.cs`
- 백업: `Assets/_Project/Scenes/Backups/BeforeStructuralAftershock_20261106_165152.unity`, `AgentScripts/AftershockEnvironmentMotion.BeforeStructural.cs.txt`

원본 유료 에셋과 기존 백업은 보존했다. 이 변경은 이전 GroundedCollapseReport의 천장 하강 대상이 비어 있던 상태를 대체한다. UI 및 XR 입력은 수정하지 않았다.

## PC 검증

Unity 6000.3.22f1에서 실제 프레임과 물리가 진행되는 Play Mode로 확인했다.

| 검증 | 결과 |
| --- | --- |
| 천장 하강·균열·낙하·초기화 | PASS, 최종 검사 3,022프레임 |
| 최대 하강량 | 14cm, 천장 8곳 중 7곳 14cm / 나머지 약 12.2cm |
| 기본 PC 착석 위치의 최소 머리 간격 | 약 27.05cm |
| 큰 파편 3개 실제 하강 | 약 1.22m / 1.23m / 1.26m |
| 중간 조각 2개 실제 하강 | 약 1.32m / 0.16m — 마지막 조각은 주변 잔해에 다시 걸림 |
| 소형 파편 | 28개 표시 유지, 28개 정착, 중력 적용 상태 확인 |
| 파이프 경로 | PASS, 1/30초 단위 최대 이동 약 10.66cm |
| 구조 엔딩 | PASS, 실제 119 통화·신호 조건·4개 개별 잔해 제거·자연광·완료·초기화 |
| 빛 차단 | PASS, 하강 전 / 7.8초 / 18.1초 × 6방향, 총 18뷰 밝은 픽셀 0 |
| XR | 카메라 및 컨트롤러 위치 보존, XR Origin 1개 |
| 최종 컴파일 | 오류 0, 컴파일 경고 0 |
| 재생 종료 | 파편 재부착 오류 재발 없음, 수정 이후 새 오류 0 |

콘솔의 이전 파편 재부착 오류 112개는 이력으로 남아 있으며 수정 후 증가하지 않았다. 현재 Console 오류는 0개다. 기존 XR 시뮬레이터의 장치 감지 경고 이력은 보존했다.

물리 낙하·접촉은 PC 시뮬레이션으로 확인했다. 실제 헤드셋 검증과 기기별 성능 측정은 하지 않았다. 머리 간격 수치는 기본 PC 테스트 위치의 측정값이다.

## 결과 자료

- `Temp/StructuralAftershockReview/Verification.json`
- `Temp/StructuralAftershockReview/Darkness.json`
- `Temp/StructuralAftershockReview/Before_Up.png`
- `Temp/StructuralAftershockReview/At_07.8_Up.png`
- `Temp/StructuralAftershockReview/At_04.0_Left.png`
- `Temp/GroundedCollapseReview/Verification.json`

구조 확인용 이미지는 임시 점광원을 켜고 먼지를 숨긴 화면이다. 게임 내 조명 상태를 변경하지 않는다. 수정 전의 실패 진단 이미지 `DarknessFailure.png`는 최종 검증 결과가 아니다.

재현 순서: 편집 모드에서 `BuildStructuralAftershock.Run`, `AttachSeamChips`, `RefineStructuralCracks.Run`, `UseLitSurface`를 사용한다. 빛 차단 검증에서 수집한 틈은 편집 모드의 `SealStructuralGaps.Run`으로 보강했다. 이미 구성된 씬에 빌더나 보강 작업을 반복 실행하지 않는다.
