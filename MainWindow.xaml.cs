using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Win32;
using SolidWorksBatchExport.Core;

namespace SolidWorksBatchExport;

public partial class MainWindow : Window, IComponentConnector
{
	private readonly FileLogger _logger;

	private readonly string _settingsFilePath;

	private readonly ObservableCollection<FileListItem> _fileItems = new ObservableCollection<FileListItem>();

	private List<string> _allFiles = new List<string>();

	private bool _loading;

	public MainWindow()
	{
		InitializeComponent();
		if (Height > SystemParameters.WorkArea.Height - 20.0)
		{
			Height = SystemParameters.WorkArea.Height - 20.0;
		}
		if (Width > SystemParameters.WorkArea.Width - 20.0)
		{
			Width = SystemParameters.WorkArea.Width - 20.0;
		}
		_logger = new FileLogger();
		_settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
		itemsFiles.ItemsSource = _fileItems;
		LoadSettings();
		Log("Uygulama başlatıldı. Log dosyası: " + _logger.LogFilePath);
		if (!string.IsNullOrWhiteSpace(txtFolderPath.Text) && Directory.Exists(txtFolderPath.Text))
		{
			ScanFolder();
		}
		else
		{
			Log("Lütfen kaynak klasörü seçin.");
		}
	}

	private void LoadSettings()
	{
		_loading = true;
		try
		{
			if (File.Exists(_settingsFilePath))
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(_settingsFilePath));
				JsonElement rootElement = jsonDocument.RootElement;
				if (rootElement.TryGetProperty("FolderPath", out var value))
				{
					txtFolderPath.Text = value.GetString();
				}
				JsonElement value3;
				if (rootElement.TryGetProperty("NotchDiameterMm", out var value2))
				{
					txtNotchDiameter.Text = value2.GetString();
				}
				else if (rootElement.TryGetProperty("NotchRadius", out value3))
				{
					txtNotchDiameter.Text = value3.GetString();
				}
				if (rootElement.TryGetProperty("UseDrawingTemplate", out var value4))
				{
					chkUseDrawingTemplate.IsChecked = value4.GetBoolean();
					txtTemplatePath.IsEnabled = chkUseDrawingTemplate.IsChecked == true;
					btnBrowseTemplate.IsEnabled = chkUseDrawingTemplate.IsChecked == true;
				}
				if (rootElement.TryGetProperty("TemplatePath", out var value5))
				{
					txtTemplatePath.Text = value5.GetString();
				}
				if (rootElement.TryGetProperty("FolderGrouping", out var value6))
				{
					string text = value6.GetString();
					foreach (ComboBoxItem item in (IEnumerable)cmbFolderGrouping.Items)
					{
						if (item.Tag?.ToString() == text)
						{
							item.IsSelected = true;
							break;
						}
					}
				}
				if (rootElement.TryGetProperty("OutputPlacement", out var value7))
				{
					string text2 = value7.GetString();
					foreach (ComboBoxItem item2 in (IEnumerable)cmbOutputPlacement.Items)
					{
						if (item2.Tag?.ToString() == text2)
						{
							item2.IsSelected = true;
							break;
						}
					}
				}
				if (rootElement.TryGetProperty("FileNamePattern", out var value8))
				{
					txtFileNamePattern.Text = value8.GetString();
					if (txtFileNamePattern.Text.Contains("%AD%ad") && !txtFileNamePattern.Text.Contains("%AD%adet"))
					{
						txtFileNamePattern.Text = txtFileNamePattern.Text.Replace("%AD%ad", "%AD%adet");
					}
				}
				if (rootElement.TryGetProperty("CustomPropertyKey", out var value9))
				{
					txtCustomPropertyKey.Text = value9.GetString();
				}
				if (rootElement.TryGetProperty("CreateNotch", out var value10))
				{
					chkEnableNotch.IsChecked = value10.GetBoolean();
				}
				if (rootElement.TryGetProperty("NotchDiameterMode", out var value11))
				{
					bool flag = string.Equals(value11.GetString(), "Fixed", StringComparison.OrdinalIgnoreCase);
					rdoNotchCustom.IsChecked = flag;
					rdoNotchAuto.IsChecked = !flag;
				}
				if (rootElement.TryGetProperty("ExportDXF", out var value12))
				{
					chkExportDXF.IsChecked = value12.GetBoolean();
				}
				if (rootElement.TryGetProperty("ExportDWG", out var value13))
				{
					chkExportDWG.IsChecked = value13.GetBoolean();
				}
				if (rootElement.TryGetProperty("HideBendLines", out var value14))
				{
					chkHideBendLines.IsChecked = value14.GetBoolean();
				}
				if (rootElement.TryGetProperty("ProcessAllConfigurations", out var value15))
				{
					chkAllConfigs.IsChecked = value15.GetBoolean();
				}
				if (rootElement.TryGetProperty("AppendThicknessQtyToName", out var value16) && value16.GetBoolean())
				{
					if (!txtFileNamePattern.Text.Contains("%KA%"))
					{
						txtFileNamePattern.Text += "_%KA%";
					}
					if (!txtFileNamePattern.Text.Contains("%AD%"))
					{
						txtFileNamePattern.Text += "_%AD%adet";
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log("Ayarlar yüklenemedi: " + ex.Message);
		}
		SyncNameOptionsFromPattern();
		UpdateNamePreview();
		txtNotchDiameter.IsEnabled = rdoNotchCustom.IsChecked == true;
		_loading = false;
	}

	private void SaveSettings()
	{
		try
		{
			string contents = JsonSerializer.Serialize(new Dictionary<string, object>
			{
				["FolderPath"] = txtFolderPath.Text,
				["NotchDiameterMm"] = txtNotchDiameter.Text,
				["NotchDiameterMode"] = ((rdoNotchCustom.IsChecked == true) ? "Fixed" : "Auto"),
				["UseDrawingTemplate"] = chkUseDrawingTemplate.IsChecked == true,
				["TemplatePath"] = txtTemplatePath.Text,
				["FolderGrouping"] = ((ComboBoxItem)cmbFolderGrouping.SelectedItem)?.Tag?.ToString() ?? "None",
				["OutputPlacement"] = ((ComboBoxItem)cmbOutputPlacement.SelectedItem)?.Tag?.ToString() ?? "OwnFolders",
				["FileNamePattern"] = txtFileNamePattern.Text,
				["CustomPropertyKey"] = txtCustomPropertyKey.Text,
				["CreateNotch"] = chkEnableNotch.IsChecked == true,
				["ExportDXF"] = chkExportDXF.IsChecked == true,
				["ExportDWG"] = chkExportDWG.IsChecked == true,
				["HideBendLines"] = chkHideBendLines.IsChecked == true,
				["ProcessAllConfigurations"] = chkAllConfigs.IsChecked == true,
				["AppendThicknessQtyToName"] = false
			});
			File.WriteAllText(_settingsFilePath, contents);
		}
		catch (Exception ex)
		{
			Log("Ayarlar kaydedilemedi: " + ex.Message);
		}
	}

	private void ScanFolder()
	{
		try
		{
			Scanner scanner = new Scanner();
			_allFiles = scanner.GetModelFilesFromFolder(txtFolderPath.Text);
			Log($"Tarama: {_allFiles.Count} dosya bulundu (.SLDPRT/.SLDASM). İşlenecekleri listeden seçebilirsiniz.");
		}
		catch (Exception ex)
		{
			_allFiles = new List<string>();
			Log("Tarama hatası: " + ex.Message);
		}
		ApplyFileFilter();
	}

	private void ApplyFileFilter()
	{
		if (itemsFiles == null || txtFileCount == null)
		{
			return;
		}
		string text = ((ComboBoxItem)(cmbFileFilter?.SelectedItem))?.Tag?.ToString() ?? "All";
		_fileItems.Clear();
		foreach (string allFile in _allFiles)
		{
			bool flag = allFile.ToLower().EndsWith(".sldasm");
			if (!((text == "Part") & flag) && (!(text == "Assembly") || flag))
			{
				_fileItems.Add(new FileListItem
				{
					IsSelected = true,
					FullPath = allFile
				});
			}
		}
		string value;
		if (text == "Part")
		{
			value = "parça";
		}
		else
		{
			value = ((text == "Assembly") ? "montaj" : "tümü");
		}
		txtFileCount.Text = $"{_fileItems.Count}/{_allFiles.Count} dosya (filtre: {value})";
	}

	private void cmbFileFilter_Changed(object sender, SelectionChangedEventArgs e)
	{
		ApplyFileFilter();
	}

	private void btnBrowse_Click(object sender, RoutedEventArgs e)
	{
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = "Lütfen Sac Parçaların Bulunduğu Klasörü Seçin"
		};
		if (openFolderDialog.ShowDialog() == true)
		{
			txtFolderPath.Text = openFolderDialog.FolderName;
			Log("Klasör seçildi: " + txtFolderPath.Text);
			ScanFolder();
		}
	}

	private void btnRescan_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(txtFolderPath.Text) || !Directory.Exists(txtFolderPath.Text))
		{
			MessageBox.Show("Önce geçerli bir klasör seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
		else
		{
			ScanFolder();
		}
	}

	private void btnSelectAll_Click(object sender, RoutedEventArgs e)
	{
		foreach (FileListItem fileItem in _fileItems)
		{
			fileItem.IsSelected = true;
		}
	}

	private void btnSelectNone_Click(object sender, RoutedEventArgs e)
	{
		foreach (FileListItem fileItem in _fileItems)
		{
			fileItem.IsSelected = false;
		}
	}

	private void chkUseDrawingTemplate_Click(object sender, RoutedEventArgs e)
	{
		bool valueOrDefault = chkUseDrawingTemplate.IsChecked == true;
		txtTemplatePath.IsEnabled = valueOrDefault;
		btnBrowseTemplate.IsEnabled = valueOrDefault;
	}

	private void NameOption_Changed(object sender, RoutedEventArgs e)
	{
		if (!_loading)
		{
			RebuildPatternFromNameOptions();
		}
	}

	private void txtFileNamePattern_TextChanged(object sender, TextChangedEventArgs e)
	{
		UpdateNamePreview();
	}

	private void RebuildPatternFromNameOptions()
	{
		if (txtFileNamePattern != null)
		{
			bool flag = (txtFileNamePattern.Text ?? "").Contains("%O1%");
			string text = "%DA%";
			if (chkNameMaterial.IsChecked == true)
			{
				text += "_%MA%";
			}
			if (chkNameThickness.IsChecked == true)
			{
				text += "_%KA%";
			}
			if (chkNameQty.IsChecked == true)
			{
				text += "_%AD%adet";
			}
			if (chkNameConfig.IsChecked == true)
			{
				text += "_%KO%";
			}
			if (flag)
			{
				text += "_%O1%";
			}
			txtFileNamePattern.Text = text;
		}
	}

	private void SyncNameOptionsFromPattern()
	{
		string text = txtFileNamePattern.Text ?? "";
		chkNameMaterial.IsChecked = text.Contains("%MA%");
		chkNameThickness.IsChecked = text.Contains("%KA%");
		chkNameQty.IsChecked = text.Contains("%AD%");
		chkNameConfig.IsChecked = text.Contains("%KO%");
	}

	private void UpdateNamePreview()
	{
		if (txtNamePreview != null)
		{
			string text = (txtFileNamePattern.Text ?? "").Replace("%DA%", "Bacak").Replace("%MA%", "S235").Replace("%KA%", "4mm")
				.Replace("%AD%", "4")
				.Replace("%KO%", "Konfig1")
				.Replace("%O1%", "Ozellik");
			string text2;
			if (chkExportDXF.IsChecked == true)
			{
				text2 = ".dxf";
			}
			else
			{
				text2 = ((chkExportDWG.IsChecked == true) ? ".dwg" : "");
			}
			txtNamePreview.Text = "Örnek çıktı: " + text + text2;
		}
	}

	private void btnBrowseTemplate_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Lütfen SolidWorks Teknik Resim Şablonunu Seçin (.drwdot)",
			Filter = "SolidWorks Teknik Resim Şablonu (*.drwdot)|*.drwdot|Tüm Dosyalar (*.*)|*.*"
		};
		if (openFileDialog.ShowDialog() == true)
		{
			txtTemplatePath.Text = openFileDialog.FileName;
		}
	}

	private void NotchMode_Changed(object sender, RoutedEventArgs e)
	{
		if (txtNotchDiameter != null)
		{
			txtNotchDiameter.IsEnabled = rdoNotchCustom.IsChecked == true;
		}
	}

	private async void btnStart_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(txtFolderPath.Text) || !Directory.Exists(txtFolderPath.Text))
		{
			MessageBox.Show("Lütfen geçerli bir klasör seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		List<string> selectedFiles = (from i in _fileItems
			where i.IsSelected
			select i.FullPath).ToList();
		if (selectedFiles.Count == 0)
		{
			MessageBox.Show("Listeden en az bir dosya seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		if (!double.TryParse(txtNotchDiameter.Text.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || result <= 0.0)
		{
			if (rdoNotchCustom.IsChecked == true)
			{
				MessageBox.Show("Lütfen 0'dan büyük geçerli bir çentik ÇAPI girin (örneğin 4.0).", "Hata", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}
			result = 4.0;
		}
		SaveSettings();
		btnStart.IsEnabled = false;
		Log($"İşlem başlatılıyor... ({selectedFiles.Count} dosya seçili)");
		try
		{
			IProgress<string> progress = new Progress<string>((string msg) =>
			{
				Log(msg);
			});
			string folderPath = txtFolderPath.Text;
			ProcessingSettings settings = new ProcessingSettings
			{
				NotchDiameterMm = result,
				NotchDiameterMode = ((rdoNotchCustom.IsChecked == true) ? "Fixed" : "Auto"),
				ExportDWG = (chkExportDWG.IsChecked == true),
				ExportDXF = (chkExportDXF.IsChecked == true),
				CreateNotch = (chkEnableNotch.IsChecked == true),
				HideBendLines = (chkHideBendLines.IsChecked == true),
				UseDrawingTemplate = (chkUseDrawingTemplate.IsChecked == true),
				DrawingTemplatePath = txtTemplatePath.Text,
				ProcessAllConfigurations = (chkAllConfigs.IsChecked == true),
				AppendThicknessQtyToName = false,
				FolderGrouping = (((ComboBoxItem)cmbFolderGrouping.SelectedItem)?.Tag?.ToString() ?? "None"),
				OutputPlacement = (((ComboBoxItem)cmbOutputPlacement.SelectedItem)?.Tag?.ToString() ?? "OwnFolders"),
				FileNamePattern = txtFileNamePattern.Text,
				CustomPropertyKey = txtCustomPropertyKey.Text
			};
			await Task.Run(() =>
			{
				progress.Report("SolidWorks'e bağlanılıyor...");
				try
				{
					using SolidWorksEngine solidWorksEngine = new SolidWorksEngine();
					progress.Report("SolidWorks bağlantısı başarılı. Modüller yükleniyor...");
					new BatchProcessor(solidWorksEngine.App, settings).ProcessFiles(selectedFiles, folderPath, progress);
				}
				catch (Exception ex2)
				{
					progress.Report("SolidWorks Hatası: " + ex2.Message);
				}
			});
		}
		catch (Exception ex)
		{
			Log("Hata oluştu: " + ex.Message);
		}
		finally
		{
			btnStart.IsEnabled = true;
		}
	}

	private void Log(string message)
	{
		_logger?.Write(message);
		Dispatcher.Invoke(() =>
		{
			txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
			txtLog.ScrollToEnd();
		});
	}
}
