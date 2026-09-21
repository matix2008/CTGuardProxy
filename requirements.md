# Guard Proxy — требования

**Версия:** 1.4

## 1. Технологическая платформа

1. Guard Proxy должен быть реализован на .NET 10 LTS.
2. Target Framework проекта: `net10.0`.
3. Для reverse proxy должен использоваться YARP.
4. Production-сборка должна публиковаться как self-contained `win-x64`.
5. Guard Proxy должен работать как отдельный процесс и не зависеть от технологического стека или версии runtime upstream-сервиса.
6. Взаимодействие Guard Proxy с upstream-сервисом должно выполняться по HTTP или HTTPS.
7. SOAP должен поддерживаться прозрачно как HTTP payload без зависимости от реализации SOAP-сервиса.
8. Для production-развёртывания self-contained сборки не должна требоваться отдельная установка .NET Runtime.

## 2. Назначение

Guard Proxy — универсальный Windows Service, устанавливаемый между клиентским приложением и upstream HTTP/SOAP-сервисом.

Guard Proxy должен:
- прозрачно проксировать рабочие HTTP/SOAP-запросы;
- ограничивать количество одновременно выполняющихся запросов;
- ограничивать время выполнения зависших запросов;
- контролировать работоспособность upstream-сервиса;
- прекращать передачу рабочих запросов при признании upstream-сервиса недоступным;
- автоматически восстанавливать передачу запросов по настраиваемым правилам;
- поддерживать active и passive health monitoring;
- вести циклические логи с настраиваемой ёмкостью.

Guard Proxy не должен содержать бизнес-логику конкретного upstream-сервиса.

## 3. Маршрутизация

1. Адрес, на котором Guard Proxy принимает рабочие запросы, должен задаваться в `config.json`.
2. Адрес upstream-сервиса должен задаваться в `config.json`.
3. Для изменения адреса Guard Proxy или upstream-сервиса не должна требоваться перекомпиляция.
4. Guard Proxy должен прозрачно передавать:
   - HTTP method;
   - path;
   - query string;
   - request body;
   - `Authorization`;
   - `Content-Type`;
   - `SOAPAction`, если он присутствует;
   - остальные необходимые HTTP headers.
5. Ответ upstream-сервиса должен возвращаться клиенту без изменения, если ответ не сформирован самим Guard Proxy.
6. HTTP-ответы upstream-сервиса, включая `401`, `4xx`, `5xx` и SOAP Fault, не должны автоматически преобразовываться Guard Proxy в другой прикладной ответ.
7. Должна поддерживаться настройка базового upstream URL и пути рабочего endpoint.


## 4. HTTPS

1. Guard Proxy должен поддерживать входящие HTTP и HTTPS endpoints.
2. Тип входящего endpoint определяется схемой `listen.url`.
3. Для `https://` endpoint должен настраиваться серверный TLS-сертификат.
4. Должны поддерживаться как минимум следующие источники серверного сертификата:
   - Windows Certificate Store;
   - PFX-файл.
5. Для сертификата из Windows Certificate Store должны настраиваться как минимум:
   - `storeName`;
   - `storeLocation`;
   - `thumbprint`.
6. Для сертификата из PFX-файла должны настраиваться как минимум:
   - путь к PFX-файлу;
   - пароль сертификата.
7. Guard Proxy не должен автоматически перенаправлять HTTP-запросы на HTTPS.
8. Upstream URL, Health Check URL и Recovery Probe URL должны поддерживать как `http://`, так и `https://`.
9. Для HTTPS upstream и probes должна использоваться стандартная проверка TLS-сертификата средствами .NET и операционной системы.
10. При `https://` в `listen.url` отсутствие TLS-конфигурации или невозможность загрузить сертификат должны считаться критической ошибкой конфигурации.
11. Загруженный серверный сертификат должен содержать private key.
12. TLS termination должен выполняться Guard Proxy средствами Kestrel.
13. Конкретные версии TLS не должны жёстко фиксироваться без отдельного требования; должны использоваться безопасные настройки .NET/операционной системы по умолчанию.

## 5. Аутентификация запросов

1. Guard Proxy должен прозрачно передавать входящий заголовок `Authorization`.
2. Basic Auth должен поддерживаться без специальной бизнес-логики Guard Proxy.
3. Guard Proxy не должен самостоятельно проверять credentials рабочих запросов, если это явно не предусмотрено отдельной конфигурацией в будущих версиях.
4. Credentials рабочих запросов не должны сохраняться Guard Proxy.

## 6. Ограничение параллельности

1. Guard Proxy должен ограничивать количество одновременно выполняющихся рабочих запросов к upstream-сервису.
2. Лимит задаётся параметром `proxy.maxConcurrentRequests`.
3. Размер очереди задаётся параметром `proxy.queueLimit`.
4. При `queueLimit = 0` запрос сверх установленного лимита должен немедленно завершаться `HTTP 503`.
5. Отклонённый по лимиту запрос не должен передаваться upstream-сервису.
6. Служебные health/recovery probes не должны занимать слоты `maxConcurrentRequests` рабочих запросов.
7. Рекомендуемые стартовые значения:
   - `maxConcurrentRequests = 8`;
   - `queueLimit = 0`.
8. Производственное значение `maxConcurrentRequests` должно определяться нагрузочным тестированием.

## 7. Таймауты рабочих запросов

1. Полный timeout рабочего запроса задаётся параметром `proxy.requestTimeoutMs`.
2. Timeout отсутствия сетевой активности задаётся параметром `proxy.activityTimeoutMs`.
3. Timeout служебных probes должен настраиваться независимо от timeout рабочих запросов.
4. При превышении полного timeout Guard Proxy должен завершать запрос контролируемой HTTP-ошибкой.
5. Рекомендуемые стартовые значения:
   - `requestTimeoutMs = 15000`;
   - `activityTimeoutMs = 10000`.

## 8. Active Health Check

1. Active Health Check должен включаться и отключаться через `config.json`.
2. Для Health Check должны настраиваться:
   - HTTP method;
   - URL;
   - interval;
   - timeout;
   - request headers;
   - request body;
   - допустимые HTTP status codes;
   - правило проверки response body.
3. Должна поддерживаться проверка JSON response body по настраиваемому полю и ожидаемому значению.
4. Для JSON-проверки должны настраиваться как минимум:
   - путь/имя поля;
   - ожидаемое значение;
   - режим точного сравнения.
5. Если проверка response body отключена, успешность probe определяется настроенными HTTP status codes.
6. Некорректный JSON при включённой JSON-проверке считается неуспешным Health Check.
7. Timeout, transport failure и HTTP status вне разрешённого списка считаются неуспешным Health Check.
8. Правило признания upstream-сервиса недоступным должно настраиваться.
9. Должно поддерживаться как минимум количество последовательных неуспешных проверок `failureThreshold`.
10. При достижении порога upstream-сервис должен переходить в состояние `Unhealthy`.

## 9. Проверка восстановления

1. Правила восстановления после состояния `Unhealthy` должны полностью задаваться через `config.json`.
2. Должны поддерживаться следующие источники recovery-проверки:
   - Health Check probe;
   - отдельный probe основного или другого указанного URL;
   - оба probe параллельно.
3. Режим задаётся параметром `recovery.mode`.
4. Поддерживаемые значения:
   - `healthcheck`;
   - `main`;
   - `parallel`.
5. Для `parallel` должна настраиваться политика успешного цикла:
   - `all` — успешны все probes;
   - `any` — успешен хотя бы один probe.
6. Main probe должен иметь независимые настройки:
   - HTTP method;
   - URL;
   - timeout;
   - request headers;
   - request body;
   - допустимые HTTP status codes;
   - правило проверки response body при необходимости.
7. Health Check probe и main probe в режиме `parallel` должны запускаться параллельно.
8. Количество последовательных успешных циклов восстановления задаётся параметром `recovery.successThreshold`.
9. Интервал recovery-проверок задаётся параметром `recovery.intervalMs`.
10. Recovery-проверки выполняются, пока upstream-сервис остаётся в состоянии `Unhealthy`; отдельное состояние `Recovering` не используется.
11. Пока условия восстановления не выполнены, рабочие запросы не должны передаваться upstream-сервису.
12. После выполнения условий восстановления upstream-сервис автоматически переводится из `Unhealthy` в `Healthy`.
13. Для одного upstream-сервиса одновременно может выполняться не более одного health/recovery-цикла.
14. В `recovery.mode = parallel` health probe и main probe выполняются параллельно внутри одного recovery-цикла.
15. Если текущий health/recovery-цикл не завершён к наступлению следующего интервала, новый цикл не запускается и не ставится в очередь.

## 10. Passive monitoring

1. Passive monitoring рабочих запросов должен включаться и отключаться через `config.json`.
2. Guard Proxy должен учитывать как failures как минимум:
   - connection refused;
   - connection reset;
   - transport timeout;
   - другие ошибки установления или поддержки соединения.
3. Должны настраиваться:
   - окно наблюдения;
   - минимальное количество запросов;
   - порог доли transport failures.
4. Passive monitoring должен анализировать только transport-level failures.
5. HTTP status, SOAP Fault и содержимое HTTP response не должны влиять на passive health state сами по себе.
6. Критерии HTTP-уровня допускаются только в Active Health Check и Recovery Probe в соответствии с их конфигурацией.
7. При превышении настроенного порога upstream-сервис должен переходить в состояние `Unhealthy`.

## 11. Состояния upstream

1. Guard Proxy должен поддерживать как минимум состояния:
   - `Unknown`;
   - `Healthy`;
   - `Unhealthy`.
2. При `Unhealthy` рабочие запросы не должны передаваться upstream-сервису.
3. Рабочие запросы при `Unhealthy` должны быстро завершаться `HTTP 503`.
4. Изменение состояния upstream должно фиксироваться в журнале.

## 12. Retry

1. Автоматический retry рабочих запросов запрещён.
2. `proxy.retryCount` должен иметь значение `0`.

## 13. Health endpoints Guard Proxy

1. Guard Proxy должен предоставлять `/health/live`.
2. `/health/live` должен подтверждать, что процесс Guard Proxy запущен и способен принимать HTTP-запросы.
3. Guard Proxy должен предоставлять `/health/ready`.
4. `/health/ready` должен отражать готовность Guard Proxy передавать рабочие запросы upstream-сервису.
5. Служебные endpoints Guard Proxy:
   - не должны проксироваться upstream;
   - не должны учитываться в concurrency limit рабочих запросов.

## 14. Логирование

1. Guard Proxy должен вести циклические перезаписываемые логи.
2. Максимальная общая ёмкость логов задаётся в `config.json`.
3. После достижения настроенной ёмкости самые старые лог-файлы должны удаляться или перезаписываться.
4. Должны настраиваться как минимум:
   - каталог логов;
   - уровень логирования;
   - максимальный общий размер;
   - максимальный размер одного файла;
   - максимальное количество файлов.
5. Для рабочего запроса логируются как минимум:
   - timestamp;
   - request ID;
   - method;
   - path;
   - duration;
   - HTTP status;
   - результат Guard Proxy;
   - текущее состояние upstream.
6. Для Health Check и recovery probe логируются как минимум:
   - timestamp;
   - тип probe;
   - duration;
   - HTTP status;
   - результат;
   - причина ошибки;
   - изменение состояния upstream.
7. Должны различаться как минимум причины:
   - `CONCURRENCY_LIMIT`;
   - `REQUEST_TIMEOUT`;
   - `CONNECT_FAILURE`;
   - `NO_HEALTHY_DESTINATION`;
   - `HEALTH_CHECK_TIMEOUT`;
   - `HEALTH_CHECK_HTTP_ERROR`;
   - `HEALTH_CHECK_INVALID_BODY`;
   - `RECOVERY_CHECK_FAILED`.
8. Request/response body рабочих запросов по умолчанию не должны записываться в лог.
9. Значение заголовка `Authorization` не должно записываться в лог.

## 15. Конфигурация

1. Guard Proxy должен читать конфигурацию из файла `config.json`.
2. Поставляется файл `config-example.json`.
3. Настройки маршрутизации, лимитов, таймаутов, probes, recovery, passive monitoring и логирования должны изменяться без перекомпиляции.
4. При старте Guard Proxy должен валидировать конфигурацию.
5. При критической ошибке конфигурации Guard Proxy не должен переходить в рабочее состояние.
6. Должны проверяться как минимум:
   - корректность listen URL;
   - наличие и корректность TLS-конфигурации при `https://` listen URL;
   - возможность загрузить серверный сертификат и наличие private key;
   - наличие upstream URL;
   - `maxConcurrentRequests > 0`;
   - `queueLimit >= 0`;
   - положительные timeout и interval;
   - корректность `recovery.mode`;
   - корректность `parallelSuccessPolicy`;
   - корректность параметров probe;
   - при включённом passive monitoring: `windowMs > 0`, `minimumRequests > 0`, `0 < failureRate <= 1`;
   - корректность `logging.level`;
   - положительные значения ограничений размера и количества лог-файлов;
   - корректность остальных обязательных параметров логирования.

## 16. Windows Service

1. Guard Proxy должен устанавливаться и запускаться как Windows Service.
2. Должен поддерживаться консольный запуск для разработки и диагностики.
3. Production-сборка должна быть self-contained `win-x64`.

## 17. Тестируемость

1. Собственная логика Guard Proxy должна быть отделена от YARP/ASP.NET Core pipeline настолько, чтобы её можно было unit-тестировать независимо.
2. Unit-тестами должны покрываться как минимум:
   - validation конфигурации;
   - проверка health response;
   - переходы состояний `Unknown/Healthy/Unhealthy`;
   - recovery policies `healthcheck/main/parallel`;
   - `parallel` policies `all/any`;
   - `successThreshold`;
   - passive failure threshold;
   - классификация transport failures;
   - политика ротации логов.
3. Поведение HTTP pipeline, YARP routing, passthrough headers, concurrency limiting и timeout должно проверяться интеграционными тестами.
4. Health, recovery и passive monitoring logic должны использовать абстракцию времени, позволяющую unit-тестам управлять временем без реального ожидания.
5. Unit-тесты интервалов, timeout-related state logic и временных окон не должны зависеть от `Thread.Sleep`, реального ожидания или wall-clock времени.

## 18. Критерии приёмки

1. Рабочий HTTP/SOAP-запрос проходит через Guard Proxy без изменения бизнесового результата.
2. Входящий `Authorization` прозрачно передаётся upstream-сервису.
3. Запрос сверх `maxConcurrentRequests` при `queueLimit = 0` получает `503` и не достигает upstream.
4. Зависший рабочий запрос завершается по настроенному timeout.
5. Active Health Check корректно определяет успешный и неуспешный probe в соответствии с конфигурацией.
6. После достижения `failureThreshold` upstream переходит в `Unhealthy`.
7. В состоянии `Unhealthy` рабочие запросы получают `503` без обращения к upstream.
8. В `recovery.mode = healthcheck` восстановление определяется Health Check probe.
9. В `recovery.mode = main` восстановление определяется main probe.
10. В `recovery.mode = parallel` probes запускаются параллельно.
11. Для `parallel` корректно работают политики `all` и `any`.
12. После достижения `recovery.successThreshold` upstream возвращается в `Healthy`.
13. Во время recovery upstream остаётся `Unhealthy`; отдельное состояние `Recovering` отсутствует.
14. Наложение нескольких health/recovery-циклов для одного upstream не допускается.
15. HTTP `500` или SOAP Fault сами по себе не считаются transport failure.
16. `/health/live` работает независимо от состояния upstream.
17. `/health/ready` отражает готовность проксировать рабочие запросы.
18. Циклические логи не превышают настроенную ёмкость.
19. Некорректный `config.json` блокирует переход Guard Proxy в рабочее состояние.
