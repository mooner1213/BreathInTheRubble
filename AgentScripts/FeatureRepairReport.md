# 기능 연결 및 배터리 경고 수정 — 2026-11-04

## 원인과 결과

기존 코드와 PC 테스트 버튼은 있었으나 ConfinedPrototype 씬에 GameSequenceController,
AftershockDirector, HeadProtectionDetector와 PhoneLowBatteryWarning이 연결되지 않았다.
씬을 백업한 뒤 기존 여진 코드를 연결하고 스마트폰 프리팹과 씬의 배터리 UI를 수정했다.

- 표시 잔량이 24% 이하가 되면 숫자, 배터리 테두리, 채움과 캡을 빨갛게 표시한다.
- 스마트폰 화면 위쪽에 현재 잔량이 적힌 경고 팝업을 띄운다. X로 닫기 전까지 유지한다.
- 닫은 뒤에는 같은 저전력 상태에서 반복 표시하지 않는다. 25% 이상 복원 후 다시 내려오면 재표시한다.
- PC 테스트의 24% 버튼은 휴대폰을 집어 깨우고 팝업을 다시 표시한다.
- 여진 시작/재시작, 단계 전환, 연출 복원과 PC 머리 보호 토글이 씬 컴포넌트를 작동시킨다.
- 테스트 창에서 단계, 경과 시간, 보호 판정과 방어/피격 횟수를 확인할 수 있다.

## 이번에 수정한 파일

- Assets/_Project/Scripts/PhoneBattery.cs — 표시 잔량 24% 이하 판정.
- Assets/_Project/Scripts/PhoneBatteryView.cs — 빨간 숫자/전체 아이콘, 정상 색상 복원. 숫자 전환은 즉시, 소수점 채움 갱신은 초당 1회.
- Assets/_Project/Scripts/PhoneLowBatteryWarning.cs — 지속 팝업, 현재 잔량, 닫기와 재진입 처리.
- Assets/_Project/Scripts/DesktopPhoneTestDriver.cs — 배터리 미리보기에서 집기/깨우기와 경고 재표시, 보호 토글 상태 노출.
- Assets/_Project/Editor/DesktopPhoneTestWindow.cs — 기능 연결 여부, 단계 및 보호/피격 결과 표시.
- Assets/_Project/Scripts/GameSequenceController.cs — 연출 복원 시 Rescue 미리보기 플래그 해제.
- Assets/_Project/Prefabs/SmartphonePrototype.prefab — 경고 UI와 배터리 색상 참조 연결.
- Assets/_Project/Scenes/ConfinedPrototype.unity — 스마트폰 경고와 여진/머리 보호/단계 전환 연결.
- Assets/_Project/Docs/ExperienceRoadmap.md — 현재 연결 상태와 배터리 동작 반영.
- AgentScripts/BuildAftershockFoundation.cs — 실행 전 대상/리소스 확인, 배터리 설정과 분리, 기존 재질 보존.

## 추가한 파일 및 적용한 기존 파일

- AgentScripts/ConfigurePhoneLowBatteryWarning.cs — 씬/프리팹 백업 후 배터리 UI 연결.
- AgentScripts/VerifyPhoneLowBatteryWarning.cs — 실제 스마트폰 인스턴스의 배터리 경고 검증.
- AgentScripts/VerifyAftershockFoundation.cs — 실제 여진과 보호 판정, 단계 전환 검증.
- AgentScripts/FeatureRepairReport.md — 이 보고서.
- Assets/_Project/Art/Aftershock/HeadImpactEdges.mat 및 Unity가 생성한 메타 파일.
- 씬 백업: Assets/_Project/Scenes/Backups/BeforeLowBatteryWarning_20261104_114113.unity
- 씬 백업: Assets/_Project/Scenes/Backups/BeforeAftershock_20261104_115148.unity
- 프리팹 백업: Temp/PhoneWarningBackups/SmartphonePrototype_20261104_114113.prefab

기존에 작성되어 있던 AftershockAudio, AftershockDebris, AftershockDirector,
AftershockEnvironmentMotion, HeadImpactFeedback, HeadProtectionDetector와
HeadImpactEdges.shader는 이번에 씬에서 사용하도록 연결했다.

## 검증 결과

- Unity 6000.3.22f1 최종 컴파일: 오류 0, 컴파일 경고 0.
- PC Play Mode 프레임 진행 확인: job wait에서 frameCount 변경 감지.
- VerifyPhoneLowBatteryWarning: PASS. 25%/24% 경계, 실제 소모로 임계점 통과,
  전체 아이콘과 숫자의 빨간색, 팝업 유지/닫기/복원/반복 테스트, 0%, 화면 범위,
  텍스트 넘침 여부, 통화 화면과의 공존을 확인했다.
- VerifyPhoneBattery: PASS. 기존 소모율, 합산 소모, 실패한 통화의 소모 중단,
  60,000회 소수점 누적, 표시 잔량, 아이콘 폭과 0% 제한을 확인했다.
- VerifyAftershockFoundation: PASS. 씬 연결, 양손 위치 판정, PC 보호 토글,
  세 번의 보호/미보호 충격, 붉은 피격 효과, 천장 하강 제한, 먼지와 파편,
  AudioSource 재생 상태, 원상복구와 4단계 미리보기를 확인했다.
  자동 75초 시작과 구조 조건 미충족 시 Isolation 유지도 확인했다.
  여진 실행 전후 HMD 카메라 위치/회전이 동일하며 XR Origin은 1개였다.
- 스마트폰 캡처를 직접 확인했다: Temp/EmergencyPhoneCloseup.png
- 최종 Console: 오류 0. 테스트를 종료하고 편집 모드로 복귀했다.
  테스트용 백그라운드 실행 오버라이드를 해제하고 자동 갱신을 종료했다.

## 남은 사항

- Rescue는 상태 전환/이벤트까지만 구현되어 있다. 실제 구조 연출과 통화 대사는 남아 있다.
- 실제 헤드셋, 컨트롤러 및 진동은 테스트하지 않았다. 효과음은 AudioSource 재생 상태만 검증했다.
- 작업 전부터 있던 OpenXRPackageSettings.asset 임포터 불일치/타임스탬프 경고는 남아 있다.
  관련 XR 설정과 기존 사용자 변경은 수정하지 않았다.
