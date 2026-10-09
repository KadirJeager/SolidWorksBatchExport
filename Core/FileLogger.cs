using System;
using System.IO;

namespace SolidWorksBatchExport.Core;

public class FileLogger
{
	private readonly string _logFilePath;

	private readonly object field_0400004A = new object();

	public string LogFilePath => _logFilePath;

	public FileLogger()
	{
		try
		{
			string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string text2 = DateTime.Now.ToString("yyyyMMdd");
			_logFilePath = Path.Combine(text, "export_log_" + text2 + ".txt");
		}
		catch
		{
			_logFilePath = Path.Combine(Path.GetTempPath(), $"export_log_{DateTime.Now:yyyyMMdd}.txt");
		}
	}

	public void Write(string P_0)
	{
		try
		{
			lock (field_0400004A)
			{
				string value = DateTime.Now.ToString("HH:mm:ss");
				string contents = $"[{value}] {P_0}{Environment.NewLine}";
				File.AppendAllText(_logFilePath, contents);
			}
		}
		catch
		{
		}
	}
}
