
opensslでの鍵生成の方法（XMLへの変換も必要です）
```sh
openssl genrsa -out private.key 4096
openssl rsa -in private.key -out private.key
openssl rsa -in private.key -pubout -out public.pem
```

もしくはSRNet.Toolsにて
```
#鍵生成
dotnet run key-generate
#秘密鍵をxmlに変換
dotnet run  key-to-xml (filepath)
```