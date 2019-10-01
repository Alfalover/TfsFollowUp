FROM mcr.microsoft.com/dotnet/core/aspnet:3.0

RUN apt-get update -y
RUN apt-get install libcurl4-openssl-dev -y
RUN apt-get install gss-ntlmssp -y
RUN apt-get autoremove -y

WORKDIR /app
COPY ./app/out /app



EXPOSE 4321
ENTRYPOINT ["dotnet", "TeamFollowUpConsole.dll"]