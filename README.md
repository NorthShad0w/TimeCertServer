# TimeCertServer

时间戳证书服务器 方便使用黑签名。

支持linux，支持docker一键部署，方便直接部署到阿里云云函数上面用。

## 使用方法

### 1.生成证书和私钥

```bash
openssl genrsa -out cakey.pem 4096
openssl req -new -key cakey.pem -out ca.csr
faketime '2008-12-24 08:15:42' openssl x509 -req -in ca.csr -signkey cakey.pem -out ca.crt -days 36500
openssl genrsa -traditional -out private.key 4096
openssl req -new -key private.key -out certificate.csr
faketime '2008-12-25 08:15:42' openssl x509 -extfile extfile.txt -extensions v3_req_p -req -sha1 -days 36500 -CA ca.crt -CAkey cakey.pem -CAcreateserial -in certificate.csr -out certificate.pem
cat ca.crt >> certificate.pem
```

将ca.crt作为跟证书安装到windows下

## 2.启动

```bash
docker build .
docker run -p 8080:8080 --rm -it --name timestamp <id-of-your-image>
```

## 3.打时间戳

```bat
signtool.exe timestamp /t "http://your-server-ip-port/TSA/2011-04-01T00:00:00“ test.exe
```

## 注意事项

请勿用于违法用途

## reference

- https://github.com/PIKACHUIM/FakeSign
- https://www.52pojie.cn/thread-908684-1-1.html