# Public/Private Key Authentication Sample Client

In order to use this sample client you will have to create a public/private key pair. In order to do so we can
use [OpenSSL](https://www.openssl.org/). For Windows users we can take advantage of the `openssl.exe`
utility which is installed along with Git (we all use Git, right?).

1. Open [Windows Terminal](https://www.microsoft.com/en-us/p/windows-terminal/9n0dx20hk701) or the 
`command prompt` **(as an Administrator)** and enter the following command:
```bash
cd C:\Program Files\Git\usr\bin
```
This will take us to the location where `openssl.exe` resides.

2. Then, in order to generate the public/private key pair enter the following command:
```bash
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes
```
This will create a file called `key.pem` and a file called 'cert.pem' which is the key pair with 
length of 4096 bits.
