using System.ComponentModel;
using System.IO;

namespace SolidWorksBatchExport;

public class FileListItem : INotifyPropertyChanged
{
	private bool _isSelected = true;

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			_isSelected = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSelected"));
		}
	}

	public string FullPath { get; set; }

	public string DisplayName => Path.GetFileName(FullPath) + ((FullPath != null && FullPath.ToLower().EndsWith(".sldasm")) ? "   [montaj]" : "");

	public event PropertyChangedEventHandler PropertyChanged;
}
