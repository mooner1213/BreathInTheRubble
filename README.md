# 잔해 속의 숨 · Breath In The Rubble

**지진으로 매몰된 공간에서 손이 닿는 물건을 활용해 상황을 파악하는 1인칭 VR 생존 시뮬레이션입니다.**

[컨셉 기획서 PPT](Assets/Docs/3조%20문재혁%20컨셉기획서.pptx) · [플레이 씬](Assets/_Project/Scenes/ConfinedPrototype.unity) · [상호작용 코드](Assets/_Project/Scripts) · [제작 기록](AgentScripts)

| 항목 | 내용 |
| --- | --- |
| 제작 | 문재혁 / 1인 개발 |
| 플레이 형태 | 앉은 시점의 VR. 이동·텔레포트 제한 |
| 개발 환경 | Unity 6000.3.22f1 · C# · OpenXR |
| 대상 기기 | 미정. PC Editor 시험 경로 제공 |
| 현재 단계 | 휴대폰과 매몰 공간의 핵심 상호작용 프로토타입 |

## 핵심 경험과 기능

어둡고 좁은 공간에서 휴대폰의 빛과 통신 상태를 확인하고 주변 물건을 직접 다루는 경험을 목표로 합니다.

| 기능 | 현재 구성 |
| --- | --- |
| 휴대폰 발견 | 지연 후 화면 점등·경고음과 화면 빛 변화 |
| 라이트 | 화면 버튼으로 후면 라이트 켜기·끄기 |
| 긴급전화 | SOS → 119 번호 화면 → 통화 시도와 실패·재시도·취소·연결 상태 |
| 수신과 배터리 | 시간에 따른 수신 변화와 라이트·통화 상태에 따른 배터리 소모 |
| 물통 | 밀봉 PET 물통 잡기와 회전 시 수평을 유지하는 액체 표현 |
| 매몰 공간 | 좁은 잔해·콘크리트·철근 디테일과 초기 먼지·부유 입자 |
| PC 시험 | 헤드셋 없이 휴대폰 잡기·회전·라이트·전화 입력 확인 |

긴급전화는 게임 내부의 연결 상태 시뮬레이션입니다.

## 기획 방향

| 선택 | 의도 |
| --- | --- |
| 움직일 수 없는 매몰 공간 | 손이 닿는 범위의 물건과 환경에 집중 |
| 라이트·긴급전화 중심의 휴대폰 UI | 익숙한 도구를 주변 탐색과 상황 판단에 연결 |
| 불안정한 수신과 통화 재시도 | 구조 요청의 불확실성과 긴장감 표현 |
| 먼지와 공간 디테일 | 매몰된 공간의 밀도와 시간 경과 전달 |

## 진행 상태

| 구분 | 범위 |
| --- | --- |
| 현재 프로토타입 | 라이트·긴급전화·배터리·물통 잡기와 액체 표현·매몰 공간 |
| 후속 제작 | 각성·여진·고립·구조의 통합 단계 제어와 구조대 대화·구조 연출 |
| 물통 범위 | 밀봉 상태의 잡기와 표현. 마시기·따르기는 현재 범위 밖 |
| 기기 검증 | 기존 보고서 기준 실제 HMD 시험과 성능 측정 미수행 |

수신 회복은 현재 임시 시간 기반 처리이며 추후 스토리 단계 이벤트와 연결할 수 있도록 통화 상태 로직과 분리되어 있습니다.

## 실행 방법

1. Unity Hub에서 프로젝트를 추가하고 **Unity 6000.3.22f1**로 엽니다.
2. 패키지 복원과 에셋 임포트를 기다립니다.
3. [ConfinedPrototype](Assets/_Project/Scenes/ConfinedPrototype.unity)을 엽니다.
4. Play Mode에서 매몰 공간과 휴대폰 프로토타입을 확인합니다.

빌드 설정에는 현재 `Assets/Scenes/SampleScene.unity`가 등록되어 있습니다. 프로토타입을 빌드할 때는 `ConfinedPrototype.unity`를 빌드 씬 목록에 지정하고 XR 설정을 확인해야 합니다.

<details>
<summary>PC Editor 시험 조작</summary>

Game 뷰에 포커스를 둔 상태로 사용합니다.

| 입력 | 동작 |
| --- | --- |
| E | 휴대폰 잡기·원래 위치로 돌려놓기 |
| F | 휴대폰을 잡고 라이트 켜기·끄기 |
| C | 휴대폰을 들고 긴급전화 화면 열기 |
| Enter | 번호 화면의 통화·종료 |
| 마우스 클릭 | 휴대폰 버튼과 번호 UI 조작 |
| 오른쪽 드래그 | 앉은 위치에서 둘러보기 |
| 왼쪽 드래그 | 들고 있는 휴대폰 회전 |
| 휠 / R | 휴대폰 거리 조절 / 회전·거리 초기화 |
| Q | 시점 방향 조준과 자유 회전 전환 |

Editor 메뉴 **BreathInTheRubble → PC Phone Test**에서도 시험 창을 열 수 있습니다. 이 입력은 Editor용입니다.

</details>

## 개발 자료

- [컨셉 기획서 PPT](Assets/Docs/3조%20문재혁%20컨셉기획서.pptx)
- [긴급전화와 수신 변화](AgentScripts/EmergencyCallGuide.md)
- [PC 입력 시험](AgentScripts/DesktopPhoneTestGuide.md)
- [물통과 매몰 공간](AgentScripts/ConfinedSpaceReport.md)
- [배터리 소모와 표시](AgentScripts/PhoneBatteryGuide.md)
- [프로젝트 작업 규칙](AGENTS.md)

기존 보고서에는 Unity 컴파일과 Editor·시뮬레이터 회귀 시험이 기록되어 있습니다. 가속된 시간·입자 검사는 실제 HMD 장시간 시험과 구분합니다.

<details>
<summary>주요 코드와 자료 구조</summary>

| 기능 | 구현 내용 | 주요 코드 |
| --- | --- | --- |
| 휴대폰 발견 연출 | 지연 후 화면 점등과 경고음. 화면 빛의 변화 | [PhoneWakeSequence](Assets/_Project/Scripts/PhoneWakeSequence.cs) |
| 라이트 | 후면 라이트 켜기·끄기와 휴대폰 화면 버튼 | [PhoneInterfaceController](Assets/_Project/Scripts/PhoneInterfaceController.cs) |
| 휴대폰 상호작용 | 접촉 기반 잡기와 토글 방식 잡기·제시 위치 처리 | [PhoneContactOnlyGrabFilter](Assets/_Project/Scripts/PhoneContactOnlyGrabFilter.cs) · [PhoneFocusPresenter](Assets/_Project/Scripts/PhoneFocusPresenter.cs) |
| 긴급전화 | SOS → 119가 입력된 번호 화면 → 통화 시도. 실패·재시도·취소·연결 상태 | [EmergencyCallSystem](Assets/_Project/Scripts/EmergencyCallSystem.cs) · [PhoneDialerView](Assets/_Project/Scripts/PhoneDialerView.cs) |
| 수신 변화 | 무신호에서 불안정한 수신을 거쳐 회복하는 임시 시간 기반 진행 | [PhoneReceptionProgression](Assets/_Project/Scripts/PhoneReceptionProgression.cs) |
| 배터리 | 라이트·통화 상태에 따른 소모와 잔량 표시 | [PhoneBattery](Assets/_Project/Scripts/PhoneBattery.cs) · [PhoneBatteryUsage](Assets/_Project/Scripts/PhoneBatteryUsage.cs) |
| 물통 | 잡을 수 있는 밀봉 PET 물통과 회전 시 수평을 유지하는 액체 표현 | [WaterBottle45.prefab](Assets/_Project/Prefabs/WaterBottle45.prefab) · [BottleLiquidVisual](Assets/_Project/Scripts/BottleLiquidVisual.cs) |
| 공간과 먼지 | 좁은 잔해 공간과 콘크리트·철근 디테일. 초기 먼지와 지속 부유 입자 | [공간 제작 기록](AgentScripts/ConfinedSpaceReport.md) |
| PC 시험 | 헤드셋 없이 휴대폰 잡기·회전·라이트·전화 입력 시험 | [DesktopPhoneTestDriver](Assets/_Project/Scripts/DesktopPhoneTestDriver.cs) |

휴대폰 프리팹에 라이트 UI와 통화 컴포넌트가 포함되고 물통 프리팹에 액체 표현 컴포넌트가 연결되어 있습니다. 긴급전화는 게임 내부 연결 상태의 시뮬레이션입니다.

## 코드와 자료 구조

| 경로 | 역할 |
| --- | --- |
| [Assets/_Project/Scenes](Assets/_Project/Scenes) | 현재 프로토타입과 변형·보존 씬 |
| [Assets/_Project/Prefabs](Assets/_Project/Prefabs) | 스마트폰과 물통 |
| [Assets/_Project/Scripts](Assets/_Project/Scripts) | 전화·라이트·배터리·잡기·액체 표현 |
| [Assets/_Project/Editor](Assets/_Project/Editor) | PC 시험 창 |
| [Assets/_Project/Art](Assets/_Project/Art) | 자체 프로젝트의 시각 리소스 |
| [AgentScripts](AgentScripts) | Editor 제작·설정·검증용 스크립트와 보고서 |
| [Assets/VRTemplateAssets](Assets/VRTemplateAssets) · [Assets/Samples](Assets/Samples) | 템플릿과 패키지 샘플 |

`Prototype.unity`는 이전 변형이며 `Scenes/Backups`는 제작 과정의 보존 씬입니다.

</details>

