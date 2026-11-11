# 쇠파이프 관통 수정과 연속 붕괴 강화 — 2026-11-09

`Assets/_Project/Scenes/ConfinedPrototype.unity`에 적용 완료.

## 수정 결과

쇠파이프의 기존 시작점은 천장 위 `(-0.19, 1.72, 0.58)`였고, 충돌을 끈 경로 이동이 닫힌 천장을 통과했다. 시작점을 천장 아래의 보이는 잔해 턱 `(-0.06, 1.075, 0.58)`으로 옮겼다. 파이프를 수평으로 받치는 작은 콘크리트 턱 2개를 추가했다. 턱에서 미끄러져 나온 다음 빈 통로로 떨어지며 약 7.21초에 기존 착지 위치에서 잡을 수 있다.

착지의 기존 전체 사인파 튕김은 후반에 바닥 아래로 내려갔다. `AftershockPipeDrop.cs`에서 바닥 위쪽으로만 튕기는 반파로 바꾸었다.

여진 보강:

- 큰 물리 잔해 11개 → 28개. 기존에 보이던 상부 벽돌 받침 8개를 실제 낙하 조각으로 전환하고 콘크리트 9개를 추가했다. 새 콘크리트는 최대 약 74cm 폭이다.
- 작은 물리 파편 28개 → 84개.
- 메시 잔파편 분출 4곳 → 10곳, 30차례 연속 분출. 한 지점에서 회당 10개씩 발생하며 시스템을 재사용한다.
- 주 붕괴 시점인 3.2 / 4.8 / 6.6초에 여러 큰 조각이 연속으로 분리되고, 후속 낙하가 약 16초까지 이어진다.
- 국소 하중 예고, 가루와 실제 물리 충돌음을 연결했다. 새 큰 조각의 메시 크기를 고정하여 부모 분리 시 비율 변형을 방지했다.
- 파이프가 지나가는 중앙 통로를 남기고 큰 낙하를 정면 바깥쪽과 양옆에 배치했다.
- 파편이 천장 안에서 가려지지 않도록 잔파편 분출점을 상부 잔해의 아래쪽으로 조정했다. 큰 조각을 볼 수 있게 여진 안개 목표 농도를 1.9에서 0.78로 낮췄다. Awakening의 기존 농도는 그대로다.
- 바닥 틈으로 빠지는 조각을 잡도록 보이는 콘크리트 메시 받침 4개를 추가했다.

XR 추적이나 입력, UI, 패키지와 SDK는 변경하지 않았다. 기존 큰 파편의 머리·손 접근 보호는 유지했다.

## PC 검증

1. 파이프 단독 경로 검사: PASS. 실제 재생 117회 표본에서 파이프 양 끝을 포함한 CapsuleCollider와 보이는 환경 메시의 삼각형 사이 관통 0건. 중심점만 검사하지 않았다.
2. 확장된 여진 전체 검사: PASS. 실제 프레임 1,963개가 진행되는 동안 큰 잔해 28개 모두 분리되었다. 최대 8개 큰 잔해가 동시에 이동했고, 메시 잔파편은 동시에 최대 429개였다.
3. 큰 잔해 25개가 보이는 상태로 정착했다. 3개는 기존 머리·손 접근 보호에 따라 숨겨졌다. 보이는 큰 잔해 중 바닥 아래로 빠진 조각은 0개였다. 작은 물리 파편은 84개 중 82개가 보이는 상태로 남았다.
4. 확장 붕괴 중 파이프 경로 추가 표본 81회에서 관통 0건, 파이프 사용 가능 상태 확인. 삼각형 표면과 캡슐 사이 깊이 1.5mm 초과를 관통으로 기록했다. 모든 실제 사용 자세의 검증을 뜻하지 않는다.
5. 큰 잔해 위치·크기 초기화, 작은 파편 재부착, 모든 분출 입자 제거: PASS.
6. 28개 큰 잔해의 3회 반복 초기화와 구조 단계 회귀 검사: PASS. 구조 60초 시점 OpeningAmount=1, SunlightAdmission=1, WhiteoutAmount=0. XR Origin 1개. 이 단계 회귀 검사는 시간 진행 API로 실행했으며 60초 전체의 실시간 체험 검사는 아니다.
7. 최종 Unity 컴파일 오류 0개, Console 오류 0개. 기존 미사용 필드와 XR Simulator/OpenXR 관련 경고 이력은 남아 있다.

편집기는 Play Mode를 종료한 상태다. 기존 auto-tick(16ms)과 백그라운드 실행 설정은 유지했다.

## 화면과 검증 파일

- [실제 휴대폰 손전등에서 본 붕괴](ReviewImages/Avalanche/StormPhone.png)
- [같은 구간의 검사 조명 화면](ReviewImages/Avalanche/StormInspection.png)
- [파이프 착지와 남은 잔해](ReviewImages/Avalanche/PipeLanded.png)
- [전체 검증 수치](ReviewImages/Avalanche/Verification.json)
- [파이프 단독 경로 검사](ReviewImages/Avalanche/PipeClearance.json)
- [초기화 결과](ReviewImages/Avalanche/Reset.json)
- [구조 회귀 검사](ReviewImages/Avalanche/Rescue.json)

손전등 화면은 추가 검사 조명 없이 기존 PC 휴대폰 조작 경로로 촬영했다. Inspection과 PipeLanded 화면에는 임시 검사 조명을 사용했다. 캡처에서는 XR 시각 요소를 숨겼다. 검사 조명은 실제 씬에 저장하지 않았다.

## 수정·추가 파일

- 수정: `Assets/_Project/Scenes/ConfinedPrototype.unity`
- 수정: `Assets/_Project/Scripts/AftershockPipeDrop.cs`
- 추가: `Assets/_Project/Art/AftershockAvalanche/`의 메시 자산 26개 및 `.meta` — 파이프 받침 2개, 기존 상부 조각의 물리용 메시 8개, 새 콘크리트 9개, 작은 파편 공용 메시 3개, 바닥 받침 4개.
- 추가: `AgentScripts/BuildAvalanche.cs`, `InspectAvalanche.cs`, `VerifyAvalanche.cs`
- 추가: 이 보고서와 `AgentScripts/ReviewImages/Avalanche/`.
- 코드 보관본: `AgentScripts/AftershockPipeDrop.BeforeAvalanche.cs.txt`
- 작업 시작 씬 보관본: `Temp/AvalancheReview/Before_20261109_*.unity`. Temp는 영구 백업이 아니다.

빌더는 이미 적용됐다. 생성 순서는 `FixPipe` → `Collapse` → `FinishLandingAndShowers`이며 현재 씬에 재실행하지 않는다. 기존 원본 프리팹·모델·재질 파일은 교체하거나 삭제하지 않았다. 기존 상부 받침의 렌더러 8개는 새 낙하 메시로 전환하면서 씬에서 비활성화했다.

## 시각 검토와 한계

기존 회색 콘크리트·붉은 벽돌·철근 재질군을 재사용했다. 새 미술 기준이나 UI 기준은 추가하지 않았다. 아래는 PC 캡처에 대한 작업자 평가이며 실제 공포 체감의 검증 결과가 아니다.

palette discipline: 4/5 — 기존 URP 재질군 유지.
style uniformity: 4/5 — 기존 상부 조각 자체를 분리하고 동일 자산군으로 보강.
lighting depth: 3/5 — 기존 휴대폰 조명 유지, 여진 중 과도한 안개 차폐 완화.
structural silhouette: 3/5 — 다수의 상부 받침과 큰 판형 조각이 분리되어 떨어짐.
feedback visibility: 3/5 → 4/5 — 손전등 화면에서 연속 낙하와 잔파편 확인, 최대 8개 큰 잔해 동시 이동.

UI와 캐릭터 평가는 이번 범위에서 제외했다. 실제 헤드셋에서 공포감·접근성·프레임 시간은 확인하지 못했다. 파편 물량 증가에 따른 목표 기기 성능 측정이 남아 있다. 음원은 기존 합성 프로토타입을 재사용했다.

적용 스킬: `unity-cli`, `craft-game-floors`의 환경·VFX·피드백 범위. 참조는 `vfx.md`, `game-feel.md`, `visual-scorecard.md`, `playmode-verification-loop.md`다. API 확인: Unity 6000.3의 [Physics.ComputePenetration](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.ComputePenetration.html), [ParticleSystem.CollisionModule](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.CollisionModule.html). 최종 파이프 검사는 비활성 충돌면도 포함하기 위해 보이는 메시 삼각형과 실제 파이프 캡슐 사이 거리를 직접 검사했다.
