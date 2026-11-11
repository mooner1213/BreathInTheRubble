# 휴대폰 빛으로 보이는 눈뜨기와 여진 먼지 증량 — 2026-11-04

처음에는 눈뜨기가 끝난 뒤 화면이 켜졌고, 휴대폰을 들지 않은 실제 시작 자세에서는 정면이 거의 검게 남았다. 화면을 시작부터 켜고, 그 빛에 비친 먼지가 눈꺼풀 틈에서 보이도록 수정했다.

- 화면은 시작 직후 켜져 0.7초에 걸쳐 밝아진다. 알림 소리는 기존 5초 시점을 유지한다. 손전등은 자동으로 켜지지 않는다.
- 눈뜨는 첫 4초 동안만 휴대폰 화면 빛을 넓혀 정면 연무에도 닿게 했다. 빛 상한은 0.08→0.4, 바깥 각도 120→150도이며 이후 2초 동안 평소의 0.08/120도로 복원한다. 여진 중에는 이 시작용 증폭이 남지 않는다. 휴대폰/그랩 위치나 카메라 자세를 바꾸지 않았다.
- 눈뜨기는 0.6초 암전 + 3.4초 개방, 총 4초다. 중앙 틈의 추가 암전이 일찍 걷혀 완전히 눈을 뜨기 전부터 푸른빛의 먼지가 보인다.
- 여진 미세 먼지는 180/120/36개에서 225/150/45개로 25% 늘렸다. 넓은 연무는 처음 22→28개, 주요 파손 때 18→22개다. 미세 먼지/연무 용량은 760/90으로 제한했다.
- 시작용 큰 연무는 고정 시드를 사용해, 첫 화면 중앙이 우연히 비는 정도가 재시작마다 크게 달라지지 않게 했다. 여진 입자들은 기존 방식으로 방출한다.
- 여진 18초, 먼지 20초 유지 + 10초 소멸, 기존 파편/벽 파손/철근 연출을 유지한다. 시작 입자 수는 346개다.

## 실제 시작 검증

이번에는 효과를 수동 재생하거나 휴대폰을 집지 않고, 새 Play Mode의 첫 5.5초를 자동 기록했다. 검사 전용 조명도 추가하지 않았다.

- 첫 0.1초: 휴대폰 화면 상태는 이미 켜짐, 눈꺼풀은 완전 암전(픽셀 최댓값 0).
- 1.5초: 눈꺼풀 개방 약 18%, 중앙에 푸른빛 먼지가 보임.
- 2.2초: 개방 약 47%, 더 넓은 영역의 연무/잔해가 드러남.
- 4.2초: 완전히 열린 상태. 시작용 빛이 평소 밝기로 서서히 돌아감.
- 일곱 캡처 모두 휴대폰을 집지 않았고, 손전등은 꺼져 있으며, 휴대폰은 원래 위치에 있었다.

실제 캡처/측정값: `Temp/PhoneLitAwakeningReview/Startup.json`.

- [처음 보이는 틈](../Temp/PhoneLitAwakeningReview/Startup_1.50s.png)
- [열리는 중](../Temp/PhoneLitAwakeningReview/Startup_2.20s.png)
- [완전히 열린 뒤](../Temp/PhoneLitAwakeningReview/Startup_4.20s.png)
- [증량한 여진 먼지](../Temp/PhoneLitAwakeningReview/Aftershock_LightOn.png)

## 변경 파일

- `Assets/_Project/Scripts/PhoneWakeSequence.cs`: 시작부터 화면을 켜는 선택 설정. 알림 지연/중복 깨우기 방지 유지.
- `Assets/_Project/Scripts/PhoneScreenSpillLight.cs`: 시작 때만 화면 빛의 세기/각도를 올렸다가 복원. 평소 빛과 가림 판정 유지.
- `Assets/_Project/Scripts/AwakeningReveal.cs`: 기본 개방 시간 3.4초.
- `Assets/_Project/Art/AwakeningEyelids.shader`: 열린 틈의 추가 암전 완화.
- `Assets/_Project/Scripts/AftershockDustEffect.cs`: 초기/파손 연무 방출량을 Inspector에서 조정.
- `Assets/_Project/Scenes/ConfinedPrototype.unity`: 시작 화면/주변 빛/개방 시간/여진 먼지 설정.
- `Assets/_Project/Editor/AwakeningStartupCapture.cs` 및 메타: 명시적으로 Arm했을 때만 다음 실제 시작을 기록하는 에디터 도구. 게임 빌드에는 포함되지 않는다.
- `AgentScripts/ConfigurePhoneLitAwakening.cs`, `VerifyPhoneLitAwakening.cs`: 구성/실제 시작 및 먼지 증량 검사.
- `AgentScripts/VerifyAwakeningReveal.cs`, `ReviewAwakeningAtmosphere.cs`: 개방 시간에 맞춘 검사/캡처.
- `Assets/_Project/Docs/ExperienceRoadmap.md`: 현재 시작과 먼지 설정 반영.

변경 전 장면: `Assets/_Project/Scenes/Backups/BeforePhoneLitAwakening_20261104_170059.unity`. 중간 조정 전 사본도 `BeforePhoneLitAwakening_20261104_170435.unity`로 보존했다.

## 결과

- Unity 컴파일 오류/경고 0, 렌더 캡처의 두 효과 셰이더 오류 없음.
- 실제 프레임 증가 확인 후 기록/검사했다.
- `VerifyPhoneLitAwakening`: PASS. 실제 시작 일곱 프레임, 중앙의 보이는 틈, 화면/알림 분리, 6초 뒤 평소 화면 빛 0.08/120도 복원, 중복 깨우기 방지, 여진 시작 225개/첫 충격 후 375개/초기 세 충격 후 675개, 용량 제한, 18초 종료 및 30초 먼지 정리. 최종 시작 캡처의 중앙 밝기는 1.5초 약 0.024/1, 4.2초 약 0.030/1이었다.
- `VerifyAwakeningReveal`: PASS. 실제 1,346프레임에서 4초 전환과 10회 복원, 머리/컨트롤러 위치와 휴대폰 손전등 상태 유지.
- `VerifySettlingDust`: PASS. 시작 0.9→0.45→0, 여진 1.9→0.95→0을 20/25/30초에서 확인.
- `VerifyAftershockAtmosphere`: PASS. 여진 파티클 용량 1,074개, 작은 파편 116개, 시작 입자 346개, 반복 생성 없이 재사용.
- `VerifyExtendedAftershock`: PASS. 여진 18초, 충격 6회, 후반 벽 파손/추가 파편, 10회 복원 유지.
- `ReviewAwakeningAtmosphere`: PASS. 손전등 켬/끔/조준 변경, 완전 암전, 입자 유무와 30초 정리 비교.
- 최종 Play Mode 종료 후 현재 Console 오류 0, 새 경고/오류 없음. 기존 경고 1개는 유지했다. 검증용 자동 틱을 끄고 에디터를 정지 상태로 돌렸다.

`unity:craft-game-floors`의 game-feel/VFX 지침을 적용했다. 실제 시작 캡처를 본 뒤 화면 빛의 각도와 눈꺼풀 중앙 암전을 수정했다. 이번 범위의 시작 전환 가독성은 2→4/5, 여진 먼지 반응은 4/5로 평가했다. 실제 헤드셋의 암부 표현/착용감/성능은 검사하지 않았다.
