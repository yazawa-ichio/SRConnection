﻿# SRConnection
安全なハンドシェイクと平文・暗号をそれぞれ送れる通信ライブラリです。  
Unityで利用する事を前提にしています。  
Unityの新しいtransportパッケージの中身を見る前に作り始めましたが、そちらが思いのほか良さそうなので、このライブラリをこれ以上拡張するかは分かりません。  

以下のパターンで利用できます。
* サーバー/クライアント型
* サーバー/クライアント型 + クライアント間P2P
* StunとHttp等のサーバーを利用したP2P型
* ローカルLANを利用したP2P型

## 使用方法について(WIP)

unityのサンプル実装に簡単な実装例があります。  

### UPMで導入

upmでインストールできます。  
manifest.jsonに以下のパッケージを追加してください。  

`"jp.ilib.srconnection.unity": "https://github.com/yazawa-ichio/SRConnection.git?path=unity/Assets/SRConnection.Unity"`


### 暗号部分の問題点について
基本的な箇所は抑えていますが、いくつかの点で弱い部分があります。  
サーバー/クライアント型のハンドシェイクではRSAを利用するため前方秘匿性がありません。  
また、ローカルLANのみハンドシェイクに中間者攻撃を回避出来ていません。（今後の実装でも対応しません）  

ゲームでの利用だとこれぐらいで強度で十分なのではという気持ちはあります。  

### 今後するかもしれない事

* RUDPの実装がとりあえずの物でパフォーマンスを確認していないので最適化するかも
* 暗号の方式をChaCha20Poly1305に切り替え+シーケンス番号のランダム化と長さを内部的にintに変更（送信サイズは2byte）
