# Автозапуск и ежедневное обновление

Calendar обновляет рабочий стол каждый день. Есть три механизма, работающих вместе.

## 1. Авторан при входе в систему (встроенный)

В разделе **«Общие»** приложения — чекбокс **«Запускать вместе с системой»**. При включении:

- **Windows:** добавляется значение `DesktopCalendar` в ключ реестра
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` со значением `"<путь к app.exe>" -auto`.
  Без прав администратора (per-user).
- **Linux (GNOME):** создаётся файл `~/.config/autostart/desktopcalendar.desktop`
  (`Type=Application`, `Exec=<путь> -auto`, `X-GNOME-Autostart-enabled=true`).

При следующем входе в систему приложение запустится с ключом `-auto` и обновит календарь.
Снятие галочки удаляет запись.

> **Важно:** авторан срабатывает **только при входе в систему**. Если машина не
> перезагружалась и пользователь не выходил из сессии, календарь за новый день
> обновится либо при открытии GUI (см. п. 2), либо через системный планировщик (см. п. 3).

## 2. Встроенный таймер в GUI

Пока окно приложения открыто, раз в 30 минут проверяется смена дня. Если наступил
новый день и целевой монитор выбран — календарь автоматически перерисовывается.
Это покрывает сценарий «компьютер работает без перезагрузки, GUI открыто».

## 3. Системный планировщик (для полного покрытия)

Чтобы обновлять календарь каждый день независимо от входов/перезагрузок и открытого GUI,
настройте системный планировщик на запуск `app -auto`.

### Linux (cron)

```sh
# Ежедневно в 06:00 от вашего пользователя:
crontab -e
# Добавить строку:
0 6 * * * /путь/к/DesktopCalendar.App -auto
```

### Linux (systemd user timer)

```ini
# ~/.config/systemd/user/desktopcalendar.service
[Unit]
Description=Desktop Calendar daily refresh

[Service]
ExecStart=/путь/к/DesktopCalendar.App -auto
Type=oneshot
```

```ini
# ~/.config/systemd/user/desktopcalendar.timer
[Unit]
Description=Run Desktop Calendar daily

[Timer]
OnCalendar=*-*-* 06:00:00
Persistent=true

[Install]
WantedBy=timers.target
```

```sh
systemctl --user daemon-reload
systemctl --user enable --now desktopcalendar.timer
```

### Windows (Task Scheduler)

Через GUI Планировщика задач или команду:

```powershell
$action = New-ScheduledTaskAction -Execute "C:\путь\к\DesktopCalendar.App.exe" -Argument "-auto"
$trigger = New-ScheduledTaskTrigger -Daily -At 6am
Register-ScheduledTask -TaskName "DesktopCalendar" -Action $action -Trigger $trigger
```

## Идемпотентность `-auto`

Команда `-auto` **идемпотентна**: если календарь уже применён сегодня (по дате UTC) и
текущие обои совпадают с последним сгенерированным файлом — команда ничего не делает и
выходит с кодом 0. Это безопасно вызывать сколько угодно раз в день. Коды возврата:

| Код | Значение |
|---|---|
| 0 | Успех (или no-op: сегодня уже применено) |
| 1 | Ошибка |
| 2 | Не выбран целевой монитор |
| 3 | Выбранный монитор недоступен (отключён) |

## Путь к приложению

`Environment.ProcessPath` возвращает путь к исполняемому файлу. При разработке через
`dotnet run` это путь к `dotnet`-хосту — авторан в этом случае указывает не туда.
**Авторан работает корректно только после публикации** (`dotnet publish`) или установки.

## Логи

Лог silent-режима пишется в файл:
- Windows: `%LOCALAPPDATA%\DesktopCalendar\app.log`
- Linux: `~/.local/share/DesktopCalendar/app.log`
