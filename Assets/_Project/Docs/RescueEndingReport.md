# 조건부 구조와 빛 엔딩

2026-11-05 구현 및 PC 검증 완료.

## 사용자 요구와 구현

구조 조건은 다음 두 가지다.

1. 119에 연결되어 구조 요청이 접수된 상태.
2. 밖에서 구조대 무전음이 실제 재생되는 동안, 플레이어가 잡은 파이프로 벽 또는 철근에 유효한 타격음을 낸 상태.

두 조건이 맞는 새 타격 한 번으로 구조가 시작된다. 임의의 성공 확률이나 추가 타격 횟수는 넣지 않았다. 구조 요청은 통화 종료/신호 상실 후에도 유지한다. 연결 시도를 했거나 번호가 틀린 경우에는 접수되지 않는다.

기존의 '통화 성공 + 240초 경과' 자동 구조를 제거했다. 240초는 첫 외부 무전음이 등장하는 연출 시점으로 사용한다. 8초간의 무전음을 놓치면 18초의 정적 뒤 다시 들린다. 이 시간들은 초기 조정값이며 표시 타이머는 없다. 소리가 들리지 않을 때의 타격은 저장하지 않으며, 놓은 파이프의 우연한 충돌도 구조 신호로 사용하지 않는다.

## 엔딩 표현

- 구조대 캐릭터는 추가하지 않았다. 잔해 밖의 공간 음원에서 무전, 응답, 작업 소리가 들린다.
- 신호 확인 후 34초 동안 기존 상부 잔해 19개를 순서대로 위/옆으로 치운다. 큰 천장판은 개구부 밖으로 빠지도록 옆 이동 거리를 늘렸다.
- 최초 작업 대기 후 생기는 틈으로 외부의 밝은 면이 보이며, 그림자를 사용하는 스포트라이트와 약한 반사광이 넓어진다.
- 열린 정도에 따라 URP Exposure/Bloom을 올리고, 마지막 11초 동안 화면을 부드럽게 흰색으로 채운다. 마지막 프레임은 완전히 흰색으로 유지한다.
- 흰색 전환은 기존 각성/피격 효과처럼 스테레오 대응 카메라 자식 메시의 시각 효과다. Canvas, HUD, 결과 메뉴, 자막은 추가하지 않았다.
- HMD와 양손 추적 Transform을 이동하지 않았다. 치워지는 상부 잔해의 콜라이더를 연출 중 비활성화하고, 복원 시 기존 상태로 돌린다.
- 엔딩 때만 카메라 후처리를 켠다. 원상복구 시 이전 설정을 유지한다.

URP 설정: Rescue Exposure and Bloom, Global Volume, priority 20, weight 0에서 시작. 전용 profile은 Assets/_Project/Art/RescueEnding/RescueEndingVolume.asset이다. ColorAdjustments postExposure는 0→3 EV, Bloom intensity는 0.15→0.9이며 Quarter, maxIterations=3, highQualityFiltering=false다. 활성화 시 카메라 Volume mask는 Default 레이어를 포함한다. 기존 공유 VolumeProfile은 수정하지 않았다.

## 구조와 파일

수정:

- Assets/_Project/Scripts/EmergencyCallSystem.cs — 실제 연결 완료 시 구조 요청 접수 상태 유지.
- Assets/_Project/Scripts/GameSequenceController.cs — 자동 구조 제거, 구조 신호 확인 API, 진행/복원 이벤트. 기존 240초 필드는 첫 탐색 시점으로 이름을 바꾸며 직렬화 값을 보존했다.
- Assets/_Project/Scripts/PipeImpactAudio.cs — 실제 재생된 타격 이벤트 제공.
- Assets/_Project/Scenes/ConfinedPrototype.unity — 조건/음원/잔해/빛/후처리/화이트아웃 연결.
- Assets/_Project/Docs/ExperienceRoadmap.md — 실제 구조 구현 상태 반영.

추가:

- Assets/_Project/Scripts/PipeRescueSignalRelay.cs — XR 또는 에디터 손에 들린 파이프의 타격만 입력 이벤트로 전달. 구조 규칙과 XR 입력 분리.
- Assets/_Project/Scripts/RescueSignalController.cs — 무전 기회 반복 및 두 조건 판정.
- Assets/_Project/Scripts/RescueEndingDirector.cs — 순차 잔해 제거, 빛/화이트아웃, 완료/복원.
- Assets/_Project/Art/RescueWhiteout.shader — URP 스테레오 대응 흰색 전환.
- Assets/_Project/Art/RescueEnding/ — 전용 재질 2개, VolumeProfile, 임시 WAV 3개 및 메타 파일.
- AgentScripts/InspectRescueSetup.cs, BuildRescueEnding.cs, VerifyRescueEnding.cs — 조사/씬 구성/검증.
- 이 보고서와 Unity가 생성한 신규 메타 파일.

변경 전 씬은 Assets/_Project/Scenes/Backups/BeforeRescueEnding_20261105_172709.unity에 보존했다. 휴대폰/물병 프리팹, XR Origin, Input Action Manager, 패키지는 이번 작업에서 변경하지 않았다.

## 검증 결과

- Unity 재컴파일: 오류 0, 경고 0.
- 최종 Console groundTruth: compilationFailed=false, consoleErrors=0. 과거 파이프 구현 당시 수정한 오류와 초기 검증 경고가 Pipeline의 누적 기록에는 남아 있어 현재 상태와 구분했다.
- job wait: Time.frameCount 변경을 확인하여 실제 Play Mode 프레임 진행 확인.
- VerifyRescueEnding.Run: PASS.
  - 전화 없음/소리 없음/타격, 전화 없음/무전 있음/타격은 모두 거부.
  - 잘못된 번호를 거부하고 실제 119 연결 코루틴 완료를 기다려 접수 상태 검증.
  - 통화 종료 및 신호 상실 뒤 접수 유지 확인.
  - 통화 성공과 시간 경과만으로 구조되지 않음.
  - 무전 구간 밖 타격 거부, 이전 타격 비축 방지, 놓친 무전 기회 재등장 확인.
  - 무전 중 놓은 파이프 타격 거부, 손에 든 유효 타격 승인 확인.
  - 잔해 이동/빛 증가/점진적 완전 흰색/완료 상태/재실행/복원 확인.
  - 초기 천장 위치와 카메라 설정 복원, HMD/양손 위치 유지, XR Origin 1개 확인.
- VerifyAftershockPipe.Run: PASS. 기존 낙하/착지/잡기/실제 콘크리트·금속 충돌/재실행 유지.
- VerifyExtendedAftershock.Run: PASS. 기존 18초/여섯 충격/후반 균열/30초 분진 정리/물병 유지/10회 재실행 유지.
- Temp/RescueReview/Before.png, Opening_08s.png, Opening_18s.png, Brightening_28s.png, Complete_34s.png를 직접 열어 검토했다. 중간 화면은 같은 머리 위치의 위를 보는 임시 카메라, 마지막은 실제 메인 카메라다. 검사 조명은 추가하지 않았다.
- 테스트용 Play Mode를 종료했고 임시 검사 카메라는 제거했다.

## 확인 방법과 남은 항목

- 정상 흐름: 실제 119 연결 → 여진 이후 파이프 확보 → 외부 무전음이 들릴 때 손에 들고 벽/철근 타격.
- PC에서는 기존 데스크톱 모드와 P(파이프 잡기/놓기), J(휘두르기)를 사용한다.
- 연출만 빠르게 보려면 기존 '다음 단계 미리보기'로 Isolation → Rescue를 선택한다. 이는 명시적인 에디터 미리보기여서 두 조건을 우회한다. RescueSignalController의 Context Menu 'Preview Search Radio (PC Play Mode)'로 무전 기회를 빠르게 확인할 수도 있다.
- 기존 PC Phone Test 창의 '구조 연출은 준비 중' 설명 문구는 실제 구현보다 오래된 내용이다. 이번에는 UI 디자인을 변경하지 않고 게임 기능과 문서만 갱신했다.
- 무전/응답은 잡음과 비프음 기반 임시 효과음이며 이해 가능한 한국어 음성은 아니다. 대사 녹음과 음성용 자막 디자인은 후속 작업이다.
- design.md의 12.3절(착석, 공간 변화로 진행 전달, 상시 HUD 지양)을 적용했다. 문서의 '머리 보호/배관 두드리기 후속 구현' 문구는 현재 구현보다 오래되어 기존 기능을 유지했다. 제안 공통 UI 자원 경로는 실제 존재하지 않는 것으로 확인했다.
- 실제 헤드셋에서 도달 거리, 빛 변화의 편안함, 양안 렌더링, 프레임 성능을 검증하지 않았다. PC 캡처와 모의 추적 검사를 실기기 검증으로 간주하지 않는다.
- 마지막은 흰 화면과 완료 상태에서 유지한다. 결과창/재시작/상위 메뉴는 이번 범위에 추가하지 않았다.
