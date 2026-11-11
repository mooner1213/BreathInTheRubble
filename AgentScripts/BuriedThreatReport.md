# 매몰 공간과 정면 여진 보강 — 2026-11-09

`Assets/_Project/Scenes/ConfinedPrototype.unity`에 적용 완료.

## 반영 내용

- 몸 앞과 양옆에 가까운 잔해층, 무릎 앞의 경사진 매몰층, 상부 가장자리의 벽돌과 철근을 추가했다. 기존 외곽 잔해 안쪽에 덧쌓는 방식이다.
- 바닥에 작은 파편 47개를 보이는 표면에 맞춰 배치하고 재질별 메시 2개로 합쳤다.
- 정면 상부에 큰 콘크리트 파편 2개를 추가했다. 첫 파편은 약 73cm 폭이다. 기존 여진의 3.2초와 6.6초 충격에 연결했다.
- 각 파편은 분리 전 2.3초 동안 국소적으로 기울고 밀리며 가루와 하중음을 낸다. 약 3.26초와 6.66초부터 실제 Rigidbody 중력·충돌로 떨어진다. 바닥 충돌에서 별도의 콘크리트 충돌음이 재생된다.
- 첫 파편의 받침은 보이는 콘크리트 메시 자체를 충돌면으로 사용한다. 머리·손 접근 시 숨기는 기존 보호 기능을 유지했다.
- 새 큰 파편의 형태를 메시로 고정해 부모 분리 전후 비율 변형을 방지했다.
- 기존 `AftershockFallingChunk.ResetEffect()`가 부모 복귀 시 원래 localScale을 복원하지 않는 버그를 수정했다. 초기화에서 Rigidbody 위치·회전도 함께 맞춘다.

기존 회색 콘크리트·붉은 벽돌·철근 재질과 Performance URP 설정을 재사용했다. 새로운 미술 스타일이나 공통 팔레트를 승인된 기준으로 추가하지 않았다. UI, 입력 바인딩, 게임 규칙, 단계 지속시간은 수정하지 않았다.

## 수정·추가 파일

- 수정: `Assets/_Project/Scenes/ConfinedPrototype.unity`
- 수정: `Assets/_Project/Scripts/AftershockFallingChunk.cs`
- 추가: `Assets/_Project/Scripts/AftershockLoadWarning.cs` 및 `.meta`
- 추가: `Assets/_Project/Art/BuriedThreat/` 및 `.meta`
  - `CrushedFloor_1.asset`, `CrushedFloor_2.asset`
  - `ForegroundSlab_1.asset`, `ForegroundSlab_2.asset`
  - `ForegroundConcreteContact.physicMaterial`
  - `LoadedConcreteStrain.wav`, `HeavyConcreteCrash.wav`
  - 각 파일의 `.meta`
- 작업·검증 스크립트: `AgentScripts/InspectBuriedThreat.cs`, `BuildBuriedPocket.cs`, `RefineBuriedPocket.cs`, `BakeForegroundConcrete.cs`, `VerifyBuriedThreat.cs`
- 보고서·이미지·검증 결과: 이 문서와 `AgentScripts/ReviewImages/BuriedThreat/`

기존 미커밋 작업은 유지했다. 작업 시작 씬 복사본은 `Temp/BuriedThreatReview/Before.unity`, 코드 복사본은 `AgentScripts/AftershockFallingChunk.BeforeBuriedThreat.cs.txt`다. Temp 폴더는 영구 백업이 아니다. 기존 사용자 변경을 함께 커밋하는 대신 현재 씬을 복사해 복구 지점을 마련했다.

빌더는 이미 적용되어 있다. 재실행하지 않는다. 생성 순서는 `BuildBuriedPocket.Run` → `Threats` → 받침 보정 `SeatLanding` → `BakeForegroundConcrete.Run`이었다. `RefineBuriedPocket`은 메시 고정 전 배치를 조정한 작업 기록이며 현재 씬에 재적용하지 않는다.

## PC 검증

`VerifyBuriedThreat.Run` 최종 PASS:

- 실제 진행 프레임 4,848개를 관측했다. 물리 시간을 수동 점프해 낙하를 대신하지 않았다.
- 새 큰 파편 낙하 거리: 약 0.64m / 1.09m.
- 두 파편 모두 보이는 상태로 분리·낙하하고 정착했다.
- 기본 PC 자세에서 새 파편 AABB와 머리 사이 관측 최소 간격: 약 0.40m. 모든 실제 착석 자세의 안전 검증을 뜻하지 않는다.
- 하중 예고와 실제 충돌음 재생을 확인했다.
- 기존 쇠파이프가 사용 가능 상태에 도달했다.
- 검사 종료 시 카메라·양손 local pose와 파편 초기 위치를 확인했다. 기존 PC 전용 흔들림은 별도 구현이며 새 연출은 추적 Transform을 참조만 한다.
- ParticleSystem 수가 증가하지 않았다.

`PhoneLightReview` PASS: 별도 검사 조명 없이 기존 PC 휴대폰 조작 경로로 손전등을 켜고, 2.82 / 3.46 / 6.90 / 17.90초에 화면을 캡처했다. 정면 낙하가 손전등 안에서 보이는 것을 확인했다.

`ResetAndRescue` PASS:

- 기존 포함 11개 큰 파편을 3회 분리·초기화했을 때 localScale·localPosition의 누적 오차가 없었다.
- 구조 60초 완료, OpeningAmount=1, SunlightAdmission=1, WhiteoutAmount=0.
- 구조 통로가 추가 잔해에 막히지 않았다. 이 검사는 단계·채광 회귀 검사이며 60초 전체 실시간 체험 검사는 아니다.

최종 감사: XR Origin 1개, Input Action Manager 1개. 휴대폰·물병·파이프의 편집 모드 배치 유지. 새 MeshRenderer 22개, ParticleSystem 2개, 삼각형 23,564개. 새 재질에 null·Default-Material·미지원 셰이더 없음. 기기 성능 측정은 수행하지 않았다.

마지막 Unity 컴파일 오류 0개, Console 오류 0개. 기존 `peelDegrees` 미사용 경고가 남아 있으며, 이번 작업 전 OpenXR 임포트 경고와 PC 테스트 종료 시 XR Simulator 손·컨트롤러 탐지 경고 이력이 있다. 패키지나 SDK는 변경하지 않았다.

Play Mode는 종료했다. 시작 시 이미 켜져 있던 auto-tick(16ms)과 runInBackground=true는 그대로 유지했다.

## 화면과 시각 검토

- [변경 전 정면](ReviewImages/BuriedThreat/BeforeFront.png)
- [최종 매몰 공간 정면](ReviewImages/BuriedThreat/PocketFront.png)
- [최종 바닥](ReviewImages/BuriedThreat/PocketFloor.png)
- [실제 휴대폰 손전등에서 본 낙하](ReviewImages/BuriedThreat/PhoneLightFall.png)
- [낙하 후 남은 콘크리트](ReviewImages/BuriedThreat/Settled.png)

`PhoneLightFall`은 게임의 휴대폰 조명으로 촬영했다. 나머지 위 비교 이미지는 기하 확인용 임시 검사 조명을 사용했고, XR 시각 요소를 숨겼다. 실제 게임 조명 설정은 변경하지 않았다. 검사 조명은 씬에 저장하지 않았다.

아래 평가는 PC 캡처에 대한 작업자 평가다. 사용자가 느끼는 공포의 검증 결과로 해석하지 않는다.

palette discipline: 4/5 — 기존 콘크리트·벽돌·철근 URP 재질 재사용.
style uniformity: 4/5 — 기존 잔해와 같은 자산군 사용, 분리 전후 파편 비율 고정.
lighting depth: 3/5 — 기존 어둠 유지, 실제 휴대폰 손전등 안에서 낙하 확인.
structural silhouette: 3/5 — 가까운 매몰층과 정면 큰 파편으로 하중 형태 보강.
feedback visibility: 2/5 → 4/5 — 정면 하중 예고·가루·중력 낙하·충돌음·잔류 확인.
support density: 2/5 → 4/5 — 몸 가까운 잔해층, 바닥 파편, 상부 가장자리 보강.

UI intentionality와 캐릭터 실루엣 평가는 이번 변경 범위에서 제외했다.

## 남은 한계

실제 헤드셋에서 공간 압박감·공포감·손 접근성·프레임 시간은 확인하지 못했다. 머리와 손을 낙하 경로로 가져가면 기존 안전 처리에 따라 해당 장식 파편이 숨겨질 수 있다. 휴대폰 손전등을 끄면 기존 설정대로 매우 어둡다. 새 하중·충돌 음원은 합성한 프로토타입이며 최종 현장 녹음 수준의 폴리는 아니다.

## 적용 스킬과 참고

적용: `unity-cli`, `craft-game-floors`의 환경·피드백·VFX 범위. 읽은 참조: `art-direction.md`, `game-feel.md`, `vfx.md`, `game-audio.md`, `placeholder-assets.md`, `visual-scorecard.md`, `playmode-verification-loop.md`. 신규 UI·전체 오디오 믹서·미술 기준 문서 작성은 이번 범위에서 제외했다.

동작 확인 문서: Unity 6000.3의 [Rigidbody.useGravity](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody-useGravity.html), [ParticleSystem.Emit](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ParticleSystem.Emit.html), [AudioSource.PlayOneShot](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSource.PlayOneShot.html), [Physics.IgnoreCollision](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.IgnoreCollision.html), [Application.runInBackground](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-runInBackground.html).
