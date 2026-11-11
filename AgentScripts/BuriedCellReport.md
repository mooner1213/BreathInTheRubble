# 초기 매몰 공간 재수정

사용자 스크린샷의 두 문제(노출된 천장 통로 내부와 넓은 정면)를 기준으로 `ConfinedPrototype` 씬을 수정했다. 이전의 '상부 연출 시야를 크게 남긴다'는 판단은 이번 요구에 맞지 않았다. 가까운 잔해를 먼저 보이고 낙하는 작은 틈으로 보이도록 바꿨다.

## 적용

- 기존 재질·모델에서 만든 온전한 잔해 10개를 정면·상부·양 어깨 앞에 추가했다. 정면 좌우 면, 중앙 받침돌, 낮은 연속 천장과 엇갈린 천장 입술이 통로의 잘린 면을 가린다.
- 기본 눈 위치 `(0, 0.9, 0)`에서 정면 충돌 표면 거리 **0.801 → 0.471m**. 정면 대각선은 **0.545 / 0.529m**, 바로 위 표면은 **0.276m**. 물리적인 배치를 바꿨으며 플레이어 카메라 위치와 저장된 화각을 바꾸지 않았다.
- 절단 메시의 열린 경계 중 연결 가능한 면을 새 메시로 막았다(176개 메시, 478개 면). 모든 절단 경계가 완전한 폐곡면이라는 주장은 하지 않는다. 초기 플레이 시야의 은폐는 가까운 실제 잔해가 담당한다.
- 낮은 천장에 기존 낙하가 가려지는 문제를 보완해, 천장 뒤에 숨은 전경 조각 54개(얇은 조각 48개·작은 판형 조각 6개)를 추가했다. 6개 구간에 서로 다른 지연을 두고 떨어지며, 충돌 후 남는다. 생성 순간을 화면에 노출하지 않는다.
- 파이프는 새 낮은 틈을 통과하도록 lip/release 마커를 조정했다. 철근 타격 위치도 가까운 벽에 맞췄다. 원래 금속음·구조 신호 경로를 사용한다.
- 새 천장 세 조각은 기존 구조 연출에서 함께 들려 나간다. 구조 종료 뒤 새 천장이 시야를 막는 회귀를 수정했다.
- 중앙의 가까운 돌은 전용 어두운 콘크리트 재질 복사본으로 과노출을 줄였다. 원본 공용 재질과 휴대폰 조명은 수정하지 않았다.

## 확인

- 원래 PC 카메라 화각(60도), 휴대폰 조명, 16:9 캡처로 초기 화면과 연속 낙하를 확인했다. 추가 조명을 넣은 100도 진단 화면에서도 위쪽을 확인했다. 진단 화면은 실제 기기 화면이 아니다.
- 기본 위치와 좌우 12cm·위/앞 10cm 샘플에서 전경 조각 중심의 초기 노출 **0개**. 머리 반경 8cm 검사에서 새 구조물 겹침 **0개**. 초기 휴대폰·물병 활성 충돌체와 새 구조물 겹침 **0개**. 이 표본 검사는 모든 실제 머리·손 자세의 여유를 보장하지 않는다.
- 실제 29초 재생에서 새 조각 **54/54 방출, 54/54 렌더러 유지, 54/54 정지**, 바닥 아래 탈출 **0개**, 리셋 통과. 5257프레임 진행을 확인했다. 시선에서 막히지 않는 전경 조각은 순간 최대 7개였다. 이 값은 화면 내 픽셀 수나 공포감의 객관적 점수가 아니다.
- 기존 층별 붕괴도 가까운 기본 구조를 넣은 뒤 별도 29초 재생: 큰 조각 **45/45**, 작은 조각 **180/180** 방출·잔류, 큰 조각 모두 정지 및 접촉 지지, 탈출 **0개**, 리셋 통과. 이 검사는 전경 54개와 중앙 받침돌을 추가하기 전 단계다. 최종 전경 검증에서도 같은 Aftershock 시퀀스 전체를 실행했다.
- 최종 파이프 실제 경로 검사 **126회**, 시각 메시 관통 **0건**, 잡기 가능 상태 도달.
- 최종 기존 PC 휘두르기에서 실제 Concrete/Metal 충돌, 금속음 재생과 신호 이벤트 **4회**. 구조 신호가 수락되는 게임 조건은 변경하지 않았다.
- 구조 단계 API로 60초 진행 후 완료·개방·채광 값 1. 새 천장 이동 거리 1.713m와 실제 열린 하늘을 확인했다. 60초 구조를 실시간으로 기다린 검사는 아니다.
- 최종 컴파일 오류 **0개**, 최종 재생 콘솔 오류 **0개**. 기존 콘솔 경고 1개 남음. 검증 도중 임시 스크립트의 Unity null 검사 오류를 수정하고 재실행했다. 게임 런타임 코드는 바꾸지 않았다.
- 시작/종료 모두 재생 중지 상태. 기존 auto-tick 16ms와 Run In Background on을 유지했다. 실제 HMD 테스트·기기 성능 측정은 하지 않았다.

## 화면

- [최종 휴대폰 조명 초기 시야](ReviewImages/BuriedCell/InitialPhone.png)
- [수정 전 천장](ReviewImages/BuriedCell/BeforeCeiling.png)
- [수정 후 100도 천장 진단](ReviewImages/BuriedCell/ClosedCeiling.png)
- [우측·위·앞으로 기울인 100도 진단](ReviewImages/BuriedCell/LeanCeiling.png)
- [전경 낙하 순간](ReviewImages/BuriedCell/ForegroundFall.png): 마지막 중앙 재질 감광 전의 연속 재생 프레임.
- [새 천장 철거 후 구조 시야](ReviewImages/BuriedCell/Rescue.png)

## 수정 파일과 범위

- 수정: `Assets/_Project/Scenes/ConfinedPrototype.unity`.
- 추가: `Assets/_Project/Art/BuriedCell/` 메시 64개와 재질 1개, `Assets/_Project/Art/ClosedCanopyCuts/` 메시 176개 및 각 메타 파일. 기존 원본 모델·메시·공용 재질은 보존했다.
- 추가: `AgentScripts/BuildBuriedCell.cs`, `CloseCanopyCuts.cs`, `InspectBuriedCell.cs`, `VerifyBuriedCell.cs`, 본 보고서 및 `ReviewImages/BuriedCell/` 검증 자료. AgentScripts는 Assets 밖의 일회성 편집·검증 코드다.
- XR Origin, 추적, Input Action Manager, UI, 패키지, SDK, 게임 단계/승인 조건은 변경하지 않았다. 강제 HMD 이동을 추가하지 않았다.
- 작업 전 씬 복사본: `Temp/BuriedCellReview/Before.unity`(Temp 경로라 영구 백업이 아님). 빌더 각 단계는 적용됐으므로 재실행하지 않는다.

## 적용한 craft 검토

`unity-cli`의 playmode-verification-loop와 `craft-game-floors`의 visual-scorecard를 적용했다. 기존 회색 콘크리트·붉은 벽돌·낡은 금속 팔레트, 휴대폰 중심 조명, 거친 실물 모델을 유지했다. 신규 게임/캐릭터/UI 바닥 단계는 범위 밖이다.

palette discipline: 4/5 — 기존 팔레트와 텍스처 유지, 중앙 재질만 같은 계열의 어두운 복사본 사용.

style uniformity: 3/5 — 온전한 기존 모델로 가까운 면을 구성. 구조 이후 멀리 보이는 기존 절단 잔해는 여전히 정리 여지가 있다.

lighting depth: 3/5 — 가까운 휴대폰 명암을 유지하고 중앙 돌의 완전한 흰색 날림을 줄임.

enclosure silhouette (scoped): 2/5 → 4/5 — 처음부터 눈앞 중앙과 위를 가까운 잔해가 막고 상부 통로가 가려짐. 캐릭터 실루엣 축 대신 환경 범위를 평가.

feedback visibility: 3/5 — 가려진 상부 대신 가까운 틈의 낙하를 확인. 공포감·VR 체감은 사용자/실기 피드백 필요.

UI intentionality: 범위 밖 — 기존 휴대폰 UI 유지, 수정하지 않음.

검증 데이터: [거리·여유·원점 은폐](ReviewImages/BuriedCell/Clearance.json), [전경 낙하](ReviewImages/BuriedCell/Runtime.json), [파이프](ReviewImages/BuriedCell/PipeClearance.json), [철근](ReviewImages/BuriedCell/RebarStrike.json), [구조](ReviewImages/BuriedCell/Rescue.json).
