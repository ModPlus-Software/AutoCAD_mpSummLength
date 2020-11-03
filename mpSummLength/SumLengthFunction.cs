namespace mpSummLength
{
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    public class SumLengthFunction
    {
        private MainWindow _resWin;
        private MainViewModel _resWinDataContext;

        [CommandMethod("ModPlus", "mpSummLength", CommandFlags.UsePickSet)]
        public void StartFunction()
        {
#if !DEBUG
            Statistic.SendCommandStarting(new ModPlusConnector());
#endif

            try
            {
                if (_resWinDataContext == null)
                    _resWinDataContext = new MainViewModel();
                var selectedEntities = SelectEntities(_resWinDataContext).ToList();

                if (selectedEntities.Any())
                {
                    _resWinDataContext.AddEntities(selectedEntities);
                    if (_resWin == null)
                    {
                        _resWin = new MainWindow();
                        _resWinDataContext.MainWindow = _resWin;
                        _resWin.DataContext = _resWinDataContext;
                        _resWin.Closed += (sender, args) =>
                        {
                            _resWin = null;
                            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
                        };
                    }

                    if (_resWin.IsLoaded)
                        _resWin.Activate();
                    else
                        AcApp.ShowModelessWindow(AcApp.MainWindow.Handle, _resWin);
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private static IEnumerable<EntityInfo> SelectEntities(MainViewModel viewModel)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var selRes = ed.SelectImplied();

            // Если сначала ничего не выбрано, просим выбрать:
            if (selRes.Status == PromptStatus.Error)
            {
                var selOpts = new PromptSelectionOptions
                {
                    MessageForAdding = $"\n{Language.GetItem("AutocadDlls", "msg1")}"
                };
                TypedValue[] values =
                {
                        new TypedValue((int)DxfCode.Operator, "<OR"),
                        new TypedValue((int)DxfCode.Start, "LINE"),
                        new TypedValue((int)DxfCode.Start, "POLYLINE"),
                        new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                        new TypedValue((int)DxfCode.Start, "CIRCLE"),
                        new TypedValue((int)DxfCode.Start, "ARC"),
                        new TypedValue((int)DxfCode.Start, "SPLINE"),
                        new TypedValue((int)DxfCode.Start, "ELLIPSE"),
                        new TypedValue((int)DxfCode.Operator, "OR>")
                };
                var selectionFilter = new SelectionFilter(values);
                selRes = ed.GetSelection(selOpts, selectionFilter);
            }

            if (selRes.Status == PromptStatus.OK)
            {
                using (var tr = doc.TransactionManager.StartTransaction())
                {
                    var objIds = selRes.Value.GetObjectIds();
                    foreach (var objId in objIds)
                    {
                        var ent = (Entity)tr.GetObject(objId, OpenMode.ForRead);
                        using (ent)
                        {
                            yield return new EntityInfo(viewModel, ent);
                        }
                    }

                    tr.Commit();
                }
            }
        }
    }
}