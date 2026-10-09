using System.Runtime.CompilerServices;

namespace SolidWorksBatchExport.Core;

public class NotchPoint
{
	[CompilerGenerated]
	private double[] _point;

	[CompilerGenerated]
	private double[] _outwardDirection;

	public double[] Point
	{
		[CompilerGenerated]
		get
		{
			return _point;
		}
		[CompilerGenerated]
		set
		{
			_point = value;
		}
	}

	public double[] OutwardDirection
	{
		[CompilerGenerated]
		get
		{
			return _outwardDirection;
		}
		[CompilerGenerated]
		set
		{
			_outwardDirection = value;
		}
	}
}
