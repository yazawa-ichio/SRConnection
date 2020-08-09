
dotnet-format

rm -rf ../unity/Assets/SRConnection.Unity/Core/*.cs
rm -rf ../unity/Assets/SRConnection.Unity/Core/**/*.cs
rm -rf ../unity/Assets/SRConnection.Unity/Core/**/**/*.cs
cp -r ./SRConnection/Core ../unity/Assets/SRConnection.Unity