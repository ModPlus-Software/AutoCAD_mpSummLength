namespace mpSummLength
{
    using System;
    using System.Collections.ObjectModel;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;

    public class EntitiesCollection : VmBase
    {
        private readonly MainViewModel _mainViewModel;
        private double _entityInfoLength = double.NaN;

        public EntitiesCollection(MainViewModel mainViewModel, string entName)
        {
            _mainViewModel = mainViewModel;
            EntityName = entName;
            EntityLocalName = GetLocalName(entName);
        }

        /// <summary>
        /// Entity name (name of type)
        /// </summary>
        public string EntityName { get; }

        /// <summary>
        /// Entity localization name
        /// </summary>
        public string EntityLocalName { get; }

        /// <summary>
        /// Collection of entities
        /// </summary>
        public ObservableCollection<EntityInfo> Entities { get; set; } = new ObservableCollection<EntityInfo>();
        
        /// <summary>
        /// Sum of entities lengths
        /// </summary>
        public double SumLength
        {
            get
            {
                if (double.IsNaN(_entityInfoLength))
                {
                    _entityInfoLength = 0.0;
                    foreach (var entityInfo in Entities)
                    {
                        _entityInfoLength += entityInfo.Length;
                    }
                }

                return Math.Round(_entityInfoLength, _mainViewModel.Precision);
            }
        }

        public void RaiseSumLengthChanged()
        {
            OnPropertyChanged(nameof(SumLength));
            foreach (var entityInfo in Entities)
            {
                entityInfo.RaiseRoundLengthChanged();
            }
        }
        
        private static string GetLocalName(string name)
        {
            switch (name)
            {
                case "Line":
                    return Language.GetItem("h9");
                case "Circle":
                    return Language.GetItem("h10");
                case "Polyline":
                    return Language.GetItem("h11");
                case "Arc":
                    return Language.GetItem("h12");
                case "Spline":
                    return Language.GetItem("h13");
                case "Ellipse":
                    return Language.GetItem("h14");
                default:
                    return name;
            }
        }
    }
}
