# 잔해 속의 숨 · Breath In The Rubble

붕괴 후 제한된 공간에 매몰된 상황에서 주변 물건과 상호작용하는 **1인 개발 VR 생존 프로젝트**입니다.

## 기획 방향

- 붕괴 후 시야가 회복되는 도입
- 파손된 스마트폰의 긴급전화와 플래시
- 손이 닿는 물통과의 상호작용
- 시간이 지나며 먼지가 가라앉는 공간 연출

위 항목은 프로젝트의 목표입니다. 현재 공개된 코드의 구현 완료 목록과 구분합니다.

## 현재 저장소

`main`은 VR 템플릿과 XR 샘플 및 초기 씬 구성 중심입니다. 현재 공개된 코드에서 위 핵심 플레이 기능의 구현 완료나 시연 완료를 확인할 수 있는 자료는 제공하지 않습니다.

## 개발 환경과 열기

- Unity **6000.3.22f1**
- Unity 기반 XR 프로젝트
- PC Editor와 VR 환경을 고려한 개발

Unity Hub에서 프로젝트를 추가한 뒤 패키지와 에셋 임포트를 완료합니다. 초기 씬은 [Assets/Scenes](Assets/Scenes)에서 확인할 수 있습니다. XR 템플릿 샘플의 기능은 프로젝트 자체 제작 기능과 구분합니다.

## 구성

| 경로 | 역할 |
| --- | --- |
| [Assets/Scenes](Assets/Scenes) | 초기 씬 |
| [Assets/VRTemplateAssets](Assets/VRTemplateAssets) | VR 템플릿 리소스 |
| [Assets/Samples](Assets/Samples) | XR 패키지 샘플 |
| [Packages](Packages) | 패키지 의존성 |
| [ProjectSettings](ProjectSettings) | 프로젝트 설정 |
