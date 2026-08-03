# WinUI フォルダ構成案

`Othello.WinUI` は、表示責務に限定した以下の構成を基本とします。

```text
Othello.WinUI/
  App.xaml
  App.xaml.cs
  Othello.WinUI.csproj

  View/
	MainWindow.xaml
	MainWindow.xaml.cs

  ViewModel/
	MainWindowViewModel.cs

  Models/
	PlatformSummaryModel.cs

  Services/
	IPlatformSummaryService.cs
	PlatformSummaryService.cs

  Controls/
	(再利用UIコントロールを配置)
```

## 将来拡張時の推奨サブ構成

```text
View/
  Pages/
  Dialogs/

ViewModel/
  Pages/
  Dialogs/

Models/
  UiState/
  Dto/

Services/
  Interfaces/
  Adapters/
```

## 役割分担
- View: 表示とバインディングのみ。
- ViewModel: 表示状態とコマンド。
- Models: 表示用DTO。
- Services: Core/AI/Training 連携の窓口。
- Controls: 再利用可能な見た目部品。
