using mpPInterface;

namespace mpSummLength
{
    public class Interface : IPluginInterface
    {
        public string Name => "mpSummLength";
        public string AvailCad => "2018";
        public string LName => "Сумма длин";
        public string Description => "Получает и выводит на экран количество и сумму длин выбранных отрезков, полилиний, окружностей, дуг и сплайнов";
        public string Author => "Пекшев Александр aka Modis";
        public string Price => "0";
    }
    public class VersionData
    {
        public const string FuncVersion = "2018";
    }
}
