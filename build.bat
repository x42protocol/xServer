dotnet restore xServer.D.sln
dotnet build -c Release -r linux-x64 -v m xServer.D.sln -p:ImportByWildcardBeforeSolution=false
dotnet publish -c Release -r linux-x64 -v m -o ./build xServer.D/xServerD.csproj -p:ImportByWildcardBeforeSolution=false
tar -cvzf x42.Node-1.1.29-linux-x64.tar.gz -C build\xserver.d *

