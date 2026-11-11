# 시작 눈뜨기와 어두운 먼지 연출 — 2026-11-04

시작 직후 0.6초 동안 시야를 완전히 가린 뒤, 2.8초에 걸쳐 중앙의 틈이 위아래로 열리고 부드럽게 사라지는 연출을 추가했다. 총 3.4초이며 한 번만 자동 재생한다. 머리·컨트롤러 위치나 입력을 이동/차단하지 않는다.

먼지는 어둠 속에서 스스로 밝게 빛나던 재질을 교체했다. 손전등의 실제 빛 방향, 거리 감쇠와 그림자를 따라 입자/연무가 드러난다. 공간의 검은 환경광·안개색은 유지하며, 극히 희미한 틈새 빛과 미세한 잔광으로 불을 껐을 때에도 일부 먼지 윤곽을 남긴다. 손전등 자체의 세기/각도와 전화 기능은 바꾸지 않았다.

## 동작과 구현 범위

- 눈꺼풀은 카메라 아래의 충돌체 없는 메시 하나다. 휴대폰 Canvas보다 뒤에 그려지도록 정렬해 완전 암전 중 글자가 새어 나오지 않는다. 완료되면 Renderer를 끈다.
- PC 테스트 어댑터가 XR용 렌더러를 숨길 때 눈꺼풀은 제외한다. PC 시작에서도 암전이 유지되고, PC/XR 시뮬레이터 모드 전환이 눈꺼풀을 다시 켜지 않는다.
- 기존 먼지 텍스처와 파티클 13개를 재사용한다. 새 URP 셰이더는 양방향 산란, 실제 조명의 감쇠/그림자, 둥글고 불규칙한 밀도, 표면과의 깊이 기반 교차 완화, 눈 근처 페이드를 적용한다. 무거운 전체 화면 볼륨 레이마칭은 추가하지 않았다.
- 표면 교차 완화에 필요한 깊이 텍스처를 주 카메라에 설정하고, 장면의 카메라 프리팹 오버라이드로 저장했다. 전체 URP 에셋 설정은 바꾸지 않았다.
- 큰 먼지 판의 가장자리를 부드럽게 지우고 작은 먼지 알갱이는 축소했다. 시작 연무 버스트는 24개에서 36개로 조정했으며, 시작 입자는 총 346개다.
- 시작 안개 밀도는 0.9, 여진은 1.9다. 두 경우 모두 20초 유지, 다음 10초 소멸, 30초에 입자와 임시 안개 정리 규칙을 유지한다. 계속 생성하는 방출/루프를 추가하지 않았다.
- `Faint Dust Light Through Rubble` 조명 하나를 추가했다. 강도 0.008, 범위 1.65m의 작은 무그림자 스포트라이트다. 기존 테스트용/배경용 조명들은 원래의 비활성 상태로 둔다.
- 기존 18초 여진, 여섯 번의 충격, 벽 파손/철근/낙하 물리, 물병/휴대폰/파이프 그랩 구성과 단계 규칙을 유지한다.

## 변경 파일

- `Assets/_Project/Scripts/AwakeningReveal.cs` 및 메타: 시작 암전/눈뜨기, 완료, 반복 미리보기. Play Mode Inspector의 `Replay Opening Eyes (Play Mode)` 메뉴로 다시 볼 수 있다.
- `Assets/_Project/Scripts/AftershockDustEffect.cs`: 시작 밀도 기본값과 발광하지 않는 어두운 안개색.
- `Assets/_Project/Scripts/DesktopPhoneTestDriver.cs`: 눈꺼풀 Renderer를 XR 시각물 숨김 대상에서 제외.
- `Assets/_Project/Art/AwakeningEyelids.shader` 및 메타: 부드러운 눈꺼풀/페이드, 스테레오 인스턴싱 매크로 포함.
- `Assets/_Project/Art/LightScatteringDust.shader` 및 메타: 조명 반응, 그림자, 부드러운 경계와 깊이 교차 처리.
- `Assets/_Project/Art/Atmosphere/` 및 메타: 새 먼지/연무/눈꺼풀 재질 3개. 원래 Unlit 재질과 텍스처는 보존했다.
- `Assets/_Project/Scenes/ConfinedPrototype.unity`: 시작 연출, 새 재질, 먼지 조정, 희미한 틈새 조명, 카메라 깊이 설정 연결.
- `Assets/_Project/Docs/ExperienceRoadmap.md`: 현재 시작/먼지 연출 설명.
- `AgentScripts/InspectAwakeningLighting.cs`: 조명·카메라·먼지 조사와 변경 직전 장면 사본.
- `AgentScripts/BuildAwakeningAtmosphere.cs`, `TuneAwakeningAtmosphere.cs`: 에디터 구성/시각 조정 도구.
- `AgentScripts/ReviewAwakeningAtmosphere.cs`: 손전등 켬/끔, 입자 유무, 눈뜨기 단계, 조준 변경과 30초 정리 캡처.
- `AgentScripts/VerifyAwakeningReveal.cs`: 실제 프레임으로 자동 눈뜨기, 반복 재생, 추적 위치/전화 상태 보존 검사.
- `AgentScripts/VerifyAftershockAtmosphere.cs`: 조명 반응 셰이더 검증으로 갱신.

변경 직전 장면은 `Assets/_Project/Scenes/Backups/BeforeAwakeningLighting_20261104_162959.unity`에 보존했다. 코드/문서 사본은 `Temp/BeforeAwakeningLighting_20261104/`에 있다. 패키지/기기 전용 SDK를 추가하지 않았다.

## 검증 결과

- Unity 6000.3.22f1 C# 컴파일 오류/경고 0. 캡처 후 두 셰이더의 컴파일 메시지 없음.
- 매 Play Mode에서 `job wait`로 frameCount 증가를 확인했다.
- `VerifyAwakeningReveal`: PASS. 저장한 장면을 새로 시작한 뒤에도 카메라 깊이/눈꺼풀 정렬 설정 유지. 최종 실제 재생 1,306프레임에서 암전→부분 개방→완료 확인. 10회 반복에서 재질 재생성 없음. 머리/컨트롤러 위치와 휴대폰 손전등 상태 유지.
- `ReviewAwakeningAtmosphere`: PASS. 눈을 감은 캡처의 전체 픽셀 최댓값 0으로 완전 암전 확인. 실제 손전등 켬/끔과 30도 좌측 조준 변경 화면을 비교했다. 검사 전용 채움 조명은 사용하지 않았다.
- 비교 캡처의 화면 평균 밝기(0~1, 휴대폰 화면 포함): 시작은 손전등 끔 0.024 / 켬 0.231, 여진은 끔 0.031 / 켬 0.116. 파티클만 숨긴 비교 화면도 저장해 벽의 조명 변화와 먼지 산란을 구분했다. 파티클 난수와 실제 조준에 따라 수치는 달라질 수 있다.
- `VerifySettlingDust`: PASS. 시작 먼지 밀도 20/25/30초에서 0.9→0.45→0, 여진 1.9→0.95→0. 방출 반복 없음, 입자와 안개 함께 정리/복원.
- `VerifyAftershockAtmosphere`: PASS. 시작 입자 346개, 작은 파편 풀 116개, 여진 파티클 용량 896개, 반복 재생 시 재사용.
- `VerifyExtendedAftershock`: PASS. 18초/충격 6회, 세 차례 후반 벽 파손, 파편 24개, 오디오/먼지 종료 및 10회 복원 유지.
- `VerifyRoomTremor`: PASS. 최종 417프레임에서 공간 진동/복원, 컨트롤러 위치와 XR 시뮬레이터 카메라 위치 보존.
- 검증용 Play Mode 종료 후에도 현재 Console 오류 0, 새 경고/오류 없음. 기존 경고 1개와 과거 로그는 지우지 않았다. 에디터는 정지 상태로 돌리고 검사용 자동 틱을 해제했다.

초기 화면 검사에서 휴대폰 글자가 암전 위에 남는 문제를 찾아 렌더 순서를 고쳤다. 새 Play Mode 검증에서 카메라 깊이 설정의 프리팹 오버라이드가 빠진 것도 확인해 저장 처리를 보완했다. 최종 새 재생에서 두 설정을 검증했다.

캡처와 밝기 기록은 `Temp/AwakeningAtmosphereReview/` 및 `Review.json`에 있다.

- [눈을 뜨기 시작하는 화면](../Temp/AwakeningAtmosphereReview/05_Eyes_FirstSlit.png)
- [시작 먼지 / 손전등 끔](../Temp/AwakeningAtmosphereReview/01_Startup_LightOff.png)
- [시작 먼지 / 손전등 켬](../Temp/AwakeningAtmosphereReview/02_Startup_LightOn.png)
- [여진 먼지 / 손전등 끔](../Temp/AwakeningAtmosphereReview/08_Aftershock_LightOff.png)
- [여진 먼지 / 손전등 켬](../Temp/AwakeningAtmosphereReview/09_Aftershock_LightOn.png)
- [여진 먼지 / 왼쪽 조준](../Temp/AwakeningAtmosphereReview/09b_Aftershock_AimedLeft.png)

## 시각 확인과 남은 범위

`unity:craft-game-floors`의 `scene-lighting.md`, `vfx.md`, `visual-scorecard.md` 지침을 적용했다. 밝은 방향광/강한 환경광 등의 일반 레시피는 사용자가 요청한 매몰 공간의 암흑 분위기에 맞춰 적용하지 않았다. 현재 콘크리트/회갈색 먼지/휴대폰의 차가운 빛 팔레트를 따른다. UI 프레임워크 안내는 확인했으며 제품 UI는 추가하지 않았다.

이번 범위의 시각 평가: 어둠과 손전등의 대비 2→4/5, 시작 전환 1→4/5, 먼지와 배경의 일관성 3→3/5. 작은 알갱이와 불규칙한 연무를 쓰지만, 기존 배경은 저폴리이며 이 연무는 가벼운 파티클 기반 근사다.

실제 헤드셋의 양안 화면/착용감/프레임률은 검사하지 않았다. 깊이 텍스처와 조명 반응 셰이더의 실제 기기 비용, 패널별 암부 표현은 목표 기기에서 확인해야 한다. PC와 XR 시뮬레이터에서 검증했다.

확인한 Unity 6000.3 공식 문서: [Particles Lit](https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/particles-lit-shader), [URP 셰이더 조명](https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/shaders/writing-custom-shaders-urp/use-built-in-shader-methods/lighting), [URP 셰이더 그림자](https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/shaders/writing-custom-shaders-urp/use-built-in-shader-methods/shadows). 설치된 URP 17.3의 `RealtimeLights.hlsl`, `DeclareDepthTexture.hlsl`, 파티클 셰이더 코드도 확인했다.
