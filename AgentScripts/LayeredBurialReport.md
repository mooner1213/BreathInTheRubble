# 천장 내부의 층별 붕괴와 영구 잔해 — 2026-11-09

현재 `ConfinedPrototype`에 적용 완료. 이 문서가 앞선 `BuriedThreatReport.md`, `AvalancheAndPipeReport.md`의 배치와 검증 상태를 대체한다.

## 사용자 사진에서 수정한 부분

사진의 공중 파이프와 두 둥근 받침은 초기 배치에서 보이지 않도록 기존 구성을 보관·비활성화했다. 파이프는 높이 2.45m의 천장 안쪽 통로에서 내려오며, 착지점은 실제 받침 메시의 표면 13곳을 조사해 맞췄다. 잡기 전에는 받침의 여진 움직임을 따라가므로 바닥이 움직이는데 파이프만 세계 좌표에 고정되는 현상도 수정했다.

단순히 생성점을 닫힌 천장 위로 옮기지 않았다. 기존 천장과 주변 잔해 메시를 새 자산으로 복사해 여섯 개의 불규칙한 통로를 냈고, 위쪽을 실제 잔해로 닫았다. 원본 모델·프리팹·재질 자산은 삭제하거나 덮어쓰지 않았다.

다섯 붕괴 통로에는 네 겹의 잔해를 배치했다. 아래층이 깨지면 다음 층이 내려와 하중을 받고, 다시 깨지는 순서다. 마지막 층은 낮아진 위치에 남아 무너진 자리를 메운다. 초기 큰 조각 중심 높이는 약 1.7~2.7m이고, 마지막 받침층은 3.15m에서 약 1.56m까지 내려앉는다.

각 통로의 주 파괴 시각은 다음처럼 다르다. 같은 간격의 전체 동시 분출은 사용하지 않는다.

| 통로 | 첫 파괴 | 다음 층 | 후속 파괴 |
|---|---:|---:|---:|
| 정면 왼쪽 | 3.05초 | 6.28초 | 11.74초 |
| 정면 오른쪽 | 3.89초 | 7.48초 | 13.15초 |
| 왼쪽 | 4.67초 | 8.66초 | 14.28초 |
| 오른쪽 | 5.41초 | 9.53초 | 15.34초 |
| 뒤쪽 | 6.12초 | 10.76초 | 14.82초 |

각 층의 세 조각에도 분리 시차가 있다. 얇은 조각은 개별 위치·크기·회전·낙하 시각을 따로 갖는다. 콘크리트·벽돌·블록의 여러 메시를 사용한다. 표면 균열 15곳과 위쪽에서 들리는 하중음을 연결했다.

## 사라지는 문제

새 구성의 큰 조각 45개와 작은 조각 180개는 실제 Rigidbody로 남는다. 기존 메시 입자 분출은 비활성화했다. 먼지 연무는 기존 효과를 유지하지만, 단단한 조각을 입자 수명으로 지우지 않는다.

`AftershockFallingChunk`의 머리·손 근처 영구 숨김 처리를 제거했다. 떨어지는 조각을 추적 위치에서 바깥쪽으로 부드럽게 유도하고 추적기·아이템과의 충돌을 분리한다. HMD나 컨트롤러 Transform을 움직이지 않는다.

기존 바닥의 틈 아래에는 실제로 렌더되는 두꺼운 연속 콘크리트 기초를 추가했다. 얇은 조각에는 렌더 메시 경계에 맞춘 BoxCollider와 Continuous Dynamic 충돌을 사용해 빠른 낙하에서의 바닥 통과를 막았다. 큰 조각은 원래 형태의 볼록 메시 충돌을 사용한다.

## 최종 PC 검증

- 초기 생성점 차폐: 기본·좌우 15cm·위쪽 12cm·앞쪽 12cm의 다섯 시점에서 파이프와 느슨한 조각의 생성점 노출 0개. 모든 실제 착석 자세의 검증을 뜻하지 않는다.
- 실제 29초 재생, 진행 프레임 5,660개. 큰 조각 45개와 작은 조각 180개 모두 분리됨.
- 29초 시점에 225개 모두 Renderer 유지, 바닥 아래 이탈 0개.
- 큰 조각 45개 모두 정착. 모두 실제 충돌 접촉이 기록됐고, 중력은 켜져 있으며 kinematic으로 공중 고정하지 않았다.
- 큰 조각 중 11개는 높이 0.8m 이상에 쌓였지만, 주변 벽·철근·다른 낙하 조각에 받쳐져 있다. 접촉 없는 큰 조각 0개. 사진과 접촉 기록을 함께 확인했다.
- 큰 조각 최대 9개 동시 이동. 이 수치는 연출 자연스러움이나 공포 체감의 대리 지표로 사용하지 않는다.
- 파이프 최종 경로 표본 128회, 보이는 메시 삼각형과 파이프 캡슐 사이 관통 0건. 이전 검사와 동일하게 1.5mm 초과 겹침을 기록했다.
- 225개 조각의 위치·크기·부착 상태 초기화 PASS.
- 실제로 잔해가 쌓인 뒤 구조 단계 검사 PASS: IsComplete=true, OpeningAmount=1, SunlightAdmission=1, WhiteoutAmount=0. 구조 시간은 단계 API로 진행했으며 60초 전체 실시간 체험 검사는 아니다.
- 최종 컴파일 오류 0개, 최종 재생 구간 Console 오류 0개. 기존 미사용 직렬화 필드 경고는 남는다.

얇은 조각의 충돌 방식 변경 중 발생한 초기화 예외는 수정 후 새 Play Mode에서 다시 검증했다. 최종 검증 전 최근 500개 진단 로그를 `Temp/LayeredBurialReview/ConsoleBeforeFinal.json`에 보관하고 콘솔을 초기화했다.

Play Mode는 종료했다. 시작 시 켜져 있던 auto-tick(16ms), runInBackground 설정은 유지했다. XR Origin·입력 구성·UI·게임 규칙·SDK는 변경하지 않았다.

## 비교 화면

- [사용자가 제공한 문제 화면](ReviewImages/LayeredBurial/UserReference.png)
- [수정 후 여진 전 — 실제 휴대폰 손전등](ReviewImages/LayeredBurial/BeforeAftershock.png)
- [층별 붕괴 진행 중 — 실제 휴대폰 손전등](ReviewImages/LayeredBurial/ProgressiveCollapse.png)
- [낙하 후 바닥에 남은 잔해와 파이프](ReviewImages/LayeredBurial/PermanentFloorRubble.png)
- [내려앉은 층이 채운 천장](ReviewImages/LayeredBurial/RefilledCeiling.png)

앞의 두 손전등 화면은 추가 검사 조명 없이 촬영했다. 바닥·천장 검토 화면에는 임시 검사 조명을 사용했고, 캡처에서 XR 시각 요소를 숨겼다. 검사 조명은 실제 씬에 저장하지 않았다.

## 변경 파일

- 수정: `Assets/_Project/Scenes/ConfinedPrototype.unity`
- 수정: `Assets/_Project/Scripts/AftershockFallingChunk.cs`
- 수정: `Assets/_Project/Scripts/AftershockPipeDrop.cs`
- 추가: `Assets/_Project/Scripts/RubbleFailurePocket.cs` 및 `.meta`
- 추가: `Assets/_Project/Art/LayeredBurial/`의 메시 613개와 메타 파일. 통로를 낸 천장 복사본, 고유 조각, 받침·틈새 마감과 기초다. 중간에 교체한 마감 자산도 삭제하지 않고 보관했다.
- 추가: `AgentScripts/BuildLayeredBurial.cs`, `ReviewLayeredBurial.cs`, 이 문서와 `AgentScripts/ReviewImages/LayeredBurial/`.

이전 부유 잔해·동시 분출 구성은 씬에서 보관·비활성화했다. 빌더는 이미 적용되어 있으므로 재실행하지 않는다. 작업 시작 씬과 두 스크립트의 복사본은 `Temp/LayeredBurialReview/Before.unity`, `AftershockFallingChunk.Before.cs.txt`, `AftershockPipeDrop.Before.cs.txt`다. Temp는 영구 백업이 아니다.

검증 원본: [Runtime.json](ReviewImages/LayeredBurial/Runtime.json), [InitialAudit.json](ReviewImages/LayeredBurial/InitialAudit.json), [PipeClearance.json](ReviewImages/LayeredBurial/PipeClearance.json), [Reset.json](ReviewImages/LayeredBurial/Reset.json), [Rescue.json](ReviewImages/LayeredBurial/Rescue.json).

## 시각 검토와 남은 한계

기존 콘크리트·벽돌·철근 URP 재질을 재사용했다. 아래는 작업자의 PC 화면 검토이며 사용자 승인이나 실제 공포 체감 점수가 아니다.

palette discipline: 4/5 — 기존 잔해 재질군 유지.
style uniformity: 3/5 — 연결된 틈새·층과 여러 형태의 조각, 부유 받침 제거.
lighting depth: 3/5 — 기존 휴대폰 조명 아래 낙하 확인.
structural silhouette: 3/5 — 윗층 내려앉음과 벽·바닥의 지지된 적층 확인.
feedback visibility: 3/5 — 균열·시차 낙하·충돌·잔류를 연속 화면으로 확인.

UI·캐릭터 평가는 변경 범위에서 제외했다. 실제 헤드셋에서의 공포감·접근성·성능은 미검증이다. 물리 조각이 많아 목표 기기에서 프로파일링이 필요하다. 작은 조각의 충돌 형상은 렌더 메시 경계를 근사하며, 하중·충돌 음원은 기존 합성 프로토타입이다.

적용 스킬: `unity-cli`, `craft-game-floors`. 참조: `vfx.md`, `visual-scorecard.md`, 기존 `game-feel.md`와 `playmode-verification-loop.md`. 확인한 API: Unity 6000.3의 [Rigidbody.linearVelocity](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody-linearVelocity.html), [Physics.IgnoreCollision](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.IgnoreCollision.html), [Continuous collision detection](https://docs.unity3d.com/6000.3/Documentation/Manual/ContinuousCollisionDetection.html).
