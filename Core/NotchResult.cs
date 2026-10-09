using System.Runtime.CompilerServices;

namespace SolidWorksBatchExport.Core;

public class NotchResult
{
	[CompilerGenerated]
	private bool _flatPatternFound;

	[CompilerGenerated]
	private bool _bendLinesFound;

	[CompilerGenerated]
	private int _totalNotches;

	[CompilerGenerated]
	private int _successfulCuts;

	[CompilerGenerated]
	private int _failedCuts;

	[CompilerGenerated]
	private int _skippedInternalBends;

	public bool FlatPatternFound
	{
		[CompilerGenerated]
		get
		{
			return _flatPatternFound;
		}
		[CompilerGenerated]
		set
		{
			_flatPatternFound = value;
		}
	}

	public bool BendLinesFound
	{
		[CompilerGenerated]
		get
		{
			return _bendLinesFound;
		}
		[CompilerGenerated]
		set
		{
			_bendLinesFound = value;
		}
	}

	public int TotalNotches
	{
		[CompilerGenerated]
		get
		{
			return _totalNotches;
		}
		[CompilerGenerated]
		set
		{
			_totalNotches = value;
		}
	}

	public int SuccessfulCuts
	{
		[CompilerGenerated]
		get
		{
			return _successfulCuts;
		}
		[CompilerGenerated]
		set
		{
			_successfulCuts = value;
		}
	}

	public int FailedCuts
	{
		[CompilerGenerated]
		get
		{
			return _failedCuts;
		}
		[CompilerGenerated]
		set
		{
			_failedCuts = value;
		}
	}

	public int SkippedInternalBends
	{
		[CompilerGenerated]
		get
		{
			return _skippedInternalBends;
		}
		[CompilerGenerated]
		set
		{
			_skippedInternalBends = value;
		}
	}

	public string GetSummary()
	{
		if (!FlatPatternFound)
		{
			return "Açınım yok";
		}
		if (!BendLinesFound)
		{
			return "Büküm çizgisi yok";
		}
		string text = ((SkippedInternalBends > 0) ? $" ({SkippedInternalBends} atlandı)" : "");
		if (TotalNotches == 0)
		{
			if (SkippedInternalBends <= 0)
			{
				return "Nokta yok";
			}
			return "0/0" + text;
		}
		return $"{SuccessfulCuts}/{TotalNotches}{text}";
	}
}
