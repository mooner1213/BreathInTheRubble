# 여진 파편·쇠파이프·개별 구조 작업 수정

## 적용한 동작

- 파이프의 보관 시작점을 y=1.20m에서 **1.72m**로 올리고 잔해 안쪽(z=.58m)으로 옮겼다. 렌더러를 갑자기 켜지 않고 시작부터 잔해 뒤에 존재한다. 미끄러짐→모서리에서 기울어짐→낙하→짧은 착지를 거쳐 기존 잡기/타격 물체로 전환한다. 착지는 보이는 받침 메시에서 다시 측정한 y≈.455m이다.
- 116개 장식 파편을 정해진 공중 좌표로 움직이던 구현을 **28개 재사용 물리 파편**으로 교체했다. 원래 이음새에 붙어 있다가 떨어져, 실제 중력·회전·메시 충돌로 착지한다. 떨어질 때 흔들리는 환경 부모에서 분리해 움직임에 끌려가지 않는다. 머리/손과 휴대폰·물병·파이프를 보호하는 제외/숨김 처리를 유지했다.
- 겹침이 심했던 기존 가까운 장식 렌더러 375개를 비활성화했다. 깊은 차광용 유료 잔해층은 유지하고 가까운 부분은 **유료 에셋 21개**로 기초석→작은 받침→기울어진 큰 조각→천장 가장자리의 구조로 재배치했다. 보이는 새 메시와 같은 메시를 충돌면으로 사용한다. 기존 벽체에서 떠 보이던 부착형 파괴 연출은 보관 상태로 비활성화하고 새 이음새 낙하로 대체했다.
- 구조 대상은 전체 천장/층 그룹 22개에서 **독립적인 큰 조각 4개**로 변경했다. 높이 2.86→2.46→2.06→1.50m 순서다. 3/17/31/45초부터 각 10초씩, 모서리를 풀고 약 .24m 들어 올린 뒤 옆으로 옮긴다. 동시에 운반하는 조각은 최대 1개다.
- 주변 천장층은 제거하지 않는다. 좁은 추출 통로만 확보하고 마지막 조각 운반 완료 후 작업음을 멈추고 기존 구조 응답 소리를 재생한다. 60초에 완료하며 HMD/컨트롤러를 인위적으로 이동시키지 않는다.
- 구조 통로는 원본 파편의 면 단위 경계를 사용하고 가장자리에 유료 파편 12개를 덧대, 정렬된 다각형 절단선이 보이는 것을 줄였다. 통로 주변 메시 수정은 프로젝트 전용 사본이다.
- 여진 먼지 6개 시스템의 큰 구름 입자를 더 작고 얇게 만들고 미세한 이동을 부여했다. 떠 있는 불투명 덩어리처럼 보이던 부분을 줄였다. 미세 입자와 기존 여진/먼지 지속 시간은 유지했다.

## 변경 파일

- `Assets/_Project/Scenes/ConfinedPrototype.unity`: 내부 지지 구조, 파이프 경로, 파편 풀, 충돌면, 개별 구조 대상과 순서, 먼지, 차광 보강.
- `Assets/_Project/Scripts/AftershockPipeDrop.cs`: 시작부터 잔해 뒤에 존재하는 파이프.
- `Assets/_Project/Scripts/AftershockDebris.cs`: 실제 물리 낙하/착지와 풀 복구.
- `Assets/_Project/Scripts/RescueEndingDirector.cs`: 개별 운반 동작, 좁은 통로의 차광 계산, 마지막 운반 후 구조 응답.
- `Assets/_Project/Art/InterlockedRubble/` 및 메타 파일: 통로 주변 메시 사본과 확산형 먼지 재질. 초기 보정용 사본도 보존했다. 원본 유료 에셋은 변경하지 않았다.
- `Assets/_Project/Scenes/Backups/BeforeGroundedCollapse_20261106_154050.unity` 및 메타 파일: 수정 전 씬.
- `AgentScripts/InspectGroundedCollapse.cs`, `BuildInterlockedRubble.cs`, `RefineInterlockedSupports.cs`, `FinishGroundedArt.cs`, `NaturalizeShaftEdges.cs`, `VerifyGroundedCollapse.cs`: 검사, 배치, 보정, 검증 도구.
- `AgentScripts/AftershockPipeDrop.BeforeGrounded.cs.txt`, `AftershockDebris.BeforeGrounded.cs.txt`, `RescueEndingDirector.BeforeGrounded.cs.txt`: 수정 전 코드 사본.
- 이 보고서.

UI, 입력 바인딩, XR Origin, 게임의 신고/구조 조건은 변경하지 않았다. 기존의 비활성 장식/애니메이션 참조는 삭제하지 않고 보관한다.

## 검증

`Temp/GroundedCollapseReview/Verification.json`: **PASS**.

- 실제 PC 프레임 2,813개가 진행되는 동안 중력과 충돌은 일반 PhysX FixedUpdate로 검사했다. `Physics.Simulate`나 시간 배율 변경은 사용하지 않았다. 게임의 여진 시계는 1/30초씩 진행시켜 경로를 샘플링했다.
- 보이는 낙하 파편 28개 중 **28개 정지/수면 확인**. 강제로 공중 좌표에 고정하지 않고 동적 Rigidbody·중력을 유지한다. 풀 복구 후 다시 부착 상태로 전환됨.
- 파이프 경로의 1/30초 샘플 최대 이동 .1067m. 연속적인 높이 변화 1.72→1.54→1.16→.455m. 손이 닿는 착지 후 잡기 활성화.
- 구조 조각 4개: 바깥/높은 것부터 이동, 시간 구간당 운반 최대 1개, 전체 천장층 이동 **0개**, 완료와 복구 정상.
- 별도 `VerifyRescueEnding.Run`: 실제 119 연결, 신고 유지, 무전 기회, 잡은 파이프 신호 조건 및 실패 분기 **PASS**. 구조 대상 4개로 확인.
- 마지막 경계 보정 후 엔딩 0/10/22/34/46/60초 캡처 **PASS**. 흰색 화면 덮개 없음. HMD 위치/회전 변경 없음.
- 마지막 차광 검사: 휴대폰 자체 표시/조명을 제외하고 하늘을 켠 상태에서 여섯 방향 모두 mean=0, clipped=0. `Temp/SealedBurialReview/Darkness.json`.
- Unity 컴파일 errors=0, warnings=0. 최종 콘솔 오류 확인.

## 화면

- `Temp/GroundedCollapseReview/Pipe_05.0.png` ~ `Pipe_08.3.png`: 파이프 이탈과 착지. 형태를 확인하기 위한 임시 검사 조명이 있다.
- `Temp/GroundedCollapseReview/Aftershock_Settled.png`: 파편 착지와 옅어진 먼지. 임시 검사 조명.
- `Temp/SealedBurialReview/Rescue_60_Ceiling.png`: 최종 경계 보정 후 작은 구조 통로. 실제 구조 조명, 추가 검사 조명 없음.

검사용 카메라는 별도로 생성했다가 제거했고, 검수 조명은 씬에 저장하지 않았다. Play Mode 종료와 Run In Background 오버라이드 해제로 에디터 상태를 돌려 놓았다.

## 한계 및 이전 기록과의 차이

실제 헤드셋의 깊이감, 손 도달성, 성능은 미검증이다. 큰 잔해는 완전한 건물 구조 해석/물리 붕괴가 아니라 접촉을 맞춘 고정 배치이며 작은 파편만 동적으로 시뮬레이션한다. 구조대 인물/손 애니메이션이나 강제 카메라 이동을 추가하지 않았다. 마지막 조각 제거, 접근한 구조 응답음과 완료 이벤트로 추출을 표현한다.

이전 보고서의 벽체 파괴 6개/116개 좌표 파편/천장 22개 제거는 이번 요구로 대체되었다. 해당 이전 구조를 전제한 옛 빌더·수량 검증을 최신 검증 결과로 해석하지 않는다. 현재 확인 도구는 `VerifyGroundedCollapse`, `VerifyRescueEnding`, `ReviewSealedBurial`이다.

`unity-cli`와 `physics-3d-collision` 진단을 사용했다. 원인은 콜백 누락이 아니라 낮은 렌더러 노출 위치, 교체된 시각 메시와 어긋난 고정 착지점, 큰 그룹 단위의 제거였다. 관련 구현을 수정한 뒤 실제 PC 프레임에서 확인했다.
