# Guard Proxy — roadmap

**Версия:** 1.9

## Правила

- План разделён на три версии: MVP 1.0, версия 2.0 и версия 3.0.
- Используется сквозная нумерация с шагом `10`: `0`, `10`, `20`, `30`...
- Для вставки шага между существующими допускаются свободные номера между ними, например `55`.
- `[ ]` — шаг не выполнен.
- `[+]` — шаг выполнен.
- `[-]` — шаг пропущен.
- Нумерация шагов сквозная между всеми версиями и не сбрасывается при переходе к следующей группе.
- Шаг переводится в `[+]` только после выполнения реализации и относящихся к нему автоматических проверок.
- После изменения `requirements.md` roadmap должен быть проверен на соответствие требованиям.

## MVP 1.0

Базовый HTTP proxy-предохранитель: прозрачное проксирование, лимит параллельности, hard timeout, Active Health Check, простое восстановление, ограниченные логи и Windows Service.

[+] **0.** Создать проект Guard Proxy на .NET 10 (`net10.0`), подключить YARP, добавить консольный запуск, Windows Service hosting и проекты unit/integration tests.

[+] **10.** Реализовать минимальную модель `config.json`, загрузку конфигурации, startup validation и подготовить MVP-версию `config-example.json`; покрыть validation unit-тестами.

[+] **20.** Реализовать HTTP YARP route от настраиваемого listen URL к одному настраиваемому upstream URL с прозрачной передачей method, path, query, body и headers.

[+] **30.** Добавить минимальные интеграционные тесты прозрачного проксирования: успешный HTTP/SOAP-запрос, Basic Auth passthrough и SOAP Fault passthrough.

[+] **40.** Реализовать ограничение параллельности через `maxConcurrentRequests`; в MVP очередь не поддерживается и запрос сверх лимита всегда должен немедленно получать `503` и не достигать upstream (эквивалент фиксированного `queueLimit = 0`).

[ ] **50.** Реализовать hard timeout рабочего запроса через `requestTimeoutMs`; интеграционно проверить медленный и зависший upstream.

[ ] **55.** Реализовать абстракцию времени для health/recovery logic, чтобы unit-тесты interval, timeout-related state logic и переходов состояний выполнялись без реального ожидания.

[ ] **60.** Реализовать один Active Health Check с параметрами `enabled`, method, URL, interval и timeout; при `enabled = false` Active Health Check не запускается.

[ ] **70.** Реализовать проверку Health Check response по JSON field/value с точным сравнением ожидаемого значения; покрыть валидатор unit-тестами.

[ ] **80.** Реализовать состояния `Unknown/Healthy/Unhealthy` и `failureThreshold`; покрыть переходы состояний unit-тестами.

[ ] **90.** Связать Active Health Check с состоянием upstream: при `Unhealthy` рабочие запросы должны получать быстрый `503` без обращения к upstream.

[ ] **100.** Реализовать простое восстановление: upstream остаётся `Unhealthy` во время recovery и возвращается в `Healthy` после одного успешного Health Check (`successThreshold = 1`, не настраивается в MVP); не допускать наложения health/recovery-циклов; добавить unit/integration tests.

[ ] **110.** Реализовать минимальное структурированное логирование ошибок, Health Check и изменений состояния upstream; проверить, что рабочие request/response body и `Authorization` не записываются в лог. Для structured file logging с ротацией использовать Serilog (Serilog.Sinks.File).

[ ] **120.** Реализовать простую ограниченную ротацию логов по `maxFileSizeMb` и `maxFiles`.

[ ] **130.** Подготовить self-contained `win-x64` publish и обеспечить установку, запуск, остановку и удаление Windows Service.

[ ] **140.** Провести полный интеграционный прогон MVP: normal traffic, Basic Auth passthrough, overload, timeout, health failure, `Unhealthy → 503` и автоматическое восстановление.

[ ] **150.** Проверить MVP-версию `config-example.json` автоматическим десериализационным/validation-тестом и выполнить финальную сверку реализации MVP 1.0.

## Версия 2.0

Расширенная устойчивость и универсальность: очередь, дополнительные timeout, гибкие probes/recovery, passive monitoring, health endpoints и расширенная диагностика.

[ ] **160.** Заменить фиксированное MVP-поведение без очереди на настраиваемый `queueLimit` и поддержку очереди запросов; проверить `queueLimit = 0` и `queueLimit > 0`.

[ ] **170.** Добавить `activityTimeoutMs` отдельно от полного `requestTimeoutMs`; интеграционно проверить отсутствие сетевой активности upstream.

[ ] **180.** Расширить Active Health Check: настраиваемые request headers, request body и список допустимых HTTP status codes.

[ ] **190.** Расширить response validation Health Check до универсальной настраиваемой политики, включая возможность отключения проверки body.

[ ] **200.** Реализовать отдельный конфигурируемый main recovery probe с независимыми method, URL, headers, body, timeout, status и response validation.

[ ] **210.** Реализовать `recovery.mode = healthcheck`.

[ ] **220.** Реализовать `recovery.mode = main`.

[ ] **230.** Реализовать `recovery.mode = parallel` с параллельным запуском Health Check и main probe внутри одного recovery-цикла; не допускать наложения recovery-циклов.

[ ] **240.** Реализовать `parallelSuccessPolicy = all/any` и настраиваемый `recovery.successThreshold`; покрыть комбинации unit-тестами.

[ ] **250.** Интеграционно проверить восстановление `Unhealthy → Healthy` для `healthcheck`, `main`, `parallel/all` и `parallel/any`.

[ ] **260.** Реализовать passive monitoring только transport-level failures с параметрами enabled, window, minimumRequests и failureRate; покрыть расчёт порога unit-тестами.

[ ] **270.** Проверить, что HTTP status, SOAP Fault и содержимое HTTP response сами по себе не влияют на passive monitoring.

[ ] **280.** Реализовать `/health/live` и `/health/ready`; исключить их из proxy route и concurrency limiter; добавить интеграционные тесты.

[ ] **290.** Расширить логирование рабочих запросов и probes: request ID, method, path, duration, HTTP status, текущее состояние upstream и диагностические причины.

[ ] **300.** Добавить фиксированные диагностические причины: `CONCURRENCY_LIMIT`, `REQUEST_TIMEOUT`, `CONNECT_FAILURE`, `NO_HEALTHY_DESTINATION`, `HEALTH_CHECK_TIMEOUT`, `HEALTH_CHECK_HTTP_ERROR`, `HEALTH_CHECK_INVALID_BODY`, `RECOVERY_CHECK_FAILED`.

[ ] **310.** Расширить ротацию логов параметром `maxTotalSizeMb` и полной retention policy; покрыть retention policy unit-тестами.

[ ] **320.** Расширить startup validation параметрами `passiveHealth`, `logging.level`, ограничениями размера/количества логов и взаимными проверками recovery-конфигурации.

[ ] **330.** Расширить `config-example.json` секциями очереди, activity timeout, recovery, passive monitoring и расширенного логирования; проверить его автоматическим десериализационным/validation-тестом.

[ ] **340.** Провести расширенный интеграционный прогон v2.0 и нагрузочный тест для определения производственного `maxConcurrentRequests`.

[ ] **350.** Выполнить финальную сверку реализации v2.0 с требованиями расширенной устойчивости.

## Версия 3.0

HTTPS и hardening: TLS для входящих и upstream-соединений, сертификаты, расширенная валидация и эксплуатационные проверки.

[ ] **360.** Реализовать входящий HTTPS endpoint через Kestrel.

[ ] **370.** Реализовать загрузку серверного TLS-сертификата из Windows Certificate Store.

[ ] **380.** Реализовать загрузку серверного TLS-сертификата из PFX-файла.

[ ] **390.** Реализовать validation TLS-конфигурации и проверку наличия private key.

[ ] **400.** Реализовать поддержку HTTPS upstream URL, Health Check URL и Recovery Probe URL со стандартной проверкой TLS-сертификата средствами .NET/операционной системы.

[ ] **410.** Добавить интеграционные тесты входящего HTTPS и HTTPS upstream/probes.

[ ] **420.** Расширить startup validation полным набором взаимных проверок конфигурации.

[ ] **430.** Расширить `config-example.json` TLS-конфигурацией для Certificate Store и PFX; проверить его автоматическим десериализационным/validation-тестом.

[ ] **440.** Провести длительный эксплуатационный прогон: HTTPS, ротация логов, health/recovery, перегрузка и восстановление после отказа.

[ ] **450.** Выполнить финальную сверку реализации, тестов и `config-example.json` с полным `requirements.md`.
