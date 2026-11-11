# 여진 연장 및 후반 균열·철근 노출 — 2026-11-04

여진을 8초에서 18초로 늘렸다. 기존 초반 붕괴 뒤에 뒤쪽의 새로운 세 지점이 갈라지고,
콘크리트 조각이 내려앉으며 휘어진 철근이 드러난다.

## 동작

- 3.2/4.8/6.6초: 기존 충격과 천장 붕괴를 원래 타이밍대로 유지.
- 10.2/13.4/16.2초: 후속 충격, 세 곳의 새 균열/철근 노출, 국소 분진과 후반 파편 낙하.
- 18초: 여진과 진동음 종료, Isolation으로 전환.
- 뒤쪽 콘크리트 덮개 12개가 벌어지고 내려앉는다. 철근 6개는 굽은 구간과 돌출 리브가
  있으며 틈 안에서 앞으로 나오며 짧게 떨린다. 머리 근접 시 철근 표시를 제한한다.
- 후반 낙하 파편 24개 추가. 전체 풀은 92개에서 116개로 증가했다.
- 후반 분진 3곳을 추가했으며 반복 생성 없이 발생 후 20초 유지/10초 소멸 규칙을 따른다.
- 기존 천장은 첫 세 충격에서 최대 14cm 하강을 완료한다. 후속 충격이 생겨도 초반
  하강 속도를 느리게 나누지 않는다. 후반에는 새 콘크리트 조각이 추가로 내려앉는다.
- 공간 진동은 18초 동안 강약을 두고 이어지며 마지막 1.8초에 잦아든다.
- 구조 진동음도 18초 길이로 맞췄다. 반복 재생 시 같은 클립을 재사용한다.
- 새 균열/철근은 여진이 끝난 뒤에도 남고 미리보기 재시작/복원 시 초기 상태로 돌아간다.
- 새 구조물/파편에는 콜라이더를 추가하지 않았다. 물병 접근 공간을 확인했다.

## 수정/추가 파일

수정:

- Assets/_Project/Scripts/AftershockDirector.cs — 긴 여진, 여섯 충격, 후반 균열 제어와 복원.
- Assets/_Project/Scripts/AftershockEnvironmentMotion.cs — 초기 천장 붕괴와 전체 진동의 타이밍 분리.
- Assets/_Project/Scripts/AftershockAudio.cs — 여진 길이에 맞는 진동음과 클립 재사용.
- Assets/_Project/Scenes/ConfinedPrototype.unity — 새 후반 구조물/철근/파편/분진과 18초 설정 연결.
- Assets/_Project/Docs/ExperienceRoadmap.md — 현재 시간/파편 수/후반 연출 반영.
- AgentScripts/VerifyAftershockFoundation.cs
- AgentScripts/VerifyAftershockAtmosphere.cs
- AgentScripts/VerifyRearAftershockCollapse.cs
- AgentScripts/VerifySettlingDust.cs

추가:

- Assets/_Project/Scripts/AftershockFractureSection.cs 및 메타 파일 — 작은 재사용 가능한 시각 연출 컴포넌트.
- AgentScripts/ExtendAftershockSequence.cs — 씬 구성 및 균열 형태 조정.
- AgentScripts/ExtendedAftershockReview.cs — 백업과 시간별/근접 화면 확인.
- AgentScripts/VerifyExtendedAftershock.cs — 연장된 시퀀스와 철근 노출 검증.
- AgentScripts/ExtendedAftershockReport.md — 이 보고서.
- Assets/_Project/Scenes/Backups/BeforeExtendedAftershock_20261104_144310.unity 및 메타 파일.

기존 코드 복사본은 Temp/BeforeExtendedAftershock_20261104_144140/에 보존했다.
기존 콘크리트/균열/녹슨 철근 재질과 메시를 재사용했다. 새 패키지나 기기 전용 SDK는 추가하지 않았다.

## 검증 결과

- Unity 6000.3.22f1 컴파일 오류/컴파일 경고 0.
- job wait로 실제 frameCount 증가 확인.
- VerifyExtendedAftershock: PASS.
  - 8초가 지나도 Aftershock 유지, 18초에 Isolation으로 전환.
  - 여섯 차례 보호 판정, 세 후반 균열의 순차 열림/철근 노출 확인.
  - 첫 후반 파편 8개, 전체 후반 파편 24개 활성 확인.
  - 기존 초기 천장 하강 완료 시점 보존.
  - 새 구조물이 물병 주변 확장 경계와 겹치지 않고 물병 위치/그랩 상태 유지.
  - 18초 진동음 클립/재생 상태와 종료 확인. 실제 청취 평가는 하지 않았다.
  - 먼지 25초 절반 페이드/30초 정리 확인.
  - 10회 반복 시 동일 파편/클립/파티클 시스템 재사용, 균열/철근 초기화 확인.
- VerifyAftershockFoundation: PASS. 자동 75초 시작, 여섯 보호/피격, 단계 전환/복원 및 XR Origin 1개 확인.
- VerifySettlingDust: PASS. 시작/여진 모두 20/25/30초 페이드 및 유한 방출 확인.
- VerifyRoomTremor: PASS. 16개 구조물의 공통 움직임과 PC 진동/복원, XR 시뮬레이션 추적 유지 확인.
  실제 435개 실행 프레임 구간에서 PC 회전 약 0.69도, 위치 약 2.93mm, 앞쪽 잔해 이동 약 12.07mm 측정.
- VerifyAftershockAtmosphere: PASS. 전체 풀 116개, 여진 파티클 용량 896개, 재사용과 정리 확인.
- VerifyRearAftershockCollapse: PASS. 기존 뒤쪽 판 3개/파편 28개/분진 2곳 유지, 기존 판과 물병 상부 최소 여유 약 25cm.
- 최종 실행 및 종료 후 새 오류 0, 현재 Console 오류 0. 기존 경고/과거 로그는 삭제하지 않았다.
- 편집 모드로 돌아왔으며 테스트용 자동 갱신을 종료했다.

## 시각 확인과 남은 사항

craft-game-floors의 game-feel, vfx, game-audio, visual-scorecard 지침을 이번 연출 범위에 적용했다.
첫 캡처에서 균열선이 굵어 보이는 부분을 가늘게 다듬고 최종 캡처를 다시 확인했다.

- feedback visibility: 2/5 → 4/5 — 후반에 새 부위가 벌어지고 철근이 드러나며 파편이 떨어지는 장면 확인.
- style uniformity: 3/5 — 기존 콘크리트/녹슨 철근 재질을 그대로 사용.
- palette discipline: 3/5 — 기존 콘크리트 회색과 녹슨 철근의 갈색 유지.

Temp/ExtendedAftershockReview/에 8.2/11.1/14.2/17.2/31초 뒤쪽 화면과 철근 근접 화면을 저장했다.
어두운 구조 확인용 임시 검사 카메라/조명을 사용했으며 검사 후 제거했다. 실제 HMD 카메라를
검사용으로 이동하지 않았다. 캡처는 형상 확인 자료이며 실제 헤드셋 성능이나 착용감 검증은 아니다.
실기기/헤드셋 테스트와 실제 손으로 물병을 집는 동작은 수행하지 않았다.

확인한 Unity 6000.3 공식 문서:
[AudioClip.Create](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/audioclip/create),
[AudioSource.PlayOneShot](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/audiosource/playoneshot),
[ParticleSystem.Emit](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/particlesystem/emit).
