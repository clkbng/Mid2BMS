# TODO

## Stage 0. Convert project to .NET 5.0

プロジェクトのランタイムを.NET Frameworkから.NET 5.0に移行して、  
最低限メインウィンドウを起動できるようにする。

## Stage 1. Find & Fix bugs

.NET 5.0化と64bit対応によりバグが発生していないか調べる。  
(十中八九発生してる)  
バグを発見した場合は一つ一つ丁寧に潰す。

## Stage 2. Refactor & Optimize

UIと直結している部分以外のコードについて、以下の修正を行う。

- 各種リファクタリング
- コード最適化
- メモリリークの修正
- C# 9.0でいい感じのコードに書き直す

## Stage 3. .NET 6.0化 & MAUI化

.NET 5.0から.NET 6.0への移行は一瞬でできる想定。 (できてくれ)  
そして、UIをWinFormsから[.NET Multi-platform App UI](https://github.com/dotnet/maui)に書き直す。  
実際に動くのはWindowsだけで構わない。 (要するに中身はXamarin.Forms.Windows)  
一連の目標の中で、このMAUI化が一番重いだろうという想定。

## Stage 4. NVorbisをCLR化する & 64bit対応

ここもやや重いかもしれない。詳細↓  
https://docs.microsoft.com/ja-jp/cpp/dotnet/walkthrough-compiling-a-cpp-program-that-targets-the-clr-in-visual-studio?view=msvc-160

## Stage 5. マルチプラットフォーム化

と言っても、実際にWindows以外で需要ありそうなのはMacOSぐらい？
LinuxはLinux対応のBMSエディタとBMSプレイヤーが出たら考える。

## Stage EX. さらなる高みへ

機能追加とか、BugFixとか、パフォーマンス向上とか、その他色々

## Current Stage

Stage 0が完了した段階。  
Stage 1は未着手。
