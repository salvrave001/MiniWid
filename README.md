# MiniWid

Портативный виджет заряда для Windows: наушники, мышь, геймпад и другие аксессуары, у которых Windows или USB-донгл уже знает батарею.

Установщик не нужен. Скачайте архив из [Releases](https://github.com/salvrave001/MiniWid/releases), распакуйте и запустите `MiniWid.exe`.

## Что умеет

- Карточка без рамки поверх окон, трей, автозапуск
- Темы: ASUS ROG, ROG 20th Anniversary, Cyberpunk 2077, Windows светлая/тёмная
- Реальный заряд ASUS ROG Delta II и WLMouse Beast X с USB-донгла
- Sony WH-1000XM4 по Bluetooth, если Windows видит процент
- Manba One V2 в списке; в режиме Xbox 360 Windows не отдаёт настоящий заряд (показывает «—», а не фейковые 100%)

На этой машине нет системной батареи — строка «Этот компьютер» скрывается.

## Требования

- Windows 10 1809 или новее, x64
- .NET вшит в портативную сборку, отдельно ставить ничего не нужно

Настройки пишутся рядом с `MiniWid.exe` в `settings.json`.

## Сборка

```powershell
.\publish-portable.ps1
```

Готовая папка: `MiniWid-portable\`. Перед пересборкой закройте `MiniWid.exe`.
