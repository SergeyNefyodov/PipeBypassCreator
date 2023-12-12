using ControlzEx.Theming;
using PipeBypassCreator.ViewModels;

namespace PipeBypassCreator.Views
{
    public partial class PipeBypassCreatorView
    {
        public PipeBypassCreatorView(PipeBypassCreatorViewModel viewModel)
        {            
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}