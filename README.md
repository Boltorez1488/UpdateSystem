# Система обновления 🇷🇺
Клиент-серверная система обновления файлов, преимущественно для игровых серверов. (Личный код устарел :nerd_face:).

Использует:
- Google Protobuf 3
- [Opcodes Generator](https://github.com/Boltorez1488/OpcodesGeneratorProto)
- NetCore 5.0
- .Net Framework 4.8
- WPF
- AspNetCore
- Vue.js
- Nginx

Функции:
- Работает в реальном времени. Это значит, что изменения в файлах сразу доставляются клиентам и они получают уведомление.
- Файлы можно загружать как через веб панель, так и через обычные методы, типа FTP.
- Сам клиентский софт можно обновлять, если загрузить его новую версию на сервер. Он может сам себя обновить.
- Для скачивания файлов используется Nginx, в связи с высокой скоростью передачи.
- Язык везде русский.

Инструкция:
1. Опубликовать WebServer (Linux), настроить cfg/server.xml.
2. Залить на сервер.
3. Настроить nginx на папку загрузок.
4. Изменить (Host, Port) в Updater.Socket.Client для подключения.
5. Отдать клиентам софт.
6. PROFIT???

# Update system 🇺🇸
Client-server system for updating files, mainly for game servers. (Personal code is deprecated :nerd_face:)

Using:
- Google Protobuf 3
- [Opcodes Generator](https://github.com/Boltorez1488/OpcodesGeneratorProto)
- NetCore 5.0
- .Net Framework 4.8
- WPF
- AspNetCore
- Vue.js
- Nginx

Features:
- Works in real time. This means that changes to files are immediately delivered to clients and they are notified.
- Files can be uploaded both through the web panel and through conventional methods such as FTP.
- The client software itself can be updated by uploading its new version to the server. He can renew himself.
- Nginx is used to download files, due to the high transfer speed.
- The language is Russian everywhere.

Instruction:
1. Publish WebServer (Linux), configure cfg/server.xml.
2. Upload to server.
3. Configure nginx to download folder.
4. Change (Host, Port) in Updater.Socket.Client for connection.
5. Give software to clients.
6. PROFIT???

## Screenshots
![Панель управления](https://user-images.githubusercontent.com/89551246/131261174-06df5adf-3230-425d-bbd3-2a13cc686aea.png)
![Клиентский софт](https://user-images.githubusercontent.com/89551246/131261191-d786f57b-a108-4290-919c-29dfb848e898.png)
