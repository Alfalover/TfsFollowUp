REM docker volume create gpm.mlserver.data (Not needed, it will create volume if don't exists 
rem docker run -p 4321:4321 -v gpm.mlserver.data:/app/Data/ gpm.mlserver  "http://+:4321"
docker run -p 4321:80 tfsfollowup  "http://+:4321"

