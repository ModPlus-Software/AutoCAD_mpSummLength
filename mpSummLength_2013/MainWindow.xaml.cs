namespace mpSummLength
{
    using System.Windows.Input;

    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("mpSummLength", "h1");
        }
        
        // Курсор попал на окно
        private void MetroWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();
        }
        // Курсор попал вне окна
        private void MetroWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            Focus();
        }
    }
}
