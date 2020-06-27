[English](https://github.com/TenteEEEE/app_binder?#AppBinder)
# AppBinderとは
<div align="center"><img src="./app_binder/appbinder_logo.png"></div>

AppBinderはアプリケーション間の拘束関係(バインディング)を作るアプリです。  
アプリケーションバインディングとは「あるプログラムが起動したらあれを起動する」のような関係のことです。  
このプログラムの目的は単なるアプリケーションランチャーではなく、**バインドされたアプリの障害検知/修復および永続化を実現すること**です。

## アプリケーションバインディングの例
### 電卓を起動したらメモ帳も開きたい
Calculatorとnotepad(C:\Windows\System32\notepad.exe)をバインディングする
![1](https://user-images.githubusercontent.com/9051681/85918770-96a69880-b8a0-11ea-90f7-20f7c674db5c.gif)

### ついでにExcelも開きたい
Excel.exeとCalculatorのバインディングを追加する
![2](https://user-images.githubusercontent.com/9051681/85918803-cfdf0880-b8a0-11ea-861b-5ea2f15793f2.gif)

### あるアプリは必ず存在してほしい(永続化)
リスタートポリシーでは"on_failure(exit codeが0以外)"と"always"が選べます
![3](https://user-images.githubusercontent.com/9051681/85918818-f0a75e00-b8a0-11ea-9543-2ce604258bbf.gif)

## 既知の問題/将来性
### トリガーがうまく動いていない
実行するトリガープログラムのプロセス名が拡張子.exeを除いたものと一致しない場合、うまく動きません。  
Trigger EXE/Processの部分で適切なプロセス名を入力してください。

### トリガープロセスを終了したけどバインドされたプログラムが終了しない
おそらくExit codeが0以外で終了しています。  
一般的には正常終了では0がexit codeとなりますが、正常終了においても-1を返すプログラムがあります。  
自身で書いたプログラムの場合は修正してください。  
そうでない場合、自分がいつか除外するexit codeのオプションを実装する気になったときになんとかします。

### 双方向バインディング
逆方向のバインディングを追加すれば良いのですが、ちょっと面倒です。  
必要な人が多そうならちゃんと考えるかも。

# AppBinder
<div align="center"><img src="./app_binder/appbinder_logo.png"></div>

AppBinder is an application-binding program.  
This is not a general application launcher.  
It acheves *fault detection/recover and application persistence*.

## What is Application-Binding?
### I need a notepad when running calculators
Bind Calculator.exe and notepad.exe
![1](https://user-images.githubusercontent.com/9051681/85918770-96a69880-b8a0-11ea-90f7-20f7c674db5c.gif)

### I need Excel too
We just bind a Calculator and Excel.exe
![2](https://user-images.githubusercontent.com/9051681/85918803-cfdf0880-b8a0-11ea-861b-5ea2f15793f2.gif)

### An application must be persistent
It supports restart policy such as "on failure" and "always".
![3](https://user-images.githubusercontent.com/9051681/85918818-f0a75e00-b8a0-11ea-9543-2ce604258bbf.gif)

## Something went wrong?
### Trigger does not work
If a process name of "something.exe" is different from "something", it does not work now. 
Please set the correct process name on the "Trigger EXE/Process" manually.

### I killed a trigger process but binding program is still running
Probably, the trigger program's exit code is non-zero value(something fault).  
If it is your application, please modify the return number.  
When I get a motivation to make "exception exit-code", it will be better.

### Bidirectional Binding
I am considering this.  
It works when we add an opposite binding as a new config now, but it is not beautiful.
