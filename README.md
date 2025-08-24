# ai-anken-mailer

* 案件情報をデータ化するIMAP用のメーラーです。
* 基本転記な閲覧のためのメーラーとしてのUI機能に加え、案件を紹介するメールの本文などから案件情報をデータ化する機能を有します。


# vscodeでの取り扱い
① NuGetパッケージの復元
```
dotnet restore AnkenMailer/AnkenMailer.csproj
```

② WPFアプリ本体のビルド
```
dotnet build AnkenMailer/AnkenMailer.csproj -c Release
```
※WPFのビルドは、「③ パッケージ化」に包括されます。
③ パッケージ化（wapprojのビルド） 
```
msbuild AnkenMailer-app/AnkenMailer-app.wapproj /p:Configuration=Release /p:Platform=x86
```
※「msbuild」コマンドはVisual Studio本体、または「Build Tools for Visual Studio」（無料）に含まれています。
下記URLから「Build Tools for Visual Studio」をダウンロード・インストールしてください。 https://visualstudio.microsoft.com/ja/downloads/#build-tools-for-visual-studio-2022 (ただし、2025/8/22時点でリンクが壊れていました。ダウンロードボタンのリンク先が```https://aka.ms/vs/17/release/vs_BuildTools.exe%20```となっていましたが、末尾の"%20"は不要です)
※「Build Tools for Visual Studio」は、Visual Studioの各エディションごとに、インストール先フォルダが異なります。
Community版がインストール済みの場合
→ C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin
Professional版がインストール済みの場合
→ C:\Program Files\Microsoft Visual Studio\2022\Professional\Msbuild\Current\Bin
何もインストールされていない状態で「Build Tools for Visual Studio」のみインストールした場合
→ C:\Program Files\Microsoft Visual Studio\2022\BuildTools\Msbuild\Current\Bin

※「Build Tools for Visual Studio」は環境変数PATHを登録しません。上記のパスを登録する必要があります。