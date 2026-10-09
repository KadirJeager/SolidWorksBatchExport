using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;

namespace SolidWorksBatchExport;

public class App : Application
{
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.29.0")]
	public void InitializeComponent()
	{
		StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.29.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
