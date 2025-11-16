VAR playerChoice = 0

=== function generateQuest ===
~ return "Торговец: Помоги мне найти артефакт в лесу!"
==

=== start ===
{generateQuest}
* [Согласиться] -> agree
* [Отказаться] -> refuse
-> END

=== agree ===
Торговец: Спасибо! Награда — 100 золота.
-> END

=== refuse ===
Торговец злится: Ты пожалеешь!
-> END