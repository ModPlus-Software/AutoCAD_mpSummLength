﻿namespace mpSummLength
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Windows.Input;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;

    public class MainViewModel : VmBase
    {
        private int _precision = 3;
        private double _sum = double.NaN;
        private const string LangItem = "mpSummLength";

        public MainViewModel()
        {
            if (int.TryParse(UserConfigFile.GetValue(LangItem, nameof(Precision)), out var i))
                Precision = i;
        }

        public ObservableCollection<EntitiesCollection> EntitiesCollections { get; set; } = new ObservableCollection<EntitiesCollection>();

        public MainWindow MainWindow { get; set; }

        /// <summary>
        /// Add to table command
        /// </summary>
        public ICommand AddToTableCommand => new RelayCommandWithoutParameter(AddToTable);

        /// <summary>
        /// Add as DBText command
        /// </summary>
        public ICommand AddAsDbTextCommand => new RelayCommandWithoutParameter(AddAsDbText);

        /// <summary>
        /// Export to Notepad command
        /// </summary>
        public ICommand ExportToNotepadCommand => new RelayCommandWithoutParameter(ExportToNotepad);

        /// <summary>
        /// Округление
        /// </summary>
        public int Precision
        {
            get => _precision;
            set
            {
                if (Equals(value, _precision))
                    return;
                _precision = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SumLengths));
                OnPropertyChanged(nameof(SumLengthWithFactor));
                foreach (var entitiesCollection in EntitiesCollections)
                {
                    entitiesCollection.RaiseSumLengthChanged();
                }

                UserConfigFile.SetValue(LangItem, nameof(Precision), value.ToString(), true);
            }
        }

        /// <summary>
        /// Сумма длин всех примитивов
        /// </summary>
        public double SumLengths
        {
            get
            {
                if (double.IsNaN(_sum))
                {
                    _sum = 0.0;
                    foreach (var entitiesCollection in EntitiesCollections)
                    {
                        foreach (var entityInfo in entitiesCollection.Entities)
                        {
                            _sum += entityInfo.Length;
                        }
                    }
                }

                return Math.Round(_sum, Precision);
            }
        }

        /// <summary>
        /// Сумма длин всех примитивов с множителем
        /// </summary>
        public double SumLengthWithFactor
        {
            get
            {
                if (double.IsNaN(_sum))
                {
                    _sum = 0.0;
                    foreach (var entitiesCollection in EntitiesCollections)
                    {
                        foreach (var entityInfo in entitiesCollection.Entities)
                        {
                            _sum += entityInfo.Length;
                        }
                    }
                }
                
                return Math.Round(_sum * GetFactor(), Precision);
            }
        }
        
        /// <summary>
        /// Порядковый индекс множителя
        /// </summary>
        public int FactorIndex
        {
            get => int.TryParse(UserConfigFile.GetValue(ModPlusConnector.Instance.Name, nameof(FactorIndex)), out var i) ? i : 4;
            set
            {
                UserConfigFile.SetValue(ModPlusConnector.Instance.Name, nameof(FactorIndex), value.ToString(), true);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SumLengthWithFactor));
            }
        }

        public void AddEntities(IEnumerable<EntityInfo> entities)
        {
            EntitiesCollections.Clear();
            _sum = double.NaN;
            var collections = new List<EntitiesCollection>();
            new List<string>
            {
                "Line", "Circle", "Polyline", "Arc", "Spline", "Ellipse"
            }.ForEach(e => collections.Add(new EntitiesCollection(this, e)));
            
            foreach (var grouping in entities.GroupBy(e => e.EntName))
            {
                var coll = collections.FirstOrDefault(c => c.EntityName == grouping.Key);
                if (coll != null)
                {
                    foreach (var entityInfo in grouping)
                    {
                        coll.Entities.Add(entityInfo);
                    }
                }
            }

            foreach (var collection in collections)
            {
                if (collection.Entities.Any())
                    EntitiesCollections.Add(collection);
            }

            OnPropertyChanged(nameof(SumLengths));
            OnPropertyChanged(nameof(SumLengthWithFactor));
        }

        private double GetFactor()
        {
            switch (FactorIndex)
            {
                case 0:
                    return 0.0001;
                case 1:
                    return 0.001;
                case 2:
                    return 0.01;
                case 3:
                    return 0.1;
                case 4:
                    return 1;
                case 5:
                    return 10;
                case 6:
                    return 100;
                case 7:
                    return 1000;
                case 8:
                    return 10000;
                default:
                    return 1;
            }
        }
        
        private void AddToTable()
        {
            try
            {
                MainWindow.Hide();
                ModPlus.Helpers.InsertToAutoCad.AddStringToAutoCadTableCell(
                    SumLengthWithFactor.ToString(CultureInfo.InvariantCulture).Replace(".", Variables.Separator),
                    string.Empty, true);
            }
            catch
            {
                // ignore
            }
            finally
            {
                MainWindow.Show();
            }
        }

        private void AddAsDbText()
        {
            try
            {
                MainWindow.Hide();
                ModPlus.Helpers.InsertToAutoCad.InsertDbText(
                    SumLengthWithFactor.ToString(CultureInfo.InvariantCulture).Replace(".", Variables.Separator));
            }
            catch
            {
                // ignore
            }
            finally
            {
                MainWindow.Show();
            }
        }

        private void ExportToNotepad()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(
                $"{Language.GetItem(LangItem, "h2")} {SumLengths.ToString(CultureInfo.InvariantCulture).Replace(".", Variables.Separator)} (* {GetFactor()} = {SumLengthWithFactor.ToString(CultureInfo.InvariantCulture).Replace(".", Variables.Separator)})");
            foreach (var entitiesCollection in EntitiesCollections)
            {
                stringBuilder.AppendLine(entitiesCollection.EntityLocalName);
                stringBuilder.AppendLine($"{Language.GetItem(LangItem, "h4")} {entitiesCollection.Entities.Count}");
                stringBuilder.AppendLine($"{Language.GetItem(LangItem, "h2")} " +
                                         $"{entitiesCollection.SumLength.ToString(CultureInfo.InvariantCulture).Replace(".", Variables.Separator)}");
                stringBuilder.AppendLine();
            }

            ModPlusAPI.IO.String.ShowTextWithNotepad(stringBuilder.ToString(), Language.GetItem(LangItem, "h1"));
        }
    }
}
