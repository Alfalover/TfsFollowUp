dotnet publish ../TeamFollowUpConsole -v n -c Release -o ../Docker/app/out
#cp ./GPM.MLServer.WebAPI/bin/Release/netstandard2.1/GPM.MLServer.WebAPI.XML ./app/out/
#cp -Force ../GPM.MLServer.Application/Data/appsettings-docker.json ./app/out/Data/appsettings.json

docker build . -t tfsfollowup -f Dockerfile