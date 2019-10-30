[![License: CC BY-NC 4.0](https://img.shields.io/badge/License-CC%20BY--NC%204.0-lightgrey.svg)](https://creativecommons.org/licenses/by-nc/4.0/)
# Расписание «СПбГМТУ»
Мобильное приложение, которое знает расписание занятий любой группы, любого преподавателя в любой день семестра.
- Расписания загружаются (парсятся) прямо с сайта Корабелки;
- Загрузить можно сколь угодно много расписаний, хоть все;
- После загрузки расписания можно просматривать автономно (без доступа к интернету);
- Реализовано автодополнение по списку имен преподавателей при загрузке расписаний;
- Возможен быстрый переход между расписаниями с сохранением просматриваемой даты;
- Текущая пара подсвечивается;
- Выводятся только занятия, которые будут в указанный день (с учетом недели: числитель/знаменатель);
- Открытый исходный код, при желании приложение можно скомпилировать самостоятельно.
## Скриншоты
![Версия 0.9](https://raw.githubusercontent.com/shults-s/SmtuSchedule/master/Screenshots/0.9.png)
## Лайфхаки
- Для возврата на текущую дату при просмотре расписания можно нажать кнопку «Назад»;
- Нажатие на имя преподавателя (группу) вызовет переход на его расписание, либо появится предложение загрузить расписание, если это не было сделано ранее;
- Удалить расписание или повторно загрузить его с сайта можно вызвав диалог действий над расписанием, для этого необходимо удерживать палец на его заголовке.
## Установка и настройка
- В настройках смартфона разрешить установку приложений из неизвестных источников (на данном этапе, пока неясно насколько это приложение будет нужно кому-то, кроме меня, нет смысла выкладывать его в Google Play Маркет);
- Загрузить [последнюю версию](https://github.com/shults-s/SmtuSchedule/releases) установочного APK-файла и запустить его;
- По окончанию установки открыть приложение и в его настройках задать значение параметра «Первый день семестра по числителю».
## Совместимость
Поддерживаются смартфоны на Android начиная с версии 5.0 и заканчивая 10 (последняя на данный момент). Поддержки iOS нет, и, скорее всего, не будет.
## Редактирование
При первом запуске приложение создает во внутренней памяти смартфона директорию с названием «Расписание СПбГМТУ». Все загружаемые в дальнейшем с сайта расписания хранятся в этой директории в виде текстовых файлов в формате [JSON](https://ru.wikipedia.org/wiki/JSON). Их можно скопировать на компьютер, отредактировать, а затем закинуть обратно на смартфон. После перезапуска приложения изменения вступят в силу. Простейшее изменение, которое можно внести в расписание – это скрыть занятие. Для этого достаточно поменять значение параметра IsDisplayed с «true» на «false». После внесения изменений код желательно проверить валидатором (например, [этим](https://jsonlint.com/)) на наличие синтаксических ошибок. Расписания с ошибками приложение не открывает, о чем выводит соответствующее сообщение.
## Обратная связь
Пожелания, вопросы, сообщения об ошибках и все, что касается этого проекта, можно написать [мне в личку](https://vk.com/shults_s).\
В случае обнаружения ошибки, особенно, если приложение вылетело, прошу подробно описывать предшествовавшие этому действия. Во всех нештатных ситуациях приложение записывает файл с отладочной информацией в директорию Logs, лежащую в директории приложения. Этот файл необходимо приложить к сообщению об ошибке. Он не содержит никакой приватной информации, кроме модели устройства и версии Android.
## Немного о проекте
Разработка приложения началась в сентябре 2018 года. Это мой первый опыт в мобильной разработке. Сейчас проект состоит из трех c небольшим тысяч строк кода, разметки, стилей и других ресурсов интерфейса. Приложение написано на языке C# с использованием библиотек [Xamarin.Android](https://docs.microsoft.com/ru-ru/xamarin/android/), [Json.NET](https://www.newtonsoft.com/json) и [HtmlAgilityPack](https://html-agility-pack.net).