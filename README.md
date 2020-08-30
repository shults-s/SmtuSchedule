[![License: CC BY-NC 4.0](https://img.shields.io/badge/License-CC%20BY--NC%204.0-lightgrey.svg)](https://creativecommons.org/licenses/by-nc/4.0/)
# Расписание СПбГМТУ
Мобильное приложение, которое знает расписание занятий любой группы, любого преподавателя в любой день семестра.

<a href="https://play.google.com/store/apps/details?id=shults.smtuschedule"><img src="https://play.google.com/intl/en_us/badges/static/images/badges/ru_badge_web_generic.png" alt="Страница приложения в Google Play Маркет" width="200" /></a>

## Возможности
- Загрузка произвольного количества расписаний;
- Автономный (без доступа к интернету) просмотр загруженных расписаний;
- Напоминания о предстоящих занятиях (beta);
- Автоматическое обновление расписаний;
- Автодополнение имен преподавателей при загрузке расписаний;
- Быстрый переход между расписаниями с сохранением просматриваемой даты;
- Подсветка текущей пары;
- Выводятся только занятия, которые будут в указанный день (с учетом недели: числитель/знаменатель);
- При необходимости можно самостоятельно редактировать расписания и распространять их измененные версии.

## Скриншоты
![Скриншоты](https://raw.githubusercontent.com/shults-s/SmtuSchedule/master/Screenshots/1.0.0.png)

## Совместимость
Поддерживаются смартфоны под управлением операционной системы Android начиная с версии 5.0 и заканчивая 11 (последняя на данный момент). Поддержки iOS нет, и, скорее всего, не будет.

## Установка и настройка
Ниже приведена инструкция по установке приложения на смартфоны, на которых по каким-либо причинам недоступен Google Play Маркет.
- В настройках смартфона разрешить установку приложений из неизвестных источников;
- Загрузить [последнюю версию](https://github.com/shults-s/SmtuSchedule/releases/latest) установочного APK-файла для своей аппаратной архитектуры и запустить его.

Для корректной работы приложению необходимо разрешение на доступ в интернет (для загрузки расписаний) и разрешение на доступ к памяти смартфона (для сохранения расписаний).

## Редактирование расписаний
При первом запуске приложение создает во внутренней памяти смартфона директорию ```Schedules``` по пути ```/Android/data/shults.smtuschedule/```. Все загружаемые в дальнейшем с сайта Корабелки расписания хранятся в этой директории в виде текстовых файлов в формате [JSON](https://ru.wikipedia.org/wiki/JSON). Их можно скопировать на компьютер, отредактировать, а затем закинуть обратно на смартфон. Изменения вступят в силу после перезапуска приложения. По завершению редактирования расписание желательно проверить валидатором (например, [этим](https://jsonlint.com/)) на наличие синтаксических ошибок. Расписания с ошибками приложение не открывает, о чем выводит соответствующее сообщение. Подробное описание возникших ошибок сохраняется в журнальный файл в соседней с ```Schedules``` директории ```Logs```.

## Обратная связь
Пожелания, вопросы, сообщения об ошибках и обо всем, что касается этого проекта, пишите [мне в личку](https://vk.com/shults_s).

## Краткое описание проекта
Разработка приложения началась в сентябре 2018 года с идеи сделать расписание университета более удобным в использовании. Это мой первый опыт в мобильной разработке. Сейчас проект насчитывает пять тысяч строк кода. Приложение написано на языке C# с использованием библиотек [Xamarin.Android](https://docs.microsoft.com/en-us/xamarin/android/), [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview) и [HtmlAgilityPack](https://html-agility-pack.net).