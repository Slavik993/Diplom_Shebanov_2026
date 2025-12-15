# Динамическая генерация игровых миров при помощи нейросетей

**Автор**: Шебанов Вячеслав  

**Стартап**: Инструментарий создания культурно-образовательных квестов    

**Тема**: Взаимодействие разных факультетов, для проведения исследований в области какие качества должен развивать педагог, чтобы работать с детьми с ОВЗ, как эти качества можно тренировать в виртуальном тренажёре, что из этого переносится на работу с остальными детьми для создания более здоровой среды в целом  

**Датасет**: Dungeons and Dragons Icons, Warcraft Icons, Запрещённые мемы 
**Модели нейронных сетей - используются**: Mistral-7B-Instruct, Awesome RPG Icon 2000
**Все модели нейронных сетей**: sd-turbo, sd3-medium, Lama 



## Результаты

**СДГВМ - "Система Динамической Генерации Виртуальных Миров"**: Проект в Unity служащий инструментарием, для генерации контента, использующегося в образовательных квестах и культурных визуальных новеллах 
**Готовый шаблон образовательного квеста в виде визуальной новеллы - является конструктором квестов**: Проект в Unity, позволяющий конструировать квесты 

## Структура проекта



**Проект СДГВМ для гнерации контента**: папка SDGVM

**Папка с основными доками и пятью приложениями**: Docs
**Диаграммы**: В папке Drawio

**Раняя версия СДГВМ**: В папке Diplom_2026_Dynamic_World_Generation
**OldInstructions**: Инструкции и описательные работы
**WeirdDocs**: Шаблоны отчётов
**HotDogs**: Датасеты, для обучения моделей
**README.md**

## Инструкция по запуску СДГВМ

Для работы языковых моделей обязательно убедиться что в компоненте LLM в инспекторе назначен Mistral-7B-Instruct v 0.2 и поставлена галочка, при запуске должны выведены сообщения о проведении необходимых действий, для запуска 
Обязательно запустить локальный сервер ComfyUI main.py находящийся по пути SDGVM\Assets\ComfyUI\ComfyUI, если на компютере нет видеокарты, то использовать терминал для запуска - команда "python main.py --cpu".   






IDEF0
<img width="1171" height="744" alt="IDEF0" src="https://github.com/user-attachments/assets/bde4f50b-45e7-4789-98b6-b67c15c4c38c" />

IDEF3
<img width="1145" height="502" alt="IDEF3" src="https://github.com/user-attachments/assets/e580d04b-8ba2-40a1-9145-863fe19ded1b" />

DFD(Йордана-Де-Марко)
<img width="1077" height="618" alt="DFD(Йордана-Де-Марко)" src="https://github.com/user-attachments/assets/4dbb6e37-461d-4277-b379-a9bc2e7a58ab" />

DFD(Гейна-Сарсона)
<img width="1005" height="718" alt="DFD(Гейна-Сарсона)" src="https://github.com/user-attachments/assets/62d49e17-d89c-410b-8bf4-3a98d9653c2e" />

EPC
<img width="545" height="676" alt="EPC" src="https://github.com/user-attachments/assets/c505759c-c0f0-4000-9a45-6f5870eef10c" />

As Is
<img width="808" height="775" alt="Is As" src="https://github.com/user-attachments/assets/77dcbb3b-b94b-40a5-929c-069e5aa6b95d" />

To Be
<img width="853" height="708" alt="To Be" src="https://github.com/user-attachments/assets/0d01fa0f-19e1-4773-a37d-d62788621726" />

Нотация BPMN
<img width="1298" height="220" alt="BPMN" src="https://github.com/user-attachments/assets/8ff4c759-02a9-4c99-9ca9-39b92ec04adb" />

Матрица Ранжирования
<img width="916" height="615" alt="Матрица ранжирования" src="https://github.com/user-attachments/assets/fa7eac19-2321-4afa-babe-b9a924318c70" />





