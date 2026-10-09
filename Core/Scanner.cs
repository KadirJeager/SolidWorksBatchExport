using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SolidWorksBatchExport.Core;

public class Scanner
{


	public List<string> GetModelFilesFromFolder(string P_0)
	{
		if (string.IsNullOrWhiteSpace(P_0) || !Directory.Exists(P_0))
		{
			throw new ArgumentException("Geçerli bir klasör yolu belirtilmedi.");
		}
		string[] field_0400008D = new string[2] { ".sldprt", ".sldasm" };
		return (from path in Directory.GetFiles(P_0, "*.*", SearchOption.AllDirectories)
			where Enumerable.Contains(field_0400008D, Path.GetExtension(path).ToLower())
			where !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal)
			select path).ToList();
	}
}
