
# Getting started ![Build status](https://github.com/github/docs/actions/workflows/on-push.yml/badge.svg)

## How to Build

```bash
dotnet build ./AnalyzeDotNetProject.sln --configuration Release
```

## How to Run
```base
cd ./bin/Release/net6.0/
./AnalyzeDotNetProject.exe "c:\yourproject\path.sln"
```

**Output**:
* result.txt: result in flat file.
* result.csv: result in csv format.
* log.txt: log file.
