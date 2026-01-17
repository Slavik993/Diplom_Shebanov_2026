# 📋 ПОДРОБНАЯ ИНСТРУКЦИЯ ПО НАСТРОЙКЕ СКРИПТОВ

## 🎯 Что нужно сделать пошагово:

### 1. 🎮 GameManager Настройка

**Найдите объект GameManager и добавьте компонент GameManager:**
```
GameManager (GameObject)
├── GameManager (Script)
```

**Заполните поля в Inspector:**
- **isGameActive**: false
- **coins**: 0
- **distance**: 0
- **gameSpeed**: 5
- **totalCoins**: 1000
- **equippedJetPack**: 0
- **equippedCostume**: 0

**UI ссылки (пока оставьте пустыми, создадим UI позже):**
- **coinsText**: [пусто]
- **distanceText**: [пусто]
- **gameOverPanel**: [пусто]
- **mainMenuPanel**: [пусто]
- **gamePanel**: [пусто]
- **shopPanel**: [пусто]

---

### 2. 🔊 AudioSystem Настройка

**Создайте объект AudioSystem:**
```
AudioSystem (GameObject)
├── AudioSystem (Script)
├── Audio Source (Music Source)
├── Audio Source (SFX Source)
```

**Заполните поля:**
- **Music Source**: перетащите первый Audio Source
- **SFX Source**: перетащите второй Audio Source
- **Остальные поля**: оставьте пустыми (добавим звуки позже)

---

### 3. 🚀 PlayerController Настройка

**Создайте объект Player:**
```
Player (GameObject)
├── SpriteRenderer
├── Rigidbody2D
├── BoxCollider2D
├── PlayerController (Script)
└── JetPacks (GameObject)
    ├── JetPack_0 (GameObject) [активен]
    ├── JetPack_1 (GameObject) [неактивен]
    ├── JetPack_2 (GameObject) [неактивен]
    └── JetParticles (GameObject)
        └── ParticleSystem
```

**Заполните поля PlayerController:**
- **Fly Force**: 5
- **Max Velocity**: 10
- **Gravity**: -9.81
- **Current JetPack Index**: 0
- **Jet Packs**: [перетащите JetPack_0, JetPack_1, JetPack_2]
- **Jet Particle**: [перетащите JetParticles]

---

### 4. 🎯 JetPack Настройка (для каждого JetPack)

**Для каждого JetPack_X:**
```
JetPack_X (GameObject)
├── SpriteRenderer
├── JetPack (Script)
```

**JetPack_0 (Basic):**
- **JetPack Name**: "Basic JetPack"
- **Price**: 0
- **Fly Force**: 5
- **Description**: "Standard jetpack for beginners"
- **JetPack Color**: Gray

**JetPack_1 (Advanced):**
- **JetPack Name**: "Advanced JetPack"
- **Price**: 100
- **Fly Force**: 5.5
- **Description**: "Improved jetpack with better performance"
- **JetPack Color**: Red

**JetPack_2 (Pro):**
- **JetPack Name**: "Pro JetPack"
- **Price**: 200
- **Fly Force**: 6
- **Description**: "Professional jetpack with maximum power"
- **JetPack Color**: Cyan

---

### 5. 🌪️ Spawner Настройка

**Создайте объект Spawner:**
```
Spawner (GameObject)
├── Spawner (Script)
```

**Заполните поля:**
- **Spawn Interval**: 2
- **Min Spawn Y**: -3
- **Max Spawn Y**: 3
- **Spawn X**: 10
- **Difficulty Increase Rate**: 0.1
- **Min Spawn Interval**: 0.5
- **Obstacles**: [пусто, создадим префабы]
- **Coins**: [пусто, создадим префабы]

---

### 6. 🪙 Prefabs Создание

**Создайте префабы:**

**Coin Prefab:**
```
Coin (GameObject)
├── SpriteRenderer (желтый круг)
├── CircleCollider2D (IsTrigger = true)
├── Coin (Script)
└── Rigidbody2D (Kinematic)
```

**Obstacle Prefabs:**
```
StaticObstacle (GameObject)
├── SpriteRenderer (красный прямоугольник)
├── BoxCollider2D
├── Obstacle (Script)
└── Type: Static

MovingObstacle (GameObject)
├── SpriteRenderer (желтый прямоугольник)
├── BoxCollider2D
├── Obstacle (Script)
└── Type: Moving

RotatingObstacle (GameObject)
├── SpriteRenderer (фиолетовая линия)
├── BoxCollider2D
├── Obstacle (Script)
└── Type: Rotating
```

---

### 7. 🎨 Canvas и UI Настройка

**Создайте Canvas:**
```
Canvas (GameObject)
├── Canvas
├── CanvasScaler
├── GraphicRaycaster
├── EventSystem (отдельный объект)
└── UI Панели:
    ├── MainMenuUI (GameObject)
    ├── GameUI (GameObject)
    └── ShopUI (GameObject)
```

---

## 🚀 БЫСТРЫЙ СПОСОБ - Я МОГУ СДЕЛАТЬ ВСЕ САМ!

Если вы хотите, я могу создать готовую сцену со всеми настройками через код. Просто скажите:

**"Сделай сам все настройки"**

И я создам:
- ✅ Все объекты с правильными компонентами
- ✅ Все заполненные ссылки
- ✅ Все префабы
- ✅ Полностью готовую игру

---

## 🎯 Проверка настроек:

После настройки проверьте:
1. **Player** может летать (пробел/клик)
2. **GameManager** сохраняет монеты
3. **Spawner** создает препятствия
4. **UI** показывает интерфейс

---

## 💡 Советы:

- **Теги**: Установите тег "Player" для объекта Player
- **Слои**: Игровые объекты на слое "Default"
- **Физика**: Rigidbody2D в Kinematic для префабов
- **Коллайдеры**: IsTrigger для монет

---

## 🔧 Если что-то не работает:

1. **Проверьте ссылки** в Inspector
2. **Убедитесь что скрипты** не имеют ошибок
3. **Проверьте теги** и слои
4. **Посмотрите консоль** на ошибки

---

**Готов помочь с настройкой!** 🚀

Выберите вариант:
1. **"Я сам настрою"** - используйте эту инструкцию
2. **"Сделай сам"** - я создам все через код
