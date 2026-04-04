using System.Windows.Controls;

namespace PianoTrainer2.Views
{
    public partial class TrainingView : UserControl
    {
        public TrainingView()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                // Wire highway to TrainingViewModel after controls exist
                if (DataContext is ViewModels.TrainingViewModel vm)
                    vm.AttachHighway(Highway);
            };
        }
    }
}
