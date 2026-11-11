# 여진 중 쇠파이프 낙하와 타격음

2026-11-05 구현 및 PC 검증 완료.

## 구현한 동작

- 기존 앞쪽 천장이 기울고 내려앉는 동안, 같은 천장에 연결된 경로에서 파이프가 서서히 미끄러져 나온다.
- 여진 상대 시간 4.95초에 등장하고, 1.6초 미끄러짐 → 0.5초 기울어짐 → 0.4초 낙하 → 0.24초 작은 착지 흔들림을 거친다.
- 약 7.69초에 착지하고 잡을 수 있다. 고립 단계에도 남는다.
- 길이 46cm, 바깥 지름 2.2cm의 속이 빈 파이프다. 착지 위치는 (-0.025, 0.493, 0.250)m이다.
- 첫 잡기 전에는 원통이 경사진 잔해에서 굴러 사라지지 않도록 위치를 유지한다. 첫 잡기에서 제약을 해제하고, 이후 놓으면 중력과 충돌이 적용된다.
- 기존 접촉 전용 잡기 필터와 그립 유지 방식을 재사용한다. 가까이 손을 대서 잡으며 임의의 먼 거리 잡기를 추가하지 않았다.
- 벽과 잔해를 치면 짧은 둔탁한 소리, 철근을 치면 긴 금속 울림이 난다. 타격 속도에 따라 음량이 달라진다.
- 정지 접촉과 잦은 충돌 반복은 속도 하한과 0.12초 재생 간격으로 제한한다. 회전으로 휘두르는 끝부분의 속도도 반영한다.
- 기존 철근의 형태를 따라 좁은 MeshCollider를 추가했다. 장식용 붕괴 파편과 새 철근/파이프 사이의 충돌은 무시한다.
- 휴대폰, 물병, XR Origin, 추적 장치의 위치를 변경하지 않았다. 새 SDK나 UI는 추가하지 않았다.

## PC 확인 방법

1. 기존 PC Phone Test에서 데스크톱 모드를 켜고 여진을 실행한다.
2. 착지 후 P로 파이프를 집거나 놓는다.
3. 기존 오른쪽 마우스 시점 조작으로 벽이나 철근을 바라보고 J로 휘두른다.

XR Interaction Simulator를 통한 기존 상호작용 경로도 유지한다. PC 어댑터는 실제 HMD가 감지되면 작동하지 않는다.

## 검증 결과

- Unity 재컴파일: 오류 0, 경고 0. 최종 콘솔 groundTruth: compilationFailed=false, consoleErrors=0.
- VerifyAftershockPipe.Run: PASS.
  - 등장 전 비표시/잡기 차단, 미끄러짐/하강, 착지 후 활성화 확인.
  - 물리 시뮬레이션 5초 후에도 첫 잡기 전 착지 위치 유지. 기준 머리 위치에서 중심까지 약 47.8cm.
  - 추적 장치를 움직이지 않는 임시 접촉 인터랙터로 실제 XRI 선택/해제 확인.
  - PC 잡기/놓기에서 초기 위치 제약 해제 및 XR 잡기 복원 확인.
  - 임시 콘크리트/금속 충돌면에 실제 Rigidbody 충돌을 일으켜 각각 한 번의 음향 요청 확인.
  - 약한 접촉 차단, 재생 간격, 고립 단계 유지, 반복 실행 초기화 확인.
  - 장식용 벽 파편의 파이프 충돌 무시 및 XR Origin 1개, HMD 위치 보존 확인.
- VerifyExtendedAftershock.Run: PASS. 기존 18초 여진, 여섯 충격, 세 후반 균열, 분진 정리, 물병 유지, 10회 재실행 확인.
- Temp/PipeReview/Landed_InspectionLight.png 및 Sliding_InspectionLight.png를 열어 배치 확인. 임시 검사 조명을 사용했으므로 실제 게임의 밝기 검증과 구분한다.
- 검사 종료 후 Play Mode를 종료했다. 임시 검사 카메라/조명/충돌면/인터랙터는 제거했다.

## 변경 파일

- Assets/_Project/Scenes/ConfinedPrototype.unity — 파이프, 천장 경로, 착지 지점, 철근 충돌면, 타격면 연결.
- Assets/_Project/Scripts/AftershockDirector.cs — 외부 연출을 위한 진행/복원 이벤트 추가.
- Assets/_Project/Scripts/AftershockPipeDrop.cs — 낙하 연출과 최초 착지/잡기 전환.
- Assets/_Project/Scripts/PipeImpactAudio.cs — 물리 충돌 기반 소리 재생.
- Assets/_Project/Scripts/PipeImpactSurface.cs — 콘크리트/금속 표면 분류.
- Assets/_Project/Scripts/DesktopPipeTestDriver.cs — 에디터 전용 P/J 물리 조작.
- Assets/_Project/Art/MetalPipe/ — 파이프 메시, 재질, 물리 재질, 긁힘/착지/벽/금속 WAV 4개 및 메타 파일.
- AgentScripts/InspectPipeSetup.cs, BuildAftershockPipe.cs, VerifyAftershockPipe.cs — 조사/씬 구성/검증 스크립트.
- 이 보고서 및 Unity가 생성하는 신규 메타 파일.

변경 전 씬: Assets/_Project/Scenes/Backups/BeforeAftershockPipe_20261105_144917.unity.

## 남은 확인

- 실제 헤드셋에서 앉은키별 도달 거리, 낙하 경로의 편안함, 물체 크기와 타격 느낌을 확인해야 한다. 실제 장치 테스트는 수행하지 않았다.
- 사운드는 합성한 프로토타입 WAV다. 실제 녹음으로 교체하거나 음량/음색을 조정할 수 있다.
- 작업 중 구형 선택 해제 API 오류는 수정했다. 초기 검증에서 잘못 고른 Poke 인터랙터가 선택을 거부한 과거 경고는 접촉 인터랙터로 검증을 수정한 뒤 재발하지 않았다. Pipeline 기록에는 과거 오류/경고도 남아 있으므로 현재 groundTruth와 구분한다.
- 이번 작업 전부터 기록된 OpenXR 설정 임포트 경고는 이 기능의 변경 범위에 포함하지 않았다.
- 구조 진행/파이프 신호 횟수 판정은 이번 기능에 추가하지 않았다.
