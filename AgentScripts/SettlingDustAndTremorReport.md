# 먼지 소멸 시간 및 여진 떨림 조정 — 2026-11-04

## 결과

- 시작과 여진의 먼지는 각각 발생 시점부터 20초간 뿌연 상태를 유지하고,
  20~30초에 천천히 가라앉는다. 여진이 끝나는 8초 시점에 먼지 시계를 재시작하지 않는다.
- 모든 먼지 시스템의 loop, rateOverTime, rateOverDistance를 끄고 일회성 방출만 남겼다.
- 큰 밝은 점을 줄이고 더 작은 회색 입자가 느리게 아래로 흐르도록 바꿨다.
  입자와 fog를 함께 흐리게 하며 30초에 입자를 정리한다. 원래 fog와 렌더러 설정은 복원한다.
- 기존 천장 붕괴, 64개 파편, 머리 보호와 단계 전환은 유지한다.
- 앞쪽 잔해를 포함한 주변 구조물 13개 그룹/38개 메시가 같은 리듬으로 떨린다.
  기존 벽과 천장의 충격 움직임에도 이 공통 진동을 더했다.
- PC 테스트에서는 데스크톱 어댑터가 실제 카메라에 짧고 빠른 진동을 적용한다.
  실제 HMD가 연결되면 이 어댑터는 비활성화된다. VR은 주변 구조물의 떨림을 사용한다.

## 수정 파일

- Assets/_Project/Scripts/AftershockDustEffect.cs — 유한한 20초 유지/10초 페이드,
  입자 렌더러의 공통 투명도 페이드와 정리, 원래 fog/프로퍼티 블록 복원.
- Assets/_Project/Scripts/AftershockEnvironmentMotion.cs — 주변 공간의 공통 진동,
  PC 시점 진동 값과 종료/복원 처리.
- Assets/_Project/Scripts/DesktopPhoneTestDriver.cs — PC 전용 카메라 진동 적용.
- Assets/_Project/Scenes/ConfinedPrototype.unity — 먼지와 진동 대상/값 연결.
- Assets/_Project/Docs/ExperienceRoadmap.md — 현재 시간 기준과 VR/PC 동작 반영.
- AgentScripts/VerifyAftershockAtmosphere.cs — 변경된 먼지 수명과 유지 시간에 맞춘 회귀 검증.
- AgentScripts/AftershockAtmosphereReport.md — 이전 기록임을 표시하고 이 보고서로 연결.

추가 파일:

- AgentScripts/InspectAftershockRoom.cs
- AgentScripts/TuneSettlingDust.cs
- AgentScripts/ConnectRoomTremor.cs
- AgentScripts/VerifySettlingDust.cs
- AgentScripts/VerifyRoomTremor.cs
- AgentScripts/SettlingDustAndTremorReport.md
- Assets/_Project/Scenes/Backups/BeforeSettlingDust_20261104_130327.unity 및 메타 파일

수정 전 코드 복사본: Temp/BeforeSettlingDust_20261104_130329/.
미저장 씬 상태까지 백업했다. 기존 사용자 변경, XR 리그, 입력 액션과 패키지는 보존했다.
새 이미지, 외부 패키지나 기기 전용 SDK는 추가하지 않았다.

## 검증

- Unity 6000.3.22f1 컴파일 오류/컴파일 경고 0.
- job wait로 Play Mode frameCount 증가 확인.
- VerifySettlingDust: PASS.
  - 모든 시스템이 유한한 방출을 사용하며 반복 생성이 없다.
  - 시작 먼지 농도: 20초 0.65 → 25초 0.325 → 30초 0.
  - 여진 먼지 농도: 20초 1.9 → 25초 0.95 → 30초 0.
  - 입자의 렌더러 투명도도 함께 줄고 30초에 입자가 남지 않는다.
  - 원래 fog 활성/모드/농도/색상과 리플레이 초기화 확인.
  - 시간 진행 API와 파티클 시뮬레이션으로 해당 시점에 맞춰 캡처했다.
- VerifyRoomTremor: PASS.
  - 실제 실행 프레임 415개 구간에서 샘플링했다.
  - PC 시점 이동 최대 약 2.58mm, 회전 최대 약 0.60도(표본 구간 측정).
  - 앞쪽 잔해 이동 최대 약 10.24mm, 13개 그룹이 공통 변환을 따르는지 확인.
  - 시점과 구조물의 원상복구, 잔여 진동 없음 확인.
  - 컨트롤러 위치/회전 유지, PC 모드를 끈 XR 시뮬레이션 카메라 위치/회전 유지 확인.
- VerifyAftershockFoundation: PASS. 기존 보호/피격, 붕괴, 자동 시작, 단계 전환과 복원 확인.
- VerifyAftershockAtmosphere: PASS. 파편 풀 64개/활성 63개, 10회 반복 재사용과 정리 확인.
- 최종 Console 오류 0. 기존 OpenXR/시뮬레이터 경고는 남아 있다.
- 검증을 마치고 편집 모드로 돌아왔으며 테스트용 자동 갱신을 껐다.

## 시각 확인과 한계

craft-game-floors의 game-feel, vfx, visual-scorecard 지침을 먼지와 진동 범위에 적용했다.
기존 콘크리트 회색 팔레트와 텍스처를 사용했다. 일반 액션 게임용 카메라 진동은
PC 테스트 어댑터에만 적용했고 실제 VR의 추적 시점을 강제로 움직이지 않는다.
먼지 수명은 사용자의 30초 요청을 따른다.

- style uniformity: 3/5 — 큰 밝은 점을 줄이고 흐린 미세 분진과 기존 콘크리트 색을 유지.
- feedback visibility: 4/5 — 20/25/30초 페이드를 캡처로 확인하고 시점/공간 떨림을 실제 프레임에서 확인.

캡처: Temp/SettlingDustReview/의 Start_20s/25s/30s.png 및 Aftershock_20s/25s/30s.png.
정지 캡처는 먼지 시간 변화를 확인하기 위한 것이며 떨림 체감은 실행 상태에서 확인해야 한다.
실제 헤드셋의 착용감, 멀미와 기기 성능은 테스트하지 않았다.
