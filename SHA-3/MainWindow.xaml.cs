using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;

namespace SHA_3
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "SHA-3 Hash Generator";
            
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                
                TextHashPanel.Visibility = tag == "text" ? Visibility.Visible : Visibility.Collapsed;
                FileHashPanel.Visibility = tag == "file" ? Visibility.Visible : Visibility.Collapsed;
                VerifyHashPanel.Visibility = tag == "verify" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextResultPanel.Visibility = Visibility.Collapsed;
        }

        private async void GenerateTextHashButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var input = InputTextBox.Text;
                if (string.IsNullOrEmpty(input))
                {
                    await ShowContentDialog("Error", "Please enter text to hash.");
                    return;
                }

                var algorithm = GetSelectedAlgorithm(TextAlgorithmComboBox);
                var hash = SHA3Helper.ComputeHash(input, algorithm);
                
                TextHashResultBox.Text = hash;
                TextResultPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                await ShowContentDialog("Error", $"Failed to generate hash: {ex.Message}");
            }
        }

        private async void GenerateFileHashButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileHashProgressBar.Visibility = Visibility.Visible;
                GenerateFileHashButton.IsEnabled = false;
                FileResultPanel.Visibility = Visibility.Collapsed;

                var filePath = FilePathTextBox.Text;
                var algorithm = GetSelectedAlgorithm(FileAlgorithmComboBox);
                
                var fileInfo = new FileInfo(filePath);
                var hash = await Task.Run(() => SHA3Helper.ComputeFileHash(filePath, algorithm));
                
                FileHashResultBox.Text = hash;
                FileInfoBar.Message = $"File: {fileInfo.Name} | Size: {FormatFileSize(fileInfo.Length)}";
                FileResultPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                await ShowContentDialog("Error", $"Failed to generate hash: {ex.Message}");
            }
            finally
            {
                FileHashProgressBar.Visibility = Visibility.Collapsed;
                GenerateFileHashButton.IsEnabled = true;
            }
        }

        private async void VerifyHashButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VerifyProgressBar.Visibility = Visibility.Visible;
                VerifyHashButton.IsEnabled = false;
                VerifyResultInfoBar.Visibility = Visibility.Collapsed;

                var filePath = VerifyFilePathTextBox.Text;
                var expectedHash = ExpectedHashTextBox.Text.Trim().Replace(" ", "").Replace("-", "");
                var algorithm = GetSelectedAlgorithm(VerifyAlgorithmComboBox);
                
                var actualHash = await Task.Run(() => SHA3Helper.ComputeFileHash(filePath, algorithm));
                
                if (actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    VerifyResultInfoBar.Title = "? Hash Verified";
                    VerifyResultInfoBar.Message = "The file hash matches the expected value.";
                    VerifyResultInfoBar.Severity = InfoBarSeverity.Success;
                    VerifyResultInfoBar.IsOpen = true;
                }
                else
                {
                    VerifyResultInfoBar.Title = "? Hash Mismatch";
                    VerifyResultInfoBar.Message = $"Expected: {expectedHash}\nActual: {actualHash}";
                    VerifyResultInfoBar.Severity = InfoBarSeverity.Error;
                    VerifyResultInfoBar.IsOpen = true;
                }
                
                VerifyResultInfoBar.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                await ShowContentDialog("Error", $"Failed to verify hash: {ex.Message}");
            }
            finally
            {
                VerifyProgressBar.Visibility = Visibility.Collapsed;
                VerifyHashButton.IsEnabled = true;
            }
        }

        private async void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            var file = await PickFileAsync();
            if (file != null)
            {
                FilePathTextBox.Text = file.Path;
                GenerateFileHashButton.IsEnabled = true;
                FileResultPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void BrowseVerifyFileButton_Click(object sender, RoutedEventArgs e)
        {
            var file = await PickFileAsync();
            if (file != null)
            {
                VerifyFilePathTextBox.Text = file.Path;
                UpdateVerifyButtonState();
                VerifyResultInfoBar.Visibility = Visibility.Collapsed;
            }
        }

        private void ExpectedHashTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateVerifyButtonState();
            VerifyResultInfoBar.Visibility = Visibility.Collapsed;
        }

        private async Task<StorageFile?> PickFileAsync()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            
            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);
            
            return await picker.PickSingleFileAsync();
        }

        private async void CopyTextHash_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(TextHashResultBox.Text);
            await ShowContentDialog("Success", "Hash copied to clipboard!");
        }

        private async void CopyFileHash_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(FileHashResultBox.Text);
            await ShowContentDialog("Success", "Hash copied to clipboard!");
        }

        private void CopyToClipboard(string text)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }

        private void UpdateVerifyButtonState()
        {
            VerifyHashButton.IsEnabled = !string.IsNullOrEmpty(VerifyFilePathTextBox.Text) && 
                                         !string.IsNullOrEmpty(ExpectedHashTextBox.Text);
        }

        private SHA3Helper.SHA3Algorithm GetSelectedAlgorithm(ComboBox comboBox)
        {
            return comboBox.SelectedIndex switch
            {
                0 => SHA3Helper.SHA3Algorithm.SHA3_224,
                1 => SHA3Helper.SHA3Algorithm.SHA3_256,
                2 => SHA3Helper.SHA3Algorithm.SHA3_384,
                3 => SHA3Helper.SHA3Algorithm.SHA3_512,
                _ => SHA3Helper.SHA3Algorithm.SHA3_256
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }
            
            return $"{size:N2} {suffixes[suffixIndex]}";
        }

        private async Task ShowContentDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            
            await dialog.ShowAsync();
        }
    }
}
