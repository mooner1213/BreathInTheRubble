# 정면 55cm와 가까운 낙하 재배치

`ConfinedPrototype`의 정면 중앙 표면을 기본 눈 위치에서 **0.550m**로 조정했다. 좌우 정면 잔해도 8cm 뒤로 옮겼다. 천장 높이·카메라·추적은 유지했다.

틈 뒤에 온전한 콘크리트·벽돌 잔해 4개를 겹쳐 넣었다. 가까운 조각이 떨어진 뒤에도 먼 빈 공간 대신 뒤쪽 매몰층이 보이게 했다. 파이프 경로와 겹치지 않도록 뒤쪽 잔해 위치를 맞췄고, 최종 파이프 자세는 기존 수평 낙하 방식이다.

전경의 기존 54개 조각을 틈 아래쪽으로 옮겼다. 6개의 판형 잔해는 처음부터 실제 벽·천장 사이에 끼워진 모습으로 보이며, 그 뒤에 작은 조각들이 숨는다. 여진 때 판형 잔해가 먼저 빠지고 뒤의 조각들이 다른 지연으로 뒤따른다. 규칙적인 줄처럼 보인 첫 배치를 수정해 모양·높이·각도를 다르게 했다. 사라지는 생성 연출 없이 실제 중력·접촉으로 떨어진다.

전경 조각은 머리·손을 미리 밀어 피하는 보정 반경을 0으로 설정했다. 깊은 매몰층 225개는 초기 속도를 플레이어 안쪽으로 돌리고, 회피 반경을 머리 8cm·손 5cm로 줄였다. 이 설정은 잔해의 이동에만 적용되며 HMD·컨트롤러를 이동시키지 않는다. 전경 조각과 파이프는 서로 물리 접촉하도록 충돌 제외 목록도 조정했다.

## 검증

- 기본 정면 ray: **0.550m**. 정면 대각선 두 방향 0.649/0.636m, 머리 바로 위 0.276m.
- 초기 활성 휴대폰·물병 충돌체와 새 구조물 겹침 0개. 기본·좌우 12cm·위/앞 10cm 눈 위치의 반경 8cm 검사에서 새 구조물 겹침 0개. 모든 실기 자세의 여유를 보장하는 검사는 아니다.
- 실제 29초 시뮬레이션, **4538프레임** 진행. **279/279개 방출**, 렌더러 유지 279개, 정지 279개, 바닥 아래 탈출 0개, 원위치 리셋 통과.
- 전경 **54/54개가 플레이어 기준 수평 75cm 이내**로 접근했다. 그중 23개가 55cm 안으로 접근했다. 전체 279개 중 254개가 1m 안으로 접근했다. 거리는 높이 1.5m 이하에서 렌더링 bounds의 가까운 점으로 측정했으며, 모든 조각이 머리를 직접 맞힌다는 뜻은 아니다. 깊은 층 일부는 상부 잔해에 걸려 하중으로 남는다.
- 원래 PC 화각 60도와 실제 휴대폰 조명으로 초기·낙하 중·낙하 후 화면을 확인했다. 전경 조각이 떨어진 뒤에도 틈 뒤가 잔해로 채워져 있다.
- 파이프 경로 메시 검사 **125회, 관통 0건**, 잡기 가능 상태 도달. 초기 조정에서 확인된 파이프 겹침을 수정한 최종 결과다.
- 기존 PC 휘두르기의 실제 금속 충돌·음원 재생·신호 이벤트 5회 확인. 구조 신호 수락 조건은 바꾸지 않았다.
- 구조 단계 60초 API 진행 후 완료·개방·채광 1, 새 천장 철거 확인. 실제 60초 대기 검사는 아니다.
- 최종 컴파일 오류 0개, 최종 콘솔 오류 0개. 기존 콘솔 경고 1개 유지. 콘솔을 지우지 않았다.
- 시작·종료 모두 재생 중지. 기존 auto-tick 16ms, Run In Background on 유지. 실제 HMD·기기 성능은 미검증.

## 파일

- 수정: `Assets/_Project/Scenes/ConfinedPrototype.unity`.
- 수정: `Assets/_Project/Scripts/AftershockFallingChunk.cs` — 기존 반경 기본값을 유지하면서 오브젝트별 머리·손 회피 반경을 설정할 수 있도록 함.
- 추가: `Assets/_Project/Art/DenseFront55/` — 메시 16개와 메타. 기존 메시·공용 재질은 보존.
- 추가: `AgentScripts/DenseFront55.cs`, `AgentScripts/VerifyDenseFront55.cs`, 본 보고서와 `ReviewImages/DenseFront55/` 자료.
- UI, XR Origin, Input Action Manager, 패키지·SDK, 카메라 위치·화각은 변경하지 않았다.
- 작업 전 씬: `Temp/DenseFront55Review/Before.unity`(임시 경로). 빌더는 이미 적용되어 재실행하지 않는다.

## 화면과 데이터

[최종 초기 시야](ReviewImages/DenseFront55/InitialPhone.png) · [가까운 낙하](ReviewImages/DenseFront55/FallingNearPlayer.png) · [낙하 후 뒤쪽 매몰층](ReviewImages/DenseFront55/AfterCollapse.png) · [밝은 진단 시야](ReviewImages/DenseFront55/Inspection.png)

[거리·충돌·초기 노출](ReviewImages/DenseFront55/Clearance.json) · [전체 낙하와 접근 거리](ReviewImages/DenseFront55/Runtime.json) · [파이프](ReviewImages/DenseFront55/PipeClearance.json) · [철근](ReviewImages/DenseFront55/RebarStrike.json) · [구조](ReviewImages/DenseFront55/Rescue.json)

초기 노출 검사에 표시되는 판형 잔해는 벽에 끼워진 구조 조각이다. 작은 조각들의 생성 원점과 구분한다.

## 범위별 craft 검토

기존 `unity-cli` playmode-verification-loop와 `craft-game-floors` visual-scorecard 기준으로 확인했다. 회색 콘크리트·붉은 벽돌·낡은 금속과 휴대폰 조명이라는 기존 방향을 유지했다.

palette discipline: 4/5 — 기존 URP 재질만 사용.

style uniformity: 2/5 → 3/5 — 규칙적인 첫 배치를 비대칭의 깨진 면으로 수정.

lighting depth: 3/5 — 실제 휴대폰 조명에서 가까운 면과 뒤의 매몰층을 확인.

enclosure silhouette (scoped): 4/5 — 정면 55cm와 메워진 뒤쪽 틈을 함께 유지.

feedback visibility: 3/5 → 4/5 — 전경 잔해가 빠지고 작은 조각이 플레이어 쪽으로 뒤따르는 실제 재생을 확인. 공포감의 실기 평가는 별도.

UI intentionality: 범위 밖 — UI 유지. 캐릭터·신규 게임·셸 관련 바닥도 이번 작업 범위 밖.
