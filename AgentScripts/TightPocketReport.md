# 초기 매몰 공간 압축과 철근 타격 — 2026-11-09

`ConfinedPrototype`에 적용 완료.

## 변경

- 몸 앞 좌우에 기울어진 콘크리트 2개, 다리 앞에 비스듬한 벽돌층 1개를 추가했다. 기존 정면 매몰층을 17cm 가까이 옮겼다.
- 위쪽과 정면에 비대칭 틈을 남겨 기존 천장 붕괴가 보이도록 했다. 카메라 위치나 FOV를 바꾸어 압박감을 만들지 않았다.
- 새 콘크리트 안에 끝이 박힌 굽은 철근 3곳을 추가했다. 형상에는 리브와 굽은 부분이 있으며, 금속 타격용 캡슐 충돌면을 연결했다.
- 철근에만 `PipeImpactSurface.Metal`을 지정했다. 콘크리트에는 Concrete를 지정했다. 기존 `PipeImpactAudio`와 `PipeRescueSignalRelay`를 재사용했다. 새로운 입력이나 구조 조건을 추가하지 않았다.
- UI, XR Origin, 입력 관리자, 패키지·SDK·공통 재질과 게임플레이 C#은 수정하지 않았다.

## 기본 PC 시점의 벽 거리

머리 위치 `(0, 0.9, 0)`에서 수평으로 쏜 광선의 첫 보이는 메시 충돌면이다. 전체 머리 가동 범위나 실제 착용 간격을 뜻하지 않는다.

| 방향 | 이전 | 이후 |
|---|---:|---:|
| 왼쪽 45도 | 0.95m | 0.65m |
| 왼쪽 25도 | 1.48m | 0.82m |
| 정면 | 0.97m | 0.80m |
| 오른쪽 25도 | 1.33m | 0.86m |
| 오른쪽 45도 | 0.89m | 0.63m |

## 검증

- 기존 PC 파이프 조작의 `SetHeld`와 `Swing`으로 실제 Rigidbody 충돌을 발생시켰다. 충격면 Concrete·Metal 감지, 오디오 재생, 잡은 파이프의 신호 이벤트 2회 전달 PASS. `TryPlayImpact`를 직접 호출한 검사가 아니다. 구조 요청·무전 조건은 기존 규칙을 유지했다.
- 휴대폰·물병과 새 잔해의 초기 충돌 겹침 0건.
- 실제 29초 연속 재생에서 큰 조각 45개와 작은 조각 180개 모두 분리·잔류. 바닥 이탈 0개, 큰 조각 45개 정착, 지지 접촉 없는 큰 조각 0개. 진행 프레임 5,669개.
- 좁아진 시야에서 기존 층별 낙하와 쌓임을 실제 휴대폰 조명 화면으로 확인했다.
- 기존 파이프 낙하 경로 126회 표본에서 메시 관통 0건.
- 초기화 PASS. 잔해가 쌓인 뒤 구조 단계 완료, OpeningAmount=1, SunlightAdmission=1, WhiteoutAmount=0. 구조 시간은 단계 API로 진행했다.
- 최종 컴파일 오류 0개, Console 오류 0개. 기존 미사용 직렬화 필드 경고는 남아 있다.

Play Mode는 종료했다. 시작 시 켜져 있던 auto-tick과 백그라운드 실행 상태를 유지했다. 실제 헤드셋의 압박감·접근성·성능은 미검증이다.

## 화면

- [이전 초기 공간](ReviewImages/TightPocket/Before.png)
- [좁아진 초기 공간](ReviewImages/TightPocket/After.png)
- [벽에 박힌 철근](ReviewImages/TightPocket/EmbeddedRebar.png)
- [기존 여진을 보는 실제 손전등 시점](ReviewImages/TightPocket/AftershockPhone.png)

초기 비교와 철근 검토 화면은 동일한 임시 검사 조명과 90도 검토 카메라를 사용했다. 실제 씬 조명·카메라 FOV는 변경하지 않았다. 손전등 화면에는 검사 조명이 없다.

## 수정·추가 파일

- 수정: `Assets/_Project/Scenes/ConfinedPrototype.unity`
- 추가: `Assets/_Project/Art/TightPocket/` — 콘크리트 메시 3개와 철근 메시 3개 및 메타 파일. 기존 재질을 참조한다.
- 추가: `AgentScripts/BuildTightPocket.cs`, `InspectTightPocket.cs`, `VerifyTightPocket.cs`, 이 문서와 `AgentScripts/ReviewImages/TightPocket/`.

작업 전 씬 복사본은 `Temp/TightPocketReview/Before.unity`다. Temp는 영구 백업이 아니다. 빌더는 이미 적용했으므로 재실행하지 않는다.

검증 원본: [금속 타격](ReviewImages/TightPocket/RebarStrike.json), [아이템 겹침](ReviewImages/TightPocket/PropClearance.json), [벽 거리](ReviewImages/TightPocket/AfterDistances.json), [여진](ReviewImages/TightPocket/Aftershock.json), [구조](ReviewImages/TightPocket/Rescue.json), [파이프 경로](ReviewImages/TightPocket/PipeClearance.json).

## 범위 내 시각 검토

작업자 PC 화면 평가이며 사용자의 실제 압박감 검증이나 승인을 뜻하지 않는다.

palette discipline: 4/5 — 기존 콘크리트·벽돌·금속 URP 재질 재사용.
style uniformity: 3/5 — 기울어진 잔해와 벽에 박힌 철근으로 연결.
lighting depth: 3/5 — 기존 휴대폰 조명 유지.
structural silhouette: 3/5 — 좌우와 하부의 열린 폭을 좁히고 상부 낙하 시야 확보.
feedback visibility: 4/5 — 실제 금속 충돌음과 기존 신호 전달 확인.

UI·캐릭터 평가는 범위에서 제외했다. 적용 스킬은 `unity-cli`, `craft-game-floors`이며, `art-direction.md`, `game-feel.md`와 기존 `visual-scorecard.md`, `playmode-verification-loop.md`를 참고했다.
