@echo off

protoc --csharp_out=../Shared/Packets client.proto
protoc --csharp_out=../Shared/Packets server.proto

opgen actions.xml

protoc --csharp_out=../Updater system.proto