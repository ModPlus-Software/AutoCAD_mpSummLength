using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Autodesk.AutoCAD.Runtime;
using ModPlus;
using mpMsg;
using mpSettings;

namespace mpSummLength
{
    /// <summary>
    /// Логика взаимодействия для MpSummLength.xaml
    /// </summary>
    public partial class MpSummLength
    {
        public XElement El;
        public double SumResult;

        public MpSummLength()
        {
            InitializeComponent();
            MpWindowHelpers.OnWindowStartUp(
                this,
                MpSettings.GetValue("Settings", "MainSet", "Theme"),
                MpSettings.GetValue("Settings", "MainSet", "AccentColor"),
                MpSettings.GetValue("Settings", "MainSet", "BordersType")
                );
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }
        private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }
        // Курсор попал на окно
        void MetroWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
        }
        // Курсор попал вне окна
        void MetroWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Focus();
        }
        // Экспорт в текстовый документ
        private void BtExportToTxt_Click(object sender, RoutedEventArgs e)
        {
            if (El != null)
            {
                var text = string.Empty;
                text += "Общая длина: " + this.TbSumLen.Text + Environment.NewLine;
                foreach (var entXml in El.Elements("Entity"))
                {
                    text += entXml.Attribute("name").Value + "\r";
                    text += "Количество: " + entXml.Attribute("count").Value + "\r";
                    text += "Общая длина: " + entXml.Attribute("lens").Value;
                    text += Environment.NewLine;
                }
                ShowTextWithNotepad(text);
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Загрузка сохраненного значения округления (с учетом, что может и не быть)
            try
            {
                this.SliderResultRound.Value = double.Parse(MpSettings.GetValue("Settings", "SummLength", "Round"));
            }
            catch { }
            // Привязываем данные
            if (El != null)
            {
                this.LbResult.ItemsSource = El.Elements("Entity");
                SetSumm();
            }
        }

        private void SetSumm()
        {
            try
            {
                // Результат
                var sum = Math.Round(SumResult, int.Parse(this.SliderResultRound.Value.ToString(CultureInfo.InvariantCulture)));
                this.TbSumLen.Text = sum.ToString(CultureInfo.InvariantCulture);
            }
            catch (System.Exception ex)
            {
                MpExWin.Show(ex);
            }
        }
        // Выбор в разделе Объекты
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ListBox)sender).SelectedIndex != -1)
            {
                try
                {
                    var doc = AcApp.DocumentManager.MdiActiveDocument;
                    var db = doc.Database;
                    var selXel = e.AddedItems[0] as XElement;
                    using (doc.LockDocument())
                    {
                        using (var tr = doc.TransactionManager.StartTransaction())
                        {
                            if (selXel != null)
                            {
                                var xAttribute = selXel.Attribute("obj");
                                if (xAttribute != null)
                                    MpCadHelpers.ZoomToEntity(new[] { GetObjectId(db, xAttribute.Value) });
                            }
                            tr.Commit();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    MpExWin.Show(ex);
                }
            }
        }
        public static ObjectId GetObjectId(Database db, string handle)
        {
            var h = new Handle(Int64.Parse(handle, NumberStyles.AllowHexSpecifier));
            var id = ObjectId.Null;
            db.TryGetObjectId(h, out id);//TryGetObjectId﻿ method
            return id;
        }
        /// <summary>
        /// Показать текст в блокноте (без сохранения на диск)
        /// </summary>
        /// <param name="text">Отображамый текст (должен быть со всеми управляющими символами типа \n, \r)</param>
        private static void ShowTextWithNotepad(string text)
        {
            var process = new Process { StartInfo = { FileName = @"notepad.exe" }, EnableRaisingEvents = true };
            process.Start(); // It will start Notepad process
            process.WaitForInputIdle(10000);
            if (process.Responding) // If currently started process(notepad) is responding
            {
                System.Windows.Forms.SendKeys.SendWait(text);
                // It will Add all the text from text variable to notepad 
            }
        }

        private void SliderResultRound_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetSumm();
        }

        private void MpSummLength_OnClosed(object sender, EventArgs e)
        {
            // Сохраняем значение округления
            MpSettings.SetValue("Settings", "SummLength", "Round", this.SliderResultRound.Value.ToString(CultureInfo.InvariantCulture), true);
        }
        // Вставить результат в ячейку таблицы
        private void BtAddToTable_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();
                MpCadHelpers.InsertToAutoCad.AddStrToAutoCadTableCell(this.TbSumLen.Text, string.Empty, true);
            }
            catch { }
            finally { this.Show();}
        }
        // Вставить результат в виде однострочного текста
        private void BtAddAsDbText_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();
                MpCadHelpers.InsertToAutoCad.InsertDbText(this.TbSumLen.Text.Replace(',', '.').Replace('.', Convert.ToChar(MpVars.MpSeparator)));
            }
            catch { }
            finally { this.Show(); }
        }
    }

    public class MpSummLenghtFunction
    {
        MpSummLength _resWin;

        [CommandMethod("ModPlus", "mpSummLength", CommandFlags.UsePickSet)]
        public void StartFunction()
        {
            double sumLen;
            List<string> entities;
            List<int> count;
            List<double> lens;
            List<List<ObjectId>> objIds;
            MpCadHelpers.GetFromAutoCad.GetLenFromEntities(out sumLen, out entities, out count, out lens, out objIds);
            if (sumLen != 0.0)
            {
                var xEl = new XElement("MpSummLenght");
                xEl.SetAttributeValue("sum", sumLen);
                for (var i = 0; i < entities.Count; i++)
                {
                    if (count[i] != 0)
                    {
                        var entXml = new XElement("Entity");
                        entXml.SetAttributeValue("name", ObjNameRus(entities[i]));
                        entXml.SetAttributeValue("count", count[i]);
                        entXml.SetAttributeValue("lens", lens[i]);
                        foreach (var objId in objIds[i])
                        {
                            var objXml = new XElement("obj");
                            objXml.SetAttributeValue("obj", objId.Handle);
                            entXml.Add(objXml);
                        }
                        xEl.Add(entXml);
                    }
                }
                if (_resWin == null)
                {
                    _resWin = new MpSummLength { El = xEl, SumResult = sumLen };
                    _resWin.Closed += resWin_Closed;
                }
                if (_resWin.IsLoaded)
                    _resWin.Activate();
                else
                    AcApp.ShowModelessWindow(Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle, _resWin);
            }
        }

        void resWin_Closed(object sender, EventArgs e)
        {
            _resWin = null;
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
        }

        private string ObjNameRus(string objName)
        {
            try
            {
                var _entities = new List<string> {"Отрезок", "Окружность", "Полилиния", "Дуга", "Сплайн", "Эллипс"};
                var entities = new List<string> {"Line", "Circle", "Polyline", "Arc", "Spline", "Ellipse"};
                return _entities[entities.IndexOf(objName)];
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
