using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectZetaTeam.Views
{
    public partial class MainView : UserControl
    {
        private readonly string[] _supportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
        public MainView()
        {
            InitializeComponent();
        }

        private void ThemeToggleButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var currentTheme = Application.Current?.ActualThemeVariant;

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = currentTheme == ThemeVariant.Dark
                    ? ThemeVariant.Light
                    : ThemeVariant.Dark;
            }
        }

        private async void SelectFileButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                string? selectedImagePath = await OpenImageFileDialogAsync();
                if (!string.IsNullOrEmpty(selectedImagePath))
                {
                    await LoadAndDisplayImageAsync(selectedImagePath);
                }
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Ошибка выбора файла: {ex.Message}";
            }
        }

        private async Task<string?> OpenImageFileDialogAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);

            if (topLevel == null) return null;

            var storageProvider = topLevel.StorageProvider;

            var options = new FilePickerOpenOptions
            {
                Title = "Выберите изображение",
                AllowMultiple = false,
                FileTypeFilter = new FilePickerFileType[]
                {
                new FilePickerFileType("Изображения (*.jpg, *.png, *.webp)")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.webp" },
                    MimeTypes = new[] { "image/jpeg", "image/png", "image/bmp", "image/gif", "image/webp" }
                },
                FilePickerFileTypes.All
                }
            };

            IReadOnlyList<IStorageFile> result = await storageProvider.OpenFilePickerAsync(options);

            if (result.Count > 0)
            {
                return result[0].Path.LocalPath;
            }

            return null;
        }

        private async void OnVisualElementDragOver(object? sender, DragEventArgs e)
        {
            if (e.DataTransfer.Contains(DataFormat.File))
                e.DragEffects = DragDropEffects.Copy;
            else
                e.DragEffects = DragDropEffects.None;
        }

        private async Task LoadAndDisplayImageAsync(string filePath)
        {
            if (SelectedImage.Source is IDisposable oldBitmap)
                oldBitmap.Dispose();

            try
            {
                byte[] imageData;
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    imageData = new byte[fileStream.Length];
                    await fileStream.ReadAsync(imageData, 0, imageData.Length);
                }

                using var memoryStream = new MemoryStream(imageData);
                var bitmap = new Bitmap(memoryStream);
                SelectedImage.Source = bitmap;
                _currentInputFilePath = filePath;
                SelectedFileText.Text = filePath;
                MessageTextBlock.Text = "";
                TextMessage.Text = "";
                MessageTextBlock.Text = "";
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Ошибка загрузки: {ex.Message}";
                SelectedImage.Source = null;
                _currentInputFilePath = null;
                SelectedFileText.Text = "";
            }
        }

        private async void OnVisualElementDrop(object? sender, DragEventArgs e)
        {
            try
            {
                if (e.DataTransfer.Contains(DataFormat.File))
                {
                    var files = e.DataTransfer.TryGetFiles();
                    if (files != null && files.Any())
                    {
                        var firstFile = files.First();
                        string localPath = firstFile.Path.LocalPath;
                        string extension = Path.GetExtension(localPath).ToLower();
                        if (_supportedExtensions.Contains(extension))
                        {
                            await LoadAndDisplayImageAsync(localPath);
                        }
                        else
                        {
                            MessageTextBlock.Text = "Ошибка: Данный формат файла не поддерживается.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Ошибка перетаскивания: {ex.Message}";
            }
        }

        private string? _currentInputFilePath;

        private async void OnEncryptAndSaveButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentInputFilePath))
                {
                    MessageTextBlock.Text = "Ошибка: Сначала выберите исходное изображение!";
                    return;
                }

                string secretText = TextMessage.Text ?? "";
                if (string.IsNullOrWhiteSpace(secretText))
                {
                    MessageTextBlock.Text = "Ошибка: Введите текст для сокрытия.";
                    return;
                }

                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var saveOptions = new FilePickerSaveOptions
                {
                    Title = "Сохранить изображение",
                    DefaultExtension = "png",
                    SuggestedFileName = $"ZetaTeam_{Guid.NewGuid().ToString().Substring(0, 8)}.png",
                    FileTypeChoices = new FilePickerFileType[]
                    {
                        new FilePickerFileType("Изображение PNG (*.png)") { Patterns = new[] { "*.png" } }
                    }
                };

                var targetFile = await topLevel.StorageProvider.SaveFilePickerAsync(saveOptions);
                if (targetFile != null)
                {
                    string outputPath = targetFile.Path.LocalPath;
                    await Task.Run(() => LsbSteganography.HideText(_currentInputFilePath, outputPath, secretText));

                    if (SelectedImage.Source is IDisposable bitmap)
                        bitmap.Dispose();
                    SelectedImage.Source = null;
                    _currentInputFilePath = null;
                    SelectedFileText.Text = "";
                    TextMessage.Text = "";
                    MessageTextBlock.Text = "Изображение успешно сохранено!";
                }
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Ошибка LSB: {ex.Message}";
            }
        }

        private async void OnDecryptButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentInputFilePath))
                {
                    MessageTextBlock.Text = "Ошибка: Сначала перетащите или выберите зашифрованный файл!";
                    return;
                }

                string hiddenMessage = await Task.Run(() => LsbSteganography.ExtractText(_currentInputFilePath));

                if (!string.IsNullOrEmpty(hiddenMessage))
                {
                    TextMessage.Text = hiddenMessage;
                    MessageTextBlock.Text = "Сообщение успешно извлечено.";
                }
                else
                {
                    MessageTextBlock.Text = "Сообщений не найдено.";
                }
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Ошибка дешифровки: {ex.Message}";
            }
        }
    }
}