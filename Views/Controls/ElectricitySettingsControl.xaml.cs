using cpu_net.Model;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace cpu_net.Views.Controls
{
    public partial class ElectricitySettingsControl : UserControl
    {
        public ElectricitySettingsControl()
        {
            InitializeComponent();
            StudentNoTextBox.PreviewTextInput += NumberOnly_PreviewTextInput;
            ThresholdTextBox.PreviewTextInput += DecimalOnly_PreviewTextInput;

            // 初始化时间下拉框
            for (int h = 0; h < 24; h++)
                CheckHourComboBox.Items.Add($"{h:D2}");
            for (int m = 0; m < 60; m += 5)
                CheckMinuteComboBox.Items.Add($"{m:D2}");
        }

        public void LoadSettings(SettingModel setting)
        {
            EnabledCheckBox.IsChecked = setting.ElectricityEnabled;
            StudentNoTextBox.Text = setting.ElectricityStudentNo;
            ThresholdTextBox.Text = setting.ElectricityThreshold.ToString("F2");
            ThresholdModeComboBox.SelectedIndex = setting.ElectricityThresholdMode switch
            {
                0 => 0,
                1 => 1,
                _ => 0
            };
            CheckHourComboBox.SelectedIndex = setting.ElectricityCheckHour >= 0 && setting.ElectricityCheckHour < 24
                ? setting.ElectricityCheckHour : 8;
            CheckMinuteComboBox.SelectedIndex = setting.ElectricityCheckMinute >= 0 && setting.ElectricityCheckMinute < 60
                ? setting.ElectricityCheckMinute / 5 : 0;
            NotifyTypeComboBox.SelectedIndex = setting.NotifyType switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                _ => 0
            };
        }

        public void SaveSettings(SettingModel setting)
        {
            setting.ElectricityEnabled = EnabledCheckBox.IsChecked ?? false;
            setting.ElectricityStudentNo = StudentNoTextBox.Text.Trim();
            setting.ElectricityThreshold = decimal.TryParse(ThresholdTextBox.Text, out var t) ? t : 10m;
            setting.ElectricityThresholdMode = ThresholdModeComboBox.SelectedIndex switch
            {
                0 => 0,
                1 => 1,
                _ => 0
            };
            setting.ElectricityCheckHour = CheckHourComboBox.SelectedIndex >= 0 ? CheckHourComboBox.SelectedIndex : 8;
            setting.ElectricityCheckMinute = CheckMinuteComboBox.SelectedIndex >= 0 ? CheckMinuteComboBox.SelectedIndex * 5 : 0;
            setting.NotifyType = NotifyTypeComboBox.SelectedIndex switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                _ => 0
            };
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        private void DecimalOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9.]+");
        }
    }
}
