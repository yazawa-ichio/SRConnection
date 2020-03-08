
鍵生成の方法
```sh
openssl genrsa -out private.key 4096
openssl rsa -in private.key -out private.key
openssl rsa -in private.key -pubout -out public.pem
```