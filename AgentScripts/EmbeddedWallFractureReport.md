# 뒤쪽 벽 균열과 철근 노출 수정 — 2026-11-04

공중의 네 조각이 바깥으로 펼쳐지던 연출을 기존 벽의 실제 표면이 뜯겨 떨어지는 방식으로 교체했다.

## 적용 내용

- 기존 `Rear Left Fractured Panel`, `Rear Diagonal Reinforced Slab`, `Rear Inner Fracture Right`의 메시를 직접 분할했다. 여진 전 외형, 기존 표면의 UV·법선·콘크리트 재질을 이어 사용한다.
- 각 벽에서 비대칭 표면 조각 두 개가 서로 다른 시점에 떨어진다. 부착 가장자리에서 짧게 벌어진 뒤 중력과 회전, 낮은 잔해와 바닥의 충돌로 내려앉는다. 총 여섯 개의 큰 조각이다.
- 철근은 처음부터 벽 안에 고정되어 있다. 콘크리트가 떨어지면 드러나며, 양 끝은 남은 콘크리트 속으로 들어간다. 철근 전체가 앞으로 자라거나 꽃처럼 펼쳐지는 애니메이션을 제거했다.
- 세 균열의 먼지와 추가 파편 24개가 실제 벽의 파손 지점에서 발생한다. 뒤쪽 작은 파편 52개의 착지 위치를 낮은 잔해/바닥 기준으로 맞추고 기존 벽 재질로 통일했다.
- 큰 조각은 겹쳐 배치된 상부 배경 구조물 및 서로 간의 충돌을 제외하고 낮은 잔해/바닥과 충돌한다. 휴대폰·물병·금속 파이프와 충돌하지 않으며, 추적 장치에 가까워지면 해당 장식 조각을 숨기고 물리를 멈춘다. 전역 충돌 레이어 설정은 바꾸지 않았다.
- 18초 여진, 10.2/13.4/16.2초의 후반 파손, 초기 세 차례 천장 붕괴, 먼지 20초 유지/10초 소멸 규칙을 유지했다.
- 철근의 95개 작은 메시를 벽별로 합쳤고, 고정 콘크리트 18개도 벽별로 합쳤다. 새 벽 연출의 활성 MeshRenderer는 12개, Collider는 고정 3개와 동적 6개다.
- 기존 벽 오브젝트와 원본 메시를 보존했다. 원래 벽의 Renderer/Collider는 비활성화하고 새 분할 벽을 연결했다. 이전 공중 균열 구성도 `Archived Floating Fractures - Inactive`로 보존했다.

## 변경 파일

- `Assets/_Project/Scripts/AftershockFractureSection.cs`: 원래 벽과 함께 움직이는 고정 철근, 표면 낙하 조각, 파손 위치 분진을 제어.
- `Assets/_Project/Scripts/AftershockFallingChunk.cs` 및 `.meta`: 부착 가장자리 파손, 중력 낙하, 충돌 필터링, 추적 장치 근접 처리와 반복 재생 복원.
- `Assets/_Project/Scenes/ConfinedPrototype.unity`: 실제 벽 분할 구성, 철근, 큰 조각 6개, 기존 뒤쪽 파편/분진의 위치와 참조 연결.
- `Assets/_Project/Art/Aftershock/EmbeddedWallFractures/` 및 메타 파일: 기존 벽에서 분할한 새 메시 및 합친 메시 30개. 원본 아트/재질 파일은 수정하지 않았다.
- `Assets/_Project/Docs/ExperienceRoadmap.md`: 현재 벽 파손 방식과 물리/구조물 수를 반영.
- `AgentScripts/InspectEmbeddedWalls.cs`: 실제 벽/충돌체 조사 및 변경 직전 장면 사본 저장.
- `AgentScripts/BuildEmbeddedWallFractures.cs`: 기존 벽 메시의 표면/내부/조각 분할과 장면 구성.
- `AgentScripts/TuneEmbeddedWallDynamics.cs`: 겹친 배경 구조물의 충돌 제외와 작은 파편의 실제 착지 위치 설정.
- `AgentScripts/ConsolidateEmbeddedWallMeshes.cs`: 고정 콘크리트와 철근 메시 합치기.
- `AgentScripts/ReviewEmbeddedWallFractures.cs`: 실제 프레임/PhysX 재생, 전후 캡처, 낙하/회전/착지/반복 복원/상호작용 물체 충돌 제외 검사.
- `AgentScripts/VerifyExtendedAftershock.cs`, `VerifySettlingDust.cs`, `VerifyAftershockAtmosphere.cs`: 새 구성과 벽에 붙은 분진 참조를 검사하도록 보완.
- `AgentScripts/ExtendAftershockSequence.cs`: 기존 공중 균열을 다시 만드는 옛 도구가 새 구성을 중복 생성하지 않도록 제한.

변경 직전 장면: `Assets/_Project/Scenes/Backups/BeforeEmbeddedWallFracture_20261104_153225.unity`.
기존 코드 사본: `Temp/BeforeEmbeddedWallFracture_20261104_152854/`.

## 검증

- Unity 6000.3.22f1 컴파일 오류/컴파일 경고 0.
- `job wait`로 실제 frameCount 증가 확인 후 재생했다.
- `ReviewEmbeddedWallFractures`: PASS. 최종 구성에서 7,654 프레임이 진행되는 동안 검사했다. 큰 조각 여섯 개 모두 약 58~83cm 내려갔고 약 58~160도 회전한 뒤 정지했다. 최종 샘플에서 여섯 조각 모두 표시 상태, 선속도 0이었다.
- 큰 조각과 모든 상호작용 물체의 충돌 제외를 확인했다. 재생 중 기존 물체의 최대 위치 변화는 약 1.02cm였으며, 검사 기준 2.5cm 이내였다. 물체를 강제로 원위치에 고정하거나 그랩 설정을 바꾸지 않았다.
- 세 국소 분진 모두 30초 에피소드 후 파티클 0. 실제 시간으로 재생하는 검사에서는 먼지의 자동 Update만 사용했다.
- `VerifyExtendedAftershock`: PASS. 18초, 충격 6회, 후반 파손 순서, 후반 파편 24개, 오디오 종료, 먼지 소멸, 10회 재생 복원.
- `VerifyAftershockFoundation`: PASS. 자동 75초 시작, 보호/피격, 단계 전환, XR Origin 단일 구성.
- `VerifySettlingDust`: PASS. 시작 먼지 0.65→0.325→0, 여진 먼지 1.9→0.95→0을 20/25/30초에서 확인.
- `VerifyAftershockAtmosphere`: PASS. 작은 파편 풀 116개, 여진 파티클 용량 896개, 반복 생성 없이 재사용.
- `VerifyRearAftershockCollapse`: PASS. 기존 뒤쪽 천장 3개/파편 28개/분진 2개 유지. 붕괴 후 물병 위 천장 여유 약 25cm.
- `VerifyRoomTremor`: PASS. 구조물 15개가 함께 움직이고, PC 진동 및 복원 확인. 컨트롤러와 XR 시뮬레이터 추적 위치 유지.
- 최종 실행 시 Console 현재 오류 0, 새 경고/오류 없음. 기존 경고 1개와 이전 세션의 과거 오류 기록은 지우지 않았다.
- 검증용 Play Mode를 종료하고 자동 틱을 해제했다. 임시 검사 카메라/조명은 제거했다.
- Play Mode 종료 후에도 현재 Console 오류 0이며 새 경고/오류가 없었다. `git diff --check`는 Unity가 저장한 장면/프리팹 및 기존 XR 설정의 빈 YAML 값 뒤 공백을 보고하므로 통과로 기록하지 않았다. Unity 직렬화 파일을 수동으로 정리하지 않았다.

첫 검사에서 큰 조각이 벽의 절단면에 걸리는 문제가 발견되어 충돌 대상을 조정했다. 최종 검사에서는 여섯 개 모두 바닥/낮은 잔해까지 떨어져 멈췄다.

## 화면 확인과 한계

`Temp/EmbeddedWallReview/`에 여진 전, 9.9~31초 후면 화면, 각 벽의 파손 전후 확대 화면을 저장했다. `PhysicsVerification.json`에 프레임과 위치/회전/속도 측정값이 있다.

- [여진 전](../Temp/EmbeddedWallReview/01_Intact_Rear.png)
- [여진 중 분진](../Temp/EmbeddedWallReview/03_Rear_11.70.png)
- [먼지가 가라앉은 뒤](../Temp/EmbeddedWallReview/03_Rear_31.00.png)
- [철근과 남은 벽 확대](../Temp/EmbeddedWallReview/04_Exposed_Wall_1.png)

형태를 확인하는 캡처에는 임시 점광원을 사용했다. 실제 HMD 카메라는 검사 목적으로 움직이지 않았다. 실제 헤드셋 착용감/프레임률/컨트롤러로 물병을 집는 동작은 검증하지 않았다. 최종 착지 모양은 PhysX의 접촉 상황에 따라 조금 달라질 수 있다.

`unity:craft-game-floors` 지침의 범위 평가: 기존 벽과의 일관성 2→4/5, 파손/낙하의 인과관계 2→4/5, 기존 팔레트 유지 4/5. 기존 저폴리 콘크리트 배경을 따르는 시각 평가이며 실사 수준이나 헤드셋 성능 점수가 아니다.

확인한 Unity 공식 문서: [Rigidbody.isKinematic](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody-isKinematic.html), [Physics.IgnoreCollision](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.IgnoreCollision.html), [MeshCollider.convex](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MeshCollider-convex.html).
