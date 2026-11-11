# 물병 뒤쪽 붕괴 보강 — 2026-11-04

## 변경 결과

- 뒤쪽 천장에 깨져 내려오는 조각 판 3개 추가. 기존 세 차례 충격에 맞춰 꺾이며
  최대 약 14cm 내려온다. 기존 뒤쪽 천장의 콘크리트 재질을 재사용했다.
- 물병 양옆/뒤쪽에 떨어지는 파편 28개 추가. 전체 풀은 64개에서 92개가 됐다.
  3.12/4.72/6.52초 부근에 나눠 떨어지며 런타임 생성/파괴 없이 재사용한다.
- 뒤쪽 균열 분진 지점 2곳 추가. 기존의 20초 유지/10초 페이드, 30초 정리 규칙을 따른다.
- 물병 주변을 비우도록 새 파편의 수평 경로를 물병 중심에서 24cm 이상 떨어뜨렸다.
  최종 붕괴 위치에서도 새 천장 판과 물병 윗부분 사이에 약 25cm가 남는다.
- 새 천장/파편에는 콜라이더나 리짓바디를 추가하지 않았다. 기존 물병 위치와
  충돌체, 그랩 설정 및 기존 앞쪽 여진 연출은 유지했다.
- 종료 검사에서 발견한 먼지 정리 참조 오류도 수정했다. 씬 해제 시 파티클/렌더러가
  먼저 사라져도 End, 페이드, Clear와 OnDisable이 안전하게 처리한다.

## 수정 및 추가 파일

수정:

- Assets/_Project/Scenes/ConfinedPrototype.unity — 뒤쪽 조각 판/파편/분진과 기존 제어 배열 연결.
- Assets/_Project/Scripts/AftershockDustEffect.cs — 이미 파괴된 파티클/렌더러 참조 처리.
- Assets/_Project/Docs/ExperienceRoadmap.md — 현재 파편/분진 수와 뒤쪽 붕괴 상태 반영.
- AgentScripts/VerifyAftershockAtmosphere.cs — 기존 파편 보존 검사를 추가 풀도 허용하도록 변경.

추가:

- AgentScripts/BuildRearAftershockCollapse.cs
- AgentScripts/RearCollapseReview.cs
- AgentScripts/VerifyRearAftershockCollapse.cs
- AgentScripts/RearCollapseReport.md
- Assets/_Project/Scenes/Backups/BeforeRearCollapse_20261104_140351.unity 및 메타 파일

먼지 정리 수정 전 코드 복사본: Temp/BeforeRearCollapseDustCleanup.cs.
미저장 씬을 포함해 백업했다. 기존 메시/재질을 재사용했으며 새 패키지, SDK,
XR Origin, 입력 관리자나 프리팹은 추가하지 않았다.

## 검증 결과

- Unity 6000.3.22f1 컴파일: 오류 0, 컴파일 경고 0.
- Play Mode frameCount 증가 확인 후 뒤쪽을 향한 임시 검사 카메라로 전후 캡처.
  이 카메라는 실제 HMD 카메라를 움직이지 않는다. 어두운 구조 확인을 위해
  검사 중에만 임시 조명을 사용했고 검사 후 카메라와 함께 제거했다.
- VerifyRearAftershockCollapse: PASS.
  - 뒤쪽 천장 판 3개 하강/꺾임, 새 뒤쪽 파편 28개 모두 활성 확인.
  - 뒤쪽 분진 2곳이 세 차례 충격에 방출되고 30초에 정리되는지 확인.
  - 물병 상부 최소 여유 0.2499m, 물병 위치/회전/충돌체/그랩 설정 유지 확인.
  - 10회 재시작해도 같은 파편을 재사용하고 판과 파편이 복원되는지 확인.
- VerifyRearAftershockCollapse.CheckTeardownSafety: PASS.
  - 임시 파티클/렌더러를 먼저 제거한 뒤 End/Advance/Clear 반복과 컴포넌트 해제를 재현했다.
  - MissingReferenceException 없이 fog가 원래 값으로 복원됐다.
- VerifyAftershockFoundation: PASS. 기존 보호/피격, 단계 전환, 자동 시작, 복원 유지.
- VerifyAftershockAtmosphere: PASS. 전체 풀 92개, 활성 91개, 여진 파티클 용량 824개,
  10회 재사용과 분진 정리 확인.
- 수정 후 실행과 Play Mode 종료까지 새 오류 0. 현재 Console 오류 0.
  이전 참조 오류의 로그 이력은 삭제하지 않았다. 기존 OpenXR/시뮬레이터 경고는 남아 있다.
- 검증 후 편집 모드로 복귀했고 테스트용 자동 갱신을 종료했다.

## 시각 확인 및 남은 사항

craft-game-floors의 game-feel, vfx, visual-scorecard 지침을 뒤쪽 붕괴 범위에 적용했다.
첫 캡처에서 새 판의 재질 차이를 확인하고 기존 뒤쪽 천장 재질로 맞춘 후 다시 캡처했다.
Unity 6000.3의 ParticleSystem.Emit/ParticleSystemRenderer 문서는 앞선 동일 연출 작업에서 확인한 내용을 따랐다.

- feedback visibility: 2/5 → 4/5 — 뒤쪽 천장 조각이 낮아지고 파편이 쏟아지는 장면 확인.
- style uniformity: 3/5 — 기존 각진 콘크리트 메시와 천장 재질을 재사용.
- palette discipline: 3/5 — 기존 콘크리트 색과 분진 색 유지.

캡처:

- Temp/RearCollapseReview/Before_Rest.png
- Temp/RearCollapseReview/Before_Collapse.png
- Temp/RearCollapseReview/After_Rest.png
- Temp/RearCollapseReview/After_Collapse.png

실제 헤드셋, 실기기 성능이나 실제 손으로 물병을 집는 동작은 테스트하지 않았다.
물병 접근 공간과 상호작용 설정 보존은 PC 씬/실행 검증 결과다.
