using cpu_net.Model;
using cpu_net.Services;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace cpu_net.Views.Controls
{
    public partial class BackgroundSettingsControl : UserControl
    {
        public BackgroundSettingsControl()
        {
            InitializeComponent();
        }

        public void LoadSettings(SettingModel setting)
        {
            BgPathTextBox.Text = !string.IsNullOrWhiteSpace(setting.BackgroundImagePath) && File.Exists(setting.BackgroundImagePath)
                ? setting.BackgroundImagePath : string.Empty;
            IconPathTextBox.Text = !string.IsNullOrWhiteSpace(setting.CustomIconPath) && File.Exists(setting.CustomIconPath)
                ? setting.CustomIconPath : string.Empty;
            OpacitySlider.Value = (int)(setting.BackgroundOpacity * 100);
            TextBoxOpacitySlider.Value = (int)(setting.TextBoxOpacity * 100);
            UpdatePreview();
        }

        public void SaveSettings(SettingModel setting)
        {
            setting.BackgroundImagePath = BgPathTextBox.Text.Trim();
            setting.CustomIconPath = IconPathTextBox.Text.Trim();
            setting.BackgroundOpacity = OpacitySlider.Value / 100.0;
            setting.TextBoxOpacity = TextBoxOpacitySlider.Value / 100.0;
        }

        private void BgBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择背景图片",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|所有文件|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                string? copiedPath = ImageProcessingService.CopyToAppDirectory(dialog.FileName, "bg_" + Path.GetFileName(dialog.FileName));
                if (copiedPath != null)
                {
                    BgPathTextBox.Text = copiedPath;
                    UpdatePreview();
                }
            }
        }

        private void IconBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择软件图标",
                Filter = "图标文件|*.ico;*.png;*.jpg;*.jpeg|所有文件|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                string? copiedPath = ImageProcessingService.CopyToAppDirectory(dialog.FileName, "icon_" + Path.GetFileName(dialog.FileName));
                if (copiedPath != null)
                {
                    IconPathTextBox.Text = copiedPath;
                }
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 防止 XAML 初始化阶段控件尚未创建时触发空引用
            if (OpacityValueText != null)
                OpacityValueText.Text = $"{(int)OpacitySlider.Value}%";
            UpdatePreview();
        }

        private void TextBoxOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextBoxOpacityValueText != null)
                TextBoxOpacityValueText.Text = $"{(int)TextBoxOpacitySlider.Value}%";
        }

        private void UpdatePreview()
        {
            if (PreviewImage == null) return;

            var bitmap = ImageProcessingService.LoadImage(BgPathTextBox.Text);
            if (bitmap != null)
            {
                PreviewImage.Source = bitmap;
                PreviewImage.Opacity = OpacitySlider.Value / 100.0;
            }
            else
            {
                PreviewImage.Source = null;
            }
        }
    }
}
