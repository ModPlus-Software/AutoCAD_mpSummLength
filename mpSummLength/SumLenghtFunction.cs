namespace mpSummLength
{
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    public class SumLengthFunction
    {
        MainWindow _resWin;
        private const string LangItem = "mpSummLength";

        [CommandMethod("ModPlus", "mpSummLength", CommandFlags.UsePickSet)]
        public void StartFunction()
        {
            Statistic.SendCommandStarting(new ModPlusConnector());

            var viewModel = new MainViewModel();
            SelectEntities(viewModel);
            if (viewModel.HasAnyEntityInCollections)
            {
                if (_resWin == null)
                {
                    _resWin = new MainWindow();
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

        private static void SelectEntities(MainViewModel viewModel)
        {
            var entities = new List<string> { "Line", "Circle", "Polyline", "Arc", "Spline", "Ellipse" };
            entities.ForEach(e => viewModel.EntitiesCollections.Add(new EntitiesCollection(e)));
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            try
            {
                var selRes = ed.SelectImplied();
                // Если сначала ничего не выбрано, просим выбрать:
                if (selRes.Status == PromptStatus.Error)
                {
                    var selOpts = new PromptSelectionOptions
                    {
                        MessageForAdding = "\n" + Language.GetItem("AutocadDlls", "msg1")
                    };
                    TypedValue[] values =
                    {
                        new TypedValue((int) DxfCode.Operator, "<OR"),
                        new TypedValue((int) DxfCode.Start, "LINE"),
                        new TypedValue((int) DxfCode.Start, "POLYLINE"),
                        new TypedValue((int) DxfCode.Start, "LWPOLYLINE"),
                        new TypedValue((int) DxfCode.Start, "CIRCLE"),
                        new TypedValue((int) DxfCode.Start, "ARC"),
                        new TypedValue((int) DxfCode.Start, "SPLINE"),
                        new TypedValue((int) DxfCode.Start, "ELLIPSE"),
                        new TypedValue((int) DxfCode.Operator, "OR>")
                    };
                    var selectionFilter = new SelectionFilter(values);
                    selRes = ed.GetSelection(selOpts, selectionFilter);

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
                                    viewModel.EntitiesCollections
                                        .FirstOrDefault(c => c.IsAllowableEntity(ent))?
                                        .Entities.Add(new EntityInfo(ent));
                                }
                            }

                            tr.Commit();
                        }
                    }
                }
                else ed.SetImpliedSelection(new ObjectId[0]);
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }
    }
}