using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using ModPlusAPI;
using ModPlusAPI.Windows;

namespace mpSummLength
{
    public partial class MpSummLength
    {
        private const string LangItem = "mpSummLength";
        public XElement El;
        public double SumResult;

        public MpSummLength()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem(LangItem, "h1");
        }
        
        // Курсор попал на окно
        void MetroWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
        }
        // Курсор попал вне окна
        void MetroWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            Focus();
        }
        // Экспорт в текстовый документ
        private void BtExportToTxt_Click(object sender, RoutedEventArgs e)
        {
            if (El != null)
            {
                var text = string.Empty;
                text += ModPlusAPI.Language.GetItem(LangItem, "h2") + " " + TbSumLen.Text + Environment.NewLine;
                foreach (var entXml in El.Elements("Entity"))
                {
                    text += entXml.Attribute("name").Value + "\r ";
                    text += ModPlusAPI.Language.GetItem(LangItem, "h4") + " " + entXml.Attribute("count").Value + "\r ";
                    text += ModPlusAPI.Language.GetItem(LangItem, "h2") + " " + entXml.Attribute("lens").Value;
                    text += Environment.NewLine;
                }
                ModPlusAPI.IO.String.ShowTextWithNotepad(text, ModPlusAPI.Language.GetItem(LangItem, "h1"));
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Загрузка сохраненного значения округления (с учетом, что может и не быть)
            try
            {
                SliderResultRound.Value =
                    double.Parse(UserConfigFile.GetValue(UserConfigFile.ConfigFileZone.Settings, "SummLength",
                        "Round"));
            }
            catch
            {
                // ignore
            }
            // Привязываем данные
            if (El != null)
            {
                LbResult.ItemsSource = El.Elements("Entity");
                SetSumm();
            }
        }

        private void SetSumm()
        {
            try
            {
                // Результат
                var sum = Math.Round(SumResult, int.Parse(SliderResultRound.Value.ToString(CultureInfo.InvariantCulture)));
                TbSumLen.Text = sum.ToString(CultureInfo.InvariantCulture);
            }
            catch (System.Exception ex)
            {
                ExceptionBox.Show(ex);
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
                            var xAttribute = selXel?.Attribute("obj");
                            if (xAttribute != null)
                                ModPlus.Helpers.AutocadHelpers.ZoomToEntities(new[] { GetObjectId(db, xAttribute.Value) });
                            tr.Commit();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ExceptionBox.Show(ex);
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

        private void SliderResultRound_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetSumm();
        }

        private void MpSummLength_OnClosed(object sender, EventArgs e)
        {
            // Сохраняем значение округления
            UserConfigFile.SetValue(UserConfigFile.ConfigFileZone.Settings, "SummLength", "Round", SliderResultRound.Value.ToString(CultureInfo.InvariantCulture), true);
        }
        // Вставить результат в ячейку таблицы
        private void BtAddToTable_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide();
                ModPlus.Helpers.InsertToAutoCad.AddStringToAutoCadTableCell(TbSumLen.Text, string.Empty, true);
            }
            catch
            {
                // ignore
            }
            finally { Show();}
        }
        // Вставить результат в виде однострочного текста
        private void BtAddAsDbText_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Hide();
                ModPlus.Helpers.InsertToAutoCad.InsertDbText(TbSumLen.Text.Replace(',', '.')
                    .Replace('.', Convert.ToChar(ModPlusAPI.Variables.Separator)));
            }
            catch
            {
                // ignore
            }
            finally { Show(); }
        }

    }

    public class MpSummLenghtFunction
    {
        MpSummLength _resWin;
        private const string LangItem = "mpSummLength";

        [CommandMethod("ModPlus", "mpSummLength", CommandFlags.UsePickSet)]
        public void StartFunction()
        {
            Statistic.SendCommandStarting(new Interface());

            ModPlus.Helpers.GetFromAutoCAD.GetLenFromEntities(
                out double sumLen, 
                out List<string> entities, 
                out List<int> count, 
                out List<double> lens, 
                out List<List<ObjectId>> objIds);
            if (Math.Abs(sumLen) > 0.000001)
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
                var _entities = new List<string>
                {
                    Language.GetItem(LangItem, "h9"),
                    Language.GetItem(LangItem, "h10"),
                    Language.GetItem(LangItem, "h11"),
                    Language.GetItem(LangItem, "h12"),
                    Language.GetItem(LangItem, "h13"),
                    Language.GetItem(LangItem, "h14")
                };
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
