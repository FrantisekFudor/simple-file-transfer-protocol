all : clean restore build publish

clean:
	rm -rf ./client/bin
	rm -rf ./client/obj
	rm -rf ./server/obj
	rm -rf ./server/bin
	rm -rf ./ipk-simpleftp-client
	rm -rf ./ipk-simpleftp-server

restore:
	dotnet restore ./server/ipk-simpleftp-server.csproj
	dotnet restore ./client/ipk-simpleftp-client.csproj

build: 
	dotnet build ./server/ipk-simpleftp-server.csproj
	dotnet build ./client/ipk-simpleftp-client.csproj

publish:

	dotnet publish ./server/ipk-simpleftp-server.csproj -r linux-x64 -o ./ -p:PublishSingleFile=true --self-contained true
	dotnet publish ./client/ipk-simpleftp-client.csproj -r linux-x64 -o ./ -p:PublishSingleFile=true --self-contained true
	