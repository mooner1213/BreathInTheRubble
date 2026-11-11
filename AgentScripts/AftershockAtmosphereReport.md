# 여진·시작 분진 강화 — 2026-11-04

이전 강화 작업의 기록이다. 현재 먼지 수명과 시점/공간 떨림은
[후속 수정 보고서](SettlingDustAndTremorReport.md)를 따른다.

시작부터 먼지가 보이고 여진 때 시야가 뿌예지도록 ConfinedPrototype 씬을 수정했다.
기존 먼지 재질은 조명 영향을 받아 어두운 장면에서 거의 보이지 않았다. 기존 재질을
보존하고 회색 콘크리트 색상의 새 URP Unlit 먼지 재질 두 개를 추가했다.

## 동작

- 시작: 미세 분진 360개와 부유 입자 170개, 먼지 베일 34개를 초기 방출한다.
  부유 입자와 얇은 분진층은 계속 유지되며, 시작 시의 짙은 안개는 서서히 빠진다.
- 여진: 기존 미세 먼지 최대 150개를 600개로 늘리고, 짙은 베일 최대 72개와
  균열 분진 3곳(각 최대 24개)을 추가했다. 충격마다 분진을 재방출한다.
- 안개: 여진 중 시야를 가리는 회색 분진이 짙어지고 종료 후 약 14초 동안 가라앉는다.
  종료/원상복구 시 원래 fog 활성 상태, 모드, 농도와 색상을 복원한다.
  런타임 효과도 기존 씬과 같은 ExponentialSquared 모드를 사용한다.
- 파편: 6개에서 64개로 늘렸다. 예비 낙하와 세 차례 붕괴 순간에 나눠 떨어지고,
  낙하 가속·회전·착지 튀김을 적용한다. 런타임 생성/파괴 없이 같은 파편을 재사용한다.
- 천장: 8초 내내 부드럽게 내려오던 움직임을 없애고 3.2/4.8/6.6초에 세 번 끊어
  무너지는 움직임으로 바꿨다. 판마다 시차를 두고 꺾이며 최대 하강량은 14cm다.
  머리 주변에서는 기존 여유 공간 계산과 회전 여유량으로 움직임을 줄인다.
- 흔들림: 벽의 기준 이동 폭을 3mm에서 18mm로 높이고 회전 및 충격 진동을 추가했다.
  VR 카메라와 추적 컨트롤러의 위치/회전에는 효과를 적용하지 않는다.
- 파편은 콜라이더/리짓바디를 추가하지 않고 머리/손 근처 경로에서 숨긴다.

## 수정/추가 파일

- Assets/_Project/Scripts/AftershockEnvironmentMotion.cs
- Assets/_Project/Scripts/AftershockDebris.cs
- Assets/_Project/Scripts/AftershockDirector.cs
- Assets/_Project/Scripts/AftershockDustEffect.cs 및 메타 파일 (추가)
- Assets/_Project/Scenes/ConfinedPrototype.unity
- Assets/_Project/Docs/ExperienceRoadmap.md
- Assets/_Project/Art/Aftershock/AtmosphericDustUnlit.mat 및 메타 파일 (추가)
- Assets/_Project/Art/Aftershock/AtmosphericVeilUnlit.mat 및 메타 파일 (추가)
- Assets/_Project/Art/Aftershock/FallingConcreteInstanced.mat 및 메타 파일 (추가)
- AgentScripts/UpgradeAftershockAtmosphere.cs (추가)
- AgentScripts/AtmospherePreview.cs (추가)
- AgentScripts/VerifyAftershockAtmosphere.cs (추가)
- AgentScripts/VerifyAftershockFoundation.cs (기존 고정 4.5cm 검사를 현재 설정 범위 검사로 변경)
- AgentScripts/AftershockAtmosphereReport.md (이 보고서)

씬 변경 전 미저장 상태까지 복사한 백업:
Assets/_Project/Scenes/Backups/BeforeStrongerAftershock_20261104_123859.unity 및 메타 파일.
기존 수정 중인 코드를 보존한 복구용 스냅샷: Temp/BeforeAtmosphere_20261104_123812/.
기존 사용자 수정이 있는 작업 트리이므로 전체를 임의로 커밋하는 대신 파일/씬 백업을 사용했다.
패키지, 기기 전용 SDK, 렌더 파이프라인, 기존 공용 조명/먼지 재질은 변경하지 않았다.

## 검증

- Unity 6000.3.22f1 컴파일 통과: 오류 0, 컴파일 경고 0.
- job wait로 실제 frameCount 증가 확인 후 게임 카메라에서 캡처했다.
- VerifyAftershockAtmosphere: PASS.
  - 0.65초 시점 시작 먼지 572개, 60초 시점에도 부유 먼지 유지.
  - 모든 시작 먼지가 조명에 의존하지 않는 전용 Unlit 재질 사용.
  - 첫 붕괴 전 천장 위치 유지, 충격 때 가속 하강, 충격 사이 하강 정지, 판 꺾임 확인.
  - 벽 움직임 증가, 원상복구 시 천장 위치와 회전 복원.
  - 풀 64개 중 63개 활성 확인, 콜라이더/리짓바디 없음.
  - 여진용 파티클 총 최대 744개, 시작/상시 먼지와 별도 관리.
  - 여진 종료 4초 후 분진 농도 1.523, 이후 원래 fog 값 복원.
  - 10회 반복해도 파편 ID와 파티클 시스템 수 동일.
  - HMD와 양쪽 컨트롤러 위치/회전 동일.
- VerifyAftershockFoundation: PASS. 머리 보호, 세 번의 피격 판정, 단계 전환,
  자동 75초 시작, 구조 조건, 연출 복원과 XR Origin 1개를 확인했다.
- Console 오류 0. 작업 전부터 있던 OpenXR 임포트/시뮬레이터 진동 경고는 남아 있다.
- PC 검증이며 실제 헤드셋/실기기 성능이나 착용감을 테스트하지 않았다.

## 시각 확인 및 범위

게임 연출 스킬의 game-feel, vfx, visual-scorecard 참고 문서를 읽고 VFX 범위에 적용했다.
기존의 어두운 콘크리트 장면과 회색/갈색 팔레트를 따랐다. 일반 액션 게임용 카메라
흔들림/시간 정지는 이 프로젝트의 VR 시점 규칙에 맞춰 적용하지 않았다. 오래 머무는
분진은 이번 요청에 따라 일반적인 짧은 이벤트 이펙트 수명보다 길게 유지했다.

범위를 먼지와 붕괴 피드백으로 한정한 자체 시각 평가:

- feedback visibility: 1/5 → 4/5 — 거의 검던 시작 장면에서 먼지가 보이고 여진 시 잔해가 분진에 가려진다.
- palette discipline: 3/5 → 3/5 — 첫 시안의 노란 기를 줄이고 콘크리트 회색으로 통일했다.
- style uniformity: 3/5 → 3/5 — 기존 각진 콘크리트 메시/텍스처를 재사용했다.

첫 캡처 후 색과 입자 크기를 수정한 뒤 최종 캡처를 다시 확인했다.
정지 캡처는 먼지 밀도 확인용이며 실기기 움직임이나 성능 측정을 의미하지 않는다.

- 시작: [After_Start.png](../Temp/AtmosphereReview/After_Start.png)
- 상시 먼지: [After_Ambient.png](../Temp/AtmosphereReview/After_Ambient.png)
- 여진(피격 효과 포함): [After_Aftershock.png](../Temp/AtmosphereReview/After_Aftershock.png)
- 여진 분진: [After_DustOnly.png](../Temp/AtmosphereReview/After_DustOnly.png)
- 비교용 원본: Temp/AtmosphereReview/Before_*.png

확인한 Unity 6000.3 공식 문서:
[ParticleSystem.Emit](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/particlesystem/emit),
[ParticleSystemRenderer](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/particlesystemrenderer),
[ObjectPool](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/pool/objectpool1),
[runInBackground](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/application/runinbackground),
[timeScale](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/time/timescale).
기존 안개 모드 유지에는 [Graphics 설정의 Fog Modes 문서](https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/editor-settings-reference/comp-manager-group/class-graphics-settings)도 참고했다.
