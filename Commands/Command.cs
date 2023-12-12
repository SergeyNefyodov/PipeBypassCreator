using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;
using PipeBypassCreator.ViewModels;
using PipeBypassCreator.Views;

namespace PipeBypassCreator.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class Command : ExternalCommand
    {
        public override void Execute()
        {
            var viewModel = new PipeBypassCreatorViewModel();
            var view = new PipeBypassCreatorView(viewModel);
            view.ShowDialog();
        }
    }
}