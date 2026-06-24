# IRellyTrid Coding Convention

> 숭실대학교 겜마루 2026 여름공모전
>
> Project: IRellyTrid
>
> Language: C# (Unity)

---

## 1. 목적

본 문서는 IRellyTrid 프로젝트의 코드 품질 향상 및 협업 효율 증대를 위해 작성되었다.

모든 팀원은 본 코딩 컨벤션을 준수하여 코드의 가독성, 유지보수성, 일관성을 확보해야 한다.

---

## 2. 기본 원칙

* Microsoft C# Coding Convention을 따른다.
* 코드보다 명확한 이름을 우선한다.
* 매직 넘버(Magic Number)를 지양한다.
* 함수는 하나의 책임만 가진다.
* 중복 코드를 최소화한다.
* 주석보다 코드 자체가 읽히도록 작성한다.

---

## 3. 네이밍 규칙

### 클래스

PascalCase 사용

```csharp
public class PlayerController
public class InventoryManager
```

### 인터페이스

I 접두사 + PascalCase 사용

```csharp
public interface IDamageable
public interface IInteractable
```

### 메서드

PascalCase 사용

```csharp
public void Move()
public void TakeDamage()
```

### 변수

camelCase 사용

```csharp
private int health;
private float moveSpeed;
```

### 매개변수

camelCase 사용

```csharp
public void Move(Vector3 direction)
```

### 프로퍼티

PascalCase 사용

```csharp
public int Health { get; private set; }
```

### 상수

PascalCase 사용

```csharp
private const int MaxHealth = 100;
private const float GravityScale = 9.8f;
```

### Enum

PascalCase 사용

```csharp
public enum PlayerState
{
    Idle,
    Move,
    Attack,
    Dead
}
```

---

## 4. 접근 제한자

항상 명시적으로 작성한다.

### Good

```csharp
private int health;
public void Move()
```

### Bad

```csharp
int health;
void Move()
```

---

## 5. 변수 선언

사용 직전에 선언한다.

### Good

```csharp
int damage = CalculateDamage();
ApplyDamage(damage);
```

### Bad

```csharp
int damage;

...

damage = CalculateDamage();
```

---

## 6. var 사용 규칙

타입이 명확할 때만 사용한다.

### Good

```csharp
var player = GetComponent<PlayerController>();
var enemies = new List<Enemy>();
```

### Bad

```csharp
var value = GetSomething();
```

---

## 7. 중괄호 규칙

항상 중괄호를 사용한다.

### Good

```csharp
if (isDead)
{
    return;
}
```

### Bad

```csharp
if (isDead)
    return;
```

## 8. Unity 스크립트 구성 순서

```csharp
public class PlayerController : MonoBehaviour
{
    #region Constants
    #endregion

    #region Fields
    #endregion

    #region Properties
    #endregion

    #region Unity Methods

    private void Awake()
    {
    }

    private void Start()
    {
    }

    private void Update()
    {
    }

    #endregion

    #region Public Methods
    #endregion

    #region Private Methods
    #endregion
}
```

---

## 9. SerializeField 사용

Inspector 노출이 필요한 경우 private + SerializeField 사용

### Good

```csharp
[SerializeField]
private float moveSpeed;
```

### Bad

```csharp
public float moveSpeed;
```

---

## 10. Null 검사

Null 가능성이 있는 객체는 반드시 검사한다.

```csharp
if (target == null)
{
    return;
}
```

## 12. 로그 출력

디버그 로그는 목적을 명확히 작성한다.

### Good

```csharp
Debug.Log($"Player HP : {currentHp}");
```

### Bad

```csharp
Debug.Log("Test");
```

---

## 13. Git Commit Convention

### Feature

```text
feat: 플레이어 이동 구현
```

### Fix

```text
fix: 점프 중 충돌 버그 수정
```

### Refactor

```text
refactor: 상태머신 구조 개선
```

### Docs

```text
docs: 코딩 컨벤션 문서 추가
```

### Chore

```text
chore: 패키지 버전 업데이트
```

---

## 14. 브랜치 규칙

```text
main
develop

feature/player-movement
feature/inventory

fix/jump-bug
```

---

## 15. 최종 규칙

* 컴파일 경고 없이 커밋한다.
* 사용하지 않는 using 제거
* 사용하지 않는 변수 제거
* TODO는 담당자와 일정 명시
* Pull Request 후 코드 리뷰를 거쳐 병합한다.

---

Version 1.3

IRellyTrid Development Team
