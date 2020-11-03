namespace mpSummLength
{
    using System;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
    using System.Windows.Input;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.EditorInput;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;

    public class EntityInfo : VmBase
    {
        private readonly MainViewModel _mainViewModel;

        public EntityInfo(MainViewModel mainViewModel, Entity entity)
        {
            _mainViewModel = mainViewModel;
            EntName = entity.GetType().Name;
            ObjectId = entity.ObjectId;
            Length = GetLength(entity);
        }

        /// <summary>
        /// Entity name (name of type)
        /// </summary>
        public string EntName { get; }
        
        /// <summary>
        /// Entity ObjectId
        /// </summary>
        public ObjectId ObjectId { get; }

        /// <summary>
        /// Entity Length
        /// </summary>
        public double Length { get; }

        /// <summary>
        /// Rounded Entity Length
        /// </summary>
        public double RoundedLength => Math.Round(Length, _mainViewModel.Precision);

        /// <summary>
        /// Zoom this entity
        /// </summary>
        public ICommand ZoomCommand => new RelayCommandWithoutParameter(Zoom);

        public void RaiseRoundLengthChanged()
        {
            OnPropertyChanged(nameof(RoundedLength));
        }

        private void Zoom()
        {
            try
            {
                var doc = AcApp.DocumentManager.MdiActiveDocument;
                using (doc.LockDocument())
                {
                    var editor = AcApp.DocumentManager.MdiActiveDocument.Editor;

                    var psr = editor.SelectImplied();
                    ObjectId[] selected = null;
                    if (psr.Status == PromptStatus.OK)
                        selected = psr.Value.GetObjectIds();
                    editor.SetImpliedSelection(new[] { ObjectId });

                    Autodesk.AutoCAD.Internal.Utils.ZoomObjects(true);

                    editor.SetImpliedSelection(selected);
                }
            }
            catch (System.Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }

        private static double GetLength(Entity ent)
        {
            switch (ent.GetType().Name)
            {
                case "Line":
                    return ((Line)ent).Length;
                case "Circle":
                    return ((Circle)ent).Circumference;
                case "Polyline":
                    return ((Polyline)ent).Length;
                case "Arc":
                    return ((Arc)ent).Length;
                case "Spline":
                    return
                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).EndParam) -
                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).StartParam);
                case "Ellipse":
                    return
                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).EndParam) -
                        ((Curve)ent).GetDistanceAtParameter(((Curve)ent).StartParam);
                default:
                    return double.NaN;
            }
        }
    }
}
