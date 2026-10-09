using System.Runtime.CompilerServices;

namespace SolidWorksBatchExport.Core;

public class ProcessingSettings
{
	[CompilerGenerated]
	private double _notchDiameterMm = 4.0;

	[CompilerGenerated]
	private string _notchDiameterMode = "Auto";

	[CompilerGenerated]
	private bool _exportDWG = true;

	[CompilerGenerated]
	private bool _exportDXF = true;

	[CompilerGenerated]
	private bool _createNotch = true;

	[CompilerGenerated]
	private bool _hideBendLines;

	[CompilerGenerated]
	private bool _useDrawingTemplate;

	[CompilerGenerated]
	private string _drawingTemplatePath = "";

	[CompilerGenerated]
	private bool _processAllConfigurations;

	[CompilerGenerated]
	private string _outputDirectoryMode = "";

	[CompilerGenerated]
	private string _customOutputDirectory = "";

	[CompilerGenerated]
	private string _folderGrouping = "None";

	[CompilerGenerated]
	private string _outputPlacement = "OwnFolders";

	[CompilerGenerated]
	private string _fileNamePattern = "%DA%";

	[CompilerGenerated]
	private bool _appendThicknessQtyToName;

	[CompilerGenerated]
	private string _customPropertyKey = "Tanım";

	[CompilerGenerated]
	private bool _fastNotches;

	public double NotchDiameterMm
	{
		[CompilerGenerated]
		get
		{
			return _notchDiameterMm;
		}
		[CompilerGenerated]
		set
		{
			_notchDiameterMm = value;
		}
	}

	public string NotchDiameterMode
	{
		[CompilerGenerated]
		get
		{
			return _notchDiameterMode;
		}
		[CompilerGenerated]
		set
		{
			_notchDiameterMode = value;
		}
	}

	public bool ExportDWG
	{
		[CompilerGenerated]
		get
		{
			return _exportDWG;
		}
		[CompilerGenerated]
		set
		{
			_exportDWG = value;
		}
	}

	public bool ExportDXF
	{
		[CompilerGenerated]
		get
		{
			return _exportDXF;
		}
		[CompilerGenerated]
		set
		{
			_exportDXF = value;
		}
	}

	public bool CreateNotch
	{
		[CompilerGenerated]
		get
		{
			return _createNotch;
		}
		[CompilerGenerated]
		set
		{
			_createNotch = value;
		}
	}

	public bool HideBendLines
	{
		[CompilerGenerated]
		get
		{
			return _hideBendLines;
		}
		[CompilerGenerated]
		set
		{
			_hideBendLines = value;
		}
	}

	public bool UseDrawingTemplate
	{
		[CompilerGenerated]
		get
		{
			return _useDrawingTemplate;
		}
		[CompilerGenerated]
		set
		{
			_useDrawingTemplate = value;
		}
	}

	public string DrawingTemplatePath
	{
		[CompilerGenerated]
		get
		{
			return _drawingTemplatePath;
		}
		[CompilerGenerated]
		set
		{
			_drawingTemplatePath = value;
		}
	}

	public bool ProcessAllConfigurations
	{
		[CompilerGenerated]
		get
		{
			return _processAllConfigurations;
		}
		[CompilerGenerated]
		set
		{
			_processAllConfigurations = value;
		}
	}

	public string OutputDirectoryMode
	{
		[CompilerGenerated]
		get
		{
			return _outputDirectoryMode;
		}
		[CompilerGenerated]
		set
		{
			_outputDirectoryMode = value;
		}
	}

	public string CustomOutputDirectory
	{
		[CompilerGenerated]
		get
		{
			return _customOutputDirectory;
		}
		[CompilerGenerated]
		set
		{
			_customOutputDirectory = value;
		}
	}

	public string FolderGrouping
	{
		[CompilerGenerated]
		get
		{
			return _folderGrouping;
		}
		[CompilerGenerated]
		set
		{
			_folderGrouping = value;
		}
	}

	public string OutputPlacement
	{
		[CompilerGenerated]
		get
		{
			return _outputPlacement;
		}
		[CompilerGenerated]
		set
		{
			_outputPlacement = value;
		}
	}

	public string FileNamePattern
	{
		[CompilerGenerated]
		get
		{
			return _fileNamePattern;
		}
		[CompilerGenerated]
		set
		{
			_fileNamePattern = value;
		}
	}

	public bool AppendThicknessQtyToName
	{
		[CompilerGenerated]
		get
		{
			return _appendThicknessQtyToName;
		}
		[CompilerGenerated]
		set
		{
			_appendThicknessQtyToName = value;
		}
	}

	public string CustomPropertyKey
	{
		[CompilerGenerated]
		get
		{
			return _customPropertyKey;
		}
		[CompilerGenerated]
		set
		{
			_customPropertyKey = value;
		}
	}

	public double PointTolerance => 0.0001;

	public bool FastNotches
	{
		[CompilerGenerated]
		get
		{
			return _fastNotches;
		}
		[CompilerGenerated]
		set
		{
			_fastNotches = value;
		}
	}
}
