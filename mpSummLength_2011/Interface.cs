using mpPInterface;

namespace mpSummLength
{
    public class Interface : IPluginInterface
    {
        private const string _Name = "mpSummLength";
        private const string _AvailCad = "2011";
        private const string _LName = "Сумма длин";
        private const string _Description = "Получает и выводит на экран количество и сумму длин выбранных отрезков, полилиний, окружностей, дуг и сплайнов";
        private const string _Author = "Пекшев Александр aka Modis";
        private const string _Price = "0";
        public string Name { get { return _Name; } }
        public string AvailCad { get { return _AvailCad; } }
        public string LName { get { return _LName; } }
        public string Description { get { return _Description; } }
        public string Author { get { return _Author; } }
        public string Price { get { return _Price; } }
    }
    public class VersionData
    {
        public const string FuncVersion = "2011";
    }
}
