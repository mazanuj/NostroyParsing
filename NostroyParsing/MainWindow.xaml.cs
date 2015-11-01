using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Microsoft.Win32;
using NostroyParsing.Properties;
using NostroyParsingLib;
using NostroyParsingLib.DataTypes;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace NostroyParsing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static ObservableCollection<string> DataItemsLog { get; set; }

        public MainWindow()
        {
            DataContext = this;
            DataItemsLog = new ObservableCollection<string>();

            Informer.OnResultReceivedStr +=
                async result =>
                    await Application.Current.Dispatcher.BeginInvoke(
                        new Action(() => DataItemsLog.Insert(0, result)));

            InitializeComponent();
            //Height = SystemParameters.WorkArea.Height;
        }

        private void LaunchNostroyParsingOnGitHub(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/mazanuj/NostroyParsing/");
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonIsEnable(false);
            await Task.Run(async () =>
            {
                Initialize.DOP = Settings.Default.Threads;
                await Initialize.MainCycle();
            });
            ButtonIsEnable(true);
        }

        private async void ButtonXls_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonIsEnable(false);
            var sfd = new SaveFileDialog
            {
                Filter = "Excel 2007 (*.xlsx)|*.xlsx",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                RestoreDirectory = true,
            };

            if (sfd.ShowDialog() != false)
            {
                try
                {
                    using (var fs = new FileStream("MainCollection.xml", FileMode.OpenOrCreate))
                        QueueHelper.CollectionSaver =
                            ((MainCollection) new XmlSerializer(typeof (MainCollection)).Deserialize(fs)).MainTypeList;
                }
                catch (Exception)
                {
                    Informer.RaiseOnResultReceived("File 'MainCollection.xml' not exists or something going wrong");
                }
                await SaveInXls(QueueHelper.CollectionSaver, sfd.FileName);
                Informer.RaiseOnResultReceived($"Successfully exported {QueueHelper.CollectionSaver.Count} rows");
            }
            ButtonIsEnable(true);
        }

        private static async Task SaveInXls(IEnumerable<MainType> collection, string fileName)
        {
            await Task.Run(() =>
            {
                try
                {
                    var xssf = new XSSFWorkbook();
                    var sheet = xssf.CreateSheet();

                    AddRowsToXls(collection.Where(x => x != null), ref sheet);

                    using (var file = new FileStream(fileName, FileMode.Create))
                    {
                        xssf.Write(file);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Недостаточно оперативной памяти для обработки экспорта");
                }

            });
        }

        private static void AddRowsToXls(IEnumerable<MainType> list, ref ISheet sheet)
        {
            //Headers
            var rowIndex = 0;
            var row = sheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue("СРО");
            row.CreateCell(1).SetCellValue("Сокращённое наименование");
            row.CreateCell(2).SetCellValue("ИНН");
            row.CreateCell(3).SetCellValue("Дата вступления");
            row.CreateCell(4).SetCellValue("Дата исключения");
            row.CreateCell(5).SetCellValue("Номер телефона");
            row.CreateCell(6).SetCellValue("Старый номер телефона");
            row.CreateCell(7).SetCellValue("Должность");
            row.CreateCell(8).SetCellValue("ФИО");
            row.CreateCell(9).SetCellValue("Статус члена");
            row.CreateCell(10).SetCellValue("Адрес");
            rowIndex++;

            //Info
            foreach (var val in list)
            {
                row = sheet.CreateRow(rowIndex);
                row.CreateCell(0).SetCellValue(val.SRO);
                row.CreateCell(1).SetCellValue(val.ShortName);
                row.CreateCell(2).SetCellValue(val.INN);
                row.CreateCell(3).SetCellValue(val.RegDat);
                row.CreateCell(4).SetCellValue(val.ExDate);
                row.CreateCell(5).SetCellValue(val.Phone);
                row.CreateCell(6).SetCellValue(val.OldPhone);
                row.CreateCell(7).SetCellValue(val.Position);
                row.CreateCell(8).SetCellValue(val.FIO);
                row.CreateCell(9).SetCellValue(val.Status == Status.Member ? "Является членом" : "Исключен");
                row.CreateCell(10).SetCellValue(val.Address);
                rowIndex++;
            }
        }

        private void ButtonIsEnable(bool value)
        {
            ButtonStart.IsEnabled = value;
            ButtonXls.IsEnabled = value;
        }
    }
}